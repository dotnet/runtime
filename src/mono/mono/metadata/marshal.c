/*
 * marshal.c: Routines for marshaling complex types in P/Invoke methods.
 * 
 * Author:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#include "config.h"
#include "object.h"
#include "loader.h"
#include "metadata/marshal.h"
#include "metadata/tabledefs.h"
#include "metadata/exception.h"
#include "metadata/appdomain.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/threadpool.h"
#include <string.h>

//#define DEBUG_RUNTIME_CODE

#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

struct _MonoMethodBuilder {
	MonoMethod *method;
	GList *locals_list;
	int locals;
	guint32 code_size, pos;
	unsigned char *code;
};

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object);

#ifdef DEBUG_RUNTIME_CODE
static char*
indenter (MonoDisHelper *dh, MonoMethod *method, guint32 ip_offset)
{
	return g_strdup (" ");
}

static MonoDisHelper marshal_dh = {
	"\n",
	"IL_%04x: ",
	"IL_%04x",
	indenter, 
	NULL,
	NULL
};
#endif 

gpointer
mono_delegate_to_ftnptr (MonoDelegate *delegate)
{
	MonoMethod *method, *wrapper;
	MonoClass *klass;

	if (!delegate)
		return NULL;

	if (delegate->delegate_trampoline)
		return delegate->delegate_trampoline;

	klass = ((MonoObject *)delegate)->vtable->klass;
	g_assert (klass->delegate);
	
	method = delegate->method_info->method;
	wrapper = mono_marshal_get_managed_wrapper (method, delegate->target);

	delegate->delegate_trampoline =  mono_compile_method (wrapper);

	return delegate->delegate_trampoline;
}

gpointer
mono_array_to_savearray (MonoArray *array)
{
	if (!array)
		return NULL;

	g_assert_not_reached ();
	return NULL;
}

gpointer
mono_array_to_lparray (MonoArray *array)
{
	if (!array)
		return NULL;

	/* fixme: maybe we need to make a copy */
	return array->vector;
}

void
mono_string_utf8_to_builder (MonoStringBuilder *sb, char *text)
{
	GError *error = NULL;
	guint16 *ut;
	glong items_written;
	int l;

	if (!sb || !text)
		return;

	l = strlen (text);

	ut = g_utf8_to_utf16 (text, l, NULL, &items_written, &error);

	if (items_written > sb->capacity)
		items_written = sb->capacity;
	
	if (!error) {
		memcpy (sb->chars->vector, ut, items_written * 2);
		sb->length = items_written;
	} else 
		g_error_free (error);

	g_free (ut);
}

gpointer
mono_string_builder_to_utf8 (MonoStringBuilder *sb)
{
	char *res;

	if (!sb)
		return NULL;

	res = g_malloc (sb->capacity + 1);

	/* fixme: copy the content of the string builder? */
	res [0] = 0;

	return res;
}

gpointer
mono_string_to_ansibstr (MonoString *string_obj)
{
	g_error ("implement me");
	return NULL;
}

gpointer
mono_string_to_bstr (MonoString *string_obj)
{
	g_error ("implement me");
	return NULL;
}

void
mono_string_to_byvalstr (gpointer dst, MonoString *src, int size)
{
	char *s;
	int len;

	g_assert (dst != NULL);
	g_assert (size > 0);

	if (!src) {
		memset (dst, 0, size);
		return;
	}

	s = mono_string_to_utf8 (src);
	len = MIN (size, strlen (s));
	memcpy (dst, s, len);
	g_free (s);

	*((char *)dst + size - 1) = 0;
}

void
mono_string_to_byvalwstr (gpointer dst, MonoString *src, int size)
{
	int len;

	g_assert (dst != NULL);
	g_assert (size > 1);

	if (!src) {
		memset (dst, 0, size);
		return;
	}

	len = MIN (size, (mono_string_length (src) * 2));
	memcpy (dst, mono_string_chars (src), len);

	*((char *)dst + size - 1) = 0;
	*((char *)dst + size - 2) = 0;
}


static MonoMethod *
mono_find_method_by_name (MonoClass *klass, const char *name, int param_count)
{
	MonoMethod *res = NULL;
	int i;

	for (i = 0; i < klass->method.count; ++i) {
		if ((klass->methods [i]->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
		    klass->methods [i]->name[0] == name [0] && 
		    !strcmp (name, klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == param_count) {
			res = klass->methods [i];
			break;
		}
	}
	return res;
}

void
mono_mb_free (MonoMethodBuilder *mb)
{
	g_list_free (mb->locals_list);
	g_free (mb);
}

MonoMethodBuilder *
mono_mb_new (MonoClass *klass, const char *name)
{
	MonoMethodBuilder *mb;
	MonoMethod *m;

	g_assert (klass != NULL);
	g_assert (name != NULL);

	mb = g_new0 (MonoMethodBuilder, 1);

	mb->method = m = (MonoMethod *)g_new0 (MonoMethodWrapper, 1);

	m->klass = klass;
	m->name = g_strdup (name);
	m->inline_info = 1;
	m->inline_count = -1;
	m->wrapper_type = MONO_WRAPPER_UNKNOWN;

	mb->code_size = 256;
	mb->code = g_malloc (mb->code_size);
	
	return mb;
}

int
mono_mb_add_local (MonoMethodBuilder *mb, MonoType *type)
{
	int res = mb->locals;

	g_assert (mb != NULL);
	g_assert (type != NULL);

	mb->locals_list = g_list_append (mb->locals_list, type);
	mb->locals++;

	return res;
}

MonoMethod *
mono_mb_create_method (MonoMethodBuilder *mb, MonoMethodSignature *signature, int max_stack)
{
	MonoMethodHeader *header;
	GList *l;
	int i;

	g_assert (mb != NULL);

	((MonoMethodNormal *)mb->method)->header = header = (MonoMethodHeader *) 
		g_malloc0 (sizeof (MonoMethodHeader) + mb->locals * sizeof (MonoType *));

	if (max_stack < 8)
		max_stack = 8;

	header->max_stack = max_stack;

	for (i = 0, l = mb->locals_list; l; l = l->next, i++) {
		header->locals [i] = (MonoType *)l->data;
	}

	mb->method->signature = signature;
	header->code = mb->code;
	header->code_size = mb->pos;
	header->num_locals = mb->locals;

#ifdef DEBUG_RUNTIME_CODE
	printf ("RUNTIME CODE FOR %s\n", mono_method_full_name (mb->method, TRUE));
	printf ("%s\n", mono_disasm_code (&marshal_dh, mb->method, mb->code, mb->code + mb->pos));
#endif

	return mb->method;
}

guint32
mono_mb_add_data (MonoMethodBuilder *mb, gpointer data)
{
	MonoMethodWrapper *mw;

	g_assert (mb != NULL);

	mw = (MonoMethodWrapper *)mb->method;

	mw->data = g_list_append (mw->data, data);

	return g_list_length (mw->data);
}

void
mono_mb_patch_addr (MonoMethodBuilder *mb, int pos, int value)
{
	mb->code [pos] = value & 0xff;
	mb->code [pos + 1] = (value >> 8) & 0xff;
	mb->code [pos + 2] = (value >> 16) & 0xff;
	mb->code [pos + 3] = (value >> 24) & 0xff;
}

void
mono_mb_patch_addr_s (MonoMethodBuilder *mb, int pos, gint8 value)
{
	*((gint8 *)(&mb->code [pos])) = value;
}

void
mono_mb_emit_byte (MonoMethodBuilder *mb, guint8 op)
{
	if (mb->pos >= mb->code_size) {
		mb->code_size += 64;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos++] = op;
}

void
mono_mb_emit_ldflda (MonoMethodBuilder *mb, gint32 offset)
{
        mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
        mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
        mono_mb_emit_icon (mb, offset);
        mono_mb_emit_byte (mb, CEE_ADD);
}

void
mono_mb_emit_i4 (MonoMethodBuilder *mb, gint32 data)
{
	if ((mb->pos + 4) >= mb->code_size) {
		mb->code_size += 64;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mono_mb_patch_addr (mb, mb->pos, data);
	mb->pos += 4;
}

void
mono_mb_emit_i2 (MonoMethodBuilder *mb, gint16 data)
{
	if ((mb->pos + 2) >= mb->code_size) {
		mb->code_size += 64;
		mb->code = g_realloc (mb->code, mb->code_size);
	}

	mb->code [mb->pos] = data & 0xff;
	mb->code [mb->pos + 1] = (data >> 8) & 0xff;
	mb->pos += 2;
}

void
mono_mb_emit_ldarg (MonoMethodBuilder *mb, guint argnum)
{
	if (argnum < 4) {
 		mono_mb_emit_byte (mb, CEE_LDARG_0 + argnum);
	} else if (argnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDARG_S);
		mono_mb_emit_byte (mb, argnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDARG);
		mono_mb_emit_i2 (mb, argnum);
	}
}

void
mono_mb_emit_ldarg_addr (MonoMethodBuilder *mb, guint argnum)
{
	if (argnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDARGA_S);
		mono_mb_emit_byte (mb, argnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDARGA);
		mono_mb_emit_i2 (mb, argnum);
	}
}

void
mono_mb_emit_ldloc_addr (MonoMethodBuilder *mb, guint locnum)
{
	if (locnum < 256) {
		mono_mb_emit_byte (mb, CEE_LDLOCA_S);
		mono_mb_emit_byte (mb, locnum);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDLOCA);
		mono_mb_emit_i2 (mb, locnum);
	}
}

