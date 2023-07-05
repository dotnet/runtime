// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly 'b99667' {}
.assembly extern xunit.core {}
.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.class public auto ansi Test_b99667 extends [mscorlib] System.Object {
.method public static int32 main()
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    ldc.i4.s 100
    ret
}
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

