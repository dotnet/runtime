/**
 * \file
 * AppDomain functions
 *
 * Authors:
 *	Dietmar Maurer (dietmar@ximian.com)
 *	Patrik Torstensson
 *	Gonzalo Paniagua Javier (gonzalo@ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2012 Xamarin Inc
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
#include <glib.h>
#include <string.h>
#include <errno.h>
#include <time.h>
#include <sys/types.h>
#include <sys/stat.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#ifdef HAVE_UTIME_H
#include <utime.h>
#else
#ifdef HAVE_SYS_UTIME_H
#include <sys/utime.h>
#endif
#endif

#include <mono/metadata/gc-internals.h>
#include <mono/metadata/object.h>
#include <mono/metadata/appdomain-icalls.h>
#include <mono/metadata/class-init.h>
#include <mono/metadata/domain-internals.h>
#include "mono/metadata/metadata-internals.h"
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/exception.h>
#include <mono/metadata/exception-internals.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/mono-gc.h>
#include <mono/metadata/mono-hash-internals.h>
#include <mono/metadata/marshal.h>
#include <mono/metadata/marshal-internals.h>
#include <mono/metadata/monitor.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/w32file.h>
#include <mono/metadata/lock-tracer.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/metadata/w32socket.h>
#include <mono/utils/mono-uri.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-stdlib.h>
#include <mono/utils/mono-io-portability.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-threads.h>
#include <mono/metadata/w32handle.h>
#include <mono/metadata/w32error.h>
#include <mono/utils/w32api.h>

#ifdef ENABLE_PERFTRACING
#include <eventpipe/ds-server.h>
#endif

#ifdef HOST_WIN32
#include <direct.h>
#endif
#include "object-internals.h"
#include "icall-decl.h"

typedef struct
{
	int runtime_count;
	int assemblybinding_count;
	MonoDomain *domain;
	gchar *filename;
} RuntimeConfig;

static gboolean no_exec = FALSE;

static int n_appctx_props;
static gunichar2 **appctx_keys;
static gunichar2 **appctx_values;

static const char *
mono_check_corlib_version_internal (void);

static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyLoadContext *alc,
			      MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data,
			      MonoError *error);

static MonoAssembly *
mono_domain_assembly_search (MonoAssemblyLoadContext *alc, MonoAssembly *requesting,
			     MonoAssemblyName *aname,
			     gboolean postload,
			     gpointer user_data,
			     MonoError *error);


static void
mono_domain_fire_assembly_load (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, gpointer user_data, MonoError *error_out);

static gboolean
mono_domain_asmctx_from_path (const char *fname, MonoAssembly *requesting_assembly, gpointer user_data, MonoAssemblyContextKind *out_asmctx);

static void
add_assemblies_to_domain (MonoDomain *domain, MonoAssembly *ass, GHashTable *ht);

static void
add_assembly_to_alc (MonoAssemblyLoadContext *alc, MonoAssembly *ass);

static void
mono_context_set_default_context (MonoDomain *domain);

static MonoLoadFunc load_function = NULL;

/* Lazy class loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (assembly, "System.Reflection", "Assembly");
static GENERATE_GET_CLASS_WITH_CACHE (app_context, "System", "AppContext");

MonoClass*
mono_class_get_appdomain_class (void)
{
	return mono_defaults.object_class;
}

static MonoDomain *
mono_domain_from_appdomain_handle (MonoAppDomainHandle appdomain);

void
mono_install_runtime_load (MonoLoadFunc func)
{
	load_function = func;
}

MonoDomain*
mono_runtime_load (const char *filename, const char *runtime_version)
{
	g_assert (load_function);
	return load_function (filename, runtime_version);
}

/**
 * mono_runtime_set_no_exec:
 *
 * Instructs the runtime to operate in static mode, i.e. avoid/do not
 * allow managed code execution. This is useful for running the AOT
 * compiler on platforms which allow full-aot execution only.  This
 * should be called before mono_runtime_init ().
 */
void
mono_runtime_set_no_exec (gboolean val)
{
	no_exec = val;
}

/**
 * mono_runtime_get_no_exec:
 *
 * If true, then the runtime will not allow managed code execution.
 */
gboolean
mono_runtime_get_no_exec (void)
{
	return no_exec;
}

