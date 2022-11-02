using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace FormulaEvaluator
{
    
   /// <summary>
   /// Evaluator class.
   /// 
   /// Author: Alan Bird
   /// 
   /// verson 1.0:Added main functionality and docummentation.
   /// </summary>
    public static class Evaluator
    {
        public delegate int Lookup(string v);

        /// <summary>
        ///Takes an expression then returns the scientific evalutation of said expression. Substituting in any variables found using the lookup funciton. 
        /// </summary>
        /// <param name="exp">the expression to be evaluated</param>
        /// <param name="variableEvaluator">function that will interpret a varriable
        ///  and thrrow ArgumentException if variable is not found. </param>
        /// <returns></returns>
        public static int Evaluate(string exp, Lookup variableEvaluator)
        {
            string[] substrings = Regex.Split(exp, "(\\()|(\\))|(-)|(\\+)|(\\*)|(/)");
            int iterator = 0;
            //trim the whitespace from each char. 
            while (iterator<substrings.Length) {
                substrings[iterator] = substrings[iterator].Trim();
                iterator++;
            }
            int num = 0;
            Stack<int> values = new Stack<int>();
            Stack<char> oper = new Stack<char>();

            

            foreach(string s in substrings)
            {
                bool usingVar = false;

                //every input that is greater than or equal to length 2 and cannot be parsed into an int will be a variable, so we set our
                //variable bool to true and set num equal to the value of the variable. If the variable is not valid, throws an ArgumentException
                if (s.Length >= 2&&!int.TryParse(s, out num))
                {
                    string pattern = "^[a-zA-Z]+[0-9]+$";
                    if (!Regex.IsMatch(s, pattern)) {
                        throw new System.ArgumentException("there is an invalid variable");
                    }
                    num=variableEvaluator(s);
                    usingVar = true;
                }

                // this will go off if s is a variable or it is an int.
                if (usingVar || int.TryParse(s, out num))
                {

                    if (TryPeek(oper).Equals('*') || TryPeek(oper).Equals('/'))
                    {
                        values.Push(num);
                        num = myMath(oper.Pop(), values);
                        values.Push(num);

                    }

                    else
                    {
                        values.Push(num);
                    }
                }

                // step 2 of the algorithm
                else if (s.Equals("+") || s.Equals("-"))
                {
                    if (TryPeek(oper).Equals('+') || TryPeek(oper).Equals('-'))
                    {

                        num = myMath(oper.Pop(), values);
                        values.Push(num);
                    }

                    oper.Push(s[0]);

                }

                //step 3 of the algorithm 
                else if (s.Equals("*") || s.Equals("/") || s.Equals("("))
                {
                    oper.Push(s[0]);
                }

                // step 5 of the algorithm 
                else if (s.Equals(")"))
                {
                    if (TryPeek(oper).Equals('-') || TryPeek(oper).Equals('+'))
                    {
                        num = myMath(oper.Pop(), values);
                        values.Push(num);
                    }

                    if (TryPeek(oper).Equals('('))
                    {
                        oper.Pop();
                    }
                    else
                    {
                        throw new System.ArgumentException("you must have a ( before a )");
                    }

                    if (TryPeek(oper).Equals('*') || TryPeek(oper).Equals('/'))
                    {
                        num = myMath(oper.Pop(), values);

                        values.Push(num);
                    }
                }
                else {
                    if (!s.Equals(""))
                    {
                        throw new System.ArgumentException("There is an unacceptable character");
                    }
                }
            }

            //this is the return if there are no remaining operations. Final step of the algorithm.
            if (oper.Count == 0)
            {

                if (values.Count == 1)
                {
                    return values.Pop();
                }

                else
                {
                    throw new System.ArgumentException("there are too many operands and not enough operators");
                }
            }

            //this is the return if there is one operation left in oper
            else {
                if (oper.Count > 1 || values.Count>2) {
                    throw new System.ArgumentException("The ratio of operators to operands is incorrect.");
                }
                return myMath(oper.Pop(), values);
            }
        }


        /// <summary>
        /// Preforms all of the nessicary math functions given two values and a operator char. If the given char is not one of the 
        /// four operators accepted throws an exceptioin.Also throws ArgumentException if it attemtps to divide by zero.
        /// </summary>
        /// <param name="oper"> The opperation to opperate with </param>
        /// <param name="values">A stack containing the two operands at the top. </param>
        /// <returns>The evaluation of the function.</returns>
        private static int myMath(char oper, Stack<int> values) {
            int val1;
            int val2;
            try
            {
                 val1 = values.Pop();
                 val2 = values.Pop();
            }
            catch {
                throw new System.ArgumentException("There are to many opperators in ratio to the number of legal operands");
            }
            if (oper.Equals('*'))
            {
                val1 = val1 * val2;
            }
            else if (oper.Equals('/'))
            {
                if (val1 == 0) {
                    throw new System.ArgumentException("you cannot divide by zero");
                }
                val1 = val2 / val1;
            }
            else if (oper.Equals('+'))
            {
                val1 = val1 + val2;
            }
            else if (oper.Equals('-'))
            {
                val1 = val2 - val1;
            }
            else {
                throw new System.ArgumentException("this application does not accept this character");
            }
            return val1;
        }

        /// <summary>
        /// attempts to pop a given Stack<char>.
        /// </summary>
        /// <param name="stack">The stack to be popped.</param>
        /// <returns>, if the stack is empty it will return 'N' so it is known that popping the stack did not work. 
        /// If the stack constains anything it returns the peeked object. </returns>
        private static char TryPeek(Stack<char> stack) {
            try
            {
                return stack.Peek();
            }
            catch 
            {
                return 'N';
            }

        }


    }

    

    
}
