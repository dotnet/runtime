// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14770
{
}
.class public ILGEN_622380794 {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	12

 .locals (int32 local_0x0,int64 local_0x1,float32 local_0x2,float64 local_0x3)

ldc.i4 0x63c627a0 
stloc local_0x0
ldc.r4 float32(0x440365e) 
stloc local_0x2

 ldloc local_0x2
 conv.i
 ldc.r8 float64(0xede35f12a00744b)
 ldc.r8 float64(0x35433ab712234209)
 clt
 cgt
 ldc.i4.1
 cgt.un
 conv.u
 conv.u1
 ldloc local_0x0
 conv.u8
 ldloca local_0x2
 ldind.r4
 ldloca local_0x0
 ldc.i4.5
 stind.i4
 ldloca local_0x2
 ldind.r4
 cgt
 shr
 conv.i4
 mul
ldc.i4 100
add
ret
}
}
