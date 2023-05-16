// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int CompareString(out string exceptionMessage, in string culture, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool StartsWith(out string exceptionMessage, in string culture, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe bool EndsWith(out string exceptionMessage, in string culture, char* str1, int str1Len, char* str2, int str2Len, global::System.Globalization.CompareOptions options);
    }
}
