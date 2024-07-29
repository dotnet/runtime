// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10666
{
}
.class public value sealed Rational extends [mscorlib]System.ValueType
{ .field int32 Numerator
  .field int32 Denominator

  .method public hidebysig specialname rtspecialname instance void  .ctor() il managed
  {
    .maxstack  8
    ldarg.0
    ldc.i4.0 
    stfld     int32 Rational::Numerator
    ldarg.0
    ldc.i4.0 
    stfld     int32 Rational::Denominator
    ret
  }
  .method instance class [mscorlib]System.String ToString1()
  { .locals (class [mscorlib]System.Text.StringBuilder SB,
             class [mscorlib]System.String S,
             class [mscorlib]System.Object N,
             class [mscorlib]System.Object D)
    newobj instance void [mscorlib]System.Text.StringBuilder::.ctor()
        stloc.s SB
    ldstr "The value is: {0}/{1}"
        stloc.s S
    ldarg.0     // By-ref to self
    dup
    ldfld int32 value class Rational::Numerator
    box [mscorlib]System.Int32
        stloc.s N
    ldfld int32 value class Rational::Denominator
    box [mscorlib]System.Int32
        stloc.s D
        ldloc.s SB
        ldloc.s S
        ldloc.s N
        ldloc.s D
    call instance class [mscorlib]System.Text.StringBuilder 
      [mscorlib]System.Text.StringBuilder::AppendFormat(class [mscorlib]System.String,
        class [mscorlib]System.Object, class [mscorlib]System.Object)
    callvirt instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
    ret
  }


  .method instance class [mscorlib]System.String ToString2()
  { .maxstack 20
    newobj instance void [mscorlib]System.Text.StringBuilder::.ctor()
    ldstr "The value is: {0}/{1}"
    ldarg.0     // By-ref to self
//    dup
    ldfld int32 value class Rational::Numerator
    box [mscorlib]System.Int32
    ldarg.0
    ldfld int32 value class Rational::Denominator
    box [mscorlib]System.Int32
    call instance class [mscorlib]System.Text.StringBuilder 
      [mscorlib]System.Text.StringBuilder::AppendFormat(class [mscorlib]System.String,
        class [mscorlib]System.Object, class [mscorlib]System.Object)
    callvirt instance class [mscorlib]System.String [mscorlib]System.Object::ToString()
    ret
  }

.method public static int32 main()
{ .entrypoint
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .locals (value class Rational Half)
  ldloca.s Half
  call    instance void Rational::.ctor()

  ldloca.s Half
  dup
  ldc.i4.1
  stfld int32 value class Rational::Numerator
  ldc.i4.2
  stfld  int32 value class Rational::Denominator

  ldloca.s Half
  call instance class [mscorlib]System.String value class Rational::ToString1()
  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

  ldloca.s Half
  call instance class [mscorlib]System.String value class Rational::ToString2()
  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

  ldstr "Passed."
  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
  ldc.i4 100
  ret
}
}
