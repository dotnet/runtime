# Exception Handling

Exception Handling In the Mono Runtime
--------------------------------------

### Introduction

There are many types of exceptions which the runtime needs to handle. These are:

-   exceptions thrown from managed code using the 'throw' or 'rethrow' CIL instructions.

-   exceptions thrown by some IL instructions like InvalidCastException thrown by the 'castclass' CIL instruction.

-   exceptions thrown by runtime code

-   synchronous signals received while in managed code

-   synchronous signals received while in native code

-   asynchronous signals

Since exception handling is very arch dependent, parts of the exception handling code reside in the arch specific exceptions-\<ARCH\>.c files. The architecture independent parts are in mini-exceptions.c. The different exception types listed above are generated in different parts of the runtime, but ultimately, they all end up in the mono_handle_exception () function in mini-exceptions.c.

### Exceptions throw programmatically from managed code

These exceptions are thrown from managed code using 'throw' or 'rethrow' CIL instructions. The JIT compiler will translate them to a call to a helper function called 'mono_arch_throw/rethrow_exception'.

These helper functions do not exist at compile time, they are created dynamically at run time by the code in the exceptions-\<ARCH\>.c files.

They perform various stack manipulation magic, then call a helper function usually named throw_exception (), which does further processing in C code, then calls mono_handle_exception() to do the rest.

### Exceptions thrown implicitly from managed code

These exceptions are thrown by some IL instructions when something goes wrong. When the JIT needs to throw such an exception, it emits a forward conditional branch and remembers its position, along with the exception which needs to be emitted. This is usually done in macros named EMIT_COND_SYSTEM_EXCEPTION in the mini-\<ARCH\>.c files.

After the machine code for the method is emitted, the JIT calls the arch dependent mono_arch_emit_exceptions () function which will add the exception throwing code to the end of the method, and patches up the previous forward branches so they will point to this code.

This has the advantage that the rarely-executed exception throwing code is kept separate from the method body, leading to better icache performance.

The exception throwing code braches to the dynamically generated mono_arch_throw_corlib_exception helper function, which will create the proper exception object, does some stack manipulation, then calls throw_exception ().

### Exceptions thrown by runtime code

These exceptions are usually thrown by the implementations of InternalCalls (icalls). First an appropriate exception object is created with the help of various helper functions in metadata/exception.c, which has a separate helper function for allocating each kind of exception object used by the runtime code. Then the mono_raise_exception () function is called to actually throw the exception. That function never returns.

An example:

       if (something_is_wrong)
          mono_raise_exception (mono_get_exception_index_out_of_range ());

mono_raise_exception () simply passes the exception to the JIT side through an API, where it will be received by the helper created by mono_arch_throw_exception (). From now on, it is treated as an exception thrown from managed code.

### Synchronous signals

For performance reasons, the runtime does not do same checks required by the CLI spec. Instead, it relies on the CPU to do them. The two main checks which are omitted are null-pointer checks, and arithmetic checks. When a null pointer is dereferenced by JITted code, the CPU will notify the kernel through an interrupt, and the kernel will send a SIGSEGV signal to the process. The runtime installs a signal handler for SIGSEGV, which is sigsegv_signal_handler () in mini.c. The signal handler creates the appropriate exception object and calls mono_handle_exception () with it. Arithmetic exceptions like division by zero are handled similarly.

### Synchronous signals in native code

Receiving a signal such as SIGSEGV while in native code means something very bad has happened. Because of this, the runtime will abort after trying to print a managed plus a native stack trace. The logic is in the mono_handle_native_sigsegv () function.

Note that there are two kinds of native code which can be the source of the signal:

-   code inside the runtime
-   code inside a native library loaded by an application, ie. libgtk+

### Stack overflow checking

Stack overflow exceptions need special handling. When a thread overflows its stack, the kernel sends it a normal SIGSEGV signal, but the signal handler tries to execute on the same stack as the thread leading to a further SIGSEGV which will terminate the thread. A solution is to use an alternative signal stack supported by UNIX operating systems through the sigaltstack (2) system call. When a thread starts up, the runtime will install an altstack using the mono_setup_altstack () function in mini-exceptions.c. When a SIGSEGV is received, the signal handler checks whenever the fault address is near the bottom of the threads normal stack. If it is, a StackOverflowException is created instead of a NullPointerException. This exception is handled like any other exception, with some minor differences.

