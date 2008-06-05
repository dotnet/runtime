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

#include "config.h"
#include <mono/metadata/exception.h>
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

#ifndef DISABLE_DECIMAL

/* needed for building microsoft dll */
#ifdef __GNUC__
#define DECINLINE __inline
#else
#define DECINLINE
#endif

#define LIT_GUINT32(x) x
#define LIT_GUINT64(x) x##LL


/* we need a UInt64 type => guint64 */
#include <glib.h>

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
#endif /*#ifdef _DEBUG*/

#define DECIMAL_MAX_SCALE 28
#define DECIMAL_MAX_INTFACTORS 9

#define DECIMAL_SUCCESS 0
#define DECIMAL_FINISHED 1
#define DECIMAL_OVERFLOW 2
#define DECIMAL_INVALID_CHARACTER 2
#define DECIMAL_INTERNAL_ERROR 3
#define DECIMAL_INVALID_BITS 4
#define DECIMAL_DIVIDE_BY_ZERO 5
#define DECIMAL_BUFFER_OVERFLOW 6

/* some MACROS */
#define DECINIT(src) memset(src, 0, sizeof(decimal_repr))

#define DECCOPY(dest, src) memcpy(dest, src, sizeof(decimal_repr))

#define DECSWAP(p1, p2, h) \
	h = (p1)->ss32; (p1)->ss32 = (p2)->ss32; (p2)->ss32 = h; \
	h = (p1)->hi32; (p1)->hi32 = (p2)->hi32; (p2)->hi32 = h; \
	h = (p1)->mid32; (p1)->mid32 = (p2)->mid32; (p2)->mid32 = h; \
	h = (p1)->lo32; (p1)->lo32 = (p2)->lo32; (p2)->lo32 = h;

#define DECNEGATE(p1) (p1)->signscale.sign = 1 - (p1)->signscale.sign

#define LIT_DEC128(hi, mid, lo) { (((guint64)mid)<<32 | lo), hi }

#define DECTO128(pd, lo, hi) \
	lo = (((guint64)(pd)->mid32) << 32) | (pd)->lo32; \
    hi = (pd)->hi32;

/* some constants */
#define LIT_GUINT32_HIGHBIT LIT_GUINT32(0x80000000)
#define LIT_GUINT64_HIGHBIT LIT_GUINT64(0x8000000000000000)

#define DECIMAL_LOG_NEGINF -1000

static const guint32 constantsDecadeInt32Factors[DECIMAL_MAX_INTFACTORS+1] = {
    LIT_GUINT32(1), LIT_GUINT32(10), LIT_GUINT32(100), LIT_GUINT32(1000), 
    LIT_GUINT32(10000), LIT_GUINT32(100000), LIT_GUINT32(1000000), 
    LIT_GUINT32(10000000), LIT_GUINT32(100000000), LIT_GUINT32(1000000000)
};

typedef struct {
    guint64 lo;
    guint64 hi;
} dec128_repr;

static const dec128_repr dec128decadeFactors[DECIMAL_MAX_SCALE+1] = {
    LIT_DEC128( 0, 0, 1u), /* == 1 */
    LIT_DEC128( 0, 0, 10u), /* == 10 */
    LIT_DEC128( 0, 0, 100u), /* == 100 */
    LIT_DEC128( 0, 0, 1000u), /* == 1e3m */
    LIT_DEC128( 0, 0, 10000u), /* == 1e4m */
    LIT_DEC128( 0, 0, 100000u), /* == 1e5m */
    LIT_DEC128( 0, 0, 1000000u), /* == 1e6m */
    LIT_DEC128( 0, 0, 10000000u), /* == 1e7m */
    LIT_DEC128( 0, 0, 100000000u), /* == 1e8m */
    LIT_DEC128( 0, 0, 1000000000u), /* == 1e9m */
    LIT_DEC128( 0, 2u, 1410065408u), /* == 1e10m */
    LIT_DEC128( 0, 23u, 1215752192u), /* == 1e11m */
    LIT_DEC128( 0, 232u, 3567587328u), /* == 1e12m */
    LIT_DEC128( 0, 2328u, 1316134912u), /* == 1e13m */
    LIT_DEC128( 0, 23283u, 276447232u), /* == 1e14m */
    LIT_DEC128( 0, 232830u, 2764472320u), /* == 1e15m */
    LIT_DEC128( 0, 2328306u, 1874919424u), /* == 1e16m */
    LIT_DEC128( 0, 23283064u, 1569325056u), /* == 1e17m */
    LIT_DEC128( 0, 232830643u, 2808348672u), /* == 1e18m */
    LIT_DEC128( 0, 2328306436u, 2313682944u), /* == 1e19m */
    LIT_DEC128( 5u, 1808227885u, 1661992960u), /* == 1e20m */
    LIT_DEC128( 54u, 902409669u, 3735027712u), /* == 1e21m */
    LIT_DEC128( 542u, 434162106u, 2990538752u), /* == 1e22m */
    LIT_DEC128( 5421u, 46653770u, 4135583744u), /* == 1e23m */
    LIT_DEC128( 54210u, 466537709u, 2701131776u), /* == 1e24m */
    LIT_DEC128( 542101u, 370409800u, 1241513984u), /* == 1e25m */
    LIT_DEC128( 5421010u, 3704098002u, 3825205248u), /* == 1e26m */
    LIT_DEC128( 54210108u, 2681241660u, 3892314112u), /* == 1e27m */
    LIT_DEC128( 542101086u, 1042612833u, 268435456u), /* == 1e28m */
};

