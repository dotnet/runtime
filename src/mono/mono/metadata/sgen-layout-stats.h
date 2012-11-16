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
#ifndef __MONO_SGEN_LAYOUT_STATS_H__
#define __MONO_SGEN_LAYOUT_STATS_H__

#ifdef SGEN_OBJECT_LAYOUT_STATISTICS

#define SGEN_OBJECT_LAYOUT_BITMAP_BITS	16

void sgen_object_layout_scanned_bitmap (unsigned int bitmap) MONO_INTERNAL;
void sgen_object_layout_scanned_bitmap_overflow (void) MONO_INTERNAL;
void sgen_object_layout_scanned_ref_array (void) MONO_INTERNAL;
void sgen_object_layout_scanned_vtype_array (void) MONO_INTERNAL;

void sgen_object_layout_dump (FILE *out) MONO_INTERNAL;

#define SGEN_OBJECT_LAYOUT_STATISTICS_DECLARE_BITMAP	unsigned int __object_layout_bitmap = 0
#define SGEN_OBJECT_LAYOUT_STATISTICS_MARK_BITMAP(o,p)	do {		\
		int __index = ((void**)(p)) - ((void**)(((char*)(o)) + sizeof (MonoObject))); \
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
