// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly mul.exe{}

.class public _mul {
.method public static  int32 I4(int32,int32,int32) {
.maxstack       3
        ldarg   0
        ldarg   1
        mul
        ldarg   2
        ceq
        ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
        ldc.i4  0x55555555
        ldc.i4  0x00000002
        ldc.i4  0xAAAAAAAA
        call    int32 _mul::I4(int32,int32,int32)
        brfalse FAIL
	ldc.i4	100
	ret
FAIL:
	ldc.i4	5
	ret
}
}
