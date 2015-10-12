// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
