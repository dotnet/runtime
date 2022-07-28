// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>Utility class providing singleton <see cref="BDD"/>s for evaluating whether a character is part of a particular Unicode category.</summary>
    internal static class UnicodeCategoryConditions
    {
        /// <summary>The number of values in <see cref="UnicodeCategory"/>.</summary>
        private const int UnicodeCategoryValueCount = 30;

        /// <summary>Array containing lazily-initialized BDDs per defined UnicodeCategory value.</summary>
        private static readonly BDD?[] s_categories = new BDD[UnicodeCategoryValueCount];
        /// <summary>Lazily-initialized BDD for \s.</summary>
        private static volatile BDD? s_whiteSpace;
        /// <summary>Lazily-initialized BDD for \w.</summary>
        private static volatile BDD? s_wordLetter;
        /// <summary>Lazily-initialized BDD for \b.</summary>
        private static volatile BDD? s_wordLetterForAnchors;

#if DEBUG
        static UnicodeCategoryConditions()
        {
            // The implementation caches a BDD per defined UnicodeCategory.  If the enum ever gets
            // additional named values, the constant will need to be updated to reflect that.
            Debug.Assert(Enum.GetValues<UnicodeCategory>().Length == UnicodeCategoryValueCount);
        }
#endif

        /// <summary>Gets a <see cref="BDD"/> that represents the specified <see cref="UnicodeCategory"/>.</summary>
        public static BDD GetCategory(UnicodeCategory category) =>
            Volatile.Read(ref s_categories[(int)category]) ??
            Interlocked.CompareExchange(ref s_categories[(int)category], BDD.Deserialize(UnicodeCategoryRanges.GetSerializedCategory(category)), null) ??
            s_categories[(int)category]!;

        /// <summary>Gets a <see cref="BDD"/> that represents the \s character class.</summary>
        public static BDD WhiteSpace =>
            s_whiteSpace ??
            Interlocked.CompareExchange(ref s_whiteSpace, BDD.Deserialize(UnicodeCategoryRanges.SerializedWhitespaceBDD), null) ??
            s_whiteSpace;

        /// <summary>Gets a <see cref="BDD"/> that represents the \w character class.</summary>
        /// <remarks>\w is the union of the 8 categories: 0,1,2,3,4,5,8,18</remarks>
        public static BDD WordLetter(CharSetSolver solver) =>
            s_wordLetter ??
            Interlocked.CompareExchange(ref s_wordLetter,
                                        solver.Or(new[]
                                        {
                                            GetCategory(UnicodeCategory.UppercaseLetter),
                                            GetCategory(UnicodeCategory.LowercaseLetter),
                                            GetCategory(UnicodeCategory.TitlecaseLetter),
                                            GetCategory(UnicodeCategory.ModifierLetter),
                                            GetCategory(UnicodeCategory.OtherLetter),
                                            GetCategory(UnicodeCategory.NonSpacingMark),
                                            GetCategory(UnicodeCategory.DecimalDigitNumber),
                                            GetCategory(UnicodeCategory.ConnectorPunctuation),
                                        }),
                                        null) ??
            s_wordLetter;

        /// <summary>
        /// Gets a <see cref="BDD"/> that represents <see cref="WordLetter"/> together with the characters
        /// \u200C (zero width non joiner) and \u200D (zero width joiner) that are treated as if they were
        /// word characters in the context of the anchors \b and \B.
        /// </summary>
        public static BDD WordLetterForAnchors(CharSetSolver solver) =>
            s_wordLetterForAnchors ??
            Interlocked.CompareExchange(ref s_wordLetterForAnchors, solver.Or(WordLetter(solver), solver.CreateBDDFromRange('\u200C', '\u200D')), null) ??
            s_wordLetterForAnchors;
    }
}
