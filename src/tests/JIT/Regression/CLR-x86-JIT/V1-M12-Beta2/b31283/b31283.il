// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b31283' {}
.assembly extern xunit.core {}
.class ILGEN_0xdba5be7f {

//BEGIN STATIC FIELDS
.field static	float64 field_0x0
.field static	int16[] field_0x1
.field static	int32 field_0x2
.field static	float32[] field_0x3
.field static	float32 field_0x4
.field static	float32 field_0x5
//END STATIC FIELDS
.method static int32 Method_0x799e() {
.maxstack	17

 .locals ()
//BEGIN INIT LOCALS

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.r8 float64(0x466567315d8b58f3) 
stsfld float64 ILGEN_0xdba5be7f::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0xdba5be7f::field_0x1
ldc.i4 0x425d2d05 
stsfld int32 ILGEN_0xdba5be7f::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0xdba5be7f::field_0x3
ldc.r4 float32(0x5b5164d8) 
stsfld float32 ILGEN_0xdba5be7f::field_0x4
ldc.r4 float32(0x6546ddc) 
stsfld float32 ILGEN_0xdba5be7f::field_0x5

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.i4.0
 ldsfld int32 ILGEN_0xdba5be7f::field_0x2
 cgt.un
 stsfld int32 ILGEN_0xdba5be7f::field_0x2
End_Orphan_1:
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 ldc.r8 float64(0x2e3b1a8f7e087fef)
 ldc.r8 float64(0xc05d7f57916e64)
 div
 rem
 conv.u8
 conv.ovf.u4
 stsfld int32 ILGEN_0xdba5be7f::field_0x2
End_Orphan_0:
Start_Orphan_3:
Start_Orphan_4:
 ldsflda int32 ILGEN_0xdba5be7f::field_0x2
 pop
End_Orphan_4:
 ldsflda float64 ILGEN_0xdba5be7f::field_0x0
Start_Orphan_5:
 ldsflda float64 ILGEN_0xdba5be7f::field_0x0
Start_Orphan_6:
 nop
End_Orphan_6:
 ldc.r8 float64(0x9fd698eeb3305a)
 ldc.r8 float64(0x55366bb42c281d9d)
 add
 stind.r8
End_Orphan_5:
Start_Orphan_7:
 ldc.i4.s 52
 pop
End_Orphan_7:
 ldc.i4.6
 conv.ovf.i.un
 ldc.i4.1
 ldc.i4.s -36
 or
 ceq
 conv.r8
 stind.r8
End_Orphan_3:
Start_Orphan_8:
Start_Orphan_9:
 nop
End_Orphan_9:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Single
 stsfld float32[] ILGEN_0xdba5be7f::field_0x3
End_Orphan_8:
Start_Orphan_a:
 ldsflda int32 ILGEN_0xdba5be7f::field_0x2
 ldc.i4.m1
 stind.i4
End_Orphan_a:
Start_Orphan_b:
 nop
End_Orphan_b:
 ldc.r8 float64(0x15196584757679ee)
 ckfinite
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 conv.r8
 div
 ckfinite
Start_Orphan_c:
 nop
End_Orphan_c:
 ldsfld float32 ILGEN_0xdba5be7f::field_0x5
 ldsfld float32 ILGEN_0xdba5be7f::field_0x4
 add
 neg
 conv.r8
 clt.un
 neg
 conv.u1
Start_Orphan_d:
Start_Orphan_e:
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 ckfinite
 stsfld float64 ILGEN_0xdba5be7f::field_0x0
End_Orphan_e:
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 ldc.r8 float64(0x561871a4380135ae)
 add
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 div
 rem
 neg
 conv.r8
 stsfld float64 ILGEN_0xdba5be7f::field_0x0
End_Orphan_d:
Start_Orphan_f:
Start_Orphan_10:
 ldsflda int32 ILGEN_0xdba5be7f::field_0x2
 pop
End_Orphan_10:
 ldsfld float64 ILGEN_0xdba5be7f::field_0x0
 ldc.r8 float64(0x262bbbb64b592b)
 div
 conv.ovf.i
Start_Orphan_11:
 nop
End_Orphan_11:
 ldc.i4.s 2
 ldc.i4.8
 add
 ldc.i4.8
 ldsfld int32 ILGEN_0xdba5be7f::field_0x2
 mul
 rem
 rem
 pop
End_Orphan_f:
Start_Orphan_12:
 ldc.i4.6
 conv.u1
 newarr [mscorlib]System.Single
 stsfld float32[] ILGEN_0xdba5be7f::field_0x3
End_Orphan_12:
Start_Orphan_13:
 nop
End_Orphan_13:
 ldsfld float32 ILGEN_0xdba5be7f::field_0x5
 ldc.r4 float32(0x689d62f9)
 sub
 conv.i
 conv.ovf.u.un
Start_Orphan_14:
 nop
End_Orphan_14:
Start_Orphan_15:
 nop
End_Orphan_15:
 ldc.r8 float64(0x3e2f35511aa91e89)
 neg
 conv.r4
 conv.i1
 clt
 conv.ovf.u1
Start_Orphan_16:
Start_Orphan_17:
Start_Orphan_18:
 nop
End_Orphan_18:
 ldc.i4.3
 conv.r4
 stsfld float32 ILGEN_0xdba5be7f::field_0x5
End_Orphan_17:
Start_Orphan_19:
 ldc.i4.3
 pop
End_Orphan_19:
Start_Orphan_1a:
 nop
End_Orphan_1a:
 ldc.i4.7
 conv.r4
 conv.ovf.u4
 conv.u1
 newarr [mscorlib]System.Single
 stsfld float32[] ILGEN_0xdba5be7f::field_0x3
End_Orphan_16:
Start_Orphan_1b:
 ldsflda int32 ILGEN_0xdba5be7f::field_0x2
Start_Orphan_1c:
 ldsfld float32 ILGEN_0xdba5be7f::field_0x4
 stsfld float32 ILGEN_0xdba5be7f::field_0x4
End_Orphan_1c:
Start_Orphan_1d:
 nop
End_Orphan_1d:
 ldc.r8 float64(0x1148161091373)
 ldc.r8 float64(0x65b4404954bb2e7)
 add
 conv.i2
 stind.i4
End_Orphan_1b:
Start_Orphan_1e:
 nop
End_Orphan_1e:
 ldsfld int32 ILGEN_0xdba5be7f::field_0x2
 ldc.i4.4
 mul
 conv.r4
 ldc.r4 float32(0x36a92bef)
Start_Orphan_1f:
 nop
End_Orphan_1f:
 ldc.r4 float32(0x23b22dd6)
 ldc.r4 float32(0x2425ba6)
 sub
 add
 div
 conv.ovf.i2
 conv.ovf.i1.un
 clt.un
 add.ovf.un
 not
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	2
.try {
 call int32 ILGEN_0xdba5be7f::Method_0x799e()
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
