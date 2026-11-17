// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.RuntimeFunction))]
internal sealed partial class RuntimeFunction : IData<RuntimeFunction>
{
    [Field] public partial uint BeginAddress { get; }

    // Not all platforms define EndAddress
    [Field] public partial uint? EndAddress { get; }
    [Field] public partial uint UnwindData { get; }
}

[CdacType(nameof(DataType.WasmRuntimeFunctionGroup))]
internal sealed partial class WasmRuntimeFunctionGroup : IData<WasmRuntimeFunctionGroup>
{
    [Field] public partial uint VirtualIPBase { get; }
    [Field] public partial uint IsFuncletBits { get; }
    [CustomInit(nameof(InitHighBitsOfUnwindOffset))] public partial TargetPointer HighBitsOfUnwindOffset { get; }
    [CustomInit(nameof(InitVirtualIPOffsets))] public partial TargetPointer VirtualIPOffsets { get; }
    [CustomInit(nameof(InitMidBitsOfUnwindOffset))] public partial TargetPointer MidBitsOfUnwindOffset { get; }
    [CustomInit(nameof(InitLowBitsOfUnwindOffset))] public partial TargetPointer LowBitsOfUnwindOffset { get; }

    [DataDescriptorDependency(nameof(HighBitsOfUnwindOffset), "uint8[]")]
    private partial TargetPointer InitHighBitsOfUnwindOffset(Target target, TargetPointer address)
        => GetFieldAddress(target, address, nameof(HighBitsOfUnwindOffset));

    [DataDescriptorDependency(nameof(VirtualIPOffsets), "uint8[]")]
    private partial TargetPointer InitVirtualIPOffsets(Target target, TargetPointer address)
        => GetFieldAddress(target, address, nameof(VirtualIPOffsets));

    [DataDescriptorDependency(nameof(MidBitsOfUnwindOffset), "uint8[]")]
    private partial TargetPointer InitMidBitsOfUnwindOffset(Target target, TargetPointer address)
        => GetFieldAddress(target, address, nameof(MidBitsOfUnwindOffset));

    [DataDescriptorDependency(nameof(LowBitsOfUnwindOffset), "uint8[]")]
    private partial TargetPointer InitLowBitsOfUnwindOffset(Target target, TargetPointer address)
        => GetFieldAddress(target, address, nameof(LowBitsOfUnwindOffset));

    private static TargetPointer GetFieldAddress(Target target, TargetPointer address, string fieldName)
        => address + (ulong)target.GetTypeInfo(DataType.WasmRuntimeFunctionGroup).Fields[fieldName].Offset;
}

[CdacType(nameof(DataType.RuntimeFunction))]
internal sealed partial class WasmRuntimeFunction : IData<WasmRuntimeFunction>
{
    private const uint GroupSize = 128;
    private const uint EntrySize = 4;
    private const uint EntriesPerGroup = GroupSize / EntrySize;

    [CustomInit(nameof(InitGroup))] public partial WasmRuntimeFunctionGroup Group { get; }
    [CustomInit(nameof(InitIsSkip))] public partial bool IsSkip { get; }
    [CustomInit(nameof(InitIsFunclet))] public partial bool IsFunclet { get; }
    [CustomInit(nameof(InitBeginAddress))] public partial uint BeginAddress { get; }
    [CustomInit(nameof(InitUnwindData))] public partial uint UnwindData { get; }

    [UsesDataDescriptorTypeSize(nameof(DataType.RuntimeFunction))]
    [UsesDataDescriptorTypeSize(nameof(DataType.WasmRuntimeFunctionGroup))]
    private partial WasmRuntimeFunctionGroup InitGroup(Target target, TargetPointer address)
    {
        if (GetSize(target) != EntrySize || WasmRuntimeFunctionGroup.GetSize(target) != GroupSize ||
            (address.Value & (EntrySize - 1)) != 0)
        {
            throw new InvalidOperationException("Invalid Wasm runtime-function group layout or entry address.");
        }

        return target.ProcessedData.GetOrAdd<WasmRuntimeFunctionGroup>(
            new TargetPointer(address.Value & ~(ulong)(GroupSize - 1)));
    }

    private partial bool InitIsSkip(Target target, TargetPointer address)
        => target.Read<byte>(Group.VirtualIPOffsets + GetIndex(address)) == byte.MaxValue;

    private partial bool InitIsFunclet(Target target, TargetPointer address)
        => (Group.IsFuncletBits & (1u << (int)GetIndex(address))) != 0;

    private partial uint InitBeginAddress(Target target, TargetPointer address)
    {
        if (IsSkip)
            throw new InvalidOperationException("A Wasm runtime-function skip has no begin address.");

        uint index = GetIndex(address);
        return checked(Group.VirtualIPBase + target.Read<byte>(Group.VirtualIPOffsets + index) + index * EntrySize);
    }

    private partial uint InitUnwindData(Target target, TargetPointer address)
    {
        if (IsSkip)
            throw new InvalidOperationException("A Wasm runtime-function skip has no unwind data.");

        uint index = GetIndex(address);
        uint high = (uint)(target.Read<byte>(Group.HighBitsOfUnwindOffset + index / 2) >> (int)((index & 1) * 4)) & 0xF;
        uint offset = ((uint)target.Read<byte>(Group.MidBitsOfUnwindOffset + index) << 8) |
            target.Read<byte>(Group.LowBitsOfUnwindOffset + index);
        if (high == 0xF)
        {
            if (index == EntriesPerGroup - 1 ||
                target.Read<byte>(Group.VirtualIPOffsets + EntriesPerGroup - 1) != byte.MaxValue)
            {
                throw new BadImageFormatException("Invalid Wasm runtime-function unwind escape.");
            }

            return ((uint)target.Read<byte>(Group.MidBitsOfUnwindOffset + EntriesPerGroup - 1) << 24) |
                ((uint)target.Read<byte>(Group.LowBitsOfUnwindOffset + EntriesPerGroup - 1) << 16) | offset;
        }

        return (high << 16) | offset;
    }

    private static uint GetIndex(TargetPointer address)
        => (uint)(address.Value & (GroupSize - 1)) / EntrySize;
}
