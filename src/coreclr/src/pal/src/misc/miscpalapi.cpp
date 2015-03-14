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

extern char g_szCoreCLRPath[MAX_PATH];

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

    lpFullPathAndName = pal_module.lib_name;
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


// Support for EnsureOpenSslInitialized

static const char * const libcryptoName = "libcrypto" PAL_SHLIB_SUFFIX;

static void* g_OpenSslLib;
static pthread_mutex_t g_OpenSslInitLock = PTHREAD_MUTEX_INITIALIZER;
static pthread_mutex_t *g_OpenSslLocks;

#define CRYPTO_LOCK 1
typedef void(*locking_function)(int mode, int n, char* file, int line);
typedef int(*CRYPTO_num_locks)(void);
typedef void(*CRYPTO_set_locking_callback)(locking_function callback);

void LockingCallback(int mode, int n, char* file, int line)
{
    int result;
    if (mode & CRYPTO_LOCK)
    {
        result = pthread_mutex_lock(&g_OpenSslLocks[n]);
    }
    else
    {
        result = pthread_mutex_unlock(&g_OpenSslLocks[n]);
    }

    if (result != 0)
    {
        ASSERT("LockingCallback(%d, %d, %s, %d) failed with error %d \n",
            mode, n, file, line, result);
    }
}

/*++
Function:
  EnsureOpenSslInitialized

  Used by cryptographic libraries in CoreFX to initialize
  threading support in OpenSSL.

  --*/

DWORD
PALAPI
EnsureOpenSslInitialized()
{
    DWORD dwRet = 0;
    int numLocks;
    CRYPTO_num_locks numLocksFunc;
    CRYPTO_set_locking_callback setCallbackFunc;
    int locksInitialized = 0;

    PERF_ENTRY(EnsureOpenSslInitialized);
    ENTRY("EnsureOpenSslInitialized()\n");

    pthread_mutex_lock(&g_OpenSslInitLock);
    if (g_OpenSslLocks != NULL)
    {
        // Already initialized; nothing more to do.
        goto done;
    }

    // Open the libcrypto library
    g_OpenSslLib = dlopen(libcryptoName, RTLD_NOW);
    if (g_OpenSslLib == NULL)
    {
        ASSERT("Unable to load OpenSSL with dlerror \"%s\" \n", dlerror());
        dwRet = 1;
        goto done;
    }

    // Get the functions we need from OpenSSL
    numLocksFunc = (CRYPTO_num_locks) dlsym(g_OpenSslLib, "CRYPTO_num_locks");
    setCallbackFunc = (CRYPTO_set_locking_callback) dlsym(g_OpenSslLib, "CRYPTO_set_locking_callback");
    if (numLocksFunc == NULL || setCallbackFunc == NULL)
    {
        ASSERT("Unable to find CRYPTO_num_locks or CRYPTO_set_locking_callback\n");
        dwRet = 2;
        goto done;
    }

    // Determine how many locks are needed
    numLocks = numLocksFunc();
    if (numLocks <= 0)
    {
        ASSERT("CRYPTO_num_locks returned invalid value: %d\n", numLocks);
        dwRet = 3;
        goto done;
    }

    // Create the locks array
    g_OpenSslLocks = (pthread_mutex_t*) PAL_malloc(sizeof(pthread_mutex_t) * numLocks);
    if (g_OpenSslLocks == NULL)
    {
        ASSERT("PAL_malloc failed\n");
        dwRet = 4;
        goto done;
    }

    // Initialize each of the locks
    for (locksInitialized = 0; locksInitialized < numLocks; locksInitialized++)
    {
        if (pthread_mutex_init(&g_OpenSslLocks[locksInitialized], NULL) != 0)
        {
            ASSERT("pthread_mutex_init failed\n");
            dwRet = 5;
            goto done;
        }
    }

    // Initialize the callback
    setCallbackFunc((locking_function) LockingCallback);

done:
    pthread_mutex_unlock(&g_OpenSslInitLock);

    if (dwRet != 0)
    {
        // Cleanup on failure

        if (g_OpenSslLocks != NULL)
        {
            for (int i = locksInitialized - 1; i >= 0; i--)
            {
                if (pthread_mutex_destroy(&g_OpenSslLocks[i]) != 0)
                {
                    ASSERT("Unable to pthread_mutex_destroy while cleaning up\n");
                }
            }
            PAL_free(g_OpenSslLocks);
            g_OpenSslLocks = NULL;
        }

        if (g_OpenSslLib != NULL)
        {
            if (dlclose(g_OpenSslLib) != 0)
            {
                ASSERT("Unable to close OpenSSL with dlerror \"%s\" \n", dlerror());
            }
            g_OpenSslLib = NULL;
        }
    }

    // If successful, keep OpenSSL library open and initialized

    LOGEXIT("EnsureOpenSslInitialized returns DWORD %u\n", dwRet);
    PERF_EXIT(EnsureOpenSslInitialized);
    return dwRet;
}
