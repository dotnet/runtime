// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Console { }

.assembly Runtime_74635.dll { }

#define TRUE "1"
#define FALSE "0"
#define THIS "0"

#define EXPECTED "0"
#define THRASHED "1"

.class Runtime_74635 extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .entrypoint

    call bool .this::ProblemWithDevirtualization()
    brtrue DEVIRT_FAILED

    ldc.i4 100
    ret

  DEVIRT_FAILED:
    ldc.i4 101
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
}

.class interface Interface
{
  .method public abstract virtual int32 ReturnSelf() { }
}

.class sealed sequential Struct extends [System.Runtime]System.ValueType implements Interface
{
  .field public int32 Value

  .method public specialname void .ctor(int32 val)
  {
    ldarg THIS
    ldarg val
    stfld int32 .this::Value
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
