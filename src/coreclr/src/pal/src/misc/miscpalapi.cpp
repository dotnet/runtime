//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

/*++



Module Name:

    miscpalapi.c

Abstract:

    Implementation misc PAL APIs

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/module.h"
#include "pal/malloc.hpp"

#include <errno.h>
#include <unistd.h> 
#include <time.h>
#include <pthread.h>
#include <dlfcn.h>

#ifdef __APPLE__
#include <mach-o/dyld.h>
#endif // __APPLE__

SET_DEFAULT_DEBUG_CHANNEL(MISC);

static const char RANDOM_DEVICE_NAME[] ="/dev/random";
static const char URANDOM_DEVICE_NAME[]="/dev/urandom";

/*++

Function :

    PAL_GetPALDirectoryW
    
    Returns the fully qualified path name
    where the PALL DLL was loaded from.
    
    On failure it returns FALSE and sets the 
    proper LastError code.
    
See rotor_pal.doc for more details.

--*/
BOOL 
PALAPI
PAL_GetPALDirectoryW( OUT LPWSTR lpDirectoryName, IN UINT cchDirectoryName ) 
{
    LPWSTR lpFullPathAndName = NULL;
    LPWSTR lpEndPoint = NULL;
    BOOL bRet = FALSE;

    PERF_ENTRY(PAL_GetPALDirectoryW);
    ENTRY( "PAL_GetPALDirectoryW( %p, %d )\n", lpDirectoryName, cchDirectoryName );

    MODSTRUCT *module = LOADGetPalLibrary();
    if (!module)
    {
        SetLastError(ERROR_INTERNAL_ERROR);
        goto EXIT;
    }
    lpFullPathAndName = module->lib_name;
    if (lpFullPathAndName == NULL)
    {
        SetLastError(ERROR_INTERNAL_ERROR);
        goto EXIT;
    }
    lpEndPoint = PAL_wcsrchr( lpFullPathAndName, '/' );
    if ( lpEndPoint )
    {
        /* The path that we return is required to have
           the trailing slash on the end.*/
        lpEndPoint++;
    }
    if ( lpFullPathAndName && lpEndPoint && *lpEndPoint != '\0' )
    {
        while ( cchDirectoryName - 1 && lpFullPathAndName != lpEndPoint )
        {
            *lpDirectoryName = *lpFullPathAndName;
            lpFullPathAndName++;
            lpDirectoryName++;
            cchDirectoryName--;
        }
            
        if ( lpFullPathAndName == lpEndPoint )
        {
            *lpDirectoryName = '\0';
            bRet = TRUE;
            goto EXIT;
        }
        else
        {
            ASSERT( "The buffer was not large enough.\n" );
            SetLastError( ERROR_INSUFFICIENT_BUFFER );
            goto EXIT;
        }
    }
    else
    {
        ASSERT( "Unable to determine the path.\n" );
    }
    
    /* Error path, should not be executed. */
    SetLastError( ERROR_INTERNAL_ERROR );
EXIT:    
    LOGEXIT( "PAL_GetPALDirectoryW returns BOOL %d.\n", bRet);
    PERF_EXIT(PAL_GetPALDirectoryW);
    return bRet;
}

PALIMPORT
BOOL
PALAPI
PAL_GetPALDirectoryA(
             OUT LPSTR lpDirectoryName,
             IN UINT cchDirectoryName)
{
    BOOL bRet;
    WCHAR PALDirW[_MAX_PATH];

    PERF_ENTRY(PAL_GetPALDirectoryA);
    ENTRY( "PAL_GetPALDirectoryA( %p, %d )\n", lpDirectoryName, cchDirectoryName );

    bRet = PAL_GetPALDirectoryW(PALDirW, _MAX_PATH);
    if (bRet) {
        if (WideCharToMultiByte(CP_ACP, 0, 
            PALDirW, -1, lpDirectoryName, cchDirectoryName, NULL, 0)) {
            bRet = TRUE;
        } else {
            bRet = FALSE;
        }
    }

    LOGEXIT( "PAL_GetPALDirectoryW returns BOOL %d.\n", bRet);
    PERF_EXIT(PAL_GetPALDirectoryA);
    return bRet;
}

