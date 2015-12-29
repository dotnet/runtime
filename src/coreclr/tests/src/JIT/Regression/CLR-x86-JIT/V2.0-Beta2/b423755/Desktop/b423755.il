// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

.assembly extern mscorlib { }
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly 'test' {
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