static void
create_domain_objects (MonoDomain *domain)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);

	MonoDomain *old_domain = mono_domain_get ();
	MonoStringHandle arg;
	MonoVTable *string_vt;
	MonoClassField *string_empty_fld;

	if (domain != old_domain) {
		mono_domain_set_internal_with_options (domain, FALSE);
	}

	/*
	 * Initialize String.Empty. This enables the removal of
	 * the static cctor of the String class.
	 */
	string_vt = mono_class_vtable_checked (mono_defaults.string_class, error);
	mono_error_assert_ok (error);
	string_empty_fld = mono_class_get_field_from_name_full (mono_defaults.string_class, "Empty", NULL);
	g_assert (string_empty_fld);
	MonoStringHandle empty_str = mono_string_new_handle ("", error);
	mono_error_assert_ok (error);
	empty_str = mono_string_intern_checked (empty_str, error);
	mono_error_assert_ok (error);
	mono_field_static_set_value_internal (string_vt, string_empty_fld, MONO_HANDLE_RAW (empty_str));
	domain->empty_string = MONO_HANDLE_RAW (empty_str);

	/*
	 * Create an instance early since we can't do it when there is no memory.
	 */
	arg = mono_string_new_handle ("Out of memory", error);
	mono_error_assert_ok (error);
	domain->out_of_memory_ex = MONO_HANDLE_RAW (mono_exception_from_name_two_strings_checked (mono_defaults.corlib, "System", "OutOfMemoryException", arg, NULL_HANDLE_STRING, error));
	mono_error_assert_ok (error);

	/* 
	 * These two are needed because the signal handlers might be executing on
	 * an alternate stack, and Boehm GC can't handle that.
	 */
	arg = mono_string_new_handle ("A null value was found where an object instance was required", error);
	mono_error_assert_ok (error);
	domain->null_reference_ex = MONO_HANDLE_RAW (mono_exception_from_name_two_strings_checked (mono_defaults.corlib, "System", "NullReferenceException", arg, NULL_HANDLE_STRING, error));
	mono_error_assert_ok (error);
	arg = mono_string_new_handle ("The requested operation caused a stack overflow.", error);
	mono_error_assert_ok (error);
	domain->stack_overflow_ex = MONO_HANDLE_RAW (mono_exception_from_name_two_strings_checked (mono_defaults.corlib, "System", "StackOverflowException", arg, NULL_HANDLE_STRING, error));
	mono_error_assert_ok (error);

	/*The ephemeron tombstone i*/
	domain->ephemeron_tombstone = MONO_HANDLE_RAW (mono_object_new_handle (mono_defaults.object_class, error));
	mono_error_assert_ok (error);

	if (domain != old_domain)
		mono_domain_set_internal_with_options (old_domain, FALSE);

	/* 
	 * This class is used during exception handling, so initialize it here, to prevent
	 * stack overflows while handling stack overflows.
	 */
	mono_class_init_internal (mono_class_create_array (mono_defaults.int_class, 1));
	HANDLE_FUNCTION_RETURN ();
}

/**
 * mono_runtime_init:
 * \param domain domain returned by \c mono_init
 *
 * Initialize the core AppDomain: this function will run also some
 * IL initialization code, so it needs the execution engine to be fully 
 * operational.
 *
 * \c AppDomain.SetupInformation is set up in \c mono_runtime_exec_main, where
 * we know the \c entry_assembly.
 *
 */
void
mono_runtime_init (MonoDomain *domain, MonoThreadStartCB start_cb, MonoThreadAttachCB attach_cb)
{
	ERROR_DECL (error);
	mono_runtime_init_checked (domain, start_cb, attach_cb, error);
	mono_error_cleanup (error);
}

void
mono_runtime_init_checked (MonoDomain *domain, MonoThreadStartCB start_cb, MonoThreadAttachCB attach_cb, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoAppDomainHandle ad;

	error_init (error);

	mono_portability_helpers_init ();
	
	mono_gc_base_init ();
	mono_monitor_init ();
	mono_marshal_init ();
	mono_gc_init_icalls ();

	// We have to append here because otherwise this will run before the netcore hook (which is installed first), see https://github.com/dotnet/runtime/issues/34273
	mono_install_assembly_preload_hook_v2 (mono_domain_assembly_preload, GUINT_TO_POINTER (FALSE), TRUE);
	mono_install_assembly_search_hook_v2 (mono_domain_assembly_search, GUINT_TO_POINTER (FALSE), FALSE, FALSE);
	mono_install_assembly_search_hook_v2 (mono_domain_assembly_postload_search, GUINT_TO_POINTER (FALSE), TRUE, FALSE);
	mono_install_assembly_load_hook_v2 (mono_domain_fire_assembly_load, NULL, FALSE);

	mono_thread_init (start_cb, attach_cb);

	if (!mono_runtime_get_no_exec ()) {
		MonoClass *klass;

		klass = mono_class_get_appdomain_class ();

		ad = MONO_HANDLE_CAST (MonoAppDomain, mono_object_new_pinned_handle (klass, error));
		goto_if_nok (error, exit);

		domain->domain = MONO_HANDLE_RAW (ad);
	}

	mono_thread_internal_attach (domain);

#if defined(ENABLE_PERFTRACING) && !defined(DISABLE_EVENTPIPE)
	ds_server_init ();
	ds_server_pause_for_diagnostics_monitor ();
#endif

	mono_type_initialization_init ();

	if (!mono_runtime_get_no_exec ())
		create_domain_objects (domain);

	/* GC init has to happen after thread init */
	mono_gc_init ();

	if (!mono_runtime_get_no_exec ())
		mono_runtime_install_appctx_properties ();

	mono_locks_tracer_init ();

	/* mscorlib is loaded before we install the load hook */
	mono_domain_fire_assembly_load (mono_domain_default_alc (domain), mono_defaults.corlib->assembly, NULL, error);
	goto_if_nok (error, exit);

exit:
	HANDLE_FUNCTION_RETURN ();
}

static void
mono_context_set_default_context (MonoDomain *domain)
{
	if (mono_runtime_get_no_exec ())
		return;

	HANDLE_FUNCTION_ENTER ();
	mono_context_set_handle (MONO_HANDLE_NEW (MonoAppContext, domain->default_context));
	HANDLE_FUNCTION_RETURN ();
}

