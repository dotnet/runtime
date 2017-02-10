/**
 * \file
 * Copyright (c) Microsoft. All rights reserved.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 *
 * Copyright 2015 Xamarin Inc
 *
 * File: decimal.c
 *
 * Ported from C++ to C and adjusted to Mono runtime
 *
 * Pending:
 *   DoToCurrency (they look like new methods we do not have)
 */
#ifndef DISABLE_DECIMAL
#include "config.h"
#include <stdint.h>
#include <glib.h>
#include <mono/utils/mono-compiler.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/object-internals.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#ifdef HAVE_MEMORY_H
#include <memory.h>
#endif
#ifdef _MSC_VER
#include <intrin.h>
#endif
#include "decimal-ms.h"
#include "number-ms.h"

#define min(a, b) (((a) < (b)) ? (a) : (b))

typedef enum {
	MONO_DECIMAL_OK,
	MONO_DECIMAL_OVERFLOW,
	MONO_DECIMAL_INVALID_ARGUMENT,
	MONO_DECIMAL_DIVBYZERO,
	MONO_DECIMAL_ARGUMENT_OUT_OF_RANGE
} MonoDecimalStatus;

#ifndef FC_GC_POLL
#   define FC_GC_POLL() 
#endif

static const uint32_t ten_to_nine    = 1000000000U;
static const uint32_t ten_to_ten_div_4 = 2500000000U;
#define POWER10_MAX     9
#define DECIMAL_NEG ((uint8_t)0x80)
#define DECMAX 28
#define DECIMAL_SCALE(dec)       ((dec).u.u.scale)
#define DECIMAL_SIGN(dec)        ((dec).u.u.sign)
#define DECIMAL_SIGNSCALE(dec)   ((dec).u.signscale)
#define DECIMAL_LO32(dec)        ((dec).v.v.Lo32)
#define DECIMAL_MID32(dec)       ((dec).v.v.Mid32)
#define DECIMAL_HI32(dec)        ((dec).Hi32)
#if G_BYTE_ORDER != G_LITTLE_ENDIAN
# define DECIMAL_LO64_GET(dec)   (((uint64_t)((dec).v.v.Mid32) << 32) | (dec).v.v.Lo32)
# define DECIMAL_LO64_SET(dec,value)   {(dec).v.v.Lo32 = (value); (dec).v.v.Mid32 = ((value) >> 32); }
#else
# define DECIMAL_LO64_GET(dec)    ((dec).v.Lo64)
# define DECIMAL_LO64_SET(dec,value)   {(dec).v.Lo64 = value; }
#endif

#define DECIMAL_SETZERO(dec) {DECIMAL_LO32(dec) = 0; DECIMAL_MID32(dec) = 0; DECIMAL_HI32(dec) = 0; DECIMAL_SIGNSCALE(dec) = 0;}
#define COPYDEC(dest, src) {DECIMAL_SIGNSCALE(dest) = DECIMAL_SIGNSCALE(src); DECIMAL_HI32(dest) = DECIMAL_HI32(src); \
    DECIMAL_MID32(dest) = DECIMAL_MID32(src); DECIMAL_LO32(dest) = DECIMAL_LO32(src); }

#define DEC_SCALE_MAX   28
#define POWER10_MAX     9

#define OVFL_MAX_9_HI   4
#define OVFL_MAX_9_MID  1266874889
#define OVFL_MAX_9_LO   3047500985u

#define OVFL_MAX_5_HI   42949
#define OVFL_MAX_5_MID  2890341191

#define OVFL_MAX_1_HI   429496729

typedef union {
	uint64_t int64;
	struct {
#if BYTE_ORDER == G_BIG_ENDIAN
        uint32_t Hi;
        uint32_t Lo;
#else
        uint32_t Lo;
        uint32_t Hi;
#endif
    } u;
} SPLIT64;

static const SPLIT64    ten_to_eighteen = { 1000000000000000000ULL };

const MonoDouble_double ds2to64 = { .s = { .sign = 0, .exp = MONO_DOUBLE_BIAS + 65, .mantHi = 0, .mantLo = 0 } };

//
// Data tables
//

static const uint32_t power10 [POWER10_MAX+1] = {
	1, 10, 100, 1000, 10000, 100000, 1000000, 10000000, 100000000, 1000000000
};


static const double double_power10[] = {
	1, 1e1, 1e2, 1e3, 1e4, 1e5, 1e6, 1e7, 1e8, 1e9, 
	1e10, 1e11, 1e12, 1e13, 1e14, 1e15, 1e16, 1e17, 1e18, 1e19, 
	1e20, 1e21, 1e22, 1e23, 1e24, 1e25, 1e26, 1e27, 1e28, 1e29, 
	1e30, 1e31, 1e32, 1e33, 1e34, 1e35, 1e36, 1e37, 1e38, 1e39, 
	1e40, 1e41, 1e42, 1e43, 1e44, 1e45, 1e46, 1e47, 1e48, 1e49, 
	1e50, 1e51, 1e52, 1e53, 1e54, 1e55, 1e56, 1e57, 1e58, 1e59,
	1e60, 1e61, 1e62, 1e63, 1e64, 1e65, 1e66, 1e67, 1e68, 1e69, 
	1e70, 1e71, 1e72, 1e73, 1e74, 1e75, 1e76, 1e77, 1e78, 1e79,
	1e80 };

const SPLIT64 sdl_power10[] = { {10000000000ULL},          // 1E10
				{100000000000ULL},         // 1E11
				{1000000000000ULL},        // 1E12
				{10000000000000ULL},       // 1E13
				{100000000000000ULL} };    // 1E14

static const uint64_t long_power10[] = {
	1,
	10ULL,
	100ULL,
	1000ULL,
	10000ULL,
	100000ULL,
	1000000ULL,
	10000000ULL,
	100000000ULL,
	1000000000ULL,
	10000000000ULL,
	100000000000ULL,
	1000000000000ULL,
	10000000000000ULL,
	100000000000000ULL,
	1000000000000000ULL,
	10000000000000000ULL,
	100000000000000000ULL,
	1000000000000000000ULL,
	10000000000000000000ULL};

typedef struct  {
	uint32_t Hi, Mid, Lo;
} DECOVFL;

const DECOVFL power_overflow[] = {
// This is a table of the largest values that can be in the upper two
// ULONGs of a 96-bit number that will not overflow when multiplied
// by a given power.  For the upper word, this is a table of 
// 2^32 / 10^n for 1 <= n <= 9.  For the lower word, this is the
// remaining fraction part * 2^32.  2^32 = 4294967296.
// 
    { 429496729u, 2576980377u, 2576980377u }, // 10^1 remainder 0.6
    { 42949672u,  4123168604u, 687194767u  }, // 10^2 remainder 0.16
    { 4294967u,   1271310319u, 2645699854u }, // 10^3 remainder 0.616
    { 429496u,    3133608139u, 694066715u  }, // 10^4 remainder 0.1616
    { 42949u,     2890341191u, 2216890319u }, // 10^5 remainder 0.51616
    { 4294u,      4154504685u, 2369172679u }, // 10^6 remainder 0.551616
    { 429u,       2133437386u, 4102387834u }, // 10^7 remainder 0.9551616
    { 42u,        4078814305u, 410238783u  }, // 10^8 remainder 0.09991616
    { 4u,         1266874889u, 3047500985u }, // 10^9 remainder 0.709551616
};


#define UInt32x32To64(a, b) ((uint64_t)((uint32_t)(a)) * (uint64_t)((uint32_t)(b)))
#define Div64by32(num, den) ((uint32_t)((uint64_t)(num) / (uint32_t)(den)))
#define Mod64by32(num, den) ((uint32_t)((uint64_t)(num) % (uint32_t)(den)))

static double
fnDblPower10(int ix)
{
    const int maxIx = (sizeof(double_power10)/sizeof(double_power10[0]));
    g_assert(ix >= 0);
    if (ix < maxIx)
        return double_power10[ix];
    return pow(10.0, ix);
} // double fnDblPower10()


static inline int64_t
DivMod32by32(int32_t num, int32_t den)
{
    SPLIT64  sdl;

    sdl.u.Lo = num / den;
    sdl.u.Hi = num % den;
    return sdl.int64;
}

static inline int64_t
DivMod64by32(int64_t num, int32_t den)
{
    SPLIT64  sdl;

    sdl.u.Lo = Div64by32(num, den);
    sdl.u.Hi = Mod64by32(num, den);
    return sdl.int64;
}

static uint64_t
UInt64x64To128(SPLIT64 op1, SPLIT64 op2, uint64_t *hi)
{
	SPLIT64  tmp1;
	SPLIT64  tmp2;
	SPLIT64  tmp3;

	tmp1.int64 = UInt32x32To64(op1.u.Lo, op2.u.Lo); // lo partial prod
	tmp2.int64 = UInt32x32To64(op1.u.Lo, op2.u.Hi); // mid 1 partial prod
	tmp1.u.Hi += tmp2.u.Lo;
	if (tmp1.u.Hi < tmp2.u.Lo)  // test for carry
		tmp2.u.Hi++;
	tmp3.int64 = UInt32x32To64(op1.u.Hi, op2.u.Hi) + (uint64_t)tmp2.u.Hi;
	tmp2.int64 = UInt32x32To64(op1.u.Hi, op2.u.Lo);
	tmp1.u.Hi += tmp2.u.Lo;
	if (tmp1.u.Hi < tmp2.u.Lo)  // test for carry
		tmp2.u.Hi++;
	tmp3.int64 += (uint64_t)tmp2.u.Hi;

	*hi = tmp3.int64;
	return tmp1.int64;
}

/**
* FullDiv64By32:
*
* Entry:
*   pdlNum  - Pointer to 64-bit dividend
*   ulDen   - 32-bit divisor
*
* Purpose:
*   Do full divide, yielding 64-bit result and 32-bit remainder.
*
* Exit:
*   Quotient overwrites dividend.
*   Returns remainder.
*
* Exceptions:
*   None.
*/
// Was: FullDiv64By32
static uint32_t
FullDiv64By32 (uint64_t *num, uint32_t den)
{
	SPLIT64  tmp;
	SPLIT64  res;
	
	tmp.int64 = *num;
	res.u.Hi = 0;
	
	if (tmp.u.Hi >= den) {
		// DivMod64by32 returns quotient in Lo, remainder in Hi.
		//
		res.u.Lo = tmp.u.Hi;
		res.int64 = DivMod64by32(res.int64, den);
		tmp.u.Hi = res.u.Hi;
		res.u.Hi = res.u.Lo;
	}
	
	tmp.int64 = DivMod64by32(tmp.int64, den);
	res.u.Lo = tmp.u.Lo;
	*num = res.int64;
	return tmp.u.Hi;
}

/***
 * SearchScale
 *
 * Entry:
 *   res_hi - Top uint32_t of quotient
 *   res_mid - Middle uint32_t of quotient
 *   res_lo - Bottom uint32_t of quotient
 *   scale  - Scale factor of quotient, range -DEC_SCALE_MAX to DEC_SCALE_MAX
 *
 * Purpose:
 *   Determine the max power of 10, <= 9, that the quotient can be scaled
 *   up by and still fit in 96 bits.
 *
 * Exit:
 *   Returns power of 10 to scale by, -1 if overflow error.
 *
 ***********************************************************************/

static int
SearchScale(uint32_t res_hi, uint32_t res_mid, uint32_t res_lo, int scale)
{
	int   cur_scale;

	// Quick check to stop us from trying to scale any more.
	//
	if (res_hi > OVFL_MAX_1_HI || scale >= DEC_SCALE_MAX) {
		cur_scale = 0;
		goto HaveScale;
	}

	if (scale > DEC_SCALE_MAX - 9) {
		// We can't scale by 10^9 without exceeding the max scale factor.
		// See if we can scale to the max.  If not, we'll fall into
		// standard search for scale factor.
		//
		cur_scale = DEC_SCALE_MAX - scale;
		if (res_hi < power_overflow[cur_scale - 1].Hi)
			goto HaveScale;

		if (res_hi == power_overflow[cur_scale - 1].Hi) {
		UpperEq:
			if (res_mid > power_overflow[cur_scale - 1].Mid ||
			    (res_mid == power_overflow[cur_scale - 1].Mid && res_lo > power_overflow[cur_scale - 1].Lo)) {
				cur_scale--;
			}
			goto HaveScale;
		}
	} else if (res_hi < OVFL_MAX_9_HI || (res_hi == OVFL_MAX_9_HI && res_mid < OVFL_MAX_9_MID) || (res_hi == OVFL_MAX_9_HI && res_mid == OVFL_MAX_9_MID && res_lo <= OVFL_MAX_9_LO))
		return 9;

	// Search for a power to scale by < 9.  Do a binary search
	// on power_overflow[].
	//
	cur_scale = 5;
	if (res_hi < OVFL_MAX_5_HI)
		cur_scale = 7;
	else if (res_hi > OVFL_MAX_5_HI)
		cur_scale = 3;
	else
		goto UpperEq;

	// cur_scale is 3 or 7.
	//
	if (res_hi < power_overflow[cur_scale - 1].Hi)
		cur_scale++;
	else if (res_hi > power_overflow[cur_scale - 1].Hi)
		cur_scale--;
	else
		goto UpperEq;

	// cur_scale is 2, 4, 6, or 8.
	//
	// In all cases, we already found we could not use the power one larger.
	// So if we can use this power, it is the biggest, and we're done.  If
	// we can't use this power, the one below it is correct for all cases 
	// unless it's 10^1 -- we might have to go to 10^0 (no scaling).
	// 
	if (res_hi > power_overflow[cur_scale - 1].Hi)
		cur_scale--;

	if (res_hi == power_overflow[cur_scale - 1].Hi)
		goto UpperEq;

HaveScale:
	// cur_scale = largest power of 10 we can scale by without overflow, 
	// cur_scale < 9.  See if this is enough to make scale factor 
	// positive if it isn't already.
	// 
	if (cur_scale + scale < 0)
		cur_scale = -1;

	return cur_scale;
}


