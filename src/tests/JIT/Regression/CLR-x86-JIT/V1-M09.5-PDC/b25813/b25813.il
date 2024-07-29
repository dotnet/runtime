// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25813' {}
.class public ILGEN_0xe1f6df8c {

.field static	float64 field_0x9

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	22

 .locals (int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11,int32 ecode)
 
 ldc.i4.1
 stloc ecode
 .try {
 
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x8
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x9
ldc.i4 0x25e21ab2 
stloc local_0xa
ldc.i4 0x51b3ad8 
stloc local_0xb
ldc.i4 0x32dc3aff 
stloc local_0xd
ldc.i4 0x61e14524 
stloc local_0xe
ldc.i8 0x37b7163e5081d5d 
stloc local_0xf
ldc.r8 float64(0x2b53cb964904d42) 
stloc local_0x11

ldc.r8 float64(0x1ad77e34868176f) 
stsfld float64 ILGEN_0xe1f6df8c::field_0x9


 ldc.r8 float64(0x62f7169b46f23b20)
 conv.ovf.u8
 ldc.i4.1
 conv.ovf.i2
 conv.u8
 ldloc local_0xf
 conv.ovf.u
 ldsfld float64 ILGEN_0xe1f6df8c::field_0x9
 ldc.r8 float64(0x365f1e0e736f54eb)
 clt.un
 ldloc local_0x11
 conv.i1
 and
 conv.ovf.u1

Start_Orphan_3a:
 ldc.i4 55
 conv.u1
 newarr [mscorlib]System.Int32
 pop
End_Orphan_3a:

 ldloc local_0x7
 ldloc local_0xb
 conv.u2
 ldelem.i8
 conv.ovf.u4.un
 sub.ovf
 clt
 shl
 add
 conv.ovf.u2
 stloc local_0xe
End_Orphan_b:
 ldloc local_0x8
 ldlen
 
 pop
 leave xx
 } catch [mscorlib]System.OverflowException {
 pop
 ldc.i4.0
 stloc ecode
 leave xx
 }
 
xx:
 ldloc ecode
ldc.i4 100
add
 ret
}
}
