// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x1d9cf497 {}
.class ILGEN_0x1d9cf497 {
.method static int32 Main() {
.entrypoint
.maxstack	20

 .locals (unsigned int16 local_0x0,unsigned int64 local_0x1)

ldc.i4 0xb24efc14 
stloc local_0x0
ldc.i8 0xca91a31aa3a4a98 
stloc local_0x1

Start_Orphan_0:
Start_Orphan_1:
 ldc.i4.m1
 conv.u4
 conv.ovf.u8.un
 conv.ovf.u.un
 conv.r4
Start_Orphan_9:
 ldloca local_0x1
 ldloc local_0x1
 stind.i8
End_Orphan_9:
 ldc.r4 float32(0xd4aef883)
 ldc.r8 float64(0x63457ba34dd852f1)
 add
 conv.r4
 conv.r8
 ldloc local_0x0
 conv.r8
 clt.un
 conv.r4
 bge Branch_0x0
 br Branch_0x1
Branch_0x0:
Branch_0x1:

End_Orphan_1:
End_Orphan_0:
ldc.i4 100
 ret
}
}
