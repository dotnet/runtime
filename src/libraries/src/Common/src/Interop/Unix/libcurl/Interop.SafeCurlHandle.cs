// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcurl
    {
        internal sealed class SafeCurlHandle : SafeHandle
        {
            public SafeCurlHandle() : base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                get { return this.handle == IntPtr.Zero; }
            }

            public static void DisposeAndClearHandle(ref SafeCurlHandle curlHandle)
            {
                if (curlHandle != null)
                {
                    curlHandle.Dispose();
                    curlHandle = null;
                }
            }

            protected override bool ReleaseHandle()
            {
                Interop.libcurl.curl_easy_cleanup(this.handle);
                return true;
            }
        }

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

            public static void DisposeAndClearHandle(ref SafeCurlMultiHandle curlHandle)
            {
                if (curlHandle != null)
                {
                    curlHandle.Dispose();
                    curlHandle = null;
                }
            }

            protected override bool ReleaseHandle()
            {
                Interop.libcurl.curl_multi_cleanup(this.handle);
                return true;
            }
        }

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
                Interop.libcurl.curl_slist_free_all(this.handle);
                return true;
            }
        }
    }
}
