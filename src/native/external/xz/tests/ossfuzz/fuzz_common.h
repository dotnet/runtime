// SPDX-License-Identifier: 0BSD

///////////////////////////////////////////////////////////////////////////////
//
/// \file       fuzz_common.h
/// \brief      Common macros and functions needed by the fuzz targets
//
//  Authors:    Maksym Vatsyk
//              Lasse Collin
//
///////////////////////////////////////////////////////////////////////////////

#include <inttypes.h>
#include <stdlib.h>
#include <stdio.h>
#include "lzma.h"

// Some header values can make liblzma allocate a lot of RAM
// (up to about 4 GiB with liblzma 5.2.x). We set a limit here to
// prevent extreme allocations when fuzzing.
#define MEM_LIMIT (300 << 20) // 300 MiB

// Amount of input to pass to lzma_code() per call at most.
#define IN_CHUNK_SIZE 2047


static void
fuzz_code(lzma_stream *stream, const uint8_t *inbuf, size_t inbuf_size) {
	// Output buffer for decompressed data. This is write only; nothing
	// cares about the actual data written here.
	uint8_t outbuf[4096];

	// Pass half of the input on the first call and then proceed in
	// chunks. It's fine that this rounds to 0 when inbuf_size is 1.
	stream->next_in = inbuf;
	stream->avail_in = inbuf_size / 2;

	lzma_action action = LZMA_RUN;

	lzma_ret ret;
	do {
		if (stream->avail_in == 0 && inbuf_size > 0) {
			const size_t chunk_size = inbuf_size < IN_CHUNK_SIZE
					? inbuf_size : IN_CHUNK_SIZE;

			stream->next_in = inbuf;
			stream->avail_in = chunk_size;

			inbuf += chunk_size;
			inbuf_size -= chunk_size;

			if (inbuf_size == 0)
				action = LZMA_FINISH;
		}

		if (stream->avail_out == 0) {
			// outbuf became full. We don't care about the
			// uncompressed data there, so we simply reuse
			// the outbuf and overwrite the old data.
			stream->next_out = outbuf;
			stream->avail_out = sizeof(outbuf);
		}
	} while ((ret = lzma_code(stream, action)) == LZMA_OK);

	// LZMA_PROG_ERROR should never happen as long as the code calling
	// the liblzma functions is correct. Thus LZMA_PROG_ERROR is a sign
	// of a bug in either this function or in liblzma.
	if (ret == LZMA_PROG_ERROR) {
		fprintf(stderr, "lzma_code() returned LZMA_PROG_ERROR\n");
		abort();
	}
}
