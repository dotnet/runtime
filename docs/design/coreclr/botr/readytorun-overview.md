Managed Executables with Native Code
===

# Motivation

Since shipping the .NET Runtime over 10 years ago, there has only been one file format which can be used to distribute and deploy managed code components: the CLI file format. This format expresses all execution as machine independent intermediate language (IL) which must either be interpreted or compiled to native code sometime before the code is run. This lack of an efficient, directly executable file format is a very significant difference between unmanaged and managed code, and has become more and more problematic over time. Problems include:

- Native code generation takes a relatively long time and consumes power.
- For security / tamper-resistance, there is a very strong desire to validate any native code that gets run (e.g. code is signed).
- Existing native codegen strategies produce brittle code such that when the runtime or low level framework is updated, all native code is invalidated, which forces the need for recompilation of all that code.

All of these problems and complexity are things that unmanaged code simply avoids. They are avoided because unmanaged code has a format with the following characteristics:

- The executable format can be efficiently executed directly. Very little needs to be updated at runtime (binding _some_ external references) to prepare for execution. What does need to be updated can be done lazily.
- As long as a set of known versioning rules are followed, version compatible changes in one executable do not affect any other executable (you can update your executables independently of one another).
- The format is clearly defined, which allows variety of compilers to produce it.

In this proposal we attack this discrepancy between managed and unmanaged code head on: by giving managed code a file format that has the characteristics of unmanaged code listed above. Having such a format brings managed up to at least parity with unmanaged code with respect to deployment characteristics. This is a huge win!


## Problem Constraints

The .NET Runtime has had a native code story (NGEN) for a long time. However what is being proposed here is architecturally different than NGEN. NGEN is fundamentally a cache (it is optional and only affects the performance of the app) and thus the fragility of the images was simply not a concern. If anything changes, the NGEN image is discarded and regenerated. On the other hand:

**A native file format carries a strong guarantee that the file will continue to run despite updates and improvements to the runtime or framework.**

Most of this proposal is the details of achieving this guarantee while giving up as little performance as possible.

This compatibility guarantee means that, unlike NGEN, anything you place in the file is a _liability_ because you will have to support it in all future runtimes. This drives a desire to be 'minimalist' and only place things into the format that really need to be there. For everything we place into the format we have to believe either:

1. It is very unlikely to change (in particular we have not changed it over the current life of CLR)
2. We have a scheme in which we can create future runtimes that could support both old and new format efficiently (both in terms of runtime efficiency and engineering complexity).

Each feature of the file format needs to have an answer to the question of how it versions, and we will be trying to be as 'minimalist' as possible.


## Solution Outline

As mentioned, while NGEN is a native file format, it is not an appropriate starting point for this proposal because it is too fragile.

Looking carefully at the CLI file format shows that it is really 'not that bad' as a starting point. At its heart CLI is a set of database-like tables (one for types, methods, fields, etc.), which have entries that point at variable-length things (e.g. method names, signatures, method bodies). Thus CLI is 'pay for play' and since it is already public and version resilient, there is very little downside to including it in the format. By including it we also get the following useful properties:

- Immediate support for _all_ features of the runtime (at least for files that include complete CLI within them)
- The option to only add the 'most important' data required to support fast, direct execution. Everything else can be left in CLI format and use the CLI code paths. This is quite valuable given our desire to be minimalist in augmenting the format.

Moreover there is an 'obvious' way of extending the CIL file to include the additional data we need. A CLI file has a well-defined header structure, and that header already has a field that can point of to 'additional information'. This is used today in NGEN images. We would use this same technique to allow the existing CLI format to include a new 'Native Header' that would then point at any additional information needed to support fast, direct execution.

The most important parts of this extra information include:

1. Native code for the methods (as well as a way of referencing things outside the module)
2. Garbage Collection (GC) information for each method that allows you to know what values in registers and on the stack are pointers to the GC heap wherever a GC is allowed.
3. Exception handling (EH) tables that allow an exception handler to be found when an exception is thrown.
4. A table that allows the GC and EH to be found given just the current instruction pointer (IP) within the code. (IP map).
5. A table that links the information in the metadata to the corresponding native structure.

That is, we need something to link the world of metadata to the world of native. We can't eliminate meta-data completely because we want to support existing functionality. In particular we need to be able to support having other CLI images refer to types, methods and fields in this image. They will do so by referencing the information in the metadata, but once they find the target in the metadata, we will need to find the actual native code or type information corresponding to that meta-data entry. This is the purpose of the additional table. Effectively, this table is the 'export' mechanism for managed references.

