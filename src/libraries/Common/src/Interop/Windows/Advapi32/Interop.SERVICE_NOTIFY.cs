// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Advapi32
    {
        internal const uint SERVICE_NOTIFY_STATUS_CHANGE = 2;

        [StructLayout(LayoutKind.Sequential)]
        internal struct SERVICE_NOTIFY
        {
            public uint version;
            public unsafe delegate* unmanaged<IntPtr, void> notifyCallback;
            public IntPtr context;
            public uint notificationStatus;
            public SERVICE_STATUS_PROCESS serviceStatus;
            public uint notificationTriggered;
            public IntPtr serviceNames;
        }
    }
}
