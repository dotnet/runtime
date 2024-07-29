// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27880' {}
.class public ILGEN_0x55738f50 {
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	11

 .locals (unsigned int64[] local_0x1,int32 local_0x2)
ldc.i4 255
 newarr [mscorlib]System.UInt64 
 stloc local_0x1
ldc.i4 0x77a61bb 
stloc local_0x2

Start_Orphan_1:
 ldloc local_0x2
 not
 stloc local_0x2
End_Orphan_1:

 ldc.i4.s -37
 dup
 sub
ldc.i4 100
add
ret
}
}
