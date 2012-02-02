/*
 * Copyright 2006-2010 Novell
 * Copyright 2011 Xamarin Inc
 */

#include <stdlib.h>
#include <glib.h>
#include "mono-counters.h"

typedef struct _MonoCounter MonoCounter;

struct _MonoCounter {
	MonoCounter *next;
	const char *name;
	void *addr;
	int type;
};

static MonoCounter *counters = NULL;
static int valid_mask = 0;
static int set_mask = 0;

/**
 * mono_counters_enable:
 * @section_mask: a mask listing the sections that will be displayed
 *
 * This is used to track which counters will be displayed.
 */
void
mono_counters_enable (int section_mask)
{
	valid_mask = section_mask & MONO_COUNTER_SECTION_MASK;
}

/**
 * mono_counters_register:
 * @name: The name for this counters.
 * @type: One of the possible MONO_COUNTER types, or MONO_COUNTER_CALLBACK for a function pointer.
 * @addr: The address to register.
 *
 * Register addr as the address of a counter of type type.
 * Note that @name must be a valid string at all times until
 * mono_counters_dump () is called.
 *
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 */
void 
mono_counters_register (const char* name, int type, void *addr)
{
	MonoCounter *counter;
	if (!(type & valid_mask))
		return;
	counter = malloc (sizeof (MonoCounter));
	if (!counter)
		return;
	counter->name = name;
	counter->type = type;
	counter->addr = addr;
	counter->next = NULL;

	set_mask |= type;

	/* Append */
	if (counters) {
		MonoCounter *item = counters;
		while (item->next)
			item = item->next;
		item->next = counter;
	} else {
		counters = counter;
	}
}

typedef int (*IntFunc) (void);
typedef guint (*UIntFunc) (void);
typedef gint64 (*LongFunc) (void);
typedef guint64 (*ULongFunc) (void);
typedef gssize (*PtrFunc) (void);
typedef double (*DoubleFunc) (void);
typedef char* (*StrFunc) (void);

