/*
 * Copyright 2006-2010 Novell
 * Copyright 2011 Xamarin Inc
 */

#include <stdlib.h>
#include <glib.h>
#include "config.h"
#include "mono-counters.h"
#include "mono-proclib.h"

#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

struct _MonoCounter {
	MonoCounter *next;
	const char *name;
	void *addr;
	int type;
	size_t size;
};

static MonoCounter *counters = NULL;
static int valid_mask = 0;
static int set_mask = 0;

/**
 * mono_counter_get_variance:
 * @counter: counter to get the variance
 *
 * Variance specifies how the counter value is expected to behave between any two samplings.
 *
 * Returns: the monotonicity of the counter.
 */
int
mono_counter_get_variance (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_VARIANCE_MASK;
}

/**
 * mono_counter_get_unit:
 * @counter: counter to get the unit
 *
 * The unit gives a high level view of the unit that the counter is measuring.
 *
 * Returns: the unit of the counter.
 */
int
mono_counter_get_unit (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_UNIT_MASK;
}

/**
 * mono_counter_get_section:
 * @counter: counter to get the section
 *
 * Sections are the unit of organization between all counters.
 *
 * Returns: the section of the counter.
 */

int
mono_counter_get_section (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_SECTION_MASK;
}

/**
 * mono_counter_get_type:
 * @counter: counter to get the type
 *
 * Returns the type used to strong the value of the counter.
 *
 * Returns:the type of the counter.
 */
int
mono_counter_get_type (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_TYPE_MASK;
}

/**
 * mono_counter_get_name:
 * @counter: counter to get the name
 *
 * Returns the counter name. The string should not be freed.
 *
 * Returns the name of the counter.
 */

const char*
mono_counter_get_name (MonoCounter *counter)
{
	return counter->name;
}

/**
 * mono_counter_get_size:
 * @counter: counter to get the max size of the counter
 *
 * Use the returned size to create the buffer used with mono_counters_sample
 *
 * Returns: the max size of the counter data.
 */
size_t
mono_counter_get_size (MonoCounter *counter)
{
	return counter->size;
}

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
 * This function should not be used with counter types that require an explicit size such as string
 * as the counter size will be set to zero making them effectively useless.
 *
 *
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 */
void 
mono_counters_register (const char* name, int type, void *addr)
{
	int size;
	switch (type & MONO_COUNTER_TYPE_MASK) {
	case MONO_COUNTER_INT:
		size = sizeof (int);
		break;
	case MONO_COUNTER_UINT:
		size = sizeof (guint);
		break;
	case MONO_COUNTER_LONG:
	case MONO_COUNTER_TIME_INTERVAL:
		size = sizeof (gint64);
		break;
	case MONO_COUNTER_ULONG:
		size = sizeof (guint64);
		break;
	case MONO_COUNTER_WORD:
		size = sizeof (gssize);
		break;
	case MONO_COUNTER_DOUBLE:
		size = sizeof (double);
		break;
	case MONO_COUNTER_STRING:
		size = 0;
		break;
	default:
		g_assert_not_reached ();
	}
	mono_counters_register_with_size (name, type, addr, size);
}

/**
 * mono_counters_register_with_size:
 * @name: The name for this counters.
 * @type: One of the possible MONO_COUNTER types, or MONO_COUNTER_CALLBACK for a function pointer.
 * @addr: The address to register.
 * @size: Max size of the counter data.
 *
 * Register addr as the address of a counter of type @type.
 * Note that @name must be a valid string at all times until
 * mono_counters_dump () is called.
 *
 * It may be a function pointer if MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 *
 * The value of @size is ignored for types with fixed size such as int and long.
 *
 * Use @size for types that can have dynamic size such as string.
 *
 * If @size is negative, it's silently converted to zero.
 */
