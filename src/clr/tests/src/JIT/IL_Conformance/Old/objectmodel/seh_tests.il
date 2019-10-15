// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern legacy library mscorlib {}
.assembly seh_tests.exe{}

.class public		seh {

.method public static	int32 main(string[]) {
.entrypoint
.locals(int32)
.maxstack	5

ldc.i4	0x0
stloc	0

try_start:
	newobj instance void [mscorlib]System.Exception::.ctor()
	throw
	leave FAIL
try_end:

aFilter:
	isinst	[mscorlib]System.Exception
	brfalse	CONTINUE
	ldc.i4  0x00000001
	br	END_FILTER
CONTINUE:
	ldc.i4  0x00000000
END_FILTER:
	endfilter

aHandler:
	isinst	[mscorlib]System.Security.SecurityException
	brtrue	bad
	leave aHandler_end
bad:
	leave FAIL
aHandler_end:
	ldc.i4 100
	stloc  0
	ldstr	"Testcase successful"
	br		END
FAIL:
	ldstr	"Testcase failed"


END:
    call void [System.Console]System.Console::WriteLine(string)
	ldloc	0
	ret

.try try_start to try_end filter aFilter handler aHandler to aHandler_end
}
}
