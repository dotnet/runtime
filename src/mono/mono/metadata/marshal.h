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
#include <mono/utils/mono-error.h>
#include <mono/metadata/icalls.h>

typedef gunichar2 *mono_bstr;
typedef const gunichar2 *mono_bstr_const;

#define mono_marshal_find_bitfield_offset(type, elem, byte_offset, bitmask) \
	do { \
		type tmp; \
		memset (&tmp, 0, sizeof (tmp)); \
		tmp.elem = 1; \
		mono_marshal_find_nonzero_bit_offset ((guint8*)&tmp, sizeof (tmp), (byte_offset), (bitmask)); \
	} while (0)


GENERATE_TRY_GET_CLASS_WITH_CACHE_DECL(stringbuilder)


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
	gboolean runtime_marshalling_enabled;
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
	/* Subtypes of MONO_WRAPPER_OTHER */
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
	WRAPPER_SUBTYPE_ICALL_WRAPPER, // specifically JIT icalls
	WRAPPER_SUBTYPE_NATIVE_FUNC,
	WRAPPER_SUBTYPE_NATIVE_FUNC_AOT,
	WRAPPER_SUBTYPE_NATIVE_FUNC_INDIRECT,
	WRAPPER_SUBTYPE_PINVOKE,
	/* Subtypes of MONO_WRAPPER_OTHER */
	WRAPPER_SUBTYPE_SYNCHRONIZED_INNER,
	WRAPPER_SUBTYPE_GSHAREDVT_IN,
	WRAPPER_SUBTYPE_GSHAREDVT_OUT,
	WRAPPER_SUBTYPE_ARRAY_ACCESSOR,
	/* Subtypes of MONO_WRAPPER_MANAGED_TO_MANAGED */
	WRAPPER_SUBTYPE_GENERIC_ARRAY_HELPER,
	/* Subtypes of MONO_WRAPPER_DELEGATE_INVOKE */
	WRAPPER_SUBTYPE_DELEGATE_INVOKE_VIRTUAL,
	WRAPPER_SUBTYPE_DELEGATE_INVOKE_BOUND,
	/* Subtypes of MONO_WRAPPER_OTHER */
	WRAPPER_SUBTYPE_GSHAREDVT_IN_SIG,
	WRAPPER_SUBTYPE_GSHAREDVT_OUT_SIG,
	WRAPPER_SUBTYPE_INTERP_IN,
	WRAPPER_SUBTYPE_INTERP_LMF,
	WRAPPER_SUBTYPE_AOT_INIT,
	WRAPPER_SUBTYPE_LLVM_FUNC
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
	MonoClass *klass;
	const char *name;
	MonoMethod *method;
} GenericArrayHelperWrapperInfo;

typedef struct {
	MonoJitICallId jit_icall_id;
} ICallWrapperInfo;

typedef struct {
	MonoMethod *method;
} ArrayAccessorWrapperInfo;

typedef struct {
	const char *gc_name;
	int alloc_type;
} AllocatorWrapperInfo;

typedef struct {
	MonoMethod *method;
} UnboxWrapperInfo;

typedef struct {
	MonoMethodSignature *sig;
} GsharedvtWrapperInfo;

typedef struct {
	MonoMethod *method;
} DelegateInvokeWrapperInfo;

typedef struct {
	MonoMethodSignature *sig;
} InterpInWrapperInfo;

typedef enum {
	AOT_INIT_METHOD = 0,
	AOT_INIT_METHOD_GSHARED_MRGCTX = 1,
	AOT_INIT_METHOD_GSHARED_THIS = 2,
	AOT_INIT_METHOD_GSHARED_VTABLE = 3,
	AOT_INIT_METHOD_NUM = 4
} MonoAotInitSubtype;

typedef struct {
	// We emit this code when we init the module,
	// and later match up the native code with this method
	// using the name.
	MonoAotInitSubtype subtype;
} AOTInitWrapperInfo;

typedef enum {
	LLVM_FUNC_WRAPPER_GC_POLL = 0
} MonoLLVMFuncWrapperSubtype;

typedef struct {
	// We emit this code when we init the module,
	// and later match up the native code with this method
	// using the name.
	MonoLLVMFuncWrapperSubtype subtype;
} LLVMFuncWrapperInfo;

