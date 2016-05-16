// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x322fc2ad {}
.class ILGEN_0x322fc2ad {
.field static	unsigned int8 field_0x0
.field static	float64[] field_0x1
.field static	int16 field_0x2
.field static	unsigned int16 field_0x3
.field static	int32 field_0x4
.field static	int16 field_0x5
.field static	unsigned int16 field_0x6
.field static	float32[] field_0x7
.method static int32 Method_0xe8e111d1() {
.maxstack	19

 .locals (unsigned int8[] local_0x0,int8 local_0x1,float64[] local_0x2,unsigned int32[] local_0x3,int16 local_0x4,int32 local_0x5,float64 local_0x6,unsigned int32 local_0x7,unsigned int16 local_0x8)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 0xb1c80f8e 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x3
ldc.i4 0x83f52be8 
stloc local_0x4
ldc.i4 0x5d2c3d87 
stloc local_0x5
ldc.r8 float64(0xa88a7b731a48485) 
stloc local_0x6
ldc.i4 0x66402520 
stloc local_0x7
ldc.i4 0xac52c703 
stloc local_0x8

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0xb2e6fbd9 
stsfld unsigned int8 ILGEN_0x322fc2ad::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x322fc2ad::field_0x1
ldc.i4 0xc4811d52 
stsfld int16 ILGEN_0x322fc2ad::field_0x2
ldc.i4 0x34162c0e 
stsfld unsigned int16 ILGEN_0x322fc2ad::field_0x3
ldc.i4 0x24837eb5 
stsfld int32 ILGEN_0x322fc2ad::field_0x4
ldc.i4 0x1c3cc5b2 
stsfld int16 ILGEN_0x322fc2ad::field_0x5
ldc.i4 0x567882c5 
stsfld unsigned int16 ILGEN_0x322fc2ad::field_0x6
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x322fc2ad::field_0x7

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldsfld float64[] ILGEN_0x322fc2ad::field_0x1
 stloc local_0x2
End_Orphan_2:
Start_Orphan_3:
 nop
End_Orphan_3:
 ldloc local_0x0
Start_Orphan_4:
 nop
End_Orphan_4:
 ldsfld int32 ILGEN_0x322fc2ad::field_0x4
 ldsfld unsigned int16 ILGEN_0x322fc2ad::field_0x6
 div.un
 ldelema [mscorlib]System.Byte
 ldind.u1
Start_Orphan_5:
 ldloc local_0x6
 pop
End_Orphan_5:
 ldloc local_0x5
 conv.ovf.u8
Start_Orphan_6:
 nop
End_Orphan_6:
 ldloc local_0x4
 ldloc local_0x8
 beq Branch_0x4

 ldc.i8 0x6c93284c68bb9bd4
 br Branch_0x5

 Branch_0x4:

 ldc.i8 0xb5cdd4c7bc302b8
 Branch_0x5:

 bgt Branch_0x2

 ldc.r4 float32(0xdef8cd6b)
 conv.ovf.u1
 br Branch_0x3

 Branch_0x2:

 ldc.r4 float32(0xa33b325b)
 conv.u4
 Branch_0x3:

 ldc.i4.5
 bge Branch_0x0

Start_Orphan_7:
 ldc.i8 0x9576ab4086366f39
 ldc.i8 0x885f577c4eecba40
 bgt.un Branch_0x6

 nop
 br Branch_0x7

 Branch_0x6:

 nop
 Branch_0x7:

End_Orphan_7:
 ldloc local_0x8
 br Branch_0x1

 Branch_0x0:

Start_Orphan_8:
 nop
End_Orphan_8:
 ldc.i4 0x549f4ec6
 conv.ovf.u2.un
 conv.u2
 Branch_0x1:

 and
 conv.u1
 newarr [mscorlib]System.UInt32
 pop
End_Orphan_1:
 ldloca local_0x1
Start_Orphan_9:
Start_Orphan_a:
 ldsfld float32[] ILGEN_0x322fc2ad::field_0x7
 stsfld float32[] ILGEN_0x322fc2ad::field_0x7
End_Orphan_a:
 ldc.i4.2
Start_Orphan_b:
Start_Orphan_c:
 nop
End_Orphan_c:
 ldc.i4.7
 stloc local_0x7
End_Orphan_b:
Start_Orphan_d:
 ldloc local_0x0
 ldloc local_0x1
 ldc.i4.6
 stelem.i1
End_Orphan_d:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldloca local_0x5
 ldind.i4
 ldloc local_0x0
 ldloc local_0x4
 ldelem.u1
 sub.ovf
Start_Orphan_f:
 ldsflda unsigned int8 ILGEN_0x322fc2ad::field_0x0
 pop
End_Orphan_f:
 ldc.i4.m1
 ldc.i4.1
 div
 ldc.i4.s -25
 conv.ovf.u
 and
 rem.un
Start_Orphan_10:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldloc local_0x0
 ldsfld int32 ILGEN_0x322fc2ad::field_0x4
 ldelema [mscorlib]System.Byte
Start_Orphan_12:
 nop
End_Orphan_12:
 ldloc local_0x8
 ldc.i4.s 28
 cgt
 stind.i1
End_Orphan_10:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.i4.4
 conv.ovf.u
 ldc.i4.7
 ldc.i4 0x18506ad3
 add.ovf.un
 div.un
 conv.ovf.i1.un
 div.un
 cgt.un
Start_Orphan_14:
 ldloca local_0x5
 pop
End_Orphan_14:
Start_Orphan_15:
 ldsflda unsigned int16 ILGEN_0x322fc2ad::field_0x6
 pop
End_Orphan_15:
Start_Orphan_16:
 nop
End_Orphan_16:
 ldloc local_0x5
 conv.ovf.i4.un
 ldsfld int16 ILGEN_0x322fc2ad::field_0x2
 conv.ovf.u
 add
 ldc.i8 0xc2406df137d3562b
 ldc.i8 0x1d1aa0709010f74d
 rem.un
 conv.ovf.i
 add
 conv.ovf.i1.un
Start_Orphan_17:
Start_Orphan_18:
 ldloc local_0x1
 stloc local_0x1
End_Orphan_18:
 ldc.i4.5
 conv.u1
 newarr [mscorlib]System.Double
 pop
End_Orphan_17:
Start_Orphan_19:
 ldsfld float64[] ILGEN_0x322fc2ad::field_0x1
 ldc.i4.6
 ldloc local_0x6
 stelem.r8
End_Orphan_19:
 ldc.i4.0
 ldc.i4.6
 div
 ldc.i4.7
 ldc.i4.3
 add
 clt.un
 not
Start_Orphan_1a:
 ldloc local_0x8
 stloc local_0x8
End_Orphan_1a:
 ldc.i4.m1
 conv.r4
 conv.ovf.u
 conv.ovf.u2
 add
 add.ovf.un
 bge.un Branch_0x8

 ldsflda int16 ILGEN_0x322fc2ad::field_0x2
 pop
 br Branch_0x9

 Branch_0x8:

 Branch_0x9:

End_Orphan_9:
pop
End_Orphan_0:

ldc.i4 55
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
call int32 ILGEN_0x322fc2ad::Method_0xe8e111d1()
pop

  leave a1
} catch [mscorlib]System.IndexOutOfRangeException {
  pop
  leave a1
}
a1:
ldc.i4 100
 ret
}

}
