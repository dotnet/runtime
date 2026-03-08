// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       range_decoder.h
/// \brief      Range Decoder
///
//  Authors:    Igor Pavlov
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_RANGE_DECODER_H
#define LZMA_RANGE_DECODER_H

#include "range_common.h"


// Choose the range decoder variants to use using a bitmask.
// If no bits are set, only the basic version is used.
// If more than one version is selected for the same feature,
// the last one on the list below is used.
//
// Bitwise-or of the following enable branchless C versions:
//   0x01   normal bittrees
//   0x02   fixed-sized reverse bittrees
//   0x04   variable-sized reverse bittrees (not faster)
//   0x08   matched literal (not faster)
//
// GCC & Clang compatible x86-64 inline assembly:
//   0x010   normal bittrees
//   0x020   fixed-sized reverse bittrees
//   0x040   variable-sized reverse bittrees
//   0x080   matched literal
//   0x100   direct bits
//
// The default can be overridden at build time by defining
// LZMA_RANGE_DECODER_CONFIG to the desired mask.
//
// 2024-02-22: Feedback from benchmarks:
//   - Brancless C (0x003) can be better than basic on x86-64 but often it's
//     slightly worse on other archs. Since asm is much better on x86-64,
//     branchless C is not used at all.
//   - With x86-64 asm, there are slight differences between GCC and Clang
//     and different processors. Overall 0x1F0 seems to be the best choice.
#ifndef LZMA_RANGE_DECODER_CONFIG
#	if defined(__x86_64__) && !defined(__ILP32__) \
			&& !defined(__NVCOMPILER) \
			&& (defined(__GNUC__) || defined(__clang__))
#		define LZMA_RANGE_DECODER_CONFIG 0x1F0
#	else
#		define LZMA_RANGE_DECODER_CONFIG 0
#	endif
#endif


// Negative RC_BIT_MODEL_TOTAL but the lowest RC_MOVE_BITS are flipped.
// This is useful for updating probability variables in branchless decoding:
//
//     uint32_t decoded_bit = ...;
//     probability tmp = RC_BIT_MODEL_OFFSET;
//     tmp &= decoded_bit - 1;
//     prob -= (prob + tmp) >> RC_MOVE_BITS;
#define RC_BIT_MODEL_OFFSET \
	((UINT32_C(1) << RC_MOVE_BITS) - 1 - RC_BIT_MODEL_TOTAL)


typedef struct {
	uint32_t range;
	uint32_t code;
	uint32_t init_bytes_left;
} lzma_range_decoder;


/// Reads the first five bytes to initialize the range decoder.
static inline lzma_ret
rc_read_init(lzma_range_decoder *rc, const uint8_t *restrict in,
		size_t *restrict in_pos, size_t in_size)
{
	while (rc->init_bytes_left > 0) {
		if (*in_pos == in_size)
			return LZMA_OK;

		// The first byte is always 0x00. It could have been omitted
		// in LZMA2 but it wasn't, so one byte is wasted in every
		// LZMA2 chunk.
		if (rc->init_bytes_left == 5 && in[*in_pos] != 0x00)
			return LZMA_DATA_ERROR;

		rc->code = (rc->code << 8) | in[*in_pos];
		++*in_pos;
		--rc->init_bytes_left;
	}

	return LZMA_STREAM_END;
}


/// Makes local copies of range decoder and *in_pos variables. Doing this
/// improves speed significantly. The range decoder macros expect also
/// variables 'in' and 'in_size' to be defined.
#define rc_to_local(range_decoder, in_pos, fast_mode_in_required) \
	lzma_range_decoder rc = range_decoder; \
	const uint8_t *rc_in_ptr = in + (in_pos); \
	const uint8_t *rc_in_end = in + in_size; \
	const uint8_t *rc_in_fast_end \
			= (rc_in_end - rc_in_ptr) <= (fast_mode_in_required) \
			? rc_in_ptr \
			: rc_in_end - (fast_mode_in_required); \
	(void)rc_in_fast_end; /* Silence a warning with HAVE_SMALL. */ \
	uint32_t rc_bound


/// Evaluates to true if there is enough input remaining to use fast mode.
#define rc_is_fast_allowed() (rc_in_ptr < rc_in_fast_end)


/// Stores the local copes back to the range decoder structure.
#define rc_from_local(range_decoder, in_pos) \
do { \
	range_decoder = rc; \
	in_pos = (size_t)(rc_in_ptr - in); \
} while (0)


