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
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#include <config.h>
#include <stdio.h>
#include <string.h>
#include <glib.h>
#include <stdlib.h>
#include <errno.h>
#include <fcntl.h>
#include "meta.h"
#include "util.h"
#include "dump.h"
#include "get.h"
#include "dis-cil.h"
#include "declsec.h"
#include <mono/metadata/class-internals.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/assembly-internals.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/w32handle.h>
#include <mono/utils/bsearch.h>
#include <mono/utils/mono-counters.h>

static void     setup_filter          (MonoImage *image);
static gboolean should_include_type   (int idx);
static gboolean should_include_method (int idx);
static gboolean should_include_field  (int idx);

FILE *output;

/* True if you want to get a dump of the header data */
gboolean dump_header_data_p = FALSE;

/* True if you want to get forward declarations */
gboolean dump_forward_decls = FALSE;

/* True if you want to dump managed resources as files */
gboolean dump_managed_resources = FALSE;

gboolean substitute_with_mscorlib_p = FALSE;

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
dump_cattrs_list (GList *list,  const char *indent)
{
	GList *tmp;

	for (tmp = list; tmp; tmp = tmp->next) {
		fprintf (output, "%s%s\n", indent, (char*)tmp->data);
		g_free (tmp->data);
	}
	g_list_free (list);
}

static void
dump_cattrs (MonoImage *m, guint32 token, const char *indent)
{
	GList *list;

	list = dis_get_custom_attrs (m, token);
	dump_cattrs_list (list, indent);
}

static const char*
get_il_security_action (int val) 
{
	static char buf [32];

	switch (val) {
	case SECURITY_ACTION_DEMAND:
		return "demand";
	case SECURITY_ACTION_ASSERT:
		return "assert";
	case SECURITY_ACTION_DENY:
		return "deny";
	case SECURITY_ACTION_PERMITONLY:
		return "permitonly";
	case SECURITY_ACTION_LINKDEMAND:
		return "linkcheck";
	case SECURITY_ACTION_INHERITDEMAND:
		return "inheritcheck";
	case SECURITY_ACTION_REQMIN:
		return "reqmin";
	case SECURITY_ACTION_REQOPT:
		return "reqopt";
	case SECURITY_ACTION_REQREFUSE:
		return "reqrefuse";
	/* Special actions (for non CAS permissions) */
	case SECURITY_ACTION_NONCASDEMAND:
		return "noncasdemand";
	case SECURITY_ACTION_NONCASLINKDEMAND:
		return "noncaslinkdemand";
	case SECURITY_ACTION_NONCASINHERITANCE:
		return "noncasinheritance";
	/* Fx 2.0 actions (for both CAS and non-CAS permissions) */
	case SECURITY_ACTION_LINKDEMANDCHOICE:
		return "linkdemandor";
	case SECURITY_ACTION_INHERITDEMANDCHOICE:
		return "inheritancedemandor";
	case SECURITY_ACTION_DEMANDCHOICE:
		return "demandor";
	default:
		g_snprintf (buf, sizeof (buf), "0x%04X", val);
		return buf;
	}
}

#define OBJECT_TYPE_TYPEDEF	0
#define OBJECT_TYPE_METHODDEF	1
#define OBJECT_TYPE_ASSEMBLYDEF	2

static void
dump_declarative_security (MonoImage *m, guint32 objectType, guint32 token, const char *indent)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_DECLSECURITY];
	guint32 cols [MONO_DECL_SECURITY_SIZE];
	int i, len;
	guint32 idx;
	const char *blob, *action;
	
	for (i = 1; i <= t->rows; i++) {
		mono_metadata_decode_row (t, i - 1, cols, MONO_DECL_SECURITY_SIZE);
		blob = mono_metadata_blob_heap (m, cols [MONO_DECL_SECURITY_PERMISSIONSET]);
		len = mono_metadata_decode_blob_size (blob, &blob);
		action = get_il_security_action (cols [MONO_DECL_SECURITY_ACTION]);
		idx = cols [MONO_DECL_SECURITY_PARENT];
		if (((idx & MONO_HAS_DECL_SECURITY_MASK) == objectType) && ((idx >> MONO_HAS_DECL_SECURITY_BITS) == token)) {
			char *dump;
			if (blob [0] == MONO_DECLSEC_FORMAT_20) {
				/* 2.0 declarative security format */
				dump = dump_declsec_entry20 (m, blob, indent);
				fprintf (output, "%s.permissionset %s = %s\n", indent, action, dump);
			} else {
				/* 1.x declarative security metadata format */
				dump = data_dump (blob, len, indent);
				fprintf (output, "%s.permissionset %s = %s", indent, action, dump);
			}
			g_free (dump);
		}
	}
}

static char *
assembly_flags (guint32 f)
{
	if (f & ASSEMBLYREF_RETARGETABLE_FLAG)
		return g_strdup ("retargetable ");
	return g_strdup ("");
}

static void
dis_directive_assembly (MonoImage *m)
{
	MonoTableInfo *t  = &m->tables [MONO_TABLE_ASSEMBLY];
	guint32 cols [MONO_ASSEMBLY_SIZE];
	char *flags;
	
	if (t->base == NULL)
		return;

	mono_metadata_decode_row (t, 0, cols, MONO_ASSEMBLY_SIZE);
	flags = assembly_flags (cols [MONO_ASSEMBLY_FLAGS]);
	
	fprintf (output, ".assembly %s'%s'\n{\n",
		 flags, mono_metadata_string_heap (m, cols [MONO_ASSEMBLY_NAME]));
	dump_cattrs (m, MONO_TOKEN_ASSEMBLY | 1, "  ");
	dump_declarative_security (m, OBJECT_TYPE_ASSEMBLYDEF, 1, "  ");
	fprintf (output,
		 "  .hash algorithm 0x%08x\n"
		 "  .ver  %d:%d:%d:%d\n",
		 cols [MONO_ASSEMBLY_HASH_ALG],
		 cols [MONO_ASSEMBLY_MAJOR_VERSION], cols [MONO_ASSEMBLY_MINOR_VERSION], 
		 cols [MONO_ASSEMBLY_BUILD_NUMBER], cols [MONO_ASSEMBLY_REV_NUMBER]);
	if (cols [MONO_ASSEMBLY_CULTURE]){
		const char *locale = mono_metadata_string_heap (m, cols [MONO_ASSEMBLY_CULTURE]);
		glong items_read, items_written;
		gunichar2 *render = g_utf8_to_utf16 (locale, strlen (locale), &items_read, &items_written, NULL);
		char *dump = data_dump ((const char *) render, items_written * sizeof (gunichar2), "\t\t");
		fprintf (output, "  .locale %s\n", dump);
		g_free (dump);
		g_free (render);
		
	} if (cols [MONO_ASSEMBLY_PUBLIC_KEY]) {
		const char* b = mono_metadata_blob_heap (m, cols [MONO_ASSEMBLY_PUBLIC_KEY]);
		int len = mono_metadata_decode_blob_size (b, &b);
		char *dump = data_dump (b, len, "\t\t");
		fprintf (output, "  .publickey =%s", dump);
		g_free (dump);
	}
	fprintf (output, "}\n");
	
	g_free (flags);
}

static void
dis_directive_assemblyref (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_ASSEMBLYREF];
	guint32 cols [MONO_ASSEMBLYREF_SIZE];
	int i;
	
	if (t->base == NULL)
		return;

	for (i = 0; i < t->rows; i++){
		char *esc, *flags;

		mono_metadata_decode_row (t, i, cols, MONO_ASSEMBLYREF_SIZE);

		esc = get_escaped_name (mono_metadata_string_heap (m, cols [MONO_ASSEMBLYREF_NAME]));
		flags = assembly_flags (cols [MONO_ASSEMBLYREF_FLAGS]);
		
		fprintf (output,
			 ".assembly extern %s%s\n"
			 "{\n"
			 "  .ver %d:%d:%d:%d\n",
			 flags,
			 esc,
			 cols [MONO_ASSEMBLYREF_MAJOR_VERSION], cols [MONO_ASSEMBLYREF_MINOR_VERSION], 
			 cols [MONO_ASSEMBLYREF_BUILD_NUMBER], cols [MONO_ASSEMBLYREF_REV_NUMBER]
			);
		dump_cattrs (m, MONO_TOKEN_ASSEMBLY_REF | (i + 1), "  ");
		if (cols [MONO_ASSEMBLYREF_CULTURE]){
			fprintf (output, "  .locale %s\n", mono_metadata_string_heap (m, cols [MONO_ASSEMBLYREF_CULTURE]));
		}
		if (cols [MONO_ASSEMBLYREF_PUBLIC_KEY]){
			const char* b = mono_metadata_blob_heap (m, cols [MONO_ASSEMBLYREF_PUBLIC_KEY]);
			int len = mono_metadata_decode_blob_size (b, &b);
			char *dump = data_dump (b, len, "\t\t");
			fprintf (output, "  .publickeytoken =%s", dump);
			g_free (dump);
		}
		fprintf (output, "}\n");
		g_free (flags);
		g_free (esc);
	}
}

