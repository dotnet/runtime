/* 
   decimal.c

   conversions and numerical operations for the c# type System.Decimal

   Author: Martin Weindel (martin.weindel@t-online.de)

   (C) 2001 by Martin Weindel
*/

/*
 * machine dependent configuration for 
 * CSharp value type System.Decimal
 */

#include <stdio.h>
#include <memory.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>

/* needed for building microsoft dll */
#define DECINLINE __inline

#define LIT_GUINT32(x) x
#define LIT_GUINT64(x) x##L


#ifndef _MSC_VER

/* we need a UInt64 type => guint64 */
#include <glib.h>


#else /* #ifndef _MSC_VER */

/* Microsoft Compiler for testing */

typedef short gint16; /* that's normally defined in glib */
typedef unsigned short guint16; /* that's normally defined in glib */
typedef int gint32; /* that's normally defined in glib */
typedef unsigned int guint32; /* that's normally defined in glib */
typedef __int64 gint64; /* that's normally defined in glib */
typedef unsigned __int64 guint64; /* that's normally defined in glib */

#ifndef _M_IX86
#error this platform is not supported
#endif

#endif

#include "decimal.h"

/*
 * Deal with anon union support.
 */
#define ss32 u.ss32
#define signscale u.signscale

/* debugging stuff */
#ifdef _DEBUG
#include <assert.h>
#define PRECONDITION(flag)  assert(flag)
#define POSTCONDITION(flag)  assert(flag)
#define TEST(flag)  assert(flag)
#define INVARIANT_TEST(p) assert(p->signscale.scale >= 0 && p->signscale.scale <= DECIMAL_MAX_SCALE \
	&& p->signscale.reserved1 == 0 && p->signscale.reserved2 == 0);
#else
#define PRECONDITION(flag)  
#define POSTCONDITION(flag)  
#define TEST(flag)
#define INVARIANT_TEST(p)
#endif //#ifdef _DEBUG

/*
void DECSIGNATURE printdec(decimal_repr* pA)
{
    printf("sign=%d scale=%d %u %u %u\n", (int)pA->signscale.sign, (int) pA->signscale.scale,
        pA->hi32, pA->mid32, pA->lo32);
}
*/

#define DECIMAL_REPR_CONSTANT(ss, hi, mid, lo)  {{ss}, hi, lo, mid}

#define DECIMAL_MAX_SCALE 28
#define DECIMAL_MAX_INTFACTORS 9

#define DECIMAL_SUCCESS 0
#define DECIMAL_FINISHED 1
#define DECIMAL_OVERFLOW 2
#define DECIMAL_INVALID_CHARACTER 2
#define DECIMAL_INTERNAL_ERROR 3
#define DECIMAL_INVALID_BITS 4
#define DECIMAL_DIVIDE_BY_ZERO 5

/* some MACROS */
#define DECINIT(src) memset(src, 0, sizeof(decimal_repr))

#define DECCOPY(dest, src) memcpy(dest, src, sizeof(decimal_repr))

#define DECSWAP(p1, p2, h) \
	h = (p1)->ss32; (p1)->ss32 = (p2)->ss32; (p2)->ss32 = h; \
	h = (p1)->hi32; (p1)->hi32 = (p2)->hi32; (p2)->hi32 = h; \
	h = (p1)->mid32; (p1)->mid32 = (p2)->mid32; (p2)->mid32 = h; \
	h = (p1)->lo32; (p1)->lo32 = (p2)->lo32; (p2)->lo32 = h;

#define DECNEGATE(p1) (p1)->signscale.sign = 1 - (p1)->signscale.sign

#define DECTO128(pd, lo, hi) \
	lo = (((guint64)(pd)->mid32) << 32) | (pd)->lo32; \
    hi = (pd)->hi32;

/* some constants */
#define LIT_GUINT32_HIGHBIT LIT_GUINT32(0x80000000)
#define LIT_GUINT64_HIGHBIT LIT_GUINT64(0x8000000000000000)

#define DECIMAL_LOG_NEGINF -1000

static guint32 constantsIntFactors[DECIMAL_MAX_INTFACTORS+1] = {
    LIT_GUINT32(1), LIT_GUINT32(10), LIT_GUINT32(100), LIT_GUINT32(1000), 
    LIT_GUINT32(10000), LIT_GUINT32(100000), LIT_GUINT32(1000000), 
    LIT_GUINT32(10000000), LIT_GUINT32(100000000), LIT_GUINT32(1000000000)
};

static decimal_repr constantsFactors[DECIMAL_MAX_SCALE+1] = {
    DECIMAL_REPR_CONSTANT(0, 0, 0, 1), /* == 1 */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 10), /* == 10 */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 100), /* == 100 */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 1000), /* == 1e3m */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 10000), /* == 1e4m */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 100000), /* == 1e5m */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 1000000), /* == 1e6m */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 10000000), /* == 1e7m */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 100000000), /* == 1e8m */
    DECIMAL_REPR_CONSTANT(0, 0, 0, 1000000000), /* == 1e9m */
    DECIMAL_REPR_CONSTANT(0, 0, 2, 1410065408), /* == 1e10m */
    DECIMAL_REPR_CONSTANT(0, 0, 23, 1215752192), /* == 1e11m */
    DECIMAL_REPR_CONSTANT(0, 0, 232, 3567587328U), /* == 1e12m */
    DECIMAL_REPR_CONSTANT(0, 0, 2328, 1316134912), /* == 1e13m */
    DECIMAL_REPR_CONSTANT(0, 0, 23283, 276447232), /* == 1e14m */
    DECIMAL_REPR_CONSTANT(0, 0, 232830, 2764472320U), /* == 1e15m */
    DECIMAL_REPR_CONSTANT(0, 0, 2328306, 1874919424), /* == 1e16m */
    DECIMAL_REPR_CONSTANT(0, 0, 23283064, 1569325056), /* == 1e17m */
    DECIMAL_REPR_CONSTANT(0, 0, 232830643, 2808348672U), /* == 1e18m */
    DECIMAL_REPR_CONSTANT(0, 0, 2328306436U, 2313682944U), /* == 1e19m */
    DECIMAL_REPR_CONSTANT(0, 5, 1808227885, 1661992960), /* == 1e20m */
    DECIMAL_REPR_CONSTANT(0, 54, 902409669, 3735027712U), /* == 1e21m */
    DECIMAL_REPR_CONSTANT(0, 542, 434162106, 2990538752U), /* == 1e22m */
    DECIMAL_REPR_CONSTANT(0, 5421, 46653770, 4135583744U), /* == 1e23m */
    DECIMAL_REPR_CONSTANT(0, 54210, 466537709, 2701131776U), /* == 1e24m */
    DECIMAL_REPR_CONSTANT(0, 542101, 370409800, 1241513984), /* == 1e25m */
    DECIMAL_REPR_CONSTANT(0, 5421010, 3704098002U, 3825205248U), /* == 1e26m */
    DECIMAL_REPR_CONSTANT(0, 54210108, 2681241660U, 3892314112U), /* == 1e27m */
    DECIMAL_REPR_CONSTANT(0, 542101086, 1042612833, 268435456), /* == 1e28m */
};