Some of this information can be omitted or stored in more efficient form, e.g.:

- The garbage collection information can be omitted for environments with conservative garbage collection, such as IL2CPP.
- The full metadata information is not strictly required for 'private' methods or types so it is possible to strip it from the CLI image.
- The metadata can be stored in more efficient form, such as the .NET Native metadata format.
- The platform native executable format (ELF, Mach-O) can be used as envelope instead of PE to take advantage of platform OS loader.


## Definition of Version Compatibility for Native Code

Even for IL or unmanaged native code, there are limits to what compatible changes can be made. For example, deleting a public method is sure to be an incompatible change for any extern code using that method.

Since CIL already has a set of [compatibility rules](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/breaking-changes.md), ideally the native format would have the same set of compatibility rules as CIL. Unfortunately, that is difficult to do efficiently in all cases. In those cases we have multiple choices:

1. Change the compatibility rules to disallow some changes
2. Never generate native structures for the problematic cases (fall back to CIL techniques)
3. Generate native structures for the problematic cases, but use them only if there was no incompatible change made
4. Generate less efficient native code that is resilient

Generally the hardest versioning issues revolve around:

- Value types (structs)
- Generic methods over value types (structs)

These are problematic because value classes are valuable precisely _because_ they have less overhead than classes. They achieve this value by being 'inlined' where they are used. This makes the code generated for value classes very fragile with respect to any changes to the value class's layout, which is bad for resilience. Generics over structs have a similar issue.

Thus this proposal does _not_ suggest that we try to solve the problem of having version resilience in the presence of layout changes to value types. Instead we suggest creating a new compatibility rule:

**It is a breaking change to change the number or type of any (including private) fields of a public value type (struct). However if the struct is non-public (that is internal), and not reachable from any nesting of value type fields in any public value type, then the restriction does not apply.**

This is a compatibility that is not present for CIL. All other changes allowed by CIL can be allowed by native code without prohibitive penalty. In particular the following changes are allowed:

1. Adding instance and static fields to reference classes
2. Adding static fields to a value class.
3. Adding virtual, instance or static methods to a reference or value class
4. Changing existing methods (assuming the semantics is compatible).
5. Adding new classes.


## Version Bubbles

When changes to managed code are made, we have to make sure that all the artifacts in a native code image _only_ depend on information in other modules that _cannot_ _change_ without breaking the compatibility rules. What is interesting about this problem is that the constraints only come into play when you _cross_ module boundaries.
As an example, consider the issue of inlining of method bodies. If module A would inline a method from Module B, that would break our desired versioning property because now if that method in module B changes, there is code in Module A that would need to be updated (which we do not wish to do). Thus inlining is illegal across modules. Inlining _within_ a module, however, is still perfectly fine.

Thus in general the performance impact of versioning decreases as module size increases because there are fewer cross-module references. We can take advantage of this observation by defining something called a version bubble. **A version bubble is a set of DLLs that we are willing to update as a set.** From a versioning perspective, this set of DLLs is a single module. Inlining and other cross-module optimizations are allowed within a version bubble.

It is worth reiterating the general principle covered in this section

**Code of methods and types that do NOT span version bubbles does NOT pay a performance penalty.**

This principle is important because it means that only a fraction (for most apps a small fraction) of all code will pay any performance penalties we discuss in the sections that follow.

The extreme case is where the entire application is a single version bubble. This configuration does not need to pay any performance penalty for respecting versioning rules. It still benefits from a clearly defined file format and runtime contract that are the essential part of this proposal.

## Runtime Versioning

The runtime versioning is solved using different techniques because the runtime is responsible for interpretation of the binary format.

To allow changes in the runtime, we simply require that the new runtime handle all old formats as well as the new format. The 'main defense' in the design of the file format is having version numbers on important structures so that the runtime has the option of supporting a new version of that structure as well as the old version unambiguously by checking the version number. Fundamentally, we are forcing the developers of the runtime to be aware of this constraint and code and test accordingly.

### Restrictions on Runtime Evolution

As mentioned previously, when designing for version compatibility we have the choice of either simply disallowing a change (by changing the breaking change rules), or insuring that the format is sufficiently flexible to allow evolution. For example, for managed code we have opted to disallow changes to value type (struct) layout so that codegen for structs can be efficient. In addition, the design also includes a small number of restrictions that affect the flexibility of evolving the runtime itself. They are:

