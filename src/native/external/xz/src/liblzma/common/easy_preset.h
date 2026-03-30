// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       easy_preset.h
/// \brief      Preset handling for easy encoder and decoder
//
//  Author:     Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#ifndef LZMA_EASY_PRESET_H
#define LZMA_EASY_PRESET_H

#include "common.h"


typedef struct {
	/// We need to keep the filters array available in case
	/// LZMA_FULL_FLUSH is used.
	lzma_filter filters[LZMA_FILTERS_MAX + 1];

	/// Options for LZMA2
	lzma_options_lzma opt_lzma;

	// Options for more filters can be added later, so this struct
	// is not ready to be put into the public API.

} lzma_options_easy;


/// Set *easy to the settings given by the preset. Returns true on error,
/// false on success.
extern bool lzma_easy_preset(lzma_options_easy *easy, uint32_t preset);

#endif
