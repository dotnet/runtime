extern long long stat_scan_object_called_nursery;
extern long long stat_scan_object_called_major;

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {	\
		void *__old = *(ptr);	\
		void *__copy;		\
		if (__old) {	\
			copy_object ((ptr), queue);	\
			__copy = *(ptr);	\
			DEBUG (9, if (__old != __copy) fprintf (gc_debug_file, "Overwrote field at %p with %p (was: %p)\n", (ptr), *(ptr), __old));	\
			if (G_UNLIKELY (ptr_in_nursery (__copy) && !ptr_in_nursery ((ptr)))) \
				mono_sgen_add_to_global_remset ((ptr));	\
		}	\
	} while (0)

/*
 * Scan the object pointed to by @start for references to
 * other objects between @from_start and @from_end and copy
 * them to the gray_objects area.
 */
static void
minor_scan_object (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_nursery);
}

/*
 * scan_vtype:
 *
 * Scan the valuetype pointed to by START, described by DESC for references to
 * other objects between @from_start and @from_end and copy them to the gray_objects area.
 * Returns a pointer to the end of the object.
 */
static char*
minor_scan_vtype (char *start, mword desc, char* from_start, char* from_end, SgenGrayQueue *queue)
{
	size_t skip_size;

	/* The descriptors include info about the MonoObject header as well */
	start -= sizeof (MonoObject);

	switch (desc & 0x7) {
	case DESC_TYPE_RUN_LENGTH:
		OBJ_RUN_LEN_FOREACH_PTR (desc,start);
		OBJ_RUN_LEN_SIZE (skip_size, desc, start);
		g_assert (skip_size);
		return start + skip_size;
	case DESC_TYPE_SMALL_BITMAP:
		OBJ_BITMAP_FOREACH_PTR (desc,start);
		OBJ_BITMAP_SIZE (skip_size, desc, start);
		return start + skip_size;
	case DESC_TYPE_LARGE_BITMAP:
	case DESC_TYPE_COMPLEX:
		// FIXME:
		g_assert_not_reached ();
		break;
	default:
		// The other descriptors can't happen with vtypes
		g_assert_not_reached ();
		break;
	}
	return NULL;
}

#undef HANDLE_PTR
#define HANDLE_PTR(ptr,obj)	do {					\
		void *__old = *(ptr);					\
		void *__copy;						\
		if (__old) {						\
			major_copy_or_mark_object ((ptr), queue);	\
			__copy = *(ptr);				\
			DEBUG (9, if (__old != __copy) mono_sgen_debug_printf (9, "Overwrote field at %p with %p (was: %p)\n", (ptr), *(ptr), __old)); \
			if (G_UNLIKELY (ptr_in_nursery (__copy) && !ptr_in_nursery ((ptr)))) \
				mono_sgen_add_to_global_remset ((ptr));	\
		}							\
	} while (0)

static void
major_scan_object (char *start, SgenGrayQueue *queue)
{
#include "sgen-scan-object.h"

	HEAVY_STAT (++stat_scan_object_called_major);
}

#define FILL_COLLECTOR_SCAN_OBJECT(collector)	do {			\
		(collector)->major_scan_object = major_scan_object;	\
		(collector)->minor_scan_object = minor_scan_object;	\
		(collector)->minor_scan_vtype = minor_scan_vtype;	\
	} while (0)
