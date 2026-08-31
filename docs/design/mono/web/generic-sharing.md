# Generic Sharing

Source code
----------

The code which implements generic sharing is in `mini-generic-sharing.c`. The architecture specific parts are in `mini-<arch>.c` and `tramp-<arch>.c`.

RGCTX register
--------------

Generic shared code needs access to type information. This information is contained in a RGCTX for non-generic methods and in an MRGCTX for generic methods. It is passed in one of several ways, depending on the type of the called method:

1.  Non-generic non-static methods of reference types have access to the RGCTX via the "this" argument (this-\>vtable-\>rgctx).

2.  Non-generic static methods of reference types and non-generic methods of value types need to be passed a pointer to the caller's class's VTable in the MONO_ARCH_RGCTX_REG register.

3.  Generic methods need to be passed a pointer to the MRGCTX in the `MONO_ARCH_RGCTX_REG` register.

The `MONO_ARCH_RGCTX_REG` must not be clobbered by trampolines.

`MONO_ARCH_RGCTX_REG` is the same as the IMT register on all platforms. The reason for this is that the RGCTX register is used to pass information to a concrete method, while the IMT register is used for indirect calls where
the called method is not known, so the the same call doesn't use both an RGCTX and an IMT register.

This register lifetime starts at the call site that loads it and ends in the callee prologue when it is either discarded or stored into a local variable.

It's better to avoid registers used for argument passing for the RGCTX as it would make the code dealing with calling conventions code a lot harder.

For indirect calls, the caller doesn't know the RGCTX value which needs to be passed to the callee. In this case, an 'rgctx trampoline' is used. These are small trampolines created by `mono_create_static_rgctx_trampoline()`. The caller calls the trampoline, which sets the RGCTX to the required value and jumps to the callee. These trampolines are inserted into the call chain when indirect calls are used (virtual calls, delegates, runtime invoke etc.).

An alternative design would pass the rgctx as a normal parameter, which would avoid the need for an RGCTX register. The problem with this approach is that the caller might not know whenever the callee needs an RGCTX argument
or not. I.e. the callee might be a non-shared method, or even a non-generic method (i.e. `Action<int>` can end up calling a `foo(int)` or a `foo<T> (T)` instantiated with `int`.).

Method prologue
---------------

Generic shared code that have a `RGCTX` receive it in `RGCTX_REG`. There must be a check in mono_arch_emit_prolog for MonoCompile::rgctx_var and if set store it. See mini-x86.c for reference.

Dealing with types
------------------

During JITting and at runtime, the generic parameters used in shared methods are represented by a `MonoGenericParam` with the `gshared_constraint` field pointing to a `MonoType` which identifies the set of types this
generic param is constrained to. If the constraint is `object`, it means the parameter can match all reference types. If its `int`, it can match `int` and all enums whose basetype is `int` etc.

Calling `mini_get_underlying_type()` on the type will return the constraint type. This is used through the JIT to handle generic parameters without needing to special case them, since for example, a generic parameter constrained to be a reference type can be handled the same way as `MONO_TYPE_OBJECT`.

(M)RGCTX lazy fetch trampoline
------------------------------

The purpose of the lazy fetch trampoline is to fetch a slot from an (M)RGCTX which might not be inited, yet. In the latter case, it needs to go make a transition to unmanaged code to fill the slot. This is the layout of a RGCTX:

         +---------------------------------+
         | next | slot 0 | slot 1 | slot 2 |
         +--|------------------------------+
            |
      +-----+
      |  +---------------------------------
      +->| next | slot 3 | slot 4 | slot 5 ....
         +--|------------------------------
            |
      +-----+
      |  +------------------------------------
      +->| next | slot 10 | slot 11 | slot 12 ....
         +--|---------------------------------
            .
            .
            .

For fetching a slot from a RGCTX the trampoline is passed a pointer (as a normal integer argument) to the VTable. From there it has to fetch the pointer to the RGCTX, which might be null. Then it has to traverse the correct number of "next" links, each of which might be NULL. Arriving at the right array it needs to fetch the slot, which might also be NULL. If any of the NULL cases, the trampoline must transition to unmanaged code to potentially setup the RGCTX and fill the slot. Here is pseudo-code for fetching slot 11:

        ; vtable ptr in r1
        ; fetch RGCTX array 0
        r2 = *(r1 + offsetof(MonoVTable, runtime_generic_context))
        if r2 == NULL goto unmanaged
        ; fetch RGCTX array 1
        r2 = *r2
        if r2 == NULL goto unmanaged
        ; fetch RGCTX array 2
        r2 = *r2
        if r2 == NULL goto unmanaged
        ; fetch slot 11
        r2 = *(r2 + 2 * sizeof (gpointer))
        if r2 == NULL goto unmanaged
        return r2
      unmanaged:
        jump unmanaged_fetch_code

