// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class DateTimeOffsetMarshaler
    {
        public static string JavaScriptToInterchangeTransform => DateTimeMarshaler.JavaScriptToInterchangeTransform;
        public static string InterchangeToJavaScriptTransform => DateTimeMarshaler.InterchangeToJavaScriptTransform;

        public static DateTimeOffset FromJavaScript (double msecsSinceEpoch)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)msecsSinceEpoch);
        }

        public static double ToJavaScript (in DateTimeOffset dto)
        {
            return (double)dto.ToUnixTimeMilliseconds();
        }
    }
}