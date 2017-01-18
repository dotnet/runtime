// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*++



Module Name:

    shmemory/shmemory.c

Abstract:

    Implementation of shared memory infrastructure for IPC

Issues :

 Interprocess synchronization


There doesn't seem to be ANY synchronization mechanism that will work
inter-process AND be pthread-safe. FreeBSD's pthread implementation has no
support for inter-process synchronization (PTHREAD_PROCESS_SHARED);
"traditionnal" inter-process syncronization functions, on the other hand, are
not pthread-aware, and thus will block entire processes instead of only the
calling thread.

From suggestions and information obtained on the freebsd-hackers mailing list,
I have come up with 2 possible strategies to ensure serialized access to our
shared memory region

Note that the estimates of relative efficiency are wild guesses; my assumptions
are that blocking entire processes is least efficient, busy wait somewhat
better, and anything that does neither is preferable. However, the overhead of
complex solutions is likely to have an important impact on performance

Option 1 : very simple; possibly less efficient. in 2 words : "busy wait"
Basically,

while(InterlockedCompareExchange(spinlock_in_shared_memory, 1, 0)
    sched_yield();

In other words, if a value is 0, set it to 1; otherwise, try again until we
succeed. use shed_yield to give the system a chance to schedule other threads
while we wait. (once a thread succeeds at this, it does its work, then sets
the value back to 0)
One inconvenient : threads will not unblock in the order they are blocked;
once a thread releases the mutex, whichever waiting thread is scheduled next
will be unblocked. This is what is called the "thundering herd" problem, and in
extreme cases, can lead to starvation
Update : we'll set the spinlock to our PID instead of 1, that way we can find
out if the lock is held by a dead process.

Option 2 : possibly more efficient, much more complex, borders on
"over-engineered". I'll explain it in stages, in the same way I deduced it.

Option 2.1 : probably less efficient, reasonably simple. stop at step 2)

1) The minimal, original idea was to use SysV semaphores for synchronization.
This didn't work, because semaphores block the entire process, which can easily
lead to deadlocks (thread 1 takes sem, thread 2 tries to take sem, blocks
process, thread 1 is blocked and never releases sem)

2) (this is option 2.1) Protect the use of the semaphores in critical sections.
Enter the critical section before taking the semaphore, leave the section after
releasing the semaphore. This ensures that 2 threads of the same process will
never try to acquire the semaphore at the same time, which avoids deadlocks.
However, the entire process still blocks if another process has the semaphore.
Here, unblocking order should match blocking order (assuming the semaphores work
properly); therefore, no risk of starvation.

3) This is where it gets complicated. To avoid blocking whole processes, we
can't use semaphores. One suggestion I got was to use multi-ended FIFOs, here's
how it would work.

-as in option 1, use InterlockedCompareExchange on a value in shared memory.
-if this was not succesful (someone else has locked the shared memory), then :
    -open a special FIFO for reading; try to read 1 byte. This will block until
     someone writes to it, and *should* only block the current thread. (note :
     more than one thread/process can open the same FIFO and block on read(),
     in this case, only one gets woken up when someone writes to it.
     *which* one is, again, not predictable; this may lead to starvation)
    -once we are unblocked, we have the lock.
-once we have the lock (either from Interlocked...() or from read()),
 we can do our work
-once the work is done, we open the FIFO for writing. this will fail if no one
 is listening.
-if no one is listening, release the lock by setting the shared memory value
 back to 0
-if someone is listening, write 1 byte to the FIFO to wake someone, then close
 the FIFO. the value in shared memory will remain nonzero until a thread tries
 to wake the next one and sees no one is listening.

problem with this option : it is possible for a thread to call Interlocked...()
BETWEEN the failed "open for write" attempt and the subsequent restoration of
the SHM value back to zero. In this case, that thread will go to sleep and will
not wake up until *another* thread asks for the lock, takes it and releases it.

so to fix that, we come to step

4) Instead of using InterlockedCompareExchange, use a SysV semaphore :
-when taking the lock :
    -take the semaphore
    -try to take the lock (check if value is zero, change it to 1 if it is)
    -if we fail : open FIFO for reading, release the semaphore, read() and block
    -if we succeed : release the semaphore
-when releasing the lock :
    -take the semaphore
    -open FIFO for write
    -if we succeed, release semaphore, then write value
    -if we fail, reset SHM value to 0, then release semaphore.

Yes, using a SysV semaphore will block the whole process, but for a very short
time (unlike option 2.1)
problem with this : again, we get deadlocks if 2 threads from a single process
try to take the semaphore. So like in option 2.1, we ave to wrap the semaphore
usage in a critical section. (complex enough yet?)

so the locking sequence becomes EnterCriticalSection - take semaphore - try to
    lock - open FIFO - release semaphore - LeaveCriticalSection - read
and the unlocking sequence becomes EnterCS - take sem - open FIFO - release
    sem - LeaveCS - write

Once again, the unblocking order probably won't match the blocking order.
This could be fixed by using multiple FIFOs : waiting thread open their own
personal FIFO, write the ID of their FIFO to another FIFO. The thread that wants
to release the lock reads ID from that FIFO, determines which FIFO to open for
writing and writes a byte to it. This way, whoever wrote its ID to the FIFO
first will be first to awake. How's that for complexity?

So to summarize, the options are
1 - busy wait
2.1 - semaphores + critical sections (whole process blocks)
2 - semaphores + critical sections + FIFOs (minimal process blocking)
2.2 - option 2 with multiple FIFOs (minimal process blocking, order preserved)

Considering the overhead involved in options 2 & 2.2, it is our guess that
option 1 may in fact be more efficient, and this is how we'll implement it for
the moment. Note that other platforms may not present the same difficulties
(i.e. other pthread implementations may support inter-process mutexes), and may
be able to use a simpler, more efficient approach.

B] Reliability.
It is important for the shared memory implementation to be as foolproof as
possible. Since more than one process will be able to modify the shared data,
it becomes possible for one unstable process to destabilize the others. The
simplest example is a process that dies while modifying shared memory : if
it doesn't release its lock, we're in trouble. (this case will be taken care
of by using PIDs in the spinlock; this we we can check if the locking process
is still alive).



--*/

#include "config.h"
#include "pal/palinternal.h"
#include "pal/dbgmsg.h"
#include "pal/shmemory.h"
#include "pal/critsect.h"
#include "pal/shmemory.h"
#include "pal/init.h"
#include "pal/process.h"
#include "pal/misc.h"

#include <sys/types.h>
#include <sys/stat.h>
#include <sys/mman.h>
#include <unistd.h>
#include <signal.h>
#include <errno.h>
#include <string.h>
#include <sched.h>
#include <pthread.h>

#if HAVE_YIELD_SYSCALL
#include <sys/syscall.h>
#endif  /* HAVE_YIELD_SYSCALL */
        
SET_DEFAULT_DEBUG_CHANNEL(SHMEM);

