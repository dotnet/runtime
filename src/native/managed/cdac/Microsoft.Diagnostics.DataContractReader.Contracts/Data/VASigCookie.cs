// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.VASigCookie))]
internal sealed partial class VASigCookie : IData<VASigCookie>
{
    [Field] public uint SizeOfArgs { get; }
    [Field] public Signature Signature { get; }
}
