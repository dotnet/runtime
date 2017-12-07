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
#include "metadata/method-builder.h"
#include "metadata/tabledefs.h"
#include "metadata/exception.h"
#include "metadata/appdomain.h"
#include "mono/metadata/abi-details.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/metadata/cominterop.h"
#include "mono/metadata/remoting.h"
#include "mono/metadata/reflection-internals.h"
#include "mono/metadata/threadpool.h"
#include "mono/metadata/handle.h"
#include "mono/utils/mono-counters.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-memory-model.h"
#include "mono/utils/atomic.h"
#include <mono/utils/mono-threads.h>
#include <mono/utils/mono-threads-coop.h>
#include <mono/utils/mono-error-internals.h>

#include <string.h>
#include <errno.h>

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
#define mono_marshal_lock() mono_locks_os_acquire (&marshal_mutex, MarshalLock)
#define mono_marshal_unlock() mono_locks_os_release (&marshal_mutex, MarshalLock)
static mono_mutex_t marshal_mutex;
static gboolean marshal_mutex_initialized;

static MonoNativeTlsKey last_error_tls_id;

static MonoNativeTlsKey load_type_info_tls_id;

static gboolean use_aot_wrappers;

static int class_marshal_info_count;

static void ftnptr_eh_callback_default (guint32 gchandle);

static MonoFtnPtrEHCallback ftnptr_eh_callback = ftnptr_eh_callback_default;

static void
delegate_hash_table_add (MonoDelegateHandle d);

static void
delegate_hash_table_remove (MonoDelegate *d);

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

static void
emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object, int offset_of_first_child_field, MonoMarshalNative string_encoding);

static void 
mono_struct_delete_old (MonoClass *klass, char *ptr);

MONO_API void *
mono_marshal_string_to_utf16 (MonoString *s);

static void *
mono_marshal_string_to_utf16_copy (MonoString *s);

#ifndef HOST_WIN32
static gpointer
mono_string_to_utf8str (MonoString *string_obj);
#endif

static MonoStringBuilder *
mono_string_utf8_to_builder2 (char *text);

static MonoStringBuilder *
mono_string_utf16_to_builder2 (gunichar2 *text);

static MonoString*
mono_string_new_len_wrapper (const char *text, guint length);

static MonoString *
mono_string_from_byvalstr (const char *data, int len);

static MonoString *
mono_string_from_byvalwstr (gunichar2 *data, int len);

static void
mono_byvalarray_to_array (MonoArray *arr, gpointer native_arr, MonoClass *eltype, guint32 elnum);

static void
mono_byvalarray_to_byte_array (MonoArray *arr, gpointer native_arr, guint32 elnum);

static void
mono_array_to_byvalarray (gpointer native_arr, MonoArray *arr, MonoClass *eltype, guint32 elnum);

static void
mono_array_to_byte_byvalarray (gpointer native_arr, MonoArray *arr, guint32 elnum);

static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params);

static MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params);

static void
mono_marshal_set_last_error_windows (int error);

static MonoObject *
mono_marshal_isinst_with_cache (MonoObject *obj, MonoClass *klass, uintptr_t *cache);

static void init_safe_handle (void);

static void*
ves_icall_marshal_alloc (gsize size);

void
mono_string_utf8_to_builder (MonoStringBuilder *sb, char *text);

void
mono_string_utf16_to_builder (MonoStringBuilder *sb, gunichar2 *text);

gchar*
mono_string_builder_to_utf8 (MonoStringBuilder *sb);

gunichar2*
mono_string_builder_to_utf16 (MonoStringBuilder *sb);

void
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size);

void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size);

gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate);

gpointer
mono_delegate_handle_to_ftnptr (MonoDelegateHandle delegate, MonoError *error);

MonoDelegate*
mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn);

MonoDelegateHandle
mono_ftnptr_to_delegate_handle (MonoClass *klass, gpointer ftn, MonoError *error);

gpointer
mono_array_to_savearray (MonoArray *array);

gpointer
mono_array_to_lparray (MonoArray *array);

void
mono_free_lparray (MonoArray *array, gpointer* nativeArray);

gpointer
mono_marshal_asany (MonoObject *obj, MonoMarshalNative string_encoding, int param_attrs);

void
mono_marshal_free_asany (MonoObject *o, gpointer ptr, MonoMarshalNative string_encoding, int param_attrs);

gpointer
mono_array_to_savearray (MonoArray *array);

gpointer
mono_array_to_lparray (MonoArray *array);

void
mono_free_lparray (MonoArray *array, gpointer* nativeArray);

static void
mono_marshal_ftnptr_eh_callback (guint32 gchandle);

static MonoThreadInfo*
mono_icall_start (HandleStackMark *stackmark, MonoError *error);

static void
mono_icall_end (MonoThreadInfo *info, HandleStackMark *stackmark, MonoError *error);

static MonoObjectHandle
mono_icall_handle_new (gpointer rawobj);

static MonoObjectHandle
mono_icall_handle_new_interior (gpointer rawobj);

/* Lazy class loading functions */
static GENERATE_GET_CLASS_WITH_CACHE (string_builder, "System.Text", "StringBuilder");
static GENERATE_GET_CLASS_WITH_CACHE (date_time, "System", "DateTime");
static GENERATE_GET_CLASS_WITH_CACHE (fixed_buffer_attribute, "System.Runtime.CompilerServices", "FixedBufferAttribute");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (unmanaged_function_pointer_attribute, "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");
static GENERATE_TRY_GET_CLASS_WITH_CACHE (icustom_marshaler, "System.Runtime.InteropServices", "ICustomMarshaler");

/* MonoMethod pointers to SafeHandle::DangerousAddRef and ::DangerousRelease */
static MonoMethod *sh_dangerous_add_ref;
static MonoMethod *sh_dangerous_release;

static void
init_safe_handle ()
{
	sh_dangerous_add_ref = mono_class_get_method_from_name (
		mono_class_try_get_safehandle_class (), "DangerousAddRef", 1);
	sh_dangerous_release = mono_class_get_method_from_name (
		mono_class_try_get_safehandle_class (), "DangerousRelease", 0);
}

static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean no_wrapper)
{
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	mono_register_jit_icall (func, name, sig, no_wrapper);
}

static void
register_icall_no_wrapper (gpointer func, const char *name, const char *sigstr)
{
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	mono_register_jit_icall (func, name, sig, TRUE);
}

MonoMethodSignature*
mono_signature_no_pinvoke (MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature (method);
	if (sig->pinvoke) {
		sig = mono_metadata_signature_dup_full (method->klass->image, sig);
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

static MonoObject*
mono_object_isinst_icall (MonoObject *obj, MonoClass *klass)
{
	if (!klass)
		return NULL;

	/* This is called from stelemref so it is expected to succeed */
	/* Fastpath */
	if (mono_class_is_interface (klass)) {
		MonoVTable *vt = obj->vtable;

		if (!klass->inited)
			mono_class_init (klass);

		if (MONO_VTABLE_IMPLEMENTS_INTERFACE (vt, klass->interface_id))
			return obj;
	}

	MonoError error;
	MonoObject *result = mono_object_isinst_checked (obj, klass, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

static MonoString*
ves_icall_mono_string_from_utf16 (gunichar2 *data)
{
	MonoError error;
	MonoString *result = mono_string_from_utf16_checked (data, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

static char*
ves_icall_mono_string_to_utf8 (MonoString *str)
{
	MonoError error;
	char *result = mono_string_to_utf8_checked (str, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

static MonoString*
ves_icall_string_new_wrapper (const char *text)
{
	if (text) {
		MonoError error;
		MonoString *res = mono_string_new_checked (mono_domain_get (), text, &error);
		mono_error_set_pending_exception (&error);
		return res;
	}

	return NULL;
}

void
mono_marshal_init (void)
{
	static gboolean module_initialized = FALSE;

	if (!module_initialized) {
		module_initialized = TRUE;
		mono_os_mutex_init_recursive (&marshal_mutex);
		marshal_mutex_initialized = TRUE;

		register_icall (ves_icall_System_Threading_Thread_ResetAbort, "ves_icall_System_Threading_Thread_ResetAbort", "void", TRUE);
		register_icall (mono_marshal_string_to_utf16, "mono_marshal_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_marshal_string_to_utf16_copy, "mono_marshal_string_to_utf16_copy", "ptr obj", FALSE);
		register_icall (mono_string_to_utf16, "mono_string_to_utf16", "ptr obj", FALSE);
		register_icall (ves_icall_mono_string_from_utf16, "ves_icall_mono_string_from_utf16", "obj ptr", FALSE);
		register_icall (mono_string_from_byvalstr, "mono_string_from_byvalstr", "obj ptr int", FALSE);
		register_icall (mono_string_from_byvalwstr, "mono_string_from_byvalwstr", "obj ptr int", FALSE);
		register_icall (mono_string_new_wrapper, "mono_string_new_wrapper", "obj ptr", FALSE);
		register_icall (ves_icall_string_new_wrapper, "ves_icall_string_new_wrapper", "obj ptr", FALSE);
		register_icall (mono_string_new_len_wrapper, "mono_string_new_len_wrapper", "obj ptr int", FALSE);
		register_icall (ves_icall_mono_string_to_utf8, "ves_icall_mono_string_to_utf8", "ptr obj", FALSE);
		register_icall (mono_string_to_utf8str, "mono_string_to_utf8str", "ptr obj", FALSE);
		register_icall (mono_string_to_ansibstr, "mono_string_to_ansibstr", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf8, "mono_string_builder_to_utf8", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf16, "mono_string_builder_to_utf16", "ptr object", FALSE);
		register_icall (mono_array_to_savearray, "mono_array_to_savearray", "ptr object", FALSE);
		register_icall (mono_array_to_lparray, "mono_array_to_lparray", "ptr object", FALSE);
		register_icall (mono_free_lparray, "mono_free_lparray", "void object ptr", FALSE);
		register_icall (mono_byvalarray_to_array, "mono_byvalarray_to_array", "void object ptr ptr int32", FALSE);
		register_icall (mono_byvalarray_to_byte_array, "mono_byvalarray_to_byte_array", "void object ptr int32", FALSE);
		register_icall (mono_array_to_byvalarray, "mono_array_to_byvalarray", "void ptr object ptr int32", FALSE);
		register_icall (mono_array_to_byte_byvalarray, "mono_array_to_byte_byvalarray", "void ptr object int32", FALSE);
		register_icall (mono_delegate_to_ftnptr, "mono_delegate_to_ftnptr", "ptr object", FALSE);
		register_icall (mono_ftnptr_to_delegate, "mono_ftnptr_to_delegate", "object ptr ptr", FALSE);
		register_icall (mono_marshal_asany, "mono_marshal_asany", "ptr object int32 int32", FALSE);
		register_icall (mono_marshal_free_asany, "mono_marshal_free_asany", "void object ptr int32 int32", FALSE);
		register_icall (ves_icall_marshal_alloc, "ves_icall_marshal_alloc", "ptr ptr", FALSE);
		register_icall (mono_marshal_free, "mono_marshal_free", "void ptr", FALSE);
		register_icall (mono_marshal_set_last_error, "mono_marshal_set_last_error", "void", TRUE);
		register_icall (mono_marshal_set_last_error_windows, "mono_marshal_set_last_error_windows", "void int32", TRUE);
		register_icall (mono_string_utf8_to_builder, "mono_string_utf8_to_builder", "void ptr ptr", FALSE);
		register_icall (mono_string_utf8_to_builder2, "mono_string_utf8_to_builder2", "object ptr", FALSE);
		register_icall (mono_string_utf16_to_builder, "mono_string_utf16_to_builder", "void ptr ptr", FALSE);
		register_icall (mono_string_utf16_to_builder2, "mono_string_utf16_to_builder2", "object ptr", FALSE);
		register_icall (mono_marshal_free_array, "mono_marshal_free_array", "void ptr int32", FALSE);
		register_icall (mono_string_to_byvalstr, "mono_string_to_byvalstr", "void ptr ptr int32", FALSE);
		register_icall (mono_string_to_byvalwstr, "mono_string_to_byvalwstr", "void ptr ptr int32", FALSE);
		register_icall (g_free, "g_free", "void ptr", FALSE);
		register_icall_no_wrapper (mono_object_isinst_icall, "mono_object_isinst_icall", "object object ptr");
		register_icall (mono_struct_delete_old, "mono_struct_delete_old", "void ptr ptr", FALSE);
		register_icall (mono_delegate_begin_invoke, "mono_delegate_begin_invoke", "object object ptr", FALSE);
		register_icall (mono_delegate_end_invoke, "mono_delegate_end_invoke", "object object ptr", FALSE);
		register_icall (mono_gc_wbarrier_generic_nostore, "wb_generic", "void ptr", FALSE);
		register_icall (mono_gchandle_get_target, "mono_gchandle_get_target", "object int32", TRUE);
		register_icall (mono_gchandle_new, "mono_gchandle_new", "uint32 object bool", TRUE);
		register_icall (mono_marshal_isinst_with_cache, "mono_marshal_isinst_with_cache", "object object ptr ptr", FALSE);
		register_icall (mono_marshal_ftnptr_eh_callback, "mono_marshal_ftnptr_eh_callback", "void uint32", TRUE);
		register_icall (mono_threads_enter_gc_safe_region_unbalanced, "mono_threads_enter_gc_safe_region_unbalanced", "ptr ptr", TRUE);
		register_icall (mono_threads_exit_gc_safe_region_unbalanced, "mono_threads_exit_gc_safe_region_unbalanced", "void ptr ptr", TRUE);
		register_icall (mono_threads_attach_coop, "mono_threads_attach_coop", "ptr ptr ptr", TRUE);
		register_icall (mono_threads_detach_coop, "mono_threads_detach_coop", "void ptr ptr", TRUE);
		register_icall (mono_icall_start, "mono_icall_start", "ptr ptr ptr", TRUE);
		register_icall (mono_icall_end, "mono_icall_end", "void ptr ptr ptr", TRUE);
		register_icall (mono_icall_handle_new, "mono_icall_handle_new", "ptr ptr", TRUE);
		register_icall (mono_icall_handle_new_interior, "mono_icall_handle_new_interior", "ptr ptr", TRUE);

		mono_cominterop_init ();
		mono_remoting_init ();

		mono_counters_register ("MonoClass::class_marshal_info_count count",
								MONO_COUNTER_METADATA | MONO_COUNTER_INT, &class_marshal_info_count);

	}
}

void
mono_marshal_cleanup (void)
{
	mono_cominterop_cleanup ();

	mono_native_tls_free (load_type_info_tls_id);
	mono_native_tls_free (last_error_tls_id);
	mono_os_mutex_destroy (&marshal_mutex);
	marshal_mutex_initialized = FALSE;
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

/* This is a JIT icall, it sets the pending exception and return NULL on error */
gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate_raw)
{
	HANDLE_FUNCTION_ENTER ();
	MonoError error;
	MONO_HANDLE_DCL (MonoDelegate, delegate);
	gpointer result = mono_delegate_handle_to_ftnptr (delegate, &error);
	mono_error_set_pending_exception (&error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

gpointer
mono_delegate_handle_to_ftnptr (MonoDelegateHandle delegate, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	gpointer result = NULL;
	error_init (error);
	MonoMethod *method, *wrapper;
	MonoClass *klass;
	uint32_t target_handle = 0;

	if (MONO_HANDLE_IS_NULL (delegate))
		goto leave;

	if (MONO_HANDLE_GETVAL (delegate, delegate_trampoline)) {
		result = MONO_HANDLE_GETVAL (delegate, delegate_trampoline);
		goto leave;
	}

	klass = mono_handle_class (delegate);
	g_assert (klass->delegate);

	method = MONO_HANDLE_GETVAL (delegate, method);
	if (MONO_HANDLE_GETVAL (delegate, method_is_virtual)) {
		MonoObjectHandle delegate_target = MONO_HANDLE_NEW_GET (MonoObject, delegate, target);
		method = mono_object_handle_get_virtual_method (delegate_target, method, error);
		goto_if_nok (error, leave);
	}

	if (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL) {
		const char *exc_class, *exc_arg;
		gpointer ftnptr;

		ftnptr = mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
		if (!ftnptr) {
			g_assert (exc_class);
			mono_error_set_generic_error (error, "System", exc_class, "%s", exc_arg);
			goto leave;
		}
		result = ftnptr;
		goto leave;
	}

	MonoObjectHandle delegate_target = MONO_HANDLE_NEW_GET (MonoObject, delegate, target);
	if (!MONO_HANDLE_IS_NULL (delegate_target)) {
		/* Produce a location which can be embedded in JITted code */
		target_handle = mono_gchandle_new_weakref (MONO_HANDLE_RAW (delegate_target), FALSE); /* FIXME: a version of mono_gchandle_new_weakref that takes a coop handle */
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
		mono_gchandle_free (target_handle);
	HANDLE_FUNCTION_RETURN_VAL (result);
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
	guint32 gchandle = 0;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	if (mono_gc_is_moving ())
		gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (delegate_hash_table, d->delegate_trampoline));
	g_hash_table_remove (delegate_hash_table, d->delegate_trampoline);
	mono_marshal_unlock ();
	if (gchandle && mono_gc_is_moving ())
		mono_gchandle_free (gchandle);
}

static void
delegate_hash_table_add (MonoDelegateHandle d)
{
	guint32 gchandle;
	guint32 old_gchandle;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	gpointer delegate_trampoline = MONO_HANDLE_GETVAL (d, delegate_trampoline);
	if (mono_gc_is_moving ()) {
		gchandle = mono_gchandle_new_weakref ((MonoObject*) MONO_HANDLE_RAW (d), FALSE);
		old_gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (delegate_hash_table, delegate_trampoline));
		g_hash_table_insert (delegate_hash_table, delegate_trampoline, GUINT_TO_POINTER (gchandle));
		if (old_gchandle)
			mono_gchandle_free (old_gchandle);
	} else {
		g_hash_table_insert (delegate_hash_table, delegate_trampoline, MONO_HANDLE_RAW (d));
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
	MonoError error;
	MonoCustomAttrInfo *cinfo;
	MonoReflectionUnmanagedFunctionPointerAttribute *attr;

	/* The attribute is only available in Net 2.0 */
	if (mono_class_try_get_unmanaged_function_pointer_attribute_class ()) {
		/* 
		 * The pinvoke attributes are stored in a real custom attribute so we have to
		 * construct it.
		 */
		cinfo = mono_custom_attrs_from_class_checked (klass, &error);
		if (!mono_error_ok (&error)) {
			g_warning ("Could not load UnmanagedFunctionPointerAttribute due to %s", mono_error_get_message (&error));
			mono_error_cleanup (&error);
		}
		if (cinfo && !mono_runtime_get_no_exec ()) {
			attr = (MonoReflectionUnmanagedFunctionPointerAttribute*)mono_custom_attrs_get_attr_checked (cinfo, mono_class_try_get_unmanaged_function_pointer_attribute_class (), &error);
			if (attr) {
				piinfo->piflags = (attr->call_conv << 8) | (attr->charset ? (attr->charset - 1) * 2 : 1) | attr->set_last_error;
			} else {
				if (!mono_error_ok (&error)) {
					g_warning ("Could not load UnmanagedFunctionPointerAttribute due to %s", mono_error_get_message (&error));
					mono_error_cleanup (&error);
				}
			}
			if (!cinfo->cached)
				mono_custom_attrs_free (cinfo);
		}
	}
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error */
MonoDelegate*
mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn)
{
	HANDLE_FUNCTION_ENTER ();
	MonoError error;
	MonoDelegateHandle result = mono_ftnptr_to_delegate_handle (klass, ftn, &error);
	mono_error_set_pending_exception (&error);
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

MonoDelegateHandle
mono_ftnptr_to_delegate_handle (MonoClass *klass, gpointer ftn, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	error_init (error);
	guint32 gchandle;
	MonoDelegateHandle d = MONO_HANDLE_NEW (MonoDelegate, NULL);

	if (ftn == NULL)
		goto leave;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();

	if (mono_gc_is_moving ()) {
		gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (delegate_hash_table, ftn));
		mono_marshal_unlock ();
		if (gchandle)
			MONO_HANDLE_ASSIGN (d, MONO_HANDLE_CAST (MonoDelegate, mono_gchandle_get_target_handle (gchandle)));
	} else {
		MONO_HANDLE_ASSIGN (d, MONO_HANDLE_NEW (MonoDelegate, g_hash_table_lookup (delegate_hash_table, ftn)));
		mono_marshal_unlock ();
	}
	if (MONO_HANDLE_IS_NULL (d)) {
		/* This is a native function, so construct a delegate for it */
		MonoMethodSignature *sig;
		MonoMethod *wrapper;
		MonoMarshalSpec **mspecs;
		MonoMethod *invoke = mono_get_delegate_invoke (klass);
		MonoMethodPInvoke piinfo;
		MonoObjectHandle  this_obj;
		int i;

		if (use_aot_wrappers) {
			wrapper = mono_marshal_get_native_func_wrapper_aot (klass);
			this_obj = MONO_HANDLE_NEW (MonoObject, mono_value_box_checked (mono_domain_get (), mono_defaults.int_class, &ftn, error));
			goto_if_nok (error, leave);
		} else {
			memset (&piinfo, 0, sizeof (piinfo));
			parse_unmanaged_function_pointer_attr (klass, &piinfo);

			mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature (invoke)->param_count + 1);
			mono_method_get_marshal_info (invoke, mspecs);
			/* Freed below so don't alloc from mempool */
			sig = mono_metadata_signature_dup (mono_method_signature (invoke));
			sig->hasthis = 0;

			wrapper = mono_marshal_get_native_func_wrapper (klass->image, sig, &piinfo, mspecs, ftn);
			this_obj = MONO_HANDLE_NEW (MonoObject, NULL);

			for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
				if (mspecs [i])
					mono_metadata_free_marshal_spec (mspecs [i]);
			g_free (mspecs);
			g_free (sig);
		}

		MONO_HANDLE_ASSIGN (d, MONO_HANDLE_NEW (MonoDelegate, mono_object_new_checked (mono_domain_get (), klass, error)));
		goto_if_nok (error, leave);
		gpointer compiled_ptr = mono_compile_method_checked (wrapper, error);
		goto_if_nok (error, leave);

		mono_delegate_ctor_with_method (MONO_HANDLE_CAST (MonoObject, d), this_obj, compiled_ptr, wrapper, error);
		goto_if_nok (error, leave);
	}

	g_assert (!MONO_HANDLE_IS_NULL (d));
	if (MONO_HANDLE_DOMAIN (d) != mono_domain_get ())
		mono_error_set_not_supported (error, "Delegates cannot be marshalled from native code into a domain other than their home domain");

leave:
	HANDLE_FUNCTION_RETURN_REF (MonoDelegate, d);
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
		uint32_t gchandle;
		void **method_data;
		MonoMethod *method;

		ji = mono_jit_info_table_find (mono_domain_get (), (char *)mono_get_addr_from_ftnptr (ptr));
		g_assert (ji);

		method = mono_jit_info_get_method (ji);
		method_data = (void **)((MonoMethodWrapper*)method)->method_data;

		/*the target gchandle is the first entry after size and the wrapper itself.*/
		gchandle = GPOINTER_TO_UINT (method_data [2]);

		if (gchandle)
			mono_gchandle_free (gchandle);

		mono_runtime_free_method (mono_object_domain (delegate), method);
	}
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error */
static MonoString *
mono_string_from_byvalstr (const char *data, int max_len)
{
	MonoError error;
	MonoDomain *domain = mono_domain_get ();
	int len = 0;

	if (!data)
		return NULL;

	while (len < max_len - 1 && data [len])
		len++;

	MonoString *result = mono_string_new_len_checked (domain, data, len, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

/* This is a JIT icall, it sets the pending exception and return NULL on error */
static MonoString *
mono_string_from_byvalwstr (gunichar2 *data, int max_len)
{
	MonoError error;
	MonoString *res = NULL;
	MonoDomain *domain = mono_domain_get ();
	int len = 0;

	if (!data)
		return NULL;

	while (data [len]) len++;

	res = mono_string_new_utf16_checked (domain, data, MIN (len, max_len), &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}
	return res;
}

gpointer
mono_array_to_savearray (MonoArray *array)
{
	if (!array)
		return NULL;

	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_array_to_lparray (MonoArray *array)
{
#ifndef DISABLE_COM
	gpointer *nativeArray = NULL;
	int nativeArraySize = 0;

	int i = 0;
	MonoClass *klass;
	MonoError error;
#endif

	if (!array)
		return NULL;
#ifndef DISABLE_COM
	error_init (&error);
	klass = array->obj.vtable->klass;

	switch (klass->element_class->byval_arg.type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_CLASS:
		nativeArraySize = array->max_length;
		nativeArray = (void **)g_malloc (sizeof(gpointer) * nativeArraySize);
		for(i = 0; i < nativeArraySize; ++i) {
			nativeArray[i] = mono_cominterop_get_com_interface (((MonoObject **)array->vector)[i], klass->element_class, &error);
			if (mono_error_set_pending_exception (&error))
				break;
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
		g_warning ("type 0x%x not handled", klass->element_class->byval_arg.type);
		g_assert_not_reached ();
	}
#endif
	return array->vector;
}

void
mono_free_lparray (MonoArray *array, gpointer* nativeArray)
{
#ifndef DISABLE_COM
	MonoClass *klass;

	if (!array)
		return;

	if (!nativeArray)
		return;
	klass = array->obj.vtable->klass;

	if (klass->element_class->byval_arg.type == MONO_TYPE_CLASS)
		g_free (nativeArray);
#endif
}

static void
mono_byvalarray_to_array (MonoArray *arr, gpointer native_arr, MonoClass *elclass, guint32 elnum)
{
	g_assert (arr->obj.vtable->klass->element_class == mono_defaults.char_class);

	if (elclass == mono_defaults.byte_class) {
		GError *error = NULL;
		guint16 *ut;
		glong items_written;

		ut = g_utf8_to_utf16 ((const gchar *)native_arr, elnum, NULL, &items_written, &error);

		if (!error) {
			memcpy (mono_array_addr (arr, guint16, 0), ut, items_written * sizeof (guint16));
			g_free (ut);
		}
		else
			g_error_free (error);
	}
	else
		g_assert_not_reached ();
}

static void
mono_byvalarray_to_byte_array (MonoArray *arr, gpointer native_arr, guint32 elnum)
{
	mono_byvalarray_to_array (arr, native_arr, mono_defaults.byte_class, elnum);
}

/* This is a JIT icall, it sets the pending exception and returns on error */
static void
mono_array_to_byvalarray (gpointer native_arr, MonoArray *arr, MonoClass *elclass, guint32 elnum)
{
	g_assert (arr->obj.vtable->klass->element_class == mono_defaults.char_class);

	if (elclass == mono_defaults.byte_class) {
		char *as;
		GError *error = NULL;

		as = g_utf16_to_utf8 (mono_array_addr (arr, gunichar2, 0), mono_array_length (arr), NULL, NULL, &error);
		if (error) {
			mono_set_pending_exception (mono_get_exception_argument ("string", error->message));
			g_error_free (error);
			return;
		}

		memcpy (native_arr, as, MIN (strlen (as), elnum));
		g_free (as);
	} else {
		g_assert_not_reached ();
	}
}

static void
mono_array_to_byte_byvalarray (gpointer native_arr, MonoArray *arr, guint32 elnum)
{
	mono_array_to_byvalarray (native_arr, arr, mono_defaults.byte_class, elnum);
}

static MonoStringBuilder *
mono_string_builder_new (int starting_string_length)
{
	static MonoClass *string_builder_class;
	static MonoMethod *sb_ctor;
	static void *args [1];

	MonoError error;
	int initial_len = starting_string_length;

	if (initial_len < 0)
		initial_len = 0;

	if (!sb_ctor) {
		MonoMethodDesc *desc;
		MonoMethod *m;

		string_builder_class = mono_class_get_string_builder_class ();
		g_assert (string_builder_class);
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

	MonoStringBuilder *sb = (MonoStringBuilder*)mono_object_new_checked (mono_domain_get (), string_builder_class, &error);
	mono_error_assert_ok (&error);

	MonoObject *exc;
	mono_runtime_try_invoke (sb_ctor, sb, args, &exc, &error);
	g_assert (exc == NULL);
	mono_error_assert_ok (&error);

	g_assert (sb->chunkChars->max_length >= initial_len);

	return sb;
}

static void
mono_string_utf16_to_builder_copy (MonoStringBuilder *sb, gunichar2 *text, size_t string_len)
{
	gunichar2 *charDst = (gunichar2 *)sb->chunkChars->vector;
	gunichar2 *charSrc = (gunichar2 *)text;
	memcpy (charDst, charSrc, sizeof (gunichar2) * string_len);

	sb->chunkLength = string_len;

	return;
}

MonoStringBuilder *
mono_string_utf16_to_builder2 (gunichar2 *text)
{
	if (!text)
		return NULL;

	int len;
	for (len = 0; text [len] != 0; ++len);

	MonoStringBuilder *sb = mono_string_builder_new (len);
	mono_string_utf16_to_builder (sb, text);

	return sb;
}

void
mono_string_utf8_to_builder (MonoStringBuilder *sb, char *text)
{
	if (!sb || !text)
		return;

	GError *error = NULL;
	glong copied;
	gunichar2* ut = g_utf8_to_utf16 (text, strlen (text), NULL, &copied, &error);
	int capacity = mono_string_builder_capacity (sb);

	if (copied > capacity)
		copied = capacity;

	if (!error) {
		MONO_OBJECT_SETREF (sb, chunkPrevious, NULL);
		mono_string_utf16_to_builder_copy (sb, ut, copied);
	} else
		g_error_free (error);

	g_free (ut);
}

MonoStringBuilder *
mono_string_utf8_to_builder2 (char *text)
{
	if (!text)
		return NULL;

	int len = strlen (text);
	MonoStringBuilder *sb = mono_string_builder_new (len);
	mono_string_utf8_to_builder (sb, text);

	return sb;
}

void
mono_string_utf16_to_builder (MonoStringBuilder *sb, gunichar2 *text)
{
	if (!sb || !text)
		return;

	guint32 len;
	for (len = 0; text [len] != 0; ++len);
	
	if (len > mono_string_builder_capacity (sb))
		len = mono_string_builder_capacity (sb);

	mono_string_utf16_to_builder_copy (sb, text, len);
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
 * This is a JIT icall, it sets the pending exception and returns NULL on error.
 */
gchar*
mono_string_builder_to_utf8 (MonoStringBuilder *sb)
{
	MonoError error;
	GError *gerror = NULL;
	glong byte_count;
	if (!sb)
		return NULL;

	gunichar2 *str_utf16 = mono_string_builder_to_utf16 (sb);

	guint str_len = mono_string_builder_string_length (sb);

	gchar *tmp = g_utf16_to_utf8 (str_utf16, str_len, NULL, &byte_count, &gerror);

	if (gerror) {
		g_error_free (gerror);
		mono_marshal_free (str_utf16);
		mono_set_pending_exception (mono_get_exception_execution_engine ("Failed to convert StringBuilder from utf16 to utf8"));
		return NULL;
	} else {
		guint len = mono_string_builder_capacity (sb) + 1;
		gchar *res = (gchar *)mono_marshal_alloc (MAX (byte_count+1, len * sizeof (gchar)), &error);
		if (!mono_error_ok (&error)) {
			mono_marshal_free (str_utf16);
			g_free (tmp);
			mono_error_set_pending_exception (&error);
			return NULL;
		}

		memcpy (res, tmp, byte_count);
		res[byte_count] = '\0';

		mono_marshal_free (str_utf16);
		g_free (tmp);
		return res;
	}
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
 * This is a JIT icall, it sets the pending exception and returns NULL on error.
 */
gunichar2*
mono_string_builder_to_utf16 (MonoStringBuilder *sb)
{
	MonoError error;

	if (!sb)
		return NULL;

	g_assert (sb->chunkChars);

	guint len = mono_string_builder_capacity (sb);

	if (len == 0)
		len = 1;

	gunichar2 *str = (gunichar2 *)mono_marshal_alloc ((len + 1) * sizeof (gunichar2), &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	str[len] = '\0';

	if (len == 0)
		return str;

	MonoStringBuilder* chunk = sb;
	do {
		if (chunk->chunkLength > 0) {
			// Check that we will not overrun our boundaries.
			gunichar2 *source = (gunichar2 *)chunk->chunkChars->vector;

			if (chunk->chunkLength <= len) {
				memcpy (str + chunk->chunkOffset, source, chunk->chunkLength * sizeof(gunichar2));
			} else {
				g_error ("A chunk in the StringBuilder had a length longer than expected from the offset.");
			}

			len -= chunk->chunkLength;
		}
		chunk = chunk->chunkPrevious;
	} while (chunk != NULL);

	return str;
}

#ifndef HOST_WIN32
/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
static gpointer
mono_string_to_utf8str (MonoString *s)
{
	MonoError error;
	char *result = mono_string_to_utf8_checked (s, &error);
	mono_error_set_pending_exception (&error);
	return result;
}
#endif

gpointer
mono_string_to_ansibstr (MonoString *string_obj)
{
	g_error ("UnmanagedMarshal.BStr is not implemented.");
	return NULL;
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
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size)
{
	MonoError error;
	char *s;
	int len;

	g_assert (dst != NULL);
	g_assert (size > 0);

	memset (dst, 0, size);
	if (!src)
		return;

	s = mono_string_to_utf8_checked (src, &error);
	if (mono_error_set_pending_exception (&error))
		return;
	len = MIN (size, strlen (s));
	if (len >= size)
		len--;
	memcpy (dst, s, len);
	g_free (s);
}

/**
 * mono_string_to_byvalwstr:
 * \param dst Where to store the null-terminated utf16 decoded string.
 * \param src the \c MonoString to copy.
 * \param size the maximum number of wide characters to copy (each consumes 2 bytes)
 *
 * Copies the \c MonoString pointed to by \p src as a utf16 string into
 * \p dst, it copies at most \p size bytes into the destination (including
 * a terminating 16-bit zero terminator).
 */
void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size)
{
	int len;

	g_assert (dst != NULL);
	g_assert (size > 1);

	if (!src) {
		memset (dst, 0, size * 2);
		return;
	}

	len = MIN (size, (mono_string_length (src)));
	memcpy (dst, mono_string_chars (src), len * 2);
	if (size <= mono_string_length (src))
		len--;
	*((gunichar2 *) dst + len) = 0;
}

/* this is an icall, it sets the pending exception and returns NULL on error */
static MonoString*
mono_string_new_len_wrapper (const char *text, guint length)
{
	MonoError error;
	MonoString *result = mono_string_new_len_checked (mono_domain_get (), text, length, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

#ifdef ENABLE_ILGEN

/*
 * mono_mb_emit_exception_marshal_directive:
 *
 *   This function assumes ownership of MSG, which should be malloc-ed.
 */
static void
mono_mb_emit_exception_marshal_directive (MonoMethodBuilder *mb, char *msg)
{
	char *s;

	if (!mb->dynamic) {
		s = mono_image_strdup (mb->method->klass->image, msg);
		g_free (msg);
	} else {
		s = g_strdup (msg);
	}
	mono_mb_emit_exception_full (mb, "System.Runtime.InteropServices", "MarshalDirectiveException", s);
}
#endif /* ENABLE_ILGEN */

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
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			goto handle_enum;
		}
		return CEE_LDOBJ;
	case MONO_TYPE_TYPEDBYREF:
		return CEE_LDOBJ;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
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
		if (type->data.klass->enumtype) {
			type = mono_class_enum_basetype (type->data.klass);
			goto handle_enum;
		}
		return CEE_STOBJ;
	case MONO_TYPE_TYPEDBYREF:
		return CEE_STOBJ;
	case MONO_TYPE_GENERICINST:
		type = &type->data.generic_class->container_class->byval_arg;
		goto handle_enum;
	default:
		g_error ("unknown type 0x%02x in type_to_stind", type->type);
	}
	return -1;
}

#ifdef ENABLE_ILGEN

static void
emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I2);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eklass = NULL;
		int esize;

		if (type->type == MONO_TYPE_SZARRAY) {
			eklass = type->data.klass;
		} else {
			g_assert_not_reached ();
		}

		esize = mono_class_native_size (eklass, NULL);

		/* create a new array */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_op (mb, CEE_NEWARR, eklass);	
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		if (eklass->blittable) {
			/* copy the elements */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem * esize);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);			
		}
		else {
			int array_var, src_var, dst_var, index_var;
			guint32 label2, label3;

			array_var = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			/* set array_var */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_stloc (mb, array_var);
		
			/* save the old src pointer */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, src_var);
			/* save the old dst pointer */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_stloc (mb, dst_var);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);

			/* Loop header */
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* src is already set */

			/* Set dst */
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
			mono_mb_emit_stloc (mb, 1);

			/* Do the conversion */
			emit_struct_conv (mb, eklass, TRUE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label3);
		
			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY: {
		MonoClass *eclass = mono_defaults.char_class;

		/* create a new array */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_op (mb, CEE_NEWARR, eclass);	
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_byvalarray_to_byte_array);
		break;
	}
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
		if (mspec && mspec->native == MONO_NATIVE_BYVALTSTR && mspec->data.array_data.num_elem) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
			mono_mb_emit_icall (mb, mono_string_from_byvalstr);
		} else {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icall (mb, ves_icall_string_new_wrapper);
		}
		mono_mb_emit_byte (mb, CEE_STIND_REF);		
		break;
	case MONO_MARSHAL_CONV_STR_BYVALWSTR:
		if (mspec && mspec->native == MONO_NATIVE_BYVALTSTR && mspec->data.array_data.num_elem) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
			mono_mb_emit_icall (mb, mono_string_from_byvalwstr);
		} else {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icall (mb, ves_icall_mono_string_from_utf16);
		}
		mono_mb_emit_byte (mb, CEE_STIND_REF);		
		break;		
	case MONO_MARSHAL_CONV_STR_LPTSTR:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
#ifdef TARGET_WIN32
		mono_mb_emit_icall (mb, ves_icall_mono_string_from_utf16);
#else
		mono_mb_emit_icall (mb, ves_icall_string_new_wrapper);
#endif
		mono_mb_emit_byte (mb, CEE_STIND_REF);	
		break;

		// In Mono historically LPSTR was treated as a UTF8STR
	case MONO_MARSHAL_CONV_STR_LPSTR:
	case MONO_MARSHAL_CONV_STR_UTF8STR:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, ves_icall_string_new_wrapper);
		mono_mb_emit_byte (mb, CEE_STIND_REF);		
		break;
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, ves_icall_mono_string_from_utf16);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		MonoClass *klass = mono_class_from_mono_type (type);
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		/* *dst = new object */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);	
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	
		/* save the old src pointer */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, dst_var);

		/* dst = pointer to newly created object data */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, 1); 

		emit_struct_conv (mb, klass, TRUE);
		
		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_stloc (mb, 0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_stloc (mb, 1);
		break;
	}
	case MONO_MARSHAL_CONV_DEL_FTN: {
		MonoClass *klass = mono_class_from_mono_type (type);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, mono_ftnptr_to_delegate);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		g_error ("Structure field of type %s can't be marshalled as LPArray", mono_class_from_mono_type (type)->name);
		break;

#ifndef DISABLE_COM
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH:
		mono_cominterop_emit_ptr_to_object_conv (mb, type, conv, mspec);
		break;
#endif /* DISABLE_COM */

	case MONO_MARSHAL_CONV_SAFEHANDLE: {
		/*
		 * Passing SafeHandles as ref does not allow the unmanaged code
		 * to change the SafeHandle value.   If the value is changed,
		 * we should issue a diagnostic exception (NotSupportedException)
		 * that informs the user that changes to handles in unmanaged code
		 * is not supported. 
		 *
		 * Since we currently have no access to the original
		 * SafeHandle that was used during the marshalling,
		 * for now we just ignore this, and ignore/discard any
		 * changes that might have happened to the handle.
		 */
		break;
	}
		
	case MONO_MARSHAL_CONV_HANDLEREF: {
		/*
		 * Passing HandleRefs in a struct that is ref()ed does not 
		 * copy the values back to the HandleRef
		 */
		break;
	}
		
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	default: {
		char *msg = g_strdup_printf ("marshaling conversion %d not implemented", conv);

		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}
	}
}

static gpointer
conv_to_icall (MonoMarshalConv conv, int *ind_store_type)
{
	int dummy;
	if (!ind_store_type)
		ind_store_type = &dummy;
	*ind_store_type = CEE_STIND_I;
	switch (conv) {
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		return mono_marshal_string_to_utf16;		
	case MONO_MARSHAL_CONV_LPWSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return ves_icall_mono_string_from_utf16;
	case MONO_MARSHAL_CONV_LPTSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return ves_icall_string_new_wrapper;
	case MONO_MARSHAL_CONV_UTF8STR_STR:
	case MONO_MARSHAL_CONV_LPSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return ves_icall_string_new_wrapper;
	case MONO_MARSHAL_CONV_STR_LPTSTR:
#ifdef TARGET_WIN32
		return mono_marshal_string_to_utf16;
#else
		return mono_string_to_utf8str;
#endif
		// In Mono historically LPSTR was treated as a UTF8STR
	case MONO_MARSHAL_CONV_STR_UTF8STR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
		return mono_string_to_utf8str;
	case MONO_MARSHAL_CONV_STR_BSTR:
		return mono_string_to_bstr;
	case MONO_MARSHAL_CONV_BSTR_STR:
		*ind_store_type = CEE_STIND_REF;
		return mono_string_from_bstr_icall;
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		return mono_string_to_ansibstr;
	case MONO_MARSHAL_CONV_SB_UTF8STR:
	case MONO_MARSHAL_CONV_SB_LPSTR:
		return mono_string_builder_to_utf8;
	case MONO_MARSHAL_CONV_SB_LPTSTR:
#ifdef TARGET_WIN32
		return mono_string_builder_to_utf16;
#else
		return mono_string_builder_to_utf8;
#endif
	case MONO_MARSHAL_CONV_SB_LPWSTR:
		return mono_string_builder_to_utf16;
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
		return mono_array_to_savearray;
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		return mono_array_to_lparray;
	case MONO_MARSHAL_FREE_LPARRAY:
		return mono_free_lparray;
	case MONO_MARSHAL_CONV_DEL_FTN:
		return mono_delegate_to_ftnptr;
	case MONO_MARSHAL_CONV_FTN_DEL:
		*ind_store_type = CEE_STIND_REF;
		return mono_ftnptr_to_delegate;
	case MONO_MARSHAL_CONV_UTF8STR_SB:
	case MONO_MARSHAL_CONV_LPSTR_SB:
		*ind_store_type = CEE_STIND_REF;
		return mono_string_utf8_to_builder;
	case MONO_MARSHAL_CONV_LPTSTR_SB:
		*ind_store_type = CEE_STIND_REF;
#ifdef TARGET_WIN32
		return mono_string_utf16_to_builder;
#else
		return mono_string_utf8_to_builder;
#endif
	case MONO_MARSHAL_CONV_LPWSTR_SB:
		*ind_store_type = CEE_STIND_REF;
		return mono_string_utf16_to_builder;
	case MONO_MARSHAL_FREE_ARRAY:
		return mono_marshal_free_array;
	case MONO_MARSHAL_CONV_STR_BYVALSTR:
		return mono_string_to_byvalstr;
	case MONO_MARSHAL_CONV_STR_BYVALWSTR:
		return mono_string_to_byvalwstr;
	default:
		g_assert_not_reached ();
	}

	return NULL;
}

static void
emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
	int pos;
	int stind_op;

	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_STIND_I4);
		break;
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_NEG);
		mono_mb_emit_byte (mb, CEE_STIND_I2);
		break;
	// In Mono historically LPSTR was treated as a UTF8STR
	case MONO_MARSHAL_CONV_STR_UTF8STR:
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR: {
		int pos;

		/* free space if free == true */
		mono_mb_emit_ldloc (mb, 2);
		pos = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, g_free);
		mono_mb_patch_short_branch (mb, pos);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
		mono_mb_emit_byte (mb, stind_op);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_DEL_FTN:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
		mono_mb_emit_byte (mb, stind_op);
		break;
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: {
		g_assert (mspec);

		mono_mb_emit_ldloc (mb, 1); /* dst */
		mono_mb_emit_ldloc (mb, 0);	
		mono_mb_emit_byte (mb, CEE_LDIND_REF); /* src String */
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eklass = NULL;
		int esize;

		if (type->type == MONO_TYPE_SZARRAY) {
			eklass = type->data.klass;
		} else {
			g_assert_not_reached ();
		}

		if (eklass->valuetype)
			esize = mono_class_native_size (eklass, NULL);
		else
			esize = sizeof (gpointer);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (eklass->blittable) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);	
			mono_mb_emit_byte (mb, CEE_LDIND_REF);	
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_icon (mb, mspec->data.array_data.num_elem * esize);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);			
		} else {
			int array_var, src_var, dst_var, index_var;
			guint32 label2, label3;

			array_var = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			/* set array_var */
			mono_mb_emit_ldloc (mb, 0);	
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_stloc (mb, array_var);

			/* save the old src pointer */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, src_var);
			/* save the old dst pointer */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_stloc (mb, dst_var);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);

			/* Loop header */
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* Set src */
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
			mono_mb_emit_stloc (mb, 0);

			/* dst is already set */

			/* Do the conversion */
			emit_struct_conv (mb, eklass, FALSE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label3);
		
			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}

		mono_mb_patch_branch (mb, pos);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY: {
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		pos = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);	
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_array_to_byte_byvalarray);
		mono_mb_patch_short_branch (mb, pos);
		break;
	}
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
		
		/* save the old src pointer */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, dst_var);

		/* src = pointer to object data */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, 0); 

		emit_struct_conv (mb, mono_class_from_mono_type (type), FALSE);
		
		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_stloc (mb, 0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_stloc (mb, 1);

		mono_mb_patch_branch (mb, pos);
		break;
	}

