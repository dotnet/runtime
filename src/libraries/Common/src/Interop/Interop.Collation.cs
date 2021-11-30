// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetSortHandle", CharSet = CharSet.Ansi)]
        internal static unsafe partial ResultCode GetSortHandle(string localeName, out IntPtr sortHandle);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_CloseSortHandle")]
        internal static partial void CloseSortHandle(IntPtr handle);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_CompareString", CharSet = CharSet.Unicode)]
        internal static unsafe partial int CompareString(IntPtr sortHandle, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IndexOf", CharSet = CharSet.Unicode)]
        internal static unsafe partial int IndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_LastIndexOf", CharSet = CharSet.Unicode)]
        internal static unsafe partial int LastIndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_StartsWith", CharSet = CharSet.Unicode)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool StartsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EndsWith", CharSet = CharSet.Unicode)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool EndsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_StartsWith", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool StartsWith(IntPtr sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EndsWith", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EndsWith(IntPtr sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetSortKey", CharSet = CharSet.Unicode)]
        internal static unsafe partial int GetSortKey(IntPtr sortHandle, char* str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetSortVersion")]
        internal static partial int GetSortVersion(IntPtr sortHandle);
    }
}