typedef struct {
	MonoClass *klass;
	MonoMethodSignature *sig;
} NativeFuncWrapperInfo;

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
		/* ALLOC */
		AllocatorWrapperInfo alloc;
		/* UNBOX */
		UnboxWrapperInfo unbox;
		/* GSHAREDVT_IN_SIG/GSHAREDVT_OUT_SIG */
		GsharedvtWrapperInfo gsharedvt;
		/* DELEGATE_INVOKE */
		DelegateInvokeWrapperInfo delegate_invoke;
		/* INTERP_IN */
		InterpInWrapperInfo interp_in;
		/* AOT_INIT */
		AOTInitWrapperInfo aot_init;
		/* LLVM_FUNC */
		LLVMFuncWrapperInfo llvm_func;
		/* NATIVE_FUNC_INDIRECT */
		NativeFuncWrapperInfo native_func;
	} d;
} WrapperInfo;

typedef enum {
	STELEMREF_OBJECT, /*no check at all*/
	STELEMREF_SEALED_CLASS, /*check vtable->klass->element_type */
	STELEMREF_CLASS, /*only the klass->parents check*/
	STELEMREF_CLASS_SMALL_IDEPTH, /* like STELEMREF_CLASS bit without the idepth check */
	STELEMREF_INTERFACE, /*interfaces without variant generic arguments. */
	STELEMREF_COMPLEX, /*arrays, MBR or types with variant generic args - go straight to icalls*/
	STELEMREF_KIND_COUNT
} MonoStelemrefKind;


typedef enum {
	EMIT_NATIVE_WRAPPER_AOT = 0x01, /* FIXME: what does "aot" mean here */
	EMIT_NATIVE_WRAPPER_CHECK_EXCEPTIONS = 0x02,
	EMIT_NATIVE_WRAPPER_FUNC_PARAM = 0x04,
	EMIT_NATIVE_WRAPPER_FUNC_PARAM_UNBOXED = 0x08,
	EMIT_NATIVE_WRAPPER_SKIP_GC_TRANS = 0x10,
	EMIT_NATIVE_WRAPPER_RUNTIME_MARSHALLING_ENABLED = 0x20,
} MonoNativeWrapperFlags;

G_ENUM_FUNCTIONS(MonoNativeWrapperFlags);

#define MONO_MARSHAL_CALLBACKS_VERSION 6

typedef struct {
	int version;
	int (*emit_marshal_array) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_boolean) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_ptr) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_char) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_scalar) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);

	int (*emit_marshal_custom) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_asany) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_vtype) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_string) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_safehandle) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_handleref) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_object) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_variant) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	void (*emit_castclass) (MonoMethodBuilder *mb);
	void (*emit_struct_to_ptr) (MonoMethodBuilder *mb, MonoClass *klass);
	void (*emit_ptr_to_struct) (MonoMethodBuilder *mb, MonoClass *klass);
	void (*emit_isinst) (MonoMethodBuilder *mb);
	void (*emit_virtual_stelemref) (MonoMethodBuilder *mb, const char **param_names, MonoStelemrefKind kind);
	void (*emit_stelemref) (MonoMethodBuilder *mb);
	void (*emit_array_address) (MonoMethodBuilder *mb, int rank, int elem_size);
	void (*emit_native_wrapper) (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, MonoNativeWrapperFlags flags);
	void (*emit_managed_wrapper) (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, MonoGCHandle target_handle, MonoError *error);
	void (*emit_runtime_invoke_body) (MonoMethodBuilder *mb, const char **param_names, MonoImage *image, MonoMethod *method, MonoMethodSignature *sig, MonoMethodSignature *callsig, gboolean virtual_, gboolean need_direct_wrapper);
	void (*emit_runtime_invoke_dynamic) (MonoMethodBuilder *mb);
	void (*emit_delegate_begin_invoke) (MonoMethodBuilder *mb, MonoMethodSignature *sig);
	void (*emit_delegate_end_invoke) (MonoMethodBuilder *mb, MonoMethodSignature *sig);
	void (*emit_delegate_invoke_internal) (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodSignature *invoke_sig, gboolean static_method_with_first_arg_bound, gboolean callvirt, gboolean closed_over_null, MonoMethod *method, MonoMethod *target_method, MonoClass *target_class, MonoGenericContext *ctx, MonoGenericContainer *container);
	void (*emit_synchronized_wrapper) (MonoMethodBuilder *mb, MonoMethod *method, MonoGenericContext *ctx, MonoGenericContainer *container, MonoMethod *enter_method, MonoMethod *exit_method, MonoMethod *gettypefromhandle_method);
	void (*emit_unbox_wrapper) (MonoMethodBuilder *mb, MonoMethod *method);
	void (*emit_array_accessor_wrapper) (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *sig, MonoGenericContext *ctx);
	void (*emit_generic_array_helper) (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig);
	void (*emit_thunk_invoke_wrapper) (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig);
	void (*emit_create_string_hack) (MonoMethodBuilder *mb, MonoMethodSignature *csig, MonoMethod *res);
	void (*emit_native_icall_wrapper) (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig, gboolean check_exceptions, gboolean aot, MonoMethodPInvoke *pinfo);
	void (*emit_icall_wrapper) (MonoMethodBuilder *mb, MonoJitICallInfo *callinfo, MonoMethodSignature *csig2, gboolean check_exceptions);
	void (*emit_return) (MonoMethodBuilder *mb);
	void (*emit_vtfixup_ftnptr) (MonoMethodBuilder *mb, MonoMethod *method, int param_count, guint16 type);
	void (*mb_skip_visibility) (MonoMethodBuilder *mb);
	void (*mb_set_dynamic) (MonoMethodBuilder *mb);
	void (*mb_emit_exception) (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg);
	void (*mb_emit_exception_for_error) (MonoMethodBuilder *mb, const MonoError *emitted_error);
	void (*mb_emit_byte) (MonoMethodBuilder *mb, guint8 op);
	void (*emit_marshal_directive_exception) (EmitMarshalContext *m, int argnum, const char* msg);
} MonoMarshalCallbacks;

