// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Threading;

namespace System.Globalization
{
    //
    // Flags used to indicate different styles of month names.
    // This is an internal flag used by internalGetMonthName().
    // Use flag here in case that we need to provide a combination of these styles
    // (such as month name of a leap year in genitive form.  Not likely for now,
    // but would like to keep the option open).
    //

    [Flags]
    internal enum MonthNameStyles
    {
        Regular = 0x00000000,
        Genitive = 0x00000001,
        LeapYear = 0x00000002,
    }

    //
    // Flags used to indicate special rule used in parsing/formatting
    // for a specific DateTimeFormatInfo instance.
    // This is an internal flag.
    //
    // This flag is different from MonthNameStyles because this flag
    // can be expanded to accomodate parsing behaviors like CJK month names
    // or alternative month names, etc.

    [Flags]
    internal enum DateTimeFormatFlags
    {
        None = 0x00000000,
        UseGenitiveMonth = 0x00000001,
        UseLeapYearMonth = 0x00000002,
        UseSpacesInMonthNames = 0x00000004, // Has spaces or non-breaking space in the month names.
        UseHebrewRule = 0x00000008,   // Format/Parse using the Hebrew calendar rule.
        UseSpacesInDayNames = 0x00000010,   // Has spaces or non-breaking space in the day names.
        UseDigitPrefixInTokens = 0x00000020,   // Has token starting with numbers.

        NotInitialized = -1,
    }


    [Serializable]
    [System.Runtime.InteropServices.ComVisible(true)]
    public sealed partial class DateTimeFormatInfo : IFormatProvider, ICloneable
    {
        // cache for the invariant culture.
        // invariantInfo is constant irrespective of your current culture.
        private static volatile DateTimeFormatInfo invariantInfo;

        // an index which points to a record in Culture Data Table.
        [NonSerialized]
        private CultureData m_cultureData;

        // The culture name used to create this DTFI.

        [OptionalField(VersionAdded = 2)]
        internal String m_name = null;

        // The language name of the culture used to create this DTFI.
        [NonSerialized]
        private String m_langName = null;

        // CompareInfo usually used by the parser.
        [NonSerialized]
        private CompareInfo m_compareInfo = null;

        // Culture matches current DTFI. mainly used for string comparisons during parsing.
        [NonSerialized]
        private CultureInfo m_cultureInfo = null;

        //
        // Caches for various properties.
        //

        internal String amDesignator = null;
        internal String pmDesignator = null;

        internal String dateSeparator = null;            // derived from short date (whidbey expects, arrowhead doesn't)

        internal String generalShortTimePattern = null;     // short date + short time (whidbey expects, arrowhead doesn't)

        internal String generalLongTimePattern = null;     // short date + long time (whidbey expects, arrowhead doesn't)

        internal String timeSeparator = null;            // derived from long time (whidbey expects, arrowhead doesn't)
        internal String monthDayPattern = null;
        // added in .NET Framework Release {2.0SP1/3.0SP1/3.5RTM}
        internal String dateTimeOffsetPattern = null;

        //
        // The following are constant values.
        //
        internal const String rfc1123Pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        // The sortable pattern is based on ISO 8601.
        internal const String sortableDateTimePattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss";
        internal const String universalSortableDateTimePattern = "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";

        //
        // The following are affected by calendar settings.
        //
        internal Calendar calendar = null;

        internal int firstDayOfWeek = -1;
        internal int calendarWeekRule = -1;


        internal String fullDateTimePattern = null;        // long date + long time (whidbey expects, arrowhead doesn't)

        internal String[] abbreviatedDayNames = null;


        internal String[] m_superShortDayNames = null;

        internal String[] dayNames = null;
        internal String[] abbreviatedMonthNames = null;
        internal String[] monthNames = null;
        // Cache the genitive month names that we retrieve from the data table.

        internal String[] genitiveMonthNames = null;

        // Cache the abbreviated genitive month names that we retrieve from the data table.

        internal String[] m_genitiveAbbreviatedMonthNames = null;

        // Cache the month names of a leap year that we retrieve from the data table.

        internal String[] leapYearMonthNames = null;

        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]

        // The "default" Date/time patterns 
        internal String longDatePattern = null;
        internal String shortDatePattern = null;
        internal String yearMonthPattern = null;
        internal String longTimePattern = null;
        internal String shortTimePattern = null;

        [OptionalField(VersionAdded = 3)]
        private String[] allYearMonthPatterns = null;
        internal String[] allShortDatePatterns = null;
        internal String[] allLongDatePatterns = null;
        internal String[] allShortTimePatterns = null;
        internal String[] allLongTimePatterns = null;

        // Cache the era names for this DateTimeFormatInfo instance.
        internal String[] m_eraNames = null;
        internal String[] m_abbrevEraNames = null;
        internal String[] m_abbrevEnglishEraNames = null;

        internal CalendarId[] optionalCalendars = null;

        private const int DEFAULT_ALL_DATETIMES_SIZE = 132;

        // CultureInfo updates this
        internal bool m_isReadOnly = false;

        // This flag gives hints about if formatting/parsing should perform special code path for things like
        // genitive form or leap year month names.

        internal DateTimeFormatFlags formatFlags = DateTimeFormatFlags.NotInitialized;

        // TODO: NLS Arrowhead - Yipe, this is used for scary internal hard coded stuff, language name might be better
        private String CultureName
        {
            get
            {
                if (m_name == null)
                {
                    m_name = this.m_cultureData.CultureName;
                }
                return (m_name);
            }
        }

        private CultureInfo Culture
        {
            get
            {
                if (m_cultureInfo == null)
                {
                    m_cultureInfo = CultureInfo.GetCultureInfo(this.CultureName);
                }
                return m_cultureInfo;
            }
        }

        // TODO: NLS Arrowhead - This ignores other cultures that might want to do something similar
        private String LanguageName
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (m_langName == null)
                {
                    m_langName = this.m_cultureData.SISO639LANGNAME;
                }
                return (m_langName);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Create an array of string which contains the abbreviated day names.
        //
        ////////////////////////////////////////////////////////////////////////////

        private String[] internalGetAbbreviatedDayOfWeekNames()
        {
            if (this.abbreviatedDayNames == null)
            {
                // Get the abbreviated day names for our current calendar
                this.abbreviatedDayNames = this.m_cultureData.AbbreviatedDayNames(Calendar.ID);
                Contract.Assert(this.abbreviatedDayNames.Length == 7, "[DateTimeFormatInfo.GetAbbreviatedDayOfWeekNames] Expected 7 day names in a week");
            }
            return (this.abbreviatedDayNames);
        }


        ////////////////////////////////////////////////////////////////////////
        //
        // Action: Returns the string array of the one-letter day of week names.
        // Returns:
        //  an array of one-letter day of week names
        // Arguments:
        //  None
        // Exceptions:
        //  None
        //
        ////////////////////////////////////////////////////////////////////////

