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
#include <mono/metadata/profiler-private.h>

#include "jit.h"
#include "codegen.h"
#include "debug.h"


//#define DEBUG_REGALLOC
//#define DEBUG_SPILLS

/* 
 * we may want a x86-specific header or we 
 * can just declare it extern in x86.brg.
 */
int mono_x86_have_cmov = 0;

static int 
cpuid (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx)
{
	int have_cpuid = 0;
	__asm__  __volatile__ (
		"pushfl\n"
		"popl %%eax\n"
		"movl %%eax, %%edx\n"
		"xorl $0x200000, %%eax\n"
		"pushl %%eax\n"
		"popfl\n"
		"pushfl\n"
		"popl %%eax\n"
		"xorl %%edx, %%eax\n"
		"andl $0x200000, %%eax\n"
		"movl %%eax, %0"
		: "=r" (have_cpuid)
		:
		: "%eax", "%edx"
	);

	if (have_cpuid) {
		__asm__ __volatile__ ("cpuid"
			: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
			: "a" (id));
		return 1;
	}
	return 0;
}

void
mono_cpu_detect (void) {
	int eax, ebx, ecx, edx;

	/* Feature Flags function, flags returned in EDX. */
	if (cpuid(1, &eax, &ebx, &ecx, &edx)) {
		if (edx & (1U << 15)) {
			mono_x86_have_cmov = 1;
		}
	}
}

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

		g_assert (align == 4 || align == 8);
		ebp += size + align - 1;
		ebp = (gpointer)((unsigned)ebp & ~(align - 1));
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
	MonoMethod *method = cfg->method;
	MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
	int i, j;

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
	if (mono_jit_profile) {
		x86_push_imm (cfg->code, cfg->method);
		x86_mov_reg_imm (cfg->code, X86_EAX, mono_profiler_method_enter);
		x86_call_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 4);
	}

	/* initialize local vars */
	if (header->num_locals) {

		if (header->init_locals) {
			MonoVarInfo *vi = &VARINFO (cfg, cfg->locals_start_index + header->num_locals - 1);
			int offset = vi->offset;  
			int size = - offset;

			for (i = 0; i < header->num_locals; ++i) {
				MonoVarInfo *rv = &VARINFO (cfg, cfg->locals_start_index + header->num_locals - 1);

				if (rv->reg >= 0)
					x86_alu_reg_imm (cfg->code, X86_XOR, rv->reg, rv->reg);
			}

			if (size == 1 || size == 2 || size == 4) {
				x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, size);
				return;
			}
			
			i = size / 4;
			j = size % 4;
	
			if (i) {
				if (!mono_regset_reg_used (cfg->rs, X86_EDI)) 
					x86_push_reg (cfg->code, X86_EDI);
				x86_lea_membase (cfg->code, X86_EDI, X86_EBP, offset);
				x86_alu_reg_reg (cfg->code, X86_XOR, X86_EAX, X86_EAX);
				x86_mov_reg_imm (cfg->code, X86_ECX, i);
				x86_cld (cfg->code);
				x86_prefix (cfg->code, X86_REP_PREFIX);
				x86_stosl (cfg->code);
				for (i = 0; i < j; i++)
					x86_stosb (cfg->code);
				if (!mono_regset_reg_used (cfg->rs, X86_EDI)) 
					x86_pop_reg (cfg->code, X86_EDI);
			} else {

				g_assert (j == 3);
				x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, 2);
				x86_mov_membase_imm (cfg->code, X86_EBP, offset + 2, 0, 1);
			}
			
		} else {

			/* we always need to initialize object pointers */

			for (i = 0; i < header->num_locals; ++i) {
				MonoType *t = header->locals [i];
				int offset = VARINFO (cfg, cfg->locals_start_index + i).offset;  

				if (t->byref) {
					x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, 4);
					continue;
				}

				switch (t->type) {
				case MONO_TYPE_STRING:
				case MONO_TYPE_CLASS:
				case MONO_TYPE_ARRAY:
				case MONO_TYPE_SZARRAY:
				case MONO_TYPE_OBJECT:
					x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, 4);
					break;
				}

			}
		}
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
	int pos = 4;
	/*
	 * note: with trace and profiling the value on the FP stack may get clobbered.
	 */
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
	if (mono_jit_profile) {
		x86_push_reg (cfg->code, X86_EAX);
		x86_push_reg (cfg->code, X86_EDX);
		x86_push_imm (cfg->code, cfg->method);
		x86_mov_reg_imm (cfg->code, X86_EAX, mono_profiler_method_leave);
		x86_call_reg (cfg->code, X86_EAX);
		x86_alu_reg_imm (cfg->code, X86_ADD, X86_ESP, 4);
		x86_pop_reg (cfg->code, X86_EDX);
		x86_pop_reg (cfg->code, X86_EAX);
	}

	if (mono_regset_reg_used (cfg->rs, X86_EBX)) {
		x86_mov_reg_membase (cfg->code, X86_EBX, X86_EBP, - (cfg->locals_size + pos), 4);
		pos += 4;
	}
	if (mono_regset_reg_used (cfg->rs, X86_EDI)) {
		x86_mov_reg_membase (cfg->code, X86_EDI, X86_EBP, - (cfg->locals_size + pos), 4);
		pos += 4;
	}
	if (mono_regset_reg_used (cfg->rs, X86_ESI)) {
		x86_mov_reg_membase (cfg->code, X86_ESI, X86_EBP, - (cfg->locals_size + pos), 4);
		pos += 4;
	}

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
				mono_print_ctree (cfg, t1); printf ("\n\n");

				mono_print_forest (cfg, forest);
				g_assert_not_reached ();
			}
		}
	}
}

