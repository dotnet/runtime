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
#include <mono/metadata/exception.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-debug-debugger.h>
#include <mono/metadata/mono-endian.h>

static guint32 debugger_lock_level = 0;
static CRITICAL_SECTION debugger_lock_mutex;
static gboolean mono_debugger_use_debugger = FALSE;
static MonoObject *last_exception = NULL;

void (*mono_debugger_event_handler) (MonoDebuggerEvent event, guint64 data, guint64 arg) = NULL;

typedef struct {
	gpointer stack_pointer;
	MonoObject *exception_obj;
	guint32 stop;
} MonoDebuggerExceptionInfo;

typedef struct
{
	guint32 index;
	MonoMethod *method;
	MonoDebugMethodAddressList *address_list;
} MethodBreakpointInfo;

typedef struct {
	MonoImage *image;
	guint64 index;
	guint32 token;
	gchar *name_space;
	gchar *name;
} ClassInitCallback;

static GPtrArray *class_init_callbacks = NULL;

static int initialized = 0;

void
mono_debugger_lock (void)
{
	g_assert (initialized);
	EnterCriticalSection (&debugger_lock_mutex);
	debugger_lock_level++;
}

void
mono_debugger_unlock (void)
{
	g_assert (initialized);
	debugger_lock_level--;
	LeaveCriticalSection (&debugger_lock_mutex);
}

void
mono_debugger_initialize (gboolean use_debugger)
{
	MONO_GC_REGISTER_ROOT (last_exception);
	
	g_assert (!mono_debugger_use_debugger);

	InitializeCriticalSection (&debugger_lock_mutex);
	mono_debugger_use_debugger = use_debugger;
	initialized = 1;
}

void
mono_debugger_event (MonoDebuggerEvent event, guint64 data, guint64 arg)
{
	if (mono_debugger_event_handler)
		(* mono_debugger_event_handler) (event, data, arg);
}

void
mono_debugger_cleanup (void)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_FINALIZE_MANAGED_CODE, 0, 0);
	mono_debugger_event_handler = NULL;
}

gboolean
mono_debugger_unhandled_exception (gpointer addr, gpointer stack, MonoObject *exc)
{
	const gchar *name;

	if (!mono_debugger_use_debugger)
		return FALSE;

	name = mono_class_get_name (mono_object_get_class (exc));
	if (!strcmp (name, "ThreadAbortException"))
		return FALSE;

	// Prevent the object from being finalized.
	last_exception = exc;

	mono_debugger_event (MONO_DEBUGGER_EVENT_UNHANDLED_EXCEPTION,
			     (guint64) (gsize) exc, (guint64) (gsize) addr);
	return TRUE;
}

void
mono_debugger_handle_exception (gpointer addr, gpointer stack, MonoObject *exc)
{
	MonoDebuggerExceptionInfo info;

	if (!mono_debugger_use_debugger)
		return;

	// Prevent the object from being finalized.
	last_exception = exc;

	info.stack_pointer = stack;
	info.exception_obj = exc;
	info.stop = 0;

	mono_debugger_event (MONO_DEBUGGER_EVENT_HANDLE_EXCEPTION, (guint64) (gsize) &info,
			     (guint64) (gsize) addr);
}

gboolean
mono_debugger_throw_exception (gpointer addr, gpointer stack, MonoObject *exc)
{
	MonoDebuggerExceptionInfo info;

	if (!mono_debugger_use_debugger)
		return FALSE;

	// Prevent the object from being finalized.
	last_exception = exc;

	info.stack_pointer = stack;
	info.exception_obj = exc;
	info.stop = 0;

	mono_debugger_event (MONO_DEBUGGER_EVENT_THROW_EXCEPTION, (guint64) (gsize) &info,
			     (guint64) (gsize) addr);
	return info.stop != 0;
}

static gchar *
get_exception_message (MonoObject *exc)
{
	char *message = NULL;
	MonoString *str; 
	MonoMethod *method;
	MonoClass *klass;
	gint i;

	if (mono_object_isinst (exc, mono_defaults.exception_class)) {
		klass = exc->vtable->klass;
		method = NULL;
		while (klass && method == NULL) {
			for (i = 0; i < klass->method.count; ++i) {
				method = klass->methods [i];
				if (!strcmp ("ToString", method->name) &&
				    mono_method_signature (method)->param_count == 0 &&
				    method->flags & METHOD_ATTRIBUTE_VIRTUAL &&
				    method->flags & METHOD_ATTRIBUTE_PUBLIC) {
					break;
				}
				method = NULL;
			}
			
			if (method == NULL)
				klass = klass->parent;
		}

		g_assert (method);

		str = (MonoString *) mono_runtime_invoke (method, exc, NULL, NULL);
		if (str)
			message = mono_string_to_utf8 (str);
	}

	return message;
}