/**
* Div96By32
*
* Entry:
*   rgulNum - Pointer to 96-bit dividend as array of uint32_ts, least-sig first
*   ulDen   - 32-bit divisor.
*
* Purpose:
*   Do full divide, yielding 96-bit result and 32-bit remainder.
*
* Exit:
*   Quotient overwrites dividend.
*   Returns remainder.
*
* Exceptions:
*   None.
*
*/
static uint32_t
Div96By32(uint32_t *num, uint32_t den)
{
	SPLIT64  tmp;

	tmp.u.Hi = 0;

	if (num[2] != 0)
		goto Div3Word;

	if (num[1] >= den)
		goto Div2Word;

	tmp.u.Hi = num[1];
	num[1] = 0;
	goto Div1Word;

Div3Word:
	tmp.u.Lo = num[2];
	tmp.int64 = DivMod64by32(tmp.int64, den);
	num[2] = tmp.u.Lo;
Div2Word:
	tmp.u.Lo = num[1];
	tmp.int64 = DivMod64by32(tmp.int64, den);
	num[1] = tmp.u.Lo;
Div1Word:
	tmp.u.Lo = num[0];
	tmp.int64 = DivMod64by32(tmp.int64, den);
	num[0] = tmp.u.Lo;
	return tmp.u.Hi;
}

/***
 * DecFixInt
 *
 * Entry:
 *   pdecRes - Pointer to Decimal result location
 *   operand  - Pointer to Decimal operand
 *
 * Purpose:
 *   Chop the value to integer.  Return remainder so Int() function
 *   can round down if non-zero.
 *
 * Exit:
 *   Returns remainder.
 *
 * Exceptions:
 *   None.
 *
 ***********************************************************************/

static uint32_t
DecFixInt(MonoDecimal * result, MonoDecimal * operand)
{
	uint32_t   num[3];
	uint32_t   rem;
	uint32_t   pwr;
	int     scale;

	if (operand->u.u.scale > 0) {
		num[0] = operand->v.v.Lo32;
		num[1] = operand->v.v.Mid32;
		num[2] = operand->Hi32;
		scale = operand->u.u.scale;
		result->u.u.sign = operand->u.u.sign;
		rem = 0;

		do {
			if (scale > POWER10_MAX)
				pwr = ten_to_nine;
			else
				pwr = power10[scale];

			rem |= Div96By32(num, pwr);
			scale -= 9;
		}while (scale > 0);

		result->v.v.Lo32 = num[0];
		result->v.v.Mid32 = num[1];
		result->Hi32 = num[2];
		result->u.u.scale = 0;

		return rem;
	}

	COPYDEC(*result, *operand);
	// Odd, the Microsoft code does not set result->reserved to zero on this case
	return 0;
}

/**
 * ScaleResult:
 *
 * Entry:
 *   res - Array of uint32_ts with value, least-significant first.
 *   hi_res  - Index of last non-zero value in res.
 *   scale  - Scale factor for this value, range 0 - 2 * DEC_SCALE_MAX
 *
 * Purpose:
 *   See if we need to scale the result to fit it in 96 bits.
 *   Perform needed scaling.  Adjust scale factor accordingly.
 *
 * Exit:
 *   res updated in place, always 3 uint32_ts.
 *   New scale factor returned, -1 if overflow error.
 *
 */
static int
ScaleResult(uint32_t *res, int hi_res, int scale)
{
	int     new_scale;
	int     cur;
	uint32_t   pwr;
	uint32_t   tmp;
	uint32_t   sticky;
	SPLIT64 sdlTmp;

	// See if we need to scale the result.  The combined scale must
	// be <= DEC_SCALE_MAX and the upper 96 bits must be zero.
	// 
	// Start by figuring a lower bound on the scaling needed to make
	// the upper 96 bits zero.  hi_res is the index into res[]
	// of the highest non-zero uint32_t.
	// 
	new_scale =   hi_res * 32 - 64 - 1;
	if (new_scale > 0) {

		// Find the MSB.
		//
		tmp = res[hi_res];
		if (!(tmp & 0xFFFF0000)) {
			new_scale -= 16;
			tmp <<= 16;
		}
		if (!(tmp & 0xFF000000)) {
			new_scale -= 8;
			tmp <<= 8;
		}
		if (!(tmp & 0xF0000000)) {
			new_scale -= 4;
			tmp <<= 4;
		}
		if (!(tmp & 0xC0000000)) {
			new_scale -= 2;
			tmp <<= 2;
		}
		if (!(tmp & 0x80000000)) {
			new_scale--;
			tmp <<= 1;
		}
    
		// Multiply bit position by log10(2) to figure it's power of 10.
		// We scale the log by 256.  log(2) = .30103, * 256 = 77.  Doing this 
		// with a multiply saves a 96-byte lookup table.  The power returned
		// is <= the power of the number, so we must add one power of 10
		// to make it's integer part zero after dividing by 256.
		// 
		// Note: the result of this multiplication by an approximation of
		// log10(2) have been exhaustively checked to verify it gives the 
		// correct result.  (There were only 95 to check...)
		// 
		new_scale = ((new_scale * 77) >> 8) + 1;

		// new_scale = min scale factor to make high 96 bits zero, 0 - 29.
		// This reduces the scale factor of the result.  If it exceeds the
		// current scale of the result, we'll overflow.
		// 
		if (new_scale > scale)
			return -1;
	}
	else
		new_scale = 0;

	// Make sure we scale by enough to bring the current scale factor
	// into valid range.
	//
	if (new_scale < scale - DEC_SCALE_MAX)
		new_scale = scale - DEC_SCALE_MAX;

	if (new_scale != 0) {
		// Scale by the power of 10 given by new_scale.  Note that this is 
		// NOT guaranteed to bring the number within 96 bits -- it could 
		// be 1 power of 10 short.
		//
		scale -= new_scale;
		sticky = 0;
		sdlTmp.u.Hi = 0; // initialize remainder

		for (;;) {

			sticky |= sdlTmp.u.Hi; // record remainder as sticky bit

			if (new_scale > POWER10_MAX)
				pwr = ten_to_nine;
			else
				pwr = power10[new_scale];

			// Compute first quotient.
			// DivMod64by32 returns quotient in Lo, remainder in Hi.
			//
			sdlTmp.int64 = DivMod64by32(res[hi_res], pwr);
			res[hi_res] = sdlTmp.u.Lo;
			cur = hi_res - 1;

			if (cur >= 0) {
				// If first quotient was 0, update hi_res.
				//
				if (sdlTmp.u.Lo == 0)
					hi_res--;

				// Compute subsequent quotients.
				//
				do {
					sdlTmp.u.Lo = res[cur];
					sdlTmp.int64 = DivMod64by32(sdlTmp.int64, pwr);
					res[cur] = sdlTmp.u.Lo;
					cur--;
				} while (cur >= 0);

			}

			new_scale -= POWER10_MAX;
			if (new_scale > 0)
				continue; // scale some more

			// If we scaled enough, hi_res would be 2 or less.  If not,
			// divide by 10 more.
			//
			if (hi_res > 2) {
				new_scale = 1;
				scale--;
				continue; // scale by 10
			}

			// Round final result.  See if remainder >= 1/2 of divisor.
			// If remainder == 1/2 divisor, round up if odd or sticky bit set.
			//
			pwr >>= 1;  // power of 10 always even
			if ( pwr <= sdlTmp.u.Hi && (pwr < sdlTmp.u.Hi ||
						    ((res[0] & 1) | sticky)) ) {
				cur = -1;
				while (++res[++cur] == 0);
				
				if (cur > 2) {
					// The rounding caused us to carry beyond 96 bits. 
					// Scale by 10 more.
					//
					hi_res = cur;
					sticky = 0;  // no sticky bit
					sdlTmp.u.Hi = 0; // or remainder
					new_scale = 1;
					scale--;
					continue; // scale by 10
				}
			}
			
			// We may have scaled it more than we planned.  Make sure the scale 
			// factor hasn't gone negative, indicating overflow.
			// 
			if (scale < 0)
				return -1;
			
			return scale;
		} // for(;;)
	}
	return scale;
}

// Decimal multiply
// Returns: MONO_DECIMAL_OVERFLOW or MONO_DECIMAL_OK
static MonoDecimalStatus
mono_decimal_multiply_result(MonoDecimal * left, MonoDecimal * right, MonoDecimal * result)
{
	SPLIT64 tmp;
	SPLIT64 tmp2;
	SPLIT64 tmp3;
	int     scale;
	int     hi_prod;
	uint32_t   pwr;
	uint32_t   rem_lo;
	uint32_t   rem_hi;
	uint32_t   prod[6];

	scale = left->u.u.scale + right->u.u.scale;

	if ((left->Hi32 | left->v.v.Mid32 | right->Hi32 | right->v.v.Mid32) == 0) {
		// Upper 64 bits are zero.
		//
		tmp.int64 = UInt32x32To64(left->v.v.Lo32, right->v.v.Lo32);
		if (scale > DEC_SCALE_MAX)
		{
			// Result scale is too big.  Divide result by power of 10 to reduce it.
			// If the amount to divide by is > 19 the result is guaranteed
			// less than 1/2.  [max value in 64 bits = 1.84E19]
			//
			scale -= DEC_SCALE_MAX;
			if (scale > 19) {
			ReturnZero:
				DECIMAL_SETZERO(*result);
				return MONO_DECIMAL_OK;
			}

			if (scale > POWER10_MAX) {
				// Divide by 1E10 first, to get the power down to a 32-bit quantity.
				// 1E10 itself doesn't fit in 32 bits, so we'll divide by 2.5E9 now
				// then multiply the next divisor by 4 (which will be a max of 4E9).
				// 
				rem_lo = FullDiv64By32(&tmp.int64, ten_to_ten_div_4);
				pwr = power10[scale - 10] << 2;
			} else {
				pwr = power10[scale];
				rem_lo = 0;
			}

			// Power to divide by fits in 32 bits.
			//
			rem_hi = FullDiv64By32(&tmp.int64, pwr);

			// Round result.  See if remainder >= 1/2 of divisor.
			// Divisor is a power of 10, so it is always even.
			//
			pwr >>= 1;
			if (rem_hi >= pwr && (rem_hi > pwr || (rem_lo | (tmp.u.Lo & 1))))
				tmp.int64++;

			scale = DEC_SCALE_MAX;
		}
		DECIMAL_LO32(*result) = tmp.u.Lo;
		DECIMAL_MID32(*result) = tmp.u.Hi;
		DECIMAL_HI32(*result) = 0;
	} else {
		// At least one operand has bits set in the upper 64 bits.
		//
		// Compute and accumulate the 9 partial products into a 
		// 192-bit (24-byte) result.
		//
		//                [l-h][l-m][l-l]   left high, middle, low
		//             x  [r-h][r-m][r-l]   right high, middle, low
		// ------------------------------
		//
		//                     [0-h][0-l]   l-l * r-l
		//                [1ah][1al]        l-l * r-m
		//                [1bh][1bl]        l-m * r-l
		//           [2ah][2al]             l-m * r-m
		//           [2bh][2bl]             l-l * r-h
		//           [2ch][2cl]             l-h * r-l
		//      [3ah][3al]                  l-m * r-h
		//      [3bh][3bl]                  l-h * r-m
		// [4-h][4-l]                       l-h * r-h
		// ------------------------------
		// [p-5][p-4][p-3][p-2][p-1][p-0]   prod[] array
		//
		tmp.int64 = UInt32x32To64(left->v.v.Lo32, right->v.v.Lo32);
		prod[0] = tmp.u.Lo;

		tmp2.int64 = UInt32x32To64(left->v.v.Lo32, right->v.v.Mid32) + tmp.u.Hi;

		tmp.int64 = UInt32x32To64(left->v.v.Mid32, right->v.v.Lo32);
		tmp.int64 += tmp2.int64; // this could generate carry
		prod[1] = tmp.u.Lo;
		if (tmp.int64 < tmp2.int64) // detect carry
			tmp2.u.Hi = 1;
		else
			tmp2.u.Hi = 0;
		tmp2.u.Lo = tmp.u.Hi;

		tmp.int64 = UInt32x32To64(left->v.v.Mid32, right->v.v.Mid32) + tmp2.int64;

		if (left->Hi32 | right->Hi32) {
			// Highest 32 bits is non-zero.  Calculate 5 more partial products.
			//
			tmp2.int64 = UInt32x32To64(left->v.v.Lo32, right->Hi32);
			tmp.int64 += tmp2.int64; // this could generate carry
			if (tmp.int64 < tmp2.int64) // detect carry
				tmp3.u.Hi = 1;
			else
				tmp3.u.Hi = 0;

			tmp2.int64 = UInt32x32To64(left->Hi32, right->v.v.Lo32);
			tmp.int64 += tmp2.int64; // this could generate carry
			prod[2] = tmp.u.Lo;
			if (tmp.int64 < tmp2.int64) // detect carry
				tmp3.u.Hi++;
			tmp3.u.Lo = tmp.u.Hi;

			tmp.int64 = UInt32x32To64(left->v.v.Mid32, right->Hi32);
			tmp.int64 += tmp3.int64; // this could generate carry
			if (tmp.int64 < tmp3.int64) // detect carry
				tmp3.u.Hi = 1;
			else
				tmp3.u.Hi = 0;

			tmp2.int64 = UInt32x32To64(left->Hi32, right->v.v.Mid32);
			tmp.int64 += tmp2.int64; // this could generate carry
			prod[3] = tmp.u.Lo;
			if (tmp.int64 < tmp2.int64) // detect carry
				tmp3.u.Hi++;
			tmp3.u.Lo = tmp.u.Hi;

			tmp.int64 = UInt32x32To64(left->Hi32, right->Hi32) + tmp3.int64;
			prod[4] = tmp.u.Lo;
			prod[5] = tmp.u.Hi;

			hi_prod = 5;
		}
		else {
			prod[2] = tmp.u.Lo;
			prod[3] = tmp.u.Hi;
			hi_prod = 3;
		}

		// Check for leading zero uint32_ts on the product
		//
		while (prod[hi_prod] == 0) {
			hi_prod--;
			if (hi_prod < 0)
				goto ReturnZero;
		}

		scale = ScaleResult(prod, hi_prod, scale);
		if (scale == -1)
			return MONO_DECIMAL_OVERFLOW;

		result->v.v.Lo32 = prod[0];
		result->v.v.Mid32 = prod[1];
		result->Hi32 = prod[2];
	}

	result->u.u.sign = right->u.u.sign ^ left->u.u.sign;
	result->u.u.scale = (char)scale;
	return MONO_DECIMAL_OK;
}

