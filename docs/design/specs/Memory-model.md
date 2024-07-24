
# .NET memory model

## ECMA 335 vs. .NET memory models
ECMA 335 standard defines a very weak memory model. After two decades the desire to have a flexible model did not result in considerable benefits due to hardware being more strict. On the other hand programming against ECMA model requires extra complexity to handle scenarios that are hard to comprehend and not possible to test.

In the course of multiple releases .NET runtime implementations settled around a memory model that is a practical compromise between what can be implemented efficiently on the current hardware, while staying reasonably approachable by the developers. This document rationalizes the invariants provided and expected by the .NET runtimes in their current implementation with expectation of that being carried to future releases.

## Alignment
When managed by the .NET runtime, variables of built-in primitive types are *properly aligned* according to the data type size. This applies to both heap and stack allocated memory.

1-byte, 2-byte, 4-byte variables are stored at 1-byte, 2-byte, 4-byte boundary, respectively.
8-byte variables are 8-byte aligned on 64 bit platforms.
Native-sized integer types and pointers have alignment that matches their size on the given platform.

The alignment of fields is not guaranteed when `FieldOffsetAttribute` is used to explicitly adjust field offsets.

## Atomic memory accesses
Memory accesses to *properly aligned* data of primitive and Enum types with size with sizes up to the platform pointer size are always atomic. The value that is observed is always a result of complete read and write operations.

Primitive types: bool, char, int8, uint8, int16, uint16, int32, uint32, int64, uint64, float32, float64, native int, native unsigned int.

Values of unmanaged pointers are treated as native integer primitive types. Memory accesses to *properly aligned* values of unmanaged pointers are atomic.

Managed references are always aligned to their size on the given platform and accesses are atomic.

The following methods perform atomic memory accesses regardless of the platform when the location of the variable is managed by the runtime.
- `System.Threading.Interlocked` methods
- `System.Threading.Volatile` methods

**Example:** `Volatile.Read<double>(ref location)` on a 32 bit platform is atomic, while an ordinary read of `location` may not be.

## Unmanaged memory access
As unmanaged pointers can point to any addressable memory, operations with such pointers may violate guarantees provided by the runtime and expose undefined or platform-specific behavior.
**Example:** memory accesses through pointers whose target address is *not properly aligned* to the data access size may be not atomic or cause faults depending on the platform and hardware configuration.

Although rare, unaligned access is a realistic scenario and thus there is some limited support for unaligned memory accesses, such as:
* `unaligned.` IL prefix
* `Unsafe.ReadUnaligned`, `Unsafe.WriteUnaligned` and ` Unsafe.CopyBlockUnaligned` helpers.

These facilities ensure fault-free access to potentially unaligned locations, but do not ensure atomicity.

As of this writing there is no specific support for operating with incoherent memory, device memory or similar. Passing non-ordinary memory to the runtime by the means of pointer operations or native interop results in Undefined Behavior.

## Side-effects and optimizations of memory accesses
.NET runtime assumes that the side-effects of memory reads and writes include only observing and changing values at specified memory locations. This applies to all reads and writes - volatile or not. **This is different from ECMA model.**

As a consequence:
* Speculative writes are not allowed.
* Reads cannot be introduced.
* Unused reads can be elided. (note: if a read can cause a fault it is not "unused")
* Adjacent non-volatile reads from the same location can be coalesced.
* Adjacent non-volatile writes to the same location can be coalesced.

The practical motivations for these rules are:
- We can't allow speculative writes as we consider changing the value to be observable, thus effects of a speculative write may not be possible to undo.
- A read cannot be re-done, since it could fetch a different value and thus introduce a data race that the program did not have.
- Reading from a variable and not observing sideeffects of the read is the same as not performing a read, thus unused reads can be removed.
- Coalescing of adjacent ordinary memory accesses to the same location is ok because most programs do not rely on presence of data races thus, unlike introducing, removing data races is ok. Programs that do rely on observing data races shall use `volatile` accesses.

## Thread-local memory accesses
It may be possible for an optimizing compiler to prove that some data is accessible only by a single thread. In such case it is permitted to perform further optimizations such as duplicating or removal of memory accesses.

## Cross-thread access to local variables
-	There is no type-safe mechanism for accessing locations on one thread’s stack from another thread.
-	Accessing managed references located on the stack of a different thread by the means of unsafe code will result in Undefined Behavior.

## Order of memory operations
* **Ordinary memory accesses**
The effects of ordinary reads and writes can be reordered as long as that preserves single-thread consistency. Such reordering can happen both due to code generation strategy of the compiler or due to weak memory ordering in the hardware.

