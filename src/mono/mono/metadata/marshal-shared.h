/**
* \file
* Functionality shared between the legacy and DisableRuntimeMarshaling marshaling subsystems. 
*
* Licensed to the .NET Foundation under one or more agreements.
* The .NET Foundation licenses this file to you under the MIT license.
*/

#include "mono/metadata/debug-helpers.h"
#include "metadata/marshal.h"
#include "metadata/method-builder-ilgen.h"
#include "metadata/custom-attrs-internals.h"
#include "metadata/class-init.h"
#include "mono/metadata/class-internals.h"
#include "metadata/reflection-internals.h"
#include "mono/metadata/handle.h"

#ifndef _MONO_METADATA_MARSHAL_SHARED_H_
#define _MONO_METADATA_MARSHAL_SHARED_H_

MonoMethod** mono_marshal_shared_get_sh_dangerous_add_ref (void);
MonoMethod** mono_marshal_shared_get_sh_dangerous_release (void);

void
mono_marshal_shared_emit_marshal_custom_get_instance(MonoMethodBuilder *mb, MonoClass *klass, MonoMarshalSpec *spec);

void
mono_marshal_shared_init_safe_handle (void);

void
mono_mb_emit_auto_layout_exception (MonoMethodBuilder *mb, MonoClass *klass);

gboolean mono_marshal_shared_is_in (const MonoType *t);

gboolean mono_marshal_shared_is_out (const MonoType *t);

MonoMarshalConv
mono_marshal_shared_conv_str_inverse (MonoMarshalConv conv);

void
mono_marshal_shared_mb_emit_exception_marshal_directive (MonoMethodBuilder *mb, char *msg);

void
mono_marshal_shared_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec);

gboolean
mono_marshal_shared_get_fixed_buffer_attr (MonoClassField *field, MonoType **out_etype, int *out_len);

void
mono_marshal_shared_emit_fixed_buf_conv (MonoMethodBuilder *mb, MonoType *type, MonoType *etype, int len, gboolean to_object, int *out_usize);

int
mono_marshal_shared_offset_of_first_nonstatic_field (MonoClass *klass);

MonoMethod*
mono_marshal_shared_get_method_nofail (MonoClass *klass, const char *method_name, int num_params, int flags);

MonoJitICallId
mono_marshal_shared_conv_to_icall (MonoMarshalConv conv, int *ind_store_type);

void
mono_marshal_shared_emit_ptr_to_object_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec);

void
mono_marshal_shared_emit_struct_conv_full (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object,
						int offset_of_first_child_field, MonoMarshalNative string_encoding);

void
mono_marshal_shared_emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

void
mono_marshal_shared_emit_thread_interrupt_checkpoint_call (MonoMethodBuilder *mb, MonoJitICallId checkpoint_icall_id);

void
mono_marshal_shared_emit_object_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, MonoMarshalSpec *mspec);

#endif /* _MONO_METADATA_MARSHAL_SHARED_H_ */