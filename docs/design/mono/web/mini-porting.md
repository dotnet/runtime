# Porting the Engine

## Introduction

This documents describes the process of porting the mono JIT to a new CPU architecture. The new mono JIT has been designed to make porting easier though at the same time enable the port to take full advantage from the new architecture features and instructions. Knowledge of the mini architecture (described in the mini-doc.txt file) is a requirement for understanding this guide, as well as an earlier document about porting the mono interpreter (available on the web site).

There are six main areas that a port needs to implement to have a fully-functional JIT for a given architecture:

-   instruction selection
-   native code emission
-   call conventions and register allocation
-   method trampolines
-   exception handling
-   minor helper methods

To take advantage of some not-so-common processor features (for example conditional execution of instructions as may be found on ARM or ia64), it may be needed to develop an high-level optimization, but doing so is not a requirement for getting the JIT to work.

We'll see in more details each of the steps required, note, though, that a new port may just as well start from a cut and paste of an existing port to a similar architecture (for example from x86 to amd64, or from powerpc to sparc).

The architecture specific code is split from the rest of the JIT, for example the x86 specific code and data is all included in the following files in the distribution:

mini-x86.h mini-x86.c inssel-x86.brg cpu-pentium.md tramp-x86.c exceptions-x86.c

I suggest a similar split for other architectures as well.

Note that this document is still incomplete: some sections are only sketched and some are missing, but the important info to get a port going is already described.

## Architecture-specific instructions and instruction selection

