/*
 * dis.c: Sample disassembler
 *
 * Author:
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 *
 * TODO:
 *   Investigate how interface inheritance works and how it should be dumped.
 *   Structs are not being labeled as `valuetype' classes
 *   Support CustomMods.
 *
 */
#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/endian.h>
#include <mono/metadata/typeattr.h>
#include <mono/metadata/fieldattr.h>
#include <mono/metadata/methodattr.h>
#include <mono/metadata/eltype.h>
#include <mono/metadata/blobsig.h>
#include <mono/metadata/paramattr.h>
#include "util.h"

FILE *output;

/* True if you want to get a dump of the header data */
gboolean dump_header_data_p = FALSE;

int dump_table = -1;

static void
dump_header_data (MonoAssembly *ass)
{
	if (!dump_header_data_p)
		return;

	fprintf (output,
		 "// Ximian's CIL disassembler, version 1.0\n"
		 "// Copyright (C) 2001 Ximian, Inc.\n\n");
}

#define CSIZE(x) (sizeof (x) / 4)
static void
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

static void
dis_directive_assembly (metadata_t *m)
{
	metadata_tableinfo_t *t  = &m->tables [META_TABLE_ASSEMBLY];
	guint32 cols [9];
	
	if (t->base == NULL)
		return;

	expand (t, 0, cols, CSIZE (cols));
	
	fprintf (output,
		 ".assembly %s\n"
		 "{\n"
		 "  .hash algorithm 0x%08x\n"
		 "  .ver  %d.%d.%d.%d"
		 "%s %s"
		 "%s"
		 "\n"
		 "}\n",
		 mono_metadata_string_heap (m, cols [7]),
		 cols [0],
		 cols [1], cols [2], cols [3], cols [4],
		 cols [8] ? "\n  .locale" : "",
		 cols [8] ? mono_metadata_string_heap (m, cols [8]) : "",
		 cols [6] ? "\n  .publickey" : ""
		);
}

static void
dis_directive_assemblyref (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_ASSEMBLYREF];
	guint32 cols [9];
	int i;
	
	if (t->base == NULL)
		return;

	for (i = 0; i < t->rows; i++){
		expand (t, i, cols, CSIZE (cols));

		fprintf (output,
			 ".assembly extern %s\n"
			 "{\n"
			 "  .ver %d.%d.%d.%d\n"
			 "}\n",
			 mono_metadata_string_heap (m, cols [6]),
			 cols [0], cols [1], cols [2], cols [3]
			);
	}
}

static map_t visibility_map [] = {
	{ TYPE_ATTRIBUTE_NOT_PUBLIC,           "private " },
	{ TYPE_ATTRIBUTE_PUBLIC,               "public " },
	{ TYPE_ATTRIBUTE_NESTED_PUBLIC,        "nested-public " },
	{ TYPE_ATTRIBUTE_NESTED_PRIVATE,       "nested-private " },
	{ TYPE_ATTRIBUTE_NESTED_FAMILY,        "family " },
	{ TYPE_ATTRIBUTE_NESTED_ASSEMBLY,      "nested-assembly" },
	{ TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM, "nested-fam-and-assembly" },
	{ TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM,  "nested-fam-or-assembly" },
	{ 0, NULL }
};

static map_t layout_map [] = {
	{ TYPE_ATTRIBUTE_AUTO_LAYOUT,          "auto " },
	{ TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT,    "sequential " },
	{ TYPE_ATTRIBUTE_EXPLICIT_LAYOUT,      "explicit " },
	{ 0, NULL }
};

static map_t format_map [] = {
	{ TYPE_ATTRIBUTE_ANSI_CLASS,           "ansi " },
	{ TYPE_ATTRIBUTE_UNICODE_CLASS,	       "unicode " },
	{ TYPE_ATTRIBUTE_AUTO_CLASS,           "auto " },
	{ 0, NULL }
};

