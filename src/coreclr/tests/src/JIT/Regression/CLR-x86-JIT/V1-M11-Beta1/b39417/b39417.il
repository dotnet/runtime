// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xa39a4fa9 {}
.class ILGEN_0xa39a4fa9 {

.field static	unsigned int32[] field_0x4
.field static	int8[] field_0x5
.method static float32 Method_0x728b(unsigned int16 Arg_0x0, int16 Arg_0x1, float32 Arg_0x2, unsigned int8 Arg_0x3, int32 Arg_0x4) {
.maxstack	19


ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stsfld unsigned int32[] ILGEN_0xa39a4fa9::field_0x4
ldc.i4 255
 newarr [mscorlib]System.SByte 
 stsfld int8[] ILGEN_0xa39a4fa9::field_0x5

Start_Orphan_0:
 ldsfld unsigned int32[] ILGEN_0xa39a4fa9::field_0x4
 ldc.i4.m1
 ldelema [mscorlib]System.UInt32
Start_Orphan_2:
 ldsfld int8[] ILGEN_0xa39a4fa9::field_0x5
 ldc.i4.0
 conv.u1
 newarr [mscorlib]System.Int64
 ldlen
 ldc.i8 0x4e947a5a
 ldc.i8 0x415a15f9
 conv.u8
 div
ldc.r8 55.5
ldarg Arg_0x2
 ceq
 conv.ovf.u8.un
 clt
  stelem.i1
End_Orphan_2:

Start_Orphan_b:
 ldc.i4.6
 starg Arg_0x0
End_Orphan_b:

Start_Orphan_a:
 ldarg Arg_0x1
 conv.u1
 newarr [mscorlib]System.Int64
 ldc.i4.2
 ldc.i4 0xa341008
 conv.u8
 stelem.i8
End_Orphan_a:

 ldarg Arg_0x4
 stind.i4
End_Orphan_0:

 ldc.i8 0x26cc446a
 conv.r4
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i4 0x5ca50d2 
ldc.i4 0x75905e07 
ldc.r4 float32(0x2ef77d28) 
ldc.i4 0x27246637 
ldc.i4 0x2165a42 

call float32 ILGEN_0xa39a4fa9::Method_0x728b(unsigned int16 Arg_0x0, int16 Arg_0x1, float32 Arg_0x2, unsigned int8 Arg_0x3, int32 Arg_0x4)
conv.i4

pop

  leave a1
} catch [mscorlib]System.IndexOutOfRangeException {
  pop
  leave a1
}
a1:
ldc.i4 100

 ret
}

}