static void
dis_directive_module (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MODULE];
	int i;

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MODULE_SIZE];
		const char *name;
		char *guid, *ename;
		
		mono_metadata_decode_row (t, i, cols, MONO_MODULE_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_MODULE_NAME]);
		ename = get_escaped_name (name);
		guid = get_guid (m, cols [MONO_MODULE_MVID]);
		fprintf (output, ".module %s // GUID = %s\n\n", ename, guid);
		g_free (ename);

		dump_cattrs (m, MONO_TOKEN_MODULE | (i + 1), "");
	}
}

static void
dis_directive_moduleref (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MODULEREF];
	int i;

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MODULEREF_SIZE];
		
		mono_metadata_decode_row (t, i, cols, MONO_MODULEREF_SIZE);

		fprintf (output, ".module extern '%s'\n", mono_metadata_string_heap (m, cols [MONO_MODULEREF_NAME]));
	}
	
}

static void
dis_nt_header (MonoImage *m)
{
	MonoCLIImageInfo *image_info = (MonoCLIImageInfo *)m->image_info;
	if (image_info && image_info->cli_header.nt.pe_stack_reserve != 0x100000)
		fprintf (output, ".stackreserve 0x%x\n", image_info->cli_header.nt.pe_stack_reserve);
}

static void
dis_directive_file (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FILE];
	int i, j, len;
	guint32 entry_point;

	entry_point = mono_image_get_entry_point (m);

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_FILE_SIZE];
		const char *name, *hash;
		guint32 token;

		mono_metadata_decode_row (t, i, cols, MONO_FILE_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_FILE_NAME]);

		hash = mono_metadata_blob_heap (m, cols [MONO_FILE_HASH_VALUE]);
		len = mono_metadata_decode_blob_size (hash, &hash);

		fprintf (output, ".file %s%s .hash = (",
				cols [MONO_FILE_FLAGS] & FILE_CONTAINS_NO_METADATA ? "nometadata " : "", name);

		for (j = 0; j < len; ++j)
			fprintf (output, " %02X", hash [j] & 0xff);

		token = mono_metadata_make_token (MONO_TABLE_FILE, i + 1);
		fprintf (output, " )%s\n", (token == entry_point) ? " .entrypoint" : "");
	}
	
}

static void
dis_directive_mresource (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;

	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MANIFEST_SIZE];
		const char *name;
		guint32 impl, idx, name_token;

		mono_metadata_decode_row (t, i, cols, MONO_MANIFEST_SIZE);

		name = mono_metadata_string_heap (m, cols [MONO_MANIFEST_NAME]);

		fprintf (output, ".mresource %s '%s'\n", (cols [MONO_MANIFEST_FLAGS] & MANIFEST_RESOURCE_VISIBILITY_MASK) == (MANIFEST_RESOURCE_PUBLIC) ? "public" : "private", name);
		fprintf (output, "{\n");
		impl = cols [MONO_MANIFEST_IMPLEMENTATION];
		if (impl) {
			idx = impl >> MONO_IMPLEMENTATION_BITS;
			if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_FILE) {
				name_token = mono_metadata_decode_row_col (&m->tables [MONO_TABLE_FILE], idx - 1, MONO_FILE_NAME);

				fprintf (output, "    .file '%s' at 0x0\n", mono_metadata_string_heap (m, name_token));
			}
			if ((impl & MONO_IMPLEMENTATION_MASK) == MONO_IMPLEMENTATION_ASSEMBLYREF) {
				name_token = mono_metadata_decode_row_col (&m->tables [MONO_TABLE_ASSEMBLYREF], idx - 1, MONO_ASSEMBLYREF_NAME);
				fprintf (output, "    .assembly extern '%s'\n", mono_metadata_string_heap (m, name_token));
			}
		}				
		fprintf (output, "}\n");
	}
	
}

static dis_map_t visibility_map [] = {
	{ TYPE_ATTRIBUTE_NOT_PUBLIC,           "private " },
	{ TYPE_ATTRIBUTE_PUBLIC,               "public " },
	{ TYPE_ATTRIBUTE_NESTED_PUBLIC,        "nested public " },
	{ TYPE_ATTRIBUTE_NESTED_PRIVATE,       "nested private " },
	{ TYPE_ATTRIBUTE_NESTED_FAMILY,	       "nested family " },
	{ TYPE_ATTRIBUTE_NESTED_ASSEMBLY,      "nested assembly " },
	{ TYPE_ATTRIBUTE_NESTED_FAM_AND_ASSEM, "nested famandassem " },
	{ TYPE_ATTRIBUTE_NESTED_FAM_OR_ASSEM,  "nested famorassem " },
	{ 0, NULL }
};

static dis_map_t layout_map [] = {
	{ TYPE_ATTRIBUTE_AUTO_LAYOUT,          "auto " },
	{ TYPE_ATTRIBUTE_SEQUENTIAL_LAYOUT,    "sequential " },
	{ TYPE_ATTRIBUTE_EXPLICIT_LAYOUT,      "explicit " },
	{ 0, NULL }
};

static dis_map_t format_map [] = {
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
		strcat (buffer, "specialname ");
	if (flags & TYPE_ATTRIBUTE_IMPORT)
		strcat (buffer, "import ");
	if (flags & TYPE_ATTRIBUTE_SERIALIZABLE)
		strcat (buffer, "serializable ");
	if (flags & TYPE_ATTRIBUTE_BEFORE_FIELD_INIT)
		strcat (buffer, "beforefieldinit ");
	if (flags & TYPE_ATTRIBUTE_FORWARDER)
		strcat (buffer, "forwarder ");

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
dis_field_list (MonoImage *m, guint32 start, guint32 end, MonoGenericContainer *container)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELD];
	guint32 cols [MONO_FIELD_SIZE];
	char *esname;
	char rva_desc [32];
	guint32 rva;
	int i;

	if (end > t->rows + 1) {
		g_warning ("ERROR index out of range in fields");
		end = t->rows;
	}
			
	for (i = start; i < end; i++){
		char *sig, *flags, *attrs = NULL;
		char *marshal_str = NULL;
		guint32 field_offset = -1;

		if (!should_include_field (i + 1))
			continue;
		mono_metadata_decode_row (t, i, cols, MONO_FIELD_SIZE);
		sig = get_field_signature (m, cols [MONO_FIELD_SIGNATURE], container);
		flags = field_flags (cols [MONO_FIELD_FLAGS]);
		
		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_HAS_FIELD_MARSHAL) {
			const char *tp;
			MonoMarshalSpec *spec;
			
			tp = mono_metadata_get_marshal_info (m, i, TRUE);
			spec = mono_metadata_parse_marshal_spec (m, tp);
			marshal_str = dis_stringify_marshal_spec (spec);
		}

		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_HAS_FIELD_RVA) {
			mono_metadata_field_info (m, i, NULL, &rva, NULL);
			g_snprintf (rva_desc, sizeof (rva_desc), " at D_%08x", rva);
		} else {
			rva_desc [0] = 0;
		}
		
		mono_metadata_field_info (m, i, &field_offset, NULL, NULL);
		if (field_offset != -1)
			attrs = g_strdup_printf ("[%d]", field_offset);
		esname = get_escaped_name (mono_metadata_string_heap (m, cols [MONO_FIELD_NAME]));
		if (cols [MONO_FIELD_FLAGS] & FIELD_ATTRIBUTE_HAS_DEFAULT){
			char *lit;
			guint32 const_cols [MONO_CONSTANT_SIZE];
			guint32 crow;
			
			if ((crow = mono_metadata_get_constant_index (m, MONO_TOKEN_FIELD_DEF | (i+1), 0))) {
				mono_metadata_decode_row (&m->tables [MONO_TABLE_CONSTANT], crow-1, const_cols, MONO_CONSTANT_SIZE);
				lit = get_constant (m, (MonoTypeEnum)const_cols [MONO_CONSTANT_TYPE], const_cols [MONO_CONSTANT_VALUE]);
			} else {
				lit = g_strdup ("not found");
			}
			
			fprintf (output, "    .field %s%s%s %s = ",
				 flags, marshal_str ? marshal_str : " ", sig, esname);
			fprintf (output, "%s\n", lit);
			g_free (lit);
		} else
			fprintf (output, "    .field %s %s%s%s %s%s\n",
				 attrs? attrs: "", flags, marshal_str ? marshal_str : " ", sig, esname, rva_desc);
		g_free (attrs);
		g_free (flags);
		g_free (marshal_str);
		g_free (sig);
		g_free (esname);
		dump_cattrs (m, MONO_TOKEN_FIELD_DEF | (i + 1), "    ");
	}
}

static dis_map_t method_access_map [] = {
	{ METHOD_ATTRIBUTE_COMPILER_CONTROLLED, "privatescope " },
	{ METHOD_ATTRIBUTE_PRIVATE,             "private " },
	{ METHOD_ATTRIBUTE_FAM_AND_ASSEM,       "famandassem " },
	{ METHOD_ATTRIBUTE_ASSEM,               "assembly " },
	{ METHOD_ATTRIBUTE_FAMILY,              "family " },
	{ METHOD_ATTRIBUTE_FAM_OR_ASSEM,        "famorassem " },
	{ METHOD_ATTRIBUTE_PUBLIC,              "public " },
	{ 0, NULL }
};