        private String[] internalGetSuperShortDayNames()
        {
            if (this.m_superShortDayNames == null)
            {
                // Get the super short day names for our current calendar
                this.m_superShortDayNames = this.m_cultureData.SuperShortDayNames(Calendar.ID);
                Contract.Assert(this.m_superShortDayNames.Length == 7, "[DateTimeFormatInfo.internalGetSuperShortDayNames] Expected 7 day names in a week");
            }
            return (this.m_superShortDayNames);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Create an array of string which contains the day names.
        //
        ////////////////////////////////////////////////////////////////////////////

        private String[] internalGetDayOfWeekNames()
        {
            if (this.dayNames == null)
            {
                // Get the day names for our current calendar
                this.dayNames = this.m_cultureData.DayNames(Calendar.ID);
                Contract.Assert(this.dayNames.Length == 7, "[DateTimeFormatInfo.GetDayOfWeekNames] Expected 7 day names in a week");
            }
            return (this.dayNames);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Create an array of string which contains the abbreviated month names.
        //
        ////////////////////////////////////////////////////////////////////////////

        private String[] internalGetAbbreviatedMonthNames()
        {
            if (this.abbreviatedMonthNames == null)
            {
                // Get the month names for our current calendar
                this.abbreviatedMonthNames = this.m_cultureData.AbbreviatedMonthNames(Calendar.ID);
                Contract.Assert(this.abbreviatedMonthNames.Length == 12 || this.abbreviatedMonthNames.Length == 13,
                    "[DateTimeFormatInfo.GetAbbreviatedMonthNames] Expected 12 or 13 month names in a year");
            }
            return (this.abbreviatedMonthNames);
        }


        ////////////////////////////////////////////////////////////////////////////
        //
        // Create an array of string which contains the month names.
        //
        ////////////////////////////////////////////////////////////////////////////

        private String[] internalGetMonthNames()
        {
            if (this.monthNames == null)
            {
                // Get the month names for our current calendar
                this.monthNames = this.m_cultureData.MonthNames(Calendar.ID);
                Contract.Assert(this.monthNames.Length == 12 || this.monthNames.Length == 13,
                    "[DateTimeFormatInfo.GetMonthNames] Expected 12 or 13 month names in a year");
            }

            return (this.monthNames);
        }


        //
        // Invariant DateTimeFormatInfo doesn't have user-overriden values
        // Default calendar is gregorian
        public DateTimeFormatInfo()
            : this(CultureInfo.InvariantCulture.m_cultureData,
                    GregorianCalendar.GetDefaultInstance())
        {
        }

        internal DateTimeFormatInfo(CultureData cultureData, Calendar cal)
        {
            Contract.Requires(cultureData != null);
            Contract.Requires(cal != null);

            // Remember our culture
            this.m_cultureData = cultureData;

            this.Calendar = cal;
        }

        [System.Security.SecuritySafeCritical]
        private void InitializeOverridableProperties(CultureData cultureData, CalendarId calendarId)
        {
            Contract.Requires(cultureData != null);
            Contract.Assert(calendarId != CalendarId.UNINITIALIZED_VALUE, "[DateTimeFormatInfo.Populate] Expected initalized calendarId");

            if (this.firstDayOfWeek == -1) { this.firstDayOfWeek = cultureData.IFIRSTDAYOFWEEK; }
            if (this.calendarWeekRule == -1) { this.calendarWeekRule = cultureData.IFIRSTWEEKOFYEAR; }

            if (this.amDesignator == null) { this.amDesignator = cultureData.SAM1159; }
            if (this.pmDesignator == null) { this.pmDesignator = cultureData.SPM2359; }
            if (this.timeSeparator == null) { this.timeSeparator = cultureData.TimeSeparator; }
            if (this.dateSeparator == null) { this.dateSeparator = cultureData.DateSeparator(calendarId); }

            this.allLongTimePatterns = this.m_cultureData.LongTimes;
            Contract.Assert(this.allLongTimePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some long time patterns");

            this.allShortTimePatterns = this.m_cultureData.ShortTimes;
            Contract.Assert(this.allShortTimePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some short time patterns");

            this.allLongDatePatterns = cultureData.LongDates(calendarId);
            Contract.Assert(this.allLongDatePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some long date patterns");

            this.allShortDatePatterns = cultureData.ShortDates(calendarId);
            Contract.Assert(this.allShortDatePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some short date patterns");

            this.allYearMonthPatterns = cultureData.YearMonths(calendarId);
            Contract.Assert(this.allYearMonthPatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some year month patterns");
        }

        [OptionalField(VersionAdded = 1)]
        private bool m_useUserOverride;

        // This was synthesized by Whidbey so we knew what words might appear in the middle of a date string
        // Now we always synthesize so its not helpful

        internal String[] m_dateWords = null;

        [OnSerializing]
        private void OnSerializing(StreamingContext ctx)
        {
            m_name = this.CultureName; // make sure the m_name is initialized.
            m_useUserOverride = this.m_cultureData.UseUserOverride;

            // Important to initialize these fields otherwise we may run into exception when deserializing on Whidbey
            // because Whidbey try to initialize some of these fields using calendar data which could be null values 
            // and then we get exceptions.  So we call the accessors to force the caches to get loaded.
            Object o;
            o = this.LongTimePattern;
            o = this.LongDatePattern;
            o = this.ShortTimePattern;
            o = this.ShortDatePattern;
            o = this.YearMonthPattern;
            o = this.AllLongTimePatterns;
            o = this.AllLongDatePatterns;
            o = this.AllShortTimePatterns;
            o = this.AllShortDatePatterns;
            o = this.AllYearMonthPatterns;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext ctx)
        {
            if (m_name != null)
            {
                m_cultureData = CultureData.GetCultureData(m_name, m_useUserOverride);
                if (m_cultureData == null)
                {
                    throw new CultureNotFoundException("m_name", m_name, SR.Argument_CultureNotSupported);
                }
            }

            if (calendar == null)
            {
                calendar = (Calendar)GregorianCalendar.GetDefaultInstance().Clone();
                calendar.SetReadOnlyState(m_isReadOnly);
            }

            InitializeOverridableProperties(m_cultureData, calendar.ID);

            //
            //  turn off read only state till we finish initializing all fields and then store read only state after we are done.
            //
            bool isReadOnly = m_isReadOnly;
            m_isReadOnly = false;

            // If we deserialized defaults ala Whidbey, make sure they're still defaults
            // Whidbey's arrays could get a bit mixed up.
            if (longDatePattern != null) this.LongDatePattern = longDatePattern;
            if (shortDatePattern != null) this.ShortDatePattern = shortDatePattern;
            if (yearMonthPattern != null) this.YearMonthPattern = yearMonthPattern;
            if (longTimePattern != null) this.LongTimePattern = longTimePattern;
            if (shortTimePattern != null) this.ShortTimePattern = shortTimePattern;

            m_isReadOnly = isReadOnly;
        }

        // Returns a default DateTimeFormatInfo that will be universally
        // supported and constant irrespective of the current culture.
        // Used by FromString methods.
        //

        public static DateTimeFormatInfo InvariantInfo
        {
            get
            {
                Contract.Ensures(Contract.Result<DateTimeFormatInfo>() != null);
                if (invariantInfo == null)
                {
                    DateTimeFormatInfo info = new DateTimeFormatInfo();
                    info.Calendar.SetReadOnlyState(true);
                    info.m_isReadOnly = true;
                    invariantInfo = info;
                }
                return (invariantInfo);
            }
        }

        // Returns the current culture's DateTimeFormatInfo.  Used by Parse methods.
        //

        public static DateTimeFormatInfo CurrentInfo
        {
            get
            {
                Contract.Ensures(Contract.Result<DateTimeFormatInfo>() != null);
                System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
                if (!culture.m_isInherited)
                {
                    DateTimeFormatInfo info = culture.dateTimeInfo;
                    if (info != null)
                    {
                        return info;
                    }
                }
                return (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo));
            }
        }


        public static DateTimeFormatInfo GetInstance(IFormatProvider provider)
        {
            // Fast case for a regular CultureInfo
            DateTimeFormatInfo info;
            CultureInfo cultureProvider = provider as CultureInfo;
            if (cultureProvider != null && !cultureProvider.m_isInherited)
            {
                return cultureProvider.DateTimeFormat;
            }
            // Fast case for a DTFI;
            info = provider as DateTimeFormatInfo;
            if (info != null)
            {
                return info;
            }
            // Wasn't cultureInfo or DTFI, do it the slower way
            if (provider != null)
            {
                info = provider.GetFormat(typeof(DateTimeFormatInfo)) as DateTimeFormatInfo;
                if (info != null)
                {
                    return info;
                }
            }
            // Couldn't get anything, just use currentInfo as fallback
            return CurrentInfo;
        }


        public Object GetFormat(Type formatType)
        {
            return (formatType == typeof(DateTimeFormatInfo) ? this : null);
        }


        public Object Clone()
        {
            DateTimeFormatInfo n = (DateTimeFormatInfo)MemberwiseClone();
            // We can use the data member calendar in the setter, instead of the property Calendar,
            // since the cloned copy should have the same state as the original copy.
            n.calendar = (Calendar)this.Calendar.Clone();
            n.m_isReadOnly = false;
            return n;
        }


        public String AMDesignator
        {
            // auto-generated
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (this.amDesignator == null)
                {
                    this.amDesignator = this.m_cultureData.SAM1159;
                }
                Contract.Assert(this.amDesignator != null, "DateTimeFormatInfo.AMDesignator, amDesignator != null");
                return (this.amDesignator);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();
                ClearTokenHashTable();
                amDesignator = value;
            }
        }


        public Calendar Calendar
        {
            get
            {
                Contract.Ensures(Contract.Result<Calendar>() != null);

                Contract.Assert(this.calendar != null, "DateTimeFormatInfo.Calendar: calendar != null");
                return (this.calendar);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value", SR.ArgumentNull_Obj);
                }
                Contract.EndContractBlock();
                if (value == calendar)
                {
                    return;
                }

                for (int i = 0; i < this.OptionalCalendars.Length; i++)
                {
                    if (this.OptionalCalendars[i] == value.ID)
                    {
                        // We can use this one, so do so.

                        // Clean related properties if we already had a calendar set
                        if (calendar != null)
                        {
                            // clean related properties which are affected by the calendar setting,
                            // so that they will be refreshed when they are accessed next time.
                            //

                            // These properites are in the order as appearing in calendar.xml.
                            m_eraNames = null;
                            m_abbrevEraNames = null;
                            m_abbrevEnglishEraNames = null;

                            monthDayPattern = null;

                            dayNames = null;
                            abbreviatedDayNames = null;
                            m_superShortDayNames = null;
                            monthNames = null;
                            abbreviatedMonthNames = null;
                            genitiveMonthNames = null;
                            m_genitiveAbbreviatedMonthNames = null;
                            leapYearMonthNames = null;
                            formatFlags = DateTimeFormatFlags.NotInitialized;

                            allShortDatePatterns = null;
                            allLongDatePatterns = null;
                            allYearMonthPatterns = null;
                            dateTimeOffsetPattern = null;

                            // The defaults need reset as well:
                            longDatePattern = null;
                            shortDatePattern = null;
                            yearMonthPattern = null;

                            // These properies are not in the OS data, but they are dependent on the values like shortDatePattern.
                            fullDateTimePattern = null; // Long date + long time
                            generalShortTimePattern = null; // short date + short time
                            generalLongTimePattern = null; // short date + long time

                            // Derived item that changes
                            dateSeparator = null;

                            // We don't need to do these because they are not changed by changing calendar
                            //      amDesignator
                            //      pmDesignator
                            //      timeSeparator
                            //      longTimePattern
                            //      firstDayOfWeek
                            //      calendarWeekRule

                            // remember to reload tokens
                            ClearTokenHashTable();
                        }

                        // Remember the new calendar
                        calendar = value;
                        InitializeOverridableProperties(m_cultureData, calendar.ID);

                        // We succeeded, return
                        return;
                    }
                }

                // The assigned calendar is not a valid calendar for this culture, throw
                throw new ArgumentOutOfRangeException("value", SR.Argument_InvalidCalendar);
            }
        }

        private CalendarId[] OptionalCalendars
        {
            get
            {
                if (this.optionalCalendars == null)
                {
                    this.optionalCalendars = this.m_cultureData.CalendarIds;
                }
                return (this.optionalCalendars);
            }
        }

        /*=================================GetEra==========================
        **Action: Get the era value by parsing the name of the era.
        **Returns: The era value for the specified era name.
        **      -1 if the name of the era is not valid or not supported.
        **Arguments: eraName    the name of the era.
        **Exceptions: None.
        ============================================================================*/


        public int GetEra(String eraName)
        {
            if (eraName == null)
            {
                throw new ArgumentNullException("eraName",
                    SR.ArgumentNull_String);
            }
            Contract.EndContractBlock();

            // confer 85900 DTFI.GetEra("") should fail on all cultures
            if (eraName.Length == 0)
            {
                return (-1);
            }

            // The following is based on the assumption that the era value is starting from 1, and has a
            // serial values.
            // If that ever changes, the code has to be changed.

            // The calls to String.Compare should use the current culture for the string comparisons, but the
            // invariant culture when comparing against the english names.
            for (int i = 0; i < EraNames.Length; i++)
            {
                // Compare the era name in a case-insensitive way for the appropriate culture.
                if (m_eraNames[i].Length > 0)
                {
                    if (this.Culture.CompareInfo.Compare(eraName, m_eraNames[i], CompareOptions.IgnoreCase) == 0)
                    {
                        return (i + 1);
                    }
                }
            }
            for (int i = 0; i < AbbreviatedEraNames.Length; i++)
            {
                // Compare the abbreviated era name in a case-insensitive way for the appropriate culture.              
                if (this.Culture.CompareInfo.Compare(eraName, m_abbrevEraNames[i], CompareOptions.IgnoreCase) == 0)
                {
                    return (i + 1);
                }
            }
            for (int i = 0; i < AbbreviatedEnglishEraNames.Length; i++)
            {
                // this comparison should use the InvariantCulture.  The English name could have linguistically
                // interesting characters.
                if (CultureInfo.InvariantCulture.CompareInfo.Compare(eraName, m_abbrevEnglishEraNames[i], CompareOptions.IgnoreCase) == 0)
                {
                    return (i + 1);
                }
            }
            return (-1);
        }


        internal String[] EraNames
        {
            get
            {
                if (this.m_eraNames == null)
                {
                    this.m_eraNames = this.m_cultureData.EraNames(Calendar.ID); ;
                }
                return (this.m_eraNames);
            }
        }

        /*=================================GetEraName==========================
        **Action: Get the name of the era for the specified era value.
        **Returns: The name of the specified era.
        **Arguments:
        **      era the era value.
        **Exceptions:
        **      ArguementException if the era valie is invalid.
        ============================================================================*/

        // Era names are 1 indexed
        public String GetEraName(int era)
        {
            if (era == Calendar.CurrentEra)
            {
                era = Calendar.CurrentEraValue;
            }

            // The following is based on the assumption that the era value is starting from 1, and has a
            // serial values.
            // If that ever changes, the code has to be changed.
            if ((--era) < EraNames.Length && (era >= 0))
            {
                return (m_eraNames[era]);
            }
            throw new ArgumentOutOfRangeException("era", SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal String[] AbbreviatedEraNames
        {
            get
            {
                if (this.m_abbrevEraNames == null)
                {
                    this.m_abbrevEraNames = this.m_cultureData.AbbrevEraNames(Calendar.ID);
                }
                return (this.m_abbrevEraNames);
            }
        }

        // Era names are 1 indexed
        public String GetAbbreviatedEraName(int era)
        {
            if (AbbreviatedEraNames.Length == 0)
            {
                // If abbreviation era name is not used in this culture,
                // return the full era name.
                return (GetEraName(era));
            }
            if (era == Calendar.CurrentEra)
            {
                era = Calendar.CurrentEraValue;
            }
            if ((--era) < m_abbrevEraNames.Length && (era >= 0))
            {
                return (m_abbrevEraNames[era]);
            }
            throw new ArgumentOutOfRangeException("era", SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal String[] AbbreviatedEnglishEraNames
        {
            get
            {
                if (this.m_abbrevEnglishEraNames == null)
                {
                    Contract.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.AbbreviatedEnglishEraNames] Expected Calendar.ID > 0");
                    this.m_abbrevEnglishEraNames = this.m_cultureData.AbbreviatedEnglishEraNames(Calendar.ID);
                }
                return (this.m_abbrevEnglishEraNames);
            }
        }


        // Note that cultureData derives this from the short date format (unless someone's set this previously)
        // Note that this property is quite undesirable.
        internal String DateSeparator
        {
            get
            {
                if (this.dateSeparator == null)
                {
                    this.dateSeparator = this.m_cultureData.DateSeparator(Calendar.ID);
                }
                Contract.Assert(this.dateSeparator != null, "DateTimeFormatInfo.DateSeparator, dateSeparator != null");
                return (this.dateSeparator);
            }
            set
            {
                throw null;
            }
        }


        public DayOfWeek FirstDayOfWeek
        {
            get
            {
                if (this.firstDayOfWeek == -1)
                {
                    this.firstDayOfWeek = this.m_cultureData.IFIRSTDAYOFWEEK;
                }
                Contract.Assert(this.firstDayOfWeek != -1, "DateTimeFormatInfo.FirstDayOfWeek, firstDayOfWeek != -1");

                return ((DayOfWeek)this.firstDayOfWeek);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value >= DayOfWeek.Sunday && value <= DayOfWeek.Saturday)
                {
                    firstDayOfWeek = (int)value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        "value", SR.Format(SR.ArgumentOutOfRange_Range,
                        DayOfWeek.Sunday, DayOfWeek.Saturday));
                }
            }
        }

        public CalendarWeekRule CalendarWeekRule
        {
            get
            {
                if (this.calendarWeekRule == -1)
                {
                    this.calendarWeekRule = this.m_cultureData.IFIRSTWEEKOFYEAR;
                }
                Contract.Assert(this.calendarWeekRule != -1, "DateTimeFormatInfo.CalendarWeekRule, calendarWeekRule != -1");
                return ((CalendarWeekRule)this.calendarWeekRule);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value >= CalendarWeekRule.FirstDay && value <= CalendarWeekRule.FirstFourDayWeek)
                {
                    calendarWeekRule = (int)value;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(
                        "value", SR.Format(SR.ArgumentOutOfRange_Range,
                        CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek));
                }
            }
        }

        public String FullDateTimePattern
        {
            get
            {
                if (fullDateTimePattern == null)
                {
                    fullDateTimePattern = LongDatePattern + " " + LongTimePattern;
                }
                return (fullDateTimePattern);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();
                fullDateTimePattern = value;
            }
        }


        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]
        public String LongDatePattern
        {
            get
            {
                // Initialize our long date pattern from the 1st array value if not set
                if (this.longDatePattern == null)
                {
                    // Initialize our data
                    this.longDatePattern = this.UnclonedLongDatePatterns[0];
                }

                return this.longDatePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();

                // Remember the new string
                this.longDatePattern = value;

                // Clear the token hash table
                ClearTokenHashTable();

                // Clean up cached values that will be affected by this property.
                this.fullDateTimePattern = null;
            }
        }

        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]
        public String LongTimePattern
        {
            get
            {
                // Initialize our long time pattern from the 1st array value if not set
                if (this.longTimePattern == null)
                {
                    // Initialize our data
                    this.longTimePattern = this.UnclonedLongTimePatterns[0];
                }

                return this.longTimePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();

                // Remember the new string
                this.longTimePattern = value;

                // Clear the token hash table
                ClearTokenHashTable();

                // Clean up cached values that will be affected by this property.
                this.fullDateTimePattern = null;     // Full date = long date + long Time
                this.generalLongTimePattern = null;  // General long date = short date + long Time
                this.dateTimeOffsetPattern = null;
            }
        }


        // Note: just to be confusing there's only 1 month day pattern, not a whole list
        public String MonthDayPattern
        {
            get
            {
                if (this.monthDayPattern == null)
                {
                    Contract.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.MonthDayPattern] Expected calID > 0");
                    this.monthDayPattern = this.m_cultureData.MonthDay(Calendar.ID);
                }
                Contract.Assert(this.monthDayPattern != null, "DateTimeFormatInfo.MonthDayPattern, monthDayPattern != null");
                return (this.monthDayPattern);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();

                this.monthDayPattern = value;
            }
        }


        public String PMDesignator
        {
            // auto-generated
            [System.Security.SecuritySafeCritical]  // auto-generated
            get
            {
                if (this.pmDesignator == null)
                {
                    this.pmDesignator = this.m_cultureData.SPM2359;
                }
                Contract.Assert(this.pmDesignator != null, "DateTimeFormatInfo.PMDesignator, pmDesignator != null");
                return (this.pmDesignator);
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();
                ClearTokenHashTable();

                pmDesignator = value;
            }
        }


        public String RFC1123Pattern
        {
            get
            {
                return (rfc1123Pattern);
            }
        }

        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]
        public String ShortDatePattern
        {
            get
            {
                // Initialize our short date pattern from the 1st array value if not set
                if (this.shortDatePattern == null)
                {
                    // Initialize our data
                    this.shortDatePattern = this.UnclonedShortDatePatterns[0];
                }

                return this.shortDatePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                Contract.EndContractBlock();

                // Remember the new string
                this.shortDatePattern = value;

                // Clear the token hash table, note that even short dates could require this
                ClearTokenHashTable();

                // Clean up cached values that will be affected by this property.
                generalLongTimePattern = null;   // General long time = short date + long time
                generalShortTimePattern = null;  // General short time = short date + short Time
                dateTimeOffsetPattern = null;
            }
        }


        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]
        public String ShortTimePattern
        {
            get
            {
                // Initialize our short time pattern from the 1st array value if not set
                if (this.shortTimePattern == null)
                {
                    // Initialize our data
                    this.shortTimePattern = this.UnclonedShortTimePatterns[0];
                }
                return this.shortTimePattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();

                // Remember the new string
                this.shortTimePattern = value;

                // Clear the token hash table, note that even short times could require this
                ClearTokenHashTable();

                // Clean up cached values that will be affected by this property.
                generalShortTimePattern = null; // General short date = short date + short time.
            }
        }


        public String SortableDateTimePattern
        {
            get
            {
                return (sortableDateTimePattern);
            }
        }

        /*=================================GeneralShortTimePattern=====================
        **Property: Return the pattern for 'g' general format: shortDate + short time
        **Note: This is used by DateTimeFormat.cs to get the pattern for 'g'
        **      We put this internal property here so that we can avoid doing the
        **      concatation every time somebody asks for the general format.
        ==============================================================================*/

        internal String GeneralShortTimePattern
        {
            get
            {
                if (generalShortTimePattern == null)
                {
                    generalShortTimePattern = ShortDatePattern + " " + ShortTimePattern;
                }
                return (generalShortTimePattern);
            }
        }

        /*=================================GeneralLongTimePattern=====================
        **Property: Return the pattern for 'g' general format: shortDate + Long time
        **Note: This is used by DateTimeFormat.cs to get the pattern for 'g'
        **      We put this internal property here so that we can avoid doing the
        **      concatation every time somebody asks for the general format.
        ==============================================================================*/

        internal String GeneralLongTimePattern
        {
            get
            {
                if (generalLongTimePattern == null)
                {
                    generalLongTimePattern = ShortDatePattern + " " + LongTimePattern;
                }
                return (generalLongTimePattern);
            }
        }

        /*=================================DateTimeOffsetPattern==========================
        **Property: Return the default pattern DateTimeOffset : shortDate + long time + time zone offset
        **Note: This is used by DateTimeFormat.cs to get the pattern for short Date + long time +  time zone offset
        **      We put this internal property here so that we can avoid doing the
        **      concatation every time somebody uses this form
        ==============================================================================*/

        /*=================================DateTimeOffsetPattern==========================
        **Property: Return the default pattern DateTimeOffset : shortDate + long time + time zone offset
        **Note: This is used by DateTimeFormat.cs to get the pattern for short Date + long time +  time zone offset
        **      We put this internal property here so that we can avoid doing the
        **      concatation every time somebody uses this form
        ==============================================================================*/

        internal String DateTimeOffsetPattern
        {
            get
            {
                if (dateTimeOffsetPattern == null)
                {
                    dateTimeOffsetPattern = ShortDatePattern + " " + LongTimePattern;

                    /* LongTimePattern might contain a "z" as part of the format string in which case we don't want to append a time zone offset */

                    bool foundZ = false;
                    bool inQuote = false;
                    char quote = '\'';
                    for (int i = 0; !foundZ && i < LongTimePattern.Length; i++)
                    {
                        switch (LongTimePattern[i])
                        {
                            case 'z':
                                /* if we aren't in a quote, we've found a z */
                                foundZ = !inQuote;
                                /* we'll fall out of the loop now because the test includes !foundZ */
                                break;
                            case '\'':
                            case '\"':
                                if (inQuote && (quote == LongTimePattern[i]))
                                {
                                    /* we were in a quote and found a matching exit quote, so we are outside a quote now */
                                    inQuote = false;
                                }
                                else if (!inQuote)
                                {
                                    quote = LongTimePattern[i];
                                    inQuote = true;
                                }
                                else
                                {
                                    /* we were in a quote and saw the other type of quote character, so we are still in a quote */
                                }
                                break;
                            case '%':
                            case '\\':
                                i++; /* skip next character that is escaped by this backslash */
                                break;
                            default:
                                break;
                        }
                    }

                    if (!foundZ)
                    {
                        dateTimeOffsetPattern = dateTimeOffsetPattern + " zzz";
                    }
                }
                return (dateTimeOffsetPattern);
            }
        }

        // Note that cultureData derives this from the long time format (unless someone's set this previously)
        // Note that this property is quite undesirable.
        // TODO: Arrowhead - consider deprecating
        internal String TimeSeparator
        {
            get
            {
                if (timeSeparator == null)
                {
                    timeSeparator = this.m_cultureData.TimeSeparator;
                }
                Contract.Assert(this.timeSeparator != null, "DateTimeFormatInfo.TimeSeparator, timeSeparator != null");
                return (timeSeparator);
            }

            set
            {
                throw null;
            }
        }


        public String UniversalSortableDateTimePattern
        {
            get
            {
                return (universalSortableDateTimePattern);
            }
        }

        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]
        public String YearMonthPattern
        {
            get
            {
                // Initialize our year/month pattern from the 1st array value if not set
                if (this.yearMonthPattern == null)
                {
                    // Initialize our data
                    this.yearMonthPattern = this.UnclonedYearMonthPatterns[0];
                }
                return this.yearMonthPattern;
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_String);
                }
                Contract.EndContractBlock();

                // Remember the new string
                this.yearMonthPattern = value;

                // Clear the token hash table, note that even short times could require this
                ClearTokenHashTable();
            }
        }

        //
        // Check if a string array contains a null value, and throw ArgumentNullException with parameter name "value"
        //
        static private void CheckNullValue(String[] values, int length)
        {
            Contract.Requires(values != null, "value != null");
            Contract.Requires(values.Length >= length);
            for (int i = 0; i < length; i++)
            {
                if (values[i] == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_ArrayValue);
                }
            }
        }


        public String[] AbbreviatedDayNames
        {
            get
            {
                return ((String[])internalGetAbbreviatedDayOfWeekNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 7)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length);
                ClearTokenHashTable();

                abbreviatedDayNames = value;
            }
        }

        // Returns the string array of the one-letter day of week names.
        [System.Runtime.InteropServices.ComVisible(false)]
        public String[] ShortestDayNames
        {
            get
            {
                return ((String[])internalGetSuperShortDayNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 7)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length);
                this.m_superShortDayNames = value;
            }
        }


        public String[] DayNames
        {
            get
            {
                return ((String[])internalGetDayOfWeekNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 7)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length);
                ClearTokenHashTable();

                dayNames = value;
            }
        }


        public String[] AbbreviatedMonthNames
        {
            get
            {
                return ((String[])internalGetAbbreviatedMonthNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length - 1);
                ClearTokenHashTable();
                abbreviatedMonthNames = value;
            }
        }


        public String[] MonthNames
        {
            get
            {
                return ((String[])internalGetMonthNames().Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length - 1);
                monthNames = value;
                ClearTokenHashTable();
            }
        }

        // Whitespaces that we allow in the month names.
        // U+00a0 is non-breaking space.
        static char[] MonthSpaces = { ' ', '\u00a0' };

        internal bool HasSpacesInMonthNames
        {
            get
            {
                return (FormatFlags & DateTimeFormatFlags.UseSpacesInMonthNames) != 0;
            }
        }

        internal bool HasSpacesInDayNames
        {
            get
            {
                return (FormatFlags & DateTimeFormatFlags.UseSpacesInDayNames) != 0;
            }
        }


        //
        //  internalGetMonthName
        //
        // Actions: Return the month name using the specified MonthNameStyles in either abbreviated form
        //      or full form.
        // Arguments:
        //      month
        //      style           To indicate a form like regular/genitive/month name in a leap year.
        //      abbreviated     When true, return abbreviated form.  Otherwise, return a full form.
        //  Exceptions:
        //      ArgumentOutOfRangeException When month name is invalid.
        //
        internal String internalGetMonthName(int month, MonthNameStyles style, bool abbreviated)
        {
            //
            // Right now, style is mutual exclusive, but I make the style to be flag so that
            // maybe we can combine flag if there is such a need.
            //
            String[] monthNamesArray = null;
            switch (style)
            {
                case MonthNameStyles.Genitive:
                    monthNamesArray = internalGetGenitiveMonthNames(abbreviated);
                    break;
                case MonthNameStyles.LeapYear:
                    monthNamesArray = internalGetLeapYearMonthNames(/*abbreviated*/);
                    break;
                default:
                    monthNamesArray = (abbreviated ? internalGetAbbreviatedMonthNames() : internalGetMonthNames());
                    break;
            }
            // The month range is from 1 ~ this.m_monthNames.Length
            // (actually is 13 right now for all cases)
            if ((month < 1) || (month > monthNamesArray.Length))
            {
                throw new ArgumentOutOfRangeException(
                    "month", SR.Format(SR.ArgumentOutOfRange_Range,
                    1, monthNamesArray.Length));
            }
            return (monthNamesArray[month - 1]);
        }

        //
        //  internalGetGenitiveMonthNames
        //
        //  Action: Retrieve the array which contains the month names in genitive form.
        //      If this culture does not use the gentive form, the normal month name is returned.
        //  Arguments:
        //      abbreviated     When true, return abbreviated form.  Otherwise, return a full form.
        //
        private String[] internalGetGenitiveMonthNames(bool abbreviated)
        {
            if (abbreviated)
            {
                if (this.m_genitiveAbbreviatedMonthNames == null)
                {
                    this.m_genitiveAbbreviatedMonthNames = this.m_cultureData.AbbreviatedGenitiveMonthNames(this.Calendar.ID);
                    Contract.Assert(this.m_genitiveAbbreviatedMonthNames.Length == 13,
                        "[DateTimeFormatInfo.GetGenitiveMonthNames] Expected 13 abbreviated genitive month names in a year");
                }
                return (this.m_genitiveAbbreviatedMonthNames);
            }

            if (this.genitiveMonthNames == null)
            {
                this.genitiveMonthNames = this.m_cultureData.GenitiveMonthNames(this.Calendar.ID);
                Contract.Assert(this.genitiveMonthNames.Length == 13,
                    "[DateTimeFormatInfo.GetGenitiveMonthNames] Expected 13 genitive month names in a year");
            }
            return (this.genitiveMonthNames);
        }

        //
        //  internalGetLeapYearMonthNames
        //
        //  Actions: Retrieve the month names used in a leap year.
        //      If this culture does not have different month names in a leap year, the normal month name is returned.
        //  Agruments: None. (can use abbreviated later if needed)
        //
        internal String[] internalGetLeapYearMonthNames(/*bool abbreviated*/)
        {
            if (this.leapYearMonthNames == null)
            {
                Contract.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.internalGetLeapYearMonthNames] Expected Calendar.ID > 0");
                this.leapYearMonthNames = this.m_cultureData.LeapYearMonthNames(Calendar.ID);
                Contract.Assert(this.leapYearMonthNames.Length == 13,
                    "[DateTimeFormatInfo.internalGetLeapYearMonthNames] Expepcted 13 leap year month names");
            }
            return (leapYearMonthNames);
        }


        public String GetAbbreviatedDayName(DayOfWeek dayofweek)
        {
            if ((int)dayofweek < 0 || (int)dayofweek > 6)
            {
                throw new ArgumentOutOfRangeException(
                    "dayofweek", SR.Format(SR.ArgumentOutOfRange_Range,
                    DayOfWeek.Sunday, DayOfWeek.Saturday));
            }
            Contract.EndContractBlock();
            //
            // Don't call the public property AbbreviatedDayNames here since a clone is needed in that
            // property, so it will be slower.  Instead, use GetAbbreviatedDayOfWeekNames() directly.
            //
            return (internalGetAbbreviatedDayOfWeekNames()[(int)dayofweek]);
        }

        // Get all possible combination of inputs
        static private String[] GetCombinedPatterns(String[] patterns1, String[] patterns2, String connectString)
        {
            Contract.Requires(patterns1 != null);
            Contract.Requires(patterns2 != null);

            // Get array size
            String[] result = new String[patterns1.Length * patterns2.Length];

            // Counter of actual results
            int k = 0;
            for (int i = 0; i < patterns1.Length; i++)
            {
                for (int j = 0; j < patterns2.Length; j++)
                {
                    // Can't combine if null or empty
                    result[k++] = patterns1[i] + connectString + patterns2[j];
                }
            }

            // Return the combinations
            return (result);
        }


        // auto-generated
        internal String[] GetAllDateTimePatterns(char format)
        {
            Contract.Ensures(Contract.Result<String[]>() != null);
            String[] result = null;

            switch (format)
            {
                case 'd':
                    result = this.AllShortDatePatterns;
                    break;
                case 'D':
                    result = this.AllLongDatePatterns;
                    break;
                case 'f':
                    result = GetCombinedPatterns(AllLongDatePatterns, AllShortTimePatterns, " ");
                    break;
                case 'F':
                case 'U':
                    result = GetCombinedPatterns(AllLongDatePatterns, AllLongTimePatterns, " ");
                    break;
                case 'g':
                    result = GetCombinedPatterns(AllShortDatePatterns, AllShortTimePatterns, " ");
                    break;
                case 'G':
                    result = GetCombinedPatterns(AllShortDatePatterns, AllLongTimePatterns, " ");
                    break;
                case 'm':
                case 'M':
                    result = new String[] { MonthDayPattern };
                    break;
                case 'o':
                case 'O':
                    result = new String[] { RoundtripFormat };
                    break;
                case 'r':
                case 'R':
                    result = new String[] { rfc1123Pattern };
                    break;
                case 's':
                    result = new String[] { sortableDateTimePattern };
                    break;
                case 't':
                    result = this.AllShortTimePatterns;
                    break;
                case 'T':
                    result = this.AllLongTimePatterns;
                    break;
                case 'u':
                    result = new String[] { UniversalSortableDateTimePattern };
                    break;
                case 'y':
                case 'Y':
                    result = this.AllYearMonthPatterns;
                    break;
                default:
                    throw new ArgumentException(SR.Format_BadFormatSpecifier, "format");
            }
            return (result);
        }


        public String GetDayName(DayOfWeek dayofweek)
        {
            if ((int)dayofweek < 0 || (int)dayofweek > 6)
            {
                throw new ArgumentOutOfRangeException(
                    "dayofweek", SR.Format(SR.ArgumentOutOfRange_Range,
                    DayOfWeek.Sunday, DayOfWeek.Saturday));
            }
            Contract.EndContractBlock();

            // Use the internal one so that we don't clone the array unnecessarily
            return (internalGetDayOfWeekNames()[(int)dayofweek]);
        }



        public String GetAbbreviatedMonthName(int month)
        {
            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException(
                    "month", SR.Format(SR.ArgumentOutOfRange_Range,
                    1, 13));
            }
            Contract.EndContractBlock();
            // Use the internal one so we don't clone the array unnecessarily
            return (internalGetAbbreviatedMonthNames()[month - 1]);
        }


        public String GetMonthName(int month)
        {
            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException(
                    "month", SR.Format(SR.ArgumentOutOfRange_Range,
                    1, 13));
            }
            Contract.EndContractBlock();
            // Use the internal one so we don't clone the array unnecessarily
            return (internalGetMonthNames()[month - 1]);
        }

        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]
        //
        // The resulting [] can get returned to the calling app, so clone it.
        private static string[] GetMergedPatterns(string[] patterns, string defaultPattern)
        {
            Contract.Assert(patterns != null && patterns.Length > 0,
                            "[DateTimeFormatInfo.GetMergedPatterns]Expected array of at least one pattern");
            Contract.Assert(defaultPattern != null,
                            "[DateTimeFormatInfo.GetMergedPatterns]Expected non null default string");

            // If the default happens to be the first in the list just return (a cloned) copy
            if (defaultPattern == patterns[0])
            {
                return (string[])patterns.Clone();
            }

            // We either need a bigger list, or the pattern from the list.
            int i;
            for (i = 0; i < patterns.Length; i++)
            {
                // Stop if we found it
                if (defaultPattern == patterns[i])
                    break;
            }

            // Either way we're going to need a new array
            string[] newPatterns;

            // Did we find it
            if (i < patterns.Length)
            {
                // Found it, output will be same size
                newPatterns = (string[])patterns.Clone();

                // Have to move [0] item to [i] so we can re-write default at [0]
                // (remember defaultPattern == [i] so this is OK)
                newPatterns[i] = newPatterns[0];
            }
            else
            {
                // Not found, make room for it
                newPatterns = new String[patterns.Length + 1];

                // Copy existing array
                Array.Copy(patterns, 0, newPatterns, 1, patterns.Length);
            }

            // Remember the default
            newPatterns[0] = defaultPattern;

            // Return the reconstructed list
            return newPatterns;
        }

        // Needed by DateTimeFormatInfo and DateTimeFormat
        internal const String RoundtripFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";
        internal const String RoundtripDateTimeUnfixed = "yyyy'-'MM'-'ddTHH':'mm':'ss zzz";

        // Default string isn't necessarily in our string array, so get the 
        // merged patterns of both
        private String[] AllYearMonthPatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedYearMonthPatterns, this.YearMonthPattern);
            }
        }

        private String[] AllShortDatePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedShortDatePatterns, this.ShortDatePattern);
            }
        }

        private String[] AllShortTimePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedShortTimePatterns, this.ShortTimePattern);
            }
        }

        private String[] AllLongDatePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedLongDatePatterns, this.LongDatePattern);
            }
        }

        private String[] AllLongTimePatterns
        {
            get
            {
                return GetMergedPatterns(this.UnclonedLongTimePatterns, this.LongTimePattern);
            }
        }

        // NOTE: Clone this string array if you want to return it to user.  Otherwise, you are returning a writable cache copy.
        // This won't include default, call AllYearMonthPatterns
        private String[] UnclonedYearMonthPatterns
        {
            get
            {
                if (this.allYearMonthPatterns == null)
                {
                    Contract.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.UnclonedYearMonthPatterns] Expected Calendar.ID > 0");
                    this.allYearMonthPatterns = this.m_cultureData.YearMonths(this.Calendar.ID);
                    Contract.Assert(this.allYearMonthPatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedYearMonthPatterns] Expected some year month patterns");
                }

                return this.allYearMonthPatterns;
            }
        }


        // NOTE: Clone this string array if you want to return it to user.  Otherwise, you are returning a writable cache copy.
        // This won't include default, call AllShortDatePatterns
        private String[] UnclonedShortDatePatterns
        {
            get
            {
                if (allShortDatePatterns == null)
                {
                    Contract.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.UnclonedShortDatePatterns] Expected Calendar.ID > 0");
                    this.allShortDatePatterns = this.m_cultureData.ShortDates(this.Calendar.ID);
                    Contract.Assert(this.allShortDatePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedShortDatePatterns] Expected some short date patterns");
                }

                return this.allShortDatePatterns;
            }
        }

        // NOTE: Clone this string array if you want to return it to user.  Otherwise, you are returning a writable cache copy.
        // This won't include default, call AllLongDatePatterns
        private String[] UnclonedLongDatePatterns
        {
            get
            {
                if (allLongDatePatterns == null)
                {
                    Contract.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.UnclonedLongDatePatterns] Expected Calendar.ID > 0");
                    this.allLongDatePatterns = this.m_cultureData.LongDates(this.Calendar.ID);
                    Contract.Assert(this.allLongDatePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedLongDatePatterns] Expected some long date patterns");
                }

                return this.allLongDatePatterns;
            }
        }

        // NOTE: Clone this string array if you want to return it to user.  Otherwise, you are returning a writable cache copy.
        // This won't include default, call AllShortTimePatterns
        private String[] UnclonedShortTimePatterns
        {
            get
            {
                if (this.allShortTimePatterns == null)
                {
                    this.allShortTimePatterns = this.m_cultureData.ShortTimes;
                    Contract.Assert(this.allShortTimePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedShortTimePatterns] Expected some short time patterns");
                }

                return this.allShortTimePatterns;
            }
        }

        // NOTE: Clone this string array if you want to return it to user.  Otherwise, you are returning a writable cache copy.
        // This won't include default, call AllLongTimePatterns
        private String[] UnclonedLongTimePatterns
        {
            get
            {
                if (this.allLongTimePatterns == null)
                {
                    this.allLongTimePatterns = this.m_cultureData.LongTimes;
                    Contract.Assert(this.allLongTimePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedLongTimePatterns] Expected some long time patterns");
                }

                return this.allLongTimePatterns;
            }
        }

        public static DateTimeFormatInfo ReadOnly(DateTimeFormatInfo dtfi)
        {
            if (dtfi == null)
            {
                throw new ArgumentNullException("dtfi",
                    SR.ArgumentNull_Obj);
            }
            Contract.EndContractBlock();
            if (dtfi.IsReadOnly)
            {
                return (dtfi);
            }
            DateTimeFormatInfo newInfo = (DateTimeFormatInfo)(dtfi.MemberwiseClone());
            // We can use the data member calendar in the setter, instead of the property Calendar,
            // since the cloned copy should have the same state as the original copy.
            newInfo.calendar = Calendar.ReadOnly(dtfi.Calendar);
            newInfo.m_isReadOnly = true;
            return (newInfo);
        }

        public bool IsReadOnly
        {
            get
            {
                return (m_isReadOnly);
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public String[] AbbreviatedMonthGenitiveNames
        {
            get
            {
                return ((String[])internalGetGenitiveMonthNames(true).Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length - 1);
                ClearTokenHashTable();
                this.m_genitiveAbbreviatedMonthNames = value;
            }
        }

        [System.Runtime.InteropServices.ComVisible(false)]
        public String[] MonthGenitiveNames
        {
            get
            {
                return ((String[])internalGetGenitiveMonthNames(false).Clone());
            }

            set
            {
                if (IsReadOnly)
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                if (value == null)
                {
                    throw new ArgumentNullException("value",
                        SR.ArgumentNull_Array);
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), "value");
                }
                Contract.EndContractBlock();
                CheckNullValue(value, value.Length - 1);
                genitiveMonthNames = value;
                ClearTokenHashTable();
            }
        }

        //
        // Positive TimeSpan Pattern
        //

        private string m_fullTimeSpanPositivePattern;
        internal String FullTimeSpanPositivePattern
        {
            get
            {
                if (m_fullTimeSpanPositivePattern == null)
                {
                    CultureData cultureDataWithoutUserOverrides;
                    if (m_cultureData.UseUserOverride)
                        cultureDataWithoutUserOverrides = CultureData.GetCultureData(m_cultureData.CultureName, false);
                    else
                        cultureDataWithoutUserOverrides = m_cultureData;
                    String decimalSeparator = new NumberFormatInfo(cultureDataWithoutUserOverrides).NumberDecimalSeparator;

                    m_fullTimeSpanPositivePattern = "d':'h':'mm':'ss'" + decimalSeparator + "'FFFFFFF";
                }
                return m_fullTimeSpanPositivePattern;
            }
        }

        //
        // Negative TimeSpan Pattern
        //

        private string m_fullTimeSpanNegativePattern;
        internal String FullTimeSpanNegativePattern
        {
            get
            {
                if (m_fullTimeSpanNegativePattern == null)
                    m_fullTimeSpanNegativePattern = "'-'" + FullTimeSpanPositivePattern;
                return m_fullTimeSpanNegativePattern;
            }
        }

        //
        // Get suitable CompareInfo from current DTFI object.
        //
        internal CompareInfo CompareInfo
        {
            get
            {
                if (m_compareInfo == null)
                {
                    // We use the regular GetCompareInfo here to make sure the created CompareInfo object is stored in the
                    // CompareInfo cache. otherwise we would just create CompareInfo using m_cultureData.
                    m_compareInfo = CompareInfo.GetCompareInfo(m_cultureData.SCOMPAREINFO);
                }

                return m_compareInfo;
            }
        }


        internal const DateTimeStyles InvalidDateTimeStyles = ~(DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite
                                                               | DateTimeStyles.AllowInnerWhite | DateTimeStyles.NoCurrentDateDefault
                                                               | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal
                                                               | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind);

        internal static void ValidateStyles(DateTimeStyles style, String parameterName)
        {
            if ((style & InvalidDateTimeStyles) != 0)
            {
                throw new ArgumentException(SR.Argument_InvalidDateTimeStyles, parameterName);
            }
            if (((style & (DateTimeStyles.AssumeLocal)) != 0) && ((style & (DateTimeStyles.AssumeUniversal)) != 0))
            {
                throw new ArgumentException(SR.Argument_ConflictingDateTimeStyles, parameterName);
            }
            Contract.EndContractBlock();
            if (((style & DateTimeStyles.RoundtripKind) != 0)
                && ((style & (DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal)) != 0))
            {
                throw new ArgumentException(SR.Argument_ConflictingDateTimeRoundtripStyles, parameterName);
            }
        }

        //
        // Actions: Return the internal flag used in formatting and parsing.
        //  The flag can be used to indicate things like if genitive forms is used in this DTFi, or if leap year gets different month names.
        //
        internal DateTimeFormatFlags FormatFlags
        {
            get
            {
                if (formatFlags == DateTimeFormatFlags.NotInitialized)
                {
                    // Build the format flags from the data in this DTFI
                    formatFlags = DateTimeFormatFlags.None;
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagGenitiveMonth(
                        MonthNames, internalGetGenitiveMonthNames(false), AbbreviatedMonthNames, internalGetGenitiveMonthNames(true));
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInMonthNames(
                        MonthNames, internalGetGenitiveMonthNames(false), AbbreviatedMonthNames, internalGetGenitiveMonthNames(true));
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInDayNames(DayNames, AbbreviatedDayNames);
                    formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseHebrewCalendar((int)Calendar.ID);
                }
                return (formatFlags);
            }
        }

        internal Boolean HasForceTwoDigitYears
        {
            get
            {
                switch (calendar.ID)
                {
                    // Add a special case for Japanese and Taiwan.
                    // If is y/yy, do not get (year % 100). "y" will print
                    // year without leading zero.  "yy" will print year with two-digit in leading zero.
                    // If pattern is yyy/yyyy/..., print year value with two-digit in leading zero.
                    // So year 5 is "05", and year 125 is "125".
                    // The reason for not doing (year % 100) is for Taiwan calendar.
                    // If year 125, then output 125 and not 25.
                    // Note: OS uses "yyyy" for Taiwan calendar by default.
                    case (CalendarId.JAPAN):
                    case (CalendarId.TAIWAN):
                        return true;
                }
                return false;
            }
        }

        // Returns whether the YearMonthAdjustment function has any fix-up work to do for this culture/calendar.
        internal Boolean HasYearMonthAdjustment
        {
            get
            {
                return ((FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0);
            }
        }

        // This is a callback that the parser can make back into the DTFI to let it fiddle with special
        // cases associated with that culture or calendar. Currently this only has special cases for
        // the Hebrew calendar, but this could be extended to other cultures.
        //
        // The return value is whether the year and month are actually valid for this calendar.
        internal Boolean YearMonthAdjustment(ref int year, ref int month, Boolean parsedMonthName)
        {
            if ((FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0)
            {
                // Special rules to fix up the Hebrew year/month

                // When formatting, we only format up to the hundred digit of the Hebrew year, although Hebrew year is now over 5000.
                // E.g. if the year is 5763, we only format as 763.
                if (year < 1000)
                {
                    year += 5000;
                }

                // Because we need to calculate leap year, we should fall out now for an invalid year.
                if (year < Calendar.GetYear(Calendar.MinSupportedDateTime) || year > Calendar.GetYear(Calendar.MaxSupportedDateTime))
                {
                    return false;
                }

                // To handle leap months, the set of month names in the symbol table does not always correspond to the numbers.
                // For non-leap years, month 7 (Adar Bet) is not present, so we need to make using this month invalid and
                // shuffle the other months down.
                if (parsedMonthName)
                {
                    if (!Calendar.IsLeapYear(year))
                    {
                        if (month >= 8)
                        {
                            month--;
                        }
                        else if (month == 7)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        //
        // DateTimeFormatInfo tokenizer.  This is used by DateTime.Parse() to break input string into tokens.
        //

        TokenHashValue[] m_dtfiTokenHash;

        private const int TOKEN_HASH_SIZE = 199;
        private const int SECOND_PRIME = 197;
        private const String dateSeparatorOrTimeZoneOffset = "-";
        private const String invariantDateSeparator = "/";
        private const String invariantTimeSeparator = ":";

        //
        // Common Ignorable Symbols
        //
        internal const String IgnorablePeriod = ".";
        internal const String IgnorableComma = ",";

        //
        // Year/Month/Day suffixes
        //
        internal const String CJKYearSuff = "\u5e74";
        internal const String CJKMonthSuff = "\u6708";
        internal const String CJKDaySuff = "\u65e5";

        internal const String KoreanYearSuff = "\ub144";
        internal const String KoreanMonthSuff = "\uc6d4";
        internal const String KoreanDaySuff = "\uc77c";

        internal const String KoreanHourSuff = "\uc2dc";
        internal const String KoreanMinuteSuff = "\ubd84";
        internal const String KoreanSecondSuff = "\ucd08";

        internal const String CJKHourSuff = "\u6642";
        internal const String ChineseHourSuff = "\u65f6";

        internal const String CJKMinuteSuff = "\u5206";
        internal const String CJKSecondSuff = "\u79d2";

        internal const String LocalTimeMark = "T";

        internal const String GMTName = "GMT";
        internal const String ZuluName = "Z";

        internal const String KoreanLangName = "ko";
        internal const String JapaneseLangName = "ja";
        internal const String EnglishLangName = "en";

        private static volatile DateTimeFormatInfo s_jajpDTFI;
        private static volatile DateTimeFormatInfo s_zhtwDTFI;

        //
        // Create a Japanese DTFI which uses JapaneseCalendar.  This is used to parse
        // date string with Japanese era name correctly even when the supplied DTFI
        // does not use Japanese calendar.
        // The created instance is stored in global s_jajpDTFI.
        //
        internal static DateTimeFormatInfo GetJapaneseCalendarDTFI()
        {
            DateTimeFormatInfo temp = s_jajpDTFI;
            if (temp == null)
            {
                temp = new CultureInfo("ja-JP", false).DateTimeFormat;
                temp.Calendar = JapaneseCalendar.GetDefaultInstance();
                s_jajpDTFI = temp;
            }
            return (temp);
        }

        //
        // Create a Taiwan DTFI which uses TaiwanCalendar.  This is used to parse
        // date string with era name correctly even when the supplied DTFI
        // does not use Taiwan calendar.
        // The created instance is stored in global s_zhtwDTFI.
        internal static DateTimeFormatInfo GetTaiwanCalendarDTFI()
        {
            DateTimeFormatInfo temp = s_zhtwDTFI;
            if (temp == null)
            {
                temp = new CultureInfo("zh-TW", false).DateTimeFormat;
                temp.Calendar = TaiwanCalendar.GetDefaultInstance();
                s_zhtwDTFI = temp;
            }
            return (temp);
        }


        // DTFI properties should call this when the setter are called.
        private void ClearTokenHashTable()
        {
            m_dtfiTokenHash = null;
            formatFlags = DateTimeFormatFlags.NotInitialized;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal TokenHashValue[] CreateTokenHashTable()
        {
            TokenHashValue[] temp = m_dtfiTokenHash;
            if (temp == null)
            {
                temp = new TokenHashValue[TOKEN_HASH_SIZE];

                bool koreanLanguage = LanguageName.Equals(KoreanLangName);

                string sep = this.TimeSeparator.Trim();
                if (IgnorableComma != sep) InsertHash(temp, IgnorableComma, TokenType.IgnorableSymbol, 0);
                if (IgnorablePeriod != sep) InsertHash(temp, IgnorablePeriod, TokenType.IgnorableSymbol, 0);

                if (KoreanHourSuff != sep && CJKHourSuff != sep && ChineseHourSuff != sep)
                {
                    //
                    // On the Macintosh, the default TimeSeparator is identical to the KoreanHourSuff, CJKHourSuff, or ChineseHourSuff for some cultures like
                    // ja-JP and ko-KR.  In these cases having the same symbol inserted into the hash table with multiple TokenTypes causes undesirable
                    // DateTime.Parse behavior.  For instance, the DateTimeFormatInfo.Tokenize() method might return SEP_DateOrOffset for KoreanHourSuff
                    // instead of SEP_HourSuff.
                    //
                    InsertHash(temp, this.TimeSeparator, TokenType.SEP_Time, 0);
                }

                InsertHash(temp, this.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                InsertHash(temp, this.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);

                // TODO: NLS Arrowhead - Yipe, this ignores similar custom cultures
                if (LanguageName.Equals("sq"))
                {
                    // Albanian allows time formats like "12:00.PD"
                    InsertHash(temp, IgnorablePeriod + this.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                    InsertHash(temp, IgnorablePeriod + this.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);
                }

                // CJK suffix
                InsertHash(temp, CJKYearSuff, TokenType.SEP_YearSuff, 0);
                InsertHash(temp, KoreanYearSuff, TokenType.SEP_YearSuff, 0);
                InsertHash(temp, CJKMonthSuff, TokenType.SEP_MonthSuff, 0);
                InsertHash(temp, KoreanMonthSuff, TokenType.SEP_MonthSuff, 0);
                InsertHash(temp, CJKDaySuff, TokenType.SEP_DaySuff, 0);
                InsertHash(temp, KoreanDaySuff, TokenType.SEP_DaySuff, 0);

                InsertHash(temp, CJKHourSuff, TokenType.SEP_HourSuff, 0);
                InsertHash(temp, ChineseHourSuff, TokenType.SEP_HourSuff, 0);
                InsertHash(temp, CJKMinuteSuff, TokenType.SEP_MinuteSuff, 0);
                InsertHash(temp, CJKSecondSuff, TokenType.SEP_SecondSuff, 0);

                // TODO: NLS Arrowhead -This ignores other custom cultures that might want to do something similar
                if (koreanLanguage)
                {
                    // Korean suffix
                    InsertHash(temp, KoreanHourSuff, TokenType.SEP_HourSuff, 0);
                    InsertHash(temp, KoreanMinuteSuff, TokenType.SEP_MinuteSuff, 0);
                    InsertHash(temp, KoreanSecondSuff, TokenType.SEP_SecondSuff, 0);
                }

                if (LanguageName.Equals("ky"))
                {
                    // For some cultures, the date separator works more like a comma, being allowed before or after any date part
                    InsertHash(temp, dateSeparatorOrTimeZoneOffset, TokenType.IgnorableSymbol, 0);
                }
                else
                {
                    InsertHash(temp, dateSeparatorOrTimeZoneOffset, TokenType.SEP_DateOrOffset, 0);
                }

                String[] dateWords = null;
                DateTimeFormatInfoScanner scanner = null;

                // We need to rescan the date words since we're always synthetic
                scanner = new DateTimeFormatInfoScanner();
                m_dateWords = dateWords = scanner.GetDateWordsOfDTFI(this);
                // Ensure the formatflags is initialized.
                DateTimeFormatFlags flag = FormatFlags;

                // For some cultures, the date separator works more like a comma, being allowed before or after any date part.
                // In these cultures, we do not use normal date separator since we disallow date separator after a date terminal state.
                // This is determined in DateTimeFormatInfoScanner.  Use this flag to determine if we should treat date separator as ignorable symbol.
                bool useDateSepAsIgnorableSymbol = false;

                String monthPostfix = null;
                if (dateWords != null)
                {
                    // There are DateWords.  It could be a real date word (such as "de"), or a monthPostfix.
                    // The monthPostfix starts with '\xfffe' (MonthPostfixChar), followed by the real monthPostfix.
                    for (int i = 0; i < dateWords.Length; i++)
                    {
                        switch (dateWords[i][0])
                        {
                            // This is a month postfix
                            case DateTimeFormatInfoScanner.MonthPostfixChar:
                                // Get the real month postfix.
                                monthPostfix = dateWords[i].Substring(1);
                                // Add the month name + postfix into the token.
                                AddMonthNames(temp, monthPostfix);
                                break;
                            case DateTimeFormatInfoScanner.IgnorableSymbolChar:
                                String symbol = dateWords[i].Substring(1);
                                InsertHash(temp, symbol, TokenType.IgnorableSymbol, 0);
                                if (this.DateSeparator.Trim(null).Equals(symbol))
                                {
                                    // The date separator is the same as the ignorable symbol.
                                    useDateSepAsIgnorableSymbol = true;
                                }
                                break;
                            default:
                                InsertHash(temp, dateWords[i], TokenType.DateWordToken, 0);
                                // TODO: NLS Arrowhead -Yipe, this ignores similar custom cultures
                                if (LanguageName.Equals("eu"))
                                {
                                    // Basque has date words with leading dots
                                    InsertHash(temp, IgnorablePeriod + dateWords[i], TokenType.DateWordToken, 0);
                                }
                                break;
                        }
                    }
                }

                if (!useDateSepAsIgnorableSymbol)
                {
                    // Use the normal date separator.
                    InsertHash(temp, this.DateSeparator, TokenType.SEP_Date, 0);
                }
                // Add the regular month names.
                AddMonthNames(temp, null);

                // Add the abbreviated month names.
                for (int i = 1; i <= 13; i++)
                {
                    InsertHash(temp, GetAbbreviatedMonthName(i), TokenType.MonthToken, i);
                }


                if ((FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
                {
                    for (int i = 1; i <= 13; i++)
                    {
                        String str;
                        str = internalGetMonthName(i, MonthNameStyles.Genitive, false);
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }

                if ((FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
                {
                    for (int i = 1; i <= 13; i++)
                    {
                        String str;
                        str = internalGetMonthName(i, MonthNameStyles.LeapYear, false);
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }

                for (int i = 0; i < 7; i++)
                {
                    //String str = GetDayOfWeekNames()[i];
                    // We have to call public methods here to work with inherited DTFI.
                    String str = GetDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);

                    str = GetAbbreviatedDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);
                }

                int[] eras = calendar.Eras;
                for (int i = 1; i <= eras.Length; i++)
                {
                    InsertHash(temp, GetEraName(i), TokenType.EraToken, i);
                    InsertHash(temp, GetAbbreviatedEraName(i), TokenType.EraToken, i);
                }

                // TODO: NLS Arrowhead -This ignores other cultures that might want to do something similar
                if (LanguageName.Equals(JapaneseLangName))
                {
                    // Japanese allows day of week forms like: "(Tue)"
                    for (int i = 0; i < 7; i++)
                    {
                        String specialDayOfWeek = "(" + GetAbbreviatedDayName((DayOfWeek)i) + ")";
                        InsertHash(temp, specialDayOfWeek, TokenType.DayOfWeekToken, i);
                    }
                    if (this.Calendar.GetType() != typeof(JapaneseCalendar))
                    {
                        // Special case for Japanese.  If this is a Japanese DTFI, and the calendar is not Japanese calendar,
                        // we will check Japanese Era name as well when the calendar is Gregorian.
                        DateTimeFormatInfo jaDtfi = GetJapaneseCalendarDTFI();
                        for (int i = 1; i <= jaDtfi.Calendar.Eras.Length; i++)
                        {
                            InsertHash(temp, jaDtfi.GetEraName(i), TokenType.JapaneseEraToken, i);
                            InsertHash(temp, jaDtfi.GetAbbreviatedEraName(i), TokenType.JapaneseEraToken, i);
                            // m_abbrevEnglishEraNames[0] contains the name for era 1, so the token value is i+1.
                            InsertHash(temp, jaDtfi.AbbreviatedEnglishEraNames[i - 1], TokenType.JapaneseEraToken, i);
                        }
                    }
                }
                // TODO: NLS Arrowhead -This isn't good, it prohibits similar custom cultures, but we hard coded the name
                else if (CultureName.Equals("zh-TW"))
                {
                    DateTimeFormatInfo twDtfi = GetTaiwanCalendarDTFI();
                    for (int i = 1; i <= twDtfi.Calendar.Eras.Length; i++)
                    {
                        if (twDtfi.GetEraName(i).Length > 0)
                        {
                            InsertHash(temp, twDtfi.GetEraName(i), TokenType.TEraToken, i);
                        }
                    }
                }

                InsertHash(temp, InvariantInfo.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                InsertHash(temp, InvariantInfo.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);

                // Add invariant month names and day names.
                for (int i = 1; i <= 12; i++)
                {
                    String str;
                    // We have to call public methods here to work with inherited DTFI.
                    // Insert the month name first, so that they are at the front of abbrevaited
                    // month names.
                    str = InvariantInfo.GetMonthName(i);
                    InsertHash(temp, str, TokenType.MonthToken, i);
                    str = InvariantInfo.GetAbbreviatedMonthName(i);
                    InsertHash(temp, str, TokenType.MonthToken, i);
                }

                for (int i = 0; i < 7; i++)
                {
                    // We have to call public methods here to work with inherited DTFI.
                    String str = InvariantInfo.GetDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);

                    str = InvariantInfo.GetAbbreviatedDayName((DayOfWeek)i);
                    InsertHash(temp, str, TokenType.DayOfWeekToken, i);
                }

                for (int i = 0; i < AbbreviatedEnglishEraNames.Length; i++)
                {
                    // m_abbrevEnglishEraNames[0] contains the name for era 1, so the token value is i+1.
                    InsertHash(temp, AbbreviatedEnglishEraNames[i], TokenType.EraToken, i + 1);
                }

                InsertHash(temp, LocalTimeMark, TokenType.SEP_LocalTimeMark, 0);
                InsertHash(temp, GMTName, TokenType.TimeZoneToken, 0);
                InsertHash(temp, ZuluName, TokenType.TimeZoneToken, 0);

                InsertHash(temp, invariantDateSeparator, TokenType.SEP_Date, 0);
                InsertHash(temp, invariantTimeSeparator, TokenType.SEP_Time, 0);

                m_dtfiTokenHash = temp;
            }
            return (temp);
        }

        private void AddMonthNames(TokenHashValue[] temp, String monthPostfix)
        {
            for (int i = 1; i <= 13; i++)
            {
                String str;
                //str = internalGetMonthName(i, MonthNameStyles.Regular, false);
                // We have to call public methods here to work with inherited DTFI.
                // Insert the month name first, so that they are at the front of abbrevaited
                // month names.
                str = GetMonthName(i);
                if (str.Length > 0)
                {
                    if (monthPostfix != null)
                    {
                        // Insert the month name with the postfix first, so it can be matched first.
                        InsertHash(temp, str + monthPostfix, TokenType.MonthToken, i);
                    }
                    else
                    {
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }
                str = GetAbbreviatedMonthName(i);
                InsertHash(temp, str, TokenType.MonthToken, i);
            }
        }

        ////////////////////////////////////////////////////////////////////////
        //
        // Actions:
        // Try to parse the current word to see if it is a Hebrew number.
        // Tokens will be updated accordingly.
        // This is called by the Lexer of DateTime.Parse().
        //
        // Unlike most of the functions in this class, the return value indicates
        // whether or not it started to parse. The badFormat parameter indicates
        // if parsing began, but the format was bad.
        //
        ////////////////////////////////////////////////////////////////////////

        private static bool TryParseHebrewNumber(
#if INSIDE_CLR
            ref __DTString str,
#else
            ref FormatProvider.__DTString str,
#endif
            out Boolean badFormat,
            out int number)
        {
            number = -1;
            badFormat = false;

            int i = str.Index;
            if (!HebrewNumber.IsDigit(str.Value[i]))
            {
                // If the current character is not a Hebrew digit, just return false.
                // There is no chance that we can parse a valid Hebrew number from here.
                return (false);
            }
            // The current character is a Hebrew digit.  Try to parse this word as a Hebrew number.
            HebrewNumberParsingContext context = new HebrewNumberParsingContext(0);
            HebrewNumberParsingState state;

            do
            {
                state = HebrewNumber.ParseByChar(str.Value[i++], ref context);
                switch (state)
                {
                    case HebrewNumberParsingState.InvalidHebrewNumber:    // Not a valid Hebrew number.
                    case HebrewNumberParsingState.NotHebrewDigit:         // The current character is not a Hebrew digit character.
                        // Break out so that we don't continue to try parse this as a Hebrew number.
                        return (false);
                }
            } while (i < str.Value.Length && (state != HebrewNumberParsingState.FoundEndOfHebrewNumber));

            // When we are here, we are either at the end of the string, or we find a valid Hebrew number.
            Contract.Assert(state == HebrewNumberParsingState.ContinueParsing || state == HebrewNumberParsingState.FoundEndOfHebrewNumber,
                "Invalid returned state from HebrewNumber.ParseByChar()");

            if (state != HebrewNumberParsingState.FoundEndOfHebrewNumber)
            {
                // We reach end of the string but we can't find a terminal state in parsing Hebrew number.
                return (false);
            }

            // We have found a valid Hebrew number.  Update the index.
            str.Advance(i - str.Index);

            // Get the final Hebrew number value from the HebrewNumberParsingContext.
            number = context.result;

            return (true);
        }

        private static bool IsHebrewChar(char ch)
        {
            return (ch >= '\x0590' && ch <= '\x05ff');
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal bool Tokenize(TokenType TokenMask, out TokenType tokenType, out int tokenValue,
#if INSIDE_CLR 
                               ref __DTString str)
#else
                               ref FormatProvider.__DTString str)
#endif

        {
            tokenType = TokenType.UnknownToken;
            tokenValue = 0;

            TokenHashValue value;
            Contract.Assert(str.Index < str.Value.Length, "DateTimeFormatInfo.Tokenize(): start < value.Length");

            char ch = str.m_current;
            bool isLetter = Char.IsLetter(ch);
            if (isLetter)
            {
                ch = this.Culture.TextInfo.ToLower(ch);
                if (IsHebrewChar(ch) && TokenMask == TokenType.RegularTokenMask)
                {
                    bool badFormat;
                    if (TryParseHebrewNumber(ref str, out badFormat, out tokenValue))
                    {
                        if (badFormat)
                        {
                            tokenType = TokenType.UnknownToken;
                            return (false);
                        }
                        // This is a Hebrew number.
                        // Do nothing here.  TryParseHebrewNumber() will update token accordingly.
                        tokenType = TokenType.HebrewNumber;
                        return (true);
                    }
                }
            }


            int hashcode = ch % TOKEN_HASH_SIZE;
            int hashProbe = 1 + ch % SECOND_PRIME;
            int remaining = str.len - str.Index;
            int i = 0;

            TokenHashValue[] hashTable = m_dtfiTokenHash;
            if (hashTable == null)
            {
                hashTable = CreateTokenHashTable();
            }
            do
            {
                value = hashTable[hashcode];
                if (value == null)
                {
                    // Not found.
                    break;
                }
                // Check this value has the right category (regular token or separator token) that we are looking for.
                if (((int)value.tokenType & (int)TokenMask) > 0 && value.tokenString.Length <= remaining)
                {
                    bool compareStrings = true;
                    if (isLetter)
                    {
                        // If this token starts with a letter, make sure that we won't allow partial match.  So you can't tokenize "MarchWed" separately.
                        // Also an optimization to avoid string comparison
                        int nextCharIndex = str.Index + value.tokenString.Length;
                        if (nextCharIndex > str.len)
                        {
                            compareStrings = false;
                        }
                        else if (nextCharIndex < str.len)
                        {
                            // Check word boundary.  The next character should NOT be a letter.
                            char nextCh = str.Value[nextCharIndex];
                            compareStrings = !(Char.IsLetter(nextCh));
                        }
                    }
                    if (compareStrings && CompareStringIgnoreCaseOptimized(str.Value, str.Index, value.tokenString.Length, value.tokenString, 0, value.tokenString.Length))
                    {
                        tokenType = value.tokenType & TokenMask;
                        tokenValue = value.tokenValue;
                        str.Advance(value.tokenString.Length);
                        return (true);
                    }
                    else if ((value.tokenType == TokenType.MonthToken && HasSpacesInMonthNames) ||
                             (value.tokenType == TokenType.DayOfWeekToken && HasSpacesInDayNames))
                    {
                        // For month or day token, we will match the names which have spaces.
                        int matchStrLen = 0;
                        if (str.MatchSpecifiedWords(value.tokenString, true, ref matchStrLen))
                        {
                            tokenType = value.tokenType & TokenMask;
                            tokenValue = value.tokenValue;
                            str.Advance(matchStrLen);
                            return (true);
                        }
                    }
                }
                i++;
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE) hashcode -= TOKEN_HASH_SIZE;
            } while (i < TOKEN_HASH_SIZE);

            return (false);
        }

        void InsertAtCurrentHashNode(TokenHashValue[] hashTable, String str, char ch, TokenType tokenType, int tokenValue, int pos, int hashcode, int hashProbe)
        {
            // Remember the current slot.
            TokenHashValue previousNode = hashTable[hashcode];

            //// Console.WriteLine("   Insert Key: {0} in {1}", str, slotToInsert);
            // Insert the new node into the current slot.
            hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue); ;

            while (++pos < TOKEN_HASH_SIZE)
            {
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE) hashcode -= TOKEN_HASH_SIZE;
                // Remember this slot
                TokenHashValue temp = hashTable[hashcode];

                if (temp != null && this.Culture.TextInfo.ToLower(temp.tokenString[0]) != ch)
                {
                    continue;
                }
                // Put the previous slot into this slot.
                hashTable[hashcode] = previousNode;
                //// Console.WriteLine("  Move {0} to slot {1}", previousNode.tokenString, hashcode);
                if (temp == null)
                {
                    // Done
                    return;
                }
                previousNode = temp;
            };
            Contract.Assert(false, "The hashtable is full.  This should not happen.");
        }

        void InsertHash(TokenHashValue[] hashTable, String str, TokenType tokenType, int tokenValue)
        {
            // The month of the 13th month is allowed to be null, so make sure that we ignore null value here.
            if (str == null || str.Length == 0)
            {
                return;
            }
            TokenHashValue value;
            int i = 0;
            // If there is whitespace characters in the beginning and end of the string, trim them since whitespaces are skipped by
            // DateTime.Parse().
            if (Char.IsWhiteSpace(str[0]) || Char.IsWhiteSpace(str[str.Length - 1]))
            {
                str = str.Trim(null);   // Trim white space characters.
                // Could have space for separators
                if (str.Length == 0)
                    return;
            }
            char ch = this.Culture.TextInfo.ToLower(str[0]);
            int hashcode = ch % TOKEN_HASH_SIZE;
            int hashProbe = 1 + ch % SECOND_PRIME;
            do
            {
                value = hashTable[hashcode];
                if (value == null)
                {
                    //// Console.WriteLine("   Put Key: {0} in {1}", str, hashcode);
                    hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue);
                    return;
                }
                else
                {
                    // Collision happens. Find another slot.
                    if (str.Length >= value.tokenString.Length)
                    {
                        // If there are two tokens with the same prefix, we have to make sure that the longer token should be at the front of
                        // the shorter ones.
                        if (this.CompareStringIgnoreCaseOptimized(str, 0, value.tokenString.Length, value.tokenString, 0, value.tokenString.Length))
                        {
                            if (str.Length > value.tokenString.Length)
                            {
                                // The str to be inserted has the same prefix as the current token, and str is longer.
                                // Insert str into this node, and shift every node behind it.
                                InsertAtCurrentHashNode(hashTable, str, ch, tokenType, tokenValue, i, hashcode, hashProbe);
                                return;
                            }
                            else
                            {
                                // Same token.  If they have different types (regular token vs separator token).  Add them.
                                // If we have the same regular token or separator token in the hash already, do NOT update the hash.
                                // Therefore, the order of inserting token is significant here regarding what tokenType will be kept in the hash.


                                //
                                // Check the current value of RegularToken (stored in the lower 8-bit of tokenType) , and insert the tokenType into the hash ONLY when we don't have a RegularToken yet.
                                // Also check the current value of SeparatorToken (stored in the upper 8-bit of token), and insert the tokenType into the hash ONLY when we don't have the SeparatorToken yet.
                                //

                                int nTokenType = (int)tokenType;
                                int nCurrentTokenTypeInHash = (int)value.tokenType;

                                //
                                // The folowing is the fix for the issue of throwing FormatException when "mar" is passed in string of the short date format dd/MMM/yyyy for es-MX
                                //

                                if (((nCurrentTokenTypeInHash & (int)TokenType.RegularTokenMask) == 0) && ((nTokenType & (int)TokenType.RegularTokenMask) != 0) ||
                                    ((nCurrentTokenTypeInHash & (int)TokenType.SeparatorTokenMask) == 0) && ((nTokenType & (int)TokenType.SeparatorTokenMask) != 0))
                                {
                                    value.tokenType |= tokenType;
                                    if (tokenValue != 0)
                                    {
                                        value.tokenValue = tokenValue;
                                    }
                                }
                                // The token to be inserted is already in the table.  Skip it.
                                return;
                            }
                        }
                    }
                }
                //// Console.WriteLine("  COLLISION. Old Key: {0}, New Key: {1}", hashTable[hashcode].tokenString, str);
                i++;
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE) hashcode -= TOKEN_HASH_SIZE;
            } while (i < TOKEN_HASH_SIZE);
            Contract.Assert(false, "The hashtable is full.  This should not happen.");
        }

        private bool CompareStringIgnoreCaseOptimized(string string1, int offset1, int length1, string string2, int offset2, int length2)
        {
            // Optimize for one character cases which are common due to date and time separators (/ and :)
            if (length1 == 1 && length2 == 1 && string1[offset1] == string2[offset2])
            {
                return true;
            }

            return (this.Culture.CompareInfo.Compare(string1, offset1, length1, string2, offset2, length2, CompareOptions.IgnoreCase) == 0);
        }

        // class DateTimeFormatInfo

        internal class TokenHashValue
        {
            internal String tokenString;
            internal TokenType tokenType;
            internal int tokenValue;

            internal TokenHashValue(String tokenString, TokenType tokenType, int tokenValue)
            {
                this.tokenString = tokenString;
                this.tokenType = tokenType;
                this.tokenValue = tokenValue;
            }
        }
    }

#if !INSIDE_CLR
    //
    // The type of token that will be returned by DateTimeFormatInfo.Tokenize().
    //
    internal enum TokenType
    {
        // The valid token should start from 1.

        // Regular tokens. The range is from 0x00 ~ 0xff.
        NumberToken = 1,    // The number.  E.g. "12"
        YearNumberToken = 2,    // The number which is considered as year number, which has 3 or more digits.  E.g. "2003"
        Am = 3,    // AM timemark. E.g. "AM"
        Pm = 4,    // PM timemark. E.g. "PM"
        MonthToken = 5,    // A word (or words) that represents a month name.  E.g. "March"
        EndOfString = 6,    // End of string
        DayOfWeekToken = 7,    // A word (or words) that represents a day of week name.  E.g. "Monday" or "Mon"
        TimeZoneToken = 8,    // A word that represents a timezone name. E.g. "GMT"
        EraToken = 9,    // A word that represents a era name. E.g. "A.D."
        DateWordToken = 10,   // A word that can appear in a DateTime string, but serves no parsing semantics.  E.g. "de" in Spanish culture.
        UnknownToken = 11,   // An unknown word, which signals an error in parsing.
        HebrewNumber = 12,   // A number that is composed of Hebrew text.  Hebrew calendar uses Hebrew digits for year values, month values, and day values.
        JapaneseEraToken = 13,   // Era name for JapaneseCalendar
        TEraToken = 14,   // Era name for TaiwanCalendar
        IgnorableSymbol = 15,   // A separator like "," that is equivalent to whitespace


        // Separator tokens.
        SEP_Unk = 0x100,         // Unknown separator.
        SEP_End = 0x200,    // The end of the parsing string.
        SEP_Space = 0x300,    // Whitespace (including comma).
        SEP_Am = 0x400,    // AM timemark. E.g. "AM"
        SEP_Pm = 0x500,    // PM timemark. E.g. "PM"
        SEP_Date = 0x600,    // date separator. E.g. "/"
        SEP_Time = 0x700,    // time separator. E.g. ":"
        SEP_YearSuff = 0x800,    // Chinese/Japanese/Korean year suffix.
        SEP_MonthSuff = 0x900,    // Chinese/Japanese/Korean month suffix.
        SEP_DaySuff = 0xa00,    // Chinese/Japanese/Korean day suffix.
        SEP_HourSuff = 0xb00,   // Chinese/Japanese/Korean hour suffix.
        SEP_MinuteSuff = 0xc00,   // Chinese/Japanese/Korean minute suffix.
        SEP_SecondSuff = 0xd00,   // Chinese/Japanese/Korean second suffix.
        SEP_LocalTimeMark = 0xe00,   // 'T', used in ISO 8601 format.
        SEP_DateOrOffset = 0xf00,   // '-' which could be a date separator or start of a time zone offset

        RegularTokenMask = 0x00ff,
        SeparatorTokenMask = 0xff00,
    }
#endif

}
