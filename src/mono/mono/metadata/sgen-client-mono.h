/*
 * sgen-client-mono.h: Mono's client definitions for SGen.
 *
 * Copyright (C) 2014 Xamarin Inc
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
 */

enum {
	INTERNAL_MEM_EPHEMERON_LINK = INTERNAL_MEM_FIRST_CLIENT,
	INTERNAL_MEM_MAX
};

#ifdef XDOMAIN_CHECKS_IN_WBARRIER
extern gboolean sgen_mono_xdomain_checks;

#define sgen_client_wbarrier_generic_nostore_check(ptr) do {		\
		/* FIXME: ptr_in_heap must be called with the GC lock held */ \
		if (sgen_mono_xdomain_checks && *(MonoObject**)ptr && ptr_in_heap (ptr)) { \
			char *start = find_object_for_ptr (ptr);	\
			MonoObject *value = *(MonoObject**)ptr;		\
			LOCK_GC;					\
			SGEN_ASSERT (0, start, "Write barrier outside an object?"); \
			if (start) {					\
				MonoObject *obj = (MonoObject*)start;	\
				if (obj->vtable->domain != value->vtable->domain) \
					SGEN_ASSERT (0, is_xdomain_ref_allowed (ptr, start, obj->vtable->domain), "Cross-domain ref not allowed"); \
			}						\
			UNLOCK_GC;					\
		}							\
	} while (0)
#else
#define sgen_client_wbarrier_generic_nostore_check(ptr)
#endif
