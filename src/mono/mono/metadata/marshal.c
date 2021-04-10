/**
 * \file
 * Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 *
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include "config.h"
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include "object.h"
#include "loader.h"
#include "cil-coff.h"
#include "metadata/marshal.h"
#include "metadata/marshal-internals.h"
#include "metadata/marshal-ilgen.h"
#include "metadata/method-builder.h"
#include "metadata/method-builder-internals.h"
#include "metadata/tabledefs.h"
#include "metadata/exception.h"
#include "metadata/appdomain.h"
#include "mono/metadata/abi-details.h"
#include "mono/metadata/class-abi-details.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/metadata/cominterop.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/handle.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/abi-details.h"
#include "mono/metadata/custom-attrs-internals.h"
#include "mono/metadata/loader-internals.h"
#include "mono/metadata/jit-info.h"
#include "mono/utils/mono-counters.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/atomic.h"
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-error-internals.h>
#include <string.h>
#include <errno.h>
#include "icall-decl.h"
#include "icall-signatures.h"

static void
mono_string_utf16len_to_builder (MonoStringBuilderHandle sb, const gunichar2 *text, gsize len, MonoError *error);

/* #define DEBUG_RUNTIME_CODE */

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

/* 
 * This mutex protects the various marshalling related caches in MonoImage
 * and a few other data structures static to this file.
 *
 * The marshal lock is a non-recursive complex lock that sits below the domain lock in the
 * runtime locking latice. Which means it can take simple locks suck as the image lock.
 */
#define mono_marshal_lock() mono_locks_coop_acquire (&marshal_mutex, MarshalLock)
#define mono_marshal_unlock() mono_locks_coop_release (&marshal_mutex, MarshalLock)
static MonoCoopMutex marshal_mutex;
static gboolean marshal_mutex_initialized;

static MonoNativeTlsKey last_error_tls_id;

static MonoNativeTlsKey load_type_info_tls_id;

static gboolean use_aot_wrappers;

static int class_marshal_info_count;

static MonoMarshalCallbacks *
get_marshal_cb (void);

static void
delegate_hash_table_add (MonoDelegateHandle d);

static void
delegate_hash_table_remove (MonoDelegate *d);

/* Lazy class loading functions */
//used by marshal-ilgen.c
GENERATE_TRY_GET_CLASS_WITH_CACHE (stringbuilder, "System.Text", "StringBuilder");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (unmanaged_function_pointer_attribute, "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");

static GENERATE_TRY_GET_CLASS_WITH_CACHE (suppress_gc_transition_attribute, "System.Runtime.InteropServices", "SuppressGCTransitionAttribute")
static GENERATE_TRY_GET_CLASS_WITH_CACHE (unmanaged_callers_only_attribute, "System.Runtime.InteropServices", "UnmanagedCallersOnlyAttribute")

static gboolean type_is_blittable (MonoType *type);

static MonoImage*
get_method_image (MonoMethod *method)
{
	return m_class_get_image (method->klass);
}

// func is an identifier, that names a function, and is also in jit-icall-reg.h,
// and therefore a field in mono_jit_icall_info and can be token pasted into an enum value.
//
// The name of func must be linkable for AOT, for example g_free does not work (monoeg_g_free instead),
// nor does the C++ overload fmod (mono_fmod instead). These functions therefore
// must be extern "C".
#ifndef DISABLE_JIT
#define register_icall(func, sig, no_wrapper) \
	(mono_register_jit_icall_info (&mono_get_jit_icall_info ()->func, func, #func, (sig), (no_wrapper), #func))
#else
/* No need for the name/C symbol */
#define register_icall(func, sig, no_wrapper) \
	(mono_register_jit_icall_info (&mono_get_jit_icall_info ()->func, func, NULL, (sig), (no_wrapper), NULL))
#endif

MonoMethodSignature*
mono_signature_no_pinvoke (MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	if (sig->pinvoke) {
		sig = mono_metadata_signature_dup_full (get_method_image (method), sig);
		sig->pinvoke = FALSE;
	}
	
	return sig;
}

void
mono_marshal_init_tls (void)
{
	mono_native_tls_alloc (&last_error_tls_id, NULL);
	mono_native_tls_alloc (&load_type_info_tls_id, NULL);
}

MonoObjectHandle
mono_object_isinst_icall_impl (MonoObjectHandle obj, MonoClass* klass, MonoError *error)
{
	if (!klass)
		return NULL_HANDLE;

	/* This is called from stelemref so it is expected to succeed */
	/* Fastpath */
	if (mono_class_is_interface (klass)) {
		MonoVTable *vt = mono_handle_vtable (obj);

		if (!m_class_is_inited (klass))
			mono_class_init_internal (klass);

		if (MONO_VTABLE_IMPLEMENTS_INTERFACE (vt, m_class_get_interface_id (klass)))
			return obj;
	}

	return mono_object_handle_isinst (obj, klass, error);
}

MonoStringHandle
ves_icall_mono_string_from_utf16_impl (const gunichar2 *data, MonoError *error)
{
	MonoString *s = mono_string_from_utf16_checked (data, error);
	return_val_if_nok (error, NULL_HANDLE_STRING);
	return MONO_HANDLE_NEW (MonoString, s);
}

char*
ves_icall_mono_string_to_utf8_impl (MonoStringHandle str, MonoError *error)
{
	return mono_string_handle_to_utf8 (str, error);
}

MonoStringHandle
ves_icall_string_new_wrapper_impl (const char *text, MonoError *error)
{
	return text ? mono_string_new_handle (text, error) : NULL_HANDLE_STRING;
}

void
mono_marshal_init (void)
{
	static gboolean module_initialized = FALSE;

	if (!module_initialized) {
		module_initialized = TRUE;
		mono_coop_mutex_init_recursive (&marshal_mutex);
		marshal_mutex_initialized = TRUE;

		register_icall (mono_marshal_string_to_utf16, mono_icall_sig_ptr_obj, FALSE);
		register_icall (mono_marshal_string_to_utf16_copy, mono_icall_sig_ptr_obj, FALSE);
		register_icall (mono_string_to_utf16_internal, mono_icall_sig_ptr_obj, FALSE);
		register_icall (ves_icall_mono_string_from_utf16, mono_icall_sig_obj_ptr, FALSE);
		register_icall (mono_string_from_byvalstr, mono_icall_sig_obj_ptr_int, FALSE);
		register_icall (mono_string_from_byvalwstr, mono_icall_sig_obj_ptr_int, FALSE);
		register_icall (mono_string_from_ansibstr, mono_icall_sig_obj_ptr, FALSE);
		register_icall (mono_string_from_tbstr, mono_icall_sig_obj_ptr, FALSE);
		register_icall (mono_string_new_wrapper_internal, mono_icall_sig_obj_ptr, FALSE);
		register_icall (ves_icall_string_new_wrapper, mono_icall_sig_obj_ptr, FALSE);
		register_icall (mono_string_new_len_wrapper, mono_icall_sig_obj_ptr_int, FALSE);
		register_icall (ves_icall_mono_string_to_utf8, mono_icall_sig_ptr_obj, FALSE);
		register_icall (mono_string_to_utf8str, mono_icall_sig_ptr_obj, FALSE);
		register_icall (mono_string_to_ansibstr, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_string_to_tbstr, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_string_builder_to_utf8, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_string_builder_to_utf16, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_array_to_savearray, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_array_to_lparray, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_free_lparray, mono_icall_sig_void_object_ptr, FALSE);
		register_icall (mono_byvalarray_to_byte_array, mono_icall_sig_void_object_ptr_int32, FALSE);
		register_icall (mono_array_to_byte_byvalarray, mono_icall_sig_void_ptr_object_int32, FALSE);
		register_icall (mono_delegate_to_ftnptr, mono_icall_sig_ptr_object, FALSE);
		register_icall (mono_ftnptr_to_delegate, mono_icall_sig_object_ptr_ptr, FALSE);
		register_icall (mono_marshal_asany, mono_icall_sig_ptr_object_int32_int32, FALSE);
		register_icall (mono_marshal_free_asany, mono_icall_sig_void_object_ptr_int32_int32, FALSE);
		register_icall (ves_icall_marshal_alloc, mono_icall_sig_ptr_ptr, FALSE);
		register_icall (mono_marshal_free, mono_icall_sig_void_ptr, FALSE);
		register_icall (mono_marshal_set_last_error, mono_icall_sig_void, TRUE);
		register_icall (mono_marshal_set_last_error_windows, mono_icall_sig_void_int32, TRUE);
		register_icall (mono_marshal_clear_last_error, mono_icall_sig_void, TRUE);
		register_icall (mono_string_utf8_to_builder, mono_icall_sig_void_ptr_ptr, FALSE);
		register_icall (mono_string_utf8_to_builder2, mono_icall_sig_object_ptr, FALSE);
		register_icall (mono_string_utf16_to_builder, mono_icall_sig_void_ptr_ptr, FALSE);
		register_icall (mono_string_utf16_to_builder2, mono_icall_sig_object_ptr, FALSE);
		register_icall (mono_marshal_free_array, mono_icall_sig_void_ptr_int32, FALSE);
		register_icall (mono_string_to_byvalstr, mono_icall_sig_void_ptr_ptr_int32, FALSE);
		register_icall (mono_string_to_byvalwstr, mono_icall_sig_void_ptr_ptr_int32, FALSE);
		// Because #define g_free monoeg_g_free.
		register_icall (monoeg_g_free, mono_icall_sig_void_ptr, FALSE);
		register_icall (mono_object_isinst_icall, mono_icall_sig_object_object_ptr, TRUE);
		register_icall (mono_struct_delete_old, mono_icall_sig_void_ptr_ptr, FALSE);
		register_icall (mono_delegate_begin_invoke, mono_icall_sig_object_object_ptr, FALSE);
		register_icall (mono_delegate_end_invoke, mono_icall_sig_object_object_ptr, FALSE);
		register_icall (mono_gc_wbarrier_generic_nostore_internal, mono_icall_sig_void_ptr, FALSE);
		register_icall (mono_gchandle_get_target_internal, mono_icall_sig_object_ptr, TRUE);
		register_icall (mono_marshal_isinst_with_cache, mono_icall_sig_object_object_ptr_ptr, FALSE);
		register_icall (mono_threads_enter_gc_safe_region_unbalanced, mono_icall_sig_ptr_ptr, TRUE);
		register_icall (mono_threads_exit_gc_safe_region_unbalanced, mono_icall_sig_void_ptr_ptr, TRUE);
		register_icall (mono_threads_enter_gc_unsafe_region_unbalanced, mono_icall_sig_ptr_ptr, TRUE);
		register_icall (mono_threads_exit_gc_unsafe_region_unbalanced, mono_icall_sig_void_ptr_ptr, TRUE);
		register_icall (mono_threads_attach_coop, mono_icall_sig_ptr_ptr_ptr, TRUE);
		register_icall (mono_threads_detach_coop, mono_icall_sig_void_ptr_ptr, TRUE);
		register_icall (mono_marshal_get_type_object, mono_icall_sig_object_ptr, TRUE);
		register_icall (mono_marshal_lookup_pinvoke, mono_icall_sig_ptr_ptr, FALSE);

		mono_cominterop_init ();

		mono_counters_register ("MonoClass::class_marshal_info_count count",
								MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_marshal_info_count);
	}
}

void
mono_marshal_lock_internal (void)
{
	mono_marshal_lock ();
}

void
mono_marshal_unlock_internal (void)
{
	mono_marshal_unlock ();
}

// This is a JIT icall, it sets the pending exception (in wrapper) and return NULL on error.
gpointer
mono_delegate_to_ftnptr_impl (MonoDelegateHandle delegate, MonoError *error)
{
	gpointer result = NULL;
	MonoMethod *method, *wrapper;
	MonoClass *klass;
	MonoGCHandle target_handle = 0;

	if (MONO_HANDLE_IS_NULL (delegate))
		goto leave;

	if (MONO_HANDLE_GETVAL (delegate, delegate_trampoline)) {
		result = MONO_HANDLE_GETVAL (delegate, delegate_trampoline);
		goto leave;
	}

	klass = mono_handle_class (delegate);
	g_assert (m_class_is_delegate (klass));

	method = MONO_HANDLE_GETVAL (delegate, method);
	if (MONO_HANDLE_GETVAL (delegate, method_is_virtual)) {
		MonoObjectHandle delegate_target = MONO_HANDLE_NEW_GET (MonoObject, delegate, target);
		method = mono_object_handle_get_virtual_method (delegate_target, method, error);
		goto_if_nok (error, leave);
	}

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		gpointer ftnptr;

		ftnptr = mono_lookup_pinvoke_call_internal (method, error);
		if (!ftnptr) {
			g_assert (!is_ok (error));
			goto leave;
		}
		result = ftnptr;
		goto leave;
	}

	MonoObjectHandle delegate_target;
	delegate_target = MONO_HANDLE_NEW_GET (MonoObject, delegate, target);
	if (!MONO_HANDLE_IS_NULL (delegate_target)) {
		/* Produce a location which can be embedded in JITted code */
		target_handle = mono_gchandle_new_weakref_from_handle (delegate_target);
	}

	wrapper = mono_marshal_get_managed_wrapper (method, klass, target_handle, error);
	goto_if_nok (error, leave);

	MONO_HANDLE_SETVAL (delegate, delegate_trampoline, gpointer, mono_compile_method_checked (wrapper, error));
	goto_if_nok (error, leave);

	// Add the delegate to the delegate hash table
	delegate_hash_table_add (delegate);

	/* when the object is collected, collect the dynamic method, too */
	mono_object_register_finalizer ((MonoObject*) MONO_HANDLE_RAW (delegate));

	result = MONO_HANDLE_GETVAL (delegate, delegate_trampoline);

leave:
	if (!is_ok (error) && target_handle != 0)
		mono_gchandle_free_internal (target_handle);
	return result;
}

/* 
 * this hash table maps from a delegate trampoline object to a weak reference
 * of the delegate. As an optimizations with a non-moving GC we store the
 * object pointer itself, otherwise we use a GC handle.
 */
static GHashTable *delegate_hash_table;

static GHashTable *
delegate_hash_table_new (void) {
	return g_hash_table_new (NULL, NULL);
}

static void 
delegate_hash_table_remove (MonoDelegate *d)
{
	MonoGCHandle gchandle = NULL;

	if (!d->target)
		return;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	gchandle = (MonoGCHandle)g_hash_table_lookup (delegate_hash_table, d->delegate_trampoline);
	g_hash_table_remove (delegate_hash_table, d->delegate_trampoline);
	mono_marshal_unlock ();
	if (gchandle)
		mono_gchandle_free_internal (gchandle);
}

static void
delegate_hash_table_add (MonoDelegateHandle d)
{
	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	gpointer delegate_trampoline = MONO_HANDLE_GETVAL (d, delegate_trampoline);
	gboolean has_target = MONO_HANDLE_GETVAL (d, target) != NULL;
	if (has_target) {
		// If the delegate has an instance method there is 1 to 1 mapping between
		// the delegate object and the delegate_trampoline
		MonoGCHandle gchandle = (MonoGCHandle)g_hash_table_lookup (delegate_hash_table, delegate_trampoline);
		if (gchandle) {
			// Somehow, some other thread beat us to it ?
			g_assert (mono_gchandle_target_equal (gchandle, MONO_HANDLE_CAST (MonoObject, d)));
		} else {
			gchandle = mono_gchandle_new_weakref_from_handle (MONO_HANDLE_CAST (MonoObject, d));
			g_hash_table_insert (delegate_hash_table, delegate_trampoline, gchandle);
		}
	} else {
		if (g_hash_table_lookup (delegate_hash_table, delegate_trampoline) == NULL) {
			MonoGCHandle gchandle = mono_gchandle_from_handle (MONO_HANDLE_CAST (MonoObject, d), FALSE);
			// This delegate will always be associated with its delegate_trampoline in the table.
			// We don't free this delegate object because it is too expensive to keep track of these
			// pairs and avoid races with the delegate finalization.
			g_hash_table_insert (delegate_hash_table, delegate_trampoline, gchandle);
		}
	}
	mono_marshal_unlock ();
}

/*
 * mono_marshal_use_aot_wrappers:
 *
 *   Instructs this module to use AOT compatible wrappers.
 */
void
mono_marshal_use_aot_wrappers (gboolean use)
{
	use_aot_wrappers = use;
}

static void
parse_unmanaged_function_pointer_attr (MonoClass *klass, MonoMethodPInvoke *piinfo)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo;
	MonoReflectionUnmanagedFunctionPointerAttribute *attr;

	/* The attribute is only available in Net 2.0 */
	if (mono_class_try_get_unmanaged_function_pointer_attribute_class ()) {
		/* 
		 * The pinvoke attributes are stored in a real custom attribute so we have to
		 * construct it.
		 */
		cinfo = mono_custom_attrs_from_class_checked (klass, error);
		if (!is_ok (error)) {
			g_warning ("Could not load UnmanagedFunctionPointerAttribute due to %s", mono_error_get_message (error));
			mono_error_cleanup (error);
		}
		if (cinfo && !mono_runtime_get_no_exec ()) {
			attr = (MonoReflectionUnmanagedFunctionPointerAttribute*)mono_custom_attrs_get_attr_checked (cinfo, mono_class_try_get_unmanaged_function_pointer_attribute_class (), error);
			if (attr) {
				piinfo->piflags = (attr->call_conv << 8) | (attr->charset ? (attr->charset - 1) * 2 : 1) | attr->set_last_error;
			} else {
				if (!is_ok (error)) {
					g_warning ("Could not load UnmanagedFunctionPointerAttribute due to %s", mono_error_get_message (error));
					mono_error_cleanup (error);
				}
			}
			if (!cinfo->cached)
				mono_custom_attrs_free (cinfo);
		}
	}
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error */
MonoDelegateHandle
mono_ftnptr_to_delegate_impl (MonoClass *klass, gpointer ftn, MonoError *error)
{
	MonoGCHandle gchandle;
	MonoDelegateHandle d = MONO_HANDLE_NEW (MonoDelegate, NULL);

	if (ftn == NULL)
		goto leave;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	gchandle = (MonoGCHandle)g_hash_table_lookup (delegate_hash_table, ftn);
	mono_marshal_unlock ();
	if (gchandle)
		MONO_HANDLE_ASSIGN (d, MONO_HANDLE_CAST (MonoDelegate, mono_gchandle_get_target_handle (gchandle)));

	if (MONO_HANDLE_IS_NULL (d)) {
		/* This is a native function, so construct a delegate for it */
		MonoMethodSignature *sig;
		MonoMethod *wrapper;
		MonoMarshalSpec **mspecs;
		MonoMethod *invoke = mono_get_delegate_invoke_internal (klass);
		MonoMethodPInvoke piinfo;
		MonoObjectHandle  this_obj;
		int i;

		if (use_aot_wrappers) {
			wrapper = mono_marshal_get_native_func_wrapper_aot (klass);
			this_obj = MONO_HANDLE_NEW (MonoObject, mono_value_box_checked (mono_defaults.int_class, &ftn, error));
			goto_if_nok (error, leave);
		} else {
			memset (&piinfo, 0, sizeof (piinfo));
			parse_unmanaged_function_pointer_attr (klass, &piinfo);

			mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature_internal (invoke)->param_count + 1);
			mono_method_get_marshal_info (invoke, mspecs);
			/* Freed below so don't alloc from mempool */
			sig = mono_metadata_signature_dup (mono_method_signature_internal (invoke));
			sig->hasthis = 0;

			wrapper = mono_marshal_get_native_func_wrapper (m_class_get_image (klass), sig, &piinfo, mspecs, ftn);
			this_obj = MONO_HANDLE_NEW (MonoObject, NULL);

			for (i = mono_method_signature_internal (invoke)->param_count; i >= 0; i--)
				if (mspecs [i])
					mono_metadata_free_marshal_spec (mspecs [i]);
			g_free (mspecs);
			g_free (sig);
		}

		MONO_HANDLE_ASSIGN (d, mono_object_new_handle (klass, error));
		goto_if_nok (error, leave);
		gpointer compiled_ptr = mono_compile_method_checked (wrapper, error);
		goto_if_nok (error, leave);

		mono_delegate_ctor (MONO_HANDLE_CAST (MonoObject, d), this_obj, compiled_ptr, wrapper, error);
		goto_if_nok (error, leave);
	}

	g_assert (!MONO_HANDLE_IS_NULL (d));
	if (MONO_HANDLE_DOMAIN (d) != mono_domain_get ())
		mono_error_set_not_supported (error, "Delegates cannot be marshalled from native code into a domain other than their home domain");
leave:
	return d;
}

