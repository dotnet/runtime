// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.PrecodeMachineDescriptor))]
internal sealed partial class PrecodeMachineDescriptor : IData<PrecodeMachineDescriptor>
{
    [Field] public partial byte InvalidPrecodeType { get; }
    [Field] public partial byte StubPrecodeType { get; }
    [Field] public partial uint StubCodePageSize { get; }
    [CustomInit(nameof(InitPInvokeImportPrecodeType))] public partial byte? PInvokeImportPrecodeType { get; }
    [CustomInit(nameof(InitThisPointerRetBufPrecodeType))] public partial byte? ThisPointerRetBufPrecodeType { get; }
    [CustomInit(nameof(InitInterpreterPrecodeType))] public partial byte? InterpreterPrecodeType { get; }
    [CustomInit(nameof(InitUMEntryPrecodeType))] public partial byte? UMEntryPrecodeType { get; }
    [CustomInit(nameof(InitDynamicHelperPrecodeType))] public partial byte? DynamicHelperPrecodeType { get; }
    [CustomInit(nameof(InitFixupStubPrecodeSize))] public partial byte? FixupStubPrecodeSize { get; }
    [CustomInit(nameof(InitFixupBytes))] public partial byte[]? FixupBytes { get; }
    [CustomInit(nameof(InitFixupIgnoredBytes))] public partial byte[]? FixupIgnoredBytes { get; }
    [CustomInit(nameof(InitStubPrecodeSize))] public partial byte? StubPrecodeSize { get; }
    [CustomInit(nameof(InitStubBytes))] public partial byte[]? StubBytes { get; }
    [CustomInit(nameof(InitStubIgnoredBytes))] public partial byte[]? StubIgnoredBytes { get; }

    [DataDescriptorDependency(nameof(PInvokeImportPrecodeType), "uint8")]
    private partial byte? InitPInvokeImportPrecodeType(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(PInvokeImportPrecodeType));

    [DataDescriptorDependency(nameof(ThisPointerRetBufPrecodeType), "uint8")]
    private partial byte? InitThisPointerRetBufPrecodeType(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(ThisPointerRetBufPrecodeType));

    [DataDescriptorDependency(nameof(InterpreterPrecodeType), "uint8")]
    private partial byte? InitInterpreterPrecodeType(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(InterpreterPrecodeType));

    [DataDescriptorDependency(nameof(UMEntryPrecodeType), "uint8")]
    private partial byte? InitUMEntryPrecodeType(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(UMEntryPrecodeType));

    [DataDescriptorDependency(nameof(DynamicHelperPrecodeType), "uint8")]
    private partial byte? InitDynamicHelperPrecodeType(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(DynamicHelperPrecodeType));

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    private partial byte? InitFixupStubPrecodeSize(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(FixupStubPrecodeSize));

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(FixupBytes), "uint8[]")]
    private partial byte[]? InitFixupBytes(Target target, TargetPointer address)
        => MaybeGetBytes(target, address, FixupStubPrecodeSize, nameof(FixupBytes));

    [DataDescriptorDependency(nameof(FixupStubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(FixupIgnoredBytes), "uint8[]")]
    private partial byte[]? InitFixupIgnoredBytes(Target target, TargetPointer address)
        => MaybeGetBytes(target, address, FixupStubPrecodeSize, nameof(FixupIgnoredBytes));

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    private partial byte? InitStubPrecodeSize(Target target, TargetPointer address)
        => MaybeGetByte(target, address, nameof(StubPrecodeSize));

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(StubBytes), "uint8[]")]
    private partial byte[]? InitStubBytes(Target target, TargetPointer address)
        => MaybeGetBytes(target, address, StubPrecodeSize, nameof(StubBytes));

    [DataDescriptorDependency(nameof(StubPrecodeSize), "uint8")]
    [DataDescriptorDependency(nameof(StubIgnoredBytes), "uint8[]")]
    private partial byte[]? InitStubIgnoredBytes(Target target, TargetPointer address)
        => MaybeGetBytes(target, address, StubPrecodeSize, nameof(StubIgnoredBytes));

    private static byte? MaybeGetByte(Target target, TargetPointer address, string fieldName)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PrecodeMachineDescriptor);
        return type.Fields.ContainsKey(fieldName)
            ? target.Read<byte>(address + (ulong)type.Fields[fieldName].Offset)
            : null;
    }

    private static byte[]? MaybeGetBytes(Target target, TargetPointer address, byte? size, string fieldName)
    {
        if (size is not byte length)
            return null;

        Target.TypeInfo type = target.GetTypeInfo(DataType.PrecodeMachineDescriptor);
        byte[] bytes = new byte[length];
        target.ReadBuffer(address + (ulong)type.Fields[fieldName].Offset, bytes);
        return bytes;
    }
}
