// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    lstr.c

Abstract:

    Implementation of functions manipulating unicode/ansi strings. (lstr*A/W)



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(CRT);


/*++
Function:
  lstrlenA


The lstrlen function returns the length in bytes (ANSI version) or
characters (Unicode version) of the specified string (not including
the terminating null character).

Parameters

lpString        [in] Pointer to a null-terminated string. 

Return Values

The return value specifies the length of the string, in TCHARs. This
refers to bytes for ANSI versions of the function or characters for
Unicode versions.

--*/
int
PALAPI
lstrlenA( IN LPCSTR lpString)
{
    int nChar = 0;

    PERF_ENTRY(lstrlenA);
    ENTRY("lstrlenA (lpString=%p (%s))\n", lpString?lpString:"NULL", lpString?lpString:"NULL");
    if (lpString)
    {
        while (*lpString++)
        {
            nChar++;      
        }
    }
    LOGEXIT("lstrlenA returning int %d\n", nChar);
    PERF_EXIT(lstrlenA);
    return nChar;
}


/*++
Function:
  lstrlenW

The lstrlen function returns the length in bytes (ANSI version) or
characters (Unicode version) of the specified string (not including
the terminating null character).

Parameters

lpString        [in] Pointer to a null-terminated string. 

Return Values

The return value specifies the length of the string, in TCHARs. This
refers to bytes for ANSI versions of the function or characters for
Unicode versions.

--*/
int
PALAPI
lstrlenW(
	 IN LPCWSTR lpString)
{
    int nChar = 0;

    PERF_ENTRY(lstrlenW);
    ENTRY("lstrlenW (lpString=%p (%S))\n", lpString?lpString:W16_NULLSTRING, lpString?lpString:W16_NULLSTRING);
    if (lpString != NULL)
    {
        while (*lpString++)
        {
            nChar++;      
        }
    }
    LOGEXIT("lstrlenW returning int %d\n", nChar);
    PERF_EXIT(lstrlenW);
    return nChar;
}
