// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly ILGEN_0xc60e022c {}
.assembly extern mscorlib{auto}

.class ILGEN_0xc60e022c {

.method static int16  Method_0xf7bb2d95() {
.zeroinit 
.maxstack	3

 BLOCK_1:
    ldc.r4 float32(0x4ec956e5)
    conv.i4
    ret
}
.method static int32 Main() {
.entrypoint
.maxstack	15

    //If the implicit conversion below is not correct, the test will return a non 100 (FAIL) return code.
    call int16  ILGEN_0xc60e022c::Method_0xf7bb2d95()
    conv.i4
    ldc.i4 29212
    sub
    dup
    call void [System.Console]System.Console::WriteLine(int32)
    ret
}

}


