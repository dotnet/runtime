// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly no.exe{}

.class public _no {

// typecheck

.method public static string typecheck_castclass(object) {
.maxstack	1
    ldarg   0
    no. typecheck castclass [mscorlib]System.String
    ret
}
.method public static int32& typecheck_unbox(object) {
.maxstack	1
    ldarg	0
    no. typecheck unbox [mscorlib]System.Object
    ret
}
.method public static object& typecheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. typecheck ldelema [mcsorlib]System.Object
    ret
}
.method public static void typecheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. typecheck stelem.ref
    ret
}

// rangecheck

.method public static object& rangecheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. rangecheck ldelema [mcsorlib]System.Object
    ret
}

.method public static object rangecheck_ldelem(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. rangecheck ldelem [mcsorlib]System.Object
    ret
}

.method public static void rangecheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. rangecheck stelem.ref
    ret
}

// nullcheck

.class private auto ansi beforefieldinit Example
    extends [System.Private.CoreLib]System.Object
{
    .field public object Field

    .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
    {
        ldarg.0
        call instance void [System.Private.CoreLib]System.Object::.ctor()
        ret
    }

    .method public newslot virtual instance void VirtualMethod() cil managed
    {
        ret
    }
}

.method public static void nullcheck_callvirt(class Example) {
.maxstack	1
    ldarg	0
    no. nullcheck callvirt instance void Example::VirtualMethod()
    ret
}

.method public static object nullcheck_ldfld(class Example) {
.maxstack	1
    ldarg	0
    no. nullcheck ldfld object Example::Field
    ret
}

.method public static void nullcheck_stfld(class Example, object) {
.maxstack	2
    ldarg	0
    ldarg   1
    no. nullcheck stfld object Example::Field
    ret
}

.method public static void* nullcheck_ldvirtfn(class Example) {
.maxstack	1
    ldarg	0
    no. nullcheck ldvirtfn instance void Example::VirtualMethod()
    ret
}

.method public static object& nullcheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. nullcheck ldelema [mcsorlib]System.Object
    ret
}

.method public static object nullcheck_ldelem(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. nullcheck ldelem [mcsorlib]System.Object
    ret
}

.method public static void nullcheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. nullcheck stelem.ref
    ret
}

// typecheck | rangecheck

.method public static object& typecheck_rangecheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. typecheck | rangecheck ldelema [mcsorlib]System.Object
    ret
}

.method public static void typecheck_rangecheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. typecheck | rangecheck stelem.ref
    ret
}

// typecheck | nullcheck

.method public static object& typecheck_nullcheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. typecheck | nullcheck ldelema [mcsorlib]System.Object
    ret
}

.method public static object typecheck_nullcheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. typecheck | rangecheck stelem.ref
    ret
}


// rangecheck | nullcheck

.method public static object& rangecheck_nullcheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. rangecheck | nullcheck ldelema [mcsorlib]System.Object
    ret
}

.method public static object rangecheck_nullcheck_ldelem(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. rangecheck | nullcheck ldelem [mcsorlib]System.Object
    ret
}

.method public static void rangecheck_nullcheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. rangecheck | nullcheck stelem.ref
    ret
}

// typecheck | rangecheck | nullcheck

.method public static object& typecheck_rangecheck_nullcheck_ldelema(object[]) {
.maxstack	2
    ldarg	0
    ldc.i4  0
    no. typecheck | rangecheck | nullcheck ldelema [mcsorlib]System.Object
    ret
}

.method public static object typecheck_rangecheck_nullcheck_stelem(object[], string) {
.maxstack	3
    ldarg	0
    ldc.i4  0
    ldarg   1
    no. typecheck | rangecheck | nullcheck stelem.ref
    ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        .locals init (
            object,
            object[],
            string,
            class Example
        )
        .try
        {
            // prepare locals

            ldc.i4 0xDEADBEEF
            box [mscorlib]System.Int32
            stloc 0

            ldc.i4 1
            newarr [mscorlib]System.Object
            stloc 1

            ldstr "Hello World!"
            stloc 2

            newobj instance void Example::.ctor()
            stloc 3

            // typecheck

            ldloc 2
            call string typecheck_castclass(object)
            pop

            ldloc 0
            call int32& typecheck_unbox(object)
            pop

            ldloc 1
            call object& typecheck_ldelema(object[])
            pop

            // rangecheck

            ldloc 1
            call object& rangecheck_ldelema(object[])
            pop

            ldloc 1
            call object& rangecheck_ldelem(object[])
            pop

            ldloc 1
            ldloc 2
            call void rangecheck_stelem(object[], string)

            // nullcheck

            ldloc 3
            call void nullcheck_callvirt(class Example)

            ldloc 3
            call object nullcheck_ldfld(class Example)
            pop

            ldloc 3
            ldloc 0
            call void nullcheck_stfld(class Example, object)

            ldloc 3
            call void* nullcheck_ldvirtfn(class Example)
            pop

            ldloc 1
            call object& nullcheck_ldelema(object[])
            pop

            ldloc 1
            call object nullcheck_ldelem(object[])
            pop

            ldloc 1
            ldloc 2
            call object nullcheck_stelem(object[], string)

            // typecheck | rangecheck

            ldloc 1
            call object& typecheck_rangecheck_ldelema(object[])
            pop

            ldloc 1
            ldloc 2
            call void typecheck_rangecheck_stelem(object[], string)

            // typecheck | nullcheck

            ldloc 1
            call object& typecheck_nullcheck_ldelema(object[])
            pop

            ldloc 1
            ldloc 2
            call object typecheck_nullcheck_stelem(object[], string)
            pop

            // rangecheck | nullcheck

            ldloc 1
            call object& rangecheck_nullcheck_ldelema(object[])
            pop

            ldloc 1
            call object& rangecheck_nullcheck_ldelem(object[])
            pop

            ldloc 1
            ldloc 2
            call void rangecheck_nullcheck_stelem(object[], string)

            // typecheck | rangecheck | nullcheck

            ldloc 1
            call object& typecheck_rangecheck_nullcheck_ldelema(object[])
            pop

            ldloc 1
            ldloc 2
            call object typecheck_rangecheck_nullcheck_stelem(object[], string)
            pop

            leave.s PASS
        }
        catch [mscorlib]System.Object
        {
            pop
            leave.s FAIL
        }

PASS:   ldc.i4 100
        ret

FAIL:   ldc.i4 0x0
        ret
}
}
