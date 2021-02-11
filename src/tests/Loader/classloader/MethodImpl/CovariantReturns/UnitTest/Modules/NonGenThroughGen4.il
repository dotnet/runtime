// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern NonGenThroughGen3{}
.assembly NonGenThroughGen4{}

.class public auto ansi beforefieldinit NonGenThroughGen4 extends [NonGenThroughGen3]NonGenThroughGen3 
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

