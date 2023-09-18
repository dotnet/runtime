// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core {}

.assembly extern mscorlib {}
.assembly test_109968 {}


// =============== CLASS MEMBERS DECLARATION ===================

.class interface public abstract auto ansi InterfaceConstraint
{
  .method public hidebysig newslot abstract virtual 
          instance void  Meth() cil managed
  {
  } // end of method InterfaceConstraint::Meth

} // end of class InterfaceConstraint

.class public auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance void  GenericMethod<([mscorlib]System.Object) T>(!!0 t) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    ret
  } // end of method Base::GenericMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method Base::.ctor

} // end of class Base

.class public auto ansi beforefieldinit Derived
       extends Base
{
  .method public hidebysig virtual instance void 
          GenericMethod<(InterfaceConstraint) T>(!!0 t) cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    ldarg.1
    box        !!0
    callvirt   instance void InterfaceConstraint::Meth()
    ret
  } // end of method Derived::GenericMethod

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    ldarg.0
    call       instance void Base::.ctor()
    ret
  } // end of method Derived::.ctor

} // end of class Derived

.class public auto ansi beforefieldinit C1
       extends [mscorlib]System.Object
       implements InterfaceConstraint
{
  .method public hidebysig newslot virtual final 
          instance void  Meth() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    ret
  } // end of method C1::Meth

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method C1::.ctor

} // end of class C1

.class public auto ansi beforefieldinit Test_test
       extends [mscorlib]System.Object
{
  .method public hidebysig static void  Eval() cil managed
  {
    // Code size       20 (0x14)
    .maxstack  2
    .locals init (class Base V_0,
             class InterfaceConstraint V_1)
    newobj     instance void Derived::.ctor()
    stloc.0
    newobj     instance void C1::.ctor()
    stloc.1
    ldloc.0
    ldloc.1
    callvirt   instance void Base::GenericMethod<class InterfaceConstraint>(!!0)
    ret
  } // end of method Test::Eval

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    // Code size       60 (0x3c)
    .maxstack  3
    .locals init (class [mscorlib]System.TypeLoadException V_0,
             class [mscorlib]System.Exception V_1,
             int32 V_2)
    .try
    {
      call       void Test_test::Eval()
      ldstr      "Test did not throw expected System.TypeLoadException"
      call       void [System.Console]System.Console::WriteLine(string)
      ldstr      "Test failed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stloc.2
      leave.s    IL_003a

    }  // end .try
    catch [mscorlib]System.TypeLoadException 
    {
      ldstr      "Test Passed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   100
      stloc.2
      leave.s    IL_003a

    }  // end handler
    catch [mscorlib]System.Exception 
    {
      stloc.1
      ldstr      "Test caught unexpected Exception"
      call       void [System.Console]System.Console::WriteLine(string)
      ldstr      "Test failed"
      call       void [System.Console]System.Console::WriteLine(string)
      ldc.i4.s   101
      stloc.2
      leave.s    IL_003a

    }  // end handler
   
    IL_003a: ldloc.2
    ret
  } // end of method Test::Main

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  } // end of method Test::.ctor

} // end of class Test

