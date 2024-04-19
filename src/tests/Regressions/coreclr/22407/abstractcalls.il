// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

.assembly extern mscorlib { }
.assembly extern xunit.core {}

.assembly abstractcalls { }

.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot virtual abstract
          instance int32 Add(int32 x) cil managed
  {
  }
}

.class public abstract auto ansi C1
  extends [mscorlib]System.Object
  implements I1
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }

  .method public hidebysig newslot virtual abstract
          instance int32 Remove(int32 x) cil managed
  {
  }
}

.class public auto ansi C2
  extends [mscorlib]System.Object
  implements I1
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void C1::.ctor()
    ret
  }

  .method public hidebysig virtual
          instance int32 Remove(int32 x) cil managed
  {
    ldc.i4.0
    ret
  }

  .method public hidebysig newslot virtual
          instance int32 Add(int32 x) cil managed
  {
    ldc.i4.5
    ret
  }
}

.method public hidebysig static void CallClass() cil managed
{
  newobj instance void C2::.ctor()
  ldc.i4.0
  call instance int32 C1::Remove(int32)
  pop
  ret
}

.method public hidebysig static void CallInterface() cil managed
{
  newobj instance void C2::.ctor()
  ldc.i4.0
  call instance int32 I1::Add(int32)
  pop
  ret
}

.class public auto ansi abstract sealed beforefieldinit abstractcalls
    extends [mscorlib]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .try
    {
      call void CallClass()
      leave Fail
    }
    catch [mscorlib]System.BadImageFormatException
    {
      pop
      leave AbstractClassOK
    }

  AbstractClassOK:

    .try
    {
      call void CallInterface()
      leave Fail
    }
    catch [mscorlib]System.BadImageFormatException
    {
      pop
      leave AbstractInterfaceOK
    }

  AbstractInterfaceOK:

    ldc.i4 100
    ret

  Fail:
    ldc.i4.m1
    ret
  }
}
