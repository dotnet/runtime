// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x3ab555a7 {}
.class ILGEN_0x3ab555a7 {

//BEGIN STATIC FIELDS
.field static	unsigned int16[] field_0x0
.field static	int64 field_0x1
.field static	float64[] field_0x2
.field static	unsigned int64[] field_0x3
.field static	int8[] field_0x4
//END STATIC FIELDS
.method static int8  Method_0x9ccd51b8(unsigned int8 Arg_0x0, int64 Arg_0x1, int64 Arg_0x2, unsigned int32 Arg_0x3) {
.maxstack	17

 .locals (unsigned int8[] LOCAL_0x0,int16[] LOCAL_0x1,unsigned int64 LOCAL_0x2,unsigned int8[] LOCAL_0x3,native int[] LOCAL_0x4,float32[] LOCAL_0x5,object LOCAL_0x6)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc LOCAL_0x1
ldc.i8 0x11a2b92dbea0cc 
stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x3
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x4
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x5
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x6

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x3ab555a7::field_0x0
ldc.i8 0xcd1ac96cc0338adc 
stsfld int64 ILGEN_0x3ab555a7::field_0x1
ldc.i4 255
 newarr [mscorlib]System.Double 
 stsfld float64[] ILGEN_0x3ab555a7::field_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0x3ab555a7::field_0x3
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0x3ab555a7::field_0x4

//END INIT OF STATIC FIELDS

 BLOCK_1:
Start_Orphan_0:
    ldc.r4 float32(0x4f91655f)
   call float64 [mscorlib]System.Math::Cos(float64)
 conv.ovf.i4
 brfalse Branch_0x0
    ldsfld unsigned int16[] ILGEN_0x3ab555a7::field_0x0
       ldc.i4 0xd4c56ce9
      conv.ovf.u1.un
     conv.ovf.u8.un
       ldsfld int64 ILGEN_0x3ab555a7::field_0x1
      conv.ovf.i8
    cgt
   ldelema [mscorlib]System.UInt16
  ldind.u2
 starg Arg_0x3
 br Branch_0x1

 Branch_0x0:

  ldloc LOCAL_0x1
       ldsfld unsigned int16[] ILGEN_0x3ab555a7::field_0x0
       ldc.i4.3
      ldelema [mscorlib]System.UInt16
     ldind.u2
       ldarg Arg_0x0
      conv.r4
     conv.ovf.u8
       ldarg Arg_0x1
    cgt
 stelem.i2
 Branch_0x1:

End_Orphan_0:
Start_Orphan_1e:
  ldarga Arg_0x3
   ldc.r4 float32(0xda90edac)
Start_Orphan_25:
      ldsfld unsigned int16[] ILGEN_0x3ab555a7::field_0x0
      ldc.i4.8
      ldc.i4.4
     stelem.i2
End_Orphan_25:
     ldsfld float64[] ILGEN_0x3ab555a7::field_0x2
       ldc.i4.0
      conv.ovf.i8
     conv.u4
    ldelem.r8
   call float64 [mscorlib]System.Math::Sin(float64)
  ceq
 stind.i4
End_Orphan_1e:
ldc.i4 55
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	9
ldc.i4 0xdb4fdfdd 
ldc.i8 0xb4d6727afd7a93ad 
ldc.i8 0x422560881dc344df 
ldc.i4 0x6ec931e9 

 call int8  ILGEN_0x3ab555a7::Method_0x9ccd51b8(unsigned int8 Arg_0x0, int64 Arg_0x1, int64 Arg_0x2, unsigned int32 Arg_0x3)
pop
ldc.i4.s 100

 ret
}

}
