// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1877c3d8 {}
.class ILGEN_0x1877c3d8 {

.method static int32 main() {
.entrypoint
.maxstack	17
  .try {
  
 ldc.i4.m1
 conv.ovf.u8
 ldc.i8 0x2
 ldc.r4 0.636
 conv.u8
 div
 mul
 conv.i1
 
  pop
  leave exit
} catch [mscorlib]System.Exception
{
  pop
  leave exit
}
exit:
  ldc.i4.0
ldc.i4 100
add 
ret
}
}
