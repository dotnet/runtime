// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Assembly))]
internal sealed partial class Assembly : IData<Assembly>
{
    [Field] public partial TargetPointer Module { get; }
    [Field] public partial byte IsCollectible { get; }
    [Field] public partial bool IsDynamic { get; }
    [Field] public partial TargetPointer Error { get; }
    [Field] public partial uint NotifyFlags { get; }
    [Field] public partial bool IsLoaded { get; }

    public bool IsError => Error != TargetPointer.Null;
}
