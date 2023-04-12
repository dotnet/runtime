// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b92713'{}

.class public _div {
//
.method public static	int32 _div(int32,int32) {
.locals		(class [mscorlib]System.Exception,int32)
.maxstack		3
try_start:
	ldarg	0
	ldarg	1
	div
	stloc.1
	leave.s	try_end
try_end:
	//- No exception
	ldloc.1
	br				END
arithmetic:
	//- Got an ArithmeticException
	isinst			[mscorlib]System.ArithmeticException
	stloc	0
	leave			AEEnd
AEEnd:

	ldloc	0
	brfalse			FAIL
	ldc.i4			0xAE
	br				END
divbyzero:
	//- Got a DivideByZeroException
	isinst			[mscorlib]System.DivideByZeroException
	stloc	0
	leave			DBZEnd
DBZEnd:

	ldloc	0
	brfalse			FAIL
	ldc.i4			0xDB0E
	br				END
FAIL:
	ldc.i4			0x00000000
	br				END
END:
	ret
.try try_start to try_end catch [mscorlib]System.DivideByZeroException handler divbyzero to DBZEnd
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler arithmetic to AEEnd
}
//
// -- Begin Main --------------------
.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
//
// -- Min / Min
	ldc.i4		0x80000000
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- Min / -1
	ldc.i4		0x80000000
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xAE
	ceq
	brfalse		FAIL
// -- Min / 0
	ldc.i4		0x80000000
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- Min / 1
	ldc.i4		0x80000000
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x80000000
	ceq
	brfalse		FAIL
// -- Min / Max
	ldc.i4		0x80000000
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- Min / Odd
	ldc.i4		0x80000000
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- Min / Even
	ldc.i4		0x80000000
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- -1 / Min
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000		
	ceq
	brfalse		FAIL
// -- -1 / -1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- -1 / 0
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- -1 / 1
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- -1 / Max
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- -1 / Odd
	ldc.i4		0xFFFFFFFF
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- -1 / Even
	ldc.i4		0xFFFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- 0 / Min
	ldc.i4		0x00000000
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 / -1
	ldc.i4		0x00000000
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 / 0
	ldc.i4		0x00000000
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- 0 / 1
	ldc.i4		0x00000000
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 / Max
	ldc.i4		0x00000000
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 / Odd
	ldc.i4		0x00000000
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 0 / Even
	ldc.i4		0x00000000
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- 1 / Min
	ldc.i4		0x00000001
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 1 / -1
	ldc.i4		0x00000001
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- 1 / 0
	ldc.i4		0x00000001
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- 1 / 1
	ldc.i4		0x00000001
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- 1 / Max
	ldc.i4		0x00000001
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 1 / Odd
	ldc.i4		0x00000001
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- 1 / Even
	ldc.i4		0x00000001
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- Max / Min
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Max / -1
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x80000001
	ceq
	brfalse		FAIL
// -- Max / 0
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- Max / 1
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x7FFFFFFF
	ceq
	brfalse		FAIL
// -- Max / Max
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- Max / Odd
	ldc.i4		0x7FFFFFFF
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- Max / Even
	ldc.i4		0x7FFFFFFF
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- Odd / Min
	ldc.i4		0x55555555
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Odd / -1
	ldc.i4		0x55555555
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xAAAAAAAB
	ceq
	brfalse		FAIL
// -- Odd / 0
	ldc.i4		0x55555555
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- Odd / 1
	ldc.i4		0x55555555
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x55555555
	ceq
	brfalse		FAIL
// -- Odd / Max
	ldc.i4		0x55555555
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Odd / Odd
	ldc.i4		0x55555555
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL
// -- Odd / Even
	ldc.i4		0x55555555
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL

//----------------------------------------------------------
// -- Even / Min
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x80000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Even / -1
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xFFFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x55555556
	ceq
	brfalse		FAIL
// -- Even / 0
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000000
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xDB0E
	ceq
	brfalse		FAIL
// -- Even / 1
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x00000001
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xAAAAAAAA
	ceq
	brfalse		FAIL
// -- Even / Max
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x7FFFFFFF
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000000
	ceq
	brfalse		FAIL
// -- Even / Odd
	ldc.i4		0xAAAAAAAA
	ldc.i4		0x55555555
	call	int32 _div::_div(int32,int32)
	ldc.i4		0xFFFFFFFF
	ceq
	brfalse		FAIL
// -- Even / Even
	ldc.i4		0xAAAAAAAA
	ldc.i4		0xAAAAAAAA
	call	int32 _div::_div(int32,int32)
	ldc.i4		0x00000001
	ceq
	brfalse		FAIL

// -------------------------
//
// -- PASS --
	ldc.i4	100
	ret
// -- FAIL --
FAIL:
	ldc.i4	101
	ret
// -- End Main ----------------------
}
//
// -- EOF ---------------------------
}
// ----------------------------------
