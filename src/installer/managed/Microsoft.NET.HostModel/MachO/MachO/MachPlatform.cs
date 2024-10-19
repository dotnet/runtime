// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel.MachO
{
    internal enum MachPlatform : uint
    {
        MacOS = 1,
        IOS = 2,
        TvOS = 3,
        WatchOS = 4,
        BridgeOS = 5,
        MacCatalyst = 6,
        IOSSimulator = 7,
        TvOSSimulator = 8,
        WatchOSSimulator = 9,
        DriverKit = 10,
    }
}
