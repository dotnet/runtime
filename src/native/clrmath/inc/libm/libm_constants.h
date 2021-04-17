/*
 * Copyright (C) 2018-2020, Advanced Micro Devices, Inc. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software without
 *    specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA,
 * OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 *
 */

#ifndef __LIBM_CONSTANTS_H__
#define __LIBM_CONSTANTS_H__

 /*
  * Double precision - alias bin64,flt64
  */

#define ALM_F64_SIGN_SIZE	1
#define ALM_F64_SIGN_SHIFT	63
#define ALM_F64_SIGN_MASK	(1ULL << ALM_F64_SIGN_SHIFT)

#define ALM_F64_EXPO_SIZE	11
#define ALM_F64_EXPO_SHIFT	52
#define ALM_F64_EXPO_MASK	(((1ULL << ALM_F64_EXPO_SIZE) - 1) << ALM_F64_EXPO_SHIFT)

#define ALM_F64_MANT_SIZE	(52)
#define ALM_F64_MANT_SHIFT	0
#define ALM_F64_MANT_MASK	(((1ULL << ALM_F64_MANT_SIZE) - 1) << ALM_F64_MANT_SHFIT)

#define ALM_F64_EXP_MIN		-1022
#define ALM_F64_EXP_MAX		1023
#define ALM_F64_EXP_BIASMAX	2046
#define ALM_F64_EXP_BIASMIN	1

#define ALM_F64_INF		0x7FF0000000000000ULL
#define ALM_F64_NINF		0xFFF0000000000000ULL
#define ALM_F64_NAN		0x7FF8000000000000ULL

#define ALM_F64_MIN		0x1.0000000000000p-1022
#define ALM_F64_MAX		0x1.fffffffffffffp+1023


  /*
   * Single precision - alias bin32,flt32
   */

#define ALM_F32_SIGN_SIZE	1
#define ALM_F32_SIGN_SHIFT	31
#define ALM_F32_SIGN_MASK	(1ULL << ALM_F32_SIGN_SHIFT)

#define ALM_F32_EXPO_SIZE	8
#define ALM_F32_EXPO_SHIFT	23
#define ALM_F32_EXPO_MASK		(((1ULL << ALM_F32_EXPO_SIZE) - 1) << ALM_F32_EXPO_SHIFT)

#define ALM_F32_MANT_SIZE	23
#define ALM_F32_MANT_SHIFT	0
#define ALM_F32_MANT_MASK	(((1ULL << ALM_F32_MANT_SIZE) - 1) << ALM_F32_MANT_SHFIT)


#define ALM_F32_EXP_MIN		-128
#define ALM_F32_EXP_MAX		127
#define ALM_F32_EXP_BIASMAX	256
#define ALM_F32_EXP_BIASMIN	1

#define ALM_F32_INF		0x7FF00000
#define ALM_F32_INF_MASK		0x7F800000
#define ALM_F32_NINF		0xFFF00000
#define ALM_F32_NAN		0x7FF80000

#define ALM_F32_MIN		0x1.0p-126f
#define ALM_F32_MAX		0x1.fffffep127f


#endif	/* LIBM_CONSTANTS_H */
