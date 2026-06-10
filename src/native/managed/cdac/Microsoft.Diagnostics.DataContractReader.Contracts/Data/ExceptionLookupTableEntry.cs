// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ExceptionLookupTableEntry))]
internal sealed partial class ExceptionLookupTableEntry : IData<ExceptionLookupTableEntry>
{
    [Field] public uint MethodStartRVA { get; }
    [Field] public uint ExceptionInfoRVA { get; }
}
