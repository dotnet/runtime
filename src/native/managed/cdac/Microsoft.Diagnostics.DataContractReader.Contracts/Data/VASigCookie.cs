// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class VASigCookie : IData<VASigCookie>
{
    static VASigCookie IData<VASigCookie>.Create(Target target, TargetPointer address)
        => new VASigCookie(target, address);

    public VASigCookie(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.VASigCookie);

        SizeOfArgs = target.ReadField<uint>(address, type, nameof(SizeOfArgs));
        SignaturePointer = target.ReadPointerField(address, type, nameof(SignaturePointer));
        SignatureLength = target.ReadField<uint>(address, type, nameof(SignatureLength));
    }

    public uint SizeOfArgs { get; init; }
    public TargetPointer SignaturePointer { get; init; }
    public uint SignatureLength { get; init; }
}
