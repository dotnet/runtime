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
#include "marshal.h"
#include "object.h"

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
	MONO_EXTERNAL_ONLY (uint32_t, mono_gchandle_new_internal (obj, pinned));
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
	MONO_EXTERNAL_ONLY (uint32_t, mono_gchandle_new_weakref_internal (obj, track_resurrection));
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
	MONO_EXTERNAL_ONLY (MonoObject*, mono_gchandle_get_target_internal (gchandle));
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
	MONO_EXTERNAL_ONLY_VOID (mono_gchandle_free_internal (gchandle));
}

/* GC write barriers support */

/**
 * mono_gc_wbarrier_set_field:
 */
void
mono_gc_wbarrier_set_field (MonoObject *obj, void* field_ptr, MonoObject* value)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_set_field_internal (obj, field_ptr, value));
}

/**
 * mono_gc_wbarrier_set_arrayref:
 */
void
mono_gc_wbarrier_set_arrayref (MonoArray *arr, void* slot_ptr, MonoObject* value)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_set_arrayref_internal (arr, slot_ptr, value));
}

void
mono_gc_wbarrier_arrayref_copy (void* dest_ptr, void* src_ptr, int count)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_arrayref_copy_internal (dest_ptr, src_ptr, count));
}

/**
 * mono_gc_wbarrier_generic_store:
 */
void
mono_gc_wbarrier_generic_store (void* ptr, MonoObject* value)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_generic_store_internal (ptr, value));
}

/**
 * mono_gc_wbarrier_generic_store_atomic_internal:
 * Same as \c mono_gc_wbarrier_generic_store but performs the store
 * as an atomic operation with release semantics.
 */
void
mono_gc_wbarrier_generic_store_atomic (void *ptr, MonoObject *value)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_generic_store_atomic_internal (ptr, value));
}

void
mono_gc_wbarrier_generic_nostore (void* ptr)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_generic_nostore_internal (ptr));
}

void
mono_gc_wbarrier_value_copy (void* dest, /*const*/ void* src, int count, MonoClass *klass)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_value_copy_internal (dest, src, count, klass));
}

/**
 * mono_gc_wbarrier_object_copy:
 *
 * Write barrier to call when \p obj is the result of a clone or copy of an object.
 */
void
mono_gc_wbarrier_object_copy (MonoObject* obj, MonoObject *src)
{
	MONO_EXTERNAL_ONLY_VOID (mono_gc_wbarrier_object_copy_internal (obj, src));
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
	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_config_for_assembly_internal (assembly));
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
	if (!force && domain->state == MONO_APPDOMAIN_UNLOADED)
		return FALSE;

	MONO_EXTERNAL_ONLY_GC_UNSAFE_VOID (mono_domain_set_internal_with_options (domain, TRUE));
	return TRUE;
}