/* Macro-definitions **********************************************************/

/* rounds 'val' up to be divisible by 'r'.  'r' must be a power of two. */
#ifndef roundup
#define roundup(val, r)  ( ((val)+(r)-1) & ~( (r)-1 ) )
#endif

#define SEGMENT_NAME_SUFFIX_LENGTH 10

/*
SHMPTR structure :
High byte is SHM segment number
Low bytes are offset in the segment
 */
#define SHMPTR_SEGMENT(shmptr) \
    (((shmptr)>>24)&0xFF)

#define SHMPTR_OFFSET(shmptr) \
    ((shmptr)&0x00FFFFFF)

#define MAKE_SHMPTR(segment,offset) \
    ((SHMPTR)((((segment)&0xFF)<<24)|((offset)&0x00FFFFFF)))

/*#define MAX_SEGMENTS 256*//*definition is now in shmemory.h*/

/* Use MAP_NOSYNC to improve performance if it's available */
#if defined(MAP_NOSYNC)
#define MAPFLAGS MAP_NOSYNC|MAP_SHARED
#else
#define MAPFLAGS MAP_SHARED
#endif


/* Type definitions ***********************************************************/

enum SHM_POOL_SIZES
{
    SPS_16 = 0,      /* 16 bytes */
    SPS_32,         /* 32 bytes */
    SPS_64,         /* 64 bytes */
    SPS_MAXPATHx2,  /* 520 bytes, for long Unicode paths */

    SPS_LAST
};
/* Block size associated to each SPS identifier */
static const int block_sizes[SPS_LAST] = {16,32,64,roundup((MAX_LONGPATH+1)*2, sizeof(INT64))};

/*
SHM_POOL_INFO
Description of a shared memory pool for a specific block size.

Note on pool structure :
first_free identifies the first available SHMPTR in the block. Free blocks are
arranged in a linked list, each free block indicating the location of the next
one. To walk the list, do something like this :
SHMPTR *shmptr_ptr=(SHMPTR *)SHMPTR_TO_PTR(pool->first_free)
while(shm_ptr)
{
    SHMPTR next = *shmptr_ptr;
    shmptr_ptr = (SHMPTR *)SHMPTR_TO_PTR(next)
}
 */
typedef struct
{
    int item_size;          /* size of 1 block, in bytes */
    int num_items;          /* total number of blocks in the pool */
    int free_items;         /* number of unused items in the pool */
    SHMPTR first_free;      /* location of first available block in the pool */
}SHM_POOL_INFO;

/*
SHM_SEGMENT_HEADER
Description of a single shared memory segment

Notes on segment names :
next_semgent contains the string generated by mkstemp() when a new segment is
generated. This allows processes to map segment files created by other
processes. To get the file name of a segment file, concatenate
"segment_name_prefix" and "next_segment".

Notes on pool segments :
Each segment is divided into one pool for each defined block size (SPS_*).
These pools are linked with pools in other segment to form one large pool for
each block size, so that SHMAlloc() doesn't have to search each segment to find
an available block.
the first_ and last_pool_blocks indicate the first and last block in a single
segment for each block size. This allows SHMFree() to determine the size of a
block by comparing its value with these boundaries. (note that within each
segment, each pool is composed of a single contiguous block of memory)
*/
typedef struct
{
    Volatile<SHMPTR> first_pool_blocks[SPS_LAST];
    Volatile<SHMPTR> last_pool_blocks[SPS_LAST];
} SHM_SEGMENT_HEADER;

/*
SHM_FIRST_HEADER
Global information about the shared memory system
In addition to the standard SHM_SEGGMENT_HEADER, the first segment contains some
information required to properly use the shared memory system.

The spinlock is used to ensure that only one process accesses shared memory at
the same time. A process can only take the spinlock if its contents is 0, and
it takes the spinlock by placing its PID in it. (this allows a process to catch
the special case where it tries to take a spinlock it already owns.

The first_* members will contain the location of the first element in the
various linked lists of shared information
 */

#ifdef TRACK_SHMLOCK_OWNERSHIP

#define SHMLOCK_OWNERSHIP_HISTORY_ARRAY_SIZE 5

#define CHECK_CANARIES(header) \
    _ASSERTE(HeadSignature == header->dwHeadCanaries[0]); \
    _ASSERTE(HeadSignature == header->dwHeadCanaries[1]); \
    _ASSERTE(TailSignature == header->dwTailCanaries[0]);   \
    _ASSERTE(TailSignature == header->dwTailCanaries[1])

typedef struct _pid_and_tid
{
    Volatile<pid_t> pid;
    Volatile<pthread_t> tid;
} pid_and_tid;

const DWORD HeadSignature  = 0x48454144;
const DWORD TailSignature  = 0x5441494C;

#endif // TRACK_SHMLOCK_OWNERSHIP

typedef struct
{
    SHM_SEGMENT_HEADER header;
#ifdef TRACK_SHMLOCK_OWNERSHIP
    Volatile<DWORD> dwHeadCanaries[2];
#endif // TRACK_SHMLOCK_OWNERSHIP
    Volatile<pid_t> spinlock;
#ifdef TRACK_SHMLOCK_OWNERSHIP
    Volatile<DWORD> dwTailCanaries[2];
    pid_and_tid pidtidCurrentOwner;
    pid_and_tid pidtidOwners[SHMLOCK_OWNERSHIP_HISTORY_ARRAY_SIZE];
    Volatile<ULONG> ulOwnersIdx;
#endif // TRACK_SHMLOCK_OWNERSHIP
    SHM_POOL_INFO pools[SPS_LAST]; /* information about each memory pool */
    Volatile<SHMPTR> shm_info[SIID_LAST]; /* basic blocks of shared information.*/
} SHM_FIRST_HEADER;


/* Static variables ***********************************************************/

/* Critical section to ensure that only one thread at a time accesses shared
memory. Rationale :
-Using a spinlock means that processes must busy-wait for the lock to be
 available. The critical section ensures taht only one thread will busy-wait,
 while the rest are put to sleep.
-Since the spinlock only contains a PID, it isn't possible to make a difference
 between threads of the same process. This could be resolved by using 2
 spinlocks, but this would introduce more busy-wait.
*/
static CRITICAL_SECTION shm_critsec;
                        
/* number of segments the current process knows about */
int shm_numsegments;

/* array containing the base address of each segment */
Volatile<LPVOID> shm_segment_bases[MAX_SEGMENTS];

/* number of locks the process currently holds (SHMLock calls without matching
SHMRelease). Because we take the critical section while inside a
SHMLock/SHMRelease pair, this is actually the number of locks held by a single
thread. */
static Volatile<LONG> lock_count;

/* thread ID of thread holding the SHM lock. used for debugging purposes : 
   SHMGet/SetInfo will verify that the calling thread holds the lock */
static Volatile<HANDLE> locking_thread;

/* Constants ******************************************************************/

