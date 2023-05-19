// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b25507' {}
.class public ILGEN_0x5b4498f4 {

.field static	unsigned int32 field_0x2

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	16

.locals (unsigned int8 local_0xa,int32 local_0xe,int64 local_0xf,float64 local_0x11)

 ldc.i4 0x1ec11a99 
 stloc local_0xa
 ldc.i4 0x18336003 
 stloc local_0xe
 ldc.i8 0x3f6673427f547107 
 stloc local_0xf
 ldc.r8 float64(0x64477b73acd10ec) 
 stloc local_0x11

 ldc.i4 0x1dfe6905 
 stsfld unsigned int32 ILGEN_0x5b4498f4::field_0x2


 ldloc local_0x11
 conv.ovf.i8
 ldloca local_0xf
 ldind.i8

Start_Orphan_f:
 ldsfld unsigned int32 ILGEN_0x5b4498f4::field_0x2
 stloc local_0xe
End_Orphan_f:

 ldloc local_0xe
 conv.u8
 and
 add
 conv.u4
 stloc local_0xa
End_Orphan_0:

ldc.i4 100
ret
}
}
