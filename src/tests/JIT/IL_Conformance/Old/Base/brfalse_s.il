// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.class public _brfalse_s {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack       10

        ldc.i4  0x0
        brfalse.s PASS

FAIL:
        ldc.i4  0x0
        ret
PASS:
        ldc.i4 100
        ret
}
}

.assembly brfalse_s {}
