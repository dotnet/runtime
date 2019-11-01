// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly a1 {}
.class ILGEN_622380794 {

.field static	unsigned int16 field_0x5
.field static	int8 field_0x6

.method static int32 main() {
.entrypoint
.maxstack	22

 .locals (unsigned int8 local_0x4,unsigned int8 local_0x18)

 ldc.i4 0x43604ea5 
 stloc local_0x4
 ldc.i4 0xff260f6 
 stloc local_0x18

 ldc.i4 0x2dda159b 
 stsfld unsigned int16 ILGEN_622380794::field_0x5
 ldc.i4 0x7d3d6bf9 
 stsfld int8 ILGEN_622380794::field_0x6

 ldc.i4.2
 conv.u8
 ldsfld int8 ILGEN_622380794::field_0x6
 conv.i8
 not
 ldloc local_0x18
 conv.r.un
 ldc.r8 float64(0x9f453122d365955)
 conv.r8
 cgt
 conv.i8
 xor
 and

 ldloca local_0x4
 ldsflda unsigned int16 ILGEN_622380794::field_0x5
 ldind.u2
 stind.i1

 conv.i4
 
 dup
 sub
ldc.i4 100
add
 ret
}
}
