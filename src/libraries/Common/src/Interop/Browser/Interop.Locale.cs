// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

internal static partial class Interop
{
    internal static unsafe partial class JsGlobalization
    {
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetMonetarySymbol(in string culture, in string isoSymbol, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetMonetaryDecimalSeparator(in string culture, in string isoSymbol, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetDecimalSeparator(in string culture, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetMonetaryThousandSeparator(in string culture, in string isoSymbol, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetThousandSeparator(in string culture, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetDigits(in string culture, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGeCurrencyName(in string culture, in string isoSymbol, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetCountryName(in string culture, in string region, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetLanguageName(in string culture, in string region, char* buffer, int bufferLength, out int exceptionalResult, out object result);
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern unsafe int JsGetDisplayName(in string culture, in string displayCulture, char* buffer, int bufferLength, out int exceptionalResult, out object result);
    }
}
