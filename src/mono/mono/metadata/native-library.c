#include "config.h"
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loader.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/utils/checked-build.h"
#include "mono/utils/mono-compiler.h"
#include "mono/utils/mono-logger-internals.h"
#include "mono/utils/mono-path.h"
#include "mono/metadata/native-library.h"
#include "mono/metadata/custom-attrs-internals.h"

static int pinvoke_search_directories_count;
static char **pinvoke_search_directories;

// sync with src/libraries/System.Private.CoreLib/src/System/Runtime/InteropServices/DllImportSearchPath.cs
typedef enum
{
	DLLIMPORTSEARCHPATH_LEGACY_BEHAVIOR = 0x0, // when no other flags are present, search the application directory and then call LoadLibraryEx with LOAD_WITH_ALTERED_SEARCH_PATH
	DLLIMPORTSEARCHPATH_USE_DLL_DIRECTORY_FOR_DEPENDENCIES = 0x100,
	DLLIMPORTSEARCHPATH_APPLICATION_DIRECTORY = 0x200,
	DLLIMPORTSEARCHPATH_USER_DIRECTORIES = 0x400,
	DLLIMPORTSEARCHPATH_SYSTEM32 = 0x800,
	DLLIMPORTSEARCHPATH_SAFE_DIRECTORIES = 0x1000,
	DLLIMPORTSEARCHPATH_ASSEMBLY_DIRECTORY = 0x2, // search the assembly directory first regardless of platform, not passed on to LoadLibraryEx
} DllImportSearchPath;
#ifdef HOST_WIN32
static const int DLLIMPORTSEARCHPATH_LOADLIBRARY_FLAG_MASK = DLLIMPORTSEARCHPATH_USE_DLL_DIRECTORY_FOR_DEPENDENCIES | DLLIMPORTSEARCHPATH_APPLICATION_DIRECTORY |
                                                             DLLIMPORTSEARCHPATH_USER_DIRECTORIES | DLLIMPORTSEARCHPATH_SYSTEM32 | DLLIMPORTSEARCHPATH_SAFE_DIRECTORIES;
#endif

// This lock may be taken within an ALC lock, and should never be the other way around.
static MonoCoopMutex native_library_module_lock;
static GHashTable *native_library_module_map;
/*
 * This blocklist is used as a set for cache invalidation purposes with netcore pinvokes.
 * When pinvokes are resolved with anything other than the last-chance managed event,
 * the results of that lookup are added to an ALC-level cache. However, if a library is then
 * unloaded with NativeLibrary.Free(), this cache should be invalidated so that a newly called
 * pinvoke will not attempt to use it, hence the blocklist. This design means that if another
 * library is loaded at the same address, it will function with a perf hit, as the entry will
 * repeatedly be added and removed from the cache due to its presence in the blocklist.
 * This is a rare scenario and considered a worthwhile tradeoff.
 */
static GHashTable *native_library_module_blocklist;

static GHashTable *global_module_map; // should only be accessed with the global loader data lock

static MonoDl *internal_module; // used when pinvoking `__Internal`

static PInvokeOverrideFn pinvoke_override;

/* Class lazy loading functions */
GENERATE_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")
GENERATE_TRY_GET_CLASS_WITH_CACHE (appdomain_unloaded_exception, "System", "AppDomainUnloadedException")
GENERATE_GET_CLASS_WITH_CACHE (native_library, "System.Runtime.InteropServices", "NativeLibrary");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (dllimportsearchpath_attribute, "System.Runtime.InteropServices", "DefaultDllImportSearchPathsAttribute");

void
mono_dllmap_insert (MonoImage *assembly, const char *dll, const char *func, const char *tdll, const char *tfunc)
{
	g_assert_not_reached ();
}

void
mono_loader_register_module (const char *name, MonoDl *module)
{
	mono_loader_init ();

	// No transition here because this is early in startup
	mono_global_loader_data_lock ();

	g_hash_table_insert (global_module_map, g_strdup (name), module);

	mono_global_loader_data_unlock ();
}

static MonoDl *
mono_loader_register_module_locking (const char *name, MonoDl *module)
{
	MonoDl *result = NULL;

	MONO_ENTER_GC_SAFE;
	mono_global_loader_data_lock ();
	MONO_EXIT_GC_SAFE;

	result = (MonoDl *)g_hash_table_lookup (global_module_map, name);
	if (result) {
		g_free (module->full_name);
		g_free (module);
		goto exit;
	}

	g_hash_table_insert (global_module_map, g_strdup (name), module);
	result = module;

exit:
	MONO_ENTER_GC_SAFE;
	mono_global_loader_data_unlock ();
	MONO_EXIT_GC_SAFE;

	return result;
}

static void
remove_cached_module (gpointer key, gpointer value, gpointer user_data)
{
	ERROR_DECL (close_error);
	mono_dl_close((MonoDl*)value, close_error);
	mono_error_cleanup (close_error);
}