/* 192 bit addition: c = a + b 
   addition is modulo 2**128, any carry is lost */
static DECINLINE void add128(guint64 alo, guint64 ahi,
                             guint64 blo, guint64 bhi,
                             guint64* pclo, guint64* pchi)
{
    alo += blo; 
    if (alo < blo) ahi++; /* carry */
    ahi += bhi;

    *pclo = alo;
    *pchi = ahi;
}

/* 128 bit subtraction: c = a - b
   subtraction is modulo 2**128, any carry is lost */
static DECINLINE void sub128(guint64 alo, guint64 ahi,
                             guint64 blo, guint64 bhi,
                             guint64* pclo, guint64* pchi)
{
    guint64 clo, chi;

    clo = alo - blo;
    chi = ahi - bhi;
    if (alo < blo) chi--; /* borrow */

    *pclo = clo;
    *pchi = chi;
}

/* 192 bit addition: c = a + b 
   addition is modulo 2**192, any carry is lost */
static DECINLINE void add192(guint64 alo, guint64 ami, guint64 ahi,
                             guint64 blo, guint64 bmi, guint64 bhi,
                             guint64* pclo, guint64* pcmi, guint64* pchi)
{
    alo += blo; 
    if (alo < blo) { /* carry low */
        ami++;
        if (ami == 0) ahi++; /* carry mid */
    }
    ami += bmi;
    if (ami < bmi) ahi++; /* carry mid */
    ahi += bhi;
    *pclo = alo;
    *pcmi = ami;
    *pchi = ahi;
}

/* 192 bit subtraction: c = a - b
   subtraction is modulo 2**192, any carry is lost */
static DECINLINE void sub192(guint64 alo, guint64 ami, guint64 ahi,
                             guint64 blo, guint64 bmi, guint64 bhi,
                             guint64* pclo, guint64* pcmi, guint64* pchi)
{
    guint64 clo, cmi, chi;

    clo = alo - blo;
    cmi = ami - bmi;
    chi = ahi - bhi;
    if (alo < blo) {
        if (cmi == 0) chi--; /* borrow mid */
        cmi--; /* borrow low */
    }
    if (ami < bmi) chi--; /* borrow mid */
    *pclo = clo;
    *pcmi = cmi;
    *pchi = chi;
}

/* multiplication c(192bit) = a(96bit) * b(96bit) */
static DECINLINE void mult96by96to192(guint32 alo, guint32 ami, guint32 ahi,
                                      guint32 blo, guint32 bmi, guint32 bhi,
                                      guint64* pclo, guint64* pcmi, guint64* pchi)
{
    guint64 a, b, c, d;
    guint32 h0, h1, h2, h3, h4, h5;
    int carry0, carry1;

    a = ((guint64)alo) * blo;
    h0 = (guint32) a;

    a >>= 32; carry0 = 0;
    b = ((guint64)alo) * bmi;
    c = ((guint64)ami) * blo;
    a += b; if (a < b) carry0++;
    a += c; if (a < c) carry0++;
    h1 = (guint32) a;

    a >>= 32; carry1 = 0;
    b = ((guint64)alo) * bhi;
    c = ((guint64)ami) * bmi;
    d = ((guint64)ahi) * blo;
    a += b; if (a < b) carry1++;
    a += c; if (a < c) carry1++;
    a += d; if (a < d) carry1++;
    h2 = (guint32) a;

    a >>= 32; a += carry0; carry0 = 0;
    b = ((guint64)ami) * bhi;
    c = ((guint64)ahi) * bmi;
    a += b; if (a < b) carry0++;
    a += c; if (a < c) carry0++;
    h3 = (guint32) a;

    a >>= 32; a += carry1;
    b = ((guint64)ahi) * bhi;
    a += b;
    h4 = (guint32) a;

    a >>= 32; a += carry0;
    h5 = (guint32) a;

    *pclo = ((guint64)h1) << 32 | h0;
    *pcmi = ((guint64)h3) << 32 | h2;
    *pchi = ((guint64)h5) << 32 | h4;
}

/* multiplication c(128bit) = a(96bit) * b(32bit) */
static DECINLINE void mult96by32to128(guint32 alo, guint32 ami, guint32 ahi,
                                      guint32 factor,
                                      guint64* pclo, guint64* pchi)
{
    guint64 a;
    guint32 h0, h1;

    a = ((guint64)alo) * factor;
    h0 = (guint32) a;

    a >>= 32;
    a += ((guint64)ami) * factor;
    h1 = (guint32) a;

    a >>= 32;
    a += ((guint64)ahi) * factor;

    *pclo = ((guint64)h1) << 32 | h0;
    *pchi = a;
}

/* multiplication c(128bit) *= b(32bit) */
static DECINLINE int mult128by32(guint64* pclo, guint64* pchi, guint32 factor, int roundBit)
{
    guint64 a;
    guint32 h0, h1;

    a = ((guint64)(guint32)(*pclo)) * factor;
	if (roundBit) a += factor / 2;
    h0 = (guint32) a;

    a >>= 32;
    a += (*pclo >> 32) * factor;
    h1 = (guint32) a;

	*pclo = ((guint64)h1) << 32 | h0;

    a >>= 32;
    a += ((guint64)(guint32)(*pchi)) * factor;
    h0 = (guint32) a;

    a >>= 32;
    a += (*pchi >> 32) * factor;
    h1 = (guint32) a;

	*pchi = ((guint64)h1) << 32 | h0;

	return ((a >> 32) == 0) ? DECIMAL_SUCCESS : DECIMAL_OVERFLOW;
}

/* division: x(128bit) /= factor(32bit) 
   returns roundBit */
