// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly c4{}

.method	static int32 main(class System.String[]) {
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
