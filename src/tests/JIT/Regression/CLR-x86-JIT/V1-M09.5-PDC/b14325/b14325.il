// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14325
{
}
.class public ILGEN_622380794 {

.field static	int64 field_0x5

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

 .locals (int32 local_0x0)
ldc.i4 5
 stloc local_0x0
 ldc.i8 0
 stsfld int64 ILGEN_622380794::field_0x5

 ldc.i4.1
 ldsflda int64 ILGEN_622380794::field_0x5
 ldind.i8
 conv.u2
 ldsfld int64 ILGEN_622380794::field_0x5
 conv.u2
 ldsfld int64 ILGEN_622380794::field_0x5
 conv.u2
 ldloc local_0x0
 conv.r.un
 conv.i4
 clt
 ceq
 ldsfld int64 ILGEN_622380794::field_0x5
 conv.r4
 conv.u
 conv.i
 conv.u2
 clt
 and
 shr.un
 
 ldc.i4.1
 sub.ovf.un
ldc.i4 100
add
 ret
}
}
