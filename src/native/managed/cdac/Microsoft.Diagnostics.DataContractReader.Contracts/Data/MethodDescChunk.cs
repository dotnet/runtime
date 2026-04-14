// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class MethodDescChunk : IData<MethodDescChunk>
{
    static MethodDescChunk IData<MethodDescChunk>.Create(Target target, TargetPointer address) => new MethodDescChunk(target, address);
    public MethodDescChunk(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.MethodDescChunk);

        MethodTable = target.ReadPointerField(address, type, nameof(MethodTable));
        Next = target.ReadPointerField(address, type, nameof(Next));
        Size = target.ReadField<byte>(address, type, nameof(Size));
        Count = target.ReadField<byte>(address, type, nameof(Count));
        FlagsAndTokenRange = target.ReadField<ushort>(address, type, nameof(FlagsAndTokenRange));

        // The first MethodDesc is at the end of the MethodDescChunk
        FirstMethodDesc = address + type.Size!.Value;
    }

    public TargetPointer MethodTable { get; init; }
    public TargetPointer Next { get; init; }
    public byte Size { get; init; }
    public byte Count { get; init; }
    public ushort FlagsAndTokenRange { get; init; }

    public TargetPointer FirstMethodDesc { get; init; }
}
