// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public rem_i4 {
.method public static	int32 _rem(int32,int32) {
.locals		(class [mscorlib]System.Exception,int32)
.maxstack		3
try_start:
	ldarg	0
	ldarg	1
	rem
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	br				END
arithmetic:
	isinst			[mscorlib]System.ArithmeticException
	stloc	0
	leave			AEEnd
AEEnd:

	ldloc	0
	brfalse			FAIL
	ldc.i4			0xAE
	br				END
divbyzero:
	isinst			[mscorlib]System.DivideByZeroException
	stloc	0
	leave			DBZEnd
DBZEnd:

	ldloc	0
	brfalse			FAIL
	ldc.i4			0xDB0E
	br				END
FAIL:
	ldc.i4			0xBAD
	br				END
END:
	ret
.try try_start to try_end catch [mscorlib]System.DivideByZeroException handler divbyzero to DBZEnd
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler arithmetic to AEEnd
}
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xAE
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xD5555555
	ceq
	brfalse		FAIL
	ldc.i4		0x80000000
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xD5555556
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

	ldc.i4		0x00000001
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL

	ldc.i4		0x7FFFFFFF
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x2AAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x2AAAAAA9
	ceq
	brfalse		FAIL

	ldc.i4		0x55555555
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL

	ldc.i4		0xAAAAAAAA
	ldc.i4		0x80000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000001
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x7FFFFFFF
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 rem_i4::_rem(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly rem_i4{}
