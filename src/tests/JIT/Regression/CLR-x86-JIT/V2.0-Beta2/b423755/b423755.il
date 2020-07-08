// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly extern mscorlib {}

.assembly 'test' {
.custom instance void [mscorlib]System.Security.SecurityTransparentAttribute::.ctor() = ( 01 00 00 00 )
}

.method public static int32 Main()
{
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
