// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xf1d71bcb {}
.assembly extern xunit.core {}
.class ILGEN_0xf1d71bcb {

.method static int32 Method_0x766c(float64 Arg_0x0, int64 Arg_0x1, int64 Arg_0x2, unsigned int16 Arg_0x3, int16 Arg_0x4) {
.maxstack	17

Start_Orphan_0:
Start_Orphan_6:
 ldarg Arg_0x0
 conv.u
 starg Arg_0x3
End_Orphan_6:
 ldarg Arg_0x4
 conv.ovf.u8
 ldc.i4.s -59
 conv.ovf.u8
 mul
 conv.i
 conv.u
 conv.ovf.u8
 conv.i8
 starg Arg_0x1
End_Orphan_0:
Start_Orphan_e:
 ldarg Arg_0x0
 conv.ovf.u
 conv.r4
 neg
 neg
 pop
End_Orphan_e:
 ldc.i4.7
 conv.u1
 conv.ovf.u2
Start_Orphan_1a:
 ldc.i8 0x1304bb05d5b2b51
 starg Arg_0x1
End_Orphan_1a:
 ldc.i4.0
 conv.i2
 ldc.i4.5
 ldc.i4.1
 div.un
 conv.u8
 conv.ovf.u
 or
 ldc.i4.2
 conv.i4
 conv.r.un
 conv.ovf.i
 ldc.i4.3
 mul.ovf
 div.un
 mul.ovf.un
 conv.u1
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.r8 float64(0x4f213c65738d159f) 
ldc.i8 0x31146df563194f8e 
ldc.i8 0x23dd19bc3fbe6011 
ldc.i4 0x56b1739b 
ldc.i4 0x6c80206a 

call int32 ILGEN_0xf1d71bcb::Method_0x766c(float64 Arg_0x0, int64 Arg_0x1, int64 Arg_0x2, unsigned int16 Arg_0x3, int16 Arg_0x4)
 pop
  leave stop
 } catch [mscorlib]System.OverflowException {
 pop
 leave stop
 }
 stop:
ldc.i4 100
ret
}

}
