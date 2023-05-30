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
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_CompareStringNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int CompareStringNative(string localeName, int lNameLen, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IndexOfNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial NSRange IndexOfNative(string localeName, int lNameLen, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, [MarshalAs(UnmanagedType.Bool)] bool fromBeginning);

        /*[LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_StartsWith", StringMarshalling = StringMarshalling.Utf16)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool StartsWith(string localeName, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EndsWith", StringMarshalling = StringMarshalling.Utf16)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool EndsWith(string localeName, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options, int* matchedLength);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_StartsWith", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool StartsWith(string localeName, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EndsWith", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EndsWith(string localeName, string target, int cwTargetLength, string source, int cwSourceLength, CompareOptions options);*/

    }
}
