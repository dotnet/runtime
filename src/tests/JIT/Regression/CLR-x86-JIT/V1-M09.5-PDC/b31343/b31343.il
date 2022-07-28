// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b31343' {}
.assembly extern xunit.core {}
.class ILGEN_0x9dd65b7c {
.method static int32 Method_0x479() {
.maxstack	13
.locals (float32 local_0x0)

Start_Orphan_0:
 ldc.r4 3
 conv.ovf.i8
 conv.r4
 stloc local_0x0
End_Orphan_0:

Start_Orphan_a:
 ldc.r4 float32(0x33bf6625)
 ckfinite
 stloc local_0x0
End_Orphan_a:

 ldc.i4 55
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call int32 ILGEN_0x9dd65b7c::Method_0x479()
ldc.i4 55
sub
ldc.i4 100
add
ret
}

}
