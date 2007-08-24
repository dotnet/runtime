/*
 * monodiet.c: an IL code garbage collector
 *
 * Author:
 *        Paolo Molaro (lupus@ximian.com)
 *
 * (C) 2004 Novell, Inc.
 */

#include <glib.h>
#include <string.h>
#include "mono/metadata/class-internals.h"
#include "mono/metadata/assembly.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/appdomain.h" /* mono_init */
#include "mono/metadata/debug-helpers.h"

/*
TODO:
*) handle proprties, events in a smart way.
*) add option that takes a directory and outputs the il files and recompiles automatically
*/
static GHashTable *type_table;
static GHashTable *method_table;
static GHashTable *field_table;
static GHashTable *image_table;
static GList *virtual_methods;
static int verbose = 0;
static int force_enums = 0;
static FILE *outf = NULL;

enum {
	TYPE_BASIC = 1 << 0,
	TYPE_FIELDS = 1 << 1,
	TYPE_METHODS = 1 << 2,
	TYPE_PROPERTIES = 1 << 3,
	TYPE_EVENTS = 1 << 4,
	TYPE_ALL = TYPE_BASIC | TYPE_FIELDS | TYPE_METHODS | TYPE_PROPERTIES | TYPE_EVENTS
};

static void handle_cattrs (MonoCustomAttrInfo* cattrs);

static void
add_type (MonoClass* klass)
{
	gpointer val = NULL, oldkey = NULL;
	if (g_hash_table_lookup_extended (type_table, klass, &oldkey, &val))
		return;
	g_hash_table_insert (type_table, klass, NULL);
	g_hash_table_insert (image_table, klass->image, NULL);
}

static void
add_types_from_signature (MonoMethodSignature *sig)
{
	MonoClass *klass;
	int i;
	for (i = 0; i < sig->param_count; ++i) {
		klass = mono_class_from_mono_type (sig->params [i]);
		add_type (klass);
	}
	klass = mono_class_from_mono_type (sig->ret);
	add_type (klass);
}

static void
add_field (MonoClassField *field) {
	MonoClass *k;
	MonoCustomAttrInfo* cattrs;
	gpointer val = NULL, oldkey = NULL;

	if (g_hash_table_lookup_extended (field_table, field, &oldkey, &val))
		return;
	g_hash_table_insert (field_table, field, NULL);
	add_type (field->parent);
	k = mono_class_from_mono_type (field->type);
	add_type (k);
	cattrs = mono_custom_attrs_from_field (field->parent, field);
	handle_cattrs (cattrs);
}

