// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern mscorlib { }
.assembly extern GenRetType { }

.assembly NonGenericDerived1 { }

.class public auto ansi beforefieldinit NonGenericDerived1<ARG1,ARG2> extends class [GenRetType]GenRetType<!ARG1,!ARG2> { 
  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed { ret } 
}
