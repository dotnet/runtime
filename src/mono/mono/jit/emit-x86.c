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
#include "codegen.h"

/**
 * arch_emit_prologue:
 * @s: pointer to status information
 *
 * Emits the function prolog.
 */
static void
arch_emit_prologue (MonoFlowGraph *cfg)
{
	x86_push_reg (cfg->code, X86_EBP);
	x86_mov_reg_reg (cfg->code, X86_EBP, X86_ESP, 4);
	
	if (cfg->locals_size)
		x86_alu_reg_imm (cfg->code, X86_SUB, X86_ESP, cfg->locals_size);

	if (mono_regset_reg_used (cfg->rs, X86_EBX)) 
		x86_push_reg (cfg->code, X86_EBX);

	if (mono_regset_reg_used (cfg->rs, X86_EDI)) 
		x86_push_reg (cfg->code, X86_EDI);

	if (mono_regset_reg_used (cfg->rs, X86_ESI))
		x86_push_reg (cfg->code, X86_ESI);
}

/**
 * arch_emit_prologue:
 * @s: pointer to status information
 *
 * Emits the function epilog.
 */
static void
arch_emit_epilogue (MonoFlowGraph *cfg)
{
	if (mono_regset_reg_used (cfg->rs, X86_EDI))
		x86_pop_reg (cfg->code, X86_EDI);

	if (mono_regset_reg_used (cfg->rs, X86_ESI))
		x86_pop_reg (cfg->code, X86_ESI);

	if (mono_regset_reg_used (cfg->rs, X86_EBX))
		x86_pop_reg (cfg->code, X86_EBX);

	x86_leave (cfg->code);
	x86_ret (cfg->code);
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

static void
mono_label_cfg (MonoFlowGraph *cfg)
{
	int i, j;
	
	for (i = 0; i < cfg->block_count; i++) {
		GPtrArray *forest = cfg->bblocks [i].forest;
		const int top = forest->len;

		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			MBState *mbstate;

			mbstate =  mono_burg_label (t1, cfg);
			if (!mbstate) {
				g_warning ("tree does not match");
				mono_print_ctree (t1); printf ("\n\n");

				mono_print_forest (forest);
				g_assert_not_reached ();
			}
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
		//case MB_TERM_MUL:
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
			g_warning ("register allocation failed %d %p %p\n",  tree->reg1, rs->free_mask, tree->exclude_mask);
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

		/*
	case MB_NTERM_addr:
		if (tree->op == MB_TERM_ADD) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
			tree->reg2 = mono_regset_alloc_reg (rs, tree->right->reg1, tree->exclude_mask);
		}
		break;
		*/
	case MB_NTERM_base:
		if (tree->op == MB_TERM_ADD) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
		}
		break;
	       
	case MB_NTERM_index:
		if (tree->op == MB_TERM_SHL ||
		    tree->op == MB_TERM_MUL) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
		}
		break;
	       
	default:
		/* do nothing */
	}

	tree->emit = mono_burg_func [ern];
}

static void
arch_allocate_regs (MonoFlowGraph *cfg)
{
	int i, j;
	
	for (i = 0; i < cfg->block_count; i++) {
		GPtrArray *forest = cfg->bblocks [i].forest;
		const int top = forest->len;

		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			//printf ("AREGSTART %d:%d %p\n", i, j, cfg->rs->free_mask);
			tree_allocate_regs (t1, 1, cfg->rs);
			//printf ("AREGENDT %d:%d %p\n", i, j, cfg->rs->free_mask);
		}
	}
}

static void
tree_emit (int goal, MonoFlowGraph *s, MBTree *tree) 
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
mono_emit_cfg (MonoFlowGraph *cfg)
{
	int i, j;

	for (i = 0; i < cfg->block_count; i++) {
		MonoBBlock *bb = &cfg->bblocks [i];
		GPtrArray *forest = bb->forest;
		const int top = forest->len;

		bb->addr = cfg->code - cfg->start;
	  
		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			tree_emit (1, cfg, t1);
		}
	}
		
	cfg->epilog = cfg->code - cfg->start;
}

static void
mono_compute_branches (MonoFlowGraph *cfg)
{
	guint8 *end;
	int i, j;

	end = cfg->code;

	for (j = 0; j < cfg->block_count; j++) {
		MonoBBlock *bb = &cfg->bblocks [j];
		GPtrArray *forest = bb->forest;
		const int top = forest->len;
	
		for (i = 0; i < top; i++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);

			if (t1->is_jump) {

				if (t1->op == MB_TERM_SWITCH) {
					MonoBBlock **jt = (MonoBBlock **)t1->data.p;
					guint32 *rt = (guint32 *)t1->data.p;

					int m = *((guint32 *)t1->data.p) + 1;
					int j;
					
					for (j = 1; j <= m; j++)
						rt [j] = (int)(jt [j]->addr + cfg->start);
				}

				/* emit the jump instruction again to update addresses */
				cfg->code = cfg->start + t1->addr;
				((MBEmitFunc)t1->emit) (t1, cfg);

			}
		}
	}

	cfg->code = end;
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
	MonoFlowGraph *cfg;
	MonoMemPool *mp = mono_mempool_new ();

	g_assert (!(method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL));
	g_assert (!(method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL));

	printf ("Start JIT compilation of %s.%s:%s\n", method->klass->name_space,
		method->klass->name, method->name);

	cfg = mono_cfg_new (method, mp);

	mono_analyze_flow (cfg);

	mono_analyze_stack (cfg);

	cfg->code = NULL;
	cfg->rs = mono_regset_new (X86_NREG);
	mono_regset_reserve_reg (cfg->rs, X86_ESP);
	mono_regset_reserve_reg (cfg->rs, X86_EBP);

	// fixme: remove limitation to 1024 bytes
	method->addr = cfg->start = cfg->code = g_malloc (1024);

	if (mono_jit_dump_forest) {
		int i;
		for (i = 0; i < cfg->block_count; i++) {
			printf ("BLOCK %d:\n", i);
			mono_print_forest (cfg->bblocks [i].forest);
		}
	}

	mono_label_cfg (cfg);

	arch_allocate_regs (cfg);

	arch_emit_prologue (cfg);

	mono_emit_cfg (cfg);

	arch_emit_epilogue (cfg);

	mono_compute_branches (cfg);
		
	if (mono_jit_dump_asm)
		mono_disassemble_code (cfg->start, cfg->code - cfg->start);

	mono_regset_free (cfg->rs);

	mono_cfg_free (cfg);

	mono_mempool_destroy (mp);

	return method->addr;
}



