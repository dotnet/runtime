// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    internal sealed class SafeLsaHandle : SafeHandle
    {
        internal SafeLsaHandle()
            : base(IntPtr.Zero, true)
        {
        }

        protected sealed override bool ReleaseHandle()
        {
            int ntStatus = Interop.SspiCli.LsaDeregisterLogonProcess(handle);
            return ntStatus == 0;
        }

        public sealed override bool IsInvalid
        {
            get
            {
                return handle == IntPtr.Zero;
            }
        }
    }
} 
