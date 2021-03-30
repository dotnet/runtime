// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private static void TryPopulateTimeZoneDisplayNamesFromGlobalizationData(string timeZoneId, TimeSpan baseUtcOffset, ref string? standardDisplayName, ref string? daylightDisplayName, ref string? displayName)
        {
            // Do nothing. We'll use the fallback values already set.
        }

        private static string GetUtcStandardDisplayName()
        {
            // Just use the invariant display name.
            return InvariantUtcStandardDisplayName;
        }

        private static string? GetAlternativeId(string id)
        {
            // No alternative IDs in this target.
            return null;
        }
    }
}
