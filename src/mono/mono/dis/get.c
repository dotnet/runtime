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
#include <string.h>
#include <glib.h>
#include "meta.h"
#include "util.h"
#include "get.h"

/**
 * expand:
 * @t: table to extract information from.
 * @idx: index in table.
 * @res: array of @res_size cols to store the results in
 *
 * This decompresses the metadata element @idx in table @t
 * into the guint32 @res array that has res_size elements
 */
void
expand (metadata_tableinfo_t *t, int idx, guint32 *res, int res_size)

{
	guint32 bitfield = t->size_bitfield;
	int i, count = meta_table_count (bitfield);
	char *data = t->base + idx * t->row_size;
	
	g_assert (res_size == count);
	
	for (i = 0; i < count; i++){
		int n = meta_table_size (bitfield, i);

		switch (n){
		case 1:
			res [i] = *data; break;
		case 2:
			res [i] = read16 (data); break;
			
		case 4:
			res [i] = read32 (data); break;
			
		default:
			g_assert_not_reached ();
		}
		data += n;
	}
}

/**
 * get_encoded_value:
 * @ptr: pointer to decode from
 * @len: result value is stored here.
 *
 * This routine decompresses 32-bit values as specified in the "Blob and
 * Signature" section (22.2)
 *
 * Returns: updated pointer location
 */
const char *
get_encoded_value (const char *_ptr, guint32 *len)
{
	const unsigned char *ptr = (unsigned char *) _ptr;
	unsigned char b = *ptr;
	
	if ((b & 0x80) == 0){
		*len = b;
		return ptr+1;
	} else if ((b & 0x40) == 0){
		*len = ((b & 0x3f) << 8 | ptr [1]);
		return ptr + 2;
	}
	*len = ((b & 0x1f) << 24) |
		(ptr [1] << 16) |
		(ptr [2] << 8) |
		ptr [3];
	
	return ptr + 4;
}

char *
get_typedef (metadata_t *m, int idx)
{
	guint32 cols [6];

	expand (&m->tables [META_TABLE_TYPEDEF], idx - 1, cols, CSIZE (cols));

	return g_strdup_printf (
		"%s.%s",
		mono_metadata_string_heap (m, cols [2]),
		mono_metadata_string_heap (m, cols [1]));
}

char *
get_module (metadata_t *m, int idx)
{
	guint32 cols [5];
	
	/*
	 * There MUST BE only one module in the Module table
	 */
	g_assert (idx == 1);
	    
	expand (&m->tables [META_TABLE_MODULEREF], idx - 1, cols, CSIZE (cols));

	return g_strdup (mono_metadata_string_heap (m, cols [6]));
}

char *
get_assemblyref (metadata_t *m, int idx)
{
	guint32 cols [9];
	
	expand (&m->tables [META_TABLE_ASSEMBLYREF], idx - 1, cols, CSIZE (cols));

	return g_strdup (mono_metadata_string_heap (m, cols [6]));
}

