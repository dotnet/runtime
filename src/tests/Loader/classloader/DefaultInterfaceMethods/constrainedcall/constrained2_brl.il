// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern Microsoft.DotNet.XUnitExtensions {}
.assembly extern xunit.core {}
.assembly constrained2_brl { }

.class interface private auto ansi abstract
  IAdder
{
  .method public hidebysig newslot virtual
    instance int32 Add(int32) cil managed
  {
    ldstr "Calling DIM from ByRefLike type is invalid"
    newobj instance void [System.Runtime]System.Exception::.ctor(string)
    throw
  }
}

.class private sequential ansi sealed beforefieldinit Adder
  extends [System.Runtime]System.ValueType
  implements IAdder
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
      01 00 00 00
  )

  .field private int32 _field

  .method public hidebysig specialname rtspecialname
    instance void .ctor (int32) cil managed
  {
    ldarg.0
    ldarg.1
    stfld int32 Adder::_field
    ret
  }

  .method public hidebysig newslot virtual
    instance int32 Add(int32) cil managed
  {
    // Load field and add with argument
    ldarg.0
    dup
    ldfld int32 valuetype Adder::_field
    ldarg.1
    add

    // Update the field
    stfld int32 valuetype Adder::_field

    // Return the field value
    ldarg.0
    ldfld int32 valuetype Adder::_field
    ret
  }
}

.class private sequential ansi sealed beforefieldinit Adder_Invalid
  extends [System.Runtime]System.ValueType
  implements IAdder
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (
      01 00 00 00
  )

  .method public hidebysig specialname rtspecialname
    instance void .ctor (int32) cil managed
  {
    ret
  }

  //
  // Deferring to the DIM on IAdder
  //
}

.method public hidebysig static int32 Check<byreflike (IAdder) T>(!!0, int32)
{
  ldarga.s 0
  ldarg.1
  constrained. !!0
  callvirt instance int32 IAdder::Add(int32)
  ret
}

.class public auto ansi abstract sealed beforefieldinit constrained2_brl
    extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main()
  {
      .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
          01 00 00 00
      )
      .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.SkipOnMonoAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = (
        01 00 2c 4d 6f 6e 6f 20 64 6f 65 73 20 6e 6f 74
        20 73 75 70 70 6f 72 74 20 42 79 52 65 66 4c 69
        6b 65 20 67 65 6e 65 72 69 63 73 20 79 65 74 ff
        ff ff ff 00 00
      )
      .entrypoint

      .locals init (
        valuetype Adder,
        valuetype Adder_Invalid
      )

      // Initialize Adder instance
      ldloca.s 0
      ldc.i4 10
      call instance void Adder::.ctor(int32)

      ldstr "Validate constrained call of ByRefLike interface method passes"
      call void [System.Console]System.Console::WriteLine(string)
      ldloc.0
      ldc.i4 20
      call int32 Check<valuetype Adder>(!!0, int32)
      ldc.i4 30
      ceq
      brfalse FAIL

      // Initialize Adder_Invalid instance
      ldloca.s 1
      ldc.i4 10
      call instance void Adder_Invalid::.ctor(int32)

      .try
      {
        ldstr "Validate constrained call of ByRefLike interface DIM fails"
        call void [System.Console]System.Console::WriteLine(string)

        ldloc.1
        ldc.i4 20
        call int32 Check<valuetype Adder_Invalid>(!!0, int32)
        leave FAIL
      }
      catch [System.Runtime]System.Exception
      {
        pop
        leave ExpectedFailure
      }

  ExpectedFailure:
      ldc.i4 100
      ret

  FAIL:
      ldc.i4 101
      ret
  }
}
