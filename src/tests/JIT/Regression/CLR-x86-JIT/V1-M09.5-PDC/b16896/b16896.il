// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16896' {}
.class public ILGEN_0x38e6a02d {

.field static	unsigned int64 field_0x3
.field static	int16 field_0x5
.field static	float32 field_0x8

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	14

 .locals (unsigned int8 local_0xa,unsigned int16 local_0xb,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10)

 ldc.i4.0  stloc.0
 ldc.i4.0  stloc.1
 ldc.i4.0  stloc.2
 ldc.i4.0  stloc.3
 ldc.i8 0  stloc 4
 ldc.r4 0  stloc 5

.try
{

ldc.i4 0x4a3e70aa 
stloc local_0xa
ldc.i4 0x496a6b4f 
stloc local_0xd
ldc.i4 0x3be55136 
stloc local_0xe
ldc.i8 0x38f9668b658c7c52 
stloc local_0xf
ldc.r4 float32(0x7b5d5f1c) 
stloc local_0x10

ldc.i8 0x2e955def5c8d4af1 
stsfld unsigned int64 ILGEN_0x38e6a02d::field_0x3
ldc.i4 0x4fcc1252 
stsfld int16 ILGEN_0x38e6a02d::field_0x5
ldc.r4 float32(0x7aa321fe) 
stsfld float32 ILGEN_0x38e6a02d::field_0x8

Start_Orphan_0:
 ldloc local_0xa
 conv.u1
 newarr [mscorlib]System.UInt16
Start_Orphan_6:
Start_Orphan_7:
 ldsfld unsigned int64 ILGEN_0x38e6a02d::field_0x3
 stloc local_0xf
End_Orphan_7:
End_Orphan_6:
 ldsfld unsigned int64 ILGEN_0x38e6a02d::field_0x3
 conv.ovf.i
 conv.r4
 ldsfld float32 ILGEN_0x38e6a02d::field_0x8
 mul
 conv.ovf.u4
 ldelema [mscorlib]System.UInt16
 pop
End_Orphan_0:
 ldloc local_0x10
 conv.ovf.u8
 ldsflda unsigned int64 ILGEN_0x38e6a02d::field_0x3
 ldind.u8
 ldc.i4.1
 conv.i8
 mul.ovf
 or
 ldsfld int16 ILGEN_0x38e6a02d::field_0x5
 ldloc local_0xb
 div
 conv.ovf.u8.un
 ldloc local_0xf
 conv.ovf.u8
 ldc.i4.8
 conv.ovf.i8
 add.ovf
 sub.ovf
 div
 ldc.i4.s -88
 ldloc local_0xe
 clt
 conv.ovf.u8.un
 mul.ovf.un
 conv.ovf.u1
 conv.u1
 pop
 
 leave Beyond
} catch [mscorlib]System.OverflowException {
	pop
 	leave Beyond
}
Beyond:
ldc.i4 100
ret
}
}
