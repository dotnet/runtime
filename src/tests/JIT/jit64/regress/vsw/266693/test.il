// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// These are just sanity checks for the CorInfoInlineRestrictions to make sure that nothing in the jit breaks
// when we are prevented from inlining something.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'test_266693' {}

.class public auto ansi beforefieldinit Test_test
       extends [mscorlib]System.Object
{
  .field static int32 i

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig static string myString() cil managed
  {
    ldstr "foo"
    ret
  }

  .method public hidebysig static void myHelperCall() cil managed
  {
    call void Test_test::myCall()
    ret
  }

  .method public hidebysig static void myCall() cil managed
  {
    ldc.i4.0
    ldc.i4.1
    add
    pop
    ldsfld int32 Test_test::i
    ldc.i4.0
    beq End
    ldsfld int32 Test_test::i
    ldc.i4.1
    sub
    stsfld int32 Test_test::i
    call void Test_test::myHelperCall()
    End:  ret
  }

  .method public hidebysig instance int32 func() cil managed
  {
    ldc.i4.3
    ret
  }

  .method public hidebysig instance void this() cil managed
  {
    newobj instance void Test_test::.ctor()
    call instance int32 Test_test::func()
    pop
    ret
  }

  .method public hidebysig static int32  Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .locals init (class Test_test V_0)
    ldc.i4 10
    stsfld int32 Test_test::i
    call void Test_test::myCall()
    newobj instance void Test_test::.ctor()
    stloc V_0
    ldloc V_0
    call instance int32 Test_test::func()
    pop
    call string Test_test::myString()
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4 100
    ret
  }
}
