// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// ARM64 (AAPCS64 / Apple ARM64) argument iterator. Integer arguments use X0-X7,
/// floating-point arguments use V0-V7, Apple stack slots are tightly packed for
/// primitives, and HFAs are reported as one slot per FP register.
/// </summary>
internal sealed class Arm64ArgIterator : ArgIteratorBase
{
    private readonly bool _isAppleArm64ABI;

    public override int NumArgumentRegisters => 8;
    public override int NumFloatArgumentRegisters => 8;
    public override int FloatRegisterSize => 16;
    public override int EnregisteredParamTypeMaxSize => 16;
    public override int EnregisteredReturnTypeIntegerMaxSize => 16;
    public override int StackSlotSize => 8;
    public override bool IsRetBuffPassedAsFirstArg => false;

    public Arm64ArgIterator(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
        : base(layout, argData, hasParamType, hasAsyncContinuation)
    {
        _isAppleArm64ABI = layout.OperatingSystem == RuntimeInfoOperatingSystem.Apple;
    }

    public override int StackElemSize(int parmSize, bool isValueType = false, bool isFloatHfa = false)
    {
        if (_isAppleArm64ABI)
        {
            if (!isValueType)
            {
                Debug.Assert((parmSize & (parmSize - 1)) == 0);
                return parmSize;
            }

            if (isFloatHfa)
            {
                Debug.Assert((parmSize % 4) == 0);
                return parmSize;
            }
        }

        return base.StackElemSize(parmSize, isValueType, isFloatHfa);
    }

    public override bool IsArgPassedByRefBySize(int size) => size > EnregisteredParamTypeMaxSize;

    public override int GetRetBuffArgOffset(bool hasThis)
        => _layout.FirstGCRefMapSlot;

    public override IEnumerable<ArgLocDesc> EnumerateArgs()
    {
        int idxGenReg = ComputeInitialNumRegistersUsed();
        int idxFPReg = 0;
        int ofsStack = 0;

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = GetElemSize(argType, argTypeInfo, _layout.PointerSize);
            bool isHomogeneousAggregate = argType == CorElementType.ValueType && argTypeInfo.IsHomogeneousAggregate;
            bool isByRef = argType == CorElementType.Byref
                || (argType == CorElementType.ValueType
                    && argSize > EnregisteredParamTypeMaxSize
                    && (!isHomogeneousAggregate || IsVarArg));

            int effectiveArgSize = isByRef ? _layout.PointerSize : argSize;
            int cFPRegs = 0;
            bool isFloatHfa = false;
            CorElementType fpElementType = argType;

            switch (argType)
            {
                case CorElementType.R4:
                    cFPRegs = 1;
                    fpElementType = CorElementType.R4;
                    break;

                case CorElementType.R8:
                    cFPRegs = 1;
                    fpElementType = CorElementType.R8;
                    break;

                case CorElementType.ValueType:
                    if (isHomogeneousAggregate)
                    {
                        int haElementSize = argTypeInfo.HomogeneousAggregateElementSize;
                        isFloatHfa = haElementSize == 4;
                        fpElementType = haElementSize == 4 ? CorElementType.R4 : CorElementType.R8;
                        cFPRegs = argSize / haElementSize;
                    }
                    break;
            }

            int cbArg = StackElemSize(effectiveArgSize, argType == CorElementType.ValueType, isFloatHfa);
            IReadOnlyList<ArgLocation> locations;

            if (cFPRegs > 0 && !IsVarArg)
            {
                if (idxFPReg + cFPRegs <= NumFloatArgumentRegisters)
                {
                    List<ArgLocation> hfaLocations = new(cFPRegs);
                    for (int i = 0; i < cFPRegs; i++)
                    {
                        hfaLocations.Add(new ArgLocation
                        {
                            Kind = ArgLocationKind.FpRegister,
                            TransitionBlockOffset = _layout.OffsetOfFloatArgumentRegisters + ((idxFPReg + i) * FloatRegisterSize),
                            Size = FloatRegisterSize,
                            ElementType = fpElementType,
                        });
                    }

                    idxFPReg += cFPRegs;
                    locations = hfaLocations;
                    goto Yield;
                }

                idxFPReg = NumFloatArgumentRegisters;
            }
            else
            {
                int regSlots = AlignUp(cbArg, _layout.PointerSize) / _layout.PointerSize;
                if (idxGenReg + regSlots <= NumArgumentRegisters)
                {
                    locations =
                    [
                        new ArgLocation
                        {
                            Kind = ArgLocationKind.GpRegister,
                            TransitionBlockOffset = _layout.ArgumentRegistersOffset + (idxGenReg * _layout.PointerSize),
                            Size = regSlots * _layout.PointerSize,
                            ElementType = argType,
                        }
                    ];
                    idxGenReg += regSlots;
                    goto Yield;
                }

                bool allowVarArgSplit = _layout.OperatingSystem == RuntimeInfoOperatingSystem.Windows
                    && IsVarArg
                    && idxGenReg < NumArgumentRegisters
                    && !isHomogeneousAggregate;
                if (allowVarArgSplit)
                {
                    int headSize = (NumArgumentRegisters - idxGenReg) * _layout.PointerSize;
                    locations =
                    [
                        new ArgLocation
                        {
                            Kind = ArgLocationKind.GpRegister,
                            TransitionBlockOffset = _layout.ArgumentRegistersOffset + (idxGenReg * _layout.PointerSize),
                            Size = headSize,
                            ElementType = argType,
                        },
                        new ArgLocation
                        {
                            Kind = ArgLocationKind.Stack,
                            TransitionBlockOffset = _layout.OffsetOfArgs + ofsStack,
                            Size = cbArg - headSize,
                            ElementType = argType,
                        }
                    ];
                    ofsStack += cbArg - headSize;
                    idxGenReg = NumArgumentRegisters;
                    goto Yield;
                }

                idxGenReg = NumArgumentRegisters;
            }

            if (_isAppleArm64ABI)
            {
                int alignment = !argTypeInfo.IsValueType
                    ? cbArg
                    : isFloatHfa ? 4 : 8;
                ofsStack = AlignUp(ofsStack, alignment);
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

        Yield:
            yield return new ArgLocDesc
            {
                ArgType = argType,
                ArgSize = argSize,
                ArgTypeInfo = argTypeInfo,
                IsByRef = isByRef,
                Locations = locations,
            };
        }
    }
}
