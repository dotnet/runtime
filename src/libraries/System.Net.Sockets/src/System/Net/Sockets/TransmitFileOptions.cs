// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Sockets
{
    [Flags]
    public enum TransmitFileOptions
    {
        UseDefaultWorkerThread = 0x00,
        Disconnect = 0x01,
        ReuseSocket = 0x02,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        WriteBehind = 0x04,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        UseSystemThread = 0x10,
        [System.Runtime.Versioning.MinimumOSPlatformAttribute("windows7.0")]
        UseKernelApc = 0x20,
    };
}
