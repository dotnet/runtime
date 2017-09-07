/**
 * \file
 * Copyright 2006-2010 Novell
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <stdlib.h>
#include <glib.h>
#include "config.h"
#include "mono-counters.h"
#include "mono-proclib.h"
#include "mono-os-mutex.h"

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
static mono_mutex_t counters_mutex;

static volatile gboolean initialized = FALSE;

static int valid_mask = 0;
static int set_mask = 0;

static GSList *register_callbacks = NULL;

static void initialize_system_counters (void);

/**
 * mono_counter_get_variance:
 * \param counter counter to get the variance
 *
 * Variance specifies how the counter value is expected to behave between any two samplings.
 *
 * \returns the monotonicity of the counter.
 */
int
mono_counter_get_variance (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_VARIANCE_MASK;
}

/**
 * mono_counter_get_unit:
 * \param counter counter to get the unit
 *
 * The unit gives a high level view of the unit that the counter is measuring.
 *
 * \returns the unit of the counter.
 */
int
mono_counter_get_unit (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_UNIT_MASK;
}

/**
 * mono_counter_get_section:
 * \param counter counter to get the section
 * Sections are the unit of organization between all counters.
 * \returns the section of the counter.
 */

int
mono_counter_get_section (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_SECTION_MASK;
}

/**
 * mono_counter_get_type:
 * \param counter counter to get the type
 * \returns the type used to store the value of the counter.
 */
int
mono_counter_get_type (MonoCounter *counter)
{
	return counter->type & MONO_COUNTER_TYPE_MASK;
}

/**
 * mono_counter_get_name:
 * \param counter counter to get the name
 * \returns the counter name. The string should not be freed.
 */

const char*
mono_counter_get_name (MonoCounter *counter)
{
	return counter->name;
}

/**
 * mono_counter_get_size:
 * \param counter counter to get the max size of the counter
 * Use the returned size to create the buffer used with \c mono_counters_sample
 * \returns the max size of the counter data.
 */
size_t
mono_counter_get_size (MonoCounter *counter)
{
	return counter->size;
}

/**
 * mono_counters_enable:
 * \param sectionmask a mask listing the sections that will be displayed
 * This is used to track which counters will be displayed.
 */
void
mono_counters_enable (int section_mask)
{
	valid_mask = section_mask & MONO_COUNTER_SECTION_MASK;
}

void
mono_counters_init (void)
{
	if (initialized)
		return;

	mono_os_mutex_init (&counters_mutex);

	initialize_system_counters ();

	initialized = TRUE;
}

static void
register_internal (const char *name, int type, void *addr, int size)
{
	MonoCounter *counter;
	GSList *register_callback;

	g_assert (size >= 0);
	if ((type & MONO_COUNTER_VARIANCE_MASK) == 0)
		type |= MONO_COUNTER_MONOTONIC;

	mono_os_mutex_lock (&counters_mutex);

	for (counter = counters; counter; counter = counter->next) {
		if (counter->addr == addr) {
			g_warning ("you are registering the same counter address twice: %s at %p", name, addr);
			mono_os_mutex_unlock (&counters_mutex);
			return;
		}
	}

	counter = (MonoCounter *) g_malloc (sizeof (MonoCounter));
	if (!counter) {
		mono_os_mutex_unlock (&counters_mutex);
		return;
	}
	counter->name = g_strdup (name);
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

	for (register_callback = register_callbacks; register_callback; register_callback = register_callback->next)
		((MonoCounterRegisterCallback)register_callback->data) (counter);

	mono_os_mutex_unlock (&counters_mutex);
}

/**
 * mono_counters_register:
 * \param name The name for this counters.
 * \param type One of the possible \c MONO_COUNTER types, or \c MONO_COUNTER_CALLBACK for a function pointer.
 * \param addr The address to register.
 *
 * Register \p addr as the address of a counter of type type.
 * Note that \p name must be a valid string at all times until
 * \c mono_counters_dump() is called.
 *
 * This function should not be used with counter types that require an explicit size such as string
 * as the counter size will be set to zero making them effectively useless.
 *
 * It may be a function pointer if \c MONO_COUNTER_CALLBACK is specified:
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

	if (!initialized)
		g_debug ("counters not enabled");
	else
		register_internal (name, type, addr, size);
}

/**
 * mono_counters_register_with_size:
 * \param name The name for this counters.
 * \param type One of the possible MONO_COUNTER types, or MONO_COUNTER_CALLBACK for a function pointer.
 * \param addr The address to register.
 * \param size Max size of the counter data.
 *
 * Register \p addr as the address of a counter of type \p type.
 * Note that \p name must be a valid string at all times until
 * \c mono_counters_dump() is called.
 *
 * It may be a function pointer if \c MONO_COUNTER_CALLBACK is specified:
 * the function should return the value and take no arguments.
 *
 * The value of \p size is ignored for types with fixed size such as int and long.
 *
 * Use \p size for types that can have dynamic size such as string.
 *
 * If \p size is negative, it's silently converted to zero.
 */
