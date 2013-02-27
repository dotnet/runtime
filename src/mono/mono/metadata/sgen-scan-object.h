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

{
#ifndef SCAN_OBJECT_NOVTABLE
	GCVTable *vt;
	mword desc;

	vt = (GCVTable*)SGEN_LOAD_VTABLE (start);
	//type = vt->desc & 0x7;

	/* gcc should be smart enough to remove the bounds check, but it isn't:( */
	desc = vt->desc;

#if defined(SGEN_BINARY_PROTOCOL) && defined(SCAN_OBJECT_PROTOCOL)
	binary_protocol_scan_begin (start, vt, sgen_safe_object_get_size ((MonoObject*)start));
#endif
#else
#if defined(SGEN_BINARY_PROTOCOL) && defined(SCAN_OBJECT_PROTOCOL)
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
