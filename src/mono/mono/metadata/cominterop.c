/*
 * cominterop.c: COM Interop Support
 * 
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif

#include "object.h"
#include "loader.h"
#include "cil-coff.h"
#include "metadata/abi-details.h"
#include "metadata/cominterop.h"
#include "metadata/marshal.h"
#include "metadata/method-builder.h"
#include "metadata/tabledefs.h"
#include "metadata/exception.h"
#include "metadata/appdomain.h"
#include "metadata/reflection-internals.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threads.h"
#include "mono/metadata/monitor.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/domain-internals.h"
#include "mono/metadata/gc-internal.h"
#include "mono/metadata/threads-types.h"
#include "mono/metadata/string-icalls.h"
#include "mono/metadata/attrdefs.h"
#include "mono/metadata/gc-internal.h"
#include "mono/utils/mono-counters.h"
#include "mono/utils/strenc.h"
#include "mono/utils/atomic.h"
#include <string.h>
#include <errno.h>

/*
Code shared between the DISABLE_COM and !DISABLE_COM
*/
static void
register_icall (gpointer func, const char *name, const char *sigstr, gboolean save)
{
	MonoMethodSignature *sig = mono_create_icall_signature (sigstr);

	mono_register_jit_icall (func, name, sig, save);
}

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
#define mono_cominterop_lock() mono_mutex_lock (&cominterop_mutex)
#define mono_cominterop_unlock() mono_mutex_unlock (&cominterop_mutex)
static mono_mutex_t cominterop_mutex;

/* STDCALL on windows, CDECL everywhere else to work with XPCOM and MainWin COM */
#ifdef  HOST_WIN32
#define STDCALL __stdcall
#else
#define STDCALL
#endif

GENERATE_GET_CLASS_WITH_CACHE (interop_proxy, Mono.Interop, ComInteropProxy)
GENERATE_GET_CLASS_WITH_CACHE (idispatch,     Mono.Interop, IDispatch)
GENERATE_GET_CLASS_WITH_CACHE (iunknown,      Mono.Interop, IUnknown)

GENERATE_GET_CLASS_WITH_CACHE (com_object, System, __ComObject)
GENERATE_GET_CLASS_WITH_CACHE (variant,    System, Variant)

/* Upon creation of a CCW, only allocate a weak handle and set the
 * reference count to 0. If the unmanaged client code decides to addref and
 * hold onto the CCW, I then allocate a strong handle. Once the reference count
 * goes back to 0, convert back to a weak handle.
 */
typedef struct {
	guint32 ref_count;
	guint32 gc_handle;
	GHashTable* vtable_hash;
#ifdef  HOST_WIN32
	gpointer free_marshaler;
#endif
} MonoCCW;

/* This type is the actual pointer passed to unmanaged code
 * to represent a COM interface.
 */
typedef struct {
	gpointer vtable;
	MonoCCW* ccw;
} MonoCCWInterface;

/* IUnknown */
static int STDCALL cominterop_ccw_addref (MonoCCWInterface* ccwe);

static int STDCALL cominterop_ccw_release (MonoCCWInterface* ccwe);

static int STDCALL cominterop_ccw_queryinterface (MonoCCWInterface* ccwe, guint8* riid, gpointer* ppv);

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

static MonoObject*
cominterop_get_ccw_object (MonoCCWInterface* ccw_entry, gboolean verify);

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
	MonoImage *image = method->klass->image;
	MonoMethodSignature *sig = mono_method_signature (method);
	gboolean preserve_sig = method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG;
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
	res->params[0] = &mono_defaults.int_class->byval_arg;

	if (preserve_sig) {
		res->ret = sig->ret;
	}
	else {
		// last arg is return type
		if (!MONO_TYPE_IS_VOID (sig->ret)) {
			res->params[param_count-1] = mono_metadata_type_dup (image, sig->ret);
			res->params[param_count-1]->byref = 1;
			res->params[param_count-1]->attrs = PARAM_ATTRIBUTE_OUT;
		}

		// return type is always int32 (HRESULT)
		res->ret = &mono_defaults.int32_class->byval_arg;
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
cominterop_object_is_rcw (MonoObject *obj)
{
	MonoClass *klass = NULL;
	MonoRealProxy* real_proxy = NULL;
	if (!obj)
		return FALSE;
	klass = mono_object_class (obj);
	if (!mono_class_is_transparent_proxy (klass))
		return FALSE;

	real_proxy = ((MonoTransparentProxy*)obj)->rp;
	if (!real_proxy)
		return FALSE;

	klass = mono_object_class (real_proxy);
	return (klass && klass == mono_class_get_interop_proxy_class ());
}

static int
cominterop_get_com_slot_begin (MonoClass* klass)
{
	static MonoClass *interface_type_attribute = NULL;
	MonoCustomAttrInfo *cinfo = NULL;
	MonoInterfaceTypeAttribute* itf_attr = NULL; 

	if (!interface_type_attribute)
		interface_type_attribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "InterfaceTypeAttribute");
	cinfo = mono_custom_attrs_from_class (klass);
	if (cinfo) {
		MonoError error;
		itf_attr = (MonoInterfaceTypeAttribute*)mono_custom_attrs_get_attr_checked (cinfo, interface_type_attribute, &error);
		g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/
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
	MonoError error;
	MonoClass *ic = method->klass;

	/* if method is on a class, we need to look up interface method exists on */
	if (!MONO_CLASS_IS_INTERFACE(method->klass)) {
		GPtrArray *ifaces = mono_class_get_implemented_interfaces (method->klass, &error);
		g_assert (mono_error_ok (&error));
		if (ifaces) {
			int i;
			mono_class_setup_vtable (method->klass);
			for (i = 0; i < ifaces->len; ++i) {
				int j, offset;
				gboolean found = FALSE;
				ic = g_ptr_array_index (ifaces, i);
				offset = mono_class_interface_offset (method->klass, ic);
				for (j = 0; j < ic->method.count; ++j) {
					if (method->klass->vtable [j + offset] == method) {
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

	if (!ic) 
		g_assert (ic);
	g_assert (MONO_CLASS_IS_INTERFACE (ic));

	return ic;
}

/**
 * cominterop_get_com_slot_for_method:
 * @method: a method
 *
 * Returns: the method's slot in the COM interface vtable
 */
static int
cominterop_get_com_slot_for_method (MonoMethod* method)
{
	guint32 slot = method->slot;
 	MonoClass *ic = method->klass;

	/* if method is on a class, we need to look up interface method exists on */
	if (!MONO_CLASS_IS_INTERFACE(ic)) {
		int offset = 0;
		int i = 0;
		ic = cominterop_get_method_interface (method);
		offset = mono_class_interface_offset (method->klass, ic);
		g_assert(offset >= 0);
		for(i = 0; i < ic->method.count; ++i) {
			if (method->klass->vtable [i + offset] == method)
			{
				slot = ic->methods[i]->slot;
				break;
			}
		}
	}

	g_assert (ic);
	g_assert (MONO_CLASS_IS_INTERFACE (ic));

	return slot + cominterop_get_com_slot_begin (ic);
}


static void
cominterop_mono_string_to_guid (MonoString* string, guint8 *guid);

static gboolean
cominterop_class_guid (MonoClass* klass, guint8* guid)
{
	static MonoClass *GuidAttribute = NULL;
	MonoCustomAttrInfo *cinfo;

	/* Handle the GuidAttribute */
	if (!GuidAttribute)
		GuidAttribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "GuidAttribute");

	cinfo = mono_custom_attrs_from_class (klass);	
	if (cinfo) {
		MonoError error;
		MonoReflectionGuidAttribute *attr = (MonoReflectionGuidAttribute*)mono_custom_attrs_get_attr_checked (cinfo, GuidAttribute, &error);
		g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/

		if (!attr)
			return FALSE;
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);

		cominterop_mono_string_to_guid (attr->guid, guid);
		return TRUE;
	}
	return FALSE;
}

static gboolean
cominterop_com_visible (MonoClass* klass)
{
	static MonoClass *ComVisibleAttribute = NULL;
	MonoError error;
	MonoCustomAttrInfo *cinfo;
	GPtrArray *ifaces;
	MonoBoolean visible = 1;

	/* Handle the ComVisibleAttribute */
	if (!ComVisibleAttribute)
		ComVisibleAttribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "ComVisibleAttribute");

	cinfo = mono_custom_attrs_from_class (klass);
	if (cinfo) {
		MonoError error;
		MonoReflectionComVisibleAttribute *attr = (MonoReflectionComVisibleAttribute*)mono_custom_attrs_get_attr_checked (cinfo, ComVisibleAttribute, &error);
		g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/

		if (attr)
			visible = attr->visible;
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
		if (visible)
			return TRUE;
	}

	ifaces = mono_class_get_implemented_interfaces (klass, &error);
	g_assert (mono_error_ok (&error));
	if (ifaces) {
		int i;
		for (i = 0; i < ifaces->len; ++i) {
			MonoClass *ic = NULL;
			ic = g_ptr_array_index (ifaces, i);
			if (MONO_CLASS_IS_IMPORT (ic))
				visible = TRUE;

		}
		g_ptr_array_free (ifaces, TRUE);
	}
	return visible;

}

static void cominterop_raise_hr_exception (int hr)
{
	static MonoMethod* throw_exception_for_hr = NULL;
	MonoException* ex;
	void* params[1] = {&hr};
	if (!throw_exception_for_hr)
		throw_exception_for_hr = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetExceptionForHR", 1);
	ex = (MonoException*)mono_runtime_invoke (throw_exception_for_hr, NULL, params, NULL);
	mono_raise_exception (ex);
}

/**
 * cominterop_get_interface:
 * @obj: managed wrapper object containing COM object
 * @ic: interface type to retrieve for COM object
 *
 * Returns: the COM interface requested
 */
static gpointer
cominterop_get_interface (MonoComObject* obj, MonoClass* ic, gboolean throw_exception)
{
	gpointer itf = NULL;

	g_assert (ic);
	g_assert (MONO_CLASS_IS_INTERFACE (ic));

	mono_cominterop_lock ();
	if (obj->itf_hash)
		itf = g_hash_table_lookup (obj->itf_hash, GUINT_TO_POINTER ((guint)ic->interface_id));
	mono_cominterop_unlock ();

	if (!itf) {
		guint8 iid [16];
		int found = cominterop_class_guid (ic, iid);
		int hr;
		g_assert(found);
		hr = ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (obj->iunknown, iid, &itf);
		if (hr < 0 && throw_exception) {
			cominterop_raise_hr_exception (hr);	
		}

		if (hr >= 0 && itf) {
			mono_cominterop_lock ();
			if (!obj->itf_hash)
				obj->itf_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
			g_hash_table_insert (obj->itf_hash, GUINT_TO_POINTER ((guint)ic->interface_id), itf);
			mono_cominterop_unlock ();
		}

	}
	if (throw_exception)
		g_assert (itf);

	return itf;
}

static int
cominterop_get_hresult_for_exception (MonoException* exc)
{
	int hr = 0;
	return hr;
}

static MonoReflectionType *
cominterop_type_from_handle (MonoType *handle)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoClass *klass = mono_class_from_mono_type (handle);

	mono_class_init (klass);
	return mono_type_get_object (domain, handle);
}

void
mono_cominterop_init (void)
{
	const char* com_provider_env;

	mono_mutex_init_recursive (&cominterop_mutex);

	com_provider_env = g_getenv ("MONO_COM");
	if (com_provider_env && !strcmp(com_provider_env, "MS"))
		com_provider = MONO_COM_MS;

	register_icall (cominterop_get_method_interface, "cominterop_get_method_interface", "ptr ptr", FALSE);
	register_icall (cominterop_get_function_pointer, "cominterop_get_function_pointer", "ptr ptr int32", FALSE);
	register_icall (cominterop_object_is_rcw, "cominterop_object_is_rcw", "int32 object", FALSE);
	register_icall (cominterop_get_ccw, "cominterop_get_ccw", "ptr object ptr", FALSE);
	register_icall (cominterop_get_ccw_object, "cominterop_get_ccw_object", "object ptr int32", FALSE);
	register_icall (cominterop_get_hresult_for_exception, "cominterop_get_hresult_for_exception", "int32 object", FALSE);
	register_icall (cominterop_get_interface, "cominterop_get_interface", "ptr object ptr int32", FALSE);

	register_icall (mono_string_to_bstr, "mono_string_to_bstr", "ptr obj", FALSE);
	register_icall (mono_string_from_bstr, "mono_string_from_bstr", "obj ptr", FALSE);
	register_icall (mono_free_bstr, "mono_free_bstr", "void ptr", FALSE);
	register_icall (cominterop_type_from_handle, "cominterop_type_from_handle", "object ptr", FALSE);

	/* SAFEARRAY marshalling */
	register_icall (mono_marshal_safearray_begin, "mono_marshal_safearray_begin", "int32 ptr ptr ptr ptr ptr int32", FALSE);
	register_icall (mono_marshal_safearray_get_value, "mono_marshal_safearray_get_value", "ptr ptr ptr", FALSE);
	register_icall (mono_marshal_safearray_next, "mono_marshal_safearray_next", "int32 ptr ptr", FALSE);
	register_icall (mono_marshal_safearray_end, "mono_marshal_safearray_end", "void ptr ptr", FALSE);
	register_icall (mono_marshal_safearray_create, "mono_marshal_safearray_create", "int32 object ptr ptr ptr", FALSE);
	register_icall (mono_marshal_safearray_set_value, "mono_marshal_safearray_set_value", "void ptr ptr ptr", FALSE);
	register_icall (mono_marshal_safearray_free_indices, "mono_marshal_safearray_free_indices", "void ptr", FALSE);
}

void
mono_cominterop_cleanup (void)
{
	mono_mutex_destroy (&cominterop_mutex);
}

void
mono_mb_emit_cominterop_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethod* method)
{
	// get function pointer from 1st arg, the COM interface pointer
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, cominterop_get_com_slot_for_method (method));
	mono_mb_emit_icall (mb, cominterop_get_function_pointer);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_SAVE_LMF);
	mono_mb_emit_calli (mb, sig);
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_RESTORE_LMF);
}

