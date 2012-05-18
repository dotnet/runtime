/*
 * marshal.c: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * Copyright 2002-2003 Ximian, Inc (http://www.ximian.com)
 * Copyright 2004-2009 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 *
 */

#include "config.h"
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include "object.h"
#include "loader.h"
#include "cil-coff.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/tabledefs.h"
#include "metadata/exception.h"
#include "metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threadpool.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internal.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/metadata/gc-internal.h"
#include "mono/metadata/cominterop.h"
#include "mono/utils/mono-counters.h"
#include "mono/utils/mono-tls.h"
#include "mono/utils/mono-memory-model.h"
#include <string.h>
#include <errno.h>

/* #define DEBUG_RUNTIME_CODE */

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

typedef enum {
	MONO_MARSHAL_NONE,			/* No marshalling needed */
	MONO_MARSHAL_COPY,			/* Can be copied by value to the new domain */
	MONO_MARSHAL_COPY_OUT,		/* out parameter that needs to be copied back to the original instance */
	MONO_MARSHAL_SERIALIZE		/* Value needs to be serialized into the new domain */
} MonoXDomainMarshalType;

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

struct _MonoRemotingMethods {
	MonoMethod *invoke;
	MonoMethod *invoke_with_check;
	MonoMethod *xdomain_invoke;
	MonoMethod *xdomain_dispatch;
};

typedef struct _MonoRemotingMethods MonoRemotingMethods;

/* 
 * This mutex protects the various marshalling related caches in MonoImage
 * and a few other data structures static to this file.
 * Note that when this lock is held it is not possible to take other runtime
 * locks like the loader lock.
 */
#define mono_marshal_lock() EnterCriticalSection (&marshal_mutex)
#define mono_marshal_unlock() LeaveCriticalSection (&marshal_mutex)
static CRITICAL_SECTION marshal_mutex;
static gboolean marshal_mutex_initialized;

static MonoNativeTlsKey last_error_tls_id;

static MonoNativeTlsKey load_type_info_tls_id;

static gboolean use_aot_wrappers;

static void
delegate_hash_table_add (MonoDelegate *d);

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

static void
emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object, MonoMarshalNative string_encoding);

static void 
mono_struct_delete_old (MonoClass *klass, char *ptr);

void *
mono_marshal_string_to_utf16 (MonoString *s);

static void *
mono_marshal_string_to_utf16_copy (MonoString *s);

static gpointer
mono_string_to_lpstr (MonoString *string_obj);

static MonoStringBuilder *
mono_string_utf8_to_builder2 (char *text);

static MonoStringBuilder *
mono_string_utf16_to_builder2 (gunichar2 *text);

static MonoString*
mono_string_new_len_wrapper (const char *text, guint length);

static MonoString *
mono_string_from_byvalwstr (gunichar2 *data, int len);

static void
mono_byvalarray_to_array (MonoArray *arr, gpointer native_arr, MonoClass *eltype, guint32 elnum);

static void
mono_array_to_byvalarray (gpointer native_arr, MonoArray *arr, MonoClass *eltype, guint32 elnum);

static MonoObject *
mono_remoting_wrapper (MonoMethod *method, gpointer *params);

static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params);

static MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params);

static void
mono_marshal_xdomain_copy_out_value (MonoObject *src, MonoObject *dst);

static gint32
mono_marshal_set_domain_by_id (gint32 id, MonoBoolean push);

static gboolean
mono_marshal_check_domain_image (gint32 domain_id, MonoImage *image);

void
mono_upgrade_remote_class_wrapper (MonoReflectionType *rtype, MonoTransparentProxy *tproxy);

static MonoReflectionType *
type_from_handle (MonoType *handle);

static void
mono_marshal_set_last_error_windows (int error);

static void init_safe_handle (void);

/* MonoMethod pointers to SafeHandle::DangerousAddRef and ::DangerousRelease */
static MonoMethod *sh_dangerous_add_ref;
static MonoMethod *sh_dangerous_release;


static void
init_safe_handle ()
{
	sh_dangerous_add_ref = mono_class_get_method_from_name (
		mono_defaults.safehandle_class, "DangerousAddRef", 1);
	sh_dangerous_release = mono_class_get_method_from_name (
		mono_defaults.safehandle_class, "DangerousRelease", 0);
}

static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean save)
{
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	mono_register_jit_icall (func, name, sig, save);
}

static MonoMethodSignature*
signature_dup (MonoImage *image, MonoMethodSignature *sig)
{
	MonoMethodSignature *res;
	int sigsize;

	res = mono_metadata_signature_alloc (image, sig->param_count);
	sigsize = MONO_SIZEOF_METHOD_SIGNATURE + sig->param_count * sizeof (MonoType *);
	memcpy (res, sig, sigsize);

	return res;
}

MonoMethodSignature*
mono_signature_no_pinvoke (MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature (method);
	if (sig->pinvoke) {
		sig = signature_dup (method->klass->image, sig);
		sig->pinvoke = FALSE;
	}
	
	return sig;
}

void
mono_marshal_init (void)
{
	static gboolean module_initialized = FALSE;

	if (!module_initialized) {
		module_initialized = TRUE;
		InitializeCriticalSection (&marshal_mutex);
		marshal_mutex_initialized = TRUE;
		mono_native_tls_alloc (&last_error_tls_id, NULL);
		mono_native_tls_alloc (&load_type_info_tls_id, NULL);

		register_icall (ves_icall_System_Threading_Thread_ResetAbort, "ves_icall_System_Threading_Thread_ResetAbort", "void", TRUE);
		register_icall (mono_marshal_string_to_utf16, "mono_marshal_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_marshal_string_to_utf16_copy, "mono_marshal_string_to_utf16_copy", "ptr obj", FALSE);
		register_icall (mono_string_to_utf16, "mono_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_string_from_utf16, "mono_string_from_utf16", "obj ptr", FALSE);
		register_icall (mono_string_from_byvalwstr, "mono_string_from_byvalwstr", "obj ptr int", FALSE);
		register_icall (mono_string_new_wrapper, "mono_string_new_wrapper", "obj ptr", FALSE);
		register_icall (mono_string_new_len_wrapper, "mono_string_new_len_wrapper", "obj ptr int", FALSE);
		register_icall (mono_string_to_utf8, "mono_string_to_utf8", "ptr obj", FALSE);
		register_icall (mono_string_to_lpstr, "mono_string_to_lpstr", "ptr obj", FALSE);
		register_icall (mono_string_to_ansibstr, "mono_string_to_ansibstr", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf8, "mono_string_builder_to_utf8", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf16, "mono_string_builder_to_utf16", "ptr object", FALSE);
		register_icall (mono_array_to_savearray, "mono_array_to_savearray", "ptr object", FALSE);
		register_icall (mono_array_to_lparray, "mono_array_to_lparray", "ptr object", FALSE);
		register_icall (mono_free_lparray, "mono_free_lparray", "void object ptr", FALSE);
		register_icall (mono_byvalarray_to_array, "mono_byvalarray_to_array", "void object ptr ptr int32", FALSE);
		register_icall (mono_array_to_byvalarray, "mono_array_to_byvalarray", "void ptr object ptr int32", FALSE);
		register_icall (mono_delegate_to_ftnptr, "mono_delegate_to_ftnptr", "ptr object", FALSE);
		register_icall (mono_ftnptr_to_delegate, "mono_ftnptr_to_delegate", "object ptr ptr", FALSE);
		register_icall (mono_marshal_asany, "mono_marshal_asany", "ptr object int32 int32", FALSE);
		register_icall (mono_marshal_free_asany, "mono_marshal_free_asany", "void object ptr int32 int32", FALSE);
		register_icall (mono_marshal_alloc, "mono_marshal_alloc", "ptr int32", FALSE);
		register_icall (mono_marshal_free, "mono_marshal_free", "void ptr", FALSE);
		register_icall (mono_marshal_set_last_error, "mono_marshal_set_last_error", "void", FALSE);
		register_icall (mono_marshal_set_last_error_windows, "mono_marshal_set_last_error_windows", "void int32", FALSE);
		register_icall (mono_string_utf8_to_builder, "mono_string_utf8_to_builder", "void ptr ptr", FALSE);
		register_icall (mono_string_utf8_to_builder2, "mono_string_utf8_to_builder2", "object ptr", FALSE);
		register_icall (mono_string_utf16_to_builder, "mono_string_utf16_to_builder", "void ptr ptr", FALSE);
		register_icall (mono_string_utf16_to_builder2, "mono_string_utf16_to_builder2", "object ptr", FALSE);
		register_icall (mono_marshal_free_array, "mono_marshal_free_array", "void ptr int32", FALSE);
		register_icall (mono_string_to_byvalstr, "mono_string_to_byvalstr", "void ptr ptr int32", FALSE);
		register_icall (mono_string_to_byvalwstr, "mono_string_to_byvalwstr", "void ptr ptr int32", FALSE);
		register_icall (g_free, "g_free", "void ptr", FALSE);
		register_icall (mono_object_isinst, "mono_object_isinst", "object object ptr", FALSE);
		register_icall (mono_struct_delete_old, "mono_struct_delete_old", "void ptr ptr", FALSE);
		register_icall (mono_remoting_wrapper, "mono_remoting_wrapper", "object ptr ptr", FALSE);
		register_icall (mono_delegate_begin_invoke, "mono_delegate_begin_invoke", "object object ptr", FALSE);
		register_icall (mono_delegate_end_invoke, "mono_delegate_end_invoke", "object object ptr", FALSE);
		register_icall (mono_marshal_xdomain_copy_value, "mono_marshal_xdomain_copy_value", "object object", FALSE);
		register_icall (mono_marshal_xdomain_copy_out_value, "mono_marshal_xdomain_copy_out_value", "void object object", FALSE);
		register_icall (mono_marshal_set_domain_by_id, "mono_marshal_set_domain_by_id", "int32 int32 int32", FALSE);
		register_icall (mono_marshal_check_domain_image, "mono_marshal_check_domain_image", "int32 int32 ptr", FALSE);
		register_icall (mono_compile_method, "mono_compile_method", "ptr ptr", FALSE);
		register_icall (mono_context_get, "mono_context_get", "object", FALSE);
		register_icall (mono_context_set, "mono_context_set", "void object", FALSE);
		register_icall (mono_upgrade_remote_class_wrapper, "mono_upgrade_remote_class_wrapper", "void object object", FALSE);
		register_icall (type_from_handle, "type_from_handle", "object ptr", FALSE);
		register_icall (mono_gc_wbarrier_generic_nostore, "wb_generic", "void ptr", FALSE);
		register_icall (mono_gchandle_get_target, "mono_gchandle_get_target", "object int32", TRUE);

		mono_cominterop_init ();
	}
}

void
mono_marshal_cleanup (void)
{
	mono_cominterop_cleanup ();

	mono_native_tls_free (load_type_info_tls_id);
	mono_native_tls_free (last_error_tls_id);
	DeleteCriticalSection (&marshal_mutex);
	marshal_mutex_initialized = FALSE;
}

MonoClass *byte_array_class;
static MonoMethod *method_rs_serialize, *method_rs_deserialize, *method_exc_fixexc, *method_rs_appdomain_target;
static MonoMethod *method_set_context, *method_get_context;
static MonoMethod *method_set_call_context, *method_needs_context_sink, *method_rs_serialize_exc;

static void
mono_remoting_marshal_init (void)
{
	MonoClass *klass;

	static gboolean module_initialized = FALSE;

	if (!module_initialized) {
		klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting", "RemotingServices");
		method_rs_serialize = mono_class_get_method_from_name (klass, "SerializeCallData", -1);
		method_rs_deserialize = mono_class_get_method_from_name (klass, "DeserializeCallData", -1);
		method_rs_serialize_exc = mono_class_get_method_from_name (klass, "SerializeExceptionData", -1);
	
		klass = mono_defaults.real_proxy_class;
		method_rs_appdomain_target = mono_class_get_method_from_name (klass, "GetAppDomainTarget", -1);
	
		klass = mono_defaults.exception_class;
		method_exc_fixexc = mono_class_get_method_from_name (klass, "FixRemotingException", -1);
	
		klass = mono_defaults.thread_class;
		method_get_context = mono_class_get_method_from_name (klass, "get_CurrentContext", -1);
	
		klass = mono_defaults.appdomain_class;
		method_set_context = mono_class_get_method_from_name (klass, "InternalSetContext", -1);
		byte_array_class = mono_array_class_get (mono_defaults.byte_class, 1);
	
		klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting.Messaging", "CallContext");
		method_set_call_context = mono_class_get_method_from_name (klass, "SetCurrentCallContext", -1);
	
		klass = mono_class_from_name (mono_defaults.corlib, "System.Runtime.Remoting.Contexts", "Context");
		method_needs_context_sink = mono_class_get_method_from_name (klass, "get_NeedsContextSink", -1);

		module_initialized = TRUE;
	}
}

gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate)
{
	MonoMethod *method, *wrapper;
	MonoClass *klass;
	uint32_t target_handle = 0;

	if (!delegate)
		return NULL;

	if (delegate->delegate_trampoline)
		return delegate->delegate_trampoline;

	klass = ((MonoObject *)delegate)->vtable->klass;
	g_assert (klass->delegate);

	method = delegate->method;

	if (mono_method_signature (method)->pinvoke) {
		const char *exc_class, *exc_arg;
		gpointer ftnptr;

		ftnptr = mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
		if (!ftnptr) {
			g_assert (exc_class);
			mono_raise_exception (mono_exception_from_name_msg (mono_defaults.corlib, "System", exc_class, exc_arg));
		}
		return ftnptr;
	}

	if (delegate->target) {
		/* Produce a location which can be embedded in JITted code */
		target_handle = mono_gchandle_new_weakref (delegate->target, FALSE);
	}

	wrapper = mono_marshal_get_managed_wrapper (method, klass, target_handle);

	delegate->delegate_trampoline = mono_compile_method (wrapper);

	// Add the delegate to the delegate hash table
	delegate_hash_table_add (delegate);

	/* when the object is collected, collect the dynamic method, too */
	mono_object_register_finalizer ((MonoObject*)delegate);

	return delegate->delegate_trampoline;
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
#ifdef HAVE_MOVING_COLLECTOR
	guint32 gchandle;
#endif
	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
#ifdef HAVE_MOVING_COLLECTOR
	gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (delegate_hash_table, d->delegate_trampoline));
#endif
	g_hash_table_remove (delegate_hash_table, d->delegate_trampoline);
	mono_marshal_unlock ();
#ifdef HAVE_MOVING_COLLECTOR
	mono_gchandle_free (gchandle);
#endif
}

static void
delegate_hash_table_add (MonoDelegate *d)
{
#ifdef HAVE_MOVING_COLLECTOR
	guint32 gchandle = mono_gchandle_new_weakref ((MonoObject*)d, FALSE);
	guint32 old_gchandle;
#endif
	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
#ifdef HAVE_MOVING_COLLECTOR
	old_gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (delegate_hash_table, d->delegate_trampoline));
	g_hash_table_insert (delegate_hash_table, d->delegate_trampoline, GUINT_TO_POINTER (gchandle));
	if (old_gchandle)
		mono_gchandle_free (old_gchandle);
#else
	g_hash_table_insert (delegate_hash_table, d->delegate_trampoline, d);
#endif
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
	static MonoClass *UnmanagedFunctionPointerAttribute;
	MonoCustomAttrInfo *cinfo;
	MonoReflectionUnmanagedFunctionPointerAttribute *attr;

	if (!UnmanagedFunctionPointerAttribute)
		UnmanagedFunctionPointerAttribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");

	/* The attribute is only available in Net 2.0 */
	if (UnmanagedFunctionPointerAttribute) {
		/* 
		 * The pinvoke attributes are stored in a real custom attribute so we have to
		 * construct it.
		 */
		cinfo = mono_custom_attrs_from_class (klass);
		if (cinfo) {
			attr = (MonoReflectionUnmanagedFunctionPointerAttribute*)mono_custom_attrs_get_attr (cinfo, UnmanagedFunctionPointerAttribute);
			if (attr) {
				piinfo->piflags = (attr->call_conv << 8) | (attr->charset ? (attr->charset - 1) * 2 : 1) | attr->set_last_error;
			}
			if (!cinfo->cached)
				mono_custom_attrs_free (cinfo);
		}
	}
}

MonoDelegate*
mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn)
{
#ifdef HAVE_MOVING_COLLECTOR
	guint32 gchandle;
#endif
	MonoDelegate *d;

	if (ftn == NULL)
		return NULL;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();

#ifdef HAVE_MOVING_COLLECTOR
	gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (delegate_hash_table, ftn));
	mono_marshal_unlock ();
	if (gchandle)
		d = (MonoDelegate*)mono_gchandle_get_target (gchandle);
	else
		d = NULL;
#else
	d = g_hash_table_lookup (delegate_hash_table, ftn);
	mono_marshal_unlock ();
#endif
	if (d == NULL) {
		/* This is a native function, so construct a delegate for it */
		MonoMethodSignature *sig;
		MonoMethod *wrapper;
		MonoMarshalSpec **mspecs;
		MonoMethod *invoke = mono_get_delegate_invoke (klass);
		MonoMethodPInvoke piinfo;
		MonoObject *this;
		int i;

		if (use_aot_wrappers) {
			wrapper = mono_marshal_get_native_func_wrapper_aot (klass);
			this = mono_value_box (mono_domain_get (), mono_defaults.int_class, &ftn);
		} else {
			memset (&piinfo, 0, sizeof (piinfo));
			parse_unmanaged_function_pointer_attr (klass, &piinfo);

			mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature (invoke)->param_count + 1);
			mono_method_get_marshal_info (invoke, mspecs);
			/* Freed below so don't alloc from mempool */
			sig = mono_metadata_signature_dup (mono_method_signature (invoke));
			sig->hasthis = 0;

			wrapper = mono_marshal_get_native_func_wrapper (klass->image, sig, &piinfo, mspecs, ftn);
			this = NULL;

			for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
				if (mspecs [i])
					mono_metadata_free_marshal_spec (mspecs [i]);
			g_free (mspecs);
			g_free (sig);
		}

		d = (MonoDelegate*)mono_object_new (mono_domain_get (), klass);
		mono_delegate_ctor_with_method ((MonoObject*)d, this, mono_compile_method (wrapper), wrapper);
	}

	if (d->object.vtable->domain != mono_domain_get ())
		mono_raise_exception (mono_get_exception_not_supported ("Delegates cannot be marshalled from native code into a domain other than their home domain"));

	return d;
}

void
mono_delegate_free_ftnptr (MonoDelegate *delegate)
{
	MonoJitInfo *ji;
	void *ptr;

	delegate_hash_table_remove (delegate);

	ptr = (gpointer)InterlockedExchangePointer (&delegate->delegate_trampoline, NULL);

	if (!delegate->target) {
		/* The wrapper method is shared between delegates -> no need to free it */
		return;
	}

	if (ptr) {
		uint32_t gchandle;
		void **method_data;
		ji = mono_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (ptr));
		g_assert (ji);

		method_data = ((MonoMethodWrapper*)ji->method)->method_data;

		/*the target gchandle is the first entry after size and the wrapper itself.*/
		gchandle = GPOINTER_TO_UINT (method_data [2]);

		if (gchandle)
			mono_gchandle_free (gchandle);

		mono_runtime_free_method (mono_object_domain (delegate), ji->method);
	}
}

