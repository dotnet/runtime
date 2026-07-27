// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.GenericsDictInfo))]
internal sealed partial class GenericsDictInfo : IData<GenericsDictInfo>
{
    [Field] public partial ushort NumDicts { get; }
    [Field] public partial ushort NumTypeArgs { get; }
}
