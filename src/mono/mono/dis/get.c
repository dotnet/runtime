/*
 * get.c: Functions to get stringified values from the metadata tables.
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */
#include <config.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <glib.h>
#include "meta.h"
#include "util.h"
#include "get.h"
#include <mono/metadata/class.h>

extern gboolean substitute_with_mscorlib_p;

char *
get_typedef (MonoImage *m, int idx)
{
	guint32 cols [MONO_TYPEDEF_SIZE];

	mono_metadata_decode_row (&m->tables [MONO_TABLE_TYPEDEF], idx - 1, cols, MONO_TYPEDEF_SIZE);

	return g_strdup_printf (
		"%s.%s",
		mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAMESPACE]),
		mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAME]));
}

char *
get_module (MonoImage *m, int idx)
{
	guint32 cols [MONO_MODULE_SIZE];
	
	/*
	 * There MUST BE only one module in the Module table
	 */
	g_assert (idx == 1);
	    
	mono_metadata_decode_row (&m->tables [MONO_TABLE_MODULEREF], idx - 1, cols, MONO_MODULE_SIZE);

	return g_strdup (mono_metadata_string_heap (m, cols [MONO_MODULE_NAME]));
}

char *
get_assemblyref (MonoImage *m, int idx)
{
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	
	mono_metadata_decode_row (&m->tables [MONO_TABLE_ASSEMBLYREF], idx - 1, cols, MONO_ASSEMBLYREF_SIZE);

	return g_strdup (mono_metadata_string_heap (m, cols [MONO_ASSEMBLYREF_NAME]));
}

/*
 *
 * Returns a string representing the ArrayShape (22.2.16).
 */
static const char *
get_array_shape (MonoImage *m, const char *ptr, char **result)
{
	GString *res = g_string_new ("[");
	guint32 rank, num_sizes, num_lo_bounds;
	guint32 *sizes = NULL, *lo_bounds = NULL;
	int i, r;
	char buffer [80];
	
	rank = mono_metadata_decode_value (ptr, &ptr);
	num_sizes = mono_metadata_decode_value (ptr, &ptr);

	if (num_sizes > 0)
		sizes = g_new (guint32, num_sizes);
	
	for (i = 0; i < num_sizes; i++)
		sizes [i] = mono_metadata_decode_value (ptr, &ptr);

	num_lo_bounds = mono_metadata_decode_value (ptr, &ptr);
	if (num_lo_bounds > 0)
		lo_bounds = g_new (guint32, num_lo_bounds);
	
	for (i = 0; i < num_lo_bounds; i++)
		lo_bounds [i] = mono_metadata_decode_value (ptr, &ptr);

	for (r = 0; r < rank; r++){
		if (r < num_sizes){
			if (r < num_lo_bounds){
				sprintf (buffer, "%d..%d", lo_bounds [r], lo_bounds [r] + sizes [r] - 1);
			} else {
				sprintf (buffer, "0..%d", sizes [r] - 1);
			}
		} else
			buffer [0] = 0;
		
		g_string_append (res, buffer);
		if ((r + 1) != rank)
			g_string_append (res, ", ");
	}
	g_string_append (res, "]");
	
	if (sizes)
		g_free (sizes);

	if (lo_bounds)
		g_free (lo_bounds);

	*result = res->str;
	g_string_free (res, FALSE);

	return ptr;
}

/**
 * get_typespec:
 * @m: metadata context
 * @blob_idx: index into the blob heap
 *
 * Returns the stringified representation of a TypeSpec signature (22.2.17)
 */
char *
get_typespec (MonoImage *m, guint32 idx)
{
	guint32 cols [MONO_TYPESPEC_SIZE];
	const char *ptr;
	char *s, *result;
	GString *res = g_string_new ("");
	int len;

	mono_metadata_decode_row (&m->tables [MONO_TABLE_TYPESPEC], idx-1, cols, MONO_TYPESPEC_SIZE);
	ptr = mono_metadata_blob_heap (m, cols [MONO_TYPESPEC_SIGNATURE]);
	len = mono_metadata_decode_value (ptr, &ptr);
	
	switch (*ptr++){
	case MONO_TYPE_PTR:
		ptr = get_custom_mod (m, ptr, &s);
		if (s){
			g_string_append (res, s);
			g_string_append_c (res, ' ');
			g_free (s);
		}
		
		if (*ptr == MONO_TYPE_VOID)
			g_string_append (res, "void");
		else {
			ptr = get_type (m, ptr, &s);
			if (s)
				g_string_append (res, s);
		}
		break;
		
	case MONO_TYPE_FNPTR:
		g_string_append (res, "FNPTR ");
		/*
		 * we assume MethodRefSig, as we do not know
		 * whether it is a MethodDefSig or a MethodRefSig.
		 */
		printf ("\n FNPTR:\n");
		
		hex_dump (ptr, 0, 40);
		break;
			
	case MONO_TYPE_ARRAY:
		ptr = get_type (m, ptr, &s);
		g_string_append (res, s);
		g_free (s);
		g_string_append_c (res, ' ');
		ptr = get_array_shape (m, ptr, &s);
		g_string_append (res, s);
		g_free (s);
		break;
		
	case MONO_TYPE_SZARRAY:
		ptr = get_custom_mod (m, ptr, &s);
		if (s){
			g_string_append (res, s);
			g_string_append_c (res, ' ');
			g_free (s);
		}
		ptr = get_type (m, ptr, &s);
		g_string_append (res, s);
		g_string_append (res, "[]");
		g_free (s);
	}

	result = res->str;
	g_string_free (res, FALSE);

	return result;
}

