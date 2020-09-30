// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides an abstraction over platform specific calling conventions (specifically, the calling convention
// utilized by the JIT on that platform). The caller enumerates each argument of a signature in turn, and is 
// provided with information mapping that argument into registers and/or stack locations.

using System;
using System.Diagnostics;

using Internal.TypeSystem;
using Internal.CorConstants;
using Internal.JitInterface;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    internal abstract class TransitionBlock
    {
        public static TransitionBlock FromTarget(TargetDetails target)
        {
            switch (target.Architecture)
            {
                case TargetArchitecture.X86:
                    return X86TransitionBlock.Instance;

                case TargetArchitecture.X64:
                    return target.OperatingSystem == TargetOS.Windows ?
                        X64WindowsTransitionBlock.Instance :
                        X64UnixTransitionBlock.Instance;

                case TargetArchitecture.ARM:
                    return Arm32TransitionBlock.Instance;

                case TargetArchitecture.ARM64:
                    return Arm64TransitionBlock.Instance;

                default:
                    throw new NotImplementedException(target.Architecture.ToString());
            }
        }

        public const int MaxArgSize = 0xFFFFFF;

        // Unix AMD64 ABI: Special offset value to represent  struct passed in registers. Such a struct can span both
        // general purpose and floating point registers, so it can have two different offsets.
        public const int StructInRegsOffset = -2;

        public abstract TargetArchitecture Architecture { get; }

        public bool IsX86 => Architecture == TargetArchitecture.X86;
        public bool IsX64 => Architecture == TargetArchitecture.X64;
        public bool IsARM => Architecture == TargetArchitecture.ARM;
        public bool IsARM64 => Architecture == TargetArchitecture.ARM64;

        /// <summary>
        /// This property is only overridden in AMD64 Unix variant of the transition block.
        /// </summary>
        public virtual bool IsX64UnixABI => false;

        public abstract int PointerSize { get; }

        public int StackElemSize() => PointerSize;

        public int StackElemSize(int size) => (((size) + StackElemSize() - 1) & -StackElemSize());

        public abstract int NumArgumentRegisters { get; }

        public int SizeOfArgumentRegisters => NumArgumentRegisters * PointerSize;

        public abstract int NumCalleeSavedRegisters { get; }

        public int SizeOfCalleeSavedRegisters => NumCalleeSavedRegisters * PointerSize;

        public abstract int SizeOfTransitionBlock { get; }

        public abstract int OffsetOfArgumentRegisters { get; }

        /// <summary>
        /// The offset of the first slot in a GC ref map. Overridden on ARM64 to return the offset of the X8 register.
        /// </summary>
        public virtual int OffsetOfFirstGCRefMapSlot => OffsetOfArgumentRegisters;

        public abstract int OffsetOfFloatArgumentRegisters { get; }

        public bool IsFloatArgumentRegisterOffset(int offset) => offset < 0;

        public abstract int EnregisteredParamTypeMaxSize { get; }

        public abstract int EnregisteredReturnTypeIntegerMaxSize { get; }

        public abstract int GetRetBuffArgOffset(bool hasThis);

        /// <summary>
        /// Only overridden on ARM64 to return false.
        /// </summary>
        public virtual bool IsRetBuffPassedAsFirstArg => true;

        /// <summary>
        /// Default implementation of ThisOffset; X86TransitionBlock provides a slightly different implementation.
        /// </summary>
        public virtual int ThisOffset { get { return OffsetOfArgumentRegisters;  } }

        /// <summary>
        /// Recalculate pos in GC ref map to actual offset. This is the default implementation for all architectures
        /// except for X86 where it's overridden to supply a more complex algorithm.
        /// </summary>
        public virtual int OffsetFromGCRefMapPos(int pos)
        {
            return OffsetOfFirstGCRefMapSlot + pos * PointerSize;
        }

        /// <summary>
        /// The transition block should define everything pushed by callee. The code assumes in number of places that
        /// end of the transition block is caller's stack pointer.
        /// </summary>
        public int OffsetOfArgs => SizeOfTransitionBlock;

        public bool IsStackArgumentOffset(int offset)
        {
            int ofsArgRegs = OffsetOfArgumentRegisters;

            return offset >= (int)(ofsArgRegs + SizeOfArgumentRegisters);
        }

        public bool IsArgumentRegisterOffset(int offset)
        {
            int ofsArgRegs = OffsetOfArgumentRegisters;

            return offset >= ofsArgRegs && offset < (int)(ofsArgRegs + SizeOfArgumentRegisters);
        }

        public int GetArgumentIndexFromOffset(int offset)
        {
            Debug.Assert(!IsX86);
            return ((offset - OffsetOfArgumentRegisters) / PointerSize);
        }

        public int GetStackArgumentIndexFromOffset(int offset)
        {
            Debug.Assert(!IsX86);
            return (offset - OffsetOfArgs) / StackElemSize();
        }

        /// <summary>
        /// X86: Indicates whether an argument is to be put in a register using the
        /// default IL calling convention. This should be called on each parameter
        /// in the order it appears in the call signature. For a non-static meethod,
        /// this function should also be called once for the "this" argument, prior
        /// to calling it for the "real" arguments. Pass in a typ of ELEMENT_TYPE_CLASS.
        /// </summary>
        /// <param name="pNumRegistersUsed">
        /// keeps track of the number of argument registers assigned previously. 
        /// The caller should initialize this variable to 0 - then each call will update it.
        /// </param>
        /// <param name="typ">parameter type</param>
        /// <param name="thArgType">Exact type info is used to check struct enregistration</param>
        public bool IsArgumentInRegister(ref int pNumRegistersUsed, CorElementType typ, TypeHandle thArgType)
        {
            Debug.Assert(IsX86);

            //        LIMITED_METHOD_CONTRACT;
            if (pNumRegistersUsed < NumArgumentRegisters)
            {
                switch (typ)
                {
                    case CorElementType.ELEMENT_TYPE_BOOLEAN:
                    case CorElementType.ELEMENT_TYPE_CHAR:
                    case CorElementType.ELEMENT_TYPE_I1:
                    case CorElementType.ELEMENT_TYPE_U1:
                    case CorElementType.ELEMENT_TYPE_I2:
                    case CorElementType.ELEMENT_TYPE_U2:
                    case CorElementType.ELEMENT_TYPE_I4:
                    case CorElementType.ELEMENT_TYPE_U4:
                    case CorElementType.ELEMENT_TYPE_STRING:
                    case CorElementType.ELEMENT_TYPE_PTR:
                    case CorElementType.ELEMENT_TYPE_BYREF:
                    case CorElementType.ELEMENT_TYPE_CLASS:
                    case CorElementType.ELEMENT_TYPE_ARRAY:
                    case CorElementType.ELEMENT_TYPE_I:
                    case CorElementType.ELEMENT_TYPE_U:
                    case CorElementType.ELEMENT_TYPE_FNPTR:
                    case CorElementType.ELEMENT_TYPE_OBJECT:
                    case CorElementType.ELEMENT_TYPE_SZARRAY:
                        pNumRegistersUsed++;
                        return true;
#if PROJECTN
                    case CorElementType.ELEMENT_TYPE_VALUETYPE:
                        {
                            // On ProjectN valuetypes of integral size are passed enregistered
                            int structSize = TypeHandle.GetElemSize(typ, thArgType);
                            switch (structSize)
                            {
                                case 1:
                                case 2:
                                case 4:
                                    pNumRegistersUsed++;
                                    return true;
                            }
                            break;
                        }
#endif
                }
            }

            return false;
        }

        /// <summary>
        /// This overload should only be used in AMD64-specific code only.
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public bool IsArgPassedByRef(int size)
        {
            Debug.Assert(IsX64);
            //        LIMITED_METHOD_CONTRACT;

            // If the size is bigger than ENREGISTERED_PARAM_TYPE_MAXSIZE, or if the size is NOT a power of 2, then
            // the argument is passed by reference.
            return (size > EnregisteredParamTypeMaxSize) || ((size & (size - 1)) != 0);
        }

        /// <summary>
        /// Check whether an arg is automatically switched to passing by reference.
        /// Note that this overload does not handle varargs. This method only works for 
        /// valuetypes - true value types, primitives, enums and TypedReference.
        /// The method is only overridden to do something meaningful on X64 and ARM64.
        /// </summary>
        /// <param name="th">Type to analyze</param>
        public virtual bool IsArgPassedByRef(TypeHandle th)
        {
            throw new NotImplementedException(Architecture.ToString());
        }

        /// <summary>
        /// This overload should be used for varargs only. The default implementation
        /// is only overridden on X64.
        /// </summary>
        /// <param name="size">Byte size of the argument</param>
        public virtual bool IsVarArgPassedByRef(int size)
        {
            return size > EnregisteredParamTypeMaxSize;
        }

        public void ComputeReturnValueTreatment(CorElementType type, TypeHandle thRetType, bool isVarArgMethod, out bool usesRetBuffer, out uint fpReturnSize)
        {
            usesRetBuffer = false;
            fpReturnSize = 0;

            switch (type)
            {
                case CorElementType.ELEMENT_TYPE_TYPEDBYREF:
                    throw new NotSupportedException();

                case CorElementType.ELEMENT_TYPE_R4:
                    fpReturnSize = sizeof(float);
                    break;

                case CorElementType.ELEMENT_TYPE_R8:
                    fpReturnSize = sizeof(double);
                    break;

                case CorElementType.ELEMENT_TYPE_VALUETYPE:
                    {
                        Debug.Assert(!thRetType.IsNull() && thRetType.IsValueType());

                        if ((Architecture == TargetArchitecture.X64) && IsX64UnixABI)
                        {
                            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor;
                            SystemVStructClassificator.GetSystemVAmd64PassStructInRegisterDescriptor(thRetType.GetRuntimeTypeHandle(), out descriptor);

                            if (descriptor.passedInRegisters)
                            {
                                if (descriptor.eightByteCount == 1)
                                {
                                    if (descriptor.eightByteClassifications0 == SystemVClassificationType.SystemVClassificationTypeSSE)
                                    {
                                        // Structs occupying just one eightbyte are treated as int / double
                                        fpReturnSize = sizeof(double);
                                    }
                                }
                                else
                                {
                                    // Size of the struct is 16 bytes
                                    fpReturnSize = 16;
                                    // The lowest two bits of the size encode the order of the int and SSE fields
                                    if (descriptor.eightByteClassifications0 == SystemVClassificationType.SystemVClassificationTypeSSE)
                                    {
                                        fpReturnSize += 1;
                                    }

                                    if (descriptor.eightByteClassifications0 == SystemVClassificationType.SystemVClassificationTypeSSE)
                                    {
                                        fpReturnSize += 2;
                                    }
                                }

                                break;
                            }
                        }
                        else
                        {
                            if (thRetType.IsHomogeneousAggregate() && !isVarArgMethod)
                            {
                                int haElementSize = thRetType.GetHomogeneousAggregateElementSize();
                                fpReturnSize = 4 * (uint)haElementSize;
                                break;
                            }

                            uint size = (uint)thRetType.GetSize();

                            if (IsX86 || IsX64)
                            {
                                // Return value types of size which are not powers of 2 using a RetBuffArg
                                if ((size & (size - 1)) != 0)
                                {
                                    usesRetBuffer = true;
                                    break;
                                }
                            }

                            if (size <= EnregisteredReturnTypeIntegerMaxSize)
                                break;
                        }
                    }

                    // Value types are returned using return buffer by default
                    usesRetBuffer = true;
                    break;

                default:
                    break;
            }
        }

        public const int InvalidOffset = -1;

        public sealed class X86Constants
        {
            public const int OffsetOfEcx = 1 * sizeof(int);
            public const int OffsetOfEdx = 0 * sizeof(int);
        }

        private sealed class X86TransitionBlock : TransitionBlock
        {
            public static TransitionBlock Instance = new X86TransitionBlock();

            public override TargetArchitecture Architecture => TargetArchitecture.X86;

            public override int PointerSize => 4;

            public override int NumArgumentRegisters => 2;
            public override int NumCalleeSavedRegisters => 4;
            // Argument registers, callee-save registers, return address
            public override int SizeOfTransitionBlock => SizeOfArgumentRegisters + SizeOfCalleeSavedRegisters + PointerSize;
            public override int OffsetOfArgumentRegisters => 0;
            // CALLDESCR_FPARGREGS is not set for X86
            public override int OffsetOfFloatArgumentRegisters => 0;
            // offsetof(ArgumentRegisters.ECX)
            public override int ThisOffset => X86Constants.OffsetOfEcx;
            public override int EnregisteredParamTypeMaxSize => 0;
            public override int EnregisteredReturnTypeIntegerMaxSize => 4;

            public override int OffsetFromGCRefMapPos(int pos)
            {
                if (pos < NumArgumentRegisters)
                {
                    return OffsetOfArgumentRegisters + SizeOfArgumentRegisters - (pos + 1) * PointerSize;
                }
                else
                {
                    return OffsetOfArgs + (pos - NumArgumentRegisters) * PointerSize;
                }
            }

            public override bool IsArgPassedByRef(TypeHandle th) => false;

            /// <summary>
            /// x86 is special as always
            /// </summary>
            public override int GetRetBuffArgOffset(bool hasThis)
            {
#if PROJECTN
                return OffsetOfArgs;
#else
                return hasThis ? X86Constants.OffsetOfEdx : X86Constants.OffsetOfEcx;
#endif
            }
        }

        public const int SizeOfM128A = 16;

        /// <summary>
        /// X64 properties common to Windows and Unix ABI.
        /// </summary>
        internal abstract class X64TransitionBlock : TransitionBlock
        {
            public override TargetArchitecture Architecture => TargetArchitecture.X64;
            public override int PointerSize => 8;

            public override bool IsArgPassedByRef(TypeHandle th)
            {
                Debug.Assert(!th.IsNull());
                Debug.Assert(th.IsValueType());
                return IsArgPassedByRef((int)th.GetSize());
            }

            public override bool IsVarArgPassedByRef(int size)
            {
                return IsArgPassedByRef(size);
            }

            public override int GetRetBuffArgOffset(bool hasThis) => OffsetOfArgumentRegisters + (hasThis ? PointerSize : 0);
        }

        private sealed class X64WindowsTransitionBlock : X64TransitionBlock
        {
            public static TransitionBlock Instance = new X64WindowsTransitionBlock();

            // RCX, RDX, R8, R9
            public override int NumArgumentRegisters => 4;
            // RDI, RSI, RBX, RBP, R12, R13, R14, R15
            public override int NumCalleeSavedRegisters => 8;
            // Callee-saved registers, return address
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + PointerSize;
            public override int OffsetOfArgumentRegisters => SizeOfTransitionBlock;
            // CALLDESCR_FPARGREGS is not set for Amd64 on 
            public override int OffsetOfFloatArgumentRegisters => 0;
            public override int EnregisteredParamTypeMaxSize => 8;
            public override int EnregisteredReturnTypeIntegerMaxSize => 8;
        }

        internal sealed class X64UnixTransitionBlock : X64TransitionBlock
        {
            public static readonly TransitionBlock Instance = new X64UnixTransitionBlock();

            public override bool IsX64UnixABI => true;

            public const int NUM_FLOAT_ARGUMENT_REGISTERS = 8;

            // RDI, RSI, RDX, RCX, R8, R9
            public override int NumArgumentRegisters => 6;
            // R12, R13, R14, R15, RBX, RBP
            public override int NumCalleeSavedRegisters => 6;
            // Argument registers, callee-saved registers, return address
            public override int SizeOfTransitionBlock => SizeOfArgumentRegisters + SizeOfCalleeSavedRegisters + PointerSize;
            public override int OffsetOfArgumentRegisters => 0;
            public override int OffsetOfFloatArgumentRegisters => SizeOfM128A * NUM_FLOAT_ARGUMENT_REGISTERS;
            public override int EnregisteredParamTypeMaxSize => 16;
            public override int EnregisteredReturnTypeIntegerMaxSize => 16;
            public override bool IsArgPassedByRef(TypeHandle th) => false;
        }

        private sealed class Arm32TransitionBlock : TransitionBlock
        {
            public static TransitionBlock Instance = new Arm32TransitionBlock();

            public override TargetArchitecture Architecture => TargetArchitecture.ARM;
            public override int PointerSize => 4;
            // R0, R1, R2, R3
            public override int NumArgumentRegisters => 4;
            // R4, R5, R6, R7, R8, R9, R10, R11, R14
            public override int NumCalleeSavedRegisters => 9;
            // Callee-saves, argument registers
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + SizeOfArgumentRegisters;
            public override int OffsetOfArgumentRegisters => SizeOfCalleeSavedRegisters;
            // D0..D7
            public override int OffsetOfFloatArgumentRegisters => 8 * sizeof(double) + PointerSize;
            public override int EnregisteredParamTypeMaxSize => 0;
            public override int EnregisteredReturnTypeIntegerMaxSize => 4;

            public override bool IsArgPassedByRef(TypeHandle th) => false;

            public override int GetRetBuffArgOffset(bool hasThis) => OffsetOfArgumentRegisters + (hasThis ? PointerSize : 0);
        }

        private sealed class Arm64TransitionBlock : TransitionBlock
        {
            public static TransitionBlock Instance = new Arm64TransitionBlock();

            public override TargetArchitecture Architecture => TargetArchitecture.ARM64;
            public override int PointerSize => 8;
            // X0 .. X7
            public override int NumArgumentRegisters => 8;
            // X29, X30, X19, X20, X21, X22, X23, X24, X25, X26, X27, X28
            public override int NumCalleeSavedRegisters => 12;
            // Callee-saves, padding, m_x8RetBuffReg, argument registers
            public override int SizeOfTransitionBlock => SizeOfCalleeSavedRegisters + 2 * PointerSize + SizeOfArgumentRegisters;
            public override int OffsetOfArgumentRegisters => SizeOfCalleeSavedRegisters + 2 * PointerSize;
            private int OffsetOfX8Register => OffsetOfArgumentRegisters - PointerSize;
            public override int OffsetOfFirstGCRefMapSlot => OffsetOfX8Register;

            // D0..D7
            public override int OffsetOfFloatArgumentRegisters => 8 * sizeof(double) + PointerSize;
            public override int EnregisteredParamTypeMaxSize => 16;
            public override int EnregisteredReturnTypeIntegerMaxSize => 16;

            public override bool IsArgPassedByRef(TypeHandle th)
            {
                Debug.Assert(!th.IsNull());
                Debug.Assert(th.IsValueType());

                // Composites greater than 16 bytes are passed by reference
                return (th.GetSize() > EnregisteredParamTypeMaxSize) && !th.IsHomogeneousAggregate();
            }

            public override int GetRetBuffArgOffset(bool hasThis) => OffsetOfX8Register;

            public override bool IsRetBuffPassedAsFirstArg => false;
        }
    }
}
