// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static partial class Globalization
    {
        internal const int AllowUnassigned = 0x1;
        internal const int UseStd3AsciiRules = 0x2;

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ToAscii")]
#endif
        internal static extern unsafe int ToAscii(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);

#if MONO
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
#else
        [DllImport(Libraries.GlobalizationNative, CharSet = CharSet.Unicode, EntryPoint = "GlobalizationNative_ToUnicode")]
#endif
        internal static extern unsafe int ToUnicode(uint flags, char* src, int srcLen, char* dstBuffer, int dstBufferCapacity);
    }
}
