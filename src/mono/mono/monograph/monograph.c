#include <glib.h>
#include <string.h>
#include <math.h>
#include "mono/metadata/metadata-internals.h"
#include "mono/metadata/class-internals.h"
#include "mono/metadata/assembly.h"
#include "mono/metadata/tokentype.h"
#include "mono/metadata/opcodes.h"
#include "mono/metadata/tabledefs.h"
#include "mono/metadata/mono-endian.h"
#include "mono/metadata/appdomain.h" /* mono_init */
#include "mono/metadata/debug-helpers.h"
#include "mono/utils/mono-compiler.h"

static FILE *output;
static int include_namespace = 0;
static int max_depth = 6;
static int verbose = 0;
static const char *graph_properties = "\tnode [fontsize=8.0]\n\tedge [len=2,color=red]\n";

#if defined(__native_client__) || defined(__native_client_codegen__)
volatile int __nacl_thread_suspension_needed = 0;
void __nacl_suspend_thread_if_needed() {}
#endif

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
	const MonoTableInfo *t;
	MonoClass *child;

	if (depth++ > max_depth)
		return;

	t = mono_image_get_table_info (image, MONO_TABLE_TYPEDEF);
	
	token = mono_metadata_token_index (class->type_token);
	token <<= MONO_TYPEDEFORREF_BITS;
	token |= MONO_TYPEDEFORREF_TYPEDEF;

	/* use a subgraph? */
	for (i = 0; i < mono_table_info_get_rows (t); ++i) {
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
	const MonoTableInfo *intf = mono_image_get_table_info (image, MONO_TABLE_INTERFACEIMPL);

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
	token <<= MONO_TYPEDEFORREF_BITS;
	token |= MONO_TYPEDEFORREF_TYPEDEF;
	for (i = 0; i < mono_table_info_get_rows (intf); ++i) {
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

static int back_branch_waste = 0;
static int branch_waste = 0;
static int var_waste = 0;
static int int_waste = 0;
static int nop_waste = 0;
static int has_exceptions = 0;
static int num_exceptions = 0;
static int max_exceptions = 0;
static int has_locals = 0;
static int num_locals = 0;
static int max_locals = 0;
static int has_args = 0;
static int num_args = 0;
static int max_args = 0;
static int has_maxstack = 0;
static int num_maxstack = 0;
static int max_maxstack = 0;
static int has_code = 0;
static int num_code = 0;
static int max_code = 0;
static int has_branch = 0;
static int num_branch = 0;
static int max_branch = 0;
static int has_condbranch = 0;
static int num_condbranch = 0;
static int max_condbranch = 0;
static int has_calls = 0;
static int num_calls = 0;
static int max_calls = 0;
static int has_throw = 0;
static int num_throw = 0;
static int max_throw = 0;
static int has_switch = 0;
static int num_switch = 0;
static int max_switch = 0;
static int cast_sealed = 0;
static int cast_iface = 0;
static int total_cast = 0;
static int nonvirt_callvirt = 0;
static int iface_callvirt = 0;
static int total_callvirt = 0;

static void
method_stats (MonoMethod *method) {
	const MonoOpcode *opcode;
	MonoMethodHeader *header;
	MonoMethodSignature *sig;
	const unsigned char *ip, *il_code_end;
	guint32 i, n;
	int local_branch = 0, local_condbranch = 0, local_throw = 0, local_calls = 0;
	gint64 l;

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))
		return;
	if (method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))
		return;

	header = mono_method_get_header (method);
	n = mono_method_header_get_num_clauses (header);
	if (n)
		has_exceptions++;
	num_exceptions += n;
	if (max_exceptions < n)
		max_exceptions = n;
	mono_method_header_get_locals (header, &n, NULL);
	if (n)
		has_locals++;
	num_locals += n;
	if (max_locals < n)
		max_locals = n;

	ip = mono_method_header_get_code (header, &n, &i);
	il_code_end = ip + n;
	if (max_maxstack < i)
		max_maxstack = i;
	num_maxstack += i;
	if (i != 8) /* just a guess */
		has_maxstack++;

	sig = mono_method_signature (method);
	n = sig->hasthis + sig->param_count;
	if (max_args < n)
		max_args = n;
	num_args += n;
	if (n)
		has_args++;

	has_code++;
	if (max_code < il_code_end - ip)
		max_code = il_code_end - ip;
	num_code += il_code_end - ip;

	while (ip < il_code_end) {
		if (*ip == 0xfe) {
			++ip;
			i = *ip + 256;
		} else {
			i = *ip;
		}

		opcode = &mono_opcodes [i];

		switch (opcode->argument) {
		case MonoInlineNone:
			if (i == MONO_CEE_NOP)
				nop_waste++;
			++ip;
			break;
		case MonoInlineI:
			n = read32 (ip + 1);
			if (n >= -1 && n <= 8) {
				int_waste += 4;
				g_print ("%s %d\n", mono_opcode_name (i), n);
			} else if (n < 128 && n >= -128) {
				int_waste += 3;
				g_print ("%s %d\n", mono_opcode_name (i), n);
			}
			ip += 5;
			break;
		case MonoInlineType:
			if (i == MONO_CEE_CASTCLASS || i == MONO_CEE_ISINST) {
				guint32 token = read32 (ip + 1);
				MonoClass *k = mono_class_get (method->klass->image, token);
				if (k && k->flags & TYPE_ATTRIBUTE_SEALED)
					cast_sealed++;
				if (k && k->flags & TYPE_ATTRIBUTE_INTERFACE)
					cast_iface++;
				total_cast++;
			}
			ip += 5;
			break;
		case MonoInlineField:
		case MonoInlineTok:
		case MonoInlineString:
		case MonoInlineSig:
		case MonoShortInlineR:
			ip += 5;
			break;
		case MonoInlineBrTarget:
			n = read32 (ip + 1);
			if (n < 128 && n >= -128) {
				branch_waste += 3;
				if (n < 0)
					back_branch_waste += 3;
			}
			ip += 5;
			break;
		case MonoInlineVar:
			n = read16 (ip + 1);
			if (n < 256) {
				if (n < 4) {
					switch (i) {
					case MONO_CEE_LDARG:
					case MONO_CEE_LDLOC:
					case MONO_CEE_STLOC:
						var_waste += 3;
						/*g_print ("%s %d\n", mono_opcode_name (i), n);*/
						break;
					default:
						var_waste += 2;
						/*g_print ("%s %d\n", mono_opcode_name (i), n);*/
						break;
					}
				} else {
					var_waste += 2;
					/*g_print ("%s %d\n", mono_opcode_name (i), n);*/
				}
			}
			ip += 3;
			break;
		case MonoShortInlineVar:
			if ((signed char)ip [1] < 4 && (signed char)ip [1] >= 0) {
				switch (i) {
				case MONO_CEE_LDARG_S:
				case MONO_CEE_LDLOC_S:
				case MONO_CEE_STLOC_S:
					var_waste++;
					/*g_print ("%s %d\n", mono_opcode_name (i), (signed char)ip [1]);*/
					break;
				default:
					break;
				}
			}
			ip += 2;
			break;
		case MonoShortInlineI:
			if ((signed char)ip [1] <= 8 && (signed char)ip [1] >= -1) {
				/*g_print ("%s %d\n", mono_opcode_name (i), (signed char)ip [1]);*/
				int_waste ++;
			}
			ip += 2;
			break;
		case MonoShortInlineBrTarget:
			ip += 2;
			break;
		case MonoInlineSwitch: {
			gint32 n;
			++ip;
			n = read32 (ip);
			ip += 4;
			ip += 4 * n;
			num_switch += n;
			has_switch++;
			if (max_switch < n)
				max_switch = n;
			break;
		}
		case MonoInlineR:
			ip += 9;
			break;
		case MonoInlineI8:
			l = read64 (ip + 1);
			/* should load and convert */
			if (l >= -1 && l <= 8) {
				int_waste += 7;
			} else if (l < 128 && l >= -128) {
				int_waste += 6;
			} else if (l <= 2147483647 && l >= (-2147483647 -1)) {
				int_waste += 3;
			}
			ip += 9;
			break;
		case MonoInlineMethod:
			if (i == MONO_CEE_CALLVIRT) {
				MonoMethod *cm = mono_get_method (method->klass->image, read32 (ip + 1), NULL);
				if (cm && !(cm->flags & METHOD_ATTRIBUTE_VIRTUAL))
					nonvirt_callvirt++;
				if (cm && (cm->klass->flags & TYPE_ATTRIBUTE_INTERFACE))
					iface_callvirt++;
				total_callvirt++;
			}
			ip += 5;
			break;
		default:
			g_assert_not_reached ();
		}

		switch (opcode->flow_type) {
		case MONO_FLOW_BRANCH:
			local_branch++;
			break;
		case MONO_FLOW_COND_BRANCH:
			local_condbranch++;
			break;
		case MONO_FLOW_CALL:
			local_calls++;
			break;
		case MONO_FLOW_ERROR:
			local_throw++;
			break;
		}
	}
	
	if (local_branch)
		has_branch++;
	if (max_branch < local_branch)
		max_branch = local_branch;
	num_branch += local_branch;

	if (local_condbranch)
		has_condbranch++;
	if (max_condbranch < local_condbranch)
		max_condbranch = local_condbranch;
	num_condbranch += local_condbranch;

	if (local_calls)
		has_calls++;
	if (max_calls < local_calls)
		max_calls = local_calls;
	num_calls += local_calls;

	if (local_throw)
		has_throw++;
	if (max_throw < local_throw)
		max_throw = local_throw;
	num_throw += local_throw;

	return;
}

