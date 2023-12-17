// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// This file is a line by line port of callingconvention.h from the CLR with the intention that we may wish to merge
// changes from the CLR in at a later time. As such, the normal coding conventions are ignored.
//

//
#if TARGET_ARM
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                        // CallDescrWorker has FloatArgumentRegisters parameter
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define FEATURE_HFA
#elif TARGET_ARM64
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#define CALLDESCR_FPARGREGS                        // CallDescrWorker has FloatArgumentRegisters parameter
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define ENREGISTERED_PARAMTYPE_MAXSIZE
#define FEATURE_HFA
#elif TARGET_X86
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#elif TARGET_AMD64
#if TARGET_UNIX
#define UNIX_AMD64_ABI
#define CALLDESCR_ARGREGS                          // CallDescrWorker has ArgumentRegister parameter
#else
#endif
#define CALLDESCR_FPARGREGS                        // CallDescrWorker has FloatArgumentRegisters parameter
#define ENREGISTERED_RETURNTYPE_MAXSIZE
#define ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE
#define ENREGISTERED_PARAMTYPE_MAXSIZE
#elif TARGET_WASM
#else
#error Unknown architecture!
#endif

// Provides an abstraction over platform specific calling conventions (specifically, the calling convention
// utilized by the JIT on that platform). The caller enumerates each argument of a signature in turn, and is
// provided with information mapping that argument into registers and/or stack locations.

using System;
using System.Runtime.InteropServices;

