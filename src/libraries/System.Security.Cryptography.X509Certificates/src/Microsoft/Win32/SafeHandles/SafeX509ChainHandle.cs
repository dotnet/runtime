// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Internal.Cryptography;
using Internal.Cryptography.Pal;

namespace Microsoft.Win32.SafeHandles
{
    public sealed class SafeX509ChainHandle : SafeHandle
    {
        private SafeX509ChainHandle()
            : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid
        {
            get { return handle == IntPtr.Zero; }
        }

        protected override bool ReleaseHandle()
        {
            return ChainPal.ReleaseSafeX509ChainHandle(handle);
        }

        internal static SafeX509ChainHandle InvalidHandle
        {
            get { return new SafeX509ChainHandle(); }
        }
    }
}

