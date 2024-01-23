// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.HybridGlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCaseNative", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int ChangeCaseNative(string localeName, int lNameLen, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, [MarshalAs(UnmanagedType.Bool)] bool bToUpper);

        [LibraryImport(Libraries.HybridGlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCaseInvariantNative", StringMarshalling = StringMarshalling.Utf8)]
        internal static unsafe partial int ChangeCaseInvariantNative(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, [MarshalAs(UnmanagedType.Bool)] bool bToUpper);

        [LibraryImport(Libraries.HybridGlobalizationNative, EntryPoint = "GlobalizationNative_InitOrdinalCasingPage", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial void InitOrdinalCasingPageNative(int pageNumber, char* pTarget);
    }
}
