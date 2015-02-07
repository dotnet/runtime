//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

unicode.cpp

Abstract:

Implementation of all functions related to Unicode support

Revision History:



--*/

#include "pal/thread.hpp"

#include "pal/palinternal.h"
#include "pal/unicode_data.h"
#include "pal/dbgmsg.h"
#include "pal/utf8.h"
#include "pal/locale.h"
#include "pal/cruntime.h"

#if !(HAVE_PTHREAD_RWLOCK_T || HAVE_COREFOUNDATION)
#error Either pthread rwlocks or Core Foundation are required for Unicode support
#endif /* !(HAVE_PTHREAD_RWLOCK_T || HAVE_COREFOUNDATION) */

#include <pthread.h>
#include <locale.h>
#include <errno.h>
#if HAVE_COREFOUNDATION
#include <corefoundation/corefoundation.h>
#endif // HAVE_COREFOUNDATION

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

// We hardcode the system's default codepage to be UTF-8.
// There are several reasons for this:
// - HFS+ file names are encoded as UTF-8.
// - When writing strings to the console, the Terminal.app will interpret them as UTF-8.
static const UINT PAL_ACP = 65001;

#else // HAVE_COREFOUNDATION

// The following outlines the process to add a new codepage support in PAL:
// 1. Type "locale" on a localized Unix machine to find out the value of "LC_TYPE",
//    e.g., on a Russian FreeBSD machine, LC_TYPE is ru_RU.KOI8-R.
// 2. Add a new entry to the CP_TO_NATIVE_TABLE in pal/corunix/locale/unicode.c file such as:
//    { 20866, "ru_RU.KOI8-R", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }
//    where 20866 is the Code-Page Identifier and "ru_RU.KOI8-R" is the locale on Unix.
//    Check MSDN documentation for the mapping between code page identifier and locale.
// 3. Add a new entry to the LANGID_map table in pal/corunix/locale/locale.c file such as:
//    {"ru_RU.KOI8-R", 0x0419}, /* Russian */
//    where 0x0419 is the Windows language ID.
//    Check MSDN documentation for mapping between locale and language ID.
// 4. Note that since locale is not named consistently on various Unix systems,
//    additional modification to configure.in may be needed to find out the
//    correct locale name for each Unix system.
//    Type "locale -a" to list all available installed locales on a Unix Machine.
// 5. If you need to use resources files for locale testing, use MS Word to create .txt file in
//    UTF-8 or UTF-16 encoding so that clix resgen.exe can convert them to .resources files.
// 6. Follow the comments in CODEPAGEInit function for hints to debug locale problem.

