#include <glib.h>
#include <string.h>
#include "mono/metadata/class.h"
#include "mono/metadata/assembly.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/cil-coff.h" /* MonoCLIImageInfo */
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/appdomain.h" /* mono_init */

static FILE *output;
static int include_namespace = 0;
static int max_depth = 6;
static int verbose = 0;
static char *graph_properties = "\tnode [fontsize=8.0]\n\tedge [len=2,color=red]\n";

static void
output_type_edge (MonoClass *first, MonoClass *second) {
	if (include_namespace)
		fprintf (output, "\t\"%s.%s\" -> \"%s.%s\"\n", first->name_space, first->name, second->name_space, second->name);
	else
		fprintf (output, "\t\"%s\" -> \"%s\"\n", first->name, second->name);
}

static void
print_subtypes (MonoImage *image, MonoClass *class, int depth) {
	int i, index;
	MonoTableInfo *t;
	MonoClass *child;

	if (depth++ > max_depth)
		return;

	t = &image->tables [MONO_TABLE_TYPEDEF];
	
	index = mono_metadata_token_index (class->type_token);
	index <<= TYPEDEFORREF_BITS;
	index |= TYPEDEFORREF_TYPEDEF;

	/* use a subgraph? */
	for (i = 0; i < t->rows; ++i) {
		if (index == mono_metadata_decode_row_col (t, i, MONO_TYPEDEF_EXTENDS)) {
			child = mono_class_get (image, MONO_TOKEN_TYPE_DEF | (i + 1));
			output_type_edge (class, child);
			print_subtypes (image, child, depth);
		}
	}
}

static void
type_graph (MonoImage *image, char* cname) {
	MonoClass *class;
	MonoClass *parent;
	MonoClass *child;
	char *name_space;
	char *p;
	int depth = 0;

	cname = g_strdup (cname);
	p = strrchr (cname, '.');
	if (p) {
		name_space = cname;
		*p++ = 0;
		cname = p;
	} else {
		name_space = "";
	}
	class = mono_class_from_name (image, name_space, cname);
	if (!class)
		g_error ("class %s.%s not found", name_space, cname);
	fprintf (output, "digraph blah {\n");
	fprintf (output, "%s", graph_properties);
	child = class;
	/* go back and print the parents for the node as well: not sure it's a good idea */
	for (parent = class->parent; parent; parent = parent->parent) {
		output_type_edge (parent, child);
		child = parent;
	}
	print_subtypes (image, class, depth);
	fprintf (output, "}\n");
}

static void
get_type (GString *res, MonoType *type) {
	switch (type->type) {
	case MONO_TYPE_VOID:
		g_string_append (res, "void"); break;
	case MONO_TYPE_CHAR:
		g_string_append (res, "char"); break;
	case MONO_TYPE_BOOLEAN:
		g_string_append (res, "bool"); break;
	case MONO_TYPE_U1:
		g_string_append (res, "byte"); break;
	case MONO_TYPE_I1:
		g_string_append (res, "sbyte"); break;
	case MONO_TYPE_U2:
		g_string_append (res, "uint16"); break;
	case MONO_TYPE_I2:
		g_string_append (res, "int16"); break;
	case MONO_TYPE_U4:
		g_string_append (res, "int"); break;
	case MONO_TYPE_I4:
		g_string_append (res, "unint"); break;
	case MONO_TYPE_U8:
		g_string_append (res, "ulong"); break;
	case MONO_TYPE_I8:
		g_string_append (res, "long"); break;
	case MONO_TYPE_FNPTR: /* who cares for the exact signature? */
		g_string_append (res, "*()"); break;
	case MONO_TYPE_U:
		g_string_append (res, "intptr"); break;
	case MONO_TYPE_I:
		g_string_append (res, "unintptr"); break;
	case MONO_TYPE_R4:
		g_string_append (res, "single"); break;
	case MONO_TYPE_R8:
		g_string_append (res, "double"); break;
	case MONO_TYPE_STRING:
		g_string_append (res, "string"); break;
	case MONO_TYPE_OBJECT:
		g_string_append (res, "object"); break;
	case MONO_TYPE_PTR:
		get_type (res, type->data.type);
		g_string_append_c (res, '*');
		break;
	case MONO_TYPE_ARRAY:
		get_type (res, type->data.array->type);
		g_string_append (res, "[,]"); /* not the full array info.. */
		break;
	case MONO_TYPE_SZARRAY:
		get_type (res, type->data.type);
		g_string_append (res, "[]");
		break;
	case MONO_TYPE_CLASS:
	case MONO_TYPE_VALUETYPE: {
		MonoClass *class = type->data.klass;
		if (!class) {
			g_string_append (res, "Unknown");
			break;
		}
		if (include_namespace && *(class->name_space))
			g_string_sprintfa (res, "%s.", class->name_space);
		g_string_sprintfa (res, "%s", class->name);
		break;
	}
	default:
		break;
	}
	if (type->byref)
		g_string_append_c (res, '&');
}

