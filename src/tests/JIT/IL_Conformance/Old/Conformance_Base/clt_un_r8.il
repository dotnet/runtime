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

.class public clt_un {

.field public static	float64 neg_inf
.field public static	float64 min
.field public static	float64 minus_one
.field public static	float64 minus_zero
.field public static	float64 zero
.field public static	float64 one
.field public static	float64 max
.field public static	float64 pos_inf
.field public static	float64 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r8		NEG_INF
	stsfld	float64 clt_un::neg_inf
	ldc.r8		MIN
	stsfld	float64 clt_un::min
	ldc.r8		MINUS_ONE
	stsfld	float64 clt_un::minus_one
	ldc.r8		MINUS_ZERO
	stsfld	float64 clt_un::minus_zero
	ldc.r8		ZERO
	stsfld	float64 clt_un::zero
	ldc.r8		ONE
	stsfld	float64 clt_un::one
	ldc.r8		MAX
	stsfld	float64 clt_un::max
	ldc.r8		POS_INF
	stsfld	float64 clt_un::pos_inf
	ldc.r8		NAN
	stsfld	float64 clt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void clt_un::initialize()
	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::min
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::minus_one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::neg_inf
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::minus_one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::min
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_one
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::minus_zero
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::zero
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::one
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::max
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::max
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse	FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::max
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brtrue		FAIL

	ldsfld	float64 clt_un::pos_inf
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::neg_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::min
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::minus_one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float64 clt_un::NaN
	ldsfld	float64 clt_un::NaN
	clt.un
	brfalse		FAIL

    // Testing early folding logic

    ldc.r8		NEG_INF
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MIN
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MINUS_ONE
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		ONE
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		NEG_INF
	ldc.r8		NAN
	clt.un
	brfalse		FAIL
	ldc.r8		MIN
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ONE
	clt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		ONE
	clt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		MIN
	ldc.r8		NAN
	clt.un
	brfalse		FAIL
	ldc.r8		MINUS_ONE
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		ONE
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ONE
	ldc.r8		NAN
	clt.un
	brfalse		FAIL
	ldc.r8		MINUS_ZERO
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		ONE
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		MINUS_ZERO
	ldc.r8		NAN
	clt.un
	brfalse		FAIL
	ldc.r8		ZERO
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		ZERO
	ldc.r8		ONE
	clt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		ZERO
	ldc.r8		NAN
	clt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		ONE
	clt.un
	brtrue		FAIL

	ldc.r8		ONE
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		ONE
	ldc.r8		NAN
	clt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		ONE
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		MAX
	clt.un
	brtrue		FAIL

	ldc.r8		MAX
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		MAX
	ldc.r8		NAN
	clt.un
	brfalse	FAIL

	ldc.r8		POS_INF
	ldc.r8		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		MIN
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		ZERO
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		ONE
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		MAX
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		POS_INF
	clt.un
	brtrue		FAIL

	ldc.r8		POS_INF
	ldc.r8		NAN
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		NEG_INF
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MIN
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MINUS_ONE
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		ZERO
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		ONE
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		MAX
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r8		NAN
	ldc.r8		NAN
	clt.un
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_un_r8.exe{}
