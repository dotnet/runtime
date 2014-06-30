/*
 * sgen-scan-object.h: Generic object scan.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2013 Xamarin Inc
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License 2.0 as published by the Free Software Foundation;
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License 2.0 along with this library; if not, write to the Free
 * Software Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 *
 *
 * Scans one object, using the OBJ_XXX macros.  The start of the
 * object must be given in the variable "char* start".  Afterwards,
 * "start" will point to the start of the next object, if the scanned
 * object contained references.  If not, the value of "start" should
 * be considered undefined after executing this code.
 *
 * Modifiers (automatically undefined):
 *
 * SCAN_OBJECT_NOSCAN - if defined, don't actually scan the object,
 * i.e. don't invoke the OBJ_XXX macros.
 *
 * SCAN_OBJECT_NOVTABLE - desc is provided by the includer, instead of
 * vt.  Complex arrays cannot not be scanned.
 *
 * SCAN_OBJECT_PROTOCOL - if defined, binary protocol the scan.
 * Should only be used for scanning that's done for the actual
 * collection, not for debugging scans.
 */

 /* code using these macros must define a HANDLE_PTR(ptr) macro that does the work */
#define OBJ_RUN_LEN_FOREACH_PTR(desc,obj)	do {	\
		if ((desc) & 0xffff0000) {	\
			/* there are pointers */	\
			void **_objptr_end;	\
			void **_objptr = (void**)(obj);	\
			_objptr += ((desc) >> 16) & 0xff;	\
			_objptr_end = _objptr + (((desc) >> 24) & 0xff);	\
			HANDLE_PTR (_objptr, (obj)); \
			_objptr ++; \
			while (_objptr < _objptr_end) {	\
				HANDLE_PTR (_objptr, (obj));	\
				_objptr++;	\
			}	\
		}	\
	} while (0)

#if defined(__GNUC__)
#define OBJ_BITMAP_FOREACH_PTR(desc,obj)       do {    \
		/* there are pointers */        \
		void **_objptr = (void**)(obj); \
		gsize _bmap = (desc) >> 16;     \
		_objptr += OBJECT_HEADER_WORDS; \
		{ \
			int _index = GNUC_BUILTIN_CTZ (_bmap);		\
			_objptr += _index; \
			_bmap >>= (_index + 1);				\
			HANDLE_PTR (_objptr, (obj));		\
			_objptr ++;							\
			} \
		while (_bmap) { \
			int _index = GNUC_BUILTIN_CTZ (_bmap);		\
			_objptr += _index; \
			_bmap >>= (_index + 1);				\
			HANDLE_PTR (_objptr, (obj));		\
			_objptr ++;							\
		}										\
	} while (0)
#else
#define OBJ_BITMAP_FOREACH_PTR(desc,obj)       do {    \
		/* there are pointers */        \
		void **_objptr = (void**)(obj); \
		gsize _bmap = (desc) >> 16;     \
		_objptr += OBJECT_HEADER_WORDS; \
		while (_bmap) {						   \
			if ((_bmap & 1)) {								   \
				HANDLE_PTR (_objptr, (obj));				   \
			}												   \
			_bmap >>= 1;									   \
			++_objptr;										   \
		}													   \
	} while (0)
#endif

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
				void **p = 	(void**)((char*)(obj) + G_STRUCT_OFFSET (MonoArray, vector)); \
				void **end_refs = (void**)((char*)(start) + el_size * mono_array_length_fast ((MonoArray*)(obj)));	\
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

{
#ifndef SCAN_OBJECT_NOVTABLE
	GCVTable *vt;
	mword desc;

	vt = (GCVTable*)SGEN_LOAD_VTABLE (start);
	//type = vt->desc & 0x7;

	/* gcc should be smart enough to remove the bounds check, but it isn't:( */
	desc = vt->desc;

#if defined(SGEN_HEAVY_BINARY_PROTOCOL) && defined(SCAN_OBJECT_PROTOCOL)
	binary_protocol_scan_begin (start, vt, sgen_safe_object_get_size ((MonoObject*)start));
#endif
#else
#if defined(SGEN_HEAVY_BINARY_PROTOCOL) && defined(SCAN_OBJECT_PROTOCOL)
	binary_protocol_scan_vtype_begin (start + sizeof (MonoObject), size);
#endif
#endif
	switch (desc & 0x7) {
	case DESC_TYPE_RUN_LENGTH:
#define SCAN OBJ_RUN_LEN_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_SMALL_BITMAP:
#define SCAN OBJ_BITMAP_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_VECTOR:
#define SCAN OBJ_VECTOR_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_LARGE_BITMAP:
#define SCAN OBJ_LARGE_BITMAP_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_COMPLEX:
		/* this is a complex object */
#define SCAN OBJ_COMPLEX_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
#ifndef SCAN_OBJECT_NOVTABLE
	case DESC_TYPE_COMPLEX_ARR:
		/* this is an array of complex structs */
#define SCAN OBJ_COMPLEX_ARR_FOREACH_PTR (vt, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
#endif
	case DESC_TYPE_COMPLEX_PTRFREE:
		/*Nothing to do*/
		break;
	default:
		g_assert_not_reached ();
	}
}

#undef SCAN_OBJECT_NOSCAN
#undef SCAN_OBJECT_NOVTABLE
#undef SCAN_OBJECT_PROTOCOL

#undef OBJ_RUN_LEN_FOREACH_PTR
#undef OBJ_BITMAP_FOREACH_PTR
#undef OBJ_LARGE_BITMAP_FOREACH_PTR
#undef OBJ_COMPLEX_FOREACH_PTR
#undef OBJ_COMPLEX_ARR_FOREACH_PTR
#undef OBJ_VECTOR_FOREACH_PTR