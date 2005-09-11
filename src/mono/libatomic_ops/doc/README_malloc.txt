The libatomic_ops_gpl includes a simple almost-lock-free malloc implementation.

This is intended as a safe way to allocate memory from a signal handler,
or to allocate memory in the context of a library that does not know what
thread library it will be used with.  In either case locking is impossible.

Note that the operations are only guaranteed to be 1-lock-free, i.e. a
single blocked thread will not prevent progress, but multiple blocked
threads may.  To safely use these operations in a signal handler,
the handler should be non-reentrant, i.e. it should not be interruptable
by another handler using these operations.  Furthermore use outside
of signal handlers in a multithreaded application should be protected
by a lock, so that at most one invocation may be interrupted by a signal.
The header will define the macro "AO_MALLOC_IS_LOCK_FREE" on platforms
on which malloc is completely lock-free, and hence these restrictions
do not apply.

In the presence of threads, but absence of contention, the time performance
of this package should be as good, or slightly better than, most system
malloc implementations.  Its space performance
is theoretically optimal (to within a constant factor), but probably
quite poor in practice.  In particular, no attempt is made to
coalesce free small memory blocks.  Something like Doug Lea's malloc is
likely to use significantly less memory for complex applications.

Perfomance on platforms without an efficient compare-and-swap implementation
will be poor.

This package was not designed for processor-scalability in the face of
high allocation rates.  If all threads happen to allocate different-sized
objects, you might get lucky.  Otherwise expect contention and false-sharing
problems.  If this is an issue, something like Maged Michael's algorithm
(PLDI 2004) would be technically a far better choice.  If you are concerned
only with scalablity, and not signal-safety, you might also consider
using Hoard instead.  We have seen a factor of 3 to 4 slowdown from the
standard glibc malloc implementation with contention, even when the
performance without contention was faster.  (To make the implementation
more scalable, one would need to replicate at least the free list headers,
so that concurrent access is possible without cache conflicts.)

Unfortunately there is no portable async-signal-safe way to obtain large
chunks of memory from the OS.  Based on reading of the source code,
mmap-based allocation appears safe under Linux, and probably BSD variants.
It is probably unsafe for operating systems built on Mach, such as
Apple's Darwin.  Without use of mmap, the allocator is
limited to a fixed size, statically preallocated heap (2MB by default),
and will fail to allocate objects above a certain size (just under 64K
by default).  Use of mmap to circumvent these limitations requires an
explicit call.

The entire interface to the AO_malloc package currently consists of:

#include <atomic_ops_malloc.h> /* includes atomic_ops.h */

void *AO_malloc(size_t sz);
void AO_free(void *p);
void AO_malloc_enable_mmap(void);