#define ENTRY_FMT "%-36s: "
static void
dump_counter (MonoCounter *counter, FILE *outfile) {
	int intval;
	guint uintval;
	gint64 int64val;
	guint64 uint64val;
	gssize wordval;
	double dval;
	const char *str;
	switch (counter->type & MONO_COUNTER_TYPE_MASK) {
	case MONO_COUNTER_INT:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      intval = ((IntFunc)counter->addr) ();
	      else
		      intval = *(int*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%d\n", counter->name, intval);
	      break;
	case MONO_COUNTER_UINT:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      uintval = ((UIntFunc)counter->addr) ();
	      else
		      uintval = *(guint*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%u\n", counter->name, uintval);
	      break;
	case MONO_COUNTER_LONG:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      int64val = ((LongFunc)counter->addr) ();
	      else
		      int64val = *(gint64*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%lld\n", counter->name, (long long)int64val);
	      break;
	case MONO_COUNTER_ULONG:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      uint64val = ((ULongFunc)counter->addr) ();
	      else
		      uint64val = *(guint64*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%llu\n", counter->name, (unsigned long long)uint64val);
	      break;
	case MONO_COUNTER_WORD:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      wordval = ((PtrFunc)counter->addr) ();
	      else
		      wordval = *(gssize*)counter->addr;
#if SIZEOF_VOID_P == 8
	      fprintf (outfile, ENTRY_FMT "%lld\n", counter->name, (gint64)wordval);
#else
	      fprintf (outfile, ENTRY_FMT "%d\n", counter->name, (gint)wordval);
#endif
	      break;
	case MONO_COUNTER_DOUBLE:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      dval = ((DoubleFunc)counter->addr) ();
	      else
		      dval = *(double*)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%.4f\n", counter->name, dval);
	      break;
	case MONO_COUNTER_STRING:
	      if (counter->type & MONO_COUNTER_CALLBACK)
		      str = ((StrFunc)counter->addr) ();
	      else
		      str = *(char**)counter->addr;
	      fprintf (outfile, ENTRY_FMT "%s\n", counter->name, str);
	      break;
	case MONO_COUNTER_TIME_INTERVAL:
	    if (counter->type & MONO_COUNTER_CALLBACK)
		      int64val = ((LongFunc)counter->addr) ();
	    else
		      int64val = *(gint64*)counter->addr;
	    fprintf (outfile, ENTRY_FMT "%.2f ms\n", counter->name, (double)int64val / 1000.0);
	    break;
	}
}

static const char
section_names [][10] = {
	"JIT",
	"GC",
	"Metadata",
	"Generics",
	"Security"
};

static void
mono_counters_dump_section (int section, FILE *outfile)
{
	MonoCounter *counter = counters;
	while (counter) {
		if (counter->type & section)
			dump_counter (counter, outfile);
		counter = counter->next;
	}
}

/**
 * mono_counters_dump:
 * @section_mask: The sections to dump counters for
 * @outfile: a FILE to dump the results to
 *
 * Displays the counts of all the enabled counters registered. 
 */
void
mono_counters_dump (int section_mask, FILE *outfile)
{
	int i, j;
	section_mask &= valid_mask;
	if (!counters)
		return;
	for (j = 0, i = MONO_COUNTER_JIT; i < MONO_COUNTER_LAST_SECTION; j++, i <<= 1) {
		if ((section_mask & i) && (set_mask & i)) {
			fprintf (outfile, "\n%s statistics\n", section_names [j]);
			mono_counters_dump_section (i, outfile);
		}
	}

	fflush (outfile);
}

/**
 * mono_counters_cleanup:
 *
 * Perform any needed cleanup at process exit.
 */
void
mono_counters_cleanup (void)
{
	MonoCounter *counter = counters;
	while (counter) {
		MonoCounter *tmp = counters;
		counter = counter->next;
		free (tmp);
	}
	counters = NULL;
}

static MonoResourceCallback limit_reached = NULL;
static uintptr_t resource_limits [MONO_RESOURCE_COUNT * 2];

/**
 * mono_runtime_resource_check_limit:
 * @resource_type: one of the #MonoResourceType enum values
 * @value: the current value of the resource usage
 *
 * Check if a runtime resource limit has been reached. This function
 * is intended to be used by the runtime only.
 */
void
mono_runtime_resource_check_limit (int resource_type, uintptr_t value)
{
	if (!limit_reached)
		return;
	/* check the hard limit first */
	if (value > resource_limits [resource_type * 2 + 1]) {
		limit_reached (resource_type, value, 0);
		return;
	}
	if (value > resource_limits [resource_type * 2])
		limit_reached (resource_type, value, 1);
}

/**
 * mono_runtime_resource_limit:
 * @resource_type: one of the #MonoResourceType enum values
 * @soft_limit: the soft limit value
 * @hard_limit: the hard limit value
 *
 * This function sets the soft and hard limit for runtime resources. When the limit
 * is reached, a user-specified callback is called. The callback runs in a restricted
 * environment, in which the world coult be stopped, so it can't take locks, perform
 * allocations etc. The callback may be called multiple times once a limit has been reached
 * if action is not taken to decrease the resource use.
 *
 * Returns: 0 on error or a positive integer otherwise.
 */
int
mono_runtime_resource_limit (int resource_type, uintptr_t soft_limit, uintptr_t hard_limit)
{
	if (resource_type >= MONO_RESOURCE_COUNT || resource_type < 0)
		return 0;
	if (soft_limit > hard_limit)
		return 0;
	resource_limits [resource_type * 2] = soft_limit;
	resource_limits [resource_type * 2 + 1] = hard_limit;
	return 1;
}

/**
 * mono_runtime_resource_set_callback:
 * @callback: a function pointer
 * 
 * Set the callback to be invoked when a resource limit is reached.
 * The callback will receive the resource type, the resource amount in resource-specific
 * units and a flag indicating whether the soft or hard limit was reached.
 */
void
mono_runtime_resource_set_callback (MonoResourceCallback callback)
{
	limit_reached = callback;
}


