
#include <string.h>
#include "mono/metadata/tokentype.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/debug-helpers.h"

struct MonoMethodDesc {
	char *namespace;
	char *klass;
	char *name;
	char *args;
	guint num_args;
	gboolean include_namespace;
};

static const char *wrapper_type_names [] = {
	"none",
	"delegate-invoke",
	"delegate-begin-invoke",
	"delegate-end-invoke",
	"runtime-invoke",
	"native-to-managed",
	"managed-to-native",
	"remoting-invoke",
	"remoting-invoke-with-check",
	"ldfld",
	"stfld",
	"synchronized",
	"dynamic-method",
	"isinst",
	"cancast",
	"proxy_isinst",
	"stelemref",
	"unknown"
};

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
	if (include_namespace && *(class->name_space))
		g_string_sprintfa (res, "%s.", class->name_space);
	g_string_sprintfa (res, "%s", class->name);
}

void
mono_type_get_desc (GString *res, MonoType *type, gboolean include_namespace) {
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
		append_class_name (res, type->data.array->eklass, include_namespace);
		g_string_sprintfa (res, "[%d]", type->data.array->rank);
		break;
	case MONO_TYPE_SZARRAY:
		mono_type_get_desc (res, &type->data.klass->byval_arg, include_namespace);
		g_string_append (res, "[]");
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE:
		append_class_name (res, type->data.klass, include_namespace);
		break;
	case MONO_TYPE_GENERICINST:
		mono_type_get_desc (res, type->data.generic_inst->generic_type, include_namespace);
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
	char *res;

	str = g_string_new ("");
	mono_type_get_desc (str, type, TRUE);

	res = g_strdup (str->str);
	g_string_free (str, TRUE);
	return res;
}

char*
mono_signature_get_desc (MonoMethodSignature *sig, gboolean include_namespace)
{
	int i;
	char *result;
	GString *res = g_string_new ("");

	for (i = 0; i < sig->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		mono_type_get_desc (res, sig->params [i], include_namespace);
	}
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

/**
 * mono_method_desc_new:
 *
 * Creates a method description for `name', which conforms to the following
 * specification:
 *
 * [namespace.]classname:methodname[(args...)]
 *
 * in all the loaded assemblies.
 *
 * Returns a parsed representation of the method description.
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
	*method_name++ = 0;
	/* allow two :: to separate the method name */
	if (*method_name == ':')
		method_name++;
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
	result->namespace = use_namespace? class_nspace: NULL;
	result->args = use_args? use_args: NULL;
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
	result->namespace = g_strdup (method->klass->name_space);

	return result;
}

/**
 * mono_method_desc_free:
 *
 * Releases the MonoMethodDesc object `desc'.
 */
void
mono_method_desc_free (MonoMethodDesc *desc)
{
	if (desc->namespace)
		g_free (desc->namespace);
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
	if (strcmp (desc->name, method->name))
		return FALSE;
	if (!desc->args)
		return TRUE;
	if (desc->num_args != method->signature->param_count)
		return FALSE;
	sig = mono_signature_get_desc (method->signature, desc->include_namespace);
	if (strcmp (sig, desc->args)) {
		g_free (sig);
		return FALSE;
	}
	g_free (sig);
	return TRUE;
}

gboolean
mono_method_desc_full_match (MonoMethodDesc *desc, MonoMethod *method)
{
	if (strcmp (desc->klass, method->klass->name))
		return FALSE;
	if (desc->namespace && strcmp (desc->namespace, method->klass->name_space))
		return FALSE;
	return mono_method_desc_match (desc, method);
}

MonoMethod*
mono_method_desc_search_in_class (MonoMethodDesc *desc, MonoClass *klass)
{
	int i;

	mono_class_init (klass);
	for (i = 0; i < klass->method.count; ++i) {
		if (mono_method_desc_match (desc, klass->methods [i]))
			return klass->methods [i];
	}
	return NULL;
}

