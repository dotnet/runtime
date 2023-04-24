// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime
{
    internal struct DynamicInvokeMapEntry
    {
        public const uint IsImportMethodFlag = 0x40000000;
        public const uint InstantiationDetailIndexMask = 0x3FFFFFFF;
    }

    internal struct VirtualInvokeTableEntry
    {
        public const int GenericVirtualMethod = 1;
        public const int FlagsMask = 1;
    }

    [Flags]
    public enum InvokeTableFlags : uint
    {
        HasVirtualInvoke = 0x00000001,
        IsGenericMethod = 0x00000002,
        HasMetadataHandle = 0x00000004,
        IsDefaultConstructor = 0x00000008,
        RequiresInstArg = 0x00000010,
        HasEntrypoint = 0x00000020,
        IsUniversalCanonicalEntry = 0x00000040,
        NeedsParameterInterpretation = 0x00000080,
        CallingConventionDefault = 0x00000000,
        Cdecl = 0x00001000,
        Winapi = 0x00002000,
        StdCall = 0x00003000,
        ThisCall = 0x00004000,
        FastCall = 0x00005000,
        CallingConventionMask = 0x00007000,
    }

    [Flags]
    public enum FieldTableFlags : uint
    {
        Instance = 0x00,
        NonGCStatic = 0x01,
        GCStatic = 0x02,
        ThreadStatic = 0x03,

        StorageClass = 0x03,

        IsUniversalCanonicalEntry = 0x04,
        HasMetadataHandle = 0x08,
        FieldOffsetEncodedDirectly = 0x20,
        IsAnyCanonicalEntry = 0x40,
        IsInitOnly = 0x80
    }
}
