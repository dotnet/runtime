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
dump_table_assembly (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_ASSEMBLY];
	guint32 cols [9];
	const char *ptr;
	int len;

	mono_metadata_decode_row (t, 0, cols, CSIZE (cols));
	fprintf (output, "Assembly Table\n");

	fprintf (output, "Name:          %s\n", mono_metadata_string_heap (m, cols [7]));
	fprintf (output, "Hash Algoritm: 0x%08x\n", cols [0]);
	fprintf (output, "Version:       %d.%d.%d.%d\n", cols [1], cols [2], cols [3], cols [4]);
	fprintf (output, "Flags:         0x%08x\n", cols [5]);
	fprintf (output, "PublicKey:     BlobPtr (0x%08x)\n", cols [6]);

	ptr = mono_metadata_blob_heap (m, cols [6]);
	ptr = mono_metadata_decode_value (ptr, &len);
	if (len > 0){
		fprintf (output, "\tDump:");
		hex_dump (ptr, 0, len);
		fprintf (output, "\n");
	} else
		fprintf (output, "\tZero sized public key\n");
	
	fprintf (output, "Culture:       %s\n", mono_metadata_string_heap (m, cols [8]));
	fprintf (output, "\n");
}

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
		guint32 cols [6];

		mono_metadata_decode_row (&m->tables [META_TABLE_TYPEDEF], i - 1, cols, CSIZE (cols));

		fprintf (output, "%d: %s (flist=%d, mlist=%d)\n", i, s, cols [4], cols [5]);
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
		const char *ptr;
		int len;
		guint32 cols [9];

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		fprintf (output, "%d: Version=%d.%d.%d.%d\n\tName=%s\n", i,
			 cols [0], cols [1], cols [2], cols [3],
			 mono_metadata_string_heap (m, cols [6]));
		ptr = mono_metadata_blob_heap (m, cols [6]);
		ptr = mono_metadata_decode_value (ptr, &len);
		if (len > 0){
			fprintf (output, "\tPublic Key:");
			hex_dump (ptr, 0, len);
			fprintf (output, "\n");
		} else
			fprintf (output, "\tZero sized public key\n");
		
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

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		fprintf (output, "%d: 0x%04x %d %s\n",
			 i,
			 cols [0], cols [1], 
			 mono_metadata_string_heap (m, cols [2]));
	}
	fprintf (output, "\n");
}

void
dump_table_field (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_FIELD];
	int i;

	fprintf (output, "Field Table (0..%d)\n", t->rows);
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [3];
		char *sig, *flags;

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		sig = get_field_signature (m, cols [2]);
		flags = field_flags (cols [0]);
		fprintf (output, "%d: %s %s: %s\n",
			 i,
			 sig,
			 mono_metadata_string_heap (m, cols [1]),
			 flags);
		g_free (sig);
		g_free (flags);
	}
	fprintf (output, "\n");
}

void
dump_table_memberref (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_MEMBERREF];
	int i, kind, idx;
	char *ks, *x, *xx;

	fprintf (output, "MemberRef Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [3];

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		
		kind = cols [0] & 7;
		idx = cols [0] >> 3;

		x = g_strdup ("UNHANDLED CASE");
		
		switch (kind){
		case 0:
			ks = "TypeDef"; break;
		case 1:
			ks = "TypeRef";
			xx = get_typeref (m, idx);
			x = g_strconcat (xx, ".", mono_metadata_string_heap (m, cols [1]), NULL);
			g_free (xx);
			break;
		case 2:
			ks = "ModuleRef"; break;
		case 3:
			ks = "MethodDef"; break;
		case 4:
			ks = "TypeSpec"; break;
		default:
			g_error ("Unknown tag: %d\n", kind);
		}
		
		fprintf (output, "%d: %s[%d] %s\n\tResolved: %s\n\tSignature: %s\n\t\n",
			 i,
			 ks, idx,
			 mono_metadata_string_heap (m, cols [1]),
			 x ? x : "",
			 get_methodref_signature (m, cols [2], NULL));

		if (x)
			g_free (x);
	}
}

void
dump_table_class_layout (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_CLASSLAYOUT];
	int i;
	fprintf (output, "ClassLayout Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [3];
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		fprintf (output, "%d: PackingSize=%d  ClassSize=%d  Parent=%s\n",
			 i, cols [0], cols [1], get_typedef (m, cols [2]));
	}
}

void
dump_table_constant (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_CONSTANT];
	int i;
	fprintf (output, "Constant Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [4];
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		fprintf (output, "%d: Parent=0x%08x %s\n",
			 i, cols [2], get_constant (m, (ElementTypeEnum) cols [0], cols [3]));
	}
	
}

void
dump_table_property (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_PROPERTY];
	int i, j, pcount;
	const char *ptr;
	char flags[128];

	fprintf (output, "Property Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [3];
		char *type;
		int bsize;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		flags [0] = 0;
		if (cols [0] & 0x0200)
			strcat (flags, "special ");
		if (cols [0] & 0x0400)
			strcat (flags, "runtime ");
		if (cols [0] & 0x1000)
			strcat (flags, "hasdefault ");

		ptr = mono_metadata_blob_heap (m, cols [2]);
		ptr = mono_metadata_decode_blob_size (ptr, &bsize);
		/* ECMA claims 0x08 ... */
		if (*ptr != 0x28 && *ptr != 0x08)
				g_warning("incorrect signature in propert blob: 0x%x", *ptr);
		ptr++;
		ptr = mono_metadata_decode_value (ptr, &pcount);
		ptr = get_type (m, ptr, &type);
		fprintf (output, "%d: %s %s (",
			 i, type, mono_metadata_string_heap (m, cols [1]));
		g_free (type);

		for (j = 0; j < pcount; j++){
				ptr = get_param (m, ptr, &type);
				fprintf (output, "%s%s", j > 0? ", " : "",type);
				g_free (type);
		}
		fprintf (output, ") %s\n", flags);
	}
}

void
dump_table_event (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_EVENT];
	int i;
	fprintf (output, "Event Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [3];
		const char *name;
		char *type;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		name = mono_metadata_string_heap (m, cols [1]);
		type = get_typedef_or_ref (m, cols [2]);
		fprintf (output, "%d: %s %s %s\n", i, type, name,
			 cols [0] & 0x200 ? "specialname " : "");
		g_free (type);
	}
	
}

void
dump_table_file (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_FILE];
	int i;
	fprintf (output, "File Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [3];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		name = mono_metadata_string_heap (m, cols[1]);
		fprintf (output, "%d: %s %s\n", i, name, cols[2]&0x1?"nometadata":"containsmetadata");
	}
	
}

void
dump_table_moduleref (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_MODULEREF];
	int i;
	fprintf (output, "ModuleRef Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [1];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		name = mono_metadata_string_heap (m, cols[0]);
		fprintf (output, "%d: %s\n", i, name);
	}
	
}

void
dump_table_method (metadata_t *m)
{
	metadata_tableinfo_t *t = &m->tables [META_TABLE_METHOD];
	int i;
	fprintf (output, "Method Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [6];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		name = mono_metadata_string_heap (m, cols[3]);
		fprintf (output, "%d: %s\n", i, name);
	}
	
}

