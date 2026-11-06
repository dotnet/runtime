// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    map.cpp

Abstract:

    Implementation of file mapping API.



--*/


#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/init.h"
#include "pal/map.hpp"
#include "pal/thread.hpp"
#include "pal/file.hpp"

#include <stddef.h>
#include <sys/stat.h>
#include <sys/types.h>
#include <sys/mman.h>
#include <unistd.h>
#include <fcntl.h>
#include <errno.h>
#include <minipal/ospagesize.h>

using namespace CorUnix;

SET_DEFAULT_DEBUG_CHANNEL(VIRTUAL);

#include "pal/utils.h"

//
// The mapping critical section guards access to the list
// of currently mapped views. If a thread needs to access
// both this critical section and the data for an object
// it must acquire the object data first. That is, a thread
// cannot acquire any other locks after taking hold of
// this critical section.
//

minipal_mutex mapping_critsec;
LIST_ENTRY MappedViewList;

#ifndef CORECLR
static PAL_ERROR MAPCreateTempFile(CPalThread *, PINT, PSZ);
#endif // !CORECLR
static PAL_ERROR MAPGrowLocalFile(INT, off_t);
static PMAPPED_VIEW_LIST MAPGetViewForAddress( LPCVOID );
static PAL_ERROR MAPDesiredAccessAllowed( DWORD, DWORD, DWORD );

static INT MAPProtectionToFileOpenFlags( DWORD );
static BOOL MAPIsRequestPermissible( DWORD, CFileProcessLocalData * );
static BOOL MAPContainsInvalidFlags( DWORD );
static DWORD MAPConvertProtectToAccess( DWORD );
static INT MAPFileMapToMmapFlags( DWORD );
#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
static NativeMapHolder * NewNativeMapHolder(CPalThread *pThread, LPVOID address, SIZE_T size,
                                     SIZE_T offset, long init_ref_count);
static LONG NativeMapHolderAddRef(NativeMapHolder * thisPMH);
static LONG NativeMapHolderRelease(CPalThread *pThread, NativeMapHolder * thisPMH);
static PMAPPED_VIEW_LIST FindSharedMappingReplacement(CPalThread *pThread, dev_t deviceNum, ino_t inodeNum,
                                                      SIZE_T size, SIZE_T offset);
#endif

/* We need MAP_ANON. However on some platforms like HP-UX, it is defined as MAP_ANONYMOUS */
#if !defined(MAP_ANON) && defined(MAP_ANONYMOUS)
#define MAP_ANON MAP_ANONYMOUS
#endif

void
FileMappingCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup,
    bool fShutdown
    );

void
CFileMappingImmutableDataCopyRoutine(
    void *pImmData,
    void *pImmDataTarget
    );

void
CFileMappingImmutableDataCleanupRoutine(
    void *pImmData
    );

CObjectType CorUnix::otFileMapping(
                otiFileMapping,
                FileMappingCleanupRoutine,
                sizeof(CFileMappingImmutableData),
                CFileMappingImmutableDataCopyRoutine,
                CFileMappingImmutableDataCleanupRoutine,
                sizeof(CFileMappingProcessLocalData),
                NULL    // No process local data cleanup routine
                );

CAllowedObjectTypes aotFileMapping(otiFileMapping);

void
CFileMappingImmutableDataCopyRoutine(
    void *pImmData,
    void *pImmDataTarget
    )
{
    CFileMappingImmutableData *pImmutableData = (CFileMappingImmutableData *) pImmData;
    CFileMappingImmutableData *pImmutableDataTarget = (CFileMappingImmutableData *) pImmDataTarget;

    if (NULL != pImmutableData->lpFileName)
    {
        pImmutableDataTarget->lpFileName = strdup(pImmutableData->lpFileName);
    }
}

void
CFileMappingImmutableDataCleanupRoutine(
    void *pImmData
    )
{
    CFileMappingImmutableData *pImmutableData = (CFileMappingImmutableData *) pImmData;

    free(pImmutableData->lpFileName);
}

void
FileMappingCleanupRoutine(
    CPalThread *pThread,
    IPalObject *pObjectToCleanup,
    bool fShutdown
    )
{
    PAL_ERROR palError = NO_ERROR;
    CFileMappingImmutableData *pImmutableData = NULL;
    CFileMappingProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;
    bool fDataChanged = FALSE;

    //
    // If we created a temporary file to back this mapping we need
    // to unlink it now
    //

    palError = pObjectToCleanup->GetImmutableData(
        reinterpret_cast<void**>(&pImmutableData)
        );

    if (NO_ERROR != palError)
    {
        ASSERT("Unable to obtain immutable data for object to be reclaimed");
        return;
    }

    if (pImmutableData->bPALCreatedTempFile)
    {
        unlink(pImmutableData->lpFileName);
    }

    if (FALSE == fShutdown)
    {
        //
        // We only need to close the object's descriptor if we're not
        // shutting down
        //

        palError = pObjectToCleanup->GetProcessLocalData(
            pThread,
            WriteLock,
            &pLocalDataLock,
            reinterpret_cast<void**>(&pLocalData)
            );

        if (NO_ERROR != palError)
        {
            ASSERT("Unable to obtain process local data for object to be reclaimed");
            return;
        }

        if (-1 != pLocalData->UnixFd)
        {
            close(pLocalData->UnixFd);
            pLocalData->UnixFd = -1;
            fDataChanged = TRUE;
        }

        pLocalDataLock->ReleaseLock(pThread, fDataChanged);
    }

    //
    // Why don't we need to deal with any views that may have been created
    // from this mapping? If the process is shutting down then there's nothing
    // that we need to take care of, as the OS will remove the underlying
    // mappings when the process goes away. If we're not shutting down then
    // there's no way for a view to exist against this mapping, since each
    // view holds a reference against the mapping object.
    //
}

