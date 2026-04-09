// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b14769
{
}
.class public ILGEN_622380794 {

//BEGIN STATIC FIELDS
//END STATIC FIELDS
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	12

 .locals (int32 local_0x0,int64 local_0x1,float32 local_0x2,float64 local_0x3)

ldc.i4 0x74f210b1 
stloc local_0x0
ldc.i8 0x7f2c6099fa4892 
stloc local_0x1
ldc.r4 float32(0x4eb12352) 
stloc local_0x2
ldc.r8 float64(0x413d39392ac27ecd) 
stloc local_0x3

Start_Orphan_0:
Start_Orphan_1:
 ldloca local_0x1
 pop
End_Orphan_1:
 ldloc local_0x3
 conv.i
 neg
 conv.i8
 stloc local_0x1
End_Orphan_0:
Start_Orphan_2:
 ldloca local_0x3
 ldc.i4.3
 conv.i8
 conv.r.un
 stind.r8
End_Orphan_2:
Start_Orphan_5:
 ldc.i4.1
 conv.r8
 stloc local_0x3
End_Orphan_5:
 ldc.i4.1
 conv.r8
 ldloca local_0x2
 ldind.r4
 ldloc local_0x3
 conv.r4
 sub
 conv.r8
 ceq
Start_Orphan_9:
 ldc.i4.5
 conv.r4
 stloc local_0x2
End_Orphan_9:
 ldc.r8 float64(0x170d268d33c65546)
 conv.u2
 ldc.i4.4
 sub
Start_Orphan_d:
 ldc.r4 float32(0x78e51701)
 stloc local_0x2
End_Orphan_d:
 ldc.i8 0x50aa2529553f6647
 ldloc local_0x1
 xor
 conv.i2
 shr
 ldloc local_0x0
 conv.r.un
 conv.u
 neg
 sub
 sub
 ldloc local_0x1
 conv.i
 clt
 
 pop
 ldc.i4 100
ret
}
}
