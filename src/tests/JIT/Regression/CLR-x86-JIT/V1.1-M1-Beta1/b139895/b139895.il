// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b139895' {}
.assembly extern xunit.core {}
.class ILGEN_0x4924301 {
.method static int32 Method_0x7ec05129() {
.maxstack	13
 .locals (native int local_0x8,unsigned int8 local_0xb)

 ldc.i4 0xe785dc78 
 stloc local_0x8
 ldc.i4 0xa02726da 
 stloc local_0xb

Start_Orphan_0:
Start_Orphan_1:
 ldc.i4 0xef557475 
 brtrue Branch_0x0
 ldc.r4 float32(0x707af62e)
 br Branch_0x1
Branch_0x0:
 ldc.r8 float64(0x57867bf6568b0b02)
Branch_0x1:
 pop
End_Orphan_1:
End_Orphan_0:
Start_Orphan_6:
 ldloca local_0xb
 ldloc local_0x8
 conv.i8
 conv.i1
 ldloc local_0x8
 ldc.i4.7
 mul
 clt
 stind.i1
End_Orphan_6:
ldc.i4 44
EOM: ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
call int32 ILGEN_0x4924301::Method_0x7ec05129()
pop
ldc.i4 100
 ret
}
}
