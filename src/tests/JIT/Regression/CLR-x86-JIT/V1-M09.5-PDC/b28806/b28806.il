// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x2e194c20 {}
.class ILGEN_0x2e194c20 {
.field static	int32 field_0x3
.method static int32 main() {
.entrypoint
.maxstack	7
 .try {
 ldc.i4.m1
stsfld int32 ILGEN_0x2e194c20::field_0x3

 ldsfld int32 ILGEN_0x2e194c20::field_0x3
 ldc.i4.m1
 mul.ovf.un
   pop
  leave exit
} catch [mscorlib]System.OverflowException
{
  pop
  leave exit
}
exit:
  ldc.i4.0
ldc.i4 100
add
ret
}
}
