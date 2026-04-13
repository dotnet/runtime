// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class ExternalMethodFrame : IData<ExternalMethodFrame>
{
    static ExternalMethodFrame IData<ExternalMethodFrame>.Create(Target target, TargetPointer address)
        => new ExternalMethodFrame(target, address);

    public ExternalMethodFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ExternalMethodFrame);
        GCRefMap = target.ReadPointerField(address, type, nameof(GCRefMap));
        Indirection = target.ReadPointerField(address, type, nameof(Indirection));
        ZapModule = target.ReadPointerField(address, type, nameof(ZapModule));
    }

    public TargetPointer GCRefMap { get; }
    public TargetPointer Indirection { get; }
    public TargetPointer ZapModule { get; }
}
