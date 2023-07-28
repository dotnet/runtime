// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16928' {}
.class public ILGEN_0x38e56b77 {

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
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	18

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11, int32 ecode)
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
ldc.i4 0x352f6f76 
stloc local_0xa
ldc.i4 0x5af468aa 
stloc local_0xb
ldc.i4 0x8284dd5 
stloc local_0xc
ldc.i4 0x46f8fbf 
stloc local_0xd
ldc.i4 0xcae2243 
stloc local_0xe
ldc.i8 0x623875f5401af4 
stloc local_0xf
ldc.r4 float32(0x90e3ab9) 
stloc local_0x10
ldc.r8 float64(0x5a391a03d2e4777) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x7435a94 
stsfld unsigned int8 ILGEN_0x38e56b77::field_0x0
ldc.i4 0x26225364 
stsfld unsigned int16 ILGEN_0x38e56b77::field_0x1
ldc.i4 0x5a053f75 
stsfld unsigned int32 ILGEN_0x38e56b77::field_0x2
ldc.i8 0x7b2550d47ea452b 
stsfld unsigned int64 ILGEN_0x38e56b77::field_0x3
ldc.i4 0x32e25308 
stsfld int8 ILGEN_0x38e56b77::field_0x4
ldc.i4 0x264e4ab4 
stsfld int16 ILGEN_0x38e56b77::field_0x5
ldc.i4 0x7643ec4 
stsfld int32 ILGEN_0x38e56b77::field_0x6
ldc.i8 0x29202bde50f26511 
stsfld int64 ILGEN_0x38e56b77::field_0x7
ldc.r4 float32(0x1367a3e) 
stsfld float32 ILGEN_0x38e56b77::field_0x8
ldc.r8 float64(0x33f459275298cd0) 
stsfld float64 ILGEN_0x38e56b77::field_0x9

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:

Start_Orphan_2:
 ldloc local_0x7
 ldc.r4 float32(0x50332250)
 conv.i4
 ldc.r4 float32(0x50332250)
 conv.ovf.u8
 stelem.i8
End_Orphan_2:

 ldc.r8 float64(0x220d69cd7baa4727)
 conv.u1
 newarr [mscorlib]System.Single
 ldlen
 conv.u1
 newarr [mscorlib]System.Int64
 stloc local_0x7
End_Orphan_0:

 ldloc local_0xf
 conv.ovf.i.un
 conv.i8
 ldloc local_0xe
 conv.ovf.i2.un
 ldloc local_0x3
 ldloc local_0xe
 ldelem.u8
 conv.ovf.u.un
 ceq
 shl
 conv.u1
Start_Orphan_16:
Start_Orphan_17:
 ldloca local_0xe
 ldind.i4
 conv.ovf.i2
 conv.u1
 newarr [mscorlib]System.UInt64
 ldsfld unsigned int64 ILGEN_0x38e56b77::field_0x3
 conv.u1
 newarr [mscorlib]System.UInt16
 ldlen
 ldc.r4 float32(0x186f7e83)
 conv.ovf.u8
 stelem.i8
End_Orphan_17:
 ldloc local_0x9
 ldc.i8 0x4a273a722ac8181d
 conv.ovf.u1
 conv.r.un
 ldc.r8 float64(0x572765381d675ddd)
 clt
 conv.i4
 conv.ovf.i2
Start_Orphan_2a:
 ldloc local_0x7
 ldc.i4.7
 ldelem.i8
 stloc local_0xf
End_Orphan_2a:
 ldsfld float32 ILGEN_0x38e56b77::field_0x8
 conv.ovf.u1
 ldloc local_0xf
 ldc.i8 0x7b356ccc7f3822da
 sub.ovf.un
 conv.ovf.u8.un
 conv.ovf.i2.un
Start_Orphan_2f:
 ldsfld int32 ILGEN_0x38e56b77::field_0x6
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.r8 float64(0x75924dd845011960)
 conv.u1
 ldc.i8 0xfdd30628314753
 stelem.i8
End_Orphan_2f:
 clt
 cgt
 ldc.i4.5
 conv.ovf.u4
 conv.ovf.u8
 not
 conv.r4
 conv.r8
 conv.r8
 stelem.r8
End_Orphan_16:
Start_Orphan_36:
 ldloc local_0x8
 ldlen
 conv.u1
 newarr [mscorlib]System.Byte
 stloc local_0x0
End_Orphan_36:
 ldsfld int32 ILGEN_0x38e56b77::field_0x6
 conv.r4
 ldc.r4 float32(0x18d63f42)
 ldloca local_0x10
 ldind.r4
 ldloc local_0x8
 ldloc local_0xb
 ldelem.r4
 rem
 ldloc local_0x8
 ldsfld unsigned int32 ILGEN_0x38e56b77::field_0x2
 conv.i4
 ldelem.r4
 add
 rem
 mul
 conv.ovf.i1
 xor

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
