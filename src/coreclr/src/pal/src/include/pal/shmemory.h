// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    include/pal/shmemory.h

Abstract:
    Header file for interface to shared memory

How to use :

The SHMalloc function can be used to allocate memory in the shared memory area.
It returns a value of type SHMPTR, which will be useable in all participating
processes. The SHMPTR_TO_PTR macro can be used to convert a SHMPTR value into
an address valid *only* within the current process. Do NOT store pointers in
shared memory, since those will not be valid for other processes. If you need
to construct linked lists or other strctures that usually use pointers, use
SHMPTR values instead of pointers. In addition, Lock/Release functions must be
used when manipulating data in shared memory, to ensure inter-process synchronization.

Example :

//a simple linked list type
typedef struct
{
int count;
SHMPTR string;
SHMPTR next;
}SHMLIST;

// Allocate a new list item
SHMPTR new_item = SHMalloc(sizeof(SHMLIST));

// get a pointer to it
SHMLIST *item_ptr = (SHMLIST *)SHMPTR_TO_PTR(new_item);

// Allocate memory for the "string" member, initialize it
item_ptr->string = SHMalloc(strlen("string"));
LPSTR str_ptr = (LPSTR)SHMPTR_TO_PTR(item_ptr->string);
strcpy(str_ptr, "string");

//Take the shared memory lock to prevent anyone from modifying the linked list
SHMLock();

//get the list's head from somewhere
SHMPTR list_head = get_list_head();

//link the list to our new item
item_ptr->next = list_head

//get a pointer to the list head's structure
SHMLIST *head_ptr = (SHMLIST *)SHMPTR_TO_PTR(list_head);

//set the new item's count value based on the head's count value
item_ptr->count = head_ptr->count + 1;

//save the new item as the new head of the list
set_list_head(new_item);

//We're done modifying the list, release the lock
SHMRelease



--*/

#ifndef _PAL_SHMEMORY_H_
#define _PAL_SHMEMORY_H_

#ifdef __cplusplus
extern "C"
{
#endif // __cplusplus

/*
Type for shared memory blocks. use SHMPTR_TO_PTR to get a useable address.
 */
typedef DWORD_PTR SHMPTR;

#define MAX_SEGMENTS 256


typedef enum {
    SIID_PROCESS_INFO,/* pointers to PROCESS structures? */
    SIID_NAMED_OBJECTS,
    SIID_FILE_LOCKS,

    SIID_LAST
} SHM_INFO_ID;

typedef enum
{
    SHM_NAMED_MAPPINGS,      /* structs with map name, file name & flags? */
    SHM_NAMED_EVENTS,        /* structs with event names & ThreadWaitingList struct? */
    SHM_NAMED_MUTEXS,        /* structs with mutext names, and ThreadWaitingList struct */

    SHM_NAMED_LAST
} SHM_NAMED_OBJECTS_ID;

typedef struct _SMNO
{
    SHM_NAMED_OBJECTS_ID ObjectType;
    SHMPTR ShmNext;
    SHMPTR ShmObjectName;
    SHMPTR ShmSelf;

}SHM_NAMED_OBJECTS, * PSHM_NAMED_OBJECTS;


/*
SHMPTR_TO_PTR

Macro to convert a SHMPTR value into a valid (for this process) pointer.

In debug builds, we always call the function to do full checks.
In release builds, check if the segment is known, and if it is, do only minimal
validation (if segment is unknown, we have to call the function)
 */
#if _DEBUG

#define SHMPTR_TO_PTR(shmptr) \
    SHMPtrToPtr(shmptr)

#else /* !_DEBUG */

extern int shm_numsegments;

/* array containing the base address of each segment */
extern Volatile<LPVOID> shm_segment_bases[MAX_SEGMENTS];

#define SHMPTR_TO_PTR(shmptr)\
    ((shmptr)?(((static_cast<int>(shmptr)>>24)<shm_numsegments)?\
    reinterpret_cast<LPVOID>(reinterpret_cast<size_t>(shm_segment_bases[static_cast<int>(shmptr)>>24].Load())+(static_cast<int>(shmptr)&0x00FFFFFF)):\
    SHMPtrToPtr(shmptr)): static_cast<LPVOID>(NULL))


#endif /* _DEBUG */

/* Set ptr to NULL if shmPtr == 0, else set ptr to SHMPTR_TO_PTR(shmptr) 
   return FALSE if SHMPTR_TO_PTR returns NULL ptr from non null shmptr, 
   TRUE otherwise */
#define SHMPTR_TO_PTR_BOOL(ptr, shmptr) \
    ((shmptr != 0) ? ((ptr = SHMPTR_TO_PTR(shmptr)) != NULL) : ((ptr = NULL) == NULL))

/*++
SHMPtrToPtr

Convert a SHMPTR value into a useable pointer.

Unlike the macro defined above, this function performs as much validation as
possible, and can handle cases when the SHMPTR is located in an aread of shared
memory the process doesn't yet know about.
--*/
LPVOID SHMPtrToPtr(SHMPTR shmptr);

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
SHMalloc

Allocate a block of memory of the specified size

Parameters :
    size_t size : size of block required

Return value :
    A SHMPTR identifying the new block, or 0 on failure. Use SHMPtrToPtr to
    convert a SHMPTR into a useable pointer (but remember to lock the shared
    memory first!)

Notes :
    SHMalloc will fail if the requested size is larger than a certain maximum.
    At the moment, the maximum is 520 bytes (MAX_PATH_FNAME*2).
--*/
SHMPTR SHMalloc(size_t size);

/*++
SHMfree

Release a block of shared memory and put it back in the shared memory pool

Parameters :
    SHMPTR shmptr : identifier of block to release

(no return value)
--*/
void SHMfree(SHMPTR shmptr);

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
    TRUE if successfull, FALSE otherwise.

Notes :
    The SHM lock should be held while manipulating shared memory
--*/
BOOL SHMSetInfo(SHM_INFO_ID element, SHMPTR value);


/********************** Shared memory help functions ********************/

/*++
SHMStrDup

Duplicates the string in shared memory.

Returns the new address as SHMPTR on success.
Returns (SHMPTR)NULL on failure.
--*/
SHMPTR SHMStrDup( LPCSTR string );

/*++
SHMWStrDup

Duplicates the wide string in shared memory.

Returns the new address as SHMPTR on success.
Returns (SHMPTR)NULL on failure.
--*/
SHMPTR SHMWStrDup( LPCWSTR string );


/*++
SHMFindNamedObjectByName

Searches for an object whose name matches the name and ID passed in.

Returns a SHMPTR to its location in shared memory. If no object
matches the name, the function returns NULL and sets pbNameExists to FALSE.
If an object matches the name but is of a different type, the function
returns NULL and sets pbNameExists to TRUE.

--*/
SHMPTR SHMFindNamedObjectByName( LPCWSTR lpName, SHM_NAMED_OBJECTS_ID oid,
                                 BOOL *pbNameExists );

/*++ 
SHMRemoveNamedObject

Removes the specified named object from the list

No return.

note : the caller is reponsible for releasing all associated memory
--*/
void SHMRemoveNamedObject( SHMPTR shmNamedObject );

/*++ SHMAddNamedObject

Adds the specified named object to the list.

No return.
--*/
void SHMAddNamedObject( SHMPTR shmNewNamedObject );

#ifdef __cplusplus
}
#endif // __cplusplus

#endif /* _PAL_SHMEMORY_H_ */

