// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10940a { }
.method public static int32 foo() {
  .entrypoint
  .locals(int32,refany)
  ldc.i4 0 //initialize local 0 (not used in all tests)
  stloc 0

  // Generate a refany on the stack.
  ldloca 0
  mkrefany value class [mscorlib]System.Int32

  stloc 1

ldc.i4 100
  ret
}
