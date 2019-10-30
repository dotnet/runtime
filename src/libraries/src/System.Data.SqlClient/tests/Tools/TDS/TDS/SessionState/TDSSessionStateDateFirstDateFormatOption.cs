// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace Microsoft.SqlServer.TDS.SessionState
{
    /// <summary>
    /// Session state for the first date of the week
    /// </summary>
    public class TDSSessionStateDateFirstDateFormatOption : TDSSessionStateOption
    {
        /// <summary>
        /// Identifier of the session state option
        /// </summary>
        public const byte ID = 2;

        /// <summary>
        /// First day of the week
        /// </summary>
        public byte DateFirst { get; set; }

        /// <summary>
        /// First day of the week
        /// </summary>
        public DateFormatType DateFormat { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TDSSessionStateDateFirstDateFormatOption() :
            base(ID) // State identifier
        {
        }

        /// <summary>
        /// Deflate state into the stream
        /// </summary>
        public override void Deflate(Stream destination)
        {
            // Write state ID
            destination.WriteByte(StateID);

            // Allocate a container
            byte[] value = new byte[2];

            // Put the date first
            value[0] = DateFirst;

            // Put the date format
            value[1] = _ToValue(DateFormat);

            // Store the value
            DeflateValue(destination, value);
        }

        /// <summary>
        /// Inflate from stream
        /// </summary>
        public override bool Inflate(Stream source)
        {
            // Reset inflation size
            InflationSize = 0;

            // NOTE: state ID is skipped because it is read by the construction factory

            // Read the value
            byte[] value = InflateValue(source);

            // Read the first byte
            DateFirst = value[0];  // VSTS# 1023839 - OIPI documentation for session state #2 (datefirst) is incorrect

            // Read the second byte
            DateFormat = _ToEnum(value[1]);

            // Inflation is complete
            return true;
        }

        /// <summary>
        /// Convert a wire representation to enum
        /// </summary>
        private DateFormatType _ToEnum(byte value)
        {
            switch (value)
            {
                case 1:
                    {
                        return DateFormatType.MonthDayYear;
                    }
                case 2:
                    {
                        return DateFormatType.DayMonthYear;
                    }
                case 3:
                    {
                        return DateFormatType.YearMonthDay;
                    }
                case 4:
                    {
                        return DateFormatType.YearDayMonth;
                    }
                case 5:
                    {
                        return DateFormatType.MonthYearDay;
                    }
                case 6:
                    {
                        return DateFormatType.DayYearMonth;
                    }
                default:
                    {
                        throw new Exception("Unrecognized date format value " + value.ToString());
                    }
            }
        }

        /// <summary>
        /// Convert enum to wire format
        /// </summary>
        private byte _ToValue(DateFormatType value)
        {
            switch (value)
            {
                case DateFormatType.MonthDayYear:
                    {
                        return 1;
                    }
                case DateFormatType.DayMonthYear:
                    {
                        return 2;
                    }
                case DateFormatType.YearMonthDay:
                    {
                        return 3;
                    }
                case DateFormatType.YearDayMonth:
                    {
                        return 4;
                    }
                case DateFormatType.MonthYearDay:
                    {
                        return 5;
                    }
                case DateFormatType.DayYearMonth:
                    {
                        return 6;
                    }
                default:
                    {
                        throw new Exception("Unrecognized date format " + value.ToString());
                    }
            }
        }
    }
}