// Addition and subtraction
static MonoDecimalStatus
DecAddSub(MonoDecimal *left, MonoDecimal *right, MonoDecimal *result, int8_t sign)
{
	uint32_t     num[6];
	uint32_t     pwr;
	int       scale;
	int       hi_prod;
	int       cur;
	SPLIT64   tmp;
	MonoDecimal decRes;
	MonoDecimal decTmp;
	MonoDecimal *pdecTmp;

	sign ^= (right->u.u.sign ^ left->u.u.sign) & DECIMAL_NEG;

	if (right->u.u.scale == left->u.u.scale) {
		// Scale factors are equal, no alignment necessary.
		//
		decRes.u.signscale = left->u.signscale;

	AlignedAdd:
		if (sign) {
			// Signs differ - subtract
			//
			DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(*left) - DECIMAL_LO64_GET(*right));
			DECIMAL_HI32(decRes) = DECIMAL_HI32(*left) - DECIMAL_HI32(*right);

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(decRes) > DECIMAL_LO64_GET(*left)) {
				decRes.Hi32--;
				if (decRes.Hi32 >= left->Hi32)
					goto SignFlip;
			} else if (decRes.Hi32 > left->Hi32) {
				// Got negative result.  Flip its sign.
				//
			SignFlip:
				DECIMAL_LO64_SET(decRes, -(uint64_t)DECIMAL_LO64_GET(decRes));
				decRes.Hi32 = ~decRes.Hi32;
				if (DECIMAL_LO64_GET(decRes) == 0)
					decRes.Hi32++;
				decRes.u.u.sign ^= DECIMAL_NEG;
			}

		} else {
			// Signs are the same - add
			//
			DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(*left) + DECIMAL_LO64_GET(*right));
			decRes.Hi32 = left->Hi32 + right->Hi32;

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(decRes) < DECIMAL_LO64_GET(*left)) {
				decRes.Hi32++;
				if (decRes.Hi32 <= left->Hi32)
					goto AlignedScale;
			} else if (decRes.Hi32 < left->Hi32) {
			AlignedScale:
				// The addition carried above 96 bits.  Divide the result by 10,
				// dropping the scale factor.
				//
				if (decRes.u.u.scale == 0)
					return MONO_DECIMAL_OVERFLOW;
				decRes.u.u.scale--;

				tmp.u.Lo = decRes.Hi32;
				tmp.u.Hi = 1;
				tmp.int64 = DivMod64by32(tmp.int64, 10);
				decRes.Hi32 = tmp.u.Lo;

				tmp.u.Lo = decRes.v.v.Mid32;
				tmp.int64 = DivMod64by32(tmp.int64, 10);
				decRes.v.v.Mid32 = tmp.u.Lo;

				tmp.u.Lo = decRes.v.v.Lo32;
				tmp.int64 = DivMod64by32(tmp.int64, 10);
				decRes.v.v.Lo32 = tmp.u.Lo;

				// See if we need to round up.
				//
				if (tmp.u.Hi >= 5 && (tmp.u.Hi > 5 || (decRes.v.v.Lo32 & 1))) {
					DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(decRes)+1)
						if (DECIMAL_LO64_GET(decRes) == 0)
							decRes.Hi32++;
				}
			}
		}
	}
	else {
		// Scale factors are not equal.  Assume that a larger scale
		// factor (more decimal places) is likely to mean that number
		// is smaller.  Start by guessing that the right operand has
		// the larger scale factor.  The result will have the larger
		// scale factor.
		//
		decRes.u.u.scale = right->u.u.scale;  // scale factor of "smaller"
		decRes.u.u.sign = left->u.u.sign;    // but sign of "larger"
		scale = decRes.u.u.scale - left->u.u.scale;

		if (scale < 0) {
			// Guessed scale factor wrong. Swap operands.
			//
			scale = -scale;
			decRes.u.u.scale = left->u.u.scale;
			decRes.u.u.sign ^= sign;
			pdecTmp = right;
			right = left;
			left = pdecTmp;
		}

		// *left will need to be multiplied by 10^scale so
		// it will have the same scale as *right.  We could be
		// extending it to up to 192 bits of precision.
		//
		if (scale <= POWER10_MAX) {
			// Scaling won't make it larger than 4 uint32_ts
			//
			pwr = power10[scale];
			DECIMAL_LO64_SET(decTmp, UInt32x32To64(left->v.v.Lo32, pwr));
			tmp.int64 = UInt32x32To64(left->v.v.Mid32, pwr);
			tmp.int64 += decTmp.v.v.Mid32;
			decTmp.v.v.Mid32 = tmp.u.Lo;
			decTmp.Hi32 = tmp.u.Hi;
			tmp.int64 = UInt32x32To64(left->Hi32, pwr);
			tmp.int64 += decTmp.Hi32;
			if (tmp.u.Hi == 0) {
				// Result fits in 96 bits.  Use standard aligned add.
				//
				decTmp.Hi32 = tmp.u.Lo;
				left = &decTmp;
				goto AlignedAdd;
			}
			num[0] = decTmp.v.v.Lo32;
			num[1] = decTmp.v.v.Mid32;
			num[2] = tmp.u.Lo;
			num[3] = tmp.u.Hi;
			hi_prod = 3;
		}
		else {
			// Have to scale by a bunch.  Move the number to a buffer
			// where it has room to grow as it's scaled.
			//
			num[0] = left->v.v.Lo32;
			num[1] = left->v.v.Mid32;
			num[2] = left->Hi32;
			hi_prod = 2;

			// Scan for zeros in the upper words.
			//
			if (num[2] == 0) {
				hi_prod = 1;
				if (num[1] == 0) {
					hi_prod = 0;
					if (num[0] == 0) {
						// Left arg is zero, return right.
						//
						DECIMAL_LO64_SET(decRes, DECIMAL_LO64_GET(*right));
						decRes.Hi32 = right->Hi32;
						decRes.u.u.sign ^= sign;
						goto RetDec;
					}
				}
			}

			// Scaling loop, up to 10^9 at a time.  hi_prod stays updated
			// with index of highest non-zero uint32_t.
			//
			for (; scale > 0; scale -= POWER10_MAX) {
				if (scale > POWER10_MAX)
					pwr = ten_to_nine;
				else
					pwr = power10[scale];

				tmp.u.Hi = 0;
				for (cur = 0; cur <= hi_prod; cur++) {
					tmp.int64 = UInt32x32To64(num[cur], pwr) + tmp.u.Hi;
					num[cur] = tmp.u.Lo;
				}

				if (tmp.u.Hi != 0)
					// We're extending the result by another uint32_t.
					num[++hi_prod] = tmp.u.Hi;
			}
		}

		// Scaling complete, do the add.  Could be subtract if signs differ.
		//
		tmp.u.Lo = num[0];
		tmp.u.Hi = num[1];

		if (sign) {
			// Signs differ, subtract.
			//
			DECIMAL_LO64_SET(decRes, tmp.int64 - DECIMAL_LO64_GET(*right));
			decRes.Hi32 = num[2] - right->Hi32;

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(decRes) > tmp.int64) {
				decRes.Hi32--;
				if (decRes.Hi32 >= num[2])
					goto LongSub;
			}
			else if (decRes.Hi32 > num[2]) {
			LongSub:
				// If num has more than 96 bits of precision, then we need to
				// carry the subtraction into the higher bits.  If it doesn't,
				// then we subtracted in the wrong order and have to flip the 
				// sign of the result.
				// 
				if (hi_prod <= 2)
					goto SignFlip;

				cur = 3;
				while(num[cur++]-- == 0);
				if (num[hi_prod] == 0)
					hi_prod--;
			}
		}
		else {
			// Signs the same, add.
			//
			DECIMAL_LO64_SET(decRes, tmp.int64 + DECIMAL_LO64_GET(*right));
			decRes.Hi32 = num[2] + right->Hi32;

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(decRes) < tmp.int64) {
				decRes.Hi32++;
				if (decRes.Hi32 <= num[2])
					goto LongAdd;
			}
			else if (decRes.Hi32 < num[2]) {
			LongAdd:
				// Had a carry above 96 bits.
				//
				cur = 3;
				do {
					if (hi_prod < cur) {
						num[cur] = 1;
						hi_prod = cur;
						break;
					}
				}while (++num[cur++] == 0);
			}
		}

		if (hi_prod > 2) {
			num[0] = decRes.v.v.Lo32;
			num[1] = decRes.v.v.Mid32;
			num[2] = decRes.Hi32;
			decRes.u.u.scale = ScaleResult(num, hi_prod, decRes.u.u.scale);
			if (decRes.u.u.scale == (uint8_t) -1)
				return MONO_DECIMAL_OVERFLOW;

			decRes.v.v.Lo32 = num[0];
			decRes.v.v.Mid32 = num[1];
			decRes.Hi32 = num[2];
		}
	}

RetDec:
	COPYDEC(*result, decRes);
	// Odd, the Microsoft code does not set result->reserved to zero on this case
	return MONO_DECIMAL_OK;
}

// Decimal addition
static MonoDecimalStatus G_GNUC_UNUSED
mono_decimal_add(MonoDecimal *left, MonoDecimal *right, MonoDecimal *result)
{
    return DecAddSub (left, right, result, 0);
}

// Decimal subtraction
static MonoDecimalStatus G_GNUC_UNUSED
mono_decimal_sub(MonoDecimal *left, MonoDecimal *right, MonoDecimal *result)
{
    return DecAddSub (left, right, result, DECIMAL_NEG);
}

/**
 * IncreaseScale:
 *
 * Entry:
 *   num - Pointer to 96-bit number as array of uint32_ts, least-sig first
 *   pwr   - Scale factor to multiply by
 *
 * Purpose:
 *   Multiply the two numbers.  The low 96 bits of the result overwrite
 *   the input.  The last 32 bits of the product are the return value.
 *
 * Exit:
 *   Returns highest 32 bits of product.
 *
 * Exceptions:
 *   None.
 *
 */
static uint32_t
IncreaseScale(uint32_t *num, uint32_t pwr)
{
	SPLIT64   sdlTmp;

	sdlTmp.int64 = UInt32x32To64(num[0], pwr);
	num[0] = sdlTmp.u.Lo;
	sdlTmp.int64 = UInt32x32To64(num[1], pwr) + sdlTmp.u.Hi;
	num[1] = sdlTmp.u.Lo;
	sdlTmp.int64 = UInt32x32To64(num[2], pwr) + sdlTmp.u.Hi;
	num[2] = sdlTmp.u.Lo;
	return sdlTmp.u.Hi;
}

/**
 * Div96By64:
 *
 * Entry:
 *   rgulNum - Pointer to 96-bit dividend as array of uint32_ts, least-sig first
 *   sdlDen  - 64-bit divisor.
 *
 * Purpose:
 *   Do partial divide, yielding 32-bit result and 64-bit remainder.
 *   Divisor must be larger than upper 64 bits of dividend.
 *
 * Exit:
 *   Remainder overwrites lower 64-bits of dividend.
 *   Returns quotient.
 *
 * Exceptions:
 *   None.
 *
 */
