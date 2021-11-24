// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xf81ec410 {}
.assembly extern xunit.core {}
.class ILGEN_0xf81ec410 {

.method static float32 Method_0x3676() {
.maxstack	7

 .locals (int32 local_0x0)

ldc.i4 0x100618b7 
stloc local_0x0
 ldloc local_0x0
 ldc.i4.m1
 rem
 ldc.i4.5
 ldc.i4.7
 div
 mul
 ldc.i4.2
 ldloc local_0x0
 beq Branch_0x2

 ldc.i4.5
 br Branch_0x3

 Branch_0x2:

 ldc.i4.3
 Branch_0x3:

 conv.u1
 bne.un Branch_0x0

 ldc.r8 float64(0x2200578d1f273c09)
 conv.r4
 br Branch_0x1

 Branch_0x0:

 ldc.i8 0x7f452bf1df25a10
 conv.r4
 Branch_0x1:

ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call float32 ILGEN_0xf81ec410::Method_0x3676()

// add the following due to ASURT#123755
 ldc.i8 0x7f452bf1df25a10
 conv.r4
 sub

conv.i4

ldc.i4 100
add

 ret
}

}
