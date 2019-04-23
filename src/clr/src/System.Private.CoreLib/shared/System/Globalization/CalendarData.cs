// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Diagnostics;

namespace System.Globalization
{
    // List of calendar data
    // Note the we cache overrides.
    // Note that localized names (resource names) aren't available from here.
    //
    //  NOTE: Calendars depend on the locale name that creates it.  Only a few
    //        properties are available without locales using CalendarData.GetCalendar(CalendarData)
    //
    // TODO-NULLABLE: this class requires refactoring for proper annotations
    //                currently from user of this class all fields are non-nullable.
    //                To avoid potential breaking changes lots of workaround have
    //                been used to suppress errors
    internal partial class CalendarData
    {
        // Max calendars
        internal const int MAX_CALENDARS = 23;

        // Identity
        internal string sNativeName = null!; // Calendar Name for the locale

        // Formats
        internal string[] saShortDates = null!; // Short Data format, default first
        internal string[] saYearMonths = null!; // Year/Month Data format, default first
        internal string[] saLongDates = null!; // Long Data format, default first
        internal string sMonthDay = null!; // Month/Day format

        // Calendar Parts Names
        internal string[] saEraNames = null!; // Names of Eras
        internal string[] saAbbrevEraNames = null!; // Abbreviated Era Names
        internal string[] saAbbrevEnglishEraNames = null!; // Abbreviated Era Names in English
        internal string[] saDayNames = null!; // Day Names, null to use locale data, starts on Sunday
        internal string[] saAbbrevDayNames = null!; // Abbrev Day Names, null to use locale data, starts on Sunday
        internal string[] saSuperShortDayNames = null!; // Super short Day of week names
        internal string[] saMonthNames = null!; // Month Names (13)
        internal string[] saAbbrevMonthNames = null!; // Abbrev Month Names (13)
        internal string[] saMonthGenitiveNames = null!; // Genitive Month Names (13)
        internal string[] saAbbrevMonthGenitiveNames = null!; // Genitive Abbrev Month Names (13)
        internal string[] saLeapYearMonthNames = null!; // Multiple strings for the month names in a leap year.

        // Integers at end to make marshaller happier
        internal int iTwoDigitYearMax = 2029; // Max 2 digit year (for Y2K bug data entry)
        internal int iCurrentEra = 0;  // current era # (usually 1)

        // Use overrides?
        internal bool bUseUserOverrides; // True if we want user overrides.

        // Static invariant for the invariant locale
        internal static readonly CalendarData Invariant = CreateInvariant();

        // Private constructor
        private CalendarData()
        {
        }

        // Invariant factory
        private static CalendarData CreateInvariant()
        {
            // Set our default/gregorian US calendar data
            // Calendar IDs are 1-based, arrays are 0 based.
            CalendarData invariant = new CalendarData();

            // Set default data for calendar
            // Note that we don't load resources since this IS NOT supposed to change (by definition)
            invariant.sNativeName = "Gregorian Calendar";  // Calendar Name

            // Year
            invariant.iTwoDigitYearMax = 2029; // Max 2 digit year (for Y2K bug data entry)
            invariant.iCurrentEra = 1; // Current era #

            // Formats
            invariant.saShortDates = new string[] { "MM/dd/yyyy", "yyyy-MM-dd" };          // short date format
            invariant.saLongDates = new string[] { "dddd, dd MMMM yyyy" };                 // long date format
            invariant.saYearMonths = new string[] { "yyyy MMMM" };                         // year month format
            invariant.sMonthDay = "MMMM dd";                                            // Month day pattern

            // Calendar Parts Names
            invariant.saEraNames = new string[] { "A.D." };     // Era names
            invariant.saAbbrevEraNames = new string[] { "AD" };      // Abbreviated Era names
            invariant.saAbbrevEnglishEraNames = new string[] { "AD" };     // Abbreviated era names in English
            invariant.saDayNames = new string[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };// day names
            invariant.saAbbrevDayNames = new string[] { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };     // abbreviated day names
            invariant.saSuperShortDayNames = new string[] { "Su", "Mo", "Tu", "We", "Th", "Fr", "Sa" };      // The super short day names
            invariant.saMonthNames = new string[] { "January", "February", "March", "April", "May", "June",
                                                            "July", "August", "September", "October", "November", "December", string.Empty}; // month names
            invariant.saAbbrevMonthNames = new string[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun",
                                                            "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", string.Empty}; // abbreviated month names
            invariant.saMonthGenitiveNames = invariant.saMonthNames;              // Genitive month names (same as month names for invariant)
            invariant.saAbbrevMonthGenitiveNames = invariant.saAbbrevMonthNames;    // Abbreviated genitive month names (same as abbrev month names for invariant)
            invariant.saLeapYearMonthNames = invariant.saMonthNames;              // leap year month names are unused in Gregorian English (invariant)

            invariant.bUseUserOverrides = false;

            return invariant;
        }

