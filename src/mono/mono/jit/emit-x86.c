/*
 * emit-x86.c: Support functions for emitting x86 code
 *
 * Authors:
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Miguel de Icaza (miguel@ximian.com)
 *
 * (C) 2001 Ximian, Inc.
 */

#include <config.h>
#include <glib.h>

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/cil-coff.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/mono-endian.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"

static void emit_method (MonoMethod *method, MBCodeGenStatus *s);

/**
 * arch_emit_prologue:
 * @s: pointer to status information
 *
 * Emits the function prolog.
 */
void
arch_emit_prologue (MBCodeGenStatus *s)
{
	x86_push_reg (s->code, X86_EBP);
	x86_mov_reg_reg (s->code, X86_EBP, X86_ESP, 4);
	
	if (s->locals_size)
		x86_alu_reg_imm (s->code, X86_SUB, X86_ESP, s->locals_size);

	if (mono_regset_reg_used (s->rs, X86_EBX)) 
		x86_push_reg (s->code, X86_EBX);

	if (mono_regset_reg_used (s->rs, X86_EDI)) 
		x86_push_reg (s->code, X86_EDI);

	if (mono_regset_reg_used (s->rs, X86_ESI))
		x86_push_reg (s->code, X86_ESI);
}

/**
 * arch_emit_prologue:
 * @s: pointer to status information
 *
 * Emits the function epilog.
 */
