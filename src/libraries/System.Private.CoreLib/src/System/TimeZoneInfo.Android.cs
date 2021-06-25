// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        // Mitchell - Why isn't this just instantiated in TimeZoneInfo.cs?
        // private static readonly TimeZoneInfo s_utcTimeZone = CreateUtcTimeZone();

        private static List<string> GetTimeZoneIds(string timeZoneDirectory)
        {
            return new List<string>();
        }
    }
}