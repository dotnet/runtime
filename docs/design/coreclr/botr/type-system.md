Type System Overview
====================

Author: David Wrighton ([@davidwrighton](https://github.com/davidwrighton)) - 2010

Introduction
============

The CLR type system is our representation the type system described in the ECMA specification + extensions.

Overview
--------

The type system is composed of a series of data structures, some of which are described in other Book of the Runtime chapters, as well as a set of algorithms which operate on and create those data structures. It is NOT the type system exposed through reflection, although that one does depend on this system.

The major data structures maintained by the type system are:

- MethodTable
- EEClass
- MethodDesc
- FieldDesc
- TypeDesc
- ClassLoader

The major algorithms contained within the type system are:

- **Type Loader:** Used to load types and create most of the primary data structures of the type system.
- **CanCastTo and similar:** The functionality of comparing types.
- **LoadTypeHandle:** Primarily used for finding types.
- **Signature parsing:** Used to compare and gather information about methods and fields.
- **GetMethod/FieldDesc:** Used to find/load methods/fields.
- **Virtual Stub Dispatch:** Used to find the destination of virtual calls to interfaces.

There are significantly more ancillary data structures and algorithms that provide various bits of information to the rest of the CLR, but they are less significant to the overall understanding of the system.

Component Architecture
----------------------

The type system's data structures are generally used by all of the various algorithms. This document does not describe the type system algorithms (as there are or should be other book of the runtime documents for those), but it does attempt to describe the various major data structures below.

Dependencies
------------

The type system is generally a service provided to many parts of the CLR, and most core components have some form of dependency on the behavior of the type system. This diagram describes the general dataflow that effects the type system. It is not exhaustive, but calls out the major information flows.

![dependencies](images/type-system-dependencies.png)

### Component Dependencies

The primary dependencies of the type system follow:

- The **loader** needed to get the correct metadata to work with.
- The **metadata system** provides a metadata API to gather information.
- The **security system** informs the type system whether or not certain type system structures are permitted (e.g. inheritance).
- The **AppDomain** provides a LoaderAllocator to handle allocation behavior for the type system data structures.

### Components Dependent on this Component

The type system has 3 primary components which depend on it.

- The **Jit interface**, and the jit helpers primarily depends on the type, method, and field searching functionality. Once the type system object is found, the data structures returned have been tailored to provide the information needed by the jit.
- **Reflection** uses the type system to provide relatively simple access to ECMA standardized concepts which we happen to capture in the CLR type system data structures.
- **General managed code execution** requires the use of the type system for type comparison logic, and virtual stub dispatch.

Design of Type System
=====================

The core type system data structures are the data structures that represent the actual loaded types (e.g. TypeHandle, MethodTable, MethodDesc, TypeDesc, EEClass) and the data structure that allow types to be found once they are loaded (e.g. ClassLoader, Assembly, Module, RIDMaps).

The data structures and algorithms for loading types are discussed in the [Type Loader](type-loader.md) and [MethodDesc](method-descriptor.md) Book of the Runtime chapters.

Tying those data structures together is a set of functionality that allows the JIT/Reflection/TypeLoader/stackwalker to find existing types and methods. The general idea is that these searches should be easily driven by the metadata tokens/signatures that are specified in the ECMA CLI specification.

And finally, when the appropriate type system data structure is found, we have algorithms to gather information from a type, and/or compare two types. A particularly complicated example of this form of algorithm may be found in the [Virtual Stub Dispatch](virtual-stub-dispatch.md) Book of the Runtime chapter.

Design Goals and Non-goals
--------------------------

### Goals

- Accessing information needed at runtime from executing (non-reflection) code is very fast.
- Accessing information needed at compilation time for generating code is straightforward.
- The garbage collector/stackwalker is able to access necessary information without taking locks, or allocating memory.
- Minimal amounts of types are loaded at a time.
- Minimal amounts of a given type are loaded at type load time.
- Type system data structures must be storable in NGEN images.

### Non-Goals

- All information in the metadata is directly reflected in the CLR data structures.
- All uses of reflection are fast.

Design of a typical algorithm used at runtime during execution of managed code
------------------------------------------------------------------------------

The casting algorithm is typical of algorithms in the type system that are heavily used during the execution of managed code.

There are at least 4 separate entry points into this algorithm. Each entry point is chosen to provide a different fast path, in the hopes that the best performance possible will be achieved.

- Can an object be cast to a particular non-type equivalent non-array type?
- Can an object be cast to an interface type that does not implement generic variance?
- Can an object be cast to an array type?
- Can an object of a type be cast to an arbitrary other managed type?

Each of these implementations with the exception of the last one is optimized to perform better at the expense of not being fully general.

For instance, the "Can a type be cast to a parent type" which is a variant of "Can an object be cast to a particular non-type equivalent non-array type?" code is implemented with a single loop that walks a singly linked list. This is only able to search a subset of possible casting operations, but it is possible to determine if that is the appropriate set by examining the type the cast is trying to enforce. This algorithm is implemented in the jit helper JIT\_ChkCastClass\_Portable.

Assumptions:

- Special purpose implementations of algorithms are a performance improvement in general.
- Extra versions of algorithms do not provide an insurmountable maintenance problem.

Design of typical search algorithm in the Type System
-----------------------------------------------------

There are a number of algorithms in the type system which follow this common pattern.

The type system is commonly used to find a type. This may be triggered via any number of inputs such as the JIT, reflection, serialization, remoting, etc.

The basic input to the type system in these cases is

- The context from which the search shall begin (a Module or assembly pointer).
- An identifier that describes the sought after type in the initial context. This is typically a token, or a string (if an assembly is the search context).

The algorithm must first decode the identifier.

For the search for a type scenario, the token may be either a TypeDef token, a TypeRef token, a TypeSpec token, or a string. Each of these different identifiers will cause a different form of lookup.

- A **typedef token** will cause a lookup in the RidMap of the Module. This is a simple array index.
- A **typeref token** will cause a lookup to find the assembly which this typeref token refers to, and then the type finding algorithm is begun anew with the found assembly pointer, and a string gathered from the typeref table.
- A **typespec token** indicates that a signature must be parsed to find the signature. Parse the signature to find the information necessary to load the type. This will recursively trigger more type finding.
- A **name** is used to bind between assemblies. The TypeDef/ExportedTypes table is searched for matches. Note: This search is optimized by hashtables on the manifest module object.

From this design a number of common characteristics of search algorithms in the type system are evident.

- Searches use input that is tightly coupled to metadata. In particular, metadata tokens and string names are commonly passed around. Also, these searches are tied to Modules, which directly map to .dll and .exe files.
- Use of cached information to improve performance. The RidMap and hash tables are data structures optimized to improve these lookups.
- The algorithms typically have 3-4 different paths based on their input.

In addition to this general design, there are a number of extra requirements that are layered onto this.

- **ASSUMPTION:** Searching for types that are already loaded is safe to perform while stopped in the GC.
- **INVARIANT:** A type which has already been loaded will always be found if searched for.
- **ISSUE:** Search routines rely on metadata reading. This can yield inadequate performance in some scenarios.

This search algorithm is typical of the routines used during JITing. It has a number of common characteristics.

- It uses metadata.
- It requires looking for data in many places.
- There is relatively little duplication of data in our data structures.
- It typically does not recurse deeply, and does not have loops.

This allows us to meet the performance requirements, and characteristics necessary for working with an IL based JIT.

Garbage Collector Requirements on the Type System
-------------------------------------------------

The garbage collector requires information about instances of types allocated in the GC heap. This is done via a pointer to a type system data structure (MethodTable) at the head of every managed object. Attached to the MethodTable, is a data structure that describes the GC layout of instances of types. There are two forms of this layout (one for normal types, and object arrays, and another for arrays of valuetypes).

- **ASSUMPTION:** Type system data structures have a lifetime that exceeds that of managed objects that are of types described in the type system data structure.
- **REQUIREMENT:** The garbage collector has a requirement to execute the stack walker while the runtime is suspended. This will be discussed next.

Stackwalker requirements on the Type System
-------------------------------------------

The stack walker/ GC stack walker requires type system input in 2 cases.

- For finding the size of valuetypes on the stack.
- For finding GC roots to report within valuetypes on the stack.

For various reasons involving the desire to delay load types, and the avoidance of generating multiple versions of code (that only differ via associated gc info) the CLR currently requires the walking of signatures of methods that are on the stack. This need is rarely exercised, as it requires the stack walker to execute at very particular moments in time, but in order to meet our reliability goals, the signature walker must be able to function while stackwalking.

The stack walker executes in approximately 3 modes.

- To walk the stack of the current thread for security or exception processing reasons.
- To walk the stack of all threads for GC purposes (all threads are suspended by the EE).
- To walk the stack of a particular thread for a profiler (that specific thread is suspended).

In the GC stack walking case, and in the profiler stack walking case, due to thread suspension, it is not safe to allocate memory or take most locks.

This has led us to develop a path through the type system which may be relied upon to follow the above requirement.

The rule required for the type system to achieve this goal is:

- If a method has been called, then all valuetype parameters of the called method will have been loaded into some appdomain in the process.
- The assembly reference from the assembly with the signature to the assembly implementing the type must be resolved before a walk of the signature is necessary as part of a stack walk.

This is enforced via an extensive and complicated set of enforcements within the type loader, NGEN image generation process, and JIT.

- **ISSUE:** Stackwalker requirements on the type system are HIGHLY fragile.
- **ISSUE:** Implementation of stack walker requirements in the type system requires a set of contract violations at every function in the type system that may be touched while searching for types which are loaded.
- **ISSUE:** The signature walks performed are done with the normal signature walking code. This code is designed to load types as it walks the signature, but in this case the type load functionality is used with the assumption that no type load will actually be triggered.
- **ISSUE:** Stackwalker requirements require support from not just the type system, but also the assembly loader. The Loader has had a number of issues meeting the needs of the type system here.

## Static variables

Static variables in CoreCLR are handled by a combination of getting the "static base", and then adjusting it by an offset to get a pointer to the actual value.
We define the statics base as either non-gc or gc for each field.
Currently non-gc statics are any statics which are represented by primitive types (byte, sbyte, char, int, uint, long, ulong, float, double, pointers of various forms), and enums.
GC statics are any statics which are represented by classes or by non-primitive valuetypes.
For valuetype statics which are GC statics, the static variable is actually a pointer to a boxed instance of the valuetype.

### Per type static variable information
As of .NET 9, the static variable bases are now all associated with their particular type.
As you can see from this diagram, the data for statics can be acquired by starting at a `MethodTable` and then getting either the `DynamicStaticsInfo` to get a statics pointer, or by getting a `ThreadStaticsInfo` to get a TLSIndex, which then can be used with the thread static variable system to get the actual thread static base.

```mermaid
classDiagram
MethodTable : MethodTableAuxiliaryData* m_pAuxData
MethodTable --> MethodTableAuxiliaryData
MethodTableAuxiliaryData --> DynamicStaticsInfo : If has static variables
MethodTableAuxiliaryData --> GenericStaticsInfo : If is generic and has static variables
MethodTableAuxiliaryData --> ThreadStaticsInfo : If has thread local static variables

DynamicStaticsInfo : StaticsPointer m_pGCStatics
DynamicStaticsInfo : StaticsPointer m_pNonGCStatics

GenericStaticsInfo : FieldDesc* m_pFieldDescs

ThreadStaticsInfo : TLSIndex NonGCTlsIndex
ThreadStaticsInfo : TLSIndex GCTlsIndex
```

```mermaid
classDiagram

note for StaticsPointer "StaticsPointer is a pointer sized integer"
StaticsPointer : void* PointerToStaticBase
StaticsPointer : bool HasClassConstructorBeenRun

note for TLSIndex "TLSIndex is a 32bit integer"
TLSIndex : TLSIndexType indexType
TLSIndex : 24bit int indexOffset
```

In the above diagram, you can see that we have separate fields for non-gc and gc statics, as well as thread and normal statics.
For normal statics, we use a single pointer sized field, which also happens to encode whether or not the class constructor has been run.
This is done to allow lock free atomic access to both get the static field address as well as determine if the class constructor needs to be triggered.
For TLS statics, handling of detecting whether or not the class constructor has been run is a more complex process described as part of the thread statics infrastructure.
The `DynamicStaticsInfo` and `ThreadStaticsInfo` structures are accessed without any locks, so it is important to ensure that access to fields on these structures can be done with a single memory access, to avoid memory order tearing issues.

Also, notably, for generic types, each field has a `FieldDesc` which is allocated per type instance, and is not shared by multiple canonical instances.

#### Lifetime management for collectible statics

Finally we have a concept of collectible assemblies in the CoreCLR runtime, so we need to handle lifetime management for static variables.
The approach chosen was to build a special GC handle type which will allow the runtime to have a pointer in the runtime data structures to the interior of a managed object on the GC heap.

The requirement of behavior here is that a static variable cannot keep its own collectible assembly alive, and so collectible statics have the peculiar property that they can exist and be finalized before the collectible assembly is finally collected.
If there is some resurrection scenario, this can lead to very surprising behavior.

### Thread Statics

Thread statics are static variables which have a lifetime which is defined to be the shorter of the lifetime of the type containing the static, and the lifetime of the thread on which the static variable is accessed.
They are created by having a static variable on a type which is attributed with `[System.Runtime.CompilerServices.ThreadStaticAttribute]`.
The general scheme of how this works is to assign an "index" to the type which is the same on all threads, and then on each thread hold a data structure which is efficiently accessed by means of this index.
However, we have a few peculiarities in our approach.

1. We segregate collectible and non-collectible thread statics (`TLSIndexType::NonCollectible` and `TLSIndexType::Collectible`)
2. We provide an ability to share a non-gc thread static between native CoreCLR code and managed code (Subset of `TLSIndexType::DirectOnThreadLocalData`)
3. We provide an extremely efficient means to access a small number of non-gc thread statics. (The rest of the usage of `TLSIndexType::DirectOnThreadLocalData`)

#### Per-Thread Statics Data structures
```mermaid
classDiagram

note for ThreadLocalInfo "There is 1 of these per thread, and it is managed by the C++ compiler/OS using standard mechanisms.
It can be found as the t_ThreadStatics variable in a C++ compiler, and is also pointed at by the native Thread class."
ThreadLocalInfo : int cNonCollectibleTlsData
ThreadLocalInfo : void** pNonCollectibleTlsArrayData
ThreadLocalInfo : int cCollectibleTlsData
ThreadLocalInfo : void** pCollectibleTlsArrayData
ThreadLocalInfo : InFlightTLSData *pInFightData
ThreadLocalInfo : Thread* pThread
ThreadLocalInfo : Special Thread Statics Shared Between Native and Managed code
ThreadLocalInfo : byte[N] ExtendedDirectThreadLocalTLSData

InFlightTLSData : InFlightTLSData* pNext
InFlightTLSData : TLSIndex tlsIndex
InFlightTLSData : OBJECTHANDLE hTLSData

ThreadLocalInfo --> InFlightTLSData : For TLS statics which have their memory allocated, but have not been accessed since the class finished running its class constructor
InFlightTLSData --> InFlightTLSData : linked list
```

#### Access patterns for getting the thread statics address

This is the pattern that the JIT will use to access a thread static which is not `DirectOnThreadLocalData`.

0. Get the TLS index somehow
1. Get TLS pointer to OS managed TLS block for the current thread ie. `pThreadLocalData = &t_ThreadStatics`
2. Read 1 integer value `pThreadLocalData->cCollectibleTlsData OR pThreadLocalData->cNonCollectibleTlsData`
3. Compare cTlsData against the index we're looking up `if (cTlsData < index.GetIndexOffset())`
4. If the index is not within range, jump to step 11.
5. Read 1 pointer value from TLS block `pThreadLocalData->pCollectibleTlsArrayData` OR `pThreadLocalData->pNonCollectibleTlsArrayData`
6. Read 1 pointer from within the TLS Array. `pTLSBaseAddress = *(intptr_t*)(((uint8_t*)pTlsArrayData) + index.GetIndexOffset()`
7. If pointer is NULL jump to step 11 `if pTLSBaseAddress == NULL`
8. If TLS index not a Collectible index, return pTLSBaseAddress
9. if `ObjectFromHandle((OBJECTHANDLE)pTLSBaseAddress)` is NULL, jump to step 11
10. Return `ObjectFromHandle((OBJECTHANDLE)pTLSBaseAddress)`
11. Tail-call a helper `return GetThreadLocalStaticBase(index)`

This is the pattern that the JIT will use to access a thread static which is on `DirectOnThreadLocalData`
0. Get the TLS index somehow
1. Get TLS pointer to OS managed TLS block for the current thread ie. `pThreadLocalData = &t_ThreadStatics`
2. Add the index offset to the start of the ThreadLocalData structure `pTLSBaseAddress = ((uint8_t*)pThreadLocalData) + index.GetIndexOffset()`

#### Lifetime management for thread static variables
We distinguish between collectible and non-collectible thread static variables for efficiency purposes.

A non-collectible thread static is a thread static defined on a type which cannot be collected by the runtime.
This describes most thread statics in actual observed practice.
The `DirectOnThreadLocalData` statics are a subset of this category which has a speical optimized form and does not need any GC reporting.
For non-collectible thread statics, the pointer (`pNonCollectibleTlsArrayData`) in the `ThreadLocalData` is a pointer to a managed `object[]` which points at either `object[]`, `byte[]`, or `double[]` arrays.
At GC scan time, the pointer to the initial object[] is the only detail which needs to be reported to the GC.

A collectible thread static is a thread static which can be collected by the runtime.
This describes the static variables defined on types which can be collected by the runtime.
The pointer (`pCollectibleTlsArrayData`) in the `ThreadLocalData` is a pointer to a chunk of memory allocated via `malloc`, and holds pointers to `object[]`, `byte[]`, or `double[]` arrays.
At GC scan time, each managed object must individually be kept alive only if the type and thread is still alive. This requires properly handling several situations.
1. If a collectible assembly becomes unreferenced, but a thread static variable associated with it has a finalizer, the object must move to the finalization queue.
2. If a thread static variable associated with a collectible assembly refers to the collectible assembly `LoaderAllocator` via a series of object references, it must not provide a reason for the collectible assembly to be considered referenced.
3. If a collectible assembly is collected, then the associated static variables no longer exist, and the TLSIndex values associated with that collectible assembly becomes re-useable.
4. If a thread is no longer executing, then all thread statics associated with that thread are no longer kept alive.

The approach chosen is to use a pair of different handle types.
For efficient access, the handle type stored in the dynamically adjusted array is a WeakTrackResurrection GCHandle.
This handle instance is associated with the slot in the TLS data, not with the exact instantiation, so it can be re-used when the if the associated collectible assembly is collected, and then the slot is re-used.
In addition, each slot that is in use will have a `LOADERHANDLE` which will keep the object alive until the `LoaderAllocator` is freed.
This `LOADERHANDLE` will be abandoned if the `LoaderAllocator` is collected, but that's ok, as `LOADERHANDLE` only needs to be cleaned up if the `LoaderAllocator` isn't collected.
On thread destroy, for each collectible slot in the tls array, we will explicitly free the `LOADERHANDLE` on the correct `LoaderAllocator`.

Physical Architecture
=====================

Major parts of the type system are found in:

- Class.cpp/inl/h – EEClass functions, and BuildMethodTable
- MethodTable.cpp/inl/h – Functions for manipulating methodtables.
- TypeDesc.cpp/inl/h – Functions for examining TypeDesc
- MetaSig.cpp SigParser – Signature code
- FieldDesc /MethodDesc – Functions for examining these data structures
- Generics – Generics specific logic.
- Array – Code for handling the special cases required for array processing
- VirtualStubDispatch.cpp/h/inl – Code for virtual stub dispatch
- VirtualCallStubCpu.hpp – Processor specific code for virtual stub dispatch.
- threadstatics.cpp/h - Handling for thread static variables.

Major entry points are BuildMethodTable, LoadTypeHandleThrowing, CanCastTo\*, GetMethodDescFromMemberDefOrRefOrSpecThrowing, GetFieldDescFromMemberRefThrowing, CompareSigs, and VirtualCallStubManager::ResolveWorkerStatic.

Related Reading
===============

- [ECMA CLI Specification](/docs/project/dotnet-standards.md)
- [Type Loader](type-loader.md) Book of the Runtime Chapter
- [Virtual Stub Dispatch](virtual-stub-dispatch.md) Book of the Runtime Chapter
- [MethodDesc](method-descriptor.md) Book of the Runtime Chapter
