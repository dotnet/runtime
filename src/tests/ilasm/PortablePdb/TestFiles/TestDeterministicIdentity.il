// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly TestDeterministicIdentity
{
}

.class public auto ansi beforefieldinit TestDeterministicIdentityType
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 F() cil managed
  {
     .line 10,10 : 9,10 'TestDeterministicIdentity.cs'
     ldc.i4.1
     ret
  }
}
