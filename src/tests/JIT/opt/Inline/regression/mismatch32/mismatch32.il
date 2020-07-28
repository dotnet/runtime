// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// When the jit considers inlining B it can get itself into
// trouble because of the type mismatch. This test tries to
// ensure the jit backs out of the inline successfully.
//
// By default (when no args are passed) execution avoids
// the problematic callsite, and the app should run without
// failing.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly main {}
.module main.exe

.class public sealed F extends System.ValueType
{

.method public instance void .ctor()
{
   ret
}

.field public string A
}

.method private static int32 B(int32 X, int32 Y)
{
   ldarg.0
   ret
}

.method public hidebysig static int32 Main(string[] args) cil managed
{
   .entrypoint
   .locals init (valuetype F v_0)
   ldarg.0
   ldlen
   ldc.i4 1
   blt DONE
   newobj instance void F::.ctor()
   ldc.i4 33
   call  int32 B(int32, int32)
   pop

DONE:

   ldc.i4 100
   ret
}