char *
get_typeref (metadata_t *m, int idx)
{
	guint32 cols [3];
	const char *s, *t;
	char *x, *ret;
	guint32 rs_idx, table;
	
	expand (&m->tables [META_TABLE_TYPEREF], idx - 1, cols, CSIZE (cols));

	t = mono_metadata_string_heap (m, cols [1]);
	s = mono_metadata_string_heap (m, cols [2]);

	rs_idx = cols [0] >> 2;
	/*
	 * Two bits in Beta2.
	 * ECMA spec claims 3 bits
	 */
	table = cols [0] & 3;
	
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
		ret = g_strdup_printf ("[%s] %s.%s", x, s, t);
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
get_typedef_or_ref (metadata_t *m, guint32 dor_token)
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
		s = g_strdup_printf ("TODO-TypeSpec: 0x%08x", idx);
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
get_encoded_typedef_or_ref (metadata_t *m, const char *ptr, char **result)
{
	guint32 token;
	
	ptr = get_encoded_value (ptr, &token);

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
get_custom_mod (metadata_t *m, const char *ptr, char **return_value)
{
	char *s;
	
	if ((*ptr == ELEMENT_TYPE_CMOD_OPT) ||
	    (*ptr == ELEMENT_TYPE_CMOD_REQD)){
		ptr++;
		ptr = get_encoded_typedef_or_ref (m, ptr, &s);

		*return_value = g_strconcat ("CMOD ", s, NULL);
		g_free (s);
	} else
		*return_value = NULL;
	return ptr;
}


/**
 * methoddefref_signature:
 * @m: metadata context 
 * @ptr: location to decode from.
 * @result: pointer to string where resulting decoded string is stored
 *
 * This routine decodes into a string a MethodDef or a MethodRef.
 *
 * result will point to a g_malloc()ed string.
 *
 * Returns: the new ptr to continue decoding
 */
static const char *
methoddefref_signature (metadata_t *m, const char *ptr, char **result)
{
	*result = g_strdup ("method-def-or-ref");
	
	return ptr;
}

static map_t element_type_map [] = {
	{ ELEMENT_TYPE_END        , "end" },
	{ ELEMENT_TYPE_VOID       , "void" },
	{ ELEMENT_TYPE_BOOLEAN    , "bool" },
	{ ELEMENT_TYPE_CHAR       , "char" }, 
	{ ELEMENT_TYPE_I1         , "sbyte" },
	{ ELEMENT_TYPE_U1         , "byte" }, 
	{ ELEMENT_TYPE_I2         , "int16" },
	{ ELEMENT_TYPE_U2         , "uint16" },
	{ ELEMENT_TYPE_I4         , "int32" },
	{ ELEMENT_TYPE_U4         , "uint32" },
	{ ELEMENT_TYPE_I8         , "int64" },
	{ ELEMENT_TYPE_U8         , "uint64" },
	{ ELEMENT_TYPE_R4         , "float32" },
	{ ELEMENT_TYPE_R8         , "float64" },
	{ ELEMENT_TYPE_STRING     , "string" },
	{ ELEMENT_TYPE_TYPEDBYREF , "TypedByRef" },
	{ ELEMENT_TYPE_I          , "native int" },
	{ ELEMENT_TYPE_U          , "native unsigned int" },
	{ ELEMENT_TYPE_OBJECT     , "object" },
	{ 0, NULL }
};

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
get_type (metadata_t *m, const char *ptr, char **result)
{
	char c;
	
	c = *ptr++;
	
	switch (c){
	case ELEMENT_TYPE_BOOLEAN:
	case ELEMENT_TYPE_CHAR:
	case ELEMENT_TYPE_I1:
	case ELEMENT_TYPE_U1:
	case ELEMENT_TYPE_I2:
	case ELEMENT_TYPE_U2:
	case ELEMENT_TYPE_I4:
	case ELEMENT_TYPE_U4:
	case ELEMENT_TYPE_I8:
	case ELEMENT_TYPE_U8:
	case ELEMENT_TYPE_R4:
	case ELEMENT_TYPE_R8:
	case ELEMENT_TYPE_I:
	case ELEMENT_TYPE_STRING:
	case ELEMENT_TYPE_OBJECT:
		*result = g_strdup (map (c, element_type_map));
		break;
		
	case ELEMENT_TYPE_VALUETYPE:
	case ELEMENT_TYPE_CLASS:
		ptr = get_encoded_typedef_or_ref (m, ptr, result);
		break;
		
	case ELEMENT_TYPE_FNPTR:
		ptr = methoddefref_signature (m, ptr, result);
		break;
		
	case ELEMENT_TYPE_SZARRAY: {
		char *child_type;
		
		ptr = get_type (m, ptr, &child_type);
		*result = g_strdup_printf ("%s[]", child_type);
		g_free (child_type);
		break;
	}
		
	case ELEMENT_TYPE_ARRAY:
 
		*result = g_strdup ("ARRAY:TODO");
	}
	
	return ptr;
}

/**
 * 
 * Returns a stringified representation of a FieldSig (22.2.4)
 */
char *
get_field_signature (metadata_t *m, guint32 blob_signature)
{
	char *allocated_modifier_string, *allocated_type_string;
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	const char *base;
	char *res;
	int len;
	
	ptr = get_encoded_value (ptr, &len);
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

ElementTypeEnum
get_field_literal_type (metadata_t *m, guint32 blob_signature)
{
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	int len;
	char *allocated_modifier_string;
	
	ptr = get_encoded_value (ptr, &len);

	/* FIELD is 0x06 */
	g_assert (*ptr == 0x06);
	ptr++; len--;
	
	ptr = get_custom_mod (m, ptr, &allocated_modifier_string);
	if (allocated_modifier_string)
		g_free (allocated_modifier_string);

	return (ElementTypeEnum) *ptr;
	
}

/**
 * decode_literal:
 * @m: metadata context
 * @token: token to decode
 *
 * decodes the literal indexed by @token.
 */
char *
decode_literal (metadata_t *m, guint32 token)
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
get_ret_type (metadata_t *m, const char *ptr, char **ret_type)
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

	if (*ptr == ELEMENT_TYPE_TYPEDBYREF){
		/* TODO: what does `typedbyref' mean? */
		g_string_append (str, "/* FIXME: What does this mean? */ typedbyref ");
		ptr++;
	} else if (*ptr == ELEMENT_TYPE_VOID){
		 g_string_append (str, "void");
		 ptr++;
	} else {
		if (*ptr == ELEMENT_TYPE_BYREF){
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
get_param (metadata_t *m, const char *ptr, char **retval)
{
	GString *str = g_string_new ("");
	char *allocated_mod_string, *allocated_type_string;
	
	ptr = get_custom_mod (m, ptr, &allocated_mod_string);
	if (allocated_mod_string){
		g_string_append (str, allocated_mod_string);
		g_string_append_c (str, ' ');
		g_free (allocated_mod_string);
	}
	
	if (*ptr == ELEMENT_TYPE_TYPEDBYREF){
		g_string_append (str, "/*FIXME: what does typedbyref mean? */ typedbyref ");
		ptr++;
	} else {
		if (*ptr == ELEMENT_TYPE_BYREF){
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
	static char buffer [1024];
	int access = f & FIELD_ATTRIBUTE_FIELD_ACCESS_MASK;
	
	buffer [0] = 0;

	strcat (buffer, map (access, field_access_map));
	strcat (buffer, flags (f, field_flags_map));
	return g_strdup (buffer);
}

/**
 * get_blob_encoded_size:
 * @ptr: pointer to a blob object
 * @size: where we return the size of the object
 *
 * This decodes a compressed size as described by 23.1.4
 *
 * Returns: the position to start decoding a blob or user string object
 * from. 
 */
const char *
get_blob_encoded_size (const char *xptr, int *size)
{
	const unsigned char *ptr = xptr;
	
	if ((*ptr & 0x80) == 0){
		*size = ptr [0] & 0x7f;
		ptr++;
	} else if ((*ptr & 0x40) == 0){
		*size = ((ptr [0] & 0x3f) << 8) + ptr [1];
		ptr += 2;
	} else {
		*size = ((ptr [0] & 0x1f) << 24) +
			(ptr [1] << 16) +
			(ptr [2] << 8) +
			ptr [3];
		ptr += 4;
	}

	return (char *) ptr;
}

/**
 * Returns a stringifed representation of a MethodRefSig (22.2.2)
 */
char *
get_methodref_signature (metadata_t *m, guint32 blob_signature)
{
	GString *res = g_string_new ("");
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	char *allocated_ret_type, *s;
	gboolean seen_vararg = 0;
	int param_count, signature_len;
	int i;
	
	ptr = get_encoded_value (ptr, &signature_len);

	if (*ptr & 0x20){
		if (*ptr & 0x40)
			g_string_append (res, "explicit-this ");
		else
			g_string_append (res, "has-this ");
	}

	if (*ptr & 0x05)
		seen_vararg = 1;

	ptr++;
	ptr = get_encoded_value (ptr, &param_count);
	ptr = get_ret_type (m, ptr, &allocated_ret_type);

	g_string_append (res, allocated_ret_type);
	g_string_append (res, " (");
	
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
 * get_constant:
 * @m: metadata context
 * @blob_index: index into the blob where the constant is stored
 *
 * Returns: An allocated value representing a stringified version of the
 * constant.
 */
char *
get_constant (metadata_t *m, ElementTypeEnum t, guint32 blob_index)
{
	const char *ptr = mono_metadata_blob_heap (m, blob_index);
	int len;
	
	ptr = get_encoded_value (ptr, &len);
	
	switch (t){
	case ELEMENT_TYPE_BOOLEAN:
		return g_strdup_printf ("%s", *ptr ? "true" : "false");
		
	case ELEMENT_TYPE_CHAR:
		return g_strdup_printf ("%c", *ptr);
		
	case ELEMENT_TYPE_U1:
		return g_strdup_printf ("0x%02x", (int) (*ptr));
		break;
		
	case ELEMENT_TYPE_I2:
		return g_strdup_printf ("%d", (int) (*(gint16 *) ptr));
		
	case ELEMENT_TYPE_I4:
		return g_strdup_printf ("%d", *(gint32 *) ptr);
		
	case ELEMENT_TYPE_I8:
		/*
		 * FIXME: This is not endian portable, does only 
		 * matter for debugging, but still.
		 */
		return g_strdup_printf ("0x%08x%08x", *(guint32 *) ptr, *(guint32 *) (ptr + 4));
		
	case ELEMENT_TYPE_U8:
		return g_strdup_printf ("0x%08x%08x", *(guint32 *) ptr, *(guint32 *) (ptr + 4));		
	case ELEMENT_TYPE_R4:
		return g_strdup_printf ("%g", (double) (* (float *) ptr));
		
	case ELEMENT_TYPE_R8:
		return g_strdup_printf ("%g", * (double *) ptr);
		
	case ELEMENT_TYPE_STRING:
		return "FIXME: Decode string constants!";
		
	case ELEMENT_TYPE_CLASS:
		return g_strdup ("CLASS CONSTANT.  MUST BE ZERO");
		
		/*
		 * These are non CLS compliant:
		 */
	case ELEMENT_TYPE_I1:
		return g_strdup_printf ("%d", (int) *ptr);

	case ELEMENT_TYPE_U2:
		return g_strdup_printf ("0x%04x", (unsigned int) (*(guint16 *) ptr));
		
	case ELEMENT_TYPE_U4:
		return g_strdup_printf ("0x%04x", (unsigned int) (*(guint32 *) ptr));
		
	default:
		g_error ("Unknown ELEMENT_TYPE (%d) on constant at Blob index (0x%08x)\n",
			 (int) *ptr, blob_index);
		return g_strdup_printf ("Unknown");
	}

}
