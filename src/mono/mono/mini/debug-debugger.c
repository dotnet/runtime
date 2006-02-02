#include <config.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>
#define _IN_THE_MONO_DEBUGGER
#include "debug-debugger.h"
#include <libgc/include/libgc-mono-debugger.h>
#include "mini.h"
#include <unistd.h>
#include <locale.h>
#include <string.h>

static GPtrArray *thread_array = NULL;
static MonoMethod *debugger_main_method;

static guint64 debugger_insert_breakpoint (guint64 method_argument, const gchar *string_argument);
static guint64 debugger_remove_breakpoint (guint64 breakpoint);
static guint64 debugger_compile_method (guint64 method_arg);
static guint64 debugger_get_virtual_method (guint64 class_arg, guint64 method_arg);
static guint64 debugger_get_boxed_object (guint64 klass_arg, guint64 val_arg);
static guint64 debugger_create_string (guint64 dummy_argument, const gchar *string_argument);
static guint64 debugger_class_get_static_field_data (guint64 klass);
static guint64 debugger_lookup_class (guint64 image_argument, guint64 token_arg);
static guint64 debugger_lookup_type (guint64 dummy_argument, const gchar *string_argument);
static guint64 debugger_lookup_assembly (guint64 dummy_argument, const gchar *string_argument);
static guint64 debugger_run_finally (guint64 argument1, guint64 argument2);
static guint64 debugger_get_thread_id (void);
static void debugger_attach (void);

static guint8 notification_function [16];
static void (*mono_debugger_notification_function) (guint64 command, guint64 data, guint64 data2);

static MonoDebuggerMetadataInfo debugger_metadata_info = {
	sizeof (MonoDebuggerMetadataInfo),
	sizeof (MonoDefaults),
	&mono_defaults,
	sizeof (MonoType),
	sizeof (MonoArrayType),
	sizeof (MonoClass),
	G_STRUCT_OFFSET (MonoClass, instance_size),
	G_STRUCT_OFFSET (MonoClass, parent),
	G_STRUCT_OFFSET (MonoClass, type_token),
	G_STRUCT_OFFSET (MonoClass, fields),
	G_STRUCT_OFFSET (MonoClass, methods),
	G_STRUCT_OFFSET (MonoClass, method.count),
	G_STRUCT_OFFSET (MonoClass, this_arg),
	G_STRUCT_OFFSET (MonoClass, byval_arg),
	G_STRUCT_OFFSET (MonoClass, generic_class),
	G_STRUCT_OFFSET (MonoClass, generic_container),
	sizeof (MonoClassField)
};

/*
 * This is a global data symbol which is read by the debugger.
 */
MonoDebuggerInfo MONO_DEBUGGER__debugger_info = {
	MONO_DEBUGGER_MAGIC,
	MONO_DEBUGGER_VERSION,
	sizeof (MonoDebuggerInfo),
	sizeof (MonoSymbolTable),
	0,
	notification_function,
	mono_trampoline_code,
	&mono_symbol_table,
	&debugger_metadata_info,
	&debugger_compile_method,
	&debugger_get_virtual_method,
	&debugger_get_boxed_object,
	&debugger_insert_breakpoint,
	&debugger_remove_breakpoint,
	&mono_debugger_runtime_invoke,
	&debugger_create_string,
	&debugger_class_get_static_field_data,
	&debugger_lookup_class,
	&debugger_lookup_type,
	&debugger_lookup_assembly,
	&debugger_run_finally,
	&debugger_get_thread_id,
	&debugger_attach
};

typedef struct {
	gpointer end_stack;
	gpointer start_stack;
	guint64 tid;
} MonoDebuggerThread;

static guint64
debugger_insert_breakpoint (guint64 method_argument, const gchar *string_argument)
{
	MonoMethodDesc *desc;

	desc = mono_method_desc_new (string_argument, TRUE);
	if (!desc)
		return 0;

	return (guint64) mono_debugger_insert_breakpoint_full (desc);
}

static guint64
debugger_remove_breakpoint (guint64 breakpoint)
{
	return mono_debugger_remove_breakpoint (breakpoint);
}