static char*
mono_get_corlib_version (void)
{
	ERROR_DECL (error);

	MonoClass *klass;
	MonoClassField *field;

	klass = mono_class_load_from_name (mono_defaults.corlib, "System", "Environment");
	mono_class_init_internal (klass);
	field = mono_class_get_field_from_name_full (klass, "mono_corlib_version", NULL);
	if (!field)
		return NULL;

	if (! (field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_LITERAL)))
		return NULL;

	char *value;
	MonoTypeEnum field_type;
	const char *data = mono_class_get_field_default_value (field, &field_type);
	if (field_type != MONO_TYPE_STRING)
		return NULL;
	mono_metadata_read_constant_value (data, field_type, &value, error);
	mono_error_assert_ok (error);

	char *res = mono_string_from_blob (value, error);
	mono_error_assert_ok (error);

	return res;
}

/**
 * mono_check_corlib_version:
 * Checks that the corlib that is loaded matches the version of this runtime.
 * \returns NULL if the runtime will work with the corlib, or a \c g_malloc
 * allocated string with the error otherwise.
 */
const char*
mono_check_corlib_version (void)
{
	const char* res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_check_corlib_version_internal ();
	MONO_EXIT_GC_UNSAFE;
	return res;
}

static const char *
mono_check_corlib_version_internal (void)
{
#if defined(MONO_CROSS_COMPILE)
	/* Can't read the corlib version because we only have the target class layouts */
	return NULL;
#else
	char *result = NULL;
	char *version = mono_get_corlib_version ();
	if (!version) {
		result = g_strdup_printf ("expected corlib string (%s) but not found or not string", MONO_CORLIB_VERSION);
		goto exit;
	}
	if (strcmp (version, MONO_CORLIB_VERSION) != 0) {
		result = g_strdup_printf ("The runtime did not find the mscorlib.dll it expected. "
					  "Expected interface version %s but found %s. Check that "
					  "your runtime and class libraries are matching.",
					  MONO_CORLIB_VERSION, version);
		goto exit;
	}

	/* Check that the managed and unmanaged layout of MonoInternalThread matches */
	guint32 native_offset;
	guint32 managed_offset;
	native_offset = (guint32) MONO_STRUCT_OFFSET (MonoInternalThread, last);
	managed_offset = mono_field_get_offset (mono_class_get_field_from_name_full (mono_defaults.internal_thread_class, "last", NULL));
	if (native_offset != managed_offset)
		result = g_strdup_printf ("expected InternalThread.last field offset %u, found %u. See InternalThread.last comment", native_offset, managed_offset);
exit:
	g_free (version);
	return result;
#endif
}

/**
 * mono_context_init:
 * \param domain The domain where the \c System.Runtime.Remoting.Context.Context is initialized
 * Initializes the \p domain's default \c System.Runtime.Remoting 's Context.
 */
void
mono_context_init (MonoDomain *domain)
{
	ERROR_DECL (error);
	mono_context_init_checked (domain, error);
	mono_error_cleanup (error);
}

void
mono_context_init_checked (MonoDomain *domain, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	MonoClass *klass;
	MonoAppContextHandle context;

	error_init (error);
	if (mono_runtime_get_no_exec ())
		goto exit;

	klass = mono_class_load_from_name (mono_defaults.corlib, "System.Runtime.Remoting.Contexts", "Context");
	context = MONO_HANDLE_CAST (MonoAppContext, mono_object_new_pinned_handle (klass, error));
	goto_if_nok (error, exit);

	MONO_HANDLE_SETVAL (context, domain_id, gint32, domain->domain_id);
	MONO_HANDLE_SETVAL (context, context_id, gint32, 0);
	mono_threads_register_app_context (context, error);
	mono_error_assert_ok (error);
	domain->default_context = MONO_HANDLE_RAW (context);
exit:
	HANDLE_FUNCTION_RETURN ();
}

/**
 * mono_runtime_cleanup:
 * \param domain unused.
 *
 * Internal routine.
 *
 * This must not be called while there are still running threads executing
 * managed code.
 */
void
mono_runtime_cleanup (MonoDomain *domain)
{
	/* This ends up calling any pending pending (for at most 2 seconds) */
	mono_gc_cleanup ();

	mono_thread_cleanup ();
	mono_marshal_cleanup ();

	mono_type_initialization_cleanup ();

	mono_monitor_cleanup ();
}

static MonoDomainFunc quit_function = NULL;

/**
 * mono_install_runtime_cleanup:
 */
void
mono_install_runtime_cleanup (MonoDomainFunc func)
{
	quit_function = func;
}

/**
 * mono_runtime_quit:
 */
void
mono_runtime_quit (void)
{
	MONO_STACKDATA (dummy);
	(void) mono_threads_enter_gc_unsafe_region_unbalanced_internal (&dummy);
	// after quit_function (in particular, mini_cleanup) everything is
	// cleaned up so MONO_EXIT_GC_UNSAFE can't work and doesn't make sense.
	
	mono_runtime_quit_internal ();
}

/**
 * mono_runtime_quit_internal:
 */
void
mono_runtime_quit_internal (void)
{
	MONO_REQ_GC_UNSAFE_MODE;
	// but note that when we return, we're not in GC Unsafe mode anymore.
	// After clean up threads don't _have_ a thread state anymore.
	
	if (quit_function != NULL)
		quit_function (mono_get_root_domain (), NULL);
}

/**
 * mono_domain_set_config:
 * \param domain \c MonoDomain initialized with the appdomain we want to change
 * \param base_dir new base directory for the appdomain
 * \param config_file_name path to the new configuration for the app domain
 *
 * Used to set the system configuration for an appdomain
 *
 * Without using this, embedded builds will get 'System.Configuration.ConfigurationErrorsException: 
 * Error Initializing the configuration system. ---> System.ArgumentException: 
 * The 'ExeConfigFilename' argument cannot be null.' for some managed calls.
 */
