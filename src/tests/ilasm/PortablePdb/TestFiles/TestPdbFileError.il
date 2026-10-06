// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This source does not assemble: the branch target is not defined.

.assembly extern System.Runtime
{
}

.assembly TestPdbFileError
{
}

.class public auto ansi beforefieldinit TestPdbFileErrorType
       extends [System.Runtime]System.Object
{
  .method public hidebysig static void Foo() cil managed
  {
     br UNDEFINED_LABEL
     ret
  }
}
