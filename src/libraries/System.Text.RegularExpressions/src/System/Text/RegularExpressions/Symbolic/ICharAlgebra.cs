// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Extends ICharAlgebra with character predicate solving and predicate pretty printing.
    /// </summary>
    /// <typeparam name="T">predicates</typeparam>
    internal interface ICharAlgebra<T> : IBooleanAlgebra<T>
    {
        /// <summary>
        /// Make a constraint describing a singleton set containing the character c.
        /// </summary>
        /// <param name="c">the given character</param>
        T CharConstraint(char c);

        /// <summary>
        /// Make a term that encodes the given character set.
        /// </summary>
        T ConvertFromCharSet(BDDAlgebra bddAlg, BDD set);

        /// <summary>
        /// Convert a predicate into a set of characters.
        /// </summary>
        BDD ConvertToCharSet(T pred);

        /// <summary>
        /// Returns the minterms (a partition of the full domain).
        /// </summary>
        T[]? GetMinterms();

        /// <summary>
        /// Pretty print the character predicate
        /// </summary>
        string PrettyPrint(T pred);
    }
}
