// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x3155fdf {}
.class ILGEN_0x3155fdf {
.method static int32 Method_0xcfd10851(int64 Arg_0x2, int8 Arg_0x3, unsigned int64 Arg_0x5, int8 Arg_0x6) {
.maxstack	11
 .locals (unsigned int8 LOCAL_0x1,int32  LOCAL_0x4)
 ldc.i4 4
 stloc LOCAL_0x1
 ldc.i4 5
 stloc LOCAL_0x4
Start_Orphan_0:
 ldarg Arg_0x2
 conv.i1
 starg Arg_0x3
End_Orphan_0:
 ldloc LOCAL_0x1
 ldarg Arg_0x6
 bne.un Branch_0xe
 ldc.i4.0
 br Branch_0xf
Branch_0xe:
 ldarg Arg_0x3
Branch_0xf:
 ldc.i4 6
 blt.un Branch_0xc
 ldc.r8 3.4
 br Branch_0xd
Branch_0xc:
 ldc.r8 1.0
Branch_0xd:
 pop
Start_Orphan_23:
 ldloca LOCAL_0x4
 ldc.i4.7
 stind.i4
End_Orphan_23:
 ldarg Arg_0x5
Branch_0x65:
 conv.i4
Branch_0x3b:
 br Branch_0x1
Branch_0x1:
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
 ldc.i8 11 
 ldc.i4 12
 ldc.i8 13
 ldc.i4 14
 call int32 ILGEN_0x3155fdf::Method_0xcfd10851(int64 Arg_0x2, int8 Arg_0x3, unsigned int64 Arg_0x5, int8 Arg_0x6)
 ldc.i4 87
 add
 ret
}
}