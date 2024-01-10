// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// These flags provide context-independent information available for every state. They provide a fast way to evaluate
    /// conditions in the inner matching loops of <see cref="SymbolicRegexMatcher{TSet}"/>. The matcher caches one of these
    /// for every state, for which they are created by <see cref="MatchingState{TSet}.BuildStateFlags(ISolver{TSet}, bool)"/>.
    /// In DFA mode the cached flags are used directly, while in NFA mode the <see cref="SymbolicRegexMatcher{TSet}.NfaStateHandler"/>
    /// handles aggregating the flags in the state set.
    /// </summary>
    [Flags]
    internal enum StateFlags : byte
    {
        IsInitialFlag = 1,
        IsDeadendFlag = 2,
        IsNullableFlag = 4,
        CanBeNullableFlag = 8,
        SimulatesBacktrackingFlag = 16,
    }

    /// <summary>
    /// These extension methods for <see cref="StateFlags"/> make checking for the presence of flags more concise.
    /// </summary>
    internal static class StateFlagsExtensions
    {
        internal static bool IsInitial(this StateFlags info) => (info & StateFlags.IsInitialFlag) != 0;
        internal static bool IsDeadend(this StateFlags info) => (info & StateFlags.IsDeadendFlag) != 0;
        internal static bool IsNullable(this StateFlags info) => (info & StateFlags.IsNullableFlag) != 0;
        internal static bool CanBeNullable(this StateFlags info) => (info & StateFlags.CanBeNullableFlag) != 0;
        internal static bool SimulatesBacktracking(this StateFlags info) => (info & StateFlags.SimulatesBacktrackingFlag) != 0;
    }
}
