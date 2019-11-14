// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1759e5bf {}
.class ILGEN_0x1759e5bf {

//BEGIN STATIC FIELDS
.field static	int8 field_0x0
.field static	int32[] field_0x1
.field static	int64 field_0x2
.field static	int16[] field_0x3
.field static	unsigned int16[] field_0x4
.field static	float64[] field_0x5
.field static	unsigned int8 field_0x6
//END STATIC FIELDS
.method static int32 Method_0x8c3e201b(int64 Arg_0x0, unsigned int8 Arg_0x1, unsigned int8 Arg_0x2, float32 Arg_0x3, unsigned int16 Arg_0x4) {
.maxstack	15

 .locals (unsigned int8[] local_0x0,unsigned int8 local_0x1,native int local_0x2,unsigned int64[] local_0x3,int16 local_0x4)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 0xb28c49eb 
stloc local_0x1
ldc.i4 0x7c121c09 
stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x3
ldc.i4 0x1f408e53 
stloc local_0x4

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 0xf44487aa 
stsfld int8 ILGEN_0x1759e5bf::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0x1759e5bf::field_0x1
ldc.i8 0x13ec132a6e9e752a 
stsfld int64 ILGEN_0x1759e5bf::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0x1759e5bf::field_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x1759e5bf::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x1759e5bf::field_0x5
ldc.i4 0x61ce5c55 
stsfld unsigned int8 ILGEN_0x1759e5bf::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
 nop
End_Orphan_1:
 ldloc local_0x2
 ldloc local_0x2
 blt.un Branch_0x4

 ldarg Arg_0x0
 br Branch_0x5

 Branch_0x4:

 ldarg Arg_0x0
 Branch_0x5:

Start_Orphan_2:
 nop
End_Orphan_2:
 ldsfld int64 ILGEN_0x1759e5bf::field_0x2
 ldc.i4.m1
 shr.un
 cgt.un
Start_Orphan_3:
 nop
End_Orphan_3:
 ldloc local_0x2
 ldsfld unsigned int8 ILGEN_0x1759e5bf::field_0x6
 beq Branch_0x8

 ldloc local_0x2
 br Branch_0x9

 Branch_0x8:

 ldloc local_0x2
 Branch_0x9:

Start_Orphan_4:
 nop
End_Orphan_4:
 ldloc local_0x2
 ldc.i4.5
 rem.un
 bne.un Branch_0x6

Start_Orphan_5:
 nop
End_Orphan_5:
 ldarg Arg_0x2
 ldloc local_0x2
 sub
 br Branch_0x7

 Branch_0x6:

 ldsfld int64 ILGEN_0x1759e5bf::field_0x2
 conv.ovf.i
 Branch_0x7:

 bgt.un Branch_0x2

Start_Orphan_6:
 nop
End_Orphan_6:
 ldsfld float64[] ILGEN_0x1759e5bf::field_0x5
 ldc.i4.3
 ldelema [mscorlib]System.Double
 pop
 br Branch_0x3

 Branch_0x2:

 ldc.i4.m1
 conv.u1
 newarr [mscorlib]System.Int32
 stsfld int32[] ILGEN_0x1759e5bf::field_0x1
 Branch_0x3:

End_Orphan_0:
 ldarg Arg_0x3
 conv.r8
 call float64 [mscorlib]System.Math::Sqrt(float64)
 call float64 [mscorlib]System.Math::Sin(float64)
Start_Orphan_7:
 ldsfld int32[] ILGEN_0x1759e5bf::field_0x1
 pop
End_Orphan_7:
 ldc.r8 float64(0xb0fa24ccd2f0a670)
 call float64 [mscorlib]System.Math::Sin(float64)
 blt Branch_0x0

Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldsfld int64 ILGEN_0x1759e5bf::field_0x2
 neg
Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.i8 0x8d0402757eaf1791
 ldc.i8 0x6032c785cb9c5386
 rem
 rem.un
 conv.u2
 stloc local_0x1
End_Orphan_8:
Start_Orphan_b:
 ldc.i4.4
 conv.u1
 newarr [mscorlib]System.Double
 stsfld float64[] ILGEN_0x1759e5bf::field_0x5
End_Orphan_b:
Start_Orphan_c:
 ldsflda int64 ILGEN_0x1759e5bf::field_0x2
 pop
End_Orphan_c:
 ldc.i4.s -61
 ldc.i4.8
 sub.ovf.un
 conv.ovf.i4.un
 ldc.i8 0xb4d1cea9a9ab8776
 conv.u4
 conv.ovf.u4.un
 ceq
 conv.i1
 conv.ovf.u4
 br Branch_0x1

 Branch_0x0:

Start_Orphan_d:
 ldc.i4.m1
 stloc local_0x4
End_Orphan_d:
 ldsfld int16[] ILGEN_0x1759e5bf::field_0x3
 ldlen
 Branch_0x1:

Start_Orphan_e:
Start_Orphan_f:
Start_Orphan_10:
 nop
End_Orphan_10:
 ldloc local_0x2
 conv.ovf.u
 stloc local_0x2
End_Orphan_f:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i4.7
 ldloc local_0x2
 clt
 conv.ovf.i2.un
Start_Orphan_12:
 nop
End_Orphan_12:
 ldc.i4.2
 conv.ovf.i2
 ldloc local_0x2
 ldloc local_0x1
 mul.ovf
 div
 clt.un
 conv.u1
 newarr [mscorlib]System.Double
Start_Orphan_13:
 ldc.i4.1
 stsfld int8 ILGEN_0x1759e5bf::field_0x0
End_Orphan_13:
 ldsfld unsigned int8 ILGEN_0x1759e5bf::field_0x6
Start_Orphan_14:
 ldloca local_0x2
 ldloc local_0x2
 stind.i
End_Orphan_14:
 ldc.r8 float64(0xab330173b0012d0d)
 ldc.r4 float32(0x3ed5bbda)
 rem 
 conv.i1
 and
 conv.ovf.u1.un
Start_Orphan_15:
 ldsfld float64[] ILGEN_0x1759e5bf::field_0x5
 ldc.i4.2
 ldelema [mscorlib]System.Double
 ldc.r4 float32(0xaed83246)
 stind.r8
End_Orphan_15:
 ldloc local_0x2
 ldloc local_0x2
 mul.ovf
Start_Orphan_16:
 nop
End_Orphan_16:
 ldsfld int8 ILGEN_0x1759e5bf::field_0x0
 ldloc local_0x2
 add.ovf
 cgt
 conv.r4
 call float64 [mscorlib]System.Math::Cos(float64)
 stelem.r8
End_Orphan_e:
Start_Orphan_17:
Start_Orphan_18:
 nop
End_Orphan_18:
 ldloc local_0x1
 conv.ovf.i1
 conv.u2
 conv.u1
 newarr [mscorlib]System.UInt16
Start_Orphan_19:
 ldc.r8 float64(0x4a01cd8d207618b3)
 conv.i8
 pop
End_Orphan_19:
Start_Orphan_1a:
 ldsflda int8 ILGEN_0x1759e5bf::field_0x0
 pop
End_Orphan_1a:
 ldloc local_0x3
 ldarg Arg_0x4
 ldelem.u8
 conv.u8
 conv.u2
Start_Orphan_1b:
Start_Orphan_1c:
 nop
End_Orphan_1c:
 ldloc local_0x0
 ldc.i8 0x611e5178771ad1a6
 ldsfld int64 ILGEN_0x1759e5bf::field_0x2
 cgt
 ldarga Arg_0x4
 ldind.u2
 stelem.i1
End_Orphan_1b:
Start_Orphan_1d:
 ldc.i4.7
 stloc local_0x4
End_Orphan_1d:
 ldsfld unsigned int16[] ILGEN_0x1759e5bf::field_0x4
Start_Orphan_1e:
 ldsfld unsigned int8 ILGEN_0x1759e5bf::field_0x6
 stsfld int8 ILGEN_0x1759e5bf::field_0x0
End_Orphan_1e:
 ldsfld unsigned int8 ILGEN_0x1759e5bf::field_0x6
 ldelem.u2
 stelem.i2
End_Orphan_17:
Start_Orphan_1f:
Start_Orphan_20:
 nop
End_Orphan_20:
 ldloc local_0x3
 ldc.i4 0x9888afd2
 ldelema [mscorlib]System.UInt64
 pop
End_Orphan_1f:
Start_Orphan_21:
 ldc.i4.0
 stsfld int8 ILGEN_0x1759e5bf::field_0x0
End_Orphan_21:
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.i4.0
 conv.ovf.u
 ldc.i4.2
 ldloc local_0x2
 rem.un
 or
 conv.ovf.u.un
Start_Orphan_23:
 nop
End_Orphan_23:
 ldloc local_0x2
 ldc.i4.8
 clt.un
 conv.ovf.u2.un
 ldloc local_0x1
 ldc.i4.6
 rem.un
Start_Orphan_24:
 nop
End_Orphan_24:
 ldloc local_0x2
 ldloc local_0x4
 clt
 div.un
 add.ovf
 add.ovf
Start_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldloc local_0x3
 ldsfld float64[] ILGEN_0x1759e5bf::field_0x5
 ldlen
 ldloc local_0x2
 conv.ovf.i8
 stelem.i8
End_Orphan_25:
Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.r4 float32(0x7fb2d484)
 conv.ovf.u2
 ldloca local_0x2
 ldind.i
 add
 conv.u
 xor
Start_Orphan_28:
 ldloca local_0x2
 pop
End_Orphan_28:
 ldloc local_0x1
 mul
 clt
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0xd3030795bf763706 
ldc.i4 0x2bf64bfc 
ldc.i4 0x8b2baad5 
ldc.r4 float32(0xc3a89ef4) 
ldc.i4 0x5bef676f 

call int32 ILGEN_0x1759e5bf::Method_0x8c3e201b(int64 Arg_0x0, unsigned int8 Arg_0x1, unsigned int8 Arg_0x2, float32 Arg_0x3, unsigned int16 Arg_0x4)
 pop
leave END
} catch [mscorlib]System.OverflowException {
pop
leave END
}
END:
ldc.i4 100
ret

}

}
