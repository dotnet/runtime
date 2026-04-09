// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly extern NonGenericDerived1{}
.assembly NonGenericDerived2{}

.class public auto ansi beforefieldinit NonGenericDerived2<T> extends class [NonGenericDerived1]NonGenericDerived1<!T,object> {
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret }
}

