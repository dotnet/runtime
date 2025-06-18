#include "interp-icalls.h"
#include <mono/metadata/mh_log.h>
#include <string.h>

typedef gint32 I4;
typedef gpointer I8;

gboolean is_scalar_vtype(MonoType* tp);

static gboolean
interp_type_as_ptr4 (MonoType *tp)
{
	if(sizeof(gpointer) == 4)
	{
		if (MONO_TYPE_IS_POINTER (tp))
			return TRUE;
		if (MONO_TYPE_IS_REFERENCE (tp))
			return TRUE;
	}
	if ((tp)->type == MONO_TYPE_I4)
		return TRUE;
	if ((tp)->type == MONO_TYPE_BOOLEAN)
		return TRUE;
	if ((tp)->type == MONO_TYPE_CHAR)
		return TRUE;
	if ((tp)->type == MONO_TYPE_VALUETYPE && m_class_is_enumtype (m_type_data_get_klass_unchecked (tp)))
		return TRUE;
	if (is_scalar_vtype (tp))
		return TRUE;
	return FALSE;
}

static gboolean
interp_type_as_ptr8 (MonoType *tp)
{
	if(sizeof(gpointer) != 8)
	{
		return false;
	}
	if (MONO_TYPE_IS_POINTER (tp))
		return TRUE;
	if (MONO_TYPE_IS_REFERENCE (tp))
		return TRUE;	
	if ((tp)->type == MONO_TYPE_I8 || (tp)->type == MONO_TYPE_U8)
		return TRUE;		
	if ((tp)->type == MONO_TYPE_R8)
		return TRUE;		
	 
	return FALSE;
}

