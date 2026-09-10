// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.MethodDesc))]
internal sealed partial class MethodDesc : IData<MethodDesc>
{
    [Field] public partial byte ChunkIndex { get; }
    [Field] public partial ushort Slot { get; }
    [Field] public partial ushort Flags { get; }
    [Field] public partial ushort Flags3AndTokenRemainder { get; }
    [Field] public partial byte EntryPointFlags { get; }
    [Field] public partial TargetPointer CodeData { get; }
    [Field] public partial TargetPointer? GCCoverageInfo { get; }
}

[CdacType(nameof(DataType.InstantiatedMethodDesc))]
internal sealed partial class InstantiatedMethodDesc : IData<InstantiatedMethodDesc>
{
    [Field] public partial TargetPointer PerInstInfo { get; }
    [Field] public partial ushort NumGenericArgs { get; }
    [Field] public partial ushort Flags2 { get; }
}

[CdacType(nameof(DataType.DynamicMethodDesc))]
internal sealed partial class DynamicMethodDesc : IData<DynamicMethodDesc>
{
    [Field] public partial TargetPointer MethodName { get; }
}

[CdacType(nameof(DataType.StoredSigMethodDesc))]
internal sealed partial class StoredSigMethodDesc : IData<StoredSigMethodDesc>
{
    [Field] public partial TargetPointer Sig { get; }
    [Field] public partial uint cSig { get; }
    [Field] public partial uint ExtendedFlags { get; }
}

[CdacType(nameof(DataType.ArrayMethodDesc))]
internal sealed partial class ArrayMethodDesc : IData<ArrayMethodDesc>
{
}

[CdacType(nameof(DataType.FCallMethodDesc))]
internal sealed partial class FCallMethodDesc : IData<FCallMethodDesc>
{
}

[CdacType(nameof(DataType.PInvokeMethodDesc))]
internal sealed partial class PInvokeMethodDesc : IData<PInvokeMethodDesc>
{
}

[CdacType(nameof(DataType.EEImplMethodDesc))]
internal sealed partial class EEImplMethodDesc : IData<EEImplMethodDesc>
{
}

[CdacType(nameof(DataType.CLRToCOMCallMethodDesc))]
internal sealed partial class CLRToCOMCallMethodDesc : IData<CLRToCOMCallMethodDesc>
{
}

[CdacType(nameof(DataType.NonVtableSlot))]
internal sealed partial class NonVtableSlot : IData<NonVtableSlot>
{
}

[CdacType(nameof(DataType.MethodImpl))]
internal sealed partial class MethodImpl : IData<MethodImpl>
{
}

[CdacType(nameof(DataType.NativeCodeSlot))]
internal sealed partial class NativeCodeSlot : IData<NativeCodeSlot>
{
}
