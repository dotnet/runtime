// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xc2d6ebd8 {}
.class ILGEN_0xc2d6ebd8 {

//BEGIN STATIC FIELDS
.field static	unsigned int8 field_0x0
.field static	unsigned int64[] field_0x1
.field static	unsigned int32 field_0x2
.field static	unsigned int32 field_0x3
.field static	unsigned int64[] field_0x4
//END STATIC FIELDS
.method static int32 Method_0x6228(unsigned int16 Arg_0x0, int16 Arg_0x1, int32 Arg_0x2, int64 Arg_0x3, int16 Arg_0x4, unsigned int64 Arg_0x5, unsigned int32 Arg_0x6) {
.maxstack	19

 .locals (unsigned int16[] local_0x0)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x0

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0x25e665a 
stsfld unsigned int8 ILGEN_0xc2d6ebd8::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x1
ldc.i4 0x13b7b89 
stsfld unsigned int32 ILGEN_0xc2d6ebd8::field_0x2
ldc.i4 0x6b8d7e1d 
stsfld unsigned int32 ILGEN_0xc2d6ebd8::field_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x4

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldsflda unsigned int32 ILGEN_0xc2d6ebd8::field_0x2
Start_Orphan_4:
 ldarg Arg_0x4
 conv.u1
 newarr [mscorlib]System.UInt16
 pop
End_Orphan_4:
 ldc.r8 float64(0x46e6354b38e51551)
 conv.ovf.i2
 stind.i4
End_Orphan_2:
 ldsflda unsigned int8 ILGEN_0xc2d6ebd8::field_0x0
Start_Orphan_5:
 ldsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x1
Start_Orphan_9:
 ldc.i4.7
 starg Arg_0x4
End_Orphan_9:
 ldarg Arg_0x0
 conv.i2
 conv.ovf.i2
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_5:
 ldc.i8 0x520e561877a3448
 conv.ovf.i4.un
 conv.u8
Start_Orphan_d:
 ldc.i4.s -127
 starg Arg_0x1
End_Orphan_d:
 ldarg Arg_0x5
 ldarg Arg_0x3
 add
 ldc.i4.8
 conv.ovf.u8.un
 add.ovf.un
Start_Orphan_f:
 ldc.i4.2
 stsfld unsigned int8 ILGEN_0xc2d6ebd8::field_0x0
End_Orphan_f:
 xor
 conv.ovf.i2
 stind.i1
End_Orphan_1:
Start_Orphan_10:
Start_Orphan_13:
 ldarga Arg_0x5
 ldarg Arg_0x5
 stind.i8
End_Orphan_13:
 ldc.i4.1
 ldc.i4.1
 sub
 conv.u2
 ldsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x1
 ldlen
 sub
Start_Orphan_16:
 ldsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x1
 ldc.i4.3
 ldelema [mscorlib]System.UInt64
 ldarg Arg_0x3
 conv.ovf.u8.un
 stind.i8
End_Orphan_16:
 ldsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x4
 ldlen
 conv.u1
 mul.ovf
 stsfld unsigned int32 ILGEN_0xc2d6ebd8::field_0x2
End_Orphan_10:
Start_Orphan_19:
 nop
End_Orphan_19:
 ldc.i4.s -37
 conv.ovf.u1
 conv.u
 ldc.i8 0x6a8a9ed21c91602
 conv.i4
 conv.i
 rem
 conv.r8
 conv.r8
 ckfinite
 conv.ovf.u8
 starg Arg_0x5
End_Orphan_0:
 ldc.i4.2
 conv.r4
 ckfinite
 conv.ovf.i8
 conv.ovf.i4.un
Start_Orphan_23:
 ldsflda unsigned int32 ILGEN_0xc2d6ebd8::field_0x3
 ldsfld unsigned int8 ILGEN_0xc2d6ebd8::field_0x0
 stind.i4
End_Orphan_23:
 conv.r4
 ldarg Arg_0x4
 conv.r8
 ckfinite
 conv.r4
Start_Orphan_32:
 ldsfld unsigned int64[] ILGEN_0xc2d6ebd8::field_0x4
 ldc.i8 0x4fe923121c8469d
 conv.i4
 ldc.i8 0x669fe1f5300bd6
 conv.ovf.u8
 stelem.i8
End_Orphan_32:
 ldc.r8 float64(0x315d2a2d4e549a)
 conv.ovf.u
 ldarg Arg_0x5
 conv.ovf.i2
 ldc.i4.5
 rem.un
 ldarg Arg_0x3
 conv.ovf.u
 add.ovf
 add
 conv.r4
 mul
 mul
 conv.i
 conv.ovf.i2
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x5fbf34e3 
ldc.i4 0xa0c21ae 
ldc.i4 0x79402563 
ldc.i8 0x6de01bb1353e626e 
ldc.i4 0x2267402e 
ldc.i8 0x6c6b7b7c415d1540 
ldc.i4 0x27f26baa 

call int32 ILGEN_0xc2d6ebd8::Method_0x6228(unsigned int16 Arg_0x0, int16 Arg_0x1, int32 Arg_0x2, int64 Arg_0x3, int16 Arg_0x4, unsigned int64 Arg_0x5, unsigned int32 Arg_0x6)
 pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
ret
}

}
