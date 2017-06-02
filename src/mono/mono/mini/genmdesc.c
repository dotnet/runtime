/**
 * \file
 * Generates the machine description
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <ctype.h>
#include <string.h>
#include <mono/metadata/opcodes.h>

#define MINI_OP(a,b,dest,src1,src2) b,
#define MINI_OP3(a,b,dest,src1,src2,src3) b,
/* keep in sync with the enum in mini.h */
static const char* const
opnames[] = {
#include "mini-ops.h"
};
#undef MINI_OP
#undef MINI_OP3

/*
 * Duplicate this from helpers.c, so the opcode name array can be omitted when 
 * DISABLE_JIT is set.
 */
static const char*
inst_name (int op) {
	if (op >= OP_LOAD && op <= OP_LAST)
		return opnames [op - OP_LOAD];
	if (op < OP_LOAD)
		return mono_opcode_name (op);
	g_error ("unknown opcode name for %d", op);
	return NULL;
}

typedef struct {
	int num;
	const char *name;
	char *desc;
	char *comment;
	char spec [MONO_INST_MAX];
} OpDesc;

static int nacl = 0;
static GHashTable *table;
static GHashTable *template_table;

#define eat_whitespace(s) while (*(s) && isspace (*(s))) s++;

// Per spec isalnum() expects input in the range 0-255
// and can misbehave if you pass in a signed char.
static int
isalnum_char(char c)
{
	return isalnum ((unsigned char)c);
}

static int
load_file (const char *name) {
	FILE *f;
	char buf [256];
	char *str, *p;
	int line;
	OpDesc *desc;
	GString *comment;

	if (!(f = fopen (name, "r")))
		g_error ("Cannot open file '%s'", name);

	comment = g_string_new ("");
	/*
	 * The format of the lines are:
	 * # comment
	 * opcode: [dest:format] [src1:format] [src2:format] [flags:format] [clob:format] 
	 * 	[cost:num] [res:format] [delay:num] [len:num] [nacl:num]
	 * format is a single letter that depends on the field
	 * NOTE: no space between the field name and the ':'
	 *
	 * len: maximum instruction length
	 */
	line = 0;
	while ((str = fgets (buf, sizeof (buf), f))) {
		gboolean is_template = FALSE;
		gboolean nacl_length_set = FALSE;

		++line;
		eat_whitespace (str);
		if (!str [0])
			continue;
		if (str [0] == '#') {
			g_string_append (comment, str);
			continue;
		}
		p = strchr (str, ':');
		if (!p)
			g_error ("Invalid format at line %d in %s\n", line, name);
		*p++ = 0;
		eat_whitespace (p);
		if (strcmp (str, "template") == 0) {
			is_template = TRUE;
			desc = g_new0 (OpDesc, 1);
		} else {
			desc = (OpDesc *)g_hash_table_lookup (table, str);
			if (!desc)
				g_error ("Invalid opcode '%s' at line %d in %s\n", str, line, name);
			if (desc->desc)
				g_error ("Duplicated opcode %s at line %d in %s\n", str, line, name);
		}
		desc->desc = g_strdup (p);
		desc->comment = g_strdup (comment->str);
		g_string_truncate (comment, 0);
		while (*p) {
			if (strncmp (p, "dest:", 5) == 0) {
				desc->spec [MONO_INST_DEST] = p [5];
				p += 6;
			} else if (strncmp (p, "src1:", 5) == 0) {
				desc->spec [MONO_INST_SRC1] = p [5];
				p += 6;
			} else if (strncmp (p, "src2:", 5) == 0) {
				desc->spec [MONO_INST_SRC2] = p [5];
				p += 6;
			} else if (strncmp (p, "src3:", 5) == 0) {
				desc->spec [MONO_INST_SRC3] = p [5];
				p += 6;
			} else if (strncmp (p, "clob:", 5) == 0) {
				desc->spec [MONO_INST_CLOB] = p [5];
				p += 6;
				/* Currently unused fields
			} else if (strncmp (p, "cost:", 5) == 0) {
				desc->spec [MONO_INST_COST] = p [5];
				p += 6;
			} else if (strncmp (p, "res:", 4) == 0) {
				desc->spec [MONO_INST_RES] = p [4];
				p += 5;
			} else if (strncmp (p, "flags:", 6) == 0) {
				desc->spec [MONO_INST_FLAGS] = p [6];
				p += 7;
			} else if (strncmp (p, "delay:", 6) == 0) {
				desc->spec [MONO_INST_DELAY] = p [6];
				p += 7;
				*/
			} else if (strncmp (p, "len:", 4) == 0) {
				unsigned long size;
				char* endptr;
				p += 4;
				size = strtoul (p, &endptr, 10);
				if (size == 0 && p == endptr)
					g_error ("Invalid length '%s' at line %d in %s\n", p, line, name);
				p = endptr;
				if (!nacl_length_set) {
					desc->spec [MONO_INST_LEN] = size;
				}
			} else if (strncmp (p, "nacl:", 5) == 0) {
				unsigned long size;
				p += 5;
				size = strtoul (p, &p, 10);
				if (nacl) {
					desc->spec [MONO_INST_LEN] = size;
					nacl_length_set = TRUE;
				}
			} else if (strncmp (p, "template:", 9) == 0) {
				char *tname;
				int i;
				OpDesc *tdesc;
				p += 9;
				tname = p;
				while (*p && isalnum_char (*p)) ++p;
				*p++ = 0;
				tdesc = (OpDesc *)g_hash_table_lookup (template_table, tname);
				if (!tdesc)
					g_error ("Invalid template name %s at '%s' at line %d in %s\n", tname, p, line, name);
				for (i = 0; i < MONO_INST_MAX; ++i) {
					if (desc->spec [i])
						g_error ("The template overrides any previous value set at line %d in %s\n", line, name);
				}
				memcpy (desc->spec, tdesc->spec, sizeof (desc->spec));
			} else if (strncmp (p, "name:", 5) == 0) {
				char *tname;
				if (!is_template)
					g_error ("name tag only valid in templates at '%s' at line %d in %s\n", p, line, name);
				if (desc->name)
					g_error ("Duplicated name tag in template %s at '%s' at line %d in %s\n", desc->name, p, line, name);
				p += 5;
				tname = p;
				while (*p && isalnum_char (*p)) ++p;
				*p++ = 0;
				if (g_hash_table_lookup (template_table, tname))
					g_error ("Duplicated template %s at line %d in %s\n", tname, line, name);
				desc->name = g_strdup (tname);
				g_hash_table_insert (template_table, (void*)desc->name, desc);
			} else {
				g_error ("Parse error at '%s' at line %d in %s\n", p, line, name);
			}
			eat_whitespace (p);
		}
		if (is_template && !desc->name)
			g_error ("Template without name at line %d in %s\n", line, name);
	}
	g_string_free (comment,TRUE);
	fclose (f);
	return 0;
}

