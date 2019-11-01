// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xcfefefe5 {}
.class ILGEN_0xcfefefe5 {

//BEGIN STATIC FIELDS
.field static	int16[] field_0x0
.field static	unsigned int32 field_0x1
.field static	float64 field_0x2
.field static	float64[] field_0x3
.field static native int[] field_0x4
.field static	unsigned int32[] field_0x5
.field static	int64 field_0x6
.field static	int16 field_0x7
.field static	unsigned int16[] field_0x8
.field static	int8[] field_0x9
.field static	float32 field_0xa
//END STATIC FIELDS
.method static int64  Method_0xf122041b(unsigned int16 Arg_0x0, int32 Arg_0x1, float64 Arg_0x2, int32 Arg_0x3, int16 Arg_0x4, float64 Arg_0x5, unsigned int32 Arg_0x6, float32 Arg_0x7, int64 Arg_0x8, unsigned int8 Arg_0x9, unsigned int32 Arg_0xa, int8 Arg_0xb, float64 Arg_0xc) {
.maxstack	15

 .locals (object LOCAL_0x0,float32 LOCAL_0x1,int8[] LOCAL_0x2,unsigned int32 LOCAL_0x3,int16 LOCAL_0x4)
//BEGIN INIT LOCALS
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x0
ldc.r4 float32(0x9fb4d604) 
stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x2
ldc.i4 0x6ce06abb 
stloc LOCAL_0x3
ldc.i4 0x7ad43165 
stloc LOCAL_0x4

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stsfld int16[] ILGEN_0xcfefefe5::field_0x0
ldc.i4 0xf680b7b1 
stsfld unsigned int32 ILGEN_0xcfefefe5::field_0x1
ldc.r8 float64(0xf2f1d1ba441b9a28) 
stsfld float64 ILGEN_0xcfefefe5::field_0x2
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0xcfefefe5::field_0x3
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0xcfefefe5::field_0x4
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xcfefefe5::field_0x5
ldc.i8 0x7ca4a965fdff701f 
stsfld int64 ILGEN_0xcfefefe5::field_0x6
ldc.i4 0x550a80a5 
stsfld int16 ILGEN_0xcfefefe5::field_0x7
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0xcfefefe5::field_0x8
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xcfefefe5::field_0x9
ldc.r4 float32(0x806b40cd) 
stsfld float32 ILGEN_0xcfefefe5::field_0xa

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
 ldsflda int16 ILGEN_0xcfefefe5::field_0x7
Start_Orphan_2:
  ldsfld int16[] ILGEN_0xcfefefe5::field_0x0
     ldc.i4.s 122
     ldc.i4.4
 blt.un Branch_0x0
     ldc.r4 float32(0xe730e7a3)
 br Branch_0x1
 Branch_0x0:
     ldc.r4 float32(0x42662cc0)
 Branch_0x1:
   conv.ovf.i4
Start_Orphan_3:
    ldc.i4.s -57
   stloc LOCAL_0x4
End_Orphan_3:
     ldc.i8 0xe3efa97637bbdcba
     ldsfld int64 ILGEN_0xcfefefe5::field_0x6
 blt Branch_0x8
     ldc.i4.2
 br Branch_0x9
 Branch_0x8:
     ldc.i4.5
 Branch_0x9:
 Branch_0x3:
  add
Start_Orphan_6:
    ldloc LOCAL_0x4
   starg Arg_0x9
End_Orphan_6:
     ldc.i4.s -78
     ldarg Arg_0x0
 bgt.un Branch_0xa
     ldc.i8 0x54cc12fd57f7f4
    conv.u1
 br Branch_0xb
 Branch_0xa:
     ldc.r8 float64(0xaa33ef28cd45dbcc)
     ldarg Arg_0x7
 blt.un Branch_0xc
     ldc.i4 0x777d3f79
 br Branch_0xd
 Branch_0xc:
     ldc.i4.3
 Branch_0xd:
 Branch_0xb:
  conv.ovf.u4
 stelem.i2
End_Orphan_2:
Start_Orphan_9:
    ldc.r4 float32(0xac0e09d3)
   call float64 [mscorlib]System.Math::Sin(float64)
     ldc.i4.1
    conv.u
   conv.r8
 ble.un Branch_0xe
    newobj     instance void [mscorlib]System.Object::.ctor()
   stloc LOCAL_0x0
 br Branch_0xf
 Branch_0xe:
     ldsfld int64 ILGEN_0xcfefefe5::field_0x6
   starg Arg_0x8
 Branch_0xf:
End_Orphan_9:
  ldarg Arg_0xb
stind.i2
End_Orphan_0:
Start_Orphan_11:
     ldarga Arg_0x1
      ldarga Arg_0x3
     ldind.i4
    stind.i
End_Orphan_11:
       ldsfld int64 ILGEN_0xcfefefe5::field_0x6
      conv.i
     conv.ovf.u8
Start_Orphan_13:
      ldarga Arg_0x9
      ldloc LOCAL_0x4
     stind.i1
End_Orphan_13:
       ldloc LOCAL_0x4
      conv.ovf.u8
    sub.ovf
   conv.ovf.i4.un
  conv.u2
       ldsfld int64 ILGEN_0xcfefefe5::field_0x6
     not
       ldc.i8 0x91519ea019fff590
    cgt.un
   conv.u1
 newarr [mscorlib]System.IntPtr
       ldc.i4.3
     conv.ovf.u1.un
  ldelem.i
 clt.un
conv.i8
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	27
.try {
ldc.i4 0x8b4a5d41 
ldc.i4 0xd31361fa 
ldc.r8 float64(0x3b70242d76985d02) 
ldc.i4 0xdf0210de 
ldc.i4 0x61a0208e 
ldc.r8 float64(0xdce14fd31dda7335) 
ldc.i4 0x5e335f73 
ldc.r4 float32(0x5d9be61) 
ldc.i8 0x26602f1058a78389 
ldc.i4 0xea8685bd 
ldc.i4 0x7677684 
ldc.i4 0x95adf6b5 
ldc.r8 float64(0x1190d31c1154fb42) 

 call int64  ILGEN_0xcfefefe5::Method_0xf122041b(unsigned int16 Arg_0x0, int32 Arg_0x1, float64 Arg_0x2, int32 Arg_0x3, int16 Arg_0x4, float64 Arg_0x5, unsigned int32 Arg_0x6, float32 Arg_0x7, int64 Arg_0x8, unsigned int8 Arg_0x9, unsigned int32 Arg_0xa, int8 Arg_0xb, float64 Arg_0xc)
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