void
mono_mb_emit_ldloc (MonoMethodBuilder *mb, guint num)
{
	if (num < 4) {
 		mono_mb_emit_byte (mb, CEE_LDLOC_0 + num);
	} else if (num < 256) {
		mono_mb_emit_byte (mb, CEE_LDLOC_S);
		mono_mb_emit_byte (mb, num);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_LDLOC);
		mono_mb_emit_i2 (mb, num);
	}
}

void
mono_mb_emit_stloc (MonoMethodBuilder *mb, guint num)
{
	if (num < 4) {
 		mono_mb_emit_byte (mb, CEE_STLOC_0 + num);
	} else if (num < 256) {
		mono_mb_emit_byte (mb, CEE_STLOC_S);
		mono_mb_emit_byte (mb, num);
	} else {
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_STLOC);
		mono_mb_emit_i2 (mb, num);
	}
}

void
mono_mb_emit_icon (MonoMethodBuilder *mb, gint32 value)
{
	if (value >= -1 && value < 8) {
		mono_mb_emit_byte (mb, CEE_LDC_I4_0 + value);
	} else if (value >= -128 && value <= 127) {
		mono_mb_emit_byte (mb, CEE_LDC_I4_S);
		mono_mb_emit_byte (mb, value);
	} else {
		mono_mb_emit_byte (mb, CEE_LDC_I4);
		mono_mb_emit_i4 (mb, value);
	}
}

void
mono_mb_emit_managed_call (MonoMethodBuilder *mb, MonoMethod *method, MonoMethodSignature *opt_sig)
{
	if (!opt_sig)
		opt_sig = method->signature;
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LDFTN);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, opt_sig));
}

void
mono_mb_emit_native_call (MonoMethodBuilder *mb, MonoMethodSignature *sig, gpointer func)
{
	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, func));
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, sig));
}

void
mono_mb_emit_exception (MonoMethodBuilder *mb)
{
	/* fixme: we need a better way to throw exception,
	 * supporting several exception types and messages */
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_THROW);
	
}

void
mono_mb_emit_add_to_local (MonoMethodBuilder *mb, guint8 local, gint8 incr)
{
	mono_mb_emit_ldloc (mb, local); 
	mono_mb_emit_icon (mb, incr);
	mono_mb_emit_byte (mb, CEE_ADD);
	mono_mb_emit_stloc (mb, local); 
}

static void
emit_ptr_to_str_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, 
		      int usize, int msize)
{
	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 5);
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_1);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		mono_mb_emit_byte (mb, CEE_BR_S);
		mono_mb_emit_byte (mb, 3);
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDC_I4_0);
		mono_mb_emit_byte (mb, CEE_STIND_I1);
		break;
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		MonoClass *eclass;
		int esize;

		if (type->type == MONO_TYPE_ARRAY)
			eclass = mono_class_from_mono_type (type->data.array->type);
		else if (type->type == MONO_TYPE_SZARRAY) {
			eclass = mono_class_from_mono_type (type->data.type);
		} else {
			g_assert_not_reached ();
		}

	     	if (eclass->valuetype)
			esize = mono_class_instance_size (eclass) - sizeof (MonoObject);
		else
			esize = sizeof (gpointer);

		/* create a new array */
		/* fixme: this only works for SZARRAYS */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_icon (mb, msize / esize);
		mono_mb_emit_byte (mb, CEE_NEWARR);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, eclass));
		mono_mb_emit_byte (mb, CEE_STIND_I);

		/* copy the elements */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_icon (mb, usize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);			

		break;
	}
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
		mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_LPSTR_STR);
		mono_mb_emit_byte (mb, CEE_STIND_I);		
		break;
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
		mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_LPSTR_STR);
		mono_mb_emit_byte (mb, CEE_STIND_I);		
		break;
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		MonoClass *klass = mono_class_from_mono_type (type);
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		/* *dst = new object */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_NEWOBJ);	
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
		mono_mb_emit_byte (mb, CEE_STIND_I);
	
		/* save the old src pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_stloc (mb, dst_var);

		/* dst = pointer to newly created object data */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_1); 

		emit_struct_conv (mb, klass, TRUE);
		
		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_byte (mb, CEE_STLOC_0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_byte (mb, CEE_STLOC_1);
		break;
	}
	case MONO_MARSHAL_CONV_DEL_FTN: {
		// fixme: we never convert functions back to delegates, dont 
		// know if thats the correct behaviour
		break;
	}
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR:
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: 
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
	default:
		g_warning ("marshaling conversion %d not implemented", conv);
		g_assert_not_reached ();
	}
}

static void
emit_str_to_ptr_conv (MonoMethodBuilder *mb, MonoType *type, MonoMarshalConv conv, int usize, int msize)
{
	int pos;

	switch (conv) {
	case MONO_MARSHAL_CONV_BOOL_I4:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		mono_mb_emit_byte (mb, CEE_STIND_I4);
		break;
	case MONO_MARSHAL_CONV_STR_LPWSTR:
	case MONO_MARSHAL_CONV_STR_LPSTR:
	case MONO_MARSHAL_CONV_STR_LPTSTR:
	case MONO_MARSHAL_CONV_STR_BSTR:
	case MONO_MARSHAL_CONV_STR_ANSIBSTR:
	case MONO_MARSHAL_CONV_STR_TBSTR:
		/* free space if free == true */
		mono_mb_emit_byte (mb, CEE_LDLOC_2);
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		mono_mb_emit_byte (mb, 4);
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_FREE);

		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
		mono_mb_emit_byte (mb, conv);
		mono_mb_emit_byte (mb, CEE_STIND_I);	
		break;
	case MONO_MARSHAL_CONV_ARRAY_SAVEARRAY:
	case MONO_MARSHAL_CONV_ARRAY_LPARRAY:
	case MONO_MARSHAL_CONV_DEL_FTN:
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
		mono_mb_emit_byte (mb, conv);
		mono_mb_emit_byte (mb, CEE_STIND_I);	
		break;
	case MONO_MARSHAL_CONV_STR_BYVALSTR: 
	case MONO_MARSHAL_CONV_STR_BYVALWSTR: {
		if (!usize)
			break;

		mono_mb_emit_byte (mb, CEE_LDLOC_1); /* dst */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);	
		mono_mb_emit_byte (mb, CEE_LDIND_I); /* src String */
		mono_mb_emit_icon (mb, usize);
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_PROC3);
		mono_mb_emit_byte (mb, conv);
		break;
	}
	case MONO_MARSHAL_CONV_ARRAY_BYVALARRAY: {
		if (!usize) 
			break;

		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);

		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_byte (mb, CEE_LDLOC_0);	
		mono_mb_emit_byte (mb, CEE_LDIND_I);	
		mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
		mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
		mono_mb_emit_icon (mb, G_STRUCT_OFFSET (MonoArray, vector));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_icon (mb, usize);
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);			
		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	case MONO_MARSHAL_CONV_OBJECT_STRUCT: {
		int src_var, dst_var;

		src_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		dst_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_byte (mb, CEE_BRFALSE_S);
		pos = mb->pos;
		mono_mb_emit_byte (mb, 0);
		
		/* save the old src pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_stloc (mb, src_var);
		/* save the old dst pointer */
		mono_mb_emit_byte (mb, CEE_LDLOC_1);
		mono_mb_emit_stloc (mb, dst_var);

		/* src = pointer to object data */
		mono_mb_emit_byte (mb, CEE_LDLOC_0);
		mono_mb_emit_byte (mb, CEE_LDIND_I);		
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_0); 

		emit_struct_conv (mb, mono_class_from_mono_type (type), FALSE);
		
		/* restore the old src pointer */
		mono_mb_emit_ldloc (mb, src_var);
		mono_mb_emit_byte (mb, CEE_STLOC_0);
		/* restore the old dst pointer */
		mono_mb_emit_ldloc (mb, dst_var);
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		mono_mb_patch_addr_s (mb, pos, mb->pos - pos - 1);
		break;
	}
	case MONO_MARSHAL_CONV_BOOL_VARIANTBOOL:
	default:
		g_warning ("marshalling conversion %d not implemented", conv);
		g_assert_not_reached ();
	}
}