void
mono_cominterop_emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_OBJECT_INTERFACE:
	case MONO_MARSHAL_CONV_OBJECT_IUNKNOWN:
	case MONO_MARSHAL_CONV_OBJECT_IDISPATCH: {
		static MonoClass* com_interop_proxy_class = NULL;
		static MonoMethod* com_interop_proxy_get_proxy = NULL;
		static MonoMethod* get_transparent_proxy = NULL;
		guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
		MonoClass *klass = NULL; 

		klass = mono_class_from_mono_type (type);

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

		if (!com_interop_proxy_class)
			com_interop_proxy_class = mono_class_from_name (mono_defaults.corlib, "Mono.Interop", "ComInteropProxy");
		if (!com_interop_proxy_get_proxy)
			com_interop_proxy_get_proxy = mono_class_get_method_from_name_flags (com_interop_proxy_class, "GetProxy", 2, METHOD_ATTRIBUTE_PRIVATE);
#ifndef DISABLE_REMOTING
		if (!get_transparent_proxy)
			get_transparent_proxy = mono_class_get_method_from_name (mono_defaults.real_proxy_class, "GetTransparentProxy", 0);
#endif

		mono_mb_add_local (mb, &com_interop_proxy_class->byval_arg);

		mono_mb_emit_ldloc (mb, 0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_ptr (mb, &mono_class_get_com_object_class ()->byval_arg);
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
}

void
mono_cominterop_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec)
{
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
			mono_mb_emit_ptr (mb, mono_type_get_class (type));
			mono_mb_emit_icon (mb, TRUE);
			mono_mb_emit_icall (mb, cominterop_get_interface);

		}
		else if (conv == MONO_MARSHAL_CONV_OBJECT_IUNKNOWN) {
			static MonoProperty* iunknown = NULL;
			
			if (!iunknown)
				iunknown = mono_class_get_property_from_name (mono_class_get_com_object_class (), "IUnknown");
			mono_mb_emit_managed_call (mb, iunknown->get, NULL);
		}
		else if (conv == MONO_MARSHAL_CONV_OBJECT_IDISPATCH) {
			static MonoProperty* idispatch = NULL;
			
			if (!idispatch)
				idispatch = mono_class_get_property_from_name (mono_class_get_com_object_class (), "IDispatch");
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
			mono_mb_emit_ptr (mb, mono_type_get_class (type));
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

	sig = mono_method_signature (method);

	// create unmanaged wrapper
	mb_native = mono_mb_new (method->klass, method->name, MONO_WRAPPER_MANAGED_TO_NATIVE);
	sig_native = cominterop_method_signature (method);

	mspecs = g_new (MonoMarshalSpec*, sig_native->param_count+1);
	memset (mspecs, 0, sizeof(MonoMarshalSpec*)*(sig_native->param_count+1));

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

	mono_marshal_emit_native_wrapper (method->klass->image, mb_native, sig_native, piinfo, mspecs, piinfo->addr, FALSE, TRUE, FALSE);

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
 * @method: managed method
 *
 * Returns: the generated method to call
 */
MonoMethod *
mono_cominterop_get_native_wrapper (MonoMethod *method)
{
	MonoMethod *res;
	GHashTable *cache;
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig, *csig;

	g_assert (method);

	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *)method;
		cache = mono_marshal_get_cache (&imethod->owner->cominterop_wrapper_cache, mono_aligned_addr_hash, NULL);
	} else
		cache = mono_marshal_get_cache (&method->klass->image->cominterop_wrapper_cache, mono_aligned_addr_hash, NULL);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	if (!method->klass->vtable)
		mono_class_setup_vtable (method->klass);
	
	if (!method->klass->methods)
		mono_class_setup_methods (method->klass);
	g_assert (!method->klass->exception_type); /*FIXME do proper error handling*/

	sig = mono_method_signature (method);
	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_COMINTEROP);

	/* if method klass is import, that means method
	 * is really a com call. let interop system emit it.
	*/
	if (MONO_CLASS_IS_IMPORT(method->klass)) {
		/* FIXME: we have to call actual class .ctor
		 * instead of just __ComObject .ctor.
		 */
		if (!strcmp(method->name, ".ctor")) {
			static MonoMethod *ctor = NULL;

			if (!ctor)
				ctor = mono_class_get_method_from_name (mono_class_get_com_object_class (), ".ctor", 0);
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_managed_call (mb, ctor, NULL);
			mono_mb_emit_byte (mb, CEE_RET);
		}
		else {
			static MonoMethod * ThrowExceptionForHR = NULL;
			MonoMethod *adjusted_method;
			int retval = 0;
			int ptr_this;
			int i;
			gboolean preserve_sig = method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG;

			// add local variables
			ptr_this = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			if (!MONO_TYPE_IS_VOID (sig->ret))
				retval =  mono_mb_add_local (mb, sig->ret);

			// get the type for the interface the method is defined on
			// and then get the underlying COM interface for that type
			mono_mb_emit_ldarg (mb, 0);
			mono_mb_emit_ptr (mb, method);
			mono_mb_emit_icall (mb, cominterop_get_method_interface);
			mono_mb_emit_icon (mb, TRUE);
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
				if (!ThrowExceptionForHR)
					ThrowExceptionForHR = mono_class_get_method_from_name (mono_defaults.marshal_class, "ThrowExceptionForHR", 1);
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
	csig = mono_metadata_signature_dup_full (method->klass->image, sig);
	csig->pinvoke = 0;
	res = mono_mb_create_and_cache (cache, method,
									mb, csig, csig->param_count + 16);
	mono_mb_free (mb);
	return res;
}

/**
 * mono_cominterop_get_invoke:
 * @method: managed method
 *
 * Returns: the generated method that calls the underlying __ComObject
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
	
	if (method->is_inflated) {
		MonoMethodInflated *imethod = (MonoMethodInflated *)method;
		cache = mono_marshal_get_cache (&imethod->owner->cominterop_invoke_cache, mono_aligned_addr_hash, NULL);
	} else
		cache = mono_marshal_get_cache (&method->klass->image->cominterop_invoke_cache, mono_aligned_addr_hash, NULL);

	g_assert (method);

	if ((res = mono_marshal_find_in_cache (cache, method)))
		return res;

	sig = mono_signature_no_pinvoke (method);

	/* we cant remote methods without this pointer */
	if (!sig->hasthis)
		return method;

	mb = mono_mb_new (method->klass, method->name, MONO_WRAPPER_COMINTEROP_INVOKE);

	/* get real proxy object, which is a ComInteropProxy in this case*/
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoTransparentProxy, rp));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);

	/* load the RCW from the ComInteropProxy*/
	mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoComInteropProxy, com_object));
	mono_mb_emit_byte (mb, CEE_LDIND_REF);

	/* load args and make the call on the RCW */
	for (i = 1; i <= sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i);

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) {
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
		static MonoClass *com_interop_proxy_class = NULL;
		static MonoMethod *cache_proxy = NULL;

		if (!com_interop_proxy_class)
			com_interop_proxy_class = mono_class_from_name (mono_defaults.corlib, "Mono.Interop", "ComInteropProxy");
		if (!cache_proxy)
			cache_proxy = mono_class_get_method_from_name (com_interop_proxy_class, "CacheProxy", 0);

		mono_mb_emit_ldarg (mb, 0);
		mono_mb_emit_ldflda (mb, MONO_STRUCT_OFFSET (MonoTransparentProxy, rp));
		mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_managed_call (mb, cache_proxy, NULL);
	}

	mono_marshal_emit_thread_interrupt_checkpoint (mb);

	mono_mb_emit_byte (mb, CEE_RET);

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

