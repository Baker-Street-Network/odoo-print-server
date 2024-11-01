using System.Drawing.Printing;

namespace OdooPrintServer
{
    public class PrinterSelection
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public PrinterConfig Settings { get; set; }
    }

    public class PrinterConfig
    {
        public string PrintFileName { get; set; }
        public string PrinterName { get; set; }
        public short Copies { get; set; }
        public bool Collate { get; set; }
        public bool PrintToFile { get; set; }
        public Duplex Duplex { get; set; }
        public int FromPage { get; set; }
        public int ToPage { get; set; }
        public PrintRange PrintRange { get; set; }
    }
}
