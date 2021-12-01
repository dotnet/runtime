/*
 * SSE optimized hash slide
 *
 * Copyright (C) 2017 Intel Corporation
 * Authors:
 *   Arjan van de Ven	<arjan@linux.intel.com>
 *   Jim Kukunas	<james.t.kukunas@linux.intel.com>
 *
 * For conditions of distribution and use, see copyright notice in zlib.h
 */
#include "deflate.h"

#ifdef USE_SSE_SLIDE
#include <immintrin.h>

void slide_hash_sse(deflate_state *s)
{
    unsigned n;
    Posf *p;
    uInt wsize = s->w_size;
    z_const __m128i xmm_wsize = _mm_set1_epi16(s->w_size);

    n = s->hash_size;
    p = &s->head[n] - 8;
    do {
        __m128i value, result;

	value = _mm_loadu_si128((__m128i *)p);
	result= _mm_subs_epu16(value, xmm_wsize);
	_mm_storeu_si128((__m128i *)p, result);
	p -= 8;
	n -= 8;
    } while (n > 0);

#ifndef FASTEST
    n = wsize;
    p = &s->prev[n] - 8;
    do {
        __m128i value, result;

	value = _mm_loadu_si128((__m128i *)p);
	result= _mm_subs_epu16(value, xmm_wsize);
	_mm_storeu_si128((__m128i *)p, result);

	p -= 8;
	n -= 8;
    } while (n > 0);
#endif
}

#endif

