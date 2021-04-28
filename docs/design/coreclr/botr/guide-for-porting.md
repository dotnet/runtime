Guide for porting .NET to a new processor architecture
========================

This document is broken up into 2 major sections.

1.  The various porting stages of porting the .NET Runtime

2.  A technical discussion of the major components affected by a port to a new
    architecture

Porting stages and steps
========================

Porting the .NET Runtime to a new architecture typically follows along the
following path.

As engineering continues along the development path, it is best if the logic can
be placed into the main branch of the runtime as soon as possible. This
will have 2 major effects.

1.  Individual commits are easier to review.

2.  Not all approaches for fixing problems will always be considered acceptable.
    It is plausible that a change may not ever be acceptable to take into
    the upstream git repo, and discovering such issues early can avoid large
    amounts of sunk cost.

3.  When some change is made which breaks other platforms, it will be relatively
    simple to identify the break. If changes are held until after all changes
    are complete and the product is fully functional, this work is likely to be
    much more difficult.

Stage 1 Initial Bring Up
------------------------

Porting .NET to a new platform starts with porting CoreCLR to a new
architecture.

The process follows the following strategy

-   Add a new target architecture to the build environment, and make it build.

-   Determine if there is sufficient incentive to bring up the interpreter, or
    if simply making the jit handle the new architecture is cheaper. The
    interpreter in the CLR is currently only used for bring up scenarios, and is
    not maintained as generally working. It is expected that the interpreter
    will take 1-2 months to enable for an engineer familiar with the CoreCLR
    codebase. A functional interpreter allows the porting team to have a set of
    engineers which focus exclusively on the JIT and a set which focusses on the
    VM portion of the runtime.

-   Build up a set of scripts that will run the coreclr tests. The normal
    routine for running coreclr tests is XUnit, which is only suitable once the
    framework is mostly functional. These scripts will evolve during the
    development effort to support ever increasing needs of development. This set
    of scripts will be expected to do the following tasks.

    -   Run a subset of the tests. Tests are arranged in a directory structure
        by category, so this subsetting mechanism will only need to be a
        directory structure system.

    -   Some set of tests will need to be excluded on a test by test basis. Once
        the product is ready to ship, most of these disabled tests will need to
        have been re-enabled, but there are tests which will be disabled for
        months/years as the product is brought up to quality.

    -   Produce crash or core dumps. The failure mode of many tests during this
        phase will be a crash. A test running tool that captures core dumps will
        make these issues easier to diagnose.

    -   Produce bucketized lists of failures. Generally the approach is to group
        by assertion, and if there is a crash, group by callstack of crash.

-   The first test category to focus on is the JIT category, to bring up the
    general ability to run .NET code. Most of these tests are very simple, but
    getting some code to work is a prerequisite for handling more complex
    scenarios. When doing initial bringup, configuring the Gen0 budget of the GC
    to be a large number so that the GC does not attempt to run during most
    tests is very useful. (Set `COMPlus_GCgen0size=99999999`)

-   Once basic code is executing, the focus shifts to enabling the GC to work.
    In this initial phase, the correct choice is to enable conservative GC
    tracking via the `FEATURE_CONSERVATIVE_GC` macro. This feature will make
    garbage collection largely function correctly, but it is not suitable for
    production use of .NET, and can under certain circumstances trigger
    unbounded memory use.

-   Once basic GC works, and basic JIT functionality is present, work can fan
    out into all of the various features of the runtime. Of particular interest
    to engineers porting the runtime are the EH, stackwalking, and interop
    portions of the test suite.

-   During this phase, porting the SOS plugin from the
    <https://github.com/dotnet/diagnostics> will be very useful. The various
    commands available via that tool such as dumpmt, dumpdomain and such are
    regularly useful to developers attempting to port the runtime.

Stage 2 Expand scenario coverage
--------------------------------

