// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly MalformedTypeSpecTests
{
}

.class public auto ansi beforefieldinit GenericBase`1<T>
    extends [System.Runtime]System.Object
{
}

.class public auto ansi beforefieldinit Extender
    extends class GenericBase`1<int32 modopt(int32)>
{
}
