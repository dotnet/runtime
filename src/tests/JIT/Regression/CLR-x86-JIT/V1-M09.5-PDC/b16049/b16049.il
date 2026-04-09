// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b16049' {}
.class public ILGEN_622380794 {

.field static	int32 field_0x2
.field static	int64 field_0x3

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	12

 .locals (int32 local_0x2,int64 local_0x3)
ldc.i4 0x18142b51 
stloc local_0x2
ldc.i8 0x444b438d296951ed 
stloc local_0x3


ldc.i8 0x3d853cd75adf5719 
stsfld int64 ILGEN_622380794::field_0x3

Start_Orphan_2:
 ldc.i4.m1
 ldc.r4 float32(0x5cef4fa2)
 conv.r4
 ldloc local_0x3
 conv.r4
 cgt.un
 add
 stloc local_0x2
End_Orphan_2:
 ldloc local_0x3
 conv.r8
 neg
 conv.i8
Start_Orphan_9:
 ldloc local_0x3
 ldc.i4.8
 shr
 stloc local_0x3
End_Orphan_9:
 ldsfld int64 ILGEN_622380794::field_0x3
 ldsfld int64 ILGEN_622380794::field_0x3
 ldloc local_0x3
 add
 ldloc local_0x3
 mul
 or
 sub
 conv.u1
 
 ldc.i4 1
 sub
ret
}
}
