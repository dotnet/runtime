/*
 * testjit.c: Test 
 *
 */

#include <config.h>
#include <glib.h>
#include <stdlib.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/endian.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"
#include "testjit.h"
#include "regset.h"
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

static gpointer mono_compile_method (MonoMethod *method);

static MonoRegSet *
get_x86_regset ()
{
	MonoRegSet *rs;

	rs = mono_regset_new (X86_NREG);

	mono_regset_reserve_reg (rs, X86_ESP);
	mono_regset_reserve_reg (rs, X86_EBP);

	return rs;
}

static void
tree_allocate_regs (MBTree *tree, int goal, MonoRegSet *rs) 
{
	MBTree *kids[10];
	int ern = mono_burg_rule (tree->state, goal);
	guint16 *nts = mono_burg_nts [ern];
	int i;
	
	mono_burg_kids (tree, ern, kids);

	for (i = 0; nts [i]; i++) 
		tree_allocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++) 
		mono_regset_free_reg (rs, kids [i]->reg);

	if (goal == MB_NTERM_reg) {
		if ((tree->reg = mono_regset_alloc_reg (rs, -1)) == -1) {
			g_warning ("register allocation failed\n");
			g_assert_not_reached ();
		}
	}

	tree->emit = mono_burg_func [ern];

}

static void
tree_emit (guint8 *cstart, guint8 **buf, MBTree *tree) 
{
	if (tree->left)
		tree_emit (cstart, buf, tree->left);
	if (tree->right)
		tree_emit (cstart, buf, tree->right);

	tree->addr = *buf - cstart;

	if (tree->emit)
		((MBEmitFunc)tree->emit) (tree, buf);
}

static gboolean
tree_get_address (MBTree *tree, gint32 vaddr, gint32 *addr)
{
	if (tree->left && tree_get_address (tree->left, vaddr, addr))
		return TRUE;

	if (tree->right && tree_get_address (tree->right, vaddr, addr))
		return TRUE;

	if (tree->vaddr == vaddr) {
		*addr = tree->addr;
		return TRUE;
	}

	return FALSE;
}

static gint32
get_address (GPtrArray *forest, guint32 vaddr)
{
	gint32 i, a;

	for (i = 0; i < forest->len; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);

		if (tree_get_address (t1, vaddr, &a))
			return a;
	}

	g_assert_not_reached ();
	return 0;
}

static void
compute_branches (guint8 *cstart, GPtrArray *forest, guint32 epilog)
{
	gint32 addr;
	int i;
	guint8 *buf;

	for (i = 0; i < forest->len; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);

		if (t1->is_jump) {
			if (t1->data.i >= 0)
				addr = get_address (forest, t1->data.i);
			else
				addr = epilog;

			t1->data.i = addr - t1->addr;
			buf = cstart + t1->addr;
			((MBEmitFunc)t1->emit) (t1, &buf);
		}
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
	t->reg = -1;
	t->is_jump = 0;
	return t;
}

static MBTree *
ctree_new_leaf (int op, MonoTypeEnum type)
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
print_forest (GPtrArray *forest)
{
	const int top = forest->len;
	int i;

	for (i = 0; i < top; i++) {
		MBTree *t = (MBTree *) g_ptr_array_index (forest, i);
		printf ("IL%04x:", t->vaddr);
		print_tree (t);
		printf ("\n");
	}

}

static void
forest_label (GPtrArray *forest)
{
	const int top = forest->len;
	int i;
	
	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);
		MBState *s;

		s =  mono_burg_label (t1);
		if (!s) {
			g_warning ("tree does not match");
			print_tree (t1); printf ("\n");
			g_assert_not_reached ();
		}
	}
}

static void
forest_emit (guint8 *cstart, guint8 **buf, GPtrArray *forest)
{
	const int top = forest->len;
	int i;
	
	
	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);
		tree_emit (cstart, buf, t1);
	}
}

static void
forest_allocate_regs (GPtrArray *forest, MonoRegSet *rs)
{
	const int top = forest->len;
	int i;
	
	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);
		tree_allocate_regs (t1, 1, rs);
	}

}

