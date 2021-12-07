// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd1507ca {}
.assembly extern xunit.core {}
.class ILGEN_0xd1507ca {
.field static	native int field_0x2
.method static int32  Method_0xee92d415(float32 Arg_0x0) {
.maxstack	5
 .locals (int32 LOCAL_0x0,int64[] LOCAL_0x1,unsigned int16 LOCAL_0x2,unsigned int32[] LOCAL_0x3,unsigned int16[] LOCAL_0x4,native int[] LOCAL_0x5,native int[] LOCAL_0x6,unsigned int32 LOCAL_0x7)
 ldc.i4 0x89afa97d 
 stloc LOCAL_0x2
 ldc.i4 0x744038af 
 stsfld native int ILGEN_0xd1507ca::field_0x2
  ldc.i8 0xa8f930bd1fb1b751
  ldc.i8 0xbe4f5d73149551d7
 clt.un
  ldsfld native int ILGEN_0xd1507ca::field_0x2
 bgt.un Branch_0x2
 ldc.i4.0
 br Branch_0x3
Branch_0x2:
 ldloc LOCAL_0x2
 conv.ovf.u1.un
Branch_0x3:
 ret
}
.method static int32 Main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	3
ldc.r4 float32(0x2794165b) 
 call int32  ILGEN_0xd1507ca::Method_0xee92d415(float32 Arg_0x0)
 ldc.i4 100
 add
 ret
}
}
