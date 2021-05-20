// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public class DateTimeMarshaler
    {
        public static string FromJavaScriptPreFilter { get; } = "value.toISOString()";
        public static string ToJavaScriptPostFilter { get; } = "new Date(value)";

        public static DateTime FromJavaScript (string s)
        {
            // Debug.WriteLine($"DateTime.FromJavaScript('{s}')");
            return DateTime.Parse(s).ToUniversalTime();
        }

        public static string ToJavaScript (ref DateTime dt)
        {
            // Debug.WriteLine($"DateTime.ToJavaScript({dt})");
            return dt.ToString("o");
        }
    }

    public class DateTimeOffsetMarshaler
    {
        public static string FromJavaScriptPreFilter { get; } = "value.toISOString()";
        public static string ToJavaScriptPostFilter { get; } = "new Date(value)";

        public static DateTimeOffset FromJavaScript (string s)
        {
            // Debug.WriteLine($"DateTimeOffset.FromJavaScript('{s}')");
            return DateTimeOffset.Parse(s);
        }

        public static string ToJavaScript (ref DateTimeOffset dto)
        {
            // Debug.WriteLine($"DateTimeOffset.ToJavaScript({dto})");
            return dto.ToString("o");
        }
    }
}