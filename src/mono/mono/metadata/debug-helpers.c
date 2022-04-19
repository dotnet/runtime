/**
 * \file
 *
 * Author:
 *	Mono Project (http://www.mono-project.com)
 *
 * Copyright (C) 2005-2008 Novell, Inc. (http://www.novell.com)
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */

#include <config.h>
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
#include "mono/metadata/abi-details.h"
#ifdef MONO_CLASS_DEF_PRIVATE
/* Rationale: we want the functions in this file to work even when everything
 * is broken.  They may be called from a debugger session, for example.  If
 * MonoClass getters include assertions or trigger class loading, we don't want
 * that kicked off by a call to one of the functions in here.
 */
#define REALLY_INCLUDE_CLASS_DEF 1
#include <mono/metadata/class-private-definition.h>
#undef REALLY_INCLUDE_CLASS_DEF
#endif


struct MonoMethodDesc {
	char *name_space;
	char *klass;
	char *name;
	char *args;
	guint num_args;
	gboolean include_namespace, klass_glob, name_glob;
};

// This, instead of an array of pointers, to optimize away a pointer and a relocation per string.
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
#define WRAPPER(a,b) offsetof (struct msgstr_t, MSGSTRFIELD(__LINE__)),
#include "wrapper-types.h"
#undef WRAPPER
};

const char*
mono_wrapper_type_to_str (guint32 wrapper_type)
{
	g_assert (wrapper_type < MONO_WRAPPER_NUM);

	return (const char*)&opstr + opidx [wrapper_type];
}

