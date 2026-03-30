// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       easy_encoder.c
/// \brief      Easy .xz Stream encoder initialization
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include "easy_preset.h"


extern LZMA_API(lzma_ret)
lzma_easy_encoder(lzma_stream *strm, uint32_t preset, lzma_check check)
{
	lzma_options_easy opt_easy;
	if (lzma_easy_preset(&opt_easy, preset))
		return LZMA_OPTIONS_ERROR;

	return lzma_stream_encoder(strm, opt_easy.filters, check);
}
