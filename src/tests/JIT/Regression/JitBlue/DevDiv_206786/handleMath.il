// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Need to be careful about constant folding and handles
// during prejitting.

.assembly extern mscorlib {}
.assembly handleMath {}
.assembly extern xunit.core {}

.class public F
{

.method public instance void .ctor(int32 a)
{
   ldarg.0
   call       instance void [mscorlib]System.Object::.ctor()
   ldarg.0
   ldarg.1
   ldc.i4 75
   add
   stfld int32 F::A
   ret
}

.field public int32 A

.method public int32 Fix() cil
{
   ldarg.0
   ldfld int32 F::A
   ldc.i4 125
   add
   ret
}

.method public static native int Add(native int x) cil managed
{
   ldarg.0
   ldc.i4 5
   add
   ret
}

.method public static native int Sub(native int x) cil managed noinlining
{
   ldarg.0
   ldc.i4 5
   sub
   ret
}

.method public hidebysig static int32 Main(string[] args) cil managed
{
   .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
       01 00 00 00
   )
   .entrypoint
   ldc.i4 -100
   newobj instance void F::.ctor(int32)
   ldftn instance int32 F::Fix()
   call native int F::Add(native int)
   call native int F::Sub(native int)
   calli int32(class F)
   ldc.i4 100
   bne.un failure

   ldc.i4 -100
   newobj instance void F::.ctor(int32)
   ldftn instance int32 F::Fix()
   neg
   neg
   calli int32(class F)
   ldc.i4 100
   bne.un failure
   
   ldc.i4 -100
   newobj instance void F::.ctor(int32)
   ldftn instance int32 F::Fix()
   not
   not
   calli int32(class F)
   ldc.i4 100
   bne.un failure

success:
   ldc.i4 100
   ret
failure:
   ldc.i4.0
   ret
}


}
