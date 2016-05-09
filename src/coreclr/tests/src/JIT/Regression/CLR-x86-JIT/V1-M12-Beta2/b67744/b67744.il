// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xc881452f {}
.class ILGEN_0xc881452f {
.field static	native int field_0x5
.method static int32 Method_0x34c75948() {
.maxstack	5
.locals (unsigned int32 LOCAL_0x1)
 ldc.i4 0x3 
 stloc LOCAL_0x1
  ldc.i4 -1
 stsfld native int ILGEN_0xc881452f::field_0x5
 ldsfld native int ILGEN_0xc881452f::field_0x5
 ldc.i4.0
 ble Branch_0x0
  ldsfld native int ILGEN_0xc881452f::field_0x5
  ldsfld native int ILGEN_0xc881452f::field_0x5
 cgt.un
 br Branch_0x1
Branch_0x0:
 ldloca LOCAL_0x1
 ldind.u4
Branch_0x1:
 ret
}
.method static int32 Main() {
.entrypoint
.maxstack	2
call int32 ILGEN_0xc881452f::Method_0x34c75948()
conv.i4
ldc.i4 97
add
 ret
}
}