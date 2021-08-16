/**
 * Functions that are in the (historical) embedding API
 * but must not be used by the runtime. Often
 * just a thin wrapper mono_foo => mono_foo_internal.
 *
 * Copyright 2018 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

// FIXME In order to confirm this is all extern_only,
// a variant of the runtime should be linked without it.

#include "config.h"
#include "class-internals.h"
#include "domain-internals.h"
#include "mono-hash-internals.h"
#include "mono-config-internals.h"
#include "object-internals.h"
#include "class-init.h"
#include "assembly.h"
#include "marshal.h"
#include "object.h"
#include "assembly-internals.h"
#include "external-only.h"
#include "threads.h"
#include "threads-types.h"
#include "jit-info.h"

/**
 * mono_gchandle_new:
 * \param obj managed object to get a handle for
 * \param pinned whether the object should be pinned
 * This returns a handle that wraps the object, this is used to keep a
 * reference to a managed object from the unmanaged world and preventing the
 * object from being disposed.
 *
 * If \p pinned is false the address of the object can not be obtained, if it is
 * true the address of the object can be obtained.  This will also pin the
 * object so it will not be possible by a moving garbage collector to move the
 * object.
 *
 * \returns a handle that can be used to access the object from unmanaged code.
 */
uint32_t
mono_gchandle_new (MonoObject *obj, mono_bool pinned)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (uint32_t, (uint32_t)(size_t)mono_gchandle_new_internal (obj, pinned));
}

MonoGCHandle
mono_gchandle_new_v2 (MonoObject *obj, mono_bool pinned)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoGCHandle, mono_gchandle_new_internal (obj, pinned));
}

/**
 * mono_gchandle_new_weakref:
 * \param obj managed object to get a handle for
 * \param track_resurrection Determines how long to track the object, if this is set to TRUE, the object is tracked after finalization, if FALSE, the object is only tracked up until the point of finalization.
 *
 * This returns a weak handle that wraps the object, this is used to
 * keep a reference to a managed object from the unmanaged world.
 * Unlike the \c mono_gchandle_new_internal the object can be reclaimed by the
 * garbage collector.  In this case the value of the GCHandle will be
 * set to zero.
 *
 * If \p track_resurrection is TRUE the object will be tracked through
 * finalization and if the object is resurrected during the execution
 * of the finalizer, then the returned weakref will continue to hold
 * a reference to the object.   If \p track_resurrection is FALSE, then
 * the weak reference's target will become NULL as soon as the object
 * is passed on to the finalizer.
 *
 * \returns a handle that can be used to access the object from
 * unmanaged code.
 */
uint32_t
mono_gchandle_new_weakref (MonoObject *obj, mono_bool track_resurrection)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (uint32_t, (uint32_t)(size_t)mono_gchandle_new_weakref_internal (obj, track_resurrection));
}

MonoGCHandle
mono_gchandle_new_weakref_v2 (MonoObject *obj, mono_bool track_resurrection)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoGCHandle, mono_gchandle_new_weakref_internal (obj, track_resurrection));
}

/**
 * mono_gchandle_get_target:
 * \param gchandle a GCHandle's handle.
 *
 * The handle was previously created by calling \c mono_gchandle_new or
 * \c mono_gchandle_new_weakref.
 *
 * \returns a pointer to the \c MonoObject* represented by the handle or
 * NULL for a collected object if using a weakref handle.
 */
MonoObject*
mono_gchandle_get_target (uint32_t gchandle)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoObject*, mono_gchandle_get_target_internal ((MonoGCHandle)(size_t)gchandle));
}

MonoObject*
mono_gchandle_get_target_v2 (MonoGCHandle gchandle)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoObject*, mono_gchandle_get_target_internal (gchandle));
}

/**
 * mono_gchandle_free:
 * \param gchandle a GCHandle's handle.
 *
 * Frees the \p gchandle handle.  If there are no outstanding
 * references, the garbage collector can reclaim the memory of the
 * object wrapped.
 */
void
mono_gchandle_free (uint32_t gchandle)
{
	/* Xamarin.Mac and Xamarin.iOS can call this from a worker thread
	 * that's not attached to the runtime. This is okay for SGen because
	 * the gchandle code is lockfree.  SGen calls back into Mono which
	 * fires a profiler event, so the profiler must be prepared to be
	 * called from threads that aren't attached to Mono. */
	MONO_EXTERNAL_ONLY_VOID (mono_gchandle_free_internal ((MonoGCHandle)(size_t)gchandle));
}

void
mono_gchandle_free_v2 (MonoGCHandle gchandle)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gchandle_free_internal (gchandle));
}

/* GC write barriers support */