void
mono_delegate_free_ftnptr (MonoDelegate *delegate)
{
	MonoJitInfo *ji;
	void *ptr;

	delegate_hash_table_remove (delegate);

	ptr = (gpointer)mono_atomic_xchg_ptr (&delegate->delegate_trampoline, NULL);

	if (!delegate->target) {
		/* The wrapper method is shared between delegates -> no need to free it */
		return;
	}

	if (ptr) {
		MonoGCHandle gchandle;
		void **method_data;
		MonoMethod *method;

		ji = mono_jit_info_table_find_internal (mono_get_addr_from_ftnptr (ptr), TRUE, FALSE);
		/* FIXME we leak wrapper with the interpreter */
		if (!ji)
			return;

		method = mono_jit_info_get_method (ji);
		method_data = (void **)((MonoMethodWrapper*)method)->method_data;

		/*the target gchandle is the first entry after size and the wrapper itself.*/
		gchandle = (MonoGCHandle)method_data [2];

		if (gchandle)
			mono_gchandle_free_internal (gchandle);

		mono_runtime_free_method (method);
	}
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error */
MonoStringHandle
mono_string_from_byvalstr_impl (const char *data, int max_len, MonoError *error)
{
	// FIXME This optimization ok to miss before wrapper? Or null is rare?
	if (!data)
		return NULL_HANDLE_STRING;

	int len = 0;
	while (len < max_len - 1 && data [len])
		len++;

	// FIXMEcoop
	MonoString *s = mono_string_new_len_checked (data, len, error);
	return_val_if_nok (error, NULL_HANDLE_STRING);
	return MONO_HANDLE_NEW (MonoString, s);
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and return NULL on error */
MonoStringHandle
mono_string_from_byvalwstr_impl (const gunichar2 *data, int max_len, MonoError *error)
{
	// FIXME This optimization ok to miss before wrapper? Or null is rare?
	if (!data)
		return NULL_HANDLE_STRING;

	// FIXME Check max_len while scanning data? mono_string_from_byvalstr does.
	const int len = g_utf16_len (data);

	return mono_string_new_utf16_handle (data, MIN (len, max_len), error);
}

gpointer
mono_array_to_savearray_impl (MonoArrayHandle array, MonoError *error)
{
	if (!MONO_HANDLE_BOOL (array))
		return NULL;

	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_array_to_lparray_impl (MonoArrayHandle array_handle, MonoError *error)
{
	if (!MONO_HANDLE_BOOL (array_handle))
		return NULL;

	MonoArray *array = MONO_HANDLE_RAW (array_handle); // FIXMEcoop

#ifndef DISABLE_COM
	gpointer *nativeArray = NULL;
	int nativeArraySize = 0;
	int i = 0;
	MonoClass *klass = array->obj.vtable->klass;
	MonoClass *klass_element_class = m_class_get_element_class (klass);

	switch (m_class_get_byval_arg (klass_element_class)->type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_CLASS:
		nativeArraySize = array->max_length;
		nativeArray = g_new (gpointer, nativeArraySize);
		for (i = 0; i < nativeArraySize; ++i) {
			nativeArray [i] = mono_cominterop_get_com_interface (((MonoObject **)array->vector)[i], klass_element_class, error);
			if (!is_ok (error)) {
				// FIXME? Returns uninitialized.
				break;
			}
		}
		return nativeArray;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I1:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_PTR:
		/* nothing to do */
		break;
	case MONO_TYPE_GENERICINST:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY: 
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_STRING:
	default:
		g_warning ("type 0x%x not handled", m_class_get_byval_arg (klass_element_class)->type);
		g_assert_not_reached ();
	}
#endif
	return array->vector;
}

void
mono_free_lparray_impl (MonoArrayHandle array, gpointer* nativeArray, MonoError *error)
{
#ifndef DISABLE_COM
	if (!nativeArray || MONO_HANDLE_IS_NULL (array))
		return;

	MonoClass * const klass = mono_handle_class (array);

	if (m_class_get_byval_arg (m_class_get_element_class (klass))->type == MONO_TYPE_CLASS)
		g_free (nativeArray);
#endif
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns on error */
void
mono_byvalarray_to_byte_array_impl (MonoArrayHandle arr, const char *native_arr, guint32 elnum, MonoError *error)
{
	g_assert (m_class_get_element_class (mono_handle_class (arr)) == mono_defaults.char_class);

	GError *gerror = NULL;
	glong items_written;
	gunichar2 *ut = g_utf8_to_utf16 (native_arr, elnum, NULL, &items_written, &gerror);
	if (gerror) {
		// FIXME set error?
		g_error_free (gerror);
		return;
	}
	MonoGCHandle gchandle = NULL;
	memcpy (MONO_ARRAY_HANDLE_PIN (arr, gunichar2, 0, &gchandle), ut, items_written * sizeof (gunichar2));
	mono_gchandle_free_internal (gchandle);
	g_free (ut);
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns on error */
void
mono_array_to_byte_byvalarray_impl (gpointer native_arr, MonoArrayHandle arr, guint32 elnum, MonoError *error)
{
	g_assert (m_class_get_element_class (mono_handle_class (arr)) == mono_defaults.char_class);

	GError *gerror = NULL;

	MonoGCHandle gchandle = NULL;
	char *as = g_utf16_to_utf8 (MONO_ARRAY_HANDLE_PIN (arr, gunichar2, 0, &gchandle), mono_array_handle_length (arr), NULL, NULL, &gerror);
	mono_gchandle_free_internal (gchandle);
	if (gerror) {
		mono_error_set_argument (error, "string", gerror->message);
		g_error_free (gerror);
		return;
	}
	memcpy (native_arr, as, MIN (strlen (as), elnum));
	g_free (as);
}

static MonoStringBuilderHandle
mono_string_builder_new (int starting_string_length, MonoError *error)
{
	static MonoClass *string_builder_class;
	static MonoMethod *sb_ctor;
	void *args [1];

	int initial_len = starting_string_length;

	if (initial_len < 0)
		initial_len = 0;

	if (!sb_ctor) {
		MonoMethodDesc *desc;
		MonoMethod *m;

		string_builder_class = mono_class_try_get_stringbuilder_class ();
		g_assert (string_builder_class); //TODO don't swallow the error
		desc = mono_method_desc_new (":.ctor(int)", FALSE);
		m = mono_method_desc_search_in_class (desc, string_builder_class);
		g_assert (m);
		mono_method_desc_free (desc);
		mono_memory_barrier ();
		sb_ctor = m;
	}

	// We make a new array in the _to_builder function, so this
	// array will always be garbage collected.
	args [0] = &initial_len;

	MonoStringBuilderHandle sb = MONO_HANDLE_CAST (MonoStringBuilder, mono_object_new_handle (string_builder_class, error));
	mono_error_assert_ok (error);

	mono_runtime_try_invoke_handle (sb_ctor, MONO_HANDLE_CAST (MonoObject, sb), args, error);
	mono_error_assert_ok (error);

	MonoArrayHandle chunkChars = MONO_HANDLE_NEW_GET (MonoArray, sb, chunkChars);
	g_assert (MONO_HANDLE_GETVAL (chunkChars, max_length) >= initial_len);

	return sb;
}

static void
mono_string_utf16_to_builder_copy (MonoStringBuilderHandle sb, const gunichar2 *text, size_t string_len, MonoError *error)
{
	MonoArrayHandle chunkChars = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoStringBuilderHandle chunk = MONO_HANDLE_NEW (MonoStringBuilder, MONO_HANDLE_RAW (sb));

	guint capacity = mono_string_builder_capacity (sb);

	g_assert (capacity >= string_len);

	MONO_ENTER_NO_SAFEPOINTS;

	do {
		MONO_HANDLE_GET (chunkChars, chunk, chunkChars);
		const int maxLength = MONO_HANDLE_GETVAL (chunkChars, max_length);
		g_assert (maxLength >= 0);
		const int chunkOffset = MONO_HANDLE_GETVAL (chunk, chunkOffset);
		g_assert (chunkOffset >= 0);
		if (maxLength > 0 && chunkOffset < string_len) {
			// Check that we will not overrun our boundaries.
			int charsToCopy = MIN (string_len - chunkOffset, maxLength);
			memcpy (MONO_HANDLE_RAW (chunkChars)->vector, text + chunkOffset, charsToCopy * sizeof (gunichar2));
			MONO_HANDLE_SETVAL (chunk, chunkLength, int, charsToCopy);
		} else {
			MONO_HANDLE_SETVAL (chunk, chunkLength, int, 0);
		}
		MONO_HANDLE_GET (chunk, chunk, chunkPrevious);
	} while (MONO_HANDLE_BOOL (chunk));

	MONO_EXIT_NO_SAFEPOINTS;
}

MonoStringBuilderHandle
mono_string_utf16_to_builder2_impl (const gunichar2 *text, MonoError *error)
{
	if (!text)
		return NULL_HANDLE_STRING_BUILDER;

	const gsize len = g_utf16_len (text);

	MonoStringBuilderHandle sb = mono_string_builder_new (len, error);
	return_val_if_nok (error, NULL_HANDLE_STRING_BUILDER);

	mono_string_utf16len_to_builder (sb, text, len, error);
	return_val_if_nok (error, NULL_HANDLE_STRING_BUILDER);

	return sb;
}

static void
mono_string_utf8len_to_builder (MonoStringBuilderHandle sb, const char *text, gsize len, MonoError *error)
{
	if (!MONO_HANDLE_BOOL (sb) || !text)
		return;

	GError *gerror = NULL;
	glong copied;
	gunichar2* ut = g_utf8_to_utf16 (text, len, NULL, &copied, &gerror);
	int capacity = mono_string_builder_capacity (sb);

	if (copied > capacity)
		copied = capacity;

	if (!gerror) {
		MONO_HANDLE_SETRAW (sb, chunkPrevious, NULL);
		mono_string_utf16_to_builder_copy (sb, ut, copied, error);
	} else {
		// FIXME? Set error?
		g_error_free (gerror);
	}

	g_free (ut);
}

void
mono_string_utf8_to_builder_impl (MonoStringBuilderHandle sb, const char *text, MonoError *error)
{
	mono_string_utf8len_to_builder (sb, text, text ? strlen (text) : 0, error);
}

MonoStringBuilderHandle
mono_string_utf8_to_builder2_impl (const char *text, MonoError *error)
{
	if (!text)
		return NULL_HANDLE_STRING_BUILDER;

	const gsize len = strlen (text);

	MonoStringBuilderHandle sb = mono_string_builder_new (len, error);
	return_val_if_nok (error, NULL_HANDLE_STRING_BUILDER);

	mono_string_utf8len_to_builder (sb, text, len, error);
	return_val_if_nok (error, NULL_HANDLE_STRING_BUILDER);

	return sb;
}

static void
mono_string_utf16len_to_builder (MonoStringBuilderHandle sb, const gunichar2 *text, gsize len, MonoError *error)
{
	if (!MONO_HANDLE_BOOL (sb) || !text)
		return;
	len = MIN (len, mono_string_builder_capacity (sb));
	mono_string_utf16_to_builder_copy (sb, text, len, error);
}

void
mono_string_utf16_to_builder_impl (MonoStringBuilderHandle sb, const gunichar2 *text, MonoError *error)
{
	mono_string_utf16len_to_builder (sb, text, text ? g_utf16_len (text) : 0, error);
}

/**
 * mono_string_builder_to_utf8:
 * \param sb the string builder
 *
 * Converts to utf8 the contents of the \c MonoStringBuilder .
 *
 * \returns a utf8 string with the contents of the \c StringBuilder .
 *
 * The return value must be released with mono_marshal_free.
 *
 * This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error.
 */
gchar*
mono_string_builder_to_utf8_impl (MonoStringBuilderHandle sb, MonoError *error)
{
	char *res = NULL;
	GError *gerror = NULL;
	char *tmp = NULL;
	gunichar2 *str_utf16 = NULL;
	glong byte_count;
	guint len = 0;

	if (!MONO_HANDLE_BOOL (sb))
		goto exit;

	str_utf16 = mono_string_builder_to_utf16_impl (sb, error);
	goto_if_nok (error, exit);

	tmp = g_utf16_to_utf8 (str_utf16, mono_string_builder_string_length (sb), NULL, &byte_count, &gerror);
	if (gerror) {
		mono_error_set_execution_engine (error, "Failed to convert StringBuilder from utf16 to utf8");
		goto exit;
	}

	len = mono_string_builder_capacity (sb) + 1;
	res = (char *)mono_marshal_alloc (MAX (byte_count + 1, len), error);
	if (!is_ok (error)) {
		res = NULL;
		goto exit;
	}

	memcpy (res, tmp, byte_count);
	res [byte_count] = 0;
exit:
	g_error_free (gerror);
	mono_marshal_free (str_utf16);
	g_free (tmp);
	return res;
}

/**
 * mono_string_builder_to_utf16:
 * \param sb the string builder
 *
 * Converts to utf16 the contents of the \c MonoStringBuilder .
 *
 * Returns: a utf16 string with the contents of the \c StringBuilder .
 *
 * The return value must be released with mono_marshal_free.
 *
 * This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error.
 */
gunichar2*
mono_string_builder_to_utf16_impl (MonoStringBuilderHandle sb, MonoError *error)
{
	if (!MONO_HANDLE_BOOL (sb))
		return NULL;

	g_assert (MONO_HANDLE_GET_BOOL (sb, chunkChars));

	guint capacity = mono_string_builder_capacity (sb);
	guint length = mono_string_builder_string_length (sb);

	// Follow CoreCLR and double NULL terminate the buffer so we have more protection
	// against native code putting garbage in there.

	gunichar2 *str = (gunichar2 *)mono_marshal_alloc ((capacity + 2) * sizeof (gunichar2), error);
	return_val_if_nok (error, NULL);

	str [capacity] = 0;
	str [capacity + 1] = 0;

	MonoArrayHandle chunkChars = MONO_HANDLE_NEW (MonoArray, NULL);
	MonoStringBuilderHandle chunk = MONO_HANDLE_NEW (MonoStringBuilder, MONO_HANDLE_RAW (sb));

	MONO_ENTER_NO_SAFEPOINTS;

	do {
		const int chunkLength = MONO_HANDLE_GETVAL (chunk, chunkLength);
		g_assert (chunkLength >= 0);
		if (chunkLength > 0) {
			// Check that we will not overrun our boundaries.
			MONO_HANDLE_GET (chunkChars, chunk, chunkChars);
			const int chunkOffset = MONO_HANDLE_GETVAL (chunk, chunkOffset);
			g_assert (chunkOffset >= 0);
			g_assertf ((chunkOffset + chunkLength) >= chunkLength, "integer overflow");
			g_assertf ((chunkOffset + chunkLength) <= capacity, "A chunk in the StringBuilder had a length longer than expected from the offset.");
			memcpy (str + chunkOffset, MONO_HANDLE_RAW (chunkChars)->vector, chunkLength * sizeof (gunichar2));
		}
		MONO_HANDLE_GET (chunk, chunk, chunkPrevious);
	} while (MONO_HANDLE_BOOL (chunk));

	str [length] = 0;

	MONO_EXIT_NO_SAFEPOINTS;

	return str;
}

#ifndef HOST_WIN32

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
gpointer
mono_string_to_utf8str_impl (MonoStringHandle s, MonoError *error)
{
	return mono_string_handle_to_utf8 (s, error);
}

#endif

// Assume ANSI == UTF8 for now, same as LPSTR

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
char *
mono_string_to_ansibstr_impl (MonoStringHandle string_obj, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (string_obj))
		return NULL;

	char *utf8_str = mono_string_handle_to_utf8 (string_obj, error);
	return_val_if_nok (error, NULL);
	char *res = mono_ptr_to_ansibstr (utf8_str, g_utf8_strlen (utf8_str, -1));
	g_free (utf8_str);
	return res;
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
MonoStringHandle
mono_string_from_ansibstr_impl (const char *data, MonoError *error)
{
	if (!data)
		return NULL_HANDLE_STRING;

	return mono_string_new_utf8_len (data, *((guint32 *)data - 1) / sizeof (char), error);
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
gpointer
mono_string_to_tbstr_impl (MonoStringHandle string_obj, MonoError *error)
{
#ifdef TARGET_WIN32
	return mono_string_to_bstr_impl (string_obj, error);
#else
	return mono_string_to_ansibstr_impl (string_obj, error);
#endif
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
MonoStringHandle
mono_string_from_tbstr_impl (gpointer data, MonoError *error)
{
#ifdef TARGET_WIN32
	return mono_string_from_bstr_checked ((mono_bstr)data, error);
#else
	return mono_string_from_ansibstr_impl ((char *)data, error);
#endif
}

/**
 * mono_string_to_byvalstr:
 * \param dst Where to store the null-terminated utf8 decoded string.
 * \param src the \c MonoString to copy.
 * \param size the maximum number of bytes to copy.
 *
 * Copies the \c MonoString pointed to by \p src as a utf8 string
 * into \p dst, it copies at most \p size bytes into the destination.
 */
void
mono_string_to_byvalstr_impl (char *dst, MonoStringHandle src, int size, MonoError *error)
{
	g_assert (dst != NULL);
	g_assert (size > 0);

	memset (dst, 0, size);
	if (!MONO_HANDLE_BOOL (src))
		return;

	// FIXME convert right into dst instead of the double copy.

	char *s = mono_string_handle_to_utf8 (src, error);
	return_if_nok (error);
	int len = MIN (size, strlen (s));
	len -= (len >= size);
	memcpy (dst, s, len);
	dst [len] = 0;
	g_free (s);
}

/**
 * mono_string_to_byvalwstr:
 * \param dst Where to store the null-terminated utf16 decoded string.
 * \param src the \c MonoString to copy.
 * \param size the maximum number of wide characters to copy (each consumes 2 bytes)
 *
 * Copies the \c MonoString pointed to by \p src as a utf16 string into
 * \p dst, it copies at most \p size gunichar2s into the destination (including
 * a terminating 16-bit zero terminator).
 */
void
mono_string_to_byvalwstr_impl (gunichar2 *dst, MonoStringHandle src, int size, MonoError *error)
{
	g_assert (dst);
	g_assert (size > 0);

	if (!MONO_HANDLE_BOOL (src)) {
		memset (dst, 0, size * sizeof (gunichar2));
		return;
	}

	MonoGCHandle gchandle = NULL;
	int len = MIN (size, mono_string_handle_length (src));
	memcpy (dst, mono_string_handle_pin_chars (src, &gchandle), len * sizeof (gunichar2));
	mono_gchandle_free_internal (gchandle);
	len -= (size <= mono_string_handle_length (src));
	dst [len] = 0;
}

/* this is an icall, it sets the pending exception and returns NULL on error */
MonoStringHandle
mono_string_new_len_wrapper_impl (const char *text, guint length, MonoError *error)
{
	MonoString *s = mono_string_new_len_checked (text, length, error);
	return_val_if_nok (error, NULL_HANDLE_STRING);
	return MONO_HANDLE_NEW (MonoString, s);
}

guint
mono_type_to_ldind (MonoType *type)
{
	if (type->byref)
		return CEE_LDIND_I;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
		return CEE_LDIND_I1;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return CEE_LDIND_U1;
	case MONO_TYPE_I2:
		return CEE_LDIND_I2;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return CEE_LDIND_U2;
	case MONO_TYPE_I4:
		return CEE_LDIND_I4;
	case MONO_TYPE_U4:
		return CEE_LDIND_U4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return CEE_LDIND_I;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return CEE_LDIND_REF;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return CEE_LDIND_I8;
	case MONO_TYPE_R4:
		return CEE_LDIND_R4;
	case MONO_TYPE_R8:
		return CEE_LDIND_R8;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			goto handle_enum;
		}
		return CEE_LDOBJ;
	case MONO_TYPE_TYPEDBYREF:
		return CEE_LDOBJ;
	case MONO_TYPE_GENERICINST:
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in type_to_ldind", type->type);
	}
	return -1;
}

guint
mono_type_to_stind (MonoType *type)
{
	if (type->byref)
		return MONO_TYPE_IS_REFERENCE (type) ? CEE_STIND_REF : CEE_STIND_I;

handle_enum:
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		return CEE_STIND_I1;
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		return CEE_STIND_I2;
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		return CEE_STIND_I4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		return CEE_STIND_I;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:    
		return CEE_STIND_REF;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		return CEE_STIND_I8;
	case MONO_TYPE_R4:
		return CEE_STIND_R4;
	case MONO_TYPE_R8:
		return CEE_STIND_R8;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (type->data.klass)) {
			type = mono_class_enum_basetype_internal (type->data.klass);
			goto handle_enum;
		}
		return CEE_STOBJ;
	case MONO_TYPE_TYPEDBYREF:
		return CEE_STOBJ;
	case MONO_TYPE_GENERICINST:
		type = m_class_get_byval_arg (type->data.generic_class->container_class);
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in type_to_stind", type->type);
	}
	return -1;
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params)
{
	mono_set_pending_exception (mono_exception_from_name (mono_defaults.corlib, "System", "PlatformNotSupportedException"));
	return NULL;
}

static char*
mono_signature_to_name (MonoMethodSignature *sig, const char *prefix)
{
	GString *res = g_string_new ("");

	if (prefix) {
		g_string_append (res, prefix);
		g_string_append_c (res, '_');
	}

	mono_type_get_desc (res, sig->ret, FALSE);

	if (sig->hasthis)
		g_string_append (res, "__this__");

	for (int i = 0; i < sig->param_count; ++i) {
		g_string_append_c (res, '_');
		mono_type_get_desc (res, sig->params [i], FALSE);
	}
	char *result = res->str;
	g_string_free (res, FALSE);
	return result;
}

/**
 * mono_marshal_get_string_encoding:
 *
 *  Return the string encoding which should be used for a given parameter.
 */
MonoMarshalNative
mono_marshal_get_string_encoding (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	/* First try the parameter marshal info */
	if (spec) {
		if (spec->native == MONO_NATIVE_LPARRAY) {
			if ((spec->data.array_data.elem_type != 0) && (spec->data.array_data.elem_type != MONO_NATIVE_MAX))
				return spec->data.array_data.elem_type;
		}
		else
			return spec->native;
	}

	if (!piinfo)
		return MONO_NATIVE_LPSTR;

	/* Then try the method level marshal info */
	switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
	case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
		return MONO_NATIVE_LPSTR;
	case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
		return MONO_NATIVE_LPWSTR;
	case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
#ifdef TARGET_WIN32
		return MONO_NATIVE_LPWSTR;
#else
		return MONO_NATIVE_LPSTR;
#endif
	default:
		return MONO_NATIVE_LPSTR;
	}
}

MonoMarshalConv
mono_marshal_get_string_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		return MONO_MARSHAL_CONV_STR_LPWSTR;
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_VBBYREFSTR:
		return MONO_MARSHAL_CONV_STR_LPSTR;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_STR_LPTSTR;
	case MONO_NATIVE_BSTR:
		return MONO_MARSHAL_CONV_STR_BSTR;
	case MONO_NATIVE_UTF8STR:
		return MONO_MARSHAL_CONV_STR_UTF8STR;
	case MONO_NATIVE_ANSIBSTR:
		return MONO_MARSHAL_CONV_STR_ANSIBSTR;
	case MONO_NATIVE_TBSTR:
		return MONO_MARSHAL_CONV_STR_TBSTR;
	default:
		return MONO_MARSHAL_CONV_INVALID;
	}
}

MonoMarshalConv
mono_marshal_get_stringbuilder_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		return MONO_MARSHAL_CONV_SB_LPWSTR;
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_SB_LPSTR;
	case MONO_NATIVE_UTF8STR:
		return MONO_MARSHAL_CONV_SB_UTF8STR;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_SB_LPTSTR;
	default:
		return MONO_MARSHAL_CONV_INVALID;
	}
}

MonoMarshalConv
mono_marshal_get_ptr_to_string_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec, gboolean *need_free)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	*need_free = TRUE;

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		*need_free = FALSE;
		return MONO_MARSHAL_CONV_LPWSTR_STR;
	case MONO_NATIVE_UTF8STR:
		return MONO_MARSHAL_CONV_UTF8STR_STR;
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_VBBYREFSTR:
		return MONO_MARSHAL_CONV_LPSTR_STR;
	case MONO_NATIVE_LPTSTR:
#ifdef TARGET_WIN32
		*need_free = FALSE;
#endif
		return MONO_MARSHAL_CONV_LPTSTR_STR;
	case MONO_NATIVE_BSTR:
		return MONO_MARSHAL_CONV_BSTR_STR;
	case MONO_NATIVE_ANSIBSTR:
		return MONO_MARSHAL_CONV_ANSIBSTR_STR;
	case MONO_NATIVE_TBSTR:
		return MONO_MARSHAL_CONV_TBSTR_STR;
	default:
		return MONO_MARSHAL_CONV_INVALID;
	}
}

MonoMarshalConv
mono_marshal_get_ptr_to_stringbuilder_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec, gboolean *need_free)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	*need_free = TRUE;

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		return MONO_MARSHAL_CONV_LPWSTR_SB;
	case MONO_NATIVE_UTF8STR:
		return MONO_MARSHAL_CONV_UTF8STR_SB;
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_LPSTR_SB;
		break;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_LPTSTR_SB;
		break;
	default:
		return MONO_MARSHAL_CONV_INVALID;
	}
}

/*
 * Return whenever a field of a native structure or an array member needs to 
 * be freed.
 */
gboolean
mono_marshal_need_free (MonoType *t, MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding;

	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		/* FIXME: Optimize this */
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
		if (t->data.klass == mono_class_try_get_stringbuilder_class ()) {
			gboolean need_free;
			mono_marshal_get_ptr_to_stringbuilder_conv (piinfo, spec, &need_free);
			return need_free;
		}
		return FALSE;
	case MONO_TYPE_STRING:
		encoding = mono_marshal_get_string_encoding (piinfo, spec);
		return (encoding == MONO_NATIVE_LPWSTR) ? FALSE : TRUE;
	default:
		return FALSE;
	}
}

/*
 * Return the hash table pointed to by VAR, lazily creating it if neccesary.
 */
static GHashTable*
get_cache (GHashTable **var, GHashFunc hash_func, GCompareFunc equal_func)
{
	if (!(*var)) {
		mono_marshal_lock ();
		if (!(*var)) {
			GHashTable *cache = 
				g_hash_table_new (hash_func, equal_func);
			mono_memory_barrier ();
			*var = cache;
		}
		mono_marshal_unlock ();
	}
	return *var;
}

GHashTable*
mono_marshal_get_cache (GHashTable **var, GHashFunc hash_func, GCompareFunc equal_func)
{
	return get_cache (var, hash_func, equal_func);
}

MonoMethod*
mono_marshal_find_in_cache (GHashTable *cache, gpointer key)
{
	MonoMethod *res;

	mono_marshal_lock ();
	res = (MonoMethod *)g_hash_table_lookup (cache, key);
	mono_marshal_unlock ();
	return res;
}

