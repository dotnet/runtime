// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string TimeZoneFileName = "tzdata";

        private static string GetApexTimeDataRoot ()
        {
            var ret = Environment.GetEnvironmentVariable ("ANDROID_TZDATA_ROOT");
            if (!string.IsNullOrEmpty(ret)) {
                return ret;
            }

            return "/apex/com.android.tzdata";
        }

        private static string GetApexRuntimeRoot ()
        {
            var ret = Environment.GetEnvironmentVariable ("ANDROID_RUNTIME_ROOT");
            if (!string.IsNullOrEmpty (ret)) {
                return ret;
            }

            return "/apex/com.android.runtime";
        }

        internal static readonly string[] Paths = new string[]{
            GetApexTimeDataRoot () + "/etc/tz/", // Android 10+, TimeData module where the updates land
            GetApexRuntimeRoot () + "/etc/tz/",  // Android 10+, Fallback location if the above isn't found or corrupted
            Environment.GetEnvironmentVariable ("ANDROID_DATA") + "/misc/zoneinfo/",
        };

        private static string GetTimeZoneDirectory()
        {
            foreach (var filePath in Paths)
            {
                if (File.Exists(Path.Combine(filePath, "TimeZoneFileName")))
                {
                    return filePath;
                }
            }

            return Environment.GetEnvironmentVariable ("ANDROID_ROOT") + DefaultTimeZoneDirectory;
        }
    }
}