void
mono_counters_register_with_size (const char *name, int type, void *addr, int size)
{
	if (!initialized)
		g_debug ("counters not enabled");
	else
		register_internal (name, type, addr, size);
}

/**
 * mono_counters_on_register
 * \param callback function to callback when a counter is registered
 * Add a callback that is going to be called when a counter is registered
 */
void
mono_counters_on_register (MonoCounterRegisterCallback callback)
{
	if (!initialized) {
		g_debug ("counters not enabled");
		return;
	}

	mono_os_mutex_lock (&counters_mutex);
	register_callbacks = g_slist_append (register_callbacks, (gpointer) callback);
	mono_os_mutex_unlock (&counters_mutex);
}

typedef int (*IntFunc) (void);
typedef guint (*UIntFunc) (void);
typedef gint64 (*LongFunc) (void);
typedef guint64 (*ULongFunc) (void);
typedef gssize (*PtrFunc) (void);
typedef double (*DoubleFunc) (void);
typedef char* (*StrFunc) (void);

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


// If cpu_load gets inlined on Windows then cpu_load_1min, cpu_load_5min and cpu_load_15min can be folded into a single function and that will
// cause a failure when registering counters since the same function address will be used by all three functions. Preventing this method from being inlined
// will make sure the registered callback functions remains unique.
#ifdef _MSC_VER
__declspec(noinline)
#endif
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
		fclose (f);
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

#define SYSCOUNTER_TIME (MONO_COUNTER_SYSTEM | MONO_COUNTER_LONG | MONO_COUNTER_TIME | MONO_COUNTER_MONOTONIC | MONO_COUNTER_CALLBACK)
#define SYSCOUNTER_BYTES (MONO_COUNTER_SYSTEM | MONO_COUNTER_LONG | MONO_COUNTER_BYTES | MONO_COUNTER_VARIABLE | MONO_COUNTER_CALLBACK)
#define SYSCOUNTER_COUNT (MONO_COUNTER_SYSTEM | MONO_COUNTER_LONG | MONO_COUNTER_COUNT | MONO_COUNTER_MONOTONIC | MONO_COUNTER_CALLBACK)
#define SYSCOUNTER_LOAD (MONO_COUNTER_SYSTEM | MONO_COUNTER_DOUBLE | MONO_COUNTER_PERCENTAGE | MONO_COUNTER_VARIABLE | MONO_COUNTER_CALLBACK)

static void
initialize_system_counters (void)
{
	register_internal ("User Time", SYSCOUNTER_TIME, (gpointer) &user_time, sizeof (gint64));
	register_internal ("System Time", SYSCOUNTER_TIME, (gpointer) &system_time, sizeof (gint64));
	register_internal ("Total Time", SYSCOUNTER_TIME, (gpointer) &total_time, sizeof (gint64));
	register_internal ("Working Set", SYSCOUNTER_BYTES, (gpointer) &working_set, sizeof (gint64));
	register_internal ("Private Bytes", SYSCOUNTER_BYTES, (gpointer) &private_bytes, sizeof (gint64));
	register_internal ("Virtual Bytes", SYSCOUNTER_BYTES, (gpointer) &virtual_bytes, sizeof (gint64));
	register_internal ("Page Faults", SYSCOUNTER_COUNT, (gpointer) &page_faults, sizeof (gint64));
	register_internal ("CPU Load Average - 1min", SYSCOUNTER_LOAD, (gpointer) &cpu_load_1min, sizeof (double));
	register_internal ("CPU Load Average - 5min", SYSCOUNTER_LOAD, (gpointer) &cpu_load_5min, sizeof (double));
	register_internal ("CPU Load Average - 15min", SYSCOUNTER_LOAD, (gpointer) &cpu_load_15min, sizeof (double));
}

/**
 * mono_counters_foreach:
 * \param cb The callback that will be called for each counter.
 * \param user_data Value passed as second argument of the callback.
 * Iterate over all counters and call \p cb for each one of them. Stop iterating if
 * the callback returns FALSE.
 */
void
mono_counters_foreach (CountersEnumCallback cb, gpointer user_data)
{
	MonoCounter *counter;

	if (!initialized) {
		g_debug ("counters not enabled");
		return;
	}

	mono_os_mutex_lock (&counters_mutex);

	for (counter = counters; counter; counter = counter->next) {
		if (!cb (counter, user_data)) {
			mono_os_mutex_unlock (&counters_mutex);
			return;
		}
	}

	mono_os_mutex_unlock (&counters_mutex);
}

#define COPY_COUNTER(type,functype) do {	\
		size = sizeof (type);	\
		if (buffer_size < size)	\
			size = -1;	\
		else			\
			*(type*)buffer = cb ? ((functype)counter->addr) () : *(type*)counter->addr; \
	} while (0);