/**
 * mono_gc_wbarrier_set_field:
 * \param obj object containing the destination field
 * \param field_ptr address of field inside the object
 * \param value reference to the object to be stored
 * Stores an object reference inside another object, executing a write barrier
 * if needed.
 */
void
mono_gc_wbarrier_set_field (MonoObject *obj, void* field_ptr, MonoObject* value)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_set_field_internal (obj, field_ptr, value));
}

/**
 * mono_gc_wbarrier_set_arrayref:
 * \param arr array containing the destination slot
 * \param slot_ptr address of slot inside the array
 * \param value reference to the object to be stored
 * Stores an object reference inside an array of objects, executing a write
 * barrier if needed.
 */
void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, void* slot_ptr, MonoObject* value)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_set_arrayref_internal (arr, slot_ptr, value));
}

/**
 * mono_gc_wbarrier_arrayref_copy:
 * \param dest_ptr destination slot address
 * \param src_ptr source slot address
 * \param count number of references to copy
 * Copies \p count references from one array to another, executing a write
 * barrier if needed.
 */
void
mono_gc_wbarrier_arrayref_copy (void* dest_ptr, /*const*/ void* src_ptr, int count)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_arrayref_copy_internal (dest_ptr, src_ptr, count));
}

/**
 * mono_gc_wbarrier_generic_store:
 * \param ptr address of field
 * \param obj object to store
 * Stores the \p value object inside the field represented by \p ptr,
 * executing a write barrier if needed.
 */
void
mono_gc_wbarrier_generic_store (void* ptr, MonoObject* value)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_generic_store_internal (ptr, value));
}

/**
 * mono_gc_wbarrier_generic_store_atomic:
 * Same as \c mono_gc_wbarrier_generic_store but performs the store
 * as an atomic operation with release semantics.
 */
void
mono_gc_wbarrier_generic_store_atomic (void *ptr, MonoObject *value)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_generic_store_atomic_internal (ptr, value));
}

/**
 * mono_gc_wbarrier_generic_nostore:
 * Executes a write barrier for an address, informing the GC that
 * the reference stored at that address has been changed.
 */
void
mono_gc_wbarrier_generic_nostore (void* ptr)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_generic_nostore_internal (ptr));
}

/**
 * mono_gc_wbarrier_object_copy:
 * \param dest destination address
 * \param src source address
 * \param count number of elements to copy
 * \param klass type of elements to copy
 * Copies \p count elements of type \p klass from \p src address to
 * \dest address, executing any necessary write barriers.
 */
void
mono_gc_wbarrier_value_copy (void* dest, /*const*/ void* src, int count, MonoClass *klass)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_value_copy_internal (dest, src, count, klass));
}

/**
 * mono_gc_wbarrier_object_copy:
 * \param obj destination object
 * \param src source object
 * Copies contents of \p src to \p obj, executing any necessary write
 * barriers.
 */
void
mono_gc_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_gc_wbarrier_object_copy_internal (obj, src));
}

/**
 * mono_class_init:
 * \param klass the class to initialize
 *
 * Compute the \c instance_size, \c class_size and other infos that cannot be
 * computed at \c mono_class_get time. Also compute vtable_size if possible.
 * Initializes the following fields in \p klass:
 * - all the fields initialized by \c mono_class_init_sizes
 * - has_cctor
 * - ghcimpl
 * - inited
 *
 * LOCKING: Acquires the loader lock.
 *
 * \returns TRUE on success or FALSE if there was a problem in loading
 * the type (incorrect assemblies, missing assemblies, methods, etc).
 */
mono_bool
mono_class_init (MonoClass *klass)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (gboolean, mono_class_init_internal (klass));
}

/**
 * mono_g_hash_table_new_type:
 */
MonoGHashTable*
mono_g_hash_table_new_type (GHashFunc hash_func, GEqualFunc key_equal_func, MonoGHashGCType type, MonoGCRootSource source, void *key, const char *msg)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoGHashTable*, mono_g_hash_table_new_type_internal (hash_func, key_equal_func, type, source, key, msg));
}

/**
 * mono_config_for_assembly:
 */
void 
mono_config_for_assembly (MonoImage *assembly)
{
}

/**
 * mono_class_get_property_from_name:
 * \param klass a class
 * \param name name of the property to lookup in the specified class
 *
 * Use this method to lookup a property in a class
 * \returns the \c MonoProperty with the given name, or NULL if the property
 * does not exist on the \p klass.
 */
MonoProperty*
mono_class_get_property_from_name (MonoClass *klass, const char *name)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (MonoProperty*, mono_class_get_property_from_name_internal (klass, name));
}

