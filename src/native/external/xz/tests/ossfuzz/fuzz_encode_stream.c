// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       fuzz_encode_stream.c
/// \brief      Fuzz test program for .xz encoding
//
//  Authors:    Maksym Vatsyk
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include <inttypes.h>
#include <stdlib.h>
#include <stdio.h>
#include "lzma.h"
#include "fuzz_common.h"


extern int
LLVMFuzzerTestOneInput(const uint8_t *inbuf, size_t inbuf_size)
{
	if (inbuf_size == 0) {
		fprintf(stderr, "no input data provided\n");
		return 0;
	}

	// Set the LZMA options based on the first input byte. The fuzzer
	// will learn through its mutational genetic algorithm with the
	// code coverage feedback that the first byte must be one of the
	// values with a switch case label. This allows us to have one fuzz
	// target cover many critical code paths so the fuzz resources can
	// be used efficiently.
	uint32_t preset_level;
	const uint8_t decider = inbuf[0];

	switch (decider) {
	case 0:
	case 1:
	case 5:
		preset_level = (uint32_t)decider;
		break;
	case 6:
		preset_level = 0 | LZMA_PRESET_EXTREME;
		break;
	case 7:
		preset_level = 3 | LZMA_PRESET_EXTREME;
		break;
	default:
		return 0;
	}

	// Initialize lzma_options with the above preset level
	lzma_options_lzma opt_lzma;
	if (lzma_lzma_preset(&opt_lzma, preset_level)){
		fprintf(stderr, "lzma_lzma_preset() failed\n");
		abort();
	}

	// Set the filter chain as only LZMA2.
	lzma_filter filters[2] = {
		{
			.id = LZMA_FILTER_LZMA2,
			.options = &opt_lzma,
		}, {
			.id = LZMA_VLI_UNKNOWN,
		}
	};

	// initialize empty LZMA stream
	lzma_stream strm = LZMA_STREAM_INIT;

	// Initialize the stream encoder using the above
	// stream, filter chain and CRC64.
	lzma_ret ret = lzma_stream_encoder(&strm, filters, LZMA_CHECK_CRC64);
	if (ret != LZMA_OK) {
		fprintf(stderr, "lzma_stream_encoder() failed (%d)\n", ret);
		abort();
	}

	fuzz_code(&strm, inbuf  + 1, inbuf_size - 1);

	// Free the allocated memory.
	lzma_end(&strm);
	return 0;
}
