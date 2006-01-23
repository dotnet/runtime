#if defined(__i386__) || defined(__x86_64__)
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>
#define _IN_THE_MONO_DEBUGGER
#include <mono/metadata/mono-debug-debugger.h>
#include <libgc/include/libgc-mono-debugger.h>
#include "mini.h"
#include <unistd.h>
#include <locale.h>
#include <string.h>

#define IO_LAYER(func) (* mono_debugger_io_layer.func)

static GPtrArray *thread_array = NULL;

static gpointer main_started_cond;
static gpointer main_ready_cond;

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

void (*mono_debugger_notification_function) (guint64 command, guint64 data, guint64 data2);

/*
 * This is a global data symbol which is read by the debugger.
 */
MonoDebuggerInfo MONO_DEBUGGER__debugger_info = {
	MONO_DEBUGGER_MAGIC,
	MONO_DEBUGGER_VERSION,
	sizeof (MonoDebuggerInfo),
	sizeof (MonoSymbolTable),
	0,
	mono_trampoline_code,
	&mono_symbol_table,
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
	&debugger_run_finally
};

MonoDebuggerManager MONO_DEBUGGER__manager = {
	sizeof (MonoDebuggerManager),
	sizeof (MonoDebuggerThread),
	NULL, NULL, NULL, 0
};

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

	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_METHOD_COMPILED, GPOINTER_TO_UINT (retval), 0);

	return retval;
}

static guint64
debugger_compile_method (guint64 method_arg)
{
	MonoMethod *method = (MonoMethod *) GUINT_TO_POINTER ((gssize) method_arg);

	return GPOINTER_TO_UINT (debugger_compile_method_cb (method));
}

static guint64
debugger_get_virtual_method (guint64 object_arg, guint64 method_arg)
{
	MonoObject *object = (MonoObject *) GUINT_TO_POINTER ((gssize) object_arg);
	MonoMethod *method = (MonoMethod *) GUINT_TO_POINTER ((gssize) method_arg);

	if (mono_class_is_valuetype (mono_method_get_class (method)))
		return method_arg;

	return GPOINTER_TO_UINT (mono_object_get_virtual_method (object, method));
}

static guint64
debugger_get_boxed_object (guint64 klass_arg, guint64 val_arg)
{
	static MonoObject *last_boxed_object = NULL;
	MonoClass *klass = (MonoClass *) GUINT_TO_POINTER ((gssize) klass_arg);
	gpointer val = (gpointer) GUINT_TO_POINTER ((gssize) val_arg);
	MonoObject *boxed;

	if (!mono_class_is_valuetype (klass))
		return val_arg;

	boxed = mono_value_box (mono_domain_get (), klass, val);
	last_boxed_object = boxed; // Protect the object from being garbage collected

	return GPOINTER_TO_UINT (boxed);
}

static guint64
debugger_create_string (guint64 dummy_argument, const gchar *string_argument)
{
	return GPOINTER_TO_UINT (mono_string_new_wrapper (string_argument));
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
	MonoImage *image = (MonoImage *) GUINT_TO_POINTER ((gssize) image_argument);
	guint32 token = (guint32) token_argument;
	MonoClass *klass;

	klass = mono_class_get (image, token);
	if (klass)
		mono_class_init (klass);

	return GPOINTER_TO_UINT (klass);
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
	MonoClass *klass = GUINT_TO_POINTER ((gssize) value);
	MonoVTable *vtable = mono_class_vtable (mono_domain_get (), klass);
	return GPOINTER_TO_UINT (mono_vtable_get_static_field_data (vtable));
}

static void
debugger_event_handler (MonoDebuggerEvent event, guint64 data, guint64 arg)
{
	mono_debugger_notification_function (event, data, arg);
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
	thread->func = func;
	thread->start_stack = start_stack;

	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_THREAD_CREATED, GPOINTER_TO_UINT (thread), tid);

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

static void
debugger_thread_manager_acquire_global_thread_lock (void)
{
	int tid = IO_LAYER (GetCurrentThreadId) ();

	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_ACQUIRE_GLOBAL_THREAD_LOCK, 0, tid);
}

static void
debugger_thread_manager_release_global_thread_lock (void)
{
	int tid = IO_LAYER (GetCurrentThreadId) ();

	mono_debugger_notification_function (
		MONO_DEBUGGER_EVENT_RELEASE_GLOBAL_THREAD_LOCK, 0, tid);
}

extern void GC_push_all_stack (gpointer b, gpointer t);