static MonoString *
mono_string_from_byvalwstr (gunichar2 *data, int max_len)
{
	MonoDomain *domain = mono_domain_get ();
	int len = 0;

	if (!data)
		return NULL;

	while (data [len]) len++;

	return mono_string_new_utf16 (domain, data, MIN (len, max_len));
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
	gpointer *nativeArray = NULL;
	int nativeArraySize = 0;

	int i = 0;
	MonoClass *klass;

	if (!array)
		return NULL;
#ifndef DISABLE_COM

	klass = array->obj.vtable->klass;

	switch (klass->element_class->byval_arg.type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
#ifndef DISABLE_COM
	case MONO_TYPE_CLASS:
		nativeArraySize = array->max_length;
		nativeArray = malloc(sizeof(gpointer) * nativeArraySize);
		for(i = 0; i < nativeArraySize; ++i) 	
			nativeArray[i] = ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal(((gpointer*)array->vector)[i]);
		return nativeArray;
#endif
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
	int i = 0;

	if (!array)
		return;

	if (!nativeArray)
		return;
	klass = array->obj.vtable->klass;

	switch (klass->element_class->byval_arg.type) {
		case MONO_TYPE_CLASS:
			for(i = 0; i < array->max_length; ++i) 	
				mono_marshal_free_ccw(nativeArray[i]);
			free(nativeArray);
		break;
	}		
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

		ut = g_utf8_to_utf16 (native_arr, elnum, NULL, &items_written, &error);

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
mono_array_to_byvalarray (gpointer native_arr, MonoArray *arr, MonoClass *elclass, guint32 elnum)
{
	g_assert (arr->obj.vtable->klass->element_class == mono_defaults.char_class);

	if (elclass == mono_defaults.byte_class) {
		char *as;
		GError *error = NULL;

		as = g_utf16_to_utf8 (mono_array_addr (arr, gunichar2, 0), mono_array_length (arr), NULL, NULL, &error);
		if (error) {
			MonoException *exc = mono_get_exception_argument ("string", error->message);
			g_error_free (error);
			mono_raise_exception (exc);
		}

		memcpy (native_arr, as, MIN (strlen (as), elnum));
		g_free (as);
	} else {
		g_assert_not_reached ();
	}
}

void
mono_string_utf8_to_builder (MonoStringBuilder *sb, char *text)
{
	GError *error = NULL;
	guint16 *ut;
	glong items_written;
	int l;

	if (!sb || !text)
		return;

	l = strlen (text);

	ut = g_utf8_to_utf16 (text, l, NULL, &items_written, &error);
	
	if (items_written > mono_stringbuilder_capacity (sb))
		items_written = mono_stringbuilder_capacity (sb);
	
	if (!error) {
		if (! sb->str || sb->str == sb->cached_str) {
			MONO_OBJECT_SETREF (sb, str, mono_string_new_size (mono_domain_get (), items_written));
			sb->cached_str = NULL;
		}
		
		memcpy (mono_string_chars (sb->str), ut, items_written * 2);
		sb->length = items_written;
	} else 
		g_error_free (error);

	g_free (ut);
}

MonoStringBuilder *
mono_string_utf8_to_builder2 (char *text)
{
	int l;
	MonoStringBuilder *sb;
	static MonoClass *string_builder_class;
	static MonoMethod *sb_ctor;
	void *args [1];
	MonoObject *exc;

	if (!text)
		return NULL;

	if (!string_builder_class) {
		MonoMethodDesc *desc;

		string_builder_class = mono_class_from_name (mono_defaults.corlib, "System.Text", "StringBuilder");
		g_assert (string_builder_class);
		desc = mono_method_desc_new (":.ctor(int)", FALSE);
		sb_ctor = mono_method_desc_search_in_class (desc, string_builder_class);
		g_assert (sb_ctor);
		mono_method_desc_free (desc);
	}

	l = strlen (text);

	sb = (MonoStringBuilder*)mono_object_new (mono_domain_get (), string_builder_class);
	g_assert (sb);
	args [0] = &l;
	mono_runtime_invoke (sb_ctor, sb, args, &exc);
	g_assert (!exc);

	mono_string_utf8_to_builder (sb, text);

	return sb;
}

/*
 * FIXME: This routine does not seem to do what it seems to do
 * the @text is never copied into the string builder
 */
void
mono_string_utf16_to_builder (MonoStringBuilder *sb, gunichar2 *text)
{
	guint32 len;

	if (!sb || !text)
		return;

	g_assert (mono_string_chars (sb->str) == text);

	for (len = 0; text [len] != 0; ++len)
		;

	sb->length = len;
}

MonoStringBuilder *
mono_string_utf16_to_builder2 (gunichar2 *text)
{
	int len;
	MonoStringBuilder *sb;
	static MonoClass *string_builder_class;
	static MonoMethod *sb_ctor;
	void *args [1];
	MonoObject *exc;

	if (!text)
		return NULL;

	if (!string_builder_class) {
		MonoMethodDesc *desc;

		string_builder_class = mono_class_from_name (mono_defaults.corlib, "System.Text", "StringBuilder");
		g_assert (string_builder_class);
		desc = mono_method_desc_new (":.ctor(int)", FALSE);
		sb_ctor = mono_method_desc_search_in_class (desc, string_builder_class);
		g_assert (sb_ctor);
		mono_method_desc_free (desc);
	}

	for (len = 0; text [len] != 0; ++len)
		;

	sb = (MonoStringBuilder*)mono_object_new (mono_domain_get (), string_builder_class);
	g_assert (sb);
	args [0] = &len;
	mono_runtime_invoke (sb_ctor, sb, args, &exc);
	g_assert (!exc);

	sb->length = len;
	memcpy (mono_string_chars (sb->str), text, len * 2);

	return sb;
}

/**
 * mono_string_builder_to_utf8:
 * @sb: the string builder
 *
 * Converts to utf8 the contents of the MonoStringBuilder.
 *
 * Returns: a utf8 string with the contents of the StringBuilder.
 *
 * The return value must be released with g_free.
 */
gpointer
mono_string_builder_to_utf8 (MonoStringBuilder *sb)
{
	GError *error = NULL;
	gchar *tmp, *res = NULL;

	if (!sb)
		return NULL;

	if ((sb->str == sb->cached_str) && (sb->str->length == 0)) {
		/* 
		 * The sb could have been allocated with the default capacity and be empty.
		 * we need to alloc a buffer of the default capacity in this case.
		 */
		MONO_OBJECT_SETREF (sb, str, mono_string_new_size (mono_domain_get (), 16));
		sb->cached_str = NULL;
	}

	tmp = g_utf16_to_utf8 (mono_string_chars (sb->str), sb->length, NULL, NULL, &error);
	if (error) {
		g_error_free (error);
		mono_raise_exception (mono_get_exception_execution_engine ("Failed to convert StringBuilder from utf16 to utf8"));
	} else {
		res = mono_marshal_alloc (mono_stringbuilder_capacity (sb) + 1);
		memcpy (res, tmp, sb->length + 1);
		g_free (tmp);
	}

	return res;
}

/**
 * mono_string_builder_to_utf16:
 * @sb: the string builder
 *
 * Converts to utf16 the contents of the MonoStringBuilder.
 *
 * Returns: a utf16 string with the contents of the StringBuilder.
 *
 * The return value must not be freed.
 */
gpointer
mono_string_builder_to_utf16 (MonoStringBuilder *sb)
{
	if (!sb)
		return NULL;

	g_assert (sb->str);

	/*
	 * The stringbuilder might not have ownership of this string. If this is
	 * the case, we must duplicate the string, so that we don't munge immutable
	 * strings
	 */
	if (sb->str == sb->cached_str) {
		/* 
		 * The sb could have been allocated with the default capacity and be empty.
		 * we need to alloc a buffer of the default capacity in this case.
		 */
		if (sb->str->length == 0)
			MONO_OBJECT_SETREF (sb, str, mono_string_new_size (mono_domain_get (), 16));
		else
			MONO_OBJECT_SETREF (sb, str, mono_string_new_utf16 (mono_domain_get (), mono_string_chars (sb->str), mono_stringbuilder_capacity (sb)));
		sb->cached_str = NULL;
	}
	
	if (sb->length == 0)
		*(mono_string_chars (sb->str)) = '\0';

	return mono_string_chars (sb->str);
}

static gpointer
mono_string_to_lpstr (MonoString *s)
{
#ifdef TARGET_WIN32
	char *as, *tmp;
	glong len;
	GError *error = NULL;

	if (s == NULL)
		return NULL;

	if (!s->length) {
		as = CoTaskMemAlloc (1);
		as [0] = '\0';
		return as;
	}

	tmp = g_utf16_to_utf8 (mono_string_chars (s), s->length, NULL, &len, &error);
	if (error) {
		MonoException *exc = mono_get_exception_argument ("string", error->message);
		g_error_free (error);
		mono_raise_exception(exc);
		return NULL;
	} else {
		as = CoTaskMemAlloc (len + 1);
		memcpy (as, tmp, len + 1);
		g_free (tmp);
		return as;
	}
#else
	return mono_string_to_utf8 (s);
#endif
}	

gpointer
mono_string_to_ansibstr (MonoString *string_obj)
{
	g_error ("UnmanagedMarshal.BStr is not implemented.");
	return NULL;
}

/**
 * mono_string_to_byvalstr:
 * @dst: Where to store the null-terminated utf8 decoded string.
 * @src: the MonoString to copy.
 * @size: the maximum number of bytes to copy.
 *
 * Copies the MonoString pointed to by @src as a utf8 string
 * into @dst, it copies at most @size bytes into the destination.
 */
void
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size)
{
	char *s;
	int len;

	g_assert (dst != NULL);
	g_assert (size > 0);

	memset (dst, 0, size);
	if (!src)
		return;

	s = mono_string_to_utf8 (src);
	len = MIN (size, strlen (s));
	if (len >= size)
		len--;
	memcpy (dst, s, len);
	g_free (s);
}

/**
 * mono_string_to_byvalwstr:
 * @dst: Where to store the null-terminated utf16 decoded string.
 * @src: the MonoString to copy.
 * @size: the maximum number of bytes to copy.
 *
 * Copies the MonoString pointed to by @src as a utf16 string into
 * @dst, it copies at most @size bytes into the destination (including
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
	memcpy (dst, mono_string_chars (src), size * 2);
	if (size <= mono_string_length (src))
		len--;
	*((gunichar2 *) dst + len) = 0;
}

static MonoString*
mono_string_new_len_wrapper (const char *text, guint length)
{
	return mono_string_new_len (mono_domain_get (), text, length);
}

static int
mono_mb_emit_proxy_check (MonoMethodBuilder *mb, int branch_code)
{
	int pos;
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_CLASSCONST);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.transparent_proxy_class));
	pos = mono_mb_emit_branch (mb, branch_code);
	return pos;
}

static int
mono_mb_emit_xdomain_check (MonoMethodBuilder *mb, int branch_code)
{
	int pos;
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoRealProxy, target_domain_id));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_icon (mb, -1);
	pos = mono_mb_emit_branch (mb, branch_code);
	return pos;
}

static int
mono_mb_emit_contextbound_check (MonoMethodBuilder *mb, int branch_code)
{
	static int offset = -1;
	static guint8 mask;

	if (offset < 0)
		mono_marshal_find_bitfield_offset (MonoClass, contextbound, &offset, &mask);

	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, remote_class));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoRemoteClass, proxy_class));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_ldflda (mb, offset);
	mono_mb_emit_byte (mb, CEE_LDIND_U1);
	mono_mb_emit_icon (mb, mask);
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_icon (mb, 0);
	return mono_mb_emit_branch (mb, branch_code);
}

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
		return CEE_STIND_I;

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
		mono_mb_emit_byte (mb, CEE_STIND_I);

		if (eklass->blittable) {
			/* copy the elements */
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
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
		mono_mb_emit_ptr (mb, mono_defaults.byte_class);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_byvalarray_to_array);
		break;
	}
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icall (mb, mono_string_new_wrapper);
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
			mono_mb_emit_icall (mb, mono_string_from_utf16);
		}
		mono_mb_emit_byte (mb, CEE_STIND_REF);		
		break;		
	case MONO_MARSHAL_CONV_STR_LPTSTR:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
#ifdef TARGET_WIN32
		mono_mb_emit_icall (mb, mono_string_from_utf16);
#else
		mono_mb_emit_icall (mb, mono_string_new_wrapper);
#endif
		mono_mb_emit_byte (mb, CEE_STIND_REF);	
		break;
	case MONO_MARSHAL_CONV_STR_LPSTR:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, mono_string_new_wrapper);
		mono_mb_emit_byte (mb, CEE_STIND_REF);		
		break;
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, mono_string_from_utf16);
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
conv_to_icall (MonoMarshalConv conv)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_STR_LPWSTR:
		return mono_marshal_string_to_utf16;		
	case MONO_MARSHAL_CONV_LPWSTR_STR:
		return mono_string_from_utf16;
	case MONO_MARSHAL_CONV_LPTSTR_STR:
		return mono_string_new_wrapper;
	case MONO_MARSHAL_CONV_LPSTR_STR:
		return mono_string_new_wrapper;
	case MONO_MARSHAL_CONV_STR_LPTSTR:
#ifdef TARGET_WIN32
		return mono_marshal_string_to_utf16;
#else
		return mono_string_to_lpstr;
#endif
	case MONO_MARSHAL_CONV_STR_LPSTR:
		return mono_string_to_lpstr;
	case MONO_MARSHAL_CONV_STR_BSTR:
		return mono_string_to_bstr;
	case MONO_MARSHAL_CONV_BSTR_STR:
		return mono_string_from_bstr;
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		return mono_string_to_ansibstr;
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
		return mono_ftnptr_to_delegate;
	case MONO_MARSHAL_CONV_LPSTR_SB:
		return mono_string_utf8_to_builder;
	case MONO_MARSHAL_CONV_LPTSTR_SB:
#ifdef TARGET_WIN32
		return mono_string_utf16_to_builder;
#else
		return mono_string_utf8_to_builder;
#endif
	case MONO_MARSHAL_CONV_LPWSTR_SB:
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
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_byte (mb, CEE_STIND_I);	
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_DEL_FTN:
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_byte (mb, CEE_STIND_I);	
		break;
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: {
		g_assert (mspec);

		mono_mb_emit_ldloc (mb, 1); /* dst */
		mono_mb_emit_ldloc (mb, 0);	
		mono_mb_emit_byte (mb, CEE_LDIND_REF); /* src String */
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
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
			mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoArray, vector));
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
		mono_mb_emit_ptr (mb, mono_defaults.byte_class);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_array_to_byvalarray);
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
		int dar_release_slot, pos;
		
		dar_release_slot = mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

		/*
		 * The following is ifdefed-out, because I have no way of doing the
		 * DangerousRelease when destroying the structure
		 */
#if 0
		/* set release = false */
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_stloc (mb, dar_release_slot);
		if (!sh_dangerous_add_ref)
			init_safe_handle ();

		/* safehandle.DangerousAddRef (ref release) */
		mono_mb_emit_ldloc (mb, 0); /* the source */
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldloc_addr (mb, dar_release_slot);
		mono_mb_emit_managed_call (mb, sh_dangerous_add_ref, NULL);
#endif
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_exception (mb, "ArgumentNullException", NULL);
		mono_mb_patch_branch (mb, pos);
		
		/* Pull the handle field from SafeHandle */
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoSafeHandle, handle));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}

	case MONO_MARSHAL_CONV_HANDLEREF: {
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoHandleRef, handle));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		break;
	}
		
	default: {
		char *msg = g_strdup_printf ("marshalling conversion %d not implemented", conv);
		MonoException *exc = mono_get_exception_not_implemented (msg);
		g_warning ("%s", msg);
		g_free (msg);
		mono_raise_exception (exc);
	}
	}
}

static void
emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object,
					   MonoMarshalNative string_encoding)
{
	MonoMarshalType *info;
	int i;

	if (klass->parent)
		emit_struct_conv(mb, klass->parent, to_object);

	info = mono_marshal_load_type_info (klass);

	if (info->native_size == 0)
		return;

	if (klass->blittable) {
		int msize = mono_class_value_size (klass, NULL);
		g_assert (msize == info->native_size);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icon (mb, msize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);

		mono_mb_emit_add_to_local (mb, 0, msize);
		mono_mb_emit_add_to_local (mb, 1, msize);
		return;
	}

	if (klass != mono_defaults.safehandle_class) {
		if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
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

		ntype = mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, klass->unicode, &conv);

		if (last_field) {
			msize = klass->instance_size - info->fields [i].field->offset;
			usize = info->native_size - info->fields [i].offset;
		} else {
			msize = info->fields [i + 1].field->offset - info->fields [i].field->offset;
			usize = info->fields [i + 1].offset - info->fields [i].offset;
		}

		if (klass != mono_defaults.safehandle_class){
			/* 
			 * FIXME: Should really check for usize==0 and msize>0, but we apply 
			 * the layout to the managed structure as well.
			 */
			
			if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) && (usize == 0)) {
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

			if (ftype->byref || ftype->type == MONO_TYPE_I ||
			    ftype->type == MONO_TYPE_U) {
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

				emit_struct_conv (mb, ftype->data.klass, to_object);

				/* restore the old src pointer */
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_stloc (mb, 0);
				/* restore the old dst pointer */
				mono_mb_emit_ldloc (mb, dst_var);
				mono_mb_emit_stloc (mb, 1);
				break;
			}
			case MONO_TYPE_OBJECT: {
				mono_init_com_types ();
				if (to_object) {
					static MonoMethod *variant_clear = NULL;
					static MonoMethod *get_object_for_native_variant = NULL;

					if (!variant_clear)
						variant_clear = mono_class_get_method_from_name (mono_defaults.variant_class, "Clear", 0);
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
	emit_struct_conv_full (mb, klass, to_object, -1);
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
	int pos_noabort;

	mono_mb_emit_ptr (mb, (gpointer) mono_thread_interruption_request_flag ());
	mono_mb_emit_byte (mb, CEE_LDIND_U4);
	pos_noabort = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	mono_mb_emit_icall (mb, checkpoint_func);
	
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
	emit_thread_interrupt_checkpoint_call (mb, mono_thread_force_interruption_checkpoint);
}

void
mono_marshal_emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb)
{
	emit_thread_interrupt_checkpoint (mb);
}

static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoDelegate *async_callback;
	MonoMulticastDelegate *mcast_delegate;
	MonoObject *state;
	MonoMethod *im;
	MonoClass *klass;
	MonoMethod *method = NULL, *method2 = NULL;

	g_assert (delegate);
	mcast_delegate = (MonoMulticastDelegate *) delegate;
	if (mcast_delegate->prev != NULL)
		mono_raise_exception (mono_get_exception_argument (NULL, "The delegate must have only one target"));

	if (delegate->target && mono_object_class (delegate->target) == mono_defaults.transparent_proxy_class) {

		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		if (!tp->remote_class->proxy_class->contextbound || tp->rp->context != (MonoObject *) mono_context_get ()) {

			/* If the target is a proxy, make a direct call. Is proxy's work
			// to make the call asynchronous.
			*/
			MonoAsyncResult *ares;
			MonoObject *exc;
			MonoArray *out_args;
			method = delegate->method;

			msg = mono_method_call_message_new (mono_marshal_method_from_wrapper (method), params, NULL, &async_callback, &state);
			ares = mono_async_result_new (mono_domain_get (), NULL, state, NULL, NULL);
			MONO_OBJECT_SETREF (ares, async_delegate, (MonoObject *)delegate);
			MONO_OBJECT_SETREF (ares, async_callback, (MonoObject *)async_callback);
			MONO_OBJECT_SETREF (msg, async_result, ares);
			msg->call_type = CallType_BeginInvoke;

			exc = NULL;
			mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args);
			if (exc)
				mono_raise_exception ((MonoException *) exc);
			return ares;
		}
	}

	klass = delegate->object.vtable->klass;

	method = mono_get_delegate_invoke (klass);
	method2 = mono_class_get_method_from_name (klass, "BeginInvoke", -1);
	if (method2)
		method = method2;
	g_assert (method != NULL);

	im = mono_get_delegate_invoke (method->klass);
	msg = mono_method_call_message_new (method, params, im, &async_callback, &state);

	return mono_thread_pool_add ((MonoObject *)delegate, msg, async_callback, state);
}

static int
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
	default:
		return -1;
	}
}

static MonoMarshalConv
mono_marshal_get_stringbuilder_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec)
{
	MonoMarshalNative encoding = mono_marshal_get_string_encoding (piinfo, spec);

	switch (encoding) {
	case MONO_NATIVE_LPWSTR:
		return MONO_MARSHAL_CONV_SB_LPWSTR;
		break;
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_SB_LPSTR;
		break;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_SB_LPTSTR;
		break;
	default:
		return -1;
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
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_VBBYREFSTR:
		return MONO_MARSHAL_CONV_LPSTR_STR;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_LPTSTR_STR;
	case MONO_NATIVE_BSTR:
		return MONO_MARSHAL_CONV_BSTR_STR;
	default:
		return -1;
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
	case MONO_NATIVE_LPSTR:
		return MONO_MARSHAL_CONV_LPSTR_SB;
		break;
	case MONO_NATIVE_LPTSTR:
		return MONO_MARSHAL_CONV_LPTSTR_SB;
		break;
	default:
		return -1;
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
	MonoMarshalConv conv;

	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		/* FIXME: Optimize this */
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
		if (t->data.klass == mono_defaults.stringbuilder_class) {
			gboolean need_free;
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (piinfo, spec, &need_free);
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

static GHashTable*
get_cache_full (GHashTable **var, GHashFunc hash_func, GCompareFunc equal_func, GDestroyNotify key_destroy_func, GDestroyNotify value_destroy_func)
{
	if (!(*var)) {
		mono_marshal_lock ();
		if (!(*var)) {
			GHashTable *cache = 
				g_hash_table_new_full (hash_func, equal_func, key_destroy_func, value_destroy_func);
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
	res = g_hash_table_lookup (cache, key);
	mono_marshal_unlock ();
	return res;
}

/* Create the method from the builder and place it in the cache */
MonoMethod*
mono_mb_create_and_cache (GHashTable *cache, gpointer key,
							   MonoMethodBuilder *mb, MonoMethodSignature *sig,
							   int max_stack)
{
	MonoMethod *res;

	mono_marshal_lock ();
	res = g_hash_table_lookup (cache, key);
	mono_marshal_unlock ();
	if (!res) {
		MonoMethod *newm;
		newm = mono_mb_create_method (mb, sig, max_stack);
		mono_marshal_lock ();
		res = g_hash_table_lookup (cache, key);
		if (!res) {
			res = newm;
			g_hash_table_insert (cache, key, res);
			mono_marshal_set_wrapper_info (res, key);
			mono_marshal_unlock ();
		} else {
			mono_marshal_unlock ();
			mono_free_method (newm);
		}
	}

	return res;
}		


static inline MonoMethod*
mono_marshal_remoting_find_in_cache (MonoMethod *method, int wrapper_type)
{
	MonoMethod *res = NULL;
	MonoRemotingMethods *wrps;

	mono_marshal_lock ();
	if (method->klass->image->remoting_invoke_cache)
		wrps = g_hash_table_lookup (method->klass->image->remoting_invoke_cache, method);
	else
		wrps = NULL;

	if (wrps) {
		switch (wrapper_type) {
		case MONO_WRAPPER_REMOTING_INVOKE: res = wrps->invoke; break;
		case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: res = wrps->invoke_with_check; break;
		case MONO_WRAPPER_XDOMAIN_INVOKE: res = wrps->xdomain_invoke; break;
		case MONO_WRAPPER_XDOMAIN_DISPATCH: res = wrps->xdomain_dispatch; break;
		}
	}
	
	/* it is important to do the unlock after the load from wrps, since in
	 * mono_remoting_mb_create_and_cache () we drop the marshal lock to be able
	 * to take the loader lock and some other thread may set the fields.
	 */
	mono_marshal_unlock ();
	return res;
}

/* Create the method from the builder and place it in the cache */
static inline MonoMethod*
mono_remoting_mb_create_and_cache (MonoMethod *key, MonoMethodBuilder *mb, 
								MonoMethodSignature *sig, int max_stack)
{
	MonoMethod **res = NULL;
	MonoRemotingMethods *wrps;
	GHashTable *cache = get_cache_full (&key->klass->image->remoting_invoke_cache, mono_aligned_addr_hash, NULL, NULL, g_free);

	mono_marshal_lock ();
	wrps = g_hash_table_lookup (cache, key);
	if (!wrps) {
		wrps = g_new0 (MonoRemotingMethods, 1);
		g_hash_table_insert (cache, key, wrps);
	}

	switch (mb->method->wrapper_type) {
	case MONO_WRAPPER_REMOTING_INVOKE: res = &wrps->invoke; break;
	case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: res = &wrps->invoke_with_check; break;
	case MONO_WRAPPER_XDOMAIN_INVOKE: res = &wrps->xdomain_invoke; break;
	case MONO_WRAPPER_XDOMAIN_DISPATCH: res = &wrps->xdomain_dispatch; break;
	default: g_assert_not_reached (); break;
	}
	mono_marshal_unlock ();

	if (*res == NULL) {
		MonoMethod *newm;
		newm = mono_mb_create_method (mb, sig, max_stack);

		mono_marshal_lock ();
		if (!*res) {
			*res = newm;
			mono_marshal_set_wrapper_info (*res, key);
			mono_marshal_unlock ();
		} else {
			mono_marshal_unlock ();
			mono_free_method (newm);
		}
	}

	return *res;
}		

MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper)
{
	gpointer res;
	int wrapper_type = wrapper->wrapper_type;
	WrapperInfo *info;

	if (wrapper_type == MONO_WRAPPER_NONE || wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
		return wrapper;

	switch (wrapper_type) {
	case MONO_WRAPPER_REMOTING_INVOKE:
	case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK:
	case MONO_WRAPPER_XDOMAIN_INVOKE:
	case MONO_WRAPPER_SYNCHRONIZED:
		res = mono_marshal_get_wrapper_info (wrapper);
		if (res == NULL)
			return wrapper;
		return res;
	case MONO_WRAPPER_MANAGED_TO_NATIVE:
		info = mono_marshal_get_wrapper_info (wrapper);
		if (info && (info->subtype == WRAPPER_SUBTYPE_NONE || info->subtype == WRAPPER_SUBTYPE_NATIVE_FUNC_AOT))
			return info->d.managed_to_native.method;
		else
			return NULL;
	case MONO_WRAPPER_RUNTIME_INVOKE:
		info = mono_marshal_get_wrapper_info (wrapper);
		if (info && (info->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT || info->subtype == WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL))
			return info->d.runtime_invoke.method;
		else
			return NULL;
	default:
		return NULL;
	}
}

/*
 * mono_marshal_get_wrapper_info:
 *
 *   Retrieve the pointer stored by mono_marshal_set_wrapper_info. The type of data
 * returned depends on the wrapper type. It is usually a method, a class, or a
 * WrapperInfo structure.
 */
gpointer
mono_marshal_get_wrapper_info (MonoMethod *wrapper)
{
	g_assert (wrapper->wrapper_type);

	return mono_method_get_wrapper_data (wrapper, 1);
}

/*
 * mono_marshal_set_wrapper_info:
 *
 *   Store an arbitrary pointer inside the wrapper which is retrievable by 
 * mono_marshal_get_wrapper_info. The format of the data depends on the type of the
 * wrapper (method->wrapper_type).
 */
void
mono_marshal_set_wrapper_info (MonoMethod *method, gpointer data)
{
	void **datav;
	/* assert */
	if (method->wrapper_type == MONO_WRAPPER_NONE || method->wrapper_type == MONO_WRAPPER_DYNAMIC_METHOD)
		return;

	datav = ((MonoMethodWrapper *)method)->method_data;
	datav [1] = data;
}

static WrapperInfo*
mono_wrapper_info_create (MonoMethod *wrapper, WrapperSubtype subtype)
{
	WrapperInfo *info;

	info = mono_image_alloc0 (wrapper->klass->image, sizeof (WrapperInfo));
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
	if (image->dynamic)
		klass = ((MonoDynamicImage*)image)->wrappers_type;
	else
		klass = mono_class_get (image, mono_metadata_make_token (MONO_TABLE_TYPEDEF, 1));
	g_assert (klass);

	return klass;
}

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "BeginInvoke"));

	sig = mono_signature_no_pinvoke (method);

	cache = get_cache (&method->klass->image->delegate_begin_invoke_cache,
					   (GHashFunc)mono_signature_hash, 
					   (GCompareFunc)mono_metadata_signature_equal);
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	name = mono_signature_to_name (sig, "begin_invoke");
	mb = mono_mb_new (get_wrapper_target_class (method->klass->image), name, MONO_WRAPPER_DELEGATE_BEGIN_INVOKE);
	g_free (name);

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_begin_invoke);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, sig, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	return res;
}

static MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params)
{
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
		MONO_OBJECT_SETREF (delegate, method_info, mono_method_get_object (domain, delegate->method, NULL));
	}

	if (!delegate->method_info || !delegate->method_info->method)
		g_assert_not_reached ();

	klass = delegate->object.vtable->klass;

	method = mono_class_get_method_from_name (klass, "EndInvoke", -1);
	g_assert (method != NULL);

	sig = mono_signature_no_pinvoke (method);

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	if (ares == NULL) {
		mono_raise_exception (mono_exception_from_name_msg (mono_defaults.corlib, "System.Runtime.Remoting", "RemotingException", "The async result object is null or of an unexpected type."));
		return NULL;
	}

	if (ares->async_delegate != (MonoObject*)delegate) {
		mono_raise_exception (mono_get_exception_invalid_operation (
			"The IAsyncResult object provided does not match this delegate."));
		return NULL;
	}

	if (delegate->target && mono_object_class (delegate->target) == mono_defaults.transparent_proxy_class) {
		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
		mono_message_init (domain, msg, delegate->method_info, NULL);
		msg->call_type = CallType_EndInvoke;
		MONO_OBJECT_SETREF (msg, async_result, ares);
		res = mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args);
	} else {
		res = mono_thread_pool_finish (ares, &out_args, &exc);
	}

	if (exc) {
		if (((MonoException*)exc)->stack_trace) {
			char *strace = mono_string_to_utf8 (((MonoException*)exc)->stack_trace);
			char  *tmp;
			tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
			g_free (strace);	
			MONO_OBJECT_SETREF (((MonoException*)exc), stack_trace, mono_string_new (domain, tmp));
			g_free (tmp);
		}
		mono_raise_exception ((MonoException*)exc);
	}

	mono_method_return_message_restore (method, params, out_args);
	return res;
}

