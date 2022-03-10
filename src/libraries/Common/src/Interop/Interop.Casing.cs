// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCase", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial void ChangeCase(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, [MarshalAs(UnmanagedType.Bool)] bool bToUpper);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCaseInvariant", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial void ChangeCaseInvariant(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, [MarshalAs(UnmanagedType.Bool)] bool bToUpper);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ChangeCaseTurkish", StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial void ChangeCaseTurkish(char* src, int srcLen, char* dstBuffer, int dstBufferCapacity, [MarshalAs(UnmanagedType.Bool)] bool bToUpper);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_InitOrdinalCasingPage", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial void InitOrdinalCasingPage(int pageNumber, char* pTarget);
    }
}