/// Resets the range decoder structure.
#define rc_reset(range_decoder) \
do { \
	(range_decoder).range = UINT32_MAX; \
	(range_decoder).code = 0; \
	(range_decoder).init_bytes_left = 5; \
} while (0)


/// When decoding has been properly finished, rc.code is always zero unless
/// the input stream is corrupt. So checking this can catch some corrupt
/// files especially if they don't have any other integrity check.
#define rc_is_finished(range_decoder) \
	((range_decoder).code == 0)


// Read the next input byte if needed.
#define rc_normalize() \
do { \
	if (rc.range < RC_TOP_VALUE) { \
		rc.range <<= RC_SHIFT_BITS; \
		rc.code = (rc.code << RC_SHIFT_BITS) | *rc_in_ptr++; \
	} \
} while (0)


/// If more input is needed but there is
/// no more input available, "goto out" is used to jump out of the main
/// decoder loop. The "_safe" macros are used in the Resumable decoder
/// mode in order to save the sequence to continue decoding from that
/// point later.
#define rc_normalize_safe(seq) \
do { \
	if (rc.range < RC_TOP_VALUE) { \
		if (rc_in_ptr == rc_in_end) { \
			coder->sequence = seq; \
			goto out; \
		} \
		rc.range <<= RC_SHIFT_BITS; \
		rc.code = (rc.code << RC_SHIFT_BITS) | *rc_in_ptr++; \
	} \
} while (0)


/// Start decoding a bit. This must be used together with rc_update_0()
/// and rc_update_1():
///
///     rc_if_0(prob) {
///         rc_update_0(prob);
///         // Do something
///     } else {
///         rc_update_1(prob);
///         // Do something else
///     }
///
#define rc_if_0(prob) \
	rc_normalize(); \
	rc_bound = (rc.range >> RC_BIT_MODEL_TOTAL_BITS) * (prob); \
	if (rc.code < rc_bound)


#define rc_if_0_safe(prob, seq) \
	rc_normalize_safe(seq); \
	rc_bound = (rc.range >> RC_BIT_MODEL_TOTAL_BITS) * (prob); \
	if (rc.code < rc_bound)


/// Update the range decoder state and the used probability variable to
/// match a decoded bit of 0.
///
/// The x86-64 assembly uses the commented method but it seems that,
/// at least on x86-64, the first version is slightly faster as C code.
#define rc_update_0(prob) \
do { \
	rc.range = rc_bound; \
	prob += (RC_BIT_MODEL_TOTAL - (prob)) >> RC_MOVE_BITS; \
	/* prob -= ((prob) + RC_BIT_MODEL_OFFSET) >> RC_MOVE_BITS; */ \
} while (0)


/// Update the range decoder state and the used probability variable to
/// match a decoded bit of 1.
#define rc_update_1(prob) \
do { \
	rc.range -= rc_bound; \
	rc.code -= rc_bound; \
	prob -= (prob) >> RC_MOVE_BITS; \
} while (0)


/// Decodes one bit and runs action0 or action1 depending on the decoded bit.
/// This macro is used as the last step in bittree reverse decoders since
/// those don't use "symbol" for anything else than indexing the probability
/// arrays.
#define rc_bit_last(prob, action0, action1) \
do { \
	rc_if_0(prob) { \
		rc_update_0(prob); \
		action0; \
	} else { \
		rc_update_1(prob); \
		action1; \
	} \
} while (0)


#define rc_bit_last_safe(prob, action0, action1, seq) \
do { \
	rc_if_0_safe(prob, seq) { \
		rc_update_0(prob); \
		action0; \
	} else { \
		rc_update_1(prob); \
		action1; \
	} \
} while (0)


/// Decodes one bit, updates "symbol", and runs action0 or action1 depending
/// on the decoded bit.
#define rc_bit(prob, action0, action1) \
	rc_bit_last(prob, \
		symbol <<= 1; action0, \
		symbol = (symbol << 1) + 1; action1)


#define rc_bit_safe(prob, action0, action1, seq) \
	rc_bit_last_safe(prob, \
		symbol <<= 1; action0, \
		symbol = (symbol << 1) + 1; action1, \
		seq)

// Unroll fixed-sized bittree decoding.
//
// A compile-time constant in final_add can be used to get rid of the high bit
// from symbol that is used for the array indexing (1U << bittree_bits).
// final_add may also be used to add offset to the result (LZMA length
// decoder does that).
//
// The reason to have final_add here is that in the asm code the addition
// can be done for free: in x86-64 there is SBB instruction with -1 as
// the immediate value, and final_add is combined with that value.
#define rc_bittree_bit(prob) \
	rc_bit(prob, , )

