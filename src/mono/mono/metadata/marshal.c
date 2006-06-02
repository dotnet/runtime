/*
 * marshal.c: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include "object.h"
#include "loader.h"
#include "metadata/marshal.h"
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
#include <mono/os/gc_wrapper.h>
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

struct _MonoMethodBuilder {
	MonoMethod *method;
	char *name;
	GList *locals_list;
	int locals;
	gboolean dynamic;
	guint32 code_size, pos;
	unsigned char *code;
};

struct _MonoRemotingMethods {
	MonoMethod *invoke;
	MonoMethod *invoke_with_check;
	MonoMethod *xdomain_invoke;
	MonoMethod *xdomain_dispatch;
};

typedef struct _MonoRemotingMethods MonoRemotingMethods;

#ifdef DEBUG_RUNTIME_CODE
static char*
indenter (MonoDisHelper *dh, MonoMethod *method, guint32 ip_offset)
{
	return g_strdup (" ");
}

static MonoDisHelper marshal_dh = {
	"\n",
	"IL_%04x: ",
	"IL_%04x",
	indenter, 
	NULL,
	NULL
};
#endif 

/* This mutex protects the various marshalling related caches in MonoImage */
#define mono_marshal_lock() EnterCriticalSection (&marshal_mutex)
#define mono_marshal_unlock() LeaveCriticalSection (&marshal_mutex)
static CRITICAL_SECTION marshal_mutex;

/* Maps wrapper methods to the methods they wrap */
static GHashTable *wrapper_hash;

static guint32 last_error_tls_id;

static void
delegate_hash_table_add (MonoDelegate *d);

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

static void 
mono_struct_delete_old (MonoClass *klass, char *ptr);

void *
mono_marshal_string_to_utf16 (MonoString *s);

static gpointer
mono_string_to_lpstr (MonoString *string_obj);

static MonoString * 
mono_string_from_bstr (gpointer bstr);

static void 
mono_free_bstr (gpointer bstr);

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

static MonoObject *
mono_marshal_xdomain_copy_value (MonoObject *val);

static void
mono_marshal_xdomain_copy_out_value (MonoObject *src, MonoObject *dst);

static gint32
mono_marshal_set_domain_by_id (gint32 id, MonoBoolean push);

void
mono_upgrade_remote_class_wrapper (MonoReflectionType *rtype, MonoTransparentProxy *tproxy);

static MonoReflectionType *
type_from_handle (MonoType *handle);

static void
mono_marshal_set_last_error_windows (int error);

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

	sigsize = sizeof (MonoMethodSignature) + ((sig->param_count - MONO_ZERO_LEN_ARRAY) * sizeof (MonoType *));
	mono_loader_lock ();
	res = mono_mempool_alloc (image->mempool, sigsize);
	mono_loader_unlock ();
	memcpy (res, sig, sigsize);

	return res;
}

static MonoMethodSignature*
signature_no_pinvoke (MonoMethod *method)
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
		wrapper_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
		last_error_tls_id = TlsAlloc ();

		register_icall (mono_marshal_string_to_utf16, "mono_marshal_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_string_to_utf16, "mono_string_to_utf16", "ptr obj", FALSE);
		register_icall (mono_string_from_utf16, "mono_string_from_utf16", "obj ptr", FALSE);
		register_icall (mono_string_new_wrapper, "mono_string_new_wrapper", "obj ptr", FALSE);
		register_icall (mono_string_to_utf8, "mono_string_to_utf8", "ptr obj", FALSE);
		register_icall (mono_string_to_lpstr, "mono_string_to_lpstr", "ptr obj", FALSE);
		register_icall (mono_string_to_bstr, "mono_string_to_bstr", "ptr obj", FALSE);
		register_icall (mono_string_from_bstr, "mono_string_from_bstr", "obj ptr", FALSE);
		register_icall (mono_free_bstr, "mono_free_bstr", "void ptr", FALSE);
		register_icall (mono_string_to_ansibstr, "mono_string_to_ansibstr", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf8, "mono_string_builder_to_utf8", "ptr object", FALSE);
		register_icall (mono_string_builder_to_utf16, "mono_string_builder_to_utf16", "ptr object", FALSE);
		register_icall (mono_array_to_savearray, "mono_array_to_savearray", "ptr object", FALSE);
		register_icall (mono_array_to_lparray, "mono_array_to_lparray", "ptr object", FALSE);
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
		register_icall (mono_string_utf16_to_builder, "mono_string_utf16_to_builder", "void ptr ptr", FALSE);
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
		register_icall (mono_compile_method, "mono_compile_method", "ptr ptr", FALSE);
		register_icall (mono_context_get, "mono_context_get", "object", FALSE);
		register_icall (mono_context_set, "mono_context_set", "void object", FALSE);
		register_icall (mono_upgrade_remote_class_wrapper, "mono_upgrade_remote_class_wrapper", "void object object", FALSE);
		register_icall (type_from_handle, "type_from_handle", "object ptr", FALSE);
		register_icall (mono_gc_wbarrier_generic_store, "wb_generic", "void ptr object", FALSE);
	}
}

void
mono_marshal_cleanup (void)
{
	g_hash_table_destroy (wrapper_hash);
	TlsFree (last_error_tls_id);
	DeleteCriticalSection (&marshal_mutex);
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

	if (!delegate)
		return NULL;

	if (delegate->delegate_trampoline)
		return delegate->delegate_trampoline;

	klass = ((MonoObject *)delegate)->vtable->klass;
	g_assert (klass->delegate);

	method = delegate->method_info->method;

	wrapper = mono_marshal_get_managed_wrapper (method, klass, delegate->target);

	delegate->delegate_trampoline =  mono_compile_method (wrapper);

	// Add the delegate to the delegate hash table
	delegate_hash_table_add (delegate);

	/* when the object is collected, collect the dunamic method, too */
	mono_object_register_finalizer ((MonoObject*)delegate);

	return delegate->delegate_trampoline;
}

static GHashTable *delegate_hash_table;

static GHashTable *
delegate_hash_table_new (void) {
	return g_hash_table_new (NULL, NULL);
}

static void 
delegate_hash_table_remove (MonoDelegate *d)
{
	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	g_hash_table_remove (delegate_hash_table, d->delegate_trampoline);
	mono_marshal_unlock ();
}

void
delegate_hash_table_add (MonoDelegate *d) 
{
	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();
	g_hash_table_insert (delegate_hash_table, d->delegate_trampoline, d);
	mono_marshal_unlock ();
}

MonoDelegate*
mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn)
{
	MonoDelegate *d;

	mono_marshal_lock ();
	if (delegate_hash_table == NULL)
		delegate_hash_table = delegate_hash_table_new ();

	d = g_hash_table_lookup (delegate_hash_table, ftn);
	mono_marshal_unlock ();
	if (d == NULL) {
		/* This is a native function, so construct a delegate for it */
		static MonoClass *UnmanagedFunctionPointerAttribute;
		MonoMethodSignature *sig;
		MonoMethod *wrapper;
		MonoMarshalSpec **mspecs;
		MonoCustomAttrInfo *cinfo;
		MonoReflectionUnmanagedFunctionPointerAttribute *attr;
		MonoMethod *invoke = mono_get_delegate_invoke (klass);
		MonoMethodPInvoke piinfo;
		int i;

		memset (&piinfo, 0, sizeof (piinfo));
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
					piinfo.piflags = (attr->call_conv << 8) | (attr->charset ? (attr->charset - 1) * 2 : 1) | attr->set_last_error;
				}
				if (!cinfo->cached)
					mono_custom_attrs_free (cinfo);
			}
		}

		mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature (invoke)->param_count + 1);
		mono_method_get_marshal_info (invoke, mspecs);
		sig = signature_dup (invoke->klass->image, mono_method_signature (invoke));
		sig->hasthis = 0;

		wrapper = mono_marshal_get_native_func_wrapper (sig, &piinfo, mspecs, ftn);

		for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
			if (mspecs [i])
				mono_metadata_free_marshal_spec (mspecs [i]);
		g_free (mspecs);
		g_free (sig);

		d = (MonoDelegate*)mono_object_new (mono_domain_get (), klass);
		mono_delegate_ctor ((MonoObject*)d, NULL, mono_compile_method (wrapper));
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
		ji = mono_jit_info_table_find (mono_domain_get (), mono_get_addr_from_ftnptr (ptr));
		g_assert (ji);

		mono_runtime_free_method (mono_object_domain (delegate), ji->method);
	}
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
	if (!array)
		return NULL;

	/* fixme: maybe we need to make a copy */
	return array->vector;
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
	}
	else
		g_assert_not_reached ();
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

gpointer
mono_string_builder_to_utf8 (MonoStringBuilder *sb)
{
	GError *error = NULL;
	glong *res;
	gchar *tmp;

	if (!sb)
		return NULL;

	res = mono_marshal_alloc (mono_stringbuilder_capacity (sb) + 1);

	tmp = g_utf16_to_utf8 (mono_string_chars (sb->str), sb->length, NULL, res, &error);
	if (error) {
		g_error_free (error);
		mono_raise_exception (mono_get_exception_execution_engine ("Failed to convert StringBuilder from utf16 to utf8"));
	}
	else {
		memcpy (res, tmp, sb->length + 1);
		g_free (tmp);
	}

	return res;
}

gpointer
mono_string_builder_to_utf16 (MonoStringBuilder *sb)
{
	if (!sb)
		return NULL;

	/* 
	 * The sb could have been allocated with the default capacity and be empty.
	 * we need to alloc a buffer of the default capacity in this case.
	 */
	if (! sb->str)
		MONO_OBJECT_SETREF (sb, str, mono_string_new_size (mono_domain_get (), mono_stringbuilder_capacity (sb)));
	/*
	 * The stringbuilder might not have ownership of this string. If this is
	 * the case, we must duplicate the string, so that we don't munge immutable
	 * strings
	 */
	else if (sb->str == sb->cached_str) {
		MONO_OBJECT_SETREF (sb, str, mono_string_new_utf16 (mono_domain_get (), mono_string_chars (sb->str), mono_stringbuilder_capacity (sb)));
		sb->cached_str = NULL;
	}
	
	return mono_string_chars (sb->str);
}

static gpointer
mono_string_to_lpstr (MonoString *s)
{
#ifdef PLATFORM_WIN32
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
	}
	else {
		as = CoTaskMemAlloc (len + 1);
		memcpy (as, tmp, len + 1);
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

gpointer
mono_string_to_bstr (MonoString *string_obj)
{
#ifdef PLATFORM_WIN32
	return SysAllocStringLen (mono_string_chars (string_obj), mono_string_length (string_obj));
#else
	g_error ("UnmanagedMarshal.BStr is not implemented.");
	return NULL;
#endif
}

MonoString *
mono_string_from_bstr (gpointer bstr)
{
#ifdef PLATFORM_WIN32
	MonoDomain *domain = mono_domain_get ();
	return mono_string_new_utf16 (domain, bstr, SysStringLen (bstr));
#else
	g_error ("UnmanagedMarshal.BStr is not implemented.");
	return NULL;
#endif
}

void
mono_free_bstr (gpointer bstr)
{
#ifdef PLATFORM_WIN32
	SysFreeString ((BSTR)bstr);
#else
	g_error ("Free BSTR is not implemented.");
#endif
}

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
	memcpy (dst, s, len);
	g_free (s);

	*((char *)dst + size - 1) = 0;
}

void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size)
{
	int len;

	g_assert (dst != NULL);
	g_assert (size > 1);

	if (!src) {
		memset (dst, 0, size);
		return;
	}

	len = MIN (size, (mono_string_length (src) * 2));
	memcpy (dst, mono_string_chars (src), len);

	*((char *)dst + size - 1) = 0;
	*((char *)dst + size - 2) = 0;
}

void
mono_mb_free (MonoMethodBuilder *mb)
{
	g_list_free (mb->locals_list);
	if (!mb->dynamic) {
		g_free (mb->method);
		g_free (mb->name);
		g_free (mb->code);
	}
	g_free (mb);
}

MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name, MonoWrapperType type)
{
	MonoMethodBuilder *mb;
	MonoMethod *m;

	g_assert (klass != NULL);
	g_assert (name != NULL);

	mb = g_new0 (MonoMethodBuilder, 1);

	mb->method = m = (MonoMethod *)g_new0 (MonoMethodWrapper, 1);

	m->klass = klass;
	m->inline_info = 1;
	m->wrapper_type = type;

	mb->name = g_strdup (name);
	mb->code_size = 40;
	mb->code = g_malloc (mb->code_size);
	
	return mb;
}

int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type)
{
	int res;

	g_assert (mb != NULL);
	g_assert (type != NULL);

	res = mb->locals;
	mb->locals_list = g_list_append (mb->locals_list, type);
	mb->locals++;

	return res;
}

