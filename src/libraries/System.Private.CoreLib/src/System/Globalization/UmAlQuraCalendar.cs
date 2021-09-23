// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    /// <remarks>
    /// Calendar support range:
    ///     Calendar    Minimum     Maximum
    ///     ==========  ==========  ==========
    ///     Gregorian   1900/04/30   2077/05/13
    ///     UmAlQura    1318/01/01   1500/12/30
    /// </remarks>
    public partial class UmAlQuraCalendar : Calendar
    {
        private const int MinCalendarYear = 1318;
        private const int MaxCalendarYear = 1500;

        private struct DateMapping
        {
            internal DateMapping(int MonthsLengthFlags, int GYear, int GMonth, int GDay)
            {
                HijriMonthsLengthFlags = MonthsLengthFlags;
                GregorianDate = new DateTime(GYear, GMonth, GDay);
            }

            internal int HijriMonthsLengthFlags;
            internal DateTime GregorianDate;
        }

        private static readonly DateMapping[] s_hijriYearInfo = InitDateMapping();

        private static DateMapping[] InitDateMapping()
        {
            short[] rawData = new short[]
            {
                // These data are taken from Tables/Excel/UmAlQura.xls please make sure that the two places are in sync
                /*  DaysPerM     GY      GM     GD      D1   D2   D3   D4   D5   D6   D7   D8   D9   D10  D11  D12
                1318*/0x02EA,    1900,     4,    30, /* 0    1    0    1    0    1    1    1    0    1    0    0    4/30/1900
                1319*/0x06E9,    1901,     4,    19, /* 1    0    0    1    0    1    1    1    0    1    1    0    4/19/1901
                1320*/0x0ED2,    1902,     4,     9, /* 0    1    0    0    1    0    1    1    0    1    1    1    4/9/1902
                1321*/0x0EA4,    1903,     3,    30, /* 0    0    1    0    0    1    0    1    0    1    1    1    3/30/1903
                1322*/0x0D4A,    1904,     3,    18, /* 0    1    0    1    0    0    1    0    1    0    1    1    3/18/1904
                1323*/0x0A96,    1905,     3,     7, /* 0    1    1    0    1    0    0    1    0    1    0    1    3/7/1905
                1324*/0x0536,    1906,     2,    24, /* 0    1    1    0    1    1    0    0    1    0    1    0    2/24/1906
                1325*/0x0AB5,    1907,     2,    13, /* 1    0    1    0    1    1    0    1    0    1    0    1    2/13/1907
                1326*/0x0DAA,    1908,     2,     3, /* 0    1    0    1    0    1    0    1    1    0    1    1    2/3/1908
                1327*/0x0BA4,    1909,     1,    23, /* 0    0    1    0    0    1    0    1    1    1    0    1    1/23/1909
                1328*/0x0B49,    1910,     1,    12, /* 1    0    0    1    0    0    1    0    1    1    0    1    1/12/1910
                1329*/0x0A93,    1911,     1,     1, /* 1    1    0    0    1    0    0    1    0    1    0    1    1/1/1911
                1330*/0x052B,    1911,    12,    21, /* 1    1    0    1    0    1    0    0    1    0    1    0    12/21/1911
                1331*/0x0A57,    1912,    12,     9, /* 1    1    1    0    1    0    1    0    0    1    0    1    12/9/1912
                1332*/0x04B6,    1913,    11,    29, /* 0    1    1    0    1    1    0    1    0    0    1    0    11/29/1913
                1333*/0x0AB5,    1914,    11,    18, /* 1    0    1    0    1    1    0    1    0    1    0    1    11/18/1914
                1334*/0x05AA,    1915,    11,     8, /* 0    1    0    1    0    1    0    1    1    0    1    0    11/8/1915
                1335*/0x0D55,    1916,    10,    27, /* 1    0    1    0    1    0    1    0    1    0    1    1    10/27/1916
                1336*/0x0D2A,    1917,    10,    17, /* 0    1    0    1    0    1    0    0    1    0    1    1    10/17/1917
                1337*/0x0A56,    1918,    10,     6, /* 0    1    1    0    1    0    1    0    0    1    0    1    10/6/1918
                1338*/0x04AE,    1919,     9,    25, /* 0    1    1    1    0    1    0    1    0    0    1    0    9/25/1919
                1339*/0x095D,    1920,     9,    13, /* 1    0    1    1    1    0    1    0    1    0    0    1    9/13/1920
                1340*/0x02EC,    1921,     9,     3, /* 0    0    1    1    0    1    1    1    0    1    0    0    9/3/1921
                1341*/0x06D5,    1922,     8,    23, /* 1    0    1    0    1    0    1    1    0    1    1    0    8/23/1922
                1342*/0x06AA,    1923,     8,    13, /* 0    1    0    1    0    1    0    1    0    1    1    0    8/13/1923
                1343*/0x0555,    1924,     8,     1, /* 1    0    1    0    1    0    1    0    1    0    1    0    8/1/1924
                1344*/0x04AB,    1925,     7,    21, /* 1    1    0    1    0    1    0    1    0    0    1    0    7/21/1925
                1345*/0x095B,    1926,     7,    10, /* 1    1    0    1    1    0    1    0    1    0    0    1    7/10/1926
                1346*/0x02BA,    1927,     6,    30, /* 0    1    0    1    1    1    0    1    0    1    0    0    6/30/1927
                1347*/0x0575,    1928,     6,    18, /* 1    0    1    0    1    1    1    0    1    0    1    0    6/18/1928
                1348*/0x0BB2,    1929,     6,     8, /* 0    1    0    0    1    1    0    1    1    1    0    1    6/8/1929
                1349*/0x0764,    1930,     5,    29, /* 0    0    1    0    0    1    1    0    1    1    1    0    5/29/1930
                1350*/0x0749,    1931,     5,    18, /* 1    0    0    1    0    0    1    0    1    1    1    0    5/18/1931
                1351*/0x0655,    1932,     5,     6, /* 1    0    1    0    1    0    1    0    0    1    1    0    5/6/1932
                1352*/0x02AB,    1933,     4,    25, /* 1    1    0    1    0    1    0    1    0    1    0    0    4/25/1933
                1353*/0x055B,    1934,     4,    14, /* 1    1    0    1    1    0    1    0    1    0    1    0    4/14/1934
                1354*/0x0ADA,    1935,     4,     4, /* 0    1    0    1    1    0    1    1    0    1    0    1    4/4/1935
                1355*/0x06D4,    1936,     3,    24, /* 0    0    1    0    1    0    1    1    0    1    1    0    3/24/1936
                1356*/0x0EC9,    1937,     3,    13, /* 1    0    0    1    0    0    1    1    0    1    1    1    3/13/1937
                1357*/0x0D92,    1938,     3,     3, /* 0    1    0    0    1    0    0    1    1    0    1    1    3/3/1938
                1358*/0x0D25,    1939,     2,    20, /* 1    0    1    0    0    1    0    0    1    0    1    1    2/20/1939
                1359*/0x0A4D,    1940,     2,     9, /* 1    0    1    1    0    0    1    0    0    1    0    1    2/9/1940
                1360*/0x02AD,    1941,     1,    28, /* 1    0    1    1    0    1    0    1    0    1    0    0    1/28/1941
                1361*/0x056D,    1942,     1,    17, /* 1    0    1    1    0    1    1    0    1    0    1    0    1/17/1942
                1362*/0x0B6A,    1943,     1,     7, /* 0    1    0    1    0    1    1    0    1    1    0    1    1/7/1943
                1363*/0x0B52,    1943,    12,    28, /* 0    1    0    0    1    0    1    0    1    1    0    1    12/28/1943
                1364*/0x0AA5,    1944,    12,    16, /* 1    0    1    0    0    1    0    1    0    1    0    1    12/16/1944
                1365*/0x0A4B,    1945,    12,     5, /* 1    1    0    1    0    0    1    0    0    1    0    1    12/5/1945
                1366*/0x0497,    1946,    11,    24, /* 1    1    1    0    1    0    0    1    0    0    1    0    11/24/1946
                1367*/0x0937,    1947,    11,    13, /* 1    1    1    0    1    1    0    0    1    0    0    1    11/13/1947
                1368*/0x02B6,    1948,    11,     2, /* 0    1    1    0    1    1    0    1    0    1    0    0    11/2/1948
                1369*/0x0575,    1949,    10,    22, /* 1    0    1    0    1    1    1    0    1    0    1    0    10/22/1949
                1370*/0x0D6A,    1950,    10,    12, /* 0    1    0    1    0    1    1    0    1    0    1    1    10/12/1950
                1371*/0x0D52,    1951,    10,     2, /* 0    1    0    0    1    0    1    0    1    0    1    1    10/2/1951
                1372*/0x0A96,    1952,     9,    20, /* 0    1    1    0    1    0    0    1    0    1    0    1    9/20/1952
                1373*/0x092D,    1953,     9,     9, /* 1    0    1    1    0    1    0    0    1    0    0    1    9/9/1953
                1374*/0x025D,    1954,     8,    29, /* 1    0    1    1    1    0    1    0    0    1    0    0    8/29/1954
                1375*/0x04DD,    1955,     8,    18, /* 1    0    1    1    1    0    1    1    0    0    1    0    8/18/1955
                1376*/0x0ADA,    1956,     8,     7, /* 0    1    0    1    1    0    1    1    0    1    0    1    8/7/1956
                1377*/0x05D4,    1957,     7,    28, /* 0    0    1    0    1    0    1    1    1    0    1    0    7/28/1957
                1378*/0x0DA9,    1958,     7,    17, /* 1    0    0    1    0    1    0    1    1    0    1    1    7/17/1958
                1379*/0x0D52,    1959,     7,     7, /* 0    1    0    0    1    0    1    0    1    0    1    1    7/7/1959
                1380*/0x0AAA,    1960,     6,    25, /* 0    1    0    1    0    1    0    1    0    1    0    1    6/25/1960
                1381*/0x04D6,    1961,     6,    14, /* 0    1    1    0    1    0    1    1    0    0    1    0    6/14/1961
                1382*/0x09B6,    1962,     6,     3, /* 0    1    1    0    1    1    0    1    1    0    0    1    6/3/1962
                1383*/0x0374,    1963,     5,    24, /* 0    0    1    0    1    1    1    0    1    1    0    0    5/24/1963
                1384*/0x0769,    1964,     5,    12, /* 1    0    0    1    0    1    1    0    1    1    1    0    5/12/1964
                1385*/0x0752,    1965,     5,     2, /* 0    1    0    0    1    0    1    0    1    1    1    0    5/2/1965
                1386*/0x06A5,    1966,     4,    21, /* 1    0    1    0    0    1    0    1    0    1    1    0    4/21/1966
                1387*/0x054B,    1967,     4,    10, /* 1    1    0    1    0    0    1    0    1    0    1    0    4/10/1967
                1388*/0x0AAB,    1968,     3,    29, /* 1    1    0    1    0    1    0    1    0    1    0    1    3/29/1968
                1389*/0x055A,    1969,     3,    19, /* 0    1    0    1    1    0    1    0    1    0    1    0    3/19/1969
                1390*/0x0AD5,    1970,     3,     8, /* 1    0    1    0    1    0    1    1    0    1    0    1    3/8/1970
                1391*/0x0DD2,    1971,     2,    26, /* 0    1    0    0    1    0    1    1    1    0    1    1    2/26/1971
                1392*/0x0DA4,    1972,     2,    16, /* 0    0    1    0    0    1    0    1    1    0    1    1    2/16/1972
                1393*/0x0D49,    1973,     2,     4, /* 1    0    0    1    0    0    1    0    1    0    1    1    2/4/1973
                1394*/0x0A95,    1974,     1,    24, /* 1    0    1    0    1    0    0    1    0    1    0    1    1/24/1974
                1395*/0x052D,    1975,     1,    13, /* 1    0    1    1    0    1    0    0    1    0    1    0    1/13/1975
                1396*/0x0A5D,    1976,     1,     2, /* 1    0    1    1    1    0    1    0    0    1    0    1    1/2/1976
                1397*/0x055A,    1976,    12,    22, /* 0    1    0    1    1    0    1    0    1    0    1    0    12/22/1976
                1398*/0x0AD5,    1977,    12,    11, /* 1    0    1    0    1    0    1    1    0    1    0    1    12/11/1977
                1399*/0x06AA,    1978,    12,     1, /* 0    1    0    1    0    1    0    1    0    1    1    0    12/1/1978
                1400*/0x0695,    1979,    11,    20, /* 1    0    1    0    1    0    0    1    0    1    1    0    11/20/1979
                1401*/0x052B,    1980,    11,     8, /* 1    1    0    1    0    1    0    0    1    0    1    0    11/8/1980
                1402*/0x0A57,    1981,    10,    28, /* 1    1    1    0    1    0    1    0    0    1    0    1    10/28/1981
                1403*/0x04AE,    1982,    10,    18, /* 0    1    1    1    0    1    0    1    0    0    1    0    10/18/1982
                1404*/0x0976,    1983,    10,     7, /* 0    1    1    0    1    1    1    0    1    0    0    1    10/7/1983
                1405*/0x056C,    1984,     9,    26, /* 0    0    1    1    0    1    1    0    1    0    1    0    9/26/1984
                1406*/0x0B55,    1985,     9,    15, /* 1    0    1    0    1    0    1    0    1    1    0    1    9/15/1985
                1407*/0x0AAA,    1986,     9,     5, /* 0    1    0    1    0    1    0    1    0    1    0    1    9/5/1986
                1408*/0x0A55,    1987,     8,    25, /* 1    0    1    0    1    0    1    0    0    1    0    1    8/25/1987
                1409*/0x04AD,    1988,     8,    13, /* 1    0    1    1    0    1    0    1    0    0    1    0    8/13/1988
                1410*/0x095D,    1989,     8,     2, /* 1    0    1    1    1    0    1    0    1    0    0    1    8/2/1989
                1411*/0x02DA,    1990,     7,    23, /* 0    1    0    1    1    0    1    1    0    1    0    0    7/23/1990
                1412*/0x05D9,    1991,     7,    12, /* 1    0    0    1    1    0    1    1    1    0    1    0    7/12/1991
                1413*/0x0DB2,    1992,     7,     1, /* 0    1    0    0    1    1    0    1    1    0    1    1    7/1/1992
                1414*/0x0BA4,    1993,     6,    21, /* 0    0    1    0    0    1    0    1    1    1    0    1    6/21/1993
                1415*/0x0B4A,    1994,     6,    10, /* 0    1    0    1    0    0    1    0    1    1    0    1    6/10/1994
                1416*/0x0A55,    1995,     5,    30, /* 1    0    1    0    1    0    1    0    0    1    0    1    5/30/1995
                1417*/0x02B5,    1996,     5,    18, /* 1    0    1    0    1    1    0    1    0    1    0    0    5/18/1996
                1418*/0x0575,    1997,     5,     7, /* 1    0    1    0    1    1    1    0    1    0    1    0    5/7/1997
                1419*/0x0B6A,    1998,     4,    27, /* 0    1    0    1    0    1    1    0    1    1    0    1    4/27/1998
                1420*/0x0BD2,    1999,     4,    17, /* 0    1    0    0    1    0    1    1    1    1    0    1    4/17/1999
                1421*/0x0BC4,    2000,     4,     6, /* 0    0    1    0    0    0    1    1    1    1    0    1    4/6/2000
                1422*/0x0B89,    2001,     3,    26, /* 1    0    0    1    0    0    0    1    1    1    0    1    3/26/2001
                1423*/0x0A95,    2002,     3,    15, /* 1    0    1    0    1    0    0    1    0    1    0    1    3/15/2002
                1424*/0x052D,    2003,     3,     4, /* 1    0    1    1    0    1    0    0    1    0    1    0    3/4/2003
                1425*/0x05AD,    2004,     2,    21, /* 1    0    1    1    0    1    0    1    1    0    1    0    2/21/2004
                1426*/0x0B6A,    2005,     2,    10, /* 0    1    0    1    0    1    1    0    1    1    0    1    2/10/2005
                1427*/0x06D4,    2006,     1,    31, /* 0    0    1    0    1    0    1    1    0    1    1    0    1/31/2006
                1428*/0x0DC9,    2007,     1,    20, /* 1    0    0    1    0    0    1    1    1    0    1    1    1/20/2007
                1429*/0x0D92,    2008,     1,    10, /* 0    1    0    0    1    0    0    1    1    0    1    1    1/10/2008
                1430*/0x0AA6,    2008,    12,    29, /* 0    1    1    0    0    1    0    1    0    1    0    1    12/29/2008
                1431*/0x0956,    2009,    12,    18, /* 0    1    1    0    1    0    1    0    1    0    0    1    12/18/2009
                1432*/0x02AE,    2010,    12,     7, /* 0    1    1    1    0    1    0    1    0    1    0    0    12/7/2010
                1433*/0x056D,    2011,    11,    26, /* 1    0    1    1    0    1    1    0    1    0    1    0    11/26/2011
                1434*/0x036A,    2012,    11,    15, /* 0    1    0    1    0    1    1    0    1    1    0    0    11/15/2012
                1435*/0x0B55,    2013,    11,     4, /* 1    0    1    0    1    0    1    0    1    1    0    1    11/4/2013
                1436*/0x0AAA,    2014,    10,    25, /* 0    1    0    1    0    1    0    1    0    1    0    1    10/25/2014
                1437*/0x094D,    2015,    10,    14, /* 1    0    1    1    0    0    1    0    1    0    0    1    10/14/2015
                1438*/0x049D,    2016,    10,     2, /* 1    0    1    1    1    0    0    1    0    0    1    0    10/2/2016
                1439*/0x095D,    2017,     9,    21, /* 1    0    1    1    1    0    1    0    1    0    0    1    9/21/2017
                1440*/0x02BA,    2018,     9,    11, /* 0    1    0    1    1    1    0    1    0    1    0    0    9/11/2018
                1441*/0x05B5,    2019,     8,    31, /* 1    0    1    0    1    1    0    1    1    0    1    0    8/31/2019
                1442*/0x05AA,    2020,     8,    20, /* 0    1    0    1    0    1    0    1    1    0    1    0    8/20/2020
                1443*/0x0D55,    2021,     8,     9, /* 1    0    1    0    1    0    1    0    1    0    1    1    8/9/2021
                1444*/0x0A9A,    2022,     7,    30, /* 0    1    0    1    1    0    0    1    0    1    0    1    7/30/2022
                1445*/0x092E,    2023,     7,    19, /* 0    1    1    1    0    1    0    0    1    0    0    1    7/19/2023
                1446*/0x026E,    2024,     7,     7, /* 0    1    1    1    0    1    1    0    0    1    0    0    7/7/2024
                1447*/0x055D,    2025,     6,    26, /* 1    0    1    1    1    0    1    0    1    0    1    0    6/26/2025
                1448*/0x0ADA,    2026,     6,    16, /* 0    1    0    1    1    0    1    1    0    1    0    1    6/16/2026
                1449*/0x06D4,    2027,     6,     6, /* 0    0    1    0    1    0    1    1    0    1    1    0    6/6/2027
                1450*/0x06A5,    2028,     5,    25, /* 1    0    1    0    0    1    0    1    0    1    1    0    5/25/2028
                1451*/0x054B,    2029,     5,    14, /* 1    1    0    1    0    0    1    0    1    0    1    0    5/14/2029
                1452*/0x0A97,    2030,     5,     3, /* 1    1    1    0    1    0    0    1    0    1    0    1    5/3/2030
                1453*/0x054E,    2031,     4,    23, /* 0    1    1    1    0    0    1    0    1    0    1    0    4/23/2031
                1454*/0x0AAE,    2032,     4,    11, /* 0    1    1    1    0    1    0    1    0    1    0    1    4/11/2032
                1455*/0x05AC,    2033,     4,     1, /* 0    0    1    1    0    1    0    1    1    0    1    0    4/1/2033
                1456*/0x0BA9,    2034,     3,    21, /* 1    0    0    1    0    1    0    1    1    1    0    1    3/21/2034
                1457*/0x0D92,    2035,     3,    11, /* 0    1    0    0    1    0    0    1    1    0    1    1    3/11/2035
                1458*/0x0B25,    2036,     2,    28, /* 1    0    1    0    0    1    0    0    1    1    0    1    2/28/2036
                1459*/0x064B,    2037,     2,    16, /* 1    1    0    1    0    0    1    0    0    1    1    0    2/16/2037
                1460*/0x0CAB,    2038,     2,     5, /* 1    1    0    1    0    1    0    1    0    0    1    1    2/5/2038
                1461*/0x055A,    2039,     1,    26, /* 0    1    0    1    1    0    1    0    1    0    1    0    1/26/2039
                1462*/0x0B55,    2040,     1,    15, /* 1    0    1    0    1    0    1    0    1    1    0    1    1/15/2040
                1463*/0x06D2,    2041,     1,     4, /* 0    1    0    0    1    0    1    1    0    1    1    0    1/4/2041
                1464*/0x0EA5,    2041,     12,   24, /* 1    0    1    0    0    1    0    1    0    1    1    1    12/24/2041
                1465*/0x0E4A,    2042,     12,   14, /* 0    1    0    1    0    0    1    0    0    1    1    1    12/14/2042
                1466*/0x0A95,    2043,     12,    3, /* 1    0    1    0    1    0    0    1    0    1    0    1    12/3/2043
                1467*/0x052D,    2044,     11,   21, /* 1    0    1    1    0    1    0    0    1    0    1    0    11/21/2044
                1468*/0x0AAD,    2045,     11,   10, /* 1    0    1    1    0    1    0    1    0    1    0    1    11/10/2045
                1469*/0x036C,    2046,     10,   31, /* 0    0    1    1    0    1    1    0    1    1    0    0    10/31/2046
                1470*/0x0759,    2047,     10,   20, /* 1    0    0    1    1    0    1    0    1    1    1    0    10/20/2047
                1471*/0x06D2,    2048,     10,    9, /* 0    1    0    0    1    0    1    1    0    1    1    0    10/9/2048
                1472*/0x0695,    2049,     9,    28, /* 1    0    1    0    1    0    0    1    0    1    1    0    9/28/2049
                1473*/0x052D,    2050,     9,    17, /* 1    0    1    1    0    1    0    0    1    0    1    0    9/17/2050
                1474*/0x0A5B,    2051,     9,     6, /* 1    1    0    1    1    0    1    0    0    1    0    1    9/6/2051
                1475*/0x04BA,    2052,     8,    26, /* 0    1    0    1    1    1    0    1    0    0    1    0    8/26/2052
                1476*/0x09BA,    2053,     8,    15, /* 0    1    0    1    1    1    0    1    1    0    0    1    8/15/2053
                1477*/0x03B4,    2054,     8,     5, /* 0    0    1    0    1    1    0    1    1    1    0    0    8/5/2054
                1478*/0x0B69,    2055,     7,    25, /* 1    0    0    1    0    1    1    0    1    1    0    1    7/25/2055
                1479*/0x0B52,    2056,     7,    14, /* 0    1    0    0    1    0    1    0    1    1    0    1    7/14/2056
                1480*/0x0AA6,    2057,     7,     3, /* 0    1    1    0    0    1    0    1    0    1    0    1    7/3/2057
                1481*/0x04B6,    2058,     6,    22, /* 0    1    1    0    1    1    0    1    0    0    1    0    6/22/2058
                1482*/0x096D,    2059,     6,    11, /* 1    0    1    1    0    1    1    0    1    0    0    1    6/11/2059
                1483*/0x02EC,    2060,     5,    31, /* 0    0    1    1    0    1    1    1    0    1    0    0    5/31/2060
                1484*/0x06D9,    2061,     5,    20, /* 1    0    0    1    1    0    1    1    0    1    1    0    5/20/2061
                1485*/0x0EB2,    2062,     5,    10, /* 0    1    0    0    1    1    0    1    0    1    1    1    5/10/2062
                1486*/0x0D54,    2063,     4,    30, /* 0    0    1    0    1    0    1    0    1    0    1    1    4/30/2063
                1487*/0x0D2A,    2064,     4,    18, /* 0    1    0    1    0    1    0    0    1    0    1    1    4/18/2064
                1488*/0x0A56,    2065,     4,     7, /* 0    1    1    0    1    0    1    0    0    1    0    1    4/7/2065
                1489*/0x04AE,    2066,     3,    27, /* 0    1    1    1    0    1    0    1    0    0    1    0    3/27/2066
                1490*/0x096D,    2067,     3,    16, /* 1    0    1    1    0    1    1    0    1    0    0    1    3/16/2067
                1491*/0x0D6A,    2068,     3,     5, /* 0    1    0    1    0    1    1    0    1    0    1    1    3/5/2068
                1492*/0x0B54,    2069,     2,    23, /* 0    0    1    0    1    0    1    0    1    1    0    1    2/23/2069
                1493*/0x0B29,    2070,     2,    12, /* 1    0    0    1    0    1    0    0    1    1    0    1    2/12/2070
                1494*/0x0A93,    2071,     2,     1, /* 1    1    0    0    1    0    0    1    0    1    0    1    2/1/2071
                1495*/0x052B,    2072,     1,    21, /* 1    1    0    1    0    1    0    0    1    0    1    0    1/21/2072
                1496*/0x0A57,    2073,     1,     9, /* 1    1    1    0    1    0    1    0    0    1    0    1    1/9/2073
                1497*/0x0536,    2073,     12,   30, /* 0    1    1    0    1    1    0    0    1    0    1    0    12/30/2073
                1498*/0x0AB5,    2074,     12,   19, /* 1    0    1    0    1    1    0    1    0    1    0    1    12/19/2074
                1499*/0x06AA,    2075,     12,    9, /* 0    1    0    1    0    1    0    1    0    1    1    0    12/9/2075
                1500*/0x0E93,    2076,     11,   27, /* 1    1    0    0    1    0    0    1    0    1    1    1    11/27/2076
                1501*/     0,    2077,     11,   17, /* 0    0    0    0    0    0    0    0    0    0    0    0    11/17/2077
    */      };
            // Direct inline initialization of DateMapping array would produce a lot of code bloat.

            // We take advantage of C# compiler compiles inline initialization of primitive type array into very compact code.
            // So we start with raw data stored in primitive type array, and initialize the DateMapping out of it

            DateMapping[] mapping = new DateMapping[rawData.Length / 4];
            for (int i = 0; i < mapping.Length; i++)
                mapping[i] = new DateMapping(rawData[i * 4], rawData[i * 4 + 1], rawData[i * 4 + 2], rawData[i * 4 + 3]);
            return mapping;
        }

        public const int UmAlQuraEra = 1;

        private const int DatePartYear = 0;
        private const int DatePartDayOfYear = 1;
        private const int DatePartMonth = 2;
        private const int DatePartDay = 3;

        private static readonly DateTime s_minDate = new DateTime(1900, 4, 30);
        private static readonly DateTime s_maxDate = new DateTime((new DateTime(2077, 11, 16, 23, 59, 59, 999)).Ticks + 9999);

        public override DateTime MinSupportedDateTime => s_minDate;

        public override DateTime MaxSupportedDateTime => s_maxDate;

        public override CalendarAlgorithmType AlgorithmType => CalendarAlgorithmType.LunarCalendar;

        public UmAlQuraCalendar()
        {
        }

        internal override CalendarId BaseCalendarID => CalendarId.HIJRI;

        internal override CalendarId ID => CalendarId.UMALQURA;

        protected override int DaysInYearBeforeMinSupportedYear =>
            // HijriCalendar has same number of days as UmAlQuraCalendar for any given year
            // HijriCalendar says year 1317 has 355 days.
            355;

        private static void ConvertHijriToGregorian(int HijriYear, int HijriMonth, int HijriDay, out int yg, out int mg, out int dg)
        {
            Debug.Assert((HijriYear >= MinCalendarYear) && (HijriYear <= MaxCalendarYear), "Hijri year is out of range.");
            Debug.Assert(HijriMonth >= 1, "Hijri month is out of range.");
            Debug.Assert(HijriDay >= 1, "Hijri day is out of range.");
            int nDays = HijriDay - 1;

            int index = HijriYear - MinCalendarYear;
            DateTime dt = s_hijriYearInfo[index].GregorianDate;
            int b = s_hijriYearInfo[index].HijriMonthsLengthFlags;

            for (int m = 1; m < HijriMonth; m++)
            {
                // Add the months lengths before mh
                nDays = nDays + 29 + (b & 1);
                b >>= 1;
            }

            dt = dt.AddDays(nDays);
            dt.GetDate(out yg, out mg, out dg);
        }

        private static long GetAbsoluteDateUmAlQura(int year, int month, int day)
        {
            ConvertHijriToGregorian(year, month, day, out int yg, out int mg, out int dg);
            return GregorianCalendar.GetAbsoluteDate(yg, mg, dg);
        }

        internal static void CheckTicksRange(long ticks)
        {
            if (ticks < s_minDate.Ticks || ticks > s_maxDate.Ticks)
            {
                throw new ArgumentOutOfRangeException(
                    "time",
                    ticks,
                    SR.Format(
                        CultureInfo.InvariantCulture,
                        SR.ArgumentOutOfRange_CalendarRange,
                        s_minDate,
                        s_maxDate));
            }
        }

        internal static void CheckEraRange(int era)
        {
            if (era != CurrentEra && era != UmAlQuraEra)
            {
                throw new ArgumentOutOfRangeException(nameof(era), era, SR.ArgumentOutOfRange_InvalidEraValue);
            }
        }

        internal static void CheckYearRange(int year, int era)
        {
            CheckEraRange(era);
            if (year < MinCalendarYear || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Range, MinCalendarYear, MaxCalendarYear));
            }
        }

        internal static void CheckYearMonthRange(int year, int month, int era)
        {
            CheckYearRange(year, era);
            if (month < 1 || month > 12)
            {
                ThrowHelper.ThrowArgumentOutOfRange_Month(month);
            }
        }

        private static void ConvertGregorianToHijri(DateTime time, out int HijriYear, out int HijriMonth, out int HijriDay)
        {
            Debug.Assert((time.Ticks >= s_minDate.Ticks) && (time.Ticks <= s_maxDate.Ticks), "Gregorian date is out of range.");

            // Find the index where we should start our search by quessing the Hijri year that we will be in HijriYearInfo.
            // A Hijri year is 354 or 355 days.  Use 355 days so that we will search from a lower index.

            int index = (int)((time.Ticks - s_minDate.Ticks) / Calendar.TicksPerDay) / 355;
            do
            {
            } while (time.CompareTo(s_hijriYearInfo[++index].GregorianDate) > 0); // while greater

            if (time.CompareTo(s_hijriYearInfo[index].GregorianDate) != 0)
            {
                index--;
            }

            TimeSpan ts = time.Subtract(s_hijriYearInfo[index].GregorianDate);
            int yh1 = index + MinCalendarYear;
            int mh1 = 1;
            int dh1 = 1;
            double nDays = ts.TotalDays;
            int b = s_hijriYearInfo[index].HijriMonthsLengthFlags;
            int daysPerThisMonth = 29 + (b & 1);

            while (nDays >= daysPerThisMonth)
            {
                nDays -= daysPerThisMonth;
                b >>= 1;
                daysPerThisMonth = 29 + (b & 1);
                mh1++;
            }
            dh1 += (int)nDays;

            HijriDay = dh1;
            HijriMonth = mh1;
            HijriYear = yh1;
        }

        /// <summary>
        /// First, we get the absolute date (the number of days from January 1st, 1 A.C) for the given ticks.
        /// Use the formula (((AbsoluteDate - 226894) * 33) / (33 * 365 + 8)) + 1, we can a rough value for the UmAlQura year.
        /// In order to get the exact UmAlQura year, we compare the exact absolute date for UmAlQuraYear and (UmAlQuraYear + 1).
        /// From here, we can get the correct UmAlQura year.
        /// </summary>
        private static int GetDatePart(DateTime time, int part)
        {
            long ticks = time.Ticks;
            CheckTicksRange(ticks);

            ConvertGregorianToHijri(time, out int UmAlQuraYear, out int UmAlQuraMonth, out int UmAlQuraDay);

            if (part == DatePartYear)
            {
                return UmAlQuraYear;
            }
            if (part == DatePartMonth)
            {
                return UmAlQuraMonth;
            }
            if (part == DatePartDay)
            {
                return UmAlQuraDay;
            }
            if (part == DatePartDayOfYear)
            {
                return (int)(GetAbsoluteDateUmAlQura(UmAlQuraYear, UmAlQuraMonth, UmAlQuraDay) - GetAbsoluteDateUmAlQura(UmAlQuraYear, 1, 1) + 1);
            }

            // Incorrect part value.
            throw new InvalidOperationException(SR.InvalidOperation_DateTimeParsing);
        }

        public override DateTime AddMonths(DateTime time, int months)
        {
            if (months < -120000 || months > 120000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(months),
                    months,
                    SR.Format(SR.ArgumentOutOfRange_Range, -120000, 120000));
            }

            // Get the date in UmAlQura calendar.
            int y = GetDatePart(time, DatePartYear);
            int m = GetDatePart(time, DatePartMonth);
            int d = GetDatePart(time, DatePartDay);
            int i = m - 1 + months;

            if (i >= 0)
            {
                m = i % 12 + 1;
                y += i / 12;
            }
            else
            {
                m = 12 + (i + 1) % 12;
                y += (i - 11) / 12;
            }

            if (d > 29)
            {
                int days = GetDaysInMonth(y, m);
                if (d > days)
                {
                    d = days;
                }
            }

            CheckYearRange(y, UmAlQuraEra);
            DateTime dt = new DateTime(GetAbsoluteDateUmAlQura(y, m, d) * TicksPerDay + time.Ticks % TicksPerDay);
            Calendar.CheckAddResult(dt.Ticks, MinSupportedDateTime, MaxSupportedDateTime);
            return dt;
        }

        public override DateTime AddYears(DateTime time, int years)
        {
            return AddMonths(time, years * 12);
        }

        public override int GetDayOfMonth(DateTime time)
        {
            return GetDatePart(time, DatePartDay);
        }

        public override DayOfWeek GetDayOfWeek(DateTime time) => time.DayOfWeek;

        public override int GetDayOfYear(DateTime time)
        {
            return GetDatePart(time, DatePartDayOfYear);
        }

        public override int GetDaysInMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);

            if ((s_hijriYearInfo[year - MinCalendarYear].HijriMonthsLengthFlags & (1 << month - 1)) == 0)
            {
                return 29;
            }
            else
            {
                return 30;
            }
        }

        internal static int RealGetDaysInYear(int year)
        {
            int days = 0;

            Debug.Assert((year >= MinCalendarYear) && (year <= MaxCalendarYear), "Hijri year is out of range.");

            int b = s_hijriYearInfo[year - MinCalendarYear].HijriMonthsLengthFlags;

            for (int m = 1; m <= 12; m++)
            {
                days = days + 29 + (b & 1);   /* Add the months lengths before mh */
                b >>= 1;
            }

            Debug.Assert((days == 354) || (days == 355), "Hijri year has to be 354 or 355 days.");
            return days;
        }

        public override int GetDaysInYear(int year, int era)
        {
            CheckYearRange(year, era);
            return RealGetDaysInYear(year);
        }

        public override int GetEra(DateTime time)
        {
            CheckTicksRange(time.Ticks);
            return UmAlQuraEra;
        }

        public override int[] Eras => new int[] { UmAlQuraEra };

        public override int GetMonth(DateTime time)
        {
            return GetDatePart(time, DatePartMonth);
        }

        public override int GetMonthsInYear(int year, int era)
        {
            CheckYearRange(year, era);
            return 12;
        }

        public override int GetYear(DateTime time)
        {
            return GetDatePart(time, DatePartYear);
        }

        public override bool IsLeapDay(int year, int month, int day, int era)
        {
            if (day >= 1 && day <= 29)
            {
                CheckYearMonthRange(year, month, era);
                return false;
            }

            // The year/month/era value checking is done in GetDaysInMonth().
            int daysInMonth = GetDaysInMonth(year, month, era);
            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }
            return false;
        }

        public override int GetLeapMonth(int year, int era)
        {
            CheckYearRange(year, era);
            return 0;
        }

        public override bool IsLeapMonth(int year, int month, int era)
        {
            CheckYearMonthRange(year, month, era);
            return false;
        }

        public override bool IsLeapYear(int year, int era)
        {
            CheckYearRange(year, era);
            return RealGetDaysInYear(year) == 355;
        }

        public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
        {
            if (day >= 1 && day <= 29)
            {
                CheckYearMonthRange(year, month, era);
                goto DayInRang;
            }

            // The year/month/era value checking is done in GetDaysInMonth().
            int daysInMonth = GetDaysInMonth(year, month, era);

            if (day < 1 || day > daysInMonth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(day),
                    day,
                    SR.Format(SR.ArgumentOutOfRange_Day, daysInMonth, month));
            }
        DayInRang:
            long lDate = GetAbsoluteDateUmAlQura(year, month, day);
            if (lDate < 0)
            {
                throw new ArgumentOutOfRangeException(null, SR.ArgumentOutOfRange_BadYearMonthDay);
            }

            return new DateTime(lDate * GregorianCalendar.TicksPerDay + TimeToTicks(hour, minute, second, millisecond));
        }

        private const int DefaultTwoDigitYearMax = 1451;

        public override int TwoDigitYearMax
        {
            get
            {
                if (_twoDigitYearMax == -1)
                {
                    _twoDigitYearMax = GetSystemTwoDigitYearSetting(ID, DefaultTwoDigitYearMax);
                }

                return _twoDigitYearMax;
            }
            set
            {
                if (value != 99 && (value < MinCalendarYear || value > MaxCalendarYear))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(value),
                        value,
                        SR.Format(SR.ArgumentOutOfRange_Range, MinCalendarYear, MaxCalendarYear));
                }

                VerifyWritable();
                // We allow year 99 to be set so that one can make ToFourDigitYearMax a no-op by setting TwoDigitYearMax to 99.
                _twoDigitYearMax = value;
            }
        }

        public override int ToFourDigitYear(int year)
        {
            if (year < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(year), year, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (year < 100)
            {
                return base.ToFourDigitYear(year);
            }

            if (year < MinCalendarYear || year > MaxCalendarYear)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(year),
                    year,
                    SR.Format(SR.ArgumentOutOfRange_Range, MinCalendarYear, MaxCalendarYear));
            }

            return year;
        }
    }
}
