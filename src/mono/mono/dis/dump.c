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
#include "mono/metadata/loader.h"
#include "mono/metadata/class.h"

void
dump_table_assembly (MonoImage *m)
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
dump_table_typeref (MonoImage *m)
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
dump_table_typedef (MonoImage *m)
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
dump_table_assemblyref (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_ASSEMBLYREF];
	int i;

	fprintf (output, "AssemblyRef Table\n");
	
	for (i = 0; i < t->rows; i++){
		const char *ptr;
		int len;
		guint32 cols [MONO_ASSEMBLYREF_SIZE];

		mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);
		fprintf (output, "%d: Version=%d.%d.%d.%d\n\tName=%s\n", i + 1,
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
dump_table_param (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_PARAM];
	int i;

	fprintf (output, "Param Table\n");
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_PARAM_SIZE];

		mono_metadata_decode_row (t, i, cols, CSIZE (cols));
		fprintf (output, "%d: 0x%04x %d %s\n",
			 i + 1,
			 cols [MONO_PARAM_FLAGS], cols [MONO_PARAM_SEQUENCE], 
			 mono_metadata_string_heap (m, cols [MONO_PARAM_NAME]));
	}
	fprintf (output, "\n");
}

void
dump_table_field (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	MonoTableInfo *td = &m->tables [MONO_TABLE_TYPEDEF];
	MonoTableInfo *fl = &m->tables [MONO_TABLE_FIELDLAYOUT];
	MonoTableInfo *rva = &m->tables [MONO_TABLE_FIELDRVA];
	int i, current_type, offset_row, rva_row;
	guint32 first_m, last_m;

	fprintf (output, "Field Table (1..%d)\n", t->rows);
	
	rva_row = offset_row = current_type = 1;
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
		if (offset_row <= fl->rows && (mono_metadata_decode_row_col (fl, offset_row - 1, MONO_FIELD_LAYOUT_FIELD) == i)) {
			fprintf (output, "\texplicit offset: %d\n", mono_metadata_decode_row_col (fl, offset_row - 1, MONO_FIELD_LAYOUT_OFFSET));
			offset_row ++;
		}
		if (rva_row <= rva->rows && (mono_metadata_decode_row_col (rva, rva_row - 1, MONO_FIELD_RVA_FIELD) == i)) {
			fprintf (output, "\trva: %d\n", mono_metadata_decode_row_col (rva, rva_row - 1, MONO_FIELD_RVA_RVA));
			rva_row ++;
		}
	}
	fprintf (output, "\n");
}

void
dump_table_memberref (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MEMBERREF];
	int i, kind, idx;
	char *ks, *x, *xx;
	char *sig;
	const char *blob;

	fprintf (output, "MemberRef Table (1..%d)\n", t->rows);

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
		blob = mono_metadata_blob_heap (m, cols [MONO_MEMBERREF_SIGNATURE]);
		mono_metadata_decode_blob_size (blob, &blob);
		if (*blob == 0x6) { /* it's a field */
			sig = get_field_signature (m, cols [MONO_MEMBERREF_SIGNATURE]);
		} else {
			sig = get_methodref_signature (m, cols [MONO_MEMBERREF_SIGNATURE], NULL);
		}
		fprintf (output, "%d: %s[%d] %s\n\tResolved: %s\n\tSignature: %s\n\t\n",
			 i + 1,
			 ks, idx,
			 mono_metadata_string_heap (m, cols [MONO_MEMBERREF_NAME]),
			 x ? x : "",
			 sig);

		if (x)
			g_free (x);
		g_free (sig);
	}
}

void
dump_table_class_layout (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_CLASSLAYOUT];
	int i;
	fprintf (output, "ClassLayout Table (1..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_CLASS_LAYOUT_SIZE];
		
		mono_metadata_decode_row (t, i, cols, CSIZE (cols));

		fprintf (output, "%d: PackingSize=%d  ClassSize=%d  Parent=%s\n",
			 i + 1, cols [MONO_CLASS_LAYOUT_PACKING_SIZE], 
			 cols [MONO_CLASS_LAYOUT_CLASS_SIZE], 
			 get_typedef (m, cols [MONO_CLASS_LAYOUT_PARENT]));
	}
}