/*++
Function:
  CreateFileMappingW

Note:
  File mapping are used to do inter-process communication.

See MSDN doc.
--*/
HANDLE
PALAPI
CreateFileMappingW(
               IN HANDLE hFile,
               IN LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
               IN DWORD flProtect,
               IN DWORD dwMaximumSizeHigh,
               IN DWORD dwMaximumSizeLow,
               IN LPCWSTR lpName)
{
    HANDLE hFileMapping = NULL;
    CPalThread *pThread = NULL;
    PAL_ERROR palError = NO_ERROR;

    PERF_ENTRY(CreateFileMappingW);
    ENTRY("CreateFileMappingW(hFile=%p, lpAttributes=%p, flProtect=%#x, "
          "dwMaxSizeH=%u, dwMaxSizeL=%u, lpName=%p (%S))\n",
          hFile, lpFileMappingAttributes, flProtect, dwMaximumSizeHigh,
          dwMaximumSizeLow, lpName?lpName:W16_NULLSTRING, lpName?lpName:W16_NULLSTRING);

    pThread = InternalGetCurrentThread();

    palError = InternalCreateFileMapping(
        pThread,
        hFile,
        lpFileMappingAttributes,
        flProtect,
        dwMaximumSizeHigh,
        dwMaximumSizeLow,
        lpName,
        &hFileMapping
        );

    //
    // We always need to set last error, even on success:
    // we need to protect ourselves from the situation
    // where last error is set to ERROR_ALREADY_EXISTS on
    // entry to the function
    //

    pThread->SetLastError(palError);

    LOGEXIT( "CreateFileMappingW returning %p .\n", hFileMapping );
    PERF_EXIT(CreateFileMappingW);
    return hFileMapping;
}

