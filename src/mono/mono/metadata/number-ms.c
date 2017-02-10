/**
 * \file
 * System.Double, System.Single and System.Number runtime support
 *
 * Author:
 *	Ludovic Henry (ludovic@xamarin.com)
 *
 * Copyright 2015 Xamarin, Inc (http://www.xamarin.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Files:
//  - src/classlibnative/bcltype/number.cpp
//
// Ported from C++ to C and adjusted to Mono runtime

#include <glib.h>

#include "number-ms.h"

static const guint64 rgval64Power10[] = {
	/* powers of 10 */
	0xa000000000000000LL,
	0xc800000000000000LL,
	0xfa00000000000000LL,
	0x9c40000000000000LL,
	0xc350000000000000LL,
	0xf424000000000000LL,
	0x9896800000000000LL,
	0xbebc200000000000LL,
	0xee6b280000000000LL,
	0x9502f90000000000LL,
	0xba43b74000000000LL,
	0xe8d4a51000000000LL,
	0x9184e72a00000000LL,
	0xb5e620f480000000LL,
	0xe35fa931a0000000LL,

	/* powers of 0.1 */
	0xcccccccccccccccdLL,
	0xa3d70a3d70a3d70bLL,
	0x83126e978d4fdf3cLL,
	0xd1b71758e219652eLL,
	0xa7c5ac471b478425LL,
	0x8637bd05af6c69b7LL,
	0xd6bf94d5e57a42beLL,
	0xabcc77118461ceffLL,
	0x89705f4136b4a599LL,
	0xdbe6fecebdedd5c2LL,
	0xafebff0bcb24ab02LL,
	0x8cbccc096f5088cfLL,
	0xe12e13424bb40e18LL,
	0xb424dc35095cd813LL,
	0x901d7cf73ab0acdcLL,
};

static const gint8 rgexp64Power10[] = {
	/* exponents for both powers of 10 and 0.1 */
	4,
	7,
	10,
	14,
	17,
	20,
	24,
	27,
	30,
	34,
	37,
	40,
	44,
	47,
	50,
};

static const guint64 rgval64Power10By16[] = {
	/* powers of 10^16 */
	0x8e1bc9bf04000000LL,
	0x9dc5ada82b70b59eLL,
	0xaf298d050e4395d6LL,
	0xc2781f49ffcfa6d4LL,
	0xd7e77a8f87daf7faLL,
	0xefb3ab16c59b14a0LL,
	0x850fadc09923329cLL,
	0x93ba47c980e98cdeLL,
	0xa402b9c5a8d3a6e6LL,
	0xb616a12b7fe617a8LL,
	0xca28a291859bbf90LL,
	0xe070f78d39275566LL,
	0xf92e0c3537826140LL,
	0x8a5296ffe33cc92cLL,
	0x9991a6f3d6bf1762LL,
	0xaa7eebfb9df9de8aLL,
	0xbd49d14aa79dbc7eLL,
	0xd226fc195c6a2f88LL,
	0xe950df20247c83f8LL,
	0x81842f29f2cce373LL,
	0x8fcac257558ee4e2LL,

	/* powers of 0.1^16 */
	0xe69594bec44de160LL,
	0xcfb11ead453994c3LL,
	0xbb127c53b17ec165LL,
	0xa87fea27a539e9b3LL,
	0x97c560ba6b0919b5LL,
	0x88b402f7fd7553abLL,
	0xf64335bcf065d3a0LL,
	0xddd0467c64bce4c4LL,
	0xc7caba6e7c5382edLL,
	0xb3f4e093db73a0b7LL,
	0xa21727db38cb0053LL,
	0x91ff83775423cc29LL,
	0x8380dea93da4bc82LL,
	0xece53cec4a314f00LL,
	0xd5605fcdcf32e217LL,
	0xc0314325637a1978LL,
	0xad1c8eab5ee43ba2LL,
	0x9becce62836ac5b0LL,
	0x8c71dcd9ba0b495cLL,
	0xfd00b89747823938LL,
	0xe3e27a444d8d991aLL,
};

static const gint16 rgexp64Power10By16[] = {
	/* exponents for both powers of 10^16 and 0.1^16 */
	54,
	107,
	160,
	213,
	266,
	319,
	373,
	426,
	479,
	532,
	585,
	638,
	691,
	745,
	798,
	851,
	904,
	957,
	1010,
	1064,
	1117,
};

static inline guint64
digits_to_int (guint16 *p, int count)
{
	g_assert (1 <= count && count <= 9);
	guint8 i = 0;
	guint64 res = 0;
	switch (count) {
	case 9: res += 100000000 * (p [i++] - '0');
	case 8: res +=  10000000 * (p [i++] - '0');
	case 7: res +=   1000000 * (p [i++] - '0');
	case 6: res +=    100000 * (p [i++] - '0');
	case 5: res +=     10000 * (p [i++] - '0');
	case 4: res +=      1000 * (p [i++] - '0');
	case 3: res +=       100 * (p [i++] - '0');
	case 2: res +=        10 * (p [i++] - '0');
	case 1: res +=         1 * (p [i++] - '0');
	}
	return res;
}

