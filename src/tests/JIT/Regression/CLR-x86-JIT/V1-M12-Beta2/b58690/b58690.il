// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x78389777 {}
.class ILGEN_0x78389777 {
.field static native int[] field_0x4
.method static int32 Method_0x440f1192(int16 Arg_0x0, unsigned int8 Arg_0x2) {
.maxstack	13

 ldc.i4 255
 newarr [mscorlib]System.IntPtr 
 stsfld native int[] ILGEN_0x78389777::field_0x4

Start_Orphan_0:
 ldc.i4.0
 ldc.i4.0
 blt Branch_0x2
 ldc.i4.s 121
 br Branch_0x3
Branch_0x2:
 ldc.i4.s 123
Branch_0x3:
 brtrue Branch_0x0
 ldsfld native int[] ILGEN_0x78389777::field_0x4
 ldarg Arg_0x2
 ldelema [mscorlib]System.IntPtr
 ldc.i4 0xaed05000 
 stind.i
Branch_0x0:
End_Orphan_0:
ldc.i4 55
EOM: ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
 ldc.i4 0xbe258710 
 ldc.i4 0x955a1385 
 call int32 ILGEN_0x78389777::Method_0x440f1192(int16 Arg_0x0, unsigned int8 Arg_0x2)
 ldc.i4 45
 add
 ret
}
}