// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.CallingConventionHelpers;

/// <summary>
/// Linux/macOS x64 (System V AMD64 ABI) argument iterator. GP args go in
/// RDI/RSI/RDX/RCX/R8/R9; FP args in XMM0-XMM7. Value-type structs &lt;= 16 bytes
/// are classified per the SystemV "eightbyte" rules and may be split across
/// the GP and SSE register banks.
/// </summary>
internal sealed class AMD64UnixArgIterator : ArgIteratorBase
{
    private readonly Target _target;

    public override int NumArgumentRegisters => 6;
    public override int NumFloatArgumentRegisters => 8;
    public override int FloatRegisterSize => 16;
    public override int EnregisteredParamTypeMaxSize => 16;
    public override int EnregisteredReturnTypeIntegerMaxSize => 16;
    public override int StackSlotSize => 8;
    public override bool IsRetBuffPassedAsFirstArg => true;

    public AMD64UnixArgIterator(
        TransitionBlockLayout layout,
        ArgIteratorData argData,
        bool hasParamType,
        bool hasAsyncContinuation)
        : base(layout, argData, hasParamType, hasAsyncContinuation)
    {
        _target = layout.Target;
    }

    public override bool IsArgPassedByRefBySize(int size) => false;

    private static bool CanClassifyStruct(ArgTypeInfo typeInfo)
        => typeInfo.RuntimeTypeHandle.IsMethodTable();

    private SystemVStructDescriptor ClassifyStruct(ArgTypeInfo typeInfo, int structSize)
        => SystemVStructClassifier.Classify(_target, typeInfo.RuntimeTypeHandle, structSize);

    protected override bool ValueTypeReturnNeedsRetBuf(ArgTypeInfo thRetType)
    {
        int size = thRetType.Size;
        if (size > EnregisteredReturnTypeIntegerMaxSize)
            return true;
        if (!CanClassifyStruct(thRetType))
            return true;
        SystemVStructDescriptor descriptor = ClassifyStruct(thRetType, size);
        return !descriptor.PassedInRegisters;
    }

