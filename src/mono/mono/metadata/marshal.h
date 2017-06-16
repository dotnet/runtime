/**
 * \file
 * Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#ifndef __MONO_MARSHAL_H__
#define __MONO_MARSHAL_H__

#include <mono/metadata/class.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/class-internals.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>
#include <mono/metadata/method-builder.h>
#include <mono/metadata/remoting.h>
#include <mono/utils/mono-error.h>

#define mono_marshal_find_bitfield_offset(type, elem, byte_offset, bitmask) \
	do { \
		type tmp; \
		memset (&tmp, 0, sizeof (tmp)); \
		tmp.elem = 1; \
		mono_marshal_find_nonzero_bit_offset ((guint8*)&tmp, sizeof (tmp), (byte_offset), (bitmask)); \
	} while (0)

/*
 * This structure holds the state kept by the emit_ marshalling functions.
 * This is exported so it can be used by cominterop.c.
 */
typedef struct {
	MonoMethodBuilder *mb;
	MonoMethodSignature *sig;
	MonoMethodPInvoke *piinfo;
	int *orig_conv_args; /* Locals containing the original values of byref args */
	int retobj_var;
	int vtaddr_var;
	MonoClass *retobj_class;
	MonoMethodSignature *csig; /* Might need to be changed due to MarshalAs directives */
	MonoImage *image; /* The image to use for looking up custom marshallers */
} EmitMarshalContext;

typedef enum {
	/*
	 * This is invoked to convert arguments from the current types to
	 * the underlying types expected by the platform routine.  If required,
	 * the methods create a temporary variable with the proper type, and return
	 * the location for it (either the passed argument, or the newly allocated
	 * local slot).
	 */
	MARSHAL_ACTION_CONV_IN,

	/*
	 * This operation is called to push the actual value that was optionally
	 * converted on the first stage
	 */
	MARSHAL_ACTION_PUSH,

	/*
	 * Convert byref arguments back or free resources allocated during the
	 * CONV_IN stage
	 */
	MARSHAL_ACTION_CONV_OUT,

	/*
	 * The result from the unmanaged call is at the top of the stack when
	 * this action is invoked.    The result should be stored in the
	 * third local variable slot. 
	 */
	MARSHAL_ACTION_CONV_RESULT,

	MARSHAL_ACTION_MANAGED_CONV_IN,
	MARSHAL_ACTION_MANAGED_CONV_OUT,
	MARSHAL_ACTION_MANAGED_CONV_RESULT
} MarshalAction;

/*
 * This is an extension of the MONO_WRAPPER_ enum to avoid adding more elements to that
 * enum.
 */