static void
add_types_from_method (MonoMethod *method) {
	const MonoOpcode *opcode;
	MonoMethodHeader *header;
	const unsigned char *ip, *il_code_end;
	gpointer val = NULL, oldkey = NULL;
	int i, n;
	guint32 token;
	MonoClass *klass;
	MonoClassField *field;
	MonoCustomAttrInfo* cattrs;
	MonoType** locals;
	gpointer exc_iter;
	MonoExceptionClause clause;

	if (g_hash_table_lookup_extended (method_table, method, &oldkey, &val))
		return;
	g_hash_table_insert (method_table, method, NULL);

	g_assert (method->klass);

	if (verbose > 1)
		g_print ("#processing method: %s\n", mono_method_full_name (method, TRUE));
	mono_class_init (method->klass);
	cattrs = mono_custom_attrs_from_method (method);
	handle_cattrs (cattrs);
	add_type (method->klass);
	add_types_from_signature (mono_method_signature (method));
	for (i = 0; i < mono_method_signature (method)->param_count + 1; ++i) {
		cattrs = mono_custom_attrs_from_param (method, i);
		handle_cattrs (cattrs);
	}

	if (method->flags & METHOD_ATTRIBUTE_VIRTUAL)
		virtual_methods = g_list_prepend (virtual_methods, method);

	/* if no IL code to parse, return */
	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))
		return;
	if (method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))
		return;

	header = mono_method_get_header (method);

	locals = mono_method_header_get_locals (header, &n, NULL);
	for (i = 0; i < n; ++i) {
		klass = mono_class_from_mono_type (locals [i]);
		add_type (klass);
	}
	for (exc_iter = NULL; mono_method_header_get_clauses (header, method, &exc_iter, &clause);) {
		if (clause.flags == MONO_EXCEPTION_CLAUSE_NONE)
			add_type (clause.data.catch_class);
	}

	ip = mono_method_header_get_code (header, &n, NULL);
	il_code_end = ip + n;

	while (ip < il_code_end) {
		if (verbose > 2)
			g_print ("#%s", mono_disasm_code_one (NULL, method, ip, NULL));
		if (*ip == 0xfe) {
			++ip;
			i = *ip + 256;
		} else {
			i = *ip;
		}

		opcode = &mono_opcodes [i];

		switch (opcode->argument) {
		case MonoInlineNone:
			ip++;
			break;
		case MonoInlineType:
			token = read32 (ip + 1);
			add_type (mono_class_get (method->klass->image, token));
			ip += 5;
			break;
		case MonoInlineField: {
			token = read32 (ip + 1);
			field = mono_field_from_token (method->klass->image, token, &klass, NULL);
			add_field (field);
			add_type (klass);
			ip += 5;
			break;
		}
		case MonoInlineTok:
		case MonoInlineSig:
			/* FIXME */
		case MonoInlineString:
		case MonoShortInlineR:
		case MonoInlineBrTarget:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
		case MonoShortInlineBrTarget:
			ip += 2;
			break;
		case MonoInlineSwitch:
			++ip;
			n = read32 (ip);
			ip += 4;
			ip += 4 * n;
			break;
		case MonoInlineI8:
		case MonoInlineR:
			ip += 9;
			break;
		case MonoInlineMethod:
			{
				MonoMethod *cm = mono_get_method (method->klass->image, read32 (ip + 1), NULL);
				add_type (cm->klass);
				add_types_from_method (cm);
			}
			ip += 5;
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

static void
handle_cattrs (MonoCustomAttrInfo* cattrs)
{
	int i;
	if (!cattrs)
		return;
	for (i = 0; i < cattrs->num_attrs; ++i) {
		add_types_from_method (cattrs->attrs [i].ctor);
	}
	mono_custom_attrs_free (cattrs);
}

static void
handle_type (MonoClass *klass, guint32 flags)
{
	int i;
	guint32 missing;
	MonoCustomAttrInfo* cattrs;
	gpointer val = NULL, oldkey = NULL;
	MonoProperty* prop;
	MonoEvent* event;
	MonoMethod* method;
	MonoClassField* field;
	gpointer iter;
	
	if (g_hash_table_lookup_extended (type_table, klass, &oldkey, &val)) {
		missing = flags & ~(GPOINTER_TO_UINT (val));
	} else {
		missing = flags;
	}
	if (!missing)
		return;
	g_hash_table_insert (type_table, klass, GUINT_TO_POINTER (missing));
	if (verbose)
		g_print ("#processing klass: %s.%s\n", klass->name_space, klass->name);
	mono_class_init (klass);
	if (klass->parent)
		add_type (klass->parent);
	if (klass->nested_in)
		add_type (klass->nested_in);
	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		if ((missing & TYPE_METHODS) || strcmp (method->name, ".cctor") == 0)
			add_types_from_method (method);
	}
	if (klass->enumtype) {
		add_field (mono_class_get_field_from_name (klass, "value__"));
	}
	if (force_enums || (missing & TYPE_FIELDS)) {
		iter = NULL;
		while ((field = mono_class_get_fields (klass, &iter)))
			add_field (field);
	}
	iter = NULL;
	while ((prop = mono_class_get_properties (klass, &iter))) {
		cattrs = mono_custom_attrs_from_property (klass, prop);
		handle_cattrs (cattrs);
	}
	iter = NULL;
	while ((event = mono_class_get_events (klass, &iter))) {
		cattrs = mono_custom_attrs_from_event (klass, event);
		handle_cattrs (cattrs);
	}
	for (i = 0; i < klass->interface_count; ++i)
		add_type (klass->interfaces [i]);
	cattrs = mono_custom_attrs_from_class (klass);
	handle_cattrs (cattrs);
}

static void
process_image (MonoImage *image, gboolean all) {
	int i;
	const MonoTableInfo *t;
	MonoClass *klass;
	MonoMethod *entry;
	guint32 eptoken;

	if (verbose)
		g_print ("#processing image: %s\n", mono_image_get_name (image));
	eptoken =  mono_image_get_entry_point (image);
	if (eptoken) {
		entry = mono_get_method (image, eptoken, NULL);
		add_types_from_method (entry);
	}
	/* we always add the <Module> type */
	klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | 1);
	handle_type (klass, all? TYPE_ALL: TYPE_BASIC);
	if (all) {
		t = mono_image_get_table_info (image, MONO_TABLE_TYPEDEF);
		for (i = 1; i < mono_table_info_get_rows (t); ++i) {
			klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | (i + 1));
			handle_type (klass, all? TYPE_ALL: TYPE_BASIC);
		}
	}
}

static void
process_assembly (MonoAssembly *assembly, gboolean all) {
	MonoCustomAttrInfo* cattrs;
	process_image (mono_assembly_get_image (assembly), all);
	cattrs = mono_custom_attrs_from_assembly (assembly);
	handle_cattrs (cattrs);
}

