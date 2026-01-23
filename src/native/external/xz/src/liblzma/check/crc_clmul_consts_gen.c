// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       crc_clmul_consts_gen.c
/// \brief      Generate constants for CLMUL CRC code
///
/// Compiling: gcc -std=c99 -o crc_clmul_consts_gen crc_clmul_consts_gen.c
///
/// This is for CRCs that use reversed bit order (bit reflection).
/// The same CLMUL CRC code can be used with CRC64 and smaller ones like
/// CRC32 apart from one special case: CRC64 needs an extra step in the
/// Barrett reduction to handle the 65th bit; the smaller ones don't.
/// Otherwise it's enough to just change the polynomial and the derived
/// constants and use the same code.
///
/// See the Intel white paper "Fast CRC Computation for Generic Polynomials
/// Using PCLMULQDQ Instruction" from 2009.
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include <inttypes.h>
#include <stdio.h>


/// CRC32 (Ethernet) polynomial in reversed representation
static const uint64_t p32 = 0xedb88320;

// CRC64 (ECMA-182) polynomial in reversed representation
static const uint64_t p64 = 0xc96c5795d7870f42;


/// Calculates floor(x^128 / p) where p is a CRC64 polynomial in
/// reversed representation. The result is in reversed representation too.
static uint64_t
calc_cldiv(uint64_t p)
{
	// Quotient
	uint64_t q = 0;

	// Align the x^64 term with the x^128 (the implied high bits of the
	// divisor and the dividend) and do the first step of polynomial long
	// division, calculating the first remainder. The variable q remains
	// zero because the highest bit of the quotient is an implied bit 1
	// (we kind of set q = 1 << -1).
	uint64_t r = p;

	// Then process the remaining 64 terms. Note that r has no implied
	// high bit, only q and p do. (And remember that a high bit in the
	// polynomial is stored at a low bit in the variable due to the
	// reversed bit order.)
	for (unsigned i = 0; i < 64; ++i) {
		q |= (r & 1) << i;
		r = (r >> 1) ^ (r & 1 ? p : 0);
	}

	return q;
}


/// Calculate the remainder of carryless division:
///
///     x^(bits + n - 1) % p, where n=64 (for CRC64)
///
/// p must be in reversed representation which omits the bit of
/// the highest term of the polynomial. Instead, it is an implied bit
/// at kind of like "1 << -1" position, as if it had just been shifted out.
///
/// The return value is in the reversed bit order. (There are no implied bits.)
static uint64_t
calc_clrem(uint64_t p, unsigned bits)
{
	// Do the first step of polynomial long division.
	uint64_t r = p;

	// Then process the remaining terms. Start with i = 1 instead of i = 0
	// to account for the -1 in x^(bits + n - 1). This -1 is convenient
	// with the reversed bit order. See the "Bit-Reflection" section in
	// the Intel white paper.
	for (unsigned i = 1; i < bits; ++i)
		r = (r >> 1) ^ (r & 1 ? p : 0);

	return r;
}


extern int
main(void)
{
	puts("// CRC64");

	// The order of the two 64-bit constants in a vector don't matter.
	// It feels logical to put them in this order as it matches the
	// order in which the input bytes are read.
	printf("const __m128i fold512 = _mm_set_epi64x("
		"0x%016" PRIx64 ", 0x%016" PRIx64 ");\n",
		calc_clrem(p64, 4 * 128 - 64),
		calc_clrem(p64, 4 * 128));

	printf("const __m128i fold128 = _mm_set_epi64x("
		"0x%016" PRIx64 ", 0x%016" PRIx64 ");\n",
		calc_clrem(p64, 128 - 64),
		calc_clrem(p64, 128));

	// When we multiply by mu, we care about the high bits of the result
	// (in reversed bit order!). It doesn't matter that the low bit gets
	// shifted out because the affected output bits will be ignored.
	// Below we add the implied high bit with "| 1" after the shifting
	// so that the high bits of the multiplication will be correct.
	//
	// p64 is shifted left by one so that the final multiplication
	// in Barrett reduction won't be misaligned by one bit. We could
	// use "(p64 << 1) | 1" instead of "p64 << 1" too but it makes
	// no difference as that bit won't affect the relevant output bits
	// (we only care about the lowest 64 bits of the result, that is,
	// lowest in the reversed bit order).
	//
	// NOTE: The 65rd bit of p64 gets shifted out. It needs to be
	// compensated with 64-bit shift and xor in the CRC64 code.
	printf("const __m128i mu_p = _mm_set_epi64x("
		"0x%016" PRIx64 ", 0x%016" PRIx64 ");\n",
		(calc_cldiv(p64) << 1) | 1,
		p64 << 1);

	puts("");

	puts("// CRC32");

	printf("const __m128i fold512 = _mm_set_epi64x("
		"0x%08" PRIx64 ", 0x%08" PRIx64 ");\n",
		calc_clrem(p32, 4 * 128 - 64),
		calc_clrem(p32, 4 * 128));

	printf("const __m128i fold128 = _mm_set_epi64x("
		"0x%08" PRIx64 ", 0x%08" PRIx64 ");\n",
		calc_clrem(p32, 128 - 64),
		calc_clrem(p32, 128));

	// CRC32 calculation is done by modulus scaling it to a CRC64.
	// Since the CRC is in reversed representation, only the mu
	// constant changes with the modulus scaling. This method avoids
	// one additional constant and one additional clmul in the final
	// reduction steps, making the code both simpler and faster.
	//
	// p32 is shifted left by one so that the final multiplication
	// in Barrett reduction won't be misaligned by one bit. We could
	// use "(p32 << 1) | 1" instead of "p32 << 1" too but it makes
	// no difference as that bit won't affect the relevant output bits.
	//
	// NOTE: The 33-bit value fits in 64 bits so, unlike with CRC64,
	// there is no need to compensate for any missing bits in the code.
	printf("const __m128i mu_p = _mm_set_epi64x("
		"0x%016" PRIx64 ", 0x%" PRIx64 ");\n",
		(calc_cldiv(p32) << 1) | 1,
		p32 << 1);

	return 0;
}