-   Once the coreclr tests are largely passing, the next step is to enable
    XUnit. At this time the clr is probably mostly capable of running XUnit
    tests, and adding testing using the libraries tests will require XUnit to
    work well.

-   Once XUnit is functional, bring up the libraries set of tests. There
    is quite a lot of the CoreCLR codebase that is largely only tested by the
    libraries test suites.

-   Engineers should also begin to attempt real scenario tests at this point,
    such as ASP.NET Core applications. If the libraries test suites work, then
    ASP.NET Core should as well.

Stage 3 Focus on performance
----------------------------

-   Throughput performance at this time is likely to be not that great. There
    are three major opportunities to improve performance at this stage.

    -   Replace conservative GC with precise GC.

    -   Tune the assembly stubs to be high performance on the platform,
        and implement optional assembly stubs where hand-written assembly would
        be faster than the equivalent C++ code.

    -   Improve the code generated by the JIT.

-   Up until this point, engineers have probably been using the JIT for all code
    instead of bringing the Ready To Run compiler (crossgen/crossgen2) into
    usage on the platform. Implementing the ahead of the time compiler starts to
    be useful at this time to improve startup performance.

Stage 4 Focus on stress
-----------------------

-   Stress testing the system is necessary to provide confidence that the system
    really works.

-   See the various test passes done in CI, but most critically GCStress testing
    is needed. See documentation around use of the ComPlus_GCStress environment
    variable.

Stage 5 productization
----------------------

-   Productization is about making the runtime able to run shipped effectively
    on a platform.

-   This document does not attempt to list out the work here as it is largely
    specific to the platform in use and the opinions of numerous stakeholders.

Design issues
=============

These large architecture specific design issues will have substantial impact on
both the JIT and VM.

1.  Calling convention rules – Caller pop vs Callee pop, HFA arguments,
    structure argument passing rules, etc. CoreCLR is designed to utilize a
    broadly similar ABI to the OS api. Managed to managed calls typically have a
    small set of tweaks or extensions to the ABI for VM efficiency purposes, but it
    is generally intended that the ABI of managed code and the ABI of native
    code are very similar. (This is not a hard requirement, and on Windows X86
    the runtime supports a managed to managed abi as well as 3 separate native
    abis for interop, but this scheme is generally not recommended.) See the
    [CLR-ABI](clr-abi.md) document for how the existing architectures work.
    Ensure that the CLR-ABI document is updated with all the requisite details
    and special cases of the new platform. When defining the behavior of a new
    processor architecture abi for CoreCLR, we must maintain that:

    1.  The `this` pointer is always passed in the same register regardless of
        other parameters.

    2.  Various stub types will require an extra "secret" parameter. Perf
        details typically drive exactly where these are placed.

    3.  When executing managed code it must be possible to hijack the return
        address. Current implementations require that the return address always
        be on the stack to do so, although this is a known performance
        deficiency for RISC platforms on arm64.

2.  Architecture specific relocation information (to represent generation of
    relocations for use by load, store, jmp and call instructions) See
    <https://docs.microsoft.com/en-us/windows/win32/debug/pe-format#coff-relocations-object-only>
    for the sort of details that need to be defined.

3.  Behavior and accessibility of processor single step features from within a
    process. On Unix the CLR debugger uses an in process thread to single step
    through functions.

4.  Unwind information. CoreCLR uses Windows style unwind data internally, even
    on Unix platforms. A Windows style unwind structure must be defined. In
    addition, it is possible to enable generation of DWARF data for exposure
    through the GDB JIT
    <https://sourceware.org/gdb/onlinedocs/gdb/JIT-Interface.html> . This
    support is conditional on an \#ifdef, but has been used in the past to
    support bring up of new platforms.

5.  EH Funclets. .NET requires a 2 pass exception model in order to properly
    support exception filters. This substantially differs from the typical
    Itanium ABI model which is used on most Linux architectures

6.  OS behavior with Signals. Especially exactly where the reported instruction
    pointer is located.