static void
append_class_name (GString *res, MonoClass *klass, gboolean include_namespace)
{
	if (!klass) {
		g_string_append (res, "Unknown");
		return;
	}
	if (klass->nested_in) {
		append_class_name (res, klass->nested_in, include_namespace);
		g_string_append_c (res, '/');
	}
	if (include_namespace && *(klass->name_space)) {
		g_string_append (res, klass->name_space);
		g_string_append_c (res, '.');
	}
	g_string_append (res, klass->name);
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

static void
mono_custom_modifiers_get_desc (GString *res, const MonoType *type, gboolean include_namespace)
{
	ERROR_DECL (error);
	int count = mono_type_custom_modifier_count (type);
	for (int i = 0; i < count; ++i) {
		gboolean required;
		MonoType *cmod_type = mono_type_get_custom_modifier (type, i, &required, error);
		mono_error_assert_ok (error);
		if (required)
			g_string_append (res, " modreq(");
		else
			g_string_append (res, " modopt(");
		mono_type_get_desc (res, cmod_type, include_namespace);
		g_string_append (res, ")");
	}
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
		mono_type_get_desc (res, &type->data.array->eklass->_byval_arg, include_namespace);
		g_string_append_c (res, '[');
		for (i = 1; i < type->data.array->rank; ++i)
			g_string_append_c (res, ',');
		g_string_append_c (res, ']');
		break;
	case MONO_TYPE_SZARRAY:
		mono_type_get_desc (res, &type->data.klass->_byval_arg, include_namespace);
		g_string_append (res, "[]");
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		append_class_name (res, type->data.klass, include_namespace);
		break;
	case MONO_TYPE_GENERICINST: {
		MonoGenericContext *context;

		mono_type_get_desc (res, &type->data.generic_class->container_class->_byval_arg, include_namespace);
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
	if (type->has_cmods) {
		mono_custom_modifiers_get_desc (res, type, include_namespace);
	}
	if (m_type_is_byref (type))
		g_string_append_c (res, '&');
}

/**
 * mono_type_full_name:
 */
char*
mono_type_full_name (MonoType *type)
{
	GString *str;

	str = g_string_new ("");
	mono_type_get_desc (str, type, TRUE);
	return g_string_free (str, FALSE);
}

/**
 * mono_signature_get_desc:
 */
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

char*
mono_signature_full_name (MonoMethodSignature *sig)
{
	int i;
	char *result;
	GString *res;

	if (!sig)
		return g_strdup ("<invalid signature>");

	res = g_string_new ("");

	mono_type_get_desc (res, sig->ret, TRUE);
	g_string_append_c (res, '(');
	for (i = 0; i < sig->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		mono_type_get_desc (res, sig->params [i], TRUE);
	}
	g_string_append_c (res, ')');
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

void
mono_ginst_get_desc (GString *str, MonoGenericInst *ginst)
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
		mono_ginst_get_desc (str, context->class_inst);
	if (context->method_inst) {
		if (context->class_inst)
			g_string_append (str, "; ");
		mono_ginst_get_desc (str, context->method_inst);
	}

	g_string_append (str, ">");
	res = g_strdup (str->str);
	g_string_free (str, TRUE);
	return res;
}

/**
 * mono_method_desc_new:
 * \param name the method name.
 * \param include_namespace whether the name includes a namespace or not.
 *
 * Creates a method description for \p name, which conforms to the following
 * specification:
 *
 * <code>[namespace.]classname:methodname[(args...)]</code>
 *
 * in all the loaded assemblies.
 *
 * Both classname and methodname can contain <code>*</code> which matches anything.
 *
 * \returns a parsed representation of the method description.
 */
MonoMethodDesc*
mono_method_desc_new (const char *name, gboolean include_namespace)
{
	MonoMethodDesc *result;
	char *class_name, *class_nspace, *method_name, *use_args, *end;
	int use_namespace;
	int generic_delim_stack;

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
		generic_delim_stack = 0;
		while (*end) {
			if (*end == '<')
				generic_delim_stack++;
			else if (*end == '>')
				generic_delim_stack--;

			if (*end == ',' && generic_delim_stack == 0)
				result->num_args++;
			++end;
		}
	}

	return result;
}

/**
 * mono_method_desc_from_method:
 */
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
 * \param desc method description to be released
 * Releases the \c MonoMethodDesc object \p desc.
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

/**
 * mono_method_desc_match:
 * \param desc \c MonoMethoDescription
 * \param method \c MonoMethod to test
 *
 * Determines whether the specified \p method matches the provided \p desc description.
 *
 * namespace and class are supposed to match already if this function is used.
 * \returns TRUE if the method matches the description, FALSE otherwise.
 */
gboolean
mono_method_desc_match (MonoMethodDesc *desc, MonoMethod *method)
{
	char *sig;
	gboolean name_match;

	if (desc->name_glob && !strcmp (desc->name, "*"))
		return TRUE;
#if 0
	/* FIXME: implement g_pattern_match_simple in eglib */
	if (desc->name_glob && g_pattern_match_simple (desc->name, method->name))
		return TRUE;
#endif
	name_match = strcmp (desc->name, method->name) == 0;
	if (!name_match)
		return FALSE;
	if (!desc->args)
		return TRUE;
	if (desc->num_args != mono_method_signature_internal (method)->param_count)
		return FALSE;
	sig = mono_signature_get_desc (mono_method_signature_internal (method), desc->include_namespace);
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
	gboolean is_terminal = TRUE;

	if (desc->klass_glob && !strcmp (desc->klass, "*"))
		return TRUE;
#ifndef _EGLIB_MAJOR
	if (desc->klass_glob && g_pattern_match_simple (desc->klass, klass->name))
		return TRUE;
#endif
	if (desc->klass[pos] == '/')
		is_terminal = FALSE;

	p = my_strrchr (desc->klass, '/', &pos);
	if (!p) {
		if (is_terminal && strcmp (desc->klass, klass->name))
			return FALSE;
		if (!is_terminal && strncmp (desc->klass, klass->name, pos))
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

/**
 * mono_method_desc_is_full:
 */
gboolean
mono_method_desc_is_full (MonoMethodDesc *desc)
{
	return desc->klass && desc->klass[0] != '\0';
}

/**
 * mono_method_desc_full_match:
 * \param desc A method description that you created with mono_method_desc_new
 * \param method a MonoMethod instance that you want to match against
 *
 * This method is used to check whether the method matches the provided
 * description, by making sure that the method matches both the class and the method parameters.
 *
 * \returns TRUE if the specified method matches the specified description, FALSE otherwise.
 */
gboolean
mono_method_desc_full_match (MonoMethodDesc *desc, MonoMethod *method)
{
	if (!desc)
		return FALSE;
	if (!desc->klass)
		return FALSE;
	if (!match_class (desc, (int)strlen (desc->klass), method->klass))
		return FALSE;

	return mono_method_desc_match (desc, method);
}

/**
 * mono_method_desc_search_in_class:
 */
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

/**
 * mono_method_desc_search_in_image:
 */
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
		klass = mono_class_try_load_from_name (image, desc->name_space, desc->klass);
		if (!klass)
			return NULL;
		return mono_method_desc_search_in_class (desc, klass);
	}

	/* FIXME: Is this call necessary?  We don't use its result. */
	mono_image_get_table_info (image, MONO_TABLE_TYPEDEF);
	methods = mono_image_get_table_info (image, MONO_TABLE_METHOD);
	for (i = 0; i < mono_table_info_get_rows (methods); ++i) {
		ERROR_DECL (error);
		guint32 token = mono_metadata_decode_row_col (methods, i, MONO_METHOD_NAME);
		const char *n = mono_metadata_string_heap (image, token);

		if (strcmp (n, desc->name))
			continue;
		method = mono_get_method_checked (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL, NULL, error);
		if (!method) {
			mono_error_cleanup (error);
			continue;
		}
		if (mono_method_desc_full_match (desc, method))
			return method;
	}
	return NULL;
}

static const unsigned char*
dis_one (GString *str, MonoDisHelper *dh, MonoMethod *method, const unsigned char *ip, const unsigned char *end)
{
	ERROR_DECL (error);
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	const MonoOpcode *opcode;
	guint32 label, token;
	gint32 sval;
	int i;
	char *tmp;
	const unsigned char* il_code;

	if (!header) {
		g_string_append_printf (str, "could not disassemble, bad header due to %s", mono_error_get_message (error));
		mono_error_cleanup (error);
		return end;
	}
	il_code = mono_method_header_get_code (header, NULL, NULL);

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
				s = g_utf16_to_utf8 (buf, (glong)len2, NULL, NULL, NULL);
				g_free (buf);
			}
#else
				s = g_utf16_to_utf8 ((gunichar2*)blob2, (glong)len2, NULL, NULL, NULL);
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
		const unsigned char *sval_end;
		sval = read32 (ip);
		ip += 4;
		sval_end = ip + sval * 4;
		g_string_append_c (str, '(');
		for (i = 0; i < sval; ++i) {
			if (i > 0)
				g_string_append (str, ", ");
			label = read32 (ip);
			if (dh->label_target)
				g_string_append_printf (str, dh->label_target, sval_end + label - il_code);
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

/**
 * mono_disasm_code_one:
 */
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

/**
 * mono_disasm_code:
 */
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

/**
 * mono_field_full_name:
 * \param field field to retrieve information for
 * \returns the full name for the field, made up of the namespace, type name and the field name.
 */
char *
mono_field_full_name (MonoClassField *field)
{
	char *res;
	const char *nspace = m_field_get_parent (field)->name_space;

	res = g_strdup_printf ("%s%s%s:%s", nspace, *nspace ? "." : "",
						   m_field_get_parent (field)->name, mono_field_get_name (field));

	return res;
}

char *
mono_method_get_name_full (MonoMethod *method, gboolean signature, gboolean ret, MonoTypeNameFormat format)
{
	char *res;
	char wrapper [64];
	char *klass_desc;
	char *inst_desc = NULL;
	ERROR_DECL (error);

	const char *class_method_separator = ":";
	const char *method_sig_space = " ";
	if (format == MONO_TYPE_NAME_FORMAT_REFLECTION) {
		class_method_separator = ".";
		method_sig_space = "";
	}

	if (format == MONO_TYPE_NAME_FORMAT_IL)
		klass_desc = mono_type_full_name (&method->klass->_byval_arg);
	else
		klass_desc = mono_type_get_name_full (&method->klass->_byval_arg, format);

	if (method->is_inflated && ((MonoMethodInflated*)method)->context.method_inst) {
		GString *str = g_string_new ("");
		if (format == MONO_TYPE_NAME_FORMAT_IL)
			g_string_append (str, "<");
		else
			g_string_append (str, "[");
		mono_ginst_get_desc (str, ((MonoMethodInflated*)method)->context.method_inst);
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
		mono_ginst_get_desc (str, container->context.method_inst);
		if (format == MONO_TYPE_NAME_FORMAT_IL)
			g_string_append_c (str, '>');
		else
			g_string_append_c (str, ']');

		inst_desc = str->str;
		g_string_free (str, FALSE);
	}

	if (method->wrapper_type != MONO_WRAPPER_NONE)
		sprintf (wrapper, "(wrapper %s) ", mono_wrapper_type_to_str (method->wrapper_type));
	else
		strcpy (wrapper, "");

	if (signature) {
		MonoMethodSignature *sig = mono_method_signature_checked (method, error);
		char *tmpsig;

		if (!is_ok (error)) {
			tmpsig = g_strdup_printf ("<unable to load signature>");
			mono_error_cleanup (error);
		} else {
			tmpsig = mono_signature_get_desc (sig, TRUE);
		}

		if (method->wrapper_type != MONO_WRAPPER_NONE)
			sprintf (wrapper, "(wrapper %s) ", mono_wrapper_type_to_str (method->wrapper_type));
		else
			strcpy (wrapper, "");
		if (ret && sig) {
			char *ret_str = mono_type_full_name (sig->ret);
			res = g_strdup_printf ("%s%s %s%s%s%s%s(%s)", wrapper, ret_str, klass_desc,
								   class_method_separator,
								   method->name, inst_desc ? inst_desc : "", method_sig_space, tmpsig);
			g_free (ret_str);
		} else {
			res = g_strdup_printf ("%s%s%s%s%s%s(%s)", wrapper, klass_desc,
								   class_method_separator,
								   method->name, inst_desc ? inst_desc : "", method_sig_space, tmpsig);
		}
		g_free (tmpsig);
	} else {
		res = g_strdup_printf ("%s%s%s%s%s", wrapper, klass_desc,
							   class_method_separator,
							   method->name, inst_desc ? inst_desc : "");
	}

	g_free (klass_desc);
	g_free (inst_desc);

	return res;
}

/**
 * mono_method_full_name:
 */
char *
mono_method_full_name (MonoMethod *method, gboolean signature)
{
	char *res;
	MONO_ENTER_GC_UNSAFE;
	res = mono_method_get_name_full (method, signature, FALSE, MONO_TYPE_NAME_FORMAT_IL);
	MONO_EXIT_GC_UNSAFE;
	return res;
}

char *
mono_method_get_full_name (MonoMethod *method)
{
	return mono_method_get_name_full (method, TRUE, TRUE, MONO_TYPE_NAME_FORMAT_IL);
}

/**
 * mono_method_get_reflection_name:
 *
 * Returns the name of the method, including signature, using the same formating as reflection.
 */
char *
mono_method_get_reflection_name (MonoMethod *method)
{
	return mono_method_get_name_full (method, TRUE, FALSE, MONO_TYPE_NAME_FORMAT_REFLECTION);
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
 * Prints to stdout a small description of the object \p obj.
 * For use in a debugger.
 */
void
mono_object_describe (MonoObject *obj)
{
	ERROR_DECL (error);
	MonoClass* klass;
	const char* sep;
	if (!obj) {
		g_print ("(null)\n");
		return;
	}
	klass = mono_object_class (obj);
	if (klass == mono_defaults.string_class) {
		char *utf8 = mono_string_to_utf8_checked_internal ((MonoString*)obj, error);
		mono_error_cleanup (error); /* FIXME don't swallow the error */
		if (utf8 && strlen (utf8) > 60) {
			utf8 [57] = '.';
			utf8 [58] = '.';
			utf8 [59] = '.';
			utf8 [60] = 0;
		}
		if (utf8) {
			g_print ("String at %p, length: %d, '%s'\n", obj, mono_string_length_internal ((MonoString*) obj), utf8);
		} else {
			g_print ("String at %p, length: %d, unable to decode UTF16\n", obj, mono_string_length_internal ((MonoString*) obj));
		}
		g_free (utf8);
	} else if (klass->rank) {
		MonoArray *array = (MonoArray*)obj;
		sep = print_name_space (klass);
		g_print ("%s%s", sep, klass->name);
		g_print (" at %p, rank: %d, length: %d\n", obj, klass->rank, (int)mono_array_length_internal (array));
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
	g_print ("At %p (ofs: %2d) %s: ", field_ptr, m_field_is_from_update (field) ? -1 : (field->offset + type_offset), mono_field_get_name (field));
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
		MonoClass *k = mono_class_from_mono_type_internal (type);
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
		g_print ("%" PRId64 "\n", *(gint64*)field_ptr);
		break;
	case MONO_TYPE_U8:
		g_print ("%" PRIu64 "\n", *(guint64*)field_ptr);
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
objval_describe (MonoClass *klass, const char *addr)
{
	MonoClassField *field;
	MonoClass *p;
	const char *field_ptr;
	gssize type_offset = 0;

	if (klass->valuetype)
		type_offset = - MONO_ABI_SIZEOF (MonoObject);

	for (p = klass; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		int printed_header = FALSE;
		while ((field = mono_class_get_fields_internal (p, &iter))) {
			if (field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA))
				continue;
			/* TODO: metadata-update: print something here */
			if (m_field_is_from_update (field))
				continue;

			if (p != klass && !printed_header) {
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
 * Prints to stdout a small description of each field of the object \p obj.
 * For use in a debugger.
 */
void
mono_object_describe_fields (MonoObject *obj)
{
	MonoClass *klass = mono_object_class (obj);
	objval_describe (klass, (char*)obj);
}

/**
 * mono_value_describe_fields:
 *
 * Prints to stdout a small description of each field of the value type
 * stored at \p addr of type \p klass.
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
 * Prints to stdout a small description of each static field of the type \p klass
 * in the current application domain.
 * For use in a debugger.
 */
void
mono_class_describe_statics (MonoClass* klass)
{
	ERROR_DECL (error);
	MonoClassField *field;
	MonoClass *p;
	const char *field_ptr;
	MonoVTable *vtable = mono_class_vtable_checked (klass, error);
	const char *addr;

	if (!vtable || !is_ok (error)) {
		mono_error_cleanup (error);
		return;
	}

	if (!(addr = (const char *)mono_vtable_get_static_field_data (vtable)))
		return;

	for (p = klass; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		while ((field = mono_class_get_fields_internal (p, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
				continue;
			if (!(field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA)))
				continue;

			/* TODO: metadata-update: print something for added fields? */
			if (m_field_is_from_update (field))
				continue;

			field_ptr = (const char*)addr + m_field_get_offset (field);

			print_field_value (field_ptr, field, 0);
		}
	}
}

/**
 * mono_print_method_code
 * \param method: a pointer to the method
 *
 * This method is used from a debugger to print the code of the method.
 *
 * This prints the IL code of the method in the standard output.
 */
void
mono_method_print_code (MonoMethod *method)
{
	ERROR_DECL (error);
	char *code;
	MonoMethodHeader *header = mono_method_get_header_checked (method, error);
	if (!header) {
		printf ("METHOD HEADER NOT FOUND DUE TO: %s\n", mono_error_get_message (error));
		mono_error_cleanup (error);
		return;
	}
	code = mono_disasm_code (0, method, header->code, header->code + header->code_size);
	printf ("CODE FOR %s:\n%s\n", mono_method_full_name (method, TRUE), code);
	g_free (code);
}
