#include "config.h"
#include "mono/utils/mono-compiler.h"

#ifdef ENABLE_NETCORE // MonoAssemblyLoadContext support only in netcore Mono

#include "mono/metadata/assembly.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/exception-internals.h"
#include "mono/metadata/icall-decl.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/loaded-images-internals.h"
#include "mono/metadata/mono-private-unstable.h"
#include "mono/utils/mono-error-internals.h"
#include "mono/utils/mono-logger-internals.h"

GENERATE_GET_CLASS_WITH_CACHE (assembly_load_context, "System.Runtime.Loader", "AssemblyLoadContext");

static void
mono_alc_init (MonoAssemblyLoadContext *alc, MonoDomain *domain, gboolean collectible)
{
	MonoLoadedImages *li = g_new0 (MonoLoadedImages, 1);
	mono_loaded_images_init (li, alc);
	alc->domain = domain;
	alc->loaded_images = li;
	alc->loaded_assemblies = NULL;
	alc->memory_manager = mono_mem_manager_create_singleton (alc, domain, collectible);
	alc->generic_memory_managers = g_ptr_array_new ();
	mono_coop_mutex_init (&alc->memory_managers_lock);
	alc->unloading = FALSE;
	alc->collectible = collectible;
	alc->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	mono_coop_mutex_init (&alc->assemblies_lock);
	mono_coop_mutex_init (&alc->pinvoke_lock);
}

static MonoAssemblyLoadContext *
mono_alc_create (MonoDomain *domain, gboolean is_default, gboolean collectible)
{
	MonoAssemblyLoadContext *alc = NULL;

	mono_domain_alcs_lock (domain);
	if (is_default && domain->default_alc)
		goto leave;

	alc = g_new0 (MonoAssemblyLoadContext, 1);
	mono_alc_init (alc, domain, collectible);

	domain->alcs = g_slist_prepend (domain->alcs, alc);
	if (is_default)
		domain->default_alc = alc;

leave:
	mono_domain_alcs_unlock (domain);
	return alc;
}

void
mono_alc_create_default (MonoDomain *domain)
{
	if (domain->default_alc)
		return;
	mono_alc_create (domain, TRUE, FALSE);
}

MonoAssemblyLoadContext *
mono_alc_create_individual (MonoDomain *domain, MonoGCHandle this_gchandle, gboolean collectible, MonoError *error)
{
	MonoAssemblyLoadContext *alc = mono_alc_create (domain, FALSE, collectible);

	alc->gchandle = this_gchandle;

	return alc;
}

static void
mono_alc_cleanup_assemblies (MonoAssemblyLoadContext *alc)
{
	// The minimum refcount on assemblies is 2: one for the domain and one for the ALC. 
	// The domain refcount might be less than optimal on netcore, but its removal is too likely to cause issues for now.
	GSList *tmp;
	MonoDomain *domain = alc->domain;

	// Remove the assemblies from domain_assemblies
	mono_domain_assemblies_lock (domain);
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;
		domain->domain_assemblies = g_slist_remove (domain->domain_assemblies, assembly);
		mono_assembly_decref (assembly);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], removing assembly %s[%p] from domain_assemblies, ref_count=%d\n", alc, assembly->aname.name, assembly, assembly->ref_count);
	}
	mono_domain_assemblies_unlock (domain);

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
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], dynamic assembly %s[%p], ref_count=%d", domain, assembly->aname.name, assembly, assembly->ref_count);
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
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], non-dynamic assembly %s[%p], ref_count=%d", domain, assembly->aname.name, assembly, assembly->ref_count);
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
	MonoDomain *domain = alc->domain;

	g_assert (alc != mono_domain_default_alc (domain));
	g_assert (alc->collectible == TRUE);

	// TODO: alc unloading profiler event

	// Remove from domain list
	mono_domain_alcs_lock (domain);
	domain->alcs = g_slist_remove (domain->alcs, alc);
	mono_domain_alcs_unlock (domain);

	mono_alc_cleanup_assemblies (alc);

	mono_mem_manager_free_singleton (alc->memory_manager, FALSE);
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
ves_icall_System_Runtime_Loader_AssemblyLoadContext_InternalInitializeNativeALC (gpointer this_gchandle_ptr, MonoBoolean is_default_alc, MonoBoolean collectible, MonoError *error)
{
	/* If the ALC is collectible, this_gchandle is weak, otherwise it's strong. */
	MonoGCHandle this_gchandle = (MonoGCHandle)this_gchandle_ptr;

	MonoDomain *domain = mono_domain_get ();
	MonoAssemblyLoadContext *alc = NULL;

	if (is_default_alc) {
		alc = mono_domain_default_alc (domain);
		g_assert (alc);
		if (!alc->gchandle)
			alc->gchandle = this_gchandle;
	} else
		alc = mono_alc_create_individual (domain, this_gchandle, collectible, error);

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

gboolean
mono_alc_is_default (MonoAssemblyLoadContext *alc)
{
	return alc == mono_alc_domain (alc)->default_alc;
}

MonoAssemblyLoadContext *
mono_alc_from_gchandle (MonoGCHandle alc_gchandle)
{
	HANDLE_FUNCTION_ENTER ();
	MonoManagedAssemblyLoadContextHandle managed_alc = MONO_HANDLE_CAST (MonoManagedAssemblyLoadContext, mono_gchandle_get_target_handle (alc_gchandle));
	MonoAssemblyLoadContext *alc = MONO_HANDLE_GETVAL (managed_alc, native_assembly_load_context);
	HANDLE_FUNCTION_RETURN_VAL (alc);
}

MonoGCHandle
mono_alc_get_default_gchandle (void)
{
	// Because the default domain is never unloadable, this should be a strong handle and never change
	return mono_domain_default_alc (mono_domain_get ())->gchandle;
}

static MonoAssembly*
invoke_resolve_method (MonoMethod *resolve_method, MonoAssemblyLoadContext *alc, MonoAssemblyName *aname, MonoError *error)
{
	MonoAssembly *result = NULL;
	char* aname_str = NULL;

	if (mono_runtime_get_no_exec ())
		return NULL;

	HANDLE_FUNCTION_ENTER ();

	aname_str = mono_stringify_assembly_name (aname);

	MonoStringHandle aname_obj = mono_string_new_handle (mono_alc_domain (alc), aname_str, error);
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

#endif /* ENABLE_NETCORE */

MONO_EMPTY_SOURCE_FILE (assembly_load_context)
