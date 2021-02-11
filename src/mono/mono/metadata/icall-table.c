/**
 * \file
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Paolo Molaro (lupus@ximian.com)
 *	 Patrik Torstensson (patrik.torstensson@labs2.com)
 *   Marek Safar (marek.safar@gmail.com)
 *   Aleksey Kliger (aleksey@xamarin.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011-2015 Xamarin Inc (http://www.xamarin.com).
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <stdarg.h>
#include <string.h>
#include <ctype.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#if defined (HAVE_WCHAR_H)
#include <wchar.h>
#endif

#include <mono/utils/mono-publib.h>
#include <mono/utils/bsearch.h>
#include <mono/metadata/icalls.h>
#include "handle-decl.h"
#include "icall-decl.h"

// These definitions are used for multiple includes of icall-def.h and eventually undefined.
#define NOHANDLES(inner) inner
#define HANDLES(id, name, func, ...)	ICALL (id, name, func ## _raw)
#define HANDLES_REUSE_WRAPPER		HANDLES
#define MONO_HANDLE_REGISTER_ICALL(...) /* nothing  */

//#define TEST_ICALL_SYMBOL_MAP 1

// Generate Icall_ constants
enum {
#define ICALL_TYPE(id,name,first)	/* nothing */
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) Icall_ ## id,
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
	Icall_last
};

enum {
#define ICALL_TYPE(id,name,first) Icall_type_ ## id,
#define ICALL(id,name,func) 		/* nothing */
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
	Icall_type_num
};

typedef struct {
	guint16 first_icall;
} IcallTypeDesc;

static const IcallTypeDesc icall_type_descs [] = {
#define ICALL_TYPE(id,name,firstic) {(Icall_ ## firstic)},
#define ICALL(id,name,func) 		/* nothing */
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
	{Icall_last}
};

#define icall_desc_num_icalls(desc) ((desc) [1].first_icall - (desc) [0].first_icall)

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.

#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line

static const struct msgstrtn_t {
#define ICALL_TYPE(id,name,first) char MSGSTRFIELD(__LINE__) [sizeof (name)];
#define ICALL(id,name,func) 		/* nothing */
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
} icall_type_names_str = {
#define ICALL_TYPE(id,name,first) (name),
#define ICALL(id,name,func)
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
};

static const guint16 icall_type_names_idx [] = {
#define ICALL_TYPE(id,name,first) (offsetof (struct msgstrtn_t, MSGSTRFIELD(__LINE__))),
#define ICALL(id,name,func) 		/* nothing */
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
};

#define icall_type_name_get(id) ((const char*)&icall_type_names_str + icall_type_names_idx [(id)])

static const struct msgstr_t {
#define ICALL_TYPE(id,name,first)	/* nothing */
#define ICALL(id,name,func) char MSGSTRFIELD(__LINE__) [sizeof (name)];
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
} icall_names_str = {
#define ICALL_TYPE(id,name,first)
#define ICALL(id,name,func) (name),
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
};

static const guint16 icall_names_idx [] = {
#define ICALL_TYPE(id,name,first)	/* nothing */
#define ICALL(id,name,func) (offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__))),
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
};

#define icall_name_get(id) ((const char*)&icall_names_str + icall_names_idx [(id)])

typedef struct MonoICallFunction {
	gpointer func;
#if ENABLE_ICALL_SYMBOL_MAP || TEST_ICALL_SYMBOL_MAP
	const char *name;
#endif
} MonoICallFunction;

static const MonoICallFunction icall_functions [] = {
#define ICALL_TYPE(id,name,first)	/* nothing */
#if ENABLE_ICALL_SYMBOL_MAP || TEST_ICALL_SYMBOL_MAP
#define ICALL(id, name, func) { (gpointer)func, #func },
#else
#define ICALL(id, name, func) { (gpointer)func },
#endif
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
	{ NULL },
};

#undef HANDLES
#undef NOHANDLES

static const guchar icall_uses_handles [] = {
#define ICALL_TYPE(id,name,first)	/* nothing */
#define ICALL(id,name,func) 0,
#define HANDLES(...) 1,
#define NOHANDLES(inner) 0,
#include "metadata/icall-def.h"
#undef ICALL_TYPE
#undef ICALL
};

#undef HANDLES
#undef HANDLES_REUSE_WRAPPER
#undef NOHANDLES
#undef MONO_HANDLE_REGISTER_ICALL

static int
compare_method_imap (const void *key, const void *elem)
{
	const char* method_name = (const char*)&icall_names_str + (*(guint16*)elem);
	return strcmp ((const char*)key, method_name);
}

static gssize
find_slot_icall (const IcallTypeDesc *imap, const char *name)
{
	const guint16 *nameslot = (const guint16 *)mono_binary_search (name, icall_names_idx + imap->first_icall, icall_desc_num_icalls (imap), sizeof (icall_names_idx [0]), compare_method_imap);
	if (!nameslot)
		return -1;
	return (nameslot - &icall_names_idx [0]);
}

static gboolean
find_uses_handles_icall (const IcallTypeDesc *imap, const char *name)
{
	const gssize slotnum = find_slot_icall (imap, name);
	if (slotnum == -1)
		return FALSE;
	return (gboolean)icall_uses_handles [slotnum];
}