static gpointer
debugger_compile_method_cb (MonoMethod *method)
{
	gpointer retval;

	mono_debugger_lock ();
	retval = mono_compile_method (method);
	mono_debugger_unlock ();

	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_METHOD_COMPILED, (guint64) (gsize) retval, 0);

	return retval;
}

static guint64
debugger_compile_method (guint64 method_arg)
{
	MonoMethod *method = (MonoMethod *) GUINT_TO_POINTER ((gsize) method_arg);

	return (guint64) (gsize) debugger_compile_method_cb (method);
}

static guint64
debugger_get_virtual_method (guint64 object_arg, guint64 method_arg)
{
	MonoObject *object = (MonoObject *) GUINT_TO_POINTER ((gsize) object_arg);
	MonoMethod *method = (MonoMethod *) GUINT_TO_POINTER ((gsize) method_arg);

	if (mono_class_is_valuetype (mono_method_get_class (method)))
		return method_arg;

	return (guint64) (gsize) mono_object_get_virtual_method (object, method);
}

static guint64
debugger_get_boxed_object (guint64 klass_arg, guint64 val_arg)
{
	static MonoObject *last_boxed_object = NULL;
	MonoClass *klass = (MonoClass *) GUINT_TO_POINTER ((gsize) klass_arg);
	gpointer val = (gpointer) GUINT_TO_POINTER ((gsize) val_arg);
	MonoObject *boxed;

	if (!mono_class_is_valuetype (klass))
		return val_arg;

	boxed = mono_value_box (mono_domain_get (), klass, val);
	last_boxed_object = boxed; // Protect the object from being garbage collected

	return (guint64) (gsize) boxed;
}

static guint64
debugger_create_string (guint64 dummy_argument, const gchar *string_argument)
{
	return (guint64) (gsize) mono_string_new_wrapper (string_argument);
}

static guint64
debugger_lookup_type (guint64 dummy_argument, const gchar *string_argument)
{
	guint64 retval;

	mono_debugger_lock ();
	// retval = mono_debugger_lookup_type (string_argument);
	retval = -1;
	mono_debugger_unlock ();
	return retval;
}

static guint64
debugger_lookup_class (guint64 image_argument, guint64 token_argument)
{
	MonoImage *image = (MonoImage *) GUINT_TO_POINTER ((gsize) image_argument);
	guint32 token = (guint32) token_argument;
	MonoClass *klass;

	klass = mono_class_get (image, token);
	if (klass)
		mono_class_init (klass);

	return (guint64) (gsize) klass;
}

static guint64
debugger_lookup_assembly (guint64 dummy_argument, const gchar *string_argument)
{
	gint64 retval;

	mono_debugger_lock ();
	retval = mono_debugger_lookup_assembly (string_argument);
	mono_debugger_unlock ();
	return retval;
}

static guint64
debugger_run_finally (guint64 context_argument, guint64 dummy)
{
	mono_debugger_run_finally (GUINT_TO_POINTER (context_argument));
	return 0;
}

static guint64
debugger_class_get_static_field_data (guint64 value)
{
	MonoClass *klass = GUINT_TO_POINTER ((gsize) value);
	MonoVTable *vtable = mono_class_vtable (mono_domain_get (), klass);
	return (guint64) (gsize) mono_vtable_get_static_field_data (vtable);
}

static void
debugger_event_handler (MonoDebuggerEvent event, guint64 data, guint64 arg)
{
	mono_debugger_notification_function (event, data, arg);
}

static guint64
debugger_get_thread_id (void)
{
	return GetCurrentThreadId ();
}

static void
debugger_attach (void)
{
	mono_debugger_init ();
	mono_debugger_create_all_threads ();

	mono_debugger_event_handler = debugger_event_handler;
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_INITIALIZE_MANAGED_CODE, 0, 0);
}

static void
debugger_thread_manager_thread_created (MonoDebuggerThread *thread)
{
	if (!thread_array)
		thread_array = g_ptr_array_new ();

	g_ptr_array_add (thread_array, thread);
}

