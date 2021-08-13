// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

.assembly extern legacy library mscorlib {}

#define NEG_INF    "float32(0xFF800000)"
#define MIN        "float32(0xFF7FFFFF)"
#define MINUS_ONE  "float32(0xBF800000)"
#define MINUS_ZERO "float32(0x80000000)"
#define ZERO       "float32(0x00000000)"
#define ONE        "float32(0x3F800000)"
#define MAX        "float32(0x7F7FFFFF)"
#define POS_INF    "float32(0x7F800000)"
#define NAN        "float32(0x7FC00000)"

.class public clt_un {

.field public static	float32 neg_inf
.field public static	float32 min
.field public static	float32 minus_one
.field public static	float32 minus_zero
.field public static	float32 zero
.field public static	float32 one
.field public static	float32 max
.field public static	float32 pos_inf
.field public static	float32 NaN
.method public static void initialize() {
.maxstack		10
	ldc.r4		NEG_INF
	stsfld	float32 clt_un::neg_inf
	ldc.r4		MIN
	stsfld	float32 clt_un::min
	ldc.r4		MINUS_ONE
	stsfld	float32 clt_un::minus_one
	ldc.r4		MINUS_ZERO
	stsfld	float32 clt_un::minus_zero
	ldc.r4		ZERO
	stsfld	float32 clt_un::zero
	ldc.r4		ONE
	stsfld	float32 clt_un::one
	ldc.r4		MAX
	stsfld	float32 clt_un::max
	ldc.r4		POS_INF
	stsfld	float32 clt_un::pos_inf
	ldc.r4		NAN
	stsfld	float32 clt_un::NaN
	ret
}

.method public static	int32 main(class [mscorlib]System.String[]) {
.entrypoint
.maxstack		10
	call	void clt_un::initialize()
	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::min
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::minus_one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::neg_inf
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::minus_one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::min
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL


	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_one
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL
	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::minus_zero
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::zero
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::one
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::max
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::max
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::min
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::minus_one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::zero
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::one
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::max
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brtrue		FAIL

	ldsfld	float32 clt_un::pos_inf
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::neg_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::min
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::minus_one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::minus_zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::zero
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::one
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::max
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::pos_inf
	clt.un
	brfalse		FAIL

	ldsfld	float32 clt_un::NaN
	ldsfld	float32 clt_un::NaN
	clt.un
	brfalse		FAIL

    // Testing early folding logic

    ldc.r4		NEG_INF
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		NEG_INF
	ldc.r4		MIN
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		MINUS_ONE
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		ONE
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		NEG_INF
	ldc.r4		NAN
	clt.un
	brfalse		FAIL
	ldc.r4		MIN
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		MIN
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		MIN
	ldc.r4		MINUS_ONE
	clt.un
	brfalse		FAIL

	ldc.r4		MIN
	ldc.r4		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		MIN
	ldc.r4		ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		MIN
	ldc.r4		ONE
	clt.un
	brfalse		FAIL

	ldc.r4		MIN
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		MIN
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		MIN
	ldc.r4		NAN
	clt.un
	brfalse		FAIL


	ldc.r4		MINUS_ONE
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		ONE
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ONE
	ldc.r4		NAN
	clt.un
	brfalse		FAIL
	ldc.r4		MINUS_ZERO
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		ONE
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		MINUS_ZERO
	ldc.r4		NAN
	clt.un
	brfalse		FAIL

	ldc.r4		ZERO
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		ZERO
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		ZERO
	ldc.r4		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r4		ZERO
	ldc.r4		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		ZERO
	ldc.r4		ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		ZERO
	ldc.r4		ONE
	clt.un
	brfalse		FAIL

	ldc.r4		ZERO
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		ZERO
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		ZERO
	ldc.r4		NAN
	clt.un
	brfalse		FAIL

	ldc.r4		ONE
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		ONE
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		ONE
	ldc.r4		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r4		ONE
	ldc.r4		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		ONE
	ldc.r4		ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		ONE
	ldc.r4		ONE
	clt.un
	brtrue		FAIL

	ldc.r4		ONE
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		ONE
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		ONE
	ldc.r4		NAN
	clt.un
	brfalse		FAIL

	ldc.r4		MAX
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		ONE
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		MAX
	clt.un
	brtrue		FAIL

	ldc.r4		MAX
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		MAX
	ldc.r4		NAN
	clt.un
	brfalse		FAIL

	ldc.r4		POS_INF
	ldc.r4		NEG_INF
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		MIN
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		MINUS_ONE
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		MINUS_ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		ZERO
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		ONE
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		MAX
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		POS_INF
	clt.un
	brtrue		FAIL

	ldc.r4		POS_INF
	ldc.r4		NAN
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		NEG_INF
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		MIN
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		MINUS_ONE
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		MINUS_ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		ZERO
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		ONE
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		MAX
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		POS_INF
	clt.un
	brfalse		FAIL

	ldc.r4		NAN
	ldc.r4		NAN
	clt.un
	brfalse		FAIL

	ldc.i4 100
	ret
FAIL:
	ldc.i4	0x0
	ret
}
}
.assembly clt_un_r4.exe{}