PAL_ERROR
CorUnix::InternalCreateFileMapping(
    CPalThread *pThread,
    HANDLE hFile,
    LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
    DWORD flProtect,
    DWORD dwMaximumSizeHigh,
    DWORD dwMaximumSizeLow,
    LPCWSTR lpName,
    HANDLE *phMapping
    )
{
    CObjectAttributes objectAttributes(lpName, lpFileMappingAttributes);
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pMapping = NULL;
    IPalObject *pRegisteredMapping = NULL;
    CFileMappingProcessLocalData *pLocalData = NULL;
    IDataLock *pLocalDataLock = NULL;
    CFileMappingImmutableData *pImmutableData = NULL;
    IPalObject *pFileObject = NULL;
    CFileProcessLocalData *pFileLocalData = NULL;
    IDataLock *pFileLocalDataLock = NULL;

    struct stat UnixFileInformation;
    INT UnixFd = -1;
    BOOL bPALCreatedTempFile = FALSE;
    UINT nFileSize = 0;
    off_t maximumSize;

    //
    // Validate parameters
    //

    if (lpName != nullptr)
    {
        ASSERT("lpName: Cross-process named objects are not supported in PAL");
        palError = ERROR_NOT_SUPPORTED;
        goto ExitInternalCreateFileMapping;
    }

    if (PAGE_READWRITE != flProtect
        && PAGE_READONLY != flProtect
        && PAGE_WRITECOPY != flProtect)
    {
        ASSERT( "invalid flProtect %#x, acceptable values are PAGE_READONLY "
                "(%#x), PAGE_READWRITE (%#x) and PAGE_WRITECOPY (%#x).\n",
                flProtect, PAGE_READONLY, PAGE_READWRITE, PAGE_WRITECOPY );
        palError = ERROR_INVALID_PARAMETER;
        goto ExitInternalCreateFileMapping;
    }

    if (hFile == INVALID_HANDLE_VALUE &&
        0 == dwMaximumSizeLow && 0 == dwMaximumSizeHigh)
    {
        ERROR( "If hFile is INVALID_HANDLE_VALUE, then you must specify a size.\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto ExitInternalCreateFileMapping;
    }

    if (hFile != INVALID_HANDLE_VALUE && NULL != lpName)
    {
        ASSERT( "If hFile is not -1, then lpName must be NULL.\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto ExitInternalCreateFileMapping;
    }

    maximumSize = ((off_t)dwMaximumSizeHigh << 32) | (off_t)dwMaximumSizeLow;

    palError = g_pObjectManager->AllocateObject(
        pThread,
        &otFileMapping,
        &objectAttributes,
        &pMapping
        );

    if (NO_ERROR != palError)
    {
        goto ExitInternalCreateFileMapping;
    }

    palError = pMapping->GetImmutableData(reinterpret_cast<void**>(&pImmutableData));
    if (NO_ERROR != palError)
    {
        goto ExitInternalCreateFileMapping;
    }

    if (hFile == INVALID_HANDLE_VALUE
#ifndef CORECLR
        && NULL == lpName
#endif // !CORECLR
        )
    {
        //
        // Note: this path is what prevents us supporting the
        // duplication of file mapping objects across processes, since
        // there is no backing file that the other process can open. We can
        // avoid this restriction by always using a temp backing file for
        // anonymous mappings.
        //

        /* Anonymous mapped files. */
        _ASSERTE(pImmutableData->lpFileName == NULL);
        pImmutableData->lpFileName = strdup("/dev/zero");
        if (pImmutableData->lpFileName == NULL)
        {
            ASSERT("Unable to copy string\n");
            palError = ERROR_INTERNAL_ERROR;
            goto ExitInternalCreateFileMapping;
        }

        UnixFd = -1;  /* will pass MAP_ANON to mmap() instead */
    }
    else
    {
        if ( hFile != INVALID_HANDLE_VALUE )
        {
            palError = g_pObjectManager->ReferenceObjectByHandle(
                pThread,
                hFile,
                &aotFile,
                &pFileObject
                );

            if (NO_ERROR != palError)
            {
                ERROR("Unable to obtain file data.\n");
                palError = ERROR_INVALID_PARAMETER;
                goto ExitInternalCreateFileMapping;
            }

            palError = pFileObject->GetProcessLocalData(
                pThread,
                ReadLock,
                &pFileLocalDataLock,
                reinterpret_cast<void**>(&pFileLocalData)
                );

            if (NO_ERROR != palError)
            {
                goto ExitInternalCreateFileMapping;
            }

            /* We need to check to ensure flProtect jives with
               the permission on the file handle */
            if (!MAPIsRequestPermissible(flProtect, pFileLocalData))
            {
                ERROR("File handle does not have the correct "
                      "permissions to create mapping\n" );
                palError = ERROR_ACCESS_DENIED;
                if (NULL != pFileLocalDataLock)
                {
                    pFileLocalDataLock->ReleaseLock(pThread, FALSE);
                }
                goto ExitInternalCreateFileMapping;
            }

            //
            // TODO: technically, the file mapping object should hold
            // a reference to the passed in file object. This implementation
            // only keeps the underlying native file structure (i.e., what
            // the duplicated descriptors point to) open. There may be a risk
            // here pertaining to the file lock information that the PAL must
            // maintain (e.g,. if the passed in handle is closed immediately
            // after the file mapping is opened then the lock information will
            // be released, since we're not doing anything to keep it alive
            // here).
            //
            // Having a direct reference to the underlying file object adds
            // some complication, especially in cross-process cases. We may
            // want to consider adding a reference to the PAL's file lock
            // information, though...
            //

#ifdef TARGET_WASI
            // WASI has no dup(); re-open by path with the same access mode
            // so writable mappings still work. O_CLOEXEC isn't meaningful
            // on WASI (no exec). Handles created by init_std_handle()
            // (STDIN/STDOUT/STDERR) have no backing path, so there is nothing
            // to re-open; fail gracefully instead of passing NULL to open().
            UnixFd = (pFileLocalData->unix_filename != NULL)
                ? open(pFileLocalData->unix_filename,
                       pFileLocalData->open_flags & O_ACCMODE)
                : -1;
#else
            UnixFd = fcntl(pFileLocalData->unix_fd, F_DUPFD_CLOEXEC, 0); // dup, but with CLOEXEC
#endif
            if (-1 == UnixFd)
            {
                ERROR( "Unable to duplicate the Unix file descriptor!\n" );
                palError = ERROR_INTERNAL_ERROR;
                if (NULL != pFileLocalDataLock)
                {
                    pFileLocalDataLock->ReleaseLock(pThread, FALSE);
                }
                goto ExitInternalCreateFileMapping;
            }

            _ASSERTE(pImmutableData->lpFileName == NULL);
            pImmutableData->lpFileName = strdup(pFileLocalData->unix_filename);
            if (pImmutableData->lpFileName == NULL)
            {
                ASSERT("Unable to copy string\n");
                palError = ERROR_INTERNAL_ERROR;
                if (NULL != pFileLocalDataLock)
                {
                    pFileLocalDataLock->ReleaseLock(pThread, FALSE);
                }
                goto ExitInternalCreateFileMapping;
            }

            if (NULL != pFileLocalDataLock)
            {
                pFileLocalDataLock->ReleaseLock(pThread, FALSE);
            }
        }
        else
        {
#ifndef CORECLR
            TRACE( "INVALID_HANDLE_VALUE was the hFile, time to try to create a "
                   "temporary file" );

            /* Create a temporary file on the filesystem in order to be
               shared across processes. */
            palError = MAPCreateTempFile(pThread, &UnixFd, pImmutableData->lpFileName);
            if (NO_ERROR != palError)
            {
                ERROR("Unable to create the temporary file.\n");
                goto ExitInternalCreateFileMapping;
            }
            bPALCreatedTempFile = TRUE;
#else // !CORECLR
            ASSERT("should not get here\n");
            palError = ERROR_INTERNAL_ERROR;
            goto ExitInternalCreateFileMapping;
#endif // !CORECLR
        }

        if (-1 == fstat(UnixFd, &UnixFileInformation))
        {
            ASSERT("fstat() failed for this reason %s.\n", strerror(errno));
            palError = ERROR_INTERNAL_ERROR;
            goto ExitInternalCreateFileMapping;
        }

        if ( 0 == UnixFileInformation.st_size && 0 == maximumSize )
        {
            ERROR( "The file cannot be a zero length file.\n" );
            palError = ERROR_FILE_INVALID;
            goto ExitInternalCreateFileMapping;
        }

        if ( INVALID_HANDLE_VALUE != hFile &&
             maximumSize > UnixFileInformation.st_size &&
             ( PAGE_READONLY == flProtect || PAGE_WRITECOPY == flProtect ) )
        {
            /* In this situation, Windows returns an error, because the
               permissions requested do not allow growing the file */
            ERROR( "The file cannot be grown do to the map's permissions.\n" );
            palError = ERROR_NOT_ENOUGH_MEMORY;
            goto ExitInternalCreateFileMapping;
        }

        if ( UnixFileInformation.st_size < maximumSize )
        {
            TRACE( "Growing the size of file on disk to match requested size.\n" );

            /* Need to grow the file on disk to match size. */
            palError = MAPGrowLocalFile(UnixFd, maximumSize);
            if (NO_ERROR != palError)
            {
                ERROR( "Unable to grow the file on disk.\n" );
                goto ExitInternalCreateFileMapping;
            }
        }
    }

    nFileSize = ( 0 == maximumSize ) ?
        UnixFileInformation.st_size : maximumSize;

    pImmutableData->MaxSize = nFileSize;
    pImmutableData->flProtect = flProtect;
    pImmutableData->bPALCreatedTempFile = bPALCreatedTempFile;
    pImmutableData->dwDesiredAccessWhenOpened = MAPConvertProtectToAccess(flProtect);


    //
    // The local data isn't grabbed / modified until here so that we don't
    // need to worry ourselves with locking issues with the passed in
    // file handle -- all operations concerning the file handle are completed
    // before we deal with the lock for the new object.
    //

    palError = pMapping->GetProcessLocalData(
        pThread,
        WriteLock,
        &pLocalDataLock,
        reinterpret_cast<void**>(&pLocalData)
        );

    if (NO_ERROR != palError)
    {
        goto ExitInternalCreateFileMapping;
    }

    pLocalData->UnixFd = UnixFd;

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
    if (-1 == UnixFd)
    {
        pLocalData->MappedFileDevNum = (dev_t)-1; /* there is no standard NO_DEV */
        pLocalData->MappedFileInodeNum = NO_INO;
    }
    else
    {
        struct stat st;

        if (0 == fstat(UnixFd, &st))
        {
            pLocalData->MappedFileDevNum = st.st_dev;
            pLocalData->MappedFileInodeNum = st.st_ino;
        }
        else
        {
            ERROR("Couldn't get inode info for fd=%d to be stored in mapping object\n", UnixFd);
            palError = ERROR_INTERNAL_ERROR;
            goto ExitInternalCreateFileMapping;
        }
    }
#endif

    pLocalDataLock->ReleaseLock(pThread, TRUE);
    pLocalDataLock = NULL;

    palError = g_pObjectManager->RegisterObject(
        pThread,
        pMapping,
        &aotFileMapping,
        phMapping,
        &pRegisteredMapping
        );

    //
    // pMapping is invalidated by the call to RegisterObject, so NULL it
    // out here to ensure that we don't try to release a reference on
    // it down the line. This also ensures that we won't attempt to release
    // any data associated with the mapping object here, as if any cleanup is
    // necessary due to a failure in RegisterObject (which includes another
    // object by the same name already existing) the cleanup will take place
    // when that routine releases the reference to pMapping.
    //

    pMapping = NULL;

ExitInternalCreateFileMapping:

    if (NULL != pLocalDataLock)
    {
        pLocalDataLock->ReleaseLock(
            pThread,
            TRUE
            );
    }

    if (NULL != pMapping)
    {
        pMapping->ReleaseReference(pThread);

        if (bPALCreatedTempFile)
        {
            unlink(pImmutableData->lpFileName);
        }

        if (-1 != UnixFd)
        {
            close(UnixFd);
        }
    }

    if (NULL != pRegisteredMapping)
    {
        pRegisteredMapping->ReleaseReference(pThread);
    }

    if (NULL != pFileObject)
    {
        pFileObject->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function:
  MapViewOfFile

  Limitations: 1) Currently file mappings are supported only at file
                  offset 0.
               2) Some platforms (specifically HP-UX) do not support
                  multiple simultaneous shared mapping of the same file
                  region in the same process. On these platforms, in case
                  we are asked for a new view completely contained in an
                  existing one, we return an address within the existing
                  mapping. In case the new requested view is overlapping
                  with the existing one, but not contained in it, the
                  mapping is impossible, and MapViewOfFile will fail.
                  Since currently the mappings are supported only at file
                  offset 0, MapViewOfFile will succeed if the new view
                  is equal or smaller of the existing one, and the address
                  returned will be the same address of the existing
                  mapping.
                  Since the underlying mapping is always the same, all
                  the shared views of the same file region will share the
                  same protection, i.e. they will have the largest
                  protection requested. If any mapping asked for a
                  read-write access, all the read-only mappings of the
                  same region will silently get a read-write access to
                  it.

See MSDN doc.
--*/
LPVOID
PALAPI
MapViewOfFile(
          IN HANDLE hFileMappingObject,
          IN DWORD dwDesiredAccess,
          IN DWORD dwFileOffsetHigh,
          IN DWORD dwFileOffsetLow,
          IN SIZE_T dwNumberOfBytesToMap)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread = NULL;
    LPVOID pvMappedBaseAddress = NULL;

    PERF_ENTRY(MapViewOfFile);
    ENTRY("MapViewOfFile(hFileMapping=%p, dwDesiredAccess=%u, "
          "dwFileOffsetH=%u, dwFileOffsetL=%u, dwNumberOfBytes=%u)\n",
          hFileMappingObject, dwDesiredAccess, dwFileOffsetHigh,
          dwFileOffsetLow, dwNumberOfBytesToMap);

    pThread = InternalGetCurrentThread();

    palError = InternalMapViewOfFile(
        pThread,
        hFileMappingObject,
        dwDesiredAccess,
        dwFileOffsetHigh,
        dwFileOffsetLow,
        dwNumberOfBytesToMap,
        &pvMappedBaseAddress
        );

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT( "MapViewOfFile returning %p.\n", pvMappedBaseAddress );
    PERF_EXIT(MapViewOfFile);
    return pvMappedBaseAddress;
}

LPVOID
PALAPI
MapViewOfFileEx(
          IN HANDLE hFileMappingObject,
          IN DWORD dwDesiredAccess,
          IN DWORD dwFileOffsetHigh,
          IN DWORD dwFileOffsetLow,
          IN SIZE_T dwNumberOfBytesToMap,
          IN LPVOID lpBaseAddress)
{
    PAL_ERROR palError = NO_ERROR;
    CPalThread *pThread = NULL;
    LPVOID pvMappedBaseAddress = NULL;

    PERF_ENTRY(MapViewOfFileEx);
    ENTRY("MapViewOfFileEx(hFileMapping=%p, dwDesiredAccess=%u, "
          "dwFileOffsetH=%u, dwFileOffsetL=%u, dwNumberOfBytes=%u, lpBaseAddress=%p)\n",
          hFileMappingObject, dwDesiredAccess, dwFileOffsetHigh,
          dwFileOffsetLow, dwNumberOfBytesToMap, lpBaseAddress);

    pThread = InternalGetCurrentThread();

    if (lpBaseAddress == NULL)
    {
        palError = InternalMapViewOfFile(
            pThread,
            hFileMappingObject,
            dwDesiredAccess,
            dwFileOffsetHigh,
            dwFileOffsetLow,
            dwNumberOfBytesToMap,
            &pvMappedBaseAddress
            );

        if (NO_ERROR != palError)
        {
            pThread->SetLastError(palError);
        }
    }
    else
    {
        // TODO: Figure out if we can support mapping at a specific address on Linux.
        pThread->SetLastError(ERROR_INVALID_PARAMETER);
    }

    LOGEXIT( "MapViewOfFileEx returning %p.\n", pvMappedBaseAddress );
    PERF_EXIT(MapViewOfFileEx);
    return pvMappedBaseAddress;
}


/*++
Function:
  UnmapViewOfFile

See MSDN doc.
--*/
BOOL
PALAPI
UnmapViewOfFile(
        IN LPCVOID lpBaseAddress) noexcept
{
    PAL_ERROR palError;
    CPalThread *pThread;

    PERF_ENTRY(UnmapViewOfFile);
    ENTRY("UnmapViewOfFile(lpBaseAddress=%p)\n", lpBaseAddress);

    pThread = InternalGetCurrentThread();

    palError = InternalUnmapViewOfFile(pThread, lpBaseAddress);

    if (NO_ERROR != palError)
    {
        pThread->SetLastError(palError);
    }

    LOGEXIT( "UnmapViewOfFile returning %s.\n", (NO_ERROR == palError) ? "TRUE" : "FALSE" );
    PERF_EXIT(UnmapViewOfFile);
    return (NO_ERROR == palError);
}

PAL_ERROR
CorUnix::InternalMapViewOfFile(
    CPalThread *pThread,
    HANDLE hFileMappingObject,
    DWORD dwDesiredAccess,
    DWORD dwFileOffsetHigh,
    DWORD dwFileOffsetLow,
    SIZE_T dwNumberOfBytesToMap,
    LPVOID *ppvBaseAddress
    )
{
    PAL_ERROR palError = NO_ERROR;
    IPalObject *pMappingObject = NULL;
    CFileMappingImmutableData *pImmutableData = NULL;
    CFileMappingProcessLocalData *pProcessLocalData = NULL;
    IDataLock *pProcessLocalDataLock = NULL;
    INT64 offset = ((INT64)dwFileOffsetHigh << 32) | (INT64)dwFileOffsetLow;

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
    PMAPPED_VIEW_LIST pReusedMapping = NULL;
#endif
    LPVOID pvBaseAddress = NULL;

    /* Sanity checks */
    if ( MAPContainsInvalidFlags( dwDesiredAccess ) )
    {
        ASSERT( "dwDesiredAccess can be one of FILE_MAP_WRITE, FILE_MAP_READ,"
               " FILE_MAP_COPY or FILE_MAP_ALL_ACCESS.\n" );
        palError = ERROR_INVALID_PARAMETER;
        goto InternalMapViewOfFileExit;
    }

    if (offset < 0)
    {
        ASSERT("dwFileOffsetHigh | dwFileOffsetLow should be non-negative.\n");
        palError = ERROR_INVALID_PARAMETER;
        goto InternalMapViewOfFileExit;
    }

    palError = g_pObjectManager->ReferenceObjectByHandle(
        pThread,
        hFileMappingObject,
        &aotFileMapping,
        &pMappingObject
        );

    if (NO_ERROR != palError)
    {
        ERROR( "Unable to reference handle %p.\n",hFileMappingObject );
        goto InternalMapViewOfFileExit;
    }

    palError = pMappingObject->GetImmutableData(
        reinterpret_cast<void**>(&pImmutableData)
        );

    if (NO_ERROR != palError)
    {
        ERROR( "Unable to obtain object immutable data");
        goto InternalMapViewOfFileExit;
    }

    palError = pMappingObject->GetProcessLocalData(
        pThread,
        ReadLock,
        &pProcessLocalDataLock,
        reinterpret_cast<void**>(&pProcessLocalData)
        );

    if (NO_ERROR != palError)
    {
        ERROR( "Unable to obtain object process local data");
        goto InternalMapViewOfFileExit;
    }

    /* If dwNumberOfBytesToMap is 0, we need to map the entire file.
     * mmap doesn't do the same thing as Windows in that case, though,
     * so we use the file size instead. */
    if (0 == dwNumberOfBytesToMap)
    {
        dwNumberOfBytesToMap = pImmutableData->MaxSize;
    }

    palError = MAPDesiredAccessAllowed(
        pImmutableData->flProtect,
        dwDesiredAccess,
        pImmutableData->dwDesiredAccessWhenOpened
        );

    if (NO_ERROR != palError)
    {
        goto InternalMapViewOfFileExit;
    }

    minipal_mutex_enter(&mapping_critsec);

    if (FILE_MAP_COPY == dwDesiredAccess)
    {
        int flags = MAP_PRIVATE;
        if (pProcessLocalData->UnixFd == -1)
        {
            flags |= MAP_ANON;
        }

        pvBaseAddress = mmap(
            NULL,
            dwNumberOfBytesToMap,
            PROT_READ|PROT_WRITE,
            flags,
            pProcessLocalData->UnixFd,
            offset
            );
    }
    else
    {
        INT prot = MAPFileMapToMmapFlags(dwDesiredAccess);
        if (prot != -1)
        {
            int flags = MAP_SHARED;
            if (pProcessLocalData->UnixFd == -1)
            {
                flags |= MAP_ANON;
            }

            pvBaseAddress = mmap(
                NULL,
                dwNumberOfBytesToMap,
                prot,
                flags,
                pProcessLocalData->UnixFd,
                offset
                );

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
            if ((MAP_FAILED == pvBaseAddress) && (ENOMEM == errno))
            {
                /* Search in list of MAPPED_MEMORY_INFO for a shared mapping
                   with the same inode number
                */
                TRACE("Mmap() failed with errno=ENOMEM probably for multiple mapping "
                      "limitation. Searching for a replacement among existing mappings\n");

                pReusedMapping = FindSharedMappingReplacement(
                    pThread,
                    pProcessLocalData->MappedFileDevNum,
                    pProcessLocalData->MappedFileInodeNum,
                    dwNumberOfBytesToMap,
                    0
                    );

                if (pReusedMapping)
                {
                    int ret;

                    TRACE("Mapping @ %p {sz=%d offs=%d} fully "
                          "contains the requested one {sz=%d offs=%d}: reusing it\n",
                          pReusedMapping->pNMHolder->address,
                          (int)pReusedMapping->pNMHolder->size,
                          (int)pReusedMapping->pNMHolder->offset,
                          dwNumberOfBytesToMap, 0);

                    /* Let's check the mapping's current protection */
                    ret = mprotect(pReusedMapping->pNMHolder->address,
                                   pReusedMapping->pNMHolder->size,
                                   prot | PROT_CHECK);
                    if (0 != ret)
                    {
                        /* We need to raise the protection to the desired
                           one. That will give write access to any read-only
                           mapping sharing this native mapping, but there is
                           no way around this problem on systems that do not
                           allow more than one mapping per file region, per
                           process */
                        TRACE("Raising protections on mapping @ %p to 0x%x\n",
                              pReusedMapping->pNMHolder->address, prot);
                        ret = mprotect(pReusedMapping->pNMHolder->address,
                                   pReusedMapping->pNMHolder->size,
                                   prot);
                    }

                    if (ret != 0)
                    {
                        ERROR( "Failed setting protections on reused mapping\n");

                        NativeMapHolderRelease(pThread, pReusedMapping->pNMHolder);
                        free(pReusedMapping);
                        pReusedMapping = NULL;
                    }
                }
            }
#endif // ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
        }
        else
        {
            ASSERT( "MapFileMapToMmapFlags failed!\n" );
            palError = ERROR_INTERNAL_ERROR;
            goto InternalMapViewOfFileLeaveCriticalSection;
        }
    }

    if (MAP_FAILED == pvBaseAddress
#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
         &&  (pReusedMapping == NULL)
#endif // ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
        )
    {
        ERROR( "mmap failed with code %s.\n", strerror( errno ) );
        palError = ERROR_NOT_ENOUGH_MEMORY;
        goto InternalMapViewOfFileLeaveCriticalSection;

    }

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
    if (pReusedMapping != NULL)
    {
        //
        // Add a reference to the file mapping object the reused mapping
        // points to (note that it may be different than the object this
        // call was actually made against) and add the view to the global
        // list. All other initialization took place in
        // FindSharedMappingReplacement
        //

        pvBaseAddress = pReusedMapping->lpAddress;
        pReusedMapping->pFileMapping->AddReference();
        InsertTailList(&MappedViewList, &pReusedMapping->Link);
    }
    else
#endif // ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
    {
        //
        // Allocate and fill out a new view structure, and add it to
        // the global list.
        //

        PMAPPED_VIEW_LIST pNewView = (PMAPPED_VIEW_LIST)malloc(sizeof(*pNewView));
        if (NULL != pNewView)
        {
            pNewView->lpAddress = pvBaseAddress;
            pNewView->NumberOfBytesToMap = dwNumberOfBytesToMap;
            pNewView->dwDesiredAccess = dwDesiredAccess;
            pNewView->pFileMapping = pMappingObject;
            pNewView->pFileMapping->AddReference();
            InsertTailList(&MappedViewList, &pNewView->Link);

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
            pNewView->MappedFileDevNum = pProcessLocalData->MappedFileDevNum;
            pNewView->MappedFileInodeNum = pProcessLocalData->MappedFileInodeNum;

            pNewView->pNMHolder = NewNativeMapHolder(
                pThread,
                pvBaseAddress,
                dwNumberOfBytesToMap,
                0,
                1
                );

            if (NULL == pNewView->pNMHolder)
            {
                pNewView->pFileMapping->ReleaseReference(pThread);
                RemoveEntryList(&pNewView->Link);
                free(pNewView);
                palError = ERROR_INTERNAL_ERROR;
            }
#endif // ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
        }
        else
        {
            palError = ERROR_INTERNAL_ERROR;
        }

        if (NO_ERROR != palError)
        {
            if (-1 == munmap(pvBaseAddress, dwNumberOfBytesToMap))
            {
                ERROR("Unable to unmap the file. Expect trouble.\n");
                goto InternalMapViewOfFileLeaveCriticalSection;
            }
        }
    }

    if (NO_ERROR == palError)
    {
        TRACE( "Added %p to the list.\n", pvBaseAddress );
        *ppvBaseAddress = pvBaseAddress;
    }

InternalMapViewOfFileLeaveCriticalSection:

    minipal_mutex_leave(&mapping_critsec);

InternalMapViewOfFileExit:

    if (NULL != pProcessLocalDataLock)
    {
        pProcessLocalDataLock->ReleaseLock(pThread, FALSE);
    }

    if (NULL != pMappingObject)
    {
        pMappingObject->ReleaseReference(pThread);
    }

    return palError;
}


PAL_ERROR
CorUnix::InternalUnmapViewOfFile(
    CPalThread *pThread,
    LPCVOID lpBaseAddress
    )
{
    PAL_ERROR palError = NO_ERROR;
    PMAPPED_VIEW_LIST pView = NULL;
    IPalObject *pMappingObject = NULL;

    minipal_mutex_enter(&mapping_critsec);

    pView = MAPGetViewForAddress(lpBaseAddress);
    if (NULL == pView)
    {
        ERROR("lpBaseAddress has to be the address returned by MapViewOfFile[Ex]");
        palError = ERROR_INVALID_HANDLE;
        goto InternalUnmapViewOfFileExit;
    }

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
    NativeMapHolderRelease(pThread, pView->pNMHolder);
    pView->pNMHolder = NULL;
#else
    if (-1 == munmap((LPVOID)lpBaseAddress, pView->NumberOfBytesToMap))
    {
        ASSERT( "Unable to unmap the memory. Error=%s.\n",
                strerror( errno ) );
        palError = ERROR_INTERNAL_ERROR;

        //
        // Even if the unmap fails we want to continue removing the
        // info for this view
        //
    }
#endif

    RemoveEntryList(&pView->Link);
    pMappingObject = pView->pFileMapping;
    free(pView);

InternalUnmapViewOfFileExit:

    minipal_mutex_leave(&mapping_critsec);

    //
    // We can't dereference the file mapping object until after
    // we've released the mapping critical section, since it may
    // start going down its cleanup path and we don't want to make
    // any assumptions as to what locks that might grab...
    //

    if (NULL != pMappingObject)
    {
        pMappingObject->ReleaseReference(pThread);
    }

    return palError;
}

/*++
Function :
    MAPInitialize

    Initialize the critical sections.

Return value:
    TRUE if initialization succeeded
    FALSE otherwise
--*/
BOOL
MAPInitialize( void )
{
    TRACE( "Initialising the critical section.\n" );

    minipal_mutex_init(&mapping_critsec);

    InitializeListHead(&MappedViewList);

    return TRUE;
}

/*++
Function :
    MAPCleanup

    Deletes the critical sections. And all other necessary cleanup.

Note:
    This function is called after the handle manager is stopped. So
    there shouldn't be any call that will cause an access to the handle
    manager.

--*/
void MAPCleanup( void )
{
    TRACE( "Deleting the critical section.\n" );
    minipal_mutex_destroy(&mapping_critsec);
}

/*++
Function :
    MAPGetViewForAddress

    Returns the mapped view (if any) that is based at the passed in address.

    Callers to this function must hold mapping_critsec
--*/
static PMAPPED_VIEW_LIST MAPGetViewForAddress( LPCVOID lpAddress )
{
    if ( NULL == lpAddress )
    {
        ERROR( "lpAddress cannot be NULL\n" );
        return NULL;
    }

    for(LIST_ENTRY *pLink = MappedViewList.Flink;
        pLink != &MappedViewList;
        pLink = pLink->Flink)
    {
        PMAPPED_VIEW_LIST pView = CONTAINING_RECORD(pLink, MAPPED_VIEW_LIST, Link);

        if (pView->lpAddress == lpAddress)
        {
            return pView;
        }
    }

    WARN( "No match found.\n" );

    return NULL;
}

/*++
Function :

    MAPDesiredAccessAllowed

    Determines if desired access is allowed based on the protection state.

    if dwDesiredAccess conflicts with flProtect then the error is
    ERROR_INVALID_PARAMETER, if the dwDesiredAccess conflicts with
    dwDesiredAccessWhenOpened, then the error code is ERROR_ACCESS_DENIED
--*/
static PAL_ERROR MAPDesiredAccessAllowed( DWORD flProtect,
                                     DWORD dwUserDesiredAccess,
                                     DWORD dwDesiredAccessWhenOpened )
{
    TRACE( "flProtect=%d, dwUserDesiredAccess=%d, dwDesiredAccessWhenOpened=%d\n",
           flProtect, dwUserDesiredAccess, dwDesiredAccessWhenOpened );

    /* check flProtect parameters*/
    if ( FILE_MAP_READ!= dwUserDesiredAccess && PAGE_READONLY == flProtect )
    {
        ERROR( "map object is read-only, can't map a view with write access\n");
        return ERROR_INVALID_PARAMETER;
    }

    if ( FILE_MAP_WRITE == dwUserDesiredAccess && PAGE_READWRITE != flProtect )
    {
        ERROR( "map object not open read-write, can't map a view with write "
               "access.\n" );
        return ERROR_INVALID_PARAMETER;
    }

    if ( FILE_MAP_COPY == dwUserDesiredAccess  && PAGE_WRITECOPY != flProtect )
    {
        ERROR( "map object not open for copy-on-write, can't map copy-on-write "
               "view.\n" );
        return ERROR_INVALID_PARAMETER;
    }

    /* Check to see we don't confict with the desired access we
    opened the mapping object with. */
    if ( ( dwUserDesiredAccess == FILE_MAP_READ ) &&
        !( ( dwDesiredAccessWhenOpened == FILE_MAP_READ ) ||
           ( dwDesiredAccessWhenOpened == FILE_MAP_ALL_ACCESS ) ) )
    {
        ERROR( "dwDesiredAccess conflict : read access requested, object not "
               "opened with read access.\n" );
        return ERROR_ACCESS_DENIED;
    }
    if ( ( dwUserDesiredAccess & FILE_MAP_WRITE ) &&
        !( ( dwDesiredAccessWhenOpened == FILE_MAP_WRITE ) ||
           ( dwDesiredAccessWhenOpened == FILE_MAP_ALL_ACCESS ) ) )
    {
        ERROR( "dwDesiredAccess conflict : write access requested, object not "
               "opened with write access.\n" );
        return ERROR_ACCESS_DENIED;
    }
    if ( ( dwUserDesiredAccess == FILE_MAP_COPY ) &&
        !( dwDesiredAccessWhenOpened == FILE_MAP_COPY ) )
    {
        ERROR( "dwDesiredAccess conflict : copy-on-write access requested, "
               "object not opened with copy-on-write access.\n" );
        return ERROR_ACCESS_DENIED;
    }

    return NO_ERROR;
}

/*++
Function :
    MAPConvertProtectToAccess

    Converts the PAGE_READONLY type flags to FILE_MAP_READ flags.

--*/
static DWORD MAPConvertProtectToAccess( DWORD flProtect )
{
    if ( PAGE_READONLY == flProtect )
    {
        return FILE_MAP_READ;
    }
    if ( PAGE_READWRITE == flProtect )
    {
        return FILE_MAP_ALL_ACCESS;
    }
    if ( PAGE_WRITECOPY == flProtect )
    {
        return FILE_MAP_COPY;
    }

    ASSERT( "Unknown flag for flProtect. This line "
            "should not have been executed.\n " );
    return (DWORD) -1;
}

/*++
Function :
    MAPFileMapToMmapFlags

    Converts the mapping flags to unix protection flags.
--*/
static INT MAPFileMapToMmapFlags( DWORD flags )
{
    if ( FILE_MAP_READ == flags )
    {
        TRACE( "FILE_MAP_READ\n" );
        return PROT_READ;
    }
    else if ( FILE_MAP_WRITE == flags )
    {
        TRACE( "FILE_MAP_WRITE\n" );
        /* The limitation of x86 architecture
        means you cant have writable but not readable
        page. In Windows maps of FILE_MAP_WRITE can still be
        read from. */
        return PROT_WRITE | PROT_READ;
    }
    else if ( (FILE_MAP_READ|FILE_MAP_WRITE) == flags )
    {
        TRACE( "FILE_MAP_READ|FILE_MAP_WRITE\n" );
        return PROT_READ | PROT_WRITE;
    }
    else if( FILE_MAP_COPY == flags)
    {
        TRACE( "FILE_MAP_COPY\n");
        return PROT_READ | PROT_WRITE;
    }

    ASSERT( "Unknown flag. This line should not have been executed.\n" );
    return -1;
}

/*++
Function :

    MAPGrowLocalFile

    Grows the file on disk to match the specified size.

--*/
static PAL_ERROR MAPGrowLocalFile( INT UnixFD, off_t NewSize )
{
    PAL_ERROR palError = NO_ERROR;
    INT  TruncateRetVal = -1;
    struct stat FileInfo;
    TRACE( "Entered MapGrowLocalFile (UnixFD=%d,NewSize%d)\n", UnixFD, NewSize );

    //
    // TODO: can we add configure flags to model the behavior of ftruncate
    // among our various target platforms? How much would that actually gain
    // us?
    //

    /* ftruncate is a standard function, but the behavior of enlarging files is
    non-standard.  So I will try to enlarge a file, and if that fails try the
    less efficient way.*/
    TruncateRetVal = ftruncate( UnixFD, NewSize );
    fstat( UnixFD, &FileInfo );

    if ( TruncateRetVal != 0 || FileInfo.st_size != NewSize )
    {
        INT OrigSize;
        CONST UINT  BUFFER_SIZE = 128;
        BYTE buf[BUFFER_SIZE];
        UINT x = 0;
        UINT CurrentPosition = 0;

        TRACE( "Trying the less efficient way.\n" );

        CurrentPosition = lseek( UnixFD, 0, SEEK_CUR );
        OrigSize = lseek( UnixFD, 0, SEEK_END );
        if ( OrigSize == -1 )
        {
            ERROR( "Unable to locate the EOF marker. Reason=%s\n",
                   strerror( errno ) );
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }

        if (NewSize <= (UINT) OrigSize)
        {
            return TRUE;
        }

        memset( buf, 0, BUFFER_SIZE );
        if (NewSize - OrigSize - BUFFER_SIZE >= 0 && BUFFER_SIZE > 0)
        {
            for ( x = 0; x < NewSize - OrigSize - BUFFER_SIZE; x += BUFFER_SIZE )
            {
                if ( write( UnixFD, (LPVOID)buf, BUFFER_SIZE ) == -1 )
                {
                    ERROR( "Unable to grow the file. Reason=%s\n", strerror( errno ) );
                    if((errno == ENOSPC) || (errno == EDQUOT))
                    {
                        palError = ERROR_DISK_FULL;
                    }
                    else
                    {
                        palError = ERROR_INTERNAL_ERROR;
                    }
                    goto done;
                }
            }
        }
        else
        {
            //This will be an infinite loop because it did not pass the check.
            palError = ERROR_INTERNAL_ERROR;
            goto done;
        }
        /* Catch any left overs. */
        if ( x != NewSize )
        {
            if ( write( UnixFD, (LPVOID)buf, NewSize - OrigSize - x) == -1 )
            {
                ERROR( "Unable to grow the file. Reason=%s\n", strerror( errno ) );
                if((errno == ENOSPC) || (errno == EDQUOT))
                {
                    palError = ERROR_DISK_FULL;
                }
                else
                {
                    palError = ERROR_INTERNAL_ERROR;
                }
                goto done;
            }
        }

        /* restore the file pointer position */
        lseek( UnixFD, CurrentPosition, SEEK_SET );
    }

done:
    return palError;
}

/*++
Function :
    MAPContainsInvalidFlags

    Checks that only valid flags are in the parameter.

--*/
static BOOL MAPContainsInvalidFlags( DWORD flags )
{

    if ( (flags == FILE_MAP_READ) ||
         (flags == FILE_MAP_WRITE) ||
         (flags == FILE_MAP_ALL_ACCESS) ||
         (flags == FILE_MAP_COPY) )
    {
        return FALSE;
    }
    else
    {
        return TRUE;
    }
}

/*++
Function :
    MAPProtectionToFileOpenFlags

    Converts the PAGE_* flags to the O_* flags.

    Returns the file open flags.
--*/
static INT MAPProtectionToFileOpenFlags( DWORD flProtect )
{
    INT retVal = 0;
    switch(flProtect)
    {
    case PAGE_READONLY:
        retVal = O_RDONLY;
        break;
    case PAGE_READWRITE:
        retVal = O_RDWR;
        break;
    case PAGE_WRITECOPY:
        retVal = O_RDONLY;
        break;
    default:
        ASSERT("unexpected flProtect value %#x\n", flProtect);
        retVal = 0;
        break;
    }
    return retVal;
}

/*++
Function :

    MAPIsRequestPermissible

        DWORD flProtect     - The requested file mapping protection .
        file * pFileStruct  - The file structure containing all the information.

--*/
static BOOL MAPIsRequestPermissible( DWORD flProtect, CFileProcessLocalData * pFileLocalData )
{
    if ( ( (flProtect == PAGE_READONLY || flProtect == PAGE_WRITECOPY) &&
           (pFileLocalData->open_flags_deviceaccessonly == TRUE ||
            pFileLocalData->open_flags & O_WRONLY) )
       )
    {
        /*
         * PAGE_READONLY or PAGE_WRITECOPY access to a file must at least be
         * readable. Contrary to what MSDN says, PAGE_WRITECOPY
         * only needs to be readable.
         */
        return FALSE;
    }
    else if ( flProtect == PAGE_READWRITE && !(pFileLocalData->open_flags & O_RDWR) )
    {
        /*
         * PAGE_READWRITE access to a file needs to be readable and writable
         */
        return FALSE;
    }
    else
    {
        /* Action is permissible */
        return TRUE;
    }
}

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS

//
// Callers of FindSharedMappingReplacement must hold mapping_critsec
//

static PMAPPED_VIEW_LIST FindSharedMappingReplacement(
    CPalThread *pThread,
    dev_t deviceNum,
    ino_t inodeNum,
    SIZE_T size,
    SIZE_T offset)
{
    PMAPPED_VIEW_LIST pNewView = NULL;

    if (size == 0)
    {
        ERROR("Mapping size cannot be NULL\n");
        return NULL;
    }

    for (LIST_ENTRY *pLink = MappedViewList.Flink;
         pLink != &MappedViewList;
         pLink = pLink->Flink)
    {
        PMAPPED_VIEW_LIST pView = CONTAINING_RECORD(pLink, MAPPED_VIEW_LIST, Link);

        if (pView->MappedFileDevNum != deviceNum
            || pView->MappedFileInodeNum != inodeNum
            || pView->dwDesiredAccess == FILE_MAP_COPY)
        {
            continue;
        }

        //
        // This is a shared mapping for the same indoe / device. Now, check
        // to see if it overlaps with the range for the new view
        //

        SIZE_T real_map_offs = pView->pNMHolder->offset;
        SIZE_T real_map_sz = pView->pNMHolder->size;

        if (real_map_offs <= offset
            && real_map_offs+real_map_sz >= offset)
        {
            //
            // The views overlap. Even if this view is not reusable for the
            // new once the search is over, as on
            // ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS systems there
            // cannot be shared mappings of two overlapping regions of the
            // same file, in the same process. Therefore, whether this view
            // is reusable or not we cannot mmap the requested region of
            // the specified file.
            //

            if (real_map_offs+real_map_sz >= offset+size)
            {
                /* The new desired mapping is fully contained in the
                   one just found: we can reuse this one */

                pNewView = (PMAPPED_VIEW_LIST)malloc(sizeof(MAPPED_VIEW_LIST));
                if (pNewView)
                {
                    memcpy(pNewView, pView, sizeof(*pNewView));
                    NativeMapHolderAddRef(pNewView->pNMHolder);
                    pNewView->lpAddress = (VOID*)((CHAR*)pNewView->pNMHolder->address +
                        offset - pNewView->pNMHolder->offset);
                    pNewView->NumberOfBytesToMap = size;
                }
                else
                {
                    ERROR("No memory for new MAPPED_VIEW_LIST node\n");
                }
            }

            break;
        }
    }

    TRACE ("FindSharedMappingReplacement returning %p\n", pNewView);
    return pNewView;
}

static NativeMapHolder * NewNativeMapHolder(CPalThread *pThread, LPVOID address, SIZE_T size,
                                     SIZE_T offset, long init_ref_count)
{
    NativeMapHolder * pThisMapHolder;

    if (init_ref_count < 0)
    {
        ASSERT("Negative initial reference count for new map holder\n");
        return NULL;
    }

    pThisMapHolder =
        (NativeMapHolder *)malloc(sizeof(NativeMapHolder));

    if (pThisMapHolder)
    {
        pThisMapHolder->ref_count = init_ref_count;
        pThisMapHolder->address = address;
        pThisMapHolder->size = size;
        pThisMapHolder->offset = offset;
    }

    return pThisMapHolder;
}

static LONG NativeMapHolderAddRef(NativeMapHolder * thisNMH)
{
    LONG ret = InterlockedIncrement(&thisNMH->ref_count);
    return ret;
}

static LONG NativeMapHolderRelease(CPalThread *pThread, NativeMapHolder * thisNMH)
{
    LONG ret = InterlockedDecrement(&thisNMH->ref_count);
    if (ret == 0)
    {
        if (-1 == munmap(thisNMH->address, thisNMH->size))
        {
            ASSERT( "Unable to unmap memory. Error=%s.\n",
                    strerror( errno ) );
        }
        else
        {
            TRACE( "Successfully unmapped %p (size=%lu)\n",
                   thisNMH->address, (unsigned long)thisNMH->size);
        }
        free (thisNMH);
    }
    else if (ret < 0)
    {
        ASSERT( "Negative reference count for map holder %p"
                " {address=%p, size=%lu}\n", thisNMH->address,
                (unsigned long)thisNMH->size);
    }

    return ret;
}

#endif // ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
