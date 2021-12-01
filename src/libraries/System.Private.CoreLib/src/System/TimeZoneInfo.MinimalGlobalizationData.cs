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
            // For this target, be consistent with other time zone display names that use an abbreviation.
            return "UTC";
        }

        private static string GetUtcFullDisplayName(string timeZoneId, string standardDisplayName)
        {
            // For this target, be consistent with other time zone display names that use the ID.
            return $"(UTC) {timeZoneId}";
        }

        private static string? GetAlternativeId(string id, out bool idIsIana)
        {
            // No alternative IDs in this target.
            idIsIana = false;
            return null;
        }

        private static unsafe bool TryConvertIanaIdToWindowsId(string ianaId, bool allocate, out string? windowsId)
        {
            windowsId = null;
            return false;
        }

        private static unsafe bool TryConvertWindowsIdToIanaId(string windowsId, string? region, bool allocate,  out string? ianaId)
        {
            ianaId = null;
            return false;
        }
    }
}