static void
emit_struct_conv (MonoMethodBuilder *mb, MonoClass *klass, gboolean to_object)
{
	MonoMarshalType *info;
	int i;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		int msize = 0;
		int usize = 0;
		gboolean last_field = i < (info->num_fields -1) ? 0 : 1;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, klass->unicode, &conv);
			
		if (last_field) {
			msize = klass->instance_size - info->fields [i].field->offset;
			usize = info->native_size - info->fields [i].offset;
		} else {
			msize = klass->fields [i + 1].offset - info->fields [i].field->offset;
			usize = info->fields [i + 1].offset - info->fields [i].offset;
		}
		g_assert (msize > 0 && usize > 0);

		switch (conv) {
		case MONO_MARSHAL_CONV_NONE: {
			int t;

			if (ftype->byref || ftype->type == MONO_TYPE_I ||
			    ftype->type == MONO_TYPE_U) {
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I);
				mono_mb_emit_byte (mb, CEE_STIND_I);
				break;
			}

			t = ftype->type;
		handle_enum:
			switch (t) {
			case MONO_TYPE_I4:
			case MONO_TYPE_U4:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I4);
				mono_mb_emit_byte (mb, CEE_STIND_I4);
				break;
			case MONO_TYPE_I1:
			case MONO_TYPE_U1:
			case MONO_TYPE_BOOLEAN:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I1);
				mono_mb_emit_byte (mb, CEE_STIND_I1);
				break;
			case MONO_TYPE_I2:
			case MONO_TYPE_U2:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I2);
				mono_mb_emit_byte (mb, CEE_STIND_I2);
				break;
			case MONO_TYPE_I8:
			case MONO_TYPE_U8:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_I8);
				mono_mb_emit_byte (mb, CEE_STIND_I8);
				break;
			case MONO_TYPE_R4:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_R4);
				mono_mb_emit_byte (mb, CEE_STIND_R4);
				break;
			case MONO_TYPE_R8:
				mono_mb_emit_byte (mb, CEE_LDLOC_1);
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_LDIND_R8);
				mono_mb_emit_byte (mb, CEE_STIND_R8);
				break;
			case MONO_TYPE_VALUETYPE:
				if (ftype->data.klass->enumtype) {
					t = ftype->data.klass->enum_basetype->type;
					goto handle_enum;
				}
				emit_struct_conv (mb, ftype->data.klass, to_object);
				continue;
			default:
				g_warning ("marshaling type %02x not implemented", ftype->type);
				g_assert_not_reached ();
			}
			break;
		}
		default:
			if (to_object) 
				emit_ptr_to_str_conv (mb, ftype, conv, usize, msize);
			else
				emit_str_to_ptr_conv (mb, ftype, conv, usize, msize);	
		}
		
		if (to_object) {
			mono_mb_emit_add_to_local (mb, 0, usize);
			mono_mb_emit_add_to_local (mb, 1, msize);
		} else {
			mono_mb_emit_add_to_local (mb, 0, msize);
			mono_mb_emit_add_to_local (mb, 1, usize);
		}				
	}
}

static MonoAsyncResult *
mono_delegate_begin_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoDelegate *async_callback;
	MonoObject *state;
	MonoMethod *im;
	MonoClass *klass;
	MonoMethod *method = NULL;
	int i;

	g_assert (delegate);

	klass = delegate->object.vtable->klass;

	method = mono_get_delegate_invoke (klass);
	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'B' && 
		    !strcmp ("BeginInvoke", klass->methods [i]->name)) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method != NULL);

	im = mono_get_delegate_invoke (method->klass);
	
	msg = mono_method_call_message_new (method, params, im, &async_callback, &state);

	return mono_thread_pool_add ((MonoObject *)delegate, msg, async_callback, state);
}

static int
mono_mb_emit_save_args (MonoMethodBuilder *mb, MonoMethodSignature *sig, gboolean save_this)
{
	int i, params_var, tmp_var;

	/* allocate local (pointer) *params[] */
	params_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local (pointer) tmp */
	tmp_var = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

	/* alloate space on stack to store an array of pointers to the arguments */
	mono_mb_emit_icon (mb, sizeof (gpointer) * (sig->param_count + 1));
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_LOCALLOC);
	mono_mb_emit_stloc (mb, params_var);

	/* tmp = params */
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_stloc (mb, tmp_var);

	if (save_this && sig->hasthis) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, 0);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (sig->param_count)
			mono_mb_emit_add_to_local (mb, tmp_var, sizeof (gpointer));

	}

	for (i = 0; i < sig->param_count; i++) {
		mono_mb_emit_ldloc (mb, tmp_var);
		mono_mb_emit_ldarg_addr (mb, i + sig->hasthis);
		mono_mb_emit_byte (mb, CEE_STIND_I);
		/* tmp = tmp + sizeof (gpointer) */
		if (i < (sig->param_count - 1))
			mono_mb_emit_add_to_local (mb, tmp_var, sizeof (gpointer));
	}

	return params_var;
}

static char*
mono_signature_to_name (MonoMethodSignature *sig, const char *prefix)
{
	int i;
	char *result;
	GString *res = g_string_new ("");

	if (prefix) {
		g_string_append (res, prefix);
		g_string_append_c (res, '_');
	}

	mono_type_get_desc (res, sig->ret, FALSE);

	for (i = 0; i < sig->param_count; ++i) {
		g_string_append_c (res, '_');
		mono_type_get_desc (res, sig->params [i], FALSE);
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

MonoMethod *
mono_marshal_get_delegate_begin_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	static MonoMethodSignature *csig = NULL;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "BeginInvoke"));

	sig = method->signature;

	cache = method->klass->image->delegate_begin_invoke_cache;
	if ((res = (MonoMethod *)g_hash_table_lookup (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	if (!csig) {
		int sigsize = sizeof (MonoMethodSignature) + 2 * sizeof (MonoType *);
		csig = g_malloc0 (sigsize);

		/* MonoAsyncResult * begin_invoke (MonoDelegate *delegate, gpointer params[]) */
		csig->param_count = 2;
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.object_class->byval_arg;
		csig->params [1] = &mono_defaults.int_class->byval_arg;
	}

	name = mono_signature_to_name (sig, "begin_invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name);
	g_free (name);

	mb->method->wrapper_type = MONO_WRAPPER_DELEGATE_BEGIN_INVOKE;
	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_native_call (mb, csig, mono_delegate_begin_invoke);
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	g_hash_table_insert (cache, sig, res);
	return res;
}

static MonoObject *
mono_delegate_end_invoke (MonoDelegate *delegate, gpointer *params)
{
	MonoDomain *domain = mono_domain_get ();
	MonoAsyncResult *ares;
	MonoMethod *method = NULL;
	MonoMethodSignature *sig;
	MonoMethodMessage *msg;
	MonoObject *res, *exc;
	MonoArray *out_args;
	MonoClass *klass;
	int i;

	g_assert (delegate);

	if (!delegate->method_info || !delegate->method_info->method)
		g_assert_not_reached ();

	klass = delegate->object.vtable->klass;

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == 'E' && 
		    !strcmp ("EndInvoke", klass->methods [i]->name)) {
			method = klass->methods [i];
			break;
		}
	}

	g_assert (method != NULL);

	sig = method->signature;

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	ares = mono_array_get (msg->args, gpointer, sig->param_count - 1);
	g_assert (ares);

	res = mono_thread_pool_finish (ares, &out_args, &exc);

	if (exc) {
		char *strace = mono_string_to_utf8 (((MonoException*)exc)->stack_trace);
		char  *tmp;
		tmp = g_strdup_printf ("%s\nException Rethrown at:\n", strace);
		g_free (strace);	
		((MonoException*)exc)->stack_trace = mono_string_new (domain, tmp);
		g_free (tmp);
		mono_raise_exception ((MonoException*)exc);
	}

	mono_method_return_message_restore (method, params, out_args);
	return res;
}

static void
mono_mb_emit_restore_result (MonoMethodBuilder *mb, MonoType *return_type)
{
	if (return_type->byref)
		return_type = &mono_defaults.int_class->byval_arg;
	else if (return_type->type == MONO_TYPE_VALUETYPE && return_type->data.klass->enumtype)
		return_type = return_type->data.klass->enum_basetype;

	switch (return_type->type) {
	case MONO_TYPE_VOID:
		g_assert_not_reached ();
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS: 
	case MONO_TYPE_OBJECT: 
	case MONO_TYPE_ARRAY: 
	case MONO_TYPE_SZARRAY: 
		/* nothing to do */
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_U1);
		break;
	case MONO_TYPE_I1:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I1);
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_U2);
		break;
	case MONO_TYPE_I2:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I2);
		break;
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I);
		break;
	case MONO_TYPE_I4:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I4);
		break;
	case MONO_TYPE_U4:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_U4);
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_I8);
		break;
	case MONO_TYPE_R4:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_R4);
		break;
	case MONO_TYPE_R8:
		mono_mb_emit_byte (mb, CEE_UNBOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (return_type)));
		mono_mb_emit_byte (mb, CEE_LDIND_R8);
		break;
	case MONO_TYPE_VALUETYPE: {
		int class;
		mono_mb_emit_byte (mb, CEE_UNBOX);
		class = mono_mb_add_data (mb, mono_class_from_mono_type (return_type));
		mono_mb_emit_i4 (mb, class);
		mono_mb_emit_byte (mb, CEE_LDOBJ);
		mono_mb_emit_i4 (mb, class);
		break;
	}
	default:
		g_warning ("type 0x%x not handled", return_type->type);
		g_assert_not_reached ();
	}

	mono_mb_emit_byte (mb, CEE_RET);
}