/**
 * mono_mb_create_method:
 *
 * Create a MonoMethod from this method builder.
 * Returns: the newly created method.
 *
 * LOCKING: Assumes the loader lock is held.
 */
MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
	MonoMethodHeader *header;
	MonoMethodWrapper *mw;
	MonoMemPool *mp;
	MonoMethod *method;
	GList *l;
	int i;

	g_assert (mb != NULL);

	mp = mb->method->klass->image->mempool;

	if (mb->dynamic) {
		method = mb->method;

		method->name = mb->name;
		method->dynamic = TRUE;

		((MonoMethodNormal *)method)->header = header = (MonoMethodHeader *) 
			g_malloc0 (sizeof (MonoMethodHeader) + mb->locals * sizeof (MonoType *));

		header->code = mb->code;
	} else {
		/* Realloc the method info into a mempool */

		method = mono_mempool_alloc (mp, sizeof (MonoMethodWrapper));
		memcpy (method, mb->method, sizeof (MonoMethodWrapper));

		method->name = mono_mempool_strdup (mp, mb->name);

		((MonoMethodNormal *)method)->header = header = (MonoMethodHeader *) 
			mono_mempool_alloc0 (mp, sizeof (MonoMethodHeader) + mb->locals * sizeof (MonoType *));

		header->code = mono_mempool_alloc (mp, mb->pos);
		memcpy ((char*)header->code, mb->code, mb->pos);
	}

	if (max_stack < 8)
		max_stack = 8;

	header->max_stack = max_stack;

	for (i = 0, l = mb->locals_list; l; l = l->next, i++) {
		header->locals [i] = (MonoType *)l->data;
	}

	method->signature = signature;

	header->code_size = mb->pos;
	header->num_locals = mb->locals;

	mw = (MonoMethodWrapper*) mb->method;
	i = g_list_length (mw->method_data);
	if (i) {
		GList *tmp;
		void **data;
		l = g_list_reverse (mw->method_data);
		if (method->dynamic)
			data = g_malloc (sizeof (gpointer) * (i + 1));
		else
			data = mono_mempool_alloc (mp, sizeof (gpointer) * (i + 1));
		/* store the size in the first element */
		data [0] = GUINT_TO_POINTER (i);
		i = 1;
		for (tmp = l; tmp; tmp = tmp->next) {
			data [i++] = tmp->data;
		}
		g_list_free (l);

		((MonoMethodWrapper*)method)->method_data = data;
	}
	/*{
		static int total_code = 0;
		static int total_alloc = 0;
		total_code += mb->pos;
		total_alloc += mb->code_size;
		g_print ("code size: %d of %d (allocated: %d)\n", mb->pos, total_code, total_alloc);
	}*/

#ifdef DEBUG_RUNTIME_CODE
	printf ("RUNTIME CODE FOR %s\n", mono_method_full_name (method, TRUE));
	printf ("%s\n", mono_disasm_code (&marshal_dh, method, mb->code, mb->code + mb->pos));
#endif

	return method;
}

guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data)
{
	MonoMethodWrapper *mw;

	g_assert (mb != NULL);

	mw = (MonoMethodWrapper *)mb->method;

	/* one O(n) is enough */
	mw->method_data = g_list_prepend (mw->method_data, data);

	return g_list_length (mw->method_data);
}

void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value)
{
	mb->code [pos] = value & 0xff;
	mb->code [pos + 1] = (value >> 8) & 0xff;
	mb->code [pos + 2] = (value >> 16) & 0xff;
	mb->code [pos + 3] = (value >> 24) & 0xff;
}

void
mono_mb_patch_addr_s (MonoMethodBuilder *mb, int pos, gint8 value)
{
	*((gint8 *)(&mb->code [pos])) = value;
}

void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op)
{
	if (mb->pos >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos++] = op;
}

void
mono_mb_emit_ldflda (MonoMethodBuilder *mb, gint32 offset)
{
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);

	if (offset) {
		mono_mb_emit_icon (mb, offset);
		mono_mb_emit_byte (mb, CEE_ADD);
	}
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
	mono_mb_emit_byte (mb, branch_code);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);
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
	mono_mb_emit_byte (mb, branch_code);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	return pos;
}

void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data)
{
	if ((mb->pos + 4) >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mono_mb_patch_addr (mb, mb->pos, data);
	mb->pos += 4;
}

void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data)
{
	if ((mb->pos + 2) >= mb->code_size) {
		mb->code_size += mb->code_size >> 1;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos] = data & 0xff;
	mb->code [mb->pos + 1] = (data >> 8) & 0xff;
	mb->pos += 2;
}

void
mono_mb_emit_ldstr (MonoMethodBuilder *mb, char *str)
{
	mono_mb_emit_byte (mb, CEE_LDSTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, str));
}


void
mono_mb_emit_ldarg (MonoMethodBuilder *mb, guint argnum)
{
	if (argnum < 4) {
 		mono_mb_emit_byte (mb, CEE_LDARG_0 + argnum);
	} else if (argnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDARG_S);
		mono_mb_emit_byte (mb, argnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDARG);
		mono_mb_emit_i2 (mb, argnum);
	}
}

void
mono_mb_emit_ldarg_addr (MonoMethodBuilder *mb, guint argnum)
{
	if (argnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDARGA_S);
		mono_mb_emit_byte (mb, argnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDARGA);
		mono_mb_emit_i2 (mb, argnum);
	}
}

void
mono_mb_emit_ldloc_addr (MonoMethodBuilder *mb, guint locnum)
{
	if (locnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDLOCA_S);
		mono_mb_emit_byte (mb, locnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDLOCA);
		mono_mb_emit_i2 (mb, locnum);
	}
}

void
mono_mb_emit_ldloc (MonoMethodBuilder *mb, guint num)
{
	if (num < 4) {
 		mono_mb_emit_byte (mb, CEE_LDLOC_0 + num);
	} else if (num < 256) {
		mono_mb_emit_byte (mb, CEE_LDLOC_S);
		mono_mb_emit_byte (mb, num);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDLOC);
		mono_mb_emit_i2 (mb, num);
	}
}

void
mono_mb_emit_stloc (MonoMethodBuilder *mb, guint num)
{
	if (num < 4) {
 		mono_mb_emit_byte (mb, CEE_STLOC_0 + num);
	} else if (num < 256) {
		mono_mb_emit_byte (mb, CEE_STLOC_S);
		mono_mb_emit_byte (mb, num);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_STLOC);
		mono_mb_emit_i2 (mb, num);
	}
}

void
mono_mb_emit_icon (MonoMethodBuilder *mb, gint32 value)
{
	if (value >= -1 && value < 8) {
		mono_mb_emit_byte (mb, CEE_LDC_I4_0 + value);
	} else if (value >= -128 && value <= 127) {
		mono_mb_emit_byte (mb, CEE_LDC_I4_S);
		mono_mb_emit_byte (mb, value);
	} else {
		mono_mb_emit_byte (mb, CEE_LDC_I4);
		mono_mb_emit_i4 (mb, value);
	}
}

guint32
mono_mb_emit_branch (MonoMethodBuilder *mb, guint8 op)
{
	guint32 res;
	mono_mb_emit_byte (mb, op);
	res = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	return res;
}

static void
mono_mb_emit_ptr (MonoMethodBuilder *mb, gpointer ptr)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ptr));
}

static void
mono_mb_emit_calli (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, sig));
}

void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig)
{
	mono_mb_emit_byte (mb, CEE_CALL);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));
}

void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_SAVE_LMF);
	mono_mb_emit_ptr (mb, func);
	mono_mb_emit_calli (mb, sig);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_RESTORE_LMF);
}

static void
mono_mb_emit_icall (MonoMethodBuilder *mb, gpointer func)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_ICALL);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, func));
}

static void
mono_mb_emit_exception_full (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg)
{
	MonoMethod *ctor = NULL;

	MonoClass *mme = mono_class_from_name (mono_defaults.corlib, exc_nspace, exc_name);
	mono_class_init (mme);
	ctor = mono_class_get_method_from_name (mme, ".ctor", 0);
	g_assert (ctor);
	mono_mb_emit_byte (mb, CEE_NEWOBJ);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ctor));
	if (msg != NULL) {
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoException, message));
		mono_mb_emit_ldstr (mb, (char*)msg);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
	}
	mono_mb_emit_byte (mb, CEE_THROW);
}

void
mono_mb_emit_exception (MonoMethodBuilder *mb, const char *exc_name, const char *msg)
{
	mono_mb_emit_exception_full (mb, "System", exc_name, msg);
}

static void
mono_mb_emit_exception_marshal_directive (MonoMethodBuilder *mb, const char *msg)
{
	mono_mb_emit_exception_full (mb, "System.Runtime.InteropServices", "MarshalDirectiveException", msg);
}

