/**
 * \file
 * Copyright 2006-2010 Novell
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <stdlib.h>
#include <glib.h>
#include "config.h"

#include <mono/utils/mono-os-mutex.h>
#include "mono/utils/mono-counters.h"

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

static void
mono_counters_dump_section (int section, int variance, FILE *outfile)
{
}

/**
 * mono_counters_dump:
 * \param section_mask The sections to dump counters for
 * \param outfile a FILE to dump the results to; NULL will default to g_print
 * Displays the counts of all the enabled counters registered.
 * To filter by variance, you can OR one or more variance with the specific section you want.
 * Use \c MONO_COUNTER_SECTION_MASK to dump all categories of a specific variance.
 */
void
mono_counters_dump (int section_mask, FILE *outfile)
{
}

#undef FPRINTF_OR_G_PRINT

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


