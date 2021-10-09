// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Globalization
{
    /// <summary>
    /// Flags used to indicate different styles of month names.
    /// This is an internal flag used by internalGetMonthName().
    /// Use flag here in case that we need to provide a combination of these styles
    /// (such as month name of a leap year in genitive form.  Not likely for now,
    /// but would like to keep the option open).
    /// </summary>

    [Flags]
    internal enum MonthNameStyles
    {
        Regular = 0x00000000,
        Genitive = 0x00000001,
        LeapYear = 0x00000002,
    }

    /// <summary>
    /// Flags used to indicate special rule used in parsing/formatting
    /// for a specific DateTimeFormatInfo instance.
    /// This is an internal flag.
    ///
    /// This flag is different from MonthNameStyles because this flag
    /// can be expanded to accommodate parsing behaviors like CJK month names
    /// or alternative month names, etc.
    /// </summary>
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

    public sealed class DateTimeFormatInfo : IFormatProvider, ICloneable
    {
        // cache for the invariant culture.
        // invariantInfo is constant irrespective of your current culture.
        private static volatile DateTimeFormatInfo? s_invariantInfo;

        // an index which points to a record in Culture Data Table.
        private readonly CultureData _cultureData;

        // The culture name used to create this DTFI.
        private string? _name;

        // The language name of the culture used to create this DTFI.
        private string? _langName;

        // CompareInfo usually used by the parser.
        private CompareInfo? _compareInfo;

        // Culture matches current DTFI. mainly used for string comparisons during parsing.
        private CultureInfo? _cultureInfo;

        private string? amDesignator;
        private string? pmDesignator;

        private string? dateSeparator;            // derived from short date (whidbey expects, arrowhead doesn't)

        private string? generalShortTimePattern;     // short date + short time (whidbey expects, arrowhead doesn't)

        private string? generalLongTimePattern;     // short date + long time (whidbey expects, arrowhead doesn't)

        private string? timeSeparator;            // derived from long time (whidbey expects, arrowhead doesn't)
        private string? monthDayPattern;
        private string? dateTimeOffsetPattern;

        private const string rfc1123Pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

        // The sortable pattern is based on ISO 8601.
        private const string sortableDateTimePattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss";
        private const string universalSortableDateTimePattern = "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";

        private Calendar calendar;

        private int firstDayOfWeek = -1;
        private int calendarWeekRule = -1;

        private string? fullDateTimePattern;        // long date + long time (whidbey expects, arrowhead doesn't)

        private string[]? abbreviatedDayNames;

        private string[]? m_superShortDayNames;

        private string[]? dayNames;
        private string[]? abbreviatedMonthNames;
        private string[]? monthNames;

        // Cache the genitive month names that we retrieve from the data table.

        private string[]? genitiveMonthNames;

        // Cache the abbreviated genitive month names that we retrieve from the data table.

        private string[]? m_genitiveAbbreviatedMonthNames;

        // Cache the month names of a leap year that we retrieve from the data table.
        private string[]? leapYearMonthNames;

        // For our "patterns" arrays we have 2 variables, a string and a string[]
        //
        // The string[] contains the list of patterns, EXCEPT the default may not be included.
        // The string contains the default pattern.
        // When we initially construct our string[], we set the string to string[0]

        // The "default" Date/time patterns
        private string? longDatePattern;
        private string? shortDatePattern;
        private string? yearMonthPattern;
        private string? longTimePattern;
        private string? shortTimePattern;

        private string[]? allYearMonthPatterns;

        private string[]? allShortDatePatterns;
        private string[]? allLongDatePatterns;
        private string[]? allShortTimePatterns;
        private string[]? allLongTimePatterns;

        // Cache the era names for this DateTimeFormatInfo instance.
        private string[]? m_eraNames;
        private string[]? m_abbrevEraNames;
        private string[]? m_abbrevEnglishEraNames;

        private CalendarId[]? optionalCalendars;

        private const int DEFAULT_ALL_DATETIMES_SIZE = 132;

        // CultureInfo updates this
        internal bool _isReadOnly;

        // This flag gives hints about if formatting/parsing should perform special code path for things like
        // genitive form or leap year month names.

        private DateTimeFormatFlags formatFlags = DateTimeFormatFlags.NotInitialized;

        private string CultureName => _name ??= _cultureData.CultureName;

        private CultureInfo Culture => _cultureInfo ??= CultureInfo.GetCultureInfo(CultureName);

        private string LanguageName => _langName ??= _cultureData.TwoLetterISOLanguageName;

        /// <summary>
        /// Create an array of string which contains the abbreviated day names.
        /// </summary>
        private string[] InternalGetAbbreviatedDayOfWeekNames() => abbreviatedDayNames ?? InternalGetAbbreviatedDayOfWeekNamesCore();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] InternalGetAbbreviatedDayOfWeekNamesCore()
        {
            // Get the abbreviated day names for our current calendar
            abbreviatedDayNames = _cultureData.AbbreviatedDayNames(Calendar.ID);
            Debug.Assert(abbreviatedDayNames.Length == 7, "[DateTimeFormatInfo.GetAbbreviatedDayOfWeekNames] Expected 7 day names in a week");
            return abbreviatedDayNames;
        }

        /// <summary>
        /// Returns the string array of the one-letter day of week names.
        /// </summary>
        private string[] InternalGetSuperShortDayNames() => m_superShortDayNames ?? InternalGetSuperShortDayNamesCore();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] InternalGetSuperShortDayNamesCore()
        {
            // Get the super short day names for our current calendar
            m_superShortDayNames = _cultureData.SuperShortDayNames(Calendar.ID);
            Debug.Assert(m_superShortDayNames.Length == 7, "[DateTimeFormatInfo.InternalGetSuperShortDayNames] Expected 7 day names in a week");
            return m_superShortDayNames;
        }

        /// <summary>
        /// Create an array of string which contains the day names.
        /// </summary>
        private string[] InternalGetDayOfWeekNames() => dayNames ?? InternalGetDayOfWeekNamesCore();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] InternalGetDayOfWeekNamesCore()
        {
            // Get the day names for our current calendar
            dayNames = _cultureData.DayNames(Calendar.ID);
            Debug.Assert(dayNames.Length == 7, "[DateTimeFormatInfo.GetDayOfWeekNames] Expected 7 day names in a week");
            return dayNames;
        }

        /// <summary>
        /// Create an array of string which contains the abbreviated month names.
        /// </summary>
        private string[] InternalGetAbbreviatedMonthNames() => abbreviatedMonthNames ?? InternalGetAbbreviatedMonthNamesCore();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] InternalGetAbbreviatedMonthNamesCore()
        {
            // Get the month names for our current calendar
            abbreviatedMonthNames = _cultureData.AbbreviatedMonthNames(Calendar.ID);
            Debug.Assert(abbreviatedMonthNames.Length == 12 || abbreviatedMonthNames.Length == 13,
                "[DateTimeFormatInfo.GetAbbreviatedMonthNames] Expected 12 or 13 month names in a year");
            return abbreviatedMonthNames;
        }

        /// <summary>
        /// Create an array of string which contains the month names.
        /// </summary>
        private string[] InternalGetMonthNames() => monthNames ?? internalGetMonthNamesCore();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string[] internalGetMonthNamesCore()
        {
            // Get the month names for our current calendar
            monthNames = _cultureData.MonthNames(Calendar.ID);
            Debug.Assert(monthNames.Length == 12 || monthNames.Length == 13,
                "[DateTimeFormatInfo.GetMonthNames] Expected 12 or 13 month names in a year");
            return monthNames;
        }

        // Invariant DateTimeFormatInfo doesn't have user-overriden values
        // Default calendar is gregorian
        public DateTimeFormatInfo()
            : this(CultureInfo.InvariantCulture._cultureData, GregorianCalendar.GetDefaultInstance())
        {
        }

        internal DateTimeFormatInfo(CultureData cultureData, Calendar cal)
        {
            Debug.Assert(cultureData != null);
            Debug.Assert(cal != null);

            // Remember our culture
            _cultureData = cultureData;
            calendar = cal;
            InitializeOverridableProperties(cultureData, calendar.ID);
        }

        private void InitializeOverridableProperties(CultureData cultureData, CalendarId calendarId)
        {
            Debug.Assert(cultureData != null);
            Debug.Assert(calendarId != CalendarId.UNINITIALIZED_VALUE, "[DateTimeFormatInfo.Populate] Expected initalized calendarId");

            if (firstDayOfWeek == -1)
            {
                firstDayOfWeek = cultureData.FirstDayOfWeek;
            }
            if (calendarWeekRule == -1)
            {
                calendarWeekRule = cultureData.CalendarWeekRule;
            }

            if (amDesignator == null)
            {
                amDesignator = cultureData.AMDesignator;
            }
            if (pmDesignator == null)
            {
                pmDesignator = cultureData.PMDesignator;
            }
            if (timeSeparator == null)
            {
                timeSeparator = cultureData.TimeSeparator;
            }
            if (dateSeparator == null)
            {
                dateSeparator = cultureData.DateSeparator(calendarId);
            }

            allLongTimePatterns = _cultureData.LongTimes;
            Debug.Assert(allLongTimePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some long time patterns");

            allShortTimePatterns = _cultureData.ShortTimes;
            Debug.Assert(allShortTimePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some short time patterns");

            allLongDatePatterns = cultureData.LongDates(calendarId);
            Debug.Assert(allLongDatePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some long date patterns");

            allShortDatePatterns = cultureData.ShortDates(calendarId);
            Debug.Assert(allShortDatePatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some short date patterns");

            allYearMonthPatterns = cultureData.YearMonths(calendarId);
            Debug.Assert(allYearMonthPatterns.Length > 0, "[DateTimeFormatInfo.Populate] Expected some year month patterns");
        }

        /// <summary>
        /// Returns a default DateTimeFormatInfo that will be universally
        /// supported and constant irrespective of the current culture.
        /// </summary>
        public static DateTimeFormatInfo InvariantInfo
        {
            get
            {
                if (s_invariantInfo == null)
                {
                    DateTimeFormatInfo info = new DateTimeFormatInfo();
                    info.Calendar.SetReadOnlyState(true);
                    info._isReadOnly = true;
                    s_invariantInfo = info;
                }
                return s_invariantInfo;
            }
        }

        /// <summary>
        /// Returns the current culture's DateTimeFormatInfo.
        /// </summary>
        public static DateTimeFormatInfo CurrentInfo
        {
            get
            {
                System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture;
                if (!culture._isInherited)
                {
                    DateTimeFormatInfo? info = culture._dateTimeInfo;
                    if (info != null)
                    {
                        return info;
                    }
                }
                return (DateTimeFormatInfo)culture.GetFormat(typeof(DateTimeFormatInfo))!;
            }
        }

        public static DateTimeFormatInfo GetInstance(IFormatProvider? provider) =>
            provider == null ? CurrentInfo :
            provider is CultureInfo cultureProvider && !cultureProvider._isInherited ? cultureProvider.DateTimeFormat :
            provider is DateTimeFormatInfo info ? info :
            provider.GetFormat(typeof(DateTimeFormatInfo)) is DateTimeFormatInfo info2 ? info2 :
            CurrentInfo; // Couldn't get anything, just use currentInfo as fallback

        public object? GetFormat(Type? formatType)
        {
            return formatType == typeof(DateTimeFormatInfo) ? this : null;
        }

        public object Clone()
        {
            DateTimeFormatInfo n = (DateTimeFormatInfo)MemberwiseClone();
            // We can use the data member calendar in the setter, instead of the property Calendar,
            // since the cloned copy should have the same state as the original copy.
            n.calendar = (Calendar)Calendar.Clone();
            n._isReadOnly = false;
            return n;
        }

        public string AMDesignator
        {
            get
            {
                if (amDesignator == null)
                {
                    amDesignator = _cultureData.AMDesignator;
                }

                Debug.Assert(amDesignator != null, "DateTimeFormatInfo.AMDesignator, amDesignator != null");
                return amDesignator;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                ClearTokenHashTable();
                amDesignator = value;
            }
        }

        public Calendar Calendar
        {
            get
            {
                Debug.Assert(calendar != null, "DateTimeFormatInfo.Calendar: calendar != null");
                return calendar;
            }
            [MemberNotNull(nameof(calendar))]
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (value == calendar)
                {
                    return;
                }

                for (int i = 0; i < OptionalCalendars.Length; i++)
                {
                    if (OptionalCalendars[i] == value.ID)
                    {
                        // We can use this one, so do so.
                        // Clean related properties if we already had a calendar set
                        if (calendar != null)
                        {
                            // clean related properties which are affected by the calendar setting,
                            // so that they will be refreshed when they are accessed next time.
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
                        InitializeOverridableProperties(_cultureData, calendar.ID);

                        // We succeeded, return
                        return;
                    }
                }

                // The assigned calendar is not a valid calendar for this culture, throw
                throw new ArgumentOutOfRangeException(nameof(value), value, SR.Argument_InvalidCalendar);
            }
        }

        private CalendarId[] OptionalCalendars => optionalCalendars ??= _cultureData.CalendarIds;

        /// <summary>
        /// Get the era value by parsing the name of the era.
        /// </summary>
        public int GetEra(string eraName)
        {
            if (eraName == null)
            {
                throw new ArgumentNullException(nameof(eraName));
            }

            // The Era Name and Abbreviated Era Name
            // for Taiwan Calendar on non-Taiwan SKU returns empty string (which
            // would be matched below) but we don't want the empty string to give
            // us an Era number
            // confer 85900 DTFI.GetEra("") should fail on all cultures
            if (eraName.Length == 0)
            {
                return -1;
            }

            // The following is based on the assumption that the era value is starting from 1, and has a
            // serial values.
            // If that ever changes, the code has to be changed.

            // The calls to String.Compare should use the current culture for the string comparisons, but the
            // invariant culture when comparing against the english names.
            for (int i = 0; i < EraNames.Length; i++)
            {
                // Compare the era name in a case-insensitive way for the appropriate culture.
                if (m_eraNames![i].Length > 0)
                {
                    if (Culture.CompareInfo.Compare(eraName, m_eraNames[i], CompareOptions.IgnoreCase) == 0)
                    {
                        return i + 1;
                    }
                }
            }
            for (int i = 0; i < AbbreviatedEraNames.Length; i++)
            {
                // Compare the abbreviated era name in a case-insensitive way for the appropriate culture.
                if (Culture.CompareInfo.Compare(eraName, m_abbrevEraNames![i], CompareOptions.IgnoreCase) == 0)
                {
                    return i + 1;
                }
            }
            for (int i = 0; i < AbbreviatedEnglishEraNames.Length; i++)
            {
                // this comparison should use the InvariantCulture.  The English name could have linguistically
                // interesting characters.
                if (CompareInfo.Invariant.Compare(eraName, m_abbrevEnglishEraNames![i], CompareOptions.IgnoreCase) == 0)
                {
                    return i + 1;
                }
            }
            return -1;
        }

        internal string[] EraNames => m_eraNames ??= _cultureData.EraNames(Calendar.ID);

        /// <summary>
        /// Get the name of the era for the specified era value.
        /// Era names are 1 indexed
        /// </summary>
        public string GetEraName(int era)
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
                return m_eraNames![era];
            }

            throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal string[] AbbreviatedEraNames => m_abbrevEraNames ??= _cultureData.AbbrevEraNames(Calendar.ID);

        /// <remarks>
        /// Era names are 1 indexed
        /// </remarks>
        public string GetAbbreviatedEraName(int era)
        {
            if (AbbreviatedEraNames.Length == 0)
            {
                // If abbreviation era name is not used in this culture,
                // return the full era name.
                return GetEraName(era);
            }

            if (era == Calendar.CurrentEra)
            {
                era = Calendar.CurrentEraValue;
            }

            if ((--era) < m_abbrevEraNames!.Length && (era >= 0))
            {
                return m_abbrevEraNames[era];
            }

            throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
        }

        internal string[] AbbreviatedEnglishEraNames
        {
            get
            {
                if (m_abbrevEnglishEraNames == null)
                {
                    Debug.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.AbbreviatedEnglishEraNames] Expected Calendar.ID > 0");
                    m_abbrevEnglishEraNames = _cultureData.AbbreviatedEnglishEraNames(Calendar.ID);
                }
                return m_abbrevEnglishEraNames;
            }
        }

        /// <remarks>
        /// Note that cultureData derives this from the short date format (unless someone's set this previously)
        /// Note that this property is quite undesirable.
        /// </remarks>
        public string DateSeparator
        {
            get
            {
                if (dateSeparator == null)
                {
                    dateSeparator = _cultureData.DateSeparator(Calendar.ID);
                }
                Debug.Assert(dateSeparator != null, "DateTimeFormatInfo.DateSeparator, dateSeparator != null");
                return dateSeparator;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                ClearTokenHashTable();
                dateSeparator = value;
            }
        }

        public DayOfWeek FirstDayOfWeek
        {
            get
            {
                if (firstDayOfWeek == -1)
                {
                    firstDayOfWeek = _cultureData.FirstDayOfWeek;
                }
                Debug.Assert(firstDayOfWeek != -1, "DateTimeFormatInfo.FirstDayOfWeek, firstDayOfWeek != -1");

                return (DayOfWeek)firstDayOfWeek;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }

                if (value < DayOfWeek.Sunday || value > DayOfWeek.Saturday)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
                }

                firstDayOfWeek = (int)value;
            }
        }

        public CalendarWeekRule CalendarWeekRule
        {
            get
            {
                if (calendarWeekRule == -1)
                {
                    calendarWeekRule = _cultureData.CalendarWeekRule;
                }

                Debug.Assert(calendarWeekRule != -1, "DateTimeFormatInfo.CalendarWeekRule, calendarWeekRule != -1");
                return (CalendarWeekRule)calendarWeekRule;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value < CalendarWeekRule.FirstDay || value > CalendarWeekRule.FirstFourDayWeek)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Range, CalendarWeekRule.FirstDay, CalendarWeekRule.FirstFourDayWeek));
                }

                calendarWeekRule = (int)value;
            }
        }

        public string FullDateTimePattern
        {
            get => fullDateTimePattern ??= LongDatePattern + " " + LongTimePattern;
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                fullDateTimePattern = value;
            }
        }

        /// <summary>
        /// For our "patterns" arrays we have 2 variables, a string and a string[]
        /// The string[] contains the list of patterns, EXCEPT the default may not be included.
        /// The string contains the default pattern.
        /// When we initially construct our string[], we set the string to string[0]
        /// </summary>
        public string LongDatePattern
        {
            get => longDatePattern ??= UnclonedLongDatePatterns[0]; // initialize from the 1st array value if not set
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Remember the new string
                longDatePattern = value;

                OnLongDatePatternChanged();
            }
        }

        private void OnLongDatePatternChanged()
        {
            // Clear the token hash table
            ClearTokenHashTable();

            // Clean up cached values that will be affected by this property.
            fullDateTimePattern = null;
        }

        /// <summary>
        /// For our "patterns" arrays we have 2 variables, a string and a string[]
        ///
        /// The string[] contains the list of patterns, EXCEPT the default may not be included.
        /// The string contains the default pattern.
        /// When we initially construct our string[], we set the string to string[0]
        /// </summary>
        public string LongTimePattern
        {
            get => longTimePattern ??= UnclonedLongTimePatterns[0]; // initialize from the 1st array value if not set
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Remember the new string
                longTimePattern = value;

                OnLongTimePatternChanged();
            }
        }

        private void OnLongTimePatternChanged()
        {
            // Clear the token hash table
            ClearTokenHashTable();

            // Clean up cached values that will be affected by this property.
            fullDateTimePattern = null;     // Full date = long date + long Time
            generalLongTimePattern = null;  // General long date = short date + long Time
            dateTimeOffsetPattern = null;
        }

        /// <remarks>
        /// Just to be confusing there's only 1 month day pattern, not a whole list
        /// </remarks>
        public string MonthDayPattern
        {
            get
            {
                if (monthDayPattern == null)
                {
                    Debug.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.MonthDayPattern] Expected calID > 0");
                    monthDayPattern = _cultureData.MonthDay(Calendar.ID);
                }

                Debug.Assert(monthDayPattern != null, "DateTimeFormatInfo.MonthDayPattern, monthDayPattern != null");
                return monthDayPattern;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                monthDayPattern = value;
            }
        }

        public string PMDesignator
        {
            get
            {
                if (pmDesignator == null)
                {
                    pmDesignator = _cultureData.PMDesignator;
                }

                Debug.Assert(pmDesignator != null, "DateTimeFormatInfo.PMDesignator, pmDesignator != null");
                return pmDesignator;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                ClearTokenHashTable();
                pmDesignator = value;
            }
        }

        public string RFC1123Pattern => rfc1123Pattern;

        /// <summary>
        /// For our "patterns" arrays we have 2 variables, a string and a string[]
        ///
        /// The string[] contains the list of patterns, EXCEPT the default may not be included.
        /// The string contains the default pattern.
        /// When we initially construct our string[], we set the string to string[0]
        /// </summary>
        public string ShortDatePattern
        {
            get
            {
                return shortDatePattern ??= UnclonedShortDatePatterns[0]; // initialize from the 1st array value if not set
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Remember the new string
                shortDatePattern = value;

                OnShortDatePatternChanged();
            }
        }

        private void OnShortDatePatternChanged()
        {
            // Clear the token hash table, note that even short dates could require this
            ClearTokenHashTable();

            // Clean up cached values that will be affected by this property.
            generalLongTimePattern = null;   // General long time = short date + long time
            generalShortTimePattern = null;  // General short time = short date + short Time
            dateTimeOffsetPattern = null;
        }

        /// <summary>
        /// For our "patterns" arrays we have 2 variables, a string and a string[]
        ///
        /// The string[] contains the list of patterns, EXCEPT the default may not be included.
        /// The string contains the default pattern.
        /// When we initially construct our string[], we set the string to string[0]
        /// </summary>
        public string ShortTimePattern
        {
            get => shortTimePattern ??= UnclonedShortTimePatterns[0]; // initialize from the 1st array value if not set
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Remember the new string
                shortTimePattern = value;

                OnShortTimePatternChanged();
            }
        }

        private void OnShortTimePatternChanged()
        {
            // Clear the token hash table, note that even short times could require this
            ClearTokenHashTable();

            // Clean up cached values that will be affected by this property.
            generalShortTimePattern = null; // General short date = short date + short time.
        }

        public string SortableDateTimePattern => sortableDateTimePattern;

        /// <summary>
        /// Return the pattern for 'g' general format: shortDate + short time
        /// This is used by DateTimeFormat.cs to get the pattern for 'g'.
        /// We put this internal property here so that we can avoid doing the
        /// concatation every time somebody asks for the general format.
        /// </summary>
        internal string GeneralShortTimePattern => generalShortTimePattern ??= ShortDatePattern + " " + ShortTimePattern;

        /// <summary>
        /// Return the pattern for 'g' general format: shortDate + Long time.
        /// We put this internal property here so that we can avoid doing the
        /// concatation every time somebody asks for the general format.
        /// </summary>
        internal string GeneralLongTimePattern => generalLongTimePattern ??= ShortDatePattern + " " + LongTimePattern;

        /// Return the default pattern DateTimeOffset : shortDate + long time + time zone offset.
        /// This is used by DateTimeFormat.cs to get the pattern for short Date + long time +  time zone offset
        /// We put this internal property here so that we can avoid doing the
        /// concatation every time somebody uses this form.
        internal string DateTimeOffsetPattern
        {
            get
            {
                if (dateTimeOffsetPattern == null)
                {
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

                    dateTimeOffsetPattern = foundZ ?
                        ShortDatePattern + " " + LongTimePattern :
                        ShortDatePattern + " " + LongTimePattern + " zzz";
                }
                return dateTimeOffsetPattern;
            }
        }

        /// <remarks>
        /// Note that cultureData derives this from the long time format (unless someone's set this previously)
        /// Note that this property is quite undesirable.
        /// </remarks>
        public string TimeSeparator
        {
            get
            {
                if (timeSeparator == null)
                {
                    timeSeparator = _cultureData.TimeSeparator;
                }
                Debug.Assert(timeSeparator != null, "DateTimeFormatInfo.TimeSeparator, timeSeparator != null");
                return timeSeparator;
            }
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                ClearTokenHashTable();
                timeSeparator = value;
            }
        }

        public string UniversalSortableDateTimePattern => universalSortableDateTimePattern;

        /// <summary>
        /// For our "patterns" arrays we have 2 variables, a string and a string[]
        ///
        /// The string[] contains the list of patterns, EXCEPT the default may not be included.
        /// The string contains the default pattern.
        /// When we initially construct our string[], we set the string to string[0]
        /// </summary>
        public string YearMonthPattern
        {
            get => yearMonthPattern ??= UnclonedYearMonthPatterns[0]; // initialize from the 1st array value if not set
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                // Remember the new string
                yearMonthPattern = value;

                OnYearMonthPatternChanged();
            }
        }

        private void OnYearMonthPatternChanged()
        {
            // Clear the token hash table, note that even short times could require this
            ClearTokenHashTable();
        }

        /// <summary>
        /// Check if a string array contains a null value, and throw ArgumentNullException with parameter name "value"
        /// </summary>
        private static void CheckNullValue(string[] values, int length)
        {
            Debug.Assert(values != null, "value != null");
            Debug.Assert(values.Length >= length);
            for (int i = 0; i < length; i++)
            {
                if (values[i] == null)
                {
                    throw new ArgumentNullException("value", SR.ArgumentNull_ArrayValue);
                }
            }
        }

        public string[] AbbreviatedDayNames
        {
            get => (string[])InternalGetAbbreviatedDayOfWeekNames().Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 7)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), nameof(value));
                }

                CheckNullValue(value, value.Length);
                ClearTokenHashTable();

                abbreviatedDayNames = value;
            }
        }

        /// <summary>
        /// Returns the string array of the one-letter day of week names.
        /// </summary>
        public string[] ShortestDayNames
        {
            get => (string[])InternalGetSuperShortDayNames().Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 7)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), nameof(value));
                }

                CheckNullValue(value, value.Length);
                m_superShortDayNames = value;
            }
        }

        public string[] DayNames
        {
            get => (string[])InternalGetDayOfWeekNames().Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 7)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 7), nameof(value));
                }

                CheckNullValue(value, value.Length);
                ClearTokenHashTable();

                dayNames = value;
            }
        }

        public string[] AbbreviatedMonthNames
        {
            get => (string[])InternalGetAbbreviatedMonthNames().Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), nameof(value));
                }

                CheckNullValue(value, value.Length - 1);
                ClearTokenHashTable();
                abbreviatedMonthNames = value;
            }
        }

        public string[] MonthNames
        {
            get => (string[])InternalGetMonthNames().Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), nameof(value));
                }

                CheckNullValue(value, value.Length - 1);
                monthNames = value;
                ClearTokenHashTable();
            }
        }

        internal bool HasSpacesInMonthNames => (FormatFlags & DateTimeFormatFlags.UseSpacesInMonthNames) != 0;

        internal bool HasSpacesInDayNames => (FormatFlags & DateTimeFormatFlags.UseSpacesInDayNames) != 0;

        /// <summary>
        /// Return the month name using the specified MonthNameStyles in either abbreviated form
        /// or full form.
        /// </summary>
        internal string InternalGetMonthName(int month, MonthNameStyles style, bool abbreviated)
        {
            string[] monthNamesArray = style switch
            {
                MonthNameStyles.Genitive => InternalGetGenitiveMonthNames(abbreviated),
                MonthNameStyles.LeapYear => InternalGetLeapYearMonthNames(),
                _ => (abbreviated ? InternalGetAbbreviatedMonthNames() : InternalGetMonthNames()),
            };

            // The month range is from 1 ~ m_monthNames.Length
            // (actually is 13 right now for all cases)
            if ((month < 1) || (month > monthNamesArray.Length))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    month,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, monthNamesArray.Length));
            }

            return monthNamesArray[month - 1];
        }

        /// <summary>
        /// Retrieve the array which contains the month names in genitive form.
        /// If this culture does not use the genitive form, the normal month name is returned.
        /// </summary>
        private string[] InternalGetGenitiveMonthNames(bool abbreviated)
        {
            if (abbreviated)
            {
                if (m_genitiveAbbreviatedMonthNames == null)
                {
                    m_genitiveAbbreviatedMonthNames = _cultureData.AbbreviatedGenitiveMonthNames(Calendar.ID);
                    Debug.Assert(m_genitiveAbbreviatedMonthNames.Length == 13,
                        "[DateTimeFormatInfo.GetGenitiveMonthNames] Expected 13 abbreviated genitive month names in a year");
                }

                return m_genitiveAbbreviatedMonthNames;
            }

            if (genitiveMonthNames == null)
            {
                genitiveMonthNames = _cultureData.GenitiveMonthNames(Calendar.ID);
                Debug.Assert(genitiveMonthNames.Length == 13,
                    "[DateTimeFormatInfo.GetGenitiveMonthNames] Expected 13 genitive month names in a year");
            }

            return genitiveMonthNames;
        }

        /// <summary>
        /// Retrieve the month names used in a leap year.
        /// If this culture does not have different month names in a leap year,
        /// the normal month name is returned.
        /// </summary>
        internal string[] InternalGetLeapYearMonthNames()
        {
            if (leapYearMonthNames == null)
            {
                Debug.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.InternalGetLeapYearMonthNames] Expected Calendar.ID > 0");
                leapYearMonthNames = _cultureData.LeapYearMonthNames(Calendar.ID);
                Debug.Assert(leapYearMonthNames.Length == 13,
                    "[DateTimeFormatInfo.InternalGetLeapYearMonthNames] Expepcted 13 leap year month names");
            }

            return leapYearMonthNames;
        }

        public string GetAbbreviatedDayName(DayOfWeek dayofweek)
        {
            if (dayofweek < DayOfWeek.Sunday || dayofweek > DayOfWeek.Saturday)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayofweek),
                    dayofweek,
                    SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

            // Don't call the public property AbbreviatedDayNames here since a clone is needed in that
            // property, so it will be slower. Instead, use GetAbbreviatedDayOfWeekNames() directly.
            return InternalGetAbbreviatedDayOfWeekNames()[(int)dayofweek];
        }

        /// <summary>
        /// Returns the super short day of week names for the specified day of week.
        /// </summary>
        public string GetShortestDayName(DayOfWeek dayOfWeek)
        {
            if (dayOfWeek < DayOfWeek.Sunday || dayOfWeek > DayOfWeek.Saturday)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayOfWeek),
                    dayOfWeek,
                    SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

            // Don't call the public property SuperShortDayNames here since a clone is needed in that
            // property, so it will be slower. Instead, use internalGetSuperShortDayNames() directly.
            return InternalGetSuperShortDayNames()[(int)dayOfWeek];
        }

        /// <summary>
        /// Get all possible combination of inputs
        /// </summary>
        private static string[] GetCombinedPatterns(string[] patterns1, string[] patterns2, string connectString)
        {
            Debug.Assert(patterns1 != null);
            Debug.Assert(patterns2 != null);

            // Get array size
            string[] result = new string[patterns1.Length * patterns2.Length];

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
            return result;
        }

        public string[] GetAllDateTimePatterns()
        {
            List<string> results = new List<string>(DEFAULT_ALL_DATETIMES_SIZE);

            for (int i = 0; i < DateTimeFormat.allStandardFormats.Length; i++)
            {
                string[] strings = GetAllDateTimePatterns(DateTimeFormat.allStandardFormats[i]);
                for (int j = 0; j < strings.Length; j++)
                {
                    results.Add(strings[j]);
                }
            }
            return results.ToArray();
        }

        public string[] GetAllDateTimePatterns(char format)
        {
            string[] result;

            switch (format)
            {
                case 'd':
                    result = AllShortDatePatterns;
                    break;
                case 'D':
                    result = AllLongDatePatterns;
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
                    result = new string[] { MonthDayPattern };
                    break;
                case 'o':
                case 'O':
                    result = new string[] { RoundtripFormat };
                    break;
                case 'r':
                case 'R':
                    result = new string[] { rfc1123Pattern };
                    break;
                case 's':
                    result = new string[] { sortableDateTimePattern };
                    break;
                case 't':
                    result = AllShortTimePatterns;
                    break;
                case 'T':
                    result = AllLongTimePatterns;
                    break;
                case 'u':
                    result = new string[] { UniversalSortableDateTimePattern };
                    break;
                case 'y':
                case 'Y':
                    result = AllYearMonthPatterns;
                    break;
                default:
                    throw new ArgumentException(SR.Format(SR.Format_BadFormatSpecifier, format), nameof(format));
            }
            return result;
        }

        public string GetDayName(DayOfWeek dayofweek)
        {
            if ((int)dayofweek < 0 || (int)dayofweek > 6)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(dayofweek),
                    dayofweek,
                    SR.Format(SR.ArgumentOutOfRange_Range, DayOfWeek.Sunday, DayOfWeek.Saturday));
            }

            // Use the internal one so that we don't clone the array unnecessarily
            return InternalGetDayOfWeekNames()[(int)dayofweek];
        }

        public string GetAbbreviatedMonthName(int month)
        {
            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    month,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, 13));
            }

            // Use the internal one so we don't clone the array unnecessarily
            return InternalGetAbbreviatedMonthNames()[month - 1];
        }

        public string GetMonthName(int month)
        {
            if (month < 1 || month > 13)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(month),
                    month,
                    SR.Format(SR.ArgumentOutOfRange_Range, 1, 13));
            }

            // Use the internal one so we don't clone the array unnecessarily
            return InternalGetMonthNames()[month - 1];
        }

        /// <summary>
        /// For our "patterns" arrays we have 2 variables, a string and a string[]
        ///
        /// The string[] contains the list of patterns, EXCEPT the default may not be included.
        /// The string contains the default pattern.
        /// When we initially construct our string[], we set the string to string[0]
        ///
        /// The resulting [] can get returned to the calling app, so clone it.
        /// </summary>
        private static string[] GetMergedPatterns(string[] patterns, string defaultPattern)
        {
            Debug.Assert(patterns != null && patterns.Length > 0,
                            "[DateTimeFormatInfo.GetMergedPatterns]Expected array of at least one pattern");
            Debug.Assert(defaultPattern != null,
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
                newPatterns = new string[patterns.Length + 1];

                // Copy existing array
                Array.Copy(patterns, 0, newPatterns, 1, patterns.Length);
            }

            // Remember the default
            newPatterns[0] = defaultPattern;

            // Return the reconstructed list
            return newPatterns;
        }

        // Needed by DateTimeFormatInfo and DateTimeFormat
        internal const string RoundtripFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK";
        internal const string RoundtripDateTimeUnfixed = "yyyy'-'MM'-'ddTHH':'mm':'ss zzz";

        /// <summary>
        /// Default string isn't necessarily in our string array, so get the
        /// merged patterns of both
        /// </summary>
        private string[] AllYearMonthPatterns => GetMergedPatterns(UnclonedYearMonthPatterns, YearMonthPattern);

        private string[] AllShortDatePatterns => GetMergedPatterns(UnclonedShortDatePatterns, ShortDatePattern);

        private string[] AllShortTimePatterns => GetMergedPatterns(UnclonedShortTimePatterns, ShortTimePattern);

        private string[] AllLongDatePatterns => GetMergedPatterns(UnclonedLongDatePatterns, LongDatePattern);

        private string[] AllLongTimePatterns => GetMergedPatterns(UnclonedLongTimePatterns, LongTimePattern);

        /// <remarks>
        /// Clone this string array if you want to return it to user.
        /// Otherwise, you are returning a writable cache copy.
        /// This won't include default, call AllYearMonthPatterns
        /// </remarks>
        private string[] UnclonedYearMonthPatterns
        {
            get
            {
                if (allYearMonthPatterns == null)
                {
                    Debug.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.UnclonedYearMonthPatterns] Expected Calendar.ID > 0");
                    allYearMonthPatterns = _cultureData.YearMonths(Calendar.ID);
                    Debug.Assert(allYearMonthPatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedYearMonthPatterns] Expected some year month patterns");
                }

                return allYearMonthPatterns;
            }
        }

        /// <remarks>
        /// Clone this string array if you want to return it to user.
        /// Otherwise, you are returning a writable cache copy.
        /// This won't include default, call AllShortDatePatterns
        /// </remarks>
        private string[] UnclonedShortDatePatterns
        {
            get
            {
                if (allShortDatePatterns == null)
                {
                    Debug.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.UnclonedShortDatePatterns] Expected Calendar.ID > 0");
                    allShortDatePatterns = _cultureData.ShortDates(Calendar.ID);
                    Debug.Assert(allShortDatePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedShortDatePatterns] Expected some short date patterns");
                }

                return allShortDatePatterns;
            }
        }

        /// <remarks>
        /// Clone this string array if you want to return it to user.
        /// Otherwise, you are returning a writable cache copy.
        /// This won't include default, call AllLongDatePatterns
        /// </remarks>
        private string[] UnclonedLongDatePatterns
        {
            get
            {
                if (allLongDatePatterns == null)
                {
                    Debug.Assert(Calendar.ID > 0, "[DateTimeFormatInfo.UnclonedLongDatePatterns] Expected Calendar.ID > 0");
                    allLongDatePatterns = _cultureData.LongDates(Calendar.ID);
                    Debug.Assert(allLongDatePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedLongDatePatterns] Expected some long date patterns");
                }

                return allLongDatePatterns;
            }
        }

        /// <remarks>
        /// Clone this string array if you want to return it to user.
        /// Otherwise, you are returning a writable cache copy.
        /// This won't include default, call AllShortTimePatterns
        /// </remarks>
        private string[] UnclonedShortTimePatterns
        {
            get
            {
                if (allShortTimePatterns == null)
                {
                    allShortTimePatterns = _cultureData.ShortTimes;
                    Debug.Assert(allShortTimePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedShortTimePatterns] Expected some short time patterns");
                }

                return allShortTimePatterns;
            }
        }

        /// <remarks>
        /// Clone this string array if you want to return it to user.
        /// Otherwise, you are returning a writable cache copy.
        /// This won't include default, call AllLongTimePatterns
        /// </remarks>
        private string[] UnclonedLongTimePatterns
        {
            get
            {
                if (allLongTimePatterns == null)
                {
                    allLongTimePatterns = _cultureData.LongTimes;
                    Debug.Assert(allLongTimePatterns.Length > 0,
                        "[DateTimeFormatInfo.UnclonedLongTimePatterns] Expected some long time patterns");
                }

                return allLongTimePatterns;
            }
        }

        public static DateTimeFormatInfo ReadOnly(DateTimeFormatInfo dtfi)
        {
            if (dtfi == null)
            {
                throw new ArgumentNullException(nameof(dtfi));
            }

            if (dtfi.IsReadOnly)
            {
                return dtfi;
            }

            DateTimeFormatInfo newInfo = (DateTimeFormatInfo)(dtfi.MemberwiseClone());
            // We can use the data member calendar in the setter, instead of the property Calendar,
            // since the cloned copy should have the same state as the original copy.
            newInfo.calendar = Calendar.ReadOnly(dtfi.Calendar);
            newInfo._isReadOnly = true;
            return newInfo;
        }

        public bool IsReadOnly => _isReadOnly;

        /// <summary>
        /// Return the native name for the calendar in DTFI.Calendar.  The native name is referred to
        /// the culture used to create the DTFI.  E.g. in the following example, the native language is Japanese.
        /// DateTimeFormatInfo dtfi = new CultureInfo("ja-JP", false).DateTimeFormat.Calendar = new JapaneseCalendar();
        /// String nativeName = dtfi.NativeCalendarName; // Get the Japanese name for the Japanese calendar.
        /// DateTimeFormatInfo dtfi = new CultureInfo("ja-JP", false).DateTimeFormat.Calendar = new GregorianCalendar(GregorianCalendarTypes.Localized);
        /// String nativeName = dtfi.NativeCalendarName; // Get the Japanese name for the Gregorian calendar.
        /// </summary>
        public string NativeCalendarName => _cultureData.CalendarName(Calendar.ID);

        /// <summary>
        /// Used by custom cultures and others to set the list of available formats. Note that none of them are
        /// explicitly used unless someone calls GetAllDateTimePatterns and subsequently uses one of the items
        /// from the list.
        ///
        /// Most of the format characters that can be used in GetAllDateTimePatterns are
        /// not really needed since they are one of the following:
        ///
        ///  r/R/s/u     locale-independent constants -- cannot be changed!
        ///  m/M/y/Y     fields with a single string in them -- that can be set through props directly
        ///  f/F/g/G/U   derived fields based on combinations of various of the below formats
        ///
        /// NOTE: No special validation is done here beyond what is done when the actual respective fields
        /// are used (what would be the point of disallowing here what we allow in the appropriate property?)
        ///
        /// WARNING: If more validation is ever done in one place, it should be done in the other.
        /// </summary>
        public void SetAllDateTimePatterns(string[] patterns, char format)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
            }
            if (patterns == null)
            {
                throw new ArgumentNullException(nameof(patterns));
            }

            if (patterns.Length == 0)
            {
                throw new ArgumentException(SR.Arg_ArrayZeroError, nameof(patterns));
            }

            for (int i = 0; i < patterns.Length; i++)
            {
                if (patterns[i] == null)
                {
                    throw new ArgumentNullException(nameof(patterns) + "[" + i + "]", SR.ArgumentNull_ArrayValue);
                }
            }

            // Remember the patterns, and use the 1st as default
            switch (format)
            {
                case 'd':
                    allShortDatePatterns = patterns;
                    shortDatePattern = allShortDatePatterns[0];
                    OnShortDatePatternChanged();
                    break;

                case 'D':
                    allLongDatePatterns = patterns;
                    longDatePattern = allLongDatePatterns[0];
                    OnLongDatePatternChanged();
                    break;

                case 't':
                    allShortTimePatterns = patterns;
                    shortTimePattern = allShortTimePatterns[0];
                    OnShortTimePatternChanged();
                    break;

                case 'T':
                    allLongTimePatterns = patterns;
                    longTimePattern = allLongTimePatterns[0];
                    OnLongTimePatternChanged();
                    break;

                case 'y':
                case 'Y':
                    allYearMonthPatterns = patterns;
                    yearMonthPattern = allYearMonthPatterns[0];
                    OnYearMonthPatternChanged();
                    break;

                default:
                    throw new ArgumentException(SR.Format(SR.Format_BadFormatSpecifier, format), nameof(format));
            }
        }

        public string[] AbbreviatedMonthGenitiveNames
        {
            get => (string[])InternalGetGenitiveMonthNames(true).Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), nameof(value));
                }

                CheckNullValue(value, value.Length - 1);
                ClearTokenHashTable();
                m_genitiveAbbreviatedMonthNames = value;
            }
        }

        public string[] MonthGenitiveNames
        {
            get => (string[])InternalGetGenitiveMonthNames(false).Clone();
            set
            {
                if (IsReadOnly)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_ReadOnly);
                }
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                if (value.Length != 13)
                {
                    throw new ArgumentException(SR.Format(SR.Argument_InvalidArrayLength, 13), nameof(value));
                }

                CheckNullValue(value, value.Length - 1);
                genitiveMonthNames = value;
                ClearTokenHashTable();
            }
        }

        // Decimal separator used by positive TimeSpan pattern
        private string? _decimalSeparator;
        internal string DecimalSeparator =>
            _decimalSeparator ??=
            new NumberFormatInfo(_cultureData.UseUserOverride ? CultureData.GetCultureData(_cultureData.CultureName, false) : _cultureData).NumberDecimalSeparator;

        // Positive TimeSpan Pattern
        private string? _fullTimeSpanPositivePattern;
        internal string FullTimeSpanPositivePattern =>
            _fullTimeSpanPositivePattern ??= "d':'h':'mm':'ss'" + DecimalSeparator + "'FFFFFFF";

        // Negative TimeSpan Pattern
        private string? _fullTimeSpanNegativePattern;
        internal string FullTimeSpanNegativePattern =>
            _fullTimeSpanNegativePattern ??= "'-'" + FullTimeSpanPositivePattern;

        // Get suitable CompareInfo from current DTFI object.
        internal CompareInfo CompareInfo =>
            // We use the regular GetCompareInfo here to make sure the created CompareInfo object is stored in the
            // CompareInfo cache. otherwise we would just create CompareInfo using _cultureData.
            _compareInfo ??= CompareInfo.GetCompareInfo(_cultureData.SortName);

        internal const DateTimeStyles InvalidDateTimeStyles = ~(DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite
                                                               | DateTimeStyles.AllowInnerWhite | DateTimeStyles.NoCurrentDateDefault
                                                               | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal
                                                               | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind);

        internal static void ValidateStyles(DateTimeStyles style, bool styles = false)
        {
            const DateTimeStyles localUniversal = DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal;

            if ((style & InvalidDateTimeStyles) != 0 ||
                (style & localUniversal) == localUniversal ||
                (style & (DateTimeStyles.RoundtripKind | localUniversal | DateTimeStyles.AdjustToUniversal)) > DateTimeStyles.RoundtripKind)
            {
                ThrowInvalid(style, styles);
            }

            static void ThrowInvalid(DateTimeStyles style, bool styles)
            {
                string message = (style & InvalidDateTimeStyles) != 0 ? SR.Argument_InvalidDateTimeStyles
                    : (style & localUniversal) == localUniversal ? SR.Argument_ConflictingDateTimeStyles
                    : SR.Argument_ConflictingDateTimeRoundtripStyles;
                throw new ArgumentException(message, styles ? nameof(styles) : nameof(style));
            }
        }

        /// <summary>
        /// Return the internal flag used in formatting and parsing.
        /// The flag can be used to indicate things like if genitive forms is used in
        /// this DTFi, or if leap year gets different month names.
        /// </summary>
        internal DateTimeFormatFlags FormatFlags => formatFlags != DateTimeFormatFlags.NotInitialized ? formatFlags : InitializeFormatFlags();

        [MethodImpl(MethodImplOptions.NoInlining)]
        private DateTimeFormatFlags InitializeFormatFlags()
        {
            // Build the format flags from the data in this DTFI
            formatFlags =
                (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagGenitiveMonth(
                    MonthNames, InternalGetGenitiveMonthNames(false), AbbreviatedMonthNames, InternalGetGenitiveMonthNames(true)) |
                (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInMonthNames(
                    MonthNames, InternalGetGenitiveMonthNames(false), AbbreviatedMonthNames, InternalGetGenitiveMonthNames(true)) |
                (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInDayNames(DayNames, AbbreviatedDayNames) |
                (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseHebrewCalendar((int)Calendar.ID);
            return formatFlags;
        }

        internal bool HasForceTwoDigitYears
        {
            get
            {
                switch (calendar.ID)
                {
                    // Handle Japanese and Taiwan cases.
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

        /// <summary>
        /// Returns whether the YearMonthAdjustment function has any fix-up work to do for this culture/calendar.
        /// </summary>
        internal bool HasYearMonthAdjustment => (FormatFlags & DateTimeFormatFlags.UseHebrewRule) != 0;

        /// <summary>
        /// This is a callback that the parser can make back into the DTFI to let it fiddle with special
        /// cases associated with that culture or calendar. Currently this only has special cases for
        /// the Hebrew calendar, but this could be extended to other cultures.
        ///
        /// The return value is whether the year and month are actually valid for this calendar.
        /// </summary>
        internal bool YearMonthAdjustment(ref int year, ref int month, bool parsedMonthName)
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

        // DateTimeFormatInfo tokenizer.  This is used by DateTime.Parse() to break input string into tokens.
        private TokenHashValue[]? _dtfiTokenHash;

        private const int TOKEN_HASH_SIZE = 199;
        private const int SECOND_PRIME = 197;
        private const string dateSeparatorOrTimeZoneOffset = "-";
        private const string invariantDateSeparator = "/";
        private const string invariantTimeSeparator = ":";

        // Common Ignorable Symbols
        internal const string IgnorablePeriod = ".";
        internal const string IgnorableComma = ",";

        // Year/Month/Day suffixes
        internal const string CJKYearSuff = "\u5e74";
        internal const string CJKMonthSuff = "\u6708";
        internal const string CJKDaySuff = "\u65e5";

        internal const string KoreanYearSuff = "\ub144";
        internal const string KoreanMonthSuff = "\uc6d4";
        internal const string KoreanDaySuff = "\uc77c";

        internal const string KoreanHourSuff = "\uc2dc";
        internal const string KoreanMinuteSuff = "\ubd84";
        internal const string KoreanSecondSuff = "\ucd08";

        internal const string CJKHourSuff = "\u6642";
        internal const string ChineseHourSuff = "\u65f6";

        internal const string CJKMinuteSuff = "\u5206";
        internal const string CJKSecondSuff = "\u79d2";

        internal const string JapaneseEraStart = "\u5143";

        internal const string LocalTimeMark = "T";

        internal const string GMTName = "GMT";
        internal const string ZuluName = "Z";

        internal const string KoreanLangName = "ko";
        internal const string JapaneseLangName = "ja";
        internal const string EnglishLangName = "en";

        private static volatile DateTimeFormatInfo? s_jajpDTFI;
        private static volatile DateTimeFormatInfo? s_zhtwDTFI;

        /// <summary>
        /// Create a Japanese DTFI which uses JapaneseCalendar.  This is used to parse
        /// date string with Japanese era name correctly even when the supplied DTFI
        /// does not use Japanese calendar.
        /// The created instance is stored in global s_jajpDTFI.
        /// </summary>
        internal static DateTimeFormatInfo GetJapaneseCalendarDTFI()
        {
            DateTimeFormatInfo? temp = s_jajpDTFI;
            if (temp == null)
            {
                temp = new CultureInfo("ja-JP", false).DateTimeFormat;
                temp.Calendar = JapaneseCalendar.GetDefaultInstance();
                s_jajpDTFI = temp;
            }
            return temp;
        }

        /// <summary>
        /// Create a Taiwan DTFI which uses TaiwanCalendar.  This is used to parse
        /// date string with era name correctly even when the supplied DTFI
        /// does not use Taiwan calendar.
        /// The created instance is stored in global s_zhtwDTFI.
        /// </summary>
        internal static DateTimeFormatInfo GetTaiwanCalendarDTFI()
        {
            DateTimeFormatInfo? temp = s_zhtwDTFI;
            if (temp == null)
            {
                temp = new CultureInfo("zh-TW", false).DateTimeFormat;
                temp.Calendar = TaiwanCalendar.GetDefaultInstance();
                s_zhtwDTFI = temp;
            }
            return temp;
        }

        /// <summary>
        /// DTFI properties should call this when the setter are called.
        /// </summary>
        private void ClearTokenHashTable()
        {
            _dtfiTokenHash = null;
            formatFlags = DateTimeFormatFlags.NotInitialized;
        }

        internal TokenHashValue[] CreateTokenHashTable()
        {
            TokenHashValue[]? temp = _dtfiTokenHash;
            if (temp == null)
            {
                temp = new TokenHashValue[TOKEN_HASH_SIZE];

                string sep = TimeSeparator.Trim();
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
                    InsertHash(temp, TimeSeparator, TokenType.SEP_Time, 0);
                }

                if (_name == "fr-CA")
                {
                    InsertHash(temp, " h", TokenType.SEP_HourSuff, 0);
                    InsertHash(temp, " h ", TokenType.SEP_HourSuff, 0);
                    InsertHash(temp, " min", TokenType.SEP_MinuteSuff, 0);
                    InsertHash(temp, " min ", TokenType.SEP_MinuteSuff, 0);
                    InsertHash(temp, " s", TokenType.SEP_SecondSuff, 0);
                    InsertHash(temp, " s ", TokenType.SEP_SecondSuff, 0);
                }

                InsertHash(temp, AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                InsertHash(temp, PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);

                if (LanguageName.Equals("sq"))
                {
                    // Albanian allows time formats like "12:00.PD"
                    InsertHash(temp, IgnorablePeriod + AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                    InsertHash(temp, IgnorablePeriod + PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);
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

                if (!LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing && Calendar.ID == CalendarId.JAPAN)
                {
                    // We need to support parsing the dates has the start of era symbol which means it is year 1 in the era.
                    // The start of era symbol has to be followed by the year symbol suffix, otherwise it would be invalid date.
                    InsertHash(temp, JapaneseEraStart, TokenType.YearNumberToken, 1);
                    InsertHash(temp, "(", TokenType.IgnorableSymbol, 0);
                    InsertHash(temp, ")", TokenType.IgnorableSymbol, 0);
                }

                // TODO: This ignores other custom cultures that might want to do something similar
                if (LanguageName.Equals(KoreanLangName))
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

                // We need to rescan the date words since we're always synthetic
                DateTimeFormatInfoScanner scanner = new DateTimeFormatInfoScanner();
                string[]? dateWords = scanner.GetDateWordsOfDTFI(this);

                // Ensure the formatflags is initialized.
                _ = FormatFlags;

                // For some cultures, the date separator works more like a comma, being allowed before or after any date part.
                // In these cultures, we do not use normal date separator since we disallow date separator after a date terminal state.
                // This is determined in DateTimeFormatInfoScanner.  Use this flag to determine if we should treat date separator as ignorable symbol.
                bool useDateSepAsIgnorableSymbol = false;

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
                                ReadOnlySpan<char> monthPostfix = dateWords[i].AsSpan(1);
                                // Add the month name + postfix into the token.
                                AddMonthNames(temp, monthPostfix);
                                break;
                            case DateTimeFormatInfoScanner.IgnorableSymbolChar:
                                string symbol = dateWords[i].Substring(1);
                                InsertHash(temp, symbol, TokenType.IgnorableSymbol, 0);
                                if (DateSeparator.Trim().Equals(symbol))
                                {
                                    // The date separator is the same as the ignorable symbol.
                                    useDateSepAsIgnorableSymbol = true;
                                }
                                break;
                            default:
                                InsertHash(temp, dateWords[i], TokenType.DateWordToken, 0);
                                // TODO: This ignores similar custom cultures
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
                    InsertHash(temp, DateSeparator, TokenType.SEP_Date, 0);
                }
                // Add the regular month names.
                AddMonthNames(temp);

                // Add the abbreviated month names.
                for (int i = 1; i <= 13; i++)
                {
                    InsertHash(temp, GetAbbreviatedMonthName(i), TokenType.MonthToken, i);
                }

                if ((FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != 0)
                {
                    string [] genitiveMonthNames = InternalGetGenitiveMonthNames(abbreviated: false);
                    string [] abbreviatedGenitiveMonthNames = InternalGetGenitiveMonthNames(abbreviated: true);

                    for (int i = 1; i <= 13; i++)
                    {
                        InsertHash(temp, genitiveMonthNames[i - 1], TokenType.MonthToken, i);
                        InsertHash(temp, abbreviatedGenitiveMonthNames[i - 1], TokenType.MonthToken, i);
                    }
                }

                if ((FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != 0)
                {
                    for (int i = 1; i <= 13; i++)
                    {
                        string str = InternalGetMonthName(i, MonthNameStyles.LeapYear, false);
                        InsertHash(temp, str, TokenType.MonthToken, i);
                    }
                }

                for (int i = 0; i < 7; i++)
                {
                    // We have to call public methods here to work with inherited DTFI.
                    string str = GetDayName((DayOfWeek)i);
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

                if (!GlobalizationMode.Invariant)
                {
                    if (LanguageName.Equals(JapaneseLangName))
                    {
                        // Japanese allows day of week forms like: "(Tue)"
                        for (int i = 0; i < 7; i++)
                        {
                            string specialDayOfWeek = "(" + GetAbbreviatedDayName((DayOfWeek)i) + ")";
                            InsertHash(temp, specialDayOfWeek, TokenType.DayOfWeekToken, i);
                        }
                        if (Calendar.GetType() != typeof(JapaneseCalendar))
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
                    // TODO: This prohibits similar custom cultures, but we hard coded the name
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
                }

                InsertHash(temp, InvariantInfo.AMDesignator, TokenType.SEP_Am | TokenType.Am, 0);
                InsertHash(temp, InvariantInfo.PMDesignator, TokenType.SEP_Pm | TokenType.Pm, 1);

                // Add invariant month names and day names.
                for (int i = 1; i <= 12; i++)
                {
                    // We have to call public methods here to work with inherited DTFI.
                    // Insert the month name first, so that they are at the front of abbreviated
                    // month names.
                    string str = InvariantInfo.GetMonthName(i);
                    InsertHash(temp, str, TokenType.MonthToken, i);
                    str = InvariantInfo.GetAbbreviatedMonthName(i);
                    InsertHash(temp, str, TokenType.MonthToken, i);
                }

                for (int i = 0; i < 7; i++)
                {
                    // We have to call public methods here to work with inherited DTFI.
                    string str = InvariantInfo.GetDayName((DayOfWeek)i);
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

                _dtfiTokenHash = temp;
            }
            return temp;
        }

        private void AddMonthNames(TokenHashValue[] temp, ReadOnlySpan<char> monthPostfix = default)
        {
            for (int i = 1; i <= 13; i++)
            {
                // We have to call public methods here to work with inherited DTFI.
                // Insert the month name first, so that they are at the front of abbreviated
                // month names.
                string str = GetMonthName(i);
                if (str.Length > 0)
                {
                    if (!monthPostfix.IsEmpty)
                    {
                        // Insert the month name with the postfix first, so it can be matched first.
                        InsertHash(temp, string.Concat(str, monthPostfix), TokenType.MonthToken, i);
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

        /// <summary>
        /// Try to parse the current word to see if it is a Hebrew number.
        /// Tokens will be updated accordingly.
        /// This is called by the Lexer of DateTime.Parse().
        ///
        /// Unlike most of the functions in this class, the return value indicates
        /// whether or not it started to parse. The badFormat parameter indicates
        /// if parsing began, but the format was bad.
        /// </summary>
        private static bool TryParseHebrewNumber(
            ref __DTString str,
            out bool badFormat,
            out int number)
        {
            number = -1;
            badFormat = false;

            int i = str.Index;
            if (!HebrewNumber.IsDigit(str.Value[i]))
            {
                // If the current character is not a Hebrew digit, just return false.
                // There is no chance that we can parse a valid Hebrew number from here.
                return false;
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
                        return false;
                }
            } while (i < str.Value.Length && (state != HebrewNumberParsingState.FoundEndOfHebrewNumber));

            // When we are here, we are either at the end of the string, or we find a valid Hebrew number.
            Debug.Assert(state == HebrewNumberParsingState.ContinueParsing || state == HebrewNumberParsingState.FoundEndOfHebrewNumber,
                "Invalid returned state from HebrewNumber.ParseByChar()");

            if (state != HebrewNumberParsingState.FoundEndOfHebrewNumber)
            {
                // We reach end of the string but we can't find a terminal state in parsing Hebrew number.
                return false;
            }

            // We have found a valid Hebrew number.  Update the index.
            str.Advance(i - str.Index);

            // Get the final Hebrew number value from the HebrewNumberParsingContext.
            number = context.result;

            return true;
        }

        private static bool IsHebrewChar(char ch)
        {
            return ch >= '\x0590' && ch <= '\x05ff';
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsAllowedJapaneseTokenFollowedByNonSpaceLetter(string tokenString, char nextCh)
        {
            // Allow the parser to recognize the case when having some date part followed by JapaneseEraStart "\u5143"
            // without spaces in between. e.g. Era name followed by \u5143 in the date formats ggy.
            // Also, allow recognizing the year suffix symbol "\u5e74" followed the JapaneseEraStart "\u5143"
            if (!LocalAppContextSwitches.EnforceLegacyJapaneseDateParsing && Calendar.ID == CalendarId.JAPAN &&
                (
                    // something like ggy, era followed by year and the year is specified using the JapaneseEraStart "\u5143"
                    nextCh == JapaneseEraStart[0] ||
                    // JapaneseEraStart followed by year suffix "\u5143"
                    (tokenString == JapaneseEraStart && nextCh == CJKYearSuff[0])
                ))
            {
                return true;
            }
            return false;
        }

        internal bool Tokenize(TokenType TokenMask, out TokenType tokenType, out int tokenValue,
                               ref __DTString str)
        {
            tokenType = TokenType.UnknownToken;
            tokenValue = 0;

            TokenHashValue value;
            Debug.Assert(str.Index < str.Value.Length, "DateTimeFormatInfo.Tokenize(): start < value.Length");

            char ch = str.m_current;
            bool isLetter = char.IsLetter(ch);
            if (isLetter)
            {
                ch = Culture.TextInfo.ToLower(ch);
                if (!GlobalizationMode.Invariant && IsHebrewChar(ch) && TokenMask == TokenType.RegularTokenMask)
                {
                    if (TryParseHebrewNumber(ref str, out bool badFormat, out tokenValue))
                    {
                        if (badFormat)
                        {
                            tokenType = TokenType.UnknownToken;
                            return false;
                        }
                        // This is a Hebrew number.
                        // Do nothing here.  TryParseHebrewNumber() will update token accordingly.
                        tokenType = TokenType.HebrewNumber;
                        return true;
                    }
                }
            }

            int hashcode = ch % TOKEN_HASH_SIZE;
            int hashProbe = 1 + ch % SECOND_PRIME;
            int remaining = str.Length - str.Index;
            int i = 0;

            TokenHashValue[] hashTable = _dtfiTokenHash ?? CreateTokenHashTable();
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
                        if (nextCharIndex > str.Length)
                        {
                            compareStrings = false;
                        }
                        else if (nextCharIndex < str.Length)
                        {
                            // Check word boundary. The next character should NOT be a letter.
                            char nextCh = str.Value[nextCharIndex];
                            compareStrings = !char.IsLetter(nextCh) || IsAllowedJapaneseTokenFollowedByNonSpaceLetter(value.tokenString, nextCh);
                        }
                    }

                    if (compareStrings &&
                        ((value.tokenString.Length == 1 && str.Value[str.Index] == value.tokenString[0]) ||
                         Culture.CompareInfo.Compare(str.Value.Slice(str.Index, value.tokenString.Length), value.tokenString, CompareOptions.IgnoreCase) == 0))
                    {
                        tokenType = value.tokenType & TokenMask;
                        tokenValue = value.tokenValue;
                        str.Advance(value.tokenString.Length);
                        return true;
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
                            return true;
                        }
                    }
                }
                i++;
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE) hashcode -= TOKEN_HASH_SIZE;
            } while (i < TOKEN_HASH_SIZE);

            return false;
        }

        private void InsertAtCurrentHashNode(TokenHashValue[] hashTable, string str, char ch, TokenType tokenType, int tokenValue, int pos, int hashcode, int hashProbe)
        {
            // Remember the current slot.
            TokenHashValue previousNode = hashTable[hashcode];

            // Insert the new node into the current slot.
            hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue);

            while (++pos < TOKEN_HASH_SIZE)
            {
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE) hashcode -= TOKEN_HASH_SIZE;
                // Remember this slot
                TokenHashValue temp = hashTable[hashcode];

                if (temp != null && Culture.TextInfo.ToLower(temp.tokenString[0]) != ch)
                {
                    continue;
                }
                // Put the previous slot into this slot.
                hashTable[hashcode] = previousNode;
                if (temp == null)
                {
                    // Done
                    return;
                }
                previousNode = temp;
            }
            Debug.Fail("The hashtable is full.  This should not happen.");
        }

        private void InsertHash(TokenHashValue[] hashTable, string str, TokenType tokenType, int tokenValue)
        {
            // The month of the 13th month is allowed to be null, so make sure that we ignore null value here.
            if (string.IsNullOrEmpty(str))
            {
                return;
            }

            TokenHashValue value;
            int i = 0;
            // If there is whitespace characters in the beginning and end of the string, trim them since whitespaces are skipped by
            // DateTime.Parse().
            if (char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[^1]))
            {
                str = str.Trim();   // Trim white space characters.
                // Could have space for separators
                if (str.Length == 0)
                {
                    return;
                }
            }

            char ch = Culture.TextInfo.ToLower(str[0]);
            int hashcode = ch % TOKEN_HASH_SIZE;
            int hashProbe = 1 + ch % SECOND_PRIME;
            do
            {
                value = hashTable[hashcode];
                if (value == null)
                {
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
                        if (CompareStringIgnoreCaseOptimized(str, 0, value.tokenString.Length, value.tokenString, 0, value.tokenString.Length))
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

                                // Check the current value of RegularToken (stored in the lower 8-bit of tokenType) , and insert the tokenType into the hash ONLY when we don't have a RegularToken yet.
                                // Also check the current value of SeparatorToken (stored in the upper 8-bit of token), and insert the tokenType into the hash ONLY when we don't have the SeparatorToken yet.

                                int nTokenType = (int)tokenType;
                                int nCurrentTokenTypeInHash = (int)value.tokenType;

                                // The following is the fix for the issue of throwing FormatException when "mar" is passed in string of the short date format dd/MMM/yyyy for es-MX
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
                i++;
                hashcode += hashProbe;
                if (hashcode >= TOKEN_HASH_SIZE) hashcode -= TOKEN_HASH_SIZE;
            } while (i < TOKEN_HASH_SIZE);
            Debug.Fail("The hashtable is full.  This should not happen.");
        }

        private bool CompareStringIgnoreCaseOptimized(string string1, int offset1, int length1, string string2, int offset2, int length2)
        {
            // Optimize for one character cases which are common due to date and time separators (/ and :)
            if (length1 == 1 && length2 == 1 && string1[offset1] == string2[offset2])
            {
                return true;
            }

            return Culture.CompareInfo.Compare(string1, offset1, length1, string2, offset2, length2, CompareOptions.IgnoreCase) == 0;
        }

        internal sealed class TokenHashValue
        {
            internal string tokenString;
            internal TokenType tokenType;
            internal int tokenValue;

            internal TokenHashValue(string tokenString, TokenType tokenType, int tokenValue)
            {
                this.tokenString = tokenString;
                this.tokenType = tokenType;
                this.tokenValue = tokenValue;
            }
        }
    }
}