/*
 * mono_mb_create:
 *
 *   Create a MonoMethod from MB, set INFO as wrapper info.
 */
MonoMethod*
mono_mb_create (MonoMethodBuilder *mb, MonoMethodSignature *sig,
				int max_stack, WrapperInfo *info)
{
	MonoMethod *res;

	res = mono_mb_create_method (mb, sig, max_stack);
	if (info)
		mono_marshal_set_wrapper_info (res, info);
	return res;
}

/* Create the method from the builder and place it in the cache */
MonoMethod*
mono_mb_create_and_cache_full (GHashTable *cache, gpointer key,
							   MonoMethodBuilder *mb, MonoMethodSignature *sig,
							   int max_stack, WrapperInfo *info, gboolean *out_found)
{
	MonoMethod *res;

	if (out_found)
		*out_found = FALSE;

	mono_marshal_lock ();
	res = (MonoMethod *)g_hash_table_lookup (cache, key);
	mono_marshal_unlock ();
	if (!res) {
		MonoMethod *newm;
		newm = mono_mb_create_method (mb, sig, max_stack);
		mono_marshal_lock ();
		res = (MonoMethod *)g_hash_table_lookup (cache, key);
		if (!res) {
			res = newm;
			g_hash_table_insert (cache, key, res);
			mono_marshal_set_wrapper_info (res, info);
			mono_marshal_unlock ();
		} else {
			if (out_found)
				*out_found = TRUE;
			mono_marshal_unlock ();
			mono_free_method (newm);
		}
	}

	return res;
}		

MonoMethod*
mono_mb_create_and_cache (GHashTable *cache, gpointer key,
							   MonoMethodBuilder *mb, MonoMethodSignature *sig,
							   int max_stack)
{
	return mono_mb_create_and_cache_full (cache, key, mb, sig, max_stack, NULL, NULL);
}

/**
 * mono_marshal_method_from_wrapper:
 */
MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper)
{
	MonoMethod *m;
	int wrapper_type = wrapper->wrapper_type;
	WrapperInfo *info;

	if (wrapper_type == MONO_WRAPPER_NONE || wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
		return wrapper;

	info = mono_marshal_get_wrapper_info (wrapper);

	switch (wrapper_type) {
	case MONO_WRAPPER_SYNCHRONIZED:
		m = info->d.synchronized.method;
		if (wrapper->is_inflated) {
			ERROR_DECL (error);
			MonoMethod *result;
			result = mono_class_inflate_generic_method_checked (m, mono_method_get_context (wrapper), error);
			g_assert (is_ok (error)); /* FIXME don't swallow the error */
			return result;
		}
		return m;
	case MONO_WRAPPER_UNBOX:
		return info->d.unbox.method;
	case MONO_WRAPPER_MANAGED_TO_NATIVE:
		if (info && (info->subtype == WRAPPER_SUBTYPE_NONE || info->subtype == WRAPPER_SUBTYPE_NATIVE_FUNC_AOT || info->subtype == WRAPPER_SUBTYPE_PINVOKE))
			return info->d.managed_to_native.method;
		else
			return NULL;
	case MONO_WRAPPER_RUNTIME_INVOKE:
		if (info && (info->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT || info->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL))
			return info->d.runtime_invoke.method;
		else
			return NULL;
	case MONO_WRAPPER_DELEGATE_INVOKE:
		if (info)
			return info->d.delegate_invoke.method;
		else
			return NULL;
	default:
		return NULL;
	}
}

/*
 * mono_marshal_get_wrapper_info:
 *
 *   Retrieve the WrapperInfo structure associated with WRAPPER.
 */
WrapperInfo*
mono_marshal_get_wrapper_info (MonoMethod *wrapper)
{
	g_assert (wrapper->wrapper_type);

	return (WrapperInfo *)mono_method_get_wrapper_data (wrapper, 1);
}

/*
 * mono_marshal_set_wrapper_info:
 *
 *   Set the WrapperInfo structure associated with the wrapper
 * method METHOD to INFO.
 */
void
mono_marshal_set_wrapper_info (MonoMethod *method, WrapperInfo *info)
{
	void **datav;
	/* assert */
	if (method->wrapper_type == MONO_WRAPPER_NONE || method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
		return;

	datav = (void **)((MonoMethodWrapper *)method)->method_data;
	datav [1] = info;
}

WrapperInfo*
mono_wrapper_info_create (MonoMethodBuilder *mb, WrapperSubtype subtype)
{
	WrapperInfo *info;

	info = (WrapperInfo *)mono_image_alloc0 (get_method_image (mb->method), sizeof (WrapperInfo));
	info->subtype = subtype;
	return info;
}

/*
 * get_wrapper_target_class:
 *
 *   Return the class where a wrapper method should be placed.
 */
static MonoClass*
get_wrapper_target_class (MonoImage *image)
{
	ERROR_DECL (error);
	MonoClass *klass;

	/*
	 * Notes:
	 * - can't put all wrappers into an mscorlib class, because they reference
	 *   metadata (signature) so they should be put into the same image as the 
	 *   method they wrap, so they are unloaded together.
	 * - putting them into a class with a type initalizer could cause the 
	 *   initializer to be executed which can be a problem if the wrappers are 
	 *   shared.
	 * - putting them into an inflated class can cause problems if the the 
	 *   class is deleted because it references an image which is unloaded.
	 * To avoid these problems, we put the wrappers into the <Module> class of 
	 * the image.
	 */
	if (image_is_dynamic (image)) {
		klass = ((MonoDynamicImage*)image)->wrappers_type;
	} else {
		klass = mono_class_get_checked (image, mono_metadata_make_token (MONO_TABLE_TYPEDEF, 1), error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
	}
	g_assert (klass);

	return klass;
}

/*
 * Wrappers for generic methods should be instances of generic wrapper methods, i.e .the wrapper for Sort<int> should be
 * an instance of the wrapper for Sort<T>. This is required for full-aot to work.
 */

/*
 * check_generic_wrapper_cache:
 *
 *   Check CACHE for the wrapper of the generic instance ORIG_METHOD, and return it if it is found.
 * KEY should be the key for ORIG_METHOD in the cache, while DEF_KEY should be the key of its
 * generic method definition.
 */
static MonoMethod*
check_generic_wrapper_cache (GHashTable *cache, MonoMethod *orig_method, gpointer key, gpointer def_key)
{
	MonoMethod *res;
	MonoMethod *inst, *def;
	MonoGenericContext *ctx;

	g_assert (orig_method->is_inflated);
	ctx = mono_method_get_context (orig_method);

	/*
	 * Look for the instance
	 */
	res = mono_marshal_find_in_cache (cache, key);
	if (res)
		return res;

	/*
	 * Look for the definition
	 */
	def = mono_marshal_find_in_cache (cache, def_key);
	if (def) {
		ERROR_DECL (error);
		inst = mono_class_inflate_generic_method_checked (def, ctx, error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
		/* Cache it */
		mono_memory_barrier ();
		mono_marshal_lock ();
		res = (MonoMethod *)g_hash_table_lookup (cache, key);
		if (!res) {
			g_hash_table_insert (cache, key, inst);
			res = inst;
		}
		mono_marshal_unlock ();
		return res;
	}
	return NULL;
}

static MonoMethod*
cache_generic_wrapper (GHashTable *cache, MonoMethod *orig_method, MonoMethod *def, MonoGenericContext *ctx, gpointer key)
{
	ERROR_DECL (error);
	MonoMethod *inst, *res;

	/*
	 * We use the same cache for the generic definition and the instances.
	 */
	inst = mono_class_inflate_generic_method_checked (def, ctx, error);
	g_assert (is_ok (error)); /* FIXME don't swallow the error */
	mono_memory_barrier ();
	mono_marshal_lock ();
	res = (MonoMethod *)g_hash_table_lookup (cache, key);
	if (!res) {
		g_hash_table_insert (cache, key, inst);
		res = inst;
	}
	mono_marshal_unlock ();
	return res;
}

static MonoMethod*
check_generic_delegate_wrapper_cache (GHashTable *cache, MonoMethod *orig_method, MonoMethod *def_method, MonoGenericContext *ctx)
{
	ERROR_DECL (error);
	MonoMethod *res;
	MonoMethod *inst, *def;

	/*
	 * Look for the instance
	 */
	res = mono_marshal_find_in_cache (cache, orig_method->klass);
	if (res)
		return res;

	/*
	 * Look for the definition
	 */
	def = mono_marshal_find_in_cache (cache, def_method->klass);
	if (def) {
		inst = mono_class_inflate_generic_method_checked (def, ctx, error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */

		/* Cache it */
		mono_memory_barrier ();
		mono_marshal_lock ();
		res = (MonoMethod *)g_hash_table_lookup (cache, orig_method->klass);
		if (!res) {
			g_hash_table_insert (cache, orig_method->klass, inst);
			res = inst;
		}
		mono_marshal_unlock ();
		return res;
	}
	return NULL;
}

static MonoMethod*
cache_generic_delegate_wrapper (GHashTable *cache, MonoMethod *orig_method, MonoMethod *def, MonoGenericContext *ctx)
{
	ERROR_DECL (error);
	MonoMethod *inst, *res;
	WrapperInfo *ginfo, *info;

	/*
	 * We use the same cache for the generic definition and the instances.
	 */
	inst = mono_class_inflate_generic_method_checked (def, ctx, error);
	g_assert (is_ok (error)); /* FIXME don't swallow the error */

	ginfo = mono_marshal_get_wrapper_info (def);
	if (ginfo) {
		info = (WrapperInfo *)mono_image_alloc0 (m_class_get_image (def->klass), sizeof (WrapperInfo));
		info->subtype = ginfo->subtype;
		if (info->subtype == WRAPPER_SUBTYPE_NONE) {
			info->d.delegate_invoke.method = mono_class_inflate_generic_method_checked (ginfo->d.delegate_invoke.method, ctx, error);
			mono_error_assert_ok (error);
		}
	}

	mono_memory_barrier ();
	mono_marshal_lock ();
	res = (MonoMethod *)g_hash_table_lookup (cache, orig_method->klass);
	if (!res) {
		g_hash_table_insert (cache, orig_method->klass, inst);
		res = inst;
	}
	mono_marshal_unlock ();
	return res;
}

/**
 * mono_marshal_get_delegate_begin_invoke:
 */
MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;

	g_assert (method && m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "BeginInvoke"));

	/*
	 * For generic delegates, create a generic wrapper, and returns an instance to help AOT.
	 */
	if (method->is_inflated) {
		orig_method = method;
		ctx = &((MonoMethodInflated*)method)->context;
		method = ((MonoMethodInflated*)method)->declaring;
	}

	sig = mono_signature_no_pinvoke (method);

	/*
	 * Check cache
	 */
	if (ctx) {
		cache = get_cache (&((MonoMethodInflated*)orig_method)->owner->wrapper_caches.delegate_begin_invoke_cache, mono_aligned_addr_hash, NULL);
		res = check_generic_delegate_wrapper_cache (cache, orig_method, method, ctx);
		if (res)
			return res;
	} else {
		cache = get_cache (&get_method_image (method)->wrapper_caches.delegate_begin_invoke_cache,
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)mono_metadata_signature_equal);
		if ((res = mono_marshal_find_in_cache (cache, sig)))
			return res;
	}

	g_assert (sig->hasthis);

	name = mono_signature_to_name (sig, "begin_invoke");
	if (ctx)
		mb = mono_mb_new (method->klass, name, MONO_WRAPPER_DELEGATE_BEGIN_INVOKE);
	else
		mb = mono_mb_new (get_wrapper_target_class (get_method_image (method)), name, MONO_WRAPPER_DELEGATE_BEGIN_INVOKE);
	g_free (name);

	get_marshal_cb ()->emit_delegate_begin_invoke (mb, sig);

	WrapperInfo *info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.delegate_invoke.method = method;

	if (ctx) {
		MonoMethod *def;
		def = mono_mb_create_and_cache_full (cache, method->klass, mb, sig, sig->param_count + 16, info, NULL);
		res = cache_generic_delegate_wrapper (cache, orig_method, def, ctx);
	} else {
		res = mono_mb_create_and_cache_full (cache, sig, mb, sig, sig->param_count + 16, info, NULL);
	}

	mono_mb_free (mb);
	return res;
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params)
{
	mono_set_pending_exception (mono_exception_from_name (mono_defaults.corlib, "System", "PlatformNotSupportedException"));
	return NULL;
}

/**
 * mono_marshal_get_delegate_end_invoke:
 */
MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;

	g_assert (method && m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "EndInvoke"));

	/*
	 * For generic delegates, create a generic wrapper, and returns an instance to help AOT.
	 */
	if (method->is_inflated) {
		orig_method = method;
		ctx = &((MonoMethodInflated*)method)->context;
		method = ((MonoMethodInflated*)method)->declaring;
	}

	sig = mono_signature_no_pinvoke (method);

	/*
	 * Check cache
	 */
	if (ctx) {
		cache = get_cache (&((MonoMethodInflated*)orig_method)->owner->wrapper_caches.delegate_end_invoke_cache, mono_aligned_addr_hash, NULL);
		res = check_generic_delegate_wrapper_cache (cache, orig_method, method, ctx);
		if (res)
			return res;
	} else {
		cache = get_cache (&get_method_image (method)->wrapper_caches.delegate_end_invoke_cache,
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)mono_metadata_signature_equal);
		if ((res = mono_marshal_find_in_cache (cache, sig)))
			return res;
	}

	g_assert (sig->hasthis);

	name = mono_signature_to_name (sig, "end_invoke");
	if (ctx)
		mb = mono_mb_new (method->klass, name, MONO_WRAPPER_DELEGATE_END_INVOKE);
	else
		mb = mono_mb_new (get_wrapper_target_class (get_method_image (method)), name, MONO_WRAPPER_DELEGATE_END_INVOKE);
	g_free (name);

	get_marshal_cb ()->emit_delegate_end_invoke (mb, sig);

	WrapperInfo *info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.delegate_invoke.method = method;

	if (ctx) {
		MonoMethod *def;
		def = mono_mb_create_and_cache_full (cache, method->klass, mb, sig, sig->param_count + 16, info, NULL);
		res = cache_generic_delegate_wrapper (cache, orig_method, def, ctx);
	} else {
		res = mono_mb_create_and_cache_full (cache, sig,
											 mb, sig, sig->param_count + 16, info, NULL);
	}
	mono_mb_free (mb);

	return res;
}

typedef struct
{
	MonoMethodSignature *sig;
	gpointer pointer;
} SignaturePointerPair;

static guint
signature_pointer_pair_hash (gconstpointer data)
{
	SignaturePointerPair *pair = (SignaturePointerPair*)data;

	return mono_signature_hash (pair->sig) ^ mono_aligned_addr_hash (pair->pointer);
}

static gboolean
signature_pointer_pair_equal (gconstpointer data1, gconstpointer data2)
{
	SignaturePointerPair *pair1 = (SignaturePointerPair*) data1, *pair2 = (SignaturePointerPair*) data2;
	return mono_metadata_signature_equal (pair1->sig, pair2->sig) && (pair1->pointer == pair2->pointer);
}

static gboolean
signature_pointer_pair_matches_pointer (gpointer key, gpointer value, gpointer user_data)
{
	SignaturePointerPair *pair = (SignaturePointerPair*)key;

	return pair->pointer == user_data;
}

static void
free_signature_pointer_pair (SignaturePointerPair *pair)
{
	g_free (pair);
}

MonoMethod *
mono_marshal_get_delegate_invoke_internal (MonoMethod *method, gboolean callvirt, gboolean static_method_with_first_arg_bound, MonoMethod *target_method)
{
	MonoMethodSignature *sig, *invoke_sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	gpointer cache_key = NULL;
	SignaturePointerPair key = { NULL, NULL };
	SignaturePointerPair *new_key;
	char *name;
	MonoClass *target_class = NULL;
	gboolean closed_over_null = FALSE;
	MonoGenericContext *ctx = NULL;
	MonoGenericContainer *container = NULL;
	MonoMethod *orig_method = method;
	WrapperInfo *info;
	WrapperSubtype subtype = WRAPPER_SUBTYPE_NONE;
	gboolean found;

	g_assert (method && m_class_get_parent (method->klass) == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "Invoke"));

	invoke_sig = sig = mono_signature_no_pinvoke (method);

	/*
	 * If the delegate target is null, and the target method is not static, a virtual 
	 * call is made to that method with the first delegate argument as this. This is 
	 * a non-documented .NET feature.
	 */
	if (callvirt) {
		subtype = WRAPPER_SUBTYPE_DELEGATE_INVOKE_VIRTUAL;
		if (target_method->is_inflated) {
			ERROR_DECL (error);
			MonoType *target_type;

			g_assert (method->signature->hasthis);
			target_type = mono_class_inflate_generic_type_checked (method->signature->params [0],
				mono_method_get_context (method), error);
			mono_error_assert_ok (error); /* FIXME don't swallow the error */
			target_class = mono_class_from_mono_type_internal (target_type);
		} else {
			target_class = target_method->klass;
		}

		closed_over_null = sig->param_count == mono_method_signature_internal (target_method)->param_count;
	}

	if (static_method_with_first_arg_bound) {
		subtype = WRAPPER_SUBTYPE_DELEGATE_INVOKE_BOUND;
		g_assert (!callvirt);
		invoke_sig = mono_method_signature_internal (target_method);
		/*
		 * The wrapper has a different lifetime from the method to be invoked.
		 * If the method is dynamic we don't want to be using its signature
		 * in the wrapper since it could get freed early.
		 */
		if (method_is_dynamic (target_method))
			invoke_sig = mono_metadata_signature_dup_full (get_method_image (target_method), invoke_sig);
	}

	/*
	 * For generic delegates, create a generic wrapper, and return an instance to help AOT.
	 */
	if (method->is_inflated && subtype == WRAPPER_SUBTYPE_NONE) {
		ctx = &((MonoMethodInflated*)method)->context;
		method = ((MonoMethodInflated*)method)->declaring;

		container = mono_method_get_generic_container (method);
		if (!container)
			container = mono_class_try_get_generic_container (method->klass); //FIXME is this a case of a try?
		g_assert (container);

		invoke_sig = sig = mono_signature_no_pinvoke (method);
	}

	/*
	 * Check cache
	 */
	if (ctx) {
		cache = get_cache (&((MonoMethodInflated*)orig_method)->owner->wrapper_caches.delegate_invoke_cache, mono_aligned_addr_hash, NULL);
		res = check_generic_delegate_wrapper_cache (cache, orig_method, method, ctx);
		if (res)
			return res;
		cache_key = method->klass;
	} else if (static_method_with_first_arg_bound) {
		GHashTable **cache_ptr;

		cache_ptr = &mono_method_get_wrapper_cache (target_method)->delegate_bound_static_invoke_cache;

		cache = get_cache (cache_ptr,
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)mono_metadata_signature_equal);
		/*
		 * The wrapper is based on sig+invoke_sig, but sig can be derived from invoke_sig.
		 */
		res = mono_marshal_find_in_cache (cache, invoke_sig);
		if (res)
			return res;
		cache_key = invoke_sig;
	} else if (callvirt) {
		GHashTable **cache_ptr;

		cache_ptr = &mono_method_get_wrapper_cache (method)->delegate_abstract_invoke_cache;

		/* We need to cache the signature+method pair */
		mono_marshal_lock ();
		if (!*cache_ptr)
			*cache_ptr = g_hash_table_new_full (signature_pointer_pair_hash, (GEqualFunc)signature_pointer_pair_equal, (GDestroyNotify)free_signature_pointer_pair, NULL);
		cache = *cache_ptr;
		key.sig = invoke_sig;
		key.pointer = target_method;
		res = (MonoMethod *)g_hash_table_lookup (cache, &key);
		mono_marshal_unlock ();
		if (res)
			return res;
	} else {
		// Inflated methods should not be in this cache because it's not stored on the imageset.
		g_assert (!method->is_inflated);
		cache = get_cache (&get_method_image (method)->wrapper_caches.delegate_invoke_cache,
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)mono_metadata_signature_equal);
		res = mono_marshal_find_in_cache (cache, sig);
		if (res)
			return res;
		cache_key = sig;
	}

	if (!static_method_with_first_arg_bound) {
		invoke_sig = mono_metadata_signature_dup_full (get_method_image (method), sig);
		invoke_sig->hasthis = 0;
	}

	if (static_method_with_first_arg_bound)
		name = mono_signature_to_name (invoke_sig, "invoke_bound");
	else if (closed_over_null)
		name = mono_signature_to_name (invoke_sig, "invoke_closed_over_null");
	else if (callvirt)
		name = mono_signature_to_name (invoke_sig, "invoke_callvirt");
	else
		name = mono_signature_to_name (invoke_sig, "invoke");
	if (ctx)
		mb = mono_mb_new (method->klass, name, MONO_WRAPPER_DELEGATE_INVOKE);
	else
		mb = mono_mb_new (get_wrapper_target_class (get_method_image (method)), name, MONO_WRAPPER_DELEGATE_INVOKE);
	g_free (name);

	get_marshal_cb ()->emit_delegate_invoke_internal (mb, sig, invoke_sig, static_method_with_first_arg_bound, callvirt, closed_over_null, method, target_method, target_class, ctx, container);

	get_marshal_cb ()->mb_skip_visibility (mb);

	info = mono_wrapper_info_create (mb, subtype);
	info->d.delegate_invoke.method = method;

	if (ctx) {
		MonoMethod *def;

		def = mono_mb_create_and_cache_full (cache, cache_key, mb, sig, sig->param_count + 16, info, NULL);
		res = cache_generic_delegate_wrapper (cache, orig_method, def, ctx);
	} else if (callvirt) {
		new_key = g_new0 (SignaturePointerPair, 1);
		*new_key = key;

		res = mono_mb_create_and_cache_full (cache, new_key, mb, sig, sig->param_count + 16, info, &found);
		if (found)
			g_free (new_key);
	} else {
		res = mono_mb_create_and_cache_full (cache, cache_key, mb, sig, sig->param_count + 16, info, NULL);
	}
	mono_mb_free (mb);

	/* mono_method_print_code (res); */

	return res;	
}

/**
 * mono_marshal_get_delegate_invoke:
 * The returned method invokes all methods in a multicast delegate.
 */
MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method, MonoDelegate *del)
{
	gboolean callvirt = FALSE;
	gboolean static_method_with_first_arg_bound = FALSE;
	MonoMethod *target_method = NULL;
	MonoMethodSignature *sig;

	sig = mono_signature_no_pinvoke (method);

	if (del && !del->target && del->method && mono_method_signature_internal (del->method)->hasthis) {
		if (!(del->method->flags & METHOD_ATTRIBUTE_VIRTUAL) && !m_class_is_valuetype (del->method->klass) && sig->param_count ==  mono_method_signature_internal (del->method)->param_count + 1) {
			/* The first argument of the delegate is passed as this, the normal invoke code can handle this */
		} else {
			callvirt = TRUE;
		}
		target_method = del->method;
	}

	if (del && del->method && mono_method_signature_internal (del->method)->param_count == sig->param_count + 1 && (del->method->flags & METHOD_ATTRIBUTE_STATIC)) {
		static_method_with_first_arg_bound = TRUE;
		target_method = del->method;
	}

	return mono_marshal_get_delegate_invoke_internal (method, callvirt, static_method_with_first_arg_bound, target_method);
}

typedef struct {
	MonoMethodSignature *ctor_sig;
	MonoMethodSignature *sig;
} CtorSigPair;

/* protected by the marshal lock, contains CtorSigPair pointers */
static GSList *strsig_list = NULL;