* **Volatile reads** have "acquire semantics" - no read or write that is later in the program order may be speculatively executed ahead of a volatile read.
  Operations with acquire semantics:
     - IL load instructions with `volatile.` prefix when instruction supports such prefix
     - `System.Threading.Volatile.Read`
     - `System.Thread.VolatileRead`
     - Acquiring a lock (`System.Threading.Monitor.Enter` or entering a synchronized method)

* **Volatile writes** have "release semantics" - the effects of a volatile write will not be observable before effects of all previous, in program order, reads and writes become observable.
  Operations with release semantics:
     - IL store instructions with `volatile.` prefix when such prefix is supported
     - `System.Threading.Volatile.Write`
     - `System.Thread.VolatileWrite`
     - Releasing a lock (`System.Threading.Monitor.Exit` or leaving a synchronized method)

* **volatile. initblk** has "release semantics" - the effects of `.volatile initblk` will not be observable earlier than the effects of preceeding reads and writes.

* **volatile. cpblk** combines ordering semantics of a volatile read and write with respect to the read and written memory locations.
     - The writes performed by `volatile. cpblk` will not be observable earlier than the effects of preceeding reads and writes.
     - No read or write that is later in the program order may be speculatively executed before the reads performed by `volatile. cpblk`
     - `cpblk` may be implemented as a sequence of reads and writes. The granularity and mutual order of such reads and writes is unspecified.

Note that volatile semantics does not by itself imply that operation is atomic or has any effect on how soon the operation is committed to the coherent memory. It only specifies the order of effects when they eventually become observable.

`volatile.` and `unaligned.` IL prefixes can be combined where both are permitted.

It may be possible for an optimizing compiler to prove that some data is accessible only by a single thread. In such case it is permitted to omit volatile semantics when accessing such data.

* **Full-fence operations**
  Full-fence operations have "full-fence semantics" - effects of reads and writes must be observable no later or no earlier than a full-fence operation according to their relative program order.
  Operations with full-fence semantics:
     - `System.Thread.MemoryBarrier`
     - `System.Threading.Interlocked` methods

## C# `volatile` feature
One common way to introduce volatile memory accesses is by using C# `volatile` language feature. Declaring a field as `volatile` does not have any effect on how .NET runtime treats the field. The decoration works as a hint to the C# compiler itself (and compilers for other .Net languages) to emit reads and writes of such field as  reads and writes with `volatile.` prefix.

## Process-wide barrier
Process-wide barrier has full-fence semantics with an additional guarantee that each thread in the program effectively performs a full fence at arbitrary point synchronized with the process-wide barrier in such a way that effects of writes that precede both barriers are observable by memory operations that follow the barriers.

The actual implementation may vary depending on the platform. For example interrupting the execution of every core in the current process' affinity mask could be a suitable implementation.

## Synchronized methods
Methods decorated with ```MethodImpl(MethodImplOptions.Synchronized)``` attribute have the same memory access semantics as if a lock is acquired at an entrance to the method and released upon leaving the method.

## Data-dependent reads are ordered
Memory ordering honors data dependency. When performing indirect reads from a location derived from a reference, it is guaranteed that reading of the data will not happen ahead of obtaining the reference. This guarantee applies to both managed references and unmanaged pointers.

**Example:** reading a field, will not use a cached value fetched from the location of the field prior obtaining a reference to the instance.
 ```cs
var x = nonlocal.a.b;
var y = nonlocal.a;
var z = y.b;

// cannot have execution order as:

var x = nonlocal.a.b;
var y = nonlocal.a;
var z = x;
```

## Object assignment
Object assignment to a location potentially accessible by other threads is a release with respect to accesses to the instance’s fields/elements and metadata. An optimizing compiler must preserve the order of object assignment and data-dependent memory accesses.

The motivation is to ensure that storing an object reference to shared memory acts as a "committing point" to all modifications that are reachable through the instance reference. It also guarantees that a freshly allocated instance is valid (for example, method table and necessary flags are set) when other threads, including background GC threads are able to access the instance.
The reading thread does not need to perform an acquiring read before accessing the content of an instance since runtime guarantees ordering of data-dependent reads.

The ordering side-effects of reference assignment should not be used for general ordering purposes because:
-	independent nonvolatile reference assignments could be reordered by the compiler.
-	an optimizing compiler can omit the release semantics if it can prove that the instance is not shared with other threads.

There was a lot of ambiguity around the guarantees provided by object assignments. Going forward the runtimes will only provide the guarantees described in this document.

