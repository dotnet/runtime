// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/125343
//
// Morph incorrectly folds FIELD_ADDR into parent stores, reordering
// the null check with respect to the data operand's side effects.
//
// The IL sequence:  call GetNull; ldflda Field; ldc.i4.1; ldarg.0; div; stind.i4
// creates STORE_IND(FIELD_ADDR(null_obj), 1/0).
//
// Per IL semantics, ldflda should null-check before the division executes,
// so a NullReferenceException is expected. If morph folds the FIELD_ADDR
// into the store's implicit null check, the division executes first and
// throws DivideByZeroException instead.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) }
.assembly extern xunit.core { }
.assembly Runtime_125343 { }

.class public auto ansi beforefieldinit MyClass
       extends [System.Runtime]System.Object
{
    .field public int32 Field

    .method public hidebysig specialname rtspecialname
            instance void .ctor() cil managed
    {
        .maxstack 1
        ldarg.0
        call instance void [System.Runtime]System.Object::.ctor()
        ret
    }
}

.class public auto ansi beforefieldinit Runtime_125343
       extends [System.Runtime]System.Object
{
    .method public hidebysig static int32 TestEntryPoint() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = ( 01 00 00 00 )
        .entrypoint
        .maxstack 1

        ldc.i4.0
        call int32 Runtime_125343::Test(int32)
        ret
    }

    // Test method: takes a divisor argument (expected to be 0).
    // Performs ldflda on a null object, then divides by the argument,
    // then stores via stind. The null check from ldflda should fire
    // before the division.
    .method private hidebysig static int32
            Test(int32 divisor) cil managed noinlining
    {
        .maxstack 4
        .locals init (int32 result)

        ldc.i4 101
        stloc.0

        .try
        {
            call class MyClass Runtime_125343::GetNull()
            ldflda int32 MyClass::Field
            ldc.i4.1
            ldarg.0
            div
            stind.i4
            leave.s DONE
        }
        catch [System.Runtime]System.NullReferenceException
        {
            pop
            // Correct: ldflda null-checked before the division ran.
            ldc.i4 100
            stloc.0
            leave.s DONE
        }
        catch [System.Runtime]System.DivideByZeroException
        {
            pop
            // Bug: the division ran before the null check.
            ldc.i4 101
            stloc.0
            leave.s DONE
        }

    DONE:
        ldloc.0
        ret
    }

    .method private hidebysig static class MyClass
            GetNull() cil managed noinlining
    {
        .maxstack 1
        ldnull
        ret
    }
}
