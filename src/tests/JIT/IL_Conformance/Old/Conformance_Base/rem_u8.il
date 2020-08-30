// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

.class public rem_un {
.method public static	int64 _rem(int64,int64) {
.locals		(class [mscorlib]System.Exception, unsigned int64)
.maxstack		3
try_start:
	ldarg	0
	ldarg	1
	rem.un
	stloc.1
	leave.s	try_end
try_end:
	ldloc.1
	br				END
divbyzero:
	isinst			[mscorlib]System.DivideByZeroException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse			FAIL
	ldc.i8			0xDB0E
	br				END
FAIL:
	ldc.i8			0x0000000000000000
	br				END
END:
	ret
.try try_start to try_end catch [mscorlib]System.DivideByZeroException handler divbyzero to HEnd
}
.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		20
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000000
	call		int64 rem_un::_rem(int64,int64)
	ldc.i8		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0x0000000000000001
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0x5555555555555555
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000000
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL

	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000000
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0x0000000000000001
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000001
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000001
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0x5555555555555555
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000001
	ceq
	brfalse		FAIL
	ldc.i8		0x0000000000000001
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000001
	ceq
	brfalse		FAIL

	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x0000000000000000
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x0000000000000001
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0x5555555555555555
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x5555555555555554
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFE
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x5555555555555554
	ceq
	brfalse		FAIL

	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000000
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x0000000000000001
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000001
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0x5555555555555555
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xFFFFFFFFFFFFFFFF
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x5555555555555555
	ceq
	brfalse		FAIL

	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000000
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0x0000000000000001
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x5555555555555555
	ceq
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x5555555555555555
	ceq
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0x5555555555555555
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0x5555555555555555
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x5555555555555555
	ceq
	brfalse		FAIL

	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000000
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xDB0E
	ceq
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x0000000000000001
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xFFFFFFFFFFFFFFFE
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xFFFFFFFFFFFFFFFF
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ceq
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0x5555555555555555
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL
	ldc.i8		0xAAAAAAAAAAAAAAAA
	ldc.i8		0xAAAAAAAAAAAAAAAA
	call	int64 rem_un::_rem(int64,int64)
	ldc.i8		0x0000000000000000
	ceq
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly rem_u8.exe{}
