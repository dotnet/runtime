// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

internal sealed class AMD64WindowsArgIterator : ArgIteratorBase
{
    public override int NumArgumentRegisters => 4;   // RCX, RDX, R8, R9
    public override int NumFloatArgumentRegisters => 0; // Shared with GP regs on Windows
    public override int FloatRegisterSize => 16;
    public override int EnregisteredParamTypeMaxSize => 8;
    public override int EnregisteredReturnTypeIntegerMaxSize => 8;   // RAX
    public override int StackSlotSize => 8;
    public override bool IsRetBuffPassedAsFirstArg => true;

    public AMD64WindowsArgIterator(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
        : base(layout, argData, hasParamType, hasAsyncContinuation)
    {
    }

    public override int SizeOfFrameArgumentArray()
        => (int)SizeOfArgStack() + SizeOfArgumentRegisters;

    public override bool IsArgPassedByRefBySize(int size)
        => size > EnregisteredParamTypeMaxSize || !BitOperations.IsPow2(size);

    public override IEnumerable<ArgLocDesc> EnumerateArgs()
    {
        int curOfs = (int)_layout.OffsetOfArgs + ComputeInitialNumRegistersUsed() * _layout.PointerSize;

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = ArgTypeInfo.GetElemSize(argType, argTypeInfo, _layout.PointerSize);

            int cFPRegs = argType is CorElementType.R4 or CorElementType.R8 ? 1 : 0;
            int argOfs = curOfs - (int)_layout.OffsetOfArgs;
            curOfs += _layout.PointerSize;

            ArgLocation location;
            if (cFPRegs == 0 || argOfs >= SizeOfArgumentRegisters)
            {
                ArgLocationKind kind = argOfs < SizeOfArgumentRegisters
                    ? ArgLocationKind.GpRegister
                    : ArgLocationKind.Stack;
                int size = kind == ArgLocationKind.GpRegister
                    ? _layout.PointerSize
                    : StackElemSize(argSize);
                location = new ArgLocation
                {
                    Kind = kind,
                    TransitionBlockOffset = argOfs + (int)_layout.OffsetOfArgs,
                    Size = size,
                    ElementType = argType,
                };
            }
            else
            {
                int idxFpReg = argOfs / _layout.PointerSize;
                location = new ArgLocation
                {
                    Kind = ArgLocationKind.FpRegister,
                    TransitionBlockOffset = _layout.OffsetOfFloatArgumentRegisters + idxFpReg * FloatRegisterSize,
                    Size = FloatRegisterSize,
                    ElementType = argType,
                };
            }

            bool isByRef = argType == CorElementType.Byref
                || (argType == CorElementType.ValueType && IsArgPassedByRefBySize(argSize));

            yield return new ArgLocDesc
            {
                ArgType = argType,
                ArgSize = argSize,
                ArgTypeInfo = argTypeInfo,
                IsByRef = isByRef,
                Locations = [location],
            };
        }
    }
}