static char *
typedef_flags (guint32 flags)
{
	static char buffer [1024];
	int visibility = flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;
	int layout = flags & TYPE_ATTRIBUTE_LAYOUT_MASK;
	int format = flags & TYPE_ATTRIBUTE_STRING_FORMAT_MASK;
	
	buffer [0] = 0;

	strcat (buffer, map (visibility, visibility_map));
	strcat (buffer, map (layout, layout_map));
	strcat (buffer, map (format, format_map));
	
	if (flags & TYPE_ATTRIBUTE_ABSTRACT)
		strcat (buffer, "abstract ");
	if (flags & TYPE_ATTRIBUTE_SEALED)
		strcat (buffer, "sealed ");
	if (flags & TYPE_ATTRIBUTE_SPECIAL_NAME)
		strcat (buffer, "special-name ");
	if (flags & TYPE_ATTRIBUTE_IMPORT)
		strcat (buffer, "import ");
	if (flags & TYPE_ATTRIBUTE_SERIALIZABLE)
		strcat (buffer, "serializable ");
	if (flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT)
		strcat (buffer, "beforefieldinit ");

	return buffer;
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
 * field_flags:
 *
 * Returns a stringified version of a Field's flags
 */
static char *
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
 * get_encoded_value:
 * @ptr: pointer to decode from
 * @len: result value is stored here.
 *
 * This routine decompresses 32-bit values as specified in the "Blob and
 * Signature" section (22.2)
 *
 * Returns: updated pointer location
 */
static const char *
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

static char *
get_typedef (metadata_t *m, int idx)
{
	guint32 cols [6];

	expand (&m->tables [META_TABLE_TYPEDEF], idx - 1, cols, CSIZE (cols));

	return g_strdup_printf (
		"%s.%s",
		mono_metadata_string_heap (m, cols [2]),
		mono_metadata_string_heap (m, cols [1]));
}

static char *
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

static char *
get_assemblyref (metadata_t *m, int idx)
{
	guint32 cols [9];
	
	expand (&m->tables [META_TABLE_ASSEMBLYREF], idx - 1, cols, CSIZE (cols));

	return g_strdup (mono_metadata_string_heap (m, cols [6]));
}

static char *
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

static char *
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
static const char *
get_encoded_typedef_or_ref (metadata_t *m, const char *ptr, char **result)
{
	guint32 token;
	
	ptr = get_encoded_value (ptr, &token);

	*result = get_typedef_or_ref (m, token);

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

/**
 * get_custom_mod:
 *
 * Decodes a CustomMod (22.2.7)
 *
 * Returns: updated pointer location
 */
static const char *
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
static const char *
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
static char *
field_signature (metadata_t *m, guint32 blob_signature)
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

/**
 * decode_literal:
 * @m: metadata context
 * @token: token to decode
 *
 * decodes the literal indexed by @token.
 */
static char *
decode_literal (metadata_t *m, guint32 token)
{
	return g_strdup ("LITERAL_VALUE");
}

/**
 * dis_field_list:
 * @m: metadata context
 * @start: starting index into the Field Table.
 * @end: ending index into Field table.
 *
 * This routine displays all the decoded fields from @start to @end
 */
static void
dis_field_list (metadata_t *m, guint32 start, guint32 end)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_FIELD];
	guint32 cols [3];
	int i;

	if (end > t->rows + 1){
		fprintf (output, "ERROR index out of range in fields");
		exit (1);
	}
			
	for (i = start; i < end; i++){
		char *sig, *flags;
		
		expand (t, i, cols, CSIZE (cols));
		sig = field_signature (m, cols [2]);
		flags = field_flags (cols [0]);
		
		if (cols [0] & FIELD_ATTRIBUTE_LITERAL){
			char *lit = decode_literal (m, cols [2]);
			
			fprintf (output, "    .field %s %s %s = ",
				 flags, sig,
				 mono_metadata_string_heap (m, cols [1]));
			fprintf (output, "%s\n", lit);
			g_free (lit);
		} else 
			fprintf (output, "    .field %s %s %s\n",
				 flags, sig,
				 mono_metadata_string_heap (m, cols [1]));
		g_free (flags);
		g_free (sig);
	}
}

static map_t method_access_map [] = {
	{ METHOD_ATTRIBUTE_COMPILER_CONTROLLED, "compilercontrolled " },
	{ METHOD_ATTRIBUTE_PRIVATE,             "private" },
	{ METHOD_ATTRIBUTE_FAM_AND_ASSEM,       "famandassem" },
	{ METHOD_ATTRIBUTE_ASSEM,               "assembly " },
	{ METHOD_ATTRIBUTE_FAMILY,              "family " },
	{ METHOD_ATTRIBUTE_FAM_OR_ASSEM,        "famorassem " },
	{ METHOD_ATTRIBUTE_PUBLIC,              "public " },
	{ 0, NULL }
};