char *
get_typeref (MonoImage *m, int idx)
{
	guint32 cols [MONO_TYPEREF_SIZE];
	const char *s, *t;
	char *x, *ret;
	guint32 rs_idx, table;
	
	mono_metadata_decode_row (&m->tables [MONO_TABLE_TYPEREF], idx - 1, cols, MONO_TYPEREF_SIZE);

	t = mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAME]);
	s = mono_metadata_string_heap (m, cols [MONO_TYPEREF_NAMESPACE]);

	rs_idx = cols [MONO_TYPEREF_SCOPE] >> 2;
	/*
	 * Two bits in Beta2.
	 * ECMA spec claims 3 bits
	 */
	table = cols [MONO_TYPEREF_SCOPE] & 3;
	
	switch (table){
	case 0: /* Module */
		x = get_module (m, rs_idx);
		ret = g_strdup_printf ("TODO:TypeRef-Module [%s] %s.%s", x, s, t);
		g_free (x);
		break;

	case 1: /* ModuleRef */
		ret = g_strdup_printf ("TODO:TypeRef-ModuleRef (%s.%s)", s, t);
		break;
			      
	case 2: /*
		 * AssemblyRef (ECMA docs claim it is 3, but it looks to
		 * me like it is 2 (tokens are prefixed with 0x23)
		 */
		x = get_assemblyref (m, rs_idx);
		ret = g_strdup_printf ("[%s]%s.%s", x, s, t);
		g_free (x);
		break;
		
	case 4: /* TypeRef */
		ret =  g_strdup_printf ("TODO:TypeRef-TypeRef: TYPEREF! (%s.%s)", s, t);
		break;
		
	default:
		ret = g_strdup_printf ("Unknown table in TypeRef %d", table);
	}

	return ret;
}

/**
 * get_typedef_or_ref:
 * @m: metadata context
 * @dor_token: def or ref encoded index
 *
 * Low two bits contain table to lookup from
 * high bits contain the index into the def or ref table
 *
 * Returns: a stringified version of the MethodDef or MethodRef
 * at (dor_token >> 2) 
 */
char *
get_typedef_or_ref (MonoImage *m, guint32 dor_token)
{
	char *temp = NULL, *s;
	int table, idx;

	/*
	 * low 2 bits contain encoding
	 */
	table = dor_token & 0x03;
	idx = dor_token >> 2;
	
	switch (table){
	case 0: /* TypeDef */
		temp = get_typedef (m, idx);
		s = g_strdup_printf ("%s", temp);
		break;
		
	case 1: /* TypeRef */
		temp = get_typeref (m, idx);
		s = g_strdup_printf ("%s", temp);
		break;
		
	case 2: /* TypeSpec */
		s = get_typespec (m, idx);
		break;

	default:
		g_error ("Unhandled encoding for typedef-or-ref coded index");

	}
	
	if (temp)
		g_free (temp);

	return s;
}

/** 
 * get_encoded_typedef_or_ref:
 * @m: metadata context 
 * @ptr: location to decode from.
 * @result: pointer to string where resulting decoded string is stored
 *
 * result will point to a g_malloc()ed string.
 *
 * Returns: the new ptr to continue decoding
 */
const char *
get_encoded_typedef_or_ref (MonoImage *m, const char *ptr, char **result)
{
	guint32 token;
	
	token = mono_metadata_decode_value (ptr, &ptr);

	*result = get_typedef_or_ref (m, token);

	return ptr;
}

/**
 * get_custom_mod:
 *
 * Decodes a CustomMod (22.2.7)
 *
 * Returns: updated pointer location
 */
