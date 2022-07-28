// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}

.assembly extern legacy library mscorlib {}
.assembly heap_ovf{
}

.class public			heap_ovf {
.method public static	int32 main(string[]) {
.locals		(class [mscorlib]System.Exception)
.entrypoint
try_start:
	call	void heap_ovf::overflow()
	leave	PASS
try_end:
	br			FAIL
handler_start:
	stloc		0
	leave		done
handler_end:
done:
	call	    class [mscorlib]System.IO.TextWriter [System.Console]System.Console::get_Out()
	ldloc		0
	callvirt	instance	string [mscorlib]System.Exception::ToString()
	callvirt	instance	void [mscorlib]System.IO.TextWriter::WriteLine(string)
	ldloc		0
	isinst		[mscorlib]System.StackOverflowException
	brtrue		PASS
	br			FAIL
PASS:
	ldstr "Test PASS"
	call void [System.Console]System.Console::WriteLine(string)	
	ldc.i4 100
	br		END
FAIL:
	ldstr "Test FAILED"
	call void [System.Console]System.Console::WriteLine(string)	
	ldc.i4	0x00000000
	br		END
END:
	ret
.try try_start to try_end catch [mscorlib]System.StackOverflowException handler handler_start to handler_end
}

.method public static void overflow(){
.locals		(int32,int32*,class [mscorlib]System.Exception)
	ldc.i4		0x0
	stloc		0
LOOP:
	ldc.i4		0x0AFF
	localloc
	stloc		1
	ldloc		0
	ldc.i4		0x1
	add
	stloc		0
	ldloc		0
	ldc.i4		0x00FF
	cgt
	brfalse		LOOP

	ret
}

}