static OpDesc *opcodes = NULL;

static void
init_table (void) {
	int i;
	OpDesc *desc;

	template_table = g_hash_table_new (g_str_hash, g_str_equal);
	table = g_hash_table_new (g_str_hash, g_str_equal);

	opcodes = g_new0 (OpDesc, OP_LAST);
	for (i = OP_LOAD; i < OP_LAST; ++i) {
		desc = opcodes + i;
		desc->num = i;
		desc->name = inst_name (i);
		g_hash_table_insert (table, (char *)desc->name, desc);
	}
}

static void
output_char (FILE *f, char c) {
	if (isalnum_char (c))
		fprintf (f, "%c", c);
	else
		fprintf (f, "\\x%x\" \"", (guint8)c);
}

static void
build_table (const char *fname, const char *name) {
	FILE *f;
	int i, j, idx;
	OpDesc *desc;
	GString *idx_array =  g_string_new ("");
	/* use this to remove duplicates */
	GHashTable *desc_ht = g_hash_table_new (g_str_hash, g_str_equal);

	if (!(f = fopen (fname, "w")))
		g_error ("Cannot open file '%s'", fname);
	fprintf (f, "/* File automatically generated by genmdesc, don't change */\n\n");
	fprintf (f, "const char mono_%s [] = {\n", name);
	fprintf (f, "\t\"");
	for (j = 0; j < MONO_INST_MAX; ++j)
		fprintf (f, "\\x0");
	fprintf (f, "\"\t/* null entry */\n");
	idx = 1;
	g_string_append_printf (idx_array, "const guint16 mono_%s_idx [] = {\n", name);

	for (i = OP_LOAD; i < OP_LAST; ++i) {
		desc = opcodes + i;
		if (!desc->desc)
			g_string_append_printf (idx_array, "\t0,\t/* %s */\n", desc->name ? desc->name : "");
		else {
			fprintf (f, "\t\"");
			for (j = 0; j < MONO_INST_MAX; ++j)
				output_char (f, desc->spec [j]);
			fprintf (f, "\"\t/* %s */\n", desc->name);
			g_string_append_printf (idx_array, "\t%d,\t/* %s */\n", idx * MONO_INST_MAX, desc->name);
			++idx;
		}
	}
	fprintf (f, "};\n\n");
	fprintf (f, "%s};\n\n", idx_array->str);
	fclose (f);
	g_string_free (idx_array, TRUE);
	g_hash_table_destroy (desc_ht);
}

static void
dump (void) {
	int i;
	OpDesc *desc;
	
	for (i = 0; i < MONO_CEE_LAST; ++i) {
		desc = opcodes + i;
		if (desc->comment)
			g_print ("%s", desc->comment);
		if (!desc->desc)
			g_print ("%s:\n", desc->name);
		else {
			g_print ("%s: %s", desc->name, desc->desc);
			if (!strchr (desc->desc, '\n'))
				g_print ("\n");
		}
	}
	for (i = OP_LOAD; i < OP_LAST; ++i) {
		desc = opcodes + i;
		if (!desc->desc)
			g_print ("%s:\n", desc->name);
		else {
			g_print ("%s: %s", desc->name, desc->desc);
			if (!strchr (desc->desc, '\n'))
				g_print ("\n");
		}
	}
}

/*
 * TODO: output the table (possibly merged), in the input format 
 */
int 
main (int argc, char* argv [])
{
	init_table ();
	if (argc == 2) {
		/* useful to get a new file when some opcodes are added: looses the comments, though */
		load_file (argv [1]);
		dump ();
	} else if (argc < 4) {
		g_print ("Usage: genmdesc arguments\n");
		g_print ("\tgenmdesc desc     Output to stdout the description file.\n");
		g_print ("\tgenmdesc [--nacl] output name desc [desc1...]\n"
			"                     Write to output the description in a table named 'name',\n"
			"                     use --nacl to generate Google NativeClient code\n");
		return 1;
	} else {
		int i = 3;
		if (strcmp (argv [1], "--nacl") == 0) {
			nacl = 1;
			i++;
		}
		
		for (; i < argc; ++i)
			load_file (argv [i]);
		
		build_table (argv [1 + nacl], argv [2 + nacl]);
	}
	return 0;
}

