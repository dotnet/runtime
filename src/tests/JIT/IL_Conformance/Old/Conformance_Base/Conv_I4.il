// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

.assembly extern legacy library mscorlib {}


.method public	static int32 main(class [mscorlib]System.String[]) {
.entrypoint
.locals	(class CConv,int32)
.maxstack	2
	newobj	instance void CConv::.ctor()
	stloc	0
	ldc.i4	0x0
	stloc	1
	ldc.i4	1
	stloc	1
	ldloc	0
	ldfld	int32	CConv::max_i4
	conv.i1
	ldc.i4	0xFFFFFFFF
	bne.un	FAIL
	ldc.i4	2
	stloc	1
	ldloc	0
	ldfld	int32	CConv::max_i4
	conv.i2
	ldc.i4	0xFFFFFFFF
	bne.un	FAIL
        ldc.i4  3
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.i4
        ldc.i4  0x7FFFFFFF
        bne.un  FAIL
        ldc.i4  4
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.i8
        ldc.i8  0x000000007FFFFFFF
        bne.un  FAIL
	ldc.i4	5
	stloc	1
	ldloc	0
	ldfld	int32	CConv::max_i4
	conv.u1
	ldc.i4	0xFF
	bne.un	FAIL
	ldc.i4	6
	stloc	1
	ldloc	0
	ldfld	int32	CConv::max_i4
	conv.u2
	ldc.i4	0xFFFF
	bne.un	FAIL
        ldc.i4  7
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.u4
        ldc.i4  0x7FFFFFFF
        bne.un  FAIL
        ldc.i4  8
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.u8
        ldc.i8  0x000000007FFFFFFF
        bne.un  FAIL

	ldc.i4	9
	stloc	1
	ldloc	0
	ldfld	int32	CConv::max_i4
	conv.r4
	ldc.r4	2147483649
	bne.un	FAIL
        ldc.i4  10
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.r8
        ldc.r8  2147483647
        bne.un  FAIL

	ldc.i4	11
	stloc	1
	ldloc	0
	ldfld	int32	CConv::max_i4
	conv.i
	ldc.i4	0x7FFFFFFF
	bne.un	FAIL
        ldc.i4  12
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.u
        ldc.i4  0x7FFFFFFF
        bne.un  FAIL

        ldc.i4  13
        stloc   1
        ldloc   0
        ldfld   int32   CConv::max_i4
        conv.r.un
        ldc.r8  2147483647
        bne.un  FAIL
	ldc.i4	100
	stloc	1
	ldloc	0
	ldfld	int32	CConv::one_i4
	conv.i1
	ldc.i4	0x01
	bne.un	FAIL
	ldc.i4	101
	stloc	1
	ldloc	0
	ldfld	int32	CConv::one_i4
	conv.i2
	ldc.i4	0x0001
	bne.un	FAIL
        ldc.i4  102
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.i4
        ldc.i4  0x00000001
        bne.un  FAIL
        ldc.i4  103
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.i8
        ldc.i8  0x0000000000000001
        bne.un  FAIL
	ldc.i4	104
	stloc	1
	ldloc	0
	ldfld	int32	CConv::one_i4
	conv.u1
	ldc.i4	0x01
	bne.un	FAIL
	ldc.i4	105
	stloc	1
	ldloc	0
	ldfld	int32	CConv::one_i4
	conv.u2
	ldc.i4	0x0001
	bne.un	FAIL
        ldc.i4  106
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.u4
        ldc.i4  0x00000001
        bne.un  FAIL
        ldc.i4  107
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.u8
        ldc.i8  0x0000000000000001
        bne.un  FAIL

	ldc.i4	108
	stloc	1
	ldloc	0
	ldfld	int32	CConv::one_i4
	conv.r4
	ldc.r4	1
	bne.un	FAIL
        ldc.i4  109
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.r8
        ldc.r8  1
        bne.un  FAIL
        ldc.i4  110
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.i
        ldc.i4  0x00000001
        bne.un  FAIL
        ldc.i4  111
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.u
        ldc.i4  0x00000001
        bne.un  FAIL
        ldc.i4  112
        stloc   1
        ldloc   0
        ldfld   int32   CConv::one_i4
        conv.r.un
        ldc.r8  1
        bne.un  FAIL
	ldc.i4	200
	stloc	1
	ldloc	0
	ldfld	int32	CConv::zero_i4
	conv.i1
	ldc.i4	0x00
	bne.un	FAIL
	ldc.i4	201
	stloc	1
	ldloc	0
	ldfld	int32	CConv::zero_i4
	conv.i2
	ldc.i4	0x0000
	bne.un	FAIL
        ldc.i4  202
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.i4
        ldc.i4  0x0000000
        bne.un  FAIL
        ldc.i4  203
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.i8
        ldc.i8  0x000000000000000
        bne.un  FAIL
	ldc.i4	204
	stloc	1
	ldloc	0
	ldfld	int32	CConv::zero_i4
	conv.u1
	ldc.i4	0x00
	bne.un	FAIL
	ldc.i4	205
	stloc	1
	ldloc	0
	ldfld	int32	CConv::zero_i4
	conv.u2
	ldc.i4	0x0000
	bne.un	FAIL
        ldc.i4  206
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.u4
        ldc.i4  0x00000000
        bne.un  FAIL
        ldc.i4  207
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.u8
        ldc.i8  0x0000000000000000
        bne.un  FAIL
	ldc.i4	208
	stloc	1
	ldloc	0
	ldfld	int32	CConv::zero_i4
	conv.r4
	ldc.r4	0
	bne.un	FAIL
        ldc.i4  209
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.r8
        ldc.r8  0
        bne.un  FAIL
        ldc.i4  210
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.i
        ldc.i4  0x00000000
        bne.un  FAIL
        ldc.i4  211
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.u
        ldc.i4  0x00000000
        bne.un  FAIL
        ldc.i4  212
        stloc   1
        ldloc   0
        ldfld   int32   CConv::zero_i4
        conv.r.un
        ldc.r8  0
        bne.un  FAIL
	ldc.i4	300
	stloc	1
	ldloc	0
	ldfld	int32	CConv::_one_i4
	conv.i1
	ldc.i4	0xFFFFFFFF
	bne.un	FAIL
	ldc.i4	301
	stloc	1
	ldloc	0
	ldfld	int32	CConv::_one_i4
	conv.i2
	ldc.i4	0xFFFFFFFF
	bne.un	FAIL
        ldc.i4  302
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.i4
        ldc.i4  0xFFFFFFFF
        bne.un  FAIL
        ldc.i4  303
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.i8
        ldc.i8  0xFFFFFFFFFFFFFFFF
        bne.un  FAIL
	ldc.i4	304
	stloc	1
	ldloc	0
	ldfld	int32	CConv::_one_i4
	conv.u1
	ldc.i4	0x000000FF
	bne.un	FAIL
	ldc.i4	305
	stloc	1
	ldloc	0
	ldfld	int32	CConv::_one_i4
	conv.u2
	ldc.i4	0x0000FFFF
	bne.un	FAIL
        ldc.i4  306
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.u4
        ldc.i4  0xFFFFFFFF
        bne.un  FAIL
        ldc.i4  307
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.u8
        ldc.i8  0x00000000FFFFFFFF
        bne.un  FAIL
	ldc.i4	308
	stloc	1
	ldloc	0
	ldfld	int32	CConv::_one_i4
	conv.r4
	ldc.r4	float32(0xBF800000)
	bne.un	FAIL
        ldc.i4  309
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.r8
        ldc.r8  float64(0xBFF0000000000000)
        bne.un  FAIL
        ldc.i4  310
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.i
        ldc.i4  0xFFFFFFFF
        conv.i
        bne.un  FAIL
        ldc.i4  311
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.u
        ldc.i4  0xFFFFFFFF
        conv.u
        bne.un  FAIL
        ldc.i4  312
        stloc   1
        ldloc   0
        ldfld   int32   CConv::_one_i4
        conv.r.un
        ldc.r8  4294967295
        bne.un  FAIL
	ldc.i4	400
	stloc	1
	ldloc	0
	ldfld	int32	CConv::min_i4
	conv.i1
	ldc.i4	0x00
	bne.un	FAIL
	ldc.i4	401
	stloc	1
	ldloc	0
	ldfld	int32	CConv::min_i4
	conv.i2
	ldc.i4	0x0000
	bne.un	FAIL
        ldc.i4  402
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.i4
        ldc.i4  0x80000000
        bne.un  FAIL
        ldc.i4  403
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.i8
        ldc.i8  0xFFFFFFFF80000000
        bne.un  FAIL
	ldc.i4	404
	stloc	1
	ldloc	0
	ldfld	int32	CConv::min_i4
	conv.u1
	ldc.i4	0x00
	bne.un	FAIL
	ldc.i4	405
	stloc	1
	ldloc	0
	ldfld	int32	CConv::min_i4
	conv.u2
	ldc.i4	0x0000
	bne.un	FAIL
        ldc.i4  406
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.u4
        ldc.i4  0x80000000
        bne.un  FAIL
        ldc.i4  407
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.u8
        ldc.i8  0x0000000080000000
        bne.un  FAIL
	ldc.i4	408
	stloc	1
	ldloc	0
	ldfld	int32	CConv::min_i4
	conv.r4
	ldc.r4	-2147483648
	bne.un	FAIL
        ldc.i4  409
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.r8
        ldc.r8  -2147483648
        bne.un  FAIL
        ldc.i4  410
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.i
        ldc.i4  0x80000000
        conv.i
        bne.un  FAIL
        ldc.i4  411
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.i
        ldc.i4  0x80000000
        conv.i
        bne.un  FAIL
        ldc.i4  412
        stloc   1
        ldloc   0
        ldfld   int32   CConv::min_i4
        conv.r.un
        ldc.r8  2147483648
        bne.un  FAIL
PASS:
	ldc.i4 100
	br		END
FAIL:
	ldloc	1
	br		END
END:
	ret
}