const char *
get_custom_mod (MonoImage *m, const char *ptr, char **return_value)
{
	char *s;
	
	if ((*ptr == MONO_TYPE_CMOD_OPT) ||
	    (*ptr == MONO_TYPE_CMOD_REQD)){
		ptr++;
		ptr = get_encoded_typedef_or_ref (m, ptr, &s);

		*return_value = g_strconcat ("CMOD ", s, NULL);
		g_free (s);
	} else
		*return_value = NULL;
	return ptr;
}


static map_t element_type_map [] = {
	{ MONO_TYPE_END        , "end" },
	{ MONO_TYPE_VOID       , "void" },
	{ MONO_TYPE_BOOLEAN    , "bool" },
	{ MONO_TYPE_CHAR       , "char" }, 
	{ MONO_TYPE_I1         , "int8" },
	{ MONO_TYPE_U1         , "unsigned int8" }, 
	{ MONO_TYPE_I2         , "int16" },
	{ MONO_TYPE_U2         , "uint16" },
	{ MONO_TYPE_I4         , "int32" },
	{ MONO_TYPE_U4         , "uint32" },
	{ MONO_TYPE_I8         , "int64" },
	{ MONO_TYPE_U8         , "uint64" },
	{ MONO_TYPE_R4         , "float32" },
	{ MONO_TYPE_R8         , "float64" },
	{ MONO_TYPE_STRING     , "string" },
	{ MONO_TYPE_TYPEDBYREF , "TypedByRef" },
	{ MONO_TYPE_I          , "native int" },
	{ MONO_TYPE_U          , "native unsigned int" },
	{ MONO_TYPE_OBJECT     , "object" },
	{ 0, NULL }
};

static map_t call_conv_type_map [] = {
	{ MONO_CALL_DEFAULT     , "default" },
	{ MONO_CALL_C           , "c" },
	{ MONO_CALL_STDCALL     , "stdcall" },
	{ MONO_CALL_THISCALL    , "thiscall" },
	{ MONO_CALL_FASTCALL    , "fastcall" },
	{ MONO_CALL_VARARG      , "vararg" },
	{ 0, NULL }
};

char*
dis_stringify_token (MonoImage *m, guint32 token)
{
	guint idx = token & 0xffffff;
	switch (token >> 24) {
	case MONO_TABLE_TYPEDEF: return get_typedef (m, idx);
	case MONO_TABLE_TYPEREF: return get_typeref (m, idx);
	case MONO_TABLE_TYPESPEC: return get_typespec (m, idx);
	default:
		 break;
	}
	return g_strdup_printf("0x%08x", token);
}

char*
dis_stringify_array (MonoImage *m, MonoArrayType *array) 
{
	char *type;
	GString *s = g_string_new("");
	int i;
	
	type = dis_stringify_type (m, array->type);
	g_string_append (s, type);
	g_free (type);
	g_string_append_c (s, '[');
	for (i = 0; i < array->rank; ++i) {
		if (i)
			g_string_append_c (s, ',');
		if (i < array->numsizes) {
			if (i < array->numlobounds && array->lobounds[i] != 0)
				g_string_sprintfa (s, "%d..%d", array->lobounds[i], array->sizes[i]);
			else
				g_string_sprintfa (s, "%d", array->sizes[i]);
		}
	}
	g_string_append_c (s, ']');
	type = s->str;
	g_string_free (s, FALSE);
	return type;
}

char*
dis_stringify_modifiers (MonoImage *m, int n, MonoCustomMod *mod)
{
	GString *s = g_string_new("");
	char *result;
	int i;
	for (i = 0; i < n; ++i) {
		char *tok = dis_stringify_token (m, mod[i].token);
		g_string_sprintfa (s, "%s %s", mod[i].required ? "opt": "reqd", tok);
		g_free (tok);
	}
	g_string_append_c (s, ' ');
	result = s->str;
	g_string_free (s, FALSE);
	return result;
}

char*
dis_stringify_param (MonoImage *m, MonoType *param) 
{
	char *t;
	char *result;
	const char *out = param->attrs & 2 ? "[out] ": "";
	t = dis_stringify_type (m, param);
	result = g_strconcat (out, t, NULL);
	g_free (t);
	return result;
}

