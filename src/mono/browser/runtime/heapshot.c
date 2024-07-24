// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#include <emscripten.h>
#include <emscripten/stack.h>
#include <stdio.h>
#include <stddef.h>
#include <stdlib.h>
#include <string.h>
#include <stdint.h>
#include <assert.h>
#include <math.h>
#include <dlfcn.h>
#include <sys/stat.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/class.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/image.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/object.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/profiler.h>
// FIXME: unavailable in emscripten
// #include <mono/metadata/gc-internals.h>

#include <mono/metadata/mono-private-unstable.h>

#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-dl-fallback.h>
#include <mono/utils/mono-counters.h>
#include <mono/jit/jit.h>
#include <mono/jit/mono-private-unstable.h>

#include "wasm-config.h"
#include "runtime.h"

#include "gc-common.h"

static void * parachute = NULL;

void
mono_wasm_heapshot_initialize (void);

int
mono_assembly_update_heapshot_scratch_byte (MonoAssembly *assembly, char new_value);

int
mono_class_update_heapshot_scratch_byte (MonoClass *klass, char new_value);

typedef enum {
	HANDLE_TYPE_MIN = 0,
	HANDLE_WEAK = HANDLE_TYPE_MIN,
	HANDLE_WEAK_TRACK,
	HANDLE_NORMAL,
	HANDLE_PINNED,
	HANDLE_WEAK_FIELDS,
	HANDLE_TYPE_MAX
} GCHandleType;

enum {
	ROOT_TYPE_NORMAL = 0, /* "normal" roots */
	ROOT_TYPE_PINNED = 1, /* roots without a GC descriptor */
	ROOT_TYPE_WBARRIER = 2, /* roots with a write barrier */
	ROOT_TYPE_NUM
};

struct _MonoProfiler {
};

static char next_heapshot_scratch_byte = 1;
static MonoProfiler heapshot_profiler;
static MonoProfilerHandle heapshot_profiler_handle = NULL;

extern void
mono_wasm_heapshot_start (int full);
extern void
mono_wasm_heapshot_assembly (
	MonoAssembly *assembly, const char *name
);
extern void
mono_wasm_heapshot_class (
	MonoClass *klass, MonoClass *element_klass, MonoClass *nesting_klass, MonoAssembly *assembly,
	const char *namespace, const char *name, int rank,
	int kind, int gparam_count, MonoClass **gparams
);
extern void
mono_wasm_heapshot_object (
	MonoObject *obj, MonoClass *klass, uint32_t size, uint32_t ref_count, MonoObject **refs
);
extern void
mono_wasm_heapshot_gchandle (MonoObject *obj, int handle_type);
extern void
mono_wasm_heapshot_roots (
	const char *kind, int count,
	const uint8_t *const *addresses,
	MonoObject *const *objects
);
extern void
mono_wasm_heapshot_stats (
	int in_use_pages, int free_pages, int external_pages, int largest_free_chunk,
	int sgen_los_size, int sgen_heap_capacity
);
extern void
mono_wasm_heapshot_counter (
	const char *name, double value
);
extern void
mono_wasm_heapshot_end (int full);

void
mwpm_compute_stats (uint32_t *in_use_pages, uint32_t *free_pages, uint32_t *external_pages, uint32_t *largest_free_chunk);

typedef void * (*SgenGCHandleIterateCallback) (void *hidden, GCHandleType handle_type, int max_generation, void *user);

size_t
sgen_gc_get_total_heap_allocation (void);

void
sgen_gchandle_iterate (GCHandleType handle_type, int max_generation, SgenGCHandleIterateCallback callback, void *user);

void *
sgen_try_reveal_pointer (void *hidden, GCHandleType handle_type);

typedef void (*SgenRootIterateCallback) (void *start, void *end, MonoGCRootSource source, int root_type, const char *msg, void *user);

void
sgen_registered_root_iterate (SgenRootIterateCallback callback, void *user_data, int root_type);

#define MALLINFO_FIELD_TYPE size_t

