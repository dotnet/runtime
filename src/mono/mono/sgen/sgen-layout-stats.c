/*
 * Copyright Xamarin Inc (http://www.xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_SGEN_GC

#include "sgen/sgen-gc.h"
#include "sgen/sgen-layout-stats.h"

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
#else

#ifdef _MSC_VER
// Quiet Visual Studio linker warning, LNK4221, in cases when this source file intentional ends up empty.
void __mono_win32_sgen_layout_stats_quiet_lnk4221(void) {}
#endif
#endif /* SGEN_OBJECT_LAYOUT_STATISTICS */
#endif
