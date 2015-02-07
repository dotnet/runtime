//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    version.c

Abstract:

    Implementation of functions for getting platform.OS versions.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

#ifdef __APPLE__
#include <CoreFoundation/CoreFoundation.h>
#endif // __APPLE__

SET_DEFAULT_DEBUG_CHANNEL(MISC);

#ifdef __APPLE__
static DWORD /* PAL_ERROR */ CFStringToVers(CFStringRef versTupleStr, DWORD *pversTuple)
{
    CFIndex len = CFStringGetLength(versTupleStr);
    if (len == 0)
        return ERROR_INTERNAL_ERROR;
    
    DWORD vers = CFStringGetIntValue(versTupleStr);

    /* check to make sure it wasn't a failed conversion */
    CFStringRef convertBackStr = CFStringCreateWithFormat(kCFAllocatorDefault, NULL,
        CFSTR("%lu"), vers);
        
    if (!convertBackStr)
        return ERROR_NOT_ENOUGH_MEMORY;
    
    Boolean fSame = CFStringCompare(versTupleStr, convertBackStr, 0) == kCFCompareEqualTo;
    CFRelease(convertBackStr);
    if (!fSame)
        return ERROR_INTERNAL_ERROR;
    
    *pversTuple = vers;
    return ERROR_SUCCESS;
}

static DWORD /* PAL_ERROR */ GetSystemVersion(DWORD *major, DWORD *minor, DWORD *build, 
    CFStringRef *serviceRelease)
{
    *major = *minor = *build = 0;

    CFURLRef systemVersPath = CFURLCreateWithFileSystemPath(kCFAllocatorDefault, 
        CFSTR("/System/Library/CoreServices/SystemVersion.plist"), kCFURLPOSIXPathStyle, false);
    if (systemVersPath == NULL)
        return ERROR_INTERNAL_ERROR;
    CFDataRef systemVersData;
    OSStatus status;
    Boolean fCreated = CFURLCreateDataAndPropertiesFromResource(kCFAllocatorDefault, 
        systemVersPath, &systemVersData, NULL, NULL, &status);
    CFRelease(systemVersPath);
    if (!fCreated)
        return ERROR_INTERNAL_ERROR;
        
    CFStringRef errorStr;
    CFPropertyListRef systemVersPlist = CFPropertyListCreateFromXMLData(kCFAllocatorDefault, 
        systemVersData, kCFPropertyListImmutable, &errorStr);
    CFRelease(systemVersData);
    if (systemVersPlist == NULL)
    {
        CFRelease(errorStr);
        return ERROR_INTERNAL_ERROR;
    }
    CFDictionaryRef dictRef = (CFDictionaryRef)systemVersPlist;
    CFStringRef productVersion = (CFStringRef)CFDictionaryGetValue(dictRef, CFSTR("ProductVersion"));
    if (!productVersion || CFGetTypeID(productVersion) != CFStringGetTypeID())
    {
        CFRelease(dictRef);
        return ERROR_INTERNAL_ERROR;
    }
    CFArrayRef versList = CFStringCreateArrayBySeparatingStrings(kCFAllocatorDefault, 
        productVersion, CFSTR("."));
    if (versList == NULL)
    {
        CFRelease(dictRef);
        return ERROR_INTERNAL_ERROR;
    }
    DWORD palError;
    switch (CFArrayGetCount(versList))
    {
    default: /* 3 or more */
        palError = CFStringToVers((CFStringRef)CFArrayGetValueAtIndex(versList, 2), build);
        if (palError != ERROR_SUCCESS)
            break;
        /* fall thru */
    case 2:
        palError = CFStringToVers((CFStringRef)CFArrayGetValueAtIndex(versList, 1), minor);
        if (palError != ERROR_SUCCESS)
            break;
        /* fall thru */
    case 1:
        palError = CFStringToVers((CFStringRef)CFArrayGetValueAtIndex(versList, 0), major);
        break;
    }
    CFRelease(versList);
    if (palError != ERROR_SUCCESS)
    {
        CFRelease(dictRef);
        return palError;
    }
    
    CFStringRef productBuildVersion = (CFStringRef)CFDictionaryGetValue(dictRef, 
        CFSTR("ProductBuildVersion"));
    if (productBuildVersion == NULL || CFGetTypeID(productBuildVersion) != CFStringGetTypeID())
        *serviceRelease = CFSTR("");
    else
        *serviceRelease = productBuildVersion;
    CFRetain(*serviceRelease);
    CFRelease(dictRef);

    return ERROR_SUCCESS;    
}
#endif // __APPLE__


