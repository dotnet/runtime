// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly tst { }
.assembly extern mscorlib { }
.assembly extern xunit.core {}
 
.method static void* GetValue()
{
    .locals init ([0] void*)
    ldc.i4 100
    conv.i
    stloc.0
    ldloca 0
    ldobj void*
    ret
}

.class public auto ansi tst {
.method public static int32 main()
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    call void* GetValue()
    conv.i4
    ret
}
}
