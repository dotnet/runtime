/*
 * Scans one object, using the OBJ_XXX macros.  The start of the
 * object must be given in the variable "char* start".  Afterwards,
 * "start" will point to the start of the next object.
 *
 * Modifiers (automatically undefined):
 *
 * SCAN_OBJECT_NOSCAN - if defined, don't actually scan the object,
 * i.e. don't invoke the OBJ_XXX macros.
 *
 * SCAN_OBJECT_ACTION - is invoked after an object has been scanned.
 * The object's start is "start", its length in bytes (including
 * padding at the end) is "skip_size".  "desc" is the object's GC
 * descriptor.
 */

#ifndef SCAN_OBJECT_ACTION
#define SCAN_OBJECT_ACTION
#endif

{
	GCVTable *vt;
	size_t skip_size;
	mword desc;

	vt = (GCVTable*)LOAD_VTABLE (start);
	//type = vt->desc & 0x7;

	/* gcc should be smart enough to remove the bounds check, but it isn't:( */
	desc = vt->desc;
	switch (desc & 0x7) {
	case DESC_TYPE_STRING:
		STRING_SIZE (skip_size, start);
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	case DESC_TYPE_RUN_LENGTH:
		OBJ_RUN_LEN_SIZE (skip_size, desc, start);
		g_assert (skip_size);
#ifndef SCAN_OBJECT_NOSCAN
		OBJ_RUN_LEN_FOREACH_PTR (desc, start);
#endif
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	case DESC_TYPE_ARRAY:
	case DESC_TYPE_VECTOR:
		skip_size = safe_object_get_size ((MonoObject*)start);
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
#ifndef SCAN_OBJECT_NOSCAN
		OBJ_VECTOR_FOREACH_PTR (vt, start);
#endif
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	case DESC_TYPE_SMALL_BITMAP:
		OBJ_BITMAP_SIZE (skip_size, desc, start);
		g_assert (skip_size);
#ifndef SCAN_OBJECT_NOSCAN
		OBJ_BITMAP_FOREACH_PTR (desc, start);
#endif
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	case DESC_TYPE_LARGE_BITMAP:
		skip_size = safe_object_get_size ((MonoObject*)start);
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
#ifndef SCAN_OBJECT_NOSCAN
		OBJ_LARGE_BITMAP_FOREACH_PTR (vt,start);
#endif
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	case DESC_TYPE_COMPLEX:
		/* this is a complex object */
		skip_size = safe_object_get_size ((MonoObject*)start);
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
#ifndef SCAN_OBJECT_NOSCAN
		OBJ_COMPLEX_FOREACH_PTR (vt, start);
#endif
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	case DESC_TYPE_COMPLEX_ARR:
		/* this is an array of complex structs */
		skip_size = safe_object_get_size ((MonoObject*)start);
		skip_size += (ALLOC_ALIGN - 1);
		skip_size &= ~(ALLOC_ALIGN - 1);
#ifndef SCAN_OBJECT_NOSCAN
		OBJ_COMPLEX_ARR_FOREACH_PTR (vt, start);
#endif
		SCAN_OBJECT_ACTION;
		start += skip_size;
		break;
	default:
		g_assert_not_reached ();
	}
}

#undef SCAN_OBJECT_NOSCAN
#undef SCAN_OBJECT_ACTION
