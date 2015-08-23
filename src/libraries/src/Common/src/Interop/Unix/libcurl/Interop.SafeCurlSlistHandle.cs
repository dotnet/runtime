// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using size_t = System.IntPtr;
using libcurl = Interop.libcurl;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        internal sealed class SafeCurlSlistHandle : SafeHandle
        {
            public SafeCurlSlistHandle()
                : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero; }
            }

            public new void SetHandle(IntPtr handle)
            {
                base.SetHandle(handle);
            }

            public static void DisposeAndClearHandle(ref SafeCurlSlistHandle curlHandle)
            {
                if (curlHandle != null)
                {
                    curlHandle.Dispose();
                    curlHandle = null;
                }
            }

            protected override bool ReleaseHandle()
            {
                libcurl.curl_slist_free_all(this.handle);
                return true;
            }
        }
    }
}
