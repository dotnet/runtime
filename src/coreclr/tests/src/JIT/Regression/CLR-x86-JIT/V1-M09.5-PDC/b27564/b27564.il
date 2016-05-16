// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly ILGEN_0xd4f4498 {}
.class ILGEN_0xd4f4498 {

.method static int32 main() {
.entrypoint
.maxstack	19

 .locals (unsigned int32 local_0x5)

 ldc.i4 0x3f887477 
 stloc local_0x5


 ldc.i4 7 
 newarr [mscorlib]System.Single
 ldc.i4 5
 ldc.i8 0x22ab10e430c242a3
 conv.r4

Start_Orphan_8:
 ldloc local_0x5
 not
 stloc local_0x5
End_Orphan_8:

 stelem.r4


 ldc.i4 100
 ret
}
}
