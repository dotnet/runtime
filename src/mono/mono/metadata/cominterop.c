/**
 * \file
 * COM Interop Support
 *
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include <glib.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include <mono/metadata/object.h>
#include <mono/metadata/loader.h>
#include "cil-coff.h"
#include "metadata/abi-details.h"
#include "metadata/cominterop.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/tabledefs.h"
#include <mono/metadata/exception.h>
#include <mono/metadata/appdomain.h>
#include "metadata/reflection-internals.h"
#include "mono/metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/method-builder-ilgen-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internals.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/utils/strenc.h"
#include "mono/utils/atomic.h"
#include "mono/utils/mono-error.h"
#include "mono/utils/mono-error-internals.h"
#include <string.h>
#include <errno.h>
#include <mono/utils/w32api.h>
#if defined (HOST_WIN32)
MONO_PRAGMA_WARNING_PUSH()
MONO_PRAGMA_WARNING_DISABLE (4115) // warning C4115: 'IRpcStubBuffer': named type definition in parentheses
#include <oleauto.h>
MONO_PRAGMA_WARNING_POP()
#include <mono/utils/w32subset.h>
#endif
#include "icall-decl.h"
#include "icall-signatures.h"

static void
mono_System_ComObject_ReleaseInterfaces (MonoComObjectHandle obj);

#if !defined (DISABLE_COM) || defined (HOST_WIN32)

static int
mono_IUnknown_QueryInterface (MonoIUnknown *pUnk, gconstpointer riid, gpointer* ppv)
{
	g_assert (pUnk);
	return pUnk->vtable->QueryInterface (pUnk, riid, ppv);
}

static int
mono_IUnknown_AddRef (MonoIUnknown *pUnk)
{
	// The return value is a reference count, generally transient, generally not to be used, except for debugging,
	// or to assert that it is > 0.
	g_assert (pUnk);
	return pUnk->vtable->AddRef (pUnk);
}

static int
mono_IUnknown_Release (MonoIUnknown *pUnk)
{
	// Release is like free -- null is silently ignored.
	// Also, the return value is a reference count, generally transient, generally not to be used, except for debugging.
	return pUnk ? pUnk->vtable->Release (pUnk) : 0;
}

#endif

/*
Code shared between the DISABLE_COM and !DISABLE_COM
*/

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

mono_bstr
mono_string_to_bstr_impl (MonoStringHandle s, MonoError *error)
{
	if (MONO_HANDLE_IS_NULL (s))
		return NULL;

	MonoGCHandle gchandle = NULL;
	mono_bstr const res = mono_ptr_to_bstr (mono_string_handle_pin_chars (s, &gchandle), mono_string_handle_length (s));
	mono_gchandle_free_internal (gchandle);
	return res;
}

static void*
mono_cominterop_get_com_interface_internal (gboolean icall, MonoObjectHandle object, MonoClass *ic, MonoError *error);

#ifndef DISABLE_COM

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,
typedef enum {
	MONO_MARSHAL_NONE,			/* No marshalling needed */
	MONO_MARSHAL_COPY,			/* Can be copied by value to the new domain */
	MONO_MARSHAL_COPY_OUT,		/* out parameter that needs to be copied back to the original instance */
	MONO_MARSHAL_SERIALIZE		/* Value needs to be serialized into the new domain */
} MonoXDomainMarshalType;

typedef enum {
	MONO_COM_DEFAULT,
	MONO_COM_MS
} MonoCOMProvider;

static MonoCOMProvider com_provider = MONO_COM_DEFAULT;

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

/* This mutex protects the various cominterop related caches in MonoImage */
#define mono_cominterop_lock() mono_os_mutex_lock (&cominterop_mutex)
#define mono_cominterop_unlock() mono_os_mutex_unlock (&cominterop_mutex)
static mono_mutex_t cominterop_mutex;

GENERATE_GET_CLASS_WITH_CACHE (interop_proxy, "Mono.Interop", "ComInteropProxy")
GENERATE_GET_CLASS_WITH_CACHE (idispatch,     "Mono.Interop", "IDispatch")
GENERATE_GET_CLASS_WITH_CACHE (iunknown,      "Mono.Interop", "IUnknown")

GENERATE_GET_CLASS_WITH_CACHE (com_object, "System", "__ComObject")
GENERATE_GET_CLASS_WITH_CACHE (variant,    "System", "Variant")

static GENERATE_GET_CLASS_WITH_CACHE (interface_type_attribute, "System.Runtime.InteropServices", "InterfaceTypeAttribute")
static GENERATE_GET_CLASS_WITH_CACHE (com_visible_attribute, "System.Runtime.InteropServices", "ComVisibleAttribute")
static GENERATE_GET_CLASS_WITH_CACHE (com_default_interface_attribute, "System.Runtime.InteropServices", "ComDefaultInterfaceAttribute")
static GENERATE_GET_CLASS_WITH_CACHE (class_interface_attribute, "System.Runtime.InteropServices", "ClassInterfaceAttribute")

/* Upon creation of a CCW, only allocate a weak handle and set the
 * reference count to 0. If the unmanaged client code decides to addref and
 * hold onto the CCW, I then allocate a strong handle. Once the reference count
 * goes back to 0, convert back to a weak handle.
 */
typedef struct {
	guint32 ref_count;
	MonoGCHandle gc_handle;
	GHashTable* vtable_hash;
#ifdef  HOST_WIN32
	MonoIUnknown *free_marshaler; // actually IMarshal
#endif
} MonoCCW;

/* This type is the actual pointer passed to unmanaged code
 * to represent a COM interface.
 */
typedef struct {
	gpointer vtable;
	MonoCCW* ccw;
} MonoCCWInterface;

/*
 * COM Callable Wrappers
 *
 * CCWs may be called on threads that aren't attached to the runtime, they can
 * then run managed code or the method implementations may use coop handles.
 * Use the macros below to setup the thread state.
 *
 * For managed methods, the runtime marshaling wrappers handle attaching and
 * coop state switching.
 */

#define MONO_CCW_CALL_ENTER do {					\
	gpointer dummy;							\
	gpointer orig_domain = mono_threads_attach_coop (mono_domain_get (), &dummy); \
	MONO_ENTER_GC_UNSAFE;						\
	HANDLE_FUNCTION_ENTER ();					\
	do {} while (0)

#define MONO_CCW_CALL_EXIT				\
	HANDLE_FUNCTION_RETURN ();			\
	MONO_EXIT_GC_UNSAFE;				\
	mono_threads_detach_coop (orig_domain, &dummy); \
	} while (0)


/* IUnknown */
static int STDCALL cominterop_ccw_addref (MonoCCWInterface* ccwe);

static int STDCALL cominterop_ccw_release (MonoCCWInterface* ccwe);

static int STDCALL cominterop_ccw_queryinterface (MonoCCWInterface* ccwe, const guint8* riid, gpointer* ppv);

/* IDispatch */
static int STDCALL cominterop_ccw_get_type_info_count (MonoCCWInterface* ccwe, guint32 *pctinfo);

static int STDCALL cominterop_ccw_get_type_info (MonoCCWInterface* ccwe, guint32 iTInfo, guint32 lcid, gpointer *ppTInfo);

static int STDCALL cominterop_ccw_get_ids_of_names (MonoCCWInterface* ccwe, gpointer riid,
											 gunichar2** rgszNames, guint32 cNames,
											 guint32 lcid, gint32 *rgDispId);

static int STDCALL cominterop_ccw_invoke (MonoCCWInterface* ccwe, guint32 dispIdMember,
								   gpointer riid, guint32 lcid,
								   guint16 wFlags, gpointer pDispParams,
								   gpointer pVarResult, gpointer pExcepInfo,
								   guint32 *puArgErr);

static MonoMethod *
cominterop_get_managed_wrapper_adjusted (MonoMethod *method);

static gpointer
cominterop_get_ccw (MonoObject* object, MonoClass* itf);

static gpointer
cominterop_get_ccw_checked (MonoObjectHandle object, MonoClass *itf, MonoError *error);

static MonoObject*
cominterop_get_ccw_object (MonoCCWInterface* ccw_entry, gboolean verify);

static MonoObjectHandle
cominterop_get_ccw_handle (MonoCCWInterface* ccw_entry, gboolean verify);

static MonoObject*
cominterop_set_ccw_object_domain (MonoObject *object, MonoDomain **prev_domain);

static void
cominterop_restore_domain (MonoDomain *domain);

/* SAFEARRAY marshalling */
static gboolean
mono_marshal_safearray_begin (gpointer safearray, MonoArray **result, gpointer *indices, gpointer empty, gpointer parameter, gboolean allocateNewArray);

static gpointer
mono_marshal_safearray_get_value (gpointer safearray, gpointer indices);

static gboolean
mono_marshal_safearray_next (gpointer safearray, gpointer indices);

static void
mono_marshal_safearray_end (gpointer safearray, gpointer indices);

static gboolean
mono_marshal_safearray_create (MonoArray *input, gpointer *newsafearray, gpointer *indices, gpointer empty);

static void
mono_marshal_safearray_set_value (gpointer safearray, gpointer indices, gpointer value);

static void
mono_marshal_safearray_free_indices (gpointer indices);

MonoClass*
mono_class_try_get_com_object_class (void)
{
	static MonoClass *tmp_class;
	static gboolean inited;
	MonoClass *klass;
	if (!inited) {
		klass = mono_class_load_from_name (mono_defaults.corlib, "System", "__ComObject");
		mono_memory_barrier ();
		tmp_class = klass;
		mono_memory_barrier ();
		inited = TRUE;
	}
	return tmp_class;
}

/**
 * cominterop_method_signature:
 * @method: a method
 *
 * Returns: the corresponding unmanaged method signature for a managed COM
 * method.
 */
static MonoMethodSignature*
cominterop_method_signature (MonoMethod* method)
{
	MonoMethodSignature *res;
	MonoImage *image = m_class_get_image (method->klass);
	MonoMethodSignature *sig = mono_method_signature_internal (method);
	gboolean const preserve_sig = (method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG) != 0;
	int sigsize;
	int i;
	int param_count = sig->param_count + 1; // convert this arg into IntPtr arg

	if (!preserve_sig &&!MONO_TYPE_IS_VOID (sig->ret))
		param_count++;

	res = mono_metadata_signature_alloc (image, param_count);
	sigsize = MONO_SIZEOF_METHOD_SIGNATURE + sig->param_count * sizeof (MonoType *);
	memcpy (res, sig, sigsize);

	// now move args forward one
	for (i = sig->param_count-1; i >= 0; i--)
		res->params[i+1] = sig->params[i];

	// first arg is interface pointer
	res->params[0] = mono_get_int_type ();

	if (preserve_sig) {
		res->ret = sig->ret;
	}
	else {
		// last arg is return type
		if (!MONO_TYPE_IS_VOID (sig->ret)) {
			res->params[param_count-1] = mono_metadata_type_dup (image, sig->ret);
			res->params[param_count-1]->byref__ = 1;
			res->params[param_count-1]->attrs = PARAM_ATTRIBUTE_OUT;
		}

		// return type is always int32 (HRESULT)
		res->ret = mono_get_int32_type ();
	}

	// no pinvoke
	res->pinvoke = FALSE;

	// no hasthis
	res->hasthis = 0;

	// set param_count
	res->param_count = param_count;

	// STDCALL on windows, CDECL everywhere else to work with XPCOM and MainWin COM
#ifdef HOST_WIN32
	res->call_convention = MONO_CALL_STDCALL;
#else
	res->call_convention = MONO_CALL_C;
#endif

	return res;
}

/**
 * cominterop_get_function_pointer:
 * @itf: a pointer to the COM interface
 * @slot: the vtable slot of the method pointer to return
 *
 * Returns: the unmanaged vtable function pointer from the interface
 */
static gpointer
cominterop_get_function_pointer (gpointer itf, int slot)
{
	gpointer func;
	func = *((*(gpointer**)itf)+slot);
	return func;
}

/**
 * cominterop_object_is_com_object:
 * @obj: a pointer to the object
 *
 * Returns: a value indicating if the object is a
 * Runtime Callable Wrapper (RCW) for a COM object
 */
static gboolean
cominterop_object_is_rcw_handle (MonoObjectHandle obj, MonoRealProxyHandle *real_proxy)
{
	MonoClass *klass;

	return  !MONO_HANDLE_IS_NULL (obj)
		&& (klass = mono_handle_class (obj))
		&& !MONO_HANDLE_IS_NULL (*real_proxy = MONO_HANDLE_NEW_GET (MonoRealProxy, MONO_HANDLE_CAST (MonoTransparentProxy, obj), rp))
		&& (klass = mono_handle_class (*real_proxy))
		&& klass == mono_class_get_interop_proxy_class ();
}

static gboolean
cominterop_object_is_rcw (MonoObject *obj_raw)
{
	if (!obj_raw)
		return FALSE;
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoObject, obj);
	MonoRealProxyHandle real_proxy;
	gboolean const result = cominterop_object_is_rcw_handle (obj, &real_proxy);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static int
cominterop_get_com_slot_begin (MonoClass* klass)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo = NULL;
	MonoInterfaceTypeAttribute* itf_attr = NULL;

	cinfo = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_assert_ok (error);
	if (cinfo) {
		itf_attr = (MonoInterfaceTypeAttribute*)mono_custom_attrs_get_attr_checked (cinfo, mono_class_get_interface_type_attribute_class (), error);
		mono_error_assert_ok (error); /*FIXME proper error handling*/
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	if (itf_attr && itf_attr->intType == 1)
		return 3; /* 3 methods in IUnknown*/
	else
		return 7; /* 7 methods in IDispatch*/
}

/**
 * cominterop_get_method_interface:
 * @method: method being called
 *
 * Returns: the MonoClass* representing the interface on which
 * the method is defined.
 */
static MonoClass*
cominterop_get_method_interface (MonoMethod* method)
{
	ERROR_DECL (error);
	MonoClass *ic = method->klass;

	/* if method is on a class, we need to look up interface method exists on */
	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (method->klass)) {
		GPtrArray *ifaces = mono_class_get_implemented_interfaces (method->klass, error);
		mono_error_assert_ok (error);
		if (ifaces) {
			int i;
			mono_class_setup_vtable (method->klass);
			for (i = 0; i < ifaces->len; ++i) {
				int j, offset;
				gboolean found = FALSE;
				ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				offset = mono_class_interface_offset (method->klass, ic);
				int mcount = mono_class_get_method_count (ic);
				MonoMethod **method_klass_vtable = m_class_get_vtable (method->klass);
				for (j = 0; j < mcount; ++j) {
					if (method_klass_vtable [j + offset] == method) {
						found = TRUE;
						break;
					}
				}
				if (found)
					break;
				ic = NULL;
			}
			g_ptr_array_free (ifaces, TRUE);
		}
	}

	return ic;
}

static void
mono_cominterop_get_interface_missing_error (MonoError* error, MonoMethod* method)
{
	mono_error_set_invalid_operation (error, "Method '%s' in ComImport class '%s' must implement an interface method.", method->name, m_class_get_name (method->klass));
}

/**
 * cominterop_get_com_slot_for_method:
 * @method: a method
 * @error: set on error
 *
 * Returns: the method's slot in the COM interface vtable
 */
static int
cominterop_get_com_slot_for_method (MonoMethod* method, MonoError* error)
{
	guint32 slot = method->slot;
 	MonoClass *ic = method->klass;

	error_init (error);

	/* if method is on a class, we need to look up interface method exists on */
	if (!MONO_CLASS_IS_INTERFACE_INTERNAL (ic)) {
		int offset = 0;
		int i = 0;
		ic = cominterop_get_method_interface (method);
		if (!ic || !MONO_CLASS_IS_INTERFACE_INTERNAL (ic)) {
			mono_cominterop_get_interface_missing_error (error, method);
			return -1;
		}
		offset = mono_class_interface_offset (method->klass, ic);
		g_assert(offset >= 0);
		int mcount = mono_class_get_method_count (ic);
		MonoMethod **ic_methods = m_class_get_methods (ic);
		MonoMethod **method_klass_vtable = m_class_get_vtable (method->klass);
		for(i = 0; i < mcount; ++i) {
			if (method_klass_vtable [i + offset] == method)
			{
				slot = ic_methods[i]->slot;
				break;
			}
		}
	}

	g_assert (ic);
	g_assert (MONO_CLASS_IS_INTERFACE_INTERNAL (ic));

	return slot + cominterop_get_com_slot_begin (ic);
}

static gboolean
cominterop_class_guid (MonoClass* klass, guint8* guid)
{
	ERROR_DECL (error);
	mono_metadata_get_class_guid (klass, guid, error);
	mono_error_assert_ok (error); /*FIXME proper error handling*/
	return TRUE;
}

