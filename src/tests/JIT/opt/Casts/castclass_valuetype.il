// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern mscorlib { auto }
.assembly extern xunit.core {}
.assembly extern System.Console { auto }
.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }

.assembly 'castclass_valuetype' { }

.class public auto Program extends [System.Runtime]System.Object
{
  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    .maxstack 8

    .try
    {
      // castclass valuetype
      ldc.i4 1234
      box int32
      castclass int32
      unbox int32
      call instance string [System.Runtime]System.Int32::ToString()
      call void [System.Console]System.Console::WriteLine(class [System.Runtime]System.String)

      // castclass nullable
      ldc.i4 1234
      box int32
      castclass valuetype [System.Runtime]System.Nullable`1<int32>
      unbox int32
      call instance string [System.Runtime]System.Int32::ToString()
      call void [System.Console]System.Console::WriteLine(class [System.Runtime]System.String)

      leave.s SUCCESS
    }
    catch [System.Runtime]System.Object
    {
      pop
      leave.s FAIL
    }

SUCCESS:
    ldstr "SUCCESS"
    call void [System.Console]System.Console::WriteLine(class [System.Runtime]System.String)
    ldc.i4 100
    ret

FAIL:
    ldstr "FAILED"
    call void [System.Console]System.Console::WriteLine(class [System.Runtime]System.String)
    ldc.i4 0
    ret
  }
}