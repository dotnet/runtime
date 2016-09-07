// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================
**
** Source: GetDateFormatW_neg.c
**
** Purpose: Negative test the GetDateFormatW API.
**          Call GetDateFormatW by passing an invalid flag
**
**
**============================================================*/
#define UNICODE
#include <palsuite.h>

int __cdecl main(int argc, char *argv[])
{
    int err;
    WCHAR *wpFormat;
    LPCSTR lpString = "gg";  
    CONST SYSTEMTIME *lpDate = NULL;
    LCID DefaultLocale;
    DWORD dwFlags;
    int DateSize;
    WCHAR *wpBuffer = NULL;
    
    /*Initialize the PAL environment*/
    err = PAL_Initialize(argc, argv);
    if(0 != err)
    {
        return FAIL;
    }

    /*convert to a wide character string*/
    wpFormat = convert((char *)lpString);

    /*    
    DefaultLocale = GetSystemDefaultLCID() which is not defined in PAL;

    LOCALE_SYSTEM_DEFAULT = MAKELCID(LANG_SYSTEM_DEFAULT, SORT_DEFAULT)
  
    LANG_SYSTEM_DEFAULT = MAKELANGID(LANG_NEUTRAL,SUBLANG_SYS_DEFAULT);
    SUBLANG_SYS_DEFAULT is not defined in PAL, here use hardcoding,
    the value is from winnt.h
    */

    DefaultLocale = MAKELCID(MAKELANGID(LANG_NEUTRAL, 0x02), SORT_DEFAULT);

    dwFlags = DATE_USE_ALT_CALENDAR; /*set the flags*/
    

    DateSize = 0;

    /*retrieve the buffer size*/
    DateSize = GetDateFormatW(
                DefaultLocale, /*system default locale*/
                dwFlags,       /*function option*/
                (SYSTEMTIME *)lpDate,        /*always is NULL*/
                wpFormat,      /*pointer to a picture string*/
                wpBuffer,      /*out buffer*/
                DateSize);     /*buffer size*/

    if(DateSize <= 0)
    {
        free(wpFormat);
        Fail("\nRetrieved an invalid buffer size\n");
    }

    wpBuffer = (WCHAR*)malloc((DateSize + 1)*sizeof(WCHAR));
    if(NULL == wpBuffer)
    {
        free(wpFormat);
        Fail("\nFailed to allocate memory to store the formatted string\n");
    }

    err = GetDateFormatW(
                DefaultLocale,        /*system default locale*/
                0x00000001|0x00000008,/*DATE_SHORTDATE|DATE_YEARMONTH */
                                      /*an invalid flag*/
                (SYSTEMTIME *)lpDate, /*always is NULL, or use system date*/
                wpFormat,      /*pointer to a picture string*/
                wpBuffer,      /*out buffer*/
                DateSize);     /*buffer size*/

    free(wpBuffer);
    free(wpFormat);

    if(0 != err || GetLastError() != ERROR_INVALID_FLAGS)
    {
        Fail("\nFailed to call GetDateFormatW for a negative test by "
                "passing an invalid flag, error code=%d\n", GetLastError());
    }


    PAL_Terminate();
    return PASS;
}
