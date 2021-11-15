// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x22bcffe4 {}
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.class ILGEN_0x22bcffe4 {
.field static	unsigned int16[] field_0x0
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 ldc.i4 255
 newarr [mscorlib]System.UInt16 
 stsfld unsigned int16[] ILGEN_0x22bcffe4::field_0x0
Start_Orphan_0:
 ldsfld unsigned int16[] ILGEN_0x22bcffe4::field_0x0
 ldc.i4.5
 ldelem.u2
 conv.i8
 conv.u1
Start_Orphan_1f:
 ldsfld unsigned int16[] ILGEN_0x22bcffe4::field_0x0
 ldc.i4.5
 ldelem.u2
 pop
End_Orphan_1f:
 ldc.i4 1
 add.ovf
 pop
End_Orphan_0:
ldc.i4 100
ret
}
}
