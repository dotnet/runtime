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
 *   
 *   How are fields with literals mapped to constants?
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
dump_header_data (MonoImage *img)
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

	mono_metadata_decode_row (t, 0, cols, CSIZE (cols));
	
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
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

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

	if (end > t->rows + 1) {
		g_warning ("ERROR index out of range in fields");
		end = t->rows;
	}
			
	for (i = start; i < end; i++){
		char *sig, *flags;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		sig = get_field_signature (m, cols [2]);
		flags = field_flags (cols [0]);
		
		if (cols [0] & FIELD_ATTRIBUTE_LITERAL){
			ElementTypeEnum type;
			char *lit;
			
			type = get_field_literal_type (m, cols [2]);
			lit = g_strdup ("FIXME:Do-not-know-how-to-get-this-from-the-constants-table");
			/* get_constant (m, type, cols [2]); */
			
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
dis_locals (metadata_t *m, guint32 token) 
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_STANDALONESIG];
	const char *ptr;
	guint32 cols[1];
	int len=0, i, bsize;

	mono_metadata_decode_row (t, (token&0xffffff)-1, cols, CSIZE(cols));
	ptr = mono_metadata_blob_heap (m, cols[0]);
	bsize = mono_metadata_decode_blob_size (ptr, &ptr);
	if (*ptr != 0x07)
			g_warning("wrong signature for locals blob");
	ptr++;
	len = mono_metadata_decode_value (ptr, &ptr);
	fprintf(output, "\t.locals ( // %d\n", len);
	for (i=0; i < len; ++i) {
		int val;
		char * desc = NULL;
		const char *p = ptr;
		MonoType *type;
		val = mono_metadata_decode_value (ptr, &ptr);
		if (val == ELEMENT_TYPE_PINNED) {
			fprintf(output, "//pinned\n");
			p = ptr;
			val = mono_metadata_decode_value (ptr, &ptr);
		}
		if (val == ELEMENT_TYPE_BYREF) {
			fprintf(output, "// byref\n");
			p = ptr;
		}
		type = mono_metadata_parse_type (m, p, &ptr);
		desc = dis_stringify_type (m, type);
		mono_metadata_free_type (type);
		fprintf(output, "\t\t%s\tV_%d\n", desc, i);
		g_free(desc);
	}
	fprintf(output, "\t)\n");
}

