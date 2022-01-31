// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b32093' {}
.assembly extern xunit.core {}
.class ILGEN_0x126afdc7 {
.field static	int32 field_0x2
.method static void Method_0x10e7() {
.maxstack	21
 ldc.i4 0x17436d41 
 stsfld int32 ILGEN_0x126afdc7::field_0x2
 ldc.i4.8
 newarr [mscorlib]System.SByte
 ldc.i4.s -82
 ldsfld int32 ILGEN_0x126afdc7::field_0x2
 mul.ovf
 ldc.i4.5
 stelem.i1
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
call void ILGEN_0x126afdc7::Method_0x10e7()
leave s
} catch [mscorlib]System.OverflowException {
pop
leave s
}
s:
ldc.i4 100
ret
}
}
