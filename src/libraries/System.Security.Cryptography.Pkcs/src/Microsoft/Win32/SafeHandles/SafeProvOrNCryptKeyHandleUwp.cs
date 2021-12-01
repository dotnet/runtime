// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using ErrorCode = Interop.NCrypt.ErrorCode;

#pragma warning disable CA1419 // TODO https://github.com/dotnet/roslyn-analyzers/issues/5232: not intended for use with P/Invoke

namespace Microsoft.Win32.SafeHandles
{
    //
    // A UWP-safe implementation of SafeProvOrNCryptKeyHandle. On UWP, NCryptIsKeyHandle() is a prohibited api. So to use this,
    // you must tell SafeProvOrNCryptKeyHandleUwp which type of handle it is.
    //
    internal sealed class SafeProvOrNCryptKeyHandleUwp : SafeProvOrNCryptKeyHandle
    {
        internal SafeProvOrNCryptKeyHandleUwp(IntPtr handle, SafeHandle parentHandle)
            : this(handle, true, true)
        {
            Debug.Assert(parentHandle != null && !parentHandle.IsClosed && !parentHandle.IsInvalid);

            // If the provided handle value wasn't valid we won't call dispose, so we shouldn't be doing this.
            Debug.Assert(!IsInvalid);

            bool addedRef = false;
            parentHandle.DangerousAddRef(ref addedRef);
            _parentHandle = parentHandle;
        }

        internal SafeProvOrNCryptKeyHandleUwp(IntPtr handle, bool ownsHandle, bool isNcrypt)
            : base(handle, ownsHandle)
        {
            _isNcrypt = isNcrypt;
        }

        protected sealed override bool ReleaseHandle()
        {
            if (_parentHandle != null)
            {
                _parentHandle.DangerousRelease();
                _parentHandle = null;
                SetHandle(IntPtr.Zero);
                return true;
            }
            else if (_isNcrypt)
            {
                ErrorCode errorCode = Interop.NCrypt.NCryptFreeObject(handle);
                return errorCode == ErrorCode.ERROR_SUCCESS;
            }
            else
            {
                bool success = Interop.Advapi32.CryptReleaseContext(handle, 0);
                return success;
            }
        }

        private readonly bool _isNcrypt;
        private SafeHandle? _parentHandle;
    }
}
