#include "config.h"
#include "mono/utils/mono-compiler.h"

#include "mono/metadata/assembly.h"
#include "mono/metadata/assembly-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/exception-internals.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loaded-images-internals.h"
#include "mono/metadata/mono-private-unstable.h"
#include "mono/metadata/mono-debug.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-logger-internals.h"

GENERATE_GET_CLASS_WITH_CACHE (assembly_load_context, "System.Runtime.Loader", "AssemblyLoadContext");
static GENERATE_GET_CLASS_WITH_CACHE (assembly, "System.Reflection", "Assembly");

static GSList *alcs;
static MonoAssemblyLoadContext *default_alc;
static MonoCoopMutex alc_list_lock; /* Used when accessing 'alcs' */
/* Protected by alc_list_lock */
static GSList *loaded_assemblies;

static inline void
alcs_lock (void)
{
	mono_coop_mutex_lock (&alc_list_lock);
}

static inline void
alcs_unlock (void)
{
	mono_coop_mutex_unlock (&alc_list_lock);
}

static void
mono_alc_init (MonoAssemblyLoadContext *alc, gboolean collectible)
{
	MonoLoadedImages *li = g_new0 (MonoLoadedImages, 1);
	mono_loaded_images_init (li, alc);
	alc->loaded_images = li;
	alc->loaded_assemblies = NULL;
	alc->memory_manager = mono_mem_manager_new (&alc, 1, collectible);
	alc->generic_memory_managers = g_ptr_array_new ();
	mono_coop_mutex_init (&alc->memory_managers_lock);
	alc->unloading = FALSE;
	alc->collectible = collectible;
	alc->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	mono_coop_mutex_init (&alc->assemblies_lock);
	mono_coop_mutex_init (&alc->pinvoke_lock);
}

static MonoAssemblyLoadContext *
mono_alc_create (gboolean collectible)
{
	MonoAssemblyLoadContext *alc = NULL;

	alc = g_new0 (MonoAssemblyLoadContext, 1);
	mono_alc_init (alc, collectible);

	alcs_lock ();
	alcs = g_slist_prepend (alcs, alc);
	alcs_unlock ();

	return alc;
}

void
mono_alcs_init (void)
{
	mono_coop_mutex_init (&alc_list_lock);

	default_alc = mono_alc_create (FALSE);
	default_alc->gchandle = mono_gchandle_new_internal (NULL, FALSE);
}

MonoAssemblyLoadContext *
mono_alc_get_default (void)
{
	g_assert (default_alc);
	return default_alc;
}

MonoAssemblyLoadContext *
mono_alc_create_individual (MonoGCHandle this_gchandle, gboolean collectible, MonoError *error)
{
	MonoAssemblyLoadContext *alc = mono_alc_create (collectible);

	alc->gchandle = this_gchandle;

	return alc;
}

static void
mono_alc_cleanup_assemblies (MonoAssemblyLoadContext *alc)
{
	// The minimum refcount on assemblies is 2: one for the domain and one for the ALC. 
	// The domain refcount might be less than optimal on netcore, but its removal is too likely to cause issues for now.
	GSList *tmp;

	// Remove the assemblies from loaded_assemblies
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;

		alcs_lock ();
		loaded_assemblies = g_slist_remove (loaded_assemblies, assembly);
		alcs_unlock ();

		mono_assembly_decref (assembly);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], removing assembly %s[%p] from loaded_assemblies, ref_count=%d\n", alc, assembly->aname.name, assembly, assembly->ref_count);
	}

	// Release the GC roots
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;
		mono_assembly_release_gc_roots (assembly);
	}

	// Close dynamic assemblies
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;
		if (!assembly->image || !image_is_dynamic (assembly->image))
			continue;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], dynamic assembly %s[%p], ref_count=%d", alc, assembly->aname.name, assembly, assembly->ref_count);
		if (!mono_assembly_close_except_image_pools (assembly))
			tmp->data = NULL;
	}

	// Close the remaining assemblies
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;
		if (!assembly)
			continue;
		if (!assembly->image || image_is_dynamic (assembly->image))
			continue;
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], non-dynamic assembly %s[%p], ref_count=%d", alc, assembly->aname.name, assembly, assembly->ref_count);
		if (!mono_assembly_close_except_image_pools (assembly))
			tmp->data = NULL;
	}

	// Complete the second closing pass on lingering assemblies
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;
		if (assembly)
			mono_assembly_close_finish (assembly);
	}

	// Free the loaded_assemblies
	g_slist_free (alc->loaded_assemblies);
	alc->loaded_assemblies = NULL;

	mono_coop_mutex_destroy (&alc->assemblies_lock);

	mono_loaded_images_free (alc->loaded_images);
	alc->loaded_images = NULL;

	// TODO: free mempool stuff/jit info tables, see domain freeing for an example
}