// Define _BitScanForward64 and BitScanForward
// Per MSDN, BitScanForward64 will search the mask data from LSB to MSB for a set bit.
// If one is found, its bit position is returned in the outPDWORD argument and 1 is returned.
// Otherwise, 0 is returned.
//
// On GCC, the equivalent function is __builtin_ffsl. It returns 1+index of the least
// significant set bit, or 0 if if mask is zero.
unsigned char
PALAPI
BitScanForward64(
        IN OUT PDWORD Index,
        IN UINT64 qwMask)
{
    unsigned char bRet = FALSE;
    int iIndex = __builtin_ffsl(qwMask);
    if (iIndex != 0)
    {
        // Set the Index after deducting unity
        *Index = (DWORD)(iIndex-1);
        bRet = TRUE;
    }

    return bRet;
}

// On GCC, the equivalent function is __builtin_ffs. It returns 1+index of the least
// significant set bit, or 0 if if mask is zero.
unsigned char
PALAPI
BitScanForward(
        IN OUT PDWORD Index,
        IN UINT wMask)
{
    unsigned char bRet = FALSE;
    int iIndex = __builtin_ffs(wMask);
    if (iIndex != 0)
    {
        // Set the Index after deducting unity
        *Index = (DWORD)(iIndex-1);
        bRet = TRUE;
    }
    
    return bRet;
}

BOOL
PALAPI
PAL_Random(
        IN BOOL bStrong,
        IN OUT LPVOID lpBuffer,
        IN DWORD dwLength)
{
    int rand_des = -1;
    BOOL bRet = FALSE;
    DWORD i;
    char buf;
    long num = 0;
    static BOOL sMissingDevRandom;
    static BOOL sMissingDevURandom;
    static BOOL sInitializedMRand;

    PERF_ENTRY(PAL_Random);
    ENTRY("PAL_Random(bStrong=%d, lpBuffer=%p, dwLength=%d)\n", 
          bStrong, lpBuffer, dwLength);

    i = 0;

    if (bStrong == TRUE && i < dwLength && !sMissingDevRandom)
    {
        // request non-blocking access to avoid hangs if the /dev/random is exhausted
        // or just simply broken
        if ((rand_des = PAL__open(RANDOM_DEVICE_NAME, O_RDONLY | O_NONBLOCK)) == -1)
        {
            if (errno == ENOENT)
            {
                sMissingDevRandom = TRUE;
            }
            else
            {
                ASSERT("PAL__open() failed, errno:%d (%s)\n", errno, strerror(errno));
            }

            // Back off and try /dev/urandom.
        }
        else
        {
            for( ; i < dwLength; i++)
            {
                if (read(rand_des, &buf, 1) < 1)
                {
                    // the /dev/random pool has been exhausted.  Fall back
                    // to /dev/urandom for the remainder of the buffer.
                    break;
                }

                *(((BYTE*)lpBuffer) + i) ^= buf;
            }

            close(rand_des);
        }
    }
 
    if (i < dwLength && !sMissingDevURandom)
    {
        if ((rand_des = PAL__open(URANDOM_DEVICE_NAME, O_RDONLY)) == -1)
        {
            if (errno == ENOENT)
            {                
                sMissingDevURandom = TRUE;                
            }
            else
            {
                ASSERT("PAL__open() failed, errno:%d (%s)\n", errno, strerror(errno));               
            }

            // Back off and try mrand48.           
        }
        else
        {
            for( ; i < dwLength; i++)
            {
                if (read(rand_des, &buf, 1) < 1)
                {
                    // Fall back to srand48 for the remainder of the buffer.
                    break;
                }

                *(((BYTE*)lpBuffer) + i) ^= buf;
            }

            close(rand_des);
        }
    }    

    if (!sInitializedMRand)
    {
        srand48(time(NULL));
        sInitializedMRand = TRUE;
    }

    // always xor srand48 over the whole buffer to get some randomness
    // in case /dev/random is not really random

    for(i = 0; i < dwLength; i++)
    {
        if (i % sizeof(long) == 0) {
            num = mrand48();
        }

        *(((BYTE*)lpBuffer) + i) ^= num;
        num >>= 8;
    }

    bRet = TRUE;

    LOGEXIT("PAL_Random returns %d\n", bRet);
    PERF_EXIT(PAL_Random);
    return bRet;
}

