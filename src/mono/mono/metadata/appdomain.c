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
#include <mono/metadata/lock-tracer.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/reflection-internals.h>
#include <mono/metadata/abi-details.h>
#include <mono/utils/mono-uri.h>
#include <mono/utils/mono-logger-internals.h>
#include <mono/utils/mono-path.h>
#include <mono/utils/mono-stdlib.h>
#include <mono/utils/mono-error-internals.h>
#include <mono/utils/atomic.h>
#include <mono/utils/mono-memory-model.h>
#include <mono/utils/mono-threads.h>
#include <mono/utils/w32api.h>
#include <mono/metadata/components.h>

#ifdef HOST_WIN32
#include <direct.h>
#endif
#include "object-internals.h"
#include "icall-decl.h"

static gboolean no_exec = FALSE;

static int n_appctx_props;
static char **appctx_keys;
static char **appctx_values;

static MonovmRuntimeConfigArguments *runtime_config_arg;
static MonovmRuntimeConfigArgumentsCleanup runtime_config_cleanup_fn;
static gpointer runtime_config_user_data;

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

static const char *
runtimeconfig_json_get_buffer (MonovmRuntimeConfigArguments *arg, MonoFileMap **file_map, gpointer *buf_handle);

static void
runtimeconfig_json_read_props (const char *ptr, const char **endp, int nprops, gunichar2 **dest_keys, gunichar2 **dest_values);

static MonoLoadFunc load_function = NULL;

/* Lazy class loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (app_context, "System", "AppContext");

MonoClass*
mono_class_get_appdomain_class (void)
{
	return mono_defaults.object_class;
}

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

	MonoStringHandle arg;
	MonoVTable *string_vt;
	MonoClassField *string_empty_fld;

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

	/* The ephemeron tombstone */
	domain->ephemeron_tombstone = MONO_HANDLE_RAW (mono_object_new_handle (mono_defaults.object_class, error));
	mono_error_assert_ok (error);

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

	mono_component_diagnostics_server ()->init ();

	mono_component_event_pipe ()->add_rundown_execution_checkpoint ("RuntimeSuspend");

	mono_component_diagnostics_server ()->pause_for_diagnostics_monitor ();

	mono_component_event_pipe ()->add_rundown_execution_checkpoint ("RuntimeResumed");

	mono_component_event_pipe ()->write_event_ee_startup_start ();

	mono_type_initialization_init ();

	if (!mono_runtime_get_no_exec ())
		create_domain_objects (domain);

	/* GC init has to happen after thread init */
	mono_gc_init ();

	if (!mono_runtime_get_no_exec ())
		mono_runtime_install_appctx_properties ();

	mono_locks_tracer_init ();

	/* mscorlib is loaded before we install the load hook */
	mono_domain_fire_assembly_load (mono_alc_get_default (), mono_defaults.corlib->assembly, NULL, error);
	goto_if_nok (error, exit);

exit:
	HANDLE_FUNCTION_RETURN ();
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

	/* Check that the managed and unmanaged layout of MonoInternalThread matches */
	guint32 native_offset;
	guint32 managed_offset;
	native_offset = (guint32) MONO_STRUCT_OFFSET (MonoInternalThread, last);
	managed_offset = mono_field_get_offset (mono_class_get_field_from_name_full (mono_defaults.internal_thread_class, "last", NULL));
	if (native_offset != managed_offset)
		result = g_strdup_printf ("expected InternalThread.last field offset %u, found %u. See InternalThread.last comment", native_offset, managed_offset);
	return result;
#endif
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

MonoReflectionAssemblyHandle
mono_domain_try_type_resolve_name (MonoAssembly *assembly, MonoStringHandle name, MonoError *error)
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

	g_assert (MONO_HANDLE_BOOL (name));

	if (mono_runtime_get_no_exec ())
		goto return_null;

	if (assembly) {
		assembly_handle = mono_assembly_get_object_handle (assembly, error);
		goto_if_nok (error, return_null);
	} else {
		assembly_handle = MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
	}

	gpointer args [2];
	args [0] = MONO_HANDLE_RAW (assembly_handle);
	args [1] = MONO_HANDLE_RAW (name);
	ret = mono_runtime_try_invoke_handle (method, NULL_HANDLE, args, error);
	goto_if_nok (error, return_null);
	goto exit;

return_null:
	ret = NULL_HANDLE;

