/**
 * \file
 * Copyright Xamarin Inc (http://www.xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGEN_LAYOUT_STATS_H__
#define __MONO_SGEN_LAYOUT_STATS_H__

#ifdef SGEN_OBJECT_LAYOUT_STATISTICS

#define SGEN_OBJECT_LAYOUT_BITMAP_BITS	16

void sgen_object_layout_scanned_bitmap (unsigned int bitmap);
void sgen_object_layout_scanned_bitmap_overflow (void);
void sgen_object_layout_scanned_ref_array (void);
void sgen_object_layout_scanned_vtype_array (void);

void sgen_object_layout_dump (FILE *out);

#define SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP	unsigned int __object_layout_bitmap = 0
#define SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP(o,p)	do {		\
		int __index = ((void**)(p)) - ((void**)(((char*)(o)) + SGEN_CLIENT_OBJECT_HEADER_SIZE)); \
		if (__index >= SGEN_OBJECT_LAYOUT_BITMAP_BITS)		\
			__object_layout_bitmap = (unsigned int)-1;	\
		else if (__object_layout_bitmap != (unsigned int)-1)	\
			__object_layout_bitmap |= (1 << __index);	\
	} while (0)
#define SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP do {		\
		if (__object_layout_bitmap == (unsigned int)-1)		\
			sgen_object_layout_scanned_bitmap_overflow ();	\
		else							\
			sgen_object_layout_scanned_bitmap (__object_layout_bitmap); \
	} while (0)

#else

#define sgen_object_layout_scanned_bitmap(bitmap)
#define sgen_object_layout_scanned_bitmap_overflow()
#define sgen_object_layout_scanned_ref_array()
#define sgen_object_layout_scanned_vtype_array()

#define sgen_object_layout_dump(out)

#define SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP
#define SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP(o,p)
#define SGEN_OBJECT_LAYOUT_STATISTICS_COMMIT_BITMAP

#endif

#endif
