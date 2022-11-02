// Written by Alan Bird 9/2020 for CS3500 PS4
// 
//  (Version 1.0) - Main implemenation and simple test cases added.
//  (Version 1.1) - Finished implementation and reached 100% test 
//                  code coverage. 
//  (Version 2.0) - Essentially final version. Added tests around
//                  inserting and replacing empty cells, and reworked
//                  all replace contents methods. 
//  (Version 3.0) - Updated to PS5 required methods. Added cell value function,
//                  updated setCell methods and added tests. Still need to add
//                  XML support. 
//  (Version 3.1) - Fixed a few bugs and begun XML support. 
//  (Version 3.2) - Final Version. Finalized XML support and finished test cases. 



using SpreadsheetUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SS
{
    /// <summary>
    /// A Spreadsheet object that contains a few methods required for the brains of a 
    /// spreadsheet. Contains an infinite number of named cells and the values of 
    /// those cells. An empty cell is a cell that contains an empty string, and a new 
    /// Spreadsheet will contain all empty cells. 
    /// </summary>
    public class Spreadsheet : AbstractSpreadsheet
    {
        private DependencyGraph depGraph = new DependencyGraph();
        private Dictionary<string, Cell> cellDictionary = new Dictionary<string, Cell>();

        private bool internalChanged = false;
        public override bool Changed { get { return internalChanged; } protected set { internalChanged = value; } }

        /// <summary>
        /// Creates a default spreadsheet with no particular normalizer or validator. The version is set to default. 
        /// </summary>
        public Spreadsheet() : base(s => true, s => s, "default")
        {
        }

        /// <summary>
        /// Creates a spreadsheet with the given validator, normalizer and with the specified version.
        /// </summary>
        public Spreadsheet(Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
        }

        /// <summary>
        /// Creates a spreadsheet from the file provided, with the given validator, normalizer. If the version spesified in the xml document and here 
        /// int the constructor do not match an error will be thrown. 
        /// </summary>
        public Spreadsheet(String file, Func<string, bool> isValid, Func<string, string> normalize, string version) : base(isValid, normalize, version)
        {
            SetSpeadsheetToFile(file);
        }


        public override object GetCellContents(string name)
        {
            if (name == null || !SystemValid(name))
                throw new InvalidNameException();
            name = Normalize(name);
            if (cellDictionary.ContainsKey(name))
                return cellDictionary[name].contents;
            else
                return "";
        }

        /// <summary>
        /// Determines if a single given string is a valid cell name. I.E. it is one or more letters followed by one or more digits.
        /// </summary>
        private bool SystemValid(string s)
        {
            Regex pattern = new Regex("^[a-zA-Z]+[0-9]+$");
            if (!pattern.IsMatch(s))
                return false;
            if (!IsValid(s))
                return false;
            return true;
        }

        public override IEnumerable<string> GetNamesOfAllNonemptyCells()
        {
            foreach (string s in cellDictionary.Keys)
                yield return s;
        }

        //These methods are now obsolete code because the other set method bypasses them entierly. 
        //They cannot be tested because they are protected but they work in theory.
        protected override IList<string> SetCellContents(string name, double number)
        {
            return SystemSetAnyCellContents(name, number);
        }

        protected override IList<string> SetCellContents(string name, string text)
        {
            return SystemSetAnyCellContents(name, text);
        }

        protected override IList<string> SetCellContents(string name, Formula formula)
        {
            return SystemSetAnyCellContents(name, formula);
        }

        /// <summary>
        /// Simplified helper method for all three methods that set contents. If the 
        /// name or contents are null, it should throw the correct exception. If the 
        /// contents are a empty string it should return the same list specified in the
        /// contents adders above, but should not add the cell to the dictionary. For
        /// other details see the other contents setters above. 
        /// </summary>
        private IList<string> SystemSetAnyCellContents(string name, object contents)
        {
            if (name == null || !SystemValid(name))
                throw new InvalidNameException();

            Object oldContents = "";
            IEnumerable<string> oldDependents = new string[] { };


            if (cellDictionary.ContainsKey(name))
            {
                //if its a formula, save the origonal contents of named cell and the dependencies.
                //If it throws catch it, reset everything and throw again. 
                if (typeof(Formula).IsInstanceOfType(contents))
                {
                    oldContents = cellDictionary[name].contents;
                    oldDependents = depGraph.GetDependents(name);
                }
                cellDictionary[name] = new Cell(contents, SystemLookupForCells);
            }
            else
                cellDictionary.Add(name, new Cell(contents, SystemLookupForCells));

            //if the contents are a string and the string is empty you must remove the
            //name key from the dictionary. 
            if (typeof(string).IsInstanceOfType(contents) && (string)contents == "")
                cellDictionary.Remove(name);

            if (typeof(Formula).IsInstanceOfType(contents))
            {
                Formula f = (Formula)contents;
                depGraph.ReplaceDependents(name, f.GetVariables());
            }
            else
                depGraph.ReplaceDependents(name, new String[] { });

            //ensures the entire spreadsheet is reset to its original position 
            //instead of updating if a circular exception is thrown.
            try
            {
                if (cellDictionary.ContainsKey(name))
                    RecalculateCells(GetCellsToRecalculate(name));

                return GetCellsToRecalculate(name).ToList();
            }
            catch (CircularException)
            {
                cellDictionary[name] = new Cell(oldContents, SystemLookupForCells);
                if (typeof(Formula).IsInstanceOfType(oldContents))
                    depGraph.ReplaceDependents(name, ((Formula)oldContents).GetVariables());
                else
                    depGraph.ReplaceDependents(name, new String[] { });
                throw new CircularException();
            }
        }

        /// <summary>
        /// Recalculates all cells in the given IEnumerable of cells. 
        /// </summary>
        /// <param name="cells"></param>
        private void RecalculateCells(IEnumerable<string> names)
        {
            foreach (string n in names)
            {
                cellDictionary[n].Recalculate(SystemLookupForCells);
            }
        }

        /// <summary>
        /// Lookup method to pass into the Formula constructor so it can access the cellDictionary. 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private double SystemLookupForCells(string name)
        {
            if (cellDictionary.ContainsKey(name) && double.TryParse(cellDictionary[name].value.ToString(), out double d))
                return d;
            throw new System.ArgumentException("This cell is not a number");

        }

        protected override IEnumerable<string> GetDirectDependents(string name)
        {
            return depGraph.GetDependees(name);
        }

        /// <summary>
        /// Sets the spreadsheet to the contents of the given xmlFile then returns the version of the new Spreadsheet. 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private string SetSpeadsheetToFile(string filename)
        {

            LinkedList<string> keys = new LinkedList<string>(cellDictionary.Keys);
            foreach (string s in keys)
            {
                SetContentsOfCell(s, "");
            }

            string newVersion = null;
            bool inACell = false;
            string cellName = "";
            string cellContents = ""; try
            {

                using (XmlReader xmlR = XmlReader.Create(filename))
                {
                    while (xmlR.Read())
                    {
                        if (xmlR.IsStartElement())
                        {
                            switch (xmlR.Name)
                            {
                                case "spreadsheet":
                                    if (Version != xmlR.GetAttribute("version") && Version != "default")
                                        throw new SpreadsheetReadWriteException("The version in the xml file does not match that of the constructor.");
                                    newVersion = xmlR.GetAttribute("version");
                                    break;

                                case "cell":
                                    inACell = true;
                                    break;
                                case "name":
                                    if (inACell)
                                    {
                                        xmlR.Read();
                                        cellName = xmlR.Value;
                                    }
                                    else
                                        throw new SpreadsheetReadWriteException("XML file has a formatting error, there is a name outside a cell.");
                                    break;
                                case "contents":
                                    if (inACell)
                                    {
                                        xmlR.Read();
                                        cellContents = xmlR.Value;
                                        try { SetContentsOfCell(cellName, cellContents); }
                                        catch
                                        {
                                            throw new SpreadsheetReadWriteException("There was a problem with the names or " +
                                        "contents of the cells in the file. Make sure there are no circular exceptions and every formula and token is valid.");
                                        }
                                    }
                                    else
                                        throw new SpreadsheetReadWriteException("XML file has a formatting error, there are contents outside a cell.");
                                    break;
                            }
                        }
                    }


                }
            }
            catch (Exception e)
            {

                throw new SpreadsheetReadWriteException(e.Message);
            }
            Changed = false;
            return newVersion;

        }

        public override string GetSavedVersion(string filename)
        {
            try
            {
                using (XmlReader xmlR = XmlReader.Create(filename))
                {
                    while (xmlR.Read())
                    {
                        if (xmlR.IsStartElement())
                        {
                            switch (xmlR.Name)
                            {
                                case "spreadsheet":
                                    if (Version != xmlR.GetAttribute("version") && Version != "default")
                                        throw new SpreadsheetReadWriteException("The version in the xml file does not match that of the constructor.");
                                    return xmlR.GetAttribute("version");
                            }
                            return null;

                        }
                    }
                }
                throw new SpreadsheetReadWriteException("XML document had no version information.");
            }
            catch
            {
                throw new SpreadsheetReadWriteException("There was a problem opening the given file.");
            }
        }


        public override void Save(string filename)
        {
            internalChanged = false;

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "    ";
            try
            {
                using (XmlWriter xmlW = XmlWriter.Create(filename, settings))
                {
                    xmlW.WriteStartDocument();
                    xmlW.WriteStartElement("spreadsheet");
                    xmlW.WriteAttributeString("version", Version);
                    foreach (string name in GetNamesOfAllNonemptyCells())
                    {
                        xmlW.WriteStartElement("cell");
                        xmlW.WriteElementString("name", name);
                        if (typeof(Formula).IsInstanceOfType(cellDictionary[name].contents))
                            xmlW.WriteElementString("contents", "=" + cellDictionary[name].contents.ToString());
                        else
                            xmlW.WriteElementString("contents", cellDictionary[name].contents.ToString());

                        xmlW.WriteEndElement();
                    }
                    //end the spreadsheet element
                    xmlW.WriteEndElement();
                    xmlW.WriteEndDocument();
                }
            }
            catch { throw new SpreadsheetReadWriteException("There was a problem writing the file, check your file path and make sure it is valid."); }

        }

        public override object GetCellValue(string name)
        {
            if (name == null || !IsValid(name))
                throw new InvalidNameException();
            name = Normalize(name);
            if (cellDictionary.ContainsKey(name))
                return cellDictionary[name].value;
            else
                return "";
        }

        public override IList<string> SetContentsOfCell(string name, string content)
        {
            internalChanged = true;
            if (content == null)
                throw new ArgumentNullException();
            object contentObj;
            if (content != "" && content[0] == '=')
                contentObj = new Formula(content.Substring(1, content.Length - 1), Normalize, IsValid);
            else if (double.TryParse(content, out double d))
                contentObj = d;
            else
                contentObj = content;
            return SystemSetAnyCellContents(Normalize(name), contentObj);
        }

        /// <summary>
        /// A cell object that contains contents and value. 
        /// </summary>
        private class Cell
        {
            public object contents;
            public object value;

            /// <summary>
            /// Creates a cell with the contents given in the parameter. The contents
            /// should only be either a double, string or formula object. 
            /// </summary>
            /// <param name="newContents"></param>
            public Cell(object newContents, Func<String, double> lookup)
            {
                contents = newContents;
                if (typeof(Formula).IsInstanceOfType(newContents))
                    value = ((Formula)newContents).Evaluate(lookup);
                else
                    value = contents;
            }

            /// <summary>
            /// Recalculates the value of a cell using the loookup delegate provided by the user of the spreaadsheet. 
            /// </summary>
            public void Recalculate(Func<String, double> lookup)
            {
                if (typeof(Formula).IsInstanceOfType(contents))
                    value = ((Formula)contents).Evaluate(lookup);
                else
                    value = contents;
            }

        }


    }


}

