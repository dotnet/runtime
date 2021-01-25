/*
 * mprof-report.c: mprof-report program source: decode and analyze the log profiler data
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Alex Rønne Petersen (alexrp@xamarin.com)
 *
 * Copyright 2010 Novell, Inc (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include "log.h"
#include <string.h>
#include <assert.h>
#include <stdio.h>
#include <time.h>
#if !defined(__APPLE__) && !defined(__FreeBSD__) && !defined(__OpenBSD__)
#include <malloc.h>
#endif
#ifndef HOST_WIN32
#include <unistd.h>
#endif
#include <stdlib.h>
#if defined (HAVE_SYS_ZLIB)
#include <zlib.h>
#endif
#include <glib.h>
#include <mono/metadata/profiler.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-counters.h>

#define HASH_SIZE 9371
#define SMALL_HASH_SIZE 31

/* Version < 14 root type enum */
typedef enum {
	/* Upper 2 bytes. */
	MONO_PROFILER_GC_ROOT_PINNING = 1 << 8,
	MONO_PROFILER_GC_ROOT_WEAKREF = 2 << 8,
	MONO_PROFILER_GC_ROOT_INTERIOR = 4 << 8,

	/* Lower 2 bytes (flags). */
	MONO_PROFILER_GC_ROOT_STACK = 1 << 0,
	MONO_PROFILER_GC_ROOT_FINALIZER = 1 << 1,
	MONO_PROFILER_GC_ROOT_HANDLE = 1 << 2,
	MONO_PROFILER_GC_ROOT_OTHER = 1 << 3,
	MONO_PROFILER_GC_ROOT_MISC = 1 << 4,

	MONO_PROFILER_GC_ROOT_TYPEMASK = 0xff,
} MonoProfilerGCRootType;

static int debug = 0;
static int collect_traces = 0;
static int show_traces = 0;
static int trace_max = 6;
static int verbose = 0;
static uintptr_t *tracked_objects = 0;
static int num_tracked_objects = 0;
static uintptr_t thread_filter = 0;
static uint64_t find_size = 0;
static const char* find_name = NULL;
static uint64_t time_from = 0;
static uint64_t time_to = 0xffffffffffffffffULL;
static int use_time_filter = 0;
static uint64_t startup_time = 0;
static FILE* outfile = NULL;

static int32_t
read_int16 (unsigned char *p)
{
	int32_t value = *p++;
	value |= (*p++) << 8;
	return value;
}

static int32_t
read_int32 (unsigned char *p)
{
	int32_t value = *p++;
	value |= (*p++) << 8;
	value |= (*p++) << 16;
	value |= (uint32_t)(*p++) << 24;
	return value;
}

static int64_t
read_int64 (unsigned char *p)
{
	uint64_t value = *p++;
	value |= (*p++) << 8;
	value |= (*p++) << 16;
	value |= (uint64_t)(*p++) << 24;
	value |= (uint64_t)(*p++) << 32;
	value |= (uint64_t)(*p++) << 40;
	value |= (uint64_t)(*p++) << 48;
	value |= (uint64_t)(*p++) << 54;
	return value;
}

static char*
pstrdup (const char *s)
{
	int len = strlen (s) + 1;
	char *p = (char *) g_malloc (len);
	memcpy (p, s, len);
	return p;
}

typedef struct _CounterValue CounterValue;
struct _CounterValue {
	uint64_t timestamp;
	unsigned char *buffer;
	CounterValue *next;
};

typedef struct _Counter Counter;
struct _Counter {
	int index;
	const char *section;
	const char *name;
	int type;
	int unit;
	int variance;
	CounterValue *values;
	CounterValue *values_last;
};

typedef struct _CounterList CounterList;
struct _CounterList {
	Counter *counter;
	CounterList *next;
};

typedef struct _CounterSection CounterSection;
struct _CounterSection {
	const char *value;
	CounterList *counters;
	CounterList *counters_last;
	CounterSection *next;
};

typedef struct _CounterTimestamp CounterTimestamp;
struct _CounterTimestamp {
	uint64_t value;
	CounterSection *sections;
	CounterSection *sections_last;
	CounterTimestamp *next;
};

static CounterList *counters = NULL;
static CounterSection *counters_sections = NULL;
static CounterTimestamp *counters_timestamps = NULL;

enum {
	COUNTERS_SORT_TIME,
	COUNTERS_SORT_CATEGORY
};

static int counters_sort_mode = COUNTERS_SORT_TIME;

static void
add_counter_to_section (Counter *counter)
{
	CounterSection *csection, *s;
	CounterList *clist;

	clist = (CounterList *) g_calloc (1, sizeof (CounterList));
	clist->counter = counter;

	for (csection = counters_sections; csection; csection = csection->next) {
		if (strcmp (csection->value, counter->section) == 0) {
			/* If section exist */
			if (!csection->counters)
				csection->counters = clist;
			else
				csection->counters_last->next = clist;
			csection->counters_last = clist;
			return;
		}
	}

	/* If section does not exist */
	csection = (CounterSection *) g_calloc (1, sizeof (CounterSection));
	csection->value = counter->section;
	csection->counters = clist;
	csection->counters_last = clist;

	if (!counters_sections) {
		counters_sections = csection;
	} else {
		s = counters_sections;
		while (s->next)
			s = s->next;
		s->next = csection;
	}
}

static void
add_counter (const char *section, const char *name, int type, int unit, int variance, int index)
{
	CounterList *list, *l;
	Counter *counter;

	for (list = counters; list; list = list->next)
		if (list->counter->index == index)
			return;

	counter = (Counter *) g_calloc (1, sizeof (Counter));
	counter->section = section;
	counter->name = name;
	counter->type = type;
	counter->unit = unit;
	counter->variance = variance;
	counter->index = index;

	list = (CounterList *) g_calloc (1, sizeof (CounterList));
	list->counter = counter;

	if (!counters) {
		counters = list;
	} else {
		l = counters;
		while (l->next)
			l = l->next;
		l->next = list;
	}

	if (counters_sort_mode == COUNTERS_SORT_CATEGORY || !verbose)
		add_counter_to_section (counter);
}

static void
add_counter_to_timestamp (uint64_t timestamp, Counter *counter)
{
	CounterTimestamp *ctimestamp, *t;
	CounterSection *csection;
	CounterList *clist;

	clist = (CounterList *) g_calloc (1, sizeof (CounterList));
	clist->counter = counter;

	for (ctimestamp = counters_timestamps; ctimestamp; ctimestamp = ctimestamp->next) {
		if (ctimestamp->value == timestamp) {
			for (csection = ctimestamp->sections; csection; csection = csection->next) {
				if (strcmp (csection->value, counter->section) == 0) {
					/* if timestamp exist and section exist */
					if (!csection->counters)
						csection->counters = clist;
					else
						csection->counters_last->next = clist;
					csection->counters_last = clist;
					return;
				}
			}

			/* if timestamp exist and section does not exist */
			csection = (CounterSection *) g_calloc (1, sizeof (CounterSection));
			csection->value = counter->section;
			csection->counters = clist;
			csection->counters_last = clist;

			if (!ctimestamp->sections)
				ctimestamp->sections = csection;
			else
				ctimestamp->sections_last->next = csection;
			ctimestamp->sections_last = csection;
			return;
		}
	}

	/* If timestamp do not exist and section does not exist */
	csection = (CounterSection *) g_calloc (1, sizeof (CounterSection));
	csection->value = counter->section;
	csection->counters = clist;
	csection->counters_last = clist;

	ctimestamp = (CounterTimestamp *) g_calloc (1, sizeof (CounterTimestamp));
	ctimestamp->value = timestamp;
	ctimestamp->sections = csection;
	ctimestamp->sections_last = csection;

	if (!counters_timestamps) {
		counters_timestamps = ctimestamp;
	} else {
		t = counters_timestamps;
		while (t->next)
			t = t->next;
		t->next = ctimestamp;
	}
}

static void
add_counter_value (int index, CounterValue *value)
{
	CounterList *list;

	for (list = counters; list; list = list->next) {
		if (list->counter->index == index) {
			if (!list->counter->values)
				list->counter->values = value;
			else
				list->counter->values_last->next = value;
			list->counter->values_last = value;

			if (counters_sort_mode == COUNTERS_SORT_TIME)
				add_counter_to_timestamp (value->timestamp, list->counter);

			return;
		}
	}
}

static const char*
section_name (int section)
{
	switch (section) {
	case MONO_COUNTER_INTERP: return "Mono Interp";
	case MONO_COUNTER_JIT: return "Mono JIT";
	case MONO_COUNTER_GC: return "Mono GC";
	case MONO_COUNTER_METADATA: return "Mono Metadata";
	case MONO_COUNTER_GENERICS: return "Mono Generics";
	case MONO_COUNTER_SECURITY: return "Mono Security";
	case MONO_COUNTER_RUNTIME: return "Mono Runtime";
	case MONO_COUNTER_SYSTEM: return "Mono System";
	case MONO_COUNTER_PROFILER: return "Mono Profiler";
	default: return "<unknown>";
	}
}

static const char*
type_name (int type)
{
	switch (type) {
	case MONO_COUNTER_INT: return "Int";
	case MONO_COUNTER_UINT: return "UInt";
	case MONO_COUNTER_WORD: return "Word";
	case MONO_COUNTER_LONG: return "Long";
	case MONO_COUNTER_ULONG: return "ULong";
	case MONO_COUNTER_DOUBLE: return "Double";
	case MONO_COUNTER_STRING: return "String";
	case MONO_COUNTER_TIME_INTERVAL: return "Time Interval";
	default: return "<unknown>";
	}
}

static const char*
unit_name (int unit)
{
	switch (unit) {
	case MONO_COUNTER_RAW: return "Raw";
	case MONO_COUNTER_BYTES: return "Bytes";
	case MONO_COUNTER_TIME: return "Time";
	case MONO_COUNTER_COUNT: return "Count";
	case MONO_COUNTER_PERCENTAGE: return "Percentage";
	default: return "<unknown>";
	}
}

static const char*
variance_name (int variance)
{
	switch (variance) {
	case MONO_COUNTER_MONOTONIC: return "Monotonic";
	case MONO_COUNTER_CONSTANT: return "Constant";
	case MONO_COUNTER_VARIABLE: return "Variable";
	default: return "<unknown>";
	}
}

static void
dump_counters_value (Counter *counter, const char *key_format, const char *key, void *value)
{
	char format[32];

	if (value == NULL) {
		snprintf (format, sizeof (format), "%s : %%s\n", key_format);
		fprintf (outfile, format, key, "<null>");
	} else {
		switch (counter->type) {
		case MONO_COUNTER_INT:
#if SIZEOF_VOID_P == 4
		case MONO_COUNTER_WORD:
#endif
			snprintf (format, sizeof (format), "%s : %%d\n", key_format);
			fprintf (outfile, format, key, *(int32_t*)value);
			break;
		case MONO_COUNTER_UINT:
			snprintf (format, sizeof (format), "%s : %%u\n", key_format);
			fprintf (outfile, format, key, *(uint32_t*)value);
			break;
		case MONO_COUNTER_LONG:
#if SIZEOF_VOID_P == 8
		case MONO_COUNTER_WORD:
#endif
		case MONO_COUNTER_TIME_INTERVAL:
			if (counter->type == MONO_COUNTER_LONG && counter->unit == MONO_COUNTER_TIME) {
				snprintf (format, sizeof (format), "%s : %%0.3fms\n", key_format);
				fprintf (outfile, format, key, (double)*(int64_t*)value / 10000.0);
			} else if (counter->type == MONO_COUNTER_TIME_INTERVAL) {
				snprintf (format, sizeof (format), "%s : %%0.3fms\n", key_format);
				fprintf (outfile, format, key, (double)*(int64_t*)value / 1000.0);
			} else {
				snprintf (format, sizeof (format), "%s : %%u\n", key_format);
				fprintf (outfile, format, key, *(int64_t*)value);
			}
			break;
		case MONO_COUNTER_ULONG:
			snprintf (format, sizeof (format), "%s : %%" PRIu64 "\n", key_format);
			fprintf (outfile, format, key, *(uint64_t*)value);
			break;
		case MONO_COUNTER_DOUBLE:
			snprintf (format, sizeof (format), "%s : %%f\n", key_format);
			fprintf (outfile, format, key, *(double*)value);
			break;
		case MONO_COUNTER_STRING:
			snprintf (format, sizeof (format), "%s : %%s\n", key_format);
			fprintf (outfile, format, key, *(char*)value);
			break;
		}
	}
}

static void
dump_counters (void)
{
	Counter *counter;
	CounterValue *cvalue;
	CounterTimestamp *ctimestamp;
	CounterSection *csection;
	CounterList *clist;
	char strtimestamp[17];
	int i, section_printed;

	fprintf (outfile, "\nCounters:\n");

	if (!verbose) {
		char counters_to_print[][64] = {
			"Methods from AOT",
			"Methods JITted using mono JIT",
			"Methods JITted using LLVM",
			"Total time spent JITting (sec)",
			"User Time",
			"System Time",
			"Total Time",
			"Working Set",
			"Private Bytes",
			"Virtual Bytes",
			"Page Faults",
			"CPU Load Average - 1min",
			"CPU Load Average - 5min",
			"CPU Load Average - 15min",
			""
		};

		for (csection = counters_sections; csection; csection = csection->next) {
			section_printed = 0;

			for (clist = csection->counters; clist; clist = clist->next) {
				counter = clist->counter;
				if (!counter->values_last)
					continue;

				for (i = 0; counters_to_print [i][0] != 0; i++) {
					if (strcmp (counters_to_print [i], counter->name) == 0) {
						if (!section_printed) {
							fprintf (outfile, "\t%s:\n", csection->value);
							section_printed = 1;
						}

						dump_counters_value (counter, "\t\t%-30s", counter->name, counter->values_last->buffer);
						break;
					}
				}
			}
		}
	} else if (counters_sort_mode == COUNTERS_SORT_TIME) {
		for (ctimestamp = counters_timestamps; ctimestamp; ctimestamp = ctimestamp->next) {
			fprintf (outfile, "\t%" PRIu64 ":%02" PRIu64 ":%02" PRIu64 ":%02" PRIu64 ".%03" PRIu64 ":\n",
				(guint64) (ctimestamp->value / 1000 / 60 / 60 / 24 % 1000),
				(guint64) (ctimestamp->value / 1000 / 60 / 60 % 24),
				(guint64) (ctimestamp->value / 1000 / 60 % 60),
				(guint64) (ctimestamp->value / 1000 % 60),
				(guint64) (ctimestamp->value % 1000));

			for (csection = ctimestamp->sections; csection; csection = csection->next) {
				fprintf (outfile, "\t\t%s:\n", csection->value);

				for (clist = csection->counters; clist; clist = clist->next) {
					counter = clist->counter;
					for (cvalue = counter->values; cvalue; cvalue = cvalue->next) {
						if (cvalue->timestamp != ctimestamp->value)
							continue;

						dump_counters_value (counter, "\t\t\t%-30s", counter->name, cvalue->buffer);
					}
				}
			}
		}
	} else if (counters_sort_mode == COUNTERS_SORT_CATEGORY) {
		for (csection = counters_sections; csection; csection = csection->next) {
			fprintf (outfile, "\t%s:\n", csection->value);

			for (clist = csection->counters; clist; clist = clist->next) {
				counter = clist->counter;
				fprintf (outfile, "\t\t%s: [type: %s, unit: %s, variance: %s]\n",
					counter->name, type_name (counter->type), unit_name (counter->unit), variance_name (counter->variance));

				for (cvalue = counter->values; cvalue; cvalue = cvalue->next) {
					snprintf (strtimestamp, sizeof (strtimestamp), "%" PRIu64 ":%02" PRIu64 ":%02" PRIu64 ":%02" PRIu64 ".%03" PRIu64,
						(guint64) (cvalue->timestamp / 1000 / 60 / 60 / 24 % 1000),
						(guint64) (cvalue->timestamp / 1000 / 60 / 60 % 24),
						(guint64) (cvalue->timestamp / 1000 / 60 % 60),
						(guint64) (cvalue->timestamp / 1000 % 60),
						(guint64) (cvalue->timestamp % 1000));

					dump_counters_value (counter, "\t\t\t%s", strtimestamp, cvalue->buffer);
				}
			}
		}
	}
}

static int num_images;
typedef struct _ImageDesc ImageDesc;
struct _ImageDesc {
	ImageDesc *next;
	intptr_t image;
	char *filename;
};

static ImageDesc* image_hash [SMALL_HASH_SIZE] = {0};

static void
add_image (intptr_t image, char *name)
{
	int slot = ((image >> 2) & 0xffff) % SMALL_HASH_SIZE;
	ImageDesc *cd = (ImageDesc *) g_malloc (sizeof (ImageDesc));
	cd->image = image;
	cd->filename = pstrdup (name);
	cd->next = image_hash [slot];
	image_hash [slot] = cd;
	num_images++;
}

static int num_assemblies;

typedef struct _AssemblyDesc AssemblyDesc;
struct _AssemblyDesc {
	AssemblyDesc *next;
	intptr_t assembly;
	char *asmname;
};

static AssemblyDesc* assembly_hash [SMALL_HASH_SIZE] = {0};

static void
add_assembly (intptr_t assembly, char *name)
{
	int slot = ((assembly >> 2) & 0xffff) % SMALL_HASH_SIZE;
	AssemblyDesc *cd = (AssemblyDesc *) g_malloc (sizeof (AssemblyDesc));
	cd->assembly = assembly;
	cd->asmname = pstrdup (name);
	cd->next = assembly_hash [slot];
	assembly_hash [slot] = cd;
	num_assemblies++;
}

