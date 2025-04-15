// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class CGrowableSymbolStream : IData<CGrowableSymbolStream>
{
    static CGrowableSymbolStream IData<CGrowableSymbolStream>.Create(Target target, TargetPointer address) => new CGrowableSymbolStream(target, address);
    public CGrowableSymbolStream(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.CGrowableSymbolStream);

        Buffer = target.ReadPointer(address + (ulong)type.Fields[nameof(Buffer)].Offset);
        Size = target.Read<uint>(address + (ulong)type.Fields[nameof(Size)].Offset);
    }

    public TargetPointer Buffer { get; init; }
    public uint Size { get; init; }
}
