// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { }
.assembly extern System.Console { }
.assembly extern System.Numerics.Vectors { }

.assembly Runtime_72133 { }

#define TRUE "1"
#define FALSE "0"

.typedef [System.Numerics.Vectors]System.Numerics.Vector2 as Vector2

.class Runtime_72133 extends [System.Runtime]System.Object
{
  .method static int32 Main()
  {
    .entrypoint
    .locals (int32 result)

    ldc.i4 100
    stloc result

    call bool .this::ProblemWithStructStObj()
    brfalse VECTOR_STOBJ

    ldloca result
    ldstr "STRUCT_STOBJ failed"
    call void .this::ReportFailure(int32*, string)

  VECTOR_STOBJ:
    call bool .this::ProblemWithVectorStObj()
    brfalse CPBLK

    ldloca result
    ldstr "VECTOR_STOBJ failed"
    call void .this::ReportFailure(int32*, string)

  CPBLK:
    call bool .this::ProblemWithCpBlk()
    brfalse INITBLK

    ldloca result
    ldstr "CPBLK failed"
    call void .this::ReportFailure(int32*, string)

  INITBLK:
    call bool .this::ProblemWithInitBlk()
    brfalse INITOBJ

    ldloca result
    ldstr "INITBLK failed"
    call void .this::ReportFailure(int32*, string)

  INITOBJ:
    call bool .this::ProblemWithInitObj()
    brfalse DIRECT_INITOBJ

    ldloca result
    ldstr "INITOBJ failed"
    call void .this::ReportFailure(int32*, string)

  DIRECT_INITOBJ:
    call bool .this::ProblemWithDirectInitObj()
    brfalse RETURN

    ldloca result
    ldstr "DIRECT_INITOBJ failed"
    call void .this::ReportFailure(int32*, string)

  RETURN:
    ldloc result
    ret
  }

  .method private static bool ProblemWithStructStObj() noinlining
  {
    .locals (int32 a, int32 b, int32 offs)

    ldc.i4 1
    call !!0 .this::Get<int32>(!!0)
    stloc a

    ldc.i4 2
    call !!0 .this::Get<int32>(!!0)
    stloc b

    ldc.i4 0
    call !!0 .this::Get<int32>(!!0)
    stloc offs

    ldloc a

    ldloca a
    ldloc offs
    add
    ldloca b
    ldobj StructWithInt
    stobj StructWithInt

    ldc.i4 1
    bne.un FAILURE
    ldc.i4 FALSE
    ret

  FAILURE:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithVectorStObj() noinlining
  {
    .locals (int64 a, int64 b, int32 offs)

    ldc.i8 1
    call !!0 .this::Get<int64>(!!0)
    stloc a

    ldc.i8 2
    call !!0 .this::Get<int64>(!!0)
    stloc b

    ldc.i4 0
    call !!0 .this::Get<int32>(!!0)
    stloc offs

    ldloc a

    ldloca a
    ldloc offs
    add
    ldloca b
    ldobj Vector2
    stobj Vector2

    ldc.i8 1
    bne.un FAILURE
    ldc.i4 FALSE
    ret

  FAILURE:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithCpBlk() noinlining
  {
    .locals (int32 a, int32 b, int32 offs)

    ldc.i4 1
    call !!0 .this::Get<int32>(!!0)
    stloc a

    ldc.i4 2
    call !!0 .this::Get<int32>(!!0)
    stloc b

    ldc.i4 0
    call !!0 .this::Get<int32>(!!0)
    stloc offs

    ldloc a

    ldloca a
    ldloc offs
    add
    ldloca b
    sizeof int32
    cpblk

    ldc.i4 1
    bne.un FAILURE
    ldc.i4 FALSE
    ret

  FAILURE:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithInitBlk() noinlining
  {
    .locals (int32 a, int32 b, int32 offs)

    ldc.i4 1
    call !!0 .this::Get<int32>(!!0)
    stloc a

    ldc.i4 2
    call !!0 .this::Get<int32>(!!0)
    stloc b

    ldc.i4 0
    call !!0 .this::Get<int32>(!!0)
    stloc offs

    ldloc a

    ldloca a
    ldloc offs
    add
    ldloc b
    sizeof int32
    initblk

    ldc.i4 1
    bne.un FAILURE
    ldc.i4 FALSE
    ret

  FAILURE:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithInitObj() noinlining
  {
    .locals (object a, int32 offs)

    newobj instance void object::.ctor()
    call !!0 .this::Get<object>(!!0)
    stloc a

    ldc.i4 0
    call !!0 .this::Get<int32>(!!0)
    stloc offs

    ldloc a

    ldloca a
    ldloc offs
    add
    initobj object

    ldnull
    beq FAILURE
    ldc.i4 FALSE
    ret

  FAILURE:
    ldc.i4 TRUE
    ret
  }

  .method private static bool ProblemWithDirectInitObj() noinlining
  {
    .locals (valuetype StructWithInt a, valuetype StructWithInt* pA)

    ldloca a
    ldflda int32 StructWithInt::Value
    ldc.i4 1
    call !!0 .this::Get<int32>(!!0)
    stfld int32 StructWithInt::Value

    ldloca a
    stloc pA

    ldloc pA
    ldfld int32 StructWithInt::Value

    ldloca a
    initobj StructWithInt

    ldc.i4 1
    bne.un FAILURE
    ldc.i4 FALSE
    ret

  FAILURE:
    ldc.i4 TRUE
    ret
  }

  .method private static !!T Get<T>(!!T arg) noinlining
  {
    ldarg arg
    ret
  }

  .method private static void ReportFailure(int32* pResult, string msg) noinlining
  {
    ldarg pResult
    ldarg pResult
    ldind.i4
    ldc.i4 1
    add
    stind.i4

    ldarg msg
    call void [System.Console]System.Console::WriteLine(string)

    ret
  }
}

.class sealed sequential StructWithInt extends [System.Runtime]System.ValueType
{
  .field public int32 Value
}
