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

#define OPDEF(a,b,c,d,e,f,g,h,i,j) b,
char *opcode_names [] = {
#include "mono/cil/opcode.def"	
};
#undef OPDEF

/* Whether to dump the assembly code after genreating it */
gboolean dump_asm    = FALSE;

/* Whether to dump the forest */
gboolean dump_forest = FALSE;

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

	if (goal == MB_NTERM_reg || (goal == MB_NTERM_lreg)) {
		switch (tree->op) {
		case MB_TERM_CALL:
		case MB_TERM_MUL:
			tree->reg1 = X86_EAX;
			tree->reg2 = X86_EDX;
			break;
		case MB_TERM_DIV:
			tree->reg1 = X86_EAX;
			tree->reg2 = X86_EDX;
			if (goal == MB_NTERM_reg) {
				tree->left->exclude_edx = 1;
				tree->right->exclude_edx = 1;
			}
			break;
		default:
			/* do nothing */
		}
	}

	for (i = 0; nts [i]; i++) 
		tree_allocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++) {
		mono_regset_free_reg (rs, kids [i]->reg1);
		mono_regset_free_reg (rs, kids [i]->reg2);
	}

	switch (goal) {
	case MB_NTERM_reg:
		if ((tree->reg1 = 
		     mono_regset_alloc_reg (rs, tree->reg1, tree->exclude_edx)) == -1) {
			g_warning ("register allocation failed\n");
			g_assert_not_reached ();
		}

		break;

	case MB_NTERM_lreg:
		if ((tree->reg1 = 
		     mono_regset_alloc_reg (rs, tree->reg1, tree->exclude_edx)) == -1 ||
		    (tree->reg2 = 
		     mono_regset_alloc_reg (rs, tree->reg2, tree->exclude_edx)) == -1) {
			g_warning ("register allocation failed\n");
			g_assert_not_reached ();
		}
		break;

	case MB_NTERM_freg:
		/* fixme: allocate floating point registers */
		break;

	default:
		/* do nothing */
	}

	tree->emit = mono_burg_func [ern];

}

static void
tree_emit (MBCodeGenStatus *s, MBTree *tree) 
{
	if (tree->left)
		tree_emit (s, tree->left);
	if (tree->right)
		tree_emit (s, tree->right);

	tree->addr = s->code - s->start;

	if (tree->emit)
		((MBEmitFunc)tree->emit) (tree, s);
}

static gint32
get_address (GPtrArray *forest, guint32 cli_addr, gint base, gint len)
{
	gint32 ind, pos;
	MBTree *t1;

	ind = (len / 2);
	pos = base + ind;

	/* skip trees with cli_addr == -1 */
	while ((t1 = (MBTree *) g_ptr_array_index (forest, pos)) &&
	       t1->cli_addr == -1 && ind) {
		ind--;
		pos--;
	}

	if (t1->cli_addr == cli_addr) {
		t1->jump_target = 1;
		return t1->first_addr;
	}

	if (len <= 1)
		return -1;

	if (t1->cli_addr > cli_addr) {
		return get_address (forest, cli_addr, base, ind);
	} else {
		ind = (len / 2);		
		return get_address (forest, cli_addr, base + ind, len - ind);
	}
}

static void
compute_branches (MBCodeGenStatus *s)
{
	GPtrArray *forest = s->forest;
	const int top = forest->len;
	gint32 addr;
	guint8 *end;
	int i;

	end = s->code;

	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);

		if (t1->is_jump) {

			if ((i + 1) < forest->len) {
				MBTree *t2 = (MBTree *) g_ptr_array_index (forest, i + 1);
				t2->jump_target = 1;
			}

			if (t1->data.i >= 0) {
				addr = get_address (forest, t1->data.i, 0, forest->len);
				if (addr == -1)
					g_error ("address 0x%x not found",
						 t1->data.i);

			} else
				addr = s->epilog;

			t1->data.i = addr - t1->addr;
			s->code = s->start + t1->addr;
			((MBEmitFunc)t1->emit) (t1, s);
		}
	}

	s->code = end;
}