static dis_map_t method_flags_map [] = {
	{ METHOD_ATTRIBUTE_STATIC,              "static " },
	{ METHOD_ATTRIBUTE_FINAL,               "final " },
	{ METHOD_ATTRIBUTE_VIRTUAL,             "virtual " },
	{ METHOD_ATTRIBUTE_HIDE_BY_SIG,         "hidebysig " },
	{ METHOD_ATTRIBUTE_VTABLE_LAYOUT_MASK,  "newslot " },
	{ METHOD_ATTRIBUTE_ABSTRACT,            "abstract " },
	{ METHOD_ATTRIBUTE_SPECIAL_NAME,        "specialname " },
	{ METHOD_ATTRIBUTE_RT_SPECIAL_NAME,     "rtspecialname " },
	{ METHOD_ATTRIBUTE_UNMANAGED_EXPORT,    "export " },
/* MS ilasm doesn't compile this statement - is must be added automagically when permissionset are present */
	{ METHOD_ATTRIBUTE_HAS_SECURITY,        "" /*"hassecurity"*/ },
	{ METHOD_ATTRIBUTE_REQUIRE_SEC_OBJECT,  "requiresecobj " },
	{ METHOD_ATTRIBUTE_PINVOKE_IMPL,        "pinvokeimpl " }, 
	{ METHOD_ATTRIBUTE_STRICT,	            "strict " }, 
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
	int rest = f & ~access;
	char *s;
	
	g_string_append (str, map (access, method_access_map));
	g_string_append (str, flags (rest, method_flags_map));

	s = str->str;
	g_string_free (str, FALSE);

	return s;
}

static dis_map_t pinvoke_flags_map [] = {
	{ PINVOKE_ATTRIBUTE_NO_MANGLE ,            "nomangle " },
	{ PINVOKE_ATTRIBUTE_SUPPORTS_LAST_ERROR,   "lasterr " },
	{ PINVOKE_ATTRIBUTE_BEST_FIT_ENABLED,      "bestfit:on " },
	{ PINVOKE_ATTRIBUTE_BEST_FIT_DISABLED,      "bestfit:off " },
	{ PINVOKE_ATTRIBUTE_THROW_ON_UNMAPPABLE_ENABLED, "charmaperror:on " },
	{ PINVOKE_ATTRIBUTE_THROW_ON_UNMAPPABLE_DISABLED, "charmaperror:off " },
	{ 0, NULL }
};

static dis_map_t pinvoke_call_conv_map [] = {
	{ PINVOKE_ATTRIBUTE_CALL_CONV_WINAPI,      "winapi " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_CDECL,       "cdecl " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_STDCALL,     "stdcall " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_THISCALL,    "thiscall " },
	{ PINVOKE_ATTRIBUTE_CALL_CONV_FASTCALL,    "fastcall " },
	{ 0, "" },
	{ -1, NULL }
};

static dis_map_t pinvoke_char_set_map [] = {
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
	int rest = f & ~(cset | cconv);
	char *s;

	g_string_append (str, map (cset, pinvoke_char_set_map));
	g_string_append (str, map (cconv, pinvoke_call_conv_map));
	g_string_append (str, flags (rest, pinvoke_flags_map));

	s = g_strdup(str->str);
	g_string_free (str, FALSE);

	return s;
}

static void
dis_locals (MonoImage *m, MonoMethodHeader *mh, const char *ptr) 
{
	int i;

	if (show_tokens) {
		unsigned char flags = *(const unsigned char *) ptr;
		unsigned char format = flags & METHOD_HEADER_FORMAT_MASK;
		guint16 fat_flags;
		guint32 local_var_sig_tok, init_locals;

		g_assert (format == METHOD_HEADER_FAT_FORMAT);
		fat_flags = read16 (ptr);
		ptr += 2;
		/* max_stack = read16 (ptr); */
		ptr += 2;
		/* code_size = read32 (ptr); */
		ptr += 4;
		local_var_sig_tok = read32 (ptr);
		ptr += 4;

		if (fat_flags & METHOD_HEADER_INIT_LOCALS)
			init_locals = 1;
		else
			init_locals = 0;

		fprintf(output, "\t.locals /*%08x*/ %s(\n",
			local_var_sig_tok, init_locals ? "init " : "");
	} else
		fprintf(output, "\t.locals %s(\n", mh->init_locals ? "init " : "");

	for (i=0; i < mh->num_locals; ++i) {
		char * desc;
		if (i)
			fprintf(output, ",\n");
		/* print also byref and pinned attributes */
		desc = dis_stringify_type (m, mh->locals[i], TRUE);
		fprintf(output, "\t\t%s\tV_%d", desc, i);
		g_free(desc);
	}
	fprintf(output, ")\n");
}

static void
dis_code (MonoImage *m, guint32 token, guint32 rva, MonoGenericContainer *container)
{
	MonoError error;
	MonoMethodHeader *mh;
	const char *ptr = mono_image_rva_map (m, rva);
	const char *loc;
	gchar *override;
	guint32 entry_point;

	if (rva == 0)
		return;

	override = get_method_override (m, token, container);
	if (override) {
		fprintf (output, "\t.override %s\n", override);
		g_free (override);
	}

	mh = mono_metadata_parse_mh_full (m, container, ptr, &error);
	entry_point = mono_image_get_entry_point (m);
	if (entry_point && mono_metadata_token_index (entry_point) && mono_metadata_token_table (entry_point) == MONO_TABLE_METHOD) {
		loc = mono_metadata_locate_token (m, entry_point);
		if (rva == read32 (loc))
			fprintf (output, "\t.entrypoint\n");
	}
	
	if (mh) {
		fprintf (output, "\t// Code size %d (0x%x)\n", mh->code_size, mh->code_size);
		fprintf (output, "\t.maxstack %d\n", mh->max_stack);
		if (mh->num_locals)
			dis_locals (m, mh, ptr);
		disassemble_cil (m, mh, container);
/*
  hex_dump (mh->code, 0, mh->code_size);
  printf ("\nAfter the code\n");
  hex_dump (mh->code + mh->code_size, 0, 64);
*/
		mono_metadata_free_mh (mh);
	} else {
		mono_error_cleanup (&error);
	}
}

static char *
pinvoke_info (MonoImage *m, guint32 mindex)
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
				
			return g_strdup_printf ("(\"%s\" as \"%s\" %s)", scope, import,
						flags);
			g_free (flags);
		}
	}

	return NULL;
}

/*
 * dump_cattrs_for_type_params
 *
 * @m: 
 * @token: TypeOrMethodDef token, owner for GenericParam
 *
 * Dumps the custom attributes for @token's type parameters
 */
static void
dump_cattrs_for_type_params (MonoImage *m, guint32 token, const char *indent)
{
	MonoTableInfo *tdef  = &m->tables [MONO_TABLE_GENERICPARAM];
	guint32 cols [MONO_GENERICPARAM_SIZE];
	guint32 owner = 0, i;
	GList *list = NULL;

	if (! (i = mono_metadata_get_generic_param_row (m, token, &owner)))
		return;

	mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	do {
		list = dis_get_custom_attrs (m, mono_metadata_make_token (MONO_TABLE_GENERICPARAM, i));
		if (list) {
			fprintf (output, "%s.param type %s\n", indent, mono_metadata_string_heap (m, cols [MONO_GENERICPARAM_NAME]));
			dump_cattrs_list (list, indent);
		}

		if (++i > tdef->rows)
			break;
		mono_metadata_decode_row (tdef, i - 1, cols, MONO_GENERICPARAM_SIZE);
	} while (cols [MONO_GENERICPARAM_OWNER] == owner);
}

