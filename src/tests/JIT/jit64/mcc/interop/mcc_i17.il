// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }

.assembly 'mcc_i17' {}


.namespace MCCTest
{
  .class public MyClass
  {
    .method assembly static pinvokeimpl("native_i1s" as "#1" stdcall)
        valuetype MCCTest.VType1 Sum(float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32) cil managed preservesig {
    }

    .method private valuetype MCCTest.VType1 GetSum()
    {
      .maxstack  64
      ldc.r4     1
      ldc.r4     2
      ldc.r4     3
      ldc.r4     4
      ldc.r4     5
      ldc.r4     6
      ldc.r4     7
      ldc.r4     8
      ldc.r4     9
      ldc.r4     10
      ldc.r4     11
      ldc.r4     12
      call       valuetype MCCTest.VType1 MCCTest.MyClass::GetSum2(float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32,
                                                                    float32)
        ret
    }

    .method private static valuetype MCCTest.VType1 GetSum2(float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32)
    {
      jmp   valuetype MCCTest.VType1 MCCTest.MyClass::Sum(float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32)
    }

    .method public specialname rtspecialname instance void  .ctor()
    {
      .maxstack  1
      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    } // end of method MyClass::.ctor

    .method public static int32 Main()
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  64
      .locals init	(
        [0] class MCCTest.MyClass me,
        [1] valuetype MCCTest.VType1 res,
        [2] int32 rc
      )

      newobj     instance void MCCTest.MyClass::.ctor()
      stloc.s    me

      ldloc.s    me
      call       instance valuetype MCCTest.VType1 MCCTest.MyClass::GetSum()
      stloc.s    res

      // Check Result
      ldloc.s    res
      ldc.i4     12
      call       int32 MCCTest.Common::CheckResult(valuetype MCCTest.VType1, int32)
      stloc.s    rc

      ldloc.s    rc
      ret
    } // end of method MyClass::Main

  } // end of class MyClass

} // end of namespace MCCTest
