/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
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
#ifndef __MONO_SGEN_DESCRIPTOR_H__
#define __MONO_SGEN_DESCRIPTOR_H__

#include <mono/metadata/gc-internal.h>
#include <mono/metadata/sgen-conf.h>


/*
 * ######################################################################
 * ########  GC descriptors
 * ######################################################################
 * Used to quickly get the info the GC needs about an object: size and
 * where the references are held.
 */
#define OBJECT_HEADER_WORDS (sizeof(MonoObject)/sizeof(gpointer))
#define LOW_TYPE_BITS 3
#define MAX_RUNLEN_OBJECT_SIZE 0xFFFF
#define SMALL_BITMAP_SHIFT 16
#define SMALL_BITMAP_SIZE (GC_BITS_PER_WORD - SMALL_BITMAP_SHIFT)
#define VECTOR_INFO_SHIFT 14
#define VECTOR_KIND_SHIFT 13
#define VECTOR_ELSIZE_SHIFT 3
#define LARGE_BITMAP_SIZE (GC_BITS_PER_WORD - LOW_TYPE_BITS)
#define MAX_ELEMENT_SIZE 0x3ff
#define VECTOR_SUBTYPE_PTRFREE (DESC_TYPE_V_PTRFREE << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_REFS    (DESC_TYPE_V_REFS << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_RUN_LEN (DESC_TYPE_V_RUN_LEN << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_BITMAP  (DESC_TYPE_V_BITMAP << VECTOR_INFO_SHIFT)

#define VECTOR_KIND_SZARRAY  (DESC_TYPE_V_SZARRAY << VECTOR_KIND_SHIFT)
#define VECTOR_KIND_ARRAY  (DESC_TYPE_V_ARRAY << VECTOR_KIND_SHIFT)

/* objects are aligned to 8 bytes boundaries
 * A descriptor is a pointer in MonoVTable, so 32 or 64 bits of size.
 * The low 3 bits define the type of the descriptor. The other bits
 * depend on the type.
 * As a general rule the 13 remaining low bits define the size, either
 * of the whole object or of the elements in the arrays. While for objects
 * the size is already in bytes, for arrays we need to shift, because
 * array elements might be smaller than 8 bytes. In case of arrays, we
 * use two bits to describe what the additional high bits represents,
 * so the default behaviour can handle element sizes less than 2048 bytes.
 * The high 16 bits, if 0 it means the object is pointer-free.
 * This design should make it easy and fast to skip over ptr-free data.
 * The first 4 types should cover >95% of the objects.
 * Note that since the size of objects is limited to 64K, larger objects
 * will be allocated in the large object heap.
 * If we want 4-bytes alignment, we need to put vector and small bitmap
 * inside complex.
 */
enum {
	/*
	 * We don't use 0 so that 0 isn't a valid GC descriptor.  No
	 * deep reason for this other than to be able to identify a
	 * non-inited descriptor for debugging.
	 *
	 * If an object contains no references, its GC descriptor is
	 * always DESC_TYPE_RUN_LENGTH, without a size, no exceptions.
	 * This is so that we can quickly check for that in
	 * copy_object_no_checks(), without having to fetch the
	 * object's class.
	 */
	DESC_TYPE_RUN_LENGTH = 1, /* 16 bits aligned byte size | 1-3 (offset, numptr) bytes tuples */
	DESC_TYPE_SMALL_BITMAP,   /* 16 bits aligned byte size | 16-48 bit bitmap */
	DESC_TYPE_COMPLEX,      /* index for bitmap into complex_descriptors */
	DESC_TYPE_VECTOR,       /* 10 bits element size | 1 bit kind | 2 bits desc | element desc */
	DESC_TYPE_LARGE_BITMAP, /* | 29-61 bitmap bits */
	DESC_TYPE_COMPLEX_ARR,  /* index for bitmap into complex_descriptors */
	DESC_TYPE_COMPLEX_PTRFREE, /*Nothing, used to encode large ptr objects. */
	/* values for array kind */
	DESC_TYPE_V_SZARRAY = 0, /*vector with no bounds data */
	DESC_TYPE_V_ARRAY = 1, /* array with bounds data */
	/* subtypes for arrays and vectors */
	DESC_TYPE_V_PTRFREE = 0,/* there are no refs: keep first so it has a zero value  */
	DESC_TYPE_V_REFS,       /* all the array elements are refs */
	DESC_TYPE_V_RUN_LEN,    /* elements are run-length encoded as DESC_TYPE_RUN_LENGTH */
	DESC_TYPE_V_BITMAP      /* elements are as the bitmap in DESC_TYPE_SMALL_BITMAP */
};