There are two reasons why sigaltstack is disabled by default:

-   The main problem with sigaltstack() is that the stack employed by it is not visible to the GC and it is possible that the GC will miss it.

-   Working sigaltstack support is very much os/kernel/libc dependent, so it is disabled by default.

### Asynchronous signals

Async signals are used by the runtime to notify a thread that it needs to change its state somehow. Currently, it is used for implementing thread abort/suspend/resume.

Handling async signals correctly is a very hard problem, since the receiving thread can be in basically any state upon receipt of the signal. It can execute managed code, native code, it can hold various managed/native locks, or it can be in a process of acquiring them, it can be starting up, shutting down etc. Most of the C APIs used by the runtime are not asynch-signal safe, meaning it is not safe to call them from an async signal handler. In particular, the pthread locking functions are not async-safe, so if a signal handler interrupted code which was in the process of acquiring a lock, and the signal handler tries to acquire a lock, the thread will deadlock.

When receiving an async signal, the signal handler first tries to determine whenever the thread was executing managed code when it was interrupted. If it did, then it is safe to interrupt it, so a ThreadAbortException is constructed and thrown. If the thread was executing native code, then it is generally not safe to interrupt it. In this case, the runtime sets a flag then returns from the signal handler. That flag is checked every time the runtime returns from native code to managed code, and the exception is thrown then. Also, a platform specific mechanism is used to cause the thread to interrupt any blocking operation it might be doing.

The async signal handler is in sigusr1_signal_handler () in mini.c, while the logic which determines whenever an exception is safe to be thrown is in mono_thread_request_interruption ().

### Stack unwinding during exception handling

The execution state of a thread during exception handling is stored in an arch-specific structure called MonoContext. This structure contains the values of all the CPU registers relevant during exception handling, which usually means:

-   IP (instruction pointer)
-   SP (stack pointer)
-   FP (frame pointer)
-   callee saved registers

Callee saved registers are the registers which are required by any procedure to be saved/restored before/after using them. They are usually defined by each platforms ABI (Application Binary Interface). For example, on x86, they are EBX, ESI and EDI.

The code which calls mono_handle_exception () is required to construct the initial MonoContext. How this is done depends on the caller. For exceptions thrown from managed code, the mono_arch_throw_exception helper function saves the values of the required registers and passes them to throw_exception (), which will save them in the MonoContext structure. For exceptions thrown from signal handlers, the MonoContext stucture is initialized from the signal info received from the kernel.

During exception handling, the runtime needs to 'unwind' the stack, i.e. given the state of the thread at a stack frame, construct the state at its callers. Since this is platform specific, it is done by a platform specific function called mono_arch_find_jit_info ().

Two kinds of stack frames need handling:

