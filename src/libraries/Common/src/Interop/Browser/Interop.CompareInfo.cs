// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int CompareString(out string exceptionMessage, in string culture, in string str1, in string str2, global::System.Globalization.CompareOptions options);
    }
}
