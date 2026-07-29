// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ported from src/coreclr/gc/gcinterface.h.

namespace Internal.Runtime.GarbageCollection
{
    /// <summary>
    /// One of the two providers that the GC can fire events from: the default and private providers.
    /// </summary>
    internal enum GCEventProvider
    {
        Default = 0,
        Private = 1,

        /// <summary>Number of providers; not a provider itself.</summary>
        Count = 2,
    }

    /// <summary>
    /// Event levels corresponding to events that can be fired by the GC.
    /// </summary>
    internal enum GCEventLevel
    {
        None = 0,
        Fatal = 1,
        Error = 2,
        Warning = 3,
        Information = 4,
        Verbose = 5,
        Max = 6,
        LogAlways = 255,
    }

    /// <summary>
    /// Event keywords corresponding to events that can be fired by the GC. These numbers come from
    /// the ETW manifest itself - please make changes to this enum if you add, remove, or change
    /// keyword sets that are used by the GC!
    /// </summary>
    [System.Flags]
    internal enum GCEventKeyword
    {
        None = 0x0,
        GC = 0x1,

        // Duplicate on purpose, GCPrivate is the same keyword as GC, with a different provider.
        GCPrivate = 0x1,

        GCHandle = 0x2,
        GCHandlePrivate = 0x4000,
        GCHeapDump = 0x100000,
        GCSampledObjectAllocationHigh = 0x200000,
        GCHeapSurvivalAndMovement = 0x400000,
        ManagedHeapCollect = 0x800000,
        GCHeapAndTypeNames = 0x1000000,
        GCSampledObjectAllocationLow = 0x2000000,

        All = GC
            | GCPrivate
            | GCHandle
            | GCHandlePrivate
            | GCHeapDump
            | GCSampledObjectAllocationHigh
            | GCHeapSurvivalAndMovement
            | ManagedHeapCollect
            | GCHeapAndTypeNames
            | GCSampledObjectAllocationLow,
    }
}
