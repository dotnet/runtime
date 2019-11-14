// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x8384f4c4 {}
.class ILGEN_0x8384f4c4 {
.field static	unsigned int64 field_0x0
.method static int32 Method_0x3d8c15be(int64 Arg_0x0, float32 Arg_0x1) {
.maxstack	17
Start_Orphan_3c:
 ldc.i4 55
 newarr [mscorlib]System.Single
 ldc.i4 255
 newarr [mscorlib]System.Int64 
 ldc.i4 0x50595a71 
 ldelem.i8
 conv.ovf.u8.un
 conv.u4
Start_Orphan_41:
 ldc.i4.1
 conv.u1
 newarr [mscorlib]System.Int64
 ldarg Arg_0x1
 conv.u1
 ldc.i8 0x30ffd5f1b3ffd155b24d98a557290806 
 ldarg Arg_0x0
 or
 stelem.i8
End_Orphan_41:
Start_Orphan_45:
 ldarg Arg_0x0
 stsfld unsigned int64 ILGEN_0x8384f4c4::field_0x0
End_Orphan_45:
 ldc.r8 float64(0x6e5708fad1e07fb4) 
 stelem.r4
End_Orphan_3c:
 ldc.i4 55
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0xb24d98a557290806 
ldc.r4 float32(0xa3938da7) 
call int32 ILGEN_0x8384f4c4::Method_0x3d8c15be(int64 Arg_0x0, float32 Arg_0x1)
pop
leave EEE
} catch [mscorlib]System.IndexOutOfRangeException {
pop
leave EEE
}
EEE:
ldc.i4 100
 ret
}
}