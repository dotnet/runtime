// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

unicode.cpp

Abstract:

Implementation of all functions related to Unicode support

Revision History:



--*/

#include "pal/thread.hpp"

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/utf8.h"
#include "pal/cruntime.h"
#include "pal/stackstring.hpp"
#include "pal/unicodedata.h"

#include <pthread.h>
#include <locale.h>
#include <errno.h>

#include <debugmacrosext.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(UNICODE);

/*++
Function:
UnicodeDataComp
This is the comparison function used by the bsearch function to search
for unicode characters in the UnicodeData array.

Parameter:
pnKey
The unicode character value to search for.
elem
A pointer to a UnicodeDataRec.

Return value:
<0 if pnKey < elem->nUnicodeValue
0 if pnKey == elem->nUnicodeValue
>0 if pnKey > elem->nUnicodeValue
--*/
static int UnicodeDataComp(const void *pnKey, const void *elem)
{
    WCHAR uValue = ((UnicodeDataRec*)elem)->nUnicodeValue;

    if (*((INT*)pnKey) < uValue)
    {
        return -1;
    }
    else if (*((INT*)pnKey) > uValue)
    {
        return 1;
    }
    else
    {
        return 0;
    }
}

/*++
Function:
GetUnicodeData
This function is used to get information about a Unicode character.

Parameters:
nUnicodeValue
The numeric value of the Unicode character to get information about.
pDataRec
The UnicodeDataRec to fill in with the data for the Unicode character.

Return value:
TRUE if the Unicode character was found.

--*/
BOOL GetUnicodeData(INT nUnicodeValue, UnicodeDataRec *pDataRec)
{
    BOOL bRet;

    UnicodeDataRec *dataRec;
    INT nNumOfChars = UNICODE_DATA_SIZE;
    dataRec = (UnicodeDataRec *) bsearch(&nUnicodeValue, UnicodeData, nNumOfChars,
                   sizeof(UnicodeDataRec), UnicodeDataComp);
    if (dataRec == NULL)
    {
        bRet = FALSE;
    }
    else
    {
        bRet = TRUE;
        *pDataRec = *dataRec;
    }
    return bRet;
}

wchar_16
__cdecl
PAL_ToUpperInvariant( wchar_16 c )
{
    UnicodeDataRec dataRec;

    PERF_ENTRY(PAL_ToUpperInvariant);
    ENTRY("PAL_ToUpperInvariant (c=%d)\n", c);

    if (!GetUnicodeData(c, &dataRec))
    {
        TRACE( "Unable to retrieve unicode data for the character %c.\n", c );
        LOGEXIT("PAL_ToUpperInvariant returns int %d\n", c );
        PERF_EXIT(PAL_ToUpperInvariant);
        return c;
    }

    if ( dataRec.nFlag != LOWER_CASE )
    {
        LOGEXIT("PAL_ToUpperInvariant returns int %d\n", c );
        PERF_EXIT(PAL_ToUpperInvariant);
        return c;
    }
    else
    {
        LOGEXIT("PAL_ToUpperInvariant returns int %d\n", dataRec.nOpposingCase );
        PERF_EXIT(PAL_ToUpperInvariant);
        return dataRec.nOpposingCase;
    }
}

wchar_16
__cdecl
PAL_ToLowerInvariant( wchar_16 c )
{
    UnicodeDataRec dataRec;

    PERF_ENTRY(PAL_ToLowerInvariant);
    ENTRY("PAL_ToLowerInvariant (c=%d)\n", c);

    if (!GetUnicodeData(c, &dataRec))
    {
        TRACE( "Unable to retrieve unicode data for the character %c.\n", c );
        LOGEXIT("PAL_ToLowerInvariant returns int %d\n", c );
        PERF_EXIT(PAL_ToLowerInvariant);
        return c;
    }

    if ( dataRec.nFlag != UPPER_CASE )
    {
        LOGEXIT("PAL_ToLowerInvariant returns int %d\n", c );
        PERF_EXIT(PAL_ToLowerInvariant);
        return c;
    }
    else
    {
        LOGEXIT("PAL_ToLowerInvariant returns int %d\n", dataRec.nOpposingCase );
        PERF_EXIT(PAL_ToLowerInvariant);
        return dataRec.nOpposingCase;
    }
}

/*++
Function:
GetConsoleOutputCP

See MSDN doc.
--*/
UINT
PALAPI
GetConsoleOutputCP(
       VOID)
{
    UINT nRet = 0;
    PERF_ENTRY(GetConsoleOutputCP);
    ENTRY("GetConsoleOutputCP()\n");
    nRet = GetACP();
    LOGEXIT("GetConsoleOutputCP returns UINT %d \n", nRet );
    PERF_EXIT(GetConsoleOutputCP);
    return nRet;
}

/*++
Function:
GetACP

See MSDN doc.
--*/
UINT
PALAPI
GetACP(VOID)
{
    PERF_ENTRY(GetACP);
    ENTRY("GetACP(VOID)\n");

    LOGEXIT("GetACP returning UINT %d\n", CP_UTF8);
    PERF_EXIT(GetACP);

    return CP_UTF8;
}

