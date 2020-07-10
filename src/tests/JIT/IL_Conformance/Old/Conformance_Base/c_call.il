// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}


.method public static int32 test() {
    ldc.i4  0xFF
    ret
}
    
.class _call {
.method public static int32 main(class [mscorlib]System.String[]) {
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


.assembly c_call.exe{}


