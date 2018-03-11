/**
 * \file
 * Generic object scan.
 *
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
 * Copyright (C) 2013 Xamarin Inc
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 *
 *
 * Scans one object, using the OBJ_XXX macros.  The start of the
 * object must be given in the variable "char* start".  Afterwards,
 * "start" will point to the start of the next object, if the scanned
 * object contained references.  If not, the value of "start" should
 * be considered undefined after executing this code.  The object's
 * GC descriptor must be in the variable "mword desc".
 *
 * The macro `HANDLE_PTR` will be invoked for every reference encountered while scanning the
 * object.  It is called with two parameters: The pointer to the reference (not the
 * reference itself!) as well as the pointer to the scanned object.
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
#if defined(SGEN_HEAVY_BINARY_PROTOCOL) && defined(SCAN_OBJECT_PROTOCOL)
	sgen_binary_protocol_scan_begin ((GCObject*)start, SGEN_LOAD_VTABLE ((GCObject*)start), sgen_safe_object_get_size ((GCObject*)start));
#endif
#else
#if defined(SGEN_HEAVY_BINARY_PROTOCOL) && defined(SCAN_OBJECT_PROTOCOL)
	sgen_binary_protocol_scan_vtype_begin (start + SGEN_CLIENT_OBJECT_HEADER_SIZE, size);
#endif
#endif
	switch (desc & DESC_TYPE_MASK) {
	case DESC_TYPE_RUN_LENGTH:
#define SCAN OBJ_RUN_LEN_FOREACH_PTR (desc, ((GCObject*)start))
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_VECTOR:
#define SCAN OBJ_VECTOR_FOREACH_PTR (desc, ((GCObject*)start))
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_BITMAP:
#define SCAN OBJ_BITMAP_FOREACH_PTR (desc, ((GCObject*)start))
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
	case DESC_TYPE_COMPLEX:
		/* this is a complex object */
#define SCAN OBJ_COMPLEX_FOREACH_PTR (desc, ((GCObject*)start))
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
#ifndef SCAN_OBJECT_NOVTABLE
	case DESC_TYPE_COMPLEX_ARR:
		/* this is an array of complex structs */
#define SCAN OBJ_COMPLEX_ARR_FOREACH_PTR (desc, ((GCObject*)start))
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
#undef SCAN
		break;
#endif
	case DESC_TYPE_SMALL_PTRFREE:
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