static void
debugger_thread_manager_add_thread (gsize tid, gpointer start_stack, gpointer func)
{
	MonoDebuggerThread *thread = g_new0 (MonoDebuggerThread, 1);

	thread->tid = tid;
	thread->start_stack = start_stack;

	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_THREAD_CREATED, (guint64) (gsize) thread, tid);

	debugger_thread_manager_thread_created (thread);
}

static void
debugger_thread_manager_start_resume (gsize tid)
{
}

static void
debugger_thread_manager_end_resume (gsize tid)
{
}

extern void GC_push_all_stack (gpointer b, gpointer t);

static void
debugger_gc_init (void)
{ }

static int count = 0;

static void
debugger_gc_stop_world (void)
{
	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_ACQUIRE_GLOBAL_THREAD_LOCK, 0, 0);
}

static void
debugger_gc_start_world (void)
{
	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_RELEASE_GLOBAL_THREAD_LOCK, 0, 0);
}

static void
debugger_gc_push_all_stacks (void)
{
	long tid;
	int i;

	tid = GetCurrentThreadId ();

	if (!thread_array)
		return;

	for (i = 0; i < thread_array->len; i++) {
		MonoDebuggerThread *thread = g_ptr_array_index (thread_array, i);
		gpointer end_stack = (thread->tid == tid) ? &i : thread->end_stack;

		GC_push_all_stack (end_stack, thread->start_stack);
	}
}

static GCThreadFunctions debugger_thread_vtable = {
	debugger_gc_init,

	debugger_gc_stop_world,
	debugger_gc_push_all_stacks,
	debugger_gc_start_world
};

static MonoThreadCallbacks thread_callbacks = {
	&debugger_compile_method_cb,
	&debugger_thread_manager_add_thread,
	&debugger_thread_manager_start_resume,
	&debugger_thread_manager_end_resume
};

void
mono_debugger_init (void)
{
	mono_debugger_notification_function = notification_function;
	mono_debugger_create_notification_function (notification_function);

	/*
	 * Initialize the thread manager.
	 */

	thread_array = g_ptr_array_new ();
	mono_install_thread_callbacks (&thread_callbacks);
	gc_thread_vtable = &debugger_thread_vtable;

	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_INITIALIZE_THREAD_MANAGER,
					     GetCurrentThreadId (), 0);
}

typedef struct 
{
	MonoDomain *domain;
	const char *file;
} DebuggerThreadArgs;

typedef struct
{
	MonoDomain *domain;
	MonoMethod *method;
	int argc;
	char **argv;
} MainThreadArgs;

static guint32
main_thread_handler (gpointer user_data)
{
	MainThreadArgs *main_args = (MainThreadArgs *) user_data;
	gpointer function;
	int retval;

	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_REACHED_MAIN,
					     (guint64) (gsize) main_args->method, 0);

	retval = mono_runtime_run_main (main_args->method, main_args->argc, main_args->argv, NULL);

	/*
	 * This will never return.
	 */
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_MAIN_EXITED, 0,
					     (guint64) (gsize) retval);

	return retval;
}

int
mono_debugger_main (MonoDomain *domain, MonoAssembly *assembly, int argc, char **argv)
{
	MainThreadArgs main_args;
	MonoImage *image;

	/*
	 * Get and compile the main function.
	 */

	image = mono_assembly_get_image (assembly);
	debugger_main_method = mono_get_method (
		image, mono_image_get_entry_point (image), NULL);

	/*
	 * Reload symbol tables.
	 */
	mono_debugger_event_handler = debugger_event_handler;
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_INITIALIZE_MANAGED_CODE, 0, 0);
	mono_debugger_unlock ();

	/*
	 * Start the main thread and wait until it's ready.
	 */

	main_args.domain = domain;
	main_args.method = debugger_main_method;
	main_args.argc = argc - 2;
	main_args.argv = argv + 2;

#if RUN_IN_SUBTHREAD
	mono_thread_create (domain, main_thread_handler, &main_args);
#else
	main_thread_handler (&main_args);
#endif

	/*
	 * This will never return.
	 */
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_WRAPPER_MAIN, 0, 0);

	return 0;
}
