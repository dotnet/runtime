// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal struct Range
    {
        public int Location;
        public int Length;
    }

    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_CompareStringNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int CompareStringNative(string localeName, int lNameLen, char* lpStr1, int cwStr1Len, char* lpStr2, int cwStr2Len, CompareOptions options);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_EndsWithNative", StringMarshalling = StringMarshalling.Utf16)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe partial int EndsWithNative(string localeName, int lNameLen, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_IndexOfNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial Range IndexOfNative(string localeName, int lNameLen, char* target, int cwTargetLength, char* pSource, int cwSourceLength, CompareOptions options, [MarshalAs(UnmanagedType.Bool)] bool fromBeginning);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_StartsWithNative", StringMarshalling = StringMarshalling.Utf16)]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static unsafe partial int StartsWithNative(string localeName, int lNameLen, char* target, int cwTargetLength, char* source, int cwSourceLength, CompareOptions options);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_GetSortKeyNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int GetSortKeyNative(string localeName, int lNameLen, char* str, int strLength, byte* sortKey, int sortKeyLength, CompareOptions options);
    }
}
