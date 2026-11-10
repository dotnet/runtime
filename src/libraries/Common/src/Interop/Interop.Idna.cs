// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        internal const int AllowUnassigned = 0x1;
        internal const int UseStd3AsciiRules = 0x2;

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ToAscii", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int ToAscii(uint flags, ReadOnlySpan<char> src, int srcLen, Span<char> dstBuffer, int dstBufferCapacity);

        [LibraryImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ToUnicode", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial int ToUnicode(uint flags, ReadOnlySpan<char> src, int srcLen, Span<char> dstBuffer, int dstBufferCapacity);
    }
}
