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
dump_table_assembly (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];
	const char *ptr;
	int len;

	mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);
	fprintf (output, "Assembly Table\n");

	fprintf (output, "Name:          %s\n", mono_metadata_string_heap (m, cols [MONO_ASSEMBLY_NAME]));
	fprintf (output, "Hash Algoritm: 0x%08x\n", cols [MONO_ASSEMBLY_HASH_ALG]);
	fprintf (output, "Version:       %d.%d.%d.%d\n", cols [MONO_ASSEMBLY_MAJOR_VERSION], 
					cols [MONO_ASSEMBLY_MINOR_VERSION], 
					cols [MONO_ASSEMBLY_BUILD_NUMBER], 
					cols [MONO_ASSEMBLY_REV_NUMBER]);
	fprintf (output, "Flags:         0x%08x\n", cols [MONO_ASSEMBLY_FLAGS]);
	fprintf (output, "PublicKey:     BlobPtr (0x%08x)\n", cols [MONO_ASSEMBLY_PUBLIC_KEY]);

	ptr = mono_metadata_blob_heap (m, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
	len = mono_metadata_decode_value (ptr, &ptr);
	if (len > 0){
		fprintf (output, "\tDump:");
		hex_dump (ptr, 0, len);
		fprintf (output, "\n");
	} else
		fprintf (output, "\tZero sized public key\n");
	
	fprintf (output, "Culture:       %s\n", mono_metadata_string_heap (m, cols [MONO_ASSEMBLY_CULTURE]));
	fprintf (output, "\n");
}

void
dump_table_typeref (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEREF];
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
dump_table_typedef (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	int i;

	fprintf (output, "Typedef Table\n");
	
	for (i = 1; i <= t->rows; i++){
		char *s = get_typedef (m, i);
		guint32 cols [MONO_TYPEDEF_SIZE];

		mono_metadata_decode_row (&m->tables [MONO_TABLE_TYPEDEF], i - 1, cols, MONO_TYPEDEF_SIZE);

		fprintf (output, "%d: %s (flist=%d, mlist=%d, flags=0x%x, extends=0x%x)\n", i, s, 
					cols [MONO_TYPEDEF_FIELD_LIST], cols [MONO_TYPEDEF_METHOD_LIST],
					cols [MONO_TYPEDEF_FLAGS], cols [MONO_TYPEDEF_EXTENDS]);
		g_free (s);
	}
	fprintf (output, "\n");
}

void
dump_table_assemblyref (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_ASSEMBLYREF];
	int i;

	fprintf (output, "AssemblyRef Table\n");
	
	for (i = 0; i < t->rows; i++){
		const char *ptr;
		int len;
		guint32 cols [MONO_ASSEMBLYREF_SIZE];

		mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);
		fprintf (output, "%d: Version=%d.%d.%d.%d\n\tName=%s\n", i,
			 cols [MONO_ASSEMBLYREF_MAJOR_VERSION], 
			 cols [MONO_ASSEMBLYREF_MINOR_VERSION], 
			 cols [MONO_ASSEMBLYREF_BUILD_NUMBER], 
			 cols [MONO_ASSEMBLYREF_REV_NUMBER],
			 mono_metadata_string_heap (m, cols [MONO_ASSEMBLYREF_NAME]));
		ptr = mono_metadata_blob_heap (m, cols [MONO_ASSEMBLYREF_PUBLIC_KEY]);
		len = mono_metadata_decode_value (ptr, &ptr);
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
dump_table_param (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_PARAM];
	int i;

	fprintf (output, "Param Table\n");
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_PARAM_SIZE];

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		fprintf (output, "%d: 0x%04x %d %s\n",
			 i,
			 cols [MONO_PARAM_FLAGS], cols [MONO_PARAM_SEQUENCE], 
			 mono_metadata_string_heap (m, cols [MONO_PARAM_NAME]));
	}
	fprintf (output, "\n");
}

void
dump_table_field (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	MonoTableInfo *td = &m->tables [MONO_TABLE_TYPEDEF];
	int i, current_type;
	guint32 first_m, last_m;

	fprintf (output, "Field Table (1..%d)\n", t->rows);
	
	current_type = 1;
	last_m = first_m = 1;
	for (i = 1; i <= t->rows; i++){
		guint32 cols [MONO_FIELD_SIZE];
		char *sig, *flags;

		/*
		 * Find the next type.
		 */
		while (current_type <= td->rows && i >= (last_m = mono_metadata_decode_row_col (td, current_type - 1, MONO_TYPEDEF_FIELD_LIST))) {
			current_type++;
		}
		if (i == first_m) {
			fprintf (output, "########## %s.%s\n",
				mono_metadata_string_heap (m, mono_metadata_decode_row_col (td, current_type - 2, MONO_TYPEDEF_NAMESPACE)),
				mono_metadata_string_heap (m, mono_metadata_decode_row_col (td, current_type - 2, MONO_TYPEDEF_NAME)));
			first_m = last_m;
		}
		mono_metadata_decode_row (t, i - 1, cols, MONO_FIELD_SIZE);
		sig = get_field_signature (m, cols [MONO_FIELD_SIGNATURE]);
		flags = field_flags (cols [MONO_FIELD_FLAGS]);
		fprintf (output, "%d: %s %s: %s\n",
			 i,
			 sig,
			 mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]),
			 flags);
		g_free (sig);
		g_free (flags);
	}
	fprintf (output, "\n");
}

