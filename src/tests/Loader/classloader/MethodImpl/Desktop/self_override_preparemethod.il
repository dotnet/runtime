// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for RuntimeHelpers.PrepareMethod with .override (MethodImpl).
//
// MyClass::B explicitly overrides MyClass::A via .override directive.
// The test calls RuntimeHelpers.PrepareMethod on A's MethodHandle, which must
// also correctly handle B's body (since B replaces A's slot). On WASM with
// PortableEntryPoints, this could crash if the override is not accounted for
// during method preparation.
//
// After PrepareMethod, we verify that virtual and non-virtual calls to A
// both execute B's body (as expected with .override), and that calling B
// directly also works.

.assembly extern mscorlib{}
.assembly extern xunit.core {}
.assembly self_override_preparemethod{}

.class public MyClass extends [mscorlib]System.Object
{
  .method public virtual newslot instance int32 A()
  {
    .maxstack 8
    ldstr      "In MyClass::A"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.4
    ret
  }

  .method public virtual newslot instance int32 B()
  {
    .override MyClass::A
    .maxstack 8
    ldstr      "In MyClass::B"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.5
    ret
  }

  .method public hidebysig specialname rtspecialname
          instance void .ctor()
  {
    .maxstack 8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
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
    .maxstack 4

    .locals init (
      bool V_0,
      class MyClass V_1,
      int32 V_2,
      valuetype [mscorlib]System.RuntimeMethodHandle V_3
    )

    ldc.i4.1
    stloc.0

    // Create an instance of MyClass
    newobj     instance void MyClass::.ctor()
    stloc.1

    // Get typeof(MyClass).GetMethod("A").MethodHandle
    ldtoken    MyClass
    call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    ldstr      "A"
    callvirt   instance class [mscorlib]System.Reflection.MethodInfo [mscorlib]System.Type::GetMethod(string)
    callvirt   instance valuetype [mscorlib]System.RuntimeMethodHandle [mscorlib]System.Reflection.MethodBase::get_MethodHandle()
    stloc.3

    // Call RuntimeHelpers.PrepareMethod(h) - this is the key operation under test.
    // On WASM/PortableEntryPoints, this must not crash even though B overrides A.
    ldloc.3
    call       void [mscorlib]System.Runtime.CompilerServices.RuntimeHelpers::PrepareMethod(valuetype [mscorlib]System.RuntimeMethodHandle)

    // Test 1: Virtual call to A() should execute B's body (returns 5)
    // because B has .override on A, replacing A's vtable slot.
    ldc.i4.5
    ldloc.1
    callvirt   instance int32 MyClass::A()
    beq.s      TEST1_PASS

    ldstr      "FAIL: Virtual call to A() did not execute B's body"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.0
    br.s       TEST2

  TEST1_PASS:
    ldstr      "PASS: Virtual call to A() correctly executed B's body"
    call       void [mscorlib]System.Console::WriteLine(string)

  TEST2:
    // Test 2: Non-virtual call to A() should also execute B's body (returns 5)
    // because .override replaces the method body at the slot level.
    ldc.i4.5
    ldloc.1
    call       instance int32 MyClass::A()
    beq.s      TEST2_PASS

    ldstr      "FAIL: Non-virtual call to A() did not execute B's body"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.0
    br.s       TEST3

  TEST2_PASS:
    ldstr      "PASS: Non-virtual call to A() correctly executed B's body"
    call       void [mscorlib]System.Console::WriteLine(string)

  TEST3:
    // Test 3: Direct virtual call to B() should execute B's body (returns 5)
    ldc.i4.5
    ldloc.1
    callvirt   instance int32 MyClass::B()
    beq.s      TEST3_PASS

    ldstr      "FAIL: Call to B() did not return expected value"
    call       void [mscorlib]System.Console::WriteLine(string)
    ldc.i4.0
    stloc.0
    br.s       DONE

  TEST3_PASS:
    ldstr      "PASS: Call to B() returned expected value"
    call       void [mscorlib]System.Console::WriteLine(string)

  DONE:
    // Return 100 for PASS, 101 for FAIL
    ldloc.0
    brtrue.s   PASS

    ldc.i4.s   101
    stloc.2
    ldstr      "FAIL"
    call       void [mscorlib]System.Console::WriteLine(string)
    br.s       EXIT

  PASS:
    ldc.i4.s   100
    stloc.2
    ldstr      "PASS"
    call       void [mscorlib]System.Console::WriteLine(string)

  EXIT:
    ldloc.2
    ret
  } // end of method CMain::Main

  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    .maxstack 8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
} // end of class CMain
