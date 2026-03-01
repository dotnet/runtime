// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly FloatSpecialValues { }

.class public auto ansi beforefieldinit TestFloatSpecialValues
       extends [System.Runtime]System.Object
{
  // Test NaN values - these should output with normalized "// -nan" comment
  .field public static literal float64 NaNValue = float64(0xFFF8000000000000)
  
  // Test infinity values for completeness
  .field public static literal float64 PositiveInf = float64(0x7FF0000000000000)
  .field public static literal float64 NegativeInf = float64(0xFFF0000000000000)
  
  // Test float32 NaN as well
  .field public static literal float32 NaNValue32 = float32(0xFFC00000)
  
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}
