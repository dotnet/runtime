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

static void
enter_method (MonoMethod *method, gpointer ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;

	printf ("ENTER: %s.%s::%s (", method->klass->name_space,
		method->klass->name, method->name);

	ebp += 8;

	if (method->signature->ret->type == MONO_TYPE_VALUETYPE) {
		int size, align;

		if ((size = mono_type_size (method->signature->ret, &align)) > 4 || size == 3) {
			printf ("VALUERET:%p, ", *((gpointer *)ebp));
			ebp += sizeof (gpointer);
		}
	}

	if (method->signature->hasthis) {
		if (method->klass->valuetype) {
			printf ("value:%p, ", *((gpointer *)ebp));
		} else {
			o = *((MonoObject **)ebp);
			class = o->klass;
			printf ("this:%p[%s.%s], ", o, class->name_space, class->name);
		}
		ebp += sizeof (gpointer);
	}

	for (i = 0; i < method->signature->param_count; ++i) {
		MonoType *type = method->signature->params [i];
		int size, align;
		size = mono_type_size (type, &align);

		switch (type->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			printf ("%d, ", *((int *)(ebp)));
			break;
		case MONO_TYPE_STRING:
		case MONO_TYPE_PTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			printf ("%p, ", *((gpointer *)(ebp)));
			break;
		case MONO_TYPE_I8:
			printf ("%lld, ", *((gint64 *)(ebp)));
			break;
		case MONO_TYPE_R8:
			printf ("%f, ", *((double *)(ebp)));
			break;
		case MONO_TYPE_VALUETYPE: 
			printf ("[");
			for (j = 0; j < size; j++)
				printf ("%02x,", *((guint8*)ebp +j));
			printf ("], ");
			break;
		default:
			printf ("XX, ");
		}

		ebp += size + 3;
		ebp = (gpointer)((unsigned)ebp & ~(3));
	}

	printf (")\n");
}

static void
leave_method (MonoMethod *method, int edx, int eax, double test)
{
	gint64 l;

	switch (method->signature->ret->type) {
	case MONO_TYPE_VOID:
		printf ("LEAVE: %s.%s::%s\n", method->klass->name_space,
			method->klass->name, method->name);
		break;
	case MONO_TYPE_BOOLEAN:
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		printf ("LEAVE: %s.%s::%s EAX=%d\n", method->klass->name_space,
			method->klass->name, method->name, eax);
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_PTR:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		printf ("LEAVE: %s.%s::%s EAX=%p\n", method->klass->name_space,
			method->klass->name, method->name, (gpointer)eax);
		break;
	case MONO_TYPE_I8:
		*((gint32 *)&l) = eax;
		*((gint32 *)&l + 1) = edx;
		printf ("LEAVE: %s.%s::%s EAX/EDX=%lld\n", method->klass->name_space,
			method->klass->name, method->name, l);
		break;
	case MONO_TYPE_R8:
		printf ("LEAVE: %s.%s::%s FP=%f\n", method->klass->name_space,
			method->klass->name, method->name, test);
		break;
	default:
		printf ("LEAVE: %s.%s::%s (unknown return type)\n", method->klass->name_space,
			method->klass->name, method->name);
	}
}

/**
 * arch_emit_prologue:
 * @cfg: pointer to status information
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

	if (mono_jit_trace_calls) {
		x86_push_reg (cfg->code, X86_EBP);
		x86_push_imm (cfg->code, cfg->method);
		x86_call_code (cfg->code, enter_method);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 8);
	}
}

/**
 * arch_emit_epilogue:
 * @cfg: pointer to status information
 *
 * Emits the function epilog.
 */
static void
arch_emit_epilogue (MonoFlowGraph *cfg)
{
	if (mono_jit_trace_calls) {
		x86_fld_reg (cfg->code, 0);
		x86_alu_reg_imm (cfg->code, X86_SUB, X86_ESP, 8);
		x86_fst_membase (cfg->code, X86_ESP, 0, TRUE, TRUE);
		x86_push_reg (cfg->code, X86_EAX);
		x86_push_reg (cfg->code, X86_EDX);
		x86_push_imm (cfg->code, cfg->method);
		x86_call_code (cfg->code, leave_method);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 4);
		x86_pop_reg (cfg->code, X86_EDX);
		x86_pop_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 8);
	}

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
 * x86_magic_trampoline:
 * @eax: saved x86 register 
 * @ecx: saved x86 register 
 * @edx: saved x86 register 
 * @esi: saved x86 register 
 * @edi: saved x86 register 
 * @ebx: saved x86 register
 * @code: pointer into caller code
 * @method: the method to translate
 *
 * This method is called by the trampoline functions for virtual
 * methods. It inspects the caller code to find the address of the
 * vtable slot, then calls the JIT compiler and writes the address
 * of the compiled method back to the vtable. All virtual methods 
 * are called with: x86_call_membase (inst, basereg, disp). We always
 * use 32 bit displacement to ensure that the length of the call 
 * instruction is 6 bytes. We need to get the value of the basereg 
 * and the constant displacement.
 */
