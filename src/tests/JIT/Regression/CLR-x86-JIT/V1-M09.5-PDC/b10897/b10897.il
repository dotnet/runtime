// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly b10897
{
}

// ----------------------------------
// -- bug --
// 0xABBA is the errorlevel on success
// ----------------------------------
.class public bug {

.method static int32 jumpee(){
.maxstack	20
	ldc.i4 0xABBA
	ret
}

.method static int32 jumper(){
.maxstack 20
	jmp int32 bug::jumpee()
}

.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack 20
	call int32 bug::jumper()
    ldc.i4 0xABBA
    ceq
    ldc.i4 1
    sub
ldc.i4 100
add
	ret
}
}
