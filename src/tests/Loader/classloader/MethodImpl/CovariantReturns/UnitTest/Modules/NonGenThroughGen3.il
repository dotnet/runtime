// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly extern NonGenThroughGen2{}
.assembly NonGenThroughGen3{}

.class public auto ansi beforefieldinit NonGenThroughGen3 extends class [NonGenThroughGen2]NonGenThroughGen2<object,int32,string> 
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}


