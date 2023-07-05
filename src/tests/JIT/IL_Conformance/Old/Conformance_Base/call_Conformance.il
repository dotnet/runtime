// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly extern xunit.core {}
.method public static int32 test() {
    ldc.i4  0xFF
    ret
}
    
.namespace call_Conformance
{
.class public _call {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack   10
    call    int32 test()
    ldc.i4  0xFF
    ceq
    brfalse FAIL

PASS:
    ldc.i4 100
    ret
FAIL:
    ldc.i4  0x0
    ret
}
}





.assembly 'call_Conformance'{}
}
