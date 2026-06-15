// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GenericsDictInfo))]
internal sealed partial class GenericsDictInfo : IData<GenericsDictInfo>
{
    [Field] public ushort NumDicts { get; }
    [Field] public ushort NumTypeArgs { get; }
}
