// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b91223' {}
.assembly extern xunit.core {}
.class ILGEN_0x9c0b37ec {

.field static	unsigned int16[] field_0x0
.field static	int16 field_0x1
.field static	unsigned int8[] field_0x2
.field static	int64[] field_0x3
.field static	int32 field_0x4
.field static native int[] field_0x5
.field static	unsigned int32 field_0x6

.method static int16  Method_0xa1f09b04(unsigned int8 Arg_0x0, unsigned int8 Arg_0x1, int8 Arg_0x2, int32 Arg_0x3, int16 Arg_0x4, unsigned int32 Arg_0x5, unsigned int8 Arg_0x6) {
.maxstack	17

 .locals (int16 LOCAL_0x0,unsigned int16 LOCAL_0x1,unsigned int8 LOCAL_0x2,float64 LOCAL_0x3,unsigned int32[] LOCAL_0x4,int64 LOCAL_0x5,unsigned int8[] LOCAL_0x6,native int LOCAL_0x7,int64 LOCAL_0x8)

ldc.i4 0xde287562 
stloc LOCAL_0x0
ldc.i4 0xa6b7e681 
stloc LOCAL_0x1
ldc.i4 0x7d5a5b2c 
stloc LOCAL_0x2
ldc.r8 float64(0xbe5d26111675c28) 
stloc LOCAL_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x4
ldc.i8 0x709252c8cd91c995 
stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x6
ldc.i4 0xdc11972b 
stloc LOCAL_0x7
ldc.i8 0x70457bcffb2cb749 
stloc LOCAL_0x8


ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x9c0b37ec::field_0x0
ldc.i4 0x725e6bd0 
stsfld int16 ILGEN_0x9c0b37ec::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x9c0b37ec::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x9c0b37ec::field_0x3
ldc.i4 0xbe00a605 
stsfld int32 ILGEN_0x9c0b37ec::field_0x4
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x9c0b37ec::field_0x5
ldc.i4 0x308db0a3 
stsfld unsigned int32 ILGEN_0x9c0b37ec::field_0x6

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
     ldloc LOCAL_0x7
     ldloc LOCAL_0x7
 ble.un Branch_0x6

     ldloc LOCAL_0x5
 br Branch_0x7

 Branch_0x6:

     ldc.i8 0x8a41a25cbcdc6aa8
 Branch_0x7:

     ldloc LOCAL_0x7
    conv.ovf.i8.un
 beq Branch_0x4

     ldarg Arg_0x4
    stloc LOCAL_0x0
 br Branch_0x5

 Branch_0x4:

     ldloca LOCAL_0x7
    pop
 Branch_0x5:

End_Orphan_2:
      ldloc LOCAL_0x7
      ldloc LOCAL_0x7
     ceq
Start_Orphan_3:
     nop
End_Orphan_3:
      ldc.i4.5
     conv.ovf.u.un
    mul.ovf.un
Start_Orphan_4:
     nop
End_Orphan_4:
      ldc.i4.3
      ldloc LOCAL_0x7
     div
Start_Orphan_5:
     nop
End_Orphan_5:
      ldloc LOCAL_0x7
     conv.ovf.i2
    add.ovf
   and
Start_Orphan_6:
Start_Orphan_7:
    nop
End_Orphan_7:
    ldarga Arg_0x3
   pop
End_Orphan_6:
Start_Orphan_8:
     ldloca LOCAL_0x3
     ldloc LOCAL_0x3
    stind.r8
End_Orphan_8:
    ldc.i4.6
Start_Orphan_9:
     ldarga Arg_0x3
     ldarg Arg_0x5
    stind.i
End_Orphan_9:
Start_Orphan_a:
     nop
End_Orphan_a:
      ldloc LOCAL_0x5
      ldloc LOCAL_0x5
     and
    conv.ovf.u1
   rem
 bne.un Branch_0x2

Start_Orphan_b:
Start_Orphan_c:
    nop
End_Orphan_c:
    ldc.i4.6
   stloc LOCAL_0x2
End_Orphan_b:
Start_Orphan_d:
     nop
End_Orphan_d:
      ldloc LOCAL_0x8
     conv.ovf.i2
    not
    ldc.i4.8
   clt
 br Branch_0x3

 Branch_0x2:

Start_Orphan_e:
    ldloca LOCAL_0x8
Start_Orphan_f:
    nop
End_Orphan_f:
    ldc.i8 0xac7eed159fe07f78
   stind.i8
End_Orphan_e:
Start_Orphan_10:
     ldloc LOCAL_0x6
    stsfld unsigned int8[] ILGEN_0x9c0b37ec::field_0x2
End_Orphan_10:
      ldc.i8 0x836e43dc18cef1f3
      ldc.i8 0xb594b019522fe895
     sub.ovf
Start_Orphan_11:
     nop
End_Orphan_11:
      ldloc LOCAL_0x8
      ldloc LOCAL_0x8
     sub.ovf
    ceq
Start_Orphan_12:
     ldloc LOCAL_0x2
    stloc LOCAL_0x0
End_Orphan_12:
Start_Orphan_13:
     nop
End_Orphan_13:
      ldc.i8 0x34c9730cd508a0f9
     conv.ovf.u
Start_Orphan_14:
     nop
End_Orphan_14:
      ldloc LOCAL_0x7
     conv.ovf.i2.un
    cgt.un
   rem
 Branch_0x3:

Start_Orphan_15:
Start_Orphan_16:
    nop
End_Orphan_16:
    ldloca LOCAL_0x5
     ldc.i8 0xabf9a66d2a1bc4be
     ldloc LOCAL_0x8
    mul.ovf.un
   stind.i8
End_Orphan_15:
Start_Orphan_17:
     ldsflda int16 ILGEN_0x9c0b37ec::field_0x1
    pop
End_Orphan_17:
Start_Orphan_18:
     nop
End_Orphan_18:
     ldloc LOCAL_0x1
      ldarga Arg_0x2
     ldind.i1
    mul
Start_Orphan_19:
     ldarga Arg_0x4
    pop
End_Orphan_19:
      ldloc LOCAL_0x7
      ldloc LOCAL_0x0
     mul.ovf
      ldsfld unsigned int32 ILGEN_0x9c0b37ec::field_0x6
      ldc.i4.6
     add.ovf
    mul.ovf
   sub
Start_Orphan_1a:
Start_Orphan_1b:
    nop
End_Orphan_1b:
    ldloca LOCAL_0x2
     ldloc LOCAL_0x7
    conv.i1
   stind.i1
End_Orphan_1a:
Start_Orphan_1c:
     ldsfld native int[] ILGEN_0x9c0b37ec::field_0x5
    stsfld native int[] ILGEN_0x9c0b37ec::field_0x5
End_Orphan_1c:
Start_Orphan_1d:
     nop
End_Orphan_1d:
      ldloc LOCAL_0x3
      ldc.r8 float64(0x5d9de9d724fcef37)
     div 
    conv.i1
   conv.ovf.i2.un
  and
 bne.un Branch_0x0

Start_Orphan_1e:
     ldarg Arg_0x4
    conv.u1
 newarr [mscorlib]System.IntPtr
     ldarg Arg_0x6
     ldloc LOCAL_0x7
    cgt.un
     ldc.i4.5
     ldloc LOCAL_0x7
    rem.un
   stelem.i
End_Orphan_1e:
   newobj     instance void [mscorlib]System.Object::.ctor()
  pop
 br Branch_0x1

 Branch_0x0:

Start_Orphan_1f:
Start_Orphan_20:
    nop
End_Orphan_20:
    ldarga Arg_0x2
   pop
End_Orphan_1f:
Start_Orphan_21:
     nop
End_Orphan_21:
      ldloc LOCAL_0x8
     conv.ovf.i1
Start_Orphan_22:
     nop
End_Orphan_22:
      ldc.i4.3
     conv.ovf.u2.un
    rem.un
   not
  starg Arg_0x3
 Branch_0x1:

End_Orphan_1:
 ldsfld unsigned int8[] ILGEN_0x9c0b37ec::field_0x2
stsfld unsigned int8[] ILGEN_0x9c0b37ec::field_0x2
End_Orphan_0:
Start_Orphan_23:
Start_Orphan_24:
    ldarga Arg_0x2
Start_Orphan_25:
     ldarga Arg_0x2
    pop
End_Orphan_25:
      ldloc LOCAL_0x3
     conv.r8
Start_Orphan_26:
     nop
End_Orphan_26:
      ldloc LOCAL_0x5
     conv.r.un
    cgt
   stind.i1
End_Orphan_24:
Start_Orphan_27:
     ldloca LOCAL_0x0
    pop
End_Orphan_27:
       ldc.i4 0x80c59d29
       ldc.i4.3
      div
Start_Orphan_28:
      nop
End_Orphan_28:
       ldloc LOCAL_0x7
       ldloc LOCAL_0x7
      add.ovf.un
     sub.ovf.un
    conv.ovf.i4
Start_Orphan_29:
Start_Orphan_2a:
     nop
End_Orphan_2a:
      ldc.i4.7
     conv.u1
 newarr [mscorlib]System.UInt16
    stsfld unsigned int16[] ILGEN_0x9c0b37ec::field_0x0
End_Orphan_29:
Start_Orphan_2b:
      ldloc LOCAL_0x1
     starg Arg_0x4
End_Orphan_2b:
Start_Orphan_2c:
      nop
End_Orphan_2c:
       ldsfld unsigned int32 ILGEN_0x9c0b37ec::field_0x6
      conv.ovf.i4.un
     conv.i
    conv.ovf.i1.un
   ceq
  conv.u1
 newarr [mscorlib]System.IntPtr
Start_Orphan_2d:
Start_Orphan_2e:
    ldloca LOCAL_0x1
     ldloc LOCAL_0x3
    conv.u2
   stind.i2
End_Orphan_2e:
      ldloca LOCAL_0x8
     ldind.i8
Start_Orphan_2f:
     nop
End_Orphan_2f:
      ldarg Arg_0x3
     conv.ovf.u8.un
    clt
   conv.u1
 newarr [mscorlib]System.Int64
      ldloc LOCAL_0x7
     neg
      ldc.r4 float32(0x3c7bb822)
     conv.ovf.u
    clt.un
Start_Orphan_30:
     ldsfld unsigned int16[] ILGEN_0x9c0b37ec::field_0x0
     ldc.i4.8
     ldsfld int32 ILGEN_0x9c0b37ec::field_0x4
    stelem.i2
End_Orphan_30:
Start_Orphan_31:
     nop
End_Orphan_31:
      ldc.r8 float64(0x99872881fbaf0ff4)
     conv.u4
    conv.u1
   shr.un
Start_Orphan_32:
     ldc.i8 0x2e9320778364e05b
    conv.r8
   pop
End_Orphan_32:
Start_Orphan_33:
     ldsfld int64[] ILGEN_0x9c0b37ec::field_0x3
    stsfld int64[] ILGEN_0x9c0b37ec::field_0x3
End_Orphan_33:
    ldc.r8 float64(0x31f5c10dc2bdd404)
   conv.ovf.u8
  stelem.i8
End_Orphan_2d:
Start_Orphan_34:
Start_Orphan_35:
     ldsfld unsigned int16[] ILGEN_0x9c0b37ec::field_0x0
    pop
End_Orphan_35:
      ldloc LOCAL_0x2
      ldloc LOCAL_0x7
     add.ovf.un
Start_Orphan_36:
     nop
End_Orphan_36:
      ldloc LOCAL_0x2
      ldc.i4.7
     div
    cgt
   stloc LOCAL_0x2
End_Orphan_34:
   ldloc LOCAL_0x4
Start_Orphan_37:
Start_Orphan_38:
     ldsfld int64[] ILGEN_0x9c0b37ec::field_0x3
    pop
End_Orphan_38:
      ldloc LOCAL_0x5
     conv.ovf.i8.un
    conv.ovf.u4.un
   starg Arg_0x4
End_Orphan_37:
Start_Orphan_39:
      ldloc LOCAL_0x1
     conv.u1
 newarr [mscorlib]System.Int64
Start_Orphan_3a:
     nop
End_Orphan_3a:
      ldc.i4.4
     conv.i2
Start_Orphan_3b:
     nop
End_Orphan_3b:
      ldloc LOCAL_0x8
      ldloc LOCAL_0x8
     rem
    stelem.i8
End_Orphan_39:
Start_Orphan_3c:
      nop
End_Orphan_3c:
       ldc.i4.6
      conv.ovf.i.un
Start_Orphan_3d:
      nop
End_Orphan_3d:
       ldloc LOCAL_0x7
      conv.ovf.i1.un
     xor
Start_Orphan_3e:
      ldloc LOCAL_0x7
     stloc LOCAL_0x7
End_Orphan_3e:
       ldloc LOCAL_0x1
      conv.ovf.u4.un
     conv.ovf.i
    or
Start_Orphan_3f:
      nop
End_Orphan_3f:
       ldc.i4.4
       ldc.i4 0x66414b1f
      shr.un
       ldloc LOCAL_0x7
       ldloc LOCAL_0x7
      rem
     add.ovf
Start_Orphan_40:
      nop
End_Orphan_40:
       ldc.r8 float64(0x4d2dbf637f195a78)
       ldc.r4 float32(0xf4ddd25d)
      add 
     conv.ovf.u1
    cgt.un
   ceq
  ldelem.u4
Start_Orphan_41:
     ldarga Arg_0x6
Start_Orphan_42:
     nop
End_Orphan_42:
      ldloc LOCAL_0x7
      ldc.i4.6
     ceq
    stind.i1
End_Orphan_41:
Start_Orphan_43:
      ldarga Arg_0x3
      ldc.i4.m1
     stind.i4
End_Orphan_43:
       ldc.i8 0xd9de1d7edacdf493
      conv.i8
     conv.ovf.i.un
    conv.ovf.i.un
Start_Orphan_44:
     newobj     instance void [mscorlib]System.Object::.ctor()
    pop
End_Orphan_44:
Start_Orphan_46:
      ldarga Arg_0x5
     pop
End_Orphan_46:
       ldloc LOCAL_0x7
       ldloc LOCAL_0x7
      add
       ldc.i4.m1
      conv.ovf.i.un
     xor
       ldsfld unsigned int32 ILGEN_0x9c0b37ec::field_0x6
       ldloc LOCAL_0x7
      clt.un
       ldloc LOCAL_0x7
       ldloc LOCAL_0x0
      add.ovf.un
     add.ovf.un
    xor
   mul.ovf.un
       ldc.r4 float32(0x46db5989)
      call float64 [mscorlib]System.Math::Abs(float64)
       ldloca LOCAL_0x3
      ldind.r8
     add 
    conv.ovf.i
       ldloc LOCAL_0x7
      conv.u1
       ldloc LOCAL_0x5
      conv.ovf.u
     add
       ldloc LOCAL_0x0
       ldloc LOCAL_0x7
      div.un
       ldloc LOCAL_0x7
       ldloc LOCAL_0x7
      sub.ovf.un
     sub.ovf
    sub.ovf.un
   sub.ovf.un
  sub.ovf.un
 stelem.i
End_Orphan_23:
        ldloc LOCAL_0x7
        ldc.i4.2
       cgt
        ldc.i4.2
        ldc.i4.5
       shl
      div
     conv.ovf.i2.un
    conv.ovf.u8
        ldloc LOCAL_0x7
        ldloc LOCAL_0x7
       rem.un
      conv.u8
     conv.ovf.i8
    conv.ovf.i8
   clt
        ldc.i4.5
       conv.r8
      conv.ovf.u8
        ldloc LOCAL_0x3
       conv.ovf.i1
      conv.u2
     shr
Start_Orphan_60:
       ldsfld int16 ILGEN_0x9c0b37ec::field_0x1
      starg Arg_0x0
End_Orphan_60:
        ldc.i4.1
       conv.ovf.i8
       ldc.i8 0xf45175b96d39b4f0
      mul.ovf
        ldarg Arg_0x2
        ldloc LOCAL_0x7
       mul
      conv.ovf.i8.un
     sub.ovf.un
    mul.ovf
   conv.ovf.i
  clt
  ldc.i4 0x25176472
 cgt.un
conv.u1
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	15
.try {
ldc.i4 0x4149699 
ldc.i4 0xfea0275c 
ldc.i4 0x4e60b7ae 
ldc.i4 0x42e8e079 
ldc.i4 0xba3460cb 
ldc.i4 0xed5a1dbd 
ldc.i4 0x5cf9093c 

 call int16  ILGEN_0x9c0b37ec::Method_0xa1f09b04(unsigned int8 Arg_0x0, unsigned int8 Arg_0x1, int8 Arg_0x2, int32 Arg_0x3, int16 Arg_0x4, unsigned int32 Arg_0x5, unsigned int8 Arg_0x6)
conv.i4
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