/* size of a single segment : 256KB */
static const int segment_size = 0x40000;

/* Static function prototypes *************************************************/

static SHMPTR SHMInitPool(SHMPTR first, int block_size, int pool_size,
                          SHM_POOL_INFO *pool);
static SHMPTR SHMLinkPool(SHMPTR first, int block_size, int num_blocks);
static BOOL   SHMMapUnknownSegments(void);
static BOOL   SHMAddSegment(void);


#define init_waste()
#define log_waste(x,y)
#define save_waste()

/* Public function implementations ********************************************/

/*++
SHMInitialize

Hook this process into the PAL shared memory system; initialize the shared
memory if no other process has done it.

--*/
BOOL SHMInitialize(void)
{
    InternalInitializeCriticalSection(&shm_critsec);

    init_waste();
    
        int size;
        SHM_FIRST_HEADER *header;
        SHMPTR pool_start;
        SHMPTR pool_end;
        enum SHM_POOL_SIZES sps;

        TRACE("Now initializing global shared memory system\n");
        
        // Not really shared in CoreCLR; we don't try to talk to other CoreCLRs.
        shm_segment_bases[0] = mmap(NULL, segment_size,PROT_READ|PROT_WRITE,
                                    MAP_ANON|MAP_PRIVATE, -1, 0);
        if(shm_segment_bases[0] == MAP_FAILED)
        {
            ERROR("mmap() failed; error is %d (%s)\n", errno, strerror(errno));
            return FALSE;
        }
        TRACE("Mapped first SHM segment at %p\n",shm_segment_bases[0].Load());

        /* Initialize first segment's header */
        header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

        InterlockedExchange((LONG *)&header->spinlock, 0);
        
#ifdef TRACK_SHMLOCK_OWNERSHIP
        header->dwHeadCanaries[0] = HeadSignature;
        header->dwHeadCanaries[1] = HeadSignature;
        header->dwTailCanaries[0] = TailSignature;
        header->dwTailCanaries[1] = TailSignature;        

        // Check spinlock size
        _ASSERTE(sizeof(DWORD) == sizeof(header->spinlock));
        // Check spinlock alignment
        _ASSERTE(0 == ((DWORD_PTR)&header->spinlock % (DWORD_PTR)sizeof(void *)));        
#endif // TRACK_SHMLOCK_OWNERSHIP

#ifdef TRACK_SHMLOCK_OWNERSHIP
        header->pidtidCurrentOwner.pid = 0;
        header->pidtidCurrentOwner.tid = 0;
        memset((void *)header->pidtidOwners, 0, sizeof(header->pidtidOwners));
        header->ulOwnersIdx = 0;
#endif // TRACK_SHMLOCK_OWNERSHIP

        /* SHM information array starts with NULLs */
        memset((void *)header->shm_info, 0, SIID_LAST*sizeof(SHMPTR));

        /* Initialize memory pools */

        /* first pool starts right after header */
        pool_start = roundup(sizeof(SHM_FIRST_HEADER), sizeof(INT64));

        /* Same size for each pool, ensuring alignment is correct */
        size = ((segment_size-pool_start)/SPS_LAST) & ~(sizeof(INT64)-1);

        for (sps = static_cast<SHM_POOL_SIZES>(0); sps < SPS_LAST;
             sps = static_cast<SHM_POOL_SIZES>(sps + 1))
        {
            pool_end = SHMInitPool(pool_start, block_sizes[sps], size,
                                   (SHM_POOL_INFO *)&header->pools[sps]);

            if(pool_end ==0)
            {
                ERROR("SHMInitPool failed.\n");
                munmap(shm_segment_bases[0],segment_size);
                return FALSE;
            }
            /* save first and last element of each pool for this segment */
            header->header.first_pool_blocks[sps] = pool_start;
            header->header.last_pool_blocks[sps] = pool_end;

            /* next pool starts immediately after this one */
            pool_start +=size;
        }

        TRACE("Global shared memory initialization complete.\n");

    shm_numsegments = 1;
    lock_count = 0;
    locking_thread = 0;

    /* hook into all SHM segments */
    if(!SHMMapUnknownSegments())
    {
        ERROR("Error while mapping segments!\n");
        SHMCleanup();
        return FALSE;
    }
    return TRUE;
}

/*++
SHMCleanup

Release all shared memory resources held; remove ourselves from the list of
registered processes, and remove all shared memory files if no process remains

Note that this function does not use thread suspension wrapper for unlink and free
because all thread objects are deleted before this function is called
in PALCommonCleanup.

--*/
void SHMCleanup(void)
{
    SHM_FIRST_HEADER *header;
    pid_t my_pid;

    TRACE("Starting shared memory cleanup\n");

    SHMLock();
    SHMRelease();

    /* We should not be holding the spinlock at this point. If we are, release
       the spinlock. by setting it to 0 */
    my_pid = gPID;
    header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

    _ASSERT_MSG(header->spinlock != my_pid,
            "SHMCleanup called while the current process still owns the lock "
            "[owner thread=%u, current thread: %u]\n", 
            locking_thread.Load(), THREADSilentGetCurrentThreadId());

    /* Now for the interprocess stuff. */
    DeleteCriticalSection(&shm_critsec);


    /* Unmap memory segments */
    while(shm_numsegments)
    {
        shm_numsegments--;
        if ( -1 == munmap( shm_segment_bases[ shm_numsegments ], 
                           segment_size ) )
        {
            ASSERT( "munmap() failed; errno is %d (%s).\n",
                  errno, strerror( errno ) );
        }
    }

    save_waste();
    TRACE("SHMCleanup complete!\n");
}

