// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern System.Runtime { }
.assembly Negative_ByRefLikeType { }

.class sequential ansi sealed beforefieldinit MyByRefLikeType
       extends [System.Runtime]System.ValueType
{
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = ( 01 00 00 00 ) 
}

.class sequential ansi sealed beforefieldinit ByRefLikeStaticField
{
  .field private static valuetype MyByRefLikeType s
}

.class sequential ansi sealed beforefieldinit ByRefLikeFieldInNonValueType
{
  .field private valuetype MyByRefLikeType f
}

.class sequential ansi sealed beforefieldinit ByRefLikeFieldInNonByRefLikeType
{
  .field private valuetype MyByRefLikeType f
}

.class public auto ansi beforefieldinit Test
       extends [System.Runtime]System.Object
{
  .method private hidebysig static void ByRefLikeBoxing() cil managed
  {
    .maxstack  1
    .locals init (valuetype MyByRefLikeType V_0)
    ldloc.0
    box valuetype MyByRefLikeType
    pop
    ret
  }

  .method private hidebysig static void ByRefLikeStaticField() cil managed
  {
    .maxstack  1
    ldsfld valuetype MyByRefLikeType ByRefLikeStaticField::s
    pop
    ret
  }

  .method private hidebysig static void ByRefLikeFieldInNonValueType() cil managed
  {
    .maxstack  1
    ldnull
    ldfld valuetype MyByRefLikeType ByRefLikeFieldInNonValueType::f
    pop
    ret
  }

  .method private hidebysig static void ByRefLikeFieldInNonByRefLikeType() cil managed
  {
    .maxstack  1
    .locals init (valuetype ByRefLikeFieldInNonByRefLikeType V_0)
    ldloc.0
    ldfld valuetype MyByRefLikeType ByRefLikeFieldInNonValueType::f
    pop
    ret
  }

  .method private hidebysig static void ByRefLikeArray() cil managed
  {
    .maxstack  1
    ldc.i4.1
    newarr     valuetype MyByRefLikeType
    pop
    ret
  }

  .method private hidebysig static void ByRefLikeGenericInstantiation() cil managed
  {
    .maxstack  1
    newobj     instance void class [System.Runtime]System.Collections.Generic.List`1<valuetype MyByRefLikeType>::.ctor()
    pop
    ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .entrypoint
    .maxstack  1

    ldstr "ByRefLikeBoxing"
    call void [System.Console]System.Console::WriteLine(string)
    .try
    {
        call void Test::ByRefLikeBoxing()
        leave TestFailed
    }
    catch [System.Runtime]System.InvalidProgramException
    {
      pop
      leave ByRefLikeBoxing_Done
    }
ByRefLikeBoxing_Done:

    ldstr "ByRefLikeStaticField"
    call void [System.Console]System.Console::WriteLine(string)
    .try
    {
        call void Test::ByRefLikeStaticField()
        leave TestFailed
    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave ByRefLikeStaticField_Done
    }
ByRefLikeStaticField_Done:

    ldstr "ByRefLikeFieldInNonValueType"
    call void [System.Console]System.Console::WriteLine(string)
    .try
    {
        call void Test::ByRefLikeFieldInNonValueType()
        leave TestFailed
    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave ByRefLikeFieldInNonValueType_Done
    }
ByRefLikeFieldInNonValueType_Done:

    ldstr "ByRefLikeFieldInNonByRefLikeType"
    call void [System.Console]System.Console::WriteLine(string)
    .try
    {
        call void Test::ByRefLikeFieldInNonByRefLikeType()
        leave TestFailed
    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave ByRefLikeFieldInNonByRefLikeType_Done
    }
ByRefLikeFieldInNonByRefLikeType_Done:

    ldstr "ByRefLikeArray"
    call void [System.Console]System.Console::WriteLine(string)
    .try
    {
        call void Test::ByRefLikeArray()
        leave TestFailed
    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave ByRefLikeArray_Done
    }
ByRefLikeArray_Done:

    ldstr "ByRefLikeGenericInstantiation"
    call void [System.Console]System.Console::WriteLine(string)
    .try
    {
        call void Test::ByRefLikeGenericInstantiation()
        leave TestFailed
    }
    catch [System.Runtime]System.TypeLoadException
    {
      pop
      leave ByRefLikeGenericInstantiation_Done
    }
ByRefLikeGenericInstantiation_Done:

    ldstr "All Tests Passed"
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4.s 100
    ret
  TestFailed:
    ldstr "Test Failed"
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4.1
    ret
  }
}