void
dump_table_memberref (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MEMBERREF];
	int i, kind, idx;
	char *ks, *x, *xx;

	fprintf (output, "MemberRef Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MEMBERREF_SIZE];

		mono_metadata_decode_row (t, i, cols, MONO_MEMBERREF_SIZE);
		
		kind = cols [MONO_MEMBERREF_CLASS] & 7;
		idx = cols [MONO_MEMBERREF_CLASS] >> 3;

		x = g_strdup ("UNHANDLED CASE");
		
		switch (kind){
		case 0:
			ks = "TypeDef"; break;
		case 1:
			ks = "TypeRef";
			xx = get_typeref (m, idx);
			x = g_strconcat (xx, ".", mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]), NULL);
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
			 mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]),
			 x ? x : "",
			 get_methodref_signature (m, cols [MONO_MEMBERREF_SIGNATURE], NULL));

		if (x)
			g_free (x);
	}
}

void
dump_table_class_layout (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_CLASSLAYOUT];
	int i;
	fprintf (output, "ClassLayout Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_CLASS_LAYOUT_SIZE];
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		fprintf (output, "%d: PackingSize=%d  ClassSize=%d  Parent=%s\n",
			 i, cols [MONO_CLASS_LAYOUT_PACKING_SIZE], 
			 cols [MONO_CLASS_LAYOUT_CLASS_SIZE], 
			 get_typedef (m, cols [MONO_CLASS_LAYOUT_PARENT]));
	}
}

void
dump_table_constant (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_CONSTANT];
	int i;
	fprintf (output, "Constant Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_CONSTANT_SIZE];
		
		mono_metadata_decode_row (t, i, cols, MONO_CONSTANT_SIZE);

		fprintf (output, "%d: Parent=0x%08x %s\n",
			 i, cols [MONO_CONSTANT_PARENT], 
			 get_constant (m, (MonoTypeEnum) cols [MONO_CONSTANT_TYPE], cols [MONO_CONSTANT_VALUE]));
	}
	
}

void
dump_table_property_map (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_PROPERTYMAP];
	int i;
	char *s;
	
	fprintf (output, "Property Map Table (1..%d)\n", t->rows);
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_PROPERTY_MAP_SIZE];
		
		mono_metadata_decode_row (t, i, cols, MONO_PROPERTY_MAP_SIZE);
		s = get_typedef (m, cols [MONO_PROPERTY_MAP_PARENT]);
		fprintf (output, "%d: %s %d\n", i + 1, s, cols [MONO_PROPERTY_MAP_PROPERTY_LIST]);
		g_free (s);
	}
}

void
dump_table_property (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_PROPERTY];
	int i, j, pcount;
	const char *ptr;
	char flags[128];

	fprintf (output, "Property Table (1..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_PROPERTY_SIZE];
		char *type;
		int bsize;
		int prop_flags;
		
		mono_metadata_decode_row (t, i, cols, MONO_PROPERTY_SIZE);
		flags [0] = 0;
		prop_flags = cols [MONO_PROPERTY_FLAGS];
		if (prop_flags & 0x0200)
			strcat (flags, "special ");
		if (prop_flags & 0x0400)
			strcat (flags, "runtime ");
		if (prop_flags & 0x1000)
			strcat (flags, "hasdefault ");

		ptr = mono_metadata_blob_heap (m, cols [MONO_PROPERTY_TYPE]);
		bsize = mono_metadata_decode_blob_size (ptr, &ptr);
		/* ECMA claims 0x08 ... */
		if (*ptr != 0x28 && *ptr != 0x08)
				g_warning("incorrect signature in propert blob: 0x%x", *ptr);
		ptr++;
		pcount = mono_metadata_decode_value (ptr, &ptr);
		ptr = get_type (m, ptr, &type);
		fprintf (output, "%d: %s %s (",
			 i + 1, type, mono_metadata_string_heap (m, cols [MONO_PROPERTY_NAME]));
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
dump_table_event (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_EVENT];
	int i;
	fprintf (output, "Event Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_EVENT_SIZE];
		const char *name;
		char *type;
		
		mono_metadata_decode_row (t, i, cols, MONO_EVENT_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_EVENT_NAME]);
		type = get_typedef_or_ref (m, cols [MONO_EVENT_TYPE]);
		fprintf (output, "%d: %s %s %s\n", i, type, name,
			 cols [MONO_EVENT_FLAGS] & 0x200 ? "specialname " : "");
		g_free (type);
	}
	
}

