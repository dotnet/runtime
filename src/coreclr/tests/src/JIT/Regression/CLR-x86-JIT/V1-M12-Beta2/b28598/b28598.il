// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb651d38d {}
.class ILGEN_0xb651d38d {

.method static int32 main() {
.entrypoint
.maxstack	19

 .locals (float32 local_0x1,unsigned int8 local_0x3)
 .try {
 ldc.r4 float32(0x74983ef5) 
 stloc local_0x1
 ldc.i4 0x5c745837 
 stloc local_0x3

 ldc.i8 0x608667fe28df4c61
 ldloca local_0x1
 ldind.r4
 conv.u8
 ldloc local_0x1
 conv.ovf.i8
 ldc.i4.0
 conv.ovf.u8

Start_Orphan_3:
 ldc.i4.1
 stloc local_0x3
End_Orphan_3:

 rem
 sub
 div
 conv.i2
 pop
 leave the_end
} catch [mscorlib]System.OverflowException {
  pop
  leave the_end
}
the_end:
ldc.i4 100
ret
}
}
