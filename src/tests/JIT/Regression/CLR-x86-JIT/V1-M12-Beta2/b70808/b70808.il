// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x499af240 {}
.assembly extern xunit.core {}
.class ILGEN_0x499af240 {
.field static	float64 field_0x5
.field static	unsigned int32 field_0x6
.field static	unsigned int32 field_0x7
.method static unsigned int8  Method_0x48382bfd() {
.maxstack	3
 ldc.r8 float64(0x9c7898c0f22195f1) 
 stsfld float64 ILGEN_0x499af240::field_0x5
 ldc.i4 0x92dc7976 
 stsfld unsigned int32 ILGEN_0x499af240::field_0x6
 ldc.i4 0xf6c7a299 
 stsfld unsigned int32 ILGEN_0x499af240::field_0x7

 ldsfld unsigned int32 ILGEN_0x499af240::field_0x7
 ldsfld unsigned int32 ILGEN_0x499af240::field_0x6
 mul
 conv.i1
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	5
 call unsigned int8  ILGEN_0x499af240::Method_0x48382bfd()
 conv.i4
 ldc.i4 34
 sub
 ret
}
}