        //
        // Get a bunch of data for a calendar
        //
        internal CalendarData(string localeName, CalendarId calendarId, bool bUseUserOverrides)
        {
            this.bUseUserOverrides = bUseUserOverrides;

            Debug.Assert(!GlobalizationMode.Invariant);

            if (!LoadCalendarDataFromSystem(localeName, calendarId))
            {
                // LoadCalendarDataFromSystem sometimes can fail on Linux if the installed ICU package is missing some resources.
                // The ICU package can miss some resources in some cases like if someone compile and build the ICU package manually or ICU has a regression.

                // Something failed, try invariant for missing parts
                // This is really not good, but we don't want the callers to crash.
                if (this.sNativeName == null) this.sNativeName = string.Empty;           // Calendar Name for the locale.

                // Formats
                if (this.saShortDates == null) this.saShortDates = Invariant.saShortDates; // Short Data format, default first
                if (this.saYearMonths == null) this.saYearMonths = Invariant.saYearMonths; // Year/Month Data format, default first
                if (this.saLongDates == null) this.saLongDates = Invariant.saLongDates;  // Long Data format, default first
                if (this.sMonthDay == null) this.sMonthDay = Invariant.sMonthDay;    // Month/Day format

                // Calendar Parts Names
                if (this.saEraNames == null) this.saEraNames = Invariant.saEraNames;              // Names of Eras
                if (this.saAbbrevEraNames == null) this.saAbbrevEraNames = Invariant.saAbbrevEraNames;        // Abbreviated Era Names
                if (this.saAbbrevEnglishEraNames == null) this.saAbbrevEnglishEraNames = Invariant.saAbbrevEnglishEraNames; // Abbreviated Era Names in English
                if (this.saDayNames == null) this.saDayNames = Invariant.saDayNames;              // Day Names, null to use locale data, starts on Sunday
                if (this.saAbbrevDayNames == null) this.saAbbrevDayNames = Invariant.saAbbrevDayNames;        // Abbrev Day Names, null to use locale data, starts on Sunday
                if (this.saSuperShortDayNames == null) this.saSuperShortDayNames = Invariant.saSuperShortDayNames;    // Super short Day of week names
                if (this.saMonthNames == null) this.saMonthNames = Invariant.saMonthNames;            // Month Names (13)
                if (this.saAbbrevMonthNames == null) this.saAbbrevMonthNames = Invariant.saAbbrevMonthNames;      // Abbrev Month Names (13)
                // Genitive and Leap names can follow the fallback below
            }

            if (calendarId == CalendarId.TAIWAN)
            {
                if (SystemSupportsTaiwaneseCalendar())
                {
                    // We got the month/day names from the OS (same as gregorian), but the native name is wrong
                    this.sNativeName = "\x4e2d\x83ef\x6c11\x570b\x66c6";
                }
                else
                {
                    this.sNativeName = string.Empty;
                }
            }

            // Check for null genitive names (in case unmanaged side skips it for non-gregorian calendars, etc)
            if (this.saMonthGenitiveNames == null || this.saMonthGenitiveNames.Length == 0 || string.IsNullOrEmpty(this.saMonthGenitiveNames[0]))
                this.saMonthGenitiveNames = this.saMonthNames;              // Genitive month names (same as month names for invariant)
            if (this.saAbbrevMonthGenitiveNames == null || this.saAbbrevMonthGenitiveNames.Length == 0 || string.IsNullOrEmpty(this.saAbbrevMonthGenitiveNames[0]))
                this.saAbbrevMonthGenitiveNames = this.saAbbrevMonthNames;    // Abbreviated genitive month names (same as abbrev month names for invariant)
            if (this.saLeapYearMonthNames == null || this.saLeapYearMonthNames.Length == 0 || string.IsNullOrEmpty(this.saLeapYearMonthNames[0]))
                this.saLeapYearMonthNames = this.saMonthNames;

            InitializeEraNames(localeName, calendarId);

            InitializeAbbreviatedEraNames(localeName, calendarId);

            // Abbreviated English Era Names are only used for the Japanese calendar.
            if (calendarId == CalendarId.JAPAN)
            {
                this.saAbbrevEnglishEraNames = JapaneseCalendar.EnglishEraNames();
            }
            else
            {
                // For all others just use the an empty string (doesn't matter we'll never ask for it for other calendars)
                this.saAbbrevEnglishEraNames = new string[] { "" };
            }

            // Japanese is the only thing with > 1 era.  Its current era # is how many ever
            // eras are in the array.  (And the others all have 1 string in the array)
            this.iCurrentEra = this.saEraNames.Length;
        }

