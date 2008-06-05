#include <config.h>
#include <mono/io-layer/io-layer.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/mono-config.h>
#define _IN_THE_MONO_DEBUGGER
#include "debug-debugger.h"
#include "debug-mini.h"
#include <libgc/include/libgc-mono-debugger.h>
#include "mini.h"
#include <unistd.h>
#include <locale.h>
#include <string.h>

/*
 * This file is only compiled on platforms where the debugger is supported - see the conditional
 * definition of `debugger_sources' in Makefile.am.
 *
 * configure.in checks whether we're using the included libgc and disables the debugger if not.
 */

#if !defined(MONO_DEBUGGER_SUPPORTED)
#error "Some clown tried to compile debug-debugger.c on an unsupported platform - fix Makefile.am!"
#elif !defined(USE_INCLUDED_LIBGC)
#error "Some clown #defined MONO_DEBUGGER_SUPPORTED without USE_INCLUDED_GC - fix configure.in!"
#endif

static guint64 debugger_compile_method (guint64 method_arg);
static guint64 debugger_get_virtual_method (guint64 class_arg, guint64 method_arg);
static guint64 debugger_get_boxed_object (guint64 klass_arg, guint64 val_arg);
static guint64 debugger_class_get_static_field_data (guint64 klass);

static guint64 debugger_run_finally (guint64 argument1, guint64 argument2);
static void debugger_initialize (void);
static guint64 debugger_init_code_buffer (void);

static void debugger_event_handler (MonoDebuggerEvent event, guint64 data, guint64 arg);

static guint64 debugger_create_string (G_GNUC_UNUSED guint64 dummy, G_GNUC_UNUSED guint64 dummy2,
				       G_GNUC_UNUSED guint64 dummy3, const gchar *string_argument);
static gint64 debugger_lookup_class (guint64 image_argument, G_GNUC_UNUSED guint64 dummy,
				     G_GNUC_UNUSED guint64 dummy2, gchar *full_name);
static guint64 debugger_insert_method_breakpoint (guint64 method_argument, guint64 index);
static guint64 debugger_insert_source_breakpoint (guint64 image_argument, guint64 token,
						  guint64 index, const gchar *class_name);
static void debugger_remove_breakpoint (guint64 index, G_GNUC_UNUSED guint64 dummy);
static guint64 debugger_register_class_init_callback (guint64 image_argument, guint64 token,
						      guint64 index, const gchar *class_name);
static void debugger_remove_class_init_callback (guint64 index, G_GNUC_UNUSED guint64 dummy);
static guint64 debugger_get_method_signature (guint64 argument1, G_GNUC_UNUSED guint64 argument2);

#define EXECUTABLE_CODE_BUFFER_SIZE 4096
static guint8 *debugger_executable_code_buffer = NULL;

static GCThreadFunctions debugger_thread_vtable;

static MonoDebuggerMetadataInfo debugger_metadata_info = {
	sizeof (MonoDebuggerMetadataInfo),
	sizeof (MonoDefaults),
	&mono_defaults,
	sizeof (MonoType),
	sizeof (MonoArrayType),
	sizeof (MonoClass),
	sizeof (MonoThread),
	G_STRUCT_OFFSET (MonoThread, tid),
	G_STRUCT_OFFSET (MonoThread, stack_ptr),
	G_STRUCT_OFFSET (MonoThread, end_stack),
	G_STRUCT_OFFSET (MonoClass, image),
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
	G_STRUCT_OFFSET (MonoClass, vtable),
	sizeof (MonoClassField),
	G_STRUCT_OFFSET (MonoClassField, type),
	G_STRUCT_OFFSET (MonoClassField, offset),
	G_STRUCT_OFFSET (MonoDefaults, corlib),
	G_STRUCT_OFFSET (MonoDefaults, object_class),
	G_STRUCT_OFFSET (MonoDefaults, byte_class),
	G_STRUCT_OFFSET (MonoDefaults, void_class),
	G_STRUCT_OFFSET (MonoDefaults, boolean_class),
	G_STRUCT_OFFSET (MonoDefaults, sbyte_class),
	G_STRUCT_OFFSET (MonoDefaults, int16_class),
	G_STRUCT_OFFSET (MonoDefaults, uint16_class),
	G_STRUCT_OFFSET (MonoDefaults, int32_class),
	G_STRUCT_OFFSET (MonoDefaults, uint32_class),
	G_STRUCT_OFFSET (MonoDefaults, int_class),
	G_STRUCT_OFFSET (MonoDefaults, uint_class),
	G_STRUCT_OFFSET (MonoDefaults, int64_class),
	G_STRUCT_OFFSET (MonoDefaults, uint64_class),
	G_STRUCT_OFFSET (MonoDefaults, single_class),
	G_STRUCT_OFFSET (MonoDefaults, double_class),
	G_STRUCT_OFFSET (MonoDefaults, char_class),
	G_STRUCT_OFFSET (MonoDefaults, string_class),
	G_STRUCT_OFFSET (MonoDefaults, enum_class),
	G_STRUCT_OFFSET (MonoDefaults, array_class),
	G_STRUCT_OFFSET (MonoDefaults, delegate_class),
	G_STRUCT_OFFSET (MonoDefaults, exception_class),
	G_STRUCT_OFFSET (MonoMethod, klass),
	G_STRUCT_OFFSET (MonoMethod, token),
	G_STRUCT_OFFSET (MonoMethod, name) + sizeof (void *),
	G_STRUCT_OFFSET (MonoMethodInflated, declaring),
	G_STRUCT_OFFSET (MonoVTable, klass),
	G_STRUCT_OFFSET (MonoVTable, vtable)
};