/*++
SHMalloc

Allocate a block of memory of the specified size

Parameters :
    size_t size : size of block required

Return value :
    A SHMPTR identifying the new block, or 0 on failure. Use SHMPTR_TO_PTR to
    convert a SHMPTR into a useable pointer (but remember to lock the shared
    memory first!)

Notes :
    SHMalloc will fail if the requested size is larger than a certain maximum.
    At the moment, the maximum is 520 bytes (MAX_LONGPATH*2).
--*/
SHMPTR SHMalloc(size_t size)
{
    enum SHM_POOL_SIZES sps;
    SHMPTR first_free;
    SHMPTR next_free;
    SHM_FIRST_HEADER *header;
    SHMPTR *shmptr_ptr;

    TRACE("SHMalloc() called; requested size is %u\n", size);

    if(0 == size)
    {
        WARN("Got a request for a 0-byte block! returning 0\n");
        return 0;
    }

    /* Find the first block size >= requested size */
    for (sps = static_cast<SHM_POOL_SIZES>(0); sps < SPS_LAST;
         sps = static_cast<SHM_POOL_SIZES>(sps + 1))
    {
        if (size <= static_cast<size_t>(block_sizes[sps]))
        {
            break;
        }
    }

    /* If no block size is found, requested size was too large. */
    if( SPS_LAST == sps )
    {
        ASSERT("Got request for shared memory block of %u bytes; maximum block "
              "size is %d.\n", size, block_sizes[SPS_LAST-1]);
        return 0;
    }

    TRACE("Best block size is %d (%d bytes wasted)\n",
          block_sizes[sps], block_sizes[sps]-size );

    log_waste(sps, block_sizes[sps]-size);

    SHMLock();
    header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

    /* If there are no free items of the specified size left, it's time to
       allocate a new shared memory segment.*/
    if(header->pools[sps].free_items == 0)
    {
        TRACE("No blocks of %d bytes left; allocating new segment.\n",
              block_sizes[sps]);
        if(!SHMAddSegment())
        {
            ERROR("Unable to allocate new shared memory segment!\n");
            SHMRelease();
            return 0;
        }
    }

    /* Remove the first free block from the pool */
    first_free = header->pools[sps].first_free;
    shmptr_ptr = static_cast<SHMPTR*>(SHMPTR_TO_PTR(first_free));

    if( 0 == first_free )
    {
        ASSERT("First free block in %d-byte pool (%08x) was invalid!\n",
              block_sizes[sps], first_free);
        SHMRelease();
        return 0;
    }

    /* the block "first_free" is the head of a linked list of free blocks;
       take the next link in the list and set it as new head of list. */
    next_free = *shmptr_ptr;
    header->pools[sps].first_free = next_free;
    header->pools[sps].free_items--;

    /* make sure we're still in a sane state */
    if(( 0 == header->pools[sps].free_items && 0 != next_free) ||
       ( 0 != header->pools[sps].free_items && 0 == next_free))
    {
        ASSERT("free block count is %d, but next free block is %#x\n", 
               header->pools[sps].free_items, next_free);
        /* assume all remaining blocks in the pool are corrupt */
        header->pools[sps].first_free = 0;
        header->pools[sps].free_items = 0;
    }
    else if (0 != next_free && 0 == SHMPTR_TO_PTR(next_free) )
    {
        ASSERT("Next free block (%#x) in %d-byte pool is invalid!\n",
               next_free, block_sizes[sps]);
        /* assume all remaining blocks in the pool are corrupt */
        header->pools[sps].first_free = 0;
        header->pools[sps].free_items = 0;
    }

    SHMRelease();

    TRACE("Allocation successful; %d blocks of %d bytes left. Returning %08x\n",
          header->pools[sps].free_items, block_sizes[sps], first_free);
    return first_free;
}

/*++
SHMfree

Release a block of shared memory and put it back in the shared memory pool

Parameters :
    SHMPTR shmptr : identifier of block to release

(no return value)
--*/
void SHMfree(SHMPTR shmptr)
{
    int segment;
    int offset;
    SHM_SEGMENT_HEADER *header;
    SHM_FIRST_HEADER *first_header;
    enum SHM_POOL_SIZES sps;
    SHMPTR *shmptr_ptr;

    if(0 == shmptr)
    {
        WARN("can't SHMfree() a NULL SHMPTR!\n");
        return;
    }
    SHMLock();

    TRACE("Releasing SHMPTR 0x%08x\n", shmptr);

    shmptr_ptr = static_cast<SHMPTR*>(SHMPTR_TO_PTR(shmptr));

    if(!shmptr_ptr)
    {
        ASSERT("Tried to free an invalid shared memory pointer 0x%08x\n", shmptr);
        SHMRelease();
        return;
    }

    /* note : SHMPTR_TO_PTR has already validated the segment/offset pair */
    segment = SHMPTR_SEGMENT(shmptr);
    header = (SHM_SEGMENT_HEADER *)shm_segment_bases[segment].Load();

    /* Find out the size of this block. Each segment tells where are its first
       and last blocks for each block size, so we simply need to check in which
       interval the block fits */
    for (sps = static_cast<SHM_POOL_SIZES>(0); sps < SPS_LAST;
         sps = static_cast<SHM_POOL_SIZES>(sps + 1))
    {
        if(header->first_pool_blocks[sps]<=shmptr &&
           header->last_pool_blocks[sps]>=shmptr)
        {
            break;
        }
    }

    /* If we didn't find an interval, then the block doesn't really belong in
       this segment (shouldn't happen, the offset check in SHMPTR_TO_PTR should
       have caught this.)  */
    if(sps == SPS_LAST)
    {
        ASSERT("Shared memory pointer 0x%08x is out of bounds!\n", shmptr);
        SHMRelease();
        return;
    }

    TRACE("SHMPTR 0x%08x is a %d-byte block located in segment %d\n",
          shmptr, block_sizes[sps], segment);

    /* Determine the offset of this block (in bytes) relative to the first
       block of the same size in this segment */
    offset = shmptr - header->first_pool_blocks[sps];

    /* Make sure that the offset is a multiple of the block size; otherwise,
       this isn't a real SHMPTR */
    if( 0 != ( offset % block_sizes[sps] ) )
    {
        ASSERT("Shared memory pointer 0x%08x is misaligned!\n", shmptr);
        SHMRelease();
        return;
    }

    /* Put the SHMPTR back in its pool. */
    first_header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

    /* first_free is the head of a linked list of free SHMPTRs. All we need to
       do is make shmptr point to first_free, and set shmptr as the new head
       of the list. */
    *shmptr_ptr = first_header->pools[sps].first_free;
    first_header->pools[sps].first_free = shmptr;
    first_header->pools[sps].free_items++;

    TRACE("SHMPTR 0x%08x released; there are now %d blocks of %d bytes "
          "available\n", shmptr, first_header->pools[sps].free_items,
          block_sizes[sps]);
    SHMRelease();
}