static void
dump_cattrs_for_method_params (MonoImage *m, guint32 midx, MonoMethodSignature *sig) {
	MonoTableInfo *methodt;
	MonoTableInfo *paramt;
	guint param_index, lastp, i;

	methodt = &m->tables [MONO_TABLE_METHOD];
	paramt = &m->tables [MONO_TABLE_PARAM];
	param_index = mono_metadata_decode_row_col (methodt, midx, MONO_METHOD_PARAMLIST);
	if (midx + 1 < methodt->rows)
		lastp = mono_metadata_decode_row_col (methodt, midx + 1, MONO_METHOD_PARAMLIST);
	else
		lastp = paramt->rows + 1;
	for (i = param_index; i < lastp; ++i) {
	  	char *lit;
	  	int crow;
		guint32 param_cols [MONO_PARAM_SIZE];
		GList *list;
		
		list = dis_get_custom_attrs (m, MONO_TOKEN_PARAM_DEF | i);

		mono_metadata_decode_row (paramt, i-1, param_cols, MONO_PARAM_SIZE);
		if (!(param_cols[MONO_PARAM_FLAGS] & PARAM_ATTRIBUTE_HAS_DEFAULT)) {
			if(list != NULL)
				fprintf (output, "\t.param [%d]\n", param_cols[MONO_PARAM_SEQUENCE]);
		} else {
			fprintf (output, "\t.param [%d] = ", param_cols[MONO_PARAM_SEQUENCE]);
		  
			if ((crow = mono_metadata_get_constant_index(m, MONO_TOKEN_PARAM_DEF | i, 0))) {
				guint32 const_cols [MONO_CONSTANT_SIZE];
				mono_metadata_decode_row( &m->tables[MONO_TABLE_CONSTANT], crow-1, const_cols, MONO_CONSTANT_SIZE);
				lit = get_constant (m, (MonoTypeEnum)const_cols [MONO_CONSTANT_TYPE], const_cols [MONO_CONSTANT_VALUE]);
			}
			else {
				lit = g_strdup ("not found");
			}
		  fprintf(output, "%s\n", lit);
		  g_free(lit);
		}
		dump_cattrs_list (list, "\t");
	}
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
dis_method_list (const char *klass_name, MonoImage *m, guint32 start, guint32 end, MonoGenericContainer *type_container)
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
		MonoError error;
		MonoMethodSignature *ms;
		MonoGenericContainer *container;
		char *flags, *impl_flags;
		const char *sig, *method_name;
		char *sig_str;
		guint32 token;

		if (!should_include_method (i + 1))
			continue;
		mono_metadata_decode_row (t, i, cols, MONO_METHOD_SIZE);

		flags = method_flags (cols [MONO_METHOD_FLAGS]);
		impl_flags = get_method_impl_flags (cols [MONO_METHOD_IMPLFLAGS]);

		sig = mono_metadata_blob_heap (m, cols [MONO_METHOD_SIGNATURE]);
		mono_metadata_decode_blob_size (sig, &sig);

		container = mono_metadata_load_generic_params (m, MONO_TOKEN_METHOD_DEF | (i + 1), type_container);
		if (container) {
			MonoError error;
			mono_metadata_load_generic_param_constraints_checked (m, MONO_TOKEN_METHOD_DEF | (i + 1), container, &error);
			g_assert (mono_error_ok (&error)); /*FIXME don't swallow the error message*/
		} else {
			container = type_container;
		}

		ms = mono_metadata_parse_method_signature_full (m, container, i + 1, sig, &sig, &error);
		if (ms != NULL){
			sig_str = dis_stringify_method_signature (m, ms, i + 1, container, FALSE);
			method_name = mono_metadata_string_heap (m, cols [MONO_METHOD_NAME]);
		} else {
			sig_str = NULL;
			method_name = g_strdup ("<NULL METHOD SIGNATURE>");
			mono_error_cleanup (&error);
		}

		fprintf (output, "    // method line %d\n", i + 1);
		fprintf (output, "    .method %s", flags);

		if ((cols [MONO_METHOD_FLAGS] & METHOD_ATTRIBUTE_PINVOKE_IMPL) && (cols [MONO_METHOD_RVA] == 0)) {
			gchar *pi = pinvoke_info (m, i);
			if (pi) {
				fprintf (output, "%s", pi);
				g_free (pi);
			}
		}

		fprintf (output, "\n           %s", sig_str);
		fprintf (output, " %s\n", impl_flags);
		g_free (flags);
		g_free (impl_flags);

		token = MONO_TOKEN_METHOD_DEF | (i + 1);
		
		fprintf (output, "    {\n");
		dump_cattrs (m, token, "        ");
		dump_cattrs_for_type_params (m, MONO_TOKEN_METHOD_DEF | (i + 1), "        ");
		dump_cattrs_for_method_params (m, i, ms);

		fprintf (output, "        // Method begins at RVA 0x%x\n", cols [MONO_METHOD_RVA]);
		dump_declarative_security (m, OBJECT_TYPE_METHODDEF, i + 1, "        ");
		if (cols [MONO_METHOD_IMPLFLAGS] & METHOD_IMPL_ATTRIBUTE_NATIVE)
			fprintf (output, "          // Disassembly of native methods is not supported\n");
		else
			dis_code (m, token, cols [MONO_METHOD_RVA], container);
		if (klass_name)
			fprintf (output, "    } // end of method %s::%s\n\n", klass_name, method_name);
		else
			fprintf (output, "    } // end of global method %s\n\n", method_name);
		mono_metadata_free_method_signature (ms);
		g_free (sig_str);
	}
}

typedef struct {
	MonoTableInfo *t;
	guint32 col_idx;
	guint32 idx;
	guint32 result;
} plocator_t;

static int
table_locator (const void *a, const void *b)
{
	plocator_t *loc = (plocator_t *) a;
	const char *bb = (const char *) b;
	guint32 table_index = (bb - loc->t->base) / loc->t->row_size;
	guint32 col;
	
	col = mono_metadata_decode_row_col (loc->t, table_index, loc->col_idx);

	if (loc->idx == col) {
		loc->result = table_index;
		return 0;
	}
	if (loc->idx < col)
		return -1;
	else 
		return 1;
}

static void
dis_property_methods (MonoImage *m, guint32 prop, MonoGenericContainer *container)
{
	guint start, end;
	MonoTableInfo *msemt = &m->tables [MONO_TABLE_METHODSEMANTICS];
	guint32 cols [MONO_METHOD_SEMA_SIZE];
	char *sig;
	const char *type[] = {NULL, ".set", ".get", NULL, ".other"};

	start = mono_metadata_methods_from_property (m, prop, &end);
	for (; start < end; ++start) {
		mono_metadata_decode_row (msemt, start, cols, MONO_METHOD_SEMA_SIZE);
		if (!should_include_method (cols [MONO_METHOD_SEMA_METHOD]))
			continue;
		sig = dis_stringify_method_signature_full (m, NULL, cols [MONO_METHOD_SEMA_METHOD], container, TRUE, FALSE);
		fprintf (output, "\t\t%s %s\n", type [cols [MONO_METHOD_SEMA_SEMANTICS]], sig);
		g_free (sig);
	}
}
static char*
dis_property_signature (MonoImage *m, guint32 prop_idx, MonoGenericContainer *container)
{
	MonoError error;
	MonoTableInfo *propt = &m->tables [MONO_TABLE_PROPERTY];
	const char *ptr;
	guint32 pcount, i;
	guint32 cols [MONO_PROPERTY_SIZE];
	MonoType *type;
	MonoType *param;
	char *blurb, *qk;
	const char *name;
	int prop_flags;
	GString *res = g_string_new ("");

	mono_metadata_decode_row (propt, prop_idx, cols, MONO_PROPERTY_SIZE);
	name = mono_metadata_string_heap (m, cols [MONO_PROPERTY_NAME]);
	prop_flags = cols [MONO_PROPERTY_FLAGS];
	ptr = mono_metadata_blob_heap (m, cols [MONO_PROPERTY_TYPE]);
	mono_metadata_decode_blob_size (ptr, &ptr);
	if (!(*ptr & 0x08))
		g_warning("incorrect signature in property blob: 0x%x", *ptr);
	if (*ptr & 0x20)
		g_string_append (res, "instance ");
	ptr++;
	pcount = mono_metadata_decode_value (ptr, &ptr);
	type = mono_metadata_parse_type_checked (m, container, 0, FALSE, ptr, &ptr, &error);
	if (type) {
		blurb = dis_stringify_type (m, type, TRUE);
	} else {
		blurb = g_strdup_printf ("Invalid type due to %s", mono_error_get_message (&error));
		mono_error_cleanup (&error);
	}
	if (prop_flags & 0x0200)
		g_string_append (res, "specialname ");
	if (prop_flags & 0x0400)
		g_string_append (res, "rtspecialname ");
	qk = get_escaped_name (name);
	g_string_append_printf (res, "%s %s (", blurb, qk);
	g_free (qk);
	g_free (blurb);
	for (i = 0; i < pcount; i++) {
		if (i)
			g_string_append (res, ", ");
		param = mono_metadata_parse_type_checked (m, container, 0, FALSE, ptr, &ptr, &error);
		if (type) {
			blurb = dis_stringify_param (m, param);
		} else {
			blurb = g_strdup_printf ("Invalid type due to %s", mono_error_get_message (&error));
			mono_error_cleanup (&error);
		}

		g_string_append (res, blurb);
		g_free (blurb);
	}
	g_string_append_c (res, ')');
	blurb = res->str;
	g_string_free (res, FALSE);
	return blurb;

}

static void
dis_property_list (MonoImage *m, guint32 typedef_row, MonoGenericContainer *container)
{
	guint start, end, i;
	start = mono_metadata_properties_from_typedef (m, typedef_row, &end);

	for (i = start; i < end; ++i) {
		char *sig = dis_property_signature (m, i, container);
		fprintf (output, "\t.property %s\n\t{\n", sig);
		dump_cattrs (m, MONO_TOKEN_PROPERTY | (i + 1), "\t\t");
		dis_property_methods (m, i, container);
		fprintf (output, "\t}\n");
		g_free (sig);
	}
}