7.  Little vs big endian. While .NET runtimes have been ported to big endian in
    the past (notable examples include Mono support various game consoles, and
    POWER, and XNA support on Xbox360) there are no current ports of CoreCLR to
    a big endian platform.

Components of the Runtime affected by a port to a new architecture
==================================================================

This a list of the notable architecture specific components of the .NET runtime.
The list is not complete, but covers most of the areas where work will need to
be done.

Notable components

1.  The JIT. The jit maintains the largest concentration of architecture
    specific logic in the stack. This is not surprising. See [Porting RyuJit](porting-ryujit.md)
    for guidance.

2.  The CLR PAL. When porting to a non-Windows OS, the PAL will be the first component
    that needs to be ported.

3.  The CLR VM. The VM is a mix of completely architecture neutral logic, and
    very machine specific paths.

4.  The unwinder. The unwinder is used to unwind stacks on non-Windows platforms.
    It is located in https://github.com/dotnet/runtime/tree/main/src/coreclr/unwinder.

4.  System.Private.CoreLib/System.Reflection. There is little to no architecture
    specific work here that is necessary for bringup. Nice-to-have work involves
    adding support for the architecture in the
    System.Reflection.ImageFileMachine enum, and the ProcessorArchitecture enum,
    and logic that manipulates it.

5.  PE File format changes to add a new architecture. Also, the C# compiler likely
    also needs a new switch to generate machine specific code for the new
    architecture.

6.  Crossgen/Crossgen2 - As the AOT compilers that produce machine specific logic
    from general purpose MSIL, these will be needed to improve startup performance.

7.  R2RDump - This allows diagnosing issues in pre-compiled code.

8.  coredistools - Necessary for GCStress (if determining instruction boundaries is
    non-trivial), as well as for SuperPMI asm diffs for JIT development.

9.  debug and diagnostics components - The managed debugger and profiler are beyond
    the scope of this document.

CLR PAL
------
The PAL provides a similar to Win32 api as the CLR codebase was originally designed
to run on Windows platforms. Mostly the PAL is concerned with OS independence, but
there are also architecture specific components.

1. pal.h - Contains architecture specific details for handling unwinding scenarios
    such as `CONTEXT` / `_KNONVOLATILE_CONTEXT_POINTERS`/ `_RUNTIME_FUNCTION`.

2. Unwinding support in `seh-unwind.cpp`

3. context.cpp - Which manipulates and captures register contexts

4. jitsupport.cpp - Depending on how the features of the CPU are exposed, there
   may need to be code to call OS apis to gather information about CPU features.

5. pal arch directory - https://github.com/dotnet/runtime/tree/main/src/coreclr/pal/src/arch
   This directory primarily contains assembly stubs for architecture specific
   handling of signals and exceptions.

In addition to the PAL source code, there is a comprehensive set of PAL tests located
in https://github.com/dotnet/runtime/tree/main/src/coreclr/pal/tests.

CLR VM
------

The VM support for architecture specific logic is encoded in a variety of
different ways.

1.  Entirely architecture specific components. These are held in an architecture
    specific folder.

2.  Features which are only enabled on certain architectures. E.g. `FEATURE_HFA`.

3.  Ad-hoc \#if blocks used for specific architectures. As needed these are
    added. The general goal is to keep these to a minimum, but difficulty here
    is primarily driven by what special behavior the processor architecture
    requires.

My recommendation would be to look at how Arm64 is implemented in the VM for the
most up to date model of how to implement a CPU architecture.

### Architecture Specific Components

There are a variety of architecture specific components that all architectures
must implement.

1.  Assembly Stubs

2.  `cgencpu.h` (CPU specific header defining stubs and miscellaneous other CPU
    specific details.)

3.  VSD call stub generation (virtualcallstubcpu.hpp and associated logic)

4.  Precode/Prestub/Jumpstub generation

5.  `callingconventions.h`/`argdestination.h` Provides an implementation of the ABI used by VM
    components. The implementation made architecture specific via a long series of
    C preprocessor macros.