typedef struct _BackTrace BackTrace;
typedef struct {
	uint64_t count;
	BackTrace *bt;
} CallContext;

typedef struct {
	int count;
	int size;
	CallContext *traces;
} TraceDesc;

typedef struct _ClassDesc ClassDesc;
struct _ClassDesc {
	ClassDesc *next;
	intptr_t klass;
	char *name;
	intptr_t allocs;
	uint64_t alloc_size;
	TraceDesc traces;
};

static ClassDesc* class_hash [HASH_SIZE] = {0};
static int num_classes = 0;

static ClassDesc*
add_class (intptr_t klass, const char *name)
{
	int slot = ((klass >> 2) & 0xffff) % HASH_SIZE;
	ClassDesc *cd;
	cd = class_hash [slot];
	while (cd && cd->klass != klass)
		cd = cd->next;
	/* we resolved an unknown class (unless we had the code unloaded) */
	if (cd) {
		/*printf ("resolved unknown: %s\n", name);*/
		g_free (cd->name);
		cd->name = pstrdup (name);
		return cd;
	}
	cd = (ClassDesc *) g_calloc (sizeof (ClassDesc), 1);
	cd->klass = klass;
	cd->name = pstrdup (name);
	cd->next = class_hash [slot];
	cd->allocs = 0;
	cd->alloc_size = 0;
	cd->traces.count = 0;
	cd->traces.size = 0;
	cd->traces.traces = NULL;
	class_hash [slot] = cd;
	num_classes++;
	return cd;
}

static ClassDesc *
lookup_class (intptr_t klass)
{
	int slot = ((klass >> 2) & 0xffff) % HASH_SIZE;
	ClassDesc *cd = class_hash [slot];
	while (cd && cd->klass != klass)
		cd = cd->next;
	if (!cd) {
		char buf [128];
		snprintf (buf, sizeof (buf), "unresolved class %p", (void*)klass);
		return add_class (klass, buf);
	}
	return cd;
}

typedef struct _VTableDesc VTableDesc;
struct _VTableDesc {
	VTableDesc *next;
	intptr_t vtable;
	ClassDesc *klass;
};

static VTableDesc* vtable_hash [HASH_SIZE] = {0};

static VTableDesc*
add_vtable (intptr_t vtable, intptr_t klass)
{
	int slot = ((vtable >> 2) & 0xffff) % HASH_SIZE;

	VTableDesc *vt = vtable_hash [slot];

	while (vt && vt->vtable != vtable)
		vt = vt->next;

	if (vt)
		return vt;

	vt = (VTableDesc *) g_calloc (sizeof (VTableDesc), 1);

	vt->vtable = vtable;
	vt->klass = lookup_class (klass);
	vt->next = vtable_hash [slot];

	vtable_hash [slot] = vt;

	return vt;
}

static VTableDesc *
lookup_vtable (intptr_t vtable)
{
	int slot = ((vtable >> 2) & 0xffff) % HASH_SIZE;
	VTableDesc *vt = vtable_hash [slot];

	while (vt && vt->vtable != vtable)
		vt = vt->next;

	if (!vt)
		return add_vtable (vtable, 0);

	return vt;
}

typedef struct _MethodDesc MethodDesc;
struct _MethodDesc {
	MethodDesc *next;
	intptr_t method;
	char *name;
	intptr_t code;
	int len;
	int recurse_count;
	int sample_hits;
	int ignore_jit; /* when this is set, we collect the metadata but don't count this method fot jit time and code size, when filtering events */
	uint64_t calls;
	uint64_t total_time;
	uint64_t callee_time;
	uint64_t self_time;
	TraceDesc traces;
};

static MethodDesc* method_hash [HASH_SIZE] = {0};
static int num_methods = 0;

static MethodDesc*
add_method (intptr_t method, const char *name, intptr_t code, int len)
{
	int slot = ((method >> 2) & 0xffff) % HASH_SIZE;
	MethodDesc *cd;
	cd = method_hash [slot];
	while (cd && cd->method != method)
		cd = cd->next;
	/* we resolved an unknown method (unless we had the code unloaded) */
	if (cd) {
		cd->code = code;
		cd->len = len;
		/*printf ("resolved unknown: %s\n", name);*/
		g_free (cd->name);
		cd->name = pstrdup (name);
		return cd;
	}
	cd = (MethodDesc *) g_calloc (sizeof (MethodDesc), 1);
	cd->method = method;
	cd->name = pstrdup (name);
	cd->code = code;
	cd->len = len;
	cd->calls = 0;
	cd->total_time = 0;
	cd->traces.count = 0;
	cd->traces.size = 0;
	cd->traces.traces = NULL;
	cd->next = method_hash [slot];
	method_hash [slot] = cd;
	num_methods++;
	return cd;
}

static MethodDesc *
lookup_method (intptr_t method)
{
	int slot = ((method >> 2) & 0xffff) % HASH_SIZE;
	MethodDesc *cd = method_hash [slot];
	while (cd && cd->method != method)
		cd = cd->next;
	if (!cd) {
		char buf [128];
		snprintf (buf, sizeof (buf), "unknown method %p", (void*)method);
		return add_method (method, buf, 0, 0);
	}
	return cd;
}

static int num_stat_samples = 0;
static int size_stat_samples = 0;
uintptr_t *stat_samples = NULL;
int *stat_sample_desc = NULL;

static void
add_stat_sample (int type, uintptr_t ip) {
	if (num_stat_samples == size_stat_samples) {
		size_stat_samples *= 2;
		if (!size_stat_samples)
		size_stat_samples = 32;
		stat_samples = (uintptr_t *) g_realloc (stat_samples, size_stat_samples * sizeof (uintptr_t));
		stat_sample_desc = (int *) g_realloc (stat_sample_desc, size_stat_samples * sizeof (int));
	}
	stat_samples [num_stat_samples] = ip;
	stat_sample_desc [num_stat_samples++] = type;
}

static MethodDesc*
lookup_method_by_ip (uintptr_t ip)
{
	int i;
	MethodDesc* m;
	/* dumb */
	for (i = 0; i < HASH_SIZE; ++i) {
		m = method_hash [i];
		while (m) {
			//printf ("checking %p against %p-%p\n", (void*)ip, (void*)(m->code), (void*)(m->code + m->len));
			if (ip >= (uintptr_t)m->code && ip < (uintptr_t)m->code + m->len) {
				return m;
			}
			m = m->next;
		}
	}
	return NULL;
}

static int
compare_method_samples (const void *a, const void *b)
{
	MethodDesc *const *A = (MethodDesc *const *)a;
	MethodDesc *const *B = (MethodDesc *const *)b;
	if ((*A)->sample_hits == (*B)->sample_hits)
		return 0;
	if ((*B)->sample_hits < (*A)->sample_hits)
		return -1;
	return 1;
}

typedef struct _UnmanagedSymbol UnmanagedSymbol;
struct _UnmanagedSymbol {
	UnmanagedSymbol *parent;
	char *name;
	int is_binary;
	uintptr_t addr;
	uintptr_t size;
	uintptr_t sample_hits;
};

static UnmanagedSymbol **usymbols = NULL;
static int usymbols_size = 0;
static int usymbols_num = 0;

static int
compare_usymbol_addr (const void *a, const void *b)
{
	UnmanagedSymbol *const *A = (UnmanagedSymbol *const *)a;
	UnmanagedSymbol *const *B = (UnmanagedSymbol *const *)b;
	if ((*B)->addr == (*A)->addr)
		return 0;
	if ((*B)->addr > (*A)->addr)
		return -1;
	return 1;
}

static int
compare_usymbol_samples (const void *a, const void *b)
{
	UnmanagedSymbol *const *A = (UnmanagedSymbol *const *)a;
	UnmanagedSymbol *const *B = (UnmanagedSymbol *const *)b;
	if ((*B)->sample_hits == (*A)->sample_hits)
		return 0;
	if ((*B)->sample_hits < (*A)->sample_hits)
		return -1;
	return 1;
}

static void
add_unmanaged_symbol (uintptr_t addr, char *name, uintptr_t size)
{
	UnmanagedSymbol *sym;
	if (usymbols_num == usymbols_size) {
		int new_size = usymbols_size * 2;
		if (!new_size)
			new_size = 16;
		usymbols = (UnmanagedSymbol **) g_realloc (usymbols, sizeof (void*) * new_size);
		usymbols_size = new_size;
	}
	sym = (UnmanagedSymbol *) g_calloc (sizeof (UnmanagedSymbol), 1);
	sym->addr = addr;
	sym->name = name;
	sym->size = size;
	usymbols [usymbols_num++] = sym;
}

/* only valid after the symbols are sorted */
static UnmanagedSymbol*
lookup_unmanaged_symbol (uintptr_t addr)
{
	int r = usymbols_num - 1;
	int l = 0;
	UnmanagedSymbol *sym;
	int last_best = -1;
	while (r >= l) {
		int m = (l + r) / 2;
		sym = usymbols [m];
		if (addr == sym->addr)
			return sym;
		if (addr < sym->addr) {
			r = m - 1;
		} else if (addr > sym->addr) {
			l = m + 1;
			last_best = m;
		}
	}
	if (last_best >= 0 && (addr - usymbols [last_best]->addr) < 4096)
		return usymbols [last_best];
	return NULL;
}

/* we use the same structure for binaries */
static UnmanagedSymbol **ubinaries = NULL;
static int ubinaries_size = 0;
static int ubinaries_num = 0;

static void
add_unmanaged_binary (uintptr_t addr, char *name, uintptr_t size)
{
	UnmanagedSymbol *sym;
	if (ubinaries_num == ubinaries_size) {
		int new_size = ubinaries_size * 2;
		if (!new_size)
			new_size = 16;
		ubinaries = (UnmanagedSymbol **) g_realloc (ubinaries, sizeof (void*) * new_size);
		ubinaries_size = new_size;
	}
	sym = (UnmanagedSymbol *) g_calloc (sizeof (UnmanagedSymbol), 1);
	sym->addr = addr;
	sym->name = name;
	sym->size = size;
	sym->is_binary = 1;
	ubinaries [ubinaries_num++] = sym;
}

static UnmanagedSymbol*
lookup_unmanaged_binary (uintptr_t addr)
{
	int i;
	for (i = 0; i < ubinaries_num; ++i) {
		UnmanagedSymbol *ubin = ubinaries [i];
		if (addr >= ubin->addr && addr < ubin->addr + ubin->size) {
			return ubin;
		}
	}
	return NULL;
}

// For backwards compatibility.
enum {
	TYPE_SAMPLE_UBIN = 2 << 4,

	TYPE_COVERAGE = 9,

	TYPE_COVERAGE_ASSEMBLY = 0 << 4,
	TYPE_COVERAGE_METHOD = 1 << 4,
	TYPE_COVERAGE_STATEMENT = 2 << 4,
	TYPE_COVERAGE_CLASS = 3 << 4,
};

enum {
	SAMPLE_CYCLES = 1,
	SAMPLE_INSTRUCTIONS,
	SAMPLE_CACHE_MISSES,
	SAMPLE_CACHE_REFS,
	SAMPLE_BRANCHES,
	SAMPLE_BRANCH_MISSES,
};

enum {
	MONO_GC_EVENT_MARK_START = 1,
	MONO_GC_EVENT_MARK_END = 2,
	MONO_GC_EVENT_RECLAIM_START = 3,
	MONO_GC_EVENT_RECLAIM_END = 4,
};

static const char*
sample_type_name (int type)
{
	switch (type) {
	case SAMPLE_CYCLES: return "cycles";
	case SAMPLE_INSTRUCTIONS: return "instructions retired";
	case SAMPLE_CACHE_MISSES: return "cache misses";
	case SAMPLE_CACHE_REFS: return "cache references";
	case SAMPLE_BRANCHES: return "executed branches";
	case SAMPLE_BRANCH_MISSES: return "unpredicted branches";
	}
	return "unknown";
}

static void
set_usym_parent (UnmanagedSymbol** cachedus, int count)
{
	int i;
	for (i = 0; i < count; ++i) {
		UnmanagedSymbol *ubin = lookup_unmanaged_binary (cachedus [i]->addr);
		if (ubin == cachedus [i])
			continue;
		cachedus [i]->parent = ubin;
	}
}

static void
print_usym (UnmanagedSymbol* um)
{
	if (um->parent)
		fprintf (outfile, "\t%6zd %6.2f %-36s in %s\n", um->sample_hits, um->sample_hits*100.0/num_stat_samples, um->name, um->parent->name);
	else
		fprintf (outfile, "\t%6zd %6.2f %s\n", um->sample_hits, um->sample_hits*100.0/num_stat_samples, um->name);
}

static int
sym_percent (uintptr_t sample_hits)
{
	double pc;
	if (verbose)
		return 1;
	pc = sample_hits*100.0/num_stat_samples;
	return pc >= 0.1;
}

static void
dump_samples (void)
{
	int i, u;
	int count = 0, msize = 0;
	int unmanaged_hits = 0;
	int unresolved_hits = 0;
	MethodDesc** cachedm = NULL;
	int ucount = 0, usize = 0;
	UnmanagedSymbol** cachedus = NULL;
	if (!num_stat_samples)
		return;
	mono_qsort (usymbols, usymbols_num, sizeof (UnmanagedSymbol*), compare_usymbol_addr);
	for (i = 0; i < num_stat_samples; ++i) {
		MethodDesc *m = lookup_method_by_ip (stat_samples [i]);
		if (m) {
			if (!m->sample_hits) {
				if (count == msize) {
					msize *= 2;
					if (!msize)
						msize = 4;
					cachedm = (MethodDesc **) g_realloc (cachedm, sizeof (void*) * msize);
				}
				cachedm [count++] = m;
			}
			m->sample_hits++;
		} else {
			UnmanagedSymbol *usym = lookup_unmanaged_symbol (stat_samples [i]);
			if (!usym) {
				unresolved_hits++;
				//printf ("unmanaged hit at %p\n", (void*)stat_samples [i]);
				usym = lookup_unmanaged_binary (stat_samples [i]);
			}
			if (usym) {
				if (!usym->sample_hits) {
					if (ucount == usize) {
						usize *= 2;
						if (!usize)
							usize = 4;
						cachedus = (UnmanagedSymbol **) g_realloc (cachedus, sizeof (void*) * usize);
					}
					cachedus [ucount++] = usym;
				}
				usym->sample_hits++;
			}
			unmanaged_hits++;
		}
	}
	mono_qsort (cachedm, count, sizeof (MethodDesc*), compare_method_samples);
	mono_qsort (cachedus, ucount, sizeof (UnmanagedSymbol*), compare_usymbol_samples);
	set_usym_parent (cachedus, ucount);
	fprintf (outfile, "\nStatistical samples summary\n");
	fprintf (outfile, "\tSample type: %s\n", sample_type_name (stat_sample_desc [0]));
	fprintf (outfile, "\tUnmanaged hits:  %6d (%4.1f%%)\n", unmanaged_hits, (100.0*unmanaged_hits)/num_stat_samples);
	fprintf (outfile, "\tManaged hits:    %6d (%4.1f%%)\n", num_stat_samples - unmanaged_hits, (100.0*(num_stat_samples-unmanaged_hits))/num_stat_samples);
	fprintf (outfile, "\tUnresolved hits: %6d (%4.1f%%)\n", unresolved_hits, (100.0*unresolved_hits)/num_stat_samples);
	fprintf (outfile, "\t%6s %6s %s\n", "Hits", "%", "Method name");
	i = 0;
	u = 0;
	while (i < count || u < ucount) {
		if (i < count) {
			MethodDesc *m = cachedm [i];
			if (u < ucount) {
				UnmanagedSymbol *um = cachedus [u];
				if (um->sample_hits > m->sample_hits) {
					if (!sym_percent (um->sample_hits))
						break;
					print_usym (um);
					u++;
					continue;
				}
			}
			if (!sym_percent (m->sample_hits))
				break;
			fprintf (outfile, "\t%6d %6.2f %s\n", m->sample_hits, m->sample_hits*100.0/num_stat_samples, m->name);
			i++;
			continue;
		}
		if (u < ucount) {
			UnmanagedSymbol *um = cachedus [u];
			if (!sym_percent (um->sample_hits))
				break;
			print_usym (um);
			u++;
			continue;
		}
	}
}

typedef struct _HeapClassDesc HeapClassDesc;
typedef struct {
	HeapClassDesc *klass;
	uint64_t count;
} HeapClassRevRef;

struct _HeapClassDesc {
	ClassDesc *klass;
	int64_t count;
	int64_t total_size;
	HeapClassRevRef *rev_hash;
	int rev_hash_size;
	int rev_count;
	uintptr_t pinned_references;
	uintptr_t root_references;
};

static int
add_rev_class_hashed (HeapClassRevRef *rev_hash, uintptr_t size, HeapClassDesc *hklass, uint64_t value)
{
	uintptr_t i;
	uintptr_t start_pos;
	start_pos = (hklass->klass->klass >> 2) % size;
	assert (start_pos < size);
	i = start_pos;
	do {
		if (rev_hash [i].klass == hklass) {
			rev_hash [i].count += value;
			return 0;
		} else if (!rev_hash [i].klass) {
			rev_hash [i].klass = hklass;
			rev_hash [i].count += value;
			start_pos = 0;
			for (i = 0; i < size; ++i)
				if (rev_hash [i].klass && rev_hash [i].klass->klass == hklass->klass)
					start_pos ++;
			assert (start_pos == 1);
			return 1;
		}
		/* wrap around */
		if (++i == size)
			i = 0;
	} while (i != start_pos);
	/* should not happen */
	printf ("failed revref store\n");
	return 0;
}