static void
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
	default:
		g_warning ("type 0x%x not handled", return_type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "EndInvoke"));

	sig = mono_signature_no_pinvoke (method);

	cache = get_cache (&method->klass->image->delegate_end_invoke_cache,
					   (GHashFunc)mono_signature_hash, 
					   (GCompareFunc)mono_metadata_signature_equal);
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	name = mono_signature_to_name (sig, "end_invoke");
	mb = mono_mb_new (get_wrapper_target_class (method->klass->image), name, MONO_WRAPPER_DELEGATE_END_INVOKE);
	g_free (name);

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_end_invoke);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else
		mono_mb_emit_restore_result (mb, sig->ret);

	res = mono_mb_create_and_cache (cache, sig,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

static MonoObject *
mono_remoting_wrapper (MonoMethod *method, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoTransparentProxy *this;
	MonoObject *res, *exc;
	MonoArray *out_args;

	this = *((MonoTransparentProxy **)params [0]);

	g_assert (this);
	g_assert (((MonoObject *)this)->vtable->klass == mono_defaults.transparent_proxy_class);
	
	/* skip the this pointer */
	params++;

	if (this->remote_class->proxy_class->contextbound && this->rp->context == (MonoObject *) mono_context_get ())
	{
		int i;
		MonoMethodSignature *sig = mono_method_signature (method);
		int count = sig->param_count;
		gpointer* mparams = (gpointer*) alloca(count*sizeof(gpointer));

		for (i=0; i<count; i++) {
			MonoClass *class = mono_class_from_mono_type (sig->params [i]);
			if (class->valuetype) {
				if (sig->params [i]->byref) {
					mparams[i] = *((gpointer *)params [i]);
				} else {
					/* runtime_invoke expects a boxed instance */
					if (mono_class_is_nullable (mono_class_from_mono_type (sig->params [i])))
						mparams[i] = mono_nullable_box (params [i], class);
					else
						mparams[i] = params [i];
				}
			} else {
				mparams[i] = *((gpointer**)params [i]);
			}
		}

		return mono_runtime_invoke (method, method->klass->valuetype? mono_object_unbox ((MonoObject*)this): this, mparams, NULL);
	}

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	res = mono_remoting_invoke ((MonoObject *)this->rp, msg, &exc, &out_args);

	if (exc)
		mono_raise_exception ((MonoException *)exc);

	mono_method_return_message_restore (method, params, out_args);

	return res;
} 

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int params_var;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE || method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE)
		return method;

	/* this seems to be the best plase to put this, as all remoting invokes seem to get filtered through here */
	if (method->klass->is_com_object || method->klass == mono_defaults.com_object_class) {
		MonoVTable *vtable = mono_class_vtable (mono_domain_get (), method->klass);
		g_assert (vtable); /*FIXME do proper error handling*/

		if (!vtable->remote) {
#ifndef DISABLE_COM
			return mono_cominterop_get_invoke (method);
#else
			g_assert_not_reached ();
#endif
		}
	}

	sig = mono_signature_no_pinvoke (method);

	/* we cant remote methods without this pointer */
	if (!sig->hasthis)
		return method;

	if ((res = mono_marshal_remoting_find_in_cache (method, MONO_WRAPPER_REMOTING_INVOKE)))
		return res;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_REMOTING_INVOKE);
	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, TRUE);

	mono_mb_emit_ptr (mb, method);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_remoting_wrapper);
	emit_thread_interrupt_checkpoint (mb);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else {
		 mono_mb_emit_restore_result (mb, sig->ret);
	}

	res = mono_remoting_mb_create_and_cache (method, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/* mono_get_xdomain_marshal_type()
 * Returns the kind of marshalling that a type needs for cross domain calls.
 */
static MonoXDomainMarshalType
mono_get_xdomain_marshal_type (MonoType *t)
{
	switch (t->type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		return MONO_MARSHAL_NONE;
	case MONO_TYPE_STRING:
		return MONO_MARSHAL_COPY;
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY: {
		MonoClass *elem_class = mono_class_from_mono_type (t)->element_class;
		if (mono_get_xdomain_marshal_type (&(elem_class->byval_arg)) != MONO_MARSHAL_SERIALIZE)
			return MONO_MARSHAL_COPY;
		break;
	}
	}

	return MONO_MARSHAL_SERIALIZE;
}


/* mono_marshal_xdomain_copy_value
 * Makes a copy of "val" suitable for the current domain.
 */
MonoObject *
mono_marshal_xdomain_copy_value (MonoObject *val)
{
	MonoDomain *domain;
	if (val == NULL) return NULL;

	domain = mono_domain_get ();

	switch (mono_object_class (val)->byval_arg.type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8: {
		return mono_value_box (domain, mono_object_class (val), ((char*)val) + sizeof(MonoObject));
	}
	case MONO_TYPE_STRING: {
		MonoString *str = (MonoString *) val;
		return (MonoObject *) mono_string_new_utf16 (domain, mono_string_chars (str), mono_string_length (str));
	}
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY: {
		MonoArray *acopy;
		MonoXDomainMarshalType mt = mono_get_xdomain_marshal_type (&(mono_object_class (val)->element_class->byval_arg));
		if (mt == MONO_MARSHAL_SERIALIZE) return NULL;
		acopy = mono_array_clone_in_domain (domain, (MonoArray *) val);
		if (mt == MONO_MARSHAL_COPY) {
			int i, len = mono_array_length (acopy);
			for (i = 0; i < len; i++) {
				MonoObject *item = mono_array_get (acopy, gpointer, i);
				mono_array_setref (acopy, i, mono_marshal_xdomain_copy_value (item));
			}
		}
		return (MonoObject *) acopy;
	}
	}

	if (mono_object_class (val) == mono_defaults.stringbuilder_class) {
		MonoStringBuilder *oldsb = (MonoStringBuilder *) val;
		MonoStringBuilder *newsb = (MonoStringBuilder *) mono_object_new (domain, mono_defaults.stringbuilder_class);
		MONO_OBJECT_SETREF (newsb, str, mono_string_new_utf16 (domain, mono_string_chars (oldsb->str), mono_string_length (oldsb->str)));
		newsb->length = oldsb->length;
		newsb->max_capacity = (gint32)0x7fffffff;
		return (MonoObject *) newsb;
	}
	return NULL;
}

/* mono_marshal_xdomain_copy_out_value()
 * Copies the contents of the src instance into the dst instance. src and dst
 * must have the same type, and if they are arrays, the same size.
 */
static void
mono_marshal_xdomain_copy_out_value (MonoObject *src, MonoObject *dst)
{
	if (src == NULL || dst == NULL) return;
	
	g_assert (mono_object_class (src) == mono_object_class (dst));

	switch (mono_object_class (src)->byval_arg.type) {
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY: {
		int mt = mono_get_xdomain_marshal_type (&(mono_object_class (src)->element_class->byval_arg));
		if (mt == MONO_MARSHAL_SERIALIZE) return;
		if (mt == MONO_MARSHAL_COPY) {
			int i, len = mono_array_length ((MonoArray *)dst);
			for (i = 0; i < len; i++) {
				MonoObject *item = mono_array_get ((MonoArray *)src, gpointer, i);
				mono_array_setref ((MonoArray *)dst, i, mono_marshal_xdomain_copy_value (item));
			}
		} else {
			mono_array_full_copy ((MonoArray *)src, (MonoArray *)dst);
		}
		return;
	}
	}

	if (mono_object_class (src) == mono_defaults.stringbuilder_class) {
		MonoStringBuilder *src_sb = (MonoStringBuilder *) src;
		MonoStringBuilder *dst_sb = (MonoStringBuilder *) dst;
	
		MONO_OBJECT_SETREF (dst_sb, str, mono_string_new_utf16 (mono_object_domain (dst), mono_string_chars (src_sb->str), mono_string_length (src_sb->str)));
		dst_sb->cached_str = NULL;
		dst_sb->length = src_sb->length;
	}
}

static void
mono_marshal_emit_xdomain_copy_value (MonoMethodBuilder *mb, MonoClass *pclass)
{
	mono_mb_emit_icall (mb, mono_marshal_xdomain_copy_value);
	mono_mb_emit_op (mb, CEE_CASTCLASS, pclass);
}

static void
mono_marshal_emit_xdomain_copy_out_value (MonoMethodBuilder *mb, MonoClass *pclass)
{
	mono_mb_emit_icall (mb, mono_marshal_xdomain_copy_out_value);
}

/* mono_marshal_supports_fast_xdomain()
 * Returns TRUE if the method can use the fast xdomain wrapper.
 */
static gboolean
mono_marshal_supports_fast_xdomain (MonoMethod *method)
{
	return !method->klass->contextbound &&
		   !((method->flags & METHOD_ATTRIBUTE_SPECIAL_NAME) && (strcmp (".ctor", method->name) == 0));
}

static gint32
mono_marshal_set_domain_by_id (gint32 id, MonoBoolean push)
{
	MonoDomain *current_domain = mono_domain_get ();
	MonoDomain *domain = mono_domain_get_by_id (id);

	if (!domain || !mono_domain_set (domain, FALSE))	
		mono_raise_exception (mono_get_exception_appdomain_unloaded ());

	if (push)
		mono_thread_push_appdomain_ref (domain);
	else
		mono_thread_pop_appdomain_ref ();

	return current_domain->domain_id;
}

static void
mono_marshal_emit_switch_domain (MonoMethodBuilder *mb)
{
	mono_mb_emit_icall (mb, mono_marshal_set_domain_by_id);
}

/* mono_marshal_emit_load_domain_method ()
 * Loads into the stack a pointer to the code of the provided method for
 * the current domain.
 */
static void
mono_marshal_emit_load_domain_method (MonoMethodBuilder *mb, MonoMethod *method)
{
	/* We need a pointer to the method for the running domain (not the domain
	 * that compiles the method).
	 */
	mono_mb_emit_ptr (mb, method);
	mono_mb_emit_icall (mb, mono_compile_method);
}

/* mono_marshal_check_domain_image ()
 * Returns TRUE if the image is loaded in the specified
 * application domain.
 */
static gboolean
mono_marshal_check_domain_image (gint32 domain_id, MonoImage *image)
{
	MonoAssembly* ass;
	GSList *tmp;
	
	MonoDomain *domain = mono_domain_get_by_id (domain_id);
	if (!domain)
		return FALSE;
	
	mono_domain_assemblies_lock (domain);
	for (tmp = domain->domain_assemblies; tmp; tmp = tmp->next) {
		ass = tmp->data;
		if (ass->image == image)
			break;
	}
	mono_domain_assemblies_unlock (domain);
	
	return tmp != NULL;
}

/* mono_marshal_get_xappdomain_dispatch ()
 * Generates a method that dispatches a method call from another domain into
 * the current domain.
 */
static MonoMethod *
mono_marshal_get_xappdomain_dispatch (MonoMethod *method, int *marshal_types, int complex_count, int complex_out_count, int ret_marshal_type)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i, j, param_index, copy_locals_base;
	MonoClass *ret_class = NULL;
	int loc_array=0, loc_return=0, loc_serialized_exc=0;
	MonoExceptionClause *main_clause;
	int pos, pos_leave;
	gboolean copy_return;

	if ((res = mono_marshal_remoting_find_in_cache (method, MONO_WRAPPER_XDOMAIN_DISPATCH)))
		return res;

	sig = mono_method_signature (method);
	copy_return = (sig->ret->type != MONO_TYPE_VOID && ret_marshal_type != MONO_MARSHAL_SERIALIZE);

	j = 0;
	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3 + sig->param_count - complex_count);
	csig->params [j++] = &mono_defaults.object_class->byval_arg;
	csig->params [j++] = &byte_array_class->this_arg;
	csig->params [j++] = &byte_array_class->this_arg;
	for (i = 0; i < sig->param_count; i++) {
		if (marshal_types [i] != MONO_MARSHAL_SERIALIZE)
			csig->params [j++] = sig->params [i];
	}
	if (copy_return)
		csig->ret = sig->ret;
	else
		csig->ret = &mono_defaults.void_class->byval_arg;
	csig->pinvoke = 1;
	csig->hasthis = FALSE;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_XDOMAIN_DISPATCH);
	mb->method->save_lmf = 1;

	/* Locals */

	loc_serialized_exc = mono_mb_add_local (mb, &byte_array_class->byval_arg);
	if (complex_count > 0)
		loc_array = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	if (sig->ret->type != MONO_TYPE_VOID) {
		loc_return = mono_mb_add_local (mb, sig->ret);
		ret_class = mono_class_from_mono_type (sig->ret);
	}

	/* try */

	main_clause = mono_image_alloc0 (method->klass->image, sizeof (MonoExceptionClause));
	main_clause->try_offset = mono_mb_get_label (mb);

	/* Clean the call context */

	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_managed_call (mb, method_set_call_context, NULL);
	mono_mb_emit_byte (mb, CEE_POP);

	/* Deserialize call data */

	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_byte (mb, CEE_DUP);
	pos = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);
	
	mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
	mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
	
	if (complex_count > 0)
		mono_mb_emit_stloc (mb, loc_array);
	else
		mono_mb_emit_byte (mb, CEE_POP);

	mono_mb_patch_short_branch (mb, pos);

	/* Get the target object */
	
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_managed_call (mb, method_rs_appdomain_target, NULL);

	/* Load the arguments */
	
	copy_locals_base = mb->locals;
	param_index = 3;	// Index of the first non-serialized parameter of this wrapper
	j = 0;
	for (i = 0; i < sig->param_count; i++) {
		MonoType *pt = sig->params [i];
		MonoClass *pclass = mono_class_from_mono_type (pt);
		switch (marshal_types [i]) {
		case MONO_MARSHAL_SERIALIZE: {
			/* take the value from the serialized array */
			mono_mb_emit_ldloc (mb, loc_array);
			mono_mb_emit_icon (mb, j++);
			if (pt->byref) {
				if (pclass->valuetype) {
					mono_mb_emit_byte (mb, CEE_LDELEM_REF);
					mono_mb_emit_op (mb, CEE_UNBOX, pclass);
				} else {
					mono_mb_emit_op (mb, CEE_LDELEMA, pclass);
				}
			} else {
				if (pclass->valuetype) {
					mono_mb_emit_byte (mb, CEE_LDELEM_REF);
					mono_mb_emit_op (mb, CEE_UNBOX, pclass);
					mono_mb_emit_op (mb, CEE_LDOBJ, pclass);
				} else {
					mono_mb_emit_byte (mb, CEE_LDELEM_REF);
					if (pclass != mono_defaults.object_class) {
						mono_mb_emit_op (mb, CEE_CASTCLASS, pclass);
					}
				}
			}
			break;
		}
		case MONO_MARSHAL_COPY_OUT: {
			/* Keep a local copy of the value since we need to copy it back after the call */
			int copy_local = mono_mb_add_local (mb, &(pclass->byval_arg));
			mono_mb_emit_ldarg (mb, param_index++);
			mono_marshal_emit_xdomain_copy_value (mb, pclass);
			mono_mb_emit_byte (mb, CEE_DUP);
			mono_mb_emit_stloc (mb, copy_local);
			break;
		}
		case MONO_MARSHAL_COPY: {
			mono_mb_emit_ldarg (mb, param_index);
			if (pt->byref) {
				mono_mb_emit_byte (mb, CEE_DUP);
				mono_mb_emit_byte (mb, CEE_DUP);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_marshal_emit_xdomain_copy_value (mb, pclass);
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			} else {
				mono_marshal_emit_xdomain_copy_value (mb, pclass);
			}
			param_index++;
			break;
		}
		case MONO_MARSHAL_NONE:
			mono_mb_emit_ldarg (mb, param_index++);
			break;
		}
	}

	/* Make the call to the real object */

	emit_thread_force_interrupt_checkpoint (mb);
	
	mono_mb_emit_op (mb, CEE_CALLVIRT, method);

	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_stloc (mb, loc_return);

	/* copy back MONO_MARSHAL_COPY_OUT parameters */

	j = 0;
	param_index = 3;
	for (i = 0; i < sig->param_count; i++) {
		if (marshal_types [i] == MONO_MARSHAL_SERIALIZE) continue;
		if (marshal_types [i] == MONO_MARSHAL_COPY_OUT) {
			mono_mb_emit_ldloc (mb, copy_locals_base + (j++));
			mono_mb_emit_ldarg (mb, param_index);
			mono_marshal_emit_xdomain_copy_out_value (mb, mono_class_from_mono_type (sig->params [i]));
		}
		param_index++;
	}

	/* Serialize the return values */
	
	if (complex_out_count > 0) {
		/* Reset parameters in the array that don't need to be serialized back */
		j = 0;
		for (i = 0; i < sig->param_count; i++) {
			if (marshal_types[i] != MONO_MARSHAL_SERIALIZE) continue;
			if (!sig->params [i]->byref) {
				mono_mb_emit_ldloc (mb, loc_array);
				mono_mb_emit_icon (mb, j);
				mono_mb_emit_byte (mb, CEE_LDNULL);
				mono_mb_emit_byte (mb, CEE_STELEM_REF);
			}
			j++;
		}
	
		/* Add the return value to the array */
	
		if (ret_marshal_type == MONO_MARSHAL_SERIALIZE) {
			mono_mb_emit_ldloc (mb, loc_array);
			mono_mb_emit_icon (mb, complex_count);	/* The array has an additional slot to hold the ret value */
			mono_mb_emit_ldloc (mb, loc_return);

			g_assert (ret_class); /*FIXME properly fail here*/
			if (ret_class->valuetype) {
				mono_mb_emit_op (mb, CEE_BOX, ret_class);
			}
			mono_mb_emit_byte (mb, CEE_STELEM_REF);
		}
	
		/* Serialize */
	
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_ldloc (mb, loc_array);
		mono_mb_emit_managed_call (mb, method_rs_serialize, NULL);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	} else if (ret_marshal_type == MONO_MARSHAL_SERIALIZE) {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_ldloc (mb, loc_return);
		if (ret_class->valuetype) {
			mono_mb_emit_op (mb, CEE_BOX, ret_class);
		}
		mono_mb_emit_managed_call (mb, method_rs_serialize, NULL);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	} else {
		mono_mb_emit_ldarg (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_managed_call (mb, method_rs_serialize, NULL);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	}

	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* Main exception catch */
	main_clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	main_clause->try_len = mono_mb_get_pos (mb) - main_clause->try_offset;
	main_clause->data.catch_class = mono_defaults.object_class;
	
	/* handler code */
	main_clause->handler_offset = mono_mb_get_label (mb);
	mono_mb_emit_managed_call (mb, method_rs_serialize_exc, NULL);
	mono_mb_emit_stloc (mb, loc_serialized_exc);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, loc_serialized_exc);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	mono_mb_emit_branch (mb, CEE_LEAVE);
	main_clause->handler_len = mono_mb_get_pos (mb) - main_clause->handler_offset;
	/* end catch */

	mono_mb_patch_branch (mb, pos_leave);
	
	if (copy_return)
		mono_mb_emit_ldloc (mb, loc_return);

	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_set_clauses (mb, 1, main_clause);

	res = mono_remoting_mb_create_and_cache (method, mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/* mono_marshal_get_xappdomain_invoke ()
 * Generates a fast remoting wrapper for cross app domain calls.
 */
MonoMethod *
mono_marshal_get_xappdomain_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i, j, complex_count, complex_out_count, copy_locals_base;
	int *marshal_types;
	MonoClass *ret_class = NULL;
	MonoMethod *xdomain_method;
	int ret_marshal_type = MONO_MARSHAL_NONE;
	int loc_array=0, loc_serialized_data=-1, loc_real_proxy;
	int loc_old_domainid, loc_domainid, loc_return=0, loc_serialized_exc=0, loc_context;
	int pos, pos_dispatch, pos_noex;
	gboolean copy_return = FALSE;

	g_assert (method);
	
	if (method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE || method->wrapper_type == MONO_WRAPPER_XDOMAIN_INVOKE)
		return method;

	/* we cant remote methods without this pointer */
	if (!mono_method_signature (method)->hasthis)
		return method;

	if (!mono_marshal_supports_fast_xdomain (method))
		return mono_marshal_get_remoting_invoke (method);
	
	mono_remoting_marshal_init ();

	if ((res = mono_marshal_remoting_find_in_cache (method, MONO_WRAPPER_XDOMAIN_INVOKE)))
		return res;
	
	sig = mono_signature_no_pinvoke (method);

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_XDOMAIN_INVOKE);
	mb->method->save_lmf = 1;

	/* Count the number of parameters that need to be serialized */

	marshal_types = alloca (sizeof (int) * sig->param_count);
	complex_count = complex_out_count = 0;
	for (i = 0; i < sig->param_count; i++) {
		MonoType *ptype = sig->params[i];
		int mt = mono_get_xdomain_marshal_type (ptype);
		
		/* If the [Out] attribute is applied to a parameter that can be internally copied,
		 * the copy will be made by reusing the original object instance
		 */
		if ((ptype->attrs & PARAM_ATTRIBUTE_OUT) != 0 && mt == MONO_MARSHAL_COPY && !ptype->byref)
			mt = MONO_MARSHAL_COPY_OUT;
		else if (mt == MONO_MARSHAL_SERIALIZE) {
			complex_count++;
			if (ptype->byref) complex_out_count++;
		}
		marshal_types [i] = mt;
	}

	if (sig->ret->type != MONO_TYPE_VOID) {
		ret_marshal_type = mono_get_xdomain_marshal_type (sig->ret);
		ret_class = mono_class_from_mono_type (sig->ret);
		copy_return = ret_marshal_type != MONO_MARSHAL_SERIALIZE;
	}
	
	/* Locals */

	if (complex_count > 0)
		loc_array = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	loc_serialized_data = mono_mb_add_local (mb, &byte_array_class->byval_arg);
	loc_real_proxy = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	if (copy_return)
		loc_return = mono_mb_add_local (mb, sig->ret);
	loc_old_domainid = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	loc_domainid = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	loc_serialized_exc = mono_mb_add_local (mb, &byte_array_class->byval_arg);
	loc_context = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* Save thread domain data */

	mono_mb_emit_icall (mb, mono_context_get);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_stloc (mb, loc_context);

	/* If the thread is not running in the default context, it needs to go
	 * through the whole remoting sink, since the context is going to change
	 */
	mono_mb_emit_managed_call (mb, method_needs_context_sink, NULL);
	pos = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);
	
	/* Another case in which the fast path can't be used: when the target domain
	 * has a different image for the same assembly.
	 */

	/* Get the target domain id */

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_stloc (mb, loc_real_proxy);

	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoRealProxy, target_domain_id));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_stloc (mb, loc_domainid);

	/* Check if the target domain has the same image for the required assembly */

	mono_mb_emit_ldloc (mb, loc_domainid);
	mono_mb_emit_ptr (mb, method->klass->image);
	mono_mb_emit_icall (mb, mono_marshal_check_domain_image);
	pos_dispatch = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

	/* Use the whole remoting sink to dispatch this message */

	mono_mb_patch_short_branch (mb, pos);

	mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	
	mono_mb_emit_managed_call (mb, mono_marshal_get_remoting_invoke (method), NULL);
	mono_mb_emit_byte (mb, CEE_RET);
	mono_mb_patch_short_branch (mb, pos_dispatch);

	/* Create the array that will hold the parameters to be serialized */

	if (complex_count > 0) {
		mono_mb_emit_icon (mb, (ret_marshal_type == MONO_MARSHAL_SERIALIZE && complex_out_count > 0) ? complex_count + 1 : complex_count);	/* +1 for the return type */
		mono_mb_emit_op (mb, CEE_NEWARR, mono_defaults.object_class);
	
		j = 0;
		for (i = 0; i < sig->param_count; i++) {
			MonoClass *pclass;
			if (marshal_types [i] != MONO_MARSHAL_SERIALIZE) continue;
			pclass = mono_class_from_mono_type (sig->params[i]);
			mono_mb_emit_byte (mb, CEE_DUP);
			mono_mb_emit_icon (mb, j);
			mono_mb_emit_ldarg (mb, i + 1);		/* 0=this */
			if (sig->params[i]->byref) {
				if (pclass->valuetype)
					mono_mb_emit_op (mb, CEE_LDOBJ, pclass);
				else
					mono_mb_emit_byte (mb, CEE_LDIND_REF);
			}
			if (pclass->valuetype)
				mono_mb_emit_op (mb, CEE_BOX, pclass);
			mono_mb_emit_byte (mb, CEE_STELEM_REF);
			j++;
		}
		mono_mb_emit_stloc (mb, loc_array);

		/* Serialize parameters */
	
		mono_mb_emit_ldloc (mb, loc_array);
		mono_mb_emit_managed_call (mb, method_rs_serialize, NULL);
		mono_mb_emit_stloc (mb, loc_serialized_data);
	} else {
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_managed_call (mb, method_rs_serialize, NULL);
		mono_mb_emit_stloc (mb, loc_serialized_data);
	}

	/* switch domain */

	mono_mb_emit_ldloc (mb, loc_domainid);
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);
	mono_marshal_emit_switch_domain (mb);
	mono_mb_emit_stloc (mb, loc_old_domainid);

	/* Load the arguments */
	
	mono_mb_emit_ldloc (mb, loc_real_proxy);
	mono_mb_emit_ldloc_addr (mb, loc_serialized_data);
	mono_mb_emit_ldloc_addr (mb, loc_serialized_exc);

	copy_locals_base = mb->locals;
	for (i = 0; i < sig->param_count; i++) {
		switch (marshal_types [i]) {
		case MONO_MARSHAL_SERIALIZE:
			continue;
		case MONO_MARSHAL_COPY: {
			mono_mb_emit_ldarg (mb, i+1);
			if (sig->params [i]->byref) {
				/* make a local copy of the byref parameter. The real parameter
				 * will be updated after the xdomain call
				 */
				MonoClass *pclass = mono_class_from_mono_type (sig->params [i]);
				int copy_local = mono_mb_add_local (mb, &(pclass->byval_arg));
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
				mono_mb_emit_stloc (mb, copy_local);
				mono_mb_emit_ldloc_addr (mb, copy_local);
			}
			break;
		}
		case MONO_MARSHAL_COPY_OUT:
		case MONO_MARSHAL_NONE:
			mono_mb_emit_ldarg (mb, i+1);
			break;
		}
	}

	/* Make the call to the invoke wrapper in the target domain */

	xdomain_method = mono_marshal_get_xappdomain_dispatch (method, marshal_types, complex_count, complex_out_count, ret_marshal_type);
	mono_marshal_emit_load_domain_method (mb, xdomain_method);
	mono_mb_emit_calli (mb, mono_method_signature (xdomain_method));

	if (copy_return)
		mono_mb_emit_stloc (mb, loc_return);

	/* Switch domain */

	mono_mb_emit_ldloc (mb, loc_old_domainid);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_marshal_emit_switch_domain (mb);
	mono_mb_emit_byte (mb, CEE_POP);
	
	/* Restore thread domain data */
	
	mono_mb_emit_ldloc (mb, loc_context);
	mono_mb_emit_icall (mb, mono_context_set);
	
	/* if (loc_serialized_exc != null) ... */

	mono_mb_emit_ldloc (mb, loc_serialized_exc);
	pos_noex = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

	mono_mb_emit_ldloc (mb, loc_serialized_exc);
	mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
	mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
	mono_mb_emit_op (mb, CEE_CASTCLASS, mono_defaults.exception_class);
	mono_mb_emit_managed_call (mb, method_exc_fixexc, NULL);
	mono_mb_emit_byte (mb, CEE_THROW);
	mono_mb_patch_short_branch (mb, pos_noex);

	/* copy back non-serialized output parameters */

	j = 0;
	for (i = 0; i < sig->param_count; i++) {
		if (!sig->params [i]->byref || marshal_types [i] != MONO_MARSHAL_COPY) continue;
		mono_mb_emit_ldarg (mb, i + 1);
		mono_mb_emit_ldloc (mb, copy_locals_base + (j++));
		mono_marshal_emit_xdomain_copy_value (mb, mono_class_from_mono_type (sig->params [i]));
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	}

	/* Deserialize out parameters */

	if (complex_out_count > 0) {
		mono_mb_emit_ldloc (mb, loc_serialized_data);
		mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
		mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
		mono_mb_emit_stloc (mb, loc_array);
	
		/* Copy back output parameters and return type */
		
		j = 0;
		for (i = 0; i < sig->param_count; i++) {
			if (marshal_types [i] != MONO_MARSHAL_SERIALIZE) continue;
			if (sig->params[i]->byref) {
				MonoClass *pclass = mono_class_from_mono_type (sig->params [i]);
				mono_mb_emit_ldarg (mb, i + 1);
				mono_mb_emit_ldloc (mb, loc_array);
				mono_mb_emit_icon (mb, j);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);
				if (pclass->valuetype) {
					mono_mb_emit_op (mb, CEE_UNBOX, pclass);
					mono_mb_emit_op (mb, CEE_LDOBJ, pclass);
					mono_mb_emit_op (mb, CEE_STOBJ, pclass);
				} else {
					if (pclass != mono_defaults.object_class)
						mono_mb_emit_op (mb, CEE_CASTCLASS, pclass);
					mono_mb_emit_byte (mb, CEE_STIND_REF);
				}
			}
			j++;
		}
	
		if (ret_marshal_type == MONO_MARSHAL_SERIALIZE) {
			mono_mb_emit_ldloc (mb, loc_array);
			mono_mb_emit_icon (mb, complex_count);
			mono_mb_emit_byte (mb, CEE_LDELEM_REF);
			if (ret_class->valuetype) {
				mono_mb_emit_op (mb, CEE_UNBOX, ret_class);
				mono_mb_emit_op (mb, CEE_LDOBJ, ret_class);
			}
		}
	} else if (ret_marshal_type == MONO_MARSHAL_SERIALIZE) {
		mono_mb_emit_ldloc (mb, loc_serialized_data);
		mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
		mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
		if (ret_class->valuetype) {
			mono_mb_emit_op (mb, CEE_UNBOX, ret_class);
			mono_mb_emit_op (mb, CEE_LDOBJ, ret_class);
		} else if (ret_class != mono_defaults.object_class) {
			mono_mb_emit_op (mb, CEE_CASTCLASS, ret_class);
		}
	} else {
		mono_mb_emit_ldloc (mb, loc_serialized_data);
		mono_mb_emit_byte (mb, CEE_DUP);
		pos = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);
		mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
	
		mono_mb_patch_short_branch (mb, pos);
		mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
		mono_mb_emit_byte (mb, CEE_POP);
	}

	if (copy_return) {
		mono_mb_emit_ldloc (mb, loc_return);
		if (ret_marshal_type == MONO_MARSHAL_COPY)
			mono_marshal_emit_xdomain_copy_value (mb, ret_class);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_remoting_mb_create_and_cache (method, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

MonoMethod *
mono_marshal_get_remoting_invoke_for_target (MonoMethod *method, MonoRemotingTarget target_type)
{
	if (target_type == MONO_REMOTING_TARGET_APPDOMAIN) {
		return mono_marshal_get_xappdomain_invoke (method);
	} else if (target_type == MONO_REMOTING_TARGET_COMINTEROP) {
#ifndef DISABLE_COM
		return mono_cominterop_get_invoke (method);
#else
		g_assert_not_reached ();
#endif
	} else {
		return mono_marshal_get_remoting_invoke (method);
	}
	/* Not erached */
	return NULL;
}

G_GNUC_UNUSED static gpointer
mono_marshal_load_remoting_wrapper (MonoRealProxy *rp, MonoMethod *method)
{
	if (rp->target_domain_id != -1)
		return mono_compile_method (mono_marshal_get_xappdomain_invoke (method));
	else
		return mono_compile_method (mono_marshal_get_remoting_invoke (method));
}

MonoMethod *
mono_marshal_get_remoting_invoke_with_check (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res, *native;
	int i, pos, pos_rem;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)
		return method;

	/* we cant remote methods without this pointer */
	g_assert (mono_method_signature (method)->hasthis);

	if ((res = mono_marshal_remoting_find_in_cache (method, MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)))
		return res;

	sig = mono_signature_no_pinvoke (method);
	
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK);

	for (i = 0; i <= sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);
	
	mono_mb_emit_ldarg (mb, 0);
	pos = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	if (mono_marshal_supports_fast_xdomain (method)) {
		mono_mb_emit_ldarg (mb, 0);
		pos_rem = mono_mb_emit_xdomain_check (mb, CEE_BEQ);
		
		/* wrapper for cross app domain calls */
		native = mono_marshal_get_xappdomain_invoke (method);
		mono_mb_emit_managed_call (mb, native, mono_method_signature (native));
		mono_mb_emit_byte (mb, CEE_RET);
		
		mono_mb_patch_branch (mb, pos_rem);
	}
	/* wrapper for normal remote calls */
	native = mono_marshal_get_remoting_invoke (method);
	mono_mb_emit_managed_call (mb, native, mono_method_signature (native));
	mono_mb_emit_byte (mb, CEE_RET);

	/* not a proxy */
	mono_mb_patch_branch (mb, pos);
	mono_mb_emit_managed_call (mb, method, mono_method_signature (method));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_remoting_mb_create_and_cache (method, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

typedef struct
{
	MonoMethodSignature *sig;
	MonoMethod *method;
} SignatureMethodPair;

static guint
signature_method_pair_hash (gconstpointer data)
{
	SignatureMethodPair *pair = (SignatureMethodPair*)data;

	return mono_signature_hash (pair->sig) ^ mono_aligned_addr_hash (pair->method);
}

static gboolean
signature_method_pair_equal (SignatureMethodPair *pair1, SignatureMethodPair *pair2)
{
	return mono_metadata_signature_equal (pair1->sig, pair2->sig) && (pair1->method == pair2->method);
}

static gboolean
signature_method_pair_matches_method (gpointer key, gpointer value, gpointer user_data)
{
	SignatureMethodPair *pair = (SignatureMethodPair*)key;
	MonoMethod *method = (MonoMethod*)user_data;

	return pair->method == method;
}

static void
free_signature_method_pair (SignatureMethodPair *pair)
{
	g_free (pair);
}

/*
 * the returned method invokes all methods in a multicast delegate.
 */
MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method, MonoDelegate *del)
{
	MonoMethodSignature *sig, *static_sig, *invoke_sig;
	int i;
	MonoMethodBuilder *mb;
	MonoMethod *res, *newm;
	GHashTable *cache;
	SignatureMethodPair key;
	SignatureMethodPair *new_key;
	int local_prev, local_target;
	int pos0;
	char *name;
	MonoMethod *target_method = NULL;
	MonoClass *target_class = NULL;
	gboolean callvirt = FALSE;
	gboolean closed_over_null = FALSE;
	gboolean static_method_with_first_arg_bound = FALSE;

	/*
	 * If the delegate target is null, and the target method is not static, a virtual 
	 * call is made to that method with the first delegate argument as this. This is 
	 * a non-documented .NET feature.
	 */
	if (del && !del->target && del->method && mono_method_signature (del->method)->hasthis) {
		callvirt = TRUE;
		target_method = del->method;
		if (target_method->is_inflated) {
			MonoType *target_type;

			g_assert (method->signature->hasthis);
			target_type = mono_class_inflate_generic_type (method->signature->params [0],
				mono_method_get_context (method));
			target_class = mono_class_from_mono_type (target_type);
		} else {
			target_class = del->method->klass;
		}
	}

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "Invoke"));
		
	invoke_sig = sig = mono_signature_no_pinvoke (method);

	if (callvirt)
		closed_over_null = sig->param_count == mono_method_signature (del->method)->param_count;

	if (del && del->method && mono_method_signature (del->method)->param_count == sig->param_count + 1 && (del->method->flags & METHOD_ATTRIBUTE_STATIC)) {
		invoke_sig = mono_method_signature (del->method);
		target_method = del->method;
		static_method_with_first_arg_bound = TRUE;
	}

	if (callvirt || static_method_with_first_arg_bound) {
		GHashTable **cache_ptr;
		if (static_method_with_first_arg_bound)
			cache_ptr = &method->klass->image->delegate_bound_static_invoke_cache;
		else
			cache_ptr = &method->klass->image->delegate_abstract_invoke_cache;

		/* We need to cache the signature+method pair */
		mono_marshal_lock ();
		if (!*cache_ptr)
			*cache_ptr = g_hash_table_new_full (signature_method_pair_hash, (GEqualFunc)signature_method_pair_equal, (GDestroyNotify)free_signature_method_pair, NULL);
		cache = *cache_ptr;
		key.sig = invoke_sig;
		key.method = target_method;
		res = g_hash_table_lookup (cache, &key);
		mono_marshal_unlock ();
		if (res)
			return res;
	} else {
		cache = get_cache (&method->klass->image->delegate_invoke_cache,
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)mono_metadata_signature_equal);
		if ((res = mono_marshal_find_in_cache (cache, sig)))
			return res;
	}

	static_sig = signature_dup (method->klass->image, sig);
	static_sig->hasthis = 0;
	if (!static_method_with_first_arg_bound)
		invoke_sig = static_sig;

	name = mono_signature_to_name (sig, "invoke");
	mb = mono_mb_new (get_wrapper_target_class (method->klass->image), name,  MONO_WRAPPER_DELEGATE_INVOKE);
	g_free (name);

	/* allocate local 0 (object) */
	local_target = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	local_prev = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	g_assert (sig->hasthis);
	
	/*
	 * if (prev != null)
         *	prev.Invoke( args .. );
	 * return this.<target>( args .. );
         */
	
	/* this wrapper can be used in unmanaged-managed transitions */
	emit_thread_interrupt_checkpoint (mb);
	
	/* get this->prev */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoMulticastDelegate, prev));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_stloc (mb, local_prev);
	mono_mb_emit_ldloc (mb, local_prev);

	/* if prev != null */
	pos0 = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/* then recurse */

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_NOT_TAKEN);

	mono_mb_emit_ldloc (mb, local_prev);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_byte (mb, CEE_POP);

	/* continued or prev == null */
	mono_mb_patch_branch (mb, pos0);

	/* get this->target */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, target));
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
			mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
			mono_mb_emit_byte (mb, CEE_LDIND_I );
			mono_mb_emit_op (mb, CEE_CALLI, sig);

			mono_mb_emit_byte (mb, CEE_RET);
		}
	
		/* else [target == null] call this->method_ptr static */
		mono_mb_patch_branch (mb, pos0);
	}

	if (callvirt) {
		if (!closed_over_null) {
			mono_mb_emit_ldarg (mb, 1);
			mono_mb_emit_op (mb, CEE_CASTCLASS, target_class);
			for (i = 1; i < sig->param_count; ++i)
				mono_mb_emit_ldarg (mb, i + 1);
			mono_mb_emit_op (mb, CEE_CALLVIRT, target_method);
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
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		mono_mb_emit_byte (mb, CEE_LDIND_I );
		mono_mb_emit_op (mb, CEE_CALLI, invoke_sig);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	if (static_method_with_first_arg_bound || callvirt) {
		// From mono_mb_create_and_cache
		mb->skip_visibility = 1;
		newm = mono_mb_create_method (mb, sig, sig->param_count + 16);
		/*We perform double checked locking, so must fence before publishing*/
		mono_memory_barrier ();
		mono_marshal_lock ();
		res = g_hash_table_lookup (cache, &key);
		if (!res) {
			res = newm;
			new_key = g_new0 (SignatureMethodPair, 1);
			*new_key = key;
			if (static_method_with_first_arg_bound)
				new_key->sig = signature_dup (del->method->klass->image, key.sig);
			g_hash_table_insert (cache, new_key, res);
			mono_marshal_set_wrapper_info (res, new_key);
			mono_marshal_unlock ();
		} else {
			mono_marshal_unlock ();
			mono_free_method (newm);
		}
	} else {
		mb->skip_visibility = 1;
		res = mono_mb_create_and_cache (cache, sig, mb, sig, sig->param_count + 16);
	}
	mono_mb_free (mb);

	return res;	
}

/*
 * signature_dup_add_this:
 *
 *  Make a copy of @sig, adding an explicit this argument.
 */
static MonoMethodSignature*
signature_dup_add_this (MonoImage *image, MonoMethodSignature *sig, MonoClass *klass)
{
	MonoMethodSignature *res;
	int i;

	res = mono_metadata_signature_alloc (image, sig->param_count + 1);
	memcpy (res, sig, MONO_SIZEOF_METHOD_SIGNATURE);
	res->param_count = sig->param_count + 1;
	res->hasthis = FALSE;
	for (i = sig->param_count - 1; i >= 0; i --)
		res->params [i + 1] = sig->params [i];
	res->params [0] = klass->valuetype ? &klass->this_arg : &klass->byval_arg;

	return res;
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
		cs = item->data;
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

	callsig = signature_dup (method->klass->image, mono_method_signature (method));
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
	if (t->byref)
		/* Can't share this with 'I' as that needs another indirection */
		return t;

	if (MONO_TYPE_IS_REFERENCE (t))
		return &mono_defaults.object_class->byval_arg;

	if (ret)
		/* The result needs to be boxed */
		return t;

handle_enum:
	switch (t->type) {
	case MONO_TYPE_U1:
		return &mono_defaults.sbyte_class->byval_arg;
	case MONO_TYPE_U2:
		return &mono_defaults.int16_class->byval_arg;
	case MONO_TYPE_U4:
		return &mono_defaults.int32_class->byval_arg;
	case MONO_TYPE_U8:
		return &mono_defaults.int64_class->byval_arg;
	case MONO_TYPE_BOOLEAN:
		return &mono_defaults.sbyte_class->byval_arg;
	case MONO_TYPE_CHAR:
		return &mono_defaults.int16_class->byval_arg;
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
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

/*
 * emit_invoke_call:
 *
 *   Emit the call to the wrapper method from a runtime invoke wrapper.
 */
static void
emit_invoke_call (MonoMethodBuilder *mb, MonoMethod *method,
				  MonoMethodSignature *sig, MonoMethodSignature *callsig,
				  int loc_res,
				  gboolean virtual, gboolean need_direct_wrapper)
{
	static MonoString *string_dummy = NULL;
	int i;
	int *tmp_nullable_locals;
	gboolean void_ret = FALSE;

	/* to make it work with our special string constructors */
	if (!string_dummy) {
		MONO_GC_REGISTER_ROOT_SINGLE (string_dummy);
		string_dummy = mono_string_new_wrapper ("dummy");
	}

	if (virtual) {
		g_assert (sig->hasthis);
		g_assert (method->flags & METHOD_ATTRIBUTE_VIRTUAL);
	}

	if (sig->hasthis) {
		if (method->string_ctor) {
			mono_mb_emit_ptr (mb, string_dummy);
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
	
	if (virtual) {
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
		if (!method->string_ctor)
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
emit_runtime_invoke_body (MonoMethodBuilder *mb, MonoClass *target_klass, MonoMethod *method,
						  MonoMethodSignature *sig, MonoMethodSignature *callsig,
						  gboolean virtual, gboolean need_direct_wrapper)
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
	emit_invoke_call (mb, method, sig, callsig, loc_res, virtual, need_direct_wrapper);

	labels [2] = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* Add a try clause around the call */
	clause = mono_image_alloc0 (target_klass->image, sizeof (MonoExceptionClause));
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
	emit_invoke_call (mb, method, sig, callsig, loc_res, virtual, need_direct_wrapper);

	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);
}

/*
 * generates IL code for the runtime invoke function 
 * MonoObject *runtime_invoke (MonoObject *this, void **params, MonoObject **exc, void* method)
 *
 * we also catch exceptions if exc != null
 * If VIRTUAL is TRUE, then METHOD is invoked virtually on THIS. This is useful since
 * it means that the compiled code for METHOD does not have to be looked up 
 * before calling the runtime invoke wrapper. In this case, the wrapper ignores
 * its METHOD argument.
 */
MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method, gboolean virtual)
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
	gboolean need_direct_wrapper = FALSE;
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

	if (virtual)
		need_direct_wrapper = TRUE;

	/* 
	 * Use a separate cache indexed by methods to speed things up and to avoid the
	 * boundless mempool growth caused by the signature_dup stuff below.
	 */
	if (virtual)
		cache = get_cache (&method->klass->image->runtime_invoke_vcall_cache, mono_aligned_addr_hash, NULL);
	else
		cache = get_cache (&method->klass->image->runtime_invoke_direct_cache, mono_aligned_addr_hash, NULL);
	res = mono_marshal_find_in_cache (cache, method);
	if (res)
		return res;
		
	if (method->klass->rank && (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
		(method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)) {
		/* 
		 * Array Get/Set/Address methods. The JIT implements them using inline code
		 * so we need to create an invoke wrapper which calls the method directly.
		 */
		need_direct_wrapper = TRUE;
	}
		
	if (method->string_ctor) {
		callsig = lookup_string_ctor_signature (mono_method_signature (method));
		if (!callsig)
			callsig = add_string_ctor_signature (method);
		/* Can't share this as we push a string as this */
		need_direct_wrapper = TRUE;
	} else {
		if (method->klass->valuetype && mono_method_signature (method)->hasthis) {
			/* 
			 * Valuetype methods receive a managed pointer as the this argument.
			 * Create a new signature to reflect this.
			 */
			callsig = signature_dup_add_this (method->klass->image, mono_method_signature (method), method->klass);
			/* Can't share this as it would be shared with static methods taking an IntPtr argument */
			need_direct_wrapper = TRUE;
		} else {
			if (method->dynamic)
				callsig = signature_dup (method->klass->image, mono_method_signature (method));
			else
				callsig = mono_method_signature (method);
		}
	}

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

		cache = get_cache (&target_klass->image->runtime_invoke_cache, 
						   (GHashFunc)mono_signature_hash, 
						   (GCompareFunc)runtime_invoke_signature_equal);

		/* from mono_marshal_find_in_cache */
		mono_marshal_lock ();
		res = g_hash_table_lookup (cache, callsig);
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
	
	sig = mono_method_signature (method);

	csig = mono_metadata_signature_alloc (target_klass->image, 4);

	csig->ret = &mono_defaults.object_class->byval_arg;
	if (method->klass->valuetype && mono_method_signature (method)->hasthis)
		csig->params [0] = callsig->params [0];
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

	name = mono_signature_to_name (callsig, virtual ? "runtime_invoke_virtual" : "runtime_invoke");
	mb = mono_mb_new (target_klass, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

	param_names [0] = "this";
	param_names [1] = "params";
	param_names [2] = "exc";
	param_names [3] = "method";
	mono_mb_set_param_names (mb, param_names);

	emit_runtime_invoke_body (mb, target_klass, method, sig, callsig, virtual, need_direct_wrapper);

	if (need_direct_wrapper) {
		mb->skip_visibility = 1;
		res = mono_mb_create_and_cache (cache, method, mb, csig, sig->param_count + 16);
		info = mono_wrapper_info_create (res, virtual ? WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL : WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT);
		info->d.runtime_invoke.method = method;
		mono_marshal_set_wrapper_info (res, info);
	} else {
		/* taken from mono_mb_create_and_cache */
		mono_marshal_lock ();
		res = g_hash_table_lookup (cache, callsig);
		mono_marshal_unlock ();

		/* Somebody may have created it before us */
		if (!res) {
			MonoMethod *newm;
			newm = mono_mb_create_method (mb, csig, sig->param_count + 16);

			mono_marshal_lock ();
			res = g_hash_table_lookup (cache, callsig);
			if (!res) {
				res = newm;
				g_hash_table_insert (cache, callsig, res);
				/* Can't insert it into wrapper_hash since the key is a signature */
				g_hash_table_insert (method->klass->image->runtime_invoke_direct_cache, method, res);
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
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod*
mono_marshal_get_runtime_invoke_dynamic (void)
{
	static MonoMethod *method;
	MonoMethodSignature *csig;
	MonoExceptionClause *clause;
	MonoMethodBuilder *mb;
	int pos, posna;
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

	clause = mono_image_alloc0 (mono_defaults.corlib, sizeof (MonoExceptionClause));
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

	/* Check for the abort exception */
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_op (mb, CEE_ISINST, mono_defaults.threadabortexception_class);
	posna = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

	/* Delay the abort exception */
	mono_mb_emit_icall (mb, ves_icall_System_Threading_Thread_ResetAbort);

	mono_mb_patch_short_branch (mb, posna);
	mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_set_clauses (mb, 1, clause);

	/* return result */
	mono_mb_patch_branch (mb, pos);
	//mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_loader_lock ();
	/* double-checked locking */
	if (!method) {
		method = mono_mb_create_method (mb, csig, 16);
		info = mono_wrapper_info_create (method, WRAPPER_SUBTYPE_RUNTIME_INVOKE_DYNAMIC);
		mono_marshal_set_wrapper_info (method, info);
	}
	mono_loader_unlock ();

	mono_mb_free (mb);

	return method;
}

static void
mono_mb_emit_auto_layout_exception (MonoMethodBuilder *mb, MonoClass *klass)
{
	char *msg = g_strdup_printf ("The type `%s.%s' layout needs to be Sequential or Explicit",
				     klass->name_space, klass->name);

	mono_mb_emit_exception_marshal_directive (mb, msg);
}

/*
 * mono_marshal_get_ldfld_remote_wrapper:
 * @klass: The return type
 *
 * This method generates a wrapper for calling mono_load_remote_field_new.
 * The return type is ignored for now, as mono_load_remote_field_new () always
 * returns an object. In the future, to optimize some codepaths, we might
 * call a different function that takes a pointer to a valuetype, instead.
 */
MonoMethod *
mono_marshal_get_ldfld_remote_wrapper (MonoClass *klass)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	static MonoMethod* cached = NULL;

	mono_marshal_lock ();
	if (cached) {
		mono_marshal_unlock ();
		return cached;
	}
	mono_marshal_unlock ();

	mb = mono_mb_new_no_dup_name (mono_defaults.object_class, "__mono_load_remote_field_new_wrapper", MONO_WRAPPER_LDFLD_REMOTE);

	mb->method->save_lmf = 1;

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->ret = &mono_defaults.object_class->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->ret = &mono_defaults.object_class->byval_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_load_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);
 
	mono_marshal_lock ();
	res = cached;
	mono_marshal_unlock ();
	if (!res) {
		MonoMethod *newm;
		newm = mono_mb_create_method (mb, sig, 4);
		mono_marshal_lock ();
		res = cached;
		if (!res) {
			res = newm;
			cached = res;
			mono_marshal_unlock ();
		} else {
			mono_marshal_unlock ();
			mono_free_method (newm);
		}
	}
	mono_mb_free (mb);

	return res;
}

/*
 * mono_marshal_get_ldfld_wrapper:
 * @type: the type of the field
 *
 * This method generates a function which can be use to load a field with type
 * @type from an object. The generated function has the following signature:
 * <@type> ldfld_wrapper (MonoObject *this, MonoClass *class, MonoClassField *field, int offset)
 */
MonoMethod *
mono_marshal_get_ldfld_wrapper (MonoType *type)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoClass *klass;
	GHashTable *cache;
	char *name;
	int t, pos0, pos1 = 0;

	type = mono_type_get_underlying_type (type);

	t = type->type;

	if (!type->byref) {
		if (type->type == MONO_TYPE_SZARRAY) {
			klass = mono_defaults.array_class;
		} else if (type->type == MONO_TYPE_VALUETYPE) {
			klass = type->data.klass;
		} else if (t == MONO_TYPE_OBJECT || t == MONO_TYPE_CLASS || t == MONO_TYPE_STRING) {
			klass = mono_defaults.object_class;
		} else if (t == MONO_TYPE_PTR || t == MONO_TYPE_FNPTR) {
			klass = mono_defaults.int_class;
		} else if (t == MONO_TYPE_GENERICINST) {
			if (mono_type_generic_inst_is_valuetype (type))
				klass = mono_class_from_mono_type (type);
			else
				klass = mono_defaults.object_class;
		} else {
			klass = mono_class_from_mono_type (type);			
		}
	} else {
		klass = mono_defaults.int_class;
	}

	cache = get_cache (&klass->image->ldfld_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	/* we add the %p pointer value of klass because class names are not unique */
	name = g_strdup_printf ("__ldfld_wrapper_%p_%s.%s", klass, klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_LDFLD);
	g_free (name);

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &mono_defaults.int_class->byval_arg;
	sig->ret = &klass->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	pos0 = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);

	mono_mb_emit_managed_call (mb, mono_marshal_get_ldfld_remote_wrapper (klass), NULL);

	/*
	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->ret = &klass->this_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_load_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);
	*/

	if (klass->valuetype) {
		mono_mb_emit_op (mb, CEE_UNBOX, klass);
		pos1 = mono_mb_emit_branch (mb, CEE_BR);
	} else {
		mono_mb_emit_byte (mb, CEE_RET);
	}


	mono_mb_patch_branch (mb, pos0);

	mono_mb_emit_ldarg (mb, 0);
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);

	if (klass->valuetype)
		mono_mb_patch_branch (mb, pos1);

	switch (t) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		mono_mb_emit_byte (mb, mono_type_to_ldind (type));
		break;
	case MONO_TYPE_VALUETYPE:
		g_assert (!klass->enumtype);
		mono_mb_emit_op (mb, CEE_LDOBJ, klass);
		break;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (type)) {
			mono_mb_emit_op (mb, CEE_LDOBJ, klass);
		} else {
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		}
		break;
	default:
		g_warning ("type %x not implemented", type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (cache, klass,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

/*
 * mono_marshal_get_ldflda_wrapper:
 * @type: the type of the field
 *
 * This method generates a function which can be used to load a field address
 * from an object. The generated function has the following signature:
 * gpointer ldflda_wrapper (MonoObject *this, MonoClass *class, MonoClassField *field, int offset);
 */
MonoMethod *
mono_marshal_get_ldflda_wrapper (MonoType *type)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoClass *klass;
	GHashTable *cache;
	char *name;
	int t, pos0, pos1, pos2, pos3;

	type = mono_type_get_underlying_type (type);
	t = type->type;

	if (!type->byref) {
		if (type->type == MONO_TYPE_SZARRAY) {
			klass = mono_defaults.array_class;
		} else if (type->type == MONO_TYPE_VALUETYPE) {
			klass = type->data.klass;
		} else if (t == MONO_TYPE_OBJECT || t == MONO_TYPE_CLASS || t == MONO_TYPE_STRING ||
			   t == MONO_TYPE_CLASS) { 
			klass = mono_defaults.object_class;
		} else if (t == MONO_TYPE_PTR || t == MONO_TYPE_FNPTR) {
			klass = mono_defaults.int_class;
		} else if (t == MONO_TYPE_GENERICINST) {
			if (mono_type_generic_inst_is_valuetype (type))
				klass = mono_class_from_mono_type (type);
			else
				klass = mono_defaults.object_class;
		} else {
			klass = mono_class_from_mono_type (type);			
		}
	} else {
		klass = mono_defaults.int_class;
	}

	cache = get_cache (&klass->image->ldflda_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	/* we add the %p pointer value of klass because class names are not unique */
	name = g_strdup_printf ("__ldflda_wrapper_%p_%s.%s", klass, klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_LDFLDA);
	g_free (name);

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &mono_defaults.int_class->byval_arg;
	sig->ret = &mono_defaults.int_class->byval_arg;

	/* if typeof (this) != transparent_proxy goto pos0 */
	mono_mb_emit_ldarg (mb, 0);
	pos0 = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	/* if same_appdomain goto pos1 */
	mono_mb_emit_ldarg (mb, 0);
	pos1 = mono_mb_emit_xdomain_check (mb, CEE_BEQ);

	mono_mb_emit_exception_full (mb, "System", "InvalidOperationException", "Attempt to load field address from object in another appdomain.");

	/* same app domain */
	mono_mb_patch_branch (mb, pos1);

	/* if typeof (this) != contextbound goto pos2 */
	mono_mb_emit_ldarg (mb, 0);
	pos2 = mono_mb_emit_contextbound_check (mb, CEE_BEQ);

	/* if this->rp->context == mono_context_get goto pos3 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoRealProxy, context));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_icall (mb, mono_context_get);
	pos3 = mono_mb_emit_branch (mb, CEE_BEQ);

	mono_mb_emit_exception_full (mb, "System", "InvalidOperationException", "Attempt to load field address from object in another context.");

	mono_mb_patch_branch (mb, pos2);
	mono_mb_patch_branch (mb, pos3);

	/* return the address of the field from this->rp->unwrapped_server */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoRealProxy, unwrapped_server));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_RET);

	/* not a proxy: return the address of the field directly */
	mono_mb_patch_branch (mb, pos0);

	mono_mb_emit_ldarg (mb, 0);
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (cache, klass,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

/*
 * mono_marshal_get_stfld_remote_wrapper:
 * klass: The type of the field
 *
 *  This function generates a wrapper for calling mono_store_remote_field_new
 * with the appropriate signature.
 * Similarly to mono_marshal_get_ldfld_remote_wrapper () this doesn't depend on the
 * klass argument anymore.
 */
MonoMethod *
mono_marshal_get_stfld_remote_wrapper (MonoClass *klass)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	static MonoMethod *cached = NULL;

	mono_marshal_lock ();
	if (cached) {
		mono_marshal_unlock ();
		return cached;
	}
	mono_marshal_unlock ();

	mb = mono_mb_new_no_dup_name (mono_defaults.object_class, "__mono_store_remote_field_new_wrapper", MONO_WRAPPER_STFLD_REMOTE);

	mb->method->save_lmf = 1;

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &mono_defaults.object_class->byval_arg;
	sig->ret = &mono_defaults.void_class->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldarg (mb, 3);

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &mono_defaults.object_class->byval_arg;
	csig->ret = &mono_defaults.void_class->byval_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_store_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);
 
	mono_marshal_lock ();
	res = cached;
	mono_marshal_unlock ();
	if (!res) {
		MonoMethod *newm;
		newm = mono_mb_create_method (mb, sig, 6);
		mono_marshal_lock ();
		res = cached;
		if (!res) {
			res = newm;
			cached = res;
			mono_marshal_unlock ();
		} else {
			mono_marshal_unlock ();
			mono_free_method (newm);
		}
	}
	mono_mb_free (mb);
	
	return res;
}

/*
 * mono_marshal_get_stfld_wrapper:
 * @type: the type of the field
 *
 * This method generates a function which can be use to store a field with type
 * @type. The generated function has the following signature:
 * void stfld_wrapper (MonoObject *this, MonoClass *class, MonoClassField *field, int offset, <@type> val)
 */
MonoMethod *
mono_marshal_get_stfld_wrapper (MonoType *type)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	MonoClass *klass;
	GHashTable *cache;
	char *name;
	int t, pos;

	type = mono_type_get_underlying_type (type);
	t = type->type;

	if (!type->byref) {
		if (type->type == MONO_TYPE_SZARRAY) {
			klass = mono_defaults.array_class;
		} else if (type->type == MONO_TYPE_VALUETYPE) {
			klass = type->data.klass;
		} else if (t == MONO_TYPE_OBJECT || t == MONO_TYPE_CLASS || t == MONO_TYPE_STRING) {
			klass = mono_defaults.object_class;
		} else if (t == MONO_TYPE_PTR || t == MONO_TYPE_FNPTR) {
			klass = mono_defaults.int_class;
		} else if (t == MONO_TYPE_GENERICINST) {
			if (mono_type_generic_inst_is_valuetype (type))
				klass = mono_class_from_mono_type (type);
			else
				klass = mono_defaults.object_class;
		} else {
			klass = mono_class_from_mono_type (type);			
		}
	} else {
		klass = mono_defaults.int_class;
	}

	cache = get_cache (&klass->image->stfld_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	/* we add the %p pointer value of klass because class names are not unique */
	name = g_strdup_printf ("__stfld_wrapper_%p_%s.%s", klass, klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_STFLD);
	g_free (name);

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 5);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &mono_defaults.int_class->byval_arg;
	sig->params [4] = &klass->byval_arg;
	sig->ret = &mono_defaults.void_class->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	pos = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldarg (mb, 4);
	if (klass->valuetype)
		mono_mb_emit_op (mb, CEE_BOX, klass);

	mono_mb_emit_managed_call (mb, mono_marshal_get_stfld_remote_wrapper (klass), NULL);

	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_patch_branch (mb, pos);

	mono_mb_emit_ldarg (mb, 0);
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_ldarg (mb, 4);

	switch (t) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_STRING:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		mono_mb_emit_byte (mb, mono_type_to_stind (type));
		break;
	case MONO_TYPE_VALUETYPE:
		g_assert (!klass->enumtype);
		mono_mb_emit_op (mb, CEE_STOBJ, klass);
		break;
	case MONO_TYPE_GENERICINST:
		mono_mb_emit_op (mb, CEE_STOBJ, klass);
		break;
	default:
		g_warning ("type %x not implemented", type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (cache, klass,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

/*
 * generates IL code for the icall wrapper (the generated method
 * calls the unmanaged code in func)
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_icall_wrapper (MonoMethodSignature *sig, const char *name, gconstpointer func, gboolean check_exceptions)
{
	MonoMethodSignature *csig, *csig2;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i;
	WrapperInfo *info;
	
	g_assert (sig->pinvoke);

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	/* Add an explicit this argument */
	if (sig->hasthis)
		csig2 = signature_dup_add_this (mono_defaults.corlib, sig, mono_defaults.object_class);
	else
		csig2 = signature_dup (mono_defaults.corlib, sig);

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + sig->hasthis);

	mono_mb_emit_native_call (mb, csig2, (gpointer) func);
	if (check_exceptions)
		emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);

	csig = signature_dup (mono_defaults.corlib, sig);
	csig->pinvoke = 0;
	if (csig->call_convention == MONO_CALL_VARARG)
		csig->call_convention = 0;

	res = mono_mb_create_method (mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_ICALL_WRAPPER);
	mono_marshal_set_wrapper_info (res, info);
	
	return res;
}

static int
emit_marshal_custom (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
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
		ICustomMarshaler = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "ICustomMarshaler");
		if (!ICustomMarshaler) {
			exception_msg = g_strdup ("Current profile doesn't support ICustomMarshaler");
			goto handle_exception;
		}

		cleanup_native = mono_class_get_method_from_name (ICustomMarshaler, "CleanUpNativeData", 1);
		g_assert (cleanup_native);
		cleanup_managed = mono_class_get_method_from_name (ICustomMarshaler, "CleanUpManagedData", 1);
		g_assert (cleanup_managed);
		marshal_managed_to_native = mono_class_get_method_from_name (ICustomMarshaler, "MarshalManagedToNative", 1);
		g_assert (marshal_managed_to_native);
		marshal_native_to_managed = mono_class_get_method_from_name (ICustomMarshaler, "MarshalNativeToManaged", 1);
		g_assert (marshal_native_to_managed);
	}

	mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, m->image);
	g_assert (mtype != NULL);
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
			g_free (exception_msg);

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
}