static GList *worklist = NULL;

static void
collect_type (const gpointer key, const gpointer val, gpointer user_data)
{
	MonoClass *klass = key;
	if (klass->rank || klass->byval_arg.type == MONO_TYPE_PTR)
		return;
	worklist = g_list_prepend (worklist, key);
}

static void
check_vmethods (MonoClass *klass, MonoMethod *method)
{
	MonoMethod **vtable;
	if (method->klass == klass)
		return;
	mono_class_init (klass);
	mono_class_init (method->klass);
	vtable = klass->vtable;
	/* iface */
	if (!vtable)
		return;
	if (method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		if (MONO_CLASS_IMPLEMENTS_INTERFACE (klass, method->klass->interface_id)) {
			int iface_offset = mono_class_interface_offset (klass, method->klass);
			g_assert (method->slot != -1);
			if (vtable [iface_offset + method->slot])
				add_types_from_method (vtable [iface_offset + method->slot]);
		}
	} else {
		if (mono_class_has_parent (klass, method->klass)) {
			g_assert (method->slot != -1);
			if (vtable [method->slot])
				add_types_from_method (vtable [method->slot]);
		}
	}
}

static void
process_images (void) {
	int old_count, new_count;
	GList *item, *vmethod;
	new_count = g_hash_table_size (type_table);
	new_count += g_hash_table_size (method_table);
	new_count += g_hash_table_size (field_table);
	do {
		old_count = new_count;
		if (verbose)
			g_print ("#processing type table: %d\n", old_count);
		g_list_free (worklist);
		worklist = NULL;
		g_hash_table_foreach (type_table, collect_type, NULL);
		for (item = worklist; item; item = item->next) {
			for (vmethod = virtual_methods; vmethod; vmethod = vmethod->next) {
				check_vmethods (item->data, vmethod->data);
			}
		}
		g_list_free (worklist);
		worklist = NULL;
		g_hash_table_foreach (type_table, collect_type, NULL);
		for (item = worklist; item; item = item->next) {
			handle_type (item->data, TYPE_BASIC);
		}
		new_count = g_hash_table_size (type_table);
		new_count += g_hash_table_size (method_table);
		new_count += g_hash_table_size (field_table);
	} while (old_count != new_count);
}

static void
foreach_method (const gpointer key, const gpointer val, gpointer user_data)
{
	MonoMethod *method = key;
	MonoClass *klass = user_data;
	if (method->klass != klass)
		return;
	/* FIXME: ensure it's the correct token */
	fprintf (outf, "M:0x%x\n", mono_metadata_token_index (method->token));
}

static void
foreach_field (const gpointer key, const gpointer val, gpointer user_data)
{
	MonoClassField *field = key;
	MonoClass *klass = user_data;
	int idx;
	if (field->parent != klass)
		return;
	idx = mono_metadata_token_index (mono_class_get_field_token (field));
	fprintf (outf, "F:0x%x\n", idx);
}

static void
foreach_type (const gpointer key, const gpointer val, gpointer user_data)
{
	MonoClass *klass = key;
	MonoImage *image = user_data;
	if (klass->image != image)
		return;
	if (klass->rank || klass->byval_arg.type == MONO_TYPE_PTR)
		return;
	fprintf (outf, "T:0x%x\n", mono_metadata_token_index (klass->type_token));
	g_hash_table_foreach (method_table, foreach_method, klass);
	g_hash_table_foreach (field_table, foreach_field, klass);
}

static void
foreach_image (const gpointer key, const gpointer val, gpointer user_data)
{
	MonoImage *image = key;
	const char* aname;
	aname = mono_image_get_name (image);
	/* later print the guid as well to prevent mismatches */
	fprintf (outf, "[%s]\n", aname);
	g_hash_table_foreach (type_table, foreach_type, image);
}

static void
dump_images (const char *filename)
{
	if (filename) {
		FILE* f = fopen (filename, "w");
		if (!f)
			g_error ("cannot write to file '%s'\n", filename);
		else
			outf = f;
	} else {
		outf = stdout;
	}
	g_hash_table_foreach (image_table, foreach_image, NULL);
	if (filename)
		fclose (outf);
}

static void
usage (int code) {
	printf ("monodiet 0.1 Copyright (c) 2004 Novell, Inc\n");
	printf ("List the metadata elements used by the named assemblies.\n");
	printf ("Usage: monodiet [options] assembly [assembly2 ...]\n\n");
	printf ("Options:\n");
	printf ("\t-v           increase verbose level\n");
	printf ("\t-h           show help screen\n");
	printf ("\t-e           force inclusion of enum members\n");
	printf ("\t-o FILENAME  output the result to filename\n");
	printf ("\t-a FILENAME  add metadata elements from description in filename\n");
	printf ("\t-F ASSEMBLY  force add all metadata elements from assembly\n");
	exit (code);
}

