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
#include "mono/metadata/debug-helpers.h"

static FILE *output;
static int include_namespace = 0;
static int max_depth = 6;
static int verbose = 0;
static const char *graph_properties = "\tnode [fontsize=8.0]\n\tedge [len=2,color=red]\n";

static void
output_type_edge (MonoClass *first, MonoClass *second) {
	if (include_namespace)
		fprintf (output, "\t\"%s.%s\" -> \"%s.%s\"\n", first->name_space, first->name, second->name_space, second->name);
	else
		fprintf (output, "\t\"%s\" -> \"%s\"\n", first->name, second->name);
}

static void
print_subtypes (MonoImage *image, MonoClass *class, int depth) {
	int i, token;
	MonoTableInfo *t;
	MonoClass *child;

	if (depth++ > max_depth)
		return;

	t = &image->tables [MONO_TABLE_TYPEDEF];
	
	token = mono_metadata_token_index (class->type_token);
	token <<= TYPEDEFORREF_BITS;
	token |= TYPEDEFORREF_TYPEDEF;

	/* use a subgraph? */
	for (i = 0; i < t->rows; ++i) {
		if (token == mono_metadata_decode_row_col (t, i, MONO_TYPEDEF_EXTENDS)) {
			child = mono_class_get (image, MONO_TOKEN_TYPE_DEF | (i + 1));
			output_type_edge (class, child);
			print_subtypes (image, child, depth);
		}
	}
}

