// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb7fdb3b {}
.assembly extern xunit.core {}
.class ILGEN_0xb7fdb3b {
.field static	unsigned int8 field_0x2
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 .locals (unsigned int8[] local_0x4,float32[] local_0x5,unsigned int64[] local_0x6,int32 local_0x7)
 ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x4
 ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x5
 ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x6
 ldc.i4 0xd3891155 
 stloc local_0x7
 ldc.i4 0x22926874 
 stsfld unsigned int8 ILGEN_0xb7fdb3b::field_0x2
Start_Orphan_0:
 ldc.i4.8
 ldc.r4 float32(0x6bd96df3) 
 ldc.r4 float32(0x6bd96df3) 
 blt Branch_0x8
 ldc.i4.8
 br Branch_0x9
Branch_0x8:
 ldc.i4.3
Branch_0x9:
 ldloc local_0x7
 ldsfld unsigned int8 ILGEN_0xb7fdb3b::field_0x2
 sub.ovf
 bne.un Branch_0x6
 ldc.i4.m1
 br Branch_0x7
Branch_0x6:
 ldc.i4.3
Branch_0x7:
 pop
 pop
End_Orphan_0:
 ldc.i4 100
 ret
}
}