void
mono_global_loader_cache_init (void)
{
	if (!global_module_map)
		global_module_map = g_hash_table_new (g_str_hash, g_str_equal);

	if (!native_library_module_map)
		native_library_module_map = g_hash_table_new (g_direct_hash, g_direct_equal);
	if (!native_library_module_blocklist)
		native_library_module_blocklist = g_hash_table_new (g_direct_hash, g_direct_equal);
	mono_coop_mutex_init (&native_library_module_lock);
}

static gboolean
is_absolute_path (const char *path)
{
	// FIXME: other platforms have similar prefixes, such as $ORIGIN
#ifdef HOST_DARWIN
	if (!strncmp (path, "@executable_path/", 17) || !strncmp (path, "@loader_path/", 13) || !strncmp (path, "@rpath/", 7))
		return TRUE;
#endif
	return g_path_is_absolute (path);
}

static gpointer
lookup_pinvoke_call_impl (MonoMethod *method, MonoLookupPInvokeStatus *status_out);

static gpointer
pinvoke_probe_for_symbol (MonoDl *module, MonoMethodPInvoke *piinfo, const char *import);

static void
pinvoke_probe_convert_status_for_api (MonoLookupPInvokeStatus *status, const char **exc_class, const char **exc_arg)
{
	if (!exc_class)
		return;
	switch (status->err_code) {
	case LOOKUP_PINVOKE_ERR_OK:
		*exc_class = NULL;
		*exc_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_LIB:
		*exc_class = "DllNotFoundException";
		*exc_arg = status->err_arg;
		status->err_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_SYM:
		*exc_class = "EntryPointNotFoundException";
		*exc_arg = status->err_arg;
		status->err_arg = NULL;
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
pinvoke_probe_convert_status_to_error (MonoLookupPInvokeStatus *status, MonoError *error)
{
	/* Note: this has to return a MONO_ERROR_GENERIC because mono_mb_emit_exception_for_error only knows how to decode generic errors. */
	switch (status->err_code) {
	case LOOKUP_PINVOKE_ERR_OK:
		return;
	case LOOKUP_PINVOKE_ERR_NO_LIB:
		mono_error_set_generic_error (error, "System", "DllNotFoundException", "%s", status->err_arg);
		g_free (status->err_arg);
		status->err_arg = NULL;
		break;
	case LOOKUP_PINVOKE_ERR_NO_SYM:
		mono_error_set_generic_error (error, "System", "EntryPointNotFoundException", "%s", status->err_arg);
		g_free (status->err_arg);
		status->err_arg = NULL;
		break;
	default:
		g_assert_not_reached ();
	}
}

/**
 * mono_lookup_pinvoke_call:
 */
gpointer
mono_lookup_pinvoke_call (MonoMethod *method, const char **exc_class, const char **exc_arg)
{
	gpointer result;
	MONO_ENTER_GC_UNSAFE;
	MonoLookupPInvokeStatus status;
	memset (&status, 0, sizeof (status));
	result = lookup_pinvoke_call_impl (method, &status);
	pinvoke_probe_convert_status_for_api (&status, exc_class, exc_arg);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

gpointer
mono_lookup_pinvoke_call_internal (MonoMethod *method, MonoError *error)
{
	gpointer result;
	MonoLookupPInvokeStatus status;
	memset (&status, 0, sizeof (status));
	result = lookup_pinvoke_call_impl (method, &status);
	if (status.err_code)
		pinvoke_probe_convert_status_to_error (&status, error);
	return result;
}

void
mono_set_pinvoke_search_directories (int dir_count, char **dirs)
{
	pinvoke_search_directories_count = dir_count;
	g_strfreev (pinvoke_search_directories);
	pinvoke_search_directories = dirs;
}

static void
native_library_lock (void)
{
	mono_coop_mutex_lock (&native_library_module_lock);
}

static void
native_library_unlock (void)
{
	mono_coop_mutex_unlock (&native_library_module_lock);
}

static void
alc_pinvoke_lock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_lock (&alc->pinvoke_lock);
}

static void
alc_pinvoke_unlock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_unlock (&alc->pinvoke_lock);
}

// LOCKING: expects you to hold native_library_module_lock
static MonoDl *
netcore_handle_lookup (gpointer handle)
{
	return (MonoDl *)g_hash_table_lookup (native_library_module_map, handle);
}

// LOCKING: expects you to hold native_library_module_lock
static gboolean
netcore_check_blocklist (MonoDl *module)
{
	return g_hash_table_contains (native_library_module_blocklist, module);
}

static int
convert_dllimport_flags (int flags)
{
#ifdef HOST_WIN32
	return flags & DLLIMPORTSEARCHPATH_LOADLIBRARY_FLAG_MASK;
#else
	// DllImportSearchPath is Windows-only, other than DLLIMPORTSEARCHPATH_ASSEMBLY_DIRECTORY
	return 0;
#endif
}

static MonoDl *
netcore_probe_for_module_variations (const char *mdirname, const char *file_name, int raw_flags, MonoError *error)
{
	void *iter = NULL;
	char *full_name = NULL;
	MonoDl *module = NULL;

	ERROR_DECL (bad_image_error);

	while (module == NULL && (full_name = mono_dl_build_path (mdirname, file_name, &iter))) {
		mono_error_cleanup (error);
		error_init_reuse (error);
		module = mono_dl_open_full (full_name, MONO_DL_LAZY, raw_flags, error);
		if (!module)
			mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "DllImport error loading library '%s': '%s'.", full_name, mono_error_get_message_without_fields (error));
		g_free (full_name);
		if (!module && !is_ok (error) && mono_error_get_error_code (error) == MONO_ERROR_BAD_IMAGE) {
			mono_error_cleanup (bad_image_error);
			mono_error_move (bad_image_error, error);
		}
	}

	if (!module && !is_ok (bad_image_error)) {
		mono_error_cleanup (error);
		mono_error_move (error, bad_image_error);
	}

	mono_error_cleanup (bad_image_error);

	return module;
}