MonoMethod *
mono_marshal_get_delegate_end_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	static MonoMethodSignature *csig = NULL;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "EndInvoke"));

	sig = method->signature;

	cache = method->klass->image->delegate_end_invoke_cache;
	if ((res = (MonoMethod *)g_hash_table_lookup (cache, sig)))
		return res;

	g_assert (sig->hasthis);

	if (!csig) {
		int sigsize = sizeof (MonoMethodSignature) + 2 * sizeof (MonoType *);
		csig = g_malloc0 (sigsize);

		/* MonoObject *end_invoke (MonoDelegate *delegate, gpointer params[]) */
		csig->param_count = 2;
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.object_class->byval_arg;
		csig->params [1] = &mono_defaults.int_class->byval_arg;
	}

	name = mono_signature_to_name (sig, "end_invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name);
	g_free (name);

	mb->method->wrapper_type = MONO_WRAPPER_DELEGATE_END_INVOKE;
	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, FALSE);

	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_native_call (mb, csig, mono_delegate_end_invoke);

	if (sig->ret->type == MONO_TYPE_VOID) {
		mono_mb_emit_byte (mb, CEE_POP);
		mono_mb_emit_byte (mb, CEE_RET);
	} else
		mono_mb_emit_restore_result (mb, sig->ret);

	res = mono_mb_create_method (mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	g_hash_table_insert (cache, sig, res);

	return res;
}

static MonoObject *
mono_remoting_wrapper (MonoMethod *method, gpointer *params)
{
	MonoMethodMessage *msg;
	MonoTransparentProxy *this;
	MonoObject *res, *exc;
	MonoArray *out_args;

	this = *((MonoTransparentProxy **)params [0]);

	g_assert (this);
	g_assert (((MonoObject *)this)->vtable->klass == mono_defaults.transparent_proxy_class);
	
	/* skip the this pointer */
	params++;

	msg = mono_method_call_message_new (method, params, NULL, NULL, NULL);

	res = mono_remoting_invoke ((MonoObject *)this->rp, msg, &exc, &out_args);

	if (exc)
		mono_raise_exception ((MonoException *)exc);

	mono_method_return_message_restore (method, params, out_args);

	return res;
} 

MonoMethod *
mono_marshal_get_remoting_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig;
	static MonoMethodSignature *csig = NULL;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int params_var;

	g_assert (method);

	if (method->wrapper_type == MONO_WRAPPER_REMOTING_INVOKE)
		return method;

	sig = method->signature;

	/* we cant remote methods without this pointer */
	if (!sig->hasthis)
		return method;

	cache = method->klass->image->remoting_invoke_cache;
	if ((res = (MonoMethod *)g_hash_table_lookup (cache, method)))
		return res;

	if (!csig) {
		int sigsize = sizeof (MonoMethodSignature) + 2 * sizeof (MonoType *);
		csig = g_malloc0 (sigsize);

		/* MonoObject *remoting_wrapper (MonoMethod *method, gpointer params[]) */
		csig->param_count = 2;
		csig->ret = &mono_defaults.object_class->byval_arg;
		csig->params [0] = &mono_defaults.int_class->byval_arg;
		csig->params [1] = &mono_defaults.int_class->byval_arg;
	}

	mb = mono_mb_new (method->klass, method->name);
	mb->method->wrapper_type = MONO_WRAPPER_REMOTING_INVOKE;
	mb->method->save_lmf = 1;

	params_var = mono_mb_emit_save_args (mb, sig, TRUE);

	mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
	mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method));
	mono_mb_emit_ldloc (mb, params_var);
	mono_mb_emit_native_call (mb, csig, mono_remoting_wrapper);

	if (sig->ret->type == MONO_TYPE_VOID)
		mono_mb_emit_byte (mb, CEE_POP);
	else
		mono_mb_emit_restore_result (mb, sig->ret);

	res = mono_mb_create_method (mb, sig, sig->param_count + 16);
	mono_mb_free (mb);
	g_hash_table_insert (cache, method, res);
	return res;
}

/*
 * the returned method invokes all methods in a multicast delegate 
 */
MonoMethod *
mono_marshal_get_delegate_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig, *static_sig;
	int i, sigsize;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	int pos0, pos1;
	char *name;

	g_assert (method && method->klass->parent == mono_defaults.multicastdelegate_class &&
		  !strcmp (method->name, "Invoke"));
		
	sig = method->signature;

	cache = method->klass->image->delegate_invoke_cache;
	if ((res = (MonoMethod *)g_hash_table_lookup (cache, sig)))
		return res;

	sigsize = sizeof (MonoMethodSignature) + sig->param_count * sizeof (MonoType *);
	static_sig = g_memdup (sig, sigsize);
	static_sig->hasthis = 0;

	name = mono_signature_to_name (sig, "invoke");
	mb = mono_mb_new (mono_defaults.multicastdelegate_class, name);
	g_free (name);

	mb->method->wrapper_type = MONO_WRAPPER_DELEGATE_INVOKE;

	/* allocate local 0 (object) */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	g_assert (sig->hasthis);
	
	/*
	 * if (prev != null)
         *	prev.Invoke( args .. );
	 * return this.<target>( args .. );
         */

	/* get this->prev */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoMulticastDelegate, prev));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_stloc (mb, 0);

	/* if prev != null */
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_BRFALSE);

	pos0 = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* then recurse */
	mono_mb_emit_ldloc (mb, 0);
	for (i = 0; i < sig->param_count; i++)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_managed_call (mb, method, method->signature);
	if (sig->ret->type != MONO_TYPE_VOID)
		mono_mb_emit_byte (mb, CEE_POP);

	/* continued or prev == null */
	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	/* get this->target */
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, target));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_stloc (mb, 0);

	/* if target != null */
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_BRFALSE);
	pos0 = mb->pos;
	mono_mb_emit_i4 (mb, 0);
	
	/* then call this->method_ptr nonstatic */
	mono_mb_emit_ldloc (mb, 0); 
	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, sig));

	mono_mb_emit_byte (mb, CEE_BR);
	pos1 = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	/* else [target == null] call this->method_ptr static */
	mono_mb_patch_addr (mb, pos0, mb->pos - (pos0 + 4));

	for (i = 0; i < sig->param_count; ++i)
		mono_mb_emit_ldarg (mb, i + 1);
	mono_mb_emit_ldarg (mb, 0);
	mono_mb_emit_ldflda (mb, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
	mono_mb_emit_byte (mb, CEE_LDIND_I );
	mono_mb_emit_byte (mb, CEE_CALLI);
	mono_mb_emit_i4 (mb, mono_mb_add_data (mb, static_sig));

	/* return */
	mono_mb_patch_addr (mb, pos1, mb->pos - (pos1 + 4));
	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	g_hash_table_insert (cache, sig, res);

	return res;	
}

/*
 * generates IL code for the runtime invoke function 
 * MonoObject *runtime_invoke (MonoObject *this, void **params, MonoObject **exc)
 *
 * we also catch exceptions if exc != null
 */
