// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14928
{
}
.class ILGEN_622380794 {

.field static	int32 field_0x0

.method static int32 main() {
.entrypoint
.maxstack	12

 .locals (float32 local_0x2)
ldc.r4 float32(0x2d8972a7) 
stloc local_0x2

ldc.i4 0x781e64a7 
stsfld int32 ILGEN_622380794::field_0x0

 ldsfld int32 ILGEN_622380794::field_0x0
 conv.u4
 ldc.r4 float32(0x18ef2f09)
 ldloc local_0x2
 cgt.un
 shl
 stsfld int32 ILGEN_622380794::field_0x0
 ldc.i4 100
ret
}
}
