// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x346ef95 {}
.class ILGEN_0x346ef95 {
.field static	float32[] field_0x1
.method static int32 Main() {
.entrypoint
.maxstack	9

 ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x346ef95::field_0x1

 ldc.i4.2
 ldc.i4.7
 brfalse Branch_0x2
 ldc.i4.0
 br Branch_0x3
 Branch_0x2:
 ldc.i4.5
 Branch_0x3:
 blt.un Branch_0x0
 ldsfld float32[] ILGEN_0x346ef95::field_0x1
 ldlen
 ldc.i4 55
 bge Branch_0x4
 br Branch_0x5
Branch_0x4:
Branch_0x5:
 br Branch_0x1
Branch_0x0:
 ldc.i4.0
 conv.ovf.u2.un
 pop
Branch_0x1:
 ldc.i4 100
 ret
}
}