void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint16 local, gint32 incr)
{
	mono_mb_emit_ldloc (mb, local); 
	mono_mb_emit_icon (mb, incr);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, local); 
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
			type = type->data.klass->enum_basetype;
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
			type = type->data.klass->enum_basetype;
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
		mono_mb_emit_byte (mb, CEE_NEWARR);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
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
			label2 = mb->pos;
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_BGE);
			label3 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* src is already set */

			/* Set dst */
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_byte (mb, CEE_LDELEMA);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
			mono_mb_emit_stloc (mb, 1);

			/* Do the conversion */
			emit_struct_conv (mb, eklass, TRUE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_byte (mb, CEE_BR);
			mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

			mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
		
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
		mono_mb_emit_byte (mb, CEE_NEWARR);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eclass));
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
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_icall (mb, mono_string_from_utf16);
		mono_mb_emit_byte (mb, CEE_STIND_REF);		
		break;		
	case MONO_MARSHAL_CONV_STR_LPTSTR:
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
		mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
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
		mono_mb_emit_byte (mb, CEE_MONO_CLASSCONST);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, mono_ftnptr_to_delegate);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		g_error ("Structure field of type %s can't be marshalled as LPArray", mono_class_from_mono_type (type)->name);
		break;
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	default:
		g_warning ("marshaling conversion %d not implemented", conv);
		g_assert_not_reached ();
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
	case MONO_MARSHAL_CONV_LPSTR_STR:
		return mono_string_new_wrapper;
	case MONO_MARSHAL_CONV_STR_LPTSTR:
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
	case MONO_MARSHAL_CONV_SB_LPTSTR:
		return mono_string_builder_to_utf8;
	case MONO_MARSHAL_CONV_SB_LPWSTR:
		return mono_string_builder_to_utf16;
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
		return mono_array_to_savearray;
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
		return mono_array_to_lparray;
	case MONO_MARSHAL_CONV_DEL_FTN:
		return mono_delegate_to_ftnptr;
	case MONO_MARSHAL_CONV_FTN_DEL:
		return mono_ftnptr_to_delegate;
	case MONO_MARSHAL_CONV_LPSTR_SB:
	case MONO_MARSHAL_CONV_LPTSTR_SB:
		return mono_string_utf8_to_builder;
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
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);
		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icall (mb, g_free);
		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);

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
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);

		if (eklass->blittable) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_ldloc (mb, 0);	
			mono_mb_emit_byte (mb, CEE_LDIND_REF);	
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
			mono_mb_emit_byte (mb, CEE_ADD);
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
			label2 = mb->pos;
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_BGE);
			label3 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* Set src */
			mono_mb_emit_ldloc (mb, array_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_byte (mb, CEE_LDELEMA);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
			mono_mb_emit_stloc (mb, 0);

			/* dst is already set */

			/* Do the conversion */
			emit_struct_conv (mb, eklass, FALSE);

			/* Loop footer */
			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_byte (mb, CEE_BR);
			mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

			mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
		
			/* restore the old src pointer */
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_stloc (mb, 0);
			/* restore the old dst pointer */
			mono_mb_emit_ldloc (mb, dst_var);
			mono_mb_emit_stloc (mb, 1);
		}

		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALCHARARRAY: {
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_ldloc (mb, 0);	
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_ptr (mb, mono_defaults.byte_class);
		mono_mb_emit_icon (mb, mspec->data.array_data.num_elem);
		mono_mb_emit_icall (mb, mono_array_to_byvalarray);
		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);
		
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

		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	default: {
		char *msg = g_strdup_printf ("marshalling conversion %d not implemented", conv);
		MonoException *exc = mono_get_exception_not_implemented (msg);
		g_warning (msg);
		g_free (msg);
		mono_raise_exception (exc);
	}
	}
}

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
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

		/* 
		 * FIXME: Should really check for usize==0 and msize>0, but we apply 
		 * the layout to the managed structure as well.
		 */
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) && (usize == 0)) {
			if (MONO_TYPE_IS_REFERENCE (info->fields [i].field->type) || ((!last_field && MONO_TYPE_IS_REFERENCE (info->fields [i + 1].field->type))))
			g_error ("Type %s which has an [ExplicitLayout] attribute cannot have a reference field at the same offset as another field.", mono_type_full_name (&klass->byval_arg));
		}

		if ((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_AUTO_LAYOUT)
			g_error ("Type %s which is passed to unmanaged code must have a StructLayout attribute", mono_type_full_name (&klass->byval_arg));

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
				mono_mb_emit_byte (mb, mono_type_to_ldind (ftype));
				mono_mb_emit_byte (mb, mono_type_to_stind (ftype));
				break;
			case MONO_TYPE_VALUETYPE: {
				int src_var, dst_var;

				if (ftype->data.klass->enumtype) {
					ftype = ftype->data.klass->enum_basetype;
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
emit_struct_free (MonoMethodBuilder *mb, MonoClass *klass, int struct_var)
{
	/* Call DestroyStructure */
	/* FIXME: Only do this if needed */
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_CLASSCONST);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
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
	
	mono_mb_patch_addr (mb, pos_noabort, mb->pos - (pos_noabort + 4));
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

static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoDelegate *async_callback;
	MonoObject *state;
	MonoMethod *im;
	MonoClass *klass;
	MonoMethod *method = NULL, *method2 = NULL;

	g_assert (delegate);

	if (delegate->target && mono_object_class (delegate->target) == mono_defaults.transparent_proxy_class) {

		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		if (!tp->remote_class->proxy_class->contextbound || tp->rp->context != (MonoObject *) mono_context_get ()) {

			/* If the target is a proxy, make a direct call. Is proxy's work
			// to make the call asynchronous.
			*/
			MonoAsyncResult *ares;
			MonoObject *exc;
			MonoArray *out_args;
			HANDLE handle;
			method = delegate->method_info->method;

			msg = mono_method_call_message_new (mono_marshal_method_from_wrapper (method), params, NULL, &async_callback, &state);
			handle = CreateEvent (NULL, TRUE, FALSE, NULL);
			ares = mono_async_result_new (mono_domain_get (), handle, state, handle, NULL);
			MONO_OBJECT_SETREF (ares, async_delegate, (MonoObject *)delegate);
			MONO_OBJECT_SETREF (ares, async_callback, (MonoObject *)async_callback);
			MONO_OBJECT_SETREF (msg, async_result, ares);
			msg->call_type = CallType_BeginInvoke;

			mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args);
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
#ifdef PLATFORM_WIN32
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
		return MONO_MARSHAL_CONV_LPWSTR_STR;
	case MONO_NATIVE_LPSTR:
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

static inline MonoMethod*
mono_marshal_find_in_cache (GHashTable *cache, gpointer key)
{
	MonoMethod *res;

	mono_marshal_lock ();
	res = g_hash_table_lookup (cache, key);
	mono_marshal_unlock ();
	return res;
}

/* Create the method from the builder and place it in the cache */
static inline MonoMethod*
mono_mb_create_and_cache (GHashTable *cache, gpointer key,
							   MonoMethodBuilder *mb, MonoMethodSignature *sig,
							   int max_stack)
{
	MonoMethod *res;

	mono_loader_lock ();
	mono_marshal_lock ();
	res = g_hash_table_lookup (cache, key);
	if (!res) {
		/* This does not acquire any locks */
		res = mono_mb_create_method (mb, sig, max_stack);
		g_hash_table_insert (cache, key, res);
		g_hash_table_insert (wrapper_hash, res, key);
	}
	mono_marshal_unlock ();
	mono_loader_unlock ();

	return res;
}		


static inline MonoMethod*
mono_marshal_remoting_find_in_cache (MonoMethod *method, int wrapper_type)
{
	MonoMethod *res = NULL;
	MonoRemotingMethods *wrps;

	mono_marshal_lock ();
	wrps = g_hash_table_lookup (method->klass->image->remoting_invoke_cache, method);

	if (wrps) {
		switch (wrapper_type) {
		case MONO_WRAPPER_REMOTING_INVOKE: res = wrps->invoke; break;
		case MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK: res = wrps->invoke_with_check; break;
		case MONO_WRAPPER_XDOMAIN_INVOKE: res = wrps->xdomain_invoke; break;
		case MONO_WRAPPER_XDOMAIN_DISPATCH: res = wrps->xdomain_dispatch; break;
		}
	}

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
	GHashTable *cache = key->klass->image->remoting_invoke_cache;

	mono_loader_lock ();
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
	
	if (*res == NULL) {
		/* This does not acquire any locks */
		*res = mono_mb_create_method (mb, sig, max_stack);
		g_hash_table_insert (wrapper_hash, *res, key);
	}

	mono_marshal_unlock ();
	mono_loader_unlock ();

	return *res;
}		

MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper)
{
	MonoMethod *res;

	if (wrapper->wrapper_type == MONO_WRAPPER_NONE)
		return wrapper;

	mono_marshal_lock ();
	res = g_hash_table_lookup (wrapper_hash, wrapper);
	mono_marshal_unlock ();
	return res;
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

	sig = signature_no_pinvoke (method);

	cache = method->klass->image->delegate_begin_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	name = mono_signature_to_name (sig, "begin_invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name, MONO_WRAPPER_DELEGATE_BEGIN_INVOKE);
	g_free (name);

	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_begin_invoke);
	emit_thread_interrupt_checkpoint (mb);
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

	if (!delegate->method_info || !delegate->method_info->method)
		g_assert_not_reached ();

	klass = delegate->object.vtable->klass;

	method = mono_class_get_method_from_name (klass, "EndInvoke", -1);
	g_assert (method != NULL);

	sig = signature_no_pinvoke (method);

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	g_assert (ares);

	if (delegate->target && mono_object_class (delegate->target) == mono_defaults.transparent_proxy_class) {
		MonoTransparentProxy* tp = (MonoTransparentProxy *)delegate->target;
		msg = (MonoMethodMessage *)mono_object_new (domain, mono_defaults.mono_method_message_class);
		mono_message_init (domain, msg, delegate->method_info, NULL);
		msg->call_type = CallType_EndInvoke;
		MONO_OBJECT_SETREF (msg, async_result, ares);
		res = mono_remoting_invoke ((MonoObject *)tp->rp, msg, &exc, &out_args);
	}
	else
		res = mono_thread_pool_finish (ares, &out_args, &exc);

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
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, mono_type_to_ldind (return_type));
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (return_type))
			break;
		/* fall through */
	case MONO_TYPE_VALUETYPE: {
		int class;
		mono_mb_emit_byte (mb, CEE_UNBOX);
		class = mono_mb_add_data (mb, mono_class_from_mono_type (return_type));
		mono_mb_emit_i4 (mb, class);
		mono_mb_emit_byte (mb, CEE_LDOBJ);
		mono_mb_emit_i4 (mb, class);
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

	sig = signature_no_pinvoke (method);

	cache = method->klass->image->delegate_end_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	name = mono_signature_to_name (sig, "end_invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name, MONO_WRAPPER_DELEGATE_END_INVOKE);
	g_free (name);

	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_icall (mb, mono_delegate_end_invoke);
	emit_thread_interrupt_checkpoint (mb);

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
				if (sig->params [i]->byref)
					mparams[i] = *((gpointer *)params [i]);
				else 
					mparams[i] = params [i];
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

	sig = signature_no_pinvoke (method);

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

	if (mono_class_from_mono_type (t) == mono_defaults.stringbuilder_class)
		return MONO_MARSHAL_COPY;
	
	return MONO_MARSHAL_SERIALIZE;
}


/* mono_marshal_xdomain_copy_value
 * Makes a copy of "val" suitable for the current domain.
 */
static MonoObject *
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
	mono_mb_emit_byte (mb, CEE_CASTCLASS);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
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
	MonoMethodHeader *header;
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

	main_clause = g_new0 (MonoExceptionClause, 1);
	main_clause->try_offset = mb->pos;

	/* Clean the call context */

	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_managed_call (mb, method_set_call_context, NULL);
	mono_mb_emit_byte (mb, CEE_POP);

	/* Deserialize call data */

	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	pos = mb->pos;
	mono_mb_emit_byte (mb, 0);
	
	mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
	mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
	
	if (complex_count > 0)
		mono_mb_emit_stloc (mb, loc_array);
	else
		mono_mb_emit_byte (mb, CEE_POP);

	mono_mb_patch_addr_s (mb, pos, mb->pos - (pos + 1));

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
					mono_mb_emit_byte (mb, CEE_UNBOX);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
				} else {
					mono_mb_emit_byte (mb, CEE_LDELEMA);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
				}
			} else {
				if (pclass->valuetype) {
					mono_mb_emit_byte (mb, CEE_LDELEM_REF);
					mono_mb_emit_byte (mb, CEE_UNBOX);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
					mono_mb_emit_byte (mb, CEE_LDOBJ);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
				} else {
					mono_mb_emit_byte (mb, CEE_LDELEM_REF);
					if (pclass != mono_defaults.object_class) {
						mono_mb_emit_byte (mb, CEE_CASTCLASS);
						mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
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
	
	mono_mb_emit_byte (mb, CEE_CALLVIRT);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));

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
			if (ret_class->valuetype) {
				mono_mb_emit_byte (mb, CEE_BOX);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
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
			mono_mb_emit_byte (mb, CEE_BOX);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
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
	mono_mb_emit_byte (mb, CEE_LEAVE);
	pos_leave = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* Main exception catch */
	main_clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	main_clause->try_len = mb->pos - main_clause->try_offset;
	main_clause->data.catch_class = mono_defaults.object_class;
	
	/* handler code */
	main_clause->handler_offset = mb->pos;
	mono_mb_emit_managed_call (mb, method_rs_serialize_exc, NULL);
	mono_mb_emit_stloc (mb, loc_serialized_exc);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, loc_serialized_exc);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	mono_mb_emit_byte (mb, CEE_LEAVE);
	mono_mb_emit_i4 (mb, 0);
	main_clause->handler_len = mb->pos - main_clause->handler_offset;
	/* end catch */

	mono_mb_patch_addr (mb, pos_leave, mb->pos - (pos_leave + 4));
	
	if (copy_return)
		mono_mb_emit_ldloc (mb, loc_return);

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_remoting_mb_create_and_cache (method, mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	header = ((MonoMethodNormal *)res)->header;
	header->num_clauses = 1;
	header->clauses = main_clause;

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
	int loc_old_domainid, loc_return=0, loc_serialized_exc=0, loc_context;
	int pos, pos_noex;
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
	
	sig = signature_no_pinvoke (method);

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
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	pos = mb->pos;
	mono_mb_emit_byte (mb, 0);
	
	mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	
	mono_mb_emit_managed_call (mb, mono_marshal_get_remoting_invoke (method), NULL);
	mono_mb_emit_byte (mb, CEE_RET);
	mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);

	/* Create the array that will hold the parameters to be serialized */

	if (complex_count > 0) {
		mono_mb_emit_icon (mb, (ret_marshal_type == MONO_MARSHAL_SERIALIZE && complex_out_count > 0) ? complex_count + 1 : complex_count);	/* +1 for the return type */
		mono_mb_emit_byte (mb, CEE_NEWARR);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.object_class));
	
		j = 0;
		for (i = 0; i < sig->param_count; i++) {
			MonoClass *pclass;
			if (marshal_types [i] != MONO_MARSHAL_SERIALIZE) continue;
			pclass = mono_class_from_mono_type (sig->params[i]);
			mono_mb_emit_byte (mb, CEE_DUP);
			mono_mb_emit_icon (mb, j);
			mono_mb_emit_ldarg (mb, i + 1);		/* 0=this */
			if (sig->params[i]->byref) {
				if (pclass->valuetype) {
					mono_mb_emit_byte (mb, CEE_LDOBJ);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
				} else {
					mono_mb_emit_byte (mb, CEE_LDIND_REF);
				}
			}
			if (pclass->valuetype) {
				mono_mb_emit_byte (mb, CEE_BOX);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
			}
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

	/* Get the target domain id */

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_byte (mb, CEE_DUP);
	mono_mb_emit_stloc (mb, loc_real_proxy);

	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoRealProxy, target_domain_id));
	mono_mb_emit_byte (mb, CEE_LDIND_I4);
	mono_mb_emit_byte (mb, CEE_LDC_I4_1);

	/* switch domain */

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
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	pos_noex = mb->pos;
	mono_mb_emit_byte (mb, 0);

	mono_mb_emit_ldloc (mb, loc_serialized_exc);
	mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
	mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
	mono_mb_emit_byte (mb, CEE_CASTCLASS);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.exception_class));
	mono_mb_emit_managed_call (mb, method_exc_fixexc, NULL);
	mono_mb_emit_byte (mb, CEE_THROW);
	mono_mb_patch_addr_s (mb, pos_noex, mb->pos - pos_noex - 1);

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
					mono_mb_emit_byte (mb, CEE_UNBOX);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
					mono_mb_emit_byte (mb, CEE_LDOBJ);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
					mono_mb_emit_byte (mb, CEE_STOBJ);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
				} else {
					if (pclass != mono_defaults.object_class) {
						mono_mb_emit_byte (mb, CEE_CASTCLASS);
						mono_mb_emit_i4 (mb, mono_mb_add_data (mb, pclass));
					}
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
				mono_mb_emit_byte (mb, CEE_UNBOX);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
				mono_mb_emit_byte (mb, CEE_LDOBJ);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
			}
		}
	} else if (ret_marshal_type == MONO_MARSHAL_SERIALIZE) {
		mono_mb_emit_ldloc (mb, loc_serialized_data);
		mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
		mono_mb_emit_managed_call (mb, method_rs_deserialize, NULL);
		if (ret_class->valuetype) {
			mono_mb_emit_byte (mb, CEE_UNBOX);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
			mono_mb_emit_byte (mb, CEE_LDOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
		} else if (ret_class != mono_defaults.object_class) {
			mono_mb_emit_byte (mb, CEE_CASTCLASS);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, ret_class));
		}
	} else {
		mono_mb_emit_ldloc (mb, loc_serialized_data);
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);
		mono_marshal_emit_xdomain_copy_value (mb, byte_array_class);
	
		mono_mb_patch_addr_s (mb, pos, mb->pos - (pos + 1));
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
	if (target_type == MONO_REMOTING_TARGET_APPDOMAIN)
		return mono_marshal_get_xappdomain_invoke (method);
	else
		return mono_marshal_get_remoting_invoke (method);
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

	sig = signature_no_pinvoke (method);
	
	/* we cant remote methods without this pointer */
	g_assert (sig->hasthis);

	if ((res = mono_marshal_remoting_find_in_cache (method, MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK)))
		return res;

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
		
		mono_mb_patch_addr (mb, pos_rem, mb->pos - (pos_rem + 4));
	}
	/* wrapper for normal remote calls */
	native = mono_marshal_get_remoting_invoke (method);
	mono_mb_emit_managed_call (mb, native, mono_method_signature (native));
	mono_mb_emit_byte (mb, CEE_RET);

	/* not a proxy */
	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
	mono_mb_emit_managed_call (mb, method, mono_method_signature (method));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_remoting_mb_create_and_cache (method, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/*
 * the returned method invokes all methods in a multicast delegate 
 */
MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig, *static_sig;
	int i;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int pos0, pos1;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "Invoke"));
		
	sig = signature_no_pinvoke (method);

	cache = method->klass->image->delegate_invoke_cache;
	if ((res = mono_marshal_find_in_cache (cache, sig)))
		return res;

	static_sig = signature_dup (method->klass->image, sig);
	static_sig->hasthis = 0;

	name = mono_signature_to_name (sig, "invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name,  MONO_WRAPPER_DELEGATE_INVOKE);
	g_free (name);

	/* allocate local 0 (object) */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

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
	mono_mb_emit_stloc (mb, 0);

	/* if prev != null */
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_BRFALSE);

	pos0 = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* then recurse */
	mono_mb_emit_ldloc (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, mono_method_signature (method));
	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_byte (mb, CEE_POP);

	/* continued or prev == null */
	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	/* get this->target */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, target));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);
	mono_mb_emit_stloc (mb, 0);

	/* if target != null */
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_BRFALSE);
	pos0 = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	
	/* then call this->method_ptr nonstatic */
	mono_mb_emit_ldloc (mb, 0); 
	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, sig));

	mono_mb_emit_byte (mb, CEE_BR);
	pos1 = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* else [target == null] call this->method_ptr static */
	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, static_sig));

	/* return */
	mono_mb_patch_addr (mb, pos1, mb->pos - (pos1 + 4));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, sig,
										 mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;	
}

