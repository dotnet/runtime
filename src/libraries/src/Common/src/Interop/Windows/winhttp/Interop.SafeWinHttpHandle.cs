// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class WinHttp
    {
        // Issue 2501: Fold SafeWinHttpHandleWithCallback and SafeWinHttpHandle into a single type.
        // SafeWinHttpHandleWithCallback is incorrectly overriding the Dispose(bool disposing) method
        // and will be fixed as part of merging the classes.
        internal class SafeWinHttpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeWinHttpHandle _parentHandle = null;
            
            public SafeWinHttpHandle() : base(true)
            {
            }

            public static void DisposeAndClearHandle(ref SafeWinHttpHandle safeHandle)
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
                
                // TODO (#7856): Add logging so we know when the handle gets closed.
                return Interop.WinHttp.WinHttpCloseHandle(handle);
            }
        }

        internal sealed class SafeWinHttpHandleWithCallback : SafeWinHttpHandle
        {
            // Add a reference to this object so that the AppDomain doesn't get unloaded before the last WinHttp callback.
            public void AttachCallback()
            {
                bool ignore = false;
                DangerousAddRef(ref ignore);
            }

            public void DetachCallback()
            {
                DangerousRelease();
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    // We need to initiate the asynchronous process of closing the WinHttp handle:
                    // 1. Ensure that all other WinHttp function calls for this handle have returned.
                    // 2. Call WinHttpCloseHandle.
                    // 3. Wait for WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING in the callback.
                    //    On this event, call DetachCallback.
                    //
                    // WinHttp guarantees that no other calls to the callback are made for this handle after 
                    // WINHTTP_CALLBACK_STATUS_HANDLE_CLOSING has been received.
                    // Holding the reference to the SafeHandle object ensures that the appdomain where the SafeHandle 
                    // lives until it is guaranteed that no further callback calls are made from the native (WinHttp) side.
                    Interop.WinHttp.WinHttpCloseHandle(handle);
                }
            }

            protected override bool ReleaseHandle()
            {
                return base.ReleaseHandle();
            }
        }
    }
}
