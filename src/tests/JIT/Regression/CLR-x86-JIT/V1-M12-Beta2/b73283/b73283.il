// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1f0f388d.exe {}
.class ILGEN_0x1f0f388d {
.method static int32 Main() {
.entrypoint
.maxstack	9
.locals (float64 LOCAL_0x3,float32[] LOCAL_0x5)
 ldc.r8 float64(0x8aebe5526c516b9e) 
 stloc LOCAL_0x3
 ldc.i4 255
 newarr [mscorlib]System.Single 
 stloc LOCAL_0x5
    ldc.r8 float64(0x62c585fa1b3f233d)
   ckfinite
Start_Orphan_2:
   ldc.r4 float32(0x3d86ebcd)
   ldloc LOCAL_0x3
  rem 
 stloc LOCAL_0x3
End_Orphan_2:
    ldloc LOCAL_0x5
    ldc.i4.0
   ldelem.r4
  conv.r8
   ldloc LOCAL_0x3
  call float64 [mscorlib]System.Math::Sin(float64)
 div 
 ble Branch_0x0
  ldc.i4.0
 conv.ovf.u8
 br Branch_0x1
Branch_0x0:
 ldc.i8 0xd3f01f1b2c75e0bd
Branch_0x1:
 conv.i4 
 ldc.i4.s 100
 add
 ret
}
}
