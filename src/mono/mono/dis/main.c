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
dis_directive_assembly (MonoMetadata *m)
{
	MonoTableInfo *t  = &m->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];
	
	if (t->base == NULL)
		return;

	mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);
	
	fprintf (output,
		 ".assembly %s\n"
		 "{\n"
		 "  .hash algorithm 0x%08x\n"
		 "  .ver  %d.%d.%d.%d"
		 "%s %s"
		 "%s"
		 "\n"
		 "}\n",
		 mono_metadata_string_heap (m, cols [MONO_ASSEMBLY_NAME]),
		 cols [MONO_ASSEMBLY_HASH_ALG],
		 cols [MONO_ASSEMBLY_MAJOR_VERSION], cols [MONO_ASSEMBLY_MINOR_VERSION], 
		 cols [MONO_ASSEMBLY_BUILD_NUMBER], cols [MONO_ASSEMBLY_REV_NUMBER],
		 cols [MONO_ASSEMBLY_CULTURE] ? "\n  .locale" : "",
		 cols [MONO_ASSEMBLY_CULTURE] ? mono_metadata_string_heap (m, cols [MONO_ASSEMBLY_CULTURE]) : "",
		 cols [MONO_ASSEMBLY_PUBLIC_KEY] ? "\n  .publickey" : ""
		);
}

static void
dis_directive_assemblyref (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_ASSEMBLYREF];
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	int i;
	
	if (t->base == NULL)
		return;

	for (i = 0; i < t->rows; i++){
		mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);

		fprintf (output,
			 ".assembly extern %s\n"
			 "{\n"
			 "  .ver %d.%d.%d.%d\n"
			 "}\n",
			 mono_metadata_string_heap (m, cols [MONO_ASSEMBLYREF_NAME]),
			 cols [MONO_ASSEMBLYREF_MAJOR_VERSION], cols [MONO_ASSEMBLYREF_MINOR_VERSION], 
			 cols [MONO_ASSEMBLYREF_BUILD_NUMBER], cols [MONO_ASSEMBLYREF_REV_NUMBER]
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
dis_field_list (MonoMetadata *m, guint32 start, guint32 end)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	guint32 cols [MONO_FIELD_SIZE];
	int i;

	if (end > t->rows + 1) {
		g_warning ("ERROR index out of range in fields");
		end = t->rows;
	}
			
	for (i = start; i < end; i++){
		char *sig, *flags;
		
		mono_metadata_decode_row (t, i, cols, MONO_FIELD_SIZE);
		sig = get_field_signature (m, cols [MONO_FIELD_SIGNATURE]);
		flags = field_flags (cols [MONO_FIELD_FLAGS]);
		
		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_LITERAL){
			MonoTypeEnum type;
			char *lit;
			
			type = get_field_literal_type (m, cols [MONO_FIELD_SIGNATURE]);
			lit = g_strdup ("FIXME:Do-not-know-how-to-get-this-from-the-constants-table");
			/* get_constant (m, type, cols [2]); */
			
			fprintf (output, "    .field %s %s %s = ",
				 flags, sig,
				 mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]));
			fprintf (output, "%s\n", lit);
			g_free (lit);
		} else 
			fprintf (output, "    .field %s %s %s\n",
				 flags, sig,
				 mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]));
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
	{ METHOD_ATTRIBUTE_UNMANAGED_EXPORT,    "export " },
	{ METHOD_ATTRIBUTE_HAS_SECURITY,        "hassecurity" },
	{ METHOD_ATTRIBUTE_REQUIRE_SEC_OBJECT,  "requiresecobj" },
	{ METHOD_ATTRIBUTE_PINVOKE_IMPL,        "pinvokeimpl " }, 
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

static map_t pinvoke_flags_map [] = {
	{ PINVOKE_ATTRIBUTE_NO_MANGLE ,            "nomangle " },
	{ PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR,   "lasterr " },
	{ 0, NULL }
};

static map_t pinvoke_call_conv_map [] = {
	{ PINVOKE_ATTRIBUTE_CALL_CONV_WINAPI,      "winapi " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_CDECL,       "cdecl " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_STDCALL,     "stdcall " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_THISCALL,    "thiscall " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_FASTCALL,    "fastcall " },
	{ 0, NULL }
};

static map_t pinvoke_char_set_map [] = {
	{ PINVOKE_ATTRIBUTE_CHAR_SET_NOT_SPEC,     "" },
	{ PINVOKE_ATTRIBUTE_CHAR_SET_ANSI,         "ansi " },
	{ PINVOKE_ATTRIBUTE_CHAR_SET_UNICODE ,     "unicode " },
	{ PINVOKE_ATTRIBUTE_CHAR_SET_AUTO,         "autochar " },
	{ 0, NULL }
};

