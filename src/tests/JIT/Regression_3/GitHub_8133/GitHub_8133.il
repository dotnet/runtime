// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.assembly 'GitHub_8133' {}

// The original repro for this test was SBCG due to incorrect computation of value numbers for the ref-typed locals
// in Test::Main(). Despite the fact that each local refers to a different array element, all of the locals were
// assigned the same value number.

.class public Test extends [mscorlib]System.Object
{
    .method static void Equal(int32 i, int32 j) cil noinlining
    {
        ldarg.0
        ldarg.1
        bne.un.s fail
        ret

fail:
        newobj instance void [mscorlib]System.Exception::.ctor()
        throw
    }

    .method static !!0& Add<T>(!!0& addr, int32 offset)
    {
        ldarg.0
        ldarg.1
        sizeof !!0
        conv.i
        mul
        add
        ret
    }

    .method public hidebysig static int32 RefAdd() cil managed
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint

        .locals init (
            [0] int32[] a,
            [1] int32& r1,
            [2] int32& r2,
            [3] int32& r3)

        ldc.i4.4
        newarr [mscorlib]System.Int32
        dup
        ldtoken field valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16' '<PrivateImplementationDetails>'::'6E9F9131664668938673AFE814BBDE210C6AE91F'
        call void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(class [mscorlib]System.Array, valuetype [mscorlib]System.RuntimeFieldHandle)
        stloc.0

        ldloc.0
        ldc.i4.0
        ldelema [mscorlib]System.Int32
        ldc.i4.1
        call !!0& Test::Add<int32>(!!0&, int32)
        stloc.1
        ldc.i4     0x234
        ldloc.1
        ldind.i4
        call void Test::Equal(int32, int32)

        ldloc.1
        ldc.i4.2
        call !!0& Test::Add<int32>(!!0&, int32)
        stloc.2
        ldc.i4 0x456
        ldloc.2
        ldind.i4
        call void Test::Equal(int32, int32)

        ldloc.2
        ldc.i4.s   -3
        call !!0& Test::Add<int32>(!!0&, int32)
        stloc.3
        ldc.i4 0x123
        ldloc.3
        ldind.i4
        call void Test::Equal(int32, int32)

        ldc.i4 100
        ret
    }
}

.class private auto ansi sealed '<PrivateImplementationDetails>'
       extends [mscorlib]System.Object
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
    .class explicit ansi sealed nested private '__StaticArrayInitTypeSize=16'
         extends [mscorlib]System.ValueType
    {
        .pack 1
        .size 16
    }

    .field static assembly initonly int32 '4C55E5E5FC2235CC8C201E69A345F7FAB3FB46FA' at I_000054DC
    .field static assembly initonly int64 '67423EBFA8454F19AC6F4686D6C0DC731A3DDD6B' at I_000054E4
    .field static assembly initonly valuetype '<PrivateImplementationDetails>'/'__StaticArrayInitTypeSize=16' '6E9F9131664668938673AFE814BBDE210C6AE91F' at I_000054EC
    .field static assembly initonly int32 '9BCE73D0C8B9ECA4F24154F3BD3B8AA473B1C3A9' at I_000054FC
}

.data cil I_000054DC = bytearray (
                 42 42 42 42)                                     // BBBB
.data cil I_000054E4 = bytearray (
                 00 01 02 03 04 05 06 07) 
.data cil I_000054EC = bytearray (
                 23 01 00 00 34 02 00 00 45 03 00 00 56 04 00 00) // #...4...E...V...
.data cil I_000054FC = bytearray (
                 12 34 56 78)                                     // .4Vx
