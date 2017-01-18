// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    disk.c

Abstract:

    Implementation of the disk information functions.

Revision History:



--*/

#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/file.h"
#include "pal/stackstring.hpp"

#include <sys/param.h>
#include <sys/mount.h>
#include <errno.h>
#if HAVE_STATVFS
#include <sys/types.h>
#include <sys/statvfs.h>
#define statfs  statvfs
#if STATVFS64_PROTOTYPE_BROKEN
typedef statvfs_t pal_statfs;
#else // STATVFS64_PROTOTYPE_BROKEN
typedef struct statvfs pal_statfs;
#endif // STATVFS64_PROTOTYPE_BROKEN
#else // HAVE_STATVFS
typedef struct statfs pal_statfs;
#endif  // HAVE_STATVFS

SET_DEFAULT_DEBUG_CHANNEL(FILE);

/*++

Function:

    GetDiskFreeSpaceW
    
See MSDN doc.
--*/
PALIMPORT
BOOL
PALAPI
GetDiskFreeSpaceW(
          LPCWSTR lpDirectoryName,
          LPDWORD lpSectorsPerCluster,
          LPDWORD lpBytesPerSector,
          LPDWORD lpNumberOfFreeClusters,  /* Caller will ignore output value */
          LPDWORD lpTotalNumberOfClusters) /* Caller will ignore output value */
{
    BOOL bRetVal = FALSE;
    pal_statfs fsInfoBuffer;
    INT  statfsRetVal = 0;
    DWORD dwLastError = NO_ERROR;
    PathCharString dirNameBufferPathString;
    size_t length;
    char * dirNameBuffer;
    int size;

    PERF_ENTRY(GetDiskFreeSpaceW);
    ENTRY( "GetDiskFreeSpaceW( lpDirectoryName=%p (%S), lpSectorsPerCluster=%p,"
           "lpBytesPerSector=%p, lpNumberOfFreeClusters=%p, "
           "lpTotalNumberOfClusters=%p )\n", lpDirectoryName ? lpDirectoryName :
            W16_NULLSTRING, lpDirectoryName ? lpDirectoryName :
            W16_NULLSTRING, lpSectorsPerCluster, lpBytesPerSector, 
            lpNumberOfFreeClusters, lpTotalNumberOfClusters );

    /* Sanity checks. */
    if ( !lpSectorsPerCluster )
    {
        ERROR( "lpSectorsPerCluster cannot be NULL!\n" );
        dwLastError = ERROR_INVALID_PARAMETER;
        goto exit;
    }
    if ( !lpBytesPerSector )
    {
        ERROR( "lpBytesPerSector cannot be NULL!\n" );
        dwLastError = ERROR_INVALID_PARAMETER;
        goto exit;
    }
    if ( lpNumberOfFreeClusters || lpTotalNumberOfClusters )
    {
        TRACE("GetDiskFreeSpaceW is ignoring lpNumberOfFreeClusters"
              " and lpTotalNumberOfClusters\n" );
    }
    if ( lpDirectoryName && PAL_wcslen( lpDirectoryName ) == 0 )
    {
        ERROR( "lpDirectoryName is empty.\n" );
        dwLastError = ERROR_INVALID_PARAMETER;
        goto exit;
    }

    /* Fusion uses this API to round file sizes up to their actual size
       on-disk based on the BytesPerSector * SectorsPerCluster.
       The intent is to avoid computing the sum of all file sizes in the
       cache just in bytes and not account for the cluster-sized slop, when
       determining if the cache is too large or not. */

    if ( lpDirectoryName )
    {
        length = (PAL_wcslen(lpDirectoryName)+1) * 3;
        dirNameBuffer = dirNameBufferPathString.OpenStringBuffer(length);
        if (NULL == dirNameBuffer)
        {
            dwLastError = ERROR_NOT_ENOUGH_MEMORY;
            goto exit;
        }

        size = WideCharToMultiByte( CP_ACP, 0, lpDirectoryName, -1,
                                  dirNameBuffer,length, 0, 0 );
        dirNameBufferPathString.CloseBuffer(size);
        if ( size != 0 )
        {
            FILEDosToUnixPathA( dirNameBuffer );
            statfsRetVal = statfs( dirNameBuffer, &fsInfoBuffer );
        }
        else
        {
            ASSERT( "Unable to convert the lpDirectoryName to multibyte.\n" );
            dwLastError = ERROR_INTERNAL_ERROR;
            goto exit;
        }
    }
    else
    {
        statfsRetVal = statfs( "/", &fsInfoBuffer );
    }

    if ( statfsRetVal == 0 )
    {
        *lpBytesPerSector = fsInfoBuffer.f_bsize;
        *lpSectorsPerCluster = 1;
        bRetVal = TRUE;
    }
    else
    {
        if ( errno == ENOTDIR || errno == ENOENT )
        {
            FILEGetProperNotFoundError( dirNameBuffer, &dwLastError );
            goto exit;
        }
        dwLastError = FILEGetLastErrorFromErrno();
        if ( ERROR_INTERNAL_ERROR == dwLastError )
        {
            ASSERT("statfs() not expected to fail with errno:%d (%s)\n", 
                   errno, strerror(errno));
        }
        else
        {
            TRACE("statfs() failed, errno:%d (%s)\n", errno, strerror(errno));
        }
    }

exit:
    if ( NO_ERROR != dwLastError )
    {
        SetLastError( dwLastError );
    }

    LOGEXIT( "GetDiskFreeSpace returning %s.\n", bRetVal == TRUE ? "TRUE" : "FALSE" );
    PERF_EXIT(GetDiskFreeSpaceW);
    return bRetVal;
}