typedef enum {
	WRAPPER_SUBTYPE_NONE,
	/* Subtypes of MONO_WRAPPER_MANAGED_TO_MANAGED */
	WRAPPER_SUBTYPE_ELEMENT_ADDR,
	WRAPPER_SUBTYPE_STRING_CTOR,
	/* Subtypes of MONO_WRAPPER_STELEMREF */
	WRAPPER_SUBTYPE_VIRTUAL_STELEMREF,
	/* Subtypes of MONO_WRAPPER_UNKNOWN */
	WRAPPER_SUBTYPE_FAST_MONITOR_ENTER,
	WRAPPER_SUBTYPE_FAST_MONITOR_ENTER_V4,
	WRAPPER_SUBTYPE_FAST_MONITOR_EXIT,
	WRAPPER_SUBTYPE_PTR_TO_STRUCTURE,
	WRAPPER_SUBTYPE_STRUCTURE_TO_PTR,
	/* Subtypes of MONO_WRAPPER_CASTCLASS */
	WRAPPER_SUBTYPE_CASTCLASS_WITH_CACHE,
	WRAPPER_SUBTYPE_ISINST_WITH_CACHE,
	/* Subtypes of MONO_WRAPPER_RUNTIME_INVOKE */
	WRAPPER_SUBTYPE_RUNTIME_INVOKE_NORMAL,
	WRAPPER_SUBTYPE_RUNTIME_INVOKE_DYNAMIC,
	WRAPPER_SUBTYPE_RUNTIME_INVOKE_DIRECT,
	WRAPPER_SUBTYPE_RUNTIME_INVOKE_VIRTUAL,
	/* Subtypes of MONO_WRAPPER_MANAGED_TO_NATIVE */
	WRAPPER_SUBTYPE_ICALL_WRAPPER,
	WRAPPER_SUBTYPE_NATIVE_FUNC_AOT,
	WRAPPER_SUBTYPE_PINVOKE,
	/* Subtypes of MONO_WRAPPER_UNKNOWN */
	WRAPPER_SUBTYPE_SYNCHRONIZED_INNER,
	WRAPPER_SUBTYPE_GSHAREDVT_IN,
	WRAPPER_SUBTYPE_GSHAREDVT_OUT,
	WRAPPER_SUBTYPE_ARRAY_ACCESSOR,
	/* Subtypes of MONO_WRAPPER_MANAGED_TO_MANAGED */
	WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER,
	/* Subtypes of MONO_WRAPPER_DELEGATE_INVOKE */
	WRAPPER_SUBTYPE_DELEGATE_INVOKE_VIRTUAL,
	WRAPPER_SUBTYPE_DELEGATE_INVOKE_BOUND,
	/* Subtypes of MONO_WRAPPER_UNKNOWN */
	WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG,
	WRAPPER_SUBTYPE_GSHAREDVT_OUT_SIG,
	WRAPPER_SUBTYPE_INTERP_IN
} WrapperSubtype;

typedef struct {
	MonoMethod *method;
	MonoClass *klass;
} NativeToManagedWrapperInfo;

typedef struct {
	MonoMethod *method;
} StringCtorWrapperInfo;

typedef struct {
	int kind;
} VirtualStelemrefWrapperInfo;

typedef struct {
	guint32 rank, elem_size;
} ElementAddrWrapperInfo;

typedef struct {
	MonoMethod *method;
	/* For WRAPPER_SUBTYPE_RUNTIME_INVOKE_NORMAL */
	MonoMethodSignature *sig;
} RuntimeInvokeWrapperInfo;

typedef struct {
	MonoMethod *method;
} ManagedToNativeWrapperInfo;

typedef struct {
	MonoMethod *method;
} SynchronizedWrapperInfo;

typedef struct {
	MonoMethod *method;
} SynchronizedInnerWrapperInfo;

typedef struct {
	MonoMethod *method;
} GenericArrayHelperWrapperInfo;

typedef struct {
	gpointer func;
} ICallWrapperInfo;

typedef struct {
	MonoMethod *method;
} ArrayAccessorWrapperInfo;

typedef struct {
	MonoClass *klass;
} ProxyWrapperInfo;

typedef struct {
	const char *gc_name;
	int alloc_type;
} AllocatorWrapperInfo;

typedef struct {
	MonoMethod *method;
} UnboxWrapperInfo;

typedef struct {
	MonoMethod *method;
} RemotingWrapperInfo;

typedef struct {
	MonoMethodSignature *sig;
} GsharedvtWrapperInfo;

typedef struct {
	MonoMethod *method;
} DelegateInvokeWrapperInfo;

typedef struct {
	MonoMethodSignature *sig;
} InterpInWrapperInfo;

/*
 * This structure contains additional information to uniquely identify a given wrapper
 * method. It can be retrieved by mono_marshal_get_wrapper_info () for certain types
 * of wrappers, i.e. ones which do not have a 1-1 association with a method/class.
 */
