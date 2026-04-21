// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Provides an abstraction over platform specific calling conventions.
// Ported from crossgen2's ArgIterator.cs.

using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.CallingConvention;

/// <summary>
/// Enumerates method arguments and maps each to a register or stack offset
/// within a <see cref="CallingConventionInfo">TransitionBlock</see>.
/// Ported from crossgen2's <c>ArgIterator</c>.
/// </summary>
internal struct ArgIterator
{
    private readonly CallingConventionInfo _ccInfo;
    private readonly ArgIteratorData _argData;
    private readonly bool _hasThis;
    private readonly bool _hasParamType;
    private readonly bool _hasAsyncContinuation;
    private readonly bool _extraFunctionPointerArg;
    private readonly bool[] _forcedByRefParams;
    private readonly bool _skipFirstArg;
    private readonly bool _extraObjectFirstArg;

    // Iteration state
    private bool _ITERATION_STARTED;
    private bool _SIZE_OF_ARG_STACK_COMPUTED;
    private bool _RETURN_FLAGS_COMPUTED;
    private bool _RETURN_HAS_RET_BUFFER;

    private CorElementType _argType;
    private ArgTypeInfo _argTypeHandle;
    private ArgTypeInfo _argTypeHandleOfByRefParam;
    private int _argSize;
    private int _argNum;
    private bool _argForceByRef;
    private int _nSizeOfArgStack;

    // Per-architecture register allocation state
    // x86
    private int _x86NumRegistersUsed;
    private int _x86OfsStack;

    // x64 Windows
    private int _x64WindowsCurOfs;

    // x64 Unix
    private int _x64UnixIdxGenReg;
    private int _x64UnixIdxStack;
    private int _x64UnixIdxFPReg;

    // ARM32
    private int _armIdxGenReg;
    private int _armOfsStack;
    private ushort _armWFPRegs;
    private bool _armRequires64BitAlignment;

    // ARM64
    private int _arm64IdxGenReg;
    private int _arm64OfsStack;
    private int _arm64IdxFPReg;

    // LoongArch64 / RISC-V64
    private int _rvLa64IdxGenReg;
    private int _rvLa64OfsStack;
    private int _rvLa64IdxFPReg;

    // Struct-in-registers tracking
    private bool _hasArgLocDescForStructInRegs;
#pragma warning disable CS0649 // Assigned in platform-specific paths (ARM64 HFA, Unix AMD64 struct-in-regs)
    private ArgLocDesc _argLocDescForStructInRegs;
#pragma warning restore CS0649

    // x86 param type location
    private enum ParamTypeLocation
    {
        Stack,
        Ecx,
        Edx,
    }
    private ParamTypeLocation _paramTypeLoc;

    private enum AsyncContinuationLocation
    {
        Stack,
        Ecx,
        Edx,
    }
    private AsyncContinuationLocation _asyncContinuationLoc;

    public bool HasThis => _hasThis;
    public bool IsVarArg => _argData.IsVarArg();
    public bool HasParamType => _hasParamType;
    public bool HasAsyncContinuation => _hasAsyncContinuation;
    public int NumFixedArgs => _argData.NumFixedArgs() + (_extraFunctionPointerArg ? 1 : 0) + (_extraObjectFirstArg ? 1 : 0);

    public ArgIterator(
        CallingConventionInfo ccInfo,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation,
        bool[] forcedByRefParams,
        bool skipFirstArg = false,
        bool extraObjectFirstArg = false,
        bool extraFunctionPointerArg = false)
    {
        this = default;
        _ccInfo = ccInfo;
        _argData = argData;
        _hasThis = argData.HasThis();
        _hasParamType = hasParamType;
        _hasAsyncContinuation = hasAsyncContinuation;
        _extraFunctionPointerArg = extraFunctionPointerArg;
        _forcedByRefParams = forcedByRefParams;
        _skipFirstArg = skipFirstArg;
        _extraObjectFirstArg = extraObjectFirstArg;
    }

