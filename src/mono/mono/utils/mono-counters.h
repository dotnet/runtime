#ifndef __MONO_COUNTERS_H__
#define __MONO_COUNTERS_H__

#include <stdio.h>
#include <mono/utils/mono-publib.h>

enum {
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
	MONO_COUNTER_SECTION_MASK = 0xffffff00,
	/* sections */
	MONO_COUNTER_JIT      = 1 << 8,
	MONO_COUNTER_GC       = 1 << 9,
	MONO_COUNTER_METADATA = 1 << 10,
	MONO_COUNTER_GENERICS = 1 << 11,
	MONO_COUNTER_SECURITY = 1 << 12,
	MONO_COUNTER_LAST_SECTION
};

void mono_counters_enable (int section_mask);

/* 
 * register addr as the address of a counter of type type.
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 */
void mono_counters_register (const char* descr, int type, void *addr);

/* 
 * Create a readable dump of the counters for section_mask sections (ORed section values)
 */
void mono_counters_dump (int section_mask, FILE *outfile);

void mono_counters_cleanup (void);

typedef enum {
	MONO_RESOURCE_JIT_CODE, /* bytes */
	MONO_RESOURCE_METADATA, /* bytes */
	MONO_RESOURCE_GC_HEAP,  /* bytes */
	MONO_RESOURCE_COUNT /* non-ABI value */
} MonoResourceType;

typedef void (*MonoResourceCallback) (int resource_type, uintptr_t value, int is_soft);

int  mono_runtime_resource_limit        (int resource_type, uintptr_t soft_limit, uintptr_t hard_limit);
void mono_runtime_resource_set_callback (MonoResourceCallback callback);
void mono_runtime_resource_check_limit  (int resource_type, uintptr_t value);

#endif /* __MONO_COUNTERS_H__ */