/* 192 bit addition: c = a + b 
   addition is modulo 2**128, any carry is lost */
DECINLINE static void add128(guint64 alo, guint64 ahi,
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
DECINLINE static void sub128(guint64 alo, guint64 ahi,
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
DECINLINE static void add192(guint64 alo, guint64 ami, guint64 ahi,
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
DECINLINE static void sub192(guint64 alo, guint64 ami, guint64 ahi,
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
DECINLINE static void mult96by96to192(guint32 alo, guint32 ami, guint32 ahi,
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
DECINLINE static void mult96by32to128(guint32 alo, guint32 ami, guint32 ahi,
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
DECINLINE static int mult128by32(guint64* pclo, guint64* pchi, guint32 factor, int roundBit)
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

DECINLINE static int mult128DecadeFactor(guint64* pclo, guint64* pchi, int powerOfTen)
{
    int idx, rc;

    while (powerOfTen > 0) {
        idx = (powerOfTen >= DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : powerOfTen;
        powerOfTen -= idx;
        rc = mult128by32(pclo, pchi, constantsDecadeInt32Factors[idx], 0);
        if (rc != DECIMAL_SUCCESS) return rc;
    }
    return DECIMAL_SUCCESS;
}

/* division: x(128bit) /= factor(32bit) 
   returns roundBit */
DECINLINE static int div128by32(guint64* plo, guint64* phi, guint32 factor, guint32* pRest)
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
    return (a >= factor || (a == factor && (c & 1) == 1)) ? 1 : 0;
}

/* division: x(192bit) /= factor(32bit) 
   no rest and no rounding*/
DECINLINE static void div192by32(guint64* plo, guint64* pmi, guint64* phi,
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
DECINLINE static guint32 div192by96to32withRest(guint64* palo, guint64* pami, guint64* pahi, 
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
DECINLINE static void div192by96to128(guint64 alo, guint64 ami, guint64 ahi,
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

DECINLINE static void roundUp128(guint64* pclo, guint64* pchi) {
    if (++(*pclo) == 0) ++(*pchi);
}

DECINLINE static int normalize128(guint64* pclo, guint64* pchi, int* pScale, 
								  int roundFlag, int roundBit)
{
    guint32 overhang = (guint32)(*pchi >> 32);
    int scale = *pScale;
    int deltaScale;

    while (overhang != 0) {
        for (deltaScale = 1; deltaScale < DECIMAL_MAX_INTFACTORS; deltaScale++)
        {
            if (overhang < constantsDecadeInt32Factors[deltaScale]) break;
        }

        scale -= deltaScale;
        if (scale < 0) return DECIMAL_OVERFLOW;

        roundBit = div128by32(pclo, pchi, constantsDecadeInt32Factors[deltaScale], 0);

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

DECINLINE static int maxLeftShift(/*[In, Out]*/decimal_repr* pA)
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

DECINLINE static void rshift128(guint64* pclo, guint64* pchi)
{
    *pclo >>= 1;
	*pclo |= (*pchi & 1) << 63;
    *pchi >>= 1;
}

DECINLINE static void lshift96(guint32* pclo, guint32* pcmid, guint32* pchi)
{
    *pchi <<= 1;
	*pchi |= (*pcmid & LIT_GUINT32_HIGHBIT) >> 31;
    *pcmid <<= 1;
	*pcmid |= (*pclo & LIT_GUINT32_HIGHBIT) >> 31;
    *pclo <<= 1;
}

DECINLINE static void lshift128(guint64* pclo, guint64* pchi)
{
    *pchi <<= 1;
	*pchi |= (*pclo & LIT_GUINT64_HIGHBIT) >> 63;
    *pclo <<= 1;
}

DECINLINE static void rshift192(guint64* pclo, guint64* pcmi, guint64* pchi)
{
    *pclo >>= 1;
	*pclo |= (*pcmi & 1) << 63;
    *pcmi >>= 1;
	*pcmi |= (*pchi & 1) << 63;
    *pchi >>= 1;
}

static inline gint
my_g_bit_nth_msf (gsize mask)
{
	/* Mask is expected to be != 0 */
#if defined(__i386__) && defined(__GNUC__)
	int r;

	__asm__("bsrl %1,%0\n\t"
			: "=r" (r) : "rm" (mask));
	return r;
#elif defined(__x86_64) && defined(__GNUC__)
	guint64 r;

	__asm__("bsrq %1,%0\n\t"
			: "=r" (r) : "rm" (mask));
	return r;
#elif defined(__i386__) && defined(_MSC_VER)
	unsigned long bIndex = 0;
	if (_BitScanReverse (&bIndex, mask))
		return bIndex;
	return -1;
#elif defined(__x86_64__) && defined(_MSC_VER)
	unsigned long bIndex = 0;
	if (_BitScanReverse64 (&bIndex, mask))
		return bIndex;
	return -1;
#else
	return g_bit_nth_msf (mask, sizeof (gsize) * 8);
#endif
}

/* returns log2(a) or DECIMAL_LOG_NEGINF for a = 0 */
DECINLINE static int log2_32(guint32 a)
{
    if (a == 0) return DECIMAL_LOG_NEGINF;

	return my_g_bit_nth_msf (a) + 1;
}

/* returns log2(a) or DECIMAL_LOG_NEGINF for a = 0 */
DECINLINE static int log2_64(guint64 a)
{
    if (a == 0) return DECIMAL_LOG_NEGINF;

#if SIZEOF_VOID_P == 8
	return my_g_bit_nth_msf (a) + 1;
#else
	if ((a >> 32) == 0)
		return my_g_bit_nth_msf ((guint32)a) + 1;
	else
		return my_g_bit_nth_msf ((guint32)(a >> 32)) + 1 + 32;
#endif
}

/* returns log2(a) or DECIMAL_LOG_NEGINF for a = 0 */
DECINLINE static int log2_128(guint64 alo, guint64 ahi)
{
    if (ahi == 0) return log2_64(alo);
    else return log2_64(ahi) + 64;
}

/* returns a upper limit for log2(a) considering scale */
DECINLINE static int log2withScale_128(guint64 alo, guint64 ahi, int scale)
{
    int tlog2 = log2_128(alo, ahi);
    if (tlog2 < 0) tlog2 = 0;
    return tlog2 - (scale * 33219) / 10000;
}

DECINLINE static int pack128toDecimal(/*[Out]*/decimal_repr* pA, guint64 alo, guint64 ahi,
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

DECINLINE static int adjustScale128(guint64* palo, guint64* pahi, int deltaScale)
{
    int idx, rc;

    if (deltaScale < 0) {
        deltaScale *= -1;
        if (deltaScale > DECIMAL_MAX_SCALE) return DECIMAL_INTERNAL_ERROR;
        while (deltaScale > 0) {
            idx = (deltaScale > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : deltaScale;
            deltaScale -= idx;
            div128by32(palo, pahi, constantsDecadeInt32Factors[idx], 0);
        }
    } else if (deltaScale > 0) {
        if (deltaScale > DECIMAL_MAX_SCALE) return DECIMAL_INTERNAL_ERROR;
        while (deltaScale > 0) {
            idx = (deltaScale > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : deltaScale;
            deltaScale -= idx;
            rc = mult128by32(palo, pahi, constantsDecadeInt32Factors[idx], 0);
            if (rc != DECIMAL_SUCCESS) return rc;
        }
    }
    
    return DECIMAL_SUCCESS;
}

/* input: c * 10^-(*pScale) * 2^-exp
   output: c * 10^-(*pScale) with 
   minScale <= *pScale <= maxScale and (chi >> 32) == 0 */
DECINLINE static int rescale128(guint64* pclo, guint64* pchi, int* pScale, int texp,
                                int minScale, int maxScale, int roundFlag)
{
    guint32 factor, overhang;
    int scale, i, rc, roundBit = 0;

    PRECONDITION(texp >= 0);

    scale = *pScale;

    if (texp > 0) {
        /* reduce exp */
        while (texp > 0 && scale <= maxScale) {
            overhang = (guint32)(*pchi >> 32);

			/* The original loop was this: */
			/*
            while (texp > 0 && (overhang > (2<<DECIMAL_MAX_INTFACTORS) || (*pclo & 1) == 0)) {
				if (--texp == 0)
					roundBit = (int)(*pclo & 1);
                rshift128(pclo, pchi);
                overhang = (guint32)(*pchi >> 32);
            }
			*/
			if (overhang > 0) {
				int msf = my_g_bit_nth_msf (overhang);
				int shift = msf - (DECIMAL_MAX_INTFACTORS + 2);

				if (shift >= texp)
					shift = texp - 1;

				if (shift > 0) {
					texp -= shift;
					*pclo = (*pclo >> shift) | ((*pchi & ((1 << shift) - 1)) << (64 - shift));
					*pchi >>= shift;
					overhang >>= shift;

					g_assert (texp > 0);
					g_assert (overhang > (2 << DECIMAL_MAX_INTFACTORS));
				}
			}
            while (texp > 0 && (overhang > (2<<DECIMAL_MAX_INTFACTORS) || (*pclo & 1) == 0)) {
				if (--texp == 0) roundBit = (int)(*pclo & 1);
                rshift128(pclo, pchi);
                overhang >>= 1;
            }

            if (texp > DECIMAL_MAX_INTFACTORS) i = DECIMAL_MAX_INTFACTORS;
            else i = texp;
            if (scale + i > maxScale) i = maxScale - scale;
            if (i == 0) break;
            texp -= i;
            scale += i;
            factor = constantsDecadeInt32Factors[i] >> i; /* 10^i/2^i=5^i */
            mult128by32(pclo, pchi, factor, 0);
    /*printf("3: %.17e\n", (((double)chi) * pow(2,64) + clo) * pow(10, -scale) * pow(2, -texp));*/
        }

        while (texp > 0) {
            if (--texp == 0) roundBit = (int)(*pclo & 1);
            rshift128(pclo, pchi);
        }
    }

    TEST(texp == 0);

    while (scale > maxScale) {
        i = scale - maxScale;
        if (i > DECIMAL_MAX_INTFACTORS) i = DECIMAL_MAX_INTFACTORS;
        scale -= i;
        roundBit = div128by32(pclo, pchi, constantsDecadeInt32Factors[i], 0);
    }

    while (scale < minScale) {
        if (!roundFlag) roundBit = 0;
        i = minScale - scale;
        if (i > DECIMAL_MAX_INTFACTORS) i = DECIMAL_MAX_INTFACTORS;
        scale += i;
        rc = mult128by32(pclo, pchi, constantsDecadeInt32Factors[i], roundBit);
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

    MONO_ARCH_SAVE_REGS;

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
        log2Result = MAX (log2A, log2B);
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

        rc = rescale128(&alo, &ahi,&scaleA, 0, 0, DECIMAL_MAX_SCALE, 1);
    }

    if (rc != DECIMAL_SUCCESS) return rc;

    return pack128toDecimal(pA, alo, ahi, scaleA, sign);
}

/* performs a += factor * constants[idx] */
static int incMultConstant128(guint64* palo, guint64* pahi, int idx, int factor)
{
    guint64 blo, bhi, h;

    PRECONDITION(idx >= 0 && idx <= DECIMAL_MAX_SCALE);
    PRECONDITION(factor > 0 && factor <= 9);

    blo = dec128decadeFactors[idx].lo;
    h = bhi = dec128decadeFactors[idx].hi;
    if (factor != 1) {
        mult128by32(&blo, &bhi, factor, 0);
        if (h > bhi) return DECIMAL_OVERFLOW;
    }
    h = *pahi;
    add128(*palo, *pahi, blo, bhi, palo, pahi);
    if (h > *pahi) return DECIMAL_OVERFLOW;
    return DECIMAL_SUCCESS;
}

DECINLINE static void div128DecadeFactor(guint64* palo, guint64* pahi, int powerOfTen)
{
    int idx, roundBit = 0;

    while (powerOfTen > 0) {
        idx = (powerOfTen > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : powerOfTen;
        powerOfTen -= idx;
        roundBit = div128by32(palo, pahi, constantsDecadeInt32Factors[idx], 0);
    }

    if (roundBit) roundUp128(palo, pahi);
}

/* calc significant digits of mantisse */
DECINLINE static int calcDigits(guint64 alo, guint64 ahi)
{
    int tlog2 = 0;
    int tlog10;

    if (ahi == 0) {
        if (alo == 0) {
            return 0; /* zero has no signficant digits */
        } else {
            tlog2 = log2_64(alo);
        }
    } else {
        tlog2 = 64 + log2_64(ahi);
    }

    tlog10 = (tlog2 * 1000) / 3322;
    /* we need an exact floor value of log10(a) */
    if (dec128decadeFactors[tlog10].hi > ahi
            || (dec128decadeFactors[tlog10].hi == ahi
                    && dec128decadeFactors[tlog10].lo > alo)) {
        --tlog10;
    }
    return tlog10+1;
}

gint32 mono_double2decimal(/*[Out]*/decimal_repr* pA, double val, gint32 digits)
{
    guint64 alo, ahi;
    guint64* p = (guint64*)(&val);
    int sigDigits, sign, texp, rc, scale;
    guint16 k;

    PRECONDITION(digits <= 15);

    sign = ((*p & LIT_GUINT64_HIGHBIT) != 0) ? 1 : 0;
    k = ((guint16)((*p) >> 52)) & 0x7FF;
    alo = (*p & LIT_GUINT64(0xFFFFFFFFFFFFF)) | LIT_GUINT64(0x10000000000000);
    ahi = 0;

    texp = (k & 0x7FF) - 0x3FF;
    if (k == 0x7FF || texp >= 96) return DECIMAL_OVERFLOW; /* NaNs, SNaNs, Infinities or >= 2^96 */
    if (k == 0 || texp <= -94) { /* Subnormals, Zeros or < 2^-94 */
        DECINIT(pA); /* return zero */
        return DECIMAL_SUCCESS;
    }

    texp -= 52;
    if (texp > 0) {
        for (; texp > 0; texp--) {
            lshift128(&alo, &ahi);
        }
    }

    scale = 0;
    rc = rescale128(&alo, &ahi, &scale, -texp, 0, DECIMAL_MAX_SCALE, 0);
    if (rc != DECIMAL_SUCCESS) return rc;

    sigDigits = calcDigits(alo, ahi);
    /* too much digits, then round */
    if (sigDigits > digits) {
        div128DecadeFactor(&alo, &ahi, sigDigits - digits);
        scale -= sigDigits - digits;
        /* check value, may be 10^(digits+1) caused by rounding */
        if (ahi == dec128decadeFactors[digits].hi
            && alo == dec128decadeFactors[digits].lo) {
            div128by32(&alo, &ahi, 10, 0);
            scale--;
        }
        if (scale < 0) {
            rc = mult128DecadeFactor(&alo, &ahi, -scale);
            if (rc != DECIMAL_SUCCESS) return rc;
            scale = 0;
        }
    }

    return pack128toDecimal(pA, alo, ahi, scale, sign);
}

/**
 * mono_string2decimal:
 * @decimal_repr:
 * @str:
 * @decrDecimal:
 * @sign:
 *
 * converts a digit string to decimal
 * The significant digits must be passed as an integer in buf !
 *
 * 1. Example:
 *   if you want to convert the number 123.456789012345678901234 to decimal
 *     buf := "123456789012345678901234"
 *     decrDecimal := 3
 *     sign := 0
 *
 * 2. Example:
 *   you want to convert -79228162514264337593543950335 to decimal
 *     buf := "79228162514264337593543950335"
 *     decrDecimal := 29
 *     sign := 1
 *
 * 3. Example:
 *   you want to convert -7922816251426433759354395033.250000000000001 to decimal
 *     buf := "7922816251426433759354395033250000000000001"
 *     decrDecimal := 29
 *     sign := 1
 *     returns (decimal)-7922816251426433759354395033.3
 *
 * 4. Example:
 *   you want to convert -7922816251426433759354395033.250000000000000 to decimal
 *     buf := "7922816251426433759354395033250000000000000"
 *     decrDecimal := 29
 *     sign := 1
 *     returns (decimal)-7922816251426433759354395033.2
 *
 * 5. Example:
 *   you want to convert -7922816251426433759354395033.150000000000000 to decimal
 *     buf := "7922816251426433759354395033150000000000000"
 *     decrDecimal := 29
 *     sign := 1
 *     returns (decimal)-7922816251426433759354395033.2
 *
 * Uses banker's rule for rounding if there are more digits than can be
 * represented by the significant
 */
gint32 mono_string2decimal(/*[Out]*/decimal_repr* pA, MonoString* str, gint32 decrDecimal, gint32 sign)
{
    gushort *buf = mono_string_chars(str);
    gushort *p;
    guint64 alo, ahi;
    int n, rc, i, len, sigLen = -1, firstNonZero;
    int scale, roundBit = 0;

    alo = ahi = 0;
    DECINIT(pA);

    for (p = buf, len = 0; *p != 0; len++, p++) { }

    for (p = buf, i = 0; *p != 0; i++, p++) {
        n = *p - '0';
        if (n < 0 || n > 9) {
            return DECIMAL_INVALID_CHARACTER;
        }
        if (n) {
            if (sigLen < 0) {
                firstNonZero = i;
                sigLen = (len - firstNonZero > DECIMAL_MAX_SCALE+1)
                    ? DECIMAL_MAX_SCALE+1+firstNonZero : len;
                if (decrDecimal > sigLen+1) return DECIMAL_OVERFLOW;
            }
            if (i >= sigLen) break;
            rc = incMultConstant128(&alo, &ahi, sigLen - 1 - i, n);
            if (rc != DECIMAL_SUCCESS) {
                return rc;
            }
        }
    }

    scale = sigLen - decrDecimal;

    if (i < len) { /* too much digits, we must round */
        n = buf[i] - '0';
        if (n < 0 || n > 9) {
            return DECIMAL_INVALID_CHARACTER;
        }
        if (n > 5) roundBit = 1;
        else if (n == 5) { /* we must take a nearer look */
            n = buf[i-1] - '0';
            for (++i; i < len; ++i) {
                if (buf[i] != '0') break; /* we are greater than .5 */
            }
            if (i < len /* greater than exactly .5 */
                || n % 2 == 1) { /* exactly .5, use banker's rule for rounding */
                roundBit = 1;
            }
        }
    }

    if (ahi != 0) {
        rc = normalize128(&alo, &ahi, &scale, 1, roundBit);
        if (rc != DECIMAL_SUCCESS) return rc;
    }

    if (alo == 0 && ahi == 0) {
        DECINIT(pA);
        return DECIMAL_SUCCESS;
    } else {
        return pack128toDecimal(pA, alo, ahi, sigLen - decrDecimal, sign);
    }
}

/**
 * mono_decimal2string:
 * @
 * returns minimal number of digit string to represent decimal
 * No leading or trailing zeros !
 * Examples:
 * *pA == 0            =>   buf = "", *pDecPos = 1, *pSign = 0
 * *pA == 12.34        =>   buf = "1234", *pDecPos = 2, *pSign = 0
 * *pA == -1000.0000   =>   buf = "1", *pDecPos = 4, *pSign = 1
 * *pA == -0.00000076  =>   buf = "76", *pDecPos = -6, *pSign = 0
 * 
 * Parameters:
 *    pA         decimal instance to convert     
 *    digits     < 0: use decimals instead
 *               = 0: gets mantisse as integer
 *               > 0: gets at most <digits> digits, rounded according to banker's rule if necessary
 *    decimals   only used if digits < 0
 *               >= 0: number of decimal places
 *    buf        pointer to result buffer
 *    bufSize    size of buffer
 *    pDecPos    receives insert position of decimal point relative to start of buffer
 *    pSign      receives sign
 */
gint32 mono_decimal2string(/*[In]*/decimal_repr* pA, gint32 digits, gint32 decimals,
                                   MonoArray* pArray, gint32 bufSize, gint32* pDecPos, gint32* pSign)
{
    guint16 tmp[41];
    guint16 *buf = (guint16*) mono_array_addr(pArray, guint16, 0);
    guint16 *q, *p = tmp;
    decimal_repr aa;
    guint64 alo, ahi;
    guint32 rest;
    gint32 sigDigits, d;
    int i, scale, len;

    MONO_ARCH_SAVE_REGS;

    scale = pA->signscale.scale;
    DECTO128(pA, alo, ahi);
    sigDigits = calcDigits(alo, ahi); /* significant digits */

    /* calc needed digits (without leading or trailing zeros) */
    d = (digits == 0) ? sigDigits : digits;
    if (d < 0) { /* use decimals ? */
        if (0 <= decimals && decimals < scale) {
            d = sigDigits - scale + decimals;
        } else {
            d = sigDigits; /* use all you can get */
        }
    } 

    if (sigDigits > d) { /* we need to round decimal number */
        DECCOPY(&aa, pA);
        aa.signscale.scale = DECIMAL_MAX_SCALE;
        mono_decimalRound(&aa, DECIMAL_MAX_SCALE - sigDigits + d);
        DECTO128(&aa, alo, ahi);
        sigDigits += calcDigits(alo, ahi) - d;
    }

    len = 0;
    if (d > 0) {
        /* get digits starting from the tail */
        for (; (alo != 0 || ahi != 0) && len < 40; len++) {
            div128by32(&alo, &ahi, 10, &rest);
            *p++ = '0' + (char) rest;
        }
    }
    *p = 0;

    if (len >= bufSize) return DECIMAL_BUFFER_OVERFLOW;

    /* now we have the minimal count of digits, 
       extend to wished count of digits or decimals */
    q = buf;
    if (digits >= 0) { /* count digits */
        if (digits >= bufSize) return DECIMAL_BUFFER_OVERFLOW;
        if (len == 0) {
            /* zero or rounded to zero */
            *pDecPos = 1;
        } else {
            /* copy significant digits */
            for (i = 0; i < len; i++) {
                *q++ = *(--p);
            }
            *pDecPos = sigDigits - scale;
        }
        /* add trailing zeros */
        for (i = len; i < digits; i++) {
            *q++ = '0';
        }
    } else { /* count decimals */
        if (scale >= sigDigits) { /* add leading zeros */
            if (decimals+2 >= bufSize) return DECIMAL_BUFFER_OVERFLOW;
            *pDecPos = 1;
            for (i = 0; i <= scale - sigDigits; i++) {
                *q++ = '0';
            }
        } else {
            if (sigDigits - scale + decimals+1 >= bufSize) return DECIMAL_BUFFER_OVERFLOW;
            *pDecPos = sigDigits - scale;
        }
        /* copy significant digits */
        for (i = 0; i < len; i++) {
            *q++ = *(--p);
        }
        /* add trailing zeros */
        for (i = scale; i < decimals; i++) {
            *q++ = '0';
        }
    }
    *q = 0;

    *pSign = (sigDigits > 0) ? pA->signscale.sign : 0; /* zero has positive sign */

    return DECIMAL_SUCCESS;
}

/**
 * mono_decimal2UInt64:
 * @pA
 * @pResult
 * converts a decimal to an UInt64 without rounding
 */
gint32 mono_decimal2UInt64(/*[In]*/decimal_repr* pA, guint64* pResult)
{
    guint64 alo, ahi;
    int scale;

    MONO_ARCH_SAVE_REGS;

    DECTO128(pA, alo, ahi);
    scale = pA->signscale.scale;
    if (scale > 0) {
        div128DecadeFactor(&alo, &ahi, scale);
    }

    /* overflow if integer too large or < 0 */
    if (ahi != 0 || (alo != 0 && pA->signscale.sign)) return DECIMAL_OVERFLOW;

    *pResult = alo;
    return DECIMAL_SUCCESS;
}

/**
 * mono_decimal2Int64:
 * @pA:
 * pResult:
 * converts a decimal to an Int64 without rounding
 */
gint32 mono_decimal2Int64(/*[In]*/decimal_repr* pA, gint64* pResult)
{
    guint64 alo, ahi;
    int sign, scale;

    MONO_ARCH_SAVE_REGS;

    DECTO128(pA, alo, ahi);
    scale = pA->signscale.scale;
    if (scale > 0) {
        div128DecadeFactor(&alo, &ahi, scale);
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
    int scale, sign, idx;
    int hasRest = 0;

    MONO_ARCH_SAVE_REGS;

    scale = pA->signscale.scale;
    if (scale == 0) return; /* nothing to do */

    DECTO128(pA, alo, ahi);
    sign = pA->signscale.sign;

    while (scale > 0) {
        idx = (scale > DECIMAL_MAX_INTFACTORS) ? DECIMAL_MAX_INTFACTORS : scale;
        factor = constantsDecadeInt32Factors[idx];
        scale -= idx;
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

    MONO_ARCH_SAVE_REGS;

    DECTO128(pA, alo, ahi);
    scale = pA->signscale.scale;
    sign = pA->signscale.sign;
    if (scale > decimals) {
        div128DecadeFactor(&alo, &ahi, scale - decimals);
        scale = decimals;
    }
    
    pack128toDecimal(pA, alo, ahi, scale, sign);
}

gint32 mono_decimalMult(/*[In, Out]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
    guint64 low, mid, high;
    guint32 factor;
    int scale, sign, rc;

    MONO_ARCH_SAVE_REGS;

    mult96by96to192(pA->lo32, pA->mid32, pA->hi32, pB->lo32, pB->mid32, pB->hi32,
        &low, &mid, &high);

    /* adjust scale and sign */
    scale = (int)pA->signscale.scale + (int)pB->signscale.scale;
    sign = pA->signscale.sign ^ pB->signscale.sign;

    /* first scaling step */
    factor = constantsDecadeInt32Factors[DECIMAL_MAX_INTFACTORS];
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

static DECINLINE int decimalDivSub(/*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB,
								   guint64* pclo, guint64* pchi, int* pExp)
{
    guint64 alo, ami, ahi;
    guint64 tlo, tmi, thi;
    guint32 blo, bmi, bhi;
    int ashift, bshift, extraBit, texp;

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
	if (ahi == 0) {
		ahi = ami;
		ami = 0;
		for (ashift = 64; (ahi & LIT_GUINT64_HIGHBIT) == 0; ++ashift) {
			ahi <<= 1;
		}
	} else {
		for (ashift = 0; (ahi & LIT_GUINT64_HIGHBIT) == 0; ++ashift) {
			lshift128(&ami, &ahi);
		}
	}

    /* ensure that divisor is at least 2^95 */
	if (bhi == 0) {

		if (bmi == 0) {
			guint32 hi_shift;
			bhi = blo;
			bmi = 0;
			blo = 0;

			//g_assert (g_bit_nth_msf (bhi, 32) == my_g_bit_nth_msf (bhi));

			hi_shift = 31 - my_g_bit_nth_msf (bhi);
			bhi <<= hi_shift;
			bshift = 64 + hi_shift;
		} else {
			bhi = bmi;
			bmi = blo;
			blo = 0;

			for (bshift = 32; (bhi & LIT_GUINT32_HIGHBIT) == 0; ++bshift) {
				bhi <<= 1;
				bhi |= (bmi & LIT_GUINT32_HIGHBIT) >> 31;
				bmi <<= 1;
			}
		}
	} else {
		for (bshift = 0; (bhi & LIT_GUINT32_HIGHBIT) == 0; ++bshift) {
			bhi <<= 1;
			bhi |= (bmi & LIT_GUINT32_HIGHBIT) >> 31;
			bmi <<= 1;
			bmi |= (blo & LIT_GUINT32_HIGHBIT) >> 31;
			blo <<= 1;
		}
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
    texp = 128 + ashift - bshift;

    if (extraBit) {
        rshift128(pclo, pchi);
        *pchi += LIT_GUINT64_HIGHBIT;
        texp--;
    }

    /* try loss free right shift */
    while (texp > 0 && (*pclo & 1) == 0) {
        /* right shift */
        rshift128(pclo, pchi);
        texp--;
    }

    *pExp = texp;

    return DECIMAL_SUCCESS;
}

gint32 mono_decimalDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
    guint64 clo, chi; /* result */
    int scale, texp, rc;

    MONO_ARCH_SAVE_REGS;

	/* Check for common cases */
	if (mono_decimalCompare (pA, pB) == 0)
		/* One */
		return pack128toDecimal (pC, 1, 0, 0, 0);
	pA->signscale.sign = pA->signscale.sign ? 0 : 1;
	if (mono_decimalCompare (pA, pB) == 0)
		/* Minus one */
		return pack128toDecimal (pC, 1, 0, 0, 1);
	pA->signscale.sign = pA->signscale.sign ? 0 : 1;

    rc = decimalDivSub(pA, pB, &clo, &chi, &texp);
    if (rc != DECIMAL_SUCCESS) {
        if (rc == DECIMAL_FINISHED) rc = DECIMAL_SUCCESS;
        return rc;
    }

    /* adjust scale and sign */
    scale = (int)pA->signscale.scale - (int)pB->signscale.scale;

    /*test: printf("0: %.17e\n", (((double)chi) * pow(2,64) + clo) * pow(10, -scale) * pow(2, -exp));*/
    rc = rescale128(&clo, &chi, &scale, texp, 0, DECIMAL_MAX_SCALE, 1);
    if (rc != DECIMAL_SUCCESS) return rc;

    return pack128toDecimal(pC, clo, chi, scale, pA->signscale.sign ^ pB->signscale.sign);
}

gint32 mono_decimalIntDiv(/*[Out]*/decimal_repr* pC, /*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
    guint64 clo, chi; /* result */
    int scale, texp, rc;

    MONO_ARCH_SAVE_REGS;

    rc = decimalDivSub(pA, pB, &clo, &chi, &texp);
    if (rc != DECIMAL_SUCCESS) {
        if (rc == DECIMAL_FINISHED) rc = DECIMAL_SUCCESS;
        return rc;
    }

    /* calc scale  */
    scale = (int)pA->signscale.scale - (int)pB->signscale.scale;

    /* truncate result to integer */
    rc = rescale128(&clo, &chi, &scale, texp, 0, 0, 0);
    if (rc != DECIMAL_SUCCESS) return rc;

    return pack128toDecimal(pC, clo, chi, scale, pA->signscale.sign);
}

/* approximation for log2 of a 
   If q is the exact value for log2(a), then q <= decimalLog2(a) <= q+1 */
DECINLINE static int decimalLog2(/*[In]*/decimal_repr* pA)
{
    int tlog2;
    int scale = pA->signscale.scale;

    if (pA->hi32 != 0) tlog2 = 64 + log2_32(pA->hi32);
    else if (pA->mid32 != 0) tlog2 = 32 + log2_32(pA->mid32);
    else tlog2 = log2_32(pA->lo32);

    if (tlog2 != DECIMAL_LOG_NEGINF) {
        tlog2 -= (scale * 33219) / 10000;
    }

    return tlog2;
}

DECINLINE static int decimalIsZero(/*[In]*/decimal_repr* pA)
{
    return (pA->lo32 == 0 && pA->mid32 == 0 && pA->hi32 == 0);
}

gint32 mono_decimalCompare(/*[In]*/decimal_repr* pA, /*[In]*/decimal_repr* pB)
{
    int log2a, log2b, delta, sign;
    decimal_repr aa;

    MONO_ARCH_SAVE_REGS;

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
DECINLINE static void buildIEEE754Double(double* pd, int sign, int texp, guint64 mantisse)
{
    guint64* p = (guint64*) pd;

    PRECONDITION(sign == 0 || sign == 1);
    *p = (((guint64)sign) << 63) | (((guint64)((1023+texp)&0x7ff)) << 52) | mantisse;
#ifdef ARM_FPU_FPA
#if G_BYTE_ORDER == G_LITTLE_ENDIAN
    {
	    guint32 temp;
	    guint32 *t = (guint32*)p;
	    temp = t [0];
	    t [0] = t [1];
	    t [1] = temp;
    }
#endif
#endif
}

double mono_decimal2double(/*[In]*/decimal_repr* pA)
{
    double d;
    guint64 alo, ahi, mantisse;
    guint32 overhang, factor, roundBits;
    int scale, texp, log5, i;

    MONO_ARCH_SAVE_REGS;

    ahi = (((guint64)(pA->hi32)) << 32) | pA->mid32;
    alo = ((guint64)(pA->lo32)) << 32;

    /* special case zero */
    if (ahi == 0 && alo == 0) return 0.0;

    texp = 0;
    scale = pA->signscale.scale;

    /* transform n * 10^-scale and exp = 0 => m * 2^-exp and scale = 0 */
    while (scale > 0) {
        while ((ahi & LIT_GUINT64_HIGHBIT) == 0) {
            lshift128(&alo, &ahi);
            texp++;
        }

        overhang = (guint32) (ahi >> 32);
        if (overhang >= 5) {
            /* estimate log5 */
            log5 = (log2_32(overhang) * 1000) / 2322; /* ln(5)/ln(2) = 2.3219... */
            if (log5 < DECIMAL_MAX_INTFACTORS) {
                /* get maximal factor=5^i, so that overhang / factor >= 1 */
                factor = constantsDecadeInt32Factors[log5] >> log5; /* 5^n = 10^n/2^n */
                i = log5 + overhang / factor;
            } else {
                i = DECIMAL_MAX_INTFACTORS; /* we have only constants up to 10^DECIMAL_MAX_INTFACTORS */
            }
            if (i > scale) i = scale;
            factor = constantsDecadeInt32Factors[i] >> i; /* 5^n = 10^n/2^n */
            /* n * 10^-scale * 2^-exp => m * 10^-(scale-i) * 2^-(exp+i) with m = n * 5^-i */
            div128by32(&alo, &ahi, factor, 0);
            scale -= i;
            texp += i;
        }
    }

    /* normalize significand (highest bit should be 1) */
    while ((ahi & LIT_GUINT64_HIGHBIT) == 0) {
        lshift128(&alo, &ahi);
        texp++;
    }

    /* round to nearest even */
    roundBits = (guint32)ahi & 0x7ff;
    ahi += 0x400;
    if ((ahi & LIT_GUINT64_HIGHBIT) == 0) { /* overflow ? */
        ahi >>= 1;
	texp--;
    } else if ((roundBits & 0x400) == 0) ahi &= ~1;

    /* 96 bit => 1 implizit bit and 52 explicit bits */
    mantisse = (ahi & ~LIT_GUINT64_HIGHBIT) >> 11;

    buildIEEE754Double(&d, pA->signscale.sign, -texp+95, mantisse);

    return d;
}

/* a *= 10^exp */
gint32 mono_decimalSetExponent(/*[In, Out]*/decimal_repr* pA, gint32 texp)
{
    guint64 alo, ahi;
    int rc;
    int scale = pA->signscale.scale;

    MONO_ARCH_SAVE_REGS;

    scale -= texp;

    if (scale < 0 || scale > DECIMAL_MAX_SCALE) {
        DECTO128(pA, alo, ahi);
        rc = rescale128(&alo, &ahi, &scale, 0, 0, DECIMAL_MAX_SCALE, 1);
        if (rc != DECIMAL_SUCCESS) return rc;
        return pack128toDecimal(pA, alo, ahi, scale, pA->signscale.sign);
    } else {
        pA->signscale.scale = scale;
        return DECIMAL_SUCCESS;
    }
}

#endif /* DISABLE_DECIMAL */