6. `gcinfodecoder.h` The GC info format is archictecture specific as it holds
   information about which specific registers hold GC data. The implementation
   is generally simplified to be defined in terms of register numbers, but if
   the architecture has more registers available for use than existing architectures
   then the format will need extension.

#### Assembly Stubs

There are many reasons for which the runtime requires various assembly stubs.
Here is an annotated list of the stubs implemented for Unix on Arm64.

1.  Only Performance. Some stubs have alternative implementations in C++ code
    which are used if there isn't an assembly stub. As compilers have gotten
    better, it has become more reasonable to just use the C++ versions. Often
    the biggest performance cost/win is due to fast paths being written that do
    not require setting up a stack frame. Most of the casting helpers fall in
    this category.

    1.  `JIT_Stelem_Ref` – very slightly faster version of
        `JIT_Stelem_Ref_Portable`.

2.  General purpose correctness. Some helpers adjust the abi of whatever they
    call in interesting ways, manipulate/parse the "secret" arguments, or do
    other not quite compilable to standardized C concepts.

    1.  `CallDescrWorkerInternal` – Needed to support VM to managed function
        calls. Necessary for all applications as this is how the main method is
        called.

    2.  `LazyMachStateCaptureState`/`HelperMethodFrameRestoreState` – Needed to
        support a GC occurring with an FCALL or HCALL on the stack. (Incorrect
        implementations will cause unpredictable crashes during or after garbage
        collection)

    3.  `NDirectImportThunk` – Needed to support saving off a set of arguments to
        a p/invoke so that the runtime can find the actual target. Also uses one
        of the secret arguments (Used by all p/invoke methods)

    4.  `PrecodeFixupThunk` – Needed to convert the secret argument from a
        FixupPrecode\* to a MethodDesc\*. This function exists to reduce the
        code size of FixupPrecodes as there are (Used by many managed methods)

    5.  `ThePreStub` - Needed to support saving off a set of arguments to the
        stack so that the runtime can find or jit the right target method.
        (Needed for any jitted method to execute Used by all managed methods)

    6.  `ThePreStubPatch` – Exists to provide a reliable spot for the managed
        debugger to put a breakpoint.

    7.  GC Write Barriers – These are used to provide the GC with information
        about what memory is being updated. The existing implementations of
        these are all complex, and there are a number of controls where the
        runtime can adjust to tweak the behavior of the barrier in various ways.
        Some of these adjustments involve modifying the code to inject
        constants, or even wholesale replacements of various bits and pieces. To
        achieve high performance, all of these features must work; however, to
        achieve bringup supporting a simple GC, focus on the case of the single
        heap workstation GC. Additionally, the
        FEATURE_MANUALLY_MANAGED_CARD_BUNDLES and
        FEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP can be implemented as
        performance needs require.

    8.  `ComCallPreStub`/ `COMToCLRDispatchHelper` /`GenericComCallStub` - not
        necessary for non-Windows platforms at this time

    9.  `TheUMEntryPrestub`/ `UMThunkStub` - used to enter the runtime from
        non-managed code through entrypoints generated from the
        Marshal.GetFunctionPointerForDelagate api.

    10. `OnHijackTripThread` - needed for thread suspension to support GC + other
        suspension requiring events. This is typically not needed for very early
        stage bringup of the product, but will be needed for any decent size
        application

    11. `CallEHFunclet` – Used to call catch, finally and fault funclets. Behavior
        is specific to exactly how funclets are implemented. Only used if
        USE_FUNCLET_CALL_HELPER is set

    12. `CallEHFilterFunclet` – Used to call filter funclets. Behavior is specific
        to exactly how funclets are implemented. Only used if
        USE_FUNCLET_CALL_HELPER is set

    13. `ResolveWorkerChainLookupAsmStub`/ `ResolveWorkerAsmStub` Used for virtual
        stub dispatch (virtual call support for interface, and some virtual
        methods). These work in tandem with the logic in virtualcallstubcpu.h to
        implement the logic described in [Virtual Stub Dispatch](virtual-stub-dispatch.md)

    14. `ProfileEnter`/ `ProfileeLeave`/ `ProfileTailcall` – Used to call function
        entry/exit profile functions acquired through the ICorProfiler
        interface. Used in VERY rare circumstances. It is reasonable to wait to
        implement these until the final stages of productization. Most profilers
        do not use this functionality.

    15. `JIT_PInvokeBegin`/`JIT_PInvokeEnd` – Leave/enter the managed runtime state. Necessary
        for ReadyToRun pre-compiled pinvoke calls, so that they do not cause GC
        starvation

    16. `VarargPInvokeStub`/ `GenericPInvokeCalliHelper` Used to support calli
        pinvokes. It is expected that C\# 8.0 will increase use of this feature.
        Today use of this feature on Unix requires hand-written IL. On Windows
        this feature is commonly used by C++/CLI

