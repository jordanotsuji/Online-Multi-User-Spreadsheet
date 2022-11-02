using System;

namespace FormulaExe
{
    class Program
    {

        public delegate int del(string v);

        static void Main(string[] args)
        {
            //test 1: string expression = "10/yourMom";
            //string expression = "1+2-3";

            del deliBoi = takeAVar;

            Console.WriteLine(FormulaEvaluator.Evaluator.Evaluate("()", takeAVar));
           
        }



        public static  int takeAVar(string s) {
            if (s.Equals("yourMom"))
            {
                return 2;
            }
            else {
                return 0;
            }
        
        }
    }
}
