
#include <mono/component/component.h>
#include <mono/component/marshal_ilgen.h>
#include <mono/metadata/marshal.h>

static bool 
marshal_ilgen_available (void)
{
	return false;
}

static int
stub_emit_marshal_ilgen (EmitMarshalContext *m, int argnum, MonoType *t,
	      MonoMarshalSpec *spec, int conv_arg,	
	      MonoType **conv_arg_type, MarshalAction action, MonoMarshalLightweightCallbacks* lightweigth_cb)
{
	g_assert(false);
	return 0;
}

static void 
mono_component_marshal_ilgen_stub_init(void)
{
	g_assert(false);	
}	

static void
stub_mono_marshal_ilgen_install_callbacks_mono (IlgenCallbacksToMono *callbacks)
{
}

static MonoComponentMarshalIlgen component_func_table = {
	{ MONO_COMPONENT_ITF_VERSION, &marshal_ilgen_available },
	mono_component_marshal_ilgen_stub_init,
	stub_emit_marshal_ilgen,
	stub_mono_marshal_ilgen_install_callbacks_mono
};

MonoComponentMarshalIlgen*
 mono_component_marshal_ilgen_init (void) 
{
	return &component_func_table;
}