// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class WebcilHeader : IData<WebcilHeader>
{
    static WebcilHeader IData<WebcilHeader>.Create(Target target, TargetPointer address) => new WebcilHeader(target, address);
    public WebcilHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.WebcilHeader);

        CoffSections = target.Read<ushort>(address + (ulong)type.Fields[nameof(CoffSections)].Offset);
    }

    public ushort CoffSections { get; init; }
}
