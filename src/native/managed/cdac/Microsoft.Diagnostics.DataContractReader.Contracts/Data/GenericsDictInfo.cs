// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class GenericsDictInfo : IData<GenericsDictInfo>
{
    static GenericsDictInfo IData<GenericsDictInfo>.Create(Target target, TargetPointer address) => new GenericsDictInfo(target, address);
    public GenericsDictInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.GenericsDictInfo);

        NumDicts = target.ReadField<ushort>(address, type, nameof(NumDicts));
        NumTypeArgs = target.ReadField<ushort>(address, type, nameof(NumTypeArgs));
    }

    public ushort NumDicts { get; init; }
    public ushort NumTypeArgs { get; init; }
}