The number of slots in the arrays must be obtained from the function `mono_class_rgctx_get_array_size()`.

The MRGCTX case is different in two aspects. First, the trampoline is not passed a pointer to a VTable, but a pointer directly to the MRGCTX, which is guaranteed not to be NULL (any of the next pointers and any of the slots can be NULL, though). Second, the layout of the first array is slightly different, in that the first two slots are occupied by a pointers to the class's VTable and to the method's method_inst. The next pointer is in the third slot and the first actual slot, "slot 0", in the fourth:

         +--------------------------------------------------------+
         | vtable | method_inst | next | slot 0 | slot 1 | slot 2 |
         +-------------------------|------------------------------+
                                   .
                                   .

All other arrays have the same layout as the RGCTX ones, except possibly for their length.

The function to create the trampoline, mono_arch_create_rgctx_lazy_fetch_trampoline(), gets passed an encoded slot number. Use the macro `MONO_RGCTX_SLOT_IS_MRGCTX` to query whether a trampoline for an MRGCTX is needed, as opposed to one for a RGCTX. Use `MONO_RGCTX_SLOT_INDEX` to get the index of the slot (like 2 for "slot 2" as above). The unmanaged fetch code is yet another trampoline created via `mono_arch_create_specific_trampoline()`, of type `MONO_TRAMPOLINE_RGCTX_LAZY_FETCH`. It's given the slot number as the trampoline argument. In addition, the pointer to the VTable/MRGCTX is passed in `MONO_ARCH_VTABLE_REG` (like the VTable to the generic class init trampoline - see above).

The RGCTX fetch trampoline code doesn't return code that must be jumped to, so, like for those trampolines (see above), the generic trampoline code must do a normal return instead.

Getting generics information about a stack frame
------------------------------------------------

If a method is compiled with generic sharing, its `MonoJitInfo` has the `has_generic_jit_info` bit set. In that case, the `mono_jit_info_get_generic_jit_info()` function will return
a `MonoGenericJitInfo` structure.

The `MonoGenericJitInfo` contains information about the location of the this/vtable/MRGCTX variable, if the `has_this` flag is set. If that is the case, there are two possibilities:

1.  `this_in_reg` is set. `this_reg` is the number of the register where the variable is stored.

2.  `this_in_reg` is not set. The variable is stored at offset `this_offset` from the address in the register with number `this_reg`.

The variable can either point to the "this" object, to a vtable or to an MRGCTX:

1.  If the method is a non-generic non-static method of a reference type, the variable points to the "this" object.

2.  If the method is a non-generic static method or a non-generic method of a value type, the variable points to the vtable of the class.

3.  If the method is a generic method, the variable points to the MRGCTX of the method.

Layout of the MRGCTX
--------------------

The MRGCTX is a structure that starts with `MonoMethodRuntimeGenericContext`, which contains a pointer to the vtable of the class and a pointer to the `MonoGenericInst` with the type arguments for the method.

Blog posts about generic code sharing
-------------------------------------

-   [September 2007: Generics Sharing in Mono](http://schani.wordpress.com/2007/09/22/generics-sharing-in-mono/)
-   [October 2007: The Trouble with Shared Generics](http://schani.wordpress.com/2007/10/12/the-trouble-with-shared-generics/)
-   [October 2007: A Quick Generics Sharing Update](http://schani.wordpress.com/2007/10/15/a-quick-generics-sharing-update/)
-   [January 2008: Other Types](http://schani.wordpress.com/2008/01/29/other-types/)
-   [February 2008: Generic Types Are Lazy](http://schani.wordpress.com/2008/02/25/generic-types-are-lazy/)
-   [March 2008: Sharing Static Methods](http://schani.wordpress.com/2008/03/10/sharing-static-methods/)
-   [April 2008: Sharing Everything And Saving Memory](http://schani.wordpress.com/2008/04/22/sharing-everything-and-saving-memory/)
-   [June 2008: Sharing Generic Methods](http://schani.wordpress.com/2008/06/02/sharing-generic-methods/)
-   [June 2008: Another Generic Sharing Update](http://schani.wordpress.com/2008/06/27/another-generic-sharing-update/)