namespace Internal.Runtime
{
#if TARGET_AMD64
#if UNIX_AMD64_ABI
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnBlock
    {
        private IntPtr returnValue;
        private IntPtr returnValue2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArgumentRegisters
    {
        private IntPtr rdi;
        private IntPtr rsi;
        private IntPtr rdx;
        private IntPtr rcx;
        private IntPtr r8;
        private IntPtr r9;
    }
#else // UNIX_AMD64_ABI
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnBlock
    {
        private IntPtr returnValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArgumentRegisters
    {
        private IntPtr rdx;
        private IntPtr rcx;
        private IntPtr r8;
        private IntPtr r9;
    }
#endif // UNIX_AMD64_ABI
    [StructLayout(LayoutKind.Sequential)]
    internal struct M128A
    {
        private IntPtr a;
        private IntPtr b;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct FloatArgumentRegisters
    {
        private M128A d0;
        private M128A d1;
        private M128A d2;
        private M128A d3;
#if UNIX_AMD64_ABI
        private M128A d4;
        private M128A d5;
        private M128A d6;
        private M128A d7;
#endif
    }

    internal struct ArchitectureConstants
    {
        // To avoid corner case bugs, limit maximum size of the arguments with sufficient margin
        public const int MAX_ARG_SIZE = 0xFFFFFF;

#if UNIX_AMD64_ABI
        public const int NUM_ARGUMENT_REGISTERS = 6;
#else
        public const int NUM_ARGUMENT_REGISTERS = 4;
#endif
        public const int ARGUMENTREGISTERS_SIZE = NUM_ARGUMENT_REGISTERS * 8;
        public const int ENREGISTERED_RETURNTYPE_MAXSIZE = 8;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE = 8;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE_PRIMITIVE = 8;
        public const int ENREGISTERED_PARAMTYPE_MAXSIZE = 8;
        public const int STACK_ELEM_SIZE = 8;
        public static int StackElemSize(int size) { return (((size) + STACK_ELEM_SIZE - 1) & ~(STACK_ELEM_SIZE - 1)); }
    }
#elif TARGET_ARM64
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnBlock
    {
        private IntPtr returnValue;
        private IntPtr returnValue2;
        private IntPtr returnValue3;
        private IntPtr returnValue4;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArgumentRegisters
    {
        private IntPtr x0;
        private IntPtr x1;
        private IntPtr x2;
        private IntPtr x3;
        private IntPtr x4;
        private IntPtr x5;
        private IntPtr x6;
        private IntPtr x7;
        private IntPtr x8;
        public static unsafe int GetOffsetOfx8()
        {
            return sizeof(IntPtr) * 8;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FloatArgumentRegisters
    {
        private double d0;
        private double d1;
        private double d2;
        private double d3;
        private double d4;
        private double d5;
        private double d6;
        private double d7;
    }

    internal struct ArchitectureConstants
    {
        // To avoid corner case bugs, limit maximum size of the arguments with sufficient margin
        public const int MAX_ARG_SIZE = 0xFFFFFF;

        public const int NUM_ARGUMENT_REGISTERS = 8;
        public const int ARGUMENTREGISTERS_SIZE = NUM_ARGUMENT_REGISTERS * 8;
        public const int ENREGISTERED_RETURNTYPE_MAXSIZE = 32;                  // bytes (four FP registers: d0,d1,d2 and d3)
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE = 16;          // bytes (two int registers: x0 and x1)
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE_PRIMITIVE = 8;
        public const int ENREGISTERED_PARAMTYPE_MAXSIZE = 16;                   // bytes (max value type size that can be passed by value)
        public const int STACK_ELEM_SIZE = 8;
        public static int StackElemSize(int size) { return (((size) + STACK_ELEM_SIZE - 1) & ~(STACK_ELEM_SIZE - 1)); }
    }
#elif TARGET_X86
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnBlock
    {
        public IntPtr returnValue;
        public IntPtr returnValue2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArgumentRegisters
    {
        public IntPtr edx;
        public static unsafe int GetOffsetOfEdx()
        {
            return 0;
        }
        public IntPtr ecx;
        public static unsafe int GetOffsetOfEcx()
        {
            return sizeof(IntPtr);
        }
    }
    // This struct isn't used by x86, but exists for compatibility with the definition of the CallDescrData struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct FloatArgumentRegisters
    {
    }

    internal struct ArchitectureConstants
    {
        // To avoid corner case bugs, limit maximum size of the arguments with sufficient margin
        public const int MAX_ARG_SIZE = 0xFFFFFF;

        public const int NUM_ARGUMENT_REGISTERS = 2;
        public const int ARGUMENTREGISTERS_SIZE = NUM_ARGUMENT_REGISTERS * 4;
        public const int ENREGISTERED_RETURNTYPE_MAXSIZE = 8;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE = 4;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE_PRIMITIVE = 4;
        public const int STACK_ELEM_SIZE = 4;
        public static int StackElemSize(int size) { return (((size) + STACK_ELEM_SIZE - 1) & ~(STACK_ELEM_SIZE - 1)); }
    }
#elif TARGET_ARM
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnBlock
    {
        private IntPtr returnValue;
        private IntPtr returnValue2;
        private IntPtr returnValue3;
        private IntPtr returnValue4;
        private IntPtr returnValue5;
        private IntPtr returnValue6;
        private IntPtr returnValue7;
        private IntPtr returnValue8;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArgumentRegisters
    {
        private IntPtr r0;
        private IntPtr r1;
        private IntPtr r2;
        private IntPtr r3;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FloatArgumentRegisters
    {
        private double d0;
        private double d1;
        private double d2;
        private double d3;
        private double d4;
        private double d5;
        private double d6;
        private double d7;
    }

    internal struct ArchitectureConstants
    {
        // To avoid corner case bugs, limit maximum size of the arguments with sufficient margin
        public const int MAX_ARG_SIZE = 0xFFFFFF;

        public const int NUM_ARGUMENT_REGISTERS = 4;
        public const int ARGUMENTREGISTERS_SIZE = NUM_ARGUMENT_REGISTERS * 4;
        public const int ENREGISTERED_RETURNTYPE_MAXSIZE = 32;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE = 4;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE_PRIMITIVE = 8;
        public const int STACK_ELEM_SIZE = 4;
        public static int StackElemSize(int size) { return (((size) + STACK_ELEM_SIZE - 1) & ~(STACK_ELEM_SIZE - 1)); }
    }

#elif TARGET_WASM
    [StructLayout(LayoutKind.Sequential)]
    internal struct ReturnBlock
    {
        private IntPtr returnValue;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ArgumentRegisters
    {
        // No registers on WASM
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FloatArgumentRegisters
    {
        // No registers on WASM
    }

    internal struct ArchitectureConstants
    {
        // To avoid corner case bugs, limit maximum size of the arguments with sufficient margin
        public const int MAX_ARG_SIZE = 0xFFFFFF;

        public const int NUM_ARGUMENT_REGISTERS = 0;
        public const int ARGUMENTREGISTERS_SIZE = NUM_ARGUMENT_REGISTERS * 4;
        public const int ENREGISTERED_RETURNTYPE_MAXSIZE = 32;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE = 4;
        public const int ENREGISTERED_RETURNTYPE_INTEGER_MAXSIZE_PRIMITIVE = 8;
        public const int STACK_ELEM_SIZE = 4;
        public static int StackElemSize(int size) { return (((size) + STACK_ELEM_SIZE - 1) & ~(STACK_ELEM_SIZE - 1)); }
    }
#endif

    //
    // TransitionBlock is layout of stack frame of method call, saved argument registers and saved callee saved registers. Even though not
    // all fields are used all the time, we use uniform form for simplicity.
    //
    [StructLayout(LayoutKind.Sequential)]
    internal struct TransitionBlock
    {
#if TARGET_X86
        public ArgumentRegisters m_argumentRegisters;
        public static unsafe int GetOffsetOfArgumentRegisters()
        {
            return 0;
        }
        public ReturnBlock m_returnBlock;
        public static unsafe int GetOffsetOfReturnValuesBlock()
        {
            return sizeof(ArgumentRegisters);
        }
        private IntPtr m_ebp;
        private IntPtr m_ReturnAddress;
#elif TARGET_AMD64

#if UNIX_AMD64_ABI
        public ReturnBlock m_returnBlock;
        public static unsafe int GetOffsetOfReturnValuesBlock()
        {
            return 0;
        }

        public ArgumentRegisters m_argumentRegisters;
        public static unsafe int GetOffsetOfArgumentRegisters()
        {
            return sizeof(ReturnBlock);
        }

        private IntPtr m_alignmentPadding;
        private IntPtr m_ReturnAddress;
#else // UNIX_AMD64_ABI
        private IntPtr m_returnBlockPadding;
        public ReturnBlock m_returnBlock;
        public static unsafe int GetOffsetOfReturnValuesBlock()
        {
            return sizeof(IntPtr);
        }
        private IntPtr m_alignmentPadding;
        private IntPtr m_ReturnAddress;
        public static unsafe int GetOffsetOfArgumentRegisters()
        {
            return sizeof(TransitionBlock);
        }
#endif // UNIX_AMD64_ABI

#elif TARGET_ARM
        public ReturnBlock m_returnBlock;
        public static unsafe int GetOffsetOfReturnValuesBlock()
        {
            return 0;
        }

        public ArgumentRegisters m_argumentRegisters;
        public static unsafe int GetOffsetOfArgumentRegisters()
        {
            return sizeof(ReturnBlock);
        }
#elif TARGET_ARM64
        public ReturnBlock m_returnBlock;
        public static unsafe int GetOffsetOfReturnValuesBlock()
        {
            return 0;
        }

        public ArgumentRegisters m_argumentRegisters;
        public static unsafe int GetOffsetOfArgumentRegisters()
        {
            return sizeof(ReturnBlock);
        }

        public IntPtr m_alignmentPad;
#elif TARGET_WASM
        public ReturnBlock m_returnBlock;
        public static unsafe int GetOffsetOfReturnValuesBlock()
        {
            return 0;
        }

        public ArgumentRegisters m_argumentRegisters;
        public static unsafe int GetOffsetOfArgumentRegisters()
        {
            return sizeof(ReturnBlock);
        }
#else
#error Portability problem
#endif

        // The transition block should define everything pushed by callee. The code assumes in number of places that
        // end of the transition block is caller's stack pointer.

        public static unsafe byte GetOffsetOfArgs()
        {
            return (byte)sizeof(TransitionBlock);
        }


        public static bool IsStackArgumentOffset(int offset)
        {
            int ofsArgRegs = GetOffsetOfArgumentRegisters();

            return offset >= (int)(ofsArgRegs + ArchitectureConstants.ARGUMENTREGISTERS_SIZE);
        }

        public static bool IsArgumentRegisterOffset(int offset)
        {
            int ofsArgRegs = GetOffsetOfArgumentRegisters();

            return offset >= ofsArgRegs && offset < (int)(ofsArgRegs + ArchitectureConstants.ARGUMENTREGISTERS_SIZE);
        }

#if !TARGET_X86
        public static unsafe int GetArgumentIndexFromOffset(int offset)
        {
            return ((offset - GetOffsetOfArgumentRegisters()) / IntPtr.Size);
        }

        public static int GetStackArgumentIndexFromOffset(int offset)
        {
            return (offset - GetOffsetOfArgs()) / ArchitectureConstants.STACK_ELEM_SIZE;
        }
#endif

#if CALLDESCR_FPARGREGS
        public static bool IsFloatArgumentRegisterOffset(int offset)
        {
            return offset < 0;
        }

        public static int GetOffsetOfFloatArgumentRegisters()
        {
            return -GetNegSpaceSize();
        }
#endif

        public static unsafe int GetNegSpaceSize()
        {
            int negSpaceSize = 0;
#if CALLDESCR_FPARGREGS
            negSpaceSize += sizeof(FloatArgumentRegisters);
#endif
            return negSpaceSize;
        }

        public static int GetThisOffset()
        {
            // This pointer is in the first argument register by default
            int ret = TransitionBlock.GetOffsetOfArgumentRegisters();

#if TARGET_X86
            // x86 is special as always
            ret += ArgumentRegisters.GetOffsetOfEcx();
#endif

            return ret;
        }

        public const int InvalidOffset = -1;
    };
}
