// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Generic Boolean Algebra solver.
    /// Provides operations for conjunction, disjunction, and negation.
    /// Allows to decide if a predicate is satisfiable and if two predicates are equivalent.
    /// </summary>
    /// <typeparam name="T">predicates</typeparam>
    internal interface IBooleanAlgebra<T>
    {
        /// <summary>
        /// Top element of the Boolean algebra, corresponds to the value true.
        /// </summary>
        T True { get; }

        /// <summary>
        /// Bottom element of the Boolean algebra, corresponds to the value false.
        /// </summary>
        T False { get; }

        /// <summary>
        /// Make a conjunction of predicate1 and predicate2.
        /// </summary>
        T And(T predicate1, T predicate2);

        /// <summary>
        /// Make a conjunction of all the predicates in the enumeration.
        /// Returns True if the enumeration is empty.
        /// </summary>
        T And(IEnumerable<T> predicates);

        /// <summary>
        /// Make a disjunction of predicate1 and predicate2.
        /// </summary>
        T Or(T predicate1, T predicate2);

        /// <summary>
        /// Make a disjunction of all the predicates in the enumeration.
        /// Must return False if the enumeration is empty.
        /// </summary>
        T Or(IEnumerable<T> predicates);

        /// <summary>
        /// Negate the predicate.
        /// </summary>
        T Not(T predicate);

        /// <summary>
        /// Returns true iff the predicate is satisfiable.
        /// </summary>
        bool IsSatisfiable(T predicate);

        /// <summary>
        /// Returns true iff predicate1 is equivalent to predicate2.
        /// </summary>
        bool AreEquivalent(T predicate1, T predicate2);

        /// <summary>
        /// True means then if two predicates are equivalent then their hashcodes are equal.
        /// This is a weak form of extensionality.
        /// </summary>
        bool HashCodesRespectEquivalence { get; }

        /// <summary>
        /// True means that if two predicates are equivalent then they are identical.
        /// </summary>
        bool IsExtensional { get; }

        /// <summary>
        /// Given an array of constraints {c_1, c_2, ..., c_n} where n&gt;=0.
        /// Enumerate all satisfiable Boolean combinations Tuple({b_1, b_2, ..., b_n}, c)
        /// where c is satisfisable and equivalent to c'_1 &amp; c'_2 &amp; ... &amp; c'_n,
        /// where c'_i = c_i if b_i = true and c'_i is Not(c_i) otherwise.
        /// If n=0 return Tuple({},True)
        /// </summary>
        /// <param name="constraints">constraints</param>
        /// <returns>constraints that are satisfiable</returns>
        List<T> GenerateMinterms(IEnumerable<T> constraints);
    }
}
