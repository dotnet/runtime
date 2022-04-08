#include "config.h"

#include "metadata/attrdefs.h"
#include "metadata/marshal-internals.h"
#include "metadata/marshal.h"
#include "metadata/marshal-legacy.h"
#include "utils/mono-compiler.h"

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

static int
emit_marshal_asany_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
					MonoMarshalSpec *spec,
					int conv_arg, MonoType **conv_arg_type,
					MarshalAction action)
{
	return conv_arg;
}

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

void
mono_marshal_noilgen_legacy_init (void)
{
	MonoMarshalCallbacksLegacy legacy_cb;

	legacy_cb.emit_marshal_array = emit_marshal_array_noilgen;
	legacy_cb.emit_marshal_ptr = emit_marshal_ptr_noilgen;
	legacy_cb.emit_marshal_boolean = emit_marshal_boolean_noilgen;
	legacy_cb.emit_marshal_char = emit_marshal_char_noilgen;
	legacy_cb.emit_marshal_custom = emit_marshal_custom_noilgen;
	legacy_cb.emit_marshal_asany = emit_marshal_asany_noilgen;
	legacy_cb.emit_marshal_vtype = emit_marshal_vtype_noilgen;
	legacy_cb.emit_marshal_string = emit_marshal_string_noilgen;
	legacy_cb.emit_marshal_safehandle = emit_marshal_safehandle_noilgen;
	legacy_cb.emit_marshal_handleref = emit_marshal_handleref_noilgen;
	legacy_cb.emit_marshal_object = emit_marshal_object_noilgen;
	legacy_cb.emit_marshal_variant = emit_marshal_variant_noilgen;
}

#endif // if !defined(ENABLE_ILGEN) || defined(DISABLE_NONBLITTABLE)