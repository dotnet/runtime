// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// Contains configurations of various static threshold data related to symbolic regexes.
    /// </summary>
    internal static class SymbolicRegexThresholds
    {
        /// <summary>Maximum number of <see cref="SymbolicRegexNode{TSet}"/> instances before switching over to NFA mode.</summary>
        /// <remarks>
        /// By default, all matching starts out using DFAs, where every state transitions to one and only one
        /// state for any minterm (each character maps to one minterm).  Some regular expressions, however, can result
        /// in really, really large DFA state graphs, much too big to actually store.  Instead of failing when we
        /// encounter such state graphs, at some point we instead switch from processing as a DFA to processing as
        /// an NFA. As an NFA, we instead track all of the states we're in at any given point.
        /// </remarks>
        /// <remarks>
        /// This limit is chosen due to memory usage constraints, the worst possible allocation is currently approx. 50 MB;
        /// There is some motivation to make this configurable, as it can exchange upfront costs with potentially
        /// significant search-time performance gains.
        /// Worst case memory consumption for the regex instance can be approximated to about (NfaNodeCountThreshold * (sizeof(MatchingState) + sizeof(SymbolicRegexNode))
        /// while it most cases the MatchingState part can be ignored, as only a subset of nodes have their own state.
        /// </remarks>
        internal const int NfaNodeCountThreshold = 125_000;

        /// <summary>
        /// Default maximum estimated safe expansion size of a <see cref="SymbolicRegexNode{TSet}"/> AST
        /// after the AST has been analyzed for safe handling.
        /// <remarks>
        /// If the AST exceeds this threshold then <see cref="NotSupportedException"/> is thrown.
        /// This default value may be overridden with the AppContext data
        /// whose name is given by  <see cref="SymbolicRegexSafeSizeThreshold_ConfigKeyName"/>.
        /// </remarks>
        /// This limit is chosen due to worst case NFA speed constraints, which is about 150kb/s,
        /// although it could be safely raised higher at the expense of worst-case NFA performance
        /// </summary>
        internal const int DefaultSymbolicRegexSafeSizeThreshold = 10_000;

        ///<summary>The environment variable name for a value overriding the default value <see cref="DefaultSymbolicRegexSafeSizeThreshold"/></summary>
        internal const string SymbolicRegexSafeSizeThreshold_ConfigKeyName = "REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE";

        /// <summary>
        /// Gets the value of the environment variable whose name is
        /// given by <see cref="SymbolicRegexSafeSizeThreshold_ConfigKeyName"/>
        /// or else returns <see cref="DefaultSymbolicRegexSafeSizeThreshold"/>
        /// if the environment variable is undefined, incorrectly formatted, or not a positive integer.
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