static gboolean
tree_allocate_regs (MonoFlowGraph *cfg, MBTree *tree, int goal, MonoRegSet *rs, 
		    guint8 exclude_mask, int *spillcount) 
{
	MBTree *kids[10];
	int ern = mono_burg_rule (tree->state, goal);
	const guint16 *nts = mono_burg_nts [ern];
	guint8 left_exclude_mask = 0, right_exclude_mask = 0;
	int i;
	
#ifdef DEBUG_REGALLOC
	printf ("tree_allocate_regs start %d %08x %d %d\n",  tree->op, rs->free_mask, goal, 
		(nts [0] && kids [0] == tree));
#endif

	mono_burg_kids (tree, ern, kids);

	switch (tree->op) {
	case MB_TERM_SHL:
	case MB_TERM_SHR:
	case MB_TERM_SHR_UN:
		exclude_mask |= (1 << X86_ECX);
		left_exclude_mask |= (1 << X86_ECX);
		break;
	case MB_TERM_MUL:
	case MB_TERM_MUL_OVF:
	case MB_TERM_MUL_OVF_UN:
	case MB_TERM_DIV:
	case MB_TERM_DIV_UN:
	case MB_TERM_REM:
	case MB_TERM_REM_UN:
		if (goal == MB_NTERM_reg) {
			left_exclude_mask |= (1 << X86_EDX);
			right_exclude_mask |= (1 << X86_EDX) | (1 << X86_EAX);
		}
		break;
	default:
		break;
	}

	if (nts [0] && kids [0] == tree) {
		/* chain rule */
		if (!tree_allocate_regs (cfg, kids [0], nts [0], rs, exclude_mask, spillcount))
			return FALSE;
		/* special case reg: coni4 */
		if (goal == MB_NTERM_reg) {
			if (tree->reg1 == -1)
				tree->reg1 = mono_regset_alloc_reg (rs, -1, exclude_mask);
			if (tree->reg1 == -1)
				return FALSE;
		}
		return TRUE;
	}

	if (tree->spilled) {
		if (tree->reg1 >= 0)
			(*spillcount)--;
		if (tree->reg2 >= 0)
			(*spillcount)--;
		if (tree->reg3 >= 0)
			(*spillcount)--;
	}

	tree->reg1 = -1;
	tree->reg2 = -1;
	tree->reg3 = -1;
	
	tree->spilled = 0;
 
	if (nts [0]) {
		if (nts [1]) { /* two kids */
			MonoRegSet saved_rs;
			if (nts [2]) /* we cant handle three kids */
				g_assert_not_reached ();

			if (!tree_allocate_regs (cfg, kids [0], nts [0], rs, left_exclude_mask, spillcount))
				return FALSE;

			saved_rs = *rs;

			if (!tree_allocate_regs (cfg, kids [1], nts [1], rs, right_exclude_mask, spillcount)) {

#ifdef DEBUG_REGALLOC
				printf ("tree_allocate_regs try 1 failed %d %d %d %d\n", 
					nts [1], kids [1]->reg1,
					kids [1]->reg2,kids [1]->reg3);
#endif
				*rs = saved_rs;

				if (kids [0]->reg1 != -1) {
					right_exclude_mask |= 1 << kids [0]->reg1;
					(*spillcount)++;
				}
				if (kids [0]->reg2 != -1) {
					right_exclude_mask |= 1 << kids [0]->reg2;
					(*spillcount)++;
				}
				if (kids [0]->reg3 != -1) {
					right_exclude_mask |= 1 << kids [0]->reg3;
					(*spillcount)++;
				}

				mono_regset_free_reg (rs, kids [0]->reg1);
				mono_regset_free_reg (rs, kids [0]->reg2);
				mono_regset_free_reg (rs, kids [0]->reg3);

				kids [0]->spilled = 1;

				if (!tree_allocate_regs (cfg, kids [1], nts [1], rs, right_exclude_mask, spillcount)) {
#ifdef DEBUG_REGALLOC
					printf ("tree_allocate_regs try 2 failed\n");
#endif
					return FALSE;
				}
#ifdef DEBUG_REGALLOC
				printf ("tree_allocate_regs try 2 succesfull\n");
#endif
			}

		} else { /* one kid */
			if (!tree_allocate_regs (cfg, kids [0], nts [0], rs, left_exclude_mask, spillcount))
				return FALSE;			
		}
	}


	for (i = 0; nts [i]; i++) {
		mono_regset_free_reg (rs, kids [i]->reg1);
		mono_regset_free_reg (rs, kids [i]->reg2);
		mono_regset_free_reg (rs, kids [i]->reg3);
	}

	tree->emit = mono_burg_func [ern];

	switch (tree->op) {
	case MB_TERM_CALL_I4:
	case MB_TERM_CALL_I8:
	case MB_TERM_CALL_R8:
	// case MB_TERM_CALL_VOID :
		if ((tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, exclude_mask)) == -1)
			return FALSE;
		if ((tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, exclude_mask)) == -1)
			return FALSE;
		if ((tree->reg3 = mono_regset_alloc_reg (rs, X86_ECX, exclude_mask)) == -1)
			return FALSE;
		return TRUE;
	}

	switch (goal) {
	case MB_NTERM_reg:
		switch (tree->op) {
		case MB_TERM_MUL_OVF_UN:
		case MB_TERM_DIV:
		case MB_TERM_DIV_UN:
		case MB_TERM_REM:
		case MB_TERM_REM_UN:
			if ((tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, exclude_mask)) == -1)
				return FALSE;			
			if ((tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, exclude_mask)) == -1)
				return FALSE;
			break;
		default:
			if ((tree->reg1 = mono_regset_alloc_reg (rs, -1, exclude_mask)) == -1)
				return FALSE;
		}
		break;

	case MB_NTERM_lreg:
		switch (tree->op) {
		case MB_TERM_MUL:
		case MB_TERM_MUL_OVF:
		case MB_TERM_MUL_OVF_UN:
		case MB_TERM_DIV:
		case MB_TERM_DIV_UN:
		case MB_TERM_REM:
		case MB_TERM_REM_UN:
			if ((tree->reg1 = mono_regset_alloc_reg (rs, X86_EAX, exclude_mask)) == -1)
				return FALSE;			
			if ((tree->reg2 = mono_regset_alloc_reg (rs, X86_EDX, exclude_mask)) == -1)
				return FALSE;
			break;
		default:
			if ((tree->reg1 = mono_regset_alloc_reg (rs, -1, exclude_mask)) == -1)
				return FALSE;
			if ((tree->reg2 = mono_regset_alloc_reg (rs, -1, exclude_mask)) == -1)
				return FALSE;
		}
		break;

	case MB_NTERM_freg:
		/* fixme: allocate floating point registers */
		break;
      
	case MB_NTERM_addr:
		if (tree->op == MB_TERM_ADD) {
			if ((tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, exclude_mask)) == -1)
				return FALSE;
			if ((tree->reg2 = mono_regset_alloc_reg (rs, tree->right->reg1, exclude_mask)) == -1)
				return FALSE;
		}
		break;
		
	case MB_NTERM_base:
		if (tree->op == MB_TERM_ADD) {
			if ((tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, exclude_mask)) == -1)
				return FALSE;
		}
		break;
	       
	case MB_NTERM_index:
		if (tree->op == MB_TERM_SHL ||
		    tree->op == MB_TERM_MUL) {
			if ((tree->reg1 = mono_regset_alloc_reg (rs, tree->left->reg1, exclude_mask)) == -1)
				return FALSE;
		}
		break;
	       
	default:
		/* do nothing */
	}