static char*
dis_event_signature (MonoImage *m, guint32 event_idx, MonoGenericContainer *container)
{
	MonoTableInfo *et = &m->tables [MONO_TABLE_EVENT];
	char *type, *result, *esname;
	guint32 cols [MONO_EVENT_SIZE];
	int event_flags;
	GString *res = g_string_new ("");
	
	mono_metadata_decode_row (et, event_idx, cols, MONO_EVENT_SIZE);
	esname = get_escaped_name (mono_metadata_string_heap (m, cols [MONO_EVENT_NAME]));
	type = get_typedef_or_ref (m, cols [MONO_EVENT_TYPE], container);
	event_flags = cols [MONO_EVENT_FLAGS];

	if (event_flags & 0x0200)
		g_string_append (res, "specialname ");
	if (event_flags & 0x0400)
		g_string_append (res, "rtspecialname ");
	g_string_append_printf (res, "%s %s", type, esname);

	g_free (type);
	g_free (esname);
	result = res->str;
	g_string_free (res, FALSE);
	return result;
}

static void
dis_event_methods (MonoImage *m, guint32 event, MonoGenericContainer *container)
{
	guint start, end;
	MonoTableInfo *msemt = &m->tables [MONO_TABLE_METHODSEMANTICS];
	guint32 cols [MONO_METHOD_SEMA_SIZE];
	char *sig;
	const char *type = "";

	start = mono_metadata_methods_from_event (m, event, &end);
	for (; start < end; ++start) {
		mono_metadata_decode_row (msemt, start, cols, MONO_METHOD_SEMA_SIZE);
		if (!should_include_method (cols [MONO_METHOD_SEMA_METHOD]))
			continue;
		sig = dis_stringify_method_signature_full (m, NULL, cols [MONO_METHOD_SEMA_METHOD], container, TRUE, FALSE);
		switch (cols [MONO_METHOD_SEMA_SEMANTICS]) {
		case METHOD_SEMANTIC_OTHER:
			type = ".other"; break;
		case METHOD_SEMANTIC_ADD_ON:
			type = ".addon"; break;
		case METHOD_SEMANTIC_REMOVE_ON:
			type = ".removeon"; break;
		case METHOD_SEMANTIC_FIRE:
			type = ".fire"; break;
		default:
			break;
		}
		fprintf (output, "\t\t%s %s\n", type, sig);
		g_free (sig);
	}
}

static void
dis_event_list (MonoImage *m, guint32 typedef_row, MonoGenericContainer *container)
{
	guint start, end, i;
	start = mono_metadata_events_from_typedef (m, typedef_row, &end);

	for (i = start; i < end; ++i) {
		char *sig = dis_event_signature (m, i, container);
		fprintf (output, "\t.event %s\n\t{\n", sig);
		dump_cattrs (m, MONO_TOKEN_EVENT | (i + 1), "\t\t");
		dis_event_methods (m, i, container);
		fprintf (output, "\t}\n");
		g_free (sig);
	}
}

static void
dis_interfaces (MonoImage *m, guint32 typedef_row, MonoGenericContainer *container)
{
	plocator_t loc;
	guint start;
	gboolean first_interface = 1;
	guint32 cols [MONO_INTERFACEIMPL_SIZE];
	char *intf;
	MonoTableInfo *table = &m->tables [MONO_TABLE_INTERFACEIMPL];

	if (!table->base)
		return;

	loc.t = table;
	loc.col_idx = MONO_INTERFACEIMPL_CLASS;
	loc.idx = typedef_row;

	if (!mono_binary_search (&loc, table->base, table->rows, table->row_size, table_locator))
		return;

	start = loc.result;
	/*
	 * We may end up in the middle of the rows... 
	 */
	while (start > 0) {
		if (loc.idx == mono_metadata_decode_row_col (table, start - 1, MONO_INTERFACEIMPL_CLASS))
			start--;
		else
			break;
	}
	while (start < table->rows) {
		mono_metadata_decode_row (table, start, cols, MONO_INTERFACEIMPL_SIZE);
		if (cols [MONO_INTERFACEIMPL_CLASS] != loc.idx)
			break;
		intf = get_typedef_or_ref (m, cols [MONO_INTERFACEIMPL_INTERFACE], container);
		if (first_interface) {
			fprintf (output, "  \timplements %s", intf);
			first_interface = 0;
		} else {
			fprintf (output, ", %s", intf);
		}
		g_free (intf);
		++start;
	}
}

/**
 * dis_type:
 * @m: metadata context
 * @n: index of type to disassemble
 * @is_nested: nested type ?
 * @forward: forward declarations?
 *
 * Disassembles the type whose index in the TypeDef table is @n.
 */
static void
dis_type (MonoImage *m, int n, int is_nested, int forward)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	const char *name, *nspace;
	char *esname, *param;
	MonoGenericContainer *container;
	guint32 packing_size, class_size;
	gboolean next_is_valid, last;
	guint32 nested;

	if (!should_include_type (n + 1))
		return;
	mono_metadata_decode_row (t, n, cols, MONO_TYPEDEF_SIZE);

	if (t->rows > n + 1) {
		mono_metadata_decode_row (t, n + 1, cols_next, MONO_TYPEDEF_SIZE);
		next_is_valid = 1;
	} else
		next_is_valid = 0;

	name = mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAME]);
	nspace = mono_metadata_string_heap (m, cols [MONO_TYPEDEF_NAMESPACE]);
	if (*nspace && !is_nested) {
		char *esnspace;
		esnspace = get_escaped_name (nspace);
		fprintf (output, ".namespace %s\n{\n", esnspace);
		g_free (esnspace);
	}

	container = mono_metadata_load_generic_params (m, MONO_TOKEN_TYPE_DEF | (n + 1), NULL);
	if (container) {
		MonoError error;
		mono_metadata_load_generic_param_constraints_checked (m, MONO_TOKEN_TYPE_DEF | (n + 1), container, &error);
		g_assert (mono_error_ok (&error)); /*FIXME don't swallow the error message*/
	}

	esname = get_escaped_name (name);
	if ((cols [MONO_TYPEDEF_FLAGS] & TYPE_ATTRIBUTE_CLASS_SEMANTIC_MASK) == TYPE_ATTRIBUTE_CLASS){
		fprintf (output, "  .class %s%s", typedef_flags (cols [MONO_TYPEDEF_FLAGS]), esname);
		
                param = get_generic_param (m, container);
		if (param) {
			fprintf (output, "%s", param);
			g_free (param);
		}
                fprintf (output, "\n");
		if (cols [MONO_TYPEDEF_EXTENDS]) {
			char *base = get_typedef_or_ref (m, cols [MONO_TYPEDEF_EXTENDS], container);
			fprintf (output, "  \textends %s\n", base);
			g_free (base);
		}
	} else {
		fprintf (output, "  .class interface %s%s", typedef_flags (cols [MONO_TYPEDEF_FLAGS]), esname);

                param = get_generic_param (m, container);
		if (param) {
			fprintf (output, "%s", param);
			g_free (param);
		}
		fprintf (output, "\n");
	}

	g_free (esname);
	dis_interfaces (m, n + 1, container);
	fprintf (output, "  {\n");
        if (!forward) {
        	dump_cattrs (m, MONO_TOKEN_TYPE_DEF | (n + 1), "    ");
        	dump_cattrs_for_type_params (m, MONO_TOKEN_TYPE_DEF | (n + 1), "    ");
	        dump_declarative_security (m, OBJECT_TYPE_TYPEDEF, (n + 1), "    ");

        	if (mono_metadata_packing_from_typedef (m, n + 1, &packing_size, &class_size)) {
	        	fprintf (output, "    .pack %d\n", packing_size);
	        	fprintf (output, "    .size %d\n", class_size);
        	}
        	/*
	         * The value in the table is always valid, we know we have fields
        	 * if the value stored is different than the next record.
        	 */
        
        	if (next_is_valid)
	        	last = cols_next [MONO_TYPEDEF_FIELD_LIST] - 1;
        	else
	        	last = m->tables [MONO_TABLE_FIELD].rows;
			
        	if (cols [MONO_TYPEDEF_FIELD_LIST] && cols [MONO_TYPEDEF_FIELD_LIST] <= m->tables [MONO_TABLE_FIELD].rows)
		        dis_field_list (m, cols [MONO_TYPEDEF_FIELD_LIST] - 1, last, container);
        	fprintf (output, "\n");

        	if (next_is_valid)
	        	last = cols_next [MONO_TYPEDEF_METHOD_LIST] - 1;
        	else
	        	last = m->tables [MONO_TABLE_METHOD].rows;
	
        	if (cols [MONO_TYPEDEF_METHOD_LIST] && cols [MONO_TYPEDEF_METHOD_LIST] <= m->tables [MONO_TABLE_METHOD].rows)
	        	dis_method_list (name, m, cols [MONO_TYPEDEF_METHOD_LIST] - 1, last, container);

        	dis_property_list (m, n, container);
	        dis_event_list (m, n, container);
        }

	t = &m->tables [MONO_TABLE_NESTEDCLASS];
	nested = mono_metadata_nesting_typedef (m, n + 1, 1);
	while (nested) {
		dis_type (m, mono_metadata_decode_row_col (t, nested - 1, MONO_NESTED_CLASS_NESTED) - 1, 1, forward);
		nested = mono_metadata_nesting_typedef (m, n + 1, nested + 1);
	}
	
	fprintf (output, "  } // end of class %s%s%s\n", nspace, *nspace? ".": "", name);
	if (*nspace && !is_nested)
		fprintf (output, "}\n");
	fprintf (output, "\n");
}