#ifndef DISABLE_COM
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN:
		mono_cominterop_emit_object_to_ptr_conv (mb, type, conv, mspec);
		break;
#endif /* DISABLE_COM */

	case MONO_MARSHAL_CONV_SAFEHANDLE: {
		int pos;
		
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception (mb, "ArgumentNullException", NULL);
		mono_mb_patch_branch (mb, pos);
		
		/* Pull the handle field from SafeHandle */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}

	case MONO_MARSHAL_CONV_HANDLEREF: {
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoHandleRef, handle));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}
		
	default: {
		g_error ("marshalling conversion %d not implemented", conv);
	}
	}
}

static int
offset_of_first_nonstatic_field (MonoClass *klass)
{
	int i;
	int fcount = mono_class_get_field_count (klass);
	mono_class_setup_fields (klass);
	for (i = 0; i < fcount; i++) {
		if (!(klass->fields[i].type->attrs & FIELD_ATTRIBUTE_STATIC) && !mono_field_is_deleted (&klass->fields[i]))
			return klass->fields[i].offset - sizeof (MonoObject);
	}

	return 0;
}

static gboolean
get_fixed_buffer_attr (MonoClassField *field, MonoType **out_etype, int *out_len)
{
	MonoError error;
	MonoCustomAttrInfo *cinfo;
	MonoCustomAttrEntry *attr;
	int aindex;

	cinfo = mono_custom_attrs_from_field_checked (field->parent, field, &error);
	if (!is_ok (&error))
		return FALSE;
	attr = NULL;
	if (cinfo) {
		for (aindex = 0; aindex < cinfo->num_attrs; ++aindex) {
			MonoClass *ctor_class = cinfo->attrs [aindex].ctor->klass;
			if (mono_class_has_parent (ctor_class, mono_class_get_fixed_buffer_attribute_class ())) {
				attr = &cinfo->attrs [aindex];
				break;
			}
		}
	}
	if (attr) {
		MonoArray *typed_args, *named_args;
		CattrNamedArg *arginfo;
		MonoObject *o;

		mono_reflection_create_custom_attr_data_args (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size, &typed_args, &named_args, &arginfo, &error);
		if (!is_ok (&error))
			return FALSE;
		g_assert (mono_array_length (typed_args) == 2);

		/* typed args */
		o = mono_array_get (typed_args, MonoObject*, 0);
		*out_etype = monotype_cast (o)->type;
		o = mono_array_get (typed_args, MonoObject*, 1);
		g_assert (o->vtable->klass == mono_defaults.int32_class);
		*out_len = *(gint32*)mono_object_unbox (o);
		g_free (arginfo);
	}
	if (cinfo && !cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return attr != NULL;
}

static void
emit_fixed_buf_conv (MonoMethodBuilder *mb, MonoType *type, MonoType *etype, int len, gboolean to_object, int *out_usize)
{
	MonoClass *klass = mono_class_from_mono_type (type);
	MonoClass *eklass = mono_class_from_mono_type (etype);
	int esize;

	esize = mono_class_native_size (eklass, NULL);

	MonoMarshalNative string_encoding = klass->unicode ? MONO_NATIVE_LPWSTR : MONO_NATIVE_LPSTR;
	int usize = mono_class_value_size (eklass, NULL);
	int msize = mono_class_value_size (eklass, NULL);

	//printf ("FIXED: %s %d %d\n", mono_type_full_name (type), eklass->blittable, string_encoding);

	if (eklass->blittable) {
		/* copy the elements */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, len * esize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {
		int index_var;
		guint32 label2, label3;

		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);

		/* Loop header */
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_icon (mb, len);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* src/dst is already set */

		/* Do the conversion */
		MonoTypeEnum t = etype->type;
		switch (t) {
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);
			if (t == MONO_TYPE_CHAR && string_encoding != MONO_NATIVE_LPWSTR) {
				if (to_object) {
					mono_mb_emit_byte (mb, CEE_LDIND_U1);
					mono_mb_emit_byte (mb, CEE_STIND_I2);
				} else {
					mono_mb_emit_byte (mb, CEE_LDIND_U2);
					mono_mb_emit_byte (mb, CEE_STIND_I1);
				}
				usize = 1;
			} else {
				mono_mb_emit_byte (mb, mono_type_to_ldind (etype));
				mono_mb_emit_byte (mb, mono_type_to_stind (etype));
			}
			break;
		default:
			g_assert_not_reached ();
			break;
		}

		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, msize);
		} else {
			mono_mb_emit_add_to_local (mb, 0, msize);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}

		/* Loop footer */
		mono_mb_emit_add_to_local (mb, index_var, 1);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label3);
	}

	*out_usize = usize * len;
}

static void
emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object,
						int offset_of_first_child_field, MonoMarshalNative string_encoding)
{
	MonoMarshalType *info;
	int i;

	if (klass->parent)
		emit_struct_conv_full (mb, klass->parent, to_object, offset_of_first_nonstatic_field (klass), string_encoding);

	info = mono_marshal_load_type_info (klass);

	if (info->native_size == 0)
		return;

	if (klass->blittable) {
		int usize = mono_class_value_size (klass, NULL);
		g_assert (usize == info->native_size);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, usize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);

		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, offset_of_first_child_field);
		} else {
			mono_mb_emit_add_to_local (mb, 0, offset_of_first_child_field);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}
		return;
	}

	if (klass != mono_class_try_get_safehandle_class ()) {
		if (mono_class_is_auto_layout (klass)) {
			char *msg = g_strdup_printf ("Type %s which is passed to unmanaged code must have a StructLayout attribute.",
										 mono_type_full_name (&klass->byval_arg));
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return;
		}
	}

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		int msize = 0;
		int usize = 0;
		gboolean last_field = i < (info->num_fields -1) ? 0 : 1;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = (MonoMarshalNative)mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, klass->unicode, &conv);

		if (last_field) {
			msize = klass->instance_size - info->fields [i].field->offset;
			usize = info->native_size - info->fields [i].offset;
		} else {
			msize = info->fields [i + 1].field->offset - info->fields [i].field->offset;
			usize = info->fields [i + 1].offset - info->fields [i].offset;
		}

		if (klass != mono_class_try_get_safehandle_class ()){
			/* 
			 * FIXME: Should really check for usize==0 and msize>0, but we apply 
			 * the layout to the managed structure as well.
			 */
			
			if (mono_class_is_explicit_layout (klass) && (usize == 0)) {
				if (MONO_TYPE_IS_REFERENCE (info->fields [i].field->type) ||
				    ((!last_field && MONO_TYPE_IS_REFERENCE (info->fields [i + 1].field->type))))
					g_error ("Type %s which has an [ExplicitLayout] attribute cannot have a "
						 "reference field at the same offset as another field.",
						 mono_type_full_name (&klass->byval_arg));
			}
		}
		
		switch (conv) {
		case MONO_MARSHAL_CONV_NONE: {
			int t;

			//XXX a byref field!?!? that's not allowed! and worse, it might miss a WB
			g_assert (!ftype->byref);
			if (ftype->type == MONO_TYPE_I || ftype->type == MONO_TYPE_U) {
				mono_mb_emit_ldloc (mb, 1);
				mono_mb_emit_ldloc (mb, 0);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_byte (mb, CEE_STIND_I);
				break;
			}

		handle_enum:
			t = ftype->type;
			switch (t) {
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
			case MONO_TYPE_PTR:
			case MONO_TYPE_R4:
			case MONO_TYPE_R8:
				mono_mb_emit_ldloc (mb, 1);
				mono_mb_emit_ldloc (mb, 0);
				if (t == MONO_TYPE_CHAR && ntype == MONO_NATIVE_U1 && string_encoding != MONO_NATIVE_LPWSTR) {
					if (to_object) {
						mono_mb_emit_byte (mb, CEE_LDIND_U1);
						mono_mb_emit_byte (mb, CEE_STIND_I2);
					} else {
						mono_mb_emit_byte (mb, CEE_LDIND_U2);
						mono_mb_emit_byte (mb, CEE_STIND_I1);
					}
				} else {
					mono_mb_emit_byte (mb, mono_type_to_ldind (ftype));
					mono_mb_emit_byte (mb, mono_type_to_stind (ftype));
				}
				break;
			case MONO_TYPE_VALUETYPE: {
				int src_var, dst_var;
				MonoType *etype;
				int len;

				if (ftype->data.klass->enumtype) {
					ftype = mono_class_enum_basetype (ftype->data.klass);
					goto handle_enum;
				}

				src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
				dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	
				/* save the old src pointer */
				mono_mb_emit_ldloc (mb, 0);
				mono_mb_emit_stloc (mb, src_var);
				/* save the old dst pointer */
				mono_mb_emit_ldloc (mb, 1);
				mono_mb_emit_stloc (mb, dst_var);

				if (get_fixed_buffer_attr (info->fields [i].field, &etype, &len)) {
					emit_fixed_buf_conv (mb, ftype, etype, len, to_object, &usize);
				} else {
					emit_struct_conv (mb, ftype->data.klass, to_object);
				}

				/* restore the old src pointer */
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_stloc (mb, 0);
				/* restore the old dst pointer */
				mono_mb_emit_ldloc (mb, dst_var);
				mono_mb_emit_stloc (mb, 1);
				break;
			}
			case MONO_TYPE_OBJECT: {
#ifndef DISABLE_COM
				if (to_object) {
					static MonoMethod *variant_clear = NULL;
					static MonoMethod *get_object_for_native_variant = NULL;

					if (!variant_clear)
						variant_clear = mono_class_get_method_from_name (mono_class_get_variant_class (), "Clear", 0);
					if (!get_object_for_native_variant)
						get_object_for_native_variant = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1);
					mono_mb_emit_ldloc (mb, 1);
					mono_mb_emit_ldloc (mb, 0);
					mono_mb_emit_managed_call (mb, get_object_for_native_variant, NULL);
					mono_mb_emit_byte (mb, CEE_STIND_REF);

					mono_mb_emit_ldloc (mb, 0);
					mono_mb_emit_managed_call (mb, variant_clear, NULL);
				}
				else {
					static MonoMethod *get_native_variant_for_object = NULL;

					if (!get_native_variant_for_object)
						get_native_variant_for_object = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetNativeVariantForObject", 2);

					mono_mb_emit_ldloc (mb, 0);
					mono_mb_emit_byte(mb, CEE_LDIND_REF);
					mono_mb_emit_ldloc (mb, 1);
					mono_mb_emit_managed_call (mb, get_native_variant_for_object, NULL);
				}
#else
				char *msg = g_strdup_printf ("COM support was disabled at compilation time.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
#endif
				break;
			}

			default: 
				g_warning ("marshaling type %02x not implemented", ftype->type);
				g_assert_not_reached ();
			}
			break;
		}
		default: {
			int src_var, dst_var;

			src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			/* save the old src pointer */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, src_var);
			/* save the old dst pointer */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_stloc (mb, dst_var);

			if (to_object) 
				emit_ptr_to_object_conv (mb, ftype, conv, info->fields [i].mspec);
			else
				emit_object_to_ptr_conv (mb, ftype, conv, info->fields [i].mspec);

			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}
		}

		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, msize);
		} else {
			mono_mb_emit_add_to_local (mb, 0, msize);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}				
	}
}

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
{
	emit_struct_conv_full (mb, klass, to_object, 0, (MonoMarshalNative)-1);
}

static void
emit_struct_free (MonoMethodBuilder *mb, MonoClass *klass, int struct_var)
{
	/* Call DestroyStructure */
	/* FIXME: Only do this if needed */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
	mono_mb_emit_ldloc (mb, struct_var);
	mono_mb_emit_icall (mb, mono_struct_delete_old);
}

static void
emit_thread_interrupt_checkpoint_call (MonoMethodBuilder *mb, gpointer checkpoint_func)
{
	int pos_noabort, pos_noex;

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR_INT_REQ_FLAG);
	mono_mb_emit_byte (mb, CEE_LDIND_U4);
	pos_noabort = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	mono_mb_emit_icall (mb, checkpoint_func);
	/* Throw the exception returned by the checkpoint function, if any */
	mono_mb_emit_byte (mb, CEE_DUP);
	pos_noex = mono_mb_emit_branch (mb, CEE_BRFALSE);
	mono_mb_emit_byte (mb, CEE_THROW);
	mono_mb_patch_branch (mb, pos_noex);
	mono_mb_emit_byte (mb, CEE_POP);
	
	mono_mb_patch_branch (mb, pos_noabort);
}

static void
emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	if (strstr (mb->name, "mono_thread_interruption_checkpoint"))
		return;
	
	emit_thread_interrupt_checkpoint_call (mb, mono_thread_interruption_checkpoint);
}

static void
emit_thread_force_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_interrupt_checkpoint_call (mb, mono_thread_force_interruption_checkpoint_noraise);
}

void
mono_marshal_emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_interrupt_checkpoint (mb);
}

void
mono_marshal_emit_thread_force_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_force_interrupt_checkpoint (mb);
}

#endif /* ENABLE_ILGEN */

/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoError error;
	MonoMulticastDelegate *mcast_delegate;
	MonoClass *klass;
	MonoMethod *method;

	g_assert (delegate);
	mcast_delegate = (MonoMulticastDelegate *) delegate;
	if (mcast_delegate->delegates != NULL) {
		mono_set_pending_exception (mono_get_exception_argument (NULL, "The delegate must have only one target"));
		return NULL;
	}

#ifndef DISABLE_REMOTING
	if (delegate->target && mono_object_is_transparent_proxy (delegate->target)) {
		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		if (!mono_class_is_contextbound (tp->remote_class->proxy_class) || tp->rp->context != (MonoObject *) mono_context_get ()) {
			/* If the target is a proxy, make a direct call. Is proxy's work
			// to make the call asynchronous.
			*/
			MonoMethodMessage *msg;
			MonoDelegate *async_callback;
			MonoObject *state;
			MonoAsyncResult *ares;
			MonoObject *exc;
			MonoArray *out_args;
			method = delegate->method;

			msg = mono_method_call_message_new (mono_marshal_method_from_wrapper (method), params, NULL, &async_callback, &state, &error);
			if (mono_error_set_pending_exception (&error))
				return NULL;
			ares = mono_async_result_new (mono_domain_get (), NULL, state, NULL, NULL, &error);
			if (mono_error_set_pending_exception (&error))
				return NULL;
			MONO_OBJECT_SETREF (ares, async_delegate, (MonoObject *)delegate);
			MONO_OBJECT_SETREF (ares, async_callback, (MonoObject *)async_callback);
			MONO_OBJECT_SETREF (msg, async_result, ares);
			msg->call_type = CallType_BeginInvoke;

			exc = NULL;
			mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args, &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}
			if (exc)
				mono_set_pending_exception ((MonoException *) exc);
			return ares;
		}
	}
#endif

	klass = delegate->object.vtable->klass;

	method = mono_class_get_method_from_name (klass, "BeginInvoke", -1);
	if (!method)
		method = mono_get_delegate_invoke (klass);
	g_assert (method);

	MonoAsyncResult *result = mono_threadpool_begin_invoke (mono_domain_get (), (MonoObject*) delegate, method, params, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

#ifdef ENABLE_ILGEN

int
mono_mb_emit_save_args (MonoMethodBuilder *mb, MonoMethodSignature *sig, gboolean save_this)
{
	int i, params_var, tmp_var;

	/* allocate local (pointer) *params[] */
	params_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local (pointer) tmp */
	tmp_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/* alloate space on stack to store an array of pointers to the arguments */
	mono_mb_emit_icon (mb, sizeof (gpointer) * (sig->param_count + 1));
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LOCALLOC);
	mono_mb_emit_stloc (mb, params_var);

	/* tmp = params */
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_stloc (mb, tmp_var);

	if (save_this && sig->hasthis) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, 0);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (sig->param_count)
			mono_mb_emit_add_to_local (mb, tmp_var, sizeof (gpointer));

	}

	for (i = 0; i < sig->param_count; i++) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, i + sig->hasthis);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (i < (sig->param_count - 1))
			mono_mb_emit_add_to_local (mb, tmp_var, sizeof (gpointer));
	}

	return params_var;
}

#endif /* ENABLE_ILGEN */

static char*
mono_signature_to_name (MonoMethodSignature *sig, const char *prefix)
{
	int i;
	char *result;
	GString *res = g_string_new ("");

	if (prefix) {
		g_string_append (res, prefix);
		g_string_append_c (res, '_');
	}

	mono_type_get_desc (res, sig->ret, FALSE);

	if (sig->hasthis)
		g_string_append (res, "__this__");

	for (i = 0; i < sig->param_count; ++i) {
		g_string_append_c (res, '_');
		mono_type_get_desc (res, sig->params [i], FALSE);
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

/**
 * mono_marshal_get_string_encoding:
 *
 *  Return the string encoding which should be used for a given parameter.
 */
static MonoMarshalNative
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

static MonoMarshalConv
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
	default:
		return MONO_MARSHAL_CONV_INVALID;
	}
}

static MonoMarshalConv
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

static MonoMarshalConv
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
	default:
		return MONO_MARSHAL_CONV_INVALID;
	}
}