/* Root bitmap descriptors are simpler: the lower three bits describe the type
 * and we either have 30/62 bitmap bits or nibble-based run-length,
 * or a complex descriptor, or a user defined marker function.
 */
enum {
	ROOT_DESC_CONSERVATIVE, /* 0, so matches NULL value */
	ROOT_DESC_BITMAP,
	ROOT_DESC_RUN_LEN, 
	ROOT_DESC_COMPLEX,
	ROOT_DESC_USER,
	ROOT_DESC_TYPE_MASK = 0x7,
	ROOT_DESC_TYPE_SHIFT = 3,
};

gsize* sgen_get_complex_descriptor (mword desc) MONO_INTERNAL;
void* sgen_get_complex_descriptor_bitmap (mword desc) MONO_INTERNAL;
MonoGCRootMarkFunc sgen_get_user_descriptor_func (mword desc) MONO_INTERNAL;


static inline gboolean
sgen_gc_descr_has_references (mword desc)
{
	/*Both string and fixed size objects are encoded using a zero run RUN_LEN*/
	if ((desc & 0xffff0007) == DESC_TYPE_RUN_LENGTH)
		return FALSE;

	/*The array is ptr-free*/
	if ((desc & 0xC007) == (DESC_TYPE_VECTOR | VECTOR_SUBTYPE_PTRFREE))
		return FALSE;

	if ((desc & 0x7) == DESC_TYPE_COMPLEX_PTRFREE)
		return FALSE;

	return TRUE;
}

#define SGEN_VTABLE_HAS_REFERENCES(vt)	(sgen_gc_descr_has_references ((mword)((MonoVTable*)(vt))->gc_descr))
#define SGEN_CLASS_HAS_REFERENCES(c)	(sgen_gc_descr_has_references ((mword)(c)->gc_descr))

/* helper macros to scan and traverse objects, macros because we resue them in many functions */
#define OBJ_RUN_LEN_SIZE(size,desc,obj) do { \
		(size) = ((desc) & 0xfff8);	\
    } while (0)

#define OBJ_BITMAP_SIZE(size,desc,obj) do { \
		(size) = ((desc) & 0xfff8);	\
    } while (0)

#ifdef __GNUC__
#define PREFETCH(addr)	__builtin_prefetch ((addr))
#else
#define PREFETCH(addr)
#endif

/* code using these macros must define a HANDLE_PTR(ptr) macro that does the work */
#define OBJ_RUN_LEN_FOREACH_PTR(desc,obj)	do {	\
		if ((desc) & 0xffff0000) {	\
			/* there are pointers */	\
			void **_objptr_end;	\
			void **_objptr = (void**)(obj);	\
			_objptr += ((desc) >> 16) & 0xff;	\
			_objptr_end = _objptr + (((desc) >> 24) & 0xff);	\
			while (_objptr < _objptr_end) {	\
				HANDLE_PTR (_objptr, (obj));	\
				_objptr++;	\
			}	\
		}	\
	} while (0)

#define OBJ_BITMAP_FOREACH_PTR(desc,obj)       do {    \
		/* there are pointers */        \
		void **_objptr = (void**)(obj); \
		gsize _bmap = (desc) >> 16;     \
		_objptr += OBJECT_HEADER_WORDS; \
		while (_bmap) { \
			if ((_bmap & 1)) {      \
				HANDLE_PTR (_objptr, (obj));    \
			}       \
			_bmap >>= 1;    \
			++_objptr;      \
			}       \
	} while (0)

/* a bitmap desc means that there are pointer references or we'd have
 * choosen run-length, instead: add an assert to check.
 */
#define OBJ_LARGE_BITMAP_FOREACH_PTR(desc,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize _bmap = (desc) >> LOW_TYPE_BITS;	\
		_objptr += OBJECT_HEADER_WORDS;	\
		while (_bmap) {	\
			if ((_bmap & 1)) {	\
				HANDLE_PTR (_objptr, (obj));	\
			}	\
			_bmap >>= 1;	\
			++_objptr;	\
		}	\
	} while (0)

