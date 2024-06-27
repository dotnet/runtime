// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Symbolic
{
    /// <summary>
    /// These flags provide context-independent information available for every state. They provide a fast way to evaluate
    /// conditions in the inner matching loops of <see cref="SymbolicRegexMatcher{TSet}"/>. The matcher caches one of these
    /// for every state, for which they are created by <see cref="MatchingState{TSet}.BuildStateFlags(bool)"/>.
    /// In DFA mode the cached flags are used directly, while in NFA mode the <see cref="SymbolicRegexMatcher{TSet}.NfaStateHandler"/>
    /// handles aggregating the flags in the state set.
    /// </summary>
    [Flags]
    internal enum StateFlags : byte
    {
        None = 0,
        IsInitialFlag = 1,
        IsNullableFlag = 4,
        CanBeNullableFlag = 8,
        SimulatesBacktrackingFlag = 16,
        IsAcceleratedFlag = 32,
    }

    /// <summary>
    /// These extension methods for <see cref="StateFlags"/> make checking for the presence of flags more concise.
    /// </summary>
    internal static class StateFlagsExtensions
    {
        internal static bool IsInitial(this StateFlags info) => (info & StateFlags.IsInitialFlag) != StateFlags.None;
        internal static bool IsNullable(this StateFlags info) => (info & StateFlags.IsNullableFlag) != StateFlags.None;

        internal static bool CanBeNullable(this StateFlags info) => (info & StateFlags.CanBeNullableFlag) != StateFlags.None;

        internal static bool SimulatesBacktracking(this StateFlags info) => (info & StateFlags.SimulatesBacktrackingFlag) != StateFlags.None;

        internal static bool IsAccelerated(this StateFlags info) => (info & (StateFlags.IsAcceleratedFlag | StateFlags.IsInitialFlag)) != StateFlags.None;
    }
}