3.  EH Correctness. Some helpers are written in assembly to provide well known
    locations for NullReferenceExceptions to be generated out of a SIGSEGV
    signal.

    1.  `JIT_MemSet`, and `JIT_MemCpy` have this requirement

#### cgencpu.h

This header is included by various code in the VM directory. It provides a large
set of functionality that is architecture specific, including but not limited to

1.  Defines that are architecture specific specifying the sizes of various data
    structures the VM should create, and such

2.  Defines which specify which of various jit helpers should be replaced with
    asm functions instead of the portable C++ implementations

3.  The CalleeSavedRegisters, ArgumentRegisters, and FloatArgumentRegisters as
    needed to describe the calling convention for the platform

4.  The ClrFlushInstructionCache function. If the architecture doesn't actually
    need to manually flush the icache, then this function is empty.

5.  Various functions for decoding and manipulating jump instructions. These are
    used by various stub routines to predict where code will go, and to produce
    simple jump stubs.

6.  The StubLinkerCpu class for the architecture. Each Architecture defines its
    own StubLinkerCpu api surface and uses it to produce VM generated code.
    There is a small set of apis that are called from general purpose vm code
    (EmitComputedInstantiatingMethodStub, EmitShuffleThunkshared) across
    multiple architectures, and then there are the individual assembly
    instruction emission functions which are architecture specific. The
    StubLinker is used to generate complex stubs, where the set of assembly
    instructions emitted varies from stub to stub.

7.  Various stub data structures. Many very simple stubs are not emitted via an
    emission of a stream of bytes, but instead are exceptionally regular, and
    are effectively the same instructions for each different stub, only with
    slightly different data members. Instead of using the StubLinker mechanism,
    the VM instead has structures that represent the entirety of the stub and
    its associate data, and fill in the assembly instructions and data fields
    with a normal constructor call setting magic numbers. In addition to being
    executable, these stubs are often parsed to determine exactly what a given
    function is, what it is doing, where control flow will lead to, etc.

#### virtualcallstubcpu.h

This header is used to provide implementation of various stubs as used by
virtual stub dispatch. These stubs are the lookup, resolver, and dispatch stubs
as described in [Virtual Stub Dispatch](virtual-stub-dispatch.md). These are
maintained in a separate file from the rest of cgencpu.h for historical reasons,
and for reasons of size (there is quite a lot of logic here.)

System.Private.CoreLib
----------------------

### Initial Bring up

In System.Private.CoreLib there is no work necessary for initial bring up.

### Complete support

Complete support involves changing the publicly visible api surface of the
product. Doing so is a process handled via public issues on GitHub and
discussions with the api review board.

-   Adding support for the architecture to the
    System.Reflection.ImageFileMachine enum, and
    System.Reflection.ProcessorArchitecture enum as well as related logic

-   Adding support for architecture specific intrinsics such as SIMD
    instructions, or other non-standard api surface.
