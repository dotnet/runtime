// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetSortHandle")]
        internal static extern unsafe ResultCode GetSortHandle(byte[] localeName, out CriticalSortHandle sortHandle);

        [SuppressUnmanagedCodeSecurity]
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CloseSortHandle")]
        internal static extern unsafe void CloseSortHandle(IntPtr handle);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareString")]
        internal static extern unsafe int CompareString(CriticalSortHandle sortHandle, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOf")]
        internal static extern unsafe int IndexOf(CriticalSortHandle sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_LastIndexOf")]
        internal static extern unsafe int LastIndexOf(CriticalSortHandle sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOfOrdinalIgnoreCase")]
        internal static extern unsafe int IndexOfOrdinalIgnoreCase(string target, int cwTargetLength, char* pSource, int cwSourceLength, bool findLast);
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOfOrdinalIgnoreCase")]
        internal static extern unsafe int IndexOfOrdinalIgnoreCase(char* target, int cwTargetLength, char* pSource, int cwSourceLength, bool findLast);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_StartsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool StartsWith(CriticalSortHandle sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EndsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool EndsWith(CriticalSortHandle sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_StartsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool StartsWith(CriticalSortHandle sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EndsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool EndsWith(CriticalSortHandle sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetSortKey")]
        internal static extern unsafe int GetSortKey(CriticalSortHandle sortHandle, char* str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);

        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareStringOrdinalIgnoreCase")]
        internal static extern unsafe int CompareStringOrdinalIgnoreCase(char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len);

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetSortVersion")]
        internal static extern int GetSortVersion(CriticalSortHandle sortHandle);

        internal class CriticalSortHandle : CriticalHandle
        {
            private CriticalSortHandle() :
                base(IntPtr.Zero)
            {
            }

            public override bool IsInvalid
            {
                get { return handle == IntPtr.Zero; }
            }

            protected override bool ReleaseHandle()
            {
                CloseSortHandle(handle);
                SetHandle(IntPtr.Zero);
                return true;
            }
        }
    }
}
