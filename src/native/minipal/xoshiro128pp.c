// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <minipal/xoshiro128pp.h>

// This code is a slightly modified version of the xoshiro128++ generator from http://prng.di.unimi.it/xoshiro128plusplus.c

/*  Written in 2019 by David Blackman and Sebastiano Vigna (vigna@acm.org)
To the extent possible under law, the author has dedicated all copyright
and related and neighboring rights to this software to the public domain
worldwide.

See <http://creativecommons.org/publicdomain/zero/1.0/>.
*/

static inline uint32_t rotl(const uint32_t x, int k) {
    return (x << k) | (x >> (32 - k));
}

/* This is the jump function for the generator. It is equivalent
	to 2^64 calls to next(); it can be used to generate 2^64
	non-overlapping subsequences for parallel computations. */

static void jump(struct minipal_xoshiro128pp* pState) {
	static const uint32_t JUMP[] = { 0x8764000b, 0xf542d2d3, 0x6fa035c3, 0x77f2db5b };

    uint32_t* s = pState->s;
	uint32_t s0 = 0;
	uint32_t s1 = 0;
	uint32_t s2 = 0;
	uint32_t s3 = 0;
	for (int i = 0; i < sizeof JUMP / sizeof * JUMP; i++)
		for (int b = 0; b < 32; b++) {
			if (JUMP[i] & UINT32_C(1) << b) {
				s0 ^= s[0];
				s1 ^= s[1];
				s2 ^= s[2];
				s3 ^= s[3];
			}
			minipal_xoshiro128pp_next(pState);
		}

	s[0] = s0;
	s[1] = s1;
	s[2] = s2;
	s[3] = s3;
}

void minipal_xoshiro128pp_init(struct minipal_xoshiro128pp* pState, uint32_t seed) {
    uint32_t* s = pState->s;
	if (seed == 0)
	{
		seed = 997;
	}

	s[0] = seed;
	s[1] = seed;
	s[2] = seed;
	s[3] = seed;
	jump(pState);
}

uint32_t minipal_xoshiro128pp_next(struct minipal_xoshiro128pp* pState) {
    uint32_t* s = pState->s;
	const uint32_t result = rotl(s[0] + s[3], 7) + s[0];

	const uint32_t t = s[1] << 9;

	s[2] ^= s[0];
	s[3] ^= s[1];
	s[1] ^= s[2];
	s[0] ^= s[3];

	s[2] ^= t;

	s[3] = rotl(s[3], 11);

	return result;
}