void
mono_domain_set_config (MonoDomain *domain, const char *base_dir, const char *config_file_name)
{
	g_assert_not_reached ();
}

/**
 * mono_domain_has_type_resolve:
 * \param domain application domain being looked up
 *
 * \returns TRUE if the \c AppDomain.TypeResolve field has been set.
 */
gboolean
mono_domain_has_type_resolve (MonoDomain *domain)
{
	// Check whether managed code is running, and if the managed AppDomain object doesn't exist neither does the event handler
	if (!domain->domain)
		return FALSE;

	return TRUE;
}

/**
 * mono_domain_try_type_resolve:
 * \param domain application domain in which to resolve the type
 * \param name the name of the type to resolve or NULL.
 * \param typebuilder A \c System.Reflection.Emit.TypeBuilder, used if name is NULL.
 *
 * This routine invokes the internal \c System.AppDomain.DoTypeResolve and returns
 * the assembly that matches name, or ((TypeBuilder)typebuilder).FullName.
 *
 * \returns A \c MonoReflectionAssembly or NULL if not found
 */
MonoReflectionAssembly *
mono_domain_try_type_resolve (MonoDomain *domain, char *name, MonoObject *typebuilder_raw)
{
	HANDLE_FUNCTION_ENTER ();

	g_assert (domain);
	g_assert (name || typebuilder_raw);

	ERROR_DECL (error);

	MonoReflectionAssemblyHandle ret = NULL_HANDLE_INIT;

	// This will not work correctly on netcore
	if (name) {
		MonoStringHandle name_handle = mono_string_new_handle (name, error);
		goto_if_nok (error, exit);
		ret = mono_domain_try_type_resolve_name (domain, NULL, name_handle, error);
	} else {
		// TODO: make this work on netcore when working on SRE.TypeBuilder
		g_assert_not_reached ();
	}

exit:
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

MonoReflectionAssemblyHandle
mono_domain_try_type_resolve_name (MonoDomain *domain, MonoAssembly *assembly, MonoStringHandle name, MonoError *error)
{
	MonoObjectHandle ret;
	MonoReflectionAssemblyHandle assembly_handle;

	HANDLE_FUNCTION_ENTER ();

	MONO_STATIC_POINTER_INIT (MonoMethod, method)

		static gboolean inited;
		// avoid repeatedly calling mono_class_get_method_from_name_checked
		if (!inited) {
			ERROR_DECL (local_error);
			MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
			g_assert (alc_class);
			method = mono_class_get_method_from_name_checked (alc_class, "OnTypeResolve", -1, 0, local_error);
			mono_error_cleanup (local_error);
			inited = TRUE;
		}

	MONO_STATIC_POINTER_INIT_END (MonoMethod, method)

	if (!method)
		goto return_null;

	g_assert (domain);
	g_assert (MONO_HANDLE_BOOL (name));

	if (mono_runtime_get_no_exec ())
		goto return_null;

	if (assembly) {
		assembly_handle = mono_assembly_get_object_handle (assembly, error);
		goto_if_nok (error, return_null);
	}

	gpointer args [2];
	args [0] = assembly ? MONO_HANDLE_RAW (assembly_handle) : NULL;
	args [1] = MONO_HANDLE_RAW (name);
	ret = mono_runtime_try_invoke_handle (method, NULL_HANDLE, args, error);
	goto_if_nok (error, return_null);
	goto exit;

return_null:
	ret = NULL_HANDLE;

exit:
	HANDLE_FUNCTION_RETURN_REF (MonoReflectionAssembly, MONO_HANDLE_CAST (MonoReflectionAssembly, ret));
}

/**
 * mono_domain_owns_vtable_slot:
 * \returns Whether \p vtable_slot is inside a vtable which belongs to \p domain.
 */
gboolean
mono_domain_owns_vtable_slot (MonoDomain *domain, gpointer vtable_slot)
{
	gboolean res;
	MonoMemoryManager *memory_manager = mono_domain_ambient_memory_manager (domain);

	mono_mem_manager_lock (memory_manager);
	res = mono_mempool_contains_addr (memory_manager->mp, vtable_slot);
	mono_mem_manager_unlock (memory_manager);
	return res;
}

gboolean
mono_domain_set_fast (MonoDomain *domain, gboolean force)
{
	MONO_REQ_GC_UNSAFE_MODE;

	mono_domain_set_internal_with_options (domain, TRUE);
	return TRUE;
}

static gboolean
add_assembly_to_array (MonoArrayHandle dest, int dest_idx, MonoAssembly* assm, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoReflectionAssemblyHandle assm_obj = mono_assembly_get_object_handle (assm, error);
	goto_if_nok (error, leave);
	MONO_HANDLE_ARRAY_SETREF (dest, dest_idx, assm_obj);
leave:
	HANDLE_FUNCTION_RETURN_VAL (is_ok (error));
}

static MonoArrayHandle
get_assembly_array_from_domain (MonoDomain *domain, MonoError *error)
{
	int i;
	GPtrArray *assemblies;

	assemblies = mono_domain_get_assemblies (domain);

	MonoArrayHandle res = mono_array_new_handle (mono_class_get_assembly_class (), assemblies->len, error);
	goto_if_nok (error, leave);
	for (i = 0; i < assemblies->len; ++i) {
		if (!add_assembly_to_array (res, i, (MonoAssembly *)g_ptr_array_index (assemblies, i), error))
			goto leave;
	}

leave:
	g_ptr_array_free (assemblies, TRUE);
	return res;
}

MonoArrayHandle
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies (MonoError *error)
{
	MonoDomain *domain = mono_domain_get ();
	return get_assembly_array_from_domain (domain, error);
}

MonoAssembly*
mono_try_assembly_resolve (MonoAssemblyLoadContext *alc, const char *fname_raw, MonoAssembly *requesting, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	MonoAssembly *result = NULL;
	MonoStringHandle fname = mono_string_new_handle (fname_raw, error);
	goto_if_nok (error, leave);
	result = mono_try_assembly_resolve_handle (alc, fname, requesting, error);
leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

MonoAssembly*
mono_try_assembly_resolve_handle (MonoAssemblyLoadContext *alc, MonoStringHandle fname, MonoAssembly *requesting, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MonoAssembly *ret = NULL;
	char *filename = NULL;

	if (mono_runtime_get_no_exec ())
		goto leave;

	MONO_STATIC_POINTER_INIT (MonoMethod, method)

		ERROR_DECL (local_error);
		static gboolean inited;
		if (!inited) {
			MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
			g_assert (alc_class);
			method = mono_class_get_method_from_name_checked (alc_class, "OnAssemblyResolve", -1, 0, local_error);
			inited = TRUE;
		}
		mono_error_cleanup (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, method)

	if (!method) {
		ret = NULL;
		goto leave;
	}

	MonoReflectionAssemblyHandle requesting_handle;
	if (requesting) {
		requesting_handle = mono_assembly_get_object_handle (requesting, error);
		goto_if_nok (error, leave);
	}

	gpointer params [2];
	params [0] = requesting ? MONO_HANDLE_RAW (requesting_handle) : NULL;
	params [1] = MONO_HANDLE_RAW (fname);
	MonoReflectionAssemblyHandle result;
	result = MONO_HANDLE_CAST (MonoReflectionAssembly, mono_runtime_try_invoke_handle (method, NULL_HANDLE, params, error));
	goto_if_nok (error, leave);

	if (MONO_HANDLE_BOOL (result))
		ret = MONO_HANDLE_GETVAL (result, assembly);

leave:
	g_free (filename);
	HANDLE_FUNCTION_RETURN_VAL (ret);
}

MonoAssembly *
mono_domain_assembly_postload_search (MonoAssemblyLoadContext *alc, MonoAssembly *requesting,
				      MonoAssemblyName *aname,
				      gboolean postload,
				      gpointer user_data,
				      MonoError *error_out)
{
	ERROR_DECL (error);
	MonoAssembly *assembly;
	char *aname_str;

	aname_str = mono_stringify_assembly_name (aname);

	/* FIXME: We invoke managed code here, so there is a potential for deadlocks */

	assembly = mono_try_assembly_resolve (alc, aname_str, requesting, error);
	g_free (aname_str);
	mono_error_cleanup (error);

	return assembly;
}
	
/*
 * LOCKING: assumes assemblies_lock in the domain is already locked.
 */
static void
add_assemblies_to_domain (MonoDomain *domain, MonoAssembly *ass, GHashTable *ht)
{
	GSList *tmp;
	gboolean destroy_ht = FALSE;

	g_assert (ass != NULL);

	if (!ass->aname.name)
		return;

	if (!ht) {
		ht = g_hash_table_new (mono_aligned_addr_hash, NULL);
		destroy_ht = TRUE;
		for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
			g_hash_table_add (ht, tmp->data);
		}
	}

	if (!g_hash_table_lookup (ht, ass)) {
		mono_assembly_addref (ass);
		g_hash_table_add (ht, ass);
		domain->domain_assemblies = g_slist_append (domain->domain_assemblies, ass);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly %s[%p] added to domain %s, ref_count=%d", ass->aname.name, ass, domain->friendly_name, ass->ref_count);
	}

	if (destroy_ht)
		g_hash_table_destroy (ht);
}

/*
 * LOCKING: assumes the ALC's assemblies lock is taken
 */
static void
add_assembly_to_alc (MonoAssemblyLoadContext *alc, MonoAssembly *ass)
{
	GSList *tmp;

	g_assert (ass != NULL);

	if (!ass->aname.name)
		return;

	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		if (tmp->data == ass) {
			return;
		}
	}

	mono_assembly_addref (ass);
	// Prepending here will break the test suite with frequent InvalidCastExceptions, so we have to append
	alc->loaded_assemblies = g_slist_append (alc->loaded_assemblies, ass);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly %s[%p] added to ALC (%p), ref_count=%d", ass->aname.name, ass, (gpointer)alc, ass->ref_count);

}

