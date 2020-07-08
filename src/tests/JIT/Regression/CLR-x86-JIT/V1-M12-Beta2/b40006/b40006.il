// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x56afce22 {}
.class ILGEN_0x56afce22 {


.method static float32 Method_0x83a6180a(int64 Arg_0x1) {
.maxstack	9

 .locals (unsigned int32 local_0x3)

 ldarg Arg_0x1
 //conv.u8
 conv.ovf.u8
 conv.r4
Start_Orphan_6:
 ldloca local_0x3
 ldarg Arg_0x1
 conv.i4
 stind.i4
End_Orphan_6:
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.i8 0x96a180a0 
call float32 ILGEN_0x56afce22::Method_0x83a6180a(int64 Arg_0x1)
conv.i4

// workaround for P4
// F->I, overflow to undefined
ldc.r4 2.527167E+09
conv.i4
sub
pop

ldc.i4 100
 ret
}
}
