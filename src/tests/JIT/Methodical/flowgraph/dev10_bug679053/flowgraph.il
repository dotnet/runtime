// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly 'flowgraph' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0x97f1b6bf {

//BEGIN STATIC FIELDS
.field static native int[] field_0x0
.field static	float64[] field_0x1
.field static	unsigned int64 field_0x2
.field static	unsigned int32 field_0x3
.field static	native int field_0x4
.field static	int64[] field_0x5
.field static native int[] field_0x6
.field static	unsigned int64 field_0x7
.field static	float32 field_0x8
.field static	float64[] field_0x9
.field static	int64 field_0xa
.field static	unsigned int8 field_0xb
.field static	int64[] field_0xc
.field static	unsigned int16[] field_0xd
.field static	int64[] field_0xe
//END STATIC FIELDS
.method static unsigned int32  Method_0xda70a253() {
.zeroinit 
.maxstack	11

 .locals (unsigned int16[] LOCAL_0x0,int64 LOCAL_0x1,int8[] LOCAL_0x2,int64[] LOCAL_0x3,unsigned int8 LOCAL_0x4,int8[] LOCAL_0x5,native int[] LOCAL_0x6,float64[] LOCAL_0x7,int32[] LOCAL_0x8,int8 LOCAL_0x9,int16 LOCAL_0xa)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc LOCAL_0x0
ldc.i8 0x1eaae9e57b942ee6 
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc LOCAL_0x3
ldc.i4 0x56220310 
stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x5
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x6
ldc.i4 255
 newarr [mscorlib]System.Double 
 stloc LOCAL_0x7
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc LOCAL_0x8
ldc.i4 0xc4704460 
stloc LOCAL_0x9
ldc.i4 0x939030b7 
stloc LOCAL_0xa

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x97f1b6bf::field_0x0
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x97f1b6bf::field_0x1
ldc.i8 0xafdbe0157639d9a7 
stsfld unsigned int64 ILGEN_0x97f1b6bf::field_0x2
ldc.i4 0xa7152895 
stsfld unsigned int32 ILGEN_0x97f1b6bf::field_0x3
ldc.i4 0x2be987db 
stsfld native int ILGEN_0x97f1b6bf::field_0x4
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x97f1b6bf::field_0x5
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x97f1b6bf::field_0x6
ldc.i8 0xc77a86231c6c028 
stsfld unsigned int64 ILGEN_0x97f1b6bf::field_0x7
ldc.r4 float32(0x6aa906bf) 
stsfld float32 ILGEN_0x97f1b6bf::field_0x8
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x97f1b6bf::field_0x9
ldc.i8 0x959a51dc85d98f1d 
stsfld int64 ILGEN_0x97f1b6bf::field_0xa
ldc.i4 0xcb7e1ab9 
stsfld unsigned int8 ILGEN_0x97f1b6bf::field_0xb
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x97f1b6bf::field_0xc
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x97f1b6bf::field_0xd
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stsfld int64[] ILGEN_0x97f1b6bf::field_0xe

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_a_1:
   nop
End_Orphan_a_1:
    ldloc LOCAL_0x8
ldc.i4 0
    //ldsfld unsigned int32 ILGEN_0x97f1b6bf::field_0x3
Start_Orphan_b_2:
   nop
End_Orphan_b_2:
   ldelema [mscorlib]System.Int32
Start_Orphan_a_3:
   nop
End_Orphan_a_3:
    ldloc LOCAL_0x8
ldc.i4 0
    //ldsfld unsigned int8 ILGEN_0x97f1b6bf::field_0xb
Start_Orphan_b_4:
   nop
End_Orphan_b_4:
   ldelema [mscorlib]System.Int32
  cpobj [mscorlib]System.Int32
End_Orphan_a_0:
  ldnull
Start_Orphan_a_5:
Start_Orphan_a_6:
   nop
End_Orphan_a_6:
   ldsflda unsigned int8 ILGEN_0x97f1b6bf::field_0xb
  pop
End_Orphan_a_5:
Start_Orphan_a_7:
    ldloc LOCAL_0x0
   stloc LOCAL_0x0
End_Orphan_a_7:
   sizeof [mscorlib]System.Single
Start_Orphan_b_8:
   ldloc LOCAL_0x8
   sizeof [mscorlib]System.UInt16
Start_Orphan_a_9:
   nop
End_Orphan_a_9:
    ldloc LOCAL_0x1
 brtrue Branch_0x4

    ldloc LOCAL_0xa
 br Branch_0x5

 Branch_0x4:

    sizeof [mscorlib]System.Byte
 Branch_0x5:

  stelem.i4
End_Orphan_b_8:
  box [mscorlib]System.UInt32
 beq Branch_0x2

  ldsfld int64 ILGEN_0x97f1b6bf::field_0xa
 br Branch_0x3

 Branch_0x2:

     ldsfld native int ILGEN_0x97f1b6bf::field_0x4
     sizeof [mscorlib]System.Single
 bgt Branch_0xc

     ldc.i4 0x789987af
 br Branch_0xd

 Branch_0xc:

     ldc.i4.7
 Branch_0xd:

     //ldc.r8 float64(0xbe87c29f7f3a842d)
    //conv.ovf.i
ldc.i4 0
 beq Branch_0xa

     ldc.r8 float64(0xb04721ba8b234a88)
     ldc.r8 float64(0x5645197329d37d7d)
Start_Orphan_b_a:
    nop
End_Orphan_b_a:
    sub  
 br Branch_0xb

 Branch_0xa:

     ldsfld float32 ILGEN_0x97f1b6bf::field_0x8
    ckfinite
 Branch_0xb:

Start_Orphan_b_b:
    nop
End_Orphan_b_b:
    //ldsfld float64[] ILGEN_0x97f1b6bf::field_0x9
Start_Orphan_a_c:
    nop
End_Orphan_a_c:
     //ldloca LOCAL_0x9
    //ldobj [mscorlib]System.SByte
Start_Orphan_b_d:
    ldloc LOCAL_0x5
   pop
End_Orphan_b_d:
   //ldelem.r8
     ldc.r8 float64(0xb04721ba8b234a88)
 bge Branch_0x6

Start_Orphan_a_e:
   nop
End_Orphan_a_e:
Start_Orphan_a_f:
    nop
End_Orphan_a_f:
     ldsfld int64 ILGEN_0x97f1b6bf::field_0xa
     sizeof [mscorlib]System.SByte
    shr.un
Start_Orphan_a_10:
    nop
End_Orphan_a_10:
     ldsfld float32 ILGEN_0x97f1b6bf::field_0x8
Start_Orphan_b_11:
    nop
End_Orphan_b_11:
    conv.i8
Start_Orphan_b_12:
    ldsfld int64[] ILGEN_0x97f1b6bf::field_0xe
   pop
End_Orphan_b_12:
   rem
 br Branch_0x7

 Branch_0x6:

Start_Orphan_a_13:
    nop
End_Orphan_a_13:
     ldloc LOCAL_0x1
     ldsfld unsigned int32 ILGEN_0x97f1b6bf::field_0x3
    shr.un
     ldsfld int64 ILGEN_0x97f1b6bf::field_0xa
     sizeof [mscorlib]System.Double
Start_Orphan_b_14:
    nop
End_Orphan_b_14:
    shl
Start_Orphan_b_15:
    ldsflda float32 ILGEN_0x97f1b6bf::field_0x8
    ldc.r8 float64(0x137eecb03968be93)
   stobj [mscorlib]System.Single
End_Orphan_b_15:
   rem.un
 Branch_0x7:

 Branch_0x3:

//conv.ovf.u2
conv.u2
 ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	100

 call unsigned int32  ILGEN_0x97f1b6bf::Method_0xda70a253()
conv.i4
ldc.i4 36537
sub
 ret
}

}