struct mallinfo {
	MALLINFO_FIELD_TYPE arena;    /* non-mmapped space allocated from system */
	MALLINFO_FIELD_TYPE ordblks;  /* number of free chunks */
	MALLINFO_FIELD_TYPE smblks;   /* always 0 */
	MALLINFO_FIELD_TYPE hblks;    /* always 0 */
	MALLINFO_FIELD_TYPE hblkhd;   /* space in mmapped regions */
	MALLINFO_FIELD_TYPE usmblks;  /* maximum total allocated space */
	MALLINFO_FIELD_TYPE fsmblks;  /* always 0 */
	MALLINFO_FIELD_TYPE uordblks; /* total allocated space */
	MALLINFO_FIELD_TYPE fordblks; /* total free space */
	MALLINFO_FIELD_TYPE keepcost; /* releasable (via malloc_trim) space */
};

struct mallinfo
mallinfo (void);

extern uint32_t sgen_los_memory_usage, sgen_los_memory_usage_total;

void
mono_wasm_heapshot_initialize (void) {
    parachute = malloc (1024 * 64 * 10);
}

static void
mono_wasm_on_gc_assembly (MonoAssembly *assembly) {
	MonoAssemblyName *aname = mono_assembly_get_name (assembly);
	mono_wasm_heapshot_assembly (assembly, mono_assembly_name_get_name (aname));
}

static void
mono_wasm_on_gc_class (MonoClass *klass) {
	MonoImage *image = mono_class_get_image (klass);
	MonoAssembly *assem = mono_image_get_assembly (image);
	if (mono_assembly_update_heapshot_scratch_byte (assem, next_heapshot_scratch_byte))
		mono_wasm_on_gc_assembly (assem);

	MonoClass *info_klass = klass;
	char namespace_buffer[1024];
	// If we're looking at an array, examine the element type instead and specify the rank
	int rank = mono_class_get_rank (klass);
	if (rank > 0) {
		info_klass = mono_class_get_element_class (klass);
		if (mono_class_update_heapshot_scratch_byte (info_klass, next_heapshot_scratch_byte))
			mono_wasm_on_gc_class (info_klass);
	}

	MonoClass *gparams[64];
	int gparam_count = mono_class_get_generic_params (info_klass, gparams, sizeof(gparams) / sizeof(gparams[0]));
	for (int i = 0; i < gparam_count; i++) {
		if (mono_class_update_heapshot_scratch_byte (gparams[i], next_heapshot_scratch_byte))
			mono_wasm_on_gc_class (gparams[i]);
	}

	MonoClass *nesting_klass = mono_class_get_nesting_type (info_klass);
	if (nesting_klass && mono_class_update_heapshot_scratch_byte (nesting_klass, next_heapshot_scratch_byte))
		mono_wasm_on_gc_class (nesting_klass);

	mono_wasm_heapshot_class (
		klass, mono_class_get_element_class (klass), nesting_klass,
		assem, mono_class_get_namespace (info_klass), mono_class_get_name (info_klass), rank,
		mono_class_get_kind (klass), gparam_count, gparam_count ? gparams : NULL
	);
}

// NOTE: for objects (like arrays) containing more than 128 refs, this will get invoked multiple times
static int
mono_wasm_on_gc_object (
	MonoObject *obj,
	MonoClass *klass,
	uintptr_t size,
	uintptr_t num,
	MonoObject **refs,
	uintptr_t *offsets,
	void *data
) {
	if (mono_class_update_heapshot_scratch_byte (klass, next_heapshot_scratch_byte))
		mono_wasm_on_gc_class (klass);
	mono_wasm_heapshot_object (obj, klass, (uint32_t)size, (uint32_t)num, refs);
	return 0;
}

static void *
mono_wasm_each_gchandle (void *hidden, GCHandleType handle_type, int max_generation, void *user)
{
	MonoObject *obj = sgen_try_reveal_pointer (hidden, handle_type);
	if (obj)
		mono_wasm_heapshot_gchandle (obj, handle_type);
	return hidden;
}

