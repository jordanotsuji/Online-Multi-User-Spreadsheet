// Skeleton written by Joe Zachary for CS 3500, September 2013
// Read the entire skeleton carefully and completely before you
// do anything else!

// Version 1.1 (9/22/13 11:45 a.m.)

// Change log:
//  (Version 1.1) Repaired mistake in GetTokens
//  (Version 1.1) Changed specification of second constructor to
//                clarify description of how validation works

// (Daniel Kopta) 
// Version 1.2 (9/10/17) 

// Change log:
//  (Version 1.2) Changed the definition of equality with regards
//                to numeric tokens
//  (Version 2.0) Begun implementation by Alan Bird. Created check for
//                validity method and basic constructor implementation. 
//  (Version 2.1) Finsihed testing and implementing every method up to evaluate. 
//  (Version 3.0) First finished version. Evaluate and all other methods appear
//                to work. Testing covers most code, still need to add tests. 
//  (Version 3.1) Updated to pass things by reference to MyMath and changed
//                how to catch divide by zero errors and tryPeek methods.
//  (Version 3.2) Final revisions to syntax and error checking. Added a few test methods. 


using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SpreadsheetUtilities
{
    /// <summary>
    /// Represents formulas written in standard infix notation using standard precedence
    /// rules.  The allowed symbols are non-negative numbers written using double-precision 
    /// floating-point syntax (without unary preceeding '-' or '+'); 
    /// variables that consist of a letter or underscore followed by 
    /// zero or more letters, underscores, or digits; parentheses; and the four operator 
    /// symbols +, -, *, and /.  
    /// 
    /// Spaces are significant only insofar that they delimit tokens.  For example, "xy" is
    /// a single variable, "x y" consists of two variables "x" and y; "x23" is a single variable; 
    /// and "x 23" consists of a variable "x" and a number "23".
    /// 
    /// Associated with every formula are two delegates:  a normalizer and a validator.  The
    /// normalizer is used to convert variables into a canonical form, and the validator is used
    /// to add extra restrictions on the validity of a variable (beyond the standard requirement 
    /// that it consist of a letter or underscore followed by zero or more letters, underscores,
    /// or digits.)  Their use is described in detail in the constructor and method comments.
    /// </summary>
    public class Formula
    {
        private LinkedList<string> tokens;


        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically invalid,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer is the identity function, and the associated validator
        /// maps every string to true.  
        /// </summary>
        public Formula(String formula) :
            this(formula, s => s, s => true)
        {
        }


        /// <summary>
        /// Creates a Formula from a string that consists of an infix expression written as
        /// described in the class comment.  If the expression is syntactically incorrect,
        /// throws a FormulaFormatException with an explanatory Message.
        /// 
        /// The associated normalizer and validator are the second and third parameters,
        /// respectively.  
        /// 
        /// If the formula contains a variable v such that normalize(v) is not a legal variable, 
        /// throws a FormulaFormatException with an explanatory message. 
        /// 
        /// If the formula contains a variable v such that isValid(normalize(v)) is false,
        /// throws a FormulaFormatException with an explanatory message.
        /// 
        /// Suppose that N is a method that converts all the letters in a string to upper case, and
        /// that V is a method that returns true only if a string consists of one letter followed
        /// by one digit.  Then:
        /// 
        /// new Formula("x2+y3", N, V) should succeed
        /// new Formula("x+y3", N, V) should throw an exception, since V(N("x")) is false
        /// new Formula("2x+y3", N, V) should throw an exception, since "2x+y3" is syntactically incorrect.
        /// </summary>
        public Formula(String formula, Func<string, string> normalize, Func<string, bool> isValid)
        {
            CheckForValidity(formula, normalize, isValid);
            tokens = new LinkedList<string>();
            foreach (string s in GetTokens(formula))
            {
                tokens.AddLast(normalize(s));
            }
        }

        /// <summary>
        /// Evaluates this Formula, using the lookup delegate to determine the values of
        /// variables.  When a variable symbol v needs to be determined, it should be looked up
        /// via lookup(normalize(v)). (Here, normalize is the normalizer that was passed to 
        /// the constructor.)
        /// 
        /// For example, if L("x") is 2, L("X") is 4, and N is a method that converts all the letters 
        /// in a string to upper case:
        /// 
        /// new Formula("x+7", N, s => true).Evaluate(L) is 11
        /// new Formula("x+7").Evaluate(L) is 9
        /// 
        /// Given a variable symbol as its parameter, lookup returns the variable's value 
        /// (if it has one) or throws an ArgumentException (otherwise).
        /// 
        /// If no undefined variables or divisions by zero are encountered when evaluating 
        /// this Formula, the value is returned.  Otherwise, a FormulaError is returned.  
        /// The Reason property of the FormulaError should have a meaningful explanation.
        ///
        /// This method should never throw an exception.
        /// </summary>
        public object Evaluate(Func<string, double> lookup)
        {
            double num = 0;
            Stack<double> values = new Stack<double>();
            Stack<char> oper = new Stack<char>();

            foreach (string s in tokens)
            {
                bool usingVar = false;

                //every input that is greater than or equal to length 2 and cannot be parsed into an int will be a variable, so we set our
                //variable bool to true and set num equal to the value of the variable. If the variable is not valid, throws an ArgumentException
                if (s.Length >= 2 && !double.TryParse(s, out num))
                {
                    try
                    {
                        num = lookup(s);
                    }
                    catch (System.ArgumentException)
                    {
                        return new FormulaError("The variable " + s + "is underdefined with the given lookup method");
                    }
                    usingVar = true;
                }

                // this will go off if s is a variable or it is an int.
                if (usingVar || double.TryParse(s, out num))
                {
                    
                    if (TryPeek(oper).Equals('*') || TryPeek(oper).Equals('/'))
                    {
                        values.Push(num);
                        if(!MyMath(oper.Pop(),values, out num))
                            return new FormulaError("You cannot divide by zero.");
                        values.Push(num);
                    }
                    else
                        values.Push(num);
                }

                // step 2 of the algorithm
                else if (s.Equals("+") || s.Equals("-"))
                {
                    if (TryPeek(oper).Equals('+') || TryPeek(oper).Equals('-'))
                    {
                        MyMath(oper.Pop(), values, out num);
                        values.Push(num);
                    }
                    oper.Push(s[0]);
                }

                //step 3 of the algorithm 
                else if (s.Equals("*") || s.Equals("/") || s.Equals("("))
                    oper.Push(s[0]);

                // step 5 of the algorithm 
                else if (s.Equals(")"))
                {
                    if (TryPeek(oper).Equals('-') || TryPeek(oper).Equals('+'))
                    {
                        MyMath(oper.Pop(), values, out num);
                        values.Push(num);
                    }

                    if (TryPeek(oper).Equals('('))
                        oper.Pop();

                    if (TryPeek(oper).Equals('*') || TryPeek(oper).Equals('/'))
                    {
                        if (!MyMath(oper.Pop(), values, out num))
                            return new FormulaError("You cannot divide by zero.");
                        values.Push(num);
                    }
                }

            }

            //this is the return if there are no remaining operations. Final step of the algorithm.
            if (oper.Count == 0)
            {
                return values.Pop();
            }
            //this is the return if there is one operation left in oper
            else
            {
                MyMath(oper.Pop(), values, out num);
                return num;
            }

        }

        /// <summary>
        /// Enumerates the normalized versions of all of the variables that occur in this 
        /// formula.  No normalization may appear more than once in the enumeration, even 
        /// if it appears more than once in this Formula.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x+y*z", N, s => true).GetVariables() should enumerate "X", "Y", and "Z"
        /// new Formula("x+X*z", N, s => true).GetVariables() should enumerate "X" and "Z".
        /// new Formula("x+X*z").GetVariables() should enumerate "x", "X", and "z".
        /// </summary>
        public IEnumerable<String> GetVariables()
        {
            HashSet<string> variables = new HashSet<string>();
            foreach (string token in tokens)
            {
                if (token[0] == '_' || char.IsLetter(token[0]))
                    variables.Add(token);
            }
            return variables;
        }

        /// <summary>
        /// Returns a string containing no spaces which, if passed to the Formula
        /// constructor, will produce a Formula f such that this.Equals(f).  All of the
        /// variables in the string should be normalized.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        /// 
        /// new Formula("x + y", N, s => true).ToString() should return "X+Y"
        /// new Formula("x + Y").ToString() should return "x+Y"
        /// </summary>
        public override string ToString()
        {
            string toReturn = "";
            foreach (string s in tokens)
            {
                toReturn += getEquivilantString(s);

            }
            return toReturn;
        }

        /// <summary>
        /// If obj is null or obj is not a Formula, returns false.  Otherwise, reports
        /// whether or not this Formula and obj are equal.
        /// 
        /// Two Formulae are considered equal if they consist of the same tokens in the
        /// same order.  To determine token equality, all tokens are compared as strings 
        /// except for numeric tokens and variable tokens.
        /// Numeric tokens are considered equal if they are equal after being "normalized" 
        /// by C#'s standard conversion from string to double, then back to string. This 
        /// eliminates any inconsistencies due to limited floating point precision.
        /// Variable tokens are considered equal if their normalized forms are equal, as 
        /// defined by the provided normalizer.
        /// 
        /// For example, if N is a method that converts all the letters in a string to upper case:
        ///  
        /// new Formula("x1+y2", N, s => true).Equals(new Formula("X1  +  Y2")) is true
        /// new Formula("x1+y2").Equals(new Formula("X1+Y2")) is false
        /// new Formula("x1+y2").Equals(new Formula("y2+x1")) is false
        /// new Formula("2.0 + x7").Equals(new Formula("2.000 + x7")) is true
        /// </summary>
        public override bool Equals(object obj)
        {
            if (!typeof(Formula).IsInstanceOfType(obj))
                return false;
            Formula f = (Formula)obj;
            IEnumerator newTokens = GetTokens(f.ToString()).GetEnumerator();
            newTokens.MoveNext();
            foreach (string s in tokens)
            {
                if (getEquivilantString((string)newTokens.Current) != getEquivilantString(s))
                    return false;
                newTokens.MoveNext();
            }
            return !newTokens.MoveNext();

        }

        /// <summary>
        /// Takes in any string, if that string can be parsed to a double, it will return an equivilent
        /// version of that double. If stringToken is not a double then it will just return stringToken.
        /// </summary>
        /// <param name="stringToken"></param>
        /// <returns></returns>
        private string getEquivilantString(string stringToken)
        {
            if (double.TryParse(stringToken, out double x))
            {
                return x.ToString();
            }
            else
                return stringToken;
        }

        /// <summary>
        /// Reports whether f1 == f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return true.  If one is
        /// null and one is not, this method should return false.
        /// </summary>
        public static bool operator ==(Formula f1, Formula f2)
        {
            return f1.Equals(f2);
        }

        /// <summary>
        /// Reports whether f1 != f2, using the notion of equality from the Equals method.
        /// Note that if both f1 and f2 are null, this method should return false.  If one is
        /// null and one is not, this method should return true.
        /// </summary>
        public static bool operator !=(Formula f1, Formula f2)
        {
            return !f1.Equals(f2);
        }

        /// <summary>
        /// Returns a hash code for this Formula.  If f1.Equals(f2), then it must be the
        /// case that f1.GetHashCode() == f2.GetHashCode().  Ideally, the probability that two 
        /// randomly-generated unequal Formulae have the same hash code should be extremely small.
        /// </summary>
        public override int GetHashCode()
        {
            return this.ToString().GetHashCode();
        }

        /// <summary>
        /// Checks to make sure that the given string is a valid formula. Both constructors will
        /// run through this method. There are numerous ways the mthod can throw exceptions, and 
        /// if it does not throw an exception the input formula must be correct. 
        /// </summary>
        /// <param name="formula">The string to determine if correct.</param>
        /// <param name="normalize">The normalize function described in the constructor.</param>
        /// <param name="isValid">The validator described in the constructor.</param>
        private void CheckForValidity(string formula, Func<string, string> normalize, Func<string, bool> isValid)
        {

            if (formula.Length == 0)
                throw new FormulaFormatException("There must be at least one value in the formula");

            bool mustBeOperator = false;
            int openParenCount = 0;
            int closeParenCount = 0;

            foreach (string s in GetTokens(formula))
            {
                if (s == "+" || s == "-" || s == "*" || s == "/")
                {
                    if (!mustBeOperator)
                        throw new FormulaFormatException("There is at least one too many operators," +
                            " make sure you only have an operator after something that can be operated on");
                    mustBeOperator = false;
                }
                else if (s == "(")
                {
                    if (mustBeOperator)
                        throw new FormulaFormatException("There is a ( symbol after a variable or number," +
                            " you must add an operator");
                    openParenCount++;
                }
                else if (s == ")")
                {
                    if (!mustBeOperator)
                        throw new FormulaFormatException("There is a ) symbol following a opperator, you must" +
                            " add a value before the )");
                    closeParenCount++;
                    if (closeParenCount > openParenCount)
                        throw new FormulaFormatException("There are too many ) symbols, add more ( symbols");
                }
                else if (double.TryParse(s, out double x))
                {
                    if (mustBeOperator)
                        throw new FormulaFormatException("There are two values not sepperated by a operator," +
                            " try adding annother opperator");
                    mustBeOperator = true;
                }
                else
                {
                    if (mustBeOperator)
                        throw new FormulaFormatException("There are two values not sepperated by a operator, try adding annother opperator");
                    if (!isValid(normalize(s))||!isValid(s))
                        throw new FormulaFormatException("The validator did not identify a token as valid, try" +
                            " adjusting your variables");
                    if (!myIsValid(normalize(s)))
                        throw new FormulaFormatException("At least one token is not a valid token, make sure after " +
                           "normalization each token is of the form: a letter or underscore followed by zero or more letters, underscores, or digits");
                    mustBeOperator = true;
                }

            }
            if (!mustBeOperator)
                throw new FormulaFormatException("The final token of the expression cannot be an opperator, try adding annother value");
            if (openParenCount != closeParenCount)
                throw new FormulaFormatException("There are too many ( symbols, try adding more ) symbols");
        }


        /// <summary>
        /// Determines if a single given string is a valid variable. IE. it is a letter or underscore followed by 
        /// zero or more letters, underscores, or digits.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        private bool myIsValid(string s)
        {
            if (s[0] == '_' || Char.IsLetter(s[0]))
            {
                foreach (char c in s)
                {
                    if (c != '_' && !char.IsLetterOrDigit(c))
                    {
                        return false;
                    }
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Given an expression, enumerates the tokens that compose it.  Tokens are left paren;
        /// right paren; one of the four operator symbols; a string consisting of a letter or underscore
        /// followed by zero or more letters, digits, or underscores; a double literal; and anything that doesn't
        /// match one of those patterns.  There are no empty tokens, and no token contains white space.
        /// </summary>
        private static IEnumerable<string> GetTokens(String formula)
        {
            // Patterns for individual tokens
            String lpPattern = @"\(";
            String rpPattern = @"\)";
            String opPattern = @"[\+\-*/]";
            String varPattern = @"[a-zA-Z_](?: [a-zA-Z_]|\d)*";
            String doublePattern = @"(?: \d+\.\d* | \d*\.\d+ | \d+ ) (?: [eE][\+-]?\d+)?";
            String spacePattern = @"\s+";

            // Overall pattern
            String pattern = String.Format("({0}) | ({1}) | ({2}) | ({3}) | ({4}) | ({5})",
                                            lpPattern, rpPattern, opPattern, varPattern, doublePattern, spacePattern);

            // Enumerate matching tokens that don't consist solely of white space.
            foreach (String s in Regex.Split(formula, pattern, RegexOptions.IgnorePatternWhitespace))
            {
                if (!Regex.IsMatch(s, @"^\s*$", RegexOptions.Singleline))
                {
                    yield return s;
                }
            }

        }


        /// <summary>
        /// Preforms a given operation on two given values, given in a stack. 
        /// </summary>
        /// <param name="oper">The operation to preform on the values</param>
        /// <param name="values">The stack of values to be preformed on.</param>
        /// <param name="num">The output number from the math operation.</param>
        /// <returns>Returns true if the math operation did not attempt to divide by zero.</returns>
        private bool MyMath(char oper, Stack<double> values, out double num)
        {
            double val1;
            double val2;
            val1 = values.Pop();
            val2 = values.Pop();

            if (oper.Equals('*'))
                val1 = val1 * val2;
            else if (oper.Equals('/'))
            {
                if (val1 == 0)
                {
                    num = 0;
                    return false;
                }
                val1 = val2 / val1;
            }
            else if (oper.Equals('+'))
                val1 = val1 + val2;
            else if (oper.Equals('-'))
                val1 = val2 - val1;
            num = val1;
            return true;
        }

        /// <summary>
        /// attempts to pop a given Stack<char>.
        /// </summary>
        /// <param name="stack">The stack to be popped.</param>
        /// <returns>, if the stack is empty it will return 'N' so it is known that popping the stack did not work. 
        /// If the stack constains anything it returns the peeked object. </returns>
        private static char TryPeek(Stack<char> stack)
        {
            if (stack.Count != 0)
                return stack.Peek();
            else
                return 'N';
        }
    }

    /// <summary>
    /// Used to report syntactic errors in the argument to the Formula constructor.
    /// </summary>
    public class FormulaFormatException : Exception
    {
        /// <summary>
        /// Constructs a FormulaFormatException containing the explanatory message.
        /// </summary>
        public FormulaFormatException(String message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Used as a possible return value of the Formula.Evaluate method.
    /// </summary>
    public struct FormulaError
    {
        /// <summary>
        /// Constructs a FormulaError containing the explanatory reason.
        /// </summary>
        /// <param name="reason"></param>
        public FormulaError(String reason)
            : this()
        {
            Reason = reason;
        }

        /// <summary>
        ///  The reason why this FormulaError was created.
        /// </summary>
        public string Reason { get; private set; }
    }
}
