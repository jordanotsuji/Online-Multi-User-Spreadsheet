using System;
using SpreadsheetUtilities;
using SS;

namespace ExecutableSpreadsheet
{
    class Program
    {
        static void Main(string[] args)
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "2");
            s.SetContentsOfCell("a2", "a1");
            s.SetContentsOfCell("a3", "=a1");
            s.Save("file.xml");

            Spreadsheet n = new Spreadsheet();
            n.SetContentsOfCell("a1", "42");
            n.GetSavedVersion("file.xml");
            n.Save("file2.xml");
        }
    }
}
