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
#include "debug.h"

static void
enter_method (MonoMethod *method, gpointer ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;

	printf ("ENTER: %s.%s::%s\n(", method->klass->name_space,
		method->klass->name, method->name);

	ebp += 8;

	if (ISSTRUCT (method->signature->ret)) {
		int size, align;
		
		g_assert (!method->signature->ret->byref);

		size = mono_type_size (method->signature->ret, &align);

		printf ("VALUERET:%p, ", *((gpointer *)ebp));
		ebp += sizeof (gpointer);
	}

	if (method->signature->hasthis) {
		if (method->klass->valuetype) {
			printf ("value:%p, ", *((gpointer *)ebp));
		} else {
			o = *((MonoObject **)ebp);

			g_assert (o);

			class = o->klass;

			if (class == mono_defaults.string_class) {
				printf ("this:[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
			} else {
				printf ("this:%p[%s.%s], ", o, class->name_space, class->name);
			}
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
		case MONO_TYPE_STRING: {
			MonoString *s = *((MonoString **)ebp);
			if (s) {
				g_assert (((MonoObject *)s)->klass == mono_defaults.string_class);
				printf ("[STRING:%p:%s], ", s, mono_string_to_utf8 (s));
			} else 
				printf ("[STRING:null], ");
			break;
		}
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT: {
			o = *((MonoObject **)ebp);
			if (o) {
				class = o->klass;
				if (class == mono_defaults.string_class) {
					printf ("[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
				} else if (class == mono_defaults.int32_class) {
					printf ("[INT32:%p:%d], ", o, *(gint32 *)((gpointer)o + sizeof (MonoObject)));
				} else
					printf ("[%s.%s:%p], ", class->name_space, class->name, o);
			} else {
				printf ("%p, ", *((gpointer *)(ebp)));				
			}
			break;
		}
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
			printf ("%p, ", *((gpointer *)(ebp)));
			break;
		case MONO_TYPE_I8:
			printf ("%lld, ", *((gint64 *)(ebp)));
			break;
		case MONO_TYPE_R4:
			printf ("%f, ", *((float *)(ebp)));
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

	printf ("LEAVE: %s.%s::%s ", method->klass->name_space,
		method->klass->name, method->name);

	switch (method->signature->ret->type) {
	case MONO_TYPE_VOID:
		break;
	case MONO_TYPE_BOOLEAN:
		if (eax)
			printf ("TRUE:%d", eax);
		else 
			printf ("FALSE");
			
		break;
	case MONO_TYPE_CHAR:
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
		printf ("EAX=%d", eax);
		break;
	case MONO_TYPE_STRING: {
		MonoString *s = (MonoString *)eax;

		if (s) {
			g_assert (((MonoObject *)s)->klass == mono_defaults.string_class);
			printf ("[STRING:%p:%s]", s, mono_string_to_utf8 (s));
		} else 
			printf ("[STRING:null], ");
		break;
	}
	case MONO_TYPE_OBJECT: {
		MonoObject *o = (MonoObject *)eax;
		
		if (o->klass == mono_defaults.boolean_class) {
			printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
		} else if  (o->klass == mono_defaults.int32_class) {
			printf ("[INT32:%p:%d]", o, *((gint32 *)((gpointer)o + sizeof (MonoObject))));	
		} else {
			if (o)
				printf ("[%s.%s:%p]", o->klass->name_space, o->klass->name, o);
			else
				printf ("[OBJECT:%p]", (gpointer)eax);
		}
		break;
	}
	case MONO_TYPE_CLASS:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
		printf ("EAX=%p", (gpointer)eax);
		break;
	case MONO_TYPE_I8:
		*((gint32 *)&l) = eax;
		*((gint32 *)&l + 1) = edx;
		printf ("EAX/EDX=%lld", l);
		break;
	case MONO_TYPE_R8:
		printf ("FP=%f\n", test);
		break;
	default:
		printf ("(unknown return type)");
	}

	printf ("\n");
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
		x86_mov_reg_imm (cfg->code, X86_EAX, enter_method);
		x86_call_reg (cfg->code, X86_EAX);
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
		x86_mov_reg_imm (cfg->code, X86_EAX, leave_method);
		x86_call_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 4);
		x86_pop_reg (cfg->code, X86_EDX);
		x86_pop_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 8);
	}

	if (mono_regset_reg_used (cfg->rs, X86_ESI))
		x86_pop_reg (cfg->code, X86_ESI);

	if (mono_regset_reg_used (cfg->rs, X86_EDI))
		x86_pop_reg (cfg->code, X86_EDI);

	if (mono_regset_reg_used (cfg->rs, X86_EBX))
		x86_pop_reg (cfg->code, X86_EBX);

	x86_leave (cfg->code);
	x86_ret (cfg->code);
}

/*
 * get_unbox_trampoline:
 * @m: method pointer
 *
 * when value type methods are called through the vtable we need to unbox the
 * this argument. This method returns a pointer to a trampoline which does
 * unboxing before calling the method
 */
static gpointer
get_unbox_trampoline (MonoMethod *m)
{
	gpointer p = arch_compile_method (m);
	guint8 *code, *start;
	int this_pos = 4;

	if (!m->signature->ret->byref && m->signature->ret->type == MONO_TYPE_VALUETYPE)
		this_pos = 8;
	    
	start = code = g_malloc (16);

	x86_alu_membase_imm (code, X86_ADD, X86_ESP, this_pos, sizeof (MonoObject));
	x86_jump_code (code, p);
	g_assert ((code - start) < 16);

	return start;
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

	if (m->klass->valuetype) {
		return *((gpointer *)o) = get_unbox_trampoline (m);
	} else
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
				cfg->invalid = 1;
				if (mono_debug_handle)
					return;
				g_warning ("tree does not match");
				mono_print_ctree (t1); printf ("\n\n");

				mono_print_forest (forest);
				g_assert_not_reached ();
			}
		}
	}
}

