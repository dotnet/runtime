// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// Shared ABI-independent logic for cDAC argument iterators.
/// </summary>
/// <remarks>
/// Architecture-specific iterators provide register counts, stack-slot sizing, and
/// per-argument location enumeration, while this base class handles hidden argument
/// bookkeeping, return-buffer decisions, and lazy stack-size computation.
/// </remarks>
internal abstract class ArgIteratorBase
{
    protected readonly TransitionBlockLayout _layout;
    protected readonly ArgIteratorData _argData;

    private readonly bool _hasThis;
    private readonly bool _hasParamType;
    private readonly bool _hasAsyncContinuation;

    protected bool _SIZE_OF_ARG_STACK_COMPUTED;
    protected int _nSizeOfArgStack;

    private bool _RETURN_FLAGS_COMPUTED;
    private bool _RETURN_HAS_RET_BUFFER;

    #region Construction

    /// <summary>
    /// Initializes a new iterator over a method signature using the supplied transition-block layout.
    /// </summary>
    protected ArgIteratorBase(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
    {
        _layout = layout;
        _argData = argData;
        _hasThis = argData.HasThis();
        _hasParamType = hasParamType;
        _hasAsyncContinuation = hasAsyncContinuation;
    }

    #endregion

    #region ABI characteristics

    public abstract int NumArgumentRegisters { get; }
    public abstract int NumFloatArgumentRegisters { get; }
    public abstract int FloatRegisterSize { get; }
    public abstract int EnregisteredParamTypeMaxSize { get; }
    public abstract int EnregisteredReturnTypeIntegerMaxSize { get; }
    public abstract int StackSlotSize { get; }
    public abstract bool IsRetBuffPassedAsFirstArg { get; }

    public bool HasThis => _hasThis;
    public bool IsVarArg => _argData.IsVarArg();
    public bool HasParamType => _hasParamType;
    public bool HasAsyncContinuation => _hasAsyncContinuation;
    public int NumFixedArgs => _argData.NumFixedArgs();
    public int SizeOfArgumentRegisters => NumArgumentRegisters * _layout.PointerSize;

    #endregion

    #region Hidden arguments

    /// <summary>
    /// Gets the transition-block offset of the hidden <c>this</c> argument.
    /// </summary>
    public virtual int GetThisOffset()
        => _layout.ArgumentRegistersOffset;

    public virtual int OffsetFromGCRefMapPos(int pos)
        => _layout.FirstGCRefMapSlot + pos * _layout.PointerSize;

    public virtual int GetRetBuffArgOffset(bool hasThis)
        => _layout.ArgumentRegistersOffset + (hasThis ? StackElemSize(_layout.PointerSize) : 0);

    public virtual int GetVASigCookieOffset()
    {
        Debug.Assert(IsVarArg);

        int offset = _layout.ArgumentRegistersOffset;
        int slotSize = StackElemSize(_layout.PointerSize);

        if (HasThis)
        {
            offset += slotSize;
        }

        if (HasRetBuffArg() && IsRetBuffPassedAsFirstArg)
        {
            offset += slotSize;
        }

        return offset;
    }

    public virtual int GetParamTypeArgOffset()
    {
        Debug.Assert(HasParamType);

        int offset = _layout.ArgumentRegistersOffset;
        int slotSize = StackElemSize(_layout.PointerSize);

        if (HasThis)
        {
            offset += slotSize;
        }

        if (HasRetBuffArg() && IsRetBuffPassedAsFirstArg)
        {
            offset += slotSize;
        }

        return offset;
    }

    public virtual int GetAsyncContinuationArgOffset()
    {
        Debug.Assert(HasAsyncContinuation);

        int offset = _layout.ArgumentRegistersOffset;
        int slotSize = StackElemSize(_layout.PointerSize);

        if (HasThis)
        {
            offset += slotSize;
        }

        if (HasRetBuffArg() && IsRetBuffPassedAsFirstArg)
        {
            offset += slotSize;
        }

        if (HasParamType)
        {
            offset += slotSize;
        }

        return offset;
    }

    public virtual int SizeOfFrameArgumentArray()
        => checked((int)SizeOfArgStack());

    public virtual uint CbStackPop()
        => 0;

    #endregion

    #region Argument sizing and iteration

    public virtual int StackElemSize(int parmSize, bool isValueType = false, bool isFloatHfa = false)
        => AlignUp(parmSize, StackSlotSize);

    public virtual bool IsArgPassedByRefBySize(int size)
        => size > EnregisteredParamTypeMaxSize;

    /// <summary>
    /// Computes the number of register slots consumed by hidden arguments before user arguments begin.
    /// </summary>
    protected virtual int ComputeInitialNumRegistersUsed()
    {
        int numRegistersUsed = 0;

        if (HasThis)
        {
            numRegistersUsed++;
        }

        if (HasRetBuffArg() && IsRetBuffPassedAsFirstArg)
        {
            numRegistersUsed++;
        }

        Debug.Assert(!IsVarArg || !HasParamType);

        if (HasParamType)
        {
            numRegistersUsed++;
        }

        if (HasAsyncContinuation)
        {
            numRegistersUsed++;
        }

        if (IsVarArg)
        {
            numRegistersUsed++;
        }

        return numRegistersUsed;
    }

    /// <summary>
    /// Enumerates the fixed user-visible arguments and their locations.
    /// </summary>
    public abstract IEnumerable<ArgLocDesc> EnumerateArgs();

    #endregion

    #region Signature inspection

    /// <summary>
    /// Gets the argument type at the specified user-visible argument index.
    /// </summary>
    public CorElementType GetArgumentType(int argNum, out ArgTypeInfo thArgType)
    {
        return _argData.GetArgumentType(argNum, out thArgType);
    }

    /// <summary>
    /// Gets the method return type.
    /// </summary>
    public CorElementType GetReturnType(out ArgTypeInfo thRetType)
        => _argData.GetReturnType(out thRetType);

    #endregion

    #region Return handling

    /// <summary>
    /// Determines whether the signature uses a hidden return-buffer argument.
    /// </summary>
    public bool HasRetBuffArg()
    {
        if (!_RETURN_FLAGS_COMPUTED)
        {
            ComputeReturnFlags();
        }

        return _RETURN_HAS_RET_BUFFER;
    }

    /// <summary>
    /// Default return-buffer policy for value-type returns.
    /// </summary>
    protected virtual bool ValueTypeReturnNeedsRetBuf(ArgTypeInfo thRetType)
    {
        int size = thRetType.Size;
        if (size > EnregisteredReturnTypeIntegerMaxSize)
        {
            return true;
        }

        if (_layout.Architecture is RuntimeInfoArchitecture.X86 or RuntimeInfoArchitecture.X64)
        {
            return size <= 0 || !BitOperations.IsPow2((uint)size);
        }

        return false;
    }

    private void ComputeReturnFlags()
    {
        CorElementType type = GetReturnType(out ArgTypeInfo thRetType);
        _RETURN_HAS_RET_BUFFER = type switch
        {
            CorElementType.TypedByRef => true,
            CorElementType.ValueType => ValueTypeReturnNeedsRetBuf(thRetType),
            _ => false,
        };
        _RETURN_FLAGS_COMPUTED = true;
    }

    #endregion

    #region Stack sizing

    /// <summary>
    /// Gets the total stack space consumed by user arguments above the transition block.
    /// </summary>
    protected uint SizeOfArgStack()
    {
        if (!_SIZE_OF_ARG_STACK_COMPUTED)
        {
            ForceSigWalk();
        }

        Debug.Assert(_SIZE_OF_ARG_STACK_COMPUTED);
        Debug.Assert((_nSizeOfArgStack % _layout.PointerSize) == 0);
        return (uint)_nSizeOfArgStack;
    }

    private void ForceSigWalk()
    {
        ComputeSizeOfArgStack();
        _SIZE_OF_ARG_STACK_COMPUTED = true;
    }

    /// <summary>
    /// Computes the stack footprint by walking the argument locations produced by <see cref="EnumerateArgs"/>.
    /// </summary>
    protected virtual void ComputeSizeOfArgStack()
    {
        int maxOffset = _layout.OffsetOfArgs;
        foreach (ArgLocDesc arg in EnumerateArgs())
        {
            foreach (ArgLocation location in arg.Locations)
            {
                if (location.Kind != ArgLocationKind.Stack)
                {
                    continue;
                }

                int endOffset = location.TransitionBlockOffset + location.Size;
                if (endOffset > maxOffset)
                {
                    maxOffset = endOffset;
                }
            }
        }

        int sizeOfArgStack = maxOffset - _layout.OffsetOfArgs;
        if (_layout.Architecture == RuntimeInfoArchitecture.X64 &&
            _layout.OperatingSystem == RuntimeInfoOperatingSystem.Windows)
        {
            sizeOfArgStack = sizeOfArgStack > SizeOfArgumentRegisters
                ? sizeOfArgStack - SizeOfArgumentRegisters
                : 0;
        }

        _nSizeOfArgStack = AlignUp(sizeOfArgStack, StackElemSize(_layout.PointerSize));
    }

    #endregion

    #region Helpers

    public static int AlignUp(int input, int alignTo)
        => (input + (alignTo - 1)) & ~(alignTo - 1);

    public static int GetElemSize(CorElementType elementType, ArgTypeInfo typeInfo, int pointerSize)
        => ArgTypeInfo.GetElemSize(elementType, typeInfo, pointerSize);

    #endregion
}