static void
mono_alc_cleanup (MonoAssemblyLoadContext *alc)
{
	g_assert (alc != default_alc);
	g_assert (alc->collectible == TRUE);

	// TODO: alc unloading profiler event

	// Remove from alc list
	alcs_lock ();
	alcs = g_slist_remove (alcs, alc);
	alcs_unlock ();

	mono_alc_cleanup_assemblies (alc);

	mono_mem_manager_free (alc->memory_manager, FALSE);
	alc->memory_manager = NULL;

	/*for (int i = 0; i < alc->generic_memory_managers->len; i++) {
		MonoGenericMemoryManager *memory_manager = (MonoGenericMemoryManager *)alc->generic_memory_managers->pdata [i];
		mono_mem_manager_free_generic (memory_manager, FALSE);
	}*/
	g_ptr_array_free (alc->generic_memory_managers, TRUE);
	mono_coop_mutex_destroy (&alc->memory_managers_lock);

	mono_gchandle_free_internal (alc->gchandle);
	alc->gchandle = NULL;

	g_hash_table_destroy (alc->pinvoke_scopes);
	alc->pinvoke_scopes = NULL;
	mono_coop_mutex_destroy (&alc->pinvoke_lock);

	g_free (alc->name);
	alc->name = NULL;

	// TODO: alc unloaded profiler event
}

static void
mono_alc_free (MonoAssemblyLoadContext *alc)
{
	mono_alc_cleanup (alc);
	g_free (alc);
}

void
mono_alc_assemblies_lock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_lock (&alc->assemblies_lock);
}

void
mono_alc_assemblies_unlock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_unlock (&alc->assemblies_lock);
}

void
mono_alc_memory_managers_lock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_lock (&alc->memory_managers_lock);
}

void
mono_alc_memory_managers_unlock (MonoAssemblyLoadContext *alc)
{
	mono_coop_mutex_unlock (&alc->memory_managers_lock);
}

gpointer
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC (gpointer this_gchandle_ptr, const char *name,
																				 MonoBoolean is_default_alc, MonoBoolean collectible, MonoError *error)
{
	/* If the ALC is collectible, this_gchandle is weak, otherwise it's strong. */
	MonoGCHandle this_gchandle = (MonoGCHandle)this_gchandle_ptr;
	MonoAssemblyLoadContext *alc = NULL;

	if (is_default_alc) {
		alc = default_alc;
		g_assert (alc);

		// Change target of the existing GCHandle
		mono_gchandle_set_target (alc->gchandle, mono_gchandle_get_target_internal (this_gchandle));
		mono_gchandle_free_internal (this_gchandle);
	} else {
		alc = mono_alc_create_individual (this_gchandle, collectible, error);
	}

	if (name)
		alc->name = g_strdup (name);
	else
		alc->name = g_strdup ("<default>");

	return alc;
}

void
ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease (gpointer alc_pointer, gpointer strong_gchandle_ptr, MonoError *error)
{
	MonoGCHandle strong_gchandle = (MonoGCHandle)strong_gchandle_ptr;
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)alc_pointer;

	g_assert (alc->collectible);
	g_assert (!alc->unloading);
	g_assert (alc->gchandle);

	alc->unloading = TRUE;

	// Replace the weak gchandle with the new strong one to keep the managed ALC alive
	MonoGCHandle weak_gchandle = alc->gchandle;
	alc->gchandle = strong_gchandle;
	mono_gchandle_free_internal (weak_gchandle);
}

