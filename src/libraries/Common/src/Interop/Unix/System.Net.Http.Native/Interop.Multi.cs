// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Http
    {
        [DllImport(Libraries.HttpNative)]
        public static extern SafeCurlMultiHandle MultiCreate();

        [DllImport(Libraries.HttpNative)]
        private static extern CURLMcode MultiDestroy(IntPtr handle);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLMcode MultiAddHandle(SafeCurlMultiHandle multi_handle, SafeCurlHandle easy_handle);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLMcode MultiRemoveHandle(SafeCurlMultiHandle multi_handle, SafeCurlHandle easy_handle);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLMcode MultiWait(
            SafeCurlMultiHandle multi_handle,
            int extraFileDescriptor,
            [MarshalAs(UnmanagedType.Bool)] out bool isExtraFileDescriptorActive,
            [MarshalAs(UnmanagedType.Bool)] out bool isTimeout);

        [DllImport(Libraries.HttpNative)]
        public static extern CURLMcode MultiPerform(SafeCurlMultiHandle multi_handle);

        [DllImport(Libraries.HttpNative)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool MultiInfoRead(SafeCurlMultiHandle multi_handle, out CURLMSG message, out IntPtr easy_handle, out CURLcode result);

        [DllImport(Libraries.HttpNative)]
        public static extern IntPtr MultiGetErrorString(int code);

        // Class for constants defined for the enum CURLMcode in multi.h
        internal enum CURLMcode : int
        {
            CURLM_OK = 0,
            CURLM_BAD_HANDLE = 1,
            CURLM_BAD_EASY_HANDLE = 2,
            CURLM_OUT_OF_MEMORY = 3,
            CURLM_INTERNAL_ERROR = 4,
            CURLM_BAD_SOCKET = 5,
            CURLM_UNKNOWN_OPTION = 6,
            CURLM_ADDED_ALREADY = 7,
        }

        // Class for constants defined for the enum CURLMSG in multi.h
        internal enum CURLMSG : int
        {
            CURLMSG_DONE = 1,
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

            protected override bool ReleaseHandle()
            {
                bool result = MultiDestroy(handle) == CURLMcode.CURLM_OK;
                SetHandle(IntPtr.Zero);
                return result;
            }
        }
    }
}
