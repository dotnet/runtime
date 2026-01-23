// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       fuzz_decode_stream_mt.c
/// \brief      Fuzz test program for multithreaded .xz decoding
//
//  Author:     Lasse Collin
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
	lzma_stream strm = LZMA_STREAM_INIT;

	lzma_mt mt = {
		.flags = LZMA_CONCATENATED | LZMA_IGNORE_CHECK,
		.threads = 2,
		.timeout = 0,
		.memlimit_threading = MEM_LIMIT / 2,
		.memlimit_stop = MEM_LIMIT,
	};

	lzma_ret ret = lzma_stream_decoder_mt(&strm, &mt);

	if (ret != LZMA_OK) {
		// This should never happen unless the system has
		// no free memory or address space to allow the small
		// allocations that the initialization requires.
		fprintf(stderr, "lzma_stream_decoder_mt() failed (%d)\n", ret);
		abort();
	}

	fuzz_code(&strm, inbuf, inbuf_size);

	lzma_end(&strm);

	return 0;
}