/*type of the function pointer of methods returned by mono_marshal_get_runtime_invoke*/
typedef MonoObject *(*RuntimeInvokeFunction) (MonoObject *this_obj, void **params, MonoObject **exc, void* compiled_method);

typedef void (*RuntimeInvokeDynamicFunction) (void *args, MonoObject **exc, void* compiled_method);

void
mono_install_marshal_callbacks (MonoMarshalCallbacks *cb);

/* marshaling helper functions */

void
mono_marshal_init (void);

void
mono_marshal_init_tls (void);

gint32
mono_class_native_size (MonoClass *klass, guint32 *align);

MonoMarshalType *
mono_marshal_load_type_info (MonoClass* klass);

gint32
mono_marshal_type_size (MonoType *type, MonoMarshalSpec *mspec, guint32 *align,
			gboolean as_field, gboolean unicode);

int
mono_type_native_stack_size (MonoType *type, guint32 *alignment);

mono_bstr
mono_ptr_to_bstr (const gunichar2* ptr, int slen);

char *
mono_ptr_to_ansibstr (const char *ptr, size_t slen);

void mono_delegate_free_ftnptr (MonoDelegate *delegate);

void
mono_marshal_ftnptr_eh_callback (guint32 gchandle);

MONO_PAL_API void
mono_marshal_set_last_error (void);

ICALL_EXPORT
void
mono_marshal_clear_last_error (void);

guint
mono_type_to_ldind (MonoType *type);

guint
mono_type_to_stind (MonoType *type);

/* functions to create various architecture independent helper functions */

MONO_COMPONENT_API MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper);

WrapperInfo*
mono_wrapper_info_create (MonoMethodBuilder *mb, WrapperSubtype subtype);

void
mono_marshal_set_wrapper_info (MonoMethod *method, WrapperInfo *info);

MONO_COMPONENT_API WrapperInfo*
mono_marshal_get_wrapper_info (MonoMethod *wrapper);

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method, MonoDelegate *del);

MonoMethod *
mono_marshal_get_delegate_invoke_internal (MonoMethod *method, gboolean callvirt, gboolean static_method_with_first_arg_bound, MonoMethod *target_method);

WrapperSubtype
mono_marshal_get_delegate_invoke_subtype (MonoMethod *method, MonoDelegate *del);

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
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, MonoGCHandle this_loc, MonoError *exernal_error);

gpointer
mono_marshal_get_vtfixup_ftnptr (MonoImage *image, guint32 token, guint16 type);

MonoMethod *
mono_marshal_get_icall_wrapper (MonoJitICallInfo *callinfo, gboolean check_exceptions);

MonoMethod *
mono_marshal_get_aot_init_wrapper (MonoAotInitSubtype subtype);

const char *
mono_marshal_get_aot_init_wrapper_name (MonoAotInitSubtype subtype);

MonoMethod *
mono_marshal_get_llvm_func_wrapper (MonoLLVMFuncWrapperSubtype subtype);

MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method, gboolean check_exceptions, gboolean aot);

