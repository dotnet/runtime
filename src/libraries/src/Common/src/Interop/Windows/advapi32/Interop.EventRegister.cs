// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Advapi32
    {
        [DllImport(Interop.Libraries.Advapi32)]
        internal extern static uint EventRegister(ref Guid ProviderId, EtwEnableCallback EnableCallback, IntPtr CallbackContext, out ulong RegHandle);

        internal unsafe delegate void EtwEnableCallback(
            ref Guid sourceId,
            int isEnabled,
            byte level,
            ulong matchAnyKeywords,
            ulong matchAllKeywords,
            Interop.Kernel32.EVENT_FILTER_DESCRIPTOR* filterData,
            IntPtr callbackContext
            );

        [StructLayout(LayoutKind.Sequential)]
        internal struct EVENT_FILTER_DESCRIPTOR
        {
            internal ulong Ptr;
            internal uint Size;
            internal uint Type;
        }

        //
        // Constants error coded returned by ETW APIs
        //

        // The event size is larger than the allowed maximum (64k - header).
        internal const int ERROR_ARITHMETIC_OVERFLOW = 534;

        // Occurs when filled buffers are trying to flush to disk, 
        // but disk IOs are not happening fast enough. 
        // This happens when the disk is slow and event traffic is heavy. 
        // Eventually, there are no more free (empty) buffers and the event is dropped.
        internal const int ERROR_NOT_ENOUGH_MEMORY = 8;

        internal const int ERROR_MORE_DATA = 0xEA;
        internal const int ERROR_NOT_SUPPORTED = 50;
        internal const int ERROR_INVALID_PARAMETER = 0x57;

        //
        // ETW Methods
        //

        internal const int EVENT_CONTROL_CODE_DISABLE_PROVIDER = 0;
        internal const int EVENT_CONTROL_CODE_ENABLE_PROVIDER = 1;
        internal const int EVENT_CONTROL_CODE_CAPTURE_STATE = 2;
    }
}
