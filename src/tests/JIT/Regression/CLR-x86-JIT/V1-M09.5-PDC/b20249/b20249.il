// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b20249' {}
.class public ILGEN_0x38e574ea {

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

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11,int32 ecode)
 ldnull  stloc.0
 ldnull  stloc.1
 ldnull  stloc.2
 ldnull  stloc.3
 ldnull  stloc 4
 ldnull  stloc 5
 ldnull  stloc 6
 ldnull  stloc 7
 ldnull  stloc 8
 ldnull  stloc 9
 ldc.i4.0  stloc 10
 ldc.i4.0  stloc 11
 ldc.i4.0  stloc 12
 ldc.i4.0  stloc 13
 ldc.i4.0  stloc 14
 ldc.i8 0  stloc 15
 ldc.r4 0  stloc 16
 ldc.r8 0  stloc 17
 ldc.i4.0  stloc 18
 
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
ldc.i4 0x6a736276 
stloc local_0xa
ldc.i4 0x7f7d4840 
stloc local_0xb
ldc.i4 0x73101209 
stloc local_0xc
ldc.i4 0xdc31a9 
stloc local_0xd
ldc.i4 0x13461af6 
stloc local_0xe
ldc.i8 0x549f76566e486d4d 
stloc local_0xf
ldc.r4 float32(0x6d8f501b) 
stloc local_0x10
ldc.r8 float64(0x52b373d013622caa) 
stloc local_0x11

ldc.i4 0x12055b6b 
stsfld unsigned int16 ILGEN_0x38e574ea::field_0x1
ldc.i4 0x1841e94 
stsfld unsigned int32 ILGEN_0x38e574ea::field_0x2
ldc.i8 0x19753c0112f71be1 
stsfld unsigned int64 ILGEN_0x38e574ea::field_0x3
ldc.i4 0x412146fd 
stsfld int32 ILGEN_0x38e574ea::field_0x6
ldc.i8 0xf501f3041341d8c 
stsfld int64 ILGEN_0x38e574ea::field_0x7
ldc.r4 float32(0x7da831e8) 
stsfld float32 ILGEN_0x38e574ea::field_0x8

 ldsfld float32 ILGEN_0x38e574ea::field_0x8

Start_Orphan_0:
 ldsfld unsigned int16 ILGEN_0x38e574ea::field_0x1
 conv.u1
 newarr [mscorlib]System.UInt64
 ldsfld unsigned int32 ILGEN_0x38e574ea::field_0x2
 ldc.i8 0x735e6d8132347e25
 stelem.i8
End_Orphan_0:

 ldc.r4 float32(0xc23559a)
 conv.r8
 conv.ovf.i8
 ldloc local_0x3
 ldloc local_0xc
 conv.u1
 newarr [mscorlib]System.Int32
 ldsfld int32 ILGEN_0x38e574ea::field_0x6
 ldelem.i4
 ldelem.u8
 div

Start_Orphan_c:
 ldsflda unsigned int64 ILGEN_0x38e574ea::field_0x3
 ldc.r8 float64(0x4a854a6975515b34)
 conv.ovf.i8
 stind.i8
End_Orphan_c:

 ldsfld unsigned int64 ILGEN_0x38e574ea::field_0x3
 conv.r8
 conv.u8
 sub.ovf
 conv.r4

Start_Orphan_16:
 ldloca local_0xd
 ldloc local_0x3
 ldlen
 stind.i2
End_Orphan_16:

Start_Orphan_19:
 ldsflda int64 ILGEN_0x38e574ea::field_0x7
 ldloc local_0x9
 ldsfld unsigned int16 ILGEN_0x38e574ea::field_0x1
 ldelem.r8
 conv.ovf.i8
 stind.i8
End_Orphan_19:

Start_Orphan_1b:
 ldloc local_0x6
 ldsfld int32 ILGEN_0x38e574ea::field_0x6
 ldelema [mscorlib]System.Int32
 ldc.i8 0x23abbd07ef56a3e
 conv.i2
 stind.i4
End_Orphan_1b:

 clt
 
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
