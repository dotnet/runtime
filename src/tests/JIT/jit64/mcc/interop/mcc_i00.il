// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern mscorlib { auto }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.namespace mcc_i00
{
  .class public MyClass
  {
    .method assembly static pinvokeimpl("native_i0c" as "#1" cdecl)
        vararg valuetype MCCTest.VType0 Sum(unsigned int64) cil managed preservesig {
    }

    .method public static int32 Main()
    {
            .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [mscorlib]System.Type, string[]) = {
            type([TestLibrary]TestLibrary.PlatformDetection)
            string[1] ('IsVarArgSupported')
        }
      .maxstack  1


    DoTest:
      call       int32 mcc_i00.MyClass::Test()
      ret
    }

    .method public static int32 Test()
    {
      .maxstack  64
      .locals init	(
        [0] valuetype MCCTest.VType0 res,
        [1] int32 rc
      )

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
      ldc.i8 1
      neg
      call   vararg valuetype MCCTest.VType0 mcc_i00.MyClass::Sum(unsigned int64, ...,
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
                                                                    unsigned int64,
                                                                    unsigned int64)

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

} // end of namespace mcc_i00
