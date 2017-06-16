//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Microsoft.ServiceModel.Syndication.Lab
{

    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml;
    using System.Globalization;

    public delegate DateTimeOffset DateParser(String dateTimeString, XmlReader reader);
    
    

    public class CustomRSSParsers
    {
        public static DateParser DateParser { get; set; }

        static  CustomRSSParsers()
        {
            //asign default parsers
            DateParser = DateParserAction;
        }
        
        //------ Part of lastBuildDate tag
        public static DateTimeOffset DateParserAction(string dateTimeString,XmlReader reader)
        {
            try {
                StringBuilder dateTimeStringBuilder = new StringBuilder(dateTimeString.Trim());
                if (dateTimeStringBuilder.Length < 18)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new XmlException(FeedUtils.AddLineInfo(reader,
                        SR.ErrorParsingDateTime)));
                }
                if (dateTimeStringBuilder[3] == ',')
                {
                    // There is a leading (e.g.) "Tue, ", strip it off
                    dateTimeStringBuilder.Remove(0, 4);
                    // There's supposed to be a space here but some implementations dont have one
                    Rss20FeedFormatter.RemoveExtraWhiteSpaceAtStart(dateTimeStringBuilder);
                }
                Rss20FeedFormatter.ReplaceMultipleWhiteSpaceWithSingleWhiteSpace(dateTimeStringBuilder);
                if (char.IsDigit(dateTimeStringBuilder[1]))
                {
                    // two-digit day, we are good
                }
                else
                {
                    dateTimeStringBuilder.Insert(0, '0');
                }
                if (dateTimeStringBuilder.Length < 19)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new XmlException(FeedUtils.AddLineInfo(reader,
                        SR.ErrorParsingDateTime)));
                }
                bool thereAreSeconds = (dateTimeStringBuilder[17] == ':');
                int timeZoneStartIndex;
                if (thereAreSeconds)
                {
                    timeZoneStartIndex = 21;
                }
                else
                {
                    timeZoneStartIndex = 18;
                }
                string timeZoneSuffix = dateTimeStringBuilder.ToString().Substring(timeZoneStartIndex);
                dateTimeStringBuilder.Remove(timeZoneStartIndex, dateTimeStringBuilder.Length - timeZoneStartIndex);
                bool isUtc;
                dateTimeStringBuilder.Append(NormalizeTimeZone(timeZoneSuffix, out isUtc));
                string wellFormattedString = dateTimeStringBuilder.ToString();

                DateTimeOffset theTime;
                string parseFormat;
                if (thereAreSeconds)
                {
                    parseFormat = "dd MMM yyyy HH:mm:ss zzz";
                }
                else
                {
                    parseFormat = "dd MMM yyyy HH:mm zzz";
                }
                if (DateTimeOffset.TryParseExact(wellFormattedString, parseFormat,
                    CultureInfo.InvariantCulture.DateTimeFormat,
                    (isUtc ? DateTimeStyles.AdjustToUniversal : DateTimeStyles.None), out theTime))
                {
                    return theTime;
                }
                throw new FormatException("There was an error with the format of the date");
                //throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                //    new XmlException(FeedUtils.AddLineInfo(reader,
                //    SR.ErrorParsingDateTime)));
            }
            catch(FormatException fe)
            {
                Console.WriteLine("There was an error in the format of the date");
            }

            return new DateTimeOffset();

        }

        static string NormalizeTimeZone(string rfc822TimeZone, out bool isUtc)
        {
            isUtc = false;
            // return a string in "-08:00" format
            if (rfc822TimeZone[0] == '+' || rfc822TimeZone[0] == '-')
            {
                // the time zone is supposed to be 4 digits but some feeds omit the initial 0
                StringBuilder result = new StringBuilder(rfc822TimeZone);
                if (result.Length == 4)
                {
                    // the timezone is +/-HMM. Convert to +/-HHMM
                    result.Insert(1, '0');
                }
                result.Insert(3, ':');
                return result.ToString();
            }
            switch (rfc822TimeZone)
            {
                case "UT":
                case "Z":
                    isUtc = true;
                    return "-00:00";
                case "GMT":
                    return "-00:00";
                case "A":
                    return "-01:00";
                case "B":
                    return "-02:00";
                case "C":
                    return "-03:00";
                case "D":
                case "EDT":
                    return "-04:00";
                case "E":
                case "EST":
                case "CDT":
                    return "-05:00";
                case "F":
                case "CST":
                case "MDT":
                    return "-06:00";
                case "G":
                case "MST":
                case "PDT":
                    return "-07:00";
                case "H":
                case "PST":
                    return "-08:00";
                case "I":
                    return "-09:00";
                case "K":
                    return "-10:00";
                case "L":
                    return "-11:00";
                case "M":
                    return "-12:00";
                case "N":
                    return "+01:00";
                case "O":
                    return "+02:00";
                case "P":
                    return "+03:00";
                case "Q":
                    return "+04:00";
                case "R":
                    return "+05:00";
                case "S":
                    return "+06:00";
                case "T":
                    return "+07:00";
                case "U":
                    return "+08:00";
                case "V":
                    return "+09:00";
                case "W":
                    return "+10:00";
                case "X":
                    return "+11:00";
                case "Y":
                    return "+12:00";
                default:
                    return "";
            }
        }
        //--------------------------------



    }


}
