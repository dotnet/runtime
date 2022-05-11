// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.assembly 'ckfinite'{}

.class public _ckfinite {

.method public static int32 _ckfinite(float32) {
.maxstack	5
try_start:
	ldarg	0
	ckfinite
	pop			//remove the value from the stack
	leave	try_end
try_end:
	ldc.i4	0x00000000
	ret
handler_start:
	pop	//remove the exception ref from the stack
	leave	done
handler_end:
done:
	ldc.i4	0x00000001
	ret
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler handler_start to handler_end
}

.method public static int32 _ckfinite(float64) {
.maxstack	5
try_start:
	ldarg	0
	ckfinite
	pop	//remove the value from the stack
	leave	try_end
try_end:
	ldc.i4	0x00000000
	ret
handler_start:
	pop	//remove the exception ref from the stack
	leave	done
handler_end:
done:
	ldc.i4	0x00000001
	ret
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler handler_start to handler_end
}

.method public static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack	10
	ldc.r4		float32(0x7F800000)
	call		int32 _ckfinite::_ckfinite(float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF800000)
	call		int32 _ckfinite::_ckfinite(float32)
	brfalse		FAIL
	ldc.r4		float32(0x7FC00000)
	call		int32 _ckfinite::_ckfinite(float32)
	brfalse		FAIL
	ldc.r4		float32(0xFF7FFFFF)
	call		int32 _ckfinite::_ckfinite(float32)
	brtrue		FAIL

	ldc.r8		float64(0x7FF0000000000000)
	call		int32 _ckfinite::_ckfinite(float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFF0000000000000)
	call		int32 _ckfinite::_ckfinite(float64)
	brfalse		FAIL
	ldc.r8		float64(0x7FF8000000000000)
	call		int32 _ckfinite::_ckfinite(float64)
	brfalse		FAIL
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call		int32 _ckfinite::_ckfinite(float64)
	brtrue		FAIL
PASS:
	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
