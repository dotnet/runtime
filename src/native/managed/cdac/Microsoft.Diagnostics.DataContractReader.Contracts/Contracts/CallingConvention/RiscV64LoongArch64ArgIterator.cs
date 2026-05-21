// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// Shared iterator for the current cDAC RISC-V64 / LoongArch64 implementation.
/// Floating-point scalars use the FP bank, integer-like values use the GP bank,
/// and overflow spills to the stack. Large value types are passed by implicit byref.
/// </summary>
internal sealed class RiscV64LoongArch64ArgIterator : ArgIteratorBase
{
    public override int NumArgumentRegisters => 8;
    public override int NumFloatArgumentRegisters => 8;
    public override int FloatRegisterSize => 8;
    public override int EnregisteredParamTypeMaxSize => 16;
    public override int EnregisteredReturnTypeIntegerMaxSize => 16;
    public override int StackSlotSize => 8;
    public override bool IsRetBuffPassedAsFirstArg => true;

    public RiscV64LoongArch64ArgIterator(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
        : base(layout, argData, hasParamType, hasAsyncContinuation)
    {
    }

    public override bool IsArgPassedByRefBySize(int size) => size > EnregisteredParamTypeMaxSize;

    public override IEnumerable<ArgLocDesc> EnumerateArgs()
    {
        int idxGenReg = ComputeInitialNumRegistersUsed();
        int idxFPReg = 0;
        int ofsStack = 0;

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = GetElemSize(argType, argTypeInfo, _layout.PointerSize);
            bool isByRef = argType == CorElementType.Byref
                || (argType == CorElementType.ValueType && argSize > EnregisteredParamTypeMaxSize);
            int effectiveArgSize = isByRef ? _layout.PointerSize : argSize;
            int cbArg = StackElemSize(effectiveArgSize, argType == CorElementType.ValueType, false);

            ArgLocation location;
            if ((argType == CorElementType.R4 || argType == CorElementType.R8) && idxFPReg < NumFloatArgumentRegisters && !IsVarArg)
            {
                location = new ArgLocation
                {
                    Kind = ArgLocationKind.FpRegister,
                    TransitionBlockOffset = _layout.OffsetOfFloatArgumentRegisters + (idxFPReg * FloatRegisterSize),
                    Size = FloatRegisterSize,
                    ElementType = argType,
                };
                idxFPReg++;
            }
            else
            {
                int regSlots = AlignUp(cbArg, _layout.PointerSize) / _layout.PointerSize;
                if (idxGenReg + regSlots <= NumArgumentRegisters)
                {
                    location = new ArgLocation
                    {
                        Kind = ArgLocationKind.GpRegister,
                        TransitionBlockOffset = _layout.ArgumentRegistersOffset + (idxGenReg * _layout.PointerSize),
                        Size = regSlots * _layout.PointerSize,
                        ElementType = argType,
                    };
                    idxGenReg += regSlots;
                }
                else
                {
                    location = new ArgLocation
                    {
                        Kind = ArgLocationKind.Stack,
                        TransitionBlockOffset = _layout.OffsetOfArgs + ofsStack,
                        Size = cbArg,
                        ElementType = argType,
                    };
                    ofsStack += cbArg;
                }
            }

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