// Don't rely on Unix to map CodePage 1252, since there's no charset that
// matches perfectly. (Even ISO 8859-1 doesn't match.)
static const WCHAR PAL_CP_1252[] = {
    0x0000, 0x0001, 0x0002, 0x0003, 0x0004, 0x0005, 0x0006, 0x0007, 0x0008, 0x0009, 
    0x000A, 0x000B, 0x000C, 0x000D, 0x000E, 0x000F, 0x0010, 0x0011, 0x0012, 0x0013, 
    0x0014, 0x0015, 0x0016, 0x0017, 0x0018, 0x0019, 0x001A, 0x001B, 0x001C, 0x001D, 
    0x001E, 0x001F, 0x0020, 0x0021, 0x0022, 0x0023, 0x0024, 0x0025, 0x0026, 0x0027, 
    0x0028, 0x0029, 0x002A, 0x002B, 0x002C, 0x002D, 0x002E, 0x002F, 0x0030, 0x0031, 
    0x0032, 0x0033, 0x0034, 0x0035, 0x0036, 0x0037, 0x0038, 0x0039, 0x003A, 0x003B, 
    0x003C, 0x003D, 0x003E, 0x003F, 0x0040, 0x0041, 0x0042, 0x0043, 0x0044, 0x0045, 
    0x0046, 0x0047, 0x0048, 0x0049, 0x004A, 0x004B, 0x004C, 0x004D, 0x004E, 0x004F, 
    0x0050, 0x0051, 0x0052, 0x0053, 0x0054, 0x0055, 0x0056, 0x0057, 0x0058, 0x0059, 
    0x005A, 0x005B, 0x005C, 0x005D, 0x005E, 0x005F, 0x0060, 0x0061, 0x0062, 0x0063, 
    0x0064, 0x0065, 0x0066, 0x0067, 0x0068, 0x0069, 0x006A, 0x006B, 0x006C, 0x006D, 
    0x006E, 0x006F, 0x0070, 0x0071, 0x0072, 0x0073, 0x0074, 0x0075, 0x0076, 0x0077, 
    0x0078, 0x0079, 0x007A, 0x007B, 0x007C, 0x007D, 0x007E, 0x007F, 0x20AC, 0x003F, 
    0x201A, 0x0192, 0x201E, 0x2026, 0x2020, 0x2021, 0x02C6, 0x2030, 0x0160, 0x2039, 
    0x0152, 0x003F, 0x017D, 0x003F, 0x003F, 0x2018, 0x2019, 0x201C, 0x201D, 0x2022, 
    0x2013, 0x2014, 0x02DC, 0x2122, 0x0161, 0x203A, 0x0153, 0x003F, 0x017E, 0x0178, 
    0x00A0, 0x00A1, 0x00A2, 0x00A3, 0x00A4, 0x00A5, 0x00A6, 0x00A7, 0x00A8, 0x00A9, 
    0x00AA, 0x00AB, 0x00AC, 0x00AD, 0x00AE, 0x00AF, 0x00B0, 0x00B1, 0x00B2, 0x00B3, 
    0x00B4, 0x00B5, 0x00B6, 0x00B7, 0x00B8, 0x00B9, 0x00BA, 0x00BB, 0x00BC, 0x00BD, 
    0x00BE, 0x00BF, 0x00C0, 0x00C1, 0x00C2, 0x00C3, 0x00C4, 0x00C5, 0x00C6, 0x00C7, 
    0x00C8, 0x00C9, 0x00CA, 0x00CB, 0x00CC, 0x00CD, 0x00CE, 0x00CF, 0x00D0, 0x00D1, 
    0x00D2, 0x00D3, 0x00D4, 0x00D5, 0x00D6, 0x00D7, 0x00D8, 0x00D9, 0x00DA, 0x00DB, 
    0x00DC, 0x00DD, 0x00DE, 0x00DF, 0x00E0, 0x00E1, 0x00E2, 0x00E3, 0x00E4, 0x00E5, 
    0x00E6, 0x00E7, 0x00E8, 0x00E9, 0x00EA, 0x00EB, 0x00EC, 0x00ED, 0x00EE, 0x00EF, 
    0x00F0, 0x00F1, 0x00F2, 0x00F3, 0x00F4, 0x00F5, 0x00F6, 0x00F7, 0x00F8, 0x00F9, 
    0x00FA, 0x00FB, 0x00FC, 0x00FD, 0x00FE,
    0x00FF};

