// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        private const string InvariantUtcStandardDisplayName = "Coordinated Universal Time";
        private const string FallbackCultureName = "en-US";
        private const string GmtId = "GMT";

        // Some time zones may give better display names using their location names rather than their generic name.
        // We can update this list as need arises.
        private static readonly string[] s_ZonesThatUseLocationName = new[] {
            "Europe/Minsk",       // Prefer "Belarus Time" over "Moscow Standard Time (Minsk)"
            "Europe/Moscow",      // Prefer "Moscow Time" over "Moscow Standard Time"
            "Europe/Simferopol",  // Prefer "Simferopol Time" over "Moscow Standard Time (Simferopol)"
            "Pacific/Apia",       // Prefer "Samoa Time" over "Apia Time"
            "Pacific/Pitcairn"    // Prefer "Pitcairn Islands Time" over "Pitcairn Time"
        };

        // Main function that is called during construction to populate the three display names
        private static void TryPopulateTimeZoneDisplayNamesFromGlobalizationData(string timeZoneId, TimeSpan baseUtcOffset, ref string? standardDisplayName, ref string? daylightDisplayName, ref string? displayName)
        {
            // Determine the culture to use
            CultureInfo uiCulture = CultureInfo.CurrentUICulture;
            if (uiCulture.Name.Length == 0)
                uiCulture = CultureInfo.GetCultureInfo(FallbackCultureName); // ICU doesn't work nicely with InvariantCulture

            // Attempt to populate the fields backing the StandardName, DaylightName, and DisplayName from globalization data.
            GetDisplayName(timeZoneId, Interop.Globalization.TimeZoneDisplayNameType.Standard, uiCulture.Name, ref standardDisplayName);
            GetDisplayName(timeZoneId, Interop.Globalization.TimeZoneDisplayNameType.DaylightSavings, uiCulture.Name, ref daylightDisplayName);
            GetFullValueForDisplayNameField(timeZoneId, baseUtcOffset, uiCulture, ref displayName);
        }

        // Helper function to get the standard display name for the UTC static time zone instance
        private static string GetUtcStandardDisplayName()
        {
            // Don't bother looking up the name for invariant or English cultures
            CultureInfo uiCulture = CultureInfo.CurrentUICulture;
            if (GlobalizationMode.Invariant || uiCulture.Name.Length == 0 || uiCulture.TwoLetterISOLanguageName == "en")
                return InvariantUtcStandardDisplayName;

            // Try to get a localized version of "Coordinated Universal Time" from the globalization data
            string? standardDisplayName = null;
            GetDisplayName(UtcId, Interop.Globalization.TimeZoneDisplayNameType.Standard, uiCulture.Name, ref standardDisplayName);

            // Final safety check.  Don't allow null or abbreviations
            if (standardDisplayName == null || standardDisplayName == "GMT" || standardDisplayName == "UTC")
                standardDisplayName = InvariantUtcStandardDisplayName;

            return standardDisplayName;
        }

        // Helper function to get the full display name for the UTC static time zone instance
        private static string GetUtcFullDisplayName(string timeZoneId, string standardDisplayName)
        {
            return $"(UTC) {standardDisplayName}";
        }

        // Helper function that retrieves various forms of time zone display names from ICU
        private static unsafe void GetDisplayName(string timeZoneId, Interop.Globalization.TimeZoneDisplayNameType nameType, string uiCulture, ref string? displayName)
        {
            if (GlobalizationMode.Invariant)
            {
                return;
            }

            string? timeZoneDisplayName;
            bool result = Interop.CallStringMethod(
                (buffer, locale, id, type) =>
                {
                    fixed (char* bufferPtr = buffer)
                    {
                        return Interop.Globalization.GetTimeZoneDisplayName(locale, id, type, bufferPtr, buffer.Length);
                    }
                },
                uiCulture,
                timeZoneId,
                nameType,
                out timeZoneDisplayName);

            if (!result && uiCulture != FallbackCultureName)
            {
                // Try to fallback using FallbackCultureName just in case we can make it work.
                result = Interop.CallStringMethod(
                    (buffer, locale, id, type) =>
                    {
                        fixed (char* bufferPtr = buffer)
                        {
                            return Interop.Globalization.GetTimeZoneDisplayName(locale, id, type, bufferPtr, buffer.Length);
                        }
                    },
                    FallbackCultureName,
                    timeZoneId,
                    nameType,
                    out timeZoneDisplayName);
            }

            // If there is an unknown error, don't set the displayName field.
            // It will be set to the abbreviation that was read out of the tzfile.
            if (result && !string.IsNullOrEmpty(timeZoneDisplayName))
            {
                displayName = timeZoneDisplayName;
            }
        }

        // Helper function that builds the value backing the DisplayName field from globalization data.
        private static void GetFullValueForDisplayNameField(string timeZoneId, TimeSpan baseUtcOffset, CultureInfo uiCulture, ref string? displayName)
        {
            // There are a few diffent ways we might show the display name depending on the data.
            // The algorithm used below should avoid duplicating the same words while still achieving the
            // goal of providing a unique, discoverable, and intuitive name.

            // Try to get the generic name for this time zone.
            string? genericName = null;
            GetDisplayName(timeZoneId, Interop.Globalization.TimeZoneDisplayNameType.Generic, uiCulture.Name, ref genericName);
            if (genericName == null)
            {
                // We'll use the fallback display name value already set.
                return;
            }

            // Get the base offset to prefix in front of the time zone.
            // Only UTC and its aliases have "(UTC)", handled earlier.  All other zones include an offset, even if it's zero.
            string baseOffsetText = $"(UTC{(baseUtcOffset >= TimeSpan.Zero ? '+' : '-')}{baseUtcOffset:hh\\:mm})";

            // Get the generic location name.
            string? genericLocationName = null;
            GetDisplayName(timeZoneId, Interop.Globalization.TimeZoneDisplayNameType.GenericLocation, uiCulture.Name, ref genericLocationName);

            // Some edge cases only apply when the offset is +00:00.
            if (baseUtcOffset == TimeSpan.Zero)
            {
                // GMT and its aliases will just use the equivalent of "Greenwich Mean Time".
                string? gmtLocationName = null;
                GetDisplayName(GmtId, Interop.Globalization.TimeZoneDisplayNameType.GenericLocation, uiCulture.Name, ref gmtLocationName);
                if (genericLocationName == gmtLocationName)
                {
                    displayName = $"{baseOffsetText} {genericName}";
                    return;
                }

                // Other zones with a zero offset and the equivalent of "Greenwich Mean Time" should only use the location name.
                // For example, prefer "Iceland Time" over "Greenwich Mean Time (Reykjavik)".
                string? gmtGenericName = null;
                GetDisplayName(GmtId, Interop.Globalization.TimeZoneDisplayNameType.Generic, uiCulture.Name, ref gmtGenericName);
                if (genericName == gmtGenericName)
                {
                    displayName = $"{baseOffsetText} {genericLocationName}";
                    return;
                }
            }

            if (genericLocationName == genericName)
            {
                // When the location name is the same as the generic name,
                // then it is generally good enough to show by itself.

                // *** Example (en-US) ***
                // id                   = "America/Havana"
                // baseOffsetText       = "(UTC-05:00)"
                // standardName         = "Cuba Standard Time"
                // genericName          = "Cuba Time"
                // genericLocationName  = "Cuba Time"
                // exemplarCityName     = "Havana"
                // displayName          = "(UTC-05:00) Cuba Time"

                displayName = $"{baseOffsetText} {genericLocationName}";
                return;
            }

            // Prefer location names in some special cases.
            if (StringArrayContains(timeZoneId, s_ZonesThatUseLocationName, StringComparison.OrdinalIgnoreCase))
            {
                displayName = $"{baseOffsetText} {genericLocationName}";
                return;
            }

            // See if we should include the exemplar city name.
            string exemplarCityName = GetExemplarCityName(timeZoneId, uiCulture.Name);
            if (uiCulture.CompareInfo.IndexOf(genericName, exemplarCityName, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0 && genericLocationName != null)
            {
                // When an exemplar city is already part of the generic name,
                // there's no need to repeat it again so just use the generic name.

                // *** Example (fr-FR) ***
                // id                   = "Australia/Lord_Howe"
                // baseOffsetText       = "(UTC+10:30)"
                // standardName         = "heure normale de Lord Howe"
                // genericName          = "heure de Lord Howe"
                // genericLocationName  = "heure : Lord Howe"
                // exemplarCityName     = "Lord Howe"
                // displayName          = "(UTC+10:30) heure de Lord Howe"

                displayName = $"{baseOffsetText} {genericName}";
            }
            else
            {
                // Finally, use the generic name and the exemplar city together.
                // This provides an intuitive name and still disambiguates.

                // *** Example (en-US) ***
                // id                   = "Europe/Rome"
                // baseOffsetText       = "(UTC+01:00)"
                // standardName         = "Central European Standard Time"
                // genericName          = "Central European Time"
                // genericLocationName  = "Italy Time"
                // exemplarCityName     = "Rome"
                // displayName          = "(UTC+01:00) Central European Time (Rome)"

                displayName = $"{baseOffsetText} {genericName} ({exemplarCityName})";
            }
        }

        // Helper function that gets an exmplar city name either from ICU or from the IANA time zone ID itself
        private static string GetExemplarCityName(string timeZoneId, string uiCultureName)
        {
            // First try to get the name through the localization data.
            string? exemplarCityName = null;
            GetDisplayName(timeZoneId, Interop.Globalization.TimeZoneDisplayNameType.ExemplarCity, uiCultureName, ref exemplarCityName);
            if (!string.IsNullOrEmpty(exemplarCityName))
                return exemplarCityName;

            // Support for getting exemplar city names was added in ICU 51.
            // We may have an older version.  For example, in Helix we test on RHEL 7.5 which uses ICU 50.1.2.
            // We'll fallback to using an English name generated from the time zone ID.
            int i = timeZoneId.LastIndexOf('/');
            return timeZoneId.Substring(i + 1).Replace('_', ' ');
        }

        // Helper function that returns an alternative ID using ICU data. Used primarily for converting from Windows IDs.
        private static unsafe string? GetAlternativeId(string id, out bool idIsIana)
        {
            idIsIana = false;
            return TryConvertWindowsIdToIanaId(id, null, out string? ianaId) ? ianaId : null;
        }
    }
}
