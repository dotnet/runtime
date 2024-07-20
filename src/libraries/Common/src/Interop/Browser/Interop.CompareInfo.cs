// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint CompareString(char* culture, int cultureLength, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options, out int resultPtr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint StartsWith(char* culture, int cultureLength, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options, out bool resultPtr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint EndsWith(char* culture, int cultureLength, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options, out bool resultPtr);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint IndexOf(char* culture, int cultureLength, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options, bool fromBeginning, out int resultPtr);
    }
}
