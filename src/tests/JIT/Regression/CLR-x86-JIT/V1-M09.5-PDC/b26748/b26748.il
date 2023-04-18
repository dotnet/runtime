// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b26748' {}
.class public ILGEN_0x862954f4 {

.field static	unsigned int64 field_0x3
.field static	float32[] field_0x12
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	18

 .locals (int64[] local_0x7,int64 local_0xf,int32 ecode)
ldc.i4.1
 stloc ecode
 .try {
 
ldc.i4 255
 newarr [mscorlib]System.Int64 
 stloc local_0x7
ldc.i8 0x2b06ff023e62ea0 
stloc local_0xf

ldc.i8 0x4d3410d73e16468 
stsfld unsigned int64 ILGEN_0x862954f4::field_0x3
ldc.i4 255
 newarr [mscorlib]System.Single 
 stsfld float32[] ILGEN_0x862954f4::field_0x12

 ldloc local_0xf
 conv.ovf.i1.un
Start_Orphan_29:
 ldloc local_0x7
 ldsfld float32[] ILGEN_0x862954f4::field_0x12
 ldc.i4.2
 ldelem.r4
 conv.ovf.u1
 ldloc local_0xf
 conv.i8
 ldc.i8 0x5cc32efe5897244
 ldsfld unsigned int64 ILGEN_0x862954f4::field_0x3
 rem.un
 sub.ovf
 stelem.i8
End_Orphan_29:
Start_Orphan_2d:
 ldloca local_0xf
 pop
End_Orphan_2d:
 conv.i4
 
 pop
 leave xx
 } catch [mscorlib]System.OverflowException {
 pop
 ldc.i4.0
 stloc ecode
 leave xx
 }
 
xx:
 ldloc ecode
ldc.i4 100
add
 ret
}
}