char*
dis_stringify_method_signature (MonoImage *m, MonoMethodSignature *method, int methoddef_row)
{
	guint32 cols [MONO_METHOD_SIZE];
	guint32 pcols [MONO_PARAM_SIZE];
	guint32 param_index = 0;
	const char *name = "";
	int free_method = 0;
	char *retval;
	GString *result = g_string_new ("");
	int i;

	g_assert (method || methoddef_row);

	if (methoddef_row) {
		mono_metadata_decode_row (&m->tables [MONO_TABLE_METHOD], methoddef_row -1, cols, MONO_METHOD_SIZE);
		name = mono_metadata_string_heap (m, cols [MONO_METHOD_NAME]);
		param_index = cols [MONO_METHOD_PARAMLIST];
		if (!method) {
			const char *sig = mono_metadata_blob_heap (m, cols [MONO_METHOD_SIGNATURE]);
			mono_metadata_decode_blob_size (sig, &sig);
			method = mono_metadata_parse_method_signature (m, 1, sig, &sig);
			free_method = 1;
		}
	}
	
	retval = dis_stringify_param (m, method->ret);
	if (method->hasthis)
		g_string_append (result, "instance ");
	g_string_append (result, map (method->call_convention, call_conv_type_map));
	g_string_sprintfa (result, " %s %s(", retval, name);
	g_free (retval);
	for (i = 0; i < method->param_count; ++i) {
		if (param_index && param_index <= m->tables [MONO_TABLE_PARAM].rows) {
			mono_metadata_decode_row (&m->tables [MONO_TABLE_PARAM], param_index - 1, pcols, MONO_PARAM_SIZE);
			name = mono_metadata_string_heap (m, pcols [MONO_PARAM_NAME]);
			method->params [i]->attrs = pcols [MONO_PARAM_FLAGS];
			param_index++;
		} else {
			name = "";
		}
		if (i)
			g_string_append (result, ", ");
		retval = dis_stringify_param (m, method->params [i]);
		g_string_sprintfa (result, "%s %s", retval, name);
		g_free (retval);
	}
	g_string_append (result, ") ");

	if (free_method)
		mono_metadata_free_method_signature (method);
	retval = result->str;
	g_string_free (result, FALSE);

	return retval;
}

static char *
dis_stringify_object (MonoImage *m, MonoType *type)
{
	const char *otype = type->type == MONO_TYPE_CLASS? "class" : "valuetype";
	char *assemblyref = NULL, *result;
	MonoClass *c = type->data.klass;
	if (!c)
		return g_strdup ("Unknown");
	if (m != c->image) {
		/* we cheat */
		if (substitute_with_mscorlib_p && !strcmp ("corlib", c->image->assembly_name))
			assemblyref = g_strdup_printf ("[%s]", "mscorlib");
		else
			assemblyref = g_strdup_printf ("[%s]", c->image->assembly_name);
	}
	result = g_strdup_printf ("%s %s%s%s%s", otype, assemblyref?assemblyref:"", c->name_space, 
				*c->name_space?".":"", c->name);
	g_free (assemblyref);
	return result;
}

char*
dis_stringify_type (MonoImage *m, MonoType *type)
{
	const char *pinned = "", *byref = "";
	char *bare = NULL, *mods = NULL;
	char *result;

	if (type->num_mods)
		mods = dis_stringify_modifiers (m, type->num_mods, type->modifiers);

	switch (type->type){
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_TYPEDBYREF:
		bare = g_strdup (map (type->type, element_type_map));
		break;
		
	case MONO_TYPE_VALUETYPE:
	case MONO_TYPE_CLASS:
		bare = dis_stringify_object (m, type);
		break;
		
	case MONO_TYPE_FNPTR:
		bare = dis_stringify_method_signature (m, type->data.method, 0);
		break;
	case MONO_TYPE_PTR:
	case MONO_TYPE_SZARRAY: {
		char *child_type;
		child_type = dis_stringify_type (m, type->data.type);
		
		bare = g_strdup_printf (type->type == MONO_TYPE_PTR ? "%s*" : "%s[]", child_type);
		g_free (child_type);
		break;
	}
	case MONO_TYPE_ARRAY:
		bare = dis_stringify_array (m, type->data.array);
		break;
	case MONO_TYPE_VOID:
		bare = g_strdup ("void");
		break;
	default:
		g_error ("Do not know how to stringify type 0x%x", type->type);
	}
	
	if (type->pinned)
		pinned = " pinned";

	if (type->byref)
		byref = "&";
		
	result = g_strconcat (mods ? mods : "", bare, byref, pinned, NULL);

	g_free (bare);

	return result;
}

/**
 * get_type:
 * @m: metadata context 
 * @ptr: location to decode from.
 * @result: pointer to string where resulting decoded string is stored
 *
 * This routine returs in @result the stringified type pointed by @ptr.
 * (22.2.12)
 *
 * Returns: the new ptr to continue decoding
 */
