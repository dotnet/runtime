/*
 * iomap.c: IOMAP string profiler for Mono.
 *
 * Authors:
 *   Marek Habersack <mhabersack@novell.com>
 *
 * Copyright (c) 2009 Novell, Inc (http://novell.com)
 *
 * Note: this profiler is completely unsafe wrt handling managed objects,
 * don't use and don't copy code from here.
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include "config.h"

#include <string.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/metadata-internals.h>
#include <mono/metadata/class.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/image.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/loader.h>
#include <mono/utils/mono-os-mutex.h>

#define LOCATION_INDENT "        "
#define BACKTRACE_SIZE 64

typedef struct _MonoStackBacktraceInfo 
{
	MonoMethod *method;
	gint native_offset;
} MonoStackBacktraceInfo;

typedef struct 
{
	guint32 count;
	gchar *requestedName;
	gchar *actualName;
} MismatchedFilesStats;

typedef struct _SavedString
{
	MonoString *string;
	MonoDomain *domain;
	void *stack [BACKTRACE_SIZE];
	gint stack_entries;
	struct _SavedString *next;
} SavedString;

typedef struct _SavedStringFindInfo
{
	guint32 hash;
	size_t len;
} SavedStringFindInfo;

typedef struct _StringLocation
{
	gchar *hint;
	struct _StringLocation *next;
} StringLocation;

struct _MonoProfiler
{
	GHashTable *mismatched_files_hash;
	GHashTable *saved_strings_hash;
	GHashTable *string_locations_hash;
	gboolean may_have_locations;
};

typedef struct _StackWalkData
{
	MonoProfiler *prof;
	void **stack;
	int stack_size;
	int frame_count;
} StackWalkData;

static mono_mutex_t mismatched_files_section;
static gboolean runtime_initialized = FALSE;

static inline void append_report (GString **report, const gchar *format, ...);
static inline void print_report (const gchar *format, ...);
static inline guint32 do_calc_string_hash (guint32 hash, const gchar *str);
static inline guint32 calc_strings_hash (const gchar *str1, const gchar *str2, guint32 *str1hash);
static void print_mismatched_stats (MonoProfiler *prof);
static inline gchar *build_hint (SavedString *head);
static inline gchar *build_hint_from_stack (MonoDomain *domain, void **stack, gint stack_entries);
static inline void store_string_location (MonoProfiler *prof, const gchar *string, guint32 hash, size_t len);
static void mono_portability_remember_string (MonoProfiler *prof, MonoDomain *domain, MonoString *str);
void mono_profiler_init_iomap (const char *desc);

static void mismatched_stats_foreach_func (gpointer key, gpointer value, gpointer user_data)
{
	MismatchedFilesStats *stats = (MismatchedFilesStats*)value;
	StringLocation *location;
	MonoProfiler *prof = (MonoProfiler*)user_data;
	guint32 hash;
	gboolean bannerShown = FALSE;

	hash = do_calc_string_hash (0, stats->requestedName);
	fprintf (stdout,
		 "    Count: %u\n"
		 "Requested: %s\n"
		 "   Actual: %s\n",
		 stats->count, stats->requestedName, stats->actualName);

	if (!prof->may_have_locations) {
		fprintf (stdout, "\n");
		return;
	}

	location = (StringLocation *)g_hash_table_lookup (prof->string_locations_hash, &hash);
	while (location) {
		if (location->hint && strlen (location->hint) > 0) {
			if (!bannerShown) {
				fprintf (stdout, "Locations:\n");
				bannerShown = TRUE;
			}
			fprintf (stdout, "%s", location->hint);
		}
		location = location->next;
		if (location)
			fprintf (stdout, LOCATION_INDENT "--\n");
	}

	fprintf (stdout, "\n");
}

static void print_mismatched_stats (MonoProfiler *prof)
{
	if (!prof->mismatched_files_hash || g_hash_table_size (prof->mismatched_files_hash) == 0)
		return;

	prof->may_have_locations = g_hash_table_size (prof->string_locations_hash) > 0;

	fprintf (stdout, "\n-=-=-=-=-=-=-= MONO_IOMAP Stats -=-=-=-=-=-=-=\n");
	g_hash_table_foreach (prof->mismatched_files_hash, mismatched_stats_foreach_func, (gpointer)prof);
	fflush (stdout);
}

static guint mismatched_files_guint32_hash (gconstpointer key)
{
	if (!key)
		return 0;

	return *((guint32*)key);
}

static gboolean mismatched_files_guint32_equal (gconstpointer key1, gconstpointer key2)
{
	if (!key1 || !key2)
		return FALSE;

	return (gboolean)(*((guint32*)key1) == *((guint32*)key2));
}

static inline guint32 do_calc_string_hash (guint32 hash, const gchar *str)
{
	guint32 ret = hash;
	gchar *cc = (gchar*)str;
	gchar *end = (gchar*)(str + strlen (str) - 1);

	for (; cc < end; cc += 2) {
		ret = (ret << 5) - ret + *cc;
		ret = (ret << 5) - ret + cc [1];
	}
	end++;
	if (cc < end)
		ret = (ret << 5) - ret + *cc;

	return ret;
}

static inline guint32 calc_strings_hash (const gchar *str1, const gchar *str2, guint32 *str1hash)
{
	guint32 hash = do_calc_string_hash (0, str1);
	if (str1hash)
		*str1hash = hash;
	return do_calc_string_hash (hash, str2);
}

static inline void print_report (const gchar *format, ...)
{
	MonoError error;
	MonoClass *klass;
	MonoProperty *prop;
	MonoString *str;
	char *stack_trace;
	va_list ap;

	fprintf (stdout, "-=-=-=-=-=-=- MONO_IOMAP REPORT -=-=-=-=-=-=-\n");
	va_start (ap, format);
	vfprintf (stdout, format, ap);
	fprintf (stdout, "\n");
	va_end (ap);
	klass = mono_class_load_from_name (mono_get_corlib (), "System", "Environment");
	mono_class_init (klass);
	prop = mono_class_get_property_from_name (klass, "StackTrace");
	str = (MonoString*)mono_property_get_value_checked (prop, NULL, NULL, &error);
	mono_error_assert_ok (&error);
	stack_trace = mono_string_to_utf8_checked (str, &error);
	mono_error_assert_ok (&error);

	fprintf (stdout, "-= Stack Trace =-\n%s\n\n", stack_trace);
	g_free (stack_trace);
	fflush (stdout);
}

static inline void append_report (GString **report, const gchar *format, ...)
{
	va_list ap;
	if (!*report)
		*report = g_string_new ("");

	va_start (ap, format);
	g_string_append_vprintf (*report, format, ap);
	va_end (ap);
}

static gboolean saved_strings_find_func (gpointer key, gpointer value, gpointer user_data)
{
	MonoError error;
	SavedStringFindInfo *info = (SavedStringFindInfo*)user_data;
	SavedString *saved = (SavedString*)value;
	gchar *utf_str;
	guint32 hash;

	if (!info || !saved || mono_string_length (saved->string) != info->len)
		return FALSE;

	utf_str = mono_string_to_utf8_checked (saved->string, &error);
	mono_error_assert_ok (&error);
	hash = do_calc_string_hash (0, utf_str);
	g_free (utf_str);

	if (hash != info->hash)
		return FALSE;

	return TRUE;
}

static inline void store_string_location (MonoProfiler *prof, const gchar *string, guint32 hash, size_t len)
{
	StringLocation *location = (StringLocation *)g_hash_table_lookup (prof->string_locations_hash, &hash);
	SavedString *saved;
	SavedStringFindInfo info;
	guint32 *hashptr;

	if (location)
		return;

	info.hash = hash;
	info.len = len;

	/* Expensive but unavoidable... */
	saved = (SavedString*)g_hash_table_find (prof->saved_strings_hash, saved_strings_find_func, &info);
	hashptr = (guint32*)g_malloc (sizeof (guint32));
	*hashptr = hash;
	location = (StringLocation*)g_malloc0 (sizeof (location));

	g_hash_table_insert (prof->string_locations_hash, hashptr, location);
	if (!saved)
		return;

	g_hash_table_remove (prof->saved_strings_hash, saved->string);
	location->hint = build_hint (saved);
}