/*++
Function:
  GetVersionExA



GetVersionEx

The GetVersionEx function obtains extended information about the
version of the operating system that is currently running.

Parameters

lpVersionInfo 
       [in/out] Pointer to an OSVERSIONINFO data structure that the
       function fills with operating system version information.

       Before calling the GetVersionEx function, set the
       dwOSVersionInfoSize member of the OSVERSIONINFO data structure
       to sizeof(OSVERSIONINFO).

Return Values

If the function succeeds, the return value is a nonzero value.

If the function fails, the return value is zero. To get extended error
information, call GetLastError. The function fails if you specify an
invalid value for the dwOSVersionInfoSize member of the OSVERSIONINFO
structure.

--*/
BOOL
PALAPI
GetVersionExA(
	      IN OUT LPOSVERSIONINFOA lpVersionInformation)
{
    BOOL bRet = TRUE;
    PERF_ENTRY(GetVersionExA);
    ENTRY("GetVersionExA (lpVersionInformation=%p)\n", lpVersionInformation);

    if (lpVersionInformation->dwOSVersionInfoSize == sizeof(OSVERSIONINFOA))
    {
#ifdef __APPLE__
        lpVersionInformation->dwPlatformId = VER_PLATFORM_MACOSX;

        CFStringRef serviceRelease;
        DWORD palError = GetSystemVersion(
            &lpVersionInformation->dwMajorVersion,
            &lpVersionInformation->dwMinorVersion,
            &lpVersionInformation->dwBuildNumber,
            &serviceRelease);
        // If we fail to get the major/minor/build, we consider it a hard error,
        // but if we cannot acquire the "Service Release", we will not fail, but
        // merely return an empty string.
        if (palError == ERROR_SUCCESS)
        {
            CFIndex len = CFStringGetLength(serviceRelease);
            if (len > static_cast<int>(sizeof(lpVersionInformation->szCSDVersion)) - 1)
                len = sizeof(lpVersionInformation->szCSDVersion) - 1;
            if (!CFStringGetCString(serviceRelease, lpVersionInformation->szCSDVersion,
                sizeof(lpVersionInformation->szCSDVersion), kCFStringEncodingUTF8))
                lpVersionInformation->szCSDVersion[0] = 0;
            CFRelease(serviceRelease);
        }
        else
        {
            SetLastError(palError);
            bRet = FALSE;
        }
#else // __APPLE__
        lpVersionInformation->dwMajorVersion = 5;       /* same as WIN2000 */
        lpVersionInformation->dwMinorVersion = 0;       /* same as WIN2000 */
        lpVersionInformation->dwBuildNumber = 0;
        lpVersionInformation->dwPlatformId = VER_PLATFORM_UNIX;
        lpVersionInformation->szCSDVersion[0] = '\0'; /* no service pack */
#endif // __APPLE__ else
    } 
    else 
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        bRet = FALSE;
    }
    LOGEXIT("GetVersionExA returning BOOL %d\n", bRet);
    PERF_EXIT(GetVersionExA);
    return bRet;
}


/*++
Function:
  GetVersionExW

See GetVersionExA
--*/
BOOL
PALAPI
GetVersionExW(
	      IN OUT LPOSVERSIONINFOW lpVersionInformation)
{
    BOOL bRet = TRUE;

    PERF_ENTRY(GetVersionExW);
    ENTRY("GetVersionExW (lpVersionInformation=%p)\n", lpVersionInformation);

    if (lpVersionInformation->dwOSVersionInfoSize == sizeof(OSVERSIONINFOW))
    {
#ifdef __APPLE__
        lpVersionInformation->dwPlatformId = VER_PLATFORM_MACOSX;

        CFStringRef serviceRelease;
        DWORD palError = GetSystemVersion(
            &lpVersionInformation->dwMajorVersion,
            &lpVersionInformation->dwMinorVersion,
            &lpVersionInformation->dwBuildNumber,
            &serviceRelease);
        // If we fail to get the major/minor/build, we consider it a hard error,
        // but if we cannot acquire the "Service Release", we will not fail, but
        // merely return an empty string.
        if (palError == ERROR_SUCCESS)
        {
            CFIndex len = CFStringGetLength(serviceRelease);
            if (len > static_cast<int>((sizeof(lpVersionInformation->szCSDVersion)/2) - 1))
                len = (sizeof(lpVersionInformation->szCSDVersion)/2) - 1;
            CFStringGetCharacters(serviceRelease, CFRangeMake(0, len), 
                (UniChar*)lpVersionInformation->szCSDVersion);
            lpVersionInformation->szCSDVersion[len] = 0;
            CFRelease(serviceRelease);
        }
        else
        {
            SetLastError(palError);
            bRet = FALSE;
        }
#else // __APPLE__
        lpVersionInformation->dwMajorVersion = 5;       /* same as WIN2000 */
        lpVersionInformation->dwMinorVersion = 0;       /* same as WIN2000 */
        lpVersionInformation->dwBuildNumber = 0;
        lpVersionInformation->dwPlatformId = VER_PLATFORM_UNIX;
        lpVersionInformation->szCSDVersion[0] = '\0'; /* no service pack */
#endif // __APPLE__ else
    } 
    else 
    {
        SetLastError(ERROR_INVALID_PARAMETER);
        bRet =  FALSE;
    }
    LOGEXIT("GetVersionExW returning BOOL %d\n", bRet);
    PERF_EXIT(GetVersionExW);
    return bRet;
}
