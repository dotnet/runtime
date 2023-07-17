// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly runtime68278 { }

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

.class interface abstract IUnrelated
{
    .field public static int8 s_fiftyEight
}

.method static int32 main()
{
    .entrypoint
    ldsfld int8 IHasCctor::s_fortyTwo
    ldsfld int8 IUnrelated::s_fiftyEight
    add
    ret
}
