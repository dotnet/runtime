
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
#include <mono/metadata/object.h>
#include <mono/metadata/opcodes.h>
#include <mono/metadata/reflection.h>

typedef struct _MonoMethodBuilder MonoMethodBuilder;

/* marshaling helper functions */

gpointer
mono_array_to_savearray (MonoArray *array);

gpointer
mono_array_to_lparray (MonoArray *array);

gpointer
mono_string_to_ansibstr (MonoString *string_obj);

gpointer
mono_string_to_bstr (MonoString *string_obj);

void
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size);

void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size);

gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate);

void * 
mono_marshal_string_array (MonoArray *array);

/* method builder functions */

void
mono_mb_free (MonoMethodBuilder *mb);

MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name);

void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value);

guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data);

void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func);

void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig);

int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type);

MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack);

void
mono_mb_emit_ldarg (MonoMethodBuilder *mb, guint argnum);

void
mono_mb_emit_ldarg_addr (MonoMethodBuilder *mb, guint argnum);

void
mono_mb_emit_ldloc (MonoMethodBuilder *mb, guint num);

void
mono_mb_emit_ldloc_addr (MonoMethodBuilder *mb, guint locnum);

void
mono_mb_emit_stloc (MonoMethodBuilder *mb, guint num);

void
mono_mb_emit_exception (MonoMethodBuilder *mb);

void
mono_mb_emit_icon (MonoMethodBuilder *mb, gint32 value);

void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint8 local, gint8 incr);

void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op);

void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data);

void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data);

/* functions to create various architecture independent helper functions */

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method);

MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoObject *this);

MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method);

MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass);

MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass);

/* marshaling internal calls */

void * 
mono_marshal_alloc (gpointer size);

void 
mono_marshal_free (gpointer ptr);

void * 
mono_marshal_realloc (gpointer ptr, gpointer size);

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length);

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length);

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr (gpointer ptr, gint32 offset);

unsigned char
ves_icall_System_Runtime_InteropServices_Marshal_ReadByte (gpointer ptr, gint32 offset);

gint16
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt16 (gpointer ptr, gint32 offset);

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt32 (gpointer ptr, gint32 offset);

gint64
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt64 (gpointer ptr, gint32 offset);

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteByte (gpointer ptr, gint32 offset, unsigned char val);

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteIntPtr (gpointer ptr, gint32 offset, gpointer val);

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt16 (gpointer ptr, gint32 offset, gint16 val);

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt32 (gpointer ptr, gint32 offset, gint32 val);

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt64 (gpointer ptr, gint32 offset, gint64 val);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len);

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr);

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void);

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionType *rtype);

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old);

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst);

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type);

#endif /* __MONO_MARSHAL_H__ */

