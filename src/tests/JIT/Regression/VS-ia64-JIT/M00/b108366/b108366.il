// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


.assembly extern legacy library mscorlib {}
.assembly extern xunit.core {}
.assembly extern System.Console
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )
  .ver 4:0:0:0
}
.assembly 'b108366'{}

.class public _div {
//-- 
.method public static	int32 _div(float64,float64,float64) {
.maxstack		3
	// -- load args _and divide
	ldarg	0
	conv.r8
	ldarg	1
	conv.r8
	div
	// -- dup so the result remains after _call to checkfinite
	dup
	// -- see if our number is finite (_ret val of 0 is finite)
	call	int32 _div::checkfinite(float64)
	ldc.i4		0x0
	ceq
	// -- If the result is finite, branch
	brtrue		COMPARE
	// -- our result was _not finite
	// -- call checkfinite again on the duped result
	// -- should return 1 fo -inf, 2 for +inf, 3 for NaN _and 255 for error
	call	int32 _div::checkfinite(float64)
	// -- call checkfinite on our expected value.
	ldarg	2
	conv.r8
	call	int32 _div::checkfinite(float64)
	ceq
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
.method public static	int32 checkfinite(float64) {
.locals		(class [mscorlib]System.Exception)
.maxstack		3
// -- 0 if finite
// -- 1 for NaN
// -- 2 for -inf
// -- 3 for +inf
// -- none of the above... return 255 (0xFF)
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
	isinst		[mscorlib]System.OverflowException
	stloc	0
	leave			HEnd
HEnd:

	ldloc	0
	brfalse		FAIL
	// -- check for -inf
	ldarg	0
	conv.r8
	ldc.r8		float64(0xFFF0000000000000)
	ceq
	brtrue		NINF
	// -- check for +inf
	ldarg	0
	conv.r8
	ldc.r8		float64(0x7FF0000000000000)
	ceq
	brtrue		PINF
	// -- must be NaN
	br			NaN
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
	ldc.i4		0xFF
	br			FINISH
FINISH:
	ret
.try try_start to try_end catch [mscorlib]System.OverflowException handler ae to HEnd
}

// -- Begin Main --------------------
.method public static	int32 main() {
.custom instance void [xunit.core]Xunit.FactAttribute::.ctor() = (
    01 00 00 00
)
.entrypoint
.maxstack		20
br START
// -- -inf/-inf --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf/-min --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf/-1 --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf/-0 --
br L1
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L1:
// -- -inf/+0 --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf/+1 --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf/+max --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf/+inf --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf.NaN --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -inf.float --
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- -min/-inf --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min/-min --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min/-1 --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min/-0 --
br L2
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L2:
// -- -min/+0 --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min/+1 --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min/+max --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min/+inf --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min.NaN --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -min.float --
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- -1/-inf --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1/-min --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1/-1 --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1/-0 --
br L3
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L3:
// -- -1/+0 --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1/+1 --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1/+max --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1/+inf --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1.NaN --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -1.float --
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- -0/-inf --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0/-min --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0/-1 --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0/-0 --
br L4
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L4:
// -- -0/+0 --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0/+1 --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0/+max --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0/+inf --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0.NaN --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- -0.float --
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- +0/-inf --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0/-min --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0/-1 --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0/-0 --
br L5
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L5:
// -- +0/+0 --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0/+1 --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0/+max --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0/+inf --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0.NaN --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +0.float --
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- +1/-inf --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1/-min --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1/-1 --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1/-0 --
br L6
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L6:
// -- +1/+0 --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1/+1 --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1/+max --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0004000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1/+inf --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1.NaN --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +1.float --
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- +max/-inf --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max/-min --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max/-1 --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max/-0 --
br L7
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L7:
// -- +max/+0 --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max/+1 --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max/+max --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max/+inf --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max.NaN --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +max.float --
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FE5555555555555)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- +inf/-inf --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf/-min --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf/-1 --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf/-0 --
br L8
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L8:
// -- +inf/+0 --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf/+1 --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf/+max --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf/+inf --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf.NaN --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- +inf.float --
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

// -- NaN/-inf --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN/-min --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN/-1 --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN/-0 --
br L9
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L9:
// -- NaN/+0 --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN/+1 --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN/+max --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN/+inf --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN.NaN --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- NaN.float --
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
br ENDTESTS

START:
// -- float/-inf --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float/-min --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xFFEFFFFFFFFFFFFF)
	ldc.r8		float64(0x8006000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float/-1 --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0xBFF0000000000000)
	ldc.r8		float64(0xBFF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float/-0 --
br L10
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x8000000000000000)
	ldc.r8		float64(0xFFF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
L10:
// -- float/+0 --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x0000000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float/+1 --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float/+max --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FEFFFFFFFFFFFFF)
	ldc.r8		float64(0x0006000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float/+inf --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF0000000000000)
	ldc.r8		float64(0x0000000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float.NaN --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	ldc.r8		float64(0x7FF8000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
// -- float.float --
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF8000000000000)
	ldc.r8		float64(0x3FF0000000000000)
	call	int32 _div::_div(float64,float64,float64)
	brfalse		FAIL
ENDTESTS:

// -- PASS --
	ldc.i4	100
	ret
// -- FAIL --
FAIL:
	ldc.i4	0x0
	ret
// -- End Main ----------------------
}
//
// -- EOF ---------------------------
}
// ----------------------------------