static map_t method_flags_map [] = {
	{ METHOD_ATTRIBUTE_STATIC,              "static " },
	{ METHOD_ATTRIBUTE_FINAL,               "final " },
	{ METHOD_ATTRIBUTE_VIRTUAL,             "virtual " },
	{ METHOD_ATTRIBUTE_HIDE_BY_SIG,         "hidebysig " },
	{ METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK,  "newslot " },
	{ METHOD_ATTRIBUTE_ABSTRACT,            "abstract " },
	{ METHOD_ATTRIBUTE_SPECIAL_NAME,        "specialname " },
	{ METHOD_ATTRIBUTE_RT_SPECIAL_NAME,     "rtspecialname " },
	{ METHOD_ATTRIBUTE_PINVOKE_IMPL,        "pinvokeimpl " }, 
	{ METHOD_ATTRIBUTE_UNMANAGED_EXPORT,    "export " },
	{ METHOD_ATTRIBUTE_HAS_SECURITY,        "hassecurity" },
	{ METHOD_ATTRIBUTE_REQUIRE_SEC_OBJECT,  "requiresecobj" },
	{ 0, NULL }
};

/**
 * method_flags:
 *
 * Returns a stringified version of the Method's flags
 */
static char *
method_flags (guint32 f)
{
	GString *str = g_string_new ("");
	int access = f & METHOD_ATTRIBUTE_MEMBER_ACCESS_MASK;
	char *s;
	
	g_string_append (str, map (access, method_access_map));
	g_string_append (str, flags (f, method_flags_map));

	s = str->str;
	g_string_free (str, FALSE);

	return s;
}

static map_t method_impl_map [] = {
	{ METHOD_IMPL_ATTRIBUTE_IL,              "cil " },
	{ METHOD_IMPL_ATTRIBUTE_NATIVE,          "native " },
	{ METHOD_IMPL_ATTRIBUTE_OPTIL,           "optil " },
	{ METHOD_IMPL_ATTRIBUTE_RUNTIME,         "runtime " },
	{ 0, NULL }
};

static map_t managed_type_map [] = {
	{ METHOD_IMPL_ATTRIBUTE_UNMANAGED,       "unmanaged " },
	{ METHOD_IMPL_ATTRIBUTE_MANAGED,         "managed " },
	{ 0, NULL }
};

static map_t managed_impl_flags [] = {
	{ METHOD_IMPL_ATTRIBUTE_FORWARD_REF,     "fwdref " },
	{ METHOD_IMPL_ATTRIBUTE_PRESERVE_SIG,    "preservesig " },
	{ METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL,   "internalcall " },
	{ METHOD_IMPL_ATTRIBUTE_SYNCHRONIZED,    "synchronized " },
	{ METHOD_IMPL_ATTRIBUTE_NOINLINING,      "noinline " },
	{ 0, NULL }
};

