// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        internal const int AllowUnassigned = 0x1;
        internal const int UseStd3AsciiRules = 0x2;

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ToAscii", CharSet = CharSet.Unicode)]
        internal static unsafe partial int ToAscii(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

        [GeneratedDllImport(Libraries.GlobalizationNative, EntryPoint = "GlobalizationNative_ToUnicode", CharSet = CharSet.Unicode)]
        internal static unsafe partial int ToUnicode(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
