// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b30799' {}
.class public ILGEN_0xa7db44c0 {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	19


 ldc.r8 float64(0x68bd1e1060dcb8c)
 conv.ovf.i
 ldc.i4 44
 mul.ovf.un
 conv.r8
 ldc.r4 float32(0x71edee7)
 conv.ovf.i8
 conv.r.un
 clt
 ldc.i4.1
 add
 ldc.i4.1
 sub
ldc.i4 100
add
ret
}
}



