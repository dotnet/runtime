// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10665 { }
.class public value sealed Rational implements [mscorlib]System.IComparable
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

  .method public virtual int32 CompareTo(class [mscorlib]System.Object o)
  // Implements IComparable::CompareTo(Object)
  {
    ldarg.0     // this as by-ref
    ldfld int32 Rational::Numerator
    ldarg.1     // o as object
    unbox value class Rational
    ldfld int32 Rational::Numerator
    beq.s TryDenom
    ldc.i4.0
    ret
TryDenom:
    ldarg.0     // this as by-ref
    ldfld int32 value class Rational::Denominator
    ldarg.1     // o as object
    unbox value class Rational
    ldfld int32 class Rational::Denominator
    ceq
    ret
  }

  .method public virtual class [mscorlib]System.String ToString()
  // Implements Object
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

  .method value class Rational Mul(value class Rational)
  { .locals (value class Rational Result)
	
	ldloca.s Result
    call    instance void Rational::.ctor()
 	ldloca.s Result
    dup
    ldarg.0     // this
    ldfld int32 value class Rational::Numerator
    ldarga 1     // arg
    ldfld int32 value class Rational::Numerator
    mul
    stfld int32 value class Rational::Numerator

    ldarg.0     // this
    ldfld int32 value class Rational::Denominator
    ldarga 1     // arg
    ldfld int32 value class Rational::Denominator
    mul
    stfld int32 value class Rational::Denominator

    ldloc.s Result
    ret
  }

.method public static int32 main()
{ .entrypoint
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .locals (value class Rational Half, value class Rational Third,
           value class Rational Temporary,
           class [mscorlib]System.Object H, class [mscorlib]System.Object T)

  ldloca.s Half
  call    instance void Rational::.ctor()
  ldloca.s Third
  call    instance void Rational::.ctor()
  ldloca.s Temporary
  call    instance void Rational::.ctor()
  
  // Initialize Half, Third, H, and T

  ldloca.s Half
  dup
  ldc.i4.1
  stfld int32 value class Rational::Numerator
  ldc.i4.2
  stfld  int32 value class Rational::Denominator

  ldloca.s Third
  dup
  ldc.i4.1
  stfld int32 value class Rational::Numerator
  ldc.i4.3 
  stfld int32 value class Rational::Denominator

  ldloc.s Half
  box value class Rational
  stloc.s H

  ldloc.s Third
  box value class Rational
  stloc.s T

  // WriteLine((Half.Mul(T)).ToString())
  // Mul is called on two value types, returning a value type
  // ToString is then called directly on that value type
  // Note that we are required to introduce a temporary variable
  //   since the call to ToString requires a by-ref (address)
  ldloca.s Half
  ldloc.s Third
  call instance value class Rational Rational::Mul(value class Rational)
  stloc.s Temporary
  ldloca.s Temporary
  call instance class [mscorlib]System.String Rational::ToString()
  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

  // WriteLine((H.Mul(T)).ToString())
  // Mul is called on a boxed instance, returning a value type
  // ToString is then called directly on that value type
  // Note that we are required to introduce a temporary variable
  //   since the call to ToString requires a by-ref (address)
  ldloc H
  unbox value class Rational
  ldloc.s Third
  call instance value class Rational value class Rational::Mul(value class Rational)
  stloc.s Temporary
  ldloca.s Temporary
  call instance class [mscorlib]System.String Rational::ToString()
  call void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)

ldc.i4 100
  ret
}
}