static MonoMarshalConv
mono_marshal_get_ptr_to_stringbuilder_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec, gboolean *need_free)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	*need_free = TRUE;

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		/* 
		 * mono_string_builder_to_utf16 does not allocate a 
		 * new buffer, so no need to free it.
		 */
		*need_free = FALSE;
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
static gboolean
mono_marshal_need_free (MonoType *t, MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding;

	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		/* FIXME: Optimize this */
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
		if (t->data.klass == mono_defaults.stringbuilder_class) {
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
	case MONO_WRAPPER_REMOTING_INVOKE:
	case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK:
	case MONO_WRAPPER_XDOMAIN_INVOKE:
		m = info->d.remoting.method;
		if (wrapper->is_inflated) {
			MonoError error;
			MonoMethod *result;
			/*
			 * A method cannot be inflated and a wrapper at the same time, so the wrapper info
			 * contains an uninflated method.
			 */
			result = mono_class_inflate_generic_method_checked (m, mono_method_get_context (wrapper), &error);
			g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
			return result;
		}
		return m;
	case MONO_WRAPPER_SYNCHRONIZED:
		m = info->d.synchronized.method;
		if (wrapper->is_inflated) {
			MonoError error;
			MonoMethod *result;
			result = mono_class_inflate_generic_method_checked (m, mono_method_get_context (wrapper), &error);
			g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
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

	info = (WrapperInfo *)mono_image_alloc0 (mb->method->klass->image, sizeof (WrapperInfo));
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
	MonoError error;
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
		klass = mono_class_get_checked (image, mono_metadata_make_token (MONO_TABLE_TYPEDEF, 1), &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
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
		MonoError error;
		inst = mono_class_inflate_generic_method_checked (def, ctx, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
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
	MonoError error;
	MonoMethod *inst, *res;

	/*
	 * We use the same cache for the generic definition and the instances.
	 */
	inst = mono_class_inflate_generic_method_checked (def, ctx, &error);
	g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
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
	MonoError error;
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
		inst = mono_class_inflate_generic_method_checked (def, ctx, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */

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
	MonoError error;
	MonoMethod *inst, *res;
	WrapperInfo *ginfo, *info;

	/*
	 * We use the same cache for the generic definition and the instances.
	 */
	inst = mono_class_inflate_generic_method_checked (def, ctx, &error);
	g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */

	ginfo = mono_marshal_get_wrapper_info (def);
	if (ginfo) {
		info = (WrapperInfo *)mono_image_alloc0 (def->klass->image, sizeof (WrapperInfo));
		info->subtype = ginfo->subtype;
		if (info->subtype == WRAPPER_SUBTYPE_NONE) {
			info->d.delegate_invoke.method = mono_class_inflate_generic_method_checked (ginfo->d.delegate_invoke.method, ctx, &error);
			mono_error_assert_ok (&error);
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
	int params_var;
	char *name;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
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
		cache = get_cache (&method->klass->image->wrapper_caches.delegate_begin_invoke_cache,
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
		mb = mono_mb_new (get_wrapper_target_class (method->klass->image), name, MONO_WRAPPER_DELEGATE_BEGIN_INVOKE);
	g_free (name);

#ifdef ENABLE_ILGEN
	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_begin_invoke);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	if (ctx) {
		MonoMethod *def;
		def = mono_mb_create_and_cache (cache, method->klass, mb, sig, sig->param_count + 16);
		res = cache_generic_delegate_wrapper (cache, orig_method, def, ctx);
	} else {
		res = mono_mb_create_and_cache (cache, sig, mb, sig, sig->param_count + 16);
	}

	mono_mb_free (mb);
	return res;
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
static MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoError error;
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	MonoMethod *method = NULL;
	MonoMethodSignature *sig;
	MonoMethodMessage *msg;
	MonoObject *res, *exc;
	MonoArray *out_args;
	MonoClass *klass;

	g_assert (delegate);

	if (!delegate->method_info) {
		g_assert (delegate->method);
		MonoReflectionMethod *rm = mono_method_get_object_checked (domain, delegate->method, NULL, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return NULL;
		}
		MONO_OBJECT_SETREF (delegate, method_info, rm);
	}

	if (!delegate->method_info || !delegate->method_info->method)
		g_assert_not_reached ();

	klass = delegate->object.vtable->klass;

	method = mono_class_get_method_from_name (klass, "EndInvoke", -1);
	g_assert (method != NULL);

	sig = mono_signature_no_pinvoke (method);

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	ares = (MonoAsyncResult *)mono_array_get (msg->args, gpointer, sig->param_count - 1);
	if (ares == NULL) {
		mono_set_pending_exception (mono_exception_from_name_msg (mono_defaults.corlib, "System.Runtime.Remoting", "RemotingException", "The async result object is null or of an unexpected type."));
		return NULL;
	}

	if (ares->async_delegate != (MonoObject*)delegate) {
		mono_set_pending_exception (mono_get_exception_invalid_operation (
			"The IAsyncResult object provided does not match this delegate."));
		return NULL;
	}

#ifndef DISABLE_REMOTING
	if (delegate->target && mono_object_is_transparent_proxy (delegate->target)) {
		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		msg = (MonoMethodMessage *)mono_object_new_checked (domain, mono_defaults.mono_method_message_class, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return NULL;
		}
		mono_message_init (domain, msg, delegate->method_info, NULL, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;
		msg->call_type = CallType_EndInvoke;
		MONO_OBJECT_SETREF (msg, async_result, ares);
		res = mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return NULL;
		}
	} else
#endif
	{
		res = mono_threadpool_end_invoke (ares, &out_args, &exc, &error);
		if (mono_error_set_pending_exception (&error))
			return NULL;
	}

	if (exc) {
		if (((MonoException*)exc)->stack_trace) {
			MonoError inner_error;
			char *strace = mono_string_to_utf8_checked (((MonoException*)exc)->stack_trace, &inner_error);
			if (is_ok (&inner_error)) {
				char  *tmp;
				tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
				g_free (strace);
				MonoString *tmp_str = mono_string_new_checked (domain, tmp, &inner_error);
				g_free (tmp);
				if (is_ok (&inner_error))
					MONO_OBJECT_SETREF (((MonoException*)exc), stack_trace, tmp_str);
			};
			if (!is_ok (&inner_error))
				mono_error_cleanup (&inner_error); /* no stack trace, but at least throw the original exception */
		}
		mono_set_pending_exception ((MonoException*)exc);
	}

	mono_method_return_message_restore (method, params, out_args, &error);
	mono_error_set_pending_exception (&error);
	return res;
}

#ifdef ENABLE_ILGEN

void
mono_mb_emit_restore_result (MonoMethodBuilder *mb, MonoType *return_type)
{
	MonoType *t = mono_type_get_underlying_type (return_type);

	if (return_type->byref)
		return_type = &mono_defaults.int_class->byval_arg;

	switch (t->type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_PTR:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS: 
	case MONO_TYPE_OBJECT: 
	case MONO_TYPE_ARRAY: 
	case MONO_TYPE_SZARRAY: 
		/* nothing to do */
		break;
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
		mono_mb_emit_op (mb, CEE_UNBOX, mono_class_from_mono_type (return_type));
		mono_mb_emit_byte (mb, mono_type_to_ldind (return_type));
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			break;
		/* fall through */
	case MONO_TYPE_VALUETYPE: {
		MonoClass *klass = mono_class_from_mono_type (return_type);
		mono_mb_emit_op (mb, CEE_UNBOX, klass);
		mono_mb_emit_op (mb, CEE_LDOBJ, klass);
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR: {
		MonoClass *klass = mono_class_from_mono_type (return_type);
		mono_mb_emit_op (mb, CEE_UNBOX_ANY, klass);
		break;
	}
	default:
		g_warning ("type 0x%x not handled", return_type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

#endif /* ENABLE_ILGEN */

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
	int params_var;
	char *name;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
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
		cache = get_cache (&method->klass->image->wrapper_caches.delegate_end_invoke_cache,
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
		mb = mono_mb_new (get_wrapper_target_class (method->klass->image), name, MONO_WRAPPER_DELEGATE_END_INVOKE);
	g_free (name);

#ifdef ENABLE_ILGEN
	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_end_invoke);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else
		mono_mb_emit_restore_result (mb, sig->ret);
#endif

	if (ctx) {
		MonoMethod *def;
		def = mono_mb_create_and_cache (cache, method->klass, mb, sig, sig->param_count + 16);
		res = cache_generic_delegate_wrapper (cache, orig_method, def, ctx);
	} else {
		res = mono_mb_create_and_cache (cache, sig,
										mb, sig, sig->param_count + 16);
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
	MonoMethodSignature *sig, *static_sig, *invoke_sig;
	int i;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	gpointer cache_key = NULL;
	SignaturePointerPair key = { NULL, NULL };
	SignaturePointerPair *new_key;
	int local_i, local_len, local_delegates, local_d, local_target, local_res;
	int pos0, pos1, pos2;
	char *name;
	MonoClass *target_class = NULL;
	gboolean closed_over_null = FALSE;
	MonoGenericContext *ctx = NULL;
	MonoGenericContainer *container = NULL;
	MonoMethod *orig_method = method;
	WrapperInfo *info;
	WrapperSubtype subtype = WRAPPER_SUBTYPE_NONE;
	gboolean found;
	gboolean void_ret;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
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
			MonoError error;
			MonoType *target_type;

			g_assert (method->signature->hasthis);
			target_type = mono_class_inflate_generic_type_checked (method->signature->params [0],
				mono_method_get_context (method), &error);
			mono_error_assert_ok (&error); /* FIXME don't swallow the error */
			target_class = mono_class_from_mono_type (target_type);
		} else {
			target_class = target_method->klass;
		}

		closed_over_null = sig->param_count == mono_method_signature (target_method)->param_count;
	}

	if (static_method_with_first_arg_bound) {
		subtype = WRAPPER_SUBTYPE_DELEGATE_INVOKE_BOUND;
		g_assert (!callvirt);
		invoke_sig = mono_method_signature (target_method);
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
		cache = get_cache (&method->klass->image->delegate_bound_static_invoke_cache,
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
		cache = get_cache (&method->klass->image->wrapper_caches.delegate_invoke_cache,
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)mono_metadata_signature_equal);
		res = mono_marshal_find_in_cache (cache, sig);
		if (res)
			return res;
		cache_key = sig;
	}

	static_sig = mono_metadata_signature_dup_full (method->klass->image, sig);
	static_sig->hasthis = 0;
	if (!static_method_with_first_arg_bound)
		invoke_sig = static_sig;

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
		mb = mono_mb_new (get_wrapper_target_class (method->klass->image), name, MONO_WRAPPER_DELEGATE_INVOKE);
	g_free (name);

#ifdef ENABLE_ILGEN
	void_ret = sig->ret->type == MONO_TYPE_VOID && !method->string_ctor;

	/* allocate local 0 (object) */
	local_i = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	local_len = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	local_delegates = mono_mb_add_local (mb, &mono_defaults.array_class->byval_arg);
	local_d = mono_mb_add_local (mb, &mono_defaults.multicastdelegate_class->byval_arg);
	local_target = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	if (!void_ret)
		local_res = mono_mb_add_local (mb, &mono_class_from_mono_type (sig->ret)->byval_arg);

	g_assert (sig->hasthis);

	/*
	 * {type: sig->ret} res;
	 * if (delegates == null) {
	 *     return this.<target> ( args .. );
	 * } else {
	 *     int i = 0, len = this.delegates.Length;
	 *     do {
	 *         res = this.delegates [i].Invoke ( args .. );
	 *     } while (++i < len);
	 *     return res;
	 * }
	 */

	/* this wrapper can be used in unmanaged-managed transitions */
	emit_thread_interrupt_checkpoint (mb);

	/* delegates = this.delegates */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoMulticastDelegate, delegates));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_stloc (mb, local_delegates);

	/* if (delegates == null) */
	mono_mb_emit_ldloc (mb, local_delegates);
	pos2 = mono_mb_emit_branch (mb, CEE_BRTRUE);

	/* return target.<target_method|method_ptr> ( args .. ); */

	/* target = d.target; */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, target));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_stloc (mb, local_target);

	/*static methods with bound first arg can have null target and still be bound*/
	if (!static_method_with_first_arg_bound) {
		/* if target != null */
		mono_mb_emit_ldloc (mb, local_target);
		pos0 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* then call this->method_ptr nonstatic */
		if (callvirt) {
			// FIXME:
			mono_mb_emit_exception_full (mb, "System", "NotImplementedException", "");
		} else {
			mono_mb_emit_ldloc (mb, local_target);
			for (i = 0; i < sig->param_count; ++i)
				mono_mb_emit_ldarg (mb, i + 1);
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, extra_arg));
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CALLI_EXTRA_ARG, sig);
			mono_mb_emit_byte (mb, CEE_RET);
		}
	
		/* else [target == null] call this->method_ptr static */
		mono_mb_patch_branch (mb, pos0);
	}

	if (callvirt) {
		if (!closed_over_null) {
			if (target_class->valuetype) {
				mono_mb_emit_ldarg (mb, 1);
				for (i = 1; i < sig->param_count; ++i)
					mono_mb_emit_ldarg (mb, i + 1);
				mono_mb_emit_op (mb, CEE_CALL, target_method);
			} else {
				mono_mb_emit_ldarg (mb, 1);
				mono_mb_emit_op (mb, CEE_CASTCLASS, target_class);
				for (i = 1; i < sig->param_count; ++i)
					mono_mb_emit_ldarg (mb, i + 1);
				mono_mb_emit_op (mb, CEE_CALLVIRT, target_method);
			}
		} else {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			for (i = 0; i < sig->param_count; ++i)
				mono_mb_emit_ldarg (mb, i + 1);
			mono_mb_emit_op (mb, CEE_CALL, target_method);
		}
	} else {
		if (static_method_with_first_arg_bound) {
			mono_mb_emit_ldloc (mb, local_target);
			if (!MONO_TYPE_IS_REFERENCE (invoke_sig->params[0]))
				mono_mb_emit_op (mb, CEE_UNBOX_ANY, mono_class_from_mono_type (invoke_sig->params[0]));
		}
		for (i = 0; i < sig->param_count; ++i)
			mono_mb_emit_ldarg (mb, i + 1);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, extra_arg));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoDelegate, method_ptr));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_CALLI_EXTRA_ARG, invoke_sig);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	/* else [delegates != null] */
	mono_mb_patch_branch (mb, pos2);

	/* len = delegates.Length; */
	mono_mb_emit_ldloc (mb, local_delegates);
	mono_mb_emit_byte (mb, CEE_LDLEN);
	mono_mb_emit_byte (mb, CEE_CONV_I4);
	mono_mb_emit_stloc (mb, local_len);

	/* i = 0; */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, local_i);

	pos1 = mono_mb_get_label (mb);

	/* d = delegates [i]; */
	mono_mb_emit_ldloc (mb, local_delegates);
	mono_mb_emit_ldloc (mb, local_i);
	mono_mb_emit_byte (mb, CEE_LDELEM_REF);
	mono_mb_emit_stloc (mb, local_d);

	/* res = d.Invoke ( args .. ); */
	mono_mb_emit_ldloc (mb, local_d);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	if (!ctx) {
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	} else {
		MonoError error;
		mono_mb_emit_op (mb, CEE_CALLVIRT, mono_class_inflate_generic_method_checked (method, &container->context, &error));
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
	}
	if (!void_ret)
		mono_mb_emit_stloc (mb, local_res);

	/* i += 1 */
	mono_mb_emit_add_to_local (mb, local_i, 1);

	/* i < l */
	mono_mb_emit_ldloc (mb, local_i);
	mono_mb_emit_ldloc (mb, local_len);
	mono_mb_emit_branch_label (mb, CEE_BLT, pos1);

	/* return res */
	if (!void_ret)
		mono_mb_emit_ldloc (mb, local_res);
	mono_mb_emit_byte (mb, CEE_RET);

	mb->skip_visibility = 1;
#endif /* ENABLE_ILGEN */

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

	if (del && !del->target && del->method && mono_method_signature (del->method)->hasthis) {
		callvirt = TRUE;
		target_method = del->method;
	}

	if (del && del->method && mono_method_signature (del->method)->param_count == sig->param_count + 1 && (del->method->flags & METHOD_ATTRIBUTE_STATIC)) {
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

	callsig = mono_metadata_signature_dup_full (method->klass->image, mono_method_signature (method));
	callsig->ret = &mono_defaults.string_class->byval_arg;
	cs = g_new (CtorSigPair, 1);
	cs->sig = callsig;
	cs->ctor_sig = mono_method_signature (method);

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
	MonoMethodSignature *sig = lookup_string_ctor_signature (mono_method_signature (method));
	if (!sig)
		sig = add_string_ctor_signature (method);

	return sig;
}

static MonoType*
get_runtime_invoke_type (MonoType *t, gboolean ret)
{
	if (t->byref) {
		if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t)))
			return t;
		/* Can't share this with 'I' as that needs another indirection */
		return &mono_defaults.int_class->this_arg;
	}

	if (MONO_TYPE_IS_REFERENCE (t))
		return &mono_defaults.object_class->byval_arg;

	if (ret)
		/* The result needs to be boxed */
		return t;

handle_enum:
	switch (t->type) {
		/* Can't share these as the argument needs to be loaded using sign/zero extension */
		/*
	case MONO_TYPE_U1:
		return &mono_defaults.sbyte_class->byval_arg;
	case MONO_TYPE_U2:
		return &mono_defaults.int16_class->byval_arg;
	case MONO_TYPE_U4:
		return &mono_defaults.int32_class->byval_arg;
		*/
	case MONO_TYPE_U8:
		return &mono_defaults.int64_class->byval_arg;
	case MONO_TYPE_BOOLEAN:
		return &mono_defaults.byte_class->byval_arg;
	case MONO_TYPE_CHAR:
		return &mono_defaults.uint16_class->byval_arg;
	case MONO_TYPE_U:
		return &mono_defaults.int_class->byval_arg;
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype) {
			t = mono_class_enum_basetype (t->data.klass);
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

#ifdef ENABLE_ILGEN

/*
 * emit_invoke_call:
 *
 *   Emit the call to the wrapper method from a runtime invoke wrapper.
 */
static void
emit_invoke_call (MonoMethodBuilder *mb, MonoMethod *method,
				  MonoMethodSignature *sig, MonoMethodSignature *callsig,
				  int loc_res,
				  gboolean virtual_, gboolean need_direct_wrapper)
{
	static MonoString *string_dummy = NULL;
	int i;
	int *tmp_nullable_locals;
	gboolean void_ret = FALSE;
	gboolean string_ctor = method && method->string_ctor;

	/* to make it work with our special string constructors */
	if (!string_dummy) {
		MonoError error;
		MONO_GC_REGISTER_ROOT_SINGLE (string_dummy, MONO_ROOT_SOURCE_MARSHAL, NULL, "Marshal Dummy String");
		string_dummy = mono_string_new_checked (mono_get_root_domain (), "dummy", &error);
		mono_error_assert_ok (&error);
	}

	if (virtual_) {
		g_assert (sig->hasthis);
		g_assert (method->flags & METHOD_ATTRIBUTE_VIRTUAL);
	}

	if (sig->hasthis) {
		if (string_ctor) {
			if (mono_gc_is_moving ()) {
				mono_mb_emit_ptr (mb, &string_dummy);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			} else {
				mono_mb_emit_ptr (mb, string_dummy);
			}
		} else {
			mono_mb_emit_ldarg (mb, 0);
		}
	}

	tmp_nullable_locals = g_new0 (int, sig->param_count);

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		int type;

		mono_mb_emit_ldarg (mb, 1);
		if (i) {
			mono_mb_emit_icon (mb, sizeof (gpointer) * i);
			mono_mb_emit_byte (mb, CEE_ADD);
		}

		if (t->byref) {
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			/* A Nullable<T> type don't have a boxed form, it's either null or a boxed T.
			 * So to make this work we unbox it to a local variablee and push a reference to that.
			 */
			if (t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t))) {
				tmp_nullable_locals [i] = mono_mb_add_local (mb, &mono_class_from_mono_type (t)->byval_arg);

				mono_mb_emit_op (mb, CEE_UNBOX_ANY, mono_class_from_mono_type (t));
				mono_mb_emit_stloc (mb, tmp_nullable_locals [i]);
				mono_mb_emit_ldloc_addr (mb, tmp_nullable_locals [i]);
			}
			continue;
		}

		/*FIXME 'this doesn't handle generic enums. Shouldn't we?*/
		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_I1:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->params [i])) {
				mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
				break;
			}

			/* fall through */
		case MONO_TYPE_VALUETYPE:
			if (type == MONO_TYPE_VALUETYPE && t->data.klass->enumtype) {
				type = mono_class_enum_basetype (t->data.klass)->type;
				goto handle_enum;
			}
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			if (mono_class_is_nullable (mono_class_from_mono_type (sig->params [i]))) {
				/* Need to convert a boxed vtype to an mp to a Nullable struct */
				mono_mb_emit_op (mb, CEE_UNBOX, mono_class_from_mono_type (sig->params [i]));
				mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type (sig->params [i]));
			} else {
				mono_mb_emit_op (mb, CEE_LDOBJ, mono_class_from_mono_type (sig->params [i]));
			}
			break;
		default:
			g_assert_not_reached ();
		}
	}
	
	if (virtual_) {
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	} else if (need_direct_wrapper) {
		mono_mb_emit_op (mb, CEE_CALL, method);
	} else {
		mono_mb_emit_ldarg (mb, 3);
		mono_mb_emit_calli (mb, callsig);
	}

	if (sig->ret->byref) {
		/* fixme: */
		g_assert_not_reached ();
	}

	switch (sig->ret->type) {
	case MONO_TYPE_VOID:
		if (!string_ctor)
			void_ret = TRUE;
		break;
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
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
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_TYPEDBYREF:
	case MONO_TYPE_GENERICINST:
		/* box value types */
		mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type (sig->ret));
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
		/* nothing to do */
		break;
	case MONO_TYPE_PTR:
		/* The result is an IntPtr */
		mono_mb_emit_op (mb, CEE_BOX, mono_defaults.int_class);
		break;
	default:
		g_assert_not_reached ();
	}

	if (!void_ret)
		mono_mb_emit_stloc (mb, loc_res);

	/* Convert back nullable-byref arguments */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];

		/* 
		 * Box the result and put it back into the array, the caller will have
		 * to obtain it from there.
		 */
		if (t->byref && t->type == MONO_TYPE_GENERICINST && mono_class_is_nullable (mono_class_from_mono_type (t))) {
			mono_mb_emit_ldarg (mb, 1);			
			mono_mb_emit_icon (mb, sizeof (gpointer) * i);
			mono_mb_emit_byte (mb, CEE_ADD);

			mono_mb_emit_ldloc (mb, tmp_nullable_locals [i]);
			mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type (t));

			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}
	}

	g_free (tmp_nullable_locals);
}

static void
emit_runtime_invoke_body (MonoMethodBuilder *mb, MonoImage *image, MonoMethod *method,
						  MonoMethodSignature *sig, MonoMethodSignature *callsig,
						  gboolean virtual_, gboolean need_direct_wrapper)
{
	gint32 labels [16];
	MonoExceptionClause *clause;
	int loc_res, loc_exc;

	/* The wrapper looks like this:
	 *
	 * <interrupt check>
	 * if (exc) {
	 *	 try {
	 *	   return <call>
	 *	 } catch (Exception e) {
	 *     *exc = e;
	 *   }
	 * } else {
	 *     return <call>
	 * }
	 */

	/* allocate local 0 (object) tmp */
	loc_res = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	/* allocate local 1 (object) exc */
	loc_exc = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* *exc is assumed to be initialized to NULL by the caller */

	mono_mb_emit_byte (mb, CEE_LDARG_2);
	labels [0] = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*
	 * if (exc) case
	 */
	labels [1] = mono_mb_get_label (mb);
	emit_thread_force_interrupt_checkpoint (mb);
	emit_invoke_call (mb, method, sig, callsig, loc_res, virtual_, need_direct_wrapper);

	labels [2] = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* Add a try clause around the call */
	clause = (MonoExceptionClause *)mono_image_alloc0 (image, sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	clause->data.catch_class = mono_defaults.exception_class;
	clause->try_offset = labels [1];
	clause->try_len = mono_mb_get_label (mb) - labels [1];

	clause->handler_offset = mono_mb_get_label (mb);

	/* handler code */
	mono_mb_emit_stloc (mb, loc_exc);	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_ldloc (mb, loc_exc);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	mono_mb_patch_branch (mb, labels [2]);
	mono_mb_emit_ldloc (mb, loc_res);
	mono_mb_emit_byte (mb, CEE_RET);

	/*
	 * if (!exc) case
	 */
	mono_mb_patch_branch (mb, labels [0]);
	emit_thread_force_interrupt_checkpoint (mb);
	emit_invoke_call (mb, method, sig, callsig, loc_res, virtual_, need_direct_wrapper);

	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);
}
#endif

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
	GHashTable *cache = NULL;
	MonoClass *target_klass;
	MonoMethod *res = NULL;
	static MonoMethodSignature *cctor_signature = NULL;
	static MonoMethodSignature *finalize_signature = NULL;
	char *name;
	const char *param_names [16];
	WrapperInfo *info;

	g_assert (method);

	if (!cctor_signature) {
		cctor_signature = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		cctor_signature->ret = &mono_defaults.void_class->byval_arg;
	}
	if (!finalize_signature) {
		finalize_signature = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		finalize_signature->ret = &mono_defaults.void_class->byval_arg;
		finalize_signature->hasthis = 1;
	}

	/* 
	 * Use a separate cache indexed by methods to speed things up and to avoid the
	 * boundless mempool growth caused by the signature_dup stuff below.
	 */
	if (virtual_)
		cache = get_cache (&method->klass->image->runtime_invoke_vcall_cache, mono_aligned_addr_hash, NULL);
	else
		cache = get_cache (&mono_method_get_wrapper_cache (method)->runtime_invoke_direct_cache, mono_aligned_addr_hash, NULL);

	res = mono_marshal_find_in_cache (cache, method);
	if (res)
		return res;
		
	if (method->string_ctor) {
		callsig = lookup_string_ctor_signature (mono_method_signature (method));
		if (!callsig)
			callsig = add_string_ctor_signature (method);
	} else {
		if (method_is_dynamic (method))
			callsig = mono_metadata_signature_dup_full (method->klass->image, mono_method_signature (method));
		else
			callsig = mono_method_signature (method);
	}

	sig = mono_method_signature (method);

	target_klass = get_wrapper_target_class (method->klass->image);

	/* Try to share wrappers for non-corlib methods with simple signatures */
	if (mono_metadata_signature_equal (callsig, cctor_signature)) {
		callsig = cctor_signature;
		target_klass = mono_defaults.object_class;
	} else if (mono_metadata_signature_equal (callsig, finalize_signature)) {
		callsig = finalize_signature;
		target_klass = mono_defaults.object_class;
	}

	if (need_direct_wrapper) {
		/* Already searched at the start */
	} else {
		MonoMethodSignature *tmp_sig;

		callsig = mono_marshal_get_runtime_invoke_sig (callsig);
		GHashTable **cache_table = NULL;

		if (method->klass->valuetype && mono_method_signature (method)->hasthis)
			cache_table = &mono_method_get_wrapper_cache (method)->runtime_invoke_vtype_cache;
		else
			cache_table = &mono_method_get_wrapper_cache (method)->runtime_invoke_cache;

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
		tmp_sig = callsig;
		callsig = mono_metadata_signature_dup_full (target_klass->image, callsig);
		g_free (tmp_sig);
	}
	
	csig = mono_metadata_signature_alloc (target_klass->image, 4);

	csig->ret = &mono_defaults.object_class->byval_arg;
	if (method->klass->valuetype && mono_method_signature (method)->hasthis)
		csig->params [0] = get_runtime_invoke_type (&method->klass->this_arg, FALSE);
	else
		csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &mono_defaults.int_class->byval_arg;
	csig->pinvoke = 1;
#if TARGET_WIN32
	/* This is called from runtime code so it has to be cdecl */
	csig->call_convention = MONO_CALL_C;
#endif

	name = mono_signature_to_name (callsig, virtual_ ? "runtime_invoke_virtual" : (need_direct_wrapper ? "runtime_invoke_direct" : "runtime_invoke"));
	mb = mono_mb_new (target_klass, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

#ifdef ENABLE_ILGEN
	param_names [0] = "this";
	param_names [1] = "params";
	param_names [2] = "exc";
	param_names [3] = "method";
	mono_mb_set_param_names (mb, param_names);

	emit_runtime_invoke_body (mb, target_klass->image, method, sig, callsig, virtual_, need_direct_wrapper);
#endif

	if (need_direct_wrapper) {
#ifdef ENABLE_ILGEN
		mb->skip_visibility = 1;
#endif
		info = mono_wrapper_info_create (mb, virtual_ ? WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL : WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT);
		info->d.runtime_invoke.method = method;
		res = mono_mb_create_and_cache_full (cache, method, mb, csig, sig->param_count + 16, info, NULL);
	} else {
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
				GHashTable *direct_cache;
				res = newm;
				g_hash_table_insert (cache, callsig, res);
				/* Can't insert it into wrapper_hash since the key is a signature */
				direct_cache = mono_method_get_wrapper_cache (method)->runtime_invoke_direct_cache;

				g_hash_table_insert (direct_cache, method, res);
			} else {
				mono_free_method (newm);
			}
			mono_marshal_unlock ();
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

	if (method->klass->rank && (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
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
	MonoExceptionClause *clause;
	MonoMethodBuilder *mb;
	int pos;
	char *name;
	WrapperInfo *info;

	if (method)
		return method;

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);

	csig->ret = &mono_defaults.void_class->byval_arg;
	csig->params [0] = &mono_defaults.int_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &mono_defaults.int_class->byval_arg;

	name = g_strdup ("runtime_invoke_dynamic");
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

#ifdef ENABLE_ILGEN
	/* allocate local 0 (object) tmp */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	/* allocate local 1 (object) exc */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* cond set *exc to null */
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	mono_mb_emit_byte (mb, 3);	
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	emit_thread_force_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_LDARG_0);
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_DYN_CALL);

	pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	clause = (MonoExceptionClause *)mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_FILTER;
	clause->try_len = mono_mb_get_label (mb);

	/* filter code */
	clause->data.filter_offset = mono_mb_get_label (mb);
	
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_CGT_UN);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_ENDFILTER);

	clause->handler_offset = mono_mb_get_label (mb);

	/* handler code */
	/* store exception */
	mono_mb_emit_stloc (mb, 1);
	
	mono_mb_emit_byte (mb, CEE_LDARG_1);
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_stloc (mb, 0);

	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	/* return result */
	mono_mb_patch_branch (mb, pos);
	//mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);
#endif /* ENABLE_ILGEN */

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

	csig = mono_metadata_signature_alloc (image, 4);
	csig->ret = &mono_defaults.object_class->byval_arg;
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &mono_defaults.int_class->byval_arg;
	csig->pinvoke = 1;
#if TARGET_WIN32
	/* This is called from runtime code so it has to be cdecl */
	csig->call_convention = MONO_CALL_C;
#endif

	name = mono_signature_to_name (callsig, "runtime_invoke_sig");
	mb = mono_mb_new (mono_defaults.object_class, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

#ifdef ENABLE_ILGEN
	param_names [0] = "this";
	param_names [1] = "params";
	param_names [2] = "exc";
	param_names [3] = "method";
	mono_mb_set_param_names (mb, param_names);

	emit_runtime_invoke_body (mb, image, NULL, sig, callsig, FALSE, FALSE);
#endif

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

#ifdef ENABLE_ILGEN
static void
mono_mb_emit_auto_layout_exception (MonoMethodBuilder *mb, MonoClass *klass)
{
	char *msg = g_strdup_printf ("The type `%s.%s' layout needs to be Sequential or Explicit",
				     klass->name_space, klass->name);

	mono_mb_emit_exception_marshal_directive (mb, msg);
}
#endif

/**
 * mono_marshal_get_icall_wrapper:
 * Generates IL code for the icall wrapper. The generated method
 * calls the unmanaged code in \p func.
 */
MonoMethod *
mono_marshal_get_icall_wrapper (MonoMethodSignature *sig, const char *name, gconstpointer func, gboolean check_exceptions)
{
	MonoMethodSignature *csig, *csig2;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i;
	WrapperInfo *info;
	
	GHashTable *cache = get_cache (&mono_defaults.object_class->image->icall_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, (gpointer) func)))
		return res;

	g_assert (sig->pinvoke);

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	/* Add an explicit this argument */
	if (sig->hasthis)
		csig2 = mono_metadata_signature_dup_add_this (mono_defaults.corlib, sig, mono_defaults.object_class);
	else
		csig2 = mono_metadata_signature_dup_full (mono_defaults.corlib, sig);