void
arch_emit_epilogue (MBCodeGenStatus *s)
{
	if (mono_regset_reg_used (s->rs, X86_EDI))
		x86_pop_reg (s->code, X86_EDI);

	if (mono_regset_reg_used (s->rs, X86_ESI))
		x86_pop_reg (s->code, X86_ESI);

	if (mono_regset_reg_used (s->rs, X86_EBX))
		x86_pop_reg (s->code, X86_EBX);

	x86_leave (s->code);
	x86_ret (s->code);
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for method. If the created
 * code is called then it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * address in method->addr with the result of the JIT compilation
 * step.
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
arch_create_jit_trampoline (MonoMethod *method)
{
	guint8 *code, *buf;

	if (method->addr)
		return method->addr;

	code = buf = g_malloc (64);

	x86_push_reg (buf, X86_EBP);
	x86_mov_reg_reg (buf, X86_EBP, X86_ESP, 4);

	x86_push_imm (buf, method);
	x86_call_code (buf, arch_compile_method);

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

/**
 * arch_compile_method:
 * @method: pointer to the method info
 *
 * JIT compilation of a single method. This method also writes the result 
 * back to method->addr, an thus overwrites the trampoline function.
 *
 * Returns: a pointer to the newly created code.
 */
gpointer
arch_compile_method (MonoMethod *method)
{
	MBCodeGenStatus cgstat;
	MonoMemPool *mp = mono_mempool_new ();
	guint locals_size;

	g_assert (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
	g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

	printf ("Start JIT compilation of %s.%s:%s\n", method->klass->name_space,
		method->klass->name, method->name);

	cgstat.forest = mono_create_forest (method, mp, &locals_size);
	cgstat.code = NULL;
	cgstat.locals_size = locals_size;
	cgstat.mp = mp;

	cgstat.rs = mono_regset_new (X86_NREG);
	mono_regset_reserve_reg (cgstat.rs, X86_ESP);
	mono_regset_reserve_reg (cgstat.rs, X86_EBP);

	emit_method (method, &cgstat);

	mono_regset_free (cgstat.rs);
	g_ptr_array_free (cgstat.forest, TRUE);

	mono_mempool_destroy (mp);

	return method->addr;
}

static void
forest_label (MBCodeGenStatus *s)
{
	GPtrArray *forest = s->forest;
	const int top = forest->len;
	int i;
	
	for (i = 0; i < top; i++) {
		MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);
		MBState *mbstate;

		mbstate =  mono_burg_label (t1, s);
		if (!mbstate) {
			g_warning ("tree does not match");
			mono_print_ctree (t1); printf ("\n\n");

			mono_print_forest (forest);
			g_assert_not_reached ();
		}
	}
}

static void
tree_allocate_regs (MBTree *tree, int goal, MonoRegSet *rs) 
{
	MBTree *kids[10];
	int ern = mono_burg_rule (tree->state, goal);
	guint16 *nts = mono_burg_nts [ern];
	int i;
	
	mono_burg_kids (tree, ern, kids);

	switch (goal) {
	case MB_NTERM_reg:
	case MB_NTERM_lreg: {
		switch (tree->op) {
		case MB_TERM_SHL:
		case MB_TERM_SHR:
		case MB_TERM_SHR_UN:
			tree->exclude_mask |= (1 << X86_ECX);
			tree->left->exclude_mask |= (1 << X86_ECX);
			break;
		case MB_TERM_CALL_R8:
		case MB_TERM_CALL_I4:
			tree->reg1 = X86_EAX;
			break;
		case MB_TERM_CALL_I8:
		case MB_TERM_MUL:
			tree->reg1 = X86_EAX;
			tree->reg2 = X86_EDX;
			break;
		case MB_TERM_DIV:
		case MB_TERM_DIV_UN:
		case MB_TERM_REM:
		case MB_TERM_REM_UN:
			tree->reg1 = X86_EAX;
			tree->reg2 = X86_EDX;
			if (goal == MB_NTERM_reg) {
				tree->left->exclude_mask |= (1 << X86_EDX);
				tree->right->exclude_mask |= (1 << X86_EDX);
			}
			break;
		default:
			break;
		}
	}
	default:
		break;
	}

	for (i = 0; nts [i]; i++)
		tree_allocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++) {
		if (kids [i] != tree) { /* we do not free register for chain rules */
			mono_regset_free_reg (rs, kids [i]->reg1);
			mono_regset_free_reg (rs, kids [i]->reg2);
		}
	}

	switch (goal) {
	case MB_NTERM_reg:
		if ((tree->reg1 = 
		     mono_regset_alloc_reg (rs, tree->reg1, tree->exclude_mask)) == -1) {
			g_warning ("register allocation failed\n");
			g_assert_not_reached ();
		}

		break;

	case MB_NTERM_lreg:
		if ((tree->reg1 = 
		     mono_regset_alloc_reg (rs, tree->reg1, tree->exclude_mask)) == -1 ||
		    (tree->reg2 = 
		     mono_regset_alloc_reg (rs, tree->reg2, tree->exclude_mask)) == -1) {
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
tree_emit (int goal, MBCodeGenStatus *s, MBTree *tree) 
{
	MBTree *kids[10];
	int i, ern = mono_burg_rule (tree->state, goal);
	guint16 *nts = mono_burg_nts [ern];
	MBEmitFunc emit;

	mono_burg_kids (tree, ern, kids);

	for (i = 0; nts [i]; i++) 
		tree_emit (nts [i], s, kids [i]);

	tree->addr = s->code - s->start;

	if ((emit = mono_burg_func [ern]))
		emit (tree, s);
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
		tree_emit (1, s, t1);
	}

	s->epilog = s->code - s->start;
}

static gint32
get_address (GPtrArray *forest, gint32 cli_addr, gint base, gint len)
{
	gint32 ind, pos;
	MBTree *t1;
	int i;

	// use a simple search
	for (i = base; i < (base + len); i++) {
		t1 = (MBTree *) g_ptr_array_index (forest, i);
		if (t1->cli_addr == cli_addr) {
			t1->jump_target = 1;
			return  t1->first_addr;
		}
	}
	return -1;

	// fixme: this binary search does not work - there is a bug somewhere ?

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

			switch (t1->op) {
			case MB_TERM_SWITCH: {
				guint32 *jt = (guint32 *)t1->data.p;
				int j, m;

				m = jt [0] + 1;
				for (j = 1; j <= m; j++) {
					addr = get_address (forest, jt [j], 0, forest->len);
					if (addr == -1) {
						g_error ("address 0x%x not found at IL_%04x",
							 jt [j], t1->cli_addr);
					}

					if (j < m) /* register jumps needs absolute address */
						jt [j] = (int)(addr + s->start);
					else /* branch needs relative address */
						jt [j] = addr - t1->addr;
				}
				break;
			}
			case MB_TERM_RET:
			case MB_TERM_RETV: {
				addr = s->epilog;
				t1->data.i = addr - t1->addr;
				break;
			}
			default:
				addr = get_address (forest, t1->data.i, 0, forest->len);
				if (addr == -1) {
					g_error ("address 0x%x not found at IL_%04x",
						 t1->data.i, t1->cli_addr);
				}
				t1->data.i = addr - t1->addr;
				break;
			}

			/* emit the jump instruction again to update addresses */
			s->code = s->start + t1->addr;
			((MBEmitFunc)t1->emit) (t1, s);

		}
	}

	s->code = end;
}

static void
emit_method (MonoMethod *method, MBCodeGenStatus *s)
{
	method->addr = s->start = s->code = g_malloc (1024);

	if (mono_jit_dump_forest)
		mono_print_forest (s->forest);

	forest_label (s);

	forest_allocate_regs (s);
	arch_emit_prologue (s);
	forest_emit (s);
	arch_emit_epilogue (s);

	compute_branches (s);

	if (mono_jit_dump_asm)
		mono_disassemble_code (s->start, s->code - s->start);
}


