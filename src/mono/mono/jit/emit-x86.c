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
#include "helpers.h"
#include "codegen.h"
#include "debug.h"


//#define DEBUG_REGALLOC
//#define DEBUG_SPILLS

const char *
arch_get_reg_name (int regnum)
{
	switch (regnum) {
	case 0:
		return "EAX";
	case 1:
		return "ECX";
	case 2:
		return "EDX";
	case 3:
		return "EBX";
	case 4:
		return "ESP";
	case 5:
		return "EBP";
	case 6:
		return "ESI";
	case 7:
		return "EDI";
	}

	g_assert_not_reached ();
	return NULL;
}


/* 
 * we may want a x86-specific header or we 
 * can just declare it extern in x86.brg.
 */
int mono_x86_have_cmov = 0;

static int 
cpuid (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx)
{
#ifdef PIC
	return 0;
#else
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
#endif
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

/*
 * arch_get_argument_info:
 * @csig:  a method signature
 * @param_count: the number of parameters to consider
 * @arg_info: an array to store the result infos
 *
 * Gathers information on parameters such as size, alignment and
 * padding. arg_info should be large enought to hold param_count + 1 entries. 
 *
 * Returns the size of the activation frame.
 */
int
arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	int k, frame_size = 0;
	int size, align, pad;
	
	if (csig->hasthis)
		frame_size += sizeof (gpointer);

	if (MONO_TYPE_ISSTRUCT (csig->ret)) 
		frame_size += sizeof (gpointer);
	
	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		
		if (csig->pinvoke)
			size = mono_type_native_stack_size (csig->params [k], &align);
		else
			size = mono_type_stack_size (csig->params [k], &align);
		
		frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);	
		arg_info [k].pad = pad;
		frame_size += size;
		arg_info [k + 1].pad = 0;
		arg_info [k + 1].size = size;
	}

	align = MONO_FRAME_ALIGNMENT;
	frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);
	arg_info [k].pad = pad;

	return frame_size;
}