/*
 * signature_dup_add_this:
 *
 *  Make a copy of @sig, adding an explicit this argument.
 */
static MonoMethodSignature*
signature_dup_add_this (MonoMethodSignature *sig, MonoClass *klass)
{
	MonoMethodSignature *res;
	int i;

	res = mono_metadata_signature_alloc (klass->image, sig->param_count + 1);
	memcpy (res, sig, sizeof (MonoMethodSignature));
	res->param_count = sig->param_count + 1;
	res->hasthis = FALSE;
	for (i = sig->param_count - 1; i >= 0; i --)
		res->params [i + 1] = sig->params [i];
	res->params [0] = &mono_ptr_class_get (&klass->byval_arg)->byval_arg;

	return res;
}

typedef struct {
	MonoMethod *ctor;
	MonoMethodSignature *sig;
} CtorSigPair;



/*
 * generates IL code for the runtime invoke function 
 * MonoObject *runtime_invoke (MonoObject *this, void **params, MonoObject **exc, void* method)
 *
 * we also catch exceptions if exc != null
 */
MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig, *csig, *callsig;
	MonoExceptionClause *clause;
	MonoMethodHeader *header;
	MonoMethodBuilder *mb;
	GHashTable *cache = NULL;
	MonoClass *target_klass;
	MonoMethod *res = NULL;
	GSList *item;
	static MonoString *string_dummy = NULL;
	static MonoMethodSignature *dealy_abort_sig = NULL;
	int i, pos, posna;
	char *name;

	g_assert (method);

	target_klass = method->klass;
	
	mono_marshal_lock ();

	if (method->string_ctor) {
		static GSList *strsig_list = NULL;
		CtorSigPair *cs;

		callsig = NULL;
		for (item = strsig_list; item; item = item->next) {
			cs = item->data;
			if (mono_metadata_signature_equal (mono_method_signature (method), mono_method_signature (cs->ctor))) {
				callsig = cs->sig;
				break;
			}
		}
		if (!callsig) {
			callsig = signature_dup (method->klass->image, mono_method_signature (method));
			callsig->ret = &mono_defaults.string_class->byval_arg;
			cs = g_new (CtorSigPair, 1);
			cs->sig = callsig;
			cs->ctor = method;
			strsig_list = g_slist_prepend (strsig_list, cs);
		}
	} else {
		if (method->klass->valuetype && mono_method_signature (method)->hasthis) {
			/* 
			 * Valuetype methods receive a managed pointer as the this argument.
			 * Create a new signature to reflect this.
			 */
			callsig = signature_dup_add_this (mono_method_signature (method), method->klass);
		} else {
			callsig = mono_method_signature (method);
		}
	}

	cache = method->klass->image->runtime_invoke_cache;

	/* from mono_marshal_find_in_cache */
	res = g_hash_table_lookup (cache, callsig);
	mono_marshal_unlock ();

	if (res) {
		return res;
	}

	if (!dealy_abort_sig) {
		dealy_abort_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
		dealy_abort_sig->ret = &mono_defaults.void_class->byval_arg;
		dealy_abort_sig->pinvoke = 0;
	}
	
	target_klass = mono_defaults.object_class;

	/* to make it work with our special string constructors */
	if (!string_dummy) {
		MONO_GC_REGISTER_ROOT (string_dummy);
		string_dummy = mono_string_new_wrapper ("dummy");
	}
	
	sig = mono_method_signature (method);

	csig = mono_metadata_signature_alloc (method->klass->image, 4);

	csig->ret = &mono_defaults.object_class->byval_arg;
	if (method->klass->valuetype && mono_method_signature (method)->hasthis)
		csig->params [0] = callsig->params [0];
	else
		csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &mono_defaults.int_class->byval_arg;

	name = mono_signature_to_name (callsig, "runtime_invoke");
	mb = mono_mb_new (target_klass, name,  MONO_WRAPPER_RUNTIME_INVOKE);
	g_free (name);

	/* allocate local 0 (object) tmp */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	/* allocate local 1 (object) exc */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* cond set *exc to null */
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	mono_mb_emit_byte (mb, 3);	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	emit_thread_force_interrupt_checkpoint (mb);

	if (sig->hasthis) {
		if (method->string_ctor) {
			mono_mb_emit_ptr (mb, string_dummy);
		} else {
			mono_mb_emit_ldarg (mb, 0);
		}
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		int type;

		mono_mb_emit_ldarg (mb, 1);
		if (i) {
			mono_mb_emit_icon (mb, sizeof (gpointer) * i);
			mono_mb_emit_byte (mb, CEE_ADD);
		}
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		if (t->byref)
			continue;

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
			mono_mb_emit_byte (mb, mono_type_to_ldind (sig->params [i]));
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			/* do nothing */
			break;
		case MONO_TYPE_VALUETYPE:
			if (t->data.klass->enumtype) {
				type = t->data.klass->enum_basetype->type;
				goto handle_enum;
			}
			if (mono_class_is_nullable (mono_class_from_mono_type (sig->params [i]))) {
				/* Need to convert a boxed vtype to an mp to a Nullable struct */
				mono_mb_emit_byte (mb, CEE_UNBOX);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (sig->params [i])));
				mono_mb_emit_byte (mb, CEE_LDOBJ);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (sig->params [i])));
			} else {
				mono_mb_emit_byte (mb, CEE_LDOBJ);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, t->data.klass));
			}
			break;
		case MONO_TYPE_GENERICINST:
			t = &t->data.generic_class->container_class->byval_arg;
			type = t->type;
			goto handle_enum;
		default:
			g_assert_not_reached ();
		}		
	}
	
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_calli (mb, callsig);

	if (sig->ret->byref) {
		/* fixme: */
		g_assert_not_reached ();
	}


	switch (sig->ret->type) {
	case MONO_TYPE_VOID:
		if (!method->string_ctor)
			mono_mb_emit_byte (mb, CEE_LDNULL);
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
		mono_mb_emit_byte (mb, CEE_BOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (sig->ret)));
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
		/* nothing to do */
		break;
	case MONO_TYPE_PTR:
	default:
		g_assert_not_reached ();
	}

	mono_mb_emit_stloc (mb, 0);
       		
	mono_mb_emit_byte (mb, CEE_LEAVE);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	mono_loader_lock ();
	clause = mono_mempool_alloc0 (target_klass->image->mempool, sizeof (MonoExceptionClause));
	mono_loader_unlock ();
	clause->flags = MONO_EXCEPTION_CLAUSE_FILTER;
	clause->try_len = mb->pos;

	/* filter code */
	clause->data.filter_offset = mb->pos;
	
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_CGT_UN);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_ENDFILTER);

	clause->handler_offset = mb->pos;

	/* handler code */
	/* store exception */
	mono_mb_emit_stloc (mb, 1);
	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_REF);

	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_stloc (mb, 0);

	/* Check for the abort exception */
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_ISINST);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.threadabortexception_class));
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	posna = mb->pos;
	mono_mb_emit_byte (mb, 0);

	/* Delay the abort exception */
	mono_mb_emit_native_call (mb, dealy_abort_sig, ves_icall_System_Threading_Thread_ResetAbort);

	mono_mb_patch_addr_s (mb, posna, mb->pos - posna - 1);
	mono_mb_emit_byte (mb, CEE_LEAVE);
	mono_mb_emit_i4 (mb, 0);

	clause->handler_len = mb->pos - clause->handler_offset;

	/* return result */
	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);

	/* taken from mono_mb_create_and_cache */
	mono_loader_lock ();
	mono_marshal_lock ();

	res = g_hash_table_lookup (cache, callsig);
	/* Somebody may have created it before us */
	if (!res) {
		res = mono_mb_create_method (mb, csig, sig->param_count + 16);
		g_hash_table_insert (cache, callsig, res);
		g_hash_table_insert (wrapper_hash, res, callsig);
	}

	mono_marshal_unlock ();
	mono_loader_unlock ();
	/* end mono_mb_create_and_cache */

	mono_mb_free (mb);

	header = ((MonoMethodNormal *)res)->header;
	header->num_clauses = 1;
	header->clauses = clause;

	return res;	
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
 * This method generates a wrapper for calling mono_load_remote_field_new with
 * the appropriate return type.
 */
