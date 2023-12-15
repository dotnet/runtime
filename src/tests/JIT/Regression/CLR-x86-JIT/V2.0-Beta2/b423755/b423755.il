// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern xunit.core {}
.assembly extern mscorlib {}

.assembly 'b423755' {
.custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
}

.class public auto ansi Test_b423755 extends [mscorlib] System.Object {
.method public static int32 Main()
{
    .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
        01 00 00 00
    )
    .entrypoint
    ldc.i4 0
    box int8
    unbox.any int8
    pop

    ldstr "Test SUCCESS"
    call void [System.Console]System.Console::WriteLine(string)

    ldc.i4 100 
    ret
}
}