    public CorElementType GetArgumentType(int argNum, out ArgTypeInfo thArgType, out bool forceByRefReturn)
    {
        forceByRefReturn = false;

        if (_extraObjectFirstArg && argNum == 0)
        {
            thArgType = ArgTypeInfo.ForPrimitive(CorElementType.Class, _ccInfo.PointerSize);
            return CorElementType.Class;
        }

        argNum = _extraObjectFirstArg ? argNum - 1 : argNum;

        if (_forcedByRefParams is not null && (argNum + 1) < _forcedByRefParams.Length)
            forceByRefReturn = _forcedByRefParams[argNum + 1];

        if (_extraFunctionPointerArg && argNum == _argData.NumFixedArgs())
        {
            thArgType = ArgTypeInfo.ForPrimitive(CorElementType.I, _ccInfo.PointerSize);
            return CorElementType.I;
        }

        return _argData.GetArgumentType(argNum, out thArgType);
    }

    public CorElementType GetReturnType(out ArgTypeInfo thRetType, out bool forceByRefReturn)
    {
        forceByRefReturn = _forcedByRefParams is not null && _forcedByRefParams.Length > 0 && _forcedByRefParams[0];
        return _argData.GetReturnType(out thRetType);
    }

    public void Reset()
    {
        _argType = default;
        _argTypeHandle = default;
        _argSize = 0;
        _argNum = 0;
        _argForceByRef = false;
        _ITERATION_STARTED = false;
    }

    private uint SizeOfArgStack()
    {
        if (!_SIZE_OF_ARG_STACK_COMPUTED)
            ForceSigWalk();
        Debug.Assert(_SIZE_OF_ARG_STACK_COMPUTED);
        return (uint)_nSizeOfArgStack;
    }

    public int SizeOfFrameArgumentArray()
    {
        uint size = SizeOfArgStack();

        if (_ccInfo.Architecture == RuntimeInfoArchitecture.X64 && !_ccInfo.IsX64UnixABI)
        {
            size += (uint)_ccInfo.SizeOfArgumentRegisters;
        }

        return (int)size;
    }

    public uint CbStackPop()
    {
        if (_ccInfo.Architecture == RuntimeInfoArchitecture.X86)
        {
            return IsVarArg ? 0 : SizeOfArgStack();
        }
        throw new NotImplementedException();
    }

    public bool HasRetBuffArg()
    {
        if (!_RETURN_FLAGS_COMPUTED)
            ComputeReturnFlags();
        return _RETURN_HAS_RET_BUFFER;
    }

    public int GetThisOffset() => _ccInfo.ThisOffset;

    public int GetVASigCookieOffset()
    {
        Debug.Assert(IsVarArg);

        if (_ccInfo.Architecture == RuntimeInfoArchitecture.X86)
        {
            return (int)_ccInfo.SizeOfTransitionBlock;
        }

        int ret = (int)_ccInfo.ArgumentRegistersOffset;
        if (HasThis)
            ret += _ccInfo.PointerSize;
        if (HasRetBuffArg() && _ccInfo.IsRetBuffPassedAsFirstArg)
            ret += _ccInfo.PointerSize;
        return ret;
    }

    public int GetParamTypeArgOffset()
    {
        Debug.Assert(HasParamType);

        if (_ccInfo.Architecture == RuntimeInfoArchitecture.X86)
        {
            if (!_SIZE_OF_ARG_STACK_COMPUTED)
                ForceSigWalk();

            return _paramTypeLoc switch
            {
                ParamTypeLocation.Ecx => (int)_ccInfo.ArgumentRegistersOffset + _ccInfo.PointerSize, // ECX offset
                ParamTypeLocation.Edx => (int)_ccInfo.ArgumentRegistersOffset, // EDX offset
                _ => (int)_ccInfo.SizeOfTransitionBlock,
            };
        }

        int ret = (int)_ccInfo.ArgumentRegistersOffset;
        if (HasThis) ret += _ccInfo.PointerSize;
        if (HasRetBuffArg() && _ccInfo.IsRetBuffPassedAsFirstArg) ret += _ccInfo.PointerSize;
        return ret;
    }

