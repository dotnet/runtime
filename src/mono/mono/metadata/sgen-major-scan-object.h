extern long long stat_scan_object_called_major;

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