typedef struct {
	WrapperSubtype subtype;
	union {
		/* RUNTIME_INVOKE_... */
		RuntimeInvokeWrapperInfo runtime_invoke;
		/* STRING_CTOR */
		StringCtorWrapperInfo string_ctor;
		/* ELEMENT_ADDR */
		ElementAddrWrapperInfo element_addr;
		/* VIRTUAL_STELEMREF */
		VirtualStelemrefWrapperInfo virtual_stelemref;
		/* MONO_WRAPPER_NATIVE_TO_MANAGED */
		NativeToManagedWrapperInfo native_to_managed;
		/* MONO_WRAPPER_MANAGED_TO_NATIVE */
		ManagedToNativeWrapperInfo managed_to_native;
		/* SYNCHRONIZED */
		SynchronizedWrapperInfo synchronized;
		/* SYNCHRONIZED_INNER */
		SynchronizedInnerWrapperInfo synchronized_inner;
		/* GENERIC_ARRAY_HELPER */
		GenericArrayHelperWrapperInfo generic_array_helper;
		/* ICALL_WRAPPER */
		ICallWrapperInfo icall;
		/* ARRAY_ACCESSOR */
		ArrayAccessorWrapperInfo array_accessor;
		/* PROXY_ISINST etc. */
		ProxyWrapperInfo proxy;
		/* ALLOC */
		AllocatorWrapperInfo alloc;
		/* UNBOX */
		UnboxWrapperInfo unbox;
		/* MONO_WRAPPER_REMOTING_INVOKE/MONO_WRAPPER_REMOTING_INVOKE_WITH_CHECK/MONO_WRAPPER_XDOMAIN_INVOKE */
		RemotingWrapperInfo remoting;
		/* GSHAREDVT_IN_SIG/GSHAREDVT_OUT_SIG */
		GsharedvtWrapperInfo gsharedvt;
		/* DELEGATE_INVOKE */
		DelegateInvokeWrapperInfo delegate_invoke;
		/* INTERP_IN */
		InterpInWrapperInfo interp_in;
	} d;
} WrapperInfo;

G_BEGIN_DECLS

/*type of the function pointer of methods returned by mono_marshal_get_runtime_invoke*/
typedef MonoObject *(*RuntimeInvokeFunction) (MonoObject *this_obj, void **params, MonoObject **exc, void* compiled_method);

typedef void (*RuntimeInvokeDynamicFunction) (void *args, MonoObject **exc, void* compiled_method);

/* marshaling helper functions */

void
mono_marshal_init (void);

void
mono_marshal_init_tls (void);

void
mono_marshal_cleanup (void);

gint32
mono_class_native_size (MonoClass *klass, guint32 *align);

MonoMarshalType *
mono_marshal_load_type_info (MonoClass* klass);

gint32
mono_marshal_type_size (MonoType *type, MonoMarshalSpec *mspec, guint32 *align,
			gboolean as_field, gboolean unicode);

int            
mono_type_native_stack_size (MonoType *type, guint32 *alignment);

gpointer
mono_string_to_ansibstr (MonoString *string_obj);

gpointer
mono_ptr_to_bstr (gpointer ptr, int slen);

gpointer
mono_string_to_bstr(MonoString* str);

void mono_delegate_free_ftnptr (MonoDelegate *delegate);

void
mono_marshal_set_last_error (void);

guint
mono_type_to_ldind (MonoType *type);

guint
mono_type_to_stind (MonoType *type);

/* functions to create various architecture independent helper functions */

MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper);

WrapperInfo*
mono_wrapper_info_create (MonoMethodBuilder *mb, WrapperSubtype subtype);

void
mono_marshal_set_wrapper_info (MonoMethod *method, WrapperInfo *info);

WrapperInfo*
mono_marshal_get_wrapper_info (MonoMethod *wrapper);

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method, MonoDelegate *del);

MonoMethod *
mono_marshal_get_delegate_invoke_internal (MonoMethod *method, gboolean callvirt, gboolean static_method_with_first_arg_bound, MonoMethod *target_method);