static void
add_heap_class_rev (HeapClassDesc *from, HeapClassDesc *to)
{
	uintptr_t i;
	if (to->rev_count * 2 >= to->rev_hash_size) {
		HeapClassRevRef *n;
		uintptr_t old_size = to->rev_hash_size;
		to->rev_hash_size *= 2;
		if (to->rev_hash_size == 0)
			to->rev_hash_size = 4;
		n = (HeapClassRevRef *) g_calloc (sizeof (HeapClassRevRef) * to->rev_hash_size, 1);
		for (i = 0; i < old_size; ++i) {
			if (to->rev_hash [i].klass)
				add_rev_class_hashed (n, to->rev_hash_size, to->rev_hash [i].klass, to->rev_hash [i].count);
		}
		if (to->rev_hash)
			g_free (to->rev_hash);
		to->rev_hash = n;
	}
	to->rev_count += add_rev_class_hashed (to->rev_hash, to->rev_hash_size, from, 1);
}

typedef struct {
	uintptr_t objaddr;
	HeapClassDesc *hklass;
	uintptr_t num_refs;
	uintptr_t refs [0];
} HeapObjectDesc;

typedef struct _HeapShot HeapShot;
struct _HeapShot {
	HeapShot *next;
	uint64_t timestamp;
	int class_count;
	int hash_size;
	HeapClassDesc **class_hash;
	HeapClassDesc **sorted;
	HeapObjectDesc **objects_hash;
	uintptr_t objects_count;
	uintptr_t objects_hash_size;
	uintptr_t num_roots;
	uintptr_t *roots;
	uintptr_t *roots_extra;
	int *roots_types;
};

static HeapShot *heap_shots = NULL;
static int num_heap_shots = 0;

static HeapShot*
new_heap_shot (uint64_t timestamp)
{
	HeapShot *hs = (HeapShot *) g_calloc (sizeof (HeapShot), 1);
	hs->hash_size = 4;
	hs->class_hash = (HeapClassDesc **) g_calloc (sizeof (void*), hs->hash_size);
	hs->timestamp = timestamp;
	num_heap_shots++;
	hs->next = heap_shots;
	heap_shots = hs;
	return hs;
}

static HeapClassDesc*
heap_class_lookup (HeapShot *hs, ClassDesc *klass)
{
	int i;
	unsigned int start_pos;
	start_pos = ((uintptr_t)klass->klass >> 2) % hs->hash_size;
	i = start_pos;
	do {
		HeapClassDesc* cd = hs->class_hash [i];
		if (!cd)
			return NULL;
		if (cd->klass == klass)
			return cd;
		/* wrap around */
		if (++i == hs->hash_size)
			i = 0;
	} while (i != start_pos);
	return NULL;
}

static int
add_heap_hashed (HeapClassDesc **hash, HeapClassDesc **retv, uintptr_t hsize, ClassDesc *klass, uint64_t size, uint64_t count)
{
	uintptr_t i;
	uintptr_t start_pos;
	start_pos = ((uintptr_t)klass->klass >> 2) % hsize;
	i = start_pos;
	do {
		if (hash [i] && hash [i]->klass == klass) {
			hash [i]->total_size += size;
			hash [i]->count += count;
			*retv = hash [i];
			return 0;
		} else if (!hash [i]) {
			if (*retv) {
				hash [i] = *retv;
				return 1;
			}
			hash [i] = (HeapClassDesc *) g_calloc (sizeof (HeapClassDesc), 1);
			hash [i]->klass = klass;
			hash [i]->total_size += size;
			hash [i]->count += count;
			*retv = hash [i];
			return 1;
		}
		/* wrap around */
		if (++i == hsize)
			i = 0;
	} while (i != start_pos);
	/* should not happen */
	printf ("failed heap class store\n");
	return 0;
}

static HeapClassDesc*
add_heap_shot_class (HeapShot *hs, ClassDesc *klass, uint64_t size)
{
	HeapClassDesc *res;
	int i;
	if (hs->class_count * 2 >= hs->hash_size) {
		HeapClassDesc **n;
		int old_size = hs->hash_size;
		hs->hash_size *= 2;
		if (hs->hash_size == 0)
			hs->hash_size = 4;
		n = (HeapClassDesc **) g_calloc (sizeof (void*) * hs->hash_size, 1);
		for (i = 0; i < old_size; ++i) {
			res = hs->class_hash [i];
			if (hs->class_hash [i])
				add_heap_hashed (n, &res, hs->hash_size, hs->class_hash [i]->klass, hs->class_hash [i]->total_size, hs->class_hash [i]->count);
		}
		if (hs->class_hash)
			g_free (hs->class_hash);
		hs->class_hash = n;
	}
	res = NULL;
	hs->class_count += add_heap_hashed (hs->class_hash, &res, hs->hash_size, klass, size, 1);
	//if (res->count == 1)
	//	printf ("added heap class: %s\n", res->klass->name);
	return res;
}

static HeapObjectDesc*
alloc_heap_obj (uintptr_t objaddr, HeapClassDesc *hklass, uintptr_t num_refs)
{
	HeapObjectDesc* ho = (HeapObjectDesc *) g_calloc (sizeof (HeapObjectDesc) + num_refs * sizeof (uintptr_t), 1);
	ho->objaddr = objaddr;
	ho->hklass = hklass;
	ho->num_refs = num_refs;
	return ho;
}

static uintptr_t
heap_shot_find_obj_slot (HeapShot *hs, uintptr_t objaddr)
{
	uintptr_t i;
	uintptr_t start_pos;
	HeapObjectDesc **hash = hs->objects_hash;
	if (hs->objects_hash_size == 0)
		return -1;
	start_pos = ((uintptr_t)objaddr >> 3) % hs->objects_hash_size;
	i = start_pos;
	do {
		if (hash [i] && hash [i]->objaddr == objaddr) {
			return i;
		} else if (!hash [i]) {
			break; /* fail */
		}
		/* wrap around */
		if (++i == hs->objects_hash_size)
			i = 0;
	} while (i != start_pos);
	/* should not happen */
	//printf ("failed heap obj slot\n");
	return -1;
}

static HeapObjectDesc*
heap_shot_obj_add_refs (HeapShot *hs, uintptr_t objaddr, uintptr_t num, uintptr_t *ref_offset)
{
	HeapObjectDesc **hash = hs->objects_hash;
	uintptr_t i = heap_shot_find_obj_slot (hs, objaddr);
	if (i >= 0) {
		HeapObjectDesc* ho = alloc_heap_obj (objaddr, hash [i]->hklass, hash [i]->num_refs + num);
		*ref_offset = hash [i]->num_refs;
		memcpy (ho->refs, hash [i]->refs, hash [i]->num_refs * sizeof (uintptr_t));
		g_free (hash [i]);
		hash [i] = ho;
		return ho;
	}
	/* should not happen */
	printf ("failed heap obj update\n");
	return NULL;

}

static uintptr_t
add_heap_hashed_obj (HeapObjectDesc **hash, uintptr_t hsize, HeapObjectDesc *obj)
{
	uintptr_t i;
	uintptr_t start_pos;
	start_pos = ((uintptr_t)obj->objaddr >> 3) % hsize;
	i = start_pos;
	do {
		if (hash [i] && hash [i]->objaddr == obj->objaddr) {
			printf ("duplicate object!\n");
			return 0;
		} else if (!hash [i]) {
			hash [i] = obj;
			return 1;
		}
		/* wrap around */
		if (++i == hsize)
			i = 0;
	} while (i != start_pos);
	/* should not happen */
	printf ("failed heap obj store\n");
	return 0;
}

static void
add_heap_shot_obj (HeapShot *hs, HeapObjectDesc *obj)
{
	uintptr_t i;
	if (hs->objects_count * 2 >= hs->objects_hash_size) {
		HeapObjectDesc **n;
		uintptr_t old_size = hs->objects_hash_size;
		hs->objects_hash_size *= 2;
		if (hs->objects_hash_size == 0)
			hs->objects_hash_size = 4;
		n = (HeapObjectDesc **) g_calloc (sizeof (void*) * hs->objects_hash_size, 1);
		for (i = 0; i < old_size; ++i) {
			if (hs->objects_hash [i])
				add_heap_hashed_obj (n, hs->objects_hash_size, hs->objects_hash [i]);
		}
		if (hs->objects_hash)
			g_free (hs->objects_hash);
		hs->objects_hash = n;
	}
	hs->objects_count += add_heap_hashed_obj (hs->objects_hash, hs->objects_hash_size, obj);
}

static void
heap_shot_resolve_reverse_refs (HeapShot *hs)
{
	uintptr_t i;
	for (i = 0; i < hs->objects_hash_size; ++i) {
		uintptr_t r;
		HeapObjectDesc *ho = hs->objects_hash [i];
		if (!ho)
			continue;
		for (r = 0; r < ho->num_refs; ++r) {
			uintptr_t oi = heap_shot_find_obj_slot (hs, ho->refs [r]);
			add_heap_class_rev (ho->hklass, hs->objects_hash [oi]->hklass);
		}
	}
}

#define MARK_GRAY 1
#define MARK_BLACK 2

static void
heap_shot_mark_objects (HeapShot *hs)
{
	uintptr_t i, oi, r;
	unsigned char *marks;
	HeapObjectDesc *obj, *ref;
	int marked_some;
	uintptr_t num_marked = 0, num_unmarked;
	for (i = 0; i < hs->num_roots; ++i) {
		HeapClassDesc *cd;
		oi = heap_shot_find_obj_slot (hs, hs->roots [i]);
		if (oi == -1) {
			continue;
		}
		obj = hs->objects_hash [oi];
		cd = obj->hklass;
		if (hs->roots_types [i] & MONO_PROFILER_GC_ROOT_PINNING)
			cd->pinned_references++;
		cd->root_references++;
	}
	if (!debug)
		return;
	/* consistency checks: it seems not all the objects are walked in the heap in some cases */
	marks = (unsigned char *) g_calloc (hs->objects_hash_size, 1);
	if (!marks)
		return;
	for (i = 0; i < hs->num_roots; ++i) {
		oi = heap_shot_find_obj_slot (hs, hs->roots [i]);
		if (oi == -1) {
			fprintf (outfile, "root type 0x%x for obj %p (%s) not found in heap\n", hs->roots_types [i], (void*)hs->roots [i], lookup_class (hs->roots_extra [i])->name);
			continue;
		}
		obj = hs->objects_hash [oi];
		if (!marks [oi]) {
			marks [oi] = obj->num_refs? MARK_GRAY: MARK_BLACK;
			num_marked++;
		}
	}
	marked_some = 1;
	while (marked_some) {
		marked_some = 0;
		for (i = 0; i < hs->objects_hash_size; ++i) {
			if (marks [i] != MARK_GRAY)
				continue;
			marks [i] = MARK_BLACK;
			obj = hs->objects_hash [i];
			for (r = 0; r < obj->num_refs; ++r) {
				oi = heap_shot_find_obj_slot (hs, obj->refs [r]);
				if (oi == -1) {
					fprintf (outfile, "referenced obj %p not found in heap\n", (void*)obj->refs [r]);
					continue;
				}
				ref = hs->objects_hash [oi];
				if (!marks [oi]) {
					marks [oi] = ref->num_refs? MARK_GRAY: MARK_BLACK;
				}
			}
			marked_some++;
		}
	}

	num_unmarked = 0;
	for (i = 0; i < hs->objects_hash_size; ++i) {
		if (hs->objects_hash [i] && !marks [i]) {
			num_unmarked++;
			fprintf (outfile, "object %p (%s) unmarked\n", (void*)hs->objects_hash [i], hs->objects_hash [i]->hklass->klass->name);
		}
	}
	fprintf (outfile, "Total unmarked: %" G_GSIZE_FORMAT "d/%" G_GSIZE_FORMAT "d\n", num_unmarked, hs->objects_count);
	g_free (marks);
}

static void
heap_shot_free_objects (HeapShot *hs)
{
	uintptr_t i;
	for (i = 0; i < hs->objects_hash_size; ++i) {
		HeapObjectDesc *ho = hs->objects_hash [i];
		if (ho)
			g_free (ho);
	}
	if (hs->objects_hash)
		g_free (hs->objects_hash);
	hs->objects_hash = NULL;
	hs->objects_hash_size = 0;
	hs->objects_count = 0;
}


struct _BackTrace {
	BackTrace *next;
	unsigned int hash;
	int count;
	int id;
	MethodDesc *methods [1];
};

static BackTrace *backtrace_hash [HASH_SIZE];
static BackTrace **backtraces = NULL;
static int num_backtraces = 0;
static int next_backtrace = 0;

static int
hash_backtrace (int count, MethodDesc **methods)
{
	int hash = count;
	int i;
	for (i = 0; i < count; ++i) {
		hash = (hash << 5) - hash + methods [i]->method;
	}
	return hash;
}

static int
compare_backtrace (BackTrace *bt, int count, MethodDesc **methods)
{
	int i;
	if (bt->count != count)
		return 0;
	for (i = 0; i < count; ++i)
		if (methods [i] != bt->methods [i])
			return 0;
	return 1;
}

static BackTrace*
add_backtrace (int count, MethodDesc **methods)
{
	int hash = hash_backtrace (count, methods);
	int slot = (hash & 0xffff) % HASH_SIZE;
	BackTrace *bt = backtrace_hash [slot];
	while (bt) {
		if (bt->hash == hash && compare_backtrace (bt, count, methods))
			return bt;
		bt = bt->next;
	}
	bt = (BackTrace *) g_malloc (sizeof (BackTrace) + ((count - 1) * sizeof (void*)));
	bt->next = backtrace_hash [slot];
	backtrace_hash [slot] = bt;
	if (next_backtrace == num_backtraces) {
		num_backtraces *= 2;
		if (!num_backtraces)
			num_backtraces = 16;
		backtraces = (BackTrace **) g_realloc (backtraces, sizeof (void*) * num_backtraces);
	}
	bt->id = next_backtrace++;
	backtraces [bt->id] = bt;
	bt->count = count;
	bt->hash = hash;
	for (slot = 0; slot < count; ++slot)
		bt->methods [slot] = methods [slot];

	return bt;
}

typedef struct _MonitorDesc MonitorDesc;
typedef struct _ThreadContext ThreadContext;
typedef struct _DomainContext DomainContext;
typedef struct _RemCtxContext RemCtxContext;

typedef struct {
	FILE *file;
#if defined (HAVE_SYS_ZLIB)
	gzFile gzfile;
#endif
	unsigned char *buf;
	int size;
	int data_version;
	int version_major;
	int version_minor;
	int timer_overhead;
	int pid;
	int port;
	char *args;
	char *arch;
	char *os;
	uint64_t startup_time;
	ThreadContext *threads;
	ThreadContext *current_thread;
	DomainContext *domains;
	DomainContext *current_domain;
	RemCtxContext *remctxs;
	RemCtxContext *current_remctx;
} ProfContext;

struct _ThreadContext {
	ThreadContext *next;
	intptr_t thread_id;
	char *name;
	/* emulated stack */
	MethodDesc **stack;
	uint64_t *time_stack;
	uint64_t *callee_time_stack;
	uint64_t last_time;
	uint64_t contention_start;
	MonitorDesc *monitor;
	int stack_size;
	int stack_id;
	HeapShot *current_heap_shot;
	uintptr_t num_roots;
	uintptr_t size_roots;
	uintptr_t *roots;
	uintptr_t *roots_extra;
	int *roots_types;
	uint64_t gc_start_times [3];
};

struct _DomainContext {
	DomainContext *next;
	intptr_t domain_id;
	const char *friendly_name;
};

struct _RemCtxContext {
	RemCtxContext *next;
	intptr_t remctx_id;
	intptr_t domain_id;
};

static void
ensure_buffer (ProfContext *ctx, int size)
{
	if (ctx->size < size) {
		ctx->buf = (unsigned char *) g_realloc (ctx->buf, size);
		ctx->size = size;
	}
}

static int
load_data (ProfContext *ctx, int size)
{
	ensure_buffer (ctx, size);
#if defined (HAVE_SYS_ZLIB)
	if (ctx->gzfile) {
		int r = gzread (ctx->gzfile, ctx->buf, size);
		if (r == 0)
			return size == 0? 1: 0;
		return r == size;
	} else
#endif
	{
		int r = fread (ctx->buf, size, 1, ctx->file);
		if (r == 0)
			return size == 0? 1: 0;
		return r;
	}
}

static ThreadContext*
get_thread (ProfContext *ctx, intptr_t thread_id)
{
	ThreadContext *thread;
	if (ctx->current_thread && ctx->current_thread->thread_id == thread_id)
		return ctx->current_thread;
	thread = ctx->threads;
	while (thread) {
		if (thread->thread_id == thread_id) {
			return thread;
		}
		thread = thread->next;
	}
	thread = (ThreadContext *) g_calloc (sizeof (ThreadContext), 1);
	thread->next = ctx->threads;
	ctx->threads = thread;
	thread->thread_id = thread_id;
	thread->last_time = 0;
	thread->stack_id = 0;
	thread->stack_size = 32;
	thread->stack = (MethodDesc **) g_malloc (thread->stack_size * sizeof (void*));
	thread->time_stack = (uint64_t *) g_malloc (thread->stack_size * sizeof (uint64_t));
	thread->callee_time_stack = (uint64_t *) g_malloc (thread->stack_size * sizeof (uint64_t));
	return thread;
}

static DomainContext *
get_domain (ProfContext *ctx, intptr_t domain_id)
{
	if (ctx->current_domain && ctx->current_domain->domain_id == domain_id)
		return ctx->current_domain;

	DomainContext *domain = ctx->domains;

	while (domain) {
		if (domain->domain_id == domain_id)
			return domain;

		domain = domain->next;
	}

	domain = (DomainContext *) g_calloc (sizeof (DomainContext), 1);
	domain->next = ctx->domains;
	ctx->domains = domain;
	domain->domain_id = domain_id;

	return domain;
}

