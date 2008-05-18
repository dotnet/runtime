
#include <string.h>
#include "mono/metadata/tokentype.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/debug-helpers.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/appdomain.h"

struct MonoMethodDesc {
	char *namespace;
	char *klass;
	char *name;
	char *args;
	guint num_args;
	gboolean include_namespace;
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
		mono_type_get_desc (res, &type->data.array->eklass->byval_arg, include_namespace);
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
		mono_type_get_desc (res, &type->data.generic_class->container_class->byval_arg, include_namespace);
		break;
	case MONO_TYPE_VAR:
	case MONO_TYPE_MVAR:
		g_string_append (res, type->data.generic_param->name);
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
 * @desc: method description to be released
 *
 * Releases the MonoMethodDesc object @desc.
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

	p = my_strrchr (desc->klass, '/', &pos);
	if (!p) {
		if (strncmp (desc->klass, klass->name, pos))
			return FALSE;
		if (desc->namespace && strcmp (desc->namespace, klass->name_space))
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
			g_string_sprintfa (str, dh->label_target, ip + sval - il_code);
		else
			g_string_sprintfa (str, "%d", sval);
		break;
	case MonoShortInlineBrTarget:
		sval = *(const signed char*)ip;
		ip ++;
		if (dh->label_target)
			g_string_sprintfa (str, dh->label_target, ip + sval - il_code);
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
				g_string_sprintfa (str, dh->label_target, end + label - il_code);
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

char *
mono_field_full_name (MonoClassField *field)
{
	char *res;
	const char *nspace = field->parent->name_space;

	res = g_strdup_printf ("%s%s%s:%s", nspace, *nspace ? "." : "",
						   field->parent->name, field->name);

	return res;
}

char *
mono_method_full_name (MonoMethod *method, gboolean signature)
{
	char *res;
	char wrapper [64];
	const char *nspace = method->klass->name_space;

	if (signature) {
		char *tmpsig = mono_signature_get_desc (mono_method_signature (method), TRUE);

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

static const char*
print_name_space (MonoClass *klass)
{
	if (klass->nested_in) {
		print_name_space (klass->nested_in);
		g_print (klass->nested_in->name);
		return "/";
	}
	if (klass->name_space [0]) {
		g_print (klass->name_space);
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
		g_print (" at %p, rank: %d, length: %d\n", obj, klass->rank, mono_array_length (array));
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
	g_print ("At %p (ofs: %2d) %s: ", field_ptr, field->offset + type_offset, field->name);
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
	const char *addr = mono_class_vtable (mono_domain_get (), klass)->data;
	if (!addr)
		return;

	for (p = klass; p != NULL; p = p->parent) {
		gpointer iter = NULL;
		while ((field = mono_class_get_fields (p, &iter))) {
			if (field->type->attrs & FIELD_ATTRIBUTE_LITERAL)
				continue;
			if (!(field->type->attrs & (FIELD_ATTRIBUTE_STATIC | FIELD_ATTRIBUTE_HAS_FIELD_RVA)))
				continue;

			field_ptr = (const char*)addr + field->offset;

			print_field_value (field_ptr, field, 0);
		}
	}
}