static DECINLINE int div128by32(guint64* plo, guint64* phi, guint32 factor, guint32* pRest)
{
    guint64 a, b, c, h;

    h = *phi;
    a = (guint32)(h >> 32);
    b = a / factor;
    a -= b * factor;
    a <<= 32;
    a |= (guint32) h;
    c = a / factor;
    a -= c * factor;
    a <<= 32;
    *phi = b << 32 | (guint32)c;

    h = *plo;
    a |= (guint32)(h >> 32);
    b = a / factor;
    a -= b * factor;
    a <<= 32;
    a |= (guint32) h;
    c = a / factor;
    a -= c * factor;
    *plo = b << 32 | (guint32)c;

	if (pRest) *pRest = (guint32) a;

	a <<= 1;
	return (a > factor || (a == factor && (c & 1) == 1)) ? 1 : 0;
}

/* division: x(192bit) /= factor(32bit) 
   no rest and no rounding*/
static DECINLINE void div192by32(guint64* plo, guint64* pmi, guint64* phi,
                                 guint32 factor)
{
    guint64 a, b, c, h;

    h = *phi;
    a = (guint32)(h >> 32);
    b = a / factor;
    a -= b * factor;
    a <<= 32;
    a |= (guint32) h;
    c = a / factor;
    a -= c * factor;
    a <<= 32;
    *phi = b << 32 | (guint32)c;

    h = *pmi;
    a |= (guint32)(h >> 32);
    b = a / factor;
    a -= b * factor;
    a <<= 32;
    a |= (guint32) h;
    c = a / factor;
    a -= c * factor;
    a <<= 32;
    *pmi = b << 32 | (guint32)c;

    h = *plo;
    a |= (guint32)(h >> 32);
    b = a / factor;
    a -= b * factor;
    a <<= 32;
    a |= (guint32) h;
    c = a / factor;
    a -= c * factor;
    a <<= 32;
    *plo = b << 32 | (guint32)c;
}

/* returns upper 32bit for a(192bit) /= b(32bit)
   a will contain remainder */
static DECINLINE guint32 div192by96to32withRest(guint64* palo, guint64* pami, guint64* pahi, 
												guint32 blo, guint32 bmi, guint32 bhi)
{
    guint64 rlo, rmi, rhi; /* remainder */
    guint64 tlo, thi; /* term */
	guint32 c;

	rlo = *palo; rmi = *pami; rhi = *pahi;
    if (rhi >= (((guint64)bhi) << 32)) {
        c = LIT_GUINT32(0xFFFFFFFF);
    } else {
        c = (guint32) (rhi / bhi);
    }
    mult96by32to128(blo, bmi, bhi, c, &tlo, &thi);
    sub192(rlo, rmi, rhi, 0, tlo, thi, &rlo, &rmi, &rhi);
    while (((gint64)rhi) < 0) {
        c--;
        add192(rlo, rmi, rhi, 0, (((guint64)bmi)<<32) | blo, bhi, &rlo, &rmi, &rhi);
    }
	*palo = rlo ; *pami = rmi ; *pahi = rhi;

	POSTCONDITION(rhi >> 32 == 0);

	return c;
}

/* c(128bit) = a(192bit) / b(96bit) 
   b must be >= 2^95 */
static DECINLINE void div192by96to128(guint64 alo, guint64 ami, guint64 ahi,
                                    guint32 blo, guint32 bmi, guint32 bhi,
                                    guint64* pclo, guint64* pchi)
{
    guint64 rlo, rmi, rhi; /* remainder */
	guint32 h, c;

    PRECONDITION(ahi < (((guint64)bhi) << 32 | bmi) 
		|| (ahi == (((guint64)bhi) << 32 | bmi) && (ami >> 32) > blo));

    /* high 32 bit*/
	rlo = alo; rmi = ami; rhi = ahi;
	h = div192by96to32withRest(&rlo, &rmi, &rhi, blo, bmi, bhi);

    /* mid 32 bit*/
    rhi = (rhi << 32) | (rmi >> 32); rmi = (rmi << 32) | (rlo >> 32); rlo <<= 32;
	*pchi = (((guint64)h) << 32) | div192by96to32withRest(&rlo, &rmi, &rhi, blo, bmi, bhi);

    /* low 32 bit */
    rhi = (rhi << 32) | (rmi >> 32); rmi = (rmi << 32) | (rlo >> 32); rlo <<= 32;
	h = div192by96to32withRest(&rlo, &rmi, &rhi, blo, bmi, bhi);

    /* estimate lowest 32 bit (two last bits may be wrong) */
    if (rhi >= bhi) {
        c = LIT_GUINT32(0xFFFFFFFF);
    } else {
		rhi <<= 32;
        c = (guint32) (rhi / bhi);
    }
	*pclo = (((guint64)h) << 32) | c;
}

static DECINLINE void roundUp128(guint64* pclo, guint64* pchi) {
	if (++(*pclo) == 0) ++(*pchi);
}

static int normalize128(guint64* pclo, guint64* pchi, int* pScale, 
						int roundFlag, int roundBit)
{
	guint32 overhang = (guint32)(*pchi >> 32);
	int scale = *pScale;
	int deltaScale;

	while (overhang != 0) {
	    for (deltaScale = 1; deltaScale < DECIMAL_MAX_INTFACTORS; deltaScale++)
		{
			if (overhang < constantsIntFactors[deltaScale]) break;
	    }

		scale -= deltaScale;
		if (scale < 0) return DECIMAL_OVERFLOW;

		roundBit = div128by32(pclo, pchi, constantsIntFactors[deltaScale], 0);

		overhang = (guint32)(*pchi >> 32);
		if (roundFlag && roundBit && *pclo == (guint64)-1 && (gint32)*pchi == (gint32)-1) {
			overhang = 1;
		}
	}

	*pScale = scale;

	if (roundFlag && roundBit) {
		roundUp128(pclo, pchi);
		TEST((*pchi >> 32) == 0);
	}
	
	return DECIMAL_SUCCESS;
}

static DECINLINE int maxLeftShift(/*[In, Out]*/decimal_repr* pA)
{
	guint64 lo64 = (((guint64)(pA->mid32)) << 32) | pA->lo32;
	guint32 hi32 = pA->hi32;
	int shift;

	for (shift = 0; ((gint32)hi32) >= 0 && shift < 96; shift++) {
		hi32 <<= 1;
		if (((gint64)lo64) < 0) hi32++;
		lo64 <<= 1;
	}

	pA->lo32 = (guint32) lo64;
	pA->mid32 = (guint32)(lo64>>32);
	pA->hi32 = hi32;

	return shift;
}

