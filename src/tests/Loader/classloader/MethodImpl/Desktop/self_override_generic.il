// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tests .override (MethodImpl) on generic methods.
// Program::B<T>() explicitly overrides Program::A<T>().
// Both virtual and non-virtual calls to A<T>() should execute B<T>(),
// for both reference type (string) and value type (int32) instantiations.

.assembly extern mscorlib{}
.assembly extern xunit.core {}
.assembly self_override_generic{}

.class public Program extends [mscorlib]System.Object
{
  .method public virtual newslot instance int32 A<T>() cil managed
  {
    ldstr "In A<T>"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.1
    ret
  }

  .method public virtual newslot instance int32 B<T>() cil managed
  {
    .override method instance int32 Program::A<[1]>()
    ldstr "In B<T>"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.2
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public beforefieldinit CMain extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class Program o, bool pass)

    ldc.i4.1
    stloc.1

    newobj instance void Program::.ctor()
    stloc.0

    // Test 1: o.A<string>() virtually - should return 2 (B<T> called)
    ldstr "Test 1: calling A<string>() virtually..."
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.2
    ldloc.0
    callvirt instance int32 Program::A<string>()
    beq.s T1_PASS
    ldstr "FAIL: A<string>() virtual call did not execute B<T>"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.1
T1_PASS:

    // Test 2: o.A<int32>() virtually - should return 2 (B<T> called)
    ldstr "Test 2: calling A<int32>() virtually..."
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.2
    ldloc.0
    callvirt instance int32 Program::A<int32>()
    beq.s T2_PASS
    ldstr "FAIL: A<int32>() virtual call did not execute B<T>"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.1
T2_PASS:

    // Test 3: o.A<string>() non-virtually - should return 2 (body replaced)
    ldstr "Test 3: calling A<string>() non-virtually..."
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.2
    ldloc.0
    call instance int32 Program::A<string>()
    beq.s T3_PASS
    ldstr "FAIL: A<string>() non-virtual call did not execute B<T>"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.1
T3_PASS:

    // Test 4: o.A<int32>() non-virtually - should return 2 (body replaced)
    ldstr "Test 4: calling A<int32>() non-virtually..."
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.2
    ldloc.0
    call instance int32 Program::A<int32>()
    beq.s T4_PASS
    ldstr "FAIL: A<int32>() non-virtual call did not execute B<T>"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.1
T4_PASS:

    // Return result
    ldloc.1
    brtrue.s PASS

    ldstr "FAIL"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.s 101
    ret

PASS:
    ldstr "PASS"
    call void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.s 100
    ret
  }

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
