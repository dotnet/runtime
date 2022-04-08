/**
 * \file
 * Copyright 2022 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MARSHAL_LEGACY_H__
#define __MARSHAL_LEGACY_H__

#include "metadata/marshal.h"

typedef struct {
	int version;
	int (*emit_marshal_vtype) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_string) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_variant) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_safehandle) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_object) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
        int (*emit_marshal_array) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_boolean) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_ptr) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_char) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_custom) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_asany) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
	int (*emit_marshal_handleref) (EmitMarshalContext *m, int argnum, MonoType *t, MonoMarshalSpec *spec, int conv_arg, MonoType **conv_arg_type, MarshalAction action);
} MonoMarshalCallbacksLegacy;

int
mono_emit_marshal_legacy (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action, MonoMarshalCallbacks *cb);

void
mono_install_marshal_callbacks_legacy (MonoMarshalCallbacksLegacy *cb);

#ifdef ENABLE_ILGEN
void
mono_marshal_ilgen_legacy_init (void);
#else
void
mono_marshal_noilgen_legacy_init (void);
#endif 


#endif // __MARSHAL_LEGACY_H__