static DECINLINE void rshift128(guint64* pclo, guint64* pchi)
{
	*pclo >>= 1;
	if (*pchi & 1) *pclo |= LIT_GUINT64_HIGHBIT;
	*pchi >>= 1;
}

static DECINLINE void lshift96(guint32* pclo, guint32* pcmid, guint32* pchi)
{
	*pchi <<= 1;
	if (*pcmid & LIT_GUINT32_HIGHBIT) (*pchi)++;
	*pcmid <<= 1;
	if (*pclo & LIT_GUINT32_HIGHBIT) (*pcmid)++;
	*pclo <<= 1;
}

static DECINLINE void lshift128(guint64* pclo, guint64* pchi)
{
	*pchi <<= 1;
	if (*pclo & LIT_GUINT64_HIGHBIT) (*pchi)++;
	*pclo <<= 1;
}

static DECINLINE void rshift192(guint64* pclo, guint64* pcmi, guint64* pchi)
{
	*pclo >>= 1;
	if (*pcmi & 1) *pclo |= LIT_GUINT64_HIGHBIT;
	*pcmi >>= 1;
	if (*pchi & 1) *pcmi |= LIT_GUINT64_HIGHBIT;
	*pchi >>= 1;
}

/* returns log2(a) or DECIMAL_LOG_NEGINF for a = 0 */
static int DECINLINE log2_32(guint32 a)
{
	int log2 = 0;

	if (a == 0) return DECIMAL_LOG_NEGINF;

	if ((a >> 16) != 0) {
		a >>= 16;
		log2 += 16;
	}
	if ((a >> 8) != 0) {
		a >>= 8;
		log2 += 8;
	}
	if ((a >> 4) != 0) {
		a >>= 4;
		log2 += 4;
	}
	if ((a >> 2) != 0) {
		a >>= 2;
		log2 += 2;
	}
	if ((a >> 1) != 0) {
		a >>= 1;
		log2 += 1;
	}
	log2 += (int) a;

	return log2;
}

/* returns log2(a) or DECIMAL_LOG_NEGINF for a = 0 */
static int DECINLINE log2_64(guint64 a)
{
	int log2 = 0;

	if (a == 0) return DECIMAL_LOG_NEGINF;

	if ((a >> 32) != 0) {
		a >>= 32;
		log2 += 32;
	}
	if ((a >> 16) != 0) {
		a >>= 16;
		log2 += 16;
	}
	if ((a >> 8) != 0) {
		a >>= 8;
		log2 += 8;
	}
	if ((a >> 4) != 0) {
		a >>= 4;
		log2 += 4;
	}
	if ((a >> 2) != 0) {
		a >>= 2;
		log2 += 2;
	}
	if ((a >> 1) != 0) {
		a >>= 1;
		log2 += 1;
	}
	log2 += (int) a;

	return log2;
}

/* returns log2(a) or DECIMAL_LOG_NEGINF for a = 0 */
static int DECINLINE log2_128(guint64 alo, guint64 ahi)
{
	if (ahi == 0) return log2_64(alo);
	else return log2_64(ahi) + 64;
}

/* returns a upper limit for log2(a) considering scale */
static int DECINLINE log2withScale_128(guint64 alo, guint64 ahi, int scale)
{
	int log2 = log2_128(alo, ahi);
	if (log2 < 0) log2 = 0;
	return log2 - (scale * 33219) / 10000;
}

static DECINLINE int pack128toDecimal(/*[Out]*/decimal_repr* pA, guint64 alo, guint64 ahi,
									  int scale, int sign)
{
	PRECONDITION((ahi >> 32) == 0);
	PRECONDITION(sign == 0 || sign == 1);
	PRECONDITION(scale >= 0 && scale <= DECIMAL_MAX_SCALE);

	if (scale < 0 || scale > DECIMAL_MAX_SCALE || (ahi >> 32) != 0) {
		return DECIMAL_OVERFLOW;
	}

	pA->lo32 = (guint32) alo;	
	pA->mid32 = (guint32) (alo >> 32);	
	pA->hi32 = (guint32) ahi;
	pA->signscale.sign = sign;
	pA->signscale.scale = scale;

	return DECIMAL_SUCCESS;
}

