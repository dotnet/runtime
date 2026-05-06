// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly 'volatileLocal1' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0xd01d9fe9 {

//BEGIN STATIC FIELDS
.field static	unsigned int32[] field_0x0
.field static	unsigned int16 field_0x1
.field static	int64 field_0x2
.field static	unsigned int16 field_0x3
.field static	unsigned int64 field_0x4
//END STATIC FIELDS
.method static native int  Method_0x594ff2c5(int16 Arg_0x0, unsigned int8 Arg_0x1, unsigned int16 Arg_0x2, int64 Arg_0x3, native int Arg_0x4, int8 Arg_0x5, unsigned int8 Arg_0x6, unsigned int16 Arg_0x7, unsigned int32 Arg_0x8, int32 Arg_0x9, int32 Arg_0xa, unsigned int8 Arg_0xb, native int Arg_0xc, int8 Arg_0xd) {
.zeroinit 
.maxstack	11

 .locals (int8[] LOCAL_0x0,float32[] LOCAL_0x1)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x1

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xd01d9fe9::field_0x0
ldc.i4 0xa8b0912b 
stsfld unsigned int16 ILGEN_0xd01d9fe9::field_0x1
ldc.i8 0x3d91e557c470bfd3 
stsfld int64 ILGEN_0xd01d9fe9::field_0x2
ldc.i4 0x683f2179 
stsfld unsigned int16 ILGEN_0xd01d9fe9::field_0x3
ldc.i8 0x701b8e7d4cd6ec8a 
stsfld unsigned int64 ILGEN_0xd01d9fe9::field_0x4

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
Start_Orphan_a_2:
  nop
End_Orphan_a_2:
   ldarg Arg_0xc
Start_Orphan_b_3:
  nop
End_Orphan_b_3:
  conv.i
Start_Orphan_a_4:
  nop
End_Orphan_a_4:
   ldarg Arg_0x4
   ldc.i4 0x7983cf0a
 bne.un Branch_0x2

   ldc.i4.8
 br Branch_0x3

 Branch_0x2:

   ldc.i4 0xef41d40a
 Branch_0x3:

 bgt.un Branch_0x0

   ldsflda int64 ILGEN_0xd01d9fe9::field_0x2
  initobj [mscorlib]System.Int64
 br Branch_0x1

 Branch_0x0:

    ldloc LOCAL_0x1
    ldc.i4.1
   ldelema [mscorlib]System.Single
  initobj [mscorlib]System.Single
 Branch_0x1:

End_Orphan_a_1:
Start_Orphan_b_5:
   sizeof [mscorlib]System.Single
Start_Orphan_b_6:
  nop
End_Orphan_b_6:
  conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_a_7:
  nop
End_Orphan_a_7:
   //ldc.i4.5
   //ldarg Arg_0x6
Start_Orphan_b_8:
  nop
End_Orphan_b_8:
  //sub
ldc.i4 1
   ldarga Arg_0xb
  unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.Byte
 stelem.i1
End_Orphan_b_5:
 ldarga Arg_0x6
Start_Orphan_a_9:
   ldarga Arg_0x2
   ldarga Arg_0x7
  cpobj [mscorlib]System.UInt16
End_Orphan_a_9:
Start_Orphan_b_a:
   nop
End_Orphan_b_a:
   ldarga Arg_0x4
  unaligned. 0x1 
 volatile. 
 ldobj [mscorlib]System.IntPtr
Start_Orphan_b_b:
Start_Orphan_a_c:
  nop
End_Orphan_a_c:
   ldsfld int64 ILGEN_0xd01d9fe9::field_0x2
  conv.i2
 starg Arg_0x7
End_Orphan_b_b:
 conv.u4
unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Byte
End_Orphan_a_0:
Start_Orphan_a_d:
  ldarga Arg_0xd
Start_Orphan_a_e:
   ldarga Arg_0xd
  initobj [mscorlib]System.SByte
End_Orphan_a_e:
Start_Orphan_b_f:
   ldsflda unsigned int64 ILGEN_0xd01d9fe9::field_0x4
   ldc.i8 0xda974567ff6ab1d4
  unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.UInt64
End_Orphan_b_f:
  sizeof [mscorlib]System.SByte
 stobj [mscorlib]System.SByte
End_Orphan_a_d:
Start_Orphan_b_10:
  ldarga Arg_0x0
Start_Orphan_a_11:
   ldarga Arg_0xc
  pop
End_Orphan_a_11:
    ldarg Arg_0x4
    ldarg Arg_0x7
   add
  conv.i2
 unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Int16
End_Orphan_b_10:
 ldarg Arg_0x0
Start_Orphan_a_12:
Start_Orphan_a_13:
   ldsfld int64 ILGEN_0xd01d9fe9::field_0x2
  stsfld int64 ILGEN_0xd01d9fe9::field_0x2
End_Orphan_a_13:
  ldarga Arg_0x4
 initobj [mscorlib]System.IntPtr
End_Orphan_a_12:
Start_Orphan_a_14:
   ldarga Arg_0x0
Start_Orphan_a_15:
   nop
End_Orphan_a_15:
    ldc.i4.2
    ldarg Arg_0x2
   cgt
  stind.i2
End_Orphan_a_14:
  sizeof [mscorlib]System.UInt16
Start_Orphan_a_16:
    nop
End_Orphan_a_16:
     ldarg Arg_0xc
     ldarg Arg_0x4
    sub
     ldc.r8 float64(0xcfc4345d958cc93a)
    conv.u4
Start_Orphan_b_17:
    ldarga Arg_0x9
   initobj [mscorlib]System.Int32
End_Orphan_b_17:
   //rem.un
add
Start_Orphan_b_18:
   ldarga Arg_0xc
Start_Orphan_a_19:
   nop
End_Orphan_a_19:
    ldarg Arg_0xc
    ldc.i4.0
   add
  unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.IntPtr
End_Orphan_b_18:
  conv.i
Start_Orphan_b_1a:
Start_Orphan_a_1b:
  nop
End_Orphan_a_1b:
Start_Orphan_b_1c:
   ldarga Arg_0x6
   sizeof [mscorlib]System.Byte
  stind.i1
End_Orphan_b_1c:
  ldarga Arg_0x6
Start_Orphan_a_1d:
   nop
End_Orphan_a_1d:
    ldc.r4 float32(0xb440af6a)
Start_Orphan_b_1e:
   nop
End_Orphan_b_1e:
   conv.u4
Start_Orphan_a_1f:
   nop
End_Orphan_a_1f:
    ldc.i4.m1
Start_Orphan_b_20:
   nop
End_Orphan_b_20:
   conv.u2
  sub
 unaligned. 0x1 
 volatile. 
 stobj [mscorlib]System.Byte
End_Orphan_b_1a:
 sub
Start_Orphan_b_21:
Start_Orphan_a_22:
   ldsfld unsigned int32[] ILGEN_0xd01d9fe9::field_0x0
   sizeof [mscorlib]System.SByte
   sizeof [mscorlib]System.Double
  stelem.i4
End_Orphan_a_22:
    ldarg Arg_0xc
    ldarg Arg_0x4
Start_Orphan_b_23:
   nop
End_Orphan_b_23:
   cgt.un
Start_Orphan_b_24:
   ldarga Arg_0xc
  initobj [mscorlib]System.IntPtr
End_Orphan_b_24:
  conv.u1
 newarr [mscorlib]System.SByte
Start_Orphan_a_25:
   ldarga Arg_0xb
  initobj [mscorlib]System.Byte
End_Orphan_a_25:
    ldarg Arg_0xc
    ldarg Arg_0x4
   clt.un
Start_Orphan_b_26:
   ldarga Arg_0x4
   ldarg Arg_0xc
  stobj [mscorlib]System.IntPtr
End_Orphan_b_26:
  conv.i1
 ldelema [mscorlib]System.SByte
Start_Orphan_a_27:
Start_Orphan_a_28:
  nop
End_Orphan_a_28:
Start_Orphan_b_29:
  nop
End_Orphan_b_29:
  ldarga Arg_0x0
 initobj [mscorlib]System.Int16
End_Orphan_a_27:
 ldarga Arg_0xd
cpobj [mscorlib]System.SByte
End_Orphan_b_21:
add
ldc.i4 929421090
add
neg
 ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	29
ldc.i4 0x72960b5e 
ldc.i4 0xdb6e0438 
ldc.i4 0x9747099f 
ldc.i8 0xef20e3c8f0bd7ec2 
ldc.i4 0x70c9a1d4 conv.i
ldc.i4 0xdd218625 
ldc.i4 0xfbd41362 
ldc.i4 0xe3c64d9d 
ldc.i4 0x9fddac71 
ldc.i4 0x24200e28 
ldc.i4 0xee5b343e 
ldc.i4 0x85644a90 
ldc.i4 0x3765795c conv.i
ldc.i4 0x2d05f578 

 call native int  ILGEN_0xd01d9fe9::Method_0x594ff2c5(int16 Arg_0x0, unsigned int8 Arg_0x1, unsigned int16 Arg_0x2, int64 Arg_0x3, native int Arg_0x4, int8 Arg_0x5, unsigned int8 Arg_0x6, unsigned int16 Arg_0x7, unsigned int32 Arg_0x8, int32 Arg_0x9, int32 Arg_0xa, unsigned int8 Arg_0xb, native int Arg_0xc, int8 Arg_0xd)
conv.i4
 ret
}

}