/**
 * mono_class_is_subclass_of:
 * \param klass class to probe if it is a subclass of another one
 * \param klassc the class we suspect is the base class
 * \param check_interfaces whether we should perform interface checks
 *
 * This method determines whether \p klass is a subclass of \p klassc.
 *
 * If the \p check_interfaces flag is set, then if \p klassc is an interface
 * this method return TRUE if the \p klass implements the interface or
 * if \p klass is an interface, if one of its base classes is \p klass.
 *
 * If \p check_interfaces is false, then if \p klass is not an interface,
 * it returns TRUE if the \p klass is a subclass of \p klassc.
 *
 * if \p klass is an interface and \p klassc is \c System.Object, then this function
 * returns TRUE.
 *
 */
gboolean
mono_class_is_subclass_of (MonoClass *klass, MonoClass *klassc, gboolean check_interfaces)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE (gboolean, mono_class_is_subclass_of_internal (klass, klassc, check_interfaces));
}

/**
 * mono_domain_set_internal:
 * \param domain the new domain
 *
 * Sets the current domain to \p domain.
 */
void
mono_domain_set_internal (MonoDomain *domain)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_domain_set_internal_with_options (domain, TRUE));
}

/**
 * mono_domain_set:
 * \param domain domain
 * \param force force setting.
 *
 * Set the current appdomain to \p domain. If \p force is set, set it even
 * if it is being unloaded.
 *
 * \returns TRUE on success; FALSE if the domain is unloaded
 */
gboolean
mono_domain_set (MonoDomain *domain, gboolean force)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_domain_set_internal_with_options (domain, TRUE));
	return TRUE;
}

/**
 * mono_assembly_name_free:
 * \param aname assembly name to free
 *
 * Frees the provided assembly name object.
 * (it does not frees the object itself, only the name members).
 */
void
mono_assembly_name_free (MonoAssemblyName *aname)
{
	if (!aname)
		return;
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_assembly_name_free_internal (aname));
}

/**
 * mono_thread_manage:
 *
 */
void
mono_thread_manage (void)
{
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_thread_manage_internal ());
}

void
mono_register_config_for_assembly (const char* assembly_name, const char* config_xml)
{
}

/**
 * mono_domain_free:
 * \param domain the domain to release
 * \param force if TRUE, it allows the root domain to be released (used at shutdown only).
 *
 * This releases the resources associated with the specific domain.
 * This is a low-level function that is invoked by the AppDomain infrastructure
 * when necessary.
 *
 * In theory, this is dead code on netcore and thus does not need to be ALC-aware.
 */
void
mono_domain_free (MonoDomain *domain, gboolean force)
{
	g_assert_not_reached ();
}

/**
 * mono_domain_get_id:
 *
 * A domain ID is guaranteed to be unique for as long as the domain
 * using it is alive. It may be reused later once the domain has been
 * unloaded.
 *
 * \returns The unique ID for \p domain.
 */
gint32
mono_domain_get_id (MonoDomain *domain)
{
	return domain->domain_id;
}

/**
 * mono_domain_get_friendly_name:
 *
 * The returned string's lifetime is the same as \p domain's. Consider
 * copying it if you need to store it somewhere.
 *
 * \returns The friendly name of \p domain. Can be NULL if not yet set.
 */
const char *
mono_domain_get_friendly_name (MonoDomain *domain)
{
	return domain->friendly_name;
}

/**
 * mono_domain_is_unloading:
 */
gboolean
mono_domain_is_unloading (MonoDomain *domain)
{
	return FALSE;
}

/**
 * mono_domain_from_appdomain:
 */
MonoDomain *
mono_domain_from_appdomain (MonoAppDomain *appdomain_raw)
{
	return mono_get_root_domain ();
}

/**
 * mono_context_set:
 */
void
mono_context_set (MonoAppContext * new_context)
{
}

/**
 * mono_context_get:
 *
 * Returns: the current Mono Application Context.
 */
MonoAppContext *
mono_context_get (void)
{
	return NULL;
}

/**
 * mono_context_get_id:
 * \param context the context to operate on.
 *
 * Context IDs are guaranteed to be unique for the duration of a Mono
 * process; they are never reused.
 *
 * \returns The unique ID for \p context.
 */
gint32
mono_context_get_id (MonoAppContext *context)
{
	return context->context_id;
}

/**
 * mono_context_get_domain_id:
 * \param context the context to operate on.
 * \returns The ID of the domain that \p context was created in.
 */
gint32
mono_context_get_domain_id (MonoAppContext *context)
{
	return context->domain_id;
}

