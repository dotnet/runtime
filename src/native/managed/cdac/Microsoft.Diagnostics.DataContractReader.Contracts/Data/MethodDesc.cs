// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDesc))]
internal sealed partial class MethodDesc : IData<MethodDesc>
{
    [Field] public byte ChunkIndex { get; }
    [Field] public ushort Slot { get; }
    [Field] public ushort Flags { get; }
    [Field] public ushort Flags3AndTokenRemainder { get; }
    [Field] public byte EntryPointFlags { get; }
    [Field] public TargetPointer CodeData { get; }
    [Field] public TargetPointer? GCCoverageInfo { get; }
}

[CdacType(nameof(DataType.InstantiatedMethodDesc))]
internal sealed partial class InstantiatedMethodDesc : IData<InstantiatedMethodDesc>
{
    [Field] public TargetPointer PerInstInfo { get; }
    [Field] public ushort NumGenericArgs { get; }
    [Field] public ushort Flags2 { get; }
}

[CdacType(nameof(DataType.DynamicMethodDesc))]
internal sealed partial class DynamicMethodDesc : IData<DynamicMethodDesc>
{
    [Field] public TargetPointer MethodName { get; }
}

[CdacType(nameof(DataType.StoredSigMethodDesc))]
internal sealed partial class StoredSigMethodDesc : IData<StoredSigMethodDesc>
{
    [Field] public TargetPointer Sig { get; }
    [Field] public uint cSig { get; }
    [Field] public uint ExtendedFlags { get; }
}
