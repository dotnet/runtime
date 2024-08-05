# Trampolines

Trampolines are small, hand-written pieces of assembly code used to perform various tasks in the mono runtime. They are generated at runtime using the native code generation macros used by the JIT. They usually have a corresponding C function they can fall back to if they need to perform a more complicated task. They can be viewed as ways to pass control from JITted code back to the runtime.

The common code for all architectures is in mini-trampolines.c, this file contains the trampoline creation functions plus the C functions called by the trampolines. The tramp-\<ARCH\>.c files contain the arch-dependent code which creates the trampolines themselves.

Most, but not all trampolines consist of two parts:

-   a generic part containing most of the code. This is created by the mono_arch_create_trampoline_code () function in tramp-\<ARCH\>.c. Generic trampolines can be large (1kb).
-   a specific part whose job is to call the generic part, passing in a parameter. The parameter to pass and the method by it is passed depends on the type of the trampoline. Specific trampolines are created by the mono_arch_create_specific_trampoline () function in tramp-\<ARCH\>.c. Specific trampolines are small, since the runtime creates lots of them.

The generic part saves the machine state to the stack, and calls one of the trampoline functions in mini-trampolines.c with the state, the call site, and the argument passed by the specific trampoline. After the C function returns, it either returns normally, or branches to the address returned by the C function, depending on the trampoline type.

Trampoline types are given by the MonoTrampolineType enumeration in [mini.h](https://github.com/mono/mono/blob/main/mono/mini/mini.h).

The platform specific code for trampolines is in the file tramp-\<ARCH\>.c for each architecture, while the cross platform code is in mini-trampolines.c. There are two types of functions in mini-trampolines.c:

-   The actual C functions called by the trampolines.
-   Functions to create the different trampolines types.

Trampoline creation functions have the following signature:

``` bash
gpointer
mono_arch_create_foo_trampoline (<args>, MonoTrampInfo **info, gboolean aot)
```

The function should return a pointer to the newly created trampoline, allocating memory from either the global code manager, or from a domain's code manager. If INFO is not NULL, it is set to a pointer to a MonoTrampInfo structure, which contains information about the trampoline, like its name, unwind info, etc. This is used for two purposes:

-   Saving the trampoline info an AOT image in 'full-aot' mode.
-   Saving debug info about the trampoline in XDEBUG mode.

### JIT Trampolines

These trampolines are used to JIT compile a method the first time it is called. When the JIT compiles a call instruction, it doesn't compile the called method right away. Instead, it creates a JIT trampoline, and emits a call instruction referencing the trampoline. When the trampoline is called, it calls mono_magic_trampoline () which compiles the target method, and returns the address of the compiled code to the trampoline which branches to it. This process is somewhat slow, so mono_magic_trampoline () tries to patch the calling JITted code so it calls the compiled code instead of the trampoline from now on. This is done by mono_arch_patch_callsite () in tramp-\<ARCH\>.c.

### Virtual Call Trampolines

There is one virtual call trampoline per vtable slot index. The trampoline uses this index plus the 'this' argument which is passed in a fixed register/stack slots by the managed calling convention to obtain the virtual method which needs to be compiled. It then patches the vtable slot with the address of the newly compiled method.

\<TODO IMT\>

### Jump Trampolines

Jump trampolines are very similar to JIT trampolines, they even use the same mono_magic_trampoline () C function. They are used to implement the LDFTN and the JMP IL opcodes.

### Class Init Trampolines

These trampolines are used to implement the type initialization sematics of the CLI spec. They call the mono_class_init_trampoline () C function which executes the class initializer of the class passed as the trampoline argument, then replaces the code calling the class init trampoline with NOPs so it is not executed anymore.

### Generic Class Init Trampoline

This is similar to the class init trampolines, but is used for initalizing classes which are only known at run-time, in generic-shared code. It receives the class to be initialized in a register instead of from a specific trampoline. This means there is only one instance of this trampoline.

### RGCTX Lazy Fetch Trampolines

These are used for fetching values from a runtime generic context, lazily initializing the values if they do not exist yet. There is one instance of this trampoline for each offset value.

### AOT Trampolines

These are similar to the JIT trampolines but instead of receiving a MonoMethod to compile, they receive an image+token pair. If the method identified by this pair is also AOT compiled, the address of its compiled code can be obtained without loading the metadata for the method.

### AOT PLT Trampolines

These trampolines handle calls made from AOT code though the PLT.

### Delegate Trampolines

These trampolines are used to handle the first call made to the delegate though its Invoke method. They call mono_delegate_trampoline () which creates a specialized calling sequence optimized to the delegate instance before calling it. Further calls will go through to this optimized code sequence.

### Monitor Enter/Exit Trampolines

These trampolines implement the fastpath of Monitor.Enter/Exit on some platforms.

\<TODO REMAINING TRAMPOLINE TYPES\>
