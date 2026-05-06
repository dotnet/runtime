Introduction to the Common Language Runtime (CLR)
===

By Vance Morrison ([@vancem](https://github.com/vancem)) - 2007

What is the Common Language Runtime (CLR)? To put it succinctly:

> The Common Language Runtime (CLR) is a complete, high level virtual machine designed to support a broad variety of programming languages and interoperation among them.

Phew, that was a mouthful.  It also in and of itself is not very illuminating.  The statement above _is_ useful however, because it is the first step in taking the large and complicated piece of software known as the [CLR][clr] and grouping its features in an understandable way.  It gives us a "10,000 foot" view of the runtime from which we can understand the broad goals and purpose of the runtime.  After understanding the CLR at this high level, it is easier to look more deeply into sub-components without as much chance of getting lost in the details.

# The CLR: A (very rare) Complete Programming Platform

Every program has a surprising number of dependencies on its runtime environment.  Most obviously, the program is written in a particular programming language, but that is only the first of many assumptions a programmer weaves into the program.  All interesting programs need some _runtime library_ that allows them to interact with the other resources of the machine (such as user input, disk files, network communications, etc).  The program also needs to be converted in some way (either by interpretation or compilation) to a form that the native hardware can execute directly.  These dependencies of a program are so numerous, interdependent and diverse that implementers of programming languages almost always defer to other standards to specify them.  For example, the C++ language does not specify the format of a C++ executable.  Instead, each C++ compiler is bound to a particular hardware architecture (e.g., X86) and to an operating system environment (e.g., Windows, Linux, or Mac OS), which describes the format of the executable file format and specifies how it will be loaded.  Thus, programmers don't make a "C++ executable," but rather a "Windows X86 executable" or a "Power PC Mac OS executable."

While leveraging existing hardware and operating system standards is usually a good thing, it has the disadvantage of tying the specification to the level of abstraction of the existing standards.  For example, no common operating system today has the concept of a garbage-collected heap.  Thus, there is no way to use existing standards to describe an interface that takes advantage of garbage collection (e.g., passing strings back and forth, without worrying about who is responsible for deleting them).  Similarly, a typical executable file format provides just enough information to run a program but not enough information for a compiler to bind other binaries to the executable.  For example, C++ programs typically use a standard library (on Windows, called msvcrt.dll) which contains most of the common functionality (e.g., printf), but the existence of that library alone is not enough.  Without the matching header files that go along with it (e.g., stdio.h), programmers can't use the library.  Thus, existing executable file format standards cannot be used both to describe a file format that can be run and to specify other information or binaries necessary to make the program complete.

The CLR fixes problems like these by defining a [very complete specification][ecma-spec] (standardized by ECMA) containing the details you need for the COMPLETE lifecycle of a program, from construction and binding through deployment and execution.  Thus, among other things, the CLR specifies:

- A GC-aware virtual machine with its own instruction set (called the Common Intermediate Language (CIL)) used to specify the primitive operations that programs perform.  This means the CLR is not dependent on a particular type of CPU.
- A rich meta data representation for program declarations (e.g., types, fields, methods, etc), so that compilers generating other executables have the information they need to call functionality from 'outside'.
- A file format that specifies exactly how to lay the bits down in a file, so that you can properly speak of a CLR EXE that is not tied to a particular operating system or computer hardware.
- The lifetime semantics of a loaded program, the mechanism by which one CLR EXE file can refer to another CLR EXE and the rules on how the runtime finds the referenced files at execution time.
- A class library that leverages the features that the CLR provides (e.g., garbage collection, exceptions, or generic types) to give access both to basic functionality (e.g., integers, strings, arrays, lists, or dictionaries) as well as to operating system services (e.g., files, network, or user interaction).

Multi-language Support
----------------------

Defining, specifying and implementing all of these details is a huge undertaking, which is why complete abstractions like the CLR are very rare.  In fact, the vast majority of such reasonably complete abstractions were built for single languages.  For example, the Java runtime, the Perl interpreter or the early version of the Visual Basic runtime offer similarly complete abstraction boundaries.  What distinguishes the CLR from these earlier efforts is its multi-language nature.  With the possible exception of Visual Basic (because it leverages the COM object model), the experience within the language is often very good, but interoperating with programs written in other languages is very difficult at best.  Interoperation is difficult because these languages can only communicate with "foreign" languages by using the primitives provided by the operating system.  Because the OS abstraction level is so low (e.g., the operating system has no concept of a garbage-collected heap), needlessly complicated techniques are necessary.  By providing a COMMON LANGUAGE RUNTIME, the CLR allows languages to communicate with each other with high-level constructs (e.g., GC-collected structures), easing the interoperation burden dramatically.

Because the runtime is shared among _many_ languages, it means that more resources can be put into supporting it well.  Building good debuggers and profilers for a language is a lot of work, and thus they exist in a full-featured form only for the most important programming languages.  Nevertheless, because languages that are implemented on the CLR can reuse this infrastructure, the burden on any particular language is reduced substantially.  Perhaps even more important, any language built on the CLR immediately has access to _all_ the class libraries built on top of the CLR.  This large (and growing) body of (debugged and supported) functionality is a huge reason why the CLR has been so successful.

In short, the runtime is a complete specification of the exact bits one has to put in a file to create and run a program.  The virtual machine that runs these files is at a high level appropriate for implementing a broad class of programming languages.  This virtual machine, along with an ever growing body of class libraries that run on that virtual machine, is what we call the common language runtime (CLR).

# The Primary Goal of the CLR

Now that we have basic idea what the CLR is, it is useful to back up just a bit and understand the problem the runtime was meant to solve.  At a very high level, the runtime has only one goal:

> The goal of the CLR is to make programming easy.

This statement is useful for two reasons.  First, it is a _very_ useful guiding principle as the runtime evolves.  For example, fundamentally only simple things can be easy, so adding **user visible** complexity to the runtime should always be viewed with suspicion.  More important than the cost/benefit ratio of a feature is its _added exposed complexity/weighted benefit over all scenarios_ ratio.  Ideally, this ratio is negative (that is, the new feature reduces complexity by removing restrictions or by generalizing existing special cases); however, more typically it is kept low by minimizing the exposed complexity and maximizing the number of scenarios to which the feature adds value.

The second reason this goal is so important is that **ease of use is the fundamental reason for the CLR's success**.  The CLR is not successful because it is faster or smaller than writing native code (in fact, well-written native code often wins).  The CLR is not successful because of any particular feature it supports (like garbage collection, platform independence, object-oriented programming or versioning support).  The CLR is successful because all of those features, as well as numerous others, combine to make programming significantly easier than it would be otherwise.  Some important but often overlooked ease of use features include:

1. Simplified languages (e.g., C# and Visual Basic are significantly simpler than C++)
2. A dedication to simplicity in the class library (e.g., we only have one string type, and it is immutable; this greatly simplifies any API that uses strings)
3. Strong consistency in the naming in the class library (e.g., requiring APIs to use whole words and consistent naming conventions)
4. Great support in the tool chain needed to create an application (e.g., Visual Studio makes building CLR applications very simple, and Intellisense makes finding the right types and methods to create the application very easy).

It is this dedication to ease of use (which goes hand in hand with simplicity of the user model) that stands out as the reason for the success of the CLR.  Oddly, some of the most important ease-of-use features are also the most "boring." For example, any programming environment could apply consistent naming conventions, yet actually doing so across a large class library is quite a lot of work.  Often such efforts conflict with other goals (such as retaining compatibility with existing interfaces), or they run into significant logistical concerns (such as the cost of renaming a method across a _very_ large code base).  It is at times like these that we have to remind ourselves about our number-one overarching goal of the runtime and ensure that we have our priorities straight to reach that goal.

# Fundamental Features of the CLR

The runtime has many features, so it is useful to categorize them as follows:

1. **Fundamental features** – Features that have broad impact on the design of other features.  These include:
    1. Garbage Collection
    2. Memory Safety and Type Safety
    3. High level support for programming languages.
2. **Secondary features** – Features enabled by the fundamental features that may not be required by many useful programs:
    1. Program isolation with AppDomains
    2. Program Security and sandboxing
3. **Other Features** – Features that all runtime environments need but that do not leverage the fundamental features of the CLR.  Instead, they are the result of the desire to create a complete programming environment.  Among them are:
    1. Versioning
    2. Debugging/Profiling
    3. Interoperation

## The CLR Garbage Collector (GC)

Of all the features that the CLR provides, the garbage collector deserves special notice.  Garbage collection (GC) is the common term for automatic memory reclamation.  In a garbage-collected system, user programs no longer need to invoke a special operator to delete memory.  Instead the runtime automatically keeps track of all references to memory in the garbage-collected heap, and from time-to-time, it will traverse these references to find out which memory is still reachable by the program.  All other memory is _garbage_ and can be reused for new allocations.

Garbage collection is a wonderful user feature because it simplifies programming.  The most obvious simplification is that most explicit delete operations are no longer necessary.  While removing the delete operations is important, the real value to the programmer is a bit more subtle:

1. Garbage collection simplifies interface design because you no longer have to carefully specify which side of the interface is responsible for deleting objects passed across the interface.  For example, CLR interfaces simply return strings; they don't take string buffers and lengths.  This means they don't have to deal with the complexity of what happens when the buffers are too small.  Thus, garbage collection allows ALL interfaces in the runtime to be simpler than they otherwise would be.
2. Garbage collection eliminates a whole class of common user mistakes.  It is frightfully easy to make mistakes concerning the lifetime of a particular object, either deleting it too soon (leading to memory corruption), or too late (unreachable memory leaks).  Since a typical program uses literally MILLIONS of objects, the probability for error is quite high.  In addition, tracking down lifetime bugs is very difficult, especially if the object is referenced by many other objects.  Making this class of mistakes impossible avoids a lot of grief.

Still, it is not the usefulness of garbage collection that makes it worthy of special note here.  More important is the simple requirement it places on the runtime itself:

> Garbage collection requires ALL references to the GC heap to be tracked.

While this is a very simple requirement, it in fact has profound ramifications for the runtime.  As you can imagine, knowing where every pointer to an object is at every moment of program execution can be quite difficult.  We have one mitigating factor, though.  Technically, this requirement only applies to when a GC actually needs to happen (thus, in theory we don't need to know where all GC references are all the time, but only at the time of a GC).  In practice, however, this mitigation doesn't completely apply because of another feature of the CLR:

> The CLR supports multiple concurrent threads of execution with a single process.

At any time some other thread of execution might perform an allocation that requires a garbage collection.  The exact sequence of operations across concurrently executing threads is non-deterministic.  We can't tell exactly what one thread will be doing when another thread requests an allocation that will trigger a GC.  Thus, GCs can really happen any time.  Now the CLR does NOT need to respond _immediately_ to another thread's desire to do a GC, so the CLR has a little "wiggle room" and doesn't need to track GC references at _all_ points of execution, but it _does_ need to do so at enough places that it can guarantee "timely" response to the need to do a GC caused by an allocation on another thread.

What this means is that the CLR needs to track _all_ references to the GC heap _almost_ all the time.  Since GC references may reside in machine registers, in local variables, statics, or other fields, there is quite a bit to track.  The most problematic of these locations are machine registers and local variables because they are so intimately related to the actual execution of user code.  Effectively, what this means is that the _machine code_ that manipulates GC references has another requirement: it must track all the GC references that it uses.  This implies some extra work for the compiler to emit the instructions to track the references.

To learn more, check out the [Garbage Collector design document](garbage-collection.md).

## The Concept of "Managed Code"

Code that does the extra bookkeeping so that it can report all of its live GC references "almost all the time" is called _managed code_ (because it is "managed" by the CLR).  Code that does not do this is called _unmanaged code_.  Thus all code that existed before the CLR is unmanaged code, and in particular, all operating system code is unmanaged.

### The stack unwinding problem

Clearly, because managed code needs the services of the operating system, there will be times when managed code calls unmanaged code.  Similarly, because the operating system originally started the managed code, there are also times when unmanaged code calls into managed code.  Thus, in general, if you stop a managed program at an arbitrary location, the call stack will have a mixture of frames created by managed code and frames created by unmanaged code.

The stack frames for unmanaged code have _no_ requirements on them over and above running the program.  In particular, there is no requirement that they can be _unwound_ at runtime to find their caller.  What this means is that if you stop a program at an arbitrary place, and it happens to be in a unmanaged method, there is no way in general<sup>[1]</sup> to find who the caller was.  You can only do this in the debugger because of extra information stored in the symbolic information (PDB file).  This information is not guaranteed to be available (which is why you sometimes don't get good stack traces in a debugger).  This is quite problematic for managed code, because any stack that can't be unwound might in fact contain managed code frames (which contain GC references that need to be reported).

Managed code has additional requirements on it: not only must it track all the GC references it uses during its execution, but it must also be able to unwind to its caller.  Additionally, whenever there is a transition from managed code to unmanaged code (or the reverse), managed code must also do additional bookkeeping to make up for the fact that unmanaged code does not know how to unwind its stack frames.  Effectively, managed code links together the parts of the stack that contain managed frames.  Thus, while it still may be impossible to unwind the unmanaged stack frames without additional information, it will always be possible to find the chunks of the stack that correspond to managed code and to enumerate the managed frames in those chunks.

[1] More recent platform ABIs (application binary interfaces) define conventions for encoding this information, however there is typically not a strict requirement for all code to follow them.

### The "World" of Managed Code

The result is that special bookkeeping is needed at every transition to and from managed code.  Managed code effectively lives in its own "world" where execution can't enter or leave unless the CLR knows about it.  The two worlds are in a very real sense distinct from one another (at any point in time the code is in the _managed world_ or the _unmanaged world_).  Moreover, because the execution of managed code is specified in a CLR format (with its [Common Intermediate Language][cil-spec] (CIL)), and it is the CLR that converts it to run on the native hardware, the CLR has _much_ more control over exactly what that execution does.  For example, the CLR could change the meaning of what it means to fetch a field from an object or call a function.  In fact, the CLR does exactly this to support the ability to create MarshalByReference objects.  These appear to be ordinary local objects, but in fact may exist on another machine.  In short, the managed world of the CLR has a large number of _execution hooks_ that it can use to support powerful features which will be explained in more detail in the coming sections.

In addition, there is another important ramification of managed code that may not be so obvious.  In the unmanaged world, GC pointers are not allowed (since they can't be tracked), and there is a bookkeeping cost associated with transitioning from managed to unmanaged code.  What this means is that while you _can_ call arbitrary unmanaged functions from managed code, it is often not pleasant to do so.  Unmanaged methods don't use GC objects in their arguments and return types, which means that any "objects" or "object handles" that those unmanaged functions create and use need to be explicitly deallocated.  This is quite unfortunate.  Because these APIs can't take advantage of CLR functionality such as exceptions or inheritance, they tend to have a "mismatched" user experience compared to how the interfaces would have been designed in managed code.

The result of this is that unmanaged interfaces are almost always _wrapped_ before being exposed to managed code developers.  For example, when accessing files, you don't use the Win32 CreateFile functions provided by the operating system, but rather the managed System.IO.File class that wraps this functionality.  It is in fact extremely rare that unmanaged functionality is exposed to users directly.

While this wrapping may seem to be "bad" in some way (more code that does not seem to do much), it is in fact good because it actually adds quite a bit of value.  Remember it was always _possible_ to expose the unmanaged interfaces directly; we _chose_ to wrap the functionality.  Why?  Because the overarching goal of the runtime is to **make programming easy**, and typically the unmanaged functions are not easy enough.  Most often, unmanaged interfaces are _not_ designed with ease of use in mind, but rather are tuned for completeness.  Anyone looking at the arguments to CreateFile or CreateProcess would be hard pressed to characterize them as "easy." Luckily, the functionality gets a "facelift" when it enters the managed world, and while this makeover is often very "low tech" (requiring nothing more complex than renaming, simplification, and organizing the functionality), it is also profoundly useful.  One of the very important documents created for the CLR is the [Framework Design Guidelines][fx-design-guidelines].  This 800+ page document details best practices in making new managed class libraries.

Thus, we have now seen that managed code (which is intimately involved with the CLR) differs from unmanaged code in two important ways:

1. High Tech: The code lives in a distinct world, where the CLR controls most aspects of program execution at a very fine level (potentially to individual instructions), and the CLR detects when execution enters and exits managed code.  This enables a wide variety of useful features.
2. Low Tech: The fact that there is a transition cost when going from managed to unmanaged code, as well as the fact that unmanaged code cannot use GC objects encourages the practice of wrapping most unmanaged code in a managed façade.  This means interfaces can get a "facelift" to simplify them and to conform to a uniform set of naming and design guidelines that produce a level of consistency and discoverability that could have existed in the unmanaged world, but does not.

**Both** of these characteristics are very important to the success of managed code.

## Memory and Type Safety

One of the less obvious but quite far-reaching features that a garbage collector enables is that of memory safety.  The invariant of memory safety is very simple: a program is memory safe if it accesses only memory that has been allocated (and not freed).  This simply means that you don't have "wild" (dangling) pointers that are pointing at random locations (more precisely, at memory that was freed prematurely).  Clearly, memory safety is a property we want all programs to have.  Dangling pointers are always bugs, and tracking them down is often quite difficult.

> A GC _is_ necessary to provide memory safety guarantees

One can quickly see how a garbage collector helps in ensuring memory safety because it removes the possibility that users will prematurely free memory (and thus access memory that was not properly allocated).  What may not be so obvious is that if you want to guarantee memory safety (that is make it _impossible_ for programmers to create memory-unsafe programs), practically speaking you can't avoid having a garbage collector.  The reason for this is that non-trivial programs need _heap style_ (dynamic) memory allocations, where the lifetime of the objects is essentially under arbitrary program control (unlike stack-allocated, or statically-allocated memory, which has a highly constrained allocation protocol).  In such an unconstrained environment, the problem of determining whether a particular explicit delete statement is correct becomes impossible to predict by program analysis.  Effectively, the only way you have to determine if a delete is correct is to check it at runtime.  This is exactly what a GC does (checks to see if memory is still live).  Thus, for any programs that need heap-style memory allocations, if you want to guarantee memory safety, you _need_ a GC.

While a GC is necessary to ensure memory safety, it is not sufficient.  The GC will not prevent the program from indexing off the end of an array or accessing a field off the end of an object (possible if you compute the field's address using a base and offset computation).  However, if we do prevent these cases, then we can indeed make it impossible for a programmer to create memory-unsafe programs.

While the [common intermediate language][cil-spec] (CIL) _does_ have operators that can fetch and set arbitrary memory (and thus violate memory safety), it also has the following memory-safe operators and the CLR strongly encourages their use in most programming:

1. Field-fetch operators (LDFLD, STFLD, LDFLDA) that fetch (read), set and take the address of a field by name.
2. Array-fetch operators (LDELEM, STELEM, LDELEMA) that fetch, set and take the address of an array element by index.  All arrays include a tag specifying their length.  This facilitates an automatic bounds check before each access.

By using these operators instead of the lower-level (and unsafe) _memory-fetch_ operators in user code, as well as avoiding other unsafe [CIL][cil-spec] operators (e.g., those that allow you to jump to arbitrary, and thus possibly bad locations) one could imagine building a system that is memory-safe but nothing more.  The CLR does not do this, however.  Instead the CLR enforces a stronger invariant: type safety.

For type safety, conceptually each memory allocation is associated with a type.  All operators that act on memory locations are also conceptually tagged with the type for which they are valid.  Type safety then requires that memory tagged with a particular type can only undergo operations allowed for that type.  Not only does this ensure memory safety (no dangling pointers), it also allows additional guarantees for each individual type.

One of the most important of these type-specific guarantees is that the visibility attributes associated with a type (and in particular with fields) are enforced.  Thus, if a field is declared to be private (accessible only by the methods of the type), then that privacy will indeed be respected by all other type-safe code.  For example, a particular type might declare a count field that represents the count of items in a table.  Assuming the fields for the count and the table are private, and assuming that the only code that updates them updates them together, there is now a strong guarantee (across all type-safe code) that the count and the number of items in the table are indeed in sync.  When reasoning about programs, programmers use the concept of type safety all the time, whether they know it or not.  The CLR elevates type-safety from being simply a programming language/compiler convention, to something that can be strictly enforced at run time.

### Verifiable Code - Enforcing Memory and Type Safety

Conceptually, to enforce type safety, every operation that the program performs has to be checked to ensure that it is operating on memory that was typed in a way that is compatible with the operation.  While the system could do this all at runtime, it would be very slow.  Instead, the CLR has the concept of [CIL][cil-spec] verification, where a static analysis is done on the [CIL][cil-spec] (before the code is run) to confirm that most operations are indeed type-safe.  Only when this static analysis can't do a complete job are runtime checks necessary.  In practice, the number of run-time checks needed is actually very small.  They include the following operations:

1. Casting a pointer to a base type to be a pointer to a derived type (the opposite direction can be checked statically)
2. Array bounds checks (just as we saw for memory safety)
3. Assigning an element in an array of pointers to a new (pointer) value.  This particular check is only required because CLR arrays have liberal casting rules (more on that later...)

Note that the need to do these checks places requirements on the runtime.  In particular:

1. All memory in the GC heap must be tagged with its type (so the casting operator can be implemented).  This type information must be available at runtime, and it must be rich enough to determine if casts are valid (e.g., the runtime needs to know the inheritance hierarchy).  In fact, the first field in every object on the GC heap points to a runtime data structure that represents its type.
2. All arrays must also have their size (for bounds checking).
3. Arrays must have complete type information about their element type.

Luckily, the most expensive requirement (tagging each heap item) was something that was already necessary to support garbage collection (the GC needs to know what fields in every object contain references that need to be scanned), so the additional cost to provide type safety is low.

Thus, by verifying the [CIL][cil-spec] of the code and by doing a few run-time checks, the CLR can ensure type safety (and memory safety).  Nevertheless, this extra safety exacts a price in programming flexibility.  While the CLR does have general memory fetch operators, these operators can only be used in very constrained ways for the code to be verifiable.  In particular, all pointer arithmetic will fail verification today.  Thus many classic C or C++ conventions cannot be used in verifiable code; you must use arrays instead.  While this constrains programming a bit, it really is not bad (arrays are quite powerful), and the benefits (far fewer "nasty" bugs), are quite real.

The CLR strongly encourages the use of verifiable, type-safe code.  Even so, there are times (mostly when dealing with unmanaged code) that unverifiable programming is needed.  The CLR allows this, but the best practice here is to try to confine this unsafe code as much as possible.  Typical programs have only a very small fraction of their code that needs to be unsafe, and the rest can be type-safe.

## High Level Features

Supporting garbage collection had a profound effect on the runtime because it requires that all code must support extra bookkeeping.  The desire for type-safety also had a profound effect, requiring that the description of the program (the [CIL][cil-spec]) be at a high level, where fields and methods have detailed type information.  The desire for type safety also forces the [CIL][cil-spec] to support other high-level programming constructs that are type-safe.  Expressing these constructs in a type-safe manner also requires runtime support.  The two most important of these high-level features are used to support two essential elements of object oriented programming: inheritance and virtual call dispatch.

### Object Oriented Programming

Inheritance is relatively simple in a mechanical sense.  The basic idea is that if the fields of type `derived` are a superset of the fields of type `base`, and `derived` lays out its fields so the fields of `base` come first, then any code that expects a pointer to an instance of `base` can be given a pointer to an instance of `derived` and the code will "just work".  Thus, type `derived` is said to inherit from `base`, meaning that it can be used anywhere `base` can be used.  Code becomes _polymorphic_ because the same code can be used on many distinct types.  Because the runtime needs to know what type coercions are possible, the runtime must formalize the way inheritance is specified so it can validate type safety.

Virtual call dispatch generalizes inheritance polymorphism.  It allows base types to declare methods that will be _overridden_ by derived types.  Code that uses variables of type `base` can expect that calls to virtual methods will be dispatched to the correct overridden method based on the actual type of the object at run time.  While such _run-time dispatch logic_ could have been implemented using primitive [CIL][cil-spec] instructions without direct support in the runtime, it would have suffered from two important disadvantages

1. It would not be type safe (mistakes in the dispatch table are catastrophic errors)
2. Each object-oriented language would likely implement a slightly different way of implementing its virtual dispatch logic.  As result, interoperability among languages would suffer (one language could not inherit from a base type implemented in another language).

For this reason, the CLR has direct support for basic object-oriented features.  To the degree possible, the CLR tried to make its model of inheritance "language neutral," in the sense that different languages might still share the same inheritance hierarchy.  Unfortunately, that was not always possible.  In particular, multiple inheritance can be implemented in many different ways.  The CLR chose not to support multiple inheritance on types with fields, but does support multiple inheritance from special types (called interfaces) that are constrained not to have fields.

It is important to keep in mind that while the runtime supports these object-oriented concepts, it does not require their use.  Languages without the concept of inheritance (e.g., functional languages) simply don't use these facilities.

### Value Types (and Boxing)

A profound, yet subtle aspect of object oriented programming is the concept of object identity: the notion that objects (allocated by separate allocation calls) can be distinguished, even if all their field values are identical.  Object identity is strongly related to the fact that objects are accessed by reference (pointer) rather than by value.  If two variables hold the same object (their pointers address the same memory), then updates to one of the variables will affect the other variable.

Unfortunately, the concept of object identity is not a good semantic match for all types.  In particular, programmers don't generally think of integers as objects.  If the number '1' was allocated at two different places, programmers generally want to consider those two items equal, and certainly don't want updates to one of those instances affecting the other.  In fact, a broad class of programming languages called `functional languages' avoid object identity and reference semantics altogether.

While it is possible to have a "pure" object oriented system, where everything (including integers) is an object (Smalltalk-80 does this), a certain amount of implementation "gymnastics" is necessary to undo this uniformity to get an efficient implementation.  Other languages (Perl, Java, JavaScript) take a pragmatic view and treat some types (like integers) by value, and others by reference.  The CLR also chose a mixed model, but unlike the others, allowed user-defined value types.

The key characteristics of value types are:

1. Each local variable, field, or array element of a value type has a distinct copy of the data in the value.
2. When one variable, field or array element is assigned to another, the value is copied.
3. Equality is always defined only in terms of the data in the variable (not its location).
4. Each value type also has a corresponding reference type which has only one implicit, unnamed field.  This is called its boxed value.  Boxed value types can participate in inheritance and have object identity (although using the object identity of a boxed value type is strongly discouraged).

Value types very closely model the C (and C++) notion of a struct (or C++ class).  Like C you can have pointers to value types, but the pointers are a type distinct from the type of the struct.

### Exceptions

Another high-level programming construct that the CLR directly supports is exceptions.  Exceptions are a language feature that allows programmers to _throw_ an arbitrary object at the point that a failure occurs.  When an object is thrown, the runtime searches the call stack for a method that declares that it can _catch_ the exception.  If such a catch declaration is found, execution continues from that point.  The usefulness of exceptions is that they avoid the very common mistake of not checking if a called method fails.  Given that exceptions help avoid programmer mistakes (thus making programming easier), it is not surprising that the CLR supports them.

As an aside, while exceptions avoid one common error (not checking for failure), they do not prevent another (restoring data structures to a consistent state in the event of a failure).  This means that after an exception is caught, it is difficult in general to know if continuing execution will cause additional errors (caused by the first failure).  This is an area where the CLR is likely to add value in the future.  Even as currently implemented, however, exceptions are a great step forward (we just need to go further).

### Parameterized Types (Generics)

Previous to version 2.0 of the CLR, the only parameterized types were arrays.  All other containers (such as hash tables, lists, queues, etc.), all operated on a generic Object type.  The inability to create List<ElemT>, or Dictionary<KeyT, ValueT> certainly had a negative performance effect because value types needed to be boxed on entry to a collection, and explicit casting was needed on element fetch.  Nevertheless, that is not the overriding reason for adding parameterized types to the CLR.  The main reason is that **parameterized types make programming easier**.

The reason for this is subtle.  The easiest way to see the effect is to imagine what a class library would look like if all types were replaced with a generic Object type.  This effect is not unlike what happens in dynamically typed languages like JavaScript.  In such a world, there are simply far more ways for a programmer to make incorrect (but type-safe) programs.  Is the parameter for that method supposed to be a list? a string? an integer? any of the above? It is no longer obvious from looking at the method's signature.  Worse, when a method returns an Object, what other methods can accept it as a parameter? Typical frameworks have hundreds of methods; if they all take parameters of type Object, it becomes very difficult to determine which Object instances are valid for the operations the method will perform.  In short, strong typing helps a programmer express their intent more clearly, and allows tools (e.g., the compiler) to enforce their intent.  This results in a big productivity boost.

These benefits do not disappear just because the type gets put into a List or a Dictionary, so clearly parameterized types have value.  The only real question is whether parameterized types are best thought of as a language specific feature which is "compiled out" by the time CIL is generated, or whether this feature should have first class support in the runtime.  Either implementation is certainly possible.  The CLR team chose first class support because without it, parameterized types would be implemented different ways by different languages.  This would imply that interoperability would be cumbersome at best.  In addition, expressing programmer intent for parameterized types is most valuable _at the interface_ of a class library.  If the CLR did not officially support parameterized types, then class libraries could not use them, and an important usability feature would be lost.

### Programs as Data (Reflection APIs)

The fundamentals of the CLR are garbage collection, type safety, and high-level language features.  These basic characteristics forced the specification of the program (the CIL) to be fairly high level.  Once this data existed at run time (something not true for C or C++ programs), it became obvious that it would also be valuable to expose this rich data to end programmers.  This idea resulted in the creation of the System.Reflection interfaces (so-called because they allow the program to look at (reflect upon) itself).  This interface allows you to explore almost all aspects of a program (what types it has, the inheritance relationship, and what methods and fields are present).  In fact, so little information is lost that very good "decompilers" for managed code are possible (e.g., [NET Reflector](http://www.red-gate.com/products/reflector/)).  While those concerned with intellectual property protection are aghast at this capability (which can be fixed by purposefully destroying information through an operation called _obfuscating_ the program), the fact that it is possible is a testament to the richness of the information available at run time in managed code.

In addition to simply inspecting programs at run time, it is also possible to perform operations on them (e.g., invoke methods, set fields, etc.), and perhaps most powerfully, to generate code from scratch at run time (System.Reflection.Emit).  In fact, the runtime libraries use this capability to create specialized code for matching strings (System.Text.RegularExpressions), and to generate code for "serializing" objects to store in a file or send across the network.  Capabilities like this were simply infeasible before (you would have to write a compiler!) but thanks to the runtime, are well within reach of many more programming problems.

While reflection capabilities are indeed powerful, that power should be used with care.  Reflection is usually significantly slower than its statically compiled counterparts.  More importantly, self-referential systems are inherently harder to understand.  This means that powerful features such as Reflection or Reflection.Emit should only be used when the value is clear and substantial.

# Other Features

The last grouping of runtime features are those that are not related to the fundamental architecture of the CLR (GC, type safety, high-level specification), but nevertheless fill important needs of any complete runtime system.

## Interoperation with Unmanaged Code

Managed code needs to be able to use functionality implemented in unmanaged code.  There are two main "flavors" of interoperation.  First is the ability simply to call unmanaged functions (this is called Platform Invoke or PINVOKE).  Unmanaged code also has an object-oriented model of interoperation called COM (component object model) which has more structure than ad hoc method calls.  Since both COM and the CLR have models for objects and other conventions (how errors are handled, lifetime of objects, etc.), the CLR can do a better job interoperating with COM code if it has special support.

## Ahead of time Compilation

In the CLR model, managed code is distributed as CIL, not native code.  Translation to native code occurs at run time.  As an optimization, the native code that is generated from the CIL can be saved in a file using a tool called crossgen (similar to .NET Framework NGEN tool).  This avoids large amounts of compilation time at run time and is very important because the class library is so large.

## Threading

The CLR fully anticipated the need to support multi-threaded programs in managed code.  From the start, the CLR libraries contained the System.Threading.Thread class which is a 1-to-1 wrapper over the operating system notion of a thread of execution.  However, because it is just a wrapper over the operating system thread, creating a System.Threading.Thread is relatively expensive (it takes milliseconds to start).  While this is fine for many operations, one style of programming creates very small work items (taking only tens of milliseconds).  This is very common in server code (e.g., each task is serving just one web page) or in code that tries to take advantage of multi-processors (e.g., a multi-core sort algorithm).  To support this, the CLR has the notion of a ThreadPool which allows WorkItems to be queued.  In this scheme, the CLR is responsible for creating the necessary threads to do the work.  While the CLR does expose the ThreadPool directly as the System.Threading.Threadpool class, the preferred mechanism is to use the [Task Parallel Library](https://msdn.microsoft.com/en-us/library/dd460717(v=vs.110).aspx), which adds additional support for very common forms of concurrency control.

From an implementation perspective, the important innovation of the ThreadPool is that it is responsible for ensuring that the optimal number of threads are used to dispatch the work.  The CLR does this using a feedback system where it monitors the throughput rate and the number of threads and adjusts the number of threads to maximize the throughput.  This is very nice because now programmers can think mostly in terms of "exposing parallelism" (that is, creating work items), rather than the more subtle question of determining the right amount of parallelism (which depends on the workload and the hardware on which the program is run).

# Summary and Resources

Phew!  The runtime does a lot! It has taken many pages just to describe _some_ of the features of the runtime, without even starting to talk about internal details.  The hope is, however, that this introduction will provide a useful framework for a deeper understanding of those internal details.  The basic outline of this framework is:

- The Runtime is a complete framework for supporting programming languages
- The Runtime's goal is to make programming easy.
- The Fundamental features of the runtime are:
  - Garbage Collection
  - Memory and Type Safety
  - Support for High-Level Language Features

## Useful Links

- [MSDN Entry for the CLR][clr]
- [Wikipedia Entry for the CLR](http://en.wikipedia.org/wiki/Common_Language_Runtime)
- [ECMA Standard for the Common Language Infrastructure (CLI)][ecma-spec]
- [.NET Framework Design Guidelines](http://msdn.microsoft.com/en-us/library/ms229042.aspx)
- [CoreCLR Repo Documentation](README.md)

[clr]: http://msdn.microsoft.com/library/8bs2ecf4.aspx
[ecma-spec]: ../../../project/dotnet-standards.md
[cil-spec]: http://download.microsoft.com/download/7/3/3/733AD403-90B2-4064-A81E-01035A7FE13C/MS%20Partition%20III.pdf
[fx-design-guidelines]: http://msdn.microsoft.com/en-us/library/ms229042.aspx