_It is believed that compiler optimizations do not violate the ordering guarantees in sections about [data-dependent reads](#data-dependent-reads-are-ordered) and [object assignments](#object-assignment), but further investigations are needed to ensure compliance or to fix possible violations. That is tracked by the following issue:_ https://github.com/dotnet/runtime/issues/79764

## Instance constructors
.NET runtime does not specify any ordering effects to the instance constructors.

## Static constructors
All side-effects of static constructor execution will become observable no later than effects of accessing any member of the type. Other member methods of the type, when invoked, will observe complete results of the type's static constructor execution.

## Hardware considerations
Currently supported implementations of .NET runtime and system libraries make a few expectations about the hardware memory model. These conditions are present on all supported platforms and transparently passed to the user of the runtime. The future supported platforms will likely support these as well because the large body of preexisting software will make it burdensome to break common assumptions.

* Naturally aligned reads and writes with sizes up to the platform pointer size are atomic.
That applies even for locations targeted by overlapping aligned reads and writes of different sizes.
**Example:** a read of a 4-byte aligned int32 variable will yield a value that existed prior to some write or after some write. It will never be a mix of before/after bytes.

*	The memory is cache-coherent and writes to a single location will be seen by all cores in the same order (multicopy atomic).
**Example:** when the same location is updated with values in ascending order (for example, 1,2,3,4,...), no observer will see a descending sequence.

*	It may be possible for a thread to see its own writes before they appear to other cores (store buffer forwarding), as long as the single-thread consistency is not violated.

*	The memory managed by the runtime is ordinary memory (not device register file or the like) and the only side-effects of memory operations are storing and reading of values.

*	It is possible to implement release consistency memory model.
Either the platform defaults to release consistency or stronger (that is, x64 is TSO, which is stronger), or provides means to implement release consistency via fencing operations.

*	It is possible to guarantee ordering of data-dependent reads.
Either the platform honors data dependedncy by default (all currently supported platforms), or provides means to order data-dependent reads via fencing operations.

## Examples and common patterns
The following examples work correctly on all supported implementations of .NET runtime regardless of the target OS or architecture.

*   Constructing an instance and sharing with another thread is safe and does not require explicit fences.

```cs

static MyClass obj;

// thread #1
void ThreadFunc1()
{
    while (true)
    {
        obj = new MyClass();
    }
}

// thread #2
void ThreadFunc2()
{
    while (true)
    {
        obj = null;
    }
}

// thread #3
void ThreadFunc3()
{
    MyClass localObj = obj;
    if (localObj != null)
    {
        // accessing members of the local object is safe because
        // - reads cannot be introduced, thus localObj cannot be re-read and become null
        // - publishing assignment to obj will not become visible earlier than write operations in the MyClass constructor
        // - indirect accesses via an instance are data-dependent reads, thus we will see results of constructor's writes
        System.Console.WriteLine(localObj.ToString());
    }
}

```

* Singleton (using a lock)

```cs
public class Singleton
{
    private static readonly object _lock = new object();
    private static Singleton _inst;

    private Singleton() { }

    public static Singleton GetInstance()
    {
        if (_inst == null)
        {
            lock (_lock)
            {
                // taking a lock is an acquire, the read of _inst will happen after taking the lock
                // releasing a lock is a release, if another thread assigned _inst, the write will be observed no later than the release of the lock
                // thus if another thread initialized the _inst, the current thread is guaranteed to see that here.

                if (_inst == null)
                {
                    _inst = new Singleton();
                }
            }
        }

        return _inst;
    }
}

```


* Singleton (using an interlocked operation)

```cs
public class Singleton
{
    private static Singleton _inst;

    private Singleton() { }

    public static Singleton GetInstance()
    {
        Singleton localInst = _inst;
        if (localInst == null)
        {
            // unlike the example with the lock, we may construct multiple instances
            // only one will "win" and become a unique singleton object
            Interlocked.CompareExchange(ref _inst, new Singleton(), null);

            // since Interlocked.CompareExchange is a full fence,
            // we cannot possibly read null or some other spurious instance that is not the singleton
            localInst = _inst;
        }

        return localInst;
    }
}
```

* Communicating with another thread by checking a flag

```cs
internal class Program
{
    static bool flag;

    static void Main(string[] args)
    {
        Task.Run(() => flag = true);

        // the repeated read will eventually see that the value of 'flag' has changed,
        // but the read must be Volatile to ensure all reads are not coalesced
        // into one read prior entering the while loop.
        while (!Volatile.Read(ref flag))
        {
        }

        System.Console.WriteLine("done");
    }
}
```
