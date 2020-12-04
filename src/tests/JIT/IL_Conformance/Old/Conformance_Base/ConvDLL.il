// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//======================================
//
// Name		- ConvDLL
// SDET		- SteveBla
// Created	- 02.15.99
//======================================
//
// Modification History
// Date		Comment
//--------------------------------------
// 00.00.00	Some Comment
//
//
//======================================

.assembly extern mscorlib {}
.assembly ConvDLL{}

//---- CLASS ----------------
.class public auto ansi ConvDLL {
//---- Fields ----------------
.field public	int8			max_i1
.field public	int16			max_i2
.field public	int32			max_i4
.field public	int64			max_i8
.field public	unsigned int8	max_u1
.field public	unsigned int16	max_u2
.field public	unsigned int32	max_u4
.field public	unsigned int64	max_u8
.field public	float32			max_r4
.field public	float64			max_r8

.field public	int8			one_i1
.field public	int16			one_i2
.field public	int32			one_i4
.field public	int64			one_i8
.field public	unsigned int8	one_u1
.field public	unsigned int16	one_u2
.field public	unsigned int32	one_u4
.field public	unsigned int64	one_u8
.field public	float32			one_r4
.field public	float64			one_r8

.field public	int8			zero_i1
.field public	int16			zero_i2
.field public	int32			zero_i4
.field public	int64			zero_i8
.field public	unsigned int8	zero_u1
.field public	unsigned int16	zero_u2
.field public	unsigned int32	zero_u4
.field public	unsigned int64	zero_u8
.field public	float32			zero_r4
.field public	float64			zero_r8

.field public	int8			_one_i1
.field public	int16			_one_i2
.field public	int32			_one_i4
.field public	int64			_one_i8
.field public	float32			_one_r4
.field public	float64			_one_r8

.field public	int8			min_i1
.field public	int16			min_i2
.field public	int32			min_i4
.field public	int64			min_i8
.field public	float32			min_r4
.field public	float64			min_r8

.field public	float32			inf_r4
.field public	float64			inf_r8

.field public	float32			_inf_r4
.field public	float64			_inf_r8

.field public	float32			NaN_r4
.field public	float64			NaN_r8

//---- CONSTRUCTOR ----------
.method public public specialname instance void .ctor() il managed{

.maxstack	2
	ldarg	0
	ldc.i4	0x0000007F
	stfld	int8	ConvDLL::max_i1
	ldarg	0
	ldc.i4	0x00007FFF
	stfld	int16	ConvDLL::max_i2
	ldarg	0
	ldc.i4	0x7FFFFFFF
	stfld	int32	ConvDLL::max_i4
	ldarg	0
	ldc.i8	0x7FFFFFFFFFFFFFFF
	stfld	int64	ConvDLL::max_i8
	ldarg	0
	ldc.i4	0x000000FF
	stfld	unsigned int8	ConvDLL::max_u1
	ldarg	0
	ldc.i4	0x0000FFFF
	stfld	unsigned int16	ConvDLL::max_u2
	ldarg	0
	ldc.i4	0xFFFFFFFF
	stfld	unsigned int32	ConvDLL::max_u4
	ldarg	0
	ldc.i8	0xFFFFFFFFFFFFFFFF
	stfld	unsigned int64	ConvDLL::max_u8
	ldarg	0
	ldc.r4	float32(0x7F7FFFFF)
	stfld	float32	ConvDLL::max_r4
	ldarg	0
	ldc.r8	float64(0x7FEFFFFFFFFFFFFF)
	stfld	float64	ConvDLL::max_r8
	
	ldarg	0
	ldc.i4	0x01
	stfld	int8	ConvDLL::one_i1
	ldarg	0
	ldc.i4	0x0001
	stfld	int16	ConvDLL::one_i2
	ldarg	0
	ldc.i4	0x00000001
	stfld	int32	ConvDLL::one_i4
	ldarg	0
	ldc.i8	0x0000000000000001
	stfld	int64	ConvDLL::one_i8
	ldarg	0
	ldc.i4	0x01
	stfld	unsigned int8	ConvDLL::one_u1
	ldarg	0
	ldc.i4	0x0001
	stfld	unsigned int16	ConvDLL::one_u2
	ldarg	0
	ldc.i4	0x00000001
	stfld	unsigned int32	ConvDLL::one_u4
	ldarg	0
	ldc.i8	0x0000000000000001
	stfld	unsigned int64	ConvDLL::one_u8
	ldarg	0
	ldc.r4	float32(0x3F800000)
	stfld	float32	ConvDLL::one_r4
	ldarg	0
	ldc.r8	float64(0x3FF0000000000000)
	stfld	float64	ConvDLL::one_r8

	ldarg	0
	ldc.i4	0x00
	stfld	int8	ConvDLL::zero_i1
	ldarg	0
	ldc.i4	0x0000
	stfld	int16	ConvDLL::zero_i2
	ldarg	0
	ldc.i4	0x00000000
	stfld	int32	ConvDLL::zero_i4
	ldarg	0
	ldc.i8	0x0000000000000000
	stfld	int64	ConvDLL::zero_i8
	ldarg	0
	ldc.i4	0x00
	stfld	unsigned int8	ConvDLL::zero_u1
	ldarg	0
	ldc.i4	0x0000
	stfld	unsigned int16	ConvDLL::zero_u2
	ldarg	0
	ldc.i4	0x00000000
	stfld	unsigned int32	ConvDLL::zero_u4
	ldarg	0
	ldc.i8	0x0000000000000000
	stfld	unsigned int64	ConvDLL::zero_u8
	ldarg	0
	ldc.r4	0
	stfld	float32	ConvDLL::zero_r4
	ldarg	0
	ldc.r8	0
	stfld	float64	ConvDLL::zero_r8

	ldarg	0
	ldc.i4	0xFF
	stfld	int8	ConvDLL::_one_i1
	ldarg	0
	ldc.i4	0xFFFF
	stfld	int16	ConvDLL::_one_i2
	ldarg	0
	ldc.i4	0xFFFFFFFF
	stfld	int32	ConvDLL::_one_i4
	ldarg	0
	ldc.i8	0xFFFFFFFFFFFFFFFF
	stfld	int64	ConvDLL::_one_i8
	ldarg	0
	ldc.r4	-1
	stfld	float32	ConvDLL::_one_r4
	ldarg	0
	ldc.r8	-1
	stfld	float64	ConvDLL::_one_r8

	ldarg	0
	ldc.i4	0x80
	stfld	int8	ConvDLL::min_i1
	ldarg	0
	ldc.i4	0x8000
	stfld	int16	ConvDLL::min_i2
	ldarg	0
	ldc.i4	0x80000000
	stfld	int32	ConvDLL::min_i4
	ldarg	0
	ldc.i8	0x8000000000000000
	stfld	int64	ConvDLL::min_i8
	ldarg	0
	ldc.r4	float32(0xFF7FFFFF)
	stfld	float32	ConvDLL::min_r4
	ldarg	0
	ldc.r8	float64(0xFFEFFFFFFFFFFFFF)
	stfld	float64	ConvDLL::min_r8

	ldarg	0
	ldc.r4	float32(0xFF800000)
	stfld	float32	ConvDLL::_inf_r4
	ldarg	0
	ldc.r8	float64(0xFFF0000000000000)
	stfld	float64	ConvDLL::_inf_r8

	ldarg	0
	ldc.r4	float32(0x7F800000)
	stfld	float32	ConvDLL::inf_r4
	ldarg	0
	ldc.r8	float64(0x7FF0000000000000)
	stfld	float64	ConvDLL::inf_r8

	ldarg	0
	ldc.r4	float32(0x7FC00000)
	stfld	float32	ConvDLL::NaN_r4
	ldarg	0
	ldc.r8	float64(0x7FF8000000000000)
	stfld	float64	ConvDLL::NaN_r8

	ret
}
//---- EOF ------------------
}

