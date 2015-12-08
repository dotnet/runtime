// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.


.assembly extern legacy library mscorlib {}
.assembly throw.exe{}

.class public	_throw {

.method public void _throw() {
.maxstack	2
	ret
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
try_start:
	AEtry_start:
		newobj	instance void [mscorlib]System.ArithmeticException::.ctor() 
		throw	//Instruction Under Test
	AEtry_end:
		leave		FAIL
	AEhandler_start:
		pop
		leave	done
	AEhandler_end:
	done:
		leave		PASS
try_end:
	br	FAIL
aHandler_start:
	pop
	leave	done2
aHandler_end:
done2:
	br	FAIL

PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
.try AEtry_start to AEtry_end catch [mscorlib]System.ArithmeticException handler AEhandler_start to AEhandler_end
.try try_start to try_end catch [mscorlib]System.Exception handler aHandler_start to aHandler_end
}
}
