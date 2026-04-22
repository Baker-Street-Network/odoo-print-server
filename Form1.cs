using Newtonsoft.Json;
using Newtonsoft.Json;
using OdooPrintServer.Properties;
using PDFtoImage;
using SkiaSharp;
using System.Drawing;
using System.Drawing.Printing;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Velopack;
using Velopack.Sources;

namespace OdooPrintServer
{
    public partial class Form1 : Form
    {
        private static List<string> GetLocalIPAddresses()
        {
            List<string> ipAddresses = [];
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    ipAddresses.Add(ip.ToString());
            return ipAddresses;
        }

        public string GetSettingsDirectory()
        {
            if (string.IsNullOrEmpty(Settings.Default.configuration))
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string myAppDirectory = Path.Combine(appDataPath, "Odoo Print Server");
                Directory.CreateDirectory(myAppDirectory);
                Settings.Default.configuration = myAppDirectory;
                Settings.Default.Save();
                logs.AppendText($"Configuration directory found/created at {myAppDirectory}" + Environment.NewLine);
                return myAppDirectory;
            }

            logs.AppendText($"Configuration directory found at {Settings.Default.configuration}" + Environment.NewLine);
            return Settings.Default.configuration;
        }

        public void ReloadPrinters()
        {
            logs.AppendText("Reloading printers..." + Environment.NewLine);
            var path = Path.Combine(Settings.Default.configuration, "configuration.json");
            if (File.Exists(path))
            {
                Selections = JsonConvert.DeserializeObject<List<PrinterSelection>>(File.ReadAllText(path)) ?? [];
            }
            else
                File.WriteAllText(path, "[]");

            dataGridView1.Rows.Clear();
            foreach (PrinterSelection selection in Selections)
                dataGridView1.Rows.Add(selection.Number, selection.Name, "Edit Printer", "Delete");

            logs.AppendText("Printers reloaded." + Environment.NewLine);
        }

        public List<PrinterSelection> Selections { get; set; } = [];

        CancellationTokenSource CancellationTokenSource = new();
        private readonly CancellationTokenSource _updateCheckCts = new();
        CancellationTokenSource ConnectCancellationTokenSource = new();
        CancellationTokenSource ReceiveCancellationTokenSource = new();
        private bool _isReconnecting = false;
        private int _reconnectAttempts = 0;
        private readonly int _maxReconnectAttempts = 100; // Effectively infinite for this use case
        private readonly int _initialReconnectDelay = 1000; // 1 second initial delay
        private long _lastMessage = 0;

        public async void StartPolling()
        {
            CancellationTokenSource = new();
            ConnectCancellationTokenSource = new();
            ReceiveCancellationTokenSource = new();
            
            _reconnectAttempts = 0;
            _isReconnecting = false;
            
            logs.AppendText("Starting polling..." + Environment.NewLine);
            await ConnectAndPollWithRetry();
        }

        private async Task ConnectAndPollWithRetry()
        {
            while (!CancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    using ClientWebSocket webSocket = new();
                    webSocket.Options.SetRequestHeader("Origin", "127.0.0.1");
                    Uri serverUri = new($"{Settings.Default.url.Replace("http", "ws")}/websocket");

                    try
                    {
                        if (_isReconnecting)
                            logs.AppendText($"Attempting to reconnect (attempt {_reconnectAttempts + 1})..." + Environment.NewLine);

                        await webSocket.ConnectAsync(serverUri, ConnectCancellationTokenSource.Token);

                        if (_isReconnecting)
                        {
                            logs.AppendText("Reconnection successful!" + Environment.NewLine);
                            _isReconnecting = false;
                            _reconnectAttempts = 0;
                        }

                        if (webSocket.State == WebSocketState.Open)
                        {
                            var poll = new
                            {
                                event_name = "subscribe",
                                data = new
                                {
                                    channels = new string[] { Settings.Default.secret, "new_print_job" },
                                    last = _lastMessage
                                }
                            };
                            await webSocket.SendAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(poll)), WebSocketMessageType.Text, true, CancellationTokenSource.Token);
                        }

                        byte[] buffer = new byte[1024 * 4];
                        while (webSocket.State == WebSocketState.Open && !CancellationTokenSource.IsCancellationRequested)
                        {
                            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), ReceiveCancellationTokenSource.Token);
                            string jsonString = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            var deserializedObject = JsonConvert.DeserializeObject<dynamic>(jsonString);

