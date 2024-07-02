// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial struct Metadata_1
{
    // The lower 16-bits of the MTFlags field are used for these flags,
    // if WFLAGS_HIGH.HasComponentSize is unset
    [Flags]
    internal enum WFLAGS_LOW : uint
    {
        GenericsMask = 0x00000030,
        GenericsMask_NonGeneric = 0x00000000,   // no instantiation

        StringArrayValues =
            GenericsMask_NonGeneric |
            0,
    }

    // Upper bits of MTFlags
    [Flags]
    internal enum WFLAGS_HIGH : uint
    {
        Category_Mask = 0x000F0000,
        Category_Array = 0x00080000,
        Category_Array_Mask = 0x000C0000,
        Category_Interface = 0x000C0000,
        ContainsGCPointers = 0x01000000,
        HasComponentSize = 0x80000000, // This is set if lower 16 bits is used for the component size,
                                       // otherwise the lower bits are used for WFLAGS_LOW
    }

    [Flags]
    internal enum WFLAGS2_ENUM : uint
    {
        DynamicStatics = 0x0002,
    }

    internal struct MethodTableFlags
    {
        public uint MTFlags { get; init; }
        public uint MTFlags2 { get; init; }
        public uint BaseSize { get; init; }

        private const int MTFlags2TypeDefRidShift = 8;
        private WFLAGS_HIGH FlagsHigh => (WFLAGS_HIGH)MTFlags;
        private WFLAGS_LOW FlagsLow => (WFLAGS_LOW)MTFlags;
        public int GetTypeDefRid() => (int)(MTFlags2 >> MTFlags2TypeDefRidShift);

        public WFLAGS_LOW GetFlag(WFLAGS_LOW mask) => throw new NotImplementedException("TODO");
        public WFLAGS_HIGH GetFlag(WFLAGS_HIGH mask) => FlagsHigh & mask;

        public WFLAGS2_ENUM GetFlag(WFLAGS2_ENUM mask) => (WFLAGS2_ENUM)MTFlags2 & mask;
        public bool IsInterface => GetFlag(WFLAGS_HIGH.Category_Mask) == WFLAGS_HIGH.Category_Interface;
        public bool IsString => HasComponentSize && !IsArray && RawGetComponentSize() == 2;

        public bool HasComponentSize => GetFlag(WFLAGS_HIGH.HasComponentSize) != 0;

        public bool IsArray => GetFlag(WFLAGS_HIGH.Category_Array_Mask) == WFLAGS_HIGH.Category_Array;

        public bool IsStringOrArray => HasComponentSize;
        public ushort RawGetComponentSize() => (ushort)(MTFlags & 0x0000ffff);

        private bool TestFlagWithMask(WFLAGS_LOW mask, WFLAGS_LOW flag)
        {
            if (IsStringOrArray)
            {
                return (WFLAGS_LOW.StringArrayValues & mask) == flag;
            }
            else
            {
                return (FlagsLow & mask) == flag;
            }
        }

        private bool TestFlagWithMask(WFLAGS2_ENUM mask, WFLAGS2_ENUM flag)
        {
            return ((WFLAGS2_ENUM)MTFlags2 & mask) == flag;
        }

        public bool HasInstantiation => !TestFlagWithMask(WFLAGS_LOW.GenericsMask, WFLAGS_LOW.GenericsMask_NonGeneric);

        public bool ContainsGCPointers => GetFlag(WFLAGS_HIGH.ContainsGCPointers) != 0;

        public bool IsDynamicStatics => GetFlag(WFLAGS2_ENUM.DynamicStatics) != 0;
    }
}