static gpointer
x86_magic_trampoline (int eax, int ecx, int edx, int esi, int edi, 
		      int ebx, guint8 *code, MonoMethod *m)
{
	guint8 ab, reg;
	gint32 disp;
	gpointer o;

	/* go to the start of the call instruction */
	code -= 6;
	g_assert (*code == 0xff);

	code++;
	ab = *code;
	g_assert ((ab >> 6) == 2);
	
	/* extract the register number containing the address */
	reg = ab & 0x07;
	code++;

	/* extract the displacement */
	disp = *((gint32*)code);

	switch (reg) {
	case X86_EAX:
		o = (gpointer)eax;
		break;
	case X86_EDX:
		o = (gpointer)edx;
		break;
	case X86_ECX:
		o = (gpointer)ecx;
		break;
	case X86_ESI:
		o = (gpointer)esi;
		break;
	case X86_EDI:
		o = (gpointer)edi;
		break;
	case X86_EBX:
		o = (gpointer)ebx;
		break;
	default:
		g_assert_not_reached ();
	}

	o += disp;

	return *((gpointer *)o) = arch_compile_method (m);
}

/**
 * arch_create_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for virtual methods. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * corresponding vtable entry (see x86_magic_trampoline).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
arch_create_jit_trampoline (MonoMethod *method)
{
	guint8 *code, *buf;
	static guint8 *vc = NULL;

	if (method->addr)
		return method->addr;

	if (!vc) {
		vc = buf = g_malloc (24);

		/* push the return address onto the stack */
		x86_push_membase (buf, X86_ESP, 4);

		/* save all register values */
		x86_push_reg (buf, X86_EBX);
		x86_push_reg (buf, X86_EDI);
		x86_push_reg (buf, X86_ESI);
		x86_push_reg (buf, X86_EDX);
		x86_push_reg (buf, X86_ECX);
		x86_push_reg (buf, X86_EAX);

		x86_call_code (buf, x86_magic_trampoline);
		x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 8*4);

		/* call the compiled method */
		x86_jump_reg (buf, X86_EAX);

		g_assert ((buf - vc) <= 24);
	}

	code = buf = g_malloc (16);
	x86_push_imm (buf, method);
	x86_jump_code (buf, vc);
	g_assert ((buf - code) <= 16);

	return code;
}

/**
 * arch_create_simple_jit_trampoline:
 * @method: pointer to the method info
 *
 * Creates a trampoline function for method. If the created
 * code is called it first starts JIT compilation of method,
 * and then calls the newly created method. I also replaces the
 * address in method->addr with the result of the JIT 
 * compilation step (in arch_compile_method).
 * 
 * Returns: a pointer to the newly created code 
 */
gpointer
arch_create_simple_jit_trampoline (MonoMethod *method)
{
	guint8 *code, *buf;

	if (method->addr)
		return method->addr;

	/* we never free the allocated code buffer */
	code = buf = g_malloc (16);
	x86_push_imm (buf, method);
	x86_call_code (buf, arch_compile_method);
	x86_alu_reg_imm (buf, X86_ADD, X86_ESP, 4);
	/* jump to the compiled method */
	x86_jump_reg (buf, X86_EAX);
	g_assert ((buf - code) < 16);

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
		case MB_TERM_CALL_I4:
			tree->reg1 = X86_EAX;
			break;
		case MB_TERM_CALL_I8:
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

	//printf ("RALLOC START %d %p %d\n",  tree->op, rs->free_mask, goal);

	if (nts [0] && kids [0] == tree) {
		/* chain rule */
		tree_allocate_regs (kids [0], nts [0], rs);
		return;
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
		     mono_regset_alloc_reg (rs, tree->reg1, tree->exclude_mask)) == -1) {
			g_warning ("register allocation failed %d 0x%08x 0x%08x\n",  tree->reg1, rs->free_mask, tree->exclude_mask);
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
      
	case MB_NTERM_addr:
		if (tree->op == MB_TERM_ADD) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
			tree->reg2 = mono_regset_alloc_reg (rs, tree->right->reg1, tree->exclude_mask);
		}
		if (tree->op == MB_TERM_CALL_I4) {
			tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, tree->exclude_mask);
		}
		break;
		
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

	//printf ("RALLOC END %d %p\n",  tree->op, rs->free_mask);
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
			g_assert (cfg->rs->free_mask == 0xffffffff);
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

	if (mono_jit_trace_calls) {
		printf ("Start JIT compilation of %s.%s:%s\n", method->klass->name_space,
			method->klass->name, method->name);
	}

	cfg = mono_cfg_new (method, mp);

	mono_analyze_flow (cfg);

	mono_analyze_stack (cfg);

	cfg->code = NULL;
	cfg->rs = mono_regset_new (X86_NREG);
	mono_regset_reserve_reg (cfg->rs, X86_ESP);
	mono_regset_reserve_reg (cfg->rs, X86_EBP);

	// fixme: remove limitation to 4096 bytes
	method->addr = cfg->start = cfg->code = g_malloc (4096);

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

	if (mono_jit_trace_calls) {
		printf ("END JIT compilation of %s.%s:%s %p %p\n", method->klass->name_space,
			method->klass->name, method->name, method, method->addr);
	}

	return method->addr;
}



