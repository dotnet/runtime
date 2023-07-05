// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25701' {}
.class public ILGEN_0x86457e59 {

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
.maxstack	12

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
ldc.i4 0x6dfc5107 
stloc local_0xa
ldc.i4 0x52d44521 
stloc local_0xb
ldc.i4 0x405b58cd 
stloc local_0xc
ldc.i4 0x2c5c4931 
stloc local_0xd
ldc.i4 0x40f01548 
stloc local_0xe
ldc.i8 0x641e3324fc271a1 
stloc local_0xf
ldc.r4 float32(0x12a96195) 
stloc local_0x10
ldc.r8 float64(0x5db71cdc5bbc2472) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x3ec622e6 
stsfld unsigned int8 ILGEN_0x86457e59::field_0x0
ldc.i4 0x31eb60cf 
stsfld unsigned int16 ILGEN_0x86457e59::field_0x1
ldc.i4 0x59521060 
stsfld unsigned int32 ILGEN_0x86457e59::field_0x2
ldc.i8 0x38c667ee183147b 
stsfld unsigned int64 ILGEN_0x86457e59::field_0x3
ldc.i4 0x2f342402 
stsfld int8 ILGEN_0x86457e59::field_0x4
ldc.i4 0x4849345f 
stsfld int16 ILGEN_0x86457e59::field_0x5
ldc.i4 0x4b741b3e 
stsfld int32 ILGEN_0x86457e59::field_0x6
ldc.i8 0x5d9813a811772 
stsfld int64 ILGEN_0x86457e59::field_0x7
ldc.r4 float32(0x755971b1) 
stsfld float32 ILGEN_0x86457e59::field_0x8
ldc.r8 float64(0x71943b0e13052978) 
stsfld float64 ILGEN_0x86457e59::field_0x9
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x86457e59::field_0xa
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x86457e59::field_0xb
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x86457e59::field_0xc
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x86457e59::field_0xd
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x86457e59::field_0xe
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x86457e59::field_0xf
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x86457e59::field_0x10
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x86457e59::field_0x11
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x86457e59::field_0x12
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x86457e59::field_0x13

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldsflda int32 ILGEN_0x86457e59::field_0x6
Start_Orphan_1:
 nop
End_Orphan_1:
 ldsfld unsigned int16 ILGEN_0x86457e59::field_0x1
 ldc.i4.7
 mul.ovf
 ldsfld unsigned int64 ILGEN_0x86457e59::field_0x3
 conv.ovf.i4
 ceq
 stind.i4
End_Orphan_0:
Start_Orphan_2:
 ldsfld int64 ILGEN_0x86457e59::field_0x7
 stsfld int64 ILGEN_0x86457e59::field_0x7
End_Orphan_2:
Start_Orphan_3:
 nop
End_Orphan_3:
 ldsfld float32[] ILGEN_0x86457e59::field_0x12
 ldc.i4.8
 ldelema [mscorlib]System.Single
 ldind.r4
 ldc.r4 float32(0xdbc7fb3)
 ldsfld float32 ILGEN_0x86457e59::field_0x8
 div
 ldloc local_0x10
 ldsfld float32 ILGEN_0x86457e59::field_0x8
 sub
 add
 ceq
 conv.r4
Start_Orphan_6:
Start_Orphan_7:
 ldsfld unsigned int8[] ILGEN_0x86457e59::field_0xa
 pop
End_Orphan_7:
 ldloc local_0x0
 ldlen
 ldsfld unsigned int16 ILGEN_0x86457e59::field_0x1
 ldsfld unsigned int16 ILGEN_0x86457e59::field_0x1
 clt.un
 sub.ovf.un
 stloc local_0xc
End_Orphan_6:
Start_Orphan_9:
 ldloc local_0x10
 conv.i8
 stsfld int64 ILGEN_0x86457e59::field_0x7
End_Orphan_9:

Start_Orphan_b:
 ldloc local_0xb
 stsfld unsigned int32 ILGEN_0x86457e59::field_0x2
End_Orphan_b:

 ldc.i4.6
 ldsfld unsigned int16 ILGEN_0x86457e59::field_0x1
 rem.un
 conv.r4
 ldsfld float32 ILGEN_0x86457e59::field_0x8
 conv.r4
 neg
 mul
Start_Orphan_d:
 ldsfld float64[] ILGEN_0x86457e59::field_0x13
 ldc.i4.3
 ldelema [mscorlib]System.Double
 pop
End_Orphan_d:
 ldc.r4 float32(0x60b14722)
 ldloc local_0x8
 ldloc local_0xa
 ldelema [mscorlib]System.Single
 ldind.r4
 mul
 mul
 cgt.un
Start_Orphan_f:
 ldsflda int64 ILGEN_0x86457e59::field_0x7
Start_Orphan_10:
 ldsflda unsigned int8 ILGEN_0x86457e59::field_0x0
 ldsfld unsigned int16 ILGEN_0x86457e59::field_0x1
 stind.i1
End_Orphan_10:
 ldsfld unsigned int64 ILGEN_0x86457e59::field_0x3
 stind.i8
End_Orphan_f:
 ldc.i4.0
 conv.ovf.u8.un
 conv.ovf.i1.un
 ldc.r4 float32(0x5f764d54)
 conv.i
 ldc.i4.0
 conv.ovf.i2.un
 sub
 mul
 conv.r8
 conv.ovf.u1
 sub.ovf.un
 
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
