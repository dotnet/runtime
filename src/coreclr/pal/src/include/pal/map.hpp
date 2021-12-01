// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/map.hpp

Abstract:

    Header file for file mapping functions.



--*/

#ifndef _PAL_MAP_H_
#define _PAL_MAP_H_

#include "corunix.hpp"
#include <sys/param.h>

extern "C"
{
#include "list.h"

#ifndef NO_INO
#define NO_INO ((ino_t)-1)
#endif

    /*++
    Function :
        MapInitialize

        Initialize the critical sections.

    Return value:
        TRUE  if initialization succeeded
        FALSE otherwise
    --*/
    BOOL MAPInitialize( void );

    /*++
    Function :
        MapCleanup

        Deletes the critical sections.

    --*/
    void MAPCleanup( void );

    /*++
    Function :
        MAPGetRegionInfo

        Parameters:
        lpAddress: pointer to the starting memory location, not necessary
                   to be rounded to the page location

        lpBuffer: if this function finds information about the specified address,
                  the information is stored in this struct

        Note: This function is to be used in virtual.c

        Returns TRUE if this function finds information about the specified address
    --*/

    BOOL MAPGetRegionInfo(LPVOID lpAddress, PMEMORY_BASIC_INFORMATION lpBuffer);

    /*++
        MAPMapPEFile -

        Map a PE format file into memory like Windows LoadLibrary() would do.
        Doesn't apply base relocations if the function is relocated.

    Parameters:
        IN hFile - file to map
        IN offset - offset within hFile where the PE "file" is located

    Return value:
        non-NULL - the base address of the mapped image
        NULL - error, with last error set.
    --*/

    void* MAPMapPEFile(HANDLE hFile, off_t offset);

    /*++
    Function :
        MAPUnmapPEFile - unmap a PE file, and remove it from the recorded list of PE files mapped

        returns TRUE if successful, FALSE otherwise
    --*/
    BOOL MAPUnmapPEFile(LPCVOID lpAddress);

    /*++
    Function :
        MAPMarkSectionAsNotNeeded - mark a section as NotNeeded
        returns TRUE if successful, FALSE otherwise
    --*/
    BOOL MAPMarkSectionAsNotNeeded(LPCVOID lpAddress);
}

namespace CorUnix
{
    extern CObjectType otFileMapping;

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
    typedef struct _NativeMapHolder
    {
        Volatile<LONG> ref_count;
        LPVOID address;
        SIZE_T size;
        SIZE_T offset; /* for future use */
    } NativeMapHolder;
#endif

    /* Process specific information. This
    structure is not stored in shared memory.*/
    typedef struct _MVL
    {
        LIST_ENTRY Link;

        //
        // Each MVL entry holds a reference to its parent file
        // mapping object.
        //

        IPalObject *pFileMapping;

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
        NativeMapHolder * pNMHolder; /* Ref-counted holder for memory mapping */
        dev_t   MappedFileDevNum;           /* ID of device containing the file to be mapped */
        ino_t   MappedFileInodeNum;         /* Inode number of file to be mapped.
                                               These two fields are used used to uniquely
                                               identify files on systems that do not allow
                                               more than one shared mmapping per region of
                                               physical file, per process */
#endif
        LPVOID lpAddress;           /* The pointer to the mapped memory. */
        SIZE_T NumberOfBytesToMap;  /* Number of bytes to map. */
        DWORD dwDesiredAccess;      /* Desired access. */
        LPVOID lpPEBaseAddress;     /* If this mapping is part of a PE file mapping, this is the
                                       base address pointer of the PE file (used to find all
                                       parts of the PE file mapping to allow PE file unload).
                                       Otherwise, it is NULL. */
    } MAPPED_VIEW_LIST, * PMAPPED_VIEW_LIST;

    class CFileMappingImmutableData
    {
    public:
        CHAR *lpFileName;
        UINT MaxSize;               // The max size of the file mapping object
        DWORD flProtect;            // Protection desired for the file view
        BOOL bPALCreatedTempFile;   // TRUE if it's a PAL created file
        DWORD dwDesiredAccessWhenOpened;  // FILE_MAP_WRITE etc
    };

    class CFileMappingProcessLocalData
    {
    public:
        INT     UnixFd;                     /* File descriptor. */

#if ONE_SHARED_MAPPING_PER_FILEREGION_PER_PROCESS
        dev_t   MappedFileDevNum;           /* ID of device containing the file to be mapped */
        ino_t   MappedFileInodeNum;         /* Inode number of file to be mapped.
                                               These two fields are used used to uniquely
                                               identify files on systems that do not allow
                                               more than one shared mmapping per region of
                                               physical file, per process */
#endif
    };

    PAL_ERROR
    InternalCreateFileMapping(
        CPalThread *pThread,
        HANDLE hFile,
        LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
        DWORD flProtect,
        DWORD dwMaximumSizeHigh,
        DWORD dwMaximumSizeLow,
        LPCWSTR lpName,
        HANDLE *phMapping
        );

    PAL_ERROR
    InternalMapViewOfFile(
        CPalThread *pThread,
        HANDLE hFileMappingObject,
        DWORD dwDesiredAccess,
        DWORD dwFileOffsetHigh,
        DWORD dwFileOffsetLow,
        SIZE_T dwNumberOfBytesToMap,
        LPVOID *ppvBaseAddress
        );

    PAL_ERROR
    InternalUnmapViewOfFile(
        CPalThread *pThread,
        LPCVOID lpBaseAddress
        );

}

#endif /* _PAL_MAP_H_ */
