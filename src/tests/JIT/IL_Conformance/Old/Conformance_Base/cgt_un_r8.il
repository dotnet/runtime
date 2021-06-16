// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

#define NEG_INF    "float64(0xFFF0000000000000)"
#define MIN        "float64(0xFF7FFFFFFFFFFFFF)"
#define MINUS_ONE  "float64(0xBFF0000000000000)"
#define MINUS_ZERO "float64(0x8000000000000000)"
#define ZERO       "float64(0x0000000000000000)"
#define ONE        "float64(0x3FF0000000000000)"
#define MAX        "float64(0x7FEFFFFFFFFFFFFF)"
#define POS_INF    "float64(0x7FF0000000000000)"
#define NAN        "float64(0x7FF8000000000000)"

.class public cgt_un {

.field public static	float64 neg_inf
.field public static	float64 min
.field public static	float64 minus_one
.field public static	float64 minus_zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 pos_inf
.field public static	float64 NaN

.method public static	void initialize() {
.maxstack	10
	ldc.r8		NEG_INF
	stsfld	float64 cgt_un::neg_inf
	ldc.r8		MIN
	stsfld	float64 cgt_un::min
	ldc.r8		MINUS_ONE
	stsfld	float64 cgt_un::minus_one
	ldc.r8		MINUS_ZERO
	stsfld	float64 cgt_un::minus_zero
	ldc.r8		ZERO
	stsfld	float64 cgt_un::zero
	ldc.r8		ONE
	stsfld	float64 cgt_un::one
	ldc.r8		MAX
	stsfld	float64 cgt_un::max
	ldc.r8		POS_INF
	stsfld	float64 cgt_un::pos_inf
	ldc.r8		NAN
	stsfld	float64 cgt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void cgt_un::initialize()

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::min
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::neg_inf
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL
	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::min
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::min
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL


	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_one
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL
	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::minus_zero
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::zero
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::zero
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::one
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::one
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::max
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::max
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::max
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brtrue		FAIL

	ldsfld	float64 cgt_un::pos_inf
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::neg_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::min
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::minus_one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::minus_zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::zero
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::one
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::max
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::pos_inf
	cgt.un
	brfalse		FAIL

	ldsfld	float64 cgt_un::NaN
	ldsfld	float64 cgt_un::NaN
	cgt.un
	brfalse		FAIL

    // Testing early folding logic

    ldc.r8		NEG_INF
	ldc.r8		NEG_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MIN
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MINUS_ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MINUS_ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL
	ldc.r8		MIN
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		MIN
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL


	ldc.r8		MINUS_ONE
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL
	ldc.r8		MINUS_ZERO
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		MINUS_ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		MINUS_ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		ZERO
	cgt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		MINUS_ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		MINUS_ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		ONE
	cgt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		MINUS_ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		MINUS_ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		MAX
	cgt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		MINUS_ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		MINUS_ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		MAX
	cgt.un
	brfalse		FAIL

	ldc.r8		POS_INF
	ldc.r8		POS_INF
	cgt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		NEG_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MIN
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MINUS_ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MINUS_ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		ZERO
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		ONE
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MAX
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		POS_INF
	cgt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		NAN
	cgt.un
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly cgt_un_r8.exe{}
