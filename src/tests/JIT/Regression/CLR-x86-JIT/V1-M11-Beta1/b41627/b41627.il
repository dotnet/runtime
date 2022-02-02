// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b41627' {}
.assembly extern xunit.core {}
.class ILGEN_0x1a8938c {

.field static	unsigned int16 field_0x2
.method static int32 Method_0x5f6dc3a1() {
.maxstack	9

 ldc.i4 0xb7dc60bb 
 stsfld unsigned int16 ILGEN_0x1a8938c::field_0x2
 ldc.i4.7
 ldsfld unsigned int16 ILGEN_0x1a8938c::field_0x2
 ceq
 brfalse Branch_0x8
 ldc.i4.8
 neg
 br Branch_0x9
Branch_0x8:
 ldc.i4.m1
 ldsfld unsigned int16 ILGEN_0x1a8938c::field_0x2
 xor
Branch_0x9:
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call int32 ILGEN_0x1a8938c::Method_0x5f6dc3a1()
dup sub
ldc.i4 100 add
 ret
}

}
