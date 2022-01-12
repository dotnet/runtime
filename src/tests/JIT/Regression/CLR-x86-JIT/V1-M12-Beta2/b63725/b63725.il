// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xb372dbdf {}
.assembly extern xunit.core {}
.class ILGEN_0xb372dbdf {
.field static	int64 field_0xc
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	20
 .locals (float32 LOCAL_0x4,int16[] LOCAL_0x5,int64 LOCAL_0x6,native int LOCAL_0x7,int64 LOCAL_0x8,unsigned int32 LOCAL_0x9,native int[] LOCAL_0xa)
ldc.r4 3.3
stloc LOCAL_0x4
ldc.i8 1 
stsfld int64 ILGEN_0xb372dbdf::field_0xc
Start_Orphan_0:
 ldc.i4 4 
 Loop_0x0: //WHAT
 ldc.i4 1
 sub
 dup
 brtrue Loop_0x0
 pop
 Loop_0x1: // End of WHAT
End_Orphan_0:
 ldc.i8 3
 ldsfld int64 ILGEN_0xb372dbdf::field_0xc
 ble.un Branch_0x2
 ldc.r8 float64(0x82b44f70f0caccf6) 
 br Branch_0x3
Branch_0x2:
 ldc.r8 float64(0xa232dc1e400c9d00)
Branch_0x3:
 ldsfld int64 ILGEN_0xb372dbdf::field_0xc
 ldc.i8 4
 bgt.un Branch_0x4
 ldc.r8 float64(0x82b44f70f0caccf6) 
 br Branch_0x5
Branch_0x4:
 ldloc LOCAL_0x4
Branch_0x5:
 bge.un Branch_0x0
 ldc.i4 3
 br Branch_0x1
Branch_0x0:
 ldc.i8 -2
 ldc.i8 1 
 bgt.un Branch_0x6
 ldc.i4 2
 br Branch_0x7
Branch_0x6:
 ldc.i4 1
Branch_0x7:
Branch_0x1:
EOM: 
ldc.i4 99
add
ret
}}
