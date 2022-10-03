// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Console { }

.assembly Runtime_71638.dll { }

.class Runtime_71638 extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .entrypoint

    ldc.i4 10
    call int32 Runtime_71638::Problem(int32)
    ldc.i4 10
    bne.un FAILURE

    ldc.i4 100
    ret

  FAILURE:
    ldc.i4 101
    ret
  }

  .method private static int32 Problem(int32 a) noinlining
  {
    .locals (int32& aRef)

    ldarga a
    stloc aRef

    ldarg a
    ldloc aRef
    ldc.i4 0
    stfld int32 StructWithIndex::Index
    ret
  }
}

.class sealed StructWithIndex extends [System.Runtime]System.ValueType
{
  .field public int32 Index;
}
