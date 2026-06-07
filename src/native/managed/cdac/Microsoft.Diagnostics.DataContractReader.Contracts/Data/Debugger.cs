// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.Debugger))]
internal sealed partial class Debugger : IData<Debugger>
{
    [Field] public int LeftSideInitialized { get; }
    [Field] public uint Defines { get; }
    [Field] public uint MDStructuresVersion { get; }
    [Field] public TargetPointer RCThread { get; }

    [Field(Writable = true)] public int RSRequestedSync { get; private set; }
    [Field(Writable = true)] public int SendExceptionsOutsideOfJMC { get; private set; }
    [Field(Writable = true)] public int GCNotificationEventsEnabled { get; private set; }
    [Field] public TargetPointer RgHijackFunction { get; }
}