/* lockless */
static int
sample_internal (MonoCounter *counter, void *buffer, int buffer_size)
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
		if (buffer_size < counter->size) {
			size = -1;
		} else if (counter->size == 0) {
			size = 0;
		} else {
			strval = cb ? ((StrFunc)counter->addr) () : (char*)counter->addr;
			if (!strval) {
				size = 0;
			} else {
				size = counter->size;
				memcpy ((char *) buffer, strval, size - 1);
				((char*)buffer)[size - 1] = '\0';
			}
		}
	}

	return size;
}

int
mono_counters_sample (MonoCounter *counter, void *buffer, int buffer_size)
{
	if (!initialized) {
		g_debug ("counters not enabled");
		return -1;
	}

	return sample_internal (counter, buffer, buffer_size);
}

#define ENTRY_FMT "%-36s: "
static void
dump_counter (MonoCounter *counter, FILE *outfile) {
	void *buffer = g_malloc0 (counter->size);
	int size = sample_internal (counter, buffer, counter->size);

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
		if ((counter->type & MONO_COUNTER_UNIT_MASK) == MONO_COUNTER_TIME)
			fprintf (outfile, ENTRY_FMT "%.2f ms\n", counter->name, (double)(*(guint64*)buffer) / 10000.0);
		else
			fprintf (outfile, ENTRY_FMT "%llu\n", counter->name, *(unsigned long long *)buffer);
		break;
	case MONO_COUNTER_WORD:
		fprintf (outfile, ENTRY_FMT "%zd\n", counter->name, *(gssize*)buffer);
		break;
	case MONO_COUNTER_DOUBLE:
		fprintf (outfile, ENTRY_FMT "%.4f\n", counter->name, *(double*)buffer);
		break;
	case MONO_COUNTER_STRING:
		fprintf (outfile, ENTRY_FMT "%s\n", counter->name, (size == 0) ? "(null)" : (char*)buffer);
		break;
	case MONO_COUNTER_TIME_INTERVAL:
		fprintf (outfile, ENTRY_FMT "%.2f ms\n", counter->name, (double)(*(gint64*)buffer) / 1000.0);
		break;
	}

	g_free (buffer);
}

static const char
section_names [][12] = {
	"JIT",
	"GC",
	"Metadata",
	"Generics",
	"Security",
	"Runtime",
	"System",
	"", // MONO_COUNTER_PERFCOUNTERS - not used.
	"Profiler",
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
 * \param section_mask The sections to dump counters for
 * \param outfile a FILE to dump the results to
 * Displays the counts of all the enabled counters registered. 
 * To filter by variance, you can OR one or more variance with the specific section you want.
 * Use \c MONO_COUNTER_SECTION_MASK to dump all categories of a specific variance.
 */
void
mono_counters_dump (int section_mask, FILE *outfile)
{
	int i, j;
	int variance;
	section_mask &= valid_mask;

	if (!initialized)
		return;

	mono_os_mutex_lock (&counters_mutex);

	if (!counters) {
		mono_os_mutex_unlock (&counters_mutex);
		return;
	}

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
	mono_os_mutex_unlock (&counters_mutex);
}

/**
 * mono_counters_cleanup:
 *
 * Perform any needed cleanup at process exit.
 */
void
mono_counters_cleanup (void)
{
	MonoCounter *counter;

	if (!initialized)
		return;

	mono_os_mutex_lock (&counters_mutex);

	counter = counters;
	counters = NULL;
	while (counter) {
		MonoCounter *tmp = counter;
		counter = counter->next;
		g_free ((void*)tmp->name);
		g_free (tmp);
	}

	mono_os_mutex_unlock (&counters_mutex);
}

static MonoResourceCallback limit_reached = NULL;
static uintptr_t resource_limits [MONO_RESOURCE_COUNT * 2];

/**
 * mono_runtime_resource_check_limit:
 * \param resource_type one of the \c MonoResourceType enum values
 * \param value the current value of the resource usage
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
 * \param resource_type one of the \c MonoResourceType enum values
 * \param soft_limit the soft limit value
 * \param hard_limit the hard limit value
 * This function sets the soft and hard limit for runtime resources. When the limit
 * is reached, a user-specified callback is called. The callback runs in a restricted
 * environment, in which the world coult be stopped, so it can't take locks, perform
 * allocations etc. The callback may be called multiple times once a limit has been reached
 * if action is not taken to decrease the resource use.
 * \returns 0 on error or a positive integer otherwise.
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
 * \param callback a function pointer
 * Set the callback to be invoked when a resource limit is reached.
 * The callback will receive the resource type, the resource amount in resource-specific
 * units and a flag indicating whether the soft or hard limit was reached.
 */
void
mono_runtime_resource_set_callback (MonoResourceCallback callback)
{
	limit_reached = callback;
}