static char *
method_impl_flags (guint32 f)
{
	GString *str = g_string_new ("");
	char *s;
	int code_type = f & METHOD_IMPL_ATTRIBUTE_CODE_TYPE_MASK;
	int managed_type = f & METHOD_IMPL_ATTRIBUTE_MANAGED_MASK;

	g_string_append (str, map (code_type, method_impl_map));
	g_string_append (str, map (managed_type, managed_type_map));
	g_string_append (str, flags (f, managed_impl_flags));
	
	s = str->str;
	g_string_free (str, FALSE);
	return s;
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
static const char *
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

static const char *
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

typedef struct {
	char  flags;
	char *ret_type;
	int   param_count;
	char **param;
} MethodSignature;

static MethodSignature *
parse_method_signature (metadata_t *m, guint32 blob_signature)
{
	GString *res = g_string_new ("");
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	MethodSignature *ms = g_new0 (MethodSignature, 1);
	char *s;
	int i, len;

	ptr = get_encoded_value (ptr, &len);
	fprintf (output, "     // SIG: ");
	hex_dump (ptr, 0, -len);
	fprintf (output, "\n");
	
	ms->flags = *ptr++;

	ptr = get_encoded_value (ptr, &ms->param_count);
	ptr = get_ret_type (m, ptr, &ms->ret_type);
	ms->param = g_new (char *, ms->param_count);
	
	for (i = 0; i < ms->param_count; i++)
		ptr = get_param (m, ptr, &(ms->param [i]));

	s = res->str;
	g_string_free (res, FALSE);
	return ms;
}

static void
free_method_signature (MethodSignature *ms)
{
	int i;
	
	for (i = 0; i < ms->param_count; i++)
		g_free (ms->param [i]);
	g_free (ms->param);
	g_free (ms->ret_type);
	g_free (ms);
}

static map_t param_map [] = {
	{ PARAM_ATTRIBUTE_IN,                "[in] " },
	{ PARAM_ATTRIBUTE_OUT,               "[out] " },
	{ PARAM_ATTRIBUTE_OPTIONAL,          "optional " },
	{ PARAM_ATTRIBUTE_HAS_DEFAULT,       "hasdefault " },
	{ PARAM_ATTRIBUTE_HAS_FIELD_MARSHAL, "fieldmarshal " },
	{ 0, NULL }
};

static char *
param_flags (guint32 f)
{
	return g_strdup (map (f, param_map));
}

/**
 * dis_field_list:
 * @m: metadata context
 * @start: starting index into the Method Table.
 * @end: ending index into Method table.
 *
 * This routine displays the methods in the Method Table from @start to @end
 */
static void
dis_method_list (metadata_t *m, guint32 start, guint32 end)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_METHOD];
	metadata_tableinfo_t *p = &m->tables [META_TABLE_PARAM];
	guint32 cols [6];
	guint32 cols_next [6];
	guint32 param_cols [3];
	int i;

	if (end > t->rows + 1){
		fprintf (output, "ERROR index out of range in methods");
		exit (1);
	}

	for (i = start; i < end; i++){
		MethodSignature *ms;
		char *flags, *impl_flags;
		
		expand (t, i, cols, CSIZE (cols));
		expand (t, i + 1, cols_next, CSIZE (cols_next));

		flags = method_flags (cols [2]);
		impl_flags = method_impl_flags (cols [1]);

		ms = parse_method_signature (m, cols [4]);
			
		fprintf (output,
			 "    .method %s\n",
			 flags);
		fprintf (output,
			 "           %s %s",
			 ms->ret_type,
			 mono_metadata_string_heap (m, cols [3]));
		if (ms->param_count > 0){
			int i;

			fprintf (output, "(\n");
			for (i = 0; i < ms->param_count; i++){
				char *pf;
				
				expand (p, i, param_cols, CSIZE (param_cols));
				pf = param_flags (param_cols [0]);
				fprintf (
					output, "\t\t%s %s %s%s", pf, ms->param [i],
					mono_metadata_string_heap (m, param_cols [2]),
					(i+1 == ms->param_count) ? ")" : ",\n");

				g_free (pf);
			}
				
		}
		fprintf (output, " %s\n", impl_flags);
		g_free (flags);
		g_free (impl_flags);
		
		fprintf (output, "    {\n");
		fprintf (output, "        // Method begins at RVA 0x%x\n", cols [0]);
		fprintf (output, "        // Param: %d %d (%d)\n", cols [5], cols_next [5], ms->param_count);
		fprintf (output, "    }\n\n");
		free_method_signature (ms);
	}
}

/**
 * dis_type:
 * @m: metadata context
 * @n: index of type to disassemble
 *
 * Disassembles the type whose index in the TypeDef table is @n.
 */
static void
dis_type (metadata_t *m, int n)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEDEF];
	guint32 cols [6];
	guint32 cols_next [6];
	const char *name;
	
	expand (t, n, cols, CSIZE (cols));
	expand (t, n + 1, cols_next, CSIZE (cols_next));

	fprintf (output, ".namespace %s\n{\n", mono_metadata_string_heap (m, cols [2]));
	name = mono_metadata_string_heap (m, cols [1]);

	if ((cols [0] & TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK) == TYPE_ATTRIBUTE_CLASS){
		char *base = get_typedef_or_ref (m, cols [3]);
		fprintf (output, "  .class %s%s\n", typedef_flags (cols [0]), name);
		fprintf (output, "  \textends %s\n", base);
		g_free (base);
	} else
		fprintf (output, "  .class interface %s%s\n", typedef_flags (cols [0]), name);
	
	fprintf (output, "  {\n");

	/*
	 * The value in the table is always valid, we know we have fields
	 * if the value stored is different than the next record.
	 */
	if (cols [4] != cols_next [4])
		dis_field_list (m, cols [4] - 1, cols_next [4] - 1);
	fprintf (output, "\n");
	if (cols [4] != cols_next [5])
		dis_method_list (m, cols [5] - 1, cols_next [5] - 1);

	fprintf (output, "  }\n}\n\n");
}

