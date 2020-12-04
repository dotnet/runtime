// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b72160 { }
.class ILGEN_0xace3f910 {

//BEGIN STATIC FIELDS
.field static	int64 field_0x0
.field static	unsigned int64[] field_0x1
//END STATIC FIELDS
.method static float32  Method_0xf9cc7d6a(unsigned int64 Arg_0x0, int64 Arg_0x1, float32 Arg_0x2, unsigned int32 Arg_0x3, unsigned int16 Arg_0x4, native int Arg_0x5, int64 Arg_0x6, unsigned int64 Arg_0x7, unsigned int64 Arg_0x8) {
.maxstack	15

 .locals (float32[] LOCAL_0x0,unsigned int8[] LOCAL_0x1,unsigned int8[] LOCAL_0x2,native int[] LOCAL_0x3)
//BEGIN INIT LOCALS
ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x0
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x1
ldc.i4 255
 newarr [mscorlib]System.Byte 
 stloc LOCAL_0x2
ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stloc LOCAL_0x3

//END INIT LOCALS

//BEGIN INIT OF STATIC FIELDS
ldc.i8 0xf35c68974a72ea19 
stsfld int64 ILGEN_0xace3f910::field_0x0
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stsfld unsigned int64[] ILGEN_0xace3f910::field_0x1

//END INIT OF STATIC FIELDS

Start_Orphan_0:
Start_Orphan_1:
Start_Orphan_2:
    ldarg Arg_0x4
    ldarg Arg_0x5
   mul.ovf.un
  pop
End_Orphan_2:
End_Orphan_1:
End_Orphan_0:
Start_Orphan_c:
Start_Orphan_d:
Start_Orphan_e:
    ldsfld int64 ILGEN_0xace3f910::field_0x0
   starg Arg_0x0
End_Orphan_e:
Start_Orphan_f:
    nop
End_Orphan_f:
     ldc.r4 float32(0x301130db)
    call float32 [mscorlib]System.Math::Abs(float32)
   conv.r8
  starg Arg_0x2
End_Orphan_d:
Start_Orphan_10:
    ldarga Arg_0x5
     ldarg Arg_0x5
    conv.ovf.i2.un
   stind.i
End_Orphan_10:
Start_Orphan_12:
     ldsfld unsigned int64[] ILGEN_0xace3f910::field_0x1
    stsfld unsigned int64[] ILGEN_0xace3f910::field_0x1
End_Orphan_12:
      ldarg Arg_0x1
    conv.ovf.i8.un
   conv.ovf.u2.un
Start_Orphan_14:
    ldarga Arg_0x3
    ldc.i4.8
   stind.i4
End_Orphan_14:
 starg Arg_0x3
End_Orphan_c:
Start_Orphan_18:
      ldarg Arg_0x5
    conv.u1
 newarr [mscorlib]System.Byte
      ldc.i4.4
     conv.u1
 newarr [mscorlib]System.UInt64
    ldlen
      ldarg Arg_0x5
      ldarg Arg_0x5
     conv.ovf.i.un
    cgt.un
   stelem.i1
End_Orphan_18:
       ldarg Arg_0x1
      conv.r8
     conv.ovf.u8
Start_Orphan_1d:
      ldloc LOCAL_0x2
      ldc.i4.0
      ldc.i4.2
     stelem.i1
End_Orphan_1d:
       ldarg Arg_0x6
      conv.ovf.i2
     conv.ovf.u8
       ldsfld int64 ILGEN_0xace3f910::field_0x0
       ldarg Arg_0x0
      sub.ovf
       ldsfld int64 ILGEN_0xace3f910::field_0x0
       ldsfld int64 ILGEN_0xace3f910::field_0x0
      mul
     cgt
    shr
   sub.ovf
  conv.ovf.i
 conv.i2
conv.r8
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	19
.try {
ldc.i8 0x197dd50a587f8d37 
ldc.i8 0x90ccc9182f24457 
ldc.r4 float32(0x4547d592) 
ldc.i4 0xdf6d0d07 
ldc.i4 0x6400921a 
ldc.i4 0x2c2f5c65 conv.i
ldc.i8 0x91cb6274b8b88118 
ldc.i8 0x122a3e69569bdf6a 
ldc.i8 0x20f8f2749ea23a66 

 call float32  ILGEN_0xace3f910::Method_0xf9cc7d6a(unsigned int64 Arg_0x0, int64 Arg_0x1, float32 Arg_0x2, unsigned int32 Arg_0x3, unsigned int16 Arg_0x4, native int Arg_0x5, int64 Arg_0x6, unsigned int64 Arg_0x7, unsigned int64 Arg_0x8)
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
