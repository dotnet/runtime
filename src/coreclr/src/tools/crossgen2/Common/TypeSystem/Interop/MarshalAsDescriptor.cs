// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;
using System;

namespace Internal.TypeSystem
{
    public enum NativeTypeKind : byte
    {
        Boolean = 0x2,
        I1 = 0x3,
        U1 = 0x4,
        I2 = 0x5,
        U2 = 0x6,
        I4 = 0x7,
        U4 = 0x8,
        I8 = 0x9,
        U8 = 0xa,
        R4 = 0xb,
        R8 = 0xc,
        LPStr = 0x14,
        LPWStr = 0x15,
        LPTStr = 0x16,        // Ptr to OS preferred (SBCS/Unicode) string
        ByValTStr = 0x17,     // OS preferred (SBCS/Unicode) inline string (only valid in structs)
        Struct = 0x1b,
        SafeArray = 0x1d,
        ByValArray = 0x1e,
        SysInt = 0x1f,
        SysUInt = 0x20,
        Int = 0x1f,
        UInt = 0x20,
        Func = 0x26,
        AsAny = 0x28,
        Array = 0x2a,
        LPStruct = 0x2b,    // This is not  defined in Ecma-335(II.23.4)
        LPUTF8Str = 0x30,
        Invalid = 0x50,      // This is the default value
        Variant = 0x51,
    }

    public class MarshalAsDescriptor
    {
        public NativeTypeKind Type { get; }
        public NativeTypeKind ArraySubType { get; }
        public uint? SizeParamIndex { get; }
        public uint? SizeConst { get; }

        public MarshalAsDescriptor(NativeTypeKind type, NativeTypeKind arraySubType, uint? sizeParamIndex, uint? sizeConst)
        {
            Type = type;
            ArraySubType = arraySubType;
            SizeParamIndex = sizeParamIndex;
            SizeConst = sizeConst;
        }
    }
}
