// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema.DateAndTime.Converters;
using System.Xml.Schema.DateAndTime.Helpers;

namespace System.Xml.Schema.DateAndTime
{
    internal struct XsdDate : IFormattable
    {
        private DateOnly Date { get; set; }

        private XsdDate(DateInfo parsedValue)
            : this()
        {
            Date = new DateOnly(parsedValue.Year, parsedValue.Month, parsedValue.Day);
        }

        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Date.ToString(format, formatProvider);
        }

        internal static bool TryParse(string text, out XsdDate result)
        {
            if (!DateAndTimeConverter.TryParse(text, out DateInfo parsedValue))
            {
                result = default;
                return false;
            }

            result = new XsdDate(parsedValue);
            return true;
        }
    }
}
