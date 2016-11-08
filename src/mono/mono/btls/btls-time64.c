/*

Copyright (c) 2007-2008  Michael G Schwern

This software originally derived from Paul Sheer's pivotal_gmtime_r.c.

The MIT License:

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

*/

/* See http://code.google.com/p/y2038 for this code's origin */

#include <assert.h>
#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <time.h>
#include <errno.h>

/* Spec says except for stftime() and the _r() functions, these
   all return static memory.  Stabbings! */
static struct tm   Static_Return_Date;
static char        Static_Return_String[35];

static const int days_in_month[2][12] = {
    {31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31},
    {31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31},
};

static const int julian_days_by_month[2][12] = {
    {0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334},
    {0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335},
};

static char const wday_name[7][3] = {
    "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"
};

static char const mon_name[12][3] = {
    "Jan", "Feb", "Mar", "Apr", "May", "Jun",
    "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
};

static const int length_of_year[2] = { 365, 366 };

/* Some numbers relating to the gregorian cycle */
static const int64_t     years_in_gregorian_cycle   = 400;
#define               days_in_gregorian_cycle      ((365 * 400) + 100 - 4 + 1)
static const int64_t seconds_in_gregorian_cycle = days_in_gregorian_cycle * 60LL * 60LL * 24LL;

/* Year range we can trust the time funcitons with */
#define MAX_SAFE_YEAR 2037
#define MIN_SAFE_YEAR 1971

/* 28 year Julian calendar cycle */
#define SOLAR_CYCLE_LENGTH 28

/* Year cycle from MAX_SAFE_YEAR down. */
static const int safe_years_high[SOLAR_CYCLE_LENGTH] = {
    2016, 2017, 2018, 2019,
    2020, 2021, 2022, 2023,
    2024, 2025, 2026, 2027,
    2028, 2029, 2030, 2031,
    2032, 2033, 2034, 2035,
    2036, 2037, 2010, 2011,
    2012, 2013, 2014, 2015
};

/* Year cycle from MIN_SAFE_YEAR up */
static const int safe_years_low[SOLAR_CYCLE_LENGTH] = {
    1996, 1997, 1998, 1971,
    1972, 1973, 1974, 1975,
    1976, 1977, 1978, 1979,
    1980, 1981, 1982, 1983,
    1984, 1985, 1986, 1987,
    1988, 1989, 1990, 1991,
    1992, 1993, 1994, 1995,
};

/* Let's assume people are going to be looking for dates in the future.
   Let's provide some cheats so you can skip ahead.
   This has a 4x speed boost when near 2008.
*/
/* Number of days since epoch on Jan 1st, 2008 GMT */
#define CHEAT_DAYS  (1199145600 / 24 / 60 / 60)
#define CHEAT_YEARS 108

#define IS_LEAP(n)      ((!(((n) + 1900) % 400) || (!(((n) + 1900) % 4) && (((n) + 1900) % 100))) != 0)
#define WRAP(a,b,m)     ((a) = ((a) <  0  ) ? ((b)--, (a) + (m)) : (a))

/* timegm() is not in the C or POSIX spec, but it is such a useful
   extension I would be remiss in leaving it out.  Also I need it
   for localtime64()
*/
int64_t btls_timegm64(const struct tm *date) {
    int64_t days    = 0;
    int64_t seconds = 0;
    int64_t     year;
    int64_t     orig_year = (int64_t)date->tm_year;
    int      cycles  = 0;

    if( orig_year > 100 ) {
        cycles = (orig_year - 100) / 400;
        orig_year -= cycles * 400;
        days      += (int64_t)cycles * days_in_gregorian_cycle;
    }
    else if( orig_year < -300 ) {
        cycles = (orig_year - 100) / 400;
        orig_year -= cycles * 400;
        days      += (int64_t)cycles * days_in_gregorian_cycle;
    }

    if( orig_year > 70 ) {
        year = 70;
        while( year < orig_year ) {
            days += length_of_year[IS_LEAP(year)];
            year++;
        }
    }
    else if ( orig_year < 70 ) {
        year = 69;
        do {
            days -= length_of_year[IS_LEAP(year)];
            year--;
        } while( year >= orig_year );
    }


    days += julian_days_by_month[IS_LEAP(orig_year)][date->tm_mon];
    days += date->tm_mday - 1;

    seconds = days * 60 * 60 * 24;

    seconds += date->tm_hour * 60 * 60;
    seconds += date->tm_min * 60;
    seconds += date->tm_sec;

    return(seconds);
}
