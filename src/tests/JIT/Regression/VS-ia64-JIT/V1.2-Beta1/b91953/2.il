// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly '2' {}
.assembly extern xunit.core {}
.class ILGEN_0xb8267005 {

.field static	float64 field_0x2
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	19

 .locals (unsigned int64 LOCAL_0x0,int64 LOCAL_0x1,native int LOCAL_0x2,int64 LOCAL_0x3,unsigned int16 LOCAL_0x4,unsigned int16[] LOCAL_0x5,int32 LOCAL_0x6,object LOCAL_0x7,int16[] LOCAL_0x8,float64[] LOCAL_0x9,float64[] LOCAL_0xa)
//BEGIN INIT LOCALS
ldc.i8 0x3f2016c6e04397c0 
stloc LOCAL_0x0
ldc.i8 0xc48d364daf72bb88 
stloc LOCAL_0x1
ldc.i4 0x81398436 
stloc LOCAL_0x2
newobj     instance void [mscorlib]System.Object::.ctor()
stloc LOCAL_0x7

//END INIT LOCALS
ldc.r8 float64(0x3658f4d211e645cf) 
stsfld float64 ILGEN_0xb8267005::field_0x2


Start_Orphan_a_0:
 ldloca LOCAL_0x7
Start_Orphan_a_1:
 ldc.i4 6 
 Loop_0x0: //WHAT
     ldloca LOCAL_0x0
    ldind.u8
   conv.i8
Start_Orphan_b_b:
      ldc.r4 float32(0xd5f6ccd2)
     ldc.r4 float32(0x792eb395) 
 beq Branch_0x8
ldc.i4 0x81398436 
 br Branch_0x9
 Branch_0x8:
ldc.i4 0x81398436 
 Branch_0x9:
ldc.i4 0x81398436 
 bne.un Branch_0x6
     ldloc LOCAL_0x3
 br Branch_0x7
 Branch_0x6:
      ldloc LOCAL_0x0
 Branch_0x7:
ldc.i4 0x81398436 
ldc.i4 0x81398436 
 bgt Branch_0xe
      ldsfld float64 ILGEN_0xb8267005::field_0x2
 br Branch_0xf
 Branch_0xe:
ldc.r8 float64(0xd44708197df14375) 

 Branch_0xf:
      ldc.r4 float32(0xe2a11a95)
      ldc.r4 float32(0xc9f53748)
 bgt Branch_0x10
      ldsfld float64 ILGEN_0xb8267005::field_0x2
 br Branch_0x11
 Branch_0x10:
      ldc.r4 float32(0x56b8f6cb)
 Branch_0x11:
pop
pop
pop
End_Orphan_b_b:
  conv.i2
 pop
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_a_1:
 newobj     instance void [mscorlib]System.Object::.ctor()
stind.ref
End_Orphan_a_0:
ldc.i4 100
 ret
}

}