MonoMethod *
mono_marshal_get_ldfld_remote_wrapper (MonoClass *klass)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;

	cache = klass->image->ldfld_remote_wrapper_cache;
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	/* 
	 * This wrapper is similar to an icall wrapper but all the wrappers
	 * call the same C function, but with a different signature.
	 */
	name = g_strdup_printf ("__mono_load_remote_field_new_wrapper_%s.%s", klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_LDFLD_REMOTE);
	g_free (name);

	mb->method->save_lmf = 1;

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->ret = &klass->this_arg;

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 3);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->ret = &klass->this_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_load_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (cache, klass,
									mb, sig, sig->param_count + 16);
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

	cache = klass->image->ldfld_wrapper_cache;
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
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));		
		mono_mb_emit_byte (mb, CEE_BR);
		pos1 = mb->pos;
		mono_mb_emit_i4 (mb, 0);
	} else {
		mono_mb_emit_byte (mb, CEE_RET);
	}


	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	mono_mb_emit_ldarg (mb, 0);
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
	mono_mb_emit_ldarg (mb, 3);
	mono_mb_emit_byte (mb, CEE_ADD);

	if (klass->valuetype)
		mono_mb_patch_addr (mb, pos1, mb->pos - (pos1 + 4));

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
		mono_mb_emit_byte (mb, CEE_LDOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		break;
	case MONO_TYPE_GENERICINST:
		if (mono_type_generic_inst_is_valuetype (type)) {
			mono_mb_emit_byte (mb, CEE_LDOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
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
	int t, pos0;

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

	cache = klass->image->ldflda_wrapper_cache;
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

	mono_mb_emit_ldarg (mb, 0);
	pos0 = mono_mb_emit_proxy_check (mb, CEE_BNE_UN);

	/* FIXME: Only throw this if the object is in another appdomain */
	mono_mb_emit_exception_full (mb, "System", "InvalidOperationException", "Attempt to load field address from object in another appdomain.");

	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

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
 */
MonoMethod *
mono_marshal_get_stfld_remote_wrapper (MonoClass *klass)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;

	cache = klass->image->stfld_remote_wrapper_cache;
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	name = g_strdup_printf ("__mono_store_remote_field_new_wrapper_%s.%s", klass->name_space, klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_STFLD_REMOTE);
	g_free (name);

	mb->method->save_lmf = 1;

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	sig->params [0] = &mono_defaults.object_class->byval_arg;
	sig->params [1] = &mono_defaults.int_class->byval_arg;
	sig->params [2] = &mono_defaults.int_class->byval_arg;
	sig->params [3] = &klass->byval_arg;
	sig->ret = &mono_defaults.void_class->byval_arg;

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldarg (mb, 3);

	if (klass->valuetype) {
		mono_mb_emit_byte (mb, CEE_BOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));		
	}

	csig = mono_metadata_signature_alloc (mono_defaults.corlib, 4);
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;
	csig->params [3] = &klass->byval_arg;
	csig->ret = &mono_defaults.void_class->byval_arg;
	csig->pinvoke = 1;

	mono_mb_emit_native_call (mb, csig, mono_store_remote_field_new);
	emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);
       
	res = mono_mb_create_and_cache (cache, klass,
									mb, sig, sig->param_count + 16);
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

	cache = klass->image->stfld_wrapper_cache;
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

	mono_mb_emit_managed_call (mb, mono_marshal_get_stfld_remote_wrapper (klass), NULL);

	mono_mb_emit_byte (mb, CEE_RET);

	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));

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
		mono_mb_emit_byte (mb, CEE_STOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		break;
	case MONO_TYPE_GENERICINST:
		mono_mb_emit_byte (mb, CEE_STOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
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
 */
MonoMethod *
mono_marshal_get_icall_wrapper (MonoMethodSignature *sig, const char *name, gconstpointer func)
{
	MonoMethodSignature *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i;
	
	g_assert (sig->pinvoke);

	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;

	/* we copy the signature, so that we can modify it */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + sig->hasthis);

	mono_mb_emit_native_call (mb, sig, (gpointer) func);
	emit_thread_interrupt_checkpoint (mb);
	mono_mb_emit_byte (mb, CEE_RET);

	csig = signature_dup (mono_defaults.corlib, sig);
	csig->pinvoke = 0;
	if (csig->call_convention == MONO_CALL_VARARG)
		csig->call_convention = 0;

	res = mono_mb_create_method (mb, csig, csig->param_count + 16);
	mono_mb_free (mb);
	
	return res;
}

typedef struct {
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	MonoMethodPInvoke *piinfo;
	int *orig_conv_args; /* Locals containing the original values of byref args */
	int retobj_var;
	MonoClass *retobj_class;
	MonoMethodSignature *csig; /* Might need to be changed due to MarshalAs directives */
} EmitMarshalContext;

typedef enum {
	MARSHAL_ACTION_CONV_IN,
	MARSHAL_ACTION_PUSH,
	MARSHAL_ACTION_CONV_OUT,
	MARSHAL_ACTION_CONV_RESULT,
	MARSHAL_ACTION_MANAGED_CONV_IN,
	MARSHAL_ACTION_MANAGED_CONV_OUT,
	MARSHAL_ACTION_MANAGED_CONV_RESULT
} MarshalAction;

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
	MonoMethod *get_instance;
	MonoMethodBuilder *mb = m->mb;
	char *exception_msg = NULL;
	guint32 loc1;
	int pos2;

	if (!ICustomMarshaler) {
		ICustomMarshaler = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "ICustomMarshaler");
		g_assert (ICustomMarshaler);

		cleanup_native = mono_class_get_method_from_name (ICustomMarshaler, "CleanUpNativeData", 1);
		g_assert (cleanup_native);
		cleanup_managed = mono_class_get_method_from_name (ICustomMarshaler, "CleanUpManagedData", 1);
		g_assert (cleanup_managed);
		marshal_managed_to_native = mono_class_get_method_from_name (ICustomMarshaler, "MarshalManagedToNative", 1);
		g_assert (marshal_managed_to_native);
		marshal_native_to_managed = mono_class_get_method_from_name (ICustomMarshaler, "MarshalNativeToManaged", 1);
		g_assert (marshal_native_to_managed);
	}

	mtype = mono_reflection_type_from_name (spec->data.custom_data.custom_name, mb->method->klass->image);
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

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_byte (mb, CEE_CALL);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_REF);

		if (t->type == MONO_TYPE_VALUETYPE) {
			/*
			 * Since we can't determine the type of the argument, we
			 * will assume the unmanaged function takes a pointer.
			 */
			*conv_arg_type = &mono_defaults.int_class->byval_arg;

			mono_mb_emit_byte (mb, CEE_BOX);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (t)));
		}

		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_managed_to_native));
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
		break;

	case MARSHAL_ACTION_CONV_OUT:
		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		if (t->byref) {
			mono_mb_emit_ldarg (mb, argnum);

			mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

			mono_mb_emit_byte (mb, CEE_CALL);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_byte (mb, CEE_CALLVIRT);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_native_to_managed));
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_byte (mb, CEE_CALL);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));

		mono_mb_emit_ldloc (mb, conv_arg);

		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_native));

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
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

		mono_mb_emit_byte (mb, CEE_CALL);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
		mono_mb_emit_byte (mb, CEE_DUP);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_native_to_managed));
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, loc1);
		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_native));

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		if (t->byref) {
			conv_arg = 0;
			break;
		}

		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);

		/* Check for null */
		mono_mb_emit_ldarg (mb, argnum);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));
		mono_mb_emit_byte (mb, CEE_CALL);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
		mono_mb_emit_ldarg (mb, argnum);
				
		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_native_to_managed));
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
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
		mono_mb_emit_byte (mb, CEE_CALL);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
		mono_mb_emit_byte (mb, CEE_DUP);

		mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, marshal_managed_to_native));
		mono_mb_emit_stloc (mb, 3);

		mono_mb_emit_ldloc (mb, loc1);
		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_managed));

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		g_assert (!t->byref);

		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos2 = mono_mb_emit_branch (mb, CEE_BRFALSE);

		/* Call CleanUpManagedData */
		mono_mb_emit_ldstr (mb, g_strdup (spec->data.custom_data.cookie));

		mono_mb_emit_byte (mb, CEE_CALL);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, get_instance));
				
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_CALLVIRT);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, cleanup_managed));

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
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
	MonoClass *klass;
	int pos = 0, pos2;

	klass = t->data.klass;

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
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
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);
		}

		if (!(t->byref && !(t->attrs & PARAM_ATTRIBUTE_IN) && (t->attrs & PARAM_ATTRIBUTE_OUT))) {
			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
		}

		if (t->byref)
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
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

		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype) {
			mono_mb_emit_ldarg (mb, argnum);
			break;
		}			
		mono_mb_emit_ldloc (mb, conv_arg);
		if (!t->byref) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LDNATIVEOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (((klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK) == TYPE_ATTRIBUTE_EXPLICIT_LAYOUT) ||
			klass->blittable || klass->enumtype)
			break;

		if (t->byref) {
			/* dst = argument */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_stloc (mb, 1);

			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);

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
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
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
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);
		}			

		mono_mb_emit_ldloc_addr (mb, conv_arg);
		mono_mb_emit_stloc (mb, 1);

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);

		if (t->byref)
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
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

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
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
			g_warning (msg);
			g_free (msg);
			mono_raise_exception (exc);
		}
		else
			mono_mb_emit_icall (mb, conv_to_icall (conv));

		mono_mb_emit_stloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			if (conv == MONO_MARSHAL_CONV_STR_BSTR) {
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_BSTR_STR));
				// BSTRs always need freed
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, mono_free_bstr);
			}
			else
				mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_LPSTR_STR));
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		} else {
			if (mono_marshal_need_free (t, m->piinfo, spec)) {
				mono_mb_emit_ldloc (mb, conv_arg);
				if (conv == MONO_MARSHAL_CONV_STR_BSTR)
					mono_mb_emit_icall (mb, mono_free_bstr);
				else
					mono_mb_emit_icall (mb, mono_marshal_free);
			}
		}
		break;

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
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
			mono_mb_emit_icall (mb, g_free);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		if (t->byref) {
			conv_arg = 0;
			break;
		}

		conv_arg = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
		*conv_arg_type = &mono_defaults.int_class->byval_arg;

		conv = mono_marshal_get_ptr_to_string_conv (m->piinfo, spec, &need_free);
		if (conv == -1) {
			char *msg = g_strdup_printf ("string marshalling conversion %d not implemented", encoding);
			mono_mb_emit_exception_marshal_directive (mb, msg);
			break;
		}

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_icall (mb, conv_to_icall (conv));
		mono_mb_emit_stloc (mb, conv_arg);
		break;	

	case MARSHAL_ACTION_MANAGED_CONV_RESULT:
		mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_STR_LPSTR));
		mono_mb_emit_stloc (mb, 3);
		break;

	default:
		g_assert_not_reached ();
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
	MonoClass *klass = t->data.klass;
	int pos, pos2, loc;

	if (mono_class_from_mono_type (t) == mono_defaults.object_class) {
		mono_raise_exception (mono_get_exception_not_implemented ("Marshalling of type object is not implemented"));
	}

	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		m->orig_conv_args [argnum] = 0;

		if (klass->delegate) {
			g_assert (!t->byref);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_DEL_FTN));
			mono_mb_emit_stloc (mb, conv_arg);
		} else if (klass == mono_defaults.stringbuilder_class) {
			MonoMarshalNative encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			MonoMarshalConv conv = mono_marshal_get_stringbuilder_to_ptr_conv (m->piinfo, spec);
			
			g_assert (!t->byref);
			mono_mb_emit_ldarg (mb, argnum);

			if (conv != -1)
				mono_mb_emit_icall (mb, conv_to_icall (conv));
			else {
				char *msg = g_strdup_printf ("stringbuilder marshalling conversion %d not implemented", encoding);
				MonoException *exc = mono_get_exception_not_implemented (msg);
				g_warning (msg);
				g_free (msg);
				mono_raise_exception (exc);
			}

			mono_mb_emit_stloc (mb, conv_arg);
		} else if (klass->blittable) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, conv_arg);
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
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);

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
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, 0);

			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);

			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		}
		break;

	case MARSHAL_ACTION_CONV_OUT:
		if (klass == mono_defaults.stringbuilder_class) {
			gboolean need_free;
			MonoMarshalNative encoding;
			MonoMarshalConv conv;

			encoding = mono_marshal_get_string_encoding (m->piinfo, spec);
			conv = mono_marshal_get_ptr_to_stringbuilder_conv (m->piinfo, spec, &need_free);

			g_assert (!t->byref);
			g_assert (encoding != -1);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);

			mono_mb_emit_icall (mb, conv_to_icall (conv));

			if (need_free) {
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, mono_marshal_free);
			}
			break;
		}

		if (klass->delegate)
			break;

		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			/* allocate a new object */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, t->data.klass));
			mono_mb_emit_byte (mb, CEE_STIND_REF);
		}

		/* dst = *argument */
		mono_mb_emit_ldarg (mb, argnum);

		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_I);

		mono_mb_emit_stloc (mb, 1);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_BRFALSE);
		pos = mb->pos;
		mono_mb_emit_i4 (mb, 0);

		if (t->byref || (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			mono_mb_emit_ldloc (mb, 1);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, 1);
			
			/* src = tmp_locals [i] */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, 0);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, t->data.klass, TRUE);

			/* Free the structure returned by the native code */
			emit_struct_free (mb, klass, conv_arg);

			if (m->orig_conv_args [argnum]) {
				/* 
				 * If the native function changed the pointer, then free
				 * the original structure plus the new pointer.
				 */
				mono_mb_emit_ldloc (mb, m->orig_conv_args [argnum]);
				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_byte (mb, CEE_BEQ);
				pos2 = mb->pos;
				mono_mb_emit_i4 (mb, 0);

				if (!(t->attrs & PARAM_ATTRIBUTE_OUT)) {
					g_assert (m->orig_conv_args [argnum]);

					emit_struct_free (mb, klass, m->orig_conv_args [argnum]);
				}

				mono_mb_emit_ldloc (mb, conv_arg);
				mono_mb_emit_icall (mb, g_free);

				mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
			}
		}
		else
			/* Free the original structure passed to native code */
			emit_struct_free (mb, klass, conv_arg);

		mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
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
			mono_mb_emit_byte (mb, CEE_MONO_CLASSCONST);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
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
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);
	
			/* allocate result object */
	
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
			mono_mb_emit_stloc (mb, 3);
					
			/* set dst  */
	
			mono_mb_emit_ldloc (mb, 3);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
			mono_mb_emit_icon (mb, sizeof (MonoObject));
			mono_mb_emit_byte (mb, CEE_ADD);
			mono_mb_emit_stloc (mb, 1);
								
			/* emit conversion code */
			emit_struct_conv (mb, klass, TRUE);
	
			emit_struct_free (mb, klass, loc);
	
			/* Free the pointer allocated by unmanaged code */
			mono_mb_emit_ldloc (mb, loc);
			mono_mb_emit_icall (mb, g_free);
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		}
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN:
		conv_arg = mono_mb_add_local (mb, &klass->byval_arg);

		if (klass->delegate) {
			g_assert (!t->byref);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_CLASSCONST);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_icall (mb, conv_to_icall (MONO_MARSHAL_CONV_FTN_DEL));
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
			mono_mb_emit_byte (mb, CEE_BRTRUE);
			pos2 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			mono_mb_emit_exception (mb, "ArgumentNullException", NULL);

			mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
		}				

		mono_mb_emit_stloc (mb, 0);

		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_BRFALSE);
		pos = mb->pos;
		mono_mb_emit_i4 (mb, 0);

		/* Create and set dst */
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, 1); 

		/* emit valuetype conversion code */
		emit_struct_conv (mb, klass, TRUE);

		mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
		break;

	case MARSHAL_ACTION_MANAGED_CONV_OUT:
		/* Check for null */
		mono_mb_emit_ldloc (mb, conv_arg);
		pos = mono_mb_emit_branch (mb, CEE_BRTRUE);
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_REF);
		pos2 = mono_mb_emit_branch (mb, CEE_BR);

		mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));			
			
		/* Set src */
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
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

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
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

		mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));

		/* Set src */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_stloc (mb, 0);

		/* Allocate and set dest */
		mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_icall (mb, mono_marshal_alloc);
		mono_mb_emit_byte (mb, CEE_DUP);
		mono_mb_emit_stloc (mb, 1);
		mono_mb_emit_stloc (mb, 3);

		emit_struct_conv (mb, klass, FALSE);

		mono_mb_patch_addr (mb, pos2, mb->pos - (pos2 + 4));
		break;

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
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
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			label1 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			if (is_string) {
				if (conv == -1) {
					char *msg = g_strdup_printf ("string/stringbuilder marshalling conversion %d not implemented", encoding);
					MonoException *exc = mono_get_exception_not_implemented (msg);
					g_warning (msg);
					g_free (msg);
					mono_raise_exception (exc);
				}
			}

			if (is_string)
				esize = sizeof (gpointer);
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
			label2 = mb->pos;
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldloc (mb, src_var);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_BGE);
			label3 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

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
				mono_mb_emit_byte (mb, CEE_LDELEMA);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
				mono_mb_emit_stloc (mb, 0);

				/* set dst_ptr */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_stloc (mb, 1);

				/* emit valuetype conversion code */
				emit_struct_conv (mb, eklass, FALSE);
			}

			mono_mb_emit_add_to_local (mb, index_var, 1);
			mono_mb_emit_add_to_local (mb, dest_ptr, esize);
			
			mono_mb_emit_byte (mb, CEE_BR);
			mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

			mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));

			if (eklass == mono_defaults.string_class) {
				/* Null terminate */
				mono_mb_emit_ldloc (mb, dest_ptr);
				mono_mb_emit_byte (mb, CEE_LDC_I4_0);
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}

			mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
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
			else
				esize = mono_class_native_size (eklass, NULL);
			src_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			loc = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			/* Check null */
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			label1 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_stloc (mb, src_ptr);

			/* Emit marshalling loop */
			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);				
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);
			label2 = mb->pos;
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_ldarg (mb, argnum);
			if (t->byref)
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			mono_mb_emit_byte (mb, CEE_LDLEN);
			mono_mb_emit_byte (mb, CEE_BGE);
			label3 = mb->pos;
			mono_mb_emit_i4 (mb, 0);

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
						mono_mb_emit_byte (mb, CEE_LDIND_I);
					mono_mb_emit_ldloc (mb, index_var);
					mono_mb_emit_byte (mb, CEE_LDELEMA);
					mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
					mono_mb_emit_stloc (mb, 1);

					/* emit valuetype conversion code */
					emit_struct_conv (mb, eklass, TRUE);
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

			mono_mb_emit_byte (mb, CEE_BR);
			mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

			mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
			mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
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
		mono_mb_emit_byte (mb, CEE_BRFALSE);
		label1 = mb->pos;
		mono_mb_emit_i4 (mb, 0);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_stloc (mb, src_ptr);

		/* Create managed array */
		/* 
		 * The LPArray marshalling spec says that sometimes param_num starts 
		 * from 1, sometimes it starts from 0. But MS seems to allways start
		 * from 0.
		 */

		if (param_num == -1)
			mono_mb_emit_icon (mb, num_elem);
		else {
			/* FIXME: Add the two together */
			mono_mb_emit_ldarg (mb, param_num);
			if (num_elem > 0) {
				mono_mb_emit_icon (mb, num_elem);
				mono_mb_emit_byte (mb, CEE_ADD);
			}
		}

		mono_mb_emit_byte (mb, CEE_NEWARR);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eklass));
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
		label2 = mb->pos;
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		mono_mb_emit_byte (mb, CEE_BGE);
		label3 = mb->pos;
		mono_mb_emit_i4 (mb, 0);

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

		mono_mb_emit_byte (mb, CEE_BR);
		mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

		mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
		mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
		
		break;
	}
	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		MonoClass *eklass;
		guint32 label1, label2, label3;
		int index_var, dest_ptr, loc, esize, param_num, num_elem;
		MonoMarshalConv conv;
		gboolean is_string = FALSE;
		
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
		mono_mb_emit_byte (mb, CEE_BRFALSE);
		label1 = mb->pos;
		mono_mb_emit_i4 (mb, 0);

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
		label2 = mb->pos;
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_ldloc (mb, conv_arg);
		mono_mb_emit_byte (mb, CEE_LDLEN);
		mono_mb_emit_byte (mb, CEE_BGE);
		label3 = mb->pos;
		mono_mb_emit_i4 (mb, 0);

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

		mono_mb_emit_byte (mb, CEE_BR);
		mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

		mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
		mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));

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
		label2 = mb->pos;
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

		mono_mb_emit_byte (mb, CEE_BR);
		mono_mb_emit_i4 (mb, label2 - (mb->pos + 4));

		mono_mb_patch_addr (mb, label3, mb->pos - (label3 + 4));
		mono_mb_patch_addr (mb, label1, mb->pos - (label1 + 4));
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
		int variant_bool = 0;
		if (!t->byref)
			break;
		if (spec == NULL) {
			local_type = &mono_defaults.int32_class->byval_arg;
		} else {
			switch (spec->native) {
			case MONO_NATIVE_I1:
				local_type = &mono_defaults.byte_class->byval_arg;
				break;
			case MONO_NATIVE_VARIANTBOOL:
				local_type = &mono_defaults.int16_class->byval_arg;
				variant_bool = 1;
				break;
			default:
				g_warning ("marshalling bool as native type %x is currently not supported", spec->native);
				local_type = &mono_defaults.int32_class->byval_arg;
				break;
			}
		}
		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, local_type);
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		if (variant_bool)
			mono_mb_emit_byte (mb, CEE_NEG);
		mono_mb_emit_stloc (mb, conv_arg);
		break;
	}

	case MARSHAL_ACTION_CONV_OUT:
		if (!t->byref)
			break;
		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, conv_arg);
		if (spec != NULL && spec->native == MONO_NATIVE_VARIANTBOOL)
			mono_mb_emit_byte (mb, CEE_NEG);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldarg (mb, argnum);
		break;

	case MARSHAL_ACTION_CONV_RESULT:
		/* maybe we need to make sure that it fits within 8 bits */
		mono_mb_emit_stloc (mb, 3);
		break;

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
		if (MONO_TYPE_ISSTRUCT (t->data.type)) {
			char *msg = g_strdup_printf ("Can not marshal 'parameter #%d': Pointers can not reference marshaled structures. Use byref instead.", argnum + 1);
			mono_mb_emit_exception_marshal_directive (m->mb, msg);
		}
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

	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return emit_marshal_custom (m, argnum, t, spec, conv_arg, conv_arg_type, action);

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return emit_marshal_asany (m, argnum, t, spec, conv_arg, conv_arg_type, action);
			
	switch (t->type) {
	case MONO_TYPE_VALUETYPE:
		return emit_marshal_vtype (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_STRING:
		return emit_marshal_string (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
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
	}

	return conv_arg;
}

/**
 * mono_marshal_emit_native_wrapper:
 * @sig: The signature of the native function
 * @piinfo: Marshalling information
 * @mspecs: Marshalling information
 * @func: the native function to call
 *
 * generates IL code for the pinvoke wrapper, the generated code calls @func.
 */
static void
mono_marshal_emit_native_wrapper (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func)
{
	EmitMarshalContext m;
	MonoMethodSignature *csig;
	MonoClass *klass;
	int i, argnum, *tmp_locals;
	int type;
	static MonoMethodSignature *get_last_error_sig = NULL;

	m.mb = mb;
	m.piinfo = piinfo;

	/* we copy the signature, so that we can set pinvoke to 0 */
	csig = signature_dup (mb->method->klass->image, sig);
	csig->pinvoke = 1;
	m.csig = csig;

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
		tmp_locals [i] = emit_marshal (&m, i + sig->hasthis, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_CONV_IN);
	}

	/* push all arguments */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++) {
		emit_marshal (&m, i + sig->hasthis, sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_PUSH);
	}			

	/* call the native method */
	mono_mb_emit_native_call (mb, csig, func);

	/* Set LastError if needed */
	if (piinfo->piflags & PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR) {
		if (!get_last_error_sig) {
			get_last_error_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 0);
			get_last_error_sig->ret = &mono_defaults.int_class->byval_arg;
			get_last_error_sig->pinvoke = 1;
		}

