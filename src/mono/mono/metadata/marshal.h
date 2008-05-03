
/*
 * marshal.h: Routines for marshaling complex types in P/Invoke methods.
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

#define mono_marshal_find_bitfield_offset(type, elem, byte_offset, bitmask) \
	do { \
		type tmp; \
		memset (&tmp, 0, sizeof (tmp)); \
		tmp.elem = 1; \
		mono_marshal_find_nonzero_bit_offset ((guint8*)&tmp, sizeof (tmp), (byte_offset), (bitmask)); \
	} while (0)

G_BEGIN_DECLS

/* marshaling helper functions */

void
mono_marshal_init (void) MONO_INTERNAL;

void
mono_marshal_cleanup (void) MONO_INTERNAL;

gint32
mono_class_native_size (MonoClass *klass, guint32 *align) MONO_INTERNAL;

MonoMarshalType *
mono_marshal_load_type_info (MonoClass* klass) MONO_INTERNAL;

gint32
mono_marshal_type_size (MonoType *type, MonoMarshalSpec *mspec, guint32 *align,
			gboolean as_field, gboolean unicode) MONO_INTERNAL;

int            
mono_type_native_stack_size (MonoType *type, guint32 *alignment) MONO_INTERNAL;

gpointer
mono_array_to_savearray (MonoArray *array) MONO_INTERNAL;

gpointer
mono_array_to_lparray (MonoArray *array) MONO_INTERNAL;

void
mono_string_utf8_to_builder (MonoStringBuilder *sb, char *text) MONO_INTERNAL;

void
mono_string_utf16_to_builder (MonoStringBuilder *sb, gunichar2 *text) MONO_INTERNAL;

gpointer
mono_string_builder_to_utf8 (MonoStringBuilder *sb) MONO_INTERNAL;

gpointer
mono_string_builder_to_utf16 (MonoStringBuilder *sb) MONO_INTERNAL;

gpointer
mono_string_to_ansibstr (MonoString *string_obj) MONO_INTERNAL;

gpointer
mono_string_to_bstr (MonoString *string_obj) MONO_INTERNAL;

void
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size) MONO_INTERNAL;

void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size) MONO_INTERNAL;

gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate) MONO_INTERNAL;

MonoDelegate*
mono_ftnptr_to_delegate (MonoClass *klass, gpointer ftn) MONO_INTERNAL;

void mono_delegate_free_ftnptr (MonoDelegate *delegate) MONO_INTERNAL;

void
mono_marshal_set_last_error (void) MONO_INTERNAL;

gpointer
mono_marshal_asany (MonoObject *obj, MonoMarshalNative string_encoding, int param_attrs) MONO_INTERNAL;

void
mono_marshal_free_asany (MonoObject *o, gpointer ptr, MonoMarshalNative string_encoding, int param_attrs) MONO_INTERNAL;

MonoMethod*
mono_marshal_get_write_barrier (void) MONO_INTERNAL;

guint
mono_type_to_ldind (MonoType *type) MONO_INTERNAL;

guint
mono_type_to_stind (MonoType *type) MONO_INTERNAL;

/* functions to create various architecture independent helper functions */

MonoMethod *
mono_marshal_method_from_wrapper (MonoMethod *wrapper) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_xappdomain_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_remoting_invoke_for_target (MonoMethod *method, MonoRemotingTarget target_type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_remoting_invoke_with_check (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method, MonoDelegate *del) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_static_rgctx_invoke (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoClass *delegate_klass, MonoObject *this) MONO_INTERNAL;

gpointer
mono_marshal_get_vtfixup_ftnptr (MonoImage *image, guint32 token, guint16 type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_icall_wrapper (MonoMethodSignature *sig, const char *name, gconstpointer func, gboolean check_exceptions) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method, gboolean check_exceptions) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_native_func_wrapper (MonoImage *image, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_stfld_wrapper (MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ldfld_wrapper (MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ldflda_wrapper (MonoType *type) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_ldfld_remote_wrapper (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_stfld_remote_wrapper (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_synchronized_wrapper (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_unbox_wrapper (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_isinst (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_castclass (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_proxy_cancast (MonoClass *klass) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_stelemref (void) MONO_INTERNAL;

MonoMethod*
mono_marshal_get_array_address (int rank, int elem_size) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_generic_array_helper (MonoClass *class, MonoClass *iface,
				       gchar *name, MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_marshal_get_thunk_invoke_wrapper (MonoMethod *method) MONO_INTERNAL;

/* marshaling internal calls */

void * 
mono_marshal_alloc (gulong size) MONO_INTERNAL;

void 
mono_marshal_free (gpointer ptr) MONO_INTERNAL;

void
mono_marshal_free_array (gpointer *ptr, int size) MONO_INTERNAL;

gboolean 
mono_marshal_free_ccw (MonoObject* obj) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length) MONO_INTERNAL;

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr (gpointer ptr, gint32 offset) MONO_INTERNAL;

unsigned char
ves_icall_System_Runtime_InteropServices_Marshal_ReadByte (gpointer ptr, gint32 offset) MONO_INTERNAL;

gint16
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt16 (gpointer ptr, gint32 offset) MONO_INTERNAL;

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt32 (gpointer ptr, gint32 offset) MONO_INTERNAL;

gint64
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt64 (gpointer ptr, gint32 offset) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteByte (gpointer ptr, gint32 offset, unsigned char val) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteIntPtr (gpointer ptr, gint32 offset, gpointer val) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt16 (gpointer ptr, gint32 offset, gint16 val) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt32 (gpointer ptr, gint32 offset, gint32 val) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt64 (gpointer ptr, gint32 offset, gint64 val) MONO_INTERNAL;

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr) MONO_INTERNAL;

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len) MONO_INTERNAL;

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr) MONO_INTERNAL;

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len) MONO_INTERNAL;

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr) MONO_INTERNAL;

guint32
ves_icall_System_Runtime_InteropServices_Marshal_GetComSlotForMethodInfoInternal (MonoReflectionMethod *m) MONO_INTERNAL;

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void) MONO_INTERNAL;

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionType *rtype) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst) MONO_INTERNAL;

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type) MONO_INTERNAL;

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionType *type, MonoString *field_name) MONO_INTERNAL;

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToBSTR (MonoString *string) MONO_INTERNAL;

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string) MONO_INTERNAL;

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure (gpointer src, MonoReflectionType *type) MONO_INTERNAL;

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocCoTaskMem (int size) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeCoTaskMem (void *ptr) MONO_INTERNAL;