static gboolean
interp_type_as_ptr(MonoType *tp)
{
	return (interp_type_as_ptr4(tp) || interp_type_as_ptr8(tp));
}
/* is_scalar_vtype taken from transform.c */
gboolean 
is_scalar_vtype (MonoType *type)
{
	MonoClass *klass;
	MonoClassField *field;
	gpointer iter;

	if (!MONO_TYPE_ISSTRUCT (type))
		return FALSE;
	klass = mono_class_from_mono_type_internal (type);
	mono_class_init_internal (klass);

	int size = mono_class_value_size (klass, NULL);
	if (size == 0 || size > SIZEOF_VOID_P)
		return FALSE;

	iter = NULL;
	int nfields = 0;
	field = NULL;
	while ((field = mono_class_get_fields_internal (klass, &iter))) {
		if (field->type->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;
		nfields ++;
		if (nfields > 1)
			return FALSE;
		MonoType *t = mini_get_underlying_type (field->type);
		if (!interp_type_as_ptr (t))
			return FALSE;
	}

	return TRUE;
}
void
stackval_from_data (MonoType *type, stackval *result, const void *data, gboolean pinvoke)
{
	MH_LOG_INDENT();
	MH_LOG("Converting data to stackval for type %s", mono_type_get_name (type));
	if (m_type_is_byref (type)) {
		result->data.p = *(gpointer*)data;
		return;
	}
	switch (type->type) {
	case MONO_TYPE_VOID:
		break;;
	case MONO_TYPE_I1:
		result->data.i = *(gint8*)data;
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		result->data.i = *(guint8*)data;
		break;
	case MONO_TYPE_I2:
		result->data.i = *(gint16*)data;
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		result->data.i = *(guint16*)data;
		break;
	case MONO_TYPE_I4:
		result->data.i = *(gint32*)data;
		break;
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		result->data.nati = *(mono_i*)data;
		break;
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		result->data.p = *(gpointer*)data;
		break;
	case MONO_TYPE_U4:
		result->data.i = *(guint32*)data;
		break;
	case MONO_TYPE_R4:
		/* memmove handles unaligned case */
		memmove (&result->data.f_r4, data, sizeof (float));
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		memmove (&result->data.l, data, sizeof (gint64));
		break;
	case MONO_TYPE_R8:
		memmove (&result->data.f, data, sizeof (double));
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		result->data.p = *(gpointer*)data;
		break;
	case MONO_TYPE_VALUETYPE:
		if (m_class_is_enumtype (m_type_data_get_klass_unchecked (type))) {
			stackval_from_data (mono_class_enum_basetype_internal (m_type_data_get_klass_unchecked (type)), result, data, pinvoke);
			break;
		} else {
			int size;
			if (pinvoke)
				size = mono_class_native_size (m_type_data_get_klass_unchecked (type), NULL);
			else
				size = mono_class_value_size (m_type_data_get_klass_unchecked (type), NULL);
			memcpy (result, data, size);
			break;
		}
	case MONO_TYPE_GENERICINST: {
		if (mono_type_generic_inst_is_valuetype (type)) {
			MonoClass *klass = mono_class_from_mono_type_internal (type);
			int size;
			if (pinvoke)
				size = mono_class_native_size (klass, NULL);
			else
				size = mono_class_value_size (klass, NULL);
			memcpy (result, data, size);
			break;
		}
		stackval_from_data (m_class_get_byval_arg (m_type_data_get_generic_class_unchecked (type)->container_class), result, data, pinvoke);
		break;
	}
	default:
		g_error ("got type 0x%02x", type->type);
	}
	MH_LOG("Converted data to stackval: %s", mono_type_get_name (type));
	MH_LOG_UNINDENT();
}
static char * log_sig(MonoMethodSignature* sig)
{
	char buffer[256];
	int offset = 0;
	for (int i = 0; i < sig->param_count; ++i) {
		MonoType* tp = sig->params[i];
		if(tp)
			offset += sprintf(buffer + offset, "%d", interp_type_as_ptr4(tp) ? 4 : interp_type_as_ptr8(tp) ? 8 : 0);
		else
			offset += sprintf(buffer + offset, "E");
	}
	offset += sprintf(buffer + offset, "_%s", sig->ret->type ==  MONO_TYPE_VOID ? "V" : (interp_type_as_ptr4(sig->ret) ? "4" : "8"));
	MH_LOG("Signature: %s", buffer);
	return strdup(buffer);
}
static void log_op(MintICallSig op)
{
	switch(op) {
	case MINT_ICALLSIG_V_V:
		MH_LOG("MINT_ICALLSIG_V_V");
		break;
	case MINT_ICALLSIG_V_P:
		MH_LOG("MINT_ICALLSIG_V_P");
		break;
	case MINT_ICALLSIG_P_V:
		MH_LOG("MINT_ICALLSIG_P_V");
		break;
	case MINT_ICALLSIG_P_P:
		MH_LOG("MINT_ICALLSIG_P_P");
		break;
	case MINT_ICALLSIG_PP_V:
		MH_LOG("MINT_ICALLSIG_PP_V");
		break;
	case MINT_ICALLSIG_PP_P:
		MH_LOG("MINT_ICALLSIG_PP_P");
		break;
	case MINT_ICALLSIG_PPP_V:
		MH_LOG("MINT_ICALLSIG_PPP_V");
		break;
	case MINT_ICALLSIG_PPP_P:
		MH_LOG("MINT_ICALLSIG_PPP_P");
		break;
	case MINT_ICALLSIG_PPPP_V:
		MH_LOG("MINT_ICALLSIG_PPPP_V");
		break;
	case MINT_ICALLSIG_PPPP_P:
		MH_LOG("MINT_ICALLSIG_PPPP_P");
		break;
	case MINT_ICALLSIG_PPPPP_V:
		MH_LOG("MINT_ICALLSIG_PPPPP_V");
		break;
	case MINT_ICALLSIG_PPPPP_P:
		MH_LOG("MINT_ICALLSIG_PPPPP_P");
		break;
	case MINT_ICALLSIG_PPPPPP_V:
		MH_LOG("MINT_ICALLSIG_PPPPPP_V");
		break;
	case MINT_ICALLSIG_PPPPPP_P:
		MH_LOG("MINT_ICALLSIG_PPPPPP_P");
		break;
	default:
		MH_LOG("Unknown MintICallSig: %d", op);
		break;
	}
}
void
do_icall (MonoMethodSignature *sig, MintICallSig op, stackval *ret_sp, stackval *sp, gpointer ptr, gboolean save_last_error)
{
	MH_LOG_INDENT();
	if (save_last_error)
		mono_marshal_clear_last_error ();
	log_op(op);
	char* sigTest = log_sig(sig);
	switch (op) {
	case MINT_ICALLSIG_V_V: {
		typedef void (*T)(void);		
		T func = (T)ptr;
        	func ();
		break;
	}
	case MINT_ICALLSIG_V_P: {
		if (interp_type_as_ptr4(sig->ret))
		{
			typedef I4 (*T)(void);
			T func = (T)ptr;
			ret_sp->data.i = func (); // note return directly into .i field (union)
		}
		else
		{
			typedef I8 (*T)(void);
			T func = (T)ptr;
			ret_sp->data.p = func ();
		}
		
		break;
	}
	case MINT_ICALLSIG_P_V: {
		typedef void (*T)(gpointer);		
		T func = (T)ptr;
		func (sp [0].data.p);
		break;
	}
	case MINT_ICALLSIG_P_P: {
		typedef gpointer (*T)(gpointer);		
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p);
		break;
	}
	case MINT_ICALLSIG_PP_V: {
		typedef void (*T)(gpointer,gpointer);		
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p);
		break;
	}
	case MINT_ICALLSIG_PP_P: {
		if(interp_type_as_ptr4(sig->ret))
		{
			if (interp_type_as_ptr4(sig->params[0]) && interp_type_as_ptr4(sig->params[1]))
			{
				typedef I4 (*T)(I4, I4);		
				T func = (T)ptr;
				ret_sp->data.i = func (sp [0].data.i, sp [1].data.i);
			}
			else if (interp_type_as_ptr4(sig->params[0]))
			{
				typedef I4 (*T)(I4, I8);		
				T func = (T)ptr;
				ret_sp->data.i = func (sp [0].data.i, sp [1].data.p);
			}
			else if (interp_type_as_ptr4(sig->params[1]))
			{
				typedef I4 (*T)(I8, I4);		
				T func = (T)ptr;
				ret_sp->data.i = func (sp [0].data.p, sp [1].data.i);
			}
			else
			{
				typedef I4 (*T)(I8, I8);		
				T func = (T)ptr;
				ret_sp->data.i = func (sp [0].data.p, sp [1].data.p);
			}			
		}
		else
		{
			if (interp_type_as_ptr4(sig->params[0]) && interp_type_as_ptr4(sig->params[1]))
			{
				typedef gpointer (*T)(I4, I4);		
				T func = (T)ptr;
				ret_sp->data.p = func (sp [0].data.i, sp [1].data.i);
			}
			else if (interp_type_as_ptr4(sig->params[0]))
			{
				typedef gpointer (*T)(I4, I8);		
				T func = (T)ptr;
				ret_sp->data.p = func (sp [0].data.i, sp [1].data.p);
			}
			else if (interp_type_as_ptr4(sig->params[1]))
			{
				typedef gpointer (*T)(I8, I4);		
				T func = (T)ptr;
				ret_sp->data.p = func (sp [0].data.p, sp [1].data.i);
			}
			else
			{
				typedef gpointer (*T)(I8, I8);		
				T func = (T)ptr;
				ret_sp->data.p = func (sp [0].data.p, sp [1].data.p);
			}
		}
		
		break;
	}
	case MINT_ICALLSIG_PPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer);		
		T func = (T)ptr;
		MH_LOG("Param count is %d, ptr is %p", sig->param_count, ptr);

		func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALLSIG_PPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPPP_V: {
		typedef void (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	case MINT_ICALLSIG_PPPPPP_P: {
		typedef gpointer (*T)(gpointer,gpointer,gpointer,gpointer,gpointer,gpointer);
		T func = (T)ptr;
		ret_sp->data.p = func (sp [0].data.p, sp [1].data.p, sp [2].data.p, sp [3].data.p, sp [4].data.p, sp [5].data.p);
		break;
	}
	default:
		g_assert_not_reached ();
	}

	if (save_last_error)
		mono_marshal_set_last_error ();

	/* convert the native representation to the stackval representation */
	if (sig)
		stackval_from_data (sig->ret, ret_sp, (char*) &ret_sp->data.p, sig->pinvoke && !sig->marshalling_disabled);
	if(sigTest)
		free(sigTest);
	MH_LOG("Returning from do_icall with ret_sp: %p", ret_sp->data.p);
	MH_LOG_UNINDENT();
}

