/**
 * \file
 * GC descriptors describe object layout.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright 2011 Xamarin Inc (http://www.xamarin.com)
 *
 * Copyright (C) 2012 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_SGEN_DESCRIPTOR_H__
#define __MONO_SGEN_DESCRIPTOR_H__

#include <mono/sgen/sgen-conf.h>

/*
 * ######################################################################
 * ########  GC descriptors
 * ######################################################################
 * Used to quickly get the info the GC needs about an object: size and
 * where the references are held.
 */
#define OBJECT_HEADER_WORDS (SGEN_CLIENT_OBJECT_HEADER_SIZE / sizeof(gpointer))
#define LOW_TYPE_BITS 3
#define DESC_TYPE_MASK	((1 << LOW_TYPE_BITS) - 1)
#define MAX_RUNLEN_OBJECT_SIZE 0xFFFF
#define VECTOR_INFO_SHIFT 14
#define VECTOR_KIND_SHIFT 13
#define VECTOR_ELSIZE_SHIFT 3
#define VECTOR_BITMAP_SHIFT 16
#define VECTOR_BITMAP_SIZE (GC_BITS_PER_WORD - VECTOR_BITMAP_SHIFT)
#define BITMAP_NUM_BITS (GC_BITS_PER_WORD - LOW_TYPE_BITS)
#define MAX_ELEMENT_SIZE 0x3ff
#define VECTOR_SUBTYPE_PTRFREE (DESC_TYPE_V_PTRFREE << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_REFS    (DESC_TYPE_V_REFS << VECTOR_INFO_SHIFT)
#define VECTOR_SUBTYPE_BITMAP  (DESC_TYPE_V_BITMAP << VECTOR_INFO_SHIFT)

#define VECTOR_KIND_SZARRAY  (DESC_TYPE_V_SZARRAY << VECTOR_KIND_SHIFT)
#define VECTOR_KIND_ARRAY  (DESC_TYPE_V_ARRAY << VECTOR_KIND_SHIFT)

/*
 * Objects are aligned to 8 bytes boundaries.
 *
 * A descriptor is a pointer in GCVTable, so 32 or 64 bits of size.
 * The low 3 bits define the type of the descriptor. The other bits
 * depend on the type.
 *
 * It's important to be able to quickly identify two properties of classes from their
 * descriptors: whether they are small enough to live in the regular major heap (size <=
 * SGEN_MAX_SMALL_OBJ_SIZE), and whether they contain references.
 *
 * To that end we have three descriptor types that only apply to small classes: RUN_LENGTH,
 * BITMAP, and SMALL_PTRFREE.  We also have the type COMPLEX_PTRFREE, which applies to
 * classes that are either not small or of unknown size (those being strings and arrays).
 * The lowest two bits of the SMALL_PTRFREE and COMPLEX_PTRFREE tags are the same, so we can
 * quickly check for references.
 *
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
 *
 * We don't use 0 so that 0 isn't a valid GC descriptor.  No deep reason for this other than
 * to be able to identify a non-inited descriptor for debugging.
 */
enum {
	/* Keep in sync with `descriptor_types` in sgen-debug.c! */
	DESC_TYPE_RUN_LENGTH = 1,   /* 16 bits aligned byte size | 1-3 (offset, numptr) bytes tuples */
	DESC_TYPE_BITMAP = 2,	    /* | 29-61 bitmap bits */
	DESC_TYPE_SMALL_PTRFREE = 3,
	DESC_TYPE_MAX_SMALL_OBJ = 3,
	DESC_TYPE_COMPLEX = 4,      /* index for bitmap into complex_descriptors */
	DESC_TYPE_VECTOR = 5,       /* 10 bits element size | 1 bit kind | 2 bits desc | element desc */
	DESC_TYPE_COMPLEX_ARR = 6,  /* index for bitmap into complex_descriptors */
	DESC_TYPE_COMPLEX_PTRFREE = 7, /* Nothing, used to encode large ptr objects and strings. */
	DESC_TYPE_MAX = 7,

	DESC_TYPE_PTRFREE_MASK = 3,
	DESC_TYPE_PTRFREE_BITS = 3
};

/* values for array kind */
enum {
	DESC_TYPE_V_SZARRAY = 0, /*vector with no bounds data */
	DESC_TYPE_V_ARRAY = 1, /* array with bounds data */
};

