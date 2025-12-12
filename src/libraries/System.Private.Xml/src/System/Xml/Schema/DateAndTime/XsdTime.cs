// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema.DateAndTime.Converters;
using System.Xml.Schema.DateAndTime.Helpers;

namespace System.Xml.Schema.DateAndTime
{
    internal struct XsdTime : IFormattable
    {
        private TimeOnly Time { get; set; }

        private XsdTime(TimeInfo parsedValue)
            : this()
        {
            Time = new TimeOnly(
                parsedValue.Hour,
                parsedValue.Minute,
                parsedValue.Second,
                parsedValue.Millisecond,
                parsedValue.Microsecond);
        }

        /// <inheritdoc/>
        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return Time.ToString(format, formatProvider);
        }

        internal static bool TryParse(string text, out XsdTime result)
        {
            if (!DateAndTimeConverter.TryParse(text, out TimeInfo parsedValue))
            {
                result = default;
                return false;
            }

            result = new XsdTime(parsedValue);
            return true;
        }
    }
}
