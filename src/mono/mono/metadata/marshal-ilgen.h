
#ifndef __MARSHAL_ILGEN_H__
#define __MARSHAL_ILGEN_H__

#include "metadata/marshal-lightweight.h"
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
} MonoMarshalIlgenCallbacks;

void
mono_install_marshal_callbacks_ilgen (MonoMarshalIlgenCallbacks *cb);


MONO_API void
mono_marshal_ilgen_init (void);
 
void
mono_marshal_noilgen_init_heavyweight (void);

void
mono_marshal_noilgen_init_lightweight (void);

int
mono_emit_marshal_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,
	      MonoType **conv_arg_type, MarshalAction action,  MonoMarshalLightweightCallbacks* lightweigth_cb);

#endif // __MARSHAL_ILGEN_H__