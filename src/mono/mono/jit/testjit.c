/*
 *
 */
#include <config.h>
#include <glib.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/endian.h>

#include "testjit.h"

/*
 * Pull the list of opcodes
 */
#define OPDEF(a,b,c,d,e,f,g,h,i,j) \
	a = i,

enum {
#include "mono/cil/opcode.def"
	LAST = 0xff
};
#undef OPDEF

static int cregnum = 0;

static void
reduce (MBTree *tree, int goal) 
{
	MBTree *kids[10];
	int ern = mono_burg_rule (tree->state, goal);
	guint16 *nts = mono_burg_nts [ern];
	int i, n;
	
	mono_burg_kids (tree, ern, kids);

	// printf ("TEST %d %d %s %d\n", goal, ern, mono_burg_rule_string [ern], nts [0]);
	
	for (i = 0; nts [i]; i++) 
		reduce (kids [i], nts [i]);

	if (goal == MB_NTERM_reg)
		tree->reg = ++cregnum;

	n = (tree->left != NULL) + (tree->right != NULL);

	if (n) { /* not a terminal */
	  // printf ("XXTE %s %d\n", mono_burg_rule_string [ern], n);
		if (mono_burg_func [ern])
			mono_burg_func [ern] (tree);
		else ;
//			g_warning ("no code for rule %s\n", 
//				   mono_burg_rule_string [ern]);
	} else {
		if (mono_burg_func [ern])
			mono_burg_func [ern] (tree);
	}
}

static MBTree *
ctree_new (int op, MonoTypeEnum type, MBTree *left, MBTree *right)
{
	MBTree *t = g_malloc (sizeof (MBTree));

	t->op = op;
	t->left = left;
	t->right = right;
	t->type = type;
	return t;
}

static MBTree *
ctree_new_leave (int op, MonoTypeEnum type)
{
	return ctree_new (op, type, NULL, NULL);
}

static void
print_tree (MBTree *tree)
{
	int arity;

	if (!tree)
		return;

	arity = (tree->left != NULL) && (tree->right != NULL);

	if (arity)
		printf ("(%s", mono_burg_term_string [tree->op]);
	else 
		printf (" %s", mono_burg_term_string [tree->op]);

	g_assert (!(tree->right && !tree->left));

	print_tree (tree->left);
	print_tree (tree->right);

	if (arity)
		printf (")");
}

static void
print_forest (GList *f)
{
	GList *l;
	int i;

	for (l = f, i = 0; l; l = l->next) {
		printf ("%03d:", i++);
		print_tree ((MBTree *)l->data);
		printf ("\n");
	}

}

#define ADD_TREE(t)     forest = g_list_append (forest, t)

#define LOCAL_POS(n)    (locals_offsets [(n)])
#define LOCAL_TYPE(n)   ((header)->locals [(n)])

