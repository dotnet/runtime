// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.Headers
{
    // Don't derive from BaseHeaderParser since parsing is delegated to DateTimeOffset.TryParseExact() 
    // which will remove leading, trailing, and whitespace in the middle of the string.
    internal class DateHeaderParser : HttpHeaderParser
    {
        internal static readonly DateHeaderParser Parser = new DateHeaderParser();

        private DateHeaderParser()
            : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is DateTimeOffset);

            return HttpRuleParser.DateToString((DateTimeOffset)value);
        }

        public override bool TryParseValue(string value, object storeValue, ref int index, out object parsedValue)
        {
            parsedValue = null;

            // Some headers support empty/null values. This one doesn't.
            if (string.IsNullOrEmpty(value) || (index == value.Length))
            {
                return false;
            }

            string dateString = value;
            if (index > 0)
            {
                dateString = value.Substring(index);
            }

            DateTimeOffset date;
            if (!HttpRuleParser.TryStringToDate(dateString, out date))
            {
                return false;
            }

            index = value.Length;
            parsedValue = date;
            return true;
        }
    }
}