static MonoDl *
netcore_probe_for_module (MonoImage *image, const char *file_name, int flags, MonoError *error)
{
	MonoDl *module = NULL;
	int lflags = convert_dllimport_flags (flags);

	// TODO: this algorithm doesn't quite match CoreCLR, so respecting DLLIMPORTSEARCHPATH_LEGACY_BEHAVIOR makes little sense
	// If the difference becomes a problem, overhaul this algorithm to match theirs exactly

	ERROR_DECL (bad_image_error);
	gboolean probe_first_without_prepend = FALSE;

#if defined(HOST_ANDROID)
	// On Android, try without any path additions first. It is sensitive to probing that will always miss
    // and lookup for some libraries is required to use a relative path
	probe_first_without_prepend = TRUE;
#else
	if (file_name != NULL && g_path_is_absolute (file_name))
		probe_first_without_prepend = TRUE;
#endif

	if (module == NULL && probe_first_without_prepend) {
		module = netcore_probe_for_module_variations (NULL, file_name, lflags, error);
		if (!module && !is_ok (error) && mono_error_get_error_code (error) == MONO_ERROR_BAD_IMAGE)
			mono_error_move (bad_image_error, error);
	}

	// Check the NATIVE_DLL_SEARCH_DIRECTORIES
	for (int i = 0; i < pinvoke_search_directories_count && module == NULL; ++i) {
		mono_error_cleanup (error);
		error_init_reuse (error);
		module = netcore_probe_for_module_variations (pinvoke_search_directories[i], file_name, lflags, error);
		if (!module && !is_ok (error) && mono_error_get_error_code (error) == MONO_ERROR_BAD_IMAGE) {
			mono_error_cleanup (bad_image_error);
			mono_error_move (bad_image_error, error);
		}
	}

	// Check the assembly directory if the search flag is set and the image exists
	if ((flags & DLLIMPORTSEARCHPATH_ASSEMBLY_DIRECTORY) != 0 && image != NULL &&
		module == NULL && (image->filename != NULL)) {
		mono_error_cleanup (error);
		error_init_reuse (error);
		char *mdirname = g_path_get_dirname (image->filename);
		if (mdirname)
			module = netcore_probe_for_module_variations (mdirname, file_name, lflags, error);
		g_free (mdirname);
	}

	// Try without any path additions, if we didn't try it already
	if (module == NULL && !probe_first_without_prepend)
	{
		module = netcore_probe_for_module_variations (NULL, file_name, lflags, error);
		if (!module && !is_ok (error) && mono_error_get_error_code (error) == MONO_ERROR_BAD_IMAGE)
			mono_error_move (bad_image_error, error);
	}

	// TODO: Pass remaining flags on to LoadLibraryEx on Windows where appropriate, see https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.dllimportsearchpath?view=netcore-3.1

	if (!module && !is_ok (bad_image_error)) {
		mono_error_cleanup (error);
		mono_error_move (error, bad_image_error);
	}

	mono_error_cleanup (bad_image_error);

	return module;
}

static MonoDl *
netcore_probe_for_module_nofail (MonoImage *image, const char *file_name, int flags)
{
	MonoDl *result = NULL;

	ERROR_DECL (error);
	result = netcore_probe_for_module (image, file_name, flags, error);
	mono_error_cleanup (error);

	return result;
}

static MonoDl*
netcore_lookup_self_native_handle (void)
{
	ERROR_DECL (load_error);
	if (!internal_module)
		internal_module = mono_dl_open_self (load_error);

	if (!internal_module)
		mono_trace (G_LOG_LEVEL_INFO, MONO_TRACE_DLLIMPORT, "DllImport error loading library '__Internal': '%s'.", mono_error_get_message_without_fields (load_error));

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found via __Internal.");
	mono_error_cleanup (load_error);

	return internal_module;
}

static MonoDl* native_handle_lookup_wrapper (gpointer handle)
{
	MonoDl *result = NULL;

	if (!internal_module)
		netcore_lookup_self_native_handle ();

	if (internal_module->handle == handle) {
		result = internal_module;
	} else {
		native_library_lock ();
		result = netcore_handle_lookup (handle);
		native_library_unlock ();
	}

	return result;
}