#define ARG_POS(n)      (args_offsets [(n)])
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])
static void
mono_compile_method (MonoMethod *method)
{
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	MBTree **sp, **stack, *t1, *t2;
	register const unsigned char *ip, *end;
	guint *locals_offsets;
	guint *args_offsets;
	GList *l, *forest = NULL;
	MBState *s;
	int i;

	g_assert (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
	g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

	header = ((MonoMethodNormal *)method)->header;
	signature = method->signature;
	image = method->klass->image;
	
	ip = header->code;
	end = ip + header->code_size;

	sp = stack = alloca (sizeof (MBTree *) * header->max_stack);
	
	if (header->num_locals) {
		int i, size, align, offset = 0;
		locals_offsets = alloca (sizeof (gint) * header->num_locals);

		for (i = 0; i < header->num_locals; ++i) {
			locals_offsets [i] = offset;
			size = mono_type_size (header->locals [i], &align);
			offset += offset % align;
			offset += size;
		}
	}
	
	if (signature->params_size) {
		int i, align, size, offset = 0;
		int has_this = signature->hasthis;

		args_offsets = alloca (sizeof (gint) * (signature->param_count + has_this));
		if (has_this) {
			args_offsets [0] = 0;
			offset += sizeof (gpointer);
		}
		for (i = 0; i < signature->param_count; ++i) {
			args_offsets [i + has_this] = offset;
			size = mono_type_size (signature->params [i], &align);
			offset += offset % align;
			offset += size;
		}
	}

	while (ip < end) {

		switch (*ip) {

		case CEE_CALL: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			guint32 token, nargs;

			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			
			nargs = csig->param_count + csig->hasthis;
			sp -= nargs;

			for (i = 0; i < nargs; i++) {
				t1 = ctree_new (MB_TERM_ARG, 0, sp [i], NULL);
				ADD_TREE (t1);
			}
			
			if (csig->ret->type != MONO_TYPE_VOID) {
				t1 = ctree_new_leave (MB_TERM_CALL, csig->ret->type);
				*sp = t1;
				sp++;
			} else {
				t1 = ctree_new_leave (MB_TERM_CALL, MONO_TYPE_VOID);
				ADD_TREE (t1);
			}
			break;
		}
		case CEE_LDC_I4_S: 
			++ip;
			t1 = ctree_new_leave (MB_TERM_CONST, MONO_TYPE_I4);
			t1->data.i = *ip;
			*sp = t1;
			++ip;
			++sp;
			break;

		case CEE_LDC_I4: 
			++ip;
			t1 = ctree_new_leave (MB_TERM_CONST, MONO_TYPE_I4);
			t1->data.i = read32 (ip);
			*sp = t1;
			ip += 4;
			++sp;
			break;

		case CEE_LDC_I4_M1:
		case CEE_LDC_I4_0:
		case CEE_LDC_I4_1:
		case CEE_LDC_I4_2:
		case CEE_LDC_I4_3:
		case CEE_LDC_I4_4:
		case CEE_LDC_I4_5:
		case CEE_LDC_I4_6:
		case CEE_LDC_I4_7:
		case CEE_LDC_I4_8:
			t1 = ctree_new_leave (MB_TERM_CONST, MONO_TYPE_I4);
			t1->data.i = (*ip) - CEE_LDC_I4_0;
			*sp = t1;
			++sp;
			++ip;
			break;

		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int n = (*ip) - CEE_LDLOC_0;
			++ip;

			t1 = ctree_new_leave (MB_TERM_LDLOC, LOCAL_TYPE (n)->type);
			t1->data.i = LOCAL_POS (n);
			*sp = t1;
			++sp;
			break;
		}
		case CEE_STLOC_0:
		case CEE_STLOC_1:
		case CEE_STLOC_2:
		case CEE_STLOC_3: {
			int n = (*ip) - CEE_STLOC_0;
			++ip;
			--sp;

			t1 = ctree_new (MB_TERM_STLOC, LOCAL_TYPE (n)->type, *sp, NULL);
			t1->data.i = LOCAL_POS (n);

			ADD_TREE (t1);			
			break;
		}

		case CEE_ADD:
			++ip;
			--sp;
			sp [-1] = ctree_new (MB_TERM_ADD, 0, sp [-1], sp [0]);
			break;

		case CEE_SUB:
			++ip;
			--sp;
			sp [-1] = ctree_new (MB_TERM_SUB, 0, sp [-1], sp [0]);
			break;

		case CEE_MUL:
			++ip;
			--sp;
			sp [-1] = ctree_new (MB_TERM_MUL, 0, sp [-1], sp [0]);
			break;

		case CEE_BR_S: 
			++ip;
			t1 = ctree_new_leave (MB_TERM_BR, 0);
			t1->data.i = (signed char) *ip;
			ADD_TREE (t1);
			++ip;
			break;

		case CEE_BR:
			++ip;
			t1 = ctree_new_leave (MB_TERM_BR, 0);
			t1->data.i = (gint32) read32(ip);
			ADD_TREE (t1);
			ip += 4;
			break;

		case CEE_BLT:
		case CEE_BLT_S: {
			int near_jump = *ip == CEE_BLT_S;
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_BLT, 0, sp [0], sp [1]);
			if (near_jump)
				t1->data.i = (signed char) *ip;
			else 
				t1->data.i = (gint32) read32 (ip);				

			ADD_TREE (t1);
			ip += near_jump ? 1: 4;		
			break;
		}
		case CEE_BEQ:
		case CEE_BEQ_S: {
			int near_jump = *ip == CEE_BEQ_S;
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_BEQ, 0, sp [0], sp [1]);
			if (near_jump)
				t1->data.i = (signed char) *ip;
			else 
				t1->data.i = (gint32) read32 (ip);				

			ADD_TREE (t1);
			ip += near_jump ? 1: 4;		
			break;
		}
		case CEE_BGE:
		case CEE_BGE_S: {
			int near_jump = *ip == CEE_BGE_S;
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_BGE, 0, sp [0], sp [1]);
			if (near_jump)
				t1->data.i = (signed char) *ip;
			else 
				t1->data.i = (gint32) read32 (ip);				

			ADD_TREE (t1);
			ip += near_jump ? 1: 4;		
			break;
		}
		case CEE_RET:
			if (signature->ret->type != MONO_TYPE_VOID) {
				--sp;
				t1 = ctree_new (MB_TERM_RETV, 0, *sp, NULL);
			} else
				t1 = ctree_new (MB_TERM_RET, 0, NULL, NULL);
				
			ADD_TREE (t1);

			if (sp > stack)
				g_warning ("more values on stack: %d", sp - stack);

			ip++;
			break;

		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int n = (*ip) - CEE_LDARG_0;
			++ip;
			t1 = ctree_new_leave (MB_TERM_LDARG, ARG_TYPE (n)->type);
			t1->data.i = ARG_POS (n);
			*sp = t1;
			++sp;
			break;
		}
		case 0xFE:
			++ip;			
			switch (*ip) {
			case CEE_LDARG: {
				guint32 n;
				++ip;
				n = read32 (ip);
				ip += 4;
				t1 = ctree_new_leave (MB_TERM_LDARG, ARG_TYPE (n)->type);
				t1->data.i = ARG_POS (n);
				*sp = t1;
				++sp;
				break;
			}
			default:
				g_error ("Unimplemented opcode IL_%04x: "
					 "0xFE %02x", ip - header->code, *ip);
			}
			break;
		default:
			g_warning ("unknown instruction IL_%04X: %02x", 
				   ip - header->code, *ip);
			print_forest (forest);
			g_assert_not_reached ();
		}
	}
	
	print_forest (forest);
	
	for (l = forest, i = 0; l; l = l->next) {
		t1 = (MBTree *)l->data;
		//printf ("%03d:", i++);print_tree ((MBTree *)l->data);printf ("\n");

		s =  mono_burg_label (t1);
		g_assert (s);
		cregnum = 0;
		reduce (t1, MB_NTERM_stmt);
	}
}

