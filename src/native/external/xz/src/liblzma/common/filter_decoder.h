// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       filter_decoder.h
/// \brief      Filter ID mapping to filter-specific functions
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_FILTER_DECODER_H
#define LZMA_FILTER_DECODER_H

#include "common.h"


extern lzma_ret lzma_raw_decoder_init(
		lzma_next_coder *next, const lzma_allocator *allocator,
		const lzma_filter *options);

#endif
