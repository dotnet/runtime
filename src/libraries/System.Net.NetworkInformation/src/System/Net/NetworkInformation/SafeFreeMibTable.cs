// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation
{
    internal class SafeFreeMibTable : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeFreeMibTable() : base(true) { }

        protected override bool ReleaseHandle()
        {
            Interop.IpHlpApi.FreeMibTable(base.handle);
            base.handle = IntPtr.Zero;
            return true;
        }
    }
}