static DECINLINE int adjustScale128(guint64* palo, guint64* pahi, int deltaScale)
{
	int index, rc;

	if (deltaScale < 0) {
		deltaScale *= -1;
		if (deltaScale > DECIMAL_MAX_SCALE) return DECIMAL_INTERNAL_ERROR;
		while (deltaScale > 0) {
			index = (deltaScale > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : deltaScale;
			deltaScale -= index;
			div128by32(palo, pahi, constantsIntFactors[index], 0);
		}
    } else if (deltaScale > 0) {
		if (deltaScale > DECIMAL_MAX_SCALE) return DECIMAL_INTERNAL_ERROR;
		while (deltaScale > 0) {
			index = (deltaScale > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : deltaScale;
			deltaScale -= index;
			rc = mult128by32(palo, pahi, constantsIntFactors[index], 0);
			if (rc != DECIMAL_SUCCESS) return rc;
		}
    }
    
	return DECIMAL_SUCCESS;
}

/* input: c * 10^-(*pScale) * 2^-exp
   output: c * 10^-(*pScale) with 
   minScale <= *pScale <= maxScale and (chi >> 32) == 0 */
static int DECINLINE rescale128(guint64* pclo, guint64* pchi, int* pScale, int exp,
						 int minScale, int maxScale, int roundFlag)
{
	guint32 factor, overhang;
	int scale, i, rc, roundBit = 0;

	PRECONDITION(exp >= 0);

	scale = *pScale;

	if (exp > 0) {
		/* reduce exp */
		while (exp > 0 && scale <= maxScale) {
			overhang = (guint32)(*pchi >> 32);
			while (exp > 0 && ((*pclo & 1) == 0 || overhang > (2<<DECIMAL_MAX_INTFACTORS))) {
				if (--exp == 0) roundBit = (int)(*pclo & 1);
				rshift128(pclo, pchi);
				overhang = (guint32)(*pchi >> 32);
			}

			if (exp > DECIMAL_MAX_INTFACTORS) i = DECIMAL_MAX_INTFACTORS;
			else i = exp;
			if (scale + i > maxScale) i = maxScale - scale;
			if (i == 0) break;
			exp -= i;
			scale += i;
			factor = constantsIntFactors[i] >> i; /* 10^i/2^i=5^i */
			mult128by32(pclo, pchi, factor, 0);
	//printf("3: %.17e\n", (((double)chi) * pow(2,64) + clo) * pow(10, -scale) * pow(2, -exp));
		}

		while (exp > 0) {
			if (--exp == 0) roundBit = (int)(*pclo & 1);
			rshift128(pclo, pchi);
		}
	}

	TEST(exp == 0);

	while (scale > maxScale) {
		i = scale - maxScale;
		if (i > DECIMAL_MAX_INTFACTORS) i = DECIMAL_MAX_INTFACTORS;
		scale -= i;
		roundBit = div128by32(pclo, pchi, constantsIntFactors[i], 0);
	}

	while (scale < minScale) {
		if (!roundFlag) roundBit = 0;
		i = minScale - scale;
		if (i > DECIMAL_MAX_INTFACTORS) i = DECIMAL_MAX_INTFACTORS;
		scale += i;
		rc = mult128by32(pclo, pchi, constantsIntFactors[i], roundBit);
		if (rc != DECIMAL_SUCCESS) return rc;
		roundBit = 0;
	}

	TEST(scale >= 0 && scale <= DECIMAL_MAX_SCALE);

	*pScale = scale;

	return normalize128(pclo, pchi, pScale, roundFlag, roundBit);
}

/* performs a += b */
gint32 mono_decimalIncr(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
	guint64 alo, ahi, blo, bhi;
    int log2A, log2B, log2Result, log10Result, rc;
	int subFlag, sign, scaleA, scaleB;

	DECTO128(pA, alo, ahi);
	DECTO128(pB, blo, bhi);

	sign = pA->signscale.sign;
    subFlag = sign - (int)pB->signscale.sign;
	scaleA = pA->signscale.scale;
	scaleB = pB->signscale.scale;
    if (scaleA == scaleB) {
        /* same scale, that's easy */
        if (subFlag) {
            sub128(alo, ahi, blo, bhi, &alo, &ahi);
			if (ahi & LIT_GUINT64_HIGHBIT) {
				alo--;
				alo = ~alo;
				if (alo == 0) ahi--;
				ahi = ~ahi;
				sign = !sign;
			}
        } else {
            add128(alo, ahi, blo, bhi, &alo, &ahi);
        }
		rc = normalize128(&alo, &ahi, &scaleA, 1, 0);
    } else {
		/* scales must be adjusted */
        /* Estimate log10 and scale of result for adjusting scales */
        log2A = log2withScale_128(alo, ahi, scaleA);
        log2B = log2withScale_128(blo, bhi, scaleB);
        log2Result = (log2A >= log2B) ? log2A : log2B;
        if (!subFlag) log2Result++; /* result can have one bit more */
		log10Result = (log2Result * 1000) / 3322 + 1;
		/* we will calculate in 128bit, so we may need to adjust scale */
		if (scaleB > scaleA) scaleA = scaleB;
        if (scaleA + log10Result > DECIMAL_MAX_SCALE + 7) {
			/* this may not fit in 128bit, so limit it */
            scaleA = DECIMAL_MAX_SCALE + 7 - log10Result;
        }

		rc = adjustScale128(&alo, &ahi, scaleA - (int)pA->signscale.scale);
		if (rc != DECIMAL_SUCCESS) return rc;
        rc = adjustScale128(&blo, &bhi, scaleA - scaleB);
		if (rc != DECIMAL_SUCCESS) return rc;

		if (subFlag) {
            sub128(alo, ahi, blo, bhi, &alo, &ahi);
			if (ahi & LIT_GUINT64_HIGHBIT) {
				alo--;
				alo = ~alo;
				if (alo == 0) ahi--;
				ahi = ~ahi;
				sign = !sign;
			}
		} else {
            add128(alo, ahi, blo, bhi, &alo, &ahi);
        }

		if (rc != DECIMAL_SUCCESS) return rc;

		rc = rescale128(&alo, &ahi,&scaleA, 0, 0, DECIMAL_MAX_SCALE, 1);
    }

	if (rc != DECIMAL_SUCCESS) return rc;

	return pack128toDecimal(pA, alo, ahi, scaleA, sign);
}

/* performs a += factor * constants[index] */
static int incMultConstant128(guint64* palo, guint64* pahi, int index, int factor)
{
	guint64 blo, bhi, h;

    PRECONDITION(index >= 0 && index <= DECIMAL_MAX_SCALE);
    PRECONDITION(factor > 0 && factor <= 9);

	DECTO128(constantsFactors + index, blo, bhi);
	if (factor != 1) {
		h = bhi;
		mult128by32(&blo, &bhi, factor, 0);
		if (h > bhi) return DECIMAL_OVERFLOW;
	}
	h = *pahi;
	add128(*palo, *pahi, blo, bhi, palo, pahi);
	if (h > *pahi) return DECIMAL_OVERFLOW;
    return DECIMAL_SUCCESS;
}

gint32 mono_double2decimal(/*[Out]*/decimal_repr* pA, double val, gint32 digits, gint32 sign)
{
    int i, dec, decrDecimal, dummySign, roundPos, roundFlag, rc;
    char ecvtcopy[40]; /* we should need maximal room for 16 digits */
    char buf[40]; /* we should need maximal room for 29 digits */
    char* p;
    char* ps;
    decimal_repr roundAdd;
    MonoString *mstring;

    DECINIT(pA);

    /* floating point number must be convert to the decimal system for rounding anyway 
       so we use conversion to character representation as starting point.
       We ask for more than the significant digits, as we must round
       according to banker's rule finally */
    /* FIXME: the following 2 lines must be made thread safe (access to global buffer of ecvt) */
    p = ecvt(val, digits+4, &dec, &dummySign);
    strcpy(ecvtcopy, p);
    ps = ecvtcopy;

    /*test printf("%s  %d  digits:%d\n", ps, dec, digits);*/
    /* determine round position */
    roundPos = dec + DECIMAL_MAX_SCALE;
    if (roundPos > digits) roundPos = digits; /* maximal sigificant digits */

    /* build decimal string */
    p = buf;
    /* leading zeros for numbers < 1 */
    for (i = dec; i < 0; i++)
        *p++ = '0';

    /* significant digits */
    for (i = 0; i < roundPos; i++)
        *p++ = *ps++;

    /* trailing zeros for numbers > 1 */
    for (i = roundPos; i < dec; i++)
        *p++ = '0';

    /* end of string */
    *p++ = 0;

    /* position of decimal point */
    if (dec > 0) {
        decrDecimal = dec;
    } else {
        decrDecimal = 0; /* as we have added leading zero digits */
    }

    /* convert string to decimal */
    mstring = mono_string_new (buf);
    rc = mono_string2decimal(pA, mstring, decrDecimal, sign);
    /* FIXME: we currently leak the string: mono_string_free (mstring); */
    if (rc != DECIMAL_SUCCESS) return rc;

    /* do we need to upround according to banker's rule ? */
    if (*ps <= '4') {
        roundFlag = 0;
    } else if (*ps >= '6') {
        roundFlag = 1;
    } else { /* banker's rule for case '5' */
        roundFlag = ((*(ps-1) - '0') % 2 == 1);
    }
    if (roundFlag) {
        /*test printf("round!!! dec=%d  decrDecimal=%d  roundPos=%d  scale=%d\n", dec, decrDecimal, roundPos, pA->signscale.scale); printdec(pA); printdec(&roundAdd);*/
        DECCOPY(&roundAdd, constantsFactors + dec + pA->signscale.scale - roundPos);
        roundAdd.ss32 = pA->ss32;
        /* perform rounding */
        mono_decimalIncr(pA, &roundAdd);
    }

    return DECIMAL_SUCCESS;
}

/* converts string to decimal */
gint32 mono_string2decimal(/*[Out]*/decimal_repr* pA, MonoString* str, gint32 decrDecimal, gint32 sign)
{
	guint64 alo, ahi;
    int len, n, rc, i;
    gushort *buf;

    alo = ahi = 0;
	DECINIT(pA);

	buf = mono_string_chars (str);
    len = str->length;
    if (len > DECIMAL_MAX_SCALE+1) {
        return DECIMAL_OVERFLOW;
    }

    for (i = 0; i < len; i++) {
        n = buf[i] - '0';
        if (n < 0 || n > 9) {
            return DECIMAL_INVALID_CHARACTER;
        }
        if (n) {
            rc = incMultConstant128(&alo, &ahi, len - 1 - i, n);
            if (rc != DECIMAL_SUCCESS) {
                return rc;
            }
        }
    }

	if (alo == 0 && ahi == 0) {
		DECINIT(pA);
		return DECIMAL_SUCCESS;
	} else {
		return pack128toDecimal(pA, alo, ahi, len - decrDecimal, sign);
	}
}

/* calc significant digits of mantisse */
static int DECINLINE calcDigits(/*[In]*/decimal_repr* pA)
{
	guint32 a;
	int log2 = 0;
	int log10;
	decimal_repr* pConst;

	a = pA->hi32;
	if (a == 0) {
		a = pA->mid32;
		if (a == 0) {
			a = pA->lo32;
			if (a ==  0) return 0; /* zero has no signficant digits */
		} else {
			log2 = 32;
		}
	} else {
		log2 = 64;
	}

	log2 += log2_32(a); /* get floor value of log2(a) */

	log10 = (log2 * 1000) / 3322;
	/* we need an exact floor value of log10(a) */
	pConst = constantsFactors + log10;
	if (pConst->hi32 > pA->hi32 
		|| (pConst->hi32 == pA->hi32 && (pConst->mid32 > pA->mid32
		|| (pConst->mid32 == pA->mid32 && pConst->lo32 > pA->lo32)))) {
		--log10;
	}
	return log10+1;
}

/* params:
	  pA         decimal instance to convert     
      digits     < 0: use decimals instead
	             = 0: gets mantisse as integer
	             > 0: gets at most <digits> digits, rounded according to banker's rule if necessary
	  decimals   only used if digits < 0
	             >= 0: number
      buf        pointer to result buffer
	  bufSize    size of buffer
	  pDecPos    receives insert position of decimal point relative to start of buffer
	  pSign      receives sign */
void mono_decimal2string(/*[In]*/decimal_repr* pA, int digits, int decimals,
								 MonoArray* buf, gint32 bufSize, gint32* pDecPos, gint32* pSign)
{
	decimal_repr aa;
	guint64 alo, ahi;
    guint32 rest;
    int i, scale, sigDigits;
    gushort* p = (gushort*) mono_array_addr (buf, gushort, 0);
    gushort *tail;

	scale = pA->signscale.scale;
	sigDigits = calcDigits(pA);

	if (digits < 0) { /* use decimals ? */
		if (0 <= decimals && decimals < scale) {
			digits = sigDigits - scale + decimals;
			if (digits == 0) digits = -1; /* no digits */
		} else {
			digits = 0; /* use all you can get */
		}
	} 

	if (digits > 0 && digits <= DECIMAL_MAX_SCALE) { /* significant digits */
		if (sigDigits > digits) { /* we need to round decimal number */
			DECCOPY(&aa, pA);
			aa.signscale.scale = DECIMAL_MAX_SCALE;
			mono_decimalRound(&aa, DECIMAL_MAX_SCALE - sigDigits + digits);
			sigDigits += calcDigits(&aa) - digits;
			DECTO128(&aa, alo, ahi);
		} else {
			DECTO128(pA, alo, ahi);
		}
	} else {
		DECTO128(pA, alo, ahi);
	}

	if (digits >= 0) {
	    /* get digits starting from the tail */
		for (i = 0; (alo != 0 || ahi != 0) && i < bufSize-1; i++) {
			div128by32(&alo, &ahi, 10, &rest);
			*p++ = '0' + (char) rest;
		}
		tail = p - 1;
		*p = 0;
	}

    /* reverse string */
	p = (gushort*) mono_array_addr (buf, gushort, 0);
	while (p < tail) {
		gushort c = *p;
		*p = *tail;
		p++;
		*tail = c;
		--tail;
	}
	if (digits > 0 && digits < bufSize) 
		mono_array_set (buf, gushort, digits, 0);

    *pDecPos = sigDigits - scale;
    *pSign = pA->signscale.sign;
}

static DECINLINE void div128DecimalFactor(guint64* palo, guint64* pahi, int powerOfTen)
{
    int index, roundBit = 0;

    while (powerOfTen > 0) {
		index = (powerOfTen > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : powerOfTen;
		powerOfTen -= index;
		roundBit = div128by32(palo, pahi, constantsIntFactors[index], 0);
    }

	if (roundBit) roundUp128(palo, pahi);
}

gint32 mono_decimal2UInt64(/*[In]*/decimal_repr* pA, guint64* pResult)
{
	guint64 alo, ahi;
    int scale;

	DECTO128(pA, alo, ahi);
    scale = pA->signscale.scale;
	if (scale > 0) {
		div128DecimalFactor(&alo, &ahi, scale);
	}

	/* overflow if integer too large or < 0 */
    if (ahi != 0 || (alo != 0 && pA->signscale.sign)) return DECIMAL_OVERFLOW;

	*pResult = alo;
    return DECIMAL_SUCCESS;
}

gint32 mono_decimal2Int64(/*[In]*/decimal_repr* pA, gint64* pResult)
{
	guint64 alo, ahi;
    int sign, scale;

	DECTO128(pA, alo, ahi);
    scale = pA->signscale.scale;
	if (scale > 0) {
		div128DecimalFactor(&alo, &ahi, scale);
	}

    if (ahi != 0) return DECIMAL_OVERFLOW;

	sign = pA->signscale.sign;
	if (sign && alo != 0) {
		if (alo > LIT_GUINT64_HIGHBIT) return DECIMAL_OVERFLOW;
		*pResult = (gint64) ~(alo-1);
	} else {
		if (alo & LIT_GUINT64_HIGHBIT) return DECIMAL_OVERFLOW;
		*pResult = (gint64) alo;
	}

    return DECIMAL_SUCCESS;
}

void mono_decimalFloorAndTrunc(/*[In, Out]*/decimal_repr* pA, gint32 floorFlag)
{
	guint64 alo, ahi;
    guint32 factor, rest;
    int scale, sign, index;
    int hasRest = 0;

    scale = pA->signscale.scale;
    if (scale == 0) return; /* nothing to do */

	DECTO128(pA, alo, ahi);
	sign = pA->signscale.sign;

    while (scale > 0) {
		index = (scale > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : scale;
		factor = constantsIntFactors[index];
		scale -= index;
		div128by32(&alo, &ahi, factor, &rest);
		hasRest = hasRest || (rest != 0);
	}

    if (floorFlag && hasRest && sign) { /* floor: if negative, we must round up */
		roundUp128(&alo, &ahi);
    }

	pack128toDecimal(pA, alo, ahi, 0, sign);
}

void mono_decimalRound(/*[In, Out]*/decimal_repr* pA, gint32 decimals)
{
	guint64 alo, ahi;
    int scale, sign;

	DECTO128(pA, alo, ahi);
    scale = pA->signscale.scale;
	sign = pA->signscale.sign;
    if (scale > decimals) {
		div128DecimalFactor(&alo, &ahi, scale - decimals);
		scale = decimals;
	}
	
	pack128toDecimal(pA, alo, ahi, scale, sign);
}

gint32 mono_decimalMult(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
    guint64 low, mid, high;
    guint32 factor;
    int scale, sign, rc;

    mult96by96to192(pA->lo32, pA->mid32, pA->hi32, pB->lo32, pB->mid32, pB->hi32,
        &low, &mid, &high);

	/* adjust scale and sign */
	scale = (int)pA->signscale.scale + (int)pB->signscale.scale;
	sign = pA->signscale.sign ^ pB->signscale.sign;

    /* first scaling step */
    factor = constantsIntFactors[DECIMAL_MAX_INTFACTORS];
    while (high != 0 || (mid>>32) >= factor) {
        if (high < 100) {
            factor /= 1000; /* we need some digits for final rounding */
            scale -= DECIMAL_MAX_INTFACTORS - 3;
        } else {
            scale -= DECIMAL_MAX_INTFACTORS;
        }

        div192by32(&low, &mid, &high, factor);
    }

	/* second and final scaling */
	rc = rescale128(&low, &mid, &scale, 0, 0, DECIMAL_MAX_SCALE, 1);
	if (rc != DECIMAL_SUCCESS) return rc;

	return pack128toDecimal(pA, low, mid, scale, sign);
}

static int DECINLINE decimalDivSub(/*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB,
									 guint64* pclo, guint64* pchi, int* pExp)
{
	guint64 alo, ami, ahi;
	guint64 tlo, tmi, thi;
	guint32 blo, bmi, bhi;
	int ashift, bshift, extraBit, exp;

	ahi = (((guint64)(pA->hi32)) << 32) | pA->mid32;
	ami = ((guint64)(pA->lo32)) << 32;
	alo = 0;
	blo = pB->lo32;
	bmi = pB->mid32;
	bhi = pB->hi32;

	if (blo == 0 && bmi == 0 && bhi == 0) {
		return DECIMAL_DIVIDE_BY_ZERO;
	}

	if (ami == 0 && ahi == 0) {
		*pclo = *pchi = 0;
		return DECIMAL_FINISHED;
	}

	/* enlarge dividend to get maximal precision */
	for (ashift = 0; (ahi & LIT_GUINT64_HIGHBIT) == 0; ++ashift) {
		lshift128(&ami, &ahi);
	}

	/* ensure that divisor is at least 2^95 */
	for (bshift = 0; (bhi & LIT_GUINT32_HIGHBIT) == 0; ++bshift) {
		lshift96(&blo, &bmi, &bhi);
	}

	thi = ((guint64)bhi)<<32 | bmi;
	tmi = ((guint64)blo)<<32;
	tlo = 0;
	if (ahi > thi || (ahi == thi && ami >= tmi)) {
		sub192(alo, ami, ahi, tlo, tmi, thi, &alo, &ami, &ahi);
		extraBit = 1;
	} else {
		extraBit = 0;
	}

	div192by96to128(alo, ami, ahi, blo, bmi, bhi, pclo, pchi);
	exp = 128 + ashift - bshift;

	if (extraBit) {
		rshift128(pclo, pchi);
		*pchi += LIT_GUINT64_HIGHBIT;
		exp--;
	}

	/* try loss free right shift */
	while (exp > 0 && (*pclo & 1) == 0) {
		/* right shift */
		rshift128(pclo, pchi);
		exp--;
	}

	*pExp = exp;

	return DECIMAL_SUCCESS;
}

gint32 mono_decimalDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
	guint64 clo, chi; /* result */
	int scale, exp, rc;

	rc = decimalDivSub(pA, pB, &clo, &chi, &exp);
	if (rc != DECIMAL_SUCCESS) {
		if (rc == DECIMAL_FINISHED) rc = DECIMAL_SUCCESS;
		return rc;
	}

	/* adjust scale and sign */
	scale = (int)pA->signscale.scale - (int)pB->signscale.scale;

	//test: printf("0: %.17e\n", (((double)chi) * pow(2,64) + clo) * pow(10, -scale) * pow(2, -exp));
	rc = rescale128(&clo, &chi, &scale, exp, 0, DECIMAL_MAX_SCALE, 1);
	if (rc != DECIMAL_SUCCESS) return rc;

	return pack128toDecimal(pC, clo, chi, scale, pA->signscale.sign ^ pB->signscale.sign);
}

gint32 mono_decimalIntDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
	guint64 clo, chi; /* result */
	int scale, exp, rc;

	rc = decimalDivSub(pA, pB, &clo, &chi, &exp);
	if (rc != DECIMAL_SUCCESS) {
		if (rc == DECIMAL_FINISHED) rc = DECIMAL_SUCCESS;
		return rc;
	}

	/* calc scale  */
	scale = (int)pA->signscale.scale - (int)pB->signscale.scale;

	/* truncate result to integer */
	rc = rescale128(&clo, &chi, &scale, exp, 0, 0, 0);
	if (rc != DECIMAL_SUCCESS) return rc;

	return pack128toDecimal(pC, clo, chi, scale, pA->signscale.sign);
}

/* approximation for log2 of a 
   If q is the exact value for log2(a), then q <= decimalLog2(a) <= q+1 */
static int DECINLINE decimalLog2(/*[In]*/decimal_repr* pA)
{
	int log2;
	int scale = pA->signscale.scale;

	if (pA->hi32 != 0) log2 = 64 + log2_32(pA->hi32);
	else if (pA->mid32 != 0) log2 = 32 + log2_32(pA->mid32);
	else log2 = log2_32(pA->lo32);

	if (log2 != DECIMAL_LOG_NEGINF) {
		log2 -= (scale * 33219) / 10000;
	}

	return log2;
}

static DECINLINE int decimalIsZero(/*[In]*/decimal_repr* pA)
{
	return (pA->lo32 == 0 && pA->mid32 == 0 && pA->hi32 == 0);
}

gint32 mono_decimalCompare(/*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
	int log2a, log2b, delta, sign;
	decimal_repr aa;

	sign = (pA->signscale.sign) ? -1 : 1;

	if (pA->signscale.sign ^ pB->signscale.sign) {
		return (decimalIsZero(pA) && decimalIsZero(pB)) ? 0 : sign;
	}

	/* try fast comparison via log2 */
	log2a = decimalLog2(pA);
	log2b = decimalLog2(pB);
	delta = log2a - log2b;
	 /* decimalLog2 is not exact, so we can say nothing 
	    if abs(delta) <= 1 */
	if (delta < -1) return -sign;
	if (delta > 1) return sign;

	DECCOPY(&aa, pA);
	DECNEGATE(&aa);
	mono_decimalIncr(&aa, pB);

	if (decimalIsZero(&aa)) return 0;

	return (aa.signscale.sign) ? 1 : -1;
}

/* d=(-1)^sign * n * 2^(k-52) with sign (1bit), k(11bit), n-2^52(52bit) */  
static DECINLINE void buildIEEE754Double(double* pd, int sign, int exp, guint64 mantisse)
{
	guint64* p = (guint64*) pd;

	PRECONDITION(sign == 0 || sign == 1);
	*p = (((guint64)sign) << 63) | (((guint64)((1023+exp)&0x7ff)) << 52) | mantisse;
}

double mono_decimal2double(/*[In]*/decimal_repr* pA)
{
	double d;
	guint64 alo, ahi, mantisse;
	guint32 overhang, factor, roundBits;
	int scale, exp, log5, i;

	ahi = (((guint64)(pA->hi32)) << 32) | pA->mid32;
	alo = ((guint64)(pA->lo32)) << 32;

	/* special case zero */
	if (ahi == 0 && alo == 0) return 0.0;

	exp = 0;
	scale = pA->signscale.scale;

	/* transform n * 10^-scale and exp = 0 => m * 2^-exp and scale = 0 */
	while (scale > 0) {
		while ((ahi & LIT_GUINT64_HIGHBIT) == 0) {
			lshift128(&alo, &ahi);
			exp++;
		}

		overhang = (guint32) (ahi >> 32);
		if (overhang >= 5) {
			/* estimate log5 */
			log5 = (log2_32(overhang) * 1000) / 2322; /* ln(5)/ln(2) = 2.3219... */
			if (log5 < DECIMAL_MAX_INTFACTORS) {
				/* get maximal factor=5^i, so that overhang / factor >= 1 */
				factor = constantsIntFactors[log5] >> log5; /* 5^n = 10^n/2^n */
				i = log5 + overhang / factor;
			} else {
				i = DECIMAL_MAX_INTFACTORS; /* we have only constants up to 10^DECIMAL_MAX_INTFACTORS */
			}
			if (i > scale) i = scale;
			factor = constantsIntFactors[i] >> i; /* 5^n = 10^n/2^n */
			/* n * 10^-scale * 2^-exp => m * 10^-(scale-i) * 2^-(exp+i) with m = n * 5^-i */
			div128by32(&alo, &ahi, factor, 0);
			scale -= i;
			exp += i;
		}
	}

	/* normalize significand (highest bit should be 1) */
	while ((ahi & LIT_GUINT64_HIGHBIT) == 0) {
		lshift128(&alo, &ahi);
		exp++;
	}

	/* round to nearest even */
	roundBits = (guint32)ahi & 0x7ff;
	ahi += 0x400;
	if ((ahi & LIT_GUINT64_HIGHBIT) == 0) { /* overflow ? */
		ahi >>= 1;
		exp++;
	} else if ((roundBits & 0x400) == 0) ahi &= ~1;

	/* 96 bit => 1 implizit bit and 52 explicit bits */
    mantisse = (ahi & ~LIT_GUINT64_HIGHBIT) >> 11;

	buildIEEE754Double(&d, pA->signscale.sign, -exp+95, mantisse);

	return d;
}

/* a *= 10^exp */
gint32 mono_decimalSetExponent(/*[In, Out]*/decimal_repr* pA, gint32 exp)
{
	guint64 alo, ahi;
	int rc;
	int scale = pA->signscale.scale;

	scale -= exp;

	if (scale < 0 || scale > DECIMAL_MAX_SCALE)	{
		DECTO128(pA, alo, ahi);
		rc = rescale128(&alo, &ahi, &scale, 0, 0, DECIMAL_MAX_SCALE, 1);
		if (rc != DECIMAL_SUCCESS) return rc;
		return pack128toDecimal(pA, alo, ahi, scale, pA->signscale.sign);
	} else {
		pA->signscale.scale = scale;
		return DECIMAL_SUCCESS;
	}
}