/**
 * pinvoke_flags:
 *
 * Returns a stringified version of the Method's pinvoke flags
 */
static char *
pinvoke_flags (guint32 f)
{
	GString *str = g_string_new ("");
	int cset = f & PINVOKE_ATTRIBUTE_CHAR_SET_MASK;
	int cconv = f & PINVOKE_ATTRIBUTE_CALL_CONV_MASK;
	char *s;
	
	g_string_append (str, map (cset, pinvoke_char_set_map));
	g_string_append (str, map (cconv, pinvoke_call_conv_map));
	g_string_append (str, flags (f, pinvoke_flags_map));

	s = g_strdup(str->str);
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
dis_locals (MonoMetadata *m, MonoMethodHeader *mh) 
{
	int i;

	fprintf(output, "\t.locals %s(\n", mh->init_locals ? "init " : "");
	for (i=0; i < mh->num_locals; ++i) {
		char * desc;
		/* print also byref and pinned attributes */
		desc = dis_stringify_type (m, mh->locals[i]);
		fprintf(output, "\t\t%s\tV_%d\n", desc, i);
		g_free(desc);
	}
	fprintf(output, "\t)\n");
}

static void
dis_code (MonoMetadata *m, MonoCLIImageInfo *ii, guint32 rva)
{
	MonoMethodHeader *mh;
	const char *ptr = mono_cli_rva_map (ii, rva);
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
	printf ("\t// Values Code Size=%d/0x%x\n\n",
		mh->code_size, mh->code_size);
	if (mh->num_locals)
		dis_locals (m, mh);
	dissasemble_cil (m, mh);
	
/*
  hex_dump (mh->code, 0, mh->code_size);
  printf ("\nAfter the code\n");
  hex_dump (mh->code + mh->code_size, 0, 64);
*/
	mono_metadata_free_mh (mh);
}

static char *
pinvoke_info (MonoMetadata *m, guint32 mindex)
{
	MonoTableInfo *im = &m->tables [MONO_TABLE_IMPLMAP];
	MonoTableInfo *mr = &m->tables [MONO_TABLE_MODULEREF];
	guint32 im_cols [MONO_IMPLMAP_SIZE];
	guint32 mr_cols [MONO_MODULEREF_SIZE];
	const char *import, *scope;
	char *flags;
	int i;

	for (i = 0; i < im->rows; i++) {

		mono_metadata_decode_row (im, i, im_cols, MONO_IMPLMAP_SIZE);

		if ((im_cols [MONO_IMPLMAP_MEMBER] >> 1) == mindex + 1) {

			flags = pinvoke_flags (im_cols [MONO_IMPLMAP_FLAGS]);

			import = mono_metadata_string_heap (m, im_cols [MONO_IMPLMAP_NAME]);

			mono_metadata_decode_row (mr, im_cols [MONO_IMPLMAP_SCOPE] - 1, 
						  mr_cols, MONO_MODULEREF_SIZE);

			scope = mono_metadata_string_heap (m, mr_cols [MONO_MODULEREF_NAME]);
				
			return g_strdup_printf ("(%s as %s %s)", scope, import,
						flags);
			g_free (flags);
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
dis_method_list (MonoMetadata *m, MonoCLIImageInfo *ii, guint32 start, guint32 end)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_METHOD];
	guint32 cols [MONO_METHOD_SIZE];
	int i;

	if (end > t->rows){
		fprintf (output, "ERROR index out of range in methods");
		/*exit (1);*/
		end = t->rows;
	}

	for (i = start; i < end; i++){
		MonoMethodSignature *ms;
		char *flags, *impl_flags;
		const char *sig;
		char *sig_str;
		
		mono_metadata_decode_row (t, i, cols, MONO_METHOD_SIZE);

		flags = method_flags (cols [MONO_METHOD_FLAGS]);
		impl_flags = method_impl_flags (cols [MONO_METHOD_IMPLFALGS]);

		sig = mono_metadata_blob_heap (m, cols [MONO_METHOD_SIGNATURE]);
		mono_metadata_decode_blob_size (sig, &sig);
		ms = mono_metadata_parse_method_signature (m, 1, sig, &sig);
		sig_str = dis_stringify_method_signature (m, ms, i + 1);
			
		fprintf (output, "    .method %s", flags);

		if (cols [MONO_METHOD_FLAGS] & METHOD_ATTRIBUTE_PINVOKE_IMPL)
			fprintf (output, "%s", pinvoke_info (m, i));

		fprintf (output, "\n           %s", sig_str);
		fprintf (output, " %s\n", impl_flags);
		g_free (flags);
		g_free (impl_flags);
		
		fprintf (output, "    {\n");
		fprintf (output, "        // Method begins at RVA 0x%x\n", cols [MONO_METHOD_RVA]);
		dis_code (m, ii, cols [MONO_METHOD_RVA]);
		fprintf (output, "    }\n\n");
		mono_metadata_free_method_signature (ms);
		g_free (sig_str);
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
dis_type (MonoMetadata *m, MonoCLIImageInfo *ii, int n)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	const char *name;
	gboolean next_is_valid, last;
	
	mono_metadata_decode_row (t, n, cols, MONO_TYPEDEF_SIZE);

	if (t->rows > n + 1) {
		mono_metadata_decode_row (t, n + 1, cols_next, MONO_TYPEDEF_SIZE);
		next_is_valid = 1;
	} else
		next_is_valid = 0;

	fprintf (output, ".namespace %s\n{\n", mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAMESPACE]));
	name = mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAME]);

	if ((cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK) == TYPE_ATTRIBUTE_CLASS){
		char *base = get_typedef_or_ref (m, cols [MONO_TYPEDEF_EXTENDS]);
		fprintf (output, "  .class %s%s\n", typedef_flags (cols [MONO_TYPEDEF_FLAGS]), name);
		fprintf (output, "  \textends %s\n", base);
		g_free (base);
	} else
		fprintf (output, "  .class interface %s%s\n", typedef_flags (cols [MONO_TYPEDEF_FLAGS]), name);
	
	fprintf (output, "  {\n");

	/*
	 * The value in the table is always valid, we know we have fields
	 * if the value stored is different than the next record.
	 */

	if (next_is_valid)
		last = cols_next [MONO_TYPEDEF_FIELD_LIST] - 1;
	else
		last = m->tables [MONO_TABLE_FIELD].rows;
			
	if (cols [MONO_TYPEDEF_FIELD_LIST] && cols [MONO_TYPEDEF_FIELD_LIST] <= m->tables [MONO_TABLE_FIELD].rows)
		dis_field_list (m, cols [MONO_TYPEDEF_FIELD_LIST] - 1, last);
	fprintf (output, "\n");

	if (next_is_valid)
		last = cols_next [MONO_TYPEDEF_METHOD_LIST] - 1;
	else
		last = m->tables [MONO_TABLE_METHOD].rows;
	
	if (cols [MONO_TYPEDEF_METHOD_LIST] < m->tables [MONO_TABLE_METHOD].rows)
		dis_method_list (m, ii, cols [MONO_TYPEDEF_METHOD_LIST] - 1, last);

	fprintf (output, "  }\n}\n\n");
}

