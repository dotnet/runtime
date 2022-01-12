// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


 // Max Args = 1 

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1dd8d826 {}
.assembly extern xunit.core {}
.class ILGEN_0x1dd8d826 {

//BEGIN STATIC FIELDS
.field static	float32[] field_0x0
.field static	unsigned int16 field_0x1
.field static	int32 field_0x2
.field static	float32 field_0x3
.field static	int64 field_0x4
.field static	unsigned int32 field_0x5
.field static	unsigned int8 field_0x6
//END STATIC FIELDS
.method static int32 Method_0x1830b93d(unsigned int32 Arg_0x0) {
.maxstack	17

 .locals init(int64[] local_0x0,float64 local_0x1)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x0
ldc.r8 float64(0xcecb0b303041a4ca) 
stloc local_0x1

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x1dd8d826::field_0x0
ldc.i4 0xbb6b50c3 
stsfld unsigned int16 ILGEN_0x1dd8d826::field_0x1
ldc.i4 0x9cba2c1c 
stsfld int32 ILGEN_0x1dd8d826::field_0x2
ldc.r4 float32(0xf60c95b7) 
stsfld float32 ILGEN_0x1dd8d826::field_0x3
ldc.i8 0x3d0ecdabb5eccdcf 
stsfld int64 ILGEN_0x1dd8d826::field_0x4
ldc.i4 0xe834c21e 
stsfld unsigned int32 ILGEN_0x1dd8d826::field_0x5
ldc.i4 0xe3b11330 
stsfld unsigned int8 ILGEN_0x1dd8d826::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 nop
End_Orphan_1:
 ldc.i8 0xf1689320bb455965
 conv.ovf.i
 ldc.i4.4
 conv.ovf.u2
 blt.un Branch_0x6

 ldsfld unsigned int32 ILGEN_0x1dd8d826::field_0x5
 conv.i
 br Branch_0x7

 Branch_0x6:

Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.i4.8
 ldc.i4.6
 bne.un Branch_0x8

 ldc.i4 0x1a9af26e
 br Branch_0x9

 Branch_0x8:

 ldc.i4.6
 Branch_0x9:

 Branch_0x7:

Start_Orphan_3:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
 pop
End_Orphan_3:
Start_Orphan_4:
 nop
End_Orphan_4:
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 conv.i
Start_Orphan_5:
 nop
End_Orphan_5:
 ldc.r4 float32(0x3626ed1a)
 ldsfld float32 ILGEN_0x1dd8d826::field_0x3
 cgt.un
 div.un
 ble.un Branch_0x4

 nop
 br Branch_0x5

 Branch_0x4:

Start_Orphan_6:
 nop
End_Orphan_6:
 ldsflda unsigned int32 ILGEN_0x1dd8d826::field_0x5
Start_Orphan_7:
 nop
End_Orphan_7:
 ldc.i8 0x35f9514f5b4c3c91
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 cgt
 stind.i4
 Branch_0x5:

End_Orphan_0:
 ldc.i4.3
 ldc.i4.7
 mul.ovf
 conv.ovf.u8.un
 conv.i8
 ldc.i4.5
 conv.ovf.u2.un
 conv.u8
 ldc.i8 0x6f04dfd256d53816
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 add.ovf.un
 neg
 or
 mul.ovf.un
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 ldc.i8 0xfc9fd2d95837ffcd
 rem
 ldarga Arg_0x0
 ldind.u4
 shr.un
 ldc.i8 0x92d417ac550b739b
 ldc.i8 0xf4d9b30870bcb3f
 div
 ldc.r8 float64(0x999207f91e4ffae)
 conv.u8
 add
 or
 conv.u8
 mul
Start_Orphan_a:
Start_Orphan_b:
 ldc.r4 float32(0x8572a993)
 stloc local_0x1
End_Orphan_b:
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
 pop
End_Orphan_a:
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 ldc.i4.0
 shl
 ldsfld int32 ILGEN_0x1dd8d826::field_0x2
 shr
 conv.i8
Start_Orphan_e:
 ldc.i8 0x1dde9519fd04dd2f
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 clt
 pop
End_Orphan_e:
 ldloca local_0x1
 ldind.r8
 conv.ovf.i
 conv.ovf.u4
 shr
Start_Orphan_10:
 ldsflda unsigned int8 ILGEN_0x1dd8d826::field_0x6
 ldc.i4.3
 stind.i1
End_Orphan_10:
 ldc.i8 0xaa45c51a87a5dcb6
 ldc.i8 0x48d49ef5b95279
 add.ovf
 ldc.i4.7
 conv.ovf.i8
 xor
 conv.u8
Start_Orphan_13:
Start_Orphan_14:
 nop
End_Orphan_14:
 ldc.r4 float32(0xda034dc4)
 conv.r8
 pop
End_Orphan_13:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldc.i8 0x246a17276e91f6d6
 conv.ovf.i8
 conv.ovf.i8.un
 ldc.i8 0xe5f493a52ffd28be
 ldc.i8 0xb89852fa96082ff
 rem.un
 ldc.i4.8
 shl
 xor
 sub.ovf
 mul
 bgt Branch_0x2

Start_Orphan_16:
Start_Orphan_17:
Start_Orphan_18:
 nop
End_Orphan_18:
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
Start_Orphan_19:
 nop
End_Orphan_19:
 ldc.i8 0x27699947ac702f2
 conv.u4
 ldloc local_0x1
 neg
 stelem.r4
End_Orphan_17:
Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldarg Arg_0x0
 conv.i
 ldc.i4.2
 neg
 mul
 conv.r4
 stsfld float32 ILGEN_0x1dd8d826::field_0x3
End_Orphan_16:
Start_Orphan_1b:
Start_Orphan_1c:
 ldc.i4.8
 stsfld unsigned int8 ILGEN_0x1dd8d826::field_0x6
End_Orphan_1c:
 ldc.i4.0
 ldc.i4 0x993c458a
 mul
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 conv.ovf.u.un
 cgt.un
 pop
End_Orphan_1b:
Start_Orphan_1d:
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldloc local_0x0
 pop
End_Orphan_1d:
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldc.r8 float64(0xeee8d9efb527da7b)
 ldc.r4 float32(0xc6e4ce6d)
 cgt
 conv.i2
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i4.5
 ldsfld unsigned int8 ILGEN_0x1dd8d826::field_0x6
 rem
 ldc.i4.8
 ldc.i4.3
 div.un
 mul.ovf.un
 add.ovf
 conv.ovf.i8
 conv.r.un
 br Branch_0x3

 Branch_0x2:

Start_Orphan_21:
Start_Orphan_22:
 ldloca local_0x1
 ldsfld float32 ILGEN_0x1dd8d826::field_0x3
 stind.r8
End_Orphan_22:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
Start_Orphan_23:
 ldloc local_0x1
 stloc local_0x1
End_Orphan_23:
Start_Orphan_24:
 nop
End_Orphan_24:
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
 ldlen
 stind.i
End_Orphan_21:
Start_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldc.i4.4
 conv.u1
 newarr [mscorlib]System.Int64
 pop
End_Orphan_25:
 ldc.i4.4
 conv.ovf.i4.un
 conv.u
Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.i4.s 26
 conv.r4
 conv.u2
 cgt
 conv.ovf.u8.un
 conv.r4
 Branch_0x3:

 ldsflda float32 ILGEN_0x1dd8d826::field_0x3
 ldind.r4
 bge.un Branch_0x0

Start_Orphan_28:
 ldsflda float32 ILGEN_0x1dd8d826::field_0x3
 pop
End_Orphan_28:
Start_Orphan_29:
 nop
End_Orphan_29:
 ldc.i4.8
 ldsfld unsigned int16 ILGEN_0x1dd8d826::field_0x1
 sub.ovf
 conv.ovf.u4.un
 ldsfld float32 ILGEN_0x1dd8d826::field_0x3
 ldc.r8 float64(0x745dce6da6797edc)
 div conv.r4
 conv.i4
 sub.ovf.un
 conv.ovf.i.un
 conv.ovf.i8.un
 conv.ovf.u1
 br Branch_0x1

 Branch_0x0:

Start_Orphan_2a:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
Start_Orphan_2b:
Start_Orphan_2c:
 ldsflda unsigned int8 ILGEN_0x1dd8d826::field_0x6
 pop
End_Orphan_2c:
 ldsflda unsigned int16 ILGEN_0x1dd8d826::field_0x1
 ldind.u2
 stsfld unsigned int8 ILGEN_0x1dd8d826::field_0x6
End_Orphan_2b:
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 mul.ovf
 ldsfld int32 ILGEN_0x1dd8d826::field_0x2
 conv.i8
 mul.ovf.un
 conv.ovf.i8
 conv.i2
 stind.i4
End_Orphan_2a:
Start_Orphan_2d:
Start_Orphan_2e:
Start_Orphan_2f:
 nop
End_Orphan_2f:
 ldc.i4.6
 conv.u1
 newarr [mscorlib]System.Single
 stsfld float32[] ILGEN_0x1dd8d826::field_0x0
End_Orphan_2e:
Start_Orphan_30:
 nop
End_Orphan_30:
 ldc.i4.5
 ldc.i4.4
 clt
Start_Orphan_31:
 nop
End_Orphan_31:
 ldsflda unsigned int16 ILGEN_0x1dd8d826::field_0x1
 ldind.u2
 shr.un
 conv.u1
 newarr [mscorlib]System.Single
 stsfld float32[] ILGEN_0x1dd8d826::field_0x0
End_Orphan_2d:
Start_Orphan_32:
Start_Orphan_33:
 ldc.i4.6
 stsfld int32 ILGEN_0x1dd8d826::field_0x2
End_Orphan_33:
Start_Orphan_34:
 nop
End_Orphan_34:
 ldsfld float32 ILGEN_0x1dd8d826::field_0x3
 conv.u1
 conv.u1
 newarr [mscorlib]System.Int64
 stloc local_0x0
End_Orphan_32:
Start_Orphan_35:
Start_Orphan_36:
 nop
End_Orphan_36:
 ldloca local_0x1
 pop
End_Orphan_35:
Start_Orphan_37:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
 ldc.i4 0x610cbd74
 stind.i4
End_Orphan_37:
 ldc.i4.0
 conv.ovf.i2.un
 not
 ldloc local_0x0
 ldlen
 clt
 conv.ovf.u1.un
Start_Orphan_38:
 ldsflda unsigned int16 ILGEN_0x1dd8d826::field_0x1
 pop
End_Orphan_38:
Start_Orphan_39:
 ldsflda unsigned int16 ILGEN_0x1dd8d826::field_0x1
 pop
End_Orphan_39:
Start_Orphan_3a:
 nop
End_Orphan_3a:
 ldsflda unsigned int8 ILGEN_0x1dd8d826::field_0x6
 ldind.u1
Start_Orphan_3b:
 ldloc local_0x0
 pop
End_Orphan_3b:
Start_Orphan_3c:
 nop
End_Orphan_3c:
 ldc.i4.3
 not
 conv.i
 mul
Start_Orphan_3d:
 nop
End_Orphan_3d:
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 conv.ovf.u
 ldc.i4.m1
 conv.ovf.u.un
 div
Start_Orphan_3f:
 ldloc local_0x0
 pop
End_Orphan_3f:
Start_Orphan_40:
 nop
End_Orphan_40:
 ldsfld unsigned int16 ILGEN_0x1dd8d826::field_0x1
 conv.ovf.i4
Start_Orphan_41:
 nop
End_Orphan_41:
 ldsfld int32 ILGEN_0x1dd8d826::field_0x2
 conv.ovf.i1
 or
 add.ovf
 sub
 conv.i
 cgt
 Branch_0x1:

Start_Orphan_42:
Start_Orphan_43:
 ldloca local_0x1
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 ldc.i8 0x1d942dfc24a09dec
 div
 conv.ovf.i2
 ldelem.r4
 stind.r8
End_Orphan_43:
 ldsflda float32 ILGEN_0x1dd8d826::field_0x3
Start_Orphan_44:
 ldsflda int64 ILGEN_0x1dd8d826::field_0x4
Start_Orphan_45:
Start_Orphan_46:
 nop
End_Orphan_46:
 ldc.i4.s 116
 ldc.i4.m1
 or
 pop
End_Orphan_45:
Start_Orphan_47:
 ldsflda unsigned int8 ILGEN_0x1dd8d826::field_0x6
 ldc.i4.5
 stind.i1
End_Orphan_47:
 ldc.i4.5
 conv.ovf.u8.un
 ldc.i4.3
 conv.ovf.i8.un
 mul.ovf
 conv.ovf.u8
 stind.i8
End_Orphan_44:
Start_Orphan_48:
Start_Orphan_49:
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
 stsfld float32[] ILGEN_0x1dd8d826::field_0x0
End_Orphan_49:
 ldsflda unsigned int8 ILGEN_0x1dd8d826::field_0x6
 pop
End_Orphan_48:
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
Start_Orphan_4a:
Start_Orphan_4b:
 ldc.r4 float32(0x950c71dc)
 stloc local_0x1
End_Orphan_4b:
 ldc.i4.s 80
 conv.u1
 newarr [mscorlib]System.Single
 ldsfld unsigned int16 ILGEN_0x1dd8d826::field_0x1
 ldelema [mscorlib]System.Single
 pop
End_Orphan_4a:
Start_Orphan_4c:
 ldsflda unsigned int8 ILGEN_0x1dd8d826::field_0x6
 ldc.i8 0x23a13397e96e9e2a
 conv.ovf.u4
 stind.i1
End_Orphan_4c:
Start_Orphan_4d:
 ldsfld int64 ILGEN_0x1dd8d826::field_0x4
 stsfld int64 ILGEN_0x1dd8d826::field_0x4
End_Orphan_4d:
 ldsfld float32[] ILGEN_0x1dd8d826::field_0x0
Start_Orphan_4e:
 ldsflda int64 ILGEN_0x1dd8d826::field_0x4
 ldc.i8 0x8c89cdfd23a1ce6
 stind.i8
End_Orphan_4e:
 ldc.r8 float64(0x6d85b3d48134f7d5)
 conv.ovf.i
 conv.ovf.u2.un
 ldelem.r4
 conv.u1
 ldelem.r4
 stind.r4
End_Orphan_42:
Start_Orphan_4f:
 ldarga Arg_0x0
 pop
End_Orphan_4f:
Start_Orphan_50:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
 pop
End_Orphan_50:
Start_Orphan_51:
Start_Orphan_52:
 ldarga Arg_0x0
 pop
End_Orphan_52:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
 ldsfld float32 ILGEN_0x1dd8d826::field_0x3
 conv.ovf.i1
 conv.u1
 stind.i4
End_Orphan_51:
Start_Orphan_53:
 nop
End_Orphan_53:
Start_Orphan_54:
 nop
End_Orphan_54:
 ldc.i4.0
 conv.u
 ldc.i4.m1
 ldc.i4.1
 mul.ovf.un
 xor
Start_Orphan_55:
 ldloc local_0x1
 stsfld float32 ILGEN_0x1dd8d826::field_0x3
End_Orphan_55:
 ldc.i4.m1
 ldc.i4.5
 rem.un
 conv.i2
 rem.un
Start_Orphan_56:
Start_Orphan_57:
 nop
End_Orphan_57:
 ldsflda int32 ILGEN_0x1dd8d826::field_0x2
 pop
End_Orphan_56:
Start_Orphan_58:
 nop
End_Orphan_58:
 ldc.i4.s 73
 conv.ovf.i8
 conv.ovf.u4
 clt
 conv.ovf.u1.un
Start_Orphan_59:
 nop
End_Orphan_59:
Start_Orphan_5a:
Start_Orphan_5b:
 nop
End_Orphan_5b:
 ldc.i4.7
 ldc.i4.6
 cgt
 conv.u1
 newarr [mscorlib]System.Int64
 pop
End_Orphan_5a:
 ldloc local_0x1
Start_Orphan_5c:
Start_Orphan_5d:
 nop
End_Orphan_5d:
 ldc.i4.8
 conv.u1
 newarr [mscorlib]System.Single
Start_Orphan_5e:
 nop
End_Orphan_5e:
 ldc.i8 0x4e720f59b3dbf1b
 conv.u2
Start_Orphan_5f:
 nop
End_Orphan_5f:
 ldc.i4.7
 conv.r8
 stelem.r4
End_Orphan_5c:
Start_Orphan_60:
 ldloc local_0x1
 stsfld float32 ILGEN_0x1dd8d826::field_0x3
End_Orphan_60:
Start_Orphan_61:
 nop
End_Orphan_61:
 ldc.i4.5
 conv.ovf.i4.un
 ldsfld int32 ILGEN_0x1dd8d826::field_0x2
 conv.ovf.i2.un
 sub
 conv.r8
 neg
 clt
 ceq
 conv.ovf.i4
 add.ovf
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x3c88442 

call int32 ILGEN_0x1dd8d826::Method_0x1830b93d(unsigned int32 Arg_0x0)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