MonoMethod *
mono_marshal_get_runtime_invoke_full (MonoMethod *method, gboolean virtual_, gboolean need_direct_wrapper);

MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method, gboolean is_virtual);

MonoMethod*
mono_marshal_get_runtime_invoke_dynamic (void);

MonoMethod *
mono_marshal_get_runtime_invoke_for_sig (MonoMethodSignature *sig);

MonoMethodSignature*
mono_marshal_get_string_ctor_signature (MonoMethod *method);

MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, uint32_t this_loc, MonoError *exernal_error);

gpointer
mono_marshal_get_vtfixup_ftnptr (MonoImage *image, guint32 token, guint16 type);

MonoMethod *
mono_marshal_get_icall_wrapper (MonoMethodSignature *sig, const char *name, gconstpointer func, gboolean check_exceptions);

MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method, gboolean check_exceptions, gboolean aot);

MonoMethod *
mono_marshal_get_native_func_wrapper (MonoImage *image, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func);

MonoMethod*
mono_marshal_get_native_func_wrapper_aot (MonoClass *klass);

MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass);

MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass);

MonoMethod *
mono_marshal_get_synchronized_wrapper (MonoMethod *method);

MonoMethod *
mono_marshal_get_synchronized_inner_wrapper (MonoMethod *method);

MonoMethod *
mono_marshal_get_unbox_wrapper (MonoMethod *method);

MonoMethod *
mono_marshal_get_castclass_with_cache (void);

MonoMethod *
mono_marshal_get_isinst_with_cache (void);

MonoMethod *
mono_marshal_get_stelemref (void);

MonoMethod*
mono_marshal_get_virtual_stelemref (MonoClass *array_class);

MonoMethod**
mono_marshal_get_virtual_stelemref_wrappers (int *nwrappers);

MonoMethod*
mono_marshal_get_array_address (int rank, int elem_size);

MonoMethod *
mono_marshal_get_array_accessor_wrapper (MonoMethod *method);

MonoMethod *
mono_marshal_get_generic_array_helper (MonoClass *klass, gchar *name, MonoMethod *method);

MonoMethod *
mono_marshal_get_thunk_invoke_wrapper (MonoMethod *method);

MonoMethod*
mono_marshal_get_gsharedvt_in_wrapper (void);

MonoMethod*
mono_marshal_get_gsharedvt_out_wrapper (void);

void
mono_marshal_free_dynamic_wrappers (MonoMethod *method);

void
mono_marshal_lock_internal (void);

void
mono_marshal_unlock_internal (void);

/* marshaling internal calls */

void * 
mono_marshal_alloc (gsize size, MonoError *error);

void 
mono_marshal_free (gpointer ptr);

void
mono_marshal_free_array (gpointer *ptr, int size);

gboolean 
mono_marshal_free_ccw (MonoObject* obj);

void
cominterop_release_all_rcws (void); 

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length);

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length);

MonoStringHandle
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr, MonoError *error);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr);

guint32
ves_icall_System_Runtime_InteropServices_Marshal_GetComSlotForMethodInfoInternal (MonoReflectionMethod *m);

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void);

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionTypeHandle rtype, MonoError *error);

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old);

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst);

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type);

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionTypeHandle type, MonoStringHandle field_name, MonoError *error);

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToBSTR (MonoString *string);

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_BufferToBSTR (MonoArray *ptr, int len);

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string);

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string);

void
ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure (gpointer src, MonoReflectionType *type);

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem (int size);

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMemSize (gulong size);

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr);

gpointer 
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem (gpointer ptr, int size);

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (gpointer size);

gpointer 
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal (gpointer ptr, gpointer size);

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal (void *ptr);

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (void *ptr);

void*
ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement (MonoArray *arrayobj, int index);

MonoDelegateHandle
ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal (void *ftn, MonoReflectionTypeHandle type, MonoError *error);

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_GetFunctionPointerForDelegateInternal (MonoDelegateHandle delegate, MonoError *error);

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (gpointer pUnk);