static uint32_t
Div96By64(uint32_t *num, SPLIT64 den)
{
	SPLIT64 quo;
	SPLIT64 sdlNum;
	SPLIT64 prod;

	sdlNum.u.Lo = num[0];

	if (num[2] >= den.u.Hi) {
		// Divide would overflow.  Assume a quotient of 2^32, and set
		// up remainder accordingly.  Then jump to loop which reduces
		// the quotient.
		//
		sdlNum.u.Hi = num[1] - den.u.Lo;
		quo.u.Lo = 0;
		goto NegRem;
	}

	// Hardware divide won't overflow
	//
	if (num[2] == 0 && num[1] < den.u.Hi)
		// Result is zero.  Entire dividend is remainder.
		//
		return 0;

	// DivMod64by32 returns quotient in Lo, remainder in Hi.
	//
	quo.u.Lo = num[1];
	quo.u.Hi = num[2];
	quo.int64 = DivMod64by32(quo.int64, den.u.Hi);
	sdlNum.u.Hi = quo.u.Hi; // remainder

	// Compute full remainder, rem = dividend - (quo * divisor).
	//
	prod.int64 = UInt32x32To64(quo.u.Lo, den.u.Lo); // quo * lo divisor
	sdlNum.int64 -= prod.int64;

	if (sdlNum.int64 > ~prod.int64) {
	NegRem:
		// Remainder went negative.  Add divisor back in until it's positive,
		// a max of 2 times.
		//
		do {
			quo.u.Lo--;
			sdlNum.int64 += den.int64;
		}while (sdlNum.int64 >= den.int64);
	}

	num[0] = sdlNum.u.Lo;
	num[1] = sdlNum.u.Hi;
	return quo.u.Lo;
}

/***
* Div128By96
*
* Entry:
*   rgulNum - Pointer to 128-bit dividend as array of uint32_ts, least-sig first
*   den - Pointer to 96-bit divisor.
*
* Purpose:
*   Do partial divide, yielding 32-bit result and 96-bit remainder.
*   Top divisor uint32_t must be larger than top dividend uint32_t.  This is
*   assured in the initial call because the divisor is normalized
*   and the dividend can't be.  In subsequent calls, the remainder
*   is multiplied by 10^9 (max), so it can be no more than 1/4 of
*   the divisor which is effectively multiplied by 2^32 (4 * 10^9).
*
* Exit:
*   Remainder overwrites lower 96-bits of dividend.
*   Returns quotient.
*
* Exceptions:
*   None.
*
***********************************************************************/

static uint32_t
Div128By96(uint32_t *num, uint32_t *den)
{
	SPLIT64 sdlQuo;
	SPLIT64 sdlNum;
	SPLIT64 sdlProd1;
	SPLIT64 sdlProd2;

	sdlNum.u.Lo = num[0];
	sdlNum.u.Hi = num[1];

	if (num[3] == 0 && num[2] < den[2]){
		// Result is zero.  Entire dividend is remainder.
		//
		return 0;
	}

	// DivMod64by32 returns quotient in Lo, remainder in Hi.
	//
	sdlQuo.u.Lo = num[2];
	sdlQuo.u.Hi = num[3];
	sdlQuo.int64 = DivMod64by32(sdlQuo.int64, den[2]);

	// Compute full remainder, rem = dividend - (quo * divisor).
	//
	sdlProd1.int64 = UInt32x32To64(sdlQuo.u.Lo, den[0]); // quo * lo divisor
	sdlProd2.int64 = UInt32x32To64(sdlQuo.u.Lo, den[1]); // quo * mid divisor
	sdlProd2.int64 += sdlProd1.u.Hi;
	sdlProd1.u.Hi = sdlProd2.u.Lo;

	sdlNum.int64 -= sdlProd1.int64;
	num[2] = sdlQuo.u.Hi - sdlProd2.u.Hi; // sdlQuo.Hi is remainder

	// Propagate carries
	//
	if (sdlNum.int64 > ~sdlProd1.int64) {
		num[2]--;
		if (num[2] >= ~sdlProd2.u.Hi)
			goto NegRem;
	} else if (num[2] > ~sdlProd2.u.Hi) {
	NegRem:
		// Remainder went negative.  Add divisor back in until it's positive,
		// a max of 2 times.
		//
		sdlProd1.u.Lo = den[0];
		sdlProd1.u.Hi = den[1];

		for (;;) {
			sdlQuo.u.Lo--;
			sdlNum.int64 += sdlProd1.int64;
			num[2] += den[2];

			if (sdlNum.int64 < sdlProd1.int64) {
				// Detected carry. Check for carry out of top
				// before adding it in.
				//
				if (num[2]++ < den[2])
					break;
			}
			if (num[2] < den[2])
				break; // detected carry
		}
	}

	num[0] = sdlNum.u.Lo;
	num[1] = sdlNum.u.Hi;
	return sdlQuo.u.Lo;
}

// Add a 32 bit unsigned long to an array of 3 unsigned longs representing a 96 integer
// Returns FALSE if there is an overflow
static gboolean
Add32To96(uint32_t *num, uint32_t value)
{
	num[0] += value;
	if (num[0] < value) {
		if (++num[1] == 0) {                
			if (++num[2] == 0) {                
				return FALSE;
			}            
		}
	}
	return TRUE;
}

static void
OverflowUnscale (uint32_t *quo, gboolean remainder)
{
	SPLIT64  sdlTmp;
	
	// We have overflown, so load the high bit with a one.
	sdlTmp.u.Hi = 1u;
	sdlTmp.u.Lo = quo[2];
	sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10u);
	quo[2] = sdlTmp.u.Lo;
	sdlTmp.u.Lo = quo[1];
	sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10u);
	quo[1] = sdlTmp.u.Lo;
	sdlTmp.u.Lo = quo[0];
	sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10u);
	quo[0] = sdlTmp.u.Lo;
	// The remainder is the last digit that does not fit, so we can use it to work out if we need to round up
	if ((sdlTmp.u.Hi > 5) || ((sdlTmp.u.Hi == 5) && ( remainder || (quo[0] & 1)))) {
		Add32To96(quo, 1u);
	}
}

// mono_decimal_divide - Decimal divide
static MonoDecimalStatus G_GNUC_UNUSED
mono_decimal_divide_result(MonoDecimal *left, MonoDecimal *right, MonoDecimal *result)
{
	uint32_t   quo[3];
	uint32_t   quoSave[3];
	uint32_t   rem[4];
	uint32_t   divisor[3];
	uint32_t   pwr;
	uint32_t   utmp;
	uint32_t   utmp1;
	SPLIT64 sdlTmp;
	SPLIT64 sdlDivisor;
	int     scale;
	int     cur_scale;

	scale = left->u.u.scale - right->u.u.scale;
	divisor[0] = right->v.v.Lo32;
	divisor[1] = right->v.v.Mid32;
	divisor[2] = right->Hi32;

	if (divisor[1] == 0 && divisor[2] == 0) {
		// Divisor is only 32 bits.  Easy divide.
		//
		if (divisor[0] == 0)
			return MONO_DECIMAL_DIVBYZERO;

		quo[0] = left->v.v.Lo32;
		quo[1] = left->v.v.Mid32;
		quo[2] = left->Hi32;
		rem[0] = Div96By32(quo, divisor[0]);

		for (;;) {
			if (rem[0] == 0) {
				if (scale < 0) {
					cur_scale = min(9, -scale);
					goto HaveScale;
				}
				break;
			}

			// We have computed a quotient based on the natural scale 
			// ( <dividend scale> - <divisor scale> ).  We have a non-zero 
			// remainder, so now we should increase the scale if possible to 
			// include more quotient bits.
			// 
			// If it doesn't cause overflow, we'll loop scaling by 10^9 and 
			// computing more quotient bits as long as the remainder stays 
			// non-zero.  If scaling by that much would cause overflow, we'll 
			// drop out of the loop and scale by as much as we can.
			// 
			// Scaling by 10^9 will overflow if quo[2].quo[1] >= 2^32 / 10^9 
			// = 4.294 967 296.  So the upper limit is quo[2] == 4 and 
			// quo[1] == 0.294 967 296 * 2^32 = 1,266,874,889.7+.  Since 
			// quotient bits in quo[0] could be all 1's, then 1,266,874,888 
			// is the largest value in quo[1] (when quo[2] == 4) that is 
			// assured not to overflow.
			// 
			cur_scale = SearchScale(quo[2], quo[1], quo [0], scale);
			if (cur_scale == 0) {
				// No more scaling to be done, but remainder is non-zero.
				// Round quotient.
				//
				utmp = rem[0] << 1;
				if (utmp < rem[0] || (utmp >= divisor[0] &&
						      (utmp > divisor[0] || (quo[0] & 1)))) {
				RoundUp:
					if (++quo[0] == 0)
						if (++quo[1] == 0)
							quo[2]++;
				}
				break;
			}

			if (cur_scale == -1)
				return MONO_DECIMAL_OVERFLOW;

		HaveScale:
			pwr = power10[cur_scale];
			scale += cur_scale;

			if (IncreaseScale(quo, pwr) != 0)
				return MONO_DECIMAL_OVERFLOW;

			sdlTmp.int64 = DivMod64by32(UInt32x32To64(rem[0], pwr), divisor[0]);
			rem[0] = sdlTmp.u.Hi;

			quo[0] += sdlTmp.u.Lo;
			if (quo[0] < sdlTmp.u.Lo) {
				if (++quo[1] == 0)
					quo[2]++;
			}
		} // for (;;)
	}
	else {
		// Divisor has bits set in the upper 64 bits.
		//
		// Divisor must be fully normalized (shifted so bit 31 of the most 
		// significant uint32_t is 1).  Locate the MSB so we know how much to 
		// normalize by.  The dividend will be shifted by the same amount so 
		// the quotient is not changed.
		//
		if (divisor[2] == 0)
			utmp = divisor[1];
		else
			utmp = divisor[2];

		cur_scale = 0;
		if (!(utmp & 0xFFFF0000)) {
			cur_scale += 16;
			utmp <<= 16;
		}
		if (!(utmp & 0xFF000000)) {
			cur_scale += 8;
			utmp <<= 8;
		}
		if (!(utmp & 0xF0000000)) {
			cur_scale += 4;
			utmp <<= 4;
		}
		if (!(utmp & 0xC0000000)) {
			cur_scale += 2;
			utmp <<= 2;
		}
		if (!(utmp & 0x80000000)) {
			cur_scale++;
			utmp <<= 1;
		}
    
		// Shift both dividend and divisor left by cur_scale.
		// 
		sdlTmp.int64 = DECIMAL_LO64_GET(*left) << cur_scale;
		rem[0] = sdlTmp.u.Lo;
		rem[1] = sdlTmp.u.Hi;
		sdlTmp.u.Lo = left->v.v.Mid32;
		sdlTmp.u.Hi = left->Hi32;
		sdlTmp.int64 <<= cur_scale;
		rem[2] = sdlTmp.u.Hi;
		rem[3] = (left->Hi32 >> (31 - cur_scale)) >> 1;

		sdlDivisor.u.Lo = divisor[0];
		sdlDivisor.u.Hi = divisor[1];
		sdlDivisor.int64 <<= cur_scale;

		if (divisor[2] == 0) {
			// Have a 64-bit divisor in sdlDivisor.  The remainder
			// (currently 96 bits spread over 4 uint32_ts) will be < divisor.
			//
			sdlTmp.u.Lo = rem[2];
			sdlTmp.u.Hi = rem[3];

			quo[2] = 0;
			quo[1] = Div96By64(&rem[1], sdlDivisor);
			quo[0] = Div96By64(rem, sdlDivisor);

			for (;;) {
				if ((rem[0] | rem[1]) == 0) {
					if (scale < 0) {
						cur_scale = min(9, -scale);
						goto HaveScale64;
					}
					break;
				}

				// Remainder is non-zero.  Scale up quotient and remainder by 
				// powers of 10 so we can compute more significant bits.
				// 
				cur_scale = SearchScale(quo[2], quo[1], quo [0], scale);
				if (cur_scale == 0) {
					// No more scaling to be done, but remainder is non-zero.
					// Round quotient.
					//
					sdlTmp.u.Lo = rem[0];
					sdlTmp.u.Hi = rem[1];
					if (sdlTmp.u.Hi >= 0x80000000 || (sdlTmp.int64 <<= 1) > sdlDivisor.int64 ||
					    (sdlTmp.int64 == sdlDivisor.int64 && (quo[0] & 1)))
						goto RoundUp;
					break;
				}

				if (cur_scale == -1)
					return MONO_DECIMAL_OVERFLOW;

			HaveScale64:
				pwr = power10[cur_scale];
				scale += cur_scale;

				if (IncreaseScale(quo, pwr) != 0)
					return MONO_DECIMAL_OVERFLOW;

				rem[2] = 0;  // rem is 64 bits, IncreaseScale uses 96
				IncreaseScale(rem, pwr);
				utmp = Div96By64(rem, sdlDivisor);
				quo[0] += utmp;
				if (quo[0] < utmp)
					if (++quo[1] == 0)
						quo[2]++;

			} // for (;;)
		}
		else {
			// Have a 96-bit divisor in divisor[].
			//
			// Start by finishing the shift left by cur_scale.
			//
			sdlTmp.u.Lo = divisor[1];
			sdlTmp.u.Hi = divisor[2];
			sdlTmp.int64 <<= cur_scale;
			divisor[0] = sdlDivisor.u.Lo;
			divisor[1] = sdlDivisor.u.Hi;
			divisor[2] = sdlTmp.u.Hi;

			// The remainder (currently 96 bits spread over 4 uint32_ts) 
			// will be < divisor.
			// 
			quo[2] = 0;
			quo[1] = 0;
			quo[0] = Div128By96(rem, divisor);

			for (;;) {
				if ((rem[0] | rem[1] | rem[2]) == 0) {
					if (scale < 0) {
						cur_scale = min(9, -scale);
						goto HaveScale96;
					}
					break;
				}

				// Remainder is non-zero.  Scale up quotient and remainder by 
				// powers of 10 so we can compute more significant bits.
				// 
				cur_scale = SearchScale(quo[2], quo[1], quo [0], scale);
				if (cur_scale == 0) {
					// No more scaling to be done, but remainder is non-zero.
					// Round quotient.
					//
					if (rem[2] >= 0x80000000)
						goto RoundUp;

					utmp = rem[0] > 0x80000000;
					utmp1 = rem[1] > 0x80000000;
					rem[0] <<= 1;
					rem[1] = (rem[1] << 1) + utmp;
					rem[2] = (rem[2] << 1) + utmp1;

					if ((rem[2] > divisor[2] || rem[2] == divisor[2]) &&
					    ((rem[1] > divisor[1] || rem[1] == divisor[1]) &&
					     ((rem[0] > divisor[0] || rem[0] == divisor[0]) &&
					      (quo[0] & 1))))
						goto RoundUp;
					break;
				}

				if (cur_scale == -1)
					return MONO_DECIMAL_OVERFLOW;

			HaveScale96:
				pwr = power10[cur_scale];
				scale += cur_scale;

				if (IncreaseScale(quo, pwr) != 0)
					return MONO_DECIMAL_OVERFLOW;

				rem[3] = IncreaseScale(rem, pwr);
				utmp = Div128By96(rem, divisor);
				quo[0] += utmp;
				if (quo[0] < utmp)
					if (++quo[1] == 0)
						quo[2]++;

			} // for (;;)
		}
	}

	// No more remainder.  Try extracting any extra powers of 10 we may have 
	// added.  We do this by trying to divide out 10^8, 10^4, 10^2, and 10^1.
	// If a division by one of these powers returns a zero remainder, then
	// we keep the quotient.  If the remainder is not zero, then we restore
	// the previous value.
	// 
	// Since 10 = 2 * 5, there must be a factor of 2 for every power of 10
	// we can extract.  We use this as a quick test on whether to try a
	// given power.
	// 
	while ((quo[0] & 0xFF) == 0 && scale >= 8) {
		quoSave[0] = quo[0];
		quoSave[1] = quo[1];
		quoSave[2] = quo[2];

		if (Div96By32(quoSave, 100000000) == 0) {
			quo[0] = quoSave[0];
			quo[1] = quoSave[1];
			quo[2] = quoSave[2];
			scale -= 8;
		}
		else
			break;
	}

	if ((quo[0] & 0xF) == 0 && scale >= 4) {
		quoSave[0] = quo[0];
		quoSave[1] = quo[1];
		quoSave[2] = quo[2];

		if (Div96By32(quoSave, 10000) == 0) {
			quo[0] = quoSave[0];
			quo[1] = quoSave[1];
			quo[2] = quoSave[2];
			scale -= 4;
		}
	}

	if ((quo[0] & 3) == 0 && scale >= 2) {
		quoSave[0] = quo[0];
		quoSave[1] = quo[1];
		quoSave[2] = quo[2];

		if (Div96By32(quoSave, 100) == 0) {
			quo[0] = quoSave[0];
			quo[1] = quoSave[1];
			quo[2] = quoSave[2];
			scale -= 2;
		}
	}

	if ((quo[0] & 1) == 0 && scale >= 1) {
		quoSave[0] = quo[0];
		quoSave[1] = quo[1];
		quoSave[2] = quo[2];

		if (Div96By32(quoSave, 10) == 0) {
			quo[0] = quoSave[0];
			quo[1] = quoSave[1];
			quo[2] = quoSave[2];
			scale -= 1;
		}
	}

	result->Hi32 = quo[2];
	result->v.v.Mid32 = quo[1];
	result->v.v.Lo32 = quo[0];
	result->u.u.scale = scale;
	result->u.u.sign = left->u.u.sign ^ right->u.u.sign;
	return MONO_DECIMAL_OK;
}

