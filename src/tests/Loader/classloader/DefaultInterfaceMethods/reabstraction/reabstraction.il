// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

.assembly extern mscorlib { }
.assembly extern xunit.core {}
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }
.assembly extern TestLibrary {}
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
    // [ActiveIssue("https://github.com/dotnet/runtimelab/issues/", typeof(PlatformDetection), nameof(PlatformDetection.IsAppleMobile))]
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [System.Runtime]System.Type, string[]) = (
                01 00 30 68 74 74 70 73 3a 2f 2f 67 69 74 68 75
                62 2e 63 6f 6d 2f 64 6f 74 6e 65 74 2f 72 75 6e
                74 69 6d 65 6c 61 62 2f 69 73 73 75 65 73 2f 54
                4f 44 4f 61 54 65 73 74 4c 69 62 72 61 72 79 2e
                50 6c 61 74 66 6f 72 6d 44 65 74 65 63 74 69 6f
                6e 2c 20 54 65 73 74 4c 69 62 72 61 72 79 2c 20
                56 65 72 73 69 6f 6e 3d 30 2e 30 2e 30 2e 30 2c
                20 43 75 6c 74 75 72 65 3d 6e 65 75 74 72 61 6c
                2c 20 50 75 62 6c 69 63 4b 65 79 54 6f 6b 65 6e
                3d 6e 75 6c 6c 01 00 00 00 0d 49 73 41 70 70 6c
                65 4d 6f 62 69 6c 65 00 00
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
