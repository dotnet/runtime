// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly simple {}
.assembly extern mscorlib {}

.method public static int32 main()
{
    .entrypoint
    ldc.i4.s 100
    ret
}

.method public static valuetype [mscorlib]System.DateTime 
        bad(int32 a, int32 b, int32 c)
{
    .locals init (class [mscorlib]System.Globalization.Calendar V_0,
           valuetype [mscorlib]System.DateTime V_1,
           valuetype [mscorlib]System.DateTime V_2,
           valuetype [mscorlib]System.DateTime V_3)

    ldc.i4 0
    starg.s 0

    ldloc.2
    ret
}

