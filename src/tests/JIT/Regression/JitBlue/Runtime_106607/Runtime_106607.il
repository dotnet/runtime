// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Console { }
.assembly extern xunit.core { }
.assembly Runtime_106607 { }

.class Runtime_106607 extends [System.Runtime]System.Object
{
  .method public static int32 Main() nooptimization
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 8
    .locals init ([0] int32 num)

    ldc.i4       42
    stloc.0
    ldloc.0
    ldc.i4       1620763441
    ldc.i4       1453536392
    add
    add
    ldloc.0
    ldloc.0 
    sub
    ldc.i4       152872638
    ldc.i4.s     31
    and
    shl
    add
    call  void [System.Console]System.Console::WriteLine(int32)

    ldc.i4 100
    ret
  }
}