- The field layout of `System.Object` cannot change. (First, there is a pointer sized field for type information and then the other fields.)
- The field layout of arrays cannot change. (First, there is a pointer sized field for type information, and then a pointer sized field for the length. After these fields is the array data, packed using existing alignment rules.)
- The field layout of `System.String` cannot change. (First, there is a pointer sized field for type information, and then a int32 sized field for the length. After these fields is the zero terminated string data in UTF16 encoding.)

These restrictions were made because the likelihood of ever wanting to change these restrictions is low, and the performance cost _not_ having these assumptions is high. If we did not assume the field layout of `System.Object` never changes, then _every_ field fetch object outside the framework itself would span a version bubble and pay a penalty. Similarly if we don't assume the field layout for arrays or strings, then every access will pay a versioning penalty.

## Selective use of the JIT

One final point that is worth making is that selective use of the JIT compiler is another tool that can be used to avoid code quality penalties associated with version resilience, in environments where JITing is permitted. For example, assume that there is a hot user method that calls across a version bubble to a method that would a good candidate for inlining, but is not inlined because of versioning constraints. For such cases, we could have an attribute that indicates that a particular method should be compiled at runtime. Since the JIT compiler is free to generate fragile code, it can perform this inlining and thus the program steady-state performance improves. It is true that a startup time cost has been paid, but if the number of such 'hot' methods is small, the amount of JIT compilation (and thus its penalty) is not great. The point is that application developers can make this determination on a case by case basis. It is very easy for the runtime to support this capability.


# Version Resilient Native Code Generation

Because our new native format starts with the current CLI format, we have the option of falling back to it whenever we wish to. Thus we can choose to add new parts to the format in chunks. In this section we talk about the 'native code' chunk. Here we discuss the parts of the format needed to emit native code for the bodies of 'ordinary' methods. Native images that have this addition information will not need to call the JIT compiler, but will still need to call the type loader to create types.

