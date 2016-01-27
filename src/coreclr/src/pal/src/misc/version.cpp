// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    version.c

Abstract:

    Implementation of functions for getting platform.OS versions.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"

SET_DEFAULT_DEBUG_CHANNEL(MISC);

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
        lpVersionInformation->dwMajorVersion = 5;       /* same as WIN2000 */
        lpVersionInformation->dwMinorVersion = 0;       /* same as WIN2000 */
        lpVersionInformation->dwBuildNumber = 0;
        lpVersionInformation->dwPlatformId = VER_PLATFORM_UNIX;
        lpVersionInformation->szCSDVersion[0] = '\0'; /* no service pack */
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
        lpVersionInformation->dwMajorVersion = 5;       /* same as WIN2000 */
        lpVersionInformation->dwMinorVersion = 0;       /* same as WIN2000 */
        lpVersionInformation->dwBuildNumber = 0;
        lpVersionInformation->dwPlatformId = VER_PLATFORM_UNIX;
        lpVersionInformation->szCSDVersion[0] = '\0'; /* no service pack */
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