static void
mono_disassemble_code (guint8 *code, int size)
{
	int i;
	FILE *ofd;

	if (!(ofd = fopen ("/tmp/test.s", "w")))
		g_assert_not_reached ();

	for (i = 0; i < size; ++i)
		fprintf (ofd, ".byte %d\n", (unsigned int) code [i]);

	fclose (ofd);

	system ("as /tmp/test.s -o /tmp/test.o;objdump -d /tmp/test.o");
}

static void
emit_method (MonoMethod *method, GPtrArray *forest, int locals_size)
{
	MonoRegSet *rs = get_x86_regset ();
	guint8 *buf, *start;
	guint32 epilog;

	method->addr = start = buf = g_malloc (1024);

	forest_label (forest);
	forest_allocate_regs (forest, rs);
	arch_emit_prologue (&buf, method, locals_size, rs);
	forest_emit (start, &buf, forest);
	
	epilog = buf - start;
	arch_emit_epilogue (&buf, method, rs);

	compute_branches (start, forest, epilog);

	mono_regset_free (rs);

	mono_disassemble_code (start, buf - start);
}

static gpointer
create_jit_trampoline (MonoMethod *method)
{
	guint8 *code, *buf;

	if (method->addr)
		return method->addr;

	code = buf = g_malloc (64);

	x86_push_reg (buf, X86_EBP);
	x86_mov_reg_reg (buf, X86_EBP, X86_ESP, 4);

	x86_push_imm (buf, method);
	x86_call_code (buf, mono_compile_method);
	x86_mov_reg_imm (buf, X86_ECX, method);
	x86_mov_membase_reg (buf, X86_ECX, G_STRUCT_OFFSET (MonoMethod, addr),
			     X86_EAX, 4);

	/* free the allocated code buffer */
	x86_push_reg (buf, X86_EAX);
	x86_push_imm (buf, code);
	x86_call_code (buf, g_free);
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);
	x86_pop_reg (buf, X86_EAX);

	x86_leave (buf);

	/* jump to the compiled method */
	x86_jump_reg (buf, X86_EAX);

	g_assert ((buf - code) < 64);

	return code;
}


#define ADD_TREE(t)     do { t->vaddr = vaddr; g_ptr_array_add (forest, (t)); } while (0)

#define PUSH_TREE(t)    do { t->vaddr = vaddr; *sp = t; sp++; } while (0)

#define LOCAL_POS(n)    (locals_offsets [(n)])
#define LOCAL_TYPE(n)   ((header)->locals [(n)])

#define ARG_POS(n)      (args_offsets [(n)])
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])

