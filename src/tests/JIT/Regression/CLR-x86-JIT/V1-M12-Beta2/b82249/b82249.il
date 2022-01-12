// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xf9a34ac1 {}
.assembly extern xunit.core {}
.class ILGEN_0xf9a34ac1 {

//BEGIN STATIC FIELDS
.field static	float32 field_0x0
.field static	native int field_0x1
.field static	int8 field_0x2
//END STATIC FIELDS
.method static unsigned int64  Method_0x2d60f019(int8 Arg_0x0, unsigned int32 Arg_0x1) {
.maxstack	9

 .locals (unsigned int32 LOCAL_0x0,unsigned int32[] LOCAL_0x1,int64[] LOCAL_0x2)
//BEGIN INIT LOCALS
ldc.i4 0xcd92e587 
stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x2

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.r4 float32(0x46a58264) 
stsfld float32 ILGEN_0xf9a34ac1::field_0x0
ldc.i4 0xa314fc6c 
stsfld native int ILGEN_0xf9a34ac1::field_0x1
ldc.i4 0x7b68c05 
stsfld int8 ILGEN_0xf9a34ac1::field_0x2

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
   ldc.i8 0xd14982f35a59c01
   ldc.i8 0x2caee22358bae36
  mul.ovf
   ldsfld float32 ILGEN_0xf9a34ac1::field_0x0
   ldsfld float32 ILGEN_0xf9a34ac1::field_0x0
 bne.un Branch_0x4

   ldc.i8 0xb4ec919c9e5c67a5
 br Branch_0x5

 Branch_0x4:

   ldc.i8 0x5d3a1f4544a51b60
 Branch_0x5:

 bgt.un Branch_0x2

   ldc.i8 0x7d127eb6d2a15f5c
  conv.ovf.i2
 br Branch_0x3

 Branch_0x2:

   ldc.i4.7
   ldc.i4.6
  add.ovf
 Branch_0x3:

   ldsfld native int ILGEN_0xf9a34ac1::field_0x1
  conv.i2
   ldloc LOCAL_0x0
  conv.u4
 mul
 bge.un Branch_0x0

   ldc.i8 0xa8f9a21755ba1fb9
  conv.ovf.u4.un
   ldc.r4 float32(0x88ea29d3)
   ldc.r8 float64(0xa47bce9daad0523f)
  clt.un
 bgt Branch_0x6

   ldsfld native int ILGEN_0xf9a34ac1::field_0x1
   ldc.i4.6
 bgt.un Branch_0x8

   nop
 br Branch_0x9

 Branch_0x8:

   nop
 Branch_0x9:

 br Branch_0x7

 Branch_0x6:

   ldc.i8 0xac4a328c6d7b9363
   ldc.i8 0x1b4c6a915771c577
 bne.un Branch_0xa

   nop
 br Branch_0xb

 Branch_0xa:

   nop
 Branch_0xb:

 Branch_0x7:

 br Branch_0x1

 Branch_0x0:

   ldsfld native int ILGEN_0xf9a34ac1::field_0x1
   ldc.i4.7
 bge Branch_0xe

   ldsfld float32 ILGEN_0xf9a34ac1::field_0x0
 br Branch_0xf

 Branch_0xe:

   ldc.r8 float64(0xb92c5438fc66244b)
 Branch_0xf:

   ldc.r8 float64(0x8953504f8156b176)
  call float64 [mscorlib]System.Math::Round(float64)
 bgt Branch_0xc

   ldsflda native int ILGEN_0xf9a34ac1::field_0x1
  pop
 br Branch_0xd

 Branch_0xc:

   ldc.i4.1
  stsfld int8 ILGEN_0xf9a34ac1::field_0x2
 Branch_0xd:

 Branch_0x1:

End_Orphan_0:
    ldc.i8 0xe6b477a22f32391f
    ldc.i8 0x4b6fdbaf747e4475
   div
  conv.ovf.u8.un
Start_Orphan_c:
 ldc.i4 3 
 Loop_0x0: //WHAT
   ldc.i4.3
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_c:
    ldc.i8 0x9cf3e0fa5c40287
   conv.ovf.i2.un
  conv.u2
 conv.ovf.u8
add
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	5
.try {
ldc.i4 0x54360131 
ldc.i4 0xcc9b6569 

 call unsigned int64  ILGEN_0xf9a34ac1::Method_0x2d60f019(int8 Arg_0x0, unsigned int32 Arg_0x1)
conv.i4
  leave.s IL_BAD
} catch [mscorlib]System.OverflowException {
  leave.s IL_GOOD
}
IL_GOOD:
ldc.i4.s 100
 ret
IL_BAD:
ldc.i4.s 101
 ret
}

}
