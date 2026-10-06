// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This source has an error that the error-tolerant option (-ERR) assembles past: the exported type names an
// assembly reference that is not declared.

.assembly extern System.Runtime
{
}

.assembly TestPdbFileRecoverableError
{
}

.class extern public MissingType
{
  .assembly extern MissingAssembly
}

.class public auto ansi beforefieldinit TestPdbFileRecoverableErrorType
       extends [System.Runtime]System.Object
{
  .method public hidebysig static void Foo() cil managed
  {
     ret
  }
}