static gboolean
cominterop_com_visible (MonoClass* klass)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo;
	GPtrArray *ifaces;
	MonoBoolean visible = 1;

	cinfo = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_assert_ok (error);
	if (cinfo) {
		MonoReflectionComVisibleAttribute *attr = (MonoReflectionComVisibleAttribute*)mono_custom_attrs_get_attr_checked (cinfo, mono_class_get_com_visible_attribute_class (), error);
		mono_error_assert_ok (error); /*FIXME proper error handling*/

		if (attr)
			visible = attr->visible;
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		if (visible)
			return TRUE;
	}

	ifaces = mono_class_get_implemented_interfaces (klass, error);
	mono_error_assert_ok (error);
	if (ifaces) {
		int i;
		for (i = 0; i < ifaces->len; ++i) {
			MonoClass *ic = NULL;
			ic = (MonoClass *)g_ptr_array_index (ifaces, i);
			if (MONO_CLASS_IS_IMPORT (ic))
				visible = TRUE;

		}
		g_ptr_array_free (ifaces, TRUE);
	}
	return visible;

}

gboolean
mono_cominterop_method_com_visible (MonoMethod *method)
{
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo;
	MonoBoolean visible = 1;

	cinfo = mono_custom_attrs_from_method_checked (method, error);
	mono_error_assert_ok (error);
	if (cinfo) {
		MonoReflectionComVisibleAttribute *attr = (MonoReflectionComVisibleAttribute*)mono_custom_attrs_get_attr_checked (cinfo, mono_class_get_com_visible_attribute_class (), error);
		mono_error_assert_ok (error); /*FIXME proper error handling*/

		if (attr)
			visible = attr->visible;
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}
	return visible;
}

static void
cominterop_set_hr_error (MonoError *oerror, int hr)
{
	ERROR_DECL (error);
	MonoException* ex;
	void* params[1] = {&hr};

	MONO_STATIC_POINTER_INIT (MonoMethod, throw_exception_for_hr)

		throw_exception_for_hr = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetExceptionForHR", 1, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, throw_exception_for_hr)

	ex = (MonoException*)mono_runtime_invoke_checked (throw_exception_for_hr, NULL, params, error);
	g_assert (ex);
	mono_error_assert_ok (error);

	mono_error_set_exception_instance (oerror, ex);
}

/**
 * cominterop_get_interface_checked:
 * @obj: managed wrapper object containing COM object
 * @ic: interface type to retrieve for COM object
 * @error: set on error
 *
 * Returns: the COM interface requested. On failure returns NULL and sets @error
 */
static gpointer
cominterop_get_interface_checked (MonoComObjectHandle obj, MonoClass* ic, MonoError *error)
{
	gpointer itf = NULL;

	g_assert (ic);
	g_assert (MONO_CLASS_IS_INTERFACE_INTERNAL (ic));

	error_init (error);

	mono_cominterop_lock ();
	if (MONO_HANDLE_GETVAL (obj, itf_hash))
		itf = g_hash_table_lookup (MONO_HANDLE_GETVAL (obj, itf_hash), GUINT_TO_POINTER ((guint)m_class_get_interface_id (ic)));
	mono_cominterop_unlock ();

	if (itf)
		return itf;

	guint8 iid [16];
	gboolean const found = cominterop_class_guid (ic, iid);
	g_assert (found);
	g_assert (MONO_HANDLE_GETVAL (obj, iunknown));
	int const hr = mono_IUnknown_QueryInterface (MONO_HANDLE_GETVAL (obj, iunknown), iid, &itf);
	if (hr < 0) {
		g_assert (!itf);
		cominterop_set_hr_error (error, hr);
		g_assert (!is_ok (error));
		return NULL;
	}

	g_assert (itf);
	mono_cominterop_lock ();
	if (!MONO_HANDLE_GETVAL (obj, itf_hash))
		MONO_HANDLE_SETVAL (obj, itf_hash, GHashTable*, g_hash_table_new (mono_aligned_addr_hash, NULL));
	g_hash_table_insert (MONO_HANDLE_GETVAL (obj, itf_hash), GUINT_TO_POINTER ((guint)m_class_get_interface_id (ic)), itf);
	mono_cominterop_unlock ();

	return itf;
}

/**
 * cominterop_get_interface:
 * @obj: managed wrapper object containing COM object
 * @ic: interface type to retrieve for COM object
 *
 * Returns: the COM interface requested
 */
static gpointer
cominterop_get_interface (MonoComObject *obj_raw, MonoClass *ic)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoComObject, obj);
	gpointer const itf = cominterop_get_interface_checked (obj, ic, error);
	g_assert (!!itf == is_ok (error)); // two equal success indicators
	mono_error_set_pending_exception (error);
	HANDLE_FUNCTION_RETURN_VAL (itf);
}

// This is an icall, it will return NULL and set pending exception (in
// mono_type_from_handle wrapper) on failure.
static MonoReflectionType *
cominterop_type_from_handle (MonoType *handle)
{
	return mono_type_from_handle (handle);
}

#endif // DISABLE_COM

void
mono_cominterop_init (void)
{
#ifndef DISABLE_COM
	mono_os_mutex_init_recursive (&cominterop_mutex);

	char* const com_provider_env = g_getenv ("MONO_COM");
	if (com_provider_env && !strcmp(com_provider_env, "MS"))
		com_provider = MONO_COM_MS;
	g_free (com_provider_env);

	register_icall (cominterop_get_method_interface, mono_icall_sig_ptr_ptr, FALSE);
	register_icall (cominterop_get_function_pointer, mono_icall_sig_ptr_ptr_int32, FALSE);
	register_icall (cominterop_object_is_rcw, mono_icall_sig_int32_object, FALSE);
	register_icall (cominterop_get_ccw, mono_icall_sig_ptr_object_ptr, FALSE);
	register_icall (cominterop_get_ccw_object, mono_icall_sig_object_ptr_int32, FALSE);
	register_icall (cominterop_get_interface, mono_icall_sig_ptr_object_ptr, FALSE);

	register_icall (cominterop_type_from_handle, mono_icall_sig_object_ptr, FALSE);

	register_icall (cominterop_set_ccw_object_domain, mono_icall_sig_object_object_ptr, FALSE);
	register_icall (cominterop_restore_domain, mono_icall_sig_void_ptr, FALSE);

	/* SAFEARRAY marshalling */
	register_icall (mono_marshal_safearray_begin, mono_icall_sig_int32_ptr_ptr_ptr_ptr_ptr_int32, FALSE);
	register_icall (mono_marshal_safearray_get_value, mono_icall_sig_ptr_ptr_ptr, FALSE);
	register_icall (mono_marshal_safearray_next, mono_icall_sig_int32_ptr_ptr, FALSE);
	register_icall (mono_marshal_safearray_end, mono_icall_sig_void_ptr_ptr, FALSE);
	register_icall (mono_marshal_safearray_create, mono_icall_sig_int32_object_ptr_ptr_ptr, FALSE);
	register_icall (mono_marshal_safearray_set_value, mono_icall_sig_void_ptr_ptr_ptr, FALSE);
	register_icall (mono_marshal_safearray_free_indices, mono_icall_sig_void_ptr, FALSE);
#endif // DISABLE_COM
	/*FIXME

	This icalls are used by the marshal code when doing PtrToStructure and StructureToPtr and pinvoke.

	If we leave them out and the FullAOT compiler finds the need to emit one of the above 3 wrappers it will
	g_assert.

	The proper fix would be to emit warning, remove them from marshal.c when DISABLE_COM is used and
	emit an exception in the generated IL.
	*/
	register_icall (mono_string_to_bstr, mono_icall_sig_ptr_obj, FALSE);
	register_icall (mono_string_from_bstr_icall, mono_icall_sig_obj_ptr, FALSE);
	register_icall (mono_free_bstr, mono_icall_sig_void_ptr, FALSE);
}

#ifndef DISABLE_COM

void
mono_mb_emit_cominterop_get_function_pointer (MonoMethodBuilder *mb, MonoMethod *method)
{
#ifndef DISABLE_JIT
	int slot;
	ERROR_DECL (error);
	// get function pointer from 1st arg, the COM interface pointer
	mono_mb_emit_ldarg (mb, 0);
	slot = cominterop_get_com_slot_for_method (method, error);
	if (is_ok (error)) {
		mono_mb_emit_icon (mb, slot);
		mono_mb_emit_icall (mb, cominterop_get_function_pointer);
		/* Leaves the function pointer on top of the stack */
	}
	else {
		mono_mb_emit_exception_for_error (mb, error);
	}
	mono_error_cleanup (error);
#endif
}

void
mono_mb_emit_cominterop_call_function_pointer (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
#ifndef DISABLE_JIT
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_SAVE_LMF);
	mono_mb_emit_calli (mb, sig);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_RESTORE_LMF);
#endif /* DISABLE_JIT */
}

void
mono_mb_emit_cominterop_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethod* method)
{
#ifndef DISABLE_JIT
	mono_mb_emit_cominterop_get_function_pointer (mb, method);

	mono_mb_emit_cominterop_call_function_pointer (mb, sig);
#endif /* DISABLE_JIT */
}

void
mono_cominterop_emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
#ifndef DISABLE_JIT
	switch (conv) {
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH: {

		guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
		MonoClass *klass = NULL;

		klass = mono_class_from_mono_type_internal (type);

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		/* load dst to store later */
		mono_mb_emit_ldloc (mb, 1);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, TRUE);
		mono_mb_emit_icall (mb, cominterop_get_ccw_object);
		pos_ccw = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

		MONO_STATIC_POINTER_INIT (MonoMethod, com_interop_proxy_get_proxy)

			ERROR_DECL (error);
			com_interop_proxy_get_proxy = mono_class_get_method_from_name_checked (mono_class_get_interop_proxy_class (), "GetProxy", 2, METHOD_ATTRIBUTE_PRIVATE, error);
			mono_error_assert_ok (error);

		MONO_STATIC_POINTER_INIT_END (MonoMethod, com_interop_proxy_get_proxy)

		static MonoMethod* const get_transparent_proxy = NULL; // FIXME?

		mono_mb_add_local (mb, m_class_get_byval_arg (mono_class_get_interop_proxy_class ()));

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ptr (mb, m_class_get_byval_arg (mono_class_get_com_object_class ()));
		mono_mb_emit_icall (mb, cominterop_type_from_handle);
		mono_mb_emit_managed_call (mb, com_interop_proxy_get_proxy, NULL);
		mono_mb_emit_managed_call (mb, get_transparent_proxy, NULL);
		if (conv == MONO_MARSHAL_CONV_OBJECT_INTERFACE) {
			g_assert (klass);
 			mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
		}
 		mono_mb_emit_byte (mb, CEE_STIND_REF);
		pos_end = mono_mb_emit_short_branch (mb, CEE_BR_S);

		/* is already managed object */
		mono_mb_patch_short_branch (mb, pos_ccw);
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, TRUE);
		mono_mb_emit_icall (mb, cominterop_get_ccw_object);

		if (conv == MONO_MARSHAL_CONV_OBJECT_INTERFACE) {
			g_assert (klass);
			mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
		}
		mono_mb_emit_byte (mb, CEE_STIND_REF);

		mono_mb_patch_short_branch (mb, pos_end);
		/* case if null */
		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}
	default:
		g_assert_not_reached ();
	}
#endif /* DISABLE_JIT */
}

void
mono_cominterop_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
#ifndef DISABLE_JIT
	switch (conv) {
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN: {
		guint32 pos_null = 0, pos_rcw = 0, pos_end = 0;

		mono_mb_emit_ldloc (mb, 1);
		mono_mb_emit_icon (mb, 0);
		mono_mb_emit_byte (mb, CEE_CONV_U);
		mono_mb_emit_byte (mb, CEE_STIND_I);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);

		// if null just break, dst was already inited to 0
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icall (mb, cominterop_object_is_rcw);
		pos_rcw = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		// load dst to store later
		mono_mb_emit_ldloc (mb, 1);

		// load src
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoTransparentProxy, rp));
		mono_mb_emit_byte (mb, CEE_LDIND_REF);

		/* load the RCW from the ComInteropProxy*/
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoComInteropProxy, com_object));
		mono_mb_emit_byte (mb, CEE_LDIND_REF);

		if (conv == MONO_MARSHAL_CONV_OBJECT_INTERFACE) {
			mono_mb_emit_ptr (mb, mono_type_get_class_internal (type));
			mono_mb_emit_icall (mb, cominterop_get_interface);
		}
		else if (conv == MONO_MARSHAL_CONV_OBJECT_IUNKNOWN) {

			MONO_STATIC_POINTER_INIT (MonoProperty, iunknown)
				iunknown = mono_class_get_property_from_name_internal (mono_class_get_com_object_class (), "IUnknown");
			MONO_STATIC_POINTER_INIT_END (MonoProperty, iunknown)

			mono_mb_emit_managed_call (mb, iunknown->get, NULL);
		}
		else if (conv == MONO_MARSHAL_CONV_OBJECT_IDISPATCH) {

			MONO_STATIC_POINTER_INIT (MonoProperty, idispatch)
				idispatch = mono_class_get_property_from_name_internal (mono_class_get_com_object_class (), "IDispatch");
			MONO_STATIC_POINTER_INIT_END (MonoProperty, idispatch)

			mono_mb_emit_managed_call (mb, idispatch->get, NULL);
		}
		else {
			g_assert_not_reached ();
		}
		mono_mb_emit_byte (mb, CEE_STIND_I);
		pos_end = mono_mb_emit_short_branch (mb, CEE_BR_S);

		// if not rcw
		mono_mb_patch_short_branch (mb, pos_rcw);
		/* load dst to store later */
		mono_mb_emit_ldloc (mb, 1);
		/* load src */
		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_REF);

		if (conv == MONO_MARSHAL_CONV_OBJECT_INTERFACE)
			mono_mb_emit_ptr (mb, mono_type_get_class_internal (type));
		else if (conv == MONO_MARSHAL_CONV_OBJECT_IUNKNOWN)
			mono_mb_emit_ptr (mb, mono_class_get_iunknown_class ());
		else if (conv == MONO_MARSHAL_CONV_OBJECT_IDISPATCH)
			mono_mb_emit_ptr (mb, mono_class_get_idispatch_class ());
		else
			g_assert_not_reached ();
		mono_mb_emit_icall (mb, cominterop_get_ccw);
		mono_mb_emit_byte (mb, CEE_STIND_I);

		mono_mb_patch_short_branch (mb, pos_end);
		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}
	default:
		g_assert_not_reached ();
	}
#endif /* DISABLE_JIT */
}

/**
 * cominterop_get_native_wrapper_adjusted:
 * @method: managed COM Interop method
 *
 * Returns: the generated method to call with signature matching
 * the unmanaged COM Method signature
 */