/**
 * mono_string_equal:
 * \param s1 First string to compare
 * \param s2 Second string to compare
 *
 * Compares two \c MonoString* instances ordinally for equality.
 *
 * \returns FALSE if the strings differ.
 */
gboolean
mono_string_equal (MonoString *s1, MonoString *s2)
{
	MONO_EXTERNAL_ONLY (gboolean, mono_string_equal_internal (s1, s2));
}

/**
 * mono_string_hash:
 * \param s the string to hash
 *
 * Compute the hash for a \c MonoString*
 * \returns the hash for the string.
 */
guint
mono_string_hash (MonoString *s)
{
	MONO_EXTERNAL_ONLY (guint, mono_string_hash_internal (s));
}

/**
 * mono_domain_create:
 *
 * Creates a new application domain, the unmanaged representation
 * of the actual domain.
 *
 * Application domains provide an isolation facilty for assemblies.   You
 * can load assemblies and execute code in them that will not be visible
 * to other application domains. This is a runtime-based virtualization
 * technology.
 *
 * It is possible to unload domains, which unloads the assemblies and
 * data that was allocated in that domain.
 *
 * When a domain is created a mempool is allocated for domain-specific
 * structures, along a dedicated code manager to hold code that is
 * associated with the domain.
 *
 * \returns New initialized \c MonoDomain, with no configuration or assemblies
 * loaded into it.
 */
MonoDomain *
mono_domain_create (void)
{
	g_assert_not_reached ();
}

/**
 * mono_domain_get_by_id:
 * \param domainid the ID
 * \returns the domain for a specific domain id.
 */
MonoDomain *
mono_domain_get_by_id (gint32 domainid)
{
	MonoDomain * domain = mono_get_root_domain ();

	if (domain->domain_id == domainid)
		return domain;
	else
		return NULL;
}

/**
 * mono_domain_assembly_open:
 * \param domain the application domain
 * \param name file name of the assembly
 */
MonoAssembly *
mono_domain_assembly_open (MonoDomain *domain, const char *name)
{
	MonoAssembly *result;
	MONO_ENTER_GC_UNSAFE;
	result = mono_domain_assembly_open_internal (mono_alc_get_default (), name);
	MONO_EXIT_GC_UNSAFE;
	return result;
}

void
mono_domain_ensure_entry_assembly (MonoDomain *domain, MonoAssembly *assembly)
{
	mono_runtime_ensure_entry_assembly (assembly);
}

/**
 * mono_domain_foreach:
 * \param func function to invoke with the domain data
 * \param user_data user-defined pointer that is passed to the supplied \p func fo reach domain
 *
 * Use this method to safely iterate over all the loaded application
 * domains in the current runtime.   The provided \p func is invoked with a
 * pointer to the \c MonoDomain and is given the value of the \p user_data
 * parameter which can be used to pass state to your called routine.
 */
void
mono_domain_foreach (MonoDomainFunc func, gpointer user_data)
{
	MONO_ENTER_GC_UNSAFE;

	func (mono_get_root_domain (), user_data);

	MONO_EXIT_GC_UNSAFE;
}

/**
 * mono_context_init:
 * \param domain The domain where the \c System.Runtime.Remoting.Context.Context is initialized
 * Initializes the \p domain's default \c System.Runtime.Remoting 's Context.
 */
void
mono_context_init (MonoDomain *domain)
{
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
		ret = mono_domain_try_type_resolve_name (NULL, name_handle, error);
	} else {
		// TODO: make this work on netcore when working on SRE.TypeBuilder
		g_assert_not_reached ();
	}

exit:
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (ret);
}

/**
 * mono_jit_info_table_find:
 * \param domain Domain that you want to look up
 * \param addr Points to an address with JITed code.
 *
 * Use this function to obtain a \c MonoJitInfo* object that can be used to get
 * some statistics. You should provide both the \p domain on which you will be
 * performing the probe, and an address. Since application domains can share code
 * the same address can be in use by multiple domains at once.
 *
 * This does not return any results for trampolines.
 *
 * \returns NULL if the address does not belong to JITed code (it might be native
 * code or a trampoline) or a valid pointer to a \c MonoJitInfo* .
 */
MonoJitInfo*
mono_jit_info_table_find (MonoDomain *domain, gpointer addr)
{
	return mono_jit_info_table_find_internal (addr, TRUE, FALSE);
}

/**
 * mono_domain_owns_vtable_slot:
 * \returns Whether \p vtable_slot is inside a vtable which belongs to \p domain.
 */
gboolean
mono_domain_owns_vtable_slot (MonoDomain *domain, gpointer vtable_slot)
{
	return mono_mem_manager_mp_contains_addr (mono_mem_manager_get_ambient (), vtable_slot);
}
