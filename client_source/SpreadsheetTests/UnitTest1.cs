using Microsoft.VisualBasic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using SS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Xml;

namespace SpreadsheetTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void NonValidCellNames1()
        {
            Spreadsheet s = new Spreadsheet();
            s.GetCellContents(null);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void NonValidCellNames2()
        {
            Spreadsheet s = new Spreadsheet();
            s.GetCellContents("420");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void NonValidCellNames3()
        {
            Spreadsheet s = new Spreadsheet();
            s.GetCellContents("&");
        }

        [TestMethod]
        public void SimpleEmptyCell()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual("", s.GetCellContents("A1"));
        }

        [TestMethod]
        public void MakingEmptyCells()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "");
            Assert.AreEqual(0, s.GetNamesOfAllNonemptyCells().Count());
        }

        [TestMethod]
        public void MakingEmptyCellsComplex()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "7");
            s.SetContentsOfCell("b1", "=a1+2");
            s.SetContentsOfCell("c1", "=b1+3");
            IList l = (IList)s.SetContentsOfCell("a1", "");
            Assert.AreEqual(3, l.Count);
            Assert.AreEqual("a1", l[0]);
            Assert.AreEqual("b1", l[1]);
            Assert.AreEqual("c1", l[2]);
        }

        [TestMethod]
        public void DependencyGraphComplexReplacementTest()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "7");
            s.SetContentsOfCell("b1", "=a1+2");
            s.SetContentsOfCell("c1", "=b1+3");
            s.SetContentsOfCell("d1", "=c1+2");
            s.SetContentsOfCell("e1", "=d1+3");
            //reset d1 to a constant
            s.SetContentsOfCell("d1", "6");
            //now set a1 to rely on e1 and see if a exception is thrown
            //and assert all of the things that rely on a1.
            Assert.AreEqual(3, s.SetContentsOfCell("a1", "=e1+3").Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void NullText()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("_valid", (string)null);
        }

        [TestMethod]
        public void SimpleDependencies()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual(1, s.SetContentsOfCell("a1", "1").Count);
            Assert.AreEqual("a2", s.SetContentsOfCell("a2", "2")[0]);
            s.SetContentsOfCell("a3", "=a1 + a2 + a4");
            Assert.AreEqual(2, s.SetContentsOfCell("a4", "3.9").Count);
        }

        [TestMethod]
        public void ComplexeDependencies()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual(1, s.SetContentsOfCell("a1", "1").Count);
            Assert.AreEqual("a2", s.SetContentsOfCell("a2", "2")[0]);
            s.SetContentsOfCell("a3", "=a1 + a2 + a4");
            Assert.AreEqual(2, s.SetContentsOfCell("a4", "3.9").Count);
            s.SetContentsOfCell("a5", "=a3");
            s.SetContentsOfCell("a6", "=a5");
            Assert.AreEqual(4, s.SetContentsOfCell("a1", "0").Count);
        }

        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void CircularExcption()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "1");
            s.SetContentsOfCell("a2", "=a1");
            s.SetContentsOfCell("a3", "=a2");
            s.SetContentsOfCell("a1", "=a3");

        }

        [TestMethod]
        public void GetContents()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "4");
            s.SetContentsOfCell("a2", "=a1");
            s.SetContentsOfCell("a3", "Alan is gonna ace his classes");

            Assert.AreEqual(4.0, s.GetCellContents("a1"));
            Assert.AreEqual("Alan is gonna ace his classes", s.GetCellContents("a3"));

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void InvalidCellName()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("AlanBird&Friends", "420");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void InvalidCellNameForString()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("AlanBird&Friends", "string");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void InvalidCellNameString2()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell(null, "annother string");
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void InvalidCellNameString3()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("_420", "679.89");
            s.SetContentsOfCell("_420", "yo mamma");
            s.SetContentsOfCell("Alan and friends", "=1");
        }

        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void CircularRefrence3()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A420", "=A420+3");
        }

        [TestMethod]
        public void EmptySpreadsheetTest()
        {
            Spreadsheet s = new Spreadsheet();
            IList l = s.GetNamesOfAllNonemptyCells().ToList();
            Assert.AreEqual(0, l.Count);
        }

        [TestMethod]
        public void ReplaceCells()
        {
            Spreadsheet s = new Spreadsheet();
            for (int n = 0; n <= 100; n++)
            {
                s.SetContentsOfCell("A420", n.ToString());
            }

            Assert.AreEqual(100.0, s.GetCellContents("A420"));
            Assert.AreEqual(1, s.GetNamesOfAllNonemptyCells().ToList().Count);
        }

        [TestMethod]
        public void GetNamesOfAllNonEmptyCells()
        {

            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "4");
            s.SetContentsOfCell("a2", "=a1");
            s.SetContentsOfCell("a3", "Alan is gonna ace his classes");

            IList l = s.GetNamesOfAllNonemptyCells().ToList();
            Assert.IsTrue(l.Contains("a1"));
            Assert.IsTrue(l.Contains("a2"));
            Assert.IsTrue(l.Contains("a3"));
            Assert.AreEqual(3, l.Count);

        }

        [TestMethod]
        public void StressTest()
        {
            //add a bunch of random cells with numbers as contence. 
            string s = "A1";
            int n = 0;
            Spreadsheet t = new Spreadsheet();

            for (; n <= 1000; n += 2)
            {
                t.SetContentsOfCell(s, (n + 7.39842).ToString());

                s += n;
            }

            n = 1;
            Random rnd = new Random();
            s = "A1";
            for (; n < 1000; n += 2)
            {
                t.SetContentsOfCell(s, "=A" + rnd.Next(0, 1000) + "+420.69");

                s += n;
            }

            //Assert that everything was added right. 
            Assert.AreEqual(1000, t.GetNamesOfAllNonemptyCells().ToList().Count);
        }

        // Begin tests spesifically for PS5 Updates

        [TestMethod]
        public void AllThreeConstructors()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "1.0");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "2");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            AbstractSpreadsheet sheet1 = new Spreadsheet();
            AbstractSpreadsheet sheet2 = new Spreadsheet(s => true, s => s, "1.0");
            AbstractSpreadsheet sheet3 = new Spreadsheet("file.xml", s => true, s => s, "1.0");
        }



        [TestMethod]
        public void ValueSimple()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "2");
            s.SetContentsOfCell("B1", "A1");
            s.SetContentsOfCell("C1", "=B1");
            s.SetContentsOfCell("D1", "=1+ A1");
            Assert.AreEqual(2.0, s.GetCellValue("A1"));
            Assert.AreEqual("A1", s.GetCellValue("B1"));
            Assert.IsTrue(typeof(FormulaError).IsInstanceOfType(s.GetCellValue("C1")));
            Assert.AreEqual(3.0, s.GetCellValue("D1"));
        }

        [TestMethod]
        public void ValueComplex()
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("A1", "2");
            s.SetContentsOfCell("B1", "A1");
            s.SetContentsOfCell("C1", "=B1");
            s.SetContentsOfCell("D1", "=1+ A1");
            s.SetContentsOfCell("B1", "=D1*2");
            s.SetContentsOfCell("A1", "=0-1");
            Assert.AreEqual(0.0, s.GetCellValue("B1"));
        }

        [TestMethod]
        public void EmptyCellValue()
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual("", s.GetCellContents("a1"));
            s.SetContentsOfCell("a1", "");
            Assert.AreEqual("", s.GetCellContents("a1"));
        }

        [TestMethod]
        [ExpectedException(typeof(CircularException))]
        public void TestUndoCircular()
        {
            Spreadsheet s = new Spreadsheet();
            try
            {
                s.SetContentsOfCell("A1", "=A2+A3");
                s.SetContentsOfCell("A2", "15");
                s.SetContentsOfCell("A3", "30");
                s.SetContentsOfCell("A2", "=A3*A1");
            }
            catch (CircularException e)
            {
                Assert.AreEqual(15, (double)s.GetCellContents("A2"), 1e-9);
                throw e;
            }
        }

        [TestMethod]
        public void XMLTestSimple()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "default");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "2");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }

            Spreadsheet s = new Spreadsheet();
            s.GetSavedVersion("file.xml");

            Assert.AreEqual(1, s.GetNamesOfAllNonemptyCells().Count());
            Assert.AreEqual("a1", s.GetNamesOfAllNonemptyCells().First());
            Assert.AreEqual(2.0, s.GetCellContents("a1"));


        }

        [TestMethod]
        public void XMLTestReplaceCells()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "default");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "2");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }

            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "hello world");
            s.SetContentsOfCell("a2", "=20+3");
            s.SetContentsOfCell("b1", "this is not a drill");
            s.GetSavedVersion("file.xml");

            Assert.AreEqual(1, s.GetNamesOfAllNonemptyCells().Count());
            Assert.AreEqual("a1", s.GetNamesOfAllNonemptyCells().First());
            Assert.AreEqual(2.0, s.GetCellContents("a1"));


        }

        [TestMethod]
        public void XMLTestFileConstructor()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "default");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "2");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }

            Spreadsheet s = new Spreadsheet("file.xml", s => true, s => s, "default");

            Assert.AreEqual(1, s.GetNamesOfAllNonemptyCells().Count());
            Assert.AreEqual("a1", s.GetNamesOfAllNonemptyCells().First());
            Assert.AreEqual(2.0, s.GetCellContents("a1"));


        }

        [TestMethod]
        public void ChangedFieldTest()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "default");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "2");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }

            Spreadsheet s = new Spreadsheet("file.xml", s => true, s => s, "default");

            Assert.IsFalse(s.Changed);
            s.SetContentsOfCell("a2", "=a1");
            Assert.IsTrue(s.Changed);
            s.Save("file.xml");
            Assert.IsFalse(s.Changed);
            s.SetContentsOfCell("a3", "42");
            s.GetSavedVersion("file.xml");
            Assert.IsFalse(s.Changed);

        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void SpesificValidator()
        {
            Spreadsheet s = new Spreadsheet(MyValidator, s => s, "default");
            s.SetContentsOfCell("a1", "=a2");

        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void SpesificValidatorComplex()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "default");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "2");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            Spreadsheet s = new Spreadsheet("file.xml", MyValidator, s => s, "default");
            s.SetContentsOfCell("a1", "=a2");

        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void ReadWriteExcpetionCircular()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "1.0");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "=a1");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            Spreadsheet s = new Spreadsheet("file.xml", s => true, s => s, "1.0");
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void ReadWriteExcpetionNoCellsName()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "1.0");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "=a1");
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            Spreadsheet s = new Spreadsheet("file.xml", s => true, s => s, "1.0");
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void ReadWriteExcpetionNoCellsContents()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "1.0");
                xmlW.WriteElementString("contents", "=a1");
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            Spreadsheet s = new Spreadsheet("file.xml", s => true, s => s, "1.0");
        }

        [TestMethod]
        public void MyNormalizerTest()
        {
            Spreadsheet s = new Spreadsheet(s => true, MyNormalizer, "BlazeIt");
            s.SetContentsOfCell("A1", "=A2");
            Assert.AreEqual("a1", s.GetNamesOfAllNonemptyCells().First());
            Assert.AreEqual(new Formula("a2"), s.GetCellContents("a1"));
        }

        [TestMethod]
        public void CircularEdgeCase() 
        {
            Spreadsheet s = new Spreadsheet();
            s.SetContentsOfCell("a1", "1");
            s.SetContentsOfCell("a2", "=a1");
            s.SetContentsOfCell("a3", "=a2");
            s.SetContentsOfCell("a4", "=a3");
            try { s.SetContentsOfCell("a2", "=a4"); }
            catch 
            {
                s.SetContentsOfCell("a1", "2");
                Assert.AreEqual(2.0, s.GetCellValue("a2"));
            }


        }

        [TestMethod]
        public void ValueEdgeCases() 
        {
            Spreadsheet s = new Spreadsheet();
            Assert.AreEqual("", s.GetCellValue("a1"));
        
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void ValueEdgeCasesException()
        {
            Spreadsheet s = new Spreadsheet(MyValidator,MyNormalizer,"default");
            Assert.AreEqual("", s.GetCellValue("a1"));

        }

        [TestMethod]
        [ExpectedException(typeof(InvalidNameException))]
        public void ValueEdgeCasesNull()
        {
            Spreadsheet s = new Spreadsheet(MyValidator, MyNormalizer, "default");
            Assert.AreEqual("", s.GetCellValue(null));
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void BadXmlTestVersion() 
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "1.0");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "=1");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            Spreadsheet s = new Spreadsheet("file.xml", s => true, s => s, "2.0");
        }

        [TestMethod]
        [ExpectedException(typeof(SpreadsheetReadWriteException))]
        public void BadXmlTestFileName()
        {
            using (XmlWriter xmlW = XmlWriter.Create("file.xml"))
            {
                xmlW.WriteStartDocument();
                xmlW.WriteStartElement("spreadsheet");
                xmlW.WriteAttributeString("version", "1.0");
                xmlW.WriteStartElement("cell");
                xmlW.WriteElementString("name", "a1");
                xmlW.WriteElementString("contents", "=1");
                xmlW.WriteEndElement();
                xmlW.WriteEndElement();
                xmlW.WriteEndDocument();
            }
            Spreadsheet s = new Spreadsheet("file..xml", s => true, s => s, "1.0");        }


        private string MyNormalizer(string s)
        {
            return s.ToLower();
        }

        private bool MyValidator(string s)
        {
            Regex pattern = new Regex("^[A-Z]+");

            return pattern.IsMatch(s);
        }

    }
}
