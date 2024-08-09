#pragma once

/*  Written in 2019 by David Blackman and Sebastiano Vigna (vigna@acm.org)

To the extent possible under law, the author has dedicated all copyright
and related and neighboring rights to this software to the public domain
worldwide.

Permission to use, copy, modify, and/or distribute this software for any
purpose with or without fee is hereby granted.

THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF OR
IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE. */

#include <stdint.h>

/* This is xoshiro128++ 1.0, one of our 32-bit all-purpose, rock-solid
   generators. It has excellent speed, a state size (128 bits) that is
   large enough for mild parallelism, and it passes all tests we are aware
   of.

   For generating just single-precision (i.e., 32-bit) floating-point
   numbers, xoshiro128+ is even faster.

   The state must be seeded so that it is not everywhere zero. */

// Note: the code has been changed to avoid static state in multi-threaded usage

struct sxoshiro128pp
{
	static inline uint32_t rotl(const uint32_t x, int k) {
		return (x << k) | (x >> (32 - k));
	}

	uint32_t s[4];

	void InitSeed(uint32_t seed)
	{
		if (seed == 0)
		{
			seed = 997;
		}

		s[0] = seed;
		s[1] = seed;
		s[2] = seed;
		s[3] = seed;
		jump();
	}

	uint32_t next(void) {
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


	/* This is the jump function for the generator. It is equivalent
	   to 2^64 calls to next(); it can be used to generate 2^64
	   non-overlapping subsequences for parallel computations. */

	void jump(void) {
		static const uint32_t JUMP[] = { 0x8764000b, 0xf542d2d3, 0x6fa035c3, 0x77f2db5b };

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
				next();
			}

		s[0] = s0;
		s[1] = s1;
		s[2] = s2;
		s[3] = s3;
	}


	/* This is the long-jump function for the generator. It is equivalent to
	   2^96 calls to next(); it can be used to generate 2^32 starting points,
	   from each of which jump() will generate 2^32 non-overlapping
	   subsequences for parallel distributed computations. */

	void long_jump(void) {
		static const uint32_t LONG_JUMP[] = { 0xb523952e, 0x0b6f099f, 0xccf5a0ef, 0x1c580662 };

		uint32_t s0 = 0;
		uint32_t s1 = 0;
		uint32_t s2 = 0;
		uint32_t s3 = 0;
		for (int i = 0; i < sizeof LONG_JUMP / sizeof * LONG_JUMP; i++)
			for (int b = 0; b < 32; b++) {
				if (LONG_JUMP[i] & UINT32_C(1) << b) {
					s0 ^= s[0];
					s1 ^= s[1];
					s2 ^= s[2];
					s3 ^= s[3];
				}
				next();
			}

		s[0] = s0;
		s[1] = s1;
		s[2] = s2;
		s[3] = s3;
	}
};