MonoObject *
mono_debugger_runtime_invoke (MonoMethod *method, void *obj, void **params, MonoObject **exc)
{
	MonoObject *retval;
	gchar *message;

	if (!strcmp (method->name, ".ctor")) {
		retval = obj = mono_object_new (mono_domain_get (), method->klass);

		mono_runtime_invoke (method, obj, params, exc);
	} else
		retval = mono_runtime_invoke (method, obj, params, exc);

	if (!exc || (*exc == NULL))
		return retval;

	message = get_exception_message (*exc);
	if (message) {
		*exc = (MonoObject *) mono_string_new_wrapper (message);
		g_free (message);
	}

	return retval;
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

void
mono_debugger_check_breakpoints (MonoMethod *method, MonoDebugMethodAddress *debug_info)
{
	MonoClass *klass;
	int i;

	if (!method_breakpoints)
		return;

	if (method->is_inflated)
		method = ((MonoMethodInflated *) method)->declaring;

	for (i = 0; i < method_breakpoints->len; i++) {
		MethodBreakpointInfo *info = g_ptr_array_index (method_breakpoints, i);

		if (method != info->method)
			continue;

		mono_debugger_event (MONO_DEBUGGER_EVENT_JIT_BREAKPOINT,
				     (guint64) (gsize) debug_info, info->index);
	}

	if (!class_init_callbacks)
		return;

	for (i = 0; i < class_init_callbacks->len; i++) {
		ClassInitCallback *info = g_ptr_array_index (class_init_callbacks, i);

		if ((method->token != info->token) || (method->klass->image != info->image))
			continue;

		mono_debugger_event (MONO_DEBUGGER_EVENT_JIT_BREAKPOINT,
				     (guint64) (gsize) debug_info, info->index);
	}
}

MonoClass *
mono_debugger_register_class_init_callback (MonoImage *image, const gchar *full_name,
					    guint32 method_token, guint32 index)
{
	ClassInitCallback *info;
	MonoClass *klass;
	gchar *name_space, *name, *pos;

	name = g_strdup (full_name);

	pos = strrchr (name, '.');
	if (pos) {
		name_space = name;
		*pos = 0;
		name = pos + 1;
	} else {
		name_space = NULL;
	}

	mono_loader_lock ();

	klass = mono_class_from_name (image, name_space ? name_space : "", name);
	if (klass && klass->inited && klass->methods) {
		mono_loader_unlock ();
		return klass;
	}

	info = g_new0 (ClassInitCallback, 1);
	info->image = image;
	info->index = index;
	info->token = method_token;
	info->name_space = name_space;
	info->name = name;

	if (!class_init_callbacks)
		class_init_callbacks = g_ptr_array_new ();

	g_ptr_array_add (class_init_callbacks, info);
	mono_loader_unlock ();
	return NULL;
}

void
mono_debugger_remove_class_init_callback (int index)
{
	int i;

	if (!class_init_callbacks)
		return;

	for (i = 0; i < class_init_callbacks->len; i++) {
		ClassInitCallback *info = g_ptr_array_index (class_init_callbacks, i);

		if (info->index != index)
			continue;

		g_ptr_array_remove (class_init_callbacks, info);
		if (info->name_space)
			g_free (info->name_space);
		else
			g_free (info->name);
		g_free (info);
	}
}

void
mono_debugger_class_initialized (MonoClass *klass)
{
	int i;

	if (!class_init_callbacks)
		return;

 again:
	for (i = 0; i < class_init_callbacks->len; i++) {
		ClassInitCallback *info = g_ptr_array_index (class_init_callbacks, i);

		if (info->name_space && strcmp (info->name_space, klass->name_space))
			continue;
		if (strcmp (info->name, klass->name))
			continue;

		mono_debugger_event (MONO_DEBUGGER_EVENT_CLASS_INITIALIZED,
				     (guint64) (gsize) klass, info->index);

		if (info->token) {
			int j;

			for (j = 0; j < klass->method.count; j++) {
				if (klass->methods [j]->token != info->token)
					continue;

				mono_debugger_insert_method_breakpoint (klass->methods [j], info->index);
			}
		}

		g_ptr_array_remove (class_init_callbacks, info);
		if (info->name_space)
			g_free (info->name_space);
		else
			g_free (info->name);
		g_free (info);
		goto again;
	}
}
