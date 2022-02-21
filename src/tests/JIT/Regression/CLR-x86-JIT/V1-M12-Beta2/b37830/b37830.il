// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b37830' {}
.assembly extern xunit.core {}
.class ILGEN_0xe012a579 {

.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 ldc.i4 0x44 
 conv.i8
 ldc.i4 255
 newarr [mscorlib]System.Int32 
 ldc.i4 0x68 
 ldelem.i4
 conv.i8
 mul
 conv.i4
ldc.i4 100
add
ret
}

}
 
