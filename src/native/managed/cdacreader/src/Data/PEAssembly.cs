// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class PEAssembly : IData<PEAssembly>
{
    static PEAssembly IData<PEAssembly>.Create(Target target, TargetPointer address)
        => new PEAssembly(target, address);

    public PEAssembly(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.PEAssembly);

        TargetPointer peImagePointer = target.ReadPointer(address + (ulong)type.Fields[nameof(PEImage)].Offset);
        if (peImagePointer != TargetPointer.Null)
            PEImage = target.ProcessedData.GetOrAdd<PEImage>(peImagePointer);
    }

    public PEImage? PEImage { get; init; }
}
