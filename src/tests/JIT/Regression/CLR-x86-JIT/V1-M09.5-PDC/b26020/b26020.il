// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b26020' {}
.class public ILGEN_0x203408cc {

//BEGIN STATIC FIELDS
.field static	unsigned int8 field_0x0
.field static	unsigned int16 field_0x1
.field static	unsigned int32 field_0x2
.field static	unsigned int64 field_0x3
.field static	int8 field_0x4
.field static	int16 field_0x5
.field static	int32 field_0x6
.field static	int64 field_0x7
.field static	float32 field_0x8
.field static	float64 field_0x9
.field static	unsigned int8[] field_0xa
.field static	unsigned int16[] field_0xb
.field static	unsigned int32[] field_0xc
.field static	unsigned int64[] field_0xd
.field static	int8[] field_0xe
.field static	int16[] field_0xf
.field static	int32[] field_0x10
.field static	int64[] field_0x11
.field static	float32[] field_0x12
.field static	float64[] field_0x13
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	22

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11,int32 ecode)
 
 ldc.i4.1
 stloc ecode
 .try {
 
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x5
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x8
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x9
ldc.i4 0x8122fc1 
stloc local_0xa
ldc.i4 0x22a351d4 
stloc local_0xb
ldc.i4 0xf317368 
stloc local_0xc
ldc.i4 0x1e84a79 
stloc local_0xd
ldc.i4 0x3612203b 
stloc local_0xe
ldc.i8 0x544a4ae275ce260c 
stloc local_0xf
ldc.r4 float32(0x56cb5d18) 
stloc local_0x10
ldc.r8 float64(0x3de234496b1263d1) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x5aaf2ac9 
stsfld unsigned int8 ILGEN_0x203408cc::field_0x0
ldc.i4 0x5bc6c19 
stsfld unsigned int16 ILGEN_0x203408cc::field_0x1
ldc.i4 0xc317ba2 
stsfld unsigned int32 ILGEN_0x203408cc::field_0x2
ldc.i8 0x6360584961f913ab 
stsfld unsigned int64 ILGEN_0x203408cc::field_0x3
ldc.i4 0xc82f7 
stsfld int8 ILGEN_0x203408cc::field_0x4
ldc.i4 0xcf45405 
stsfld int16 ILGEN_0x203408cc::field_0x5
ldc.i4 0x6ab54028 
stsfld int32 ILGEN_0x203408cc::field_0x6
ldc.i8 0x14c354427d0443fe 
stsfld int64 ILGEN_0x203408cc::field_0x7
ldc.r4 float32(0x31c158ff) 
stsfld float32 ILGEN_0x203408cc::field_0x8
ldc.r8 float64(0xe6467b58bb5308) 
stsfld float64 ILGEN_0x203408cc::field_0x9
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x203408cc::field_0xa
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x203408cc::field_0xb
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x203408cc::field_0xc
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x203408cc::field_0xd
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x203408cc::field_0xe
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x203408cc::field_0xf
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x203408cc::field_0x10
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x203408cc::field_0x11
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x203408cc::field_0x12
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x203408cc::field_0x13

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldloc local_0x5
Start_Orphan_e:
 ldloc local_0x8
 ldc.i4.7
 conv.ovf.i1
 ldloc local_0x10
 stelem.r4
End_Orphan_e:
 ldsfld unsigned int8[] ILGEN_0x203408cc::field_0xa
 ldloc local_0x9
 ldlen
 ldelema [mscorlib]System.Byte
 ldind.u1
 ldelema [mscorlib]System.Int16
Start_Orphan_13:
 ldloc local_0x9
 ldc.i4.3
 ldelema [mscorlib]System.Double
 ldsfld float64[] ILGEN_0x203408cc::field_0x13
 ldc.i4.2
 ldelem.r8
 stind.r8
End_Orphan_13:
 ldloca local_0xb
 ldind.u2
 stind.i2
End_Orphan_0:
Start_Orphan_1f:
 ldsfld int32[] ILGEN_0x203408cc::field_0x10
 ldsfld int64 ILGEN_0x203408cc::field_0x7
 conv.ovf.i1
 ldsfld int16 ILGEN_0x203408cc::field_0x5
 ldloc local_0xe
 cgt
 ldc.i4 0x79785407
 conv.i4
 add
 ceq
 ldelema [mscorlib]System.Int32
 pop
End_Orphan_1f:
 ldloc local_0x10
 conv.ovf.u8
 conv.u1
Start_Orphan_38:
Start_Orphan_39:
 ldloc local_0x7
 ldsfld int32[] ILGEN_0x203408cc::field_0x10
 ldlen
 conv.ovf.u4.un
 ldsfld int32 ILGEN_0x203408cc::field_0x6
 ldloc local_0x11
 dup
 ceq
 add.ovf
 ldc.i4.7
 ldloca local_0xa
 ldind.u1
 ldloc local_0xb
 or
 shl
 and
 ceq
 ldc.i4.4
 conv.u8
 stelem.i8
End_Orphan_39:

Start_Orphan_49:
 ldloc local_0x8
 ldc.i4 55
 ldloc local_0x10
 stelem.r4
End_Orphan_49:

End_Orphan_38:

pop
 leave xx
 } catch [mscorlib]System.IndexOutOfRangeException {
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


