// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;

namespace Claunia.PropertyList
{
    /// <summary>Represents a date</summary>
    /// @author Daniel Dreibrodt
    /// @author Natalia Portillo
    public class NSDate : NSObject
    {
        private static readonly DateTime EPOCH = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // The datetime ends with 'Z', which indicates UTC time. To make sure .NET
        // understands the 'Z' character as a timezone, specify the 'K' format string.
        private const string sdfDefault = "yyyy-MM-dd'T'HH:mm:ssK";
        private const string sdfGnuStep = "yyyy-MM-dd HH:mm:ss zzz";
        private static readonly string[] sdfAll =
        {
            sdfDefault, sdfGnuStep
        };
        private static readonly CultureInfo provider = CultureInfo.InvariantCulture;

        /// <summary>Creates a date from its binary representation.</summary>
        /// <param name="bytes">bytes The date bytes</param>
        public NSDate(ReadOnlySpan<byte> bytes) =>

            //dates are 8 byte big-endian double, seconds since the epoch
            Date = EPOCH.AddSeconds(BinaryPropertyListParser.ParseDouble(bytes));

        /// <summary>
        ///     Parses a date from its textual representation. That representation has the following pattern:
        ///     <code>yyyy-MM-dd'T'HH:mm:ss'Z'</code>
        /// </summary>
        /// <param name="textRepresentation">The textual representation of the date (ISO 8601 format)</param>
        /// <exception cref="FormatException">When the date could not be parsed, i.e. it does not match the expected pattern.</exception>
        public NSDate(string textRepresentation) => Date = ParseDateString(textRepresentation);

        /// <summary>Creates a NSDate from a .NET DateTime</summary>
        /// <param name="d">The date</param>
        public NSDate(DateTime d) => Date = d;

        /// <summary>Gets the date.</summary>
        /// <returns>The date.</returns>
        public DateTime Date { get; }

        /// <summary>Parses the XML date string and creates a .NET DateTime object from it.</summary>
        /// <returns>The parsed Date</returns>
        /// <param name="textRepresentation">The date string as found in the XML property list</param>
        /// <exception cref="FormatException">Given string cannot be parsed</exception>
        private static DateTime ParseDateString(string textRepresentation) =>
            DateTime.ParseExact(textRepresentation, sdfAll, provider, DateTimeStyles.None);

        /// <summary>
        ///     Generates a String representation of a .NET DateTime object. The string is formatted according to the
        ///     specification for XML property list dates.
        /// </summary>
        /// <param name="date">The date which should be represented.</param>
        /// <returns>The string representation of the date.</returns>
        public static string MakeDateString(DateTime date) => date.ToUniversalTime().ToString(sdfDefault, provider);

        /// <summary>
        ///     Generates a String representation of a .NET DateTime object. The string is formatted according to the
        ///     specification for GnuStep ASCII property list dates.
        /// </summary>
        /// <param name="date">The date which should be represented.</param>
        /// <returns>The string representation of the date.</returns>
        private static string MakeDateStringGnuStep(DateTime date) => date.ToString(sdfGnuStep, provider);

        /// <summary>
        ///     Determines whether the specified <see cref="object" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSDate" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="object" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.NSDate" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="object" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSDate" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj.GetType().Equals(GetType()) && Date.Equals(((NSDate)obj).Date);

        /// <summary>Serves as a hash function for a <see cref="Claunia.PropertyList.NSDate" /> object.</summary>
        /// <returns>
        ///     A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a
        ///     hash table.
        /// </returns>
        public override int GetHashCode() => Date.GetHashCode();

        internal override void ToXml(StringBuilder xml, int level)
        {
            Indent(xml, level);
            xml.Append("<date>");
            xml.Append(MakeDateString(Date));
            xml.Append("</date>");
        }

        internal override void ToBinary(BinaryPropertyListWriter outPlist)
        {
            outPlist.Write(0x33);
            outPlist.WriteDouble((Date - EPOCH).TotalSeconds);
        }

        /// <summary>Generates a string representation of the date.</summary>
        /// <returns>A string representation of the date.</returns>
        public override string ToString() => Date.ToString();

        internal override void ToASCII(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append('"');
            ascii.Append(MakeDateString(Date));
            ascii.Append('"');
        }

        internal override void ToASCIIGnuStep(StringBuilder ascii, int level)
        {
            Indent(ascii, level);
            ascii.Append("<*D");
            ascii.Append(MakeDateStringGnuStep(Date));
            ascii.Append('>');
        }

        /// <summary>
        ///     Determines whether the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSDate" />.
        /// </summary>
        /// <param name="obj">
        ///     The <see cref="Claunia.PropertyList.NSObject" /> to compare with the current
        ///     <see cref="Claunia.PropertyList.NSDate" />.
        /// </param>
        /// <returns>
        ///     <c>true</c> if the specified <see cref="Claunia.PropertyList.NSObject" /> is equal to the current
        ///     <see cref="Claunia.PropertyList.NSDate" />; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(NSObject obj)
        {
            if(obj is not NSDate date)
                return false;

            int equality = DateTime.Compare(Date, date.Date);

            return equality == 0;
        }

        public static explicit operator DateTime(NSDate value) => value.Date;

        public static explicit operator NSDate(DateTime value) => new(value);
    }
}
