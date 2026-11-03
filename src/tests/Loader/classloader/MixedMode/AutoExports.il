// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly AutoExports {}

// No .vtfixup or .vtentry: ILASM must synthesize the table and clear ILONLY.
.method public static int32 Triple(int32 input) cil managed
{
    .export [8]
    ldarg.0
    ldc.i4.3
    mul
    ret
}

.method public static int32 Double(int32 input) cil managed
{
    .export [7] as Twice
    ldarg.0
    ldc.i4.2
    mul
    ret
}
