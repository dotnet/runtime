// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY {}
.assembly MiAggressiveOptimization {}
.module MiAggressiveOptimization.dll
// MVID: {B5A4E68E-19B7-4FA3-9BE2-2A0B7265233B}

.class public abstract auto ansi sealed beforefieldinit Program
       extends [CORE_ASSEMBLY]System.Object
{
  .method public hidebysig static void  Main() cil managed aggressiveoptimization
  {
    .entrypoint
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method Program::Main

} // end of class Program
