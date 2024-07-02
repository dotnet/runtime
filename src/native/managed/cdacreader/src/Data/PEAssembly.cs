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

        PEImage = target.ReadPointer(address + (ulong)type.Fields[nameof(PEImage)].Offset);
    }

    public TargetPointer PEImage { get; init; }
}