MonoMethod*
mono_method_desc_search_in_image (MonoMethodDesc *desc, MonoImage *image)
{
	MonoClass *klass;
	const MonoTableInfo *tdef;
	const MonoTableInfo *methods;
	MonoMethod *method;
	int i;

	if (desc->namespace && desc->klass) {
		klass = mono_class_from_name (image, desc->namespace, desc->klass);
		if (!klass)
			return NULL;
		return mono_method_desc_search_in_class (desc, klass);
	}

	tdef = mono_image_get_table_info (image, MONO_TABLE_TYPEDEF);
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
	guint32 i, label, token;
	gint32 sval;
	char *tmp;

	label = ip - header->code;
	if (dh->indenter) {
		tmp = dh->indenter (dh, method, label);
		g_string_append (str, tmp);
		g_free (tmp);
	}
	if (dh->label_format)
		g_string_sprintfa (str, dh->label_format, label);
	
	i = mono_opcode_value (&ip, end);
	ip++;
	opcode = &mono_opcodes [i];
	g_string_sprintfa (str, "%-10s", mono_opcode_name (i));

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
			g_string_sprintfa (str, "0x%08x", token);
		}
		ip += 4;
		break;
	case MonoInlineString:
		/* TODO */
		ip += 4;
		break;
	case MonoInlineVar:
		g_string_sprintfa (str, "%d", read16 (ip));
		ip += 2;
		break;
	case MonoShortInlineVar:
		g_string_sprintfa (str, "%d", (*ip));
		ip ++;
		break;
	case MonoInlineBrTarget:
		sval = read32 (ip);
		ip += 4;
		if (dh->label_target)
			g_string_sprintfa (str, dh->label_target, ip + sval - header->code);
		else
			g_string_sprintfa (str, "%d", sval);
		break;
	case MonoShortInlineBrTarget:
		sval = *(const signed char*)ip;
		ip ++;
		if (dh->label_target)
			g_string_sprintfa (str, dh->label_target, ip + sval - header->code);
		else
			g_string_sprintfa (str, "%d", sval);
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
				g_string_sprintfa (str, dh->label_target, end + label - header->code);
			else
				g_string_sprintfa (str, "%d", label);
			ip += 4;
		}
		g_string_append_c (str, ')');
		break;
	}
	case MonoInlineR: {
		double r;
		readr8 (ip, &r);
		g_string_sprintfa (str, "%g", r);
		ip += 8;
		break;
	}
	case MonoShortInlineR: {
		float r;
		readr4 (ip, &r);
		g_string_sprintfa (str, "%g", r);
		ip += 4;
		break;
	}
	case MonoInlineI:
		g_string_sprintfa (str, "%d", (gint32)read32 (ip));
		ip += 4;
		break;
	case MonoShortInlineI:
		g_string_sprintfa (str, "%d", *(const signed char*)ip);
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

static const char*
wrapper_type_to_str (guint32 wrapper_type)
{
	g_assert (wrapper_type < sizeof (wrapper_type_names) / sizeof (char*));

	return wrapper_type_names [wrapper_type];
}

char *
mono_method_full_name (MonoMethod *method, gboolean signature)
{
	char *res;
	char wrapper [64];
	const char *nspace = method->klass->name_space;

	if (signature) {
		char *tmpsig = mono_signature_get_desc (method->signature, TRUE);

		if (method->wrapper_type != MONO_WRAPPER_NONE)
			sprintf (wrapper, "(wrapper %s) ", wrapper_type_to_str (method->wrapper_type));
		else
			strcpy (wrapper, "");
		res = g_strdup_printf ("%s%s%s%s:%s (%s)", wrapper, 
							   nspace, *nspace ? "." : "",
							   method->klass->name, method->name, tmpsig);
		g_free (tmpsig);
	} else {

		res = g_strdup_printf ("%02d %s%s%s:%s", method->wrapper_type,
							   nspace, *nspace ? "." : "",
							   method->klass->name, method->name);
	}

	return res;
}

MonoMethod *
mono_find_method_by_name (MonoClass *klass, const char *name, int param_count)
{
	MonoMethod *res = NULL;
	int i;

	mono_class_init (klass);

	for (i = 0; i < klass->method.count; ++i) {
		if (klass->methods [i]->name[0] == name [0] && 
		    !strcmp (name, klass->methods [i]->name) &&
		    klass->methods [i]->signature->param_count == param_count) {
			res = klass->methods [i];
			break;
		}
	}
	return res;
}