void
dump_table_file (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FILE];
	int i;
	fprintf (output, "File Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_FILE_SIZE];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, MONO_FILE_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_FILE_NAME]);
		fprintf (output, "%d: %s %s\n", i, name, 
				cols [MONO_FILE_FLAGS] & 0x1 ? "nometadata" : "containsmetadata");
	}
	
}

void
dump_table_moduleref (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MODULEREF];
	int i;
	fprintf (output, "ModuleRef Table (0..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MODULEREF_SIZE];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, MONO_MODULEREF_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_MODULEREF_NAME]);
		fprintf (output, "%d: %s\n", i, name);
	}
	
}

void
dump_table_method (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_METHOD];
	MonoTableInfo *td = &m->tables [MONO_TABLE_TYPEDEF];
	int i, current_type;
	guint32 first_m, last_m;
	fprintf (output, "Method Table (1..%d)\n", t->rows);

	current_type = 1;
	last_m = first_m = 1;
	for (i = 1; i <= t->rows; i++){
		guint32 cols [MONO_METHOD_SIZE];
		char *sig;
		const char *sigblob;
		MonoMethodSignature *method;

		/*
		 * Find the next type.
		 */
		while (current_type <= td->rows && i >= (last_m = mono_metadata_decode_row_col (td, current_type - 1, MONO_TYPEDEF_METHOD_LIST))) {
			current_type++;
		}
		if (i == first_m) {
			fprintf (output, "########## %s.%s\n",
				mono_metadata_string_heap (m, mono_metadata_decode_row_col (td, current_type - 2, MONO_TYPEDEF_NAMESPACE)),
				mono_metadata_string_heap (m, mono_metadata_decode_row_col (td, current_type - 2, MONO_TYPEDEF_NAME)));
			first_m = last_m;
		}
		mono_metadata_decode_row (t, i - 1, cols, MONO_METHOD_SIZE);
		sigblob = mono_metadata_blob_heap (m, cols [MONO_METHOD_SIGNATURE]);
		mono_metadata_decode_blob_size (sigblob, &sigblob);
		method = mono_metadata_parse_method_signature (m, 1, sigblob, &sigblob);
		sig = dis_stringify_method_signature (m, method, i);
		fprintf (output, "%d: %s\n", i, sig);
		g_free (sig);
		mono_metadata_free_method_signature (method);
	}
	
}

static map_t semantics_map [] = {
		{1, "setter"},
		{2, "getter"},
		{4, "other"},
		{8, "add-on"},
		{0x10, "remove-on"},
		{0x20, "fire"},
		{0, NULL},
};

void
dump_table_methodsem (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_METHODSEMANTICS];
	int i, is_property, index;
	const char *semantics;
	
	fprintf (output, "Method Semantics Table (1..%d)\n", t->rows);
	for (i = 1; i <= t->rows; i++){
		guint32 cols [MONO_METHOD_SEMA_SIZE];
		
		mono_metadata_decode_row (t, i - 1, cols, MONO_METHOD_SEMA_SIZE);
		semantics = flags (cols [MONO_METHOD_SEMA_SEMANTICS], semantics_map);
		is_property = cols [MONO_METHOD_SEMA_ASSOCIATION] & 1;
		index = cols [MONO_METHOD_SEMA_ASSOCIATION] >> 1;
		fprintf (output, "%d: %s method: %d %s %d\n", i, semantics,
						cols [MONO_METHOD_SEMA_METHOD] - 1, 
						is_property? "property" : "event",
						index);
	}
}

void 
dump_table_interfaceimpl (MonoMetadata *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_INTERFACEIMPL];
	int i;

	fprintf (output, "Interface Implementation Table (1..%d)\n", t->rows);
	for (i = 1; i <= t->rows; i++) {
		guint32 cols [MONO_INTERFACEIMPL_SIZE];
		
		mono_metadata_decode_row (t, i - 1, cols, MONO_INTERFACEIMPL_SIZE);
		fprintf (output, "%d: %s implements %s\n", i,
			get_typedef (m, cols [MONO_INTERFACEIMPL_CLASS]),
			get_typedef_or_ref (m, cols [MONO_INTERFACEIMPL_INTERFACE]));
	}
}