#ifdef DEBUG_REGALLOC
	printf ("tree_allocate_regs end %d %08x\n",  tree->op, rs->free_mask);
#endif
	return TRUE;
}

static void
arch_allocate_regs (MonoFlowGraph *cfg)
{
	int i, j, max_spillcount = 0;
	
	for (i = 0; i < cfg->block_count; i++) {
		GPtrArray *forest = cfg->bblocks [i].forest;
		int top;

		if (!cfg->bblocks [i].reached) /* unreachable code */
			continue;

		top = forest->len;

		for (j = 0; j < top; j++) {
			MBTree *t1 = (MBTree *) g_ptr_array_index (forest, j);
			int spillcount = 0;
#ifdef DEBUG_REGALLOC
			printf ("arch_allocate_regs start %d:%d %08x\n", i, j, cfg->rs->free_mask);
#endif
			if (!tree_allocate_regs (cfg, t1, 1, cfg->rs, 0, &spillcount)) {
				mono_print_ctree (cfg, t1);
				g_error ("register allocation failed");
			}

			max_spillcount = MAX (max_spillcount, spillcount);

#ifdef DEBUG_REGALLOC
			printf ("arch_allocate_regs end %d:%d %08x\n", i, j, cfg->rs->free_mask);
#endif
			g_assert (cfg->rs->free_mask == 0xffffffff);
		}
	}

	/* allocate space for spilled regs */

	cfg->spillvars = mono_mempool_alloc0 (cfg->mp, sizeof (gint) *  max_spillcount);
	cfg->spillcount = max_spillcount;

	for (i = 0; i < max_spillcount; i++) {
		int spillvar;
		spillvar = arch_allocate_var (cfg, sizeof (gpointer), sizeof (gpointer),
					      MONO_TEMPVAR, VAL_I32);
		cfg->spillvars [i] = VARINFO (cfg, spillvar).offset;
	}
}