static RemCtxContext *
get_remctx (ProfContext *ctx, intptr_t remctx_id)
{
	if (ctx->current_remctx && ctx->current_remctx->remctx_id == remctx_id)
		return ctx->current_remctx;

	RemCtxContext *remctx = ctx->remctxs;

	while (remctx) {
		if (remctx->remctx_id == remctx_id)
			return remctx;

		remctx = remctx->next;
	}

	remctx = (RemCtxContext *) g_calloc (sizeof (RemCtxContext), 1);
	remctx->next = ctx->remctxs;
	ctx->remctxs = remctx;
	remctx->remctx_id = remctx_id;

	return remctx;
}

static ThreadContext*
load_thread (ProfContext *ctx, intptr_t thread_id)
{
	ThreadContext *thread = get_thread (ctx, thread_id);
	ctx->current_thread = thread;
	return thread;
}

static void
ensure_thread_stack (ThreadContext *thread)
{
	if (thread->stack_id == thread->stack_size) {
		thread->stack_size *= 2;
		thread->stack = (MethodDesc **) g_realloc (thread->stack, thread->stack_size * sizeof (void*));
		thread->time_stack = (uint64_t *) g_realloc (thread->time_stack, thread->stack_size * sizeof (uint64_t));
		thread->callee_time_stack = (uint64_t *) g_realloc (thread->callee_time_stack, thread->stack_size * sizeof (uint64_t));
	}
}

static int
add_trace_hashed (CallContext *traces, int size, BackTrace *bt, uint64_t value)
{
	int i;
	unsigned int start_pos;
	start_pos = bt->hash % size;
	i = start_pos;
	do {
		if (traces [i].bt == bt) {
			traces [i].count += value;
			return 0;
		} else if (!traces [i].bt) {
			traces [i].bt = bt;
			traces [i].count += value;
			return 1;
		}
		/* wrap around */
		if (++i == size)
			i = 0;
	} while (i != start_pos);
	/* should not happen */
	printf ("failed trace store\n");
	return 0;
}

static void
add_trace_bt (BackTrace *bt, TraceDesc *trace, uint64_t value)
{
	int i;
	if (!collect_traces)
		return;
	if (trace->count * 2 >= trace->size) {
		CallContext *n;
		int old_size = trace->size;
		trace->size *= 2;
		if (trace->size == 0)
			trace->size = 4;
		n = (CallContext *) g_calloc (sizeof (CallContext) * trace->size, 1);
		for (i = 0; i < old_size; ++i) {
			if (trace->traces [i].bt)
				add_trace_hashed (n, trace->size, trace->traces [i].bt, trace->traces [i].count);
		}
		if (trace->traces)
			g_free (trace->traces);
		trace->traces = n;
	}
	trace->count += add_trace_hashed (trace->traces, trace->size, bt, value);
}

static BackTrace*
add_trace_thread (ThreadContext *thread, TraceDesc *trace, uint64_t value)
{
	BackTrace *bt;
	int count = thread->stack_id;
	if (!collect_traces)
		return NULL;
	if (count > trace_max)
		count = trace_max;
	bt = add_backtrace (count, thread->stack + thread->stack_id - count);
	add_trace_bt (bt, trace, value);
	return bt;
}

static BackTrace*
add_trace_methods (MethodDesc **methods, int count, TraceDesc *trace, uint64_t value)
{
	BackTrace *bt;
	if (!collect_traces)
		return NULL;
	if (count > trace_max)
		count = trace_max;
	bt = add_backtrace (count, methods);
	add_trace_bt (bt, trace, value);
	return bt;
}

static void
thread_add_root (ThreadContext *ctx, uintptr_t obj, int root_type, uintptr_t extra_info)
{
	if (ctx->num_roots == ctx->size_roots) {
		int new_size = ctx->size_roots * 2;
		if (!new_size)
			new_size = 4;
		ctx->roots = (uintptr_t *) g_realloc (ctx->roots, new_size * sizeof (uintptr_t));
		ctx->roots_extra = (uintptr_t *) g_realloc (ctx->roots_extra, new_size * sizeof (uintptr_t));
		ctx->roots_types = (int *) g_realloc (ctx->roots_types, new_size * sizeof (int));
		ctx->size_roots = new_size;
	}
	ctx->roots_types [ctx->num_roots] = root_type;
	ctx->roots_extra [ctx->num_roots] = extra_info;
	ctx->roots [ctx->num_roots++] = obj;
}

static int
compare_callc (const void *a, const void *b)
{
	const CallContext *A = (const CallContext *)a;
	const CallContext *B = (const CallContext *)b;
	if (B->count == A->count)
		return 0;
	if (B->count < A->count)
		return -1;
	return 1;
}

static void
sort_context_array (TraceDesc* traces)
{
	int i, j;
	for (i = 0, j = 0; i < traces->size; ++i) {
		if (traces->traces [i].bt) {
			traces->traces [j].bt = traces->traces [i].bt;
			traces->traces [j].count = traces->traces [i].count;
			j++;
		}
	}
	mono_qsort (traces->traces, traces->count, sizeof (CallContext), compare_callc);
}

static void
push_method (ThreadContext *thread, MethodDesc *method, uint64_t timestamp)
{
	ensure_thread_stack (thread);
	thread->time_stack [thread->stack_id] = timestamp;
	thread->callee_time_stack [thread->stack_id] = 0;
	thread->stack [thread->stack_id++] = method;
	method->recurse_count++;
}

static void
pop_method (ThreadContext *thread, MethodDesc *method, uint64_t timestamp)
{
	method->recurse_count--;
	if (thread->stack_id > 0 && thread->stack [thread->stack_id - 1] == method) {
		uint64_t tdiff;
		thread->stack_id--;
		method->calls++;
		if (timestamp < thread->time_stack [thread->stack_id])
			fprintf (outfile, "time went backwards for %s\n", method->name);
		tdiff = timestamp - thread->time_stack [thread->stack_id];
		if (thread->callee_time_stack [thread->stack_id] > tdiff)
			fprintf (outfile, "callee time bigger for %s\n", method->name);
		method->self_time += tdiff - thread->callee_time_stack [thread->stack_id];
		method->callee_time += thread->callee_time_stack [thread->stack_id];
		if (thread->stack_id)
			thread->callee_time_stack [thread->stack_id - 1] += tdiff;
		//fprintf (outfile, "method %s took %d\n", method->name, (int)(tdiff/1000));
	} else {
		fprintf (outfile, "unmatched leave at stack pos: %d for method %s\n", thread->stack_id, method->name);
	}
}

typedef struct {
	uint64_t total_time;
	uint64_t max_time;
	int count;
} GCDesc;
static GCDesc gc_info [3];
static uint64_t max_heap_size;
static uint64_t gc_object_moves;
static int gc_resizes;
typedef struct {
	uint64_t created;
	uint64_t destroyed;
	uint64_t live;
	uint64_t max_live;
	TraceDesc traces;
	TraceDesc destroy_traces;
} HandleInfo;
static HandleInfo handle_info [4];

static const char*
gc_event_name (int ev)
{
	switch (ev) {
	case MONO_GC_EVENT_START: return "start";
	case MONO_GC_EVENT_MARK_START: return "mark start";
	case MONO_GC_EVENT_MARK_END: return "mark end";
	case MONO_GC_EVENT_RECLAIM_START: return "reclaim start";
	case MONO_GC_EVENT_RECLAIM_END: return "reclaim end";
	case MONO_GC_EVENT_END: return "end";
	case MONO_GC_EVENT_PRE_STOP_WORLD: return "pre stop";
	case MONO_GC_EVENT_PRE_STOP_WORLD_LOCKED: return "pre stop lock";
	case MONO_GC_EVENT_POST_STOP_WORLD: return "post stop";
	case MONO_GC_EVENT_PRE_START_WORLD: return "pre start";
	case MONO_GC_EVENT_POST_START_WORLD: return "post start";
	case MONO_GC_EVENT_POST_START_WORLD_UNLOCKED: return "post start unlock";
	default:
		return "unknown";
	}
}

static const char*
sync_point_name (int type)
{
	switch (type) {
	case SYNC_POINT_PERIODIC: return "periodic";
	case SYNC_POINT_WORLD_STOP: return "world stop";
	case SYNC_POINT_WORLD_START: return "world start";
	default:
		return "unknown";
	}
}

static uint64_t clause_summary [MONO_EXCEPTION_CLAUSE_FAULT + 1];
static uint64_t throw_count = 0;
static TraceDesc exc_traces;

static const char*
clause_name (int type)
{
	switch (type) {
	case MONO_EXCEPTION_CLAUSE_NONE: return "catch";
	case MONO_EXCEPTION_CLAUSE_FILTER: return "filter";
	case MONO_EXCEPTION_CLAUSE_FINALLY: return "finally";
	case MONO_EXCEPTION_CLAUSE_FAULT: return "fault";
	default: return "invalid";
	}
}

static uint64_t monitor_contention;
static uint64_t monitor_failed;
static uint64_t monitor_acquired;

struct _MonitorDesc {
	MonitorDesc *next;
	uintptr_t objid;
	uintptr_t contentions;
	uint64_t wait_time;
	uint64_t max_wait_time;
	TraceDesc traces;
};

static MonitorDesc* monitor_hash [SMALL_HASH_SIZE] = {0};
static int num_monitors = 0;

static MonitorDesc*
lookup_monitor (uintptr_t objid)
{
	int slot = ((objid >> 3) & 0xffff) % SMALL_HASH_SIZE;
	MonitorDesc *cd = monitor_hash [slot];
	while (cd && cd->objid != objid)
		cd = cd->next;
	if (!cd) {
		cd = (MonitorDesc *) g_calloc (sizeof (MonitorDesc), 1);
		cd->objid = objid;
		cd->next = monitor_hash [slot];
		monitor_hash [slot] = cd;
		num_monitors++;
	}
	return cd;
}

static const char*
monitor_ev_name (int ev)
{
	switch (ev) {
	case MONO_PROFILER_MONITOR_CONTENTION: return "contended";
	case MONO_PROFILER_MONITOR_DONE: return "acquired";
	case MONO_PROFILER_MONITOR_FAIL: return "not taken";
	default: return "invalid";
	}
}

static const char*
get_handle_name (int htype)
{
	switch (htype) {
	case 0: return "weak";
	case 1: return "weaktrack";
	case 2: return "normal";
	case 3: return "pinned";
	default: return "unknown";
	}
}

static const char*
get_root_name (int rtype)
{
	switch (rtype & MONO_PROFILER_GC_ROOT_TYPEMASK) {
	case MONO_PROFILER_GC_ROOT_STACK: return "stack";
	case MONO_PROFILER_GC_ROOT_FINALIZER: return "finalizer";
	case MONO_PROFILER_GC_ROOT_HANDLE: return "handle";
	case MONO_PROFILER_GC_ROOT_OTHER: return "other";
	case MONO_PROFILER_GC_ROOT_MISC: return "misc";
	default: return "unknown";
	}
}

static uint64_t
decode_uleb128 (uint8_t *buf, uint8_t **endbuf)
{
	uint64_t res = 0;
	int shift = 0;

	while (1) {
		uint8_t b = *buf++;
		res |= (((uint64_t) (b & 0x7f)) << shift);

		if (!(b & 0x80))
			break;

		shift += 7;
	}

	*endbuf = buf;

	return res;
}

static intptr_t
decode_sleb128 (uint8_t *buf, uint8_t **endbuf)
{
	uint8_t *p = buf;
	intptr_t res = 0;
	int shift = 0;

	while (1) {
		uint8_t b = *p;
		p++;

		res = res | (((intptr_t) (b & 0x7f)) << shift);
		shift += 7;

		if (!(b & 0x80)) {
			if (shift < sizeof (intptr_t) * 8 && (b & 0x40))
				res |= - ((intptr_t) 1 << shift);

			break;
		}
	}

	*endbuf = p;

	return res;
}

static MethodDesc**
decode_bt (ProfContext *ctx, MethodDesc** sframes, int *size, unsigned char *p, unsigned char **endp, intptr_t ptr_base, intptr_t *method_base)
{
	MethodDesc **frames;
	int i;
	if (ctx->data_version < 13)
		decode_uleb128 (p, &p); /* flags */
	int count = decode_uleb128 (p, &p);
	if (count > *size)
		frames = (MethodDesc **) g_malloc (count * sizeof (void*));
	else
		frames = sframes;
	for (i = 0; i < count; ++i) {
		intptr_t ptrdiff = decode_sleb128 (p, &p);
		if (ctx->data_version > 12) {
			*method_base += ptrdiff;
			frames [i] = lookup_method (*method_base);
		} else {
			frames [i] = lookup_method (ptr_base + ptrdiff);
		}
	}
	*size = count;
	*endp = p;
	return frames;
}

static void
tracked_creation (uintptr_t obj, ClassDesc *cd, uint64_t size, BackTrace *bt, uint64_t timestamp)
{
	int i;
	for (i = 0; i < num_tracked_objects; ++i) {
		if (tracked_objects [i] != obj)
			continue;
		fprintf (outfile, "Object %p created (%s, %" PRIu64 " bytes) at %.3f secs.\n", (void*)obj, cd->name, (guint64) size, (timestamp - startup_time)/1000000000.0);
		if (bt && bt->count) {
			int k;
			for (k = 0; k < bt->count; ++k)
				fprintf (outfile, "\t%s\n", bt->methods [k]->name);
		}
	}
}

static void
track_handle (uintptr_t obj, int htype, uint32_t handle, BackTrace *bt, uint64_t timestamp)
{
	int i;
	for (i = 0; i < num_tracked_objects; ++i) {
		if (tracked_objects [i] != obj)
			continue;
		fprintf (outfile, "Object %p referenced from handle %u at %.3f secs.\n", (void*)obj, handle, (timestamp - startup_time) / 1000000000.0);
		if (bt && bt->count) {
			int k;
			for (k = 0; k < bt->count; ++k)
				fprintf (outfile, "\t%s\n", bt->methods [k]->name);
		}
	}
}

static void
track_move (uintptr_t src, uintptr_t dst)
{
	int i;
	for (i = 0; i < num_tracked_objects; ++i) {
		if (tracked_objects [i] == src)
			fprintf (outfile, "Object %p moved to %p\n", (void*)src, (void*)dst);
		else if (tracked_objects [i] == dst)
			fprintf (outfile, "Object %p moved from %p\n", (void*)dst, (void*)src);
	}
}

static void
track_obj_reference (uintptr_t obj, uintptr_t parent, ClassDesc *cd)
{
	int i;
	for (i = 0; i < num_tracked_objects; ++i) {
		if (tracked_objects [i] == obj)
			fprintf (outfile, "Object %p referenced from %p (%s).\n", (void*)obj, (void*)parent, cd->name);
	}
}

static void
found_object (uintptr_t obj)
{
	num_tracked_objects ++;
	tracked_objects = (uintptr_t *) g_realloc (tracked_objects, num_tracked_objects * sizeof (tracked_objects [0]));
	tracked_objects [num_tracked_objects - 1] = obj;
}

static int num_jit_helpers = 0;
static int jit_helpers_code_size = 0;

static const char*
code_buffer_desc (int type)
{
	switch (type) {
	case MONO_PROFILER_CODE_BUFFER_METHOD:
		return "method";
	case MONO_PROFILER_CODE_BUFFER_METHOD_TRAMPOLINE:
		return "method trampoline";
	case MONO_PROFILER_CODE_BUFFER_UNBOX_TRAMPOLINE:
		return "unbox trampoline";
	case MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE:
		return "imt trampoline";
	case MONO_PROFILER_CODE_BUFFER_GENERICS_TRAMPOLINE:
		return "generics trampoline";
	case MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE:
		return "specific trampoline";
	case MONO_PROFILER_CODE_BUFFER_HELPER:
		return "misc helper";
	case MONO_PROFILER_CODE_BUFFER_MONITOR:
		return "monitor/lock";
	case MONO_PROFILER_CODE_BUFFER_DELEGATE_INVOKE:
		return "delegate invoke";
	case MONO_PROFILER_CODE_BUFFER_EXCEPTION_HANDLING:
		return "exception handling";
	default:
		return "unspecified";
	}
}

#define OBJ_ADDR(diff) ((obj_base + diff) << 3)
#define LOG_TIME(base,diff) /*fprintf("outfile, time %" PRIu64 " + %" PRIu64 " near offset %d\n", base, diff, p - ctx->buf)*/


/* Stats */
#define BUFFER_HEADER_SIZE 48

typedef struct {
	int count, min_size, max_size, bytes;
} EventStat;

static int buffer_count;
static EventStat stats [256];

static void
record_event_stats (int type, int size)
{
	++stats [type].count;
	if (!stats [type].min_size)
		stats [type].min_size = size;
	stats [type].min_size = MIN (stats [type].min_size, size);
	stats [type].max_size = MAX (stats [type].max_size, size);
	stats [type].bytes += size;
}

