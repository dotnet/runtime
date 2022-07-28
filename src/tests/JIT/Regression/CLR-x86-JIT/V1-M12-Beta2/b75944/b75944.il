// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b75944 { }
.assembly extern xunit.core {}
.class ILGEN_0x32b4c023 {
.method static native int  Method_0xbc5d3052(int16 Arg_0x0, native int Arg_0x1) {
.maxstack	7
.locals (native int LOCAL_0x0)
 ldc.i4 0x6dda893d 
 stloc LOCAL_0x0
   ldc.i8 0xcf8480949a554a4b
  conv.i2
 conv.ovf.i2.un
   ldc.i4 0x4611615e
 conv.u
 beq Branch_0x2
   ldarg Arg_0x1
   ldc.i8 0xcdc124826bcee61d
  conv.ovf.u2.un
 add
 br Branch_0x3
Branch_0x2:
   ldloc LOCAL_0x0
   ldarg Arg_0x1
 rem
Branch_0x3:
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	5
.try {
 ldc.i4 0xfb159a20 
 ldc.i4 0x39ef97fe conv.i
 call native int  ILGEN_0x32b4c023::Method_0xbc5d3052(int16 Arg_0x0, native int Arg_0x1)
 conv.i4
 leave.s IL_FAIL
} catch [mscorlib]System.OverflowException {
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