static void
mono_domain_fire_assembly_load_event (MonoDomain *domain, MonoAssembly *assembly, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();

	g_assert (domain);
	g_assert (assembly);

	MONO_STATIC_POINTER_INIT (MonoMethod, method)

		static gboolean inited;
		if (!inited) {
			ERROR_DECL (local_error);
			MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
			g_assert (alc_class);
			method = mono_class_get_method_from_name_checked (alc_class, "OnAssemblyLoad", -1, 0, local_error);
			mono_error_cleanup (local_error);
			inited = TRUE;
		}

	MONO_STATIC_POINTER_INIT_END (MonoMethod, method)
	if (!method)
		goto exit;

	MonoReflectionAssemblyHandle assembly_handle;
	assembly_handle = mono_assembly_get_object_handle (assembly, error);
	goto_if_nok (error, exit);

	gpointer args [1];
	args [0] = MONO_HANDLE_RAW (assembly_handle);
	mono_runtime_try_invoke_handle (method, NULL_HANDLE, args, error);

exit:
	HANDLE_FUNCTION_RETURN ();
}

static void
mono_domain_fire_assembly_load (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, gpointer user_data, MonoError *error_out)
{
	ERROR_DECL (error);
	MonoDomain *domain = mono_alc_domain (alc);

	g_assert (assembly);
	g_assert (domain);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Loading assembly %s (%p) into domain %s (%p) and ALC %p", assembly->aname.name, assembly, domain->friendly_name, domain, alc);

	mono_domain_assemblies_lock (domain);
	mono_alc_assemblies_lock (alc);

	add_assemblies_to_domain (domain, assembly, NULL);
	add_assembly_to_alc (alc, assembly);

	mono_alc_assemblies_unlock (alc);
	mono_domain_assemblies_unlock (domain);

	if (!MONO_BOOL (domain->domain))
		goto leave; // This can happen during startup

	if (!mono_runtime_get_no_exec () && assembly->context.kind != MONO_ASMCTX_INTERNAL)
		mono_domain_fire_assembly_load_event (domain, assembly, error_out);

leave:
	mono_error_cleanup (error);
}