void
mono_counters_register_with_size (const char *name, int type, void *addr, int size)
{
	MonoCounter *counter;

	if ((type & MONO_COUNTER_VARIANCE_MASK) == 0)
		type |= MONO_COUNTER_MONOTONIC;
	if (size < 0)
		size = 0;

	counter = malloc (sizeof (MonoCounter));
	if (!counter)
		return;
	counter->name = name;
	counter->type = type;
	counter->addr = addr;
	counter->next = NULL;
	counter->size = size;

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
	void *buffer = g_malloc0 (counter->size);
	mono_counters_sample (counter, buffer, counter->size);

	switch (counter->type & MONO_COUNTER_TYPE_MASK) {
	case MONO_COUNTER_INT:
		fprintf (outfile, ENTRY_FMT "%d\n", counter->name, *(int*)buffer);
		break;
	case MONO_COUNTER_UINT:
		fprintf (outfile, ENTRY_FMT "%u\n", counter->name, *(guint*)buffer);
		break;
	case MONO_COUNTER_LONG:
		if ((counter->type & MONO_COUNTER_UNIT_MASK) == MONO_COUNTER_TIME)
			fprintf (outfile, ENTRY_FMT "%.2f ms\n", counter->name, (double)(*(gint64*)buffer) / 10000.0);
		else
			fprintf (outfile, ENTRY_FMT "%lld\n", counter->name, *(long long *)buffer);
		break;
	case MONO_COUNTER_ULONG:
		fprintf (outfile, ENTRY_FMT "%llu\n", counter->name, *(unsigned long long *)buffer);
		break;
	case MONO_COUNTER_WORD:
		fprintf (outfile, ENTRY_FMT "%zd\n", counter->name, *(gssize*)buffer);
		break;
	case MONO_COUNTER_DOUBLE:
		fprintf (outfile, ENTRY_FMT "%.4f\n", counter->name, *(double*)buffer);
		break;
	case MONO_COUNTER_STRING:
		fprintf (outfile, ENTRY_FMT "%s\n", counter->name, (counter->size == 0) ? "(null)" : (char*)buffer);
		break;
	case MONO_COUNTER_TIME_INTERVAL:
		fprintf (outfile, ENTRY_FMT "%.2f ms\n", counter->name, (double)(*(gint64*)buffer) / 1000.0);
		break;
	}

	g_free (buffer);
}

static gint64
user_time (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_USER_TIME);
}

static gint64
system_time (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_SYSTEM_TIME);
}

static gint64
total_time (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_TOTAL_TIME);
}

static gint64
working_set (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_WORKING_SET);
}

static gint64
private_bytes (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_PRIVATE_BYTES);
}

static gint64
virtual_bytes (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_VIRTUAL_BYTES);
}

static gint64
page_faults (void)
{
	return mono_process_get_data (GINT_TO_POINTER (mono_process_current_pid ()), MONO_PROCESS_FAULTS);
}

static double
cpu_load (int kind)
{
#if defined(TARGET_WIN32)
#elif defined(TARGET_MACH)
	double load [3];
	if (getloadavg (load, 3) > 0)
		return load [kind];
#else
	char buffer[512], *b;
	int len, i;
	FILE *f = fopen ("/proc/loadavg", "r");
	if (f) {
		len = fread (buffer, 1, sizeof (buffer) - 1, f);
		if (len > 0) {
			buffer [len < 511 ? len : 511] = 0;
			b = buffer;
			for (i = 0; i < 3; i++) {
				if (kind == i)
					return strtod (b, NULL);
				if (i < 2) {
					b = strchr (b, ' ');
					if (!b)
						return 0;
					b += 1;
				}
			}
		}
		fclose (f);
	}
#endif
	return 0;
}

static double
cpu_load_1min (void)
{
	return cpu_load (0);
}

static double
cpu_load_5min (void)
{
	return cpu_load (1);
}

static double
cpu_load_15min (void)
{
	return cpu_load (2);
}

static gboolean system_counters_initialized = FALSE;

#define SYSCOUNTER_TIME (MONO_COUNTER_SYSTEM | MONO_COUNTER_LONG | MONO_COUNTER_TIME | MONO_COUNTER_MONOTONIC | MONO_COUNTER_CALLBACK)
#define SYSCOUNTER_BYTES (MONO_COUNTER_SYSTEM | MONO_COUNTER_LONG | MONO_COUNTER_BYTES | MONO_COUNTER_VARIABLE | MONO_COUNTER_CALLBACK)
#define SYSCOUNTER_COUNT (MONO_COUNTER_SYSTEM | MONO_COUNTER_LONG | MONO_COUNTER_COUNT | MONO_COUNTER_MONOTONIC | MONO_COUNTER_CALLBACK)
#define SYSCOUNTER_LOAD (MONO_COUNTER_SYSTEM | MONO_COUNTER_DOUBLE | MONO_COUNTER_PERCENTAGE | MONO_COUNTER_VARIABLE | MONO_COUNTER_CALLBACK)

