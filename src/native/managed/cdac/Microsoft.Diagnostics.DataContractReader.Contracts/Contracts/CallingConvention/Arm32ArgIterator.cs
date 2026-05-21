// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// ARM32 (AAPCS) argument iterator. Integer arguments use R0-R3, hard-float
/// targets use the VFP argument register bank for floating-point values, and
/// overflow spills to the stack with the ABI's 64-bit-alignment rules.
/// </summary>
internal sealed class Arm32ArgIterator : ArgIteratorBase
{
    private readonly bool _isArmhfABI;

    public override int NumArgumentRegisters => 4;
    public override int NumFloatArgumentRegisters => 16;
    public override int FloatRegisterSize => 4;
    public override int EnregisteredParamTypeMaxSize => 0;
    public override int EnregisteredReturnTypeIntegerMaxSize => 4;
    public override int StackSlotSize => 4;
    public override bool IsRetBuffPassedAsFirstArg => true;

    public Arm32ArgIterator(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation,
        bool isArmhfABI = true)
        : base(layout, argData, hasParamType, hasAsyncContinuation)
    {
        _isArmhfABI = isArmhfABI;
    }

    public override bool IsArgPassedByRefBySize(int size) => false;

    public override IEnumerable<ArgLocDesc> EnumerateArgs()
    {
        int idxGenReg = ComputeInitialNumRegistersUsed();
        int ofsStack = 0;
        ushort wFPRegs = 0;

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = GetElemSize(argType, argTypeInfo, _layout.PointerSize);
            int cbArg = StackElemSize(argSize);

            bool isFloatingPoint = false;
            bool requiresAlign64Bit = false;
            CorElementType fpElementType = argType;

            switch (argType)
            {
                case CorElementType.I8:
                case CorElementType.U8:
                    requiresAlign64Bit = true;
                    break;

                case CorElementType.R4:
                    isFloatingPoint = true;
                    fpElementType = CorElementType.R4;
                    break;

                case CorElementType.R8:
                    isFloatingPoint = true;
                    requiresAlign64Bit = true;
                    fpElementType = CorElementType.R8;
                    break;

                case CorElementType.ValueType:
                    requiresAlign64Bit = argTypeInfo.RequiresAlign8;
                    if (argTypeInfo.IsHomogeneousAggregate)
                    {
                        isFloatingPoint = true;
                        fpElementType = argTypeInfo.HomogeneousAggregateElementSize == 4
                            ? CorElementType.R4
                            : CorElementType.R8;
                    }
                    break;
            }

            IReadOnlyList<ArgLocation> locations;
            if (isFloatingPoint && _isArmhfABI && !IsVarArg)
            {
                ushort wAllocMask = checked((ushort)((1 << (cbArg / 4)) - 1));
                ushort cSteps = (ushort)(requiresAlign64Bit ? 9 - (cbArg / 8) : 17 - (cbArg / 4));
                ushort cShift = requiresAlign64Bit ? (ushort)2 : (ushort)1;

                for (ushort i = 0; i < cSteps; i++)
                {
                    if ((wFPRegs & wAllocMask) == 0)
                    {
                        wFPRegs |= wAllocMask;
                        locations =
                        [
                            new ArgLocation
                            {
                                Kind = ArgLocationKind.FpRegister,
                                TransitionBlockOffset = _layout.OffsetOfFloatArgumentRegisters + (i * cShift * 4),
                                Size = cbArg,
                                ElementType = fpElementType,
                            }
                        ];
                        goto Yield;
                    }

                    wAllocMask <<= cShift;
                }

                wFPRegs = 0xffff;
                if (requiresAlign64Bit)
                {
                    ofsStack = AlignUp(ofsStack, _layout.PointerSize * 2);
                }

                locations =
                [
                    new ArgLocation
                    {
                        Kind = ArgLocationKind.Stack,
                        TransitionBlockOffset = _layout.OffsetOfArgs + ofsStack,
                        Size = cbArg,
                        ElementType = argType,
                    }
                ];
                ofsStack += cbArg;
            }
            else
            {
                if (idxGenReg < NumArgumentRegisters)
                {
                    if (requiresAlign64Bit)
                    {
                        idxGenReg = AlignUp(idxGenReg, 2);
                    }

                    int argOffset = _layout.ArgumentRegistersOffset + idxGenReg * _layout.PointerSize;
                    int remainingRegs = NumArgumentRegisters - idxGenReg;
                    if (cbArg <= remainingRegs * _layout.PointerSize)
                    {
                        idxGenReg += AlignUp(cbArg, _layout.PointerSize) / _layout.PointerSize;
                        locations =
                        [
                            new ArgLocation
                            {
                                Kind = ArgLocationKind.GpRegister,
                                TransitionBlockOffset = argOffset,
                                Size = cbArg,
                                ElementType = argType,
                            }
                        ];
                        goto Yield;
                    }

                    idxGenReg = NumArgumentRegisters;
                    if (ofsStack == 0 && remainingRegs > 0)
                    {
                        int regSize = remainingRegs * _layout.PointerSize;
                        int stackSize = cbArg - regSize;
                        locations =
                        [
                            new ArgLocation
                            {
                                Kind = ArgLocationKind.GpRegister,
                                TransitionBlockOffset = argOffset,
                                Size = regSize,
                                ElementType = argType,
                            },
                            new ArgLocation
                            {
                                Kind = ArgLocationKind.Stack,
                                TransitionBlockOffset = _layout.OffsetOfArgs,
                                Size = stackSize,
                                ElementType = argType,
                            }
                        ];
                        ofsStack += stackSize;
                        goto Yield;
                    }
                }

                if (requiresAlign64Bit)
                {
                    ofsStack = AlignUp(ofsStack, _layout.PointerSize * 2);
                }

                locations =
                [
                    new ArgLocation
                    {
                        Kind = ArgLocationKind.Stack,
                        TransitionBlockOffset = _layout.OffsetOfArgs + ofsStack,
                        Size = cbArg,
                        ElementType = argType,
                    }
                ];
                ofsStack += cbArg;
            }

        Yield:
            yield return new ArgLocDesc
            {
                ArgType = argType,
                ArgSize = argSize,
                ArgTypeInfo = argTypeInfo,
                IsByRef = argType == CorElementType.Byref,
                Locations = locations,
            };
        }
    }
}
