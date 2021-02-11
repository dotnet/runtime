// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern mscorlib {}
.assembly Library {}

.class public auto ansi sealed MyClass1
{
    .field public int32 myValue1
}

.class public auto ansi MyClass2 extends MyClass1
{
    .field public int32 myValue2
}