static int
emit_marshal_asany (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
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

	return conv_arg;
}

static int
emit_marshal_vtype (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec, 
					int conv_arg, MonoType **conv_arg_type, 
					MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass, *date_time_class;
	int pos = 0, pos2;

	klass = mono_class_from_mono_type (t);

	date_time_class = mono_class_from_name_cached (mono_defaults.corlib, "System", "DateTime");

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

		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
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

			if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
				klass->blittable || klass->enumtype)
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

		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype) {
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

		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
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
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable) {
			mono_mb_emit_stloc (mb, 3);
			break;
		}

		/* load pointer to returned value type */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
		/* store the address of the source into local variable 0 */
		mono_mb_emit_stloc (mb, 0);
		/* set dst_ptr */
		mono_mb_emit_ldloc_addr (mb, 3);
		mono_mb_emit_stloc (mb, 1);
				
		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype) {
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
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype) {
			break;
		}

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
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype) {
			mono_mb_emit_stloc (mb, 3);
			m->retobj_var = 0;
			break;
		}
			
		/* load pointer to returned value type */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			
		/* store the address of the source into local variable 0 */
		mono_mb_emit_stloc (mb, 0);
		/* allocate space for the native struct and
		 * store the address into dst_ptr */
		m->retobj_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		m->retobj_class = klass;
		g_assert (m->retobj_var);
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, mono_marshal_alloc);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_stloc (mb, m->retobj_var);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, FALSE);
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_string (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec, 
					 int conv_arg, MonoType **conv_arg_type, 
					 MarshalAction action)
{
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

		if (conv == -1) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			MonoException *exc = mono_get_exception_not_implemented (msg);
			g_warning ("%s", msg);
			g_free (msg);
			mono_raise_exception (exc);
		}
		else
			mono_mb_emit_icall (mb, conv_to_icall (conv));

		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT:
		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == -1) {
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
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, conv_to_icall (conv));
			mono_mb_emit_byte (mb, CEE_STIND_REF);
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
		if (conv == -1) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
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
		if (conv == -1) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		if (t->byref) {
			if (conv_arg) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, conv_to_icall (conv));
				mono_mb_emit_byte (mb, CEE_STIND_I);
			}
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		if (conv_to_icall (conv) == mono_marshal_string_to_utf16)
			/* We need to make a copy so the caller is able to free it */
			mono_mb_emit_icall (mb, mono_marshal_string_to_utf16_copy);
		else
			mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_safehandle (EmitMarshalContext *m, int argnum, MonoType *t, 
			 MonoMarshalSpec *spec, int conv_arg, 
			 MonoType **conv_arg_type, MarshalAction action)
{
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
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoSafeHandle, handle));
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
			mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoSafeHandle, handle));
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
		
		if (t->data.klass->flags & TYPE_ATTRIBUTE_ABSTRACT){
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
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoSafeHandle, handle));
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

	return conv_arg;
}