const char *
get_type (MonoImage *m, const char *ptr, char **result)
{
	MonoType *type = mono_metadata_parse_type (m, MONO_PARSE_TYPE, 0, ptr, &ptr);
	*result = dis_stringify_type (m, type);
	mono_metadata_free_type (type);
	return ptr;
}

/**
 * 
 * Returns a stringified representation of a FieldSig (22.2.4)
 */
char *
get_field_signature (MonoImage *m, guint32 blob_signature)
{
	char *allocated_modifier_string, *allocated_type_string;
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	const char *base;
	char *res;
	int len;
	
	len = mono_metadata_decode_value (ptr, &ptr);
	base = ptr;
	/* FIELD is 0x06 */
	g_assert (*ptr == 0x06);
/*	hex_dump (ptr, 0, len); */
	ptr++; len--;
	
	ptr = get_custom_mod (m, ptr, &allocated_modifier_string);
	ptr = get_type (m, ptr, &allocated_type_string);

	res = g_strdup_printf (
		"%s %s",
		allocated_modifier_string ? allocated_modifier_string : "",
		allocated_type_string);
	
	if (allocated_modifier_string)
		g_free (allocated_modifier_string);
	if (allocated_type_string)
		g_free (allocated_modifier_string);
	
	return res;
}

MonoTypeEnum
get_field_literal_type (MonoImage *m, guint32 blob_signature)
{
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	int len;
	char *allocated_modifier_string;
	
	len = mono_metadata_decode_value (ptr, &ptr);

	/* FIELD is 0x06 */
	g_assert (*ptr == 0x06);
	ptr++; len--;
	
	ptr = get_custom_mod (m, ptr, &allocated_modifier_string);
	if (allocated_modifier_string)
		g_free (allocated_modifier_string);

	return (MonoTypeEnum) *ptr;
	
}

/**
 * decode_literal:
 * @m: metadata context
 * @token: token to decode
 *
 * decodes the literal indexed by @token.
 */
char *
decode_literal (MonoImage *m, guint32 token)
{
	return g_strdup ("LITERAL_VALUE");
}

/**
 * get_ret_type:
 * @m: metadata context 
 * @ptr: location to decode from.
 * @result: pointer to string where resulting decoded string is stored
 *
 * This routine returns in @result the stringified RetType (22.2.11)
 *
 * Returns: the new ptr to continue decoding.
 */
const char *
get_ret_type (MonoImage *m, const char *ptr, char **ret_type)
{
	GString *str = g_string_new ("");
	char *mod = NULL;
	char *allocated_type_string;
	
	ptr = get_custom_mod (m, ptr, &mod);
	if (mod){
		g_string_append (str, mod);
		g_string_append_c (str, ' ');
		g_free (mod);
	}

	if (*ptr == MONO_TYPE_TYPEDBYREF){
		/* TODO: what does `typedbyref' mean? */
		g_string_append (str, "/* FIXME: What does this mean? */ typedbyref ");
		ptr++;
	} else if (*ptr == MONO_TYPE_VOID){
		 g_string_append (str, "void");
		 ptr++;
	} else {
		if (*ptr == MONO_TYPE_BYREF){
			g_string_append (str, "[out] ");
			ptr++;
		}

		ptr = get_type (m, ptr, &allocated_type_string);
		g_string_append (str, allocated_type_string);
		g_free (allocated_type_string);
	}

	*ret_type = str->str;
	g_string_free (str, FALSE);

	return ptr;
}

/**
 * get_param:
 * @m: metadata context 
 * @ptr: location to decode from.
 * @result: pointer to string where resulting decoded string is stored
 *
 * This routine returns in @result the stringified Param (22.2.10)
 *
 * Returns: the new ptr to continue decoding.
 */
const char *
get_param (MonoImage *m, const char *ptr, char **retval)
{
	GString *str = g_string_new ("");
	char *allocated_mod_string, *allocated_type_string;
	
	ptr = get_custom_mod (m, ptr, &allocated_mod_string);
	if (allocated_mod_string){
		g_string_append (str, allocated_mod_string);
		g_string_append_c (str, ' ');
		g_free (allocated_mod_string);
	}
	
	if (*ptr == MONO_TYPE_TYPEDBYREF){
		g_string_append (str, "/*FIXME: what does typedbyref mean? */ typedbyref ");
		ptr++;
	} else {
		if (*ptr == MONO_TYPE_BYREF){
			g_string_append (str, "[out] ");
			ptr++;
		}
		ptr = get_type (m, ptr, &allocated_type_string);
		g_string_append (str, allocated_type_string);
		g_free (allocated_type_string);
	}

	*retval = str->str;
	g_string_free (str, FALSE);
	return ptr;
}

