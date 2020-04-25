// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Ansi, EntryPoint = "GlobalizationNative_GetSortHandle")]
#endif
        internal static extern unsafe ResultCode GetSortHandle(string localeName, out IntPtr sortHandle);

        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_CloseSortHandle")]
        internal static extern unsafe void CloseSortHandle(IntPtr handle);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareString")]
#endif
        internal static extern unsafe int CompareString(IntPtr sortHandle, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOf")]
#endif
        internal static extern unsafe int IndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_LastIndexOf")]
#endif
        internal static extern unsafe int LastIndexOf(IntPtr sortHandle, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, int* matchLengthPtr);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOfOrdinalIgnoreCase")]
#endif
        internal static extern unsafe int IndexOfOrdinalIgnoreCase(string target, int cwTargetLength, char* pSource, int cwSourceLength, bool findLast);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_IndexOfOrdinalIgnoreCase")]
#endif
        internal static extern unsafe int IndexOfOrdinalIgnoreCase(char* target, int cwTargetLength, char* pSource, int cwSourceLength, bool findLast);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_StartsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool StartsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EndsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool EndsWith(IntPtr sortHandle, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_StartsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool StartsWith(IntPtr sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_EndsWith")]
        [return: MarshalAs(UnmanagedType.Bool)]
#endif
        internal static extern unsafe bool EndsWith(IntPtr sortHandle, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_GetSortKey")]
#endif
        internal static extern unsafe int GetSortKey(IntPtr sortHandle, char* str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_CompareStringOrdinalIgnoreCase")]
#endif
        internal static extern unsafe int CompareStringOrdinalIgnoreCase(char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len);
#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetSortVersion")]
#endif
        internal static extern int GetSortVersion(IntPtr sortHandle);
    }
}
