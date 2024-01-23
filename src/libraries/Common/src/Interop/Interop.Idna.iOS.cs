// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {

        [LibraryImport(Libraries.HybridGlobalizationNative, EntryPoint = "GlobalizationNative_ToAscii", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int ToAsciiNative(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

        [LibraryImport(Libraries.HybridGlobalizationNative, EntryPoint = "GlobalizationNative_ToUnicode", StringMarshalling = StringMarshalling.Utf16)]
        internal static unsafe partial int ToUnicodeNative(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