static void
mono_wasm_generate_heapshot_stats () {
	uint32_t in_use_pages, free_pages, external_pages, largest_free_chunk;
	mwpm_compute_stats (&in_use_pages, &free_pages, &external_pages, &largest_free_chunk);
	mono_wasm_heapshot_stats (
		in_use_pages, free_pages, external_pages, largest_free_chunk, (int)sgen_los_memory_usage, (int)sgen_gc_get_total_heap_allocation ()
	);
	struct mallinfo minfo = mallinfo ();
#define mifield_inner(a, b) a.b
#define mifield(name) mono_wasm_heapshot_counter("dlmalloc/" #name, mifield_inner(minfo, name))
	mifield(arena);
	mifield(ordblks);
	mifield(smblks);
	mifield(hblks);
	mifield(hblkhd);
	mifield(usmblks);
	mifield(fsmblks);
	mifield(uordblks);
	mifield(fordblks);
	mifield(keepcost);
}

static void
mono_wasm_on_gc_event (
	MonoProfiler *prof,
	MonoProfilerGCEvent gc_event,
	uint32_t generation,
	mono_bool serial
) {
	if (gc_event != MONO_GC_EVENT_PRE_START_WORLD)
		return;

	mono_wasm_generate_heapshot_stats();
	mono_gc_walk_heap (0, mono_wasm_on_gc_object, NULL);
	for (int ht = HANDLE_TYPE_MIN; ht < HANDLE_TYPE_MAX; ht++)
		sgen_gchandle_iterate ((GCHandleType)ht, 2, mono_wasm_each_gchandle, prof);
}

static void
mono_wasm_on_gc_roots (
	MonoProfiler *prof,
	uint64_t count,
	const uint8_t *const *addresses,
	MonoObject *const *objects,
	const char *kind
) {
	mono_wasm_heapshot_roots (
		kind, count, addresses, objects
	);
}

struct _MonoCounter {
	MonoCounter *next;
	const char *name;
	void *addr;
	int type;
	size_t size;
};

static int
mono_wasm_on_counter (
	MonoCounter *counter, gpointer user_data
) {
	double value = 0;
	switch (counter->type & MONO_COUNTER_TYPE_MASK) {
		case MONO_COUNTER_INT:    /* 32 bit int */
			value = *(int32_t *)counter->addr;
			break;
		case MONO_COUNTER_UINT:    /* 32 bit uint */
			value = *(uint32_t *)counter->addr;
			break;
		case MONO_COUNTER_WORD:   /* pointer-sized int */
			value = *(ssize_t *)counter->addr;
			break;
		case MONO_COUNTER_LONG:   /* 64 bit int */
			value = *(int64_t *)counter->addr;
			break;
		case MONO_COUNTER_ULONG:   /* 64 bit uint */
			value = *(uint64_t *)counter->addr;
			break;
		case MONO_COUNTER_DOUBLE:
			value = *(double *)counter->addr;
			break;
		default:
			return 1;
	}
	mono_wasm_heapshot_counter (counter->name, value);
	return 1;
}

EMSCRIPTEN_KEEPALIVE void
mono_wasm_perform_heapshot (int full) {
	if (parachute) {
		free(parachute);
		parachute = NULL;
	}
	if (!heapshot_profiler_handle) {
		memset (&heapshot_profiler, 0, sizeof(MonoProfiler));
		heapshot_profiler_handle = mono_profiler_create (&heapshot_profiler);
	}
	// intentional wraparound
	next_heapshot_scratch_byte = (char)((int)next_heapshot_scratch_byte + 1);
	mono_wasm_heapshot_start (full);
	mono_profiler_set_gc_event_callback (heapshot_profiler_handle, mono_wasm_on_gc_event);
	mono_profiler_set_gc_roots_callback (heapshot_profiler_handle, mono_wasm_on_gc_roots);
	if (full)
		mono_gc_collect (mono_gc_max_generation ());
	else
		mono_wasm_generate_heapshot_stats ();
	mono_counters_foreach (mono_wasm_on_counter, NULL);
	mono_profiler_set_gc_event_callback (heapshot_profiler_handle, NULL);
	mono_profiler_set_gc_roots_callback (heapshot_profiler_handle, NULL);
	mono_wasm_heapshot_end (full);
}
