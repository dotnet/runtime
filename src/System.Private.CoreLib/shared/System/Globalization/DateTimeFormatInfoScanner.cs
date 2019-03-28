// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////
//
// DateTimeFormatInfoScanner
//
//  Scan a specified DateTimeFormatInfo to search for data used in DateTime.Parse()
//
//  The data includes:
//
//      DateWords: such as "de" used in es-ES (Spanish) LongDatePattern.
//      Postfix: such as "ta" used in fi-FI after the month name.
//
//  This class is shared among mscorlib.dll and sysglobl.dll.
//  Use conditional CULTURE_AND_REGIONINFO_BUILDER_ONLY to differentiate between
//  methods for mscorlib.dll and sysglobl.dll.
//
////////////////////////////////////////////////////////////////////////////

#nullable enable
using System.Collections.Generic;
using System.Text;

namespace System.Globalization
{
    //
    // from LocaleEx.txt header
    //
    //; IFORMATFLAGS
    //;       Parsing/formatting flags.
    internal enum FORMATFLAGS
    {
        None = 0x00000000,
        UseGenitiveMonth = 0x00000001,
        UseLeapYearMonth = 0x00000002,
        UseSpacesInMonthNames = 0x00000004,
        UseHebrewParsing = 0x00000008,
        UseSpacesInDayNames = 0x00000010,   // Has spaces or non-breaking space in the day names.
        UseDigitPrefixInTokens = 0x00000020,   // Has token starting with numbers.
    }

    internal enum CalendarId : ushort
    {
        UNINITIALIZED_VALUE = 0,
        GREGORIAN = 1,     // Gregorian (localized) calendar
        GREGORIAN_US = 2,     // Gregorian (U.S.) calendar
        JAPAN = 3,     // Japanese Emperor Era calendar
                       /* SSS_WARNINGS_OFF */
        TAIWAN = 4,     // Taiwan Era calendar /* SSS_WARNINGS_ON */
        KOREA = 5,     // Korean Tangun Era calendar
        HIJRI = 6,     // Hijri (Arabic Lunar) calendar
        THAI = 7,     // Thai calendar
        HEBREW = 8,     // Hebrew (Lunar) calendar
        GREGORIAN_ME_FRENCH = 9,     // Gregorian Middle East French calendar
        GREGORIAN_ARABIC = 10,     // Gregorian Arabic calendar
        GREGORIAN_XLIT_ENGLISH = 11,     // Gregorian Transliterated English calendar
        GREGORIAN_XLIT_FRENCH = 12,
        // Note that all calendars after this point are MANAGED ONLY for now.
        JULIAN = 13,
        JAPANESELUNISOLAR = 14,
        CHINESELUNISOLAR = 15,
        SAKA = 16,     // reserved to match Office but not implemented in our code
        LUNAR_ETO_CHN = 17,     // reserved to match Office but not implemented in our code
        LUNAR_ETO_KOR = 18,     // reserved to match Office but not implemented in our code
        LUNAR_ETO_ROKUYOU = 19,     // reserved to match Office but not implemented in our code
        KOREANLUNISOLAR = 20,
        TAIWANLUNISOLAR = 21,
        PERSIAN = 22,
        UMALQURA = 23,
        LAST_CALENDAR = 23      // Last calendar ID
    }

    internal class DateTimeFormatInfoScanner
    {
        // Special prefix-like flag char in DateWord array.

        // Use char in PUA area since we won't be using them in real data.
        // The char used to tell a read date word or a month postfix.  A month postfix
        // is "ta" in the long date pattern like "d. MMMM'ta 'yyyy" for fi-FI.
        // In this case, it will be stored as "\xfffeta" in the date word array.
        internal const char MonthPostfixChar = '\xe000';

        // Add ignorable symbol in a DateWord array.

        // hu-HU has:
        //      shrot date pattern: yyyy. MM. dd.;yyyy-MM-dd;yy-MM-dd
        //      long date pattern: yyyy. MMMM d.
        // Here, "." is the date separator (derived from short date pattern). However,
        // "." also appear at the end of long date pattern.  In this case, we just
        // "." as ignorable symbol so that the DateTime.Parse() state machine will not
        // treat the additional date separator at the end of y,m,d pattern as an error
        // condition.
        internal const char IgnorableSymbolChar = '\xe001';

        // Known CJK suffix
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

        // The collection fo date words & postfix.
        internal List<string> m_dateWords = new List<string>();
        // Hashtable for the known words.
        private static volatile Dictionary<string, string> s_knownWords;

