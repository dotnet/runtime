// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Contains configurations of various static threshold data related to symbolic regexes.
    /// </summary>
    internal static class SymbolicRegexThresholds
    {
        /// <summary>Maximum number of built states before switching over to NFA mode.</summary>
        /// <remarks>
        /// By default, all matching starts out using DFAs, where every state transitions to one and only one
        /// state for any minterm (each character maps to one minterm).  Some regular expressions, however, can result
        /// in really, really large DFA state graphs, much too big to actually store.  Instead of failing when we
        /// encounter such state graphs, at some point we instead switch from processing as a DFA to processing as
        /// an NFA.  As an NFA, we instead track all of the states we're in at any given point, and transitioning
        /// from one "state" to the next really means for every constituent state that composes our current "state",
        /// we find all possible states that transitioning out of each of them could result in, and the union of
        /// all of those is our new "state".  This constant represents the size of the graph after which we start
        /// processing as an NFA instead of as a DFA.  This processing doesn't change immediately, however. All
        /// processing starts out in DFA mode, even if we've previously triggered NFA mode for the same regex.
        /// We switch over into NFA mode the first time a given traversal (match operation) results in us needing
        /// to create a new node and the graph is already or newly beyond this threshold.
        /// </remarks>
        internal const int NfaThreshold = 10_000;

        /// <summary>
        /// Default maximum estimated safe expansion size of a <see cref="SymbolicRegexNode{TSet}"/> AST
        /// after the AST has been anlayzed for safe handling.
        /// <remarks>
        /// If the AST exceeds this threshold then <see cref="NotSupportedException"/> is thrown.
        /// This default value may be overridden with the AppContext data
        /// whose name is given by  <see cref="SymbolicRegexSafeSizeThreshold_ConfigKeyName"/>.
        /// </remarks>
        /// </summary>
        internal const int DefaultSymbolicRegexSafeSizeThreshold = 1000;

        ///<summary>The environment variable name for a value overriding the default value <see cref="DefaultSymbolicRegexSafeSizeThreshold"/></summary>
        internal const string SymbolicRegexSafeSizeThreshold_ConfigKeyName = "REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE";

        /// <summary>
        /// Gets the value of the environment variable whose name is
        /// given by <see cref="SymbolicRegexSafeSizeThreshold_ConfigKeyName"/>
        /// or else returns <see cref="DefaultSymbolicRegexSafeSizeThreshold"/>
        /// if the environment variable is undefined, incorrectly formated, or not a positive integer.
        /// </summary>
        /// <remarks>
        /// The value is queried from <code>AppContext</code>
        /// If the AppContext's data value for that key is
        /// not a positive integer then <see cref="DefaultSymbolicRegexSafeSizeThreshold"/> is returned.
        /// </remarks>
        internal static int GetSymbolicRegexSafeSizeThreshold()
        {
            object? safeSizeThreshold = AppContext.GetData(SymbolicRegexSafeSizeThreshold_ConfigKeyName);

            return (safeSizeThreshold is not int safeSizeThresholdInt || safeSizeThresholdInt <= 0) ?
                DefaultSymbolicRegexSafeSizeThreshold :
                safeSizeThresholdInt;
        }
    }
}
