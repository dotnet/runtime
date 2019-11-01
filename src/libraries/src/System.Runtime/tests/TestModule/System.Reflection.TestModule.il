// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Runtime
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A)
  .ver 4:0:0:0
}

.assembly System.Reflection.TestModule
{
    .ver 1:0:0:0
}

.module System.Reflection.TestModule.dll

.field public static int32 TestInt

.field private static int64 TestLong

.method public static void .cctor()
{
    ldc.i4.1
    stsfld int32 TestInt
    ldc.i4.2
    stsfld int64 TestLong
    ret
}

.namespace System.Reflection.TestModule
{
    .class public Dummy extends [System.Runtime]System.Object
    {
    }
}
