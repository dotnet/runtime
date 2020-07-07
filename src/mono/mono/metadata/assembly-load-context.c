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

void
mono_alc_init (MonoAssemblyLoadContext *alc, MonoDomain *domain, gboolean collectible)
{
	MonoLoadedImages *li = g_new0 (MonoLoadedImages, 1);
	mono_loaded_images_init (li, alc);
	alc->domain = domain;
	alc->loaded_images = li;
	alc->loaded_assemblies = NULL;
	alc->unloading = FALSE;
	alc->collectible = collectible;
	alc->pinvoke_scopes = g_hash_table_new_full (g_str_hash, g_str_equal, g_free, NULL);
	mono_coop_mutex_init (&alc->assemblies_lock);
	mono_coop_mutex_init (&alc->pinvoke_lock);
}

void
mono_alc_cleanup (MonoAssemblyLoadContext *alc)
{
	/*
	 * This is still very much WIP. It needs to be split up into various other functions and adjusted to work with the 
	 * managed LoaderAllocator design. For now, I've put it all in this function, but don't look at it too closely.
	 * 
	 * Of particular note: the minimum refcount on assemblies is 2: one for the domain and one for the ALC. 
	 * The domain refcount might be less than optimal on netcore, but its removal is too likely to cause issues for now.
	 */
	GSList *tmp;
	MonoDomain *domain = alc->domain;

	g_assert (alc != mono_domain_default_alc (domain));
	g_assert (alc->collectible == TRUE);

	// FIXME: alc unloading profiler event

	// Remove the assemblies from domain_assemblies
	mono_domain_assemblies_lock (domain);
	for (tmp = alc->loaded_assemblies; tmp; tmp = tmp->next) {
		MonoAssembly *assembly = (MonoAssembly *)tmp->data;
		g_slist_remove (domain->domain_assemblies, assembly);
		mono_atomic_dec_i32 (&assembly->ref_count);
		mono_trace (G_LOG_LEVEL_DEBUG, MONO_TRACE_ASSEMBLY, "Unloading ALC [%p], removing assembly %s[%p] from domain_assemblies, ref_count=%d\n", alc, assembly->aname.name, assembly, assembly->ref_count);
	}
	mono_domain_assemblies_unlock (domain);

	// Some equivalent to mono_gc_clear_domain? I guess in our case we just have to assert that we have no lingering references?

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

	// FIXME: alc unloaded profiler event

	g_hash_table_destroy (alc->pinvoke_scopes);
	mono_coop_mutex_destroy (&alc->assemblies_lock);
	mono_coop_mutex_destroy (&alc->pinvoke_lock);

	mono_loaded_images_free (alc->loaded_images);

	// TODO: free mempool stuff/jit info tables, see domain freeing for an example
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
	} else {
		/* create it */
		alc = mono_domain_create_individual_alc (domain, this_gchandle, collectible, error);
	}
	return alc;
}

void
ves_icall_System_Runtime_Loader_AssemblyLoadContext_PrepareForAssemblyLoadContextRelease (gpointer alc_pointer, gpointer strong_gchandle_ptr, MonoError *error)
{
	MonoGCHandle strong_gchandle = (MonoGCHandle)strong_gchandle_ptr;
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)alc_pointer;

	g_assert (alc->collectible == TRUE);
	g_assert (alc->unloading == FALSE);
	alc->unloading = TRUE;

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
	MonoManagedAssemblyLoadContextHandle managed_alc = MONO_HANDLE_CAST (MonoManagedAssemblyLoadContext, mono_gchandle_get_target_handle (alc_gchandle));
	MonoAssemblyLoadContext *alc = (MonoAssemblyLoadContext *)MONO_HANDLE_GETVAL (managed_alc, native_assembly_load_context);
	return alc;
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
		MonoClass *alc_class = mono_class_get_assembly_load_context_class ();
		g_assert (alc_class);
		resolve = mono_class_get_method_from_name_checked (alc_class, "MonoResolveUsingResolvingEvent", -1, 0, local_error);
		mono_error_assert_ok (local_error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, resolve)

	g_assert (resolve);

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