#ifdef ENABLE_ILGEN
	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + sig->hasthis);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_JIT_ICALL_ADDR, (gpointer)func);
	mono_mb_emit_calli (mb, csig2);
	if (check_exceptions)
		emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	csig = mono_metadata_signature_dup_full (mono_defaults.corlib, sig);
	csig->pinvoke = 0;
	if (csig->call_convention == MONO_CALL_VARARG)
		csig->call_convention = 0;

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_ICALL_WRAPPER);
	info->d.icall.func = (gpointer)func;
	res = mono_mb_create_and_cache_full (cache, (gpointer) func, mb, csig, csig->param_count + 16, info, NULL);
	mono_mb_free (mb);

	return res;
}

static int
emit_marshal_custom (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
#ifndef ENABLE_ILGEN
	if (action == MARSHAL_ACTION_CONV_IN && t->type == MONO_TYPE_VALUETYPE)
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
	return conv_arg;
#else
	MonoError error;
	MonoType *mtype;
	MonoClass *mklass;
	static MonoClass *ICustomMarshaler = NULL;
	static MonoMethod *cleanup_native, *cleanup_managed;
	static MonoMethod *marshal_managed_to_native, *marshal_native_to_managed;
	MonoMethod *get_instance = NULL;
	MonoMethodBuilder *mb = m->mb;
	char *exception_msg = NULL;
	guint32 loc1;
	int pos2;

	if (!ICustomMarshaler) {
		MonoClass *klass = mono_class_try_get_icustom_marshaler_class ();
		if (!klass) {
			exception_msg = g_strdup ("Current profile doesn't support ICustomMarshaler");
			goto handle_exception;
		}

		cleanup_native = mono_class_get_method_from_name (klass, "CleanUpNativeData", 1);
		g_assert (cleanup_native);
		cleanup_managed = mono_class_get_method_from_name (klass, "CleanUpManagedData", 1);
		g_assert (cleanup_managed);
		marshal_managed_to_native = mono_class_get_method_from_name (klass, "MarshalManagedToNative", 1);
		g_assert (marshal_managed_to_native);
		marshal_native_to_managed = mono_class_get_method_from_name (klass, "MarshalNativeToManaged", 1);
		g_assert (marshal_native_to_managed);

		mono_memory_barrier ();
		ICustomMarshaler = klass;
	}

	if (spec->data.custom_data.image)
		mtype = mono_reflection_type_from_name_checked (spec->data.custom_data.custom_name, spec->data.custom_data.image, &error);
	else
		mtype = mono_reflection_type_from_name_checked (spec->data.custom_data.custom_name, m->image, &error);
	g_assert (mtype != NULL);
	mono_error_assert_ok (&error);
	mklass = mono_class_from_mono_type (mtype);
	g_assert (mklass != NULL);

	if (!mono_class_is_assignable_from (ICustomMarshaler, mklass))
		exception_msg = g_strdup_printf ("Custom marshaler '%s' does not implement the ICustomMarshaler interface.", mklass->name);

	get_instance = mono_class_get_method_from_name_flags (mklass, "GetInstance", 1, METHOD_ATTRIBUTE_STATIC);
	if (get_instance) {
		MonoMethodSignature *get_sig = mono_method_signature (get_instance);
		if ((get_sig->ret->type != MONO_TYPE_CLASS) ||
			(mono_class_from_mono_type (get_sig->ret) != ICustomMarshaler) ||
			(get_sig->params [0]->type != MONO_TYPE_STRING))
			get_instance = NULL;
	}

	if (!get_instance)
		exception_msg = g_strdup_printf ("Custom marshaler '%s' does not implement a static GetInstance method that takes a single string parameter and returns an ICustomMarshaler.", mklass->name);

handle_exception:
	/* Throw exception and emit compensation code if neccesary */
	if (exception_msg) {
		switch (action) {
		case MARSHAL_ACTION_CONV_IN:
		case MARSHAL_ACTION_CONV_RESULT:
		case MARSHAL_ACTION_MANAGED_CONV_RESULT:
			if ((action == MARSHAL_ACTION_CONV_RESULT) || (action == MARSHAL_ACTION_MANAGED_CONV_RESULT))
				mono_mb_emit_byte (mb, CEE_POP);

			mono_mb_emit_exception_full (mb, "System", "ApplicationException", exception_msg);

			break;
		case MARSHAL_ACTION_PUSH:
			mono_mb_emit_byte (mb, CEE_LDNULL);
			break;
		default:
			break;
		}
		return 0;
	}

	/* FIXME: MS.NET seems to create one instance for each klass + cookie pair */
	/* FIXME: MS.NET throws an exception if GetInstance returns null */

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_VALUETYPE:
			break;

		default:
			g_warning ("custom marshalling of type %x is currently not supported", t->type);
			g_assert_not_reached ();
			break;
		}

		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT))
			break;

		/* Minic MS.NET behavior */
		if (!t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT) && !(t->attrs & PARAM_ATTRIBUTE_IN))
			break;

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_op (mb, CEE_CALL, get_instance);
				
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_REF);

		if (t->type == MONO_TYPE_VALUETYPE) {
			/*
			 * Since we can't determine the type of the argument, we
			 * will assume the unmanaged function takes a pointer.
			 */
			*conv_arg_type = &mono_defaults.int_class->byval_arg;

			mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type (t));
		}

		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_CONV_OUT:
		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (t->byref) {
			mono_mb_emit_ldarg (mb, argnum);

			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

			mono_mb_emit_op (mb, CEE_CALL, get_instance);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (t->attrs & PARAM_ATTRIBUTE_OUT) {
			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

			mono_mb_emit_op (mb, CEE_CALL, get_instance);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);

			/* We have nowhere to store the result */
			mono_mb_emit_byte (mb, CEE_POP);
		}

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_op (mb, CEE_CALL, get_instance);

		mono_mb_emit_ldloc (mb, conv_arg);

		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_native);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		loc1 = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_stloc (mb, loc1);

		/* Check for null */
		mono_mb_emit_ldloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_op (mb, CEE_CALL, get_instance);
		mono_mb_emit_byte (mb, CEE_DUP);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, loc1);
		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_native);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		if (t->byref && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));
		mono_mb_emit_op (mb, CEE_CALL, get_instance);
				
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);
				
		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_native_to_managed);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		g_assert (!t->byref);

		loc1 = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			
		mono_mb_emit_stloc (mb, 3);
			
		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_stloc (mb, loc1);

		/* Check for null */
		mono_mb_emit_ldloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));
		mono_mb_emit_op (mb, CEE_CALL, get_instance);
		mono_mb_emit_byte (mb, CEE_DUP);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, loc1);
		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:

		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (t->byref) {
			mono_mb_emit_ldarg (mb, argnum);

			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

			mono_mb_emit_op (mb, CEE_CALL, get_instance);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_op (mb, CEE_CALLVIRT, marshal_managed_to_native);
			mono_mb_emit_byte (mb, CEE_STIND_I);
		}

		/* Call CleanUpManagedData */
		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_op (mb, CEE_CALL, get_instance);
				
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_op (mb, CEE_CALLVIRT, cleanup_managed);

		mono_mb_patch_branch (mb, pos2);
		break;

	default:
		g_assert_not_reached ();
	}
	return conv_arg;
#endif

}

static int
emit_marshal_asany (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
#ifdef ENABLE_ILGEN
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, NULL);

		g_assert (t->type == MONO_TYPE_OBJECT);
		g_assert (!t->byref);

		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_icon (mb, encoding);
		mono_mb_emit_icon (mb, t->attrs);
		mono_mb_emit_icall (mb, mono_marshal_asany);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, NULL);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_icon (mb, encoding);
		mono_mb_emit_icon (mb, t->attrs);
		mono_mb_emit_icall (mb, mono_marshal_free_asany);
		break;
	}

	default:
		g_assert_not_reached ();
	}
#endif
	return conv_arg;
}

static int
emit_marshal_vtype (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
#ifdef ENABLE_ILGEN
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass, *date_time_class;
	int pos = 0, pos2;

	klass = mono_class_from_mono_type (t);

	date_time_class = mono_class_get_date_time_class ();

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		if (klass == date_time_class) {
			/* Convert it to an OLE DATE type */
			static MonoMethod *to_oadate;

			if (!to_oadate)
				to_oadate = mono_class_get_method_from_name (date_time_class, "ToOADate", 0);
			g_assert (to_oadate);

			conv_arg = mono_mb_add_local (mb, &mono_defaults.double_class->byval_arg);

			if (t->byref) {
				mono_mb_emit_ldarg (mb, argnum);
				pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
			}

			if (!(t->byref && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
				if (!t->byref)
					m->csig->params [argnum - m->csig->hasthis] = &mono_defaults.double_class->byval_arg;

				mono_mb_emit_ldarg_addr (mb, argnum);
				mono_mb_emit_managed_call (mb, to_oadate, NULL);
				mono_mb_emit_stloc (mb, conv_arg);
			}

			if (t->byref)
				mono_mb_patch_branch (mb, pos);
			break;
		}

		if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype)
			break;

		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			
		/* store the address of the source into local variable 0 */
		if (t->byref)
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);
		
		mono_mb_emit_stloc (mb, 0);
			
		/* allocate space for the native struct and
		 * store the address into local variable 1 (dest) */
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LOCALLOC);
		mono_mb_emit_stloc (mb, conv_arg);

		if (t->byref) {
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
		}

		if (!(t->byref && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
		}

		if (t->byref)
			mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_PUSH:
		if (spec && spec->native == MONO_NATIVE_LPSTRUCT) {
			/* FIXME: */
			g_assert (!t->byref);

			/* Have to change the signature since the vtype is passed byref */
			m->csig->params [argnum - m->csig->hasthis] = &mono_defaults.int_class->byval_arg;

			if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype)
				mono_mb_emit_ldarg_addr (mb, argnum);
			else
				mono_mb_emit_ldloc (mb, conv_arg);
			break;
		}

		if (klass == date_time_class) {
			if (t->byref)
				mono_mb_emit_ldloc_addr (mb, conv_arg);
			else
				mono_mb_emit_ldloc (mb, conv_arg);
			break;
		}

		if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype) {
			mono_mb_emit_ldarg (mb, argnum);
			break;
		}			
		mono_mb_emit_ldloc (mb, conv_arg);
		if (!t->byref) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_LDNATIVEOBJ, klass);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (klass == date_time_class) {
			/* Convert from an OLE DATE type */
			static MonoMethod *from_oadate;

			if (!t->byref)
				break;

			if (!((t->attrs & PARAM_ATTRIBUTE_IN) && !(t->attrs & PARAM_ATTRIBUTE_OUT))) {
				if (!from_oadate)
					from_oadate = mono_class_get_method_from_name (date_time_class, "FromOADate", 1);
				g_assert (from_oadate);

				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_managed_call (mb, from_oadate, NULL);
				mono_mb_emit_op (mb, CEE_STOBJ, date_time_class);
			}
			break;
		}

		if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype)
			break;

		if (t->byref) {
			/* dst = argument */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_stloc (mb, 1);

			mono_mb_emit_ldloc (mb, 1);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			if (!((t->attrs & PARAM_ATTRIBUTE_IN) && !(t->attrs & PARAM_ATTRIBUTE_OUT))) {
				/* src = tmp_locals [i] */
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_stloc (mb, 0);

				/* emit valuetype conversion code */
				emit_struct_conv (mb, klass, TRUE);
			}
		}

		emit_struct_free (mb, klass, conv_arg);
		
		if (t->byref)
			mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		if (mono_class_is_explicit_layout (klass) || klass->blittable) {
			mono_mb_emit_stloc (mb, 3);
			break;
		}

		/* load pointer to returned value type */
		g_assert (m->vtaddr_var);
		mono_mb_emit_ldloc (mb, m->vtaddr_var);
		/* store the address of the source into local variable 0 */
		mono_mb_emit_stloc (mb, 0);
		/* set dst_ptr */
		mono_mb_emit_ldloc_addr (mb, 3);
		mono_mb_emit_stloc (mb, 1);
				
		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype) {
			conv_arg = 0;
			break;
		}

		conv_arg = mono_mb_add_local (mb, &klass->byval_arg);

		if (t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		if (t->byref) 
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);
		mono_mb_emit_stloc (mb, 0);

		if (t->byref) {
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
		}			

		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);

		if (t->byref)
			mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype)
			break;
		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_IN) && !(t->attrs & PARAM_ATTRIBUTE_OUT))
			break;

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Set src */
		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_stloc (mb, 0);

		/* Set dest */
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, FALSE);

		mono_mb_patch_branch (mb, pos2);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype) {
			mono_mb_emit_stloc (mb, 3);
			m->retobj_var = 0;
			break;
		}
			
		/* load pointer to returned value type */
		g_assert (m->vtaddr_var);
		mono_mb_emit_ldloc (mb, m->vtaddr_var);
			
		/* store the address of the source into local variable 0 */
		mono_mb_emit_stloc (mb, 0);
		/* allocate space for the native struct and
		 * store the address into dst_ptr */
		m->retobj_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		m->retobj_class = klass;
		g_assert (m->retobj_var);
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, m->retobj_var);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, FALSE);
		break;

	default:
		g_assert_not_reached ();
	}
#endif
	return conv_arg;
}

static int
emit_marshal_string (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
#ifndef ENABLE_ILGEN
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		break;
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		break;
	}
#else
	MonoMethodBuilder *mb = m->mb;
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
	MonoMarshalConv conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
	gboolean need_free;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		if (t->byref) {
			if (t->attrs & PARAM_ATTRIBUTE_OUT)
				break;

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);				
		} else {
			mono_mb_emit_ldarg (mb, argnum);
		}

		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
		} else {
			mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));

			mono_mb_emit_stloc (mb, conv_arg);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		if (encoding == MONO_NATIVE_VBBYREFSTR) {
			static MonoMethod *m;

			if (!m) {
				m = mono_class_get_method_from_name_flags (mono_defaults.string_class, "get_Length", -1, 0);
				g_assert (m);
			}

			if (!t->byref) {
				char *msg = g_strdup_printf ("VBByRefStr marshalling requires a ref parameter.", encoding);
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			/* 
			 * Have to allocate a new string with the same length as the original, and
			 * copy the contents of the buffer pointed to by CONV_ARG into it.
			 */
			g_assert (t->byref);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);				
			mono_mb_emit_managed_call (mb, m, NULL);
			mono_mb_emit_icall (mb, mono_string_new_len_wrapper);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			int stind_op;
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, stind_op);
			need_free = TRUE;
		}

		if (need_free) {
			mono_mb_emit_ldloc (mb, conv_arg);
			if (conv == MONO_MARSHAL_CONV_BSTR_STR)
				mono_mb_emit_icall (mb, mono_free_bstr);
			else
				mono_mb_emit_icall (mb, mono_marshal_free);
		}
		break;

	case MARSHAL_ACTION_PUSH:
		if (t->byref && encoding != MONO_NATIVE_VBBYREFSTR)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		mono_mb_emit_stloc (mb, 0);
				
		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, 3);

		/* free the string */
		mono_mb_emit_ldloc (mb, 0);
		if (conv == MONO_MARSHAL_CONV_BSTR_STR)
			mono_mb_emit_icall (mb, mono_free_bstr);
		else
			mono_mb_emit_icall (mb, mono_marshal_free);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		*conv_arg_type = &mono_defaults.int_class->byval_arg;

		if (t->byref) {
			if (t->attrs & PARAM_ATTRIBUTE_OUT)
				break;
		}

		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == MONO_MARSHAL_CONV_INVALID) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (t->byref) {
			if (conv_arg) {
				int stind_op;
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
				mono_mb_emit_byte (mb, stind_op);
			}
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (conv_to_icall (conv, NULL) == mono_marshal_string_to_utf16)
			/* We need to make a copy so the caller is able to free it */
			mono_mb_emit_icall (mb, mono_marshal_string_to_utf16_copy);
		else
			mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		g_assert_not_reached ();
	}
#endif
	return conv_arg;
}


static int
emit_marshal_safehandle (EmitMarshalContext *m, int argnum, MonoType *t, 
			 MonoMarshalSpec *spec, int conv_arg, 
			 MonoType **conv_arg_type, MarshalAction action)
{
#ifndef ENABLE_ILGEN
	if (action == MARSHAL_ACTION_CONV_IN)
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
#else
	MonoMethodBuilder *mb = m->mb;

	switch (action){
	case MARSHAL_ACTION_CONV_IN: {
		MonoType *intptr_type;
		int dar_release_slot, pos;

		intptr_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, intptr_type);
		*conv_arg_type = intptr_type;

		if (!sh_dangerous_add_ref)
			init_safe_handle ();

		mono_mb_emit_ldarg (mb, argnum);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception (mb, "ArgumentNullException", NULL);
		
		mono_mb_patch_branch (mb, pos);
		if (t->byref){
			/*
			 * My tests in show that ref SafeHandles are not really
			 * passed as ref objects.  Instead a NULL is passed as the
			 * value of the ref
			 */
			mono_mb_emit_icon (mb, 0);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		} 

		/* Create local to hold the ref parameter to DangerousAddRef */
		dar_release_slot = mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

		/* set release = false; */
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_stloc (mb, dar_release_slot);

		/* safehandle.DangerousAddRef (ref release) */
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc_addr (mb, dar_release_slot);
		mono_mb_emit_managed_call (mb, sh_dangerous_add_ref, NULL);

		/* Pull the handle field from SafeHandle */
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, conv_arg);

		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else 
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		/* The slot for the boolean is the next temporary created after conv_arg, see the CONV_IN code */
		int dar_release_slot = conv_arg + 1;
		int label_next;

		if (!sh_dangerous_release)
			init_safe_handle ();

		if (t->byref){
			MonoMethod *ctor;
			
			/*
			 * My tests indicate that ref SafeHandles parameters are not actually
			 * passed by ref, but instead a new Handle is created regardless of
			 * whether a change happens in the unmanaged side.
			 *
			 * Also, the Handle is created before calling into unmanaged code,
			 * but we do not support that mechanism (getting to the original
			 * handle) and it makes no difference where we create this
			 */
			ctor = mono_class_get_method_from_name (t->data.klass, ".ctor", 0);
			if (ctor == NULL){
				mono_mb_emit_exception (mb, "MissingMethodException", "paramterless constructor required");
				break;
			}
			/* refval = new SafeHandleDerived ()*/
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
			mono_mb_emit_byte (mb, CEE_STIND_REF);

			/* refval.handle = returned_handle */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_STIND_I);
		} else {
			mono_mb_emit_ldloc (mb, dar_release_slot);
			label_next = mono_mb_emit_branch (mb, CEE_BRFALSE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_managed_call (mb, sh_dangerous_release, NULL);
			mono_mb_patch_branch (mb, label_next);
		}
		break;
	}
		
	case MARSHAL_ACTION_CONV_RESULT: {
		MonoMethod *ctor = NULL;
		int intptr_handle_slot;
		
		if (mono_class_is_abstract (t->data.klass)) {
			mono_mb_emit_byte (mb, CEE_POP);
			mono_mb_emit_exception_marshal_directive (mb, g_strdup ("Returned SafeHandles should not be abstract"));
			break;
		}

		ctor = mono_class_get_method_from_name (t->data.klass, ".ctor", 0);
		if (ctor == NULL){
			mono_mb_emit_byte (mb, CEE_POP);
			mono_mb_emit_exception (mb, "MissingMethodException", "paramterless constructor required");
			break;
		}
		/* Store the IntPtr results into a local */
		intptr_handle_slot = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_stloc (mb, intptr_handle_slot);

		/* Create return value */
		mono_mb_emit_op (mb, CEE_NEWOBJ, ctor);
		mono_mb_emit_stloc (mb, 3);

		/* Set the return.handle to the value, am using ldflda, not sure if thats a good idea */
		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoSafeHandle, handle));
		mono_mb_emit_ldloc (mb, intptr_handle_slot);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}
		
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_IN\n");
		break;
		
	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_OUT\n");
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_RESULT\n");
		break;
	default:
		printf ("Unhandled case for MarshalAction: %d\n", action);
	}
#endif
	return conv_arg;
}


static int
emit_marshal_handleref (EmitMarshalContext *m, int argnum, MonoType *t, 
			MonoMarshalSpec *spec, int conv_arg, 
			MonoType **conv_arg_type, MarshalAction action)
{
#ifndef ENABLE_ILGEN
	if (action == MARSHAL_ACTION_CONV_IN)
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
#else
	MonoMethodBuilder *mb = m->mb;

	switch (action){
	case MARSHAL_ACTION_CONV_IN: {
		MonoType *intptr_type;

		intptr_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, intptr_type);
		*conv_arg_type = intptr_type;

		if (t->byref){
			char *msg = g_strdup ("HandleRefs can not be returned from unmanaged code (or passed by ref)");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		} 
		mono_mb_emit_ldarg_addr (mb, argnum);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoHandleRef, handle));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		/* no resource release required */
		break;
	}
		
	case MARSHAL_ACTION_CONV_RESULT: {
		char *msg = g_strdup ("HandleRefs can not be returned from unmanaged code (or passed by ref)");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}
		
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_IN\n");
		break;
		
	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_OUT\n");
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		fprintf (stderr, "mono/marshal: SafeHandles missing MANAGED_CONV_RESULT\n");
		break;
	default:
		fprintf (stderr, "Unhandled case for MarshalAction: %d\n", action);
	}
#endif
	return conv_arg;
}


static int
emit_marshal_object (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec, 
		     int conv_arg, MonoType **conv_arg_type, 
		     MarshalAction action)
{
#ifndef ENABLE_ILGEN
	if (action == MARSHAL_ACTION_CONV_IN)
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
#else
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = mono_class_from_mono_type (t);
	int pos, pos2, loc;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		m->orig_conv_args [argnum] = 0;

		if (mono_class_from_mono_type (t) == mono_defaults.object_class) {
			char *msg = g_strdup_printf ("Marshalling of type object is not implemented");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		if (klass->delegate) {
			if (t->byref) {
				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					char *msg = g_strdup_printf ("Byref marshalling of delegates is not implemented.");
					mono_mb_emit_exception_marshal_directive (mb, msg);
				}
				mono_mb_emit_byte (mb, CEE_LDNULL);
				mono_mb_emit_stloc (mb, conv_arg);
			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, NULL));
				mono_mb_emit_stloc (mb, conv_arg);
			}
		} else if (klass == mono_defaults.stringbuilder_class) {
			MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			MonoMarshalConv conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);

#if 0			
			if (t->byref) {
				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					char *msg = g_strdup_printf ("Byref marshalling of stringbuilders is not implemented.");
					mono_mb_emit_exception_marshal_directive (mb, msg);
				}
				break;
			}
#endif

			if (t->byref && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))
				break;

			if (conv == MONO_MARSHAL_CONV_INVALID) {
				char *msg = g_strdup_printf ("stringbuilder marshalling conversion %d not implemented", encoding);
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
		} else if (klass->blittable) {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_emit_ldarg (mb, argnum);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldflda (mb, sizeof (MonoObject));
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_patch_branch (mb, pos);
			break;
		} else {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);

			if (t->byref) {
				/* we dont need any conversions for out parameters */
				if (t->attrs & PARAM_ATTRIBUTE_OUT)
					break;

				mono_mb_emit_ldarg (mb, argnum);				
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			}
				
			/* store the address of the source into local variable 0 */
			mono_mb_emit_stloc (mb, 0);
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

			/* allocate space for the native struct and store the address */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, conv_arg);

			if (t->byref) {
				/* Need to store the original buffer so we can free it later */
				m->orig_conv_args [argnum] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_stloc (mb, m->orig_conv_args [argnum]);
			}

			/* set the src_ptr */
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_ldflda (mb, sizeof (MonoObject));
			mono_mb_emit_stloc (mb, 0);

			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_branch (mb, pos);
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (klass == mono_defaults.stringbuilder_class) {
			gboolean need_free;
			MonoMarshalNative encoding;
			MonoMarshalConv conv;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free);

			g_assert (encoding != -1);

			if (t->byref) {
				//g_assert (!(t->attrs & PARAM_ATTRIBUTE_OUT));

				need_free = TRUE;

				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);

				switch (encoding) {
				case MONO_NATIVE_LPWSTR:
					mono_mb_emit_icall (mb, mono_string_utf16_to_builder2);
					break;
				case MONO_NATIVE_LPSTR:
					mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
					break;
				case MONO_NATIVE_UTF8STR:
					mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
					break;
				default:
					g_assert_not_reached ();
				}

				mono_mb_emit_byte (mb, CEE_STIND_REF);
			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);

				mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
			}

			if (need_free) {
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, mono_marshal_free);
			}
			break;
		}

		if (klass->delegate) {
			if (t->byref) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}
			break;
		}

		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			/* allocate a new object */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}

		/* dst = *argument */
		mono_mb_emit_ldarg (mb, argnum);

		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_stloc (mb, 1);

		mono_mb_emit_ldloc (mb, 1);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (t->byref || (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, 1);
			
			/* src = tmp_locals [i] */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 0);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, TRUE);

			/* Free the structure returned by the native code */
			emit_struct_free (mb, klass, conv_arg);

			if (m->orig_conv_args [argnum]) {
				/* 
				 * If the native function changed the pointer, then free
				 * the original structure plus the new pointer.
				 */
				mono_mb_emit_ldloc (mb, m->orig_conv_args [argnum]);
				mono_mb_emit_ldloc (mb, conv_arg);
				pos2 = mono_mb_emit_branch (mb, CEE_BEQ);

				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					g_assert (m->orig_conv_args [argnum]);

					emit_struct_free (mb, klass, m->orig_conv_args [argnum]);
				}

				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, mono_marshal_free);

				mono_mb_patch_branch (mb, pos2);
			}
		}
		else
			/* Free the original structure passed to native code */
			emit_struct_free (mb, klass, conv_arg);

		mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		if (klass->delegate) {
			g_assert (!t->byref);
			mono_mb_emit_stloc (mb, 0);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
			mono_mb_emit_stloc (mb, 3);
		} else if (klass == mono_defaults.stringbuilder_class) {
			// FIXME:
			char *msg = g_strdup_printf ("Return marshalling of stringbuilders is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
		} else {
			/* set src */
			mono_mb_emit_stloc (mb, 0);
	
			/* Make a copy since emit_conv modifies local 0 */
			loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			mono_mb_emit_ldloc (mb, 0);
			mono_mb_emit_stloc (mb, loc);
	
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, 3);
	
			mono_mb_emit_ldloc (mb, 0);
			pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
	
			/* allocate result object */
	
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);	
			mono_mb_emit_stloc (mb, 3);
					
			/* set dst  */
	
			mono_mb_emit_ldloc (mb, 3);
			mono_mb_emit_ldflda (mb, sizeof (MonoObject));
			mono_mb_emit_stloc (mb, 1);
								
			/* emit conversion code */
			emit_struct_conv (mb, klass, TRUE);
	
			emit_struct_free (mb, klass, loc);
	
			/* Free the pointer allocated by unmanaged code */
			mono_mb_emit_ldloc (mb, loc);
			mono_mb_emit_icall (mb, mono_marshal_free);
			mono_mb_patch_branch (mb, pos);
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		conv_arg = mono_mb_add_local (mb, &klass->byval_arg);

		if (klass->delegate) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		if (klass == mono_defaults.stringbuilder_class) {
			MonoMarshalNative encoding;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);

			// FIXME:
			g_assert (encoding == MONO_NATIVE_LPSTR || encoding == MONO_NATIVE_UTF8STR);

			g_assert (!t->byref);
			g_assert (encoding != -1);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		/* The class can not have an automatic layout */
		if (mono_class_is_auto_layout (klass)) {
			mono_mb_emit_auto_layout_exception (mb, klass);
			break;
		}

		if (t->attrs & PARAM_ATTRIBUTE_OUT) {
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		/* Set src */
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref) {
			int pos2;

			/* Check for NULL and raise an exception */
			pos2 = mono_mb_emit_branch (mb, CEE_BRTRUE);

			mono_mb_emit_exception (mb, "ArgumentNullException", NULL);

			mono_mb_patch_branch (mb, pos2);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		}				

		mono_mb_emit_stloc (mb, 0);

		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_emit_ldloc (mb, 0);
		pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Create and set dst */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_NEWOBJ, klass);	
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_stloc (mb, 1); 

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);

		mono_mb_patch_branch (mb, pos);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (klass->delegate) {
			if (t->byref) {
				int stind_op;
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, &stind_op));
				mono_mb_emit_byte (mb, stind_op);
				break;
			}
		}

		if (t->byref) {
			/* Check for null */
			mono_mb_emit_ldloc (mb, conv_arg);
			pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
			pos2 = mono_mb_emit_branch (mb, CEE_BR);

			mono_mb_patch_branch (mb, pos);			
			
			/* Set src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldflda (mb, sizeof (MonoObject));
			mono_mb_emit_stloc (mb, 0);

			/* Allocate and set dest */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
			mono_mb_emit_stloc (mb, 1);
			
			/* Update argument pointer */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_STIND_I);
		
			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_branch (mb, pos2);
		} else if (klass == mono_defaults.stringbuilder_class) {
			// FIXME: What to do here ?
		} else {
			/* byval [Out] marshalling */

			/* FIXME: Handle null */

			/* Set src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldflda (mb, sizeof (MonoObject));
			mono_mb_emit_stloc (mb, 0);

			/* Set dest */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_stloc (mb, 1);
			
			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
		}			
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (klass->delegate) {
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN, NULL));
			mono_mb_emit_stloc (mb, 3);
			break;
		}

		/* The class can not have an automatic layout */
		if (mono_class_is_auto_layout (klass)) {
			mono_mb_emit_auto_layout_exception (mb, klass);
			break;
		}

		mono_mb_emit_stloc (mb, 0);
		/* Check for null */
		mono_mb_emit_ldloc (mb, 0);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, 3);
		pos2 = mono_mb_emit_branch (mb, CEE_BR);

		mono_mb_patch_branch (mb, pos);

		/* Set src */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_stloc (mb, 0);

		/* Allocate and set dest */
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_stloc (mb, 3);

		emit_struct_conv (mb, klass, FALSE);

		mono_mb_patch_branch (mb, pos2);
		break;

	default:
		g_assert_not_reached ();
	}
#endif
	return conv_arg;
}

#ifdef ENABLE_ILGEN
#ifndef DISABLE_COM

static int
emit_marshal_variant (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec, 
		     int conv_arg, MonoType **conv_arg_type, 
		     MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	static MonoMethod *get_object_for_native_variant = NULL;
	static MonoMethod *get_native_variant_for_object = NULL;

	if (!get_object_for_native_variant)
		get_object_for_native_variant = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1);
	g_assert (get_object_for_native_variant);

	if (!get_native_variant_for_object)
		get_native_variant_for_object = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetNativeVariantForObject", 2);
	g_assert (get_native_variant_for_object);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		conv_arg = mono_mb_add_local (mb, &mono_class_get_variant_class ()->byval_arg);
		
		if (t->byref)
			*conv_arg_type = &mono_class_get_variant_class ()->this_arg;
		else
			*conv_arg_type = &mono_class_get_variant_class ()->byval_arg;

		if (t->byref && !(t->attrs & PARAM_ATTRIBUTE_IN) && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte(mb, CEE_LDIND_REF);
		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_managed_call (mb, get_native_variant_for_object, NULL);
		break;
	}

	case MARSHAL_ACTION_CONV_OUT: {
		static MonoMethod *variant_clear = NULL;

		if (!variant_clear)
			variant_clear = mono_class_get_method_from_name (mono_class_get_variant_class (), "Clear", 0);
		g_assert (variant_clear);


		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc_addr (mb, conv_arg);
			mono_mb_emit_managed_call (mb, get_object_for_native_variant, NULL);
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}

		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_managed_call (mb, variant_clear, NULL);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT: {
		char *msg = g_strdup ("Marshalling of VARIANT not supported as a return type.");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		if (t->byref)
			*conv_arg_type = &mono_class_get_variant_class ()->this_arg;
		else
			*conv_arg_type = &mono_class_get_variant_class ()->byval_arg;

		if (t->byref && !(t->attrs & PARAM_ATTRIBUTE_IN) && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		if (t->byref)
			mono_mb_emit_ldarg (mb, argnum);
		else
			mono_mb_emit_ldarg_addr (mb, argnum);
		mono_mb_emit_managed_call (mb, get_object_for_native_variant, NULL);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT || !(t->attrs & PARAM_ATTRIBUTE_IN))) {
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_managed_call (mb, get_native_variant_for_object, NULL);
		}
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_RESULT: {
		char *msg = g_strdup ("Marshalling of VARIANT not supported as a return type.");
		mono_mb_emit_exception_marshal_directive (mb, msg);
		break;
	}

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

