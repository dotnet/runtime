// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xf0dc526d {}
.assembly extern xunit.core {}
.class ILGEN_0xf0dc526d {
.method static int32 Method_0x1672(int64 Arg_0x5) {
.maxstack	13

Start_Orphan_0:
 ldarg Arg_0x5
 conv.ovf.u2.un
 pop
End_Orphan_0:

Start_Orphan_6:
 ldarg Arg_0x5
 ldc.i8 0x3ec336203bad33bf
 add
 conv.i2
 pop
End_Orphan_6:
 ldc.i4 55
 ret
}

.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i8 0x5fa5595572023792 

call int32 ILGEN_0xf0dc526d::Method_0x1672(int64 Arg_0x5)
 pop
 leave the_end
 } catch [mscorlib]System.OverflowException {
 pop
 leave the_end
 }
 the_end:
 ldc.i4 100
ret
}

}

