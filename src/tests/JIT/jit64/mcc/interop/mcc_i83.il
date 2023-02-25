// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }

.assembly 'mcc_i83' {}


.namespace MCCTest
{
  .class public MyClass
  {
    .method assembly static pinvokeimpl("native_i8c" as "#1" cdecl)
        vararg valuetype MCCTest.VType8 Sum(float64, int32, int64, float32, int16, float64) cil managed preservesig {
    }

    .method private valuetype MCCTest.VType8 GetSum()
    {
      .maxstack  64
      .locals init	(
        [0] valuetype MCCTest.VType8 v1,
        [1] valuetype MCCTest.VType8 v2,
        [2] valuetype MCCTest.VType8 v3,
        [3] valuetype MCCTest.VType8 v4,
        [4] valuetype MCCTest.VType8 v5,
        [5] valuetype MCCTest.VType8 v6,
        [6] valuetype MCCTest.VType8 v7,
        [7] valuetype MCCTest.VType8 v8,
        [8] valuetype MCCTest.VType8 v9,
        [9] valuetype MCCTest.VType8 v10,
        [10] valuetype MCCTest.VType8 v11,
        [11] valuetype MCCTest.VType8 v12
      )

      // Initialize v1 thru v12
      ldloca.s   v1
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v2
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v3
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v4
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v5
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v6
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v7
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v8
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v9
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v10
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v11
      call       instance void MCCTest.VType8::Init()
      ldloca.s   v12
      call       instance void MCCTest.VType8::Init()

      ldc.r8     5
      ldc.i4     1
      ldc.i8     2
      ldc.r4     -1
      ldc.i4     -3
      conv.i2
      ldc.r8     8
      ldloc.s    v1
      ldloc.s    v2
      ldloc.s    v3
      ldloc.s    v4
      ldloc.s    v5
      ldloc.s    v6
      ldloc.s    v7
      ldloc.s    v8
      ldloc.s    v9
      ldloc.s    v10
      ldloc.s    v11
      ldloc.s    v12
      call   vararg valuetype MCCTest.VType8 MCCTest.MyClass::GetSum2(float64, int32, int64, float32, int16, float64, ...,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8,
                                                                valuetype MCCTest.VType8)
        ret
    }

    .method private static vararg valuetype MCCTest.VType8 GetSum2(float64, int32, int64, float32, int16, float64)
    {
      jmp   vararg valuetype MCCTest.VType8 MCCTest.MyClass::Sum(float64, int32, int64, float32, int16, float64)
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
        [1] valuetype MCCTest.VType8 res,
        [2] int32 rc
      )

      newobj     instance void MCCTest.MyClass::.ctor()
      stloc.s    me

      ldloc.s    me
      call   instance valuetype MCCTest.VType8 MCCTest.MyClass::GetSum()
      stloc.s    res

      // Check Result
      ldloc.s    res
      ldc.i4     12
      call       int32 MCCTest.Common::CheckResult(valuetype MCCTest.VType8, int32)
      stloc.s    rc

      ldloc.s    rc
      ret
    } // end of method MyClass::Main

  } // end of class MyClass

} // end of namespace MCCTest
