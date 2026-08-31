// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern NonGenericDerived3{}
.assembly NonGenericDerived4{}

.class public auto ansi beforefieldinit NonGenericDerived4 extends [NonGenericDerived3]NonGenericDerived3 { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
