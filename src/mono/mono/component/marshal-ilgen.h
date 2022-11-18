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
	void (*ilgen_init_internal) (void);
	int (*emit_marshal_ilgen) (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action,  MonoMarshalLightweightCallbacks* lightweigth_cb);
	void (*install_callbacks_mono) (IlgenCallbacksToMono *callbacks);
} MonoComponentMarshalILgen;

MONO_COMPONENT_EXPORT_ENTRYPOINT
MonoComponentMarshalILgen* mono_component_marshal_ilgen_init (void);
 
#endif // __MARSHAL_ILGEN_H__
