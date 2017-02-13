/**
 * \file
 */

#ifndef __MONO_COUNTERS_H__
#define __MONO_COUNTERS_H__

#include <stdio.h>
#include <mono/utils/mono-publib.h>

enum {
	/* Counter type, bits 0-7. */
	MONO_COUNTER_INT,    /* 32 bit int */
	MONO_COUNTER_UINT,    /* 32 bit uint */
	MONO_COUNTER_WORD,   /* pointer-sized int */
	MONO_COUNTER_LONG,   /* 64 bit int */
	MONO_COUNTER_ULONG,   /* 64 bit uint */
	MONO_COUNTER_DOUBLE,
	MONO_COUNTER_STRING, /* char* */
	MONO_COUNTER_TIME_INTERVAL, /* 64 bits signed int holding usecs. */
	MONO_COUNTER_TYPE_MASK = 0xf,
	MONO_COUNTER_CALLBACK = 128, /* ORed with the other values */
	MONO_COUNTER_SECTION_MASK = 0x00ffff00,
	/* Sections, bits 8-23 (16 bits) */
	MONO_COUNTER_JIT      = 1 << 8,
	MONO_COUNTER_GC       = 1 << 9,
	MONO_COUNTER_METADATA = 1 << 10,
	MONO_COUNTER_GENERICS = 1 << 11,
	MONO_COUNTER_SECURITY = 1 << 12,
	MONO_COUNTER_RUNTIME  = 1 << 13,
	MONO_COUNTER_SYSTEM   = 1 << 14,
	MONO_COUNTER_PERFCOUNTERS = 1 << 15,
	MONO_COUNTER_PROFILER = 1 << 16,
	MONO_COUNTER_LAST_SECTION,

	/* Unit, bits 24-27 (4 bits) */
	MONO_COUNTER_UNIT_SHIFT = 24,
	MONO_COUNTER_UNIT_MASK = 0xFu << MONO_COUNTER_UNIT_SHIFT,
	MONO_COUNTER_RAW        = 0 << 24,  /* Raw value */
	MONO_COUNTER_BYTES      = 1 << 24, /* Quantity of bytes. RSS, active heap, etc */
	MONO_COUNTER_TIME       = 2 << 24,  /* Time interval in 100ns units. Minor pause, JIT compilation*/
	MONO_COUNTER_COUNT      = 3 << 24, /*  Number of things (threads, queued jobs) or Number of events triggered (Major collections, Compiled methods).*/
	MONO_COUNTER_PERCENTAGE = 4 << 24, /* [0-1] Fraction Percentage of something. Load average. */

	/* Monotonicity, bits 28-31 (4 bits) */
	MONO_COUNTER_VARIANCE_SHIFT = 28,
	MONO_COUNTER_VARIANCE_MASK = 0xFu << MONO_COUNTER_VARIANCE_SHIFT,
	MONO_COUNTER_MONOTONIC      = 1 << 28, /* This counter value always increase/decreases over time. Reported by --stat. */
	MONO_COUNTER_CONSTANT       = 1 << 29, /* Fixed value. Used by configuration data. */
	MONO_COUNTER_VARIABLE       = 1 << 30, /* This counter value can be anything on each sampling. Only interesting when sampling. */
};

typedef struct _MonoCounter MonoCounter;

MONO_API void mono_counters_enable (int section_mask);
MONO_API void mono_counters_init (void);

/* 
 * register addr as the address of a counter of type type.
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 */
MONO_API void mono_counters_register (const char* descr, int type, void *addr);
MONO_API void mono_counters_register_with_size (const char *name, int type, void *addr, int size);

typedef void (*MonoCounterRegisterCallback) (MonoCounter*);
MONO_API void mono_counters_on_register (MonoCounterRegisterCallback callback);

/* 
 * Create a readable dump of the counters for section_mask sections (ORed section values)
 */
MONO_API void mono_counters_dump (int section_mask, FILE *outfile);

MONO_API void mono_counters_cleanup (void);

typedef mono_bool (*CountersEnumCallback) (MonoCounter *counter, void *user_data);

MONO_API void mono_counters_foreach (CountersEnumCallback cb, void *user_data);

MONO_API int mono_counters_sample (MonoCounter *counter, void *buffer, int buffer_size);

MONO_API const char* mono_counter_get_name (MonoCounter *name);
MONO_API int mono_counter_get_type (MonoCounter *counter);
MONO_API int mono_counter_get_section (MonoCounter *counter);
MONO_API int mono_counter_get_unit (MonoCounter *counter);
MONO_API int mono_counter_get_variance (MonoCounter *counter);
MONO_API size_t mono_counter_get_size (MonoCounter *counter);

typedef enum {
	MONO_RESOURCE_JIT_CODE, /* bytes */
	MONO_RESOURCE_METADATA, /* bytes */
	MONO_RESOURCE_GC_HEAP,  /* bytes */
	MONO_RESOURCE_COUNT /* non-ABI value */
} MonoResourceType;

typedef void (*MonoResourceCallback) (int resource_type, uintptr_t value, int is_soft);

MONO_API int  mono_runtime_resource_limit        (int resource_type, uintptr_t soft_limit, uintptr_t hard_limit);
MONO_API void mono_runtime_resource_set_callback (MonoResourceCallback callback);
MONO_API void mono_runtime_resource_check_limit  (int resource_type, uintptr_t value);

#endif /* __MONO_COUNTERS_H__ */

