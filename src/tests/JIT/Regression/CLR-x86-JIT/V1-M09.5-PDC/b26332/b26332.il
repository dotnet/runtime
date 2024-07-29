// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b26332' {}
.class public ILGEN_0x38fd50b5 {
.field static	unsigned int16 field_0x1
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	12

.locals (int32 ecode)

 ldc.i4.1
 stloc ecode
 .try {

ldc.i4 0x30fc30d0 
stsfld unsigned int16 ILGEN_0x38fd50b5::field_0x1

Start_Orphan_0:
 ldc.i4 1
 neg
 conv.ovf.u2
 ldc.i4 4
 conv.i
 ldsfld unsigned int16 ILGEN_0x38fd50b5::field_0x1
 ldc.i4 -1
 div.un
 ceq
 mul.ovf.un
 pop
End_Orphan_0:

 leave xx
 } catch [mscorlib]System.OverflowException {
 pop
 ldc.i4.0
 stloc ecode
 leave xx
 }
 
xx:
 ldloc ecode
ldc.i4 100
add
 ret
}
}