/*++
SHMLock

Restrict shared memory access to the current thread of the current process

(no parameters)

Return value :
    New lock count

Notes :
see comments at the declaration of shm_critsec for rationale of critical
section usage
--*/
int SHMLock(void)
{
    /* Hold the critical section until the lock is released */
    PALCEnterCriticalSection(&shm_critsec);

    _ASSERTE((0 == lock_count && 0 == locking_thread) ||
             (0 < lock_count && (HANDLE)pthread_self() == locking_thread));
             
    if(lock_count == 0)
    {
        SHM_FIRST_HEADER *header;
        pid_t my_pid, tmp_pid;
        int spincount = 1;
#ifdef TRACK_SHMLOCK_OWNERSHIP
        ULONG ulIdx;
#endif // TRACK_SHMLOCK_OWNERSHIP

        TRACE("First-level SHM lock : taking spinlock\n");

        header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

        // Store the id of the current thread as the (only) one that is 
        // trying to grab the spinlock from the current process
        locking_thread = (HANDLE)pthread_self();

        my_pid = gPID;
        
        while(TRUE)
        {
#ifdef TRACK_SHMLOCK_OWNERSHIP
            _ASSERTE(0 != my_pid);
            _ASSERTE(getpid() == my_pid);
            _ASSERTE(my_pid != header->spinlock);
            CHECK_CANARIES(header);
#endif // TRACK_SHMLOCK_OWNERSHIP

            //
            // Try to grab the spinlock
            //
            tmp_pid = InterlockedCompareExchange((LONG *) &header->spinlock, my_pid,0);

#ifdef TRACK_SHMLOCK_OWNERSHIP
            CHECK_CANARIES(header);
#endif // TRACK_SHMLOCK_OWNERSHIP

            if (0 == tmp_pid)
            {
                // Spinlock acquired: break out of the loop
                break;
            }

            /* Check if lock holder is alive. If it isn't, we can reset the
               spinlock and try to take it again. If it is, we have to wait.
               We use "spincount" to do this check only every 8th time through
               the loop, for performance reasons.*/
            if( (0 == (spincount&0x7)) &&
                (-1 == kill(tmp_pid,0)) &&
                (errno == ESRCH))
            {
                TRACE("SHM spinlock owner (%08x) is dead; releasing its lock\n",
                      tmp_pid);

                InterlockedCompareExchange((LONG *) &header->spinlock, 0, tmp_pid);
            }
            else
            {
                /* another process is holding the lock... we want to yield and 
                   give the holder a chance to release the lock
                   The function sched_yield() only yields to a thread in the 
                   current process; this doesn't help us much, anddoens't help 
                   at all if there's only 1 thread. There doesn't seem to be 
                   any clean way to force a yield to another process, but the 
                   FreeBSD syscall "yield" does the job. We alternate between 
                   both methods to give other threads of this process a chance 
                   to run while we wait.
                 */
#if HAVE_YIELD_SYSCALL
                if(spincount&1)
                {
#endif  /* HAVE_YIELD_SYSCALL */
                    sched_yield();
#if HAVE_YIELD_SYSCALL
                }
                else
                {
                    /* use the syscall first, since we know we'l need to yield 
                       to another process eventually - the lock can't be held 
                       by the current process, thanks to the critical section */
                    syscall(SYS_yield, 0);
                }
#endif  /* HAVE_YIELD_SYSCALL */
            }

            // Increment spincount
            spincount++;
        }

        _ASSERT_MSG(my_pid == header->spinlock,
            "\n(my_pid = %u) != (header->spinlock = %u)\n"
            "tmp_pid         = %u\n"
            "spincount       = %d\n"
            "locking_thread  = %u\n", 
            (DWORD)my_pid, (DWORD)header->spinlock, 
            (DWORD)tmp_pid,
            (int)spincount,
            (HANDLE)locking_thread);

#ifdef TRACK_SHMLOCK_OWNERSHIP
        _ASSERTE(0 == header->pidtidCurrentOwner.pid);
        _ASSERTE(0 == header->pidtidCurrentOwner.tid);

        header->pidtidCurrentOwner.pid = my_pid;
        header->pidtidCurrentOwner.tid = locking_thread;

        ulIdx = header->ulOwnersIdx % (sizeof(header->pidtidOwners) / sizeof(header->pidtidOwners[0])); 
        
        header->pidtidOwners[ulIdx].pid = my_pid;
        header->pidtidOwners[ulIdx].tid = locking_thread;

        header->ulOwnersIdx += 1;
#endif // TRACK_SHMLOCK_OWNERSHIP
        
    }

    lock_count++;
    TRACE("SHM lock level is now %d\n", lock_count.Load());
    return lock_count;
}

/*++
SHMRelease

Release a lock on shared memory taken with SHMLock.

(no parameters)

Return value :
    New lock count

--*/
int SHMRelease(void)
{
    /* prevent a thread from releasing another thread's lock */
    PALCEnterCriticalSection(&shm_critsec);

    if(lock_count==0)
    {
        ASSERT("SHMRelease called without matching SHMLock!\n");
        PALCLeaveCriticalSection(&shm_critsec);
        return 0;
    }

    lock_count--;

    _ASSERTE(lock_count >= 0);

    /* If lock count is 0, this call matches the first Lock call; it's time to
       set the spinlock back to 0. */
    if(lock_count == 0)
    {
        SHM_FIRST_HEADER *header;
        pid_t my_pid, tmp_pid;

        TRACE("Releasing first-level SHM lock : resetting spinlock\n");

        my_pid = gPID;
        
        header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

#ifdef TRACK_SHMLOCK_OWNERSHIP
        CHECK_CANARIES(header);
        _ASSERTE(0 != my_pid);
        _ASSERTE(getpid() == my_pid);        
        _ASSERTE(my_pid == header->spinlock);
        _ASSERTE(header->pidtidCurrentOwner.pid == my_pid);
        _ASSERTE(pthread_self() == header->pidtidCurrentOwner.tid);
        _ASSERTE((pthread_t)locking_thread == header->pidtidCurrentOwner.tid);

        header->pidtidCurrentOwner.pid = 0;
        header->pidtidCurrentOwner.tid = 0;
#endif // TRACK_SHMLOCK_OWNERSHIP


        /* Make sure we don't touch the spinlock if we don't own it. We're
           supposed to own it if we get here, but just in case... */
        tmp_pid = InterlockedCompareExchange((LONG *) &header->spinlock, 0, my_pid);

        if (tmp_pid != my_pid)
        {
            ASSERT("Process 0x%08x tried to release spinlock owned by process "
                   "0x%08x! \n", my_pid, tmp_pid);
            PALCLeaveCriticalSection(&shm_critsec);
            return 0;
        }

        /* indicate no thread (in this process) holds the SHM lock */
        locking_thread = 0;

#ifdef TRACK_SHMLOCK_OWNERSHIP
        CHECK_CANARIES(header);
#endif // TRACK_SHMLOCK_OWNERSHIP
    }

    TRACE("SHM lock level is now %d\n", lock_count.Load());

    /* This matches the EnterCriticalSection from SHMRelease */
    PALCLeaveCriticalSection(&shm_critsec);

    /* This matches the EnterCriticalSection from SHMLock */
    PALCLeaveCriticalSection(&shm_critsec);

    return lock_count;
}

