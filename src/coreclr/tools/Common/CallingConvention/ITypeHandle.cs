// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Suppress analyzer warnings for crossgen2 code style when file-linked into cDAC
#pragma warning disable SA1001 // Commas should be followed by whitespace

using Internal.CorConstants;
using Internal.JitInterface;
using Internal.TypeSystem;

namespace Internal.CallingConvention
{
    /// <summary>
    /// Abstraction over type information needed by ArgIterator and TransitionBlock
    /// for calling convention computation. Implementations can be backed by crossgen2's
    /// TypeDesc or by the cDAC's MethodTable reading.
    /// </summary>
    internal interface ITypeHandle
    {
        bool IsNull();
        bool IsValueType();
        bool IsPointerType();
        bool HasIndeterminateSize();
        int PointerSize { get; }
        int GetSize();
        CorElementType GetCorElementType();
        bool RequiresAlign8();

        // HFA - ARM/ARM64
        bool IsHomogeneousAggregate();
        int GetHomogeneousAggregateElementSize();

        // SystemV AMD64 - x64 Unix struct classification
        void GetSystemVAmd64PassStructInRegisterDescriptor(out SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor);

        // RISC-V / LoongArch64 FP struct classification
        FpStructInRegistersInfo GetFpStructInRegistersInfo(TargetArchitecture architecture);

        // x86 - trivial pointer-sized struct check for register passing
        bool IsTrivialPointerSizedStruct();

        // LoongArch64/Wasm alignment
        int GetFieldAlignment();

        private static readonly int[] s_elemSizes = new int[]
        {
            0, //ELEMENT_TYPE_END          0x0
            0, //ELEMENT_TYPE_VOID         0x1
            1, //ELEMENT_TYPE_BOOLEAN      0x2
            2, //ELEMENT_TYPE_CHAR         0x3
            1, //ELEMENT_TYPE_I1           0x4
            1, //ELEMENT_TYPE_U1           0x5
            2, //ELEMENT_TYPE_I2           0x6
            2, //ELEMENT_TYPE_U2           0x7
            4, //ELEMENT_TYPE_I4           0x8
            4, //ELEMENT_TYPE_U4           0x9
            8, //ELEMENT_TYPE_I8           0xa
            8, //ELEMENT_TYPE_U8           0xb
            4, //ELEMENT_TYPE_R4           0xc
            8, //ELEMENT_TYPE_R8           0xd
            -2,//ELEMENT_TYPE_STRING       0xe
            -2,//ELEMENT_TYPE_PTR          0xf
            -2,//ELEMENT_TYPE_BYREF        0x10
            -1,//ELEMENT_TYPE_VALUETYPE    0x11
            -2,//ELEMENT_TYPE_CLASS        0x12
            0, //ELEMENT_TYPE_VAR          0x13
            -2,//ELEMENT_TYPE_ARRAY        0x14
            0, //ELEMENT_TYPE_GENERICINST  0x15
            0, //ELEMENT_TYPE_TYPEDBYREF   0x16
            0, // UNUSED                   0x17
            -2,//ELEMENT_TYPE_I            0x18
            -2,//ELEMENT_TYPE_U            0x19
            0, // UNUSED                   0x1a
            -2,//ELEMENT_TYPE_FPTR         0x1b
            -2,//ELEMENT_TYPE_OBJECT       0x1c
            -2,//ELEMENT_TYPE_SZARRAY      0x1d
        };

        static int GetElemSize(CorElementType t, ITypeHandle thValueType)
        {
            if (((int)t) <= 0x1d)
            {
                int elemSize = s_elemSizes[(int)t];
                if (elemSize == -1)
                {
                    return thValueType.GetSize();
                }
                if (elemSize == -2)
                {
                    return thValueType.PointerSize;
                }
                return elemSize;
            }
            return 0;
        }
    }
}
