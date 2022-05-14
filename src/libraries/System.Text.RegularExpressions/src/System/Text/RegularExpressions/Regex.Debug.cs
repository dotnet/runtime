// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if DEBUG
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions.Symbolic;

namespace System.Text.RegularExpressions
{
    public partial class Regex
    {
        /// <summary>Unwind the regex and save the resulting state graph in DGML</summary>
        /// <param name="writer">Writer to which the DGML is written.</param>
        /// <param name="nfa">True to create an NFA instead of a DFA.</param>
        /// <param name="addDotStar">True to prepend .*? onto the pattern (outside of the implicit root capture).</param>
        /// <param name="reverse">If true, then unwind the regex backwards (and <paramref name="addDotStar"/> is ignored).</param>
        /// <param name="maxStates">The approximate maximum number of states to include; less than or equal to 0 for no maximum.</param>
        /// <param name="maxLabelLength">maximum length of labels in nodes anything over that length is indicated with .. </param>
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        internal void SaveDGML(TextWriter writer, bool nfa, bool addDotStar, bool reverse, int maxStates, int maxLabelLength)
        {
            if (factory is not SymbolicRegexRunnerFactory srmFactory)
            {
                throw new NotSupportedException();
            }

            srmFactory._matcher.SaveDGML(writer, nfa, addDotStar, reverse, maxStates, maxLabelLength);
        }

        /// <summary>
        /// Generates UnicodeCategoryRanges.cs for the namespace System.Text.RegularExpressions.Symbolic.Unicode
        /// in the given directory path. Only avaliable in DEBUG mode.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        internal static void GenerateUnicodeTables(string path)
        {
            UnicodeCategoryRangesGenerator.Generate("System.Text.RegularExpressions.Symbolic", "UnicodeCategoryRanges", path);
        }

        /// <summary>
        /// Generates up to k random strings matched by the regex
        /// </summary>
        /// <param name="k">upper bound on the number of generated strings</param>
        /// <param name="randomseed">random seed for the generator, 0 means no random seed</param>
        /// <param name="negative">if true then generate inputs that do not match</param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage(Justification = "Debug only")]
        internal IEnumerable<string> GenerateRandomMembers(int k, int randomseed, bool negative)
        {
            if (factory is not SymbolicRegexRunnerFactory srmFactory)
            {
                throw new NotSupportedException();
            }

            return srmFactory._matcher.GenerateRandomMembers(k, randomseed, negative);
        }
    }
}
#endif