static const CP_MAPPING CP_TO_NATIVE_TABLE[] = {

    { 1252, ISO_NAME("en_US", "8859", "1"), 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1252, "C", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1252, "POSIX", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

    /* Minor differences. */
    // Not present on Solaris 8.
    // { 1250, "la_LN.ISO_8859-2", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 

    // Available on FreeBSD and Solaris only.
    { 20866, "ru_RU.KOI8-R", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

    /* Minor differences. */
    { 1253, ISO_NAME("el_GR", "8859", "7"), 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    { 1254, ISO_NAME("tr_TR", "8859", "9"), 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

    /*
    * Not present on default FreeBSD 4.5 installation.
    * { 1255, "ISO-8859-5", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    * { 1251, "bg_BG.CP1251", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    *
    */

    /* 
    * Not compatible.
    * { 1256, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    *
    */

    /*
    * Not in FreeBSD 4.5
    * { 1257, "ISO_8859-13", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    *
    */

    /* Close but different */
    // This is actually Windows Vietnamese, which doesn't have much to do
    // with U.S. ISO 8859-1.
    { 1258, ISO_NAME("en_US", "8859", "1"), 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

    { 932, JA_JP_LOCALE_NAME, 2, { 129, 159, 224, 252, 0, 0, 0, 0, 0, 0, 0, 0 } },

    { 949, KO_KR_LOCALE_NAME, 2, { 129, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },

    /*
    * No mapping
    * { 936, "POSIX", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    *
    */

    { 950, ZH_TW_LOCALE_NAME, 2, { 129, 254, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }

    /*
    * No mapping.
    * { 437, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    *
    */

    /*
    * No mapping.
    * { 850, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    *
    */

    /*
    * Old DOS Code pages. No equivent on BSD
    * { 852, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 855, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } },
    * { 874, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 737, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 775, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 857, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 860, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 861, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 862, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 863, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 864, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 865, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } }, 
    * { 869, "---", 1, { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 } } 
    *
    */
    
};

static const LPCSTR c_lpszPalDefaultLctype = "C";
static LPSTR lpszPalOriginalLctype = NULL;

/* What the system's code page is. */
static UINT PAL_ACP = 0;
static const UINT PAL_DEFAULT_CP = 1252;

#endif // HAVE_COREFOUNDATION

#if !HAVE_COREFOUNDATION || ENABLE_DOWNLEVEL_FOR_NLS

static pthread_rwlock_t lock;

/*++
Function :
CODEPAGEInit -

    Initializes PAL_ACP to the systems current code page, 
    based on the LC_CYTPE locale identifier, and inits the 
    read/write lock.
--*/

BOOL CODEPAGEInit( void )
{
    BOOL bRetVal = FALSE;

    TRACE( "CODEPAGEInit( void )\n" );
    /* Init the rwlock. */
    if ( 0 != pthread_rwlock_init( &lock, NULL ) )
    {
    ERROR( "Unable to init the read write lock! Reason %s(%d)\n",
        strerror( errno ), errno );
    }
#if !HAVE_COREFOUNDATION
    else
    {
        /* get the systems code page. */
        LPSTR lpCodePage = setlocale( LC_CTYPE, "" );
        // Use the following steps to check if a particular locale works properly:
        // 1. Use MS Word to create a localized resource file in your locale of interest, e.g.,
        //    for ru_RU.KOI8-R locale, save a localized resource file as a plain text file by 
        //    choosing "Cyrillic(KOI8-R)" encoding in save file menu.
        // 2. Temporarily set lpCodePage to your locale of interest here such as
        //    lpCodePage = "ru_RU.KOI8-R";
        // 3. Rebuild PAL and then run you program and save the console output to a file.
        // 4. Use "od -a" to compare the string encoding in the output file with
        //    the localized resource file.
        if ( lpCodePage )
        {
            UINT nSize = sizeof( CP_TO_NATIVE_TABLE ) / sizeof( CP_TO_NATIVE_TABLE[ 0 ] );
            UINT nIndex;

            /* Check to see if it is supported. */
            for (nIndex = 0; nIndex < nSize; nIndex++)
            {
                if ( 0 == strcmp( lpCodePage, CP_TO_NATIVE_TABLE[ nIndex ].lpBSDEquivalent ) )
                {
                    lpszPalOriginalLctype = PAL__strdup(lpCodePage);
                    PAL_ACP = CP_TO_NATIVE_TABLE[ nIndex ].nCodePage;
                    bRetVal = TRUE;
                    break;
                }
            }

            if ( !bRetVal )
            {
                WARN( "Code page is not supported. Defaulting to \"C\"(1252)\n" );
                if ( NULL == setlocale( LC_CTYPE, c_lpszPalDefaultLctype ) )
                {
                    ERROR( "Unable to set the LC_CTYPE to %s\n", 
                        c_lpszPalDefaultLctype );
                        pthread_rwlock_destroy( &lock );
                }
                else
                {   
                    lpszPalOriginalLctype = PAL__strdup(lpCodePage);
                    PAL_ACP = PAL_DEFAULT_CP;
                    bRetVal = TRUE;
                }
            }
        }
    }
#else // !HAVE_COREFOUNDATION
    bRetVal = TRUE;
#endif // !HAVE_COREFOUNDATION
    return bRetVal;
}

/*++
Function :

CODEPAGEAcquireReadLock - Sets the read lock.

Returns TRUE on success, FALSE otherwise.
--*/
BOOL 
CODEPAGEAcquireReadLock( void )
{
    UINT nRet = 0;
    CPalThread *pthrCurrent = InternalGetCurrentThread();
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();
    if (0 != (nRet = pthread_rwlock_rdlock(&lock)))
    {
        pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
        ERROR("Unable to Acquire a readlock! Reason %s(%d)\n",
            strerror(nRet), nRet);
        return FALSE;
    }
    return TRUE;
}

/*++
Function :

CODEPAGEAcquireWriteLock - Sets the write lock.

Returns TRUE on success, FALSE otherwise.
--*/
BOOL 
CODEPAGEAcquireWriteLock( void )
{
    UINT nRet = 0;
    CPalThread *pthrCurrent = InternalGetCurrentThread();
    pthrCurrent->suspensionInfo.EnterUnsafeRegion();
    if (0 != (nRet = pthread_rwlock_wrlock(&lock)))
    {
        pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
        ERROR("Unable to Acquire a writelock! Reason %s(%d)\n",
            strerror(nRet), nRet);
        return FALSE;
    }
    return TRUE;
}

/*++
Function :

CODEPAGEReleaseLock - Releases the lock.

Returns TRUE on success, FALSE otherwise.
--*/
BOOL 
CODEPAGEReleaseLock( void )
{
    UINT nRet = 0;
    CPalThread *pthrCurrent = InternalGetCurrentThread();
    if (0 != (nRet = pthread_rwlock_unlock(&lock)))
    {
        ERROR("Unable to release the lock! Reason %s(%d)\n",
            strerror(nRet), nRet);
        return FALSE;
    }
    pthrCurrent->suspensionInfo.LeaveUnsafeRegion();
    return TRUE;
}

/*++
Function :

CODEPAGECleanup - Destroys the lock.

--*/
void CODEPAGECleanup( void )
{
    UINT nRet = 0;
    if ( 0 != ( nRet = pthread_rwlock_destroy( &lock ) ) )
    {
        ERROR( "Unable to destroy the lock! Reason %s(%d)\n",
            strerror( nRet ), nRet );
    }
#if !HAVE_COREFOUNDATION
    else
    {
        if (lpszPalOriginalLctype) 
        {
            if ( NULL == setlocale( LC_CTYPE, lpszPalOriginalLctype ) )
            {
                ERROR( "Unable to restore the LC_CTYPE.\n" );
            }
            PAL_free(lpszPalOriginalLctype);
            lpszPalOriginalLctype = NULL;
        }
    }
#endif // !HAVE_COREFOUNDATION
}

#endif // !HAVE_COREFOUNDATION || ENABLE_DOWNLEVEL_FOR_NLS


#if !HAVE_COREFOUNDATION
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
    WORD  rangeValue = ((UnicodeDataRec*)elem)->rangeValue;

    if (*((INT*)pnKey) < uValue)
    {
        return -1;
    }
    else
    {
        if (*((INT*)pnKey) > (uValue + rangeValue))
        {
            return 1;
        }
        else
        {
            return 0;
        }
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
    if (nUnicodeValue <= UNICODE_DATA_DIRECT_ACCESS)
    {
        *pDataRec = UnicodeData[nUnicodeValue];
        bRet = TRUE;
    }
    else
    {
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
    }
    return bRet;
}
#endif /* !HAVE_COREFOUNDATION */

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
AreFileApisANSI

The AreFileApisANSI function determines whether the file I/O functions
are using the ANSI or OEM character set code page. This function is
useful for 8-bit console input and output operations.

Return Values

If the set of file I/O functions is using the ANSI code page, the return value is nonzero.

If the set of file I/O functions is using the OEM code page, the return value is zero.

In the ROTOR version we always return true since there is no concept
of OEM code pages.

--*/
BOOL
PALAPI
AreFileApisANSI(
    VOID)
{
    PERF_ENTRY(AreFileApisANSI);
    ENTRY("AreFileApisANSI ()\n");

    LOGEXIT("AreFileApisANSI returns BOOL TRUE\n");
    PERF_EXIT(AreFileApisANSI);
    return TRUE;
}


/*++
Function:
GetConsoleCP

See MSDN doc.
--*/
UINT
PALAPI
GetConsoleCP(
     VOID)
{
    UINT nRet = 0;
    PERF_ENTRY(GetConsoleCP);
    ENTRY("GetConsoleCP()\n");
     
    nRet = GetACP();

    LOGEXIT("GetConsoleCP returns UINT %d\n", nRet );
    PERF_EXIT(GetConsoleCP);
    return nRet;
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
Note : CP_UTF7 support isn't required for Rotor
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
        /* valid in Win32, but not supported in Rotor */
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

#if ENABLE_DOWNLEVEL_FOR_NLS
/*++
Function:
GetStringTypeEx

See MSDN doc.
--*/
BOOL
PALAPI
GetStringTypeExW(
     IN LCID Locale,
     IN DWORD dwInfoType,
     IN LPCWSTR lpSrcStr,
     IN int cchSrc,
     OUT LPWORD lpCharType)
{


    int i = 0;
#if !HAVE_COREFOUNDATION
    UnicodeDataRec unicodeDataRec;
#endif /* !HAVE_COREFOUNDATION */
    BOOL bRet = TRUE;
    wchar_t  wcstr ;
    PERF_ENTRY(GetStringTypeExW);
    ENTRY("GetStringTypeExW(Locale=%#x, dwInfoType=%#x, lpSrcStr=%p (%S), "
    "cchSrc=%d, lpCharType=%p)\n",
    Locale, dwInfoType, lpSrcStr?lpSrcStr:W16_NULLSTRING, lpSrcStr?lpSrcStr:W16_NULLSTRING, cchSrc, lpCharType);

    if((Locale != LOCALE_USER_DEFAULT)||(dwInfoType != CT_CTYPE1)
        || (cchSrc != 1) || (lpSrcStr == (LPCWSTR)lpCharType))
    {
        ASSERT("One of the input parameters is invalid\n");
        SetLastError(ERROR_INVALID_PARAMETER);
        bRet = FALSE;
        goto GetStringTypeExExit;
    }


    /*
    * get length if needed...
    */
    if(cchSrc == -1)
    {
      cchSrc = PAL_wcslen(lpSrcStr);
    }

    /*
    * Loop through each character of the source string and update
    * lpCharType accordingly.
    */
    for(i = 0; i < cchSrc; i++)
    {
        wcstr = lpSrcStr[i];
#if HAVE_COREFOUNDATION
        lpCharType[i] = 0;
        if (PAL_iswlower(wcstr))
        {
            lpCharType[i] |= C1_LOWER;
        }
        if (PAL_iswupper(wcstr))
        {
            lpCharType[i] |= C1_UPPER;
        }
        if (PAL_iswalpha(wcstr))
        {
            lpCharType[i] |= C1_ALPHA;
        }
        if (PAL_iswdigit(wcstr))
        {
            lpCharType[i] |= C1_DIGIT;
        }
        if (PAL_iswspace(wcstr))
        {
            lpCharType[i] |= C1_SPACE;
        }
        if (PAL_iswblank(wcstr))
        {
            lpCharType[i] |= C1_BLANK;
        }
        if (PAL_iswcntrl(wcstr))
        {
            lpCharType[i] |= C1_CNTRL;
        }
        if (PAL_iswpunct(wcstr))
        {
            lpCharType[i] |= C1_PUNCT;
        }
#else /* HAVE_COREFOUNDATION */
        /*
         * Get the unicode data record for that character.
         */
        if(GetUnicodeData(wcstr, &unicodeDataRec))
        {
            lpCharType[i] = unicodeDataRec.C1_TYPE_FLAGS;
        }
        else
        {
            lpCharType[i] = 0;
        }
#endif /* HAVE_COREFOUNDATION */
    }

    GetStringTypeExExit:
    LOGEXIT("GetStringTypeEx returns BOOL %d\n", bRet);
    PERF_EXIT(GetStringTypeExW);
    return bRet;
}
#endif // ENABLE_DOWNLEVEL_FOR_NLS

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
#if !HAVE_COREFOUNDATION
    LPSTR lpLCType = NULL;
    LPSTR lpCurrentUnixLCType = NULL;
#else /* HAVE_COREFOUNDATION */
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
    if ( (CP_ACP == CodePage && 1252 == GetACP()) || 1252 == CodePage )
    {
        UINT nIndex = 0;

        if ( cbMultiByte == -1)
        {
            cbMultiByte = strlen(lpMultiByteStr) + 1;
        }

        if (cchWideChar == 0)
        {
            retval = cbMultiByte;
            goto EXIT;
        }

        if ( cbMultiByte > cchWideChar )
        {
            ERROR( "The output buffer is too small\n" );
            SetLastError( ERROR_INSUFFICIENT_BUFFER );
            retval = 0;
            goto EXIT;
        }

        for (nIndex=0; nIndex < cbMultiByte; nIndex++ )
        {
            /* we must implicitely convert lpMultiByteStr[nIndex]. 
                  here what happens : lpMultiByteStr contains chars (which are signed), 
                  we have to prevent values above 127 from becoming negative numbers */
            lpWideCharStr[nIndex] =  PAL_CP_1252[ (unsigned char)lpMultiByteStr[nIndex] ];
        }

        retval = nIndex;
        goto EXIT;
    }
    else 
    {
        wchar_t wchar_temp;
        int num_wchars;

        if ( CodePage == CP_ACP || CodePage == GetACP() )
        {
            /* Need the read lock */
            if ( !CODEPAGEAcquireReadLock() )
            {
                /* 
                * Could not get the readlock. 
                * Errors printed in the helper function.
                */  
                SetLastError( ERROR_INTERNAL_ERROR );
                goto EXIT;
            }
        }
        else
        {
            const CP_MAPPING * lpCPStruct;

            /* We require the write lock. */
            if ( !CODEPAGEAcquireWriteLock() )
            {
                /* 
                * Could not get a write lock.
                * Errors printed in the helper function.
                */  
                SetLastError( ERROR_INTERNAL_ERROR );
                goto EXIT;
            }

            lpLCType = setlocale( LC_CTYPE, NULL );
            if (lpLCType)
            {
                lpCurrentUnixLCType = PAL__strdup(lpLCType);
                if (NULL == lpCurrentUnixLCType)
                {
                    ERROR( "Cannot allocate buffer for storing current locale string\n" );
                    SetLastError( ERROR_INTERNAL_ERROR );
                    goto ReleaseLock;
                }
            }

            if( NULL != ( lpCPStruct = CODEPAGEGetData( CodePage ) ) )
            {
                if ( NULL == setlocale( LC_CTYPE, lpCPStruct->lpBSDEquivalent ) )
                {
                    /* Error. Locale not supported. */
                    ERROR( "This locale code page is not in the system.\n" );
                    SetLastError( ERROR_INVALID_PARAMETER );
                    goto ReleaseLock;
                }
            }
            else
            {
                ERROR( "This locale code page is not in the system.\n" );
                SetLastError( ERROR_INVALID_PARAMETER );
                goto ReleaseLock;
            }
    }

        /* if no byte count is specified, figure it out ourselves */
        if (cbMultiByte == -1)
        {
            /* cbMultiByte is in bytes, not in characters. don't use _mbslen */
            cbMultiByte = strlen(lpMultiByteStr)+1;
        }

        num_wchars = 0;
        while(0<cbMultiByte && (num_wchars<cchWideChar || 0 == cchWideChar))
        {
            int bytes_processed;

            /* mbtowc will not convert '\0', so do it manually */
            if('\0' == *lpMultiByteStr)
            {
                if(0 != cchWideChar)
                {
                    lpWideCharStr[num_wchars] = 0;
                }
                num_wchars++;
                cbMultiByte--;
                lpMultiByteStr++;
                continue;
            } 
            bytes_processed = mbtowc(&wchar_temp,lpMultiByteStr,cbMultiByte);
            if(0 >= bytes_processed)
            {
                ASSERT("mbtowc() returned unexpected value %d\n", bytes_processed);
                SetLastError(ERROR_INTERNAL_ERROR);
                goto ReleaseLock;
            }
            cbMultiByte-=bytes_processed;
            if(0 != cchWideChar)
            {
                lpWideCharStr[num_wchars] = (WCHAR)wchar_temp;
            }
            num_wchars++;
            lpMultiByteStr+=bytes_processed;
        }

        if (0 != cbMultiByte)
        {
            ERROR("conversion failed : insufficient buffer\n");
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            retval = 0;
        }
        else
        {
            retval = num_wchars;
        }
    }

ReleaseLock:

    if ( CP_ACP != CodePage && GetACP() != CodePage )
    {
        if ( NULL == setlocale( LC_CTYPE, lpCurrentUnixLCType ) )
        {
            ASSERT( "Unable to reset the original code!!!\n" );
            SetLastError( ERROR_INTERNAL_ERROR );
        }
    }
    if( !CODEPAGEReleaseLock() )
    {
        ERROR( "Unable to release the readwrite lock\n" );
    }
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

#if !HAVE_COREFOUNDATION
    if (lpCurrentUnixLCType)
    PAL_free(lpCurrentUnixLCType);
#endif /* !HAVE_COREFOUNDATION */

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
#if !HAVE_COREFOUNDATION
    LPSTR lpLCType = NULL;
    LPSTR lpCurrentUnixLCType = NULL;
#else /* !HAVE_COREFOUNDATION */
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
#if !HAVE_COREFOUNDATION
    if ((CodePage == CP_ACP && 1252 == GetACP())  || 1252 == CodePage )
    {
        UINT nIndex = 0;

        if ( cchWideChar == -1)
        {
            cchWideChar = PAL_wcslen(lpWideCharStr) + 1; 
        }

        if (cbMultiByte == 0)
        {
            /* cbMultiByte is 0, we must return the length of 
            the destination buffer in bytes */
            retval = cchWideChar; 
            goto EXIT;
        }

        if ( cchWideChar > cbMultiByte )
        {
            ERROR( "The output buffer is too small\n" );
            SetLastError( ERROR_INSUFFICIENT_BUFFER );
            retval = 0;
            goto EXIT;
        }

        /* perform a reverse lookup on the PAL_CP_1252 table */
        for (nIndex=0 ; nIndex < cchWideChar; nIndex++ )
        {  
            int i;
            if ((lpWideCharStr[nIndex] < 0x80) || 
            (lpWideCharStr[nIndex] >= 0xA0 && lpWideCharStr[nIndex] <= 0xFF))
            {
                lpMultiByteStr[nIndex] = (unsigned char) lpWideCharStr[nIndex];
            }
            else
            {
                for(i=0x80;i<0xA0;i++)
                {
                    if( lpWideCharStr[nIndex] == PAL_CP_1252[i])
                    {
                        break;
                    }
                }
                if (i == 0xA0)
                {
                    TRACE("Unable to convert wide character 0x%x, using \'%c\'\n", 
                    lpWideCharStr[nIndex], defaultChar);
                    lpMultiByteStr[nIndex] = defaultChar;
                    usedDefaultChar = TRUE;
                }
                else
                {
                    lpMultiByteStr[nIndex] = i;
                }
            }
        }
         
        retval = nIndex;
        goto EXIT;
    } 
    else
    {
        int num_bytes;
        char temp_bytes[8]; /* should always be enough */

        if ( CodePage == CP_ACP || CodePage == GetACP() )
        {
            /* Need the read lock */
            if ( !CODEPAGEAcquireReadLock() )
            {
                /* 
                * Could not get the readlock. 
                * Errors printed in the helper function.
                */  
                SetLastError( ERROR_INTERNAL_ERROR );
                goto EXIT;
            }
        }        
        else
        {
            const CP_MAPPING * lpCPStruct;

            /* We require the write lock. */
            if ( !CODEPAGEAcquireWriteLock() )
                {
                /* 
                * Could not get the writelock. 
                * Errors printed in the helper function.
                */  
                SetLastError( ERROR_INTERNAL_ERROR );
                goto EXIT;
            }

            lpLCType = setlocale( LC_CTYPE, NULL );
            if (lpLCType)
            {
                lpCurrentUnixLCType = PAL__strdup(lpLCType);
                if (NULL == lpCurrentUnixLCType)
                {
                    ERROR( "Cannot allocate buffer for storing current locale string.\n" );
                    SetLastError( ERROR_INTERNAL_ERROR );
                    goto ReleaseLock;
                }
            }

            if( NULL != ( lpCPStruct = CODEPAGEGetData( CodePage ) ) )
            {
                if ( NULL == setlocale( LC_CTYPE, lpCPStruct->lpBSDEquivalent ) )
                {
                    /* Error. Locale not supported. */
                    ERROR( "This locale code page is not in the system.\n" );
                    SetLastError( ERROR_INVALID_PARAMETER );
                    goto ReleaseLock;
                }
            }
            else
            {
                ERROR( "This locale code page is not in the system.\n" );
                SetLastError( ERROR_INVALID_PARAMETER );
                goto ReleaseLock;
            }
        }

        if (cchWideChar == -1)
        {
            cchWideChar = PAL_wcslen(lpWideCharStr) + 1;
        }

        num_bytes = 0;
        while(0<cchWideChar && (num_bytes < cbMultiByte || 0 == cbMultiByte))
        {
            int bytes_processed;
            int i;

            bytes_processed = wctomb(temp_bytes, (wchar_t)*lpWideCharStr);
            if(0 >= bytes_processed)
            {
                TRACE("Unable to convert wide character 0x%x, using \'%c\'\n", 
                      *lpWideCharStr, defaultChar);
                bytes_processed = 1;
                temp_bytes[0] = defaultChar;
                usedDefaultChar = TRUE;
            }
            if(bytes_processed > 2)
            {
                ASSERT("wchar expands to more than 2 bytes!?\n");
                SetLastError(ERROR_INTERNAL_ERROR);
                goto ReleaseLock;
            }
            if( 0 != cbMultiByte )
            {
                if( bytes_processed+num_bytes > cbMultiByte )
                {
                    /* not enough room! */
                    break;
                }
                for(i=0; i<bytes_processed; i++)
                {
                lpMultiByteStr[i] = temp_bytes[i];
                }
                lpMultiByteStr += bytes_processed;
            }

            num_bytes += bytes_processed;

            cchWideChar--;
            lpWideCharStr++;
        }

        if (0 != cchWideChar)
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            retval = 0;
        }
        else
        {
            retval = num_bytes;
        }
    }

ReleaseLock:
    if ( CP_ACP != CodePage && GetACP() != CodePage )
    {
        if ( NULL == setlocale( LC_CTYPE, lpCurrentUnixLCType ) )
        {
            ASSERT( "Unable to reset the original code!!!\n" );
            SetLastError( ERROR_INTERNAL_ERROR );
        }
    }
    if( !CODEPAGEReleaseLock() )
    {
        ERROR( "Unable to release the readwrite lock\n" );
    }
#else /* !HAVE_COREFOUNDATION */
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
#endif /* !HAVE_COREFOUNDATION */

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

#if !HAVE_COREFOUNDATION
    if (lpCurrentUnixLCType)
    PAL_free(lpCurrentUnixLCType);
#endif /* !HAVE_COREFOUNDATION */

    LOGEXIT("WideCharToMultiByte returns INT %d\n", retval);
    PERF_EXIT(WideCharToMultiByte);
    return retval;
}