static MonoMethodSignature *
lookup_string_ctor_signature (MonoMethodSignature *sig)
{
	MonoMethodSignature *callsig;
	CtorSigPair *cs;
	GSList *item;

	mono_marshal_lock ();
	callsig = NULL;
	for (item = strsig_list; item; item = item->next) {
		cs = (CtorSigPair *)item->data;
		/* mono_metadata_signature_equal () is safe to call with the marshal lock
		 * because it is lock-free.
		 */
		if (mono_metadata_signature_equal (sig, cs->ctor_sig)) {
			callsig = cs->sig;
			break;
		}
	}
	mono_marshal_unlock ();
	return callsig;
}

static MonoMethodSignature *
add_string_ctor_signature (MonoMethod *method)
{
	MonoMethodSignature *callsig;
	CtorSigPair *cs;

	callsig = mono_metadata_signature_dup_full (get_method_image (method), mono_method_signature_internal (method));
	callsig->ret = m_class_get_byval_arg (mono_defaults.string_class);
	cs = g_new (CtorSigPair, 1);
	cs->sig = callsig;
	cs->ctor_sig = mono_method_signature_internal (method);

	mono_marshal_lock ();
	strsig_list = g_slist_prepend (strsig_list, cs);
	mono_marshal_unlock ();
	return callsig;
}

/*
 * mono_marshal_get_string_ctor_signature:
 *
 *   Return the modified signature used by string ctors (they return the newly created
 * string).
 */
MonoMethodSignature*
mono_marshal_get_string_ctor_signature (MonoMethod *method)
{
	MonoMethodSignature *sig = lookup_string_ctor_signature (mono_method_signature_internal (method));
	if (!sig)
		sig = add_string_ctor_signature (method);

	return sig;
}

static MonoType*
get_runtime_invoke_type (MonoType *t, gboolean ret)
{
	if (t->byref) {
		if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type_internal (t)))
			return t;

		/* The result needs loaded indirectly */
		if (ret)
			return t;

		/* Can't share this with 'I' as that needs another indirection */
		return m_class_get_this_arg (mono_defaults.int_class);
	}

	if (MONO_TYPE_IS_REFERENCE (t))
		return mono_get_object_type ();

	if (ret)
		/* The result needs to be boxed */
		return t;

handle_enum:
	switch (t->type) {
		/* Can't share these as the argument needs to be loaded using sign/zero extension */
		/*
	case MONO_TYPE_U1:
		return m_class_get_byval_arg (mono_defaults.sbyte_class);
	case MONO_TYPE_U2:
		return m_class_get_byval_arg (mono_defaults.int16_class);
	case MONO_TYPE_U4:
		return mono_get_int32_type ();
		*/
	case MONO_TYPE_U8:
		return m_class_get_byval_arg (mono_defaults.int64_class);
	case MONO_TYPE_BOOLEAN:
		return m_class_get_byval_arg (mono_defaults.byte_class);
	case MONO_TYPE_CHAR:
		return m_class_get_byval_arg (mono_defaults.uint16_class);
	case MONO_TYPE_U:
		return mono_get_int_type ();
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (t->data.klass)) {
			t = mono_class_enum_basetype_internal (t->data.klass);
			goto handle_enum;
		}
		return t;
	default:
		return t;
	}
}

/*
 * mono_marshal_get_runtime_invoke_sig:
 *
 *   Return a common signature used for sharing runtime invoke wrappers.
 */
static MonoMethodSignature*
mono_marshal_get_runtime_invoke_sig (MonoMethodSignature *sig)
{
	MonoMethodSignature *res = mono_metadata_signature_dup (sig);
	int i;

	res->generic_param_count = 0;
	res->ret = get_runtime_invoke_type (sig->ret, TRUE);
	for (i = 0; i < res->param_count; ++i)
		res->params [i] = get_runtime_invoke_type (sig->params [i], FALSE);

	return res;
}

static gboolean
runtime_invoke_signature_equal (MonoMethodSignature *sig1, MonoMethodSignature *sig2)
{
	/* Can't share wrappers which return a vtype since it needs to be boxed */
	if (sig1->ret != sig2->ret && !(MONO_TYPE_IS_REFERENCE (sig1->ret) && MONO_TYPE_IS_REFERENCE (sig2->ret)) && !mono_metadata_type_equal (sig1->ret, sig2->ret))
		return FALSE;
	else
		return mono_metadata_signature_equal (sig1, sig2);
}

struct _MonoWrapperMethodCacheKey {
	MonoMethod *method;
	gboolean virtual_;
	gboolean need_direct_wrapper;
};

struct _MonoWrapperSignatureCacheKey {
	MonoMethodSignature *signature;
	gboolean valuetype;
};

typedef struct _MonoWrapperMethodCacheKey MonoWrapperMethodCacheKey;
typedef struct _MonoWrapperSignatureCacheKey MonoWrapperSignatureCacheKey;

static guint
wrapper_cache_method_key_hash (MonoWrapperMethodCacheKey *key)
{
	return mono_aligned_addr_hash (key->method) ^ (((!!key->virtual_) << 17) | ((!!key->need_direct_wrapper) << 19) * 17);
}

static guint
wrapper_cache_signature_key_hash (MonoWrapperSignatureCacheKey *key)
{
	return mono_signature_hash (key->signature) ^ (((!!key->valuetype) << 18) * 17);
}

static gboolean
wrapper_cache_method_key_equal (MonoWrapperMethodCacheKey *key1, MonoWrapperMethodCacheKey *key2)
{
	if (key1->virtual_ != key2->virtual_ || key1->need_direct_wrapper != key2->need_direct_wrapper)
		return FALSE;
	return key1->method == key2->method;
}

static gboolean
wrapper_cache_signature_key_equal (MonoWrapperSignatureCacheKey *key1, MonoWrapperSignatureCacheKey *key2)
{
	if (key1->valuetype != key2->valuetype)
		return FALSE;
	return runtime_invoke_signature_equal (key1->signature, key2->signature);
}

/**
 * mono_marshal_get_runtime_invoke:
 * Generates IL code for the runtime invoke function:
 *
 * <code>MonoObject *runtime_invoke (MonoObject *this_obj, void **params, MonoObject **exc, void* method)</code>
 *
 * We also catch exceptions if \p exc is not NULL.
 * If \p virtual is TRUE, then \p method is invoked virtually on \p this. This is useful since
 * it means that the compiled code for \p method does not have to be looked up 
 * before calling the runtime invoke wrapper. In this case, the wrapper ignores
 * its \p method argument.
 */
MonoMethod *
mono_marshal_get_runtime_invoke_full (MonoMethod *method, gboolean virtual_, gboolean need_direct_wrapper)
{
	MonoMethodSignature *sig, *csig, *callsig;
	MonoMethodBuilder *mb;
	GHashTable *method_cache = NULL, *sig_cache = NULL;
	GHashTable **cache_table = NULL;
	MonoClass *target_klass;
	MonoMethod *res = NULL;
	static MonoMethodSignature *cctor_signature = NULL;
	static MonoMethodSignature *finalize_signature = NULL;
	char *name;
	const char *param_names [16];
	WrapperInfo *info;
	MonoWrapperMethodCacheKey *method_key;
	MonoWrapperMethodCacheKey method_key_lookup_only;
	memset (&method_key_lookup_only, 0, sizeof (method_key_lookup_only));
	method_key_lookup_only.method = method;
	method_key_lookup_only.virtual_ = virtual_;
	method_key_lookup_only.need_direct_wrapper = need_direct_wrapper;
	method_key = &method_key_lookup_only;

	g_assert (method);

	if (!cctor_signature) {
		cctor_signature = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		cctor_signature->ret = mono_get_void_type ();
	}
	if (!finalize_signature) {
		finalize_signature = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		finalize_signature->ret = mono_get_void_type ();
		finalize_signature->hasthis = 1;
	}

	cache_table = &mono_method_get_wrapper_cache (method)->runtime_invoke_method_cache;
	method_cache = get_cache (cache_table, (GHashFunc) wrapper_cache_method_key_hash, (GCompareFunc) wrapper_cache_method_key_equal);

	res = mono_marshal_find_in_cache (method_cache, method_key);
	if (res)
		return res;
		
	if (method->string_ctor) {
		callsig = lookup_string_ctor_signature (mono_method_signature_internal (method));
		if (!callsig)
			callsig = add_string_ctor_signature (method);
	} else {
		if (method_is_dynamic (method))
			callsig = mono_metadata_signature_dup_full (get_method_image (method), mono_method_signature_internal (method));
		else
			callsig = mono_method_signature_internal (method);
	}

	sig = mono_method_signature_internal (method);

	target_klass = get_wrapper_target_class (m_class_get_image (method->klass));

	/* Try to share wrappers for non-corlib methods with simple signatures */
	if (mono_metadata_signature_equal (callsig, cctor_signature)) {
		callsig = cctor_signature;
		target_klass = mono_defaults.object_class;
	} else if (mono_metadata_signature_equal (callsig, finalize_signature)) {
		callsig = finalize_signature;
		target_klass = mono_defaults.object_class;
	}

	if (need_direct_wrapper || virtual_) {
		/* Already searched at the start. We cannot cache those wrappers based
		 * on signatures because they contain a reference to the method */
	} else {
		MonoMethodSignature *tmp_sig;

		callsig = mono_marshal_get_runtime_invoke_sig (callsig);
		MonoWrapperSignatureCacheKey sig_key;
		memset (&sig_key, 0, sizeof (sig_key));
		sig_key.signature = callsig;
		sig_key.valuetype = m_class_is_valuetype (method->klass);

		cache_table = &mono_method_get_wrapper_cache (method)->runtime_invoke_signature_cache;
		sig_cache = get_cache (cache_table, (GHashFunc) wrapper_cache_signature_key_hash, (GCompareFunc) wrapper_cache_signature_key_equal);

		/* from mono_marshal_find_in_cache */
		mono_marshal_lock ();
		res = (MonoMethod *)g_hash_table_lookup (sig_cache, &sig_key);
		mono_marshal_unlock ();

		if (res) {
			g_free (callsig);
			return res;
		}

		/* Make a copy of the signature from the image mempool */
		tmp_sig = callsig;
		callsig = mono_metadata_signature_dup_full (m_class_get_image (target_klass), callsig);
		g_free (tmp_sig);
	}

	csig = mono_metadata_signature_alloc (m_class_get_image (target_klass), 4);

	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();

	csig->ret = object_type;
	if (m_class_is_valuetype (method->klass) && mono_method_signature_internal (method)->hasthis)
		csig->params [0] = get_runtime_invoke_type (m_class_get_this_arg (method->klass), FALSE);
	else
		csig->params [0] = object_type;
	csig->params [1] = int_type;
	csig->params [2] = int_type;
	csig->params [3] = int_type;
	csig->pinvoke = 1;
#if TARGET_WIN32
	/* This is called from runtime code so it has to be cdecl */
	csig->call_convention = MONO_CALL_C;
#endif

	name = mono_signature_to_name (callsig, virtual_ ? "runtime_invoke_virtual" : (need_direct_wrapper ? "runtime_invoke_direct" : "runtime_invoke"));
	mb = mono_mb_new (target_klass, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

	param_names [0] = "this";
	param_names [1] = "params";
	param_names [2] = "exc";
	param_names [3] = "method";

	get_marshal_cb ()->emit_runtime_invoke_body (mb, param_names, m_class_get_image (target_klass), method, sig, callsig, virtual_, need_direct_wrapper);

	method_key = g_new (MonoWrapperMethodCacheKey, 1);
	memcpy (method_key, &method_key_lookup_only, sizeof (MonoWrapperMethodCacheKey));

	if (need_direct_wrapper || virtual_) {
		get_marshal_cb ()->mb_skip_visibility (mb);
		info = mono_wrapper_info_create (mb, virtual_ ? WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL : WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT);
		info->d.runtime_invoke.method = method;
		res = mono_mb_create_and_cache_full (method_cache, method_key, mb, csig, sig->param_count + 16, info, NULL);
	} else {
		MonoWrapperSignatureCacheKey *sig_key = g_new0 (MonoWrapperSignatureCacheKey, 1);
		sig_key->signature = callsig;
		sig_key->valuetype = m_class_is_valuetype (method->klass);

		/* taken from mono_mb_create_and_cache */
		mono_marshal_lock ();
		res = (MonoMethod *)g_hash_table_lookup (sig_cache, sig_key);
		mono_marshal_unlock ();

		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_RUNTIME_INVOKE_NORMAL);
		info->d.runtime_invoke.sig = callsig;

		/* Somebody may have created it before us */
		if (!res) {
			MonoMethod *newm;
			newm = mono_mb_create (mb, csig, sig->param_count + 16, info);

			mono_marshal_lock ();
			res = (MonoMethod *)g_hash_table_lookup (sig_cache, sig_key);
			if (!res) {
				res = newm;
				g_hash_table_insert (sig_cache, sig_key, res);
				g_hash_table_insert (method_cache, method_key, res);
			} else {
				mono_free_method (newm);
				g_free (sig_key);
				g_free (method_key);
			}
			mono_marshal_unlock ();
		} else {
			g_free (sig_key);
			g_free (method_key);
		}

		/* end mono_mb_create_and_cache */
	}

	mono_mb_free (mb);

	return res;	
}

MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method, gboolean virtual_)
{
	gboolean need_direct_wrapper = FALSE;

	if (virtual_)
		need_direct_wrapper = TRUE;

	if (method->dynamic)
		need_direct_wrapper = TRUE;

	if (m_class_get_rank (method->klass) && (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
		(method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
		/*
		 * Array Get/Set/Address methods. The JIT implements them using inline code
		 * so we need to create an invoke wrapper which calls the method directly.
		 */
		need_direct_wrapper = TRUE;
	}

	if (method->string_ctor) {
		/* Can't share this as we push a string as this */
		need_direct_wrapper = TRUE;
	}

	return mono_marshal_get_runtime_invoke_full (method, virtual_, need_direct_wrapper);
}

/*
 * mono_marshal_get_runtime_invoke_dynamic:
 *
 *   Return a method which can be used to invoke managed methods from native code
 * dynamically.
 * The signature of the returned method is given by RuntimeInvokeDynamicFunction:
 * void runtime_invoke (void *args, MonoObject **exc, void *compiled_method)
 * ARGS should point to an architecture specific structure containing 
 * the arguments and space for the return value.
 * The other arguments are the same as for runtime_invoke (), except that
 * ARGS should contain the this argument too.
 * This wrapper serves the same purpose as the runtime-invoke wrappers, but there
 * is only one copy of it, which is useful in full-aot.
 */
MonoMethod*
mono_marshal_get_runtime_invoke_dynamic (void)
{
	static MonoMethod *method;
	MonoMethodSignature *csig;
	MonoMethodBuilder *mb;
	char *name;
	WrapperInfo *info;

	if (method)
		return method;

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);

	MonoType *void_type = mono_get_void_type ();
	MonoType *int_type = mono_get_int_type ();

	csig->ret = void_type;
	csig->params [0] = int_type;
	csig->params [1] = int_type;
	csig->params [2] = int_type;
	csig->params [3] = int_type;

	name = g_strdup ("runtime_invoke_dynamic");
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

	get_marshal_cb ()->emit_runtime_invoke_dynamic (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_RUNTIME_INVOKE_DYNAMIC);

	mono_marshal_lock ();
	/* double-checked locking */
	if (!method)
		method = mono_mb_create (mb, csig, 16, info);

	mono_marshal_unlock ();

	mono_mb_free (mb);

	return method;
}

/*
 * mono_marshal_get_runtime_invoke_for_sig:
 *
 *   Return a runtime invoke wrapper for a given signature.
 */
MonoMethod *
mono_marshal_get_runtime_invoke_for_sig (MonoMethodSignature *sig)
{
	MonoMethodSignature *csig, *callsig;
	MonoMethodBuilder *mb;
	MonoImage *image;
	GHashTable *cache = NULL;
	GHashTable **cache_table = NULL;
	MonoMethod *res = NULL;
	char *name;
	const char *param_names [16];
	WrapperInfo *info;

	/* A simplified version of mono_marshal_get_runtime_invoke */

	image = mono_defaults.corlib;

	callsig = mono_marshal_get_runtime_invoke_sig (sig);

	cache_table = &image->wrapper_caches.runtime_invoke_sig_cache;

	cache = get_cache (cache_table, (GHashFunc)mono_signature_hash,
					   (GCompareFunc)runtime_invoke_signature_equal);

	/* from mono_marshal_find_in_cache */
	mono_marshal_lock ();
	res = (MonoMethod *)g_hash_table_lookup (cache, callsig);
	mono_marshal_unlock ();

	if (res) {
		g_free (callsig);
		return res;
	}

	/* Make a copy of the signature from the image mempool */
	callsig = mono_metadata_signature_dup_full (image, callsig);

	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();
	csig = mono_metadata_signature_alloc (image, 4);
	csig->ret = object_type;
	csig->params [0] = object_type;
	csig->params [1] = int_type;
	csig->params [2] = int_type;
	csig->params [3] = int_type;
	csig->pinvoke = 1;
#if TARGET_WIN32
	/* This is called from runtime code so it has to be cdecl */
	csig->call_convention = MONO_CALL_C;
#endif

	name = mono_signature_to_name (callsig, "runtime_invoke_sig");
	mb = mono_mb_new (mono_defaults.object_class, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

	param_names [0] = "this";
	param_names [1] = "params";
	param_names [2] = "exc";
	param_names [3] = "method";

	get_marshal_cb ()->emit_runtime_invoke_body (mb, param_names, image, NULL, sig, callsig, FALSE, FALSE);

	/* taken from mono_mb_create_and_cache */
	mono_marshal_lock ();
	res = (MonoMethod *)g_hash_table_lookup (cache, callsig);
	mono_marshal_unlock ();

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_RUNTIME_INVOKE_NORMAL);
	info->d.runtime_invoke.sig = callsig;

	/* Somebody may have created it before us */
	if (!res) {
		MonoMethod *newm;
		newm = mono_mb_create (mb, csig, sig->param_count + 16, info);

		mono_marshal_lock ();
		res = (MonoMethod *)g_hash_table_lookup (cache, callsig);
		if (!res) {
			res = newm;
			g_hash_table_insert (cache, callsig, res);
		} else {
			mono_free_method (newm);
		}
		mono_marshal_unlock ();
	}

	/* end mono_mb_create_and_cache */

	mono_mb_free (mb);

	return res;
}

/**
 * mono_marshal_get_icall_wrapper:
 * Generates IL code for the JIT icall wrapper. The generated method
 * calls the unmanaged code in \p callinfo->func.
 */
MonoMethod *
mono_marshal_get_icall_wrapper (MonoJitICallInfo *callinfo, gboolean check_exceptions)
{
	MonoMethodSignature *csig, *csig2;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	WrapperInfo *info;

	gconstpointer const func = callinfo->func;
	
	GHashTable *cache = get_cache (& m_class_get_image (mono_defaults.object_class)->icall_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, (gpointer) func)))
		return res;

	MonoMethodSignature *const sig = callinfo->sig;
	g_assert (sig->pinvoke);

	char *const name = g_strdup_printf ("__icall_wrapper_%s", callinfo->name);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	/* Add an explicit this argument */
	if (sig->hasthis)
		csig2 = mono_metadata_signature_dup_add_this (mono_defaults.corlib, sig, mono_defaults.object_class);
	else
		csig2 = mono_metadata_signature_dup_full (mono_defaults.corlib, sig);

	get_marshal_cb ()->emit_icall_wrapper (mb, callinfo, csig2, check_exceptions);

	csig = mono_metadata_signature_dup_full (mono_defaults.corlib, sig);
	csig->pinvoke = 0;
	if (csig->call_convention == MONO_CALL_VARARG)
		csig->call_convention = 0;

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_ICALL_WRAPPER);
	info->d.icall.jit_icall_id = mono_jit_icall_info_id (callinfo);
	res = mono_mb_create_and_cache_full (cache, (gpointer) func, mb, csig, csig->param_count + 16, info, NULL);
	mono_mb_free (mb);
	g_free (name);

	return res;
}

const char *
mono_marshal_get_aot_init_wrapper_name (MonoAotInitSubtype subtype)
{
	const char *name = NULL;
	switch (subtype) {
		case AOT_INIT_METHOD:
			name = "init_method";
			break;
		case AOT_INIT_METHOD_GSHARED_MRGCTX:
			name = "init_method_gshared_mrgctx";
			break;
		case AOT_INIT_METHOD_GSHARED_THIS:
			name = "init_method_gshared_this";
			break;
		case AOT_INIT_METHOD_GSHARED_VTABLE:
			name = "init_method_gshared_vtable";
			break;
		default:
			g_assert_not_reached ();
	}
	return name;
}

MonoMethod *
mono_marshal_get_aot_init_wrapper (MonoAotInitSubtype subtype)
{
	MonoMethodBuilder *mb;
	MonoMethod *res;
	WrapperInfo *info;
	MonoMethodSignature *csig = NULL;
	MonoType *void_type = mono_get_void_type ();
	MonoType *int_type = mono_get_int_type ();
	const char *name = mono_marshal_get_aot_init_wrapper_name (subtype);

	switch (subtype) {
		case AOT_INIT_METHOD:
			csig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);
			csig->ret = void_type;
			csig->params [0] = int_type;
			csig->params [1] = int_type;
			break;
		case AOT_INIT_METHOD_GSHARED_MRGCTX:
		case AOT_INIT_METHOD_GSHARED_THIS:
		case AOT_INIT_METHOD_GSHARED_VTABLE:
			csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
			csig->ret = void_type;
			csig->params [0] = int_type;
			csig->params [1] = int_type;
			csig->params [2] = int_type;
			break;
		default:
			g_assert_not_reached ();
	}

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_OTHER);

	// Just stub out the method with a "CEE_RET"
	// Our codegen backend generates other code here
	get_marshal_cb ()->emit_return (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_AOT_INIT);
	info->d.aot_init.subtype = subtype;
	res = mono_mb_create (mb, csig, csig->param_count + 16, info);
	mono_mb_free (mb);

	return res;
}

/*
 * mono_marshal_get_llvm_func_wrapper:
 *
 *   Return a dummy wrapper which represents an LLVM function to the
 * rest of the runtime for EH etc. purposes. The body of the method is
 * LLVM code.
 */
MonoMethod *
mono_marshal_get_llvm_func_wrapper (MonoLLVMFuncWrapperSubtype subtype)
{
	MonoMethodBuilder *mb;
	MonoMethod *res;
	WrapperInfo *info;
	MonoMethodSignature *csig = NULL;
	MonoType *void_type = mono_get_void_type ();
	char *name = g_strdup_printf ("llvm_func_wrapper_%d", subtype);

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	csig->ret = void_type;

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_OTHER);

	// Just stub out the method with a "CEE_RET"
	// Our codegen backend generates other code here
	get_marshal_cb ()->emit_return (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_LLVM_FUNC);
	info->d.llvm_func.subtype = subtype;
	res = mono_mb_create (mb, csig, csig->param_count + 16, info);
	mono_mb_free (mb);

	return res;
}

gboolean
mono_pinvoke_is_unicode (MonoMethodPInvoke *piinfo)
{
	switch (piinfo->piflags & PINVOKE_ATTRIBUTE_CHAR_SET_MASK) {
	case PINVOKE_ATTRIBUTE_CHAR_SET_ANSI:
		return FALSE;
	case PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE:
		return TRUE;
	case PINVOKE_ATTRIBUTE_CHAR_SET_AUTO:
	default:
#ifdef TARGET_WIN32
		return TRUE;
#else
		return FALSE;
#endif
	}
}

