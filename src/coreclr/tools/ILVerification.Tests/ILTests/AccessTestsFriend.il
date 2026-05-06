// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Runtime
{
}

.assembly extern AccessTests
{
}

.assembly AccessTestsFriend
{
    // InternalsVisibleTo("AccessTests")
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = (
        01 00 0b 41 63 63 65 73 73 54 65 73 74 73 00 00
    )

    // InternalsVisibleTo("AccessTestsExtern ")
    //                                      ^ purposeful space at the end
    .custom instance void [System.Runtime]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = (
        01 00 12 41 63 63 65 73 73 54 65 73 74 73 45 78
        74 65 72 6e 20 00 00
    )
}

.class private auto ansi beforefieldinit AccessTestsFriendType
        extends [System.Runtime]System.Object
{
    .field private static int32 privateField
    .field assembly static int32 assemblyField
}
