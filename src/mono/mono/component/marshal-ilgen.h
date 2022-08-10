/**
 * \file
 * Copyright 2022 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MARSHAL_ILGEN_H__
#define __MARSHAL_ILGEN_H__

#include "metadata/marshal-lightweight.h"
#include "metadata/marshal.h"
#include "mono/component/component.h"

typedef struct MonoComponentMarshalILgen {
	MonoComponent component;
	void (*ilgen_init) (void);
	int (*emit_marshal_ilgen) (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action,  MonoMarshalLightweightCallbacks* lightweigth_cb);
	void (*install_callbacks_mono) (IlgenCallbacksToMono *callbacks);

} MonoComponentMarshalILgen;

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
} MonoMarshalILgenCallbacks;

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentMarshalILgen* mono_component_marshal_ilgen_init (void);

void
mono_install_marshal_callbacks_ilgen (MonoMarshalILgenCallbacks *cb);

MONO_API void
mono_marshal_ilgen_init (void);
 
int
mono_emit_marshal_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action,  MonoMarshalLightweightCallbacks* lightweigth_cb);

void
mono_marshal_ilgen_install_callbacks_mono (IlgenCallbacksToMono *callbacks);

#endif // __MARSHAL_ILGEN_H__