#endif /* DISABLE_COM */
#endif /* ENABLE_ILGEN */

static gboolean
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


static int
emit_marshal_array (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
#ifndef ENABLE_ILGEN
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.object_class->byval_arg;
		break;
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		break;
	}
#else
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = mono_class_from_mono_type (t);
	gboolean need_convert, need_free;
	MonoMarshalNative encoding;

	encoding = mono_marshal_get_string_encoding (m->piinfo, spec);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.object_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		if (klass->element_class->blittable) {
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_ARRAY_LPARRAY, NULL));
			mono_mb_emit_stloc (mb, conv_arg);
		} else {
			MonoClass *eklass;
			guint32 label1, label2, label3;
			int index_var, src_var, dest_ptr, esize;
			MonoMarshalConv conv;
			gboolean is_string = FALSE;

			dest_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			eklass = klass->element_class;

			if (eklass == mono_defaults.string_class) {
				is_string = TRUE;
				conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
			}
			else if (eklass == mono_defaults.stringbuilder_class) {
				is_string = TRUE;
				conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
			}
			else
				conv = MONO_MARSHAL_CONV_INVALID;

			if (is_string && conv == MONO_MARSHAL_CONV_INVALID) {
				char *msg = g_strdup_printf ("string/stringbuilder marshalling conversion %d not implemented", encoding);
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			src_var = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_stloc (mb, src_var);

			/* Check null */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, src_var);
			label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

			if (is_string)
				esize = sizeof (gpointer);
			else if (eklass == mono_defaults.char_class) /*can't call mono_marshal_type_size since it causes all sorts of asserts*/
				esize = mono_pinvoke_is_unicode (m->piinfo) ? 2 : 1;
			else
				esize = mono_class_native_size (eklass, NULL);

			/* allocate space for the native struct and store the address */
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);

			if (eklass == mono_defaults.string_class) {
				/* Make the array bigger for the terminating null */
				mono_mb_emit_byte (mb, CEE_LDC_I4_1);
				mono_mb_emit_byte (mb, CEE_ADD);
			}
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, conv_arg);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, dest_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);				
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* Emit marshalling code */

			if (is_string) {
				int stind_op;
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);
				mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
				mono_mb_emit_byte (mb, stind_op);
			} else {
				/* set the src_ptr */
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
				mono_mb_emit_stloc (mb, 0);

				/* set dst_ptr */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_stloc (mb, 1);

				/* emit valuetype conversion code */
				emit_struct_conv_full (mb, eklass, FALSE, 0, eklass == mono_defaults.char_class ? encoding : (MonoMarshalNative)-1);
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, dest_ptr, esize);
			
			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label3);

			if (eklass == mono_defaults.string_class) {
				/* Null terminate */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_byte (mb, CEE_LDC_I4_0);
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}

			mono_mb_patch_branch (mb, label1);
		}

		break;

	case MARSHAL_ACTION_CONV_OUT:
		/* Unicode character arrays are implicitly marshalled as [Out] under MS.NET */
		need_convert = ((klass->element_class == mono_defaults.char_class) && (encoding == MONO_NATIVE_LPWSTR)) || (klass->element_class == mono_defaults.stringbuilder_class) || (t->attrs & PARAM_ATTRIBUTE_OUT);
		need_free = mono_marshal_need_free (&klass->element_class->byval_arg, 
											m->piinfo, spec);

		if ((t->attrs & PARAM_ATTRIBUTE_OUT) && spec && spec->native == MONO_NATIVE_LPARRAY && spec->data.array_data.param_num != -1) {
			int param_num = spec->data.array_data.param_num;
			MonoType *param_type;

			param_type = m->sig->params [param_num];

			if (param_type->byref && param_type->type != MONO_TYPE_I4) {
				char *msg = g_strdup ("Not implemented.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				break;
			}

			if (t->byref ) {
				mono_mb_emit_ldarg (mb, argnum);

				/* Create the managed array */
				mono_mb_emit_ldarg (mb, param_num);
				if (m->sig->params [param_num]->byref)
					// FIXME: Support other types
					mono_mb_emit_byte (mb, CEE_LDIND_I4);
				mono_mb_emit_byte (mb, CEE_CONV_OVF_I);
				mono_mb_emit_op (mb, CEE_NEWARR, klass->element_class);
				/* Store into argument */
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}
		}

		if (need_convert || need_free) {
			/* FIXME: Optimize blittable case */
			MonoClass *eklass;
			guint32 label1, label2, label3;
			int index_var, src_ptr, loc, esize;

			eklass = klass->element_class;
			if ((eklass == mono_defaults.stringbuilder_class) || (eklass == mono_defaults.string_class))
				esize = sizeof (gpointer);
			else if (eklass == mono_defaults.char_class)
				esize = mono_pinvoke_is_unicode (m->piinfo) ? 2 : 1;
			else
				esize = mono_class_native_size (eklass, NULL);
			src_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			/* Check null */
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, src_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);				
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mono_mb_get_label (mb);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			label3 = mono_mb_emit_branch (mb, CEE_BGE);

			/* Emit marshalling code */

			if (eklass == mono_defaults.stringbuilder_class) {
				gboolean need_free2;
				MonoMarshalConv conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free2);

				g_assert (conv != MONO_MARSHAL_CONV_INVALID);

				/* dest */
				mono_mb_emit_ldarg (mb, argnum);
				if (t->byref)
					mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);

				/* src */
				mono_mb_emit_ldloc (mb, src_ptr);
				mono_mb_emit_byte (mb, CEE_LDIND_I);

				mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));

				if (need_free) {
					/* src */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_LDIND_I);

					mono_mb_emit_icall (mb, mono_marshal_free);
				}
			}
			else if (eklass == mono_defaults.string_class) {
				if (need_free) {
					/* src */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_byte (mb, CEE_LDIND_I);

					mono_mb_emit_icall (mb, mono_marshal_free);
				}
			}
			else {
				if (need_convert) {
					/* set the src_ptr */
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_stloc (mb, 0);

					/* set dst_ptr */
					mono_mb_emit_ldarg (mb, argnum);
					if (t->byref)
						mono_mb_emit_byte (mb, CEE_LDIND_REF);
					mono_mb_emit_ldloc (mb, index_var);
					mono_mb_emit_op (mb, CEE_LDELEMA, eklass);
					mono_mb_emit_stloc (mb, 1);

					/* emit valuetype conversion code */
					emit_struct_conv_full (mb, eklass, TRUE, 0, eklass == mono_defaults.char_class ? encoding : (MonoMarshalNative)-1);
				}

				if (need_free) {
					mono_mb_emit_ldloc (mb, src_ptr);
					mono_mb_emit_stloc (mb, loc);
					mono_mb_emit_ldloc (mb, loc);

					emit_struct_free (mb, eklass, loc);
				}
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, src_ptr, esize);

			mono_mb_emit_branch_label (mb, CEE_BR, label2);

			mono_mb_patch_branch (mb, label1);
			mono_mb_patch_branch (mb, label3);
		}
		
		if (klass->element_class->blittable) {
			/* free memory allocated (if any) by MONO_MARSHAL_CONV_ARRAY_LPARRAY */

			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_FREE_LPARRAY, NULL));
		}

		break;

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* fixme: we need conversions here */
		mono_mb_emit_stloc (mb, 3);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		MonoClass *eklass;
		guint32 label1, label2, label3;
		int index_var, src_ptr, esize, param_num, num_elem;
		MonoMarshalConv conv;
		gboolean is_string = FALSE;
		
		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
		*conv_arg_type = &mono_defaults.int_class->byval_arg;

		if (t->byref) {
			char *msg = g_strdup ("Byref array marshalling to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}
		if (!spec) {
			char *msg = g_strdup ("[MarshalAs] attribute required to marshal arrays to managed code.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}			
		if (spec->native != MONO_NATIVE_LPARRAY) {
			char *msg = g_strdup ("Non LPArray marshalling of arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;			
		}

		/* FIXME: t is from the method which is wrapped, not the delegate type */
		/* g_assert (t->attrs & PARAM_ATTRIBUTE_IN); */

		param_num = spec->data.array_data.param_num;
		num_elem = spec->data.array_data.num_elem;
		if (spec->data.array_data.elem_mult == 0)
			/* param_num is not specified */
			param_num = -1;

		if (param_num == -1) {
			if (num_elem <= 0) {
				char *msg = g_strdup ("Either SizeConst or SizeParamIndex should be specified when marshalling arrays to managed code.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
		}

		/* FIXME: Optimize blittable case */

		eklass = klass->element_class;
		if (eklass == mono_defaults.string_class) {
			is_string = TRUE;
			conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		}
		else if (eklass == mono_defaults.stringbuilder_class) {
			is_string = TRUE;
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free);
		}
		else
			conv = MONO_MARSHAL_CONV_INVALID;

		mono_marshal_load_type_info (eklass);

		if (is_string)
			esize = sizeof (gpointer);
		else
			esize = mono_class_native_size (eklass, NULL);
		src_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		/* Check param index */
		if (param_num != -1) {
			if (param_num >= m->sig->param_count) {
				char *msg = g_strdup ("Array size control parameter index is out of range.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
			switch (m->sig->params [param_num]->type) {
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
			case MONO_TYPE_I:
			case MONO_TYPE_U:
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				break;
			default: {
				char *msg = g_strdup ("Array size control parameter must be an integral type.");
				mono_mb_emit_exception_marshal_directive (mb, msg);
				return conv_arg;
			}
			}
		}

		/* Check null */
		mono_mb_emit_ldarg (mb, argnum);
		label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, src_ptr);

		/* Create managed array */
		/* 
		 * The LPArray marshalling spec says that sometimes param_num starts 
		 * from 1, sometimes it starts from 0. But MS seems to allways start
		 * from 0.
		 */

		if (param_num == -1) {
			mono_mb_emit_icon (mb, num_elem);
		} else {
			mono_mb_emit_ldarg (mb, param_num);
			if (num_elem > 0) {
				mono_mb_emit_icon (mb, num_elem);
				mono_mb_emit_byte (mb, CEE_ADD);
			}
			mono_mb_emit_byte (mb, CEE_CONV_OVF_I);
		}

		mono_mb_emit_op (mb, CEE_NEWARR, eklass);
		mono_mb_emit_stloc (mb, conv_arg);

		if (eklass->blittable) {
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);
			mono_mb_patch_branch (mb, label1);
			break;
		}

		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* Emit marshalling code */
		if (is_string) {
			g_assert (conv != MONO_MARSHAL_CONV_INVALID);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_ldloc (mb, src_ptr);
			mono_mb_emit_byte (mb, CEE_LDIND_I);

			mono_mb_emit_icall (mb, conv_to_icall (conv, NULL));
			mono_mb_emit_byte (mb, CEE_STELEM_REF);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string and non-blittable arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, index_var, 1);
		mono_mb_emit_add_to_local (mb, src_ptr, esize);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label1);
		mono_mb_patch_branch (mb, label3);
		
		break;
	}
	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		MonoClass *eklass;
		guint32 label1, label2, label3;
		int index_var, dest_ptr, esize, param_num, num_elem;
		MonoMarshalConv conv;
		gboolean is_string = FALSE;

		if (!spec)
			/* Already handled in CONV_IN */
			break;
		
		/* These are already checked in CONV_IN */
		g_assert (!t->byref);
		g_assert (spec->native == MONO_NATIVE_LPARRAY);
		g_assert (t->attrs & PARAM_ATTRIBUTE_OUT);

		param_num = spec->data.array_data.param_num;
		num_elem = spec->data.array_data.num_elem;

		if (spec->data.array_data.elem_mult == 0)
			/* param_num is not specified */
			param_num = -1;

		if (param_num == -1) {
			if (num_elem <= 0) {
				g_assert_not_reached ();
			}
		}

		/* FIXME: Optimize blittable case */

		eklass = klass->element_class;
		if (eklass == mono_defaults.string_class) {
			is_string = TRUE;
			conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
		}
		else if (eklass == mono_defaults.stringbuilder_class) {
			is_string = TRUE;
			conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
		}
		else
			conv = MONO_MARSHAL_CONV_INVALID;

		mono_marshal_load_type_info (eklass);

		if (is_string)
			esize = sizeof (gpointer);
		else
			esize = mono_class_native_size (eklass, NULL);

		dest_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		/* Check null */
		mono_mb_emit_ldloc (mb, conv_arg);
		label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, dest_ptr);

		if (eklass->blittable) {
			/* dest */
			mono_mb_emit_ldarg (mb, argnum);
			/* src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_CONV_I);
			mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			/* length */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_icon (mb, esize);
			mono_mb_emit_byte (mb, CEE_MUL);
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_CPBLK);			
			break;
		}

		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* Emit marshalling code */
		if (is_string) {
			int stind_op;
			g_assert (conv != MONO_MARSHAL_CONV_INVALID);

			/* dest */
			mono_mb_emit_ldloc (mb, dest_ptr);

			/* src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_byte (mb, CEE_LDELEM_REF);

			mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, stind_op);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string and non-blittable arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, index_var, 1);
		mono_mb_emit_add_to_local (mb, dest_ptr, esize);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label1);
		mono_mb_patch_branch (mb, label3);

		break;
	}
	case MARSHAL_ACTION_MANAGED_CONV_RESULT: {
		MonoClass *eklass;
		guint32 label1, label2, label3;
		int index_var, src, dest, esize;
		MonoMarshalConv conv = MONO_MARSHAL_CONV_INVALID;
		gboolean is_string = FALSE;
		
		g_assert (!t->byref);

		eklass = klass->element_class;

		mono_marshal_load_type_info (eklass);

		if (eklass == mono_defaults.string_class) {
			is_string = TRUE;
			conv = mono_marshal_get_string_to_ptr_conv (m->piinfo, spec);
		}
		else {
			g_assert_not_reached ();
		}

		if (is_string)
			esize = sizeof (gpointer);
		else if (eklass == mono_defaults.char_class)
			esize = mono_pinvoke_is_unicode (m->piinfo) ? 2 : 1;
		else
			esize = mono_class_native_size (eklass, NULL);

		src = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
		dest = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			
		mono_mb_emit_stloc (mb, src);
		mono_mb_emit_ldloc (mb, src);
		mono_mb_emit_stloc (mb, 3);

		/* Check for null */
		mono_mb_emit_ldloc (mb, src);
		label1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Allocate native array */
		mono_mb_emit_icon (mb, esize);
		mono_mb_emit_ldloc (mb, src);
		mono_mb_emit_byte (mb, CEE_LDLEN);

		if (eklass == mono_defaults.string_class) {
			/* Make the array bigger for the terminating null */
			mono_mb_emit_byte (mb, CEE_LDC_I4_1);
			mono_mb_emit_byte (mb, CEE_ADD);
		}
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_icall (mb, ves_icall_marshal_alloc);
		mono_mb_emit_stloc (mb, dest);
		mono_mb_emit_ldloc (mb, dest);
		mono_mb_emit_stloc (mb, 3);

		/* Emit marshalling loop */
		index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);
		label2 = mono_mb_get_label (mb);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, src);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		label3 = mono_mb_emit_branch (mb, CEE_BGE);

		/* Emit marshalling code */
		if (is_string) {
			int stind_op;
			g_assert (conv != MONO_MARSHAL_CONV_INVALID);

			/* dest */
			mono_mb_emit_ldloc (mb, dest);

			/* src */
			mono_mb_emit_ldloc (mb, src);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_byte (mb, CEE_LDELEM_REF);

			mono_mb_emit_icall (mb, conv_to_icall (conv, &stind_op));
			mono_mb_emit_byte (mb, stind_op);
		}
		else {
			char *msg = g_strdup ("Marshalling of non-string arrays to managed code is not implemented.");
			mono_mb_emit_exception_marshal_directive (mb, msg);
			return conv_arg;
		}

		mono_mb_emit_add_to_local (mb, index_var, 1);
		mono_mb_emit_add_to_local (mb, dest, esize);

		mono_mb_emit_branch_label (mb, CEE_BR, label2);

		mono_mb_patch_branch (mb, label3);
		mono_mb_patch_branch (mb, label1);
		break;
	}
	default:
		g_assert_not_reached ();
	}
#endif
	return conv_arg;
}

static MonoType*
marshal_boolean_conv_in_get_local_type (MonoMarshalSpec *spec, guint8 *ldc_op /*out*/)
{
	if (spec == NULL) {
		return &mono_defaults.int32_class->byval_arg;
	} else {
		switch (spec->native) {
		case MONO_NATIVE_I1:
		case MONO_NATIVE_U1:
			return &mono_defaults.byte_class->byval_arg;
		case MONO_NATIVE_VARIANTBOOL:
			if (ldc_op) *ldc_op = CEE_LDC_I4_M1;
			return &mono_defaults.int16_class->byval_arg;
		case MONO_NATIVE_BOOLEAN:
			return &mono_defaults.int32_class->byval_arg;
		default:
			g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
			return &mono_defaults.int32_class->byval_arg;
		}
	}
}

static MonoClass*
marshal_boolean_managed_conv_in_get_conv_arg_class (MonoMarshalSpec *spec, guint8 *ldop/*out*/)
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

static int
emit_marshal_boolean (EmitMarshalContext *m, int argnum, MonoType *t,
		      MonoMarshalSpec *spec, 
		      int conv_arg, MonoType **conv_arg_type, 
		      MarshalAction action)
{
#ifndef ENABLE_ILGEN
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		if (t->byref)
			*conv_arg_type = &mono_defaults.int_class->byval_arg;
		else
			*conv_arg_type = marshal_boolean_conv_in_get_local_type (spec, NULL);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		MonoClass* conv_arg_class = marshal_boolean_managed_conv_in_get_conv_arg_class (spec, NULL);
		if (t->byref)
			*conv_arg_type = &conv_arg_class->this_arg;
		else
			*conv_arg_type = &conv_arg_class->byval_arg;
		break;
	}

	}
#else
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoType *local_type;
		int label_false;
		guint8 ldc_op = CEE_LDC_I4_1;

		local_type = marshal_boolean_conv_in_get_local_type (spec, &ldc_op);
		if (t->byref)
			*conv_arg_type = &mono_defaults.int_class->byval_arg;
		else
			*conv_arg_type = local_type;
		conv_arg = mono_mb_add_local (mb, local_type);
		
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I1);
		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, ldc_op);
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_patch_branch (mb, label_false);

		break;
	}

	case MARSHAL_ACTION_CONV_OUT:
	{
		int label_false, label_end;
		if (!t->byref)
			break;

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);
		
		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);

		label_end = mono_mb_emit_branch (mb, CEE_BR);
		mono_mb_patch_branch (mb, label_false);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_patch_branch (mb, label_end);

		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else if (conv_arg)
			mono_mb_emit_ldloc (mb, conv_arg);
		else
			mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* maybe we need to make sure that it fits within 8 bits */
		mono_mb_emit_stloc (mb, 3);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		MonoClass* conv_arg_class = mono_defaults.int32_class;
		guint8 ldop = CEE_LDIND_I4;
		int label_null, label_false;

		conv_arg_class = marshal_boolean_managed_conv_in_get_conv_arg_class (spec, &ldop);
		conv_arg = mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

		if (t->byref)
			*conv_arg_type = &conv_arg_class->this_arg;
		else
			*conv_arg_type = &conv_arg_class->byval_arg;


		mono_mb_emit_ldarg (mb, argnum);
		
		/* Check null */
		if (t->byref) {
			label_null = mono_mb_emit_branch (mb, CEE_BRFALSE);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, ldop);
		} else
			label_null = 0;

		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_patch_branch (mb, label_false);

		if (t->byref) 
			mono_mb_patch_branch (mb, label_null);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		guint8 stop = CEE_STIND_I4;
		guint8 ldc_op = CEE_LDC_I4_1;
		int label_null,label_false, label_end;;

		if (!t->byref)
			break;
		if (spec) {
			switch (spec->native) {
			case MONO_NATIVE_I1:
			case MONO_NATIVE_U1:
				stop = CEE_STIND_I1;
				break;
			case MONO_NATIVE_VARIANTBOOL:
				stop = CEE_STIND_I2;
				ldc_op = CEE_LDC_I4_M1;
				break;
			default:
				break;
			}
		}
		
		/* Check null */
		mono_mb_emit_ldarg (mb, argnum);
		label_null = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);

		label_false = mono_mb_emit_branch (mb, CEE_BRFALSE);
		mono_mb_emit_byte (mb, ldc_op);
		label_end = mono_mb_emit_branch (mb, CEE_BR);

		mono_mb_patch_branch (mb, label_false);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_patch_branch (mb, label_end);

		mono_mb_emit_byte (mb, stop);
		mono_mb_patch_branch (mb, label_null);
		break;
	}

	default:
		g_assert_not_reached ();
	}
#endif
	return conv_arg;
}

static int
emit_marshal_ptr (EmitMarshalContext *m, int argnum, MonoType *t, 
		  MonoMarshalSpec *spec, int conv_arg, 
		  MonoType **conv_arg_type, MarshalAction action)
{
#ifdef ENABLE_ILGEN
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		/* MS seems to allow this in some cases, ie. bxc #158 */
		/*
		if (MONO_TYPE_ISSTRUCT (t->data.type) && !mono_class_from_mono_type (t->data.type)->blittable) {
			char *msg = g_strdup_printf ("Can not marshal 'parameter #%d': Pointers can not reference marshaled structures. Use byref instead.", argnum + 1);
			mono_mb_emit_exception_marshal_directive (m->mb, msg);
		}
		*/
		break;

	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* no conversions necessary */
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		break;
	}
#endif
	return conv_arg;
}

static int
emit_marshal_char (EmitMarshalContext *m, int argnum, MonoType *t, 
		   MonoMarshalSpec *spec, int conv_arg, 
		   MonoType **conv_arg_type, MarshalAction action)
{
#ifdef ENABLE_ILGEN
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_PUSH:
		/* fixme: dont know how to marshal that. We cant simply
		 * convert it to a one byte UTF8 character, because an
		 * unicode character may need more that one byte in UTF8 */
		mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* fixme: we need conversions here */
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		break;
	}
#endif
	return conv_arg;
}

static int
emit_marshal_scalar (EmitMarshalContext *m, int argnum, MonoType *t, 
		     MonoMarshalSpec *spec, int conv_arg, 
		     MonoType **conv_arg_type, MarshalAction action)
{
#ifdef ENABLE_ILGEN
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_PUSH:
		mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* no conversions necessary */
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		break;
	}
#endif
	return conv_arg;
}

