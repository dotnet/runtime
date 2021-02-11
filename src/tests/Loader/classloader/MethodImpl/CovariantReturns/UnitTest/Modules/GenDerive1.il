// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly extern GenRetType{}
.assembly GenDerive1{}

.class public auto ansi beforefieldinit GenDerive1<V1,V2> extends class [GenRetType]GenRetType<!V1,!V2> 
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
