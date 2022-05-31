#include "config.h"

#include <mono/metadata/attrdefs.h>
#include "metadata/marshal-internals.h"
#include "metadata/marshal.h"
#include "metadata/marshal-ilgen.h"
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

static int
emit_marshal_scalar_noilgen (EmitMarshalContext *m, int argnum, MonoType *t,
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
#endif

#ifndef ENABLE_ILGEN
static void
emit_managed_wrapper_noilgen (MonoMethodBuilder *mb, MonoMethodSignature *invoke_sig, MonoMarshalSpec **mspecs, EmitMarshalContext* m, MonoMethod *method, MonoGCHandle target_handle, MonoError *error)
{
	MonoMethodSignature *sig, *csig;
	int i;
	MonoType *int_type = mono_get_int_type ();

	sig = m->sig;
	csig = m->csig;

	/* we first do all conversions */
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];

		switch (t->type) {
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_STRING:
		case MONO_TYPE_BOOLEAN:
			mono_emit_marshal (m, i, sig->params [i], mspecs [i + 1], 0, &csig->params [i], MARSHAL_ACTION_MANAGED_CONV_IN);
		}
	}

	if (!m_type_is_byref (sig->ret)) {
		switch (sig->ret->type) {
		case MONO_TYPE_STRING:
			csig->ret = int_type;
			break;
		default:
			break;
		}
	}
}

static void
emit_synchronized_wrapper_noilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoGenericContext *ctx, MonoGenericContainer *container, MonoMethod *enter_method, MonoMethod *exit_method, MonoMethod *gettypefromhandle_method)
{
	if (m_class_is_valuetype (method->klass) && !(method->flags & MONO_METHOD_ATTR_STATIC)) {
		/* FIXME Is this really the best way to signal an error here?  Isn't this called much later after class setup? -AK */
		mono_class_set_type_load_failure (method->klass, "");
		return;
	}

}

static void
emit_delegate_begin_invoke_noilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
}

static void
emit_delegate_end_invoke_noilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig)
{
}

static void
mb_skip_visibility_noilgen (MonoMethodBuilder *mb)
{
}

static void
mb_set_dynamic_noilgen (MonoMethodBuilder *mb)
{
}

static void
mb_emit_exception_noilgen (MonoMethodBuilder *mb, const char *exc_nspace, const char *exc_name, const char *msg)
{
}

static void
emit_marshal_directive_exception_noilgen (EmitMarshalContext *m, int argnum, const char* msg)
{
}

static void
mb_emit_exception_for_error_noilgen (MonoMethodBuilder *mb, const MonoError *error)
{
}

static void
emit_delegate_invoke_internal_noilgen (MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodSignature *invoke_sig, gboolean static_method_with_first_arg_bound, gboolean callvirt, gboolean closed_over_null, MonoMethod *method, MonoMethod *target_method, MonoClass *target_class, MonoGenericContext *ctx, MonoGenericContainer *container)
{
}

static void
emit_runtime_invoke_body_noilgen (MonoMethodBuilder *mb, const char **param_names, MonoImage *image, MonoMethod *method,
						  MonoMethodSignature *sig, MonoMethodSignature *callsig,
						  gboolean virtual_, gboolean need_direct_wrapper)
{
}

static void
emit_runtime_invoke_dynamic_noilgen (MonoMethodBuilder *mb)
{
}

static void
emit_icall_wrapper_noilgen (MonoMethodBuilder *mb, MonoJitICallInfo *callinfo, MonoMethodSignature *csig2, gboolean check_exceptions)
{
}

static void
emit_return_noilgen (MonoMethodBuilder *mb)
{
}

static void
emit_create_string_hack_noilgen (MonoMethodBuilder *mb, MonoMethodSignature *csig, MonoMethod *res)
{
}

static void
emit_native_icall_wrapper_noilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig, gboolean check_exceptions, gboolean aot, MonoMethodPInvoke *pinfo)
{
}

static void
emit_vtfixup_ftnptr_noilgen (MonoMethodBuilder *mb, MonoMethod *method, int param_count, guint16 type)
{
}

static void
emit_castclass_noilgen (MonoMethodBuilder *mb)
{
}

static void
emit_isinst_noilgen (MonoMethodBuilder *mb)
{
}

static void
emit_struct_to_ptr_noilgen (MonoMethodBuilder *mb, MonoClass *klass)
{
}

static void
emit_ptr_to_struct_noilgen (MonoMethodBuilder *mb, MonoClass *klass)
{
}

static void
emit_unbox_wrapper_noilgen (MonoMethodBuilder *mb, MonoMethod *method)
{
}

static void
emit_virtual_stelemref_noilgen (MonoMethodBuilder *mb, const char **param_names, MonoStelemrefKind kind)
{
}

static void
emit_stelemref_noilgen (MonoMethodBuilder *mb)
{
}

static void
mb_emit_byte_noilgen (MonoMethodBuilder *mb, guint8 op)
{
}

static void
emit_array_address_noilgen (MonoMethodBuilder *mb, int rank, int elem_size)
{
}

