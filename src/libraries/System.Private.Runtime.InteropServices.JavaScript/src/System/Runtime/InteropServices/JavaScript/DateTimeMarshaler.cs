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
        public static string FromJavaScriptPreFilter => "BINDING._pre_filter_date(value)";
        public static string ToJavaScriptPostFilter => "new Date(value)";

        public static DateTime FromJavaScript (double msecsSinceEpoch)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds((long)msecsSinceEpoch).UtcDateTime;
        }

        public static double ToJavaScript (in DateTime dt)
        {
            return (double)((new DateTimeOffset(dt)).ToUnixTimeMilliseconds());
        }
    }

    public static class DateTimeOffsetMarshaler
    {
        public static string FromJavaScriptPreFilter => "BINDING._pre_filter_date(value)";
        public static string ToJavaScriptPostFilter => "new Date(value)";

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