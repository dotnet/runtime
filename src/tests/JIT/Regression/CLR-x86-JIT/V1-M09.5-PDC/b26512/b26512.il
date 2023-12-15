// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b26512' {}
.class public ILGEN_0xeed686f5 {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	25
.try {
 ldc.i4 44
 newarr [mscorlib]System.Int64
 ldc.i4 44
 ldelem.i8
 ldc.i8 8
 div
 conv.i4
 ldc.i4 5
 xor
 conv.u8
 conv.r4
 conv.i4
 
  pop
  leave exit
} catch [mscorlib]System.IndexOutOfRangeException
{
  pop
  leave exit
}
exit:
  ldc.i4 100
  ret
}
}
