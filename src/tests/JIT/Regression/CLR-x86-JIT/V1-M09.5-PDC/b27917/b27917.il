// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27917' {}
.class public ILGEN_0x84875418 {

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	19

 .locals (unsigned int64[] local_0x0,int8 local_0x3,unsigned int16[] local_0x5)

ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x0
ldc.i4 0x4 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x5


Start_Orphan_0:
 ldloca local_0x3
 ldc.i4 0xc
 ldc.r8 5.5
 ldc.r8 4.4
 ckfinite
 rem
 conv.u
 rem

Start_Orphan_17:
 ldloc local_0x5
 ldc.i4.1
 ldelema [mscorlib]System.UInt16
 ldc.i4.1
 stind.i2
End_Orphan_17:

 ldc.r8 float64(0x19f87c1b4c036c43)
 conv.ovf.i8.un

Start_Orphan_1f:
 ldloc local_0x0
 ldc.i4.1
 ldelem.u8
 pop
End_Orphan_1f:

 conv.u1
 cgt
stind.i1
End_Orphan_0:

Start_Orphan_26:
 ldc.i4 55
 conv.u8
 ldloc local_0x0
 ldlen
 conv.ovf.i8

Start_Orphan_30:
ldc.i4 55
 newarr [mscorlib]System.Int64
 ldloc local_0x5
 ldloc local_0x3
 ldelema [mscorlib]System.UInt16
 ldind.u2
 conv.i1
 ldc.i4.1
 conv.i8
 stelem.i8
End_Orphan_30:

 mul.ovf.un
 pop
End_Orphan_26:

 ldc.i4.0
ldc.i4 100
add
ret
}
}
