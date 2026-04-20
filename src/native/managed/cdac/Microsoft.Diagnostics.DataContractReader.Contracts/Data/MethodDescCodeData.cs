// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDescCodeData : IData<MethodDescCodeData>
{
    static MethodDescCodeData IData<MethodDescCodeData>.Create(Target target, TargetPointer address) => new MethodDescCodeData(target, address);
    public MethodDescCodeData(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDescCodeData);

        TemporaryEntryPoint = target.ReadCodePointerField(address, type, nameof(TemporaryEntryPoint));
        VersioningState = target.ReadPointerField(address, type, nameof(VersioningState));
        OptimizationTier = target.ReadField<uint>(address, type, nameof(OptimizationTier));
    }

    public TargetCodePointer TemporaryEntryPoint { get; set; }
    public TargetPointer VersioningState { get; set; }
    public uint OptimizationTier { get; init; }
}