exit:
	HANDLE_FUNCTION_RETURN_REF (MonoReflectionAssembly, MONO_HANDLE_CAST (MonoReflectionAssembly, ret));
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
	} else {
		requesting_handle = MONO_HANDLE_CAST (MonoReflectionAssembly, NULL_HANDLE);
	}

	gpointer params [2];
	params [0] = MONO_HANDLE_RAW (requesting_handle);
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
	MonoDomain *domain = mono_get_root_domain ();

	g_assert (assembly);
	g_assert (domain);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Loading assembly %s (%p) into domain %s (%p) and ALC %p", assembly->aname.name, assembly, domain->friendly_name, domain, alc);

	mono_alc_add_assembly (alc, assembly);

	if (!MONO_BOOL (domain->domain))
		goto leave; // This can happen during startup

	if (!mono_runtime_get_no_exec () && !assembly->context.no_managed_load_event)
		mono_domain_fire_assembly_load_event (domain, assembly, error_out);

leave:
	mono_error_cleanup (error);
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
	size_t len;

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
	MonoAssembly *result = NULL;

	g_assert (alc);

	MonoAssemblyCandidatePredicate predicate = NULL;
	void* predicate_ud = NULL;
	if (mono_loader_get_strict_assembly_name_check ()) {
		predicate = &mono_assembly_candidate_predicate_sn_same_name;
		predicate_ud = aname;
	}
	MonoAssemblyOpenRequest req;
	mono_assembly_request_prepare_open (&req, alc);
	req.request.predicate = predicate;
	req.request.predicate_ud = predicate_ud;

	if (!mono_runtime_get_no_exec ()) {
		char *search_path [2];
		search_path [1] = NULL;

		char *base_dir = get_app_context_base_directory (error);
		search_path [0] = base_dir;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "ApplicationBase is %s", base_dir);

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

	return mono_alc_find_assembly (alc, aname);
}

MonoReflectionAssemblyHandle
ves_icall_System_Reflection_Assembly_InternalLoad (MonoStringHandle name_handle, MonoStackCrawlMark *stack_mark, gpointer load_Context, MonoError *error)
{
	error_init (error);
	MonoAssembly *ass = NULL;
	MonoAssemblyName aname;
	MonoAssemblyByNameRequest req;
	MonoImageOpenStatus status = MONO_IMAGE_OK;
	gboolean parsed;
	char *name;

	MonoAssembly *requesting_assembly = mono_runtime_get_caller_from_stack_mark (stack_mark);
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)load_Context;

#if HOST_WASI
	// On WASI, mono_assembly_get_alc isn't yet supported. However it should be possible to make it work.
	if (!alc)
		alc = mono_alc_get_default ();
#endif

	if (!alc)
		alc = mono_assembly_get_alc (requesting_assembly);
	if (!alc)
		g_assert_not_reached ();

	g_assert (alc);
	mono_assembly_request_prepare_byname (&req, alc);
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

/* Remember properties so they can be be installed in AppContext during runtime init */
void
mono_runtime_register_appctx_properties (int nprops, const char **keys,  const char **values)
{
	n_appctx_props = nprops;
	appctx_keys = g_new0 (char *, n_appctx_props);
	appctx_values = g_new0 (char *, n_appctx_props);

	for (int i = 0; i < nprops; ++i) {
		appctx_keys [i] = g_strdup (keys [i]);
		appctx_values [i] = g_strdup (values [i]);
	}
}

void
mono_runtime_register_runtimeconfig_json_properties (MonovmRuntimeConfigArguments *arg, MonovmRuntimeConfigArgumentsCleanup cleanup_fn, void *user_data)
{
	runtime_config_arg = arg;
	runtime_config_cleanup_fn = cleanup_fn;
	runtime_config_user_data = user_data;
}

static GENERATE_GET_CLASS_WITH_CACHE (appctx, "System", "AppContext")