static int
emit_marshal (EmitMarshalContext *m, int argnum, MonoType *t, 
	      MonoMarshalSpec *spec, int conv_arg, 
	      MonoType **conv_arg_type, MarshalAction action)
{
	/* Ensure that we have marshalling info for this param */
	mono_marshal_load_type_info (mono_class_from_mono_type (t));

	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return emit_marshal_custom (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return emit_marshal_asany (m, argnum, t, spec, conv_arg, conv_arg_type, action);
			
	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass == mono_defaults.handleref_class)
			return emit_marshal_handleref (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		
		return emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_STRING:
		return emit_marshal_string (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
#if !defined(DISABLE_COM) && defined(ENABLE_ILGEN)
		if (spec && spec->native == MONO_NATIVE_STRUCT)
			return emit_marshal_variant (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

#if !defined(DISABLE_COM)
		if (spec && (spec->native == MONO_NATIVE_IUNKNOWN ||
			spec->native == MONO_NATIVE_IDISPATCH ||
			spec->native == MONO_NATIVE_INTERFACE))
			return mono_cominterop_emit_marshal_com_interface (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		if (spec && (spec->native == MONO_NATIVE_SAFEARRAY) && 
			(spec->data.safearray_data.elem_type == MONO_VARIANT_VARIANT) && 
			((action == MARSHAL_ACTION_CONV_OUT) || (action == MARSHAL_ACTION_CONV_IN) || (action == MARSHAL_ACTION_PUSH)))
			return mono_cominterop_emit_marshal_safearray (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

		if (mono_class_try_get_safehandle_class () != NULL && t->data.klass &&
		    mono_class_is_subclass_of (t->data.klass,  mono_class_try_get_safehandle_class (), FALSE))
			return emit_marshal_safehandle (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		
		return emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		return emit_marshal_array (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_BOOLEAN:
		return emit_marshal_boolean (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_PTR:
		return emit_marshal_ptr (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CHAR:
		return emit_marshal_char (m, argnum, t, spec, conv_arg, conv_arg_type, action);
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
		return emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (t))
			return emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		else
			return emit_marshal_object (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	default:
		return conv_arg;
	}
}

/* How the arguments of an icall should be wrapped */
typedef enum {
	/* Don't wrap at all, pass the argument as is */
	ICALL_HANDLES_WRAP_NONE,
	/* Wrap the argument in an object handle, pass the handle to the icall */
	ICALL_HANDLES_WRAP_OBJ,
	/* Wrap the argument in an object handle, pass the handle to the icall,
	   write the value out from the handle when the icall returns */
	ICALL_HANDLES_WRAP_OBJ_INOUT,
	/* Initialized an object handle to null, pass to the icalls,
	   write the value out from the handle when the icall returns */
	ICALL_HANDLES_WRAP_OBJ_OUT,
	/* Wrap the argument (a valuetype reference) in a handle to pin its enclosing object,
	   but pass the raw reference to the icall */
	ICALL_HANDLES_WRAP_VALUETYPE_REF,
} IcallHandlesWrap;

typedef struct {
	IcallHandlesWrap wrap;
	/* if wrap is NONE or OBJ or VALUETYPE_REF, this is not meaningful.
	   if wrap is OBJ_INOUT it's the local var that holds the MonoObjectHandle.
	*/
	int handle;
}  IcallHandlesLocal;

/*
 * Describes how to wrap the given parameter.
 *
 */
static IcallHandlesWrap
signature_param_uses_handles (MonoMethodSignature *sig, int param)
{
	if (MONO_TYPE_IS_REFERENCE (sig->params [param])) {
		if (mono_signature_param_is_out (sig, param))
			return ICALL_HANDLES_WRAP_OBJ_OUT;
		else if (mono_type_is_byref (sig->params [param]))
			return ICALL_HANDLES_WRAP_OBJ_INOUT;
		else
			return ICALL_HANDLES_WRAP_OBJ;
	} else if (mono_type_is_byref (sig->params [param]))
		return ICALL_HANDLES_WRAP_VALUETYPE_REF;
	else
		return ICALL_HANDLES_WRAP_NONE;
}


#ifdef ENABLE_ILGEN
/**
 * mono_marshal_emit_native_wrapper:
 * \param image the image to use for looking up custom marshallers
 * \param sig The signature of the native function
 * \param piinfo Marshalling information
 * \param mspecs Marshalling information
 * \param aot whenever the created method will be compiled by the AOT compiler
 * \param method if non-NULL, the pinvoke method to call
 * \param check_exceptions Whenever to check for pending exceptions after the native call
 * \param func_param the function to call is passed as a boxed IntPtr as the first parameter
 *
 * generates IL code for the pinvoke wrapper, the generated code calls \p func .
 */
void
mono_marshal_emit_native_wrapper (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, gboolean aot, gboolean check_exceptions, gboolean func_param)
{
	EmitMarshalContext m;
	MonoMethodSignature *csig;
	MonoClass *klass;
	int i, argnum, *tmp_locals;
	int type, param_shift = 0;
	int coop_gc_stack_dummy, coop_gc_var;
#ifndef DISABLE_COM
	int coop_cominterop_fnptr;
#endif

	memset (&m, 0, sizeof (m));
	m.mb = mb;
	m.sig = sig;
	m.piinfo = piinfo;

	/* we copy the signature, so that we can set pinvoke to 0 */
	if (func_param) {
		/* The function address is passed as the first argument */
		g_assert (!sig->hasthis);
		param_shift += 1;
	}
	csig = mono_metadata_signature_dup_full (mb->method->klass->image, sig);
	csig->pinvoke = 1;
	m.csig = csig;
	m.image = image;

	if (sig->hasthis)
		param_shift += 1;

	/* we allocate local for use with emit_struct_conv() */
	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	/* delete_old = FALSE */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	if (mono_threads_is_blocking_transition_enabled ()) {
		/* local 4, dummy local used to get a stack address for suspend funcs */
		coop_gc_stack_dummy = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* local 5, the local to be used when calling the suspend funcs */
		coop_gc_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
#ifndef DISABLE_COM
		if (!func_param && MONO_CLASS_IS_IMPORT (mb->method->klass)) {
			coop_cominterop_fnptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		}
#endif
	}

	/*
	 * cookie = mono_threads_enter_gc_safe_region_unbalanced (ref dummy);
	 *
	 * ret = method (...);
	 *
	 * mono_threads_exit_gc_safe_region_unbalanced (cookie, ref dummy);
	 *
	 * <interrupt check>
	 *
	 * return ret;
	 */

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		m.vtaddr_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	if (mspecs [0] && mspecs [0]->native == MONO_NATIVE_CUSTOM) {
		/* Return type custom marshaling */
		/*
		 * Since we can't determine the return type of the unmanaged function,
		 * we assume it returns a pointer, and pass that pointer to
		 * MarshalNativeToManaged.
		 */
		csig->ret = &mono_defaults.int_class->byval_arg;
	}

	/* we first do all conversions */
	tmp_locals = (int *)alloca (sizeof (int) * sig->param_count);
	m.orig_conv_args = (int *)alloca (sizeof (int) * (sig->param_count + 1));

	for (i = 0; i < sig->param_count; i ++) {
		tmp_locals [i] = emit_marshal (&m, i + param_shift, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_CONV_IN);
	}

	// In coop mode need to register blocking state during native call
	if (mono_threads_is_blocking_transition_enabled ()) {
		// Perform an extra, early lookup of the function address, so any exceptions
		// potentially resulting from the lookup occur before entering blocking mode.
		if (!func_param && !MONO_CLASS_IS_IMPORT (mb->method->klass) && aot) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_ICALL_ADDR, &piinfo->method);
			mono_mb_emit_byte (mb, CEE_POP); // Result not needed yet
		}

#ifndef DISABLE_COM
		if (!func_param && MONO_CLASS_IS_IMPORT (mb->method->klass)) {
			mono_mb_emit_cominterop_get_function_pointer (mb, &piinfo->method);
			mono_mb_emit_stloc (mb, coop_cominterop_fnptr);
		}
#endif

		mono_mb_emit_ldloc_addr (mb, coop_gc_stack_dummy);
		mono_mb_emit_icall (mb, mono_threads_enter_gc_safe_region_unbalanced);
		mono_mb_emit_stloc (mb, coop_gc_var);
	}

	/* push all arguments */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++) {
		emit_marshal (&m, i + param_shift, sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_PUSH);
	}			

	/* call the native method */
	if (func_param) {
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_op (mb, CEE_UNBOX, mono_defaults.int_class);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_calli (mb, csig);
	} else if (MONO_CLASS_IS_IMPORT (mb->method->klass)) {
#ifndef DISABLE_COM
		if (!mono_threads_is_blocking_transition_enabled ()) {
			mono_mb_emit_cominterop_call (mb, csig, &piinfo->method);
		} else {
			mono_mb_emit_ldloc (mb, coop_cominterop_fnptr);
			mono_mb_emit_cominterop_call_function_pointer (mb, csig);
		}
#else
		g_assert_not_reached ();
#endif
	} else {
		if (aot) {
			/* Reuse the ICALL_ADDR opcode for pinvokes too */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_ICALL_ADDR, &piinfo->method);
			mono_mb_emit_calli (mb, csig);
		} else {			
			mono_mb_emit_native_call (mb, csig, func);
		}
	}

	/* Set LastError if needed */
	if (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) {
#ifdef TARGET_WIN32
		if (!aot) {
			static MonoMethodSignature *get_last_error_sig = NULL;
			if (!get_last_error_sig) {
				get_last_error_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
				get_last_error_sig->ret = &mono_defaults.int_class->byval_arg;
				get_last_error_sig->pinvoke = 1;
			}

			/*
			 * Have to call GetLastError () early and without a wrapper, since various runtime components could
			 * clobber its value.
			 */
			mono_mb_emit_native_call (mb, get_last_error_sig, GetLastError);
			mono_mb_emit_icall (mb, mono_marshal_set_last_error_windows);
		} else {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_GET_LAST_ERROR);
			mono_mb_emit_icall (mb, mono_marshal_set_last_error_windows);
		}
#else
		mono_mb_emit_icall (mb, mono_marshal_set_last_error);
#endif
	}

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		MonoClass *klass = mono_class_from_mono_type (sig->ret);
		mono_class_init (klass);
		if (!(mono_class_is_explicit_layout (klass) || klass->blittable)) {
			/* This is used by emit_marshal_vtype (), but it needs to go right before the call */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			mono_mb_emit_stloc (mb, m.vtaddr_var);
		}
	}

	/* Unblock before converting the result, since that can involve calls into the runtime */
	if (mono_threads_is_blocking_transition_enabled ()) {
		mono_mb_emit_ldloc (mb, coop_gc_var);
		mono_mb_emit_ldloc_addr (mb, coop_gc_stack_dummy);
		mono_mb_emit_icall (mb, mono_threads_exit_gc_safe_region_unbalanced);
	}

	/* convert the result */
	if (!sig->ret->byref) {
		MonoMarshalSpec *spec = mspecs [0];
		type = sig->ret->type;

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
		} else {
		handle_enum:
			switch (type) {
			case MONO_TYPE_VOID:
				break;
			case MONO_TYPE_VALUETYPE:
				klass = sig->ret->data.klass;
				if (klass->enumtype) {
					type = mono_class_enum_basetype (sig->ret->data.klass)->type;
					goto handle_enum;
				}
				emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
				break;
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
			case MONO_TYPE_STRING:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_BOOLEAN:
			case MONO_TYPE_ARRAY:
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CHAR:
			case MONO_TYPE_PTR:
			case MONO_TYPE_GENERICINST:
				emit_marshal (&m, 0, sig->ret, spec, 0, NULL, MARSHAL_ACTION_CONV_RESULT);
				break;
			case MONO_TYPE_TYPEDBYREF:
			default:
				g_warning ("return type 0x%02x unknown", sig->ret->type);	
				g_assert_not_reached ();
			}
		}
	} else {
		mono_mb_emit_stloc (mb, 3);
	}

	/* 
	 * Need to call this after converting the result since MONO_VTADDR needs 
	 * to be adjacent to the call instruction.
	 */
	if (check_exceptions)
		emit_thread_interrupt_checkpoint (mb);

	/* we need to convert byref arguments back and free string arrays */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		argnum = i + param_shift;

		if (spec && ((spec->native == MONO_NATIVE_CUSTOM) || (spec->native == MONO_NATIVE_ASANY))) {
			emit_marshal (&m, argnum, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_CONV_OUT);
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_STRING:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_BOOLEAN:
			emit_marshal (&m, argnum, t, spec, tmp_locals [i], NULL, MARSHAL_ACTION_CONV_OUT);
			break;
		default:
			break;
		}
	}

	if (!MONO_TYPE_IS_VOID(sig->ret))
		mono_mb_emit_ldloc (mb, 3);

	mono_mb_emit_byte (mb, CEE_RET);
}
#endif /* ENABLE_ILGEN */

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
	gpointer iter;
	int i;
	const char *exc_class = "MissingMethodException";
	const char *exc_arg = NULL;
	WrapperInfo *info;

	g_assert (method != NULL);
	g_assert (mono_method_signature (method)->pinvoke);

	GHashTable **cache_ptr;

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

	sig = mono_method_signature (method);

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		pinvoke = TRUE;

	if (!piinfo->addr) {
		if (pinvoke) {
			if (method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)
				exc_arg = "Method contains unsupported native code";
			else if (!aot)
				mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
		} else {
			piinfo->addr = mono_lookup_internal_call (method);
		}
	}

	/* hack - redirect certain string constructors to CreateString */
	if (piinfo->addr == ves_icall_System_String_ctor_RedirectToCreateString) {
		g_assert (!pinvoke);
		g_assert (method->string_ctor);
		g_assert (sig->hasthis);

		/* CreateString returns a value */
		csig = mono_metadata_signature_dup_full (method->klass->image, sig);
		csig->ret = &mono_defaults.string_class->byval_arg;
		csig->pinvoke = 0;

		iter = NULL;
		while ((res = mono_class_get_methods (mono_defaults.string_class, &iter))) {
			if (!strcmp ("CreateString", res->name) &&
				mono_metadata_signature_equal (csig, mono_method_signature (res))) {
				WrapperInfo *info;

				g_assert (!(res->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
				g_assert (!(res->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

				/* create a wrapper to preserve .ctor in stack trace */
				mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_MANAGED);

#ifdef ENABLE_ILGEN
				mono_mb_emit_byte (mb, CEE_LDARG_0);
				for (i = 1; i <= csig->param_count; i++)
					mono_mb_emit_ldarg (mb, i);
				mono_mb_emit_managed_call (mb, res, NULL);
				mono_mb_emit_byte (mb, CEE_RET);
#endif

				info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_STRING_CTOR);
				info->d.string_ctor.method = method;

				/* use native_wrapper_cache because internal calls are looked up there */
				res = mono_mb_create_and_cache_full (cache, method, mb, csig,
													 csig->param_count + 1, info, NULL);
				mono_mb_free (mb);

				return res;
			}
		}

		/* exception will be thrown */
		piinfo->addr = NULL;
		g_warning ("cannot find CreateString for .ctor");
	}

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	/*
	 * In AOT mode and embedding scenarios, it is possible that the icall is not
	 * registered in the runtime doing the AOT compilation.
	 */
	if (!piinfo->addr && !aot) {
#ifdef ENABLE_ILGEN
		mono_mb_emit_exception (mb, exc_class, exc_arg);
#endif
		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
		info->d.managed_to_native.method = method;

		csig = mono_metadata_signature_dup_full (method->klass->image, sig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache_full (cache, method, mb, csig,
											 csig->param_count + 16, info, NULL);
		mono_mb_free (mb);

		return res;
	}

	/* internal calls: we simply push all arguments and call the method (no conversions) */
	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		if (sig->hasthis)
			csig = mono_metadata_signature_dup_add_this (method->klass->image, sig, method->klass);
		else
			csig = mono_metadata_signature_dup_full (method->klass->image, sig);

		//printf ("%s\n", mono_method_full_name (method, 1));

		/* hack - string constructors returns a value */
		if (method->string_ctor)
			csig->ret = &mono_defaults.string_class->byval_arg;

#ifdef ENABLE_ILGEN
		// FIXME:
		MonoClass *handle_stack_mark_class;
		MonoClass *error_class;
		int thread_info_var = -1, stack_mark_var = -1, error_var = -1;
		MonoMethodSignature *call_sig = csig;
		gboolean uses_handles = FALSE;
		gboolean save_handles_to_locals = FALSE;
		IcallHandlesLocal *handles_locals = NULL;

		(void) mono_lookup_internal_call_full (method, &uses_handles);

		/* If it uses handles and MonoError, it had better check exceptions */
		g_assert (!uses_handles || check_exceptions);

		if (uses_handles) {
			MonoMethodSignature *ret;

			/* Add a MonoError argument and figure out which args need to be wrapped in handles */
			// FIXME: The stuff from mono_metadata_signature_dup_internal_with_padding ()
			ret = mono_metadata_signature_alloc (method->klass->image, csig->param_count + 1);

			ret->param_count = csig->param_count + 1;
			ret->ret = csig->ret;

			handles_locals = g_new0 (IcallHandlesLocal, csig->param_count);
			for (int i = 0; i < csig->param_count; ++i) {
				IcallHandlesWrap w = signature_param_uses_handles (csig, i);
				handles_locals [i].wrap = w;
				switch (w) {
				case ICALL_HANDLES_WRAP_OBJ:
				case ICALL_HANDLES_WRAP_OBJ_INOUT:
				case ICALL_HANDLES_WRAP_OBJ_OUT:
					ret->params [i] = mono_class_get_byref_type (mono_class_from_mono_type(csig->params[i]));
					if (w == ICALL_HANDLES_WRAP_OBJ_OUT || w == ICALL_HANDLES_WRAP_OBJ_INOUT)
						save_handles_to_locals = TRUE;
					break;
				case ICALL_HANDLES_WRAP_NONE:
				case ICALL_HANDLES_WRAP_VALUETYPE_REF:
					ret->params [i] = csig->params [i];
					break;
				default:
					g_assert_not_reached ();
				}
			}
			/* Add MonoError* param */
			ret->params [csig->param_count] = &mono_get_intptr_class ()->byval_arg;
			ret->pinvoke = csig->pinvoke;

			call_sig = ret;
		}

		if (uses_handles) {
			handle_stack_mark_class = mono_class_load_from_name (mono_get_corlib (), "Mono", "RuntimeStructs/HandleStackMark");
			error_class = mono_class_load_from_name (mono_get_corlib (), "Mono", "RuntimeStructs/MonoError");

			thread_info_var = mono_mb_add_local (mb, &mono_get_intptr_class ()->byval_arg);
			stack_mark_var = mono_mb_add_local (mb, &handle_stack_mark_class->byval_arg);
			error_var = mono_mb_add_local (mb, &error_class->byval_arg);

			if (save_handles_to_locals) {
				/* add a local var to hold the handles for each out arg */
				for (int i = 0; i < sig->param_count; ++i) {
					int j = i + sig->hasthis;
					switch (handles_locals[j].wrap) {
					case ICALL_HANDLES_WRAP_NONE:
					case ICALL_HANDLES_WRAP_OBJ:
					case ICALL_HANDLES_WRAP_VALUETYPE_REF:
						handles_locals [j].handle = -1;
						break;
					case ICALL_HANDLES_WRAP_OBJ_INOUT:
					case ICALL_HANDLES_WRAP_OBJ_OUT:
						handles_locals [j].handle = mono_mb_add_local (mb, sig->params [i]);
						break;
					default:
						g_assert_not_reached ();
					}
				}
			}
		}

		if (sig->hasthis) {
			int pos;

			/*
			 * Add a null check since public icalls can be called with 'call' which
			 * does no such check.
			 */
			mono_mb_emit_byte (mb, CEE_LDARG_0);			
			pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
			mono_mb_emit_exception (mb, "NullReferenceException", NULL);
			mono_mb_patch_branch (mb, pos);
		}

		if (uses_handles) {
			mono_mb_emit_ldloc_addr (mb, stack_mark_var);
			mono_mb_emit_ldloc_addr (mb, error_var);
			mono_mb_emit_icall (mb, mono_icall_start);
			mono_mb_emit_stloc (mb, thread_info_var);

			if (sig->hasthis) {
				mono_mb_emit_byte (mb, CEE_LDARG_0);
				/* TODO support adding wrappers to non-static struct methods */
				g_assert (!mono_class_is_valuetype(mono_method_get_class (method)));
				mono_mb_emit_icall (mb, mono_icall_handle_new);
			}
			for (i = 0; i < sig->param_count; i++) {
				/* load each argument. references into the managed heap get wrapped in handles */
				int j = i + sig->hasthis;
				switch (handles_locals[j].wrap) {
				case ICALL_HANDLES_WRAP_NONE:
					mono_mb_emit_ldarg (mb, j);
					break;
				case ICALL_HANDLES_WRAP_OBJ:
					/* argI = mono_handle_new (argI_raw) */
					mono_mb_emit_ldarg (mb, j);
					mono_mb_emit_icall (mb, mono_icall_handle_new);
					break;
				case ICALL_HANDLES_WRAP_OBJ_INOUT:
				case ICALL_HANDLES_WRAP_OBJ_OUT:
					/* if inout:
					 *   handleI = argI = mono_handle_new (*argI_raw)
					 * otherwise:
					 *   handleI = argI = mono_handle_new (NULL)
					 */
					if (handles_locals[j].wrap == ICALL_HANDLES_WRAP_OBJ_INOUT) {
						mono_mb_emit_ldarg (mb, j);
						mono_mb_emit_byte (mb, CEE_LDIND_REF);
					} else
						mono_mb_emit_byte (mb, CEE_LDNULL);
					mono_mb_emit_icall (mb, mono_icall_handle_new);
					/* tmp = argI */
					mono_mb_emit_byte (mb, CEE_DUP);
					/* handleI = tmp */
					mono_mb_emit_stloc (mb, handles_locals[j].handle);
					break;
				case ICALL_HANDLES_WRAP_VALUETYPE_REF:
					/* (void) mono_handle_new (argI); argI */
					mono_mb_emit_ldarg (mb, j);
					mono_mb_emit_byte (mb, CEE_DUP);
					mono_mb_emit_icall (mb, mono_icall_handle_new_interior);
					mono_mb_emit_byte (mb, CEE_POP);
#if 0
					fprintf (stderr, " Method %s.%s.%s has byref valuetype argument %d\n", method->klass->name_space, method->klass->name, method->name, i);
#endif
					break;
				default:
					g_assert_not_reached ();
				}
			}
			mono_mb_emit_ldloc_addr (mb, error_var);
		} else {
			if (sig->hasthis)
				mono_mb_emit_byte (mb, CEE_LDARG_0);
			for (i = 0; i < sig->param_count; i++)
				mono_mb_emit_ldarg (mb, i + sig->hasthis);
		}

		if (aot) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_ICALL_ADDR, &piinfo->method);
			mono_mb_emit_calli (mb, call_sig);
		} else {
			g_assert (piinfo->addr);
			mono_mb_emit_native_call (mb, call_sig, piinfo->addr);
		}

		if (uses_handles) {
			if (MONO_TYPE_IS_REFERENCE (sig->ret)) {
				// if (ret != NULL_HANDLE) {
				//   ret = MONO_HANDLE_RAW(ret)
				// }
				mono_mb_emit_byte (mb, CEE_DUP);
				int pos = mono_mb_emit_branch (mb, CEE_BRFALSE);
				mono_mb_emit_ldflda (mb, MONO_HANDLE_PAYLOAD_OFFSET (MonoObject));
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_mb_patch_branch (mb, pos);
			}
			if (save_handles_to_locals) {
				for (i = 0; i < sig->param_count; i++) {
					int j = i + sig->hasthis;
					switch (handles_locals [j].wrap) {
					case ICALL_HANDLES_WRAP_NONE:
					case ICALL_HANDLES_WRAP_OBJ:
					case ICALL_HANDLES_WRAP_VALUETYPE_REF:
						break;
					case ICALL_HANDLES_WRAP_OBJ_INOUT:
					case ICALL_HANDLES_WRAP_OBJ_OUT:
						/* *argI_raw = MONO_HANDLE_RAW (handleI) */

						/* argI_raw */
						mono_mb_emit_ldarg (mb, j);
						/* handleI */
						mono_mb_emit_ldloc (mb, handles_locals [j].handle);
						/* MONO_HANDLE_RAW(handleI) */
						mono_mb_emit_ldflda (mb, MONO_HANDLE_PAYLOAD_OFFSET (MonoObject));
						mono_mb_emit_byte (mb, CEE_LDIND_REF);
						/* *argI_raw = MONO_HANDLE_RAW(handleI) */
						mono_mb_emit_byte (mb, CEE_STIND_REF);
						break;
					default:
						g_assert_not_reached ();
					}
				}
			}
			g_free (handles_locals);

			mono_mb_emit_ldloc (mb, thread_info_var);
			mono_mb_emit_ldloc_addr (mb, stack_mark_var);
			mono_mb_emit_ldloc_addr (mb, error_var);
			mono_mb_emit_icall (mb, mono_icall_end);
		}

		if (check_exceptions)
			emit_thread_interrupt_checkpoint (mb);
		mono_mb_emit_byte (mb, CEE_RET);
#endif
		info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
		info->d.managed_to_native.method = method;

		csig = mono_metadata_signature_dup_full (method->klass->image, csig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache_full (cache, method, mb, csig, csig->param_count + 16,
											 info, NULL);

		mono_mb_free (mb);
		return res;
	}

	g_assert (pinvoke);
	if (!aot)
		g_assert (piinfo->addr);

#ifdef ENABLE_ILGEN
	mspecs = g_new (MonoMarshalSpec*, sig->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	mono_marshal_emit_native_wrapper (mb->method->klass->image, mb, sig, piinfo, mspecs, piinfo->addr, aot, check_exceptions, FALSE);
#endif
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_PINVOKE);
	info->d.managed_to_native.method = method;

	csig = mono_metadata_signature_dup_full (method->klass->image, sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache_full (cache, method, mb, csig, csig->param_count + 16,
										 info, NULL);
	mono_mb_free (mb);

#ifdef ENABLE_ILGEN
	for (i = sig->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);
#endif

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

#ifdef ENABLE_ILGEN
	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, func, FALSE, TRUE, FALSE);
#endif

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
	MonoMethod *invoke = mono_get_delegate_invoke (klass);
	MonoImage *image = invoke->klass->image;
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

	mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature (invoke)->param_count + 1);
	mono_method_get_marshal_info (invoke, mspecs);
	/* Freed below so don't alloc from mempool */
	sig = mono_metadata_signature_dup (mono_method_signature (invoke));
	sig->hasthis = 0;

	name = g_strdup_printf ("wrapper_aot_native");
	mb = mono_mb_new (invoke->klass, name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	mb->method->save_lmf = 1;

#ifdef ENABLE_ILGEN
	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, NULL, FALSE, TRUE, TRUE);
#endif

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NATIVE_FUNC_AOT);
	info->d.managed_to_native.method = invoke;

	g_assert (!sig->hasthis);
	csig = mono_metadata_signature_dup_add_this (image, sig, mono_defaults.object_class);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache_full (cache, invoke,
										 mb, csig, csig->param_count + 16,
										 info, NULL);
	mono_mb_free (mb);

	for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);
	g_free (sig);

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
mono_marshal_emit_managed_wrapper (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, uint32_t target_handle)
{
#ifndef ENABLE_ILGEN
	MonoMethodSignature *sig, *csig;
	int i;

	sig = m->sig;
	csig = m->csig;

	/* we first do all conversions */
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];

		switch (t->type) {
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
		case MONO_TYPE_BOOLEAN:
			emit_marshal (m, i, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_MANAGED_CONV_IN);
		}
	}

	if (!sig->ret->byref) {
		switch (sig->ret->type) {
		case MONO_TYPE_STRING:
			csig->ret = &mono_defaults.int_class->byval_arg;
			break;
		default:
			break;
		}
	}
#else
	MonoMethodSignature *sig, *csig;
	MonoExceptionClause *clauses, *clause_finally, *clause_catch;
	int i, *tmp_locals, ex_local, e_local, attach_cookie_local, attach_dummy_local;
	int leave_try_pos, leave_catch_pos, ex_m1_pos;
	gboolean closed = FALSE;

	sig = m->sig;
	csig = m->csig;

	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	if (!sig->hasthis && sig->param_count != invoke_sig->param_count) {
		/* Closed delegate */
		g_assert (sig->param_count == invoke_sig->param_count + 1);
		closed = TRUE;
		/* Use a new signature without the first argument */
		sig = mono_metadata_signature_dup (sig);
		memmove (&sig->params [0], &sig->params [1], (sig->param_count - 1) * sizeof (MonoType*));
		sig->param_count --;
	}

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	if (MONO_TYPE_ISSTRUCT (sig->ret))
		m->vtaddr_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	ex_local = mono_mb_add_local (mb, &mono_defaults.uint32_class->byval_arg);
	e_local = mono_mb_add_local (mb, &mono_defaults.exception_class->byval_arg);

	attach_cookie_local = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	attach_dummy_local = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/*
	 * guint32 ex = -1;
	 * try {
	 *   // does (STARTING|RUNNING|BLOCKING) -> RUNNING + set/switch domain
	 *   mono_threads_attach_coop ();
	 *
	 *   <interrupt check>
	 *
	 *   ret = method (...);
	 * } catch (Exception e) {
	 *   ex = mono_gchandle_new (e, false);
	 * } finally {
	 *   // does RUNNING -> (RUNNING|BLOCKING) + unset/switch domain
	 *   mono_threads_detach_coop ();
	 *
	 *   if (ex != -1)
	 *     mono_marshal_ftnptr_eh_callback (ex);
	 * }
	 *
	 * return ret;
	 */

	clauses = g_new0 (MonoExceptionClause, 2);

	clause_catch = &clauses [0];
	clause_catch->flags = MONO_EXCEPTION_CLAUSE_NONE;
	clause_catch->data.catch_class = mono_defaults.exception_class;

	clause_finally = &clauses [1];
	clause_finally->flags = MONO_EXCEPTION_CLAUSE_FINALLY;

	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	mono_mb_emit_icon (mb, -1);
	mono_mb_emit_byte (mb, CEE_CONV_U4);
	mono_mb_emit_stloc (mb, ex_local);

	/* try { */
	clause_catch->try_offset = clause_finally->try_offset = mono_mb_get_label (mb);

	if (!mono_threads_is_blocking_transition_enabled ()) {
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_JIT_ATTACH);
	} else {
		/* mono_threads_attach_coop (); */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_LDDOMAIN);
		mono_mb_emit_ldloc_addr (mb, attach_dummy_local);
		/*
		 * This icall is special cased in the JIT so it works in native-to-managed wrappers in unattached threads.
		 * Keep this in sync with the CEE_JIT_ICALL code in the JIT.
		 */
		mono_mb_emit_icall (mb, mono_threads_attach_coop);
		mono_mb_emit_stloc (mb, attach_cookie_local);
	}

	/* <interrupt check> */
	emit_thread_interrupt_checkpoint (mb);

	/* we first do all conversions */
	tmp_locals = (int *)alloca (sizeof (int) * sig->param_count);
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];

		switch (t->type) {
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
		case MONO_TYPE_BOOLEAN:
			tmp_locals [i] = emit_marshal (m, i, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_MANAGED_CONV_IN);

			break;
		default:
			tmp_locals [i] = 0;
			break;
		}
	}

	if (sig->hasthis) {
		if (target_handle) {
			mono_mb_emit_icon (mb, (gint32)target_handle);
			mono_mb_emit_icall (mb, mono_gchandle_get_target);
		} else {
			/* fixme: */
			g_assert_not_reached ();
		}
	} else if (closed) {
		mono_mb_emit_icon (mb, (gint32)target_handle);
		mono_mb_emit_icall (mb, mono_gchandle_get_target);
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];

		if (tmp_locals [i]) {
			if (t->byref)
				mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			else
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
		}
		else
			mono_mb_emit_ldarg (mb, i);
	}

	/* ret = method (...) */
	mono_mb_emit_managed_call (mb, method, NULL);

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		MonoClass *klass = mono_class_from_mono_type (sig->ret);
		mono_class_init (klass);
		if (!(mono_class_is_explicit_layout (klass) || klass->blittable)) {
			/* This is used by emit_marshal_vtype (), but it needs to go right before the call */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			mono_mb_emit_stloc (mb, m->vtaddr_var);
		}
	}

	if (mspecs [0] && mspecs [0]->native == MONO_NATIVE_CUSTOM) {
		emit_marshal (m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
	} else if (!sig->ret->byref) { 
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		case MONO_TYPE_OBJECT:
			mono_mb_emit_stloc (mb, 3);
			break;
		case MONO_TYPE_STRING:
			csig->ret = &mono_defaults.int_class->byval_arg;
			emit_marshal (m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
			break;
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
			emit_marshal (m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
			break;
		default:
			g_warning ("return type 0x%02x unknown", sig->ret->type);	
			g_assert_not_reached ();
		}
	} else {
		mono_mb_emit_stloc (mb, 3);
	}

	/* Convert byref arguments back */
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		if (spec && spec->native == MONO_NATIVE_CUSTOM) {
			emit_marshal (m, i, t, mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
		}
		else if (t->byref) {
			switch (t->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
			case MONO_TYPE_OBJECT:
			case MONO_TYPE_STRING:
			case MONO_TYPE_BOOLEAN:
				emit_marshal (m, i, t, mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			default:
				break;
			}
		}
		else if (invoke_sig->params [i]->attrs & PARAM_ATTRIBUTE_OUT) {
			/* The [Out] information is encoded in the delegate signature */
			switch (t->type) {
			case MONO_TYPE_SZARRAY:
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
				emit_marshal (m, i, invoke_sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	leave_try_pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* } [endtry] */

	/* catch (Exception e) { */
	clause_catch->try_len = mono_mb_get_label (mb) - clause_catch->try_offset;
	clause_catch->handler_offset = mono_mb_get_label (mb);

	mono_mb_emit_stloc (mb, e_local);

	/* ex = mono_gchandle_new (e, false); */
	mono_mb_emit_ldloc (mb, e_local);
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_icall (mb, mono_gchandle_new);
	mono_mb_emit_stloc (mb, ex_local);

	leave_catch_pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* } [endcatch] */
	clause_catch->handler_len = mono_mb_get_pos (mb) - clause_catch->handler_offset;

	/* finally { */
	clause_finally->try_len = mono_mb_get_label (mb) - clause_finally->try_offset;
	clause_finally->handler_offset = mono_mb_get_label (mb);

	if (!mono_threads_is_blocking_transition_enabled ()) {
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_JIT_DETACH);
	} else {
		/* mono_threads_detach_coop (); */
		mono_mb_emit_ldloc (mb, attach_cookie_local);
		mono_mb_emit_ldloc_addr (mb, attach_dummy_local);
		mono_mb_emit_icall (mb, mono_threads_detach_coop);
	}

	/* if (ex != -1) */
	mono_mb_emit_ldloc (mb, ex_local);
	mono_mb_emit_icon (mb, -1);
	mono_mb_emit_byte (mb, CEE_CONV_U4);
	ex_m1_pos = mono_mb_emit_branch (mb, CEE_BEQ);

	/* mono_marshal_ftnptr_eh_callback (ex) */
	mono_mb_emit_ldloc (mb, ex_local);
	mono_mb_emit_icall (mb, mono_marshal_ftnptr_eh_callback);

	/* [ex == -1] */
	mono_mb_patch_branch (mb, ex_m1_pos);

	mono_mb_emit_byte (mb, CEE_ENDFINALLY);

	/* } [endfinally] */
	clause_finally->handler_len = mono_mb_get_pos (mb) - clause_finally->handler_offset;

	mono_mb_patch_branch (mb, leave_try_pos);
	mono_mb_patch_branch (mb, leave_catch_pos);

	/* return ret; */
	if (m->retobj_var) {
		mono_mb_emit_ldloc (mb, m->retobj_var);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_op (mb, CEE_MONO_RETOBJ, m->retobj_class);
	}
	else {
		if (!MONO_TYPE_IS_VOID(sig->ret))
			mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_byte (mb, CEE_RET);
	}

	mono_mb_set_clauses (mb, 2, clauses);

	if (closed)
		g_free (sig);
#endif
}

static void 
mono_marshal_set_callconv_from_modopt (MonoMethod *method, MonoMethodSignature *csig)
{
	MonoMethodSignature *sig;
	int i;

#ifdef TARGET_WIN32
	/* 
	 * Under windows, delegates passed to native code must use the STDCALL
	 * calling convention.
	 */
	csig->call_convention = MONO_CALL_STDCALL;
#endif

	sig = mono_method_signature (method);

	/* Change default calling convention if needed */
	/* Why is this a modopt ? */
	if (sig->ret && sig->ret->num_mods) {
		for (i = 0; i < sig->ret->num_mods; ++i) {
			MonoError error;
			MonoClass *cmod_class = mono_class_get_checked (method->klass->image, sig->ret->modifiers [i].token, &error);
			g_assert (mono_error_ok (&error));
			if ((cmod_class->image == mono_defaults.corlib) && !strcmp (cmod_class->name_space, "System.Runtime.CompilerServices")) {
				if (!strcmp (cmod_class->name, "CallConvCdecl"))
					csig->call_convention = MONO_CALL_C;
				else if (!strcmp (cmod_class->name, "CallConvStdcall"))
					csig->call_convention = MONO_CALL_STDCALL;
				else if (!strcmp (cmod_class->name, "CallConvFastcall"))
					csig->call_convention = MONO_CALL_FASTCALL;
				else if (!strcmp (cmod_class->name, "CallConvThiscall"))
					csig->call_convention = MONO_CALL_THISCALL;
			}
		}
	}
}

/**
 * mono_marshal_get_managed_wrapper:
 * Generates IL code to call managed methods from unmanaged code 
 * If \p target_handle is \c 0, the wrapper info will be a \c WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, uint32_t target_handle, MonoError *error)
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

	invoke = mono_get_delegate_invoke (delegate_klass);
	invoke_sig = mono_method_signature (invoke);

	mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature (invoke)->param_count + 1);
	mono_method_get_marshal_info (invoke, mspecs);

	sig = mono_method_signature (method);

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);

	/*the target gchandle must be the first entry after size and the wrapper itself.*/
	mono_mb_add_data (mb, GUINT_TO_POINTER (target_handle));

	/* we copy the signature, so that we can modify it */
	if (target_handle)
		/* Need to free this later */
		csig = mono_metadata_signature_dup (invoke_sig);
	else
		csig = mono_metadata_signature_dup_full (method->klass->image, invoke_sig);
	csig->hasthis = 0;
	csig->pinvoke = 1;

	memset (&m, 0, sizeof (m));
	m.mb = mb;
	m.sig = sig;
	m.piinfo = NULL;
	m.retobj_var = 0;
	m.csig = csig;
	m.image = method->klass->image;

	mono_marshal_set_callconv_from_modopt (invoke, csig);

	/* The attribute is only available in Net 2.0 */
	if (mono_class_try_get_unmanaged_function_pointer_attribute_class ()) {
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
			MonoArray *typed_args, *named_args;
			CattrNamedArg *arginfo;
			MonoObject *o;
			gint32 call_conv;
			gint32 charset = 0;
			MonoBoolean set_last_error = 0;
			MonoError error;

			mono_reflection_create_custom_attr_data_args (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size, &typed_args, &named_args, &arginfo, &error);
			g_assert (mono_error_ok (&error));
			g_assert (mono_array_length (typed_args) == 1);

			/* typed args */
			o = mono_array_get (typed_args, MonoObject*, 0);
			call_conv = *(gint32*)mono_object_unbox (o);

			/* named args */
			for (i = 0; i < mono_array_length (named_args); ++i) {
				CattrNamedArg *narg = &arginfo [i];

				o = mono_array_get (named_args, MonoObject*, i);

				g_assert (narg->field);
				if (!strcmp (narg->field->name, "CharSet")) {
					charset = *(gint32*)mono_object_unbox (o);
				} else if (!strcmp (narg->field->name, "SetLastError")) {
					set_last_error = *(MonoBoolean*)mono_object_unbox (o);
				} else if (!strcmp (narg->field->name, "BestFitMapping")) {
					// best_fit_mapping = *(MonoBoolean*)mono_object_unbox (o);
				} else if (!strcmp (narg->field->name, "ThrowOnUnmappableChar")) {
					// throw_on_unmappable = *(MonoBoolean*)mono_object_unbox (o);
				} else {
					g_assert_not_reached ();
				}
			}

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
#ifdef ENABLE_ILGEN
		mb->dynamic = TRUE;
#endif
		res = mono_mb_create (mb, csig, sig->param_count + 16, NULL);
	}
	mono_mb_free (mb);

	for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* mono_method_print_code (res); */

	return res;
}

gpointer
mono_marshal_get_vtfixup_ftnptr (MonoImage *image, guint32 token, guint16 type)
{
	MonoError error;
	MonoMethod *method;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	int i, param_count;

	g_assert (token);

	method = mono_get_method_checked (image, token, NULL, NULL, &error);
	if (!method)
		g_error ("Could not load vtfixup token 0x%x due to %s", token, mono_error_get_message (&error));
	g_assert (method);

	if (type & (VTFIXUP_TYPE_FROM_UNMANAGED | VTFIXUP_TYPE_FROM_UNMANAGED_RETAIN_APPDOMAIN)) {
		MonoMethodSignature *csig;
		MonoMarshalSpec **mspecs;
		EmitMarshalContext m;

		sig = mono_method_signature (method);
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

		mono_marshal_set_callconv_from_modopt (method, csig);

		/* FIXME: Implement VTFIXUP_TYPE_FROM_UNMANAGED_RETAIN_APPDOMAIN. */

		mono_marshal_emit_managed_wrapper (mb, sig, mspecs, &m, method, 0);

#ifdef ENABLE_ILGEN
		mb->dynamic = TRUE;
#endif
		method = mono_mb_create (mb, csig, sig->param_count + 16, NULL);
		mono_mb_free (mb);

		for (i = sig->param_count; i >= 0; i--)
			if (mspecs [i])
				mono_metadata_free_marshal_spec (mspecs [i]);
		g_free (mspecs);

		gpointer compiled_ptr = mono_compile_method_checked (method, &error);
		mono_error_assert_ok (&error);
		return compiled_ptr;
	}

	sig = mono_method_signature (method);
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_MANAGED);

	param_count = sig->param_count + sig->hasthis;
#ifdef ENABLE_ILGEN
	for (i = 0; i < param_count; i++)
		mono_mb_emit_ldarg (mb, i);

	if (type & VTFIXUP_TYPE_CALL_MOST_DERIVED)
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	else
		mono_mb_emit_op (mb, CEE_CALL, method);
	mono_mb_emit_byte (mb, CEE_RET);

	mb->dynamic = TRUE;
#endif

	method = mono_mb_create (mb, sig, param_count, NULL);
	mono_mb_free (mb);

	gpointer compiled_ptr = mono_compile_method_checked (method, &error);
	mono_error_assert_ok (&error);
	return compiled_ptr;
}

#ifdef ENABLE_ILGEN

/*
 * The code directly following this is the cache hit, value positive branch
 *
 * This function takes a new method builder with 0 locals and adds two locals
 * to create multiple out-branches and the fall through state of having the object
 * on the stack after a cache miss
 */
static void
generate_check_cache (int obj_arg_position, int class_arg_position, int cache_arg_position, // In-parameters
											int *null_obj, int *cache_hit_neg, int *cache_hit_pos, // Out-parameters
											MonoMethodBuilder *mb)
{
	int cache_miss_pos;

	/* allocate local 0 (pointer) obj_vtable */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) cached_vtable */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/*if (!obj)*/
	mono_mb_emit_ldarg (mb, obj_arg_position);
	*null_obj = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*obj_vtable = obj->vtable;*/
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 0);

	/* cached_vtable = *cache*/
	mono_mb_emit_ldarg (mb, cache_arg_position);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 1);

	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_LDC_I4);
	mono_mb_emit_i4 (mb, ~0x1);
	mono_mb_emit_byte (mb, CEE_CONV_I);
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_ldloc (mb, 0);
	/*if ((cached_vtable & ~0x1)== obj_vtable)*/
	cache_miss_pos = mono_mb_emit_branch (mb, CEE_BNE_UN);

	/*return (cached_vtable & 0x1) ? NULL : obj;*/
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte(mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, CEE_CONV_U);
	mono_mb_emit_byte (mb, CEE_AND);
	*cache_hit_neg = mono_mb_emit_branch (mb, CEE_BRTRUE);
	*cache_hit_pos = mono_mb_emit_branch (mb, CEE_BR);

	// slow path
	mono_mb_patch_branch (mb, cache_miss_pos);

	// if isinst
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_ldarg (mb, class_arg_position);
	mono_mb_emit_ldarg (mb, cache_arg_position);
	mono_mb_emit_icall (mb, mono_marshal_isinst_with_cache);
}

