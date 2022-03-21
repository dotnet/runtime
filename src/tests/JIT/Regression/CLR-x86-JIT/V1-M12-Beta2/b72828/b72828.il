// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b72828 { }
.assembly extern xunit.core {}
.class ILGEN_0x3531e7ef {
.method static int16  Method_0xac3c20e0() {
.maxstack	3
.locals (int8[] LOCAL_0x0)
 ldc.i4 255
 newarr [mscorlib]System.SByte 
 stloc LOCAL_0x0
  ldloc LOCAL_0x0
  ldc.i4 0x33b75e17
 ldelema [mscorlib]System.SByte
ldind.i1
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	5
.try {
 call int16  ILGEN_0x3531e7ef::Method_0xac3c20e0()
conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.IndexOutOfRangeException {
leave.s IL_END
}
IL_END:
ldc.i4.s 100
ret
IL_FAIL:
ldc.i4.s 101
ret


}
}