static void
debugger_gc_stop_world (void)
{
	debugger_thread_manager_acquire_global_thread_lock ();
}

static void
debugger_gc_start_world (void)
{
	debugger_thread_manager_release_global_thread_lock ();
}

static void
debugger_gc_push_all_stacks (void)
{
	int i, tid;

	tid = IO_LAYER (GetCurrentThreadId) ();

	if (!thread_array)
		return;

	for (i = 0; i < thread_array->len; i++) {
		MonoDebuggerThread *thread = g_ptr_array_index (thread_array, i);
		gpointer end_stack = (thread->tid == tid) ? &i : thread->end_stack;

		GC_push_all_stack (end_stack, thread->start_stack);
	}
}

static GCThreadFunctions debugger_thread_vtable = {
	NULL,

	debugger_gc_stop_world,
	debugger_gc_push_all_stacks,
	debugger_gc_start_world
};

static void
debugger_thread_manager_init (void)
{
	if (!thread_array)
		thread_array = g_ptr_array_new ();

	gc_thread_vtable = &debugger_thread_vtable;
}

static MonoThreadCallbacks thread_callbacks = {
	&debugger_compile_method_cb,
	&debugger_thread_manager_add_thread,
	&debugger_thread_manager_start_resume,
	&debugger_thread_manager_end_resume
};

void
mono_debugger_init (void)
{
	main_started_cond = IO_LAYER (CreateSemaphore) (NULL, 0, 1, NULL);
	main_ready_cond = IO_LAYER (CreateSemaphore) (NULL, 0, 1, NULL);

	mono_debugger_notification_function = mono_debugger_create_notification_function
		(&MONO_DEBUGGER__manager.notification_address);
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
	int retval;

	MONO_DEBUGGER__manager.main_tid = IO_LAYER (GetCurrentThreadId) ();
	MONO_DEBUGGER__manager.main_thread = g_new0 (MonoDebuggerThread, 1);
	MONO_DEBUGGER__manager.main_thread->tid = IO_LAYER (GetCurrentThreadId) ();
	MONO_DEBUGGER__manager.main_thread->start_stack = &main_args;

	debugger_thread_manager_thread_created (MONO_DEBUGGER__manager.main_thread);

	IO_LAYER (ReleaseSemaphore) (main_started_cond, 1, NULL);

	/*
	 * Wait until everything is ready.
	 */
	IO_LAYER (WaitForSingleObject) (main_ready_cond, INFINITE, FALSE);

	retval = mono_runtime_run_main (main_args->method, main_args->argc, main_args->argv, NULL);
	/*
	 * This will never return.
	 */
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_MAIN_EXITED, 0, GPOINTER_TO_UINT (retval));

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
	MONO_DEBUGGER__manager.main_function = mono_compile_method (debugger_main_method);

	/*
	 * Start the main thread and wait until it's ready.
	 */

	main_args.domain = domain;
	main_args.method = debugger_main_method;
	main_args.argc = argc - 2;
	main_args.argv = argv + 2;

	mono_thread_create (domain, main_thread_handler, &main_args);
	IO_LAYER (WaitForSingleObject) (main_started_cond, INFINITE, FALSE);

	/*
	 * Initialize the thread manager.
	 */

	mono_debugger_event_handler = debugger_event_handler;
	mono_install_thread_callbacks (&thread_callbacks);
	debugger_thread_manager_init ();

	/*
	 * Reload symbol tables.
	 */
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_INITIALIZE_MANAGED_CODE, 0, 0);
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_INITIALIZE_THREAD_MANAGER, 0, 0);

	mono_debugger_unlock ();

	/*
	 * Signal the main thread that it can execute the managed Main().
	 */
	IO_LAYER (ReleaseSemaphore) (main_ready_cond, 1, NULL);

	/*
	 * This will never return.
	 */
	mono_debugger_notification_function (MONO_DEBUGGER_EVENT_WRAPPER_MAIN, 0, 0);

	return 0;
}

#else /* defined(__x86__) || defined(__x86_64__) */
/*
 * We're on an unsupported platform for the debugger.
 */
#include "mini.h"

void
mono_debugger_init (void)
{
	/*
	 * This method is only called when we're running inside the Mono Debugger, but
	 * since the debugger doesn't work on this platform, this line should never be reached.
	 */
	g_error ("The Mono Debugger is not supported on this platform.");
}

int
mono_debugger_main (MonoDomain *domain, MonoAssembly *assembly, int argc, char **argv)
{
	g_assert_not_reached ();
	return 0;
}

#endif