// mono_decimal_absolute - Decimal Absolute Value
static void G_GNUC_UNUSED
mono_decimal_absolute (MonoDecimal *pdecOprd, MonoDecimal *result)
{
	COPYDEC(*result, *pdecOprd);
	result->u.u.sign &= ~DECIMAL_NEG;
	// Microsoft does not set reserved here
}

// mono_decimal_fix - Decimal Fix (chop to integer)
static void
mono_decimal_fix (MonoDecimal *pdecOprd, MonoDecimal *result)
{
	DecFixInt(result, pdecOprd);
}

// mono_decimal_round_to_int - Decimal Int (round down to integer)
static void
mono_decimal_round_to_int (MonoDecimal *pdecOprd, MonoDecimal *result)
{
	if (DecFixInt(result, pdecOprd) != 0 && (result->u.u.sign & DECIMAL_NEG)) {
		// We have chopped off a non-zero amount from a negative value.  Since
		// we round toward -infinity, we must increase the integer result by
		// 1 to make it more negative.  This will never overflow because
		// in order to have a remainder, we must have had a non-zero scale factor.
		// Our scale factor is back to zero now.
		//
		DECIMAL_LO64_SET(*result, DECIMAL_LO64_GET(*result) + 1);
		if (DECIMAL_LO64_GET(*result) == 0)
			result->Hi32++;
	}
}

// mono_decimal_negate - Decimal Negate
static void G_GNUC_UNUSED
mono_decimal_negate (MonoDecimal *pdecOprd, MonoDecimal *result)
{
	COPYDEC(*result, *pdecOprd);
	// Microsoft does not set result->reserved to zero on this case.
	result->u.u.sign ^= DECIMAL_NEG;
}

//
// Returns: MONO_DECIMAL_INVALID_ARGUMENT, MONO_DECIMAL_OK
//
static MonoDecimalStatus
mono_decimal_round_result(MonoDecimal *input, int cDecimals, MonoDecimal *result)
{
	uint32_t num[3];
	uint32_t rem;
	uint32_t sticky;
	uint32_t pwr;
	int scale;

	if (cDecimals < 0)
		return MONO_DECIMAL_INVALID_ARGUMENT;

	scale = input->u.u.scale - cDecimals;
	if (scale > 0) {
		num[0] = input->v.v.Lo32;
		num[1] = input->v.v.Mid32;
		num[2] = input->Hi32;
		result->u.u.sign = input->u.u.sign;
		rem = sticky = 0;

		do {
			sticky |= rem;
			if (scale > POWER10_MAX)
				pwr = ten_to_nine;
			else
				pwr = power10[scale];

			rem = Div96By32(num, pwr);
			scale -= 9;
		}while (scale > 0);

		// Now round.  rem has last remainder, sticky has sticky bits.
		// To do IEEE rounding, we add LSB of result to sticky bits so
		// either causes round up if remainder * 2 == last divisor.
		//
		sticky |= num[0] & 1;
		rem = (rem << 1) + (sticky != 0);
		if (pwr < rem &&
		    ++num[0] == 0 &&
		    ++num[1] == 0
			)
			++num[2];

		result->v.v.Lo32 = num[0];
		result->v.v.Mid32 = num[1];
		result->Hi32 = num[2];
		result->u.u.scale = cDecimals;
		return MONO_DECIMAL_OK;
	}

	COPYDEC(*result, *input);
	// Odd, the Microsoft source does not set the result->reserved to zero here.
	return MONO_DECIMAL_OK;
}

//
// Returns MONO_DECIMAL_OK or MONO_DECIMAL_OVERFLOW
static MonoDecimalStatus
mono_decimal_from_float (float input_f, MonoDecimal* result)
{
	int         exp;    // number of bits to left of binary point
	int         power;
	uint32_t       mant;
	double      dbl;
	SPLIT64     sdlLo;
	SPLIT64     sdlHi;
	int         lmax, cur;  // temps used during scale reduction
	MonoSingle_float input = { .f = input_f };

	// The most we can scale by is 10^28, which is just slightly more
	// than 2^93.  So a float with an exponent of -94 could just
	// barely reach 0.5, but smaller exponents will always round to zero.
	//
	if ((exp = input.s.exp - MONO_SINGLE_BIAS) < -94 ) {
		DECIMAL_SETZERO(*result);
		return MONO_DECIMAL_OK;
	}

	if (exp > 96)
		return MONO_DECIMAL_OVERFLOW;

	// Round the input to a 7-digit integer.  The R4 format has
	// only 7 digits of precision, and we want to keep garbage digits
	// out of the Decimal were making.
	//
	// Calculate max power of 10 input value could have by multiplying 
	// the exponent by log10(2).  Using scaled integer multiplcation, 
	// log10(2) * 2 ^ 16 = .30103 * 65536 = 19728.3.
	//
	dbl = fabs(input.f);
	power = 6 - ((exp * 19728) >> 16);
	
	if (power >= 0) {
		// We have less than 7 digits, scale input up.
		//
		if (power > DECMAX)
			power = DECMAX;
		
		dbl = dbl * double_power10[power];
	} else {
		if (power != -1 || dbl >= 1E7)
			dbl = dbl / fnDblPower10(-power);
		else 
			power = 0; // didn't scale it
	}
	
	g_assert (dbl < 1E7);
	if (dbl < 1E6 && power < DECMAX) {
		dbl *= 10;
		power++;
		g_assert(dbl >= 1E6);
	}
	
	// Round to integer
	//
	mant = (int32_t)dbl;
	dbl -= (double)mant;  // difference between input & integer
	if ( dbl > 0.5 || (dbl == 0.5 && (mant & 1)))
		mant++;
	
	if (mant == 0) {
		DECIMAL_SETZERO(*result);
		return MONO_DECIMAL_OK;
	}
	
	if (power < 0) {
		// Add -power factors of 10, -power <= (29 - 7) = 22.
		//
		power = -power;
		if (power < 10) {
			sdlLo.int64 = UInt32x32To64(mant, (uint32_t)long_power10[power]);
			
			DECIMAL_LO32(*result) = sdlLo.u.Lo;
			DECIMAL_MID32(*result) = sdlLo.u.Hi;
			DECIMAL_HI32(*result) = 0;
		} else {
			// Have a big power of 10.
			//
			if (power > 18) {
				sdlLo.int64 = UInt32x32To64(mant, (uint32_t)long_power10[power - 18]);
				sdlLo.int64 = UInt64x64To128(sdlLo, ten_to_eighteen, &sdlHi.int64);
				
				if (sdlHi.u.Hi != 0)
					return MONO_DECIMAL_OVERFLOW;
			}
			else {
				sdlLo.int64 = UInt32x32To64(mant, (uint32_t)long_power10[power - 9]);
				sdlHi.int64 = UInt32x32To64(ten_to_nine, sdlLo.u.Hi);
				sdlLo.int64 = UInt32x32To64(ten_to_nine, sdlLo.u.Lo);
				sdlHi.int64 += sdlLo.u.Hi;
				sdlLo.u.Hi = sdlHi.u.Lo;
				sdlHi.u.Lo = sdlHi.u.Hi;
			}
			DECIMAL_LO32(*result) = sdlLo.u.Lo;
			DECIMAL_MID32(*result) = sdlLo.u.Hi;
			DECIMAL_HI32(*result) = sdlHi.u.Lo;
		}
		DECIMAL_SCALE(*result) = 0;
	} else {
		// Factor out powers of 10 to reduce the scale, if possible.
		// The maximum number we could factor out would be 6.  This
		// comes from the fact we have a 7-digit number, and the
		// MSD must be non-zero -- but the lower 6 digits could be
		// zero.  Note also the scale factor is never negative, so
		// we can't scale by any more than the power we used to
		// get the integer.
		//
		// DivMod32by32 returns the quotient in Lo, the remainder in Hi.
		//
		lmax = min(power, 6);
		
		// lmax is the largest power of 10 to try, lmax <= 6.
		// We'll try powers 4, 2, and 1 unless they're too big.
		//
		for (cur = 4; cur > 0; cur >>= 1)
		{
			if (cur > lmax)
				continue;
			
			sdlLo.int64 = DivMod32by32(mant, (uint32_t)long_power10[cur]);
			
			if (sdlLo.u.Hi == 0) {
				mant = sdlLo.u.Lo;
				power -= cur;
				lmax -= cur;
			}
		}
		DECIMAL_LO32(*result) = mant;
		DECIMAL_MID32(*result) = 0;
		DECIMAL_HI32(*result) = 0;
		DECIMAL_SCALE(*result) = power;
	}
	
	DECIMAL_SIGN(*result) = (char)input.s.sign << 7;
	return MONO_DECIMAL_OK;
}

