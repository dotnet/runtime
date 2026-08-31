// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27873' {}
.class public ILGEN_0x3aa9c940 {

//BEGIN STATIC FIELDS
.field static	int16 field_0x0
.field static	float64 field_0x1
.field static	unsigned int32 field_0x2
.field static	unsigned int16 field_0x3
.field static	float32[] field_0x4
.field static	unsigned int32[] field_0x5
.field static	int16[] field_0x6
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	17

 .locals (unsigned int8[] local_0x0,unsigned int16[] local_0x1,unsigned int32[] local_0x2,unsigned int64[] local_0x3,int8[] local_0x4,int16[] local_0x5,int32[] local_0x6,int64[] local_0x7,float32[] local_0x8,float64[] local_0x9,unsigned int8 local_0xa,unsigned int16 local_0xb,int8 local_0xc,int16 local_0xd,int32 local_0xe,int64 local_0xf,float32 local_0x10,float64 local_0x11)
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
ldc.i4 0x1f1e51 
stloc local_0xa
ldc.i4 0x5007d98 
stloc local_0xb
ldc.i4 0x6b5445e3 
stloc local_0xc
ldc.i4 0x68033a62 
stloc local_0xd
ldc.i4 0x223543ee 
stloc local_0xe
ldc.i8 0xcb3440350d2ffa 
stloc local_0xf
ldc.r4 float32(0x552f418d) 
stloc local_0x10
ldc.r8 float64(0x787f5b6a623a26ec) 
stloc local_0x11

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x57062988 
stsfld int16 ILGEN_0x3aa9c940::field_0x0
ldc.r8 float64(0x20e27dd653724071) 
stsfld float64 ILGEN_0x3aa9c940::field_0x1
ldc.i4 0x5ee62bc7 
stsfld unsigned int32 ILGEN_0x3aa9c940::field_0x2
ldc.i4 0x39367a4f 
stsfld unsigned int16 ILGEN_0x3aa9c940::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x3aa9c940::field_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x3aa9c940::field_0x5
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x3aa9c940::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_e:
 ldloc local_0xf
 conv.u1
 ldc.i4.3
 ldloc local_0xa
 rem.un
 mul.ovf.un
 stloc local_0xc
End_Orphan_e:
 ldloc local_0xf
 conv.u2
 ldloc local_0x11
 ldsfld float64 ILGEN_0x3aa9c940::field_0x1
 rem
 conv.r4
 conv.u
 rem.un
Start_Orphan_13:
 ldloc local_0x4
 ldc.i4.0
 ldelema [mscorlib]System.SByte
 pop
End_Orphan_13:
 ldloc local_0xa
 ldc.r8 float64(0xb492e9927bd7465)
 conv.u8
 ldloc local_0x1
 ldloc local_0xc
 ldelem.u2
 shr
 conv.i
 sub
 cgt
 conv.ovf.u
 ldc.i4.8
 sub.ovf.un
Start_Orphan_17:
Start_Orphan_18:
 ldsfld unsigned int32 ILGEN_0x3aa9c940::field_0x2
 conv.i8
 conv.i2
 conv.u1
 newarr [mscorlib]System.UInt32
 stloc local_0x2
End_Orphan_18:
 ldloc local_0x0
 ldsfld unsigned int32 ILGEN_0x3aa9c940::field_0x2
 conv.i2
 ldc.i4.4
 mul.ovf
 ldloc local_0x1
 ldc.i8 0x30f9636262c4467
 conv.r4
 conv.ovf.i4
 conv.ovf.u4.un
 ldelema [mscorlib]System.UInt16
 ldind.u2
 stelem.i1
End_Orphan_17:
 ldsflda int16 ILGEN_0x3aa9c940::field_0x0
 ldind.i2
 ldsfld unsigned int16 ILGEN_0x3aa9c940::field_0x3
 ldloc local_0xa
 add
 not
 conv.ovf.i1
 add.ovf
 conv.ovf.u.un
Start_Orphan_26:
 ldloc local_0xa
 conv.u1
 newarr [mscorlib]System.SByte
 ldc.i4.3
 ldloc local_0x10
 conv.ovf.i2
 stelem.i1
End_Orphan_26:
 ldc.i4.5
 conv.ovf.u1
 conv.ovf.u8.un
 conv.ovf.u.un
 ldloc local_0xf
 conv.r4
 ldc.i4.1
 conv.r4
 add
 conv.i
 mul.ovf
 ldloc local_0x10
 ldsfld float64 ILGEN_0x3aa9c940::field_0x1
 conv.r4
 conv.u2
 conv.r4
 clt
 sub
 and
 ldloc local_0xe
 conv.r8
 conv.i1
 conv.r4
 ldc.i4 0x5a942a6a
 ldloc local_0xe
 mul.ovf
 conv.u8
 conv.r4
 div
 ckfinite
 ldsfld unsigned int32 ILGEN_0x3aa9c940::field_0x2
 conv.ovf.u4.un
 conv.r4
 conv.r4
 ldsfld float32[] ILGEN_0x3aa9c940::field_0x4
 ldc.r4 float32(0x47e6d)
 conv.ovf.i4
 ldelem.r4
 sub
Start_Orphan_47:
 ldloc local_0x3
 ldc.i4.2
 ldelema [mscorlib]System.UInt64
 ldloca local_0xf
 ldind.i8
 stind.i8
End_Orphan_47:
 ldloc local_0x0
 ldlen
 conv.r4
 rem
 cgt.un
 cgt
 clt
 pop
 leave stop
 } catch [mscorlib]System.DivideByZeroException {
 pop
 leave stop
 }
 stop:
 ldc.i4 100
ret
}
}