static gboolean ignore_frame (MonoMethod *method)
{
	MonoClass *klass = method->klass;

	if (method->wrapper_type != MONO_WRAPPER_NONE)
		return TRUE;

	/* Now ignore the assemblies we know shouldn't contain mixed-case names (only the most frequent cases) */
	if (klass->image ) {
		if (strcmp (klass->image->assembly_name, "mscorlib") == 0)
			return TRUE;
		else if (strcmp (klass->image->assembly_name, "System") == 0)
			return TRUE;
		else if (strncmp (klass->image->assembly_name, "Mono.", 5) == 0)
			return TRUE;
		else if (strncmp (klass->image->assembly_name, "System.", 7) == 0)
			return TRUE;
		else if (strcmp (klass->image->assembly_name, "PEAPI") == 0)
			return TRUE;
	}

	return FALSE;
}

static inline gchar *build_hint_from_stack (MonoDomain *domain, void **stack, gint stack_entries)
{
	gchar *hint;
	MonoMethod *method, *selectedMethod;
	MonoAssembly *assembly;
	MonoImage *image;
	MonoDebugSourceLocation *location;
	MonoStackBacktraceInfo *info;
	gboolean use_full_trace;
	char *methodName;
	gint i, native_offset, firstAvailable;

	selectedMethod = NULL;
	firstAvailable = -1;
	use_full_trace = FALSE;
	native_offset = -1;
	for (i = 0; i < stack_entries; i++) {
		info = (MonoStackBacktraceInfo*) stack [i];
		method = info ? info->method : NULL;

		if (!method || method->wrapper_type != MONO_WRAPPER_NONE)
			continue;

		if (firstAvailable == -1)
			firstAvailable = i;

		image = method->klass->image;
		assembly = image->assembly;

		if ((assembly && assembly->in_gac) || ignore_frame (method))
			continue;
		selectedMethod = method;
		native_offset = info->native_offset;
		break;
	}

	if (!selectedMethod) {
		/* All the frames were from assemblies installed in GAC. Find first frame that is
		 * not in the ignore list */
		for (i = 0; i < stack_entries; i++) {
			info = (MonoStackBacktraceInfo*) stack [i];
			method = info ? info->method : NULL;

			if (!method || ignore_frame (method))
				continue;
			selectedMethod = method;
			native_offset = info->native_offset;
			break;
		}

		if (!selectedMethod)
			use_full_trace = TRUE;
	}

	hint = NULL;
	if (use_full_trace) {
		GString *trace = g_string_new ("Full trace:\n");
		for (i = firstAvailable; i < stack_entries; i++) {
			info = (MonoStackBacktraceInfo*) stack [i];
			method = info ? info->method : NULL;
			if (!method || method->wrapper_type != MONO_WRAPPER_NONE)
				continue;

			location = mono_debug_lookup_source_location (method, info->native_offset, domain);
			methodName = mono_method_full_name (method, TRUE);

			if (location) {
				append_report (&trace, LOCATION_INDENT "%s in %s:%u\n", methodName, location->source_file, location->row);
				mono_debug_free_source_location (location);
			} else
				append_report (&trace, LOCATION_INDENT "%s\n", methodName);
			g_free (methodName);
		}

		if (trace) {
			if (trace->len)
				hint = g_string_free (trace, FALSE);
			else
				g_string_free (trace, TRUE);
		}
	} else {
		location = mono_debug_lookup_source_location (selectedMethod, native_offset, domain);
		methodName = mono_method_full_name (selectedMethod, TRUE);

		if (location) {
			hint = g_strdup_printf (LOCATION_INDENT "%s in %s:%u\n", methodName, location->source_file, location->row);
			mono_debug_free_source_location (location);
		} else
			hint = g_strdup_printf (LOCATION_INDENT "%s\n", methodName);
		g_free (methodName);
	}

	return hint;
}

