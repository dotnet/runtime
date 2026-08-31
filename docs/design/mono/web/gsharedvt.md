# Generic sharing for valuetypes

## The problem

In some environments like ios, its not possible to generate native code at runtime. This means that we have to compile all possible methods used by the application at compilation time. For generic methods, this is not always possible, i.e.:

``` c
interface IFace {
    void foo<T> (T t);
}

class Class1 : IFace {
    public virtual void foo<T> (T t) {
      ...
    }
}

IFace o = new Class1 ();
o.foo<int> ();
```

In this particular case, it is very hard to determine at compile time that `Class1:foo<int>` will be needed at runtime. For generic methods instantiated with reference types, the mono runtime supports 'generic sharing'.

This means that we only compile one version of the method, and use it for all instantiations made with reference types, i.e. `Array.Sort<string>` and `Array.Sort<object>` is actually the same native method at runtime. Generating native code for generic shared methods is not very complex since all reference types have the same size: 1 word.

In order to extend generic sharing to valuetypes, we need to solve many problems. Take the following method:

``` c
void swap<T> (T[] a, int i, int j)
{
   var t = a [i];
   a [i] = a [j];
   a [j] = t;
}
```

Here, the size of 'T' is only known at runtime, so we don't know how much stack space to allocate for 't', or how much memory to copy from a \[i\] to t in the first assignment.

For methods which contain their type parameters in their signatures, the situation is even more complex:

``` c
public T return_t<T> (T t) {
    return t;
}
```

Here, the native signature of the method depends on its type parameter. One caller might call this as `return_t<int> (1)`, passing in an int in one register, and expecting the result to be in the return register, while another might call this with a struct, passing it in registers and/or the stack, and expecting the result to be in a memory area whose address was passed in as an extra hidden parameter.

## Basic implementation

### Inside methods

We refer to types which are type variables, or generic instances instantiated with type variables as 'gsharedvt types'. Types whose size depends on type variables are referred as 'variable types'. Since the size of variable types is only known at runtime, we cannot allocate static stack slots for them. Instead, we allocate a stack area for them at runtime using localloc, and dynamically compute their address when needed. The information required for this is stored in a `MonoGSharedVtMethodRuntimeInfo` structure. This structure is stored in an rgctx slot. At the start of the method, the following pseudo code is used to initialize the locals area:

``` c
info_var = rgctx_fetch(<METHOD GSHAREDVT INFO>)
locals_var = localloc (info_var->locals_size)
```

Whenever an address of a variable sized locals is required, its computed using:

``` c
locals_var + info_var->locals_offsets [<local idx>]
```

Local variables are initialized using memset, and copied using memcpy. The size of the locals is fetched from the rgctx. So

``` c
T a = b;
```

is compiled to:

``` c
a_addr = locals_var + info_var->locals_offsets [<a idx>]
b_addr = locals_var + info_var->locals_offsets [<b idx>]
size = rgctx_fetch(<T size>)
memcpy(a_addr, b_addr, size)
```

Methods complied with this type of sharing are called 'gsharedvt' methods.

### Calling gsharedvt methods

GSharedvt methods whose signature includes variable types use a different calling convention where gsharedvt arguments are passed by ref.

``` c
foo(int,int,int,T)
```

is called using:

``` c
foo(inti,int,int,T&)
```

The return value is returned using the same calling convention used to return large structures, i.e. by passing a hidden parameter pointing to a memory area where the method is expected to store the return value.

When a call is made to a generic method from a normal method, the caller uses a signature with concrete types, i.e.: `return_t<int> (1)`. If the callee is also a normal method, then there is no further work needed. However, if the callee is a gsharedvt method, then we have to transition between the signature used by the caller (int (int) in this case), and the signature used by the callee . This process is very low level and architecture specific.

It typically involves reordering values in registers, stack slots etc. It is done by a trampoline called the gsharedvt trampoline. The trampoline receives a pointer to an info structure which describes the calling convention used by the caller and the callee, and the steps needed to transition between the two. The info structure is not passed by the caller, so we use another trampoline to pass the info structure to the trampoline:

So a call goes:

``` c
<caller> -> <gsharedvt arg trampoline> -> <gsharedvt trampoline> -> <callee>
```

The same is true in the reverse case, i.e. when the caller is a gsharedvt method, and the callee is a normal method.

The info structure contains everything need to transfer arguments and make the call, this includes:

-   the callee address.
-   an rgctx to pass to the callee.
-   a mapping for registers and stack slots.
-   whenever this in an 'in' or 'out' case.
-   etc.

As an example, here is what happens for the `return_t<int>` case on ARM:

-   The caller passes in the argument in r0, and expects the return value to be in r0.

-   The callee receives the address of the int value in r1, and it receives the valuetype return address in r0.

Here is the calling sequence:

-   The caller puts the value 1 in r0, then makes the call, which goes to the trampoline code.

-   The trampoline infrastructure detects that the call needs a gsharedvt trampoline. It computes the info structure holding the calling convention information, then creates a gsharedvt arg trampoline for it.

-   The gsharedvt arg trampoline is called, which calls the gsharedvt trampoline, passing the info structure as an argument.

-   The trampoline allocates a new stack frame, along with a 1 word area to hold the return value.

-   It receives the parameter value in r0, saves it into one of its stack slots, and passes the address of the stack slot in r1.

-   It puts the address of the return value into r0.

-   It calls the gsharedvt method.

-   The method copies the memory pointed to by r1 to the memory pointed to by r0, and returns to the trampoline.

-   The trampoline loads the return value from the return value area into r0 and returns to the caller.

-   The caller receives the return value in r0.

For exception handling purposes, we create a wrapper method for the gsharedvt trampoline, so it shows up in stack traces, and the unwind code can unwind through it. There are two kinds of wrappers, 'in' and 'out'. 'in' wrappers handle calls made to gsharedvt methods from callers which use a variable signature, while 'out' wrappers handle calls made to normal methods from callers which use a variable signature. In later parts of this document, we use the term 'wrapper' to mean a gsharedvt arg trampoline.

### Making calls out of gsharedvt methods

#### Normal calls using a non-variable signature

These are handed normally.

#### Direct calls made using a variable signature

These have several problems:

-   The callee might end up being a gsharedvt or a non-gsharedvt method. The former doesn't need a wrapper, the latter does.

-   The wrapper needs to do different things for different instantiations. This means that the call cannot be patched to go to a wrapper, since the wrapper is specific to one instantiation.

To solve these problems, we make an indirect call through an rgctx entry. The rgctx entry resolver code determines what wrapper is needed, and patches the rgctx entry with the address of the wrapper, so later calls made from the gsharedvt method with the same instantiation will go straight to the wrapper.

#### Virtual calls made using a variable signature

Virtual methods have an extra complexity: there is only one vtable entry for a method, and it can be called by both normal and gsharedvt code. To solve this, when a virtual method is compiled as gsharedvt, we put an 'in' wrapper around it, and put the address of this wrapper into the vtable slot, instead of the method code. The virtual call will add an 'out' wrapper, so the call sequence will be:

``` c
<caller> -> <out wrapper> -> <in wrapper> -> <callee>
```

## AOT support

We AOT a gsharedvt version of every generic method, and use it at runtime if the specific instantiation of a method is not found. We also save the gsharedvt trampoline to the mscorlib AOT image, along with a bunch of gsharedvt arg trampolines.

## Implementation details

The gsharedvt version of a method is represented by inflating the method with type parameters, just like in the normal gshared case. To distinguish between the two, we use anon generic parameters whose `gshared_constraint` field is set to point to a valuetype.

Relevant files/functions include:

-   `method-to-ir.c`:
-   `mini-generic-sharing.c`: `instantiate_info ()`: This contains the code which handles calls made from gsharedvt methods through an rgctx entry.
-   `mini-trampolines.c` `mini_add_method_trampolines ()`: This contains the code which handles calls made from normal methods to gsharedvt methods.
-   `mini-<ARCH>-gsharedvt.c`: `mono_arch_get_gsharedvt_call_info ()`: This returns the arch specific info structure passed to the gsharedvt trampoline.
-   `tramp-<ARCH>-gsharedvt.c`: `mono_arch_get_gsharedvt_trampoline ()`: This creates the gsharedvt trampoline. `mono_aot_get_gsharedvt_arg_trampoline ()`: This returns a gsharedvt arg trampoline which calls the gsharedvt trampoline passing in the info structure in an arch specific way.

## Possible future work

-   Optimizations:
    -   Allocate the `info_var` and `locals_var` into registers.
    -   Put more information into the info structure, to avoid rgctx fetch calls.
    -   For calls made between gsharedvt methods, we add both an out and an in wrapper. This needs to be optimized so we only uses one wrapper in more cases, or create a more generalized wrapper, which can function as both an out and an in wrapper at the same time.
-   The AOT complier tries to compile every instantiation which can be used at runtime. This leads to a lot of instantiations which are never used, and take up a lot of space. We might want to avoid generating some of these instantiations and use their gsharedvt versions instead. This is particularly true for methods where using the gsharedvt version might mean very little or no overhead.