int
mono_cominterop_emit_marshal_com_interface (EmitMarshalContext *m, int argnum, 
											MonoType *t,
											MonoMarshalSpec *spec, 
											int conv_arg, MonoType **conv_arg_type, 
											MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;
	MonoClass *klass = t->data.klass;
	static MonoMethod* get_object_for_iunknown = NULL;
	static MonoMethod* get_iunknown_for_object_internal = NULL;
	static MonoMethod* get_com_interface_for_object_internal = NULL;
	static MonoMethod* get_idispatch_for_object_internal = NULL;
	static MonoMethod* marshal_release = NULL;
	static MonoMethod* AddRef = NULL;
	if (!get_object_for_iunknown)
		get_object_for_iunknown = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetObjectForIUnknown", 1);
	if (!get_iunknown_for_object_internal)
		get_iunknown_for_object_internal = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetIUnknownForObjectInternal", 1);
	if (!get_idispatch_for_object_internal)
		get_idispatch_for_object_internal = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetIDispatchForObjectInternal", 1);
	if (!get_com_interface_for_object_internal)
		get_com_interface_for_object_internal = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetComInterfaceForObjectInternal", 2);
	if (!marshal_release)
		marshal_release = mono_class_get_method_from_name (mono_defaults.marshal_class, "Release", 1);

	switch (action) {
	case MARSHAL_ACTION_CONV_IN: {
		guint32 pos_null = 0;

		*conv_arg_type = &mono_defaults.int_class->byval_arg;
		conv_arg = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

		mono_mb_emit_ptr (mb, NULL);
		mono_mb_emit_stloc (mb, conv_arg);	

		/* we dont need any conversions for out parameters */
		if (t->byref && t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		mono_mb_emit_ldarg (mb, argnum);	
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		/* if null just break, conv arg was already inited to 0 */
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
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
		if (t->byref && (t->attrs & PARAM_ATTRIBUTE_OUT)) {
			int ccw_obj;
			guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
			ccw_obj = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

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
		if (t->byref)
			mono_mb_emit_ldloc_addr (mb, conv_arg);
		else
			mono_mb_emit_ldloc (mb, conv_arg);
		break;

	case MARSHAL_ACTION_CONV_RESULT: {
		int ccw_obj, ret_ptr;
		guint32 pos_null = 0, pos_ccw = 0, pos_end = 0;
		ccw_obj = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
		ret_ptr = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

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
		ccw_obj = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		klass = mono_class_from_mono_type (t);
		conv_arg = mono_mb_add_local (mb, &klass->byval_arg);
		*conv_arg_type = &mono_defaults.int_class->byval_arg;

		mono_mb_emit_byte (mb, CEE_LDNULL);
		mono_mb_emit_stloc (mb, conv_arg);
		if (t->attrs & PARAM_ATTRIBUTE_OUT)
			break;

		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		pos_null = mono_mb_emit_short_branch (mb, CEE_BRFALSE_S);

		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
			mono_mb_emit_byte (mb, CEE_LDIND_REF);
		mono_mb_emit_icon (mb, TRUE);
		mono_mb_emit_icall (mb, cominterop_get_ccw_object);
		mono_mb_emit_stloc (mb, ccw_obj);
		mono_mb_emit_ldloc (mb, ccw_obj);
		pos_ccw = mono_mb_emit_short_branch (mb, CEE_BRTRUE_S);


		mono_mb_emit_ldarg (mb, argnum);
		if (t->byref)
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
		if (t->byref && t->attrs & PARAM_ATTRIBUTE_OUT) {
			guint32 pos_null = 0;

			if (!AddRef)
				AddRef = mono_class_get_method_from_name (mono_defaults.marshal_class, "AddRef", 1);

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
			mono_mb_emit_managed_call (mb, AddRef, NULL);
			mono_mb_emit_byte (mb, CEE_POP);

			mono_mb_patch_short_branch (mb, pos_null);
		}
		break;
	}

	case MARSHAL_ACTION_MANAGED_CONV_RESULT: {
		guint32 pos_null = 0;
		int ccw_obj;
		ccw_obj = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

		if (!AddRef)
			AddRef = mono_class_get_method_from_name (mono_defaults.marshal_class, "AddRef", 1);

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
		
		mono_mb_emit_managed_call (mb, AddRef, NULL);
		mono_mb_emit_byte (mb, CEE_POP);

		mono_mb_patch_short_branch (mb, pos_null);
		break;
	}

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

typedef struct
{
	int (STDCALL *QueryInterface)(gpointer pUnk, gpointer riid, gpointer* ppv);
	int (STDCALL *AddRef)(gpointer pUnk);
	int (STDCALL *Release)(gpointer pUnk);
} MonoIUnknown;

#define MONO_S_OK 0x00000000L
#define MONO_E_NOINTERFACE 0x80004002L
#define MONO_E_NOTIMPL 0x80004001L
#define MONO_E_INVALIDARG          0x80070057L
#define MONO_E_DISP_E_UNKNOWNNAME  0x80020006L
#define MONO_E_DISPID_UNKNOWN      (gint32)-1

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (gpointer pUnk)
{
	g_assert (pUnk);
	return (*(MonoIUnknown**)pUnk)->AddRef(pUnk);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (gpointer pUnk, gpointer riid, gpointer* ppv)
{
	g_assert (pUnk);
	return (*(MonoIUnknown**)pUnk)->QueryInterface(pUnk, riid, ppv);
}

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (gpointer pUnk)
{
	g_assert (pUnk);
	return (*(MonoIUnknown**)pUnk)->Release(pUnk);
}

static gboolean cominterop_can_support_dispatch (MonoClass* klass)
{
	if (!(klass->flags & TYPE_ATTRIBUTE_PUBLIC) )
		return FALSE;

	if (!cominterop_com_visible (klass))
		return FALSE;

	return TRUE;
}

static void*
cominterop_get_idispatch_for_object (MonoObject* object)
{
	if (!object)
		return NULL;

	if (cominterop_object_is_rcw (object)) {
		return cominterop_get_interface (((MonoComInteropProxy*)((MonoTransparentProxy*)object)->rp)->com_object, 
			mono_class_get_idispatch_class (), TRUE);
	}
	else {
		MonoClass* klass = mono_object_class (object);
		if (!cominterop_can_support_dispatch (klass) )
			cominterop_raise_hr_exception (MONO_E_NOINTERFACE);
		return cominterop_get_ccw (object, mono_class_get_idispatch_class ());
	}
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal (MonoObject* object)
{
#ifndef DISABLE_COM
	if (!object)
		return NULL;

	if (cominterop_object_is_rcw (object)) {
		MonoClass *klass = NULL;
		MonoRealProxy* real_proxy = NULL;
		if (!object)
			return NULL;
		klass = mono_object_class (object);
		if (!mono_class_is_transparent_proxy (klass)) {
			g_assert_not_reached ();
			return NULL;
		}

		real_proxy = ((MonoTransparentProxy*)object)->rp;
		if (!real_proxy) {
			g_assert_not_reached ();
			return NULL;
		}

		klass = mono_object_class (real_proxy);
		if (klass != mono_class_get_interop_proxy_class ()) {
			g_assert_not_reached ();
			return NULL;
		}

		if (!((MonoComInteropProxy*)real_proxy)->com_object) {
			g_assert_not_reached ();
			return NULL;
		}

		return ((MonoComInteropProxy*)real_proxy)->com_object->iunknown;
	}
	else {
		return cominterop_get_ccw (object, mono_class_get_iunknown_class ());
	}
#else
	g_assert_not_reached ();
#endif
}

MonoObject*
ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW (void* pUnk)
{
#ifndef DISABLE_COM
	MonoObject* object = NULL;

	if (!pUnk)
		return NULL;

	/* see if it is a CCW */
	object = cominterop_get_ccw_object ((MonoCCWInterface*)pUnk, TRUE);

	return object;
#else
	g_assert_not_reached ();
#endif
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIDispatchForObjectInternal (MonoObject* object)
{
#ifndef DISABLE_COM
	return cominterop_get_idispatch_for_object (object);
#else
	g_assert_not_reached ();
#endif
}

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetCCW (MonoObject* object, MonoReflectionType* type)
{
#ifndef DISABLE_COM
	MonoClass* klass = NULL;
	void* itf = NULL;
	g_assert (type);
	g_assert (type->type);
	klass = mono_type_get_class (type->type);
	g_assert (klass);
	if (!mono_class_init (klass)) {
		mono_set_pending_exception (mono_class_get_exception_for_failure (klass));
		return NULL;
	}

	itf = cominterop_get_ccw (object, klass);
	g_assert (itf);
	return itf;
#else
	g_assert_not_reached ();
#endif
}


MonoBoolean
ves_icall_System_Runtime_InteropServices_Marshal_IsComObject (MonoObject* object)
{
#ifndef DISABLE_COM
	return (MonoBoolean)cominterop_object_is_rcw (object);
#else
	g_assert_not_reached ();
#endif
}

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal (MonoObject* object)
{
#ifndef DISABLE_COM
	MonoComInteropProxy* proxy = NULL;
	gint32 ref_count = 0;

	g_assert (object);
	g_assert (cominterop_object_is_rcw (object));

	proxy = (MonoComInteropProxy*)((MonoTransparentProxy*)object)->rp;
	g_assert (proxy);

	if (proxy->ref_count == 0)
		return -1;

	ref_count = InterlockedDecrement (&proxy->ref_count);

	g_assert (ref_count >= 0);

	if (ref_count == 0)
		ves_icall_System_ComObject_ReleaseInterfaces (proxy->com_object);

	return ref_count;
#else
	g_assert_not_reached ();
#endif
}

guint32
ves_icall_System_Runtime_InteropServices_Marshal_GetComSlotForMethodInfoInternal (MonoReflectionMethod *m)
{
#ifndef DISABLE_COM
	return cominterop_get_com_slot_for_method (m->method);
#else
	g_assert_not_reached ();
#endif
}

/* Only used for COM RCWs */
MonoObject *
ves_icall_System_ComObject_CreateRCW (MonoReflectionType *type)
{
	MonoClass *klass;
	MonoDomain *domain;
	MonoObject *obj;
	
	domain = mono_object_domain (type);
	klass = mono_class_from_mono_type (type->type);

	/* call mono_object_new_alloc_specific instead of mono_object_new
	 * because we want to actually create object. mono_object_new checks
	 * to see if type is import and creates transparent proxy. this method
	 * is called by the corresponding real proxy to create the real RCW.
	 * Constructor does not need to be called. Will be called later.
	*/
	obj = mono_object_new_alloc_specific (mono_class_vtable_full (domain, klass, TRUE));
	return obj;
}

static gboolean    
cominterop_rcw_interface_finalizer (gpointer key, gpointer value, gpointer user_data)
{
	ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (value);
	return TRUE;
}

void
ves_icall_System_ComObject_ReleaseInterfaces (MonoComObject* obj)
{
	g_assert(obj);
	if (obj->itf_hash) {
		guint32 gchandle = 0;
		mono_cominterop_lock ();
		gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (rcw_hash, obj->iunknown));
		if (gchandle) {
			mono_gchandle_free (gchandle);
			g_hash_table_remove (rcw_hash, obj->iunknown);
		}

		g_hash_table_foreach_remove (obj->itf_hash, cominterop_rcw_interface_finalizer, NULL);
		g_hash_table_destroy (obj->itf_hash);
		ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (obj->iunknown);
		obj->itf_hash = obj->iunknown = NULL;
		mono_cominterop_unlock ();
	}
}

static gboolean    
cominterop_rcw_finalizer (gpointer key, gpointer value, gpointer user_data)
{
	guint32 gchandle = 0;

	gchandle = GPOINTER_TO_UINT (value);
	if (gchandle) {
		MonoComInteropProxy* proxy = (MonoComInteropProxy*)mono_gchandle_get_target (gchandle);
		
		if (proxy) {
			if (proxy->com_object->itf_hash) {
				g_hash_table_foreach_remove (proxy->com_object->itf_hash, cominterop_rcw_interface_finalizer, NULL);
				g_hash_table_destroy (proxy->com_object->itf_hash);
			}
			if (proxy->com_object->iunknown)
				ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (proxy->com_object->iunknown);
			proxy->com_object->itf_hash = proxy->com_object->iunknown = NULL;
		}
		
		mono_gchandle_free (gchandle);
	}

	return TRUE;
}

void
cominterop_release_all_rcws (void)
{
	if (!rcw_hash)
		return;

	mono_cominterop_lock ();

	g_hash_table_foreach_remove (rcw_hash, cominterop_rcw_finalizer, NULL);
	g_hash_table_destroy (rcw_hash);
	rcw_hash = NULL;

	mono_cominterop_unlock ();
}

gpointer
ves_icall_System_ComObject_GetInterfaceInternal (MonoComObject* obj, MonoReflectionType* type, MonoBoolean throw_exception)
{
#ifndef DISABLE_COM
	MonoClass *class = mono_type_get_class (type->type);
	if (!mono_class_init (class)) {
		mono_set_pending_exception (mono_class_get_exception_for_failure (class));
		return NULL;
	}

	return cominterop_get_interface (obj, class, (gboolean)throw_exception);
#else
	g_assert_not_reached ();
#endif
}

void
ves_icall_Mono_Interop_ComInteropProxy_AddProxy (gpointer pUnk, MonoComInteropProxy* proxy)
{
#ifndef DISABLE_COM
	guint32 gchandle = 0;
	if (!rcw_hash) {
		mono_cominterop_lock ();
		rcw_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
		mono_cominterop_unlock ();
	}

	gchandle = mono_gchandle_new_weakref ((MonoObject*)proxy, FALSE);

	mono_cominterop_lock ();
	g_hash_table_insert (rcw_hash, pUnk, GUINT_TO_POINTER (gchandle));
	mono_cominterop_unlock ();
#else
	g_assert_not_reached ();
#endif
}

MonoComInteropProxy*
ves_icall_Mono_Interop_ComInteropProxy_FindProxy (gpointer pUnk)
{
#ifndef DISABLE_COM
	MonoComInteropProxy* proxy = NULL;
	guint32 gchandle = 0;

	mono_cominterop_lock ();
	if (rcw_hash)
		gchandle = GPOINTER_TO_UINT (g_hash_table_lookup (rcw_hash, pUnk));
	mono_cominterop_unlock ();
	if (gchandle) {
		proxy = (MonoComInteropProxy*)mono_gchandle_get_target (gchandle);
		/* proxy is null means we need to free up old RCW */
		if (!proxy) {
			mono_gchandle_free (gchandle);
			g_hash_table_remove (rcw_hash, pUnk);
		}
	}
	return proxy;
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
static MonoObject*
cominterop_get_ccw_object (MonoCCWInterface* ccw_entry, gboolean verify)
{
	MonoCCW *ccw = NULL;

	/* no CCW's exist yet */
	if (!ccw_interface_hash)
		return NULL;

	if (verify) {
		ccw = g_hash_table_lookup (ccw_interface_hash, ccw_entry);
	}
	else {
		ccw = ccw_entry->ccw;
		g_assert (ccw);
	}
	if (ccw)
		return mono_gchandle_get_target (ccw->gc_handle);
	else
		return NULL;
}

static void
cominterop_setup_marshal_context (EmitMarshalContext *m, MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	sig = mono_method_signature (method);
	/* we copy the signature, so that we can modify it */
	/* FIXME: which to use? */
	csig = mono_metadata_signature_dup_full (method->klass->image, sig);
	/* csig = mono_metadata_signature_dup (sig); */
	
	/* STDCALL on windows, CDECL everywhere else to work with XPCOM and MainWin COM */
#ifdef HOST_WIN32
	csig->call_convention = MONO_CALL_STDCALL;
#else
	csig->call_convention = MONO_CALL_C;
#endif
	csig->hasthis = 0;
	csig->pinvoke = 1;

	m->image = method->klass->image;
	m->piinfo = NULL;
	m->retobj_var = 0;
	m->sig = sig;
	m->csig = csig;
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
cominterop_get_ccw (MonoObject* object, MonoClass* itf)
{
	int i;
	MonoCCW *ccw = NULL;
	MonoCCWInterface* ccw_entry = NULL;
	gpointer *vtable = NULL;
	static gpointer iunknown[3] = {NULL, NULL, NULL};
	static gpointer idispatch[4] = {NULL, NULL, NULL, NULL};
	MonoClass* iface = NULL;
	MonoClass* klass = NULL;
	EmitMarshalContext m;
	int start_slot = 3;
	int method_count = 0;
	GList *ccw_list, *ccw_list_item;
	MonoCustomAttrInfo *cinfo = NULL;

	if (!object)
		return NULL;

	klass = mono_object_get_class (object);

	mono_cominterop_lock ();
	if (!ccw_hash)
		ccw_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
	if (!ccw_interface_hash)
		ccw_interface_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);

	ccw_list = g_hash_table_lookup (ccw_hash, GINT_TO_POINTER (mono_object_hash (object)));
	mono_cominterop_unlock ();

	ccw_list_item = ccw_list;
	while (ccw_list_item) {
		MonoCCW* ccw_iter = ccw_list_item->data;
		if (mono_gchandle_get_target (ccw_iter->gc_handle) == object) {
			ccw = ccw_iter;
			break;
		}
		ccw_list_item = g_list_next(ccw_list_item);
	}

	if (!iunknown [0]) {
		iunknown [0] = cominterop_ccw_queryinterface;
		iunknown [1] = cominterop_ccw_addref;
		iunknown [2] = cominterop_ccw_release;
	}

	if (!idispatch [0]) {
		idispatch [0] = cominterop_ccw_get_type_info_count;
		idispatch [1] = cominterop_ccw_get_type_info;
		idispatch [2] = cominterop_ccw_get_ids_of_names;
		idispatch [3] = cominterop_ccw_invoke;
	}

	if (!ccw) {
		ccw = g_new0 (MonoCCW, 1);
#ifdef HOST_WIN32
		ccw->free_marshaler = 0;
#endif
		ccw->vtable_hash = g_hash_table_new (mono_aligned_addr_hash, NULL);
		ccw->ref_count = 0;
		/* just alloc a weak handle until we are addref'd*/
		ccw->gc_handle = mono_gchandle_new_weakref (object, FALSE);

		if (!ccw_list) {
			ccw_list = g_list_alloc ();
			ccw_list->data = ccw;
		}
		else
			ccw_list = g_list_append (ccw_list, ccw);
		mono_cominterop_lock ();
		g_hash_table_insert (ccw_hash, GINT_TO_POINTER (mono_object_hash (object)), ccw_list);
		mono_cominterop_unlock ();
		/* register for finalization to clean up ccw */
		mono_object_register_finalizer (object);
	}

	cinfo = mono_custom_attrs_from_class (itf);
	if (cinfo) {
		static MonoClass* coclass_attribute = NULL;
		if (!coclass_attribute)
			coclass_attribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "CoClassAttribute");
		if (mono_custom_attrs_has_attr (cinfo, coclass_attribute)) {
			g_assert(itf->interface_count && itf->interfaces[0]);
			itf = itf->interfaces[0];
		}
		if (!cinfo->cached)
			mono_custom_attrs_free (cinfo);
	}

	iface = itf;
	if (iface == mono_class_get_iunknown_class ()) {
		start_slot = 3;
	}
	else if (iface == mono_class_get_idispatch_class ()) {
		start_slot = 7;
	}
	else {
		method_count += iface->method.count;
		start_slot = cominterop_get_com_slot_begin (iface);
		iface = NULL;
	}

	ccw_entry = g_hash_table_lookup (ccw->vtable_hash, itf);

	if (!ccw_entry) {
		int vtable_index = method_count-1+start_slot;
		vtable = mono_image_alloc0 (klass->image, sizeof (gpointer)*(method_count+start_slot));
		memcpy (vtable, iunknown, sizeof (iunknown));
		if (start_slot == 7)
			memcpy (vtable+3, idispatch, sizeof (idispatch));

		iface = itf;
		for (i = iface->method.count-1; i >= 0;i--) {
			int param_index = 0;
			MonoMethodBuilder *mb;
			MonoMarshalSpec ** mspecs;
			MonoMethod *wrapper_method, *adjust_method;
			MonoMethod *method = iface->methods [i];
			MonoMethodSignature* sig_adjusted;
			MonoMethodSignature* sig = mono_method_signature (method);
			gboolean preserve_sig = method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG;


			mb = mono_mb_new (iface, method->name, MONO_WRAPPER_NATIVE_TO_MANAGED);
			adjust_method = cominterop_get_managed_wrapper_adjusted (method);
			sig_adjusted = mono_method_signature (adjust_method);
			
			mspecs = g_new (MonoMarshalSpec*, sig_adjusted->param_count + 1);
			mono_method_get_marshal_info (method, mspecs);

			
			/* move managed args up one */
			for (param_index = sig->param_count; param_index >= 1; param_index--) {
				int mspec_index = param_index+1;
				mspecs [mspec_index] = mspecs [param_index];

				if (mspecs[mspec_index] == NULL) {
					if (sig_adjusted->params[param_index]->type == MONO_TYPE_OBJECT) {
						mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
						mspecs[mspec_index]->native = MONO_NATIVE_STRUCT;
					}
					else if (sig_adjusted->params[param_index]->type == MONO_TYPE_STRING) {
						mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
						mspecs[mspec_index]->native = MONO_NATIVE_BSTR;
					}
					else if (sig_adjusted->params[param_index]->type == MONO_TYPE_CLASS) {
						mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
						mspecs[mspec_index]->native = MONO_NATIVE_INTERFACE;
					}
					else if (sig_adjusted->params[param_index]->type == MONO_TYPE_BOOLEAN) {
						mspecs[mspec_index] = g_new0 (MonoMarshalSpec, 1);
						mspecs[mspec_index]->native = MONO_NATIVE_VARIANTBOOL;
					}
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
				if (mspecs [0] == NULL) {
					if (sig_adjusted->params[sig_adjusted->param_count-1]->type == MONO_TYPE_OBJECT) {
						mspecs[0] = g_new0 (MonoMarshalSpec, 1);
						mspecs[0]->native = MONO_NATIVE_STRUCT;
					}
					else if (sig_adjusted->params[sig_adjusted->param_count-1]->type == MONO_TYPE_STRING) {
						mspecs[0] = g_new0 (MonoMarshalSpec, 1);
						mspecs[0]->native = MONO_NATIVE_BSTR;
					}
					else if (sig_adjusted->params[sig_adjusted->param_count-1]->type == MONO_TYPE_CLASS) {
						mspecs[0] = g_new0 (MonoMarshalSpec, 1);
						mspecs[0]->native = MONO_NATIVE_INTERFACE;
					}
					else if (sig_adjusted->params[sig_adjusted->param_count-1]->type == MONO_TYPE_BOOLEAN) {
						mspecs[0] = g_new0 (MonoMarshalSpec, 1);
						mspecs[0]->native = MONO_NATIVE_VARIANTBOOL;
					}
				}

				mspecs [sig_adjusted->param_count] = mspecs [0];
				mspecs [0] = NULL;
			}

			/* skip visiblity since we call internal methods */
			mb->skip_visibility = TRUE;

			cominterop_setup_marshal_context (&m, adjust_method);
			m.mb = mb;
			mono_marshal_emit_managed_wrapper (mb, sig_adjusted, mspecs, &m, adjust_method, 0);
			mono_cominterop_lock ();
			wrapper_method = mono_mb_create_method (mb, m.csig, m.csig->param_count + 16);
			mono_cominterop_unlock ();

			vtable [vtable_index--] = mono_compile_method (wrapper_method);

			
			for (param_index = sig_adjusted->param_count; param_index >= 0; param_index--)
				if (mspecs [param_index])
					mono_metadata_free_marshal_spec (mspecs [param_index]);
			g_free (mspecs);
		}

		ccw_entry = g_new0 (MonoCCWInterface, 1);
		ccw_entry->ccw = ccw;
		ccw_entry->vtable = vtable;
		g_hash_table_insert (ccw->vtable_hash, itf, ccw_entry);
		g_hash_table_insert (ccw_interface_hash, ccw_entry, ccw);
	}

	return ccw_entry;
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
 * @object: the mono object
 *
 * Returns: whether the object had a CCW
 */
gboolean
mono_marshal_free_ccw (MonoObject* object)
{
	GList *ccw_list, *ccw_list_orig, *ccw_list_item;
	/* no ccw's were created */
	if (!ccw_hash || g_hash_table_size (ccw_hash) == 0)
		return FALSE;

	/* need to cache orig list address to remove from hash_table if empty */
	mono_cominterop_lock ();
	ccw_list = ccw_list_orig = g_hash_table_lookup (ccw_hash, GINT_TO_POINTER (mono_object_hash (object)));
	mono_cominterop_unlock ();

	if (!ccw_list)
		return FALSE;

	ccw_list_item = ccw_list;
	while (ccw_list_item) {
		MonoCCW* ccw_iter = ccw_list_item->data;
		MonoObject* handle_target = mono_gchandle_get_target (ccw_iter->gc_handle);

		/* Looks like the GC NULLs the weakref handle target before running the
		 * finalizer. So if we get a NULL target, destroy the CCW as well.
		 * Unless looking up the object from the CCW shows it not the right object.
		*/
		gboolean destroy_ccw = !handle_target || handle_target == object;
		if (!handle_target) {
			MonoCCWInterface* ccw_entry = g_hash_table_lookup (ccw_iter->vtable_hash, mono_class_get_iunknown_class ());
			if (!(ccw_entry && object == cominterop_get_ccw_object (ccw_entry, FALSE)))
				destroy_ccw = FALSE;
		}

		if (destroy_ccw) {
			/* remove all interfaces */
			g_hash_table_foreach_remove (ccw_iter->vtable_hash, mono_marshal_free_ccw_entry, NULL);
			g_hash_table_destroy (ccw_iter->vtable_hash);

			/* get next before we delete */
			ccw_list_item = g_list_next(ccw_list_item);

			/* remove ccw from list */
			ccw_list = g_list_remove (ccw_list, ccw_iter);

#ifdef HOST_WIN32
			if (ccw_iter->free_marshaler)
				ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (ccw_iter->free_marshaler);
#endif

			g_free (ccw_iter);
		}
		else
			ccw_list_item = g_list_next (ccw_list_item);
	}

	/* if list is empty remove original address from hash */
	if (g_list_length (ccw_list) == 0)
		g_hash_table_remove (ccw_hash, GINT_TO_POINTER (mono_object_hash (object)));
	else if (ccw_list != ccw_list_orig)
		g_hash_table_insert (ccw_hash, GINT_TO_POINTER (mono_object_hash (object)), ccw_list);

	return TRUE;
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
	static MonoMethod *get_hr_for_exception = NULL;
	MonoMethod *res = NULL;
	MonoMethodBuilder *mb;
	MonoMarshalSpec **mspecs;
	MonoMethodSignature *sig, *sig_native;
	MonoExceptionClause *main_clause = NULL;
	int pos_leave;
	int hr = 0;
	int i;
	gboolean preserve_sig = method->iflags & METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG;

	if (!get_hr_for_exception)
		get_hr_for_exception = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetHRForException", -1);

	sig = mono_method_signature (method);

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

	if (!preserve_sig) {
		hr = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
	}
	else if (!MONO_TYPE_IS_VOID (sig->ret))
		hr = mono_mb_add_local (mb, sig->ret);

	/* try */
	main_clause = g_new0 (MonoExceptionClause, 1);
	main_clause->try_offset = mono_mb_get_label (mb);

	/* load last param to store result if not preserve_sig and not void */
	if (!preserve_sig && !MONO_TYPE_IS_VOID (sig->ret))
		mono_mb_emit_ldarg (mb, sig_native->param_count-1);

	/* the CCW -> object conversion */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_icon (mb, FALSE);
	mono_mb_emit_icall (mb, cominterop_get_ccw_object);

	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i+1);

	mono_mb_emit_managed_call (mb, method, NULL);

	if (!MONO_TYPE_IS_VOID (sig->ret)) {
		if (!preserve_sig) {
			MonoClass *rclass = mono_class_from_mono_type (sig->ret);
			if (rclass->valuetype) {
				mono_mb_emit_op (mb, CEE_STOBJ, rclass);
			} else {
				mono_mb_emit_byte (mb, mono_type_to_stind (sig->ret));
			}
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
	
	if (!preserve_sig || (sig->ret && !sig->ret->byref && (sig->ret->type == MONO_TYPE_U4 || sig->ret->type == MONO_TYPE_I4))) {
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

	mono_mb_emit_byte (mb, CEE_RET);

	mono_cominterop_lock ();
	res = mono_mb_create_method (mb, sig_native, sig_native->param_count + 16);	
	mono_cominterop_unlock ();

	mono_mb_free (mb);

	for (i = sig_native->param_count; i >= 0; i--)
		if (mspecs [i])
			mono_metadata_free_marshal_spec (mspecs [i]);
	g_free (mspecs);

	return res;
}

/**
 * cominterop_mono_string_to_guid:
 *
 * Converts the standard string representation of a GUID 
 * to a 16 byte Microsoft GUID.
 */
static void
cominterop_mono_string_to_guid (MonoString* string, guint8 *guid) {
	gunichar2 * chars = mono_string_chars (string);
	int i = 0;
	static guint8 indexes[16] = {7, 5, 3, 1, 12, 10, 17, 15, 20, 22, 25, 27, 29, 31, 33, 35};

	for (i = 0; i < sizeof(indexes); i++)
		guid [i] = g_unichar_xdigit_value (chars [indexes [i]]) + (g_unichar_xdigit_value (chars [indexes [i] - 1]) << 4);
}

static gboolean
cominterop_class_guid_equal (guint8* guid, MonoClass* klass)
{
	guint8 klass_guid [16];
	if (cominterop_class_guid (klass, klass_guid))
		return !memcmp (guid, klass_guid, sizeof (klass_guid));
	return FALSE;
}

static int STDCALL 
cominterop_ccw_addref (MonoCCWInterface* ccwe)
{
	gint32 ref_count = 0;
	MonoCCW* ccw = ccwe->ccw;
	g_assert (ccw);
	g_assert (ccw->gc_handle);
	ref_count = InterlockedIncrement ((gint32*)&ccw->ref_count);
	if (ref_count == 1) {
		guint32 oldhandle = ccw->gc_handle;
		g_assert (oldhandle);
		/* since we now have a ref count, alloc a strong handle*/
		ccw->gc_handle = mono_gchandle_new (mono_gchandle_get_target (oldhandle), FALSE);
		mono_gchandle_free (oldhandle);
	}
	return ref_count;
}

static int STDCALL 
cominterop_ccw_release (MonoCCWInterface* ccwe)
{
	gint32 ref_count = 0;
	MonoCCW* ccw = ccwe->ccw;
	g_assert (ccw);
	g_assert (ccw->ref_count > 0);
	ref_count = InterlockedDecrement ((gint32*)&ccw->ref_count);
	if (ref_count == 0) {
		/* allow gc of object */
		guint32 oldhandle = ccw->gc_handle;
		g_assert (oldhandle);
		ccw->gc_handle = mono_gchandle_new_weakref (mono_gchandle_get_target (oldhandle), FALSE);
		mono_gchandle_free (oldhandle);
	}
	return ref_count;
}

#ifdef HOST_WIN32
static const IID MONO_IID_IMarshal = {0x3, 0x0, 0x0, {0xC0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x46}};
#endif

#ifdef HOST_WIN32
/* All ccw objects are free threaded */
static int
cominterop_ccw_getfreethreadedmarshaler (MonoCCW* ccw, MonoObject* object, gpointer* ppv)
{
#ifdef HOST_WIN32
	if (!ccw->free_marshaler) {
		int ret = 0;
		gpointer tunk;
		tunk = cominterop_get_ccw (object, mono_class_get_iunknown_class ());
		ret = CoCreateFreeThreadedMarshaler (tunk, (LPUNKNOWN*)&ccw->free_marshaler);
	}
		
	if (!ccw->free_marshaler)
		return MONO_E_NOINTERFACE;

	return ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (ccw->free_marshaler, (IID*)&MONO_IID_IMarshal, ppv);
#else
	return MONO_E_NOINTERFACE;
#endif
}
#endif

static int STDCALL 
cominterop_ccw_queryinterface (MonoCCWInterface* ccwe, guint8* riid, gpointer* ppv)
{
	MonoError error;
	GPtrArray *ifaces;
	MonoClass *itf = NULL;
	int i;
	MonoCCW* ccw = ccwe->ccw;
	MonoClass* klass = NULL;
	MonoClass* klass_iter = NULL;
	MonoObject* object = mono_gchandle_get_target (ccw->gc_handle);
	
	g_assert (object);
	klass = mono_object_class (object);

	if (ppv)
		*ppv = NULL;

	if (!mono_domain_get ())
		mono_thread_attach (mono_get_root_domain ());

	/* handle IUnknown special */
	if (cominterop_class_guid_equal (riid, mono_class_get_iunknown_class ())) {
		*ppv = cominterop_get_ccw (object, mono_class_get_iunknown_class ());
		/* remember to addref on QI */
		cominterop_ccw_addref (*ppv);
		return MONO_S_OK;
	}

	/* handle IDispatch special */
	if (cominterop_class_guid_equal (riid, mono_class_get_idispatch_class ())) {
		if (!cominterop_can_support_dispatch (klass))
			return MONO_E_NOINTERFACE;
		
		*ppv = cominterop_get_ccw (object, mono_class_get_idispatch_class ());
		/* remember to addref on QI */
		cominterop_ccw_addref (*ppv);
		return MONO_S_OK;
	}

#ifdef HOST_WIN32
	/* handle IMarshal special */
	if (0 == memcmp (riid, &MONO_IID_IMarshal, sizeof (IID))) {
		return cominterop_ccw_getfreethreadedmarshaler (ccw, object, ppv);	
	}
#endif
	klass_iter = klass;
	while (klass_iter && klass_iter != mono_defaults.object_class) {
		ifaces = mono_class_get_implemented_interfaces (klass_iter, &error);
		g_assert (mono_error_ok (&error));
		if (ifaces) {
			for (i = 0; i < ifaces->len; ++i) {
				MonoClass *ic = NULL;
				ic = g_ptr_array_index (ifaces, i);
				if (cominterop_class_guid_equal (riid, ic)) {
					itf = ic;
					break;
				}
			}
			g_ptr_array_free (ifaces, TRUE);
		}

		if (itf)
			break;

		klass_iter = klass_iter->parent;
	}
	if (itf) {
		*ppv = cominterop_get_ccw (object, itf);
		/* remember to addref on QI */
		cominterop_ccw_addref (*ppv);
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
cominterop_ccw_get_ids_of_names (MonoCCWInterface* ccwe, gpointer riid,
											 gunichar2** rgszNames, guint32 cNames,
											 guint32 lcid, gint32 *rgDispId)
{
	static MonoClass *ComDispIdAttribute = NULL;
	MonoCustomAttrInfo *cinfo = NULL;
	int i,ret = MONO_S_OK;
	MonoMethod* method;
	gchar* methodname;
	MonoClass *klass = NULL;
	MonoCCW* ccw = ccwe->ccw;
	MonoObject* object = mono_gchandle_get_target (ccw->gc_handle);

	/* Handle DispIdAttribute */
	if (!ComDispIdAttribute)
		ComDispIdAttribute = mono_class_from_name (mono_defaults.corlib, "System.Runtime.InteropServices", "DispIdAttribute");

	g_assert (object);
	klass = mono_object_class (object);

	if (!mono_domain_get ())
		 mono_thread_attach (mono_get_root_domain ());

	for (i=0; i < cNames; i++) {
		methodname = mono_unicode_to_external (rgszNames[i]);

		method = mono_class_get_method_from_name(klass, methodname, -1);
		if (method) {
			cinfo = mono_custom_attrs_from_method (method);
			if (cinfo) {
				MonoError error;
				MonoObject *result = mono_custom_attrs_get_attr_checked (cinfo, ComDispIdAttribute, &error);
				g_assert (mono_error_ok (&error)); /*FIXME proper error handling*/;

				if (result)
					rgDispId[i] = *(gint32*)mono_object_unbox (result);
				else
					rgDispId[i] = (gint32)method->token;

				if (!cinfo->cached)
					mono_custom_attrs_free (cinfo);
			}
			else
				rgDispId[i] = (gint32)method->token;
		} else {
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

typedef gpointer (STDCALL *SysAllocStringLenFunc)(gunichar* str, guint32 len);
typedef guint32 (STDCALL *SysStringLenFunc)(gpointer bstr);
typedef void (STDCALL *SysFreeStringFunc)(gunichar* str);

static SysAllocStringLenFunc sys_alloc_string_len_ms = NULL;
static SysStringLenFunc sys_string_len_ms = NULL;
static SysFreeStringFunc sys_free_string_ms = NULL;

#ifndef HOST_WIN32

typedef struct tagSAFEARRAYBOUND {
	ULONG cElements;
	LONG lLbound;
}SAFEARRAYBOUND,*LPSAFEARRAYBOUND;
#define VT_VARIANT 12

#endif 

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
	static gboolean initialized = FALSE;
	char *error_msg;
	MonoDl *module = NULL;
	const char* scope = "liboleaut32.so";

	if (initialized)
		return TRUE;

	module = mono_dl_open(scope, MONO_DL_LAZY, &error_msg);
	if (error_msg) {
		g_warning ("Error loading COM support library '%s': %s", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}
	error_msg = mono_dl_symbol (module, "SysAllocStringLen", (gpointer*)&sys_alloc_string_len_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SysAllocStringLen", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SysStringLen", (gpointer*)&sys_string_len_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SysStringLen", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SysFreeString", (gpointer*)&sys_free_string_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SysFreeString", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayGetDim", (gpointer*)&safe_array_get_dim_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayGetDim", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayGetLBound", (gpointer*)&safe_array_get_lbound_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayGetLBound", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayGetUBound", (gpointer*)&safe_array_get_ubound_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayGetUBound", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayPtrOfIndex", (gpointer*)&safe_array_ptr_of_index_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayPtrOfIndex", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayDestroy", (gpointer*)&safe_array_destroy_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayDestroy", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayPutElement", (gpointer*)&safe_array_put_element_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayPutElement", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	error_msg = mono_dl_symbol (module, "SafeArrayCreate", (gpointer*)&safe_array_create_ms);
	if (error_msg) {
		g_warning ("Error loading entry point '%s' in COM support library '%s': %s", "SafeArrayCreate", scope, error_msg);
		g_assert_not_reached ();
		return FALSE;
	}

	initialized = TRUE;
	return TRUE;
}

gpointer
mono_string_to_bstr (MonoString *string_obj)
{
	if (!string_obj)
		return NULL;
#ifdef HOST_WIN32
	return SysAllocStringLen (mono_string_chars (string_obj), mono_string_length (string_obj));
#else
	if (com_provider == MONO_COM_DEFAULT) {
		int slen = mono_string_length (string_obj);
		/* allocate len + 1 utf16 characters plus 4 byte integer for length*/
		char *ret = g_malloc ((slen + 1) * sizeof(gunichar2) + sizeof(guint32));
		if (ret == NULL)
			return NULL;
		memcpy (ret + sizeof(guint32), mono_string_chars (string_obj), slen * sizeof(gunichar2));
		* ((guint32 *) ret) = slen * sizeof(gunichar2);
		ret [4 + slen * sizeof(gunichar2)] = 0;
		ret [5 + slen * sizeof(gunichar2)] = 0;

		return ret + 4;
	} else if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		gpointer ret = NULL;
		gunichar* str = NULL;
		guint32 len;
		len = mono_string_length (string_obj);
		str = g_utf16_to_ucs4 (mono_string_chars (string_obj), len,
			NULL, NULL, NULL);
		ret = sys_alloc_string_len_ms (str, len);
		g_free(str);
		return ret;
	} else {
		g_assert_not_reached ();
	}
#endif
}

MonoString *
mono_string_from_bstr (gpointer bstr)
{
	if (!bstr)
		return NULL;
#ifdef HOST_WIN32
	return mono_string_new_utf16 (mono_domain_get (), bstr, SysStringLen (bstr));
#else
	if (com_provider == MONO_COM_DEFAULT) {
		return mono_string_new_utf16 (mono_domain_get (), bstr, *((guint32 *)bstr - 1) / sizeof(gunichar2));
	} else if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		MonoString* str = NULL;
		glong written = 0;
		gunichar2* utf16 = NULL;

		utf16 = g_ucs4_to_utf16 (bstr, sys_string_len_ms (bstr), NULL, &written, NULL);
		str = mono_string_new_utf16 (mono_domain_get (), utf16, written);
		g_free (utf16);
		return str;
	} else {
		g_assert_not_reached ();
	}

#endif
}

void
mono_free_bstr (gpointer bstr)
{
	if (!bstr)
		return;
#ifdef HOST_WIN32
	SysFreeString ((BSTR)bstr);
#else
	if (com_provider == MONO_COM_DEFAULT) {
		g_free (((char *)bstr) - 4);
	} else if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		sys_free_string_ms (bstr);
	} else {
		g_assert_not_reached ();
	}

#endif
}


/* SAFEARRAY marshalling */
int
mono_cominterop_emit_marshal_safearray (EmitMarshalContext *m, int argnum, MonoType *t,
										MonoMarshalSpec *spec,
										int conv_arg, MonoType **conv_arg_type,
										MarshalAction action)
{
	MonoMethodBuilder *mb = m->mb;

	switch (action) {

	case MARSHAL_ACTION_CONV_IN: {

		if (t->attrs & PARAM_ATTRIBUTE_IN) {

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
			static MonoMethod *get_native_variant_for_object = NULL;
			static MonoMethod *get_value_impl = NULL;
			static MonoMethod *variant_clear = NULL;

			conv_arg = safearray_var = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			indices_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			empty_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			if (t->byref) {
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

			index_var = mono_mb_add_local (mb, &mono_defaults.int32_class->byval_arg);
			mono_mb_emit_byte (mb, CEE_LDC_I4_0);
			mono_mb_emit_stloc (mb, index_var);

			label3 = mono_mb_get_label (mb);

			if (!get_value_impl)
				get_value_impl = mono_class_get_method_from_name (mono_defaults.array_class, "GetValueImpl", 1);
			g_assert (get_value_impl);

			if (t->byref) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDIND_REF);
			} else
				mono_mb_emit_ldarg (mb, argnum);

			mono_mb_emit_ldloc (mb, index_var);

			mono_mb_emit_managed_call (mb, get_value_impl, NULL);

			if (!get_native_variant_for_object)
				get_native_variant_for_object = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetNativeVariantForObject", 2);
			g_assert (get_native_variant_for_object);

			elem_var =  mono_mb_add_local (mb, &mono_class_get_variant_class ()->byval_arg);
			mono_mb_emit_ldloc_addr (mb, elem_var);

			mono_mb_emit_managed_call (mb, get_native_variant_for_object, NULL);

			mono_mb_emit_ldloc (mb, safearray_var);
			mono_mb_emit_ldloc (mb, indices_var);
			mono_mb_emit_ldloc_addr (mb, elem_var);
			mono_mb_emit_icall (mb, mono_marshal_safearray_set_value);

			if (!variant_clear)
				variant_clear = mono_class_get_method_from_name (mono_class_get_variant_class (), "Clear", 0);

			mono_mb_emit_ldloc_addr (mb, elem_var);
			mono_mb_emit_managed_call (mb, variant_clear, NULL);

			mono_mb_emit_add_to_local (mb, index_var, 1);

			mono_mb_emit_ldloc (mb, safearray_var);
			mono_mb_emit_ldloc (mb, indices_var);
			mono_mb_emit_icall (mb, mono_marshal_safearray_next);
			mono_mb_emit_branch_label (mb, CEE_BRTRUE, label3);

			mono_mb_patch_short_branch (mb, label2);

			mono_mb_emit_ldloc (mb, indices_var);
			mono_mb_emit_icall (mb, mono_marshal_safearray_free_indices);

			mono_mb_patch_short_branch (mb, label1);
		}
		break;
	}

	case MARSHAL_ACTION_PUSH:
		if (t->byref)
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
					bool byValue = !t->byref && (t->attrs & PARAM_ATTRIBUTE_IN);
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
			static MonoMethod *get_object_for_native_variant = NULL;
			static MonoMethod *set_value_impl = NULL;
			gboolean byValue = !t->byref && (t->attrs & PARAM_ATTRIBUTE_IN);

			result_var = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			indices_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			empty_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

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

			index_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
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

			if (!get_object_for_native_variant)
				get_object_for_native_variant = mono_class_get_method_from_name (mono_defaults.marshal_class, "GetObjectForNativeVariant", 1);
			g_assert (get_object_for_native_variant);

			if (!set_value_impl)
				set_value_impl = mono_class_get_method_from_name (mono_defaults.array_class, "SetValueImpl", 2);
			g_assert (set_value_impl);

			elem_var = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

			mono_mb_emit_managed_call (mb, get_object_for_native_variant, NULL);
			mono_mb_emit_stloc (mb, elem_var);

			mono_mb_emit_ldloc (mb, result_var);
			mono_mb_emit_ldloc (mb, elem_var);
			mono_mb_emit_ldloc (mb, index_var);
			mono_mb_emit_managed_call (mb, set_value_impl, NULL);

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

	default:
		g_assert_not_reached ();
	}

	return conv_arg;
}

static 
guint32 mono_marshal_safearray_get_dim (gpointer safearray)
{
	guint32 result=0;
#ifdef HOST_WIN32
	result = SafeArrayGetDim (safearray);
#else
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		result = safe_array_get_dim_ms (safearray);
	} else {
		g_assert_not_reached ();
	}
#endif
	return result;
}

static 
int mono_marshal_safe_array_get_lbound (gpointer psa, guint nDim, glong* plLbound)
{
	int result=MONO_S_OK;
#ifdef HOST_WIN32
	result = SafeArrayGetLBound (psa, nDim, plLbound);
#else
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		result = safe_array_get_lbound_ms (psa, nDim, plLbound);
	} else {
		g_assert_not_reached ();
	}
#endif
	return result;
}

static 
int mono_marshal_safe_array_get_ubound (gpointer psa, guint nDim, glong* plUbound)
{
	int result=MONO_S_OK;
#ifdef HOST_WIN32
	result = SafeArrayGetUBound (psa, nDim, plUbound);
#else
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		result = safe_array_get_ubound_ms (psa, nDim, plUbound);
	} else {
		g_assert_not_reached ();
	}
#endif
	return result;
}

static gboolean
mono_marshal_safearray_begin (gpointer safearray, MonoArray **result, gpointer *indices, gpointer empty, gpointer parameter, gboolean allocateNewArray)
{
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

			sizes = alloca (dim * sizeof(uintptr_t));
			bounds = alloca (dim * sizeof(intptr_t));

			for (i=0; i<dim; ++i) {
				glong lbound, ubound;
				int cursize;
				int hr;

				hr = mono_marshal_safe_array_get_lbound (safearray, i+1, &lbound);
				if (hr < 0) {
					cominterop_raise_hr_exception (hr);
				}
				if (lbound != 0)
					bounded = TRUE;
				hr = mono_marshal_safe_array_get_ubound (safearray, i+1, &ubound);
				if (hr < 0) {
					cominterop_raise_hr_exception (hr);
				}
				cursize = ubound-lbound+1;
				sizes [i] = cursize;
				bounds [i] = lbound;

				((int*)*indices) [i] = lbound;

				if (cursize != 0)
					(*(int*)empty) = FALSE;
			}

			if (allocateNewArray) {
				aklass = mono_bounded_array_class_get (mono_defaults.object_class, dim, bounded);
				*result = mono_array_new_full (mono_domain_get (), aklass, sizes, bounds);
			} else {
				*result = parameter;
			}
		}
	}
	return TRUE;
}

static 
gpointer mono_marshal_safearray_get_value (gpointer safearray, gpointer indices)
{
	gpointer result;
#ifdef HOST_WIN32
	int hr = SafeArrayPtrOfIndex (safearray, indices, &result);
	if (hr < 0) {
		cominterop_raise_hr_exception (hr);
	}
#else
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		int hr = safe_array_ptr_of_index_ms (safearray, indices, &result);
		if (hr < 0) {
			cominterop_raise_hr_exception (hr);
		}
	} else {
		g_assert_not_reached ();
	}
#endif
	return result;
}

static 
gboolean mono_marshal_safearray_next (gpointer safearray, gpointer indices)
{
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
			cominterop_raise_hr_exception (hr);
		}

		if (++pIndices[i] <= ubound) {
			break;
		}

		hr = mono_marshal_safe_array_get_lbound (safearray, i+1, &lbound);
		if (hr < 0) {
			cominterop_raise_hr_exception (hr);
		}

		pIndices[i] = lbound;

		if (i == 0)
			ret = FALSE;
	}
	return ret;
}

static 
void mono_marshal_safearray_end (gpointer safearray, gpointer indices)
{
	g_free(indices);
#ifdef HOST_WIN32
	SafeArrayDestroy (safearray);
#else
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		safe_array_destroy_ms (safearray);
	} else {
		g_assert_not_reached ();
	}
#endif
}

static gboolean
mono_marshal_safearray_create (MonoArray *input, gpointer *newsafearray, gpointer *indices, gpointer empty)
{
	int dim;
	SAFEARRAYBOUND *bounds;
	int i;
	int max_array_length;

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

	max_array_length = mono_array_length (input);
	dim = ((MonoObject *)input)->vtable->klass->rank;

	*indices = g_malloc (dim * sizeof (int));
	bounds = alloca (dim * sizeof (SAFEARRAYBOUND));
	(*(int*)empty) = (max_array_length == 0);

	if (dim > 1) {
		for (i=0; i<dim; ++i) {
			((int*)*indices) [i] = bounds [i].lLbound = input->bounds [i].lower_bound;
			bounds [i].cElements = input->bounds [i].length;
		}
	} else {
		((int*)*indices) [0] = 0;
		bounds [0].cElements = max_array_length;
		bounds [0].lLbound = 0;
	}

#ifdef HOST_WIN32
	*newsafearray = SafeArrayCreate (VT_VARIANT, dim, bounds);
#else
	*newsafearray = safe_array_create_ms (VT_VARIANT, dim, bounds);
#endif

	return TRUE;
}

static 
void mono_marshal_safearray_set_value (gpointer safearray, gpointer indices, gpointer value)
{
#ifdef HOST_WIN32
	int hr = SafeArrayPutElement (safearray, indices, value);
	if (hr < 0)
		cominterop_raise_hr_exception (hr);
#else
	if (com_provider == MONO_COM_MS && init_com_provider_ms ()) {
		int hr = safe_array_put_element_ms (safearray, indices, value);
		if (hr < 0) {
			cominterop_raise_hr_exception (hr);
		}
	} else
		g_assert_not_reached ();
#endif
}

static 
void mono_marshal_safearray_free_indices (gpointer indices)
{
	g_free (indices);
}

#else /* DISABLE_COM */

void
mono_cominterop_init (void)
{
	/*FIXME
	
	This icalls are used by the marshal code when doing PtrToStructure and StructureToPtr and pinvoke.

	If we leave them out and the FullAOT compiler finds the need to emit one of the above 3 wrappers it will
	g_assert.

	The proper fix would be to emit warning, remove them from marshal.c when DISABLE_COM is used and
	emit an exception in the generated IL.
	*/
	register_icall (mono_string_to_bstr, "mono_string_to_bstr", "ptr obj", FALSE);
	register_icall (mono_string_from_bstr, "mono_string_from_bstr", "obj ptr", FALSE);
	register_icall (mono_free_bstr, "mono_free_bstr", "void ptr", FALSE);
}

void
mono_cominterop_cleanup (void)
{
}

void
cominterop_release_all_rcws (void)
{
}

gpointer
mono_string_to_bstr (MonoString *string_obj)
{
	if (!string_obj)
		return NULL;
#ifdef HOST_WIN32
	return SysAllocStringLen (mono_string_chars (string_obj), mono_string_length (string_obj));
#else
	{
		int slen = mono_string_length (string_obj);
		/* allocate len + 1 utf16 characters plus 4 byte integer for length*/
		char *ret = g_malloc ((slen + 1) * sizeof(gunichar2) + sizeof(guint32));
		if (ret == NULL)
			return NULL;
		memcpy (ret + sizeof(guint32), mono_string_chars (string_obj), slen * sizeof(gunichar2));
		* ((guint32 *) ret) = slen * sizeof(gunichar2);
		ret [4 + slen * sizeof(gunichar2)] = 0;
		ret [5 + slen * sizeof(gunichar2)] = 0;

		return ret + 4;
	}
#endif
}

MonoString *
mono_string_from_bstr (gpointer bstr)
{
	if (!bstr)
		return NULL;
#ifdef HOST_WIN32
	return mono_string_new_utf16 (mono_domain_get (), bstr, SysStringLen (bstr));
#else
	return mono_string_new_utf16 (mono_domain_get (), bstr, *((guint32 *)bstr - 1) / sizeof(gunichar2));
#endif
}

void
mono_free_bstr (gpointer bstr)
{
	if (!bstr)
		return;
#ifdef HOST_WIN32
	SysFreeString ((BSTR)bstr);
#else
	g_free (((char *)bstr) - 4);
#endif
}

gboolean
mono_marshal_free_ccw (MonoObject* object)
{
	return FALSE;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (gpointer pUnk)
{
	g_assert_not_reached ();
	return 0;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (gpointer pUnk)
{
	g_assert_not_reached ();
	return 0;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (gpointer pUnk, gpointer riid, gpointer* ppv)
{
	g_assert_not_reached ();
	return 0;
}

#endif /* DISABLE_COM */

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr)
{
	return mono_string_from_bstr(ptr);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToBSTR (MonoString* ptr)
{
	return mono_string_to_bstr(ptr);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (gpointer ptr)
{
	mono_free_bstr (ptr);
}