static gpointer
find_method_icall (const IcallTypeDesc *imap, const char *name)
{
	const gssize slotnum = find_slot_icall (imap, name);
	if (slotnum == -1)
		return NULL;
	return icall_functions [slotnum].func;
}

static int
compare_class_imap (const void *key, const void *elem)
{
	const char* class_name = (const char*)&icall_type_names_str + (*(guint16*)elem);
	return strcmp ((const char*)key, class_name);
}

static const IcallTypeDesc*
find_class_icalls (const char *name)
{
	const guint16 *nameslot = (const guint16 *)mono_binary_search (name, icall_type_names_idx, Icall_type_num, sizeof (icall_type_names_idx [0]), compare_class_imap);
	if (!nameslot)
		return NULL;
	return &icall_type_descs [nameslot - &icall_type_names_idx [0]];
}

static gpointer
icall_table_lookup (MonoMethod *method, char *classname, char *methodname, char *sigstart, gboolean *uses_handles)
{
	const IcallTypeDesc *imap = NULL;
	gpointer res;

	imap = find_class_icalls (classname);

	/* it wasn't found in the static call tables */
	if (!imap) {
		if (uses_handles)
			*uses_handles = FALSE;
		return NULL;
	}
	res = find_method_icall (imap, methodname);
	if (res) {
		if (uses_handles)
			*uses_handles = find_uses_handles_icall (imap, methodname);
		return res;
	}
	/* try _with_ signature */
	*sigstart = '(';
	res = find_method_icall (imap, methodname);
	if (res) {
		if (uses_handles)
			*uses_handles = find_uses_handles_icall (imap, methodname);
		return res;
	}
	return NULL;
}

#if ENABLE_ICALL_SYMBOL_MAP || TEST_ICALL_SYMBOL_MAP

static
int
#if _WIN32
__cdecl
#endif
mono_qsort_icall_function_compare_indirect (gconstpointer va, gconstpointer vb)
{
	const gpointer a = icall_functions [*(const guint16*)va].func;
	const gpointer b = icall_functions [*(const guint16*)vb].func;
	return (a < b) ? -1 : (a > b) ? 1 : 0;
}

static
int
#if _WIN32
__cdecl
#endif
mono_bsearch_icall_function_compare_indirect (gconstpointer a, gconstpointer vb)
{
	const gpointer b = icall_functions [*(const guint16*)vb].func;
	return (a < b) ? -1 : (a > b) ? 1 : 0;
}

#endif // ENABLE_ICALL_SYMBOL_MAP || TEST_ICALL_SYMBOL_MAP

#if !TEST_ICALL_SYMBOL_MAP
static
#endif
const char*
mono_lookup_icall_symbol_internal (gpointer func)
{
#if ENABLE_ICALL_SYMBOL_MAP || TEST_ICALL_SYMBOL_MAP
	typedef guint16 T;
	const gsize N = G_N_ELEMENTS (icall_functions) - 1; // skip terminal null element
	g_static_assert (N <= 0xFFFF); // If this fails, change T to guint32
	static T *static_functions_sorted;

	if (!func)
		return NULL;

	if (!static_functions_sorted) {

		T *functions_sorted = g_new (T, N);

		// Initialize with identity mapping. One line is easier to step over.
		for (T i = 0; i < N; ++i) functions_sorted [i] = i;

		mono_qsort (functions_sorted, N, sizeof (T), mono_qsort_icall_function_compare_indirect);

		gpointer old = mono_atomic_cas_ptr ((gpointer*)&static_functions_sorted, functions_sorted, NULL);
		if (old)
			g_free (functions_sorted);
	}

	T const * const slot = (const T*)bsearch (func, static_functions_sorted, N, sizeof (T), mono_bsearch_icall_function_compare_indirect);
	if (!slot)
		return NULL;
	return icall_functions [*slot].name;
#else
	fprintf (stderr, "icall symbol maps not enabled, pass --enable-icall-symbol-map to configure.\n");
	g_assert_not_reached ();
	return NULL;
#endif
}

void
mono_icall_table_init (void)
{
	int i = 0;

	/* check that tables are sorted: disable in release */
	if (TRUE) {
		int j;
		const char *prev_class = NULL;
		const char *prev_method;
		
		for (i = 0; i < Icall_type_num; ++i) {
			const IcallTypeDesc *desc;
			int num_icalls;
			prev_method = NULL;
			if (prev_class && strcmp (prev_class, icall_type_name_get (i)) >= 0)
				g_print ("class %s should come before class %s\n", icall_type_name_get (i), prev_class);
			prev_class = icall_type_name_get (i);
			desc = &icall_type_descs [i];
			num_icalls = icall_desc_num_icalls (desc);
			/*g_print ("class %s has %d icalls starting at %d\n", prev_class, num_icalls, desc->first_icall);*/
			for (j = 0; j < num_icalls; ++j) {
				const char *methodn = icall_name_get (desc->first_icall + j);
				if (prev_method && strcmp (prev_method, methodn) >= 0)
					g_print ("method %s should come before method %s\n", methodn, prev_method);
				prev_method = methodn;
			}
		}
	}

	static const MonoIcallTableCallbacks mono_icall_table_callbacks =
	{
		MONO_ICALL_TABLE_CALLBACKS_VERSION,
		icall_table_lookup,
		mono_lookup_icall_symbol_internal,
	};
	mono_install_icall_table_callbacks (&mono_icall_table_callbacks);
}
