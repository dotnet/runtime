/*
 * cominterop.h: COM Interop Support
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
mono_cominterop_init (void) MONO_INTERNAL;

void
mono_cominterop_cleanup (void) MONO_INTERNAL;

void
mono_mb_emit_cominterop_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethod* method) MONO_INTERNAL;

void
mono_cominterop_emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec) MONO_INTERNAL;

void
mono_cominterop_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec) MONO_INTERNAL;

MonoMethod *
mono_cominterop_get_native_wrapper (MonoMethod *method) MONO_INTERNAL;

MonoMethod *
mono_cominterop_get_invoke (MonoMethod *method) MONO_INTERNAL;

int
mono_cominterop_emit_marshal_com_interface (EmitMarshalContext *m, int argnum, 
											MonoType *t,
											MonoMarshalSpec *spec, 
											int conv_arg, MonoType **conv_arg_type, 
											MarshalAction action) MONO_INTERNAL;

int
mono_cominterop_emit_marshal_safearray (EmitMarshalContext *m, int argnum,
										MonoType *t,
										MonoMarshalSpec *spec, 
										int conv_arg, MonoType **conv_arg_type,
										MarshalAction action) MONO_INTERNAL;

MonoString * 
mono_string_from_bstr (gpointer bstr);

void 
mono_free_bstr (gpointer bstr);

#endif /* __MONO_COMINTEROP_H__ */
