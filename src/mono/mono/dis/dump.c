/*
 * dump.c: Dumping routines for the disassembler.
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
#include "dump.h"
#include "get.h"

void
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

void
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

void
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

void
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