MonoType*
mono_marshal_boolean_conv_in_get_local_type (MonoMarshalSpec *spec, guint8 *ldc_op /*out*/)
{
	if (spec == NULL) {
		return mono_get_int32_type ();
	} else {
		switch (spec->native) {
		case MONO_NATIVE_I1:
		case MONO_NATIVE_U1:
			return m_class_get_byval_arg (mono_defaults.byte_class);
		case MONO_NATIVE_VARIANTBOOL:
			if (ldc_op) *ldc_op = CEE_LDC_I4_M1;
			return m_class_get_byval_arg (mono_defaults.int16_class);
		case MONO_NATIVE_BOOLEAN:
			return mono_get_int32_type ();
		default:
			g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
			return mono_get_int32_type ();
		}
	}
}

MonoClass*
mono_marshal_boolean_managed_conv_in_get_conv_arg_class (MonoMarshalSpec *spec, guint8 *ldop/*out*/)
{
	MonoClass* conv_arg_class = mono_defaults.int32_class;
	if (spec) {
		switch (spec->native) {
		case MONO_NATIVE_I1:
		case MONO_NATIVE_U1:
			conv_arg_class = mono_defaults.byte_class;
			if (ldop) *ldop = CEE_LDIND_I1;
			break;
		case MONO_NATIVE_VARIANTBOOL:
			conv_arg_class = mono_defaults.int16_class;
			if (ldop) *ldop = CEE_LDIND_I2;
			break;
		case MONO_NATIVE_BOOLEAN:
			break;
		default:
			g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
		}
	}
	return conv_arg_class;
}

int
mono_emit_marshal (EmitMarshalContext *m, int argnum, MonoType *t, 
	      MonoMarshalSpec *spec, int conv_arg, 
	      MonoType **conv_arg_type, MarshalAction action)
{
	/* Ensure that we have marshalling info for this param */
	mono_marshal_load_type_info (mono_class_from_mono_type_internal (t));

	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return get_marshal_cb ()->emit_marshal_custom (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return get_marshal_cb ()->emit_marshal_asany (m, argnum, t, spec, conv_arg, conv_arg_type, action);
			
	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass == mono_class_try_get_handleref_class ())
			return get_marshal_cb ()->emit_marshal_handleref (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		
		return get_marshal_cb ()->emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_STRING:
		return get_marshal_cb ()->emit_marshal_string (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
#if !defined(DISABLE_COM)
		if (spec && spec->native == MONO_NATIVE_STRUCT)
			return get_marshal_cb ()->emit_marshal_variant (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

#if !defined(DISABLE_COM)
		if ((spec && (spec->native == MONO_NATIVE_IUNKNOWN ||
			spec->native == MONO_NATIVE_IDISPATCH ||
			spec->native == MONO_NATIVE_INTERFACE)) ||
			(t->type == MONO_TYPE_CLASS && mono_cominterop_is_interface(t->data.klass)))
			return mono_cominterop_emit_marshal_com_interface (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		if (spec && (spec->native == MONO_NATIVE_SAFEARRAY) && 
			(spec->data.safearray_data.elem_type == MONO_VARIANT_VARIANT) && 
			((action == MARSHAL_ACTION_CONV_OUT) || (action == MARSHAL_ACTION_CONV_IN) || (action == MARSHAL_ACTION_PUSH)))
			return mono_cominterop_emit_marshal_safearray (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

		if (mono_class_try_get_safehandle_class () != NULL && t->data.klass &&
		    mono_class_is_subclass_of_internal (t->data.klass,  mono_class_try_get_safehandle_class (), FALSE))
			return get_marshal_cb ()->emit_marshal_safehandle (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		
		return get_marshal_cb ()->emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		return get_marshal_cb ()->emit_marshal_array (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_BOOLEAN:
		return get_marshal_cb ()->emit_marshal_boolean (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_PTR:
		return get_marshal_cb ()->emit_marshal_ptr (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CHAR:
		return get_marshal_cb ()->emit_marshal_char (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_FNPTR:
		return get_marshal_cb ()->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t))
			return get_marshal_cb ()->emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		else
			return get_marshal_cb ()->emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	default:
		return conv_arg;
	}
}

static void 
mono_marshal_set_callconv_from_modopt (MonoMethod *method, MonoMethodSignature *csig, gboolean set_default)
{
	MonoMethodSignature *sig;
	int i;

#ifdef TARGET_WIN32
	/* 
	 * Under windows, delegates passed to native code must use the STDCALL
	 * calling convention.
	 */
	if (set_default)
		csig->call_convention = MONO_CALL_STDCALL;
#endif

	sig = mono_method_signature_internal (method);

	int cmod_count = 0;
	if (sig->ret)
		cmod_count = mono_type_custom_modifier_count (sig->ret);

	/* Change default calling convention if needed */
	/* Why is this a modopt ? */
	if (cmod_count == 0)
		return;

	for (i = 0; i < cmod_count; ++i) {
		ERROR_DECL (error);
		gboolean required;
		MonoType *cmod_type = mono_type_get_custom_modifier (sig->ret, i, &required, error);
		mono_error_assert_ok (error);
		MonoClass *cmod_class = mono_class_from_mono_type_internal (cmod_type);
		if ((m_class_get_image (cmod_class) == mono_defaults.corlib) && !strcmp (m_class_get_name_space (cmod_class), "System.Runtime.CompilerServices")) {
			const char *cmod_class_name = m_class_get_name (cmod_class);
			if (!strcmp (cmod_class_name, "CallConvCdecl"))
				csig->call_convention = MONO_CALL_C;
			else if (!strcmp (cmod_class_name, "CallConvStdcall"))
				csig->call_convention = MONO_CALL_STDCALL;
			else if (!strcmp (cmod_class_name, "CallConvFastcall"))
				csig->call_convention = MONO_CALL_FASTCALL;
			else if (!strcmp (cmod_class_name, "CallConvThiscall"))
				csig->call_convention = MONO_CALL_THISCALL;
		}
	}
}

/**
 * mono_marshal_get_native_wrapper:
 * \param method The \c MonoMethod to wrap.
 * \param check_exceptions Whenever to check for pending exceptions
 *
 * Generates IL code for the pinvoke wrapper. The generated method
 * calls the unmanaged code in \c piinfo->addr.
 */
MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method, gboolean check_exceptions, gboolean aot)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *) method;
	MonoMethodBuilder *mb;
	MonoMarshalSpec **mspecs;
	MonoMethod *res;
	GHashTable *cache;
	gboolean pinvoke = FALSE;
	gboolean skip_gc_trans = FALSE;
	gboolean pinvoke_not_found = FALSE;
	gpointer iter;
	int i;
	ERROR_DECL (emitted_error);
	WrapperInfo *info;

	g_assert (method != NULL);
	g_assertf (mono_method_signature_internal (method)->pinvoke, "%s flags:%X iflags:%X param_count:%X",
		method->name, method->flags, method->iflags, mono_method_signature_internal (method)->param_count);

	GHashTable **cache_ptr;

	MonoType *string_type = m_class_get_byval_arg (mono_defaults.string_class);

	if (aot) {
		if (check_exceptions)
			cache_ptr = &mono_method_get_wrapper_cache (method)->native_wrapper_aot_check_cache;
		else
			cache_ptr = &mono_method_get_wrapper_cache (method)->native_wrapper_aot_cache;
	} else {
		if (check_exceptions)
			cache_ptr = &mono_method_get_wrapper_cache (method)->native_wrapper_check_cache;
		else
			cache_ptr = &mono_method_get_wrapper_cache (method)->native_wrapper_cache;
	}

	cache = get_cache (cache_ptr, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	if (MONO_CLASS_IS_IMPORT (method->klass)) {
		/* The COM code is not AOT compatible, it calls mono_custom_attrs_get_attr_checked () */
		if (aot)
			return method;
#ifndef DISABLE_COM
		return mono_cominterop_get_native_wrapper (method);
#else
		g_assert_not_reached ();
#endif
	}

	sig = mono_method_signature_internal (method);

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		pinvoke = TRUE;

	if (!piinfo->addr) {
		if (pinvoke) {
			if (method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)
				mono_error_set_generic_error (emitted_error, "System", "MissingMethodException", "Method contains unsupported native code");
			else if (!aot)
				mono_lookup_pinvoke_call_internal (method, emitted_error);
		} else {
			if (!aot || (method->klass == mono_defaults.string_class))
				piinfo->addr = mono_lookup_internal_call (method);
		}
	}

	/* hack - redirect certain string constructors to CreateString */
	if (piinfo->addr == ves_icall_System_String_ctor_RedirectToCreateString) {
		MonoMethod *m;

		g_assert (!pinvoke);
		g_assert (method->string_ctor);
		g_assert (sig->hasthis);

		/* CreateString returns a value */
		csig = mono_metadata_signature_dup_full (get_method_image (method), sig);
		csig->ret = string_type;
		csig->pinvoke = 0;

		res = NULL;
		iter = NULL;
		while ((m = mono_class_get_methods (mono_defaults.string_class, &iter))) {
			/*
			 * Find the corresponding String::Ctor () method which has the same signature but its static
			 * and returns a string.
			 */
			if (!strcmp ("Ctor", m->name)) {
				int i;

				MonoMethodSignature *rsig = mono_method_signature_internal (m);
				if (csig->param_count == rsig->param_count) {
					for (i = 0; i < csig->param_count; ++i)
						if (!mono_metadata_type_equal (csig->params [i], rsig->params [i]))
							break;
					if (i == csig->param_count) {
						res = m;
						break;
					}
				}
			}
		}

		g_assert (res);

		WrapperInfo *info;

		g_assert (!(res->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
		g_assert (!(res->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

		/* create a wrapper to preserve .ctor in stack trace */
		mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_MANAGED);

		get_marshal_cb ()->emit_create_string_hack (mb, csig, res);

		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_STRING_CTOR);
		info->d.string_ctor.method = method;

		/* use native_wrapper_cache because internal calls are looked up there */
		res = mono_mb_create_and_cache_full (cache, method, mb, csig,
											 csig->param_count + 1, info, NULL);
		mono_mb_free (mb);

		return res;
	}

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	if (G_UNLIKELY (pinvoke && mono_method_has_unmanaged_callers_only_attribute (method))) {
		/* emit a wrapper that throws a NotSupportedException */
		get_marshal_cb ()->mb_emit_exception (mb, "System", "NotSupportedException", "Method canot be marked with both  DllImportAttribute and UnmanagedCallersOnlyAttribute");

		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
		info->d.managed_to_native.method = method;

		csig = mono_metadata_signature_dup_full (get_method_image (method), sig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache_full (cache, method, mb, csig,
											 csig->param_count + 16, info, NULL);
		mono_mb_free (mb);

		return res;
	}

	/*
	 * In AOT mode and embedding scenarios, it is possible that the icall is not
	 * registered in the runtime doing the AOT compilation.
	 */
	/* Handled at runtime */
	pinvoke_not_found = !pinvoke && !piinfo->addr && !aot;
	if (pinvoke_not_found) {
		/* if there's no code but the error isn't set, just use a fairly generic exception. */
		if (is_ok (emitted_error))
			mono_error_set_generic_error (emitted_error, "System", "MissingMethodException", "");
		get_marshal_cb ()->mb_emit_exception_for_error (mb, emitted_error);
		mono_error_cleanup (emitted_error);

		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
		info->d.managed_to_native.method = method;

		csig = mono_metadata_signature_dup_full (get_method_image (method), sig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache_full (cache, method, mb, csig,
											 csig->param_count + 16, info, NULL);
		mono_mb_free (mb);

		return res;
	}

	/* internal calls: we simply push all arguments and call the method (no conversions) */
	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		if (sig->hasthis)
			csig = mono_metadata_signature_dup_add_this (get_method_image (method), sig, method->klass);
		else
			csig = mono_metadata_signature_dup_full (get_method_image (method), sig);

		//printf ("%s\n", mono_method_full_name (method, 1));

		/* hack - string constructors returns a value */
		if (method->string_ctor)
			csig->ret = string_type;

		get_marshal_cb ()->emit_native_icall_wrapper (mb, method, csig, check_exceptions, aot, piinfo);

		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
		info->d.managed_to_native.method = method;

		csig = mono_metadata_signature_dup_full (get_method_image (method), csig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache_full (cache, method, mb, csig, csig->param_count + 16,
											 info, NULL);

		mono_mb_free (mb);
		return res;
	}

	g_assert (pinvoke);

	csig = mono_metadata_signature_dup_full (get_method_image (method), sig);
	mono_marshal_set_callconv_from_modopt (method, csig, FALSE);

	mspecs = g_new (MonoMarshalSpec*, sig->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	if (mono_class_try_get_suppress_gc_transition_attribute_class ()) {
		MonoCustomAttrInfo *cinfo;
		ERROR_DECL (error);

		cinfo = mono_custom_attrs_from_method_checked (method, error);
		mono_error_assert_ok (error);
		gboolean found = FALSE;
		if (cinfo) {
			for (i = 0; i < cinfo->num_attrs; ++i) {
				MonoClass *ctor_class = cinfo->attrs [i].ctor->klass;
				if (ctor_class == mono_class_try_get_suppress_gc_transition_attribute_class ()) {
					found = TRUE;
					break;
				}
			}
		}
		if (found)
			skip_gc_trans = TRUE;
		if (cinfo && !cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	MonoNativeWrapperFlags flags = aot ? EMIT_NATIVE_WRAPPER_AOT : (MonoNativeWrapperFlags)0;
	flags |= check_exceptions ? EMIT_NATIVE_WRAPPER_CHECK_EXCEPTIONS : (MonoNativeWrapperFlags)0;
	flags |= skip_gc_trans ? EMIT_NATIVE_WRAPPER_SKIP_GC_TRANS : (MonoNativeWrapperFlags)0;

	mono_marshal_emit_native_wrapper (get_method_image (mb->method), mb, csig, piinfo, mspecs, piinfo->addr, flags);
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_PINVOKE);
	info->d.managed_to_native.method = method;

	csig->pinvoke = 0;
	res = mono_mb_create_and_cache_full (cache, method, mb, csig, csig->param_count + 16,
										 info, NULL);
	mono_mb_free (mb);

	for (i = sig->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* mono_method_print_code (res); */

	return res;
}

/**
 * mono_marshal_get_native_func_wrapper:
 * \param image The image to use for memory allocation and for looking up custom marshallers.
 * \param sig The signature of the function
 * \param func The native function to wrap
 *
 * \returns a wrapper method around native functions, similar to the pinvoke
 * wrapper.
 */
MonoMethod *
mono_marshal_get_native_func_wrapper (MonoImage *image, MonoMethodSignature *sig, 
									  MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func)
{
	MonoMethodSignature *csig;

	SignaturePointerPair key, *new_key;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	gboolean found;
	char *name;

	key.sig = sig;
	key.pointer = func;

	// Generic types are not safe to place in MonoImage caches.
	g_assert (!sig->is_inflated);

	cache = get_cache (&image->native_func_wrapper_cache, signature_pointer_pair_hash, signature_pointer_pair_equal);
	if ((res = mono_marshal_find_in_cache (cache, &key)))
		return res;

	name = g_strdup_printf ("wrapper_native_%p", func);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	mb->method->save_lmf = 1;

	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, func, EMIT_NATIVE_WRAPPER_CHECK_EXCEPTIONS);

	csig = mono_metadata_signature_dup_full (image, sig);
	csig->pinvoke = 0;

	new_key = g_new (SignaturePointerPair,1);
	new_key->sig = csig;
	new_key->pointer = func;

	res = mono_mb_create_and_cache_full (cache, new_key, mb, csig, csig->param_count + 16, NULL, &found);
	if (found)
		g_free (new_key);

	mono_mb_free (mb);

	mono_marshal_set_wrapper_info (res, NULL);

	return res;
}

/*
 * The wrapper receives the native function as a boxed IntPtr as its 'this' argument. This is easier to support in
 * AOT.
 */
MonoMethod*
mono_marshal_get_native_func_wrapper_aot (MonoClass *klass)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;
	WrapperInfo *info;
	MonoMethodPInvoke mpiinfo;
	MonoMethodPInvoke *piinfo = &mpiinfo;
	MonoMarshalSpec **mspecs;
	MonoMethod *invoke = mono_get_delegate_invoke_internal (klass);
	MonoImage *image = get_method_image (invoke);
	int i;

	// FIXME: include UnmanagedFunctionPointerAttribute info

	/*
	 * The wrapper is associated with the delegate type, to pick up the marshalling info etc.
	 */
	cache = get_cache (&mono_method_get_wrapper_cache (invoke)->native_func_wrapper_aot_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, invoke)))
		return res;

	memset (&mpiinfo, 0, sizeof (mpiinfo));
	parse_unmanaged_function_pointer_attr (klass, &mpiinfo);

	mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature_internal (invoke)->param_count + 1);
	mono_method_get_marshal_info (invoke, mspecs);
	/* Freed below so don't alloc from mempool */
	sig = mono_metadata_signature_dup (mono_method_signature_internal (invoke));
	sig->hasthis = 0;

	name = g_strdup_printf ("wrapper_aot_native");
	mb = mono_mb_new (invoke->klass, name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	mb->method->save_lmf = 1;

	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, NULL, EMIT_NATIVE_WRAPPER_CHECK_EXCEPTIONS | EMIT_NATIVE_WRAPPER_FUNC_PARAM);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NATIVE_FUNC_AOT);
	info->d.managed_to_native.method = invoke;

	g_assert (!sig->hasthis);
	csig = mono_metadata_signature_dup_add_this (image, sig, mono_defaults.object_class);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache_full (cache, invoke,
										 mb, csig, csig->param_count + 16,
										 info, NULL);
	mono_mb_free (mb);

	for (i = mono_method_signature_internal (invoke)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);
	g_free (sig);

	return res;
}

/*
 * Gets a wrapper for an indirect call to a function with the given signature.
 * The actual function is passed as the first argument to the wrapper.
 *
 * The wrapper is
 *
 * retType wrapper (fnPtr, arg1... argN) {
 *   enter_gc_safe;
 *   ret = fnPtr (arg1, ... argN);
 *   exit_gc_safe;
 *   return ret;
 * }
 *
 */
MonoMethod*
mono_marshal_get_native_func_wrapper_indirect (MonoClass *caller_class, MonoMethodSignature *sig,
					       gboolean aot)
{
	caller_class = mono_class_get_generic_type_definition (caller_class);
	MonoImage *image = m_class_get_image (caller_class);
	g_assert (sig->pinvoke);
	g_assert (!sig->hasthis && ! sig->explicit_this);
	g_assert (!sig->is_inflated && !sig->has_type_parameters);

#if 0
	/*
	 * Since calli sigs are already part of ECMA-335, they were already used by C++/CLI, which
	 * allowed non-blittable types.  So the C# function pointers spec doesn't restrict this to
	 * blittable tyhpes only.
	 */
	g_assertf (type_is_blittable (sig->ret), "sig return type %s is not blittable\n", mono_type_full_name (sig->ret));

	for (int i = 0; i < sig->param_count; ++i) {
		MonoType *ty = sig->params [i];
		g_assertf (type_is_blittable (ty), "sig param %d (type %s) is not blittable\n", i, mono_type_full_name (ty));
	}
#endif

	GHashTable *cache = get_cache (&image->wrapper_caches.native_func_wrapper_indirect_cache,
				       (GHashFunc)mono_signature_hash, 
				       (GCompareFunc)mono_metadata_signature_equal);
	
	MonoMethod *res;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
	    return res;
	
#if 0
	fprintf (stderr, "generating wrapper for signature %s\n", mono_signature_full_name (sig));
#endif
	
	/* FIXME: better wrapper name */
	char * name = g_strdup_printf ("wrapper_native_indirect_%p", sig);
	MonoMethodBuilder *mb = mono_mb_new (caller_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	mb->method->save_lmf = 1;

	WrapperInfo *info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NATIVE_FUNC_INDIRECT);
	info->d.managed_to_native.method = NULL;

	MonoMethodPInvoke *piinfo = NULL;
	MonoMarshalSpec **mspecs = g_new0 (MonoMarshalSpec *, 1 + sig->param_count);
	MonoNativeWrapperFlags flags = aot ? EMIT_NATIVE_WRAPPER_AOT : (MonoNativeWrapperFlags)0;
	flags |= EMIT_NATIVE_WRAPPER_FUNC_PARAM | EMIT_NATIVE_WRAPPER_FUNC_PARAM_UNBOXED;
	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, /*func*/NULL, flags);
	g_free (mspecs);

	MonoMethodSignature *csig = mono_metadata_signature_dup_add_this (image, sig, mono_defaults.int_class);
	csig->pinvoke = 0;

	MonoMethodSignature *key_sig = mono_metadata_signature_dup_full (image, sig);

	gboolean found;
	res = mono_mb_create_and_cache_full (cache, key_sig, mb, csig, csig->param_count + 16, info, &found);

	mono_mb_free (mb);

	return res;
}

/*
 * mono_marshal_emit_managed_wrapper:
 *
 *   Emit the body of a native-to-managed wrapper. INVOKE_SIG is the signature of
 * the delegate which wraps the managed method to be called. For closed delegates,
 * it could have fewer parameters than the method it wraps.
 * THIS_LOC is the memory location where the target of the delegate is stored.
 */
void
mono_marshal_emit_managed_wrapper (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, MonoGCHandle target_handle)
{
	get_marshal_cb ()->emit_managed_wrapper (mb, invoke_sig, mspecs, m, method, target_handle);
}

static gboolean
type_is_blittable (MonoType *type)
{
	if (type->byref)
		return FALSE;
	type = mono_type_get_underlying_type (type);
	switch (type->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_VOID:
		return TRUE;
	default: {
		MonoClass *klass = mono_class_from_mono_type_internal (type);
		mono_class_init_sizes (klass);
		return m_class_is_blittable (klass);
	}
	}
}

static gboolean
method_signature_is_blittable (MonoMethodSignature *sig)
{
	if (!type_is_blittable (sig->ret))
		return FALSE;

	for (int i = 0; i < sig->param_count; ++i) {
		MonoType *type = sig->params [i];
		if (!type_is_blittable (type))
			return FALSE;
	}
	return TRUE;
}

/**
 * mono_marshal_get_managed_wrapper:
 * Generates IL code to call managed methods from unmanaged code 
 * If \p target_handle is \c 0, the wrapper info will be a \c WrapperInfo structure.
 *
 * If \p delegate_klass is \c NULL, we're creating a wrapper for a function pointer to a method marked with
 * UnamangedCallersOnlyAttribute.
 */
MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, MonoGCHandle target_handle, MonoError *error)
{
	MonoMethodSignature *sig, *csig, *invoke_sig;
	MonoMethodBuilder *mb;
	MonoMethod *res, *invoke;
	MonoMarshalSpec **mspecs;
	MonoMethodPInvoke piinfo;
	GHashTable *cache;
	int i;
	EmitMarshalContext m;

	g_assert (method != NULL);
	error_init (error);

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		mono_error_set_invalid_program (error, "Failed because method (%s) marked PInvokeCallback (managed method) and extern (unmanaged) simultaneously.", mono_method_full_name (method, TRUE));
		return NULL;
	}

	/* 
	 * FIXME: Should cache the method+delegate type pair, since the same method
	 * could be called with different delegates, thus different marshalling
	 * options.
	 */
	cache = get_cache (&mono_method_get_wrapper_cache (method)->managed_wrapper_cache, mono_aligned_addr_hash, NULL);

	if (!target_handle && (res = mono_marshal_find_in_cache (cache, method)))
		return res;

	if (G_UNLIKELY (!delegate_klass)) {
		/* creating a wrapper for a function pointer with UnmanagedCallersOnlyAttribute */
		if (mono_method_has_marshal_info (method)) {
			mono_error_set_invalid_program (error, "method %s with UnmanadedCallersOnlyAttribute has marshal specs", mono_method_full_name (method, TRUE));
			return NULL;
		}
		invoke = NULL;
		invoke_sig = mono_method_signature_internal (method);
		if (invoke_sig->hasthis) {
			mono_error_set_invalid_program (error, "method %s with UnamanagedCallersOnlyAttribute is an instance method", mono_method_full_name (method, TRUE));
			return NULL;
		}
		if (method->is_generic || method->is_inflated || mono_class_is_ginst (method->klass)) {
			mono_error_set_invalid_program (error, "method %s with UnamangedCallersOnlyAttribute is generic", mono_method_full_name (method, TRUE));
			return NULL;
		}
		if (!method_signature_is_blittable (invoke_sig)) {
			mono_error_set_invalid_program (error, "method %s with UnmanagedCallersOnlyAttribute has non-blittable parameters or return type", mono_method_full_name (method, TRUE));
			return NULL;
		}
		mspecs = g_new0 (MonoMarshalSpec*, invoke_sig->param_count + 1);
	} else {
		invoke = mono_get_delegate_invoke_internal (delegate_klass);
		invoke_sig = mono_method_signature_internal (invoke);
		mspecs = g_new0 (MonoMarshalSpec*, invoke_sig->param_count + 1);
		mono_method_get_marshal_info (invoke, mspecs);
	}

	sig = mono_method_signature_internal (method);

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);

	/*the target gchandle must be the first entry after size and the wrapper itself.*/
	mono_mb_add_data (mb, target_handle);

	/* we copy the signature, so that we can modify it */
	if (target_handle)
		/* Need to free this later */
		csig = mono_metadata_signature_dup (invoke_sig);
	else
		csig = mono_metadata_signature_dup_full (get_method_image (method), invoke_sig);
	csig->hasthis = 0;
	csig->pinvoke = 1;

	memset (&m, 0, sizeof (m));
	m.mb = mb;
	m.sig = sig;
	m.piinfo = NULL;
	m.retobj_var = 0;
	m.csig = csig;
	m.image = get_method_image (method);

	if (invoke)
		mono_marshal_set_callconv_from_modopt (invoke, csig, TRUE);

	/* The attribute is only available in Net 2.0 */
	if (delegate_klass && mono_class_try_get_unmanaged_function_pointer_attribute_class ()) {
		MonoCustomAttrInfo *cinfo;
		MonoCustomAttrEntry *attr;

		/* 
		 * The pinvoke attributes are stored in a real custom attribute. Obtain the
		 * contents of the attribute without constructing it, as that might not be
		 * possible when running in cross-compiling mode.
		 */
		cinfo = mono_custom_attrs_from_class_checked (delegate_klass, error);
		mono_error_assert_ok (error);
		attr = NULL;
		if (cinfo) {
			for (i = 0; i < cinfo->num_attrs; ++i) {
				MonoClass *ctor_class = cinfo->attrs [i].ctor->klass;
				if (mono_class_has_parent (ctor_class, mono_class_try_get_unmanaged_function_pointer_attribute_class ())) {
					attr = &cinfo->attrs [i];
					break;
				}
			}
		}
		if (attr) {
			gpointer *typed_args, *named_args;
			CattrNamedArg *arginfo;
			gint32 call_conv;
			gint32 charset = 0;
			MonoBoolean set_last_error = 0;
			int num_named_args;
			ERROR_DECL (error);

			mono_reflection_create_custom_attr_data_args_noalloc (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size,
																  &typed_args, &named_args, &num_named_args, &arginfo, error);
			g_assert (is_ok (error));

			/* typed args */
			call_conv = *(gint32*)typed_args [0];
			/* named args */
			for (i = 0; i < num_named_args; ++i) {
				CattrNamedArg *narg = &arginfo [i];

				g_assert (narg->field);
				if (!strcmp (narg->field->name, "CharSet")) {
					charset = *(gint32*)named_args [i];
				} else if (!strcmp (narg->field->name, "SetLastError")) {
					set_last_error = *(MonoBoolean*)named_args [i];
				} else if (!strcmp (narg->field->name, "BestFitMapping")) {
					// best_fit_mapping = *(MonoBoolean*)mono_object_unbox_internal (o);
				} else if (!strcmp (narg->field->name, "ThrowOnUnmappableChar")) {
					// throw_on_unmappable = *(MonoBoolean*)mono_object_unbox_internal (o);
				} else {
					g_assert_not_reached ();
				}
				g_free (named_args [i]);
			}
			g_free (typed_args [0]);
			g_free (typed_args);
			g_free (named_args);
			g_free (arginfo);

			memset (&piinfo, 0, sizeof (piinfo));
			m.piinfo = &piinfo;
			piinfo.piflags = (call_conv << 8) | (charset ? (charset - 1) * 2 : 1) | set_last_error;

			csig->call_convention = call_conv - 1;
		}

		if (cinfo && !cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	mono_marshal_emit_managed_wrapper (mb, invoke_sig, mspecs, &m, method, target_handle);

	if (!target_handle) {
		WrapperInfo *info;

		// FIXME: Associate it with the method+delegate_klass pair
		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
		info->d.native_to_managed.method = method;
		info->d.native_to_managed.klass = delegate_klass;

		res = mono_mb_create_and_cache_full (cache, method,
											 mb, csig, sig->param_count + 16,
											 info, NULL);
	} else {
		get_marshal_cb ()->mb_set_dynamic (mb);
		res = mono_mb_create (mb, csig, sig->param_count + 16, NULL);
	}
	mono_mb_free (mb);

	for (i = invoke_sig->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* mono_method_print_code (res); */

	return res;
}

gpointer
mono_marshal_get_vtfixup_ftnptr (MonoImage *image, guint32 token, guint16 type)
{
	ERROR_DECL (error);
	MonoMethod *method;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	int i, param_count;

	g_assert (token);

	method = mono_get_method_checked (image, token, NULL, NULL, error);
	if (!method)
		g_error ("Could not load vtfixup token 0x%x due to %s", token, mono_error_get_message (error));
	g_assert (method);

	if (type & (VTFIXUP_TYPE_FROM_UNMANAGED | VTFIXUP_TYPE_FROM_UNMANAGED_RETAIN_APPDOMAIN)) {
		MonoMethodSignature *csig;
		MonoMarshalSpec **mspecs;
		EmitMarshalContext m;

		sig = mono_method_signature_internal (method);
		g_assert (!sig->hasthis);

		mspecs = g_new0 (MonoMarshalSpec*, sig->param_count + 1);
		mono_method_get_marshal_info (method, mspecs);

		mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);
		csig = mono_metadata_signature_dup_full (image, sig);
		csig->hasthis = 0;
		csig->pinvoke = 1;

		memset (&m, 0, sizeof (m));
		m.mb = mb;
		m.sig = sig;
		m.piinfo = NULL;
		m.retobj_var = 0;
		m.csig = csig;
		m.image = image;

		mono_marshal_set_callconv_from_modopt (method, csig, TRUE);

		/* FIXME: Implement VTFIXUP_TYPE_FROM_UNMANAGED_RETAIN_APPDOMAIN. */

		mono_marshal_emit_managed_wrapper (mb, sig, mspecs, &m, method, 0);

		get_marshal_cb ()->mb_set_dynamic (mb);
		method = mono_mb_create (mb, csig, sig->param_count + 16, NULL);
		mono_mb_free (mb);

		for (i = sig->param_count; i >= 0; i--)
			if (mspecs [i])
				mono_metadata_free_marshal_spec (mspecs [i]);
		g_free (mspecs);

		gpointer compiled_ptr = mono_compile_method_checked (method, error);
		mono_error_assert_ok (error);
		return compiled_ptr;
	}

	sig = mono_method_signature_internal (method);
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_MANAGED);

	param_count = sig->param_count + sig->hasthis;
	get_marshal_cb ()->emit_vtfixup_ftnptr (mb, method, param_count, type);
	get_marshal_cb ()->mb_set_dynamic (mb);

	method = mono_mb_create (mb, sig, param_count, NULL);
	mono_mb_free (mb);

	gpointer compiled_ptr = mono_compile_method_checked (method, error);
	mono_error_assert_ok (error);
	return compiled_ptr;
}

/**
 * mono_marshal_get_castclass_with_cache:
 * This does the equivalent of \c mono_object_castclass_with_cache.
 */
MonoMethod *
mono_marshal_get_castclass_with_cache (void)
{
	static MonoMethod *cached;
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	WrapperInfo *info;

	if (cached)
		return cached;

	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();

	mb = mono_mb_new (mono_defaults.object_class, "__castclass_with_cache", MONO_WRAPPER_CASTCLASS);
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	sig->params [TYPECHECK_OBJECT_ARG_POS] = object_type;
	sig->params [TYPECHECK_CLASS_ARG_POS] = int_type;
	sig->params [TYPECHECK_CACHE_ARG_POS] = int_type;
	sig->ret = object_type;
	sig->pinvoke = 0;

	get_marshal_cb ()->emit_castclass (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_CASTCLASS_WITH_CACHE);
	res = mono_mb_create (mb, sig, 8, info);
	STORE_STORE_FENCE;

	if (mono_atomic_cas_ptr ((volatile gpointer *)&cached, res, NULL)) {
		mono_free_method (res);
		mono_metadata_free_method_signature (sig);
	}
	mono_mb_free (mb);

	return cached;
}

/* this is an icall */
MonoObject *
mono_marshal_isinst_with_cache (MonoObject *obj, MonoClass *klass, uintptr_t *cache)
{
	ERROR_DECL (error);
	MonoObject *isinst = mono_object_isinst_checked (obj, klass, error);
	if (mono_error_set_pending_exception (error))
		return NULL;

	uintptr_t cache_update = (uintptr_t)obj->vtable;
	if (!isinst)
		cache_update = cache_update | 0x1;

	*cache = cache_update;

	return isinst;
}

/**
 * mono_marshal_get_isinst_with_cache:
 * This does the equivalent of \c mono_marshal_isinst_with_cache.
 */
MonoMethod *
mono_marshal_get_isinst_with_cache (void)
{
	static MonoMethod *cached;
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	WrapperInfo *info;

	if (cached)
		return cached;

	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();

	mb = mono_mb_new (mono_defaults.object_class, "__isinst_with_cache", MONO_WRAPPER_CASTCLASS);
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	// The object
	sig->params [TYPECHECK_OBJECT_ARG_POS] = object_type;
	// The class
	sig->params [TYPECHECK_CLASS_ARG_POS] = int_type;
	// The cache
	sig->params [TYPECHECK_CACHE_ARG_POS] = int_type;
	sig->ret = object_type;
	sig->pinvoke = 0;

	get_marshal_cb ()->emit_isinst (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_ISINST_WITH_CACHE);
	res = mono_mb_create (mb, sig, 8, info);
	STORE_STORE_FENCE;

	if (mono_atomic_cas_ptr ((volatile gpointer *)&cached, res, NULL)) {
		mono_free_method (res);
		mono_metadata_free_method_signature (sig);
	}
	mono_mb_free (mb);

	return cached;
}

/**
 * mono_marshal_get_struct_to_ptr:
 * \param klass \c MonoClass
 *
 * Generates IL code for <code>StructureToPtr (object structure, IntPtr ptr, bool fDeleteOld)</code>
 */
MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	MonoMethod *res;
	WrapperInfo *info;

	g_assert (klass != NULL);

	mono_marshal_load_type_info (klass);

	MonoMarshalType *marshal_info = mono_class_get_marshal_info (klass);

	if ((res = marshal_info->str_to_ptr))
		return res;

	MONO_STATIC_POINTER_INIT (MonoMethod, stoptr)

		ERROR_DECL (error);
		stoptr = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "StructureToPtr", 3, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, stoptr)

	g_assert (stoptr);

	mb = mono_mb_new (klass, stoptr->name, MONO_WRAPPER_OTHER);

	get_marshal_cb ()->emit_struct_to_ptr (mb, klass);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_STRUCTURE_TO_PTR);
	res = mono_mb_create (mb, mono_signature_no_pinvoke (stoptr), 0, info);
	mono_mb_free (mb);

	mono_marshal_lock ();
	if (!marshal_info->str_to_ptr)
		marshal_info->str_to_ptr = res;
	else
		res = marshal_info->str_to_ptr;
	mono_marshal_unlock ();
	return res;
}

