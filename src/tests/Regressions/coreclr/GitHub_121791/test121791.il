// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime {}
.assembly extern xunit.core {}

.assembly test121791{}

.class public auto ansi test121791
       extends [System.Runtime]System.Object
{
  .method public hidebysig static int32
          Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .maxstack  1
    .try
    {
      ldstr      "FieldOfMissingTypeInExistingAssembly"
      call       class [System.Runtime]System.Type [System.Runtime]System.Type::GetType(string)
      pop
      leave.s    Failure
    }
    catch [System.Runtime]System.Object 
    {
      pop
      leave.s    Success
    }

  Success:
    ldc.i4     100
    ret

  Failure:
    ldc.i4     10
    ret
  }
}

.class private auto ansi beforefieldinit FieldOfMissingTypeInExistingAssembly
       extends [System.Runtime]System.Object
{
  .field private static valuetype [System.Runtime]MissingType f
}