        private void InitializeEraNames(string localeName, CalendarId calendarId)
        {
            // Note that the saEraNames only include "A.D."  We don't have localized names for other calendars available from windows
            switch (calendarId)
            {
                // For Localized Gregorian we really expect the data from the OS.
                case CalendarId.GREGORIAN:
                    // Fallback for CoreCLR < Win7 or culture.dll missing
                    if (this.saEraNames == null || this.saEraNames.Length == 0 || string.IsNullOrEmpty(this.saEraNames[0]))
                    {
                        this.saEraNames = new string[] { "A.D." };
                    }
                    break;

                // The rest of the calendars have constant data, so we'll just use that
                case CalendarId.GREGORIAN_US:
                case CalendarId.JULIAN:
                    this.saEraNames = new string[] { "A.D." };
                    break;
                case CalendarId.HEBREW:
                    this.saEraNames = new string[] { "C.E." };
                    break;
                case CalendarId.HIJRI:
                case CalendarId.UMALQURA:
                    if (localeName == "dv-MV")
                    {
                        // Special case for Divehi
                        this.saEraNames = new string[] { "\x0780\x07a8\x0796\x07b0\x0783\x07a9" };
                    }
                    else
                    {
                        this.saEraNames = new string[] { "\x0628\x0639\x062F \x0627\x0644\x0647\x062C\x0631\x0629" };
                    }
                    break;
                case CalendarId.GREGORIAN_ARABIC:
                case CalendarId.GREGORIAN_XLIT_ENGLISH:
                case CalendarId.GREGORIAN_XLIT_FRENCH:
                    // These are all the same:
                    this.saEraNames = new string[] { "\x0645" };
                    break;

                case CalendarId.GREGORIAN_ME_FRENCH:
                    this.saEraNames = new string[] { "ap. J.-C." };
                    break;

                case CalendarId.TAIWAN:
                    if (SystemSupportsTaiwaneseCalendar())
                    {
                        this.saEraNames = new string[] { "\x4e2d\x83ef\x6c11\x570b" };
                    }
                    else
                    {
                        this.saEraNames = new string[] { string.Empty };
                    }
                    break;

                case CalendarId.KOREA:
                    this.saEraNames = new string[] { "\xb2e8\xae30" };
                    break;

                case CalendarId.THAI:
                    this.saEraNames = new string[] { "\x0e1e\x002e\x0e28\x002e" };
                    break;

                case CalendarId.JAPAN:
                case CalendarId.JAPANESELUNISOLAR:
                    this.saEraNames = JapaneseCalendar.EraNames();
                    break;

                case CalendarId.PERSIAN:
                    if (this.saEraNames == null || this.saEraNames.Length == 0 || string.IsNullOrEmpty(this.saEraNames[0]))
                    {
                        this.saEraNames = new string[] { "\x0647\x002e\x0634" };
                    }
                    break;

                default:
                    // Most calendars are just "A.D."
                    this.saEraNames = Invariant.saEraNames;
                    break;
            }
        }

