// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly reabstraction { }

// Interface with a default method
.class interface public abstract auto ansi I1
{
  .method public hidebysig newslot virtual
          instance int32 Add(int32 x) cil managed
  {
    ldc.i4.1
    ldarg.1
    add
    ret
  }
}

// Interface that reabstracts the default method
.class interface public abstract auto ansi I2
  implements I1
{
  .method public hidebysig newslot virtual abstract final
          instance int32 Add(int32 x) cil managed
  {
    .override I1::Add
  }
}

// Interface that overrides reabstracted method
.class interface public abstract auto ansi I3
  implements I2
{
  .method public hidebysig newslot virtual final
          instance int32 Add(int32 x) cil managed
  {
    .override I1::Add
    ldc.i4.2
    ldarg.1
    add
    ret
  }
}

// Interface that overrides normal default method
.class interface public abstract auto ansi I4
  implements I1
{
  .method public hidebysig newslot virtual final
          instance int32 Add(int32 x) cil managed
  {
    .override I1::Add
    ldc.i4.3
    ldarg.1
    add
    ret
  }
}

// Plain old interface
.class interface public abstract auto ansi I5
{
  .method public hidebysig newslot virtual abstract
          instance int32 Add(int32 x) cil managed
  {
  }
}

// Interface that re-abstracts an already abstract method
.class interface public abstract auto ansi I6
  implements I5
{
  .method public hidebysig newslot virtual abstract final
          instance int32 Add(int32 x) cil managed
  {
    .override I5::Add
  }
}

// Class that doesn't implement reabstracted method
.class public auto ansi C1
  extends [mscorlib]System.Object
  implements I2
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

// Class that implements reabstracted method
.class public auto ansi C2
  extends [mscorlib]System.Object
  implements I3
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

// Class with a diamond between reabstracted and implemented default method
.class public auto ansi C3
  extends [mscorlib]System.Object
  implements I2, I4
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

// Class that doesn't implement interface method that also never had an implementation
.class public auto ansi C4
  extends [mscorlib]System.Object
  implements I6
{
  .method public hidebysig specialname rtspecialname
          instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [mscorlib]System.Object::.ctor()
    ret
  }
}

.class public auto ansi abstract sealed beforefieldinit reabstraction
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .try
    {
      newobj instance void C1::.ctor()
      ldc.i4.0
      callvirt instance int32 I1::Add(int32)
      pop
      leave Fail
    }
    catch [mscorlib]System.EntryPointNotFoundException
    {
      pop
      leave PureVirtualOK
    }
  PureVirtualOK:

    .try
    {
      newobj instance void C1::.ctor()
      dup
      ldvirtftn instance int32 I1::Add(int32)
      newobj instance void class [mscorlib]System.Func`2<int32,int32>::.ctor(object,
                                                                            native int)
      pop
      leave Fail
    }
    catch [mscorlib]System.EntryPointNotFoundException
    {
      pop
      leave PureVirtualDelegateOK
    }
  PureVirtualDelegateOK:

    newobj instance void C2::.ctor()
    ldc.i4.0
    callvirt instance int32 I1::Add(int32)
    ldc.i4.2
    bne.un Fail

    .try
    {
      newobj instance void C3::.ctor()
      ldc.i4.0
      callvirt instance int32 I1::Add(int32)
      pop
      leave Fail
    }
    catch [System.Runtime]System.Runtime.AmbiguousImplementationException
    {
      pop
      leave DiamondCaseOK
    }
  DiamondCaseOK:

    .try
    {
      newobj instance void C4::.ctor()
      ldc.i4.0
      callvirt instance int32 I5::Add(int32)
      pop
      leave Fail
    }
    catch [mscorlib]System.EntryPointNotFoundException
    {
      pop
      leave NeverImplementedOK
    }
  NeverImplementedOK:

    ldc.i4 100
    ret

  Fail:
    ldc.i4.m1
    ret
  }
}
