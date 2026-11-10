// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly GitHub_92566 {}

.class public auto ansi beforefieldinit G`1<T>
       extends [System.Runtime]System.Object
{
  .param constraint T, mdtoken(0x1B0000FF)

  .method public hidebysig specialname rtspecialname instance void .ctor() cil managed
  {
    ldarg.0
    call instance void [System.Runtime]System.Object::.ctor()
    ret
  }
}

.class public auto ansi beforefieldinit GitHub_92566
       extends [System.Runtime]System.Object
{
  .method private hidebysig static void LoadType() cil managed noinlining
  {
    ldtoken class G`1
    call class [System.Runtime]System.Type [System.Runtime]System.Type::GetTypeFromHandle(valuetype [System.Runtime]System.RuntimeTypeHandle)
    call void [System.Console]System.Console::WriteLine(object)
    ret
  }

  .method public hidebysig static int32 Main() cil managed
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (01 00 00 00)
    .entrypoint
    .maxstack 2
    .try
    {
      call void GitHub_92566::LoadType()
      leave.s Fail
    }
    catch [System.Runtime]System.TypeLoadException
    {
      call void [System.Console]System.Console::WriteLine(object)
      leave.s Pass
    }
  Fail:
    ldstr "Failed: no TypeLoadException"
    call void [System.Console]System.Console::WriteLine(string)
    ldc.i4.m1
    ret
  Pass:
    ldc.i4 100
    ret
  }
}
