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

const char* szTextFile = "text.txt";

HANDLE hToken[NUM_TOKENS];
CRITICAL_SECTION CriticalSection;

WCHAR* convert(const char * aString) 
{
    WCHAR* wideBuffer = nullptr;

    if (aString != nullptr)
    {
        int size = MultiByteToWideChar(CP_ACP,0,aString,-1,NULL,0);
        wideBuffer = (WCHAR*) malloc(size*sizeof(WCHAR));
        if (wideBuffer == NULL)
        {
            Fail("ERROR: Unable to allocate memory!\n");
        }
        MultiByteToWideChar(CP_ACP,0,aString,-1,wideBuffer,size);
    }

    return wideBuffer;
}

char* convertC(const WCHAR * wString) 
{
    int size;
    char * MultiBuffer = NULL;

    size = WideCharToMultiByte(CP_ACP,0,wString,-1,MultiBuffer,0,NULL,NULL);
    MultiBuffer = (char*) malloc(size);
    if (MultiBuffer == NULL)
    {
        Fail("ERROR: Unable to allocate memory!\n");
    }
    WideCharToMultiByte(CP_ACP,0,wString,-1,MultiBuffer,size,NULL,NULL);
    return MultiBuffer;
}

UINT64 GetHighPrecisionTimeStamp(LARGE_INTEGER performanceFrequency)
{
    LARGE_INTEGER ts;
    if (!QueryPerformanceCounter(&ts))
    {
        Fail("ERROR: Unable to query performance counter!\n");      
    }
    
    return ts.QuadPart / (performanceFrequency.QuadPart / 1000);    
}

static const char* rgchPathDelim = "\\";


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

    /* ensure ((dirName + DELIM + fileName + \0) =< _MAX_PATH ) */
    if( sizeAPN > _MAX_PATH )
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
mkAbsoluteFilenameW ( 
    LPWSTR dirName,  
    DWORD dwDirLength, 
    LPCWSTR fileName, 
    DWORD dwFileLength,
    LPWSTR absPathName )
{
    const WCHAR szPathDelimW[] = {'\\','\0'};

    DWORD sizeDN, sizeFN, sizeAPN;

    sizeDN = wcslen( dirName );
    sizeFN = wcslen( fileName );
    sizeAPN = (sizeDN + 1 + sizeFN + 1);

    /* insure ((dirName + DELIM + fileName + \0) =< _MAX_PATH ) */
    if ( sizeAPN > _MAX_PATH )
    {
	return ( 0 );
    }
    
    wcsncpy(absPathName, dirName, dwDirLength +1);
    wcsncpy(absPathName, szPathDelimW, 2);
    wcsncpy(absPathName, fileName, dwFileLength +1);

    return (sizeAPN);

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
    
    /* insure ((dirName + DELIM + fileName + \0) =< _MAX_PATH ) */
    if ( sizeAPN > _MAX_PATH )
    {
        return ( 0 );
    }
    
    strncpy(absPathName, dirName, dwDirLength +1);
    strcat(absPathName, szPathDelimA);
    strcat(absPathName, fileName);
    
    return (sizeAPN);
  
} 

BOOL
DeleteFileW(
        IN LPCWSTR lpFileName)
{
    _ASSERTE(lpFileName != NULL);

    CHAR mbFileName[ _MAX_PATH ];

    if (WideCharToMultiByte( CP_ACP, 0, lpFileName, -1, mbFileName, sizeof(mbFileName), NULL, NULL ) != 0 )
    {
        return remove(mbFileName) == 0;
    }

    return FALSE;
}