MonoMethod *
mono_marshal_get_runtime_invoke (MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoExceptionClause *clause;
	MonoMethodHeader *header;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	static MonoString *string_dummy = NULL;
	int i, pos, sigsize;

	g_assert (method);

	cache = method->klass->image->runtime_invoke_cache;
	if ((res = (MonoMethod *)g_hash_table_lookup (cache, method)))
		return res;
	
	/* to make it work with our special string constructors */
	if (!string_dummy)
		string_dummy = mono_string_new_wrapper ("dummy");

	sig = method->signature;

	sigsize = sizeof (MonoMethodSignature) + 3 * sizeof (MonoType *);
	csig = g_malloc0 (sigsize);

	csig->param_count = 3;
	csig->ret = &mono_defaults.object_class->byval_arg;
	csig->params [0] = &mono_defaults.object_class->byval_arg;
	csig->params [1] = &mono_defaults.int_class->byval_arg;
	csig->params [2] = &mono_defaults.int_class->byval_arg;

	mb = mono_mb_new (method->klass, method->name);
	mb->method->wrapper_type = MONO_WRAPPER_RUNTIME_INVOKE;

	/* allocate local 0 (object) tmp */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
	/* allocate local 1 (object) exc */
	mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);

	/* cond set *exc to null */
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_BRFALSE_S);
	mono_mb_emit_byte (mb, 3);	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	if (sig->hasthis) {
		if (method->string_ctor) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, string_dummy));
		} else {
			mono_mb_emit_ldarg (mb, 0);
			if (method->klass->valuetype) {
				mono_mb_emit_byte (mb, CEE_UNBOX);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, method->klass));
			} 
		}
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		int type;

		mono_mb_emit_ldarg (mb, 1);
		if (i) {
			mono_mb_emit_icon (mb, sizeof (gpointer) * i);
			mono_mb_emit_byte (mb, CEE_ADD);
		}
		mono_mb_emit_byte (mb, CEE_LDIND_I);

		if (t->byref)
			continue;

		type = sig->params [i]->type;
handle_enum:
		switch (type) {
		case MONO_TYPE_I1:
			mono_mb_emit_byte (mb, CEE_LDIND_I1);
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
			mono_mb_emit_byte (mb, CEE_LDIND_U1);
			break;
		case MONO_TYPE_I2:
			mono_mb_emit_byte (mb, CEE_LDIND_I2);
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			mono_mb_emit_byte (mb, CEE_LDIND_U2);
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			mono_mb_emit_byte (mb, CEE_LDIND_I);
			break;
		case MONO_TYPE_I4:
			mono_mb_emit_byte (mb, CEE_LDIND_I4);
			break;
		case MONO_TYPE_U4:
			mono_mb_emit_byte (mb, CEE_LDIND_U4);
			break;
		case MONO_TYPE_R4:
			mono_mb_emit_byte (mb, CEE_LDIND_R4);
			break;
		case MONO_TYPE_R8:
			mono_mb_emit_byte (mb, CEE_LDIND_R8);
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_byte (mb, CEE_LDIND_I8);
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_PTR:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			/* do nothing */
			break;
		case MONO_TYPE_VALUETYPE:
			if (t->data.klass->enumtype) {
				type = t->data.klass->enum_basetype->type;
				goto handle_enum;
			}
			mono_mb_emit_byte (mb, CEE_LDOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, t->data.klass));
			break;
		default:
			g_assert_not_reached ();
		}		
	}

	if (method->string_ctor) {
		MonoMethodSignature *strsig;

		sigsize = sizeof (MonoMethodSignature) + sig->param_count * sizeof (MonoType *);
		strsig = g_memdup (sig, sigsize);
		strsig->ret = &mono_defaults.string_class->byval_arg;

		mono_mb_emit_managed_call (mb, method, strsig);		
	} else 
		mono_mb_emit_managed_call (mb, method, NULL);

	if (sig->ret->byref) {
		/* fixme: */
		g_assert_not_reached ();
	}


	switch (sig->ret->type) {
	case MONO_TYPE_VOID:
		if (!method->string_ctor)
			mono_mb_emit_byte (mb, CEE_LDNULL);
		break;
	case MONO_TYPE_BOOLEAN:
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
	case MONO_TYPE_VALUETYPE:
		/* box value types */
		mono_mb_emit_byte (mb, CEE_BOX);
		mono_mb_emit_i4 (mb, mono_mb_add_data (mb, mono_class_from_mono_type (sig->ret)));
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
		/* nothing to do */
		break;
	case MONO_TYPE_PTR:
	default:
		g_assert_not_reached ();
	}

	mono_mb_emit_stloc (mb, 0);
       		
	mono_mb_emit_byte (mb, CEE_LEAVE);
	pos = mb->pos;
	mono_mb_emit_i4 (mb, 0);

	clause = g_new0 (MonoExceptionClause, 1);
	clause->flags = MONO_EXCEPTION_CLAUSE_FILTER;
	clause->try_len = mb->pos;

	/* filter code */
	clause->token_or_filter = mb->pos;
	
	mono_mb_emit_byte (mb, CEE_POP);
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_byte (mb, CEE_LDC_I4_0);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_CGT_UN);
	mono_mb_emit_byte (mb, CEE_PREFIX1);
	mono_mb_emit_byte (mb, CEE_ENDFILTER);

	clause->handler_offset = mb->pos;

	/* handler code */
	/* store exception */
	mono_mb_emit_stloc (mb, 1);
	
	mono_mb_emit_byte (mb, CEE_LDARG_2);
	mono_mb_emit_ldloc (mb, 1);
	mono_mb_emit_byte (mb, CEE_STIND_I);

	mono_mb_emit_byte (mb, CEE_LEAVE);
	mono_mb_emit_i4 (mb, 0);

	clause->handler_len = mb->pos - clause->handler_offset;

	/* return result */
	mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
	mono_mb_emit_ldloc (mb, 0);
	mono_mb_emit_byte (mb, CEE_RET);
	
	res = mono_mb_create_method (mb, csig, sig->param_count + 16);
	mono_mb_free (mb);

	header = ((MonoMethodNormal *)res)->header;
	header->num_clauses = 1;
	header->clauses = clause;

	g_hash_table_insert (cache, method, res);

	return res;	
}

/*
 * generates IL code to call managed methods from unmanaged code 
 */
