// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace System.Globalization
{
    /*
    **  Calendar support range:
    **      Calendar               Minimum             Maximum
    **      ==========             ==========          ==========
    **      Gregorian              1960/01/28          2050/01/22
    **      JapaneseLunisolar      1960/01/01          2049/12/29
    */

    public class JapaneseLunisolarCalendar : EastAsianLunisolarCalendar
    {
        //
        // The era value for the current era.
        //

        public const int JapaneseEra = 1;

        internal GregorianCalendarHelper helper;

        internal const int MIN_LUNISOLAR_YEAR = 1960;
        internal const int MAX_LUNISOLAR_YEAR = 2049;

        internal const int MIN_GREGORIAN_YEAR = 1960;
        internal const int MIN_GREGORIAN_MONTH = 1;
        internal const int MIN_GREGORIAN_DAY = 28;

        internal const int MAX_GREGORIAN_YEAR = 2050;
        internal const int MAX_GREGORIAN_MONTH = 1;
        internal const int MAX_GREGORIAN_DAY = 22;

        internal static DateTime minDate = new DateTime(MIN_GREGORIAN_YEAR, MIN_GREGORIAN_MONTH, MIN_GREGORIAN_DAY);
        internal static DateTime maxDate = new DateTime((new DateTime(MAX_GREGORIAN_YEAR, MAX_GREGORIAN_MONTH, MAX_GREGORIAN_DAY, 23, 59, 59, 999)).Ticks + 9999);

        public override DateTime MinSupportedDateTime
        {
            get
            {
                return (minDate);
            }
        }


        public override DateTime MaxSupportedDateTime
        {
            get
            {
                return (maxDate);
            }
        }
        protected override int DaysInYearBeforeMinSupportedYear
        {
            get
            {
                // 1959 from ChineseLunisolarCalendar
                return 354;
            }
        }

        private static readonly int[,] s_yinfo =
        {
            /*Y            LM        Lmon    Lday        DaysPerMonth    D1    D2    D3    D4    D5    D6    D7    D8    D9    D10    D11    D12    D13    #Days
            1960    */
          {    6    ,    1    ,    28    ,    44368    },/*    30    29    30    29    30    30    29    30    29    30    29    30    29    384
1961    */{    0    ,    2    ,    15    ,    43856    },/*    30    29    30    29    30    29    30    30    29    30    29    30    0    355
1962    */{    0    ,    2    ,    5     ,    19808    },/*    29    30    29    29    30    30    29    30    29    30    30    29    0    354
1963    */{    4    ,    1    ,    25    ,    42352    },/*    30    29    30    29    29    30    29    30    29    30    30    30    29    384
1964    */{    0    ,    2    ,    13    ,    42352    },/*    30    29    30    29    29    30    29    30    29    30    30    30    0    355
1965    */{    0    ,    2    ,    2     ,    21104    },/*    29    30    29    30    29    29    30    29    29    30    30    30    0    354
1966    */{    3    ,    1    ,    22    ,    26928    },/*    29    30    30    29    30    29    29    30    29    29    30    30    29    383
1967    */{    0    ,    2    ,    9     ,    55632    },/*    30    30    29    30    30    29    29    30    29    30    29    30    0    355
1968    */{    7    ,    1    ,    30    ,    27304    },/*    29    30    30    29    30    29    30    29    30    29    30    29    30    384
1969    */{    0    ,    2    ,    17    ,    22176    },/*    29    30    29    30    29    30    30    29    30    29    30    29    0    354
1970    */{    0    ,    2    ,    6     ,    39632    },/*    30    29    29    30    30    29    30    29    30    30    29    30    0    355
1971    */{    5    ,    1    ,    27    ,    19176    },/*    29    30    29    29    30    29    30    29    30    30    30    29    30    384
1972    */{    0    ,    2    ,    15    ,    19168    },/*    29    30    29    29    30    29    30    29    30    30    30    29    0    354
1973    */{    0    ,    2    ,    3     ,    42208    },/*    30    29    30    29    29    30    29    29    30    30    30    29    0    354
1974    */{    4    ,    1    ,    23    ,    53864    },/*    30    30    29    30    29    29    30    29    29    30    30    29    30    384
1975    */{    0    ,    2    ,    11    ,    53840    },/*    30    30    29    30    29    29    30    29    29    30    29    30    0    354
1976    */{    8    ,    1    ,    31    ,    54600    },/*    30    30    29    30    29    30    29    30    29    30    29    29    30    384
1977    */{    0    ,    2    ,    18    ,    46400    },/*    30    29    30    30    29    30    29    30    29    30    29    29    0    354
1978    */{    0    ,    2    ,    7     ,    54944    },/*    30    30    29    30    29    30    30    29    30    29    30    29    0    355
1979    */{    6    ,    1    ,    28    ,    38608    },/*    30    29    29    30    29    30    30    29    30    30    29    30    29    384
1980    */{    0    ,    2    ,    16    ,    38320    },/*    30    29    29    30    29    30    29    30    30    29    30    30    0    355
1981    */{    0    ,    2    ,    5     ,    18864    },/*    29    30    29    29    30    29    29    30    30    29    30    30    0    354
1982    */{    4    ,    1    ,    25    ,    42200    },/*    30    29    30    29    29    30    29    29    30    30    29    30    30    384
1983    */{    0    ,    2    ,    13    ,    42160    },/*    30    29    30    29    29    30    29    29    30    29    30    30    0    354
1984    */{    10   ,    2    ,    2     ,    45656    },/*    30    29    30    30    29    29    30    29    29    30    29    30    30    384
1985    */{    0    ,    2    ,    20    ,    27216    },/*    29    30    30    29    30    29    30    29    29    30    29    30    0    354
1986    */{    0    ,    2    ,    9     ,    27968    },/*    29    30    30    29    30    30    29    30    29    30    29    29    0    354
1987    */{    6    ,    1    ,    29    ,    46504    },/*    30    29    30    30    29    30    29    30    30    29    30    29    30    385
1988    */{    0    ,    2    ,    18    ,    11104    },/*    29    29    30    29    30    29    30    30    29    30    30    29    0    354
1989    */{    0    ,    2    ,    6     ,    38320    },/*    30    29    29    30    29    30    29    30    30    29    30    30    0    355
1990    */{    5    ,    1    ,    27    ,    18872    },/*    29    30    29    29    30    29    29    30    30    29    30    30    30    384
1991    */{    0    ,    2    ,    15    ,    18800    },/*    29    30    29    29    30    29    29    30    29    30    30    30    0    354
1992    */{    0    ,    2    ,    4     ,    25776    },/*    29    30    30    29    29    30    29    29    30    29    30    30    0    354
1993    */{    3    ,    1    ,    23    ,    27216    },/*    29    30    30    29    30    29    30    29    29    30    29    30    29    383
1994    */{    0    ,    2    ,    10    ,    59984    },/*    30    30    30    29    30    29    30    29    29    30    29    30    0    355
1995    */{    8    ,    1    ,    31    ,    27976    },/*    29    30    30    29    30    30    29    30    29    30    29    29    30    384
1996    */{    0    ,    2    ,    19    ,    23248    },/*    29    30    29    30    30    29    30    29    30    30    29    30    0    355
1997    */{    0    ,    2    ,    8     ,    11104    },/*    29    29    30    29    30    29    30    30    29    30    30    29    0    354
1998    */{    5    ,    1    ,    28    ,    37744    },/*    30    29    29    30    29    29    30    30    29    30    30    30    29    384
1999    */{    0    ,    2    ,    16    ,    37600    },/*    30    29    29    30    29    29    30    29    30    30    30    29    0    354
2000    */{    0    ,    2    ,    5     ,    51552    },/*    30    30    29    29    30    29    29    30    29    30    30    29    0    354
2001    */{    4    ,    1    ,    24    ,    58536    },/*    30    30    30    29    29    30    29    29    30    29    30    29    30    384
2002    */{    0    ,    2    ,    12    ,    54432    },/*    30    30    29    30    29    30    29    29    30    29    30    29    0    354
2003    */{    0    ,    2    ,    1     ,    55888    },/*    30    30    29    30    30    29    30    29    29    30    29    30    0    355
2004    */{    2    ,    1    ,    22    ,    23208    },/*    29    30    29    30    30    29    30    29    30    29    30    29    30    384
2005    */{    0    ,    2    ,    9     ,    22208    },/*    29    30    29    30    29    30    30    29    30    30    29    29    0    354
2006    */{    7    ,    1    ,    29    ,    43736    },/*    30    29    30    29    30    29    30    29    30    30    29    30    30    385
2007    */{    0    ,    2    ,    18    ,    9680     },/*    29    29    30    29    29    30    29    30    30    30    29    30    0    354
2008    */{    0    ,    2    ,    7     ,    37584    },/*    30    29    29    30    29    29    30    29    30    30    29    30    0    354
2009    */{    5    ,    1    ,    26    ,    51544    },/*    30    30    29    29    30    29    29    30    29    30    29    30    30    384
2010    */{    0    ,    2    ,    14    ,    43344    },/*    30    29    30    29    30    29    29    30    29    30    29    30    0    354
2011    */{    0    ,    2    ,    3     ,    46240    },/*    30    29    30    30    29    30    29    29    30    29    30    29    0    354
2012    */{    3    ,    1    ,    23    ,    47696    },/*    30    29    30    30    30    29    30    29    29    30    29    30    29    384
2013    */{    0    ,    2    ,    10    ,    46416    },/*    30    29    30    30    29    30    29    30    29    30    29    30    0    355
2014    */{    9    ,    1    ,    31    ,    21928    },/*    29    30    29    30    29    30    29    30    30    29    30    29    30    384
2015    */{    0    ,    2    ,    19    ,    19360    },/*    29    30    29    29    30    29    30    30    30    29    30    29    0    354
2016    */{    0    ,    2    ,    8     ,    42416    },/*    30    29    30    29    29    30    29    30    30    29    30    30    0    355
2017    */{    5    ,    1    ,    28    ,    21176    },/*    29    30    29    30    29    29    30    29    30    29    30    30    30    384
2018    */{    0    ,    2    ,    16    ,    21168    },/*    29    30    29    30    29    29    30    29    30    29    30    30    0    354
2019    */{    0    ,    2    ,    5     ,    43344    },/*    30    29    30    29    30    29    29    30    29    30    29    30    0    354
2020    */{    4    ,    1    ,    25    ,    46248    },/*    30    29    30    30    29    30    29    29    30    29    30    29    30    384
2021    */{    0    ,    2    ,    12    ,    27296    },/*    29    30    30    29    30    29    30    29    30    29    30    29    0    354
2022    */{    0    ,    2    ,    1     ,    44368    },/*    30    29    30    29    30    30    29    30    29    30    29    30    0    355
2023    */{    2    ,    1    ,    22    ,    21928    },/*    29    30    29    30    29    30    29    30    30    29    30    29    30    384
2024    */{    0    ,    2    ,    10    ,    19296    },/*    29    30    29    29    30    29    30    30    29    30    30    29    0    354
2025    */{    6    ,    1    ,    29    ,    42352    },/*    30    29    30    29    29    30    29    30    29    30    30    30    29    384
2026    */{    0    ,    2    ,    17    ,    42352    },/*    30    29    30    29    29    30    29    30    29    30    30    30    0    355
2027    */{    0    ,    2    ,    7     ,    21104    },/*    29    30    29    30    29    29    30    29    29    30    30    30    0    354
2028    */{    5    ,    1    ,    27    ,    26928    },/*    29    30    30    29    30    29    29    30    29    29    30    30    29    383
2029    */{    0    ,    2    ,    13    ,    55600    },/*    30    30    29    30    30    29    29    30    29    29    30    30    0    355
2030    */{    0    ,    2    ,    3     ,    23200    },/*    29    30    29    30    30    29    30    29    30    29    30    29    0    354
2031    */{    3    ,    1    ,    23    ,    43856    },/*    30    29    30    29    30    29    30    30    29    30    29    30    29    384
2032    */{    0    ,    2    ,    11    ,    38608    },/*    30    29    29    30    29    30    30    29    30    30    29    30    0    355
2033    */{    11   ,    1    ,    31    ,    19176    },/*    29    30    29    29    30    29    30    29    30    30    30    29    30    384
2034    */{    0    ,    2    ,    19    ,    19168    },/*    29    30    29    29    30    29    30    29    30    30    30    29    0    354
2035    */{    0    ,    2    ,    8     ,    42192    },/*    30    29    30    29    29    30    29    29    30    30    29    30    0    354
2036    */{    6    ,    1    ,    28    ,    53864    },/*    30    30    29    30    29    29    30    29    29    30    30    29    30    384
2037    */{    0    ,    2    ,    15    ,    53840    },/*    30    30    29    30    29    29    30    29    29    30    29    30    0    354
2038    */{    0    ,    2    ,    4     ,    54560    },/*    30    30    29    30    29    30    29    30    29    29    30    29    0    354
2039    */{    5    ,    1    ,    24    ,    55968    },/*    30    30    29    30    30    29    30    29    30    29    30    29    29    384
2040    */{    0    ,    2    ,    12    ,    46752    },/*    30    29    30    30    29    30    30    29    30    29    30    29    0    355
2041    */{    0    ,    2    ,    1     ,    38608    },/*    30    29    29    30    29    30    30    29    30    30    29    30    0    355
2042    */{    2    ,    1    ,    22    ,    19160    },/*    29    30    29    29    30    29    30    29    30    30    29    30    30    384
2043    */{    0    ,    2    ,    10    ,    18864    },/*    29    30    29    29    30    29    29    30    30    29    30    30    0    354
2044    */{    7    ,    1    ,    30    ,    42168    },/*    30    29    30    29    29    30    29    29    30    29    30    30    30    384
2045    */{    0    ,    2    ,    17    ,    42160    },/*    30    29    30    29    29    30    29    29    30    29    30    30    0    354
2046    */{    0    ,    2    ,    6     ,    45648    },/*    30    29    30    30    29    29    30    29    29    30    29    30    0    354
2047    */{    5    ,    1    ,    26    ,    46376    },/*    30    29    30    30    29    30    29    30    29    29    30    29    30    384
2048    */{    0    ,    2    ,    14    ,    27968    },/*    29    30    30    29    30    30    29    30    29    30    29    29    0    354
2049    */{    0    ,    2    ,    2     ,    44448    },/*    30    29    30    29    30    30    29    30    30    29    30    29    0    355
     */ };

        internal override int MinCalendarYear
        {
            get
            {
                return (MIN_LUNISOLAR_YEAR);
            }
        }

        internal override int MaxCalendarYear
        {
            get
            {
                return (MAX_LUNISOLAR_YEAR);
            }
        }

        internal override DateTime MinDate
        {
            get
            {
                return (minDate);
            }
        }

        internal override DateTime MaxDate
        {
            get
            {
                return (maxDate);
            }
        }

        internal override EraInfo[] CalEraInfo
        {
            get
            {
                return (JapaneseCalendar.GetEraInfo());
            }
        }

        internal override int GetYearInfo(int lunarYear, int index)
        {
            if ((lunarYear < MIN_LUNISOLAR_YEAR) || (lunarYear > MAX_LUNISOLAR_YEAR))
            {
                throw new ArgumentOutOfRangeException(
                            "year",
                            String.Format(
                                CultureInfo.CurrentCulture,
                                SR.ArgumentOutOfRange_Range,
                                MIN_LUNISOLAR_YEAR,
                                MAX_LUNISOLAR_YEAR));
            }

            return s_yinfo[lunarYear - MIN_LUNISOLAR_YEAR, index];
        }

        internal override int GetYear(int year, DateTime time)
        {
            return helper.GetYear(year, time);
        }

        internal override int GetGregorianYear(int year, int era)
        {
            return helper.GetGregorianYear(year, era);
        }

        // Trim off the eras that are before our date range
        private static EraInfo[] TrimEras(EraInfo[] baseEras)
        {
            EraInfo[] newEras = new EraInfo[baseEras.Length];
            int newIndex = 0;

            // Eras have most recent first, so start with that
            for (int i = 0; i < baseEras.Length; i++)
            {
                // If this one's minimum year is bigger than our maximum year
                // then we can't use it.
                if (baseEras[i].yearOffset + baseEras[i].minEraYear >= MAX_LUNISOLAR_YEAR)
                {
                    // skip this one.
                    continue;
                }

                // If this one's maximum era is less than our minimum era
                // then we've gotten too low in the era #s, so we're done
                if (baseEras[i].yearOffset + baseEras[i].maxEraYear < MIN_LUNISOLAR_YEAR)
                {
                    break;
                }

                // Wasn't too large or too small, can use this one
                newEras[newIndex] = baseEras[i];
                newIndex++;
            }

            // If we didn't copy any then something was wrong, just return base
            if (newIndex == 0) return baseEras;

            // Resize the output array
            Array.Resize(ref newEras, newIndex);
            return newEras;
        }


        // Construct an instance of JapaneseLunisolar calendar.
        public JapaneseLunisolarCalendar()
        {
            helper = new GregorianCalendarHelper(this, TrimEras(JapaneseCalendar.GetEraInfo()));
        }


        public override int GetEra(DateTime time)
        {
            return (helper.GetEra(time));
        }

        internal override CalendarId BaseCalendarID
        {
            get
            {
                return (CalendarId.JAPAN);
            }
        }

        internal override CalendarId ID
        {
            get
            {
                return (CalendarId.JAPANESELUNISOLAR);
            }
        }


        public override int[] Eras
        {
            get
            {
                return (helper.Eras);
            }
        }
    }
}
