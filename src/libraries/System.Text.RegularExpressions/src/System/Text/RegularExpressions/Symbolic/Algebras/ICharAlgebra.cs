// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Extends ICharAlgebra with character predicate solving and predicate pretty printing.
    /// </summary>
    /// <typeparam name="T">predicates</typeparam>
    internal interface ICharAlgebra<T> : IBooleanAlgebra<T>
    {
        /// <summary>
        /// Make a constraint describing the set of all characters between a (inclusive) and b (inclusive).
        /// Add both uppercase and lowercase elelements if caseInsensitive is true using the given culture
        /// or the current culture when the given culture is null.
        /// </summary>
        T RangeConstraint(char lower, char upper, bool caseInsensitive = false, string? culture = null);

        /// <summary>
        /// Make a constraint describing a singleton set containing the character c, or
        /// a set containing also the upper and lowercase versions of c if caseInsensitive is true.
        /// </summary>
        /// <param name="caseInsensitive">if true include both the uppercase and the lowercase versions of the given character</param>
        /// <param name="c">the given character</param>
        /// <param name="culture">given culture, if null then the current culture is assumed</param>
        T CharConstraint(char c, bool caseInsensitive = false, string? culture = null);

        /// <summary>
        /// Make a term that encodes the given character set.
        /// </summary>
        T ConvertFromCharSet(BDDAlgebra bddAlg, BDD set);

        /// <summary>
        /// Compute the number of elements in the set
        /// </summary>
        ulong ComputeDomainSize(T set);

        /// <summary>
        /// Enumerate all characters in the set
        /// </summary>
        /// <param name="set">given set</param>
        IEnumerable<char> GenerateAllCharacters(T set);

        /// <summary>
        /// Convert a predicate into a set of characters.
        /// </summary>
        BDD ConvertToCharSet(ICharAlgebra<BDD> bddalg, T pred);

        /// <summary>
        /// Gets the underlying character set solver.
        /// </summary>
        CharSetSolver CharSetProvider { get; }

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
