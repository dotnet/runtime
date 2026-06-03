// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal+ObjcTrackingInformation")]
internal sealed partial class ObjcTrackingInformation : IData<ObjcTrackingInformation>
{
    [Field("_memory")]
    public TargetPointer Memory { get; }
}
