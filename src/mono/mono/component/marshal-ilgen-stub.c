
#include <mono/component/component.h>
#include <mono/component/marshal-ilgen.h>
#include <mono/metadata/marshal.h>

static bool 
marshal_ilgen_available (void)
{
	return false;
}

static void emit_throw_exception (MonoMarshalLightweightCallbacks* lightweight_cb, 
		MonoMethodBuilder* mb, const char* exc_nspace, const char* exc_name, const char* msg)
{
	lightweight_cb->mb_emit_exception (mb, exc_nspace, exc_name, msg);
}

static int
stub_emit_marshal_ilgen (EmitMarshalContext* m, int argnum, MonoType* t,
		MonoMarshalSpec* spec, int conv_arg,	
		MonoType** conv_arg_type, MarshalAction action, MonoMarshalLightweightCallbacks* lightweight_cb)
{
	if (spec) {
		g_assert (spec->native != MONO_NATIVE_ASANY);
		g_assert (spec->native != MONO_NATIVE_CUSTOM);
	}
	
	g_assert (!m_type_is_byref(t));

	switch (t->type) {
	case MONO_TYPE_PTR:
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
		emit_throw_exception (lightweight_cb, m->mb, "System", "ApplicationException",
			g_strdup("Cannot marshal nonblittlable types without marshal-ilgen."));
		break;
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