/*++
Function:
MultiByteToWideChar

See MSDN doc.

--*/
int
PALAPI
MultiByteToWideChar(
        IN UINT CodePage,
        IN DWORD dwFlags,
        IN LPCSTR lpMultiByteStr,
        IN int cbMultiByte,
        OUT LPWSTR lpWideCharStr,
        IN int cchWideChar)
{
    INT retval =0;

    PERF_ENTRY(MultiByteToWideChar);
    ENTRY("MultiByteToWideChar(CodePage=%u, dwFlags=%#x, lpMultiByteStr=%p (%s),"
    " cbMultiByte=%d, lpWideCharStr=%p, cchWideChar=%d)\n",
    CodePage, dwFlags, lpMultiByteStr?lpMultiByteStr:"NULL", lpMultiByteStr?lpMultiByteStr:"NULL",
    cbMultiByte, lpWideCharStr, cchWideChar);

    if (dwFlags & ~(MB_ERR_INVALID_CHARS | MB_PRECOMPOSED))
    {
        ASSERT("Error dwFlags(0x%x) parameter is invalid\n", dwFlags);
        SetLastError(ERROR_INVALID_FLAGS);
        goto EXIT;
    }

    if ( (cbMultiByte == 0) || (cchWideChar < 0) ||
        (lpMultiByteStr == NULL) ||
        ((cchWideChar != 0) &&
        ((lpWideCharStr == NULL) ||
        (lpMultiByteStr == (LPSTR)lpWideCharStr))) )
    {
        ERROR("Error lpMultiByteStr parameters are invalid\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    // Use UTF8ToUnicode on all systems, since it replaces
    // invalid characters and Core Foundation doesn't do that.
    if (CodePage == CP_UTF8 || CodePage == CP_ACP)
    {
        if (cbMultiByte <= -1)
        {
        cbMultiByte = strlen(lpMultiByteStr) + 1;
        }

        retval = UTF8ToUnicode(lpMultiByteStr, cbMultiByte, lpWideCharStr, cchWideChar, dwFlags);
        goto EXIT;
    }

    ERROR( "This code page is not in the system.\n" );
    SetLastError( ERROR_INVALID_PARAMETER );
    goto EXIT;

EXIT:

    LOGEXIT("MultiByteToWideChar returns %d.\n",retval);
    PERF_EXIT(MultiByteToWideChar);
    return retval;
}


/*++
Function:
WideCharToMultiByte

See MSDN doc.

--*/
int
PALAPI
WideCharToMultiByte(
        IN UINT CodePage,
        IN DWORD dwFlags,
        IN LPCWSTR lpWideCharStr,
        IN int cchWideChar,
        OUT LPSTR lpMultiByteStr,
        IN int cbMultiByte,
        IN LPCSTR lpDefaultChar,
        OUT LPBOOL lpUsedDefaultChar)
{
    INT retval =0;
    char defaultChar = '?';
    BOOL usedDefaultChar = FALSE;

    PERF_ENTRY(WideCharToMultiByte);
    ENTRY("WideCharToMultiByte(CodePage=%u, dwFlags=%#x, lpWideCharStr=%p (%S), "
          "cchWideChar=%d, lpMultiByteStr=%p, cbMultiByte=%d, "
          "lpDefaultChar=%p, lpUsedDefaultChar=%p)\n",
          CodePage, dwFlags, lpWideCharStr?lpWideCharStr:W16_NULLSTRING, lpWideCharStr?lpWideCharStr:W16_NULLSTRING,
          cchWideChar, lpMultiByteStr, cbMultiByte,
          lpDefaultChar, lpUsedDefaultChar);

    if (dwFlags & ~WC_NO_BEST_FIT_CHARS)
    {
        ERROR("dwFlags %d invalid\n", dwFlags);
        SetLastError(ERROR_INVALID_FLAGS);
        goto EXIT;
    }

    // No special action is needed for WC_NO_BEST_FIT_CHARS. The default
    // behavior of this API on Unix is not to find the best fit for a unicode
    // character that does not map directly into a code point in the given
    // code page. The best fit functionality is not available in wctomb on Unix
    // and is better left unimplemented for security reasons anyway.

    if ((cchWideChar < -1) || (cbMultiByte < 0) ||
        (lpWideCharStr == NULL) ||
        ((cbMultiByte != 0) &&
        ((lpMultiByteStr == NULL) ||
        (lpWideCharStr == (LPWSTR)lpMultiByteStr))) )
    {
        ERROR("Error lpWideCharStr parameters are invalid\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    if (lpDefaultChar != NULL)
    {
        defaultChar = *lpDefaultChar;
    }

    // Use UnicodeToUTF8 on all systems because we use
    // UTF8ToUnicode in MultiByteToWideChar() on all systems.
    if (CodePage == CP_UTF8 || CodePage == CP_ACP)
    {
        if (cchWideChar == -1)
        {
            cchWideChar = PAL_wcslen(lpWideCharStr) + 1;
        }
        retval = UnicodeToUTF8(lpWideCharStr, cchWideChar, lpMultiByteStr, cbMultiByte);
        goto EXIT;
    }

    ERROR( "This code page is not in the system.\n" );
    SetLastError( ERROR_INVALID_PARAMETER );
    goto EXIT;

EXIT:

    if ( lpUsedDefaultChar != NULL )
    {
        *lpUsedDefaultChar = usedDefaultChar;
    }

    /* Flag the cases when WC_NO_BEST_FIT_CHARS was not specified
     * but we found characters that had to be replaced with default
     * characters. Note that Windows would have attempted to find
     * best fit characters under these conditions and that could pose
     * a security risk.
     */
    _ASSERT_MSG((dwFlags & WC_NO_BEST_FIT_CHARS) || !usedDefaultChar,
          "WideCharToMultiByte found a string which doesn't round trip: (%p)%S "
          "and WC_NO_BEST_FIT_CHARS was not specified\n",
          lpWideCharStr, lpWideCharStr);

    LOGEXIT("WideCharToMultiByte returns INT %d\n", retval);
    PERF_EXIT(WideCharToMultiByte);
    return retval;
}

extern char * g_szCoreCLRPath;