/* Install properties into AppContext */
void
mono_runtime_install_appctx_properties (void)
{
	ERROR_DECL (error);
	gpointer args [3];
	int n_runtimeconfig_json_props = 0;
	int n_combined_props;
	gunichar2 **combined_keys;
	gunichar2 **combined_values;
	MonoFileMap *runtimeconfig_json_map = NULL;
	gpointer runtimeconfig_json_map_handle = NULL;
	const char *buffer_start = runtimeconfig_json_get_buffer (runtime_config_arg, &runtimeconfig_json_map, &runtimeconfig_json_map_handle);
	const char *buffer = buffer_start;

	MonoMethod *setup = mono_class_get_method_from_name_checked (mono_class_get_appctx_class (), "Setup", 3, 0, error);
	g_assert (setup);

	// FIXME: TRUSTED_PLATFORM_ASSEMBLIES is very large

	// Combine and convert properties
	if (buffer)
		n_runtimeconfig_json_props = mono_metadata_decode_value (buffer, &buffer);

	n_combined_props = n_appctx_props + n_runtimeconfig_json_props;
	combined_keys = g_new0 (gunichar2 *, n_combined_props);
	combined_values = g_new0 (gunichar2 *, n_combined_props);

	for (int i = 0; i < n_appctx_props; ++i) {
		combined_keys [i] = g_utf8_to_utf16 (appctx_keys [i], -1, NULL, NULL, NULL);
		combined_values [i] = g_utf8_to_utf16 (appctx_values [i], -1, NULL, NULL, NULL);
	}

	runtimeconfig_json_read_props (buffer, &buffer, n_runtimeconfig_json_props, combined_keys + n_appctx_props, combined_values + n_appctx_props);

	/* internal static unsafe void Setup(char** pNames, char** pValues, int count) */
	args [0] = combined_keys;
	args [1] = combined_values;
	args [2] = &n_combined_props;

	mono_runtime_invoke_checked (setup, NULL, args, error);
	mono_error_assert_ok (error);

	if (runtimeconfig_json_map != NULL) {
		mono_file_unmap ((gpointer)buffer_start, runtimeconfig_json_map_handle);
		mono_file_map_close (runtimeconfig_json_map);
	}

	// Call user defined cleanup function
	if (runtime_config_cleanup_fn)
		(*runtime_config_cleanup_fn) (runtime_config_arg, runtime_config_user_data);

	/* No longer needed */
	for (int i = 0; i < n_combined_props; ++i) {
		g_free (combined_keys [i]);
		g_free (combined_values [i]);
	}
	g_free (combined_keys);
	g_free (combined_values);
	for (int i = 0; i < n_appctx_props; ++i) {
		g_free (appctx_keys [i]);
		g_free (appctx_values [i]);
	}
	g_free (appctx_keys);
	g_free (appctx_values);

	appctx_keys = NULL;
	appctx_values = NULL;
	if (runtime_config_arg) {
		runtime_config_arg = NULL;
		runtime_config_cleanup_fn = NULL;
		runtime_config_user_data = NULL;
	}
}

static const char *
runtimeconfig_json_get_buffer (MonovmRuntimeConfigArguments *arg, MonoFileMap **file_map, gpointer *buf_handle)
{
	if (arg != NULL) {
		switch (arg->kind) {
		case 0: {
			char *buffer = NULL;
			guint64 file_len = 0;

			*file_map = mono_file_map_open (arg->runtimeconfig.name.path);
			g_assert (*file_map);
			file_len = mono_file_map_size (*file_map);
			g_assert (file_len > 0);
			buffer = (char *)mono_file_map (file_len, MONO_MMAP_READ|MONO_MMAP_PRIVATE, mono_file_map_fd (*file_map), 0, buf_handle);
			g_assert (buffer);
			return buffer;
		}
		case 1: {
			*file_map = NULL;
			*buf_handle = NULL;
			return arg->runtimeconfig.data.data;
		}
		default:
			g_assert_not_reached ();
		}
	}

	*file_map = NULL;
	*buf_handle = NULL;
	return NULL;
}

static void
runtimeconfig_json_read_props (const char *ptr, const char **endp, int nprops, gunichar2 **dest_keys, gunichar2 **dest_values)
{
	for (int i = 0; i < nprops; ++i) {
		int str_len;

		str_len = mono_metadata_decode_value (ptr, &ptr);
		dest_keys [i] = g_utf8_to_utf16 (ptr, str_len, NULL, NULL, NULL);
		ptr += str_len;

		str_len = mono_metadata_decode_value (ptr, &ptr);
		dest_values [i] = g_utf8_to_utf16 (ptr, str_len, NULL, NULL, NULL);
		ptr += str_len;
	}

	*endp = ptr;
}

void
mono_security_enable_core_clr ()
{
	// no-op
}

void
mono_security_set_core_clr_platform_callback (MonoCoreClrPlatformCB callback)
{
	// no-op
}