extern void MONO_DEBUGGER__notification_function (guint64 command, guint64 data, guint64 data2);

/*
 * This is a global data symbol which is read by the debugger.
 */
MonoDebuggerInfo MONO_DEBUGGER__debugger_info = {
	MONO_DEBUGGER_MAGIC,
	MONO_DEBUGGER_VERSION,
	sizeof (MonoDebuggerInfo),
	sizeof (MonoSymbolTable),
	MONO_TRAMPOLINE_NUM,
	mono_trampoline_code,
	&MONO_DEBUGGER__notification_function,
	&mono_symbol_table,
	&debugger_metadata_info,
	&mono_debug_debugger_version,

	&debugger_compile_method,
	&debugger_get_virtual_method,
	&debugger_get_boxed_object,
	&mono_debugger_runtime_invoke,
	&debugger_class_get_static_field_data,
	&debugger_run_finally,
	&debugger_initialize,

	&debugger_create_string,
	&debugger_lookup_class,

	&debugger_insert_method_breakpoint,
	&debugger_insert_source_breakpoint,
	&debugger_remove_breakpoint,

	&debugger_register_class_init_callback,
	&debugger_remove_class_init_callback,

	&mono_debugger_thread_table,

	&debugger_executable_code_buffer,
	mono_breakpoint_info,
	mono_breakpoint_info_index,

	EXECUTABLE_CODE_BUFFER_SIZE,
	MONO_BREAKPOINT_ARRAY_SIZE,

	debugger_get_method_signature,
	debugger_init_code_buffer,

	&gc_thread_vtable,
	&debugger_thread_vtable,

	&mono_debugger_event_handler,
	debugger_event_handler,

	&_mono_debug_using_mono_debugger
};

