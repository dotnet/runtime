// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern mscorlib { }
.assembly GitHub_42719 {}
.module GitHub_42719.exe
.method public static int32 Main() cil managed
{
    .entrypoint
    .maxstack  5
    // "0 u> -1" shouldn't be optimized into "-1 >> 31"
    ldc.i4.s   100
    ldc.i4.0
    ldc.i4.m1
    cgt.un
    add  
    ret
}