/**
 * mono_marshal_get_ptr_to_struct:
 * \param klass \c MonoClass
 * Generates IL code for <code>PtrToStructure (IntPtr src, object structure)</code>
 */
MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethodSignature *ptostr = NULL;
	MonoMethod *res;
	WrapperInfo *info;

	g_assert (klass != NULL);

	mono_marshal_load_type_info (klass);

	MonoMarshalType *marshal_info = mono_class_get_marshal_info (klass);
	if (marshal_info->ptr_to_str)
		return marshal_info->ptr_to_str;

	if (!ptostr) {
		MonoMethodSignature *sig;

		/* Create the signature corresponding to
		 	  static void PtrToStructure (IntPtr ptr, object structure);
		   defined in class/corlib/System.Runtime.InteropServices/Marshal.cs */
		sig = mono_icall_sig_void_ptr_object;
		sig = mono_metadata_signature_dup_full (mono_defaults.corlib, sig);
		sig->pinvoke = 0;
		mono_memory_barrier ();
		ptostr = sig;
	}

	mb = mono_mb_new (klass, "PtrToStructure", MONO_WRAPPER_OTHER);

	get_marshal_cb ()->emit_ptr_to_struct (mb, klass);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_PTR_TO_STRUCTURE);
	res = mono_mb_create (mb, ptostr, 0, info);
	mono_mb_free (mb);

	mono_marshal_lock ();
	if (!marshal_info->ptr_to_str)
		marshal_info->ptr_to_str = res;
	else
		res = marshal_info->ptr_to_str;
	mono_marshal_unlock ();
	return res;
}

/*
 * Return a dummy wrapper for METHOD which is called by synchronized wrappers.
 * This is used to avoid infinite recursion since it is hard to determine where to
 * replace a method with its synchronized wrapper, and where not.
 * The runtime should execute METHOD instead of the wrapper.
 */
MonoMethod *
mono_marshal_get_synchronized_inner_wrapper (MonoMethod *method)
{
	MonoMethodBuilder *mb;
	WrapperInfo *info;
	MonoMethodSignature *sig;
	MonoMethod *res;
	MonoGenericContext *ctx = NULL;
	MonoGenericContainer *container = NULL;

	if (method->is_inflated && !mono_method_get_context (method)->method_inst) {
		ctx = &((MonoMethodInflated*)method)->context;
		method = ((MonoMethodInflated*)method)->declaring;
		container = mono_method_get_generic_container (method);
		if (!container)
			container = mono_class_try_get_generic_container (method->klass); //FIXME is this a case of a try?
		g_assert (container);
	}

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_OTHER);
	get_marshal_cb ()->mb_emit_exception (mb, "System", "ExecutionEngineException", "Shouldn't be called.");
	get_marshal_cb ()->mb_emit_byte (mb, CEE_RET);

	sig = mono_metadata_signature_dup_full (get_method_image (method), mono_method_signature_internal (method));

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_SYNCHRONIZED_INNER);
	info->d.synchronized_inner.method = method;
	res = mono_mb_create (mb, sig, 0, info);
	mono_mb_free (mb);
	if (ctx) {
		ERROR_DECL (error);
		res = mono_class_inflate_generic_method_checked (res, ctx, error);
		g_assert (is_ok (error)); /* FIXME don't swallow the error */
	}
	return res;
}

/**
 * mono_marshal_get_synchronized_wrapper:
 * Generates IL code for the synchronized wrapper: the generated method
 * calls \p method while locking \c this or the parent type.
 */
MonoMethod *
mono_marshal_get_synchronized_wrapper (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	WrapperInfo *info;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;
	MonoGenericContainer *container = NULL;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED)
		return method;

	/* FIXME: Support generic methods too */
	if (method->is_inflated && !mono_method_get_context (method)->method_inst) {
		orig_method = method;
		ctx = &((MonoMethodInflated*)method)->context;
		method = ((MonoMethodInflated*)method)->declaring;
		container = mono_method_get_generic_container (method);
		if (!container)
			container = mono_class_try_get_generic_container (method->klass); //FIXME is this a case of a try?
		g_assert (container);
	}

	/*
	 * Check cache
	 */
	if (ctx) {
		cache = get_cache (&((MonoMethodInflated*)orig_method)->owner->wrapper_caches.synchronized_cache, mono_aligned_addr_hash, NULL);
		res = check_generic_wrapper_cache (cache, orig_method, orig_method, method);
		if (res)
			return res;
	} else {
		cache = get_cache (&get_method_image (method)->wrapper_caches.synchronized_cache, mono_aligned_addr_hash, NULL);
		if ((res = mono_marshal_find_in_cache (cache, method)))
			return res;
	}

	sig = mono_metadata_signature_dup_full (get_method_image (method), mono_method_signature_internal (method));
	sig->pinvoke = 0;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_SYNCHRONIZED);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.synchronized.method = method;

	mono_marshal_lock ();

	MONO_STATIC_POINTER_INIT (MonoMethod, enter_method)
		MonoMethodDesc *desc = mono_method_desc_new ("Monitor:Enter(object,bool&)", FALSE);
		enter_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (enter_method);
		mono_method_desc_free (desc);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, enter_method)

	MONO_STATIC_POINTER_INIT (MonoMethod, exit_method)
		MonoMethodDesc *desc = mono_method_desc_new ("Monitor:Exit", FALSE);
		exit_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (exit_method);
		mono_method_desc_free (desc);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, exit_method)

	MONO_STATIC_POINTER_INIT (MonoMethod, gettypefromhandle_method)
		MonoMethodDesc *desc = mono_method_desc_new ("Type:GetTypeFromHandle", FALSE);
		gettypefromhandle_method = mono_method_desc_search_in_class (desc, mono_defaults.systemtype_class);
		g_assert (gettypefromhandle_method);
		mono_method_desc_free (desc);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, gettypefromhandle_method)

	mono_marshal_unlock ();

	get_marshal_cb ()->mb_skip_visibility (mb);
	get_marshal_cb ()->emit_synchronized_wrapper (mb, method, ctx, container, enter_method, exit_method, gettypefromhandle_method);

	if (ctx) {
		MonoMethod *def;
		def = mono_mb_create_and_cache_full (cache, method, mb, sig, sig->param_count + 16, info, NULL);
		res = cache_generic_wrapper (cache, orig_method, def, ctx, orig_method);
	} else {
		res = mono_mb_create_and_cache_full (cache, method,
											 mb, sig, sig->param_count + 16, info, NULL);
	}
	mono_mb_free (mb);

	return res;	
}

/**
 * mono_marshal_get_unbox_wrapper:
 * The returned method calls \p method unboxing the \c this argument.
 */
MonoMethod *
mono_marshal_get_unbox_wrapper (MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	WrapperInfo *info;

	cache = get_cache (&mono_method_get_wrapper_cache (method)->unbox_wrapper_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_UNBOX);

	g_assert (sig->hasthis);
	
	get_marshal_cb ()->emit_unbox_wrapper (mb, method);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.unbox.method = method;

	res = mono_mb_create_and_cache_full (cache, method,
										 mb, sig, sig->param_count + 16, info, NULL);
	mono_mb_free (mb);

	/* mono_method_print_code (res); */

	return res;	
}

static gboolean
is_monomorphic_array (MonoClass *klass)
{
	MonoClass *element_class;
	if (m_class_get_rank (klass) != 1)
		return FALSE;

	element_class = m_class_get_element_class (klass);
	return mono_class_is_sealed (element_class) || m_class_is_valuetype (element_class);
}

static MonoStelemrefKind
get_virtual_stelemref_kind (MonoClass *element_class)
{
	if (element_class == mono_defaults.object_class)
		return STELEMREF_OBJECT;
	if (is_monomorphic_array (element_class))
		return STELEMREF_SEALED_CLASS;

	/* magic ifaces requires aditional checks for when the element type is an array */
	if (MONO_CLASS_IS_INTERFACE_INTERNAL (element_class) && m_class_is_array_special_interface (element_class))
		return STELEMREF_COMPLEX;

	/* Compressed interface bitmaps require code that is quite complex, so don't optimize for it. */
	if (MONO_CLASS_IS_INTERFACE_INTERNAL (element_class) && !mono_class_has_variant_generic_params (element_class))
#ifdef COMPRESSED_INTERFACE_BITMAP
		return STELEMREF_COMPLEX;
#else
		return STELEMREF_INTERFACE;
#endif
	/*Arrays are sealed but are covariant on their element type, We can't use any of the fast paths.*/
	if (m_class_get_rank (element_class) || mono_class_has_variant_generic_params (element_class))
		return STELEMREF_COMPLEX;
	if (mono_class_is_sealed (element_class))
		return STELEMREF_SEALED_CLASS;
	if (m_class_get_idepth (element_class) <= MONO_DEFAULT_SUPERTABLE_SIZE)
		return STELEMREF_CLASS_SMALL_IDEPTH;

	return STELEMREF_CLASS;
}

#if 0
static void
record_slot_vstore (MonoObject *array, size_t index, MonoObject *value)
{
	char *name = mono_type_get_full_name (m_class_element_class (mono_object_class (array)));
	printf ("slow vstore of %s\n", name);
	g_free (name);
}
#endif

static const char *strelemref_wrapper_name[] = {
	"object", "sealed_class", "class", "class_small_idepth", "interface", "complex"
};

static const gchar *
mono_marshal_get_strelemref_wrapper_name (MonoStelemrefKind kind)
{
	return strelemref_wrapper_name [kind];
}

/*
 * TODO:
 *	- Separate simple interfaces from variant interfaces or mbr types. This way we can avoid the icall for them.
 *	- Emit a (new) mono bytecode that produces OP_COND_EXC_NE_UN to raise ArrayTypeMismatch
 *	- Maybe mve some MonoClass field into the vtable to reduce the number of loads
 *	- Add a case for arrays of arrays.
 */
MonoMethod*
mono_marshal_get_virtual_stelemref_wrapper (MonoStelemrefKind kind)
{
	static MonoMethod *cached_methods [STELEMREF_KIND_COUNT] = { NULL }; /*object iface sealed regular*/
	static MonoMethodSignature *signature;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	char *name;
	const char *param_names [16];
	WrapperInfo *info;

	if (cached_methods [kind])
		return cached_methods [kind];

	MonoType *void_type = mono_get_void_type ();
	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();

	name = g_strdup_printf ("virt_stelemref_%s", mono_marshal_get_strelemref_wrapper_name (kind));
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_STELEMREF);
	g_free (name);

	if (!signature) {
		MonoMethodSignature *sig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);

		/* void this::stelemref (size_t idx, void* value) */
		sig->ret = void_type;
		sig->hasthis = TRUE;
		sig->params [0] = int_type; /* this is a natural sized int */
		sig->params [1] = object_type;
		signature = sig;
	}

	param_names [0] = "index";
	param_names [1] = "value";
	get_marshal_cb ()->emit_virtual_stelemref (mb, param_names, kind);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_VIRTUAL_STELEMREF);
	info->d.virtual_stelemref.kind = kind;
	res = mono_mb_create (mb, signature, 4, info);
	res->flags |= METHOD_ATTRIBUTE_VIRTUAL;

	mono_marshal_lock ();
	if (!cached_methods [kind]) {
		cached_methods [kind] = res;
		mono_marshal_unlock ();
	} else {
		mono_marshal_unlock ();
		mono_free_method (res);
	}

	mono_mb_free (mb);
	return cached_methods [kind];
}

MonoMethod*
mono_marshal_get_virtual_stelemref (MonoClass *array_class)
{
	MonoStelemrefKind kind;

	g_assert (m_class_get_rank (array_class) == 1);
	kind = get_virtual_stelemref_kind (m_class_get_element_class (array_class));

	return mono_marshal_get_virtual_stelemref_wrapper (kind);
}