static int
emit_marshal_handleref (EmitMarshalContext *m, int argnum, MonoType *t, 
			MonoMarshalSpec *spec, int conv_arg, 
			MonoType **conv_arg_type, MarshalAction action)
{
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
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoHandleRef, handle));
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

	return conv_arg;
}

static int
emit_marshal_object (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec, 
		     int conv_arg, MonoType **conv_arg_type, 
		     MarshalAction action)
{
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
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN));
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

			if (t->byref && !t->attrs & PARAM_ATTRIBUTE_IN && t->attrs & PARAM_ATTRIBUTE_OUT)
				break;

			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);

			if (conv != -1)
				mono_mb_emit_icall (mb, conv_to_icall (conv));
			else {
				char *msg = g_strdup_printf ("stringbuilder marshalling conversion %d not implemented", encoding);
				MonoException *exc = mono_get_exception_not_implemented (msg);
				g_warning ("%s", msg);
				g_free (msg);
				mono_raise_exception (exc);
			}

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
				default:
					g_assert_not_reached ();
				}

				mono_mb_emit_byte (mb, CEE_STIND_REF);
			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, conv_arg);

				mono_mb_emit_icall (mb, conv_to_icall (conv));
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
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL));
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
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL));
			mono_mb_emit_stloc (mb, 3);
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
			g_assert (!t->byref);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_CLASSCONST, klass);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL));
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		if (klass == mono_defaults.stringbuilder_class) {
			MonoMarshalNative encoding;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);

			// FIXME:
			g_assert (encoding == MONO_NATIVE_LPSTR);

			g_assert (!t->byref);
			g_assert (encoding != -1);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, mono_string_utf8_to_builder2);
			mono_mb_emit_stloc (mb, conv_arg);
			break;
		}

		/* The class can not have an automatic layout */
		if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
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
			mono_mb_emit_icall (mb, mono_marshal_alloc);
			mono_mb_emit_stloc (mb, 1);
			
			/* Update argument pointer */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_STIND_I);
		
			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_branch (mb, pos2);
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
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN));
			mono_mb_emit_stloc (mb, 3);
			break;
		}

		/* The class can not have an automatic layout */
		if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
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
		mono_mb_emit_icall (mb, mono_marshal_alloc);
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_stloc (mb, 3);

		emit_struct_conv (mb, klass, FALSE);

		mono_mb_patch_branch (mb, pos2);
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static int
emit_marshal_variant (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec, 
		     int conv_arg, MonoType **conv_arg_type, 
		     MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	static MonoMethod *get_object_for_native_variant = NULL;
	static MonoMethod *get_native_variant_for_object = NULL;

	mono_init_com_types ();
	
	if (!get_object_for_native_variant)
		get_object_for_native_variant = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1);
	g_assert (get_object_for_native_variant);

	if (!get_native_variant_for_object)
		get_native_variant_for_object = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetNativeVariantForObject", 2);
	g_assert (get_native_variant_for_object);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		conv_arg = mono_mb_add_local (mb, &mono_defaults.variant_class->byval_arg);
		
		if (t->byref)
			*conv_arg_type = &mono_defaults.variant_class->this_arg;
		else
			*conv_arg_type = &mono_defaults.variant_class->byval_arg;

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
			variant_clear = mono_class_get_method_from_name (mono_defaults.variant_class, "Clear", 0);
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
			*conv_arg_type = &mono_defaults.variant_class->this_arg;
		else
			*conv_arg_type = &mono_defaults.variant_class->byval_arg;

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
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_ARRAY_LPARRAY));
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
				conv = -1;

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

			if (is_string) {
				if (conv == -1) {
					char *msg = g_strdup_printf ("string/stringbuilder marshalling conversion %d not implemented", encoding);
					MonoException *exc = mono_get_exception_not_implemented (msg);
					g_warning ("%s", msg);
					g_free (msg);
					mono_raise_exception (exc);
				}
			}

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
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_ldloc (mb, src_var);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);
				mono_mb_emit_icall (mb, conv_to_icall (conv));
				mono_mb_emit_byte (mb, CEE_STIND_I);
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
				emit_struct_conv_full (mb, eklass, FALSE, eklass == mono_defaults.char_class ? encoding : -1);
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

				g_assert (conv != -1);

				/* dest */
				mono_mb_emit_ldarg (mb, argnum);
				if (t->byref)
					mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_byte (mb, CEE_LDELEM_REF);

				/* src */
				mono_mb_emit_ldloc (mb, src_ptr);
				mono_mb_emit_byte (mb, CEE_LDIND_I);

				mono_mb_emit_icall (mb, conv_to_icall (conv));

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
					emit_struct_conv_full (mb, eklass, TRUE, eklass == mono_defaults.char_class ? encoding : -1);
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
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_FREE_LPARRAY));
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
		int index_var, src_ptr, loc, esize, param_num, num_elem;
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
			conv = -1;

		mono_marshal_load_type_info (eklass);

		if (is_string)
			esize = sizeof (gpointer);
		else
			esize = mono_class_native_size (eklass, NULL);
		src_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

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
			mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_ldarg (mb, argnum);
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
			g_assert (conv != -1);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_ldloc (mb, src_ptr);
			mono_mb_emit_byte (mb, CEE_LDIND_I);

			mono_mb_emit_icall (mb, conv_to_icall (conv));
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
		int index_var, dest_ptr, loc, esize, param_num, num_elem;
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
			conv = -1;

		mono_marshal_load_type_info (eklass);

		if (is_string)
			esize = sizeof (gpointer);
		else
			esize = mono_class_native_size (eklass, NULL);

		dest_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

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
			mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
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
			g_assert (conv != -1);

			/* dest */
			mono_mb_emit_ldloc (mb, dest_ptr);

			/* src */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_byte (mb, CEE_LDELEM_REF);

			mono_mb_emit_icall (mb, conv_to_icall (conv));
			mono_mb_emit_byte (mb, CEE_STIND_I);
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
		MonoMarshalConv conv = -1;
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
		mono_mb_emit_icall (mb, mono_marshal_alloc);
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
			g_assert (conv != -1);

			/* dest */
			mono_mb_emit_ldloc (mb, dest);

			/* src */
			mono_mb_emit_ldloc (mb, src);
			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_byte (mb, CEE_LDELEM_REF);

			mono_mb_emit_icall (mb, conv_to_icall (conv));
			mono_mb_emit_byte (mb, CEE_STIND_I);
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

	return conv_arg;
}

