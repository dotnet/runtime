// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe nint GetLocaleInfo(char* locale, int localeLength, char* culture, int cultureLength, char* buffer, int bufferLength, out int resultLength);
    }
}
