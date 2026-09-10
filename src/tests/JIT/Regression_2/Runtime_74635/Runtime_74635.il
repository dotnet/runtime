// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console { }

.assembly Runtime_74635 { }

#define TRUE "1"
#define FALSE "0"
#define THIS "0"

#define EXPECTED "0"
#define THRASHED "1"

.class public Runtime_74635 extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    call bool .this::ProblemWithDevirtualization()
    brtrue DEVIRT_FAILED

    call bool .this::ProblemWithCtors()
    brtrue CTORS_FAILED

    call bool .this::ProblemWithLocAlloc()
    brtrue LOCALLOC_FAILED

    ldc.i4 100
    ret

  DEVIRT_FAILED:
    ldc.i4 101
    ret

  CTORS_FAILED:
    ldc.i4 102
    ret

  LOCALLOC_FAILED:
    ldc.i4 103
    ret
  }

  .method private static bool ProblemWithDevirtualization() noinlining
  {
    ldc.i4 EXPECTED
    newobj instance void Struct::.ctor(int32)
    box valuetype Struct
    callvirt instance int32 Interface::ReturnSelf()
    ldc.i4 EXPECTED
    bne.un FAILED

    ldc.i4 FALSE
    ret

  FAILED:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithCtors() noinlining
  {
    ldc.i4 EXPECTED
    ldc.i4 THRASHED
    newobj instance void Struct::.ctor(int32, int32)
    pop

    ldsfld int32 Struct::StaticValue
    ldc.i4 EXPECTED
    bne.un FAILED

    ldc.i4 FALSE
    ret

  FAILED:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithLocAlloc() noinlining
  {
    sizeof valuetype Struct
    localloc
    call int32 .this::ReturnExpectedWithLocAlloc(void*)
    ldc.i4 EXPECTED
    bne.un FAILED

    ldc.i4 FALSE
    ret

  FAILED:
    ldc.i4 TRUE
    ret
  }

  .method private static int32 ReturnExpectedWithLocAlloc(void* pLocAlloc)
  {
    ldarg pLocAlloc
    ldc.i4 EXPECTED
    stind.i4

    ldarg pLocAlloc
    ldfld int32 Struct::Value
    ldarg pLocAlloc
    ldc.i4 THRASHED
    stind.i4
    ret
  }
}

.class interface Interface
{
  .method public abstract virtual int32 ReturnSelf() { }
}

.class sealed sequential Struct extends [System.Runtime]System.ValueType implements Interface
{
  .field public static int32 StaticValue

  .field public int32 Value

  .method public void .ctor(int32 val)
  {
    ldarg THIS
    ldarg val
    stfld int32 .this::Value

    ret
  }

  .method public void .ctor(int32 expected, int32 thrashed)
  {
    ldarg THIS
    ldarg expected
    stfld int32 .this::Value

    ldarg THIS
    ldfld int32 .this::Value
    ldarg THIS
    ldarg thrashed
    stind.i4
    stsfld int32 .this::StaticValue

    ret
  }

  .method public void .ctor(int32 val, valuetype Closure& closureRef) noinlining
  {
    ldarg THIS
    ldarg val
    stfld int32 .this::Value

    ldarg closureRef
    ldarg THIS
    stfld valuetype Struct& Closure::StructRef

    ret
  }

  .method public virtual int32 ReturnSelf()
  {
    ldarg THIS
    ldfld int32 .this::Value
    ldarg THIS
    ldc.i4 THRASHED
    stind.i4

    ret
  }
}

.class sealed Closure extends [System.Runtime]System.ValueType
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)

  .field public valuetype Struct& StructRef
}
