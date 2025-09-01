// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#define CORE_ASSEMBLY "System.Runtime"

.assembly extern CORE_ASSEMBLY {}
.assembly MiAsUnquotedIdentifier {}
.module MiAsUnquotedIdentifier.dll

.class public auto ansi Program extends [CORE_ASSEMBLY]System.Object
{
  .method public static int32 Main() il managed
  {
    .entrypoint
    .maxstack  8

    ret
  } 

  .method public specialname rtspecialname instance void .ctor() cil managed
  {
    .maxstack  8

    ldarg.0
    call       instance void [CORE_ASSEMBLY]System.Object::.ctor()
    ret
  } 

  .method public instance void async(int32 async) cil managed async
  {
    .maxstack  8

    ret
  }

  .method public instance void runtime(int32 managed) cil managed noinlining
  {
    .maxstack  8

    ret
  } 

}