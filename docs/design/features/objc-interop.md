# Objective-C interoperability

This design document describes a singular .NET Platform support scenario for an Objective-C runtime. This design is heavily influenced by existing [Xamarin-macios][xamarin_repo] support which is built on top of Mono's [Embedding API](https://www.mono-project.com/docs/advanced/embedding/).

## Objective-C concepts

There are concepts in Objective-C that require attention for interoperability.

**Message dispatch** <a name="message_disp"></a>

Method dispatch in Objective-C is typically done through message passing. A suite of functions make up the message passing system.

- [`objc_msgSend`](https://developer.apple.com/documentation/objectivec/1456712-objc_msgsend) - Canonical message passing.
- [`objc_msgsend_fpret`](https://developer.apple.com/documentation/objectivec/1456697-objc_msgsend_fpret) - Message returns a floating point value.
- [`objc_msgSend_stret`](https://developer.apple.com/documentation/objectivec/1456730-objc_msgsend_stret) - Message returns a structure.
- [`objc_msgSendSuper`](https://developer.apple.com/documentation/objectivec/1456716-objc_msgsendsuper) - Send message to the super class.
- [`objc_msgSendSuper_stret`](https://developer.apple.com/documentation/objectivec/1456722-objc_msgsendsuper_stret) - Send message that returns a structure to the super class.

Each of these functions are designed to look up the target method on the object ([`id`](https://developer.apple.com/documentation/objectivec/id)) based on the supplied selector ([`SEL`](https://developer.apple.com/documentation/objectivec/sel)) and then `jmp` to that method ([`IMP`](https://developer.apple.com/documentation/objectivec/objective-c_runtime/imp)). Note that this is a `jmp`, not a `call`. From the perspective of the caller the return is from the target method not the message dispatch function. Looking up the above functions, one will note each are defined with a `void(*)(void)` signature. During the compilation of Objective-C code, the compiler will compute the appropriate function signature, cast the message passing function, and make the call. All Objective-C methods have two implied arguments - the object (i.e. `id`) and the message selector (`SEL`). This means the following Objective-C statement:

```objective-c
int b = [obj doubleNumber: a];
```

Will be converted by the compiler to the C style signature of:

```C
// The SEL, "doubleNumberSel", is computed at compile time.
// A SEL can also be constructed at run time.
int b = ((int(*)(id,SEL,int))&objc_msgSend)(obj, doubleNumberSel, a);
```

The Objective-C runtime adheres to the [`cdecl`](https://en.wikipedia.org/wiki/X86_calling_conventions#cdecl) calling convention for all function calls. It should be noted that variadic arguments are supported in Objective-C.

**Exceptions**

The Objective-C implementation of exceptions is able to be correctly simulated through a series of well defined C functions provided by the Objective-C runtime. Details on this ABI aren't discussed here, but can be observed in various hand-written assembly code (e.g. [x86_64](https://github.com/xamarin/xamarin-macios/blob/main/runtime/trampolines-x86_64-objc_msgSend-post.inc)) in the Xamarin-macios code base.

**Blocks**

An Objective-C Block is conceptually a closure. The [Block ABI][blocks_abi] described by the clang compiler is used to inform interoperability with the .NET Platform. The Objective-C signature of a Block has an implied first argument for the Block itself. For example, a Block that takes an integer and returns an integer would have the follow C function signature: `int (*)(id, int)`.

## Interaction between Objective-C and .NET types

Creating an acceptable interaction model between Objective-C and .NET type instances hinges on addressing several issues.

**Identity**

The mapping between an Objective-C [`id`](https://developer.apple.com/documentation/objectivec/id) and a .NET `object` is of the upmost importance. Aside from issues around inefficient memory usage, the ability to know a .NET `object`'s true self is relied upon in many important scenarios (e.g. Key in a `Dictionary<K,V>`).

Ensuring a robust mapping between these concepts can be handled more efficiently within a runtime implementation. This necessitates the ability for the consumer to register a pair (`id` and `object`) and whenever asked for one, the other can be returned.

**Lifetime**

Objective-C lifetime semantics are handled through [manual or automatic reference counting](https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/MemoryMgmt/Articles/MemoryMgmt.html). Reconciling this with the .NET Platform's Garbage Collector will require different solutions for each .NET runtime implementation.

_Note_: In the following illustrations, a strong reference is depicted as a solid line (`===`) and a weak reference is depicted as a dashed line (`= = =`).

When a .NET object enters an Objective-C environment, the Objective-C proxy is subject to reference counting and ensuring it extends the lifetime of the managed object it wraps. This can be accomplished in CoreCLR through use of the internal `HNDTYPE_REFCOUNTED` GC handle type. This handle, coupled with a reference count, can be used to transition a GC handle between a weak and strong reference.

Creating an Objective-C proxy will also require overriding the built-in [`retain`](https://developer.apple.com/documentation/objectivec/1418956-nsobject/1571946-retain) and [`release`](https://developer.apple.com/documentation/objectivec/1418956-nsobject/1571957-release) methods provided by the Objective-C runtime.

```
 --------------------                  ----------------------
|   Managed object   |                |   Objective-C proxy  |
|                    |                | Ref count: 1         |
|  ----------------  |                |  ------------------  |
| | Weak reference |=| = = = = = = = >| | REFCOUNTED handle| |
| |    to proxy    | |<===============|=|    to object     | |
|  ----------------  |                |  ------------------  |
 --------------------                  ----------------------
```

When an Objective-C object enters a .NET environment, its lifetime must be extended by the managed proxy. The managed proxy needs only to retain a reference count to extend the Objective-C object's lifetime. When the managed proxy is finalized, it will release its reference count on the Objective-C object.

```
 --------------------                  ----------------------
| Objective-C object |                |     Managed proxy    |
| Ref count: +1      |                |                      |
|  ----------------  |                |  ------------------  |
| | Weak reference |=| = = = = = = = >| | Strong reference | |
| |    to proxy    | |<===============|=|    to object     | |
|  ----------------  |                |  ------------------  |
 --------------------                  ----------------------
```

_Note_: There is a special case with interoperability and threading in Objective-C's reference counting system - .NET [thread pools](https://docs.microsoft.com/dotnet/standard/threading/the-managed-thread-pool). The Objective-C runtime assumes each thread of execution possesses its own [`NSAutoreleasePool`](https://developer.apple.com/documentation/foundation/nsautoreleasepool) instance. In order to ensure this invariant, support for managing a thread's `NSAutoreleasePool` instance is required.

**Delegates and Blocks**

The expression of [Delegates][delegates_usage] in Objective-C and [Blocks][blocks_usage] in .NET represents a special challenge given the non-trivial [Block ABI][blocks_abi]. Conceptually the concept of a closure can be satisfied by using a C function with an extra "context" parameter and this exactly how one can interop with Blocks. Following the Block ABI the internal function pointer accepting the "context" can be acquire.

An Objective-C Block variable defined as:

```objective-c
int(^blk)(int) = ^(int a) { return a*2; };
```

Can be queried, following the ABI, for its invoke function pointer and dispatched in C as:

```C
void* fptr = extract_using_abi(blk);
int b = ((int(*)(id,int))fptr)(blk, a);
```

**Method dispatch**

The Objective-C [message dispatch mechanism](#message_disp) will require special handling if either exception propagation or variadic variable signatures are desired. If neither exceptions nor variadic arguments are a concern then acquiring the native function pointer to the appropriate message function would be sufficient for internal uses - C# function pointers make this very easy. However, as officially described [here](https://docs.microsoft.com/xamarin/ios/internals/objective-c-selectors), users are permitted to declare their own P/Invoke signatures and perform manual message dispatch - this complicates things.

Xamarin presently defines a set of hand-written assembly for the messaging functions and they handle execptions and most variadic argument cases. Then, utilizing Mono's [`mono_dllmap_insert`](http://docs.go-mono.com/?link=root:/embed) API, maps these entries to override the official Objective-C runtime APIs when loaded through a P/Invoke. This results in users only needing to know the official Objective-C runtime APIs but getting all the benefits of the hand-written assembly.

There are several possible approaches to address this going forward:

* Support a `mono_dllmap_insert` type API to permit overloading.
* Hardcode these special cases in the runtime with the assembly code.
* Hardcode these special cases in the runtime but permit the "overrides" to be provided through a managed API.

The last option seems to have the most flexibility and follows the "pay-for-play" principle since some users may not care about exception handling or variadic argument support.

## Application Model

The application model that has been defined by the existing Xamarin scenarios contains multiple layers for consideration. Xamarin applications begin as a C/Objective-C based binary. From there the Mono runtime is activated, types registered with the Objective-C runtime, and the .NET "main" entered.

### Hosting <a name="hosting"></a>

For Xamarin, activation of the .NET Platform is done through Mono's Embedding API and as such is presently exclusive to the Mono version of the .NET Platform. During the Xamarin start-up the Objective-C runtime is implicitly activated since the entry binary is itself an Objective-C application. This entry point concept poses a unique challenge for the CoreCLR given most main stream scenarios leverage the .NET supplied [AppHost](https://docs.microsoft.com/dotnet/core/project-sdk/msbuild-props#useapphost) entry binary.

The mutual activation of both the .NET Platform and Objective-C runtime is required for providing an interop scenario. Multiple options exist for how to accomplish this:

* Create a new macOS specific AppHost entry binary that activates the Objective-C runtime.
    * This solution has a few flavors, but in general would bring complexity. If a single macOS option is provided users that have no interest in the Objective-C runtime will always get it and violate .NET's "pay-for-play" principle.  If multiple macOS AppHosts are produced, complexity is introduced at the build and SDK layer. 
* Provide a series of object files that can be linked on the platform if Objective-C support is determined at build time.
    * This solution addresses the "pay-for-play" principle, but introduces additional complexity in the build and SDK layer.
* Load an Objective-C binary early on in the activation of the AppHost binary.
    * This solution satisfies the "pay-for-play" principle and avoids impacting build or the SDK. Loading this Objective-C binary would need to be done early for [Type Registration](#type_reg) purposes. Loading could also be accomplished as an extension to the AppHost or by leveraging [Startup hook](https://github.com/dotnet/runtime/blob/master/docs/design/features/host-startup-hook.md).

### Type Registration <a name="type_reg"></a>

The Objective-C runtime possesses a static (compile time) and dynamic (run time) mechanism for discovering available types and their shape. The dynamic mechanism is a series of C-style function calls that can be found in the [Objective-C runtime API][objc_runtime]. The static approach is far more complex and requires the Objective-C compiler.

For static registration, at compile time all available types are computed and special data structures are embedded within an Objective-C binary. Xamarin currently leverages this mechanism by generating Objective-C code after the .NET assembly has been built. This generated Objective-C code is then included when the Xamarin entry binary is compiled. Thus the Objective-C compiler creates all the necessary data structures for the .NET defined types that are exposed to the Objective-C runtime. The .NET code can query for this information quickly and thus reduce the need to register dynamically.

The Xamarin approach for static registration directly influenced the design of the hosting approach taken when using Mono. Similarly it should be considered when evaluating any future [hosting](#hosting) approach.

## Diagnostics - CoreCLR

The CoreCLR Diagnostics' infrastructure (i.e. [SOS](https://github.com/dotnet/diagnostics)) should be updated to assist in live and coredump scenarios. The following details should be retrievable from types:

* Determine the address of the Objective-C instance wrapping a specific managed object as well as the external reference count on that managed object (e.g. `DumpOCCW` - "Dump Objective-C callable wrapper").
* Determine the address of the Objective-C instance that a managed object is representing in a managed environment (e.g. `DumpROCW` - "Dump Runtime Objective-C wrapper").

## References

[Xamarin-macios repository][xamarin_repo].

[Objective-C runtime][objc_runtime].

<!-- Forward references -->

[xamarin_repo]:https://github.com/xamarin/xamarin-macios
[objc_runtime]:https://developer.apple.com/documentation/objectivec
[blocks_usage]:https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/ProgrammingWithObjectiveC/WorkingwithBlocks/WorkingwithBlocks.html
[blocks_abi]:(https://clang.llvm.org/docs/Block-ABI-Apple.html)
[delegates_usage]:https://docs.microsoft.com/dotnet/csharp/programming-guide/delegates/