gpointer 
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocCoTaskMem (gpointer ptr, int size) MONO_INTERNAL;

void*
ves_icall_System_Runtime_InteropServices_Marshal_AllocHGlobal (int size) MONO_INTERNAL;

gpointer 
ves_icall_System_Runtime_InteropServices_Marshal_ReAllocHGlobal (gpointer ptr, int size) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeHGlobal (void *ptr) MONO_INTERNAL;

void
ves_icall_System_Runtime_InteropServices_Marshal_FreeBSTR (void *ptr) MONO_INTERNAL;

void*
ves_icall_System_Runtime_InteropServices_Marshal_UnsafeAddrOfPinnedArrayElement (MonoArray *arrayobj, int index) MONO_INTERNAL;

MonoDelegate*
ves_icall_System_Runtime_InteropServices_Marshal_GetDelegateForFunctionPointerInternal (void *ftn, MonoReflectionType *type) MONO_INTERNAL;

int
ves_icall_System_Runtime_InteropServices_Marshal_AddRefInternal (gpointer pUnk) MONO_INTERNAL;

int
ves_icall_System_Runtime_InteropServices_Marshal_QueryInterfaceInternal (gpointer pUnk, gpointer riid, gpointer* ppv) MONO_INTERNAL;

int
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseInternal (gpointer pUnk) MONO_INTERNAL;

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIUnknownForObjectInternal (MonoObject* object) MONO_INTERNAL;

MonoObject*
ves_icall_System_Runtime_InteropServices_Marshal_GetObjectForCCW (void* pUnk) MONO_INTERNAL;

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetIDispatchForObjectInternal (MonoObject* object) MONO_INTERNAL;

void*
ves_icall_System_Runtime_InteropServices_Marshal_GetCCW (MonoObject* object, MonoReflectionType* type) MONO_INTERNAL;

MonoBoolean
ves_icall_System_Runtime_InteropServices_Marshal_IsComObject (MonoObject* object) MONO_INTERNAL;

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReleaseComObjectInternal (MonoObject* object) MONO_INTERNAL;

MonoObject *
ves_icall_System_ComObject_CreateRCW (MonoReflectionType *type) MONO_INTERNAL;

void
ves_icall_System_ComObject_ReleaseInterfaces(MonoComObject* obj) MONO_INTERNAL;

gpointer
ves_icall_System_ComObject_GetInterfaceInternal (MonoComObject* obj, MonoReflectionType* type, MonoBoolean throw_exception) MONO_INTERNAL;

void
ves_icall_Mono_Interop_ComInteropProxy_AddProxy (gpointer pUnk, MonoComInteropProxy* proxy) MONO_INTERNAL;

MonoComInteropProxy*
ves_icall_Mono_Interop_ComInteropProxy_FindProxy (gpointer pUnk) MONO_INTERNAL;

void
mono_win32_compat_CopyMemory (gpointer dest, gconstpointer source, gsize length);

void
mono_win32_compat_FillMemory (gpointer dest, gsize length, guchar fill);

void
mono_win32_compat_MoveMemory (gpointer dest, gconstpointer source, gsize length);

void
mono_win32_compat_ZeroMemory (gpointer dest, gsize length);

void
mono_marshal_find_nonzero_bit_offset (guint8 *buf, int len, int *byte_offset, guint8 *bitmask) MONO_INTERNAL;

G_END_DECLS

#endif /* __MONO_MARSHAL_H__ */

