// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeEventLogReadHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        internal SafeEventLogReadHandle() : base(true) { }

        [DllImport(Interop.Libraries.Advapi32, CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        internal static extern SafeEventLogReadHandle OpenEventLog(string UNCServerName, string sourceName);

        [DllImport(Interop.Libraries.Advapi32, SetLastError = true)]
        private static extern bool CloseEventLog(IntPtr hEventLog);

        protected override bool ReleaseHandle()
        {
            return CloseEventLog(handle);
        }
    }
}