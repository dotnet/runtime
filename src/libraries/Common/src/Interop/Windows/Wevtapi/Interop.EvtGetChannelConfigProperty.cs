// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Eventing.Reader;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Wevtapi
    {
        [LibraryImport(Libraries.Wevtapi, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EvtGetChannelConfigProperty(
                            EventLogHandle channelConfig,
                            EVT_CHANNEL_CONFIG_PROPERTY_ID propertyId,
                            int flags,
                            int propertyValueBufferSize,
                            IntPtr propertyValueBuffer,
                            out int propertyValueBufferUsed);
    }
}
