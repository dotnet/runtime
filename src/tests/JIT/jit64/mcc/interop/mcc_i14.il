// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }

.namespace mcc_i14
{
  .class public MyClass
  {
    .method assembly static pinvokeimpl("native_i1s" as "#1" stdcall)
        valuetype MCCTest.VType1 Sum(float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32, float32) cil managed preservesig {
    }

    .method public static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.PlatformSpecificAttribute::.ctor(valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = {
            int32 (1) // TestPlatforms.Windows
        }
      .maxstack  1

    DoTest:
      call       int32 mcc_i14.MyClass::Test()
      ret
    }

    .method public static int32 Test()
    {
      .maxstack  64
      .locals init	(
        [0] valuetype MCCTest.VType1 res,
        [1] int32 rc
      )

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
      call       valuetype MCCTest.VType1 mcc_i14.MyClass::Sum(float32,
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

      stloc.s    res

      // Check Result
      ldloc.s    res
      ldc.i4     12
      call       int32 MCCTest.Common::CheckResult(valuetype MCCTest.VType1, int32)
      stloc.s    rc

      ldloc.s    rc
      ret
    } // end of method MyClass::Test

  } // end of class MyClass

} // end of namespace mcc_i14



