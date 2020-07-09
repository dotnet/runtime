// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2e384c7a {}
.class ILGEN_0x2e384c7a {

.method static int32 Method_0x5f927ae5(int16 Arg_0x0, int8 Arg_0x1, int32 Arg_0x2, int16 Arg_0x3, unsigned int32 Arg_0x4, float32 Arg_0x5, unsigned int32 Arg_0x6, float32 Arg_0x7, int8 Arg_0x8) {
.maxstack	9

 .locals (int32[] local_0x0,int16 local_0x1,unsigned int32[] local_0x2,unsigned int16[] local_0x3,int64[] local_0x4,int32 local_0x5,int32 local_0x6,int16[] local_0x7)
ldc.i4 255
 newarr [mscorlib]System.Int32 
 stloc local_0x0
ldc.i4 0x265e33f8 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2
ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x4
ldc.i4 0xb77efb83 
stloc local_0x5
ldc.i4 0x11b56939 
stloc local_0x6
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x7

Start_Orphan_0:
 ldarga Arg_0x4
Start_Orphan_1:
 ldc.i4.7
 ldc.i4.5
 beq Branch_0x0

 nop
 br Branch_0x1

 Branch_0x0:

 nop
 Branch_0x1:

End_Orphan_1:
Start_Orphan_2:
 nop
End_Orphan_2:
 ldc.i8 0x5705e3c92648806d
 ldc.i8 0xef574414f226db0d
 rem
 ldc.i8 0xf4cf9da7e868c8c8
 ldc.i8 0x84b11dd3e004c7ef
 bge.un Branch_0x2

 ldc.i8 0xa7447d43766f5561
 br Branch_0x3

 Branch_0x2:

 ldc.i8 0x73c41e49b86c40a1
 Branch_0x3:

 ceq
 stind.i4
End_Orphan_0:
Start_Orphan_3:
 ldloc local_0x2
 ldc.i8 0x5bf02f3e8d1167d2
 ldc.i8 0x4ebda656d1a264f9
 bne.un Branch_0x4

 ldc.i4.0
 br Branch_0x5

 Branch_0x4:

 ldarg Arg_0x1
 Branch_0x5:

 ldarg Arg_0x2
 ldarg Arg_0x6
 blt.un Branch_0x6

 ldloc local_0x1
 br Branch_0x7

 Branch_0x6:

 ldc.i4.s 68
 Branch_0x7:

 stelem.i4
End_Orphan_3:
 ldloc local_0x7
 ldlen
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0x249ba163 
ldc.i4 0xe204845a 
ldc.i4 0x9c10e44b 
ldc.i4 0x84f1655a 
ldc.i4 0x53620a14 
ldc.r4 float32(0x9879a9a5) 
ldc.i4 0xf0593615 
ldc.r4 float32(0xbd969085) 
ldc.i4 0xddc1c95e 

call int32 ILGEN_0x2e384c7a::Method_0x5f927ae5(int16 Arg_0x0, int8 Arg_0x1, int32 Arg_0x2, int16 Arg_0x3, unsigned int32 Arg_0x4, float32 Arg_0x5, unsigned int32 Arg_0x6, float32 Arg_0x7, int8 Arg_0x8)
ldc.i4 -155
add
 ret
}

}
