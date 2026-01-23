// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       lzma_decoder.c
/// \brief      LZMA decoder
///
//  Authors:    Igor Pavlov
//              Lasse Collin
//              Jia Tan
//
///////////////////////////////////////////////////////////////////////////////

#include "lz_decoder.h"
#include "lzma_common.h"
#include "lzma_decoder.h"
#include "range_decoder.h"

// The macros unroll loops with switch statements.
// Silence warnings about missing fall-through comments.
#if TUKLIB_GNUC_REQ(7, 0) || defined(__clang__)
#	pragma GCC diagnostic ignored "-Wimplicit-fallthrough"
#endif

// Minimum number of input bytes to safely decode one LZMA symbol.
// The worst case is that we decode 22 bits using probabilities and 26
// direct bits. This may decode at maximum 20 bytes of input.
#define LZMA_IN_REQUIRED 20


// Macros for (somewhat) size-optimized code.
// This is used to decode the match length (how many bytes must be repeated
// from the dictionary). This version is used in the Resumable mode and
// does not unroll any loops.
#define len_decode(target, ld, pos_state, seq) \
do { \
case seq ## _CHOICE: \
	rc_if_0_safe(ld.choice, seq ## _CHOICE) { \
		rc_update_0(ld.choice); \
		probs = ld.low[pos_state];\
		limit = LEN_LOW_SYMBOLS; \
		target = MATCH_LEN_MIN; \
	} else { \
		rc_update_1(ld.choice); \
case seq ## _CHOICE2: \
		rc_if_0_safe(ld.choice2, seq ## _CHOICE2) { \
			rc_update_0(ld.choice2); \
			probs = ld.mid[pos_state]; \
			limit = LEN_MID_SYMBOLS; \
			target = MATCH_LEN_MIN + LEN_LOW_SYMBOLS; \
		} else { \
			rc_update_1(ld.choice2); \
			probs = ld.high; \
			limit = LEN_HIGH_SYMBOLS; \
			target = MATCH_LEN_MIN + LEN_LOW_SYMBOLS \
					+ LEN_MID_SYMBOLS; \
		} \
	} \
	symbol = 1; \
case seq ## _BITTREE: \
	do { \
		rc_bit_safe(probs[symbol], , , seq ## _BITTREE); \
	} while (symbol < limit); \
	target += symbol - limit; \
} while (0)


// This is the faster version of the match length decoder that does not
// worry about being resumable. It unrolls the bittree decoding loop.
#define len_decode_fast(target, ld, pos_state) \
do { \
	symbol = 1; \
	rc_if_0(ld.choice) { \
		rc_update_0(ld.choice); \
		rc_bittree3(ld.low[pos_state], \
				-LEN_LOW_SYMBOLS + MATCH_LEN_MIN); \
		target = symbol; \
	} else { \
		rc_update_1(ld.choice); \
		rc_if_0(ld.choice2) { \
			rc_update_0(ld.choice2); \
			rc_bittree3(ld.mid[pos_state], -LEN_MID_SYMBOLS \
					+ MATCH_LEN_MIN + LEN_LOW_SYMBOLS); \
			target = symbol; \
		} else { \
			rc_update_1(ld.choice2); \
			rc_bittree8(ld.high, -LEN_HIGH_SYMBOLS \
					+ MATCH_LEN_MIN \
					+ LEN_LOW_SYMBOLS + LEN_MID_SYMBOLS); \
			target = symbol; \
		} \
	} \
} while (0)


/// Length decoder probabilities; see comments in lzma_common.h.
typedef struct {
	probability choice;
	probability choice2;
	probability low[POS_STATES_MAX][LEN_LOW_SYMBOLS];
	probability mid[POS_STATES_MAX][LEN_MID_SYMBOLS];
	probability high[LEN_HIGH_SYMBOLS];
} lzma_length_decoder;


typedef struct {
	///////////////////
	// Probabilities //
	///////////////////

	/// Literals; see comments in lzma_common.h.
	probability literal[LITERAL_CODERS_MAX * LITERAL_CODER_SIZE];

	/// If 1, it's a match. Otherwise it's a single 8-bit literal.
	probability is_match[STATES][POS_STATES_MAX];

	/// If 1, it's a repeated match. The distance is one of rep0 .. rep3.
	probability is_rep[STATES];

	/// If 0, distance of a repeated match is rep0.
	/// Otherwise check is_rep1.
	probability is_rep0[STATES];

	/// If 0, distance of a repeated match is rep1.
	/// Otherwise check is_rep2.
	probability is_rep1[STATES];

	/// If 0, distance of a repeated match is rep2. Otherwise it is rep3.
	probability is_rep2[STATES];

	/// If 1, the repeated match has length of one byte. Otherwise
	/// the length is decoded from rep_len_decoder.
	probability is_rep0_long[STATES][POS_STATES_MAX];

	/// Probability tree for the highest two bits of the match distance.
	/// There is a separate probability tree for match lengths of
	/// 2 (i.e. MATCH_LEN_MIN), 3, 4, and [5, 273].
	probability dist_slot[DIST_STATES][DIST_SLOTS];

	/// Probability trees for additional bits for match distance when the
	/// distance is in the range [4, 127].
	probability pos_special[FULL_DISTANCES - DIST_MODEL_END];

	/// Probability tree for the lowest four bits of a match distance
	/// that is equal to or greater than 128.
	probability pos_align[ALIGN_SIZE];

	/// Length of a normal match
	lzma_length_decoder match_len_decoder;

	/// Length of a repeated match
	lzma_length_decoder rep_len_decoder;

	///////////////////
	// Decoder state //
	///////////////////

	// Range coder
	lzma_range_decoder rc;

	// Types of the most recently seen LZMA symbols
	lzma_lzma_state state;

	uint32_t rep0;      ///< Distance of the latest match
	uint32_t rep1;      ///< Distance of second latest match
	uint32_t rep2;      ///< Distance of third latest match
	uint32_t rep3;      ///< Distance of fourth latest match

	uint32_t pos_mask; // (1U << pb) - 1
	uint32_t literal_context_bits;
	uint32_t literal_mask;

	/// Uncompressed size as bytes, or LZMA_VLI_UNKNOWN if end of
	/// payload marker is expected.
	lzma_vli uncompressed_size;

	/// True if end of payload marker (EOPM) is allowed even when
	/// uncompressed_size is known; false if EOPM must not be present.
	/// This is ignored if uncompressed_size == LZMA_VLI_UNKNOWN.
	bool allow_eopm;

	////////////////////////////////
	// State of incomplete symbol //
	////////////////////////////////

	/// Position where to continue the decoder loop
	enum {
		SEQ_NORMALIZE,
		SEQ_IS_MATCH,
		SEQ_LITERAL,
		SEQ_LITERAL_MATCHED,
		SEQ_LITERAL_WRITE,
		SEQ_IS_REP,
		SEQ_MATCH_LEN_CHOICE,
		SEQ_MATCH_LEN_CHOICE2,
		SEQ_MATCH_LEN_BITTREE,
		SEQ_DIST_SLOT,
		SEQ_DIST_MODEL,
		SEQ_DIRECT,
		SEQ_ALIGN,
		SEQ_EOPM,
		SEQ_IS_REP0,
		SEQ_SHORTREP,
		SEQ_IS_REP0_LONG,
		SEQ_IS_REP1,
		SEQ_IS_REP2,
		SEQ_REP_LEN_CHOICE,
		SEQ_REP_LEN_CHOICE2,
		SEQ_REP_LEN_BITTREE,
		SEQ_COPY,
	} sequence;

	/// Base of the current probability tree
	probability *probs;

	/// Symbol being decoded. This is also used as an index variable in
	/// bittree decoders: probs[symbol]
	uint32_t symbol;

	/// Used as a loop termination condition on bittree decoders and
	/// direct bits decoder.
	uint32_t limit;

	/// Matched literal decoder: 0x100 or 0 to help avoiding branches.
	/// Bittree reverse decoders: Offset of the next bit: 1 << offset
	uint32_t offset;

	/// If decoding a literal: match byte.
	/// If decoding a match: length of the match.
	uint32_t len;
} lzma_lzma1_decoder;


static lzma_ret
lzma_decode(void *coder_ptr, lzma_dict *restrict dictptr,
		const uint8_t *restrict in,
		size_t *restrict in_pos, size_t in_size)
{
	lzma_lzma1_decoder *restrict coder = coder_ptr;

	////////////////////
	// Initialization //
	////////////////////

	{
		const lzma_ret ret = rc_read_init(
				&coder->rc, in, in_pos, in_size);
		if (ret != LZMA_STREAM_END)
			return ret;
	}

	///////////////
	// Variables //
	///////////////

	// Making local copies of often-used variables improves both
	// speed and readability.

	lzma_dict dict = *dictptr;

	const size_t dict_start = dict.pos;

	// Range decoder
	rc_to_local(coder->rc, *in_pos, LZMA_IN_REQUIRED);

	// State
	uint32_t state = coder->state;
	uint32_t rep0 = coder->rep0;
	uint32_t rep1 = coder->rep1;
	uint32_t rep2 = coder->rep2;
	uint32_t rep3 = coder->rep3;

	const uint32_t pos_mask = coder->pos_mask;

	// These variables are actually needed only if we last time ran
	// out of input in the middle of the decoder loop.
	probability *probs = coder->probs;
	uint32_t symbol = coder->symbol;
	uint32_t limit = coder->limit;
	uint32_t offset = coder->offset;
	uint32_t len = coder->len;

	const uint32_t literal_mask = coder->literal_mask;
	const uint32_t literal_context_bits = coder->literal_context_bits;

	// Temporary variables
	uint32_t pos_state = dict.pos & pos_mask;

	lzma_ret ret = LZMA_OK;

	// This is true when the next LZMA symbol is allowed to be EOPM.
	// That is, if this is false, then EOPM is considered
	// an invalid symbol and we will return LZMA_DATA_ERROR.
	//
	// EOPM is always required (not just allowed) when
	// the uncompressed size isn't known. When uncompressed size
	// is known, eopm_is_valid may be set to true later.
	bool eopm_is_valid = coder->uncompressed_size == LZMA_VLI_UNKNOWN;

	// If uncompressed size is known and there is enough output space
	// to decode all the data, limit the available buffer space so that
	// the main loop won't try to decode past the end of the stream.
	bool might_finish_without_eopm = false;
	if (coder->uncompressed_size != LZMA_VLI_UNKNOWN
			&& coder->uncompressed_size <= dict.limit - dict.pos) {
		dict.limit = dict.pos + (size_t)(coder->uncompressed_size);
		might_finish_without_eopm = true;
	}

	// The main decoder loop. The "switch" is used to resume the decoder at
	// correct location. Once resumed, the "switch" is no longer used.
	// The decoder loops is split into two modes:
	//
	// 1 - Non-resumable mode (fast). This is used when it is guaranteed
	//     there is enough input to decode the next symbol. If the output
	//     limit is reached, then the decoder loop will save the place
	//     for the resumable mode to continue. This mode is not used if
	//     HAVE_SMALL is defined. This is faster than Resumable mode
	//     because it reduces the number of branches needed and allows
	//     for more compiler optimizations.
	//
	// 2 - Resumable mode (slow). This is used when a previous decoder
	//     loop did not have enough space in the input or output buffers
	//     to complete. It uses sequence enum values to set remind
	//     coder->sequence where to resume in the decoder loop. This
	//     is the only mode used when HAVE_SMALL is defined.

	switch (coder->sequence)
	while (true) {
		// Calculate new pos_state. This is skipped on the first loop
		// since we already calculated it when setting up the local
		// variables.
		pos_state = dict.pos & pos_mask;

#ifndef HAVE_SMALL

		///////////////////////////////
		// Non-resumable Mode (fast) //
		///////////////////////////////

		// Go to Resumable mode (1) if there is not enough input to
		// safely decode any possible LZMA symbol or (2) if the
		// dictionary is full, which may need special checks that
		// are only done in the Resumable mode.
		if (unlikely(!rc_is_fast_allowed()
				|| dict.pos == dict.limit))
			goto slow;

		// Decode the first bit from the next LZMA symbol.
		// If the bit is a 0, then we handle it as a literal.
		// If the bit is a 1, then it is a match of previously
		// decoded data.
		rc_if_0(coder->is_match[state][pos_state]) {
			/////////////////////
			// Decode literal. //
			/////////////////////

			// Update the RC that we have decoded a 0.
			rc_update_0(coder->is_match[state][pos_state]);

			// Get the correct probability array from lp and
			// lc params.
			probs = literal_subcoder(coder->literal,
					literal_context_bits, literal_mask,
					dict.pos, dict_get0(&dict));

			if (is_literal_state(state)) {
				update_literal_normal(state);

				// Decode literal without match byte.
				rc_bittree8(probs, 0);
			} else {
				update_literal_matched(state);

				// Decode literal with match byte.
				rc_matched_literal(probs,
						dict_get(&dict, rep0));
			}

			// Write decoded literal to dictionary
			dict_put(&dict, symbol);
			continue;
		}

		///////////////////
		// Decode match. //
		///////////////////

		// Instead of a new byte we are going to decode a
		// distance-length pair. The distance represents how far
		// back in the dictionary to begin copying. The length
		// represents how many bytes to copy.

		rc_update_1(coder->is_match[state][pos_state]);

		rc_if_0(coder->is_rep[state]) {
			///////////////////
			// Simple match. //
			///////////////////

			// Not a repeated match. In this case,
			// the length (how many bytes to copy) must be
			// decoded first. Then, the distance (where to
			// start copying) is decoded.
			//
			// This is also how we know when we are done
			// decoding. If the distance decodes to UINT32_MAX,
			// then we know to stop decoding (end of payload
			// marker).

			rc_update_0(coder->is_rep[state]);
			update_match(state);

			// The latest three match distances are kept in
			// memory in case there are repeated matches.
			rep3 = rep2;
			rep2 = rep1;
			rep1 = rep0;

			// Decode the length of the match.
			len_decode_fast(len, coder->match_len_decoder,
					pos_state);

			// Next, decode the distance into rep0.

			// The next 6 bits determine how to decode the
			// rest of the distance.
			probs = coder->dist_slot[get_dist_state(len)];

			rc_bittree6(probs, -DIST_SLOTS);
			assert(symbol <= 63);

			if (symbol < DIST_MODEL_START) {
				// If the decoded symbol is < DIST_MODEL_START
				// then we use its value directly as the
				// match distance. No other bits are needed.
				// The only possible distance values
				// are [0, 3].
				rep0 = symbol;
			} else {
				// Use the first two bits of symbol as the
				// highest bits of the match distance.

				// "limit" represents the number of low bits
				// to decode.
				limit = (symbol >> 1) - 1;
				assert(limit >= 1 && limit <= 30);
				rep0 = 2 + (symbol & 1);

				if (symbol < DIST_MODEL_END) {
					// When symbol is > DIST_MODEL_START,
					// but symbol < DIST_MODEL_END, then
					// it can decode distances between
					// [4, 127].
					assert(limit <= 5);
					rep0 <<= limit;
					assert(rep0 <= 96);

					// -1 is fine, because we start
					// decoding at probs[1], not probs[0].
					// NOTE: This violates the C standard,
					// since we are doing pointer
					// arithmetic past the beginning of
					// the array.
					assert((int32_t)(rep0 - symbol - 1)
							>= -1);
					assert((int32_t)(rep0 - symbol - 1)
							<= 82);
					probs = coder->pos_special + rep0
							- symbol - 1;
					symbol = 1;
					offset = 1;

					// Variable number (1-5) of bits
					// from a reverse bittree. This
					// isn't worth manual unrolling.
					//
					// NOTE: Making one or many of the
					// variables (probs, symbol, offset,
					// or limit) local here (instead of
					// using those declared outside the
					// main loop) can affect code size
					// and performance which isn't a
					// surprise but it's not so clear
					// what is the best.
					do {
						rc_bit_add_if_1(probs,
								rep0, offset);
						offset <<= 1;
					} while (--limit > 0);
				} else {
					// The distance is >= 128. Decode the
					// lower bits without probabilities
					// except the lowest four bits.
					assert(symbol >= 14);
					assert(limit >= 6);

					limit -= ALIGN_BITS;
					assert(limit >= 2);

					rc_direct(rep0, limit);

					// Decode the lowest four bits using
					// probabilities.
					rep0 <<= ALIGN_BITS;
					rc_bittree_rev4(coder->pos_align);
					rep0 += symbol;

					// If the end of payload marker (EOPM)
					// is detected, jump to the safe code.
					// The EOPM handling isn't speed
					// critical at all.
					//
					// A final normalization is needed
					// after the EOPM (there can be a
					// dummy byte to read in some cases).
					// If the normalization was done here
					// in the fast code, it would need to
					// be taken into account in the value
					// of LZMA_IN_REQUIRED. Using the
					// safe code allows keeping
					// LZMA_IN_REQUIRED as 20 instead of
					// 21.
					if (rep0 == UINT32_MAX)
						goto eopm;
				}
			}

			// Validate the distance we just decoded.
			if (unlikely(!dict_is_distance_valid(&dict, rep0))) {
				ret = LZMA_DATA_ERROR;
				goto out;
			}

		} else {
			rc_update_1(coder->is_rep[state]);

			/////////////////////
			// Repeated match. //
			/////////////////////

			// The match distance is a value that we have decoded
			// recently. The latest four match distances are
			// available as rep0, rep1, rep2 and rep3. We will
			// now decode which of them is the new distance.
			//
			// There cannot be a match if we haven't produced
			// any output, so check that first.
			if (unlikely(!dict_is_distance_valid(&dict, 0))) {
				ret = LZMA_DATA_ERROR;
				goto out;
			}

			rc_if_0(coder->is_rep0[state]) {
				rc_update_0(coder->is_rep0[state]);
				// The distance is rep0.

				// Decode the next bit to determine if 1 byte
				// should be copied from rep0 distance or
				// if the number of bytes needs to be decoded.

				// If the next bit is 0, then it is a
				// "Short Rep Match" and only 1 bit is copied.
				// Otherwise, the length of the match is
				// decoded after the "else" statement.
				rc_if_0(coder->is_rep0_long[state][pos_state]) {
					rc_update_0(coder->is_rep0_long[
							state][pos_state]);

					update_short_rep(state);
					dict_put(&dict, dict_get(&dict, rep0));
					continue;
				}

				// Repeating more than one byte at
				// distance of rep0.
				rc_update_1(coder->is_rep0_long[
						state][pos_state]);

			} else {
				rc_update_1(coder->is_rep0[state]);

				// The distance is rep1, rep2 or rep3. Once
				// we find out which one of these three, it
				// is stored to rep0 and rep1, rep2 and rep3
				// are updated accordingly. There is no
				// "Short Rep Match" option, so the length
				// of the match must always be decoded next.
				rc_if_0(coder->is_rep1[state]) {
					// The distance is rep1.
					rc_update_0(coder->is_rep1[state]);

					const uint32_t distance = rep1;
					rep1 = rep0;
					rep0 = distance;

				} else {
					rc_update_1(coder->is_rep1[state]);

					rc_if_0(coder->is_rep2[state]) {
						// The distance is rep2.
						rc_update_0(coder->is_rep2[
								state]);

						const uint32_t distance = rep2;
						rep2 = rep1;
						rep1 = rep0;
						rep0 = distance;

					} else {
						// The distance is rep3.
						rc_update_1(coder->is_rep2[
								state]);

						const uint32_t distance = rep3;
						rep3 = rep2;
						rep2 = rep1;
						rep1 = rep0;
						rep0 = distance;
					}
				}
			}

			update_long_rep(state);

			// Decode the length of the repeated match.
			len_decode_fast(len, coder->rep_len_decoder,
					pos_state);
		}

		/////////////////////////////////
		// Repeat from history buffer. //
		/////////////////////////////////

		// The length is always between these limits. There is no way
		// to trigger the algorithm to set len outside this range.
		assert(len >= MATCH_LEN_MIN);
		assert(len <= MATCH_LEN_MAX);

		// Repeat len bytes from distance of rep0.
		if (unlikely(dict_repeat(&dict, rep0, &len))) {
			coder->sequence = SEQ_COPY;
			goto out;
		}

		continue;

slow:
#endif
	///////////////////////////
	// Resumable Mode (slow) //
	///////////////////////////

	// This is very similar to Non-resumable Mode, so most of the
	// comments are not repeated. The main differences are:
	// - case labels are used to resume at the correct location.
	// - Loops are not unrolled.
	// - Range coder macros take an extra sequence argument
	//   so they can save to coder->sequence the location to
	//   resume in case there is not enough input.
	case SEQ_NORMALIZE:
	case SEQ_IS_MATCH:
		if (unlikely(might_finish_without_eopm
				&& dict.pos == dict.limit)) {
			// In rare cases there is a useless byte that needs
			// to be read anyway.
			rc_normalize_safe(SEQ_NORMALIZE);

			// If the range decoder state is such that we can
			// be at the end of the LZMA stream, then the
			// decoding is finished.
			if (rc_is_finished(rc)) {
				ret = LZMA_STREAM_END;
				goto out;
			}

			// If the caller hasn't allowed EOPM to be present
			// together with known uncompressed size, then the
			// LZMA stream is corrupt.
			if (!coder->allow_eopm) {
				ret = LZMA_DATA_ERROR;
				goto out;
			}

			// Otherwise continue decoding with the expectation
			// that the next LZMA symbol is EOPM.
			eopm_is_valid = true;
		}

		rc_if_0_safe(coder->is_match[state][pos_state], SEQ_IS_MATCH) {
			/////////////////////
			// Decode literal. //
			/////////////////////

			rc_update_0(coder->is_match[state][pos_state]);

			probs = literal_subcoder(coder->literal,
					literal_context_bits, literal_mask,
					dict.pos, dict_get0(&dict));
			symbol = 1;

			if (is_literal_state(state)) {
				update_literal_normal(state);

				// Decode literal without match byte.
				// The "slow" version does not unroll
				// the loop.
	case SEQ_LITERAL:
				do {
					rc_bit_safe(probs[symbol], , ,
							SEQ_LITERAL);
				} while (symbol < (1 << 8));
			} else {
				update_literal_matched(state);

				// Decode literal with match byte.
				len = (uint32_t)(dict_get(&dict, rep0)) << 1;

				offset = 0x100;

	case SEQ_LITERAL_MATCHED:
				do {
					const uint32_t match_bit
							= len & offset;
					const uint32_t subcoder_index
							= offset + match_bit
							+ symbol;

					rc_bit_safe(probs[subcoder_index],
							offset &= ~match_bit,
							offset &= match_bit,
							SEQ_LITERAL_MATCHED);

					// It seems to be faster to do this
					// here instead of putting it to the
					// beginning of the loop and then
					// putting the "case" in the middle
					// of the loop.
					len <<= 1;

				} while (symbol < (1 << 8));
			}

	case SEQ_LITERAL_WRITE:
			if (dict_put_safe(&dict, symbol)) {
				coder->sequence = SEQ_LITERAL_WRITE;
				goto out;
			}

			continue;
		}

		///////////////////
		// Decode match. //
		///////////////////

		rc_update_1(coder->is_match[state][pos_state]);

	case SEQ_IS_REP:
		rc_if_0_safe(coder->is_rep[state], SEQ_IS_REP) {
			///////////////////
			// Simple match. //
			///////////////////

			rc_update_0(coder->is_rep[state]);
			update_match(state);

			rep3 = rep2;
			rep2 = rep1;
			rep1 = rep0;

			len_decode(len, coder->match_len_decoder,
					pos_state, SEQ_MATCH_LEN);

			probs = coder->dist_slot[get_dist_state(len)];
			symbol = 1;

	case SEQ_DIST_SLOT:
			do {
				rc_bit_safe(probs[symbol], , , SEQ_DIST_SLOT);
			} while (symbol < DIST_SLOTS);

			symbol -= DIST_SLOTS;
			assert(symbol <= 63);

			if (symbol < DIST_MODEL_START) {
				rep0 = symbol;
			} else {
				limit = (symbol >> 1) - 1;
				assert(limit >= 1 && limit <= 30);
				rep0 = 2 + (symbol & 1);

				if (symbol < DIST_MODEL_END) {
					assert(limit <= 5);
					rep0 <<= limit;
					assert(rep0 <= 96);
					// -1 is fine, because we start
					// decoding at probs[1], not probs[0].
					// NOTE: This violates the C standard,
					// since we are doing pointer
					// arithmetic past the beginning of
					// the array.
					assert((int32_t)(rep0 - symbol - 1)
							>= -1);
					assert((int32_t)(rep0 - symbol - 1)
							<= 82);
					probs = coder->pos_special + rep0
							- symbol - 1;
					symbol = 1;
					offset = 0;
	case SEQ_DIST_MODEL:
					do {
						rc_bit_safe(probs[symbol], ,
							rep0 += 1U << offset,
							SEQ_DIST_MODEL);
					} while (++offset < limit);
				} else {
					assert(symbol >= 14);
					assert(limit >= 6);
					limit -= ALIGN_BITS;
					assert(limit >= 2);
	case SEQ_DIRECT:
					rc_direct_safe(rep0, limit,
							SEQ_DIRECT);

					rep0 <<= ALIGN_BITS;
					symbol = 0;
					offset = 1;
	case SEQ_ALIGN:
					do {
						rc_bit_last_safe(
							coder->pos_align[
								offset
								+ symbol],
							,
							symbol += offset,
							SEQ_ALIGN);
						offset <<= 1;
					} while (offset < ALIGN_SIZE);

					rep0 += symbol;

					if (rep0 == UINT32_MAX) {
						// End of payload marker was
						// found. It may only be
						// present if
						//   - uncompressed size is
						//     unknown or
						//   - after known uncompressed
						//     size amount of bytes has
						//     been decompressed and
						//     caller has indicated
						//     that EOPM might be used
						//     (it's not allowed in
						//     LZMA2).
#ifndef HAVE_SMALL
eopm:
#endif
						if (!eopm_is_valid) {
							ret = LZMA_DATA_ERROR;
							goto out;
						}

	case SEQ_EOPM:
						// LZMA1 stream with
						// end-of-payload marker.
						rc_normalize_safe(SEQ_EOPM);
						ret = rc_is_finished(rc)
							? LZMA_STREAM_END
							: LZMA_DATA_ERROR;
						goto out;
					}
				}
			}

			if (unlikely(!dict_is_distance_valid(&dict, rep0))) {
				ret = LZMA_DATA_ERROR;
				goto out;
			}

		} else {
			/////////////////////
			// Repeated match. //
			/////////////////////

			rc_update_1(coder->is_rep[state]);

			if (unlikely(!dict_is_distance_valid(&dict, 0))) {
				ret = LZMA_DATA_ERROR;
				goto out;
			}

	case SEQ_IS_REP0:
			rc_if_0_safe(coder->is_rep0[state], SEQ_IS_REP0) {
				rc_update_0(coder->is_rep0[state]);

	case SEQ_IS_REP0_LONG:
				rc_if_0_safe(coder->is_rep0_long
						[state][pos_state],
						SEQ_IS_REP0_LONG) {
					rc_update_0(coder->is_rep0_long[
							state][pos_state]);

					update_short_rep(state);

	case SEQ_SHORTREP:
					if (dict_put_safe(&dict,
							dict_get(&dict,
							rep0))) {
						coder->sequence = SEQ_SHORTREP;
						goto out;
					}

					continue;
				}

				rc_update_1(coder->is_rep0_long[
						state][pos_state]);

			} else {
				rc_update_1(coder->is_rep0[state]);

	case SEQ_IS_REP1:
				rc_if_0_safe(coder->is_rep1[state], SEQ_IS_REP1) {
					rc_update_0(coder->is_rep1[state]);

					const uint32_t distance = rep1;
					rep1 = rep0;
					rep0 = distance;

				} else {
					rc_update_1(coder->is_rep1[state]);
	case SEQ_IS_REP2:
					rc_if_0_safe(coder->is_rep2[state],
							SEQ_IS_REP2) {
						rc_update_0(coder->is_rep2[
								state]);

						const uint32_t distance = rep2;
						rep2 = rep1;
						rep1 = rep0;
						rep0 = distance;

					} else {
						rc_update_1(coder->is_rep2[
								state]);

						const uint32_t distance = rep3;
						rep3 = rep2;
						rep2 = rep1;
						rep1 = rep0;
						rep0 = distance;
					}
				}
			}

			update_long_rep(state);

			len_decode(len, coder->rep_len_decoder,
					pos_state, SEQ_REP_LEN);
		}

		/////////////////////////////////
		// Repeat from history buffer. //
		/////////////////////////////////

		assert(len >= MATCH_LEN_MIN);
		assert(len <= MATCH_LEN_MAX);

	case SEQ_COPY:
		if (unlikely(dict_repeat(&dict, rep0, &len))) {
			coder->sequence = SEQ_COPY;
			goto out;
		}
	}

out:
	// Save state

	// NOTE: Must not copy dict.limit.
	dictptr->pos = dict.pos;
	dictptr->full = dict.full;

	rc_from_local(coder->rc, *in_pos);

	coder->state = state;
	coder->rep0 = rep0;
	coder->rep1 = rep1;
	coder->rep2 = rep2;
	coder->rep3 = rep3;

	coder->probs = probs;
	coder->symbol = symbol;
	coder->limit = limit;
	coder->offset = offset;
	coder->len = len;

	// Update the remaining amount of uncompressed data if uncompressed
	// size was known.
	if (coder->uncompressed_size != LZMA_VLI_UNKNOWN) {
		coder->uncompressed_size -= dict.pos - dict_start;

		// If we have gotten all the output but the decoder wants
		// to write more output, the file is corrupt. There are
		// three SEQ values where output is produced.
		if (coder->uncompressed_size == 0 && ret == LZMA_OK
				&& (coder->sequence == SEQ_LITERAL_WRITE
					|| coder->sequence == SEQ_SHORTREP
					|| coder->sequence == SEQ_COPY))
			ret = LZMA_DATA_ERROR;
	}

	if (ret == LZMA_STREAM_END) {
		// Reset the range decoder so that it is ready to reinitialize
		// for a new LZMA2 chunk.
		rc_reset(coder->rc);
		coder->sequence = SEQ_IS_MATCH;
	}

	return ret;
}


static void
lzma_decoder_uncompressed(void *coder_ptr, lzma_vli uncompressed_size,
		bool allow_eopm)
{
	lzma_lzma1_decoder *coder = coder_ptr;
	coder->uncompressed_size = uncompressed_size;
	coder->allow_eopm = allow_eopm;
}


static void
lzma_decoder_reset(void *coder_ptr, const void *opt)
{
	lzma_lzma1_decoder *coder = coder_ptr;
	const lzma_options_lzma *options = opt;

	// NOTE: We assume that lc/lp/pb are valid since they were
	// successfully decoded with lzma_lzma_decode_properties().

	// Calculate pos_mask. We don't need pos_bits as is for anything.
	coder->pos_mask = (1U << options->pb) - 1;

	// Initialize the literal decoder.
	literal_init(coder->literal, options->lc, options->lp);

	coder->literal_context_bits = options->lc;
	coder->literal_mask = literal_mask_calc(options->lc, options->lp);

	// State
	coder->state = STATE_LIT_LIT;
	coder->rep0 = 0;
	coder->rep1 = 0;
	coder->rep2 = 0;
	coder->rep3 = 0;
	coder->pos_mask = (1U << options->pb) - 1;

	// Range decoder
	rc_reset(coder->rc);

	// Bit and bittree decoders
	for (uint32_t i = 0; i < STATES; ++i) {
		for (uint32_t j = 0; j <= coder->pos_mask; ++j) {
			bit_reset(coder->is_match[i][j]);
			bit_reset(coder->is_rep0_long[i][j]);
		}

		bit_reset(coder->is_rep[i]);
		bit_reset(coder->is_rep0[i]);
		bit_reset(coder->is_rep1[i]);
		bit_reset(coder->is_rep2[i]);
	}

	for (uint32_t i = 0; i < DIST_STATES; ++i)
		bittree_reset(coder->dist_slot[i], DIST_SLOT_BITS);

	for (uint32_t i = 0; i < FULL_DISTANCES - DIST_MODEL_END; ++i)
		bit_reset(coder->pos_special[i]);

	bittree_reset(coder->pos_align, ALIGN_BITS);

	// Len decoders (also bit/bittree)
	const uint32_t num_pos_states = 1U << options->pb;
	bit_reset(coder->match_len_decoder.choice);
	bit_reset(coder->match_len_decoder.choice2);
	bit_reset(coder->rep_len_decoder.choice);
	bit_reset(coder->rep_len_decoder.choice2);

	for (uint32_t pos_state = 0; pos_state < num_pos_states; ++pos_state) {
		bittree_reset(coder->match_len_decoder.low[pos_state],
				LEN_LOW_BITS);
		bittree_reset(coder->match_len_decoder.mid[pos_state],
				LEN_MID_BITS);

		bittree_reset(coder->rep_len_decoder.low[pos_state],
				LEN_LOW_BITS);
		bittree_reset(coder->rep_len_decoder.mid[pos_state],
				LEN_MID_BITS);
	}

	bittree_reset(coder->match_len_decoder.high, LEN_HIGH_BITS);
	bittree_reset(coder->rep_len_decoder.high, LEN_HIGH_BITS);

	coder->sequence = SEQ_IS_MATCH;
	coder->probs = NULL;
	coder->symbol = 0;
	coder->limit = 0;
	coder->offset = 0;
	coder->len = 0;

	return;
}


extern lzma_ret
lzma_lzma_decoder_create(lzma_lz_decoder *lz, const lzma_allocator *allocator,
		const lzma_options_lzma *options, lzma_lz_options *lz_options)
{
	if (lz->coder == NULL) {
		lz->coder = lzma_alloc(sizeof(lzma_lzma1_decoder), allocator);
		if (lz->coder == NULL)
			return LZMA_MEM_ERROR;

		lz->code = &lzma_decode;
		lz->reset = &lzma_decoder_reset;
		lz->set_uncompressed = &lzma_decoder_uncompressed;
	}

	// All dictionary sizes are OK here. LZ decoder will take care of
	// the special cases.
	lz_options->dict_size = options->dict_size;
	lz_options->preset_dict = options->preset_dict;
	lz_options->preset_dict_size = options->preset_dict_size;

	return LZMA_OK;
}


/// Allocate and initialize LZMA decoder. This is used only via LZ
/// initialization (lzma_lzma_decoder_init() passes function pointer to
/// the LZ initialization).
static lzma_ret
lzma_decoder_init(lzma_lz_decoder *lz, const lzma_allocator *allocator,
		lzma_vli id, const void *options, lzma_lz_options *lz_options)
{
	if (!is_lclppb_valid(options))
		return LZMA_PROG_ERROR;

	lzma_vli uncomp_size = LZMA_VLI_UNKNOWN;
	bool allow_eopm = true;

	if (id == LZMA_FILTER_LZMA1EXT) {
		const lzma_options_lzma *opt = options;

		// Only one flag is supported.
		if (opt->ext_flags & ~LZMA_LZMA1EXT_ALLOW_EOPM)
			return LZMA_OPTIONS_ERROR;

		// FIXME? Using lzma_vli instead of uint64_t is weird because
		// this has nothing to do with .xz headers and variable-length
		// integer encoding. On the other hand, using LZMA_VLI_UNKNOWN
		// instead of UINT64_MAX is clearer when unknown size is
		// meant. A problem with using lzma_vli is that now we
		// allow > LZMA_VLI_MAX which is fine in this file but
		// it's still confusing. Note that alone_decoder.c also
		// allows > LZMA_VLI_MAX when setting uncompressed size.
		uncomp_size = opt->ext_size_low
				+ ((uint64_t)(opt->ext_size_high) << 32);
		allow_eopm = (opt->ext_flags & LZMA_LZMA1EXT_ALLOW_EOPM) != 0
				|| uncomp_size == LZMA_VLI_UNKNOWN;
	}

	return_if_error(lzma_lzma_decoder_create(
			lz, allocator, options, lz_options));

	lzma_decoder_reset(lz->coder, options);
	lzma_decoder_uncompressed(lz->coder, uncomp_size, allow_eopm);

	return LZMA_OK;
}


extern lzma_ret
lzma_lzma_decoder_init(lzma_next_coder *next, const lzma_allocator *allocator,
		const lzma_filter_info *filters)
{
	// LZMA can only be the last filter in the chain. This is enforced
	// by the raw_decoder initialization.
	assert(filters[1].init == NULL);

	return lzma_lz_decoder_init(next, allocator, filters,
			&lzma_decoder_init);
}


extern bool
lzma_lzma_lclppb_decode(lzma_options_lzma *options, uint8_t byte)
{
	if (byte > (4 * 5 + 4) * 9 + 8)
		return true;

	// See the file format specification to understand this.
	options->pb = byte / (9 * 5);
	byte -= options->pb * 9 * 5;
	options->lp = byte / 9;
	options->lc = byte - options->lp * 9;

	return options->lc + options->lp > LZMA_LCLP_MAX;
}


extern uint64_t
lzma_lzma_decoder_memusage_nocheck(const void *options)
{
	const lzma_options_lzma *const opt = options;
	return sizeof(lzma_lzma1_decoder)
			+ lzma_lz_decoder_memusage(opt->dict_size);
}


extern uint64_t
lzma_lzma_decoder_memusage(const void *options)
{
	if (!is_lclppb_valid(options))
		return UINT64_MAX;

	return lzma_lzma_decoder_memusage_nocheck(options);
}


extern lzma_ret
lzma_lzma_props_decode(void **options, const lzma_allocator *allocator,
		const uint8_t *props, size_t props_size)
{
	if (props_size != 5)
		return LZMA_OPTIONS_ERROR;

	lzma_options_lzma *opt
			= lzma_alloc(sizeof(lzma_options_lzma), allocator);
	if (opt == NULL)
		return LZMA_MEM_ERROR;

	if (lzma_lzma_lclppb_decode(opt, props[0]))
		goto error;

	// All dictionary sizes are accepted, including zero. LZ decoder
	// will automatically use a dictionary at least a few KiB even if
	// a smaller dictionary is requested.
	opt->dict_size = read32le(props + 1);

	opt->preset_dict = NULL;
	opt->preset_dict_size = 0;

	*options = opt;

	return LZMA_OK;

error:
	lzma_free(opt, allocator);
	return LZMA_OPTIONS_ERROR;
}
