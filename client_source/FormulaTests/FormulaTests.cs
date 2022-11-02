using Microsoft.VisualStudio.TestTools.UnitTesting;
using SpreadsheetUtilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography.X509Certificates;

// FormulaTests written by Alan Bird. 
namespace FormulaTests
{
    [TestClass]
    public class FormulaTests
    {
        /// <summary>
        /// This test and the 5 methods following it all simply put equations into the constructor and 
        /// check and make sure that they recognize the input as a valid function and throw no errors. 
        /// </summary>
        [TestMethod]
        public void TestConstructorNoErrors()
        {
            new Formula("alanrox420 + 69");
        }

        [TestMethod]
        public void TestConstructorNoErrors2()
        {
            new Formula("_yoMomma + (420+96)");
        }

        [TestMethod]
        public void TestConstructorNoErrors3()
        {
            new Formula("_CS3500 + (420+96)*100+(96)");
        }

        [TestMethod]
        public void TestConstructorNoErrors4()
        {
            new Formula("_ + (420+96*(0))/0+24");
        }

        [TestMethod]
        public void TestConstructorNoErrors5()
        {
            new Formula("A + (420+96) -6+7-8+(slob)-69/0+(100+(200+(3)))*2");
        }


        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorNoTokens()
        {
            new Formula("");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorOperators()
        {
            new Formula("1+2+3+4+6++9+8+6+3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorOpenParen1()
        {
            new Formula("1+2+3+4+6(9+8+6+3)");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorOpenParen2()
        {
            new Formula("1+(2+(3+4+6+(9+8+6+3)");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorCloseParen1()
        {
            new Formula("1+2+3+)4+69+8+6+3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorCloseParen2()
        {
            new Formula("1+2+3+4+6+(9+8)+6)+3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorCloseParen3()
        {
            new Formula("1+2+(3+4+(6+92+8+6+)3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorValidator()
        {
            new Formula("1+2+3+4+6+wassup+8+6+3", normalizer, validator);
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidToken()
        {
            new Formula("1+2+3+4+6+92a+9+8+6+3");
        }


        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidToken2()
        {
            new Formula("1+2+3+4+6+92+9 9+8+6+3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidToken3()
        {
            new Formula("1+2+3+4+6+9+_3&2+8+6+3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidToken4()
        {
            new Formula("1+2+3+4+6+92+& 9+8+6+3");
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidNormalizedToken()
        {
            new Formula("1+2+3+4+6+92+code+8+6+3", normalizer, validator);
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorEndWithOperator()
        {
            new Formula("1+2+3+4+6+92+8+6+3/");
        }

        [TestMethod]
        public void TestGetVariables()
        {
            Formula f = new Formula("a+a+a+a+a");
            int n = 0;
            foreach (string s in f.GetVariables())
            {
                Assert.AreEqual("a", s);
                n++;
            }
            Assert.AreEqual(1, n);
        }

        [TestMethod]
        public void TestGetVariables2()
        {
            Formula f = new Formula("a+_+a+_27a+a");
            int n = 0;
            foreach (string s in f.GetVariables())
            {
                switch (n)
                {
                    case 0:
                        Assert.AreEqual("a", s);
                        n++;
                        break;
                    case 1:
                        Assert.AreEqual("_", s);
                        n++;
                        break;
                    case 2:
                        Assert.AreEqual("_27a", s);
                        n++;
                        break;

                }
            }
            Assert.AreEqual(3, n);

        }

        [TestMethod]
        public void TestGetVariablesWithNormalizer()
        {
            Formula f = new Formula("a+A+a+A+a", normalizer, validator);
            int n = 0;
            foreach (string s in f.GetVariables())
            {
                Assert.AreEqual("a", s);
                n++;
            }
            Assert.AreEqual(1, n);
        }


        [TestMethod]
        public void TestToString1()
        {
            Formula f = new Formula("X+2+ _hello-    67");
            Assert.AreEqual("X+2+_hello-67", f.ToString());
            Assert.AreEqual(f, new Formula(f.ToString()));
        }

        [TestMethod]
        public void TestToStringWithNormalizer()
        {
            Formula f = new Formula("X+ 2+ _hELlo-    67", normalizer, validator);
            Assert.AreEqual("x+2+_hello-67", f.ToString());
        }

        [TestMethod]
        public void TestEquals()
        {
            Formula f = new Formula("1.00+2.33+3.4+4.5");
            Formula g = new Formula("1.00+ 2.3300 +3.4+ 4.5");
            Formula h = new Formula("1.00 + 2.303 + 3.4 + 4.5");
            Assert.IsTrue(f.Equals(g));
            Assert.IsFalse(f.Equals(7));
            Assert.IsFalse(f.Equals(h));
        }

        [TestMethod]
        public void TestEqualsOpperator()
        {
            Formula f = new Formula("1.00+2.33+3.4+4.5");
            Formula g = new Formula("1.00+ 2.3300 +3.4+ 4.5");
            Formula h = new Formula("1.00 + 2.303 + 3.4 + 4.5");
            Assert.IsTrue(f == g);
            Assert.IsFalse(f == h);
        }

        [TestMethod]
        public void TestNotEqualsOpperator()
        {
            Formula f = new Formula("1.00+2.33+3.4+4.5");
            Formula g = new Formula("1.00+ 2.3300 +3.4+ 4.5");
            Formula h = new Formula("1.00 + 2.303 + 3.4 + 4.5");
            Assert.IsFalse(f != g);
            Assert.IsTrue(f != h);
        }

        [TestMethod]
        public void TestHashCode()
        {
            Formula f = new Formula("1.00+2.33+3.4+4.5");
            Formula g = new Formula("1.00+ 2.3300 +3.4+ 4.5");
            Formula h = new Formula("1.00 + 2.303 + 3.4 + 4.5");
            Assert.IsTrue(f.GetHashCode() == g.GetHashCode());
            Assert.IsFalse(f.GetHashCode() == h.GetHashCode());
        }

        [TestMethod]
        public void TestEvaluateAddition()
        {
            Formula f = new Formula("1+2+3");
            Assert.AreEqual(6.0, (double)f.Evaluate(lookerUpper));
        }

        [TestMethod]
        public void TestEvaluateSubtraction()
        {
            Formula f = new Formula("1-2.5-3");
            Assert.AreEqual(-4.5, (double)f.Evaluate(lookerUpper));
        }

        [TestMethod]
        public void TestEvaluateMultiplicatoin()
        {
            Formula f = new Formula("1*2.5-3+.5+(37*0)");
            Assert.AreEqual(0, (double)f.Evaluate(lookerUpper));
        }

        [TestMethod]
        public void TestEvaluateDivision()
        {
            Formula f = new Formula("2*4*0+100/50*.5");
            Assert.AreEqual(1, (double)f.Evaluate(lookerUpper));
        }

        [TestMethod]
        public void TestEvaluateParen()
        {
            Formula f = new Formula("(22)+(33)-(55)+((500)/(500))");
            Assert.AreEqual(1, (double)f.Evaluate(lookerUpper));
        }

        [TestMethod]
        public void TestEvaluateUsingLookup()
        {
            Formula f = new Formula("(bird-420)*(100/100)*(469*8/(32-6)/22)", normalizer, validator);
            Assert.AreEqual(0, (double)f.Evaluate(lookerUpper));
        }

        [TestMethod]
        public void TestDivideByZero1()
        {
            Formula f = new Formula("0/0");
            Assert.IsTrue(typeof(FormulaError).IsInstanceOfType(f.Evaluate(lookerUpper)));
        }

        [TestMethod]
        public void TestDivideByZero2()
        {
            Formula f = new Formula("1.49+2.36+3.77-(1.9999/(0+0))");
            Assert.IsTrue(typeof(FormulaError).IsInstanceOfType(f.Evaluate(lookerUpper)));
        }

        [TestMethod]
        public void TestDivideByZero3()
        {
            Formula f = new Formula("(1.6666-6999/9.577+420)/0");
            Assert.IsTrue(typeof(FormulaError).IsInstanceOfType(f.Evaluate(lookerUpper)));
        }

        [TestMethod]
        public void TestIllegalVariable()
        {
            Formula f = new Formula("(1.6666-6999/9.577+420-null)");
            Assert.IsTrue(typeof(FormulaError).IsInstanceOfType(f.Evaluate(lookerUpper)));
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidTokenBefore()
        {
            new Formula("1.1+2.2+3.3+5+4+00NotValid", normalizer, validator);
        }

        [TestMethod]
        [ExpectedException(typeof(FormulaFormatException))]
        public void TestConstructorErrorInvalidTokenAfter()
        {
            new Formula("1.1+2.2+3.3+5+4+validBefore", normalizer, validator);
        }

        [TestMethod]
        public void TestScientificNotation() 
        {
            Formula f = new Formula("1.7E+3");
            Formula g = new Formula("1700");
            Assert.IsTrue(f.Equals(g));
            Assert.IsTrue(f.GetHashCode() == g.GetHashCode());
        
        }






        /// <summary>
        /// Simple validator method to pass into Formula.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool validator(string s)
        {
            if (s == "yo_mamma")
                return false;
            else return true;
        }

        /// <summary>
        /// simple normalizer method to pass into Formula.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private string normalizer(string s)
        {
            if (s == "validBefore")
                return "00NotValid";
            if (s == "00NotValid")
                return "validAfter";
            if (s == "bird")
                return "alanrox69";
            if (s == "wassup")
            {
                return "yo_mamma";
            }
            else if (s == "code")
            {
                return "_break *";
            }
            else
            {
                return s.ToLower();
            }
        }

        private double lookerUpper(string s)
        {
            if (s == "null")
                throw new System.ArgumentException("null is not a valid token");
            if (s == "alanrox69")
                return 420;
            return 0;
        }
    }
}