static inline gchar *build_hint (SavedString *head)
{
	SavedString *current;
	gchar *tmp;
	GString *hint = NULL;

	current = head;
	while (current) {
		tmp = build_hint_from_stack (current->domain, current->stack, current->stack_entries);
		current = current->next;
		if (!tmp)
			continue;

		append_report (&hint, tmp);
	}

	if (hint) {
		if (hint->len)
			return g_string_free (hint, FALSE);
		else {
			g_string_free (hint, FALSE);
			return NULL;
		}
	}

	return NULL;
}

static gboolean stack_walk_func (MonoMethod *method, gint32 native_offset, gint32 il_offset, gboolean managed, gpointer data)
{
	StackWalkData *swdata = (StackWalkData*)data;
	MonoStackBacktraceInfo *info;

	if (swdata->frame_count >= swdata->stack_size)
		return TRUE;

	info = (MonoStackBacktraceInfo*)g_malloc (sizeof (*info));
	info->method = method;
	info->native_offset = native_offset;

	swdata->stack [swdata->frame_count++] = info;
	return FALSE;
}

static inline int mono_stack_backtrace (MonoProfiler *prof, MonoDomain *domain, void **stack, int size)
{
	StackWalkData data;

	data.prof = prof;
	data.stack = stack;
	data.stack_size = size;
	data.frame_count = 0;

	mono_stack_walk_no_il (stack_walk_func, (gpointer)&data);

	return data.frame_count;
}