static void
emit_array_accessor_wrapper_noilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *sig, MonoGenericContext *ctx)
{
}

static void
emit_generic_array_helper_noilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig)
{
}

static void
emit_thunk_invoke_wrapper_noilgen (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *csig)
{
}

static void
emit_native_wrapper_noilgen (MonoImage *image, MonoMethodBuilder *mb, MonoMethodSignature *sig, MonoMethodPInvoke *piinfo, MonoMarshalSpec **mspecs, gpointer func, MonoNativeWrapperFlags flags)
{
}

void
mono_marshal_noilgen_init_lightweight (void)
{
	MonoMarshalLightweightCallbacks lightweight_cb;

	lightweight_cb.version = MONO_MARSHAL_CALLBACKS_VERSION;
	lightweight_cb.emit_marshal_scalar = emit_marshal_scalar_noilgen;
	lightweight_cb.emit_castclass = emit_castclass_noilgen;
	lightweight_cb.emit_struct_to_ptr = emit_struct_to_ptr_noilgen;
	lightweight_cb.emit_ptr_to_struct = emit_ptr_to_struct_noilgen;
	lightweight_cb.emit_isinst = emit_isinst_noilgen;
	lightweight_cb.emit_virtual_stelemref = emit_virtual_stelemref_noilgen;
	lightweight_cb.emit_stelemref = emit_stelemref_noilgen;
	lightweight_cb.emit_array_address = emit_array_address_noilgen;
	lightweight_cb.emit_native_wrapper = emit_native_wrapper_noilgen;
	lightweight_cb.emit_managed_wrapper = emit_managed_wrapper_noilgen;
	lightweight_cb.emit_runtime_invoke_body = emit_runtime_invoke_body_noilgen;
	lightweight_cb.emit_runtime_invoke_dynamic = emit_runtime_invoke_dynamic_noilgen;
	lightweight_cb.emit_delegate_begin_invoke = emit_delegate_begin_invoke_noilgen;
	lightweight_cb.emit_delegate_end_invoke = emit_delegate_end_invoke_noilgen;
	lightweight_cb.emit_delegate_invoke_internal = emit_delegate_invoke_internal_noilgen;
	lightweight_cb.emit_synchronized_wrapper = emit_synchronized_wrapper_noilgen;
	lightweight_cb.emit_unbox_wrapper = emit_unbox_wrapper_noilgen;
	lightweight_cb.emit_array_accessor_wrapper = emit_array_accessor_wrapper_noilgen;
	lightweight_cb.emit_generic_array_helper = emit_generic_array_helper_noilgen;
	lightweight_cb.emit_thunk_invoke_wrapper = emit_thunk_invoke_wrapper_noilgen;
	lightweight_cb.emit_create_string_hack = emit_create_string_hack_noilgen;
	lightweight_cb.emit_native_icall_wrapper = emit_native_icall_wrapper_noilgen;
	lightweight_cb.emit_icall_wrapper = emit_icall_wrapper_noilgen;
	lightweight_cb.emit_return = emit_return_noilgen;
	lightweight_cb.emit_vtfixup_ftnptr = emit_vtfixup_ftnptr_noilgen;
	lightweight_cb.mb_skip_visibility = mb_skip_visibility_noilgen;
	lightweight_cb.mb_set_dynamic = mb_set_dynamic_noilgen;
	lightweight_cb.mb_emit_exception = mb_emit_exception_noilgen;
	lightweight_cb.mb_emit_exception_for_error = mb_emit_exception_for_error_noilgen;
	lightweight_cb.emit_marshal_directive_exception = emit_marshal_directive_exception_noilgen;
	lightweight_cb.mb_emit_byte = mb_emit_byte_noilgen;

	mono_install_marshal_callbacks (&lightweight_cb);

}

void
mono_marshal_noilgen_init_heavyweight (void)
{
	MonoMarshalIlgenCallbacks ilgen_cb;

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

#else
void
mono_marshal_noilgen_init_lightweight (void)
{
}

void
mono_marshal_noilgen_init_heavyweight (void)
{
}
#endif

#ifdef DISABLE_NONBLITTABLE
void
mono_marshal_noilgen_init_blittable (MonoMarshalCallbacks *cb)
{
	cb->emit_marshal_boolean = emit_marshal_boolean_noilgen;
	cb->emit_marshal_char = emit_marshal_char_noilgen;
	cb->emit_marshal_custom = emit_marshal_custom_noilgen;
	cb->emit_marshal_asany = emit_marshal_asany_noilgen;
	cb->emit_marshal_vtype = emit_marshal_vtype_noilgen;
	cb->emit_marshal_string = emit_marshal_string_noilgen;
	cb->emit_marshal_safehandle = emit_marshal_safehandle_noilgen;
	cb->emit_marshal_handleref = emit_marshal_handleref_noilgen;
	cb->emit_marshal_object = emit_marshal_object_noilgen;
	cb->emit_marshal_variant = emit_marshal_variant_noilgen;
}
#else
void
mono_marshal_noilgen_init_blittable (MonoMarshalLightweightCallbacks *cb)
{
}
#endif
