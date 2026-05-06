
Design
------

The design is based on docs/design/features/unloadability.md.

Memory managers
---------------

Most runtime memory is owned by MonoMemoryManager structures, which are owned by one or more ALCs. For example,
if type T1 is from ALC1, and T2 is from ALC2, then the memory related to the generic instance T1<T2> will
be owned by the memory manager for [ALC1, ALC2]. A memory manager is collectible if one of its ALCs is collectible.

LoaderAllocator objects
-----------------------

Each memory manager has a corresponding LoaderAllocator object. This object is referenced explicitly or
implicitly by every managed object which is related to the memory manager. Until unloading starts,
the memory manager keeps a strong reference to this object.

For objects whose type is from a collectible ALC, the GC maintains an implicit reference by marking
the LoaderAllocator object when the object is marked.

Reflection objects referencing the ALC have an explicit 'keepalive' field which points to the
corresponding LoaderAllocator object.

When the LoaderAllocator object is collected by the GC, it means there are no more managed
references to its memory allocator, so it can be unloaded.

Reflection caches and static variables
--------------------------------------

Reflection objects are kept in caches inside the runtime. These caches keep strong references to the
reflection objects. This doesn't work for collectible ALCs, since the objects keep the LoaderAllocator
object alive. So for collectible ALCs, we use a different kind of hash table whose keys/values are
stored in object[] arrays inside the LoaderAllocator object, and the hash table holds a weak reference
to these arrays. This means that the reflection objects are only kept alive by the LoaderAllocator and
by application code.

Similarly, normal static variables are treated as GC roots so any static variable pointing to an object
inside the ALC would keep the ALC alive. Instead, we store static variables inside arrays in the
LoaderAllocator object. Normal reference type variable are allocated an entry in a pinned object[],
and their address becomes an interior pointer to the pinned array element. For valuetypes, we
allocate a pinned boxed object and the static variable address becomes the address of the unboxed
object.

Unloading process
-----------------

- The app calls AssemblyLoadContext.Unload ()
- The runtime iterates over the memory managers of the ALC. Each ALC holds a strong reference to its
LoaderAllocator object. We change the strong reference to a weak one, allowing the LoaderAllocator
to the collected.
- The app eventually drops all implicit and explicit references to the LoaderAllocator object.
- The finalizer of the LoaderAllocator object calls into the runtime to free the corresponding
memory manager.
- When all memory managers referencing the ALC are freed, the ALC itself is freed.

See the document in the Design section about the LoaderAllocatorScout object.

Remaining work
---------------

- Managed frames
Frames belonging to code in the ALC should keep the ALC alive. This can be implemented by
having all methods allocate a volatile local variable and store a reference to their LoaderAllocator object
into it.
- Thread abort
Although its not part of current .NET APIs, it might be useful to have a way to abort threads executing code
in an ALC, similarly to how domain unloads worked previously.
- Reflection pointers
Icalls which take a assembly/type etc. handle as parameter need to keep the ALC alive, otherwise there will
be subtle races.
- Boehm GC support
- TLS variables
- Enable/disable compiler flag
- Profiling, perf counters, etc. support
- Diagnostics support, i.e. what keeps an ALC alive
- Testing
- Leak detection
