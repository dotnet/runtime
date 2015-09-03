/*
 * debug-helpers.c:
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 */

#include <string.h>
#include "mono/metadata/tokentype.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/appdomain.h"

struct MonoMethodDesc {
	char *name_space;
	char *klass;
	char *name;
	char *args;
	guint num_args;
	gboolean include_namespace, klass_glob, name_glob;
};

#ifdef HAVE_ARRAY_ELEM_INIT
#define MSGSTRFIELD(line) MSGSTRFIELD1(line)
#define MSGSTRFIELD1(line) str##line
static const struct msgstr_t {
#define WRAPPER(a,b) char MSGSTRFIELD(__LINE__) [sizeof (b)];
#include "wrapper-types.h"
#undef WRAPPER
} opstr = {
#define WRAPPER(a,b) b,
#include "wrapper-types.h"
#undef WRAPPER
};
static const gint16 opidx [] = {
#define WRAPPER(a,b) [MONO_WRAPPER_ ## a] = offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "wrapper-types.h"
#undef WRAPPER
};

static const char*
wrapper_type_to_str (guint32 wrapper_type)
{
	g_assert (wrapper_type < MONO_WRAPPER_NUM);

	return (const char*)&opstr + opidx [wrapper_type];
}

#else
#define WRAPPER(a,b) b,
static const char* const
wrapper_type_names [MONO_WRAPPER_NUM + 1] = {
#include "wrapper-types.h"
	NULL
};

static const char*
wrapper_type_to_str (guint32 wrapper_type)
{
	g_assert (wrapper_type < MONO_WRAPPER_NUM);

	return wrapper_type_names [wrapper_type];
}

#endif

static void
append_class_name (GString *res, MonoClass *class, gboolean include_namespace)
{
	if (!class) {
		g_string_append (res, "Unknown");
		return;
	}
	if (class->nested_in) {
		append_class_name (res, class->nested_in, include_namespace);
		g_string_append_c (res, '/');
	}
	if (include_namespace && *(class->name_space)) {
		g_string_append (res, class->name_space);
		g_string_append_c (res, '.');
	}
	g_string_append (res, class->name);
}

static MonoClass*
find_system_class (const char *name)
{
	if (!strcmp (name, "void")) 
		return mono_defaults.void_class;
	else if (!strcmp (name, "char")) return mono_defaults.char_class;
	else if (!strcmp (name, "bool")) return mono_defaults.boolean_class;
	else if (!strcmp (name, "byte")) return mono_defaults.byte_class;
	else if (!strcmp (name, "sbyte")) return mono_defaults.sbyte_class;
	else if (!strcmp (name, "uint16")) return mono_defaults.uint16_class;
	else if (!strcmp (name, "int16")) return mono_defaults.int16_class;
	else if (!strcmp (name, "uint")) return mono_defaults.uint32_class;
	else if (!strcmp (name, "int")) return mono_defaults.int32_class;
	else if (!strcmp (name, "ulong")) return mono_defaults.uint64_class;
	else if (!strcmp (name, "long")) return mono_defaults.int64_class;
	else if (!strcmp (name, "uintptr")) return mono_defaults.uint_class;
	else if (!strcmp (name, "intptr")) return mono_defaults.int_class;
	else if (!strcmp (name, "single")) return mono_defaults.single_class;
	else if (!strcmp (name, "double")) return mono_defaults.double_class;
	else if (!strcmp (name, "string")) return mono_defaults.string_class;
	else if (!strcmp (name, "object")) return mono_defaults.object_class;
	else
		return NULL;
}