/*++
SHMPtrToPtr

Convert a SHMPTR value to a valid pointer within the address space of the
current process

Parameters :
    SHMPTR shmptr : SHMPTR value to convert into a pointer

Return value :
    Address corresponding to the given SHMPTR, valid for the current process

Notes :
(see notes for SHMPTR_SEGMENT macro for details on SHMPTR structure)

It is possible for the segment index to be greater than the known total number
of segments (shm_numsegments); this means that the SHMPTR points to a memory
block in a shared memory segment this process doesn't know about. In this case,
we must obtain an address for that new segment and add it to our array
(see SHMMapUnknownSegments for details)

In the simplest case (no need to map new segments), there is no need to hold
the lock, since we don't access any information that can change
--*/
LPVOID SHMPtrToPtr(SHMPTR shmptr)
{
    void *retval;
    int segment;
    int offset;

    TRACE("Converting SHMPTR 0x%08x to a valid pointer...\n", shmptr);
    if(!shmptr)
    {
        WARN("Got SHMPTR \"0\"; returning NULL pointer\n");
        return NULL;
    }

    segment = SHMPTR_SEGMENT(shmptr);

    /* If segment isn't known, it may have been added by another process. We
       need to map all new segments into our address space. */
    if(segment>= shm_numsegments)
    {
        TRACE("SHMPTR is in segment %d, we know only %d. We must now map all "
              "unknowns.\n", segment, shm_numsegments);
        SHMMapUnknownSegments();

        /* if segment is still unknown, then it doesn't exist */
        if(segment>=shm_numsegments)
        {
            ASSERT("Segment %d still unknown; returning NULL\n", segment);
            return NULL;
        }
        TRACE("Segment %d found; continuing\n", segment);
    }

    /* Make sure the offset doesn't point outside the segment */
    offset = SHMPTR_OFFSET(shmptr);
    if(offset>=segment_size)
    {
        ASSERT("Offset %d is larger than segment size (%d)! returning NULL\n",
              offset, segment_size);
        return NULL;

    }

    /* Make sure the offset doesn't point in the segment's header */
    if(segment == 0)
    {
        if (static_cast<size_t>(offset) < roundup(sizeof(SHM_FIRST_HEADER), sizeof(INT64)))
        {
            ASSERT("Offset %d is in segment header! returning NULL\n", offset);
            return NULL;
        }
    }
    else
    {
        if (static_cast<size_t>(offset) < sizeof(SHM_SEGMENT_HEADER))
        {
            ASSERT("Offset %d is in segment header! returning NULL\n", offset);
            return NULL;
        }
    }

    retval = shm_segment_bases[segment];
    retval = static_cast<BYTE*>(retval) + offset;

    TRACE("SHMPTR %#x is at offset %d in segment %d; maps to address %p\n",
          shmptr, offset, segment, retval);
    return retval;
}


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
SHMPTR SHMGetInfo(SHM_INFO_ID element)
{
    SHM_FIRST_HEADER *header = NULL;
    SHMPTR retval = 0;

    if(element < 0 || element >= SIID_LAST)
    {
        ASSERT("Invalid SHM info element %d\n", element);
        return 0;
    }

    /* verify that this thread holds the SHM lock. No race condition: if the 
       current thread is here, it can't be in SHMLock or SHMUnlock */
    if( (HANDLE)pthread_self() != locking_thread )
    {
        ASSERT("SHMGetInfo called while thread does not hold the SHM lock!\n");
    }

    header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

    retval = header->shm_info[element];

    TRACE("SHM info element %d is %08x\n", element, retval );
    return retval;
}


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
BOOL SHMSetInfo(SHM_INFO_ID element, SHMPTR value)
{
    SHM_FIRST_HEADER *header;

    if(element < 0 || element >= SIID_LAST)
    {
        ASSERT("Invalid SHM info element %d\n", element);
        return FALSE;
    }
    
    /* verify that this thread holds the SHM lock. No race condition: if the 
       current thread is here, it can't be in SHMLock or SHMUnlock */
    if( (HANDLE)pthread_self() != locking_thread )
    {
        ASSERT("SHMGetInfo called while thread does not hold the SHM lock!\n");
    }

    header = (SHM_FIRST_HEADER*)shm_segment_bases[0].Load();

    TRACE("Setting SHM info element %d to %08x; used to be %08x\n",
          element, value, header->shm_info[element].Load() );

    header->shm_info[element] = value;

    return TRUE;
}


/* Static function implementations ********************************************/

/*++
SHMInitPool

Perform one-time initialization for a shared memory pool.

Parameters :
    SHMPTR first : SHMPTR of first memory block in the pool
    int block_size : size (in bytes) of a memory block in this pool
    int pool_size : total size (in bytes) of this pool
    SHM_POOL_INFO *pool : pointer to initialize with information about the pool

Return value :
    SHMPTR of last memory block in the pool

Notes :
This function is used to initialize the memory pools of the first SHM segment.
In addition to creating a linked list of SHMPTRs, it initializes the given
SHM_POOL_INFO based on the given information.
--*/
static SHMPTR SHMInitPool(SHMPTR first, int block_size, int pool_size,
                          SHM_POOL_INFO *pool)
{
    int num_blocks;
    SHMPTR last;

    TRACE("Initializing SHM pool for %d-byte blocks\n", block_size);

    /* Number of memory blocks of size "block_size" that can fit in "pool_size"
       bytes (rounded down) */
    num_blocks = pool_size/block_size;

    /* Create the initial linked list of free blocks */
    last = SHMLinkPool(first, block_size, num_blocks);
    if( 0 == last )
    {
        ERROR("Failed to create linked list of free blocks!\n");
        return 0;
    }

    /* Initialize SHM_POOL_INFO */
    pool->first_free = first;
    pool->free_items = num_blocks;
    pool->item_size = block_size;
    pool->num_items = num_blocks;

    TRACE("New SHM pool extends from SHMPTR 0x%08x to 0x%08x\n", first, last);
    return last;
}

/*++
SHMLinkPool

Joins contiguous blocks of memory into a linked list..

Parameters :
    SHMPTR first : First SHMPTR in the memory pool; first link in the list
    int block_size : size (in bytes) of the memory blocks
    int num_blocks : number of contiguous blocks to link

Return value :
    SHMPTR of last memory block in the pool

Notes :
The linked list is created by saving the value of the next SHMPTR in the list
in the memory location corresponding to the previous SHMPTR :
*(SHMPTR *)SHMPTR_TO_PTR(previous) = previous + block_size
--*/
static SHMPTR SHMLinkPool(SHMPTR first, int block_size, int num_blocks)
{
    LPBYTE item_ptr;
    SHMPTR *shmptr_ptr;
    SHMPTR next_shmptr;
    int i;

    TRACE("Linking %d blocks of %d bytes, starting at 0x%08x\n",
          num_blocks, block_size, first);

    item_ptr = static_cast<LPBYTE>(
        static_cast<LPBYTE>(shm_segment_bases[SHMPTR_SEGMENT(first)].Load()) +
            (SHMPTR_OFFSET(first)));
    next_shmptr = first/*+block_size*/;

    /* Link blocks together */
    for(i=0; i<num_blocks; i++)
    {
        next_shmptr += block_size;

        /* item_ptr is char * (so we can increment with +=blocksize), we cast
           it to a SHMPTR * and set its content to the next SHMPTR in the list*/
        shmptr_ptr = (SHMPTR *)item_ptr;
        *shmptr_ptr = next_shmptr;

        item_ptr+=block_size;
    }
    /* Last SHMPTR in the list must point to NULL */
    item_ptr-=block_size;
    shmptr_ptr = (SHMPTR *)item_ptr;
    *shmptr_ptr = 0;

    /* Return SHMPTR of last element in the list */
    next_shmptr -= block_size;

    TRACE("New linked pool goes from 0x%08x to 0x%08x\n", first, next_shmptr);
    return next_shmptr;
}

