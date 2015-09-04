// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x126afdc7 {}
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
