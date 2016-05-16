// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b13586
{
}
.class ILGEN_6 {
 .method static int32 main() {
.entrypoint
.maxstack	20

 .locals (float64)
 ldc.r8 3.3
 stloc 0
 
 ldloca 0
 ldind.r8
 ldc.r8 1.1 
 ldloc 0
 sub
 cgt
 ldloca 0
 ldind.r8
 ldloc 0
 clt
 sub
 
 ldc.i4 99
 add
 ret
}
}
