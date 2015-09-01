// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        internal sealed class SafeCurlMultiHandle : SafeHandle
        {
            public SafeCurlMultiHandle()
                : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero; }
            }

            protected override bool ReleaseHandle()
            {
                return curl_multi_cleanup(handle) == CURLMcode.CURLM_OK;
            }
        }
    }
}
