// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// ldlen.il
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'isinst_ldlen'
{
  
  
}
.assembly extern xunit.core {}
.namespace JitTest_isinst_ldlen_array_il
{
  .class public auto ansi beforefieldinit Test
         extends [mscorlib]System.Object
  {
    .method public hidebysig static int32
            Main() cil managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  4
      .locals (class [mscorlib]System.Array)
      ldc.i4.6
      newarr [mscorlib]System.Int32
      isinst [mscorlib]System.Array
      stloc.0
      
      ldloc.0
      isinst	int32[]
      ldlen
      conv.i4
      ldc.i4.6
      beq.s      continue_1
      newobj     instance void [mscorlib]System.Exception::.ctor()
      throw
      
   continue_1:
      ldc.i4.6
      newarr  [mscorlib]System.Single
      isinst  [mscorlib]System.Array
      stloc.0
      
      ldloc.0
      isinst float32[]
      ldlen
      conv.i4
      ldc.i4.6
      beq.s      continue_2
      newobj     instance void [mscorlib]System.Exception::.ctor()
      throw

	continue_2:  
      ldc.i4.6
      newarr     [mscorlib]System.Double
      isinst    [mscorlib]System.Array
      stloc.0 
     
      ldloc.0
      isinst float64[]
      ldlen
      conv.i4
      ldc.i4.6
      beq.s      continue_3
      newobj     instance void [mscorlib]System.Exception::.ctor()
      throw

    continue_3:
      ldc.i4 100
      ret
    }
    .method public hidebysig specialname rtspecialname 
            instance void .ctor() cil managed
    {
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ret
    }
  }
}