static MBTree *
ctree_new (int op, MonoTypeEnum type, MBTree *left, MBTree *right)
{
	MBTree *t = g_malloc (sizeof (MBTree));

	t->op = op;
	t->left = left;
	t->right = right;
	t->type = type;
	t->reg1 = -1;
	t->reg2 = -1;
	t->is_jump = 0;
	t->jump_target = 0;
	t->last_instr = 0;
	t->exclude_edx = 0;
	t->cli_addr = -1;
	t->addr = 0;
	t->first_addr = 0;
	t->emit = NULL;
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
		if (t->jump_target && t->cli_addr >= 0)
			printf ("IL%04x:", t->cli_addr);
		else
			printf ("       ");

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
forest_emit (MBCodeGenStatus *s)
{
	GPtrArray *forest = s->forest;
	const int top = forest->len;
	int i;
		
	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);
		t1->first_addr = s->code - s->start;
		tree_emit (s, t1);
	}

	s->epilog = s->code - s->start;
}

static void
forest_allocate_regs (MBCodeGenStatus *s)
{
	GPtrArray *forest = s->forest;
	const int top = forest->len;
	int i;
	
	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);
		tree_allocate_regs (t1, 1, s->rs);
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
emit_method (MonoMethod *method, MBCodeGenStatus *s)
{
	method->addr = s->start = s->code = g_malloc (1024);

	forest_label (s->forest);

	forest_allocate_regs (s);
	arch_emit_prologue (s);
	forest_emit (s);
	arch_emit_epilogue (s);

	compute_branches (s);

	if (dump_forest)
		print_forest (s->forest);

	if (dump_asm)
		mono_disassemble_code (s->start, s->code - s->start);
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


#define ADD_TREE(t)     do { g_ptr_array_add (forest, (t)); } while (0)
#define PUSH_TREE(t)    do { *sp = t; sp++; } while (0)

#define LOCAL_POS(n)    (locals_offsets [(n)])
#define LOCAL_TYPE(n)   ((header)->locals [(n)])

#define ARG_POS(n)      (args_offsets [(n)])
#define ARG_TYPE(n)     ((n) ? (signature)->params [(n) - (signature)->hasthis] : \
			(signature)->hasthis ? &method->klass->this_arg: (signature)->params [(0)])

