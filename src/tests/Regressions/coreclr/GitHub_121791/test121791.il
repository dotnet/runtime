// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
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
      call       object test121791::M()
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

  .method private hidebysig static object 
          M() cil managed noinlining
  {
    .maxstack  8
    IL_0000:  ldsfld     valuetype  [System.Runtime]MissingType FieldOfMissingTypeInExistingAssembly::f
    IL_0005:  box        [System.Runtime]MissingType
    IL_000a:  ret
  }
}

.class private auto ansi beforefieldinit FieldOfMissingTypeInExistingAssembly
       extends [System.Runtime]System.Object
{
  .field public static valuetype [System.Runtime]MissingType f
}
