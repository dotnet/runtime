// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Assembly))]
internal sealed partial class Assembly : IData<Assembly>
{
    [Field] public TargetPointer Module { get; }
    [Field] public byte IsCollectible { get; }
    [Field] public bool IsDynamic { get; }
    [Field] public TargetPointer Error { get; }
    [Field] public uint NotifyFlags { get; }
    [Field] public bool IsLoaded { get; }

    public bool IsError => Error != TargetPointer.Null;
}