gpointer
ves_icall_System_Runtime_Loader_AssemblyLoadContext_GetLoadContextForAssembly (MonoReflectionAssemblyHandle assm_obj, MonoError *error)
{
	MonoAssembly *assm = MONO_HANDLE_GETVAL (assm_obj, assembly);
	MonoAssemblyLoadContext *alc = mono_assembly_get_alc (assm);

	return (gpointer)alc->gchandle;
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

MonoArrayHandle
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalGetLoadedAssemblies (MonoError *error)
{
	GPtrArray *assemblies = mono_alc_get_all_loaded_assemblies ();

	MonoArrayHandle res = mono_array_new_handle (mono_class_get_assembly_class (), assemblies->len, error);
	goto_if_nok (error, leave);
	for (int i = 0; i < assemblies->len; ++i) {
		if (!add_assembly_to_array (res, i, (MonoAssembly *)g_ptr_array_index (assemblies, i), error))
			goto leave;
	}

leave:
	g_ptr_array_free (assemblies, TRUE);
	return res;
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

gboolean
mono_alc_is_default (MonoAssemblyLoadContext *alc)
{
	return alc == default_alc;
}

MonoAssemblyLoadContext *
mono_alc_from_gchandle (MonoGCHandle alc_gchandle)
{
	if (alc_gchandle == default_alc->gchandle)
		return default_alc;

	HANDLE_FUNCTION_ENTER ();
	MonoManagedAssemblyLoadContextHandle managed_alc = MONO_HANDLE_CAST (MonoManagedAssemblyLoadContext, mono_gchandle_get_target_handle (alc_gchandle));
	g_assert (!MONO_HANDLE_IS_NULL (managed_alc));
	MonoAssemblyLoadContext *alc = MONO_HANDLE_GETVAL (managed_alc, native_assembly_load_context);
	HANDLE_FUNCTION_RETURN_VAL (alc);
}

MonoGCHandle
mono_alc_get_default_gchandle (void)
{
	// Because the default alc is never unloadable, this should be a strong handle and never change
	return default_alc->gchandle;
}

static MonoAssembly*
invoke_resolve_method (MonoMethod *resolve_method, MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	MonoAssembly *result = NULL;
	char* aname_str = NULL;

	if (mono_runtime_get_no_exec ())
		return NULL;

	if (!mono_gchandle_get_target_internal (alc->gchandle))
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	aname_str = mono_stringify_assembly_name (aname);

	MonoStringHandle aname_obj = mono_string_new_handle (aname_str, error);
	goto_if_nok (error, leave);

	MonoReflectionAssemblyHandle assm;
	gpointer gchandle;
	gchandle = (gpointer)alc->gchandle;
	gpointer args [2];
	args [0] = &gchandle;
	args [1] = MONO_HANDLE_RAW (aname_obj);
	assm = MONO_HANDLE_CAST (MonoReflectionAssembly, mono_runtime_try_invoke_handle (resolve_method, NULL_HANDLE, args, error));
	goto_if_nok (error, leave);

	if (MONO_HANDLE_BOOL (assm))
		result = MONO_HANDLE_GETVAL (assm, assembly);

leave:
	g_free (aname_str);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoAssembly*
mono_alc_invoke_resolve_using_load (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, resolve)

		ERROR_DECL (local_error);
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		resolve = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingLoad", -1, 0, local_error);
		mono_error_assert_ok (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)

	g_assert (resolve);

	return invoke_resolve_method (resolve, alc, aname, error);
}

MonoAssembly*
mono_alc_invoke_resolve_using_load_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *result = NULL;
	ERROR_DECL (error);

	result = mono_alc_invoke_resolve_using_load (alc, aname, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Error while invoking ALC Load(\"%s\") method: '%s'", aname->name, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

static MonoAssembly*
mono_alc_invoke_resolve_using_resolving_event (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, resolve)

		ERROR_DECL (local_error);
		static gboolean inited;
		if (!inited) {
			MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
			g_assert (alc_class);
			resolve = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingResolvingEvent", -1, 0, local_error);
			inited = TRUE;
		}
		mono_error_cleanup (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)

	if (!resolve)
		return NULL;

	return invoke_resolve_method (resolve, alc, aname, error);
}

MonoAssembly*
mono_alc_invoke_resolve_using_resolving_event_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *result = NULL;
	ERROR_DECL (error);

	result = mono_alc_invoke_resolve_using_resolving_event (alc, aname, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Error while invoking ALC Resolving(\"%s\") event: '%s'", aname->name, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

static MonoAssembly*
mono_alc_invoke_resolve_using_resolve_satellite (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, resolve)

		ERROR_DECL (local_error);
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		resolve = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingResolveSatelliteAssembly", -1, 0, local_error);
		mono_error_assert_ok (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)

	g_assert (resolve);

	return invoke_resolve_method (resolve, alc, aname, error);
}

MonoAssembly*
mono_alc_invoke_resolve_using_resolve_satellite_nofail (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
	MonoAssembly *result = NULL;
	ERROR_DECL (error);

	result = mono_alc_invoke_resolve_using_resolve_satellite (alc, aname, error);
	if (!is_ok (error))
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Error while invoking ALC ResolveSatelliteAssembly(\"%s\") method: '%s'", aname->name, mono_error_get_message (error));

	mono_error_cleanup (error);

	return result;
}

void
mono_alc_add_assembly (MonoAssemblyLoadContext *alc, MonoAssembly *ass)
{
	GSList *tmp;

	g_assert (ass);

	if (!ass->aname.name)
		return;

	mono_alc_assemblies_lock (alc);
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		if (tmp->data == ass) {
			mono_alc_assemblies_unlock (alc);
			return;
		}
	}

	mono_assembly_addref (ass);
	// Prepending here will break the test suite with frequent InvalidCastExceptions, so we have to append
	alc->loaded_assemblies = g_slist_append (alc->loaded_assemblies, ass);
	mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Assembly %s[%p] added to ALC '%s'[%p], ref_count=%d", ass->aname.name, ass, alc->name, (gpointer)alc, ass->ref_count);
	mono_alc_assemblies_unlock (alc);

	alcs_lock ();
	loaded_assemblies = g_slist_append (loaded_assemblies, ass);
	alcs_unlock ();
}

MonoAssembly*
mono_alc_find_assembly (MonoAssemblyLoadContext *alc, MonoAssemblyName *aname)
{
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

/*
 * mono_alc_get_all_loaded_assemblies:
 *
 *   Return a list of loaded assemblies in all appdomains.
 */
GPtrArray*
mono_alc_get_all_loaded_assemblies (void)
{
	GSList *tmp;
	GPtrArray *assemblies;
	MonoAssembly *ass;

	assemblies = g_ptr_array_new ();
	alcs_lock ();
	for (tmp = loaded_assemblies; tmp; tmp = tmp->next) {
		ass = (MonoAssembly *)tmp->data;
		g_ptr_array_add (assemblies, ass);
	}
	alcs_unlock ();
	return assemblies;
}