#ifdef PLATFORM_WIN32
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
					type = sig->ret->data.klass->enum_basetype->type;
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
	emit_thread_interrupt_checkpoint (mb);

	/* we need to convert byref arguments back and free string arrays */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		MonoMarshalSpec *spec = mspecs [i + 1];

		argnum = i + sig->hasthis;

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

/**
 * mono_marshal_get_native_wrapper:
 * @method: The MonoMethod to wrap.
 *
 * generates IL code for the pinvoke wrapper (the generated method
 * calls the unmanaged code in piinfo->addr)
 */
MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *) method;
	MonoMethodBuilder *mb;
	MonoMarshalSpec **mspecs;
	MonoMethod *res;
	GHashTable *cache;
	gboolean pinvoke = FALSE;
	int i;
	const char *exc_class = "MissingMethodException";
	const char *exc_arg = NULL;

	g_assert (method != NULL);
	g_assert (mono_method_signature (method)->pinvoke);

	cache = method->klass->image->native_wrapper_cache;
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = mono_method_signature (method);

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		pinvoke = TRUE;

	if (!piinfo->addr) {
		if (pinvoke)
			mono_lookup_pinvoke_call (method, &exc_class, &exc_arg);
		else
			piinfo->addr = mono_lookup_internal_call (method);
	}

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_NATIVE);

	mb->method->save_lmf = 1;
	
	if (!piinfo->addr) {
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

		/* hack - string constructors returns a value */
		if (method->string_ctor) {
			csig = signature_dup (method->klass->image, sig);
			csig->ret = &mono_defaults.string_class->byval_arg;
		} else
			csig = sig;

		if (sig->hasthis)
			mono_mb_emit_byte (mb, CEE_LDARG_0);

		for (i = 0; i < sig->param_count; i++)
			mono_mb_emit_ldarg (mb, i + sig->hasthis);

		g_assert (piinfo->addr);
		mono_mb_emit_native_call (mb, csig, piinfo->addr);
		emit_thread_interrupt_checkpoint (mb);
		mono_mb_emit_byte (mb, CEE_RET);

		csig = signature_dup (method->klass->image, csig);
		csig->pinvoke = 0;
		res = mono_mb_create_and_cache (cache, method,
										mb, csig, csig->param_count + 16);
		mono_mb_free (mb);
		return res;
	}

	g_assert (pinvoke);

	mspecs = g_new (MonoMarshalSpec*, sig->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	mono_marshal_emit_native_wrapper (mb, sig, piinfo, mspecs, piinfo->addr);

	csig = signature_dup (method->klass->image, sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, method,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	for (i = sig->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (res, TRUE), mono_disasm_code (0, res, ((MonoMethodNormal*)res)->header->code, ((MonoMethodNormal*)res)->header->code + ((MonoMethodNormal*)res)->header->code_size)); */ 

	return res;
}

/**
 * mono_marshal_get_native_func_wrapper:
 * @sig: The signature of the function
 * @func: The native function to wrap
 *
 *   Returns a wrapper method around native functions, similar to the pinvoke
 * wrapper.
 */
MonoMethod *
mono_marshal_get_native_func_wrapper (MonoMethodSignature *sig, 
									  MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func)
{
	MonoMethodSignature *csig;

	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	char *name;

	cache = mono_defaults.corlib->native_wrapper_cache;
	if ((res = mono_marshal_find_in_cache (cache, func)))
		return res;

	name = g_strdup_printf ("wrapper_native_%p", func);
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	mb->method->save_lmf = 1;

	mono_marshal_emit_native_wrapper (mb, sig, piinfo, mspecs, func);

	csig = signature_dup (mb->method->klass->image, sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, func,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);

	/* printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (res, TRUE), mono_disasm_code (0, res, ((MonoMethodNormal*)res)->header->code, ((MonoMethodNormal*)res)->header->code + ((MonoMethodNormal*)res)->header->code_size)); */ 

	return res;
}
			     
/*
 * generates IL code to call managed methods from unmanaged code 
 */
MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, MonoObject *this)
{
	static MonoClass *UnmanagedFunctionPointerAttribute;
	MonoMethodSignature *sig, *csig, *invoke_sig;
	MonoMethodBuilder *mb;
	MonoMethod *res, *invoke;
	MonoMarshalSpec **mspecs;
	MonoMethodPInvoke piinfo;
	GHashTable *cache;
	int i, *tmp_locals;
	EmitMarshalContext m;

	g_assert (method != NULL);
	g_assert (!mono_method_signature (method)->pinvoke);

	/* 
	 * FIXME: Should cache the method+delegate type pair, since the same method
	 * could be called with different delegates, thus different marshalling
	 * options.
	 */
	cache = method->klass->image->managed_wrapper_cache;
	if (!this && (res = mono_marshal_find_in_cache (cache, method)))
		return res;

	invoke = mono_class_get_method_from_name (delegate_klass, "Invoke", mono_method_signature (method)->param_count);
	invoke_sig = mono_method_signature (invoke);

	mspecs = g_new0 (MonoMarshalSpec*, mono_method_signature (invoke)->param_count + 1);
	mono_method_get_marshal_info (invoke, mspecs);

	sig = mono_method_signature (method);

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);

	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	if (!MONO_TYPE_IS_VOID(sig->ret)) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_stloc (mb, 2);

	/* we copy the signature, so that we can modify it */
	csig = signature_dup (method->klass->image, sig);
	csig->hasthis = 0;
	csig->pinvoke = 1;

	m.mb = mb;
	m.sig = sig;
	m.piinfo = NULL;
	m.retobj_var = 0;
	m.csig = csig;