static guint64
debugger_compile_method (guint64 method_arg)
{
	MonoMethod *method = (MonoMethod *) GUINT_TO_POINTER ((gsize) method_arg);
	gpointer addr;

	mono_debugger_lock ();
	addr = mono_compile_method (method);
	mono_debugger_unlock ();

	return (guint64) (gsize) addr;
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
debugger_create_string (G_GNUC_UNUSED guint64 dummy, G_GNUC_UNUSED guint64 dummy2,
			G_GNUC_UNUSED guint64 dummy3, const gchar *string_argument)
{
	return (guint64) (gsize) mono_string_new_wrapper (string_argument);
}

static gint64
debugger_lookup_class (guint64 image_argument, G_GNUC_UNUSED guint64 dummy,
		       G_GNUC_UNUSED guint64 dummy2, gchar *full_name)
{
	MonoImage *image = (MonoImage *) GUINT_TO_POINTER ((gsize) image_argument);
	gchar *name_space, *name, *pos;
	MonoClass *klass;

	pos = strrchr (full_name, '.');
	if (pos) {
		name_space = full_name;
		*pos = 0;
		name = pos + 1;
	} else {
		name = full_name;
		name_space = NULL;
	}

	klass = mono_class_from_name (image, name_space ? name_space : "", name);
	if (!klass)
		return -1;

	mono_class_init (klass);
	mono_class_setup_methods (klass);
	return (gint64) (gssize) klass;
}

static guint64
debugger_run_finally (guint64 context_argument, G_GNUC_UNUSED guint64 dummy)
{
	mono_debugger_run_finally (GUINT_TO_POINTER ((gsize)context_argument));
	return 0;
}

static guint64
debugger_class_get_static_field_data (guint64 value)
{
	MonoClass *klass = GUINT_TO_POINTER ((gsize) value);
	MonoVTable *vtable = mono_class_vtable (mono_domain_get (), klass);
	return (guint64) (gsize) mono_vtable_get_static_field_data (vtable);
}

static guint64
debugger_insert_method_breakpoint (guint64 method_argument, guint64 index)
{
	MonoMethod *method = GUINT_TO_POINTER ((gsize) method_argument);
	MonoDebugMethodAddressList *info;

	mono_debugger_lock ();

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		const char *name = method->name;
		MonoMethod *nm = NULL;

		if (method->klass->parent == mono_defaults.multicastdelegate_class) {
			if (*name == 'I' && (strcmp (name, "Invoke") == 0))
			        nm = mono_marshal_get_delegate_invoke (method, NULL);
			else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0))
				nm = mono_marshal_get_delegate_begin_invoke (method);
			else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0))
				nm = mono_marshal_get_delegate_end_invoke (method);
		}

		if (!nm) {
			mono_debugger_unlock ();
			return 0;
		}

		method = nm;
	}

	info = mono_debugger_insert_method_breakpoint (method, index);

	mono_debugger_unlock ();
	return (guint64) (gsize) info;
}

static guint64
debugger_insert_source_breakpoint (guint64 image_argument, guint64 token, guint64 index,
				   const gchar *class_name)
{
	MonoImage *image = GUINT_TO_POINTER ((gsize) image_argument);
	MonoDebugMethodAddressList *info;
	MonoClass *klass;
	int i;

	mono_debugger_lock ();

	klass = mono_debugger_register_class_init_callback (image, class_name, token, index);
	if (!klass) {
		mono_debugger_unlock ();
		return 0;
	}

	for (i = 0; i < klass->method.count; i++) {
		MonoMethod *method = klass->methods [i];

		if (method->token != token)
			continue;

		if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
			const char *name = method->name;
			MonoMethod *nm = NULL;

			if (method->klass->parent == mono_defaults.multicastdelegate_class) {
				if (*name == 'I' && (strcmp (name, "Invoke") == 0))
					nm = mono_marshal_get_delegate_invoke (method, NULL);
				else if (*name == 'B' && (strcmp (name, "BeginInvoke") == 0))
					nm = mono_marshal_get_delegate_begin_invoke (method);
				else if (*name == 'E' && (strcmp (name, "EndInvoke") == 0))
					nm = mono_marshal_get_delegate_end_invoke (method);
			}

			if (!nm) {
				mono_debugger_unlock ();
				return 0;
			}

			method = nm;
		}

		info = mono_debugger_insert_method_breakpoint (method, index);
		mono_debugger_unlock ();
		return (guint64) (gsize) info;
	}

	mono_debugger_unlock ();
	return 0;
}

static void
debugger_remove_breakpoint (guint64 index, G_GNUC_UNUSED guint64 dummy)
{
	mono_debugger_lock ();
	mono_debugger_remove_method_breakpoint (index);
	mono_debugger_unlock ();
}

static guint64
debugger_register_class_init_callback (guint64 image_argument, guint64 token, guint64 index,
				       const gchar *class_name)
{
	MonoImage *image = GUINT_TO_POINTER ((gsize) image_argument);
	MonoClass *klass;

	mono_debugger_lock ();
	klass = mono_debugger_register_class_init_callback (image, class_name, token, index);
	mono_debugger_unlock ();
	return (guint64) (gsize) klass;
}

static void
debugger_remove_class_init_callback (guint64 index, G_GNUC_UNUSED guint64 dummy)
{
	mono_debugger_lock ();
	mono_debugger_remove_class_init_callback (index);
	mono_debugger_unlock ();
}