static int
emit_marshal_boolean (EmitMarshalContext *m, int argnum, MonoType *t,
		      MonoMarshalSpec *spec, 
		      int conv_arg, MonoType **conv_arg_type, 
		      MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		MonoType *local_type;
		int label_false;
		guint8 ldc_op = CEE_LDC_I4_1;

		if (spec == NULL) {
			local_type = &mono_defaults.int32_class->byval_arg;
		} else {
			switch (spec->native) {
			case MONO_NATIVE_I1:
			case MONO_NATIVE_U1:
				local_type = &mono_defaults.byte_class->byval_arg;
				break;
			case MONO_NATIVE_VARIANTBOOL:
				local_type = &mono_defaults.int16_class->byval_arg;
				ldc_op = CEE_LDC_I4_M1;
				break;
			case MONO_NATIVE_BOOLEAN:
				local_type = &mono_defaults.int32_class->byval_arg;
				break;
			default:
				g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
				local_type = &mono_defaults.int32_class->byval_arg;
				break;
			}
		}
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

		conv_arg = mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

		if (spec) {
			switch (spec->native) {
			case MONO_NATIVE_I1:
			case MONO_NATIVE_U1:
				conv_arg_class = mono_defaults.byte_class;
				ldop = CEE_LDIND_I1;
				break;
			case MONO_NATIVE_VARIANTBOOL:
				conv_arg_class = mono_defaults.int16_class;
				ldop = CEE_LDIND_I2;
				break;
			case MONO_NATIVE_BOOLEAN:
				break;
			default:
				g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
			}
		}

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

	return conv_arg;
}

static int
emit_marshal_ptr (EmitMarshalContext *m, int argnum, MonoType *t, 
		  MonoMarshalSpec *spec, int conv_arg, 
		  MonoType **conv_arg_type, MarshalAction action)
{
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

	return conv_arg;
}

static int
emit_marshal_char (EmitMarshalContext *m, int argnum, MonoType *t, 
		   MonoMarshalSpec *spec, int conv_arg, 
		   MonoType **conv_arg_type, MarshalAction action)
{
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

	return conv_arg;
}

static int
emit_marshal_scalar (EmitMarshalContext *m, int argnum, MonoType *t, 
		     MonoMarshalSpec *spec, int conv_arg, 
		     MonoType **conv_arg_type, MarshalAction action)
{
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

	return conv_arg;
}

static int
emit_marshal (EmitMarshalContext *m, int argnum, MonoType *t, 
	      MonoMarshalSpec *spec, int conv_arg, 
	      MonoType **conv_arg_type, MarshalAction action)
{
	/* Ensure that we have marshalling info for this param */
	mono_marshal_load_type_info (mono_class_from_mono_type (t));

#ifdef DISABLE_JIT
	/* Not JIT support, no need to generate correct IL */
	return conv_arg;
#else
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
		if (spec && spec->native == MONO_NATIVE_STRUCT)
			return emit_marshal_variant (m, argnum, t, spec, conv_arg, conv_arg_type, action);

#ifndef DISABLE_COM
		if (spec && (spec->native == MONO_NATIVE_IUNKNOWN ||
			spec->native == MONO_NATIVE_IDISPATCH ||
			spec->native == MONO_NATIVE_INTERFACE))
			return mono_cominterop_emit_marshal_com_interface (m, argnum, t, spec, conv_arg, conv_arg_type, action);
		if (spec && (spec->native == MONO_NATIVE_SAFEARRAY) && 
			(spec->data.safearray_data.elem_type == MONO_VARIANT_VARIANT) && 
			((action == MARSHAL_ACTION_CONV_OUT) || (action == MARSHAL_ACTION_CONV_IN) || (action == MARSHAL_ACTION_PUSH)))
			return mono_cominterop_emit_marshal_safearray (m, argnum, t, spec, conv_arg, conv_arg_type, action);
#endif

		if (mono_defaults.safehandle_class != NULL && t->data.klass &&
		    mono_class_is_subclass_of (t->data.klass,  mono_defaults.safehandle_class, FALSE))
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
	}
#endif

	return conv_arg;
}

/**
 * mono_marshal_emit_native_wrapper:
 * @image: the image to use for looking up custom marshallers
 * @sig: The signature of the native function
 * @piinfo: Marshalling information
 * @mspecs: Marshalling information
 * @aot: whenever the created method will be compiled by the AOT compiler
 * @method: if non-NULL, the pinvoke method to call
 * @check_exceptions: Whenever to check for pending exceptions after the native call
 * @func_param: the function to call is passed as a boxed IntPtr as the first parameter
 *
 * generates IL code for the pinvoke wrapper, the generated code calls @func.
 */
void
mono_marshal_emit_native_wrapper (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, gboolean aot, gboolean check_exceptions, gboolean func_param)
{
	EmitMarshalContext m;
	MonoMethodSignature *csig;
	MonoClass *klass;
	int i, argnum, *tmp_locals;
	int type, param_shift = 0;
	static MonoMethodSignature *get_last_error_sig = NULL;

	m.mb = mb;
	m.piinfo = piinfo;

	/* we copy the signature, so that we can set pinvoke to 0 */
	if (func_param) {
		/* The function address is passed as the first argument */
		g_assert (!sig->hasthis);
		param_shift += 1;
	}
	csig = signature_dup (mb->method->klass->image, sig);
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

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

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
	tmp_locals = alloca (sizeof (int) * sig->param_count);
	m.orig_conv_args = alloca (sizeof (int) * (sig->param_count + 1));

	for (i = 0; i < sig->param_count; i ++) {
		tmp_locals [i] = emit_marshal (&m, i + param_shift, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_CONV_IN);
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
		mono_mb_emit_cominterop_call (mb, csig, &piinfo->method);
#else
		g_assert_not_reached ();
#endif
	}
	else {
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
		if (!get_last_error_sig) {
			get_last_error_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
			get_last_error_sig->ret = &mono_defaults.int_class->byval_arg;
			get_last_error_sig->pinvoke = 1;
		}

#ifdef TARGET_WIN32
		/* 
		 * Have to call GetLastError () early and without a wrapper, since various runtime components could
		 * clobber its value.
		 */
		mono_mb_emit_native_call (mb, get_last_error_sig, GetLastError);
		mono_mb_emit_icall (mb, mono_marshal_set_last_error_windows);
#else
		mono_mb_emit_icall (mb, mono_marshal_set_last_error);
#endif
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
		}
	}

	if (!MONO_TYPE_IS_VOID(sig->ret))
		mono_mb_emit_ldloc (mb, 3);

	mono_mb_emit_byte (mb, CEE_RET);
}

G_GNUC_UNUSED static void
code_for (MonoMethod *method) {
	MonoMethodHeader *header = mono_method_get_header (method);
	printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (method, TRUE), mono_disasm_code (0, method, header->code, header->code + header->code_size));
}

/**
 * mono_marshal_get_native_wrapper:
 * @method: The MonoMethod to wrap.
 * @check_exceptions: Whenever to check for pending exceptions
 *
 * generates IL code for the pinvoke wrapper (the generated method
 * calls the unmanaged code in piinfo->addr)
 * The wrapper info for the wrapper is a WrapperInfo structure.
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

	if (aot)
		cache = get_cache (&method->klass->image->native_wrapper_aot_cache, mono_aligned_addr_hash, NULL);
	else
		cache = get_cache (&method->klass->image->native_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	if (MONO_CLASS_IS_IMPORT (method->klass)) {
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
		if (pinvoke)
			if (method->iflags & METHOD_IMPL_ATTRIBUTE_NATIVE)
				exc_arg = "Method contains unsupported native code";
			else
				mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
		else
			piinfo->addr = mono_lookup_internal_call (method);
	}

	/* hack - redirect certain string constructors to CreateString */
	if (piinfo->addr == ves_icall_System_String_ctor_RedirectToCreateString) {
		g_assert (!pinvoke);
		g_assert (method->string_ctor);
		g_assert (sig->hasthis);

		/* CreateString returns a value */
		csig = signature_dup (method->klass->image, sig);
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

				mono_mb_emit_byte (mb, CEE_LDARG_0);
				for (i = 1; i <= csig->param_count; i++)
					mono_mb_emit_ldarg (mb, i);
				mono_mb_emit_managed_call (mb, res, NULL);
				mono_mb_emit_byte (mb, CEE_RET);

				/* use native_wrapper_cache because internal calls are looked up there */
				res = mono_mb_create_and_cache (cache, method,
					mb, csig, csig->param_count + 1);

				mono_mb_free (mb);

				info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_STRING_CTOR);
				info->d.string_ctor.method = method;

				mono_marshal_set_wrapper_info (res, info);

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
		mono_mb_emit_exception (mb, exc_class, exc_arg);
		csig = signature_dup (method->klass->image, sig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache (cache, method,
										mb, csig, csig->param_count + 16);
		mono_mb_free (mb);
		return res;
	}

	/* internal calls: we simply push all arguments and call the method (no conversions) */
	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {
		if (sig->hasthis)
			csig = signature_dup_add_this (method->klass->image, sig, method->klass);
		else
			csig = signature_dup (method->klass->image, sig);

		/* hack - string constructors returns a value */
		if (method->string_ctor)
			csig->ret = &mono_defaults.string_class->byval_arg;

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

			mono_mb_emit_byte (mb, CEE_LDARG_0);
		}

		for (i = 0; i < sig->param_count; i++)
			mono_mb_emit_ldarg (mb, i + sig->hasthis);

		if (aot) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_op (mb, CEE_MONO_ICALL_ADDR, &piinfo->method);
			mono_mb_emit_calli (mb, csig);
		} else {
			g_assert (piinfo->addr);
			mono_mb_emit_native_call (mb, csig, piinfo->addr);
		}
		if (check_exceptions)
			emit_thread_interrupt_checkpoint (mb);
		mono_mb_emit_byte (mb, CEE_RET);

		csig = signature_dup (method->klass->image, csig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache (cache, method,
										mb, csig, csig->param_count + 16);

		info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_NONE);
		info->d.managed_to_native.method = method;
		mono_marshal_set_wrapper_info (res, info);

		mono_mb_free (mb);
		return res;
	}

	g_assert (pinvoke);
	if (!aot)
		g_assert (piinfo->addr);

	mspecs = g_new (MonoMarshalSpec*, sig->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	mono_marshal_emit_native_wrapper (mb->method->klass->image, mb, sig, piinfo, mspecs, piinfo->addr, aot, check_exceptions, FALSE);

	csig = signature_dup (method->klass->image, sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, method,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_NONE);
	info->d.managed_to_native.method = method;
	mono_marshal_set_wrapper_info (res, info);

	for (i = sig->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* code_for (res); */

	return res;
}

/**
 * mono_marshal_get_native_func_wrapper:
 * @image: The image to use for memory allocation and for looking up custom marshallers.
 * @sig: The signature of the function
 * @func: The native function to wrap
 *
 *   Returns a wrapper method around native functions, similar to the pinvoke
 * wrapper.
 */
MonoMethod *
mono_marshal_get_native_func_wrapper (MonoImage *image, MonoMethodSignature *sig, 
									  MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func)
{
	MonoMethodSignature *csig;

	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;

	cache = get_cache (&image->native_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, func)))
		return res;

	name = g_strdup_printf ("wrapper_native_%p", func);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	mb->method->save_lmf = 1;

	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, func, FALSE, TRUE, FALSE);

	csig = signature_dup (image, sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, func,
									mb, csig, csig->param_count + 16);
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
	cache = get_cache (&image->native_func_wrapper_aot_cache, mono_aligned_addr_hash, NULL);
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

	mono_marshal_emit_native_wrapper (image, mb, sig, piinfo, mspecs, NULL, FALSE, TRUE, TRUE);

	g_assert (!sig->hasthis);
	csig = signature_dup_add_this (image, sig, mono_defaults.int_class);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, invoke,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_NATIVE_FUNC_AOT);
	info->d.managed_to_native.method = invoke;

	mono_marshal_set_wrapper_info (res, info);

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
	MonoMethodSignature *sig, *csig;
	int i, *tmp_locals;
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

	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	/*
	 * Might need to attach the thread to the JIT or change the
	 * domain for the callback.
	 */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_JIT_ATTACH);

	/* we first do all conversions */
	tmp_locals = alloca (sizeof (int) * sig->param_count);
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

	emit_thread_interrupt_checkpoint (mb);

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

	mono_mb_emit_managed_call (mb, method, NULL);

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

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_JIT_DETACH);

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

	if (closed)
		g_free (sig);
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
			MonoClass *cmod_class = mono_class_get (method->klass->image, sig->ret->modifiers [i].token);
			g_assert (cmod_class);
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

/*
 * generates IL code to call managed methods from unmanaged code 
 * If target_handle==0, the wrapper info will be a WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, uint32_t target_handle)
{
	static MonoClass *UnmanagedFunctionPointerAttribute;
	MonoMethodSignature *sig, *csig, *invoke_sig;
	MonoMethodBuilder *mb;
	MonoMethod *res, *invoke;
	MonoMarshalSpec **mspecs;
	MonoMethodPInvoke piinfo;
	GHashTable *cache;
	int i;
	EmitMarshalContext m;

	g_assert (method != NULL);
	g_assert (!mono_method_signature (method)->pinvoke);

	/* 
	 * FIXME: Should cache the method+delegate type pair, since the same method
	 * could be called with different delegates, thus different marshalling
	 * options.
	 */
	cache = get_cache (&method->klass->image->managed_wrapper_cache, mono_aligned_addr_hash, NULL);
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
		csig = signature_dup (method->klass->image, invoke_sig);
	csig->hasthis = 0;
	csig->pinvoke = 1;

	m.mb = mb;
	m.sig = sig;
	m.piinfo = NULL;
	m.retobj_var = 0;
	m.csig = csig;
	m.image = method->klass->image;

	mono_marshal_set_callconv_from_modopt (invoke, csig);

	/* Handle the UnmanagedFunctionPointerAttribute */
	if (!UnmanagedFunctionPointerAttribute)
		UnmanagedFunctionPointerAttribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");

	/* The attribute is only available in Net 2.0 */
	if (UnmanagedFunctionPointerAttribute) {
		MonoCustomAttrInfo *cinfo;
		MonoCustomAttrEntry *attr;

		/* 
		 * The pinvoke attributes are stored in a real custom attribute. Obtain the
		 * contents of the attribute without constructing it, as that might not be
		 * possible when running in cross-compiling mode.
		 */
		cinfo = mono_custom_attrs_from_class (delegate_klass);
		attr = NULL;
		if (cinfo) {
			for (i = 0; i < cinfo->num_attrs; ++i) {
				MonoClass *ctor_class = cinfo->attrs [i].ctor->klass;
				if (mono_class_has_parent (ctor_class, UnmanagedFunctionPointerAttribute)) {
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
			MonoBoolean best_fit_mapping = 0;
			MonoBoolean throw_on_unmappable = 0;

			mono_reflection_create_custom_attr_data_args (mono_defaults.corlib, attr->ctor, attr->data, attr->data_size, &typed_args, &named_args, &arginfo);

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
					best_fit_mapping = *(MonoBoolean*)mono_object_unbox (o);
				} else if (!strcmp (narg->field->name, "ThrowOnUnmappableChar")) {
					throw_on_unmappable = *(MonoBoolean*)mono_object_unbox (o);
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

		res = mono_mb_create_and_cache (cache, method,
											 mb, csig, sig->param_count + 16);
		// FIXME: Associate it with the method+delegate_klass pair
		info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_NONE);
		info->d.native_to_managed.method = method;
		info->d.native_to_managed.klass = delegate_klass;
		mono_marshal_set_wrapper_info (res, info);
	} else {
		mb->dynamic = 1;
		res = mono_mb_create_method (mb, csig, sig->param_count + 16);
	}
	mono_mb_free (mb);

	for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* code_for (res); */

	return res;
}

gpointer
mono_marshal_get_vtfixup_ftnptr (MonoImage *image, guint32 token, guint16 type)
{
	MonoMethod *method;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	int i, param_count;

	g_assert (token);

	method = mono_get_method (image, token, NULL);
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
		csig = signature_dup (image, sig);
		csig->hasthis = 0;
		csig->pinvoke = 1;

		m.mb = mb;
		m.sig = sig;
		m.piinfo = NULL;
		m.retobj_var = 0;
		m.csig = csig;
		m.image = image;

		mono_marshal_set_callconv_from_modopt (method, csig);

		/* FIXME: Implement VTFIXUP_TYPE_FROM_UNMANAGED_RETAIN_APPDOMAIN. */

		mono_marshal_emit_managed_wrapper (mb, sig, mspecs, &m, method, 0);

		mb->dynamic = 1;
		method = mono_mb_create_method (mb, csig, sig->param_count + 16);
		mono_mb_free (mb);

		for (i = sig->param_count; i >= 0; i--)
			if (mspecs [i])
				mono_metadata_free_marshal_spec (mspecs [i]);
		g_free (mspecs);

		return mono_compile_method (method);
	}

	sig = mono_method_signature (method);
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_MANAGED);

	param_count = sig->param_count + sig->hasthis;
	for (i = 0; i < param_count; i++)
		mono_mb_emit_ldarg (mb, i);

	if (type & VTFIXUP_TYPE_CALL_MOST_DERIVED)
		mono_mb_emit_op (mb, CEE_CALLVIRT, method);
	else
		mono_mb_emit_op (mb, CEE_CALL, method);
	mono_mb_emit_byte (mb, CEE_RET);

	mb->dynamic = 1;
	method = mono_mb_create_method (mb, sig, param_count);
	mono_mb_free (mb);

	return mono_compile_method (method);
}

static MonoReflectionType *
type_from_handle (MonoType *handle)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *klass = mono_class_from_mono_type (handle);

	MONO_ARCH_SAVE_REGS;

	mono_class_init (klass);
	return mono_type_get_object (domain, handle);
}

/*
 * This does the equivalent of mono_object_castclass_with_cache.
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_castclass_with_cache (void)
{
	static MonoMethod *cached;
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	int return_null_pos, cache_miss_pos, invalid_cast_pos;
	WrapperInfo *info;

	if (cached)
		return cached;

	mb = mono_mb_new (mono_defaults.object_class, "__castclass_with_cache", MONO_WRAPPER_CASTCLASS);
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->ret = &mono_defaults.object_class->byval_arg;
	sig->pinvoke = 0;

	/* allocate local 0 (pointer) obj_vtable */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/*if (!obj)*/
	mono_mb_emit_ldarg (mb, 0);
	return_null_pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*obj_vtable = obj->vtable;*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 0);

	/* *cache */
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldloc (mb, 0);

	/*if (*cache == obj_vtable)*/
	cache_miss_pos = mono_mb_emit_branch (mb, CEE_BNE_UN);

	/*return obj;*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_patch_branch (mb, cache_miss_pos);
	/*if (mono_object_isinst (obj, klass)) */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_icall (mb, mono_object_isinst);
	invalid_cast_pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/**cache = obj_vtable;*/
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/*return obj;*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);

	/*fails*/
	mono_mb_patch_branch (mb, invalid_cast_pos);
	mono_mb_emit_exception (mb, "InvalidCastException", NULL);

	/*return null*/
	mono_mb_patch_branch (mb, return_null_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, sig, 8);
	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_CASTCLASS_WITH_CACHE);
	mono_marshal_set_wrapper_info (res, info);
	STORE_STORE_FENCE;

	if (InterlockedCompareExchangePointer ((volatile gpointer *)&cached, res, NULL)) {
		mono_free_method (res);
		mono_metadata_free_method_signature (sig);
	}
	mono_mb_free (mb);

	return cached;
}

