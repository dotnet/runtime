// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'DevDiv_405852'{}

.class public _rem {
//-- 
.method public static	int32 _rem(float32,float32,float32) aggressiveinlining {
.maxstack		3
.locals	(int32)
	// -- load args _and divide
	ldarg	0
	ldarg	1
	rem
	// -- dup so the result remains after _call to checkfinite
	dup
	// -- see if our number is finite (_ret val of 0 is finite)
	call	int32 _rem::checkfinite(float32)
	ldc.i4		0x0
	ceq
	// -- If the result is finite, branch
	brtrue		COMPARE

	// -- our result was _not finite, now check abnormal case
	// -- call checkfinite again on the initial result
	// -- should return  1 for NaN, 2 fo -inf, 3 for +inf, _and 255 for error
	call	int32 _rem::checkfinite(float32)

	stloc	0	
	ldloc	0	
	ldc.i4		0xFF
	ceq
	brtrue		FAIL	// error while determining type

	// -- call checkfinite on our expected value.
	ldarg	2
	call	int32 _rem::checkfinite(float32)

	// -- if the results are == we PASS.  If _not, FAIL.
	ldloc	0
	ceq
	// -- cannot be finite
	// -- if the results are == we PASS.  If _not, FAIL.
	brfalse		FAIL

	ldc.i4		0x1
	br			FINISH

COMPARE:
	// -- our result was a finite number.
	// -- compare it to our expected result.
	ldarg	2
	ceq
	brfalse		FAIL

	ldc.i4		0x1
	br			FINISH

FAIL:
	ldc.i4		0x0
	br			FINISH

FINISH:
	ret
}

//-----------------------------
.method public static	int32 checkfinite(float32) {
.locals		(class [mscorlib]System.ArithmeticException)
.maxstack		3
// -- 0 if finite
// -- 1 for NaN
// -- 2 for -inf
// -- 3 for +inf
// -- 255 (0xFF) none of the above 

try_start:
	ldarg	0
	ckfinite
	pop
	leave.s	try_end
try_end:
// -- our result is a finite number.  
	ldc.i4		0x0
	br			FINISH

ae:
// -- our result is NaN, +inf, _or -inf.  Find out which one. 
	isinst		[mscorlib]System.ArithmeticException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL

	// -- check for -inf
	ldarg	0
	ldc.r4		float32(0xFF800000)
	ceq
	brtrue		NINF

	// -- check for +inf
	ldarg	0
	ldc.r4		float32(0x7F800000)
	ceq
	brtrue		PINF

	// -- must be NaN
	ldc.i4		0x00000001
	br			FINISH

NINF:
	ldc.i4		0x00000002
	br			FINISH

PINF:
	ldc.i4		0x00000003
	br			FINISH

NaN:
	ldc.i4		0x00000001
	br			FINISH

FAIL:
//	pop
	ldc.i4		0xFF
	br			FINISH

FINISH:
	ret
.try try_start to try_end catch [mscorlib]System.ArithmeticException handler ae to HEnd
}

// -- Begin Main --------------------
.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
// -- -inf/-inf --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/-min --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/-1 --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/-0 --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/+0 --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/+1 --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/+max --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf/+inf --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf.NaN --
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -inf.float32 --
	ldc.r4		float32(0xFF800000)
	ldc.r4		1.5
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- -min/-inf --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/-min --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/-1 --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/-0 --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/+0 --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/+1 --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/+max --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min/+inf --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF7FFFFF)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min.NaN --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -min.float32 --
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		1.5
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- -1/-inf --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0xBF800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1/-min --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0xBF800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1/-1 --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1/-0 --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1/+0 --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1/+1 --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1/+max --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xBF800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL
// -- -1/+inf --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xBF800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1.NaN --
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -1.float32 --
	ldc.r4		float32(0xBF800000)
	ldc.r4		1.5
	ldc.r4		float32(0xBF800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- -0/-inf --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/-min --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/-1 --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/-0 --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/+0 --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/+1 --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/+max --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0/+inf --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0.NaN --
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- -0.float32 --
	ldc.r4		float32(0x80000000)
	ldc.r4		1.5
	ldc.r4		float32(0x80000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- +0/-inf --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/-min --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/-1 --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/-0 --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/+0 --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/+1 --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/+max --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0/+inf --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0.NaN --
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +0.float32 --
	ldc.r4		float32(0x00000000)
	ldc.r4		1.5
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- +1/-inf --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x3F800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/-min --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x3F800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/-1 --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/-0 --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/+0 --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/+1 --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/+max --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x3F800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1/+inf --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x3F800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1.NaN --
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +1.float32 --
	ldc.r4		float32(0x3F800000)
	ldc.r4		1.5
	ldc.r4		float32(0x3F800000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- +max/-inf --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/-min --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/-1 --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/-0 --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/+0 --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/+1 --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/+max --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max/+inf --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F7FFFFF)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max.NaN --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +max.float32 --
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		1.5
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- +inf/-inf --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/-min --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/-1 --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/-0 --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/+0 --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/+1 --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/+max --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf/+inf --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf.NaN --
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- +inf.float32 --
	ldc.r4		float32(0x7F800000)
	ldc.r4		1.5
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- NaN/-inf --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/-min --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/-1 --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/-0 --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/+0 --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/+1 --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/+max --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN/+inf --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN.NaN --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- NaN.float32 --
	ldc.r4		float32(0x7FC00000)
	ldc.r4		1.5
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

//-----------------------------
// -- float32/-inf --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xFF800000)
	ldc.r4		float32(0x3FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/-min --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xFF7FFFFF)
	ldc.r4		float32(0x3FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/-1 --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0xBF800000)
	ldc.r4		float32(0x3F000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/-0 --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x80000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/+0 --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x00000000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/+1 --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x3F800000)
	ldc.r4		float32(0x3F000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/+max --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7F7FFFFF)
	ldc.r4		float32(0x3FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32/+inf --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7F800000)
	ldc.r4		float32(0x3FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32.NaN --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x7FC00000)
	ldc.r4		float32(0x7FC00000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

// -- float32.float32 --
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x3FC00000)
	ldc.r4		float32(0x00000000)
	call	int32 _rem::_rem(float32,float32,float32)
	brfalse		FAIL

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