#ifdef PLATFORM_WIN32
	/* 
	 * Under windows, delegates passed to native code must use the STDCALL
	 * calling convention.
	 */
	csig->call_convention = MONO_CALL_STDCALL;
#endif

	/* Change default calling convention if needed */
	/* Why is this a modopt ? */
	if (invoke_sig->ret && invoke_sig->ret->num_mods) {
		for (i = 0; i < invoke_sig->ret->num_mods; ++i) {
			MonoClass *cmod_class = mono_class_get (delegate_klass->image, invoke_sig->ret->modifiers [i].token);
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

	/* Handle the UnmanagedFunctionPointerAttribute */
	if (!UnmanagedFunctionPointerAttribute)
		UnmanagedFunctionPointerAttribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "UnmanagedFunctionPointerAttribute");

	/* The attribute is only available in Net 2.0 */
	if (UnmanagedFunctionPointerAttribute) {
		MonoReflectionUnmanagedFunctionPointerAttribute *attr;
		MonoCustomAttrInfo *cinfo;

		/* 
		 * The pinvoke attributes are stored in a real custom attribute so we have to
		 * construct it.
		 */
		cinfo = mono_custom_attrs_from_class (delegate_klass);
		if (cinfo) {
			attr = (MonoReflectionUnmanagedFunctionPointerAttribute*)mono_custom_attrs_get_attr (cinfo, UnmanagedFunctionPointerAttribute);
			if (attr) {
				memset (&piinfo, 0, sizeof (piinfo));
				m.piinfo = &piinfo;
				piinfo.piflags = (attr->call_conv << 8) | (attr->charset ? (attr->charset - 1) * 2 : 1) | attr->set_last_error;

				csig->call_convention = attr->call_conv - 1;
			}
			if (!cinfo->cached)
				mono_custom_attrs_free (cinfo);
		}
	}

	/* fixme: howto handle this ? */
	if (sig->hasthis) {
		if (this) {
			mono_mb_emit_ptr (mb, this);
		} else {
			/* fixme: */
			g_assert_not_reached ();
		}
	} 

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
			tmp_locals [i] = emit_marshal (&m, i, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_MANAGED_CONV_IN);

			break;
		default:
			tmp_locals [i] = 0;
			break;
		}
	}

	emit_thread_interrupt_checkpoint (mb);

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
		emit_marshal (&m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
	}
	else
	if (!sig->ret->byref) { 
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
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
			emit_marshal (&m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
			break;
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_SZARRAY:
			emit_marshal (&m, 0, sig->ret, mspecs [0], 0, NULL, MARSHAL_ACTION_MANAGED_CONV_RESULT);
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
			emit_marshal (&m, i, t, mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
		}
		else if (t->byref) {
			switch (t->type) {
			case MONO_TYPE_CLASS:
			case MONO_TYPE_VALUETYPE:
				emit_marshal (&m, i, t, mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			}
		}
		else if (invoke_sig->params [i]->attrs & PARAM_ATTRIBUTE_OUT) {
			/* The [Out] information is encoded in the delegate signature */
			switch (t->type) {
			case MONO_TYPE_SZARRAY:
				emit_marshal (&m, i, invoke_sig->params [i], mspecs [i + 1], tmp_locals [i], NULL, MARSHAL_ACTION_MANAGED_CONV_OUT);
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	if (m.retobj_var) {
		mono_mb_emit_ldloc (mb, m.retobj_var);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_RETOBJ);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, m.retobj_class));
	}
	else {
		if (!MONO_TYPE_IS_VOID(sig->ret))
			mono_mb_emit_ldloc (mb, 3);
		mono_mb_emit_byte (mb, CEE_RET);
	}

	if (!this)
		res = mono_mb_create_and_cache (cache, method,
											 mb, csig, sig->param_count + 16);
	else {
		mb->dynamic = 1;
		res = mono_mb_create_method (mb, csig, sig->param_count + 16);
	}
	mono_mb_free (mb);

	for (i = mono_method_signature (invoke)->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	/* printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (res, TRUE), mono_disasm_code (0, res, ((MonoMethodNormal*)res)->header->code, ((MonoMethodNormal*)res)->header->code + ((MonoMethodNormal*)res)->header->code_size)); */

	return res;
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

	cache = klass->image->isinst_cache;
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
	mono_mb_emit_byte (mb, CEE_MONO_CISINST);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));

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
	
	mono_mb_patch_addr (mb, pos_failed, mb->pos - (pos_failed + 4));
	mono_mb_emit_byte (mb, CEE_LDNULL);
	pos_end2 = mono_mb_emit_branch (mb, CEE_BR);
	
	/* success */
	
	mono_mb_patch_addr (mb, pos_was_ok, mb->pos - (pos_was_ok + 4));
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_ldarg (mb, 0);
	
	/* the end */
	
	mono_mb_patch_addr (mb, pos_end, mb->pos - (pos_end + 4));
	mono_mb_patch_addr (mb, pos_end2, mb->pos - (pos_end2 + 4));
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

	cache = klass->image->castclass_cache;
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
	mono_mb_emit_byte (mb, CEE_MONO_CCASTCLASS);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));

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
	mono_mb_patch_addr (mb, pos_was_ok, mb->pos - (pos_was_ok + 4));
	mono_mb_patch_addr (mb, pos_was_ok2, mb->pos - (pos_was_ok2 + 4));
	mono_mb_emit_ldarg (mb, 0);
	
	/* the end */
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, klass, mb, castclass_sig, castclass_sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

MonoMethod *
mono_marshal_get_proxy_cancast (MonoClass *klass)
{
	static MonoMethodSignature *isint_sig = NULL;
	GHashTable *cache;
	MonoMethod *res;
	int pos_failed, pos_end;
	char *name;
	MonoMethod *can_cast_to;
	MonoMethodDesc *desc;
	MonoMethodBuilder *mb;

	cache = klass->image->proxy_isinst_cache;
	if ((res = mono_marshal_find_in_cache (cache, klass)))
		return res;

	if (!isint_sig) {
		isint_sig = mono_metadata_signature_alloc (mono_defaults.corlib, 1);
		isint_sig->params [0] = &mono_defaults.object_class->byval_arg;
		isint_sig->ret = &mono_defaults.object_class->byval_arg;
		isint_sig->pinvoke = 0;
	}
	
	name = g_strdup_printf ("__proxy_isinst_wrapper_%s", klass->name); 
	mb = mono_mb_new (mono_defaults.object_class, name, MONO_WRAPPER_PROXY_ISINST);
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
	mono_mb_emit_byte (mb, CEE_CALLVIRT);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, can_cast_to));

	
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
	
	mono_mb_patch_addr (mb, pos_failed, mb->pos - (pos_failed + 4));
	mono_mb_emit_byte (mb, CEE_LDNULL);
	
	/* the end */
	
	mono_mb_patch_addr (mb, pos_end, mb->pos - (pos_end + 4));
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
 */
MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethod *stoptr = NULL;
	MonoMethod *res;

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

	res = mono_mb_create_method (mb, mono_method_signature (stoptr), 0);
	mono_mb_free (mb);

	klass->marshal_info->str_to_ptr = res;
	return res;
}

/**
 * mono_marshal_get_ptr_to_struct:
 * @klass:
 *
 * generates IL code for PtrToStructure (IntPtr src, object structure)
 */
MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethodSignature *ptostr = NULL;
	MonoMethod *res;

	g_assert (klass != NULL);

	mono_marshal_load_type_info (klass);

	if (klass->marshal_info->ptr_to_str)
		return klass->marshal_info->ptr_to_str;

	if (!ptostr)
		/* Create the signature corresponding to
		 	  static void PtrToStructure (IntPtr ptr, object structure);
		   defined in class/corlib/System.Runtime.InteropServices/Marshal.cs */
		ptostr = mono_create_icall_signature ("void ptr object");

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
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		mono_mb_emit_stloc (mb, 1);

		emit_struct_conv (mb, klass, TRUE);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, ptostr, 0);
	mono_mb_free (mb);

	klass->marshal_info->ptr_to_str = res;
	return res;
}

/*
 * generates IL code for the synchronized wrapper: the generated method
 * calls METHOD while locking 'this' or the parent type.
 */