/**
 * dis_globals
 * @m: metadata context
 *
 * disassembles all the global fields and methods
 */
static void
dis_globals (MonoImage *m)
{
        MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	guint32 cols [MONO_TYPEDEF_SIZE];
	guint32 cols_next [MONO_TYPEDEF_SIZE];
	gboolean next_is_valid, last;

        mono_metadata_decode_row (t, 0, cols, MONO_TYPEDEF_SIZE);

	if (t->rows > 1) {
		mono_metadata_decode_row (t, 1, cols_next, MONO_TYPEDEF_SIZE);
		next_is_valid = 1;
	} else
		next_is_valid = 0;
        
	/*
	 * The value in the table is always valid, we know we have fields
	 * if the value stored is different than the next record.
	 */

	if (next_is_valid)
		last = cols_next [MONO_TYPEDEF_FIELD_LIST] - 1;
	else
		last = m->tables [MONO_TABLE_FIELD].rows;
			
	if (cols [MONO_TYPEDEF_FIELD_LIST] && cols [MONO_TYPEDEF_FIELD_LIST] <= m->tables [MONO_TABLE_FIELD].rows)
		dis_field_list (m, cols [MONO_TYPEDEF_FIELD_LIST] - 1, last, NULL);
	fprintf (output, "\n");

	if (next_is_valid)
		last = cols_next [MONO_TYPEDEF_METHOD_LIST] - 1;
	else
		last = m->tables [MONO_TABLE_METHOD].rows;
	
	if (cols [MONO_TYPEDEF_METHOD_LIST] && cols [MONO_TYPEDEF_METHOD_LIST] <= m->tables [MONO_TABLE_METHOD].rows)
		dis_method_list (NULL, m, cols [MONO_TYPEDEF_METHOD_LIST] - 1, last, NULL);

}

static void dis_resources_worker (MonoImage *m, gboolean just_print)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_MANIFESTRESOURCE];
	int i;
	
	for (i = 0; i < t->rows; i++){
		guint32 cols [MONO_MANIFEST_SIZE];
		const char *name, *res;
		guint32 size;
		FILE* fp;

		mono_metadata_decode_row (t, i, cols, MONO_MANIFEST_SIZE);
		name = mono_metadata_string_heap (m, cols [MONO_MANIFEST_NAME]);

		if (just_print)
			fprintf (output, "%8x: %s", cols [MONO_MANIFEST_OFFSET], name);
		
		if (! (res = mono_image_get_resource (m, cols [MONO_MANIFEST_OFFSET], &size))) {
			if (just_print)
				fprintf (output, " (absent from image)\n");
			continue;	
		}

		if (just_print) {
			fprintf (output, " (size %u)\n", size);
			continue;
		}
		
		if ( (fp = fopen (name, "ab")) ) {
			if (ftell (fp) == 0)
				fwrite (res, size, 1, fp);
			else 
				g_warning ("Error creating managed resource - %s : File already exists.", name);

			fclose (fp);
		} else
			g_warning ("Error creating managed resource - %s : %s", name, g_strerror (errno));
	}		
}

static void
dis_mresource (MonoImage *m)
{
	dis_resources_worker (m, FALSE);
}

static void
dis_presource (MonoImage *m)
{
	dis_resources_worker (m, TRUE);
}

static char *
exported_type_flags (guint32 flags)
{
	static char buffer [1024];
	int visibility = flags & TYPE_ATTRIBUTE_VISIBILITY_MASK;

	buffer [0] = 0;

	if (flags & TYPE_ATTRIBUTE_FORWARDER) {
		strcat (buffer, "forwarder ");
		return buffer;
	}

	strcat (buffer, map (visibility, visibility_map));
	return buffer;
}

static char *
get_escaped_fullname (MonoImage *m, guint32 nspace_idx, guint32 name_idx)
{
	const char *name, *nspace;
	char *fullname, *esfullname;

	nspace = mono_metadata_string_heap (m, nspace_idx);
	name = mono_metadata_string_heap (m, name_idx);

	fullname = g_strdup_printf ("%s%s%s", nspace, *nspace ? "." : "", name);
	esfullname = get_escaped_name (fullname);

	g_free (fullname);

	return esfullname;
}

static void
dis_exported_types (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_EXPORTEDTYPE];
	int i;

	for (i = 1; i <= t->rows; i++) {
		char *fullname;
		guint32 impl, idx, type_token;
		guint32 cols [MONO_EXP_TYPE_SIZE];

		mono_metadata_decode_row (t, i - 1, cols, MONO_EXP_TYPE_SIZE);

		fullname = get_escaped_fullname (m, cols [MONO_EXP_TYPE_NAMESPACE], cols [MONO_EXP_TYPE_NAME]);

		fprintf (output, "\n");
		fprintf (output, ".class extern %s%s\n", exported_type_flags (cols [MONO_EXP_TYPE_FLAGS]), fullname);
		fprintf (output, "{\n");

		g_free (fullname);

		impl = cols [MONO_EXP_TYPE_IMPLEMENTATION];
		if (impl) {
			idx = impl >> MONO_IMPLEMENTATION_BITS;
			switch (impl & MONO_IMPLEMENTATION_MASK) {
			case MONO_IMPLEMENTATION_FILE:
				fprintf (output, "    .file '%s'\n",
					mono_metadata_string_heap (m, mono_metadata_decode_row_col (&m->tables [MONO_TABLE_FILE], idx - 1, MONO_FILE_NAME)));
				break;
			case MONO_IMPLEMENTATION_ASSEMBLYREF:
				fprintf (output, "    .assembly extern '%s'\n",
					mono_metadata_string_heap (m, mono_metadata_decode_row_col (&m->tables [MONO_TABLE_ASSEMBLYREF], idx - 1, MONO_ASSEMBLYREF_NAME)));
				break;
			case MONO_IMPLEMENTATION_EXP_TYPE:
				fullname = get_escaped_fullname (
					m,
					mono_metadata_decode_row_col (&m->tables [MONO_TABLE_EXPORTEDTYPE], idx - 1, MONO_EXP_TYPE_NAMESPACE),
					mono_metadata_decode_row_col (&m->tables [MONO_TABLE_EXPORTEDTYPE], idx - 1, MONO_EXP_TYPE_NAME));
				fprintf (output, "    .class extern %s\n", fullname);
				g_free (fullname);
				break;
			default:
				g_assert_not_reached ();
				break;
			}
		}

		type_token = cols [MONO_EXP_TYPE_TYPEDEF];
		if (type_token)
			fprintf (output, "    .class 0x%08x\n", type_token | MONO_TOKEN_TYPE_DEF);

		fprintf (output, "}\n");
	}
}

/**
 * dis_types:
 * @m: metadata context
 *
 * disassembles all types in the @m context
 */
static void
dis_types (MonoImage *m, int forward)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_TYPEDEF];
	int i;
	guint32 flags;

        dis_globals (m);
        
	for (i = 1; i < t->rows; i++) {
		flags = mono_metadata_decode_row_col (t, i, MONO_TYPEDEF_FLAGS);
		flags &= TYPE_ATTRIBUTE_VISIBILITY_MASK;
		if (flags == TYPE_ATTRIBUTE_PUBLIC || flags == TYPE_ATTRIBUTE_NOT_PUBLIC)
			dis_type (m, i, 0, forward);
	}
}

static const char *
get_uninitialized_data_type (guint32 size)
{
	switch (size) {
	case 1:
		return "int8";
	case 2:
		return "int16";
	case 4:
		return "int32";
	case 8:
		return "int64";
	default:
		g_error ("get_uninitialized_data_type for size: %d\n", size);
	}
	return NULL;
}

/**
 * dis_data:
 * @m: metadata context
 *
 * disassembles all data blobs references in the FieldRVA table in the @m context
 */