static map_t param_map [] = {
	{ PARAM_ATTRIBUTE_IN,                "[in] " },
	{ PARAM_ATTRIBUTE_OUT,               "[out] " },
	{ PARAM_ATTRIBUTE_OPTIONAL,          "optional " },
	{ PARAM_ATTRIBUTE_HAS_DEFAULT,       "hasdefault " },
	{ PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL, "fieldmarshal " },
	{ 0, NULL }
};

char *
param_flags (guint32 f)
{
	return g_strdup (flags (f, param_map));
}

static map_t field_access_map [] = {
	{ FIELD_ATTRIBUTE_COMPILER_CONTROLLED, "compilercontrolled " },
	{ FIELD_ATTRIBUTE_PRIVATE,             "private " },
	{ FIELD_ATTRIBUTE_FAM_AND_ASSEM,       "famandassem " },
	{ FIELD_ATTRIBUTE_ASSEMBLY,            "assembly " },
	{ FIELD_ATTRIBUTE_FAMILY,              "family " },
	{ FIELD_ATTRIBUTE_FAM_OR_ASSEM,        "famorassem " },
	{ FIELD_ATTRIBUTE_PUBLIC,              "public " },
	{ 0, NULL }
};

static map_t field_flags_map [] = {
	{ FIELD_ATTRIBUTE_STATIC,              "static " },
	{ FIELD_ATTRIBUTE_INIT_ONLY,           "initonly " },
	{ FIELD_ATTRIBUTE_LITERAL,             "literal " },
	{ FIELD_ATTRIBUTE_NOT_SERIALIZED,      "notserialized " },
	{ FIELD_ATTRIBUTE_SPECIAL_NAME,        "specialname " },
	{ FIELD_ATTRIBUTE_PINVOKE_IMPL,        "FIXME:pinvokeimpl " },
	{ FIELD_ATTRIBUTE_RT_SPECIAL_NAME,        "rtspecialname " },
	{ FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL,        "hasfieldmarshal " },
	{ FIELD_ATTRIBUTE_HAS_DEFAULT,        "hasdefault " },
	{ FIELD_ATTRIBUTE_HAS_FIELD_RVA,        "hasfieldrva " },
	{ 0, NULL }
};

/**
 * field_flags:
 *
 * Returns a stringified version of a Field's flags
 */
char *
field_flags (guint32 f)
{
	char buffer [1024];
	int access = f & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK;
	
	buffer [0] = 0;

	strcat (buffer, map (access, field_access_map));
	strcat (buffer, flags (f, field_flags_map));
	return g_strdup (buffer);
}

/**
 * Returns a stringifed representation of a MethodRefSig (22.2.2)
 */
char *
get_methodref_signature (MonoImage *m, guint32 blob_signature, const char *fancy_name)
{
	GString *res = g_string_new ("");
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	char *allocated_ret_type, *s;
	gboolean seen_vararg = 0;
	int param_count, signature_len;
	int i;
	
	signature_len = mono_metadata_decode_value (ptr, &ptr);

	if (*ptr & 0x20){
		if (*ptr & 0x40)
			g_string_append (res, "explicit-this ");
		else
			g_string_append (res, "instance "); /* has-this */
	}

	if (*ptr & 0x05)
		seen_vararg = 1;

	ptr++;
	param_count = mono_metadata_decode_value (ptr, &ptr);
	ptr = get_ret_type (m, ptr, &allocated_ret_type);

	g_string_append (res, allocated_ret_type);

	if (fancy_name){
		g_string_append_c (res, ' ');
		g_string_append (res, fancy_name);
	}
	
	g_string_append (res, "(");
	
	/*
	 * param_count describes parameters *before* and *after*
	 * the vararg sentinel
	 */
	for (i = 0; i < param_count; i++){
		char *param = NULL;
		
		/*
		 * If ptr is a SENTINEL
		 */
		if (*ptr == 0x41){
			g_string_append (res, " varargs ");
			continue;
		}

		ptr = get_param (m, ptr, &param);
		g_string_append (res, param);
		if (i+1 != param_count)
			g_string_append (res, ", ");
		g_free (param);
	}
	g_string_append (res, ")");
	
	/*
	 * cleanup and return
	 */
	g_free (allocated_ret_type);
	s = res->str;
	g_string_free (res, FALSE);
	return s;
}

/**
 * get_field:
 * @m: metadata context
 * @token: a FIELD_DEF token
 *
 * This routine has to locate the TypeDef that "owns" this Field.
 * Since there is no backpointer in the Field table, we have to scan
 * the TypeDef table and locate the actual "owner" of the field
 */