static int num_pdepth = 0;
static int max_pdepth = 0;
static int num_pdepth_ovf = 0;
static int num_ifaces = 0;
static int *pdepth_array = NULL;
static int pdepth_array_size = 0;
static int pdepth_array_next = 0;

static void
type_stats (MonoClass *klass) {
	MonoClass *parent;
	int depth = 1;

	if (klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
		num_ifaces++;
		return;
	}
	parent = klass->parent;
	while (parent) {
		depth++;
		parent = parent->parent;
	}
	if (pdepth_array_next >= pdepth_array_size) {
		pdepth_array_size *= 2;
		if (!pdepth_array_size)
			pdepth_array_size = 128;
		pdepth_array = g_realloc (pdepth_array, pdepth_array_size * sizeof (int));
	}
	pdepth_array [pdepth_array_next++] = depth;
	num_pdepth += depth;
	if (max_pdepth < depth)
		max_pdepth = depth;
	if (depth > MONO_DEFAULT_SUPERTABLE_SIZE) {
		/*g_print ("overflow parent depth: %s.%s\n", klass->name_space, klass->name);*/
		num_pdepth_ovf++;
	}
}

static void
stats (MonoImage *image, const char *name) {
	int i, num_methods, num_types;
	MonoMethod *method;
	MonoClass *klass;
	
	num_methods = mono_image_get_table_rows (image, MONO_TABLE_METHOD);
	for (i = 0; i < num_methods; ++i) {
		method = mono_get_method (image, MONO_TOKEN_METHOD_DEF | (i + 1), NULL);
		method_stats (method);
	}
	num_types = mono_image_get_table_rows (image, MONO_TABLE_TYPEDEF);
	for (i = 0; i < num_types; ++i) {
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | (i + 1));
		type_stats (klass);
	}

	g_print ("Methods and code stats:\n");
	g_print ("back branch waste: %d\n", back_branch_waste);
	g_print ("branch waste: %d\n", branch_waste);
	g_print ("var waste: %d\n", var_waste);
	g_print ("int waste: %d\n", int_waste);
	g_print ("nop waste: %d\n", nop_waste);
	g_print ("has exceptions: %d/%d, total: %d, max: %d, mean: %f\n", has_exceptions, num_methods, num_exceptions, max_exceptions, num_exceptions/(double)has_exceptions);
	g_print ("has locals: %d/%d, total: %d, max: %d, mean: %f\n", has_locals, num_methods, num_locals, max_locals, num_locals/(double)has_locals);
	g_print ("has args: %d/%d, total: %d, max: %d, mean: %f\n", has_args, num_methods, num_args, max_args, num_args/(double)has_args);
	g_print ("has maxstack: %d/%d, total: %d, max: %d, mean: %f\n", has_maxstack, num_methods, num_maxstack, max_maxstack, num_maxstack/(double)i);
	g_print ("has code: %d/%d, total: %d, max: %d, mean: %f\n", has_code, num_methods, num_code, max_code, num_code/(double)has_code);
	g_print ("has branch: %d/%d, total: %d, max: %d, mean: %f\n", has_branch, num_methods, num_branch, max_branch, num_branch/(double)has_branch);
	g_print ("has condbranch: %d/%d, total: %d, max: %d, mean: %f\n", has_condbranch, num_methods, num_condbranch, max_condbranch, num_condbranch/(double)has_condbranch);
	g_print ("has switch: %d/%d, total: %d, max: %d, mean: %f\n", has_switch, num_methods, num_switch, max_switch, num_switch/(double)has_switch);
	g_print ("has calls: %d/%d, total: %d, max: %d, mean: %f\n", has_calls, num_methods, num_calls, max_calls, num_calls/(double)has_calls);
	g_print ("has throw: %d/%d, total: %d, max: %d, mean: %f\n", has_throw, num_methods, num_throw, max_throw, num_throw/(double)has_throw);
	g_print ("sealed type cast: %d/%d\n", cast_sealed, total_cast);
	g_print ("interface type cast: %d/%d\n", cast_iface, total_cast);
	g_print ("non virtual callvirt: %d/%d\n", nonvirt_callvirt, total_callvirt);
	g_print ("interface callvirt: %d/%d\n", iface_callvirt, total_callvirt);
	
	g_print ("\nType stats:\n");
	g_print ("interface types: %d/%d\n", num_ifaces, num_types);
	{
		double mean = 0;
		double stddev = 0;
		if (pdepth_array_next) {
			int i;
			mean = (double)num_pdepth/pdepth_array_next;
			for (i = 0; i < pdepth_array_next; ++i) {
				stddev += (pdepth_array [i] - mean) * (pdepth_array [i] - mean);
			}
			stddev = sqrt (stddev/pdepth_array_next);
		}
		g_print ("parent depth: max: %d, mean: %f, sttdev: %f, overflowing: %d\n", max_pdepth, mean, stddev, num_pdepth_ovf);
	}
}