/**
 * dis_types:
 * @m: metadata context
 *
 * disassembles all types in the @m context
 */
static void
dis_types (MonoMetadata *m, MonoCLIImageInfo *ii)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	int i;

	for (i = 1; i < t->rows; i++)
		dis_type (m, ii, i);
}

struct {
	char *name;
	int table;
	void (*dumper) (MonoMetadata *m);
} table_list [] = {
	{ "--assembly",    MONO_TABLE_ASSEMBLY,    dump_table_assembly },
	{ "--assemblyref", MONO_TABLE_ASSEMBLYREF, dump_table_assemblyref },
	{ "--fields",      MONO_TABLE_FIELD,       dump_table_field },
	{ "--memberref",   MONO_TABLE_MEMBERREF,   dump_table_memberref },
	{ "--param",       MONO_TABLE_PARAM,       dump_table_param },
	{ "--typedef",     MONO_TABLE_TYPEDEF,     dump_table_typedef },
	{ "--typeref",     MONO_TABLE_TYPEREF,     dump_table_typeref },
	{ "--classlayout", MONO_TABLE_CLASSLAYOUT, dump_table_class_layout },
	{ "--constant",    MONO_TABLE_CONSTANT,    dump_table_constant },
	{ "--property",    MONO_TABLE_PROPERTY,    dump_table_property },
	{ "--event",       MONO_TABLE_EVENT,       dump_table_event },
	{ "--file",        MONO_TABLE_FILE,        dump_table_file },
	{ "--moduleref",   MONO_TABLE_MODULEREF,   dump_table_moduleref },
	{ "--method",      MONO_TABLE_METHOD,      dump_table_method },
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
	MonoCLIImageInfo *ii;
	MonoMetadata *m;

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
	GString *args = g_string_new ("[--help] ");
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
			for (j = 0; table_list [j].name != NULL; j++) {
				if (strcmp (argv [i], table_list [j].name) == 0)
					dump_table = j;
			}
			if (dump_table < 0)
				usage ();
		} else
			input_files = g_list_append (input_files, argv [i]);
	}

	if (input_files == NULL)
		usage ();
	
	for (l = input_files; l; l = l->next)
		disassemble_file (l->data);

	return 0;
}