    public int GetAsyncContinuationArgOffset()
    {
        Debug.Assert(HasAsyncContinuation);

        if (_ccInfo.Architecture == RuntimeInfoArchitecture.X86)
        {
            if (!_SIZE_OF_ARG_STACK_COMPUTED)
                ForceSigWalk();

            return _asyncContinuationLoc switch
            {
                AsyncContinuationLocation.Ecx => (int)_ccInfo.ArgumentRegistersOffset + _ccInfo.PointerSize,
                AsyncContinuationLocation.Edx => (int)_ccInfo.ArgumentRegistersOffset,
                _ => HasParamType && _paramTypeLoc == ParamTypeLocation.Stack
                    ? (int)_ccInfo.SizeOfTransitionBlock + _ccInfo.PointerSize
                    : (int)_ccInfo.SizeOfTransitionBlock,
            };
        }

        int ret = (int)_ccInfo.ArgumentRegistersOffset;
        if (HasThis) ret += _ccInfo.PointerSize;
        if (HasRetBuffArg() && _ccInfo.IsRetBuffPassedAsFirstArg) ret += _ccInfo.PointerSize;
        if (HasParamType) ret += _ccInfo.PointerSize;
        return ret;
    }

    public bool IsArgPassedByRef()
    {
        if (_argForceByRef)
            return true;
        if (_argType == CorElementType.Byref)
            return true;

        if (_ccInfo.EnregisteredParamTypeMaxSize != 0)
        {
            return _ccInfo.Architecture switch
            {
                RuntimeInfoArchitecture.X64 => _ccInfo.IsArgPassedByRef(_argSize),
                RuntimeInfoArchitecture.Arm64 => _argType == CorElementType.ValueType
                    && (_argSize > _ccInfo.EnregisteredParamTypeMaxSize) && (!_argTypeHandle.IsHomogeneousAggregate || IsVarArg),
                RuntimeInfoArchitecture.LoongArch64 or RuntimeInfoArchitecture.RiscV64 => _argType == CorElementType.ValueType
                    && _argSize > _ccInfo.EnregisteredParamTypeMaxSize,
                _ => false,
            };
        }
        return false;
    }

    public ArgLocDesc? GetArgLoc(int _)
    {
        return _hasArgLocDescForStructInRegs ? _argLocDescForStructInRegs : null;
    }

    public int GetArgSize() => _argSize;

    /// <summary>
    /// Returns the next argument's offset in the transition block, or
    /// <see cref="CallingConventionInfo.InvalidOffset"/> when all arguments
    /// have been enumerated.
    /// </summary>
    public int GetNextOffset()
    {
        if (!_ITERATION_STARTED)
        {
            int numRegistersUsed = 0;

            if (HasThis) numRegistersUsed++;
            if (HasRetBuffArg() && _ccInfo.IsRetBuffPassedAsFirstArg) numRegistersUsed++;

            Debug.Assert(!IsVarArg || !HasParamType);

            if (_ccInfo.Architecture != RuntimeInfoArchitecture.X86)
            {
                if (HasParamType) numRegistersUsed++;
                if (HasAsyncContinuation) numRegistersUsed++;
            }

            if (_ccInfo.Architecture != RuntimeInfoArchitecture.X86 && IsVarArg)
                numRegistersUsed++;

            switch (_ccInfo.Architecture)
            {
                case RuntimeInfoArchitecture.X86:
                    if (IsVarArg) numRegistersUsed = _ccInfo.NumArgumentRegisters;
                    _x86NumRegistersUsed = numRegistersUsed;
                    _x86OfsStack = (int)(_ccInfo.OffsetOfArgs + SizeOfArgStack());
                    break;

                case RuntimeInfoArchitecture.X64:
                    if (_ccInfo.IsX64UnixABI)
                    {
                        _x64UnixIdxGenReg = numRegistersUsed;
                        _x64UnixIdxStack = 0;
                        _x64UnixIdxFPReg = 0;
                    }
                    else
                    {
                        _x64WindowsCurOfs = (int)_ccInfo.OffsetOfArgs + numRegistersUsed * _ccInfo.PointerSize;
                    }
                    break;

                case RuntimeInfoArchitecture.Arm:
                    _armIdxGenReg = numRegistersUsed;
                    _armOfsStack = 0;
                    _armWFPRegs = 0;
                    break;

                case RuntimeInfoArchitecture.Arm64:
                    _arm64IdxGenReg = numRegistersUsed;
                    _arm64OfsStack = 0;
                    _arm64IdxFPReg = 0;
                    break;

                case RuntimeInfoArchitecture.LoongArch64:
                case RuntimeInfoArchitecture.RiscV64:
                    _rvLa64IdxGenReg = numRegistersUsed;
                    _rvLa64OfsStack = 0;
                    _rvLa64IdxFPReg = 0;
                    break;

                default:
                    throw new NotSupportedException(_ccInfo.Architecture.ToString());
            }

            _argNum = _skipFirstArg ? 1 : 0;
            _ITERATION_STARTED = true;
        }

        if (_argNum >= NumFixedArgs)
            return CallingConventionInfo.InvalidOffset;

        CorElementType argType = GetArgumentType(_argNum, out _argTypeHandle, out _argForceByRef);
        _argTypeHandleOfByRefParam = argType == CorElementType.Byref ? _argData.GetByRefArgumentType(_argNum) : default;
        _argNum++;

        int argSize = ArgTypeInfo.GetElemSize(argType, _argTypeHandle, _ccInfo.PointerSize);
        _argType = argType;
        _argSize = argSize;

        argType = _argForceByRef ? CorElementType.Byref : argType;
        argSize = _argForceByRef ? _ccInfo.PointerSize : argSize;

        _hasArgLocDescForStructInRegs = false;

        switch (_ccInfo.Architecture)
        {
            case RuntimeInfoArchitecture.X64:
                return GetNextOffsetX64(argType, argSize);

            case RuntimeInfoArchitecture.Arm64:
                return GetNextOffsetArm64(argType, argSize);

            case RuntimeInfoArchitecture.X86:
                return GetNextOffsetX86(argType, argSize);

            case RuntimeInfoArchitecture.Arm:
                return GetNextOffsetArm32(argType, argSize);

            case RuntimeInfoArchitecture.LoongArch64:
            case RuntimeInfoArchitecture.RiscV64:
                return GetNextOffsetRiscVLoongArch(argType, argSize);

            default:
                throw new NotSupportedException(_ccInfo.Architecture.ToString());
        }
    }

