// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b56780'{}

.class public auto ansi Test_b56780 extends [mscorlib] System.Object {
.method	public static int32 main() {
	.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
	    01 00 00 00
	)
	.entrypoint
	.maxstack	10

//----------------------------------
	ldc.i4	0x7FFFFFFF
	conv.r4
	ldc.r4	0x7FFFFFFF
	bne.un	FAIL
//----------------------------------

PASS:
	ldstr	"Passed"
	br		END
FAIL:
	ldstr	"Failed"
END:
	call	void [System.Console]System.Console::WriteLine(class [mscorlib]System.String)
	ldc.i4 100
	ret
}
}