MonoMethod *
mono_marshal_get_native_func_wrapper (MonoImage *image, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func);

MonoMethod*
mono_marshal_get_native_func_wrapper_aot (MonoClass *klass);

MonoMethod*
mono_marshal_get_native_func_wrapper_indirect (MonoClass *caller_class, MonoMethodSignature *sig,
					       gboolean aot);

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
mono_marshal_get_virtual_stelemref_wrapper (MonoStelemrefKind kind);

MonoMethod*
mono_marshal_get_array_address (int rank, int elem_size);

MonoMethod *
mono_marshal_get_array_accessor_wrapper (MonoMethod *method);

MonoMethod *
mono_marshal_get_generic_array_helper (MonoClass *klass, const gchar *name, MonoMethod *method);

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

ICALL_EXPORT
void
mono_marshal_free (gpointer ptr);

ICALL_EXPORT
void
mono_marshal_free_array (gpointer *ptr, int size);

gboolean
mono_marshal_free_ccw (MonoObject* obj);

MONO_API void *
mono_marshal_string_to_utf16 (MonoString *s);

ICALL_EXPORT
void
mono_marshal_set_last_error_windows (int error);

ICALL_EXPORT
void
mono_struct_delete_old (MonoClass *klass, char *ptr);

int
mono_emit_marshal (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action);

ICALL_EXPORT
MonoObject *
mono_marshal_isinst_with_cache (MonoObject *obj, MonoClass *klass, uintptr_t *cache);

ICALL_EXPORT
MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params);

ICALL_EXPORT
MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params);

MonoMarshalNative
mono_marshal_get_string_encoding (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec);

MonoMarshalConv
mono_marshal_get_string_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec);

MonoMarshalConv
mono_marshal_get_stringbuilder_to_ptr_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec);

MonoMarshalConv
mono_marshal_get_ptr_to_stringbuilder_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec, gboolean *need_free);

MonoMarshalConv
mono_marshal_get_ptr_to_string_conv (MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec, gboolean *need_free);

MonoType*
mono_marshal_boolean_conv_in_get_local_type (MonoMarshalSpec *spec, guint8 *ldc_op /*out*/);

MonoClass*
mono_marshal_boolean_managed_conv_in_get_conv_arg_class (MonoMarshalSpec *spec, guint8 *ldop/*out*/);

gboolean
mono_pinvoke_is_unicode (MonoMethodPInvoke *piinfo);

gboolean
mono_marshal_need_free (MonoType *t, MonoMethodPInvoke *piinfo, MonoMarshalSpec *spec);

ICALL_EXPORT
MonoObject* mono_marshal_get_type_object (MonoClass *klass);

ICALL_EXPORT
gpointer
mono_marshal_lookup_pinvoke (MonoMethod *method);

ICALL_EXPORT
guint32
ves_icall_System_Runtime_InteropServices_Marshal_GetLastPInvokeError (void);

ICALL_EXPORT
void
ves_icall_System_Runtime_InteropServices_Marshal_SetLastPInvokeError (guint32 err);

ICALL_EXPORT
mono_bstr
ves_icall_System_Runtime_InteropServices_Marshal_BufferToBSTR (const gunichar2 *ptr, int len);

ICALL_EXPORT
void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (mono_bstr_const ptr);

ICALL_EXPORT
int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (MonoIUnknown *pUnk);

ICALL_EXPORT
int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (MonoIUnknown *pUnk, gconstpointer riid, gpointer* ppv);

ICALL_EXPORT
int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (MonoIUnknown *pUnk);

MONO_API void
mono_win32_compat_CopyMemory (gpointer dest, gconstpointer source, gsize length);

MONO_API void
mono_win32_compat_FillMemory (gpointer dest, gsize length, guchar fill);

MONO_API void
mono_win32_compat_MoveMemory (gpointer dest, gconstpointer source, gsize length);

MONO_API void
mono_win32_compat_ZeroMemory (gpointer dest, gsize length);

void
mono_marshal_find_nonzero_bit_offset (guint8 *buf, int len, int *byte_offset, guint8 *bitmask);

MonoMethodSignature*
mono_signature_no_pinvoke (MonoMethod *method);

/* Called from cominterop.c/remoting.c */

void
mono_marshal_emit_native_wrapper (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, MonoNativeWrapperFlags flags);

void
mono_marshal_emit_managed_wrapper (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, MonoGCHandle target_handle, MonoError *error);

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

#endif /* __MONO_MARSHAL_H__ */
