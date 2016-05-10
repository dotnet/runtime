// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10940b { }
.method public static int32 foo() {
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