static void
type_graph (MonoImage *image, const char* cname) {
	MonoClass *class;
	MonoClass *parent;
	MonoClass *child;
	const char *name_space;
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
	if (!class) {
		g_print ("class %s.%s not found\n", name_space, cname);
		exit (1);
	}
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
interface_graph (MonoImage *image, const char* cname) {
	MonoClass *class;
	MonoClass *child;
	const char *name_space;
	char *p;
	guint32 cols [MONO_INTERFACEIMPL_SIZE];
	guint32 token, i, count = 0;
	MonoTableInfo *intf = &image->tables [MONO_TABLE_INTERFACEIMPL];

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
	if (!class) {
		g_print ("interface %s.%s not found\n", name_space, cname);
		exit (1);
	}
	/* chek if it's really an interface... */
	fprintf (output, "digraph interface {\n");
	fprintf (output, "%s", graph_properties);
	/* TODO: handle inetrface defined in one image and class defined in another. */
	token = mono_metadata_token_index (class->type_token);
	token <<= TYPEDEFORREF_BITS;
	token |= TYPEDEFORREF_TYPEDEF;
	for (i = 0; i < intf->rows; ++i) {
		mono_metadata_decode_row (intf, i, cols, MONO_INTERFACEIMPL_SIZE);
		/*g_print ("index: %d [%d implements %d]\n", index, cols [MONO_INTERFACEIMPL_CLASS], cols [MONO_INTERFACEIMPL_INTERFACE]);*/
		if (token == cols [MONO_INTERFACEIMPL_INTERFACE]) {
			child = mono_class_get (image, MONO_TOKEN_TYPE_DEF | cols [MONO_INTERFACEIMPL_CLASS]);
			output_type_edge (class, child);
			count++;
		}
	}
	fprintf (output, "}\n");
	if (verbose && !count)
		g_print ("No class implements %s.%s\n", class->name_space, class->name);

}

static char *
get_signature (MonoMethod *method) {
	GString *res;
	static GHashTable *hash = NULL;
	char *result;

	if (!hash)
		hash = g_hash_table_new (g_direct_hash, g_direct_equal);
	if ((result = g_hash_table_lookup (hash, method)))
		return result;

	res = g_string_new ("");
	if (include_namespace && *(method->klass->name_space))
		g_string_sprintfa (res, "%s.", method->klass->name_space);
	result = mono_signature_get_desc (method->signature, include_namespace);
	g_string_sprintfa (res, "%s:%s(%s)", method->klass->name, method->name, result);
	g_free (result);
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
method_graph (MonoImage *image, const char *name) {
	int depth = 0;
	MonoMethod *method = NULL;
	
	if (!name) {
		guint32 token = ((MonoCLIImageInfo*)image->image_info)->cli_cli_header.ch_entry_point;
		if (!token || !(method = mono_get_method (image, token, NULL))) {
			g_print ("Cannot find entry point in %s: specify an explict method name.\n", image->name);
			exit (1);
		}
	} else {
		/* search the method */
		MonoMethodDesc *desc;

		desc = mono_method_desc_new (name, include_namespace);
		if (!desc) {
			g_print ("Invalid method name %s\n", name);
			exit (1);
		}
		method = mono_method_desc_search_in_image (desc, image);
		if (!method) {
			g_print ("Cannot find method %s\n", name);
			exit (1);
		}
	}
	fprintf (output, "digraph blah {\n");
	fprintf (output, "%s", graph_properties);

	print_method (method, depth);
	
	fprintf (output, "}\n");
}

typedef struct MonoBasicBlock MonoBasicBlock;

struct MonoBasicBlock {
	const unsigned char* cil_code;
	gint32 cil_length;
	gint dfn;
	GList *in_bb;
	GList *out_bb;
};

static const unsigned char *debug_start;

static void
link_bblock (MonoBasicBlock *from, MonoBasicBlock* to)
{
	from->out_bb = g_list_prepend (from->out_bb, to);
	to->in_bb = g_list_prepend (to->in_bb, from);
	/*fprintf (stderr, "linking IL_%04x to IL_%04x\n", from->cil_code-debug_start, to->cil_code-debug_start);*/
}

static int
compare_bblock (void *a, void *b)
{
	MonoBasicBlock **ab = a;
	MonoBasicBlock **bb = b;

	return (*ab)->cil_code - (*bb)->cil_code;
}

static GPtrArray*
mono_method_find_bblocks (MonoMethodHeader *header)
{
	const unsigned char *ip, *end, *start;
	const MonoOpcode *opcode;
	int i, block_end = 0;
	GPtrArray *result = g_ptr_array_new ();
	GHashTable *table = g_hash_table_new (g_direct_hash, g_direct_equal);
	MonoBasicBlock *entry_bb, *end_bb, *bb, *target;

	ip = header->code;
	end = ip + header->code_size;
	debug_start = ip;

	entry_bb = g_new0 (MonoBasicBlock, 1);
	end_bb = g_new0 (MonoBasicBlock, 1);
	g_ptr_array_add (result, entry_bb);
	g_ptr_array_add (result, end_bb);

	bb = g_new0 (MonoBasicBlock, 1);
	bb->cil_code = ip;
	g_ptr_array_add (result, bb);
	link_bblock (entry_bb, bb);
	g_hash_table_insert (table, (char*)ip, bb);
	block_end = TRUE;

	/* handle exception code blocks... */
	while (ip < end) {
		start = ip;
		if ((target = g_hash_table_lookup (table, ip)) && target != bb) {
			if (!block_end)
				link_bblock (bb, target);
			bb = target;
			block_end = FALSE;
		}
		if (block_end) {
			/*fprintf (stderr, "processing bbclok at IL_%04x\n", ip - header->code);*/
			if (!(bb = g_hash_table_lookup (table, ip))) {
				bb = g_new0 (MonoBasicBlock, 1);
				bb->cil_code = ip;
				g_ptr_array_add (result, bb);
				g_hash_table_insert (table, (char*)ip, bb);
			}
			block_end = FALSE;
		}
		if (*ip == 0xfe) {
			++ip;
			i = *ip + 256;
		} else {
			i = *ip;
		}

		opcode = &mono_opcodes [i];
		switch (opcode->flow_type) {
		case MONO_FLOW_RETURN:
			link_bblock (bb, end_bb);
		case MONO_FLOW_ERROR:
			block_end = 1;
			break;
		case MONO_FLOW_BRANCH: /* we handle branch when checking the argument type */
		case MONO_FLOW_COND_BRANCH:
		case MONO_FLOW_CALL:
		case MONO_FLOW_NEXT:
		case MONO_FLOW_META:
			break;
		default:
			g_assert_not_reached ();
		}
		switch (opcode->argument) {
		case MonoInlineNone:
			++ip;
			break;
		case MonoInlineType:
		case MonoInlineField:
		case MonoInlineMethod:
		case MonoInlineTok:
		case MonoInlineString:
		case MonoInlineSig:
		case MonoShortInlineR:
		case MonoInlineI:
			ip += 5;
			break;
		case MonoInlineVar:
			ip += 3;
			break;
		case MonoShortInlineVar:
		case MonoShortInlineI:
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
		case MonoInlineBrTarget:
			ip++;
			if (opcode->argument == MonoShortInlineBrTarget) {
				i = (signed char)*ip;
				ip++;
			} else {
				i = (gint32) read32 (ip);
				ip += 4;
			}
			if (opcode->flow_type == MONO_FLOW_COND_BRANCH) {
				if (!(target = g_hash_table_lookup (table, ip))) {
					target = g_new0 (MonoBasicBlock, 1);
					target->cil_code = ip;
					g_ptr_array_add (result, target);
					g_hash_table_insert (table, (char*)ip, target);
				}
				link_bblock (bb, target);
			}
			if (!(target = g_hash_table_lookup (table, ip + i))) {
				target = g_new0 (MonoBasicBlock, 1);
				target->cil_code = ip + i;
				g_ptr_array_add (result, target);
				g_hash_table_insert (table, (char*)ip + i, target);
			}
			link_bblock (bb, target);
			block_end = 1;
			break;
		case MonoInlineSwitch: {
			gint32 n;
			const char *itarget, *st;
			++ip;
			n = read32 (ip);
			ip += 4;
			st = (const char*)ip + 4 * n;

			for (i = 0; i < n; i++) {
				itarget = st + read32 (ip);
				ip += 4;
				if (!(target = g_hash_table_lookup (table, itarget))) {
					target = g_new0 (MonoBasicBlock, 1);
					target->cil_code = itarget;
					g_ptr_array_add (result, target);
					g_hash_table_insert (table, itarget, target);
				}
				link_bblock (bb, target);
			}
			/*
			 * Note: the code didn't set block_end in switch.
			 */
			break;
		}
		case MonoInlineR:
		case MonoInlineI8:
			ip += 9;
			break;
		default:
			g_assert_not_reached ();
		}

	}
	g_hash_table_destroy (table);
	qsort (result->pdata, result->len, sizeof (gpointer), compare_bblock);
	/* skip entry and end */
	bb = target = NULL;
	for (i = 2; i < result->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (result, i);
		if (target)
			target->cil_length = bb->cil_code - target->cil_code;
		target = bb;
		/*fprintf (stderr, "bblock %d at IL_%04x:\n", i, bb->cil_code - header->code);*/
	}
	bb->cil_length = header->code + header->code_size - bb->cil_code;
	return result;
}

static char*
indenter (MonoDisHelper *dh, MonoMethod *method, guint32 ip_offset)
{
	return g_strdup (" ");
}

static MonoDisHelper graph_dh = {
	"\\l",
	NULL,
	"IL_%04x",
	indenter, 
	NULL,
	NULL
};

static void
df_visit (MonoBasicBlock *bb, int *dfn, const unsigned char* code)
{
	GList *tmp;
	MonoBasicBlock *next;
	
	if (bb->dfn)
		return;
	++(*dfn);
	bb->dfn = *dfn;
	for (tmp = bb->out_bb; tmp; tmp = tmp->next) {
		next = tmp->data;
		if (!next->dfn) {
			if (!bb->cil_code)
				fprintf (output, "\t\"DF entry\" -> \"IL_%04x (%d)\"\n", next->cil_code - code, *dfn + 1);
			else
				fprintf (output, "\t\"IL_%04x (%d)\" -> \"IL_%04x (%d)\"\n", bb->cil_code - code, bb->dfn, next->cil_code - code, *dfn + 1);
			df_visit (next, dfn, code);
		}
	}
}

static void
print_method_cfg (MonoMethod *method) {
	GPtrArray *bblocks;
	GList *tmp;
	MonoBasicBlock *bb, *target;
	MonoMethodHeader *header;
	int i, dfn;
	char *code;

	header = ((MonoMethodNormal*)method)->header;
	bblocks = mono_method_find_bblocks (header);
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		if (i == 0)
			fprintf (output, "\tB%p [shape=record,label=\"entry\"]\n", bb);
		else if (i == 1)
			fprintf (output, "\tB%p [shape=record,label=\"end\"]\n", bb);
		else {
			code = mono_disasm_code (&graph_dh, method, bb->cil_code, bb->cil_code + bb->cil_length);
			fprintf (output, "\tB%p [shape=record,label=\"IL_%04x\\n%s\"]\n", bb, bb->cil_code - header->code, code);
			g_free (code);
		}
	}
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		for (tmp = bb->out_bb; tmp; tmp = tmp->next) {
			target = tmp->data;
			fprintf (output, "\tB%p -> B%p\n", bb, target);
		}
	}
#if 0
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		bb->dfn = 0;
	}
	dfn = 0;
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		df_visit (bb, &dfn, header->code);
	}
