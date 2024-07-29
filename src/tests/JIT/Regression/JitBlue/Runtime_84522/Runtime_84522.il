// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern xunit.core { }
.assembly Runtime_84522 { }

.class sealed StructWithInt extends [System.Runtime]System.ValueType
{
  .field public int32 Value
}

.class Runtime_84522 extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint

    .try
    {
      ldc.i4 0
      newarr StructWithInt
      call void .this::UnusedLdelem(valuetype StructWithInt[])
      leave FAIL
    }
    catch [System.Runtime]System.IndexOutOfRangeException
    {
      pop
      leave SUCCESS
    }

  FAIL:
    ldc.i4 0
    ret

  SUCCESS:
    ldc.i4 100
    ret
  }

  .method private static void UnusedLdelem(valuetype StructWithInt[] arr) noinlining
  {
    ldarg arr
    ldc.i4 0
    ldelem valuetype StructWithInt
    pop
    ret
  }
}