/* subtypes for arrays and vectors */
enum {
	DESC_TYPE_V_PTRFREE = 0,/* there are no refs: keep first so it has a zero value  */
	DESC_TYPE_V_REFS,       /* all the array elements are refs */
	DESC_TYPE_V_RUN_LEN,    /* elements are run-length encoded as DESC_TYPE_RUN_LENGTH */
	DESC_TYPE_V_BITMAP      /* elements are as the bitmap in DESC_TYPE_SMALL_BITMAP */
};

#define SGEN_DESC_STRING	(DESC_TYPE_COMPLEX_PTRFREE | (1 << LOW_TYPE_BITS))

/* Root bitmap descriptors are simpler: the lower three bits describe the type
 * and we either have 30/62 bitmap bits or nibble-based run-length,
 * or a complex descriptor, or a user defined marker function.
 */
enum {
	ROOT_DESC_CONSERVATIVE, /* 0, so matches NULL value */
	ROOT_DESC_BITMAP,
	ROOT_DESC_RUN_LEN, 
	ROOT_DESC_COMPLEX,
	ROOT_DESC_VECTOR,
	ROOT_DESC_USER,
	ROOT_DESC_TYPE_MASK = 0x7,
	ROOT_DESC_TYPE_SHIFT = 3,
};

typedef void (*SgenUserMarkFunc)     (GCObject **addr, void *gc_data);
typedef void (*SgenUserReportRootFunc)     (void *addr, GCObject *obj, void *gc_data);
typedef void (*SgenUserRootMarkFunc) (void *addr, SgenUserMarkFunc mark_func, void *gc_data);

SgenDescriptor sgen_make_user_root_descriptor (SgenUserRootMarkFunc marker);

gsize* sgen_get_complex_descriptor (SgenDescriptor desc);
void* sgen_get_complex_descriptor_bitmap (SgenDescriptor desc);
SgenUserRootMarkFunc sgen_get_user_descriptor_func (SgenDescriptor desc);

void sgen_init_descriptors (void);

#ifdef HEAVY_STATISTICS
void sgen_descriptor_count_scanned_object (SgenDescriptor desc);
void sgen_descriptor_count_copied_object (SgenDescriptor desc);
#endif

static inline gboolean
sgen_gc_descr_has_references (SgenDescriptor desc)
{
	/* This covers SMALL_PTRFREE and COMPLEX_PTRFREE */
	if ((desc & DESC_TYPE_PTRFREE_MASK) == DESC_TYPE_PTRFREE_BITS)
		return FALSE;

	/*The array is ptr-free*/
	if ((desc & 0xC007) == (DESC_TYPE_VECTOR | VECTOR_SUBTYPE_PTRFREE))
		return FALSE;

	return TRUE;
}

#define SGEN_VTABLE_HAS_REFERENCES(vt)	(sgen_gc_descr_has_references (sgen_vtable_get_descriptor ((vt))))
#define SGEN_OBJECT_HAS_REFERENCES(o)	(SGEN_VTABLE_HAS_REFERENCES (SGEN_LOAD_VTABLE ((o))))

/* helper macros to scan and traverse objects, macros because we resue them in many functions */
#ifdef __GNUC__
#define PREFETCH_READ(addr)	__builtin_prefetch ((addr), 0, 1)
#define PREFETCH_WRITE(addr)	__builtin_prefetch ((addr), 1, 1)
#else
#define PREFETCH_READ(addr)
#define PREFETCH_WRITE(addr)
#endif

#if defined(__GNUC__) && SIZEOF_VOID_P==4
#define GNUC_BUILTIN_CTZ(bmap)	__builtin_ctz(bmap)
#elif defined(__GNUC__) && SIZEOF_VOID_P==8
#define GNUC_BUILTIN_CTZ(bmap)	__builtin_ctzl(bmap)
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
				HANDLE_PTR ((GCObject**)_objptr, (obj)); \
				_objptr++;	\
			};	\
		}	\
	} while (0)

/* a bitmap desc means that there are pointer references or we'd have
 * choosen run-length, instead: add an assert to check.
 */
#ifdef __GNUC__
#define OBJ_BITMAP_FOREACH_PTR(desc,obj)	do {		\
		/* there are pointers */			\
		void **_objptr = (void**)(obj);			\
		gsize _bmap = (desc) >> LOW_TYPE_BITS;		\
		_objptr += OBJECT_HEADER_WORDS;			\
		do {						\
			int _index = GNUC_BUILTIN_CTZ (_bmap);	\
			_objptr += _index;			\
			_bmap >>= (_index + 1);			\
			HANDLE_PTR ((GCObject**)_objptr, (obj));	\
			++_objptr;				\
		} while (_bmap);				\
	} while (0)