static gpointer
mono_compile_method (MonoMethod *method)
{
	MonoMethodHeader *header;
	MonoMethodSignature *signature;
	MonoImage *image;
	MBTree **sp, **stack, *t1, *t2;
	register const unsigned char *ip, *end;
	guint *locals_offsets;
	guint *args_offsets;
	GPtrArray *forest;
	int local_offset = 0;

	g_assert (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
	g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

	printf ("Start JIT compilation of %s\n", method->name);

	header = ((MonoMethodNormal *)method)->header;
	signature = method->signature;
	image = method->klass->image;
	
	ip = header->code;
	end = ip + header->code_size;

	sp = stack = alloca (sizeof (MBTree *) * header->max_stack);
	
	if (header->num_locals) {
		int i, size, align;
		locals_offsets = alloca (sizeof (gint) * header->num_locals);

		for (i = 0; i < header->num_locals; ++i) {
			locals_offsets [i] = local_offset;
			size = mono_type_size (header->locals [i], &align);
			local_offset += local_offset % align;
			local_offset += size;
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

	forest = g_ptr_array_new ();

	while (ip < end) {
		guint32 vaddr = ip - header->code;
		printf ("VADDR %0x\n", vaddr);
		switch (*ip) {

		case CEE_CALL: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			guint32 token, nargs;
			int i;
			
			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			
			nargs = csig->param_count + csig->hasthis;

			for (i = 0; i < nargs; i++) {
				sp--;
				t1 = ctree_new (MB_TERM_ARG, 0, *sp, NULL);
				ADD_TREE (t1);
			}
			
			cm->addr = create_jit_trampoline (cm);
			
			if (csig->ret->type != MONO_TYPE_VOID) {
				int size, align;
				t1 = ctree_new_leaf (MB_TERM_CALL, csig->ret->type);
				t1->data.p = cm;
				t2 = ctree_new (MB_TERM_STLOC, csig->ret->type, t1, NULL);
				size = mono_type_size (csig->ret, &align);
				t2->data.i = local_offset;
				local_offset += local_offset % align;
				local_offset += size;
				ADD_TREE (t2);
				t1 = ctree_new_leaf (MB_TERM_LDLOC, t2->type);
				t1->data.i = t2->data.i;
				PUSH_TREE (t1);
			} else {
				t1 = ctree_new_leaf (MB_TERM_CALL, MONO_TYPE_VOID);
				t1->data.p = cm;
				ADD_TREE (t1);
			}
			break;
		}
		case CEE_LDC_I4_S: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_CONST, MONO_TYPE_I4);
			t1->data.i = *ip;
			++ip;
			PUSH_TREE (t1);
			break;

		case CEE_LDC_I4: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_CONST, MONO_TYPE_I4);
			t1->data.i = read32 (ip);
			ip += 4;
			PUSH_TREE (t1);
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
			t1 = ctree_new_leaf (MB_TERM_CONST, MONO_TYPE_I4);
			t1->data.i = (*ip) - CEE_LDC_I4_0;
			++ip;
			PUSH_TREE (t1);
			break;

		case CEE_LDLOC_0:
		case CEE_LDLOC_1:
		case CEE_LDLOC_2:
		case CEE_LDLOC_3: {
			int n = (*ip) - CEE_LDLOC_0;
			++ip;

			t1 = ctree_new_leaf (MB_TERM_LDLOC, LOCAL_TYPE (n)->type);
			t1->data.i = LOCAL_POS (n);
			PUSH_TREE (t1);
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
			sp -= 2;
			t1 = ctree_new (MB_TERM_ADD, 0, sp [0], sp [1]);
			PUSH_TREE (t1);
			break;

		case CEE_SUB:
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_SUB, 0, sp [0], sp [1]);
			PUSH_TREE (t1);
			break;

		case CEE_MUL:
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_MUL, 0, sp [0], sp [1]);
			PUSH_TREE (t1);
			break;

		case CEE_BR_S: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_BR, 0);
			t1->data.i = vaddr + 2 + (signed char) *ip;
			ADD_TREE (t1);
			++ip;
			break;

		case CEE_BR:
			++ip;
			t1 = ctree_new_leaf (MB_TERM_BR, 0);
			t1->data.i = vaddr + 5 + (gint32) read32(ip);
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
				t1->data.i = vaddr + 2 + (signed char) *ip;
			else 
				t1->data.i = vaddr + 5 + (gint32) read32 (ip);

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
				t1->data.i = vaddr + 2 + (signed char) *ip;
			else 
				t1->data.i = vaddr + 5 + (gint32) read32 (ip);

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
				t1->data.i = vaddr + 2 + (signed char) *ip;
			else 
				t1->data.i = vaddr + 5 + (gint32) read32 (ip);

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
				
			t1->data.i = -1;

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
			t1 = ctree_new_leaf (MB_TERM_LDARG, ARG_TYPE (n)->type);
			t1->data.i = ARG_POS (n);
			PUSH_TREE (t1);
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
				t1 = ctree_new_leaf (MB_TERM_LDARG, ARG_TYPE (n)->type);
				t1->data.i = ARG_POS (n);
				PUSH_TREE (t1);
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
	
	printf ("LOCALS ARE: %d\n", local_offset);
	print_forest (forest);
	emit_method (method, forest, local_offset);

	// printf ("COMPILED %p %p\n", method, method->addr);

	return method->addr;
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

typedef int (*MonoMainIntVoid) ();

static int 
ves_exec (MonoAssembly *assembly, int argc, char *argv[])
{
	MonoImage *image = assembly->image;
	MonoCLIImageInfo *iinfo;
	MonoMethod *method;
	int res = 0;

	iinfo = image->image_info;
	method = mono_get_method (image, iinfo->cli_cli_header.ch_entry_point, NULL);

	if (method->signature->param_count) {
		g_warning ("Main () with arguments not yet supported");
		exit (1);
	} else {
		MonoMainIntVoid mfunc;

		mfunc = create_jit_trampoline (method);
		res = mfunc ();
	}
	
	return res;
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


	retval = ves_exec (assembly, argc, argv);
	//jit_assembly (assembly);

	mono_assembly_close (assembly);

	printf ("RESULT: %d\n", retval);

	return retval;
}