void
dump_table_constant (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_CONSTANT];
	int i;
	const char *desc [] = {
		"Field",
		"Param",
		"Property",
		""
	};
	fprintf (output, "Constant Table (1..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_CONSTANT_SIZE];
		char *parent = desc [cols [MONO_CONSTANT_PARENT] & HASCONSTANT_MASK];
		
		mono_metadata_decode_row (t, i, cols, MONO_CONSTANT_SIZE);

		fprintf (output, "%d: Parent= %s: %d %s\n",
			 i + 1, parent, cols [MONO_CONSTANT_PARENT] >> HASCONSTANT_BITS, 
			 get_constant (m, (MonoTypeEnum) cols [MONO_CONSTANT_TYPE], cols [MONO_CONSTANT_VALUE]));
	}
	
}

void
dump_table_property_map (MonoImage *m)
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
dump_table_property (MonoImage *m)
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
dump_table_event (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_EVENT];
	int i;
	fprintf (output, "Event Table (1..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_EVENT_SIZE];
		const char *name;
		char *type;
		
		mono_metadata_decode_row (t, i, cols, MONO_EVENT_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_EVENT_NAME]);
		type = get_typedef_or_ref (m, cols [MONO_EVENT_TYPE]);
		fprintf (output, "%d: %s %s %s\n", i + 1, type, name,
			 cols [MONO_EVENT_FLAGS] & 0x200 ? "specialname " : "");
		g_free (type);
	}
	
}

void
dump_table_file (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FILE];
	int i;
	fprintf (output, "File Table (1..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_FILE_SIZE];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, MONO_FILE_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_FILE_NAME]);
		fprintf (output, "%d: %s %s\n", i + 1, name, 
				cols [MONO_FILE_FLAGS] & 0x1 ? "nometadata" : "containsmetadata");
	}
	
}

void
dump_table_moduleref (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MODULEREF];
	int i;
	fprintf (output, "ModuleRef Table (1..%d)\n", t->rows);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MODULEREF_SIZE];
		const char *name;
		
		mono_metadata_decode_row (t, i, cols, MONO_MODULEREF_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_MODULEREF_NAME]);
		fprintf (output, "%d: %s\n", i + 1, name);
	}
	
}

void
dump_table_method (MonoImage *m)
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
		fprintf (output, "%d: %s (param: %d)\n", i, sig, cols [MONO_METHOD_PARAMLIST]);
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
dump_table_methodsem (MonoImage *m)
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
dump_table_interfaceimpl (MonoImage *m)
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

static char*
has_cattr_get_table (MonoImage *m, guint32 val)
{
	guint32 t = val & CUSTOM_ATTR_MASK;
	guint32 index = val >> CUSTOM_ATTR_BITS;
	char *table;

	switch (t) {
	case CUSTOM_ATTR_METHODDEF:
		table = "MethodDef";
		break;
	case CUSTOM_ATTR_FIELDDEF:
		table = "FieldDef";
		break;
	case CUSTOM_ATTR_TYPEREF:
		table = "TypeRef";
		break;
	case CUSTOM_ATTR_TYPEDEF:
		table = "TypeDef";
		break;
	case CUSTOM_ATTR_PARAMDEF:
		table = "Param";
		break;
	case CUSTOM_ATTR_INTERFACE:
		table = "InterfaceImpl";
		break;
	case CUSTOM_ATTR_MEMBERREF:
		table = "MemberRef";
		break;
	case CUSTOM_ATTR_MODULE:
		table = "Module";
		break;
	case CUSTOM_ATTR_PERMISSION:
		table = "DeclSecurity?";
		break;
	case CUSTOM_ATTR_PROPERTY:
		table = "Property";
		break;
	case CUSTOM_ATTR_EVENT:
		table = "Event";
		break;
	case CUSTOM_ATTR_SIGNATURE:
		table = "StandAloneSignature";
		break;
	case CUSTOM_ATTR_MODULEREF:
		table = "ModuleRef";
		break;
	case CUSTOM_ATTR_TYPESPEC:
		table = "TypeSpec";
		break;
	case CUSTOM_ATTR_ASSEMBLY:
		table = "Assembly";
		break;
	case CUSTOM_ATTR_ASSEMBLYREF:
		table = "AssemblyRef";
		break;
	case CUSTOM_ATTR_FILE:
		table = "File";
		break;
	case CUSTOM_ATTR_EXP_TYPE:
		table = "ExportedType";
		break;
	case CUSTOM_ATTR_MANIFEST:
		table = "Manifest";
		break;
	default:
		table = "Unknown";
		break;
	}
	/*
	 * FIXME: we should decode the index into something more uman-friendly.
	 */
	return g_strdup_printf ("%s: %d", table, index);
}

