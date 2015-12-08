// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern legacy library mscorlib {}
.assembly ldstr.exe{}

.class public	_ldstr {


.method public static int32 main(string[]) {
.entrypoint
.locals	(class [mscorlib]System.IO.TextWriter)
.maxstack	10
	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
	stloc	0

	ldloc	0
	ldstr		"String 1 should equal String 2"



	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

	ldloc	0
	ldstr		"This is a Test String!"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)
	ldloc	0
	ldstr		"This is a Test String!"


	callvirt	instance void [mscorlib]System.IO.TextWriter::WriteLine(string)

	ldstr		"This is a Test String!"


	ldstr		"This is a Test String!"


	call		instance bool [mscorlib]System.String::Equals(string)
	brfalse		FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
