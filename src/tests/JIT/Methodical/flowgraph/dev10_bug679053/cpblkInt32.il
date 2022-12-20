// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly 'cpblkInt32' {}
.assembly extern xunit.core {}
.assembly extern mscorlib{auto}

.class public ILGEN_0xb8816d7d {

//BEGIN STATIC FIELDS
.field static native int[] field_0x0
//END STATIC FIELDS
.method static float32  Method_0xac55c91a(float32 Arg_0x0, int16 Arg_0x1, int16 Arg_0x2, unsigned int16 Arg_0x3, unsigned int8 Arg_0x4, unsigned int16 Arg_0x5) {
.zeroinit 
.maxstack	9

 .locals (int8[] LOCAL_0x0,native int LOCAL_0x1,int64 LOCAL_0x2,float32 LOCAL_0x3,object LOCAL_0x4,float32 LOCAL_0x5,unsigned int32 LOCAL_0x6,native int[] LOCAL_0x7,float64 LOCAL_0x8,int32 LOCAL_0x9,unsigned int32[] LOCAL_0xa,unsigned int32[] LOCAL_0xb,int64 LOCAL_0xc)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x0
ldc.i4 0x6aeefaf2 
stloc LOCAL_0x1
ldc.i8 0xc00333c25f8e3a82 
stloc LOCAL_0x2
ldc.r4 float32(0x9aacd1ba) 
stloc LOCAL_0x3
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x4
ldc.r4 float32(0x42762e1f) 
stloc LOCAL_0x5
ldc.i4 0x704c8a18 
stloc LOCAL_0x6
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x7
ldc.r8 float64(0xb78fdc7f62103239) 
stloc LOCAL_0x8
ldc.i4 0xaeb1868f 
stloc LOCAL_0x9
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0xa
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0xb
ldc.i8 0xeddfded3b06b74a 
stloc LOCAL_0xc

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0xb8816d7d::field_0x0

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_a_0:
Start_Orphan_b_1:
  call void [mscorlib]System.GC::Collect()
End_Orphan_b_1:
  ldloca LOCAL_0xc
Start_Orphan_a_2:
  call void [mscorlib]System.GC::Collect()
End_Orphan_a_2:
   ldloc LOCAL_0x2
   ldc.i8 0x9a790d8a28f3ffce
Start_Orphan_b_3:
  nop
End_Orphan_b_3:
  div.un
 stind.i8
End_Orphan_a_0:
Start_Orphan_a_4:
   nop
End_Orphan_a_4:
    ldloc LOCAL_0x1
    ldloc LOCAL_0x9
Start_Orphan_b_5:
   call void [mscorlib]System.GC::Collect()
End_Orphan_b_5:
   rem
Start_Orphan_b_6:
   ldarga Arg_0x5
   ldloc LOCAL_0x9
  ldc.i4 2 
 unaligned. 0x1 
 volatile. 
 initblk
End_Orphan_b_6:
  conv.ovf.i4
 conv.u1
Start_Orphan_a_7:
Start_Orphan_a_8:
  nop
End_Orphan_a_8:
   ldloc LOCAL_0x1
Start_Orphan_b_9:
  call void [mscorlib]System.GC::Collect()
End_Orphan_b_9:
  not
 stloc LOCAL_0x1
End_Orphan_a_7:
  ldloc LOCAL_0x1
Start_Orphan_a_a:
   ldarga Arg_0x0
   ldloca LOCAL_0x3
  cpobj [mscorlib]System.Single
End_Orphan_a_a:
Start_Orphan_a_b:
   nop
End_Orphan_a_b:
   ldc.i4.m1
   sizeof [mscorlib]System.UInt64
Start_Orphan_b_c:
   ldloca LOCAL_0x1
   ldloca LOCAL_0x1
  cpobj [mscorlib]System.IntPtr
End_Orphan_b_c:
  cgt
 mul
 bgt.un Branch_0x8

Start_Orphan_a_d:
Start_Orphan_a_e:
  call void [mscorlib]System.GC::Collect()
End_Orphan_a_e:
  ldloca LOCAL_0x8
Start_Orphan_a_f:
  call void [mscorlib]System.GC::Collect()
End_Orphan_a_f:
  ldloca LOCAL_0x8
 cpobj [mscorlib]System.Double
End_Orphan_a_d:
Start_Orphan_b_10:
   ldarga Arg_0x2
  initobj [mscorlib]System.Int16
End_Orphan_b_10:
  ldloc LOCAL_0xc
 conv.r8
 br Branch_0x9

 Branch_0x8:

Start_Orphan_a_11:
   ldloca LOCAL_0x9
   ldarg Arg_0x4
  ldc.i4 4 
 initblk
End_Orphan_a_11:
Start_Orphan_b_12:
   ldloca LOCAL_0x6
   ldloca LOCAL_0x6
  ldc.i4 4 
 cpblk
End_Orphan_b_12:
  ldarga Arg_0x0
 ldobj [mscorlib]System.Single
 Branch_0x9:

 ret
}
.method public static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	13
ldc.r4 float32(0x177ad9a4) 
ldc.i4 0xb7272bfc 
ldc.i4 0x5ce5799d 
ldc.i4 0x79c18539 
ldc.i4 0xa3a77bd5 
ldc.i4 0x17799c06 

 call float32  ILGEN_0xb8816d7d::Method_0xac55c91a(float32 Arg_0x0, int16 Arg_0x1, int16 Arg_0x2, unsigned int16 Arg_0x3, unsigned int8 Arg_0x4, unsigned int16 Arg_0x5)
conv.i4
ldc.i4 100
add
 ret
}

}
