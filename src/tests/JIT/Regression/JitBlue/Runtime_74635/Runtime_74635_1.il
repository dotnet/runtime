// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A ) }
.assembly extern xunit.core {}
.assembly extern System.Console { }
.assembly extern TestLibrary {}
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
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    // [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsNotMonoInterpreter))]
    .custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ConditionalFactAttribute::.ctor(class [System.Runtime]System.Type,
                                                                                                  string[]) = ( 01 00 61 54 65 73 74 4C 69 62 72 61 72 79 2E 50   // ..aTestLibrary.P
                                                                                                                6C 61 74 66 6F 72 6D 44 65 74 65 63 74 69 6F 6E   // latformDetection
                                                                                                                2C 20 54 65 73 74 4C 69 62 72 61 72 79 2C 20 56   // , TestLibrary, V
                                                                                                                65 72 73 69 6F 6E 3D 30 2E 30 2E 30 2E 30 2C 20   // ersion=0.0.0.0, 
                                                                                                                43 75 6C 74 75 72 65 3D 6E 65 75 74 72 61 6C 2C   // Culture=neutral,
                                                                                                                20 50 75 62 6C 69 63 4B 65 79 54 6F 6B 65 6E 3D   //  PublicKeyToken=
                                                                                                                6E 75 6C 6C 01 00 00 00 14 49 73 4E 6F 74 4D 6F   // null.....IsNotMo
                                                                                                                6E 6F 49 6E 74 65 72 70 72 65 74 65 72 00 00 )    // noInterpreter..
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