        static Dictionary<string, string> KnownWords
        {
            get
            {
                if (s_knownWords == null)
                {
                    Dictionary<string, string> temp = new Dictionary<string, string>();
                    // Add known words into the hash table.

                    // Skip these special symbols.
                    temp.Add("/", string.Empty);
                    temp.Add("-", string.Empty);
                    temp.Add(".", string.Empty);
                    // Skip known CJK suffixes.
                    temp.Add(CJKYearSuff, string.Empty);
                    temp.Add(CJKMonthSuff, string.Empty);
                    temp.Add(CJKDaySuff, string.Empty);
                    temp.Add(KoreanYearSuff, string.Empty);
                    temp.Add(KoreanMonthSuff, string.Empty);
                    temp.Add(KoreanDaySuff, string.Empty);
                    temp.Add(KoreanHourSuff, string.Empty);
                    temp.Add(KoreanMinuteSuff, string.Empty);
                    temp.Add(KoreanSecondSuff, string.Empty);
                    temp.Add(CJKHourSuff, string.Empty);
                    temp.Add(ChineseHourSuff, string.Empty);
                    temp.Add(CJKMinuteSuff, string.Empty);
                    temp.Add(CJKSecondSuff, string.Empty);

                    s_knownWords = temp;
                }
                return (s_knownWords);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        //  Parameters:
        //      pattern: The pattern to be scanned.
        //      currentIndex: the current index to start the scan.
        //
        //  Returns:
        //      Return the index with the first character that is a letter, which will
        //      be the start of a date word.
        //      Note that the index can be pattern.Length if we reach the end of the string.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static int SkipWhiteSpacesAndNonLetter(string pattern, int currentIndex)
        {
            while (currentIndex < pattern.Length)
            {
                char ch = pattern[currentIndex];
                if (ch == '\\')
                {
                    // Escaped character. Look ahead one character.
                    currentIndex++;
                    if (currentIndex < pattern.Length)
                    {
                        ch = pattern[currentIndex];
                        if (ch == '\'')
                        {
                            // Skip the leading single quote.  We will
                            // stop at the first letter.
                            continue;
                        }
                        // Fall thru to check if this is a letter.
                    }
                    else
                    {
                        // End of string
                        break;
                    }
                }
                if (char.IsLetter(ch) || ch == '\'' || ch == '.')
                {
                    break;
                }
                // Skip the current char since it is not a letter.
                currentIndex++;
            }
            return (currentIndex);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // A helper to add the found date word or month postfix into ArrayList for date words.
        //
        // Parameters:
        //      formatPostfix: What kind of postfix this is.
        //          Possible values:
        //              null: This is a regular date word
        //              "MMMM": month postfix
        //      word: The date word or postfix to be added.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal void AddDateWordOrPostfix(string? formatPostfix, string str)
        {
            if (str.Length > 0)
            {
                // Some cultures use . like an abbreviation
                if (str.Equals("."))
                {
                    AddIgnorableSymbols(".");
                    return;
                }
                string words;
                if (KnownWords.TryGetValue(str, out words) == false)
                {
                    if (m_dateWords == null)
                    {
                        m_dateWords = new List<string>();
                    }
                    if (formatPostfix == "MMMM")
                    {
                        // Add the word into the ArrayList as "\xfffe" + real month postfix.
                        string temp = MonthPostfixChar + str;
                        if (!m_dateWords.Contains(temp))
                        {
                            m_dateWords.Add(temp);
                        }
                    }
                    else
                    {
                        if (!m_dateWords.Contains(str))
                        {
                            m_dateWords.Add(str);
                        }
                        if (str[str.Length - 1] == '.')
                        {
                            // Old version ignore the trailing dot in the date words. Support this as well.
                            string strWithoutDot = str.Substring(0, str.Length - 1);
                            if (!m_dateWords.Contains(strWithoutDot))
                            {
                                m_dateWords.Add(strWithoutDot);
                            }
                        }
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Scan the pattern from the specified index and add the date word/postfix
        // when appropriate.
        //
        //  Parameters:
        //      pattern: The pattern to be scanned.
        //      index: The starting index to be scanned.
        //      formatPostfix: The kind of postfix to be scanned.
        //          Possible values:
        //              null: This is a regular date word
        //              "MMMM": month postfix
        //
        //
        ////////////////////////////////////////////////////////////////////////////
        internal int AddDateWords(string pattern, int index, string? formatPostfix)
        {
            // Skip any whitespaces so we will start from a letter.
            int newIndex = SkipWhiteSpacesAndNonLetter(pattern, index);
            if (newIndex != index && formatPostfix != null)
            {
                // There are whitespaces. This will not be a postfix.
                formatPostfix = null;
            }
            index = newIndex;

            // This is the first char added into dateWord.
            // Skip all non-letter character.  We will add the first letter into DateWord.
            StringBuilder dateWord = new StringBuilder();
            // We assume that date words should start with a letter.
            // Skip anything until we see a letter.

            while (index < pattern.Length)
            {
                char ch = pattern[index];
                if (ch == '\'')
                {
                    // We have seen the end of quote.  Add the word if we do not see it before,
                    // and break the while loop.
                    AddDateWordOrPostfix(formatPostfix, dateWord.ToString());
                    index++;
                    break;
                }
                else if (ch == '\\')
                {
                    //
                    // Escaped character.  Look ahead one character
                    //

                    // Skip escaped backslash.
                    index++;
                    if (index < pattern.Length)
                    {
                        dateWord.Append(pattern[index]);
                        index++;
                    }
                }
                else if (char.IsWhiteSpace(ch))
                {
                    // Found a whitespace.  We have to add the current date word/postfix.
                    AddDateWordOrPostfix(formatPostfix, dateWord.ToString());
                    if (formatPostfix != null)
                    {
                        // Done with postfix.  The rest will be regular date word.
                        formatPostfix = null;
                    }
                    // Reset the dateWord.
                    dateWord.Length = 0;
                    index++;
                }
                else
                {
                    dateWord.Append(ch);
                    index++;
                }
            }
            return (index);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // A simple helper to find the repeat count for a specified char.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static int ScanRepeatChar(string pattern, char ch, int index, out int count)
        {
            count = 1;
            while (++index < pattern.Length && pattern[index] == ch)
            {
                count++;
            }
            // Return the updated position.
            return (index);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Add the text that is a date separator but is treated like ignroable symbol.
        // E.g.
        // hu-HU has:
        //      shrot date pattern: yyyy. MM. dd.;yyyy-MM-dd;yy-MM-dd
        //      long date pattern: yyyy. MMMM d.
        // Here, "." is the date separator (derived from short date pattern). However,
        // "." also appear at the end of long date pattern.  In this case, we just
        // "." as ignorable symbol so that the DateTime.Parse() state machine will not
        // treat the additional date separator at the end of y,m,d pattern as an error
        // condition.
        //
        ////////////////////////////////////////////////////////////////////////////

        internal void AddIgnorableSymbols(string? text)
        {
            if (m_dateWords == null)
            {
                // Create the date word array.
                m_dateWords = new List<string>();
            }
            // Add the ignorable symbol into the ArrayList.
            string temp = IgnorableSymbolChar + text;
            if (!m_dateWords.Contains(temp))
            {
                m_dateWords.Add(temp);
            }
        }


        //
        // Flag used to trace the date patterns (yy/yyyyy/M/MM/MMM/MMM/d/dd) that we have seen.
        //
        private enum FoundDatePattern
        {
            None = 0x0000,
            FoundYearPatternFlag = 0x0001,
            FoundMonthPatternFlag = 0x0002,
            FoundDayPatternFlag = 0x0004,
            FoundYMDPatternFlag = 0x0007, // FoundYearPatternFlag | FoundMonthPatternFlag | FoundDayPatternFlag;
        }

        // Check if we have found all of the year/month/day pattern.
        private FoundDatePattern _ymdFlags = FoundDatePattern.None;


        ////////////////////////////////////////////////////////////////////////////
        //
        // Given a date format pattern, scan for date word or postfix.
        //
        // A date word should be always put in a single quoted string.  And it will
        // start from a letter, so whitespace and symbols will be ignored before
        // the first letter.
        //
        // Examples of date word:
        //  'de' in es-SP: dddd, dd' de 'MMMM' de 'yyyy
        //  "\x0443." in bg-BG: dd.M.yyyy '\x0433.'
        //
        // Example of postfix:
        //  month postfix:
        //      "ta" in fi-FI: d. MMMM'ta 'yyyy
        //  Currently, only month postfix is supported.
        //
        // Usage:
        //  Always call this with Framework-style pattern, instead of Windows style pattern.
        //  Windows style pattern uses '' for single quote, while .NET uses \'
        //
        ////////////////////////////////////////////////////////////////////////////
        internal void ScanDateWord(string pattern)
        {
            // Check if we have found all of the year/month/day pattern.
            _ymdFlags = FoundDatePattern.None;

            int i = 0;
            while (i < pattern.Length)
            {
                char ch = pattern[i];
                int chCount;

                switch (ch)
                {
                    case '\'':
                        // Find a beginning quote.  Search until the end quote.
                        i = AddDateWords(pattern, i + 1, null);
                        break;
                    case 'M':
                        i = ScanRepeatChar(pattern, 'M', i, out chCount);
                        if (chCount >= 4)
                        {
                            if (i < pattern.Length && pattern[i] == '\'')
                            {
                                i = AddDateWords(pattern, i + 1, "MMMM");
                            }
                        }
                        _ymdFlags |= FoundDatePattern.FoundMonthPatternFlag;
                        break;
                    case 'y':
                        i = ScanRepeatChar(pattern, 'y', i, out chCount);
                        _ymdFlags |= FoundDatePattern.FoundYearPatternFlag;
                        break;
                    case 'd':
                        i = ScanRepeatChar(pattern, 'd', i, out chCount);
                        if (chCount <= 2)
                        {
                            // Only count "d" & "dd".
                            // ddd, dddd are day names.  Do not count them.
                            _ymdFlags |= FoundDatePattern.FoundDayPatternFlag;
                        }
                        break;
                    case '\\':
                        // Found a escaped char not in a quoted string.  Skip the current backslash
                        // and its next character.
                        i += 2;
                        break;
                    case '.':
                        if (_ymdFlags == FoundDatePattern.FoundYMDPatternFlag)
                        {
                            // If we find a dot immediately after the we have seen all of the y, m, d pattern.
                            // treat it as a ignroable symbol.  Check for comments in AddIgnorableSymbols for
                            // more details.
                            AddIgnorableSymbols(".");
                            _ymdFlags = FoundDatePattern.None;
                        }
                        i++;
                        break;
                    default:
                        if (_ymdFlags == FoundDatePattern.FoundYMDPatternFlag && !char.IsWhiteSpace(ch))
                        {
                            // We are not seeing "." after YMD. Clear the flag.
                            _ymdFlags = FoundDatePattern.None;
                        }
                        // We are not in quote.  Skip the current character.
                        i++;
                        break;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Given a DTFI, get all of the date words from date patterns and time patterns.
        //
        ////////////////////////////////////////////////////////////////////////////

        internal string[]? GetDateWordsOfDTFI(DateTimeFormatInfo dtfi)
        {
            // Enumarate all LongDatePatterns, and get the DateWords and scan for month postfix.
            string[] datePatterns = dtfi.GetAllDateTimePatterns('D');
            int i;

            // Scan the long date patterns
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            // Scan the short date patterns
            datePatterns = dtfi.GetAllDateTimePatterns('d');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }
            // Scan the YearMonth patterns.
            datePatterns = dtfi.GetAllDateTimePatterns('y');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            // Scan the month/day pattern
            ScanDateWord(dtfi.MonthDayPattern);

            // Scan the long time patterns.
            datePatterns = dtfi.GetAllDateTimePatterns('T');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            // Scan the short time patterns.
            datePatterns = dtfi.GetAllDateTimePatterns('t');
            for (i = 0; i < datePatterns.Length; i++)
            {
                ScanDateWord(datePatterns[i]);
            }

            string[]? result = null;
            if (m_dateWords != null && m_dateWords.Count > 0)
            {
                result = new string[m_dateWords.Count];
                for (i = 0; i < m_dateWords.Count; i++)
                {
                    result[i] = m_dateWords[i];
                }
            }
            return result;
        }


        ////////////////////////////////////////////////////////////////////////////
        //
        // Scan the month names to see if genitive month names are used, and return
        // the format flag.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static FORMATFLAGS GetFormatFlagGenitiveMonth(string[] monthNames, string[] genitveMonthNames, string[] abbrevMonthNames, string[] genetiveAbbrevMonthNames)
        {
            // If we have different names in regular and genitive month names, use genitive month flag.
            return ((!EqualStringArrays(monthNames, genitveMonthNames) || !EqualStringArrays(abbrevMonthNames, genetiveAbbrevMonthNames))
                ? FORMATFLAGS.UseGenitiveMonth : 0);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Scan the month names to see if spaces are used or start with a digit, and return the format flag
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static FORMATFLAGS GetFormatFlagUseSpaceInMonthNames(string[] monthNames, string[] genitveMonthNames, string[] abbrevMonthNames, string[] genetiveAbbrevMonthNames)
        {
            FORMATFLAGS formatFlags = 0;
            formatFlags |= (ArrayElementsBeginWithDigit(monthNames) ||
                    ArrayElementsBeginWithDigit(genitveMonthNames) ||
                    ArrayElementsBeginWithDigit(abbrevMonthNames) ||
                    ArrayElementsBeginWithDigit(genetiveAbbrevMonthNames)
                    ? FORMATFLAGS.UseDigitPrefixInTokens : 0);

            formatFlags |= (ArrayElementsHaveSpace(monthNames) ||
                    ArrayElementsHaveSpace(genitveMonthNames) ||
                    ArrayElementsHaveSpace(abbrevMonthNames) ||
                    ArrayElementsHaveSpace(genetiveAbbrevMonthNames)
                    ? FORMATFLAGS.UseSpacesInMonthNames : 0);
            return (formatFlags);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Scan the day names and set the correct format flag.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static FORMATFLAGS GetFormatFlagUseSpaceInDayNames(string[] dayNames, string[] abbrevDayNames)
        {
            return ((ArrayElementsHaveSpace(dayNames) ||
                    ArrayElementsHaveSpace(abbrevDayNames))
                    ? FORMATFLAGS.UseSpacesInDayNames : 0);
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Check the calendar to see if it is HebrewCalendar and set the Hebrew format flag if necessary.
        //
        ////////////////////////////////////////////////////////////////////////////
        internal static FORMATFLAGS GetFormatFlagUseHebrewCalendar(int calID)
        {
            return (calID == (int)CalendarId.HEBREW ?
                FORMATFLAGS.UseHebrewParsing | FORMATFLAGS.UseLeapYearMonth : 0);
        }


        //-----------------------------------------------------------------------------
        // EqualStringArrays
        //      compares two string arrays and return true if all elements of the first
        //      array equals to all elmentsof the second array.
        //      otherwise it returns false.
        //-----------------------------------------------------------------------------

        private static bool EqualStringArrays(string[] array1, string[] array2)
        {
            // Shortcut if they're the same array
            if (array1 == array2)
            {
                return true;
            }

            // This is effectively impossible
            if (array1.Length != array2.Length)
            {
                return false;
            }

            // Check each string
            for (int i = 0; i < array1.Length; i++)
            {
                if (array1[i] != array2[i])
                {
                    return false;
                }
            }

            return true;
        }

        //-----------------------------------------------------------------------------
        // ArrayElementsHaveSpace
        //      It checks all input array elements if any of them has space character
        //      returns true if found space character in one of the array elements.
        //      otherwise returns false.
        //-----------------------------------------------------------------------------

        private static bool ArrayElementsHaveSpace(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                // it is faster to check for space character manually instead of calling IndexOf
                // so we don't have to go to native code side.
                for (int j = 0; j < array[i].Length; j++)
                {
                    if (char.IsWhiteSpace(array[i][j]))
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        ////////////////////////////////////////////////////////////////////////////
        //
        // Check if any element of the array start with a digit.
        //
        ////////////////////////////////////////////////////////////////////////////
        private static bool ArrayElementsBeginWithDigit(string[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                // it is faster to check for space character manually instead of calling IndexOf
                // so we don't have to go to native code side.
                if (array[i].Length > 0 &&
                   array[i][0] >= '0' && array[i][0] <= '9')
                {
                    int index = 1;
                    while (index < array[i].Length && array[i][index] >= '0' && array[i][index] <= '9')
                    {
                        // Skip other digits.
                        index++;
                    }
                    if (index == array[i].Length)
                    {
                        return (false);
                    }

                    if (index == array[i].Length - 1)
                    {
                        // Skip known CJK month suffix.
                        // CJK uses month name like "1\x6708", since \x6708 is a known month suffix,
                        // we don't need the UseDigitPrefixInTokens since it is slower.
                        switch (array[i][index])
                        {
                            case '\x6708': // CJKMonthSuff
                            case '\xc6d4': // KoreanMonthSuff
                                return (false);
                        }
                    }

                    if (index == array[i].Length - 4)
                    {
                        // Skip known CJK month suffix.
                        // Starting with Windows 8, the CJK months for some cultures looks like: "1' \x6708'"
                        // instead of just "1\x6708"
                        if (array[i][index] == '\'' && array[i][index + 1] == ' ' &&
                           array[i][index + 2] == '\x6708' && array[i][index + 3] == '\'')
                        {
                            return (false);
                        }
                    }
                    return (true);
                }
            }

            return false;
        }
    }
}