static MonoMethod *
cominterop_get_native_wrapper_adjusted (MonoMethod *method)
{
	MonoMethod *res;
	MonoMethodBuilder *mb_native;
	MonoMarshalSpec **mspecs;
	MonoMethodSignature *sig, *sig_native;
	MonoMethodPInvoke *piinfo = (MonoMethodPInvoke *) method;
	int i;

	sig = mono_method_signature_internal (method);

	// create unmanaged wrapper
	mb_native = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	sig_native = cominterop_method_signature (method);

	mspecs = g_new0 (MonoMarshalSpec*, sig_native->param_count + 1);

	mono_method_get_marshal_info (method, mspecs);

	// move managed args up one
	for (i = sig->param_count; i >= 1; i--)
		mspecs[i+1] = mspecs[i];

	// first arg is IntPtr for interface
	mspecs[1] = NULL;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG)) {
		// move return spec to last param
		if (!MONO_TYPE_IS_VOID (sig->ret))
			mspecs[sig_native->param_count] = mspecs[0];

		mspecs[0] = NULL;
	}

	for (i = 1; i < sig_native->param_count; i++) {
		int mspec_index = i + 1;
		if (mspecs[mspec_index] == NULL) {
			// default object to VARIANT
			if (sig_native->params[i]->type == MONO_TYPE_OBJECT) {
				mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
				mspecs[mspec_index]->native = MONO_NATIVE_STRUCT;
			}
			else if (sig_native->params[i]->type == MONO_TYPE_STRING) {
				mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
				mspecs[mspec_index]->native = MONO_NATIVE_BSTR;
			}
			else if (sig_native->params[i]->type == MONO_TYPE_CLASS) {
				mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
				mspecs[mspec_index]->native = MONO_NATIVE_INTERFACE;
			}
			else if (sig_native->params[i]->type == MONO_TYPE_BOOLEAN) {
				mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
				mspecs[mspec_index]->native = MONO_NATIVE_VARIANTBOOL;
			}
		}
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG) {
		// move return spec to last param
		if (!MONO_TYPE_IS_VOID (sig->ret) && mspecs[0] == NULL) {
			// default object to VARIANT
			if (sig->ret->type == MONO_TYPE_OBJECT) {
				mspecs[0] = g_new0 (MonoMarshalSpec, 1);
				mspecs[0]->native = MONO_NATIVE_STRUCT;
			}
			else if (sig->ret->type == MONO_TYPE_STRING) {
				mspecs[0] = g_new0 (MonoMarshalSpec, 1);
				mspecs[0]->native = MONO_NATIVE_BSTR;
			}
			else if (sig->ret->type == MONO_TYPE_CLASS) {
				mspecs[0] = g_new0 (MonoMarshalSpec, 1);
				mspecs[0]->native = MONO_NATIVE_INTERFACE;
			}
			else if (sig->ret->type == MONO_TYPE_BOOLEAN) {
				mspecs[0] = g_new0 (MonoMarshalSpec, 1);
				mspecs[0]->native = MONO_NATIVE_VARIANTBOOL;
			}
		}
	}

	mono_marshal_emit_native_wrapper (m_class_get_image (method->klass), mb_native, sig_native, piinfo, mspecs, piinfo->addr, EMIT_NATIVE_WRAPPER_CHECK_EXCEPTIONS | EMIT_NATIVE_WRAPPER_RUNTIME_MARSHALLING_ENABLED);

	res = mono_mb_create_method (mb_native, sig_native, sig_native->param_count + 16);

	mono_mb_free (mb_native);

	for (i = sig_native->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

/**
 * mono_cominterop_get_native_wrapper:
 * \param method managed method
 * \returns the generated method to call
 */
MonoMethod *
mono_cominterop_get_native_wrapper (MonoMethod *method)
{
	MonoMethod *res;
	GHashTable *cache;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig, *csig;

	g_assert (method);

	cache = mono_marshal_get_cache (&mono_method_get_wrapper_cache (method)->cominterop_wrapper_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	if (!m_class_get_vtable (method->klass))
		mono_class_setup_vtable (method->klass);

	if (!m_class_get_methods (method->klass))
		mono_class_setup_methods (method->klass);
	g_assert (!mono_class_has_failure (method->klass)); /*FIXME do proper error handling*/

	sig = mono_method_signature_internal (method);
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_COMINTEROP);

#ifndef DISABLE_JIT
	/* if method klass is import, that means method
	 * is really a com call. let interop system emit it.
	*/
	if (MONO_CLASS_IS_IMPORT(method->klass)) {
		/* FIXME: we have to call actual class .ctor
		 * instead of just __ComObject .ctor.
		 */
		if (!strcmp(method->name, ".ctor")) {

			MONO_STATIC_POINTER_INIT (MonoMethod, ctor)

				ERROR_DECL (error);
				ctor = mono_class_get_method_from_name_checked (mono_class_get_com_object_class (), ".ctor", 0, 0, error);
				mono_error_assert_ok (error);

			MONO_STATIC_POINTER_INIT_END (MonoMethod, ctor)

			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_managed_call (mb, ctor, NULL);
			mono_mb_emit_byte (mb, CEE_RET);
		}
		else if (method->flags & METHOD_ATTRIBUTE_STATIC) {
			/*
			 * The method's class must implement an interface.
			 * However, no interfaces are allowed to have static methods.
			 * Thus, calling it should invariably lead to an exception.
			 */
			ERROR_DECL (error);
			mono_cominterop_get_interface_missing_error (error, method);
			mono_mb_emit_exception_for_error (mb, error);
			mono_error_cleanup (error);
		}
		else {
			MonoMethod *adjusted_method;
			int retval = 0;
			int ptr_this;
			int i;
			gboolean const preserve_sig = (method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG) != 0;

			// add local variables
			ptr_this = mono_mb_add_local (mb, mono_get_int_type ());
			if (!MONO_TYPE_IS_VOID (sig->ret))
				retval =  mono_mb_add_local (mb, sig->ret);

			// get the type for the interface the method is defined on
			// and then get the underlying COM interface for that type
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ptr (mb, method);
			mono_mb_emit_icall (mb, cominterop_get_method_interface);
			mono_mb_emit_icall (mb, cominterop_get_interface);
			mono_mb_emit_stloc (mb, ptr_this);

			// arg 1 is unmanaged this pointer
			mono_mb_emit_ldloc (mb, ptr_this);

			// load args
			for (i = 1; i <= sig->param_count; i++)
				mono_mb_emit_ldarg (mb, i);

			// push managed return value as byref last argument
			if (!MONO_TYPE_IS_VOID (sig->ret) && !preserve_sig)
				mono_mb_emit_ldloc_addr (mb, retval);

			adjusted_method = cominterop_get_native_wrapper_adjusted (method);
			mono_mb_emit_managed_call (mb, adjusted_method, NULL);

			if (!preserve_sig) {

				MONO_STATIC_POINTER_INIT (MonoMethod, ThrowExceptionForHR)

					ERROR_DECL (error);
					ThrowExceptionForHR = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "ThrowExceptionForHR", 1, 0, error);
					mono_error_assert_ok (error);

				MONO_STATIC_POINTER_INIT_END (MonoMethod, ThrowExceptionForHR)

				mono_mb_emit_managed_call (mb, ThrowExceptionForHR, NULL);

				// load return value managed is expecting
				if (!MONO_TYPE_IS_VOID (sig->ret))
					mono_mb_emit_ldloc (mb, retval);
			}

			mono_mb_emit_byte (mb, CEE_RET);
		}


	}
	/* Does this case ever get hit? */
	else {
		char *msg = g_strdup ("non imported interfaces on \
			imported classes is not yet implemented.");
		mono_mb_emit_exception (mb, "NotSupportedException", msg);
	}
#endif /* DISABLE_JIT */

	csig = mono_metadata_signature_dup_full (m_class_get_image (method->klass), sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, method,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);
	return res;
}

/**
 * mono_cominterop_get_invoke:
 * \param method managed method
 * \returns the generated method that calls the underlying \c __ComObject
 * rather than the proxy object.
 */
MonoMethod *
mono_cominterop_get_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	int i;
	GHashTable* cache;

	cache = mono_marshal_get_cache (&mono_method_get_wrapper_cache (method)->cominterop_invoke_cache, mono_aligned_addr_hash, NULL);

	g_assert (method);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = mono_signature_no_pinvoke (method);

	/* we cant remote methods without this pointer */
	if (!sig->hasthis)
		return method;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_COMINTEROP_INVOKE);

#ifndef DISABLE_JIT
	/* get real proxy object, which is a ComInteropProxy in this case*/
	mono_mb_add_local (mb, mono_get_object_type ());
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);

	/* load the RCW from the ComInteropProxy*/
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoComInteropProxy, com_object));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);

	/* load args and make the call on the RCW */
	for (i = 1; i <= sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) || mono_class_is_interface (method->klass)) {
		MonoMethod * native_wrapper = mono_cominterop_get_native_wrapper(method);
		mono_mb_emit_managed_call (mb, native_wrapper, NULL);
	}
	else {
		if (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
			mono_mb_emit_op (mb, CEE_CALLVIRT, method);
		else
			mono_mb_emit_op (mb, CEE_CALL, method);
	}

	if (!strcmp(method->name, ".ctor"))	{
		MONO_STATIC_POINTER_INIT (MonoMethod, cache_proxy)

			ERROR_DECL (error);
			cache_proxy = mono_class_get_method_from_name_checked (mono_class_get_interop_proxy_class (), "CacheProxy", 0, 0, error);
			mono_error_assert_ok (error);

		MONO_STATIC_POINTER_INIT_END (MonoMethod, cache_proxy)

		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoTransparentProxy, rp));
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_managed_call (mb, cache_proxy, NULL);
	}

	mono_marshal_emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);
#endif /* DISABLE_JIT */

	res = mono_mb_create_and_cache (cache, method, mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	return res;
}

/* Maps a managed object to its unmanaged representation
 * i.e. it's COM Callable Wrapper (CCW).
 * Key: MonoObject*
 * Value: MonoCCW*
 */
static GHashTable* ccw_hash = NULL;

/* Maps a CCW interface to it's containing CCW.
 * Note that a CCW support many interfaces.
 * Key: MonoCCW*
 * Value: MonoCCWInterface*
 */
static GHashTable* ccw_interface_hash = NULL;

/* Maps the IUnknown value of a RCW to
 * it's MonoComInteropProxy*.
 * Key: void*
 * Value: gchandle
 */
static GHashTable* rcw_hash = NULL;

static MonoMethod*
mono_get_addref (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, AddRef)
		ERROR_DECL (error);
		AddRef = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "AddRef", 1, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, AddRef)

	return AddRef;
}

int
mono_cominterop_emit_marshal_com_interface (EmitMarshalContext *m, int argnum,
											MonoType *t,
											MonoMarshalSpec *spec,
											int conv_arg, MonoType **conv_arg_type,
											MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = t->data.klass;
	ERROR_DECL (error);

	MONO_STATIC_POINTER_INIT (MonoMethod, get_object_for_iunknown)
		get_object_for_iunknown = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetObjectForIUnknown", 1, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_object_for_iunknown)

	MONO_STATIC_POINTER_INIT (MonoMethod, get_iunknown_for_object_internal)
		get_iunknown_for_object_internal = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetIUnknownForObjectInternal", 1, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_iunknown_for_object_internal)

	MONO_STATIC_POINTER_INIT (MonoMethod, get_idispatch_for_object_internal)
		get_idispatch_for_object_internal = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetIDispatchForObjectInternal", 1, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_idispatch_for_object_internal)

	MONO_STATIC_POINTER_INIT (MonoMethod, get_com_interface_for_object_internal)
		get_com_interface_for_object_internal = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetComInterfaceForObjectInternal", 2, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_com_interface_for_object_internal)

	MONO_STATIC_POINTER_INIT (MonoMethod, marshal_release)
		marshal_release = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "Release", 1, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, marshal_release)


#ifdef DISABLE_JIT
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = mono_get_int_type ();
		break;
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		*conv_arg_type = mono_get_int_type ();
		break;
	default:
		break;
	}
#else
	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		guint32 pos_null = 0;

		MonoType *int_type = mono_get_int_type ();
		*conv_arg_type = int_type;
		conv_arg = mono_mb_add_local (mb, int_type);

		mono_mb_emit_ptr (mb, NULL);
		mono_mb_emit_stloc (mb, conv_arg);

		/* we dont need any conversions for out parameters */
		if (m_type_is_byref (t) && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		/* if null just break, conv arg was already inited to 0 */
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_REF);

		if (klass && klass != mono_defaults.object_class) {
			mono_mb_emit_ptr (mb, t);
			mono_mb_emit_icall (mb, cominterop_type_from_handle);
			mono_mb_emit_managed_call (mb, get_com_interface_for_object_internal, NULL);
		}
		else if (spec->native == MONO_NATIVE_IUNKNOWN)
			mono_mb_emit_managed_call (mb, get_iunknown_for_object_internal, NULL);
		else if (spec->native == MONO_NATIVE_IDISPATCH)
			mono_mb_emit_managed_call (mb, get_idispatch_for_object_internal, NULL);
		else if (!klass && spec->native == MONO_NATIVE_INTERFACE)
			mono_mb_emit_managed_call (mb, get_iunknown_for_object_internal, NULL);
		else
			g_assert_not_reached ();
		mono_mb_emit_stloc (mb, conv_arg);
		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}

	case MARSHAL_ACTION_CONV_OUT: {
		if (m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			int ccw_obj;
			guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
			ccw_obj = mono_mb_add_local (mb, mono_get_object_type ());

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDNULL);
			mono_mb_emit_byte (mb, CEE_STIND_REF);

			mono_mb_emit_ldloc (mb, conv_arg);
			pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_icon (mb, TRUE);
			mono_mb_emit_icall (mb, cominterop_get_ccw_object);
			mono_mb_emit_stloc (mb, ccw_obj);
			mono_mb_emit_ldloc (mb, ccw_obj);
			pos_ccw = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_managed_call (mb, get_object_for_iunknown, NULL);

			if (klass && klass != mono_defaults.object_class)
				mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
			mono_mb_emit_byte (mb, CEE_STIND_REF);

			pos_end = mono_mb_emit_short_branch (mb, CEE_BR_S);

			/* is already managed object */
			mono_mb_patch_short_branch (mb, pos_ccw);
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, ccw_obj);

			if (klass && klass != mono_defaults.object_class)
				mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
			mono_mb_emit_byte (mb, CEE_STIND_REF);

			mono_mb_patch_short_branch (mb, pos_end);

			/* need to call Release to follow COM rules of ownership */
			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_managed_call (mb, marshal_release, NULL);
			mono_mb_emit_byte (mb, CEE_POP);

			/* case if null */
			mono_mb_patch_short_branch (mb, pos_null);
		}
		break;
	}
	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT: {
		int ccw_obj, ret_ptr;
		guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
		ccw_obj = mono_mb_add_local (mb, mono_get_object_type ());
		ret_ptr = mono_mb_add_local (mb, mono_get_int_type ());

		/* store return value */
		mono_mb_emit_stloc (mb, ret_ptr);

		mono_mb_emit_ldloc (mb, ret_ptr);
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldloc (mb, ret_ptr);
		mono_mb_emit_icon (mb, TRUE);
		mono_mb_emit_icall (mb, cominterop_get_ccw_object);
		mono_mb_emit_stloc (mb, ccw_obj);
		mono_mb_emit_ldloc (mb, ccw_obj);
		pos_ccw = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

		mono_mb_emit_ldloc (mb, ret_ptr);
		mono_mb_emit_managed_call (mb, get_object_for_iunknown, NULL);

		if (klass && klass != mono_defaults.object_class)
			mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
		mono_mb_emit_stloc (mb, 3);

		pos_end = mono_mb_emit_short_branch (mb, CEE_BR_S);

		/* is already managed object */
		mono_mb_patch_short_branch (mb, pos_ccw);
		mono_mb_emit_ldloc (mb, ccw_obj);

		if (klass && klass != mono_defaults.object_class)
			mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
		mono_mb_emit_stloc (mb, 3);

		mono_mb_patch_short_branch (mb, pos_end);

		/* need to call Release to follow COM rules of ownership */
		mono_mb_emit_ldloc (mb, ret_ptr);
		mono_mb_emit_managed_call (mb, marshal_release, NULL);
		mono_mb_emit_byte (mb, CEE_POP);

		/* case if null */
		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		int ccw_obj;
		guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
		ccw_obj = mono_mb_add_local (mb, mono_get_object_type ());

		klass = mono_class_from_mono_type_internal (t);
		conv_arg = mono_mb_add_local (mb, m_class_get_byval_arg (klass));
		*conv_arg_type = mono_get_int_type ();

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);
		if (t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icon (mb, TRUE);
		mono_mb_emit_icall (mb, cominterop_get_ccw_object);
		mono_mb_emit_stloc (mb, ccw_obj);
		mono_mb_emit_ldloc (mb, ccw_obj);
		pos_ccw = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);


		mono_mb_emit_ldarg (mb, argnum);
		if (m_type_is_byref (t))
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_managed_call (mb, get_object_for_iunknown, NULL);

		if (klass && klass != mono_defaults.object_class)
			mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
		mono_mb_emit_stloc (mb, conv_arg);
		pos_end = mono_mb_emit_short_branch (mb, CEE_BR_S);

		/* is already managed object */
		mono_mb_patch_short_branch (mb, pos_ccw);
		mono_mb_emit_ldloc (mb, ccw_obj);
		if (klass && klass != mono_defaults.object_class)
			mono_mb_emit_op (mb, CEE_CASTCLASS, klass);
		mono_mb_emit_stloc (mb, conv_arg);

		mono_mb_patch_short_branch (mb, pos_end);
		/* case if null */
		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_OUT: {
		if (m_type_is_byref (t) && t->attrs & PARAM_ATTRIBUTE_OUT) {
			guint32 pos_null = 0;

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_byte (mb, CEE_STIND_I);

			mono_mb_emit_ldloc (mb, conv_arg);
			pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

			/* to store later */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_ldloc (mb, conv_arg);
			if (klass && klass != mono_defaults.object_class) {
				mono_mb_emit_ptr (mb, t);
				mono_mb_emit_icall (mb, cominterop_type_from_handle);
				mono_mb_emit_managed_call (mb, get_com_interface_for_object_internal, NULL);
			}
			else if (spec->native == MONO_NATIVE_IUNKNOWN)
				mono_mb_emit_managed_call (mb, get_iunknown_for_object_internal, NULL);
			else if (spec->native == MONO_NATIVE_IDISPATCH)
				mono_mb_emit_managed_call (mb, get_idispatch_for_object_internal, NULL);
			else if (!klass && spec->native == MONO_NATIVE_INTERFACE)
				mono_mb_emit_managed_call (mb, get_iunknown_for_object_internal, NULL);
			else
				g_assert_not_reached ();
			mono_mb_emit_byte (mb, CEE_STIND_I);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			mono_mb_emit_managed_call (mb, mono_get_addref (), NULL);
			mono_mb_emit_byte (mb, CEE_POP);

			mono_mb_patch_short_branch (mb, pos_null);
		}
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_RESULT: {
		guint32 pos_null = 0;
		int ccw_obj;
		ccw_obj = mono_mb_add_local (mb, mono_get_object_type ());

		/* store return value */
		mono_mb_emit_stloc (mb, ccw_obj);

		mono_mb_emit_ldloc (mb, ccw_obj);

		/* if null just break, conv arg was already inited to 0 */
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		/* to store later */
		mono_mb_emit_ldloc (mb, ccw_obj);
		if (klass && klass != mono_defaults.object_class) {
			mono_mb_emit_ptr (mb, t);
			mono_mb_emit_icall (mb, cominterop_type_from_handle);
			mono_mb_emit_managed_call (mb, get_com_interface_for_object_internal, NULL);
		}
		else if (spec->native == MONO_NATIVE_IUNKNOWN)
			mono_mb_emit_managed_call (mb, get_iunknown_for_object_internal, NULL);
		else if (spec->native == MONO_NATIVE_IDISPATCH)
			mono_mb_emit_managed_call (mb, get_idispatch_for_object_internal, NULL);
		else if (!klass && spec->native == MONO_NATIVE_INTERFACE)
			mono_mb_emit_managed_call (mb, get_iunknown_for_object_internal, NULL);
		else
			g_assert_not_reached ();
		mono_mb_emit_stloc (mb, 3);
		mono_mb_emit_ldloc (mb, 3);

		mono_mb_emit_managed_call (mb, mono_get_addref (), NULL);
		mono_mb_emit_byte (mb, CEE_POP);

		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}

	default:
		g_assert_not_reached ();
	}
