// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class GlobalizationInterop
    {
        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetSortHandle")]
        internal unsafe static extern SafeSortHandle GetSortHandle(byte[] localeName);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CloseSortHandle")]
        internal unsafe static extern void CloseSortHandle(IntPtr handle);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareString")]
        internal unsafe static extern int CompareString(SafeSortHandle sortHandle, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOf")]
        internal unsafe static extern int IndexOf(SafeSortHandle sortHandle, string target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_LastIndexOf")]
        internal unsafe static extern int LastIndexOf(SafeSortHandle sortHandle, string target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOfOrdinalIgnoreCase")]
        internal unsafe static extern int IndexOfOrdinalIgnoreCase(string target, int cwTargetLength, char* pSource, int cwSourceLength, bool findLast);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_StartsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool StartsWith(SafeSortHandle sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EndsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal unsafe static extern bool EndsWith(SafeSortHandle sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetSortKey")]
        internal unsafe static extern int GetSortKey(SafeSortHandle sortHandle, string str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);

        [SecurityCritical]
        [DllImport(Libraries.GlobalizationInterop, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareStringOrdinalIgnoreCase")]
        internal unsafe static extern int CompareStringOrdinalIgnoreCase(char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len);

        [SecurityCritical]
        internal class SafeSortHandle : SafeHandle
        {
            private SafeSortHandle() :
                base(IntPtr.Zero, true)
            {
            }

            public override bool IsInvalid
            {
                [SecurityCritical]
                get { return handle == IntPtr.Zero; }
            }

            [SecurityCritical]
            protected override bool ReleaseHandle()
            {
                CloseSortHandle(handle);
                SetHandle(IntPtr.Zero);
                return true;
            }
        }
    }
}
