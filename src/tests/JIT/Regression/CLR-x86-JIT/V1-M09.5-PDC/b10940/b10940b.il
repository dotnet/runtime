// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10940b { }
.class public auto ansi Test_b10940b extends [mscorlib] System.Object {
.method public static int32 foo() {
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .locals init (int32,refany)
  ldc.i4 0 //initialize local 0 (not used in all tests)
  stloc 0

  // Generate a refany on the stack.
  ldloca 0
  mkrefany value class [mscorlib]System.Int32

  stloc 1
ldc.i4 100
  ret
}
}