#define rc_bittree3(probs, final_add) \
do { \
	symbol = 1; \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	symbol += (uint32_t)(final_add); \
} while (0)

#define rc_bittree6(probs, final_add) \
do { \
	symbol = 1; \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	symbol += (uint32_t)(final_add); \
} while (0)

#define rc_bittree8(probs, final_add) \
do { \
	symbol = 1; \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	rc_bittree_bit(probs[symbol]); \
	symbol += (uint32_t)(final_add); \
} while (0)


// Fixed-sized reverse bittree
#define rc_bittree_rev4(probs) \
do { \
	symbol = 0; \
	rc_bit_last(probs[symbol + 1], , symbol += 1); \
	rc_bit_last(probs[symbol + 2], , symbol += 2); \
	rc_bit_last(probs[symbol + 4], , symbol += 4); \
	rc_bit_last(probs[symbol + 8], , symbol += 8); \
} while (0)


// Decode one bit from variable-sized reverse bittree. The loop is done
// in the code that uses this macro. This could be changed if the assembly
// version benefited from having the loop done in assembly but it didn't
// seem so in early 2024.
//
// Also, if the loop was done here, the loop counter would likely be local
// to the macro so that it wouldn't modify yet another input variable.
// If a _safe version of a macro with a loop was done then a modifiable
// input variable couldn't be avoided though.
#define rc_bit_add_if_1(probs, dest, value_to_add_if_1) \
	rc_bit(probs[symbol], \
		, \
		dest += value_to_add_if_1)


// Matched literal
#define decode_with_match_bit \
		t_match_byte <<= 1; \
		t_match_bit = t_match_byte & t_offset; \
		t_subcoder_index = t_offset + t_match_bit + symbol; \
		rc_bit(probs[t_subcoder_index], \
				t_offset &= ~t_match_bit, \
				t_offset &= t_match_bit)

#define rc_matched_literal(probs_base_var, match_byte) \
do { \
	uint32_t t_match_byte = (match_byte); \
	uint32_t t_match_bit; \
	uint32_t t_subcoder_index; \
	uint32_t t_offset = 0x100; \
	symbol = 1; \
	decode_with_match_bit; \
	decode_with_match_bit; \
	decode_with_match_bit; \
	decode_with_match_bit; \
	decode_with_match_bit; \
	decode_with_match_bit; \
	decode_with_match_bit; \
	decode_with_match_bit; \
} while (0)


/// Decode a bit without using a probability.
//
// NOTE: GCC 13 and Clang/LLVM 16 can, at least on x86-64, optimize the bound
// calculation to use an arithmetic right shift so there's no need to provide
// the alternative code which, according to C99/C11/C23 6.3.1.3-p3 isn't
// perfectly portable: rc_bound = (uint32_t)((int32_t)rc.code >> 31);
#define rc_direct(dest, count_var) \
do { \
	dest = (dest << 1) + 1; \
	rc_normalize(); \
	rc.range >>= 1; \
	rc.code -= rc.range; \
	rc_bound = UINT32_C(0) - (rc.code >> 31); \
	dest += rc_bound; \
	rc.code += rc.range & rc_bound; \
} while (--count_var > 0)



#define rc_direct_safe(dest, count_var, seq) \
do { \
	rc_normalize_safe(seq); \
	rc.range >>= 1; \
	rc.code -= rc.range; \
	rc_bound = UINT32_C(0) - (rc.code >> 31); \
	rc.code += rc.range & rc_bound; \
	dest = (dest << 1) + (rc_bound + 1); \
} while (--count_var > 0)


//////////////////
// Branchless C //
//////////////////

/// Decode a bit using a branchless method. This reduces the number of
/// mispredicted branches and thus can improve speed.
#define rc_c_bit(prob, action_bit, action_neg) \
do { \
	probability *p = &(prob); \
	rc_normalize(); \
	rc_bound = (rc.range >> RC_BIT_MODEL_TOTAL_BITS) * *p; \
	uint32_t rc_mask = rc.code >= rc_bound; /* rc_mask = decoded bit */ \
	action_bit; /* action when rc_mask is 0 or 1 */ \
	/* rc_mask becomes 0 if bit is 0 and 0xFFFFFFFF if bit is 1: */ \
	rc_mask = 0U - rc_mask; \
	rc.range &= rc_mask; /* If bit 0: set rc.range = 0 */ \
	rc_bound ^= rc_mask; \
	rc_bound -= rc_mask; /* If bit 1: rc_bound = 0U - rc_bound */ \
	rc.range += rc_bound; \
	rc_bound &= rc_mask; \
	rc.code += rc_bound; \
	action_neg; /* action when rc_mask is 0 or 0xFFFFFFFF */ \
	rc_mask = ~rc_mask; /* If bit 0: all bits are set in rc_mask */ \
	rc_mask &= RC_BIT_MODEL_OFFSET; \
	*p -= (*p + rc_mask) >> RC_MOVE_BITS; \
} while (0)


