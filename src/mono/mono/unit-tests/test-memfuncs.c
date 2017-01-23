/*
 * test-sgen-qsort.c: Unit test for our own bzero/memmove.
 *
 * Copyright (C) 2013 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"

#include "utils/memfuncs.h"

#include <stdlib.h>
#include <string.h>
#include <time.h>
#include <assert.h>

#define POOL_SIZE	2048
#define START_OFFSET	128

#define BZERO_OFFSETS	64
#define BZERO_SIZES	256

#define MEMMOVE_SRC_OFFSETS		32
#define MEMMOVE_DEST_OFFSETS		32
#define MEMMOVE_SIZES			256
#define MEMMOVE_NONOVERLAP_START	1024

int
main (void)
{
	unsigned char *random_mem = (unsigned char *)malloc (POOL_SIZE);
	unsigned char *reference = (unsigned char *)malloc (POOL_SIZE);
	unsigned char *playground = (unsigned char *)malloc (POOL_SIZE);
	long *long_random_mem;
	int i, offset, size, src_offset, dest_offset;

	srandom (time (NULL));

	/* init random memory */
	long_random_mem = (long*)random_mem;
	for (i = 0; i < POOL_SIZE / sizeof (long); ++i)
		long_random_mem [i] = random ();

	/* test bzero */
	for (offset = 0; offset <= BZERO_OFFSETS; ++offset) {
		for (size = 0; size <= BZERO_SIZES; ++size) {
			memcpy (reference, random_mem, POOL_SIZE);
			memcpy (playground, random_mem, POOL_SIZE);

			memset (reference + START_OFFSET + offset, 0, size);
			mono_gc_bzero_atomic (playground + START_OFFSET + offset, size);

			assert (!memcmp (reference, playground, POOL_SIZE));
		}
	}

	/* test memmove */
	for (src_offset = -MEMMOVE_SRC_OFFSETS; src_offset <= MEMMOVE_SRC_OFFSETS; ++src_offset) {
		for (dest_offset = -MEMMOVE_DEST_OFFSETS; dest_offset <= MEMMOVE_DEST_OFFSETS; ++dest_offset) {
			for (size = 0; size <= MEMMOVE_SIZES; ++size) {
				/* overlapping */
				memcpy (reference, random_mem, POOL_SIZE);
				memcpy (playground, random_mem, POOL_SIZE);

				memmove (reference + START_OFFSET + dest_offset, reference + START_OFFSET + src_offset, size);
				mono_gc_memmove_atomic (playground + START_OFFSET + dest_offset, playground + START_OFFSET + src_offset, size);

				assert (!memcmp (reference, playground, POOL_SIZE));

				/* non-overlapping with dest < src */
				memcpy (reference, random_mem, POOL_SIZE);
				memcpy (playground, random_mem, POOL_SIZE);

				memmove (reference + START_OFFSET + dest_offset, reference + MEMMOVE_NONOVERLAP_START + src_offset, size);
				mono_gc_memmove_atomic (playground + START_OFFSET + dest_offset, playground + MEMMOVE_NONOVERLAP_START + src_offset, size);

				assert (!memcmp (reference, playground, POOL_SIZE));

				/* non-overlapping with dest > src */
				memcpy (reference, random_mem, POOL_SIZE);
				memcpy (playground, random_mem, POOL_SIZE);

				memmove (reference + MEMMOVE_NONOVERLAP_START + dest_offset, reference + START_OFFSET + src_offset, size);
				mono_gc_memmove_atomic (playground + MEMMOVE_NONOVERLAP_START + dest_offset, playground + START_OFFSET + src_offset, size);

				assert (!memcmp (reference, playground, POOL_SIZE));
			}
		}
	}

	free (random_mem);
	free (reference);
	free (playground);

	return 0;
}