static gboolean
mono_domain_asmctx_from_path (const char *fname, MonoAssembly *requesting_assembly, gpointer user_data, MonoAssemblyContextKind *out_asmctx)
{
	MonoDomain *domain = mono_domain_get ();
	char **search_path = NULL;

        for (search_path = domain->search_path; search_path && *search_path; search_path++) {
		if (mono_path_filename_in_basedir (fname, *search_path)) {
			*out_asmctx = MONO_ASMCTX_DEFAULT;
			return TRUE;
		}
	}
	return FALSE;
}

/**
 * mono_domain_from_appdomain:
 */
MonoDomain *
mono_domain_from_appdomain (MonoAppDomain *appdomain_raw)
{
	return mono_domain_get ();
}

MonoDomain *
mono_domain_from_appdomain_handle (MonoAppDomainHandle appdomain)
{
	return mono_get_root_domain ();
}


static gboolean
try_load_from (MonoAssembly **assembly,
	       const gchar *path1, const gchar *path2,
	       const gchar *path3, const gchar *path4,
	       const MonoAssemblyOpenRequest *req)
{
	gchar *fullpath;
	gboolean found = FALSE;
	
	*assembly = NULL;
	fullpath = g_build_filename (path1, path2, path3, path4, (const char*)NULL);

	if (IS_PORTABILITY_SET) {
		gchar *new_fullpath = mono_portability_find_file (fullpath, TRUE);
		if (new_fullpath) {
			g_free (fullpath);
			fullpath = new_fullpath;
			found = TRUE;
		}
	} else
		found = g_file_test (fullpath, G_FILE_TEST_IS_REGULAR);
	
	if (found) {
		*assembly = mono_assembly_request_open (fullpath, req, NULL);
	}

	g_free (fullpath);
	return (*assembly != NULL);
}

