// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*++



Module Name:

    include/pal/shmemory.h

Abstract:
    Header file for interface to shared memory

How to use :

Lock/Release functions must be used when manipulating data in shared memory, to ensure inter-process synchronization.



--*/

#ifndef _PAL_SHMEMORY_H_
#define _PAL_SHMEMORY_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*
Type for shared memory blocks
 */
typedef LPVOID SHMPTR;

typedef enum {
    SIID_NAMED_OBJECTS,
    SIID_FILE_LOCKS,

    SIID_LAST
} SHM_INFO_ID;

/*++
SHMInitialize

Hook this process into the PAL shared memory system; initialize the shared
memory if no other process has done it.
--*/
BOOL SHMInitialize(void);

/*++
SHMCleanup

Release all shared memory resources held; remove ourselves from the list of
registered processes, and remove all shared memory files if no process remains
--*/
void SHMCleanup(void);

/*++
SHMLock

Restrict shared memory access to the current thread of the current process

(no parameters)

Return value :
    New lock count
--*/
int SHMLock(void);

/*++
SHMRelease

Release a lock on shared memory taken with SHMLock.

(no parameters)

Return value :
    New lock count
--*/
int SHMRelease(void);


/*++
Function :
    SHMGetInfo

    Retrieve some information from shared memory

Parameters :
    SHM_INFO_ID element : identifier of element to retrieve

Return value :
    Value of specified element

Notes :
    The SHM lock should be held while manipulating shared memory
--*/
SHMPTR SHMGetInfo(SHM_INFO_ID element);

/*++
Function :
    SHMSetInfo

    Place some information into shared memory

Parameters :
    SHM_INFO_ID element : identifier of element to save
    SHMPTR value : new value of element

Return value :
    TRUE if successful, FALSE otherwise.

Notes :
    The SHM lock should be held while manipulating shared memory
--*/
BOOL SHMSetInfo(SHM_INFO_ID element, SHMPTR value);

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_SHMEMORY_H_ */