MonoMethod *
mono_marshal_get_managed_wrapper (MonoMethod *method, MonoObject *this)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoClass *klass;
	MonoMethod *res;
	GHashTable *cache;
	int i, pos, sigsize, *tmp_locals;

	g_assert (method != NULL);

	cache = method->klass->image->managed_wrapper_cache;
	if (!this && (res = (MonoMethod *)g_hash_table_lookup (cache, method)))
		return res;

	if (this) {
		/* fime: howto free that memory ? */
	}
	
	sig = method->signature;

	mb = mono_mb_new (method->klass, method->name);
	mb->method->wrapper_type = MONO_WRAPPER_NATIVE_TO_MANAGED;

	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	mono_mb_emit_byte (mb, CEE_LDNULL);
	mono_mb_emit_byte (mb, CEE_STLOC_2);

	/* we copy the signature, so that we can modify it */
	sigsize = sizeof (MonoMethodSignature) + sig->param_count * sizeof (MonoType *);
	csig = g_memdup (sig, sigsize);
	csig->hasthis = 0;
	csig->pinvoke = 1;

	/* fixme: howto handle this ? */
	if (sig->hasthis) {

		if (this) {
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_LDPTR);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, this));


		} else {
			/* fixme: */
			g_assert_not_reached ();
		}
	} 


	/* we first do all conversions */
	tmp_locals = alloca (sizeof (int) * sig->param_count);
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];

		tmp_locals [i] = 0;
		
		switch (t->type) {
		case MONO_TYPE_VALUETYPE:
			
			klass = sig->params [i]->data.klass;
			if (klass->blittable || klass->enumtype)
				break;

			tmp_locals [i] = mono_mb_add_local (mb, &klass->byval_arg);

			if (t->byref) 
				mono_mb_emit_ldarg (mb, i);
			else
				mono_mb_emit_ldarg_addr (mb, i);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			if (t->byref) {
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_BRFALSE);
				pos = mb->pos;
				mono_mb_emit_i4 (mb, 0);
			}			

			mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* emit valuetype convnversion code code */
			emit_struct_conv (mb, klass, TRUE);

			if (t->byref)
				mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
			break;
		case MONO_TYPE_STRING:
			if (t->byref)
				continue;

			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			csig->params [i] = &mono_defaults.int_class->byval_arg;

			mono_mb_emit_ldarg (mb, i);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
			mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_LPSTR_STR);
			mono_mb_emit_stloc (mb, tmp_locals [i]);
			break;	
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			if (t->byref)
				continue;

			klass = mono_class_from_mono_type (t);

			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.object_class->byval_arg);
			csig->params [i] = &mono_defaults.int_class->byval_arg;

			g_warning ("array marshaling not implemented");
			g_assert_not_reached ();
			break;
		}
	}

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];

		switch (t->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_ldarg (mb, i);
			break;
		case MONO_TYPE_STRING:
			if (t->byref) {
				mono_mb_emit_ldarg (mb, i);
			} else {
				g_assert (tmp_locals [i]);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			}
			break;	
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
			/* fixme: conversions ? */
			mono_mb_emit_ldarg (mb, i);
			break;
		case MONO_TYPE_VALUETYPE:
			klass = sig->params [i]->data.klass;
			if (klass->blittable || klass->enumtype) {
				mono_mb_emit_ldarg (mb, i);
				break;
			}

			g_assert (tmp_locals [i]);
			if (t->byref)
				mono_mb_emit_ldloc_addr (mb, tmp_locals [i]);
			else
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			break;
		default:
			g_warning ("type 0x%02x unknown", t->type);	
			g_assert_not_reached ();
		}
	}

	mono_mb_emit_managed_call (mb, method, NULL);

	if (!sig->ret->byref) { 
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			/* do nothing */
			break;
		case MONO_TYPE_STRING:		
			csig->ret = &mono_defaults.int_class->byval_arg;

			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
			mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_STR_LPSTR);
			break;
		case MONO_TYPE_VALUETYPE: {
			int tmp;
			klass = sig->ret->data.klass;
			if (klass->blittable || klass->enumtype)
				break;
			
			/* load pointer to returned value type */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			
			/* store the address of the source into local variable 0 */
			mono_mb_emit_byte (mb, CEE_STLOC_0);
			/* allocate space for the native struct and
			 * store the address into dst_ptr */
			tmp = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			g_assert (tmp);
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_byte (mb, CEE_STLOC_1);
			mono_mb_emit_byte (mb, CEE_LDLOC_1);
			mono_mb_emit_stloc (mb, tmp);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
			mono_mb_emit_ldloc (mb, tmp);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_RETOBJ);
			mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
			break;
		}
		default:
			g_warning ("return type 0x%02x unknown", sig->ret->type);	
			g_assert_not_reached ();
		}
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, csig, sig->param_count + 16);
	mono_mb_free (mb);

	if (!this)
		g_hash_table_insert (cache, method, res);

	return res;
}

/*
 * generates IL code for the pinvoke wrapper (the generated method
 * calls the unamnage code in method->addr)
 */
MonoMethod *
mono_marshal_get_native_wrapper (MonoMethod *method)
{
	MonoMethodSignature *sig, *csig;
	MonoMethodBuilder *mb;
	MonoMethod *res;
	GHashTable *cache;
	MonoClass *klass;
	gboolean pinvoke = FALSE;
	int i, pos, argnum, *tmp_locals;
	int type, sigsize;

	g_assert (method != NULL);

	cache = method->klass->image->native_wrapper_cache;
	if ((res = (MonoMethod *)g_hash_table_lookup (cache, method)))
		return res;

	sig = method->signature;

	if (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) &&
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL))
		pinvoke = TRUE;

	mb = mono_mb_new (method->klass, method->name);
	mb->method->wrapper_type = MONO_WRAPPER_MANAGED_TO_NATIVE;

	mb->method->save_lmf = 1;

	if (pinvoke && !method->addr)
		mono_lookup_pinvoke_call (method);

	if (!method->addr) {
		mono_mb_emit_exception (mb);
		res = mono_mb_create_method (mb, sig, sig->param_count + 16);
		mono_mb_free (mb);
		g_hash_table_insert (cache, method, res);
		return res;
	}

	/* we copy the signature, so that we can modify it */
	sigsize = sizeof (MonoMethodSignature) + sig->param_count * sizeof (MonoType *);

	/* internal calls: we simply push all arguments and call the method (no conversions) */
	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME)) {

		MonoMethodSignature *call_sig;

		/* hack - string constructors returns a value */
		if (method->string_ctor) {
			csig = g_memdup (sig, sigsize);
			csig->ret = &mono_defaults.string_class->byval_arg;
		} else
			csig = sig;

		if (sig->hasthis)
			mono_mb_emit_byte (mb, CEE_LDARG_0);

		for (i = 0; i < sig->param_count; i++)
			mono_mb_emit_ldarg (mb, i + sig->hasthis);

		call_sig = g_memdup (csig, sigsize);
		call_sig->pinvoke = 1;

		g_assert (method->addr);
		mono_mb_emit_native_call (mb, call_sig, method->addr);

		mono_mb_emit_byte (mb, CEE_RET);

		res = mono_mb_create_method (mb, csig, sig->param_count + 16);
		mono_mb_free (mb);
		g_hash_table_insert (cache, method, res);
		return res;
	}

	g_assert (pinvoke);

	/* pinvoke: we need to convert the arguments if necessary */

	csig = g_memdup (sig, sigsize);
	csig->pinvoke = 1;

	/* we allocate local for use with emit_struct_conv() */
	/* allocate local 0 (pointer) src_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 1 (pointer) dst_ptr */
	mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
	/* allocate local 2 (boolean) delete_old */
	mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);

	/* delete_old = FALSE */
	mono_mb_emit_icon (mb, 0);
	mono_mb_emit_byte (mb, CEE_STLOC_2);

	if (sig->ret->type != MONO_TYPE_VOID) {
		/* allocate local 3 to store the return value */
		mono_mb_add_local (mb, sig->ret);
	}

	/* we first do all conversions */
	tmp_locals = alloca (sizeof (int) * sig->param_count);
	for (i = 0; i < sig->param_count; i ++) {
		MonoType *t = sig->params [i];

		argnum = i + sig->hasthis;
		tmp_locals [i] = 0;

		switch (t->type) {
		case MONO_TYPE_VALUETYPE:			
			klass = t->data.klass;
			if (klass->blittable || klass->enumtype)
				break;

			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
			
			/* store the address of the source into local variable 0 */
			if (t->byref)
				mono_mb_emit_ldarg (mb, argnum);
			else
				mono_mb_emit_ldarg_addr (mb, argnum);

			mono_mb_emit_byte (mb, CEE_STLOC_0);
			
			if (t->byref) {
				mono_mb_emit_byte (mb, CEE_LDLOC_0);
				mono_mb_emit_byte (mb, CEE_BRFALSE);
				pos = mb->pos;
				mono_mb_emit_i4 (mb, 0);
			}

			/* allocate space for the native struct and
			 * store the address into local variable 1 (dest) */
			mono_mb_emit_icon (mb, mono_class_native_size (klass, NULL));
			mono_mb_emit_byte (mb, CEE_PREFIX1);
			mono_mb_emit_byte (mb, CEE_LOCALLOC);
			mono_mb_emit_stloc (mb, tmp_locals [i]);
			/* set dst_ptr */
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, FALSE);
			
			if (t->byref)
				mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
			break;
		case MONO_TYPE_STRING:
			if (t->byref)
				continue;

			csig->params [argnum] = &mono_defaults.int_class->byval_arg;
			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
			mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_STR_LPSTR);
			mono_mb_emit_stloc (mb, tmp_locals [i]);
			break;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			if (t->byref)
				continue;

			csig->params [argnum] = &mono_defaults.int_class->byval_arg;
			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			if (t->data.klass->delegate) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
				mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_DEL_FTN);
				mono_mb_emit_stloc (mb, tmp_locals [i]);
			} else if (t->data.klass == mono_defaults.stringbuilder_class) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
				mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_SB_LPSTR);
				mono_mb_emit_stloc (mb, tmp_locals [i]);
			} else {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_OBJADDR);
				/* fixme: convert to what ? */
				mono_mb_emit_stloc (mb, tmp_locals [i]);
			}

			break;
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			if (t->byref)
				continue;

			klass = mono_class_from_mono_type (t);

			csig->params [argnum] = &mono_defaults.int_class->byval_arg;
			tmp_locals [i] = mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);

			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
			if (klass->element_class == mono_defaults.string_class) 
				mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_STRARRAY_STRLPARRAY);
			else
				mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_ARRAY_LPARRAY);
			mono_mb_emit_stloc (mb, tmp_locals [i]);
			break;
		}
	}

	/* push all arguments */

	if (sig->hasthis)
		mono_mb_emit_byte (mb, CEE_LDARG_0);

	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		
		argnum = i + sig->hasthis;

		switch (t->type) {
		case MONO_TYPE_BOOLEAN:
			if (t->byref)
				g_warning ("byref boolean marshalling not inplemented");
			mono_mb_emit_ldarg (mb, argnum);
			break;
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			mono_mb_emit_ldarg (mb, argnum);
			break;
		case MONO_TYPE_VALUETYPE:
			klass = sig->params [i]->data.klass;
			if (klass->blittable || klass->enumtype) {
				mono_mb_emit_ldarg (mb, argnum);
				break;
			}			
			g_assert (tmp_locals [i]);
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			if (!t->byref) {
				mono_mb_emit_byte (mb, CEE_LDOBJ);
				mono_mb_emit_i4 (mb, mono_mb_add_data (mb, klass));
			}
			break;
		case MONO_TYPE_STRING:
			if (t->byref) {
				mono_mb_emit_ldarg (mb, argnum);
			} else {
				g_assert (tmp_locals [i]);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			}
			break;
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			if (t->byref) {
				mono_mb_emit_ldarg (mb, argnum);
			} else {
				g_assert (tmp_locals [i]);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			}
			break;
		case MONO_TYPE_CHAR:
			/* fixme: dont know how to marshal that. We cant simply
			 * convert it to a one byte UTF8 character, because an
			 * unicode character may need more that one byte in UTF8 */
			mono_mb_emit_ldarg (mb, argnum);
			break;
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			if (t->byref) {
				mono_mb_emit_ldarg (mb, argnum);
			} else {
				g_assert (tmp_locals [i]);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
			}
			break;
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_FNPTR:
		default:
			g_warning ("type 0x%02x unknown", t->type);	
			g_assert_not_reached ();
		}
	}			

	/* call the native method */
	mono_mb_emit_native_call (mb, csig, method->addr);

	/* return the result */

	/* we need to convert byref arguments back and free string arrays */
	for (i = 0; i < sig->param_count; i++) {
		MonoType *t = sig->params [i];
		
		argnum = i + sig->hasthis;

		switch (t->type) {
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:			
			if (t->byref)
				continue;
     
			if (t->data.klass == mono_defaults.stringbuilder_class) {
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_PROC2);
				mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_LPSTR_SB);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_FREE);
			}
			break;
		case MONO_TYPE_VALUETYPE:
			if (!t->byref)
				continue;
	
			klass = t->data.klass;
			if (klass->blittable || klass->enumtype)
				break;

			/* dst = argument */
			mono_mb_emit_ldarg (mb, argnum);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			mono_mb_emit_byte (mb, CEE_LDLOC_1);
			mono_mb_emit_byte (mb, CEE_BRFALSE);
			pos = mb->pos;
			mono_mb_emit_i4 (mb, 0);

			/* src = tmp_locals [i] */
			mono_mb_emit_ldloc (mb, tmp_locals [i]);
			mono_mb_emit_byte (mb, CEE_STLOC_0);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, klass, TRUE);
			
			mono_mb_patch_addr (mb, pos, mb->pos - (pos + 4));
			break;
		case MONO_TYPE_SZARRAY:
			if (t->byref)
				continue;
 
			klass = mono_class_from_mono_type (t);
			
			if (klass->element_class == mono_defaults.string_class) {
				g_assert (tmp_locals [i]);
				mono_mb_emit_ldloc (mb, tmp_locals [i]);
				mono_mb_emit_ldarg (mb, argnum);
				mono_mb_emit_byte (mb, CEE_LDLEN);				
				mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
				mono_mb_emit_byte (mb, CEE_MONO_PROC2);
				mono_mb_emit_byte (mb, MONO_MARSHAL_FREE_ARRAY);
			}

			break;
		}
	}

	if (!sig->ret->byref) {
		type = sig->ret->type;
	handle_enum:
		switch (type) {
		case MONO_TYPE_VOID:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
		/* no conversions necessary */
			break;
		case MONO_TYPE_BOOLEAN:
			/* maybe we need to make sure that it fits within 8 bits */
			break;
		case MONO_TYPE_VALUETYPE: {
			int tmp;

			klass = sig->ret->data.klass;
			if (klass->enumtype) {
				type = sig->ret->data.klass->enum_basetype->type;
				goto handle_enum;
			}

			if (klass->blittable)
				break;

			tmp = mono_mb_add_local (mb, sig->ret);
			g_assert (tmp);
			/* load pointer to returned value type */
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_VTADDR);
			/* store the address of the source into local variable 0 */
			mono_mb_emit_byte (mb, CEE_STLOC_0);
			/* set dst_ptr */
			mono_mb_emit_ldloc_addr (mb, tmp);
			mono_mb_emit_byte (mb, CEE_STLOC_1);

			/* emit valuetype conversion code */
			emit_struct_conv (mb, sig->ret->data.klass, TRUE);

			mono_mb_emit_ldloc (mb, tmp);
			break;
		}
		case MONO_TYPE_STRING:
			mono_mb_emit_byte (mb, MONO_CUSTOM_PREFIX);
			mono_mb_emit_byte (mb, CEE_MONO_FUNC1);
			mono_mb_emit_byte (mb, MONO_MARSHAL_CONV_LPSTR_STR);
			break;
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
			/* fixme: we need conversions here */
			break;
		case MONO_TYPE_CHAR:
			/* fixme: we need conversions here */
			break;
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_FNPTR:
		default:
			g_warning ("return type 0x%02x unknown", sig->ret->type);	
			g_assert_not_reached ();
		}
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, sig, sig->param_count + 16);
	mono_mb_free (mb);

	g_hash_table_insert (cache, method, res);

	return res;
}

