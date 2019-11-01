// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Http
    {
        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_SListAppend", CharSet = CharSet.Ansi)]
        private static extern IntPtr SListAppend(IntPtr slist, string headerValue);

        internal static bool SListAppend(SafeCurlSListHandle slist, string headerValue)
        {
            bool gotRef = false;
            try
            {
                slist.DangerousAddRef(ref gotRef);
                IntPtr newHandle = SListAppend(slist.DangerousGetHandle(), headerValue);
                if (newHandle != IntPtr.Zero)
                {
                    slist.SetHandle(newHandle);
                    return true;
                }
                return false;
            }
            finally
            {
                if (gotRef)
                    slist.DangerousRelease();
            }
        }

        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_SListFreeAll")]
        private static extern void SListFreeAll(IntPtr slist);

        internal sealed class SafeCurlSListHandle : SafeHandle
        {
            public SafeCurlSListHandle() : base(IntPtr.Zero, true)
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
                SListFreeAll(handle);
                SetHandle(IntPtr.Zero);
                return true;
            }
        }
    }
}
