// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string ZoneTabFileName = "zone.tab";

        /// <summary>
        /// Returns a collection of TimeZone Id values from the zone.tab file in the timeZoneDirectory.
        /// </summary>
        /// <remarks>
        /// Lines that start with # are comments and are skipped.
        /// </remarks>
        private static List<string> GetTimeZoneIds(string timeZoneDirectory)
        {
            List<string> timeZoneIds = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(timeZoneDirectory, ZoneTabFileName), Encoding.UTF8))
                {
                    string? zoneTabFileLine;
                    while ((zoneTabFileLine = sr.ReadLine()) != null)
                    {
                        if (!string.IsNullOrEmpty(zoneTabFileLine) && zoneTabFileLine[0] != '#')
                        {
                            // the format of the line is "country-code \t coordinates \t TimeZone Id \t comments"

                            int firstTabIndex = zoneTabFileLine.IndexOf('\t');
                            if (firstTabIndex != -1)
                            {
                                int secondTabIndex = zoneTabFileLine.IndexOf('\t', firstTabIndex + 1);
                                if (secondTabIndex != -1)
                                {
                                    string timeZoneId;
                                    int startIndex = secondTabIndex + 1;
                                    int thirdTabIndex = zoneTabFileLine.IndexOf('\t', startIndex);
                                    if (thirdTabIndex != -1)
                                    {
                                        int length = thirdTabIndex - startIndex;
                                        timeZoneId = zoneTabFileLine.Substring(startIndex, length);
                                    }
                                    else
                                    {
                                        timeZoneId = zoneTabFileLine.Substring(startIndex);
                                    }

                                    if (!string.IsNullOrEmpty(timeZoneId))
                                    {
                                        timeZoneIds.Add(timeZoneId);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            return timeZoneIds;
        }
    }
}
