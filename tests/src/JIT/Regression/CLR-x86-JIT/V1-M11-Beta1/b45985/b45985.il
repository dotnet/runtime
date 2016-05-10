// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x23dc78ef {}
.class ILGEN_0x23dc78ef {
.method static int32 Method_0x24e1ee4b(float32 Arg_0x0) {
.maxstack	11
.locals (int8 local_0x0)
 ldc.i4 0x7301dbc2  
 stloc local_0x0
Start_Orphan_0:
 ldarg Arg_0x0
 dup 
 ckfinite
 ble Branch_0x0
 br Branch_0x1
Branch_0x0:
Branch_0x1:
End_Orphan_0:
ldloca local_0x0
pop
ldc.i4.0
ret
}
.method static int32 Main() {
.entrypoint
.maxstack	20
ldc.r4 float32(0xdf09543a) 
call int32 ILGEN_0x23dc78ef::Method_0x24e1ee4b(float32 Arg_0x0)
ldc.i4 100
add
 ret
}
}
