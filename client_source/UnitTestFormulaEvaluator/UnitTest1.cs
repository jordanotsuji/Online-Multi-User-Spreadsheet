using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections;

namespace UnitTestFormulaEvaluator
{
    [TestClass]
    public class UnitTest1
    {
        private string arg;
        private int outp;
        private delegate int del(string v);

        /*
         * testing simple addition
         */
        [TestMethod]
        public void additionTest()
        {
            arg = "1+2+3";
            outp = 6;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);
        }

        /*
         * testing addition and whitespace
         */
        [TestMethod]
        public void whitespaceTest() {
            arg = "1 + 2 + 3 + 8";
            outp = 14;

            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);
            arg = "1     +  2 - 3 +       14";
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

            arg = "0";
            outp = 0;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);
        }

        /*
         *testing the multiplication and division
         */
        [TestMethod]
        public void multiDiviTest()
        {
            arg = "1*2*3";
            outp = 6;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);
            arg = "6*9*1*1*2";
            outp = 108;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

            arg = "10/5";
            outp = 2;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

            arg = "55/5/11";
            outp = 1;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);
        }

        /*
         * testing the parenthesis methods
         */
        [TestMethod]
        public void parenthTest()
        {
            arg = "(100 - 20) * 1 + 2 * 4 - 0";
            outp = 88;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

            arg = "12+3*(60+2)";
            outp = 198;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

            arg = "(90)+ (20)";
            outp = 110;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);
        }

        /*
         * testing the variable imput method
         */
        [TestMethod]
        public void varTest()
        {
            arg = "alanrules69+a1";
            outp = 425;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

            arg = "(a2+b4)*a2+20-5";
            outp = 1115;
            Assert.AreEqual(FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar), outp);

        }

        /*
         * testing that dividing by zero throws the correct exception.
         */
        [TestMethod]
        public void divZeroTest() {
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate("0/0", takeAVar));
            arg = "(7+8-3/2)/0";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            arg = "(7+8-3/2)/a4";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
        }

        /*
         * testing that more illegal arguments throw the correct excpetion.
         */
        [TestMethod]
        public void illegalArgTest(){
            arg = "4+a+b+8";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            arg = "^%$#%$*()";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            arg = "**+-4";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            //this one is tricky
            arg = "$%^^7";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));

        }

        /// <summary>
        /// too many operators and operands
        /// </summary>
        public void tooManyOpesTest() {
            arg = "++++";
                Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            arg = "2 2 +7";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            arg = "12+4**9";
            Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
            arg = "(0)+(900(";
                Assert.ThrowsException<System.ArgumentException>(()
                  => FormulaEvaluator.Evaluator.Evaluate(arg, takeAVar));
        }











        /*
         *method to create variables and pass into evaluate. 
         */
        public static int takeAVar(string s)
        {
            switch (s)
            {
                case "a1":
                    return 5;
                case "a2":
                    return 10;
                case "alanrules69":
                    return 420;
                case "b4":
                    return 100;
                case "a4":
                    return 0;
            }

            return 0;
        }
    }
}
