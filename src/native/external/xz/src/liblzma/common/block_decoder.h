// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       block_decoder.h
/// \brief      Decodes .xz Blocks
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_BLOCK_DECODER_H
#define LZMA_BLOCK_DECODER_H

#include "common.h"


extern lzma_ret lzma_block_decoder_init(lzma_next_coder *next,
		const lzma_allocator *allocator, lzma_block *block);

#endif
