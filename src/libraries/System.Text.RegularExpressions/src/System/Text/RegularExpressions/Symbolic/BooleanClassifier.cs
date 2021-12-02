// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Classifies characters as true or false based on a supplied <see cref="BDD"/>.</summary>
    /// <remarks>
    /// The classification is determined entirely by the BDD used to construct the classifier, and in fact
    /// simply calling Contains on the BDD instead of using the classifier would suffice from a correctness
    /// perspective. The classifier as a wrapper for the BDD is valuable in order to optimize for ASCII, as
    /// it precomputes the results for ASCII inputs and stores them in a separate table, only falling back
    /// to using the BDD for non-ASCII.
    /// </remarks>
    internal sealed class BooleanClassifier
    {
        /// <summary>Lookup table used for ASCII characters.</summary>
        private readonly bool[] _ascii;
        /// <summary>BDD used for non-ASCII characters.</summary>
        private readonly BDD _nonAscii;

        /// <summary>Create a Boolean classifier.</summary>
        /// <param name="solver">Character algebra (the algebra is not stored in the classifier)</param>
        /// <param name="bdd">Elements that map to true.</param>
        public BooleanClassifier(CharSetSolver solver, BDD bdd)
        {
            // We want to optimize for ASCII, so query the BDD for each ASCII character in
            // order to precompute a lookup table we'll use at match time.
            var ascii = new bool[128];
            for (int i = 0; i < ascii.Length; i++)
            {
                ascii[i] = bdd.Contains(i);
            }

            // At this point, we'll never consult the BDD for ASCII characters, so as an
            // optimization we can remove them from the BDD in hopes of simplifying it and making
            // it faster to query for the non-ASCII characters we will use it for. However, while
            // this is typically an optimization, it isn't always: the act of removing some
            // characters from the BDD can actually make the branching more complicated.  The
            // extreme case of this is when the BDD is True, meaning everything maps to True, which
            // is as simple a BDD as you can get.  In such a case, even though it's rare, this would
            // definitively be a deoptimization, so we avoid doing so.  Other trivial cases are handled
            // by And itself, e.g. if the BDD == False, then And will just return False.
            if (!bdd.IsFull)
            {
                bdd = solver.And(solver._nonAscii, bdd);
            }

            _ascii = ascii;
            _nonAscii = bdd;
        }

        /// <summary>Gets whether the specified character is classified as true.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsTrue(char c)
        {
            bool[] ascii = _ascii;
            return c < ascii.Length ? ascii[c] : _nonAscii.Contains(c);
        }
    }
}
