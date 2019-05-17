// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#include "pal/locale.h"
#include "pal/cruntime.h"
#include "pal/stackstring.hpp"
#include "pal/unicodedata.h"

#if !(HAVE_PTHREAD_RWLOCK_T || HAVE_COREFOUNDATION)
#error Either pthread rwlocks or Core Foundation are required for Unicode support
#endif /* !(HAVE_PTHREAD_RWLOCK_T || HAVE_COREFOUNDATION) */

#include <pthread.h>
#include <locale.h>
#if HAVE_LIBINTL_H
#include <libintl.h>
#endif // HAVE_LIBINTL_H
#include <errno.h>
#if HAVE_COREFOUNDATION
#include <CoreFoundation/CoreFoundation.h>
#endif // HAVE_COREFOUNDATION

#include <debugmacrosext.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(UNICODE);

#if HAVE_COREFOUNDATION

static CP_MAPPING CP_TO_NATIVE_TABLE[] = {
    { 65001, kCFStringEncodingUTF8, 4, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1252, kCFStringEncodingWindowsLatin1, 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1251, kCFStringEncodingWindowsCyrillic, 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1253, kCFStringEncodingWindowsGreek, 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1254, kCFStringEncodingWindowsLatin5, 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1258, kCFStringEncodingWindowsVietnamese, 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 932, kCFStringEncodingDOSJapanese, 2, { 129, 159, 224, 252, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 949, kCFStringEncodingDOSKorean, 2, { 129, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 950, kCFStringEncodingDOSChineseTrad, 2, { 129, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }
};

#else // HAVE_COREFOUNDATION

static const CP_MAPPING CP_TO_NATIVE_TABLE[] = {
    { 65001, "utf8", 4, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }
};

#endif // HAVE_COREFOUNDATION

// We hardcode the system's default codepage to be UTF-8.
// There are several reasons for this:
// - On OSX, HFS+ file names are encoded as UTF-8.
// - On OSX, When writing strings to the console, the Terminal.app will interpret them as UTF-8.
// - We want Ansi marshalling to mean marshal to UTF-8 on Mac and Linux
static const UINT PAL_ACP = 65001;

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
CODEPAGEGetData

    IN UINT CodePage - The code page the caller
    is attempting to retrieve data on.

    Returns a pointer to structure, NULL otherwise.
--*/
const CP_MAPPING *
CODEPAGEGetData( IN UINT CodePage )
{
    UINT nSize = sizeof( CP_TO_NATIVE_TABLE ) / sizeof( CP_TO_NATIVE_TABLE[ 0 ] );
    UINT nIndex = 0;

    if ( CP_ACP == CodePage )
    {
        CodePage = PAL_ACP;
    }

    /* checking if the CodePage is ACP and returning true if so */
    while (nIndex < nSize)
    {
        if ( ( CP_TO_NATIVE_TABLE[ nIndex ] ).nCodePage == CodePage )
        {
            return &(CP_TO_NATIVE_TABLE[ nIndex ]);
        }
        nIndex++;
    }
    return NULL;
}

#if HAVE_COREFOUNDATION
/*++
Function :

CODEPAGECPToCFStringEncoding - Gets the CFStringEncoding for
the given codepage.

Returns the CFStringEncoding for the given codepage.
--*/
CFStringEncoding CODEPAGECPToCFStringEncoding(UINT codepage)
{
    const CP_MAPPING *cp_mapping = CODEPAGEGetData(codepage);
    if (cp_mapping == NULL)
    {
        return kCFStringEncodingInvalidId;
    }
    else
    {
        return cp_mapping->nCFEncoding;
    }
}
#endif // HAVE_COREFOUNDATION

/*++
Function:
CharNextA

Parameters

lpsz
[in] Pointer to a character in a null-terminated string.

Return Values

A pointer to the next character in the string, or to the terminating null character if at the end of the string, indicates success.

If lpsz points to the terminating null character, the return value is equal to lpsz.

See MSDN doc.
--*/
LPSTR
PALAPI
CharNextA(
  IN LPCSTR lpsz)
{
    LPSTR pRet;
    PERF_ENTRY(CharNextA);
    ENTRY("CharNextA (lpsz=%p (%s))\n", lpsz?lpsz:NULL, lpsz?lpsz:NULL);

    pRet = CharNextExA(GetACP(), lpsz, 0);

    LOGEXIT ("CharNextA returns LPSTR %p\n", pRet);
    PERF_EXIT(CharNextA);
    return pRet;
}


/*++
Function:
CharNextExA

See MSDN doc.
--*/
LPSTR
PALAPI
CharNextExA(
    IN WORD CodePage,
    IN LPCSTR lpCurrentChar,
    IN DWORD dwFlags)
{
    LPSTR pRet = (LPSTR) lpCurrentChar;

    PERF_ENTRY(CharNextExA);
    ENTRY("CharNextExA (CodePage=%hu, lpCurrentChar=%p (%s), dwFlags=%#x)\n",
    CodePage, lpCurrentChar?lpCurrentChar:"NULL", lpCurrentChar?lpCurrentChar:"NULL", dwFlags);

    if ((lpCurrentChar != NULL) && (*lpCurrentChar != 0))
    {
        pRet += (*(lpCurrentChar+1) != 0) &&
            IsDBCSLeadByteEx(CodePage, *lpCurrentChar) ?  2 : 1;
    }

    LOGEXIT("CharNextExA returns LPSTR:%p (%s)\n", pRet, pRet);
    PERF_EXIT(CharNextExA);
    return pRet;
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
IsValidCodePage

See MSDN doc.

Notes :
"pseudo code pages", like CP_ACP, aren't considered 'valid' in this context.
CP_UTF7 and CP_UTF8, however, *are* considered valid code pages, even though
MSDN fails to mention them in the IsValidCodePage entry.
Note : CP_UTF7 support isn't required for CoreCLR
--*/
BOOL
PALAPI
IsValidCodePage(
    IN UINT CodePage)
{
    BOOL retval = FALSE;

    PERF_ENTRY(IsValidCodePage);
    ENTRY("IsValidCodePage(%d)\n", CodePage );

    switch(CodePage)
    {
    case CP_ACP       : /* fall through */
    case CP_OEMCP      : /* fall through */
    case CP_MACCP      : /* fall through */
    case CP_THREAD_ACP:
        /* 'pseudo code pages' : not valid */
        retval = FALSE;
        break;
    case CP_UTF7:
        /* valid in Win32, but not supported in the PAL */
        retval = FALSE;
        break;
    case CP_UTF8:
        /* valid, but not part of CODEPAGEGetData's tables */
        retval = TRUE;
        break;
    default:
        retval = (NULL != CODEPAGEGetData( CodePage ));
        break;
    }

    LOGEXIT("IsValidCodePage returns BOOL %d\n",retval);
    PERF_EXIT(IsValidCodePage);
    return retval;
}

/*++
Function:
GetCPInfo

See MSDN doc.
--*/
BOOL
PALAPI
GetCPInfo(
  IN UINT CodePage,
  OUT LPCPINFO lpCPInfo)
{
    const CP_MAPPING * lpStruct = NULL;
    BOOL bRet = FALSE;

    PERF_ENTRY(GetCPInfo);
    ENTRY("GetCPInfo(CodePage=%hu, lpCPInfo=%p)\n", CodePage, lpCPInfo);

    /*check if the input code page is valid*/
    if( CP_ACP != CodePage && !IsValidCodePage( CodePage ) )
    {
        /* error, invalid argument */
        ERROR("CodePage(%d) parameter is invalid\n",CodePage);
        SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }

    /*check if the lpCPInfo parameter is valid. */
    if( !lpCPInfo )
    {
        /* error, invalid argument */
        ERROR("lpCPInfo cannot be NULL\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }

    if ( NULL != ( lpStruct = CODEPAGEGetData( CodePage ) ) )
    {
        lpCPInfo->MaxCharSize = lpStruct->nMaxByteSize;;
        memcpy( lpCPInfo->LeadByte, lpStruct->LeadByte , MAX_LEADBYTES );

        /* Don't need to be set, according to the spec. */
        memset( lpCPInfo->DefaultChar, '?', MAX_DEFAULTCHAR );

        bRet = TRUE;
    }

done:
    LOGEXIT("GetCPInfo returns BOOL %d \n",bRet);
    PERF_EXIT(GetCPInfo);
    return bRet;
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

    LOGEXIT("GetACP returning UINT %d\n", PAL_ACP );
    PERF_EXIT(GetACP);

    return PAL_ACP;
}


/*++
Function:
IsDBCSLeadByteEx

See MSDN doc.
--*/
BOOL
PALAPI
IsDBCSLeadByteEx(
     IN UINT CodePage,
     IN BYTE TestChar)
{
    CPINFO cpinfo;
    SIZE_T i;
    BOOL bRet = FALSE;

    PERF_ENTRY(IsDBCSLeadByteEx);
    ENTRY("IsDBCSLeadByteEx(CodePage=%#x, TestChar=%d)\n", CodePage, TestChar);

    /* Get the lead byte info with respect to the given codepage*/
    if( !GetCPInfo( CodePage, &cpinfo ) )
    {
        ERROR("Error CodePage(%#x) parameter is invalid\n", CodePage );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto done;
    }

    for( i=0; i < sizeof(cpinfo.LeadByte)/sizeof(cpinfo.LeadByte[0]); i += 2 )
    {
        if( 0 == cpinfo.LeadByte[ i ] )
        {
            goto done;
        }

        /*check if the given char is in one of the lead byte ranges*/
        if( cpinfo.LeadByte[i] <= TestChar && TestChar<= cpinfo.LeadByte[i+1] )
        {
            bRet = TRUE;
            goto done;
        }
    }
done:
    LOGEXIT("IsDBCSLeadByteEx returns BOOL %d\n",bRet);
    PERF_EXIT(IsDBCSLeadByteEx);
    return bRet;
}

/*++
Function:
IsDBCSLeadByte

See MSDN doc.
--*/
BOOL
PALAPI
IsDBCSLeadByte(
        IN BYTE TestChar)
{
    // UNIXTODO: Implement this!
    ERROR("Needs Implementation!!!");
    return FALSE;
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
#if HAVE_COREFOUNDATION
    CFStringRef cfString = NULL;
    CFStringEncoding cfEncoding;
    int bytesToConvert;
#endif /* HAVE_COREFOUNDATION */

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
    if (CodePage == CP_UTF8 || (CodePage == CP_ACP && GetACP() == CP_UTF8))
    {
        if (cbMultiByte <= -1)
        {
        cbMultiByte = strlen(lpMultiByteStr) + 1;
        }

        retval = UTF8ToUnicode(lpMultiByteStr, cbMultiByte, lpWideCharStr, cchWideChar, dwFlags);
        goto EXIT;
    }

#if !HAVE_COREFOUNDATION
    ERROR( "This code page is not in the system.\n" );
    SetLastError( ERROR_INVALID_PARAMETER );
    goto EXIT;
#else /* !HAVE_COREFOUNDATION */
    bytesToConvert = cbMultiByte;
    if (bytesToConvert == -1)
    {
        /* Plus one for the trailing '\0', which will end up
        * in the CFString. */
        bytesToConvert = strlen(lpMultiByteStr) + 1;
    }

    cfEncoding = CODEPAGECPToCFStringEncoding(CodePage);
    if (cfEncoding == kCFStringEncodingInvalidId)
    {
        ERROR( "This code page is not in the system.\n" );
        SetLastError( ERROR_INVALID_PARAMETER );
        goto EXIT;
    }

    cfString = CFStringCreateWithBytes(kCFAllocatorDefault, (UInt8*)lpMultiByteStr,
                     bytesToConvert, cfEncoding, TRUE);
    if (cfString == NULL)
    {
        ERROR( "Failed to convert the string to the specified encoding.\n" );
        SetLastError( ERROR_NO_UNICODE_TRANSLATION );
        goto EXIT;
    }

    if (cchWideChar != 0)
    {
        /* Do the conversion. */
        CFIndex length = CFStringGetLength(cfString);
        if (length > cchWideChar)
        {
            ERROR("Error insufficient buffer\n");
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            retval = 0;
            goto ReleaseString;
        }
        CFStringGetCharacters(cfString, CFRangeMake(0, length),
                (UniChar*)lpWideCharStr);
        retval = length;
    }
    else
    {
        /* Just return the number of wide characters needed. */
        retval = CFStringGetLength(cfString);
    }

ReleaseString:
    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
#endif /* !HAVE_COREFOUNDATION */

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
#if HAVE_COREFOUNDATION
    CFStringRef cfString = NULL;
    CFStringEncoding cfEncoding;
    int charsToConvert;
    CFIndex charsConverted;
    CFIndex bytesConverted;
#endif /* !HAVE_COREFOUNDATION */

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
    if (CodePage == CP_UTF8 || (CodePage == CP_ACP && GetACP() == CP_UTF8))
    {
        if (cchWideChar == -1)
        {
            cchWideChar = PAL_wcslen(lpWideCharStr) + 1;
        }
        retval = UnicodeToUTF8(lpWideCharStr, cchWideChar, lpMultiByteStr, cbMultiByte);
        goto EXIT;
    }

#if HAVE_COREFOUNDATION
    charsToConvert = cchWideChar;
    if (charsToConvert == -1)
    {
        LPCWSTR ptr = lpWideCharStr;

        charsToConvert = 0;
        while(*ptr++ != 0)
        {
            charsToConvert++;
        }
        charsToConvert++;   /* For the terminating '\0' */
    }

    cfEncoding = CODEPAGECPToCFStringEncoding(CodePage);
    if (cfEncoding == kCFStringEncodingInvalidId)
    {
        ERROR( "This code page is not in the system.\n" );
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    cfString = CFStringCreateWithCharacters(kCFAllocatorDefault,
                      (const UniChar*)lpWideCharStr, charsToConvert);
    if (cfString == NULL)
    {
        ERROR("CFString creation failed.\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto EXIT;
    }

    if (cbMultiByte == 0)
    {
        lpMultiByteStr = NULL;
    }
    charsConverted = CFStringGetBytes(cfString,
                    CFRangeMake(0, charsToConvert),
                    cfEncoding, '?', TRUE, (UInt8*)lpMultiByteStr,
                    cbMultiByte, &bytesConverted);
    if (charsConverted != charsToConvert)
    {
        if (lpMultiByteStr != NULL)
        {
            // CFStringGetBytes can fail due to an insufficient buffer or for
            // other reasons. We need to check if we're out of buffer space.
            charsConverted = CFStringGetBytes(cfString,
                        CFRangeMake(0, charsToConvert),
                        cfEncoding, '?', TRUE, NULL,
                        0, &bytesConverted);
            if (cbMultiByte < bytesConverted)
            {
                ERROR("Insufficient buffer for CFStringGetBytes.\n");
                SetLastError(ERROR_INSUFFICIENT_BUFFER);
                goto ReleaseString;
            }
        }
        ERROR("Not all characters were converted.\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        goto ReleaseString;
    }
    retval = bytesConverted;

ReleaseString:
    if (cfString != NULL)
    {
        CFRelease(cfString);
    }
#else /*HAVE_COREFOUNDATION */
    ERROR( "This code page is not in the system.\n" );
    SetLastError( ERROR_INVALID_PARAMETER );
    goto EXIT;
#endif /* HAVE_COREFOUNDATION */

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

/*++
Function :

PAL_BindResources - bind the resource domain to the path where the coreclr resides

Returns TRUE if it succeeded, FALSE if it failed due to OOM
--*/
BOOL
PALAPI
PAL_BindResources(IN LPCSTR lpDomain)
{
#if HAVE_LIBINTL_H
    _ASSERTE(g_szCoreCLRPath != NULL);
    char * coreCLRDirectoryPath;
    PathCharString coreCLRDirectoryPathPS;
    int len = strlen(g_szCoreCLRPath);
    coreCLRDirectoryPath = coreCLRDirectoryPathPS.OpenStringBuffer(len);
    if (NULL == coreCLRDirectoryPath)
    {
        return FALSE;
    }
    DWORD size = FILEGetDirectoryFromFullPathA(g_szCoreCLRPath, len, coreCLRDirectoryPath);
    coreCLRDirectoryPathPS.CloseBuffer(size);

    LPCSTR boundPath = bindtextdomain(lpDomain, coreCLRDirectoryPath);

    return boundPath != NULL;
#else // HAVE_LIBINTL_H
    // UNIXTODO: Implement for Unixes without libintl if necessary
    return TRUE;
#endif // HAVE_LIBINTL_H
}

/*++
Function :

PAL_GetResourceString - get localized string for a specified resource.
The string that is passed in should be the English string, since it
will be returned if an appropriately localized version is not found.

Returns number of characters retrieved, 0 if it failed.
--*/
int
PALAPI
PAL_GetResourceString(
        IN LPCSTR lpDomain,
        IN LPCSTR lpResourceStr,
        OUT LPWSTR lpWideCharStr,
        IN int cchWideChar
      )
{
#if HAVE_LIBINTL_H
    // NOTE: dgettext returns the key if it fails to locate the appropriate
    // resource. In our case, that will be the English string.
    LPCSTR resourceString = dgettext(lpDomain, lpResourceStr);
#else // HAVE_LIBINTL_H
    // UNIXTODO: Implement for OSX using the native localization API

    // This is a temporary solution until we add the real native resource support.
    LPCSTR resourceString = lpResourceStr;
#endif // HAVE_LIBINTL_H

    int length = strlen(resourceString);
    return UTF8ToUnicode(lpResourceStr, length + 1, lpWideCharStr, cchWideChar, 0);
}