#endif /* ENABLE_ILGEN */

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
	int return_null_pos, positive_cache_hit_pos, negative_cache_hit_pos, invalid_cast_pos;
	WrapperInfo *info;

	const int obj_arg_position = 0;
	const int class_arg_position = 1;
	const int cache_arg_position = 2;

	if (cached)
		return cached;

	mb = mono_mb_new (mono_defaults.object_class, "__castclass_with_cache", MONO_WRAPPER_CASTCLASS);
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	sig->params [obj_arg_position] = &mono_defaults.object_class->byval_arg;
	sig->params [class_arg_position] = &mono_defaults.int_class->byval_arg;
	sig->params [cache_arg_position] = &mono_defaults.int_class->byval_arg;
	sig->ret = &mono_defaults.object_class->byval_arg;
	sig->pinvoke = 0;

#ifdef ENABLE_ILGEN
	generate_check_cache (obj_arg_position, class_arg_position, cache_arg_position, 
												&return_null_pos, &negative_cache_hit_pos, &positive_cache_hit_pos, mb);
	invalid_cast_pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*return obj;*/
	mono_mb_patch_branch (mb, positive_cache_hit_pos);
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_byte (mb, CEE_RET);

	/*fails*/
	mono_mb_patch_branch (mb, negative_cache_hit_pos);
	mono_mb_patch_branch (mb, invalid_cast_pos);
	mono_mb_emit_exception (mb, "InvalidCastException", NULL);

	/*return null*/
	mono_mb_patch_branch (mb, return_null_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_RET);
#endif /* ENABLE_ILGEN */

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
static MonoObject *
mono_marshal_isinst_with_cache (MonoObject *obj, MonoClass *klass, uintptr_t *cache)
{
	MonoError error;
	MonoObject *isinst = mono_object_isinst_checked (obj, klass, &error);
	if (mono_error_set_pending_exception (&error))
		return NULL;

	if (mono_object_is_transparent_proxy (obj))
		return isinst;

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
	int return_null_pos, positive_cache_hit_pos, negative_cache_hit_pos;
	WrapperInfo *info;

	const int obj_arg_position = 0;
	const int class_arg_position = 1;
	const int cache_arg_position = 2;

	if (cached)
		return cached;

	mb = mono_mb_new (mono_defaults.object_class, "__isinst_with_cache", MONO_WRAPPER_CASTCLASS);
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	// The object
	sig->params [obj_arg_position] = &mono_defaults.object_class->byval_arg;
	// The class
	sig->params [class_arg_position] = &mono_defaults.int_class->byval_arg;
	// The cache
	sig->params [cache_arg_position] = &mono_defaults.int_class->byval_arg;
	sig->ret = &mono_defaults.object_class->byval_arg;
	sig->pinvoke = 0;

#ifdef ENABLE_ILGEN
	generate_check_cache (obj_arg_position, class_arg_position, cache_arg_position, 
		&return_null_pos, &negative_cache_hit_pos, &positive_cache_hit_pos, mb);
	// Return the object gotten via the slow path.
	mono_mb_emit_byte (mb, CEE_RET);

	// return NULL;
	mono_mb_patch_branch (mb, negative_cache_hit_pos);
	mono_mb_patch_branch (mb, return_null_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_RET);

	// return obj
	mono_mb_patch_branch (mb, positive_cache_hit_pos);
	mono_mb_emit_ldarg (mb, obj_arg_position);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

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
	static MonoMethod *stoptr = NULL;
	MonoMethod *res;
	WrapperInfo *info;

	g_assert (klass != NULL);

	mono_marshal_load_type_info (klass);

	MonoMarshalType *marshal_info = mono_class_get_marshal_info (klass);
	if (marshal_info->str_to_ptr)
		return marshal_info->str_to_ptr;

	if (!stoptr) 
		stoptr = mono_class_get_method_from_name (mono_defaults.marshal_class, "StructureToPtr", 3);
	g_assert (stoptr);

	mb = mono_mb_new (klass, stoptr->name, MONO_WRAPPER_UNKNOWN);

#ifdef ENABLE_ILGEN
	if (klass->blittable) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 2 (boolean) delete_old */
		mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDARG_2);
		mono_mb_emit_stloc (mb, 2);

		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_stloc (mb, 0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_stloc (mb, 1);

		emit_struct_conv (mb, klass, FALSE);
	}

	mono_mb_emit_byte (mb, CEE_RET);
#endif
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
		sig = mono_create_icall_signature ("void ptr object");
		sig = mono_metadata_signature_dup_full (mono_defaults.corlib, sig);
		sig->pinvoke = 0;
		mono_memory_barrier ();
		ptostr = sig;
	}

	mb = mono_mb_new (klass, "PtrToStructure", MONO_WRAPPER_UNKNOWN);

#ifdef ENABLE_ILGEN
	if (klass->blittable) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_ldflda (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, &klass->this_arg);
		
		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_stloc (mb, 0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_op (mb, CEE_UNBOX, klass);
		mono_mb_emit_stloc (mb, 1);

		emit_struct_conv (mb, klass, TRUE);
	}

	mono_mb_emit_byte (mb, CEE_RET);
#endif
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

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_UNKNOWN);
#ifdef ENABLE_ILGEN
	mono_mb_emit_exception_full (mb, "System", "ExecutionEngineException", "Shouldn't be called.");
	mono_mb_emit_byte (mb, CEE_RET);
#endif
	sig = mono_metadata_signature_dup_full (method->klass->image, mono_method_signature (method));

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_SYNCHRONIZED_INNER);
	info->d.synchronized_inner.method = method;
	res = mono_mb_create (mb, sig, 0, info);
	mono_mb_free (mb);
	if (ctx) {
		MonoError error;
		res = mono_class_inflate_generic_method_checked (res, ctx, &error);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
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
	static MonoMethod *enter_method, *exit_method, *gettypefromhandle_method;
	MonoMethodSignature *sig;
	MonoExceptionClause *clause;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	WrapperInfo *info;
	int i, pos, pos2, this_local, taken_local, ret_local = 0;
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
		cache = get_cache (&method->klass->image->wrapper_caches.synchronized_cache, mono_aligned_addr_hash, NULL);
		if ((res = mono_marshal_find_in_cache (cache, method)))
			return res;
	}

	sig = mono_metadata_signature_dup_full (method->klass->image, mono_method_signature (method));
	sig->pinvoke = 0;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_SYNCHRONIZED);

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.synchronized.method = method;

#ifdef ENABLE_ILGEN
	mb->skip_visibility = 1;
	/* result */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		ret_local = mono_mb_add_local (mb, sig->ret);
#endif

	if (method->klass->valuetype && !(method->flags & MONO_METHOD_ATTR_STATIC)) {
		/* FIXME Is this really the best way to signal an error here?  Isn't this called much later after class setup? -AK */
		mono_class_set_type_load_failure (method->klass, "");
#ifdef ENABLE_ILGEN
		/* This will throw the type load exception when the wrapper is compiled */
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_op (mb, CEE_ISINST, method->klass);
		mono_mb_emit_byte (mb, CEE_POP);

		if (!MONO_TYPE_IS_VOID (sig->ret))
			mono_mb_emit_ldloc (mb, ret_local);
		mono_mb_emit_byte (mb, CEE_RET);
#endif

		res = mono_mb_create_and_cache_full (cache, method,
											 mb, sig, sig->param_count + 16, info, NULL);
		mono_mb_free (mb);

		return res;
	}

#ifdef ENABLE_ILGEN
	/* this */
	this_local = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	taken_local = mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	clause = (MonoExceptionClause *)mono_image_alloc0 (method->klass->image, sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_FINALLY;
#endif

	mono_marshal_lock ();

	if (!enter_method) {
		MonoMethodDesc *desc;

		desc = mono_method_desc_new ("Monitor:Enter(object,bool&)", FALSE);
		enter_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (enter_method);
		mono_method_desc_free (desc);

		desc = mono_method_desc_new ("Monitor:Exit", FALSE);
		exit_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (exit_method);
		mono_method_desc_free (desc);

		desc = mono_method_desc_new ("Type:GetTypeFromHandle", FALSE);
		gettypefromhandle_method = mono_method_desc_search_in_class (desc, mono_defaults.systemtype_class);
		g_assert (gettypefromhandle_method);
		mono_method_desc_free (desc);
	}

	mono_marshal_unlock ();

#ifdef ENABLE_ILGEN
	/* Push this or the type object */
	if (method->flags & METHOD_ATTRIBUTE_STATIC) {
		/* We have special handling for this in the JIT */
		int index = mono_mb_add_data (mb, method->klass);
		mono_mb_add_data (mb, mono_defaults.typehandle_class);
		mono_mb_emit_byte (mb, CEE_LDTOKEN);
		mono_mb_emit_i4 (mb, index);

		mono_mb_emit_managed_call (mb, gettypefromhandle_method, NULL);
	}
	else
		mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_stloc (mb, this_local);

	/* Call Monitor::Enter() */
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_ldloc_addr (mb, taken_local);
	mono_mb_emit_managed_call (mb, enter_method, NULL);

	clause->try_offset = mono_mb_get_label (mb);

	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));

	if (ctx) {
		MonoError error;
		mono_mb_emit_managed_call (mb, mono_class_inflate_generic_method_checked (method, &container->context, &error), NULL);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
	} else {
		mono_mb_emit_managed_call (mb, method, NULL);
	}

	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, ret_local);

	pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
	clause->handler_offset = mono_mb_get_label (mb);

	/* Call Monitor::Exit() if needed */
	mono_mb_emit_ldloc (mb, taken_local);
	pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_managed_call (mb, exit_method, NULL);
	mono_mb_patch_branch (mb, pos2);
	mono_mb_emit_byte (mb, CEE_ENDFINALLY);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_patch_branch (mb, pos);
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldloc (mb, ret_local);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_set_clauses (mb, 1, clause);
#endif

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
	MonoMethodSignature *sig = mono_method_signature (method);
	int i;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	WrapperInfo *info;

	cache = get_cache (&mono_method_get_wrapper_cache (method)->unbox_wrapper_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_UNBOX);

	g_assert (sig->hasthis);
	
#ifdef ENABLE_ILGEN
	mono_mb_emit_ldarg (mb, 0); 
	mono_mb_emit_icon (mb, sizeof (MonoObject));
	mono_mb_emit_byte (mb, CEE_ADD);
	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);
#endif

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	info->d.unbox.method = method;

	res = mono_mb_create_and_cache_full (cache, method,
										 mb, sig, sig->param_count + 16, info, NULL);
	mono_mb_free (mb);

	/* mono_method_print_code (res); */

	return res;	
}

enum {
	STELEMREF_OBJECT, /*no check at all*/
	STELEMREF_SEALED_CLASS, /*check vtable->klass->element_type */
	STELEMREF_CLASS, /*only the klass->parents check*/
	STELEMREF_CLASS_SMALL_IDEPTH, /* like STELEMREF_CLASS bit without the idepth check */
	STELEMREF_INTERFACE, /*interfaces without variant generic arguments. */
	STELEMREF_COMPLEX, /*arrays, MBR or types with variant generic args - go straight to icalls*/
	STELEMREF_KIND_COUNT
};

static const char *strelemref_wrapper_name[] = {
	"object", "sealed_class", "class", "class_small_idepth", "interface", "complex"
};

static gboolean
is_monomorphic_array (MonoClass *klass)
{
	MonoClass *element_class;
	if (klass->rank != 1)
		return FALSE;

	element_class = klass->element_class;
	return mono_class_is_sealed (element_class) || element_class->valuetype;
}

static int
get_virtual_stelemref_kind (MonoClass *element_class)
{
	if (element_class == mono_defaults.object_class)
		return STELEMREF_OBJECT;
	if (is_monomorphic_array (element_class))
		return STELEMREF_SEALED_CLASS;
	/* Compressed interface bitmaps require code that is quite complex, so don't optimize for it. */
	if (MONO_CLASS_IS_INTERFACE (element_class) && !mono_class_has_variant_generic_params (element_class))
#ifdef COMPRESSED_INTERFACE_BITMAP
		return STELEMREF_COMPLEX;
#else
		return STELEMREF_INTERFACE;
#endif
	/*Arrays are sealed but are covariant on their element type, We can't use any of the fast paths.*/
	if (mono_class_is_marshalbyref (element_class) || element_class->rank || mono_class_has_variant_generic_params (element_class))
		return STELEMREF_COMPLEX;
	if (mono_class_is_sealed (element_class))
		return STELEMREF_SEALED_CLASS;
	if (element_class->idepth <= MONO_DEFAULT_SUPERTABLE_SIZE)
		return STELEMREF_CLASS_SMALL_IDEPTH;

	return STELEMREF_CLASS;
}

#ifdef ENABLE_ILGEN

static void
load_array_element_address (MonoMethodBuilder *mb)
{
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_op (mb, CEE_LDELEMA, mono_defaults.object_class);
}

static void
load_array_class (MonoMethodBuilder *mb, int aklass)
{
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, element_class));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);
}

static void
load_value_class (MonoMethodBuilder *mb, int vklass)
{
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);
}
#endif

#if 0
static void
record_slot_vstore (MonoObject *array, size_t index, MonoObject *value)
{
	char *name = mono_type_get_full_name (array->vtable->klass->element_class);
	printf ("slow vstore of %s\n", name);
	g_free (name);
}
#endif

/*
 * TODO:
 *	- Separate simple interfaces from variant interfaces or mbr types. This way we can avoid the icall for them.
 *	- Emit a (new) mono bytecode that produces OP_COND_EXC_NE_UN to raise ArrayTypeMismatch
 *	- Maybe mve some MonoClass field into the vtable to reduce the number of loads
 *	- Add a case for arrays of arrays.
 */
static MonoMethod*
get_virtual_stelemref_wrapper (int kind)
{
	static MonoMethod *cached_methods [STELEMREF_KIND_COUNT] = { NULL }; /*object iface sealed regular*/
	static MonoMethodSignature *signature;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	char *name;
	const char *param_names [16];
	guint32 b1, b2, b3, b4;
	int aklass, vklass, vtable, uiid;
	int array_slot_addr;
	WrapperInfo *info;

	if (cached_methods [kind])
		return cached_methods [kind];

	name = g_strdup_printf ("virt_stelemref_%s", strelemref_wrapper_name [kind]);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_STELEMREF);
	g_free (name);

	if (!signature) {
		MonoMethodSignature *sig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);

		/* void this::stelemref (size_t idx, void* value) */
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->hasthis = TRUE;
		sig->params [0] = &mono_defaults.int_class->byval_arg; /* this is a natural sized int */
		sig->params [1] = &mono_defaults.object_class->byval_arg;
		signature = sig;
	}

#ifdef ENABLE_ILGEN
	param_names [0] = "index";
	param_names [1] = "value";
	mono_mb_set_param_names (mb, param_names);

	/*For now simply call plain old stelemref*/
	switch (kind) {
	case STELEMREF_OBJECT:
		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		/* do_store */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);
		break;

	case STELEMREF_COMPLEX: {
		int b_fast;
		/*
		<ldelema (bound check)>
		if (!value)
			goto store;
		if (!mono_object_isinst (value, aklass))
			goto do_exception;

		 do_store:
			 *array_slot_addr = value;

		do_exception:
			throw new ArrayTypeMismatchException ();
		*/

		aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		vklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		array_slot_addr = mono_mb_add_local (mb, &mono_defaults.object_class->this_arg);

#if 0
		{
			/*Use this to debug/record stores that are going thru the slow path*/
			MonoMethodSignature *csig;
			csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
			csig->ret = &mono_defaults.void_class->byval_arg;
			csig->params [0] = &mono_defaults.object_class->byval_arg;
			csig->params [1] = &mono_defaults.int_class->byval_arg; /* this is a natural sized int */
			csig->params [2] = &mono_defaults.object_class->byval_arg;
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_ldarg (mb, 2);
			mono_mb_emit_native_call (mb, csig, record_slot_vstore);
		}
#endif

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);
		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/* fastpath */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldloc (mb, aklass);
		b_fast = mono_mb_emit_branch (mb, CEE_BEQ);

		/*if (mono_object_isinst (value, aklass)) */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_icall (mb, mono_object_isinst_icall);
		b2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_patch_branch (mb, b_fast);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b2);

		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;
	}
	case STELEMREF_SEALED_CLASS:
		/*
		<ldelema (bound check)>
		if (!value)
			goto store;

		aklass = array->vtable->klass->element_class;
		vklass = value->vtable->klass;

		if (vklass != aklass)
			goto do_exception;

		do_store:
			 *array_slot_addr = value;

		do_exception:
			throw new ArrayTypeMismatchException ();
		*/
		aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		vklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		array_slot_addr = mono_mb_add_local (mb, &mono_defaults.object_class->this_arg);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/*if (vklass != aklass) goto do_exception; */
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldloc (mb, vklass);
		b2 = mono_mb_emit_branch (mb, CEE_BNE_UN);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b2);
		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;

	case STELEMREF_CLASS: {
		/*
		the method:
		<ldelema (bound check)>
		if (!value)
			goto do_store;

		aklass = array->vtable->klass->element_class;
		vklass = value->vtable->klass;

		if (vklass->idepth < aklass->idepth)
			goto do_exception;

		if (vklass->supertypes [aklass->idepth - 1] != aklass)
			goto do_exception;

		do_store:
			*array_slot_addr = value;
			return;

		long:
			throw new ArrayTypeMismatchException ();
		*/
		aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		vklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		array_slot_addr = mono_mb_add_local (mb, &mono_defaults.object_class->this_arg);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/* if (vklass->idepth < aklass->idepth) goto failue */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		b3 = mono_mb_emit_branch (mb, CEE_BLT_UN);

		/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, supertypes));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_icon (mb, sizeof (void*));
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		b4 = mono_mb_emit_branch (mb, CEE_BNE_UN);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b3);
		mono_mb_patch_branch (mb, b4);

		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;
	}

	case STELEMREF_CLASS_SMALL_IDEPTH:
		/*
		the method:
		<ldelema (bound check)>
		if (!value)
			goto do_store;

		aklass = array->vtable->klass->element_class;
		vklass = value->vtable->klass;

		if (vklass->supertypes [aklass->idepth - 1] != aklass)
			goto do_exception;

		do_store:
			*array_slot_addr = value;
			return;

		long:
			throw new ArrayTypeMismatchException ();
		*/
		aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		vklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		array_slot_addr = mono_mb_add_local (mb, &mono_defaults.object_class->this_arg);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* aklass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vklass = value->vtable->klass */
		load_value_class (mb, vklass);

		/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, supertypes));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_icon (mb, sizeof (void*));
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		b4 = mono_mb_emit_branch (mb, CEE_BNE_UN);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b4);

		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;

	case STELEMREF_INTERFACE:
		/*Mono *klass;
		MonoVTable *vt;
		unsigned uiid;
		if (value == NULL)
			goto store;

		klass = array->obj.vtable->klass->element_class;
		vt = value->vtable;
		uiid = klass->interface_id;
		if (uiid > vt->max_interface_id)
			goto exception;
		if (!(vt->interface_bitmap [(uiid) >> 3] & (1 << ((uiid)&7))))
			goto exception;
		store:
			mono_array_setref (array, index, value);
			return;
		exception:
			mono_raise_exception (mono_get_exception_array_type_mismatch ());*/

		array_slot_addr = mono_mb_add_local (mb, &mono_defaults.object_class->this_arg);
		aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		vtable = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		uiid = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);

		/* ldelema (implicit bound check) */
		load_array_element_address (mb);
		mono_mb_emit_stloc (mb, array_slot_addr);

		/* if (!value) goto do_store */
		mono_mb_emit_ldarg (mb, 2);
		b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* klass = array->vtable->klass->element_class */
		load_array_class (mb, aklass);

		/* vt = value->vtable */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, vtable);

		/* uiid = klass->interface_id; */
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, interface_id));
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		mono_mb_emit_stloc (mb, uiid);

		/*if (uiid > vt->max_interface_id)*/
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_ldloc (mb, vtable);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, max_interface_id));
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		b2 = mono_mb_emit_branch (mb, CEE_BGT_UN);

		/* if (!(vt->interface_bitmap [(uiid) >> 3] & (1 << ((uiid)&7)))) */

		/*vt->interface_bitmap*/
		mono_mb_emit_ldloc (mb, vtable);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, interface_bitmap));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		/*uiid >> 3*/
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_icon (mb, 3);
		mono_mb_emit_byte (mb, CEE_SHR_UN);

		/*vt->interface_bitmap [(uiid) >> 3]*/
		mono_mb_emit_byte (mb, CEE_ADD); /*interface_bitmap is a guint8 array*/
		mono_mb_emit_byte (mb, CEE_LDIND_U1);

		/*(1 << ((uiid)&7)))*/
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_icon (mb, 7);
		mono_mb_emit_byte (mb, CEE_AND);
		mono_mb_emit_byte (mb, CEE_SHL);

		/*bitwise and the whole thing*/
		mono_mb_emit_byte (mb, CEE_AND);
		b3 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* do_store: */
		mono_mb_patch_branch (mb, b1);
		mono_mb_emit_ldloc (mb, array_slot_addr);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		mono_mb_emit_byte (mb, CEE_RET);

		/* do_exception: */
		mono_mb_patch_branch (mb, b2);
		mono_mb_patch_branch (mb, b3);
		mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
		break;

	default:
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_managed_call (mb, mono_marshal_get_stelemref (), NULL);
		mono_mb_emit_byte (mb, CEE_RET);
		g_assert (0);
	}
#endif /* ENABLE_ILGEN */
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
	int kind;

	g_assert (array_class->rank == 1);
	kind = get_virtual_stelemref_kind (array_class->element_class);

	return get_virtual_stelemref_wrapper (kind);
}

MonoMethod**
mono_marshal_get_virtual_stelemref_wrappers (int *nwrappers)
{
	MonoMethod **res;
	int i;

	*nwrappers = STELEMREF_KIND_COUNT;
	res = (MonoMethod **)g_malloc0 (STELEMREF_KIND_COUNT * sizeof (MonoMethod*));
	for (i = 0; i < STELEMREF_KIND_COUNT; ++i)
		res [i] = get_virtual_stelemref_wrapper (i);
	return res;
}

/**
 * mono_marshal_get_stelemref:
 */
MonoMethod*
mono_marshal_get_stelemref (void)
{
	static MonoMethod* ret = NULL;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	WrapperInfo *info;
	
	guint32 b1, b2, b3, b4;
	guint32 copy_pos;
	int aklass, vklass;
	int array_slot_addr;
	
	if (ret)
		return ret;
	
	mb = mono_mb_new (mono_defaults.object_class, "stelemref", MONO_WRAPPER_STELEMREF);
	

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);

	/* void stelemref (void* array, int idx, void* value) */
	sig->ret = &mono_defaults.void_class->byval_arg;
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg; /* this is a natural sized int */
	sig->params [2] = &mono_defaults.object_class->byval_arg;

#ifdef ENABLE_ILGEN
	aklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	vklass = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	array_slot_addr = mono_mb_add_local (mb, &mono_defaults.object_class->this_arg);
	
	/*
	the method:
	<ldelema (bound check)>
	if (!value)
		goto store;
	
	aklass = array->vtable->klass->element_class;
	vklass = value->vtable->klass;
	
	if (vklass->idepth < aklass->idepth)
		goto long;
	
	if (vklass->supertypes [aklass->idepth - 1] != aklass)
		goto long;
	
	store:
		*array_slot_addr = value;
		return;
	
	long:
		if (mono_object_isinst (value, aklass))
			goto store;
		
		throw new ArrayTypeMismatchException ();
	*/
	
	/* ldelema (implicit bound check) */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_op (mb, CEE_LDELEMA, mono_defaults.object_class);
	mono_mb_emit_stloc (mb, array_slot_addr);
		
	/* if (!value) goto do_store */
	mono_mb_emit_ldarg (mb, 2);
	b1 = mono_mb_emit_branch (mb, CEE_BRFALSE);
	
	/* aklass = array->vtable->klass->element_class */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, element_class));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);
	
	/* vklass = value->vtable->klass */
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);
	
	/* if (vklass->idepth < aklass->idepth) goto failue */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);
	
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);
	
	b2 = mono_mb_emit_branch (mb, CEE_BLT_UN);
	
	/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, supertypes));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);
	mono_mb_emit_icon (mb, 1);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_icon (mb, sizeof (void*));
	mono_mb_emit_byte (mb, CEE_MUL);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	
	mono_mb_emit_ldloc (mb, aklass);
	
	b3 = mono_mb_emit_branch (mb, CEE_BNE_UN);
	
	copy_pos = mono_mb_get_label (mb);
	/* do_store */
	mono_mb_patch_branch (mb, b1);
	mono_mb_emit_ldloc (mb, array_slot_addr);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	
	mono_mb_emit_byte (mb, CEE_RET);
	
	/* the hard way */
	mono_mb_patch_branch (mb, b2);
	mono_mb_patch_branch (mb, b3);
	
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_icall (mb, mono_object_isinst_icall);
	
	b4 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	mono_mb_patch_addr (mb, b4, copy_pos - (b4 + 4));
	mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
	
	mono_mb_emit_byte (mb, CEE_RET);
#endif
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_NONE);
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

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
	static MonoMethod* ret = NULL;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	WrapperInfo *info;

	if (ret)
		return ret;
	
	mb = mono_mb_new (mono_defaults.object_class, "gsharedvt_in", MONO_WRAPPER_UNKNOWN);
	
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	sig->ret = &mono_defaults.void_class->byval_arg;

#ifdef ENABLE_ILGEN
	/*
	 * The body is generated by the JIT, we use a wrapper instead of a trampoline so EH works.
	 */
	mono_mb_emit_byte (mb, CEE_RET);
#endif
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GSHAREDVT_IN);
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

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
	static MonoMethod* ret = NULL;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	WrapperInfo *info;

	if (ret)
		return ret;
	
	mb = mono_mb_new (mono_defaults.object_class, "gsharedvt_out", MONO_WRAPPER_UNKNOWN);
	
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
	sig->ret = &mono_defaults.void_class->byval_arg;

#ifdef ENABLE_ILGEN
	/*
	 * The body is generated by the JIT, we use a wrapper instead of a trampoline so EH works.
	 */
	mono_mb_emit_byte (mb, CEE_RET);
#endif
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GSHAREDVT_OUT);
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

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
	int i, bounds, ind, realidx;
	int branch_pos, *branch_positions;
	int cached;

	ret = NULL;
	mono_marshal_lock ();
	for (i = 0; i < elem_addr_cache_next; ++i) {
		if (elem_addr_cache [i].rank == rank && elem_addr_cache [i].elem_size == elem_size) {
			ret = elem_addr_cache [i].method;
			break;
		}
	}
	mono_marshal_unlock ();
	if (ret)
		return ret;

	branch_positions = g_new0 (int, rank);

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1 + rank);

	/* void* address (void* array, int idx0, int idx1, int idx2, ...) */
	sig->ret = &mono_defaults.int_class->byval_arg;
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	for (i = 0; i < rank; ++i) {
		sig->params [i + 1] = &mono_defaults.int32_class->byval_arg;
	}

	name = g_strdup_printf ("ElementAddr_%d", elem_size);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_MANAGED);
	g_free (name);
	
