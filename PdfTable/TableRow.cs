using System;
using System.Collections.Generic;
using System.Text;

namespace PdfTable
{
    public class TableRow
    {
        public double Height { get; set; }
        public double Y { get; set; }
        public string[] Content { get; set; }
        public List<List<CellContent>> ContentToDraw { get; set; } = new List<List<CellContent>>();

    }
}
