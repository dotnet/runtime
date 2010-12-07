/*
 * Copyright 2001-2003 Ximian, Inc
 * Copyright 2003-2010 Novell, Inc.
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
 * SCAN_OBJECT_ACTION - is invoked after an object has been scanned.
 * The object's start is "start", its length in bytes (including
 * padding at the end) is "skip_size".  "desc" is the object's GC
 * descriptor.  The action can use the macro
 * "SCAN" to scan the object.
 */

#ifndef SCAN_OBJECT_ACTION
#define SCAN_OBJECT_ACTION
#endif

{
	GCVTable *vt;
	mword desc;

	vt = (GCVTable*)SGEN_LOAD_VTABLE (start);
	//type = vt->desc & 0x7;

	/* gcc should be smart enough to remove the bounds check, but it isn't:( */
	desc = vt->desc;
	switch (desc & 0x7) {
	case DESC_TYPE_RUN_LENGTH:
#define SCAN OBJ_RUN_LEN_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
		SCAN_OBJECT_ACTION;
#undef SCAN
		break;
	case DESC_TYPE_ARRAY:
	case DESC_TYPE_VECTOR:
#define SCAN OBJ_VECTOR_FOREACH_PTR (vt, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
		SCAN_OBJECT_ACTION;
#undef SCAN
		break;
	case DESC_TYPE_SMALL_BITMAP:
#define SCAN OBJ_BITMAP_FOREACH_PTR (desc, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
		SCAN_OBJECT_ACTION;
#undef SCAN
		break;
	case DESC_TYPE_LARGE_BITMAP:
#define SCAN OBJ_LARGE_BITMAP_FOREACH_PTR (vt,start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
		SCAN_OBJECT_ACTION;
#undef SCAN
		break;
	case DESC_TYPE_COMPLEX:
		/* this is a complex object */
#define SCAN OBJ_COMPLEX_FOREACH_PTR (vt, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
		SCAN_OBJECT_ACTION;
#undef SCAN
		break;
	case DESC_TYPE_COMPLEX_ARR:
		/* this is an array of complex structs */
#define SCAN OBJ_COMPLEX_ARR_FOREACH_PTR (vt, start)
#ifndef SCAN_OBJECT_NOSCAN
		SCAN;
#endif
		SCAN_OBJECT_ACTION;
#undef SCAN
		break;
	default:
		g_assert_not_reached ();
	}
}

#undef SCAN_OBJECT_NOSCAN
#undef SCAN_OBJECT_ACTION