#else
#define OBJ_BITMAP_FOREACH_PTR(desc,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize _bmap = (desc) >> LOW_TYPE_BITS;	\
		_objptr += OBJECT_HEADER_WORDS;	\
		do {	\
			if ((_bmap & 1)) {	\
				HANDLE_PTR ((GCObject**)_objptr, (obj));	\
			}	\
			_bmap >>= 1;	\
			++_objptr;	\
		} while (_bmap);	\
	} while (0)
#endif

#define OBJ_COMPLEX_FOREACH_PTR(desc,obj)	do {	\
		/* there are pointers */	\
		void **_objptr = (void**)(obj);	\
		gsize *bitmap_data = sgen_get_complex_descriptor ((desc)); \
		gsize bwords = (*bitmap_data) - 1;	\
		void **start_run = _objptr;	\
		bitmap_data++;	\
		while (bwords-- > 0) {	\
			gsize _bmap = *bitmap_data++;	\
			_objptr = start_run;	\
			/*g_print ("bitmap: 0x%x/%d at %p\n", _bmap, bwords, _objptr);*/	\
			while (_bmap) {	\
				if ((_bmap & 1)) {	\
					HANDLE_PTR ((GCObject**)_objptr, (obj));	\
				}	\
				_bmap >>= 1;	\
				++_objptr;	\
			}	\
			start_run += GC_BITS_PER_WORD;	\
		}	\
	} while (0)

/* this one is untested */
#define OBJ_COMPLEX_ARR_FOREACH_PTR(desc,obj)	do {	\
		/* there are pointers */	\
		GCVTable vt = SGEN_LOAD_VTABLE (obj); \
		gsize *mbitmap_data = sgen_get_complex_descriptor ((desc)); \
		gsize mbwords = (*mbitmap_data++) - 1;	\
		gsize el_size = sgen_client_array_element_size (vt);	\
		char *e_start = sgen_client_array_data_start ((GCObject*)(obj));	\
		char *e_end = e_start + el_size * sgen_client_array_length ((GCObject*)(obj));	\
		while (e_start < e_end) {	\
			void **_objptr = (void**)e_start;	\
			gsize *bitmap_data = mbitmap_data;	\
			gsize bwords = mbwords;	\
			while (bwords-- > 0) {	\
				gsize _bmap = *bitmap_data++;	\
				void **start_run = _objptr;	\
				/*g_print ("bitmap: 0x%x\n", _bmap);*/	\
				while (_bmap) {	\
					if ((_bmap & 1)) {	\
						HANDLE_PTR ((GCObject**)_objptr, (obj));	\
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
				void **p = (void**)sgen_client_array_data_start ((GCObject*)(obj));	\
				void **end_refs = (void**)((char*)p + el_size * sgen_client_array_length ((GCObject*)(obj)));	\
				/* Note: this code can handle also arrays of struct with only references in them */	\
				while (p < end_refs) {	\
					HANDLE_PTR ((GCObject**)p, (obj));	\
					++p;	\
				}	\
			} else if (etype == DESC_TYPE_V_RUN_LEN << 14) {	\
				int offset = ((desc) >> 16) & 0xff;	\
				int num_refs = ((desc) >> 24) & 0xff;	\
				char *e_start = sgen_client_array_data_start ((GCObject*)(obj));	\
				char *e_end = e_start + el_size * sgen_client_array_length ((GCObject*)(obj));	\
				while (e_start < e_end) {	\
					void **p = (void**)e_start;	\
					int i;	\
					p += offset;	\
					for (i = 0; i < num_refs; ++i) {	\
						HANDLE_PTR ((GCObject**)p + i, (obj));	\
					}	\
					e_start += el_size;	\
				}	\
			} else if (etype == DESC_TYPE_V_BITMAP << 14) {	\
				char *e_start = sgen_client_array_data_start ((GCObject*)(obj));	\
				char *e_end = e_start + el_size * sgen_client_array_length ((GCObject*)(obj));	\
				while (e_start < e_end) {	\
					void **p = (void**)e_start;	\
					gsize _bmap = (desc) >> 16;	\
					/* Note: there is no object header here to skip */	\
					while (_bmap) {	\
						if ((_bmap & 1)) {	\
							HANDLE_PTR ((GCObject**)p, (obj));	\
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