static MonoDl *
netcore_resolve_with_dll_import_resolver (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, const char *scope, guint32 flags, MonoError *error)
{
	MonoDl *result = NULL;
	gpointer lib = NULL;

	MONO_STATIC_POINTER_INIT (MonoMethod, resolve)

		ERROR_DECL (local_error);
		static gboolean inited;
		if (!inited) {
			MonoClass *native_lib_class = mono_class_get_native_library_class ();
			g_assert (native_lib_class);
			resolve = mono_class_get_method_from_name_checked (native_lib_class, "MonoLoadLibraryCallbackStub", -1, 0, local_error);
			inited = TRUE;
		}
		mono_error_cleanup (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)

	if (!resolve)
		return NULL;

	if (mono_runtime_get_no_exec ())
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	MonoStringHandle scope_handle;
	scope_handle = mono_string_new_handle (scope, error);
	goto_if_nok (error, leave);

	MonoReflectionAssemblyHandle assembly_handle;
	assembly_handle = mono_assembly_get_object_handle (assembly, error);
	goto_if_nok (error, leave);

	gboolean has_search_flags;
	has_search_flags = flags != 0 ? TRUE : FALSE;
	gpointer args [5];
	args [0] = MONO_HANDLE_RAW (scope_handle);
	args [1] = MONO_HANDLE_RAW (assembly_handle);
	args [2] = &has_search_flags;
	args [3] = &flags;
	args [4] = &lib;
	mono_runtime_invoke_checked (resolve, NULL, args, error);
	goto_if_nok (error, leave);

	result = native_handle_lookup_wrapper (lib);

leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoDl *
netcore_resolve_with_dll_import_resolver_nofail (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, const char *scope, guint32 flags)
{
	MonoDl *result = NULL;
	ERROR_DECL (error);

	result = netcore_resolve_with_dll_import_resolver (alc, assembly, scope, flags, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Error while invoking ALC DllImportResolver(\"%s\") delegate: '%s'", scope, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

static MonoDl *
netcore_resolve_with_load (MonoAssemblyLoadContext *alc, const char *scope, MonoError *error)
{
	MonoDl *result = NULL;
	gpointer lib = NULL;

	MONO_STATIC_POINTER_INIT (MonoMethod, resolve)

		ERROR_DECL (local_error);
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		resolve = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUnmanagedDll", -1, 0, local_error);
		mono_error_assert_ok (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)
	g_assert (resolve);

	if (mono_runtime_get_no_exec ())
		return NULL;

	/* default ALC LoadUnmanagedDll always returns null */
	/* NOTE: This is more than an optimization.  It allows us to avoid triggering creation of
	 * the managed object for the Default ALC while we're looking up icalls for Monitor.  The
	 * AssemblyLoadContext base constructor uses a lock on the allContexts variable which leads
	 * to recursive static constructor invocation which may show unexpected side effects ---
	 * such as a null AssemblyLoadContext.Default --- early in the runtime.
	 */
	if (mono_alc_is_default (alc))
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	MonoStringHandle scope_handle;
	scope_handle = mono_string_new_handle (scope, error);
	goto_if_nok (error, leave);

	gpointer gchandle = mono_alc_get_gchandle_for_resolving (alc);
	gpointer args [3];
	args [0] = MONO_HANDLE_RAW (scope_handle);
	args [1] = &gchandle;
	args [2] = &lib;
	mono_runtime_invoke_checked (resolve, NULL, args, error);
	goto_if_nok (error, leave);

	result = native_handle_lookup_wrapper (lib);

leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoDl *
netcore_resolve_with_load_nofail (MonoAssemblyLoadContext *alc, const char *scope)
{
	MonoDl *result = NULL;
	ERROR_DECL (error);

	result = netcore_resolve_with_load (alc, scope, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Error while invoking ALC LoadUnmanagedDll(\"%s\") method: '%s'", scope, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

static MonoDl *
netcore_resolve_with_resolving_event (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, const char *scope, MonoError *error)
{
	MonoDl *result = NULL;
	gpointer lib = NULL;

	MONO_STATIC_POINTER_INIT (MonoMethod, resolve)

		ERROR_DECL (local_error);
		static gboolean inited;
		if (!inited) {
			MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
			g_assert (alc_class);
			resolve = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUnmanagedDllUsingEvent", -1, 0, local_error);
			inited = TRUE;
		}
		mono_error_cleanup (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)
	if (!resolve)
		return NULL;

	if (mono_runtime_get_no_exec ())
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	MonoStringHandle scope_handle;
	scope_handle = mono_string_new_handle (scope, error);
	goto_if_nok (error, leave);

	MonoReflectionAssemblyHandle assembly_handle;
	assembly_handle = mono_assembly_get_object_handle (assembly, error);
	goto_if_nok (error, leave);

	gpointer gchandle = mono_alc_get_gchandle_for_resolving (alc);
	gpointer args [4];
	args [0] = MONO_HANDLE_RAW (scope_handle);
	args [1] = MONO_HANDLE_RAW (assembly_handle);
	args [2] = &gchandle;
	args [3] = &lib;
	mono_runtime_invoke_checked (resolve, NULL, args, error);
	goto_if_nok (error, leave);

	result = native_handle_lookup_wrapper (lib);

leave:
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoDl *
netcore_resolve_with_resolving_event_nofail (MonoAssemblyLoadContext *alc, MonoAssembly *assembly, const char *scope)
{
	MonoDl *result = NULL;
	ERROR_DECL (error);

	result = netcore_resolve_with_resolving_event (alc, assembly, scope, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Error while invoking ALC ResolvingUnmangedDll(\"%s\") event: '%s'", scope, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

// LOCKING: expects you to hold the ALC's pinvoke lock
static MonoDl *
netcore_check_alc_cache (MonoAssemblyLoadContext *alc, const char *scope)
{
	MonoDl *result = NULL;

	result = (MonoDl *)g_hash_table_lookup (alc->pinvoke_scopes, scope);

	if (result) {
		gboolean blocklisted;

		native_library_lock ();
		blocklisted = netcore_check_blocklist (result);
		native_library_unlock ();

		if (blocklisted) {
			g_hash_table_remove (alc->pinvoke_scopes, scope);
			result = NULL;
		}
	}

	return result;
}

static MonoDl *
netcore_lookup_native_library (MonoAssemblyLoadContext *alc, MonoImage *image, const char *scope, guint32 flags)
{
	MonoDl *module = NULL;
	MonoDl *cached;
	MonoAssembly *assembly = mono_image_get_assembly (image);

	MONO_REQ_GC_UNSAFE_MODE;

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "DllImport attempting to load: '%s'.", scope);

	// We allow a special name to dlopen from the running process namespace, which is not present in CoreCLR
	if (strcmp (scope, "__Internal") == 0) {
		return netcore_lookup_self_native_handle();
	}

	/*
	 * Try these until one of them succeeds:
	 *
	 * 1. Check the cache in the active ALC.
	 *
	 * 2. Call the DllImportResolver on the active assembly.
	 *
	 * 3. Call LoadUnmanagedDll on the active ALC.
	 *
	 * 4. Check the global cache.
	 *
	 * 5. Run the unmanaged probing logic.
	 *
	 * 6. Raise the ResolvingUnmanagedDll event on the active ALC.
	 *
	 * 7. Return NULL.
	 */

	alc_pinvoke_lock (alc);
	module = netcore_check_alc_cache (alc, scope);
	alc_pinvoke_unlock (alc);
	if (module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found in the active ALC cache: '%s'.", scope);
		goto leave;
	}

	module = (MonoDl *)netcore_resolve_with_dll_import_resolver_nofail (alc, assembly, scope, flags);
	if (module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found via DllImportResolver: '%s'.", scope);
		goto add_to_alc_cache;
	}

	module = (MonoDl *)netcore_resolve_with_load_nofail (alc, scope);
	if (module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found via LoadUnmanagedDll: '%s'.", scope);
		goto add_to_alc_cache;
	}

	MONO_ENTER_GC_SAFE;
	mono_global_loader_data_lock ();
	MONO_EXIT_GC_SAFE;
	module = (MonoDl *)g_hash_table_lookup (global_module_map, scope);
	MONO_ENTER_GC_SAFE;
	mono_global_loader_data_unlock ();
	MONO_EXIT_GC_SAFE;
	if (module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found in the global cache: '%s'.", scope);
		goto add_to_alc_cache;
	}

	module = netcore_probe_for_module_nofail (image, scope, flags);
	if (module) {
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found via filesystem probing: '%s'.", scope);
		goto add_to_global_cache;
	}

	/* As this is last chance, I've opted not to put it in a cache, but that is not necessarily the correct decision.
	 * It is rather convenient here, however, because it means the global cache will only be populated by libraries
	 * resolved via netcore_probe_for_module and not NativeLibrary, eliminating potential races/conflicts.
	 */
	module = netcore_resolve_with_resolving_event_nofail (alc, assembly, scope);
	if (module)
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "Native library found via the Resolving event: '%s'.", scope);
	goto leave;

add_to_global_cache:
	module = mono_loader_register_module_locking (scope, module);

add_to_alc_cache:
	/* Nothing is closed here because the only two places this can come from are:
	 * 1. A managed callback that made use of NativeLibrary.Load, in which case closing is dependent on NativeLibrary.Free
	 * 2. The global cache, which is only populated by results of netcore_probe_for_module. When adding to the global cache,
	 *      we free the new MonoDl if another thread beat us, so we don't have to repeat that here.
	 */
	alc_pinvoke_lock (alc);
	cached = netcore_check_alc_cache (alc, scope);
	if (cached)
		module = cached;
	else
		g_hash_table_insert (alc->pinvoke_scopes, g_strdup (scope), module);
	alc_pinvoke_unlock (alc);

leave:
	return module;
}

static int
get_dllimportsearchpath_flags (MonoCustomAttrInfo *cinfo)
{
	ERROR_DECL (error);
	MonoCustomAttrEntry *attr = NULL;
	MonoClass *dllimportsearchpath = mono_class_try_get_dllimportsearchpath_attribute_class ();
	int idx;
	int flags;

	if (!dllimportsearchpath)
		return -1;
	if (!cinfo)
		return -2;

	for (idx = 0; idx < cinfo->num_attrs; ++idx) {
		MonoClass *ctor_class = cinfo->attrs [idx].ctor->klass;
		if (ctor_class == dllimportsearchpath) {
			attr = &cinfo->attrs [idx];
			break;
		}
	}
	if (!attr)
		return -3;

	MonoDecodeCustomAttr *decoded_args = mono_reflection_create_custom_attr_data_args_noalloc (m_class_get_image (attr->ctor->klass), attr->ctor, attr->data, attr->data_size, error);
	if (!is_ok (error)) {
		mono_error_cleanup (error);
		return -4;
	}

	flags = *(gint32*)decoded_args->typed_args[0]->value.primitive;
	mono_reflection_free_custom_attr_data_args_noalloc (decoded_args);

	return flags;
}

gpointer
lookup_pinvoke_call_impl (MonoMethod *method, MonoLookupPInvokeStatus *status_out)
{
	MonoImage *image = m_class_get_image (method->klass);
	MonoAssemblyLoadContext *alc = mono_image_get_alc (image);
	MonoCustomAttrInfo *cinfo;
	int flags;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *)method;
	MonoTableInfo *tables = image->tables;
	MonoTableInfo *im = &tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 scope_token;
	const char *orig_import = NULL;
	const char *new_import = NULL;
	const char *orig_scope = NULL;
	const char *new_scope = NULL;
	const char *error_scope = NULL;
	MonoDl *module = NULL;
	gpointer addr = NULL;

	MONO_REQ_GC_UNSAFE_MODE;

	g_assert (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL);

	g_assert (status_out);

	if (piinfo->addr)
		return piinfo->addr;

	if (image_is_dynamic (image)) {
		MonoReflectionMethodAux *method_aux =
			(MonoReflectionMethodAux *)g_hash_table_lookup (
				((MonoDynamicImage*)m_class_get_image (method->klass))->method_aux_hash, method);
		if (!method_aux)
			goto exit;

		orig_import = method_aux->dllentry;
		orig_scope = method_aux->dll;
	}
	else {
		if (!piinfo->implmap_idx || mono_metadata_table_bounds_check (image, MONO_TABLE_IMPLMAP, piinfo->implmap_idx))
			goto exit;

		mono_metadata_decode_row (im, piinfo->implmap_idx - 1, im_cols, MONO_IMPLMAP_SIZE);

		if (!im_cols [MONO_IMPLMAP_SCOPE] || mono_metadata_table_bounds_check (image, MONO_TABLE_MODULEREF, im_cols [MONO_IMPLMAP_SCOPE]))
			goto exit;

		piinfo->piflags = GUINT32_TO_UINT16 (im_cols [MONO_IMPLMAP_FLAGS]);
		orig_import = mono_metadata_string_heap (image, im_cols [MONO_IMPLMAP_NAME]);
		scope_token = mono_metadata_decode_row_col (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, MONO_MODULEREF_NAME);
		orig_scope = mono_metadata_string_heap (image, scope_token);
	}

	new_scope = g_strdup (orig_scope);
	new_import = g_strdup (orig_import);

	error_scope = new_scope;

	/* If qcalls are disabled, we fall back to the normal pinvoke code for them */
#ifndef DISABLE_QCALLS
	if (strcmp (new_scope, "QCall") == 0) {
		piinfo->addr = mono_lookup_pinvoke_qcall_internal (new_import);
		if (!piinfo->addr) {
			mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DLLIMPORT,
						"Unable to find qcall for '%s'.",
						new_import);
			status_out->err_code = LOOKUP_PINVOKE_ERR_NO_SYM;
			status_out->err_arg = g_strdup (new_import);
		}
		return piinfo->addr;
	}
#endif

	if (pinvoke_override) {
		addr = pinvoke_override (new_scope, new_import);
		if (addr)
			goto exit;
	}

#ifndef HOST_WIN32
retry_with_libcoreclr:
#endif
	{
		ERROR_DECL (local_error);
		cinfo = mono_custom_attrs_from_method_checked (method, local_error);
		mono_error_cleanup (local_error);
	}
	flags = get_dllimportsearchpath_flags (cinfo);
	if (cinfo && !cinfo->cached)
		mono_custom_attrs_free (cinfo);

	if (flags < 0) {
		ERROR_DECL (local_error);
		cinfo = mono_custom_attrs_from_assembly_checked (m_class_get_image (method->klass)->assembly, TRUE, local_error);
		mono_error_cleanup (local_error);
		flags = get_dllimportsearchpath_flags (cinfo);
		if (cinfo && !cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}
	if (flags < 0)
		flags = DLLIMPORTSEARCHPATH_ASSEMBLY_DIRECTORY;
	module = netcore_lookup_native_library (alc, image, new_scope, flags);

	if (!module) {
		mono_trace (G_LOG_LEVEL_WARNING, MONO_TRACE_DLLIMPORT,
				"DllImport unable to load library '%s'.",
				error_scope);

		status_out->err_code = LOOKUP_PINVOKE_ERR_NO_LIB;
		status_out->err_arg = g_strdup (error_scope);
		goto exit;
	}

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT,
				"DllImport searching in: '%s' ('%s').", new_scope, module->full_name);


	addr = pinvoke_probe_for_symbol (module, piinfo, new_import);

	if (!addr) {
#ifndef HOST_WIN32
		if (strcmp (new_scope, "__Internal") == 0) {
			g_assert (error_scope == new_scope);
			new_scope = g_strdup (MONO_LOADER_LIBRARY_NAME);
			goto retry_with_libcoreclr;
		}
#endif
		status_out->err_code = LOOKUP_PINVOKE_ERR_NO_SYM;
		status_out->err_arg = g_strdup (new_import);
		goto exit;
	}
	piinfo->addr = addr;

exit:
	if (error_scope != new_scope) {
		g_free ((char *)error_scope);
	}
	g_free ((char *)new_import);
	g_free ((char *)new_scope);
	return addr;
}

static gpointer
pinvoke_probe_for_symbol (MonoDl *module, MonoMethodPInvoke *piinfo, const char *import)
{
	gpointer addr = NULL;

	ERROR_DECL (symbol_error);

	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT,
				"Searching for '%s'.", import);

#ifdef HOST_WIN32 // For netcore, name mangling is Windows-exclusive
	if (piinfo->piflags & PINVOKE_ATTRIBUTE_NO_MANGLE)
		addr = mono_dl_symbol (module, import, symbol_error);
	else {
		/*
		 * Search using a variety of mangled names
		 */
		for (int mangle_stdcall = 0; mangle_stdcall <= 1 && addr == NULL; mangle_stdcall++) {
#if HOST_WIN32 && HOST_X86
			const int max_managle_param_count = (mangle_stdcall == 0) ? 0 : 256;
#else
			const int max_managle_param_count = 0;
#endif
			for (int mangle_charset = 0; mangle_charset <= 1 && addr == NULL; mangle_charset ++) {
				for (int mangle_param_count = 0; mangle_param_count <= max_managle_param_count && addr == NULL; mangle_param_count += 4) {

					char *mangled_name = (char*)import;
					switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
					case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
						/* Try the mangled name first */
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", (const char*)NULL);
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
#ifdef HOST_WIN32
						if (mangle_charset == 0)
							mangled_name = g_strconcat (import, "W", (const char*)NULL);
#else
						/* Try the mangled name last */
						if (mangle_charset == 1)
							mangled_name = g_strconcat (import, "A", (const char*)NULL);
#endif
						break;
					case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
					default:
						/* Try the mangled name last */
						if (mangle_charset == 1)
							mangled_name = g_strconcat (import, "A", (const char*)NULL);
						break;
					}

#if HOST_WIN32 && HOST_X86
					/* Try the stdcall mangled name */
					/*
					 * gcc under windows creates mangled names without the underscore, but MS.NET
					 * doesn't support it, so we doesn't support it either.
					 */
					if (mangle_stdcall == 1) {
						MonoMethod *method = &piinfo->method;
						int param_count;
						if (mangle_param_count == 0)
							param_count = mono_method_signature_internal (method)->param_count * sizeof (gpointer);
						else
							/* Try brute force, since it would be very hard to compute the stack usage correctly */
							param_count = mangle_param_count;

						char *mangled_stdcall_name = g_strdup_printf ("_%s@%d", mangled_name, param_count);

						if (mangled_name != import)
							g_free (mangled_name);

						mangled_name = mangled_stdcall_name;
					}
#endif
					mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT,
								"Probing '%s'.", mangled_name);

					error_init_reuse (symbol_error);
					addr = mono_dl_symbol (module, mangled_name, symbol_error);

					if (addr)
						mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT,
									"Found as '%s'.", mangled_name);
					else
						mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT,
									"Could not find '%s' due to '%s'.", mangled_name, mono_error_get_message_without_fields (symbol_error));

					mono_error_cleanup (symbol_error);

					if (mangled_name != import)
						g_free (mangled_name);
				}
			}
		}
	}
#else
	addr = mono_dl_symbol (module, import, symbol_error);
	mono_error_cleanup (symbol_error);
#endif

	return addr;
}

void
ves_icall_System_Runtime_InteropServices_NativeLibrary_FreeLib (gpointer lib, MonoError *error)
{
	ERROR_DECL (close_error);

	MonoDl *module;
	guint32 ref_count;

	g_assert (lib);

	// Don't free __Internal
	if (internal_module && lib == internal_module->handle)
		return;

	native_library_lock ();

	module = netcore_handle_lookup (lib);
	if (module) {
		ref_count = mono_refcount_dec (module);
		if (ref_count > 0)
			goto leave;

		g_hash_table_remove (native_library_module_map, module->handle);
		g_hash_table_add (native_library_module_blocklist, module);
		mono_dl_close (module, close_error);
	} else {
		MonoDl *raw_module = (MonoDl *) g_malloc0 (sizeof (MonoDl));
		if (raw_module) {
			raw_module->handle = lib;
			mono_dl_close (raw_module, close_error);
		}
	}

leave:

	if (!is_ok (close_error)) {
		mono_error_set_invalid_operation (error, NULL);
		mono_error_cleanup (close_error);
	}

	native_library_unlock ();
}

gpointer
ves_icall_System_Runtime_InteropServices_NativeLibrary_GetSymbol (gpointer lib, MonoStringHandle symbol_name_handle, MonoBoolean throw_on_error, MonoError *error)
{
	MonoDl *module;
	gpointer symbol = NULL;
	char *symbol_name;

	g_assert (lib);

	ERROR_LOCAL_BEGIN (local_error, error, throw_on_error)

	symbol_name = mono_string_handle_to_utf8 (symbol_name_handle, error);
	goto_if_nok (error, leave_nolock);

	native_library_lock ();

	module = netcore_handle_lookup (lib);
	if (module) {
		symbol = mono_dl_symbol (module, symbol_name, error);
		if (!symbol) {
			mono_error_cleanup (error);
			error_init_reuse (error);
			mono_error_set_generic_error (error, "System", "EntryPointNotFoundException", "%s: %s", module->full_name, symbol_name);
		}
	} else {
		MonoDl raw_module = { { 0 } };
		raw_module.handle = lib;
		symbol = mono_dl_symbol (&raw_module, symbol_name, error);
		if (!symbol) {
			mono_error_cleanup (error);
			error_init_reuse (error);
			mono_error_set_generic_error (error, "System", "EntryPointNotFoundException", "%p: %s", lib, symbol_name);
		}
	}

	native_library_unlock ();

leave_nolock:
	ERROR_LOCAL_END (local_error);
	g_free (symbol_name);

	return symbol;
}

// LOCKING: expects you to hold native_library_module_lock
static MonoDl *
check_native_library_cache (MonoDl *module)
{
	gpointer handle = module->handle;

	MonoDl *cached_module = netcore_handle_lookup (handle);
	if (cached_module) {
		g_free (module->full_name);
		g_free (module);
		mono_refcount_inc (cached_module);
		return cached_module;
	}
	g_hash_table_insert (native_library_module_map, handle, (gpointer)module);

	return module;
}

gpointer
ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadByName (MonoStringHandle lib_name_handle, MonoReflectionAssemblyHandle assembly_handle, MonoBoolean has_search_flag, guint32 search_flag, MonoBoolean throw_on_error, MonoError *error)
{
	MonoDl *module;
	gpointer handle = NULL;
	MonoAssembly *assembly = MONO_HANDLE_GETVAL (assembly_handle, assembly);
	MonoImage *image = mono_assembly_get_image_internal (assembly);
	char *lib_name;

	ERROR_LOCAL_BEGIN (local_error, error, throw_on_error)

	lib_name = mono_string_handle_to_utf8 (lib_name_handle, error);
	goto_if_nok (error, leave);

	// FIXME: implement search flag defaults properly
	{
		ERROR_DECL (load_error);
		module = netcore_probe_for_module (image, lib_name, has_search_flag ? search_flag : DLLIMPORTSEARCHPATH_ASSEMBLY_DIRECTORY, load_error);
		if (!module) {
			if (mono_error_get_error_code (load_error) == MONO_ERROR_BAD_IMAGE)
				mono_error_set_generic_error (error, "System", "BadImageFormatException", "%s", lib_name);
			else
				mono_error_set_generic_error (error, "System", "DllNotFoundException", "%s", lib_name);
		}
		mono_error_cleanup (load_error);
	}

	goto_if_nok (error, leave);

	native_library_lock ();
	module = check_native_library_cache (module);
	native_library_unlock ();

	handle = module->handle;

leave:
	ERROR_LOCAL_END (local_error);
	g_free (lib_name);

	return handle;
}

gpointer
ves_icall_System_Runtime_InteropServices_NativeLibrary_LoadFromPath (MonoStringHandle lib_path_handle, MonoBoolean throw_on_error, MonoError *error)
{
	MonoDl *module;
	gpointer handle = NULL;
	char *lib_path;

	ERROR_LOCAL_BEGIN (local_error, error, throw_on_error)

	lib_path = mono_string_handle_to_utf8 (lib_path_handle, error);
	goto_if_nok (error, leave);

	ERROR_DECL (load_error);
	module = mono_dl_open (lib_path, MONO_DL_LAZY, load_error);
	if (!module) {
		const char *error_msg = mono_error_get_message_without_fields (load_error);
		guint16 error_code = mono_error_get_error_code (load_error);

		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_DLLIMPORT, "DllImport error loading library '%s': '%s'.", lib_path, error_msg);

		if (error_code == MONO_ERROR_BAD_IMAGE)
			mono_error_set_generic_error (error, "System", "BadImageFormatException", "'%s': '%s'", lib_path, error_msg);
		else
			mono_error_set_generic_error (error, "System", "DllNotFoundException", "'%s': '%s'", lib_path, error_msg);
	}
	mono_error_cleanup (load_error);
	goto_if_nok (error, leave);

	native_library_lock ();
	module = check_native_library_cache (module);
	native_library_unlock ();

	handle = module->handle;

leave:
	ERROR_LOCAL_END (local_error);
	g_free (lib_path);

	return handle;
}

void
mono_loader_install_pinvoke_override (PInvokeOverrideFn override_fn)
{
	pinvoke_override = override_fn;
}
