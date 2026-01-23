// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       delta_decoder.h
/// \brief      Delta filter decoder
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_DELTA_DECODER_H
#define LZMA_DELTA_DECODER_H

#include "delta_common.h"

extern lzma_ret lzma_delta_decoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator,
		const lzma_filter_info *filters);

extern lzma_ret lzma_delta_props_decode(
		void **options, const lzma_allocator *allocator,
		const uint8_t *props, size_t props_size);

#endif
