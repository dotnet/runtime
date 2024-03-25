# Thread Safety/Synchronization

Thread safety of metadata structures
------------------------------------

### Synchronization of read-only data

Read-only data is data which is not modified after creation, like the actual binary metadata in the metadata tables.

There are three kinds of threads with regards to read-only data:

-   readers
-   the creator of the data
-   the destroyer of the data

Most threads are readers.

-   synchronization between readers is not necessary
-   synchronization between the writers is done using locks.
-   synchronization between the readers and the creator is done by not exposing the data to readers before it is fully constructed.
-   synchronization between the readers and the destroyer: TBD.

### Deadlock prevention plan

Hold locks for the shortest time possible. Avoid calling functions inside locks which might obtain global locks (i.e. locks known outside this module).

### Locks

#### Simple locks

There are a lot of global data structures which can be protected by a 'simple' lock. Simple means:

-   the lock protects only this data structure or it only protects the data structures in a given C module. An example would be the appdomains list in domain.c
-   the lock can span many modules, but it still protects access to a single resource or set of resources. An example would be the image lock, which protects all data structures that belong to a given MonoImage.
-   the lock is only held for a short amount of time, and no other lock is acquired inside this simple lock. Thus there is no possibility of deadlock.

Simple locks include, at least, the following :

-   the per-image lock acquired by using mono_image_(un)lock functions.
-   the threads lock acquired by using mono_threads_(un)lock.

#### The loader lock

This locks is held by class loading routines and any global synchronization routines. This is effectively the runtime global lock. Other locks can call code that acquire the loader lock out of order if the current thread already owns it.

#### The domain lock

Each appdomain has a lock which protects the per-domain data structures.

#### The domain jit code hash lock

This per-domain lock protects the JIT'ed code of each domain. Originally we used the domain lock, but it was split to reduce contention.

#### Allocation locks and foreign locks

Mono features a few memory allocation subsystems such as: a lock-free allocator, the GC. Those subsystems are designed so they don't rely on any of the other subsystems in the runtime. This ensures that locking within them is transparent to the rest of the runtime and are not covered here. It's the same rule  when dealing with locking that happens within libc.

### The locking hierarchy

It is useful to model locks by a locking hierarchy, which is a relation between locks, which is reflexive, transitive, and antisymmetric, in other words, a lattice. If a thread wants to acquire a lock B, while already holding A, it can only do it if A \< B. If all threads work this way, then no deadlocks can occur.

Our locking hierarchy so far looks like this (if lock A is above lock B, then A \< B):

        <LOADER LOCK>
            \
           <DOMAIN LOCK>
            \                 \                  \
           <DOMAIN JIT LOCK> <SIMPLE LOCK 1>    <SIMPLE LOCK 2>

For example: if a thread wants to hold a domain jit lock, a domain lock and the loader lock, it must acquire them in the order: loader lock, domain lock, domain jit lock.

### Notes

Some common scenarios:

-   if a function needs to access a data structure, then it should lock it itself, and do not count on its caller locking it. So for example, the image-\>class_cache hash table would be locked by mono_class_get().

-   there are lots of places where a runtime data structure is created and stored in a cache. In these places, care must be taken to avoid multiple threads creating the same runtime structure, for example, two threads might call mono_class_get () with the same class name. There are two choices here:

<!-- -->

        <enter mutex>
        <check that item is created>
        if (created) {
            <leave mutex>
            return item
        }
        <create item>
        <store it in cache>
        <leave mutex>

This is the easiest solution, but it requires holding the lock for the whole time which might create a scalability problem, and could also lead to deadlock.

        <enter mutex>
        <check that item is created>
        <leave mutex>
        if (created) {
            return item
        }
        <create item>
        <enter mutex>
        <check that item is created>
        if (created) {
            /* Another thread already created and stored the same item */
            <free our item>
            <leave mutex>
            return orig item
        }
        else {
            <store item in cache>
            <leave mutex>
            return item
        }

This solution does not present scalability problems, but the created item might be hard to destroy (like a MonoClass). If memory is allocated from a mempool, that memory is leaked, but the leak is very rare and it is bounded.

-   lazy initialization of hashtables etc. is not thread safe

[Original version of this document in git](https://github.com/mono/mono/blob/8f91e420d7fbbab7da758e57160d1d762129f38a/docs/thread-safety.txt)

### The Lock Tracer

Mono now have a lock tracer that allows to record the locking behavior of the runtime during execution and later verify it's correctness.

To enable lock tracer support define LOCK_TRACER in mono/mono/metadata/lock-tracer.h and recompile mono. To enable it at runtime define the MONO_ENABLE_LOCK_TRACER environment variable.

The lock tracer produces a file in the same directory of the application, it's named 'lock.ZZZ' where ZZZ is the pid of the mono process.

After producing such lock file, run the trace decoder that can be found in mono/data/lock-decoder. It currently only works on linux and macOS, it requires binutils to be installed. The decoder will report locking errors specifying the functions that caused it.