static void
tree_preallocate_regs (MBTree *tree, int goal, MonoRegSet *rs) 
{
	switch (tree->op) {
	case MB_TERM_CALL_I4:
	case MB_TERM_CALL_I8:
	case MB_TERM_CALL_R8:
//	case MB_TERM_CALL_VOID :
		tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, tree->exclude_mask);
		tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, tree->exclude_mask);
		tree->reg3 = mono_regset_alloc_reg (rs, X86_ECX, tree->exclude_mask);
		return;
	default: break;
	}

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
		case MB_TERM_DIV:
		case MB_TERM_DIV_UN:
		case MB_TERM_REM:
		case MB_TERM_REM_UN:
			tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, tree->exclude_mask);
			tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, tree->exclude_mask);
			if (goal == MB_NTERM_reg) {
				tree->left->exclude_mask |= (1 << X86_EDX);
				tree->right->exclude_mask |= (1 << X86_EDX);
			}
			break;
		default:
			break;
		}
		break;
	}
	default:
		break;
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

	//printf ("RALLOC START %d %p %d\n",  tree->op, rs->free_mask, goal);

	if (nts [0] && kids [0] == tree) {
		/* chain rule */
		tree_allocate_regs (kids [0], nts [0], rs);
		return;
	}

	for (i = 0; nts [i]; i++)
		tree_preallocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++)
		tree_allocate_regs (kids [i], nts [i], rs);

	for (i = 0; nts [i]; i++) {
		mono_regset_free_reg (rs, kids [i]->reg1);
		mono_regset_free_reg (rs, kids [i]->reg2);
		mono_regset_free_reg (rs, kids [i]->reg3);
	}

	switch (goal) {
	case MB_NTERM_reg:
		if (tree->reg1 < 0) { 
			tree->reg1 = mono_regset_alloc_reg (rs, -1, tree->exclude_mask);
			g_assert (tree->reg1 != -1);
		}
		break;

	case MB_NTERM_lreg:
		if (tree->reg1 < 0) { 
			tree->reg1 = mono_regset_alloc_reg (rs, -1, tree->exclude_mask);
			g_assert (tree->reg1 != -1);
		}
		if (tree->reg2 < 0) { 
			tree->reg2 = mono_regset_alloc_reg (rs, -1, tree->exclude_mask);
			g_assert (tree->reg2 != -1);
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
tree_emit (int goal, MonoFlowGraph *cfg, MBTree *tree) 
{
	MBTree *kids[10];
	int i, ern = mono_burg_rule (tree->state, goal);
	guint16 *nts = mono_burg_nts [ern];
	MBEmitFunc emit;
	int offset;

	mono_burg_kids (tree, ern, kids);

	for (i = 0; nts [i]; i++) 
		tree_emit (nts [i], cfg, kids [i]);

	tree->addr = offset = cfg->code - cfg->start;

	// we assume an instruction uses a maximum of 128 bytes
	if ((cfg->code_size - offset) <= 128) {
		int add = MIN ((cfg->code_size * 2), 1024);

		cfg->code_size += add;
		cfg->start = g_realloc (cfg->start, cfg->code_size);
		g_assert (cfg->start);
		cfg->code = cfg->start + offset;
	}

	if ((emit = mono_burg_func [ern]))
		emit (tree, cfg);

	g_assert ((cfg->code - cfg->start) < cfg->code_size);
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

static int
match_debug_method (MonoMethod* method)
{
	GList *tmp = mono_debug_methods;

	for (; tmp; tmp = tmp->next) {
		if (strcmp (method->name, tmp->data) == 0) {
			return 1;
		}
	}
	return 0;
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

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("Start JIT compilation of %s.%s:%s\n", method->klass->name_space,
			method->klass->name, method->name);
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		MonoClassField *field;
		const char *name = method->name;
		static guint target_offset = 0;
		static guint method_offset = 0;
		guint8 *code;
		gboolean delegate = FALSE;

		if (method->klass->parent && 
		    method->klass->parent->parent == mono_defaults.delegate_class)
			delegate = TRUE;
				
		if (!target_offset) {
			mono_class_init (mono_defaults.delegate_class);

			field = mono_class_get_field_from_name (mono_defaults.delegate_class, "m_target");
			target_offset = field->offset;
			field = mono_class_get_field_from_name (mono_defaults.delegate_class, "method_ptr");
			method_offset = field->offset;
		}
		
		if (delegate && *name == '.' && (strcmp (name, ".ctor") == 0)) {
			method->addr = code = g_malloc (32);
			x86_push_reg (code, X86_EBP);
			x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
			
			/* load the this pointer */
			x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4); 
			/* load m_target arg */
			x86_mov_reg_membase (code, X86_EDX, X86_EBP, 12, 4);
			/* store mtarget */
			x86_mov_membase_reg (code, X86_EAX, target_offset, X86_EDX, 4); 
			/* load method_ptr arg */
			x86_mov_reg_membase (code, X86_EDX, X86_EBP, 16, 4);
			/* store method_ptr */
			x86_mov_membase_reg (code, X86_EAX, method_offset, X86_EDX, 4); 

			x86_leave (code);
			x86_ret (code);

			g_assert ((code - (guint8*)method->addr) < 32);

		} else if (delegate && *name == 'I' && (strcmp (name, "Invoke") == 0)) {
			MonoMethodSignature *csig = method->signature;
			int i, target, this_pos = 4;
			guint8 *source;

			method->addr = g_malloc (32);

			if (csig->ret->type == MONO_TYPE_VALUETYPE) {
				g_assert (!csig->ret->byref);
				this_pos = 8;
			}

			for (i = 0; i < 2; i ++) {
				code = method->addr;
				/* load the this pointer */
				x86_mov_reg_membase (code, X86_EAX, X86_ESP, this_pos, 4);
				/* load mtarget */
				x86_mov_reg_membase (code, X86_EDX, X86_EAX, target_offset, 4); 
				/* check if zero (static method call without this pointer) */
				x86_alu_reg_imm (code, X86_CMP, X86_EDX, 0);
				x86_branch32 (code, X86_CC_EQ, target, TRUE); 
				source = code;
				
				/* virtual call -  we have to replace the this pointer */
				x86_mov_membase_reg (code, X86_ESP, this_pos, X86_EDX, 4); 

				/* jump to method_ptr() */
				target = code - source;
				x86_jump_membase (code, X86_EAX, method_offset);
			}

			g_assert ((code - (guint8*)method->addr) < 32);

		} else {
			if (mono_debug_handle)
				return NULL;
			g_error ("Don't know how to exec runtime method %s.%s::%s", 
				 method->klass->name_space, method->klass->name, method->name);
		}
	
	} else {
		MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
		MonoJitInfo *ji = g_new0 (MonoJitInfo, 1);
		
		cfg = mono_cfg_new (method, mp);

		mono_analyze_flow (cfg);
		if (cfg->invalid)
			return NULL;

		mono_analyze_stack (cfg);
		if (cfg->invalid)
			return NULL;
	
		cfg->rs = mono_regset_new (X86_NREG);
		mono_regset_reserve_reg (cfg->rs, X86_ESP);
		mono_regset_reserve_reg (cfg->rs, X86_EBP);

		cfg->code_size = 256;
		cfg->start = cfg->code = g_malloc (cfg->code_size);

		if (match_debug_method (method))
			x86_breakpoint (cfg->code);

		if (mono_jit_dump_forest) {
			int i;
			printf ("FOREST %s.%s:%s\n", method->klass->name_space,
				method->klass->name, method->name);
			for (i = 0; i < cfg->block_count; i++) {
				printf ("BLOCK %d:\n", i);
				mono_print_forest (cfg->bblocks [i].forest);
			}
		}
	
		mono_label_cfg (cfg);
		if (cfg->invalid)
			return NULL;

		arch_allocate_regs (cfg);

		/* align to 8 byte boundary */
		cfg->locals_size += 7;
		cfg->locals_size &= ~7;

		arch_emit_prologue (cfg);
		mono_emit_cfg (cfg);
		arch_emit_epilogue (cfg);		

		method->addr = cfg->start;

		mono_compute_branches (cfg);
		
		if (mono_jit_dump_asm) {
			char *id = g_strdup_printf ("%s_%s__%s", method->klass->name_space,
						    method->klass->name, method->name);
			mono_disassemble_code (cfg->start, cfg->code - cfg->start, id);
			g_free (id);
		}
		if (mono_debug_handle)
			mono_debug_add_method (mono_debug_handle, cfg);

		ji->code_size = cfg->code - cfg->start;
		ji->used_regs = cfg->rs->used_mask;
		ji->method = method;
		ji->code_start = method->addr;
		mono_jit_info_table_add (mono_jit_info_table, ji);

		if (header->num_clauses) {
			int i, start_block, end_block;

			ji->num_clauses = header->num_clauses;
			ji->clauses = g_new0 (MonoJitExceptionInfo, header->num_clauses);

			for (i = 0; i < header->num_clauses; i++) {
				MonoExceptionClause *ec = &header->clauses [i];
				MonoJitExceptionInfo *ei = &ji->clauses [i];
			
				ei->flags = ec->flags;
				ei->token_or_filter = ec->token_or_filter;

				g_assert (cfg->bcinfo [ec->try_offset].is_block_start);
				start_block = cfg->bcinfo [ec->try_offset].block_id;
				end_block = cfg->bcinfo [ec->try_offset + ec->try_len].block_id;
				g_assert (cfg->bcinfo [ec->try_offset + ec->try_len].is_block_start);
				
				ei->try_start = cfg->start + cfg->bblocks [start_block].addr;
				ei->try_end = cfg->start + cfg->bblocks [end_block].addr;
				
				g_assert (cfg->bcinfo [ec->handler_offset].is_block_start);
				start_block = cfg->bcinfo [ec->handler_offset].block_id;
				ei->handler_start = cfg->start + cfg->bblocks [start_block].addr;	
				
				//printf ("TEST %x %x %x\n", ei->try_start, ei->try_end, ei->handler_start);
			}
		}
		
		mono_regset_free (cfg->rs);

		mono_cfg_free (cfg);

		mono_mempool_destroy (mp);

	}

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("END JIT compilation of %s.%s:%s %p %p\n", method->klass->name_space,
			method->klass->name, method->name, method, method->addr);
	}


	return method->addr;
}

