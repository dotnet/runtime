// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class WinHttp
    {
        // TODO: Move SafeHandleZeroOrMinusOneIsInvalid class to Common
        internal abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
        {
            protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
                : base(IntPtr.Zero, ownsHandle)
            {
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero || this.handle == (IntPtr)(-1); }
            }
        }

        internal class SafeWinHttpHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeWinHttpHandle() : base(true)
            {
            }

            public static void DisposeAndClearHandle(ref SafeWinHttpHandle handle)
            {
                if (handle != null)
                {
                    handle.Dispose();
                    handle = null;
                }
            }

            protected override bool ReleaseHandle()
            {
                // TODO: Add logging so we know when the handle gets closed.
                return Interop.WinHttp.WinHttpCloseHandle(this.handle);
            }
        }
    }
}
