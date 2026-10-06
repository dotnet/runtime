// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TestPdbFile1
{
}

.class public auto ansi beforefieldinit TestPdbFileType
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Foo() cil managed
  {
     ldc.i4.1
     ret
  }
}