/*++
SHMMapUnknownSegments

Map into this process all SHM segments not yet mapped

(no parameters)

Return value :
    TRUE on success, FALSE in case of error
--*/
static BOOL SHMMapUnknownSegments(void)
{
    return TRUE;
}

/*++
SHMAddSegment

Create a new SHM segment, map it into this process, initialize it, then link it
to the other SHM segments

(no parameters)

Return value :
    TRUE on success, FALSE in case of error

Notes :
    This function assumes the SHM lock is held.
--*/
static BOOL SHMAddSegment(void)
{
    LPVOID segment_base;
    SHM_SEGMENT_HEADER *header;
    SHM_FIRST_HEADER *first_header;
    SHMPTR first_shmptr;
    SHMPTR *shmptr_ptr;
    int sps;
    int used_size;
    int new_size;
    int current_pool_size;
    int used_pool_size;
    int new_pool_size;
    int num_new_items;

    /* Map all segments this process doesn't yet know about, so we link the new
       segment at the right place */
    if(!SHMMapUnknownSegments())
    {
        ERROR("SHMMapUnknownSegments failed!\n");
        return FALSE;
    }

    /* Avoid overflowing */
    if(shm_numsegments == MAX_SEGMENTS)
    {
        ERROR("Can't map more segments : maximum number (%d) reached!\n",
              MAX_SEGMENTS);
        return FALSE;
    }

    TRACE("Creating SHM segment #%d\n", shm_numsegments);

    segment_base = mmap(NULL, segment_size, PROT_READ|PROT_WRITE,
                        MAP_ANON|MAP_PRIVATE,-1, 0);

    if(segment_base == MAP_FAILED)
    {
        ERROR("mmap() failed! error is %d (%s)\n", errno, strerror(errno));
        return FALSE;
    }

    shm_segment_bases[shm_numsegments] = segment_base;

    /* Save name (well, suffix) of new segment in the header of the old last
       segment, so that other processes know where it is. */
    header = (SHM_SEGMENT_HEADER *)shm_segment_bases[shm_numsegments-1].Load();

    /* Indicate that the new segment is the last one */
    header = (SHM_SEGMENT_HEADER *)segment_base;

    /* We're now ready to update our memory pools */

    first_header = (SHM_FIRST_HEADER *)shm_segment_bases[0].Load();

    /* Calculate total amount of used memory (in bytes) */
    used_size = 0;
    for(sps = 0; sps<SPS_LAST;sps++)
    {
        /* Add total size of this pool */
        used_size += first_header->pools[sps].num_items*block_sizes[sps];

        /* Remove unused size of this pool */
        used_size -= first_header->pools[sps].free_items*block_sizes[sps];
    }

    /* Determine how to divide the new segment between the pools for the
       different block sizes, then update the pool inforamtion accordingly
       Allocation strategy :
       1) Calculate the proportion of used memory used by each pool
       2) Allocate this proportion of the new segment to each pool
     */

    /* Add the new segment to the total amount of SHM memory */
    new_size = segment_size-roundup(sizeof(SHM_SEGMENT_HEADER), sizeof(INT64));

    /* Calculate value of first SHMPTR in the new segment : segment is
       shm_numsegments (not yet incremented); offset is the first byte after
       the segment header */
    first_shmptr = MAKE_SHMPTR(shm_numsegments,roundup(sizeof(SHM_SEGMENT_HEADER), sizeof(INT64)));

    TRACE("Updating SHM pool information; Total memory used is %d bytes; "
          "we are adding %d bytes\n", used_size, new_size);

    /* We want to allocate at least 1 block of each size (to avoid adding
       special cases everywhere). We remove the required space for these blocks
       from the size used in the calculations, then add 1 to each block count */
    for(sps=0;sps<SPS_LAST;sps++)
        new_size -= block_sizes[sps];

    /* Loop through all block sizes */
    for(sps=0; sps<SPS_LAST; sps++)
    {
        TRACE("Now processing block size \"%d\"...\n", block_sizes[sps]);
        /* amount of memory currently reserved for this block size */
        current_pool_size = first_header->pools[sps].num_items*block_sizes[sps];

        /* how much of that is actually used? */
        used_pool_size = current_pool_size -
                         first_header->pools[sps].free_items*block_sizes[sps];

        DBGOUT("%d bytes of %d bytes used (%d%%)\n", used_pool_size,
               current_pool_size, (used_pool_size*100)/current_pool_size);

        /* amount of memory we want to add to the pool for this block size :
           amount used by this pool/total amount used * new segment's size */
        new_pool_size = (((LONGLONG)used_pool_size)*new_size)/used_size;

        DBGOUT("Allocating %d bytes of %d to %d-byte pool\n",
               new_pool_size, new_size, block_sizes[sps]);

        /* determine the number of blocks that can fit in the chosen amount */
        num_new_items = new_pool_size/block_sizes[sps];

        /* make sure we allocate at least 1 block of each size */
        num_new_items +=1;

        DBGOUT("Adding %d new blocks\n", num_new_items);

        /* Save the first and last block of the current block size in the new
           segment; join all blocks in between in a linked list */
        header->first_pool_blocks[sps] = first_shmptr;
        header->last_pool_blocks[sps] = SHMLinkPool(first_shmptr,
                                                    block_sizes[sps],
                                                    num_new_items);

        /* Link the last block in the new linked list to the first block of the
           old global linked list. We don't use SHMPTR_TO_PTR because the pool
           data isn't updated yet */
        shmptr_ptr = reinterpret_cast<SHMPTR*>(
            static_cast<LPBYTE>(shm_segment_bases[SHMPTR_SEGMENT(header->last_pool_blocks[sps])].Load()) +
                     SHMPTR_OFFSET(header->last_pool_blocks[sps]));

        *shmptr_ptr = first_header->pools[sps].first_free;

        /* Save the first block of the new linked list as the new beginning of
           the global linked list; the global list now contains all new blocks
           AND all blocks that were already free */
        first_header->pools[sps].first_free = header->first_pool_blocks[sps];

        /* Update block counts to include new blocks */
        first_header->pools[sps].free_items+=num_new_items;
        first_header->pools[sps].num_items+=num_new_items;

        DBGOUT("There are now %d %d-byte blocks, %d are free\n",
               first_header->pools[sps].num_items, block_sizes[sps],
               first_header->pools[sps].free_items);

        /* Update first_shmptr to first byte after the new pool */
        first_shmptr+=num_new_items*block_sizes[sps];
    }
    shm_numsegments++;

    return TRUE;
}

