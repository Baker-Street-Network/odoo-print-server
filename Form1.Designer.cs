namespace OdooPrintServer
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            printIps = new Label();
            dataGridView1 = new DataGridView();
            numberColumn = new DataGridViewTextBoxColumn();
            configNameColumn = new DataGridViewTextBoxColumn();
            editGridButton = new DataGridViewButtonColumn();
            removeGridButton = new DataGridViewButtonColumn();
            printDialog1 = new PrintDialog();
            addPrinterButton = new Button();
            printDocument1 = new System.Drawing.Printing.PrintDocument();
            pathTextbox = new TextBox();
            folderBrowserDialog1 = new FolderBrowserDialog();
            relocateButton = new Button();
            resetButton = new Button();
            label1 = new Label();
            label2 = new Label();
            odooUrl = new TextBox();
            connectButton = new Button();
            ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
            SuspendLayout();
            // 
            // printIps
            // 
            printIps.AutoSize = true;
            printIps.Location = new Point(12, 522);
            printIps.Name = "printIps";
            printIps.Size = new Size(277, 25);
            printIps.TabIndex = 1;
            printIps.Text = "This printer server's IP address is: ";
            // 
            // dataGridView1
            // 
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.AllowUserToDeleteRows = false;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridView1.Columns.AddRange(new DataGridViewColumn[] { numberColumn, configNameColumn, editGridButton, removeGridButton });
            dataGridView1.Location = new Point(12, 205);
            dataGridView1.Name = "dataGridView1";
            dataGridView1.RowHeadersWidth = 62;
            dataGridView1.Size = new Size(831, 300);
            dataGridView1.TabIndex = 2;
            dataGridView1.CellContentClick += dataGridView1_CellContentClick;
            dataGridView1.CellValueChanged += dataGridView1_CellValueChanged;
            // 
            // numberColumn
            // 
            numberColumn.HeaderText = "Number";
            numberColumn.MinimumWidth = 8;
            numberColumn.Name = "numberColumn";
            numberColumn.ReadOnly = true;
            numberColumn.Width = 150;
            // 
            // configNameColumn
            // 
            configNameColumn.HeaderText = "Configuration Name";
            configNameColumn.MinimumWidth = 8;
            configNameColumn.Name = "configNameColumn";
            configNameColumn.Width = 300;
            // 
            // editGridButton
            // 
            editGridButton.HeaderText = "";
            editGridButton.MinimumWidth = 8;
            editGridButton.Name = "editGridButton";
            editGridButton.Text = "Edit";
            editGridButton.Width = 150;
            // 
            // removeGridButton
            // 
            removeGridButton.HeaderText = "";
            removeGridButton.MinimumWidth = 8;
            removeGridButton.Name = "removeGridButton";
            removeGridButton.Text = "Remove";
            removeGridButton.Width = 150;
            // 
            // printDialog1
            // 
            printDialog1.UseEXDialog = true;
            // 
            // addPrinterButton
            // 
            addPrinterButton.Location = new Point(12, 165);
            addPrinterButton.Name = "addPrinterButton";
            addPrinterButton.Size = new Size(112, 34);
            addPrinterButton.TabIndex = 3;
            addPrinterButton.Text = "Add Printer";
            addPrinterButton.UseVisualStyleBackColor = true;
            addPrinterButton.Click += button1_Click;
            // 
            // pathTextbox
            // 
            pathTextbox.Location = new Point(12, 114);
            pathTextbox.Name = "pathTextbox";
            pathTextbox.ReadOnly = true;
            pathTextbox.Size = new Size(595, 31);
            pathTextbox.TabIndex = 4;
            // 
            // relocateButton
            // 
            relocateButton.Location = new Point(613, 114);
            relocateButton.Name = "relocateButton";
            relocateButton.Size = new Size(112, 34);
            relocateButton.TabIndex = 5;
            relocateButton.Text = "Relocate";
            relocateButton.UseVisualStyleBackColor = true;
            relocateButton.Click += button2_Click;
            // 
            // resetButton
            // 
            resetButton.Location = new Point(731, 114);
            resetButton.Name = "resetButton";
            resetButton.Size = new Size(112, 34);
            resetButton.TabIndex = 6;
            resetButton.Text = "Reset";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Click += button3_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(9, 82);
            label1.Name = "label1";
            label1.Size = new Size(236, 25);
            label1.TabIndex = 7;
            label1.Text = "Configuration file located at:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(9, 9);
            label2.Name = "label2";
            label2.Size = new Size(113, 25);
            label2.TabIndex = 8;
            label2.Text = "Odoo Server";
            // 
            // odooUrl
            // 
            odooUrl.Location = new Point(12, 37);
            odooUrl.Name = "odooUrl";
            odooUrl.Size = new Size(713, 31);
            odooUrl.TabIndex = 9;
            odooUrl.TextChanged += odooUrl_TextChanged;
            // 
            // connectButton
            // 
            connectButton.Location = new Point(731, 34);
            connectButton.Name = "connectButton";
            connectButton.Size = new Size(112, 34);
            connectButton.TabIndex = 10;
            connectButton.Text = "Connect";
            connectButton.UseVisualStyleBackColor = true;
            connectButton.Click += connectButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(855, 563);
            Controls.Add(connectButton);
            Controls.Add(odooUrl);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(resetButton);
            Controls.Add(relocateButton);
            Controls.Add(pathTextbox);
            Controls.Add(addPrinterButton);
            Controls.Add(dataGridView1);
            Controls.Add(printIps);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Form1";
            Text = "Odoo Print Server";
            ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private Label printIps;
        private DataGridView dataGridView1;
        private PrintDialog printDialog1;
        private Button addPrinterButton;
        private System.Drawing.Printing.PrintDocument printDocument1;
        private TextBox pathTextbox;
        private FolderBrowserDialog folderBrowserDialog1;
        private Button relocateButton;
        private Button resetButton;
        private Label label1;
        private Label label2;
        private TextBox odooUrl;
        private Button connectButton;
        private DataGridViewTextBoxColumn numberColumn;
        private DataGridViewTextBoxColumn configNameColumn;
        private DataGridViewButtonColumn editGridButton;
        private DataGridViewButtonColumn removeGridButton;
    }
}