static char *
get_signature (MonoMethod *method) {
	GString *res;
	static GHashTable *hash = NULL;
	char *result;
	int i;

	if (!hash)
		hash = g_hash_table_new (g_direct_hash, g_direct_equal);
	if ((result = g_hash_table_lookup (hash, method)))
		return result;

	res = g_string_new ("");
	if (include_namespace && *(method->klass->name_space))
		g_string_sprintfa (res, "%s.", method->klass->name_space);
	g_string_sprintfa (res, "%s:%s(", method->klass->name, method->name);
	for (i = 0; i < method->signature->param_count; ++i) {
		if (i > 0)
			g_string_append_c (res, ',');
		get_type (res, method->signature->params [i]);
	}
	g_string_sprintfa (res, ")");
	g_hash_table_insert (hash, method, res->str);

	result = res->str;
	g_string_free (res, FALSE);
	return result;
		
}

static void
output_method_edge (MonoMethod *first, MonoMethod *second) {
	char * f = get_signature (first);
	char * s = get_signature (second);
	
	fprintf (output, "\t\"%s\" -> \"%s\"\n", f, s);
}

/*
 * We need to handle virtual calls is derived types.
 * We could check what types implement the method and
 * disassemble all of them: this can make the graph to explode.
 * We could try and keep track of the 'this' pointer type and
 * consider only the methods in the classes derived from that:
 * this can reduce the graph complexity somewhat (and it would 
 * be the more correct approach).
 */
static void
print_method (MonoMethod *method, int depth) {
	const MonoOpcode *opcode;
	MonoMethodHeader *header;
	GHashTable *hash;
	const unsigned char *ip;
	int i;

	if (depth++ > max_depth)
		return;
	if (method->info) /* avoid recursion */
		return;
	method->info = method;

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))
		return;
	if (method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))
		return;

	header = ((MonoMethodNormal *)method)->header;
	ip = header->code;

	hash = g_hash_table_new (g_direct_hash, g_direct_equal);
	
	while (ip < (header->code + header->code_size)) {
		if (*ip == 0xfe) {
			++ip;
			i = *ip + 256;
		} else {
			i = *ip;
		}

		opcode = &mono_opcodes [i];

		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineTok:
		case MonoInlineString:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
		case MonoInlineBrTarget:
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
		case MonoInlineSwitch: {
			gint32 n;
			++ip;
			n = read32 (ip);
			ip += 4;
			ip += 4 * n;
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		case MonoInlineMethod: {
			guint32 token;
			MonoMethod *called;
			ip++;
			token = read32 (ip);
			ip += 4;
			called = mono_get_method (method->klass->image, token, NULL);
			if (!called)
				break; /* warn? */
			if (g_hash_table_lookup (hash, called))
				break;
			g_hash_table_insert (hash, called, called);
			output_method_edge (method, called);
			print_method (called, depth);
			break;
		}
		default:
			g_assert_not_reached ();
		}
	}
	g_hash_table_destroy (hash);
}

static void
method_graph (MonoImage *image, char *name) {
	int depth = 0;
	MonoMethod *method = NULL;
	
	if (!name) {
		method = mono_get_method (image, ((MonoCLIImageInfo*)image->image_info)->cli_cli_header.ch_entry_point, NULL);
		if (!method)
			g_error ("Cannot find entry point");
	} else {
		/* search the method */
		MonoTableInfo *tdef = &image->tables [MONO_TABLE_TYPEDEF];
		MonoTableInfo *methods = &image->tables [MONO_TABLE_METHOD];
		char *class_name, *class_nspace, *method_name, *use_args;
		int use_namespace, i;
		
		class_nspace = g_strdup (name);
		use_args = strchr (class_nspace, '(');
		if (use_args)
			*use_args++ = 0;
		method_name = strrchr (class_nspace, ':');
		if (!method_name)
			g_error ("Invalid method name %s", name);
		*method_name++ = 0;
		class_name = strrchr (class_nspace, '.');
		if (class_name) {
			*class_name++ = 0;
			use_namespace = 1;
		} else {
			class_name = class_nspace;
			use_namespace = 0;
		}
		for (i = 0; i < methods->rows; ++i) {
			guint32 index = mono_metadata_decode_row_col (methods, i, MONO_METHOD_NAME);
			guint32 idx;
			const char *n = mono_metadata_string_heap (image, index);

			if (strcmp (n, method_name))
				continue;
			index = mono_metadata_typedef_from_method (image, i + 1);
			idx = mono_metadata_decode_row_col (tdef, index - 1, MONO_TYPEDEF_NAME);
			n = mono_metadata_string_heap (image, idx);
			if (strcmp (n, class_name))
				continue;
			if (use_namespace) {
				idx = mono_metadata_decode_row_col (tdef, index - 1, MONO_TYPEDEF_NAMESPACE);
				n = mono_metadata_string_heap (image, idx);
				if (strcmp (n, class_nspace))
					continue;
			}
			method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
			if (use_args) {
				/* check the signature */
				n = get_signature (method);
				if (strcmp (n, name) == 0)
					break;
				if (verbose)
					g_print ("signature check failed: '%s' != '%s'.\n", n, name);
				method = NULL;
			}
		}
		if (!method)
			g_error ("Cannot find method %s", name);
		g_free (class_nspace);
	}
	fprintf (output, "digraph blah {\n");
	fprintf (output, "%s", graph_properties);

	print_method (method, depth);
	
	fprintf (output, "}\n");
}

