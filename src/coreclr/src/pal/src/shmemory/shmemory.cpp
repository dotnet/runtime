//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information. 
//

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

#if defined(_DEBUG) && defined(_HPUX_)
#define TRACK_SHMLOCK_OWNERSHIP
#endif // _DEBUG && _HPUX_

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
#ifndef CORECLR
    char next_segment[SEGMENT_NAME_SUFFIX_LENGTH+1]; /* name of next segment */
#endif // !CORECLR
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

#ifndef CORECLR
/* suffix template for mkstemp */
static char segment_name_template[MAX_LONGPATH];
static char lockfile_name[MAX_LONGPATH];
#endif // !defined(CORECLR)

/* Constants ******************************************************************/

#ifndef CORECLR

/* file name for first shared memory segment */
static const char first_segment_suffix[] = "segment_1";

/* prefix of file name for subsequent shared memory segments */
static const char segment_name_prefix[] = ".rotor_pal_shm";

/* name of lock file (for init/cleanup) */
static const char lockfile_shortname[] = ".rotor_pal_shmlockfile";

#endif // !CORECLR

/* size of a single segment : 256KB */
static const int segment_size = 0x40000;

#if !defined(CORECLR) && defined(_DEBUG)
/* environment variable, set to a non 0 value if we need to output waste 
   information to the file shm_waste_log (during process termination) */
static const char* PAL_SAVE_WASTE = "PAL_SAVE_WASTE";
#endif

/* Static function prototypes *************************************************/

static SHMPTR SHMInitPool(SHMPTR first, int block_size, int pool_size,
                          SHM_POOL_INFO *pool);
static SHMPTR SHMLinkPool(SHMPTR first, int block_size, int num_blocks);
#ifndef CORECLR
static LPVOID SHMMapSegment(char *segment_name);
#endif // !CORECLR
static BOOL   SHMMapUnknownSegments(void);
static BOOL   SHMAddSegment(void);
#ifndef CORECLR
static BOOL   SHMInitSegmentFileSize(int fd);
static int    SHMGetProcessList(int fd, pid_t **process_list, BOOL strip_me);
#endif // !CORECLR

#if !defined(CORECLR) && defined(_DEBUG)

static void init_waste(void);
static void log_waste(enum SHM_POOL_SIZES size, int waste);
static void save_waste(void);

#else                 

#define init_waste()
#define log_waste(x,y)
#define save_waste()

#endif

/* Public function implementations ********************************************/

