// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEventLogWriteHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeEventLogWriteHandle() : base(true) { }

        [DllImport(Interop.Libraries.Advapi32, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        internal static extern SafeEventLogWriteHandle RegisterEventSource(string uncServerName, string sourceName);

        [DllImport(Interop.Libraries.Advapi32, SetLastError = true)]
        private static extern bool DeregisterEventSource(IntPtr hEventLog);

        protected override bool ReleaseHandle()
        {
            return DeregisterEventSource(handle);
        }
    }
}