    // ---- Per-architecture GetNextOffset implementations ----
    // These match crossgen2's ArgIterator.GetNextOffset() switch cases.

    private int GetNextOffsetX64(CorElementType argType, int argSize)
    {
        if (_ccInfo.IsX64UnixABI)
        {
            // TODO: Full Unix AMD64 implementation with SystemV struct classification
            // For now, simplified: all args go through GP regs then stack
            int cbArg = _ccInfo.StackElemSize(argSize);
            int cGenRegs = cbArg / 8;

            if (argType is CorElementType.R4 or CorElementType.R8)
            {
                if (_x64UnixIdxFPReg < _ccInfo.NumFloatArgumentRegisters)
                {
                    int argOfs = _ccInfo.OffsetOfFloatArgumentRegisters + _x64UnixIdxFPReg * 8;
                    _x64UnixIdxFPReg++;
                    return argOfs;
                }
            }
            else if (cGenRegs > 0 && _x64UnixIdxGenReg + cGenRegs <= _ccInfo.NumArgumentRegisters)
            {
                int argOfs = (int)_ccInfo.ArgumentRegistersOffset + _x64UnixIdxGenReg * 8;
                _x64UnixIdxGenReg += cGenRegs;
                return argOfs;
            }

            int stackOfs = (int)_ccInfo.OffsetOfArgs + _x64UnixIdxStack * 8;
            _x64UnixIdxStack += _ccInfo.StackElemSize(argSize) / _ccInfo.PointerSize;
            return stackOfs;
        }
        else
        {
            // Windows x64: each arg takes exactly one slot
            int cFPRegs = argType is CorElementType.R4 or CorElementType.R8 ? 1 : 0;

            int argOfs = _x64WindowsCurOfs - (int)_ccInfo.OffsetOfArgs;
            _x64WindowsCurOfs += _ccInfo.PointerSize;

            if (cFPRegs == 0 || argOfs >= _ccInfo.SizeOfArgumentRegisters)
            {
                return argOfs + (int)_ccInfo.OffsetOfArgs;
            }
            else
            {
                int idxFpReg = argOfs / _ccInfo.PointerSize;
                return _ccInfo.OffsetOfFloatArgumentRegisters + idxFpReg * 16; // SizeOfM128A
            }
        }
    }

