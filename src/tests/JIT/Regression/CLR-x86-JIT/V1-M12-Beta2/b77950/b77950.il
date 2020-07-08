// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b77950 { }
.class ILGEN_0x1bd95bae {

//BEGIN STATIC FIELDS
.field static	unsigned int32[] field_0x0
.field static	unsigned int64 field_0x1
.field static	unsigned int32 field_0x2
.field static	native int field_0x3
.field static	int8 field_0x4
.field static	int16 field_0x5
.field static	int64[] field_0x6
.field static native int[] field_0x7
//END STATIC FIELDS
.method static int16  Method_0x40637edd(int32 Arg_0x0, int8 Arg_0x1, int16 Arg_0x2, int16 Arg_0x3, float64 Arg_0x4, float32 Arg_0x5, unsigned int8 Arg_0x6, float32 Arg_0x7) {
.maxstack	15

 .locals init(unsigned int8[] LOCAL_0x0,object LOCAL_0x1,float32 LOCAL_0x2,unsigned int8 LOCAL_0x3,unsigned int64 LOCAL_0x4,int32 LOCAL_0x5)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x0
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x1
ldc.r4 float32(0xd12b38f0) 
stloc LOCAL_0x2
ldc.i4 0x6da44a53 
stloc LOCAL_0x3
ldc.i8 0xeea7429ddb1483fe 
stloc LOCAL_0x4
ldc.i4 0x50973174 
stloc LOCAL_0x5

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0x1bd95bae::field_0x0
ldc.i8 0x69d8d8756cb9af5b 
stsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
ldc.i4 0x49ae33ff 
stsfld unsigned int32 ILGEN_0x1bd95bae::field_0x2
ldc.i4 0x47edfe9b 
stsfld native int ILGEN_0x1bd95bae::field_0x3
ldc.i4 0x356a0b67 
stsfld int8 ILGEN_0x1bd95bae::field_0x4
ldc.i4 0x6516de10 
stsfld int16 ILGEN_0x1bd95bae::field_0x5
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x1bd95bae::field_0x6
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x1bd95bae::field_0x7

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
    nop
End_Orphan_2:
     ldsfld int64[] ILGEN_0x1bd95bae::field_0x6
     ldc.i4.4
    ldelem.i8
     ldsfld native int ILGEN_0x1bd95bae::field_0x3
     ldsfld native int ILGEN_0x1bd95bae::field_0x3
 ble Branch_0x4

     ldc.i8 0xcc0bd1194496e801
 br Branch_0x5

 Branch_0x4:

     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
 Branch_0x5:

   and
Start_Orphan_3:
    ldnull
   stloc LOCAL_0x1
End_Orphan_3:
Start_Orphan_4:
    nop
End_Orphan_4:
     ldsfld native int ILGEN_0x1bd95bae::field_0x3
 brfalse Branch_0x8

     ldsfld native int ILGEN_0x1bd95bae::field_0x3
 br Branch_0x9

 Branch_0x8:

     ldsfld native int ILGEN_0x1bd95bae::field_0x3
 Branch_0x9:

     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
    conv.ovf.i
 beq Branch_0x6

     ldc.i8 0xa8d8c1c3dc7c7e09
     ldloc LOCAL_0x4
 ble.un Branch_0xa

     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
 br Branch_0xb

 Branch_0xa:

     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
 Branch_0xb:

 br Branch_0x7

 Branch_0x6:

     ldsfld native int ILGEN_0x1bd95bae::field_0x3
     ldloc LOCAL_0x3
 bge Branch_0xc

     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
 br Branch_0xd

 Branch_0xc:

     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
 Branch_0xd:

 Branch_0x7:

 beq Branch_0x2

Start_Orphan_5:
    nop
End_Orphan_5:
     ldarg Arg_0x4
     ldarg Arg_0x5
    clt.un
     ldsfld native int ILGEN_0x1bd95bae::field_0x3
     ldarg Arg_0x1
 ble.un Branch_0x10

     ldsfld int16 ILGEN_0x1bd95bae::field_0x5
 br Branch_0x11

 Branch_0x10:

     ldc.i4.7
 Branch_0x11:

 bge.un Branch_0xe

     ldarga Arg_0x6
     ldc.i4.3
    stind.i1
 br Branch_0xf

 Branch_0xe:

     ldsfld native int[] ILGEN_0x1bd95bae::field_0x7
    stsfld native int[] ILGEN_0x1bd95bae::field_0x7
 Branch_0xf:

 br Branch_0x3

 Branch_0x2:

Start_Orphan_6:
    nop
End_Orphan_6:
     ldc.i4.2
    conv.i8
   pop
 Branch_0x3:

End_Orphan_1:
Start_Orphan_7:
     ldloc LOCAL_0x0
    pop
End_Orphan_7:
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     add.ovf.un
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     xor
    add.ovf.un
Start_Orphan_8:
     ldloc LOCAL_0x3
    stsfld int8 ILGEN_0x1bd95bae::field_0x4
End_Orphan_8:
Start_Orphan_9:
     nop
End_Orphan_9:
      ldloc LOCAL_0x4
     conv.ovf.i1.un
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
      ldc.i4.7
     add
    mul
   div.un
Start_Orphan_a:
Start_Orphan_b:
    nop
End_Orphan_b:
     ldloc LOCAL_0x4
    conv.ovf.u8
   stloc LOCAL_0x4
End_Orphan_a:
Start_Orphan_c:
     ldarga Arg_0x2
    pop
End_Orphan_c:
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     cgt
      ldc.i4.5
      ldc.i4.4
     rem.un
    add.ovf.un
Start_Orphan_d:
     ldloc LOCAL_0x0
    stloc LOCAL_0x0
End_Orphan_d:
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     rem.un
    conv.ovf.i1.un
   rem
  add.ovf.un
Start_Orphan_e:
   ldloca LOCAL_0x4
Start_Orphan_f:
    ldloca LOCAL_0x3
    ldarg Arg_0x1
   stind.i1
End_Orphan_f:
     ldsfld native int ILGEN_0x1bd95bae::field_0x3
    conv.ovf.i2.un
   conv.ovf.u8.un
  stind.i8
End_Orphan_e:
Start_Orphan_11:
     ldc.i8 0xf93e3c94c7cf98b0
    not
   stsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
End_Orphan_11:
    ldsfld int8 ILGEN_0x1bd95bae::field_0x4
   conv.ovf.u
Start_Orphan_13:
     ldsfld int64[] ILGEN_0x1bd95bae::field_0x6
     ldc.i4 0xe37dd11c
     ldsfld unsigned int64 ILGEN_0x1bd95bae::field_0x1
    stelem.i8
End_Orphan_13:
    ldsfld native int[] ILGEN_0x1bd95bae::field_0x7
      ldsfld int64[] ILGEN_0x1bd95bae::field_0x6
      ldc.i4.0
     ldelem.i8
    conv.i4
   ldelem.i
  sub.ovf
 sub
   ldloc LOCAL_0x0
    ldc.i8 0xd59c09a7456a4372
   conv.i2
  ldelem.u1
Start_Orphan_16:
     ldc.i4.2
    conv.i2
     ldloc LOCAL_0x4
    conv.i2
   sub
  starg Arg_0x0
End_Orphan_16:
      ldc.i4.6
     conv.ovf.u2
    conv.ovf.u8.un
      ldc.i4.1
     conv.r8
    conv.u8
   ceq
      ldc.i4.7
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     add.ovf.un
    conv.ovf.u4
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     conv.r4
    conv.ovf.i4
   shl
  shr.un
 add.ovf.un
 bgt Branch_0x0

  ldloca LOCAL_0x4
  ldc.i8 0x24210b8aece91cee
 stind.i8
 br Branch_0x1

 Branch_0x0:
      ldarg Arg_0x6
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     mul.ovf.un
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
      ldsfld native int ILGEN_0x1bd95bae::field_0x3
     mul
    xor
   conv.i2
  conv.u1
 newarr [mscorlib]System.Int64
 pop
 Branch_0x1:

End_Orphan_0:
     ldloca LOCAL_0x2
    ldind.r4
   conv.u8
  conv.r.un
 conv.i1
conv.ovf.u2.un
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	17
.try {
ldc.i4 0x94b175a3 
ldc.i4 0xc8d208e4 
ldc.i4 0x990c6d19 
ldc.i4 0x42423854 
ldc.r8 float64(0x8a8bae62f1791fc9) 
ldc.r4 float32(0xb1edd60) 
ldc.i4 0xb395dac0 
ldc.r4 float32(0x556f0087) 

 call int16  ILGEN_0x1bd95bae::Method_0x40637edd(int32 Arg_0x0, int8 Arg_0x1, int16 Arg_0x2, int16 Arg_0x3, float64 Arg_0x4, float32 Arg_0x5, unsigned int8 Arg_0x6, float32 Arg_0x7)
conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.OverflowException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}

}
