// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x81ba964 {}
.assembly extern xunit.core {}
.class ILGEN_0x81ba964 {
.field static	unsigned int8[] field_0x0
.method static int32 Method_0x444e1c4(int8 Arg_0x0, unsigned int16 Arg_0x1, int64 Arg_0x2) {
.maxstack	15

 .locals (float64 local_0x0)

ldc.r8 float64(0x3d5ed36bd7efc07f) 
stloc local_0x0

ldc.i4 255
 newarr [mscorlib]System.Byte 
 stsfld unsigned int8[] ILGEN_0x81ba964::field_0x0

 BLOCK_1:
Start_Orphan_0:
Start_Orphan_4:
 ldsfld unsigned int8[] ILGEN_0x81ba964::field_0x0
 ldarg Arg_0x1
 ldelema [mscorlib]System.Byte
 pop
End_Orphan_4:
 ldc.i4.1
 conv.i8
 ldarg Arg_0x2
 ldarg Arg_0x2
 or
 sub.ovf.un
 conv.u8
 ldc.i4.4
 ldc.i8 0xde01cc4871553bce
 ldarg Arg_0x2
 clt
 sub.ovf
 conv.ovf.u8
 cgt
 ldarg Arg_0x0
 beq Branch_0x0

 ldsfld unsigned int8[] ILGEN_0x81ba964::field_0x0
 ldc.i4.3
 ldelema [mscorlib]System.Byte
 ldc.i4.8
 stind.i1
 br Branch_0x1

 Branch_0x0:

 Branch_0x1:

End_Orphan_0:
ldc.i4 99
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x5c6d49a5 
ldc.i4 0xda9105e3 
ldc.i8 0x97efa191d74c9da5a 

call int32 ILGEN_0x81ba964::Method_0x444e1c4(int8 Arg_0x0, unsigned int16 Arg_0x1, int64 Arg_0x2)
pop

  leave a1
} catch [mscorlib]System.IndexOutOfRangeException {
  pop
  leave a1
}
a1:
ldc.i4 100
 ret
}

}
