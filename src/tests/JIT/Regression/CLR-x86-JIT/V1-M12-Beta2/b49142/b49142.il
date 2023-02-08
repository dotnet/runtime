// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b49142' {}
.class public ILGEN_0xf7c4ec97 {

.field static	unsigned int32[] field_0xc

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10

 .locals (unsigned int32[] local_0x2,unsigned int64[] local_0x3,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc)

ldc.i4 0x7020 
stloc local_0xa
ldc.i4 0x1696 
stloc local_0xc

ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xf7c4ec97::field_0xc

Start_Orphan_1:
 ldloca local_0xa
 pop
End_Orphan_1:

 ldloc local_0xc
 conv.ovf.u8.un
 ldloc local_0xa
 conv.ovf.i8.un

Start_Orphan_4:
 ldsfld unsigned int32[] ILGEN_0xf7c4ec97::field_0xc
 ldc.i4 6
 ldc.i4 1
 stelem.i4
End_Orphan_4:

 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.i4.1
 ldelem.i8
 add
 rem.un
 conv.u2
 
pop
ldc.i4 100
ret
}
}