static void
enter_method (MonoMethod *method, char *ebp)
{
	int i, j;
	MonoClass *class;
	MonoObject *o;
	char *fname;

	fname = mono_method_full_name (method, TRUE);
	printf ("ENTER: %s\n(", fname);
	g_free (fname);
	
	if (((int)ebp & (MONO_FRAME_ALIGNMENT - 1)) != 0) {
		g_error ("unaligned stack detected (%p)", ebp);
	}

	ebp += 8;

	if (MONO_TYPE_ISSTRUCT (method->signature->ret)) {
		g_assert (!method->signature->ret->byref);

		printf ("VALUERET:%p, ", *((gpointer *)ebp));
		ebp += sizeof (gpointer);
	}

	if (method->signature->hasthis) {
		if (method->klass->valuetype) {
			printf ("value:%p, ", *((gpointer *)ebp));
		} else {
			o = *((MonoObject **)ebp);

			if (o) {
				class = o->vtable->klass;

				if (class == mono_defaults.string_class) {
					printf ("this:[STRING:%p:%s], ", o, mono_string_to_utf8 ((MonoString *)o));
				} else {
					printf ("this:%p[%s.%s], ", o, class->name_space, class->name);
				}
			} else 
				printf ("this:NULL, ");
		}
		ebp += sizeof (gpointer);
	}

	for (i = 0; i < method->signature->param_count; ++i) {
		int size, align;
		MonoType *type = method->signature->params [i];

		if (method->signature->pinvoke)
			size = mono_type_native_stack_size (type, &align);
		else
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
	char *fname;

	fname = mono_method_full_name (method, TRUE);
	printf ("LEAVE: %s", fname);
	g_free (fname);

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
			} else if  (o->vtable->klass == mono_defaults.int64_class) {
				printf ("[INT64:%p:%lld]", o, *((gint64 *)((char *)o + sizeof (MonoObject))));	
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
	int i, j, k, alloc_size, pos;

	x86_push_reg (cfg->code, X86_EBP);
	x86_mov_reg_reg (cfg->code, X86_EBP, X86_ESP, 4);

	alloc_size = cfg->locals_size;
	pos = 0;

	if (method->save_lmf) {
		
		pos += sizeof (MonoLMF);

		/* save the current IP */
		cfg->lmfip_offset = cfg->code + 1 - cfg->start;
		x86_push_imm (cfg->code, 0);
		/* save all caller saved regs */
		x86_push_reg (cfg->code, X86_EBX);
		x86_push_reg (cfg->code, X86_EDI);
		x86_push_reg (cfg->code, X86_ESI);
		x86_push_reg (cfg->code, X86_EBP);

		/* save method info */
		x86_push_imm (cfg->code, method);
	
		/* get the address of lmf for the current thread */
		x86_call_code (cfg->code, mono_get_lmf_addr);
		/* push lmf */
		x86_push_reg (cfg->code, X86_EAX); 
		/* push *lfm (previous_lmf) */
		x86_push_membase (cfg->code, X86_EAX, 0);
		/* *(lmf) = ESP */
		x86_mov_membase_reg (cfg->code, X86_EAX, 0, X86_ESP, 4);
	}

#if 0
	/* activation frame alignment check */
	x86_mov_reg_reg (cfg->code, X86_EAX, X86_ESP, 4);
	x86_alu_reg_imm (cfg->code, X86_AND, X86_EAX, MONO_FRAME_ALIGNMENT - 1);
	x86_alu_reg_imm (cfg->code, X86_CMP, X86_EAX, 0);
	x86_branch32 (cfg->code, X86_CC_EQ, 1, FALSE);
	x86_breakpoint (cfg->code);

#endif

	if (mono_regset_reg_used (cfg->rs, X86_EBX)) {
		x86_push_reg (cfg->code, X86_EBX);
		pos += 4;
	}

	if (mono_regset_reg_used (cfg->rs, X86_EDI)) {
		x86_push_reg (cfg->code, X86_EDI);
		pos += 4;
	}

	if (mono_regset_reg_used (cfg->rs, X86_ESI)) {
		x86_push_reg (cfg->code, X86_ESI);
		pos += 4;
	}

	alloc_size -= pos;

	if (alloc_size)
		x86_alu_reg_imm (cfg->code, X86_SUB, X86_ESP, alloc_size);

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
		gboolean unassigned_locals = TRUE;

		if (cfg->bblocks [0].live_in_set) {
			i = mono_bitset_find_first (cfg->bblocks [0].live_in_set, 
						    cfg->locals_start_index - 1);
			unassigned_locals = (i >= 0 && i < cfg->locals_start_index + 
					     header->num_locals);
		}

		if (unassigned_locals && header->init_locals) {
			MonoVarInfo *vi = &VARINFO (cfg, cfg->locals_start_index + header->num_locals - 1);
			int offset = vi->offset;  
			int size = - offset;
			int inited = 0;
			
			/* do not clear caller saved registers */
			size -= 12;

			for (i = 0; i < header->num_locals; ++i) {
				MonoVarInfo *rv = &VARINFO (cfg, cfg->locals_start_index + i);

				if (rv->reg >= 0) {
					int ind = 1 << rv->reg;
					if (!(inited & ind))
						x86_alu_reg_reg (cfg->code, X86_XOR, rv->reg, rv->reg);
					inited |= ind;
				}
			}

			if (size == 1 || size == 2 || size == 4) {
				x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, size);
				return;
			}
			
			i = size / 4;
			j = size % 4;

			if (i < 3) {
				for (k = 0; k < i; k++) {
					x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, 4);
					offset += 4;
				}

				if (j & 2) {
					x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, 2);
					offset += 2;
				}
				if (j & 1)
					x86_mov_membase_imm (cfg->code, X86_EBP, offset, 0, 1);
				return;
			}
			
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
	int pos;
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

	if (cfg->method->save_lmf) {
		pos = -sizeof (MonoLMF) - 4;
	} else
		pos = -4;

	if (mono_regset_reg_used (cfg->rs, X86_EBX)) {
		x86_mov_reg_membase (cfg->code, X86_EBX, X86_EBP, pos, 4);
		pos -= 4;
	}
	if (mono_regset_reg_used (cfg->rs, X86_EDI)) {
		x86_mov_reg_membase (cfg->code, X86_EDI, X86_EBP, pos, 4);
		pos -= 4;
	}
	if (mono_regset_reg_used (cfg->rs, X86_ESI)) {
		x86_mov_reg_membase (cfg->code, X86_ESI, X86_EBP, pos, 4);
		pos -= 4;
	}

	if (cfg->method->save_lmf) {
		pos = -sizeof (MonoLMF);

		x86_lea_membase (cfg->code, X86_ESP, X86_EBP, pos);

		/* ebx = previous_lmf */
		x86_pop_reg (cfg->code, X86_EBX);
		/* edi = lmf */
		x86_pop_reg (cfg->code, X86_EDI);
		/* *(lmf) = previous_lmf */
		x86_mov_membase_reg (cfg->code, X86_EDI, 0, X86_EBX, 4);

		/* discard method info */
		x86_pop_reg (cfg->code, X86_ESI);

		/* restore caller saved regs */
		x86_pop_reg (cfg->code, X86_EBP);
		x86_pop_reg (cfg->code, X86_ESI);
		x86_pop_reg (cfg->code, X86_EDI);
		x86_pop_reg (cfg->code, X86_EBX);

	}

	x86_leave (cfg->code);
	x86_ret (cfg->code);
}

