// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test shows an inlining of `byref LCL_VAR_ADDR - byref CNST_INT` method that returns a native int.
// However, Jit could try to optimize `-` as `+ -CNST_INT` that could lead to an incorrect `long + (-byref)`.

.assembly extern System.Console {}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly 'Runtime_44266' { }
.class public a extends [mscorlib]System.Object
{
  .field static class ctest S_1
  .method public static native int byrefsubbyref(class ctest& V_1, class ctest& V_2) aggressiveinlining
  {
    ldarg 0
    ldarg 1
    sub
    ret
  }

  .method public static int32 main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    .locals init (class ctest V_1,
             class ctest V_2,
             native int V_3)
    newobj     instance void ctest::.ctor()
    stloc.0
    newobj     instance void ctest::.ctor()
    dup
    stsfld class ctest a::S_1
    stloc.1

    ldloca V_1
    ldsflda class ctest a::S_1
    call native int a::byrefsubbyref(class ctest&, class ctest&)
    stloc V_3
    ldloc V_3
    call       void a::KeepA(native int)
    ldc.i4.s   100    
    ret
  }
  
  .method public hidebysig static void  KeepA(native int a) cil managed noinlining
  {
    .maxstack  8
    ret
  }
}

.class private auto ansi ctest
       extends [mscorlib]System.Object
{
  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .maxstack  1
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } 
}