static void
jit_assembly (MonoAssembly *assembly)
{
	MonoImage *image = assembly->image;
	MonoMethod *method;
	MonoTableInfo *t = &image->tables [MONO_TABLE_METHOD];
	int i;

	for (i = 0; i < t->rows; i++) {

		method = mono_get_method (image, 
					  (MONO_TABLE_METHOD << 24) | (i + 1), 
					  NULL);

		printf ("\nStart Method %s\n\n", method->name);

		mono_compile_method (method);

	}

}

static int 
ves_exec (MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	MonoMethod *method;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);

	if (method->signature->param_count) {
		g_warning ("Main () with arguments not yet supported");
		exit (1);
	} else {
		mono_compile_method (method);
	}
	
	return 0;
}

static void
usage (char *name)
{
	fprintf (stderr,
		 "%s %s, the Mono ECMA CLI JITTER, (C) 2001 Ximian, Inc.\n\n"
		 "Usage is: %s [options] executable args...\n", name,  VERSION, name);
	fprintf (stderr,
		 "Valid Options are:\n"
		 "--help\n");
	exit (1);
}

int 
main (int argc, char *argv [])
{
	MonoAssembly *assembly;
	int retval = 0, i;
	char *file;

	if (argc < 2)
		usage (argv [0]);

	for (i = 1; argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--help") == 0)
			usage (argv [0]);
		else
			usage (argv [0]);
	}
	
	file = argv [i];

	mono_init ();
	mono_init_icall ();

	assembly = mono_assembly_open (file, NULL, NULL);
	if (!assembly){
		fprintf (stderr, "Can not open image %s\n", file);
		exit (1);
	}

	//retval = ves_exec (assembly, argc, argv);
	jit_assembly (assembly);

	mono_assembly_close (assembly);

	return retval;
}



