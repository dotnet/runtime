// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime
{
    //
    // Please keep the data structures in this file in sync with the native version at
    //  src/coreclr/inc/readytorun.h
    //  src/coreclr/nativeaot/Runtime/inc/ModuleHeaders.h
    //

    internal struct ReadyToRunHeaderConstants
    {
        public const uint Signature = 0x00525452; // 'RTR'

        public const ushort CurrentMajorVersion = 9;
        public const ushort CurrentMinorVersion = 2;
    }
#if READYTORUN
#pragma warning disable 0169
    internal struct ReadyToRunHeader
    {
        private uint Signature;      // ReadyToRunHeaderConstants.Signature
        private ushort MajorVersion;
        private ushort MinorVersion;

        private uint Flags;

        private ushort NumberOfSections;
        private byte EntrySize;
        private byte EntryType;

        // Array of sections follows.
    };
#pragma warning restore 0169
#endif

    //
    // ReadyToRunSectionType IDs are used by the runtime to look up specific global data sections
    // from each module linked into the final binary. New sections should be added at the bottom
    // of the enum and deprecated sections should not be removed to preserve ID stability.
    //
    // This list should be kept in sync with the runtime version at
    // https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/readytorun.h
    //
#if SYSTEM_PRIVATE_CORELIB
    internal
#else
    public
#endif
    enum ReadyToRunSectionType
    {
        //
        // CoreCLR ReadyToRun sections
        //
        CompilerIdentifier = 100,
        ImportSections = 101,
        RuntimeFunctions = 102,
        MethodDefEntryPoints = 103,
        ExceptionInfo = 104,
        DebugInfo = 105,
        DelayLoadMethodCallThunks = 106,
        // 107 is deprecated - it was used by an older format of AvailableTypes
        AvailableTypes = 108,
        InstanceMethodEntryPoints = 109,
        InliningInfo = 110, // Added in v2.1, deprecated in 4.1
        ProfileDataInfo = 111, // Added in v2.2
        ManifestMetadata = 112, // Added in v2.3
        AttributePresence = 113, // Added in V3.1
        InliningInfo2 = 114, // Added in 4.1
        ComponentAssemblies = 115, // Added in 4.1
        OwnerCompositeExecutable = 116, // Added in 4.1
        PgoInstrumentationData = 117, // Added in 5.2
        ManifestAssemblyMvids = 118, // Added in 5.3
        CrossModuleInlineInfo = 119, // Added in 6.3
        HotColdMap = 120, // Added in 8.0
        MethodIsGenericMap = 121, // Added in V9.0
        EnclosingTypeMap = 122, // Added in V9.0
        TypeGenericInfoMap = 123, // Added in V9.0

        //
        // NativeAOT ReadyToRun sections
        //
        StringTable = 200, // Unused
        GCStaticRegion = 201,
        ThreadStaticRegion = 202,
        // Unused = 203,
        TypeManagerIndirection = 204,
        EagerCctor = 205,
        FrozenObjectRegion = 206,
        DehydratedData = 207,
        ThreadStaticOffsetRegion = 208,
        // 209 is unused - it was used by ThreadStaticGCDescRegion
        // 210 is unused - it was used by ThreadStaticIndex
        // 211 is unused - it was used by LoopHijackFlag
        ImportAddressTables = 212,
        ModuleInitializerList = 213,

        // Sections 300 - 399 are reserved for RhFindBlob backwards compatibility
        ReadonlyBlobRegionStart = 300,
        ReadonlyBlobRegionEnd = 399,
    }

    [Flags]
    internal enum ModuleInfoFlags : int
    {
        HasEndPointer = 0x1,
    }
}
