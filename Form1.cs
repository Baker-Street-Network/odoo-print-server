using Newtonsoft.Json;
using OdooPrintServer.Properties;
using System.Drawing.Printing;
using System.Net;
using System.Net.Sockets;

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

        public static string GetSettingsDirectory()
        {
            if (string.IsNullOrEmpty(Settings.Default.configuration))
            {
                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string myAppDirectory = Path.Combine(appDataPath, "Odoo Print Server");
                Directory.CreateDirectory(myAppDirectory);
                Settings.Default.configuration = myAppDirectory;
                Settings.Default.Save();
                return myAppDirectory;
            }

            return Settings.Default.configuration;
        }

        public void ReloadPrinters()
        {
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
        }

        public List<PrinterSelection> Selections { get; set; } = [];

        public Form1()
        {
            InitializeComponent();

            printIps.Text += string.Join(", ", GetLocalIPAddresses());

            pathTextbox.Text = GetSettingsDirectory();
            ReloadPrinters();
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

        private void connectButton_Click(object sender, EventArgs e)
        {
            
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
                int number = int.Parse(dataGridView1.Rows[e.RowIndex].Cells[0].Value.ToString()!);
                Selections.RemoveAll(s => s.Number == number);
                SaveNewConfiguration();
            }
        }

        private void odooUrl_TextChanged(object sender, EventArgs e) =>
            Settings.Default.url = odooUrl.Text;
    }
}
