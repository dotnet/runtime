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

typedef struct {
	int code;
	char *str;
} map_t;

static const char *
map (int code, map_t *table)
{
	int i;

	for (i = 0; table [i].str != NULL; i++)
		if (table [i].code == code)
			return table [i].str;
	g_assert_not_reached ();
	return "";
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

static void
dis_fields (metadata_t *m, guint32 idx)
{
	fprintf (output, "FIELD_LIST: %d\n", idx);
}

static void
dis_methods (metadata_t *m, guint32 idx)
{
}

static char *
typedef_or_ref (guint32 index)
{
	return "";
}
	       
static void
dis_type (metadata_t *m, metadata_tableinfo_t *t, int n)
{
	guint32 cols [6];
	const char *name;
	char *tn;
	
	expand (t, n, cols, CSIZE (cols));

	name = mono_metadata_string_heap (m, cols [1]);

	if ((cols [0] & TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK) == TYPE_ATTRIBUTE_CLASS)
		tn = "class";
	else
		tn = "interface";
	
	fprintf (output, "  .%s %s%s\n", tn, typedef_flags (cols [0]), name);
	fprintf (output, "  \textends %s\n", typedef_or_ref (cols [3]));
	fprintf (output, "  {\n");
	dis_fields (m, cols [4]);
	dis_methods (m, cols [5]);
	fprintf (output, "  }\n");
}

static void
dis_types (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_TYPEDEF];
	int i;

	for (i = 0; i < t->rows; i++){
		dis_type (m, t, i);
	}
}

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