#ifdef ENABLE_ILGEN
	bounds = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	ind = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	realidx = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);

	/* bounds = array->bounds; */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, bounds));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, bounds);

	/* ind is the overall element index, realidx is the partial index in a single dimension */
	/* ind = idx0 - bounds [0].lower_bound */
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldloc (mb, bounds);
	mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_stloc (mb, ind);
	/* if (ind >= bounds [0].length) goto exeception; */
	mono_mb_emit_ldloc (mb, ind);
	mono_mb_emit_ldloc (mb, bounds);
	mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoArrayBounds, length));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	/* note that we use unsigned comparison */
	branch_pos = mono_mb_emit_branch (mb, CEE_BGE_UN);

 	/* For large ranks (> 4?) use a loop n IL later to reduce code size.
	 * We could also decide to ignore the passed elem_size and get it
	 * from the array object, to reduce the number of methods we generate:
	 * the additional cost is 3 memory loads and a non-immediate mul.
	 */
	for (i = 1; i < rank; ++i) {
		/* realidx = idxi - bounds [i].lower_bound */
		mono_mb_emit_ldarg (mb, 1 + i);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + MONO_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_stloc (mb, realidx);
		/* if (realidx >= bounds [i].length) goto exeception; */
		mono_mb_emit_ldloc (mb, realidx);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + MONO_STRUCT_OFFSET (MonoArrayBounds, length));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		branch_positions [i] = mono_mb_emit_branch (mb, CEE_BGE_UN);
		/* ind = ind * bounds [i].length + realidx */
		mono_mb_emit_ldloc (mb, ind);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + MONO_STRUCT_OFFSET (MonoArrayBounds, length));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_ldloc (mb, realidx);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, ind);
	}

	/* return array->vector + ind * element_size */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoArray, vector));
	mono_mb_emit_ldloc (mb, ind);
	if (elem_size) {
		mono_mb_emit_icon (mb, elem_size);
	} else {
		/* Load arr->vtable->klass->sizes.element_class */
		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoObject, vtable));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoVTable, klass));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		/* sizes is an union, so this reads sizes.element_size */
		mono_mb_emit_icon (mb, MONO_STRUCT_OFFSET (MonoClass, sizes));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
	}
		mono_mb_emit_byte (mb, CEE_MUL);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_RET);

	/* patch the branches to get here and throw */
	for (i = 1; i < rank; ++i) {
		mono_mb_patch_branch (mb, branch_positions [i]);
	}
	mono_mb_patch_branch (mb, branch_pos);
	/* throw exception */
	mono_mb_emit_exception (mb, "IndexOutOfRangeException", NULL);

	g_free (branch_positions);
#endif /* ENABLE_ILGEN */

	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_ELEMENT_ADDR);
	info->d.element_addr.rank = rank;
	info->d.element_addr.elem_size = elem_size;
	ret = mono_mb_create (mb, sig, 4, info);
	mono_mb_free (mb);

	/* cache the result */
	cached = 0;
	mono_marshal_lock ();
	for (i = 0; i < elem_addr_cache_next; ++i) {
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
	int i;
	MonoGenericContext *ctx = NULL;
	MonoMethod *orig_method = NULL;
	MonoGenericContainer *container = NULL;
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
		cache = get_cache (&method->klass->image->array_accessor_cache, mono_aligned_addr_hash, NULL);
		if ((res = mono_marshal_find_in_cache (cache, method)))
			return res;
	}

	sig = mono_metadata_signature_dup_full (method->klass->image, mono_method_signature (method));
	sig->pinvoke = 0;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_UNKNOWN);

#ifdef ENABLE_ILGEN
	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));

	if (ctx) {
		MonoError error;
		mono_mb_emit_managed_call (mb, mono_class_inflate_generic_method_checked (method, &container->context, &error), NULL);
		g_assert (mono_error_ok (&error)); /* FIXME don't swallow the error */
	} else {
		mono_mb_emit_managed_call (mb, method, NULL);
	}
	mono_mb_emit_byte (mb, CEE_RET);
#endif

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
static inline void*
mono_marshal_alloc_co_task_mem (size_t size)
{
	if ((gulong)size == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		size = 4;

	return g_try_malloc ((gulong)size);
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
		mono_error_set_out_of_memory (error, "Could not allocate %lu bytes", size);

	return res;
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
static void*
ves_icall_marshal_alloc (gsize size)
{
	MonoError error;
	void *ret = mono_marshal_alloc (size, &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	return ret;
}

#ifndef HOST_WIN32
static inline void
mono_marshal_free_co_task_mem (void *ptr)
{
	g_free (ptr);
	return;
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
		if (ptr [i])
			g_free (ptr [i]);
}

void *
mono_marshal_string_to_utf16 (MonoString *s)
{
	return s ? mono_string_chars (s) : NULL;
}

/* This is a JIT icall, it sets the pending exception and returns NULL on error. */
static void *
mono_marshal_string_to_utf16_copy (MonoString *s)
{
	if (s == NULL) {
		return NULL;
	} else {
		MonoError error;
		gunichar2 *res = (gunichar2 *)mono_marshal_alloc ((mono_string_length (s) * 2) + 2, &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return NULL;
		}
		memcpy (res, mono_string_chars (s), mono_string_length (s) * 2);
		res [mono_string_length (s)] = 0;
		return res;
	}
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
	MONO_REQ_GC_SAFE_MODE;
#ifdef WIN32
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (GetLastError ()));
#else
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (errno));
#endif
}

static void
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
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length)
{
	int element_size;
	void *source_addr;

	MONO_CHECK_ARG_NULL (src,);
	MONO_CHECK_ARG_NULL (dest,);

	if (src->obj.vtable->klass->rank != 1) {
		mono_set_pending_exception (mono_get_exception_argument ("array", "array is multi-dimensional"));
		return;
	}
	if (start_index < 0) {
		mono_set_pending_exception (mono_get_exception_argument ("startIndex", "Must be >= 0"));
		return;
	}
	if (length < 0) {
		mono_set_pending_exception (mono_get_exception_argument ("length", "Must be >= 0"));
		return;
	}
	if (start_index + length > mono_array_length (src)) {
		mono_set_pending_exception (mono_get_exception_argument ("length", "start_index + length > array length"));
		return;
	}

	element_size = mono_array_element_size (src->obj.vtable->klass);

	/* no references should be involved */
	source_addr = mono_array_addr_with_size_fast (src, element_size, start_index);

	memcpy (dest, source_addr, length * element_size);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length)
{
	int element_size;
	void *dest_addr;

	MONO_CHECK_ARG_NULL (src,);
	MONO_CHECK_ARG_NULL (dest,);

	if (dest->obj.vtable->klass->rank != 1) {
		mono_set_pending_exception (mono_get_exception_argument ("array", "array is multi-dimensional"));
		return;
	}
	if (start_index < 0) {
		mono_set_pending_exception (mono_get_exception_argument ("startIndex", "Must be >= 0"));
		return;
	}
	if (length < 0) {
		mono_set_pending_exception (mono_get_exception_argument ("length", "Must be >= 0"));
		return;
	}
	if (start_index + length > mono_array_length (dest)) {
		mono_set_pending_exception (mono_get_exception_argument ("length", "start_index + length > array length"));
		return;
	}
	element_size = mono_array_element_size (dest->obj.vtable->klass);
	  
	/* no references should be involved */
	dest_addr = mono_array_addr_with_size_fast (dest, element_size, start_index);

	memcpy (dest_addr, src, length * element_size);
}

MonoStringHandle
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr, MonoError *error)
{
	error_init (error);
	if (ptr == NULL)
		return MONO_HANDLE_CAST (MonoString, NULL_HANDLE);
	else
		return mono_string_new_handle (mono_domain_get (), ptr, error);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len)
{
	MonoError error;
	MonoString *result = NULL;
	error_init (&error);
	if (ptr == NULL)
		mono_error_set_argument_null (&error, "ptr", "");
	else
		result = mono_string_new_len_checked (mono_domain_get (), ptr, len, &error);
	mono_error_set_pending_exception (&error);
	return result;
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr)
{
	MonoError error;
	MonoString *res = NULL;
	MonoDomain *domain = mono_domain_get (); 
	int len = 0;
	guint16 *t = ptr;

	if (ptr == NULL)
		return NULL;

	while (*t++)
		len++;

	res = mono_string_new_utf16_checked (domain, ptr, len, &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}
	return res;
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len)
{
	MonoError error;
	MonoString *res = NULL;
	MonoDomain *domain = mono_domain_get (); 

	error_init (&error);

	if (ptr == NULL) {
		res = NULL;
		mono_error_set_argument_null (&error, "ptr", "");
	} else {
		res = mono_string_new_utf16_checked (domain, ptr, len, &error);
	}

	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);
	return res;
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void)
{
	return (GPOINTER_TO_INT (mono_native_tls_get_value (last_error_tls_id)));
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionTypeHandle rtype, MonoError *error)
{
	MonoClass *klass;
	MonoType *type;
	guint32 layout;

	error_init (error);

	if (MONO_HANDLE_IS_NULL (rtype)) {
		mono_error_set_argument_null (error, "type", "");
		return 0;
	}

	type = MONO_HANDLE_GETVAL (rtype, type);
	klass = mono_class_from_mono_type (type);
	if (!mono_class_init (klass)) {
		mono_error_set_for_class_failure (error, klass);
		return 0;
	}

	layout = (mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK);

	if (type->type == MONO_TYPE_PTR || type->type == MONO_TYPE_FNPTR) {
		return sizeof (gpointer);
	} else if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		mono_error_set_argument (error, "t", "Type %s cannot be marshaled as an unmanaged structure.", klass->name);
		return 0;
	}

	return mono_class_native_size (klass, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old)
{
	MonoError error;
	MonoMethod *method;
	gpointer pa [3];

	MONO_CHECK_ARG_NULL (obj,);
	MONO_CHECK_ARG_NULL (dst,);

	method = mono_marshal_get_struct_to_ptr (obj->vtable->klass);

	pa [0] = obj;
	pa [1] = &dst;
	pa [2] = &delete_old;

	mono_runtime_invoke_checked (method, NULL, pa, &error);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);
}

static void
ptr_to_structure (gpointer src, MonoObject *dst, MonoError *error)
{
	MonoMethod *method;
	gpointer pa [2];

	error_init (error);

	method = mono_marshal_get_ptr_to_struct (dst->vtable->klass);

	pa [0] = &src;
	pa [1] = dst;

	mono_runtime_invoke_checked (method, NULL, pa, error);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst)
{
	MonoType *t;
	MonoError error;

	MONO_CHECK_ARG_NULL (src,);
	MONO_CHECK_ARG_NULL (dst,);
	
	t = mono_type_get_underlying_type (mono_class_get_type (dst->vtable->klass));

	if (t->type == MONO_TYPE_VALUETYPE) {
		MonoException *exc;
		gchar *tmp;

		tmp = g_strdup_printf ("Destination is a boxed value type.");
		exc = mono_get_exception_argument ("dst", tmp);
		g_free (tmp);  

		mono_set_pending_exception (exc);
		return;
	}

	ptr_to_structure (src, dst, &error);
	if (!mono_error_ok (&error))
		mono_error_set_pending_exception (&error);
}

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type)
{
	MonoError error;
	MonoClass *klass;
	MonoDomain *domain = mono_domain_get (); 
	MonoObject *res;

	if (src == NULL)
		return NULL;
	MONO_CHECK_ARG_NULL (type, NULL);

	klass = mono_class_from_mono_type (type->type);
	if (!mono_class_init (klass)) {
		mono_set_pending_exception (mono_class_get_exception_for_failure (klass));
		return NULL;
	}

	res = mono_object_new_checked (domain, klass, &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	ptr_to_structure (src, res, &error);
	if (!mono_error_ok (&error)) {
		mono_error_set_pending_exception (&error);
		return NULL;
	}

	return res;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionTypeHandle ref_type, MonoStringHandle field_name, MonoError *error)
{
	error_init (error);
	if (MONO_HANDLE_IS_NULL (ref_type)) {
		mono_error_set_argument_null (error, "type", "");
		return 0;
	}
	if (MONO_HANDLE_IS_NULL (field_name)) {
		mono_error_set_argument_null (error, "fieldName", "");
		return 0;
	}

	char *fname = mono_string_handle_to_utf8 (field_name, error);
	return_val_if_nok (error, 0);

	MonoType *type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass *klass = mono_class_from_mono_type (type);
	if (!mono_class_init (klass)) {
		mono_error_set_for_class_failure (error, klass);
		return 0;
	}

	int match_index = -1;
	while (klass && match_index == -1) {
		MonoClassField* field;
		int i = 0;
		gpointer iter = NULL;
		while ((field = mono_class_get_fields (klass, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (!strcmp (fname, mono_field_get_name (field))) {
				match_index = i;
				break;
			}
			i ++;
		}

		if (match_index == -1)
			klass = klass->parent;
        }

	g_free (fname);

	if(match_index == -1) {
		/* Get back original class instance */
		klass = mono_class_from_mono_type (type);

		mono_error_set_argument (error, "fieldName", "Field passed in is not a marshaled member of the type %s", klass->name);
		return 0;
	}

	MonoMarshalType *info = mono_marshal_load_type_info (klass);
	return info->fields [match_index].offset;
}

#ifndef HOST_WIN32
gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string)
{
	MonoError error;
	char *ret = mono_string_to_utf8_checked (string, &error);
	mono_error_set_pending_exception (&error);
	return ret;
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string)
{
	if (string == NULL)
		return NULL;
	else {
		gunichar2 *res = (gunichar2 *)g_malloc ((mono_string_length (string) + 1) * 2);

		memcpy (res, mono_string_chars (string), mono_string_length (string) * 2);
		res [mono_string_length (string)] = 0;
		return res;
	}
}
#endif /* !HOST_WIN32 */

static void
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
				klass->unicode, &conv);
			
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
		case MONO_MARSHAL_CONV_STR_BSTR:
		case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		case MONO_MARSHAL_CONV_STR_TBSTR:
		case MONO_MARSHAL_CONV_STR_UTF8STR:
			mono_marshal_free (*(gpointer *)cpos);
			break;

		default:
			continue;
		}
	}
}

void
ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure (gpointer src, MonoReflectionType *type)
{
	MonoClass *klass;

	MONO_CHECK_ARG_NULL (src,);
	MONO_CHECK_ARG_NULL (type,);

	klass = mono_class_from_mono_type (type->type);
	if (!mono_class_init (klass)) {
		mono_set_pending_exception (mono_class_get_exception_for_failure (klass));
		return;
	}

	mono_struct_delete_old (klass, (char *)src);
}

#ifndef HOST_WIN32
static inline void *
mono_marshal_alloc_hglobal (size_t size)
{
	return g_try_malloc (size);
}
#endif

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (gpointer size)
{
	gpointer res;
	size_t s = (size_t)size;

	if (s == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		s = 4;

	res = mono_marshal_alloc_hglobal (s);

	if (!res) {
		mono_set_pending_exception (mono_domain_get ()->out_of_memory_ex);
		return NULL;
	}

	return res;
}

#ifndef HOST_WIN32
static inline gpointer
mono_marshal_realloc_hglobal (gpointer ptr, size_t size)
{
	return g_try_realloc (ptr, size);
}
#endif

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal (gpointer ptr, gpointer size)
{
	gpointer res;
	size_t s = (size_t)size;

	if (ptr == NULL) {
		mono_set_pending_exception (mono_domain_get ()->out_of_memory_ex);
		return NULL;
	}

	res = mono_marshal_realloc_hglobal (ptr, s);

	if (!res) {
		mono_set_pending_exception (mono_domain_get ()->out_of_memory_ex);
		return NULL;
	}

	return res;
}

#ifndef HOST_WIN32
static inline void
mono_marshal_free_hglobal (gpointer ptr)
{
	g_free (ptr);
	return;
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
		mono_set_pending_exception (mono_domain_get ()->out_of_memory_ex);
		return NULL;
	}
	return res;
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMemSize (gulong size)
{
	void *res = mono_marshal_alloc_co_task_mem (size);

	if (!res) {
		mono_set_pending_exception (mono_domain_get ()->out_of_memory_ex);
		return NULL;
	}
	return res;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr)
{
	mono_marshal_free_co_task_mem (ptr);
	return;
}

#ifndef HOST_WIN32
static inline gpointer
mono_marshal_realloc_co_task_mem (gpointer ptr, size_t size)
{
	return g_try_realloc (ptr, (gulong)size);
}
#endif

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem (gpointer ptr, int size)
{
	void *res = mono_marshal_realloc_co_task_mem (ptr, size);

	if (!res) {
		mono_set_pending_exception (mono_domain_get ()->out_of_memory_ex);
		return NULL;
	}
	return res;
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement (MonoArray *arrayobj, int index)
{
	return mono_array_addr_with_size_fast (arrayobj, mono_array_element_size (arrayobj->obj.vtable->klass), index);
}

MonoDelegateHandle
ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal (void *ftn, MonoReflectionTypeHandle type, MonoError *error)
{
	error_init (error);
	MonoClass *klass = mono_type_get_class (MONO_HANDLE_GETVAL (type, type));
	if (!mono_class_init (klass)) {
		mono_error_set_for_class_failure (error, klass);
		return NULL;
	}

	return mono_ftnptr_to_delegate_handle (klass, ftn, error);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_GetFunctionPointerForDelegateInternal (MonoDelegateHandle delegate, MonoError *error)
{
	error_init (error);
	return mono_delegate_handle_to_ftnptr (delegate, error);
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

	if (!klass->inited)
		mono_class_init (klass);

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
	while ((field = mono_class_get_fields (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		count++;
	}

	layout = mono_class_get_flags (klass) & TYPE_ATTRIBUTE_LAYOUT_MASK;

	info = (MonoMarshalType *)mono_image_alloc0 (klass->image, MONO_SIZEOF_MARSHAL_TYPE + sizeof (MonoMarshalField) * count);
	info->num_fields = count;
	
	/* Try to find a size for this type in metadata */
	mono_metadata_packing_from_typedef (klass->image, klass->type_token, NULL, &native_size);

	if (klass->parent) {
		int parent_size = mono_class_native_size (klass->parent, NULL);

		/* Add parent size to real size */
		native_size += parent_size;
		info->native_size = parent_size;
	}

	packing = klass->packing_size ? klass->packing_size : 8;
	iter = NULL;
	j = 0;
	while ((field = mono_class_get_fields (klass, &iter))) {
		int size;
		guint32 align;
		
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		if (mono_field_is_deleted (field))
			continue;
		if (field->type->attrs & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL)
			mono_metadata_field_info_with_mempool (klass->image, mono_metadata_token_index (mono_class_get_field_token (field)) - 1, 
						  NULL, NULL, &info->fields [j].mspec);

		info->fields [j].field = field;

		if ((mono_class_num_fields (klass) == 1) && (klass->instance_size == sizeof (MonoObject)) &&
			(strcmp (mono_field_get_name (field), "$PRIVATE$") == 0)) {
			/* This field is a hack inserted by MCS to empty structures */
			continue;
		}

		switch (layout) {
		case TYPE_ATTRIBUTE_AUTO_LAYOUT:
		case TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT:
			size = mono_marshal_type_size (field->type, info->fields [j].mspec, 
						       &align, TRUE, klass->unicode);
			align = klass->packing_size ? MIN (klass->packing_size, align): align;
			min_align = MAX (align, min_align);
			info->fields [j].offset = info->native_size;
			info->fields [j].offset += align - 1;
			info->fields [j].offset &= ~(align - 1);
			info->native_size = info->fields [j].offset + size;
			break;
		case TYPE_ATTRIBUTE_EXPLICIT_LAYOUT:
			size = mono_marshal_type_size (field->type, info->fields [j].mspec, 
						       &align, TRUE, klass->unicode);
			min_align = MAX (align, min_align);
			info->fields [j].offset = field->offset - sizeof (MonoObject);
			info->native_size = MAX (info->native_size, info->fields [j].offset + size);
			break;
		}	
		j++;
	}

	if (klass->byval_arg.type == MONO_TYPE_PTR)
		info->native_size = sizeof (gpointer);

	if (layout != TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		info->native_size = MAX (native_size, info->native_size);
		/*
		 * If the provided Size is equal or larger than the calculated size, and there
		 * was no Pack attribute, we set min_align to 1 to avoid native_size being increased
		 */
		if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) {
			if (native_size && native_size == info->native_size && klass->packing_size == 0)
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
		mono_loader_lock ();
		klass->blittable = FALSE;
		mono_loader_unlock ();
	}

	/* If this is an array type, ensure that we have element info */
	if (klass->rank && !mono_marshal_is_loading_type_info (klass->element_class)) {
		mono_marshal_load_type_info (klass->element_class);
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
		*align = sizeof (gpointer);
		return sizeof (gpointer);
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
		*align = sizeof (gpointer);
		return sizeof (gpointer);
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
			*align = sizeof (gpointer);
			return sizeof (gpointer);
		} 
		/* Fall through */
	case MONO_TYPE_TYPEDBYREF:
	case MONO_TYPE_VALUETYPE: {
		guint32 size;
		MonoClass *klass = mono_class_from_mono_type (t);

		if (klass->enumtype)
			return mono_type_native_stack_size (mono_class_enum_basetype (klass), align);
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
	MonoMarshalNative native_type = mono_type_to_unmanaged (type, mspec, as_field, unicode, NULL);
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
		return sizeof (gpointer);
	case MONO_NATIVE_STRUCT: 
		klass = mono_class_from_mono_type (type);
		if (klass == mono_defaults.object_class &&
			(mspec && mspec->native == MONO_NATIVE_STRUCT)) {
		*align = 16;
		return 16;
		}
		return mono_class_native_size (klass, align);
	case MONO_NATIVE_BYVALTSTR: {
		int esize = unicode ? 2: 1;
		g_assert (mspec);
		*align = esize;
		return mspec->data.array_data.num_elem * esize;
	}
	case MONO_NATIVE_BYVALARRAY: {
		// FIXME: Have to consider ArraySubType
		int esize;
		klass = mono_class_from_mono_type (type);
		if (klass->element_class == mono_defaults.char_class) {
			esize = unicode ? 2 : 1;
			*align = esize;
		} else {
			esize = mono_class_native_size (klass->element_class, align);
		}
		g_assert (mspec);
		return mspec->data.array_data.num_elem * esize;
	}
	case MONO_NATIVE_CUSTOM:
		*align = sizeof (gpointer);
		return sizeof (gpointer);
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
 * This is a JIT icall, it sets the pending exception and returns NULL on error.
 */
gpointer
mono_marshal_asany (MonoObject *o, MonoMarshalNative string_encoding, int param_attrs)
{
	MonoError error;
	MonoType *t;
	MonoClass *klass;

	if (o == NULL)
		return NULL;

	t = &o->vtable->klass->byval_arg;
	switch (t->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_PTR:
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
		return mono_object_unbox (o);
		break;
	case MONO_TYPE_STRING:
		switch (string_encoding) {
		case MONO_NATIVE_LPWSTR:
			return mono_marshal_string_to_utf16_copy ((MonoString*)o);
		case MONO_NATIVE_LPSTR:
		case MONO_NATIVE_UTF8STR:
			// Same code path, because in Mono, we treated strings as Utf8
			return mono_string_to_utf8str ((MonoString*)o);
		default:
			g_warning ("marshaling conversion %d not implemented", string_encoding);
			g_assert_not_reached ();
		}
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoMethod *method;
		gpointer pa [3];
		gpointer res;
		MonoBoolean delete_old = FALSE;

		klass = t->data.klass;

		if (mono_class_is_auto_layout (klass))
			break;

		if (klass->valuetype && (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype))
			return mono_object_unbox (o);

		res = mono_marshal_alloc (mono_class_native_size (klass, NULL), &error);
		if (!mono_error_ok (&error)) {
			mono_error_set_pending_exception (&error);
			return NULL;
		}

		if (!((param_attrs & PARAM_ATTRIBUTE_OUT) && !(param_attrs & PARAM_ATTRIBUTE_IN))) {
			method = mono_marshal_get_struct_to_ptr (o->vtable->klass);

			pa [0] = o;
			pa [1] = &res;
			pa [2] = &delete_old;

			mono_runtime_invoke_checked (method, NULL, pa, &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return NULL;
			}
		}

		return res;
	}
	default:
		break;
	}
	mono_set_pending_exception (mono_get_exception_argument ("", "No PInvoke conversion exists for value passed to Object-typed parameter."));
	return NULL;
}

/**
 * mono_marshal_free_asany:
 * This is a JIT icall, it sets the pending exception
 */
void
mono_marshal_free_asany (MonoObject *o, gpointer ptr, MonoMarshalNative string_encoding, int param_attrs)
{
	MonoError error;
	MonoType *t;
	MonoClass *klass;

	if (o == NULL)
		return;

	t = &o->vtable->klass->byval_arg;
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

		if (klass->valuetype && (mono_class_is_explicit_layout (klass) || klass->blittable || klass->enumtype))
			break;

		if (param_attrs & PARAM_ATTRIBUTE_OUT) {
			MonoMethod *method = mono_marshal_get_ptr_to_struct (o->vtable->klass);
			gpointer pa [2];

			pa [0] = &ptr;
			pa [1] = o;

			mono_runtime_invoke_checked (method, NULL, pa, &error);
			if (!mono_error_ok (&error)) {
				mono_error_set_pending_exception (&error);
				return;
			}
		}

		if (!((param_attrs & PARAM_ATTRIBUTE_OUT) && !(param_attrs & PARAM_ATTRIBUTE_IN))) {
			mono_struct_delete_old (klass, (char *)ptr);
		}

		mono_marshal_free (ptr);
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
mono_marshal_get_generic_array_helper (MonoClass *klass, gchar *name, MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	WrapperInfo *info;
	int i;

	mb = mono_mb_new_no_dup_name (klass, name, MONO_WRAPPER_MANAGED_TO_MANAGED);
	mb->method->slot = -1;

	mb->method->flags = METHOD_ATTRIBUTE_PRIVATE | METHOD_ATTRIBUTE_VIRTUAL |
		METHOD_ATTRIBUTE_NEW_SLOT | METHOD_ATTRIBUTE_HIDE_BY_SIG | METHOD_ATTRIBUTE_FINAL;

	sig = mono_method_signature (method);
	csig = mono_metadata_signature_dup_full (method->klass->image, sig);
	csig->generic_param_count = 0;

#ifdef ENABLE_ILGEN
	mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < csig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);

	/* We can corlib internal methods */
	mb->skip_visibility = TRUE;
#endif
	info = mono_wrapper_info_create (mb, WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER);
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
	MonoExceptionClause *clause;
	MonoImage *image;
	MonoClass *klass;
	GHashTable *cache;
	MonoMethod *res;
	int i, param_count, sig_size, pos_leave;

	g_assert (method);

	// FIXME: we need to store the exception into a MonoHandle
	g_assert (!mono_threads_is_coop_enabled ());

	klass = method->klass;
	image = method->klass->image;

	cache = get_cache (&mono_method_get_wrapper_cache (method)->thunk_invoke_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = mono_method_signature (method);
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
		csig->params [0] = &klass->byval_arg;
		/* move params up by one */
		for (i = 0; i < sig->param_count; i++)
			csig->params [i + 1] = sig->params [i];
	}

	/* setup exception param as byref+[out] */
	csig->params [param_count - 1] = mono_metadata_type_dup (image,
		 &mono_defaults.exception_class->byval_arg);
	csig->params [param_count - 1]->byref = 1;
	csig->params [param_count - 1]->attrs = PARAM_ATTRIBUTE_OUT;

	/* convert struct return to object */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		csig->ret = &mono_defaults.object_class->byval_arg;

#ifdef ENABLE_ILGEN
	/* local 0 (temp for exception object) */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* local 1 (temp for result) */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_add_local (mb, sig->ret);

	/* clear exception arg */
	mono_mb_emit_ldarg (mb, param_count - 1);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	/* try */
	clause = (MonoExceptionClause *)mono_image_alloc0 (image, sizeof (MonoExceptionClause));
	clause->try_offset = mono_mb_get_label (mb);

	/* push method's args */
	for (i = 0; i < param_count - 1; i++) {
		MonoType *type;
		MonoClass *klass;

		mono_mb_emit_ldarg (mb, i);

		/* get the byval type of the param */
		klass = mono_class_from_mono_type (csig->params [i]);
		type = &klass->byval_arg;

		/* unbox struct args */
		if (MONO_TYPE_ISSTRUCT (type)) {
			mono_mb_emit_op (mb, CEE_UNBOX, klass);

			/* byref args & and the "this" arg must remain a ptr.
			   Otherwise make a copy of the value type */
			if (!(csig->params [i]->byref || (i == 0 && sig->hasthis)))
				mono_mb_emit_op (mb, CEE_LDOBJ, klass);

			csig->params [i] = &mono_defaults.object_class->byval_arg;
		}
	}

	/* call */
	if (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	else
		mono_mb_emit_op (mb, CEE_CALL, method);

	/* save result at local 1 */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, 1);

	pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* catch */
	clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
	clause->data.catch_class = mono_defaults.object_class;

	clause->handler_offset = mono_mb_get_label (mb);

	/* store exception at local 0 */
	mono_mb_emit_stloc (mb, 0);
	mono_mb_emit_ldarg (mb, param_count - 1);
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	mono_mb_patch_branch (mb, pos_leave);
	/* end-try */

	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		mono_mb_emit_ldloc (mb, 1);

		/* box the return value */
		if (MONO_TYPE_ISSTRUCT (sig->ret))
			mono_mb_emit_op (mb, CEE_BOX, mono_class_from_mono_type (sig->ret));
	}

	mono_mb_emit_byte (mb, CEE_RET);
#endif

	res = mono_mb_create_and_cache (cache, method, mb, csig, param_count + 16);
	mono_mb_free (mb);

	return res;
}

/*
 * mono_marshal_free_dynamic_wrappers:
 *
 *   Free wrappers of the dynamic method METHOD.
 */
void
mono_marshal_free_dynamic_wrappers (MonoMethod *method)
{
	MonoImage *image = method->klass->image;

	g_assert (method_is_dynamic (method));

	/* This could be called during shutdown */
	if (marshal_mutex_initialized)
		mono_marshal_lock ();
	/* 
	 * FIXME: We currently leak the wrappers. Freeing them would be tricky as
	 * they could be shared with other methods ?
	 */
	if (image->wrapper_caches.runtime_invoke_direct_cache)
		g_hash_table_remove (image->wrapper_caches.runtime_invoke_direct_cache, method);
	if (image->wrapper_caches.delegate_abstract_invoke_cache)
		g_hash_table_foreach_remove (image->wrapper_caches.delegate_abstract_invoke_cache, signature_pointer_pair_matches_pointer, method);
	// FIXME: Need to clear the caches in other images as well
	if (image->delegate_bound_static_invoke_cache)
		g_hash_table_remove (image->delegate_bound_static_invoke_cache, mono_method_signature (method));

	if (marshal_mutex_initialized)
		mono_marshal_unlock ();
}

static void
mono_marshal_ftnptr_eh_callback (guint32 gchandle)
{
	g_assert (ftnptr_eh_callback);
	ftnptr_eh_callback (gchandle);
}

static void
ftnptr_eh_callback_default (guint32 gchandle)
{
	MonoException *exc;
	gpointer stackdata;

	mono_threads_enter_gc_unsafe_region_unbalanced (&stackdata);

	exc = (MonoException*) mono_gchandle_get_target (gchandle);

	mono_gchandle_free (gchandle);

	mono_reraise_exception_deprecated (exc);
}

/*
 * mono_install_ftnptr_eh_callback:
 *
 *   Install a callback that should be called when there is a managed exception
 *   in a native-to-managed wrapper. This is mainly used by iOS to convert a
 *   managed exception to a native exception, to properly unwind the native
 *   stack; this native exception will then be converted back to a managed
 *   exception in their managed-to-native wrapper.
 */
void
mono_install_ftnptr_eh_callback (MonoFtnPtrEHCallback callback)
{
	ftnptr_eh_callback = callback;
}

static MonoThreadInfo*
mono_icall_start (HandleStackMark *stackmark, MonoError *error)
{
	MonoThreadInfo *info = mono_thread_info_current ();

	mono_stack_mark_init (info, stackmark);
	error_init (error);
	return info;
}

static void
mono_icall_end (MonoThreadInfo *info, HandleStackMark *stackmark, MonoError *error)
{
	mono_stack_mark_pop (info, stackmark);
	if (G_UNLIKELY (!is_ok (error)))
		mono_error_set_pending_exception (error);
}

static MonoObjectHandle
mono_icall_handle_new (gpointer rawobj)
{
#ifdef MONO_HANDLE_TRACK_OWNER
	return mono_handle_new (rawobj, "<marshal args>");
#else
	return mono_handle_new (rawobj);
#endif
}

static MonoObjectHandle
mono_icall_handle_new_interior (gpointer rawobj)
{
#ifdef MONO_HANDLE_TRACK_OWNER
	return mono_handle_new_interior (rawobj, "<marshal args>");
#else
	return mono_handle_new_interior (rawobj);
#endif
}
