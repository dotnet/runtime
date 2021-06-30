// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string TimeZoneFileName = "zone.tab";
        private const string TimeZoneDirectoryEnvironmentVariable = "TZDIR";

        private static TimeZoneInfo GetLocalTimeZoneCore()
        {
            // Without Registry support, create the TimeZoneInfo from a TZ file
            return GetLocalTimeZoneFromTzFile();
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachineCore(string id, out TimeZoneInfo? value, out Exception? e)
        {
            value = null;
            e = null;

            string timeZoneDirectory = GetTimeZoneDirectory();
            string timeZoneFilePath = Path.Combine(timeZoneDirectory, id);
            byte[] rawData;
            try
            {
                rawData = File.ReadAllBytes(timeZoneFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                e = ex;
                return TimeZoneInfoResult.SecurityException;
            }
            catch (FileNotFoundException ex)
            {
                e = ex;
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }
            catch (DirectoryNotFoundException ex)
            {
                e = ex;
                return TimeZoneInfoResult.TimeZoneNotFoundException;
            }
            catch (IOException ex)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, timeZoneFilePath), ex);
                return TimeZoneInfoResult.InvalidTimeZoneException;
            }

            value = GetTimeZoneFromTzData(rawData, id);

            if (value == null)
            {
                e = new InvalidTimeZoneException(SR.Format(SR.InvalidTimeZone_InvalidFileData, id, timeZoneFilePath));
                return TimeZoneInfoResult.InvalidTimeZoneException;
            }

            return TimeZoneInfoResult.Success;
        }

        /// <summary>
        /// Returns a collection of TimeZone Id values from the time zone file in the timeZoneDirectory.
        /// </summary>
        /// <remarks>
        /// Lines that start with # are comments and are skipped.
        /// </remarks>
        private static List<string> GetTimeZoneIds()
        {
            List<string> timeZoneIds = new List<string>();

            try
            {
                using (StreamReader sr = new StreamReader(Path.Combine(GetTimeZoneDirectory(), TimeZoneFileName), Encoding.UTF8))
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

        /// <summary>
        /// Helper function used by 'GetLocalTimeZone()' - this function wraps the call
        /// for loading time zone data from computers without Registry support.
        ///
        /// The TryGetLocalTzFile() call returns a Byte[] containing the compiled tzfile.
        /// </summary>
        private static TimeZoneInfo GetLocalTimeZoneFromTzFile()
        {
            byte[]? rawData;
            string? id;
            if (TryGetLocalTzFile(out rawData, out id))
            {
                TimeZoneInfo? result = GetTimeZoneFromTzData(rawData, id);
                if (result != null)
                {
                    return result;
                }
            }

            // if we can't find a local time zone, return UTC
            return Utc;
        }

        private static string GetTimeZoneDirectory()
        {
            string? tzDirectory = Environment.GetEnvironmentVariable(TimeZoneDirectoryEnvironmentVariable);

            if (tzDirectory == null)
            {
                tzDirectory = DefaultTimeZoneDirectory;
            }
            else if (!tzDirectory.EndsWith(Path.DirectorySeparatorChar))
            {
                tzDirectory += PathInternal.DirectorySeparatorCharAsString;
            }

            return tzDirectory;
        }
    }
}
