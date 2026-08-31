// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Debugger))]
internal sealed partial class Debugger : IData<Debugger>
{
    [Field] public partial int LeftSideInitialized { get; }
    [Field] public partial TargetPointer RCThread { get; }

    [Field(Writable = true)] public partial int RSRequestedSync { get; private set; }
    [Field(Writable = true)] public partial int SendExceptionsOutsideOfJMC { get; private set; }
    [Field(Writable = true)] public partial int GCNotificationEventsEnabled { get; private set; }
    [Field] public partial TargetPointer RgHijackFunction { get; }
}
