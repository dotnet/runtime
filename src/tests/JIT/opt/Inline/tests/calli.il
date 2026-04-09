// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test that inliner can now inline g1 and g2

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}

.assembly legacy library 'calli' {}
.class public auto ansi beforefieldinit calli_test
       extends [mscorlib]System.Object
{
  .field private static int32 a
  .field private static int32 b
  .method public static int32 f1()
  {
    ldsfld     int32 calli_test::a
    ret
  }
  .method public static int32 f2()
  {
    ldsfld     int32 calli_test::b
    ret
  }
  .method public static int32 g1()
  {
    ldftn int32 calli_test::f1()
    calli int32 ()
    ret
  }
  .method public static int32 g2()
  {
    ldftn int32 calli_test::f2()
    calli int32 ()
    ret
  }
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  2
    call int32 calli_test::g1()
    call int32 calli_test::g2()
    beq.s      GOOD
    ldc.i4 0
    br.s       DONE
    GOOD: 
    ldc.i4 100
    DONE:
    ret
  }
}