int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (gpointer pUnk, gpointer riid, gpointer* ppv);

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (gpointer pUnk);

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal (MonoObject* object);

MonoObject*
ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW (void* pUnk);

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIDispatchForObjectInternal (MonoObject* object);

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetCCW (MonoObject* object, MonoReflectionType* type);

MonoBoolean
ves_icall_System_Runtime_InteropServices_Marshal_IsComObject (MonoObject* object);

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal (MonoObject* object);

MonoObject *
ves_icall_System_ComObject_CreateRCW (MonoReflectionType *type);

void
ves_icall_System_ComObject_ReleaseInterfaces(MonoComObject* obj);

gpointer
ves_icall_System_ComObject_GetInterfaceInternal (MonoComObject* obj, MonoReflectionType* type, MonoBoolean throw_exception);

void
ves_icall_Mono_Interop_ComInteropProxy_AddProxy (gpointer pUnk, MonoComInteropProxy* proxy);

MonoComInteropProxy*
ves_icall_Mono_Interop_ComInteropProxy_FindProxy (gpointer pUnk);

MONO_API void
mono_win32_compat_CopyMemory (gpointer dest, gconstpointer source, gsize length);

MONO_API void
mono_win32_compat_FillMemory (gpointer dest, gsize length, guchar fill);

MONO_API void
mono_win32_compat_MoveMemory (gpointer dest, gconstpointer source, gsize length);

MONO_API void
mono_win32_compat_ZeroMemory (gpointer dest, gsize length);

void
mono_marshal_find_nonzero_bit_offset (guint8 *buf, int len, int *byte_offset, guint8 *bitmask) MONO_LLVM_INTERNAL;

MonoMethodSignature*
mono_signature_no_pinvoke (MonoMethod *method);

/* Called from cominterop.c/remoting.c */

void
mono_marshal_emit_native_wrapper (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, gboolean aot, gboolean check_exceptions, gboolean func_param);

void
mono_marshal_emit_managed_wrapper (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, uint32_t target_handle);

GHashTable*
mono_marshal_get_cache (GHashTable **var, GHashFunc hash_func, GCompareFunc equal_func);

MonoMethod*
mono_marshal_find_in_cache (GHashTable *cache, gpointer key);

MonoMethod*
mono_mb_create_and_cache (GHashTable *cache, gpointer key,
						  MonoMethodBuilder *mb, MonoMethodSignature *sig,
						  int max_stack);
void
mono_marshal_emit_thread_interrupt_checkpoint (MonoMethodBuilder *mb);

void
mono_marshal_emit_thread_force_interrupt_checkpoint (MonoMethodBuilder *mb);

void
mono_marshal_use_aot_wrappers (gboolean use);

MonoObject *
mono_marshal_xdomain_copy_value (MonoObject *val, MonoError *error);

MonoObject *
ves_icall_mono_marshal_xdomain_copy_value (MonoObject *val);

int
mono_mb_emit_save_args (MonoMethodBuilder *mb, MonoMethodSignature *sig, gboolean save_this);

void
mono_mb_emit_restore_result (MonoMethodBuilder *mb, MonoType *return_type);

MonoMethod*
mono_mb_create (MonoMethodBuilder *mb, MonoMethodSignature *sig,
				int max_stack, WrapperInfo *info);

MonoMethod*
mono_mb_create_and_cache_full (GHashTable *cache, gpointer key,
							   MonoMethodBuilder *mb, MonoMethodSignature *sig,
							   int max_stack, WrapperInfo *info, gboolean *out_found);

typedef void (*MonoFtnPtrEHCallback) (guint32 gchandle);

MONO_API void
mono_install_ftnptr_eh_callback (MonoFtnPtrEHCallback callback);

G_END_DECLS

#endif /* __MONO_MARSHAL_H__ */


