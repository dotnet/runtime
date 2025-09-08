#include "interp-icalls.h"
#include <mono/metadata/mh_log.h>
#include <string.h>

typedef gint32 I4;
typedef gpointer I8;

gboolean is_scalar_vtype(MonoType* tp);

gboolean
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

gboolean
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
	MH_LOGV(MH_LVL_TRACE, "MintIcallSig logging not implemented\n");	
}
void
do_icall (MonoMethodSignature *sig, MintICallSig op, stackval *ret_sp, stackval *sp, gpointer ptr, gboolean save_last_error)
{
	MH_LOG_INDENT();
	MH_LOG("Sig: %p op: %d ret_sp: %p sp: %p ptr: %p, save_last: %s", (void*)sig, op, (void*)ret_sp, (void*)sp, (void*)ptr, save_last_error ? "T" : "F");
	
	if (save_last_error)
		mono_marshal_clear_last_error();
	MH_LOG("About to execute function, sig enum value is : %d", op);
	switch (op) {
	case MINT_ICALLSIG_V_V: {
		typedef void (*T)(void);
		T func = (T)ptr;
		func();
		break;
	}
	case MINT_ICALLSIG_V_4: {
		typedef I4(*T)(void);
		T func = (T)ptr;
		ret_sp->data.i = func();
		break;
	}
	case MINT_ICALLSIG_V_8: {
		typedef I8(*T)(void);
		T func = (T)ptr;
		ret_sp->data.p = func();
		break;
	}
	case MINT_ICALLSIG_4_V: {
		typedef void (*T)(I4);
		T func = (T)ptr;
		func(sp[0].data.i);
		break;
	}
	case MINT_ICALLSIG_8_V: {
		typedef void (*T)(I8);
		T func = (T)ptr;
		func(sp[0].data.p);
		break;
	}
	case MINT_ICALLSIG_4_4: {
		typedef I4(*T)(I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i);
		break;
	}
	case MINT_ICALLSIG_8_4: {
		typedef I4(*T)(I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p);
		break;
	}
	case MINT_ICALLSIG_4_8: {
		typedef I8(*T)(I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i);
		break;
	}
	case MINT_ICALLSIG_8_8: {
		typedef I8(*T)(I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p);
		break;
	}
	case MINT_ICALLSIG_44_V: {
		typedef void (*T)(I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i);
		break;
	}
	case MINT_ICALLSIG_48_V: {
		typedef void (*T)(I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p);
		break;
	}
	case MINT_ICALLSIG_84_V: {
		typedef void (*T)(I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i);
		break;
	}
	case MINT_ICALLSIG_88_V: {
		typedef void (*T)(I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p);
		break;
	}
	case MINT_ICALLSIG_44_4: {
		typedef I4(*T)(I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i);
		break;
	}
	case MINT_ICALLSIG_48_4: {
		typedef I4(*T)(I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p);
		break;
	}
	case MINT_ICALLSIG_84_4: {
		typedef I4(*T)(I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i);
		break;
	}
	case MINT_ICALLSIG_88_4: {
		typedef I4(*T)(I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p);
		break;
	}
	case MINT_ICALLSIG_44_8: {
		typedef I8(*T)(I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i);
		break;
	}
	case MINT_ICALLSIG_48_8: {
		typedef I8(*T)(I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p);
		break;
	}
	case MINT_ICALLSIG_84_8: {
		typedef I8(*T)(I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i);
		break;
	}
	case MINT_ICALLSIG_88_8: {
		typedef I8(*T)(I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p);
		break;
	}
	case MINT_ICALLSIG_444_V: {
		typedef void (*T)(I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_448_V: {
		typedef void (*T)(I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_484_V: {
		typedef void (*T)(I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_488_V: {
		typedef void (*T)(I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_844_V: {
		typedef void (*T)(I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_848_V: {
		typedef void (*T)(I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_884_V: {
		typedef void (*T)(I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_888_V: {
		typedef void (*T)(I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_444_4: {
		typedef I4(*T)(I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_448_4: {
		typedef I4(*T)(I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_484_4: {
		typedef I4(*T)(I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_488_4: {
		typedef I4(*T)(I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_844_4: {
		typedef I4(*T)(I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_848_4: {
		typedef I4(*T)(I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_884_4: {
		typedef I4(*T)(I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_888_4: {
		typedef I4(*T)(I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_444_8: {
		typedef I8(*T)(I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_448_8: {
		typedef I8(*T)(I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_484_8: {
		typedef I8(*T)(I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_488_8: {
		typedef I8(*T)(I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_844_8: {
		typedef I8(*T)(I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_848_8: {
		typedef I8(*T)(I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_884_8: {
		typedef I8(*T)(I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i);
		break;
	}
	case MINT_ICALLSIG_888_8: {
		typedef I8(*T)(I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p);
		break;
	}
	case MINT_ICALLSIG_4444_V: {
		typedef void (*T)(I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4448_V: {
		typedef void (*T)(I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4484_V: {
		typedef void (*T)(I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4488_V: {
		typedef void (*T)(I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4844_V: {
		typedef void (*T)(I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4848_V: {
		typedef void (*T)(I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4884_V: {
		typedef void (*T)(I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4888_V: {
		typedef void (*T)(I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8444_V: {
		typedef void (*T)(I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8448_V: {
		typedef void (*T)(I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8484_V: {
		typedef void (*T)(I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8488_V: {
		typedef void (*T)(I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8844_V: {
		typedef void (*T)(I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8848_V: {
		typedef void (*T)(I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8884_V: {
		typedef void (*T)(I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8888_V: {
		typedef void (*T)(I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4444_4: {
		typedef I4(*T)(I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4448_4: {
		typedef I4(*T)(I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4484_4: {
		typedef I4(*T)(I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4488_4: {
		typedef I4(*T)(I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4844_4: {
		typedef I4(*T)(I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4848_4: {
		typedef I4(*T)(I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4884_4: {
		typedef I4(*T)(I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4888_4: {
		typedef I4(*T)(I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8444_4: {
		typedef I4(*T)(I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8448_4: {
		typedef I4(*T)(I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8484_4: {
		typedef I4(*T)(I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8488_4: {
		typedef I4(*T)(I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8844_4: {
		typedef I4(*T)(I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8848_4: {
		typedef I4(*T)(I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8884_4: {
		typedef I4(*T)(I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8888_4: {
		typedef I4(*T)(I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4444_8: {
		typedef I8(*T)(I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4448_8: {
		typedef I8(*T)(I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4484_8: {
		typedef I8(*T)(I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4488_8: {
		typedef I8(*T)(I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4844_8: {
		typedef I8(*T)(I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4848_8: {
		typedef I8(*T)(I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_4884_8: {
		typedef I8(*T)(I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_4888_8: {
		typedef I8(*T)(I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8444_8: {
		typedef I8(*T)(I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8448_8: {
		typedef I8(*T)(I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8484_8: {
		typedef I8(*T)(I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8488_8: {
		typedef I8(*T)(I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8844_8: {
		typedef I8(*T)(I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8848_8: {
		typedef I8(*T)(I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_8884_8: {
		typedef I8(*T)(I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i);
		break;
	}
	case MINT_ICALLSIG_8888_8: {
		typedef I8(*T)(I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p);
		break;
	}
	case MINT_ICALLSIG_44444_V: {
		typedef void (*T)(I4, I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44448_V: {
		typedef void (*T)(I4, I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44484_V: {
		typedef void (*T)(I4, I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44488_V: {
		typedef void (*T)(I4, I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44844_V: {
		typedef void (*T)(I4, I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44848_V: {
		typedef void (*T)(I4, I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44884_V: {
		typedef void (*T)(I4, I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44888_V: {
		typedef void (*T)(I4, I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48444_V: {
		typedef void (*T)(I4, I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48448_V: {
		typedef void (*T)(I4, I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48484_V: {
		typedef void (*T)(I4, I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48488_V: {
		typedef void (*T)(I4, I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48844_V: {
		typedef void (*T)(I4, I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48848_V: {
		typedef void (*T)(I4, I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48884_V: {
		typedef void (*T)(I4, I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48888_V: {
		typedef void (*T)(I4, I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84444_V: {
		typedef void (*T)(I8, I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84448_V: {
		typedef void (*T)(I8, I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84484_V: {
		typedef void (*T)(I8, I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84488_V: {
		typedef void (*T)(I8, I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84844_V: {
		typedef void (*T)(I8, I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84848_V: {
		typedef void (*T)(I8, I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84884_V: {
		typedef void (*T)(I8, I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84888_V: {
		typedef void (*T)(I8, I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88444_V: {
		typedef void (*T)(I8, I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88448_V: {
		typedef void (*T)(I8, I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88484_V: {
		typedef void (*T)(I8, I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88488_V: {
		typedef void (*T)(I8, I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88844_V: {
		typedef void (*T)(I8, I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88848_V: {
		typedef void (*T)(I8, I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88884_V: {
		typedef void (*T)(I8, I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88888_V: {
		typedef void (*T)(I8, I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44444_4: {
		typedef I4(*T)(I4, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44448_4: {
		typedef I4(*T)(I4, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44484_4: {
		typedef I4(*T)(I4, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44488_4: {
		typedef I4(*T)(I4, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44844_4: {
		typedef I4(*T)(I4, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44848_4: {
		typedef I4(*T)(I4, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44884_4: {
		typedef I4(*T)(I4, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44888_4: {
		typedef I4(*T)(I4, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48444_4: {
		typedef I4(*T)(I4, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48448_4: {
		typedef I4(*T)(I4, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48484_4: {
		typedef I4(*T)(I4, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48488_4: {
		typedef I4(*T)(I4, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48844_4: {
		typedef I4(*T)(I4, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48848_4: {
		typedef I4(*T)(I4, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48884_4: {
		typedef I4(*T)(I4, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48888_4: {
		typedef I4(*T)(I4, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84444_4: {
		typedef I4(*T)(I8, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84448_4: {
		typedef I4(*T)(I8, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84484_4: {
		typedef I4(*T)(I8, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84488_4: {
		typedef I4(*T)(I8, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84844_4: {
		typedef I4(*T)(I8, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84848_4: {
		typedef I4(*T)(I8, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84884_4: {
		typedef I4(*T)(I8, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84888_4: {
		typedef I4(*T)(I8, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88444_4: {
		typedef I4(*T)(I8, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88448_4: {
		typedef I4(*T)(I8, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88484_4: {
		typedef I4(*T)(I8, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88488_4: {
		typedef I4(*T)(I8, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88844_4: {
		typedef I4(*T)(I8, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88848_4: {
		typedef I4(*T)(I8, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88884_4: {
		typedef I4(*T)(I8, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88888_4: {
		typedef I4(*T)(I8, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44444_8: {
		typedef I8(*T)(I4, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44448_8: {
		typedef I8(*T)(I4, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44484_8: {
		typedef I8(*T)(I4, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44488_8: {
		typedef I8(*T)(I4, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44844_8: {
		typedef I8(*T)(I4, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44848_8: {
		typedef I8(*T)(I4, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_44884_8: {
		typedef I8(*T)(I4, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_44888_8: {
		typedef I8(*T)(I4, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48444_8: {
		typedef I8(*T)(I4, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48448_8: {
		typedef I8(*T)(I4, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48484_8: {
		typedef I8(*T)(I4, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48488_8: {
		typedef I8(*T)(I4, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48844_8: {
		typedef I8(*T)(I4, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48848_8: {
		typedef I8(*T)(I4, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_48884_8: {
		typedef I8(*T)(I4, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_48888_8: {
		typedef I8(*T)(I4, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84444_8: {
		typedef I8(*T)(I8, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84448_8: {
		typedef I8(*T)(I8, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84484_8: {
		typedef I8(*T)(I8, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84488_8: {
		typedef I8(*T)(I8, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84844_8: {
		typedef I8(*T)(I8, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84848_8: {
		typedef I8(*T)(I8, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_84884_8: {
		typedef I8(*T)(I8, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_84888_8: {
		typedef I8(*T)(I8, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88444_8: {
		typedef I8(*T)(I8, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88448_8: {
		typedef I8(*T)(I8, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88484_8: {
		typedef I8(*T)(I8, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88488_8: {
		typedef I8(*T)(I8, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88844_8: {
		typedef I8(*T)(I8, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88848_8: {
		typedef I8(*T)(I8, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_88884_8: {
		typedef I8(*T)(I8, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i);
		break;
	}
	case MINT_ICALLSIG_88888_8: {
		typedef I8(*T)(I8, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p);
		break;
	}
	case MINT_ICALLSIG_444444_V: {
		typedef void (*T)(I4, I4, I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444448_V: {
		typedef void (*T)(I4, I4, I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444484_V: {
		typedef void (*T)(I4, I4, I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444488_V: {
		typedef void (*T)(I4, I4, I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444844_V: {
		typedef void (*T)(I4, I4, I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444848_V: {
		typedef void (*T)(I4, I4, I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444884_V: {
		typedef void (*T)(I4, I4, I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444888_V: {
		typedef void (*T)(I4, I4, I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448444_V: {
		typedef void (*T)(I4, I4, I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448448_V: {
		typedef void (*T)(I4, I4, I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448484_V: {
		typedef void (*T)(I4, I4, I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448488_V: {
		typedef void (*T)(I4, I4, I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448844_V: {
		typedef void (*T)(I4, I4, I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448848_V: {
		typedef void (*T)(I4, I4, I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448884_V: {
		typedef void (*T)(I4, I4, I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448888_V: {
		typedef void (*T)(I4, I4, I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484444_V: {
		typedef void (*T)(I4, I8, I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484448_V: {
		typedef void (*T)(I4, I8, I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484484_V: {
		typedef void (*T)(I4, I8, I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484488_V: {
		typedef void (*T)(I4, I8, I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484844_V: {
		typedef void (*T)(I4, I8, I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484848_V: {
		typedef void (*T)(I4, I8, I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484884_V: {
		typedef void (*T)(I4, I8, I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484888_V: {
		typedef void (*T)(I4, I8, I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488444_V: {
		typedef void (*T)(I4, I8, I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488448_V: {
		typedef void (*T)(I4, I8, I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488484_V: {
		typedef void (*T)(I4, I8, I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488488_V: {
		typedef void (*T)(I4, I8, I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488844_V: {
		typedef void (*T)(I4, I8, I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488848_V: {
		typedef void (*T)(I4, I8, I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488884_V: {
		typedef void (*T)(I4, I8, I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488888_V: {
		typedef void (*T)(I4, I8, I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844444_V: {
		typedef void (*T)(I8, I4, I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844448_V: {
		typedef void (*T)(I8, I4, I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844484_V: {
		typedef void (*T)(I8, I4, I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844488_V: {
		typedef void (*T)(I8, I4, I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844844_V: {
		typedef void (*T)(I8, I4, I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844848_V: {
		typedef void (*T)(I8, I4, I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844884_V: {
		typedef void (*T)(I8, I4, I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844888_V: {
		typedef void (*T)(I8, I4, I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848444_V: {
		typedef void (*T)(I8, I4, I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848448_V: {
		typedef void (*T)(I8, I4, I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848484_V: {
		typedef void (*T)(I8, I4, I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848488_V: {
		typedef void (*T)(I8, I4, I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848844_V: {
		typedef void (*T)(I8, I4, I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848848_V: {
		typedef void (*T)(I8, I4, I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848884_V: {
		typedef void (*T)(I8, I4, I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848888_V: {
		typedef void (*T)(I8, I4, I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884444_V: {
		typedef void (*T)(I8, I8, I4, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884448_V: {
		typedef void (*T)(I8, I8, I4, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884484_V: {
		typedef void (*T)(I8, I8, I4, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884488_V: {
		typedef void (*T)(I8, I8, I4, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884844_V: {
		typedef void (*T)(I8, I8, I4, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884848_V: {
		typedef void (*T)(I8, I8, I4, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884884_V: {
		typedef void (*T)(I8, I8, I4, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884888_V: {
		typedef void (*T)(I8, I8, I4, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888444_V: {
		typedef void (*T)(I8, I8, I8, I4, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888448_V: {
		typedef void (*T)(I8, I8, I8, I4, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888484_V: {
		typedef void (*T)(I8, I8, I8, I4, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888488_V: {
		typedef void (*T)(I8, I8, I8, I4, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888844_V: {
		typedef void (*T)(I8, I8, I8, I8, I4, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888848_V: {
		typedef void (*T)(I8, I8, I8, I8, I4, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888884_V: {
		typedef void (*T)(I8, I8, I8, I8, I8, I4);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888888_V: {
		typedef void (*T)(I8, I8, I8, I8, I8, I8);
		T func = (T)ptr;
		func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444444_4: {
		typedef I4(*T)(I4, I4, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444448_4: {
		typedef I4(*T)(I4, I4, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444484_4: {
		typedef I4(*T)(I4, I4, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444488_4: {
		typedef I4(*T)(I4, I4, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444844_4: {
		typedef I4(*T)(I4, I4, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444848_4: {
		typedef I4(*T)(I4, I4, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444884_4: {
		typedef I4(*T)(I4, I4, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444888_4: {
		typedef I4(*T)(I4, I4, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448444_4: {
		typedef I4(*T)(I4, I4, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448448_4: {
		typedef I4(*T)(I4, I4, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448484_4: {
		typedef I4(*T)(I4, I4, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448488_4: {
		typedef I4(*T)(I4, I4, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448844_4: {
		typedef I4(*T)(I4, I4, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448848_4: {
		typedef I4(*T)(I4, I4, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448884_4: {
		typedef I4(*T)(I4, I4, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448888_4: {
		typedef I4(*T)(I4, I4, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484444_4: {
		typedef I4(*T)(I4, I8, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484448_4: {
		typedef I4(*T)(I4, I8, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484484_4: {
		typedef I4(*T)(I4, I8, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484488_4: {
		typedef I4(*T)(I4, I8, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484844_4: {
		typedef I4(*T)(I4, I8, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484848_4: {
		typedef I4(*T)(I4, I8, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484884_4: {
		typedef I4(*T)(I4, I8, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484888_4: {
		typedef I4(*T)(I4, I8, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488444_4: {
		typedef I4(*T)(I4, I8, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488448_4: {
		typedef I4(*T)(I4, I8, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488484_4: {
		typedef I4(*T)(I4, I8, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488488_4: {
		typedef I4(*T)(I4, I8, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488844_4: {
		typedef I4(*T)(I4, I8, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488848_4: {
		typedef I4(*T)(I4, I8, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488884_4: {
		typedef I4(*T)(I4, I8, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488888_4: {
		typedef I4(*T)(I4, I8, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844444_4: {
		typedef I4(*T)(I8, I4, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844448_4: {
		typedef I4(*T)(I8, I4, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844484_4: {
		typedef I4(*T)(I8, I4, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844488_4: {
		typedef I4(*T)(I8, I4, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844844_4: {
		typedef I4(*T)(I8, I4, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844848_4: {
		typedef I4(*T)(I8, I4, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844884_4: {
		typedef I4(*T)(I8, I4, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844888_4: {
		typedef I4(*T)(I8, I4, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848444_4: {
		typedef I4(*T)(I8, I4, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848448_4: {
		typedef I4(*T)(I8, I4, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848484_4: {
		typedef I4(*T)(I8, I4, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848488_4: {
		typedef I4(*T)(I8, I4, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848844_4: {
		typedef I4(*T)(I8, I4, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848848_4: {
		typedef I4(*T)(I8, I4, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848884_4: {
		typedef I4(*T)(I8, I4, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848888_4: {
		typedef I4(*T)(I8, I4, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884444_4: {
		typedef I4(*T)(I8, I8, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884448_4: {
		typedef I4(*T)(I8, I8, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884484_4: {
		typedef I4(*T)(I8, I8, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884488_4: {
		typedef I4(*T)(I8, I8, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884844_4: {
		typedef I4(*T)(I8, I8, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884848_4: {
		typedef I4(*T)(I8, I8, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884884_4: {
		typedef I4(*T)(I8, I8, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884888_4: {
		typedef I4(*T)(I8, I8, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888444_4: {
		typedef I4(*T)(I8, I8, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888448_4: {
		typedef I4(*T)(I8, I8, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888484_4: {
		typedef I4(*T)(I8, I8, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888488_4: {
		typedef I4(*T)(I8, I8, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888844_4: {
		typedef I4(*T)(I8, I8, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888848_4: {
		typedef I4(*T)(I8, I8, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888884_4: {
		typedef I4(*T)(I8, I8, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888888_4: {
		typedef I4(*T)(I8, I8, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.i = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444444_8: {
		typedef I8(*T)(I4, I4, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444448_8: {
		typedef I8(*T)(I4, I4, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444484_8: {
		typedef I8(*T)(I4, I4, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444488_8: {
		typedef I8(*T)(I4, I4, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444844_8: {
		typedef I8(*T)(I4, I4, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444848_8: {
		typedef I8(*T)(I4, I4, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_444884_8: {
		typedef I8(*T)(I4, I4, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_444888_8: {
		typedef I8(*T)(I4, I4, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448444_8: {
		typedef I8(*T)(I4, I4, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448448_8: {
		typedef I8(*T)(I4, I4, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448484_8: {
		typedef I8(*T)(I4, I4, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448488_8: {
		typedef I8(*T)(I4, I4, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448844_8: {
		typedef I8(*T)(I4, I4, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448848_8: {
		typedef I8(*T)(I4, I4, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_448884_8: {
		typedef I8(*T)(I4, I4, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_448888_8: {
		typedef I8(*T)(I4, I4, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484444_8: {
		typedef I8(*T)(I4, I8, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484448_8: {
		typedef I8(*T)(I4, I8, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484484_8: {
		typedef I8(*T)(I4, I8, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484488_8: {
		typedef I8(*T)(I4, I8, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484844_8: {
		typedef I8(*T)(I4, I8, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484848_8: {
		typedef I8(*T)(I4, I8, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_484884_8: {
		typedef I8(*T)(I4, I8, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_484888_8: {
		typedef I8(*T)(I4, I8, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488444_8: {
		typedef I8(*T)(I4, I8, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488448_8: {
		typedef I8(*T)(I4, I8, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488484_8: {
		typedef I8(*T)(I4, I8, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488488_8: {
		typedef I8(*T)(I4, I8, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488844_8: {
		typedef I8(*T)(I4, I8, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488848_8: {
		typedef I8(*T)(I4, I8, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_488884_8: {
		typedef I8(*T)(I4, I8, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_488888_8: {
		typedef I8(*T)(I4, I8, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.i, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844444_8: {
		typedef I8(*T)(I8, I4, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844448_8: {
		typedef I8(*T)(I8, I4, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844484_8: {
		typedef I8(*T)(I8, I4, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844488_8: {
		typedef I8(*T)(I8, I4, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844844_8: {
		typedef I8(*T)(I8, I4, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844848_8: {
		typedef I8(*T)(I8, I4, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_844884_8: {
		typedef I8(*T)(I8, I4, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_844888_8: {
		typedef I8(*T)(I8, I4, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848444_8: {
		typedef I8(*T)(I8, I4, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848448_8: {
		typedef I8(*T)(I8, I4, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848484_8: {
		typedef I8(*T)(I8, I4, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848488_8: {
		typedef I8(*T)(I8, I4, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848844_8: {
		typedef I8(*T)(I8, I4, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848848_8: {
		typedef I8(*T)(I8, I4, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_848884_8: {
		typedef I8(*T)(I8, I4, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_848888_8: {
		typedef I8(*T)(I8, I4, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.i, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884444_8: {
		typedef I8(*T)(I8, I8, I4, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884448_8: {
		typedef I8(*T)(I8, I8, I4, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884484_8: {
		typedef I8(*T)(I8, I8, I4, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884488_8: {
		typedef I8(*T)(I8, I8, I4, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884844_8: {
		typedef I8(*T)(I8, I8, I4, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884848_8: {
		typedef I8(*T)(I8, I8, I4, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_884884_8: {
		typedef I8(*T)(I8, I8, I4, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_884888_8: {
		typedef I8(*T)(I8, I8, I4, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.i, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888444_8: {
		typedef I8(*T)(I8, I8, I8, I4, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888448_8: {
		typedef I8(*T)(I8, I8, I8, I4, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888484_8: {
		typedef I8(*T)(I8, I8, I8, I4, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888488_8: {
		typedef I8(*T)(I8, I8, I8, I4, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.i, sp[4].data.p, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888844_8: {
		typedef I8(*T)(I8, I8, I8, I8, I4, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888848_8: {
		typedef I8(*T)(I8, I8, I8, I8, I4, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.i, sp[5].data.p);
		break;
	}
	case MINT_ICALLSIG_888884_8: {
		typedef I8(*T)(I8, I8, I8, I8, I8, I4);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.i);
		break;
	}
	case MINT_ICALLSIG_888888_8: {
		typedef I8(*T)(I8, I8, I8, I8, I8, I8);
		T func = (T)ptr;
		ret_sp->data.p = func(sp[0].data.p, sp[1].data.p, sp[2].data.p, sp[3].data.p, sp[4].data.p, sp[5].data.p);
		break;
	}
	default:
		g_assert_not_reached();
	}

	if (save_last_error)
	{
		MH_LOG("Setting last error");
		mono_marshal_set_last_error();
	}
	/* convert the native representation to the stackval representation */
	if (sig)
	{
		MH_LOG("Setting stackval from data");
		stackval_from_data(sig->ret, ret_sp, (char*)&ret_sp->data.p, sig->pinvoke && !sig->marshalling_disabled);
		MH_LOG("Set stackval from data");
	}
	else
		MH_LOG("Not trying to set stackval from data - no sig");

	MH_LOG("Returning from do_icall with ret_sp: %p", ret_sp->data.p);
}