void
mono_type_get_desc (GString *res, MonoType *type, gboolean include_namespace)
{
	int i;

	switch (type->type) {
	case MONO_TYPE_VOID:
		g_string_append (res, "void"); break;
	case MONO_TYPE_CHAR:
		g_string_append (res, "char"); break;
	case MONO_TYPE_BOOLEAN:
		g_string_append (res, "bool"); break;
	case MONO_TYPE_U1:
		g_string_append (res, "byte"); break;
	case MONO_TYPE_I1:
		g_string_append (res, "sbyte"); break;
	case MONO_TYPE_U2:
		g_string_append (res, "uint16"); break;
	case MONO_TYPE_I2:
		g_string_append (res, "int16"); break;
	case MONO_TYPE_U4:
		g_string_append (res, "uint"); break;
	case MONO_TYPE_I4:
		g_string_append (res, "int"); break;
	case MONO_TYPE_U8:
		g_string_append (res, "ulong"); break;
	case MONO_TYPE_I8:
		g_string_append (res, "long"); break;
	case MONO_TYPE_FNPTR: /* who cares for the exact signature? */
		g_string_append (res, "*()"); break;
	case MONO_TYPE_U:
		g_string_append (res, "uintptr"); break;
	case MONO_TYPE_I:
		g_string_append (res, "intptr"); break;
	case MONO_TYPE_R4:
		g_string_append (res, "single"); break;
	case MONO_TYPE_R8:
		g_string_append (res, "double"); break;
	case MONO_TYPE_STRING:
		g_string_append (res, "string"); break;
	case MONO_TYPE_OBJECT:
		g_string_append (res, "object"); break;
	case MONO_TYPE_PTR:
		mono_type_get_desc (res, type->data.type, include_namespace);
		g_string_append_c (res, '*');
		break;
	case MONO_TYPE_ARRAY:
		mono_type_get_desc (res, &type->data.array->eklass->byval_arg, include_namespace);
		g_string_append_printf (res, "[%d]", type->data.array->rank);
		break;
	case MONO_TYPE_SZARRAY:
		mono_type_get_desc (res, &type->data.klass->byval_arg, include_namespace);
		g_string_append (res, "[]");
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		append_class_name (res, type->data.klass, include_namespace);
		break;
	case MONO_TYPE_GENERICINST: {
		MonoGenericContext *context;

		mono_type_get_desc (res, &type->data.generic_class->container_class->byval_arg, include_namespace);
		g_string_append (res, "<");
		context = &type->data.generic_class->context;
		if (context->class_inst) {
			for (i = 0; i < context->class_inst->type_argc; ++i) {
				if (i > 0)
					g_string_append (res, ", ");
				mono_type_get_desc (res, context->class_inst->type_argv [i], include_namespace);
			}
		}
		if (context->method_inst) {
			if (context->class_inst)
					g_string_append (res, "; ");
			for (i = 0; i < context->method_inst->type_argc; ++i) {
				if (i > 0)
					g_string_append (res, ", ");
				mono_type_get_desc (res, context->method_inst->type_argv [i], include_namespace);
			}
		}
		g_string_append (res, ">");
		break;
	}
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		if (type->data.generic_param) {
			const char *name = mono_generic_param_name (type->data.generic_param);
			if (name)
				g_string_append (res, name);
			else
				g_string_append_printf (res, "%s%d", type->type == MONO_TYPE_VAR ? "!" : "!!", mono_generic_param_num (type->data.generic_param));
		} else {
			g_string_append (res, "<unknown>");
		}
		break;
	case MONO_TYPE_TYPEDBYREF:
		g_string_append (res, "typedbyref");
		break;
	default:
		break;
	}
	if (type->byref)
		g_string_append_c (res, '&');
}

char*
mono_type_full_name (MonoType *type)
{
	GString *str;

	str = g_string_new ("");
	mono_type_get_desc (str, type, TRUE);
	return g_string_free (str, FALSE);
}

