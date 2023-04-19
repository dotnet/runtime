// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }

.assembly 'mcc_i77' {}


.namespace MCCTest
{
  .class public MyClass
  {
    .method assembly static pinvokeimpl("native_i7s" as "#1" stdcall)
        valuetype MCCTest.VType7 Sum(unsigned int64, valuetype MCCTest.VType7, float64,	valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7,
                                    int32, valuetype MCCTest.VType7, unsigned int16, valuetype MCCTest.VType7, unsigned int32, valuetype MCCTest.VType7,
                                    float32, valuetype MCCTest.VType7, int64, valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7,
                                    float64, valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7, int16, valuetype MCCTest.VType7) cil managed preservesig {
    }

    .method private valuetype MCCTest.VType7 GetSum()
    {
      .maxstack  64
      .locals init	(
        [0] valuetype MCCTest.VType7 v1,
        [1] valuetype MCCTest.VType7 v2,
        [2] valuetype MCCTest.VType7 v3,
        [3] valuetype MCCTest.VType7 v4,
        [4] valuetype MCCTest.VType7 v5,
        [5] valuetype MCCTest.VType7 v6,
        [6] valuetype MCCTest.VType7 v7,
        [7] valuetype MCCTest.VType7 v8,
        [8] valuetype MCCTest.VType7 v9,
        [9] valuetype MCCTest.VType7 v10,
        [10] valuetype MCCTest.VType7 v11,
        [11] valuetype MCCTest.VType7 v12
      )

      // Initialize v1 thru v12
      ldloca.s   v1
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v2
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v3
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v4
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v5
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v6
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v7
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v8
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v9
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v10
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v11
      call       instance void MCCTest.VType7::Init()
      ldloca.s   v12
      call       instance void MCCTest.VType7::Init()

      ldc.i8     1
      ldloc.s    v1
      ldc.r8     2
      ldloc.s    v2
      ldc.r4     3
      ldloc.s    v3
      ldc.i4     4
      ldloc.s    v4
      ldc.i8     5
      conv.u2
      ldloc.s    v5
      ldc.i4     6
      ldloc.s    v6
      ldc.r4     7
      ldloc.s    v7
      ldc.i8     8
      ldloc.s    v8
      ldc.r4     9
      ldloc.s    v9
      ldc.r8     10
      ldloc.s    v10
      ldc.r4     11
      ldloc.s    v11
      ldc.i8     12
      conv.i2
      ldloc.s    v12
      call       valuetype MCCTest.VType7 MCCTest.MyClass::GetSum2(	unsigned int64,	valuetype MCCTest.VType7,
                                                                float64,		valuetype MCCTest.VType7,
                                                                float32,		valuetype MCCTest.VType7,
                                                                int32,			valuetype MCCTest.VType7,
                                                                unsigned int16,	valuetype MCCTest.VType7,
                                                                unsigned int32,	valuetype MCCTest.VType7,
                                                                float32,		valuetype MCCTest.VType7,
                                                                int64,			valuetype MCCTest.VType7,
                                                                float32,		valuetype MCCTest.VType7,
                                                                float64,		valuetype MCCTest.VType7,
                                                                float32,		valuetype MCCTest.VType7,
                                                                int16,			valuetype MCCTest.VType7)
        ret
    }

    .method private static valuetype MCCTest.VType7 GetSum2(unsigned int64, valuetype MCCTest.VType7, float64,	valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7,
                                    int32, valuetype MCCTest.VType7, unsigned int16, valuetype MCCTest.VType7, unsigned int32, valuetype MCCTest.VType7,
                                    float32, valuetype MCCTest.VType7, int64, valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7,
                                    float64, valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7, int16, valuetype MCCTest.VType7)
    {
      jmp   valuetype MCCTest.VType7 MCCTest.MyClass::Sum(unsigned int64, valuetype MCCTest.VType7, float64,	valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7,
                                    int32, valuetype MCCTest.VType7, unsigned int16, valuetype MCCTest.VType7, unsigned int32, valuetype MCCTest.VType7,
                                    float32, valuetype MCCTest.VType7, int64, valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7,
                                    float64, valuetype MCCTest.VType7, float32, valuetype MCCTest.VType7, int16, valuetype MCCTest.VType7)
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
        [1] valuetype MCCTest.VType7 res,
        [2] int32 rc
      )

      newobj     instance void MCCTest.MyClass::.ctor()
      stloc.s    me

      ldloc.s    me
      call   instance valuetype MCCTest.VType7 MCCTest.MyClass::GetSum()
      stloc.s    res

      // Check Result
      ldloc.s    res
      ldc.i4     12
      call       int32 MCCTest.Common::CheckResult(valuetype MCCTest.VType7, int32)
      stloc.s    rc

      ldloc.s    rc
      ret
    } // end of method MyClass::Main

  } // end of class MyClass

} // end of namespace MCCTest
