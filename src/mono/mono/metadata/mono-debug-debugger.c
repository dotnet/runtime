#include <config.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>

void (*mono_debugger_event_handler) (MonoDebuggerEvent event, gpointer data, gpointer data2) = NULL;

static GPtrArray *breakpoints = NULL;

void
mono_debugger_event (MonoDebuggerEvent event, gpointer data, gpointer data2)
{
	if (mono_debugger_event_handler)
		(* mono_debugger_event_handler) (event, data, data2);
}

/*
 * Debugger breakpoint interface.
 *
 * This interface is used to insert breakpoints on methods which are not yet JITed.
 * The debugging code keeps a list of all such breakpoints and automatically inserts the
 * breakpoint when the method is JITed.
 */
int
mono_debugger_insert_breakpoint_full (MonoMethodDesc *desc, gboolean use_trampoline)
{
	static int last_breakpoint_id = 0;
	MonoDebuggerBreakpointInfo *info;

	info = g_new0 (MonoDebuggerBreakpointInfo, 1);
	info->desc = desc;
	info->use_trampoline = use_trampoline;
	info->index = ++last_breakpoint_id;

	if (!breakpoints)
		breakpoints = g_ptr_array_new ();

	g_ptr_array_add (breakpoints, info);

	return info->index;
}

int
mono_debugger_remove_breakpoint (int breakpoint_id)
{
	int i;

	if (!breakpoints)
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->index != breakpoint_id)
			continue;

		mono_method_desc_free (info->desc);
		g_ptr_array_remove (breakpoints, info);
		g_free (info);
		return 1;
	}

	return 0;
}

int
mono_debugger_insert_breakpoint (const gchar *method_name, gboolean include_namespace)
{
	MonoMethodDesc *desc;
	gboolean in_the_debugger;

	desc = mono_method_desc_new (method_name, include_namespace);
	if (!desc)
		return 0;

	in_the_debugger = mono_debug_format == MONO_DEBUG_FORMAT_DEBUGGER;
	return mono_debugger_insert_breakpoint_full (desc, in_the_debugger);
}

int
mono_debugger_method_has_breakpoint (MonoMethod* method, gboolean use_trampoline)
{
	int i;

	if (!breakpoints || (method->wrapper_type != MONO_WRAPPER_NONE))
		return 0;

	for (i = 0; i < breakpoints->len; i++) {
		MonoDebuggerBreakpointInfo *info = g_ptr_array_index (breakpoints, i);

		if (info->use_trampoline != use_trampoline)
			continue;

		if (!mono_method_desc_full_match (info->desc, method))
			continue;

		return info->index;
	}

	return 0;
}

void
mono_debugger_trampoline_breakpoint_callback (void)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_BREAKPOINT_TRAMPOLINE, NULL, NULL);
}