/*
 * generates IL code for StructureToPtr (object structure, IntPtr ptr, bool fDeleteOld)
 */
MonoMethod *
mono_marshal_get_struct_to_ptr (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethod *stoptr = NULL;
	MonoMethod *res;

	g_assert (klass != NULL);

	if (klass->str_to_ptr)
		return klass->str_to_ptr;

	if (!stoptr) 
		stoptr = mono_find_method_by_name (mono_defaults.marshal_class, "StructureToPtr", 3);
	g_assert (stoptr);

	mb = mono_mb_new (klass, stoptr->name);

	if (klass->blittable) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 2 (boolean) delete_old */
		mono_mb_add_local (mb, &mono_defaults.boolean_class->byval_arg);
		mono_mb_emit_byte (mb, CEE_LDARG_2);
		mono_mb_emit_byte (mb, CEE_STLOC_2);

		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		emit_struct_conv (mb, klass, FALSE);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, stoptr->signature, 0);
	mono_mb_free (mb);

	klass->str_to_ptr = res;
	return res;
}

/*
 * generates IL code for PtrToStructure (IntPtr src, object structure)
 */
MonoMethod *
mono_marshal_get_ptr_to_struct (MonoClass *klass)
{
	MonoMethodBuilder *mb;
	static MonoMethod *ptostr = NULL;
	MonoMethod *res;

	g_assert (klass != NULL);

	if (klass->ptr_to_str)
		return klass->ptr_to_str;

	if (!ptostr) 
		ptostr = mono_find_method_by_name (mono_defaults.marshal_class, "PtrToStructure", 2);
	g_assert (ptostr);

	mb = mono_mb_new (klass, ptostr->name);

	if (klass->blittable) {
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_icon (mb, mono_class_value_size (klass, NULL));
		mono_mb_emit_byte (mb, CEE_PREFIX1);
		mono_mb_emit_byte (mb, CEE_CPBLK);
	} else {

		/* allocate local 0 (pointer) src_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		/* allocate local 1 (pointer) dst_ptr */
		mono_mb_add_local (mb, &mono_defaults.int_class->byval_arg);
		
		/* initialize src_ptr to point to the start of object data */
		mono_mb_emit_byte (mb, CEE_LDARG_0);
		mono_mb_emit_byte (mb, CEE_STLOC_0);

		/* initialize dst_ptr */
		mono_mb_emit_byte (mb, CEE_LDARG_1);
		mono_mb_emit_icon (mb, sizeof (MonoObject));
		mono_mb_emit_byte (mb, CEE_ADD);
		mono_mb_emit_byte (mb, CEE_STLOC_1);

		emit_struct_conv (mb, klass, TRUE);
	}

	mono_mb_emit_byte (mb, CEE_RET);

	res = mono_mb_create_method (mb, ptostr->signature, 0);
	mono_mb_free (mb);

	klass->ptr_to_str = res;
	return res;
}

/* FIXME: on win32 we should probably use GlobalAlloc(). */
void*
mono_marshal_alloc (gpointer size) 
{
	MONO_ARCH_SAVE_REGS;

	return g_try_malloc ((gulong)size);
}