static MonoAssembly *
real_load (gchar **search_path, const gchar *culture, const gchar *name, const MonoAssemblyOpenRequest *req)
{
	MonoAssembly *result = NULL;
	gchar **path;
	gchar *filename;
	const gchar *local_culture;
	gint len;

	if (!culture || *culture == '\0') {
		local_culture = "";
	} else {
		local_culture = culture;
	}

	filename =  g_strconcat (name, ".dll", (const char*)NULL);
	len = strlen (filename);

	for (path = search_path; *path; path++) {
		if (**path == '\0') {
			continue; /* Ignore empty ApplicationBase */
		}

		/* See test cases in bug #58992 and bug #57710 */
		/* 1st try: [culture]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, "", filename, req))
			break;

		/* 2nd try: [culture]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, "", filename, req))
			break;

		/* 3rd try: [culture]/[name]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, name, filename, req))
			break;

		/* 4th try: [culture]/[name]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, name, filename, req))
			break;
	}

	g_free (filename);
	return result;
}

static char *
get_app_context_base_directory (MonoError *error)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_basedir)

		ERROR_DECL (local_error);
		MonoClass *app_context = mono_class_get_app_context_class ();
		g_assert (app_context);
		get_basedir = mono_class_get_method_from_name_checked (app_context, "get_BaseDirectory", -1, 0, local_error);
		mono_error_assert_ok (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_basedir)

	HANDLE_FUNCTION_ENTER ();

	MonoStringHandle result = MONO_HANDLE_CAST (MonoString, mono_runtime_try_invoke_handle (get_basedir, NULL_HANDLE, NULL, error));
	char *base_dir = mono_string_handle_to_utf8 (result, error);

	HANDLE_FUNCTION_RETURN_VAL (base_dir);
}

/*
 * Try loading the assembly from ApplicationBase and PrivateBinPath 
 * and then from assemblies_path if any.
 * LOCKING: This is called from the assembly loading code, which means the caller
 * might hold the loader lock. Thus, this function must not acquire the domain lock.
 */
static MonoAssembly *
mono_domain_assembly_preload (MonoAssemblyLoadContext *alc,
			      MonoAssemblyName *aname,
			      gchar **assemblies_path,
			      gpointer user_data,
			      MonoError *error)
{
	MonoDomain *domain = mono_alc_domain (alc);
	MonoAssembly *result = NULL;

	g_assert (alc);
	g_assert (domain == mono_domain_get ());

	MonoAssemblyCandidatePredicate predicate = NULL;
	void* predicate_ud = NULL;
	if (mono_loader_get_strict_assembly_name_check ()) {
		predicate = &mono_assembly_candidate_predicate_sn_same_name;
		predicate_ud = aname;
	}
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, MONO_ASMCTX_DEFAULT, alc);
	req.request.predicate = predicate;
	req.request.predicate_ud = predicate_ud;

	if (!mono_runtime_get_no_exec ()) {
		char *search_path [2];
		search_path [1] = NULL;

		char *base_dir = get_app_context_base_directory (error);
		search_path [0] = base_dir;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Domain (%p) ApplicationBase is %s", domain, base_dir);

		result = real_load (search_path, aname->culture, aname->name, &req);

		g_free (base_dir);
	}

	if (result == NULL && assemblies_path && assemblies_path [0] != NULL) {
		result = real_load (assemblies_path, aname->culture, aname->name, &req);
	}

	return result;
}

/*
 * Check whenever a given assembly was already loaded in the current appdomain.
 */
static MonoAssembly *
mono_domain_assembly_search (MonoAssemblyLoadContext *alc, MonoAssembly *requesting,
			     MonoAssemblyName *aname,
			     gboolean postload,
			     gpointer user_data,
			     MonoError *error)
{
	g_assert (aname != NULL);
	GSList *tmp;
	MonoAssembly *ass;

	const MonoAssemblyNameEqFlags eq_flags = MONO_ANAME_EQ_IGNORE_PUBKEY | MONO_ANAME_EQ_IGNORE_VERSION | MONO_ANAME_EQ_IGNORE_CASE;

	mono_alc_assemblies_lock (alc);
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		ass = (MonoAssembly *)tmp->data;
		g_assert (ass != NULL);
		// FIXME: Can dynamic assemblies match here for netcore?
		if (assembly_is_dynamic (ass) || !mono_assembly_names_equal_flags (aname, &ass->aname, eq_flags))
			continue;

		mono_alc_assemblies_unlock (alc);
		return ass;
	}
	mono_alc_assemblies_unlock (alc);

	return NULL;
}

MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_InternalLoad (MonoStringHandle name_handle, MonoStackCrawlMark *stack_mark, gpointer load_Context, MonoError *error)
{
	error_init (error);
	MonoAssembly *ass = NULL;
	MonoAssemblyName aname;
	MonoAssemblyByNameRequest req;
	MonoAssemblyContextKind asmctx;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	gboolean parsed;
	char *name;

	MonoAssembly *requesting_assembly = mono_runtime_get_caller_from_stack_mark (stack_mark);
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)load_Context;

	if (!alc)
		alc = mono_assembly_get_alc (requesting_assembly);
	if (!alc)
		g_assert_not_reached ();
	
	g_assert (alc);
	asmctx = MONO_ASMCTX_DEFAULT;
	mono_assembly_request_prepare_byname (&req, asmctx, alc);
	req.basedir = NULL;
	/* Everything currently goes through this function, and the postload hook (aka the AppDomain.AssemblyResolve event)
	 * is triggered under some scenarios. It's not completely obvious to me in what situations (if any) this should be disabled,
	 * other than for corlib satellite assemblies (which I've dealt with further down the call stack).
	 */
	//req.no_postload_search = TRUE;
	req.requesting_assembly = requesting_assembly;

	name = mono_string_handle_to_utf8 (name_handle, error);
	goto_if_nok (error, fail);
	parsed = mono_assembly_name_parse (name, &aname);
	g_free (name);
	if (!parsed)
		goto fail;

	MonoAssemblyCandidatePredicate predicate;
	void* predicate_ud;
	predicate = NULL;
	predicate_ud = NULL;
	if (mono_loader_get_strict_assembly_name_check ()) {
		predicate = &mono_assembly_candidate_predicate_sn_same_name;
		predicate_ud = &aname;
	}
	req.request.predicate = predicate;
	req.request.predicate_ud = predicate_ud;

	ass = mono_assembly_request_byname (&aname, &req, &status);
	if (!ass)
		goto fail;

	MonoReflectionAssemblyHandle refass;
	refass = mono_assembly_get_object_handle (ass, error);
	goto_if_nok (error, fail);
	return refass;

fail:
	return MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
}

static
MonoAssembly *
mono_alc_load_file (MonoAssemblyLoadContext *alc, MonoStringHandle fname, MonoAssembly *executing_assembly, MonoAssemblyContextKind asmctx, MonoError *error)
{
	MonoAssembly *ass = NULL;
	HANDLE_FUNCTION_ENTER ();
	char *filename = NULL;
	if (MONO_HANDLE_IS_NULL (fname)) {
		mono_error_set_argument_null (error, "assemblyFile", "");
		goto leave;
	}

	filename = mono_string_handle_to_utf8 (fname, error);
	goto_if_nok (error, leave);

	if (!g_path_is_absolute (filename)) {
		mono_error_set_argument (error, "assemblyFile", "Absolute path information is required.");
		goto leave;
	}

	MonoImageOpenStatus status;
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, asmctx, alc);
	req.requesting_assembly = executing_assembly;
	ass = mono_assembly_request_open (filename, &req, &status);
	if (!ass) {
		if (status == MONO_IMAGE_IMAGE_INVALID)
			mono_error_set_bad_image_by_name (error, filename, "Invalid Image: %s", filename);
		else
			mono_error_set_simple_file_not_found (error, filename);
	}

