// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        internal sealed class SafeCurlSlistHandle : SafeHandle
        {
            public SafeCurlSlistHandle() : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return handle == IntPtr.Zero; }
            }

            public new void SetHandle(IntPtr newHandle)
            {
                base.SetHandle(newHandle);
            }

            protected override bool ReleaseHandle()
            {
                curl_slist_free_all(handle);
                return true;
            }
        }
    }
}
