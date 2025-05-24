// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern legacy library mscorlib {}
.assembly 'unquotedidentifier'
{
  
  
}
.namespace App
{
  .class public auto ansi Foo extends [mscorlib]System.Object
  {
    .method public static int32  Main() il managed
    {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .entrypoint
      .maxstack  8

      ldloc.0
      ret
    } 

    .method public specialname rtspecialname instance void .ctor() cil managed
    {
      .maxstack  8

      ldarg.0
      call       instance void [mscorlib]System.Object::.ctor()
      ret
    } 

    .method public instance void async(int32 async) cil managed aggressiveoptimization
    {
      .maxstack  8

      ret
    }

    .method public instance void runtime(int32 managed) cil managed noinlining
    {
      .maxstack  8

      ret
    } 

    .method public instance void cil(int32 unmanaged) cil managed synchronized
    {
      .maxstack  8

      ret
    } 

  } 

} 
