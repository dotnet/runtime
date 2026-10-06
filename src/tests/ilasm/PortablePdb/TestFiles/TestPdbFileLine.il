// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TestPdbFileLine
{
}

.class public auto ansi beforefieldinit TestPdbFileLineType
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Foo() cil managed
  {
     .line 10,10 : 9,10 'TestPdbFileLine.cs'
     ldc.i4.1
     ret
  }
}
