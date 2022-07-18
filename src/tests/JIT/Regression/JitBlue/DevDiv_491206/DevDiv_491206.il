// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib {}
.assembly 'DevDiv_491206' {}
.assembly extern xunit.core {}

// This test originally triggered an assert Extra_flags_on_tree, because fgMorphCast did not reset an assignment flag, when
// its children was optimized in the assertion propagation.

.class ILGEN_CLASS
{
    .method public hidebysig static int64  DoubleToInt64(float64 val) cil managed
    {
      // Code size       108 (0x6c)
      .maxstack  2
      .locals init (float64 V_0)
      IL_0000:  ldarg.0
      IL_0001:  call       bool [mscorlib]System.Double::IsNaN(float64)
      IL_0006:  brfalse.s  IL_000b
      IL_0008:  ldc.i4.0
      IL_0009:  conv.i8
      IL_000a:  ret
      IL_000b:  ldc.r8     -9.2233720368547758e+018
      IL_0014:  ldarg.0
      IL_0015:  bgt.un.s   IL_0026
      IL_0017:  ldarg.0
      IL_0018:  ldc.r8     9.2233720368547758e+018
      IL_0021:  bgt.un.s   IL_0026
      IL_0023:  ldarg.0
      IL_0024:  conv.i8
      IL_0025:  ret
      IL_0026:  ldarg.0
      IL_0027:  call       bool [mscorlib]System.Double::IsInfinity(float64)
      IL_002c:  brfalse.s  IL_0031
      IL_002e:  ldc.i4.0
      IL_002f:  conv.i8
      IL_0030:  ret
      IL_0031:  ldarg.0
      IL_0032:  call       int32 [mscorlib]System.Math::Sign(float64)
      IL_0037:  conv.r8
      IL_0038:  ldarg.0
      IL_0039:  call       float64 [mscorlib]System.Math::Abs(float64)
      IL_003e:  call       float64 [mscorlib]System.Math::Floor(float64)
      IL_0043:  mul
      IL_0044:  ldc.r8     1.8446744073709552e+019
      IL_004d:  call       float64 [mscorlib]System.Math::IEEERemainder(float64,
                                                                        float64)
      IL_0052:  stloc.0
      IL_0053:  ldloc.0
      IL_0054:  ldc.r8     9.2233720368547758e+018
      IL_005d:  bne.un.s   IL_0069
      IL_005f:  ldc.i8     0x8000000000000000
      IL_0068:  ret
      IL_0069:  ldloc.0
      IL_006a:  conv.i8
      IL_006b:  ret
    } // end of method DoubleToInt64

    .method public static int32 Main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
          ldc.r8     9.2233720368547758e+018
          call       int64 ILGEN_CLASS::DoubleToInt64(float64)
          pop
          ldc.i4 100
          ret
    }
}
