// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=====================================================================
**
** Source:    test1.c (localtime)
**
** Purpose:   Tests the PAL implementation of the localtime function.
**			  localtime() is passed a date in seconds, since January 01
**			  1970 midnight, UTC. localtime() converts the time to the 
**			  tm struct, those values are tested for validity.
**
**
**===================================================================*/
#include <palsuite.h>

int __cdecl main(int argc, char **argv)
{
    time_t dates[] ={1003327482,	// Oct   17, 2001 10:04:42 am
                    701307301,		// March 22, 1992  6:35:01 pm
                    973620900,		// Nov   07, 2000  1:15:00 pm
                    924632589,		// April 20, 1999  2:23:09 pm
                    951934989};		// March 01, 2000  1:23:09 pm
    struct tm * converted_date;
    int i;

    if (PAL_Initialize(argc, argv) != 0)

        return (FAIL);   

    /*Convert from time_t to struct tm*/
    for ( i=0; i < (sizeof(dates)/sizeof(time_t)); i++)
    {
        converted_date = localtime(&dates[i]);

        if ((converted_date->tm_hour < 0) || (converted_date->tm_hour > 23))
        {
            Fail("ERROR: localtime returned %d for tm_hour\n", converted_date->tm_hour);
        }
        if ((converted_date->tm_mday < 1) || (converted_date->tm_mday > 31))
        {
            Fail("ERROR: localtime returned %d for tm_mday\n",converted_date->tm_mday);
        }
        if ((converted_date->tm_min < 0) || (converted_date->tm_min > 59))
        {
            Fail("ERROR: localtime returned %d for tm_min\n",converted_date->tm_min);
        }
        if ((converted_date->tm_mon < 0) || (converted_date->tm_mon > 11))
        {
            Fail("ERROR: localtime returned %d for tm_mon\n",converted_date->tm_mon);
        }
        if ((converted_date->tm_sec < 0) || (converted_date->tm_sec > 59))
        {
            Fail("ERROR: localtime returned %d for tm_sec\n",converted_date->tm_sec);
        }
        if ((converted_date->tm_wday < 0) || (converted_date->tm_wday > 6 ))
        {
            Fail("ERROR: localtime returned %d for tm_wday\n",converted_date->tm_wday);	
        }
        if ((converted_date->tm_yday < 0) || (converted_date->tm_yday > 365))
        {
            Fail("ERROR: localtime returned %d for tm_yday\n",converted_date->tm_yday);
        }
    }

    PAL_Terminate();
    return (PASS);
}
