// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.LayoutEEClass))]
internal sealed partial class LayoutEEClass : IData<LayoutEEClass>
{
    [Field] public partial EEClassLayoutInfo LayoutInfo { get; }
}
