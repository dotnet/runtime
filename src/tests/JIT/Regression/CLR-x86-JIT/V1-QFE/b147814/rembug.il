// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly rembug {}

.method public static unmanagedexp int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl)
 main() cil managed 
 {
  .entrypoint
  .maxstack  3
  .locals (int32* V_0, int32 V_1)
  	  ldloca.s   V_1
  	  stloc.0
  	  ldloc.0
  	  ldloc.0
  	  ldind.i4
  	  ldc.i4     0x80000000
  	  rem
  	  stind.i4
  	  ldc.i4 100
  	  ret
} 

