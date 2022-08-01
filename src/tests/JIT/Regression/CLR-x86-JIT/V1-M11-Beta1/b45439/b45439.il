// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b45439' {}
.assembly extern xunit.core {}
.class ILGEN_0x1c02a9a5 {
.field static	unsigned int64[] field_0x0
.field static	unsigned int8[] field_0x1
.field static	unsigned int32 field_0x2
.field static	unsigned int32[] field_0x3
.field static	float32 field_0x4
.field static	int16 field_0x5

.method static int32 Method_0xdcb0d2ac(int16 Arg_0x0, unsigned int8 Arg_0x1, int32 Arg_0x2, float64 Arg_0x3, unsigned int64 Arg_0x4, int16 Arg_0x5) {
.maxstack	9

 .locals (unsigned int8 local_0x0,unsigned int16 local_0x1,int16[] local_0x2,unsigned int32 local_0x3,unsigned int32[] local_0x4)


ldc.i4 0xf324f5b3 
stloc local_0x0
ldc.i4 0xc30276a8 
stloc local_0x1
ldc.i4 255
 newarr [mscorlib]System.Int16 
 stloc local_0x2
ldc.i4 0x788e8047 
stloc local_0x3
ldc.i4 255
 newarr [mscorlib]System.UInt32 
 stloc local_0x4

 ldc.i4.7
 ldarg Arg_0x2
 bgt Branch_0x4

 ldc.i4.4
 br Branch_0x5

 Branch_0x4:

 ldarg Arg_0x1
 Branch_0x5:

 ldc.i4.2

 blt.un Branch_0x0

 ldc.i4.5
 br Branch_0x1

 Branch_0x0:

 ldarg Arg_0x1
 Branch_0x1:

Start_Orphan_6:
 ldc.i4.7
 conv.u1
 newarr [mscorlib]System.UInt64
ldc.i4 0x12ab2df8 
 conv.ovf.i2
 ldarg Arg_0x4
 conv.ovf.u8
 stelem.i8
End_Orphan_6:
Start_Orphan_9:
 ldarg Arg_0x0
stsfld int16 ILGEN_0x1c02a9a5::field_0x5
End_Orphan_9:
 ldc.i4.3
 shr
ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
.try {
ldc.i4 0x3f924bf1 
ldc.i4 0xe6a1610a 
ldc.i4 0xaa9ef4c1 
ldc.r8 float64(0x5b101fdea0204281) 
ldc.i8 0x13a5c8e44b3007c1 
ldc.i4 0xacde1a68 

call int32 ILGEN_0x1c02a9a5::Method_0xdcb0d2ac(int16 Arg_0x0, unsigned int8 Arg_0x1, int32 Arg_0x2, float64 Arg_0x3, unsigned int64 Arg_0x4, int16 Arg_0x5)

  pop
  leave out_of_here
} catch [mscorlib]System.OverflowException { 
  pop
  leave out_of_here
}
out_of_here: ldc.i4 100 ret
}

}