/*
 * arch_get_restore_context:
 *
 * Returns a pointer to a method which restores a previously saved sigcontext.
 */
static gpointer
arch_get_restore_context ()
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	/* restore_contect (struct sigcontext *ctx) */
	/* we do not restore X86_EAX, X86_EDX */

	start = code = malloc (1024);
	
	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_ESP, 4, 4);

	/* get return address, stored in EDX */
	x86_mov_reg_membase (code, X86_EDX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, eip), 4);

	/* restore EBX */
	x86_mov_reg_membase (code, X86_EBX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ebx), 4);
	/* restore EDI */
	x86_mov_reg_membase (code, X86_EDI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, edi), 4);
	/* restore ESI */
	x86_mov_reg_membase (code, X86_ESI, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, esi), 4);
	/* restore ESP */
	x86_mov_reg_membase (code, X86_ESP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, esp), 4);
	/* restore EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ebp), 4);
	/* restore ECX. the exception object is passed here to the catch handler */
	x86_mov_reg_membase (code, X86_ECX, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ecx), 4);

	/* jump to the saved IP */
	x86_jump_reg (code, X86_EDX);

	return start;
}

/*
 * arch_get_call_finally:
 *
 * Returns a pointer to a method which calls a finally handler.
 */
static gpointer
arch_get_call_finally ()
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	/* call_finally (struct sigcontext *ctx, unsigned long eip) */
	start = code = malloc (1024);

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);

	/* load ctx */
	x86_mov_reg_membase (code, X86_EAX, X86_EBP, 8, 4);
	/* load eip */
	x86_mov_reg_membase (code, X86_ECX, X86_EBP, 12, 4);
	/* save EBP */
	x86_push_reg (code, X86_EBP);
	/* set new EBP */
	x86_mov_reg_membase (code, X86_EBP, X86_EAX,  G_STRUCT_OFFSET (struct sigcontext, ebp), 4);
	/* call the handler */
	x86_call_reg (code, X86_ECX);
	/* restore EBP */
	x86_pop_reg (code, X86_EBP);
	/* restore saved regs */
	x86_pop_reg (code, X86_ESI);
	x86_pop_reg (code, X86_EDI);
	x86_pop_reg (code, X86_EBX);
	x86_leave (code);
	x86_ret (code);

	return start;
}

