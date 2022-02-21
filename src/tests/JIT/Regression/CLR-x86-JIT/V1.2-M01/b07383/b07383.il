// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b07383' {}
.assembly extern xunit.core {}
.class ILGEN_0xfe8f9a4a {
.field static	unsigned int8 field_0x8
.method static int8  Method_0xe6c0a898() {
.maxstack	19
.locals (float32[] LOCAL_0x0)
   ldc.i4 255
  newarr [mscorlib]System.Single 
 stloc LOCAL_0x0
    ldloc LOCAL_0x0
       ldsfld unsigned int8 ILGEN_0xfe8f9a4a::field_0x8
   ldelema [mscorlib]System.Single
  ldind.r4
 conv.i1
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	1
 call int8  ILGEN_0xfe8f9a4a::Method_0xe6c0a898()
pop
ldc.i4 100
 ret
}
}