leave:
	g_free (filename);
	HANDLE_FUNCTION_RETURN_VAL (ass);
}

MonoReflectionAssemblyHandle
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFile (gpointer alc_ptr, MonoStringHandle fname, MonoStackCrawlMark *stack_mark, MonoError *error)
{
	MonoReflectionAssemblyHandle result = MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)alc_ptr;

	MonoAssembly *executing_assembly;
	executing_assembly = mono_runtime_get_caller_from_stack_mark (stack_mark);
	MonoAssembly *ass = mono_alc_load_file (alc, fname, executing_assembly, mono_alc_is_default (alc) ? MONO_ASMCTX_LOADFROM : MONO_ASMCTX_INDIVIDUAL, error);
	goto_if_nok (error, leave);

	result = mono_assembly_get_object_handle (ass, error);

leave:
	return result;
}

static MonoAssembly*
mono_alc_load_raw_bytes (MonoAssemblyLoadContext *alc, guint8 *raw_assembly, guint32 raw_assembly_len, guint8 *raw_symbol_data, guint32 raw_symbol_len, MonoError *error);

MonoReflectionAssemblyHandle
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalLoadFromStream (gpointer native_alc, gpointer raw_assembly_ptr, gint32 raw_assembly_len, gpointer raw_symbols_ptr, gint32 raw_symbols_len, MonoError *error)
{
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)native_alc;
	MonoReflectionAssemblyHandle result = MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
	MonoAssembly *assm = NULL;
	assm = mono_alc_load_raw_bytes (alc, (guint8 *)raw_assembly_ptr, raw_assembly_len, (guint8 *)raw_symbols_ptr, raw_symbols_len, error);
	goto_if_nok (error, leave);

	result = mono_assembly_get_object_handle (assm, error);

leave:
	return result;
}

static MonoAssembly*
mono_alc_load_raw_bytes (MonoAssemblyLoadContext *alc, guint8 *assembly_data, guint32 raw_assembly_len, guint8 *raw_symbol_data, guint32 raw_symbol_len, MonoError *error)
{
	MonoAssembly *ass = NULL;
	MonoImageOpenStatus status;
	MonoImage *image = mono_image_open_from_data_internal (alc, (char*)assembly_data, raw_assembly_len, TRUE, NULL, FALSE, NULL, NULL);

	if (!image) {
		mono_error_set_bad_image_by_name (error, "In memory assembly", "0x%p", assembly_data);
		return ass;
	}

	if (raw_symbol_data)
		mono_debug_open_image_from_memory (image, raw_symbol_data, raw_symbol_len);

	MonoAssemblyLoadRequest req;
	mono_assembly_request_prepare_load (&req, MONO_ASMCTX_INDIVIDUAL, alc);
	ass = mono_assembly_request_load_from (image, "", &req, &status);

	if (!ass) {
		mono_image_close (image);
		mono_error_set_bad_image_by_name (error, "In Memory assembly", "0x%p", assembly_data);
		return ass;
	}

	/* Clear the reference added by mono_image_open_from_data_internal above */
	mono_image_close (image);

	return ass;
}

/**
 * mono_domain_is_unloading:
 */
gboolean
mono_domain_is_unloading (MonoDomain *domain)
{
	return FALSE;
}

/* Remember properties so they can be be installed in AppContext during runtime init */
void
mono_runtime_register_appctx_properties (int nprops, const char **keys,  const char **values)
{
	n_appctx_props = nprops;
	appctx_keys = g_new0 (gunichar2*, nprops);
	appctx_values = g_new0 (gunichar2*, nprops);

	for (int i = 0; i < nprops; ++i) {
		appctx_keys [i] = g_utf8_to_utf16 (keys [i], strlen (keys [i]), NULL, NULL, NULL);
		appctx_values [i] = g_utf8_to_utf16 (values [i], strlen (values [i]), NULL, NULL, NULL);
	}
}

static GENERATE_GET_CLASS_WITH_CACHE (appctx, "System", "AppContext")

/* Install properties into AppContext */
void
mono_runtime_install_appctx_properties (void)
{
	ERROR_DECL (error);
	gpointer args [3];

	MonoMethod *setup = mono_class_get_method_from_name_checked (mono_class_get_appctx_class (), "Setup", 3, 0, error);
	g_assert (setup);

	// FIXME: TRUSTED_PLATFORM_ASSEMBLIES is very large

	/* internal static unsafe void Setup(char** pNames, char** pValues, int count) */
	args [0] = appctx_keys;
	args [1] = appctx_values;
	args [2] = &n_appctx_props;

	mono_runtime_invoke_checked (setup, NULL, args, error);
	mono_error_assert_ok (error);

	/* No longer needed */
	for (int i = 0; i < n_appctx_props; ++i) {
		g_free (appctx_keys [i]);
		g_free (appctx_values [i]);
	}
	g_free (appctx_keys);
	g_free (appctx_values);
	appctx_keys = NULL;
	appctx_values = NULL;
}
