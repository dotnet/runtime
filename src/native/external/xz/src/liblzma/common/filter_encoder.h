// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       filter_encoder.h
/// \brief      Filter ID mapping to filter-specific functions
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_FILTER_ENCODER_H
#define LZMA_FILTER_ENCODER_H

#include "common.h"


extern lzma_ret lzma_raw_encoder_init(
		lzma_next_coder *next, const lzma_allocator *allocator,
		const lzma_filter *filters);

#endif
