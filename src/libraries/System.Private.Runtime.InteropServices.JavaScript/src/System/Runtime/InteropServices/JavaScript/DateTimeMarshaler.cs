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

        public static DateTime FromJavaScript (string s)
        {
            // For consistency with the old DateTime marshaling implementation we
            //  convert JS Date values (which have no time zone) to UTC after parsing.
            // toISOString always produces UTC strings anyway, so this is correct.
            return DateTime.Parse(s).ToUniversalTime();
        }

        public static string ToJavaScript (in DateTime dt)
        {
            // "o" produces a culture-independent ISO 8601 datetime value that can be
            //  safely exchanged with JavaScript.
            return dt.ToString("o");
        }
    }

    public static class DateTimeOffsetMarshaler
    {
        public static string FromJavaScriptPreFilter => "BINDING._pre_filter_date(value)";
        public static string ToJavaScriptPostFilter => "new Date(value)";

        public static DateTimeOffset FromJavaScript (string s)
        {
            return DateTimeOffset.Parse(s);
        }

        public static string ToJavaScript (in DateTimeOffset dto)
        {
            // As above, produces a culture-independent ISO 8601 datetime value
            return dto.ToString("o");
        }
    }
}