// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly tst { }
 
.method static void* GetValue()
{
    .locals init ([0] void*)
    ldc.i4 100
    conv.i
    stloc.0
    ldloca 0
    ldobj void*
    ret
}

.method static int32 main()
{
    .entrypoint
    call void* GetValue()
    conv.i4
    ret
}