#define OBJ_COMPLEX_FOREACH_PTR(vt,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize *bitmap_data = sgen_get_complex_descriptor ((desc)); \
		int bwords = (*bitmap_data) - 1;	\
		void **start_run = _objptr;	\
		bitmap_data++;	\
		if (0) {	\
			MonoObject *myobj = (MonoObject*)obj;	\
			g_print ("found %d at %p (0x%zx): %s.%s\n", bwords, (obj), (desc), myobj->vtable->klass->name_space, myobj->vtable->klass->name); \
		}	\
		while (bwords-- > 0) {	\
			gsize _bmap = *bitmap_data++;	\
			_objptr = start_run;	\
			/*g_print ("bitmap: 0x%x/%d at %p\n", _bmap, bwords, _objptr);*/	\
			while (_bmap) {	\
				if ((_bmap & 1)) {	\
					HANDLE_PTR (_objptr, (obj));	\
				}	\
				_bmap >>= 1;	\
				++_objptr;	\
			}	\
			start_run += GC_BITS_PER_WORD;	\
		}	\
	} while (0)

/* this one is untested */
#define OBJ_COMPLEX_ARR_FOREACH_PTR(vt,obj)	do {	\
		/* there are pointers */	\
		gsize *mbitmap_data = sgen_get_complex_descriptor ((vt)->desc); \
		int mbwords = (*mbitmap_data++) - 1;	\
		int el_size = mono_array_element_size (vt->klass);	\
		char *e_start = (char*)(obj) +  G_STRUCT_OFFSET (MonoArray, vector);	\
		char *e_end = e_start + el_size * mono_array_length_fast ((MonoArray*)(obj));	\
		if (0)							\
                        g_print ("found %d at %p (0x%zx): %s.%s\n", mbwords, (obj), (vt)->desc, vt->klass->name_space, vt->klass->name); \
		while (e_start < e_end) {	\
			void **_objptr = (void**)e_start;	\
			gsize *bitmap_data = mbitmap_data;	\
			unsigned int bwords = mbwords;	\
			while (bwords-- > 0) {	\
				gsize _bmap = *bitmap_data++;	\
				void **start_run = _objptr;	\
				/*g_print ("bitmap: 0x%x\n", _bmap);*/	\
				while (_bmap) {	\
					if ((_bmap & 1)) {	\
						HANDLE_PTR (_objptr, (obj));	\
					}	\
					_bmap >>= 1;	\
					++_objptr;	\
				}	\
				_objptr = start_run + GC_BITS_PER_WORD;	\
			}	\
			e_start += el_size;	\
		}	\
	} while (0)

#define OBJ_VECTOR_FOREACH_PTR(desc,obj)	do {	\
		/* note: 0xffffc000 excludes DESC_TYPE_V_PTRFREE */	\
		if ((desc) & 0xffffc000) {				\
			int el_size = ((desc) >> 3) & MAX_ELEMENT_SIZE;	\
			/* there are pointers */	\
			int etype = (desc) & 0xc000;			\
			if (etype == (DESC_TYPE_V_REFS << 14)) {	\
				void **p = (void**)((char*)(obj) + G_STRUCT_OFFSET (MonoArray, vector));	\
				void **end_refs = (void**)((char*)p + el_size * mono_array_length_fast ((MonoArray*)(obj)));	\
				/* Note: this code can handle also arrays of struct with only references in them */	\
				while (p < end_refs) {	\
					HANDLE_PTR (p, (obj));	\
					++p;	\
				}	\
			} else if (etype == DESC_TYPE_V_RUN_LEN << 14) {	\
				int offset = ((desc) >> 16) & 0xff;	\
				int num_refs = ((desc) >> 24) & 0xff;	\
				char *e_start = (char*)(obj) + G_STRUCT_OFFSET (MonoArray, vector);	\
				char *e_end = e_start + el_size * mono_array_length_fast ((MonoArray*)(obj));	\
				while (e_start < e_end) {	\
					void **p = (void**)e_start;	\
					int i;	\
					p += offset;	\
					for (i = 0; i < num_refs; ++i) {	\
						HANDLE_PTR (p + i, (obj));	\
					}	\
					e_start += el_size;	\
				}	\
			} else if (etype == DESC_TYPE_V_BITMAP << 14) {	\
				char *e_start = (char*)(obj) +  G_STRUCT_OFFSET (MonoArray, vector);	\
				char *e_end = e_start + el_size * mono_array_length_fast ((MonoArray*)(obj));	\
				while (e_start < e_end) {	\
					void **p = (void**)e_start;	\
					gsize _bmap = (desc) >> 16;	\
					/* Note: there is no object header here to skip */	\
					while (_bmap) {	\
						if ((_bmap & 1)) {	\
							HANDLE_PTR (p, (obj));	\
						}	\
						_bmap >>= 1;	\
						++p;	\
					}	\
					e_start += el_size;	\
				}	\
			}	\
		}	\
	} while (0)


#endif