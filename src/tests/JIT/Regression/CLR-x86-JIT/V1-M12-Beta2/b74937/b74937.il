// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x845850e7.exe {}
.class ILGEN_0x845850e7 {
.method static float64  Method_0x498875e6(int64 Arg_0x0, float32 Arg_0x4) {
.maxstack	15
.locals (native int LOCAL_0x1,native int LOCAL_0x2,unsigned int32[] LOCAL_0x3)
 ldc.i4 0xc298b3eb 
 stloc LOCAL_0x1
 ldc.i4 0x329d8322 
 stloc LOCAL_0x2
 ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc LOCAL_0x3
       ldloc LOCAL_0x2
   conv.u8
       ldarg Arg_0x0
  conv.ovf.i8
       ldloc LOCAL_0x2
      conv.r4
     call float64 [mscorlib]System.Math::Sin(float64)
       ldloc LOCAL_0x1
   conv.r.un
  cgt
 shr
 blt Branch_0x0
       ldloc LOCAL_0x2
     conv.r.un
Start_Orphan_18:
      ldloc LOCAL_0x1
     conv.ovf.i2.un
    conv.r8
    ldarg Arg_0x4
   add 
  starg Arg_0x4
End_Orphan_18:
 br Branch_0x1
Branch_0x0:
 ldarg Arg_0x0
 conv.r.un
Branch_0x1:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	15
ldc.i8 0x60def450d87d153c 
ldc.r4 float32(0xbb1960fe) 
 call float64  ILGEN_0x845850e7::Method_0x498875e6(int64 Arg_0x0,float32 Arg_0x4)
 conv.i4
 dup
 xor
 ldc.i4.s 100
 or
 ret
}
}
