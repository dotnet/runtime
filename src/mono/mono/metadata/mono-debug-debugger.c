/*
 * mono-debug-debugger.c: 
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 */

#include <config.h>
#include <stdlib.h>
#include <string.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/metadata.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/domain-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-endian.h>

static guint32 debugger_lock_level = 0;
static mono_mutex_t debugger_lock_mutex;

typedef struct
{
	guint32 index;
	MonoMethod *method;
	MonoDebugMethodAddressList *address_list;
} MethodBreakpointInfo;

static int initialized = 0;

void
mono_debugger_lock (void)
{
	g_assert (initialized);
	mono_mutex_lock (&debugger_lock_mutex);
	debugger_lock_level++;
}

void
mono_debugger_unlock (void)
{
	g_assert (initialized);
	debugger_lock_level--;
	mono_mutex_unlock (&debugger_lock_mutex);
}

void
mono_debugger_initialize ()
{
	mono_mutex_init_recursive (&debugger_lock_mutex);
	initialized = 1;
}

/*
 * Debugger breakpoint interface.
 *
 * This interface is used to insert breakpoints on methods which are not yet JITed.
 * The debugging code keeps a list of all such breakpoints and automatically inserts the
 * breakpoint when the method is JITed.
 */

static GPtrArray *method_breakpoints = NULL;

MonoDebugMethodAddressList *
mono_debugger_insert_method_breakpoint (MonoMethod *method, guint64 index)
{
	MethodBreakpointInfo *info;

	info = g_new0 (MethodBreakpointInfo, 1);
	info->method = method;
	info->index = index;

	info->address_list = mono_debug_lookup_method_addresses (method);

	if (!method_breakpoints)
		method_breakpoints = g_ptr_array_new ();

	g_ptr_array_add (method_breakpoints, info);

	return info->address_list;
}

int
mono_debugger_remove_method_breakpoint (guint64 index)
{
	int i;

	if (!method_breakpoints)
		return 0;

	for (i = 0; i < method_breakpoints->len; i++) {
		MethodBreakpointInfo *info = g_ptr_array_index (method_breakpoints, i);

		if (info->index != index)
			continue;

		g_ptr_array_remove (method_breakpoints, info);
		g_free (info->address_list);
		g_free (info);
		return 1;
	}

	return 0;
}