#endif
}

/*
 * TODO: change to create the DF tree, dominance relation etc.
 */
static void
method_cfg (MonoImage *image, const char *name) {
	MonoMethod *method = NULL;
	const static char *cfg_graph_properties = "\tnode [fontsize=8.0]\n\tedge [len=1.5,color=red]\n";
	
	if (!name) {
		guint32 token = ((MonoCLIImageInfo*)image->image_info)->cli_cli_header.ch_entry_point;
		if (!token || !(method = mono_get_method (image, token, NULL))) {
			g_print ("Cannot find entry point in %s: specify an explict method name.\n", image->name);
			exit (1);
		}
	} else {
		/* search the method */
		MonoMethodDesc *desc;

		desc = mono_method_desc_new (name, include_namespace);
		if (!desc) {
			g_print ("Invalid method name %s\n", name);
			exit (1);
		}
		method = mono_method_desc_search_in_image (desc, image);
		if (!method) {
			g_print ("Cannot find method %s\n", name);
			exit (1);
		}
	}
	fprintf (output, "digraph blah {\n");
	fprintf (output, "%s", cfg_graph_properties);

	print_method_cfg (method);
	
	fprintf (output, "}\n");
}

static void
usage (void) {
	printf ("monograph 0.2 Copyright (c) 2002 Ximian, Inc\n");
	printf ("Create call graph or type hierarchy information from CIL assemblies.\n");
	printf ("Usage: monograph [options] [assembly [typename|methodname]]\n");
	printf ("Valid options are:\n");
	printf ("\t-c|--call             output call graph instead of type hierarchy\n");
	printf ("\t-C|--control-flow     output control flow of methodname\n");
	printf ("\t-d|--depth num        max depth recursion (default: 6)\n");
	printf ("\t-o|--output filename  write graph to file filename (default: stdout)\n");
	printf ("\t-f|--fullname         include namespace in type and method names\n");
	printf ("\t-n|--neato            invoke neato directly\n");
	printf ("\t-v|--verbose          verbose operation\n");
	printf ("The default assembly is corlib.dll. The default method for\n");
	printf ("the --call and --control-flow options is the entrypoint.\n");
	printf ("When the --neato option is used the output type info is taken\n");
	printf ("from the output filename extension. You need the graphviz package installed\n");
	printf ("to be able to use this option.\n");
	printf ("Sample runs:\n");
	printf ("\tmonograph -n -o vt.png corlib.dll System.ValueType\n");
	printf ("\tmonograph -n -o expr.png mcs.exe Mono.CSharp.Expression\n");
	printf ("\tmonograph -n -o cfg.png -C mcs.exe Driver:Main\n");
	printf ("\tmonograph -d 3 -n -o callgraph.png -c mis.exe\n");
	exit (1);
}

