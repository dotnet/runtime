/**
 * \file
 * Copyright 2006-2010 Novell
 * Copyright 2011 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <stdlib.h>
#include <glib.h>
#include "config.h"

#include <mono/utils/mono-counters.h>

// NOTE: this was turned into a no-op in dotnet/runtime, check git history for the
//       original implementation in case we want to bring this back in the future

struct _MonoCounter {
	MonoCounter *next;
	const char *name;
	void *addr;
	int type;
	size_t size;
};

int
mono_counter_get_variance (MonoCounter *counter)
{
	return 0;
}

int
mono_counter_get_unit (MonoCounter *counter)
{
	return 0;
}

int
mono_counter_get_section (MonoCounter *counter)
{
	return 0;
}

int
mono_counter_get_type (MonoCounter *counter)
{
	return 0;
}

const char*
mono_counter_get_name (MonoCounter *name)
{
	return NULL;
}

size_t
mono_counter_get_size (MonoCounter *counter)
{
	return 0;
}

void
mono_counters_enable (int section_mask)
{
}

void
mono_counters_init (void)
{
}

void
mono_counters_register (const char* name, int type, void *addr)
{
}

void
mono_counters_register_with_size (const char *name, int type, void *addr, int size)
{
}

void
mono_counters_on_register (MonoCounterRegisterCallback callback)
{
}

void
mono_counters_foreach (CountersEnumCallback cb, gpointer user_data)
{
}

int
mono_counters_sample (MonoCounter *counter, void *buffer, int buffer_size)
{
	return 0;
}

void
mono_counters_dump (int section_mask, FILE *outfile)
{
}

void
mono_counters_cleanup (void)
{
}

void
mono_runtime_resource_check_limit (int resource_type, uintptr_t value)
{
}

int
mono_runtime_resource_limit (int resource_type, uintptr_t soft_limit, uintptr_t hard_limit)
{
	return 1;
}

void
mono_runtime_resource_set_callback (MonoResourceCallback callback)
{
}