static void mono_portability_remember_string (MonoProfiler *prof, MonoDomain *domain, MonoString *str)
{
	SavedString *head, *entry;

	if (!str || !domain || !runtime_initialized)
		return;

	entry = (SavedString*)g_malloc0 (sizeof (SavedString));
	entry->string = str;
	entry->domain = domain;
	entry->stack_entries = mono_stack_backtrace (prof, domain, entry->stack, BACKTRACE_SIZE);
	if (entry->stack_entries == 0) {
		g_free (entry);
		return;
	}

	mono_os_mutex_lock (&mismatched_files_section);
	head = (SavedString*)g_hash_table_lookup (prof->saved_strings_hash, (gpointer)str);
	if (head) {
		while (head->next)
			head = head->next;
		head->next = entry;
	} else
		g_hash_table_insert (prof->saved_strings_hash, (gpointer)str, (gpointer)entry);
	mono_os_mutex_unlock (&mismatched_files_section);
}

static MonoClass *string_class = NULL;

static void mono_portability_remember_alloc (MonoProfiler *prof, MonoObject *obj)
{
	if (mono_object_get_class (obj) != string_class)
		return;
	mono_portability_remember_string (prof, mono_object_get_domain (obj), (MonoString*)obj);
}

static void mono_portability_iomap_event (MonoProfiler *prof, const char *report, const char *pathname, const char *new_pathname)
{
	guint32 hash, pathnameHash;
	MismatchedFilesStats *stats;

	if (!runtime_initialized)
		return;

	mono_os_mutex_lock (&mismatched_files_section);
	hash = calc_strings_hash (pathname, new_pathname, &pathnameHash);
	stats = (MismatchedFilesStats*)g_hash_table_lookup (prof->mismatched_files_hash, &hash);
	if (stats == NULL) {
		guint32 *hashptr;

		stats = (MismatchedFilesStats*) g_malloc (sizeof (MismatchedFilesStats));
		stats->count = 1;
		stats->requestedName = g_strdup (pathname);
		stats->actualName = g_strdup (new_pathname);
		hashptr = (guint32*)g_malloc (sizeof (guint32));
		if (hashptr) {
			*hashptr = hash;
			g_hash_table_insert (prof->mismatched_files_hash, (gpointer)hashptr, stats);
		} else
			g_error ("Out of memory allocating integer pointer for mismatched files hash table.");

		store_string_location (prof, (const gchar*)stats->requestedName, pathnameHash, strlen (stats->requestedName));
		mono_os_mutex_unlock (&mismatched_files_section);

		print_report ("%s -     Found file path: '%s'\n", report, new_pathname);
	} else {
		mono_os_mutex_unlock (&mismatched_files_section);
		stats->count++;
	}
}

static void runtime_initialized_cb (MonoProfiler *prof)
{
	runtime_initialized = TRUE;
	string_class = mono_get_string_class ();
}

static void profiler_shutdown (MonoProfiler *prof)
{
	print_mismatched_stats (prof);
	mono_os_mutex_destroy (&mismatched_files_section);
}

void mono_profiler_init_iomap (const char *desc)
{
	MonoProfiler *prof = g_new0 (MonoProfiler, 1);

	mono_os_mutex_init (&mismatched_files_section);
	prof->mismatched_files_hash = g_hash_table_new (mismatched_files_guint32_hash, mismatched_files_guint32_equal);
	prof->saved_strings_hash = g_hash_table_new (NULL, NULL);
	prof->string_locations_hash = g_hash_table_new (mismatched_files_guint32_hash, mismatched_files_guint32_equal);

	MonoProfilerHandle handle = mono_profiler_create (prof);
	mono_profiler_set_runtime_shutdown_end_callback (handle, profiler_shutdown);
	mono_profiler_set_runtime_initialized_callback (handle, runtime_initialized_cb);
	mono_profiler_set_iomap_report_callback (handle, mono_portability_iomap_event);
	mono_profiler_enable_allocations ();
	mono_profiler_set_gc_allocation_callback (handle, mono_portability_remember_alloc);
}