/*++
SHMStrDup

Duplicates the string in shared memory.

Returns the new address as SHMPTR on success.
Returns (SHMPTR)NULL on failure.
--*/
SHMPTR SHMStrDup( LPCSTR string )
{
    UINT length = 0;
    SHMPTR retVal = 0;

    if ( string )
    {
        length = strlen( string );

        retVal = SHMalloc( ++length );

        if ( retVal != 0 ) 
        {
            LPVOID ptr = SHMPTR_TO_PTR( retVal );
            _ASSERT_MSG(ptr != NULL, "SHMPTR_TO_PTR returned NULL.\n");
            if (ptr != NULL)
            {
                memcpy( ptr, string, length );
            }
            else
            {
                // This code should never be reached. If a valid pointer
                // is passed to SHMPTR_TO_PTR and NULL is returned, then
                // there's a problem in either the macro, or the underlying
                // call to SHMPtrToPtr. In case the impossible happens,
                // though, free the memory and return NULL rather than
                // returning uninitialized memory.
                SHMfree( retVal );
                retVal = NULL;
            }
        }
    }
    return retVal;
}

/*++
SHMWStrDup

Duplicates the wide string in shared memory.

Returns the new address as SHMPTR on success.
Returns (SHMPTR)NULL on failure.
--*/
SHMPTR SHMWStrDup( LPCWSTR string )
{
    UINT length = 0;
    SHMPTR retVal = 0;

    if ( string )
    {
        length = ( PAL_wcslen( string ) + 1 ) * sizeof( WCHAR );
        
        retVal = SHMalloc( length );

        if ( retVal != 0 ) 
        {
            LPVOID ptr = SHMPTR_TO_PTR(retVal);
            _ASSERT_MSG(ptr != NULL, "SHMPTR_TO_PTR returned NULL.\n");
            if (ptr != NULL)
            {
                memcpy( ptr, string, length );
            }
            else
            {
                // This code should never be reached. If a valid pointer
                // is passed to SHMPTR_TO_PTR and NULL is returned, then
                // there's a problem in either the macro, or the underlying
                // call to SHMPtrToPtr. In case the impossible happens,
                // though, free the memory and return NULL rather than
                // returning uninitialized memory.
                SHMfree( retVal );
                retVal = NULL;
            }
        }
    }
    return retVal;
}



/*++
SHMFindNamedObjectByName

Searches for an object whose name matches the name and ID passed in.

Returns a SHMPTR to its location in shared memory. If no object
matches the name, the function returns NULL and sets pbNameExists to FALSE.
If an object matches the name but is of a different type, the function
returns NULL and sets pbNameExists to TRUE.

--*/
SHMPTR SHMFindNamedObjectByName( LPCWSTR lpName, SHM_NAMED_OBJECTS_ID oid,
                                 BOOL *pbNameExists )
{
    PSHM_NAMED_OBJECTS pNamedObject = NULL;
    SHMPTR shmNamedObject = 0;
    LPWSTR object_name = NULL;

    if(oid==SHM_NAMED_LAST)
    {
        ASSERT("Invalid named object type.\n");
        return 0;
    }
    
    if (pbNameExists == NULL)
    {
        ASSERT("pbNameExists must be non-NULL.\n");
    }

    SHMLock();
    
    *pbNameExists = FALSE;
    shmNamedObject = SHMGetInfo( SIID_NAMED_OBJECTS );
    
    TRACE( "Entering SHMFindNamedObjectByName looking for %S .\n", 
           lpName?lpName:W16_NULLSTRING );

    while ( shmNamedObject )
    {
        pNamedObject = (PSHM_NAMED_OBJECTS)SHMPTR_TO_PTR( shmNamedObject );
        if(NULL == pNamedObject)
        {
            ASSERT("Got invalid SHMPTR value; list of named objects is "
                   "corrupted.\n");
            break;
        }
        
        if ( pNamedObject->ShmObjectName )
        {
            object_name = (LPWSTR)SHMPTR_TO_PTR( pNamedObject->ShmObjectName );
        }

        if ( object_name && 
             PAL_wcscmp( lpName, object_name ) == 0 )
        {
            if(oid == pNamedObject->ObjectType)
            {
                TRACE( "Returning the kernel object %p.\n", pNamedObject );
            }
            else
            {
                shmNamedObject = 0;
                *pbNameExists = TRUE;
            }
            goto Exit;
        }
        shmNamedObject = pNamedObject->ShmNext;
    }

    shmNamedObject = 0;
    TRACE( "No matching kernel object was found.\n" );

Exit:
    SHMRelease();
    return shmNamedObject;

}

/*++ 
SHMRemoveNamedObject

Removes the specified named object from the list

No return.

note : the caller is reponsible for releasing all associated memory
--*/
void SHMRemoveNamedObject( SHMPTR shmNamedObject )
{
    PSHM_NAMED_OBJECTS pshmLast = 0;
    PSHM_NAMED_OBJECTS pshmCurrent = 0;

    TRACE( "Entered SHMDeleteNamedObject shmNamedObject = %d\n", shmNamedObject );
    SHMLock();

    pshmCurrent = 
        (PSHM_NAMED_OBJECTS)SHMPTR_TO_PTR( SHMGetInfo( SIID_NAMED_OBJECTS ) );
    pshmLast = pshmCurrent;

    while ( pshmCurrent )
    {
        if ( pshmCurrent->ShmSelf == shmNamedObject )
        {
            TRACE( "Patching the list.\n" );
            
            /* Patch the list, and delete the object. */
            if ( pshmLast->ShmSelf == pshmCurrent->ShmSelf )
            {
                /* Either the first element or no elements left. */
                SHMSetInfo( SIID_NAMED_OBJECTS, pshmCurrent->ShmNext );
            }
            else if ( (PSHM_NAMED_OBJECTS)SHMPTR_TO_PTR( pshmCurrent->ShmNext ) )
            {
                pshmLast->ShmNext = pshmCurrent->ShmNext;
            }
            else
            {
                /* Only one left. */
                pshmLast->ShmNext = 0;
            }
            
            break;
        }
        else
        {
            pshmLast = pshmCurrent;
            pshmCurrent = (PSHM_NAMED_OBJECTS)SHMPTR_TO_PTR( pshmCurrent->ShmNext );
        }
    }

    SHMRelease();
    return;
}

/*++ SHMAddNamedObject

Adds the specified named object to the list.

No return.
--*/
void SHMAddNamedObject( SHMPTR shmNewNamedObject )
{
    PSHM_NAMED_OBJECTS pshmNew = 0;
   
    pshmNew = (PSHM_NAMED_OBJECTS)SHMPTR_TO_PTR( shmNewNamedObject );
   
    if ( pshmNew == NULL )
    {
        ASSERT( "pshmNew should not be NULL\n" );
    }
 
    SHMLock();
    
    pshmNew->ShmNext = SHMGetInfo( SIID_NAMED_OBJECTS );
    
    if ( !SHMSetInfo( SIID_NAMED_OBJECTS, shmNewNamedObject ) )
    {
        ASSERT( "Unable to add the mapping object to shared memory.\n" );
    }

    SHMRelease();
    return;
}
