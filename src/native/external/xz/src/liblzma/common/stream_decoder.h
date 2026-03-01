// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       stream_decoder.h
/// \brief      Decodes .xz Streams
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_STREAM_DECODER_H
#define LZMA_STREAM_DECODER_H

#include "common.h"

extern lzma_ret lzma_stream_decoder_init(
		lzma_next_coder *next, const lzma_allocator *allocator,
		uint64_t memlimit, uint32_t flags);

#endif