char *
get_field (MonoImage *m, guint32 token)
{
	int idx = mono_metadata_token_index (token);
	guint32 cols [MONO_FIELD_SIZE];
	char *sig, *res, *type;
	guint32 type_idx;

	/*
	 * We can get here also with a MenberRef token (for a field
	 * defined in another module/assembly, just like in get_method ()
	 */
	if (mono_metadata_token_code (token) == MONO_TOKEN_MEMBER_REF) {
		return g_strdup_printf ("fieldref-0x%08x", token);
	}
	g_assert (mono_metadata_token_code (token) == MONO_TOKEN_FIELD_DEF);

	mono_metadata_decode_row (&m->tables [MONO_TABLE_FIELD], idx - 1, cols, MONO_FIELD_SIZE);
	sig = get_field_signature (m, cols [MONO_FIELD_SIGNATURE]);

	/*
	 * To locate the actual "container" for this field, we have to scan
	 * the TypeDef table.  LAME!
	 */
	type_idx = mono_metadata_typedef_from_field (m, idx);

	type = get_typedef (m, type_idx);
	res = g_strdup_printf ("%s %s::%s",
			       sig, type,
			       mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]));
	g_free (type);
	g_free (sig);

	return res;
}

static char *
get_memberref_parent (MonoImage *m, guint32 mrp_token)
{
	/*
	 * mrp_index is a MemberRefParent coded index
	 */
	guint32 table = mrp_token & 7;
	guint32 idx = mrp_token >> 3;

	switch (table){
	case 0: /* TypeDef */
		return get_typedef (m, idx);
		
	case 1: /* TypeRef */
		return get_typeref (m, idx);
		
	case 2: /* ModuleRef */
		return g_strdup_printf ("TODO:MemberRefParent-ModuleRef");
		
	case 3: /* MethodDef */
		return g_strdup ("TODO:MethodDef");
		
	case 4: /* TypeSpec */
		return get_typespec (m, idx);
	}
	g_assert_not_reached ();
	return NULL;
}

/**
 * get_method:
 * @m: metadata context
 * @token: a METHOD_DEF or MEMBER_REF token
 *
 * This routine has to locate the TypeDef that "owns" this Field.
 * Since there is no backpointer in the Field table, we have to scan
 * the TypeDef table and locate the actual "owner" of the field
 */
char *
get_method (MonoImage *m, guint32 token)
{
	int idx = mono_metadata_token_index (token);
	guint32 member_cols [MONO_MEMBERREF_SIZE], method_cols [MONO_METHOD_SIZE];
	char *sig;
	const char *name;

	MonoMethod *mh;

	mh = mono_get_method (m, token, NULL);
	if (mh)
		name = g_strdup_printf ("%s.%s::%s", mh->klass->name_space, 
				mh->klass->name, mh->name); 
	else
		name = NULL;

	switch (mono_metadata_token_code (token)){
	case MONO_TOKEN_METHOD_DEF:
		mono_metadata_decode_row (&m->tables [MONO_TABLE_METHOD], 
					  idx - 1, method_cols, MONO_METHOD_SIZE);

		sig = get_methodref_signature (m, method_cols [MONO_METHOD_SIGNATURE], name);
		return sig;
		
	case MONO_TOKEN_MEMBER_REF: {
		
		mono_metadata_decode_row (&m->tables [MONO_TABLE_MEMBERREF],
					  idx - 1, member_cols, MONO_MEMBERREF_SIZE);
		if (!name)
			name = g_strdup_printf ("%s::%s",
					get_memberref_parent (m, member_cols [MONO_MEMBERREF_CLASS]),
					mono_metadata_string_heap (m, member_cols [MONO_MEMBERREF_NAME]));
		sig = get_methodref_signature (
			m, member_cols [MONO_MEMBERREF_SIGNATURE], name);
		return sig;
	}
	       
	default:
		g_assert_not_reached ();
	}
	g_assert_not_reached ();
	return NULL;
}

/**
 * get_constant:
 * @m: metadata context
 * @blob_index: index into the blob where the constant is stored
 *
 * Returns: An allocated value representing a stringified version of the
 * constant.
 */
