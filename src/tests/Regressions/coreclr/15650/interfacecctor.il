// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly interfacecctor { }

.class interface private abstract auto ansi IFoo
{
  .method private hidebysig specialname rtspecialname static
          void  .cctor() cil managed
  {
    .maxstack  8
    ldc.i4.s   100
    stsfld int32 s_result
    ret
  }

  .method public hidebysig newslot virtual
          instance void Frob() cil managed
  {
    .maxstack  8
    ret
  }
}

.class public auto ansi beforefieldinit Fooer
       extends [mscorlib]System.Object
       implements IFoo
{
  .method public hidebysig specialname rtspecialname
          instance void  .ctor() cil managed
  {
    .maxstack  8
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.field public static int32 s_result

.class public auto ansi abstract sealed beforefieldinit interfacecctor
    extends [mscorlib]System.Object
{
  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack  8
    newobj     instance void Fooer::.ctor()
    callvirt   instance void IFoo::Frob()
    ldsfld int32 s_result
    ret
  }
}
