#include "mono/component/marshal-ilgen.h"
#include "mono/component/marshal-ilgen-noilgen.h"

#ifndef ENABLE_ILGEN
static int
emit_marshal_array_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	MonoType *object_type = mono_get_object_type ();
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = object_type;
		break;
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		*conv_arg_type = int_type;
		break;
	}
	return conv_arg;
}

static int
emit_marshal_ptr_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		  MonoMarshalSpec *spec, int conv_arg,
		  MonoType **conv_arg_type, MarshalAction action)
{
	return conv_arg;
}
#endif

#if !defined(ENABLE_ILGEN) || defined(DISABLE_NONBLITTABLE)
static int
emit_marshal_vtype_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	return conv_arg;
}

static int
emit_marshal_string_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec,
					 int conv_arg, MonoType **conv_arg_type,
					 MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		*conv_arg_type = int_type;
		break;
	case MARSHAL_ACTION_MANAGED_CONV_IN:
		*conv_arg_type = int_type;
		break;
	}
	return conv_arg;
}

static int
emit_marshal_safehandle_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
			 MonoMarshalSpec *spec, int conv_arg,
			 MonoType **conv_arg_type, MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	if (action == MARSHAL_ACTION_CONV_IN)
		*conv_arg_type = int_type;
	return conv_arg;
}

static int
emit_marshal_handleref_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
			MonoMarshalSpec *spec, int conv_arg,
			MonoType **conv_arg_type, MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	if (action == MARSHAL_ACTION_CONV_IN)
		*conv_arg_type = int_type;
	return conv_arg;
}

static int
emit_marshal_object_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec,
		     int conv_arg, MonoType **conv_arg_type,
		     MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	if (action == MARSHAL_ACTION_CONV_IN)
		*conv_arg_type = int_type;
	return conv_arg;
}

static int
emit_marshal_variant_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		     MonoMarshalSpec *spec,
		     int conv_arg, MonoType **conv_arg_type,
		     MarshalAction action)
{
	g_assert_not_reached ();
}

static int
emit_marshal_asany_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	return conv_arg;
}

static int
emit_marshal_boolean_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		      MonoMarshalSpec *spec,
		      int conv_arg, MonoType **conv_arg_type,
		      MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	switch (action) {
	case MARSHAL_ACTION_CONV_IN:
		if (m_type_is_byref (t))
			*conv_arg_type = int_type;
		else
			*conv_arg_type = mono_marshal_boolean_conv_in_get_local_type (spec, NULL);
		break;

	case MARSHAL_ACTION_MANAGED_CONV_IN: {
		MonoClass* conv_arg_class = mono_marshal_boolean_managed_conv_in_get_conv_arg_class (spec, NULL);
		if (m_type_is_byref (t))
			*conv_arg_type = m_class_get_this_arg (conv_arg_class);
		else
			*conv_arg_type = m_class_get_byval_arg (conv_arg_class);
		break;
	}

	}
	return conv_arg;
}

static int
emit_marshal_char_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
		   MonoMarshalSpec *spec, int conv_arg,
		   MonoType **conv_arg_type, MarshalAction action)
{
	return conv_arg;
}

static int
emit_marshal_custom_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					 MonoMarshalSpec *spec,
					 int conv_arg, MonoType **conv_arg_type,
					 MarshalAction action)
{
	MonoType *int_type = mono_get_int_type ();
	if (action == MARSHAL_ACTION_CONV_IN && t->type == MONO_TYPE_VALUETYPE)
		*conv_arg_type = int_type;
	return conv_arg;
}
#endif

#ifndef ENABLE_ILGEN

void
mono_marshal_noilgen_init_heavyweight (void)
{
	MonoMarshalILgenCallbacks ilgen_cb;

	ilgen_cb.version = MONO_MARSHAL_CALLBACKS_VERSION;
	ilgen_cb.emit_marshal_array = emit_marshal_array_noilgen;
	ilgen_cb.emit_marshal_vtype = emit_marshal_vtype_noilgen;
	ilgen_cb.emit_marshal_string = emit_marshal_string_noilgen;
	ilgen_cb.emit_marshal_safehandle = emit_marshal_safehandle_noilgen;
	ilgen_cb.emit_marshal_handleref = emit_marshal_handleref_noilgen;
	ilgen_cb.emit_marshal_object = emit_marshal_object_noilgen;
	ilgen_cb.emit_marshal_variant = emit_marshal_variant_noilgen;
	ilgen_cb.emit_marshal_asany = emit_marshal_asany_noilgen;
	ilgen_cb.emit_marshal_boolean = emit_marshal_boolean_noilgen;
	ilgen_cb.emit_marshal_custom = emit_marshal_custom_noilgen;
	ilgen_cb.emit_marshal_ptr = emit_marshal_ptr_noilgen;

	ilgen_cb.emit_marshal_char = emit_marshal_char_noilgen;
	mono_install_marshal_callbacks_ilgen(&ilgen_cb);
}

#endif