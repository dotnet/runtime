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

.field static	unsigned int16 field_0x1
.field static	int16 field_0x3

.method static int32 main() {
.entrypoint
.maxstack	10

 .locals (unsigned int32[] local_0x2)

 ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x2


 ldc.i4 0x19 
 stsfld unsigned int16 ILGEN_622380794::field_0x1
 ldc.i4 0x41 
 stsfld int16 ILGEN_622380794::field_0x3


 BLOCK_2:
 ldsfld int16 ILGEN_622380794::field_0x3

 ldloc local_0x2
 ldsfld unsigned int16 ILGEN_622380794::field_0x1
 ldc.i4 7
 stelem.i4

  dup
  sub
ldc.i4 100
add
  ret
}
}
