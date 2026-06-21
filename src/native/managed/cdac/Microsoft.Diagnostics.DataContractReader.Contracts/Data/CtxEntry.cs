// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.CtxEntry))]
internal sealed partial class CtxEntry : IData<CtxEntry>
{
    [Field] public TargetPointer STAThread { get; }
    [Field] public TargetPointer CtxCookie { get; }
}
