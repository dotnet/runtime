// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly extern GenToNonGen1{}
.assembly extern Dictionary{}
.assembly GenToNonGen2{}

.class public auto ansi beforefieldinit GenToNonGen2<U1,U2,U3> extends class [GenToNonGen1]GenToNonGen1<!U1, class [Dictionary]Dictionary<!U3,!U2>> 
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