char*
mono_signature_get_desc (MonoMethodSignature *sig, gboolean include_namespace)
{
	int i;
	char *result;
	GString *res;

	if (!sig)
		return g_strdup ("<invalid signature>");

	res = g_string_new ("");

	for (i = 0; i < sig->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		mono_type_get_desc (res, sig->params [i], include_namespace);
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

static void
ginst_get_desc (GString *str, MonoGenericInst *ginst)
{
	int i;

	for (i = 0; i < ginst->type_argc; ++i) {
		if (i > 0)
			g_string_append (str, ", ");
		mono_type_get_desc (str, ginst->type_argv [i], TRUE);
	}
}

char*
mono_context_get_desc (MonoGenericContext *context)
{
	GString *str;
	char *res;

	str = g_string_new ("");
	g_string_append (str, "<");

	if (context->class_inst)
		ginst_get_desc (str, context->class_inst);
	if (context->method_inst) {
		if (context->class_inst)
			g_string_append (str, "; ");
		ginst_get_desc (str, context->method_inst);
	}

	g_string_append (str, ">");
	res = g_strdup (str->str);
	g_string_free (str, TRUE);
	return res;
}	

/**
 * mono_method_desc_new:
 * @name: the method name.
 * @include_namespace: whether the name includes a namespace or not.
 *
 * Creates a method description for @name, which conforms to the following
 * specification:
 *
 * [namespace.]classname:methodname[(args...)]
 *
 * in all the loaded assemblies.
 *
 * Both classname and methodname can contain '*' which matches anything.
 *
 * Returns: a parsed representation of the method description.
 */
MonoMethodDesc*
mono_method_desc_new (const char *name, gboolean include_namespace)
{
	MonoMethodDesc *result;
	char *class_name, *class_nspace, *method_name, *use_args, *end;
	int use_namespace;
	
	class_nspace = g_strdup (name);
	use_args = strchr (class_nspace, '(');
	if (use_args) {
		/* Allow a ' ' between the method name and the signature */
		if (use_args > class_nspace && use_args [-1] == ' ')
			use_args [-1] = 0;
		*use_args++ = 0;
		end = strchr (use_args, ')');
		if (!end) {
			g_free (class_nspace);
			return NULL;
		}
		*end = 0;
	}
	method_name = strrchr (class_nspace, ':');
	if (!method_name) {
		g_free (class_nspace);
		return NULL;
	}
	/* allow two :: to separate the method name */
	if (method_name != class_nspace && method_name [-1] == ':')
		method_name [-1] = 0;
	*method_name++ = 0;
	class_name = strrchr (class_nspace, '.');
	if (class_name) {
		*class_name++ = 0;
		use_namespace = 1;
	} else {
		class_name = class_nspace;
		use_namespace = 0;
	}
	result = g_new0 (MonoMethodDesc, 1);
	result->include_namespace = include_namespace;
	result->name = method_name;
	result->klass = class_name;
	result->name_space = use_namespace? class_nspace: NULL;
	result->args = use_args? use_args: NULL;
	if (strstr (result->name, "*"))
		result->name_glob = TRUE;
	if (strstr (result->klass, "*"))
		result->klass_glob = TRUE;
	if (use_args) {
		end = use_args;
		if (*end)
			result->num_args = 1;
		while (*end) {
			if (*end == ',')
				result->num_args++;
			++end;
		}
	}

	return result;
}

MonoMethodDesc*
mono_method_desc_from_method (MonoMethod *method)
{
	MonoMethodDesc *result;
	
	result = g_new0 (MonoMethodDesc, 1);
	result->include_namespace = TRUE;
	result->name = g_strdup (method->name);
	result->klass = g_strdup (method->klass->name);
	result->name_space = g_strdup (method->klass->name_space);

	return result;
}

/**
 * mono_method_desc_free:
 * @desc: method description to be released
 *
 * Releases the MonoMethodDesc object @desc.
 */
void
mono_method_desc_free (MonoMethodDesc *desc)
{
	if (desc->name_space)
		g_free (desc->name_space);
	else if (desc->klass)
		g_free (desc->klass);
	g_free (desc);
}

/*
 * namespace and class are supposed to match already if this function is used.
 */
gboolean
mono_method_desc_match (MonoMethodDesc *desc, MonoMethod *method)
{
	char *sig;
	gboolean name_match;

	name_match = strcmp (desc->name, method->name) == 0;
#ifndef _EGLIB_MAJOR
	if (!name_match && desc->name_glob)
		name_match = g_pattern_match_simple (desc->name, method->name);
#endif
	if (!name_match)
		return FALSE;
	if (!desc->args)
		return TRUE;
	if (desc->num_args != mono_method_signature (method)->param_count)
		return FALSE;
	sig = mono_signature_get_desc (mono_method_signature (method), desc->include_namespace);
	if (strcmp (sig, desc->args)) {
		g_free (sig);
		return FALSE;
	}
	g_free (sig);
	return TRUE;
}

static const char *
my_strrchr (const char *str, char ch, int *len)
{
	int pos;

	for (pos = (*len)-1; pos >= 0; pos--) {
		if (str [pos] != ch)
			continue;

		*len = pos;
		return str + pos;
	}

	return NULL;
}

static gboolean
match_class (MonoMethodDesc *desc, int pos, MonoClass *klass)
{
	const char *p;

	if (desc->klass_glob && !strcmp (desc->klass, "*"))
		return TRUE;
#ifndef _EGLIB_MAJOR
	if (desc->klass_glob && g_pattern_match_simple (desc->klass, klass->name))
		return TRUE;
#endif
	p = my_strrchr (desc->klass, '/', &pos);
	if (!p) {
		if (strncmp (desc->klass, klass->name, pos))
			return FALSE;
		if (desc->name_space && strcmp (desc->name_space, klass->name_space))
			return FALSE;
		return TRUE;
	}

	if (strcmp (p+1, klass->name))
		return FALSE;
	if (!klass->nested_in)
		return FALSE;

	return match_class (desc, pos, klass->nested_in);
}

gboolean
mono_method_desc_full_match (MonoMethodDesc *desc, MonoMethod *method)
{
	if (!desc->klass)
		return FALSE;
	if (!match_class (desc, strlen (desc->klass), method->klass))
		return FALSE;

	return mono_method_desc_match (desc, method);
}

MonoMethod*
mono_method_desc_search_in_class (MonoMethodDesc *desc, MonoClass *klass)
{
	MonoMethod* m;
	gpointer iter = NULL;
	
	while ((m = mono_class_get_methods (klass, &iter)))
		if (mono_method_desc_match (desc, m))
			return m;
	return NULL;
}

MonoMethod*
mono_method_desc_search_in_image (MonoMethodDesc *desc, MonoImage *image)
{
	MonoClass *klass;
	const MonoTableInfo *methods;
	MonoMethod *method;
	int i;

	/* Handle short names for system classes */
	if (!desc->name_space && image == mono_defaults.corlib) {
		klass = find_system_class (desc->klass);
		if (klass)
			return mono_method_desc_search_in_class (desc, klass);
	}

	if (desc->name_space && desc->klass) {
		klass = mono_class_from_name (image, desc->name_space, desc->klass);
		if (!klass)
			return NULL;
		return mono_method_desc_search_in_class (desc, klass);
	}

	/* FIXME: Is this call necessary?  We don't use its result. */
	mono_image_get_table_info (image, MONO_TABLE_TYPEDEF);
	methods = mono_image_get_table_info (image, MONO_TABLE_METHOD);
	for (i = 0; i < mono_table_info_get_rows (methods); ++i) {
		guint32 token = mono_metadata_decode_row_col (methods, i, MONO_METHOD_NAME);
		const char *n = mono_metadata_string_heap (image, token);

		if (strcmp (n, desc->name))
			continue;
		method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
		if (mono_method_desc_full_match (desc, method))
			return method;
	}
	return NULL;
}

static const unsigned char*
dis_one (GString *str, MonoDisHelper *dh, MonoMethod *method, const unsigned char *ip, const unsigned char *end)
{
	MonoMethodHeader *header = mono_method_get_header (method);
	const MonoOpcode *opcode;
	guint32 label, token;
	gint32 sval;
	int i;
	char *tmp;
	const unsigned char* il_code = mono_method_header_get_code (header, NULL, NULL);

	label = ip - il_code;
	if (dh->indenter) {
		tmp = dh->indenter (dh, method, label);
		g_string_append (str, tmp);
		g_free (tmp);
	}
	if (dh->label_format)
		g_string_append_printf (str, dh->label_format, label);
	
	i = mono_opcode_value (&ip, end);
	ip++;
	opcode = &mono_opcodes [i];
	g_string_append_printf (str, "%-10s", mono_opcode_name (i));

	switch (opcode->argument) {
	case MonoInlineNone:
		break;
	case MonoInlineType:
	case MonoInlineField:
	case MonoInlineMethod:
	case MonoInlineTok:
	case MonoInlineSig:
		token = read32 (ip);
		if (dh->tokener) {
			tmp = dh->tokener (dh, method, token);
			g_string_append (str, tmp);
			g_free (tmp);
		} else {
			g_string_append_printf (str, "0x%08x", token);
		}
		ip += 4;
		break;
	case MonoInlineString: {
		const char *blob;
		char *s;
		size_t len2;
		char *blob2 = NULL;

		if (!image_is_dynamic (method->klass->image) && !method_is_dynamic (method)) {
			token = read32 (ip);
			blob = mono_metadata_user_string (method->klass->image, mono_metadata_token_index (token));

			len2 = mono_metadata_decode_blob_size (blob, &blob);
			len2 >>= 1;

#ifdef NO_UNALIGNED_ACCESS
			/* The blob might not be 2 byte aligned */
			blob2 = g_malloc ((len2 * 2) + 1);
			memcpy (blob2, blob, len2 * 2);
#else
			blob2 = (char*)blob;
#endif

#if G_BYTE_ORDER != G_LITTLE_ENDIAN
			{
				guint16 *buf = g_new (guint16, len2 + 1);
				int i;

				for (i = 0; i < len2; ++i)
					buf [i] = GUINT16_FROM_LE (((guint16*)blob2) [i]);
				s = g_utf16_to_utf8 (buf, len2, NULL, NULL, NULL);
				g_free (buf);
			}
#else
				s = g_utf16_to_utf8 ((gunichar2*)blob2, len2, NULL, NULL, NULL);
#endif

			g_string_append_printf (str, "\"%s\"", s);
			g_free (s);
			if (blob != blob2)
				g_free (blob2);
		}
		ip += 4;
		break;
	}
	case MonoInlineVar:
		g_string_append_printf (str, "%d", read16 (ip));
		ip += 2;
		break;
	case MonoShortInlineVar:
		g_string_append_printf (str, "%d", (*ip));
		ip ++;
		break;
	case MonoInlineBrTarget:
		sval = read32 (ip);
		ip += 4;
		if (dh->label_target)
			g_string_append_printf (str, dh->label_target, ip + sval - il_code);
		else
			g_string_append_printf (str, "%d", sval);
		break;
	case MonoShortInlineBrTarget:
		sval = *(const signed char*)ip;
		ip ++;
		if (dh->label_target)
			g_string_append_printf (str, dh->label_target, ip + sval - il_code);
		else
			g_string_append_printf (str, "%d", sval);
		break;
	case MonoInlineSwitch: {
		const unsigned char *end;
		sval = read32 (ip);
		ip += 4;
		end = ip + sval * 4;
		g_string_append_c (str, '(');
		for (i = 0; i < sval; ++i) {
			if (i > 0)
				g_string_append (str, ", ");
			label = read32 (ip);
			if (dh->label_target)
				g_string_append_printf (str, dh->label_target, end + label - il_code);
			else
				g_string_append_printf (str, "%d", label);
			ip += 4;
		}
		g_string_append_c (str, ')');
		break;
	}
	case MonoInlineR: {
		double r;
		readr8 (ip, &r);
		g_string_append_printf (str, "%g", r);
		ip += 8;
		break;
	}
	case MonoShortInlineR: {
		float r;
		readr4 (ip, &r);
		g_string_append_printf (str, "%g", r);
		ip += 4;
		break;
	}
	case MonoInlineI:
		g_string_append_printf (str, "%d", (gint32)read32 (ip));
		ip += 4;
		break;
	case MonoShortInlineI:
		g_string_append_printf (str, "%d", *(const signed char*)ip);
		ip ++;
		break;
	case MonoInlineI8:
		ip += 8;
		break;
	default:
		g_assert_not_reached ();
	}
	if (dh->newline)
		g_string_append (str, dh->newline);

	mono_metadata_free_mh (header);
	return ip;
}

static MonoDisHelper
default_dh = {
	"\n",
	"IL_%04x: ", /* label_format */
	"IL_%04x", /* label_target */
	NULL, /* indenter */
	NULL, /* tokener */
	NULL  /* user data */
};

char*
mono_disasm_code_one (MonoDisHelper *dh, MonoMethod *method, const guchar *ip, const guchar **endp)
{
	char *result;
	GString *res = g_string_new ("");

	if (!dh)
		dh = &default_dh;
	/* set ip + 2 as the end: this is just a debugging method */
	ip = dis_one (res, dh, method, ip, ip + 2);
	if (endp)
		*endp = ip;
	
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

char*
mono_disasm_code (MonoDisHelper *dh, MonoMethod *method, const guchar *ip, const guchar* end)
{
	char *result;
	GString *res = g_string_new ("");

	if (!dh)
		dh = &default_dh;
	while (ip < end) {
		ip = dis_one (res, dh, method, ip, end);
	}
	
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

char *
mono_field_full_name (MonoClassField *field)
{
	char *res;
	const char *nspace = field->parent->name_space;

	res = g_strdup_printf ("%s%s%s:%s", nspace, *nspace ? "." : "",
						   field->parent->name, mono_field_get_name (field));

	return res;
}

char *
mono_method_get_name_full (MonoMethod *method, gboolean signature, MonoTypeNameFormat format)
{
	char *res;
	char wrapper [64];
	char *klass_desc;
	char *inst_desc = NULL;

	if (format == MONO_TYPE_NAME_FORMAT_IL)
		klass_desc = mono_type_full_name (&method->klass->byval_arg);
	else
		klass_desc = mono_type_get_name_full (&method->klass->byval_arg, format);

	if (method->is_inflated && ((MonoMethodInflated*)method)->context.method_inst) {
		GString *str = g_string_new ("");
		if (format == MONO_TYPE_NAME_FORMAT_IL)
			g_string_append (str, "<");
		else
			g_string_append (str, "[");
		ginst_get_desc (str, ((MonoMethodInflated*)method)->context.method_inst);
		if (format == MONO_TYPE_NAME_FORMAT_IL)
			g_string_append_c (str, '>');
		else
			g_string_append_c (str, ']');

		inst_desc = str->str;
		g_string_free (str, FALSE);
	} else if (method->is_generic) {
		MonoGenericContainer *container = mono_method_get_generic_container (method);

		GString *str = g_string_new ("");
		if (format == MONO_TYPE_NAME_FORMAT_IL)
			g_string_append (str, "<");
		else
			g_string_append (str, "[");
		ginst_get_desc (str, container->context.method_inst);
		if (format == MONO_TYPE_NAME_FORMAT_IL)
			g_string_append_c (str, '>');
		else
			g_string_append_c (str, ']');

		inst_desc = str->str;
		g_string_free (str, FALSE);
	}

	if (method->wrapper_type != MONO_WRAPPER_NONE)
		sprintf (wrapper, "(wrapper %s) ", wrapper_type_to_str (method->wrapper_type));
	else
		strcpy (wrapper, "");

	if (signature) {
		char *tmpsig = mono_signature_get_desc (mono_method_signature (method), TRUE);

		if (method->wrapper_type != MONO_WRAPPER_NONE)
			sprintf (wrapper, "(wrapper %s) ", wrapper_type_to_str (method->wrapper_type));
		else
			strcpy (wrapper, "");
		res = g_strdup_printf ("%s%s:%s%s (%s)", wrapper, klass_desc, 
							   method->name, inst_desc ? inst_desc : "", tmpsig);
		g_free (tmpsig);
	} else {
		res = g_strdup_printf ("%s%s:%s%s", wrapper, klass_desc,
							   method->name, inst_desc ? inst_desc : "");
	}

	g_free (klass_desc);
	g_free (inst_desc);

	return res;
}

char *
mono_method_full_name (MonoMethod *method, gboolean signature)
{
	return mono_method_get_name_full (method, signature, MONO_TYPE_NAME_FORMAT_IL);
}

static const char*
print_name_space (MonoClass *klass)
{
	if (klass->nested_in) {
		print_name_space (klass->nested_in);
		g_print ("%s", klass->nested_in->name);
		return "/";
	}
	if (klass->name_space [0]) {
		g_print ("%s", klass->name_space);
		return ".";
	}
	return "";
}

/**
 * mono_object_describe:
 *
 * Prints to stdout a small description of the object @obj.
 * For use in a debugger.
 */
void
mono_object_describe (MonoObject *obj)
{
	MonoClass* klass;
	const char* sep;
	if (!obj) {
		g_print ("(null)\n");
		return;
	}
	klass = mono_object_class (obj);
	if (klass == mono_defaults.string_class) {
		char *utf8 = mono_string_to_utf8 ((MonoString*)obj);
		if (strlen (utf8) > 60) {
			utf8 [57] = '.';
			utf8 [58] = '.';
			utf8 [59] = '.';
			utf8 [60] = 0;
		}
		g_print ("String at %p, length: %d, '%s'\n", obj, mono_string_length ((MonoString*) obj), utf8);
		g_free (utf8);
	} else if (klass->rank) {
		MonoArray *array = (MonoArray*)obj;
		sep = print_name_space (klass);
		g_print ("%s%s", sep, klass->name);
		g_print (" at %p, rank: %d, length: %d\n", obj, klass->rank, (int)mono_array_length (array));
	} else {
		sep = print_name_space (klass);
		g_print ("%s%s", sep, klass->name);
		g_print (" object at %p (klass: %p)\n", obj, klass);
	}

}

static void
print_field_value (const char *field_ptr, MonoClassField *field, int type_offset)
{
	MonoType *type;
	g_print ("At %p (ofs: %2d) %s: ", field_ptr, field->offset + type_offset, mono_field_get_name (field));
	type = mono_type_get_underlying_type (field->type);

	switch (type->type) {
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
		g_print ("%p\n", *(const void**)field_ptr);
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_ARRAY:
		mono_object_describe (*(MonoObject**)field_ptr);
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (type)) {
			mono_object_describe (*(MonoObject**)field_ptr);
			break;
		} else {
			/* fall through */
		}
	case MONO_TYPE_VALUETYPE: {
		MonoClass *k = mono_class_from_mono_type (type);
		g_print ("%s ValueType (type: %p) at %p\n", k->name, k, field_ptr);
		break;
	}
	case MONO_TYPE_I1:
		g_print ("%d\n", *(gint8*)field_ptr);
		break;
	case MONO_TYPE_U1:
		g_print ("%d\n", *(guint8*)field_ptr);
		break;
	case MONO_TYPE_I2:
		g_print ("%d\n", *(gint16*)field_ptr);
		break;
	case MONO_TYPE_U2:
		g_print ("%d\n", *(guint16*)field_ptr);
		break;
	case MONO_TYPE_I4:
		g_print ("%d\n", *(gint32*)field_ptr);
		break;
	case MONO_TYPE_U4:
		g_print ("%u\n", *(guint32*)field_ptr);
		break;
	case MONO_TYPE_I8:
		g_print ("%lld\n", (long long int)*(gint64*)field_ptr);
		break;
	case MONO_TYPE_U8:
		g_print ("%llu\n", (long long unsigned int)*(guint64*)field_ptr);
		break;
	case MONO_TYPE_R4:
		g_print ("%f\n", *(gfloat*)field_ptr);
		break;
	case MONO_TYPE_R8:
		g_print ("%f\n", *(gdouble*)field_ptr);
		break;
	case MONO_TYPE_BOOLEAN:
		g_print ("%s (%d)\n", *(guint8*)field_ptr? "True": "False", *(guint8*)field_ptr);
		break;
	case MONO_TYPE_CHAR:
		g_print ("'%c' (%d 0x%04x)\n", *(guint16*)field_ptr, *(guint16*)field_ptr, *(guint16*)field_ptr);
		break;
	default:
		g_assert_not_reached ();
		break;
	}
}

static void
objval_describe (MonoClass *class, const char *addr)
{
	MonoClassField *field;
	MonoClass *p;
	const char *field_ptr;
	gssize type_offset = 0;
	if (class->valuetype)
		type_offset = -sizeof (MonoObject);

	for (p = class; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		int printed_header = FALSE;
		while ((field = mono_class_get_fields (p, &iter))) {
			if (field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA))
				continue;

			if (p != class && !printed_header) {
				const char *sep;
				g_print ("In class ");
				sep = print_name_space (p);
				g_print ("%s%s:\n", sep, p->name);
				printed_header = TRUE;
			}
			field_ptr = (const char*)addr + field->offset + type_offset;

			print_field_value (field_ptr, field, type_offset);
		}
	}
}

/**
 * mono_object_describe_fields:
 *
 * Prints to stdout a small description of each field of the object @obj.
 * For use in a debugger.
 */
void
mono_object_describe_fields (MonoObject *obj)
{
	MonoClass *class = mono_object_class (obj);
	objval_describe (class, (char*)obj);
}

/**
 * mono_value_describe_fields:
 *
 * Prints to stdout a small description of each field of the value type
 * stored at @addr of type @klass.
 * For use in a debugger.
 */
void
mono_value_describe_fields (MonoClass* klass, const char* addr)
{
	objval_describe (klass, addr);
}

/**
 * mono_class_describe_statics:
 *
 * Prints to stdout a small description of each static field of the type @klass
 * in the current application domain.
 * For use in a debugger.
 */
void
mono_class_describe_statics (MonoClass* klass)
{
	MonoClassField *field;
	MonoClass *p;
	const char *field_ptr;
	MonoVTable *vtable = mono_class_vtable_full (mono_domain_get (), klass, FALSE);
	const char *addr;

	if (!vtable)
		return;
	if (!(addr = mono_vtable_get_static_field_data (vtable)))
		return;

	for (p = klass; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		while ((field = mono_class_get_fields (p, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
				continue;
			if (!(field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA)))
				continue;
			// Special static fields don't have a domain-level static slot
			if (mono_class_field_is_special_static (field))
				continue;

			field_ptr = (const char*)addr + field->offset;

			print_field_value (field_ptr, field, 0);
		}
	}
}

/**
 * mono_print_method_code
 * @MonoMethod: a pointer to the method
 *
 * This method is used from a debugger to print the code of the method.
 *
 * This prints the IL code of the method in the standard output.
 */
void
mono_method_print_code (MonoMethod *method)
{
	char *code;
	MonoMethodHeader *header = mono_method_get_header (method);
	if (!header) {
		printf ("METHOD HEADER NOT FOUND\n");
		return;
	}
	code = mono_disasm_code (0, method, header->code, header->code + header->code_size);
	printf ("CODE FOR %s:\n%s\n", mono_method_full_name (method, TRUE), code);
	g_free (code);
}
