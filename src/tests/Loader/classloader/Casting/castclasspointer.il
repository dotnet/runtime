// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly castclasspointer { }
.assembly extern xunit.core {}
.assembly extern mscorlib { }

.class public value MyGen`1<T>
{
  .field !T _fld;
}

.method public static int32 Main<valuetype T>()
{
  ldnull
  castclass !!T*
  pop

  newobj instance void [mscorlib]System.Object::.ctor()
  isinst !!T*
  brtrue BAD

  .try
  {
    newobj instance void [mscorlib]System.Object::.ctor()
    castclass !!T*
    pop
    leave BAD
  }
  catch [mscorlib]System.InvalidCastException
  {
    pop
    leave OK
  }

BAD:
  ldc.i4 2
  ret

OK:
  ldc.i4 100
  ret
}

.class public auto ansi abstract sealed beforefieldinit castclasspointer
    extends [mscorlib]System.Object
{
  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    ldnull
    castclass void*
    pop

    newobj instance void [mscorlib]System.Object::.ctor()
    isinst void*
    brtrue BAD

    .try
    {
      newobj instance void [mscorlib]System.Object::.ctor()
      castclass void*
      pop
      leave BAD
    }
    catch [mscorlib]System.InvalidCastException
    {
      pop
      leave OK
    }

  BAD:
    ldc.i4 1
    ret

  OK:

    call int32 Main<valuetype MyGen`1<object>>()
    ret
  }
}
