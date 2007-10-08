#ifndef __MONO_COUNTERS_H__
#define __MONO_COUNTERS_H__

#include <stdio.h>

enum {
	MONO_COUNTER_INT,    /* 32 bit int */
	MONO_COUNTER_UINT,    /* 32 bit uint */
	MONO_COUNTER_WORD,   /* pointer-sized int */
	MONO_COUNTER_LONG,   /* 64 bit int */
	MONO_COUNTER_ULONG,   /* 64 bit uint */
	MONO_COUNTER_DOUBLE,
	MONO_COUNTER_STRING, /* char* */
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

#endif /* __MONO_COUNTERS_H__ */

