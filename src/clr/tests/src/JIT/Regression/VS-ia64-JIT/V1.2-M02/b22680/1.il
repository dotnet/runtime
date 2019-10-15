// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0x5f95bcd2 {}
.class ILGEN_0x5f95bcd2 {
.method static int32 Main() {
.entrypoint
.maxstack	21
.zeroinit
 .locals (unsigned int64 LOCAL_0x3,native int temp)
    ldc.i8 0xb6c20baab1d28c3 
  stloc LOCAL_0x3
Start_Orphan_a_1d:
       ldloc LOCAL_0x3
      conv.i1
     conv.u4
      ldc.r4 55.6
	ldc.i4 55
      conv.r4
     ceq
    cgt
   conv.i
 stloc temp
End_Orphan_a_1d:
ldc.i4 100
 ret
}
}