static int
decode_buffer (ProfContext *ctx)
{
	unsigned char *p;
	unsigned char *end;
	intptr_t thread_id;
	intptr_t ptr_base;
	intptr_t obj_base;
	intptr_t method_base;
	uint64_t time_base;
	uint64_t file_offset;
	int len, i;
	ThreadContext *thread;

#ifdef HAVE_SYS_ZLIB
	if (ctx->gzfile)
		file_offset = gztell (ctx->gzfile);
	else
#endif
		file_offset = ftell (ctx->file);
	if (!load_data (ctx, 48))
		return 0;
	p = ctx->buf;
	if (read_int32 (p) != BUF_ID) {
		fprintf (outfile, "Incorrect buffer id: 0x%x\n", read_int32 (p));
		for (i = 0; i < 48; ++i) {
			fprintf (outfile, "0x%x%s", p [i], i % 8?" ":"\n");
		}
		return 0;
	}
	len = read_int32 (p + 4);
	time_base = read_int64 (p + 8);
	ptr_base = read_int64 (p + 16);
	obj_base = read_int64 (p + 24);
	thread_id = read_int64 (p + 32);
	method_base = read_int64 (p + 40);
	if (debug)
		fprintf (outfile, "buf: thread:%" G_GSIZE_FORMAT "x, len: %d, time: %" PRIu64 ", file offset: %" PRIu64 "\n", thread_id, len, (guint64) time_base, (guint64) file_offset);
	thread = load_thread (ctx, thread_id);
	if (!load_data (ctx, len))
		return 0;

	++buffer_count;

	if (!startup_time) {
		startup_time = time_base;
		if (use_time_filter) {
			time_from += startup_time;
			time_to += startup_time;
		}
	}
	for (i = 0; i < thread->stack_id; ++i)
		thread->stack [i]->recurse_count++;
	p = ctx->buf;
	end = p + len;
	while (p < end) {
		unsigned char *start = p;
		unsigned char event = *p;
		switch (*p & 0xf) {
		case TYPE_GC: {
			int subtype = *p & 0xf0;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			if (subtype == TYPE_GC_RESIZE) {
				uint64_t new_size = decode_uleb128 (p, &p);
				if (debug)
					fprintf (outfile, "gc heap resized to %" PRIu64 "\n", (guint64) new_size);
				gc_resizes++;
				if (new_size > max_heap_size)
					max_heap_size = new_size;
			} else if (subtype == TYPE_GC_EVENT) {
				uint64_t ev;
				if (ctx->data_version > 12)
					ev = *p++;
				else
					ev = decode_uleb128 (p, &p);
				int gen;
				if (ctx->data_version > 12)
					gen = *p++;
				else
					gen = decode_uleb128 (p, &p);
				if (debug)
					fprintf (outfile, "gc event for gen%d: %s at %" PRIu64 " (thread: 0x%" G_GSIZE_FORMAT "x)\n", gen, gc_event_name (ev), (guint64) time_base, thread->thread_id);
				if (gen > 2) {
					fprintf (outfile, "incorrect gc gen: %d\n", gen);
					break;
				}
				if (ev == MONO_GC_EVENT_START) {
					thread->gc_start_times [gen] = time_base;
					gc_info [gen].count++;
				} else if (ev == MONO_GC_EVENT_END) {
					tdiff = time_base - thread->gc_start_times [gen];
					gc_info [gen].total_time += tdiff;
					if (tdiff > gc_info [gen].max_time)
						gc_info [gen].max_time = tdiff;
				}
			} else if (subtype == TYPE_GC_MOVE) {
				int j, num = decode_uleb128 (p, &p);
				gc_object_moves += num / 2;
				for (j = 0; j < num; j += 2) {
					intptr_t obj1diff = decode_sleb128 (p, &p);
					intptr_t obj2diff = decode_sleb128 (p, &p);
					if (num_tracked_objects)
						track_move (OBJ_ADDR (obj1diff), OBJ_ADDR (obj2diff));
					if (debug) {
						fprintf (outfile, "moved obj %p to %p\n", (void*)OBJ_ADDR (obj1diff), (void*)OBJ_ADDR (obj2diff));
					}
				}
			} else if (subtype == TYPE_GC_HANDLE_CREATED || subtype == TYPE_GC_HANDLE_CREATED_BT) {
				int has_bt = subtype == TYPE_GC_HANDLE_CREATED_BT;
				int num_bt = 0;
				MethodDesc *sframes [8];
				MethodDesc **frames = sframes;
				int htype = decode_uleb128 (p, &p);
				uint32_t handle = decode_uleb128 (p, &p);
				intptr_t objdiff = decode_sleb128 (p, &p);
				if (has_bt) {
					num_bt = 8;
					frames = decode_bt (ctx, sframes, &num_bt, p, &p, ptr_base, &method_base);
					if (!frames) {
						fprintf (outfile, "Cannot load backtrace\n");
						return 0;
					}
				}
				if (htype > 3)
					return 0;
				if ((thread_filter && thread_filter == thread->thread_id) || (time_base >= time_from && time_base < time_to)) {
					handle_info [htype].created++;
					handle_info [htype].live++;
					if (handle_info [htype].live > handle_info [htype].max_live)
						handle_info [htype].max_live = handle_info [htype].live;
					BackTrace *bt;
					if (has_bt)
						bt = add_trace_methods (frames, num_bt, &handle_info [htype].traces, 1);
					else
						bt = add_trace_thread (thread, &handle_info [htype].traces, 1);
					if (num_tracked_objects)
						track_handle (OBJ_ADDR (objdiff), htype, handle, bt, time_base);
				}
				if (debug)
					fprintf (outfile, "handle (%s) %u created for object %p\n", get_handle_name (htype), handle, (void*)OBJ_ADDR (objdiff));
				if (frames != sframes)
					g_free (frames);
			} else if (subtype == TYPE_GC_HANDLE_DESTROYED || subtype == TYPE_GC_HANDLE_DESTROYED_BT) {
				int has_bt = subtype == TYPE_GC_HANDLE_DESTROYED_BT;
				int num_bt = 0;
				MethodDesc *sframes [8];
				MethodDesc **frames = sframes;
				int htype = decode_uleb128 (p, &p);
				uint32_t handle = decode_uleb128 (p, &p);
				if (has_bt) {
					num_bt = 8;
					frames = decode_bt (ctx, sframes, &num_bt, p, &p, ptr_base, &method_base);
					if (!frames) {
						fprintf (outfile, "Cannot load backtrace\n");
						return 0;
					}
				}
				if (htype > 3)
					return 0;
				if ((thread_filter && thread_filter == thread->thread_id) || (time_base >= time_from && time_base < time_to)) {
					handle_info [htype].destroyed ++;
					handle_info [htype].live--;
					BackTrace *bt;
					if (has_bt)
						bt = add_trace_methods (frames, num_bt, &handle_info [htype].destroy_traces, 1);
					else
						bt = add_trace_thread (thread, &handle_info [htype].destroy_traces, 1);
					/* TODO: track_handle_free () - would need to record and keep track of the associated object address... */
				}
				if (debug)
					fprintf (outfile, "handle (%s) %u destroyed\n", get_handle_name (htype), handle);
				if (frames != sframes)
					g_free (frames);
			} else if (subtype == TYPE_GC_FINALIZE_START) {
				// TODO: Generate a finalizer report based on these events.
				if (debug)
					fprintf (outfile, "gc finalizer queue being processed at %" PRIu64 "\n", (guint64) time_base);
			} else if (subtype == TYPE_GC_FINALIZE_END) {
				if (debug)
					fprintf (outfile, "gc finalizer queue finished processing at %" PRIu64 "\n", (guint64) time_base);
			} else if (subtype == TYPE_GC_FINALIZE_OBJECT_START) {
				intptr_t objdiff = decode_sleb128 (p, &p);
				if (debug)
					fprintf (outfile, "gc finalizing object %p at %" PRIu64 "\n", (void *) OBJ_ADDR (objdiff), (guint64) time_base);
			} else if (subtype == TYPE_GC_FINALIZE_OBJECT_END) {
				intptr_t objdiff = decode_sleb128 (p, &p);
				if (debug)
					fprintf (outfile, "gc finalized object %p at %" PRIu64 "\n", (void *) OBJ_ADDR (objdiff), (guint64) time_base);
			}
			break;
		}
		case TYPE_METADATA: {
			int subtype = *p & 0xf0;
			const char *load_str = subtype == TYPE_END_LOAD ? "loaded" : "unloaded";
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			int mtype = *p++;
			intptr_t ptrdiff = decode_sleb128 (p, &p);
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			if (mtype == TYPE_CLASS) {
				intptr_t imptrdiff = decode_sleb128 (p, &p);
				if (ctx->data_version < 13)
					decode_uleb128 (p, &p); /* flags */
				if (debug)
					fprintf (outfile, "%s class %p (%s in %p) at %" PRIu64 "\n", load_str, (void*)(ptr_base + ptrdiff), p, (void*)(ptr_base + imptrdiff), (guint64) time_base);
				add_class (ptr_base + ptrdiff, (char*)p);
				while (*p) p++;
				p++;
			} else if (mtype == TYPE_IMAGE) {
				if (ctx->data_version < 13)
					decode_uleb128 (p, &p); /* flags */
				if (debug)
					fprintf (outfile, "%s image %p (%s) at %" PRIu64 "\n", load_str, (void*)(ptr_base + ptrdiff), p, (guint64) time_base);
				if (subtype == TYPE_END_LOAD)
					add_image (ptr_base + ptrdiff, (char*)p);
				while (*p) p++;
				p++;
				if (ctx->data_version >= 16 && subtype == TYPE_END_LOAD) {
					while (*p) p++; // mvid
					p++;
				}
			} else if (mtype == TYPE_ASSEMBLY) {
				if (ctx->data_version > 13)
					decode_sleb128 (p, &p); // image
				if (ctx->data_version < 13)
					decode_uleb128 (p, &p); /* flags */
				if (debug)
					fprintf (outfile, "%s assembly %p (%s) at %" PRIu64 "\n", load_str, (void*)(ptr_base + ptrdiff), p, (guint64) time_base);
				if (subtype == TYPE_END_LOAD)
					add_assembly (ptr_base + ptrdiff, (char*)p);
				while (*p) p++;
				p++;
			} else if (mtype == TYPE_DOMAIN) {
				if (ctx->data_version < 13)
					decode_uleb128 (p, &p); /* flags */
				DomainContext *nd = get_domain (ctx, ptr_base + ptrdiff);
				/* no subtype means it's a name event, rather than start/stop */
				if (subtype == 0)
					nd->friendly_name = pstrdup ((char *) p);
				if (debug) {
					if (subtype == 0)
						fprintf (outfile, "domain %p named at %" PRIu64 ": %s\n", (void *) (ptr_base + ptrdiff), (guint64) time_base, p);
					else
						fprintf (outfile, "%s thread %p at %" PRIu64 "\n", load_str, (void *) (ptr_base + ptrdiff), (guint64) time_base);
				}
				if (subtype == 0) {
					while (*p) p++;
					p++;
				}
			} else if (mtype == TYPE_CONTEXT) {
				if (ctx->data_version < 13)
					decode_uleb128 (p, &p); /* flags */
				intptr_t domaindiff = decode_sleb128 (p, &p);
				if (debug)
					fprintf (outfile, "%s context %p (%p) at %" PRIu64 "\n", load_str, (void*)(ptr_base + ptrdiff), (void *) (ptr_base + domaindiff), (guint64) time_base);
				if (subtype == TYPE_END_LOAD)
					get_remctx (ctx, ptr_base + ptrdiff)->domain_id = ptr_base + domaindiff;
			} else if (mtype == TYPE_THREAD) {
				if (ctx->data_version < 13)
					decode_uleb128 (p, &p); /* flags */
				ThreadContext *nt = get_thread (ctx, ptr_base + ptrdiff);
				/* no subtype means it's a name event, rather than start/stop */
				if (subtype == 0)
					nt->name = pstrdup ((char*)p);
				if (debug) {
					if (subtype == 0)
						fprintf (outfile, "thread %p named at %" PRIu64 ": %s\n", (void*)(ptr_base + ptrdiff), (guint64) time_base, p);
					else
						fprintf (outfile, "%s thread %p at %" PRIu64 "\n", load_str, (void *) (ptr_base + ptrdiff), (guint64) time_base);
				}
				if (subtype == 0) {
					while (*p) p++;
					p++;
				}
			} else if (mtype == TYPE_VTABLE) {
				intptr_t domaindiff = decode_sleb128 (p, &p);
				intptr_t classdiff = decode_sleb128 (p, &p);
				if (debug)
					fprintf (outfile, "vtable %p for class %p in domain %p at %" PRIu64 "\n", (void *) (ptrdiff + ptr_base), (void *) (classdiff + ptr_base), (void *) (domaindiff + ptr_base), (guint64) time_base);
				add_vtable (ptr_base + ptrdiff, ptr_base + classdiff);
			}
			break;
		}
		case TYPE_ALLOC: {
			int has_bt = *p & TYPE_ALLOC_BT;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			intptr_t ptrdiff = decode_sleb128 (p, &p);
			intptr_t objdiff = decode_sleb128 (p, &p);
			uint64_t len;
			int num_bt = 0;
			MethodDesc* sframes [8];
			MethodDesc** frames = sframes;
			ClassDesc *cd;
			if (ctx->data_version > 14) {
				VTableDesc *vt = lookup_vtable (ptr_base + ptrdiff);
				cd = vt->klass;
			} else
				cd = lookup_class (ptr_base + ptrdiff);
			len = decode_uleb128 (p, &p);
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			if (debug)
				fprintf (outfile, "alloced object %p, size %" PRIu64 " (%s) at %" PRIu64 "\n", (void*)OBJ_ADDR (objdiff), (guint64) len, cd->name, (guint64) time_base);
			if (has_bt) {
				num_bt = 8;
				frames = decode_bt (ctx, sframes, &num_bt, p, &p, ptr_base, &method_base);
				if (!frames) {
					fprintf (outfile, "Cannot load backtrace\n");
					return 0;
				}
			}
			if ((thread_filter && thread_filter == thread->thread_id) || (time_base >= time_from && time_base < time_to)) {
				BackTrace *bt;
				cd->allocs++;
				cd->alloc_size += len;
				if (has_bt)
					bt = add_trace_methods (frames, num_bt, &cd->traces, len);
				else
					bt = add_trace_thread (thread, &cd->traces, len);
				if (find_size && len >= find_size) {
					if (!find_name || strstr (cd->name, find_name))
						found_object (OBJ_ADDR (objdiff));
				} else if (!find_size && find_name && strstr (cd->name, find_name)) {
					found_object (OBJ_ADDR (objdiff));
				}
				if (num_tracked_objects)
					tracked_creation (OBJ_ADDR (objdiff), cd, len, bt, time_base);
			}
			if (frames != sframes)
				g_free (frames);
			break;
		}
		case TYPE_METHOD: {
			int subtype = *p & 0xf0;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			int64_t ptrdiff = decode_sleb128 (p, &p);
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			method_base += ptrdiff;
			if (subtype == TYPE_JIT) {
				intptr_t codediff = decode_sleb128 (p, &p);
				int codelen = decode_uleb128 (p, &p);
				MethodDesc *jitted_method;
				if (debug)
					fprintf (outfile, "jitted method %p (%s), size: %d, code: %p\n", (void*)(method_base), p, codelen, (void*)(ptr_base + codediff));
				jitted_method = add_method (method_base, (char*)p, ptr_base + codediff, codelen);
				if (!(time_base >= time_from && time_base < time_to))
					jitted_method->ignore_jit = 1;
				while (*p) p++;
				p++;
			} else {
				MethodDesc *method;
				if ((thread_filter && thread_filter != thread->thread_id))
					break;
				if (!(time_base >= time_from && time_base < time_to))
					break;
				method = lookup_method (method_base);
				if (subtype == TYPE_ENTER) {
					add_trace_thread (thread, &method->traces, 1);
					push_method (thread, method, time_base);
				} else {
					pop_method (thread, method, time_base);
				}
				if (debug)
					fprintf (outfile, "%s method %s\n", subtype == TYPE_ENTER? "enter": subtype == TYPE_EXC_LEAVE? "exleave": "leave", method->name);
			}
			break;
		}
		case TYPE_HEAP: {
			int subtype = *p & 0xf0;
			if (subtype == TYPE_HEAP_OBJECT) {
				HeapObjectDesc *ho = NULL;
				int i;
				intptr_t objdiff;
				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p + 1, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
					objdiff = decode_sleb128 (p, &p);
				} else
					objdiff = decode_sleb128 (p + 1, &p);
				intptr_t ptrdiff = decode_sleb128 (p, &p);
				uint64_t size = decode_uleb128 (p, &p);
				if (ctx->data_version >= 16)
					p++; // generation
				uintptr_t num = decode_uleb128 (p, &p);
				uintptr_t ref_offset = 0;
				uintptr_t last_obj_offset = 0;
				ClassDesc *cd;
				if (ctx->data_version > 14) {
					VTableDesc *vt = lookup_vtable (ptr_base + ptrdiff);
					cd = vt->klass;
				} else
					cd = lookup_class (ptr_base + ptrdiff);
				if (size) {
					HeapClassDesc *hcd = add_heap_shot_class (thread->current_heap_shot, cd, size);
					if (collect_traces) {
						ho = alloc_heap_obj (OBJ_ADDR (objdiff), hcd, num);
						add_heap_shot_obj (thread->current_heap_shot, ho);
						ref_offset = 0;
					}
				} else {
					if (collect_traces)
						ho = heap_shot_obj_add_refs (thread->current_heap_shot, OBJ_ADDR (objdiff), num, &ref_offset);
				}
				for (i = 0; i < num; ++i) {
					/* FIXME: use object distance to measure how good
					 * the GC is at keeping related objects close
					 */
					uintptr_t offset = ctx->data_version > 1? last_obj_offset + decode_uleb128 (p, &p): -1;
					intptr_t obj1diff = decode_sleb128 (p, &p);
					last_obj_offset = offset;
					if (collect_traces)
						ho->refs [ref_offset + i] = OBJ_ADDR (obj1diff);
					if (num_tracked_objects)
						track_obj_reference (OBJ_ADDR (obj1diff), OBJ_ADDR (objdiff), cd);
				}
				if (debug && size)
					fprintf (outfile, "traced object %p, size %" PRIu64 " (%s), refs: %" G_GSIZE_FORMAT "d\n", (void*)OBJ_ADDR (objdiff), (guint64) size, cd->name, num);
			} else if (subtype == TYPE_HEAP_ROOT) {
				uintptr_t num;
				if (ctx->data_version > 14) {
					int i;
					uint64_t tdiff = decode_uleb128 (p + 1, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
					num = decode_uleb128 (p, &p);
					for (i = 0; i < num; ++i) {
						intptr_t ptrdiff = decode_sleb128 (p, &p);
						intptr_t objdiff = decode_sleb128 (p, &p);

						if (debug)
							fprintf (outfile, "root object %p at address %p\n", (void*)OBJ_ADDR (objdiff), (void *) (ptr_base + ptrdiff));
						if (collect_traces)
							thread_add_root (thread, OBJ_ADDR (objdiff), MONO_PROFILER_GC_ROOT_MISC, 0);
					}
				} else {
					if (ctx->data_version > 12) {
						uint64_t tdiff = decode_uleb128 (p + 1, &p);
						LOG_TIME (time_base, tdiff);
						time_base += tdiff;
						num = decode_uleb128 (p, &p);
					} else
						num = decode_uleb128 (p + 1, &p);
					uintptr_t gc_num G_GNUC_UNUSED = decode_uleb128 (p, &p);
					int i;
					for (i = 0; i < num; ++i) {
						intptr_t objdiff = decode_sleb128 (p, &p);
						int root_type;
						if (ctx->data_version == 13)
							root_type = *p++;
						else
							root_type = decode_uleb128 (p, &p);
						/* we just discard the extra info for now */
						uintptr_t extra_info = decode_uleb128 (p, &p);
						if (debug)
							fprintf (outfile, "object %p is a %s root\n", (void*)OBJ_ADDR (objdiff), get_root_name (root_type));
						if (collect_traces)
							thread_add_root (thread, OBJ_ADDR (objdiff), root_type, extra_info);
					}
				}
			} else if (subtype == TYPE_HEAP_ROOT_REGISTER) {
				uint64_t tdiff = decode_uleb128 (p + 1, &p);
				LOG_TIME (time_base, tdiff);
				time_base += tdiff;

				int64_t ptrdiff = decode_sleb128 (p, &p);
				uint64_t size = decode_uleb128 (p, &p);
				int type = *p++;
				int64_t keydiff = decode_sleb128 (p, &p);
				char *desc = (char*) p;
				while (*p++);

				if (debug)
					fprintf (outfile, "root register address %p size %" PRId64 " type %d key %p name %s\n", (void *) (ptr_base + ptrdiff), (guint64) size, type, (void *) (ptr_base + keydiff), desc);
			} else if (subtype == TYPE_HEAP_ROOT_UNREGISTER) {
				uint64_t tdiff = decode_uleb128 (p + 1, &p);
				LOG_TIME (time_base, tdiff);
				time_base += tdiff;
				int64_t ptrdiff = decode_sleb128 (p, &p);

				if (debug)
					fprintf (outfile, "root unregister address %p\n", (void *) (ptr_base + ptrdiff));
			} else if (subtype == TYPE_HEAP_END) {
				uint64_t tdiff = decode_uleb128 (p + 1, &p);
				LOG_TIME (time_base, tdiff);
				time_base += tdiff;
				if (debug)
					fprintf (outfile, "heap shot end\n");
				if (collect_traces) {
					HeapShot *hs = thread->current_heap_shot;
					if (hs && thread->num_roots) {
						/* transfer the root ownershipt to the heapshot */
						hs->num_roots = thread->num_roots;
						hs->roots = thread->roots;
						hs->roots_extra = thread->roots_extra;
						hs->roots_types = thread->roots_types;
					} else {
						g_free (thread->roots);
						g_free (thread->roots_extra);
						g_free (thread->roots_types);
					}
					thread->num_roots = 0;
					thread->size_roots = 0;
					thread->roots = NULL;
					thread->roots_extra = NULL;
					thread->roots_types = NULL;
					heap_shot_resolve_reverse_refs (hs);
					heap_shot_mark_objects (hs);
					heap_shot_free_objects (hs);
				}
				thread->current_heap_shot = NULL;
			} else if (subtype == TYPE_HEAP_START) {
				uint64_t tdiff = decode_uleb128 (p + 1, &p);
				LOG_TIME (time_base, tdiff);
				time_base += tdiff;
				if (debug)
					fprintf (outfile, "heap shot start\n");
				thread->current_heap_shot = new_heap_shot (time_base);
			}
			break;
		}
		case TYPE_MONITOR: {
			int has_bt = *p & TYPE_MONITOR_BT;
			int event;
			if (ctx->data_version < 13)
				event = (*p >> 4) & 0x3;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			if (ctx->data_version > 13)
				event = *p++;
			intptr_t objdiff = decode_sleb128 (p, &p);
			MethodDesc* sframes [8];
			MethodDesc** frames = sframes;
			int record;
			int num_bt = 0;
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			record = (!thread_filter || thread_filter == thread->thread_id);
			if (!(time_base >= time_from && time_base < time_to))
				record = 0;
			MonitorDesc *mdesc = lookup_monitor (OBJ_ADDR (objdiff));
			if (event == MONO_PROFILER_MONITOR_CONTENTION) {
				if (record) {
					monitor_contention++;
					mdesc->contentions++;
					thread->monitor = mdesc;
					thread->contention_start = time_base;
				}
			} else if (event == MONO_PROFILER_MONITOR_FAIL) {
				if (record) {
					monitor_failed++;
					if (thread->monitor && thread->contention_start) {
						uint64_t wait_time = time_base - thread->contention_start;
						if (wait_time > thread->monitor->max_wait_time)
							thread->monitor->max_wait_time = wait_time;
						thread->monitor->wait_time += wait_time;
						thread->monitor = NULL;
						thread->contention_start = 0;
					}
				}
			} else if (event == MONO_PROFILER_MONITOR_DONE) {
				if (record) {
					monitor_acquired++;
					if (thread->monitor && thread->contention_start) {
						uint64_t wait_time = time_base - thread->contention_start;
						if (wait_time > thread->monitor->max_wait_time)
							thread->monitor->max_wait_time = wait_time;
						thread->monitor->wait_time += wait_time;
						thread->monitor = NULL;
						thread->contention_start = 0;
					}
				}
			}
			if (has_bt) {
				num_bt = 8;
				frames = decode_bt (ctx, sframes, &num_bt, p, &p, ptr_base, &method_base);
				if (!frames) {
					fprintf (outfile, "Cannot load backtrace\n");
					return 0;
				}
				if (record && event == MONO_PROFILER_MONITOR_CONTENTION)
					add_trace_methods (frames, num_bt, &mdesc->traces, 1);
			} else {
				if (record)
					add_trace_thread (thread, &mdesc->traces, 1);
			}
			if (debug)
				fprintf (outfile, "monitor %s for object %p\n", monitor_ev_name (event), (void*)OBJ_ADDR (objdiff));
			if (frames != sframes)
				g_free (frames);
			break;
		}
		case TYPE_EXCEPTION: {
			int subtype = *p & 0x70;
			int has_bt = *p & TYPE_THROW_BT;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			MethodDesc* sframes [8];
			MethodDesc** frames = sframes;
			int record;
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			record = (!thread_filter || thread_filter == thread->thread_id);
			if (!(time_base >= time_from && time_base < time_to))
				record = 0;
			if (subtype == TYPE_CLAUSE) {
				int clause_type;
				if (ctx->data_version > 12)
					clause_type = *p++;
				else
					clause_type = decode_uleb128 (p, &p);
				int clause_num = decode_uleb128 (p, &p);
				int64_t ptrdiff = decode_sleb128 (p, &p);
				method_base += ptrdiff;
				if (ctx->data_version > 13)
					decode_uleb128 (p, &p); // exception object
				if (record)
					clause_summary [clause_type]++;
				if (debug)
					fprintf (outfile, "clause %s (%d) in method %s\n", clause_name (clause_type), clause_num, lookup_method (method_base)->name);
			} else {
				intptr_t objdiff = decode_sleb128 (p, &p);
				if (record)
					throw_count++;
				if (has_bt) {
					has_bt = 8;
					frames = decode_bt (ctx, sframes, &has_bt, p, &p, ptr_base, &method_base);
					if (!frames) {
						fprintf (outfile, "Cannot load backtrace\n");
						return 0;
					}
					if (record)
						add_trace_methods (frames, has_bt, &exc_traces, 1);
				} else {
					if (record)
						add_trace_thread (thread, &exc_traces, 1);
				}
				if (frames != sframes)
					g_free (frames);
				if (debug)
					fprintf (outfile, "throw %p\n", (void*)OBJ_ADDR (objdiff));
			}
			break;
		}
		case TYPE_RUNTIME: {
			int subtype = *p & 0xf0;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			if (subtype == TYPE_JITHELPER) {
				int type;
				if (ctx->data_version > 12)
					type = *p++;
				else
					type = decode_uleb128 (p, &p);
				if (ctx->data_version < 14)
					--type;
				intptr_t codediff = decode_sleb128 (p, &p);
				int codelen = decode_uleb128 (p, &p);
				const char *name;
				if (type == MONO_PROFILER_CODE_BUFFER_SPECIFIC_TRAMPOLINE) {
					name = (const char *)p;
					while (*p) p++;
						p++;
				} else {
					name = code_buffer_desc (type);
				}
				num_jit_helpers++;
				jit_helpers_code_size += codelen;
				if (debug)
					fprintf (outfile, "jit helper %s, size: %d, code: %p\n", name, codelen, (void*)(ptr_base + codediff));
			}
			break;
		}
		case TYPE_SAMPLE: {
			int subtype = *p & 0xf0;
			if (subtype == TYPE_SAMPLE_HIT) {
				int i;
				int sample_type;
				uint64_t tstamp;
				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p + 1, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
					if (ctx->data_version < 14)
						sample_type = *p++;
					else
						sample_type = SAMPLE_CYCLES;
					tstamp = time_base;
				} else {
					sample_type = decode_uleb128 (p + 1, &p);
					tstamp = decode_uleb128 (p, &p);
				}
				void *tid = (void *) thread_id;
				if (ctx->data_version > 10)
					tid = (void *) (ptr_base + decode_sleb128 (p, &p));
				int count = decode_uleb128 (p, &p);
				for (i = 0; i < count; ++i) {
					uintptr_t ip = ptr_base + decode_sleb128 (p, &p);
					if ((tstamp >= time_from && tstamp < time_to))
						add_stat_sample (sample_type, ip);
					if (debug)
						fprintf (outfile, "sample hit, type: %d at %p for thread %p\n", sample_type, (void*)ip, tid);
				}
				if (ctx->data_version > 5) {
					count = decode_uleb128 (p, &p);
					for (i = 0; i < count; ++i) {
						MethodDesc *method;
						int64_t ptrdiff = decode_sleb128 (p, &p);
						method_base += ptrdiff;
						method = lookup_method (method_base);
						if (debug)
							fprintf (outfile, "sample hit bt %d: %s\n", i, method->name);
						if (ctx->data_version < 13) {
							decode_sleb128 (p, &p); /* il offset */
							decode_sleb128 (p, &p); /* native offset */
						}
					}
				}
			} else if (subtype == TYPE_SAMPLE_USYM) {
				/* un unmanaged symbol description */
				uintptr_t addr;
				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p + 1, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
					addr = ptr_base + decode_sleb128 (p, &p);
				} else
					addr = ptr_base + decode_sleb128 (p + 1, &p);
				uintptr_t size = decode_uleb128 (p, &p);
				char *name;
				name = pstrdup ((char*)p);
				add_unmanaged_symbol (addr, name, size);
				if (debug)
					fprintf (outfile, "unmanaged symbol %s at %p\n", name, (void*)addr);
				while (*p) p++;
				p++;
			} else if (subtype == TYPE_SAMPLE_UBIN) {
				/* un unmanaged binary loaded in memory */
				uint64_t tdiff = decode_uleb128 (p + 1, &p);
				uintptr_t addr = decode_sleb128 (p, &p);
				if (ctx->data_version > 13)
					addr += ptr_base;
				uint64_t offset G_GNUC_UNUSED = decode_uleb128 (p, &p);
				uintptr_t size = decode_uleb128 (p, &p);
				char *name;
				LOG_TIME (time_base, tdiff);
				time_base += tdiff;
				name = pstrdup ((char*)p);
				add_unmanaged_binary (addr, name, size);
				if (debug)
					fprintf (outfile, "unmanaged binary %s at %p\n", name, (void*)addr);
				while (*p) p++;
				p++;
			} else if (subtype == TYPE_SAMPLE_COUNTERS_DESC) {
				uint64_t i, len;
				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p + 1, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
					len = decode_uleb128 (p, &p);
				} else
					len = decode_uleb128 (p + 1, &p);
				for (i = 0; i < len; i++) {
					uint64_t type, unit, variance, index;
					uint64_t section = decode_uleb128 (p, &p);
					char *section_str, *name;
					if (section != MONO_COUNTER_PERFCOUNTERS) {
						section_str = (char*) section_name (section);
					} else {
						section_str = pstrdup ((char*)p);
						while (*p++);
					}
					name = pstrdup ((char*)p);
					while (*p++);
					if (ctx->data_version > 12 && ctx->data_version < 15) {
						type = *p++;
						unit = *p++;
						variance = *p++;
					} else {
						type = decode_uleb128 (p, &p);
						unit = decode_uleb128 (p, &p);
						variance = decode_uleb128 (p, &p);
					}
					index = decode_uleb128 (p, &p);
					add_counter (section_str, name, (int)type, (int)unit, (int)variance, (int)index);
				}
			} else if (subtype == TYPE_SAMPLE_COUNTERS) {
				int i;
				CounterValue *value, *previous = NULL;
				CounterList *list;
				uint64_t timestamp; // milliseconds since startup
				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p + 1, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
					timestamp = (time_base - startup_time) / 1000 / 1000;
				} else
					timestamp = decode_uleb128 (p + 1, &p);
				uint64_t time_between = timestamp / 1000 * 1000 * 1000 * 1000 + startup_time;
				while (1) {
					uint64_t type, index = decode_uleb128 (p, &p);
					if (index == 0)
						break;

					for (list = counters; list; list = list->next) {
						if (list->counter->index == (int)index) {
							previous = list->counter->values_last;
							break;
						}
					}

					if (ctx->data_version > 12 && ctx->data_version < 15)
						type = *p++;
					else
						type = decode_uleb128 (p, &p);

					value = (CounterValue *) g_calloc (1, sizeof (CounterValue));
					value->timestamp = timestamp;

					switch (type) {
					case MONO_COUNTER_INT:
#if SIZEOF_VOID_P == 4
					case MONO_COUNTER_WORD:
#endif
						value->buffer = (unsigned char *)g_malloc (sizeof (int32_t));
						*(int32_t*)value->buffer = (int32_t)decode_sleb128 (p, &p) + (previous ? (*(int32_t*)previous->buffer) : 0);
						break;
					case MONO_COUNTER_UINT:
						value->buffer = (unsigned char *) g_malloc (sizeof (uint32_t));
						*(uint32_t*)value->buffer = (uint32_t)decode_uleb128 (p, &p) + (previous ? (*(uint32_t*)previous->buffer) : 0);
						break;
					case MONO_COUNTER_LONG:
#if SIZEOF_VOID_P == 8
					case MONO_COUNTER_WORD:
#endif
					case MONO_COUNTER_TIME_INTERVAL:
						value->buffer = (unsigned char *) g_malloc (sizeof (int64_t));
						*(int64_t*)value->buffer = (int64_t)decode_sleb128 (p, &p) + (previous ? (*(int64_t*)previous->buffer) : 0);
						break;
					case MONO_COUNTER_ULONG:
						value->buffer = (unsigned char *) g_malloc (sizeof (uint64_t));
						*(uint64_t*)value->buffer = (uint64_t)decode_uleb128 (p, &p) + (previous ? (*(uint64_t*)previous->buffer) : 0);
						break;
					case MONO_COUNTER_DOUBLE:
						value->buffer = (unsigned char *) g_malloc (sizeof (double));
#if TARGET_BYTE_ORDER == G_LITTLE_ENDIAN
						for (i = 0; i < sizeof (double); i++)
#else
						for (i = sizeof (double) - 1; i >= 0; i--)
#endif
							value->buffer[i] = *p++;
						break;
					case MONO_COUNTER_STRING:
						if (*p++ == 0) {
							value->buffer = NULL;
						} else {
							value->buffer = (unsigned char*) pstrdup ((char*)p);
							while (*p++);
						}
						break;
					}
					if (time_between >= time_from && time_between <= time_to)
						add_counter_value (index, value);
				}
			} else {
				return 0;
			}
			break;
		}
		case TYPE_COVERAGE:{
			int subtype = *p & 0xf0;
			switch (subtype) {
			case TYPE_COVERAGE_METHOD: {
				p++;

				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
				}

				while (*p) p++;
				p++;
				while (*p) p++;
				p++;
				while (*p) p++;
				p++;
				while (*p) p++;
				p++;
				while (*p) p++;
				p++;

				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);

				break;
			}
			case TYPE_COVERAGE_STATEMENT: {
				p++;

				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
				}

				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);

				break;
			}
			case TYPE_COVERAGE_ASSEMBLY: {
				p++;

				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
				}

				while (*p) p++;
				p++;
				while (*p) p++;
				p++;
				while (*p) p++;
				p++;

				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);

				break;
			}
			case TYPE_COVERAGE_CLASS: {
				p++;

				if (ctx->data_version > 12) {
					uint64_t tdiff = decode_uleb128 (p, &p);
					LOG_TIME (time_base, tdiff);
					time_base += tdiff;
				}

				while (*p) p++;
				p++;
				while (*p) p++;
				p++;

				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);
				decode_uleb128 (p, &p);

				break;
			}

			default:
				break;
			}
			break;
		}
		case TYPE_META: {
			int subtype = *p & 0xf0;
			uint64_t tdiff = decode_uleb128 (p + 1, &p);
			LOG_TIME (time_base, tdiff);
			time_base += tdiff;
			if (subtype == TYPE_SYNC_POINT) {
				int type = *p++;
				if (debug)
					fprintf (outfile, "sync point %i (%s)\n", type, sync_point_name (type));
			} else if (subtype == TYPE_AOT_ID) {
				if (debug)
					fprintf (outfile, "aot id %s\n", p);
				while (*p) p++; // aot id
				p++;
			}
			break;
		}
		default:
			fprintf (outfile, "unhandled profiler event: 0x%x at file offset: %" PRIu64 " + %" PRId64 " (len: %d\n)\n", *p, (guint64) file_offset, (gint64) (p - ctx->buf), len);
			exit (1);
		}
		record_event_stats (event, p - start);
	}
	thread->last_time = time_base;
	for (i = 0; i < thread->stack_id; ++i)
		thread->stack [i]->recurse_count = 0;
	return 1;
}