// Testing on x86-64 give an impression that only the normal bittrees and
// the fixed-sized reverse bittrees are worth the branchless C code.
// It should be tested on other archs for which there isn't assembly code
// in this file.

// Using addition in "(symbol << 1) + rc_mask" allows use of x86 LEA
// or RISC-V SH1ADD instructions. Compilers might infer it from
// "(symbol << 1) | rc_mask" too if they see that mask is 0 or 1 but
// the use of addition doesn't require such analysis from compilers.
#if LZMA_RANGE_DECODER_CONFIG & 0x01
#undef rc_bittree_bit
#define rc_bittree_bit(prob) \
	rc_c_bit(prob, \
		symbol = (symbol << 1) + rc_mask, \
		)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x01

#if LZMA_RANGE_DECODER_CONFIG & 0x02
#undef rc_bittree_rev4
#define rc_bittree_rev4(probs) \
do { \
	symbol = 0; \
	rc_c_bit(probs[symbol + 1], symbol += rc_mask, ); \
	rc_c_bit(probs[symbol + 2], symbol += rc_mask << 1, ); \
	rc_c_bit(probs[symbol + 4], symbol += rc_mask << 2, ); \
	rc_c_bit(probs[symbol + 8], symbol += rc_mask << 3, ); \
} while (0)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x02

#if LZMA_RANGE_DECODER_CONFIG & 0x04
#undef rc_bit_add_if_1
#define rc_bit_add_if_1(probs, dest, value_to_add_if_1) \
	rc_c_bit(probs[symbol], \
		symbol = (symbol << 1) + rc_mask, \
		dest += (value_to_add_if_1) & rc_mask)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x04


#if LZMA_RANGE_DECODER_CONFIG & 0x08
#undef decode_with_match_bit
#define decode_with_match_bit \
		t_match_byte <<= 1; \
		t_match_bit = t_match_byte & t_offset; \
		t_subcoder_index = t_offset + t_match_bit + symbol; \
		rc_c_bit(probs[t_subcoder_index], \
			symbol = (symbol << 1) + rc_mask, \
			t_offset &= ~t_match_bit ^ rc_mask)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x08


////////////
// x86-64 //
////////////

#if LZMA_RANGE_DECODER_CONFIG & 0x1F0

// rc_asm_y and rc_asm_n are used as arguments to macros to control which
// strings to include or omit.
#define rc_asm_y(str) str
#define rc_asm_n(str)

// There are a few possible variations for normalization.
// This is the smallest variant which is also used by LZMA SDK.
//
//   - This has partial register write (the MOV from (%[in_ptr])).
//
//   - INC saves one byte in code size over ADD. False dependency on
//     partial flags from INC shouldn't become a problem on any processor
//     because the instructions after normalization don't read the flags
//     until SUB which sets all flags.
//
#define rc_asm_normalize \
	"cmp	%[top_value], %[range]\n\t" \
	"jae	1f\n\t" \
	"shl	%[shift_bits], %[code]\n\t" \
	"mov	(%[in_ptr]), %b[code]\n\t" \
	"shl	%[shift_bits], %[range]\n\t" \
	"inc	%[in_ptr]\n" \
	"1:\n"

// rc_asm_calc(prob) is roughly equivalent to the C version of rc_if_0(prob)...
//
//     rc_bound = (rc.range >> RC_BIT_MODEL_TOTAL_BITS) * (prob);
//     if (rc.code < rc_bound)
//
// ...but the bound is stored in "range":
//
//     t0 = range;
//     range = (range >> RC_BIT_MODEL_TOTAL_BITS) * (prob);
//     t0 -= range;
//     t1 = code;
//     code -= range;
//
// The carry flag (CF) from the last subtraction holds the negation of
// the decoded bit (if CF==0 then the decoded bit is 1).
// The values in t0 and t1 are needed for rc_update_0(prob) and
// rc_update_1(prob). If the bit is 0, rc_update_0(prob)...
//
//     rc.range = rc_bound;
//
// ...has already been done but the "code -= range" has to be reverted using
// the old value stored in t1. (Also, prob needs to be updated.)
//
// If the bit is 1, rc_update_1(prob)...
//
//     rc.range -= rc_bound;
//     rc.code -= rc_bound;
//
// ...is already done for "code" but the value for "range" needs to be taken
// from t0. (Also, prob needs to be updated here as well.)
//
// The assignments from t0 and t1 can be done in a branchless manner with CMOV
// after the instructions from this macro. The CF from SUB tells which moves
// are needed.
#define rc_asm_calc(prob) \
		"mov	%[range], %[t0]\n\t" \
		"shr	%[bit_model_total_bits], %[range]\n\t" \
		"imul	%[" prob "], %[range]\n\t" \
		"sub	%[range], %[t0]\n\t" \
		"mov	%[code], %[t1]\n\t" \
		"sub	%[range], %[code]\n\t"