static gpointer
mono_compile_method (MonoMethod *method)
{
	MBCodeGenStatus cgstat;
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
			size = mono_type_size (header->locals [i], &align);
			local_offset += align - 1;
			local_offset &= ~(align - 1);
			local_offset += size;
			locals_offsets [i] = - local_offset;
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
			if (size < 4) {
				size = 4; 
				align = 4;
			}
			offset += align - 1;
			offset &= ~(align - 1);
			offset += size;
		}
	}

	forest = g_ptr_array_new ();

	while (ip < end) {
		guint32 cli_addr = ip - header->code;
		switch (*ip) {

		case CEE_CALL: {
			MonoMethodSignature *csig;
			MonoMethod *cm;
			guint32 token, nargs;
			int i;
			gint32 fa;

			++ip;
			token = read32 (ip);
			ip += 4;

			cm = mono_get_method (image, token, NULL);
			csig = cm->signature;
			g_assert (csig->call_convention == MONO_CALL_DEFAULT);
			
			if ((nargs = csig->param_count + csig->hasthis)) {

				fa = sp [-nargs]->cli_addr;

				for (i = 0; i < nargs; i++) {
					sp--;
					t1 = ctree_new (MB_TERM_ARG, 0, *sp, NULL);
				
					if (i)
						t1->cli_addr = -1;
					else
						t1->cli_addr = fa;
					
					ADD_TREE (t1);
				}

				fa = -1;
			} else
				fa = cli_addr;
			
			cm->addr = create_jit_trampoline (cm);
			
			if (csig->ret->type != MONO_TYPE_VOID) {
				int size, align;
				t1 = ctree_new_leaf (MB_TERM_CALL, csig->ret->type);
				t1->data.p = cm;
				t2 = ctree_new (MB_TERM_STLOC, csig->ret->type, t1, NULL);
				size = mono_type_size (csig->ret, &align);
				local_offset += align - 1;
				local_offset &= ~(align - 1);
				local_offset += size;
				t2->data.i = - local_offset;
				t2->cli_addr = fa;
				ADD_TREE (t2);
				t1 = ctree_new_leaf (MB_TERM_LDLOC, t2->type);
				t1->data.i = t2->data.i;
				PUSH_TREE (t1);
			} else {
				t1 = ctree_new_leaf (MB_TERM_CALL, MONO_TYPE_VOID);
				t1->data.p = cm;
				t1->cli_addr = fa;
				ADD_TREE (t1);
			}
			break;
		}
		case CEE_LDC_I4_S: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_CONST_I4, MONO_TYPE_I4);
			t1->data.i = *ip;
			++ip;
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;

		case CEE_LDC_I4: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_CONST_I4, MONO_TYPE_I4);
			t1->data.i = read32 (ip);
			ip += 4;
			t1->cli_addr = cli_addr;
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
			t1 = ctree_new_leaf (MB_TERM_CONST_I4, MONO_TYPE_I4);
			t1->data.i = (*ip) - CEE_LDC_I4_0;
			++ip;
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;

		case CEE_LDC_R8: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_CONST_R8, MONO_TYPE_R8);
			(const void *) t1->data.p = ip;
			t1->cli_addr = cli_addr;
			ip += 8;
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
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;
		}
		case CEE_LDLOC_S: {
			++ip;

			t1 = ctree_new_leaf (MB_TERM_LDLOC, LOCAL_TYPE (*ip)->type);
			t1->data.i = LOCAL_POS (*ip);
			t1->cli_addr = cli_addr;
			++ip;

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
			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);			
			break;
		}
		case CEE_STLOC_S: {
			++ip;
			--sp;

			t1 = ctree_new (MB_TERM_STLOC, LOCAL_TYPE (*ip)->type, *sp, NULL);
			t1->data.i = LOCAL_POS (*ip);
			t1->cli_addr = sp [0]->cli_addr;
			++ip;

			ADD_TREE (t1);			
			break;
		}
		case CEE_ADD:
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_ADD, 0, sp [0], sp [1]);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
			break;

		case CEE_SUB:
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_SUB, 0, sp [0], sp [1]);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
			break;

		case CEE_MUL:
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_MUL, 0, sp [0], sp [1]);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
			break;

		case CEE_DIV:
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_DIV, 0, sp [0], sp [1]);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);
			break;

		case CEE_BR_S: 
			++ip;
			t1 = ctree_new_leaf (MB_TERM_BR, 0);
			t1->data.i = cli_addr + 2 + (signed char) *ip;
			t1->cli_addr = cli_addr;
			ADD_TREE (t1);
			++ip;
			break;

		case CEE_BR:
			++ip;
			t1 = ctree_new_leaf (MB_TERM_BR, 0);
			t1->data.i = cli_addr + 5 + (gint32) read32(ip);
			t1->cli_addr = cli_addr;
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
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);
			ip += near_jump ? 1: 4;		
			break;
		}
		case CEE_BNE_UN:
		case CEE_BNE_UN_S: {
			int near_jump = *ip == CEE_BNE_UN_S;
			++ip;
			sp -= 2;
			t1 = ctree_new (MB_TERM_BNE_UN, 0, sp [0], sp [1]);
			if (near_jump)
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			t1->cli_addr = sp [0]->cli_addr;
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
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			t1->cli_addr = sp [0]->cli_addr;
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
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);
			ip += near_jump ? 1: 4;		
			break;
		}

		case CEE_BRTRUE:
		case CEE_BRTRUE_S: {
			int near_jump = *ip == CEE_BRTRUE_S;
			++ip;
			--sp;

			t1 = ctree_new (MB_TERM_BRTRUE, 0, sp [0], NULL);

			if (near_jump)
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			ip += near_jump ? 1: 4;
			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);
			break;
		}

		case CEE_BRFALSE:
		case CEE_BRFALSE_S: {
			int near_jump = *ip == CEE_BRFALSE_S;
			++ip;
			--sp;

			t1 = ctree_new (MB_TERM_BRFALSE, 0, sp [0], NULL);

			if (near_jump)
				t1->data.i = cli_addr + 2 + (signed char) *ip;
			else 
				t1->data.i = cli_addr + 5 + (gint32) read32 (ip);

			ip += near_jump ? 1: 4;
			t1->cli_addr = sp [0]->cli_addr;
			ADD_TREE (t1);
			break;
		}
		case CEE_RET:
			ip++;

			if (signature->ret->type != MONO_TYPE_VOID) {
				--sp;
				t1 = ctree_new (MB_TERM_RETV, 0, *sp, NULL);
				t1->cli_addr = sp [0]->cli_addr;
			} else {
				t1 = ctree_new (MB_TERM_RET, 0, NULL, NULL);
				t1->cli_addr = cli_addr;
			}

			t1->data.i = -1;
			t1->last_instr = (ip == end);

			ADD_TREE (t1);

			if (sp > stack)
				g_warning ("more values on stack: %d", sp - stack);

			break;

		case CEE_LDARG_0:
		case CEE_LDARG_1:
		case CEE_LDARG_2:
		case CEE_LDARG_3: {
			int n = (*ip) - CEE_LDARG_0;
			++ip;
			t1 = ctree_new_leaf (MB_TERM_LDARG, ARG_TYPE (n)->type);
			t1->data.i = ARG_POS (n);
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			break;
		}

		case CEE_LDARG_S:
			++ip;
			t1 = ctree_new_leaf (MB_TERM_LDARG, ARG_TYPE (*ip)->type);
			t1->data.i = ARG_POS (*ip);
			t1->cli_addr = cli_addr;
			PUSH_TREE (t1);
			++ip;
			break;

		case CEE_DUP: 
			++ip; 

			if (sp [-1]->op == MB_TERM_LDLOC) {
				t1 = ctree_new (0, 0, NULL, NULL);
				*t1 = *sp [-1];
				PUSH_TREE (t1);		
			} else 
				g_assert_not_reached ();

			break;

		case CEE_POP:
			++ip;
			--sp;
			/*
			 * all side effects are already on the forest,
			 * so we can simply ignore this tree
			 */
			g_free (*sp);
			break;

		case CEE_CONV_U1: 
		case CEE_CONV_I1: 
			++ip;
			sp--;
			t1 = ctree_new (MB_TERM_CONV_I1, MONO_TYPE_I4, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
	       
		case CEE_CONV_U2: 
		case CEE_CONV_I2: 
			++ip;
			sp--;
			t1 = ctree_new (MB_TERM_CONV_I2, MONO_TYPE_I4, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
	       
		case CEE_CONV_I: 
		case CEE_CONV_I4:
			++ip;
			sp--;
			t1 = ctree_new (MB_TERM_CONV_I4, MONO_TYPE_I4, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
	       
		case CEE_CONV_I8:
			++ip;
			sp--;
			t1 = ctree_new (MB_TERM_CONV_I8, MONO_TYPE_I8, *sp, NULL);
			t1->cli_addr = sp [0]->cli_addr;
			PUSH_TREE (t1);		
			break;
	       
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
				t1->cli_addr = cli_addr;
				PUSH_TREE (t1);
				break;
			}
			default:
				g_error ("Unimplemented opcode at IL_%04x"
					 "0xFE %02x", ip - header->code, *ip);
			}
			break;
		default:
			g_warning ("unknown instruction `%s' at IL_%04X", 
				   opcode_names [*ip], ip - header->code);
			print_forest (forest);
			g_assert_not_reached ();
		}
	}
	
	cgstat.forest = forest;
	cgstat.code = NULL;
	cgstat.locals_size = local_offset;
	cgstat.rs = get_x86_regset ();

	emit_method (method, &cgstat);

	mono_regset_free (cgstat.rs);

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
		 "%s %s, the Mono ECMA CLI JIT Compiler, (C) 2001 Ximian, Inc.\n\n"
		 "Usage is: %s [options] executable args...\n", name,  VERSION, name);
	fprintf (stderr,
		 "Valid Options are:\n"
		 "-d            debug the jit, show disassembler output.\n"
		 "--dump-asm    dumps the assembly code generated\n"
		 "--dump-forest dumps the reconstructed forest\n"
		 "--help        print this help message\n");
	exit (1);
}

int 
main (int argc, char *argv [])
{
	MonoAssembly *assembly;
	int retval = 0, i;
	char *file;
	gboolean testjit = FALSE;

	if (argc < 2)
		usage (argv [0]);

	for (i = 1; i < argc - 1 && argv [i][0] == '-'; i++){
		if (strcmp (argv [i], "--help") == 0) {
			usage (argv [0]);
		} else if (strcmp (argv [i], "-d") == 0) {
			testjit = TRUE;
			dump_asm = TRUE;
			dump_forest = TRUE;
		} else if (strcmp (argv [i], "--dump-asm") == 0)
			dump_asm = TRUE;
		else if (strcmp (argv [i], "--dump-forest") == 0)
			dump_forest = TRUE;
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

	if (testjit) {
		jit_assembly (assembly);
	} else {
		retval = ves_exec (assembly, argc, argv);
		printf ("RESULT: %d\n", retval);
	}

	mono_assembly_close (assembly);

	return retval;
}



