// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// x86 managed calling-convention iterator. User arguments are enregistered into
/// ECX/EDX when eligible; remaining arguments are laid out on the stack in the
/// native x86 reverse-push order.
/// </summary>
internal sealed class X86ArgIterator : ArgIteratorBase
{
    private enum ParamTypeLocation
    {
        Stack,
        Ecx,
        Edx,
    }

    private enum AsyncContinuationLocation
    {
        Stack,
        Ecx,
        Edx,
    }

    private ParamTypeLocation _paramTypeLoc;
    private AsyncContinuationLocation _asyncContinuationLoc;

    public override int NumArgumentRegisters => 2;
    public override int NumFloatArgumentRegisters => 0;
    public override int FloatRegisterSize => 0;
    public override int EnregisteredParamTypeMaxSize => 0;
    public override int EnregisteredReturnTypeIntegerMaxSize => 4;
    public override int StackSlotSize => 4;
    public override bool IsRetBuffPassedAsFirstArg => true;

    public X86ArgIterator(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
        : base(layout, argData, hasParamType, hasAsyncContinuation)
    {
    }

    public override int GetThisOffset()
        => _layout.ArgumentRegistersOffset + _layout.PointerSize;

    public override int OffsetFromGCRefMapPos(int pos)
    {
        if (pos < NumArgumentRegisters)
        {
            return _layout.FirstGCRefMapSlot + SizeOfArgumentRegisters - ((pos + 1) * _layout.PointerSize);
        }

        return _layout.OffsetOfArgs + ((pos - NumArgumentRegisters) * _layout.PointerSize);
    }

    public override int GetRetBuffArgOffset(bool hasThis)
        => _layout.ArgumentRegistersOffset + (hasThis ? 0 : _layout.PointerSize);

    public override uint CbStackPop()
        => IsVarArg ? 0u : SizeOfArgStack();

    public override int GetVASigCookieOffset()
    {
        Debug.Assert(IsVarArg);
        return _layout.SizeOfTransitionBlock;
    }

    public override int GetParamTypeArgOffset()
    {
        Debug.Assert(HasParamType);
        _ = SizeOfArgStack();

        return _paramTypeLoc switch
        {
            ParamTypeLocation.Ecx => _layout.ArgumentRegistersOffset + _layout.PointerSize,
            ParamTypeLocation.Edx => _layout.ArgumentRegistersOffset,
            _ => _layout.SizeOfTransitionBlock,
        };
    }

    public override int GetAsyncContinuationArgOffset()
    {
        Debug.Assert(HasAsyncContinuation);
        _ = SizeOfArgStack();

        return _asyncContinuationLoc switch
        {
            AsyncContinuationLocation.Ecx => _layout.ArgumentRegistersOffset + _layout.PointerSize,
            AsyncContinuationLocation.Edx => _layout.ArgumentRegistersOffset,
            _ => HasParamType && _paramTypeLoc == ParamTypeLocation.Stack
                ? _layout.SizeOfTransitionBlock + _layout.PointerSize
                : _layout.SizeOfTransitionBlock,
        };
    }

    protected override int ComputeInitialNumRegistersUsed()
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

        return numRegistersUsed;
    }

    protected override void ComputeSizeOfArgStack()
    {
        int numRegistersUsed = 0;
        int sizeOfArgStack = 0;

        if (HasThis)
        {
            numRegistersUsed++;
        }

        if (HasRetBuffArg() && IsRetBuffPassedAsFirstArg)
        {
            numRegistersUsed++;
        }

        if (IsVarArg)
        {
            sizeOfArgStack += _layout.PointerSize;
            numRegistersUsed = NumArgumentRegisters;
        }

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = GetElemSize(argType, argTypeInfo, _layout.PointerSize);
            if (!IsArgumentInRegister(ref numRegistersUsed, argType, argSize))
            {
                sizeOfArgStack += StackElemSize(argSize);
            }
        }

        if (HasAsyncContinuation)
        {
            if (numRegistersUsed < NumArgumentRegisters)
            {
                numRegistersUsed++;
                _asyncContinuationLoc = numRegistersUsed == 1
                    ? AsyncContinuationLocation.Ecx
                    : AsyncContinuationLocation.Edx;
            }
            else
            {
                sizeOfArgStack += _layout.PointerSize;
                _asyncContinuationLoc = AsyncContinuationLocation.Stack;
            }
        }

        if (HasParamType)
        {
            if (numRegistersUsed < NumArgumentRegisters)
            {
                numRegistersUsed++;
                _paramTypeLoc = numRegistersUsed == 1
                    ? ParamTypeLocation.Ecx
                    : ParamTypeLocation.Edx;
            }
            else
            {
                sizeOfArgStack += _layout.PointerSize;
                _paramTypeLoc = ParamTypeLocation.Stack;
            }
        }

        _nSizeOfArgStack = AlignUp(sizeOfArgStack, StackElemSize(_layout.PointerSize));
    }

    public override IEnumerable<ArgLocDesc> EnumerateArgs()
    {
        int stackSize = (int)SizeOfArgStack();
        int numRegistersUsed = ComputeInitialNumRegistersUsed();
        int ofsStack = _layout.OffsetOfArgs + stackSize;

        if (IsVarArg)
        {
            numRegistersUsed = NumArgumentRegisters;
        }

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = GetElemSize(argType, argTypeInfo, _layout.PointerSize);

            ArgLocation location;
            if (IsArgumentInRegister(ref numRegistersUsed, argType, argSize))
            {
                location = new ArgLocation
                {
                    Kind = ArgLocationKind.GpRegister,
                    TransitionBlockOffset = _layout.ArgumentRegistersOffset + ((NumArgumentRegisters - numRegistersUsed) * _layout.PointerSize),
                    Size = _layout.PointerSize,
                    ElementType = argType,
                };
            }
            else
            {
                int stackElemSize = StackElemSize(argSize);
                ofsStack -= stackElemSize;
                location = new ArgLocation
                {
                    Kind = ArgLocationKind.Stack,
                    TransitionBlockOffset = ofsStack,
                    Size = stackElemSize,
                    ElementType = argType,
                };
            }

            yield return new ArgLocDesc
            {
                ArgType = argType,
                ArgSize = argSize,
                ArgTypeInfo = argTypeInfo,
                IsByRef = argType == CorElementType.Byref,
                Locations = [location],
            };
        }
    }

    private static bool IsArgumentInRegister(ref int numRegistersUsed, CorElementType elementType, int argSize)
    {
        if (numRegistersUsed >= 2)
        {
            return false;
        }

        bool enregister = elementType switch
        {
            CorElementType.Boolean or
            CorElementType.Char or
            CorElementType.I1 or
            CorElementType.U1 or
            CorElementType.I2 or
            CorElementType.U2 or
            CorElementType.I4 or
            CorElementType.U4 or
            CorElementType.I or
            CorElementType.U or
            CorElementType.Ptr or
            CorElementType.Byref or
            CorElementType.Class or
            CorElementType.Object or
            CorElementType.String or
            CorElementType.SzArray or
            CorElementType.Array or
            CorElementType.FnPtr => true,
            CorElementType.ValueType => argSize is 1 or 2 or 4,
            CorElementType.R4 or
            CorElementType.R8 or
            CorElementType.I8 or
            CorElementType.U8 or
            CorElementType.TypedByRef => false,
            _ => false,
        };

        if (!enregister)
        {
            return false;
        }

        numRegistersUsed++;
        return true;
    }
}
