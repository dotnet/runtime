// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
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