    private int GetNextOffsetArm64(CorElementType argType, int argSize)
    {
        int cFPRegs = 0;
        bool isFloatHFA = false;

        switch (argType)
        {
            case CorElementType.R4:
            case CorElementType.R8:
                cFPRegs = 1;
                break;

            case CorElementType.ValueType:
                if (_argTypeHandle.IsHomogeneousAggregate)
                {
                    int haElementSize = _argTypeHandle.HomogeneousAggregateElementSize;
                    if (haElementSize == 4) isFloatHFA = true;
                    cFPRegs = argSize / haElementSize;
                }
                else if (argSize > _ccInfo.EnregisteredParamTypeMaxSize)
                {
                    argSize = _ccInfo.PointerSize;
                }
                break;
        }

        bool isValueType = argType == CorElementType.ValueType;
        int cbArg = _ccInfo.StackElemSize(argSize, isValueType, isFloatHFA);

        if (cFPRegs > 0 && !IsVarArg)
        {
            if (cFPRegs + _arm64IdxFPReg <= 8)
            {
                int argOfs = _ccInfo.OffsetOfFloatArgumentRegisters + _arm64IdxFPReg * 16;
                _arm64IdxFPReg += cFPRegs;
                return argOfs;
            }
            else
            {
                _arm64IdxFPReg = 8;
            }
        }
        else
        {
            int regSlots = CallingConventionInfo.AlignUp(cbArg, _ccInfo.PointerSize) / _ccInfo.PointerSize;
            if (_arm64IdxGenReg + regSlots <= 8)
            {
                int argOfs = (int)_ccInfo.ArgumentRegistersOffset + _arm64IdxGenReg * 8;
                _arm64IdxGenReg += regSlots;
                return argOfs;
            }
            else
            {
                _arm64IdxGenReg = 8;
            }
        }

        if (_ccInfo.IsAppleArm64ABI)
        {
            int alignment = isValueType ? (isFloatHFA ? 4 : 8) : cbArg;
            _arm64OfsStack = CallingConventionInfo.AlignUp(_arm64OfsStack, alignment);
        }

        int result = (int)_ccInfo.OffsetOfArgs + _arm64OfsStack;
        _arm64OfsStack += cbArg;
        return result;
    }

    private int GetNextOffsetX86(CorElementType argType, int argSize)
    {
        if (_x86NumRegistersUsed < _ccInfo.NumArgumentRegisters
            && argType is not CorElementType.ValueType
                and not CorElementType.R4
                and not CorElementType.R8
                and not CorElementType.I8
                and not CorElementType.U8)
        {
            _x86NumRegistersUsed++;
            return (int)_ccInfo.ArgumentRegistersOffset +
                (_ccInfo.NumArgumentRegisters - _x86NumRegistersUsed) * _ccInfo.PointerSize;
        }

        int cbArg = _ccInfo.StackElemSize(argSize);
        _x86OfsStack -= cbArg;
        return _x86OfsStack;
    }

