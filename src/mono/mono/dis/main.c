/*
 * main.c: Sample disassembler
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
#include "meta.h"
#include "util.h"
#include "dump.h"
#include "get.h"
#include "dis-cil.h"

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

	if (end > t->rows + 1)
		g_error ("ERROR index out of range in fields");
			
	for (i = start; i < end; i++){
		char *sig, *flags;
		
		expand (t, i, cols, CSIZE (cols));
		sig = get_field_signature (m, cols [2]);
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

static void
dis_code (metadata_t *m, cli_image_info_t *ii, guint32 rva)
{
	MonoMetaMethodHeader *mh;
	const char *ptr = cli_rva_map (ii, rva);

	if (rva == 0)
		return;

	mh = mono_metadata_parse_mh (ptr);
	fprintf (output, "\t.maxstack %d\n", mh->max_stack);
	fprintf (output, "\t// Code size=%d (0x%x)\n", mh->code_size, mh->code_size);
	printf ("\t// Values Code Size=%d/0x%x\n\t// LocalTok=%x\n\n",
		mh->code_size, mh->code_size, mh->local_var_sig_tok);
	dissasemble_cil (m, mh->code, mh->code_size);
	
/*
  hex_dump (mh->code, 0, mh->code_size);
  printf ("\nAfter the code\n");
  hex_dump (mh->code + mh->code_size, 0, 64);
*/
	mono_metadata_free_mh (mh);
}

typedef struct {
	char  flags;
	char *ret_type;
	int   param_count;
	char **param;
} MethodSignature;

/**
 * parse_method_signature:
 * @m: metadata context 
 * @blob_signature: pointer to the signature in the Blob heap
 *
 * 22.2.1: MethodDefSig.  
 *
 * Returns the parsed information in the MethodSignature structure
 * needs to be deallocated with free_method_signature().
 */
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

/**
 * dis_field_list:
 * @m: metadata context
 * @start: starting index into the Method Table.
 * @end: ending index into Method table.
 *
 * This routine displays the methods in the Method Table from @start to @end
 */
static void
dis_method_list (metadata_t *m, cli_image_info_t *ii, guint32 start, guint32 end)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_METHOD];
	metadata_tableinfo_t *p = &m->tables [META_TABLE_PARAM];
	guint32 cols [6];
	guint32 cols_next [6];
	guint32 param_cols [3];
	int i;

	if (end > t->rows){
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
		dis_code (m, ii, cols [0]);
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
dis_type (metadata_t *m, cli_image_info_t *ii, int n)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEDEF];
	guint32 cols [6];
	guint32 cols_next [6];
	const char *name;
	gboolean next_is_valid, last;
	
	expand (t, n, cols, CSIZE (cols));

	if (t->rows > n){
		expand (t, n + 1, cols_next, CSIZE (cols_next));
		next_is_valid = 1;
	} else
		next_is_valid = 0;

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
	if (next_is_valid)
		last = cols_next [4] - 1;
	else
		last = m->tables [META_TABLE_FIELD].rows;
			
	if (cols [4] != cols_next [4] && cols_next [4] != 0)
		dis_field_list (m, cols [4] - 1, last);
	fprintf (output, "\n");

	if (next_is_valid)
		last = cols_next [5] - 1;
	else
		last = m->tables [META_TABLE_METHOD].rows;
	
	if (cols [4] != cols_next [5] && cols_next [5] != 0)
		dis_method_list (m, ii, cols [5] - 1, last);

	fprintf (output, "  }\n}\n\n");
}

/**
 * dis_types:
 * @m: metadata context
 *
 * disassembles all types in the @m context
 */
static void
dis_types (metadata_t *m, cli_image_info_t *ii)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEDEF];
	int i;

	for (i = 1; i < t->rows; i++)
		dis_type (m, ii, i);
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
		case META_TABLE_FIELD:
			dump_table_field (m);
			break;
		default:
			g_error ("Internal error");
		}
	} else {
		dump_header_data (ass);
		
		dis_directive_assemblyref (m);
		dis_directive_assembly (m);
		dis_types (m, ii);
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
			else if (strcmp (argv [i], "--fields") == 0)
				dump_table = META_TABLE_FIELD;
		} else
			input_files = g_list_append (input_files, argv [i]);
	}

	if (input_files == NULL)
		usage ();
	
	for (l = input_files; l; l = l->next)
		disassemble_file (l->data);

	return 0;
}
