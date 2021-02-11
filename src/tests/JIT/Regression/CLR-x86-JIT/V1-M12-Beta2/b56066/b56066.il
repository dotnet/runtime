// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x3e559786 {}
.class ILGEN_0x3e559786 {
.method static int32 Method_0x17d83b09(float32 Arg_0x4, int16 Arg_0x5) {
.maxstack	15
Start_Orphan_0:
 ldarg Arg_0x4
 ldc.r4 float32(0xe7132384)
 bgt Branch_0x8
 ldc.i8 0x75f257213c1a512a
 br Branch_0x9
Branch_0x8:
 ldc.i8 0x6833668a90a08ef2
Branch_0x9:
 ldc.i4.6
 conv.i8
 bgt.un Branch_0x6
 ldarg Arg_0x5
 ldc.i4.1
 div
 br Branch_0x7
Branch_0x6:
 ldc.i4.8
Branch_0x7:
 pop
End_Orphan_0:
 ldc.i4.8
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
 ldc.r4 float32(0x49605a96) 
 ldc.i4 0xf2f5dc01 
 call int32 ILGEN_0x3e559786::Method_0x17d83b09(float32 Arg_0x4, int16 Arg_0x5)
 ldc.i4 92
 add
 ret
}
}