enum {
	GRAPH_TYPES,
	GRAPH_CALL,
	GRAPH_INTERFACE,
	GRAPH_CONTROL_FLOW
};

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
 * * reverse call graph: given a method what methods call it?
 */
int
main (int argc, char *argv[]) {
	MonoAssembly *assembly;
	const char *cname = NULL;
	const char *aname = NULL;
	char *outputfile = NULL;
	int graphtype = GRAPH_TYPES;
	int callneato = 0;
	int i;
	
	mono_init (argv [0]);
	output = stdout;

	for (i = 1; i < argc; ++i) {
		if (argv [i] [0] != '-')
			break;
		if (strcmp (argv [i], "--call") == 0 || strcmp (argv [i], "-c") == 0) {
			graphtype = GRAPH_CALL;
		} else if (strcmp (argv [i], "--control-flow") == 0 || strcmp (argv [i], "-C") == 0) {
			graphtype = GRAPH_CONTROL_FLOW;
		} else if (strcmp (argv [i], "--interface") == 0 || strcmp (argv [i], "-i") == 0) {
			graphtype = GRAPH_INTERFACE;
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
	if (!cname && (graphtype == GRAPH_TYPES))
		cname = "System.Object";

	assembly = mono_assembly_open (aname, NULL, NULL);
	if (!assembly) {
		g_print ("cannot open assembly %s\n", aname);
		exit (1);
	}

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
		if (!output) {
			g_print ("Cannot run neato: you may need to install the graphviz package.\n");
			exit (1);
		}
	} else if (outputfile) {
		output = fopen (outputfile, "w");
		if (!output) {
			g_print ("Cannot open file: %s\n", outputfile);
			exit (1);
		}
	}
	/* if it looks like a method name, we want a callgraph. */
	if (cname && strchr (cname, ':') && graphtype == GRAPH_TYPES)
		graphtype = GRAPH_CALL;

	switch (graphtype) {
	case GRAPH_TYPES:
		type_graph (assembly->image, cname);
		break;
	case GRAPH_CALL:
		method_graph (assembly->image, cname);
		break;
	case GRAPH_INTERFACE:
		interface_graph (assembly->image, cname);
		break;
	case GRAPH_CONTROL_FLOW:
		method_cfg (assembly->image, cname);
		break;
	default:
		g_error ("wrong graph type");
	}
	
	if (callneato) {
		if (verbose)
			g_print ("waiting for neato.\n");
		pclose (output);
	} else if (outputfile)
		fclose (output);
	return 0;
}


