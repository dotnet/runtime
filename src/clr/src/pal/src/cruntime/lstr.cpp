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
  lstrcatW

The lstrcat function appends one string to another. 

Parameters

lpString1        [in/out] Pointer to a null-terminated string. The buffer must be large
                          enough to contain both strings. 
lpString2        [in]     Pointer to the null-terminated string to be appended to the
                          string specified in the lpString1 parameter. 

Return Values

If the function succeeds, the return value is a pointer to the buffer.
If the function fails, the return value is NULL. 

--*/
LPWSTR
PALAPI
lstrcatW(
	 IN OUT LPWSTR lpString1,
	 IN LPCWSTR lpString2)
{
    LPWSTR lpStart = lpString1;

    PERF_ENTRY(lstrcatW);
    ENTRY("lstrcatW (lpString1=%p (%S), lpString2=%p (%S))\n",
          lpString1?lpString1:W16_NULLSTRING,
          lpString1?lpString1:W16_NULLSTRING, lpString2?lpString2:W16_NULLSTRING, lpString2?lpString2:W16_NULLSTRING);

    if (lpString1 == NULL)
    {
        ERROR("invalid lpString1 argument\n");
        LOGEXIT("lstrcatW returning LPWSTR NULL\n");
        PERF_EXIT(lstrcatW);
        return NULL;
    }

    if (lpString2 == NULL)
    {
        ERROR("invalid lpString2 argument\n");
        LOGEXIT("lstrcatW returning LPWSTR NULL\n");
        PERF_EXIT(lstrcatW);
        return NULL;
    }

    /* find end of source string */
    while (*lpString1)
    {
        lpString1++;
    }

    /* concatenate new string */
    while(*lpString2)
    {
        *lpString1++ = *lpString2++;
    }

    /* add terminating null */
    *lpString1 = '\0';

    LOGEXIT("lstrcatW returning LPWSTR %p (%S)\n", lpStart, lpStart);
    PERF_EXIT(lstrcatW);
    return lpStart;
}


/*++
Function:
  lstrcpyW

The lstrcpy function copies a string to a buffer. 

To copy a specified number of characters, use the lstrcpyn function.

Parameters

lpString1        [out] Pointer to a buffer to receive the contents of the string pointed
                       to by the lpString2 parameter. The buffer must be large enough to
                       contain the string, including the terminating null character. 

lpString2        [in] Pointer to the null-terminated string to be copied. 

Return Values

If the function succeeds, the return value is a pointer to the buffer.
If the function fails, the return value is NULL. 

--*/
LPWSTR
PALAPI
lstrcpyW(
	 OUT LPWSTR lpString1,
	 IN LPCWSTR lpString2)
{
    LPWSTR lpStart = lpString1;

    PERF_ENTRY(lstrcpyW);
    ENTRY("lstrcpyW (lpString1=%p, lpString2=%p (%S))\n",
          lpString1?lpString1:W16_NULLSTRING, lpString2?lpString2:W16_NULLSTRING, lpString2?lpString2:W16_NULLSTRING);

    if (lpString1 == NULL)
    {
        ERROR("invalid lpString1 argument\n");
        LOGEXIT("lstrcpyW returning LPWSTR NULL\n");
        PERF_EXIT(lstrcpyW);
        return NULL;
    }

    if (lpString2 == NULL)
    {
        ERROR("invalid lpString2 argument\n");
        LOGEXIT("lstrcpyW returning LPWSTR NULL\n");
        PERF_EXIT(lstrcpyW);
        return NULL;
    }

    /* copy source string to destination string */
    while(*lpString2)
    {
        *lpString1++ = *lpString2++;
    }

    /* add terminating null */
    *lpString1 = '\0';

    LOGEXIT("lstrcpyW returning LPWSTR %p (%S)\n", lpStart, lpStart);
    PERF_EXIT(lstrcpyW);
    return lpStart;
}


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


/*++
Function:
  lstrcpynW

The lstrcpyn function copies a specified number of characters from a
source string into a buffer.

Parameters

lpString1        [out] Pointer to a buffer into which the function copies characters.
                       The buffer must be large enough to contain the number of TCHARs
                       specified by iMaxLength, including room for a terminating null character. 
lpString2        [in]  Pointer to a null-terminated string from which the function copies
                       characters. 
iMaxLength       [in]  Specifies the number of TCHARs to be copied from the string pointed
                       to by lpString2 into the buffer pointed to by lpString1, including a
                       terminating null character.

Return Values

If the function succeeds, the return value is a pointer to the buffer.
If the function fails, the return value is NULL. 

--*/
LPWSTR
PALAPI
lstrcpynW(
	  OUT LPWSTR lpString1,
	  IN LPCWSTR lpString2,
	  IN int iMaxLength)
{
    LPWSTR lpStart = lpString1;

    PERF_ENTRY(lstrcpynW);
    ENTRY("lstrcpynW (lpString1=%p, lpString2=%p (%S), iMaxLength=%d)\n",
              lpString1?lpString1:W16_NULLSTRING, lpString2?lpString2:W16_NULLSTRING, lpString2?lpString2:W16_NULLSTRING, iMaxLength);

    if (lpString1 == NULL)
    {
        ERROR("invalid lpString1 argument\n");
        LOGEXIT("lstrcpynW returning LPWSTR NULL\n");
        PERF_EXIT(lstrcpynW);
        return NULL;
    }

    if (lpString2 == NULL)
    {
        ERROR("invalid lpString2 argument\n");
        LOGEXIT("lstrcpynW returning LPWSTR NULL\n");
        PERF_EXIT(lstrcpynW);
        return NULL;
    }

    /* copy source string to destination string */
    while(iMaxLength > 1 && *lpString2)
    {
        *lpString1++ = *lpString2++;
        iMaxLength--;
    }

    /* add terminating null */
    if (iMaxLength > 0)
    {
        *lpString1 = '\0';
    }

    LOGEXIT("lstrcpynW returning LPWSTR %p (%S)\n", lpStart, lpStart);
    PERF_EXIT(lstrcpynW);
    return lpStart;

}