static MonoImage*
find_image (char *name)
{
	return mono_image_loaded (name);
}

static MonoClass*
find_class (MonoImage *image, char *name)
{
	char *p = strrchr (name, '.');
	if (p) {
		*p = 0;
		return mono_class_from_name (image, name, p + 1);
	}
	return mono_class_from_name (image, "", name);
}

static void
load_roots (const char* filename)
{
	FILE *file;
	char buf [2048];
	char *p, *s;
	int line = 0;
	MonoImage *image = NULL;
	MonoClass *klass = NULL;
	MonoClassField *field;
	MonoMethodDesc *mdesc;
	MonoMethod *method;

	if (!(file = fopen (filename, "r")))
		return;
	
	while (fgets (buf, sizeof (buf), file)) {
		/* FIXME:
		 * decide on the format to use to express types, fields, methods,
		 * maybe the same used on output from the tool, but with explicit
		 * names and signatures instead of token indexes
		 * add wildcard support
		 */
		++line;
		s = buf;
		while (*s && g_ascii_isspace (*s)) ++s;
		switch (*s) {
		case 0:
		case '#':
			continue; /* comment */
		case '[':
			p = strchr (s, ']');
			if (!p)
				g_error ("invalid assembly format at line %d\n", line);
			*p = 0;
			p = s + 1;
			image = find_image (p);
			if (!image)
				g_error ("image not loaded: %s\n", p);
			klass = NULL;
		 	break;
		case 'T':
			if (s [1] != ':')
				g_error ("invalid type format at line %d\n", line);
			if (!image)
				break;
			klass = find_class (image, s + 2);
			break;
		case 'F':
			if (s [1] != ':')
				g_error ("invalid field format at line %d\n", line);
			if (!image || !klass)
				break;
			p = s + 2;
			if (*p == '*') {
				handle_type (klass, TYPE_FIELDS);
				break;
			}
			field = mono_class_get_field_from_name (klass, p);
			if (!field)
				g_warning ("no field '%s' at line %d\n", p, line);
			else
				add_field (field);
			break;
		case 'M':
			if (s [1] != ':')
				g_error ("invalid method format at line %d\n", line);
			if (!image || !klass)
				break;
			p = s + 2;
			if (*p == '*') {
				handle_type (klass, TYPE_METHODS);
				break;
			}
			mdesc = mono_method_desc_new (p, FALSE);
			if (!mdesc) {
				g_error ("invalid method desc at line %d\n", line);
			}
			method = mono_method_desc_search_in_class (mdesc, klass);
			if (!method)
				g_warning ("no method '%s' at line %d\n", p, line);
			else
				add_types_from_method (method);
			mono_method_desc_free (mdesc);
			break;
		default:
			g_error ("invalid format at line %d\n", line);
		}
	}
	fclose (file);
}

int
main (int argc, char *argv[]) {
	MonoAssembly *assembly = NULL;
	const char *aname = NULL;
	const char *outfile = NULL;
	const char *rootfile = NULL;
	int i;
	gboolean all_meta = FALSE;

	mono_init (argv [0]);

	type_table = g_hash_table_new (NULL, NULL);
	method_table = g_hash_table_new (NULL, NULL);
	field_table = g_hash_table_new (NULL, NULL);
	image_table = g_hash_table_new (NULL, NULL);

	for (i = 1; i < argc; ++i) {
		all_meta = FALSE;
		aname = argv [i];
		if (strcmp (aname, "-v") == 0) {
			verbose++;
			continue;
		} else if (strcmp (aname, "-e") == 0) {
			force_enums = 1;
			continue;
		} else if (strcmp (aname, "-h") == 0) {
			usage (0);
		} else if (strcmp (aname, "-o") == 0) {
			i++;
			if (i >= argc)
				usage (1);
			outfile = argv [i];
			continue;
		} else if (strcmp (aname, "-F") == 0) {
			i++;
			if (i >= argc)
				usage (1);
			all_meta = TRUE;
			aname = argv [i];
		} else if (strcmp (aname, "-a") == 0) {
			i++;
			if (i >= argc)
				usage (1);
			rootfile = argv [i];
			continue;
		}
		assembly = mono_assembly_open (aname, NULL);
		if (!assembly) {
			g_print ("cannot open assembly %s\n", aname);
			exit (1);
		}
		process_assembly (assembly, all_meta);
	}
	if (!assembly)
		usage (1);
	if (rootfile)
		load_roots (rootfile);
	process_images ();
	dump_images (outfile);

	return 0;
}


