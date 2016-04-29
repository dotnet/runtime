// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace Microsoft.Win32.SafeHandles
{
    //
    // A UWP-safe implementation of SafeProvOrNCryptKeyHandle. On UWP, NCryptIsKeyHandle() is a prohibited api. So to use this,
    // you must tell SafeProvOrNCryptKeyHandleUwp which type of handle it is.
    // 
    internal sealed class SafeProvOrNCryptKeyHandleUwp : SafeProvOrNCryptKeyHandle
    {
        internal SafeProvOrNCryptKeyHandleUwp(IntPtr handle, bool ownsHandle, bool isNcrypt)
            : base(handle, ownsHandle)
        {
            _isNcrypt = isNcrypt;
        }

        protected sealed override bool ReleaseHandle()
        {
            if (_isNcrypt)
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptFreeObject(handle);
                return errorCode == ErrorCode.ERROR_SUCCESS;
            }
            else
            {
                bool success = Interop.Crypt32.CryptReleaseContext(handle, 0);
                return success;
            }
        }

        private readonly bool _isNcrypt;
    }
}

