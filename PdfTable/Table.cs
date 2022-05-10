using PdfSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PdfTable
{
    public class Table
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public TableRow Header { get; set; }
        public List<TableRow> Rows { get; set; }
        public TableRow Summary { get; set; }
        public XGraphics GFX { get; set; }
        public double[] ColumnsWidths { get; set; }
        public bool ColumnsEquals { get; set; }
        public int ColumnsCount { get; set; }
        public double X { get; set; }
        public double Y { get; set; }

        public Table()
        {
            ColumnsWidths = new double[ColumnsCount];

            if (ColumnsEquals)
            {
                var columnWidth = Width / ColumnsCount;
                for (int i = 0; i < ColumnsCount - 1; i++)
                {
                    ColumnsWidths[i] = columnWidth;
                }
            }
        }
        public void Draw(double x, double y)
        {
            X = x;
            Y = y;
            NormalizeColumnsWidths(ColumnsWidths, X);
            FillInData(y);
            var xPen = new XPen(XColors.Black, 1);
            Height = Header != null ? Summary != null ? Rows.Sum(x => x.Height) + Header.Height + Summary.Height : Rows.Sum(x => x.Height) + Header.Height : Rows.Sum(x => x.Height);
            DrawTableBorders(x, y, xPen);
            DrawRowsAndColumns(x, y, xPen);

        }

        private void DrawRowsAndColumns(double x, double y, XPen xPen)
        {
            var rowBeginingY = y;
            if (Header != null)
            {
                var pen = new XPen(XColors.Black, 2);
                GFX.DrawLine(pen, new XPoint(x, y + Header.Height), new XPoint(x + Width, y + Header.Height));

                rowBeginingY = rowBeginingY + Header.Height;

            }
            foreach (var row in Rows)
            {
                row.Y = rowBeginingY;
                GFX.DrawLine(xPen, new XPoint(x, rowBeginingY + row.Height), new XPoint(x + Width, rowBeginingY + row.Height));
                rowBeginingY = rowBeginingY + row.Height;
            }
            if (Summary != null)
            {
                GFX.DrawLine(xPen, new XPoint(x, rowBeginingY + Summary.Height), new XPoint(x + Width, rowBeginingY + Summary.Height));
            }

            foreach (var col in ColumnsWidths)
            {
                GFX.DrawLine(xPen, new XPoint(col, y), new XPoint(col, y + Height));
            }
        }

        private void DrawTableBorders(double x, double y, XPen xPen)
        {
            GFX.DrawLine(xPen, new XPoint(x, y), new XPoint(x + Width, y));
            GFX.DrawLine(xPen, new XPoint(x, y), new XPoint(x, y + Height));
            GFX.DrawLine(xPen, new XPoint(x + Width, y), new XPoint(x + Width, y + Height));
            GFX.DrawLine(xPen, new XPoint(x, y + Height), new XPoint(x + Width, y + Height));
        }

        private void FillInData(double y)
        {
            if (Header != null)
            {
                FillRowData(Header, GFX, y);
            }
            for (int i = 0; i < Rows.Count; i++)
            {
                var yOrigin = i == 0 ? (Header != null ? Header.Y + Header.Height : Y) : Rows[i - 1].Y + Rows[i - 1].Height;
                FillRowData(Rows[i], GFX, yOrigin);
            }
            if (Summary != null)
            {
                var yOrigin = Rows[Rows.Count - 1].Y + Rows[Rows.Count - 1].Height;
                FillRowData(Summary, GFX, yOrigin, true);
            }
        }

        public void FillRowData(TableRow row, XGraphics gfx, double yOrigin, bool bold = false)
        {
            var cellFont = new XFont("Courier", 10, XFontStyle.Regular);
            if (bold)
            {
                cellFont = new XFont("Courier", 10, XFontStyle.Bold);
            }

            row.Y = yOrigin;
            for (int i = 0; i < row.Content?.Length; i++)
            {
                PrepareDateForCell(row, gfx, yOrigin, cellFont, i);
            }
            foreach (var contentList in row.ContentToDraw)
            {
                for (int i = 0; i < contentList.Count; i++)
                {
                    var item = contentList[i];
                    gfx.DrawString(item.String, cellFont, XBrushes.Black, new XRect(item.X, contentList[0].Y + i * row.Height / contentList.Count, item.Width, row.Height / contentList.Count), XStringFormats.Center);

                }

            }
        }

        private void PrepareDateForCell(TableRow row, XGraphics gfx, double yOrigin, XFont underText, int i)
        {
            row.ContentToDraw.Add(new List<CellContent>());
            var content = row.Content[i];
            var x = i == 0 ? X : ColumnsWidths[i - 1];
            var width = i == 0 ? ColumnsWidths[i] - X : ColumnsWidths[i] - ColumnsWidths[i - 1];
            var stringSize = gfx.MeasureString(content, underText);
            if (width < stringSize.Width)
            {
                FitStringToCell(row, yOrigin, i, content, x, width, stringSize, 10);

            }
            else
            {
                row.ContentToDraw[i].Add(new CellContent { String = row.Content[i], StringHeight = stringSize.Height, Width = width, X = x, Y = yOrigin });
                if (row.Height < stringSize.Height)
                {
                    row.Height = stringSize.Height;
                }
            }
        }

        private void FitStringToCell(TableRow row, double yOrigin, int i, string content, double x, double width, XSize stringSize, double verticalPadding)
        {
            var stringPartsToFitTheCell = stringSize.Width / width;
            var stringLengthToFitTheCell = Math.Floor(content.Length / stringPartsToFitTheCell);
            var startIndex = 0;
            var iterator = 0;
            int indexOfWhiteSpace;
            do
            {
                indexOfWhiteSpace = FindIndexOfBestWhiteSpace(content, stringLengthToFitTheCell, startIndex, startIndex);
                var subString = indexOfWhiteSpace == -1 ? content.Substring(startIndex) : content.Substring(startIndex, indexOfWhiteSpace - startIndex);
                startIndex = indexOfWhiteSpace + 1;
                row.ContentToDraw[i].Add(new CellContent { String = subString, StringHeight = stringSize.Height, Width = width, X = x, Y = yOrigin + iterator * stringSize.Height });

                if (row.Height < (iterator * stringSize.Height) + verticalPadding)
                {
                    row.Height = iterator * stringSize.Height + verticalPadding;
                }

                iterator++;
            }
            while (indexOfWhiteSpace != -1);
        }

        private void NormalizeColumnsWidths(double[] columnWidths, double rowBeginingX)
        {
            var columnsWidthSum = columnWidths.Sum();
            double colWidth = rowBeginingX;
            for (int i = 0; i < columnWidths.Length; i++)
            {
                colWidth += Width / columnsWidthSum * columnWidths[i];
                columnWidths[i] = colWidth;
            }
        }

        private int FindIndexOfBestWhiteSpace(string content, double number, int startIndex = 0, int previousWhiteSpace = 0)
        {
            var index = content.IndexOf(' ', startIndex);
            if (index == -1)
            {
                if (content.Length - previousWhiteSpace <= number)
                {
                    return index;
                }
                index = content.Length;
            }
            if ((index - previousWhiteSpace) < number)
            {
                return FindIndexOfBestWhiteSpace(content, number, index + 1, previousWhiteSpace);
            }
            if ((index - previousWhiteSpace) == number)
            {
                return index;
            }
            if (startIndex != previousWhiteSpace)
            {
                return startIndex - 1;
            }

            return index;
        }
    }
}