/*++
SHMInitialize

Hook this process into the PAL shared memory system; initialize the shared
memory if no other process has done it.

--*/
BOOL SHMInitialize(void)
{
#ifndef CORECLR
    pid_t *pal_processes;
    pid_t this_process;
    int n_pal_processes = 0;
    int fd_lock;
    int fd_map;
    int i;
    int j;
    CHAR config_dir[MAX_LONGPATH];
    CHAR first_segment_name[MAX_LONGPATH];
    ssize_t sBytes;
#endif // !CORECLR

    InternalInitializeCriticalSection(&shm_critsec);

    init_waste();
    
#ifndef CORECLR
    if ( PALGetPalConfigDir( config_dir, MAX_LONGPATH ) )
    {
        if ( ( strlen( config_dir ) + strlen( segment_name_prefix ) + 
                                           /* + 3 for the / _ and \0 */
               strlen( first_segment_suffix ) + 3 ) < MAX_LONGPATH && 
             
             ( strlen( config_dir ) + strlen( segment_name_prefix ) + 
               SEGMENT_NAME_SUFFIX_LENGTH + 3 ) < MAX_LONGPATH ) 
        {
            /* build first segment's file name */
            sprintf( first_segment_name,"%s/%s_%s", config_dir,
                     segment_name_prefix, first_segment_suffix );

            /* Initialize mkstemp() template */
            j = sprintf(segment_name_template, "%s/%s_", 
                        config_dir, segment_name_prefix);
            
            for ( i = 0; i < SEGMENT_NAME_SUFFIX_LENGTH ; i++ )
            {
                segment_name_template[i+j] = 'X';
            }
            segment_name_template[i+j] = '\0';
        }
        else
        {
            ASSERT( "Configuration directory length + segment name length "
                   "is greater then MAX_LONGPATH.\n" );
            return FALSE;
        }
    }
    else
    {
        ASSERT( "Unable to determine the PAL config directory.\n" );
        return FALSE;
    }

    /* Once we have access to shared memory, we can use our spinlock for
       synchronization. But we also have to be synchronized while we *gain*
       access, to prevent 2 processes from trying to initialize the shared
       memory system at the same time. We use a separate lockfile for this
       purpose; this lockfile will also be used to keep track of which
       processes are using shared memory */

    /* build the lockfile path name */
    sprintf(lockfile_name, "%s/%s", config_dir, lockfile_shortname);

#ifdef O_EXLOCK
    fd_lock = PAL__open(lockfile_name, O_RDWR|O_CREAT|O_EXLOCK, 0600);
#else   // O_EXLOCK
    fd_lock = PAL__open(lockfile_name, O_RDWR|O_CREAT, 0600);
#endif  // O_EXLOCK
    if(fd_lock == -1)
    {
        int retry_count = 0;

        while( fd_lock == -1 && ENOENT == errno && 10 > retry_count)
        {
            /* directory didn't exist! this means a shutting down PAL 
               deleted it between our call to INIT_InitPalConfigDir and 
               here. Try to re-create it a few times, but give up if it 
               doesn't seem to be working : there's probably something 
               else wrong */
            WARN("PAL temp dir has disappeared; trying to recreate\n");
            if(!INIT_InitPalConfigDir())
            {
                ERROR("couldn't recreate PAL temp dir!\n");
                return FALSE;
            }
#ifdef O_EXLOCK
            fd_lock = PAL__open(lockfile_name,O_RDWR|O_CREAT|O_EXLOCK, 0600);
#else   // O_EXLOCK
            fd_lock = PAL__open(lockfile_name,O_RDWR|O_CREAT, 0600);
#endif  // O_EXLOCK
            retry_count++;
        }
        if(-1 == fd_lock)
        {
            ERROR("PAL__open() failed; error is %d (%s)\n", errno, strerror(errno));
            return FALSE;
        }
    }
#ifndef O_EXLOCK
    if (lockf(fd_lock, F_LOCK, 0) == -1)
    {
        ERROR("lockf() failed; error is %d (%s)\n", errno, strerror(errno));
        close(fd_lock);
        return FALSE;
    }
#endif  // O_EXLOCK

    TRACE("SHM lock file acquired\n");

    /* got the lock; now check if any other processes are already using shared
       memory, or if we must initialize it. The first DWORD of the lockfile
       contains the number of registered PAL processes */
    n_pal_processes = SHMGetProcessList(fd_lock, &pal_processes, FALSE);
    if( -1 == n_pal_processes )
    {
        ERROR("Unable to read process list from lock file!\n");
#ifdef O_EXLOCK
        flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
        lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
        close(fd_lock);
        return FALSE;
    }

    /* If there are no registered PAL-aware processes, we can initialize
       (or re-initialize) the shared memory system */
    if(n_pal_processes == 0)
    {
#endif // !CORECLR
        int size;
        SHM_FIRST_HEADER *header;
        SHMPTR pool_start;
        SHMPTR pool_end;
        enum SHM_POOL_SIZES sps;

        TRACE("Now initializing global shared memory system\n");
        
#ifndef CORECLR
        /* open the file, create if necessary (should be necessary)*/
        fd_map = PAL__open(first_segment_name,O_RDWR|O_CREAT,0600);
        if(fd_map == -1)
        {
            ERROR("PAL__open() failed; error is %d (%s)\n", errno, strerror(errno));
            PAL_free(pal_processes);
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            return FALSE;
        }

        /* Grow file to segment size (256KB) */
        if(!SHMInitSegmentFileSize(fd_map))
        {
            ERROR("SHMInitSegmentFileSize() failed; error is %d (%s)\n",
                  errno, strerror(errno));
            PAL_free(pal_processes);
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            close(fd_map);
            return FALSE;
        }

        shm_segment_bases[0] = mmap(NULL, segment_size,PROT_READ|PROT_WRITE,
                                    MAPFLAGS, fd_map, 0);

        close(fd_map);
#else // !CORECLR
        // Not really shared in CoreCLR; we don't try to talk to other CoreCLRs.
        shm_segment_bases[0] = mmap(NULL, segment_size,PROT_READ|PROT_WRITE,
                                    MAP_ANON|MAP_PRIVATE, -1, 0);
#endif // !CORECLR
        if(shm_segment_bases[0] == MAP_FAILED)
        {
            ERROR("mmap() failed; error is %d (%s)\n", errno, strerror(errno));
#ifndef CORECLR
            PAL_free(pal_processes);
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
#endif // !CORECLR
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

#ifndef CORECLR
        header->header.next_segment[0] = '\0'; /* no next segment */
#endif // !CORECLR

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
#ifndef CORECLR
                PAL_free(pal_processes);
#ifdef O_EXLOCK
                flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
                lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
                close(fd_lock);
#endif // !CORECLR
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
#ifndef CORECLR
    }
    else
    {
        struct stat sb;

        TRACE("Shared memory already initialized : mapping first segment\n")

        fd_map = PAL__open(first_segment_name, O_RDWR);
        if(fd_map == -1)
        {
            ERROR("PAL__open() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            PAL_free(pal_processes);
            return FALSE;
        }

        /* find out the segment's size (currently always 256KB, but this may
           change and even become variable) */
        if( -1 == fstat(fd_map,&sb) )
        {
            ERROR("fstat() failed! error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            close(fd_map);
            PAL_free(pal_processes);
            return FALSE;
        }

        shm_segment_bases[0] = mmap(NULL, sb.st_size,PROT_READ|PROT_WRITE,
                                    MAPFLAGS, fd_map, 0);
        close(fd_map);

        if(shm_segment_bases[0] == MAP_FAILED)
        {
            ERROR("mmap() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            PAL_free(pal_processes);
            return FALSE;
        }
        TRACE("Successfully accessed first shared memory segment\n");
    }

    /* Re-write lockfile with updated process list */

    /* empty the file */
    ftruncate(fd_lock,0);
    lseek(fd_lock, 0, SEEK_SET);

    n_pal_processes++;
    sBytes = write(fd_lock, &n_pal_processes, sizeof(n_pal_processes));
    if(sBytes == -1)
    {
        ERROR("write() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
        flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
        lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
        close(fd_lock);
        PAL_free(pal_processes);
        return FALSE;
    }
    /* put back all processes that are still alive */
    if(n_pal_processes>1)
    {
        sBytes = write(fd_lock, pal_processes, (n_pal_processes-1)*sizeof(pid_t));
        if(sBytes == -1)
        {
            ERROR("write() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            PAL_free(pal_processes);
            return FALSE;
        }
    }
    PAL_free(pal_processes);

    /* add this process to the list */
    this_process = gPID;
    sBytes = write(fd_lock, &this_process, sizeof(pid_t));
    if(sBytes == -1)
    {
        ERROR("write() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
        flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
        lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
        close(fd_lock);
        return FALSE;
    }

    /* We're done with the lock file : release it. */
#ifdef O_EXLOCK
    flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
    lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
    close(fd_lock);

    TRACE("Lock file released; ready to map all shared memory segments\n");
#endif // !CORECLR

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
#ifndef CORECLR
    int fd_lock;
    int n_pal_processes;
    pid_t *pal_processes;
    ssize_t sBytes;
#endif // !CORECLR

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

#ifndef CORECLR
    TRACE("Segment unmapping complete; now unregistering this process\n");

    /* Unregister this process by removing its PID from the lock file. By
       locking the lock file here, we ensure that any ongoing SHMInitialize call
       will complete before we do anything here, and no subsequent call to
       SHMInitialize can proceed until we're done. */
#ifdef O_EXLOCK
    fd_lock = PAL__open(lockfile_name,O_RDWR | O_EXLOCK);
#else   // O_EXLOCK
    fd_lock = PAL__open(lockfile_name,O_RDWR);
#endif  // O_EXLOCK
    if(fd_lock == -1)
    {
        ERROR("PAL__open() failed; error is %d (%s)\n", errno, strerror(errno));
        return;
    }
#ifndef O_EXLOCK
    if (lockf(fd_lock, F_LOCK, 0) == -1)
    {
        ERROR("lockf() failed; error is %d (%s)\n", errno, strerror(errno));
        close(fd_lock);
        return;
    }
#endif  // O_EXLOCK

    TRACE("Lockfile acquired\n");

    /* got the lock; now remove this proces from the list, as well as any
       dead process that didn't clean up after itself */
    n_pal_processes = SHMGetProcessList(fd_lock, &pal_processes, TRUE);
    if( -1 == n_pal_processes )
    {
        ERROR("Unable to read process list from lock file!\n");
#ifdef O_EXLOCK
        flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
        lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
        close(fd_lock);
        return;
    }

    /* re-write lockfile with updated process list */
    lseek(fd_lock, 0, SEEK_SET);
    ftruncate(fd_lock, 0);

    sBytes = write(fd_lock, &n_pal_processes, sizeof(n_pal_processes));
    if(sBytes == -1)
    {
        ERROR("write() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
        flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
        lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
        close(fd_lock);
        return;
    }

    _ASSERT_MSG(0 < n_pal_processes || 0 == header->spinlock,
        "SHMCleanup called while process %u still owns the lock [current process=%u\n",
        header->spinlock.Load(), getpid());
#endif // !CORECLR

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

#ifndef CORECLR
    if(n_pal_processes>0)
    {        
        sBytes = write(fd_lock, pal_processes, n_pal_processes*sizeof(pid_t));
        if(sBytes == -1)
        {
            ERROR("write() failed; error is %d (%s)\n", errno, strerror(errno));
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            return;
        }
    }
    else /* Clean up everything. */
    {
        CHAR PalDir[ MAX_LONGPATH + 1 ];
        CHAR FileName[ MAX_LONGPATH + 1 ];
        UINT nEndOfPathAndPrefix = 0;
        SHM_SEGMENT_HEADER segment_header;
        LPCSTR suffix;
        int fd_segment;

        /* Build start of filename. */
        if ( !PALGetPalConfigDir( PalDir, MAX_LONGPATH ) )
        {
            ASSERT( "Unable to determine the PAL config directory.\n" );
#ifdef O_EXLOCK
            flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
            lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
            close(fd_lock);
            return;
        }
        sprintf( FileName, "%s/%s_", PalDir, segment_name_prefix );
        nEndOfPathAndPrefix = strlen( FileName );

        /* Unlink all segment files */
        /* Note: we can't rely on all the mapped segment, since it's
           possible that another process created another segment that 
           the current process is not aware. So the solution is to open
           the segment files and look at the next one in a loop */
        suffix = first_segment_suffix;

        while ( strlen(suffix) > 0 )
        {
            if (strcat_s( FileName, sizeof(FileName), suffix) != SAFECRT_SUCCESS)
            {
                ERROR("strcat_s failed!");
                break;
            }

            fd_segment = PAL__open( FileName, O_RDONLY );
            if( fd_segment == -1 )
            {
                WARN("Unable to PAL__open shared memory segment file: %s "
                     "errno is %d (%s)\n", 
                     FileName, errno, strerror( errno ) );                    
                break;
            }
           
            if ( read(fd_segment, &segment_header, sizeof(SHM_SEGMENT_HEADER)) !=
                 sizeof(SHM_SEGMENT_HEADER) )
            {
                WARN("Unable to read shared memory segment file: %s " 
                     "errno is %d (%s)\n", 
                     FileName, errno, strerror( errno ));
                close(fd_segment);
                break;
            }

            close(fd_segment);
            if (unlink(FileName) == -1)
            {
                WARN("Unable to unlink the shared memory segment file: %s " 
                     "errno is %d (%s)\n", 
                     FileName, errno, strerror( errno ));
            }
            suffix =  segment_header.next_segment;
            FileName[ nEndOfPathAndPrefix ] = '\0';
        }

        if ( -1 == unlink(lockfile_name) )
        {
            WARN( "Unable to unlink the file! Reason=(%d)%s\n",
                  errno, strerror( errno ) );
        }
        
        /* try to remove the PAL's temp directory. this will fail if there are 
           still files in them; don't insist if that happens */
        INIT_RemovePalConfigDir();
    }
    free(pal_processes);

#ifdef O_EXLOCK
    flock(fd_lock, LOCK_UN);
#else   // O_EXLOCK
    lockf(fd_lock, F_ULOCK, 0);
#endif  // O_EXLOCK
    close(fd_lock);
#endif // !CORECLR

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

#ifdef _HPUX_
            //
            // TODO: workaround for VSW # 381564
            // 
            if (0 == tmp_pid && my_pid != header->spinlock)
            {
                ERROR("InterlockedCompareExchange returned the Comperand but "
                      "failed to store the Exchange value to the Destination: "
                      "looping again [my_pid=%u header->spinlock=%u tmp_pid=%u "
                      "spincount=%d locking_thread=%u]\n", (DWORD)my_pid, 
                      (DWORD)header->spinlock, (DWORD)tmp_pid, (int)spincount, 
                      (HANDLE)locking_thread);

                // Keep looping
                tmp_pid = 42;
            }
#endif // _HPUX_

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


#ifdef _HPUX_
        //
        // TODO: workaround for VSW # 381564 
        //
        do
#endif // _HPUX_
        {
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
        }
#ifdef _HPUX_
        //
        // TODO: workaround for VSW # 381564
        //
        while (my_pid == header->spinlock);
#endif // _HPUX_

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

#ifndef CORECLR
/*++
SHMMapSegment

Map the specified SHM segment file into the current process

Parameters :
    char *segment_name : suffix of the segment's file name, as determined by
    mkstemp()

Return value :
    Address where the file was mapped
--*/
static LPVOID SHMMapSegment(char *segment_name)
{
    char segment_path[MAX_LONGPATH];
    int segment_fd;
    LPVOID *segment;
    struct stat sb;

    /* Construct the file's full path */
    if ( !PALGetPalConfigDir( segment_path, MAX_LONGPATH ) )
    {
        ASSERT( "Unable to determine the PAL config directory.\n" );
        return NULL;
    }
    sprintf_s(segment_path+strlen(segment_path), sizeof(segment_path)-strlen(segment_path), "/%s_%s",
            segment_name_prefix, segment_name);

    TRACE("Mapping SHM file %s into process...\n", segment_path);
    segment_fd = PAL__open(segment_path,O_RDWR);
    if(-1 == segment_fd)
    {
        ERROR("PAL__open failed! error is %d (%s)\n", errno, strerror(errno));
        return NULL;
    }

    /* stat the file to determine its size */
    if( -1 == fstat(segment_fd, &sb) )
    {
        ERROR("fstat() failed! error is %d (%s)\n", errno, strerror(errno));
        close(segment_fd);
        return NULL;
    }

    /* mmap() the file; use MAP_NOSYNC to avoid unnecessary file I/O */
    segment = static_cast<LPVOID *>(
        mmap(NULL, sb.st_size,PROT_READ|PROT_WRITE, MAPFLAGS, segment_fd, 0));
    close(segment_fd);
    if(segment == MAP_FAILED)
    {
        ERROR("mmap() failed; error is %d (%s)\n", errno, strerror(errno));
        return NULL;
    }

    TRACE("SHM file %s successfully mapped at address %p\n",
          segment_name, segment);

    return segment;
}
#endif // !CORECLR

/*++
SHMMapUnknownSegments

Map into this process all SHM segments not yet mapped

(no parameters)

Return value :
    TRUE on success, FALSE in case of error
--*/
static BOOL SHMMapUnknownSegments(void)
{
#ifndef CORECLR
    SHM_SEGMENT_HEADER *header;
    int num_new = 0;
    BOOL retval = FALSE;

    TRACE("Mapping unknown segments into this process...\n");

    SHMLock();

    /* Get header of last known segment */
    header = (SHM_SEGMENT_HEADER *) shm_segment_bases[shm_numsegments-1].Load();

    /* header->next_segment is the suffix of the next segment's file name
       (from mkstemp). We use it to map the next segment, then check it to get
       the name of the next one, etc. */
    while('\0' != header->next_segment[0])
    {
        /* If segment array is full, enlarge it */
        if(shm_numsegments == MAX_SEGMENTS)
        {
            ERROR("Can't map more segments : maximum number (%d) reached!\n",
                  MAX_SEGMENTS);
            goto done;
        }
        shm_segment_bases[shm_numsegments] =
                    SHMMapSegment(header->next_segment);
        if(!shm_segment_bases[shm_numsegments])
        {
            ERROR("Failed to map next shared memory segment!\n");
            goto done;
        }

        /* Get header of new segment to see if there are others after it */
        header = (SHM_SEGMENT_HEADER *)
                        shm_segment_bases[shm_numsegments].Load();
        shm_numsegments++;
        num_new++;
    }
    retval = TRUE;
done:
    SHMRelease();
    TRACE("Mapped %d new segments (total is now %d)\n",
          num_new, shm_numsegments);

    return retval;
#else // !CORECLR
    return TRUE;
#endif // !CORECLR
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
#ifndef CORECLR
    char segment_name[MAX_LONGPATH];
    char *suffix_start;
    int fd_map;
#endif // !CORECLR
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

#ifndef CORECLR
    /* Create name template for mkstemp() */
    strcpy(segment_name, segment_name_template);

    suffix_start = segment_name + strlen(segment_name) -
                   SEGMENT_NAME_SUFFIX_LENGTH;

    fd_map = PAL_mkstemp(segment_name);
    if(fd_map == -1)
    {
        ERROR("mkstemp() failed! error is %d (%s)\n", errno, strerror(errno));
        return FALSE;
    }
    if(-1 == fchmod(fd_map, 0600))
    {
        ERROR("fchmod() failed! error is %d (%s)\n", errno, strerror(errno));
        close(fd_map);
        PAL_unlink(segment_name);
        return FALSE;
    }

    /* Grow new file to desired size */
    if(!SHMInitSegmentFileSize(fd_map))
    {
        ERROR("SHMInitSegmentFileSize() failed! error is %d (%s)\n", 
              errno, strerror(errno));
        close(fd_map);
        PAL_unlink(segment_name);
        return FALSE;
    }

    /* mmap() the new file */
    segment_base = mmap(NULL, segment_size, PROT_READ|PROT_WRITE,
                        MAPFLAGS,fd_map, 0);
    close(fd_map);
#else // !CORECLR
    segment_base = mmap(NULL, segment_size, PROT_READ|PROT_WRITE,
                        MAP_ANON|MAP_PRIVATE,-1, 0);
#endif // !CORECLR

    if(segment_base == MAP_FAILED)
    {
        ERROR("mmap() failed! error is %d (%s)\n", errno, strerror(errno));
#ifndef CORECLR
        PAL_unlink(segment_name);
#endif // !CORECLR
        return FALSE;
    }

#ifndef CORECLR
    TRACE("Mapped SHM segment #%d at %p; name is %s\n",
          shm_numsegments, segment_base, suffix_start);
#endif // !CORECLR

    shm_segment_bases[shm_numsegments] = segment_base;

    /* Save name (well, suffix) of new segment in the header of the old last
       segment, so that other processes know where it is. */
    header = (SHM_SEGMENT_HEADER *)shm_segment_bases[shm_numsegments-1].Load();
#ifndef CORECLR
    strcpy(header->next_segment, suffix_start);
#endif // !CORECLR

    /* Indicate that the new segment is the last one */
    header = (SHM_SEGMENT_HEADER *)segment_base;
#ifndef CORECLR
    header->next_segment[0] = '\0';
#endif // !CORECLR

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

#ifndef CORECLR
/*++
SHMInitSegmentFileSize

Grow a file to the size of a SHM segment

Parameters :
    int fd : File descriptor of file to initialize

(no return value)

--*/
static BOOL SHMInitSegmentFileSize(int fd)
{
    /* Make sure the file starts out empty */
    if(ftruncate(fd, 0)==-1)
    {
      ERROR("ftruncate failed; error is %d (%s)\n", errno, strerror(errno));
      return FALSE;
    }

    if(lseek(fd, 0, SEEK_SET)==-1)
    {
      ERROR("lseek failed; error is %d (%s)\n", errno, strerror(errno));
      return FALSE;
    }

    /* grow the file size, by lseek. See lseek manual. */ 
    if(lseek(fd, segment_size, SEEK_SET)==-1)
    {
      ERROR("lseek failed; error is %d (%s)\n", errno, strerror(errno));
      return FALSE;
    }
    if(ftruncate(fd, segment_size)==-1)
    {
      ERROR("ftruncate failed; error is %d (%s)\n", errno, strerror(errno));
      return FALSE;
    }

    return TRUE;
}


/*++
SHMGetProcessList

Read the list of registered PAL processes from the lockfile, stripping dead
processes along the way.

Parameters :
    int fd : file to read from
    pid_t **process_list : pointer where process list will be placed
    BOOL strip_me : if TRUE, the PID of the current process will be removed

Return value :
    -1 on failure
    Number of PIDs in the list on success

--*/
static int SHMGetProcessList(int fd, pid_t **process_list, BOOL strip_me)
{
    int n_pal_processes;
    pid_t *list;
    pid_t me;

    me = gPID;

    if( 0 < read(fd, &n_pal_processes, sizeof(n_pal_processes))
        && n_pal_processes > 0)
    {
        /* read succesful, there are registered processes */
        int i,n;

        TRACE("LockFile says there are %d registered PAL processes.\n",
              n_pal_processes);

        list = (pid_t *)PAL_malloc(n_pal_processes*sizeof(pid_t));
        if(!list)
        {
            ERROR("malloc() failed; error is %d (%s)\n",
                  errno, strerror(errno));
            return -1;
        }
        i = n = 0;

        /* read PIDs of registered processes into an array. We use this chance
           to discard processes that died without unregistering themselves.*/
        for( i = 0; i< n_pal_processes; i++)
        {
            /* read 1 PID from lock file */

            if(0 >= read(fd, &list[n], sizeof(pid_t)))
            {
                WARN("read() failed! error is %d (%s)\n",
                     errno, strerror(errno));
                break;
            }

            /* Discard the PID of the currentprocess if required, and make sure
               the PID is still valid. If invalid, kill(pid,0) fails and errno
               is set to ESRCH */
            if(strip_me && list[n] == me)
            {
                TRACE("Removing current process (%08x) from list of registered "
                      "processes\n", list[n]);
            }
            else if(-1 != kill(list[n],0) || errno != ESRCH)
            {
                /* good PID : keep it*/
                n++;
            }
            else
            {
                TRACE("Discarding registered process 0x%08x : it's dead\n",
                      list[n]);
            }
        }
        n_pal_processes = n;
        if(n == 0)
        {
            TRACE("There are no registered PAL processes left.\n");
            PAL_free(list);
            list = NULL;
        }
        else
        {
            TRACE("Number of registered PAL processes : %d\n", n_pal_processes);
        }
    }
    else
    {
        /* read failed or number of processes was 0 : no registered processes */
        n_pal_processes = 0;
        list = NULL;
        TRACE("No PAL processes registered for shared memory access\n");
    }

    *process_list = list;
    return n_pal_processes;
}
#endif // !CORECLR


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

#if !defined(CORECLR) && defined(_DEBUG)

static DWORD alloc_log[SPS_LAST];
static DWORD waste_log[SPS_LAST];

/*++
init_waste

Initialize emmory waste logging arays (set all to zero)

(no parameters, no return value)
--*/
static void init_waste(void)
{
    enum SHM_POOL_SIZES sps;

    for (sps = static_cast<SHM_POOL_SIZES>(0); sps < SPS_LAST;
         sps = static_cast<SHM_POOL_SIZES>(sps + 1))
    {
        alloc_log[sps] = 0;
        waste_log[sps] = 0;
    }
}

/*++
log_waste

add waste information from 1 allocation to the log

Parameters :
    enum SHM_POOL_SIZES size : pool size selected for allocation
    int waste : number of bytes wasted by allocation
    
(no return value)

   Note: this could be a lot fancier : we could remember every single size 
   ever requested, and how often it was requested. do we need that much? 
   we could also keep a cumulative, system-wide total... ? 
--*/
static void log_waste(enum SHM_POOL_SIZES size, int waste)
{
    /* avoid overflowing*/
    if( ((DWORD)(~0) != alloc_log[size]) && 
      ((DWORD)(~0)-(DWORD)waste > waste_log[size]))
    {
        alloc_log[size]++;
        waste_log[size]+=waste;
    }
    else
    {
        WARN("can't track shared memory usage : overflowing\n");
    }
}

/*++
save_waste

output waste information to file (during process termination)

(no parameters, no return value)
--*/
static void save_waste(void)
{
    int fd;
    FILE *log_file;
    char file_name[MAX_LONGPATH];
    enum SHM_POOL_SIZES sps;
    int avg;
    LPSTR env_string;
 
    env_string = MiscGetenv(PAL_SAVE_WASTE);
    
    if(!env_string || env_string[0] =='\0' || env_string[0]=='0')
    {
        return;
    }
    
    if ( !PALGetPalConfigDir( file_name, MAX_LONGPATH ) )
    {
        ERROR( "Unable to determine the PAL config directory.\n" );
        return;
    }
    
    if (strcat_s(file_name, sizeof(file_name), "/shm_waste_log") != SAFECRT_SUCCESS)
    {
        ERROR( "strcat_s failed!\n" );
        return;
    }

    // Get an exclusive lock on the file -- we don't want 
    // interleaving between the output of multiple processes.
#ifdef O_EXLOCK
    fd = PAL__open(file_name, O_APPEND|O_WRONLY|O_CREAT|O_EXLOCK, 0600);
#else   // O_EXLOCK
    fd = PAL__open(file_name, O_APPEND|O_WRONLY|O_CREAT, 0600);
#endif  // O_EXLOCK
    if(-1 == fd)
    {
        WARN("Unable to open log file %s : not logging shared memory waste. "
             "PAL__open() failed with errno %d (%s)\n",
             file_name, errno, strerror(errno));
        return;
    }
#ifndef O_EXLOCK
    if (lockf(fd, F_LOCK, 0) == -1)
    {
        WARN("Unable to lock log file %s. Not logging shared memory waste."
             "lockf() failed with errno %d (%s)\n",
             file_name, errno, strerror(errno));
        close(fd);
        return;
    }
#endif  // O_EXLOCK

    /* open in append mode, so we preserve the output of other processes */
    log_file = fdopen(fd, "at");
    if( NULL == log_file)
    {
        WARN("Unable to open log file : not logging shared memory waste\n");
#ifdef O_EXLOCK
        flock(fd, LOCK_UN);
#else   // O_EXLOCK
        lockf(fd, F_ULOCK, 0);
#endif  // O_EXLOCK
        close(fd);
        return;
    }
    TRACE("now writing shared memory waste log to file %s\n", file_name);
    fprintf(log_file, "Shared memory waste for process %ld:\n", (long) gPID);
    
    /* output one line per block size */
    for (sps = static_cast<SHM_POOL_SIZES>(0); sps < SPS_LAST;
         sps = static_cast<SHM_POOL_SIZES>(sps + 1))
    {
        if(0 == alloc_log[sps])
        {
            avg = 0;
        }
        else 
        {
            avg = waste_log[sps]/alloc_log[sps];
        }

        fprintf(log_file, "size %4d : %8u bytes wasted in %8u blocks "
                "(avg. %8d bytes/block)\n", 
                block_sizes[sps], waste_log[sps], alloc_log[sps], avg );
    }
    fprintf(log_file, "\n");
            
#ifdef O_EXLOCK
    flock(fd, LOCK_UN);
#else   // O_EXLOCK
    lockf(fd, F_ULOCK, 0);
#endif  // O_EXLOCK
    fclose(log_file);
}

#endif  // !defined(CORECLR) && defined(_DEBUG)
