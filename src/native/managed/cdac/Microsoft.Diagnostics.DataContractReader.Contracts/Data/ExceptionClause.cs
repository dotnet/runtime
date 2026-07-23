// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal interface IExceptionClauseData
{
    uint Flags { get; }
    uint TryStartPC { get; }
    uint TryEndPC { get; }
    uint HandlerStartPC { get; }
    uint HandlerEndPC { get; }
    uint ClassToken { get; }
    uint FilterOffset { get; }
}

[CdacType(nameof(DataType.EEExceptionClause))]
internal sealed partial class EEExceptionClause : IData<EEExceptionClause>, IExceptionClauseData
{
    [Field] public partial uint Flags { get; }
    [Field] public partial uint TryStartPC { get; }
    [Field] public partial uint TryEndPC { get; }
    [Field] public partial uint HandlerStartPC { get; }
    [Field] public partial uint HandlerEndPC { get; }
    [Field] public partial TargetNUInt TypeHandle { get; }

    public uint ClassToken => (uint)TypeHandle.Value;

    public uint FilterOffset => ClassToken;
}

[CdacType(nameof(DataType.R2RExceptionClause))]
internal sealed partial class R2RExceptionClause : IData<R2RExceptionClause>, IExceptionClauseData
{
    [Field] public partial uint Flags { get; }
    [Field] public partial uint TryStartPC { get; }
    [Field] public partial uint TryEndPC { get; }
    [Field] public partial uint HandlerStartPC { get; }
    [Field] public partial uint HandlerEndPC { get; }
    [Field] public partial uint ClassToken { get; }

    public uint FilterOffset => ClassToken;
}