/*
 * This does the equivalent of mono_object_isinst_with_cache.
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_isinst_with_cache (void)
{
	static MonoMethod *cached;
	MonoMethod *res;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	int return_null_pos, cache_miss_pos, cache_hit_pos, not_an_instance_pos, negative_cache_hit_pos;
	WrapperInfo *info;

	if (cached)
		return cached;

	mb = mono_mb_new (mono_defaults.object_class, "__isinst_with_cache", MONO_WRAPPER_CASTCLASS);
	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->ret = &mono_defaults.object_class->byval_arg;
	sig->pinvoke = 0;

	/* allocate local 0 (pointer) obj_vtable */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) cached_vtable */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/*if (!obj)*/
	mono_mb_emit_ldarg (mb, 0);
	return_null_pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/*obj_vtable = obj->vtable;*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 0);

	/* cached_vtable = *cache*/
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, 1);

	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_LDC_I4);
	mono_mb_emit_i4 (mb, ~0x1);
	mono_mb_emit_byte (mb, CEE_CONV_U);
	mono_mb_emit_byte (mb, CEE_AND);
	mono_mb_emit_ldloc (mb, 0);
	/*if ((cached_vtable & ~0x1)== obj_vtable)*/
	cache_miss_pos = mono_mb_emit_branch (mb, CEE_BNE_UN);

	/*return (cached_vtable & 0x1) ? NULL : obj;*/
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte(mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, CEE_CONV_U);
	mono_mb_emit_byte (mb, CEE_AND);
	negative_cache_hit_pos = mono_mb_emit_branch (mb, CEE_BRTRUE);

	/*obj*/
	mono_mb_emit_ldarg (mb, 0);
	cache_hit_pos = mono_mb_emit_branch (mb, CEE_BR);

	/*NULL*/
	mono_mb_patch_branch (mb, negative_cache_hit_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);

	mono_mb_patch_branch (mb, cache_hit_pos);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_patch_branch (mb, cache_miss_pos);
	/*if (mono_object_isinst (obj, klass)) */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_icall (mb, mono_object_isinst);
	not_an_instance_pos = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/**cache = obj_vtable;*/
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/*return obj;*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);

	/*not an instance*/
	mono_mb_patch_branch (mb, not_an_instance_pos);
	/* *cache = (gpointer)(obj_vtable | 0x1);*/
	mono_mb_emit_ldarg (mb, 2);
	/*obj_vtable | 0x1*/
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte(mb, CEE_LDC_I4_1);
	mono_mb_emit_byte (mb, CEE_CONV_U);
	mono_mb_emit_byte (mb, CEE_OR);

	/* *cache = ... */
	mono_mb_emit_byte (mb, CEE_STIND_I);

	/*return null*/
	mono_mb_patch_branch (mb, return_null_pos);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, sig, 8);
	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_ISINST_WITH_CACHE);
	mono_marshal_set_wrapper_info (res, info);
	STORE_STORE_FENCE;

	if (InterlockedCompareExchangePointer ((volatile gpointer *)&cached, res, NULL)) {
		mono_free_method (res);
		mono_metadata_free_method_signature (sig);
	}
	mono_mb_free (mb);

	return cached;
}

/*
 * mono_marshal_get_isinst:
 * @klass: the type of the field
 *
 * This method generates a function which can be used to check if an object is
 * an instance of the given type, icluding the case where the object is a proxy.
 * The generated function has the following signature:
 * MonoObject* __isinst_wrapper_ (MonoObject *obj)
 */
MonoMethod *
mono_marshal_get_isinst (MonoClass *klass)
{
	static MonoMethodSignature *isint_sig = NULL;
	GHashTable *cache;
	MonoMethod *res;
	int pos_was_ok, pos_failed, pos_end, pos_end2;
	char *name;
	MonoMethodBuilder *mb;

	cache = get_cache (&klass->image->isinst_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	if (!isint_sig) {
		isint_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		isint_sig->params [0] = &mono_defaults.object_class->byval_arg;
		isint_sig->ret = &mono_defaults.object_class->byval_arg;
		isint_sig->pinvoke = 0;
	}
	
	name = g_strdup_printf ("__isinst_wrapper_%s", klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_ISINST);
	g_free (name);
	
	mb->method->save_lmf = 1;

	/* check if the object is a proxy that needs special cast */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_CISINST, klass);

	/* The result of MONO_ISINST can be:
	   	0) the type check succeeded
		1) the type check did not succeed
		2) a CanCastTo call is needed */
	
	mono_mb_emit_byte (mb, CEE_DUP);
	pos_was_ok = mono_mb_emit_branch (mb, CEE_BRFALSE);

	mono_mb_emit_byte (mb, CEE_LDC_I4_2);
	pos_failed = mono_mb_emit_branch (mb, CEE_BNE_UN);
	
	/* get the real proxy from the transparent proxy*/

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_managed_call (mb, mono_marshal_get_proxy_cancast (klass), NULL);
	pos_end = mono_mb_emit_branch (mb, CEE_BR);
	
	/* fail */
	
	mono_mb_patch_branch (mb, pos_failed);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	pos_end2 = mono_mb_emit_branch (mb, CEE_BR);
	
	/* success */
	
	mono_mb_patch_branch (mb, pos_was_ok);
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_ldarg (mb, 0);
	
	/* the end */
	
	mono_mb_patch_branch (mb, pos_end);
	mono_mb_patch_branch (mb, pos_end2);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, klass, mb, isint_sig, isint_sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/*
 * mono_marshal_get_castclass:
 * @klass: the type of the field
 *
 * This method generates a function which can be used to cast an object to
 * an instance of the given type, icluding the case where the object is a proxy.
 * The generated function has the following signature:
 * MonoObject* __castclass_wrapper_ (MonoObject *obj)
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_castclass (MonoClass *klass)
{
	static MonoMethodSignature *castclass_sig = NULL;
	GHashTable *cache;
	MonoMethod *res;
	int pos_was_ok, pos_was_ok2;
	char *name;
	MonoMethodBuilder *mb;
	WrapperInfo *info;

	cache = get_cache (&klass->image->castclass_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	if (!castclass_sig) {
		castclass_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		castclass_sig->params [0] = &mono_defaults.object_class->byval_arg;
		castclass_sig->ret = &mono_defaults.object_class->byval_arg;
		castclass_sig->pinvoke = 0;
	}
	
	name = g_strdup_printf ("__castclass_wrapper_%s", klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_CASTCLASS);
	g_free (name);
	
	mb->method->save_lmf = 1;

	/* check if the object is a proxy that needs special cast */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_op (mb, CEE_MONO_CCASTCLASS, klass);

	/* The result of MONO_ISINST can be:
	   	0) the cast is valid
		1) cast of unknown proxy type
		or an exception if the cast is is invalid
	*/
	
	pos_was_ok = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/* get the real proxy from the transparent proxy*/

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_managed_call (mb, mono_marshal_get_proxy_cancast (klass), NULL);
	pos_was_ok2 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	
	/* fail */
	mono_mb_emit_exception (mb, "InvalidCastException", NULL);
	
	/* success */
	mono_mb_patch_branch (mb, pos_was_ok);
	mono_mb_patch_branch (mb, pos_was_ok2);
	mono_mb_emit_ldarg (mb, 0);
	
	/* the end */
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, klass, mb, castclass_sig, castclass_sig->param_count + 16);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_NONE);
	mono_marshal_set_wrapper_info (res, info);

	return res;
}

MonoMethod *
mono_marshal_get_proxy_cancast (MonoClass *klass)
{
	static MonoMethodSignature *isint_sig = NULL;
	GHashTable *cache;
	MonoMethod *res;
	int pos_failed, pos_end;
	char *name, *klass_name;
	MonoMethod *can_cast_to;
	MonoMethodDesc *desc;
	MonoMethodBuilder *mb;

	cache = get_cache (&klass->image->proxy_isinst_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	if (!isint_sig) {
		isint_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		isint_sig->params [0] = &mono_defaults.object_class->byval_arg;
		isint_sig->ret = &mono_defaults.object_class->byval_arg;
		isint_sig->pinvoke = 0;
	}

	klass_name = mono_type_full_name (&klass->byval_arg);
	name = g_strdup_printf ("__proxy_isinst_wrapper_%s", klass_name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_PROXY_ISINST);
	g_free (klass_name);
	g_free (name);
	
	mb->method->save_lmf = 1;

	/* get the real proxy from the transparent proxy*/
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	
	/* get the reflection type from the type handle */
	mono_mb_emit_ptr (mb, &klass->byval_arg);
	mono_mb_emit_icall (mb, type_from_handle);
	
	mono_mb_emit_ldarg (mb, 0);
	
	/* make the call to CanCastTo (type, ob) */
	desc = mono_method_desc_new ("IRemotingTypeInfo:CanCastTo", FALSE);
	can_cast_to = mono_method_desc_search_in_class (desc, mono_defaults.iremotingtypeinfo_class);
	g_assert (can_cast_to);
	mono_method_desc_free (desc);
	mono_mb_emit_op (mb, CEE_CALLVIRT, can_cast_to);
	
	pos_failed = mono_mb_emit_branch (mb, CEE_BRFALSE);

	/* Upgrade the proxy vtable by calling: mono_upgrade_remote_class_wrapper (type, ob)*/
	mono_mb_emit_ptr (mb, &klass->byval_arg);
	mono_mb_emit_icall (mb, type_from_handle);
	mono_mb_emit_ldarg (mb, 0);
	
	mono_mb_emit_icall (mb, mono_upgrade_remote_class_wrapper);
	emit_thread_interrupt_checkpoint (mb);
	
	mono_mb_emit_ldarg (mb, 0);
	pos_end = mono_mb_emit_branch (mb, CEE_BR);
	
	/* fail */
	
	mono_mb_patch_branch (mb, pos_failed);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	
	/* the end */
	
	mono_mb_patch_branch (mb, pos_end);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, klass, mb, isint_sig, isint_sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

void
mono_upgrade_remote_class_wrapper (MonoReflectionType *rtype, MonoTransparentProxy *tproxy)
{
	MonoClass *klass;
	MonoDomain *domain = ((MonoObject*)tproxy)->vtable->domain;
	klass = mono_class_from_mono_type (rtype->type);
	mono_upgrade_remote_class (domain, (MonoObject*)tproxy, klass);
}

/**
 * mono_marshal_get_struct_to_ptr:
 * @klass:
 *
 * generates IL code for StructureToPtr (object structure, IntPtr ptr, bool fDeleteOld)
 * The wrapper info for the wrapper is a WrapperInfo structure.
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

	if (klass->marshal_info->str_to_ptr)
		return klass->marshal_info->str_to_ptr;

	if (!stoptr) 
		stoptr = mono_class_get_method_from_name (mono_defaults.marshal_class, "StructureToPtr", 3);
	g_assert (stoptr);

	mb = mono_mb_new (klass, stoptr->name, MONO_WRAPPER_UNKNOWN);

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

	res = mono_mb_create_method (mb, mono_signature_no_pinvoke (stoptr), 0);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_STRUCTURE_TO_PTR);
	mono_marshal_set_wrapper_info (res, info);

	klass->marshal_info->str_to_ptr = res;
	return res;
}

/**
 * mono_marshal_get_ptr_to_struct:
 * @klass:
 *
 * generates IL code for PtrToStructure (IntPtr src, object structure)
 * The wrapper info for the wrapper is a WrapperInfo structure.
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

	if (klass->marshal_info->ptr_to_str)
		return klass->marshal_info->ptr_to_str;

	if (!ptostr) {
		MonoMethodSignature *sig;

		/* Create the signature corresponding to
		 	  static void PtrToStructure (IntPtr ptr, object structure);
		   defined in class/corlib/System.Runtime.InteropServices/Marshal.cs */
		sig = mono_create_icall_signature ("void ptr object");
		sig = signature_dup (mono_defaults.corlib, sig);
		sig->pinvoke = 0;
		mono_memory_barrier ();
		ptostr = sig;
	}

	mb = mono_mb_new (klass, "PtrToStructure", MONO_WRAPPER_UNKNOWN);

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

	res = mono_mb_create_method (mb, ptostr, 0);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_PTR_TO_STRUCTURE);
	mono_marshal_set_wrapper_info (res, info);

	klass->marshal_info->ptr_to_str = res;
	return res;
}

/*
 * Return a dummy wrapper for METHOD which is called by synchronized wrappers.
 * This is used to avoid infinite recursion since it is hard to determine where to
 * replace a method with its synchronized wrapper, and where not.
 * The runtime should execute METHOD instead of the wrapper.
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod *
mono_marshal_get_synchronized_inner_wrapper (MonoMethod *method)
{
	MonoMethodBuilder *mb;
	WrapperInfo *info;
	MonoMethodSignature *sig;
	MonoMethod *res;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_UNKNOWN);
	mono_mb_emit_exception_full (mb, "System", "ExecutionEngineException", "Shouldn't be called.");
	mono_mb_emit_byte (mb, CEE_RET);
	sig = signature_dup (method->klass->image, mono_method_signature (method));
	res = mono_mb_create_method (mb, sig, 0);
	mono_mb_free (mb);
	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_SYNCHRONIZED_INNER);
	info->d.synchronized_inner.method = method;
	mono_marshal_set_wrapper_info (res, info);
	return res;
}

/*
 * generates IL code for the synchronized wrapper: the generated method
 * calls METHOD while locking 'this' or the parent type.
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
	int i, pos, this_local, ret_local = 0;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED)
		return method;

	cache = get_cache (&method->klass->image->synchronized_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = signature_dup (method->klass->image, mono_method_signature (method));
	sig->pinvoke = 0;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_SYNCHRONIZED);

	/* result */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		ret_local = mono_mb_add_local (mb, sig->ret);

	if (method->klass->valuetype && !(method->flags & MONO_METHOD_ATTR_STATIC)) {
		mono_class_set_failure (method->klass, MONO_EXCEPTION_TYPE_LOAD, NULL);
		/* This will throw the type load exception when the wrapper is compiled */
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_op (mb, CEE_ISINST, method->klass);
		mono_mb_emit_byte (mb, CEE_POP);

		if (!MONO_TYPE_IS_VOID (sig->ret))
			mono_mb_emit_ldloc (mb, ret_local);
		mono_mb_emit_byte (mb, CEE_RET);

		res = mono_mb_create_and_cache (cache, method,
										mb, sig, sig->param_count + 16);
		mono_mb_free (mb);

		return res;
	}

	/* this */
	this_local = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	clause = mono_image_alloc0 (method->klass->image, sizeof (MonoExceptionClause));
	clause->flags = MONO_EXCEPTION_CLAUSE_FINALLY;

	mono_loader_lock ();

	if (!enter_method) {
		MonoMethodDesc *desc;

		desc = mono_method_desc_new ("Monitor:Enter", FALSE);
		enter_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (enter_method);
		mono_method_desc_free (desc);

		desc = mono_method_desc_new ("Monitor:Exit", FALSE);
		exit_method = mono_method_desc_search_in_class (desc, mono_defaults.monitor_class);
		g_assert (exit_method);
		mono_method_desc_free (desc);

		desc = mono_method_desc_new ("Type:GetTypeFromHandle", FALSE);
		gettypefromhandle_method = mono_method_desc_search_in_class (desc, mono_defaults.monotype_class->parent);
		g_assert (gettypefromhandle_method);
		mono_method_desc_free (desc);
	}

	mono_loader_unlock ();

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
	mono_mb_emit_managed_call (mb, enter_method, NULL);

	clause->try_offset = mono_mb_get_label (mb);

	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));

	mono_mb_emit_managed_call (mb, method, NULL);

	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, ret_local);

	pos = mono_mb_emit_branch (mb, CEE_LEAVE);

	clause->try_len = mono_mb_get_pos (mb) - clause->try_offset;
	clause->handler_offset = mono_mb_get_label (mb);

	/* Call Monitor::Exit() */
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_managed_call (mb, exit_method, NULL);
	mono_mb_emit_byte (mb, CEE_ENDFINALLY);

	clause->handler_len = mono_mb_get_pos (mb) - clause->handler_offset;

	mono_mb_patch_branch (mb, pos);
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldloc (mb, ret_local);
	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_set_clauses (mb, 1, clause);

	res = mono_mb_create_and_cache (cache, method,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;	
}


/*
 * the returned method calls 'method' unboxing the this argument
 */
MonoMethod *
mono_marshal_get_unbox_wrapper (MonoMethod *method)
{
	MonoMethodSignature *sig = mono_method_signature (method);
	int i;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;

	cache = get_cache (&method->klass->image->unbox_wrapper_cache, mono_aligned_addr_hash, NULL);
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_UNBOX);

	g_assert (sig->hasthis);
	
	mono_mb_emit_ldarg (mb, 0); 
	mono_mb_emit_icon (mb, sizeof (MonoObject));
	mono_mb_emit_byte (mb, CEE_ADD);
	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, method,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	/* code_for (res); */

	return res;	
}

enum {
	STELEMREF_OBJECT, /*no check at all*/
	STELEMREF_SEALED_CLASS, /*check vtable->klass->element_type */
	STELEMREF_CLASS, /*only the klass->parents check*/
	STELEMREF_INTERFACE, /*interfaces without variant generic arguments. */
	STELEMREF_COMPLEX, /*arrays, MBR or types with variant generic args - go straight to icalls*/
	STELEMREF_KIND_COUNT
};

static const char *strelemref_wrapper_name[] = {
	"object", "sealed_class", "class", "interface", "complex"
};

static gboolean
is_monomorphic_array (MonoClass *klass)
{
	MonoClass *element_class;
	if (klass->rank != 1)
		return FALSE;

	element_class = klass->element_class;
	return (element_class->flags & TYPE_ATTRIBUTE_SEALED) || element_class->valuetype;
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
	if (element_class->marshalbyref || element_class->rank || mono_class_has_variant_generic_params (element_class))
		return STELEMREF_COMPLEX;
	if (element_class->flags & TYPE_ATTRIBUTE_SEALED)
		return STELEMREF_SEALED_CLASS;
	return STELEMREF_CLASS;
}

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
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, element_class));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);
}

static void
load_value_class (MonoMethodBuilder *mb, int vklass)
{
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);
}

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
 * The wrapper info for the wrapper is a WrapperInfo structure.
 *
 * TODO:
 *	- Separate simple interfaces from variant interfaces or mbr types. This way we can avoid the icall for them.
 *	- Emit a (new) mono bytecode that produces OP_COND_EXC_NE_UN to raise ArrayTypeMismatch
 *	- Maybe mve some MonoClass field into the vtable to reduce the number of loads
 *	- Add a case for arrays of arrays.
 */
MonoMethod*
mono_marshal_get_virtual_stelemref (MonoClass *array_class)
{
	static MonoMethod *cached_methods [STELEMREF_KIND_COUNT] = { NULL }; /*object iface sealed regular*/
	static MonoMethodSignature *signature;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int kind;
	char *name;
	const char *param_names [16];
	guint32 b1, b2, b3;
	int aklass, vklass, vtable, uiid;
	int array_slot_addr;
	WrapperInfo *info;

	g_assert (array_class->rank == 1);
	kind = get_virtual_stelemref_kind (array_class->element_class);

	if (cached_methods [kind])
		return cached_methods [kind];

	name = g_strdup_printf ("virt_stelemref_%s", strelemref_wrapper_name [kind]);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_STELEMREF);
	g_free (name);

	param_names [0] = "index";
	param_names [1] = "value";
	mono_mb_set_param_names (mb, param_names);

	if (!signature) {
		MonoMethodSignature *sig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);

		/* void this::stelemref (size_t idx, void* value) */
		sig->ret = &mono_defaults.void_class->byval_arg;
		sig->hasthis = TRUE;
		sig->params [0] = &mono_defaults.int_class->byval_arg; /* this is a natural sized int */
		sig->params [1] = &mono_defaults.object_class->byval_arg;
		signature = sig;
	}

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

	case STELEMREF_COMPLEX:
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

		/*if (mono_object_isinst (value, aklass)) */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_icall (mb, mono_object_isinst);
		b2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

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

	case STELEMREF_CLASS:
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

		/*if (mono_object_isinst (value, aklass)) */
		mono_mb_emit_ldarg (mb, 2);
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_icall (mb, mono_object_isinst);
		b2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* if (vklass->idepth < aklass->idepth) goto failue */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);

		b2 = mono_mb_emit_branch (mb, CEE_BLT_UN);

		/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
		mono_mb_emit_ldloc (mb, vklass);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, supertypes));
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		mono_mb_emit_icon (mb, 1);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_icon (mb, sizeof (void*));
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_ldloc (mb, aklass);
		b3 = mono_mb_emit_branch (mb, CEE_BNE_UN);

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
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_stloc (mb, vtable);

		/* uiid = klass->interface_id; */
		mono_mb_emit_ldloc (mb, aklass);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, interface_id));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		mono_mb_emit_stloc (mb, uiid);

		/*if (uiid > vt->max_interface_id)*/
		mono_mb_emit_ldloc (mb, uiid);
		mono_mb_emit_ldloc (mb, vtable);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, max_interface_id));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		b2 = mono_mb_emit_branch (mb, CEE_BGT_UN);

		/* if (!(vt->interface_bitmap [(uiid) >> 3] & (1 << ((uiid)&7)))) */

		/*vt->interface_bitmap*/
		mono_mb_emit_ldloc (mb, vtable);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, interface_bitmap));
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

	res = mono_mb_create_method (mb, signature, 4);
	res->flags |= METHOD_ATTRIBUTE_VIRTUAL;

	info = mono_wrapper_info_create (res, WRAPPER_SUBTYPE_VIRTUAL_STELEMREF);
	info->d.virtual_stelemref.kind = kind;
	mono_marshal_set_wrapper_info (res, info);

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

/*
 * The wrapper info for the wrapper is a WrapperInfo structure.
 */
MonoMethod*
mono_marshal_get_stelemref ()
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
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, element_class));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, aklass);
	
	/* vklass = value->vtable->klass */
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoObject, vtable));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoVTable, klass));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, vklass);
	
	/* if (vklass->idepth < aklass->idepth) goto failue */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);
	
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
	mono_mb_emit_byte (mb, CEE_LDIND_U2);
	
	b2 = mono_mb_emit_branch (mb, CEE_BLT_UN);
	
	/* if (vklass->supertypes [aklass->idepth - 1] != aklass) goto failure */
	mono_mb_emit_ldloc (mb, vklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, supertypes));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoClass, idepth));
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
	mono_mb_emit_icall (mb, mono_object_isinst);
	
	b4 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	mono_mb_patch_addr (mb, b4, copy_pos - (b4 + 4));
	mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
	
	mono_mb_emit_byte (mb, CEE_RET);
	ret = mono_mb_create_method (mb, sig, 4);
	mono_mb_free (mb);

	info = mono_wrapper_info_create (ret, WRAPPER_SUBTYPE_NONE);
	mono_marshal_set_wrapper_info (ret, info);

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
 * @rank: rank of the array type
 * @elem_size: size in bytes of an element of an array.
 *
 * Returns a MonoMethd that implements the code to get the address
 * of an element in a multi-dimenasional array of @rank dimensions.
 * The returned method takes an array as the first argument and then
 * @rank indexes for the @rank dimensions.
 */
MonoMethod*
mono_marshal_get_array_address (int rank, int elem_size)
{
	MonoMethod *ret;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
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

	mb = mono_mb_new (mono_defaults.object_class, "ElementAddr", MONO_WRAPPER_MANAGED_TO_MANAGED);
	
	bounds = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	ind = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	realidx = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);

	/* bounds = array->bounds; */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoArray, bounds));
	mono_mb_emit_byte (mb, CEE_LDIND_I);
	mono_mb_emit_stloc (mb, bounds);

	/* ind is the overall element index, realidx is the partial index in a single dimension */
	/* ind = idx0 - bounds [0].lower_bound */
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldloc (mb, bounds);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_byte (mb, CEE_SUB);
	mono_mb_emit_stloc (mb, ind);
	/* if (ind >= bounds [0].length) goto exeception; */
	mono_mb_emit_ldloc (mb, ind);
	mono_mb_emit_ldloc (mb, bounds);
	mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArrayBounds, length));
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
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + G_STRUCT_OFFSET (MonoArrayBounds, lower_bound));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_SUB);
		mono_mb_emit_stloc (mb, realidx);
		/* if (realidx >= bounds [i].length) goto exeception; */
		mono_mb_emit_ldloc (mb, realidx);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + G_STRUCT_OFFSET (MonoArrayBounds, length));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		branch_positions [i] = mono_mb_emit_branch (mb, CEE_BGE_UN);
		/* ind = ind * bounds [i].length + realidx */
		mono_mb_emit_ldloc (mb, ind);
		mono_mb_emit_ldloc (mb, bounds);
		mono_mb_emit_icon (mb, (i * sizeof (MonoArrayBounds)) + G_STRUCT_OFFSET (MonoArrayBounds, length));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		mono_mb_emit_byte (mb, CEE_MUL);
		mono_mb_emit_ldloc (mb, realidx);
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, ind);
	}

	/* return array->vector + ind * element_size */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoArray, vector));
	mono_mb_emit_ldloc (mb, ind);
	mono_mb_emit_icon (mb, elem_size);
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
	ret = mono_mb_create_method (mb, sig, 4);
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
		WrapperInfo *info;

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

		info = mono_wrapper_info_create (ret, WRAPPER_SUBTYPE_ELEMENT_ADDR);
		info->d.element_addr.rank = rank;
		info->d.element_addr.elem_size = elem_size;
		mono_marshal_set_wrapper_info (ret, info);
	}
	mono_marshal_unlock ();
	return ret;
}

