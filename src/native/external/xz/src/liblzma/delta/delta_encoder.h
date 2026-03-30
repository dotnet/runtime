// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       delta_encoder.h
/// \brief      Delta filter encoder
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_DELTA_ENCODER_H
#define LZMA_DELTA_ENCODER_H

#include "delta_common.h"

extern lzma_ret lzma_delta_encoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator,
		const lzma_filter_info *filters);

extern lzma_ret lzma_delta_props_encode(const void *options, uint8_t *out);

#endif
