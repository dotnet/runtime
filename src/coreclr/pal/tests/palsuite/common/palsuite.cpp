// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================================
**
** Source:  palsuite.cpp
**
** Purpose: Define constants and implement functions that are useful to
**          multiple function categories.
**
**==========================================================================*/


#include "palsuite.h"
#include <dn-u16.h>
#include "minipal/time.h"
#include "minipal/utf8.h"

const char* szTextFile = "text.txt";

HANDLE hToken[NUM_TOKENS];

WCHAR* convert(const char * aString)
{
    WCHAR* wideBuffer = nullptr;

    if (aString != nullptr)
    {
        size_t length = strlen(aString) + 1;
        size_t size = minipal_get_length_utf8_to_utf16(aString, length, 0);
        wideBuffer = (WCHAR*) malloc(size*sizeof(WCHAR));
        if (wideBuffer == NULL)
        {
            Fail("ERROR: Unable to allocate memory!\n");
        }
        minipal_convert_utf8_to_utf16(aString, length, (CHAR16_T*)wideBuffer, size, 0);
    }

    return wideBuffer;
}

char* convertC(const WCHAR * wString)
{
    size_t length = u16_strlen(wString) + 1;
    size_t size = minipal_get_length_utf16_to_utf8((const CHAR16_T*)wString, length, 0);
    char * MultiBuffer = (char*) malloc(size);
    if (MultiBuffer == NULL)
    {
        Fail("ERROR: Unable to allocate memory!\n");
    }
    minipal_convert_utf16_to_utf8((const CHAR16_T*)wString, length, MultiBuffer, size, 0);
    return MultiBuffer;
}

static const char* rgchPathDelim = "/";


int
mkAbsoluteFilename( LPSTR dirName,
                    DWORD dwDirLength,
                    LPCSTR fileName,
                    DWORD dwFileLength,
                    LPSTR absPathName )
{
    DWORD sizeDN, sizeFN, sizeAPN;

    sizeDN = strlen( dirName );
    sizeFN = strlen( fileName );
    sizeAPN = (sizeDN + 1 + sizeFN + 1);

    /* ensure ((dirName + DELIM + fileName + \0) =< MAX_PATH ) */
    if( sizeAPN > MAX_PATH )
    {
        return ( 0 );
    }

    strncpy( absPathName, dirName, dwDirLength +1 );
    strncpy( absPathName, rgchPathDelim, 2 );
    strncpy( absPathName, fileName, dwFileLength +1 );

    return (sizeAPN);

}


BOOL CleanupHelper (HANDLE *hArray, DWORD dwIndex)
{
    BOOL bCHRet;

    bCHRet = CloseHandle(hArray[dwIndex]);
    if (!bCHRet)
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%u'.\n", hArray[dwIndex],
              GetLastError());
    }

    return (bCHRet);
}

BOOL Cleanup(HANDLE *hArray, DWORD dwIndex)
{
    BOOL bCRet;
    BOOL bCHRet = 0;

    while (--dwIndex > 0)
    {
        bCHRet = CleanupHelper(&hArray[0], dwIndex);
    }

    bCRet = CloseHandle(hArray[0]);
    if (!bCRet)
    {
        Trace("PALSUITE ERROR: Unable to execute CloseHandle(%p) during "
              "clean up.\nGetLastError returned '%u'.\n", hArray[dwIndex],
              GetLastError());
    }

    return (bCRet&&bCHRet);
}

/*
 * Take two wide strings representing file and directory names
 * (dirName, fileName), join the strings with the appropriate path
 * delimiter and populate a wide character buffer (absPathName) with
 * the resulting string.
 *
 * Returns: The number of wide characters in the resulting string.
 * 0 is returned on Error.
 */
int
mkAbsoluteFilenameA (
    LPSTR dirName,
    DWORD dwDirLength,
    LPCSTR fileName,
    DWORD dwFileLength,
    LPSTR absPathName )
{
    const char *szPathDelimA = "\\";

    DWORD sizeDN;
    DWORD sizeFN;
    DWORD sizeAPN;

    sizeDN = strlen( dirName );
    sizeFN = strlen( fileName );
    sizeAPN = (sizeDN + 1 + sizeFN + 1);

    /* ensure ((dirName + DELIM + fileName + \0) =< MAX_PATH ) */
    if ( sizeAPN > MAX_PATH )
    {
        return ( 0 );
    }

    strncpy(absPathName, dirName, dwDirLength +1);
    strcat(absPathName, szPathDelimA);
    strcat(absPathName, fileName);

    return (sizeAPN);

}