The JIT already provides a set of instructions that can be easily mapped to a great variety of different processor instructions. Sometimes it may be necessary or advisable to add a new instruction that represent more closely an instruction in the architecture. Note that a mini instruction can be used to represent also a short sequence of CPU low-level instructions, but note that each instruction represents the minimum amount of code the instruction scheduler will handle (i.e., the scheduler won't schedule the instructions that compose the low-level sequence as individual instructions, but just the whole sequence, as an indivisible block).

New instructions are created by adding a line in the mini-ops.h file, assigning an opcode and a name. To specify the input and output for the instruction, there are two different places, depending on the context in which the instruction gets used.

If an instruction is used as a low-level CPU instruction, the info is specified in a machine description file. The description file is processed by the genmdesc program to provide a data structure that can be easily used from C code to query the needed info about the instruction.

As an example, let's consider the add instruction for both x86 and ppc:

    x86 version:
        add: dest:i src1:i src2:i len:2 clob:1
    ppc version:
        add: dest:i src1:i src2:i len:4

Note that the instruction takes two input integer registers on both CPU, but on x86 the first source register is clobbered (clob:1) and the length in bytes of the instruction differs.

Note that integer adds and floating point adds use different opcodes, unlike the IL language (64 bit add is done with two instructions on 32 bit architectures, using a add that sets the carry and an add with carry).

A specific CPU port may assign any meaning to the clob field for an instruction since the value will be processed in an arch-specific file anyway.

See the top of the existing cpu-pentium.md file for more info on other fields: the info may or may not be applicable to a different CPU, in this latter case the info can be ignored.

So, one of the first things needed in a port is to write a cpu-$(arch).md machine description file and fill it with the needed info. As a start, only a few instructions can be specified, like the ones required to do simple integer operations. The default rules of the instruction selector will emit the common instructions and so we're ready to go for the next step in porting the JIT.

## Native code emission

Since the first step in porting mono to a new CPU is to port the interpreter, there should be already a file that allows the emission of binary native code in a buffer for the architecture. This file should be placed in the

``` bash
   mono/arch/$(arch)/
```

directory.

The bulk of the code emission happens in the mini-$(arch).c file, in a function called `mono_arch_output_basic_block ()`. This function takes a basic block, walks the list of instructions in the block and emits the binary code for each. Optionally a peephole optimization pass is done on the basic block, but this can be left for later, when the port actually works.

This function is very simple, there is just a big switch on the instruction opcode and in the corresponding case the functions or macros to emit the binary native code are used. Note that in this function the lengths of the instructions are used to determine if the buffer for the code needs enlarging.

To complete the code emission for a method, a few other functions need implementing as well:

``` c
  mono_arch_emit_prolog ()
    mono_arch_emit_epilog ()
    mono_arch_patch_code ()
```

`mono_arch_emit_prolog ()` will emit the code to setup the stack frame for a method, optionally call the callbacks used in profiling and tracing, and move the arguments to their home location (in a caller-save register if the variable was allocated to one, or in a stack location if the argument was passed in a volatile register and wasn't allocated a non-volatile one). caller-save registers used by the function are saved in the prolog as well.

`mono_arch_emit_epilog ()` will emit the code needed to return from the function, optionally calling the profiling or tracing callbacks. At this point the basic blocks or the code that was moved out of the normal flow for the function can be emitted as well (this is usually done to provide better info for the static branch predictor). In the epilog, caller-save registers are restored if they were used.

Note that, to help exception handling and stack unwinding, when there is a transition from managed to unmanaged code, some special processing needs to be done (basically, saving all the registers and setting up the links in the Last Managed Frame structure).

When the epilog has been emitted, the upper level code arranges for the buffer of memory that contains the native code to be copied in an area of executable memory and at this point, instructions that use relative addressing need to be patched to have the right offsets: this work is done by `mono_arch_patch_code ()`.

## Call conventions and register allocation

To account for the differences in the call conventions, a few functions need to be implemented.

`mono_arch_allocate_vars ()` assigns to both arguments and local variables the offset relative to the frame register where they are stored, dead variables are simply discarded. The total amount of stack needed is calculated.

`mono_arch_call_opcode ()` is the function that more closely deals with the call convention on a given system. For each argument to a function call, an instruction is created that actually puts the argument where needed, be it the stack or a specific register. This function can also re-arrange th order of evaluation when multiple arguments are involved if needed (like, on x86 arguments are pushed on the stack in reverse order). The function needs to carefully take into accounts platform specific issues, like how structures are returned as well as the differences in size and/or alignment of managed and corresponding unmanaged structures.

The other chunk of code that needs to deal with the call convention and other specifics of a CPU, is the local register allocator, implemented in a function named `mono_arch_local_regalloc ()`. The local allocator deals with a basic block at a time and basically just allocates registers for temporary values during expression evaluation, spilling and unspilling as necessary.

The local allocator needs to take into account clobbering information, both during simple instructions and during function calls and it needs to deal with other architecture-specific weirdnesses, like instructions that take inputs only in specific registers or output only is some.

Some effort will be put later in moving most of the local register allocator to a common file so that the code can be shared more for similar, risc-like CPUs. The register allocator does a first pass on the instructions in a block, collecting liveness information and in a backward pass on the same list performs the actual register allocation, inserting the instructions needed to spill values, if necessary.

The cross-platform local register allocator is now implemented and it is documented in the jit-regalloc file.

When this part of code is implemented, some testing can be done with the generated code for the new architecture. Most helpful is the use of the --regression command line switch to run the regression tests (basic.cs, for example).

Note that the JIT will try to initialize the runtime, but it may not be able yet to compile and execute complex code: commenting most of the code in the `mini_init()` function in mini.c is needed to let the JIT just compile the regression tests. Also, using multiple -v switches on the command line makes the JIT dump an increasing amount of information during compilation.

Values loaded into registers need to be extended as needed by the ECMA specs:

-   integers smaller than 4 bytes are extended to int32 values
-   32 bit floats are extended to double precision (in particular this means that currently all the floating point operations operate on doubles)

## Method trampolines

To get better startup performance, the JIT actually compiles a method only when needed. To achieve this, when a call to a method is compiled, we actually emit a call to a magic trampoline. The magic trampoline is a function written in assembly that invokes the compiler to compile the given method and jumps to the newly compiled code, ensuring the arguments it received are passed correctly to the actual method.

Before jumping to the new code, though, the magic trampoline takes care of patching the call site so that next time the call will go directly to the method instead of the trampoline. How does this all work?

`mono_arch_create_jit_trampoline ()` creates a small function that just preserves the arguments passed to it and adds an additional argument (the method to compile) before calling the generic trampoline. This small function is called the specific trampoline, because it is method-specific (the method to compile is hard-code in the instruction stream).

The generic trampoline saves all the arguments that could get clobbered and calls a C function that will do two things:

-   actually call the JIT to compile the method
-   identify the calling code so that it can be patched to call directly the actual method

If the 'this' argument to a method is a boxed valuetype that is passed to a method that expects just a pointer to the data, an additional unboxing trampoline will need to be inserted as well.

## Exception handling

Exception handling is likely the most difficult part of the port, as it needs to deal with unwinding (both managed and unmanaged code) and calling catch and filter blocks. It also needs to deal with signals, because mono takes advantage of the MMU in the CPU and of the operation system to handle dereferences of the NULL pointer. Some of the function needed to implement the mechanisms are:

`mono_arch_get_throw_exception ()` returns a function that takes an exception object and invokes an arch-specific function that will enter the exception processing. To do so, all the relevant registers need to be saved and passed on.

`mono_arch_handle_exception ()` this function takes the exception thrown and a context that describes the state of the CPU at the time the exception was thrown. The function needs to implement the exception handling mechanism, so it makes a search for an handler for the exception and if none is found, it follows the unhandled exception path (that can print a trace and exit or just abort the current thread). The difficulty here is to unwind the stack correctly, by restoring the register state at each call site in the call chain, calling finally, filters and handler blocks while doing so.

As part of exception handling a couple of internal calls need to be implemented as well.

`ves_icall_get_frame_info ()` returns info about a specific frame.

`mono_jit_walk_stack ()` walks the stack and calls a callback with info for each frame found.

`ves_icall_get_trace ()` return an array of StackFrame objects.

### Code generation for filter/finally handlers

Filter and finally handlers are called from 2 different locations:

-   from within the method containing the exception clauses
-   from the stack unwinding code

To make this possible we implement them like subroutines, ending with a "return" statement. The subroutine does not save the base pointer, because we need access to the local variables of the enclosing method. Its is possible that instructions inside those handlers modify the stack pointer, thus we save the stack pointer at the start of the handler, and restore it at the end. We have to use a "call" instruction to execute such finally handlers.

The MIR code for filter and finally handlers looks like:

       OP_START_HANDLER
       ...
       OP_END_FINALLY | OP_ENDFILTER(reg)

OP_START_HANDLER: should save the stack pointer somewhere OP_END_FINALLY: restores the stack pointers and returns. OP_ENDFILTER (reg): restores the stack pointers and returns the value in "reg".

### Calling finally/filter handlers

There is a special opcode to call those handler, its called OP_CALL_HANDLER. It simple emits a call instruction.

Its a bit more complex to call handler from outside (in the stack unwinding code), because we have to restore the whole context of the method first. After that we simply emit a call instruction to invoke the handler. Its usually possible to use the same code to call filter and finally handlers (see arch_get_call_filter).

### Calling catch handlers

Catch handlers are always called from the stack unwinding code. Unlike finally clauses or filters, catch handler never return. Instead we simply restore the whole context, and restart execution at the catch handler.

### Passing Exception objects to catch handlers and filters

We use a local variable to store exception objects. The stack unwinding code must store the exception object into this variable before calling catch handler or filter.

## Minor helper methods

A few minor helper methods are referenced from the arch-independent code. Some of them are:

`mono_arch_cpu_optimizations ()` This function returns a mask of optimizations that should be enabled for the current CPU and a mask of optimizations that should be excluded, instead.

`mono_arch_regname ()` Returns the name for a numeric register.

`mono_arch_get_allocatable_int_vars ()` Returns a list of variables that can be allocated to the integer registers in the current architecture.

`mono_arch_get_global_int_regs ()` Returns a list of caller-save registers that can be used to allocate variables in the current method.

`mono_arch_instrument_mem_needs ()`

`mono_arch_instrument_prolog ()`

`mono_arch_instrument_epilog ()` Functions needed to implement the profiling interface.

## Testing the port

The JIT has a set of regression tests in \*.cs files inside the mini directory.

The usual method of testing a port is by compiling these tests on another machine with a working runtime by typing 'make rcheck', then copying TestDriver.dll and \*.exe to the mini directory. The tests can be run by typing:

``` bash
   ./mono --regression <exe file name>
```

The suggested order for working through these tests is the following:

-   basic.exe
-   basic-long.exe
-   basic-float.exe
-   basic-calls.exe
-   objects.exe
-   arrays.exe
-   exceptions.exe
-   iltests.exe
-   generics.exe

## Writing regression tests

Regression tests for the JIT should be written for any bug found in the JIT in one of the \*.cs files in the mini directory. Eventually all the operations of the JIT should be tested (including the ones that get selected only when some specific optimization is enabled).

## Platform specific optimizations

An example of a platform-specific optimization is the peephole optimization: we look at a small window of code at a time and we replace one or more instructions with others that perform better for the given architecture or CPU.

## Function descriptors

Some ABIs, like those for IA64 and PPC64, don't use direct function pointers, but so called function descriptors. A function descriptor is a short data structure which contains at least a pointer to the code of the function and a pointer to a GOT/TOC, which needs to be loaded into a specific register prior to jumping to the function. Global variables and large constants are accessed through that register.

Mono does not need function descriptors for the JITted code, but we need to handle them when calling unmanaged code and we need to create them when passing managed code to unmanaged code.

`mono_create_ftnptr()` creates a function descriptor for a piece of generated code within a specific domain.

`mono_get_addr_from_ftnptr()` returns the pointer to the native code in a function descriptor. Never use this function to generate a jump to a function without loading the GOT/TOC register unless the function descriptor was created by `mono_create_ftnptr()`.

See the sources for IA64 and PPC64 on when to create and when to dereference function descriptors. On PPC64 function descriptors for various generated helper functions (in exceptions-ppc.c and tramp-ppc.c) are generated in front of the code they refer to (see `ppc_create_pre_code_ftnptr()`). On IA64 they are created separately.

## Emulated opcodes

Mini has code for emulating quite a few opcodes, most notably operations on longs, int/float conversions and atomic operations. If an architecture wishes such an opcode to be emulated, mini produces icalls instead of those opcodes. This should only be considered when the operation cannot be implemented efficiently and thus the overhead occured by the icall is not relatively large. Emulation of operations is controlled by #defines in the arch header, but the naming is not consistent. They usually start with `MONO_ARCH_EMULATE_`, `MONO_ARCH_NO_EMULATE_` and `MONO_ARCH_HAVE_`.

## Prolog/Epilog

The method prolog is emitted by the mono_arch_emit_prolog () function. It usually consists of the following parts:

-   Allocate frame: set fp to sp, decrement sp.
-   Save callee saved registers to the frame
-   Initialize the LMF structure
-   Link the LMF structure: This implements the following pseudo code:

<!-- -->

     lmf->lmf_addr = mono_get_lmf_addr ()
     lmf->previous_lmf = *(lmf->lmf_addr)
     *(lmf->lmf_addr)->lmf

-   Compute bb->max_offset for each basic block: This enables mono_jit_output_basic_block () to emit short branches where possible.
-   Store the runtime generic context, see the Generic Sharing section.
-   Store the signature cookie used by vararg methods.
-   Transfer arguments to the location they are allocated to, i.e. load arguments received on the stack to registers if needed, and store arguments received in registers to the stack/callee saved registers if needed.
-   Initialize the various variables used by the soft debugger code.
-   Implement tracing support.

The epilog is emitted by the mono_arch_emit_epilog () function. It usually consists of the following parts:

-   Restore the LMF by doing:

<!-- -->

     *(lmf->lmf_addr) = lmf->previous_lmf.

-   Load returned valuetypes into registers if needed.
-   Implement tracing support.
-   Restore callee saved registers.
-   Pop frame.
-   Return to the caller.

Care must be taken during these steps to avoid clobbering the registers holding the return value of the method.

Callee saved registers are either saved to dedicated stack slots, or they are saved into the LMF. The stack slots where various things are saved are allocated by mono_arch_allocate_vars ().

## Delegate Invocation

A delegate is invoked like this by JITted code:

delegate->invoke_impl (delegate, arg1, arg2, arg3, ...)

Here, 'invoke_impl' originally points to a trampoline which ends up calling the 'mono_delegate_trampoline' C function. This function tries to find an architecture specific optimized implementation by calling 'mono_arch_get_delegate_invoke_impl'.

mono_arch_get_delegate_invoke_impl () should return a small trampoline for invoking the delegate which matches the following pseudo code:

-for instance delegates:

delegate->method_ptr (delegate->target, arg1, arg2, arg3, ...)

-   for static delegates:

delegate->method_ptr (arg1, arg2, arg3, ...)

## Varargs

The vararg calling convention is implemented as follows:

### Caller side

-   The caller passes in a 'signature cookie', which is a hidden argument containing a MonoSignature\*.

<!-- -->

     This argument is passed just before the implicit arguments, i.e. if the callee signature is this:
     foo (string format, ...)

and the callee signature is this:

     foo ("%d %d", 1, 2)

then the real callee signature would look like:

     foo ("%d %d", <signature cookie>, 1, 2)

To simplify things, both the sig cookie and the implicit arguments are always passed on the stack and not in registers. mono_arch_emit_call () is responsible for emitting this argument.

### Callee side

-   mono_arch_allocate_vars () is responsible for allocating a local variable slot where the sig cookie will be saved. cfg->sig_cookie should contain the stack offset of the local variable slot.
-   mono_arch_emit_prolog () is responsible for saving the sig cookie argument into the local variable.
-   The implementation of OP_ARGLIST should load the sig cookie from the local variable, and save it into its dreg, which will point to a local variable of type RuntimeArgumentHandle.
-   The fetching of vararg arguments is implemented by icalls in icalls.c.

tests/vararg.exe contains test cases to exercise this functionality.

## Unwind info

On most platforms, the JIT uses DWARF unwind info to unwind the stack during exception handling. The API and some documentation is in the mini-unwind.h file. The mono_arch_emit_prolog () function is required to emit this information using the macros in mini-unwind.h, and the mono_arch_find_jit_info () function needs to pass it to mono_unwind_frame (). In addition to this, the various trampolines might also have unwind info, which makes stack walks possible when using the gdb integration (XDEBUG).

The task of a stack unwinder is to construct the machine state at the caller of the current stack frame, i.e: - find the return address of the caller - find the values of the various callee saved registers in the caller at the point of the call

The DWARF unwinder is based on the concept of a CFA, or Canonical Frame Address. This is an address of the stack frame which does not change during the execution of the method. By convention, the CFA is equal to the value of the stack pointer prior to the instruction which transferred execution to the current method. So for example, on x86, the value of the CFA on enter to the method is esp+4 because of the pushing of the return address. There are two kinds of unwind directives:

-   those that specify how to compute the CFA at any point in the method using a \<reg>+\<offset>
-   those that specify where a given register is saved in relation to the CFA.

For a typical x86 method prolog, the unwind info might look like this:

``` bash
- <cfa=esp+8>
- <return addr at cfa+0>
push ebp
- <ebp saved at cfa-4>
mov ebp, esp
- <cfa=ebp+8>
```

## Generic Sharing

Generic code sharing is optional. See the document on [generic-sharing](/docs/advanced/runtime/docs/generic-sharing/) for information on how to support it on an architecture.

### MONO_ARCH_RGCTX_REG

The MONO_ARCH_RGCTX_REG define should be set to a hardware register which will be used to pass the 'mrgctx' hidden argument to generic shared methods. It should be a caller saved register which is not used in local register allocation. Also, any code which gets executed between the caller and the callee (i.e. trampolines) needs to avoid clobbering this registers. The easiest solution is to set it to the be the same as MONO_ARCH_IMT_REG, since IMT/generic sharing are never used together during a call. The method prolog must save this register to cfg->rgctx_var.

### Static RGCTX trampolines

These trampolines are created by mono_arch_get_static_rgctx_trampoline (). They are used to call generic shared methods indirectly from code which cannot pass an MRGCTX. They should implement the following pseudo code:

    <mrgctx reg> = mrgctx
    jump <method addr>

### Generic Class Init Trampoline

This one of a kind trampoline is created by mono_arch_create_generic_class_init_trampoline (). They are used to run the .cctor of the vtable passed in as an argument in MONO_ARCH_VTABLE_REG. They should implement the following pseudo code:

    vtable = <vtable reg>
    if (!vtable->initialized)
      <call jit icall "specific_trampoline_generic_class_init">

The generic trampoline code needs to be modified to pass the argument received in MONO_ARCH_VTABLE_REG to the C trampoline function, which is mono_generic_class_init_trampoline ().

### RGCTX Lazy Fetch Trampoline

These trampolines are created by mono_arch_create_rgctx_lazy_fetch_trampoline (). They are used for fetching values out of an MonoRuntimeGenericContext, lazily initializing them as needed.