static int
read_header_string (ProfContext *ctx, char **field)
{
	if (!load_data (ctx, 4))
		return 0;

	if (!load_data (ctx, read_int32 (ctx->buf)))
		return 0;

	*field = pstrdup ((const char *) ctx->buf);

	return 1;
}

static ProfContext*
load_file (char *name)
{
	unsigned char *p;
	ProfContext *ctx = (ProfContext *) g_calloc (sizeof (ProfContext), 1);
	if (strcmp (name, "-") == 0)
		ctx->file = stdin;
	else
		ctx->file = fopen (name, "rb");
	if (!ctx->file) {
		printf ("Cannot open file: %s\n", name);
		exit (1);
	}
#if defined (HAVE_SYS_ZLIB)
	if (ctx->file != stdin)
		ctx->gzfile = gzdopen (fileno (ctx->file), "rb");
#endif
	if (!load_data (ctx, 16))
		return NULL;
	p = ctx->buf;
	if (read_int32 (p) != LOG_HEADER_ID)
		return NULL;
	p += 4;
	ctx->version_major = *p++;
	ctx->version_minor = *p++;
	ctx->data_version = *p++;
	if (ctx->data_version > LOG_DATA_VERSION)
		return NULL;
	/* reading 64 bit files on 32 bit systems not supported yet */
	if (*p++ > sizeof (void*))
		return NULL;
	ctx->startup_time = read_int64 (p);
	p += 8;
	// nanoseconds startup time
	if (ctx->version_major >= 3)
		if (!load_data (ctx, 8))
			return NULL;
	if (!load_data (ctx, 14))
		return NULL;
	p = ctx->buf;
	ctx->timer_overhead = read_int32 (p);
	p += 4;
	if (read_int32 (p)) /* flags must be 0 */
		return NULL;
	p += 4;
	ctx->pid = read_int32 (p);
	p += 4;
	ctx->port = read_int16 (p);
	p += 2;
	if (ctx->version_major >= 1) {
		if (!read_header_string (ctx, &ctx->args))
			return NULL;
		if (!read_header_string (ctx, &ctx->arch))
			return NULL;
		if (!read_header_string (ctx, &ctx->os))
			return NULL;
	} else {
		if (!load_data (ctx, 2)) /* old opsys field, was never used */
			return NULL;
	}
	return ctx;
}