#endif /* DISABLE_JIT */

	return conv_arg;
}

#define MONO_S_OK 0x00000000L
#define MONO_E_NOINTERFACE 0x80004002L
#define MONO_E_NOTIMPL 0x80004001L
#define MONO_E_INVALIDARG          0x80070057L
#define MONO_E_DISP_E_UNKNOWNNAME  0x80020006L
#define MONO_E_DISPID_UNKNOWN      (gint32)-1

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (MonoIUnknown *pUnk)
{
	return mono_IUnknown_AddRef (pUnk);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (MonoIUnknown *pUnk, gconstpointer riid, gpointer* ppv)
{
	return mono_IUnknown_QueryInterface (pUnk, riid, ppv);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (MonoIUnknown *pUnk)
{
	g_assert (pUnk);
	return mono_IUnknown_Release (pUnk);
}

static gboolean
cominterop_can_support_dispatch (MonoClass* klass)
{
	if (!mono_class_is_public (klass))
		return FALSE;

	if (!cominterop_com_visible (klass))
		return FALSE;

	return TRUE;
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal (MonoObjectHandle object, MonoError *error)
{
	return mono_cominterop_get_com_interface_internal (TRUE, object, NULL, error);
}

MonoObjectHandle
ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW (void* pUnk, MonoError *error)
{
#ifndef DISABLE_COM
	/* see if it is a CCW */
	return pUnk ? cominterop_get_ccw_handle ((MonoCCWInterface*)pUnk, TRUE) : NULL_HANDLE;
#else
	g_assert_not_reached ();
#endif
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIDispatchForObjectInternal (MonoObjectHandle object, MonoError *error)
{
#ifndef DISABLE_COM
	if (MONO_HANDLE_IS_NULL (object))
		return NULL;

	MonoRealProxyHandle real_proxy;

	if (cominterop_object_is_rcw_handle (object, &real_proxy)) {
		MonoComInteropProxyHandle com_interop_proxy = MONO_HANDLE_CAST (MonoComInteropProxy, real_proxy);
		MonoComObjectHandle com_object = MONO_HANDLE_NEW_GET (MonoComObject, com_interop_proxy, com_object);
		return cominterop_get_interface_checked (com_object, mono_class_get_idispatch_class (), error);
	}
	else if (!cominterop_can_support_dispatch (mono_handle_class (object)) ) {
		cominterop_set_hr_error (error, MONO_E_NOINTERFACE);
		return NULL;
	}
	return cominterop_get_ccw_checked (object, mono_class_get_idispatch_class (), error);
#else
	g_assert_not_reached ();
#endif
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetCCW (MonoObjectHandle object, MonoReflectionTypeHandle ref_type, MonoError *error)
{
#ifndef DISABLE_COM
	g_assert (!MONO_HANDLE_IS_NULL (ref_type));
	MonoType * const type = MONO_HANDLE_GETVAL (ref_type, type);
	g_assert (type);
	MonoClass * klass = mono_type_get_class_internal (type);
	g_assert (klass);
	if (!mono_class_init_checked (klass, error))
		return NULL;

	MonoCustomAttrInfo *cinfo = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_assert_ok (error);
	if (cinfo) {
		MonoReflectionComDefaultInterfaceAttribute *attr = (MonoReflectionComDefaultInterfaceAttribute *)
			mono_custom_attrs_get_attr_checked (cinfo, mono_class_get_com_default_interface_attribute_class (), error);
		mono_error_assert_ok (error); /*FIXME proper error handling*/

		if (attr) {
			MonoType *def_itf = attr->type->type;
			if (def_itf->type == MONO_TYPE_CLASS)
				klass = mono_type_get_class_internal (def_itf);
		}
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	return cominterop_get_ccw_checked (object, klass, error);
#else
	g_assert_not_reached ();
#endif
}

MonoBoolean
ves_icall_System_Runtime_InteropServices_Marshal_IsComObject (MonoObjectHandle object, MonoError *error)
{
#ifndef DISABLE_COM
	MonoRealProxyHandle real_proxy;
	return (MonoBoolean)cominterop_object_is_rcw_handle (object, &real_proxy);
#else
	g_assert_not_reached ();
#endif
}

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal (MonoObjectHandle object, MonoError *error)
{
#ifndef DISABLE_COM
	g_assert (!MONO_HANDLE_IS_NULL (object));

	MonoRealProxyHandle real_proxy;
	gboolean const is_rcw = cominterop_object_is_rcw_handle (object, &real_proxy);
	g_assert (is_rcw);

	MonoComInteropProxyHandle proxy = MONO_HANDLE_CAST (MonoComInteropProxy, real_proxy);
	g_assert (!MONO_HANDLE_IS_NULL (proxy));

	if (MONO_HANDLE_GETVAL (proxy, ref_count) == 0)
		return -1;

	gint32 ref_count = mono_atomic_dec_i32 (&MONO_HANDLE_GETVAL (proxy, ref_count));
	g_assert (ref_count >= 0);

	if (ref_count == 0)
		mono_System_ComObject_ReleaseInterfaces (MONO_HANDLE_NEW_GET (MonoComObject, proxy, com_object));

	return ref_count;
#else
	g_assert_not_reached ();
#endif
}

guint32
ves_icall_System_Runtime_InteropServices_Marshal_GetComSlotForMethodInfoInternal (MonoReflectionMethodHandle m, MonoError *error)
{
#ifndef DISABLE_COM
	int const slot = cominterop_get_com_slot_for_method (MONO_HANDLE_GETVAL (m, method), error);
	mono_error_assert_ok (error);
	return slot;
#else
	g_assert_not_reached ();
#endif
}

/* Only used for COM RCWs */
MonoObjectHandle
ves_icall_System_ComObject_CreateRCW (MonoReflectionTypeHandle ref_type, MonoError *error)
{
	MonoDomain * const domain = MONO_HANDLE_DOMAIN (ref_type);
	MonoType * const type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass * const klass = mono_class_from_mono_type_internal (type);

	/* Call mono_object_new_alloc_by_vtable instead of mono_object_new_by_vtable
	 * because we want to actually create object. mono_object_new_by_vtable checks
	 * to see if type is import and creates transparent proxy. This method
	 * is called by the corresponding real proxy to create the real RCW.
	 * Constructor does not need to be called. Will be called later.
	 */
	MonoVTable *vtable = mono_class_vtable_checked (klass, error);
	return_val_if_nok (error, NULL_HANDLE);
	return mono_object_new_alloc_by_vtable (vtable, error);
}

static gboolean
cominterop_rcw_interface_finalizer (gpointer key, gpointer value, gpointer user_data)
{
	mono_IUnknown_Release ((MonoIUnknown*)value);
	return TRUE;
}

void
mono_System_ComObject_ReleaseInterfaces (MonoComObjectHandle obj)
{
	g_assert (!MONO_HANDLE_IS_NULL (obj));
	if (!MONO_HANDLE_GETVAL (obj, itf_hash))
		return;

	mono_cominterop_lock ();
	MonoGCHandle gchandle = (MonoGCHandle)g_hash_table_lookup (rcw_hash, MONO_HANDLE_GETVAL (obj, iunknown));
	if (gchandle) {
		mono_gchandle_free_internal (gchandle);
		g_hash_table_remove (rcw_hash, MONO_HANDLE_GETVAL (obj, iunknown));
	}

	g_hash_table_foreach_remove (MONO_HANDLE_GETVAL (obj, itf_hash), cominterop_rcw_interface_finalizer, NULL);
	g_hash_table_destroy (MONO_HANDLE_GETVAL (obj, itf_hash));
	mono_IUnknown_Release (MONO_HANDLE_GETVAL (obj, iunknown));
	MONO_HANDLE_SETVAL (obj, iunknown, MonoIUnknown*, NULL);
	MONO_HANDLE_SETVAL (obj, itf_hash, GHashTable*, NULL);
	mono_cominterop_unlock ();
}

void
ves_icall_System_ComObject_ReleaseInterfaces (MonoComObjectHandle obj, MonoError *error)
{
	mono_System_ComObject_ReleaseInterfaces (obj);
}

gpointer
ves_icall_System_ComObject_GetInterfaceInternal (MonoComObjectHandle obj, MonoReflectionTypeHandle ref_type, MonoBoolean throw_exception, MonoError *error)
{
#ifndef DISABLE_COM
	MonoType * const type = MONO_HANDLE_GETVAL (ref_type, type);
	MonoClass * const klass = mono_class_from_mono_type_internal (type);
	if (!mono_class_init_checked (klass, error))
		return NULL;

	ERROR_DECL (error_ignored);
	gpointer const itf = cominterop_get_interface_checked (obj, klass, throw_exception ? error : error_ignored);
	mono_error_cleanup (error_ignored);
	return itf;
#else
	g_assert_not_reached ();
#endif
}

/**
 * cominterop_get_ccw_object:
 * @ccw_entry: a pointer to the CCWEntry
 * @verify: verify ccw_entry is in fact a ccw
 *
 * Returns: the corresponding object for the CCW
 */
static MonoGCHandle
cominterop_get_ccw_gchandle (MonoCCWInterface* ccw_entry, gboolean verify)
{
	/* no CCW's exist yet */
	if (!ccw_interface_hash)
		return 0;

	MonoCCW * const ccw = verify ? (MonoCCW *)g_hash_table_lookup (ccw_interface_hash, ccw_entry) : ccw_entry->ccw;
	g_assert (verify || ccw);
	return ccw ? ccw->gc_handle : 0;
}

static MonoObjectHandle
cominterop_get_ccw_handle (MonoCCWInterface* ccw_entry, gboolean verify)
{
	MonoGCHandle const gchandle = cominterop_get_ccw_gchandle (ccw_entry, verify);
	return gchandle ? mono_gchandle_get_target_handle (gchandle) : NULL_HANDLE;
}

static MonoObject*
cominterop_get_ccw_object (MonoCCWInterface* ccw_entry, gboolean verify)
{
	MonoGCHandle const gchandle = cominterop_get_ccw_gchandle (ccw_entry, verify);
	return gchandle ? mono_gchandle_get_target_internal (gchandle) : NULL;
}

static MonoDomain*
cominterop_get_domain_for_appdomain (MonoAppDomain *ad_raw)
{
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoAppDomain, ad);
	MonoDomain * result = MONO_HANDLE_GETVAL (ad, data);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static MonoObject*
cominterop_set_ccw_object_domain (MonoObject *object, MonoDomain **prev_domain)
{
	MonoDomain *current = mono_domain_get (), *obj_domain;

	if (mono_object_class (object) == mono_defaults.appdomain_class)
		obj_domain = cominterop_get_domain_for_appdomain ((MonoAppDomain *)object);
	else
		obj_domain = mono_object_domain (object);

	if (obj_domain != current) {
		*prev_domain = current;
		mono_domain_set_internal_with_options (obj_domain, FALSE);
	}
	else
		*prev_domain = NULL;

	return object;
}

static void
cominterop_restore_domain (MonoDomain *domain)
{
	if (!domain)
		return;

	mono_domain_set_internal_with_options (domain, FALSE);
}

static void
cominterop_setup_marshal_context (EmitMarshalContext *m, MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoImage *method_klass_image = m_class_get_image (method->klass);
	sig = mono_method_signature_internal (method);
	/* we copy the signature, so that we can modify it */
	/* FIXME: which to use? */
	csig = mono_metadata_signature_dup_full (method_klass_image, sig);
	/* csig = mono_metadata_signature_dup (sig); */

	/* STDCALL on windows, CDECL everywhere else to work with XPCOM and MainWin COM */
#ifdef HOST_WIN32
	csig->call_convention = MONO_CALL_STDCALL;
#else
	csig->call_convention = MONO_CALL_C;
#endif
	csig->hasthis = 0;
	csig->pinvoke = 1;

	m->image = method_klass_image;
	m->piinfo = NULL;
	m->retobj_var = 0;
	m->sig = sig;
	m->csig = csig;
}

static MonoMarshalSpec*
cominterop_get_ccw_default_mspec (const MonoType *param_type)
{
	MonoMarshalVariant elem_type;
	MonoMarshalNative native;
	MonoMarshalSpec *result;

	switch (param_type->type) {
	case MONO_TYPE_OBJECT:
		native = MONO_NATIVE_STRUCT;
		break;
	case MONO_TYPE_STRING:
		native = MONO_NATIVE_BSTR;
		break;
	case MONO_TYPE_CLASS:
		native = MONO_NATIVE_INTERFACE;
		break;
	case MONO_TYPE_BOOLEAN:
		native = MONO_NATIVE_VARIANTBOOL;
		break;
	case MONO_TYPE_SZARRAY:
		/* object[] -> SAFEARRAY(VARIANT) */
		native = MONO_NATIVE_SAFEARRAY;
		if (param_type->data.array->eklass == mono_defaults.object_class)
			elem_type = MONO_VARIANT_VARIANT;
		else
			return NULL;
		break;
	default:
		return NULL;
	}

	result = g_new0 (MonoMarshalSpec, 1);
	result->native = native;
	if (native == MONO_NATIVE_SAFEARRAY)
		result->data.safearray_data.elem_type = elem_type;

	return result;
}

static MonoClass*
cominterop_get_default_iface (MonoClass *klass)
{
	if (mono_class_is_interface (klass))
		return klass;

	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_assert_ok (error);

	if (!cinfo)
		return mono_class_get_idispatch_class ();

	MonoClassInterfaceAttribute *class_attr = (MonoClassInterfaceAttribute *)mono_custom_attrs_get_attr_checked (cinfo, mono_class_get_class_interface_attribute_class (), error);
	MonoClass *ret;

	if (class_attr)
	{
		if (class_attr->intType == 0) {
			ret = mono_defaults.object_class;
			for (guint16 i = 0; i < m_class_get_interface_count (klass); i++) {
				MonoClass *iface = m_class_get_interfaces (klass) [i];
				if (cominterop_com_visible (iface)) {
					ret = iface;
					break;
				}
			}
		}
		else if (class_attr->intType == 1)
			ret = mono_class_get_idispatch_class ();
		else
			ret = klass;
	} else
		ret = mono_class_get_idispatch_class ();

	if (!cinfo->cached)
		mono_custom_attrs_free (cinfo);
	return ret;
}

static gboolean
cominterop_class_method_is_visible (MonoMethod *method)
{
	guint16 flags = method->flags;

	if ((flags & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK) != METHOD_ATTRIBUTE_PUBLIC)
		return FALSE;

	if (flags & METHOD_ATTRIBUTE_STATIC)
		return FALSE;

	if (flags & METHOD_ATTRIBUTE_RT_SPECIAL_NAME)
		return FALSE;

	if (!mono_cominterop_method_com_visible (method))
		return FALSE;

	/* if the method is an override, ignore it and use the original definition */
	if ((flags & METHOD_ATTRIBUTE_VIRTUAL) && !(flags & METHOD_ATTRIBUTE_NEW_SLOT))
		return FALSE;

	return TRUE;
}

static gpointer
cominterop_get_ccw_method (MonoClass *iface, MonoMethod *method, MonoError *error)
{
	int param_index = 0;
	MonoMethodBuilder *mb;
	MonoMarshalSpec ** mspecs;
	MonoMethod *wrapper_method, *adjust_method;
	MonoMethodSignature* sig_adjusted;
	MonoMethodSignature* sig = mono_method_signature_internal (method);
	gboolean const preserve_sig = (method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG) != 0;
	EmitMarshalContext m;

	mb = mono_mb_new (iface, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);
	adjust_method = cominterop_get_managed_wrapper_adjusted (method);
	sig_adjusted = mono_method_signature_internal (adjust_method);

	mspecs = g_new0 (MonoMarshalSpec*, sig_adjusted->param_count + 1);
	mono_method_get_marshal_info (method, mspecs);

	/* move managed args up one */
	for (param_index = sig->param_count; param_index >= 1; param_index--) {
		int mspec_index = param_index+1;
		mspecs [mspec_index] = mspecs [param_index];

		if (mspecs[mspec_index] == NULL) {
			mspecs[mspec_index] = cominterop_get_ccw_default_mspec (sig_adjusted->params[param_index]);
		} else {
			/* increase SizeParamIndex since we've added a param */
			if (sig_adjusted->params[param_index]->type == MONO_TYPE_ARRAY ||
			    sig_adjusted->params[param_index]->type == MONO_TYPE_SZARRAY)
				if (mspecs[mspec_index]->data.array_data.param_num != -1)
					mspecs[mspec_index]->data.array_data.param_num++;
		}
	}

	/* first arg is IntPtr for interface */
	mspecs [1] = NULL;

	/* move return spec to last param */
	if (!preserve_sig && !MONO_TYPE_IS_VOID (sig->ret)) {
		if (mspecs [0] == NULL)
			mspecs[0] = cominterop_get_ccw_default_mspec (sig_adjusted->params[sig_adjusted->param_count-1]);

		mspecs [sig_adjusted->param_count] = mspecs [0];
		mspecs [0] = NULL;
	}

#ifndef DISABLE_JIT
	/* skip visiblity since we call internal methods */
	mb->skip_visibility = TRUE;
#endif

	cominterop_setup_marshal_context (&m, adjust_method);
	m.mb = mb;
	m.runtime_marshalling_enabled = TRUE;
	mono_marshal_emit_managed_wrapper (mb, sig_adjusted, mspecs, &m, adjust_method, 0, error);

	gpointer ret = NULL;
	if (is_ok (error)) {
		mono_cominterop_lock ();
		wrapper_method = mono_mb_create_method (mb, m.csig, m.csig->param_count + 16);
		mono_cominterop_unlock ();

		ret = mono_compile_method_checked (wrapper_method, error);
	}

	mono_mb_free (mb);
	for (param_index = sig_adjusted->param_count; param_index >= 0; param_index--)
		if (mspecs [param_index])
			mono_metadata_free_marshal_spec (mspecs [param_index]);
	g_free (mspecs);

	return ret;
}

/**
 * cominterop_get_ccw_checked:
 * @object: a pointer to the object
 * @itf: interface type needed
 * @error: set on error
 *
 * Returns: a value indicating if the object is a
 * Runtime Callable Wrapper (RCW) for a COM object.
 * On failure returns NULL and sets @error.
 */
static gpointer
cominterop_get_ccw_checked (MonoObjectHandle object, MonoClass* itf, MonoError *error)
{
	int i, j;
	MonoCCW *ccw = NULL;
	MonoCCWInterface* ccw_entry = NULL;
	gpointer *vtable = NULL;
	MonoClass* iface = NULL;
	int start_slot = 3;
	int method_count = 0;
	GList *ccw_list, *ccw_list_item;
	MonoCustomAttrInfo *cinfo = NULL;

	if (MONO_HANDLE_IS_NULL (object))
		return NULL;

	MonoClass* klass = mono_handle_class (object);

	mono_cominterop_lock ();
	if (!ccw_hash)
		ccw_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	if (!ccw_interface_hash)
		ccw_interface_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	ccw_list = (GList *)g_hash_table_lookup (ccw_hash, GINT_TO_POINTER (mono_handle_hash (object)));
	mono_cominterop_unlock ();

	ccw_list_item = ccw_list;
	while (ccw_list_item) {
		MonoCCW* ccw_iter = (MonoCCW *)ccw_list_item->data;
		if (mono_gchandle_target_equal (ccw_iter->gc_handle, object)) {
			ccw = ccw_iter;
			break;
		}
		ccw_list_item = g_list_next(ccw_list_item);
	}

	if (!ccw) {
		ccw = g_new0 (MonoCCW, 1);
#ifdef HOST_WIN32
		ccw->free_marshaler = 0;
#endif
		ccw->vtable_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
		ccw->ref_count = 0;
		/* just alloc a weak handle until we are addref'd*/
		ccw->gc_handle = mono_gchandle_new_weakref_from_handle (object);

		if (!ccw_list) {
			ccw_list = g_list_alloc ();
			ccw_list->data = ccw;
		}
		else
			ccw_list = g_list_append (ccw_list, ccw);
		mono_cominterop_lock ();
		g_hash_table_insert (ccw_hash, GINT_TO_POINTER (mono_handle_hash (object)), ccw_list);
		mono_cominterop_unlock ();
		/* register for finalization to clean up ccw */
		mono_object_register_finalizer_handle (object);
	}

	cinfo = mono_custom_attrs_from_class_checked (itf, error);
	mono_error_assert_ok (error);
	if (cinfo) {
		MONO_STATIC_POINTER_INIT (MonoClass, coclass_attribute)

			coclass_attribute = mono_class_load_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "CoClassAttribute");

		MONO_STATIC_POINTER_INIT_END (MonoClass, coclass_attribute)

		if (mono_custom_attrs_has_attr (cinfo, coclass_attribute)) {
			g_assert(m_class_get_interface_count (itf) && m_class_get_interfaces (itf)[0]);
			itf = m_class_get_interfaces (itf)[0];
		}
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	iface = cominterop_get_default_iface(itf);
	if (iface == mono_class_get_iunknown_class ()) {
		start_slot = 3;
	}
	else if (iface == mono_class_get_idispatch_class ()) {
		start_slot = 7;
	}
	else if (mono_class_is_interface (iface)) {
		method_count += mono_class_get_method_count (iface);
		start_slot = cominterop_get_com_slot_begin (iface);
	}
	else {
		/* auto-dual object */
		start_slot = 7;

		MonoClass *klass_iter;
		for (klass_iter = iface; klass_iter; klass_iter = m_class_get_parent (klass_iter)) {
			int mcount = mono_class_get_method_count (klass_iter);
			if (mcount && !m_class_get_methods (klass_iter))
				mono_class_setup_methods (klass_iter);

			for (i = 0; i < mcount; ++i) {
				MonoMethod *method = m_class_get_methods (klass_iter) [i];
				if (cominterop_class_method_is_visible (method))
					++method_count;
			}

			/* FIXME: accessors for public fields */
		}
	}

	ccw_entry = (MonoCCWInterface *)g_hash_table_lookup (ccw->vtable_hash, itf);

	if (!ccw_entry) {
		int vtable_index = method_count-1+start_slot;
		vtable = (void **)mono_image_alloc0 (m_class_get_image (klass), sizeof (gpointer)*(method_count+start_slot));
		vtable [0] = (gpointer)cominterop_ccw_queryinterface;
		vtable [1] = (gpointer)cominterop_ccw_addref;
		vtable [2] = (gpointer)cominterop_ccw_release;
		if (start_slot == 7) {
			vtable [3] = (gpointer)cominterop_ccw_get_type_info_count;
			vtable [4] = (gpointer)cominterop_ccw_get_type_info;
			vtable [5] = (gpointer)cominterop_ccw_get_ids_of_names;
			vtable [6] = (gpointer)cominterop_ccw_invoke;
		}

		if (mono_class_is_interface (iface)) {
			if (method_count && !m_class_get_methods (iface))
				mono_class_setup_methods (iface);

			for (i = method_count - 1; i >= 0; i--) {
				vtable [vtable_index--] = cominterop_get_ccw_method (iface, m_class_get_methods (iface) [i], error);
				return_val_if_nok (error, NULL);
			}
		}
		else {
			/* Auto-dual object. The methods on an auto-dual interface are
			 * exposed starting from the innermost parent (i.e. Object) and
			 * proceeding outwards. The methods within each interfaces are
			 * exposed in the following order:
			 *
			 * 1. Virtual methods
			 * 2. Interface methods
			 * 3. Nonvirtual methods
			 * 4. Fields (get, then put)
			 *
			 * Interface methods are exposed in the order that the interface
			 * was declared. Child interface methods are exposed before parents.
			 *
			 * Because we need to expose superclass methods starting from the
			 * innermost parent, we expose methods in reverse order, so that
			 * we can just iterate using m_class_get_parent (). */

			mono_class_setup_vtable (iface);

			MonoClass *klass_iter;
			for (klass_iter = iface; klass_iter; klass_iter = m_class_get_parent (klass_iter)) {
				mono_class_setup_vtable (klass_iter);

				/* 3. Nonvirtual methods */
				for (i = mono_class_get_method_count (klass_iter) - 1; i >= 0; i--) {
					MonoMethod *method = m_class_get_methods (klass_iter) [i];
					if (cominterop_class_method_is_visible (method) && !(method->flags & METHOD_ATTRIBUTE_VIRTUAL)) {
						vtable [vtable_index--] = cominterop_get_ccw_method (iface, method, error);
						return_val_if_nok (error, NULL);
					}
				}

				/* 2. Interface methods */
				GPtrArray *ifaces = mono_class_get_implemented_interfaces (klass_iter, error);
				mono_error_assert_ok (error);
				if (ifaces) {
					for (i = ifaces->len - 1; i >= 0; i--) {
						MonoClass *ic = (MonoClass *)g_ptr_array_index (ifaces, i);
						int offset = mono_class_interface_offset (iface, ic);
						g_assert (offset >= 0);
						for (j = mono_class_get_method_count (ic) - 1; j >= 0; j--) {
							MonoMethod *method = m_class_get_methods (ic) [j];
							vtable [vtable_index--] = cominterop_get_ccw_method (iface, m_class_get_vtable (iface) [offset + method->slot], error);
							if (!is_ok (error)) {
								g_ptr_array_free (ifaces, TRUE);
								return NULL;
							}
						}
					}
					g_ptr_array_free (ifaces, TRUE);
				}

				/* 1. Virtual methods */
				for (i = mono_class_get_method_count (klass_iter) - 1; i >= 0; i--) {
					MonoMethod *method = m_class_get_methods (klass_iter) [i];
					if (cominterop_class_method_is_visible (method) && (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
						&& !cominterop_get_method_interface (method)) {
						vtable [vtable_index--] = cominterop_get_ccw_method (iface, m_class_get_vtable (iface) [method->slot], error);
						return_val_if_nok (error, NULL);
					}
				}
			}
		}

		ccw_entry = g_new0 (MonoCCWInterface, 1);
		ccw_entry->ccw = ccw;
		ccw_entry->vtable = vtable;
		g_hash_table_insert (ccw->vtable_hash, itf, ccw_entry);
		g_hash_table_insert (ccw_interface_hash, ccw_entry, ccw);
	}

	return ccw_entry;
}

/**
 * cominterop_get_ccw:
 * @object: a pointer to the object
 * @itf: interface type needed
 *
 * Returns: a value indicating if the object is a
 * Runtime Callable Wrapper (RCW) for a COM object
 */
static gpointer
cominterop_get_ccw (MonoObject* object_raw, MonoClass* itf)
{
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MONO_HANDLE_DCL (MonoObject, object);
	gpointer const ccw_entry = cominterop_get_ccw_checked (object, itf, error);
	mono_error_set_pending_exception (error);
	HANDLE_FUNCTION_RETURN_VAL (ccw_entry);
}

static gboolean
mono_marshal_free_ccw_entry (gpointer key, gpointer value, gpointer user_data)
{
	g_hash_table_remove (ccw_interface_hash, value);
	g_assert (value);
	g_free (value);
	return TRUE;
}

/**
 * mono_marshal_free_ccw:
 * \param object the mono object
 * \returns whether the object had a CCW
 */
static gboolean
mono_marshal_free_ccw_handle (MonoObjectHandle object)
{
	/* no ccw's were created */
	if (!ccw_hash || g_hash_table_size (ccw_hash) == 0)
		return FALSE;

	mono_cominterop_lock ();
	GList *ccw_list = (GList *)g_hash_table_lookup (ccw_hash, GINT_TO_POINTER (mono_handle_hash (object)));
	mono_cominterop_unlock ();

	if (!ccw_list)
		return FALSE;

	/* need to cache orig list address to remove from hash_table if empty */
	GList * const ccw_list_orig = ccw_list;

	for (GList* ccw_list_item = ccw_list; ccw_list_item; ) {
		MonoCCW* ccw_iter = (MonoCCW *)ccw_list_item->data;
		gboolean is_null = FALSE;
		gboolean is_equal = FALSE;
		mono_gchandle_target_is_null_or_equal (ccw_iter->gc_handle, object, &is_null, &is_equal);

		/* Looks like the GC NULLs the weakref handle target before running the
		 * finalizer. So if we get a NULL target, destroy the CCW as well.
		 * Unless looking up the object from the CCW shows it not the right object.
		*/
		gboolean destroy_ccw = is_null || is_equal;
		if (is_null) {
			MonoCCWInterface* ccw_entry = (MonoCCWInterface *)g_hash_table_lookup (ccw_iter->vtable_hash, mono_class_get_iunknown_class ());
			MonoGCHandle gchandle = NULL;
			if (!(ccw_entry && (gchandle = cominterop_get_ccw_gchandle (ccw_entry, FALSE)) && mono_gchandle_target_equal (gchandle, object)))
				destroy_ccw = FALSE;
		}
		if (destroy_ccw) {
			/* remove all interfaces */
			g_hash_table_foreach_remove (ccw_iter->vtable_hash, mono_marshal_free_ccw_entry, NULL);
			g_hash_table_destroy (ccw_iter->vtable_hash);

			/* get next before we delete */
			ccw_list_item = g_list_next (ccw_list_item);

			/* remove ccw from list */
			ccw_list = g_list_remove (ccw_list, ccw_iter);
#ifdef HOST_WIN32
			mono_IUnknown_Release (ccw_iter->free_marshaler);
#endif
			g_free (ccw_iter);
		}
		else
			ccw_list_item = g_list_next (ccw_list_item);
	}

	/* if list is empty remove original address from hash */
	if (g_list_length (ccw_list) == 0)
		g_hash_table_remove (ccw_hash, GINT_TO_POINTER (mono_handle_hash (object)));
	else if (ccw_list != ccw_list_orig)
		g_hash_table_insert (ccw_hash, GINT_TO_POINTER (mono_handle_hash (object)), ccw_list);

	return TRUE;
}

gboolean
mono_marshal_free_ccw (MonoObject* object_raw)
{
	/* no ccw's were created */
	if (!ccw_hash || g_hash_table_size (ccw_hash) == 0)
		return FALSE;

	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoObject, object);
	gboolean const result = mono_marshal_free_ccw_handle (object);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

/**
 * cominterop_get_managed_wrapper_adjusted:
 * @method: managed COM Interop method
 *
 * Returns: the generated method to call with signature matching
 * the unmanaged COM Method signature
 */
static MonoMethod *
cominterop_get_managed_wrapper_adjusted (MonoMethod *method)
{
	MonoMethod *res = NULL;
	MonoMethodBuilder *mb;
	MonoMarshalSpec **mspecs;
	MonoMethodSignature *sig, *sig_native;
	MonoExceptionClause *main_clause = NULL;
	int hr = 0, retval = 0;
	int pos_leave, domain_var;
	int i;
	gboolean const preserve_sig = (method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG) != 0;
	MonoType *int_type = mono_get_int_type ();

	MONO_STATIC_POINTER_INIT (MonoMethod, get_hr_for_exception)

		ERROR_DECL (error);
		get_hr_for_exception = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetHRForException", -1, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_hr_for_exception)

	sig = mono_method_signature_internal (method);

	/* create unmanaged wrapper */
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_COMINTEROP);

	sig_native = cominterop_method_signature (method);

	mspecs = g_new0 (MonoMarshalSpec*, sig_native->param_count+1);

	mono_method_get_marshal_info (method, mspecs);

	/* move managed args up one */
	for (i = sig->param_count; i >= 1; i--)
		mspecs [i+1] = mspecs [i];

	/* first arg is IntPtr for interface */
	mspecs [1] = NULL;

	/* move return spec to last param */
	if (!preserve_sig && !MONO_TYPE_IS_VOID (sig->ret))
		mspecs [sig_native->param_count] = mspecs [0];

	mspecs [0] = NULL;

#ifndef DISABLE_JIT
	if (!preserve_sig) {
		if (!MONO_TYPE_IS_VOID (sig->ret))
			retval = mono_mb_add_local (mb, sig->ret);
		hr = mono_mb_add_local (mb, mono_get_int32_type ());
	}
	else if (!MONO_TYPE_IS_VOID (sig->ret))
		hr = mono_mb_add_local (mb, sig->ret);

	/* try */
	main_clause = g_new0 (MonoExceptionClause, 1);
	main_clause->try_offset = mono_mb_get_label (mb);

	domain_var = mono_mb_add_local (mb, int_type);

	/* the CCW -> object conversion */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, FALSE);
	mono_mb_emit_icall (mb, cominterop_get_ccw_object);

	/* Object is left on stack */
	mono_mb_emit_ldloc_addr (mb, domain_var);
	mono_mb_emit_icall (mb, cominterop_set_ccw_object_domain);

	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i+1);

	mono_mb_emit_managed_call (mb, method, NULL);

	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		if (!preserve_sig) {
			mono_mb_emit_stloc (mb, retval);
			mono_mb_emit_ldarg (mb, sig_native->param_count - 1);
			const int pos_null = mono_mb_emit_branch (mb, CEE_BRFALSE);

			mono_mb_emit_ldarg (mb, sig_native->param_count - 1);
			mono_mb_emit_ldloc (mb, retval);

			MonoClass *rclass = mono_class_from_mono_type_internal (sig->ret);
			if (m_class_is_valuetype (rclass)) {
				mono_mb_emit_op (mb, CEE_STOBJ, rclass);
			} else {
				mono_mb_emit_byte (mb, mono_type_to_stind (sig->ret));
			}

			mono_mb_patch_branch (mb, pos_null);
		} else
			mono_mb_emit_stloc (mb, hr);
	}

	pos_leave = mono_mb_emit_branch (mb, CEE_LEAVE);

	/* Main exception catch */
	main_clause->flags = MONO_EXCEPTION_CLAUSE_NONE;
	main_clause->try_len = mono_mb_get_pos (mb) - main_clause->try_offset;
	main_clause->data.catch_class = mono_defaults.object_class;

	/* handler code */
	main_clause->handler_offset = mono_mb_get_label (mb);

	if (!preserve_sig || (sig->ret && !m_type_is_byref (sig->ret) && (sig->ret->type == MONO_TYPE_U4 || sig->ret->type == MONO_TYPE_I4))) {
		mono_mb_emit_managed_call (mb, get_hr_for_exception, NULL);
		mono_mb_emit_stloc (mb, hr);
	}
	else {
		mono_mb_emit_byte (mb, CEE_POP);
	}

	mono_mb_emit_branch (mb, CEE_LEAVE);
	main_clause->handler_len = mono_mb_get_pos (mb) - main_clause->handler_offset;
	/* end catch */

	mono_mb_set_clauses (mb, 1, main_clause);

	mono_mb_patch_branch (mb, pos_leave);

	if (!preserve_sig || !MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldloc (mb, hr);

	mono_mb_emit_ldloc (mb, domain_var);
	mono_mb_emit_icall (mb, cominterop_restore_domain);

	mono_mb_emit_byte (mb, CEE_RET);
#endif /* DISABLE_JIT */

	mono_cominterop_lock ();
	res = mono_mb_create_method (mb, sig_native, sig_native->param_count + 16);
	mono_cominterop_unlock ();

	mono_mb_free (mb);

	for (i = sig_native->param_count; i >= 0; i--)
		mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

static gboolean
cominterop_class_guid_equal (const guint8* guid, MonoClass* klass)
{
	guint8 klass_guid [16];
	if (cominterop_class_guid (klass, klass_guid))
		return !memcmp (guid, klass_guid, sizeof (klass_guid));
	return FALSE;
}

static int STDCALL
cominterop_ccw_addref_impl (MonoCCWInterface* ccwe);

static int STDCALL
cominterop_ccw_addref (MonoCCWInterface* ccwe)
{
	int result;
	MONO_CCW_CALL_ENTER;
	result = cominterop_ccw_addref_impl (ccwe);
	MONO_CCW_CALL_EXIT;
	return result;
}

static int STDCALL
cominterop_ccw_addref_impl (MonoCCWInterface* ccwe)
{
	MONO_REQ_GC_UNSAFE_MODE;
	MonoCCW* ccw = ccwe->ccw;
	g_assert (ccw);
	g_assert (ccw->gc_handle);
	gint32 const ref_count = mono_atomic_inc_i32 ((gint32*)&ccw->ref_count);
	if (ref_count == 1) {
		MonoGCHandle oldhandle = ccw->gc_handle;
		g_assert (oldhandle);
		/* since we now have a ref count, alloc a strong handle*/
		ccw->gc_handle = mono_gchandle_from_handle (mono_gchandle_get_target_handle (oldhandle), FALSE);
		mono_gchandle_free_internal (oldhandle);
	}
	return ref_count;
}

static int STDCALL
cominterop_ccw_release_impl (MonoCCWInterface* ccwe);

static int STDCALL
cominterop_ccw_release (MonoCCWInterface* ccwe)
{
	int result;
	MONO_CCW_CALL_ENTER;
	result = cominterop_ccw_release_impl (ccwe);
	MONO_CCW_CALL_EXIT;
	return result;
}

static int STDCALL
cominterop_ccw_release_impl (MonoCCWInterface* ccwe)
{
	MONO_REQ_GC_UNSAFE_MODE;
	MonoCCW* ccw = ccwe->ccw;
	g_assert (ccw);
	g_assert (ccw->ref_count > 0);
	gint32 const ref_count = mono_atomic_dec_i32 ((gint32*)&ccw->ref_count);
	if (ref_count == 0) {
		/* allow gc of object */
		MonoGCHandle oldhandle = ccw->gc_handle;
		g_assert (oldhandle);
		ccw->gc_handle = mono_gchandle_new_weakref_from_handle (mono_gchandle_get_target_handle (oldhandle));
		mono_gchandle_free_internal (oldhandle);
	}
	return ref_count;
}

#ifdef HOST_WIN32
static const IID MONO_IID_IMarshal = {0x3, 0x0, 0x0, {0xC0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x46}};

/* All ccw objects are free threaded */
static int
cominterop_ccw_getfreethreadedmarshaler (MonoCCW* ccw, MonoObjectHandle object, gpointer* ppv, MonoError *error)
{
	if (!ccw->free_marshaler) {
		gpointer const tunk = cominterop_get_ccw_checked (object, mono_class_get_iunknown_class (), error);
		return_val_if_nok (error, MONO_E_NOINTERFACE);
		int const ret = CoCreateFreeThreadedMarshaler ((LPUNKNOWN)tunk, (LPUNKNOWN*)&ccw->free_marshaler);
	}

	return ccw->free_marshaler ? mono_IUnknown_QueryInterface (ccw->free_marshaler, &MONO_IID_IMarshal, ppv)
				   : MONO_E_NOINTERFACE;
}
#endif

static int STDCALL
cominterop_ccw_queryinterface_impl (MonoCCWInterface* ccwe, const guint8* riid, gpointer* ppv);

static int STDCALL
cominterop_ccw_queryinterface (MonoCCWInterface* ccwe, const guint8* riid, gpointer* ppv)
{
	int result;
	MONO_CCW_CALL_ENTER;
	result = cominterop_ccw_queryinterface_impl (ccwe, riid, ppv);
	MONO_CCW_CALL_EXIT;
	return result;
}

static int STDCALL
cominterop_ccw_queryinterface_impl (MonoCCWInterface* ccwe, const guint8* riid, gpointer* ppv)
{
	MONO_REQ_GC_UNSAFE_MODE;
	ERROR_DECL (error);
	GPtrArray *ifaces;
	MonoClass *itf = NULL;
	int i;
	MonoCCW* ccw = ccwe->ccw;
	MonoClass* klass_iter = NULL;
	MonoObjectHandle object = mono_gchandle_get_target_handle (ccw->gc_handle);

	g_assert (!MONO_HANDLE_IS_NULL (object));
	MonoClass* const klass = mono_handle_class (object);

	if (ppv)
		*ppv = NULL;

	if (!mono_domain_get ())
		mono_thread_attach_external_native_thread (mono_get_root_domain (), FALSE);

	/* handle IUnknown special */
	if (cominterop_class_guid_equal (riid, mono_class_get_iunknown_class ())) {
		*ppv = cominterop_get_ccw_checked (object, mono_class_get_iunknown_class (), error);
		mono_error_assert_ok (error);
		/* remember to addref on QI */
		cominterop_ccw_addref_impl ((MonoCCWInterface *)*ppv);
		return MONO_S_OK;
	}

	/* handle IDispatch special */
	if (cominterop_class_guid_equal (riid, mono_class_get_idispatch_class ())) {
		if (!cominterop_can_support_dispatch (klass))
			return MONO_E_NOINTERFACE;

		*ppv = cominterop_get_ccw_checked (object, mono_class_get_idispatch_class (), error);
		mono_error_assert_ok (error);
		/* remember to addref on QI */
		cominterop_ccw_addref_impl ((MonoCCWInterface *)*ppv);
		return MONO_S_OK;
	}

#ifdef HOST_WIN32
	/* handle IMarshal special */
	if (0 == memcmp (riid, &MONO_IID_IMarshal, sizeof (IID))) {
		int const res = cominterop_ccw_getfreethreadedmarshaler (ccw, object, ppv, error);
		mono_error_assert_ok (error);
		return res;
	}
#endif
	klass_iter = klass;
	while (klass_iter && klass_iter != mono_defaults.object_class) {
		ifaces = mono_class_get_implemented_interfaces (klass_iter, error);
		mono_error_assert_ok (error);
		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				MonoClass *ic = NULL;
				ic = (MonoClass *)g_ptr_array_index (ifaces, i);
				if (cominterop_class_guid_equal (riid, ic)) {
					itf = ic;
					break;
				}
			}
			g_ptr_array_free (ifaces, TRUE);
		}

		if (itf)
			break;

		klass_iter = m_class_get_parent (klass_iter);
	}
	if (itf) {
		*ppv = cominterop_get_ccw_checked (object, itf, error);
		if (!is_ok (error)) {
			mono_error_cleanup (error); /* FIXME don't swallow the error */
			return MONO_E_NOINTERFACE;
		}
		/* remember to addref on QI */
		cominterop_ccw_addref_impl ((MonoCCWInterface *)*ppv);
		return MONO_S_OK;
	}

	return MONO_E_NOINTERFACE;
}

static int STDCALL
cominterop_ccw_get_type_info_count (MonoCCWInterface* ccwe, guint32 *pctinfo)
{
	if(!pctinfo)
		return MONO_E_INVALIDARG;

	*pctinfo = 1;

	return MONO_S_OK;
}

static int STDCALL
cominterop_ccw_get_type_info (MonoCCWInterface* ccwe, guint32 iTInfo, guint32 lcid, gpointer *ppTInfo)
{
	return MONO_E_NOTIMPL;
}

static int STDCALL
cominterop_ccw_get_ids_of_names_impl (MonoCCWInterface* ccwe, gpointer riid,
				      gunichar2** rgszNames, guint32 cNames,
				      guint32 lcid, gint32 *rgDispId);


static int STDCALL
cominterop_ccw_get_ids_of_names (MonoCCWInterface* ccwe, gpointer riid,
											 gunichar2** rgszNames, guint32 cNames,
											 guint32 lcid, gint32 *rgDispId)
{
	int result;
	MONO_CCW_CALL_ENTER;
	result = cominterop_ccw_get_ids_of_names_impl (ccwe, riid, rgszNames, cNames, lcid, rgDispId);
	MONO_CCW_CALL_EXIT;
	return result;
}

static int STDCALL
cominterop_ccw_get_ids_of_names_impl (MonoCCWInterface* ccwe, gpointer riid,
				      gunichar2** rgszNames, guint32 cNames,
				      guint32 lcid, gint32 *rgDispId)
{
	MONO_REQ_GC_UNSAFE_MODE;
	ERROR_DECL (error);
	MonoCustomAttrInfo *cinfo = NULL;
	int i,ret = MONO_S_OK;
	MonoMethod* method;
	gchar* methodname;
	MonoClass *klass = NULL;
	MonoCCW* ccw = ccwe->ccw;
	MonoObject* object = mono_gchandle_get_target_internal (ccw->gc_handle);

	/* Handle DispIdAttribute */

	MONO_STATIC_POINTER_INIT (MonoClass, ComDispIdAttribute)

		ComDispIdAttribute = mono_class_load_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "DispIdAttribute");

	MONO_STATIC_POINTER_INIT_END (MonoClass, ComDispIdAttribute)

	g_assert (object);
	klass = mono_object_class (object);

	if (!mono_domain_get ())
		mono_thread_attach_external_native_thread (mono_get_root_domain (), FALSE);

	for (i=0; i < cNames; i++) {
		methodname = mono_unicode_to_external (rgszNames[i]);

		method = mono_class_get_method_from_name_checked(klass, methodname, -1, 0, error);
		if (method && is_ok (error)) {
			cinfo = mono_custom_attrs_from_method_checked (method, error);
			mono_error_assert_ok (error); /* FIXME what's reasonable to do here */
			if (cinfo) {
				MonoObject *result = mono_custom_attrs_get_attr_checked (cinfo, ComDispIdAttribute, error);
				mono_error_assert_ok (error); /*FIXME proper error handling*/;

				if (result)
					rgDispId[i] = *(gint32*)mono_object_unbox_internal (result);
				else
					rgDispId[i] = (gint32)method->token;

				if (!cinfo->cached)
					mono_custom_attrs_free (cinfo);
			}
			else
				rgDispId[i] = (gint32)method->token;
		} else {
			mono_error_cleanup (error);
			error_init (error); /* reuse for next iteration */
			rgDispId[i] = MONO_E_DISPID_UNKNOWN;
			ret = MONO_E_DISP_E_UNKNOWNNAME;
		}
	}

	return ret;
}

static int STDCALL
cominterop_ccw_invoke (MonoCCWInterface* ccwe, guint32 dispIdMember,
								   gpointer riid, guint32 lcid,
								   guint16 wFlags, gpointer pDispParams,
								   gpointer pVarResult, gpointer pExcepInfo,
								   guint32 *puArgErr)
{
	return MONO_E_NOTIMPL;
}

#ifndef HOST_WIN32

typedef mono_bstr (STDCALL *SysAllocStringLenFunc)(const gunichar* str, guint32 len);
typedef guint32 (STDCALL *SysStringLenFunc)(mono_bstr_const bstr);
typedef void (STDCALL *SysFreeStringFunc)(mono_bstr_const str);

static SysAllocStringLenFunc sys_alloc_string_len_ms = NULL;
static SysStringLenFunc sys_string_len_ms = NULL;
static SysFreeStringFunc sys_free_string_ms = NULL;

typedef struct tagSAFEARRAYBOUND {
	ULONG cElements;
	LONG lLbound;
}SAFEARRAYBOUND,*LPSAFEARRAYBOUND;
#define VT_VARIANT 12

typedef guint32 (STDCALL *SafeArrayGetDimFunc)(gpointer psa);
typedef int (STDCALL *SafeArrayGetLBoundFunc)(gpointer psa, guint32 nDim, glong* plLbound);
typedef int (STDCALL *SafeArrayGetUBoundFunc)(gpointer psa, guint32 nDim, glong* plUbound);
typedef int (STDCALL *SafeArrayPtrOfIndexFunc)(gpointer psa, glong* rgIndices, gpointer* ppvData);
typedef int (STDCALL *SafeArrayDestroyFunc)(gpointer psa);
typedef int (STDCALL *SafeArrayPutElementFunc)(gpointer psa, glong* rgIndices, gpointer* ppvData);
typedef gpointer (STDCALL *SafeArrayCreateFunc)(int vt, guint32 cDims, SAFEARRAYBOUND* rgsabound);

static SafeArrayGetDimFunc safe_array_get_dim_ms = NULL;
static SafeArrayGetLBoundFunc safe_array_get_lbound_ms = NULL;
static SafeArrayGetUBoundFunc safe_array_get_ubound_ms = NULL;
static SafeArrayPtrOfIndexFunc safe_array_ptr_of_index_ms = NULL;
static SafeArrayDestroyFunc safe_array_destroy_ms = NULL;
static SafeArrayPutElementFunc safe_array_put_element_ms = NULL;
static SafeArrayCreateFunc safe_array_create_ms = NULL;

static gboolean
init_com_provider_ms (void)
{
	ERROR_DECL (error);

	static gboolean initialized = FALSE;
	MonoDl *module = NULL;
	const char* scope = "liboleaut32.so";

	if (initialized) {
		// Barrier here prevents reads of sys_alloc_string_len_ms etc.
		// from being reordered before initialized.
		mono_memory_barrier ();
		return TRUE;
	}

	module = mono_dl_open (scope, MONO_DL_LAZY, error);
	if (!module) {
		g_warning ("Error loading COM support library '%s': %s", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	sys_alloc_string_len_ms = (SysAllocStringLenFunc)mono_dl_symbol (module, "SysAllocStringLen", error);
	if (!sys_alloc_string_len_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SysAllocStringLen", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	sys_string_len_ms = (SysStringLenFunc)mono_dl_symbol (module, "SysStringLen", error);
	if (!sys_string_len_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SysStringLen", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	sys_free_string_ms = (SysFreeStringFunc)mono_dl_symbol (module, "SysFreeString", error);
	if (!sys_free_string_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SysFreeString", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_get_dim_ms = (SafeArrayGetDimFunc)mono_dl_symbol (module, "SafeArrayGetDim", error);
	if (!safe_array_get_dim_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayGetDim", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_get_lbound_ms = (SafeArrayGetLBoundFunc)mono_dl_symbol (module, "SafeArrayGetLBound", error);
	if (!safe_array_get_lbound_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayGetLBound", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_get_ubound_ms = (SafeArrayGetUBoundFunc)mono_dl_symbol (module, "SafeArrayGetUBound", error);
	if (!safe_array_get_ubound_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayGetUBound", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_ptr_of_index_ms = (SafeArrayPtrOfIndexFunc)mono_dl_symbol (module, "SafeArrayPtrOfIndex", error);
	if (!safe_array_ptr_of_index_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayPtrOfIndex", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_destroy_ms = (SafeArrayDestroyFunc)mono_dl_symbol (module, "SafeArrayDestroy", error);
	if (!safe_array_destroy_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayDestroy", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_put_element_ms = (SafeArrayPutElementFunc)mono_dl_symbol (module, "SafeArrayPutElement", error);
	if (!safe_array_put_element_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayPutElement", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	safe_array_create_ms = (SafeArrayCreateFunc)mono_dl_symbol (module, "SafeArrayCreate", error);
	if (!safe_array_create_ms) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayCreate", scope, mono_error_get_message_without_fields (error));
		mono_error_cleanup (error);
		g_assert_not_reached ();
		return FALSE;
	}

	mono_memory_barrier ();
	initialized = TRUE;
	return TRUE;
}

#endif // WIN32
#endif // DISABLE_COM

// This function is used regardless of the BSTR type, so cast the return value
// Inputted string length, in bytes, should include the null terminator
// Returns the start of the string itself
static gpointer
mono_bstr_alloc (size_t str_byte_len)
{
	// Allocate string length plus pointer-size integer to store the length, aligned to 16 bytes
	size_t alloc_size = str_byte_len + SIZEOF_VOID_P;
	alloc_size += (16 - 1);
	alloc_size &= ~(16 - 1);
	gpointer ret = g_malloc0 (alloc_size);
	return ret ? (char *)ret + SIZEOF_VOID_P : NULL;
}

static void
mono_bstr_set_length (gunichar2 *bstr, int slen)
{
	*((guint32 *)bstr - 1) = slen * sizeof (gunichar2);
}

static mono_bstr
default_ptr_to_bstr (const gunichar2* ptr, int slen)
{
	// In Mono, historically BSTR was allocated with a guaranteed size prefix of 4 bytes regardless of platform.
	// Presumably this is due to the BStr documentation page, which indicates that behavior and then directs you to call
	// SysAllocString on Windows to handle the allocation for you. Unfortunately, this is not actually how it works:
	// The allocation pre-string is pointer-sized, and then only 4 bytes are used for the length regardless. Additionally,
	// the total length is also aligned to a 16-byte boundary. This preserves the old behavior on legacy and fixes it for
	// netcore moving forward.
	mono_bstr const s = (mono_bstr)mono_bstr_alloc ((slen + 1) * sizeof (gunichar2));
	if (s == NULL)
		return NULL;

	mono_bstr_set_length (s, slen);
	if (ptr)
		memcpy (s, ptr, slen * sizeof (gunichar2));
	s [slen] = 0;
	return s;
}

/* PTR can be NULL */
mono_bstr
mono_ptr_to_bstr (const gunichar2* ptr, int slen)
{
#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_BSTR
	return SysAllocStringLen (ptr, slen);
#else
	return default_ptr_to_bstr (ptr, slen);
#endif
#else
#ifndef DISABLE_COM
	if (com_provider == MONO_COM_DEFAULT) {
#endif
		return default_ptr_to_bstr (ptr, slen);
#ifndef DISABLE_COM
	}
	else if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		guint32 const len = slen;
		gunichar* const str = ptr ? g_utf16_to_ucs4 (ptr, len, NULL, NULL, NULL) : NULL;
		mono_bstr const ret = sys_alloc_string_len_ms (str, len);
		g_free (str);
		return ret;
	}
	else {
		g_assert_not_reached();
	}
#endif
#endif
}

char *
mono_ptr_to_ansibstr (const char *ptr, size_t slen)
{
	// FIXME: should this behave differently without DISABLE_COM?
	char *s = (char *)mono_bstr_alloc ((slen + 1) * sizeof(char));
	if (s == NULL)
		return NULL;
	*((guint32 *)s - 1) = (guint32)(slen * sizeof (char));
	if (ptr)
		memcpy (s, ptr, slen * sizeof (char));
	s [slen] = 0;
	return s;
}

MonoStringHandle
mono_string_from_bstr_checked (mono_bstr_const bstr, MonoError *error)
{
	if (!bstr)
		return NULL_HANDLE_STRING;
#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_BSTR
	return mono_string_new_utf16_handle (bstr, SysStringLen ((BSTR)bstr), error);
#else
	return mono_string_new_utf16_handle (bstr, *((guint32 *)bstr - 1) / sizeof (gunichar2), error);
#endif /* HAVE_API_SUPPORT_WIN32_BSTR */
#else
#ifndef DISABLE_COM
	if (com_provider == MONO_COM_DEFAULT)
#endif
		return mono_string_new_utf16_handle (bstr, *((guint32 *)bstr - 1) / sizeof (gunichar2), error);
#ifndef DISABLE_COM
	else if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		glong written = 0;
		// FIXME mono_string_new_utf32_handle to combine g_ucs4_to_utf16 and mono_string_new_utf16_handle.
		gunichar2* utf16 = g_ucs4_to_utf16 ((const gunichar *)bstr, sys_string_len_ms (bstr), NULL, &written, NULL);
		MonoStringHandle res = mono_string_new_utf16_handle (utf16, written, error);
		g_free (utf16);
		return res;
	} else {
		g_assert_not_reached ();
	}
#endif // DISABLE_COM
#endif // HOST_WIN32
}

MonoString *
mono_string_from_bstr (/*mono_bstr_const*/gpointer bstr)
{
	// FIXME gcmode
	HANDLE_FUNCTION_ENTER ();
	ERROR_DECL (error);
	MonoStringHandle result = mono_string_from_bstr_checked ((mono_bstr_const)bstr, error);
	mono_error_cleanup (error);
	HANDLE_FUNCTION_RETURN_OBJ (result);
}

MonoStringHandle
mono_string_from_bstr_icall_impl (mono_bstr_const bstr, MonoError *error)
{
	return mono_string_from_bstr_checked (bstr, error);
}

MONO_API void
mono_free_bstr (/*mono_bstr_const*/gpointer bstr)
{
	if (!bstr)
		return;
#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_BSTR
	SysFreeString ((BSTR)bstr);
#else
	g_free (((char *)bstr) - 4);
#endif /* HAVE_API_SUPPORT_WIN32_BSTR */
#else
#ifndef DISABLE_COM
	if (com_provider == MONO_COM_DEFAULT) {
#endif
		g_free (((char *)bstr) - SIZEOF_VOID_P);
#ifndef DISABLE_COM
	} else if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		sys_free_string_ms ((mono_bstr_const)bstr);
	} else {
		g_assert_not_reached ();
	}
#endif // DISABLE_COM
#endif // HOST_WIN32
}

// FIXME There are multiple caches of "GetObjectForNativeVariant".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Marshal_GetObjectForNativeVariant (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_object_for_native_variant)
		ERROR_DECL (error);
		get_object_for_native_variant = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_object_for_native_variant)

	g_assert (get_object_for_native_variant);

	return get_object_for_native_variant;
}

// FIXME There are multiple caches of "GetNativeVariantForObject".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Marshal_GetNativeVariantForObject (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, get_native_variant_for_object)

		ERROR_DECL (error);
		get_native_variant_for_object = mono_class_get_method_from_name_checked (mono_defaults.marshal_class, "GetNativeVariantForObject", 2, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, get_native_variant_for_object)

	g_assert (get_native_variant_for_object);

	return get_native_variant_for_object;
}

G_GNUC_UNUSED
static MonoMethod*
mono_get_Array_SetValueImpl (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, set_value_impl)

		ERROR_DECL (error);
		set_value_impl = mono_class_get_method_from_name_checked (mono_defaults.array_class, "SetValueImpl", 2, 0, error);
		mono_error_assert_ok (error);

	MONO_STATIC_POINTER_INIT_END (MonoMethod, set_value_impl)

	g_assert (set_value_impl);

	return set_value_impl;
}

#ifndef DISABLE_COM

// FIXME There are multiple caches of "Clear".
G_GNUC_UNUSED
static MonoMethod*
mono_get_Variant_Clear (void)
{
	MONO_STATIC_POINTER_INIT (MonoMethod, variant_clear)
		ERROR_DECL (error);
		variant_clear = mono_class_get_method_from_name_checked (mono_class_get_variant_class (), "Clear", 0, 0, error);
		mono_error_assert_ok (error);
	MONO_STATIC_POINTER_INIT_END (MonoMethod, variant_clear)

	g_assert (variant_clear);
	return variant_clear;
}

/* SAFEARRAY marshalling */
int
mono_cominterop_emit_marshal_safearray (EmitMarshalContext *m, int argnum, MonoType *t,
										MonoMarshalSpec *spec,
										int conv_arg, MonoType **conv_arg_type,
										MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

#ifndef DISABLE_JIT
	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		if ((t->attrs & (PARAM_ATTRIBUTE_IN | PARAM_ATTRIBUTE_OUT)) == PARAM_ATTRIBUTE_OUT)
			break;

		/* Generates IL code for the following algorithm:

				SafeArray safearray;   // safearray_var
				IntPtr indices; // indices_var
				int empty;      // empty_var
				if (mono_marshal_safearray_create (array, out safearray, out indices, out empty)) {
					if (!empty) {
						int index=0; // index_var
						do { // label3
							variant elem = Marshal.GetNativeVariantForObject (array.GetValueImpl(index));
							mono_marshal_safearray_set_value (safearray, indices, elem);
							++index;
						}
						while (mono_marshal_safearray_next (safearray, indices));
					} // label2
					mono_marshal_safearray_free_indices (indices);
				} // label1
		*/

		int safearray_var, indices_var, empty_var, elem_var, index_var;
		guint32 label1 = 0, label2 = 0, label3 = 0;

		MonoType *int_type = mono_get_int_type ();
		conv_arg = safearray_var = mono_mb_add_local (mb, mono_get_object_type ());
		indices_var = mono_mb_add_local (mb, int_type);
		empty_var = mono_mb_add_local (mb, int_type);

		if (m_type_is_byref (t)) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		} else
			mono_mb_emit_ldarg (mb, argnum);

		mono_mb_emit_ldloc_addr (mb, safearray_var);
		mono_mb_emit_ldloc_addr (mb, indices_var);
		mono_mb_emit_ldloc_addr (mb, empty_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_create);

		label1 = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldloc (mb, empty_var);

		label2 = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

		index_var = mono_mb_add_local (mb, mono_get_int32_type ());
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);

		label3 = mono_mb_get_label (mb);

		MONO_STATIC_POINTER_INIT (MonoMethod, get_value_impl)

			ERROR_DECL (error);
			get_value_impl = mono_class_get_method_from_name_checked (mono_defaults.array_class, "GetValueImpl", 1, 0, error);
			mono_error_assert_ok (error);

		MONO_STATIC_POINTER_INIT_END (MonoMethod, get_value_impl)

		g_assert (get_value_impl);

		if (m_type_is_byref (t)) {
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		} else
			mono_mb_emit_ldarg (mb, argnum);

		mono_mb_emit_ldloc (mb, index_var);

		mono_mb_emit_managed_call (mb, get_value_impl, NULL);

		elem_var =  mono_mb_add_local (mb, m_class_get_byval_arg (mono_class_get_variant_class ()));
		mono_mb_emit_ldloc_addr (mb, elem_var);

		mono_mb_emit_managed_call (mb, mono_get_Marshal_GetNativeVariantForObject (), NULL);

		mono_mb_emit_ldloc (mb, safearray_var);
		mono_mb_emit_ldloc (mb, indices_var);
		mono_mb_emit_ldloc_addr (mb, elem_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_set_value);

		mono_mb_emit_ldloc_addr (mb, elem_var);
		mono_mb_emit_managed_call (mb, mono_get_Variant_Clear (), NULL);

		mono_mb_emit_add_to_local (mb, index_var, 1);

		mono_mb_emit_ldloc (mb, safearray_var);
		mono_mb_emit_ldloc (mb, indices_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_next);
		mono_mb_emit_branch_label (mb, CEE_BRTRUE, label3);

		mono_mb_patch_short_branch (mb, label2);

		mono_mb_emit_ldloc (mb, indices_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_free_indices);

		mono_mb_patch_short_branch (mb, label1);
		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (m_type_is_byref (t))
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_OUT: {
		if (t->attrs & PARAM_ATTRIBUTE_OUT) {
			/* Generates IL code for the following algorithm:

					Array result;   // result_var
					IntPtr indices; // indices_var
					int empty;      // empty_var
					bool byValue = !m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_IN);
					if (mono_marshal_safearray_begin(safearray, out result, out indices, out empty, parameter, byValue)) {
						if (!empty) {
							int index=0; // index_var
							do { // label3
								if (!byValue || (index < parameter.Length)) {
									object elem = Variant.GetObjectForNativeVariant(mono_marshal_safearray_get_value(safearray, indices));
									result.SetValueImpl(elem, index);
								}
								++index;
							}
							while (mono_marshal_safearray_next(safearray, indices));
						} // label2
						mono_marshal_safearray_end(safearray, indices);
					} // label1
					if (!byValue)
						return result;
			*/

			int result_var, indices_var, empty_var, elem_var, index_var;
			guint32 label1 = 0, label2 = 0, label3 = 0, label4 = 0;
			gboolean byValue = !m_type_is_byref (t) && (t->attrs & PARAM_ATTRIBUTE_IN);

			MonoType *object_type = mono_get_object_type ();
			MonoType *int_type = mono_get_int_type ();
			result_var = mono_mb_add_local (mb, object_type);
			indices_var = mono_mb_add_local (mb, int_type);
			empty_var = mono_mb_add_local (mb, int_type);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc_addr (mb, result_var);
			mono_mb_emit_ldloc_addr (mb, indices_var);
			mono_mb_emit_ldloc_addr (mb, empty_var);
			mono_mb_emit_ldarg (mb, argnum);
			if (byValue)
				mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			else
				mono_mb_emit_byte (mb, CEE_LDC_I4_1);
			mono_mb_emit_icall (mb, mono_marshal_safearray_begin);

			label1 = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

			mono_mb_emit_ldloc (mb, empty_var);

			label2 = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

			index_var = mono_mb_add_local (mb, int_type);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);

			label3 = mono_mb_get_label (mb);

			if (byValue) {
				mono_mb_emit_ldloc (mb, index_var);
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDLEN);
				label4 = mono_mb_emit_branch (mb, CEE_BGE);
			}

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, indices_var);
			mono_mb_emit_icall (mb, mono_marshal_safearray_get_value);

			elem_var = mono_mb_add_local (mb, object_type);

			mono_mb_emit_managed_call (mb, mono_get_Marshal_GetObjectForNativeVariant (), NULL);
			mono_mb_emit_stloc (mb, elem_var);

			mono_mb_emit_ldloc (mb, result_var);
			mono_mb_emit_ldloc (mb, elem_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_managed_call (mb, mono_get_Array_SetValueImpl (), NULL);

			if (byValue)
				mono_mb_patch_short_branch (mb, label4);

			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, indices_var);
			mono_mb_emit_icall (mb, mono_marshal_safearray_next);
			mono_mb_emit_branch_label (mb, CEE_BRTRUE, label3);

			mono_mb_patch_short_branch (mb, label2);

			mono_mb_emit_ldloc (mb, conv_arg);
			mono_mb_emit_ldloc (mb, indices_var);
			mono_mb_emit_icall (mb, mono_marshal_safearray_end);

			mono_mb_patch_short_branch (mb, label1);

			if (!byValue) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, result_var);
				mono_mb_emit_byte (mb, CEE_STIND_REF);
			}
		}
		break;
	}
	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		if ((t->attrs & (PARAM_ATTRIBUTE_IN | PARAM_ATTRIBUTE_OUT)) == PARAM_ATTRIBUTE_OUT)
			break;

		/* Generates IL code for the following algorithm:

				Array result;   // result_var
				IntPtr indices; // indices_var
				int empty;      // empty_var
				if (mono_marshal_safearray_begin(safearray, out result, out indices, out empty, NULL, TRUE)) {
					if (!empty) {
						int index=0; // index_var
						do { // label3
							object elem = Variant.GetObjectForNativeVariant(mono_marshal_safearray_get_value(safearray, indices));
							result.SetValueImpl(elem, index);
							++index;
						}
						while (mono_marshal_safearray_next(safearray, indices));
					} // label2
					mono_marshal_safearray_free_indices(indices);
				} // label1
		*/

		int result_var, indices_var, empty_var, elem_var, index_var;
		guint32 label1 = 0, label2 = 0, label3 = 0;

		MonoType *object_type = mono_get_object_type ();
		MonoType *int_type = mono_get_int_type ();
		result_var = mono_mb_add_local (mb, object_type);
		indices_var = mono_mb_add_local (mb, int_type);
		empty_var = mono_mb_add_local (mb, int_type);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc_addr (mb, result_var);
		mono_mb_emit_ldloc_addr (mb, indices_var);
		mono_mb_emit_ldloc_addr (mb, empty_var);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_CONV_I);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_icall (mb, mono_marshal_safearray_begin);

		label1 = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldloc (mb, empty_var);

		label2 = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);

		index_var = mono_mb_add_local (mb, int_type);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_stloc (mb, index_var);

		label3 = mono_mb_get_label (mb);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, indices_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_get_value);

		elem_var = mono_mb_add_local (mb, object_type);

		mono_mb_emit_managed_call (mb, mono_get_Marshal_GetObjectForNativeVariant (), NULL);
		mono_mb_emit_stloc (mb, elem_var);

		mono_mb_emit_ldloc (mb, result_var);
		mono_mb_emit_ldloc (mb, elem_var);
		mono_mb_emit_ldloc (mb, index_var);
		mono_mb_emit_managed_call (mb, mono_get_Array_SetValueImpl (), NULL);

		mono_mb_emit_add_to_local (mb, index_var, 1);

		mono_mb_emit_ldarg (mb, argnum);
		mono_mb_emit_ldloc (mb, indices_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_next);
		mono_mb_emit_branch_label (mb, CEE_BRTRUE, label3);

		mono_mb_patch_short_branch (mb, label2);

		mono_mb_emit_ldloc (mb, indices_var);
		mono_mb_emit_icall (mb, mono_marshal_safearray_free_indices);

		mono_mb_patch_short_branch (mb, label1);

		mono_mb_emit_ldloc (mb, result_var);
		mono_mb_emit_stloc (mb, conv_arg);

		break;
	}
	default:
		g_assert_not_reached ();
	}