It is useful to break the problem of generating version resilient native code by CIL instruction. Many CIL instructions (e.g. `ADD`, `MUL`, `LDLOC` ... naturally translate to native code in a version resilient ways. However CIL that deals with object model (e.g. `NEWOBJ`, `LDFLD`, etc) need special care as explained below. The descriptions below are roughly ordered in the performance priority in typical applications. Typically, each section will describe what code generation looks like when all information is within the version bubble, and then when the information crosses version bubbles. We use x64 as our native instruction set, applying the same strategy to other processor architectures is straightforward. We use the following trivial example to demonstrate the concepts

    interface Intf
    {
        void intfMethod();
    }

    class BaseClass
    {
        static int sField;
        int iField;

        public void iMethod()
        {
        }

        public virtual void vMethod(BaseClass aC)
        {
        }
    }

    class SubClass : BaseClass, Intf
    {
        int subField;

        public override void vMethod(BaseClass aC)
        {
        }

        virtual void intfMethod()
        {
        }
    }

## Instance Field access - LDFLD / STFLD

The CLR stores fields in the 'standard' way, so if RCX holds a BaseClass then

    MOV RAX, [RCX + iField_Offset]

will fetch `iField` from this object. `iField_Offset` is a constant known at native code generation time. This is known at compile time only because we mandated that the field layout of `System.Object` is fixed, and thus the entire inheritance chain of `BaseClass` is in the version bubble. It's also true even when fields in `BaseClass` contain structs (even from outside the version bubble), because we have made it a breaking change to modify the field layout of any public value type. Thus for types whose inheritance hierarchy does not span a version bubble, field fetch is as it always was.

To consider the inter-bubble case, assume that `SubClass` is defined in a different version bubble than BaseClass and we are fetching `subField`. The normal layout rules for classes require `subField` to come after all the fields of `BaseClass`. However `BaseClass` could change over time, so we can't wire in a literal constant anymore. Instead we require the following code

	    MOV TMP, [SIZE_OF_BASECLASS]
	    MOV EAX, [RCX + TMP + subfield_OffsetInSubClass]

	 .data // In the data section
     SIZE_OF_BASECLASS: UINT32 // One per EXTERN CLASS that is subclassed

Which simply assumes that a uint32 sized location has been reserved in the module and that it will be filled in with the size of `BaseClass` before this code is executed. Now a field fetch has one extra instruction, which fetches this size and that dynamic value is used to compute the field. This sequence is a great candidate for CSE (common sub-expression elimination) optimization when multiple fields of the same class are accessed by single method.

A special attention needs to be given to alignment requirements of `SubClass`.

### GC Write Barrier

The .NET GC is generational, which means that most GCs do not collect the whole heap, and instead only collect the 'new' part (which is much more likely to contain garbage). To do this it needs to know the set of roots that point into this 'new' part. This is what the GC write barrier does. Every time an object reference that lives in the GC heap is updated, bookkeeping code needs to be called to log that fact. Any fields whose values were updated are used as potential roots on these partial GCs. The important part here is that any field update of a GC reference must do this extra bookkeeping.

The write barrier is implemented as a set of helper functions in the runtime. These functions have special calling conventions (they do not trash any registers). Thus these helpers act more like instructions than calls. The write barrier logic does not need to be changed to support versioning (it works fine the way it is).


### Initializing the field size information

A key observation is that you only need this overhead for each distinct class that inherits across a version bubble. Thus there is unlikely to be many slots like `SIZE_OF_BASECLASS`. Because there are likely to be few of them, the compiler can choose to simply initialize them at module load.

Note that if you accessed an instance field of a class that was defined in another module, it is not the size that you need but the offset of a particular field. The code generated will be the same (in fact it will be simpler as no displacement is needed in the second instruction). Our coding guidelines strongly discourage public instance fields so this scenario is not particularly likely in practice (it will end up being a property call) but we can handle it in a natural way. Note also that even complex inheritance hierarchies that span multiple version bubbles are not a problem. In the end all you need is the final size of the base type. It might take a bit longer to compute during one time initialization, but that is the extent of the extra cost.

### Performance Impact

Clearly we have added an instruction and thus made the code bigger and more expensive to run. However what is also true is that the additional cost is small. The 'worst' case would be if this field fetch was in a tight loop. To measure this we created a linked list element which inherited across a version bubble. The list was long (1K) but small enough to fit in the L1 cache. Even for this extreme example (which by the way is contrived, linked list nodes do not normally inherit in such a way), the extra cost was small (< 1%).

### Null checks

The managed runtime requires any field access on null instance pointer to generate null reference exception. To avoid inserting explicit null checks, the code generator assumes that memory access at addresses smaller than certain threshold (64k on Windows NT) will generate null reference exception. If we allowed unlimited growth of the base class for cross-version bubble inheritance hierarchies, this optimization would be no longer possible.

To make this optimization possible, we will limit growth of the base class size for cross-module inheritance hierarchies. It is a new versioning restriction that does not exist in IL today.


## Non-Virtual Method Calls - CALL

### Intra-module call

If RCX holds a `BaseClass` and the caller of `iMethod` is in the same module as BaseClass then a method call is simple machine call instruction

        CALL ENTRY_IMETHOD

### Inter-module call

However if the caller is outside the module of BaseClass (even if it is in the same version bubble) we need to call it using an indirection

	    CALL [PTR_IMETHOD]

	.data // In the data section
    PTR_IMETHOD: PTR = RUNTIME_ENTRY_FIXUP_METHOD // One per call TARGET.

Just like the field case, the pointer sized data slot `PTR_IMETHOD` must be fixed up to point at the entry point of `BaseClass.iMethod`. However unlike the field case, because we are fixing up a call (and not a MOV), we can have the call fix itself up lazily via standard delay loading mechanism.
The delay loading mechanism often uses low-level tricks for maximum efficiency. Any low-level implementation of delay loading can be used as long as the resolution of the call target is left to the runtime.

### Retained Flexibility for runtime innovation

Note that it might seem that we have forever removed the possibility of innovating in the way we do SLOT fixup, since we 'burn' these details into the code generation and runtime helpers. However this is not true. What we have done is require that we support the _current_ mechanism for doing such fixup. Thus we must always support a `RUNTIME_ENTRY_FIXUP_METHOD` helper. However we could devise a completely different scheme. All that would be required is that you use a _new_ helper and _keep_ the old one. Thus you can have a mix of old and new native code in the same process without issue.

### Calling Convention

The examples above did not have arguments and the issue of calling convention was not obvious. However it is certainly true that the native code at the call site does depend heavily on the calling convention and that convention must be agreed to between the caller and the callee at least for any particular caller-callee pair.

The issue of calling convention is not specific to managed code and thus hardware manufacturers typically define a calling convention that tends to be used by all languages on the system (thus allowing interoperability). In fact for all platforms except x86, CLR attempts to follow the platform calling convention.

Our understanding of the most appropriate managed convention evolved over time. Our experience tells us that it is worthwhile for implementation simplicity to always pass managed `this` pointer in the fixed register, even if the platform standard calling convention says otherwise.

#### Managed Code Specific Conventions

In addition the normal conventions for passing parameters as well as the normal convention of having a hidden byref parameter for returning value types, CLR has a few managed code specific argument conventions:

1. Shared generic code has a hidden parameter that represents the type parameters in some cases for methods on generic types and for generic methods.
2. GC interactions with hidden return buffer. The convention for whether the hidden return buffer can be allocated in the GC heap, and thus needs to be written to using write barrier.

These conventions would be codified as well.

### Performance Impact

Because it was already the case that methods outside the current module had to use an indirect call, versionability does not introduce more overhead for non-virtual method calls if inlining was not done. Thus the main cost of  making the native code version resilient is the requirement that no cross version bubble inlining can happen.

The best solution to this problem is to avoid 'chatty' library designs (Unfortunately, `IEnumerable`, is such a chatty design, where each iteration does a `MoveNext` and `Current` property fetch). Another mitigation is the one mentioned previously: to allow clients of the library to selectively JIT compile some methods that make these chatty calls. Finally you can also use new custom `NonVersionableAttribute` attribute, which effectively changes the versioning contract to indicate that the library supplier has given up their right to change that method's body and thus it would be legal to inline.

The proposal is to disallow cross-version bubble inlining by default, and selectively allow inlining for critical methods (by giving up the right to change the method).

Experiments with disabled cross-module inlining with the selectively enabled inlining of critical methods showed no visible regression in ASP.NET throughput.

## Non-Virtual calls as the baseline solution to all other versioning issues

It is important to observe that once you have a mechanism for doing non-virtual function calls in a version resilient way (by having an indirect CALL through a slot that that can be fixed lazily at runtime, all other versioning problems _can_ be solved in that way by calling back to the 'definer' module, and having the operation occur there instead. Issues associated with this technique

1. You will pay the cost of a true indirection function call and return, as well as any argument setup cost. This cost may be visible in constructs that do not contain a call naturally, like fetching string literals or other constants. You may be able to get better performance from another technique (for example, we did so with instance field access).
2. It introduces a lot of indirect calls. It is not friendly to systems that disallow on the fly code generation. A small helper stub has to be created at runtime in the most straightforward implementation, or there has to be a scheme how to pre-create or recycle the stubs.
3. It requires that the defining assembly 'know' the operations that it is responsible for defining. In general this could be fixed by JIT compiling whatever is needed at runtime (where the needed operations are known), but JIT compiling is the kind of expensive operation that we are trying to avoid at runtime.

So while there are limitations to the technique, it works very well on a broad class of issues, and is conceptually simple. Moreover, it has very nice simplicity on the caller side (a single indirect call). It is hard to get simpler than this. This simplicity means that you have wired very few assumptions into the caller which maximizes the versioning flexibility, which is another very nice attribute. Finally, this technique also allows generation of optimal code once the indirect call was made. This makes for a very flexible technique that we will use again and again.

The runtime currently supports two mechanisms for virtual dispatch. One mechanism is called virtual stub dispatch (VSD). It is used when calling interface methods. The other is a variation on traditional vtable-based dispatch and it is used when a non-interface virtual is called. We first discuss the VSD approach.

Assume that RCX holds a `Intf` then the call to `intfMethod()` would look like

	     CALL [PTR_CALLSITE]
	.data // in the data section
    PTR_CALLSITE: INT_PTR = RUNTIME_ENTRY_FIXUP_METHOD // One per call SITE.

This looks same as the cross-module, non-virtual case, but there are important differences. Like the non-virtual case there is an indirect call through a pointer that lives in the module. However unlike the non-virtual case, there is one such slot per call site (not per target). What is in this slot is always guaranteed to get to the target (in this case to `Intf.intfMethod()`), but it is expected to change over time. It starts out pointing to a 'dumb' stub which simply calls a runtime helper that does the lookup (in likely a slow way). However, it can update the `PTR_CALLSITE` slot to a stub that efficiently dispatches to the interface for the type that actually occurred (the remaining details of stubbed based interface dispatch are not relevant to versioning).

The above description is accurate for the current CLR implementation for interface dispatch. What's more, is that nothing needs to be changed about the code generation to make it version resilient. It 'just works' today. Thus interface dispatch is version resilient with no performance penalty.

What's more, we can actually see VSD is really just a modification of the basic 'indirect call through updateable slot' technique that was used for non-virtual method dispatch. The main difference is that because the target depends on values that are not known until runtime (the type of the 'this' pointer), the 'fixup' function can never remove itself completely but must always check this runtime value and react accordingly (which might include fixing up the slot again). To make as likely as possible that the value in the fixup slot stabilizes, we create a fixup slot per call site (rather than per target).

### Vtable Dispatch

The CLR current also supports doing virtual dispatch through function tables (vtables). Unfortunately, vtables have the same version resilience problem as fields. This problem can be fixed in a similar way, however unlike fields, the likelihood of having many cross bubble fixups is higher for methods than for instance fields. Further, unlike fields we already have a version resilient mechanism that works (VSD), so it would have to be better than that to be worth investing in. Vtable dispatch is only better than VSD for polymorphic call sites (where VSD needs to resort to a hash lookup). If we find we need to improve dispatch for this case we have some possible mitigations to try:

1. If the polymorphism is limited, simply trying more cases before falling back to the hash table has been prototyped and seems to be a useful optimization.
2. For high polymorphism case, we can explore the idea of dynamic vtable slots (where over time the virtual method a particular vtable slot holds can change). Before falling back to the hash table a virtual method could claim a vtable slot and now the dispatch of that method for _any_ type will be fast.

In short, because of the flexibility and natural version resilience of VSD, we propose determining if VSD can be 'fixed' before investing in making vtables version resilient and use VSD for all cross version bubble interface dispatch. This does not preclude using vtables within a version bubble, nor adding support for vtable based dispatch in the future if we determine that VSD dispatch can't be fixed.


## Object Creation - NEWOBJ / NEWARR

Object allocation is always done by a helper call that allocates the uninitialized object memory (but does initialize the type information `MethodTable` pointer), followed by calling the class constructor. There are a number of different helpers depending on the characteristics of the type (does it have a finalizer, is it smaller than a certain size, ...).

We will defer the choice of the helper to use to allocate the object to runtime. For example, to create an instance of `SubClass` the code would be:

        CALL [NEWOBJ_SUBCLASS]
    	MOV RCX, RAX  // EAX holds the new object
		// If the constructor had parameters, set them
	    CALL SUBCLASS_CONSTRUCTOR

    .data // In the data section
    NEWOBJ_SUBCLASS: RUNTIME_ENTRY_FIXUP // One per type

where the `NEWOBJ_SUBCLASS` would be fixed up using the standard lazy technique.

The same technique works for creating new arrays (NEWARR instruction).


## Type Casting - ISINST / CASTCLASS

The proposal is to use the same technique as for object creation. Note that type casting could easily be a case where VSD techniques would be helpful (as any particular call might be monomorphic), and thus caching the result of the last type cast would be a performance win. However this optimization is not necessary for version resilience.


## GC Information for Types

To do its job the garbage collector must be able to take an arbitrary object in the GC heap and find all the GC references in that object. It is also necessary for the GC to 'scan' the GC from start to end, which means it needs to know the size of every object. Fast access to two pieces of information is what is needed.
From a versioning perspective, the fundamental problem with GC information is that (like field offsets) it incorporates information from the entire inheritance hierarchy in general case. This means that the information is not version resilient.

While it is possible to make the GC information resilient and have the GC use this resilient data, GC happens frequently and type loading happens infrequently, so arguably you should trade type loading speed for GC speed if given the choice. Moreover the size of the GC information is typically quite small (e.g. 12-32 bytes) and will only occur for those types that cross version bubbles. Thus forming the GC information on the fly (from a version resilient form) is a reasonable starting point.

Another important observation is that `MethodTable` contains other very frequently accessed data, like flags indicating whether the `MethodTable` represents an array, or pointer to parent type. This data tends to change a lot with the evolution of the runtime. Thus, generating method tables at runtime will solve a number of other versioning issues in addition to the GC information versioning.

# Current State

The design and implementation is a work in progress under code name ReadyToRun (`FEATURE_READYTORUN`). RyuJIT is used as the code generator to produce the ReadyToRun images currently.
