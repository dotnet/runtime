# Bitcode

## Introduction

Bitcode imposes the following major restrictions:

-   No inline assembly/machine code
-   Compilation using stock clang

To enable the runtime to operate in this environment, a new execution mode 'llvmonly' was implemented. In this mode:

-   everything is compiled to llvm bitcode, then compiled to native code using clang.
-   no trampolines, etc. are used.

In the rest of this document, 'normal mode' is used to refer to the JIT/full aot mode previously supported by the runtime.

## Concepts

### Passing extra arguments

The runtime used trampolines to pass extra arguments to some generic shared methods. This is not possible in llvmonly mode. Instead, these arguments are passed normally as an additional argument, and the caller is responsible for passing them. The method address and the possible additional argument are encapsulated together into a function descriptor represented by a MonoFtnDesc structure. These function descriptors are used instead of method addresses anywhere where a callee might require an extra argument. A call using an ftndesc looks like this:

``` c
ftndesc->addr (<normal args>, ftndesc->arg);
```

The 'arg' field might be null, in which case the caller will pass one more argument than the callee requires, but that is not a problem with most calling conventions.

### Lazy initialization

Trampolines were used in many places in the runtime to initialize/load methods/code on demand. Instead, either the caller or the callee needs to check whenever initialization is required, and call into runtime code to do it.

## Details

### Method initialization

AOT methods require the initialization of GOT slots they are using. In normal execution mode, this was accomplished by calling them through PLT entries. The PLT entry would look up the method code, initialize its GOT slots, then transfer control to it. In llvmonly mode, methods initialize themselves. Every AOT module has an 'inited' bit array with one bit for every method. The method code checks this bit in its prolog, and if its 0, calls a runtime function to initialize the method.

In llvmonly mode, no trampolines are created for methods. Instead, the method's code is looked up immediately. This doesn't create lazy initialization problems because the method is initialized lazily, so looking up its code doesn't change managed state, i.e. it doesn't run type cctors etc.

### Looking up methods

In normal mode, AOT images contained a table mapping method indexes to method addresses. This table was emitted using inline assembly. In llvmonly mode, there is a generated llvm function which does this mapping using a switch statement.

### Unbox trampolines

In normal mode, these were emitted using inline assembly. In llvmonly mode, these are emitted as llvm code. With optimizations enabled, llvm can emit the same or very similar code.

### Null checks

Since the target plaform for bitcode doesn't support sigsegv signal handlers, explicit null checks are emitted.

### Normal calls

Calls are made through a GOT slot, or directly, if the callee is in the same assembly, and its corresponding llvm method can be looked up at compile time.

### Virtual calls

Vtable slots contain ftn descriptors. They are initialized to null when the vtable is created, so the calling code has to initialize them on demand. So a virtual calls looks like this:

``` c
if (vtable [slot] == null)
   init_vtable_slot (vtable, slot);
ftndesc = vtable [slot];
<call using ftndesc>
```

### Interface calls

Interface calls are implemented using IMT. The imt entries in the vtable contain an ftndesc. The ftndesc points to a imt thunk. IMT thunks are C functions implemented in the runtime. They receive the imt method, and a table of `<method, ftndesc>` pairs, and return the ftndesc corresponding to the imt method.

The generated code looks like this:

``` c
imt_ftndesc = vtable [imt_slot];
ftndesc = imt_ftndesc->addr (imt_method, imt_ftndesc->arg);
<call using ftndesc>
```

The imt entries are initialized to point to an 'initial imt thunk', which computes the real imt thunk when first called, and replaces the imt entry to point to the real imt thunk. This means that the generated code doesn't need to check whenever the imt entry is initialized.

### Generic virtual calls

These are handled similarly to interface calls.

### Gsharedvt

There are two kinds of gsharedvt methods: ones with a variable signature, and those without one. A variable signature is a signature which includes parameters/return values whose size is not known at compile time. Gsharedvt methods without variable signatures are handler similarly as in normal mode. Methods with variable signatures are handles as follows: all parameters and returned by ref, even the fixed size ones. I.e., for `T foo<T> (int i, T t)`, both 'i' and 't' are passed by ref, and the result is returned by ref using a hidden argument. So the real signature of the gsharedvt version of foo looks like this:

``` c
void foo (ref T_GSHAREDVT vret, ref int i, ref T_GSHAREDVT t, <rgctx arg>);
```

Calls between normal and gsharedvt methods with a variable signature go though gsharedvt in/out wrappers. These are normal runtime wrappers generated by the runtime as IL code. The AOT compiler collects every possible concrete signature from the program, and generates in/out wrappers for them. Wrappers for similar signatures are shared to decrease the number of required wrappers.

A gsharedvt in wrapper for the method above looks like this (T==int):

``` c
int gsharedvt_in_int_int (int i, int t, ftndesc callee)
{
    int res;

    callee->addr (&res, &i, &t, callee->arg);
    return res;
}
```

While a gsharedvt out wrapper for the same instantiation looks like:

``` c
void gsharedvt_out_int_int (ref int vret, ref int i, ref int t, ftndesc callee)
{
    *vret = callee->addr (*i, *t, callee->arg);
}
```

The last argument to the wrappers is an ftndesc for the method which needs to be called.

### Delegates

In normal mode, delegate trampolines and various small invoke trampolines are used to implement delegate creation/invocation efficiently. In llvmonly mode, we fall back to the normal delegate-invoke wrappers. The delegates need to invoke an ftndesc since the target method can require an extra argument. The 'addr' part of the ftndesc is stored in `MonoDelegate.method_ptr`, and the 'arg' part is stored in `MonoDelegate.extra_arg`. The delegate invoke wrapper uses a special IL opcode called `CEE_MONO_CALLI_EXTRA_ARG` to make the call which takes this into account.

If the target method is gsharedvt, we cannot add an gsharedvt in wrapper around it, since the concrete signature required might not exist at compile time if the delegate is only invoked through a gsharedvt delegate-invoke wrapper. To work around this, we set the lowest bit of `MonoDelegate.extra_arg` to indicate this, and the `CALLI_EXTRA_ARG` opcode generates code which checks at runtime to see which calling conv needs to be used.

### Runtime invoke

Runtime invoke is used to dynamically invoke managed methods. It is implemented using runtime-invoke wrappers, which receive an C array of parameter values, and pass it to a method which is called.

For example, the runtime-invoke wrapper for the `foo<int>` method above looks like:

``` c
void runtime_invoke_int_int (gpointer[] params, gpointer addr, gpointer *exc)
{
    try {
         int ret = addr (params [0], params [1]);
         return box(ret, typeof<int>);
    } catch (Exception ex) {
         *exc = ex;
   }
}
```

There is one runtime invoke wrapper for each possible signature, with some sharing. To cut down on the number of wrappers generated, in normal mode, we use a 'dyn-call' opcode which can support a large number of signatures.

In llvmonly mode, we use the gsharedvt out wrappers which are already generated to support gsharedvt to implement runtime invokes. This is useful because the possible set of signatures for gsharedvt out wrappers is limited since all their arguments are pointers. Instead of invoking the method directly from the runtime-invoke wrapper, we invoke the gsharedvt out wrapper. So the call looks like this: runtime-invoke wrapper -> gsharedvt out wrapper -> target method.