#endif /* DISABLE_JIT */

	return conv_arg;
}

#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static guint32
mono_marshal_win_safearray_get_dim (gpointer safearray)
{
	return SafeArrayGetDim ((SAFEARRAY*)safearray);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static guint32
mono_marshal_win_safearray_get_dim (gpointer safearray)
{
	g_unsupported_api ("SafeArrayGetDim");
	SetLastError (ERROR_NOT_SUPPORTED);
	return MONO_E_NOTIMPL;
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

static guint32
mono_marshal_safearray_get_dim (gpointer safearray)
{
	return mono_marshal_win_safearray_get_dim (safearray);
}

#else /* HOST_WIN32 */

static guint32
mono_marshal_safearray_get_dim (gpointer safearray)
{
	guint32 result=0;
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		result = safe_array_get_dim_ms (safearray);
	} else {
		g_assert_not_reached ();
	}
	return result;
}
#endif /* HOST_WIN32 */

#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safe_array_get_lbound (gpointer psa, guint nDim, glong* plLbound)
{
	return SafeArrayGetLBound ((SAFEARRAY*)psa, nDim, plLbound);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safe_array_get_lbound (gpointer psa, guint nDim, glong* plLbound)
{
	g_unsupported_api ("SafeArrayGetLBound");
	SetLastError (ERROR_NOT_SUPPORTED);
	return MONO_E_NOTIMPL;
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

static int
mono_marshal_safe_array_get_lbound (gpointer psa, guint nDim, glong* plLbound)
{
	return mono_marshal_win_safe_array_get_lbound (psa, nDim, plLbound);
}

#else /* HOST_WIN32 */

static int
mono_marshal_safe_array_get_lbound (gpointer psa, guint nDim, glong* plLbound)
{
	int result=MONO_S_OK;
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		result = safe_array_get_lbound_ms (psa, nDim, plLbound);
	} else {
		g_assert_not_reached ();
	}
	return result;
}
#endif /* HOST_WIN32 */

#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safe_array_get_ubound (gpointer psa, guint nDim, glong* plUbound)
{
	return SafeArrayGetUBound ((SAFEARRAY*)psa, nDim, plUbound);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safe_array_get_ubound (gpointer psa, guint nDim, glong* plUbound)
{
	g_unsupported_api ("SafeArrayGetUBound");
	SetLastError (ERROR_NOT_SUPPORTED);
	return MONO_E_NOTIMPL;
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

static int
mono_marshal_safe_array_get_ubound (gpointer psa, guint nDim, glong* plUbound)
{
	return mono_marshal_win_safe_array_get_ubound (psa, nDim, plUbound);
}

#else /* HOST_WIN32 */

static int
mono_marshal_safe_array_get_ubound (gpointer psa, guint nDim, glong* plUbound)
{
	int result=MONO_S_OK;
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		result = safe_array_get_ubound_ms (psa, nDim, plUbound);
	} else {
		g_assert_not_reached ();
	}
	return result;
}
#endif /* HOST_WIN32 */

/* This is an icall */
static gboolean
mono_marshal_safearray_begin (gpointer safearray, MonoArray **result, gpointer *indices, gpointer empty, gpointer parameter, gboolean allocateNewArray)
{
	ERROR_DECL (error);
	int dim;
	uintptr_t *sizes;
	intptr_t *bounds;
	MonoClass *aklass;
	int i;
	gboolean bounded = FALSE;

#ifndef HOST_WIN32
	// If not on windows, check that the MS provider is used as it is
	// required for SAFEARRAY support.
	// If SAFEARRAYs are not supported, returning FALSE from this
	// function will prevent the other mono_marshal_safearray_xxx functions
	// from being called.
	if ((com_provider != MONO_COM_MS) || !init_com_provider_ms ()) {
		return FALSE;
	}
#endif

	(*(int*)empty) = TRUE;

	if (safearray != NULL) {

		dim = mono_marshal_safearray_get_dim (safearray);

		if (dim > 0) {

			*indices = g_malloc (dim * sizeof(int));

			sizes = g_newa (uintptr_t, dim);
			bounds = g_newa (intptr_t, dim);

			for (i=0; i<dim; ++i) {
				glong lbound, ubound;
				int cursize;
				int hr;

				hr = mono_marshal_safe_array_get_lbound (safearray, i+1, &lbound);
				if (hr < 0) {
					cominterop_set_hr_error (error, hr);
					if (mono_error_set_pending_exception (error))
						return FALSE;
				}
				if (lbound != 0)
					bounded = TRUE;
				hr = mono_marshal_safe_array_get_ubound (safearray, i+1, &ubound);
				if (hr < 0) {
					cominterop_set_hr_error (error, hr);
					if (mono_error_set_pending_exception (error))
						return FALSE;
				}
				cursize = ubound-lbound+1;
				sizes [i] = cursize;
				bounds [i] = lbound;

				((int*)*indices) [i] = lbound;

				if (cursize != 0)
					(*(int*)empty) = FALSE;
			}

			if (allocateNewArray) {
				aklass = mono_class_create_bounded_array (mono_defaults.object_class, dim, bounded);
				*result = mono_array_new_full_checked (mono_domain_get (), aklass, sizes, bounds, error);
				if (mono_error_set_pending_exception (error))
					return FALSE;
			} else {
				*result = (MonoArray *)parameter;
			}
		}
	}
	return TRUE;
}

/* This is an icall */
#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safearray_get_value (gpointer safearray, gpointer indices, gpointer *result)
{
	return SafeArrayPtrOfIndex ((SAFEARRAY*)safearray, (LONG*)indices, result);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safearray_get_value (gpointer safearray, gpointer indices, gpointer *result)
{
	ERROR_DECL (error);
	g_unsupported_api ("SafeArrayPtrOfIndex");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "SafeArrayPtrOfIndex");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return MONO_E_NOTIMPL;
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

static gpointer
mono_marshal_safearray_get_value (gpointer safearray, gpointer indices)
{
	ERROR_DECL (error);
	gpointer result;

	int hr = mono_marshal_win_safearray_get_value (safearray, indices, &result);
	if (hr < 0) {
			cominterop_set_hr_error (error, hr);
			mono_error_set_pending_exception (error);
			result = NULL;
	}

	return result;
}

#else /* HOST_WIN32 */

static gpointer
mono_marshal_safearray_get_value (gpointer safearray, gpointer indices)
{
	ERROR_DECL (error);
	gpointer result;

	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		int hr = safe_array_ptr_of_index_ms (safearray, (glong *)indices, &result);
		if (hr < 0) {
			cominterop_set_hr_error (error, hr);
			mono_error_set_pending_exception (error);
			return NULL;
		}
	} else {
		g_assert_not_reached ();
	}
	return result;
}
#endif /* HOST_WIN32 */

/* This is an icall */
static
gboolean mono_marshal_safearray_next (gpointer safearray, gpointer indices)
{
	ERROR_DECL (error);
	int i;
	int dim = mono_marshal_safearray_get_dim (safearray);
	gboolean ret= TRUE;
	int *pIndices = (int*) indices;
	int hr;

	for (i=dim-1; i>=0; --i)
	{
		glong lbound, ubound;

		hr = mono_marshal_safe_array_get_ubound (safearray, i+1, &ubound);
		if (hr < 0) {
			cominterop_set_hr_error (error, hr);
			mono_error_set_pending_exception (error);
			return FALSE;
		}

		if (++pIndices[i] <= ubound) {
			break;
		}

		hr = mono_marshal_safe_array_get_lbound (safearray, i+1, &lbound);
		if (hr < 0) {
			cominterop_set_hr_error (error, hr);
			mono_error_set_pending_exception (error);
			return FALSE;
		}

		pIndices[i] = lbound;

		if (i == 0)
			ret = FALSE;
	}
	return ret;
}

#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static void
mono_marshal_win_safearray_end (gpointer safearray, gpointer indices)
{
	g_free(indices);
	SafeArrayDestroy ((SAFEARRAY*)safearray);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static void
mono_marshal_win_safearray_end (gpointer safearray, gpointer indices)
{
	g_free(indices);
	g_unsupported_api ("SafeArrayDestroy");
	SetLastError (ERROR_NOT_SUPPORTED);
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

static void
mono_marshal_safearray_end (gpointer safearray, gpointer indices)
{
	mono_marshal_win_safearray_end (safearray, indices);
}

#else /* HOST_WIN32 */

static void
mono_marshal_safearray_end (gpointer safearray, gpointer indices)
{
	g_free(indices);
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		safe_array_destroy_ms (safearray);
	} else {
		g_assert_not_reached ();
	}
}
#endif /* HOST_WIN32 */

#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static gboolean
mono_marshal_win_safearray_create_internal (UINT cDims, SAFEARRAYBOUND *rgsabound, gpointer *newsafearray)
{
	*newsafearray = SafeArrayCreate (VT_VARIANT, cDims, rgsabound);
	return TRUE;
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static gboolean
mono_marshal_win_safearray_create_internal (UINT cDims, SAFEARRAYBOUND *rgsabound, gpointer *newsafearray)
{
	g_unsupported_api ("SafeArrayCreate");
	SetLastError (ERROR_NOT_SUPPORTED);
	*newsafearray = NULL;
	return FALSE;
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

static gboolean
mono_marshal_safearray_create_internal (UINT cDims, SAFEARRAYBOUND *rgsabound, gpointer *newsafearray)
{
	return mono_marshal_win_safearray_create_internal (cDims, rgsabound, newsafearray);
}

#else /* HOST_WIN32 */

static gboolean
mono_marshal_safearray_create_internal (UINT cDims, SAFEARRAYBOUND *rgsabound, gpointer *newsafearray)
{
	*newsafearray = safe_array_create_ms (VT_VARIANT, cDims, rgsabound);
	return TRUE;
}

#endif /* HOST_WIN32 */

static gboolean
mono_marshal_safearray_create (MonoArray *input, gpointer *newsafearray, gpointer *indices, gpointer empty)
{
#ifndef HOST_WIN32
	// If not on windows, check that the MS provider is used as it is
	// required for SAFEARRAY support.
	// If SAFEARRAYs are not supported, returning FALSE from this
	// function will prevent the other mono_marshal_safearray_xxx functions
	// from being called.
	if (com_provider != MONO_COM_MS || !init_com_provider_ms ()) {
		return FALSE;
	}
#endif

	int const max_array_length = mono_array_length_internal (input);
	int const dim = m_class_get_rank (mono_object_class (input));

	*indices = g_malloc (dim * sizeof (int));
	SAFEARRAYBOUND * const bounds = g_newa (SAFEARRAYBOUND, dim);
	(*(int*)empty) = (max_array_length == 0);

	if (dim > 1) {
		for (int i = 0; i < dim; ++i) {
			((int*)*indices) [i] = bounds [i].lLbound = input->bounds [i].lower_bound;
			bounds [i].cElements = input->bounds [i].length;
		}
	} else {
		((int*)*indices) [0] = 0;
		bounds [0].cElements = max_array_length;
		bounds [0].lLbound = 0;
	}

	return mono_marshal_safearray_create_internal (dim, bounds, newsafearray);
}

/* This is an icall */
#ifdef HOST_WIN32
#if HAVE_API_SUPPORT_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safearray_set_value (gpointer safearray, gpointer indices, gpointer value)
{
	return SafeArrayPutElement ((SAFEARRAY*)safearray, (LONG*)indices, value);
}
#elif !HAVE_EXTERN_DEFINED_WIN32_SAFE_ARRAY
static int
mono_marshal_win_safearray_set_value (gpointer safearray, gpointer indices, gpointer value)
{
	ERROR_DECL (error);
	g_unsupported_api ("SafeArrayPutElement");
	mono_error_set_not_supported (error, G_UNSUPPORTED_API, "SafeArrayPutElement");
	mono_error_set_pending_exception (error);
	SetLastError (ERROR_NOT_SUPPORTED);
	return MONO_E_NOTIMPL;
}
#endif /* HAVE_API_SUPPORT_WIN32_SAFE_ARRAY */

#endif /* HOST_WIN32 */

static void
mono_marshal_safearray_set_value (gpointer safearray, gpointer indices, gpointer value)
{
	ERROR_DECL (error);
#ifdef HOST_WIN32
	int const hr = mono_marshal_win_safearray_set_value (safearray, indices, value);
#else
	int hr = 0;
	if (com_provider == MONO_COM_MS && init_com_provider_ms ())
		hr = safe_array_put_element_ms (safearray, (glong *)indices, (void **)value);
	else
		g_assert_not_reached ();
#endif
	if (hr < 0) {
		cominterop_set_hr_error (error, hr);
		mono_error_set_pending_exception (error);
	}
}

static
void mono_marshal_safearray_free_indices (gpointer indices)
{
	g_free (indices);
}

#else /* DISABLE_COM */

gboolean
mono_marshal_free_ccw (MonoObject* object)
{
	return FALSE;
}

#ifdef HOST_WIN32

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (MonoIUnknown *pUnk)
{
	return mono_IUnknown_AddRef (pUnk);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (MonoIUnknown *pUnk)
{
	g_assert (pUnk);
	return mono_IUnknown_Release (pUnk);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (MonoIUnknown *pUnk, gconstpointer riid, gpointer* ppv)
{
	return mono_IUnknown_QueryInterface (pUnk, riid, ppv);
}

#else /* HOST_WIN32 */

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (MonoIUnknown *pUnk)
{
	g_assert_not_reached ();
	return 0;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (MonoIUnknown *pUnk)
{
	g_assert_not_reached ();
	return 0;
}


int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (MonoIUnknown *pUnk, gconstpointer riid, gpointer* ppv)
{
	g_assert_not_reached ();
	return 0;
}

#endif /* HOST_WIN32 */
#endif /* DISABLE_COM */

mono_bstr
ves_icall_System_Runtime_InteropServices_Marshal_BufferToBSTR (const gunichar2* ptr, int len)
{
	return mono_ptr_to_bstr (ptr, len);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (mono_bstr_const ptr)
{
	mono_free_bstr ((gpointer)ptr);
}

void*
mono_cominterop_get_com_interface (MonoObject *object_raw, MonoClass *ic, MonoError *error)
{
	HANDLE_FUNCTION_ENTER ();
	MONO_HANDLE_DCL (MonoObject, object);
	void* const result = mono_cominterop_get_com_interface_internal (FALSE, object, ic, error);
	HANDLE_FUNCTION_RETURN_VAL (result);
}

static void*
mono_cominterop_get_com_interface_internal (gboolean icall, MonoObjectHandle object, MonoClass *ic, MonoError *error)
{
	// Common code for mono_cominterop_get_com_interface and
	// ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal,
	// which are almost identical.
#ifndef DISABLE_COM
	if (MONO_HANDLE_IS_NULL (object))
		return NULL;

	MonoRealProxyHandle real_proxy;

	if (cominterop_object_is_rcw_handle (object, &real_proxy)) {
		MonoClass *klass = NULL;
		klass = mono_handle_class (object);
		if (TRUE) {
			g_assertf (!icall, "Class is not transparent");
			mono_error_set_invalid_operation (error, "Class is not transparent");
			return NULL;
		}

		if (MONO_HANDLE_IS_NULL (real_proxy)) {
			g_assertf (!icall, "RealProxy is null");
			mono_error_set_invalid_operation (error, "RealProxy is null");
			return NULL;
		}

		klass = mono_handle_class (real_proxy);
		if (klass != mono_class_get_interop_proxy_class ()) {
			g_assertf (!icall, "Object is not a proxy");
			mono_error_set_invalid_operation (error, "Object is not a proxy");
			return NULL;
		}

		MonoComInteropProxyHandle com_interop_proxy = MONO_HANDLE_CAST (MonoComInteropProxy, real_proxy);
		MonoComObjectHandle com_object = MONO_HANDLE_NEW_GET (MonoComObject, com_interop_proxy, com_object);

		if (MONO_HANDLE_IS_NULL (com_object)) {
			g_assertf (!icall, "Proxy points to null COM object");
			mono_error_set_invalid_operation (error, "Proxy points to null COM object");
			return NULL;
		}

		if (icall)
			return MONO_HANDLE_GETVAL (com_object, iunknown);
		return cominterop_get_interface_checked (com_object, ic, error);
	}
	else {
		if (icall)
			ic = mono_class_get_iunknown_class ();
		return cominterop_get_ccw_checked (object, ic, error);
	}
#else
	g_assert_not_reached ();
#endif
}

gboolean
mono_cominterop_is_interface (MonoClass* klass)
{
#ifndef DISABLE_COM
	ERROR_DECL (error);
	MonoCustomAttrInfo* cinfo = NULL;
	gboolean ret = FALSE;
	int i;

	cinfo = mono_custom_attrs_from_class_checked (klass, error);
	mono_error_assert_ok (error);
	if (cinfo) {
		for (i = 0; i < cinfo->num_attrs; ++i) {
			MonoClass *ctor_class = cinfo->attrs [i].ctor->klass;
			if (mono_class_has_parent (ctor_class, mono_class_get_interface_type_attribute_class ())) {
				ret = TRUE;
				break;
			}
		}
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	return ret;
#else
	g_assert_not_reached ();
#endif
}
