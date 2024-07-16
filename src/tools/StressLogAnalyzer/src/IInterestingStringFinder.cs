// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Diagnostics.DataContractReader;

namespace StressLogAnalyzer;

public enum WellKnownString
{
    THREAD_WAIT,
    THREAD_WAIT_DONE,
    GCSTART,
    GCEND,
    MARK_START,
    PLAN_START,
    RELOCATE_START,
    RELOCATE_END,
    COMPACT_START,
    COMPACT_END,
    GCROOT,
    PLUG_MOVE,
    GCMEMCOPY,
    GCROOT_PROMOTE,
    PLAN_PLUG,
    PLAN_PINNED_PLUG,
    DESIRED_NEW_ALLOCATION,
    MAKE_UNUSED_ARRAY,
    START_BGC_THREAD,
    RELOCATE_REFERENCE,
    LOGGING_OFF,
}

public interface IInterestingStringFinder
{
    bool IsInteresting(TargetPointer formatStringPointer, out WellKnownString? wellKnownString);

    bool IsWellKnown([NotNullWhen(true)] out WellKnownString? wellKnownString);
}
