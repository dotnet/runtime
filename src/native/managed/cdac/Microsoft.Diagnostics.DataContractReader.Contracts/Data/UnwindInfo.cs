// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.UnwindInfo))]
internal sealed partial class UnwindInfo : IData<UnwindInfo>
{
    [CustomInit(nameof(InitFunctionLength))] public partial uint? FunctionLength { get; }
    [CustomInit(nameof(InitHeader))] public partial uint? Header { get; }

    [DataDescriptorDependency(nameof(FunctionLength), "uint32")]
    private partial uint? InitFunctionLength(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.UnwindInfo);
        // The unwind info contains the function length on some platforms (x86)
        return type.Fields.ContainsKey(nameof(FunctionLength))
            ? target.ReadField<uint>(address, type, nameof(FunctionLength))
            : null;
    }

    private partial uint? InitHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.UnwindInfo);
        // When the function length is absent, the unwind info starts with a bitfield header
        return type.Fields.ContainsKey(nameof(FunctionLength))
            ? null
            : target.Read<uint>(address);
    }
}
