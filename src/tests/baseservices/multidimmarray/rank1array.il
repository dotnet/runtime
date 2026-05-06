// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests that SzArray is castable to multidimensional array of rank 1 with zero lower bounds
// and that the MdArray methods can operate on such `this`.
// We cast an SzArray to MdArray, call methods on it, and validate the SzArray was updated
// at the expected memory locations.

.assembly extern mscorlib { }
.assembly extern xunit.core {}

.assembly rank1array
{
}

.module rank1array.exe
.subsystem 0x0003
.corflags 0x00000001

.class public auto ansi rank1array {
.method public hidebysig static int32 Main () cil managed
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .locals init(
        [0] int32[] szArray,
        [1] int32[0...] mdArray
    )

    .maxstack 8
    .entrypoint

    ldc.i4.2
    newarr int32
    stloc.0

    // SzArray is castable to MdArray rank 1 with zero lower bounds
    ldloc.0
    castclass int32[0...]
    stloc.1

    ldloc.1
    ldc.i4.0
    ldc.i4 0x4d2
    call instance void int32[0...]::Set(int32, int32)

    // The call to Set had better updated the memory location we expect
    ldloc.0
    ldc.i4.0
    ldelem.i4
    ldc.i4 0x4d2
    ceq
    brtrue SetOK
    ldc.i4.1
    ret

SetOK:
    ldloc.1
    ldc.i4.0
    call instance int32 int32[0...]::Get(int32)
    ldc.i4 0x4d2
    ceq
    brtrue GetOK
    ldc.i4.2
    ret

GetOK:
    ldloc.1
    ldc.i4.1
    call instance int32& int32[0...]::Address(int32)
    ldc.i4 42
    stind.i4

    // The call to Address had better given us the memory location we expect
    ldloc.0
    ldc.i4.1
    ldelem.i4
    ldc.i4 42
    ceq
    brtrue AddressOK
    ldc.i4.3
    ret

AddressOK:

    // We can also cast through type-size-equivalence
    ldc.i4.0
    newarr int32
    isinst uint32[0...]
    brtrue SizeEquivalenceOK
    ldc.i4.4
    ret

SizeEquivalenceOK:

    // We follow all the size equivalence rules though
    ldc.i4.0
    newarr float32
    isinst uint32[0...]
    brfalse SizeEquivalenceNegativeOK
    ldc.i4.5
    ret

SizeEquivalenceNegativeOK:

    // String -> object casts
    ldc.i4.0
    newarr string
    isinst object[0...]
    brtrue StringObjectCastOK
    ldc.i4.6
    ret

StringObjectCastOK:

    // Object -> string negative cast
    ldc.i4.0
    newarr object
    isinst string[0...]
    brfalse ObjectStringNegativeOK
    ldc.i4.7
    ret

ObjectStringNegativeOK:

    // MdArray of rank 1 is also castable to SzArray
    ldc.i4.0
    newobj instance void int32[0...]::.ctor(int32)
    isinst int32[]
    brtrue MdSzArrayOK
    ldc.i4.8
    ret

MdSzArrayOK:

    // "newobj instance void int32[0...]::.ctor(int32)" actually gives you int[]
    ldc.i4.1
    newobj instance void int32[0...]::.ctor(int32)
    call instance class [mscorlib]System.Type class [mscorlib]System.Object::GetType()
    ldtoken int32[]
    call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ceq
    brtrue Int32ArrayRank1IsInt32SzArray
    ldc.i4 9
    ret

Int32ArrayRank1IsInt32SzArray:

    // int32[] and int32[0..] are still different types though
    ldtoken int32[]
    call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ldtoken int32[0...]
    call class [mscorlib]System.Type class [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ceq
    brfalse DifferentTypes
    ldc.i4 10
    ret

DifferentTypes:

    ldc.i4 100
    ret
}
}
