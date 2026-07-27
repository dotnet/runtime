// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InFlightTLSData))]
internal sealed partial class InflightTLSData : IData<InflightTLSData>
{
    [Field] public partial TargetPointer Next { get; }
    [Field] public partial TLSIndex TlsIndex { get; }
    [Field] public partial ObjectHandle TLSData { get; }
}