static char*
custom_attr_params (MonoImage *m, MonoMethodSignature* sig, const char* value)
{
	int len, i, slen;
	GString *res;
	char *s;
	const char *p = value;

	len = mono_metadata_decode_value (p, &p);
	if (len < 2 || read16 (p) != 0x0001) /* Prolog */
		return g_strdup ("");

	res = g_string_new ("");
	for (i = 0; i < sig->param_count; ++i) {
		if (read16 (p) != 0x0001)
			g_warning ("no prolog in custom attr");
		p += 2;
		if (i != 0)
			g_string_append (res, ", ");
		switch (sig->params [i]->type) {
		case MONO_TYPE_BOOLEAN:
			g_string_sprintfa (res, "%s", *p?"true":"false");
			++p;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				/*
				 * FIXME: we should check the unrelying eum type...
				 */
				g_string_sprintfa (res, "0x%x", read32 (p));
				p += 4;
			} else {
				g_warning ("generic valutype not handled in custom attr value decoding");
			}
			break;
		case MONO_TYPE_STRING:
			slen = mono_metadata_decode_value (p, &p);
			g_string_append_c (res, '"');
			g_string_append (res, p);
			g_string_append_c (res, '"');
			p += slen;
			break;
		default:
			g_warning ("Type %02x not handled in custom attr value decoding", sig->params [i]->type);
			break;
		}
	}
	/*
	 * FIXME: handle named args only when all the type are handled in fixed args.
	 * slen = read16 (p);
	 */
	s = res->str;
	g_string_free (res, FALSE);
	return s;
}

void
dump_table_customattr (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_CUSTOMATTRIBUTE];
	int i;

	fprintf (output, "Custom Attributes Table (1..%d)\n", t->rows);
	for (i = 1; i <= t->rows; i++) {
		guint32 cols [MONO_CUSTOM_ATTR_SIZE];
		guint32 mtoken;
		char * desc;
		char *method;
		char *params;
		MonoMethod *meth;
		
		mono_metadata_decode_row (t, i - 1, cols, MONO_CUSTOM_ATTR_SIZE);
		desc = has_cattr_get_table (m, cols [MONO_CUSTOM_ATTR_PARENT]);
		mtoken = cols [MONO_CUSTOM_ATTR_TYPE] >> CUSTOM_ATTR_TYPE_BITS;
		switch (cols [MONO_CUSTOM_ATTR_TYPE] & CUSTOM_ATTR_TYPE_MASK) {
		case CUSTOM_ATTR_TYPE_METHODDEF:
			mtoken |= MONO_TOKEN_METHOD_DEF;
			break;
		case CUSTOM_ATTR_TYPE_MEMBERREF:
			mtoken |= MONO_TOKEN_MEMBER_REF;
			break;
		default:
			g_warning ("Unknown table for custom attr type %08x", cols [MONO_CUSTOM_ATTR_TYPE]);
			break;
		}
		method = get_method (m, mtoken);
		meth = mono_get_method (m, mtoken, NULL);
		params = custom_attr_params (m, meth->signature, mono_metadata_blob_heap (m, cols [MONO_CUSTOM_ATTR_VALUE]));
		fprintf (output, "%d: %s: %s [%s]\n", i, desc, method, params);
		g_free (desc);
		g_free (method);
		g_free (params);
	}
}

void
dump_table_nestedclass (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_NESTEDCLASS];
	guint32 cols [MONO_NESTED_CLASS_SIZE];
	int i;
	char *nested, *nesting;
	fprintf (output, "NestedClass Table (1..%d)\n", t->rows);

	for (i = 1; i <= t->rows; i++){
		mono_metadata_decode_row (t, i - 1, cols, MONO_NESTED_CLASS_SIZE);
		nested = get_typedef (m, cols [MONO_NESTED_CLASS_NESTED]);
		nesting = get_typedef (m, cols [MONO_NESTED_CLASS_ENCLOSING]);
		fprintf (output, "%d: %d %d: %s in %s\n", i,
				cols [MONO_NESTED_CLASS_NESTED], 
				cols [MONO_NESTED_CLASS_ENCLOSING], nested, nesting);
		g_free (nested);
		g_free (nesting);
	}
	
}

