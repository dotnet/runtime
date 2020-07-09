// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Storing to arg 0 in an instance method requires special 
// handling by the jit.

.assembly extern mscorlib {}
.assembly starg0 {}
.module starg0.exe

.class public F
{

.method public instance void .ctor(int32 a)
{
   ldarg.0
   ldarg.1
   ldc.i4 100
   add
   stfld int32 F::A
   ret
}

.field public int32 A

.method public int32 Starg0() cil managed
{
   ldarg.0
   ldfld int32 F::A
   newobj instance void F::.ctor(int32)
   starg.s 0
   ldarg.0
   ldfld int32 F::A
   ret
}

.method public hidebysig static int32 Main(string[] args) cil managed
{
   .entrypoint
   ldc.i4 -100
   newobj instance void F::.ctor(int32)
   call instance int32 F::Starg0()
   ret
}


}