static void
initialize_system_counters (void)
{
	mono_counters_register ("User Time", SYSCOUNTER_TIME, &user_time);
	mono_counters_register ("System Time", SYSCOUNTER_TIME, &system_time);
	mono_counters_register ("Total Time", SYSCOUNTER_TIME, &total_time);
	mono_counters_register ("Working Set", SYSCOUNTER_BYTES, &working_set);
	mono_counters_register ("Private Bytes", SYSCOUNTER_BYTES, &private_bytes);
	mono_counters_register ("Virtual Bytes", SYSCOUNTER_BYTES, &virtual_bytes);
	mono_counters_register ("Page Faults", SYSCOUNTER_COUNT, &page_faults);
	mono_counters_register ("CPU Load Average - 1min", SYSCOUNTER_LOAD, &cpu_load_1min);
	mono_counters_register ("CPU Load Average - 5min", SYSCOUNTER_LOAD, &cpu_load_5min);
	mono_counters_register ("CPU Load Average - 15min", SYSCOUNTER_LOAD, &cpu_load_15min);

	system_counters_initialized = TRUE;
}

/**
 * mono_counters_foreach:
 * @cb: The callback that will be called for each counter.
 * @user_data: Value passed as second argument of the callback.
 *
 * Iterate over all counters and call @cb for each one of them. Stop iterating if
 * the callback returns FALSE.
 *
 */
void
mono_counters_foreach (CountersEnumCallback cb, gpointer user_data)
{
	MonoCounter *counter;

	if (!system_counters_initialized)
		initialize_system_counters ();

	for (counter = counters; counter; counter = counter->next) {
		if (!cb (counter, user_data))
			return;
	}
}

#define COPY_COUNTER(type,functype) do {	\
		size = sizeof (type);	\
		if (buffer_size < size)	\
			return -1;	\
		*(type*)buffer = cb ? ((functype)counter->addr) () : *(type*)counter->addr; \
	} while (0);

int
mono_counters_sample (MonoCounter *counter, void *buffer, int buffer_size)
{
	int cb = counter->type & MONO_COUNTER_CALLBACK;
	int size = -1;

	char *strval;

	switch (mono_counter_get_type (counter)) {
	case MONO_COUNTER_INT:
		COPY_COUNTER (int, IntFunc);
		break;
	case MONO_COUNTER_UINT:
		COPY_COUNTER (guint, UIntFunc);
		break;
	case MONO_COUNTER_LONG:
	case MONO_COUNTER_TIME_INTERVAL:
		COPY_COUNTER (gint64, LongFunc);
		break;
	case MONO_COUNTER_ULONG:
		COPY_COUNTER (guint64, ULongFunc);
		break;
	case MONO_COUNTER_WORD:
		COPY_COUNTER (gssize, PtrFunc);
		break;
	case MONO_COUNTER_DOUBLE:
		COPY_COUNTER (double, DoubleFunc);
		break;
	case MONO_COUNTER_STRING:
		if (buffer_size < counter->size)
			return -1;
		if (counter->size == 0)
			return 0;
		strval = cb ? ((StrFunc)counter->addr) () : (char*)counter->addr;
		if (!strval)
			return 0;
		size = counter->size;
		strncpy (buffer, strval, size - 1);
		((char*)buffer)[size - 1] = '\0';
	}

	return size;
}

static const char
section_names [][10] = {
	"JIT",
	"GC",
	"Metadata",
	"Generics",
	"Security",
	"Runtime",
	"System",
};

static void
mono_counters_dump_section (int section, int variance, FILE *outfile)
{
	MonoCounter *counter = counters;
	while (counter) {
		if ((counter->type & section) && (mono_counter_get_variance (counter) & variance))
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
 * To filter by variance, you can OR one or more variance with the specific section you want.
 * Use MONO_COUNTER_SECTION_MASK to dump all categories of a specific variance.
 */
void
mono_counters_dump (int section_mask, FILE *outfile)
{
	int i, j;
	int variance;
	section_mask &= valid_mask;
	if (!counters)
		return;

	variance = section_mask & MONO_COUNTER_VARIANCE_MASK;

	/* If no variance mask is supplied, we default to all kinds. */
	if (!variance)
		variance = MONO_COUNTER_VARIANCE_MASK;
	section_mask &= ~MONO_COUNTER_VARIANCE_MASK;

	for (j = 0, i = MONO_COUNTER_JIT; i < MONO_COUNTER_LAST_SECTION; j++, i <<= 1) {
		if ((section_mask & i) && (set_mask & i)) {
			fprintf (outfile, "\n%s statistics\n", section_names [j]);
			mono_counters_dump_section (i, variance, outfile);
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
	counters = NULL;
	while (counter) {
		MonoCounter *tmp = counter;
		counter = counter->next;
		free (tmp);
	}
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


