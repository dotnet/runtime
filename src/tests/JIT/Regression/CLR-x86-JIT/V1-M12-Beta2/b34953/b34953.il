// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xbd98be78 {}
.assembly extern xunit.core {}
.class ILGEN_0xbd98be78 {

//BEGIN STATIC FIELDS
.field static	int8[] field_0x0
.field static	int8[] field_0x1
.field static	float64[] field_0x2
.field static	float64 field_0x3
.field static	unsigned int8 field_0x4
.field static	int8[] field_0x5
.field static	unsigned int32[] field_0x6
.field static	float64 field_0x7
.field static	int32[] field_0x8
//END STATIC FIELDS
.method static int32 Method_0x40eb(unsigned int16 Arg_0x0, unsigned int8 Arg_0x1, int64 Arg_0x2, unsigned int32 Arg_0x3, float64 Arg_0x4) {
.maxstack	17

 .locals (int8[] local_0x0,int32 local_0x1,unsigned int32[] local_0x2,unsigned int8[] local_0x3,float64[] local_0x4,unsigned int64[] local_0x5)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc local_0x0
ldc.i4 0x48e15ce6 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc local_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x5

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xbd98be78::field_0x0
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xbd98be78::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0xbd98be78::field_0x2
ldc.r8 float64(0x1ab722c96e27315a) 
stsfld float64 ILGEN_0xbd98be78::field_0x3
ldc.i4 0x18442c9f 
stsfld unsigned int8 ILGEN_0xbd98be78::field_0x4
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xbd98be78::field_0x5
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xbd98be78::field_0x6
ldc.r8 float64(0x1930213c2d1e5ec) 
stsfld float64 ILGEN_0xbd98be78::field_0x7
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0xbd98be78::field_0x8

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
Start_Orphan_3:
 nop
End_Orphan_3:
 ldsfld float64[] ILGEN_0xbd98be78::field_0x2
Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.r8 float64(0x2c7f340a5f6e3c08)
 conv.ovf.u4
 ldc.r8 float64(0x6baa39296e06835)
 ckfinite
 stelem.r8
End_Orphan_2:
Start_Orphan_5:
 ldloc local_0x5
 ldc.i4.3
 ldelema [mscorlib]System.UInt64
 ldarg Arg_0x2
 stind.i8
End_Orphan_5:
 ldarg Arg_0x4
 conv.ovf.u1
Start_Orphan_6:
 nop
End_Orphan_6:
 ldc.i4 0x576b3b4
 sub.ovf
 conv.u1
 newarr [mscorlib]System.SByte
 pop
End_Orphan_1:
Start_Orphan_7:
Start_Orphan_8:
 ldloca local_0x1
 ldc.i4.2
 stind.i4
End_Orphan_8:
 ldloc local_0x0
 stloc local_0x0
End_Orphan_7:
Start_Orphan_9:
 ldc.r4 float32(0x26423578)
 conv.i1
 stsfld unsigned int8 ILGEN_0xbd98be78::field_0x4
End_Orphan_9:
Start_Orphan_a:
 nop
End_Orphan_a:
 ldc.i4.6
 conv.ovf.i4.un
Start_Orphan_b:
 nop
End_Orphan_b:
 ldloc local_0x1
 add
 conv.u
Start_Orphan_c:
 nop
End_Orphan_c:
 ldc.i4.s 89
 ldloc local_0x2
 ldlen
 sub.ovf.un
 conv.i
 sub
 ldc.i4.6
 clt
Start_Orphan_d:
 ldsfld float64[] ILGEN_0xbd98be78::field_0x2
 stsfld float64[] ILGEN_0xbd98be78::field_0x2
End_Orphan_d:
Start_Orphan_e:
Start_Orphan_f:
 ldarg Arg_0x4
 stsfld float64 ILGEN_0xbd98be78::field_0x3
End_Orphan_f:
 ldc.i4.6
 conv.i2
 conv.r.un
 stsfld float64 ILGEN_0xbd98be78::field_0x7
End_Orphan_e:
Start_Orphan_10:
Start_Orphan_11:
 nop
End_Orphan_11:
 ldsflda float64 ILGEN_0xbd98be78::field_0x7
 ldc.r8 float64(0x194214931444c6c)
 stind.r8
End_Orphan_10:
Start_Orphan_12:
 ldarga Arg_0x4
 ldarg Arg_0x4
 stind.r8
End_Orphan_12:
 ldc.i4.4
 conv.ovf.i
Start_Orphan_13:
 nop
End_Orphan_13:
 ldarg Arg_0x0
 conv.i1
 div.un
Start_Orphan_14:
 nop
End_Orphan_14:
 ldc.i4.4
 conv.r.un
 conv.ovf.i2
 mul
 conv.r8
 conv.ovf.i2
 add.ovf.un
 starg Arg_0x0
End_Orphan_0:
Start_Orphan_15:
 ldloc local_0x5
Start_Orphan_16:
Start_Orphan_17:
 ldsfld int8[] ILGEN_0xbd98be78::field_0x1
 ldc.i4.2
 ldc.i4.3
 stelem.i1
End_Orphan_17:
 ldloca local_0x1
 ldarg Arg_0x0
 ldc.i4.5
 or
 conv.i4
 stind.i4
End_Orphan_16:
Start_Orphan_18:
 ldloca local_0x1
 ldc.i4.6
 stind.i4
End_Orphan_18:
 ldarg Arg_0x2
 conv.ovf.u
 conv.u
 ldc.i4.4
 conv.ovf.u1
 conv.ovf.i2
 add.ovf
 conv.i1
 ldelem.u8
 pop
End_Orphan_15:
Start_Orphan_19:
 ldarg Arg_0x2
 ldarg Arg_0x2
 add.ovf.un
 conv.ovf.i8.un
Start_Orphan_1a:
 ldsfld int8[] ILGEN_0xbd98be78::field_0x0
 ldarg Arg_0x1
 ldelema [mscorlib]System.SByte
 ldc.i4.1
 stind.i1
End_Orphan_1a:
 ldc.i8 0x405825af6b3a3907
 ldc.i8 0x79633eb3333d1ab6
 add.ovf
 ldarg Arg_0x2
 ldarg Arg_0x2
 add.ovf.un
 sub.ovf
 rem
Start_Orphan_1b:
 ldsflda float64 ILGEN_0xbd98be78::field_0x7
 pop
End_Orphan_1b:
Start_Orphan_1c:
 nop
End_Orphan_1c:
 ldc.r8 float64(0x3aae4fb877c846f7)
 ldsfld float64 ILGEN_0xbd98be78::field_0x3
 rem
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldarg Arg_0x4
 neg
 sub
 conv.ovf.u8
 rem
 starg Arg_0x2
End_Orphan_19:
Start_Orphan_1e:
 ldloc local_0x0
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldsfld float64 ILGEN_0xbd98be78::field_0x7
 ldsfld float64 ILGEN_0xbd98be78::field_0x3
 clt.un
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i8 0x368564d140dc5f4e
 conv.ovf.i2
 stelem.i1
End_Orphan_1e:
Start_Orphan_21:
 ldarga Arg_0x4
 ldc.r8 float64(0x72333069409a2ed2)
 stind.r8
End_Orphan_21:
 ldc.r4 float32(0x4fa23722)
 conv.i
 conv.r4
 conv.ovf.i
 conv.ovf.u8.un
 conv.r.un
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.r4 float32(0x44a07dac)
 ckfinite
 conv.u2
 conv.u8
Start_Orphan_23:
Start_Orphan_24:
 nop
End_Orphan_24:
 ldc.r8 float64(0x77e473526e0157f)
 ldsfld float64 ILGEN_0xbd98be78::field_0x7
 add
 pop
End_Orphan_23:
Start_Orphan_25:
 ldc.r4 float32(0x61f41cf4)
 pop
End_Orphan_25:
Start_Orphan_26:
 nop
End_Orphan_26:
 ldarg Arg_0x2
 conv.u2
 conv.ovf.i4
 shr.un
 conv.r8
 mul
 ckfinite
 conv.ovf.i4
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x22ba45e1 
ldc.i4 0x1fab1526 
ldc.i8 0x324815d4a1f24ae 
ldc.i4 0x2f3331f 
ldc.r8 float64(0x37802a625f1a5b9d) 

call int32 ILGEN_0xbd98be78::Method_0x40eb(unsigned int16 Arg_0x0, unsigned int8 Arg_0x1, int64 Arg_0x2, unsigned int32 Arg_0x3, float64 Arg_0x4)
pop
leave sss
} catch [mscorlib]System.OverflowException {
pop
leave sss
}
sss:
ldc.i4 100
ret
}

}
