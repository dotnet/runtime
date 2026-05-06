// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly extern NonGenThroughGen1{}
.assembly extern Dictionary{}
.assembly NonGenThroughGen2{}

.class public auto ansi beforefieldinit NonGenThroughGen2<U1,U2,U3> extends class [NonGenThroughGen1]NonGenThroughGen1<!U1, class [Dictionary]Dictionary<!U3,!U2>> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