static void
usage () {
	printf ("monograph 0.1 Copyright (c) 2002 Ximian, Inc\n");
	printf ("Create call graph or type hierarchy information from CIL assemblies.\n");
	printf ("Usage: monograph [options] [assembly [typename|methodname]]\n");
	printf ("Valid options are:\n");
	printf ("\t-c|--call             output call graph instead of type hierarchy\n");
	printf ("\t-d|--depth num        max depth recursion (default: 6)\n");
	printf ("\t-o|--output filename  write graph to file filename (default: stdout)\n");
	printf ("\t-f|--fullname         include namespace in type and method names\n");
	printf ("\t-n|--neato            invoke neato directly\n");
	printf ("\t-v|--verbose          verbose operation\n");
	printf ("The default assembly is corlib.dll. The default method for\n");
	printf ("the --call option is the entrypoint.\n");
	printf ("When the --neato option is used the output type info is taken\n");
	printf ("from the output filename extension.\n");
	printf ("Sample runs:\n");
	printf ("\tmonograph -n -o vt.png corlib.dll System.ValueType\n");
	printf ("\tmonograph -n -o expr.png mcs.exe Mono.CSharp.Expression\n");
	printf ("\tmonograph -d 3 -n -o callgraph.png -c mis.exe\n");
	exit (1);
}

/*
 * TODO:
 * * virtual method calls as explained above
 * * maybe track field references
 * * track what exceptions a method could throw?
 * * for some inputs neato appears to hang or take a long time: kill it?
 * * allow passing additional command-line arguments to neato
 * * allow setting different graph/node/edge options directly
 * * option to avoid specialname methods
 * * make --neato option the default?
 * * use multiple classes/method roots?
 * * write a manpage
 */
int
main (int argc, char *argv[]) {
	MonoAssembly *assembly;
	char *cname = NULL;
	char *aname = NULL;
	char *outputfile = NULL;
	int callgraph = 0;
	int callneato = 0;
	int i;
	
	mono_init ();
	output = stdout;

	for (i = 1; i < argc; ++i) {
		if (argv [i] [0] != '-')
			break;
		if (strcmp (argv [i], "--call") == 0 || strcmp (argv [i], "-c") == 0) {
			callgraph = 1;
		} else if (strcmp (argv [i], "--fullname") == 0 || strcmp (argv [i], "-f") == 0) {
			include_namespace = 1;
		} else if (strcmp (argv [i], "--neato") == 0 || strcmp (argv [i], "-n") == 0) {
			callneato = 1;
		} else if (strcmp (argv [i], "--verbose") == 0 || strcmp (argv [i], "-v") == 0) {
			verbose++;
		} else if (strcmp (argv [i], "--output") == 0 || strcmp (argv [i], "-o") == 0) {
			if (i + 1 >= argc)
				usage ();
			outputfile = argv [++i];
		} else if (strcmp (argv [i], "--depth") == 0 || strcmp (argv [i], "-d") == 0) {
			if (i + 1 >= argc)
				usage ();
			max_depth = atoi (argv [++i]);
		} else {
			usage ();
		}
		
	}
	if (argc > i)
		aname = argv [i];
	if (argc > i + 1)
		cname = argv [i + 1];
	if (!aname)
		aname = "corlib.dll";
	if (!cname && !callgraph)
		cname = "System.Object";

	assembly = mono_assembly_open (aname, NULL, NULL);
	if (!assembly)
		g_error ("cannot open assembly %s", aname);

	if (callneato) {
		GString *command = g_string_new ("neato");
		char *type = NULL;

		if (outputfile) {
			type = strrchr (outputfile, '.');
			g_string_sprintfa (command, " -o %s", outputfile);
		}
		if (type)
			g_string_sprintfa (command, " -T%s", type + 1);
		output = popen (command->str, "w");
		if (!output)
			g_error ("Cannot run neato");
	} else if (outputfile) {
		output = fopen (outputfile, "w");
		if (!output)
			g_error ("Cannot open file: %s", outputfile);
	}
	/* if it looks like a method name, we want a callgraph. */
	if (cname && strchr (cname, ':'))
		callgraph = 1;

	if (callgraph)
		method_graph (assembly->image, cname);
	else
		type_graph (assembly->image, cname);
	if (callneato) {
		if (verbose)
			g_print ("waiting for neato.\n");
		pclose (output);
	} else if (outputfile)
		fclose (output);
	return 0;
}


