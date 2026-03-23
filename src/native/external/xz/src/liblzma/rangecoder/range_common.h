// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       range_common.h
/// \brief      Common things for range encoder and decoder
///
//  Authors:    Igor Pavlov
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_RANGE_COMMON_H
#define LZMA_RANGE_COMMON_H

// Skip common.h if building price_tablegen.c.
#ifndef BUILDING_PRICE_TABLEGEN
#	include "common.h"
#endif


///////////////
// Constants //
///////////////

#define RC_SHIFT_BITS 8
#define RC_TOP_BITS 24
#define RC_TOP_VALUE (UINT32_C(1) << RC_TOP_BITS)
#define RC_BIT_MODEL_TOTAL_BITS 11
#define RC_BIT_MODEL_TOTAL (UINT32_C(1) << RC_BIT_MODEL_TOTAL_BITS)
#define RC_MOVE_BITS 5


////////////
// Macros //
////////////

// Resets the probability so that both 0 and 1 have probability of 50 %
#define bit_reset(prob) \
	prob = RC_BIT_MODEL_TOTAL >> 1

// This does the same for a complete bit tree.
// (A tree represented as an array.)
#define bittree_reset(probs, bit_levels) \
	for (uint32_t bt_i = 0; bt_i < (1 << (bit_levels)); ++bt_i) \
		bit_reset((probs)[bt_i])


//////////////////////
// Type definitions //
//////////////////////

/// \brief      Type of probabilities used with range coder
///
/// This needs to be at least 12-bit integer, so uint16_t is a logical choice.
/// However, on some architecture and compiler combinations, a bigger type
/// may give better speed, because the probability variables are accessed
/// a lot. On the other hand, bigger probability type increases cache
/// footprint, since there are 2 to 14 thousand probability variables in
/// LZMA (assuming the limit of lc + lp <= 4; with lc + lp <= 12 there
/// would be about 1.5 million variables).
///
/// With malicious files, the initialization speed of the LZMA decoder can
/// become important. In that case, smaller probability variables mean that
/// there is less bytes to write to RAM, which makes initialization faster.
/// With big probability type, the initialization can become so slow that it
/// can be a problem e.g. for email servers doing virus scanning.
///
/// I will be sticking to uint16_t unless some specific architectures
/// are *much* faster (20-50 %) with uint32_t.
///
/// Update in 2024: The branchless C and x86-64 assembly was written so that
/// probability is assumed to be uint16_t. (In contrast, LZMA SDK 23.01
/// assembly supports both types.)
typedef uint16_t probability;

#endif
