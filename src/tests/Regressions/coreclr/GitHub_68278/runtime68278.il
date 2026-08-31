// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly runtime68278 { }
.assembly extern xunit.core {}

.class interface abstract IHasCctor
{
    .data cil FortyTwo = int8(42)
    .field public static int8 s_fortyTwo at FortyTwo
    .method specialname rtspecialname static void .cctor()
    {
        ldc.i4 58
        stsfld int8 IUnrelated::s_fiftyEight
        ret
    }
}

.class public interface abstract IUnrelated
{
    .field public static int8 s_fiftyEight
}

.class public auto ansi abstract sealed beforefieldinit runtime68278
    extends [mscorlib]System.Object
{
    .method public static int32 main()
    {
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        ldsfld int8 IHasCctor::s_fortyTwo
        ldsfld int8 IUnrelated::s_fiftyEight
        add
        ret
    }
}