// Also, prob needs to be updated: The update math depends on the decoded bit.
// It can be expressed in a few slightly different ways but this is fairly
// convenient here:
//
//     prob -= (prob + (bit ? 0 : RC_BIT_MODEL_OFFSET)) >> RC_MOVE_BITS;
//
// To do it in branchless way when the negation of the decoded bit is in CF,
// both "prob" and "prob + RC_BIT_MODEL_OFFSET" are needed. Then the desired
// value can be picked with CMOV. The addition can be done using LEA without
// affecting CF.
//
// (This prob update method is a tiny bit different from LZMA SDK 23.01.
// In the LZMA SDK a single register is reserved solely for a constant to
// be used with CMOV when updating prob. That is fine since there are enough
// free registers to do so. The method used here uses one fewer register,
// which is valuable with inline assembly.)
//
// * * *
//
// In bittree decoding, each (unrolled) loop iteration decodes one bit
// and needs one prob variable. To make it faster, the prob variable of
// the iteration N+1 is loaded during iteration N. There are two possible
// prob variables to choose from for N+1. Both are loaded from memory and
// the correct one is chosen with CMOV using the same CF as is used for
// other things described above.
//
// This preloading/prefetching requires an extra register. To avoid
// useless moves from "preloaded prob register" to "current prob register",
// the macros swap between the two registers for odd and even iterations.
//
// * * *
//
// Finally, the decoded bit has to be stored in "symbol". Since the negation
// of the bit is in CF, this can be done with SBB: symbol -= CF - 1. That is,
// if the decoded bit is 0 (CF==1) the operation is a no-op "symbol -= 0"
// and when bit is 1 (CF==0) the operation is "symbol -= 0 - 1" which is
// the same as "symbol += 1".
//
// The instructions for all things are intertwined for a few reasons:
//   - freeing temporary registers for new use
//   - not modifying CF too early
//   - instruction scheduling
//
// The first and last iterations can cheat a little. For example,
// on the first iteration "symbol" is known to start from 1 so it
// doesn't need to be read; it can even be immediately initialized
// to 2 to prepare for the second iteration of the loop.
//
// * * *
//
// a = number of the current prob variable (0 or 1)
// b = number of the next prob variable (1 or 0)
// *_only = rc_asm_y or _n to include or exclude code marked with them
#define rc_asm_bittree(a, b, first_only, middle_only, last_only) \
	first_only( \
		"movzwl	2(%[probs_base]), %[prob" #a "]\n\t" \
		"mov	$2, %[symbol]\n\t" \
		"movzwl	4(%[probs_base]), %[prob" #b "]\n\t" \
	) \
	middle_only( \
		/* Note the scaling of 4 instead of 2: */ \
		"movzwl	(%[probs_base], %q[symbol], 4), %[prob" #b "]\n\t" \
	) \
	last_only( \
		"add	%[symbol], %[symbol]\n\t" \
	) \
		\
		rc_asm_normalize \
		rc_asm_calc("prob" #a) \
		\
		"cmovae	%[t0], %[range]\n\t" \
		\
	first_only( \
		"movzwl	6(%[probs_base]), %[t0]\n\t" \
		"cmovae	%[t0], %[prob" #b "]\n\t" \
	) \
	middle_only( \
		"movzwl	2(%[probs_base], %q[symbol], 4), %[t0]\n\t" \
		"lea	(%q[symbol], %q[symbol]), %[symbol]\n\t" \
		"cmovae	%[t0], %[prob" #b "]\n\t" \
	) \
		\
		"lea	%c[bit_model_offset](%q[prob" #a "]), %[t0]\n\t" \
		"cmovb	%[t1], %[code]\n\t" \
		"mov	%[symbol], %[t1]\n\t" \
		"cmovae	%[prob" #a "], %[t0]\n\t" \
		\
	first_only( \
		"sbb	$-1, %[symbol]\n\t" \
	) \
	middle_only( \
		"sbb	$-1, %[symbol]\n\t" \
	) \
	last_only( \
		"sbb	%[last_sbb], %[symbol]\n\t" \
	) \
		\
		"shr	%[move_bits], %[t0]\n\t" \
		"sub	%[t0], %[prob" #a "]\n\t" \
		/* Scaling of 1 instead of 2 because symbol <<= 1. */ \
		"mov	%w[prob" #a "], (%[probs_base], %q[t1], 1)\n\t"

// NOTE: The order of variables in __asm__ can affect speed and code size.
#define rc_asm_bittree_n(probs_base_var, final_add, asm_str) \
do { \
	uint32_t t0; \
	uint32_t t1; \
	uint32_t t_prob0; \
	uint32_t t_prob1; \
	\
	__asm__( \
		asm_str \
		: \
		[range]     "+&r"(rc.range), \
		[code]      "+&r"(rc.code), \
		[t0]        "=&r"(t0), \
		[t1]        "=&r"(t1), \
		[prob0]     "=&r"(t_prob0), \
		[prob1]     "=&r"(t_prob1), \
		[symbol]    "=&r"(symbol), \
		[in_ptr]    "+&r"(rc_in_ptr) \
		: \
		[probs_base]           "r"(probs_base_var), \
		[last_sbb]             "n"(-1 - (final_add)), \
		[top_value]            "n"(RC_TOP_VALUE), \
		[shift_bits]           "n"(RC_SHIFT_BITS), \
		[bit_model_total_bits] "n"(RC_BIT_MODEL_TOTAL_BITS), \
		[bit_model_offset]     "n"(RC_BIT_MODEL_OFFSET), \
		[move_bits]            "n"(RC_MOVE_BITS) \
		: \
		"cc", "memory"); \
} while (0)


#if LZMA_RANGE_DECODER_CONFIG & 0x010
#undef rc_bittree3
#define rc_bittree3(probs_base_var, final_add) \
	rc_asm_bittree_n(probs_base_var, final_add, \
		rc_asm_bittree(0, 1, rc_asm_y, rc_asm_n, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(0, 1, rc_asm_n, rc_asm_n, rc_asm_y) \
	)

#undef rc_bittree6
#define rc_bittree6(probs_base_var, final_add) \
	rc_asm_bittree_n(probs_base_var, final_add, \
		rc_asm_bittree(0, 1, rc_asm_y, rc_asm_n, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(0, 1, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(0, 1, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_n, rc_asm_y) \
	)

#undef rc_bittree8
#define rc_bittree8(probs_base_var, final_add) \
	rc_asm_bittree_n(probs_base_var, final_add, \
		rc_asm_bittree(0, 1, rc_asm_y, rc_asm_n, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(0, 1, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(0, 1, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(0, 1, rc_asm_n, rc_asm_y, rc_asm_n) \
		rc_asm_bittree(1, 0, rc_asm_n, rc_asm_n, rc_asm_y) \
	)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x010


// Fixed-sized reverse bittree
//
// This uses the indexing that constructs the final value in symbol directly.
// add    = 1,  2,   4,  8
// dcur   = -,  4,   8, 16
// dnext0 = 4,   8, 16,  -
// dnext0 = 6,  12, 24,  -
#define rc_asm_bittree_rev(a, b, add, dcur, dnext0, dnext1, \
		first_only, middle_only, last_only) \
	first_only( \
		"movzwl	2(%[probs_base]), %[prob" #a "]\n\t" \
		"xor	%[symbol], %[symbol]\n\t" \
		"movzwl	4(%[probs_base]), %[prob" #b "]\n\t" \
	) \
	middle_only( \
		"movzwl	" #dnext0 "(%[probs_base], %q[symbol], 2), " \
			"%[prob" #b "]\n\t" \
	) \
		\
		rc_asm_normalize \
		rc_asm_calc("prob" #a) \
		\
		"cmovae	%[t0], %[range]\n\t" \
		\
	first_only( \
		"movzwl	6(%[probs_base]), %[t0]\n\t" \
		"cmovae	%[t0], %[prob" #b "]\n\t" \
	) \
	middle_only( \
		"movzwl	" #dnext1 "(%[probs_base], %q[symbol], 2), %[t0]\n\t" \
		"cmovae	%[t0], %[prob" #b "]\n\t" \
	) \
		\
		"lea	" #add "(%q[symbol]), %[t0]\n\t" \
		"cmovb	%[t1], %[code]\n\t" \
	middle_only( \
		"mov	%[symbol], %[t1]\n\t" \
	) \
	last_only( \
		"mov	%[symbol], %[t1]\n\t" \
	) \
		"cmovae	%[t0], %[symbol]\n\t" \
		"lea	%c[bit_model_offset](%q[prob" #a "]), %[t0]\n\t" \
		"cmovae	%[prob" #a "], %[t0]\n\t" \
		\
		"shr	%[move_bits], %[t0]\n\t" \
		"sub	%[t0], %[prob" #a "]\n\t" \
	first_only( \
		"mov	%w[prob" #a "], 2(%[probs_base])\n\t" \
	) \
	middle_only( \
		"mov	%w[prob" #a "], " \
			#dcur "(%[probs_base], %q[t1], 2)\n\t" \
	) \
	last_only( \
		"mov	%w[prob" #a "], " \
			#dcur "(%[probs_base], %q[t1], 2)\n\t" \
	)

#if LZMA_RANGE_DECODER_CONFIG & 0x020
#undef rc_bittree_rev4
#define rc_bittree_rev4(probs_base_var) \
rc_asm_bittree_n(probs_base_var, 4, \
	rc_asm_bittree_rev(0, 1, 1,  -,  4,  6, rc_asm_y, rc_asm_n, rc_asm_n) \
	rc_asm_bittree_rev(1, 0, 2,  4,  8, 12, rc_asm_n, rc_asm_y, rc_asm_n) \
	rc_asm_bittree_rev(0, 1, 4,  8, 16, 24, rc_asm_n, rc_asm_y, rc_asm_n) \
	rc_asm_bittree_rev(1, 0, 8, 16,  -,  -, rc_asm_n, rc_asm_n, rc_asm_y) \
)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x020


#if LZMA_RANGE_DECODER_CONFIG & 0x040
#undef rc_bit_add_if_1
#define rc_bit_add_if_1(probs_base_var, dest_var, value_to_add_if_1) \
do { \
	uint32_t t0; \
	uint32_t t1; \
	uint32_t t2 = (value_to_add_if_1); \
	uint32_t t_prob; \
	uint32_t t_index; \
	\
	__asm__( \
		"movzwl	(%[probs_base], %q[symbol], 2), %[prob]\n\t" \
		"mov	%[symbol], %[index]\n\t" \
		\
		"add	%[dest], %[t2]\n\t" \
		"add	%[symbol], %[symbol]\n\t" \
		\
		rc_asm_normalize \
		rc_asm_calc("prob") \
		\
		"cmovae	%[t0], %[range]\n\t" \
		"lea	%c[bit_model_offset](%q[prob]), %[t0]\n\t" \
		"cmovb	%[t1], %[code]\n\t" \
		"cmovae	%[prob], %[t0]\n\t" \
		\
		"cmovae	%[t2], %[dest]\n\t" \
		"sbb	$-1, %[symbol]\n\t" \
		\
		"sar	%[move_bits], %[t0]\n\t" \
		"sub	%[t0], %[prob]\n\t" \
		"mov	%w[prob], (%[probs_base], %q[index], 2)" \
		: \
		[range]     "+&r"(rc.range), \
		[code]      "+&r"(rc.code), \
		[t0]        "=&r"(t0), \
		[t1]        "=&r"(t1), \
		[prob]      "=&r"(t_prob), \
		[index]     "=&r"(t_index), \
		[symbol]    "+&r"(symbol), \
		[t2]        "+&r"(t2), \
		[dest]      "+&r"(dest_var), \
		[in_ptr]    "+&r"(rc_in_ptr) \
		: \
		[probs_base]           "r"(probs_base_var), \
		[top_value]            "n"(RC_TOP_VALUE), \
		[shift_bits]           "n"(RC_SHIFT_BITS), \
		[bit_model_total_bits] "n"(RC_BIT_MODEL_TOTAL_BITS), \
		[bit_model_offset]     "n"(RC_BIT_MODEL_OFFSET), \
		[move_bits]            "n"(RC_MOVE_BITS) \
		: \
		"cc", "memory"); \
} while (0)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x040


// Literal decoding uses a normal 8-bit bittree but literal with match byte
// is more complex in picking the probability variable from the correct
// subtree. This doesn't use preloading/prefetching of the next prob because
// there are four choices instead of two.
//
// FIXME? The first iteration starts with symbol = 1 so it could be optimized
// by a tiny amount.
#define rc_asm_matched_literal(nonlast_only) \
		"add	%[offset], %[symbol]\n\t" \
		"and	%[offset], %[match_bit]\n\t" \
		"add	%[match_bit], %[symbol]\n\t" \
		\
		"movzwl	(%[probs_base], %q[symbol], 2), %[prob]\n\t" \
		\
		"add	%[symbol], %[symbol]\n\t" \
		\
	nonlast_only( \
		"xor	%[match_bit], %[offset]\n\t" \
		"add	%[match_byte], %[match_byte]\n\t" \
	) \
		\
		rc_asm_normalize \
		rc_asm_calc("prob") \
		\
		"cmovae	%[t0], %[range]\n\t" \
		"lea	%c[bit_model_offset](%q[prob]), %[t0]\n\t" \
		"cmovb	%[t1], %[code]\n\t" \
		"mov	%[symbol], %[t1]\n\t" \
		"cmovae	%[prob], %[t0]\n\t" \
		\
	nonlast_only( \
		"cmovae	%[match_bit], %[offset]\n\t" \
		"mov	%[match_byte], %[match_bit]\n\t" \
	) \
		\
		"sbb	$-1, %[symbol]\n\t" \
		\
		"shr	%[move_bits], %[t0]\n\t" \
		/* Undo symbol += match_bit + offset: */ \
		"and	$0x1FF, %[symbol]\n\t" \
		"sub	%[t0], %[prob]\n\t" \
		\
		/* Scaling of 1 instead of 2 because symbol <<= 1. */ \
		"mov	%w[prob], (%[probs_base], %q[t1], 1)\n\t"


#if LZMA_RANGE_DECODER_CONFIG & 0x080
#undef rc_matched_literal
#define rc_matched_literal(probs_base_var, match_byte_value) \
do { \
	uint32_t t0; \
	uint32_t t1; \
	uint32_t t_prob; \
	uint32_t t_match_byte = (uint32_t)(match_byte_value) << 1; \
	uint32_t t_match_bit = t_match_byte; \
	uint32_t t_offset = 0x100; \
	symbol = 1; \
	\
	__asm__( \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_y) \
		rc_asm_matched_literal(rc_asm_n) \
		: \
		[range]       "+&r"(rc.range), \
		[code]        "+&r"(rc.code), \
		[t0]          "=&r"(t0), \
		[t1]          "=&r"(t1), \
		[prob]        "=&r"(t_prob), \
		[match_bit]   "+&r"(t_match_bit), \
		[symbol]      "+&r"(symbol), \
		[match_byte]  "+&r"(t_match_byte), \
		[offset]      "+&r"(t_offset), \
		[in_ptr]      "+&r"(rc_in_ptr) \
		: \
		[probs_base]           "r"(probs_base_var), \
		[top_value]            "n"(RC_TOP_VALUE), \
		[shift_bits]           "n"(RC_SHIFT_BITS), \
		[bit_model_total_bits] "n"(RC_BIT_MODEL_TOTAL_BITS), \
		[bit_model_offset]     "n"(RC_BIT_MODEL_OFFSET), \
		[move_bits]            "n"(RC_MOVE_BITS) \
		: \
		"cc", "memory"); \
} while (0)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x080


// Doing the loop in asm instead of C seems to help a little.
#if LZMA_RANGE_DECODER_CONFIG & 0x100
#undef rc_direct
#define rc_direct(dest_var, count_var) \
do { \
	uint32_t t0; \
	uint32_t t1; \
	\
	__asm__( \
		"2:\n\t" \
		"add	%[dest], %[dest]\n\t" \
		"lea	1(%q[dest]), %[t1]\n\t" \
		\
		rc_asm_normalize \
		\
		"shr	$1, %[range]\n\t" \
		"mov	%[code], %[t0]\n\t" \
		"sub	%[range], %[code]\n\t" \
		"cmovns	%[t1], %[dest]\n\t" \
		"cmovs	%[t0], %[code]\n\t" \
		"dec	%[count]\n\t" \
		"jnz	2b\n\t" \
		: \
		[range]       "+&r"(rc.range), \
		[code]        "+&r"(rc.code), \
		[t0]          "=&r"(t0), \
		[t1]          "=&r"(t1), \
		[dest]        "+&r"(dest_var), \
		[count]       "+&r"(count_var), \
		[in_ptr]      "+&r"(rc_in_ptr) \
		: \
		[top_value]   "n"(RC_TOP_VALUE), \
		[shift_bits]  "n"(RC_SHIFT_BITS) \
		: \
		"cc", "memory"); \
} while (0)
#endif // LZMA_RANGE_DECODER_CONFIG & 0x100

#endif // x86_64

#endif
