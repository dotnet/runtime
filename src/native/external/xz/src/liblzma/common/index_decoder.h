// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       index_decoder.h
/// \brief      Decodes the Index field
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_INDEX_DECODER_H
#define LZMA_INDEX_DECODER_H

#include "common.h"
#include "index.h"


extern lzma_ret lzma_index_decoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator,
		lzma_index **i, uint64_t memlimit);


#endif