enum {
	ALLOC_SORT_BYTES,
	ALLOC_SORT_COUNT
};
static int alloc_sort_mode = ALLOC_SORT_BYTES;

static int
compare_class (const void *a, const void *b)
{
	ClassDesc *const *A = (ClassDesc *const *)a;
	ClassDesc *const *B = (ClassDesc *const *)b;
	uint64_t vala, valb;
	if (alloc_sort_mode == ALLOC_SORT_BYTES) {
		vala = (*A)->alloc_size;
		valb = (*B)->alloc_size;
	} else {
		vala = (*A)->allocs;
		valb = (*B)->allocs;
	}
	if (valb == vala)
		return 0;
	if (valb < vala)
		return -1;
	return 1;
}

static void
dump_header (ProfContext *ctx)
{
	time_t st = ctx->startup_time / 1000;
	char *t = ctime (&st);
	fprintf (outfile, "\nMono log profiler data\n");
	fprintf (outfile, "\tProfiler version: %d.%d\n", ctx->version_major, ctx->version_minor);
	fprintf (outfile, "\tData version: %d\n", ctx->data_version);
	if (ctx->version_major >= 1) {
		fprintf (outfile, "\tArguments: %s\n", ctx->args);
		fprintf (outfile, "\tArchitecture: %s\n", ctx->arch);
		fprintf (outfile, "\tOperating system: %s\n", ctx->os);
	}
	fprintf (outfile, "\tMean timer overhead: %d nanoseconds\n", ctx->timer_overhead);
	fprintf (outfile, "\tProgram startup: %s", t);
	if (ctx->pid)
		fprintf (outfile, "\tProgram ID: %d\n", ctx->pid);
	if (ctx->port)
		fprintf (outfile, "\tServer listening on: %d\n", ctx->port);
}

static void
dump_traces (TraceDesc *traces, const char *desc)
{
	int j;
	if (!show_traces)
		return;
	if (!traces->count)
		return;
	sort_context_array (traces);
	for (j = 0; j < traces->count; ++j) {
		int k;
		BackTrace *bt;
		bt = traces->traces [j].bt;
		if (!bt->count)
			continue;
		fprintf (outfile, "\t%" PRIu64 " %s from:\n", (guint64) traces->traces [j].count, desc);
		for (k = 0; k < bt->count; ++k)
			fprintf (outfile, "\t\t%s\n", bt->methods [k]->name);
	}
}

static void
dump_threads (ProfContext *ctx)
{
	ThreadContext *thread;
	fprintf (outfile, "\nThread summary\n");
	for (thread = ctx->threads; thread; thread = thread->next) {
		if (thread->thread_id) {
			fprintf (outfile, "\tThread: %p, name: \"%s\"\n", (void*)thread->thread_id, thread->name? thread->name: "");
		}
	}
}

static void
dump_domains (ProfContext *ctx)
{
	fprintf (outfile, "\nDomain summary\n");

	for (DomainContext *domain = ctx->domains; domain; domain = domain->next)
		fprintf (outfile, "\tDomain: %p, friendly name: \"%s\"\n", (void *) domain->domain_id, domain->friendly_name);
}

static void
dump_remctxs (ProfContext *ctx)
{
	fprintf (outfile, "\nContext summary\n");

	for (RemCtxContext *remctx = ctx->remctxs; remctx; remctx = remctx->next)
		fprintf (outfile, "\tContext: %p, domain: %p\n", (void *) remctx->remctx_id, (void *) remctx->domain_id);
}

static void
dump_exceptions (void)
{
	int i;
	fprintf (outfile, "\nException summary\n");
	fprintf (outfile, "\tThrows: %" PRIu64 "\n", (guint64) throw_count);
	dump_traces (&exc_traces, "throws");
	for (i = 0; i <= MONO_EXCEPTION_CLAUSE_FAULT; ++i) {
		if (!clause_summary [i])
			continue;
		fprintf (outfile, "\tExecuted %s clauses: %" PRIu64 "\n", clause_name (i), (guint64) clause_summary [i]);
	}
}

static int
compare_monitor (const void *a, const void *b)
{
	MonitorDesc *const *A = (MonitorDesc *const *)a;
	MonitorDesc *const *B = (MonitorDesc *const *)b;
	if ((*B)->wait_time == (*A)->wait_time)
		return 0;
	if ((*B)->wait_time < (*A)->wait_time)
		return -1;
	return 1;
}

static void
dump_monitors (void)
{
	MonitorDesc **monitors;
	int i, j;
	if (!num_monitors)
		return;
	monitors = (MonitorDesc **) g_malloc (sizeof (void*) * num_monitors);
	for (i = 0, j = 0; i < SMALL_HASH_SIZE; ++i) {
		MonitorDesc *mdesc = monitor_hash [i];
		while (mdesc) {
			monitors [j++] = mdesc;
			mdesc = mdesc->next;
		}
	}
	mono_qsort (monitors, num_monitors, sizeof (void*), compare_monitor);
	fprintf (outfile, "\nMonitor lock summary\n");
	for (i = 0; i < num_monitors; ++i) {
		MonitorDesc *mdesc = monitors [i];
		fprintf (outfile, "\tLock object %p: %d contentions\n", (void*)mdesc->objid, (int)mdesc->contentions);
		fprintf (outfile, "\t\t%.6f secs total wait time, %.6f max, %.6f average\n",
			mdesc->wait_time/1000000000.0, mdesc->max_wait_time/1000000000.0, mdesc->wait_time/1000000000.0/mdesc->contentions);
		dump_traces (&mdesc->traces, "contentions");
	}
	fprintf (outfile, "\tLock contentions: %" PRIu64 "\n", (guint64) monitor_contention);
	fprintf (outfile, "\tLock acquired: %" PRIu64 "\n", (guint64) monitor_acquired);
	fprintf (outfile, "\tLock failures: %" PRIu64 "\n", (guint64) monitor_failed);
}

static void
dump_gcs (void)
{
	int i;
	fprintf (outfile, "\nGC summary\n");
	fprintf (outfile, "\tGC resizes: %d\n", gc_resizes);
	fprintf (outfile, "\tMax heap size: %" PRIu64 "\n", (guint64) max_heap_size);
	fprintf (outfile, "\tObject moves: %" PRIu64 "\n", (guint64) gc_object_moves);
	for (i = 0; i < 3; ++i) {
		if (!gc_info [i].count)
			continue;
		fprintf (outfile, "\tGen%d collections: %d, max time: %" PRIu64 "us, total time: %" PRIu64 "us, average: %" PRIu64 "us\n",
			i, gc_info [i].count,
			(guint64) (gc_info [i].max_time / 1000),
			(guint64) (gc_info [i].total_time / 1000),
			(guint64) (gc_info [i].total_time / gc_info [i].count / 1000));
	}
	for (i = 0; i < 3; ++i) {
		if (!handle_info [i].max_live)
			continue;
		fprintf (outfile, "\tGC handles %s: created: %" PRIu64 ", destroyed: %" PRIu64 ", max: %" PRIu64 "\n",
			get_handle_name (i),
			(guint64) (handle_info [i].created),
			(guint64) (handle_info [i].destroyed),
			(guint64) (handle_info [i].max_live));
		dump_traces (&handle_info [i].traces, "created");
		dump_traces (&handle_info [i].destroy_traces, "destroyed");
	}
}

static void
dump_jit (void)
{
	int i;
	int code_size = 0;
	int compiled_methods = 0;
	MethodDesc* m;
	fprintf (outfile, "\nJIT summary\n");
	for (i = 0; i < HASH_SIZE; ++i) {
		m = method_hash [i];
		for (m = method_hash [i]; m; m = m->next) {
			if (!m->code || m->ignore_jit)
				continue;
			compiled_methods++;
			code_size += m->len;
		}
	}
	fprintf (outfile, "\tCompiled methods: %d\n", compiled_methods);
	fprintf (outfile, "\tGenerated code size: %d\n", code_size);
	fprintf (outfile, "\tJIT helpers: %d\n", num_jit_helpers);
	fprintf (outfile, "\tJIT helpers code size: %d\n", jit_helpers_code_size);
}

static void
dump_allocations (void)
{
	int i, c;
	intptr_t allocs = 0;
	uint64_t size = 0;
	int header_done = 0;
	ClassDesc **classes = (ClassDesc **) g_malloc (num_classes * sizeof (void*));
	ClassDesc *cd;
	c = 0;
	for (i = 0; i < HASH_SIZE; ++i) {
		cd = class_hash [i];
		while (cd) {
			classes [c++] = cd;
			cd = cd->next;
		}
	}
	mono_qsort (classes, num_classes, sizeof (void*), compare_class);
	for (i = 0; i < num_classes; ++i) {
		cd = classes [i];
		if (!cd->allocs)
			continue;
		allocs += cd->allocs;
		size += cd->alloc_size;
		if (!header_done++) {
			fprintf (outfile, "\nAllocation summary\n");
			fprintf (outfile, "%10s %10s %8s Type name\n", "Bytes", "Count", "Average");
		}
		fprintf (outfile, "%10" PRIu64 " %10zd %8" PRIu64 " %s\n",
			(guint64) (cd->alloc_size),
			cd->allocs,
			(guint64) (cd->alloc_size / cd->allocs),
			cd->name);
		dump_traces (&cd->traces, "bytes");
	}
	if (allocs)
		fprintf (outfile, "Total memory allocated: %" PRIu64 " bytes in %" G_GSIZE_FORMAT "d objects\n", (guint64) size, allocs);
}

enum {
	METHOD_SORT_TOTAL,
	METHOD_SORT_SELF,
	METHOD_SORT_CALLS
};

static int method_sort_mode = METHOD_SORT_TOTAL;

static int
compare_method (const void *a, const void *b)
{
	MethodDesc *const *A = (MethodDesc *const *)a;
	MethodDesc *const *B = (MethodDesc *const *)b;
	uint64_t vala, valb;
	if (method_sort_mode == METHOD_SORT_SELF) {
		vala = (*A)->self_time;
		valb = (*B)->self_time;
	} else if (method_sort_mode == METHOD_SORT_CALLS) {
		vala = (*A)->calls;
		valb = (*B)->calls;
	} else {
		vala = (*A)->total_time;
		valb = (*B)->total_time;
	}
	if (vala == valb)
		return 0;
	if (valb < vala)
		return -1;
	return 1;
}

static void
dump_metadata (void)
{
	fprintf (outfile, "\nMetadata summary\n");
	fprintf (outfile, "\tLoaded images: %d\n", num_images);
	if (verbose) {
		ImageDesc *image;
		int i;
		for (i = 0; i < SMALL_HASH_SIZE; ++i) {
			image = image_hash [i];
			while (image) {
				fprintf (outfile, "\t\t%s\n", image->filename);
				image = image->next;
			}
		}
	}
	fprintf (outfile, "\tLoaded assemblies: %d\n", num_assemblies);
	if (verbose) {
		AssemblyDesc *assembly;
		int i;
		for (i = 0; i < SMALL_HASH_SIZE; ++i) {
			assembly = assembly_hash [i];
			while (assembly) {
				fprintf (outfile, "\t\t%s\n", assembly->asmname);
				assembly = assembly->next;
			}
		}
	}
}

static void
dump_methods (void)
{
	int i, c;
	uint64_t calls = 0;
	int header_done = 0;
	MethodDesc **methods = (MethodDesc **) g_malloc (num_methods * sizeof (void*));
	MethodDesc *cd;
	c = 0;
	for (i = 0; i < HASH_SIZE; ++i) {
		cd = method_hash [i];
		while (cd) {
			cd->total_time = cd->self_time + cd->callee_time;
			methods [c++] = cd;
			cd = cd->next;
		}
	}
	mono_qsort (methods, num_methods, sizeof (void*), compare_method);
	for (i = 0; i < num_methods; ++i) {
		uint64_t msecs;
		uint64_t smsecs;
		cd = methods [i];
		if (!cd->calls)
			continue;
		calls += cd->calls;
		msecs = cd->total_time / 1000000;
		smsecs = (cd->total_time - cd->callee_time) / 1000000;
		if (!msecs && !verbose)
			continue;
		if (!header_done++) {
			fprintf (outfile, "\nMethod call summary\n");
			fprintf (outfile, "%8s %8s %10s Method name\n", "Total(ms)", "Self(ms)", "Calls");
		}
		fprintf (outfile, "%8" PRIu64 " %8" PRIu64 " %10" PRIu64 " %s\n",
			(guint64) (msecs),
			(guint64) (smsecs),
			(guint64) (cd->calls),
			cd->name);
		dump_traces (&cd->traces, "calls");
	}
	if (calls)
		fprintf (outfile, "Total calls: %" PRIu64 "\n", (guint64) calls);
}

static int
compare_heap_class (const void *a, const void *b)
{
	HeapClassDesc *const *A = (HeapClassDesc *const *)a;
	HeapClassDesc *const *B = (HeapClassDesc *const *)b;
	uint64_t vala, valb;
	if (alloc_sort_mode == ALLOC_SORT_BYTES) {
		vala = (*A)->total_size;
		valb = (*B)->total_size;
	} else {
		vala = (*A)->count;
		valb = (*B)->count;
	}
	if (valb == vala)
		return 0;
	if (valb < vala)
		return -1;
	return 1;
}

static int
compare_rev_class (const void *a, const void *b)
{
	const HeapClassRevRef *A = (const HeapClassRevRef *)a;
	const HeapClassRevRef *B = (const HeapClassRevRef *)b;
	if (B->count == A->count)
		return 0;
	if (B->count < A->count)
		return -1;
	return 1;
}

static void
dump_rev_claases (HeapClassRevRef *revs, int count)
{
	int j;
	if (!show_traces)
		return;
	if (!count)
		return;
	for (j = 0; j < count; ++j) {
		HeapClassDesc *cd = revs [j].klass;
		fprintf (outfile, "\t\t%" PRIu64 " references from: %s\n",
			(guint64) (revs [j].count),
			cd->klass->name);
	}
}

