// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType("System.Runtime.InteropServices.ObjectiveC.ObjectiveCMarshal")]
internal sealed partial class ObjectiveCMarshal : IData<ObjectiveCMarshal>
{
    [StaticReference("s_objects")]
    public static partial TargetPointer? ObjectTrackingInfoTable(Target target);
}
