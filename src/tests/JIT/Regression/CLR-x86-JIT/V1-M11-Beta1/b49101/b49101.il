// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x6a2f58fb {}
.class ILGEN_0x6a2f58fb {

.method static int32 Method_0x89b7ef42(int32 Arg_0x2,float32 Arg_0x4) {
.maxstack	13

 .locals (float64 local_0x0,int8 local_0x1)

ldc.r8 float64(0xa237a1b1935ccc78) 
stloc local_0x0
ldc.i4 0xea93110d 
stloc local_0x1

Start_Orphan_0:
 ldloca local_0x1
Start_Orphan_1:
Start_Orphan_2:
 ldc.r8 float64(0xbda8eae5589f5b0f)
 ldarg Arg_0x4
 blt Branch_0x2
 br Branch_0x3
Branch_0x2:
Branch_0x3:
End_Orphan_2:
 ldc.i4 0x63dd4f2a
 ldc.i4.0
 ble Branch_0x0
 ldarg Arg_0x2
 brtrue Branch_0x8
 ldc.r4 float32(0x3b873359)
 br Branch_0x9
Branch_0x8:
 ldc.r4 float32(0x500ea3a4)
Branch_0x9:
 ldloc local_0x0
 ldarg Arg_0x4
 add 
 bgt Branch_0x6
 br Branch_0x7
Branch_0x6:
Branch_0x7:
Branch_0x0:
Branch_0x1:

End_Orphan_1:
 ldc.i4.s 98
 stind.i1
End_Orphan_0:
ldc.i4 55
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i4 0xeaec0788 
ldc.r4 float32(0x39b13b98) 

call int32 ILGEN_0x6a2f58fb::Method_0x89b7ef42(int32 Arg_0x2, float32 Arg_0x4)
ldc.i4 55
sub
ldc.i4 100
add
 ret
}

}
