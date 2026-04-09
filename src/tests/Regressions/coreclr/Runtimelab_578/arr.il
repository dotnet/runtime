// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly 'arr' { }

.method static int32[] alloc() cil managed
{
    ldc.i4.8
    ldc.i4.8
    newobj instance void int32[]::.ctor(int32,int32)
    ret
}

.method static int32 negative() cil managed
{
    .try
    {
        call int32[] alloc()
        leave bad
    }
    catch [mscorlib]System.MissingMethodException
    {
        pop
        leave good
    }

  good:
    ldc.i4.1
    ret

  bad:
    ldc.i4.0
    ret
}

.class public auto ansi arr {
.method public static int32 main() cil managed
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .locals init (int32[][] a)
    .entrypoint

    ldc.i4.7
    newobj instance void int32[]::.ctor(int32)
    ldlen

    ldc.i4.2
    ldc.i4.3
    newobj instance void int32[][]::.ctor(int32,int32)
    dup
    stloc a
    ldlen
    ldc.i4 40
    mul // 2 * 40 = 80
    add // 7 + 80 = 87

    ldloc a
    ldc.i4.0
    ldelem int32[]
    ldlen
    ldc.i4.4
    mul // 3 * 4 = 12

    add // 87 + 12 = 99

    call int32 negative()
    add // 99 + 1 = 100

    ret
}
}
