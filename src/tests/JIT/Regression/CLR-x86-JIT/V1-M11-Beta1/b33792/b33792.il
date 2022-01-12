// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xcdff6a8e {}
.assembly extern xunit.core {}
.class ILGEN_0xcdff6a8e {

.method static int32 Method_0x1e5a(){
.maxstack	17

 .locals (int32 local_0x2,int32 local_0x4)

ldc.i4 0x31641618 
stloc local_0x2
ldc.i4 0x3776414 
stloc local_0x4

Start_Orphan_1:
 ldloca local_0x4
 ldc.i4.4
 conv.ovf.i4
 stind.i4
End_Orphan_1:

Start_Orphan_0:
 ldc.i4.m1
 conv.ovf.i8.un
 ldloc local_0x4
 conv.ovf.i8.un
 mul
 conv.u2
 newarr [mscorlib]System.SByte
 pop
End_Orphan_0:
 ldc.i4 44
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20

call int32 ILGEN_0xcdff6a8e::Method_0x1e5a()
ldc.i4 44
sub
ldc.i4 100
add
ret
}

}