void*
mono_marshal_alloc (gulong size)
{
	gpointer res;

#ifdef HOST_WIN32
	res = CoTaskMemAlloc (size);
#else
	res = g_try_malloc ((gulong)size);
	if (!res)
		mono_gc_out_of_memory ((gulong)size);
#endif
	return res;
}

void
mono_marshal_free (gpointer ptr)
{
#ifdef HOST_WIN32
	CoTaskMemFree (ptr);
#else
	g_free (ptr);
#endif
}

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

static void *
mono_marshal_string_to_utf16_copy (MonoString *s)
{
	if (s == NULL) {
		return NULL;
	} else {
		gunichar2 *res = mono_marshal_alloc ((mono_string_length (s) * 2) + 2);
		memcpy (res, mono_string_chars (s), mono_string_length (s) * 2);
		res [mono_string_length (s)] = 0;
		return res;
	}
}

/**
 * mono_marshal_set_last_error:
 *
 * This function is invoked to set the last error value from a P/Invoke call
 * which has SetLastError set.
 */
void
mono_marshal_set_last_error (void)
{
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
	mono_native_tls_set_value (last_error_tls_id, GINT_TO_POINTER (error));
#endif
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length)
{
	int element_size;
	void *source_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	if (src->obj.vtable->klass->rank != 1)
		mono_raise_exception (mono_get_exception_argument ("array", "array is multi-dimensional"));
	if (start_index < 0)
		mono_raise_exception (mono_get_exception_argument ("startIndex", "Must be >= 0"));
	if (length < 0)
		mono_raise_exception (mono_get_exception_argument ("length", "Must be >= 0"));
	if (start_index + length > mono_array_length (src))
		mono_raise_exception (mono_get_exception_argument ("length", "start_index + length > array length"));

	element_size = mono_array_element_size (src->obj.vtable->klass);

	/* no references should be involved */
	source_addr = mono_array_addr_with_size (src, element_size, start_index);

	memcpy (dest, source_addr, length * element_size);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length)
{
	int element_size;
	void *dest_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	if (dest->obj.vtable->klass->rank != 1)
		mono_raise_exception (mono_get_exception_argument ("array", "array is multi-dimensional"));
	if (start_index < 0)
		mono_raise_exception (mono_get_exception_argument ("startIndex", "Must be >= 0"));
	if (length < 0)
		mono_raise_exception (mono_get_exception_argument ("length", "Must be >= 0"));
	if (start_index + length > mono_array_length (dest))
		mono_raise_exception (mono_get_exception_argument ("length", "start_index + length > array length"));

	element_size = mono_array_element_size (dest->obj.vtable->klass);
	  
	/* no references should be involved */
	dest_addr = mono_array_addr_with_size (dest, element_size, start_index);

	memcpy (dest_addr, src, length * element_size);
}

#if NO_UNALIGNED_ACCESS
#define RETURN_UNALIGNED(type, addr) \
	{ \
		type val; \
		memcpy(&val, p + offset, sizeof(val)); \
		return val; \
	}
#define WRITE_UNALIGNED(type, addr, val) \
	memcpy(addr, &val, sizeof(type))
#else
#define RETURN_UNALIGNED(type, addr) \
	return *(type*)(p + offset);
#define WRITE_UNALIGNED(type, addr, val) \
	(*(type *)(addr) = (val))
#endif

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gpointer, p + offset);
}

unsigned char
ves_icall_System_Runtime_InteropServices_Marshal_ReadByte (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(unsigned char*)(p + offset);
}

gint16
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt16 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gint16, p + offset);
}

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt32 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gint32, p + offset);
}

gint64
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt64 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	RETURN_UNALIGNED(gint64, p + offset);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteByte (gpointer ptr, gint32 offset, unsigned char val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(unsigned char*)(p + offset) = val;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteIntPtr (gpointer ptr, gint32 offset, gpointer val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gpointer, p + offset, val);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt16 (gpointer ptr, gint32 offset, gint16 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gint16, p + offset, val);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt32 (gpointer ptr, gint32 offset, gint32 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gint32, p + offset, val);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt64 (gpointer ptr, gint32 offset, gint64 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	WRITE_UNALIGNED(gint64, p + offset, val);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr)
{
	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL)
		return NULL;
	else
		return mono_string_new (mono_domain_get (), ptr);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len)
{
	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL) {
		mono_raise_exception (mono_get_exception_argument_null ("ptr"));
		g_assert_not_reached ();
		return NULL;
	} else {
		return mono_string_new_len (mono_domain_get (), ptr, len);
	}
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr)
{
	MonoDomain *domain = mono_domain_get (); 
	int len = 0;
	guint16 *t = ptr;

	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL)
		return NULL;

	while (*t++)
		len++;

	return mono_string_new_utf16 (domain, ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len)
{
	MonoDomain *domain = mono_domain_get (); 

	MONO_ARCH_SAVE_REGS;

	if (ptr == NULL) {
		mono_raise_exception (mono_get_exception_argument_null ("ptr"));
		g_assert_not_reached ();
		return NULL;
	} else {
		return mono_string_new_utf16 (domain, ptr, len);
	}
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void)
{
	MONO_ARCH_SAVE_REGS;

	return (GPOINTER_TO_INT (mono_native_tls_get_value (last_error_tls_id)));
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionType *rtype)
{
	MonoClass *klass;
	MonoType *type;
	guint32 layout;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (rtype);

	type = rtype->type;
	klass = mono_class_from_mono_type (type);
	if (!mono_class_init (klass))
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	layout = (klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK);

	if (layout == TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		gchar *msg;
		MonoException *exc;

		msg = g_strdup_printf ("Type %s cannot be marshaled as an unmanaged structure.", klass->name);
		exc = mono_get_exception_argument ("t", msg);
		g_free (msg);
		mono_raise_exception (exc);
	}


	return mono_class_native_size (klass, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old)
{
	MonoMethod *method;
	gpointer pa [3];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (obj);
	MONO_CHECK_ARG_NULL (dst);

	method = mono_marshal_get_struct_to_ptr (obj->vtable->klass);

	pa [0] = obj;
	pa [1] = &dst;
	pa [2] = &delete_old;

	mono_runtime_invoke (method, NULL, pa, NULL);
}

static void
ptr_to_structure (gpointer src, MonoObject *dst)
{
	MonoMethod *method;
	gpointer pa [2];

	method = mono_marshal_get_ptr_to_struct (dst->vtable->klass);

	pa [0] = &src;
	pa [1] = dst;

	mono_runtime_invoke (method, NULL, pa, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst)
{
	MonoType *t;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dst);
	
	t = mono_type_get_underlying_type (mono_class_get_type (dst->vtable->klass));

	if (t->type == MONO_TYPE_VALUETYPE) {
		MonoException *exc;
		gchar *tmp;

		tmp = g_strdup_printf ("Destination is a boxed value type.");
		exc = mono_get_exception_argument ("dst", tmp);
		g_free (tmp);  

		mono_raise_exception (exc);
		return;
	}

	ptr_to_structure (src, dst);
}

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain = mono_domain_get (); 
	MonoObject *res;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	klass = mono_class_from_mono_type (type->type);
	if (!mono_class_init (klass))
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	res = mono_object_new (domain, klass);

	ptr_to_structure (src, res);

	return res;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionType *type, MonoString *field_name)
{
	MonoMarshalType *info;
	MonoClass *klass;
	char *fname;
	int match_index = -1;
	
	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (field_name);

	fname = mono_string_to_utf8 (field_name);
	klass = mono_class_from_mono_type (type->type);
	if (!mono_class_init (klass))
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

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
		MonoException* exc;
		gchar *tmp;

		/* Get back original class instance */
		klass = mono_class_from_mono_type (type->type);

		tmp = g_strdup_printf ("Field passed in is not a marshaled member of the type %s", klass->name);
		exc = mono_get_exception_argument ("fieldName", tmp);
		g_free (tmp);
 
		mono_raise_exception ((MonoException*)exc);
	}

	info = mono_marshal_load_type_info (klass);     
	return info->fields [match_index].offset;
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string)
{
#ifdef HOST_WIN32
	char* tres, *ret;
	size_t len;
	tres = mono_string_to_utf8 (string);
	if (!tres)
		return tres;

	len = strlen (tres) + 1;
	ret = ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (len);
	memcpy (ret, tres, len);
	g_free (tres);
	return ret;

#else
	return mono_string_to_utf8 (string);
#endif
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string)
{
	MONO_ARCH_SAVE_REGS;

	if (string == NULL)
		return NULL;
	else {
#ifdef TARGET_WIN32
		gunichar2 *res = ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal 
			((mono_string_length (string) + 1) * 2);
#else
		gunichar2 *res = g_malloc ((mono_string_length (string) + 1) * 2);		
#endif
		memcpy (res, mono_string_chars (string), mono_string_length (string) * 2);
		res [mono_string_length (string)] = 0;
		return res;
	}
}

static void
mono_struct_delete_old (MonoClass *klass, char *ptr)
{
	MonoMarshalType *info;
	int i;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		char *cpos;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, 
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

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	klass = mono_class_from_mono_type (type->type);
	if (!mono_class_init (klass))
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	mono_struct_delete_old (klass, (char *)src);
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (int size)
{
	gpointer res;

	MONO_ARCH_SAVE_REGS;

	if ((gulong)size == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		size = 4;

#ifdef HOST_WIN32
	res = GlobalAlloc (GMEM_FIXED, (gulong)size);
#else
	res = g_try_malloc ((gulong)size);
#endif
	if (!res)
		mono_gc_out_of_memory ((gulong)size);

	return res;
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal (gpointer ptr, int size)
{
	gpointer res;

	if (ptr == NULL) {
		mono_gc_out_of_memory ((gulong)size);
		return NULL;
	}

#ifdef HOST_WIN32
	res = GlobalReAlloc (ptr, (gulong)size, GMEM_MOVEABLE);
#else
	res = g_try_realloc (ptr, (gulong)size);
#endif
	if (!res)
		mono_gc_out_of_memory ((gulong)size);

	return res;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal (void *ptr)
{
	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	GlobalFree (ptr);
#else
	g_free (ptr);
#endif
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem (int size)
{
	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	return CoTaskMemAlloc (size);
#else
	return g_try_malloc ((gulong)size);
#endif
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr)
{
	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	CoTaskMemFree (ptr);
#else
	g_free (ptr);
#endif
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem (gpointer ptr, int size)
{
	MONO_ARCH_SAVE_REGS;

#ifdef HOST_WIN32
	return CoTaskMemRealloc (ptr, size);
#else
	return g_try_realloc (ptr, (gulong)size);
#endif
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement (MonoArray *arrayobj, int index)
{
	return mono_array_addr_with_size (arrayobj, mono_array_element_size (arrayobj->obj.vtable->klass), index);
}

MonoDelegate*
ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal (void *ftn, MonoReflectionType *type)
{
	MonoClass *klass = mono_type_get_class (type->type);
	if (!mono_class_init (klass))
		mono_raise_exception (mono_class_get_exception_for_failure (klass));

	return mono_ftnptr_to_delegate (klass, ftn);
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
	GSList *loads_list = mono_native_tls_get_value (load_type_info_tls_id);

	return g_slist_find (loads_list, klass) != NULL;
}

/**
 * mono_marshal_load_type_info:
 *
 *  Initialize klass->marshal_info using information from metadata. This function can
 * recursively call itself, and the caller is responsible to avoid that by calling 
 * mono_marshal_is_loading_type_info () beforehand.
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

	if (klass->marshal_info)
		return klass->marshal_info;

	if (!klass->inited)
		mono_class_init (klass);

	mono_loader_lock ();

	if (klass->marshal_info) {
		mono_loader_unlock ();
		return klass->marshal_info;
	}

	/*
	 * This function can recursively call itself, so we keep the list of classes which are
	 * under initialization in a TLS list.
	 */
	g_assert (!mono_marshal_is_loading_type_info (klass));
	loads_list = mono_native_tls_get_value (load_type_info_tls_id);
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

	layout = klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;

	/* The mempool is protected by the loader lock */
	info = mono_image_alloc0 (klass->image, MONO_SIZEOF_MARSHAL_TYPE + sizeof (MonoMarshalField) * count);
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
			min_align = packing;
			info->fields [j].offset = field->offset - sizeof (MonoObject);
			info->native_size = MAX (info->native_size, info->fields [j].offset + size);
			break;
		}	
		j++;
	}

	if (layout != TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		info->native_size = MAX (native_size, info->native_size);
		/*
		 * If the provided Size is equal or larger than the calculated size, and there
		 * was no Pack attribute, we set min_align to 1 to avoid native_size being increased
		 */
		if (layout == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT)
			if (native_size && native_size == info->native_size && klass->packing_size == 0)
				min_align = 1;
	}

	if (info->native_size & (min_align - 1)) {
		info->native_size += min_align - 1;
		info->native_size &= ~(min_align - 1);
	}

	info->min_align = min_align;

	/* Update the class's blittable info, if the layouts don't match */
	if (info->native_size != mono_class_value_size (klass, NULL))
		klass->blittable = FALSE;

	/* If this is an array type, ensure that we have element info */
	if (klass->rank && !mono_marshal_is_loading_type_info (klass->element_class)) {
		mono_marshal_load_type_info (klass->element_class);
	}

	loads_list = mono_native_tls_get_value (load_type_info_tls_id);
	loads_list = g_slist_remove (loads_list, klass);
	mono_native_tls_set_value (load_type_info_tls_id, loads_list);

	/*We do double-checking locking on marshal_info */
	mono_memory_barrier ();

	klass->marshal_info = info;

	mono_loader_unlock ();

	return klass->marshal_info;
}

/**
 * mono_class_native_size:
 * @klass: a class 
 * 
 * Returns: the native size of an object instance (when marshaled 
 * to unmanaged code) 
 */
gint32
mono_class_native_size (MonoClass *klass, guint32 *align)
{	
	if (!klass->marshal_info) {
		if (mono_marshal_is_loading_type_info (klass)) {
			if (align)
				*align = 0;
			return 0;
		} else {
			mono_marshal_load_type_info (klass);
		}
	}

	if (align)
		*align = klass->marshal_info->min_align;

	return klass->marshal_info->native_size;
}

/* __alignof__ returns the preferred alignment of values not the actual alignment used by
   the compiler so is wrong e.g. for Linux where doubles are aligned on a 4 byte boundary
   but __alignof__ returns 8 - using G_STRUCT_OFFSET works better */
#define ALIGNMENT(type) G_STRUCT_OFFSET(struct { char c; type x; }, x)

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
		*align = ALIGNMENT (gdouble);
		return 8;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		*align = ALIGNMENT (glong);
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
		*align = ALIGNMENT(guint64);
		return 8;
	case MONO_NATIVE_R4:
		*align = 4;
		return 4;
	case MONO_NATIVE_R8:
		*align = ALIGNMENT(double);
		return 8;
	case MONO_NATIVE_INT:
	case MONO_NATIVE_UINT:
	case MONO_NATIVE_LPSTR:
	case MONO_NATIVE_LPWSTR:
	case MONO_NATIVE_LPTSTR:
	case MONO_NATIVE_BSTR:
	case MONO_NATIVE_ANSIBSTR:
	case MONO_NATIVE_TBSTR:
	case MONO_NATIVE_LPARRAY:
	case MONO_NATIVE_SAFEARRAY:
	case MONO_NATIVE_IUNKNOWN:
	case MONO_NATIVE_IDISPATCH:
	case MONO_NATIVE_INTERFACE:
	case MONO_NATIVE_ASANY:
	case MONO_NATIVE_FUNC:
	case MONO_NATIVE_LPSTRUCT:
		*align = ALIGNMENT(gpointer);
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

gpointer
mono_marshal_asany (MonoObject *o, MonoMarshalNative string_encoding, int param_attrs)
{
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
			break;
		case MONO_NATIVE_LPSTR:
			return mono_string_to_lpstr ((MonoString*)o);
			break;
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

		if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT)
			break;

		if (klass->valuetype && (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype))
			return mono_object_unbox (o);

		res = mono_marshal_alloc (mono_class_native_size (klass, NULL));

		if (!((param_attrs & PARAM_ATTRIBUTE_OUT) && !(param_attrs & PARAM_ATTRIBUTE_IN))) {
			method = mono_marshal_get_struct_to_ptr (o->vtable->klass);

			pa [0] = o;
			pa [1] = &res;
			pa [2] = &delete_old;

			mono_runtime_invoke (method, NULL, pa, NULL);
		}

		return res;
	}
	}

	mono_raise_exception (mono_get_exception_argument ("", "No PInvoke conversion exists for value passed to Object-typed parameter."));

	return NULL;
}

void
mono_marshal_free_asany (MonoObject *o, gpointer ptr, MonoMarshalNative string_encoding, int param_attrs)
{
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

		if (klass->valuetype && (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
								 klass->blittable || klass->enumtype))
			break;

		if (param_attrs & PARAM_ATTRIBUTE_OUT) {
			MonoMethod *method = mono_marshal_get_ptr_to_struct (o->vtable->klass);
			gpointer pa [2];

			pa [0] = &ptr;
			pa [1] = o;

			mono_runtime_invoke (method, NULL, pa, NULL);
		}

		if (!((param_attrs & PARAM_ATTRIBUTE_OUT) && !(param_attrs & PARAM_ATTRIBUTE_IN))) {
			mono_struct_delete_old (klass, ptr);
		}

		mono_marshal_free (ptr);
		break;
	}
	default:
		break;
	}
}

MonoMethod *
mono_marshal_get_generic_array_helper (MonoClass *class, MonoClass *iface, gchar *name, MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i;

	mb = mono_mb_new_no_dup_name (class, name, MONO_WRAPPER_MANAGED_TO_MANAGED);
	mb->method->slot = -1;

	mb->method->flags = METHOD_ATTRIBUTE_PRIVATE | METHOD_ATTRIBUTE_VIRTUAL |
		METHOD_ATTRIBUTE_NEW_SLOT | METHOD_ATTRIBUTE_HIDE_BY_SIG | METHOD_ATTRIBUTE_FINAL;

	sig = mono_method_signature (method);
	csig = signature_dup (method->klass->image, sig);
	csig->generic_param_count = 0;

	mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < csig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, NULL);
	mono_mb_emit_byte (mb, CEE_RET);

	/* We can corlib internal methods */
	mb->skip_visibility = TRUE;

	res = mono_mb_create_method (mb, csig, csig->param_count + 16);

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

	klass = method->klass;
	image = method->klass->image;
	cache = get_cache (&image->thunk_invoke_cache, mono_aligned_addr_hash, NULL);

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
	clause = mono_image_alloc0 (image, sizeof (MonoExceptionClause));
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

	g_assert (method->dynamic);

	/* This could be called during shutdown */
	if (marshal_mutex_initialized)
		mono_marshal_lock ();
	/* 
	 * FIXME: We currently leak the wrappers. Freeing them would be tricky as
	 * they could be shared with other methods ?
	 */
	if (image->runtime_invoke_direct_cache)
		g_hash_table_remove (image->runtime_invoke_direct_cache, method);
	if (image->delegate_bound_static_invoke_cache)
		g_hash_table_foreach_remove (image->delegate_bound_static_invoke_cache, signature_method_pair_matches_method, method);
	if (image->delegate_abstract_invoke_cache)
		g_hash_table_foreach_remove (image->delegate_abstract_invoke_cache, signature_method_pair_matches_method, method);

	if (marshal_mutex_initialized)
		mono_marshal_unlock ();
}

/*
 * mono_marshal_free_inflated_wrappers:
 *
 *   Free wrappers of the inflated method METHOD.
 */

static gboolean
signature_method_pair_matches_signature (gpointer key, gpointer value, gpointer user_data)
{
       SignatureMethodPair *pair = (SignatureMethodPair*)key;
       MonoMethodSignature *sig = (MonoMethodSignature*)user_data;

       return mono_metadata_signature_equal (pair->sig, sig);
}

void
mono_marshal_free_inflated_wrappers (MonoMethod *method)
{
       MonoMethodSignature *sig = method->signature;

       g_assert (method->is_inflated);

       /* Ignore calls occuring late during cleanup.  */
       if (!marshal_mutex_initialized)
               return;

       mono_marshal_lock ();
       /*
        * FIXME: We currently leak the wrappers. Freeing them would be tricky as
        * they could be shared with other methods ?
        */

        /*
         * indexed by MonoMethodSignature
         */
	   /* FIXME: This could remove unrelated wrappers as well */
       if (sig && method->klass->image->delegate_begin_invoke_cache)
               g_hash_table_remove (method->klass->image->delegate_begin_invoke_cache, sig);
       if (sig && method->klass->image->delegate_end_invoke_cache)
               g_hash_table_remove (method->klass->image->delegate_end_invoke_cache, sig);
       if (sig && method->klass->image->delegate_invoke_cache)
               g_hash_table_remove (method->klass->image->delegate_invoke_cache, sig);
       if (sig && method->klass->image->runtime_invoke_cache)
               g_hash_table_remove (method->klass->image->runtime_invoke_cache, sig);

        /*
         * indexed by SignatureMethodPair
         */
       if (sig && method->klass->image->delegate_abstract_invoke_cache)
               g_hash_table_foreach_remove (method->klass->image->delegate_abstract_invoke_cache,
                                            signature_method_pair_matches_signature, (gpointer)sig);

       if (sig && method->klass->image->delegate_bound_static_invoke_cache)
                g_hash_table_foreach_remove (method->klass->image->delegate_bound_static_invoke_cache,
                                             signature_method_pair_matches_signature, (gpointer)sig);

        /*
         * indexed by MonoMethod pointers
         */
       if (method->klass->image->runtime_invoke_direct_cache)
               g_hash_table_remove (method->klass->image->runtime_invoke_direct_cache, method);
       if (method->klass->image->managed_wrapper_cache)
               g_hash_table_remove (method->klass->image->managed_wrapper_cache, method);
       if (method->klass->image->native_wrapper_cache)
               g_hash_table_remove (method->klass->image->native_wrapper_cache, method);
       if (method->klass->image->remoting_invoke_cache)
               g_hash_table_remove (method->klass->image->remoting_invoke_cache, method);
       if (method->klass->image->synchronized_cache)
               g_hash_table_remove (method->klass->image->synchronized_cache, method);
       if (method->klass->image->unbox_wrapper_cache)
               g_hash_table_remove (method->klass->image->unbox_wrapper_cache, method);
       if (method->klass->image->cominterop_invoke_cache)
               g_hash_table_remove (method->klass->image->cominterop_invoke_cache, method);
       if (method->klass->image->cominterop_wrapper_cache)
               g_hash_table_remove (method->klass->image->cominterop_wrapper_cache, method);
       if (method->klass->image->thunk_invoke_cache)
               g_hash_table_remove (method->klass->image->thunk_invoke_cache, method);
       if (method->klass->image->native_func_wrapper_aot_cache)
               g_hash_table_remove (method->klass->image->native_func_wrapper_aot_cache, method);

       mono_marshal_unlock ();
}
