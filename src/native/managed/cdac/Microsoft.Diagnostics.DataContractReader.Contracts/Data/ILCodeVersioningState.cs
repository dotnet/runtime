// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class ILCodeVersioningState : IData<ILCodeVersioningState>
{
    static ILCodeVersioningState IData<ILCodeVersioningState>.Create(Target target, TargetPointer address)
        => new ILCodeVersioningState(target, address);

    public ILCodeVersioningState(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ILCodeVersioningState);

        FirstVersionNode = target.ReadPointerField(address, type, nameof(FirstVersionNode));
        ActiveVersionKind = target.ReadField<uint>(address, type, nameof(ActiveVersionKind));
        ActiveVersionNode = target.ReadPointerField(address, type, nameof(ActiveVersionNode));
        ActiveVersionModule = target.ReadPointerField(address, type, nameof(ActiveVersionModule));
        ActiveVersionMethodDef = target.ReadField<uint>(address, type, nameof(ActiveVersionMethodDef));
    }

    public TargetPointer FirstVersionNode { get; set; }
    public uint ActiveVersionKind { get; set; }
    public TargetPointer ActiveVersionNode { get; set; }
    public TargetPointer ActiveVersionModule { get; set; }
    public uint ActiveVersionMethodDef { get; set; }
}