/**
 * arch_handle_exception:
 * @ctx: saved processor state
 * @obj:
 */
void
arch_handle_exception (struct sigcontext *ctx, gpointer obj)
{
	MonoJitInfo *ji;
	gpointer ip = (gpointer)ctx->eip;
	static void (*restore_context) (struct sigcontext *);
	static void (*call_finally) (struct sigcontext *, unsigned long);

	ji = mono_jit_info_table_find (mono_jit_info_table, ip);

	if (!restore_context)
		restore_context = arch_get_restore_context ();
	
	if (!call_finally)
		call_finally = arch_get_call_finally ();

	if (ji) { /* we are inside managed code */
		MonoMethod *m = ji->method;
		unsigned next_bp, next_ip;
		int offset = 2;

		if (ji->num_clauses) {
			int i;

			g_assert (ji->clauses);
			
			for (i = 0; i < ji->num_clauses; i++) {
				MonoJitExceptionInfo *ei = &ji->clauses [i];

				if (ei->try_start <= ip && ip <= (ei->try_end)) { 
					/* catch block */
					if (ei->flags == 0 && mono_object_isinst (obj, 
					        mono_class_get (m->klass->image, ei->token_or_filter))) {
					
						ctx->eip = (unsigned long)ei->handler_start;
						ctx->ecx = (unsigned long)obj;
						restore_context (ctx);
						g_assert_not_reached ();
					}
				}
			}

			/* no handler found - we need to call all finally handlers */
			for (i = 0; i < ji->num_clauses; i++) {
				MonoJitExceptionInfo *ei = &ji->clauses [i];

				if (ei->try_start <= ip && ip < (ei->try_end) &&
				    (ei->flags & MONO_EXCEPTION_CLAUSE_FINALLY)) {
					call_finally (ctx, (unsigned long)ei->handler_start);
				}
			}
		}

		/* continue unwinding */

		/* restore caller saved registers */
		if (ji->used_regs & X86_ESI_MASK) {
			ctx->esi = *((int *)ctx->ebp + offset);
			offset++;
		}
		if (ji->used_regs & X86_EDI_MASK) {
			ctx->edi = *((int *)ctx->ebp + offset);
			offset++;
		}
		if (ji->used_regs & X86_EBX_MASK) {
			ctx->ebx = *((int *)ctx->ebp + offset);
		}

		ctx->esp = ctx->ebp;
		ctx->eip = *((int *)ctx->ebp + 1);
		ctx->ebp = *((int *)ctx->ebp);
		
		if (next_bp < (unsigned)mono_end_of_stack)
			arch_handle_exception (ctx, obj);
		else
			mono_jit_abort (obj);

	} else {
		gpointer *lmf_addr = TlsGetValue (lmf_thread_id);
		MonoLMF *lmf;
		MonoMethod *m;

		g_assert (lmf_addr);
		lmf = *((MonoLMF **)lmf_addr);

		if (!lmf)
			mono_jit_abort (obj);

		m = lmf->method;

		*lmf_addr = lmf->previous_lmf;

		ctx->esi = lmf->esi;
		ctx->edi = lmf->edi;
		ctx->ebx = lmf->ebx;
		ctx->ebp = lmf->ebp;
		ctx->eip = lmf->eip;
		ctx->esp = lmf;

		/*
		g_warning ("Exception inside unmanaged code. %s.%s::%s %p", m->klass->name_space,
			   m->klass->name, m->name, lmf->previous_lmf);
		*/

		if (ctx->eip < (unsigned)mono_end_of_stack)
			arch_handle_exception (ctx, obj);
		else
			mono_jit_abort (obj);
	}

	g_assert_not_reached ();
}

