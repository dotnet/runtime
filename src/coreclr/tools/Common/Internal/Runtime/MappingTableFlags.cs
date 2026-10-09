// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Internal.Runtime
{
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
        IsDefaultConstructor = 0x00000004,
        RequiresInstArg = 0x00000008,
        HasEntrypoint = 0x00000010,
        NeedsParameterInterpretation = 0x00000020,
    }

    [Flags]
    public enum FieldTableFlags : uint
    {
        Instance = 0x00,
        NonGCStatic = 0x01,
        GCStatic = 0x02,
        ThreadStatic = 0x03,

        StorageClass = 0x03,

        FieldOffsetEncodedDirectly = 0x04,
        IsInitOnly = 0x08
    }
}
