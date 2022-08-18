// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides an abstraction over platform specific calling conventions (specifically, the calling convention
// utilized by the JIT on that platform). The caller enumerates each argument of a signature in turn, and is 
// provided with information mapping that argument into registers and/or stack locations.

using System;
using System.Diagnostics;

using Internal.JitInterface;
using Internal.NativeFormat;
using Internal.TypeSystem;
using Internal.CorConstants;


namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public enum CORCOMPILE_GCREFMAP_TOKENS : byte
    {
        GCREFMAP_SKIP = 0,
        GCREFMAP_REF = 1,
        GCREFMAP_INTERIOR = 2,
        GCREFMAP_METHOD_PARAM = 3,
        GCREFMAP_TYPE_PARAM = 4,
        GCREFMAP_VASIG_COOKIE = 5,
    };

    public enum CallingConventions
    {
        ManagedInstance,
        ManagedStatic,
        StdCall,
        /*FastCall, CDecl */
    }

    internal struct TypeHandle
    {
        public TypeHandle(TypeDesc type)
        {
            _type = type;
            _isByRef = _type.IsByRef;
            if (_isByRef)
            {
                _type = ((ByRefType)_type).ParameterType;
            }
        }

        private readonly TypeDesc _type;
        private readonly bool _isByRef;

        public bool Equals(TypeHandle other)
        {
            return _isByRef == other._isByRef && _type == other._type;
        }

        public override int GetHashCode() { return (int)_type.GetHashCode(); }

        public bool IsNull() { return _type == null && !_isByRef; }
        public bool IsValueType() { if (_isByRef) return false; return _type.IsValueType; }
        public bool IsPointerType() { if (_isByRef) return false; return _type.IsPointer; }

        public bool HasIndeterminateSize() { return IsValueType() && ((DefType)_type).InstanceFieldSize.IsIndeterminate; }

        public int PointerSize => _type.Context.Target.PointerSize;

        public int GetSize()
        {
            if (IsValueType())
                return ((DefType)_type).InstanceFieldSize.AsInt;
            else
                return PointerSize;
        }

        public bool RequiresAlign8()
        {
            if (_type.Context.Target.Architecture != TargetArchitecture.ARM)
            {
                return false;
            }
            if (_isByRef)
            {
                return false;
            }
            return _type.RequiresAlign8();
        }

        public bool IsHomogeneousAggregate()
        {
            TargetArchitecture targetArch = _type.Context.Target.Architecture;
            if ((targetArch != TargetArchitecture.ARM) && (targetArch != TargetArchitecture.ARM64))
            {
                return false;
            }
            if (_isByRef)
            {
                return false;
            }
            return _type is DefType defType && defType.IsHomogeneousAggregate;
        }

        public int GetHomogeneousAggregateElementSize()
        {
            Debug.Assert(IsHomogeneousAggregate());
            switch (_type.Context.Target.Architecture)
            {
                case TargetArchitecture.ARM:
                    return RequiresAlign8() ? 8 : 4;

                case TargetArchitecture.ARM64:
                    return ((DefType)_type).GetHomogeneousAggregateElementSize();
            }
            throw new InvalidOperationException();
        }

        public CorElementType GetCorElementType()
        {
            if (_isByRef)
            {
                return CorElementType.ELEMENT_TYPE_BYREF;
            }

            Internal.TypeSystem.TypeFlags category = _type.UnderlyingType.Category;
            // We use the UnderlyingType to handle Enums properly
            return category switch
            {
                Internal.TypeSystem.TypeFlags.Boolean => CorElementType.ELEMENT_TYPE_BOOLEAN,
                Internal.TypeSystem.TypeFlags.Char => CorElementType.ELEMENT_TYPE_CHAR,
                Internal.TypeSystem.TypeFlags.SByte => CorElementType.ELEMENT_TYPE_I1,
                Internal.TypeSystem.TypeFlags.Byte => CorElementType.ELEMENT_TYPE_U1,
                Internal.TypeSystem.TypeFlags.Int16 => CorElementType.ELEMENT_TYPE_I2,
                Internal.TypeSystem.TypeFlags.UInt16 => CorElementType.ELEMENT_TYPE_U2,
                Internal.TypeSystem.TypeFlags.Int32 => CorElementType.ELEMENT_TYPE_I4,
                Internal.TypeSystem.TypeFlags.UInt32 => CorElementType.ELEMENT_TYPE_U4,
                Internal.TypeSystem.TypeFlags.Int64 => CorElementType.ELEMENT_TYPE_I8,
                Internal.TypeSystem.TypeFlags.UInt64 => CorElementType.ELEMENT_TYPE_U8,
                Internal.TypeSystem.TypeFlags.IntPtr => CorElementType.ELEMENT_TYPE_I,
                Internal.TypeSystem.TypeFlags.UIntPtr => CorElementType.ELEMENT_TYPE_U,
                Internal.TypeSystem.TypeFlags.Single => CorElementType.ELEMENT_TYPE_R4,
                Internal.TypeSystem.TypeFlags.Double => CorElementType.ELEMENT_TYPE_R8,
                Internal.TypeSystem.TypeFlags.ValueType => CorElementType.ELEMENT_TYPE_VALUETYPE,
                Internal.TypeSystem.TypeFlags.Nullable => CorElementType.ELEMENT_TYPE_VALUETYPE,
                Internal.TypeSystem.TypeFlags.Void => CorElementType.ELEMENT_TYPE_VOID,
                Internal.TypeSystem.TypeFlags.Pointer => CorElementType.ELEMENT_TYPE_PTR,
                Internal.TypeSystem.TypeFlags.FunctionPointer => CorElementType.ELEMENT_TYPE_FNPTR,

                _ => CorElementType.ELEMENT_TYPE_CLASS
            };
        }

        private static int[] s_elemSizes = new int[]
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

        public static int GetElemSize(CorElementType t, TypeHandle thValueType)
        {
            if (((int)t) <= 0x1d)
            {
                int elemSize = s_elemSizes[(int)t];
                if (elemSize == -1)
                {
                    return (int)thValueType.GetSize();
                }
                if (elemSize == -2)
                {
                    return thValueType.PointerSize;
                }
                return elemSize;
            }
            return 0;
        }

        public TypeDesc GetRuntimeTypeHandle() { return _type; }
    }

    // Describes how a single argument is laid out in registers and/or stack locations when given as an input to a
    // managed method as part of a larger signature.
    //
    // Locations are split into floating point registers, general registers and stack offsets. Registers are
    // obviously architecture dependent but are represented as a zero-based index into the usual sequence in which
    // such registers are allocated for input on the platform in question. For instance:
    //      X86: 0 == ecx, 1 == edx
    //      ARM: 0 == r0, 1 == r1, 2 == r2 etc.
    //
    // Stack locations are represented as offsets from the stack pointer (at the point of the call). The offset is
    // given as an index of a pointer sized slot. Similarly the size of data on the stack is given in slot-sized
    // units. For instance, given an index of 2 and a size of 3:
    //      X86:   argument starts at [ESP + 8] and is 12 bytes long
    //      AMD64: argument starts at [RSP + 16] and is 24 bytes long
    //
    // The structure is flexible enough to describe an argument that is split over several (consecutive) registers
    // and possibly on to the stack as well.
    internal struct ArgLocDesc
    {
        public int m_idxFloatReg;  // First floating point register used (or -1)
        public int m_cFloatReg;    // Count of floating point registers used (or 0)

        public int m_idxGenReg;    // First general register used (or -1)
        public short m_cGenReg;      // Count of general registers used (or 0)

        public bool m_fRequires64BitAlignment;  // ARM - True if the argument should always be aligned (in registers or on the stack

        public int m_byteStackIndex;     // Stack offset in bytes (or -1)
        public int m_byteStackSize;      // Stack size in bytes

        public uint m_floatFlags;        // struct with two-fields can be passed by registers.
        // Initialize to represent a non-placed argument (no register or stack slots referenced).
        public void Init()
        {
            m_idxFloatReg = -1;
            m_cFloatReg = 0;
            m_idxGenReg = -1;
            m_cGenReg = 0;
            m_byteStackIndex = -1;
            m_byteStackSize = 0;
            m_floatFlags = 0;

            m_fRequires64BitAlignment = false;
        }
    };

    // The ArgDestination class represents a destination location of an argument.
    internal readonly struct ArgDestination
    {
        /// <summary>
        /// Transition block context.
        /// </summary>
        private readonly TransitionBlock _transitionBlock;

        // Offset of the argument relative to the base. On AMD64 on Unix, it can have a special
        // value that represent a struct that contain both general purpose and floating point fields 
        // passed in registers.
        private readonly int _offset;

        // For structs passed in registers, this member points to an ArgLocDesc that contains
        // details on the layout of the struct in general purpose and floating point registers.
        private readonly ArgLocDesc? _argLocDescForStructInRegs;

        // Construct the ArgDestination
        public ArgDestination(TransitionBlock transitionBlock, int offset, ArgLocDesc? argLocDescForStructInRegs)
        {
            _transitionBlock = transitionBlock;
            _offset = offset;
            _argLocDescForStructInRegs = argLocDescForStructInRegs;
        }

        public void GcMark(CORCOMPILE_GCREFMAP_TOKENS[] frame, int delta, bool interior)
        {
            frame[_offset + delta] = interior ? CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_INTERIOR : CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_REF;
        }

        // Returns true if the ArgDestination represents a homogeneous aggregate struct
        bool IsHomogeneousAggregate()
        {
            return _argLocDescForStructInRegs.HasValue;
        }

        // Unix AMD64 ABI: Returns true if the ArgDestination represents a struct passed in registers.
        public bool IsStructPassedInRegs()
        {
            return _offset == TransitionBlock.StructInRegsOffset;
        }

        private int GetStructFloatRegDestinationAddress()
        {
            Debug.Assert(IsStructPassedInRegs());
            return _transitionBlock.OffsetOfFloatArgumentRegisters + _argLocDescForStructInRegs.Value.m_idxFloatReg * 16;
        }

        // Get destination address for non-floating point fields of a struct passed in registers.
        private int GetStructGenRegDestinationAddress()
        {
            Debug.Assert(IsStructPassedInRegs());
            return _transitionBlock.OffsetOfArgumentRegisters + _argLocDescForStructInRegs.Value.m_idxGenReg * 8;
        }

        // Report managed object pointers in the struct in registers
        // Arguments:
        //  fn - promotion function to apply to each managed object pointer
        //  sc - scan context to pass to the promotion function
        //  fieldBytes - size of the structure
        internal void ReportPointersFromStructInRegisters(TypeDesc type, int delta, CORCOMPILE_GCREFMAP_TOKENS[] frame)
        {
            Debug.Assert(IsStructPassedInRegs());

            int genRegDest = GetStructGenRegDestinationAddress();

            SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor;
            SystemVStructClassificator.GetSystemVAmd64PassStructInRegisterDescriptor(type, out descriptor);

            for (int i = 0; i < descriptor.eightByteCount; i++)
            {
                int eightByteSize = (i == 0) ? descriptor.eightByteSizes0 : descriptor.eightByteSizes1;
                SystemVClassificationType eightByteClassification = (i == 0) ? descriptor.eightByteClassifications0 : descriptor.eightByteClassifications1;

                if (eightByteClassification != SystemVClassificationType.SystemVClassificationTypeSSE)
                {
                    if ((eightByteClassification == SystemVClassificationType.SystemVClassificationTypeIntegerReference) ||
                        (eightByteClassification == SystemVClassificationType.SystemVClassificationTypeIntegerByRef))
                    {
                        Debug.Assert(eightByteSize == 8);
                        Debug.Assert((genRegDest & 7) == 0);

                        CORCOMPILE_GCREFMAP_TOKENS token = (eightByteClassification == SystemVClassificationType.SystemVClassificationTypeIntegerByRef) ? CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_INTERIOR : CORCOMPILE_GCREFMAP_TOKENS.GCREFMAP_REF;
                        frame[delta + genRegDest] = token;
                    }

                    genRegDest += eightByteSize;
                }
            }
        }
    }

    internal class ArgIteratorData
    {
        public ArgIteratorData(bool hasThis,
                        bool isVarArg,
                        TypeHandle[] parameterTypes,
                        TypeHandle returnType)
        {
            _hasThis = hasThis;
            _isVarArg = isVarArg;
            _parameterTypes = parameterTypes;
            _returnType = returnType;
        }

        private bool _hasThis;
        private bool _isVarArg;
        private TypeHandle[] _parameterTypes;
        private TypeHandle _returnType;

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;

            ArgIteratorData other = obj as ArgIteratorData;
            if (other == null)
                return false;

            if (_hasThis != other._hasThis || _isVarArg != other._isVarArg || !_returnType.Equals(other._returnType))
                return false;

            if (_parameterTypes == null)
                return other._parameterTypes == null;

            if (other._parameterTypes == null || _parameterTypes.Length != other._parameterTypes.Length)
                return false;

            for (int i = 0; i < _parameterTypes.Length; i++)
                if (!_parameterTypes[i].Equals(other._parameterTypes[i]))
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return 37 + (_parameterTypes == null ?
                _returnType.GetHashCode() :
                TypeHashingAlgorithms.ComputeGenericInstanceHashCode(_returnType.GetHashCode(), _parameterTypes));
        }

        public bool HasThis() { return _hasThis; }
        public bool IsVarArg() { return _isVarArg; }
        public int NumFixedArgs() { return _parameterTypes != null ? _parameterTypes.Length : 0; }

        // Argument iteration.
        public CorElementType GetArgumentType(int argNum, out TypeHandle thArgType)
        {
            thArgType = _parameterTypes[argNum];
            CorElementType returnValue = thArgType.GetCorElementType();
            return returnValue;
        }

        public TypeHandle GetByRefArgumentType(int argNum)
        {
            return (argNum < _parameterTypes.Length && _parameterTypes[argNum].GetCorElementType() == CorElementType.ELEMENT_TYPE_BYREF) ?
                _parameterTypes[argNum] :
                default(TypeHandle);
        }

        public CorElementType GetReturnType(out TypeHandle thRetType)
        {
            thRetType = _returnType;
            return thRetType.GetCorElementType();
        }
    }

    //-----------------------------------------------------------------------
    // ArgIterator is helper for dealing with calling conventions.
    // It is tightly coupled with TransitionBlock. It uses offsets into
    // TransitionBlock to represent argument locations for efficiency
    // reasons. Alternatively, it can also return ArgLocDesc for less
    // performance critical code.
    //
    // The ARGITERATOR_BASE argument of the template is provider of the parsed
    // method signature. Typically, the arg iterator works on top of MetaSig. 
    // Reflection invoke uses alternative implementation to save signature parsing
    // time because of it has the parsed signature available.
    //-----------------------------------------------------------------------
    //template<class ARGITERATOR_BASE>
    internal struct ArgIterator
    {
        private readonly TypeSystemContext _context;

        private readonly TransitionBlock _transitionBlock;

        private bool _hasThis;
        private bool _hasParamType;
        private bool _extraFunctionPointerArg;
        private ArgIteratorData _argData;
        private bool[] _forcedByRefParams;
        private bool _skipFirstArg;
        private bool _extraObjectFirstArg;
        private CallingConventions _interpreterCallingConvention;
        private bool _hasArgLocDescForStructInRegs;
        private ArgLocDesc _argLocDescForStructInRegs;

        public bool HasThis => _hasThis;
        public bool IsVarArg => _argData.IsVarArg();
        public bool HasParamType => _hasParamType;
        public int NumFixedArgs => _argData.NumFixedArgs() + (_extraFunctionPointerArg ? 1 : 0) + (_extraObjectFirstArg ? 1 : 0);

        // Argument iteration.
        public CorElementType GetArgumentType(int argNum, out TypeHandle thArgType, out bool forceByRefReturn)
        {
            forceByRefReturn = false;

            if (_extraObjectFirstArg && argNum == 0)
            {
                thArgType = new TypeHandle(_context.GetWellKnownType(WellKnownType.Object));
                return CorElementType.ELEMENT_TYPE_CLASS;
            }

            argNum = _extraObjectFirstArg ? argNum - 1 : argNum;
            Debug.Assert(argNum >= 0);

            if (_forcedByRefParams != null && (argNum + 1) < _forcedByRefParams.Length)
                forceByRefReturn = _forcedByRefParams[argNum + 1];

            if (_extraFunctionPointerArg && argNum == _argData.NumFixedArgs())
            {
                thArgType = new TypeHandle(_context.GetWellKnownType(WellKnownType.IntPtr));
                return CorElementType.ELEMENT_TYPE_I;
            }

            return _argData.GetArgumentType(argNum, out thArgType);
        }

        public CorElementType GetReturnType(out TypeHandle thRetType, out bool forceByRefReturn)
        {
            if (_forcedByRefParams != null && _forcedByRefParams.Length > 0)
                forceByRefReturn = _forcedByRefParams[0];
            else
                forceByRefReturn = false;

            return _argData.GetReturnType(out thRetType);
        }

        public void Reset()
        {
            _argType = default(CorElementType);
            _argTypeHandle = default(TypeHandle);
            _argSize = 0;
            _argNum = 0;
            _argForceByRef = false;
            _ITERATION_STARTED = false;
        }

        //public:
        //------------------------------------------------------------
        // Constructor
        //------------------------------------------------------------
        public ArgIterator(
            TypeSystemContext context,
            ArgIteratorData argData, 
            CallingConventions callConv, 
            bool hasParamType, 
            bool extraFunctionPointerArg, 
            bool[] forcedByRefParams, 
            bool skipFirstArg, 
            bool extraObjectFirstArg)
        {
            this = default(ArgIterator);
            _context = context;
            _argData = argData;
            _hasThis = callConv == CallingConventions.ManagedInstance;
            _hasParamType = hasParamType;
            _extraFunctionPointerArg = extraFunctionPointerArg;
            _forcedByRefParams = forcedByRefParams;
            _skipFirstArg = skipFirstArg;
            _extraObjectFirstArg = extraObjectFirstArg;
            _interpreterCallingConvention = callConv;
            _transitionBlock = TransitionBlock.FromTarget(context.Target);
        }

        private uint SizeOfArgStack()
        {
            //        WRAPPER_NO_CONTRACT;
            if (!_SIZE_OF_ARG_STACK_COMPUTED)
                ForceSigWalk();
            Debug.Assert(_SIZE_OF_ARG_STACK_COMPUTED);
            Debug.Assert((_nSizeOfArgStack % _transitionBlock.PointerSize) == 0);
            return (uint)_nSizeOfArgStack;
        }

        // For use with ArgIterator. This function computes the amount of additional
        // memory required above the TransitionBlock.  The parameter offsets
        // returned by ArgIterator::GetNextOffset are relative to a
        // FramedMethodFrame, and may be in either of these regions.
        public int SizeOfFrameArgumentArray()
        {
            //        WRAPPER_NO_CONTRACT;

            uint size = SizeOfArgStack();

            if (_transitionBlock.IsX64 && !_transitionBlock.IsX64UnixABI)
            {
                // The argument registers are not included in the stack size on AMD64
                size += (uint)_transitionBlock.SizeOfArgumentRegisters;
            }

            Debug.Assert((size % _transitionBlock.PointerSize) == 0);
            return (int)size;
        }

        //------------------------------------------------------------------------

        public uint CbStackPop()
        {
            if (_transitionBlock.IsX86)
            {
                //        WRAPPER_NO_CONTRACT;

                if (IsVarArg)
                    return 0;
                else
                    return SizeOfArgStack();
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        // Is there a hidden parameter for the return parameter? 
        //
        public bool HasRetBuffArg()
        {
            //        WRAPPER_NO_CONTRACT;
            if (!_RETURN_FLAGS_COMPUTED)
                ComputeReturnFlags();
            return _RETURN_HAS_RET_BUFFER;
        }

        public uint GetFPReturnSize()
        {
            //        WRAPPER_NO_CONTRACT;
            if (!_RETURN_FLAGS_COMPUTED)
                ComputeReturnFlags();
            return _fpReturnSize;
        }

        public bool IsArgPassedByRef()
        {
            //        LIMITED_METHOD_CONTRACT;
            if (IsArgForcedPassedByRef())
            {
                return true;
            }

            if (_argType == CorElementType.ELEMENT_TYPE_BYREF)
            {
                return true;
            }
            if (_transitionBlock.EnregisteredParamTypeMaxSize != 0)
            {
                switch (_transitionBlock.Architecture)
                {
                    case TargetArchitecture.X64:
                        return _transitionBlock.IsArgPassedByRef(_argSize);
                    case TargetArchitecture.ARM64:
                        if (_argType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                        {
                            Debug.Assert(!_argTypeHandle.IsNull());
                            return ((_argSize > _transitionBlock.EnregisteredParamTypeMaxSize) && (!_argTypeHandle.IsHomogeneousAggregate() || IsVarArg));
                        }
                        return false;
                    case TargetArchitecture.LoongArch64:
                        if (_argType == CorElementType.ELEMENT_TYPE_VALUETYPE)
                        {
                            Debug.Assert(!_argTypeHandle.IsNull());
                            return ((_argSize > _transitionBlock.EnregisteredParamTypeMaxSize) || _transitionBlock.IsArgPassedByRef(_argTypeHandle));
                        }
                        return false;
                    default:
                        throw new NotImplementedException();
                }
            }
            else
            {
                return false;
            }
        }

        private bool IsArgForcedPassedByRef()
        {
            // This should be true for valuetypes instantiated over T in a generic signature using universal shared generic calling convention
            return _argForceByRef;
        }

        //------------------------------------------------------------
        // Return the offsets of the special arguments
        //------------------------------------------------------------

        public int GetThisOffset()
        {
            return _transitionBlock.ThisOffset;
        }

        public int GetVASigCookieOffset()
        {
            //            WRAPPER_NO_CONTRACT;

            Debug.Assert(IsVarArg);

            if (_transitionBlock.IsX86)
            {
                // x86 is special as always
                return _transitionBlock.SizeOfTransitionBlock;
            }
            else
            {
                // VaSig cookie is after this and retbuf arguments by default.
                int ret = _transitionBlock.OffsetOfArgumentRegisters;

                if (HasThis)
                {
                    ret += _transitionBlock.PointerSize;
                }

                if (HasRetBuffArg() && _transitionBlock.IsRetBuffPassedAsFirstArg)
                {
                    ret += _transitionBlock.PointerSize;
                }

                return ret;
            }
        }

        public int GetParamTypeArgOffset()
        {
            Debug.Assert(HasParamType);

            if (_transitionBlock.IsX86)
            {
                // x86 is special as always
                if (!_SIZE_OF_ARG_STACK_COMPUTED)
                    ForceSigWalk();

                switch (_paramTypeLoc)
                {
                    case ParamTypeLocation.Ecx:// PARAM_TYPE_REGISTER_ECX:
                        return _transitionBlock.OffsetOfArgumentRegisters + TransitionBlock.X86Constants.OffsetOfEcx;
                    case ParamTypeLocation.Edx:
                        return _transitionBlock.OffsetOfArgumentRegisters + TransitionBlock.X86Constants.OffsetOfEdx;
                    default:
                        break;
                }

                // The param type arg is last stack argument otherwise
                return _transitionBlock.SizeOfTransitionBlock;
            }
            else
            {
                // The hidden arg is after this and retbuf arguments by default.
                int ret = _transitionBlock.OffsetOfArgumentRegisters;

                if (HasThis)
                {
                    ret += _transitionBlock.PointerSize;
                }

                if (HasRetBuffArg() && _transitionBlock.IsRetBuffPassedAsFirstArg)
                {
                    ret += _transitionBlock.PointerSize;
                }

                return ret;
            }
        }

        //------------------------------------------------------------
        // Each time this is called, this returns a byte offset of the next
        // argument from the TransitionBlock* pointer. This offset can be positive *or* negative.
        //
        // Returns TransitionBlock::InvalidOffset once you've hit the end 
        // of the list.
        //------------------------------------------------------------
        public int GetNextOffset()
        {
            //            WRAPPER_NO_CONTRACT;
            //            SUPPORTS_DAC;

            if (!_ITERATION_STARTED)
            {
                int numRegistersUsed = 0;

                if (HasThis)
                    numRegistersUsed++;

                if (HasRetBuffArg() && _transitionBlock.IsRetBuffPassedAsFirstArg)
                {
                    numRegistersUsed++;
                }

                Debug.Assert(!IsVarArg || !HasParamType);

                // DESKTOP BEHAVIOR - This block is disabled for x86 as the param arg is the last argument on .NET Framework x86.
                if (!_transitionBlock.IsX86)
                {
                    if (HasParamType)
                    {
                        numRegistersUsed++;
                    }
                }

                if (!_transitionBlock.IsX86 && IsVarArg)
                {
                    numRegistersUsed++;
                }

                switch (_transitionBlock.Architecture)
                {
                    case TargetArchitecture.X86:
                        if (IsVarArg)
                        {
                            numRegistersUsed = _transitionBlock.NumArgumentRegisters; // Nothing else gets passed in registers for varargs
                        }

#if FEATURE_INTERPRETER
                        switch (_interpreterCallingConvention)
                        {
                            case CallingConventions.StdCall:
                                _numRegistersUsed = ArchitectureConstants.NUM_ARGUMENT_REGISTERS;
                                _ofsStack = TransitionBlock.GetOffsetOfArgs() + numRegistersUsed * _transitionBlock.PointerSize + initialArgOffset;
                                break;

                            case CallingConventions.ManagedStatic:
                            case CallingConventions.ManagedInstance:
                                _numRegistersUsed = numRegistersUsed;
                                // DESKTOP BEHAVIOR     _curOfs = (int)(TransitionBlock.GetOffsetOfArgs() + SizeOfArgStack());
                                _ofsStack= (int)(TransitionBlock.GetOffsetOfArgs() + initialArgOffset);
                                break;

                            default:
                                Environment.FailFast("Unsupported calling convention.");
                                break;
                        }
#endif
                        _x86NumRegistersUsed = numRegistersUsed;
                        _x86OfsStack = (int)(_transitionBlock.OffsetOfArgs + SizeOfArgStack());
                        break;

                    case TargetArchitecture.X64:
                        if (_transitionBlock.IsX64UnixABI)
                        {
                            _x64UnixIdxGenReg = numRegistersUsed;
                            _x64UnixIdxStack = 0;
                            _x64UnixIdxFPReg = 0;
                        }
                        else
                        {
                            _x64WindowsCurOfs = _transitionBlock.OffsetOfArgs + numRegistersUsed * _transitionBlock.PointerSize;
                        }
                        break;

                    case TargetArchitecture.ARM:
                        _armIdxGenReg = numRegistersUsed;
                        _armOfsStack = 0;

                        _armWFPRegs = 0;
                        break;

                    case TargetArchitecture.ARM64:
                        _arm64IdxGenReg = numRegistersUsed;
                        _arm64OfsStack = 0;

                        _arm64IdxFPReg = 0;
                        break;

                    case TargetArchitecture.LoongArch64:
                        _loongarch64IdxGenReg = numRegistersUsed;
                        _loongarch64OfsStack = 0;

                        _loongarch64IdxFPReg = 0;
                        break;

                    default:
                        throw new NotImplementedException();
                }

                _argNum = (_skipFirstArg ? 1 : 0);

                _ITERATION_STARTED = true;
            }

            if (_argNum >= NumFixedArgs)
                return TransitionBlock.InvalidOffset;

            CorElementType argType = GetArgumentType(_argNum, out _argTypeHandle, out _argForceByRef);

            _argTypeHandleOfByRefParam = (argType == CorElementType.ELEMENT_TYPE_BYREF ? _argData.GetByRefArgumentType(_argNum) : default(TypeHandle));

            _argNum++;

            int argSize = TypeHandle.GetElemSize(argType, _argTypeHandle);

            _argType = argType;
            _argSize = argSize;

            argType = _argForceByRef ? CorElementType.ELEMENT_TYPE_BYREF : argType;
            argSize = _argForceByRef ? _transitionBlock.PointerSize : argSize;

            int argOfs;

            switch (_transitionBlock.Architecture)
            {
                case TargetArchitecture.X86:
#if FEATURE_INTERPRETER
                    if (_interpreterCallingConvention != CallingConventions.ManagedStatic && _interpreterCallingConvention != CallingConventions.ManagedInstance)
                    {
                        argOfs = _curOfs;
                        _curOfs += ArchitectureConstants.StackElemSize(argSize);
                        return argOfs;
                    }
#endif
                    if (_transitionBlock.IsArgumentInRegister(ref _x86NumRegistersUsed, argType, _argTypeHandle))
                    {
                        return _transitionBlock.OffsetOfArgumentRegisters + (_transitionBlock.NumArgumentRegisters - _x86NumRegistersUsed) * _transitionBlock.PointerSize;
                    }

                    _x86OfsStack -= _transitionBlock.StackElemSize(argSize);
                    argOfs = _x86OfsStack;

                    Debug.Assert(argOfs >= _transitionBlock.OffsetOfArgs);
                    return argOfs;

                case TargetArchitecture.X64:
                    if (_transitionBlock.IsX64UnixABI)
                    {
                        int cbArg = _transitionBlock.StackElemSize(argSize);

                        _hasArgLocDescForStructInRegs = false;
                        _fX64UnixArgInRegisters = true;
                        int cFPRegs = 0;
                        int cGenRegs = 0;

                        switch (argType)
                        {

                            case CorElementType.ELEMENT_TYPE_R4:
                                // 32-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_R8:
                                // 64-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_VALUETYPE:
                            {
                                SYSTEMV_AMD64_CORINFO_STRUCT_REG_PASSING_DESCRIPTOR descriptor;
                                SystemVStructClassificator.GetSystemVAmd64PassStructInRegisterDescriptor(_argTypeHandle.GetRuntimeTypeHandle(), out descriptor);

                                if (descriptor.passedInRegisters)
                                {
                                    cGenRegs = 0;
                                    for (int i = 0; i < descriptor.eightByteCount; i++)
                                    {
                                        switch ((i == 0) ? descriptor.eightByteClassifications0 : descriptor.eightByteClassifications1)
                                        {
                                            case SystemVClassificationType.SystemVClassificationTypeInteger:
                                            case SystemVClassificationType.SystemVClassificationTypeIntegerReference:
                                            case SystemVClassificationType.SystemVClassificationTypeIntegerByRef:
                                                cGenRegs++;
                                                break;
                                            case SystemVClassificationType.SystemVClassificationTypeSSE:
                                                cFPRegs++;
                                                break;
                                            default:
                                                Debug.Assert(false);
                                                break;
                                        }
                                    }

                                    // Check if we have enough registers available for the struct passing
                                    if ((cFPRegs + _x64UnixIdxFPReg <= TransitionBlock.X64UnixTransitionBlock.NUM_FLOAT_ARGUMENT_REGISTERS) && (cGenRegs + _x64UnixIdxGenReg) <= _transitionBlock.NumArgumentRegisters)
                                    {
                                        _argLocDescForStructInRegs = new ArgLocDesc();
                                        _argLocDescForStructInRegs.m_cGenReg = (short)cGenRegs;
                                        _argLocDescForStructInRegs.m_cFloatReg = cFPRegs;
                                        _argLocDescForStructInRegs.m_idxGenReg = _x64UnixIdxGenReg;
                                        _argLocDescForStructInRegs.m_idxFloatReg = _x64UnixIdxFPReg;

                                        _hasArgLocDescForStructInRegs = true;

                                        _x64UnixIdxGenReg += cGenRegs;
                                        _x64UnixIdxFPReg += cFPRegs;

                                        return TransitionBlock.StructInRegsOffset;
                                    }
                                }

                                // Set the register counts to indicate that this argument will not be passed in registers
                                cFPRegs = 0;
                                cGenRegs = 0;
                                break;
                            }

                            default:
                                cGenRegs = cbArg / 8; // GP reg size
                                break;
                        }

                        if (cFPRegs > 0)
                        {
                            if (cFPRegs + _x64UnixIdxFPReg <= TransitionBlock.X64UnixTransitionBlock.NUM_FLOAT_ARGUMENT_REGISTERS)
                            {
                                int argOfsInner = _transitionBlock.OffsetOfFloatArgumentRegisters + _x64UnixIdxFPReg * 8;
                                _x64UnixIdxFPReg += cFPRegs;
                                return argOfsInner;
                            }
                        }
                        else if (cGenRegs > 0)
                        {
                            if (_x64UnixIdxGenReg + cGenRegs <= _transitionBlock.NumArgumentRegisters)
                            {
                                int argOfsInner = _transitionBlock.OffsetOfArgumentRegisters + _x64UnixIdxGenReg * 8;
                                _x64UnixIdxGenReg += cGenRegs;
                                return argOfsInner;
                            }
                        }

                        _fX64UnixArgInRegisters = false;

                        argOfs = _transitionBlock.OffsetOfArgs + _x64UnixIdxStack * 8;
                        int cArgSlots = cbArg / _transitionBlock.PointerSize;

                        _x64UnixIdxStack += cArgSlots;
                        return argOfs;
                    }
                    else
                    {
                        int cFPRegs = 0;

                        switch (argType)
                        {
                            case CorElementType.ELEMENT_TYPE_R4:
                                // 32-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_R8:
                                // 64-bit floating point argument.
                                cFPRegs = 1;
                                break;
                        }

                        // Each argument takes exactly one slot on AMD64
                        argOfs = _x64WindowsCurOfs - _transitionBlock.OffsetOfArgs;
                        _x64WindowsCurOfs += _transitionBlock.PointerSize;

                        if ((cFPRegs == 0) || (argOfs >= _transitionBlock.SizeOfArgumentRegisters))
                        {
                            return argOfs + _transitionBlock.OffsetOfArgs;
                        }
                        else
                        {
                            int idxFpReg = argOfs / _transitionBlock.PointerSize;
                            return _transitionBlock.OffsetOfFloatArgumentRegisters + idxFpReg * TransitionBlock.SizeOfM128A;
                        }
                    }

                case TargetArchitecture.ARM:
                    {
                        // First look at the underlying type of the argument to determine some basic properties:
                        //  1) The size of the argument in bytes (rounded up to the stack slot size of 4 if necessary).
                        //  2) Whether the argument represents a floating point primitive (ELEMENT_TYPE_R4 or ELEMENT_TYPE_R8).
                        //  3) Whether the argument requires 64-bit alignment (anything that contains a Int64/UInt64).

                        bool fFloatingPoint = false;
                        bool fRequiresAlign64Bit = false;

                        switch (argType)
                        {
                            case CorElementType.ELEMENT_TYPE_I8:
                            case CorElementType.ELEMENT_TYPE_U8:
                                // 64-bit integers require 64-bit alignment on ARM.
                                fRequiresAlign64Bit = true;
                                break;

                            case CorElementType.ELEMENT_TYPE_R4:
                                // 32-bit floating point argument.
                                fFloatingPoint = true;
                                break;

                            case CorElementType.ELEMENT_TYPE_R8:
                                // 64-bit floating point argument.
                                fFloatingPoint = true;
                                fRequiresAlign64Bit = true;
                                break;

                            case CorElementType.ELEMENT_TYPE_VALUETYPE:
                                {
                                    // Value type case: extract the alignment requirement, note that this has to handle 
                                    // the interop "native value types".
                                    fRequiresAlign64Bit = _argTypeHandle.RequiresAlign8();

                                    // Handle HFAs: packed structures of 1-4 floats or doubles that are passed in FP argument
                                    // registers if possible.
                                    if (_argTypeHandle.IsHomogeneousAggregate())
                                        fFloatingPoint = true;

                                    break;
                                }

                            default:
                                // The default is are 4-byte arguments (or promoted to 4 bytes), non-FP and don't require any
                                // 64-bit alignment.
                                break;
                        }

                        // Now attempt to place the argument into some combination of floating point or general registers and
                        // the stack.

                        // Save the alignment requirement
                        _armRequires64BitAlignment = fRequiresAlign64Bit;

                        int cbArg = _transitionBlock.StackElemSize(argSize);
                        Debug.Assert((cbArg % _transitionBlock.PointerSize) == 0);

                        // Ignore floating point argument placement in registers if we're dealing with a vararg function (the ABI
                        // specifies this so that vararg processing on the callee side is simplified).
                        if (fFloatingPoint && _transitionBlock.IsArmhfABI && !IsVarArg)
                        {
                            // Handle floating point (primitive) arguments.

                            // First determine whether we can place the argument in VFP registers. There are 16 32-bit
                            // and 8 64-bit argument registers that share the same register space (e.g. D0 overlaps S0 and
                            // S1). The ABI specifies that VFP values will be passed in the lowest sequence of registers that
                            // haven't been used yet and have the required alignment. So the sequence (float, double, float)
                            // would be mapped to (S0, D1, S1) or (S0, S2/S3, S1).
                            //
                            // We use a 16-bit bitmap to record which registers have been used so far.
                            //
                            // So we can use the same basic loop for each argument type (float, double or HFA struct) we set up
                            // the following input parameters based on the size and alignment requirements of the arguments:
                            //   wAllocMask : bitmask of the number of 32-bit registers we need (1 for 1, 3 for 2, 7 for 3 etc.)
                            //   cSteps     : number of loop iterations it'll take to search the 16 registers
                            //   cShift     : how many bits to shift the allocation mask on each attempt

                            ushort wAllocMask = checked((ushort)((1 << (cbArg / 4)) - 1));
                            ushort cSteps = (ushort)(fRequiresAlign64Bit ? 9 - (cbArg / 8) : 17 - (cbArg / 4));
                            ushort cShift = fRequiresAlign64Bit ? (ushort)2 : (ushort)1;

                            // Look through the availability bitmask for a free register or register pair.
                            for (ushort i = 0; i < cSteps; i++)
                            {
                                if ((_armWFPRegs & wAllocMask) == 0)
                                {
                                    // We found one, mark the register or registers as used. 
                                    _armWFPRegs |= wAllocMask;

                                    // Indicate the registers used to the caller and return.
                                    return _transitionBlock.OffsetOfFloatArgumentRegisters + (i * cShift * 4);
                                }
                                wAllocMask <<= cShift;
                            }

                            // The FP argument is going to live on the stack. Once this happens the ABI demands we mark all FP
                            // registers as unavailable.
                            _armWFPRegs = 0xffff;

                            // Doubles or HFAs containing doubles need the stack aligned appropriately.
                            if (fRequiresAlign64Bit)
                                _armOfsStack = ALIGN_UP(_armOfsStack, _transitionBlock.PointerSize * 2);

                            // Indicate the stack location of the argument to the caller.
                            int argOfsInner = _transitionBlock.OffsetOfArgs + _armOfsStack;

                            // Record the stack usage.
                            _armOfsStack += cbArg;

                            return argOfsInner;
                        }

                        //
                        // Handle the non-floating point case.
                        //

                        if (_armIdxGenReg < 4)
                        {
                            if (fRequiresAlign64Bit)
                            {
                                // The argument requires 64-bit alignment. Align either the next general argument register if
                                // we have any left.  See step C.3 in the algorithm in the ABI spec.       
                                _armIdxGenReg = ALIGN_UP(_armIdxGenReg, 2);
                            }

                            int argOfsInner = _transitionBlock.OffsetOfArgumentRegisters + _armIdxGenReg * 4;

                            int cRemainingRegs = 4 - _armIdxGenReg;
                            if (cbArg <= cRemainingRegs * _transitionBlock.PointerSize)
                            {
                                // Mark the registers just allocated as used.
                                _armIdxGenReg += ALIGN_UP(cbArg, _transitionBlock.PointerSize) / _transitionBlock.PointerSize;
                                return argOfsInner;
                            }

                            // The ABI supports splitting a non-FP argument across registers and the stack. But this is
                            // disabled if the FP arguments already overflowed onto the stack (i.e. the stack index is not
                            // zero). The following code marks the general argument registers as exhausted if this condition
                            // holds.  See steps C.5 in the algorithm in the ABI spec.

                            _armIdxGenReg = 4;

                            if (_armOfsStack == 0)
                            {
                                _armOfsStack += cbArg - cRemainingRegs * _transitionBlock.PointerSize;
                                return argOfsInner;
                            }
                        }

                        if (fRequiresAlign64Bit)
                        {
                            // The argument requires 64-bit alignment. If it is going to be passed on the stack, align
                            // the next stack slot.  See step C.6 in the algorithm in the ABI spec.  
                            _armOfsStack = ALIGN_UP(_armOfsStack, _transitionBlock.PointerSize * 2);
                        }

                        argOfs = _transitionBlock.OffsetOfArgs + _armOfsStack;

                        // Advance the stack pointer over the argument just placed.
                        _armOfsStack += cbArg;

                        return argOfs;
                    }

                case TargetArchitecture.ARM64:
                    {
                        int cFPRegs = 0;
                        bool isFloatHFA = false;

                        switch (argType)
                        {
                            case CorElementType.ELEMENT_TYPE_R4:
                                // 32-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_R8:
                                // 64-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_VALUETYPE:
                                {
                                    // Handle HAs: packed structures of 1-4 floats, doubles, or short vectors
                                    // that are passed in FP argument registers if possible.
                                    if (_argTypeHandle.IsHomogeneousAggregate())
                                    {
                                        _argLocDescForStructInRegs = new ArgLocDesc();
                                        _argLocDescForStructInRegs.m_idxFloatReg = _arm64IdxFPReg;

                                        int haElementSize = _argTypeHandle.GetHomogeneousAggregateElementSize();
                                        if (haElementSize == 4)
                                        {
                                            isFloatHFA = true;
                                        }
                                        cFPRegs = argSize / haElementSize;
                                        _argLocDescForStructInRegs.m_cFloatReg = cFPRegs;

                                        // Check if we have enough registers available for the HA passing
                                        if (cFPRegs + _arm64IdxFPReg <= 8)
                                        {
                                            _hasArgLocDescForStructInRegs = true;
                                        }
                                    }
                                    else
                                    {
                                        // Composite greater than 16 bytes should be passed by reference
                                        if (argSize > _transitionBlock.EnregisteredParamTypeMaxSize)
                                        {
                                            argSize = _transitionBlock.PointerSize;
                                        }
                                    }

                                    break;
                                }

                            default:
                                break;
                        }

                        bool isValueType = (argType == CorElementType.ELEMENT_TYPE_VALUETYPE);
                        int cbArg = _transitionBlock.StackElemSize(argSize, isValueType, isFloatHFA);

                        if (cFPRegs > 0 && !IsVarArg)
                        {
                            if (cFPRegs + _arm64IdxFPReg <= 8)
                            {
                                // Each floating point register in the argument area is 16 bytes.
                                int argOfsInner = _transitionBlock.OffsetOfFloatArgumentRegisters + _arm64IdxFPReg * 16;
                                _arm64IdxFPReg += cFPRegs;
                                return argOfsInner;
                            }
                            else
                            {
                                _arm64IdxFPReg = 8;
                            }
                        }
                        else
                        {
                            Debug.Assert(_transitionBlock.IsAppleArm64ABI || (cbArg % _transitionBlock.PointerSize) == 0);

                            int regSlots = ALIGN_UP(cbArg, _transitionBlock.PointerSize) / _transitionBlock.PointerSize;
                            // Only x0-x7 are valid argument registers (x8 is always the return buffer)
                            if (_arm64IdxGenReg + regSlots <= 8)
                            {
                                // The entirety of the arg fits in the register slots.
                                int argOfsInner = _transitionBlock.OffsetOfArgumentRegisters + _arm64IdxGenReg * 8;
                                _arm64IdxGenReg += regSlots;
                                return argOfsInner;
                            }
                            else if (_context.Target.IsWindows && IsVarArg && (_arm64IdxGenReg < 8))
                            {
                                // Address the Windows ARM64 varargs case where an arg is split between regs and stack.
                                // This can happen in the varargs case because the first 64 bytes of the stack are loaded
                                // into x0-x7, and any remaining stack arguments are placed normally.
                                int argOfsInner = _transitionBlock.OffsetOfArgumentRegisters + _arm64IdxGenReg * 8;

                                // Increase m_ofsStack to account for the space used for the remainder of the arg after
                                // registers are filled.
                                _arm64OfsStack += cbArg + (_arm64IdxGenReg - 8) * _transitionBlock.PointerSize;

                                // We used up the remaining reg slots.
                                _arm64IdxGenReg = 8;

                                return argOfsInner;
                            }
                            else
                            {
                                // Don't use reg slots for this. It will be passed purely on the stack arg space.
                                _arm64IdxGenReg = 8;
                            }
                        }

                        if (_transitionBlock.IsAppleArm64ABI)
                        {
                            int alignment;
                            if (!isValueType)
                            {
                                Debug.Assert((cbArg & (cbArg - 1)) == 0);
                                alignment = cbArg;
                            }
                            else if (isFloatHFA)
                            {
                                alignment = 4;
                            }
                            else
                            {
                                alignment = 8;
                            }
                            _arm64OfsStack = ALIGN_UP(_arm64OfsStack, alignment);
                        }

                        argOfs = _transitionBlock.OffsetOfArgs + _arm64OfsStack;
                        _arm64OfsStack += cbArg;
                        return argOfs;
                    }

                case TargetArchitecture.LoongArch64:
                    {
                        int cFPRegs = 0;
                        uint floatFieldFlags = (uint)StructFloatFieldInfoFlags.STRUCT_NO_FLOAT_FIELD;
                        _hasArgLocDescForStructInRegs = false;

                        switch (argType)
                        {
                            case CorElementType.ELEMENT_TYPE_R4:
                                // 32-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_R8:
                                // 64-bit floating point argument.
                                cFPRegs = 1;
                                break;

                            case CorElementType.ELEMENT_TYPE_VALUETYPE:
                                {
                                    // Composite greater than 16 bytes should be passed by reference
                                    if (argSize > _transitionBlock.EnregisteredParamTypeMaxSize)
                                    {
                                        argSize = _transitionBlock.PointerSize;
                                    }
                                    else
                                    {
                                        floatFieldFlags = LoongArch64PassStructInRegister.GetLoongArch64PassStructInRegisterFlags(_argTypeHandle.GetRuntimeTypeHandle());
                                        if ((floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_FLOAT_FIELD_ONLY_TWO) != 0)
                                        {
                                            cFPRegs = 2;
                                        }
                                        else if ((floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_HAS_FLOAT_FIELDS_MASK) != 0)
                                        {
                                            cFPRegs = 1;
                                        }
                                    }

                                    break;
                                }

                            default:
                                break;
                        }

                        bool isValueType = (argType == CorElementType.ELEMENT_TYPE_VALUETYPE);
                        int cbArg = _transitionBlock.StackElemSize(argSize, isValueType, false);

                        if (cFPRegs > 0 && !IsVarArg)
                        {
                            if (isValueType && ((floatFieldFlags & (uint)StructFloatFieldInfoFlags.STRUCT_HAS_ONE_FLOAT_MASK) != 0))
                            {
                                if ((_loongarch64IdxFPReg < 8) && (_loongarch64IdxGenReg < 8))
                                {
                                    _argLocDescForStructInRegs = new ArgLocDesc();
                                    _argLocDescForStructInRegs.m_idxFloatReg = _loongarch64IdxFPReg;
                                    _argLocDescForStructInRegs.m_cFloatReg = 1;

                                    _argLocDescForStructInRegs.m_idxGenReg = _loongarch64IdxGenReg;
                                    _argLocDescForStructInRegs.m_cGenReg = 1;

                                    _hasArgLocDescForStructInRegs = true;
                                    _argLocDescForStructInRegs.m_floatFlags = floatFieldFlags;

                                    int argOfsInner = _transitionBlock.OffsetOfFloatArgumentRegisters + _loongarch64IdxFPReg * 8;
                                    _loongarch64IdxFPReg++;
                                    _loongarch64IdxGenReg++;
                                    return argOfsInner;
                                }
                                else
                                {
                                    _loongarch64IdxFPReg = 8;
                                }
                            }
                            else if (cFPRegs + _loongarch64IdxFPReg <= 8)
                            {
                                // Each floating point register in the argument area is 8 bytes.
                                int argOfsInner = _transitionBlock.OffsetOfFloatArgumentRegisters + _loongarch64IdxFPReg * 8;
                                _loongarch64IdxFPReg += cFPRegs;
                                return argOfsInner;
                            }
                            else
                            {
                                _loongarch64IdxFPReg = 8;
                            }
                        }

                        {
                            Debug.Assert((cbArg % _transitionBlock.PointerSize) == 0);

                            int regSlots = ALIGN_UP(cbArg, _transitionBlock.PointerSize) / _transitionBlock.PointerSize;
                            // Only R4-R11 are valid argument registers.
                            if (_loongarch64IdxGenReg + regSlots <= 8)
                            {
                                // The entirety of the arg fits in the register slots.
                                int argOfsInner = _transitionBlock.OffsetOfArgumentRegisters + _loongarch64IdxGenReg * 8;
                                _loongarch64IdxGenReg += regSlots;
                                return argOfsInner;
                            }
                            else if (_loongarch64IdxGenReg < 8)
                            {
                                int argOfsInner = _transitionBlock.OffsetOfArgumentRegisters + _loongarch64IdxGenReg * 8;
                                _loongarch64IdxGenReg = 8;
                                _loongarch64OfsStack += 8;
                                return argOfsInner;
                            }
                            else
                            {
                                // Don't use reg slots for this. It will be passed purely on the stack arg space.
                                _loongarch64IdxGenReg = 8;
                            }
                        }

                        argOfs = _transitionBlock.OffsetOfArgs + _loongarch64OfsStack;
                        _loongarch64OfsStack += cbArg;
                        return argOfs;
                    }

                default:
                    throw new NotImplementedException();
            }
        }

        public CorElementType GetArgType(out TypeHandle pTypeHandle)
        {
            //        LIMITED_METHOD_CONTRACT;
            pTypeHandle = _argTypeHandle;
            return _argType;
        }

        public CorElementType GetByRefArgType(out TypeHandle pByRefArgTypeHandle)
        {
            //        LIMITED_METHOD_CONTRACT;
            pByRefArgTypeHandle = _argTypeHandleOfByRefParam;
            return _argType;
        }

        public int GetArgSize()
        {
            //        LIMITED_METHOD_CONTRACT;
            return _argSize;
        }

        public bool IsValueType()
        {
            return (_argType == CorElementType.ELEMENT_TYPE_VALUETYPE);
        }

        public bool IsFloatHfa()
        {
            if (IsValueType() && !IsVarArg && _argTypeHandle.IsHomogeneousAggregate())
            {
                int hfaElementSize = _argTypeHandle.GetHomogeneousAggregateElementSize();
                return hfaElementSize == 4;
            }
            return false;
        }

        private void ForceSigWalk()
        {
            // This can be only used before the actual argument iteration started
            Debug.Assert(!_ITERATION_STARTED);

            int numRegistersUsed = 0;
            int nSizeOfArgStack = 0;

            if (_transitionBlock.IsX86)
            {
                //
                // x86 is special as always
                //

                if (HasThis)
                    numRegistersUsed++;

                if (HasRetBuffArg() && _transitionBlock.IsRetBuffPassedAsFirstArg)
                {
                    numRegistersUsed++;
                }

                if (IsVarArg)
                {
                    nSizeOfArgStack += _transitionBlock.PointerSize;
                    numRegistersUsed = _transitionBlock.NumArgumentRegisters; // Nothing else gets passed in registers for varargs
                }

#if FEATURE_INTERPRETER
                switch (_interpreterCallingConvention)
                {
                    case CallingConventions.StdCall:
                        numRegistersUsed = ArchitectureConstants.NUM_ARGUMENT_REGISTERS;
                        break;

                    case CallingConventions.ManagedStatic:
                    case CallingConventions.ManagedInstance:
                        break;

                    default:
                        Environment.FailFast("Unsupported calling convention.");
                        break;
                }
#endif // FEATURE_INTERPRETER

                int nArgs = NumFixedArgs;
                for (int i = (_skipFirstArg ? 1 : 0); i < nArgs; i++)
                {
                    TypeHandle thArgType;
                    bool argForcedToBeByref;
                    CorElementType type = GetArgumentType(i, out thArgType, out argForcedToBeByref);
                    if (argForcedToBeByref)
                        type = CorElementType.ELEMENT_TYPE_BYREF;

                    if (!_transitionBlock.IsArgumentInRegister(ref numRegistersUsed, type, thArgType))
                    {
                        int structSize = TypeHandle.GetElemSize(type, thArgType);

                        nSizeOfArgStack += _transitionBlock.StackElemSize(structSize);

                        if (nSizeOfArgStack > TransitionBlock.MaxArgSize)
                        {
                            throw new NotSupportedException();
                        }
                    }
                }

                if (HasParamType)
                {
                    if (numRegistersUsed < _transitionBlock.NumArgumentRegisters)
                    {
                        numRegistersUsed++;
                        _paramTypeLoc = (numRegistersUsed == 1) ?
                            ParamTypeLocation.Ecx : ParamTypeLocation.Edx;
                    }
                    else
                    {
                        nSizeOfArgStack += _transitionBlock.PointerSize;
                        _paramTypeLoc = ParamTypeLocation.Stack;
                    }
                }
            }
            else
            {
                int maxOffset = _transitionBlock.OffsetOfArgs;

                int ofs;
                while (TransitionBlock.InvalidOffset != (ofs = GetNextOffset()))
                {
                    int stackElemSize;

                    if (_transitionBlock.IsX64)
                    {
                        if (_transitionBlock.IsX64UnixABI)
                        {
                            if (_fX64UnixArgInRegisters)
                            {
                                continue;
                            }

                            stackElemSize = _transitionBlock.StackElemSize(GetArgSize());
                        }
                        else
                        {
                            // All stack arguments take just one stack slot on AMD64 because of arguments bigger 
                            // than a stack slot are passed by reference. 
                            stackElemSize = _transitionBlock.PointerSize;
                        }
                    }
                    else
                    {
                        stackElemSize = _transitionBlock.StackElemSize(GetArgSize(), IsValueType(), IsFloatHfa());

                        if (IsArgPassedByRef())
                            stackElemSize = _transitionBlock.PointerSize;
                    }

                    int endOfs = ofs + stackElemSize;
                    if (endOfs > maxOffset)
                    {
                        if (endOfs > TransitionBlock.MaxArgSize)
                        {
                            throw new NotSupportedException();
                        }
                        maxOffset = endOfs;
                    }
                }
                // Clear the iterator started flag
                _ITERATION_STARTED = false;

                nSizeOfArgStack = maxOffset - _transitionBlock.OffsetOfArgs;

                if (_transitionBlock.IsX64 && !_transitionBlock.IsX64UnixABI)
                {
                    nSizeOfArgStack = (nSizeOfArgStack > (int)_transitionBlock.SizeOfArgumentRegisters) ?
                        (nSizeOfArgStack - _transitionBlock.SizeOfArgumentRegisters) : 0;
                }
            }

            // arg stack size is rounded to the pointer size on all platforms.
            nSizeOfArgStack = ALIGN_UP(nSizeOfArgStack, _transitionBlock.PointerSize);

            // Cache the result
            _nSizeOfArgStack = nSizeOfArgStack;
            _SIZE_OF_ARG_STACK_COMPUTED = true;

            Reset();
        }

        // Get layout information for the argument that the ArgIterator is currently visiting.
        public ArgLocDesc? GetArgLoc(int argOffset)
        {
            switch (_transitionBlock.Architecture)
            {
                case TargetArchitecture.ARM:
                    {
                        //        LIMITED_METHOD_CONTRACT;

                        ArgLocDesc pLoc = new ArgLocDesc();

                        pLoc.m_fRequires64BitAlignment = _armRequires64BitAlignment;

                        int byteArgSize = GetArgSize();

                        if (_transitionBlock.IsFloatArgumentRegisterOffset(argOffset))
                        {
                            int floatRegOfsInBytes = argOffset - _transitionBlock.OffsetOfFloatArgumentRegisters;
                            Debug.Assert((floatRegOfsInBytes % _transitionBlock.FloatRegisterSize) == 0);
                            pLoc.m_idxFloatReg = floatRegOfsInBytes / _transitionBlock.FloatRegisterSize;
                            pLoc.m_cFloatReg = ALIGN_UP(byteArgSize, _transitionBlock.FloatRegisterSize) / _transitionBlock.FloatRegisterSize;
                            return pLoc;
                        }

                        if (!_transitionBlock.IsStackArgumentOffset(argOffset))
                        {
                            pLoc.m_idxGenReg = _transitionBlock.GetArgumentIndexFromOffset(argOffset);

                            if (byteArgSize <= (4 - pLoc.m_idxGenReg) * _transitionBlock.PointerSize)
                            {
                                pLoc.m_cGenReg = (short)(ALIGN_UP(byteArgSize, _transitionBlock.PointerSize) / _transitionBlock.PointerSize);
                            }
                            else
                            {
                                pLoc.m_cGenReg = (short)(4 - pLoc.m_idxGenReg);

                                pLoc.m_byteStackIndex = 0;
                                pLoc.m_byteStackSize = _transitionBlock.StackElemSize(byteArgSize) - pLoc.m_cGenReg * _transitionBlock.PointerSize;
                            }
                        }
                        else
                        {
                            pLoc.m_byteStackIndex = _transitionBlock.GetStackArgumentByteIndexFromOffset(argOffset);
                            pLoc.m_byteStackSize = _transitionBlock.StackElemSize(byteArgSize);
                        }
                        return pLoc;
                    }

                case TargetArchitecture.ARM64:
                    {
                        //        LIMITED_METHOD_CONTRACT;

                        ArgLocDesc pLoc = new ArgLocDesc();

                        if (_transitionBlock.IsFloatArgumentRegisterOffset(argOffset))
                        {
                            int floatRegOfsInBytes = argOffset - _transitionBlock.OffsetOfFloatArgumentRegisters;
                            Debug.Assert((floatRegOfsInBytes % _transitionBlock.FloatRegisterSize) == 0);
                            pLoc.m_idxFloatReg = floatRegOfsInBytes / _transitionBlock.FloatRegisterSize;

                            if (!_argTypeHandle.IsNull() && _argTypeHandle.IsHomogeneousAggregate())
                            {
                                int haElementSize = _argTypeHandle.GetHomogeneousAggregateElementSize();
                                pLoc.m_cFloatReg = GetArgSize() / haElementSize;
                            }
                            else
                            {
                                pLoc.m_cFloatReg = 1;
                            }
                            return pLoc;
                        }

                        int byteArgSize = GetArgSize();

                        // On ARM64 some composites are implicitly passed by reference.
                        if (IsArgPassedByRef())
                        {
                            byteArgSize = _transitionBlock.PointerSize;
                        }

                        if (!_transitionBlock.IsStackArgumentOffset(argOffset))
                        {
                            pLoc.m_idxGenReg = _transitionBlock.GetArgumentIndexFromOffset(argOffset);
                            pLoc.m_cGenReg = (short)(ALIGN_UP(byteArgSize, _transitionBlock.PointerSize) / _transitionBlock.PointerSize);
                        }
                        else
                        {
                            pLoc.m_byteStackIndex = _transitionBlock.GetStackArgumentByteIndexFromOffset(argOffset);
                            pLoc.m_byteStackSize = _transitionBlock.StackElemSize(byteArgSize, IsValueType(), IsFloatHfa());
                        }
                        return pLoc;
                    }

                case TargetArchitecture.LoongArch64:
                    {
                        if (_hasArgLocDescForStructInRegs)
                        {
                            return _argLocDescForStructInRegs;
                        }

                        //        LIMITED_METHOD_CONTRACT;

                        ArgLocDesc pLoc = new ArgLocDesc();

                        if (_transitionBlock.IsFloatArgumentRegisterOffset(argOffset))
                        {
                            int floatRegOfsInBytes = argOffset - _transitionBlock.OffsetOfFloatArgumentRegisters;
                            Debug.Assert((floatRegOfsInBytes % _transitionBlock.FloatRegisterSize) == 0);
                            pLoc.m_idxFloatReg = floatRegOfsInBytes / _transitionBlock.FloatRegisterSize;

                            if (!_argTypeHandle.IsNull() && _argTypeHandle.IsHomogeneousAggregate())
                            {
                                int haElementSize = _argTypeHandle.GetHomogeneousAggregateElementSize();
                                pLoc.m_cFloatReg = GetArgSize() / haElementSize;
                            }
                            else
                            {
                                pLoc.m_cFloatReg = 1;
                            }
                            return pLoc;
                        }

                        int byteArgSize = GetArgSize();

                        // Composites greater than 16bytes are passed by reference
                        TypeHandle dummy;
                        if (GetArgType(out dummy) == CorElementType.ELEMENT_TYPE_VALUETYPE && GetArgSize() > _transitionBlock.EnregisteredParamTypeMaxSize)
                        {
                            byteArgSize = _transitionBlock.PointerSize;
                        }

                        if (!_transitionBlock.IsStackArgumentOffset(argOffset))
                        {
                            pLoc.m_idxGenReg = _transitionBlock.GetArgumentIndexFromOffset(argOffset);
                            if ((pLoc.m_idxGenReg == 7) && (byteArgSize > _transitionBlock.PointerSize))
                            {
                                pLoc.m_cGenReg = 1;
                                pLoc.m_byteStackIndex = 0;
                                pLoc.m_byteStackSize = 8;
                            }
                            else
                                pLoc.m_cGenReg = (short)(ALIGN_UP(byteArgSize, _transitionBlock.PointerSize) / _transitionBlock.PointerSize);
                        }
                        else
                        {
                            pLoc.m_byteStackIndex = _transitionBlock.GetStackArgumentByteIndexFromOffset(argOffset);
                            pLoc.m_byteStackSize = _transitionBlock.StackElemSize(byteArgSize, IsValueType(), IsFloatHfa());
                        }
                        return pLoc;
                    }

                case TargetArchitecture.X64:
                    if (_transitionBlock.IsX64UnixABI)
                    {
                        if (_hasArgLocDescForStructInRegs)
                        {
                            return _argLocDescForStructInRegs;
                        }

                        if (argOffset == TransitionBlock.StructInRegsOffset)
                        {
                            // We always already have argLocDesc for structs passed in registers, we 
                            // compute it in the GetNextOffset for those since it is always needed.
                            Debug.Assert(false);
                            return null;
                        }

                        ArgLocDesc pLoc = new ArgLocDesc();

                        if (_transitionBlock.IsFloatArgumentRegisterOffset(argOffset))
                        {
                            int floatRegOfsInBytes = argOffset - _transitionBlock.OffsetOfFloatArgumentRegisters;
                            Debug.Assert((floatRegOfsInBytes % _transitionBlock.FloatRegisterSize) == 0);
                            pLoc.m_idxFloatReg = floatRegOfsInBytes / _transitionBlock.FloatRegisterSize;
                            pLoc.m_cFloatReg = 1;
                        }
                        else if (!_transitionBlock.IsStackArgumentOffset(argOffset))
                        {
                            pLoc.m_idxGenReg = _transitionBlock.GetArgumentIndexFromOffset(argOffset);
                            pLoc.m_cGenReg = 1;
                        }
                        else
                        {
                            pLoc.m_byteStackIndex = _transitionBlock.GetStackArgumentByteIndexFromOffset(argOffset);
                            int argSizeInBytes;
                            if (IsArgPassedByRef())
                                argSizeInBytes = _transitionBlock.PointerSize;
                            else
                                argSizeInBytes = GetArgSize();
                            pLoc.m_byteStackSize = _transitionBlock.StackElemSize(argSizeInBytes);
                        }
                        return pLoc;
                    }
                    else
                    {
                        return null;
                    }

                case TargetArchitecture.X86:
                    return null;

                default:
                    throw new NotImplementedException();
            }
        }

        private int _nSizeOfArgStack;      // Cached value of SizeOfArgStack

        private int _argNum;

        // Cached information about last argument
        private CorElementType _argType;
        private int _argSize;
        private TypeHandle _argTypeHandle;
        private TypeHandle _argTypeHandleOfByRefParam;
        private bool _argForceByRef;

        private int _x86OfsStack;           // Current position of the stack iterator
        private int _x86NumRegistersUsed;

        private int _x64UnixIdxGenReg;
        private int _x64UnixIdxStack;
        private int _x64UnixIdxFPReg;
        private bool _fX64UnixArgInRegisters;
        private int _x64WindowsCurOfs;           // Current position of the stack iterator

        private int _armIdxGenReg;        // Next general register to be assigned a value
        private int _armOfsStack;         // Offset of next stack location to be assigned a value

        private ushort _armWFPRegs;          // Bitmask of available floating point argument registers (s0-s15/d0-d7)
        private bool _armRequires64BitAlignment; // Cached info about the current arg

        private int _arm64IdxGenReg;        // Next general register to be assigned a value
        private int _arm64OfsStack;         // Offset of next stack location to be assigned a value
        private int _arm64IdxFPReg;         // Next FP register to be assigned a value

        private int _loongarch64IdxGenReg;  // Next general register to be assigned a value
        private int _loongarch64OfsStack;   // Offset of next stack location to be assigned a value
        private int _loongarch64IdxFPReg;   // Next FP register to be assigned a value

        // These are enum flags in CallingConventions.h, but that's really ugly in C#, so I've changed them to bools.
        private bool _ITERATION_STARTED; // Started iterating over arguments
        private bool _SIZE_OF_ARG_STACK_COMPUTED;
        private bool _RETURN_FLAGS_COMPUTED;
        private bool _RETURN_HAS_RET_BUFFER; // Cached value of HasRetBuffArg
        private uint _fpReturnSize;

        /*        ITERATION_STARTED               = 0x0001,   
                SIZE_OF_ARG_STACK_COMPUTED      = 0x0002,
                RETURN_FLAGS_COMPUTED           = 0x0004,
                RETURN_HAS_RET_BUFFER           = 0x0008,   // Cached value of HasRetBuffArg
        */
        private enum ParamTypeLocation
        {
            Stack,
            Ecx,
            Edx
        }

        private ParamTypeLocation _paramTypeLoc;
        /* X86: PARAM_TYPE_REGISTER_MASK        = 0x0030,
                PARAM_TYPE_REGISTER_STACK       = 0x0010,
                PARAM_TYPE_REGISTER_ECX         = 0x0020,
                PARAM_TYPE_REGISTER_EDX         = 0x0030,*/

        //        METHOD_INVOKE_NEEDS_ACTIVATION  = 0x0040,   // Flag used by ArgIteratorForMethodInvoke

        //        RETURN_FP_SIZE_SHIFT            = 8,        // The rest of the flags is cached value of GetFPReturnSize

        private void ComputeReturnFlags()
        {
            TypeHandle thRetType;
            CorElementType type = GetReturnType(out thRetType, out _RETURN_HAS_RET_BUFFER);

            if (!_RETURN_HAS_RET_BUFFER)
            {
                _transitionBlock.ComputeReturnValueTreatment(type, thRetType, IsVarArg, out _RETURN_HAS_RET_BUFFER, out _fpReturnSize);
            }

            _RETURN_FLAGS_COMPUTED = true;
        }

        public static int ALIGN_UP(int input, int align_to)
        {
            return (input + (align_to - 1)) & ~(align_to - 1);
        }
    };
}
