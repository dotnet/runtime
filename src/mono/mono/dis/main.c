/*
 * dis.c: Sample disassembler
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
#include <mono/metadata/assembly.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/endian.h>
#include <mono/metadata/typeattr.h>
#include <mono/metadata/fieldattr.h>
#include <mono/metadata/eltype.h>
#include "util.h"

FILE *output;

/* True if you want to get a dump of the header data */
gboolean dump_header_data_p = FALSE;

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
	{ TYPE_ATTRIBUTE_NOT_PUBLIC,           "not-public " },
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

static map_t access_map [] = {
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
	{ ELEMENT_TYPE_VOID       , "System.Void" },
	{ ELEMENT_TYPE_BOOLEAN    , "System.Bool" },
	{ ELEMENT_TYPE_CHAR       , "System.Char" }, 
	{ ELEMENT_TYPE_I1         , "System.SByte" },
	{ ELEMENT_TYPE_U1         , "System.Byte" }, 
	{ ELEMENT_TYPE_I2         , "System.Int16" },
	{ ELEMENT_TYPE_U2         , "System.UInt16" },
	{ ELEMENT_TYPE_I4         , "System.Int32" },
	{ ELEMENT_TYPE_U4         , "System.UInt32" },
	{ ELEMENT_TYPE_I8         , "System.Int64" },
	{ ELEMENT_TYPE_U8         , "System.UInt64" },
	{ ELEMENT_TYPE_R4         , "System.Single" },
	{ ELEMENT_TYPE_R8         , "System.Double" },
	{ ELEMENT_TYPE_STRING     , "System.String" },
	{ ELEMENT_TYPE_TYPEDBYREF , "TypedByRef" },
	{ ELEMENT_TYPE_I          , "System.Int32" },
	{ ELEMENT_TYPE_U          , "System.UPtr" },
	{ ELEMENT_TYPE_OBJECT     , "System.Object" },
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

	strcat (buffer, map (access, access_map));
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

/**
 * get_custom_mod:
 *
 * Decodes a CustomMod (22.2.7)
 *
 * Returns: updated pointer location
 */
static const char *
get_custom_mod (const char *ptr, char **return_value)
{
	if ((*ptr == ELEMENT_TYPE_CMOD_OPT) ||
	    (*ptr == ELEMENT_TYPE_CMOD_REQD)){
		fprintf (stderr, "FIXME: still do not support CustomMods (22.2.7)");
		exit (1);
	}
	*return_value = NULL;
	return ptr;
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
	guint32 cols [9];

/*	g_assert (idx <= m->tables [META_TABLE_MODULE].rows); */
	    
/*	return g_strdup_printf ("IDX=0x%x", idx); */
	expand (&m->tables [META_TABLE_ASSEMBLYREF], 0, cols, CSIZE (cols));

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
	rs_idx = cols [0] >> 3;
	table = cols [0] & 7;
	printf ("------------ %d %d --------\n", rs_idx, table);
		
	switch (table){
	case 0: /* Module */
		x = get_module (m, rs_idx);
		ret = g_strdup_printf ("[%08x:%s] %s.%s", cols [0], x, s, t);
		g_free (x);
		break;

	case 1: /* ModuleRef */
		ret = g_strdup_printf ("TypeRef: ModuleRef (%s.%s)", s, t);
		
	case 3: /* AssemblyRef */
		ret = g_strdup_printf ("TypeRef: AssemblyRef (%s.%s)", s, t);
		
	case 4: /* TypeRef */
		ret =  g_strdup_printf ("TypeRef: TYPEREF! (%s.%s)", s, t);

	default:
		ret = g_strdup ("ERROR");
	}

	return ret;
}

static char *
typedef_or_ref (metadata_t *m, guint32 dor_token)
{
	int table = dor_token & 0x03;
	int idx = dor_token >> 2;
	char *s, *temp = NULL;
		
	switch (table){
	case 0: /* TypeDef */
		temp = get_typedef (m, idx);
		s = g_strdup_printf ("%s", temp);
		break;
		
	case 1: /* TypeRef */
		temp = get_typeref (m, idx);
		s = g_strdup_printf ("/* 0x%08x */ %s", dor_token, temp);
		break;
		
	case 2: /* TypeSpec */
		s = g_strdup_printf ("TypeSpec: 0x%08x", dor_token);
		break;
		
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

	*result = typedef_or_ref (m, token);

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
 * get_type:
 * @m: metadata context 
 * @ptr: location to decode from.
 * @result: pointer to string where resulting decoded string is stored
 *
 * This routine returs in @result the stringified type pointed by @ptr.
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
		
	case ELEMENT_TYPE_ARRAY:
		*result = g_strdup ("ARRAY:TODO");
		break;
		
	case ELEMENT_TYPE_SZARRAY:
		*result = g_strdup ("SZARRAY:TODO");
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
	int len;
	static char buffer [8192];
	
	ptr = get_encoded_value (ptr, &len);
	base = ptr;
	/* FIELD is 0x06 */
	g_assert (*ptr == 0x06);
	hex_dump (ptr, 0, len);
	ptr++; len--;
	
	ptr = get_custom_mod (ptr, &allocated_modifier_string);
	ptr = get_type (m, ptr, &allocated_type_string);
	
	sprintf (buffer, "LEN=%d::::   ", len);
	strcat (buffer, allocated_type_string);
	
	if (allocated_modifier_string)
		g_free (allocated_modifier_string);
	if (allocated_type_string)
		g_free (allocated_modifier_string);
	
	return g_strdup (buffer);
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
	char *tn;
	
	expand (t, n, cols, CSIZE (cols));
	expand (t, n + 1, cols_next, CSIZE (cols_next));

	name = mono_metadata_string_heap (m, cols [1]);

	if ((cols [0] & TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK) == TYPE_ATTRIBUTE_CLASS)
		tn = "class";
	else
		tn = "interface";
	
	fprintf (output, "  .%s %s%s\n", tn, typedef_flags (cols [0]), name);
	fprintf (output, "  \textends %s\n", typedef_or_ref (m, cols [3]));
	fprintf (output, "  {\n");

	/*
	 * The value in the table is always valid, we know we have fields
	 * if the value stored is different than the next record.
	 */
	if (cols [4] != cols_next [4])
		dis_field_list (m, cols [4] - 1, cols_next [4] - 1);
	if (cols [4] != cols_next [5])
		dis_method_list (m, cols [5], cols_next [5]);

	fprintf (output, "  }\n");
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

	dump_header_data (ass);

	ii = ass->image_info;
	m = &ii->cli_metadata;
	dis_directive_assemblyref (m);
	dis_directive_assembly (m);
	dis_types (m);
	
	mono_assembly_close (ass);
}

static void
usage (void)
{
	fprintf (stderr, "Usage is: monodis file1 ..\n");
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
		} else
			input_files = g_list_append (input_files, argv [i]);
	}

	if (input_files == NULL)
		usage ();
	
	for (l = input_files; l; l = l->next)
		disassemble_file (l->data);

	return 0;
}
