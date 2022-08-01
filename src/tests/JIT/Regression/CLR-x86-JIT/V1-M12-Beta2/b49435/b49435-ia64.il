// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b49435-ia64' {}
.assembly extern xunit.core {}
.class ILGEN_0xa691ee4d {
.field static	int32[] field_0x0
.field static	float64[] field_0x1
.field static	int16[] field_0x2
.field static	int64[] field_0x3
.method static int32 Method_0xf329b3e5(int64 Arg_0x0) {
.maxstack	17
 .locals (unsigned int8[] local_0x0,float32[] local_0x1,int16[] local_0x2)
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc local_0x0
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x2

ldc.i4 255
 newarr [mscorlib]System.Int32 
 stsfld int32[] ILGEN_0xa691ee4d::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0xa691ee4d::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0xa691ee4d::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0xa691ee4d::field_0x3

Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 ldc.r4 float32(0x6b3df552)
 ldc.r8 float64(0x6b241a0c67722783)
 beq Branch_0x4

 ldc.i4.s -32
 br Branch_0x5

 Branch_0x4:

 ldc.i4 0x98b9f75f
 Branch_0x5:

 ldc.i4.0
 ldc.i4.7
 bge.un Branch_0x6

 ldc.i4.0
 br Branch_0x7

 Branch_0x6:

 ldc.i4 0x4b24ec07
 Branch_0x7:

 bgt Branch_0x2

 ldc.i4.m1
 ldc.i4.2
 beq Branch_0x8

 nop
 br Branch_0x9

 Branch_0x8:

 nop
 Branch_0x9:

 br Branch_0x3

 Branch_0x2:

 ldloc local_0x2
 pop
 Branch_0x3:

End_Orphan_2:
 ldc.i4.3
 conv.ovf.u1.un
 not
 ldarg Arg_0x0
 conv.ovf.i4
Start_Orphan_4:
 nop
End_Orphan_4:
 ldc.i8 0xdf23e3606fe311df
 conv.u2
 ceq
 and
 conv.ovf.u8
 pop
End_Orphan_1:
Start_Orphan_5:
Start_Orphan_6:
 nop
End_Orphan_6:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Single
 ldc.r8 float64(0x2850ca681758167)
 conv.ovf.u2
 ldelema [mscorlib]System.Single
Start_Orphan_7:
 ldc.r8 float64(0xedd3006abaaec579)
 pop
End_Orphan_7:
Start_Orphan_8:
 nop
End_Orphan_8:
 ldc.r4 float32(0x30de34a3)
 ldc.r8 float64(0x37d72dc7b64aa744)
 rem 
 ckfinite
 stind.r4
End_Orphan_5:
Start_Orphan_9:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.2
 ldelema [mscorlib]System.Int32
 ldc.i4.m1
 stind.i
End_Orphan_9:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
Start_Orphan_a:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.5
 ldc.i4.4
 stelem.i4
End_Orphan_a:
Start_Orphan_b:
 nop
End_Orphan_b:
 ldc.i4.1
 conv.ovf.i1
 not
 ldelem.i8
Start_Orphan_c:
 ldloc local_0x0
Start_Orphan_d:
 nop
End_Orphan_d:
 ldc.i4.5
 ldc.i4.6
 shl
 ldc.i4.m1
 ldc.i4 0x738de16d
 clt
 stelem.i1
End_Orphan_c:
Start_Orphan_e:
 nop
End_Orphan_e:
 ldc.i4.s -65
 ldc.i4.2
 add.ovf
 conv.ovf.u8.un
 conv.i8
 sub.ovf
Start_Orphan_f:
 ldloc local_0x1
Start_Orphan_10:
 nop
End_Orphan_10:
 ldc.i4.s 124
 ldc.i4.4
 cgt
 ldelema [mscorlib]System.Single
Start_Orphan_11:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 pop
End_Orphan_11:
Start_Orphan_12:
 nop
End_Orphan_12:
 ldc.i4.6
 ldc.i4.5
 clt
 conv.r8
 stind.r4
End_Orphan_f:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldc.i4.3
 ldc.i4.2
 mul.ovf
 conv.ovf.i8
Start_Orphan_14:
 ldsfld int16[] ILGEN_0xa691ee4d::field_0x2
 stloc local_0x2
End_Orphan_14:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldc.i8 0xe199aa4b86aa9b36
 conv.ovf.i8
Start_Orphan_16:
 nop
End_Orphan_16:
 ldc.i4.2
 ldc.i4.1
 cgt
 shr.un
 add.ovf.un
 conv.ovf.i8.un
 mul
 conv.ovf.i2.un
Start_Orphan_17:
Start_Orphan_18:
Start_Orphan_19:
 nop
End_Orphan_19:
 ldloc local_0x0
 pop
End_Orphan_18:
Start_Orphan_1a:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.4
 ldelema [mscorlib]System.Int32
 pop
End_Orphan_1a:
Start_Orphan_1b:
 nop
End_Orphan_1b:
 ldc.i8 0x95a57a0436763819
 ldarg Arg_0x0
 sub.ovf.un
 conv.ovf.u1
 conv.u1
 newarr [mscorlib]System.Int16
Start_Orphan_1c:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.3
 ldelema [mscorlib]System.Int32
 ldc.i4.s -59
 stind.i4
End_Orphan_1c:
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldc.r8 float64(0x8929d7507261df06)
 ldc.r8 float64(0x56d48d30f205372b)
 sub  
 conv.ovf.i4
 conv.u2
 ldelema [mscorlib]System.Int16
 pop
End_Orphan_17:
Start_Orphan_1e:
Start_Orphan_1f:
 ldloc local_0x1
 pop
End_Orphan_1f:
Start_Orphan_20:
 nop
End_Orphan_20:
 ldc.i8 0xe5b8884971339db5
 conv.ovf.u2
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.i4.0
 ldc.i4.3
 sub
 conv.ovf.i1
 ldelema [mscorlib]System.Int32
Start_Orphan_21:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.s -126
 ldelema [mscorlib]System.Int32
Start_Orphan_22:
 nop
End_Orphan_22:
 ldc.i4.0
 conv.ovf.i2
 stind.i
End_Orphan_21:
 ldc.i4.m1
 ldc.i4.5
 mul.ovf.un
 ldc.i4.7
 conv.ovf.i4.un
 ceq
 ldc.i4.8
 shr.un
 stind.i4
End_Orphan_1e:
Start_Orphan_23:
Start_Orphan_24:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.8
 ldelema [mscorlib]System.Int32
 ldc.i4.0
 stind.i4
End_Orphan_24:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 ldc.r4 float32(0x6ca53743)
 conv.u1
 ldelema [mscorlib]System.Int64
 pop
End_Orphan_23:
Start_Orphan_25:
 ldsfld float64[] ILGEN_0xa691ee4d::field_0x1
 ldc.i4.3
 ldelema [mscorlib]System.Double
 ldc.r8 float64(0x1ada910fe461ae10)
 stind.r8
End_Orphan_25:
 ldc.i4.4
 conv.i1
 conv.ovf.i1.un
Start_Orphan_26:
 ldc.i4.2
 conv.u1
 newarr [mscorlib]System.Double
 ldarg Arg_0x0
 conv.ovf.u1
Start_Orphan_27:
 nop
End_Orphan_27:
 ldc.r8 float64(0x79b94dcdbd8020ff)
 call float64 [mscorlib]System.Math::Sqrt(float64)
 stelem.r8
End_Orphan_26:
Start_Orphan_28:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 ldc.i4.8
 ldarg Arg_0x0
 stelem.i8
End_Orphan_28:
 ldc.i8 0xc2d70645f22b29be
 ldc.i4.2
 shl
 ldc.i8 0x4565a2437a6ba407
 ldc.i8 0x184aabd645be31d3
 sub
 sub.ovf
Start_Orphan_29:
 nop
End_Orphan_29:
 ldarg Arg_0x0
 not
Start_Orphan_2a:
 nop
End_Orphan_2a:
 ldc.i4.2
 conv.ovf.u8.un
 sub.ovf.un
 xor
 conv.ovf.u1
 rem.un
 ldc.i4.m1
 add
 bge Branch_0x0

Start_Orphan_2b:
 ldarga Arg_0x0
 ldarg Arg_0x0
 stind.i8
End_Orphan_2b:
Start_Orphan_2c:
 nop
End_Orphan_2c:
 ldc.r8 float64(0xb7a2dccab561de36)
 conv.r8
 conv.i
 conv.ovf.u1
 conv.u1
 newarr [mscorlib]System.Int64
Start_Orphan_2d:
Start_Orphan_2e:
 ldloc local_0x1
 pop
End_Orphan_2e:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 ldc.i4.1
 conv.i
 conv.u1
Start_Orphan_2f:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 pop
End_Orphan_2f:
Start_Orphan_30:
 nop
End_Orphan_30:
 ldc.i8 0x836ed479c56f0af6
 ldc.i8 0x52d393864c083b91
 cgt.un
Start_Orphan_31:
 nop
End_Orphan_31:
 ldc.i4.7
 conv.u4
 clt.un
 stelem.i4
End_Orphan_2d:
Start_Orphan_32:
 ldc.i4.m1
 pop
End_Orphan_32:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
Start_Orphan_33:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 ldc.i4.7
 ldc.i8 0x2f7f0e586e14bffc
 stelem.i8
End_Orphan_33:
 ldc.i4.3
 ldc.i4.1
 add
 conv.i4
 ldelema [mscorlib]System.Int32
 ldind.i
 ldelem.i8
 starg Arg_0x0
 br Branch_0x1

 Branch_0x0:

Start_Orphan_34:
Start_Orphan_35:
Start_Orphan_36:
 nop
End_Orphan_36:
 ldc.i4.5
 conv.ovf.i8.un
 pop
End_Orphan_35:
 ldsfld int32[] ILGEN_0xa691ee4d::field_0x0
 stsfld int32[] ILGEN_0xa691ee4d::field_0x0
End_Orphan_34:
 ldarga Arg_0x0
 pop
 Branch_0x1:

End_Orphan_0:
Start_Orphan_37:
Start_Orphan_38:
 ldarga Arg_0x0
Start_Orphan_39:
 ldc.i4.0
 conv.u1
 newarr [mscorlib]System.Double
 pop
End_Orphan_39:
Start_Orphan_3a:
 nop
End_Orphan_3a:
 ldc.i8 0x9b73f678918d1a2c
 ldc.i4.2
 shr
Start_Orphan_3b:
 nop
End_Orphan_3b:
 ldc.i4.7
 ldc.i4.8
 mul.ovf.un
 shl
Start_Orphan_3c:
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 pop
End_Orphan_3c:
Start_Orphan_3d:
 nop
End_Orphan_3d:
 ldarga Arg_0x0
 ldind.i8
Start_Orphan_3e:
 nop
End_Orphan_3e:
 ldc.i4.s 86
 conv.i8
 mul.ovf
 rem.un
 stind.i8
End_Orphan_38:
 ldloc local_0x0
Start_Orphan_3f:
 ldc.i4.3
 ldc.i4.4
 cgt
 conv.u1
 newarr [mscorlib]System.Int32
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 ldc.i4.s -13
 ldelem.i8
 conv.ovf.u2
 ldc.i4.8
 conv.ovf.i1
 ldc.i4.6
 conv.ovf.u1.un
 ceq
 stelem.i4
End_Orphan_3f:
 ldc.i8 0xde8d33be8530396d
 conv.ovf.u8
 conv.r8
 conv.u8
 conv.u
 conv.i1
 ldc.i4.3
 ldc.i4.7
 rem
 ldc.i4.2
 conv.ovf.u1.un
 div
 ldc.i4.s -103
 conv.ovf.i2.un
 conv.ovf.u2
 ceq
Start_Orphan_46:
 ldloc local_0x0
 ldc.i4.8
 ldelema [mscorlib]System.Byte
 ldc.i4.s -59
 stind.i1
End_Orphan_46:
 ldc.i4.1
 ldc.i4.3
 cgt.un
 ldc.i4.0
 neg
 mul
 conv.u1
 xor
 ldc.i4.8
 conv.i8
 conv.ovf.i1
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.i4.m1
 ldc.i4.3
 mul.ovf.un
 ldarg Arg_0x0
 conv.i
 sub.ovf.un
 ldc.i4.2
 conv.u
 ceq
 ldelem.i4
 mul.ovf
 stelem.i1
End_Orphan_37:
 ldc.r4 float32(0xa7e41819)
 ldc.r8 float64(0xe7da4f3edb90da9d)
 cgt
 conv.ovf.u8.un
 conv.ovf.u.un
 ldc.i8 0x50dcf6aa49f59088
 conv.ovf.i
 conv.ovf.u2
Start_Orphan_4f:
 ldloc local_0x0
 ldc.i4.1
 ldelema [mscorlib]System.Byte
 pop
End_Orphan_4f:
 ldc.r4 float32(0xedd2285a)
 conv.ovf.u
 ldc.i4.5
 conv.u
 and
 sub.ovf
 or
Start_Orphan_50:
 ldc.i4.s -108
 conv.i4
 conv.u1
 newarr [mscorlib]System.Byte
 pop
End_Orphan_50:
 ldc.i4 0x5e8ee2d7
 ldc.i4.m1
 cgt.un
 conv.ovf.i8
 ldsfld int64[] ILGEN_0xa691ee4d::field_0x3
 ldc.i4.s 42
 ldelem.i8
 ldc.r4 float32(0x207d18cc)
 conv.u8
 div
 div.un
 conv.ovf.u
 div.un
 conv.u1
Start_Orphan_54:
 ldc.i4.7
 conv.u1
 newarr [mscorlib]System.Int32
 ldc.r8 float64(0x3f8e1dc63d13c81)
 ldc.r4 float32(0x6747730a)
 cgt
 ldelema [mscorlib]System.Int32
 ldc.i4.4
 ldc.i4.2
 mul.ovf.un
 ldarg Arg_0x0
 conv.ovf.u4
 cgt
 stind.i
End_Orphan_54:
 ldc.i4.4
 conv.i
 conv.ovf.u2.un
 conv.r8
 conv.i8
 conv.ovf.i2.un
 ldloc local_0x2
 ldc.r8 float64(0x5c934d67171b21b9)
 conv.ovf.u8
 ldc.i8 0x8a4d4b4b8a44aea6
 ceq
 ldc.r4 float32(0xb1f9a639)
 conv.r8
 conv.ovf.u4
 cgt
 ldelema [mscorlib]System.Int16
 ldind.i2
 mul.ovf.un
 rem.un
 conv.i2
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x9e5f5652a9ac02de 

call int32 ILGEN_0xa691ee4d::Method_0xf329b3e5(int64 Arg_0x0)
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
