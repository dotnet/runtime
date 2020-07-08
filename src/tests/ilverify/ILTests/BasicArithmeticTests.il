// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly BasicArithmeticTests
{
}

.class public auto ansi beforefieldinit BasicArithmeticTestsType
       extends [System.Runtime]System.Object
{
  .method public hidebysig instance int32 
          SimpleAdd_Valid(int32 a,
              int32 b) cil managed
  {
     ldarg.1
     ldarg.2
     add
     ret
  }

  .method public hidebysig instance void  SimpleAdd_Invalid_ExpectedNumericType() cil managed
  {
    ldstr "sfdsf"
    ldc.i4.3
    add
    ret
  }
}