MonoMethod**
mono_marshal_get_virtual_stelemref_wrappers (int *nwrappers)
{
	MonoMethod **res;
	int i;

	*nwrappers = STELEMREF_KIND_COUNT;
	res = (MonoMethod **)g_malloc0 (STELEMREF_KIND_COUNT * sizeof (MonoMethod*));
	for (i = 0; i < STELEMREF_KIND_COUNT; ++i)
		res [i] = mono_marshal_get_virtual_stelemref_wrapper ((MonoStelemrefKind)i);
	return res;
}

/**
 * mono_marshal_get_stelemref:
 */
MonoMethod*
mono_marshal_get_stelemref (void)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	WrapperInfo *info;

	MONO_STATIC_POINTER_INIT (MonoMethod, ret)
	
	mb = mono_mb_new (mono_defaults.object_class, "stelemref", MONO_WRAPPER_STELEMREF);

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);

	MonoType *void_type = mono_get_void_type ();
	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();


	/* void stelemref (void* array, int idx, void* value) */
	sig->ret = void_type;
	sig->params [0] = object_type;
	sig->params [1] = int_type; /* this is a natural sized int */
	sig->params [2] = object_type;

	get_marshal_cb ()->emit_stelemref (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, ret)

	return ret;
}

/*
 * mono_marshal_get_gsharedvt_in_wrapper:
 *
 *   This wrapper handles calls from normal code to gsharedvt code.
 */
MonoMethod*
mono_marshal_get_gsharedvt_in_wrapper (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, ret)

	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	WrapperInfo *info;
	
	mb = mono_mb_new (mono_defaults.object_class, "gsharedvt_in", MONO_WRAPPER_OTHER);
	
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	sig->ret = mono_get_void_type ();

	/*
	 * The body is generated by the JIT, we use a wrapper instead of a trampoline so EH works.
	 */
	get_marshal_cb ()->mb_emit_byte (mb, CEE_RET);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GSHAREDVT_IN);
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, ret)

	return ret;
}

/*
 * mono_marshal_get_gsharedvt_out_wrapper:
 *
 *   This wrapper handles calls from gsharedvt code to normal code.
 */
MonoMethod*
mono_marshal_get_gsharedvt_out_wrapper (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, ret)

	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	WrapperInfo *info;
	
	mb = mono_mb_new (mono_defaults.object_class, "gsharedvt_out", MONO_WRAPPER_OTHER);
	
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	sig->ret = mono_get_void_type ();

	/*
	 * The body is generated by the JIT, we use a wrapper instead of a trampoline so EH works.
	 */
	get_marshal_cb ()->mb_emit_byte (mb, CEE_RET);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GSHAREDVT_OUT);
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, ret)

	return ret;
}

typedef struct {
	int rank;
	int elem_size;
	MonoMethod *method;
} ArrayElemAddr;

/* LOCKING: vars accessed under the marshal lock */
static ArrayElemAddr *elem_addr_cache = NULL;
static int elem_addr_cache_size = 0;
static int elem_addr_cache_next = 0;

/**
 * mono_marshal_get_array_address:
 * \param rank rank of the array type
 * \param elem_size size in bytes of an element of an array.
 *
 * Returns a MonoMethod that implements the code to get the address
 * of an element in a multi-dimenasional array of \p rank dimensions.
 * The returned method takes an array as the first argument and then
 * \p rank indexes for the \p rank dimensions.
 * If ELEM_SIZE is 0, read the array size from the array object.
 */
MonoMethod*
mono_marshal_get_array_address (int rank, int elem_size)
{
	MonoMethod *ret;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	WrapperInfo *info;
	char *name;
	int cached;

	ret = NULL;
	mono_marshal_lock ();
	for (int i = 0; i < elem_addr_cache_next; ++i) {
		if (elem_addr_cache [i].rank == rank && elem_addr_cache [i].elem_size == elem_size) {
			ret = elem_addr_cache [i].method;
			break;
		}
	}
	mono_marshal_unlock ();
	if (ret)
		return ret;

	MonoType *object_type = mono_get_object_type ();
	MonoType *int_type = mono_get_int_type ();
	MonoType *int32_type = mono_get_int32_type ();

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1 + rank);

	/* void* address (void* array, int idx0, int idx1, int idx2, ...) */
	sig->ret = int_type;
	sig->params [0] = object_type;
	for (int i = 0; i < rank; ++i) {
		sig->params [i + 1] = int32_type;
	}

	name = g_strdup_printf ("ElementAddr_%d", elem_size);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_MANAGED);
	g_free (name);
	
	get_marshal_cb ()->emit_array_address (mb, rank, elem_size);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_ELEMENT_ADDR);
	info->d.element_addr.rank = rank;
	info->d.element_addr.elem_size = elem_size;
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

	/* cache the result */
	cached = 0;
	mono_marshal_lock ();
	for (int i = 0; i < elem_addr_cache_next; ++i) {
		if (elem_addr_cache [i].rank == rank && elem_addr_cache [i].elem_size == elem_size) {
			/* FIXME: free ret */
			ret = elem_addr_cache [i].method;
			cached = TRUE;
			break;
		}
	}
	if (!cached) {
		if (elem_addr_cache_next >= elem_addr_cache_size) {
			int new_size = elem_addr_cache_size + 4;
			ArrayElemAddr *new_array = g_new0 (ArrayElemAddr, new_size);
			memcpy (new_array, elem_addr_cache, elem_addr_cache_size * sizeof (ArrayElemAddr));
			g_free (elem_addr_cache);
			elem_addr_cache = new_array;
			elem_addr_cache_size = new_size;
		}
		elem_addr_cache [elem_addr_cache_next].rank = rank;
		elem_addr_cache [elem_addr_cache_next].elem_size = elem_size;
		elem_addr_cache [elem_addr_cache_next].method = ret;
		elem_addr_cache_next ++;
	}
	mono_marshal_unlock ();
	return ret;
}

/*
 * mono_marshal_get_array_accessor_wrapper:
 *
 *   Return a wrapper which just calls METHOD, which should be an Array Get/Set/Address method.
 */
MonoMethod *
mono_marshal_get_array_accessor_wrapper (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;
	WrapperInfo *info;

	/*
	 * These wrappers are needed to avoid the JIT replacing the calls to these methods with intrinsics
	 * inside runtime invoke wrappers, thereby making the wrappers not unshareable.
	 * FIXME: Use generic methods.
	 */
	/*
	 * Check cache
	 */
	if (ctx) {
		cache = NULL;
		g_assert_not_reached ();
	} else {
		cache = get_cache (&get_method_image (method)->array_accessor_cache, mono_aligned_addr_hash, NULL);
		if ((res = mono_marshal_find_in_cache (cache, method)))
			return res;
	}

	sig = mono_metadata_signature_dup_full (get_method_image (method), mono_method_signature_internal (method));
	sig->pinvoke = 0;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_OTHER);

	get_marshal_cb ()->emit_array_accessor_wrapper (mb, method, sig, ctx);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_ARRAY_ACCESSOR);
	info->d.array_accessor.method = method;

	if (ctx) {
		MonoMethod *def;
		def = mono_mb_create_and_cache_full (cache, method, mb, sig, sig->param_count + 16, info, NULL);
		res = cache_generic_wrapper (cache, orig_method, def, ctx, orig_method);
	} else {
		res = mono_mb_create_and_cache_full (cache, method,
											 mb, sig, sig->param_count + 16,
											 info, NULL);
	}
	mono_mb_free (mb);

	return res;	
}

#ifndef HOST_WIN32

void*
mono_marshal_alloc_co_task_mem (size_t size)
{
	if (size == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		size = 4;

	return g_try_malloc (size);
}
#endif

/**
 * mono_marshal_alloc:
 */
void*
mono_marshal_alloc (gsize size, MonoError *error)
{
	gpointer res;

	error_init (error);

	res = mono_marshal_alloc_co_task_mem (size);

	if (!res)
		mono_error_set_out_of_memory (error, "Could not allocate %" G_GSIZE_FORMAT " bytes", size);

	return res;
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
void*
ves_icall_marshal_alloc_impl (gsize size, MonoError *error)
{
	return mono_marshal_alloc (size, error);
}

#ifndef HOST_WIN32

void
mono_marshal_free_co_task_mem (void *ptr)
{
	g_free (ptr);
}

#endif

/**
 * mono_marshal_free:
 */
void
mono_marshal_free (gpointer ptr)
{
	mono_marshal_free_co_task_mem (ptr);
}

/**
 * mono_marshal_free_array:
 */
void
mono_marshal_free_array (gpointer *ptr, int size) 
{
	int i;

	if (!ptr)
		return;

	for (i = 0; i < size; i++)
		g_free (ptr [i]);
}

void *
mono_marshal_string_to_utf16 (MonoString *s)
{
	// FIXME This should be an intrinsic.
	// FIXMEcoop The input parameter is easy to deal with,
	// but what happens with the result?
	// See https://github.com/mono/mono/issues/12165.
	return s ? mono_string_chars_internal (s) : NULL;
}

/* This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error. */
gunichar2*
mono_marshal_string_to_utf16_copy_impl (MonoStringHandle s, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (s))
		return NULL;

	gsize const length = mono_string_handle_length (s);
	gunichar2 *res = (gunichar2 *)mono_marshal_alloc ((length + 1) * sizeof (*res), error);
	return_val_if_nok (error, NULL);
	MonoGCHandle gchandle = NULL;
	memcpy (res, mono_string_handle_pin_chars (s, &gchandle), length * sizeof (*res));
	mono_gchandle_free_internal (gchandle);
	res [length] = 0;
	return res;
}

/**
 * mono_marshal_set_last_error:
 *
 * This function is invoked to set the last error value from a P/Invoke call
 * which has \c SetLastError set.
 */
void
mono_marshal_set_last_error (void)
{
	/* This icall is called just after a P/Invoke call before the P/Invoke
	 * wrapper transitions the runtime back to running mode. */
#ifdef WIN32
	MONO_REQ_GC_SAFE_MODE;
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (GetLastError ()));
#else
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (errno));
#endif
}

void
mono_marshal_set_last_error_windows (int error)
{
#ifdef WIN32
	/* This icall is called just after a P/Invoke call before the P/Invoke
	 * wrapper transitions the runtime back to running mode. */
	MONO_REQ_GC_SAFE_MODE;
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (error));
#endif
}

void
mono_marshal_clear_last_error (void)
{
	/* This icall is called just before a P/Invoke call. */
#ifdef WIN32
	SetLastError (ERROR_SUCCESS);
#else
	errno = 0;
#endif
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void)
{
	return GPOINTER_TO_INT (mono_native_tls_get_value (last_error_tls_id));
}

void
ves_icall_System_Runtime_InteropServices_Marshal_SetLastWin32Error (guint32 err)
{
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (err));
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionTypeHandle rtype, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (rtype)) {
		mono_error_set_argument_null (error, "type", "");
		return 0;
	}

	MonoType * const type = MONO_HANDLE_GETVAL (rtype, type);
	MonoClass * const klass = mono_class_from_mono_type_internal (type);
	if (!mono_class_init_checked (klass, error))
		return 0;

	guint32 const layout = (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK);

	if (type->type == MONO_TYPE_PTR || type->type == MONO_TYPE_FNPTR) {
		return sizeof (gpointer);
	} else if (type->type == MONO_TYPE_VOID) {
		return 1;
	} else if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		mono_error_set_argument_format (error, "t", "Type %s cannot be marshaled as an unmanaged structure.", m_class_get_name (klass));
		return 0;
	}

	guint32 align;
	return (guint32)mono_marshal_type_size (type, NULL, &align, FALSE, m_class_is_unicode (klass));
}

guint32
ves_icall_System_Runtime_InteropServices_Marshal_SizeOfHelper (MonoReflectionTypeHandle rtype, MonoBoolean throwIfNotMarshalable, MonoError *error)
{
	return ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (rtype, error);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObjectHandle obj, gpointer dst, MonoBoolean delete_old, MonoError *error)
{
	MONO_CHECK_ARG_NULL_HANDLE_NAMED (obj, "structure",);
	MONO_CHECK_ARG_NULL_NAMED (dst, "ptr",);

	MonoClass *klass = mono_handle_class (obj);
	if (m_class_is_auto_layout (klass)) {
		mono_error_set_argument (error, "structure", "The specified structure must be blittable or have layout information.");
		return;
	}
	if (m_class_is_ginst (klass)) {
		mono_error_set_argument (error, "structure", "The specified object must not be an instance of a generic type.");
		return;
	}

	MonoMethod *method = mono_marshal_get_struct_to_ptr (mono_handle_class (obj));

	gpointer pa [ ] = { MONO_HANDLE_RAW (obj), &dst, &delete_old };

	mono_runtime_invoke_handle_void (method, NULL_HANDLE, pa, error);
}

static void
ptr_to_structure (gconstpointer src, MonoObjectHandle dst, MonoError *error)
{
	MonoMethod *method = mono_marshal_get_ptr_to_struct (mono_handle_class (dst));

	gpointer pa [ ] = { &src, MONO_HANDLE_RAW (dst) };

	// FIXMEcoop? mono_runtime_invoke_handle causes a GC assertion failure in marshal2 with interpreter
	mono_runtime_invoke_checked (method, NULL, pa, error);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructureInternal (gconstpointer src, MonoObjectHandle dst, MonoBoolean allow_vtypes, MonoError *error)
{
	MonoType *t;
	MonoClass *klass;

	t = m_class_get_byval_arg (mono_handle_class (dst));
	if (!allow_vtypes && MONO_TYPE_ISSTRUCT (t)) {
		mono_error_set_argument (error, "structure", "The structure must not be a value class.");
		return;
	}

	klass = mono_class_from_mono_type_internal (t);
	if (m_class_is_auto_layout (klass)) {
		mono_error_set_argument (error, "structure", "The specified structure must be blittable or have layout information.");
		return;
	}

	ptr_to_structure (src, dst, error);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionTypeHandle ref_type, MonoStringHandle field_name, MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (ref_type)) {
		mono_error_set_argument_null (error, "t", "");
		return 0;
	}
	if (MONO_HANDLE_IS_NULL (field_name)) {
		mono_error_set_argument_null (error, NULL, "");
		return 0;
	}

	if (!m_class_is_runtime_type (MONO_HANDLE_GET_CLASS (ref_type))) {
		mono_error_set_argument (error, "fieldName", "");
		return 0;
	}

	char *fname = mono_string_handle_to_utf8 (field_name, error);
	return_val_if_nok (error, 0);

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type_internal (type);
	if (!mono_class_init_checked (klass, error))
		return 0;

	if (m_class_is_auto_layout (klass)) {
		mono_error_set_argument (error, NULL, "");
		return 0;
	}

	int match_index = -1;
	while (klass && match_index == -1) {
		MonoClassField* field;
		int i = 0;
		gpointer iter = NULL;
		while ((field = mono_class_get_fields_internal (klass, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (!strcmp (fname, mono_field_get_name (field))) {
				match_index = i;
				break;
			}
			i ++;
		}

		if (match_index == -1)
			klass = m_class_get_parent (klass);
        }

	g_free (fname);

	if(match_index == -1) {
		/* Get back original class instance */
		klass = mono_class_from_mono_type_internal (type);

		mono_error_set_argument_format (error, "fieldName", "Field passed in is not a marshaled member of the type %s", m_class_get_name (klass));
		return 0;
	}

	MonoMarshalType *info = mono_marshal_load_type_info (klass);
	return info->fields [match_index].offset;
}

#ifndef HOST_WIN32
void *
mono_marshal_alloc_hglobal (size_t size)
{
	return g_try_malloc (size);
}
#endif /* !HOST_WIN32 */

void
mono_struct_delete_old (MonoClass *klass, char *ptr)
{
	MonoMarshalType *info;
	int i;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		char *cpos;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, 
					m_class_is_unicode (klass), &conv);
			
		cpos = ptr + info->fields [i].offset;

		switch (conv) {
		case MONO_MARSHAL_CONV_NONE:
			if (MONO_TYPE_ISSTRUCT (ftype)) {
				mono_struct_delete_old (ftype->data.klass, cpos);
				continue;
			}
			break;
		case MONO_MARSHAL_CONV_STR_LPWSTR:
			/* We assume this field points inside a MonoString */
			break;
		case MONO_MARSHAL_CONV_STR_LPTSTR:
#ifdef TARGET_WIN32
			/* We assume this field points inside a MonoString 
			 * on Win32 */
			break;
#endif
		case MONO_MARSHAL_CONV_STR_LPSTR:
		case MONO_MARSHAL_CONV_STR_UTF8STR:
			mono_marshal_free (*(gpointer *)cpos);
			break;

		case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		case MONO_MARSHAL_CONV_STR_TBSTR:
		case MONO_MARSHAL_CONV_STR_BSTR:
			mono_free_bstr (*(gpointer*)cpos);
			break;
		default:
			continue;
		}
	}
}

void
ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure (gpointer src, MonoReflectionTypeHandle type, MonoError *error)
{
	MONO_CHECK_ARG_NULL_NAMED (src, "ptr",);
	MONO_CHECK_ARG_NULL_HANDLE_NAMED (type, "structureType",);

	if (!m_class_is_runtime_type (MONO_HANDLE_GET_CLASS (type))) {
		mono_error_set_argument (error, "structureType", "");
		return;
	}

	MonoClass *klass = mono_class_from_mono_type_handle (type);
	if (!mono_class_init_checked (klass, error))
		return;

	if (m_class_is_auto_layout (klass)) {
		mono_error_set_argument (error, "structureType", "The specified structure must be blittable or have layout information.");
		return;
	}

	mono_struct_delete_old (klass, (char *)src);
}

void*
mono_marshal_alloc_hglobal_error (gsize size, MonoError *error)
{
	if (size == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		size = 4;

	void* p = mono_marshal_alloc_hglobal (size);
	if (!p)
		mono_error_set_out_of_memory (error, "");
	return p;
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (gsize size)
{
	ERROR_DECL (error);
	void* result = mono_marshal_alloc_hglobal_error (size, error);
	mono_error_set_pending_exception (error);
	return result;
}

#ifndef HOST_WIN32

gpointer
mono_marshal_realloc_hglobal (gpointer ptr, size_t size)
{
	return g_try_realloc (ptr, size);
}

#endif

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal (gpointer ptr, gsize size)
{
	gpointer res = ptr ? mono_marshal_realloc_hglobal (ptr, size) : NULL;

	if (!res) {
		ERROR_DECL (error);
		mono_error_set_out_of_memory (error, "");
		mono_error_set_pending_exception (error);
	}

	return res;
}

#ifndef HOST_WIN32

void
mono_marshal_free_hglobal (gpointer ptr)
{
	g_free (ptr);
}

#endif

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal (void *ptr)
{
	mono_marshal_free_hglobal (ptr);
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem (int size)
{
	void *res = mono_marshal_alloc_co_task_mem (size);

	if (!res) {
		ERROR_DECL (error);
		mono_error_set_out_of_memory (error, "");
		mono_error_set_pending_exception (error);
	}

	return res;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr)
{
	mono_marshal_free_co_task_mem (ptr);
}

#ifndef HOST_WIN32

gpointer
mono_marshal_realloc_co_task_mem (gpointer ptr, size_t size)
{
	return g_try_realloc (ptr, size);
}
#endif

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem (gpointer ptr, int size)
{
	void *res = mono_marshal_realloc_co_task_mem (ptr, size);

	if (!res) {
		ERROR_DECL (error);
		mono_error_set_out_of_memory (error, "");
		mono_error_set_pending_exception (error);
	}

	return res;
}

MonoDelegateHandle
ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal (void *ftn, MonoReflectionTypeHandle type, MonoError *error)
{
	MonoClass *klass = mono_type_get_class_internal (MONO_HANDLE_GETVAL (type, type));
	if (!mono_class_init_checked (klass, error))
		return MONO_HANDLE_CAST (MonoDelegate, NULL_HANDLE);

	return mono_ftnptr_to_delegate_impl (klass, ftn, error);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_GetFunctionPointerForDelegateInternal (MonoDelegateHandle delegate, MonoError *error)
{
	return mono_delegate_to_ftnptr_impl (delegate, error);
}

MonoBoolean
ves_icall_System_Runtime_InteropServices_Marshal_IsPinnableType (MonoReflectionTypeHandle type_h, MonoError *error)
{
	MonoClass *klass = mono_class_from_mono_type_internal (MONO_HANDLE_GETVAL (type_h, type));

	if (m_class_get_rank (klass)) {
		MonoClass *eklass = m_class_get_element_class (klass);
		if (m_class_is_primitive (eklass))
			return TRUE;		
		return eklass != mono_defaults.object_class && m_class_is_blittable (eklass);
	} else
		return m_class_is_blittable (klass);
}

/**
 * mono_marshal_is_loading_type_info:
 *
 *  Return whenever mono_marshal_load_type_info () is being executed for KLASS by this
 * thread.
 */
static gboolean
mono_marshal_is_loading_type_info (MonoClass *klass)
{
	GSList *loads_list = (GSList *)mono_native_tls_get_value (load_type_info_tls_id);

	return g_slist_find (loads_list, klass) != NULL;
}

/**
 * mono_marshal_load_type_info:
 *
 * Initialize \c klass::marshal_info using information from metadata. This function can
 * recursively call itself, and the caller is responsible to avoid that by calling 
 * \c mono_marshal_is_loading_type_info beforehand.
 *
 * LOCKING: Acquires the loader lock.
 */
MonoMarshalType *
mono_marshal_load_type_info (MonoClass* klass)
{
	int j, count = 0;
	guint32 native_size = 0, min_align = 1, packing;
	MonoMarshalType *info;
	MonoClassField* field;
	gpointer iter;
	guint32 layout;
	GSList *loads_list;

	g_assert (klass != NULL);

	info = mono_class_get_marshal_info (klass);
	if (info)
		return info;

	if (!m_class_is_inited (klass))
		mono_class_init_internal (klass);

	info = mono_class_get_marshal_info (klass);
	if (info)
		return info;

	/*
	 * This function can recursively call itself, so we keep the list of classes which are
	 * under initialization in a TLS list.
	 */
	g_assert (!mono_marshal_is_loading_type_info (klass));
	loads_list = (GSList *)mono_native_tls_get_value (load_type_info_tls_id);
	loads_list = g_slist_prepend (loads_list, klass);
	mono_native_tls_set_value (load_type_info_tls_id, loads_list);
	
	iter = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		count++;
	}

	layout = mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK;

	info = (MonoMarshalType *)mono_image_alloc0 (m_class_get_image (klass), MONO_SIZEOF_MARSHAL_TYPE + sizeof (MonoMarshalField) * count);
	info->num_fields = count;
	
	/* Try to find a size for this type in metadata */
	mono_metadata_packing_from_typedef (m_class_get_image (klass), m_class_get_type_token (klass), NULL, &native_size);

	if (m_class_get_parent (klass)) {
		int parent_size = mono_class_native_size (m_class_get_parent (klass), NULL);

		/* Add parent size to real size */
		native_size += parent_size;
		info->native_size = parent_size;
	}

	packing = m_class_get_packing_size (klass) ? m_class_get_packing_size (klass) : 8;
	iter = NULL;
	j = 0;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		int size;
		guint32 align;
		
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		if (mono_field_is_deleted (field))
			continue;
		if (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL)
			mono_metadata_field_info_with_mempool (m_class_get_image (klass), mono_metadata_token_index (mono_class_get_field_token (field)) - 1, 
						  NULL, NULL, &info->fields [j].mspec);

		info->fields [j].field = field;

		if ((mono_class_num_fields (klass) == 1) && (m_class_get_instance_size (klass) == MONO_ABI_SIZEOF (MonoObject)) &&
			(strcmp (mono_field_get_name (field), "$PRIVATE$") == 0)) {
			/* This field is a hack inserted by MCS to empty structures */
			continue;
		}

		switch (layout) {
		case TYPE_ATTRIBUTE_AUTO_LAYOUT:
		case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
			size = mono_marshal_type_size (field->type, info->fields [j].mspec, 
						       &align, TRUE, m_class_is_unicode (klass));
			align = m_class_get_packing_size (klass) ? MIN (m_class_get_packing_size (klass), align): align;
			min_align = MAX (align, min_align);
			info->fields [j].offset = info->native_size;
			info->fields [j].offset += align - 1;
			info->fields [j].offset &= ~(align - 1);
			info->native_size = info->fields [j].offset + size;
			break;
		case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT:
			size = mono_marshal_type_size (field->type, info->fields [j].mspec, 
						       &align, TRUE, m_class_is_unicode (klass));
			min_align = MAX (align, min_align);
			info->fields [j].offset = field->offset - MONO_ABI_SIZEOF (MonoObject);
			info->native_size = MAX (info->native_size, info->fields [j].offset + size);
			break;
		}	
		j++;
	}

	if (m_class_get_byval_arg (klass)->type == MONO_TYPE_PTR)
		info->native_size = TARGET_SIZEOF_VOID_P;

	if (layout != TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		info->native_size = MAX (native_size, info->native_size);
		/*
		 * If the provided Size is equal or larger than the calculated size, and there
		 * was no Pack attribute, we set min_align to 1 to avoid native_size being increased
		 */
		if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
			if (native_size && native_size == info->native_size && m_class_get_packing_size (klass) == 0)
				min_align = 1;
			else
				min_align = MIN (min_align, packing);
		}
	}

	if (info->native_size & (min_align - 1)) {
		info->native_size += min_align - 1;
		info->native_size &= ~(min_align - 1);
	}

	info->min_align = min_align;

	/* Update the class's blittable info, if the layouts don't match */
	if (info->native_size != mono_class_value_size (klass, NULL)) {
		mono_class_set_nonblittable (klass); /* FIXME - how is this justified? what if we previously thought the class was blittable? */
	}

	/* If this is an array type, ensure that we have element info */
	if (m_class_get_rank (klass) && !mono_marshal_is_loading_type_info (m_class_get_element_class (klass))) {
		mono_marshal_load_type_info (m_class_get_element_class (klass));
	}

	loads_list = (GSList *)mono_native_tls_get_value (load_type_info_tls_id);
	loads_list = g_slist_remove (loads_list, klass);
	mono_native_tls_set_value (load_type_info_tls_id, loads_list);

	mono_marshal_lock ();
	MonoMarshalType *info2 = mono_class_get_marshal_info (klass);
	if (!info2) {
		/*We do double-checking locking on marshal_info */
		mono_memory_barrier ();
		mono_class_set_marshal_info (klass, info);
		++class_marshal_info_count;
		info2 = info;
	}

	mono_marshal_unlock ();

	return info2;
}

/**
 * mono_class_native_size:
 * \param klass a class 
 * \returns the native size of an object instance (when marshaled 
 * to unmanaged code) 
 */
gint32
mono_class_native_size (MonoClass *klass, guint32 *align)
{
	MonoMarshalType *info = mono_class_get_marshal_info (klass);
	if (!info) {
		if (mono_marshal_is_loading_type_info (klass)) {
			if (align)
				*align = 0;
			return 0;
		} else {
			mono_marshal_load_type_info (klass);
		}
		info = mono_class_get_marshal_info (klass);
	}

	if (align)
		*align = info->min_align;

	return info->native_size;
}

/*
 * mono_type_native_stack_size:
 * @t: the type to return the size it uses on the stack
 *
 * Returns: the number of bytes required to hold an instance of this
 * type on the native stack
 */
int
mono_type_native_stack_size (MonoType *t, guint32 *align)
{
	guint32 tmp;

	g_assert (t != NULL);

	if (!align)
		align = &tmp;

	if (t->byref) {
		*align = TARGET_SIZEOF_VOID_P;
		return TARGET_SIZEOF_VOID_P;
	}

	switch (t->type){
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
		*align = 4;
		return 4;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
		*align = TARGET_SIZEOF_VOID_P;
		return TARGET_SIZEOF_VOID_P;
	case MONO_TYPE_R4:
		*align = 4;
		return 4;
	case MONO_TYPE_R8:
		*align = MONO_ABI_ALIGNOF (double);
		return 8;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*align = MONO_ABI_ALIGNOF (gint64);
		return 8;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t)) {
			*align = TARGET_SIZEOF_VOID_P;
			return TARGET_SIZEOF_VOID_P;
		} 
		/* Fall through */
	case MONO_TYPE_TYPEDBYREF:
	case MONO_TYPE_VALUETYPE: {
		guint32 size;
		MonoClass *klass = mono_class_from_mono_type_internal (t);

		if (m_class_is_enumtype (klass))
			return mono_type_native_stack_size (mono_class_enum_basetype_internal (klass), align);
		else {
			size = mono_class_native_size (klass, align);
			*align = *align + 3;
			*align &= ~3;
			
			size +=  3;
			size &= ~3;

			return size;
		}
	}
	default:
		g_error ("type 0x%02x unknown", t->type);
	}
	return 0;
}