static guint64
debugger_get_method_signature (guint64 method_arg, G_GNUC_UNUSED guint64 dummy)
{
	MonoMethod *method = (MonoMethod *) GUINT_TO_POINTER ((gsize) method_arg);
	MonoMethodSignature *sig;

	sig = mono_method_signature (method);
	return (guint64) (gsize) sig;
}

static void
debugger_event_handler (MonoDebuggerEvent event, guint64 data, guint64 arg)
{
	MONO_DEBUGGER__notification_function (event, data, arg);
}

static void
debugger_gc_thread_created (pthread_t thread, void *stack_ptr)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_GC_THREAD_CREATED,
			     (guint64) (gsize) stack_ptr, thread);
}

static void
debugger_gc_thread_exited (pthread_t thread, void *stack_ptr)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_GC_THREAD_EXITED,
			     (guint64) (gsize) stack_ptr, thread);
}

static void
debugger_gc_stop_world (void)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_ACQUIRE_GLOBAL_THREAD_LOCK, 0, 0);
}

static void
debugger_gc_start_world (void)
{
	mono_debugger_event (MONO_DEBUGGER_EVENT_RELEASE_GLOBAL_THREAD_LOCK, 0, 0);
}

static GCThreadFunctions debugger_thread_vtable = {
	NULL,

	debugger_gc_thread_created,
	debugger_gc_thread_exited,

	debugger_gc_stop_world,
	debugger_gc_start_world
};

static void
debugger_init_threads (void)
{
	gc_thread_vtable = &debugger_thread_vtable;
}

static void
debugger_finalize_threads (void)
{
	gc_thread_vtable = NULL;
}

static guint64
debugger_init_code_buffer (void)
{
	if (!debugger_executable_code_buffer)
		debugger_executable_code_buffer = mono_global_codeman_reserve (EXECUTABLE_CODE_BUFFER_SIZE);
	return (guint64) (gsize) debugger_executable_code_buffer;
}

extern MonoDebuggerInfo *MONO_DEBUGGER__debugger_info_ptr;

static void
debugger_initialize (void)
{
}

void
mono_debugger_init (void)
{
	debugger_executable_code_buffer = mono_global_codeman_reserve (EXECUTABLE_CODE_BUFFER_SIZE);
	mono_debugger_event_handler = debugger_event_handler;

	/*
	 * Use an indirect call so gcc can't optimize it away.
	 */
	MONO_DEBUGGER__debugger_info.initialize ();

	debugger_init_threads ();

	/*
	 * Initialize the thread manager.
	 *
	 * NOTE: We only reference the `MONO_DEBUGGER__debugger_info_ptr' here to prevent the
	 * linker from removing the .mdb_debug_info section.
	 */

	mono_debugger_event (MONO_DEBUGGER_EVENT_INITIALIZE_THREAD_MANAGER,
			     (guint64) (gssize) MONO_DEBUGGER__debugger_info_ptr, 0);
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

	return mono_runtime_run_main (main_args->method, main_args->argc, main_args->argv, NULL);
}

int
mono_debugger_main (MonoDomain *domain, MonoAssembly *assembly, int argc, char **argv)
{
	MainThreadArgs main_args;
	MonoImage *image;
	MonoMethod *main_method;

	/*
	 * Get and compile the main function.
	 */

	image = mono_assembly_get_image (assembly);
	main_method = mono_get_method (image, mono_image_get_entry_point (image), NULL);

	/*
	 * Initialize managed code.
	 */
	mono_debugger_event (MONO_DEBUGGER_EVENT_INITIALIZE_MANAGED_CODE,
			     (guint64) (gssize) main_method, 0);

	/*
	 * Start the main thread and wait until it's ready.
	 */

	main_args.domain = domain;
	main_args.method = main_method;
	main_args.argc = argc;
	main_args.argv = argv;

#if RUN_IN_SUBTHREAD
	mono_thread_create (domain, main_thread_handler, &main_args);
#else
	main_thread_handler (&main_args);
#endif

	mono_thread_manage ();

	/*
	 * This will never return.
	 */
	mono_debugger_event (MONO_DEBUGGER_EVENT_WRAPPER_MAIN, 0, 0);

	return 0;
}
