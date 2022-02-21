// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b36301' {}
.assembly extern xunit.core {}
.class ILGEN_0x7ff68940 {

.method static float32 Method_0x2784() {
.maxstack	9

 ldc.i8 0x2
 ldc.i8 0x6
 mul
 conv.i
 conv.r4
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call float32 ILGEN_0x7ff68940::Method_0x2784()
conv.i4
ldc.i4 12
sub
ldc.i4 100
add
 ret
}

}
