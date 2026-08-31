// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib{}
.assembly extern GenToNonGen2 { }

.assembly GenToNonGen3{}

.class public auto ansi beforefieldinit GenToNonGen3<T1,T2> extends class [GenToNonGen2]GenToNonGen2<!T1,!T2,string> 
{
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