static void
dis_code (metadata_t *m, cli_image_info_t *ii, guint32 rva)
{
	MonoMetaMethodHeader *mh;
	const char *ptr = cli_rva_map (ii, rva);
	char *loc;

	if (rva == 0)
		return;

	mh = mono_metadata_parse_mh (m, ptr);
	if (ii->cli_cli_header.ch_entry_point){
		loc = mono_metadata_locate_token (m, ii->cli_cli_header.ch_entry_point);
		if (rva == read32 (loc))
			fprintf (output, "\t.entrypoint\n");
	}
	
	fprintf (output, "\t.maxstack %d\n", mh->max_stack);
	fprintf (output, "\t// Code size=%d (0x%x)\n", mh->code_size, mh->code_size);
	printf ("\t// Values Code Size=%d/0x%x\n\t// LocalTok=%x\n\n",
		mh->code_size, mh->code_size, mh->local_var_sig_tok);
	if (mh->local_var_sig_tok)
		dis_locals (m, mh->local_var_sig_tok);
	dissasemble_cil (m, mh);
	
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
	const char *ptr = mono_metadata_blob_heap (m, blob_signature);
	MethodSignature *ms = g_new0 (MethodSignature, 1);
	int i, len;

	len = mono_metadata_decode_value (ptr, &ptr);
	fprintf (output, "     // SIG: ");
	hex_dump (ptr, 0, -len);
	fprintf (output, "\n");
	
	ms->flags = *ptr++;

	ms->param_count = mono_metadata_decode_value (ptr, &ptr);
	ptr = get_ret_type (m, ptr, &ms->ret_type);
	ms->param = g_new (char *, ms->param_count);
	
	for (i = 0; i < ms->param_count; i++)
		ptr = get_param (m, ptr, &(ms->param [i]));

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


static char *
pinvoke_info (metadata_t *m, guint32 mindex)
{
	metadata_tableinfo_t *im = &m->tables [META_TABLE_IMPLMAP];
	metadata_tableinfo_t *mr = &m->tables [META_TABLE_MODULEREF];
	guint32 im_cols [4];
	guint32 mr_cols [1];
	const char *import, *scope;
	int i;

	for (i = 0; i < im->rows; i++) {

		mono_metadata_decode_row (im, i, im_cols, CSIZE (im_cols));

		if ((im_cols[1] >> 1) == mindex + 1) {

			import = mono_metadata_string_heap (m, im_cols [2]);

			mono_metadata_decode_row (mr, im_cols [3] - 1, 
						  mr_cols, CSIZE (mr_cols));

			scope = mono_metadata_string_heap (m, mr_cols [0]);
				
			return g_strdup_printf ("(%s:%s)", scope, import);
		}
	}

	return NULL;
}

/**
 * dis_method_list:
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
		/*exit (1);*/
		end = t->rows;
	}

	for (i = start; i < end; i++){
		MethodSignature *ms;
		char *flags, *impl_flags;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		mono_metadata_decode_row (t, i + 1, cols_next, CSIZE (cols_next));

		flags = method_flags (cols [2]);
		impl_flags = method_impl_flags (cols [1]);

		ms = parse_method_signature (m, cols [4]);
			
		fprintf (output, "    .method %s", flags);

		if (cols [2] & METHOD_ATTRIBUTE_PINVOKE_IMPL)
			fprintf (output, "%s", pinvoke_info (m, i));

		fprintf (output,
			 "\n           %s %s",
			 ms->ret_type,
			 mono_metadata_string_heap (m, cols [3]));
		if (ms->param_count > 0){
			int i;

			fprintf (output, "(\n");
			for (i = 0; i < ms->param_count; i++){
				char *pf;
				
				mono_metadata_decode_row (p, i, param_cols, CSIZE (param_cols));
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
	
	mono_metadata_decode_row (t, n, cols, CSIZE (cols));

	if (t->rows > n+1){
		mono_metadata_decode_row (t, n + 1, cols_next, CSIZE (cols_next));
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
			
	if (cols[4] && cols[4] <= m->tables [META_TABLE_FIELD].rows)
		dis_field_list (m, cols [4] - 1, last);
	fprintf (output, "\n");

	if (next_is_valid)
		last = cols_next [5] - 1;
	else
		last = m->tables [META_TABLE_METHOD].rows;
	
	if (cols [5] < m->tables [META_TABLE_METHOD].rows)
		dis_method_list (m, ii, cols [5]-1, last);

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

struct {
	char *name;
	int table;
	void (*dumper) (metadata_t *m);
} table_list [] = {
	{ "--assembly",    META_TABLE_ASSEMBLY,    dump_table_assembly },
	{ "--assemblyref", META_TABLE_ASSEMBLYREF, dump_table_assemblyref },
	{ "--fields",      META_TABLE_FIELD,       dump_table_field },
	{ "--memberref",   META_TABLE_MEMBERREF,   dump_table_memberref },
	{ "--param",       META_TABLE_PARAM,       dump_table_param },
	{ "--typedef",     META_TABLE_TYPEDEF,     dump_table_typedef },
	{ "--typeref",     META_TABLE_TYPEREF,     dump_table_typeref },
	{ "--classlayout", META_TABLE_CLASSLAYOUT, dump_table_class_layout },
	{ "--constant",    META_TABLE_CONSTANT,    dump_table_constant },
	{ "--property",    META_TABLE_PROPERTY,    dump_table_property },
	{ "--event",       META_TABLE_EVENT,       dump_table_event },
	{ "--file",        META_TABLE_FILE,        dump_table_file },
	{ "--moduleref",   META_TABLE_MODULEREF,   dump_table_moduleref },
	{ "--method",      META_TABLE_METHOD,      dump_table_method },
	{ NULL, -1 }
};

/**
 * disassemble_file:
 * @file: file containing CIL code.
 *
 * Disassembles the @file file.
 */
static void
disassemble_file (const char *file)
{
	enum MonoImageOpenStatus status;
	MonoImage *img;
	cli_image_info_t *ii;
	metadata_t *m;

	fprintf (output, "// Disassembling %s\n", file);

	img = mono_image_open (file, &status);
	if (img == NULL){
		fprintf (stderr, "Error while trying to process %s\n", file);
		return;
	}

	ii = img->image_info;
	m = &img->metadata;
	
	if (dump_table != -1){
		(*table_list [dump_table].dumper) (m);
	} else {
		dump_header_data (img);
		
		dis_directive_assemblyref (m);
		dis_directive_assembly (m);
		dis_types (m, ii);
	}
	
	mono_image_close (img);
}

static void
usage (void)
{
	GString *args = g_string_new ("[--help]");
	int i;
	
	for (i = 0; table_list [i].name != NULL; i++){
		g_string_append (args, "[");
		g_string_append (args, table_list [i].name);
		g_string_append (args, "] ");
		if (((i-2) % 5) == 0)
			g_string_append_c (args, '\n');
	}
	fprintf (stderr,
		 "Usage is: monodis %s file ..\n", args->str);
	exit (1);
}

int
main (int argc, char *argv [])
{
	GList *input_files = NULL, *l;
	int i, j;

	output = stdout;
	for (i = 1; i < argc; i++){
		if (argv [i][0] == '-'){
			if (argv [i][1] == 'h')
				usage ();
			else if (argv [i][1] == 'd')
				dump_header_data_p = TRUE;
			else if (strcmp (argv [i], "--help") == 0)
				usage ();
			for (j = 0; table_list [j].name != NULL; j++)
				if (strcmp (argv [i], table_list [j].name) == 0)
					dump_table = j;
		} else
			input_files = g_list_append (input_files, argv [i]);
	}

	if (input_files == NULL)
		usage ();
	
	for (l = input_files; l; l = l->next)
		disassemble_file (l->data);

	return 0;
}