int
arch_allocate_var (MonoFlowGraph *cfg, int size, int align, MonoVarType vartype, MonoValueType type)
{
	MonoVarInfo vi;

	mono_jit_stats.allocate_var++;

	vi.range.last_use.abs_pos = 0;
	vi.range.first_use.pos.bid = 0xffff;
	vi.range.first_use.pos.tid = 0;	
	vi.isvolatile = 0;
	vi.reg = -1;
	vi.varnum = cfg->varinfo->len;

	if (size != sizeof (gpointer))
		vi.isvolatile = 1;
	
	switch (vartype) {
	case MONO_TEMPVAR:
	case MONO_LOCALVAR: {
		cfg->locals_size += size;
		cfg->locals_size += align - 1;
		cfg->locals_size &= ~(align - 1);

		SET_VARINFO (vi, type, vartype, - cfg->locals_size, size);
		g_array_append_val (cfg->varinfo, vi);
		break;
	}
	case MONO_ARGVAR: {
		int arg_start = 8 + cfg->has_vtarg*4;

		g_assert ((align & 3) == 0);

		SET_VARINFO (vi, type, vartype, cfg->args_size + arg_start, size);
		g_array_append_val (cfg->varinfo, vi);
		
		cfg->args_size += size;
		cfg->args_size += 3;
		cfg->args_size &= ~3;
		break;
	}
	default:
		g_assert_not_reached ();
	}

	return cfg->varinfo->len - 1;
}

static gboolean
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
				if (mono_debug_format != MONO_DEBUG_FORMAT_NONE)
					return FALSE;
				g_warning ("tree does not match in %s: 0x%04x",
					   mono_method_full_name (cfg->method, TRUE), t1->cli_addr);
				mono_print_ctree (cfg, t1); printf ("\n\n");

				mono_print_forest (cfg, forest);
				g_assert_not_reached ();
			}
		}
	}

	return TRUE;
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

			if (nts [2]) {
				if (nts [3]) /* we cant handle four kids */
					g_assert_not_reached ();

				if (!tree_allocate_regs (cfg, kids [2], nts [2], rs, right_exclude_mask, spillcount))
					return FALSE;
				
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
		break;
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
				printf ("\n");
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

			if (nts [2]) {
				g_assert (!nts [3]);
				tree_emit (nts [2], cfg, kids [2], spillcount);
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
		unsigned char *ip = GUINT_TO_POINTER (GPOINTER_TO_UINT (ji->ip) + cfg->start);
		unsigned char *target;

		switch (ji->type) {
		case MONO_JUMP_INFO_BB:
			target = ji->data.bb->addr + cfg->start;
			break;
		case MONO_JUMP_INFO_ABS:
			target = ji->data.target;
			break;
		case MONO_JUMP_INFO_EPILOG:
			target = cfg->epilog + cfg->start;
			break;
		case MONO_JUMP_INFO_IP:
			*(unsigned char**)ip = ip;
			continue;
		default:
			g_assert_not_reached ();
		}
		x86_patch (ip, target);
	}

	/* patch the IP in the LMF saving code */
	if (cfg->lmfip_offset) {
		*((guint32 *)(cfg->start + cfg->lmfip_offset)) =  
			(gint32)(cfg->start + cfg->lmfip_offset);
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

MonoJitInfo *
arch_jit_compile_cfg (MonoDomain *target_domain, MonoFlowGraph *cfg)
{
	MonoJitInfo *ji;
	guint32 ls_used_mask = 0;
	MonoMethod *method = cfg->method;

	ji = mono_mempool_alloc0 (target_domain->mp, sizeof (MonoJitInfo));
		
	cfg->rs = mono_regset_new (X86_NREG);
	mono_regset_reserve_reg (cfg->rs, X86_ESP);
	mono_regset_reserve_reg (cfg->rs, X86_EBP);

	/* we can use this regs for global register allocation */
	mono_regset_reserve_reg (cfg->rs, X86_EBX);
	mono_regset_reserve_reg (cfg->rs, X86_ESI);

	if (mono_use_linear_scan) {
		mono_linear_scan (cfg, &ls_used_mask);
		cfg->rs->used_mask |= ls_used_mask;
	}
	
	if (mono_jit_dump_forest) {
		int i;
		printf ("FOREST %s\n", mono_method_full_name (method, TRUE));
		for (i = 0; i < cfg->block_count; i++) {
			printf ("BLOCK %d:\n", i);
			mono_print_forest (cfg, cfg->bblocks [i].forest);
		}
	}
			
	if (!mono_label_cfg (cfg))
		return NULL;
		
	arch_allocate_regs (cfg);

	/* align to 8 byte boundary */
	cfg->locals_size += 7;
	cfg->locals_size &= ~7;

	arch_emit_prologue (cfg);
	cfg->prologue_end = cfg->code - cfg->start;
	mono_emit_cfg (cfg);
	arch_emit_epilogue (cfg);		
	cfg->epilogue_end = cfg->code - cfg->start;

	mono_compute_branches (cfg);

	ji->code_size = cfg->code - cfg->start;
	ji->used_regs = cfg->rs->used_mask;
	ji->method = method;
	ji->code_start = cfg->start;

	return ji;
}