    public override IEnumerable<ArgLocDesc> EnumerateArgs()
    {
        int idxGenReg = ComputeInitialNumRegistersUsed();
        int idxStack = 0;
        int idxFPReg = 0;

        for (int argNum = 0; argNum < NumFixedArgs; argNum++)
        {
            CorElementType argType = GetArgumentType(argNum, out ArgTypeInfo argTypeInfo);
            int argSize = GetElemSize(argType, argTypeInfo, _layout.PointerSize);

            IReadOnlyList<ArgLocation> locations;

            if (argType != CorElementType.ValueType && argType != CorElementType.TypedByRef)
            {
                locations = [PlaceScalar(argType, argSize, ref idxGenReg, ref idxFPReg, ref idxStack)];
            }
            else
            {
                SystemVStructDescriptor descriptor = default;
                if (argSize <= SystemVStructDescriptor.MaxStructBytesToPassInRegisters
                    && CanClassifyStruct(argTypeInfo))
                {
                    descriptor = ClassifyStruct(argTypeInfo, argSize);
                }

                if (descriptor.PassedInRegisters
                    && TryClassifySysVLocations(descriptor, idxGenReg, idxFPReg, out List<ArgLocation> sysvLocations))
                {
                    locations = sysvLocations;
                    foreach (ArgLocation l in sysvLocations)
                    {
                        if (l.Kind == ArgLocationKind.GpRegister) idxGenReg++;
                        else if (l.Kind == ArgLocationKind.FpRegister) idxFPReg++;
                    }
                }
                else
                {
                    locations = [PlaceStructOnStackLocal(argSize, argType, ref idxStack)];
                }
            }

            bool isByRef = argType == CorElementType.Byref;

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

    private ArgLocation PlaceScalar(
        CorElementType argType, int argSize,
        ref int idxGenReg, ref int idxFPReg, ref int idxStack)
    {
        int cbArg = StackElemSize(argSize);

        if (argType is CorElementType.R4 or CorElementType.R8)
        {
            if (idxFPReg < NumFloatArgumentRegisters)
            {
                var loc = new ArgLocation
                {
                    Kind = ArgLocationKind.FpRegister,
                    TransitionBlockOffset = _layout.OffsetOfFloatArgumentRegisters + idxFPReg * FloatRegisterSize,
                    Size = FloatRegisterSize,
                    ElementType = argType,
                };
                idxFPReg++;
                return loc;
            }
        }
        else
        {
            int cGenRegs = cbArg / 8;
            if (cGenRegs == 0) cGenRegs = 1;
            if (idxGenReg + cGenRegs <= NumArgumentRegisters)
            {
                var loc = new ArgLocation
                {
                    Kind = ArgLocationKind.GpRegister,
                    TransitionBlockOffset = _layout.ArgumentRegistersOffset + idxGenReg * _layout.PointerSize,
                    Size = cGenRegs * _layout.PointerSize,
                    ElementType = argType,
                };
                idxGenReg += cGenRegs;
                return loc;
            }
        }

        return PlaceOnStackLocal(cbArg, argType, ref idxStack);
    }

    private ArgLocation PlaceStructOnStackLocal(int argSize, CorElementType argType, ref int idxStack)
        => PlaceOnStackLocal(StackElemSize(argSize), argType, ref idxStack);

    private ArgLocation PlaceOnStackLocal(int cbArg, CorElementType argType, ref int idxStack)
    {
        int stackOfs = _layout.OffsetOfArgs + idxStack * _layout.PointerSize;
        int slots = cbArg / _layout.PointerSize;
        if (slots == 0) slots = 1;
        idxStack += slots;
        return new ArgLocation
        {
            Kind = ArgLocationKind.Stack,
            TransitionBlockOffset = stackOfs,
            Size = cbArg,
            ElementType = argType,
        };
    }

    private bool TryClassifySysVLocations(
        SystemVStructDescriptor descriptor,
        int startGenReg, int startFPReg,
        out List<ArgLocation> locations)
    {
        int neededGen = 0, neededFP = 0;
        for (int i = 0; i < descriptor.EightByteCount; i++)
        {
            SystemVClassification c = descriptor.Classification(i);
            if (c is SystemVClassification.Integer or SystemVClassification.IntegerReference or SystemVClassification.IntegerByRef)
                neededGen++;
            else if (c == SystemVClassification.SSE)
                neededFP++;
            else { locations = null!; return false; }
        }

        if (startGenReg + neededGen > NumArgumentRegisters || startFPReg + neededFP > NumFloatArgumentRegisters)
        { locations = null!; return false; }

        locations = new List<ArgLocation>(descriptor.EightByteCount);
        int genIdx = startGenReg, fpIdx = startFPReg;
        for (int i = 0; i < descriptor.EightByteCount; i++)
        {
            SystemVClassification c = descriptor.Classification(i);
            switch (c)
            {
                case SystemVClassification.Integer:
                    locations.Add(new ArgLocation { Kind = ArgLocationKind.GpRegister, TransitionBlockOffset = _layout.ArgumentRegistersOffset + genIdx * _layout.PointerSize, Size = _layout.PointerSize, ElementType = CorElementType.I8 });
                    genIdx++; break;
                case SystemVClassification.IntegerReference:
                    locations.Add(new ArgLocation { Kind = ArgLocationKind.GpRegister, TransitionBlockOffset = _layout.ArgumentRegistersOffset + genIdx * _layout.PointerSize, Size = _layout.PointerSize, ElementType = CorElementType.Class });
                    genIdx++; break;
                case SystemVClassification.IntegerByRef:
                    locations.Add(new ArgLocation { Kind = ArgLocationKind.GpRegister, TransitionBlockOffset = _layout.ArgumentRegistersOffset + genIdx * _layout.PointerSize, Size = _layout.PointerSize, ElementType = CorElementType.Byref });
                    genIdx++; break;
                case SystemVClassification.SSE:
                    locations.Add(new ArgLocation { Kind = ArgLocationKind.FpRegister, TransitionBlockOffset = _layout.OffsetOfFloatArgumentRegisters + fpIdx * FloatRegisterSize, Size = FloatRegisterSize, ElementType = CorElementType.R8 });
                    fpIdx++; break;
            }
        }
        return true;
    }
}
