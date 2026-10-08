// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console { }
.assembly extern TestLibrary { .ver 0:0:0:0 }
.assembly extern Microsoft.DotNet.XUnitExtensions { .publickeytoken = (31 BF 38 56 AD 36 4E 35 ) }

.assembly Runtime_74635_1 { }

#define TRUE "1"
#define FALSE "0"
#define THIS "0"

#define EXPECTED "0"
#define THRASHED "1"

.class public Runtime_74635_1 extends [System.Runtime]System.Object
{
  .method public static int32 Main()
  {
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = {}
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
        string('https://github.com/dotnet/runtime/issues/74687')
        type([TestLibrary]TestLibrary.PlatformDetection)
        string[1] ('IsAppleMobile')
    }
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {
        string('https://github.com/dotnet/runtime/issues/74687')
        type([TestLibrary]TestLibrary.PlatformDetection)
        string[1] ('IsMonoInterpreter')
    }
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, valuetype [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = {
        string('https://github.com/dotnet/runtime/issues/74687')
        int32(0x2400) // Browser | Wasi
    }

    .entrypoint

    .locals (valuetype Closure closure)

    ldc.i4 EXPECTED
    ldloca closure
    newobj instance void Struct::.ctor(int32, valuetype Closure&)
    ldloc closure
    ldfld valuetype Struct& Closure::StructRef
    ldc.i4 THRASHED
    stfld int32 Struct::Value
    ldfld int32 Struct::Value
    ldc.i4 EXPECTED
    bne.un FAILED

    ldc.i4 100
    ret

    FAILED:
      ldc.i4 101
      ret
  }
}

.class sealed sequential Struct extends [System.Runtime]System.ValueType
{
  .field public int32 Value

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
}

.class sealed Closure extends [System.Runtime]System.ValueType
{
  .custom instance void [System.Runtime]System.Runtime.CompilerServices.IsByRefLikeAttribute::.ctor() = (01 00 00 00)

  .field public valuetype Struct& StructRef
}