                            foreach (var j in deserializedObject)
                            {
                                var job = j.message.payload;
                                var jobId = job.id;
                                var secret = job.secret;
                                var machineName = job.machine_name;
                                var printerNumber = job.printer_number;

                                _lastMessage = j.id;

                                logs.AppendText($"New print job received. Job: {jobId} Secret: {secret} Machine: {machineName} Printer: {printerNumber}" + Environment.NewLine);

                                if (machineName == Environment.MachineName && Selections.Any(s => s.Number == (int)printerNumber))
                                {
                                    string fileUrl = $"{Settings.Default.url}/odoo_print_server/download_job/{jobId}";

                                    using HttpClient client = new();
                                    var response = await client.PostAsJsonAsync(fileUrl, new { application_secret = Settings.Default.secret, job_secret = secret.ToString() });
                                    response.EnsureSuccessStatusCode();
                                    var fileBytes = await response.Content.ReadAsStringAsync();
                                    var data = JsonConvert.DeserializeObject<dynamic>(fileBytes);

                                    if (fileBytes.Contains("error"))
                                    {
                                        logs.AppendText($"Error during report download: {data.result.error}" + Environment.NewLine);
                                        continue; // Continue instead of return to keep connection alive
                                    }

                                    var rstring = data.result.data.ToString();
                                    var bytes = Convert.FromBase64String(rstring);
                                    string tempFilePath = Path.GetTempFileName();
                                    File.WriteAllBytes(tempFilePath, bytes);
                                    PrintFile(tempFilePath, (int)printerNumber);
                                }
                            }
                        }
                    }
                    catch (WebSocketException ex)
                    {
                        _isReconnecting = true;
                        _reconnectAttempts++;
                        
                        logs.AppendText($"WebSocket error: {ex.Message}. Automatically reconnecting..." + Environment.NewLine);
                        
                        // Calculate backoff delay (exponential with jitter)
                        int delayMs = Math.Min(_initialReconnectDelay * (1 << Math.Min(_reconnectAttempts, 6)), 30000);
                        delayMs = new Random().Next(delayMs / 2, delayMs); // Add jitter
                        
                        logs.AppendText($"Waiting {delayMs / 1000.0:0.#} seconds before reconnection..." + Environment.NewLine);
                        await Task.Delay(delayMs, CancellationTokenSource.Token);
                        continue; // Continue the reconnection loop
                    }
                    catch (TaskCanceledException)
                    {
                        if (CancellationTokenSource.IsCancellationRequested)
                        {
                            logs.AppendText("Polling stopped by user." + Environment.NewLine);
                            return; // Exit reconnection loop
                        }
                        
                        _isReconnecting = true;
                        _reconnectAttempts++;
                        logs.AppendText("Connection task canceled. Automatically reconnecting..." + Environment.NewLine);
                        await Task.Delay(1000, CancellationTokenSource.Token);
                        continue; // Continue the reconnection loop
                    }
                    catch (Exception ex)
                    {
                        _isReconnecting = true;
                        _reconnectAttempts++;
                        logs.AppendText($"Unexpected error: {ex.Message}. Automatically reconnecting..." + Environment.NewLine);
                        await Task.Delay(2000, CancellationTokenSource.Token);
                        continue; // Continue the reconnection loop
                    }
                }
                catch (TaskCanceledException)
                {
                    logs.AppendText("Reconnection process canceled." + Environment.NewLine);
                    return; // Exit if cancellation was requested
                }
                catch (Exception ex)
                {
                    logs.AppendText($"Critical error in connection loop: {ex.Message}" + Environment.NewLine);
                    await Task.Delay(5000, CancellationTokenSource.Token);
                }
            }
        }

        private void PrintFile(string filePath, int printerNumber)
        {
            try
            {
                var printerSelection = Selections.First(s => s.Number == printerNumber);

                string pdfPath = Path.ChangeExtension(filePath, ".pdf");
                File.Move(filePath, pdfPath);

                // Use PDFtoImage to render PDF pages and print them
                // Render at 300 DPI for high-quality printing
                var pdfBytes = File.ReadAllBytes(pdfPath);
                const int dpi = 300;
                var options = new PDFtoImage.RenderOptions { Dpi = dpi };
                var pages = Conversion.ToImages(pdfBytes, options: options).ToList();
                int currentPage = 0;

                var printDoc = new PrintDocument();
                printDoc.PrinterSettings.PrinterName = printerSelection.Settings.PrinterName;
                printDoc.PrinterSettings.Copies = printerSelection.Settings.Copies;
                printDoc.PrinterSettings.Collate = printerSelection.Settings.Collate;
                printDoc.PrinterSettings.Duplex = printerSelection.Settings.Duplex;

                // Get the first page dimensions and try to find a matching paper size
                if (pages.Count > 0)
                {
                    var firstPage = pages[0];
                    // Convert pixels to hundredths of an inch (PrintDocument uses 1/100th inch units)
                    int widthInHundredths = (int)Math.Round((firstPage.Width / (double)dpi) * 100);
                    int heightInHundredths = (int)Math.Round((firstPage.Height / (double)dpi) * 100);
                    
                    logs.AppendText($"PDF dimensions: {firstPage.Width}x{firstPage.Height} pixels @ {dpi} DPI = {widthInHundredths/100.0}\" x {heightInHundredths/100.0}\"" + Environment.NewLine);
                    
                    // Try to find a matching paper size from the printer's supported sizes
                    PaperSize? matchingSize = null;
                    var availableSizes = new List<string>();
                    foreach (PaperSize size in printDoc.PrinterSettings.PaperSizes)
                    {
                        // Collect available sizes for debugging
                        if (size.Kind != PaperKind.Custom)
                        {
                            availableSizes.Add($"{size.PaperName} ({size.Width/100.0}\"x{size.Height/100.0}\")");
                        }
                        
                        // Allow 2% tolerance for matching (some printers have slight variations)
                        if (Math.Abs(size.Width - widthInHundredths) <= Math.Max(widthInHundredths * 0.02, 5) &&
                            Math.Abs(size.Height - heightInHundredths) <= Math.Max(heightInHundredths * 0.02, 5))
                        {
                            matchingSize = size;
                            break;
                        }
                    }
                    
                    if (matchingSize != null)
                    {
                        // Use the printer's native paper size
                        printDoc.DefaultPageSettings.PaperSize = matchingSize;
                        logs.AppendText($"✓ Matched printer paper size: {matchingSize.PaperName} ({matchingSize.Width/100.0}\" x {matchingSize.Height/100.0}\")" + Environment.NewLine);
                    }
                    else
                    {
                        // Create a custom paper size
                        var customSize = new PaperSize("Custom", widthInHundredths, heightInHundredths);
                        printDoc.DefaultPageSettings.PaperSize = customSize;
                        logs.AppendText($"⚠ No matching paper size found for {widthInHundredths/100.0}\" x {heightInHundredths/100.0}\"" + Environment.NewLine);
                        logs.AppendText($"⚠ Using custom size - printer driver may override!" + Environment.NewLine);
                        logs.AppendText($"Available sizes: {string.Join(", ", availableSizes.Take(5))}" + (availableSizes.Count > 5 ? $" and {availableSizes.Count - 5} more..." : "") + Environment.NewLine);
                        logs.AppendText($"→ Configure your printer driver to support {widthInHundredths/100.0}\" x {heightInHundredths/100.0}\" labels." + Environment.NewLine);
                    }
                }

                printDoc.PrintPage += (sender, e) =>
                {
                    if (currentPage < pages.Count)
                    {
                        var skBitmap = pages[currentPage];
                        using var bitmap = skBitmap.ToBitmap();
                        
                        // Log what the printer driver is actually using
                        if (currentPage == 0)
                        {
                            logs.AppendText($"Printer is using: {e.PageSettings.PaperSize.PaperName} ({e.PageSettings.PaperSize.Width/100.0}\" x {e.PageSettings.PaperSize.Height/100.0}\"), PrintableArea: {e.PageBounds.Width/e.Graphics!.DpiX:F2}\" x {e.PageBounds.Height/e.Graphics.DpiY:F2}\"" + Environment.NewLine);
                        }
                        
                        // Draw the image at the correct physical size
                        // The page size is already set, so just draw at the correct scale
                        float widthInInches = skBitmap.Width / (float)dpi;
                        float heightInInches = skBitmap.Height / (float)dpi;
                        float widthInGraphicsUnits = widthInInches * e.Graphics!.DpiX;
                        float heightInGraphicsUnits = heightInInches * e.Graphics.DpiY;
                        
                        e.Graphics.DrawImage(bitmap, 0, 0, widthInGraphicsUnits, heightInGraphicsUnits);
                        currentPage++;
                        e.HasMorePages = currentPage < pages.Count;
                    }
                };

                printDoc.Print();
                
                // Clean up SKBitmap objects
                foreach (var page in pages)
                {
                    page?.Dispose();
                }

                logs.AppendText($"Sent print job to printer {printerSelection.Settings.PrinterName}." + Environment.NewLine);
                
                // Clean up temporary file
                try { File.Delete(pdfPath); } catch { }
            }
            catch (Exception e)
            {
                logs.AppendText($"Error: {e}" + Environment.NewLine);
            }
        }

        public Form1()
        {
            InitializeComponent();

            printIps.Text += string.Join(", ", GetLocalIPAddresses());

            pathTextbox.Text = GetSettingsDirectory();
            odooUrl.Text = Settings.Default.url;
            odooSecret.Text = Settings.Default.secret;
            ReloadPrinters();

            //start the long polling here
            if (!string.IsNullOrEmpty(odooUrl.Text) && !string.IsNullOrEmpty(odooSecret.Text))
                StartPolling();

            logs.AppendText("Application started." + Environment.NewLine);

            // Start background update checker
            _ = Task.Run(() => UpdateCheckLoopAsync(_updateCheckCts.Token));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _updateCheckCts.Cancel();
            base.OnFormClosing(e);
        }

        /// <summary>Thread-safe log append.</summary>
        private void AppendLog(string message)
        {
            if (logs.InvokeRequired)
                logs.Invoke(() => logs.AppendText(message + Environment.NewLine));
            else
                logs.AppendText(message + Environment.NewLine);
        }

        private async Task UpdateCheckLoopAsync(CancellationToken cancellationToken)
        {
            // Wait 30 seconds after startup before first check
            try { await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); }
            catch (TaskCanceledException) { return; }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForUpdatesAsync();
                }
                catch (Exception ex)
                {
                    AppendLog($"Update check error: {ex.Message}");
                }

                try { await Task.Delay(TimeSpan.FromHours(4), cancellationToken); }
                catch (TaskCanceledException) { return; }
            }
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                AppendLog("Checking for updates...");
                var source = new GithubSource(
                    "https://github.com/Baker-Street-Network/odoo-print-server",
                    null,
                    false);
                var updateManager = new UpdateManager(source);

                var newVersion = await updateManager.CheckForUpdatesAsync();
                if (newVersion == null)
                {
                    AppendLog("No updates available.");
                    return;
                }

                AppendLog($"Update available: {newVersion.TargetFullRelease.Version}. Downloading...");
                await updateManager.DownloadUpdatesAsync(newVersion);
                AppendLog("Update downloaded. Restarting to apply...");
                updateManager.ApplyUpdatesAndRestart(newVersion);
            }
            catch (Exception ex) when (
                ex.Message.Contains("not currently installed") ||
                ex.Message.Contains("Unable to locate"))
            {
                // Running in dev/debug mode — not installed via Velopack, skip silently.
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PrintDialog printDialog = new();
            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                using Prompt prompt = new("Give your printer configuration a name:", "Name Configuraton");
                UpdateConfiguration(printDialog.PrinterSettings, prompt.Result);
            }
        }

        public void UpdateConfiguration(PrinterSettings settings, string name, int? id = null)
        {
            var mapped = new PrinterConfig
            {
                PrinterName = settings.PrinterName,
                PrintFileName = settings.PrintFileName,
                Collate = settings.Collate,
                Copies = settings.Copies,
                Duplex = settings.Duplex,
                FromPage = settings.FromPage,
                PrintRange = settings.PrintRange,
                PrintToFile = settings.PrintToFile,
                ToPage = settings.ToPage,
            };

            if (id == null)
            {

                var nextNumber = Selections.Count == 0 ? 1 : Selections.Max(s => s.Number) + 1;
                Selections.Add(new PrinterSelection
                {
                    Name = name,
                    Number = nextNumber,
                    Settings = mapped
                });
            }
            else
            {
                var selection = Selections.First(s => s.Number == id);
                selection.Name = name;
                selection.Settings = mapped;
            }

            SaveNewConfiguration();
        }

        public void SaveNewConfiguration()
        {
            var path = Path.Combine(Settings.Default.configuration, "configuration.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(Selections, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            }));
            ReloadPrinters();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            var result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                pathTextbox.Text = folderBrowserDialog1.SelectedPath;
            }
            Settings.Default.configuration = pathTextbox.Text;
            Settings.Default.Save();
            ReloadPrinters();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            pathTextbox.Text = GetSettingsDirectory();
            Settings.Default.configuration = pathTextbox.Text;
            Settings.Default.Save();
            ReloadPrinters();
        }

        private async void connectButton_Click(object sender, EventArgs e)
        {
            // First, ensure all previous connections are properly cancelled
            CancellationTokenSource.Cancel();
            ConnectCancellationTokenSource.Cancel();
            ReceiveCancellationTokenSource.Cancel();
            
            // Wait a bit to ensure connections have time to close properly
            await Task.Delay(300);
            
            string url = odooUrl.Text + "/odoo_print_server/verify_connection";
            using HttpClient client = new HttpClient();

            try
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(url, new
                {
                    application_secret = Settings.Default.secret
                });
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logs.AppendText("The Odoo Print Server is not installed on this Odoo instance." + Environment.NewLine);
                    MessageBox.Show("The Odoo Print Server is not installed on this Odoo instance.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseBody);
                if (jsonResponse != null && responseBody.Contains("error"))
                {
                    logs.AppendText("Error: " + jsonResponse.result.error.ToString() + Environment.NewLine);
                    MessageBox.Show("Error: " + jsonResponse.result.error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    logs.AppendText("Connection successful!" + Environment.NewLine);
                    MessageBox.Show("Connection successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (HttpRequestException ex)
            {
                logs.AppendText("Request error: " + ex.Message + Environment.NewLine);
                MessageBox.Show("Request error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                logs.AppendText("Unexpected error: " + ex.Message + Environment.NewLine);
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Start new connections after previous ones are properly closed
            StartPolling();
        }

        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1)
            {
                var row = dataGridView1.Rows[e.RowIndex];
                var newName = row.Cells[1].Value.ToString();
                Selections.First(s => s.Number == int.Parse(row.Cells[0].Value.ToString()!)).Name = newName ?? "";
                SaveNewConfiguration();
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            // edit button
            if (dataGridView1.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.ColumnIndex == 2)
            {
                int number = int.Parse(dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString()!);

                PrintDialog printDialog = new();
                var selection = Selections.First(s => s.Number == number);

                printDialog.PrinterSettings.PrinterName = selection.Settings.PrinterName;
                printDialog.PrinterSettings.PrintFileName = selection.Settings.PrintFileName;
                printDialog.PrinterSettings.Collate = selection.Settings.Collate;
                printDialog.PrinterSettings.Copies = selection.Settings.Copies;
                printDialog.PrinterSettings.Duplex = selection.Settings.Duplex;
                printDialog.PrinterSettings.FromPage = selection.Settings.FromPage;
                printDialog.PrinterSettings.PrintRange = selection.Settings.PrintRange;
                printDialog.PrinterSettings.PrintToFile = selection.Settings.PrintToFile;
                printDialog.PrinterSettings.ToPage = selection.Settings.ToPage;

                if (printDialog.ShowDialog() == DialogResult.OK)
                    UpdateConfiguration(printDialog.PrinterSettings, selection.Name, number);
            }

            // delete button
            if (dataGridView1.Columns[e.ColumnIndex] is DataGridViewButtonColumn && e.ColumnIndex == 3)
            {
                var confirmResult = MessageBox.Show("Are you sure to delete this printer?",
                    "Confirm Delete",
                    MessageBoxButtons.YesNo);

                if (confirmResult == DialogResult.Yes)
                {
                    int number = int.Parse(dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString()!);
                    Selections.RemoveAll(s => s.Number == number);
                    SaveNewConfiguration();
                }
            }
        }

        private void odooUrl_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.url = odooUrl.Text;
            Settings.Default.Save();
        }

        private async void syncDetailsButton_Click(object sender, EventArgs e)
        {
            string url = odooUrl.Text + "/odoo_print_server/sync_remote_channels";
            using HttpClient client = new();

            try
            {
                HttpResponseMessage response = await client.PostAsJsonAsync(url, new
                {
                    machine_name = Environment.MachineName,
                    channels = Selections.Select(s => (
                        new
                        {
                            number = s.Number,
                            name = s.Name,
                            printer_name = s.Settings.PrinterName
                        }))
                });

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    logs.AppendText("The Odoo Print Server is not installed on this Odoo instance." + Environment.NewLine);
                    MessageBox.Show("The Odoo Print Server is not installed on this Odoo instance.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string responseBody = await response.Content.ReadAsStringAsync();

                var jsonResponse = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseBody);
                if (jsonResponse != null && jsonResponse.ContainsKey("error"))
                {
                    logs.AppendText("Error: " + jsonResponse["error"].ToString() + Environment.NewLine);
                    MessageBox.Show("Error: " + jsonResponse["error"].ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    logs.AppendText("Sync completed successfully." + Environment.NewLine);
                    MessageBox.Show("Sync completed successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (HttpRequestException ex)
            {
                logs.AppendText("Request error: " + ex.Message + Environment.NewLine);
                MessageBox.Show("Request error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                logs.AppendText("Unexpected error: " + ex.Message + Environment.NewLine);
                MessageBox.Show("Unexpected error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            Settings.Default.secret = odooSecret.Text;
            Settings.Default.Save();
        }

        private void logs_TextChanged(object sender, EventArgs e)
        {

        }
    }

    public static class SKBitmapExtensions
    {
        public static Bitmap ToBitmap(this SKBitmap skBitmap)
        {
            using var ms = new MemoryStream();
            skBitmap.Encode(ms, SKEncodedImageFormat.Png, 100);
            ms.Seek(0, SeekOrigin.Begin);
            return new Bitmap(ms);
        }
    }
}
