// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime.Extensions { auto }
.assembly extern xunit.core {}
.assembly extern mscorlib { auto }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary { .ver 0:0:0:0 }

.module 'mcc_interop.dll'
.assembly 'mcc_interop' {}

.namespace mcc_interop
{
  .class public Program
  {
    .method public static int32 Main()
    {
      .entrypoint
      .maxstack  2
      .locals init (
        [0] int32 result
      )

      ldstr "mcc_i00"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i00.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i01"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i01.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i02"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i02.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i03"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i03.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i04"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i04.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i05"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i05.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i06"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i06.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i07"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i07.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i10"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i10.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i11"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i11.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i12"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i12.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i13"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i13.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i14"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i14.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i15"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i15.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i16"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i16.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i17"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i17.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i30"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i30.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i31"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i31.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i32"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i32.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i33"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i33.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i34"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i34.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i35"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i35.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i36"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i36.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i37"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i37.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i50"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i50.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i51"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i51.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i52"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i52.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i53"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i53.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i54"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i54.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i55"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i55.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i56"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i56.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i57"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i57.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i60"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i60.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i61"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i61.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i62"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i62.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i63"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i63.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i64"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i64.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i65"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i65.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i66"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i66.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i67"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i67.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i70"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i70.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i71"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i71.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i72"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i72.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i73"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i73.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i74"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i74.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i75"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i75.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i76"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i76.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i77"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i77.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i80"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i80.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i81"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i81.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i82"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i82.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i83"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i83.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i84"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i84.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i85"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i85.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i86"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i86.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldstr "mcc_i87"
      call       void [mscorlib]System.Console::WriteLine(string)
      call       int32 mcc_i87.MyClass::Main()
      stloc.0
      ldloc.0
      ldc.i4     100
      bne.un     Fail

      ldc.i4     100
      ret

    Fail:
      ldc.i4     1
      ret
    }
  }
} // end of namespace mcc_interop

