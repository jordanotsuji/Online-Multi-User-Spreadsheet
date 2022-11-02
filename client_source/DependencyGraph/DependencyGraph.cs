// Skeleton implementation written by Joe Zachary for CS 3500, September 2013.
// Version 1.1 (Fixed error in comment for RemoveDependency.)
// Version 1.2 - Daniel Kopta 
//               (Clarified meaning of dependent and dependee.)
//               (Clarified names in solution/project structure.)
// Version 2.0 - Alan Bird 
//               (First implementation up to replace dependents and dependees)
// Version 2.1 - (Implemented "replace" methods and added a few tests.)
// Version 2.2 - FINAL VERSION (added a few tests and fixed a few bugs.)
// Version 2.3 - ACTUAL FINAL (changed file name from DependancyGraph to DependencyGraph (probably will requre further adjustment))

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace SpreadsheetUtilities
{

    /// <summary>
    /// (s1,t1) is an ordered pair of strings
    /// t1 depends on s1; s1 must be evaluated before t1
    /// 
    /// A DependencyGraph can be modeled as a set of ordered pairs of strings.  Two ordered pairs
    /// (s1,t1) and (s2,t2) are considered equal if and only if s1 equals s2 and t1 equals t2.
    /// Recall that sets never contain duplicates.  If an attempt is made to add an element to a 
    /// set, and the element is already in the set, the set remains unchanged.
    /// 
    /// Given a DependencyGraph DG:
    /// 
    ///    (1) If s is a string, the set of all strings t such that (s,t) is in DG is called dependents(s).
    ///        (The set of things that depend on s)    
    ///        
    ///    (2) If s is a string, the set of all strings t such that (t,s) is in DG is called dependees(s).
    ///        (The set of things that s depends on) 
    //
    // For example, suppose DG = {("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}
    //     dependents("a") = {"b", "c"}
    //     dependents("b") = {"d"}
    //     dependents("c") = {}
    //     dependents("d") = {"d"}
    //     dependees("a") = {}
    //     dependees("b") = {"a"}
    //     dependees("c") = {"a"}
    //     dependees("d") = {"b", "d"}
    /// </summary>
    public class DependencyGraph
    {
        // if something is a dependee (or it has dependants) it goes into the key of 
        //forwardDictionary. The dependents go into the HashSet.
        private Dictionary<string, HashSet<string>> ForwardDictioinary;
        // if something is a dependent (or relys on a dependee) it goes into the key of the 
        //backwardDictionary. Its dependees go into the HashSet. 
        private Dictionary<string, HashSet<string>> BackwardDictioinary;
        private int size;



        /// <summary>
        /// Creates an empty DependencyGraph.
        /// </summary>
        public DependencyGraph()
        {
            ForwardDictioinary = new Dictionary<string, HashSet<string>>();
            BackwardDictioinary = new Dictionary<string, HashSet<string>>();
            size = 0;

        }


        /// <summary>
        /// The number of ordered pairs in the DependencyGraph.
        /// </summary>
        public int Size
        {
            get { return size; }
        }


        /// <summary>
        /// The size of dependees(s).
        /// This property is an example of an indexer.  If dg is a DependencyGraph, you would
        /// invoke it like this:
        /// dg["a"]
        /// It should return the size of dependees("a")
        /// </summary>
        public int this[string s]
        {
            get
            {
                if (BackwardDictioinary.ContainsKey(s))
                    return BackwardDictioinary[s].Count();
                else
                    return 0;
            }
        }


        /// <summary>
        /// Reports whether dependents(s) is non-empty.
        /// </summary>
        public bool HasDependents(string s)
        {
            if (ForwardDictioinary.ContainsKey(s))
                return ForwardDictioinary[s].Count > 0;
            return false;
        }


        /// <summary>
        /// Reports whether dependees(s) is non-empty.
        /// </summary>
        public bool HasDependees(string s)
        {
            if (BackwardDictioinary.ContainsKey(s))
                return BackwardDictioinary[s].Count > 0;
            return false;
        }


        /// <summary>
        /// Enumerates dependents(s).
        /// </summary>
        public IEnumerable<string> GetDependents(string s)
        {
            if (ForwardDictioinary.ContainsKey(s))
            {
                LinkedList<string> toReturn = new LinkedList<string>();
                foreach (string t in ForwardDictioinary[s])
                    toReturn.AddLast(t);
                return toReturn;
            }
            return new String[] { };
        }

        /// <summary>
        /// Enumerates dependees(s).
        /// </summary>
        public IEnumerable<string> GetDependees(string s)
        {
            if (BackwardDictioinary.ContainsKey(s))
            {
                LinkedList<string> toReturn = new LinkedList<string>();
                foreach (string t in BackwardDictioinary[s])
                    toReturn.AddLast(t);
                return toReturn;
            }
            return new String[] { };
        }


        /// <summary>
        /// <para>Adds the ordered pair (s,t), if it doesn't exist</para>
        /// 
        /// <para>This should be thought of as:</para>   
        /// 
        ///   t depends on s
        ///
        /// </summary>
        /// <param name="s"> s must be evaluated first. T depends on S</param>
        /// <param name="t"> t cannot be evaluated until s is</param>        /// 
        public void AddDependency(string s, string t)
        {
            if (!ForwardDictioinary.ContainsKey(s))
            {
                size++;
                //if the forward dictionary does not contain our first value we simply add it
                //and assign it a new HashSet containing t.
                ForwardDictioinary.Add(s, new HashSet<string>(new string[] { t }));
                if (!BackwardDictioinary.ContainsKey(t))
                    BackwardDictioinary.Add(t, new HashSet<string>(new string[] { s }));
                else
                    BackwardDictioinary[t].Add(s);
            }
            else if (!ForwardDictioinary[s].Contains(t))
            {
                size++;
                ForwardDictioinary[s].Add(t);
                if (!BackwardDictioinary.ContainsKey(t))
                    BackwardDictioinary.Add(t, new HashSet<string>(new string[] { s }));
                else
                    BackwardDictioinary[t].Add(s);

            }
        }


        /// <summary>
        /// Removes the ordered pair (s,t), if it exists
        /// </summary>
        /// <param name="s"></param>
        /// <param name="t"></param>
        public void RemoveDependency(string s, string t)
        {
            if (ForwardDictioinary.ContainsKey(s))
            {
                if (ForwardDictioinary[s].Contains(t))
                {
                    ForwardDictioinary[s].Remove(t);
                    BackwardDictioinary[t].Remove(s);
                    size--;
                }
            }

        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (s,r).  Then, for each
        /// t in newDependents, adds the ordered pair (s,t).
        /// </summary>
        public void ReplaceDependents(string s, IEnumerable<string> newDependents)
        {
            IEnumerable<string> toRemove = GetDependents(s);
            foreach (string r in toRemove)
                RemoveDependency(s, r);
            foreach (string t in newDependents)
            {
                AddDependency(s, t);
            }
        }


        /// <summary>
        /// Removes all existing ordered pairs of the form (r,s).  Then, for each 
        /// t in newDependees, adds the ordered pair (t,s).
        /// </summary>
        public void ReplaceDependees(string s, IEnumerable<string> newDependees)
        {
            if (BackwardDictioinary.ContainsKey(s))
            {
                IEnumerable toRemove = GetDependees(s);
                foreach (string t in toRemove)
                    RemoveDependency(t, s);
            }

            foreach (string t in newDependees)
                AddDependency(t, s);
        }

    }

}