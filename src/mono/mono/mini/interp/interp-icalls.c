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
	// return true for value types that are NOT enums
	if ((tp)->type == MONO_TYPE_VALUETYPE && !m_class_is_enumtype (m_type_data_get_klass_unchecked (tp)))
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
	intptr_t data_ptr = *(intptr_t *)data;
	MH_LOG("Converting data to stackval for type %s: ,value as intptr_t is %p", mono_type_get_name (type), (void*)data_ptr);
	
//	memset(result, 0, sizeof(stackval));
	log_mono_type(type);
	if (m_type_is_byref (type)) {
		result->data.p = *(gpointer*)data;
		MH_LOG_UNINDENT();
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
		MH_LOG("Assigned U1 or BOOLEAN value assigned: %p", (void*)result->data.i);
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
		MH_LOG("Assigned I4 value assigned: (int) %d (ptr) %p", result->data.i, (void*)result->data.i);
		break;
	case MONO_TYPE_U:
	case MONO_TYPE_I:
		result->data.nati = *(mono_u*)data;
		break;
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		result->data.p = *(gpointer*)data;
		MH_LOG("Assigned pointer  assigned: %p", result->data.p);
		break;
	case MONO_TYPE_U4:		
		result->data.i = *(guint32*)data;
		MH_LOG("Assigned U4 value assigned: (int) %d (ptr) %p",  result->data.i, (void*)result->data.i);
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
		MH_LOG("Assigned pointer value: %p",  result->data.p);
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
			assert (size <= sizeof(stackval));
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
			assert (size <= sizeof(stackval));
			memcpy (result, data, size);
			break;
		}
		stackval_from_data (m_class_get_byval_arg (m_type_data_get_generic_class_unchecked (type)->container_class), result, data, pinvoke);
		break;
	}
	default:
		g_error ("got type 0x%02x", type->type);
	}
	MH_LOG("Converted data to stackval. Type: %s, ptr value (result->data.p): %p", mono_type_get_name (type), (void*)result->data.p);
	MH_LOG_UNINDENT();
}
static char * log_sig(MonoMethodSignature* sig)
{
	char buffer[256];
	int offset = 0;
	if (!sig) {
		MH_LOG("Signature is NULL");
		return NULL;
	}
	MH_LOG_INDENT();
	for (int i = 0; i < sig->param_count; ++i) {
		MonoType* tp = sig->params[i];		
		MH_LOG("Param %d: %s", (int)tp->type, mono_type_get_name (tp));
		if(tp)
			offset += sprintf(buffer + offset, "%d", interp_type_as_ptr8(tp) ? 8 : interp_type_as_ptr4(tp) ? 4 : 0);
		else
			offset += sprintf(buffer + offset, "E");
		
	}
	MH_LOG_UNINDENT();
	if (!sig->param_count)
		offset += sprintf(buffer + offset, "V");
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
	MH_LOG("Sig: %p op: %d ret_sp: %p sp: %p ptr: %p, save_last: %s", (void*)sig, op, (void*)ret_sp, (void*)sp, (void*)ptr, save_last_error ? "T" : "F");
	
	if (save_last_error)
		mono_marshal_clear_last_error ();
	log_op(op);
	// FIXME: this string/cookie comparison is rubbish - use an enum
	char* sigTest = log_sig(sig); // currently must be called to get the cookie!
	MH_LOG("Got signature cookie %s", sigTest);
	if (!sigTest)
	{
		// can't do this. need to handle no sig
		memset(&ret_sp->data, 0, sizeof(stackval));
	}
	else
	{	
		switch (op) {
		case MINT_ICALLSIG_V_V:
		case MINT_ICALLSIG_V_P: {
			if (!strcmp(sigTest,"V_V")) {
				typedef void (*T)(void);
				T func = (T)ptr;
				func();
			};
			if (!strcmp(sigTest,"V_4")) {
				typedef I4 (*T)(void);
				T func = (T)ptr;
				ret_sp->data.i = func();
			};
			if (!strcmp(sigTest,"V_8")) {
				typedef I8 (*T)(void);
				T func = (T)ptr;
				ret_sp->data.p = func();
			};
		break;
		}
		case MINT_ICALLSIG_P_V:
		case MINT_ICALLSIG_P_P: {
			if (!strcmp(sigTest,"4_V")) {
				typedef void (*T)(I4);
				T func = (T)ptr;
				func(sp[0].data.i);
			};
			if (!strcmp(sigTest,"8_V")) {
				typedef void (*T)(I8);
				T func = (T)ptr;
				func(sp[0].data.p);
			};
			if (!strcmp(sigTest,"4_4")) {
				typedef I4 (*T)(I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i);
			};
			if (!strcmp(sigTest,"8_4")) {
				typedef I4 (*T)(I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p);
			};
			if (!strcmp(sigTest,"4_8")) {
				typedef I8 (*T)(I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i);
			};
			if (!strcmp(sigTest,"8_8")) {
				typedef I8 (*T)(I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p);
			};
		break;
		}
		case MINT_ICALLSIG_PP_V:
		case MINT_ICALLSIG_PP_P: {
			if (!strcmp(sigTest,"44_V")) {
				typedef void (*T)(I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i);
			};
			if (!strcmp(sigTest,"48_V")) {
				typedef void (*T)(I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p);
			};
			if (!strcmp(sigTest,"84_V")) {
				typedef void (*T)(I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i);
			};
			if (!strcmp(sigTest,"88_V")) {
				typedef void (*T)(I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p);
			};
			if (!strcmp(sigTest,"44_4")) {
				typedef I4 (*T)(I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i);
			};
			if (!strcmp(sigTest,"48_4")) {
				typedef I4 (*T)(I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p);
			};
			if (!strcmp(sigTest,"84_4")) {
				typedef I4 (*T)(I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i);
			};
			if (!strcmp(sigTest,"88_4")) {
				typedef I4 (*T)(I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p);
			};
			if (!strcmp(sigTest,"44_8")) {
				typedef I8 (*T)(I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i);
			};
			if (!strcmp(sigTest,"48_8")) {
				typedef I8 (*T)(I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p);
			};
			if (!strcmp(sigTest,"84_8")) {
				typedef I8 (*T)(I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i);
			};
			if (!strcmp(sigTest,"88_8")) {
				typedef I8 (*T)(I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p);
			};
		break;
		}
		case MINT_ICALLSIG_PPP_V:
		case MINT_ICALLSIG_PPP_P: {
			if (!strcmp(sigTest,"444_V")) {
				typedef void (*T)(I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i);
			};
			if (!strcmp(sigTest,"448_V")) {
				typedef void (*T)(I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p);
			};
			if (!strcmp(sigTest,"484_V")) {
				typedef void (*T)(I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i);
			};
			if (!strcmp(sigTest,"488_V")) {
				typedef void (*T)(I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p);
			};
			if (!strcmp(sigTest,"844_V")) {
				typedef void (*T)(I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i);
			};
			if (!strcmp(sigTest,"848_V")) {
				typedef void (*T)(I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p);
			};
			if (!strcmp(sigTest,"884_V")) {
				typedef void (*T)(I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i);
			};
			if (!strcmp(sigTest,"888_V")) {
				typedef void (*T)(I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p);
			};
			if (!strcmp(sigTest,"444_4")) {
				typedef I4 (*T)(I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i);
			};
			if (!strcmp(sigTest,"448_4")) {
				typedef I4 (*T)(I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p);
			};
			if (!strcmp(sigTest,"484_4")) {
				typedef I4 (*T)(I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i);
			};
			if (!strcmp(sigTest,"488_4")) {
				typedef I4 (*T)(I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p);
			};
			if (!strcmp(sigTest,"844_4")) {
				typedef I4 (*T)(I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i);
			};
			if (!strcmp(sigTest,"848_4")) {
				typedef I4 (*T)(I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p);
			};
			if (!strcmp(sigTest,"884_4")) {
				typedef I4 (*T)(I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i);
			};
			if (!strcmp(sigTest,"888_4")) {
				typedef I4 (*T)(I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p);
			};
			if (!strcmp(sigTest,"444_8")) {
				typedef I8 (*T)(I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i);
			};
			if (!strcmp(sigTest,"448_8")) {
				typedef I8 (*T)(I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p);
			};
			if (!strcmp(sigTest,"484_8")) {
				typedef I8 (*T)(I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i);
			};
			if (!strcmp(sigTest,"488_8")) {
				typedef I8 (*T)(I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p);
			};
			if (!strcmp(sigTest,"844_8")) {
				typedef I8 (*T)(I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i);
			};
			if (!strcmp(sigTest,"848_8")) {
				typedef I8 (*T)(I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p);
			};
			if (!strcmp(sigTest,"884_8")) {
				typedef I8 (*T)(I8,I8,I4);
				T func = (T)ptr;
				MH_LOG("Calling 884_8 func with: %p %p %d", (void*)sp[0].data.p, (void*)sp[1].data.p, sp[2].data.i);
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i);
			};
			if (!strcmp(sigTest,"888_8")) {
				typedef I8 (*T)(I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p);
			};
		break;
		}
		case MINT_ICALLSIG_PPPP_V:
		case MINT_ICALLSIG_PPPP_P: {
			if (!strcmp(sigTest,"4444_V")) {
				typedef void (*T)(I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4448_V")) {
				typedef void (*T)(I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4484_V")) {
				typedef void (*T)(I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4488_V")) {
				typedef void (*T)(I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4844_V")) {
				typedef void (*T)(I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4848_V")) {
				typedef void (*T)(I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4884_V")) {
				typedef void (*T)(I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4888_V")) {
				typedef void (*T)(I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8444_V")) {
				typedef void (*T)(I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8448_V")) {
				typedef void (*T)(I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8484_V")) {
				typedef void (*T)(I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8488_V")) {
				typedef void (*T)(I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8844_V")) {
				typedef void (*T)(I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8848_V")) {
				typedef void (*T)(I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8884_V")) {
				typedef void (*T)(I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8888_V")) {
				typedef void (*T)(I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4444_4")) {
				typedef I4 (*T)(I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4448_4")) {
				typedef I4 (*T)(I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4484_4")) {
				typedef I4 (*T)(I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4488_4")) {
				typedef I4 (*T)(I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4844_4")) {
				typedef I4 (*T)(I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4848_4")) {
				typedef I4 (*T)(I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4884_4")) {
				typedef I4 (*T)(I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4888_4")) {
				typedef I4 (*T)(I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8444_4")) {
				typedef I4 (*T)(I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8448_4")) {
				typedef I4 (*T)(I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8484_4")) {
				typedef I4 (*T)(I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8488_4")) {
				typedef I4 (*T)(I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8844_4")) {
				typedef I4 (*T)(I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8848_4")) {
				typedef I4 (*T)(I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8884_4")) {
				typedef I4 (*T)(I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8888_4")) {
				typedef I4 (*T)(I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4444_8")) {
				typedef I8 (*T)(I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4448_8")) {
				typedef I8 (*T)(I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4484_8")) {
				typedef I8 (*T)(I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4488_8")) {
				typedef I8 (*T)(I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4844_8")) {
				typedef I8 (*T)(I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4848_8")) {
				typedef I8 (*T)(I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"4884_8")) {
				typedef I8 (*T)(I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"4888_8")) {
				typedef I8 (*T)(I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8444_8")) {
				typedef I8 (*T)(I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8448_8")) {
				typedef I8 (*T)(I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8484_8")) {
				typedef I8 (*T)(I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8488_8")) {
				typedef I8 (*T)(I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8844_8")) {
				typedef I8 (*T)(I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8848_8")) {
				typedef I8 (*T)(I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p);
			};
			if (!strcmp(sigTest,"8884_8")) {
				typedef I8 (*T)(I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i);
			};
			if (!strcmp(sigTest,"8888_8")) {
				typedef I8 (*T)(I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p);
			};
		break;
		}
		case MINT_ICALLSIG_PPPPP_V:
		case MINT_ICALLSIG_PPPPP_P: {
			//log_sig(sig);  // just here for a breakpoint
			if (!strcmp(sigTest,"44444_V")) {
				typedef void (*T)(I4,I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44448_V")) {
				typedef void (*T)(I4,I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44484_V")) {
				typedef void (*T)(I4,I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44488_V")) {
				typedef void (*T)(I4,I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44844_V")) {
				typedef void (*T)(I4,I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44848_V")) {
				typedef void (*T)(I4,I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44884_V")) {
				typedef void (*T)(I4,I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44888_V")) {
				typedef void (*T)(I4,I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48444_V")) {
				typedef void (*T)(I4,I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48448_V")) {
				typedef void (*T)(I4,I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48484_V")) {
				typedef void (*T)(I4,I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48488_V")) {
				typedef void (*T)(I4,I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48844_V")) {
				typedef void (*T)(I4,I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48848_V")) {
				typedef void (*T)(I4,I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48884_V")) {
				typedef void (*T)(I4,I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48888_V")) {
				typedef void (*T)(I4,I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84444_V")) {
				typedef void (*T)(I8,I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84448_V")) {
				typedef void (*T)(I8,I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84484_V")) {
				typedef void (*T)(I8,I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84488_V")) {
				typedef void (*T)(I8,I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84844_V")) {
				typedef void (*T)(I8,I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84848_V")) {
				typedef void (*T)(I8,I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84884_V")) {
				typedef void (*T)(I8,I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84888_V")) {
				typedef void (*T)(I8,I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88444_V")) {
				typedef void (*T)(I8,I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88448_V")) {
				typedef void (*T)(I8,I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88484_V")) {
				typedef void (*T)(I8,I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88488_V")) {
				typedef void (*T)(I8,I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88844_V")) {
				typedef void (*T)(I8,I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88848_V")) {
				typedef void (*T)(I8,I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88884_V")) {
				typedef void (*T)(I8,I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88888_V")) {
				typedef void (*T)(I8,I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44444_4")) {
				typedef I4 (*T)(I4,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44448_4")) {
				typedef I4 (*T)(I4,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44484_4")) {
				typedef I4 (*T)(I4,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44488_4")) {
				typedef I4 (*T)(I4,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44844_4")) {
				typedef I4 (*T)(I4,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44848_4")) {
				typedef I4 (*T)(I4,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44884_4")) {
				typedef I4 (*T)(I4,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44888_4")) {
				typedef I4 (*T)(I4,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48444_4")) {
				typedef I4 (*T)(I4,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48448_4")) {
				typedef I4 (*T)(I4,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48484_4")) {
				typedef I4 (*T)(I4,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48488_4")) {
				typedef I4 (*T)(I4,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48844_4")) {
				typedef I4 (*T)(I4,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48848_4")) {
				typedef I4 (*T)(I4,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48884_4")) {
				typedef I4 (*T)(I4,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48888_4")) {
				typedef I4 (*T)(I4,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84444_4")) {
				typedef I4 (*T)(I8,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84448_4")) {
				typedef I4 (*T)(I8,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84484_4")) {
				typedef I4 (*T)(I8,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84488_4")) {
				typedef I4 (*T)(I8,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84844_4")) {
				typedef I4 (*T)(I8,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84848_4")) {
				typedef I4 (*T)(I8,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84884_4")) {
				typedef I4 (*T)(I8,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84888_4")) {
				typedef I4 (*T)(I8,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88444_4")) {
				typedef I4 (*T)(I8,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88448_4")) {
				typedef I4 (*T)(I8,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88484_4")) {
				typedef I4 (*T)(I8,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88488_4")) {
				typedef I4 (*T)(I8,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88844_4")) {
				typedef I4 (*T)(I8,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88848_4")) {
				typedef I4 (*T)(I8,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88884_4")) {
				typedef I4 (*T)(I8,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88888_4")) {
				typedef I4 (*T)(I8,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44444_8")) {
				typedef I8 (*T)(I4,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44448_8")) {
				typedef I8 (*T)(I4,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44484_8")) {
				typedef I8 (*T)(I4,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44488_8")) {
				typedef I8 (*T)(I4,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44844_8")) {
				typedef I8 (*T)(I4,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44848_8")) {
				typedef I8 (*T)(I4,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"44884_8")) {
				typedef I8 (*T)(I4,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"44888_8")) {
				typedef I8 (*T)(I4,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48444_8")) {
				typedef I8 (*T)(I4,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48448_8")) {
				typedef I8 (*T)(I4,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48484_8")) {
				typedef I8 (*T)(I4,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48488_8")) {
				typedef I8 (*T)(I4,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48844_8")) {
				typedef I8 (*T)(I4,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48848_8")) {
				typedef I8 (*T)(I4,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"48884_8")) {
				typedef I8 (*T)(I4,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"48888_8")) {
				typedef I8 (*T)(I4,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84444_8")) {
				typedef I8 (*T)(I8,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84448_8")) {
				typedef I8 (*T)(I8,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84484_8")) {
				typedef I8 (*T)(I8,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84488_8")) {
				typedef I8 (*T)(I8,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84844_8")) {
				typedef I8 (*T)(I8,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84848_8")) {
				typedef I8 (*T)(I8,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"84884_8")) {
				typedef I8 (*T)(I8,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"84888_8")) {
				typedef I8 (*T)(I8,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88444_8")) {
				typedef I8 (*T)(I8,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88448_8")) {
				typedef I8 (*T)(I8,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88484_8")) {
				typedef I8 (*T)(I8,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88488_8")) {
				typedef I8 (*T)(I8,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88844_8")) {
				typedef I8 (*T)(I8,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88848_8")) {
				typedef I8 (*T)(I8,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p);
			};
			if (!strcmp(sigTest,"88884_8")) {
				typedef I8 (*T)(I8,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i);
			};
			if (!strcmp(sigTest,"88888_8")) {
				typedef I8 (*T)(I8,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p);
			};
		break;
		}
		case MINT_ICALLSIG_PPPPPP_V:
		case MINT_ICALLSIG_PPPPPP_P: {
			//log_sig(sig);  // just here for a breakpoint
			if (!strcmp(sigTest,"444444_V")) {
				typedef void (*T)(I4,I4,I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444448_V")) {
				typedef void (*T)(I4,I4,I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444484_V")) {
				typedef void (*T)(I4,I4,I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444488_V")) {
				typedef void (*T)(I4,I4,I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444844_V")) {
				typedef void (*T)(I4,I4,I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444848_V")) {
				typedef void (*T)(I4,I4,I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444884_V")) {
				typedef void (*T)(I4,I4,I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444888_V")) {
				typedef void (*T)(I4,I4,I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448444_V")) {
				typedef void (*T)(I4,I4,I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448448_V")) {
				typedef void (*T)(I4,I4,I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448484_V")) {
				typedef void (*T)(I4,I4,I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448488_V")) {
				typedef void (*T)(I4,I4,I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448844_V")) {
				typedef void (*T)(I4,I4,I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448848_V")) {
				typedef void (*T)(I4,I4,I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448884_V")) {
				typedef void (*T)(I4,I4,I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448888_V")) {
				typedef void (*T)(I4,I4,I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484444_V")) {
				typedef void (*T)(I4,I8,I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484448_V")) {
				typedef void (*T)(I4,I8,I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484484_V")) {
				typedef void (*T)(I4,I8,I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484488_V")) {
				typedef void (*T)(I4,I8,I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484844_V")) {
				typedef void (*T)(I4,I8,I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484848_V")) {
				typedef void (*T)(I4,I8,I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484884_V")) {
				typedef void (*T)(I4,I8,I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484888_V")) {
				typedef void (*T)(I4,I8,I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488444_V")) {
				typedef void (*T)(I4,I8,I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488448_V")) {
				typedef void (*T)(I4,I8,I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488484_V")) {
				typedef void (*T)(I4,I8,I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488488_V")) {
				typedef void (*T)(I4,I8,I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488844_V")) {
				typedef void (*T)(I4,I8,I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488848_V")) {
				typedef void (*T)(I4,I8,I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488884_V")) {
				typedef void (*T)(I4,I8,I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488888_V")) {
				typedef void (*T)(I4,I8,I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844444_V")) {
				typedef void (*T)(I8,I4,I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844448_V")) {
				typedef void (*T)(I8,I4,I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844484_V")) {
				typedef void (*T)(I8,I4,I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844488_V")) {
				typedef void (*T)(I8,I4,I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844844_V")) {
				typedef void (*T)(I8,I4,I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844848_V")) {
				typedef void (*T)(I8,I4,I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844884_V")) {
				typedef void (*T)(I8,I4,I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844888_V")) {
				typedef void (*T)(I8,I4,I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848444_V")) {
				typedef void (*T)(I8,I4,I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848448_V")) {
				typedef void (*T)(I8,I4,I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848484_V")) {
				typedef void (*T)(I8,I4,I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848488_V")) {
				typedef void (*T)(I8,I4,I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848844_V")) {
				typedef void (*T)(I8,I4,I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848848_V")) {
				typedef void (*T)(I8,I4,I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848884_V")) {
				typedef void (*T)(I8,I4,I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848888_V")) {
				typedef void (*T)(I8,I4,I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884444_V")) {
				typedef void (*T)(I8,I8,I4,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884448_V")) {
				typedef void (*T)(I8,I8,I4,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884484_V")) {
				typedef void (*T)(I8,I8,I4,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884488_V")) {
				typedef void (*T)(I8,I8,I4,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884844_V")) {
				typedef void (*T)(I8,I8,I4,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884848_V")) {
				typedef void (*T)(I8,I8,I4,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884884_V")) {
				typedef void (*T)(I8,I8,I4,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884888_V")) {
				typedef void (*T)(I8,I8,I4,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888444_V")) {
				typedef void (*T)(I8,I8,I8,I4,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888448_V")) {
				typedef void (*T)(I8,I8,I8,I4,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888484_V")) {
				typedef void (*T)(I8,I8,I8,I4,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888488_V")) {
				typedef void (*T)(I8,I8,I8,I4,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888844_V")) {
				typedef void (*T)(I8,I8,I8,I8,I4,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888848_V")) {
				typedef void (*T)(I8,I8,I8,I8,I4,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888884_V")) {
				typedef void (*T)(I8,I8,I8,I8,I8,I4);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888888_V")) {
				typedef void (*T)(I8,I8,I8,I8,I8,I8);
				T func = (T)ptr;
				func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444444_4")) {
				typedef I4 (*T)(I4,I4,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444448_4")) {
				typedef I4 (*T)(I4,I4,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444484_4")) {
				typedef I4 (*T)(I4,I4,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444488_4")) {
				typedef I4 (*T)(I4,I4,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444844_4")) {
				typedef I4 (*T)(I4,I4,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444848_4")) {
				typedef I4 (*T)(I4,I4,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444884_4")) {
				typedef I4 (*T)(I4,I4,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444888_4")) {
				typedef I4 (*T)(I4,I4,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448444_4")) {
				typedef I4 (*T)(I4,I4,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448448_4")) {
				typedef I4 (*T)(I4,I4,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448484_4")) {
				typedef I4 (*T)(I4,I4,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448488_4")) {
				typedef I4 (*T)(I4,I4,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448844_4")) {
				typedef I4 (*T)(I4,I4,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448848_4")) {
				typedef I4 (*T)(I4,I4,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448884_4")) {
				typedef I4 (*T)(I4,I4,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448888_4")) {
				typedef I4 (*T)(I4,I4,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484444_4")) {
				typedef I4 (*T)(I4,I8,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484448_4")) {
				typedef I4 (*T)(I4,I8,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484484_4")) {
				typedef I4 (*T)(I4,I8,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484488_4")) {
				typedef I4 (*T)(I4,I8,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484844_4")) {
				typedef I4 (*T)(I4,I8,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484848_4")) {
				typedef I4 (*T)(I4,I8,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484884_4")) {
				typedef I4 (*T)(I4,I8,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484888_4")) {
				typedef I4 (*T)(I4,I8,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488444_4")) {
				typedef I4 (*T)(I4,I8,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488448_4")) {
				typedef I4 (*T)(I4,I8,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488484_4")) {
				typedef I4 (*T)(I4,I8,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488488_4")) {
				typedef I4 (*T)(I4,I8,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488844_4")) {
				typedef I4 (*T)(I4,I8,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488848_4")) {
				typedef I4 (*T)(I4,I8,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488884_4")) {
				typedef I4 (*T)(I4,I8,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488888_4")) {
				typedef I4 (*T)(I4,I8,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844444_4")) {
				typedef I4 (*T)(I8,I4,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844448_4")) {
				typedef I4 (*T)(I8,I4,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844484_4")) {
				typedef I4 (*T)(I8,I4,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844488_4")) {
				typedef I4 (*T)(I8,I4,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844844_4")) {
				typedef I4 (*T)(I8,I4,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844848_4")) {
				typedef I4 (*T)(I8,I4,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844884_4")) {
				typedef I4 (*T)(I8,I4,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844888_4")) {
				typedef I4 (*T)(I8,I4,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848444_4")) {
				typedef I4 (*T)(I8,I4,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848448_4")) {
				typedef I4 (*T)(I8,I4,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848484_4")) {
				typedef I4 (*T)(I8,I4,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848488_4")) {
				typedef I4 (*T)(I8,I4,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848844_4")) {
				typedef I4 (*T)(I8,I4,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848848_4")) {
				typedef I4 (*T)(I8,I4,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848884_4")) {
				typedef I4 (*T)(I8,I4,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848888_4")) {
				typedef I4 (*T)(I8,I4,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884444_4")) {
				typedef I4 (*T)(I8,I8,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884448_4")) {
				typedef I4 (*T)(I8,I8,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884484_4")) {
				typedef I4 (*T)(I8,I8,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884488_4")) {
				typedef I4 (*T)(I8,I8,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884844_4")) {
				typedef I4 (*T)(I8,I8,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884848_4")) {
				typedef I4 (*T)(I8,I8,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884884_4")) {
				typedef I4 (*T)(I8,I8,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884888_4")) {
				typedef I4 (*T)(I8,I8,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888444_4")) {
				typedef I4 (*T)(I8,I8,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888448_4")) {
				typedef I4 (*T)(I8,I8,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888484_4")) {
				typedef I4 (*T)(I8,I8,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888488_4")) {
				typedef I4 (*T)(I8,I8,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888844_4")) {
				typedef I4 (*T)(I8,I8,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888848_4")) {
				typedef I4 (*T)(I8,I8,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888884_4")) {
				typedef I4 (*T)(I8,I8,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888888_4")) {
				typedef I4 (*T)(I8,I8,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.i = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444444_8")) {
				typedef I8 (*T)(I4,I4,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444448_8")) {
				typedef I8 (*T)(I4,I4,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444484_8")) {
				typedef I8 (*T)(I4,I4,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444488_8")) {
				typedef I8 (*T)(I4,I4,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444844_8")) {
				typedef I8 (*T)(I4,I4,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444848_8")) {
				typedef I8 (*T)(I4,I4,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"444884_8")) {
				typedef I8 (*T)(I4,I4,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"444888_8")) {
				typedef I8 (*T)(I4,I4,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448444_8")) {
				typedef I8 (*T)(I4,I4,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448448_8")) {
				typedef I8 (*T)(I4,I4,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448484_8")) {
				typedef I8 (*T)(I4,I4,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448488_8")) {
				typedef I8 (*T)(I4,I4,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448844_8")) {
				typedef I8 (*T)(I4,I4,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448848_8")) {
				typedef I8 (*T)(I4,I4,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"448884_8")) {
				typedef I8 (*T)(I4,I4,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"448888_8")) {
				typedef I8 (*T)(I4,I4,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484444_8")) {
				typedef I8 (*T)(I4,I8,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484448_8")) {
				typedef I8 (*T)(I4,I8,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484484_8")) {
				typedef I8 (*T)(I4,I8,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484488_8")) {
				typedef I8 (*T)(I4,I8,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484844_8")) {
				typedef I8 (*T)(I4,I8,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484848_8")) {
				typedef I8 (*T)(I4,I8,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"484884_8")) {
				typedef I8 (*T)(I4,I8,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"484888_8")) {
				typedef I8 (*T)(I4,I8,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488444_8")) {
				typedef I8 (*T)(I4,I8,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488448_8")) {
				typedef I8 (*T)(I4,I8,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488484_8")) {
				typedef I8 (*T)(I4,I8,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488488_8")) {
				typedef I8 (*T)(I4,I8,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488844_8")) {
				typedef I8 (*T)(I4,I8,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488848_8")) {
				typedef I8 (*T)(I4,I8,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"488884_8")) {
				typedef I8 (*T)(I4,I8,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"488888_8")) {
				typedef I8 (*T)(I4,I8,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.i,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844444_8")) {
				typedef I8 (*T)(I8,I4,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844448_8")) {
				typedef I8 (*T)(I8,I4,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844484_8")) {
				typedef I8 (*T)(I8,I4,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844488_8")) {
				typedef I8 (*T)(I8,I4,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844844_8")) {
				typedef I8 (*T)(I8,I4,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844848_8")) {
				typedef I8 (*T)(I8,I4,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"844884_8")) {
				typedef I8 (*T)(I8,I4,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"844888_8")) {
				typedef I8 (*T)(I8,I4,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848444_8")) {
				typedef I8 (*T)(I8,I4,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848448_8")) {
				typedef I8 (*T)(I8,I4,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848484_8")) {
				typedef I8 (*T)(I8,I4,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848488_8")) {
				typedef I8 (*T)(I8,I4,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848844_8")) {
				typedef I8 (*T)(I8,I4,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848848_8")) {
				typedef I8 (*T)(I8,I4,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"848884_8")) {
				typedef I8 (*T)(I8,I4,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"848888_8")) {
				typedef I8 (*T)(I8,I4,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.i,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884444_8")) {
				typedef I8 (*T)(I8,I8,I4,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884448_8")) {
				typedef I8 (*T)(I8,I8,I4,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884484_8")) {
				typedef I8 (*T)(I8,I8,I4,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884488_8")) {
				typedef I8 (*T)(I8,I8,I4,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884844_8")) {
				typedef I8 (*T)(I8,I8,I4,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884848_8")) {
				typedef I8 (*T)(I8,I8,I4,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"884884_8")) {
				typedef I8 (*T)(I8,I8,I4,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"884888_8")) {
				typedef I8 (*T)(I8,I8,I4,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.i,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888444_8")) {
				typedef I8 (*T)(I8,I8,I8,I4,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888448_8")) {
				typedef I8 (*T)(I8,I8,I8,I4,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888484_8")) {
				typedef I8 (*T)(I8,I8,I8,I4,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888488_8")) {
				typedef I8 (*T)(I8,I8,I8,I4,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.i,sp[4].data.p,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888844_8")) {
				typedef I8 (*T)(I8,I8,I8,I8,I4,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888848_8")) {
				typedef I8 (*T)(I8,I8,I8,I8,I4,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.i,sp[5].data.p);
			};
			if (!strcmp(sigTest,"888884_8")) {
				typedef I8 (*T)(I8,I8,I8,I8,I8,I4);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.i);
			};
			if (!strcmp(sigTest,"888888_8")) {
				typedef I8 (*T)(I8,I8,I8,I8,I8,I8);
				T func = (T)ptr;
				ret_sp->data.p = func(sp[0].data.p,sp[1].data.p,sp[2].data.p,sp[3].data.p,sp[4].data.p,sp[5].data.p);
			};
		break;
		}
		default:
		    g_assert_not_reached();
		}
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