static inline guint64
mul_64_lossy (guint64 a, guint64 b, gint *pexp)
{
	/* it's ok to losse some precision here - it will be called
	 * at most twice during the conversion, so the error won't
	 * propagate to any of the 53 significant bits of the result */
	guint64 val =
		  ((((guint64) (guint32) (a >> 32)) * ((guint64) (guint32) (b >> 32)))      )
		+ ((((guint64) (guint32) (a >> 32)) * ((guint64) (guint32) (b      ))) >> 32)
		+ ((((guint64) (guint32) (a      )) * ((guint64) (guint32) (b >> 32))) >> 32);

	/* normalize */
	if ((val & 0x8000000000000000LL) == 0) {
		val <<= 1;
		*pexp -= 1;
	}

	return val;
}

static inline void
number_to_double (MonoNumber *number, gdouble *value)
{
	guint64 val;
	guint16 *src;
	gint exp, remaining, total, count, scale, absscale, index;

	total = 0;
	src = number->digits;
	while (*src++) total ++;

	remaining = total;

	src = number->digits;
	while (*src == '0') {
		remaining --;
		src ++;
	}

	if (remaining == 0) {
		*value = 0;
		goto done;
	}

	count = MIN (remaining, 9);
	remaining -= count;
	val = digits_to_int (src, count);

	if (remaining > 0) {
		count = MIN (remaining, 9);
		remaining -= count;

		/* get the denormalized power of 10 */
		guint32 mult = (guint32) (rgval64Power10 [count - 1] >> (64 - rgexp64Power10 [count - 1]));
		val = ((guint64) (guint32) val) * ((guint64) mult) + digits_to_int (src + 9, count);
	}

	scale = number->scale - (total - remaining);
	absscale = abs (scale);

	if (absscale >= 22 * 16) {
		/* overflow / underflow */
		*(guint64*) value = (scale > 0) ? 0x7FF0000000000000LL : 0;
		goto done;
	}

	exp = 64;

	/* normalize the mantiss */
	if ((val & 0xFFFFFFFF00000000LL) == 0) { val <<= 32; exp -= 32; }
	if ((val & 0xFFFF000000000000LL) == 0) { val <<= 16; exp -= 16; }
	if ((val & 0xFF00000000000000LL) == 0) { val <<= 8;  exp -= 8;  }
	if ((val & 0xF000000000000000LL) == 0) { val <<= 4;  exp -= 4;  }
	if ((val & 0xC000000000000000LL) == 0) { val <<= 2;  exp -= 2;  }
	if ((val & 0x8000000000000000LL) == 0) { val <<= 1;  exp -= 1;  }

	index = absscale & 15;
	if (index) {
		gint multexp = rgexp64Power10 [index - 1];
		/* the exponents are shared between the inverted and regular table */
		exp += (scale < 0) ? (-multexp + 1) : multexp;

		guint64 multval = rgval64Power10 [index + ((scale < 0) ? 15 : 0) - 1];
		val = mul_64_lossy (val, multval, &exp);
	}

	index = absscale >> 4;
	if (index) {
		gint multexp = rgexp64Power10By16 [index - 1];
		/* the exponents are shared between the inverted and regular table */
		exp += (scale < 0) ? (-multexp + 1) : multexp;

		guint64 multval = rgval64Power10By16 [index + ((scale < 0) ? 21 : 0) - 1];
		val = mul_64_lossy (val, multval, &exp);
	}

	if ((guint32) val & (1 << 10)) {
		/* IEEE round to even */
		guint64 tmp = val + ((1 << 10) - 1) + (((guint32) val >> 11) & 1);
		if (tmp < val) {
			/* overflow */
			tmp = (tmp >> 1) | 0x8000000000000000LL;
			exp += 1;
		}
		val = tmp;
	}

	/* return the exponent to a biased state */
	exp += 0x3FE;

	/* handle overflow, underflow, "Epsilon - 1/2 Epsilon", denormalized, and the normal case */
	if (exp <= 0) {
		if (exp == -52 && (val >= 0x8000000000000058LL)) {
			/* round X where {Epsilon > X >= 2.470328229206232730000000E-324} up to Epsilon (instead of down to zero) */
			val = 0x0000000000000001LL;
		} else if (exp <= -52) {
			/* underflow */
			val = 0;
		} else {
			/* denormalized */
			val >>= (-exp + 11 + 1);
		}
	} else if (exp >= 0x7FF) {
		/* overflow */
		val = 0x7FF0000000000000LL;
	} else {
		/* normal postive exponent case */
		val = ((guint64) exp << 52) + ((val >> 11) & 0x000FFFFFFFFFFFFFLL);
	}

	*(guint64*) value = val;

done:
	if (number->sign)
		*(guint64*) value |= 0x8000000000000000LL;
}

gint
mono_double_from_number (gpointer from, MonoDouble *target)
{
	MonoDouble_double res;
	guint e, mant_lo, mant_hi;

	res.d = 0;

	number_to_double ((MonoNumber*) from, &res.d);
	e = res.s.exp;
	mant_lo = res.s.mantLo;
	mant_hi = res.s.mantHi;

	if (e == 0x7ff)
		return 0;

	if (e == 0 && mant_lo == 0 && mant_hi == 0)
		res.d = 0;

	*target = res.s;
	return 1;
}
