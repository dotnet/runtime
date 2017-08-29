/**
 * \file
 * COM Interop Support
 * 
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#ifndef __MONO_COMINTEROP_H__
#define __MONO_COMINTEROP_H__

#include <mono/metadata/method-builder.h>
#include <mono/metadata/marshal.h>

void
mono_cominterop_init (void);

void
mono_cominterop_cleanup (void);

void
mono_mb_emit_cominterop_get_function_pointer (MonoMethodBuilder *mb, MonoMethod* method);

void
mono_mb_emit_cominterop_call_function_pointer (MonoMethodBuilder *mb, MonoMethodSignature *sig);

void
mono_mb_emit_cominterop_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethod* method);

void
mono_cominterop_emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec);

void
mono_cominterop_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec);

MonoMethod *
mono_cominterop_get_native_wrapper (MonoMethod *method);

MonoMethod *
mono_cominterop_get_invoke (MonoMethod *method);

int
mono_cominterop_emit_marshal_com_interface (EmitMarshalContext *m, int argnum, 
											MonoType *t,
											MonoMarshalSpec *spec, 
											int conv_arg, MonoType **conv_arg_type, 
											MarshalAction action);

int
mono_cominterop_emit_marshal_safearray (EmitMarshalContext *m, int argnum,
										MonoType *t,
										MonoMarshalSpec *spec, 
										int conv_arg, MonoType **conv_arg_type,
										MarshalAction action);

MONO_RT_EXTERNAL_ONLY
MONO_API MonoString * 
mono_string_from_bstr (gpointer bstr);

MonoString *
mono_string_from_bstr_icall (gpointer bstr);

MonoString *
mono_string_from_bstr_checked (gpointer bstr, MonoError *error);

MONO_API void 
mono_free_bstr (gpointer bstr);

MonoClass*
mono_class_try_get_com_object_class (void);

void*
mono_cominterop_get_com_interface (MonoObject* object, MonoClass* ic, MonoError *error);

#endif /* __MONO_COMINTEROP_H__ */
