// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib
{
}
.assembly layouttokens
{
}
.module layouttokens.dll

.class public sequential ansi TokenLayout extends [mscorlib]System.Object
{
    .field public int32 never_used
}

.class public auto ansi AutoTokenLayout extends [mscorlib]System.Object
{
    .field public int32 never_used
}

.class public auto ansi TokenUser extends [mscorlib]System.Object
{
    .method public static void Touch() cil managed
    {
        ldtoken TokenLayout
        pop
        ldtoken AutoTokenLayout
        pop
        ret
    }
}
