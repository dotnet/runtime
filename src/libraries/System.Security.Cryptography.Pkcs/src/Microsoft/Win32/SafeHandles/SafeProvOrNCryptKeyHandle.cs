// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles
{
    // 
    // Abstract base for a handle that can be either HCRYPTPROV or NCRYPT_KEY_HANDLE. The CNG api NCryptIsKeyHandle() determines which it is.
    //
    // Unfortunately, NCryptIsKeyHandle() is a prohibited api on UWP. This means that packages that want to run on both UWP and non-UWP must
    // use #if's to avoid declaring a P/Invoke for NCryptIsKeyHandle(). They must also be very careful never to allow a HCRYPTPROV to leak into this
    // since on UWP, SafeProvOrNCryptKeyHandle will use the wrong Windows api to "free" the handle, leading to unpredictable, but probably bad, results.
    //
    // Because of this, we only provide the abstract base class. The most appropriate implementation is up to the package to choose.
    //
    internal abstract class SafeProvOrNCryptKeyHandle : SafeHandle
    {
        internal SafeProvOrNCryptKeyHandle(IntPtr handle, bool ownsHandle)
            : base(handle, ownsHandle)
        {
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

