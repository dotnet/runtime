// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern System.Console { }
.assembly extern xunit.core
{
    .publickeytoken = (
        8d 05 b1 bb 7a 6f db 6c
     )
     .ver 2:4:2:0
}
.assembly test3{}
.assembly extern mscorlib{}

.class public CMain{
    .method public static int32 Main(){
        .custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
            01 00 00 00
        )
        .entrypoint
        call int32 IFoo::Foo()
        ret
    }
}

.class public interface IFoo{
    .method public static int32 Foo(){
        ldstr "PASS: In IFoo::Foo"
        call void [System.Console]System.Console::WriteLine(string)
        ldc.i4 100
        ret
    }
}
