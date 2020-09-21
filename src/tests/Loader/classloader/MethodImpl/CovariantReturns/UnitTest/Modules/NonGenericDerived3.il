// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern NonGenericDerived2{}
.assembly NonGenericDerived3{}

.class public auto ansi beforefieldinit NonGenericDerived3 extends class [NonGenericDerived2]NonGenericDerived2<int32> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
