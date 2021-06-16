// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public static class DateTimeMarshaler
    {
        public static string JavaScriptToInterchangeTransform => "return BINDING._pre_filter_date(value)";
        public static string InterchangeToJavaScriptTransform => "return new Date(value)";

        public static DateTime FromJavaScript (double msecsSinceEpoch)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)msecsSinceEpoch).UtcDateTime;
        }

        public static double ToJavaScript (in DateTime dt)
        {
            return (double)new DateTimeOffset(dt).ToUnixTimeMilliseconds();
        }
    }
}