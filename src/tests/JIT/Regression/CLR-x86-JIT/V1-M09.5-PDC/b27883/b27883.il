// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b27883' {}
.class public ILGEN_0x14459604 {

.field static	int8 field_0x0

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	9

 .locals (unsigned int32 local_0x0,float64 local_0x1)
.try {
ldc.i4 0x40843ee5 
stloc local_0x0
ldc.r8 float64(0x441014da65813abd) 
stloc local_0x1

ldc.i4 0x5a955a1e 
stsfld int8 ILGEN_0x14459604::field_0x0

 ldc.r4 float32(0x4acf4269)
 conv.ovf.u.un
Start_Orphan_2:
 ldloca local_0x1
 ldc.r8 float64(0x76e9144a27e63dc3)
 stind.r8
End_Orphan_2:
 ldloc local_0x0
 ldsfld int8 ILGEN_0x14459604::field_0x0
 mul.ovf.un
 cgt
  pop
  leave exit
} catch [mscorlib]System.OverflowException
{
  pop
  leave exit
}
exit:
  ldc.i4.0
ldc.i4 100
add
ret
}
}
