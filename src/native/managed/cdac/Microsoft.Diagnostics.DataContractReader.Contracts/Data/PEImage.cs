// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PEImage : IData<PEImage>
{
    static PEImage IData<PEImage>.Create(Target target, TargetPointer address) => new PEImage(target, address);
    public PEImage(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PEImage);

        LoadedImageLayout = target.ReadPointer(address + (ulong)type.Fields[nameof(LoadedImageLayout)].Offset);
        ProbeExtensionResult = target.ProcessedData.GetOrAdd<ProbeExtensionResult>(address + (ulong)type.Fields[nameof(ProbeExtensionResult)].Offset);
    }

    public TargetPointer LoadedImageLayout { get; init; }
    public ProbeExtensionResult ProbeExtensionResult { get; init; }
}
