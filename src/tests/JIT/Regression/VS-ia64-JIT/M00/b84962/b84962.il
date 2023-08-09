// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b84962'{}
.class public _ret {
.field public static native int PTR
.method public static		int32 retI(int32*) {
.maxstack	5
	ldarg		0
	conv.i4
	ret
}
.method public static int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack	10
        ldsflda		native int _ret::PTR
        call    int32 _ret::retI(int32*)
        ldsflda		native int _ret::PTR
	conv.i4
        ceq
        brfalse         FAIL
	ldc.i4	100
	ret
FAIL:
	ldc.i4	5
	ret
}
}
