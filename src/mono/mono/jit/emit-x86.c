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
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/mono-endian.h>
#include <mono/arch/x86/x86-codegen.h>

#include "jit.h"
#include "codegen.h"
#include "debug.h"

static void
enter_method (MonoMethod *method, char *ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;

	printf ("ENTER: %s.%s::%s\n(", method->klass->name_space,
		method->klass->name, method->name);

	
	if (((int)ebp & 3) != 0) {
		g_error ("unaligned stack detected (%p)", ebp);
	}

	ebp += 8;

	if (ISSTRUCT (method->signature->ret)) {
		int size, align;
		
		g_assert (!method->signature->ret->byref);

		size = mono_type_stack_size (method->signature->ret, &align);

		printf ("VALUERET:%p, ", *((gpointer *)ebp));
		ebp += sizeof (gpointer);
	}

	if (method->signature->hasthis) {
		if (method->klass->valuetype) {
			printf ("value:%p, ", *((gpointer *)ebp));
		} else {
			o = *((MonoObject **)ebp);

			g_assert (o);

			class = o->vtable->klass;

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
		size = mono_type_stack_size (type, &align);

		if (type->byref) {
			printf ("[BYREF:%p], ", *((gpointer *)ebp)); 
		} else switch (type->type) {
			
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
				g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
				printf ("[STRING:%p:%s], ", s, mono_string_to_utf8 (s));
			} else 
				printf ("[STRING:null], ");
			break;
		}
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT: {
			o = *((MonoObject **)ebp);
			if (o) {
				class = o->vtable->klass;
		    
				if (class == mono_defaults.string_class) {
					printf ("[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
				} else if (class == mono_defaults.int32_class) {
					printf ("[INT32:%p:%d], ", o, *(gint32 *)((char *)o + sizeof (MonoObject)));
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

		g_assert (align == 4);
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
			g_assert (((MonoObject *)s)->vtable->klass == mono_defaults.string_class);
			printf ("[STRING:%p:%s]", s, mono_string_to_utf8 (s));
		} else 
			printf ("[STRING:null], ");
		break;
	}
	case MONO_TYPE_OBJECT: {
		MonoObject *o = (MonoObject *)eax;

		if (o) {
			if (o->vtable->klass == mono_defaults.boolean_class) {
				printf ("[BOOLEAN:%p:%d]", o, *((guint8 *)o + sizeof (MonoObject)));		
			} else if  (o->vtable->klass == mono_defaults.int32_class) {
				printf ("[INT32:%p:%d]", o, *((gint32 *)((char *)o + sizeof (MonoObject))));	
			} else
				printf ("[%s.%s:%p]", o->vtable->klass->name_space, o->vtable->klass->name, o);
		} else
			printf ("[OBJECT:%p]", o);
	       
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

static void
mono_label_cfg (MonoFlowGraph *cfg)
{
	int i, j;
	
	for (i = 0; i < cfg->block_count; i++) {
		GPtrArray *forest = cfg->bblocks [i].forest;
		int top;

		if (!cfg->bblocks [i].reached) /* unreachable code */
			continue;
		
		top = forest->len;

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
		case MB_TERM_MUL:
		case MB_TERM_MUL_OVF:
		case MB_TERM_MUL_OVF_UN:
		case MB_TERM_DIV:
		case MB_TERM_DIV_UN:
		case MB_TERM_REM:
		case MB_TERM_REM_UN:
			tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, tree->exclude_mask);
			tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, tree->exclude_mask);
			if (goal == MB_NTERM_reg) {
				tree->left->exclude_mask |= (1 << X86_EDX);
				tree->right->exclude_mask |= (1 << X86_EDX) | (1 << X86_EAX);
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
	const guint16 *nts = mono_burg_nts [ern];
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
		int top;

		if (!cfg->bblocks [i].reached) /* unreachable code */
			continue;

		top = forest->len;

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
	const guint16 *nts = mono_burg_nts [ern];
	MBEmitFunc emit;
	int offset;

	mono_burg_kids (tree, ern, kids);

	for (i = 0; nts [i]; i++) 
		tree_emit (nts [i], cfg, kids [i]);

	tree->addr = offset = cfg->code - cfg->start;

	// we assume an instruction uses a maximum of 128 bytes
	if ((cfg->code_size - offset) <= 128) {
		int add = MIN (cfg->code_size, 128);
		cfg->code_size += add;
		mono_jit_stats.code_reallocs++;
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
		int top;

		if (!bb->reached) /* unreachable code */
			continue;
		
		top = forest->len;

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
	MonoJumpInfo *ji;
	guint8 *end;
	int i, j;

	end = cfg->code;

	for (j = 0; j < cfg->block_count; j++) {
		MonoBBlock *bb = &cfg->bblocks [j];
		GPtrArray *forest = bb->forest;
		int top;
		
		if (!bb->reached) /* unreachable code */
			continue;

		top = forest->len;
	
		for (i = 0; i < top; i++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, i);

			if (t1->op == MB_TERM_SWITCH) {
				MonoBBlock **jt = (MonoBBlock **)t1->data.p;
				guint32 *rt = (guint32 *)t1->data.p;
				int m = *((guint32 *)t1->data.p) + 1;
				int k;
				
				for (k = 1; k <= m; k++)
					rt [k] = (int)(jt [k]->addr + cfg->start);
				
				/* emit the switch instruction again to update addresses */
				cfg->code = cfg->start + t1->addr;
				((MBEmitFunc)t1->emit) (t1, cfg);
			}
		}
	}

	cfg->code = end;

	for (ji = cfg->jump_info; ji; ji = ji->next) {
		gpointer *ip = GUINT_TO_POINTER (GPOINTER_TO_UINT (ji->ip) + cfg->start);
		char *target;

		switch (ji->type) {
		case MONO_JUMP_INFO_BB:
			target = ji->data.bb->addr + cfg->start;
			*ip = target - GPOINTER_TO_UINT(ip) - 4;
			break;
		case MONO_JUMP_INFO_ABS:
			target = ji->data.target;
			*ip = target - GPOINTER_TO_UINT(ip) - 4;
			break;
		case MONO_JUMP_INFO_EPILOG:
			target = cfg->epilog + cfg->start;
			*ip = target - GPOINTER_TO_UINT(ip) - 4;
			break;
		case MONO_JUMP_INFO_IP:
			*ip = ip;
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void
mono_add_jump_info (MonoFlowGraph *cfg, gpointer ip, MonoJumpInfoType type, gpointer target)
{
	MonoJumpInfo *ji = mono_mempool_alloc (cfg->mp, sizeof (MonoJumpInfo));

	ji->type = type;
	ji->ip = GUINT_TO_POINTER (GPOINTER_TO_UINT (ip) - GPOINTER_TO_UINT (cfg->start));
	ji->data.target = target;
	ji->next = cfg->jump_info;

	cfg->jump_info = ji;
}

static int
match_debug_method (MonoMethod* method)
{
	GList *tmp = mono_debug_methods;

	for (; tmp; tmp = tmp->next) {
		if (mono_method_desc_full_match (tmp->data, method))
			return 1;
	}
	return 0;
}

/**
 * arch_compile_method:
 * @method: pointer to the method info
 *
 * JIT compilation of a single method. 
 *
 * Returns: a pointer to the newly created code.
 */
gpointer
arch_compile_method (MonoMethod *method)
{
	MonoDomain *target_domain, *domain = mono_domain_get ();
	MonoJitInfo *ji;
	MonoMemPool *mp;
	guint8 *addr;
	GHashTable *jit_code_hash;

	if ((method->iflags & METHOD_IMPL_ATTRIBUTE_INTERNAL_CALL) ||
	    (method->flags & METHOD_ATTRIBUTE_PINVOKE_IMPL)) {
		if (!method->info)
			method->info = arch_create_native_wrapper (method);
		return method->info;
	}

	if (mono_jit_share_code)
		target_domain = mono_root_domain;
	else 
		target_domain = domain;

	jit_code_hash = target_domain->jit_code_hash;

	if ((addr = g_hash_table_lookup (jit_code_hash, method))) {
		mono_jit_stats.methods_lookups++;
		return addr;
	}

	mono_jit_stats.methods_compiled++;
	
	mp = mono_mempool_new ();

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("Start JIT compilation of %s.%s:%s\n", method->klass->name_space,
			method->klass->name, method->name);
	}

	if (method->iflags & METHOD_IMPL_ATTRIBUTE_RUNTIME) {
		const char *name = method->name;
		guint8 *code;
		gboolean delegate = FALSE;

		if (method->klass->parent == mono_defaults.multicastdelegate_class)
			delegate = TRUE;
				
		if (delegate && *name == '.' && (strcmp (name, ".ctor") == 0)) {
			addr = (gpointer)mono_delegate_ctor;
		} else if (delegate && *name == 'I' && (strcmp (name, "Invoke") == 0)) {
			int size;

			addr = arch_get_delegate_invoke (method, &size);

			if (mono_jit_dump_asm) {
				char *id = g_strdup_printf ("%s.%s_%s", method->klass->name_space,
							    method->klass->name, method->name);
				mono_disassemble_code (addr, size, id);
				g_free (id);
			}
		} else if (delegate && *name == 'B' && (strcmp (name, "BeginInvoke") == 0)) {
			code = addr = g_malloc (256);
			x86_push_imm (code, method);
			x86_call_code (code, arch_begin_invoke);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			x86_ret (code);
		} else if (delegate && *name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
			/* this can raise exceptions, so we need a wrapper to save/restore LMF */
			method->addr = (gpointer)arch_end_invoke;
			addr = arch_create_native_wrapper (method);
		} else {
			if (mono_debug_handle) 
				return NULL;

			g_error ("Don't know how to exec runtime method %s.%s::%s", 
				 method->klass->name_space, method->klass->name, method->name);
		}
	
	} else {
		MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
		MonoFlowGraph *cfg;

		gulong code_size_ratio;

		ji = mono_mempool_alloc0 (target_domain->mp, sizeof (MonoJitInfo));
		
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

		cfg->code_size = MAX (header->code_size * 5, 256);
		cfg->start = cfg->code = g_malloc (cfg->code_size);

		mono_debug_last_breakpoint_address = cfg->code;

		if (match_debug_method (method) || mono_debug_insert_breakpoint)
			x86_breakpoint (cfg->code);
		else if (mono_debug_handle)
			x86_nop (cfg->code);

		if (mono_debug_insert_breakpoint > 0)
			mono_debug_insert_breakpoint--;

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
		cfg->prologue_end = cfg->code - cfg->start;
		mono_emit_cfg (cfg);
		cfg->epilogue_begin = cfg->code - cfg->start;
		arch_emit_epilogue (cfg);		

		addr = cfg->start;

		mono_jit_stats.allocated_code_size += cfg->code_size;

		code_size_ratio = cfg->code - cfg->start;
		if (code_size_ratio > mono_jit_stats.biggest_method_size) {
			mono_jit_stats.biggest_method_size = code_size_ratio;
			mono_jit_stats.biggest_method = method;
		}
		code_size_ratio = (code_size_ratio * 100) / header->code_size;
		if (code_size_ratio > mono_jit_stats.max_code_size_ratio) {
			mono_jit_stats.max_code_size_ratio = code_size_ratio;
			mono_jit_stats.max_ratio_method = method;
		}

		mono_compute_branches (cfg);
		
		if (mono_jit_dump_asm) {
			char *id = g_strdup_printf ("%s.%s_%s", method->klass->name_space,
						    method->klass->name, method->name);
			mono_disassemble_code (cfg->start, cfg->code - cfg->start, id);
			g_free (id);
		}
		if (mono_debug_handle)
			mono_debug_add_method (mono_debug_handle, cfg);

		ji->code_size = cfg->code - cfg->start;
		ji->used_regs = cfg->rs->used_mask;
		ji->method = method;
		ji->code_start = addr;

		mono_jit_stats.native_code_size += ji->code_size;

		if (header->num_clauses) {
			int i, start_block, end_block;

			ji->num_clauses = header->num_clauses;
			ji->clauses = mono_mempool_alloc0 (target_domain->mp, 
			        sizeof (MonoJitExceptionInfo) * header->num_clauses);

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
		
		mono_jit_info_table_add (target_domain, ji);

		mono_regset_free (cfg->rs);

		mono_cfg_free (cfg);

		mono_mempool_destroy (mp);

	}

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("END JIT compilation of %s.%s:%s %p %p\n", method->klass->name_space,
			method->klass->name, method->name, method, addr);
	}

	g_hash_table_insert (jit_code_hash, method, addr);

	return addr;
}

