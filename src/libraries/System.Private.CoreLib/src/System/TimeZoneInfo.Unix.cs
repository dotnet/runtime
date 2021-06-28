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
