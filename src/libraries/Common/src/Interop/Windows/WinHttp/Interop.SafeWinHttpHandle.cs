// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class WinHttp
    {
        internal class SafeWinHttpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeWinHttpHandle? _parentHandle;

            public SafeWinHttpHandle() : base(true)
            {
            }

            public static void DisposeAndClearHandle(ref SafeWinHttpHandle? safeHandle)
            {
                if (safeHandle != null)
                {
                    safeHandle.Dispose();
                    safeHandle = null;
                }
            }

            public void SetParentHandle(SafeWinHttpHandle parentHandle)
            {
                Debug.Assert(_parentHandle == null);
                Debug.Assert(parentHandle != null);
                Debug.Assert(!parentHandle.IsInvalid);

                bool ignore = false;
                parentHandle.DangerousAddRef(ref ignore);

                _parentHandle = parentHandle;
            }

            // Important: WinHttp API calls should not happen while another WinHttp call for the same handle did not
            // return. During finalization that was not initiated by the Dispose pattern we don't expect any other WinHttp
            // calls in progress.
            protected override bool ReleaseHandle()
            {
                if (_parentHandle != null)
                {
                    _parentHandle.DangerousRelease();
                    _parentHandle = null;
                }

                return Interop.WinHttp.WinHttpCloseHandle(handle);
            }
        }
    }
}
