// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }

.namespace mcc_i06
{
  .class public MyClass
  {
    .method assembly static pinvokeimpl("native_i0s" as "#1" stdcall)
        valuetype MCCTest.VType0 Sum(unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64, unsigned int64) cil managed preservesig {
    }

    .method private valuetype MCCTest.VType0 GetSum()
    {
      .maxstack  64
      ldc.i8 1
      ldc.i8 2
      ldc.i8 3
      ldc.i8 4
      ldc.i8 5
      ldc.i8 6
      ldc.i8 7
      ldc.i8 8
      ldc.i8 9
      ldc.i8 10
      ldc.i8 11
      ldc.i8 12
      tail.
      call        valuetype MCCTest.VType0 mcc_i06.MyClass::Sum(unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64,
                                                                unsigned int64)
        ret
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
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.PlatformSpecificAttribute::.ctor(valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = {
            int32 (1) // TestPlatforms.Windows
        }
      .maxstack  1

    DoTest:
      call       int32 mcc_i06.MyClass::Test()
      ret
    }

    .method public static int32 Test()
    {
      .maxstack  64
      .locals init	(
        [0] class mcc_i06.MyClass me,
        [1] valuetype MCCTest.VType0 res,
        [2] int32 rc
      )

      newobj     instance void mcc_i06.MyClass::.ctor()
      stloc.s    me

      ldloc.s    me
      call   instance valuetype MCCTest.VType0 mcc_i06.MyClass::GetSum()
      stloc.s    res

      // Check Result
      ldloc.s    res
      ldc.i4     12
      call       int32 MCCTest.Common::CheckResult(valuetype MCCTest.VType0, int32)
      stloc.s    rc

      ldloc.s    rc
      ret
    } // end of method MyClass::Test

  } // end of class MyClass

} // end of namespace mcc_i06



