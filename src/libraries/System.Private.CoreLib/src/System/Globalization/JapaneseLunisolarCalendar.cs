// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    /// <remarks>
    /// Calendar support range:
    ///     Calendar               Minimum             Maximum
    ///     ==========             ==========          ==========
    ///     Gregorian              1960/01/28          2050/01/22
    ///     JapaneseLunisolar      1960/01/01          2049/12/29
    /// </remarks>
    public class JapaneseLunisolarCalendar : EastAsianLunisolarCalendar
    {
        public const int JapaneseEra = 1;

        private readonly GregorianCalendarHelper _helper;

        private const int MinLunisolarYear = 1960;
        private const int MaxLunisolarYear = 2049;

        private static readonly DateTime s_minDate = new DateTime(1960, 1, 28);
        private static readonly DateTime s_maxDate = new DateTime((new DateTime(2050, 1, 22, 23, 59, 59, 999)).Ticks + 9999);

        public override DateTime MinSupportedDateTime => s_minDate;

        public override DateTime MaxSupportedDateTime => s_maxDate;

        protected override int DaysInYearBeforeMinSupportedYear
        {
            get
            {
                // 1959 from ChineseLunisolarCalendar
                return 354;
            }
        }

        // Data for years 1960-2049 matches output of Calendrical Calculations [1] and published calendar tables [2].
        // [1] Reingold, Edward M, and Nachum Dershowitz. Calendrical Calculations: The Ultimate Edition. Cambridge [etc.: Cambridge University Press, 2018. Print.
        // [2] Nishizawa, Yūsō. Rekijitsu Taikan: Meiji Kaireki 1873-Nen-2100-Nen Shinkyūreki, Kanshi Kyūsei Rokuyō Taishō. Tōkyō: Shin Jinbutsu Ōraisha, 1994. Print.
        private static readonly int[,] s_yinfo =
        {
/*Y           LM  Lmon  Lday    DaysPerMonth               D1   D2   D3   D4   D5   D6   D7   D8   D9   D10  D11  D12  D13  #Days
1960     */ { 06,   01,   28,   0b1010110101010000 }, /*   30   29   30   29   30   30   29   30   29   30   29   30   29   384
1961     */ { 00,   02,   15,   0b1010101101010000 }, /*   30   29   30   29   30   29   30   30   29   30   29   30        355
1962     */ { 00,   02,   05,   0b0100101101100000 }, /*   29   30   29   29   30   29   30   30   29   30   30   29        354
1963     */ { 04,   01,   25,   0b1010010101110000 }, /*   30   29   30   29   29   30   29   30   29   30   30   30   29   384
1964     */ { 00,   02,   13,   0b1010010101110000 }, /*   30   29   30   29   29   30   29   30   29   30   30   30        355
1965     */ { 00,   02,   02,   0b0101001001110000 }, /*   29   30   29   30   29   29   30   29   29   30   30   30        354
1966     */ { 03,   01,   22,   0b0110100100110000 }, /*   29   30   30   29   30   29   29   30   29   29   30   30   29   383
1967     */ { 00,   02,   09,   0b1101100101010000 }, /*   30   30   29   30   30   29   29   30   29   30   29   30        355
1968     */ { 07,   01,   30,   0b0110101010101000 }, /*   29   30   30   29   30   29   30   29   30   29   30   29   30   384
1969     */ { 00,   02,   17,   0b0101011010100000 }, /*   29   30   29   30   29   30   30   29   30   29   30   29        354
1970     */ { 00,   02,   06,   0b1001101011010000 }, /*   30   29   29   30   30   29   30   29   30   30   29   30        355
1971     */ { 05,   01,   27,   0b0100101011101000 }, /*   29   30   29   29   30   29   30   29   30   30   30   29   30   384
1972     */ { 00,   02,   15,   0b0100101011100000 }, /*   29   30   29   29   30   29   30   29   30   30   30   29        354
1973     */ { 00,   02,   03,   0b1010010011100000 }, /*   30   29   30   29   29   30   29   29   30   30   30   29        354
1974     */ { 04,   01,   23,   0b1101001001101000 }, /*   30   30   29   30   29   29   30   29   29   30   30   29   30   384
1975     */ { 00,   02,   11,   0b1101001001010000 }, /*   30   30   29   30   29   29   30   29   29   30   29   30        354
1976     */ { 08,   01,   31,   0b1101010101001000 }, /*   30   30   29   30   29   30   29   30   29   30   29   29   30   384
1977     */ { 00,   02,   18,   0b1011010101000000 }, /*   30   29   30   30   29   30   29   30   29   30   29   29        354
1978     */ { 00,   02,   07,   0b1101011010100000 }, /*   30   30   29   30   29   30   30   29   30   29   30   29        355
1979     */ { 06,   01,   28,   0b1001011011010000 }, /*   30   29   29   30   29   30   30   29   30   30   29   30   29   384
1980     */ { 00,   02,   16,   0b1001010110110000 }, /*   30   29   29   30   29   30   29   30   30   29   30   30        355
1981     */ { 00,   02,   05,   0b0100100110110000 }, /*   29   30   29   29   30   29   29   30   30   29   30   30        354
1982     */ { 04,   01,   25,   0b1010010011011000 }, /*   30   29   30   29   29   30   29   29   30   30   29   30   30   384
1983     */ { 00,   02,   13,   0b1010010010110000 }, /*   30   29   30   29   29   30   29   29   30   29   30   30        354
1984     */ { 10,   02,   02,   0b1011001001011000 }, /*   30   29   30   30   29   29   30   29   29   30   29   30   30   384
1985     */ { 00,   02,   20,   0b0110101001010000 }, /*   29   30   30   29   30   29   30   29   29   30   29   30        354
1986     */ { 00,   02,   09,   0b0110110101000000 }, /*   29   30   30   29   30   30   29   30   29   30   29   29        354
1987     */ { 06,   01,   29,   0b1011010110101000 }, /*   30   29   30   30   29   30   29   30   30   29   30   29   30   385
1988     */ { 00,   02,   18,   0b0010101101100000 }, /*   29   29   30   29   30   29   30   30   29   30   30   29        354
1989     */ { 00,   02,   06,   0b1001010110110000 }, /*   30   29   29   30   29   30   29   30   30   29   30   30        355
1990     */ { 05,   01,   27,   0b0100100110111000 }, /*   29   30   29   29   30   29   29   30   30   29   30   30   30   384
1991     */ { 00,   02,   15,   0b0100100101110000 }, /*   29   30   29   29   30   29   29   30   29   30   30   30        354
1992     */ { 00,   02,   04,   0b0110010010110000 }, /*   29   30   30   29   29   30   29   29   30   29   30   30        354
1993     */ { 03,   01,   23,   0b0110101001010000 }, /*   29   30   30   29   30   29   30   29   29   30   29   30   29   383
1994     */ { 00,   02,   10,   0b1110101001010000 }, /*   30   30   30   29   30   29   30   29   29   30   29   30        355
1995     */ { 08,   01,   31,   0b0110110101001000 }, /*   29   30   30   29   30   30   29   30   29   30   29   29   30   384
1996     */ { 00,   02,   19,   0b0101101011010000 }, /*   29   30   29   30   30   29   30   29   30   30   29   30        355
1997     */ { 00,   02,   08,   0b0010101101100000 }, /*   29   29   30   29   30   29   30   30   29   30   30   29        354
1998     */ { 05,   01,   28,   0b1001001101110000 }, /*   30   29   29   30   29   29   30   30   29   30   30   30   29   384
1999     */ { 00,   02,   16,   0b1001001011100000 }, /*   30   29   29   30   29   29   30   29   30   30   30   29        354
2000     */ { 00,   02,   05,   0b1100100101100000 }, /*   30   30   29   29   30   29   29   30   29   30   30   29        354
2001     */ { 04,   01,   24,   0b1110010010101000 }, /*   30   30   30   29   29   30   29   29   30   29   30   29   30   384
2002     */ { 00,   02,   12,   0b1101010010100000 }, /*   30   30   29   30   29   30   29   29   30   29   30   29        354
2003     */ { 00,   02,   01,   0b1101101001010000 }, /*   30   30   29   30   30   29   30   29   29   30   29   30        355
2004     */ { 02,   01,   22,   0b0101101010101000 }, /*   29   30   29   30   30   29   30   29   30   29   30   29   30   384
2005     */ { 00,   02,   09,   0b0101011011000000 }, /*   29   30   29   30   29   30   30   29   30   30   29   29        354
2006     */ { 07,   01,   29,   0b1010101011011000 }, /*   30   29   30   29   30   29   30   29   30   30   29   30   30   385
2007     */ { 00,   02,   18,   0b0010010111010000 }, /*   29   29   30   29   29   30   29   30   30   30   29   30        354
2008     */ { 00,   02,   07,   0b1001001011010000 }, /*   30   29   29   30   29   29   30   29   30   30   29   30        354
2009     */ { 05,   01,   26,   0b1100100101011000 }, /*   30   30   29   29   30   29   29   30   29   30   29   30   30   384
2010     */ { 00,   02,   14,   0b1010100101010000 }, /*   30   29   30   29   30   29   29   30   29   30   29   30        354
2011     */ { 00,   02,   03,   0b1011010010100000 }, /*   30   29   30   30   29   30   29   29   30   29   30   29        354
2012     */ { 03,   01,   23,   0b1011101001010000 }, /*   30   29   30   30   30   29   30   29   29   30   29   30   29   384
2013     */ { 00,   02,   10,   0b1011010101010000 }, /*   30   29   30   30   29   30   29   30   29   30   29   30        355
2014     */ { 09,   01,   31,   0b0101010110101000 }, /*   29   30   29   30   29   30   29   30   30   29   30   29   30   384
2015     */ { 00,   02,   19,   0b0100101110100000 }, /*   29   30   29   29   30   29   30   30   30   29   30   29        354
2016     */ { 00,   02,   08,   0b1010010110110000 }, /*   30   29   30   29   29   30   29   30   30   29   30   30        355
2017     */ { 05,   01,   28,   0b0101001010111000 }, /*   29   30   29   30   29   29   30   29   30   29   30   30   30   384
2018     */ { 00,   02,   16,   0b0101001010110000 }, /*   29   30   29   30   29   29   30   29   30   29   30   30        354
2019     */ { 00,   02,   05,   0b1010100101010000 }, /*   30   29   30   29   30   29   29   30   29   30   29   30        354
2020     */ { 04,   01,   25,   0b1011010010101000 }, /*   30   29   30   30   29   30   29   29   30   29   30   29   30   384
2021     */ { 00,   02,   12,   0b0110101010100000 }, /*   29   30   30   29   30   29   30   29   30   29   30   29        354
2022     */ { 00,   02,   01,   0b1010110101010000 }, /*   30   29   30   29   30   30   29   30   29   30   29   30        355
2023     */ { 02,   01,   22,   0b0101010110101000 }, /*   29   30   29   30   29   30   29   30   30   29   30   29   30   384
2024     */ { 00,   02,   10,   0b0100101101100000 }, /*   29   30   29   29   30   29   30   30   29   30   30   29        354
2025     */ { 06,   01,   29,   0b1010010101110000 }, /*   30   29   30   29   29   30   29   30   29   30   30   30   29   384
2026     */ { 00,   02,   17,   0b1010010101110000 }, /*   30   29   30   29   29   30   29   30   29   30   30   30        355
2027     */ { 00,   02,   07,   0b0101001001110000 }, /*   29   30   29   30   29   29   30   29   29   30   30   30        354
2028     */ { 05,   01,   27,   0b0110100100110000 }, /*   29   30   30   29   30   29   29   30   29   29   30   30   29   383
2029     */ { 00,   02,   13,   0b1101100100110000 }, /*   30   30   29   30   30   29   29   30   29   29   30   30        355
2030     */ { 00,   02,   03,   0b0101101010100000 }, /*   29   30   29   30   30   29   30   29   30   29   30   29        354
2031     */ { 03,   01,   23,   0b1010101101010000 }, /*   30   29   30   29   30   29   30   30   29   30   29   30   29   384
2032     */ { 00,   02,   11,   0b1001011011010000 }, /*   30   29   29   30   29   30   30   29   30   30   29   30        355
2033     */ { 11,   01,   31,   0b0100101011101000 }, /*   29   30   29   29   30   29   30   29   30   30   30   29   30   384
2034     */ { 00,   02,   19,   0b0100101011100000 }, /*   29   30   29   29   30   29   30   29   30   30   30   29        354
2035     */ { 00,   02,   08,   0b1010010011010000 }, /*   30   29   30   29   29   30   29   29   30   30   29   30        354
2036     */ { 06,   01,   28,   0b1101001001101000 }, /*   30   30   29   30   29   29   30   29   29   30   30   29   30   384
2037     */ { 00,   02,   15,   0b1101001001010000 }, /*   30   30   29   30   29   29   30   29   29   30   29   30        354
2038     */ { 00,   02,   04,   0b1101010100100000 }, /*   30   30   29   30   29   30   29   30   29   29   30   29        354
2039     */ { 05,   01,   24,   0b1101101010100000 }, /*   30   30   29   30   30   29   30   29   30   29   30   29   29   384
2040     */ { 00,   02,   12,   0b1011011010100000 }, /*   30   29   30   30   29   30   30   29   30   29   30   29        355
2041     */ { 00,   02,   01,   0b1001011011010000 }, /*   30   29   29   30   29   30   30   29   30   30   29   30        355
2042     */ { 02,   01,   22,   0b0100101011011000 }, /*   29   30   29   29   30   29   30   29   30   30   29   30   30   384
2043     */ { 00,   02,   10,   0b0100100110110000 }, /*   29   30   29   29   30   29   29   30   30   29   30   30        354
2044     */ { 07,   01,   30,   0b1010010010111000 }, /*   30   29   30   29   29   30   29   29   30   29   30   30   30   384
2045     */ { 00,   02,   17,   0b1010010010110000 }, /*   30   29   30   29   29   30   29   29   30   29   30   30        354
2046     */ { 00,   02,   06,   0b1011001001010000 }, /*   30   29   30   30   29   29   30   29   29   30   29   30        354
2047     */ { 05,   01,   26,   0b1011010100101000 }, /*   30   29   30   30   29   30   29   30   29   29   30   29   30   384
2048     */ { 00,   02,   14,   0b0110110101000000 }, /*   29   30   30   29   30   30   29   30   29   30   29   29        354
2049     */ { 00,   02,   02,   0b1010110110100000 }, /*   30   29   30   29   30   30   29   30   30   29   30   29        355
         */ };

        internal override int MinCalendarYear => MinLunisolarYear;

        internal override int MaxCalendarYear => MaxLunisolarYear;

        internal override DateTime MinDate => s_minDate;

        internal override DateTime MaxDate => s_maxDate;

        internal override EraInfo[]? CalEraInfo => JapaneseCalendar.GetEraInfo();

        internal override int GetYearInfo(int lunarYear, int index)
        {
            if (lunarYear < MinLunisolarYear || lunarYear > MaxLunisolarYear)
            {
                throw new ArgumentOutOfRangeException(
                    "year",
                    lunarYear,
                    SR.Format(SR.ArgumentOutOfRange_Range, MinLunisolarYear, MaxLunisolarYear));
            }

            return s_yinfo[lunarYear - MinLunisolarYear, index];
        }

        internal override int GetYear(int year, DateTime time)
        {
            return _helper.GetYear(year, time);
        }

        internal override int GetGregorianYear(int year, int era)
        {
            return _helper.GetGregorianYear(year, era);
        }

        /// <summary>
        /// Trim off the eras that are before our date range
        /// </summary>
        private static EraInfo[] TrimEras(EraInfo[] baseEras)
        {
            EraInfo[] newEras = new EraInfo[baseEras.Length];
            int newIndex = 0;

            // Eras have most recent first, so start with that
            for (int i = 0; i < baseEras.Length; i++)
            {
                // If this one's minimum year is bigger than our maximum year
                // then we can't use it.
                if (baseEras[i].yearOffset + baseEras[i].minEraYear >= MaxLunisolarYear)
                {
                    // skip this one.
                    continue;
                }

                // If this one's maximum era is less than our minimum era
                // then we've gotten too low in the era #s, so we're done
                if (baseEras[i].yearOffset + baseEras[i].maxEraYear < MinLunisolarYear)
                {
                    break;
                }

                // Wasn't too large or too small, can use this one
                newEras[newIndex] = baseEras[i];
                newIndex++;
            }

            // If we didn't copy any then something was wrong, just return base
            if (newIndex == 0) return baseEras;

            Array.Resize(ref newEras!, newIndex); // TODO-NULLABLE: Remove ! when nullable attributes are respected
            return newEras;
        }

        public JapaneseLunisolarCalendar()
        {
            _helper = new GregorianCalendarHelper(this, TrimEras(JapaneseCalendar.GetEraInfo()));
        }

        public override int GetEra(DateTime time) => _helper.GetEra(time);

        internal override CalendarId BaseCalendarID => CalendarId.JAPAN;

        internal override CalendarId ID => CalendarId.JAPANESELUNISOLAR;

        public override int[] Eras => _helper.Eras;
    }
}