void
mono_marshal_free (gpointer ptr) 
{
	MONO_ARCH_SAVE_REGS;

	g_free (ptr);
}

void
mono_marshal_free_array (gpointer *ptr, int size) {
	int i;

	for (i = 0; i < size; i++)
		if (ptr [i])
			g_free (ptr [i]);
}

void *
mono_marshal_realloc (gpointer ptr, gpointer size) 
{
	MONO_ARCH_SAVE_REGS;

	return g_try_realloc (ptr, (gulong)size);
}

void *
mono_marshal_string_array (MonoArray *array)
{
	char **result;
	int i, len;

	if (!array)
		return NULL;

	len = mono_array_length (array);

	result = g_malloc (sizeof (char *) * (len + 1));
	for (i = 0; i < len; ++i) {
		MonoString *s = (MonoString *)mono_array_get (array, gpointer, i);
		result [i] = s ? mono_string_to_utf8 (s): NULL;
	}
	/* null terminate the array */
	result [i] = NULL;

	return result;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_to_unmanaged (MonoArray *src, gint32 start_index,
								    gpointer dest, gint32 length)
{
	int element_size;
	void *source_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	g_assert (src->obj.vtable->klass->rank == 1);
	g_assert (start_index >= 0 && start_index < mono_array_length (src));
	g_assert (start_index + length <= mono_array_length (src));

	element_size = mono_array_element_size (src->obj.vtable->klass);
	  
	source_addr = mono_array_addr_with_size (src, element_size, start_index);

	memcpy (dest, source_addr, length * element_size);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_copy_from_unmanaged (gpointer src, gint32 start_index,
								      MonoArray *dest, gint32 length)
{
	int element_size;
	void *dest_addr;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dest);

	g_assert (dest->obj.vtable->klass->rank == 1);
	g_assert (start_index >= 0 && start_index < mono_array_length (dest));
	g_assert (start_index + length <= mono_array_length (dest));

	element_size = mono_array_element_size (dest->obj.vtable->klass);
	  
	dest_addr = mono_array_addr_with_size (dest, element_size, start_index);

	memcpy (dest_addr, src, length * element_size);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_ReadIntPtr (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(gpointer*)(p + offset);
}

unsigned char
ves_icall_System_Runtime_InteropServices_Marshal_ReadByte (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(unsigned char*)(p + offset);
}

gint16
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt16 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(gint16*)(p + offset);
}

gint32
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt32 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(gint32*)(p + offset);
}

gint64
ves_icall_System_Runtime_InteropServices_Marshal_ReadInt64 (gpointer ptr, gint32 offset)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	return *(gint64*)(p + offset);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteByte (gpointer ptr, gint32 offset, unsigned char val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(unsigned char*)(p + offset) = val;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteIntPtr (gpointer ptr, gint32 offset, gpointer val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(gpointer*)(p + offset) = val;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt16 (gpointer ptr, gint32 offset, gint16 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(gint16*)(p + offset) = val;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt32 (gpointer ptr, gint32 offset, gint32 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(gint32*)(p + offset) = val;
}

void
ves_icall_System_Runtime_InteropServices_Marshal_WriteInt64 (gpointer ptr, gint32 offset, gint64 val)
{
	char *p = ptr;

	MONO_ARCH_SAVE_REGS;

	*(gint64*)(p + offset) = val;
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi (char *ptr)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_new (mono_domain_get (), ptr);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringAnsi_len (char *ptr, gint32 len)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_new_len (mono_domain_get (), ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni (guint16 *ptr)
{
	MonoDomain *domain = mono_domain_get (); 
	int len = 0;
	guint16 *t = ptr;

	MONO_ARCH_SAVE_REGS;

	while (*t++)
		len++;

	return mono_string_new_utf16 (domain, ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringUni_len (guint16 *ptr, gint32 len)
{
	MonoDomain *domain = mono_domain_get (); 

	MONO_ARCH_SAVE_REGS;

	return mono_string_new_utf16 (domain, ptr, len);
}

MonoString *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStringBSTR (gpointer ptr)
{
	MONO_ARCH_SAVE_REGS;

	g_warning ("PtrToStringBSTR not implemented");
	g_assert_not_reached ();

	return NULL;
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_GetLastWin32Error (void)
{
	MONO_ARCH_SAVE_REGS;

	return (GetLastError ());
}

guint32 
ves_icall_System_Runtime_InteropServices_Marshal_SizeOf (MonoReflectionType *rtype)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (rtype);

	klass = mono_class_from_mono_type (rtype->type);

	return mono_class_native_size (klass, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_StructureToPtr (MonoObject *obj, gpointer dst, MonoBoolean delete_old)
{
	MonoMethod *method;
	gpointer pa [3];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (obj);
	MONO_CHECK_ARG_NULL (dst);

	method = mono_marshal_get_struct_to_ptr (obj->vtable->klass);

	pa [0] = obj;
	pa [1] = &dst;
	pa [2] = &delete_old;

	mono_runtime_invoke (method, NULL, pa, NULL);
}

void
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (gpointer src, MonoObject *dst)
{
	MonoMethod *method;
	gpointer pa [2];

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (dst);

	method = mono_marshal_get_ptr_to_struct (dst->vtable->klass);

	pa [0] = &src;
	pa [1] = dst;

	mono_runtime_invoke (method, NULL, pa, NULL);
}

MonoObject *
ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure_type (gpointer src, MonoReflectionType *type)
{
	MonoDomain *domain = mono_domain_get (); 
	MonoObject *res;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	res = mono_object_new (domain, mono_class_from_mono_type (type->type));

	ves_icall_System_Runtime_InteropServices_Marshal_PtrToStructure (src, res);

	return res;
}

int
ves_icall_System_Runtime_InteropServices_Marshal_OffsetOf (MonoReflectionType *type, MonoString *field_name)
{
	MonoMarshalType *info;
	MonoClass *klass;
	char *fname;
	int i;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (type);
	MONO_CHECK_ARG_NULL (field_name);

	fname = mono_string_to_utf8 (field_name);
	klass = mono_class_from_mono_type (type->type);

	info = mono_marshal_load_type_info (klass);	
	
	for (i = 0; i < klass->field.count; ++i) {
		if (*fname == *klass->fields [i].name && 
		    strcmp (fname, klass->fields [i].name) == 0)
			break;
	}
	g_free (fname);

	mono_assert (i < klass->field.count);

	return info->fields [i].offset;
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalAnsi (MonoString *string)
{
	MONO_ARCH_SAVE_REGS;

	return mono_string_to_utf8 (string);
}

gpointer
ves_icall_System_Runtime_InteropServices_Marshal_StringToHGlobalUni (MonoString *string)
{
	MONO_ARCH_SAVE_REGS;

	return g_memdup (mono_string_chars (string), mono_string_length (string)*2);
}

static void
mono_struct_delete_old (MonoClass *klass, char *ptr)
{
	MonoMarshalType *info;
	int i;

	info = mono_marshal_load_type_info (klass);

	for (i = 0; i < info->num_fields; i++) {
		MonoMarshalNative ntype;
		MonoMarshalConv conv;
		MonoType *ftype = info->fields [i].field->type;
		char *cpos;

		if (ftype->attrs & FIELD_ATTRIBUTE_STATIC)
			continue;

		ntype = mono_type_to_unmanaged (ftype, info->fields [i].mspec, TRUE, 
						klass->unicode, &conv);
			
		cpos = ptr + info->fields [i].offset;

		switch (conv) {
		case MONO_MARSHAL_CONV_NONE:
			if (MONO_TYPE_ISSTRUCT (ftype)) {
				mono_struct_delete_old (ftype->data.klass, cpos);
				continue;
			}
			break;
		case MONO_MARSHAL_CONV_STR_LPWSTR:
		case MONO_MARSHAL_CONV_STR_LPSTR:
		case MONO_MARSHAL_CONV_STR_LPTSTR:
		case MONO_MARSHAL_CONV_STR_BSTR:
		case MONO_MARSHAL_CONV_STR_ANSIBSTR:
		case MONO_MARSHAL_CONV_STR_TBSTR:
			g_free (*(gpointer *)cpos);
			break;
		default:
			continue;
		}
	}
}

void
ves_icall_System_Runtime_InteropServices_Marshal_DestroyStructure (gpointer src, MonoReflectionType *type)
{
	MonoClass *klass;

	MONO_ARCH_SAVE_REGS;

	MONO_CHECK_ARG_NULL (src);
	MONO_CHECK_ARG_NULL (type);

	klass = mono_class_from_mono_type (type->type);

	mono_struct_delete_old (klass, (char *)src);
}

