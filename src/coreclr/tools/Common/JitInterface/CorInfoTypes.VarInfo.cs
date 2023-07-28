// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

//
// The types in this file are referenced directly from ILCompiler.Compiler
// so cannot be easily factored out into ILCompiler.RyuJit until we build
// some sort of abstraction.
//

namespace Internal.JitInterface
{
    public enum VarLocType : uint
    {
        VLT_REG,        // variable is in a register
        VLT_REG_BYREF,  // address of the variable is in a register
        VLT_REG_FP,     // variable is in an fp register
        VLT_STK,        // variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_STK_BYREF,  // address of the variable is on the stack (memory addressed relative to the frame-pointer)
        VLT_REG_REG,    // variable lives in two registers
        VLT_REG_STK,    // variable lives partly in a register and partly on the stack
        VLT_STK_REG,    // reverse of VLT_REG_STK
        VLT_STK2,       // variable lives in two slots on the stack
        VLT_FPSTK,      // variable lives on the floating-point stack
        VLT_FIXED_VA,   // variable is a fixed argument in a varargs function (relative to VARARGS_HANDLE)

        VLT_COUNT,
        VLT_INVALID
    };

    public struct NativeVarInfo
    {
        public uint startOffset;
        public uint endOffset;
        public uint varNumber;
        public VarLoc varLoc;
    };

    // The following 16 bytes come from coreclr types. See comment below.
    [StructLayout(LayoutKind.Sequential)]
    public struct VarLoc
    {
        public IntPtr A; // vlType + padding
        public int B;
        public int C;
        public int D;

        public VarLocType LocationType => (VarLocType)(A.ToInt64() & 0xFFFFFFFF);

        /*
           Changes to the following types may require revisiting the above layout.

            In coreclr\inc\cordebuginfo.h

            enum VarLocType
            {
                VLT_REG,        // variable is in a register
                VLT_REG_BYREF,  // address of the variable is in a register
                VLT_REG_FP,     // variable is in an fp register
                VLT_STK,        // variable is on the stack (memory addressed relative to the frame-pointer)
                VLT_STK_BYREF,  // address of the variable is on the stack (memory addressed relative to the frame-pointer)
                VLT_REG_REG,    // variable lives in two registers
                VLT_REG_STK,    // variable lives partly in a register and partly on the stack
                VLT_STK_REG,    // reverse of VLT_REG_STK
                VLT_STK2,       // variable lives in two slots on the stack
                VLT_FPSTK,      // variable lives on the floating-point stack
                VLT_FIXED_VA,   // variable is a fixed argument in a varargs function (relative to VARARGS_HANDLE)

                VLT_COUNT,
                VLT_INVALID,
        #ifdef MDIL
                VLT_MDIL_SYMBOLIC = 0x20
        #endif

            };

            struct VarLoc
            {
                VarLocType      vlType;

                union
                {
                    // VLT_REG/VLT_REG_FP -- Any pointer-sized enregistered value (TYP_INT, TYP_REF, etc)
                    // eg. EAX
                    // VLT_REG_BYREF -- the specified register contains the address of the variable
                    // eg. [EAX]

                    struct
                    {
                        RegNum      vlrReg;
                    } vlReg;

                    // VLT_STK -- Any 32 bit value which is on the stack
                    // eg. [ESP+0x20], or [EBP-0x28]
                    // VLT_STK_BYREF -- the specified stack location contains the address of the variable
                    // eg. mov EAX, [ESP+0x20]; [EAX]

                    struct
                    {
                        RegNum      vlsBaseReg;
                        signed      vlsOffset;
                    } vlStk;

                    // VLT_REG_REG -- TYP_LONG with both DWords enregistred
                    // eg. RBM_EAXEDX

                    struct
                    {
                        RegNum      vlrrReg1;
                        RegNum      vlrrReg2;
                    } vlRegReg;

                    // VLT_REG_STK -- Partly enregistered TYP_LONG
                    // eg { LowerDWord=EAX UpperDWord=[ESP+0x8] }

                    struct
                    {
                        RegNum      vlrsReg;
                        struct
                        {
                            RegNum      vlrssBaseReg;
                            signed      vlrssOffset;
                        }           vlrsStk;
                    } vlRegStk;

                    // VLT_STK_REG -- Partly enregistered TYP_LONG
                    // eg { LowerDWord=[ESP+0x8] UpperDWord=EAX }

                    struct
                    {
                        struct
                        {
                            RegNum      vlsrsBaseReg;
                            signed      vlsrsOffset;
                        }           vlsrStk;
                        RegNum      vlsrReg;
                    } vlStkReg;

                    // VLT_STK2 -- Any 64 bit value which is on the stack,
                    // in 2 successive DWords.
                    // eg 2 DWords at [ESP+0x10]

                    struct
                    {
                        RegNum      vls2BaseReg;
                        signed      vls2Offset;
                    } vlStk2;

                    // VLT_FPSTK -- enregisterd TYP_DOUBLE (on the FP stack)
                    // eg. ST(3). Actually it is ST("FPstkHeight - vpFpStk")

                    struct
                    {
                        unsigned        vlfReg;
                    } vlFPstk;

                    // VLT_FIXED_VA -- fixed argument of a varargs function.
                    // The argument location depends on the size of the variable
                    // arguments (...). Inspecting the VARARGS_HANDLE indicates the
                    // location of the first arg. This argument can then be accessed
                    // relative to the position of the first arg

                    struct
                    {
                        unsigned        vlfvOffset;
                    } vlFixedVarArg;

                    // VLT_MEMORY

                    struct
                    {
                        void        *rpValue; // pointer to the in-process
                        // location of the value.
                    } vlMemory;
                };
            };
        */
    };
}