// Returns MONO_DECIMAL_OK or MONO_DECIMAL_OVERFLOW
static MonoDecimalStatus
mono_decimal_from_double (double input_d, MonoDecimal *result)
{
	int         exp;    // number of bits to left of binary point
	int         power;  // power-of-10 scale factor
	SPLIT64     sdlMant;
	SPLIT64     sdlLo;
	double      dbl;
	int         lmax, cur;  // temps used during scale reduction
	uint32_t       pwr_cur;
	uint32_t       quo;
	MonoDouble_double input = { .d = input_d };
	
	// The most we can scale by is 10^28, which is just slightly more
	// than 2^93.  So a float with an exponent of -94 could just
	// barely reach 0.5, but smaller exponents will always round to zero.
	//
	if ((exp = input.s.exp - MONO_DOUBLE_BIAS) < -94) {
		DECIMAL_SETZERO(*result);
		return MONO_DECIMAL_OK;
	}

	if (exp > 96)
		return MONO_DECIMAL_OVERFLOW;

	// Round the input to a 15-digit integer.  The R8 format has
	// only 15 digits of precision, and we want to keep garbage digits
	// out of the Decimal were making.
	//
	// Calculate max power of 10 input value could have by multiplying 
	// the exponent by log10(2).  Using scaled integer multiplcation, 
	// log10(2) * 2 ^ 16 = .30103 * 65536 = 19728.3.
	//
	dbl = fabs(input.d);
	power = 14 - ((exp * 19728) >> 16);
	
	if (power >= 0) {
		// We have less than 15 digits, scale input up.
		//
		if (power > DECMAX)
			power = DECMAX;

		dbl = dbl * double_power10[power];
	} else {
		if (power != -1 || dbl >= 1E15)
			dbl = dbl / fnDblPower10(-power);
		else 
			power = 0; // didn't scale it
	}

	g_assert (dbl < 1E15);
	if (dbl < 1E14 && power < DECMAX) {
		dbl *= 10;
		power++;
		g_assert(dbl >= 1E14);
	}

	// Round to int64
	//
	sdlMant.int64 = (int64_t)dbl;
	dbl -= (double)(int64_t)sdlMant.int64;  // dif between input & integer
	if ( dbl > 0.5 || (dbl == 0.5 && (sdlMant.u.Lo & 1)))
		sdlMant.int64++;

	if (sdlMant.int64 == 0) {
		DECIMAL_SETZERO(*result);
		return MONO_DECIMAL_OK;
	}

	if (power < 0) {
		// Add -power factors of 10, -power <= (29 - 15) = 14.
		//
		power = -power;
		if (power < 10) {
			sdlLo.int64 = UInt32x32To64(sdlMant.u.Lo, (uint32_t)long_power10[power]);
			sdlMant.int64 = UInt32x32To64(sdlMant.u.Hi, (uint32_t)long_power10[power]);
			sdlMant.int64 += sdlLo.u.Hi;
			sdlLo.u.Hi = sdlMant.u.Lo;
			sdlMant.u.Lo = sdlMant.u.Hi;
		}
		else {
			// Have a big power of 10.
			//
			g_assert(power <= 14);
			sdlLo.int64 = UInt64x64To128(sdlMant, sdl_power10[power-10], &sdlMant.int64);

			if (sdlMant.u.Hi != 0)
				return MONO_DECIMAL_OVERFLOW;
		}
		DECIMAL_LO32(*result) = sdlLo.u.Lo;
		DECIMAL_MID32(*result) = sdlLo.u.Hi;
		DECIMAL_HI32(*result) = sdlMant.u.Lo;
		DECIMAL_SCALE(*result) = 0;
	}
	else {
		// Factor out powers of 10 to reduce the scale, if possible.
		// The maximum number we could factor out would be 14.  This
		// comes from the fact we have a 15-digit number, and the 
		// MSD must be non-zero -- but the lower 14 digits could be 
		// zero.  Note also the scale factor is never negative, so
		// we can't scale by any more than the power we used to
		// get the integer.
		//
		// DivMod64by32 returns the quotient in Lo, the remainder in Hi.
		//
		lmax = min(power, 14);

		// lmax is the largest power of 10 to try, lmax <= 14.
		// We'll try powers 8, 4, 2, and 1 unless they're too big.
		//
		for (cur = 8; cur > 0; cur >>= 1)
		{
			if (cur > lmax)
				continue;

			pwr_cur = (uint32_t)long_power10[cur];

			if (sdlMant.u.Hi >= pwr_cur) {
				// Overflow if we try to divide in one step.
				//
				sdlLo.int64 = DivMod64by32(sdlMant.u.Hi, pwr_cur);
				quo = sdlLo.u.Lo;
				sdlLo.u.Lo = sdlMant.u.Lo;
				sdlLo.int64 = DivMod64by32(sdlLo.int64, pwr_cur);
			}
			else {
				quo = 0;
				sdlLo.int64 = DivMod64by32(sdlMant.int64, pwr_cur);
			}

			if (sdlLo.u.Hi == 0) {
				sdlMant.u.Hi = quo;
				sdlMant.u.Lo = sdlLo.u.Lo;
				power -= cur;
				lmax -= cur;
			}
		}

		DECIMAL_HI32(*result) = 0;
		DECIMAL_SCALE(*result) = power;
		DECIMAL_LO32(*result) = sdlMant.u.Lo;
		DECIMAL_MID32(*result) = sdlMant.u.Hi;
	}

	DECIMAL_SIGN(*result) = (char)input.s.sign << 7;
	return MONO_DECIMAL_OK;
}

// Returns: MONO_DECIMAL_OK, or MONO_DECIMAL_INVALID_ARGUMENT
static MonoDecimalStatus
mono_decimal_to_double_result(MonoDecimal *input, double *result)
{
	SPLIT64  tmp;
	double   dbl;
	
	if (DECIMAL_SCALE(*input) > DECMAX || (DECIMAL_SIGN(*input) & ~DECIMAL_NEG) != 0)
		return MONO_DECIMAL_INVALID_ARGUMENT;
	
	tmp.u.Lo = DECIMAL_LO32(*input);
	tmp.u.Hi = DECIMAL_MID32(*input);
	
	if ((int32_t)DECIMAL_MID32(*input) < 0)
		dbl = (ds2to64.d + (double)(int64_t)tmp.int64 +
		       (double)DECIMAL_HI32(*input) * ds2to64.d) / fnDblPower10(DECIMAL_SCALE(*input)) ;
	else
		dbl = ((double)(int64_t)tmp.int64 +
		       (double)DECIMAL_HI32(*input) * ds2to64.d) / fnDblPower10(DECIMAL_SCALE(*input));
	
	if (DECIMAL_SIGN(*input))
		dbl = -dbl;
	
	*result = dbl;
	return MONO_DECIMAL_OK;
}

// Returns: MONO_DECIMAL_OK, or MONO_DECIMAL_INVALID_ARGUMENT
static MonoDecimalStatus
mono_decimal_to_float_result(MonoDecimal *input, float *result)
{
	double   dbl;
	
	if (DECIMAL_SCALE(*input) > DECMAX || (DECIMAL_SIGN(*input) & ~DECIMAL_NEG) != 0)
		return MONO_DECIMAL_INVALID_ARGUMENT;
	
	// Can't overflow; no errors possible.
	//
	mono_decimal_to_double_result(input, &dbl);
	*result = (float)dbl;
	return MONO_DECIMAL_OK;
}

static void
DecShiftLeft(MonoDecimal* value)
{
	unsigned int c0 = DECIMAL_LO32(*value) & 0x80000000? 1: 0;
    unsigned int c1 = DECIMAL_MID32(*value) & 0x80000000? 1: 0;
    g_assert(value != NULL);

    DECIMAL_LO32(*value) <<= 1;
    DECIMAL_MID32(*value) = DECIMAL_MID32(*value) << 1 | c0;
    DECIMAL_HI32(*value) = DECIMAL_HI32(*value) << 1 | c1;
}

static int
D32AddCarry(uint32_t* value, uint32_t i)
{
    uint32_t v = *value;
    uint32_t sum = v + i;
    *value = sum;
    return sum < v || sum < i? 1: 0;
}

static void
DecAdd(MonoDecimal *value, MonoDecimal* d)
{
	g_assert(value != NULL && d != NULL);

	if (D32AddCarry(&DECIMAL_LO32(*value), DECIMAL_LO32(*d))) {
		if (D32AddCarry(&DECIMAL_MID32(*value), 1)) {
			D32AddCarry(&DECIMAL_HI32(*value), 1);
		}
	}
	if (D32AddCarry(&DECIMAL_MID32(*value), DECIMAL_MID32(*d))) {
		D32AddCarry(&DECIMAL_HI32(*value), 1);
	}
	D32AddCarry(&DECIMAL_HI32(*value), DECIMAL_HI32(*d));
}

static void
DecMul10(MonoDecimal* value)
{
	MonoDecimal d = *value;
	g_assert (value != NULL);

	DecShiftLeft(value);
	DecShiftLeft(value);
	DecAdd(value, &d);
	DecShiftLeft(value);
}

static void
DecAddInt32(MonoDecimal* value, unsigned int i)
{
	g_assert(value != NULL);

	if (D32AddCarry(&DECIMAL_LO32(*value), i)) {
		if (D32AddCarry(&DECIMAL_MID32(*value), 1)) {
			D32AddCarry(&DECIMAL_HI32(*value), 1);
		}
	}
}

MonoDecimalCompareResult
mono_decimal_compare (MonoDecimal *left, MonoDecimal *right)
{
	uint32_t   left_sign;
	uint32_t   right_sign;
	MonoDecimal result;

	result.Hi32 = 0; 	// Just to shut up the compiler

	// First check signs and whether either are zero.  If both are
	// non-zero and of the same sign, just use subtraction to compare.
	//
	left_sign = left->v.v.Lo32 | left->v.v.Mid32 | left->Hi32;
	right_sign = right->v.v.Lo32 | right->v.v.Mid32 | right->Hi32;
	if (left_sign != 0)
		left_sign = (left->u.u.sign & DECIMAL_NEG) | 1;

	if (right_sign != 0)
		right_sign = (right->u.u.sign & DECIMAL_NEG) | 1;

	// left_sign & right_sign have values 1, 0, or 0x81 depending on if the left/right
	// operand is +, 0, or -.
	//
	if (left_sign == right_sign) {
		if (left_sign == 0)    // both are zero
			return MONO_DECIMAL_CMP_EQ; // return equal

		DecAddSub(left, right, &result, DECIMAL_NEG);
		if (DECIMAL_LO64_GET(result) == 0 && result.Hi32 == 0)
			return MONO_DECIMAL_CMP_EQ;
		if (result.u.u.sign & DECIMAL_NEG)
			return MONO_DECIMAL_CMP_LT;
		return MONO_DECIMAL_CMP_GT;
	}

	//
	// Signs are different.  Use signed byte comparison
	//
	if ((signed char)left_sign > (signed char)right_sign)
		return MONO_DECIMAL_CMP_GT;
	return MONO_DECIMAL_CMP_LT;
}

void
mono_decimal_init_single (MonoDecimal *_this, float value)
{
	if (mono_decimal_from_float (value, _this) == MONO_DECIMAL_OVERFLOW) {
		mono_set_pending_exception (mono_get_exception_overflow ());
		return;
	}
	_this->reserved = 0;
}

void
mono_decimal_init_double (MonoDecimal *_this, double value)
{
	if (mono_decimal_from_double (value, _this) == MONO_DECIMAL_OVERFLOW) {
		mono_set_pending_exception (mono_get_exception_overflow ());
		return;
	}
	_this->reserved = 0;
}

void
mono_decimal_floor (MonoDecimal *d)
{
	MonoDecimal decRes;

	mono_decimal_round_to_int(d, &decRes);
	
	// copy decRes into d
	COPYDEC(*d, decRes);
	d->reserved = 0;
	FC_GC_POLL ();
}

int32_t
mono_decimal_get_hash_code (MonoDecimal *d)
{
	double dbl;

	if (mono_decimal_to_double_result(d, &dbl) != MONO_DECIMAL_OK)
		return 0;
	
	if (dbl == 0.0) {
		// Ensure 0 and -0 have the same hash code
		return 0;
	}
	// conversion to double is lossy and produces rounding errors so we mask off the lowest 4 bits
	// 
	// For example these two numerically equal decimals with different internal representations produce
	// slightly different results when converted to double:
	//
	// decimal a = new decimal(new int[] { 0x76969696, 0x2fdd49fa, 0x409783ff, 0x00160000 });
	//                     => (decimal)1999021.176470588235294117647000000000 => (double)1999021.176470588
	// decimal b = new decimal(new int[] { 0x3f0f0f0f, 0x1e62edcc, 0x06758d33, 0x00150000 }); 
	//                     => (decimal)1999021.176470588235294117647000000000 => (double)1999021.1764705882
	//
	return ((((int *)&dbl)[0]) & 0xFFFFFFF0) ^ ((int *)&dbl)[1];
	
}

void
mono_decimal_multiply (MonoDecimal *d1, MonoDecimal *d2)
{
	MonoDecimal decRes;

	MonoDecimalStatus status = mono_decimal_multiply_result(d1, d2, &decRes);
	if (status != MONO_DECIMAL_OK) {
		mono_set_pending_exception (mono_get_exception_overflow ());
		return;
	}

	COPYDEC(*d1, decRes);
	d1->reserved = 0;

	FC_GC_POLL ();
}

void
mono_decimal_round (MonoDecimal *d, int32_t decimals)
{
	MonoDecimal decRes;
	
	// GC is only triggered for throwing, no need to protect result 
	if (decimals < 0 || decimals > 28) {
		mono_set_pending_exception (mono_get_exception_argument_out_of_range ("d"));
		return;
	}

	mono_decimal_round_result(d, decimals, &decRes);

	// copy decRes into d
	COPYDEC(*d, decRes);
	d->reserved = 0;

	FC_GC_POLL();
}

void
mono_decimal_tocurrency (MonoDecimal *decimal)
{
	// TODO
}

double
mono_decimal_to_double (MonoDecimal d)
{
	double result = 0.0;
	// Note: this can fail if the input is an invalid decimal, but for compatibility we should return 0
	mono_decimal_to_double_result(&d, &result);
	return result;
}

int32_t
mono_decimal_to_int32 (MonoDecimal d)
{
	MonoDecimal result;
	
	// The following can not return an error, it only returns INVALID_ARG if the decimals is < 0
	mono_decimal_round_result(&d, 0, &result);
	
	if (DECIMAL_SCALE(result) != 0) {
		d = result;
		mono_decimal_fix (&d, &result);
	}
	
	if (DECIMAL_HI32(result) == 0 && DECIMAL_MID32(result) == 0) {
		int32_t i = DECIMAL_LO32(result);
		if ((int16_t)DECIMAL_SIGNSCALE(result) >= 0) {
			if (i >= 0)
				return i;
		} else {
			i = -i;
			if (i <= 0)
				return i;
		}
	}
	
	mono_set_pending_exception (mono_get_exception_overflow ());
	return 0;
}

