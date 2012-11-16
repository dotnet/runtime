/*
 * Copyright Xamarin Inc (http://www.xamarin.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "metadata/sgen-gc.h"
#include "metadata/sgen-layout-stats.h"

#ifdef SGEN_OBJECT_LAYOUT_STATISTICS

#define NUM_HISTOGRAM_ENTRIES	(1 << SGEN_OBJECT_LAYOUT_BITMAP_BITS)

static unsigned long histogram [NUM_HISTOGRAM_ENTRIES];
static unsigned long count_bitmap_overflow;
static unsigned long count_ref_array;
static unsigned long count_vtype_array;

void
sgen_object_layout_scanned_bitmap (unsigned int bitmap)
{
	g_assert (!(bitmap >> SGEN_OBJECT_LAYOUT_BITMAP_BITS));
	++histogram [bitmap];
}

void
sgen_object_layout_scanned_bitmap_overflow (void)
{
	++count_bitmap_overflow;
}

void
sgen_object_layout_scanned_ref_array (void)
{
	++count_ref_array;
}

void
sgen_object_layout_scanned_vtype_array (void)
{
	++count_vtype_array;
}

void
sgen_object_layout_dump (FILE *out)
{
	int i;

	for (i = 0; i < NUM_HISTOGRAM_ENTRIES; ++i) {
		if (!histogram [i])
			continue;
		fprintf (out, "%d %lu\n", i, histogram [i]);
	}
	fprintf (out, "bitmap-overflow %lu\n", count_bitmap_overflow);
	fprintf (out, "ref-array %lu\n", count_ref_array);
	fprintf (out, "vtype-array %lu\n", count_vtype_array);
}

#endif
#endif
