// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

internal partial class Interop
{
    internal partial class WinHttp
    {
        // Issue 2501: Fold SafeWinHttpHandleWithCallback and SafeWinHttpHandle into a single type.
        //
        // SafeWinHttpHandleWithCallback is a better pattern; SafeWinHttpHandle is probably a better
        // name. The implementation for the former should probably be folded into the latter.

        internal class SafeWinHttpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeWinHttpHandle() : base(true)
            {
            }

            public static void DisposeAndClearHandle(ref SafeWinHttpHandle winHttpHandler)
            {
                if (winHttpHandler != null)
                {
                    winHttpHandler.Dispose();
                    winHttpHandler = null;
                }
            }

            // Important: WinHttp API calls should not happen while another WinHttp call for the same handle did not 
            // return. During finalization that was not initiated by the Dispose pattern we don't expect any other WinHttp
            // calls in progress.
            protected override bool ReleaseHandle()
            {
                // TODO(Issue 2500): Add logging so we know when the handle gets closed.
                return Interop.WinHttp.WinHttpCloseHandle(handle);
            }
        }

        internal sealed class SafeWinHttpHandleWithCallback : SafeWinHttpHandle
        {
            // Add a reference to this object so that the AppDomain doesn't get unloaded before the last WinHttp callback.
            public void AttachCallback()
            {
                bool success = false;
                DangerousAddRef(ref success);
                if (!success)
                {
                    throw new InvalidOperationException("Could not increase SafeHandle reference.");
                }
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