    private int GetNextOffsetArm32(CorElementType argType, int argSize)
    {
        bool fFloatingPoint = false;
        bool fRequiresAlign64Bit = false;

        switch (argType)
        {
            case CorElementType.I8:
            case CorElementType.U8:
                fRequiresAlign64Bit = true;
                break;
            case CorElementType.R4:
                fFloatingPoint = true;
                break;
            case CorElementType.R8:
                fFloatingPoint = true;
                fRequiresAlign64Bit = true;
                break;
            case CorElementType.ValueType:
                fRequiresAlign64Bit = _argTypeHandle.RequiresAlign8;
                if (_argTypeHandle.IsHomogeneousAggregate) fFloatingPoint = true;
                break;
        }

        _armRequires64BitAlignment = fRequiresAlign64Bit;
        int cbArg = _ccInfo.StackElemSize(argSize);

        if (fFloatingPoint && _ccInfo.IsArmhfABI && !IsVarArg)
        {
            ushort wAllocMask = checked((ushort)((1 << (cbArg / 4)) - 1));
            ushort cSteps = (ushort)(fRequiresAlign64Bit ? 9 - (cbArg / 8) : 17 - (cbArg / 4));
            ushort cShift = fRequiresAlign64Bit ? (ushort)2 : (ushort)1;

            for (ushort i = 0; i < cSteps; i++)
            {
                if ((_armWFPRegs & wAllocMask) == 0)
                {
                    _armWFPRegs |= wAllocMask;
                    return _ccInfo.OffsetOfFloatArgumentRegisters + (i * cShift * 4);
                }
                wAllocMask <<= cShift;
            }

            _armWFPRegs = 0xffff;

            if (fRequiresAlign64Bit)
                _armOfsStack = CallingConventionInfo.AlignUp(_armOfsStack, _ccInfo.PointerSize * 2);

            int argOfs = (int)_ccInfo.OffsetOfArgs + _armOfsStack;
            _armOfsStack += cbArg;
            return argOfs;
        }

        if (_armIdxGenReg < 4)
        {
            if (fRequiresAlign64Bit)
                _armIdxGenReg = CallingConventionInfo.AlignUp(_armIdxGenReg, 2);

            int argOfs = (int)_ccInfo.ArgumentRegistersOffset + _armIdxGenReg * 4;
            int cRemainingRegs = 4 - _armIdxGenReg;

            if (cbArg <= cRemainingRegs * _ccInfo.PointerSize)
            {
                _armIdxGenReg += CallingConventionInfo.AlignUp(cbArg, _ccInfo.PointerSize) / _ccInfo.PointerSize;
                return argOfs;
            }

            _armIdxGenReg = 4;

            if (_armOfsStack == 0)
            {
                _armOfsStack += cbArg - cRemainingRegs * _ccInfo.PointerSize;
                return argOfs;
            }
        }

        if (fRequiresAlign64Bit)
            _armOfsStack = CallingConventionInfo.AlignUp(_armOfsStack, _ccInfo.PointerSize * 2);

        int result = (int)_ccInfo.OffsetOfArgs + _armOfsStack;
        _armOfsStack += cbArg;
        return result;
    }

    private int GetNextOffsetRiscVLoongArch(CorElementType argType, int argSize)
    {
        // Simplified: no FP struct detection, just use integer calling convention
        int cFPRegs = argType is CorElementType.R4 or CorElementType.R8 ? 1 : 0;

        if (argType == CorElementType.ValueType && argSize > _ccInfo.EnregisteredParamTypeMaxSize)
            argSize = _ccInfo.PointerSize;

        int cbArg = _ccInfo.StackElemSize(argSize);

        if (cFPRegs > 0 && !IsVarArg && cFPRegs + _rvLa64IdxFPReg <= _ccInfo.NumFloatArgumentRegisters)
        {
            int argOfs = _ccInfo.OffsetOfFloatArgumentRegisters + _rvLa64IdxFPReg * _ccInfo.FloatRegisterSize;
            _rvLa64IdxFPReg += cFPRegs;
            return argOfs;
        }

        int regSlots = CallingConventionInfo.AlignUp(cbArg, _ccInfo.PointerSize) / _ccInfo.PointerSize;
        if (_rvLa64IdxGenReg + regSlots <= _ccInfo.NumArgumentRegisters)
        {
            int argOfs = (int)_ccInfo.ArgumentRegistersOffset + _rvLa64IdxGenReg * _ccInfo.PointerSize;
            _rvLa64IdxGenReg += regSlots;
            return argOfs;
        }
        else
        {
            _rvLa64IdxGenReg = _ccInfo.NumArgumentRegisters;
        }

        int result = (int)_ccInfo.OffsetOfArgs + _rvLa64OfsStack;
        _rvLa64OfsStack += cbArg;
        return result;
    }

    // ---- Return type computation ----