/**
 * mono_marshal_type_size:
 */
gint32
mono_marshal_type_size (MonoType *type, MonoMarshalSpec *mspec, guint32 *align,
			gboolean as_field, gboolean unicode)
{
	gint32 padded_size;
	MonoMarshalNative native_type = (MonoMarshalNative)mono_type_to_unmanaged (type, mspec, as_field, unicode, NULL);
	MonoClass *klass;

	switch (native_type) {
	case MONO_NATIVE_BOOLEAN:
		*align = 4;
		return 4;
	case MONO_NATIVE_I1:
	case MONO_NATIVE_U1:
		*align = 1;
		return 1;
	case MONO_NATIVE_I2:
	case MONO_NATIVE_U2:
	case MONO_NATIVE_VARIANTBOOL:
		*align = 2;
		return 2;
	case MONO_NATIVE_I4:
	case MONO_NATIVE_U4:
	case MONO_NATIVE_ERROR:
		*align = 4;
		return 4;
	case MONO_NATIVE_I8:
	case MONO_NATIVE_U8:
		*align = MONO_ABI_ALIGNOF (gint64);
		return 8;
	case MONO_NATIVE_R4:
		*align = 4;
		return 4;
	case MONO_NATIVE_R8:
		*align = MONO_ABI_ALIGNOF (double);
		return 8;
	case MONO_NATIVE_INT:
	case MONO_NATIVE_UINT:
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_LPWSTR:
	case MONO_NATIVE_LPTSTR:
	case MONO_NATIVE_BSTR:
	case MONO_NATIVE_ANSIBSTR:
	case MONO_NATIVE_TBSTR:
	case MONO_NATIVE_UTF8STR:
	case MONO_NATIVE_LPARRAY:
	case MONO_NATIVE_SAFEARRAY:
	case MONO_NATIVE_IUNKNOWN:
	case MONO_NATIVE_IDISPATCH:
	case MONO_NATIVE_INTERFACE:
	case MONO_NATIVE_ASANY:
	case MONO_NATIVE_FUNC:
	case MONO_NATIVE_LPSTRUCT:
		*align = MONO_ABI_ALIGNOF (gpointer);
		return TARGET_SIZEOF_VOID_P;
	case MONO_NATIVE_STRUCT: 
		klass = mono_class_from_mono_type_internal (type);
		if (klass == mono_defaults.object_class &&
			(mspec && mspec->native == MONO_NATIVE_STRUCT)) {
			*align = 16;
			return 16;
		} else if (strcmp (m_class_get_name_space (klass), "System") == 0 && 
			strcmp (m_class_get_name (klass), "Decimal") == 0) {
			
			// Special case: Managed Decimal consists of 4 int32 fields, the alignment should be 8 on x64 to follow 
			// https://github.com/dotnet/coreclr/blob/4450e5ca663b9e66c20e6f9751c941efa3716fde/src/vm/methodtablebuilder.cpp#L9753
			*align = MONO_ABI_ALIGNOF (gpointer);
			return mono_class_native_size (klass, NULL);
		}
		padded_size = mono_class_native_size (klass, align);
		if (padded_size == 0)
			padded_size = 1;
		return padded_size;
	case MONO_NATIVE_BYVALTSTR: {
		int esize = unicode ? 2: 1;
		g_assert (mspec);
		*align = esize;
		return mspec->data.array_data.num_elem * esize;
	}
	case MONO_NATIVE_BYVALARRAY: {
		// FIXME: Have to consider ArraySubType
		int esize;
		klass = mono_class_from_mono_type_internal (type);
		if (m_class_get_element_class (klass) == mono_defaults.char_class) {
			esize = unicode ? 2 : 1;
			*align = esize;
		} else {
			esize = mono_class_native_size (m_class_get_element_class (klass), align);
		}
		g_assert (mspec);
		return mspec->data.array_data.num_elem * esize;
	}
	case MONO_NATIVE_CUSTOM:
		*align = TARGET_SIZEOF_VOID_P;
		return TARGET_SIZEOF_VOID_P;
		break;
	case MONO_NATIVE_CURRENCY:
	case MONO_NATIVE_VBBYREFSTR:
	default:
		g_error ("native type %02x not implemented", native_type); 
		break;
	}
	g_assert_not_reached ();
	return 0;
}

/**
 * mono_marshal_asany:
 * This is a JIT icall, it sets the pending exception (in wrapper) and returns NULL on error.
 */
gpointer
mono_marshal_asany_impl (MonoObjectHandle o, MonoMarshalNative string_encoding, int param_attrs, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (o))
		return NULL;

	MonoType *t = m_class_get_byval_arg (mono_handle_class (o));
	switch (t->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_PTR:
	case MONO_TYPE_I:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return mono_handle_unbox_unsafe (o);
	case MONO_TYPE_STRING:
		switch (string_encoding) {
		case MONO_NATIVE_LPWSTR:
			return mono_marshal_string_to_utf16_copy_impl (MONO_HANDLE_CAST (MonoString, o), error);
		case MONO_NATIVE_LPSTR:
		case MONO_NATIVE_UTF8STR:
			// Same code path, because in Mono, we treated strings as Utf8
			return mono_string_to_utf8str_impl (MONO_HANDLE_CAST (MonoString, o), error);
		default:
			g_warning ("marshaling conversion %d not implemented", string_encoding);
			g_assert_not_reached ();
		}
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {

		MonoClass *klass = t->data.klass;

		if (mono_class_is_auto_layout (klass))
			break;

		if (m_class_is_valuetype (klass) && (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass)))
			return mono_handle_unbox_unsafe (o);

		gpointer res = mono_marshal_alloc (mono_class_native_size (klass, NULL), error);
		return_val_if_nok (error, NULL);

		if (!((param_attrs & PARAM_ATTRIBUTE_OUT) && !(param_attrs & PARAM_ATTRIBUTE_IN))) {
			MonoMethod *method = mono_marshal_get_struct_to_ptr (mono_handle_class (o));
			MonoBoolean delete_old = FALSE;
			gpointer pa [ ] = { MONO_HANDLE_RAW (o), &res, &delete_old };

			mono_runtime_invoke_handle_void (method, NULL_HANDLE, pa, error);
			return_val_if_nok (error, NULL);
		}

		return res;
	}
	case MONO_TYPE_SZARRAY: {
		//TODO: Implement structs and in-params for all value types	
		MonoClass *klass = t->data.klass;
		MonoClass *eklass = m_class_get_element_class (klass);
		MonoArray *arr = (MonoArray *) MONO_HANDLE_RAW (o);

		// we only support char[] for in-params; we return a pointer to the managed heap here, and that's not 'in'-safe
		if ((param_attrs & PARAM_ATTRIBUTE_IN) && eklass != mono_get_char_class ())
			break;

		if (m_class_get_rank (klass) > 1)
			break;

		if (arr->bounds)
			if (arr->bounds->lower_bound != 0)
				break;

		if (mono_class_is_auto_layout (eklass))
			break;

		if (m_class_is_valuetype (eklass) && (mono_class_is_explicit_layout (eklass) || m_class_is_blittable (eklass) || m_class_is_enumtype (eklass)))
			return arr->vector;

		if (eklass == mono_get_char_class ()) {
			char *res =  mono_utf16_to_utf8 ((mono_unichar2 *) arr->vector, arr->max_length, error);
			return_val_if_nok (error, NULL);
			return res;
		}
		break;
	}
	default:
		break;
	}
	mono_error_set_argument (error, "", "No PInvoke conversion exists for value passed to Object-typed parameter.");
	return NULL;
}

/**
 * mono_marshal_free_asany:
 * This is a JIT icall, it sets the pending exception (in wrapper)
 */
void
mono_marshal_free_asany_impl (MonoObjectHandle o, gpointer ptr, MonoMarshalNative string_encoding, int param_attrs, MonoError *error)
{
	MonoType *t;
	MonoClass *klass;

	if (MONO_HANDLE_IS_NULL (o))
		return;

	t = m_class_get_byval_arg (mono_handle_class (o));
	switch (t->type) {
	case MONO_TYPE_STRING:
		switch (string_encoding) {
		case MONO_NATIVE_LPWSTR:
		case MONO_NATIVE_LPSTR:
		case MONO_NATIVE_UTF8STR:
			mono_marshal_free (ptr);
			break;
		default:
			g_warning ("marshaling conversion %d not implemented", string_encoding);
			g_assert_not_reached ();
		}
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		klass = t->data.klass;

		if (m_class_is_valuetype (klass) && (mono_class_is_explicit_layout (klass) || m_class_is_blittable (klass) || m_class_is_enumtype (klass)))
			break;

		if (param_attrs & PARAM_ATTRIBUTE_OUT) {
			MonoMethod *method = mono_marshal_get_ptr_to_struct (mono_handle_class (o));
			gpointer pa [2];

			pa [0] = &ptr;
			pa [1] = MONO_HANDLE_RAW (o);

			mono_runtime_invoke_checked (method, NULL, pa, error);
			if (!is_ok (error))
				return;
		}

		if (!((param_attrs & PARAM_ATTRIBUTE_OUT) && !(param_attrs & PARAM_ATTRIBUTE_IN))) {
			mono_struct_delete_old (klass, (char *)ptr);
		}

		mono_marshal_free (ptr);
		break;
	}
	case MONO_TYPE_SZARRAY: {
		MonoClass *klass = t->data.klass;
		MonoClass *eklass = m_class_get_element_class (klass);
		MonoArray *arr = (MonoArray *) MONO_HANDLE_RAW (o);

		if (eklass != mono_get_char_class ())
			break;

		mono_unichar2 *utf16_array = g_utf8_to_utf16 ((const char *)ptr, arr->max_length, NULL, NULL, NULL);
		g_free (ptr);
		memcpy (arr->vector, utf16_array, arr->max_length * sizeof (mono_unichar2));
		g_free (utf16_array);
		break;
	}
	default:
		break;
	}
}

/*
 * mono_marshal_get_generic_array_helper:
 *
 *   Return a wrapper which is used to implement the implicit interfaces on arrays.
 * The wrapper routes calls to METHOD, which is one of the InternalArray_ methods in Array.
 */
MonoMethod *
mono_marshal_get_generic_array_helper (MonoClass *klass, const gchar *name, MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	WrapperInfo *info;

	mb = mono_mb_new_no_dup_name (klass, name, MONO_WRAPPER_MANAGED_TO_MANAGED);
	mb->method->slot = -1;

	mb->method->flags = METHOD_ATTRIBUTE_PRIVATE | METHOD_ATTRIBUTE_VIRTUAL |
		METHOD_ATTRIBUTE_NEW_SLOT | METHOD_ATTRIBUTE_HIDE_BY_SIG | METHOD_ATTRIBUTE_FINAL;

	sig = mono_method_signature_internal (method);
	csig = mono_metadata_signature_dup_full (get_method_image (method), sig);
	csig->generic_param_count = 0;

	get_marshal_cb ()->emit_generic_array_helper (mb, method, csig);

	/* We can corlib internal methods */
	get_marshal_cb ()->mb_skip_visibility (mb);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER);
	/* Assume name is constant/not freed */
	info->d.generic_array_helper.name = name;
	info->d.generic_array_helper.klass = klass;
	info->d.generic_array_helper.method = method;
	res = mono_mb_create (mb, csig, csig->param_count + 16, info);

	mono_mb_free (mb);

	return res;
}

/*
 * The mono_win32_compat_* functions are implementations of inline
 * Windows kernel32 APIs, which are DllImport-able under MS.NET,
 * although not exported by kernel32.
 *
 * We map the appropiate kernel32 entries to these functions using
 * dllmaps declared in the global etc/mono/config.
 */

void
mono_win32_compat_CopyMemory (gpointer dest, gconstpointer source, gsize length)
{
	if (!dest || !source)
		return;

	memcpy (dest, source, length);
}

void
mono_win32_compat_FillMemory (gpointer dest, gsize length, guchar fill)
{
	memset (dest, fill, length);
}

void
mono_win32_compat_MoveMemory (gpointer dest, gconstpointer source, gsize length)
{
	if (!dest || !source)
		return;

	memmove (dest, source, length);
}

void
mono_win32_compat_ZeroMemory (gpointer dest, gsize length)
{
	memset (dest, 0, length);
}

void
mono_marshal_find_nonzero_bit_offset (guint8 *buf, int len, int *byte_offset, guint8 *bitmask)
{
	int i;
	guint8 byte;

	for (i = 0; i < len; ++i)
		if (buf [i])
			break;

	g_assert (i < len);

	byte = buf [i];
	while (byte && !(byte & 1))
		byte >>= 1;
	g_assert (byte == 1);

	*byte_offset = i;
	*bitmask = buf [i];
}

MonoMethod *
mono_marshal_get_thunk_invoke_wrapper (MonoMethod *method)
{
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig, *csig;
	MonoImage *image;
	MonoClass *klass;
	GHashTable *cache;
	MonoMethod *res;
	int i, param_count, sig_size;

	g_assert (method);

	klass = method->klass;
	image = m_class_get_image (klass);

	cache = get_cache (&mono_method_get_wrapper_cache (method)->thunk_invoke_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	MonoType *object_type = mono_get_object_type ();

	sig = mono_method_signature_internal (method);
	mb = mono_mb_new (klass, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);

	/* add "this" and exception param */
	param_count = sig->param_count + sig->hasthis + 1;

	/* dup & extend signature */
	csig = mono_metadata_signature_alloc (image, param_count);
	sig_size = MONO_SIZEOF_METHOD_SIGNATURE + sig->param_count * sizeof (MonoType *);
	memcpy (csig, sig, sig_size);
	csig->param_count = param_count;
	csig->hasthis = 0;
	csig->pinvoke = 1;
	csig->call_convention = MONO_CALL_DEFAULT;

	if (sig->hasthis) {
		/* add "this" */
		csig->params [0] = m_class_get_byval_arg (klass);
		/* move params up by one */
		for (i = 0; i < sig->param_count; i++)
			csig->params [i + 1] = sig->params [i];
	}

	/* setup exception param as byref+[out] */
	csig->params [param_count - 1] = mono_metadata_type_dup (image, m_class_get_byval_arg (mono_defaults.exception_class));
	csig->params [param_count - 1]->byref = 1;
	csig->params [param_count - 1]->attrs = PARAM_ATTRIBUTE_OUT;

	/* convert struct return to object */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		csig->ret = object_type;

	get_marshal_cb ()->emit_thunk_invoke_wrapper (mb, method, csig);

	res = mono_mb_create_and_cache (cache, method, mb, csig, param_count + 16);
	mono_mb_free (mb);

	return res;
}

static void
clear_runtime_invoke_method_cache (GHashTable *table, MonoMethod *method)
{
	MonoWrapperMethodCacheKey hash_key = {method, FALSE, FALSE};
	/*
	 * Since we have a small set of possible keys, remove each one separately, thus
	 * avoiding the traversal of the entire hash table, when using foreach_remove.
	 */
	g_hash_table_remove (table, &hash_key);
	hash_key.need_direct_wrapper = TRUE;
	g_hash_table_remove (table, &hash_key);
	hash_key.virtual_ = TRUE;
	g_hash_table_remove (table, &hash_key);
	hash_key.need_direct_wrapper = FALSE;
	g_hash_table_remove (table, &hash_key);
}

/*
 * mono_marshal_free_dynamic_wrappers:
 *
 *   Free wrappers of the dynamic method METHOD.
 */
void
mono_marshal_free_dynamic_wrappers (MonoMethod *method)
{
	if (!method)
		return;

	MonoImage *image = get_method_image (method);

	g_assert (method_is_dynamic (method));

	/* This could be called during shutdown */
	if (marshal_mutex_initialized)
		mono_marshal_lock ();
	/* 
	 * FIXME: We currently leak the wrappers. Freeing them would be tricky as
	 * they could be shared with other methods ?
	 */
	if (image->wrapper_caches.runtime_invoke_method_cache)
		clear_runtime_invoke_method_cache (image->wrapper_caches.runtime_invoke_method_cache, method);
	if (image->wrapper_caches.delegate_abstract_invoke_cache)
		g_hash_table_foreach_remove (image->wrapper_caches.delegate_abstract_invoke_cache, signature_pointer_pair_matches_pointer, method);
	// FIXME: Need to clear the caches in other images as well
	if (image->wrapper_caches.delegate_bound_static_invoke_cache)
		g_hash_table_remove (image->wrapper_caches.delegate_bound_static_invoke_cache, mono_method_signature_internal (method));

	if (marshal_mutex_initialized)
		mono_marshal_unlock ();
}

MonoObject*
mono_marshal_get_type_object (MonoClass *klass)
{
	ERROR_DECL (error);
	MonoType *type = m_class_get_byval_arg (klass);
	MonoObject *result = (MonoObject*)mono_type_get_object_checked (type, error);
	mono_error_set_pending_exception (error);
	return result;
}

gpointer
mono_marshal_lookup_pinvoke (MonoMethod *method)
{
	ERROR_DECL (error);
	gpointer addr;

	g_assert (method);
	addr = mono_lookup_pinvoke_call_internal (method, error);
	if (!addr)
		g_assert (!is_ok (error));
	mono_error_set_pending_exception (error);
	return addr;
}

void
mono_marshal_emit_native_wrapper (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, MonoNativeWrapperFlags flags)
{
	get_marshal_cb ()->emit_native_wrapper (image, mb, sig, piinfo, mspecs, func, flags);
}

static MonoMarshalCallbacks marshal_cb;
static gboolean cb_inited = FALSE;

void
mono_install_marshal_callbacks (MonoMarshalCallbacks *cb)
{
	g_assert (!cb_inited);
	g_assert (cb->version == MONO_MARSHAL_CALLBACKS_VERSION);
	memcpy (&marshal_cb, cb, sizeof (MonoMarshalCallbacks));
	cb_inited = TRUE;
}

static MonoMarshalCallbacks *
get_marshal_cb (void)
{
	if (G_UNLIKELY (!cb_inited)) {
#ifdef ENABLE_ILGEN
		mono_marshal_ilgen_init ();
#else
		mono_marshal_noilgen_init ();
#endif
	}
	return &marshal_cb;
}

/**
 * mono_method_has_unmanaged_callers_only_attribute:
 *
 * Returns \c TRUE if \p method has the \c UnmanagedCallersOnlyAttribute
 */
gboolean
mono_method_has_unmanaged_callers_only_attribute (MonoMethod *method)
{
	ERROR_DECL (attr_error);
	MonoClass *attr_klass = NULL;
	attr_klass = mono_class_try_get_unmanaged_callers_only_attribute_class ();
	if (!attr_klass)
		return FALSE;
	MonoCustomAttrInfo *cinfo;
	cinfo = mono_custom_attrs_from_method_checked (method, attr_error);
	if (!is_ok (attr_error) || !cinfo) {
		mono_error_cleanup (attr_error);
		return FALSE;
	}
	gboolean result;
	result = mono_custom_attrs_has_attr (cinfo, attr_klass);
	if (!cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return result;
}