static void
dump_table_typeref (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEREF];
	int i;

	fprintf (output, "Typeref Table\n");
	
	for (i = 1; i <= t->rows; i++){
		char *s = get_typeref (m, i);
		
		fprintf (output, "%d: %s\n", i, s);
		g_free (s);
	}
	fprintf (output, "\n");
}

static void
dump_table_typedef (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEDEF];
	int i;

	fprintf (output, "Typedef Table\n");
	
	for (i = 1; i <= t->rows; i++){
		char *s = get_typedef (m, i);
		
		fprintf (output, "%d: %s\n", i, s);
		g_free (s);
	}
	fprintf (output, "\n");
}

static void
dump_table_assemblyref (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_ASSEMBLYREF];
	int i;

	fprintf (output, "AssemblyRef Table\n");
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [9];

		expand (t, i, cols, CSIZE (cols));
		fprintf (output, "%d: %d.%d.%d.%d %s\n", i,
			 cols [0], cols [1], cols [2], cols [3],
			 mono_metadata_string_heap (m, cols [6]));
	}
	fprintf (output, "\n");
}

static void
dump_table_param (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_PARAM];
	int i;

	fprintf (output, "Param Table\n");
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [3];

		expand (t, i, cols, CSIZE (cols));
		fprintf (output, "%d: 0x%04x %d %s\n",
			 i,
			 cols [0], cols [1], 
			 mono_metadata_string_heap (m, cols [2]));
	}
	fprintf (output, "\n");
}

/**
 * dis_types:
 * @m: metadata context
 *
 * disassembles all types in the @m context
 */
static void
dis_types (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEDEF];
	int i;

	for (i = 0; i < t->rows; i++)
		dis_type (m, i);
}

/**
 * disassemble_file:
 * @file: file containing CIL code.
 *
 * Disassembles the @file file.
 */
static void
disassemble_file (const char *file)
{
	enum MonoAssemblyOpenStatus status;
	MonoAssembly *ass;
	cli_image_info_t *ii;
	metadata_t *m;


	ass = mono_assembly_open (file, &status);
	if (ass == NULL){
		fprintf (stderr, "Error while trying to process %s\n", file);
		
	}

	ii = ass->image_info;
	m = &ii->cli_metadata;
	
	if (dump_table != -1){
		switch (dump_table){
		case META_TABLE_TYPEDEF:
			dump_table_typedef (m);
			break;
		case META_TABLE_TYPEREF:
			dump_table_typeref (m);
			break;
		case META_TABLE_ASSEMBLYREF:
			dump_table_assemblyref (m);
			break;
		case META_TABLE_PARAM:
			dump_table_param (m);
			break;
		default:
			g_error ("Internal error");
		}
	} else {
		dump_header_data (ass);
		
		dis_directive_assemblyref (m);
		dis_directive_assembly (m);
		dis_types (m);
	}
	
	mono_assembly_close (ass);
}

static void
usage (void)
{
	fprintf (stderr, "Usage is: monodis [--typeref][--typedef][--assemblyref] file ..\n");
	exit (1);
}

int
main (int argc, char *argv [])
{
	GList *input_files = NULL, *l;
	int i;

	output = stdout;
	for (i = 1; i < argc; i++){
		if (argv [i][0] == '-'){
			if (argv [i][1] == 'h')
				usage ();
			else if (argv [i][1] == 'd')
				dump_header_data_p = TRUE;
			else if (strcmp (argv [i], "--help") == 0)
				usage ();
			else if (strcmp (argv [i], "--typeref") == 0)
				dump_table = META_TABLE_TYPEREF;
			else if (strcmp (argv [i], "--typedef") == 0)
				dump_table = META_TABLE_TYPEDEF;
			else if (strcmp (argv [i], "--assemblyref") == 0)
				dump_table = META_TABLE_ASSEMBLYREF;
			else if (strcmp (argv [i], "--param") == 0)
				dump_table = META_TABLE_PARAM;
		} else
			input_files = g_list_append (input_files, argv [i]);
	}

	if (input_files == NULL)
		usage ();
	
	for (l = input_files; l; l = l->next)
		disassemble_file (l->data);

	return 0;
}