MonoMethod *
mono_marshal_get_synchronized_wrapper (MonoMethod *method)
{
	static MonoMethod *enter_method, *exit_method;
	MonoMethodSignature *sig;
	MonoExceptionClause *clause;
	MonoMethodHeader *header;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int i, pos, this_local, ret_local = 0;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_SYNCHRONIZED)
		return method;

	cache = method->klass->image->synchronized_cache;
	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = signature_dup (method->klass->image, mono_method_signature (method));
	sig->pinvoke = 0;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_SYNCHRONIZED);

	/* result */
	if (!MONO_TYPE_IS_VOID (sig->ret))
		ret_local = mono_mb_add_local (mb, sig->ret);

	/* this */
	this_local = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	mono_loader_lock ();
	clause = mono_mempool_alloc0 (method->klass->image->mempool, sizeof (MonoExceptionClause));
	mono_loader_unlock ();
	clause->flags = MONO_EXCEPTION_CLAUSE_FINALLY;

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
	}

	/* Push this or the type object */
	if (method->flags & METHOD_ATTRIBUTE_STATIC) {
		/*
		 * GetTypeFromHandle isn't called as a managed method because it has
		 * a funky calling sequence, e.g. ldtoken+GetTypeFromHandle gets
		 * transformed into something else by the JIT.
		 */
		mono_mb_emit_ptr (mb, &method->klass->byval_arg);
		mono_mb_emit_icall (mb, type_from_handle);
	}
	else
		mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_stloc (mb, this_local);

	/* Call Monitor::Enter() */
	mono_mb_emit_ldloc (mb, this_local);
	mono_mb_emit_managed_call (mb, enter_method, NULL);

	clause->try_offset = mb->pos;

	/* Call the method */
	if (sig->hasthis)
		mono_mb_emit_ldarg (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + (sig->hasthis == TRUE));
	
	/* this is needed to avoid recursion */
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LDFTN);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));
	mono_mb_emit_calli (mb, mono_method_signature (method));

	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_stloc (mb, ret_local);

	mono_mb_emit_byte (mb, CEE_LEAVE);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	clause->try_len = mb->pos - clause->try_offset;
	clause->handler_offset = mb->pos;

	/* Call Monitor::Exit() */
	mono_mb_emit_ldloc (mb, this_local);
/*	mono_mb_emit_native_call (mb, exit_sig, mono_monitor_exit); */
	mono_mb_emit_managed_call (mb, exit_method, NULL);
	mono_mb_emit_byte (mb, CEE_ENDFINALLY);

	clause->handler_len = mb->pos - clause->handler_offset;

	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
	if (!MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldloc (mb, ret_local);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_and_cache (cache, method,
									mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	header = ((MonoMethodNormal *)res)->header;
	header->num_clauses = 1;
	header->clauses = clause;

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

	cache = method->klass->image->unbox_wrapper_cache;
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

	/* printf ("CODE FOR %s: \n%s.\n", mono_method_full_name (res, TRUE), mono_disasm_code (0, res, ((MonoMethodNormal*)res)->header->code, ((MonoMethodNormal*)res)->header->code + ((MonoMethodNormal*)res)->header->code_size)); */

	return res;	
}

MonoMethod*
mono_marshal_get_stelemref ()
{
	static MonoMethod* ret = NULL;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	
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
	mono_mb_emit_byte (mb, CEE_LDELEMA);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_defaults.object_class));
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
	
	copy_pos = mb->pos;
	/* do_store */
	mono_mb_patch_addr (mb, b1, mb->pos - (b1 + 4));
	mono_mb_emit_ldloc (mb, array_slot_addr);
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_byte (mb, CEE_STIND_REF);
	
	mono_mb_emit_byte (mb, CEE_RET);
	
	/* the hard way */
	mono_mb_patch_addr (mb, b2, mb->pos - (b2 + 4));
	mono_mb_patch_addr (mb, b3, mb->pos - (b3 + 4));
	
	mono_mb_emit_ldarg (mb, 2);
	mono_mb_emit_ldloc (mb, aklass);
	mono_mb_emit_icall (mb, mono_object_isinst);
	
	b4 = mono_mb_emit_branch (mb, CEE_BRTRUE);
	mono_mb_patch_addr (mb, b4, copy_pos - (b4 + 4));
	mono_mb_emit_exception (mb, "ArrayTypeMismatchException", NULL);
	
	mono_mb_emit_byte (mb, CEE_RET);
	ret = mono_mb_create_method (mb, sig, 4);
	mono_mb_free (mb);
	return ret;
}

MonoMethod*
mono_marshal_get_write_barrier (void)
{
	static MonoMethod* ret = NULL;
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	int max_stack = 2;

	if (ret)
		return ret;
	
	mb = mono_mb_new (mono_defaults.object_class, "writebarrier", MONO_WRAPPER_WRITE_BARRIER);

	sig = mono_metadata_signature_alloc (mono_defaults.corlib, 2);

	/* void writebarrier (MonoObject** addr, MonoObject* obj) */
	sig->ret = &mono_defaults.void_class->byval_arg;
	sig->params [0] = &mono_defaults.object_class->this_arg;
	sig->params [1] = &mono_defaults.object_class->byval_arg;

	/* just the store right now: add an hook for the GC to use, maybe something
	 * that can be used for stelemref as well
	 * We need a write barrier variant to be used with struct copies as well, though
	 * there are also other approaches possible, like writing a wrapper specific to
	 * the struct or to the reference pattern in the struct...
	 * Depending on the GC, we may want variants that take the object we store to
	 * when it is available.
	 */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldarg (mb, 1);
	mono_mb_emit_icall (mb, mono_gc_wbarrier_generic_store);
	/*mono_mb_emit_byte (mb, CEE_STIND_REF);*/

	mono_mb_emit_byte (mb, CEE_RET);

	ret = mono_mb_create_method (mb, sig, max_stack);
	mono_mb_free (mb);
	return ret;
}

void*
mono_marshal_alloc (gulong size)
{
	gpointer res;

#ifdef PLATFORM_WIN32
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
#ifdef PLATFORM_WIN32
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
mono_marshal_realloc (gpointer ptr, gpointer size) 
{
	MONO_ARCH_SAVE_REGS;

	return g_try_realloc (ptr, (gulong)size);
}

void *
mono_marshal_string_to_utf16 (MonoString *s)
{
	return s ? mono_string_chars (s) : NULL;
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
	TlsSetValue (last_error_tls_id, GINT_TO_POINTER (GetLastError ()));
#else
	TlsSetValue (last_error_tls_id, GINT_TO_POINTER (errno));
#endif
}

#ifdef WIN32
static void
mono_marshal_set_last_error_windows (int error)
{
	TlsSetValue (last_error_tls_id, GINT_TO_POINTER (error));
}
#endif

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length)
{
	int element_size;
	void *source_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	g_assert (src->obj.vtable->klass->rank == 1);
	g_assert (start_index >= 0);
	g_assert (length >= 0);
	g_assert (start_index + length <= mono_array_length (src));

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

	g_assert (dest->obj.vtable->klass->rank == 1);
	g_assert (start_index >= 0);
	g_assert (length >= 0);
	g_assert (start_index + length <= mono_array_length (dest));

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
	} else
		return mono_string_new_len (mono_domain_get (), ptr, len);
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
	} else
		return mono_string_new_utf16 (domain, ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_from_bstr(ptr);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToBSTR (MonoString* ptr)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_to_bstr(ptr);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (gpointer ptr)
{
	MONO_ARCH_SAVE_REGS;

	mono_free_bstr (ptr);
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void)
{
	MONO_ARCH_SAVE_REGS;

	return (GPOINTER_TO_INT (TlsGetValue (last_error_tls_id)));
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
	MonoDomain *domain = mono_domain_get (); 
	MonoObject *res;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	res = mono_object_new (domain, mono_class_from_mono_type (type->type));

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

	while (klass && match_index == -1) {
		MonoClassField* field;
		int i = 0;
		gpointer iter = NULL;
		while ((field = mono_class_get_fields (klass, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
				continue;
			if (!strcmp (fname, field->name)) {
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
	MONO_ARCH_SAVE_REGS;

	return mono_string_to_utf8 (string);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string)
{
	MONO_ARCH_SAVE_REGS;

	if (string == NULL)
		return NULL;
	else {
		gunichar2 *res = g_malloc ((mono_string_length (string) + 1) * 2);
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
		case MONO_MARSHAL_CONV_STR_LPSTR:
		case MONO_MARSHAL_CONV_STR_LPTSTR:
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

	mono_struct_delete_old (klass, (char *)src);
}


/* FIXME: on win32 we should probably use GlobalAlloc(). */
void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (int size)
{
	gpointer res;

	MONO_ARCH_SAVE_REGS;

	if ((gulong)size == 0)
		/* This returns a valid pointer for size 0 on MS.NET */
		size = 4;

	res = g_try_malloc ((gulong)size);
	if (!res)
		mono_gc_out_of_memory ((gulong)size);

	return res;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal (void *ptr)
{
	MONO_ARCH_SAVE_REGS;

	g_free (ptr);
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem (int size)
{
	MONO_ARCH_SAVE_REGS;

#ifdef PLATFORM_WIN32
	return CoTaskMemAlloc (size);
#else
	return g_try_malloc ((gulong)size);
#endif
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr)
{
	MONO_ARCH_SAVE_REGS;

#ifdef PLATFORM_WIN32
	CoTaskMemFree (ptr);
#else
	g_free (ptr);
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
	return mono_ftnptr_to_delegate (mono_type_get_class (type->type), ftn);
}

MonoMarshalType *
mono_marshal_load_type_info (MonoClass* klass)
{
	int j, count = 0;
	guint32 native_size = 0, min_align = 1;
	MonoMarshalType *info;
	MonoClassField* field;
	gpointer iter;
	guint32 layout;

	g_assert (klass != NULL);

	if (klass->marshal_info)
		return klass->marshal_info;

	if (!klass->inited)
		mono_class_init (klass);
	
	iter = NULL;
	while ((field = mono_class_get_fields (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		if (mono_field_is_deleted (field))
			continue;
		count++;
	}

	layout = klass->flags & TYPE_ATTRIBUTE_LAYOUT_MASK;

	klass->marshal_info = info = g_malloc0 (sizeof (MonoMarshalType) + sizeof (MonoMarshalField) * count);
	info->num_fields = count;
	
	/* Try to find a size for this type in metadata */
	mono_metadata_packing_from_typedef (klass->image, klass->type_token, NULL, &native_size);

	if (klass->parent) {
		int parent_size = mono_class_native_size (klass->parent, NULL);

		/* Add parent size to real size */
		native_size += parent_size;
		info->native_size = parent_size;
	}
	
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
			mono_metadata_field_info (klass->image, mono_metadata_token_index (mono_class_get_field_token (field)) - 1, 
						  NULL, NULL, &info->fields [j].mspec);

		info->fields [j].field = field;

		if ((mono_class_num_fields (klass) == 1) && (klass->instance_size == sizeof (MonoObject)) &&
			(strcmp (field->name, "$PRIVATE$") == 0)) {
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
			align = klass->packing_size ? MIN (klass->packing_size, align): align;
			min_align = MAX (align, min_align);
			info->fields [j].offset = field->offset - sizeof (MonoObject);
			info->native_size = MAX (info->native_size, info->fields [j].offset + size);
			break;
		}	
		j++;
	}

	if(layout != TYPE_ATTRIBUTE_AUTO_LAYOUT) {
		info->native_size = MAX (native_size, info->native_size);
	}

	if (info->native_size & (min_align - 1)) {
		info->native_size += min_align - 1;
		info->native_size &= ~(min_align - 1);
	}

	/* Update the class's blittable info, if the layouts don't match */
	if (info->native_size != mono_class_value_size (klass, NULL))
		klass->blittable = FALSE;

	/* If this is an array type, ensure that we have element info */
	if (klass->element_class) {
		mono_marshal_load_type_info (klass->element_class);
	}

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
	if (!klass->marshal_info)
		mono_marshal_load_type_info (klass);

	if (align)
		*align = klass->min_align;

	return klass->marshal_info->native_size;
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
		*align = 4;
		return 4;
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
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_TYPEDBYREF:
		*align = 4;
		return 4;
	case MONO_TYPE_R4:
		*align = 4;
		return 4;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R8:
		*align = 4;
		return 8;
	case MONO_TYPE_VALUETYPE: {
		guint32 size;

		if (t->data.klass->enumtype)
			return mono_type_native_stack_size (t->data.klass->enum_basetype, align);
		else {
			size = mono_class_native_size (t->data.klass, align);
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

/* __alignof__ returns the preferred alignment of values not the actual alignment used by
   the compiler so is wrong e.g. for Linux where doubles are aligned on a 4 byte boundary
   but __alignof__ returns 8 - using G_STRUCT_OFFSET works better */
#define ALIGNMENT(type) G_STRUCT_OFFSET(struct { char c; type x; }, x)

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
		g_assert_not_reached ();
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
			return mono_string_to_utf16 ((MonoString*)o);
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
