// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Please keep the data structures in this file in sync with the managed version at
//  src/Common/src/Internal/Runtime/ModuleHeaders.cs
//  src/coreclr/inc/readytorun.h


struct ReadyToRunHeaderConstants
{
    static const uint32_t Signature = 0x00525452; // 'RTR'

    static const uint32_t CurrentMajorVersion = 9;
    static const uint32_t CurrentMinorVersion = 3;
};

struct ReadyToRunHeader
{
    uint32_t                Signature;      // ReadyToRunHeaderConstants.Signature
    uint16_t                MajorVersion;
    uint16_t                MinorVersion;

    uint32_t                Flags;

    uint16_t                NumberOfSections;
    uint8_t                 EntrySize;
    uint8_t                 EntryType;

    // Array of sections follows.
};

//
// ReadyToRunSectionType IDs are used by the runtime to look up specific global data sections
// from each module linked into the final binary. New sections should be added at the bottom
// of the enum and deprecated sections should not be removed to preserve ID stability.
//
// Eventually this will be reconciled with ReadyToRunSectionType from
// https://github.com/dotnet/runtime/blob/main/src/coreclr/inc/readytorun.h
//
enum class ReadyToRunSectionType
{
    StringTable                 = 200,
    GCStaticRegion              = 201,
    ThreadStaticRegion          = 202,
    // unused                   = 203,
    TypeManagerIndirection      = 204,
    EagerCctor                  = 205,
    FrozenObjectRegion          = 206,
    // 207 is unused - it was used by GCStaticDesc
    ThreadStaticOffsetRegion    = 208,
    // 209 is unused - it was used by ThreadStaticGCDescRegion
    // 210 is unused - it was used by ThreadStaticIndex
    // 211 is unused - it was used by LoopHijackFlag
    ImportAddressTables         = 212,

    // Sections 300 - 399 are reserved for RhFindBlob backwards compatibility
    ReadonlyBlobRegionStart     = 300,
    ReadonlyBlobRegionEnd       = 399,
};

enum class ModuleInfoFlags
{
    HasEndPointer               = 0x1,
};