static void
tree_emit (int goal, MonoFlowGraph *cfg, MBTree *tree, int *spillcount) 
{
	MBTree *kids[10];
	int ern = mono_burg_rule (tree->state, goal);
	const guint16 *nts = mono_burg_nts [ern];
	MBEmitFunc emit;
	int offset;

	mono_burg_kids (tree, ern, kids);

	if (nts [0]) {
		if (nts [1]) {
			int spilloffset1, spilloffset2, spilloffset3;
			
			if (nts [2])
				g_assert_not_reached ();

			tree_emit (nts [0], cfg, kids [0], spillcount);

			if (kids [0]->spilled) {
#ifdef DEBUG_SPILLS
				printf ("SPILL_REGS %d %03x %s.%s:%s\n", 
					nts [0], cfg->code - cfg->start,
					cfg->method->klass->name_space,
					cfg->method->klass->name, cfg->method->name);

				mono_print_ctree (cfg, kids [0]);printf ("\n\n");
#endif
				spilloffset1 = 0;
				spilloffset2 = 0;
				spilloffset3 = 0;

				if (kids [0]->reg1 != -1) {
					spilloffset1 = cfg->spillvars [(*spillcount)++];
					x86_mov_membase_reg (cfg->code, X86_EBP, spilloffset1, 
							     kids [0]->reg1, 4);
				}
				if (kids [0]->reg2 != -1) {
					spilloffset2 = cfg->spillvars [(*spillcount)++];
					x86_mov_membase_reg (cfg->code, X86_EBP, spilloffset2, 
							     kids [0]->reg2, 4);
				}
				if (kids [0]->reg3 != -1) {
					spilloffset3 = cfg->spillvars [(*spillcount)++];
					x86_mov_membase_reg (cfg->code, X86_EBP, spilloffset3, 
							     kids [0]->reg3, 4);
				}
			}

			tree_emit (nts [1], cfg, kids [1], spillcount);

			if (kids [0]->spilled) {

#ifdef DEBUG_SPILLS
				printf ("RELOAD_REGS %03x %s.%s:%s\n", 
					cfg->code - cfg->start,
					cfg->method->klass->name_space,
					cfg->method->klass->name, cfg->method->name);
#endif

				if (kids [0]->reg3 != -1) 
					x86_mov_reg_membase (cfg->code, kids [0]->reg3, X86_EBP, 
							     spilloffset3, 4);
				if (kids [0]->reg2 != -1) 
					x86_mov_reg_membase (cfg->code, kids [0]->reg2, X86_EBP, 
							     spilloffset2, 4);
				if (kids [0]->reg1 != -1) 
					x86_mov_reg_membase (cfg->code, kids [0]->reg1, X86_EBP, 
							     spilloffset1, 4);
			}
		} else {
			tree_emit (nts [0], cfg, kids [0], spillcount);
		}
	}

	g_assert ((*spillcount) <= cfg->spillcount);

	tree->addr = offset = cfg->code - cfg->start;

	/* we assume an instruction uses a maximum of 128 bytes */
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
	int i, j, spillcount;

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
			
			spillcount = 0;
			tree_emit (1, cfg, t1, &spillcount);
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
			code = addr = g_malloc (32);
			x86_push_imm (code, method);
			x86_call_code (code, arch_begin_invoke);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			x86_ret (code);
			g_assert ((code - addr) <= 32);
		} else if (delegate && *name == 'E' && (strcmp (name, "EndInvoke") == 0)) {
			/* this can raise exceptions, so we need a wrapper to save/restore LMF */
			method->addr = (gpointer)arch_end_invoke;
			addr = arch_create_native_wrapper (method);
		} else {
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			if (mono_debug_handle) 
				return NULL;

			g_error ("Don't know how to exec runtime method %s.%s::%s", 
				 method->klass->name_space, method->klass->name, method->name);
		}
	
	} else {
		MonoMethodHeader *header = ((MonoMethodNormal *)method)->header;
		MonoFlowGraph *cfg;
		MonoMemPool *mp;
		gulong code_size_ratio;
		guint32 ls_used_mask = 0;
	
		mono_profiler_method_jit (method);
	
		ji = mono_mempool_alloc0 (target_domain->mp, sizeof (MonoJitInfo));
		
		mp = mono_mempool_new ();

		cfg = mono_cfg_new (method, mp);

		mono_analyze_flow (cfg);
		if (cfg->invalid) {
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			return NULL;
		}
		
		mono_analyze_stack (cfg);
		if (cfg->invalid) {
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			return NULL;
		}
		
		cfg->rs = mono_regset_new (X86_NREG);
		mono_regset_reserve_reg (cfg->rs, X86_ESP);
		mono_regset_reserve_reg (cfg->rs, X86_EBP);

		/* we can use this regs for global register allocation */
		mono_regset_reserve_reg (cfg->rs, X86_EBX);
		mono_regset_reserve_reg (cfg->rs, X86_ESI);

		cfg->code_size = MAX (header->code_size * 5, 256);
		cfg->start = cfg->code = g_malloc (cfg->code_size);

		mono_debug_last_breakpoint_address = cfg->code;

		if (match_debug_method (method) || mono_debug_insert_breakpoint)
			x86_breakpoint (cfg->code);
		else if (mono_debug_handle)
			x86_nop (cfg->code);

		if (mono_debug_insert_breakpoint > 0)
			mono_debug_insert_breakpoint--;

		if (mono_use_linear_scan) {
			mono_linear_scan (cfg, &ls_used_mask);
			cfg->rs->used_mask |= ls_used_mask;
		}

		if (mono_jit_dump_forest) {
			int i;
			printf ("FOREST %s.%s:%s\n", method->klass->name_space,
				method->klass->name, method->name);
			for (i = 0; i < cfg->block_count; i++) {
				printf ("BLOCK %d:\n", i);
				mono_print_forest (cfg, cfg->bblocks [i].forest);
			}
		}
			
		mono_label_cfg (cfg);

		if (cfg->invalid) {
			mono_profiler_method_end_jit (method, MONO_PROFILE_FAILED);
			return NULL;
		}
		
		arch_allocate_regs (cfg);

		/* align to 8 byte boundary */
		cfg->locals_size += 7;
		cfg->locals_size &= ~7;

		arch_emit_prologue (cfg);
		cfg->prologue_end = cfg->code - cfg->start;
		mono_emit_cfg (cfg);
		arch_emit_epilogue (cfg);		
		cfg->epilogue_end = cfg->code - cfg->start;

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

		mono_profiler_method_end_jit (method, MONO_PROFILE_OK);
	}

	if (mono_jit_trace_calls || mono_jit_dump_asm || mono_jit_dump_forest) {
		printf ("END JIT compilation of %s.%s:%s %p %p\n", method->klass->name_space,
			method->klass->name, method->name, method, addr);
	}

	g_hash_table_insert (jit_code_hash, method, addr);

	return addr;
}