-   Managed frames are easier. The JIT will store some information about each managed method, like which callee-saved registers it uses. Based on this information, mono_arch_find_jit_info () can find the values of the registers on the thread stack, and restore them. On some platforms, the runtime now uses a generic unwinder based on the [DWARF unwinding interface](http://dwarfstd.org/Dwarf3.pdf). The generic unwinder is in the files unwind.h/unwind.c.

-   Native frames are problematic, since we have no information about how to unwind through them. Some compilers generate unwind information for code, some don't. Also, there is no general purpose library to obtain and decode this unwind information. So the runtime uses a different solution. When managed code needs to call into native code, it does through a managed-\>native wrapper function, which is generated by the JIT. This function is responsible for saving the machine state into a per-thread structure called MonoLMF (Last Managed Frame). These LMF structures are stored on the threads stack, and are linked together using one of their fields. When the unwinder encounters a native frame, it simply pops one entry of the LMF 'stack', and uses it to restore the frame state to the moment before control passed to native code. In effect, all successive native frames are skipped together.

### Problems/future work

#### Raising exceptions from native code

Currently, exceptions are raised by calling mono_raise_exception () in the middle of runtime code. This has two problems:

-   No cleanup is done, ie. if the caller of the function which throws an exception has taken locks, or allocated memory, that is not cleaned up. For this reason, it is only safe to call mono_raise_exception () 'very close' to managed code, ie. in the icall functions themselves.

-   To allow mono_raise_exception () to unwind through native code, we need to save the LMF structures which can add a lot of overhead even in the common case when no exception is thrown. So this is not zero-cost exception handling.

An alternative might be to use a JNI style set-pending-exception API. Runtime code could call mono_set_pending_exception (), then return to its caller with an error indication allowing the caller to clean up. When execution returns to managed code, then managed-\>native wrapper could check whenever there is a pending exception and throw it if necessary. Since we already check for pending thread interruption, this would have no overhead, allowing us to drop the LMF saving/restoring code, or significant parts of it.

### libunwind

There is an OSS project called libunwind which is a standalone stack unwinding library. It is currently in development, but it is used by default by gcc on ia64 for its stack unwinding. The mono runtime also uses it on ia64. It has several advantages in relation to our current unwinding code:

-   it has a platform independent API, i.e. the same unwinding code can be used on multiple platforms.

-   it can generate unwind tables which are correct at every instruction, i.e. can be used for unwinding from async signals.

-   given sufficient unwind info generated by a C compiler, it can unwind through C code.

-   most of its API is async-safe

-   it implements the gcc C++ exception handling API, so in theory it can be used to implement mixed-language exception handling (i.e. C++ exception caught in mono, mono exception caught in C++).

-   it is MIT licensed

The biggest problem with libuwind is its platform support. ia64 support is complete/well tested, while support for other platforms is missing/incomplete.

[http://www.hpl.hp.com/research/linux/libunwind/](http://www.hpl.hp.com/research/linux/libunwind/)

### Architecture specific functions for EH

This section contains documentation for the architecture specific functions which are needed to be implemented by each backend. These functions usually reside in the exceptions-\<ARCH\>.c file.

#### mono_arch_handle_exception ()

Prototype:

``` bash
gboolean
mono_arch_handle_exception (void *ctx, gpointer obj);
```

This function is called by signal handlers. It receives the machine state as passed to the signal handlers in he CTX argument. On unix, this is an uncontext_t structure, It also receives the exception object in OBJ, which might be null. Handling exceptions in signal handlers is problematic for many reasons, so this function should set up CTX so when the signal handler returns, execution continues in another runtime function which does the real work. CTX/OBJ needs to be passed to that function. The former can be passed in TLS, while the later has to be passed in registers/on the stack (by modifying CTX), since TLS storage might not be GC tracked.

[Original version of this document in git.](https://github.com/mono/mono/blob/2279f440996923ac66a6ea85cf101d89615aad69/docs/exception-handling.txt)

#### mono_arch_get_restore_context ()

Prototype:

``` bash
gpointer
mono_arch_get_restore_context (MonoTrampInfo **info, gboolean aot);
```

This function should return a trampoline with the following signature:

``` bash
void restore_context (MonoContext *ctx);
```

The trampoline should set the machine state to the state in CTX, then jump to the PC in CTX. Only a subset of the state needs to be restored, i.e. the callee saved registers/sp/fp.

#### mono_arch_get_call_filter ()

Prototype:

``` bash
gpointer
mono_arch_get_call_filter (MonoTrampInfo **info, gboolean aot)
```

This function should return a trampoline with the following signature:

``` bash
int call_filter (MonoContext *ctx, gpointer addr);
```

This trampoline is used to call finally and filter clauses during exception handling. It should setup a new stack frame, save callee saved registers there, restore the same registers from CTX, then make a call to ADDR, restore the saved registers, and return the result returned by the call as its result. Finally clauses need access to the method state, but they need to make calls etc too, so they execute in a nonstandard stack frame, where FP points to the original FP of the method frame, while SP is normal, i.e. it is below the frame created by call_filter (). This means that call_filter () needs to load FP from CTX, but it shouldn't load SP.
