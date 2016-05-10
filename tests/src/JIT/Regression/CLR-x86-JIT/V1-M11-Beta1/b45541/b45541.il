// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2985f7b9 {}
.class ILGEN_0x2985f7b9 {
.field static	unsigned int32[] field_0x1
.field static	float32 field_0x4

.method static int32 Method_0xe2c8c158(unsigned int64 Arg_0x0, int64 Arg_0x1, unsigned int8 Arg_0x2, int64 Arg_0x3, unsigned int64 Arg_0x4, int16 Arg_0x5, float32 Arg_0x6, unsigned int32 Arg_0x7, float32 Arg_0x8) {
.maxstack	19

 .locals (int32 local_0x0,int8 local_0x3,float32[] local_0x4,unsigned int16[] local_0x5,unsigned int64 local_0x7)

ldc.i4 0x231dd340 
stloc local_0x0
ldc.i4 0xaeee284b 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x5
ldc.i8 0x3e0ec10e49101e22 
stloc local_0x7

ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x2985f7b9::field_0x1
ldc.r4 float32(0x3d107dae) 
stsfld float32 ILGEN_0x2985f7b9::field_0x4

Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldsfld unsigned int32[] ILGEN_0x2985f7b9::field_0x1
 ldc.i4.0
 ldelem.u4
 conv.ovf.u
 ldc.i4.5
 conv.r8
 ldarg Arg_0x8
 ldc.r4 float32(0xe628a4d6)
 rem conv.r4
 div conv.r4
 ldarg Arg_0x4
 conv.r4
 ble Branch_0x6

 ldloca local_0x0
 ldind.i
 ldloca local_0x0
 ldind.i4
 ceq
 br Branch_0x7

 Branch_0x6:

 ldloc local_0x5
 ldlen
 conv.ovf.i1
 Branch_0x7:

 ble Branch_0x4

 ldc.i8 0xf0747df5222045de
 not
 conv.i2
 starg Arg_0x5
 br Branch_0x5

 Branch_0x4:

 ldloc local_0x5
 pop
 Branch_0x5:

End_Orphan_2:
 ldloc local_0x7
 ldarg Arg_0x4
 rem
 ldarg Arg_0x0
 conv.u8
 add.ovf
 conv.ovf.i2.un
 conv.ovf.i2.un
 ldloc local_0x5
Start_Orphan_d:
 ldloc local_0x3
 ldarg Arg_0x2
 rem
 stloc local_0x0
End_Orphan_d:
Start_Orphan_f:
 nop
End_Orphan_f:
 ldsfld float32 ILGEN_0x2985f7b9::field_0x4
 conv.ovf.i1
 ldelema [mscorlib]System.UInt16
 ldind.u2
 conv.ovf.u
 mul.ovf.un
 ldloc local_0x4
 ldc.r8 float64(0x1dd77ab4b503c18a)
 conv.ovf.u4
 ldelem.r4
 conv.ovf.u2
 conv.ovf.u.un
 ldsfld float32 ILGEN_0x2985f7b9::field_0x4
 conv.i
 ldloc local_0x7
 conv.i2
 div.un
 rem.un
 ble.un Branch_0x2

 ldloc local_0x4
 ldarg Arg_0x2
 conv.u8
 conv.i
Start_Orphan_28:
 ldloc local_0x5
 ldc.i4.4
 ldelema [mscorlib]System.UInt16
 ldc.i4 0x1a577c23
 stind.i2
End_Orphan_28:
 ldc.r8 float64(0x76f1914863e0ac25)
 ckfinite
 conv.r8
 stelem.r4
 br Branch_0x3

 Branch_0x2:

 ldarg Arg_0x5
 conv.ovf.u2
 conv.u1
 newarr [mscorlib]System.SByte
 pop
 Branch_0x3:

End_Orphan_1:
End_Orphan_0:
 ldc.i4.s 60
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x5b5b8f1aeee22e65 
ldc.i8 0x88a4bf4d403f66ed 
ldc.i4 0xb4bb6a9f 
ldc.i8 0xef4c03bdeeb7b2f1 
ldc.i8 0x91c560587df9f040 
ldc.i4 0x49e88dc0 
ldc.r4 float32(0x81325833) 
ldc.i4 0xcb7a2d83 
ldc.r4 float32(0xa3373992) 

call int32 ILGEN_0x2985f7b9::Method_0xe2c8c158(unsigned int64 Arg_0x0, int64 Arg_0x1, unsigned int8 Arg_0x2, int64 Arg_0x3, unsigned int64 Arg_0x4, int16 Arg_0x5, float32 Arg_0x6, unsigned int32 Arg_0x7, float32 Arg_0x8)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100
 ret
}

}
