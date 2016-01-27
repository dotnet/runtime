// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: GetCalendarInfoW.c
**
** Purpose: Positive test the GetCalendarInfoW API.
**          Call GetCalendarInfoW to retrieve the information of a 
**          calendar
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;
    LCID Locale = LOCALE_USER_DEFAULT;
    CALTYPE CalType = CAL_ITWODIGITYEARMAX|CAL_RETURN_NUMBER;
    DWORD dwValue;

    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }
    
    err = GetCalendarInfoW(Locale,/*locale idendifier*/
                            CAL_GREGORIAN, /*calendar identifier*/
                            CalType,  /*calendar tyope*/
                            NULL,     /*buffer to store the retrieve info*/
                            0,        /*alwayse zero*/
                            &dwValue);/*to store the requrest data*/               
    if (0 == err)
    {
        Fail("GetCalendarInfoW failed for CAL_GREGORIAN!\n");
    }
    
    err = GetCalendarInfoW(Locale,/*locale idendifier*/
                            CAL_GREGORIAN_US, /*calendar identifier*/
                            CalType,  /*calendar tyope*/
                            NULL,     /*buffer to store the retreive info*/
                            0,        /*alwayse zero*/
                            &dwValue);/*to store the requrest data*/               
    if (0 == err)
    {
        Fail("GetCalendarInfoW failed for CAL_GREGORIAN_US!\n");
    }


    PAL_Terminate();
    return PASS;
}
