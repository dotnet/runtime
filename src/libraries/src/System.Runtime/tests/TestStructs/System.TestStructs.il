// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 4:0:0:0
}

.assembly System.TestStructs
{
    .ver 1:0:0:0
}

.module System.TestStructs.dll

.namespace System.Tests
{
    .class public sequential sealed StructWithDefaultConstructor
        extends [System.Runtime]System.ValueType
    {
        .size 1
    }

    .class public sequential sealed StructWithPublicDefaultConstructor
        extends [System.Runtime]System.ValueType
    {
        .field public initonly bool ConstructorInvoked

        .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
        {
            ldarg.0
            ldc.i4.1
            stfld bool System.Tests.StructWithPublicDefaultConstructor::ConstructorInvoked
            ret
        }
    }

    .class public sequential sealed StructWithPrivateDefaultConstructor
        extends [System.Runtime]System.ValueType
    {
        .size 1

        .method private hidebysig specialname rtspecialname instance void .ctor () cil managed 
        {
            ret
        }
    }

    .class public sequential sealed StructWithoutDefaultConstructor
        extends [System.Runtime]System.ValueType
    {
        .size 1

        .method public hidebysig specialname rtspecialname instance void .ctor (int32 v) cil managed 
        {
            ret
        }
    }

    .class public sequential sealed StructWithDefaultConstructorThatThrows
        extends [System.Runtime]System.ValueType
    {
        .size 1

        .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
        {
            newobj instance void [System.Runtime]System.Exception::.ctor()
            throw
        }
    }
}