static void
type_size_stats (MonoClass *klass)
{
	int code_size = 0;
	MonoMethod *method;
	MonoMethodHeader *header;
	gpointer iter;

	iter = NULL;
	while ((method = mono_class_get_methods (klass, &iter))) {
		guint32 size, maxs;
		header = mono_method_get_header (method);
		if (!header)
			continue;
		mono_method_header_get_code (header, &size, &maxs);
		code_size += size;
	}
	g_print ("%s.%s: code: %d\n", klass->name_space, klass->name, code_size);
}

static void
size_stats (MonoImage *image, const char *name) {
	int i, num_types;
	MonoClass *klass;
	
	num_types = mono_image_get_table_rows (image, MONO_TABLE_TYPEDEF);
	for (i = 0; i < num_types; ++i) {
		klass = mono_class_get (image, MONO_TOKEN_TYPE_DEF | (i + 1));
		type_size_stats (klass);
	}
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
		g_string_append_printf (res, "%s.", method->klass->name_space);
	result = mono_signature_get_desc (mono_method_signature (method), include_namespace);
	g_string_append_printf (res, "%s:%s(%s)", method->klass->name, method->name, result);
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
	static GHashTable *visited = NULL;
	const unsigned char *ip, *il_code_end;
	guint32 i;

	if (depth++ > max_depth)
		return;
	
	if (! visited)
		visited = g_hash_table_new (NULL, NULL);
	
	if (g_hash_table_lookup (visited, method))
		return;
	
	g_hash_table_insert (visited, method, method);

	if (method->iflags & (METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL | METHOD_IMPL_ATTRIBUTE_RUNTIME))
		return;
	if (method->flags & (METHOD_ATTRIBUTE_PINVOKE_IMPL | METHOD_ATTRIBUTE_ABSTRACT))
		return;

	header = mono_method_get_header (method);
	ip = mono_method_header_get_code (header, &i, NULL);
	il_code_end = ip + i;

	hash = g_hash_table_new (g_direct_hash, g_direct_equal);
	
	while (ip < il_code_end) {
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
		guint32 token = mono_image_get_entry_point (image);
		if (!token || !(method = mono_get_method (image, token, NULL))) {
			g_print ("Cannot find entry point in %s: specify an explict method name.\n", mono_image_get_filename (image));
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
compare_bblock (const void *a, const void *b)
{
	MonoBasicBlock * const *ab = a;
	MonoBasicBlock * const *bb = b;

	return (*ab)->cil_code - (*bb)->cil_code;
}

static GPtrArray*
mono_method_find_bblocks (MonoMethodHeader *header)
{
	const unsigned char *ip, *end, *start;
	const MonoOpcode *opcode;
	guint32 i, block_end = 0;
	GPtrArray *result = g_ptr_array_new ();
	GHashTable *table = g_hash_table_new (g_direct_hash, g_direct_equal);
	MonoBasicBlock *entry_bb, *end_bb, *bb, *target;

	ip = mono_method_header_get_code (header, &i, NULL);
	end = ip + i;
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
					target->cil_code = (const guchar*)itarget;
					g_ptr_array_add (result, target);
					g_hash_table_insert (table, (gpointer) itarget, target);
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
	bb->cil_length = end - bb->cil_code;
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
				fprintf (output, "\t\"DF entry\" -> \"IL_%04x (%d)\"\n", (unsigned int)(next->cil_code - code), *dfn + 1);
			else
				fprintf (output, "\t\"IL_%04x (%d)\" -> \"IL_%04x (%d)\"\n", (unsigned int)(bb->cil_code - code), bb->dfn, (unsigned int)(next->cil_code - code), *dfn + 1);
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
	const unsigned char *il_code;

	header = mono_method_get_header (method);
	il_code = mono_method_header_get_code (header, NULL, NULL);
	bblocks = mono_method_find_bblocks (header);
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		if (i == 0)
			fprintf (output, "\tB%p [shape=record,label=\"entry\"]\n", bb);
		else if (i == 1)
			fprintf (output, "\tB%p [shape=record,label=\"end\"]\n", bb);
		else {
			code = mono_disasm_code (&graph_dh, method, bb->cil_code, bb->cil_code + bb->cil_length);
			fprintf (output, "\tB%p [shape=record,label=\"IL_%04x\\n%s\"]\n", bb, (unsigned int)(bb->cil_code - il_code), code);
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
#if 1
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		bb->dfn = 0;
	}
	dfn = 0;
	for (i = 0; i < bblocks->len; ++i) {
		bb = (MonoBasicBlock*)g_ptr_array_index (bblocks, i);
		df_visit (bb, &dfn, il_code);
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
		guint32 token = mono_image_get_entry_point (image);
		if (!token || !(method = mono_get_method (image, token, NULL))) {
			g_print ("Cannot find entry point in %s: specify an explict method name.\n", mono_image_get_filename (image));
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
	printf ("Usage: monograph [options] [assembly [typename|methodname]]\n\n");
	printf ("Valid options are:\n");
	printf ("\t-c|--call             output call graph instead of type hierarchy\n");
	printf ("\t-C|--control-flow     output control flow of methodname\n");
	printf ("\t--stats               output some statistics about the assembly\n");
	printf ("\t--size                output some size statistics about the assembly\n");
	printf ("\t-d|--depth num        max depth recursion (default: 6)\n");
	printf ("\t-o|--output filename  write graph to file filename (default: stdout)\n");
	printf ("\t-f|--fullname         include namespace in type and method names\n");
	printf ("\t-n|--neato            invoke neato directly\n");
	printf ("\t-v|--verbose          verbose operation\n\n");
	printf ("The default assembly is mscorlib.dll. The default method for\n");
	printf ("the --call and --control-flow options is the entrypoint.\n\n");
	printf ("When the --neato option is used the output type info is taken\n");
	printf ("from the output filename extension. You need the graphviz package\n");
	printf ("installed to be able to use this option and build bitmap files.\n");
	printf ("Without --neato, monograph will create .dot files, a description\n");
	printf ("file for a graph.\n\n");
	printf ("Sample runs:\n");
	printf ("\tmonograph -n -o vt.png mscorlib.dll System.ValueType\n");
	printf ("\tmonograph -n -o expr.png mcs.exe Mono.CSharp.Expression\n");
	printf ("\tmonograph -n -o cfg.png -C mcs.exe Driver:Main\n");
	printf ("\tmonograph -d 3 -n -o callgraph.png -c mis.exe\n");
	exit (1);
}

enum {
	GRAPH_TYPES,
	GRAPH_CALL,
	GRAPH_INTERFACE,
	GRAPH_CONTROL_FLOW,
	GRAPH_SIZE_STATS,
	GRAPH_STATS
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
	MonoImage *image;
	const char *cname = NULL;
	const char *aname = NULL;
	char *outputfile = NULL;
	int graphtype = GRAPH_TYPES;
	int callneato = 0;
	int i;
	
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
		} else if (strcmp (argv [i], "--stats") == 0) {
			graphtype = GRAPH_STATS;
		} else if (strcmp (argv [i], "--size") == 0) {
			graphtype = GRAPH_SIZE_STATS;
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
	if (aname) {
		mono_init_from_assembly (argv [0], aname);
		assembly = mono_assembly_open (aname, NULL);
	} else {
		mono_init (argv [0]);
		assembly = mono_image_get_assembly (mono_get_corlib ());
	}
	if (!cname && (graphtype == GRAPH_TYPES))
		cname = "System.Object";

	if (!assembly) {
		g_print ("cannot open assembly %s\n", aname);
		exit (1);
	}

	if (callneato) {
		GString *command = g_string_new ("neato");
		char *type = NULL;

		if (outputfile) {
			type = strrchr (outputfile, '.');
			g_string_append_printf (command, " -o %s", outputfile);
		}
		if (type)
			g_string_append_printf (command, " -T%s", type + 1);
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

	image = mono_assembly_get_image (assembly);
	switch (graphtype) {
	case GRAPH_TYPES:
		type_graph (image, cname);
		break;
	case GRAPH_CALL:
		method_graph (image, cname);
		break;
	case GRAPH_INTERFACE:
		interface_graph (image, cname);
		break;
	case GRAPH_CONTROL_FLOW:
		method_cfg (image, cname);
		break;
	case GRAPH_STATS:
		stats (image, cname);
		break;
	case GRAPH_SIZE_STATS:
		size_stats (image, cname);
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