    private void ComputeReturnFlags()
    {
        _RETURN_FLAGS_COMPUTED = true;
        CorElementType retType = GetReturnType(out ArgTypeInfo thRetType, out bool forceByRef);

        if (forceByRef)
        {
            _RETURN_HAS_RET_BUFFER = true;
            return;
        }

        switch (retType)
        {
            case CorElementType.TypedByRef:
                _RETURN_HAS_RET_BUFFER = true;
                break;

            case CorElementType.ValueType:
                if (thRetType.Size > _ccInfo.EnregisteredParamTypeMaxSize && _ccInfo.EnregisteredParamTypeMaxSize > 0)
                {
                    _RETURN_HAS_RET_BUFFER = true;
                }
                else if (_ccInfo.Architecture is RuntimeInfoArchitecture.X86 or RuntimeInfoArchitecture.X64)
                {
                    int size = thRetType.Size;
                    if ((size & (size - 1)) != 0) // not power of 2
                        _RETURN_HAS_RET_BUFFER = true;
                }
                break;
        }
    }

    private void ForceSigWalk()
    {
        Debug.Assert(!_ITERATION_STARTED);

        int numRegistersUsed = 0;
        int nSizeOfArgStack = 0;

        if (_ccInfo.Architecture == RuntimeInfoArchitecture.X86)
        {
            if (HasThis) numRegistersUsed++;
            if (HasRetBuffArg() && _ccInfo.IsRetBuffPassedAsFirstArg) numRegistersUsed++;
            if (IsVarArg)
            {
                nSizeOfArgStack += _ccInfo.PointerSize;
                numRegistersUsed = _ccInfo.NumArgumentRegisters;
            }

            int nArgs = NumFixedArgs;
            for (int i = _skipFirstArg ? 1 : 0; i < nArgs; i++)
            {
                CorElementType type = GetArgumentType(i, out ArgTypeInfo thArgType, out bool argForced);
                if (argForced) type = CorElementType.Byref;

                // Simplified: assume all non-trivial types go to stack
                int structSize = ArgTypeInfo.GetElemSize(type, thArgType, _ccInfo.PointerSize);
                nSizeOfArgStack += _ccInfo.StackElemSize(structSize);
            }

            if (HasAsyncContinuation)
            {
                if (numRegistersUsed < _ccInfo.NumArgumentRegisters)
                {
                    numRegistersUsed++;
                    _asyncContinuationLoc = numRegistersUsed == 1 ? AsyncContinuationLocation.Ecx : AsyncContinuationLocation.Edx;
                }
                else
                {
                    nSizeOfArgStack += _ccInfo.PointerSize;
                    _asyncContinuationLoc = AsyncContinuationLocation.Stack;
                }
            }

            if (HasParamType)
            {
                if (numRegistersUsed < _ccInfo.NumArgumentRegisters)
                {
                    numRegistersUsed++;
                    _paramTypeLoc = numRegistersUsed == 1 ? ParamTypeLocation.Ecx : ParamTypeLocation.Edx;
                }
                else
                {
                    nSizeOfArgStack += _ccInfo.PointerSize;
                    _paramTypeLoc = ParamTypeLocation.Stack;
                }
            }
        }
        else
        {
            // Non-x86: iterate through GetNextOffset to compute stack size
            int maxOffset = (int)_ccInfo.OffsetOfArgs;
            int ofs;
            while (CallingConventionInfo.InvalidOffset != (ofs = GetNextOffset()))
            {
                int stackElemSize;
                if (_ccInfo.Architecture == RuntimeInfoArchitecture.X64)
                {
                    stackElemSize = _ccInfo.IsX64UnixABI
                        ? _ccInfo.StackElemSize(GetArgSize())
                        : _ccInfo.PointerSize;
                }
                else
                {
                    stackElemSize = _ccInfo.StackElemSize(GetArgSize());
                }

                int endOfs = ofs + stackElemSize;
                if (IsArgumentRegisterOffset(ofs))
                    continue;
                if (CallingConventionInfo.IsFloatArgumentRegisterOffset(ofs))
                    continue;
                if (ofs == CallingConventionInfo.StructInRegsOffset)
                    continue;
                if (endOfs > maxOffset)
                    maxOffset = endOfs;
            }

            nSizeOfArgStack = maxOffset - (int)_ccInfo.OffsetOfArgs;
            Reset();
        }

        _nSizeOfArgStack = nSizeOfArgStack;
        _SIZE_OF_ARG_STACK_COMPUTED = true;
    }

    private bool IsArgumentRegisterOffset(int offset)
    {
        return _ccInfo.IsArgumentRegisterOffset(offset);
    }
}
