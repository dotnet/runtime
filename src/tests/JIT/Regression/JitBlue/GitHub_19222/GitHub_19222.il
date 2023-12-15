// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Ensure that the jit doesn't rely on final methods for devirtualization
// since a final method can be overridden by an explicit .override

.assembly extern mscorlib { auto }
.assembly extern xunit.core {}

.assembly GitHub_19222 { }

.class public auto ansi Test_GitHub_19222 extends [mscorlib] System.Object {
.method public static int32 modopt([mscorlib]System.Runtime.CompilerServices.CallConvCdecl) 
        main() cil managed
{
  .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
      01 00 00 00
  )
  .entrypoint
  .maxstack  2
  .locals ([0] class C c)

  // Multiple defs of local 0 here currently inhibit jit exact type tracking
  // and so the call to A::f below should not devirtualize
  newobj     instance void C::.ctor()
  stloc.0
  newobj     instance void D::.ctor()
  stloc.0

  // Must invoke D::f
  ldloc.0
  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) A::f()

  // Must invoke C::f
  ldloc.0
  callvirt   instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) C::f()

  // Sum of the 2 calls should give 100
  add
  ret
}
}

.class public auto ansi beforefieldinit A
       extends [mscorlib]System.Object
{
  .method public hidebysig newslot virtual 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
          f() cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

}

.class public auto ansi beforefieldinit B
       extends A
{
  .method public hidebysig virtual instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
          f() cil managed
  {
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

}

.class public auto ansi beforefieldinit C
       extends B
{
  .method public hidebysig newslot virtual final 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
          f() cil managed
  {
    .override A::f
    ldc.i4 34
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

}

.class public auto ansi beforefieldinit D
       extends C
{
  .method public hidebysig newslot virtual final 
          instance int32 modopt([mscorlib]System.Runtime.CompilerServices.IsLong) 
          f() cil managed
  {
    .override A::f
    ldc.i4 66
    ret
  }

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }

}