static void
heap_shot_summary (HeapShot *hs, int hs_num, HeapShot *last_hs)
{
	uint64_t size = 0;
	uint64_t count = 0;
	int ccount = 0;
	int i;
	HeapClassDesc *cd;
	HeapClassDesc **sorted;
	sorted = (HeapClassDesc **) g_malloc (sizeof (void*) * hs->class_count);
	for (i = 0; i < hs->hash_size; ++i) {
		cd = hs->class_hash [i];
		if (!cd)
			continue;
		count += cd->count;
		size += cd->total_size;
		sorted [ccount++] = cd;
	}
	hs->sorted = sorted;
	mono_qsort (sorted, ccount, sizeof (void*), compare_heap_class);
	fprintf (outfile, "\n\tHeap shot %d at %.3f secs: size: %" PRIu64 ", object count: %" PRIu64 ", class count: %d, roots: %" G_GSIZE_FORMAT "d\n",
		hs_num,
		(hs->timestamp - startup_time)/1000000000.0,
		(guint64) (size),
		(guint64) (count),
		ccount, hs->num_roots);
	if (!verbose && ccount > 30)
		ccount = 30;
	fprintf (outfile, "\t%10s %10s %8s Class name\n", "Bytes", "Count", "Average");
	for (i = 0; i < ccount; ++i) {
		HeapClassRevRef *rev_sorted;
		int j, k;
		HeapClassDesc *ocd = NULL;
		cd = sorted [i];
		if (last_hs)
			ocd = heap_class_lookup (last_hs, cd->klass);
		fprintf (outfile, "\t%10" PRIu64 " %10" PRIu64 " %8" PRIu64 " %s",
			(guint64) (cd->total_size),
			(guint64) (cd->count),
			(guint64) (cd->total_size / cd->count),
			cd->klass->name);
		if (ocd) {
			int64_t bdiff = cd->total_size - ocd->total_size;
			int64_t cdiff = cd->count - ocd->count;
			fprintf (outfile, " (bytes: %+" PRId64 ", count: %+" PRId64 ")\n", (gint64) bdiff, (gint64) cdiff);
		} else {
			fprintf (outfile, "\n");
		}
		if (!collect_traces)
			continue;
		rev_sorted = (HeapClassRevRef *) g_malloc (cd->rev_count * sizeof (HeapClassRevRef));
		k = 0;
		for (j = 0; j < cd->rev_hash_size; ++j) {
			if (cd->rev_hash [j].klass)
				rev_sorted [k++] = cd->rev_hash [j];
		}
		assert (cd->rev_count == k);
		mono_qsort (rev_sorted, cd->rev_count, sizeof (HeapClassRevRef), compare_rev_class);
		if (cd->root_references)
			fprintf (outfile, "\t\t%" G_GSIZE_FORMAT "d root references (%" G_GSIZE_FORMAT "d pinning)\n", cd->root_references, cd->pinned_references);
		dump_rev_claases (rev_sorted, cd->rev_count);
		g_free (rev_sorted);
	}
	g_free (sorted);
}

static int
compare_heap_shots (const void *a, const void *b)
{
	HeapShot *const *A = (HeapShot *const *)a;
	HeapShot *const *B = (HeapShot *const *)b;
	if ((*B)->timestamp == (*A)->timestamp)
		return 0;
	if ((*B)->timestamp > (*A)->timestamp)
		return -1;
	return 1;
}

static void
dump_heap_shots (void)
{
	HeapShot **hs_sorted;
	HeapShot *hs;
	HeapShot *last_hs = NULL;
	int i;
	if (!heap_shots)
		return;
	hs_sorted = (HeapShot **) g_malloc (num_heap_shots * sizeof (void*));
	fprintf (outfile, "\nHeap shot summary\n");
	i = 0;
	for (hs = heap_shots; hs; hs = hs->next)
		hs_sorted [i++] = hs;
	mono_qsort (hs_sorted, num_heap_shots, sizeof (void*), compare_heap_shots);
	for (i = 0; i < num_heap_shots; ++i) {
		hs = hs_sorted [i];
		heap_shot_summary (hs, i, last_hs);
		last_hs = hs;
	}
}

#define DUMP_EVENT_STAT(EVENT,SUBTYPE) dump_event (#EVENT, #SUBTYPE, EVENT, SUBTYPE);

static void
dump_event (const char *event_name, const char *subtype_name, int event, int subtype)
{
	int idx = event | subtype;
	EventStat evt = stats [idx];
	if (!evt.count)
		return;

	fprintf (outfile, "\t%16s\t%26s\tcount %6d\tmin %3d\tmax %6d\tbytes %d\n", event_name, subtype_name, evt.count, evt.min_size, evt.max_size, evt.bytes);
}

static void
dump_stats (void)
{
	fprintf (outfile, "\nMlpd statistics\n");
	fprintf (outfile, "\tBuffer count %d\toverhead %d (%d bytes per header)\n", buffer_count, buffer_count * BUFFER_HEADER_SIZE, BUFFER_HEADER_SIZE);
	fprintf (outfile, "\nEvent details:\n");

	DUMP_EVENT_STAT (TYPE_ALLOC, TYPE_ALLOC_NO_BT);
	DUMP_EVENT_STAT (TYPE_ALLOC, TYPE_ALLOC_BT);

	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_EVENT);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_RESIZE);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_MOVE);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_HANDLE_CREATED);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_HANDLE_DESTROYED);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_HANDLE_CREATED_BT);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_HANDLE_DESTROYED_BT);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_FINALIZE_START);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_FINALIZE_END);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_FINALIZE_OBJECT_START);
	DUMP_EVENT_STAT (TYPE_GC, TYPE_GC_FINALIZE_OBJECT_END);

	DUMP_EVENT_STAT (TYPE_METADATA, TYPE_END_LOAD);
	DUMP_EVENT_STAT (TYPE_METADATA, TYPE_END_UNLOAD);

	DUMP_EVENT_STAT (TYPE_METHOD, TYPE_LEAVE);
	DUMP_EVENT_STAT (TYPE_METHOD, TYPE_ENTER);
	DUMP_EVENT_STAT (TYPE_METHOD, TYPE_EXC_LEAVE);
	DUMP_EVENT_STAT (TYPE_METHOD, TYPE_JIT);

	DUMP_EVENT_STAT (TYPE_EXCEPTION, TYPE_THROW_NO_BT);
	DUMP_EVENT_STAT (TYPE_EXCEPTION, TYPE_THROW_BT);
	DUMP_EVENT_STAT (TYPE_EXCEPTION, TYPE_CLAUSE);

	DUMP_EVENT_STAT (TYPE_MONITOR, TYPE_MONITOR_NO_BT);
	DUMP_EVENT_STAT (TYPE_MONITOR, TYPE_MONITOR_BT);

	DUMP_EVENT_STAT (TYPE_HEAP, TYPE_HEAP_START);
	DUMP_EVENT_STAT (TYPE_HEAP, TYPE_HEAP_END);
	DUMP_EVENT_STAT (TYPE_HEAP, TYPE_HEAP_OBJECT);
	DUMP_EVENT_STAT (TYPE_HEAP, TYPE_HEAP_ROOT);

	DUMP_EVENT_STAT (TYPE_SAMPLE, TYPE_SAMPLE_HIT);
	DUMP_EVENT_STAT (TYPE_SAMPLE, TYPE_SAMPLE_USYM);
	DUMP_EVENT_STAT (TYPE_SAMPLE, TYPE_SAMPLE_UBIN);
	DUMP_EVENT_STAT (TYPE_SAMPLE, TYPE_SAMPLE_COUNTERS_DESC);
	DUMP_EVENT_STAT (TYPE_SAMPLE, TYPE_SAMPLE_COUNTERS);

	DUMP_EVENT_STAT (TYPE_RUNTIME, TYPE_JITHELPER);

	DUMP_EVENT_STAT (TYPE_COVERAGE, TYPE_COVERAGE_ASSEMBLY);
	DUMP_EVENT_STAT (TYPE_COVERAGE, TYPE_COVERAGE_METHOD);
	DUMP_EVENT_STAT (TYPE_COVERAGE, TYPE_COVERAGE_STATEMENT);
	DUMP_EVENT_STAT (TYPE_COVERAGE, TYPE_COVERAGE_CLASS);

	DUMP_EVENT_STAT (TYPE_META, TYPE_SYNC_POINT);
	DUMP_EVENT_STAT (TYPE_META, TYPE_AOT_ID);
}



static void
flush_context (ProfContext *ctx)
{
	ThreadContext *thread;
	/* FIXME: sometimes there are leftovers: indagate */
	for (thread = ctx->threads; thread; thread = thread->next) {
		while (thread->stack_id) {
			if (debug)
				fprintf (outfile, "thread %p has %d items on stack\n", (void*)thread->thread_id, thread->stack_id);
			pop_method (thread, thread->stack [thread->stack_id - 1], thread->last_time);
		}
	}
}

static const char *reports = "header,jit,gc,sample,alloc,call,metadata,exception,monitor,thread,domain,context,heapshot,counters";

static const char*
match_option (const char *p, const char *opt)
{
	int len = strlen (opt);
	if (strncmp (p, opt, len) == 0) {
		if (p [len] == ',')
			len++;
		return p + len;
	}
	return p;
}

static int
print_reports (ProfContext *ctx, const char *reps, int parse_only)
{
	const char *opt;
	const char *p;
	for (p = reps; *p; p = opt) {
		if ((opt = match_option (p, "header")) != p) {
			if (!parse_only)
				dump_header (ctx);
			continue;
		}
		if ((opt = match_option (p, "thread")) != p) {
			if (!parse_only)
				dump_threads (ctx);
			continue;
		}
		if ((opt = match_option (p, "domain")) != p) {
			if (!parse_only)
				dump_domains (ctx);
			continue;
		}
		if ((opt = match_option (p, "context")) != p) {
			if (!parse_only)
				dump_remctxs (ctx);
			continue;
		}
		if ((opt = match_option (p, "gc")) != p) {
			if (!parse_only)
				dump_gcs ();
			continue;
		}
		if ((opt = match_option (p, "jit")) != p) {
			if (!parse_only)
				dump_jit ();
			continue;
		}
		if ((opt = match_option (p, "alloc")) != p) {
			if (!parse_only)
				dump_allocations ();
			continue;
		}
		if ((opt = match_option (p, "call")) != p) {
			if (!parse_only)
				dump_methods ();
			continue;
		}
		if ((opt = match_option (p, "metadata")) != p) {
			if (!parse_only)
				dump_metadata ();
			continue;
		}
		if ((opt = match_option (p, "exception")) != p) {
			if (!parse_only)
				dump_exceptions ();
			continue;
		}
		if ((opt = match_option (p, "monitor")) != p) {
			if (!parse_only)
				dump_monitors ();
			continue;
		}
		if ((opt = match_option (p, "heapshot")) != p) {
			if (!parse_only)
				dump_heap_shots ();
			continue;
		}
		if ((opt = match_option (p, "sample")) != p) {
			if (!parse_only)
				dump_samples ();
			continue;
		}
		if ((opt = match_option (p, "counters")) != p) {
			if (!parse_only)
				dump_counters ();
			continue;
		}
		if ((opt = match_option (p, "coverage")) != p) {
			printf ("The log profiler no longer supports code coverage. Please use the dedicated coverage profiler instead. See mono-profilers(1) for more information.\n");
			continue;
		}
		if ((opt = match_option (p, "stats")) != p) {
			if (!parse_only)
				dump_stats ();
			continue;
		}
		return 0;
	}
	return 1;
}

static int
add_find_spec (const char *p)
{
	if (p [0] == 'S' && p [1] == ':') {
		char *vale;
		find_size = strtoul (p + 2, &vale, 10);
		return 1;
	} else if (p [0] == 'T' && p [1] == ':') {
		find_name = p + 2;
		return 1;
	}
	return 0;
}

static void
usage (void)
{
	printf ("Mono log profiler report version %d.%d\n", LOG_VERSION_MAJOR, LOG_VERSION_MINOR);
	printf ("Usage: mprof-report [OPTIONS] FILENAME\n");
	printf ("FILENAME can be '-' to read from standard input.\n");
	printf ("Options:\n");
	printf ("\t--help               display this help\n");
	printf ("\t--out=FILE           write to FILE instead of stdout\n");
	printf ("\t--traces             collect and show backtraces\n");
	printf ("\t--maxframes=NUM      limit backtraces to NUM entries\n");
	printf ("\t--reports=R1[,R2...] print the specified reports. Defaults are:\n");
	printf ("\t                     %s\n", reports);
	printf ("\t--method-sort=MODE   sort methods according to MODE: total, self, calls\n");
	printf ("\t--alloc-sort=MODE    sort allocations according to MODE: bytes, count\n");
	printf ("\t--counters-sort=MODE sort counters according to MODE: time, category\n");
	printf ("\t                     only accessible in verbose mode\n");
	printf ("\t--track=OB1[,OB2...] track what happens to objects OBJ1, O2 etc.\n");
	printf ("\t--find=FINDSPEC      find and track objects matching FINFSPEC, where FINDSPEC is:\n");
	printf ("\t                     S:minimum_size or T:partial_name\n");
	printf ("\t--thread=THREADID    consider just the data for thread THREADID\n");
	printf ("\t--time=FROM-TO       consider data FROM seconds from startup up to TO seconds\n");
	printf ("\t--verbose            increase verbosity level\n");
	printf ("\t--debug              display decoding debug info for mprof-report devs\n");
}

int
main (int argc, char *argv[])
{
	ProfContext *ctx;
	int i;
	outfile = stdout;
	for (i = 1; i < argc; ++i) {
		if (strcmp ("--debug", argv [i]) == 0) {
			debug++;
		} else if (strcmp ("--help", argv [i]) == 0) {
			usage ();
			return 0;
		} else if (strncmp ("--alloc-sort=", argv [i], 13) == 0) {
			const char *val = argv [i] + 13;
			if (strcmp (val, "bytes") == 0) {
				alloc_sort_mode = ALLOC_SORT_BYTES;
			} else if (strcmp (val, "count") == 0) {
				alloc_sort_mode = ALLOC_SORT_COUNT;
			} else {
				usage ();
				return 1;
			}
		} else if (strncmp ("--method-sort=", argv [i], 14) == 0) {
			const char *val = argv [i] + 14;
			if (strcmp (val, "total") == 0) {
				method_sort_mode = METHOD_SORT_TOTAL;
			} else if (strcmp (val, "self") == 0) {
				method_sort_mode = METHOD_SORT_SELF;
			} else if (strcmp (val, "calls") == 0) {
				method_sort_mode = METHOD_SORT_CALLS;
			} else {
				usage ();
				return 1;
			}
		} else if (strncmp ("--counters-sort=", argv [i], 16) == 0) {
			const char *val = argv [i] + 16;
			if (strcmp (val, "time") == 0) {
				counters_sort_mode = COUNTERS_SORT_TIME;
			} else if (strcmp (val, "category") == 0) {
				counters_sort_mode = COUNTERS_SORT_CATEGORY;
			} else {
				usage ();
				return 1;
			}
		} else if (strncmp ("--reports=", argv [i], 10) == 0) {
			const char *val = argv [i] + 10;
			if (!print_reports (NULL, val, 1)) {
				usage ();
				return 1;
			}
			reports = val;
		} else if (strncmp ("--out=", argv [i], 6) == 0) {
			const char *val = argv [i] + 6;
			outfile = fopen (val, "w");
			if (!outfile) {
				printf ("Cannot open output file: %s\n", val);
				return 1;
			}
		} else if (strncmp ("--maxframes=", argv [i], 12) == 0) {
			const char *val = argv [i] + 12;
			char *vale;
			trace_max = strtoul (val, &vale, 10);
		} else if (strncmp ("--find=", argv [i], 7) == 0) {
			const char *val = argv [i] + 7;
			if (!add_find_spec (val)) {
				usage ();
				return 1;
			}
		} else if (strncmp ("--track=", argv [i], 8) == 0) {
			const char *val = argv [i] + 8;
			char *vale;
			while (*val) {
				uintptr_t tracked_obj;
				if (*val == ',') {
					val++;
					continue;
				}
				tracked_obj = strtoul (val, &vale, 0);
				found_object (tracked_obj);
				val = vale;
			}
		} else if (strncmp ("--thread=", argv [i], 9) == 0) {
			const char *val = argv [i] + 9;
			char *vale;
			thread_filter = strtoul (val, &vale, 0);
		} else if (strncmp ("--time=", argv [i], 7) == 0) {
			char *val = pstrdup (argv [i] + 7);
			double from_secs, to_secs;
			char *top = strchr (val, '-');
			if (!top) {
				usage ();
				return 1;
			}
			*top++ = 0;
			from_secs = atof (val);
			to_secs = atof (top);
			g_free (val);
			if (from_secs > to_secs) {
				usage ();
				return 1;
			}
			time_from = from_secs * 1000000000;
			time_to = to_secs * 1000000000;
			use_time_filter = 1;
		} else if (strcmp ("--verbose", argv [i]) == 0) {
			verbose++;
		} else if (strcmp ("--traces", argv [i]) == 0) {
			show_traces = 1;
			collect_traces = 1;
		} else if (strncmp ("--coverage-out=", argv [i], 15) == 0) {
			// For backwards compatibility.
		} else {
			break;
		}
	}
	if (i >= argc) {
		usage ();
		return 2;
	}
	ctx = load_file (argv [i]);
	if (!ctx) {
		printf ("Not a log profiler data file (or unsupported version).\n");
		return 1;
	}
	while (decode_buffer (ctx));
	flush_context (ctx);
	if (num_tracked_objects)
		return 0;
	print_reports (ctx, reports, 0);
	return 0;
}
