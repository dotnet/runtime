
#include <mono/component/component.h>
#include <mono/component/marshal-ilgen.h>
#include <mono/metadata/marshal.h>

static bool 
marshal_ilgen_available (void)
{
	return false;
}

static int
stub_emit_marshal_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		MonoMarshalSpec *spec, int conv_arg,	
		MonoType **conv_arg_type, MarshalAction action, MonoMarshalLightweightCallbacks* lightweight_cb)
{
	if (spec && spec->native == MONO_NATIVE_CUSTOM)
		return conv_arg;

	if (spec && spec->native == MONO_NATIVE_ASANY)
		return conv_arg;

	switch (t->type) {
	case MONO_TYPE_BOOLEAN:
		return lightweight_cb->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_PTR:
		return lightweight_cb->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_CHAR:
		return lightweight_cb->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_FNPTR:
		return lightweight_cb->emit_marshal_scalar (m, argnum, t, spec, conv_arg, conv_arg_type, action);
	default:
		return conv_arg;
	}

	return 0;
}

static void 
mono_component_marshal_ilgen_stub_init(void)
{
}

static void
stub_mono_marshal_ilgen_install_callbacks_mono (IlgenCallbacksToMono *callbacks)
{
}

static MonoComponentMarshalILgen component_func_table = {
	{ MONO_COMPONENT_ITF_VERSION, &marshal_ilgen_available },
	mono_component_marshal_ilgen_stub_init,
	stub_emit_marshal_ilgen,
	stub_mono_marshal_ilgen_install_callbacks_mono
};

MonoComponentMarshalILgen*
mono_component_marshal_ilgen_init (void) 
{
	return &component_func_table;
}
