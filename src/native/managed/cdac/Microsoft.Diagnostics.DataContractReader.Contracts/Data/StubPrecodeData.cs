// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.StubPrecodeData))]
internal sealed partial class StubPrecodeData_2 : IData<StubPrecodeData_2>
{
    [Field] public partial TargetPointer SecretParam { get; }
    [Field] public partial byte Type { get; }
}
