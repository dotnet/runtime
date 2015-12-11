// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.class          _nop {



.method public static int32 return(int32) {
.maxstack       2
        ldarg   0
        ret
}

.method public         void _nop() {
.maxstack       0
        ret
}

.method public static  int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack               5

        ldc.i4  0xFF
        call    int32 _nop::return(int32)
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        nop
        ldc.i4  0xFF
        call    int32 _nop::return(int32)
        ceq
        brfalse FAIL

PASS:
        ldc.i4 100
        br              END

FAIL:
        ldc.i4  0x00000000

END:
        ret
}
}


.assembly c_nop.exe {}