char *
get_constant (MonoImage *m, MonoTypeEnum t, guint32 blob_index)
{
	const char *ptr = mono_metadata_blob_heap (m, blob_index);
	int len;
	
	len = mono_metadata_decode_value (ptr, &ptr);
	
	switch (t){
	case MONO_TYPE_BOOLEAN:
		return g_strdup_printf ("%s", *ptr ? "true" : "false");
		
	case MONO_TYPE_CHAR:
		return g_strdup_printf ("%c", *ptr); /* FIXME: unicode char */
		
	case MONO_TYPE_U1:
	case MONO_TYPE_I1:
		return g_strdup_printf ("int8(0x%02x)", (int) (*ptr));
		break;
		
	case MONO_TYPE_U2:
	case MONO_TYPE_I2:
		return g_strdup_printf ("int16(0x%08x)", (int) read16 (ptr));
		
	case MONO_TYPE_U4:
	case MONO_TYPE_I4:
		return g_strdup_printf ("int32(%d)", read32 (ptr));
		
	case MONO_TYPE_I8: {
		guint32 low, high;
		low = read32 (ptr);
		high = read32 (ptr + 4);
		return g_strdup_printf ("0x%08x%08x", high, low);
	}
	case MONO_TYPE_U8: {
		guint32 low, high;
		low = read32 (ptr);
		high = read32 (ptr + 4);
		return g_strdup_printf ("0x%08x%08x", high, low);
	}
	case MONO_TYPE_R4: {
		float r;
		readr4 (ptr, &r);
		return g_strdup_printf ("%g", (double) r);
	}
	case MONO_TYPE_R8: {
		double r;
		readr8 (ptr, &r);
		return g_strdup_printf ("%g", r);
	}
	case MONO_TYPE_STRING: {
		int i, j, e;
		char *res;
		e = len = 0;
		for (i = 0; !ptr [i+1]; i += 2){
			len++;
			switch (ptr [i]) {
			case '"':
			case '\\':
			case '\n': /* add more */
				e++;
			}
		}
		res = g_malloc (len + e + 3);
		j = 1;
		res [0] = '"';

		for (i = 0; i < len; i += 2){
			switch(ptr[i]) {
			case '"': 
				res[j++] = '\\';
				res[j++] = '"';
			case '\\': 
				res[j++] = '\\';
				res[j++] = '\\';
			case '\n':
				res[j++] = '\\';
				res[j++] = 'n';
				break;
			default:
				res[j++] = isprint (ptr [i]) ? ptr [i] : '.';
				break;
			}
		}
		res[j++] = '"';
		res[j] = 0;
		return res;
	}
		
	case MONO_TYPE_CLASS:
		return g_strdup ("CLASS CONSTANT.  MUST BE ZERO");
		
	default:
		g_error ("Unknown MONO_TYPE (%d) on constant at Blob index (0x%08x)\n",
			 (int) *ptr, blob_index);
		return g_strdup_printf ("Unknown");
	}

}

/**
 * get_token:
 * @m: metadata context
 * @token: token that we want to decode.
 *
 * Returns: An allocated value representing a stringified version of the
 * constant.
 */
char *
get_token (MonoImage *m, guint32 token)
{
	guint32 idx = mono_metadata_token_index (token);

	switch (mono_metadata_token_code (token)){
	case MONO_TOKEN_FIELD_DEF:
		return (get_field (m, token));
	case MONO_TOKEN_TYPE_DEF:
		return get_typedef (m, idx);
	case MONO_TOKEN_TYPE_REF:
		return get_typeref (m, idx);
	case MONO_TOKEN_TYPE_SPEC:
		return get_typespec (m, idx);
	default:		
		g_error ("Do not know how to decode tokens of type 0x%08x", token);
	}

	g_assert_not_reached ();
	return g_strdup ("ERROR");
}

/**
 * get_token_type:
 * @m: metadata context
 * @token: the token can belong to any of the following tables:
 * MONO_TOKEN_TYPE_REF, MONO_TOKEN_TYPE_DEF, MONO_TOKEN_TYPE_SPEC
 *
 * Returns: a stringified version of the MethodDef or MethodRef or TypeSpecn
 * at (token & 0xffffff) 
 */
char *
get_token_type (MonoImage *m, guint32 token)
{
	char *temp = NULL, *s;
	int idx;

	idx = mono_metadata_token_index (token);
	
	switch (mono_metadata_token_code (token)){
	case MONO_TOKEN_TYPE_DEF:
		temp = get_typedef (m, idx);
		s = g_strdup_printf ("%s", temp);
		break;
		
	case MONO_TOKEN_TYPE_REF: 
		temp = get_typeref (m, idx);
		s = g_strdup_printf ("%s", temp);
		break;
		
	case MONO_TOKEN_TYPE_SPEC:
		s = get_typespec (m, idx);
		break;

	default:
		g_error ("Unhandled encoding for typedef-or-ref coded index");

	}
	
	if (temp)
		g_free (temp);

	return s;
}