static void
throw_exception (unsigned long eax, unsigned long ecx, unsigned long edx, unsigned long ebx,
		 unsigned long esi, unsigned long edi, unsigned long ebp, MonoObject *exc,
		 unsigned long eip,  unsigned long esp)
{
	struct sigcontext ctx;
	
	ctx.esp = esp;
	ctx.eip = eip;
	ctx.ebp = ebp;
	ctx.edi = edi;
	ctx.esi = esi;
	ctx.ebx = ebx;
	ctx.edx = edx;
	ctx.ecx = ecx;
	ctx.eax = eax;
	
	arch_handle_exception (&ctx, exc);

	g_assert_not_reached ();
}

gpointer 
arch_get_throw_exception (void)
{
	static guint8 *start = NULL;
	guint8 *code;

	if (start)
		return start;

	code = start = g_malloc (1024);

	x86_push_reg (code, X86_ESP);
	x86_push_membase (code, X86_ESP, 4); /* IP */
	x86_push_membase (code, X86_ESP, 12); /* exception */
	x86_push_reg (code, X86_EBP);
	x86_push_reg (code, X86_EDI);
	x86_push_reg (code, X86_ESI);
	x86_push_reg (code, X86_EBX);
	x86_push_reg (code, X86_EDX);
	x86_push_reg (code, X86_ECX);
	x86_push_reg (code, X86_EAX);
	x86_call_code (code, throw_exception);
	/* we should never reach this breakpoint */
	x86_breakpoint (code);

	return start;
}