static void
dis_data (MonoImage *m)
{
	MonoTableInfo *t = &m->tables [MONO_TABLE_FIELDRVA];
	MonoTableInfo *ft = &m->tables [MONO_TABLE_FIELD];
	int i, b;
	const char *rva, *sig;
	guint32 size;
	gint align;
	guint32 cols [MONO_FIELD_RVA_SIZE];
	MonoType *type;

	for (i = 0; i < t->rows; i++) {
		MonoError error;
		mono_metadata_decode_row (t, i, cols, MONO_FIELD_RVA_SIZE);
		rva = mono_image_rva_map (m, cols [MONO_FIELD_RVA_RVA]);
		sig = mono_metadata_blob_heap (m, mono_metadata_decode_row_col (ft, cols [MONO_FIELD_RVA_FIELD] -1, MONO_FIELD_SIGNATURE));
		mono_metadata_decode_value (sig, &sig);
		/* FIELD signature == 0x06 */
		g_assert (*sig == 0x06);
		type = mono_metadata_parse_type_checked (m, NULL, 0, FALSE, sig + 1, &sig, &error);
		if (!type) {
			fprintf (output, "// invalid field %d due to %s\n", i, mono_error_get_message (&error));
			mono_error_cleanup (&error);
			continue;
		}
		mono_class_init (mono_class_from_mono_type (type));
		size = mono_type_size (type, &align);

		if (rva) {
			fprintf (output, ".data D_%08x = bytearray (", cols [MONO_FIELD_RVA_RVA]);
			for (b = 0; b < size; ++b) {
				if (!(b % 16))
					fprintf (output, "\n\t");
				fprintf (output, " %02X", rva [b] & 0xff);
			}
			fprintf (output, ") // size: %d\n", size);
		} else
			fprintf (output, ".data D_%08x = %s [%d]\n",
				cols [MONO_FIELD_RVA_RVA], get_uninitialized_data_type (size), size);
	}
}

struct {
	const char *name;
	int table;
	void (*dumper) (MonoImage *m);
} table_list [] = {
	{ "--assembly",    MONO_TABLE_ASSEMBLY,    	dump_table_assembly },
	{ "--assemblyref", MONO_TABLE_ASSEMBLYREF, 	dump_table_assemblyref },
	{ "--classlayout", MONO_TABLE_CLASSLAYOUT, 	dump_table_class_layout },
	{ "--constant",    MONO_TABLE_CONSTANT,    	dump_table_constant },
	{ "--customattr",  MONO_TABLE_CUSTOMATTRIBUTE,  dump_table_customattr },
	{ "--declsec",     MONO_TABLE_DECLSECURITY, 	dump_table_declsec },
	{ "--event",       MONO_TABLE_EVENT,       	dump_table_event },
	{ "--exported",    MONO_TABLE_EXPORTEDTYPE,     dump_table_exported },
	{ "--fields",      MONO_TABLE_FIELD,       	dump_table_field },
	{ "--file",        MONO_TABLE_FILE,        	dump_table_file },
	{ "--genericpar",  MONO_TABLE_GENERICPARAM,     dump_table_genericpar },
	{ "--interface",   MONO_TABLE_INTERFACEIMPL,    dump_table_interfaceimpl },
	{ "--manifest",    MONO_TABLE_MANIFESTRESOURCE, dump_table_manifest },
	{ "--marshal",     MONO_TABLE_FIELDMARSHAL,	dump_table_field_marshal },
	{ "--memberref",   MONO_TABLE_MEMBERREF,   	dump_table_memberref },
	{ "--method",      MONO_TABLE_METHOD,      	dump_table_method },
	{ "--methodimpl",  MONO_TABLE_METHODIMPL,  	dump_table_methodimpl },
	{ "--methodsem",   MONO_TABLE_METHODSEMANTICS,  dump_table_methodsem },
	{ "--methodspec",  MONO_TABLE_METHODSPEC,       dump_table_methodspec },
	{ "--moduleref",   MONO_TABLE_MODULEREF,   	dump_table_moduleref },
	{ "--module",      MONO_TABLE_MODULE,      	dump_table_module },
	{ "--mresources",  0,	dis_mresource },
	{ "--presources", 0, dis_presource },
	{ "--nested",      MONO_TABLE_NESTEDCLASS, 	dump_table_nestedclass },
	{ "--param",       MONO_TABLE_PARAM,       	dump_table_param },
	{ "--parconst",    MONO_TABLE_GENERICPARAMCONSTRAINT, dump_table_parconstraint },
	{ "--property",    MONO_TABLE_PROPERTY,    	dump_table_property },
	{ "--propertymap", MONO_TABLE_PROPERTYMAP, 	dump_table_property_map },
	{ "--typedef",     MONO_TABLE_TYPEDEF,     	dump_table_typedef },
	{ "--typeref",     MONO_TABLE_TYPEREF,     	dump_table_typeref },
	{ "--typespec",    MONO_TABLE_TYPESPEC,     	dump_table_typespec },
	{ "--implmap",     MONO_TABLE_IMPLMAP,     	dump_table_implmap },
	{ "--fieldrva",    MONO_TABLE_FIELDRVA,     dump_table_fieldrva },
	{ "--standalonesig", MONO_TABLE_STANDALONESIG,  dump_table_standalonesig },
	{ "--methodptr", MONO_TABLE_METHOD_POINTER,  dump_table_methodptr },
	{ "--fieldptr", MONO_TABLE_FIELD_POINTER,  dump_table_fieldptr },
	{ "--paramptr", MONO_TABLE_PARAM_POINTER,  dump_table_paramptr },
	{ "--eventptr", MONO_TABLE_EVENT_POINTER,  dump_table_eventptr },
	{ "--propertyptr", MONO_TABLE_PROPERTY_POINTER,  dump_table_propertyptr },
	{ "--blob", 0, dump_stream_blob },
	{ "--strings", 0, dump_stream_strings },
	{ "--userstrings", 0, dump_stream_us },
	{ NULL, -1, }
};

/**
 * disassemble_file:
 * @file: file containing CIL code.
 *
 * Disassembles the @file file.
 */
static int
disassemble_file (const char *file)
{
	MonoImageOpenStatus status;
	MonoImage *img;

	img = mono_image_open (file, &status);
	if (!img) {
		fprintf (stderr, "Error while trying to process %s\n", file);
		return 1;
	} else {
		/* FIXME: is this call necessary? */
		mono_assembly_load_from_full (img, file, &status, FALSE);
	}

	setup_filter (img);

	if (dump_table != -1){
		(*table_list [dump_table].dumper) (img);
	} else {
		dump_header_data (img);
		
		dis_directive_assemblyref (img);
		dis_directive_assembly (img);
		dis_directive_file (img);
		dis_directive_mresource (img);
		dis_directive_module (img);
		dis_directive_moduleref (img);
		dis_exported_types (img);
		dis_nt_header (img);
                if (dump_managed_resources)
        		dis_mresource (img);
		if (dump_forward_decls) {
			fprintf (output, "// *************** Forward Declarations for Classes ***************\n\n"); 
			dis_types (img, 1);
			fprintf (output, "// *************** End-Of Forward Declarations for Classes ***************\n\n"); 
		}	
		dis_types (img, 0);
		dis_data (img);
	}
	
	mono_image_close (img);
	return 0;
}

typedef struct {
	int size;
	int count;
	int *elems;
} TableFilter;

typedef struct {
	char *name;
	char *guid;
	TableFilter types;
	TableFilter fields;
	TableFilter methods;
} ImageFilter;

static GList *filter_list = NULL;
static ImageFilter *cur_filter = NULL;

static void     
setup_filter (MonoImage *image)
{
	ImageFilter *ifilter;
	GList *item;
	const char *name = mono_image_get_name (image);

	for (item = filter_list; item; item = item->next) {
		ifilter = (ImageFilter *)item->data;
		if (strcmp (ifilter->name, name) == 0) {
			cur_filter = ifilter;
			return;
		}
	}
	cur_filter = NULL;
}

static int
int_cmp (const void *e1, const void *e2)
{
	const int *i1 = (const int *)e1;
	const int *i2 = (const int *)e2;
	return *i1 - *i2;
}

static gboolean 
table_includes (TableFilter *tf, int idx)
{
	if (!tf->count)
		return FALSE;
	return mono_binary_search (&idx, tf->elems, tf->count, sizeof (int), int_cmp) != NULL;
}

static gboolean 
should_include_type (int idx)
{
	if (!cur_filter)
		return TRUE;
	return table_includes (&cur_filter->types, idx);
}

static gboolean
should_include_method (int idx)
{
	if (!cur_filter)
		return TRUE;
	return table_includes (&cur_filter->methods, idx);
}

static gboolean
should_include_field (int idx)
{
	if (!cur_filter)
		return TRUE;
	return table_includes (&cur_filter->fields, idx);
}

static ImageFilter*
add_filter (const char *name)
{
	ImageFilter *ifilter;
	GList *item;

	for (item = filter_list; item; item = item->next) {
		ifilter = (ImageFilter *)item->data;
		if (strcmp (ifilter->name, name) == 0)
			return ifilter;
	}
	ifilter = g_new0 (ImageFilter, 1);
	ifilter->name = g_strdup (name);
	filter_list = g_list_prepend (filter_list, ifilter);
	return ifilter;
}

static void
add_item (TableFilter *tf, int val)
{
	if (tf->count >= tf->size) {
		if (!tf->size) {
			tf->size = 8;
			tf->elems = (int *)g_malloc (sizeof (int) * tf->size);
		} else {
			tf->size *= 2;
			tf->elems = (int *)g_realloc (tf->elems, sizeof (int) * tf->size);
		}
	}
	tf->elems [tf->count++] = val;
}