.class public		CConv {
.field public  int8                    max_i1
.field public  int16                   max_i2
.field public  int32                   max_i4
.field public  int64                   max_i8
.field public  unsigned int8   max_u1
.field public  unsigned int16  max_u2
.field public  unsigned int32  max_u4
.field public  unsigned int64  max_u8
.field public  float32                 max_r4
.field public  float64                 max_r8
.field public  int8                    one_i1
.field public  int16                   one_i2
.field public  int32                   one_i4
.field public  int64                   one_i8
.field public  unsigned int8   one_u1
.field public  unsigned int16  one_u2
.field public  unsigned int32  one_u4
.field public  unsigned int64  one_u8
.field public  float32                 one_r4
.field public  float64                 one_r8

.field public  int8                    zero_i1
.field public  int16                   zero_i2
.field public  int32                   zero_i4
.field public  int64                   zero_i8
.field public  unsigned int8   zero_u1
.field public  unsigned int16  zero_u2
.field public  unsigned int32  zero_u4
.field public  unsigned int64  zero_u8
.field public  float32                 zero_r4
.field public  float64                 zero_r8

.field public  int8                    _one_i1
.field public  int16                   _one_i2
.field public  int32                   _one_i4
.field public  int64                   _one_i8
.field public  float32                 _one_r4
.field public  float64                 _one_r8

.field public  int8                    min_i1
.field public  int16                   min_i2
.field public  int32                   min_i4
.field public  int64                   min_i8
.field public  float32                 min_r4
.field public  float64                 min_r8

.field public  float32                 inf_r4
.field public  float64                 inf_r8

.field public  float32                 _inf_r4
.field public  float64                 _inf_r8

.field public  float32                 NaN_r4
.field public  float64                 NaN_r8

.method public void .ctor() {

.maxstack       2
        ldarg   0
        ldc.i4  0x0000007F
        stfld   int8    CConv::max_i1
        ldarg   0
        ldc.i4  0x00007FFF
        stfld   int16   CConv::max_i2
        ldarg   0
        ldc.i4  0x7FFFFFFF
        stfld   int32   CConv::max_i4
        ldarg   0
        ldc.i8  0x7FFFFFFFFFFFFFFF
        stfld   int64   CConv::max_i8
        ldarg   0
        ldc.i4  0x000000FF
        stfld   unsigned int8   CConv::max_u1
        ldarg   0
        ldc.i4  0x0000FFFF
        stfld   unsigned int16  CConv::max_u2
        ldarg   0
        ldc.i4  0xFFFFFFFF
        stfld   unsigned int32  CConv::max_u4
        ldarg   0
        ldc.i8  0xFFFFFFFFFFFFFFFF
        stfld   unsigned int64  CConv::max_u8
        ldarg   0
        ldc.r4  float32(0x7F7FFFFF)
        stfld   float32 CConv::max_r4
        ldarg   0
        ldc.r8  float64(0x7FEFFFFFFFFFFFFF)
        stfld   float64 CConv::max_r8

        ldarg   0
        ldc.i4  0x01
        stfld   int8    CConv::one_i1
        ldarg   0
        ldc.i4  0x0001
        stfld   int16   CConv::one_i2
        ldarg   0
        ldc.i4  0x00000001
        stfld   int32   CConv::one_i4
        ldarg   0
        ldc.i8  0x0000000000000001
        stfld   int64   CConv::one_i8
        ldarg   0
        ldc.i4  0x01
        stfld   unsigned int8   CConv::one_u1
        ldarg   0
        ldc.i4  0x0001
        stfld   unsigned int16  CConv::one_u2
        ldarg   0
        ldc.i4  0x00000001
        stfld   unsigned int32  CConv::one_u4
        ldarg   0
        ldc.i8  0x0000000000000001
        stfld   unsigned int64  CConv::one_u8
        ldarg   0
        ldc.r4  float32(0x3F800000)
        stfld   float32 CConv::one_r4
        ldarg   0
        ldc.r8  float64(0x3FF0000000000000)
        stfld   float64 CConv::one_r8
        ldarg   0
        ldc.i4  0x00
        stfld   int8    CConv::zero_i1
        ldarg   0
        ldc.i4  0x0000
        stfld   int16   CConv::zero_i2
        ldarg   0
        ldc.i4  0x00000000
        stfld   int32   CConv::zero_i4
        ldarg   0
        ldc.i8  0x0000000000000000
        stfld   int64   CConv::zero_i8
        ldarg   0
        ldc.i4  0x00
        stfld   unsigned int8   CConv::zero_u1
        ldarg   0
        ldc.i4  0x0000
        stfld   unsigned int16  CConv::zero_u2
        ldarg   0
        ldc.i4  0x00000000
        stfld   unsigned int32  CConv::zero_u4
        ldarg   0
        ldc.i8  0x0000000000000000
        stfld   unsigned int64  CConv::zero_u8
        ldarg   0
        ldc.r4  0
        stfld   float32 CConv::zero_r4
        ldarg   0
        ldc.r8  0
        stfld   float64 CConv::zero_r8

        ldarg   0
        ldc.i4  0xFF
        stfld   int8    CConv::_one_i1
        ldarg   0
        ldc.i4  0xFFFF
        stfld   int16   CConv::_one_i2
        ldarg   0
        ldc.i4  0xFFFFFFFF
        stfld   int32   CConv::_one_i4
        ldarg   0
        ldc.i8  0xFFFFFFFFFFFFFFFF
        stfld   int64   CConv::_one_i8
        ldarg   0
        ldc.r4  -1
        stfld   float32 CConv::_one_r4
        ldarg   0
        ldc.r8  -1
        stfld   float64 CConv::_one_r8

        ldarg   0
        ldc.i4  0x80
        stfld   int8    CConv::min_i1
        ldarg   0
        ldc.i4  0x8000
        stfld   int16   CConv::min_i2
        ldarg   0
        ldc.i4  0x80000000
        stfld   int32   CConv::min_i4
        ldarg   0
        ldc.i8  0x8000000000000000
        stfld   int64   CConv::min_i8

        ldarg   0
        ldc.r4  float32(0xFF7FFFFF)
        stfld   float32 CConv::min_r4
        ldarg   0
        ldc.r8  float64(0xFFEFFFFFFFFFFFFF)
        stfld   float64 CConv::min_r8

        ldarg   0
        ldc.r4  float32(0xFF800000)
        stfld   float32 CConv::_inf_r4
        ldarg   0
        ldc.r8  float64(0xFFF0000000000000)
        stfld   float64 CConv::_inf_r8

        ldarg   0
        ldc.r4  float32(0x7F800000)
        stfld   float32 CConv::inf_r4
        ldarg   0
        ldc.r8  float64(0x7FF0000000000000)
        stfld   float64 CConv::inf_r8

        ldarg   0
        ldc.r4  float32(0x7FC00000)
        stfld   float32 CConv::NaN_r4
        ldarg   0
        ldc.r8  float64(0x7FF8000000000000)
        stfld   float64 CConv::NaN_r8
        ret
}
}

.assembly Conv_I4.exe{}