float
mono_decimal_to_float (MonoDecimal d)
{
	float result = 0.0f;
	// Note: this can fail if the input is an invalid decimal, but for compatibility we should return 0
	mono_decimal_to_float_result(&d, &result);
	return result;
}

void
mono_decimal_truncate (MonoDecimal *d)
{
	MonoDecimal decRes;

	mono_decimal_fix(d, &decRes);

	// copy decRes into d
	COPYDEC(*d, decRes);
	d->reserved = 0;
	FC_GC_POLL();
}

void
mono_decimal_addsub (MonoDecimal *left, MonoDecimal *right, uint8_t sign)
{
	MonoDecimal result, decTmp;
	MonoDecimal *pdecTmp, *leftOriginal;
	uint32_t    num[6], pwr;
	int         scale, hi_prod, cur;
	SPLIT64     sdlTmp;
	
	g_assert(sign == 0 || sign == DECIMAL_NEG);

	leftOriginal = left;

	sign ^= (DECIMAL_SIGN(*right) ^ DECIMAL_SIGN(*left)) & DECIMAL_NEG;

	if (DECIMAL_SCALE(*right) == DECIMAL_SCALE(*left)) {
		// Scale factors are equal, no alignment necessary.
		//
		DECIMAL_SIGNSCALE(result) = DECIMAL_SIGNSCALE(*left);

	AlignedAdd:
		if (sign) {
			// Signs differ - subtract
			//
			DECIMAL_LO64_SET(result, (DECIMAL_LO64_GET(*left) - DECIMAL_LO64_GET(*right)));
			DECIMAL_HI32(result) = DECIMAL_HI32(*left) - DECIMAL_HI32(*right);

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(result) > DECIMAL_LO64_GET(*left)) {
				DECIMAL_HI32(result)--;
				if (DECIMAL_HI32(result) >= DECIMAL_HI32(*left))
					goto SignFlip;
			} else if (DECIMAL_HI32(result) > DECIMAL_HI32(*left)) {
				// Got negative result.  Flip its sign.
				// 
			SignFlip:
				DECIMAL_LO64_SET(result, -(int64_t)DECIMAL_LO64_GET(result));
				DECIMAL_HI32(result) = ~DECIMAL_HI32(result);
				if (DECIMAL_LO64_GET(result) == 0)
					DECIMAL_HI32(result)++;
				DECIMAL_SIGN(result) ^= DECIMAL_NEG;
			}

		} else {
			// Signs are the same - add
			//
			DECIMAL_LO64_SET(result, (DECIMAL_LO64_GET(*left) + DECIMAL_LO64_GET(*right)));
			DECIMAL_HI32(result) = DECIMAL_HI32(*left) + DECIMAL_HI32(*right);

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(result) < DECIMAL_LO64_GET(*left)) {
				DECIMAL_HI32(result)++;
				if (DECIMAL_HI32(result) <= DECIMAL_HI32(*left))
					goto AlignedScale;
			} else if (DECIMAL_HI32(result) < DECIMAL_HI32(*left)) {
			AlignedScale:
				// The addition carried above 96 bits.  Divide the result by 10,
				// dropping the scale factor.
				// 
				if (DECIMAL_SCALE(result) == 0) {
					mono_set_pending_exception (mono_get_exception_overflow ());
					return;
				}
				DECIMAL_SCALE(result)--;

				sdlTmp.u.Lo = DECIMAL_HI32(result);
				sdlTmp.u.Hi = 1;
				sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
				DECIMAL_HI32(result) = sdlTmp.u.Lo;

				sdlTmp.u.Lo = DECIMAL_MID32(result);
				sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
				DECIMAL_MID32(result) = sdlTmp.u.Lo;

				sdlTmp.u.Lo = DECIMAL_LO32(result);
				sdlTmp.int64 = DivMod64by32(sdlTmp.int64, 10);
				DECIMAL_LO32(result) = sdlTmp.u.Lo;

				// See if we need to round up.
				//
				if (sdlTmp.u.Hi >= 5 && (sdlTmp.u.Hi > 5 || (DECIMAL_LO32(result) & 1))) {
					DECIMAL_LO64_SET(result, DECIMAL_LO64_GET(result)+1);
					if (DECIMAL_LO64_GET(result) == 0)
						DECIMAL_HI32(result)++;
				}
			}
		}
	} else {
		// Scale factors are not equal.  Assume that a larger scale
		// factor (more decimal places) is likely to mean that number
		// is smaller.  Start by guessing that the right operand has
		// the larger scale factor.  The result will have the larger
		// scale factor.
		//
		DECIMAL_SCALE(result) = DECIMAL_SCALE(*right);  // scale factor of "smaller"
		DECIMAL_SIGN(result) = DECIMAL_SIGN(*left);    // but sign of "larger"
		scale = DECIMAL_SCALE(result)- DECIMAL_SCALE(*left);

		if (scale < 0) {
			// Guessed scale factor wrong. Swap operands.
			//
			scale = -scale;
			DECIMAL_SCALE(result) = DECIMAL_SCALE(*left);
			DECIMAL_SIGN(result) ^= sign;
			pdecTmp = right;
			right = left;
			left = pdecTmp;
		}

		// *left will need to be multiplied by 10^scale so
		// it will have the same scale as *right.  We could be
		// extending it to up to 192 bits of precision.
		//
		if (scale <= POWER10_MAX) {
			// Scaling won't make it larger than 4 uint32_ts
			//
			pwr = power10[scale];
			DECIMAL_LO64_SET(decTmp, UInt32x32To64(DECIMAL_LO32(*left), pwr));
			sdlTmp.int64 = UInt32x32To64(DECIMAL_MID32(*left), pwr);
			sdlTmp.int64 += DECIMAL_MID32(decTmp);
			DECIMAL_MID32(decTmp) = sdlTmp.u.Lo;
			DECIMAL_HI32(decTmp) = sdlTmp.u.Hi;
			sdlTmp.int64 = UInt32x32To64(DECIMAL_HI32(*left), pwr);
			sdlTmp.int64 += DECIMAL_HI32(decTmp);
			if (sdlTmp.u.Hi == 0) {
				// Result fits in 96 bits.  Use standard aligned add.
				//
				DECIMAL_HI32(decTmp) = sdlTmp.u.Lo;
				left = &decTmp;
				goto AlignedAdd;
			}
			num[0] = DECIMAL_LO32(decTmp);
			num[1] = DECIMAL_MID32(decTmp);
			num[2] = sdlTmp.u.Lo;
			num[3] = sdlTmp.u.Hi;
			hi_prod = 3;
		} else {
			// Have to scale by a bunch.  Move the number to a buffer
			// where it has room to grow as it's scaled.
			//
			num[0] = DECIMAL_LO32(*left);
			num[1] = DECIMAL_MID32(*left);
			num[2] = DECIMAL_HI32(*left);
			hi_prod = 2;

			// Scan for zeros in the upper words.
			//
			if (num[2] == 0) {
				hi_prod = 1;
				if (num[1] == 0) {
					hi_prod = 0;
					if (num[0] == 0) {
						// Left arg is zero, return right.
						//
						DECIMAL_LO64_SET(result, DECIMAL_LO64_GET(*right));
						DECIMAL_HI32(result) = DECIMAL_HI32(*right);
						DECIMAL_SIGN(result) ^= sign;
						goto RetDec;
					}
				}
			}

			// Scaling loop, up to 10^9 at a time.  hi_prod stays updated
			// with index of highest non-zero uint32_t.
			//
			for (; scale > 0; scale -= POWER10_MAX) {
				if (scale > POWER10_MAX)
					pwr = ten_to_nine;
				else
					pwr = power10[scale];

				sdlTmp.u.Hi = 0;
				for (cur = 0; cur <= hi_prod; cur++) {
					sdlTmp.int64 = UInt32x32To64(num[cur], pwr) + sdlTmp.u.Hi;
					num[cur] = sdlTmp.u.Lo;
				}

				if (sdlTmp.u.Hi != 0)
					// We're extending the result by another uint32_t.
					num[++hi_prod] = sdlTmp.u.Hi;
			}
		}

		// Scaling complete, do the add.  Could be subtract if signs differ.
		//
		sdlTmp.u.Lo = num[0];
		sdlTmp.u.Hi = num[1];

		if (sign) {
			// Signs differ, subtract.
			//
			DECIMAL_LO64_SET(result, (sdlTmp.int64 - DECIMAL_LO64_GET(*right)));
			DECIMAL_HI32(result) = num[2] - DECIMAL_HI32(*right);

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(result) > sdlTmp.int64) {
				DECIMAL_HI32(result)--;
				if (DECIMAL_HI32(result) >= num[2])
					goto LongSub;
			} else if (DECIMAL_HI32(result) > num[2]) {
			LongSub:
				// If num has more than 96 bits of precision, then we need to 
				// carry the subtraction into the higher bits.  If it doesn't, 
				// then we subtracted in the wrong order and have to flip the 
				// sign of the result.
				// 
				if (hi_prod <= 2)
					goto SignFlip;

				cur = 3;
				while(num[cur++]-- == 0);
				if (num[hi_prod] == 0)
					hi_prod--;
			}
		} else {
			// Signs the same, add.
			//
			DECIMAL_LO64_SET(result, (sdlTmp.int64 + DECIMAL_LO64_GET(*right)));
			DECIMAL_HI32(result) = num[2] + DECIMAL_HI32(*right);

			// Propagate carry
			//
			if (DECIMAL_LO64_GET(result) < sdlTmp.int64) {
				DECIMAL_HI32(result)++;
				if (DECIMAL_HI32(result) <= num[2])
					goto LongAdd;
			} else if (DECIMAL_HI32(result) < num[2]) {
			LongAdd:
				// Had a carry above 96 bits.
				//
				cur = 3;
				do {
					if (hi_prod < cur) {
						num[cur] = 1;
						hi_prod = cur;
						break;
					}
				}while (++num[cur++] == 0);
			}
		}

		if (hi_prod > 2) {
			num[0] = DECIMAL_LO32(result);
			num[1] = DECIMAL_MID32(result);
			num[2] = DECIMAL_HI32(result);
			DECIMAL_SCALE(result) = (uint8_t)ScaleResult(num, hi_prod, DECIMAL_SCALE(result));
			if (DECIMAL_SCALE(result) == (uint8_t)-1) {
				mono_set_pending_exception (mono_get_exception_overflow ());
				return;
			}

			DECIMAL_LO32(result) = num[0];
			DECIMAL_MID32(result) = num[1];
			DECIMAL_HI32(result) = num[2];
		}
	}

RetDec:
	left = leftOriginal;
	COPYDEC(*left, result);
	left->reserved = 0;
}