static void
sort_filter_elems (void)
{
	ImageFilter *ifilter;
	GList *item;

	for (item = filter_list; item; item = item->next) {
		ifilter = (ImageFilter *)item->data;
		qsort (ifilter->types.elems, ifilter->types.count, sizeof (int), int_cmp);
		qsort (ifilter->fields.elems, ifilter->fields.count, sizeof (int), int_cmp);
		qsort (ifilter->methods.elems, ifilter->methods.count, sizeof (int), int_cmp);
	}
}

static void
load_filter (const char* filename)
{
	FILE *file;
	char buf [1024];
	char *p, *s, *endptr;
	int line = 0;
	ImageFilter *ifilter = NULL;
	int value = 0;
	
	if (!(file = fopen (filename, "r"))) {
		g_print ("Cannot open filter file '%s'\n", filename);
		exit (1);
	}
	while (fgets (buf, sizeof (buf), file) != NULL) {
		++line;
		s = buf;
		while (*s && g_ascii_isspace (*s)) ++s;
		switch (*s) {
		case 0:
		case '#':
			break;
		case '[':
			p = strchr (s, ']');
			if (!p)
				g_error ("No matching ']' in filter at line %d\n", line);
			*p = 0;
			ifilter = add_filter (s + 1);
			break;
		case 'T':
			if (!ifilter)
				g_error ("Invalid format in filter at line %d\n", line);
			if ((s [1] != ':') || !(value = strtol (s + 2, &endptr, 0)) || (endptr == s + 2))
				g_error ("Invalid type number in filter at line %d\n", line);
			add_item (&ifilter->types, value);
			break;
		case 'M':
			if (!ifilter)
				g_error ("Invalid format in filter at line %d\n", line);
			if ((s [1] != ':') || !(value = strtol (s + 2, &endptr, 0)) || (endptr == s + 2))
				g_error ("Invalid method number in filter at line %d\n", line);
			add_item (&ifilter->methods, value);
			break;
		case 'F':
			if (!ifilter)
				g_error ("Invalid format in filter at line %d\n", line);
			if ((s [1] != ':') || !(value = strtol (s + 2, &endptr, 0)) || (endptr == s + 2))
				g_error ("Invalid field number in filter at line %d\n", line);
			add_item (&ifilter->fields, value);
			break;
		default:
			g_error ("Invalid format in filter at line %d\n", line);
		}
	}
	fclose (file);
	sort_filter_elems ();
}


static gboolean
try_load_from (MonoAssembly **assembly,
	       const gchar *path1, const gchar *path2,
	       const gchar *path3, const gchar *path4, gboolean refonly,
	       MonoAssemblyCandidatePredicate predicate, gpointer user_data)
{
	gchar *fullpath;

	*assembly = NULL;
	fullpath = g_build_filename (path1, path2, path3, path4, NULL);
	if (g_file_test (fullpath, G_FILE_TEST_IS_REGULAR))
		*assembly = mono_assembly_open_predicate (fullpath, refonly, FALSE, predicate, user_data, NULL);

	g_free (fullpath);
	return (*assembly != NULL);
}

static MonoAssembly *
real_load (gchar **search_path, const gchar *culture, const gchar *name, gboolean refonly,
	   MonoAssemblyCandidatePredicate predicate, gpointer user_data)
{
	MonoAssembly *result = NULL;
	gchar **path;
	gchar *filename;
	const gchar *local_culture;
	gint len;

	if (!culture || *culture == '\0') {
		local_culture = "";
	} else {
		local_culture = culture;
	}

	filename =  g_strconcat (name, ".dll", NULL);
	len = strlen (filename);

	for (path = search_path; *path; path++) {
		if (**path == '\0')
			continue; /* Ignore empty ApplicationBase */

		/* See test cases in bug #58992 and bug #57710 */
		/* 1st try: [culture]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, "", filename, refonly, predicate, user_data))
			break;

		/* 2nd try: [culture]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, "", filename, refonly, predicate, user_data))
			break;

		/* 3rd try: [culture]/[name]/[name].dll (culture may be empty) */
		strcpy (filename + len - 4, ".dll");
		if (try_load_from (&result, *path, local_culture, name, filename, refonly, predicate, user_data))
			break;

		/* 4th try: [culture]/[name]/[name].exe (culture may be empty) */
		strcpy (filename + len - 4, ".exe");
		if (try_load_from (&result, *path, local_culture, name, filename, refonly, predicate, user_data))
			break;
	}

	g_free (filename);
	return result;
}

/*
 * Try to load referenced assemblies from assemblies_path.
 */
static MonoAssembly *
monodis_preload (MonoAssemblyName *aname,
				 gchar **assemblies_path,
				 gpointer user_data)
{
	MonoAssembly *result = NULL;
	gboolean refonly = GPOINTER_TO_UINT (user_data);

	if (assemblies_path && assemblies_path [0] != NULL) {
		result = real_load (assemblies_path, aname->culture, aname->name, refonly, NULL, NULL);
	}

	return result;
}

static GList *loaded_assemblies = NULL;

static void
monodis_assembly_load_hook (MonoAssembly *assembly, gpointer user_data)
{
	loaded_assemblies = g_list_prepend (loaded_assemblies, assembly);
}

static MonoAssembly *
monodis_assembly_search_hook (MonoAssemblyName *aname, gpointer user_data)
{
        GList *tmp;

       for (tmp = loaded_assemblies; tmp; tmp = tmp->next) {
               MonoAssembly *ass = (MonoAssembly *)tmp->data;
               if (mono_assembly_names_equal (aname, &ass->aname))
		       return ass;
       }
       return NULL;
}

static void
usage (void)
{
	GString *args = g_string_new ("[--output=filename] [--filter=filename]\n");
	int i;

	g_string_append (args, "[--help] [--mscorlib] [--show-tokens] [--show-method-tokens]\n");
	
	for (i = 0; table_list [i].name != NULL; i++){
		g_string_append (args, "[");
		g_string_append (args, table_list [i].name);
		g_string_append (args, "] ");
		if (((i-2) % 5) == 0)
			g_string_append_c (args, '\n');
	}
	g_string_append (args, "[--forward-decls]");
	fprintf (stderr,
		 "monodis -- Mono Common Intermediate Language Disassembler\n" 
		 "Usage is: monodis %s file ..\n", args->str);
	exit (1);
}

static void
thread_state_init (MonoThreadUnwindState *ctx)
{
}

int
main (int argc, char *argv [])
{
	MonoThreadInfoRuntimeCallbacks ticallbacks;

	GList *input_files = NULL, *l;
	int i, j;

	output = stdout;
	init_key_table ();
	for (i = 1; i < argc; i++){
		if (argv [i][0] == '-'){
			if (argv [i][1] == 'h')
				usage ();
			else if (argv [i][1] == 'd')
				dump_header_data_p = TRUE;
			else if (strcmp (argv [i], "--mscorlib") == 0) {
				substitute_with_mscorlib_p = TRUE;
				continue;
			} else if (strcmp (argv [i], "--show-method-tokens") == 0) {
				show_method_tokens = TRUE;
				continue;
			} else if (strcmp (argv [i], "--show-tokens") == 0) {
				show_tokens = TRUE;
				continue;
			} else if (strncmp (argv [i], "--output=", 9) == 0) {
				output = fopen (argv [i]+9, "w");
				if (output == NULL) {
					fprintf (stderr, "Can't open output file `%s': %s\n",
						 argv [i]+9, strerror (errno));
					exit (1);
				}
				dump_managed_resources = TRUE;
				continue;
			} else if (strncmp (argv [i], "--filter=", 9) == 0) {
				load_filter (argv [i]+9);
				continue;
			} else if (strcmp (argv [i], "--forward-decls") == 0) {
				dump_forward_decls = TRUE;
				continue;
			} else if (strcmp (argv [i], "--help") == 0)
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

	CHECKED_MONO_INIT ();
	mono_counters_init ();
	mono_tls_init_runtime_keys ();
	memset (&ticallbacks, 0, sizeof (ticallbacks));
	ticallbacks.thread_state_init = thread_state_init;
#ifndef HOST_WIN32
	mono_w32handle_init ();
#endif
	mono_thread_info_runtime_init (&ticallbacks);

	mono_install_assembly_load_hook (monodis_assembly_load_hook, NULL);
	mono_install_assembly_search_hook (monodis_assembly_search_hook, NULL);

	/*
	 * If we just have one file, use the corlib version it requires.
	 */
	if (!input_files->next) {
		char *filename = (char *)input_files->data;

		mono_init_from_assembly (argv [0], filename);

		mono_install_assembly_preload_hook (monodis_preload, GUINT_TO_POINTER (FALSE));

		return disassemble_file (filename);
	} else {
		mono_init (argv [0]);

		i = 0;
		for (l = input_files; l; l = l->next)
			if (disassemble_file ((const char *)l->data) == 1) i = 1;
		return i;
	}

	return 0;
}