        private void InitializeAbbreviatedEraNames(string localeName, CalendarId calendarId)
        {
            // Note that the saAbbrevEraNames only include "AD"  We don't have localized names for other calendars available from windows
            switch (calendarId)
            {
                // For Localized Gregorian we really expect the data from the OS.
                case CalendarId.GREGORIAN:
                    // Fallback for CoreCLR < Win7 or culture.dll missing
                    if (this.saAbbrevEraNames == null || this.saAbbrevEraNames.Length == 0 || string.IsNullOrEmpty(this.saAbbrevEraNames[0]))
                    {
                        this.saAbbrevEraNames = new string[] { "AD" };
                    }
                    break;

                // The rest of the calendars have constant data, so we'll just use that
                case CalendarId.GREGORIAN_US:
                case CalendarId.JULIAN:
                    this.saAbbrevEraNames = new string[] { "AD" };
                    break;
                case CalendarId.JAPAN:
                case CalendarId.JAPANESELUNISOLAR:
                    this.saAbbrevEraNames = JapaneseCalendar.AbbrevEraNames();
                    break;
                case CalendarId.HIJRI:
                case CalendarId.UMALQURA:
                    if (localeName == "dv-MV")
                    {
                        // Special case for Divehi
                        this.saAbbrevEraNames = new string[] { "\x0780\x002e" };
                    }
                    else
                    {
                        this.saAbbrevEraNames = new string[] { "\x0647\x0640" };
                    }
                    break;
                case CalendarId.TAIWAN:
                    // Get era name and abbreviate it
                    this.saAbbrevEraNames = new string[1];
                    if (this.saEraNames[0].Length == 4)
                    {
                        this.saAbbrevEraNames[0] = this.saEraNames[0].Substring(2, 2);
                    }
                    else
                    {
                        this.saAbbrevEraNames[0] = this.saEraNames[0];
                    }
                    break;

                case CalendarId.PERSIAN:
                    if (this.saAbbrevEraNames == null || this.saAbbrevEraNames.Length == 0 || string.IsNullOrEmpty(this.saAbbrevEraNames[0]))
                    {
                        this.saAbbrevEraNames = this.saEraNames;
                    }
                    break;

                default:
                    // Most calendars just use the full name
                    this.saAbbrevEraNames = this.saEraNames;
                    break;
            }
        }

        internal static CalendarData GetCalendarData(CalendarId calendarId)
        {
            //
            // Get a calendar.
            // Unfortunately we depend on the locale in the OS, so we need a locale
            // no matter what.  So just get the appropriate calendar from the
            // appropriate locale here
            //

            // Get a culture name
            // TODO: Note that this doesn't handle the new calendars (lunisolar, etc)
            string culture = CalendarIdToCultureName(calendarId);

            // Return our calendar
            return CultureInfo.GetCultureInfo(culture)._cultureData.GetCalendar(calendarId);
        }

        private static string CalendarIdToCultureName(CalendarId calendarId)
        {
            switch (calendarId)
            {
                case CalendarId.GREGORIAN_US:
                    return "fa-IR";             // "fa-IR" Iran

                case CalendarId.JAPAN:
                    return "ja-JP";             // "ja-JP" Japan

                case CalendarId.TAIWAN:
                    return "zh-TW";             // zh-TW Taiwan

                case CalendarId.KOREA:
                    return "ko-KR";             // "ko-KR" Korea

                case CalendarId.HIJRI:
                case CalendarId.GREGORIAN_ARABIC:
                case CalendarId.UMALQURA:
                    return "ar-SA";             // "ar-SA" Saudi Arabia

                case CalendarId.THAI:
                    return "th-TH";             // "th-TH" Thailand

                case CalendarId.HEBREW:
                    return "he-IL";             // "he-IL" Israel

                case CalendarId.GREGORIAN_ME_FRENCH:
                    return "ar-DZ";             // "ar-DZ" Algeria

                case CalendarId.GREGORIAN_XLIT_ENGLISH:
                case CalendarId.GREGORIAN_XLIT_FRENCH:
                    return "ar-IQ";             // "ar-IQ"; Iraq

                default:
                    // Default to gregorian en-US
                    break;
            }

            return "en-US";
        }
    }
}