void
mono_decimal_divide (MonoDecimal *left, MonoDecimal *right)
{
	uint32_t quo[3], quo_save[3],rem[4], divisor[3];
	uint32_t pwr, tmp, tmp1;
	SPLIT64  sdlTmp, sdlDivisor;
	int      scale, cur_scale;
	gboolean unscale;

	scale = DECIMAL_SCALE(*left) - DECIMAL_SCALE(*right);
	unscale = FALSE;
	divisor[0] = DECIMAL_LO32(*right);
	divisor[1] = DECIMAL_MID32(*right);
	divisor[2] = DECIMAL_HI32(*right);

	if (divisor[1] == 0 && divisor[2] == 0) {
		// Divisor is only 32 bits.  Easy divide.
		//
		if (divisor[0] == 0) {
			mono_set_pending_exception (mono_get_exception_divide_by_zero ());
			return;
		}

		quo[0] = DECIMAL_LO32(*left);
		quo[1] = DECIMAL_MID32(*left);
		quo[2] = DECIMAL_HI32(*left);
		rem[0] = Div96By32(quo, divisor[0]);

		for (;;) {
			if (rem[0] == 0) {
				if (scale < 0) {
					cur_scale = min(9, -scale);
					goto HaveScale;
				}
				break;
			}
			// We need to unscale if and only if we have a non-zero remainder
			unscale = TRUE;

			// We have computed a quotient based on the natural scale 
			// ( <dividend scale> - <divisor scale> ).  We have a non-zero 
			// remainder, so now we should increase the scale if possible to 
			// include more quotient bits.
			// 
			// If it doesn't cause overflow, we'll loop scaling by 10^9 and 
			// computing more quotient bits as long as the remainder stays 
			// non-zero.  If scaling by that much would cause overflow, we'll 
			// drop out of the loop and scale by as much as we can.
			// 
			// Scaling by 10^9 will overflow if quo[2].quo[1] >= 2^32 / 10^9 
			// = 4.294 967 296.  So the upper limit is quo[2] == 4 and 
			// quo[1] == 0.294 967 296 * 2^32 = 1,266,874,889.7+.  Since 
			// quotient bits in quo[0] could be all 1's, then 1,266,874,888 
			// is the largest value in quo[1] (when quo[2] == 4) that is 
			// assured not to overflow.
			// 
			cur_scale = SearchScale(quo[2], quo[1], quo[0], scale);
			if (cur_scale == 0) {
				// No more scaling to be done, but remainder is non-zero.
				// Round quotient.
				//
				tmp = rem[0] << 1;
				if (tmp < rem[0] || (tmp >= divisor[0] &&
							   (tmp > divisor[0] || (quo[0] & 1)))) {
				RoundUp:
					if (!Add32To96(quo, 1)) {
						if (scale == 0) {
							mono_set_pending_exception (mono_get_exception_overflow ());
							return;
						}
						scale--;
						OverflowUnscale(quo, TRUE);
						break;
					}      
				}
				break;
			}

			if (cur_scale < 0) {
				mono_set_pending_exception (mono_get_exception_overflow ());
				return;
			}

		HaveScale:
			pwr = power10[cur_scale];
			scale += cur_scale;

			if (IncreaseScale(quo, pwr) != 0) {
				mono_set_pending_exception (mono_get_exception_overflow ());
				return;
			}

			sdlTmp.int64 = DivMod64by32(UInt32x32To64(rem[0], pwr), divisor[0]);
			rem[0] = sdlTmp.u.Hi;

			if (!Add32To96(quo, sdlTmp.u.Lo)) {
				if (scale == 0) {
					mono_set_pending_exception (mono_get_exception_overflow ());
					return;
				}
				scale--;
				OverflowUnscale(quo, (rem[0] != 0));
				break;
			}
		} // for (;;)
	} else {
		// Divisor has bits set in the upper 64 bits.
		//
		// Divisor must be fully normalized (shifted so bit 31 of the most 
		// significant uint32_t is 1).  Locate the MSB so we know how much to 
		// normalize by.  The dividend will be shifted by the same amount so 
		// the quotient is not changed.
		//
		if (divisor[2] == 0)
			tmp = divisor[1];
		else
			tmp = divisor[2];

		cur_scale = 0;
		if (!(tmp & 0xFFFF0000)) {
			cur_scale += 16;
			tmp <<= 16;
		}
		if (!(tmp & 0xFF000000)) {
			cur_scale += 8;
			tmp <<= 8;
		}
		if (!(tmp & 0xF0000000)) {
			cur_scale += 4;
			tmp <<= 4;
		}
		if (!(tmp & 0xC0000000)) {
			cur_scale += 2;
			tmp <<= 2;
		}
		if (!(tmp & 0x80000000)) {
			cur_scale++;
			tmp <<= 1;
		}
    
		// Shift both dividend and divisor left by cur_scale.
		// 
		sdlTmp.int64 = DECIMAL_LO64_GET(*left) << cur_scale;
		rem[0] = sdlTmp.u.Lo;
		rem[1] = sdlTmp.u.Hi;
		sdlTmp.u.Lo = DECIMAL_MID32(*left);
		sdlTmp.u.Hi = DECIMAL_HI32(*left);
		sdlTmp.int64 <<= cur_scale;
		rem[2] = sdlTmp.u.Hi;
		rem[3] = (DECIMAL_HI32(*left) >> (31 - cur_scale)) >> 1;

		sdlDivisor.u.Lo = divisor[0];
		sdlDivisor.u.Hi = divisor[1];
		sdlDivisor.int64 <<= cur_scale;

		if (divisor[2] == 0) {
			// Have a 64-bit divisor in sdlDivisor.  The remainder 
			// (currently 96 bits spread over 4 uint32_ts) will be < divisor.
			// 
			sdlTmp.u.Lo = rem[2];
			sdlTmp.u.Hi = rem[3];

			quo[2] = 0;
			quo[1] = Div96By64(&rem[1], sdlDivisor);
			quo[0] = Div96By64(rem, sdlDivisor);

			for (;;) {
				if ((rem[0] | rem[1]) == 0) {
					if (scale < 0) {
						cur_scale = min(9, -scale);
						goto HaveScale64;
					}
					break;
				}

				// We need to unscale if and only if we have a non-zero remainder
				unscale = TRUE;

				// Remainder is non-zero.  Scale up quotient and remainder by 
				// powers of 10 so we can compute more significant bits.
				// 
				cur_scale = SearchScale(quo[2], quo[1], quo[0], scale);
				if (cur_scale == 0) {
					// No more scaling to be done, but remainder is non-zero.
					// Round quotient.
					//
					sdlTmp.u.Lo = rem[0];
					sdlTmp.u.Hi = rem[1];
					if (sdlTmp.u.Hi >= 0x80000000 || (sdlTmp.int64 <<= 1) > sdlDivisor.int64 ||
					    (sdlTmp.int64 == sdlDivisor.int64 && (quo[0] & 1)))
						goto RoundUp;
					break;
				}

				if (cur_scale < 0) {
					mono_set_pending_exception (mono_get_exception_overflow ());
					return;
				}

			HaveScale64:
				pwr = power10[cur_scale];
				scale += cur_scale;

				if (IncreaseScale(quo, pwr) != 0) {
					mono_set_pending_exception (mono_get_exception_overflow ());
					return;
				}
				
				rem[2] = 0;  // rem is 64 bits, IncreaseScale uses 96
				IncreaseScale(rem, pwr);
				tmp = Div96By64(rem, sdlDivisor);
				if (!Add32To96(quo, tmp)) {
					if (scale == 0) {
						mono_set_pending_exception (mono_get_exception_overflow ());
						return;
					}
					scale--;
					OverflowUnscale(quo, (rem[0] != 0 || rem[1] != 0));
					break;
				}      

			} // for (;;)
		} else {
			// Have a 96-bit divisor in divisor[].
			//
			// Start by finishing the shift left by cur_scale.
			//
			sdlTmp.u.Lo = divisor[1];
			sdlTmp.u.Hi = divisor[2];
			sdlTmp.int64 <<= cur_scale;
			divisor[0] = sdlDivisor.u.Lo;
			divisor[1] = sdlDivisor.u.Hi;
			divisor[2] = sdlTmp.u.Hi;

			// The remainder (currently 96 bits spread over 4 uint32_ts) 
			// will be < divisor.
			// 
			quo[2] = 0;
			quo[1] = 0;
			quo[0] = Div128By96(rem, divisor);

			for (;;) {
				if ((rem[0] | rem[1] | rem[2]) == 0) {
					if (scale < 0) {
						cur_scale = min(9, -scale);
						goto HaveScale96;
					}
					break;
				}

				// We need to unscale if and only if we have a non-zero remainder
				unscale = TRUE;

				// Remainder is non-zero.  Scale up quotient and remainder by 
				// powers of 10 so we can compute more significant bits.
				// 
				cur_scale = SearchScale(quo[2], quo[1], quo[0], scale);
				if (cur_scale == 0) {
					// No more scaling to be done, but remainder is non-zero.
					// Round quotient.
					//
					if (rem[2] >= 0x80000000)
						goto RoundUp;

					tmp = rem[0] > 0x80000000;
					tmp1 = rem[1] > 0x80000000;
					rem[0] <<= 1;
					rem[1] = (rem[1] << 1) + tmp;
					rem[2] = (rem[2] << 1) + tmp1;

					if (rem[2] > divisor[2] || (rem[2] == divisor[2] && (rem[1] > divisor[1] || rem[1] == (divisor[1] && (rem[0] > divisor[0] || (rem[0] == divisor[0] && (quo[0] & 1)))))))
						goto RoundUp;
					break;
				}

				if (cur_scale < 0) {
					mono_set_pending_exception (mono_get_exception_overflow ());
					return;
				}
				
			HaveScale96:
				pwr = power10[cur_scale];
				scale += cur_scale;

				if (IncreaseScale(quo, pwr) != 0) {
					mono_set_pending_exception (mono_get_exception_overflow ());
					return;
				}

				rem[3] = IncreaseScale(rem, pwr);
				tmp = Div128By96(rem, divisor);
				if (!Add32To96(quo, tmp)) {
					if (scale == 0) {
						mono_set_pending_exception (mono_get_exception_overflow ());
						return;
					}
					
					scale--;
					OverflowUnscale(quo, (rem[0] != 0 || rem[1] != 0 || rem[2] != 0 || rem[3] != 0));
					break;
				}      

			} // for (;;)
		}
	}

	// We need to unscale if and only if we have a non-zero remainder
	if (unscale) {
		// Try extracting any extra powers of 10 we may have 
		// added.  We do this by trying to divide out 10^8, 10^4, 10^2, and 10^1.
		// If a division by one of these powers returns a zero remainder, then
		// we keep the quotient.  If the remainder is not zero, then we restore
		// the previous value.
		// 
		// Since 10 = 2 * 5, there must be a factor of 2 for every power of 10
		// we can extract.  We use this as a quick test on whether to try a
		// given power.
		// 
		while ((quo[0] & 0xFF) == 0 && scale >= 8) {
			quo_save[0] = quo[0];
			quo_save[1] = quo[1];
			quo_save[2] = quo[2];

			if (Div96By32(quo_save, 100000000) == 0) {
				quo[0] = quo_save[0];
				quo[1] = quo_save[1];
				quo[2] = quo_save[2];
				scale -= 8;
			} else
				break;
		}

		if ((quo[0] & 0xF) == 0 && scale >= 4) {
			quo_save[0] = quo[0];
			quo_save[1] = quo[1];
			quo_save[2] = quo[2];

			if (Div96By32(quo_save, 10000) == 0) {
				quo[0] = quo_save[0];
				quo[1] = quo_save[1];
				quo[2] = quo_save[2];
				scale -= 4;
			}
		}

		if ((quo[0] & 3) == 0 && scale >= 2) {
			quo_save[0] = quo[0];
			quo_save[1] = quo[1];
			quo_save[2] = quo[2];

			if (Div96By32(quo_save, 100) == 0) {
				quo[0] = quo_save[0];
				quo[1] = quo_save[1];
				quo[2] = quo_save[2];
				scale -= 2;
			}
		}

		if ((quo[0] & 1) == 0 && scale >= 1) {
			quo_save[0] = quo[0];
			quo_save[1] = quo[1];
			quo_save[2] = quo[2];

			if (Div96By32(quo_save, 10) == 0) {
				quo[0] = quo_save[0];
				quo[1] = quo_save[1];
				quo[2] = quo_save[2];
				scale -= 1;
			}
		}
	}

	DECIMAL_SIGN(*left) = DECIMAL_SIGN(*left) ^ DECIMAL_SIGN(*right);
	DECIMAL_HI32(*left) = quo[2];
	DECIMAL_MID32(*left) = quo[1];
	DECIMAL_LO32(*left) = quo[0];
	DECIMAL_SCALE(*left) = (uint8_t)scale;
	left->reserved = 0;

}

#define DECIMAL_PRECISION 29

int
mono_decimal_from_number (void *from, MonoDecimal *target)
{
	MonoNumber *number = (MonoNumber *) from;
	uint16_t* p = number->digits;
	MonoDecimal d;
	int e = number->scale;
	g_assert(number != NULL);
	g_assert(target != NULL);

	d.reserved = 0;
	DECIMAL_SIGNSCALE(d) = 0;
	DECIMAL_HI32(d) = 0;
	DECIMAL_LO32(d) = 0;
	DECIMAL_MID32(d) = 0;
	g_assert(p != NULL);
	if (!*p) {
		// To avoid risking an app-compat issue with pre 4.5 (where some app was illegally using Reflection to examine the internal scale bits), we'll only force
		// the scale to 0 if the scale was previously positive
		if (e > 0) {
			e = 0;
		}
	} else {
		if (e > DECIMAL_PRECISION) return 0;
		while ((e > 0 || (*p && e > -28)) && (DECIMAL_HI32(d) < 0x19999999 || (DECIMAL_HI32(d) == 0x19999999 && (DECIMAL_MID32(d) < 0x99999999 || (DECIMAL_MID32(d) == 0x99999999 && (DECIMAL_LO32(d) < 0x99999999 || (DECIMAL_LO32(d) == 0x99999999 && *p <= '5'))))))) {
			DecMul10(&d);
			if (*p)
				DecAddInt32(&d, *p++ - '0');
			e--;
		}
		if (*p++ >= '5') {
			gboolean round = TRUE;
			if (*(p-1) == '5' && *(p-2) % 2 == 0) { // Check if previous digit is even, only if the when we are unsure whether hows to do Banker's rounding
				// For digits > 5 we will be roundinp up anyway.
				int count = 20; // Look at the next 20 digits to check to round
				while (*p == '0' && count != 0) {
					p++;
					count--;
				}
				if (*p == '\0' || count == 0) 
					round = FALSE;// Do nothing
			}
			
			if (round) {
				DecAddInt32(&d, 1);
				if ((DECIMAL_HI32(d) | DECIMAL_MID32(d) | DECIMAL_LO32(d)) == 0) {
					DECIMAL_HI32(d) = 0x19999999;
					DECIMAL_MID32(d) = 0x99999999;
					DECIMAL_LO32(d) = 0x9999999A;
					e++;
				}
			}
		}
	}
	if (e > 0)
		return 0;
	if (e <= -DECIMAL_PRECISION) {
		// Parsing a large scale zero can give you more precision than fits in the decimal.
		// This should only happen for actual zeros or very small numbers that round to zero.
		DECIMAL_SIGNSCALE(d) = 0;
		DECIMAL_HI32(d) = 0;
		DECIMAL_LO32(d) = 0;
		DECIMAL_MID32(d) = 0;
		DECIMAL_SCALE(d) = (DECIMAL_PRECISION - 1);
	} else {
		DECIMAL_SCALE(d) = (uint8_t)(-e);
	}
	
	DECIMAL_SIGN(d) = number->sign? DECIMAL_NEG: 0;
	*target = d;
	return 1;
}


#endif
