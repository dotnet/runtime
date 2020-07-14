// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x498e48f6 {}
.class ILGEN_0x498e48f6 {

.method static int32 Method_0x1eae(unsigned int64 Arg_0x0) {
.maxstack	19

Start_Orphan_b:
 ldc.i8 0x31a371cb3bf485d
 starg Arg_0x0
End_Orphan_b:
 ldc.r4 5.5
 conv.u1
 ldc.i4.3
 ldc.i4.s -56
 mul
 conv.ovf.u8
 ldc.i4.0
 conv.r4
 conv.ovf.u1
 conv.u8
 mul.ovf.un
 conv.r4
 ldc.r4 5.5
 ceq
 sub
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
.try {
ldc.i8 0x706143df1915479 

call int32 ILGEN_0x498e48f6::Method_0x1eae(unsigned int64 Arg_0x0)
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
