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

The Objective-C runtime adheres to the [`cdecl`](https://en.wikipedia.org/wiki/X86_calling_conventions#cdecl) calling convention for all function calls. Note that method signatures with variadic arguments are supported in Objective-C.

The dispatching of messages to the target method can be impacted by the concept of "method swizzling". This is a mechanism in Objective-C where one can change the implementation of a type's method. This changing of the target method implementation is typically done in the type's `+load` or `+initialize` methods but could conceivably be done at any time. The usefulness of this feature for interoperability is presently unknown, but the technique should be kept in mind during investigations into unexpected behavior.

**Protocols**

In Objective-C the [Protocol](https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/ProgrammingWithObjectiveC/WorkingwithProtocols/WorkingwithProtocols.html) is used to define a contract that an implementing class adheres to. The Protocol feature is similar but not equivalent to the C# `interface`. The differences are primarily permissabilty rather than the concept itself, but there are important considerations in the interop domain.

For example, if an implementation responds to all the messages defined on a Protocol but doesn't declare it supports the Protocol, it can still be used where that Protocol is declared when passed to .NET. This is due to the concept of [duck typing](https://en.wikipedia.org/wiki/Duck_typing). Furthermore it is also possible that a private Objective-C implementation explicitly or implicitly supports two different Protocols and supplies the same instance of that implementation when either Protocol is requested. This issue results in a case where the same Objective-C implementation (i.e. same native instance) enters the .NET runtime environment but has a mapping to two different wrapper implementations.

**Exceptions**

The Objective-C implementation of exceptions is able to be correctly simulated through a series of well defined C functions provided by the Objective-C runtime. Details on this ABI aren't discussed here, but can be observed in various hand-written assembly code (e.g. [x86_64](https://github.com/xamarin/xamarin-macios/blob/main/runtime/trampolines-x86_64-objc_msgSend-post.inc)) in the [Xamarin-macios][xamarin_repo] code base.

**Blocks**

An Objective-C Block is conceptually a closure. The [Block ABI][blocks_abi] described by the clang compiler is used to inform interoperability with the .NET Platform. The Objective-C signature of a Block has an implied first argument for the Block itself. For example, a Block that takes an integer and returns an integer would have the following C function signature: `int (*)(id, int)`.

## Interaction between Objective-C and .NET types

Creating an acceptable interaction model between Objective-C and .NET type instances hinges on addressing several issues.

**Identity**

The mapping between an Objective-C [`id`](https://developer.apple.com/documentation/objectivec/id) and a .NET `object` is of the upmost importance. Aside from issues around inefficient memory usage, the ability to know a .NET `object`'s true self is relied upon in many important scenarios (e.g. Key in a `Dictionary<K,V>`).

Ensuring a robust mapping between these concepts can be handled more efficiently within a runtime implementation. This necessitates the ability for the consumer to register a pair (`id` and `object`) and whenever asked for one, the other can be returned. The identity mapping here also influences the subsequent design of how references and lifetime is handled (i.e. strong and weak references between Objective-C and managed objects).

**Lifetime**

Objective-C lifetime semantics are handled through [manual or automatic reference counting](https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/MemoryMgmt/Articles/MemoryMgmt.html). Reconciling this with the .NET Platform's Garbage Collector (GC) will require different solutions for each .NET runtime implementation if the interop API attempts to abstract away this complexity. Alternatively, hooks could be provided for an Objective-C interop implementation (OCII) (i.e. Xamarin) to handle lifetime issues and confine the details to the implementation. The provided hook approach is described below.

In order to facilitate accurate lifetime management an OCII must be able to provide a callback to assist the GC in determining if the type is still reachable. The following C prototype is sufficient for these purposes. It will receive the Objective-C instance that is associated with a managed object during the GC pass to determine if the associated managed object is alive. The implementation is expected to return a `0` if there is no reference or `1` if there is a reference.

```C
int callback(void*);
```

The .NET Platform will request a callback from the OCII whenever a new Objective-C instance enters the runtime. The .NET Platform will also indicate if the supplied callback will be used on a .NET defined Objective-C type or a pure Objective-C type. This distinction permits an optimization in the implementation for the callback given how .NET defined Objective-C types will be allocated.

When a .NET defined Objective-C type is allocated additional memory is requested in order to provide space for tracking metadata. This tracking metadata can be retreived through use of the Objective-C [`object_getIndexedIvars`](https://developer.apple.com/documentation/objectivec/1441508-object_getindexedivars) API. The returned pointer can be cast to the following type and is available for use by the OCII for any purpose.

```C
struct ManagedObjectWrapperLifetime
{
    /* The following will be initialized to (size_t)-1 */
    size_t scratch;
};
```

**Delegates and Blocks**

The expression of [Delegates][delegates_usage] in Objective-C and [Blocks][blocks_usage] in .NET represents a special challenge given the non-trivial [Block ABI][blocks_abi]. Conceptually the concept of a closure can be satisfied by using a C function with an extra "context" parameter and this exactly how one can interop with Blocks. Following the Block ABI the internal function pointer accepting the "context" can be acquire.

An Objective-C Block variable defined as:

```objective-c
int(^blk)(int) = ^(int a) { return a * 2; };
```

Can be queried, following the ABI, for its invoke function pointer and dispatched in C as:

```C
void* fptr = extract_using_abi(blk);
int b = ((int(*)(id,int))fptr)(blk, a);
```

The lifetime of Blocks initially relies upon semantics similar to stack clean-up in C++. This makes lifetime management more complicated in .NET. A key take away here is that the runtime will always initially create a Block that is, according to Objective-C semantics, stack allocated. The contract for Blocks and how reference counting works means that if the Objective-C caller requires the Block longer than the current calling scope the Block should be copied (i.e. [`Block_copy`](https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/Blocks/Articles/bxUsing.html#//apple_ref/doc/uid/TP40007502-CH5-SW2)) and when the current managed calling scope is left the created Block released.

**Method dispatch**

The Objective-C [message dispatch mechanism](#message_disp) will require special handling if either exception propagation or variadic variable signatures are desired. If neither exceptions nor variadic arguments are a concern then acquiring the native function pointer to the appropriate message function would be sufficient for internal uses - C# function pointers make this very easy. However, as officially described [here](https://docs.microsoft.com/xamarin/ios/internals/objective-c-selectors), users are permitted to declare their own P/Invoke signatures and perform manual message dispatch - this complicates things.

Xamarin presently defines a set of hand-written assembly for the messaging functions and they handle execptions and most variadic argument cases. Then, utilizing Mono's [`mono_dllmap_insert`](http://docs.go-mono.com/?link=root:/embed) API, maps these entries to override the official Objective-C runtime APIs when loaded through a P/Invoke. This results in users only needing to know the official Objective-C runtime APIs but getting all the benefits of the hand-written assembly.

There are several possible approaches to address this going forward:

* Support a `mono_dllmap_insert` type API to permit overloading.
* Hardcode these special cases in the runtime with the assembly code.
* Hardcode these special cases in the runtime but permit the "overrides" to be provided through a managed API.

The last option seems to have the most flexibility and follows the "pay-for-play" principle since some users may not care about exception handling or variadic argument support.

## Application Model

The application model that has been defined by the existing Xamarin scenarios contains multiple layers for consideration. Xamarin applications begin as a C/Objective-C based binary. From there the Mono runtime is activated, types registered with the Objective-C runtime, and the .NET "main" entered. This model supports a comprehensive solution for .NET to be an Objective-C application. Depending on the desired supported scenarios the early hosting may not be needed.  

### Hosting <a name="hosting"></a>

For Xamarin, activation of the .NET Platform is done through Mono's Embedding API and as such is presently exclusive to the Mono version of the .NET Platform. During the Xamarin start-up the Objective-C runtime is implicitly activated since the entry binary is itself an Objective-C application. This entry point concept poses a unique challenge for the CoreCLR given most main stream scenarios leverage the .NET supplied [AppHost](https://docs.microsoft.com/dotnet/core/project-sdk/msbuild-props#useapphost) entry binary. Alternatively the add-on scenario (i.e. consumption of native APIs) can be simplified since merely loading an Objective-C binary "activates: the Objective-C runtime.

The mutual activation of both the .NET Platform and Objective-C runtime is required for providing an interop scenario. Multiple options exist for how to accomplish this but are all primarily concerned with when as opposed to how:

* Create a new macOS specific AppHost entry binary that activates the Objective-C runtime.
    * This solution has a few flavors, but in general would bring complexity. If a single macOS option is provided users that have no interest in the Objective-C runtime will always get it and violate .NET's "pay-for-play" principle.  If multiple macOS AppHosts are produced, complexity is introduced at the build and SDK layer. 
* Provide a series of object files that can be linked on the platform if Objective-C support is determined at build time.
    * This solution addresses the "pay-for-play" principle, but introduces additional complexity in the build and SDK layer.
* Load an Objective-C binary when the Objective-C runtime is needed.
    * This solution satisfies the "pay-for-play" principle and avoids impacting build or the SDK. Loading this Objective-C binary would need to be done prior to usage of projected Objective-C types due to [Type Registration](#type_reg) purposes. Loading could also be accomplished as an extension to the AppHost, leveraging [Startup hook](https://github.com/dotnet/runtime/blob/master/docs/design/features/host-startup-hook.md), or P/Invoking into the Objective-C binary.

### Type Registration <a name="type_reg"></a>

The Objective-C runtime possesses a static (compile time) and dynamic (run time) mechanism for discovering available types and their shape. The dynamic mechanism is a series of C-style function calls that can be found in the [Objective-C runtime API][objc_runtime]. The static approach is far more complex and requires the Objective-C compiler.

For static registration, at compile time all available types are computed and special data structures are embedded within an Objective-C binary. Xamarin currently leverages this mechanism by generating Objective-C code after the .NET assembly has been built. This generated Objective-C code is then included when the Xamarin entry binary is compiled. Thus the Objective-C compiler creates all the necessary data structures for the .NET defined types that are exposed to the Objective-C runtime. The .NET code can query for this information quickly and thus reduce the need to register dynamically.

The Xamarin approach for static registration directly influenced the design of the hosting approach taken when using Mono. Similarly it should be considered when evaluating any future [hosting](#hosting) approach.

## Diagnostics - CoreCLR

The CoreCLR Diagnostics' infrastructure (i.e. [SOS](https://github.com/dotnet/diagnostics)) should be updated to assist in live and coredump scenarios. The following details should be retrievable from types:

* Determine the address of the Objective-C instance wrapping a specific managed object as well as the external reference count on that managed object (e.g. `DumpOCCW` - "Dump Objective-C Callable Wrapper").
* Determine the address of the Objective-C instance that a managed object is representing in a managed environment (e.g. `DumpROCW` - "Dump Runtime callable Objective-C Wrapper").

## References

[Xamarin-macios repository][xamarin_repo].

[Objective-C runtime][objc_runtime].

<!-- Forward references -->

[xamarin_repo]:https://github.com/xamarin/xamarin-macios
[objc_runtime]:https://developer.apple.com/documentation/objectivec
[blocks_usage]:https://developer.apple.com/library/archive/documentation/Cocoa/Conceptual/ProgrammingWithObjectiveC/WorkingwithBlocks/WorkingwithBlocks.html
[blocks_abi]:(https://clang.llvm.org/docs/Block-ABI-Apple.html)
[delegates_usage]:https://docs.microsoft.com/dotnet/csharp/programming-guide/delegates/
