/*
 * mini-ppc.c: PowerPC backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>

#include "mini-ppc.h"
#include "inssel.h"
#include "cpu-g4.h"
#include "trace.h"

int mono_exc_esp_offset = 0;

const char*
mono_arch_regname (int reg) {
	static const char * rnames[] = {
		"ppc_r0", "ppc_sp", "ppc_r2", "ppc_r3", "ppc_r4",
		"ppc_r5", "ppc_r6", "ppc_r7", "ppc_r8", "ppc_r9",
		"ppc_r10", "ppc_r11", "ppc_r12", "ppc_r13", "ppc_r14",
		"ppc_r15", "ppc_r16", "ppc_r17", "ppc_r18", "ppc_r19",
		"ppc_r20", "ppc_r21", "ppc_r22", "ppc_r23", "ppc_r24",
		"ppc_r25", "ppc_r26", "ppc_r27", "ppc_r28", "ppc_r29",
		"ppc_r30", "ppc_r31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

/* this function overwrites r0, r11, r12 */
static guint8*
emit_memcpy (guint8 *code, int size, int dreg, int doffset, int sreg, int soffset)
{
	/* unrolled, use the counter in big */
	if (size > sizeof (gpointer) * 5) {
		int shifted = size >> 2;
		guint8 *copy_loop_start, *copy_loop_jump;

		ppc_load (code, ppc_r0, shifted);
		ppc_mtctr (code, ppc_r0);
		g_assert (sreg == ppc_r11);
		ppc_addi (code, ppc_r12, dreg, (doffset - 4));
		ppc_addi (code, ppc_r11, sreg, (soffset - 4));
		copy_loop_start = code;
		ppc_lwzu (code, ppc_r0, ppc_r11, 4);
		ppc_stwu (code, ppc_r0, 4, ppc_r12);
		copy_loop_jump = code;
		ppc_bc (code, PPC_BR_DEC_CTR_NONZERO, 0, 0);
		ppc_patch (copy_loop_jump, copy_loop_start);
		size -= shifted * 4;
		doffset = soffset = 0;
		dreg = ppc_r12;
	}
	while (size >= 4) {
		ppc_lwz (code, ppc_r0, soffset, sreg);
		ppc_stw (code, ppc_r0, doffset, dreg);
		size -= 4;
		soffset += 4;
		doffset += 4;
	}
	while (size >= 2) {
		ppc_lhz (code, ppc_r0, soffset, sreg);
		ppc_sth (code, ppc_r0, doffset, dreg);
		size -= 2;
		soffset += 2;
		doffset += 2;
	}
	while (size >= 1) {
		ppc_lbz (code, ppc_r0, soffset, sreg);
		ppc_stb (code, ppc_r0, doffset, dreg);
		size -= 1;
		soffset += 1;
		doffset += 1;
	}
	return code;
}

/*
 * mono_arch_get_argument_info:
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
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	int k, frame_size = 0;
	int size, align, pad;
	int offset = 8;

	if (MONO_TYPE_ISSTRUCT (csig->ret)) { 
		frame_size += sizeof (gpointer);
		offset += 4;
	}

	arg_info [0].offset = offset;

	if (csig->hasthis) {
		frame_size += sizeof (gpointer);
		offset += 4;
	}

	arg_info [0].size = frame_size;

	for (k = 0; k < param_count; k++) {
		
		if (csig->pinvoke)
			size = mono_type_native_stack_size (csig->params [k], &align);
		else
			size = mono_type_stack_size (csig->params [k], &align);

		/* ignore alignment for now */
		align = 1;

		frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);	
		arg_info [k].pad = pad;
		frame_size += size;
		arg_info [k + 1].pad = 0;
		arg_info [k + 1].size = size;
		offset += pad;
		arg_info [k + 1].offset = offset;
		offset += size;
	}

	align = MONO_ARCH_FRAME_ALIGNMENT;
	frame_size += pad = (align - (frame_size & (align - 1))) & (align - 1);
	arg_info [k].pad = pad;

	return frame_size;
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	guint32 opts = 0;

	/* no ppc-specific optimizations yet */
	*exclude_mask = MONO_OPT_INLINE;
	return opts;
}

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	t = mono_type_get_underlying_type (t);
	switch (t->type) {
	case MONO_TYPE_I4:
	case MONO_TYPE_U4:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
		return FALSE;
	}
	return FALSE;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		/* we can only allocate 32 bit values */
		if (is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = mono_varlist_insert_sorted (cfg, vars, vmv, FALSE);
		}
	}

	return vars;
}

#define USE_EXTRA_TEMPS ((1<<30) | (1<<29))
//#define USE_EXTRA_TEMPS 0

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i, top = 32;
	if (cfg->frame_reg != ppc_sp)
		top = 31;
#if USE_EXTRA_TEMPS
	top = 29;
#endif
	for (i = 13; i < top; ++i)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (i));

	return regs;
}

/*
 * mono_arch_regalloc_cost:
 *
 *  Return the cost, in number of memory references, of the action of 
 * allocating the variable VMV into a register during global register
 * allocation.
 */
guint32
mono_arch_regalloc_cost (MonoCompile *cfg, MonoMethodVar *vmv)
{
	/* FIXME: */
	return 2;
}

// code from ppc/tramp.c, try to keep in sync
#define MIN_CACHE_LINE 8

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	guint i;
	guint8 *p;

	p = code;
	for (i = 0; i < size; i += MIN_CACHE_LINE, p += MIN_CACHE_LINE) {
		asm ("dcbst 0,%0;" : : "r"(p) : "memory");
	}
	asm ("sync");
	p = code;
	for (i = 0; i < size; i += MIN_CACHE_LINE, p += MIN_CACHE_LINE) {
		asm ("icbi 0,%0; sync;" : : "r"(p) : "memory");
	}
	asm ("sync");
	asm ("isync");
}

#define NOT_IMPLEMENTED(x) \
                g_error ("FIXME: %s is not yet implemented. (trampoline)", x);

#ifdef __APPLE__
#define ALWAYS_ON_STACK(s) s
#define FP_ALSO_IN_REG(s) s
#else
#define ALWAYS_ON_STACK(s)
#define FP_ALSO_IN_REG(s) s
#define ALIGN_DOUBLES
#endif

enum {
	RegTypeGeneral,
	RegTypeBase,
	RegTypeFP,
	RegTypeStructByVal,
	RegTypeStructByAddr
};

typedef struct {
	gint32  offset;
	guint16 vtsize; /* in param area */
	guint8  reg;
	guint8  regtype : 4; /* 0 general, 1 basereg, 2 floating point register, see RegType* */
	guint8  size    : 4; /* 1, 2, 4, 8, or regs used by RegTypeStructByVal */
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 struct_ret;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a)

static void inline
add_general (guint *gr, guint *stack_size, ArgInfo *ainfo, gboolean simple)
{
	if (simple) {
		if (*gr >= 3 + PPC_NUM_REG_ARGS) {
			ainfo->offset = PPC_STACK_PARAM_OFFSET + *stack_size;
			ainfo->reg = ppc_sp; /* in the caller */
			ainfo->regtype = RegTypeBase;
			*stack_size += 4;
		} else {
			ALWAYS_ON_STACK (*stack_size += 4);
			ainfo->reg = *gr;
		}
	} else {
		if (*gr >= 3 + PPC_NUM_REG_ARGS - 1) {
#ifdef ALIGN_DOUBLES
			//*stack_size += (*stack_size % 8);
#endif
			ainfo->offset = PPC_STACK_PARAM_OFFSET + *stack_size;
			ainfo->reg = ppc_sp; /* in the caller */
			ainfo->regtype = RegTypeBase;
			*stack_size += 8;
		} else {
#ifdef ALIGN_DOUBLES
		if (!((*gr) & 1))
			(*gr) ++;
#endif
			ALWAYS_ON_STACK (*stack_size += 8);
			ainfo->reg = *gr;
		}
		(*gr) ++;
	}
	(*gr) ++;
}

static CallInfo*
calculate_sizes (MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint i, fr, gr;
	int n = sig->hasthis + sig->param_count;
	guint32 simpletype;
	guint32 stack_size = 0;
	CallInfo *cinfo = g_malloc0 (sizeof (CallInfo) + sizeof (ArgInfo) * n);

	fr = PPC_FIRST_FPARG_REG;
	gr = PPC_FIRST_ARG_REG;

	/* FIXME: handle returning a struct */
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		add_general (&gr, &stack_size, &cinfo->ret, TRUE);
		cinfo->struct_ret = PPC_FIRST_ARG_REG;
	}

	n = 0;
	if (sig->hasthis) {
		add_general (&gr, &stack_size, cinfo->args + n, TRUE);
		n++;
	}
        DEBUG(printf("params: %d\n", sig->param_count));
	for (i = 0; i < sig->param_count; ++i) {
		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
                        /* Prevent implicit arguments and sig_cookie from
			   being passed in registers */
                        gr = PPC_LAST_ARG_REG + 1;
                        /* Emit the signature cookie just before the implicit arguments */
                        add_general (&gr, &stack_size, &cinfo->sig_cookie, TRUE);
                }
                DEBUG(printf("param %d: ", i));
		if (sig->params [i]->byref) {
                        DEBUG(printf("byref\n"));
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			continue;
		}
		simpletype = mono_type_get_underlying_type (sig->params [i])->type;
	enum_calc_size:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			cinfo->args [n].size = 1;
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
			cinfo->args [n].size = 2;
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			cinfo->args [n].size = 4;
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			cinfo->args [n].size = sizeof (gpointer);
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_VALUETYPE: {
			gint size;
			MonoClass *klass;
			klass = mono_class_from_mono_type (sig->params [i]);
			if (is_pinvoke)
			    size = mono_class_native_size (klass, NULL);
			else
			    size = mono_class_value_size (klass, NULL);
			DEBUG(printf ("load %d bytes struct\n",
				      mono_class_native_size (sig->params [i]->data.klass, NULL)));
#if PPC_PASS_STRUCTS_BY_VALUE
			{
				int align_size = size;
				int nwords = 0;
				align_size += (sizeof (gpointer) - 1);
				align_size &= ~(sizeof (gpointer) - 1);
				nwords = (align_size + sizeof (gpointer) -1 ) / sizeof (gpointer);
				cinfo->args [n].regtype = RegTypeStructByVal;
				if (gr > PPC_LAST_ARG_REG || (size >= 3 && size % 4 != 0)) {
					cinfo->args [n].size = 0;
					cinfo->args [n].vtsize = nwords;
				} else {
					int rest = PPC_LAST_ARG_REG - gr + 1;
					int n_in_regs = rest >= nwords? nwords: rest;
					cinfo->args [n].size = n_in_regs;
					cinfo->args [n].vtsize = nwords - n_in_regs;
					cinfo->args [n].reg = gr;
					gr += n_in_regs;
				}
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
				/*g_print ("offset for arg %d at %d\n", n, PPC_STACK_PARAM_OFFSET + stack_size);*/
				stack_size += nwords * sizeof (gpointer);
			}
#else
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			cinfo->args [n].regtype = RegTypeStructByAddr;
#endif
			n++;
			break;
		}
		case MONO_TYPE_TYPEDBYREF: {
			int size = sizeof (MonoTypedRef);
			/* keep in sync or merge with the valuetype case */
#if PPC_PASS_STRUCTS_BY_VALUE
			{
				int nwords = (size + sizeof (gpointer) -1 ) / sizeof (gpointer);
				cinfo->args [n].regtype = RegTypeStructByVal;
				if (gr <= PPC_LAST_ARG_REG) {
					int rest = PPC_LAST_ARG_REG - gr + 1;
					int n_in_regs = rest >= nwords? nwords: rest;
					cinfo->args [n].size = n_in_regs;
					cinfo->args [n].vtsize = nwords - n_in_regs;
					cinfo->args [n].reg = gr;
					gr += n_in_regs;
				} else {
					cinfo->args [n].size = 0;
					cinfo->args [n].vtsize = nwords;
				}
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
				/*g_print ("offset for arg %d at %d\n", n, PPC_STACK_PARAM_OFFSET + stack_size);*/
				stack_size += nwords * sizeof (gpointer);
			}
#else
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			cinfo->args [n].regtype = RegTypeStructByAddr;
#endif
			n++;
			break;
		}
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->args [n].size = 8;
			add_general (&gr, &stack_size, cinfo->args + n, FALSE);
			n++;
			break;
		case MONO_TYPE_R4:
			cinfo->args [n].size = 4;

			/* It was 7, now it is 8 in LinuxPPC */
			if (fr <= PPC_LAST_FPARG_REG) {
				cinfo->args [n].regtype = RegTypeFP;
				cinfo->args [n].reg = fr;
				fr ++;
				FP_ALSO_IN_REG (gr ++);
				ALWAYS_ON_STACK (stack_size += 4);
			} else {
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
				cinfo->args [n].regtype = RegTypeBase;
				cinfo->args [n].reg = ppc_sp; /* in the caller*/
				stack_size += 4;
			}
			n++;
			break;
		case MONO_TYPE_R8:
			cinfo->args [n].size = 8;
			/* It was 7, now it is 8 in LinuxPPC */
			if (fr <= PPC_LAST_FPARG_REG) {
				cinfo->args [n].regtype = RegTypeFP;
				cinfo->args [n].reg = fr;
				fr ++;
				FP_ALSO_IN_REG (gr += 2);
				ALWAYS_ON_STACK (stack_size += 8);
			} else {
				cinfo->args [n].offset = PPC_STACK_PARAM_OFFSET + stack_size;
				cinfo->args [n].regtype = RegTypeBase;
				cinfo->args [n].reg = ppc_sp; /* in the caller*/
				stack_size += 8;
			}
			n++;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	{
		simpletype = mono_type_get_underlying_type (sig->ret)->type;
enum_retvalue:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_FNPTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_STRING:
			cinfo->ret.reg = ppc_r3;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.reg = ppc_r3;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.reg = ppc_f1;
			cinfo->ret.regtype = RegTypeFP;
			break;
		case MONO_TYPE_VALUETYPE:
			break;
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/* align stack size to 16 */
	DEBUG (printf ("      stack size: %d (%d)\n", (stack_size + 15) & ~15, stack_size));
	stack_size = (stack_size + 15) & ~15;

	cinfo->stack_usage = stack_size;
	return cinfo;
}


/*
 * Set var information according to the calling convention. ppc version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;
	int frame_reg = ppc_sp;


	/* allow room for the vararg method args: void* and long/double */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (m->method))
		m->param_area = MAX (m->param_area, sizeof (gpointer)*8);
	/* this is bug #60332: remove when #59509 is fixed, so no weird vararg 
	 * call convs needs to be handled this way.
	 */
	if (m->flags & MONO_CFG_HAS_VARARGS)
		m->param_area = MAX (m->param_area, sizeof (gpointer)*8);

	header = mono_method_get_header (m->method);

	/* 
	 * We use the frame register also for any method that has
	 * exception clauses. This way, when the handlers are called,
	 * the code will reference local variables using the frame reg instead of
	 * the stack pointer: if we had to restore the stack pointer, we'd
	 * corrupt the method frames that are already on the stack (since
	 * filters get called before stack unwinding happens) when the filter
	 * code would call any method (this also applies to finally etc.).
	 */ 
	if ((m->flags & MONO_CFG_HAS_ALLOCA) || header->num_clauses)
		frame_reg = ppc_r31;
	m->frame_reg = frame_reg;
	if (frame_reg != ppc_sp) {
		m->used_int_regs |= 1 << frame_reg;
	}

	sig = m->method->signature;
	
	offset = 0;
	curinst = 0;
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		m->ret->opcode = OP_REGVAR;
		m->ret->inst_c0 = ppc_r3;
	} else {
		/* FIXME: handle long and FP values */
		switch (mono_type_get_underlying_type (sig->ret)->type) {
		case MONO_TYPE_VOID:
			break;
		default:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = ppc_r3;
			break;
		}
	}
	/* local vars are at a positive offset from the stack pointer */
	/* 
	 * also note that if the function uses alloca, we use ppc_r31
	 * to point at the local variables.
	 */
	offset = PPC_MINIMAL_STACK_SIZE; /* linkage area */
	/* align the offset to 16 bytes: not sure this is needed here  */
	//offset += 16 - 1;
	//offset &= ~(16 - 1);

	/* add parameter area size for called functions */
	offset += m->param_area;
	offset += 16 - 1;
	offset &= ~(16 - 1);

	/* allow room to save the return value */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (m->method))
		offset += 8;

	/* the MonoLMF structure is stored just below the stack pointer */

#if 0
	/* this stuff should not be needed on ppc and the new jit,
	 * because a call on ppc to the handlers doesn't change the 
	 * stack pointer and the jist doesn't manipulate the stack pointer
	 * for operations involving valuetypes.
	 */
	/* reserve space to store the esp */
	offset += sizeof (gpointer);

	/* this is a global constant */
	mono_exc_esp_offset = offset;
#endif
	if (sig->call_convention == MONO_CALL_VARARG) {
                m->sig_cookie = PPC_STACK_PARAM_OFFSET;
        }

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		inst = m->ret;
		offset += sizeof(gpointer) - 1;
		offset &= ~(sizeof(gpointer) - 1);
		inst->inst_offset = offset;
		inst->opcode = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset += sizeof(gpointer);
		if (sig->call_convention == MONO_CALL_VARARG)
			m->sig_cookie += sizeof (gpointer);
	}

	curinst = m->locals_start;
	for (i = curinst; i < m->num_varinfo; ++i) {
		inst = m->varinfo [i];
		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR)
			continue;

		/* inst->unused indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->unused && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
			size = mono_class_native_size (mono_class_from_mono_type (inst->inst_vtype), &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		offset += align - 1;
		offset &= ~(align - 1);
		inst->inst_offset = offset;
		inst->opcode = OP_REGOFFSET;
		inst->inst_basereg = frame_reg;
		offset += size;
		//g_print ("allocating local %d to %d\n", i, inst->inst_offset);
	}

	curinst = 0;
	if (sig->hasthis) {
		inst = m->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			offset += sizeof (gpointer) - 1;
			offset &= ~(sizeof (gpointer) - 1);
			inst->inst_offset = offset;
			offset += sizeof (gpointer);
			if (sig->call_convention == MONO_CALL_VARARG)
				m->sig_cookie += sizeof (gpointer);
		}
		curinst++;
	}

	for (i = 0; i < sig->param_count; ++i) {
		inst = m->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = frame_reg;
			size = mono_type_size (sig->params [i], &align);
			offset += align - 1;
			offset &= ~(align - 1);
			inst->inst_offset = offset;
			offset += size;
			if ((sig->call_convention == MONO_CALL_VARARG) && (i < sig->sentinelpos)) 
				m->sig_cookie += size;
		}
		curinst++;
	}

	/* align the offset to 16 bytes */
	offset += 16 - 1;
	offset &= ~(16 - 1);

	/* change sign? */
	m->stack_offset = offset;

}

/* Fixme: we need an alignment solution for enter_method and mono_arch_call_opcode,
 * currently alignment in mono_arch_call_opcode is computed without arch_get_argument_info 
 */

/* 
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 * Issue: who does the spilling if needed, and when?
 */
MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual) {
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = calculate_sizes (sig, sig->pinvoke);
	if (cinfo->struct_ret)
		call->used_iregs |= 1 << cinfo->struct_ret;

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;
		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			MonoInst *sig_arg;
			cfg->disable_aot = TRUE;
				
			MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
			sig_arg->inst_p0 = call->signature;
			
			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			arg->inst_imm = cinfo->sig_cookie.offset;
			arg->inst_left = sig_arg;
			
			/* prepend, so they get reversed */
			arg->next = call->out_args;
			call->out_args = arg;
		}
		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instrucion */
			in = call->args [i];
			call->used_iregs |= 1 << ainfo->reg;
		} else {
			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code = in->cil_code;
			arg->inst_left = in;
			arg->type = in->type;
			/* prepend, we'll need to reverse them later */
			arg->next = call->out_args;
			call->out_args = arg;
			if (ainfo->regtype == RegTypeGeneral) {
				arg->unused = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
				if (arg->type == STACK_I8)
					call->used_iregs |= 1 << (ainfo->reg + 1);
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				/* FIXME: where si the data allocated? */
				arg->unused = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
			} else if (ainfo->regtype == RegTypeStructByVal) {
				int cur_reg;
				/* mark the used regs */
				for (cur_reg = 0; cur_reg < ainfo->size; ++cur_reg) {
					call->used_iregs |= 1 << (ainfo->reg + cur_reg);
				}
				arg->opcode = OP_OUTARG_VT;
				arg->unused = ainfo->reg | (ainfo->size << 8) | (ainfo->vtsize << 16);
				arg->inst_imm = ainfo->offset;
			} else if (ainfo->regtype == RegTypeBase) {
				arg->opcode = OP_OUTARG;
				arg->unused = ainfo->reg | (ainfo->size << 8);
				arg->inst_imm = ainfo->offset;
			} else if (ainfo->regtype == RegTypeFP) {
				arg->opcode = OP_OUTARG_R8;
				arg->unused = ainfo->reg;
				call->used_fregs |= 1 << ainfo->reg;
				if (ainfo->size == 4) {
					arg->opcode = OP_OUTARG_R8;
					/* we reduce the precision */
					/*MonoInst *conv;
					MONO_INST_NEW (cfg, conv, OP_FCONV_TO_R4);
					conv->inst_left = arg->inst_left;
					arg->inst_left = conv;*/
				}
			} else {
				g_assert_not_reached ();
			}
		}
	}
	/*
	 * Reverse the call->out_args list.
	 */
	{
		MonoInst *prev = NULL, *list = call->out_args, *next;
		while (list) {
			next = list->next;
			list->next = prev;
			prev = list;
			list = next;
		}
		call->out_args = prev;
	}
	call->stack_usage = cinfo->stack_usage;
	cfg->param_area = MAX (cfg->param_area, cinfo->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;
	/* 
	 * should set more info in call, such as the stack space
	 * used by the args that needs to be added back to esp
	 */

	g_free (cinfo);
	return call;
}

/*
 * Allow tracing to work with this interface (with an optional argument)
 */

/*
 * This may be needed on some archs or for debugging support.
 */
void
mono_arch_instrument_mem_needs (MonoMethod *method, int *stack, int *code)
{
	/* no stack room needed now (may be needed for FASTCALL-trace support) */
	*stack = 0;
	/* split prolog-epilog requirements? */
	*code = 50; /* max bytes needed: check this number */
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;

	ppc_load (code, ppc_r3, cfg->method);
	ppc_li (code, ppc_r4, 0); /* NULL ebp for now */
	ppc_load (code, ppc_r0, func);
	ppc_mtlr (code, ppc_r0);
	ppc_blrl (code);
	return code;
}

enum {
	SAVE_NONE,
	SAVE_STRUCT,
	SAVE_ONE,
	SAVE_TWO,
	SAVE_FP
};

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
	int save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;
	int rtype = mono_type_get_underlying_type (method->signature->ret)->type;
	int save_offset = PPC_STACK_PARAM_OFFSET + cfg->param_area;
	save_offset += 15;
	save_offset &= ~15;
	
handle_enum:
	switch (rtype) {
	case MONO_TYPE_VOID:
		/* special case string .ctor icall */
		if (strcmp (".ctor", method->name) && method->klass == mono_defaults.string_class)
			save_mode = SAVE_ONE;
		else
			save_mode = SAVE_NONE;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		save_mode = SAVE_TWO;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		save_mode = SAVE_FP;
		break;
	case MONO_TYPE_VALUETYPE:
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_ONE;
		break;
	}

	switch (save_mode) {
	case SAVE_TWO:
		ppc_stw (code, ppc_r3, save_offset, cfg->frame_reg);
		ppc_stw (code, ppc_r4, save_offset + 4, cfg->frame_reg);
		if (enable_arguments) {
			ppc_mr (code, ppc_r5, ppc_r4);
			ppc_mr (code, ppc_r4, ppc_r3);
		}
		break;
	case SAVE_ONE:
		ppc_stw (code, ppc_r3, save_offset, cfg->frame_reg);
		if (enable_arguments) {
			ppc_mr (code, ppc_r4, ppc_r3);
		}
		break;
	case SAVE_FP:
		ppc_stfd (code, ppc_f1, save_offset, cfg->frame_reg);
		if (enable_arguments) {
			/* FIXME: what reg?  */
			ppc_fmr (code, ppc_f3, ppc_f1);
			ppc_lwz (code, ppc_r4, save_offset, cfg->frame_reg);
			ppc_lwz (code, ppc_r5, save_offset + 4, cfg->frame_reg);
		}
		break;
	case SAVE_STRUCT:
		if (enable_arguments) {
			/* FIXME: get the actual address  */
			ppc_mr (code, ppc_r4, ppc_r3);
		}
		break;
	case SAVE_NONE:
	default:
		break;
	}

	ppc_load (code, ppc_r3, cfg->method);
	ppc_load (code, ppc_r0, func);
	ppc_mtlr (code, ppc_r0);
	ppc_blrl (code);

	switch (save_mode) {
	case SAVE_TWO:
		ppc_lwz (code, ppc_r3, save_offset, cfg->frame_reg);
		ppc_lwz (code, ppc_r4, save_offset + 4, cfg->frame_reg);
		break;
	case SAVE_ONE:
		ppc_lwz (code, ppc_r3, save_offset, cfg->frame_reg);
		break;
	case SAVE_FP:
		ppc_lfd (code, ppc_f1, save_offset, cfg->frame_reg);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}
/*
 * Conditional branches have a small offset, so if it is likely overflowed,
 * we do a branch to the end of the method (uncond branches have much larger
 * offsets) where we perform the conditional and jump back unconditionally.
 * It's slightly slower, since we add two uncond branches, but it's very simple
 * with the current patch implementation and such large methods are likely not
 * going to be perf critical anyway.
 */
typedef struct {
	MonoBasicBlock *bb;
	void *ip;
	guint16 b0_cond;
	guint16 b1_cond;
} MonoOvfJump;

#define EMIT_COND_BRANCH_FLAGS(ins,b0,b1) \
if (ins->flags & MONO_INST_BRLABEL) { \
        if (0 && ins->inst_i0->inst_c0) { \
		ppc_bc (code, (b0), (b1), (code - cfg->native_code + ins->inst_i0->inst_c0) & 0xffff);	\
        } else { \
	        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_LABEL, ins->inst_i0); \
		ppc_bc (code, (b0), (b1), 0);	\
        } \
} else { \
        if (0 && ins->inst_true_bb->native_offset) { \
		ppc_bc (code, (b0), (b1), (code - cfg->native_code + ins->inst_true_bb->native_offset) & 0xffff); \
        } else { \
		int br_disp = ins->inst_true_bb->max_offset - offset;	\
		if (!ppc_is_imm16 (br_disp + 1024) || ! ppc_is_imm16 (ppc_is_imm16 (br_disp - 1024))) {	\
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));	\
			ovfj->bb = ins->inst_true_bb;	\
			ovfj->ip = NULL;	\
			ovfj->b0_cond = (b0);	\
			ovfj->b1_cond = (b1);	\
		        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB_OVF, ovfj); \
			ppc_b (code, 0);	\
		} else {	\
		        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
			ppc_bc (code, (b0), (b1), 0);	\
		}	\
        } \
}

#define EMIT_COND_BRANCH(ins,cond) EMIT_COND_BRANCH_FLAGS(ins, branch_b0_table [(cond)], branch_b1_table [(cond)])

/* emit an exception if condition is fail
 *
 * We assign the extra code used to throw the implicit exceptions
 * to cfg->bb_exit as far as the big branch handling is concerned
 */
#define EMIT_COND_SYSTEM_EXCEPTION_FLAGS(b0,b1,exc_name)            \
        do {                                                        \
		int br_disp = cfg->bb_exit->max_offset - offset;	\
		if (!ppc_is_imm16 (br_disp + 1024) || ! ppc_is_imm16 (ppc_is_imm16 (br_disp - 1024))) {	\
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));	\
			ovfj->bb = NULL;	\
			ovfj->ip = code;	\
			ovfj->b0_cond = (b0);	\
			ovfj->b1_cond = (b1);	\
			/* FIXME: test this code */	\
		        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj); \
			ppc_b (code, 0);	\
			cfg->bb_exit->max_offset += 24;	\
		} else {	\
			mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name);  \
			ppc_bc (code, (b0), (b1), 0);	\
		}	\
	} while (0); 

#define EMIT_COND_SYSTEM_EXCEPTION(cond,exc_name) EMIT_COND_SYSTEM_EXCEPTION_FLAGS(branch_b0_table [(cond)], branch_b1_table [(cond)], (exc_name))

static void
peephole_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *last_ins = NULL;
	ins = bb->code;

	while (ins) {

		switch (ins->opcode) {
		case OP_MUL_IMM: 
			/* remove unnecessary multiplication with 1 */
			if (ins->inst_imm == 1) {
				if (ins->dreg != ins->sreg1) {
					ins->opcode = OP_MOVE;
				} else {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				}
			} else {
				int power2 = mono_is_power_of_two (ins->inst_imm);
				if (power2 > 0) {
					ins->opcode = OP_SHL_IMM;
					ins->inst_imm = power2;
				}
			}
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
			/* 
			 * OP_STORE_MEMBASE_REG reg, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG 
					 || last_ins->opcode == OP_STORE_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}

			/* 
			 * Note: reg1 must be different from the basereg in the second load
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_LOAD_MEMBASE offset(basereg), reg1
			 * OP_MOVE reg1, reg2
			 */
			} if (last_ins && (last_ins->opcode == OP_LOADI4_MEMBASE
					   || last_ins->opcode == OP_LOAD_MEMBASE) &&
			      ins->inst_basereg != last_ins->dreg &&
			      ins->inst_basereg == last_ins->inst_basereg &&
			      ins->inst_offset == last_ins->inst_offset) {

				if (ins->dreg == last_ins->dreg) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->dreg;
				}

				//g_assert_not_reached ();

#if 0
			/* 
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg
			 * -->
			 * OP_STORE_MEMBASE_IMM imm, offset(basereg) 
			 * OP_ICONST reg, imm
			 */
			} else if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_IMM
						|| last_ins->opcode == OP_STORE_MEMBASE_IMM) &&
				   ins->inst_basereg == last_ins->inst_destbasereg &&
				   ins->inst_offset == last_ins->inst_offset) {
				//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
				ins->opcode = OP_ICONST;
				ins->inst_c0 = last_ins->inst_imm;
				g_assert_not_reached (); // check this rule
#endif
			}
			break;
		case OP_LOADU1_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}
			}
			break;
		case OP_LOADU2_MEMBASE:
		case OP_LOADI2_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					last_ins->next = ins->next;				
					ins = ins->next;				
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}
			}
			break;
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
		case OP_SETREG:
			ins->opcode = OP_MOVE;
			/* 
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				if (last_ins)
					last_ins->next = ins->next;				
				ins = ins->next;
				continue;
			}
			/* 
			 * OP_MOVE sreg, dreg 
			 * OP_MOVE dreg, sreg
			 */
			if (last_ins && last_ins->opcode == OP_MOVE &&
			    ins->sreg1 == last_ins->dreg &&
			    ins->dreg == last_ins->sreg1) {
				last_ins->next = ins->next;				
				ins = ins->next;				
				continue;
			}
			break;
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

/* 
 * the branch_b0_table should maintain the order of these
 * opcodes.
case CEE_BEQ:
case CEE_BGE:
case CEE_BGT:
case CEE_BLE:
case CEE_BLT:
case CEE_BNE_UN:
case CEE_BGE_UN:
case CEE_BGT_UN:
case CEE_BLE_UN:
case CEE_BLT_UN:
 */
static const guchar 
branch_b0_table [] = {
	PPC_BR_TRUE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	
	PPC_BR_FALSE, 
	PPC_BR_FALSE, 
	PPC_BR_TRUE, 
	PPC_BR_FALSE,
	PPC_BR_TRUE
};

static const guchar 
branch_b1_table [] = {
	PPC_BR_EQ, 
	PPC_BR_LT, 
	PPC_BR_GT, 
	PPC_BR_GT,
	PPC_BR_LT, 
	
	PPC_BR_EQ, 
	PPC_BR_LT, 
	PPC_BR_GT, 
	PPC_BR_GT,
	PPC_BR_LT 
};

/*
 * returns the offset used by spillvar. It allocates a new
 * spill variable if necessary. 
 */
static int
mono_spillvar_offset (MonoCompile *cfg, int spillvar)
{
	MonoSpillInfo **si, *info;
	int i = 0;

	si = &cfg->spill_info; 
	
	while (i <= spillvar) {

		if (!*si) {
			*si = info = mono_mempool_alloc (cfg->mempool, sizeof (MonoSpillInfo));
			info->next = NULL;
			info->offset = cfg->stack_offset;
			cfg->stack_offset += sizeof (gpointer);
		}

		if (i == spillvar)
			return (*si)->offset;

		i++;
		si = &(*si)->next;
	}

	g_assert_not_reached ();
	return 0;
}

static int
mono_spillvar_offset_float (MonoCompile *cfg, int spillvar)
{
	MonoSpillInfo **si, *info;
	int i = 0;

	si = &cfg->spill_info_float; 
	
	while (i <= spillvar) {

		if (!*si) {
			*si = info = mono_mempool_alloc (cfg->mempool, sizeof (MonoSpillInfo));
			info->next = NULL;
			cfg->stack_offset += 7;
			cfg->stack_offset &= ~7;
			info->offset = cfg->stack_offset;
			cfg->stack_offset += sizeof (double);
		}

		if (i == spillvar)
			return (*si)->offset;

		i++;
		si = &(*si)->next;
	}

	g_assert_not_reached ();
	return 0;
}

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a
//#define DEBUG(a)
/* use ppc_r3-ppc_10,ppc_r12 as temp registers, f1-f13 for FP registers */
#define PPC_CALLER_REGS ((0xff<<3) | (1<<12) | USE_EXTRA_TEMPS)
#define PPC_CALLER_FREGS (0x3ffe)

#define reg_is_freeable(r) (PPC_CALLER_REGS & 1 << (r))
#define freg_is_freeable(r) ((r) >= 1 && (r) <= 13)

typedef struct {
	int born_in;
	int killed_in;
	int last_use;
	int prev_use;
} RegTrack;

static const char*const * ins_spec = ppcg4;

static void
print_ins (int i, MonoInst *ins)
{
	const char *spec = ins_spec [ins->opcode];
	g_print ("\t%-2d %s", i, mono_inst_name (ins->opcode));
	if (spec [MONO_INST_DEST]) {
		if (ins->dreg >= MONO_MAX_IREGS)
			g_print (" R%d <-", ins->dreg);
		else
			g_print (" %s <-", mono_arch_regname (ins->dreg));
	}
	if (spec [MONO_INST_SRC1]) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			g_print (" R%d", ins->sreg1);
		else
			g_print (" %s", mono_arch_regname (ins->sreg1));
	}
	if (spec [MONO_INST_SRC2]) {
		if (ins->sreg2 >= MONO_MAX_IREGS)
			g_print (" R%d", ins->sreg2);
		else
			g_print (" %s", mono_arch_regname (ins->sreg2));
	}
	if (spec [MONO_INST_CLOB])
		g_print (" clobbers: %c", spec [MONO_INST_CLOB]);
	g_print ("\n");
}

static void
print_regtrack (RegTrack *t, int num)
{
	int i;
	char buf [32];
	const char *r;
	
	for (i = 0; i < num; ++i) {
		if (!t [i].born_in)
			continue;
		if (i >= MONO_MAX_IREGS) {
			g_snprintf (buf, sizeof(buf), "R%d", i);
			r = buf;
		} else
			r = mono_arch_regname (i);
		g_print ("liveness: %s [%d - %d]\n", r, t [i].born_in, t[i].last_use);
	}
}

typedef struct InstList InstList;

struct InstList {
	InstList *prev;
	InstList *next;
	MonoInst *data;
};

static inline InstList*
inst_list_prepend (MonoMemPool *pool, InstList *list, MonoInst *data)
{
	InstList *item = mono_mempool_alloc (pool, sizeof (InstList));
	item->data = data;
	item->prev = NULL;
	item->next = list;
	if (list)
		list->prev = item;
	return item;
}

/*
 * Force the spilling of the variable in the symbolic register 'reg'.
 */
static int
get_register_force_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, int reg)
{
	MonoInst *load;
	int i, sel, spill;
	
	sel = cfg->rs->iassign [reg];
	/*i = cfg->rs->isymbolic [sel];
	g_assert (i == reg);*/
	i = reg;
	spill = ++cfg->spill_count;
	cfg->rs->iassign [i] = -spill - 1;
	mono_regstate_free_int (cfg->rs, sel);
	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%sp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_int (cfg->rs, 1 << sel);
	g_assert (i == sel);

	return sel;
}

static int
get_register_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, guint32 regmask, int reg)
{
	MonoInst *load;
	int i, sel, spill;

	DEBUG (g_print ("start regmask to assign R%d: 0x%08x (R%d <- R%d R%d)\n", reg, regmask, ins->dreg, ins->sreg1, ins->sreg2));
	/* exclude the registers in the current instruction */
	if (reg != ins->sreg1 && (reg_is_freeable (ins->sreg1) || (ins->sreg1 >= MONO_MAX_IREGS && cfg->rs->iassign [ins->sreg1] >= 0))) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg1]);
		else
			regmask &= ~ (1 << ins->sreg1);
		DEBUG (g_print ("excluding sreg1 %s\n", mono_arch_regname (ins->sreg1)));
	}
	if (reg != ins->sreg2 && (reg_is_freeable (ins->sreg2) || (ins->sreg2 >= MONO_MAX_IREGS && cfg->rs->iassign [ins->sreg2] >= 0))) {
		if (ins->sreg2 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg2]);
		else
			regmask &= ~ (1 << ins->sreg2);
		DEBUG (g_print ("excluding sreg2 %s %d\n", mono_arch_regname (ins->sreg2), ins->sreg2));
	}
	if (reg != ins->dreg && reg_is_freeable (ins->dreg)) {
		regmask &= ~ (1 << ins->dreg);
		DEBUG (g_print ("excluding dreg %s\n", mono_arch_regname (ins->dreg)));
	}

	DEBUG (g_print ("available regmask: 0x%08x\n", regmask));
	g_assert (regmask); /* need at least a register we can free */
	sel = -1;
	/* we should track prev_use and spill the register that's farther */
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (regmask & (1 << i)) {
			sel = i;
			DEBUG (g_print ("selected register %s has assignment %d\n", mono_arch_regname (sel), cfg->rs->iassign [sel]));
			break;
		}
	}
	i = cfg->rs->isymbolic [sel];
	spill = ++cfg->spill_count;
	cfg->rs->iassign [i] = -spill - 1;
	mono_regstate_free_int (cfg->rs, sel);
	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, OP_LOAD_MEMBASE);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%sp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_int (cfg->rs, 1 << sel);
	g_assert (i == sel);
	
	return sel;
}

static int
get_float_register_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, guint32 regmask, int reg)
{
	MonoInst *load;
	int i, sel, spill;

	DEBUG (g_print ("start regmask to assign R%d: 0x%08x (R%d <- R%d R%d)\n", reg, regmask, ins->dreg, ins->sreg1, ins->sreg2));
	/* exclude the registers in the current instruction */
	if (reg != ins->sreg1 && (freg_is_freeable (ins->sreg1) || (ins->sreg1 >= MONO_MAX_FREGS && cfg->rs->fassign [ins->sreg1] >= 0))) {
		if (ins->sreg1 >= MONO_MAX_FREGS)
			regmask &= ~ (1 << cfg->rs->fassign [ins->sreg1]);
		else
			regmask &= ~ (1 << ins->sreg1);
		DEBUG (g_print ("excluding sreg1 %s\n", mono_arch_regname (ins->sreg1)));
	}
	if (reg != ins->sreg2 && (freg_is_freeable (ins->sreg2) || (ins->sreg2 >= MONO_MAX_FREGS && cfg->rs->fassign [ins->sreg2] >= 0))) {
		if (ins->sreg2 >= MONO_MAX_FREGS)
			regmask &= ~ (1 << cfg->rs->fassign [ins->sreg2]);
		else
			regmask &= ~ (1 << ins->sreg2);
		DEBUG (g_print ("excluding sreg2 %s %d\n", mono_arch_regname (ins->sreg2), ins->sreg2));
	}
	if (reg != ins->dreg && freg_is_freeable (ins->dreg)) {
		regmask &= ~ (1 << ins->dreg);
		DEBUG (g_print ("excluding dreg %s\n", mono_arch_regname (ins->dreg)));
	}

	DEBUG (g_print ("available regmask: 0x%08x\n", regmask));
	g_assert (regmask); /* need at least a register we can free */
	sel = -1;
	/* we should track prev_use and spill the register that's farther */
	for (i = 0; i < MONO_MAX_FREGS; ++i) {
		if (regmask & (1 << i)) {
			sel = i;
			DEBUG (g_print ("selected register %s has assignment %d\n", mono_arch_regname (sel), cfg->rs->fassign [sel]));
			break;
		}
	}
	i = cfg->rs->fsymbolic [sel];
	spill = ++cfg->spill_count;
	cfg->rs->fassign [i] = -spill - 1;
	mono_regstate_free_float(cfg->rs, sel);
	/* we need to create a spill var and insert a load to sel after the current instruction */
	MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
	load->dreg = sel;
	load->inst_basereg = cfg->frame_reg;
	load->inst_offset = mono_spillvar_offset_float (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("SPILLED LOAD FP (%d at 0x%08x(%%sp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_float (cfg->rs, 1 << sel);
	g_assert (i == sel);
	
	return sel;
}

static MonoInst*
create_copy_ins (MonoCompile *cfg, int dest, int src, MonoInst *ins)
{
	MonoInst *copy;
	MONO_INST_NEW (cfg, copy, OP_MOVE);
	copy->dreg = dest;
	copy->sreg1 = src;
	if (ins) {
		copy->next = ins->next;
		ins->next = copy;
	}
	DEBUG (g_print ("\tforced copy from %s to %s\n", mono_arch_regname (src), mono_arch_regname (dest)));
	return copy;
}

static MonoInst*
create_copy_ins_float (MonoCompile *cfg, int dest, int src, MonoInst *ins)
{
	MonoInst *copy;
	MONO_INST_NEW (cfg, copy, OP_FMOVE);
	copy->dreg = dest;
	copy->sreg1 = src;
	if (ins) {
		copy->next = ins->next;
		ins->next = copy;
	}
	DEBUG (g_print ("\tforced copy from %s to %s\n", mono_arch_regname (src), mono_arch_regname (dest)));
	return copy;
}

static MonoInst*
create_spilled_store (MonoCompile *cfg, int spill, int reg, int prev_reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORE_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = cfg->frame_reg;
	store->inst_offset = mono_spillvar_offset (cfg, spill);
	if (ins) {
		store->next = ins->next;
		ins->next = store;
	}
	DEBUG (g_print ("SPILLED STORE (%d at 0x%08x(%%sp)) R%d (from %s)\n", spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
	return store;
}

static MonoInst*
create_spilled_store_float (MonoCompile *cfg, int spill, int reg, int prev_reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORER8_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = cfg->frame_reg;
	store->inst_offset = mono_spillvar_offset_float (cfg, spill);
	if (ins) {
		store->next = ins->next;
		ins->next = store;
	}
	DEBUG (g_print ("SPILLED STORE FP (%d at 0x%08x(%%sp)) R%d (from %s)\n", spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
	return store;
}

static void
insert_before_ins (MonoInst *ins, InstList *item, MonoInst* to_insert)
{
	MonoInst *prev;
	g_assert (item->next);
	prev = item->next->data;

	while (prev->next != ins)
		prev = prev->next;
	to_insert->next = ins;
	prev->next = to_insert;
	/* 
	 * needed otherwise in the next instruction we can add an ins to the 
	 * end and that would get past this instruction.
	 */
	item->data = to_insert; 
}

static int
alloc_int_reg (MonoCompile *cfg, InstList *curinst, MonoInst *ins, int sym_reg, guint32 allow_mask)
{
	int val = cfg->rs->iassign [sym_reg];
	if (val < 0) {
		int spill = 0;
		if (val < -1) {
			/* the register gets spilled after this inst */
			spill = -val -1;
		}
		val = mono_regstate_alloc_int (cfg->rs, allow_mask);
		if (val < 0)
			val = get_register_spilling (cfg, curinst, ins, allow_mask, sym_reg);
		cfg->rs->iassign [sym_reg] = val;
		/* add option to store before the instruction for src registers */
		if (spill)
			create_spilled_store (cfg, spill, val, sym_reg, ins);
	}
	cfg->rs->isymbolic [val] = sym_reg;
	return val;
}

/*
 * Local register allocation.
 * We first scan the list of instructions and we save the liveness info of
 * each register (when the register is first used, when it's value is set etc.).
 * We also reverse the list of instructions (in the InstList list) because assigning
 * registers backwards allows for more tricks to be used.
 */
void
mono_arch_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoRegState *rs = cfg->rs;
	int i, val;
	RegTrack *reginfo, *reginfof;
	RegTrack *reginfo1, *reginfo2, *reginfod;
	InstList *tmp, *reversed = NULL;
	const char *spec;
	guint32 src1_mask, src2_mask, dest_mask;
	guint32 cur_iregs, cur_fregs;

	if (!bb->code)
		return;
	rs->next_vireg = bb->max_ireg;
	rs->next_vfreg = bb->max_freg;
	mono_regstate_assign (rs);
	reginfo = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vireg);
	reginfof = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vfreg);
	rs->ifree_mask = PPC_CALLER_REGS;
	rs->ffree_mask = PPC_CALLER_FREGS;

	ins = bb->code;
	i = 1;
	DEBUG (g_print ("LOCAL regalloc: basic block: %d\n", bb->block_num));
	/* forward pass on the instructions to collect register liveness info */
	while (ins) {
		spec = ins_spec [ins->opcode];
		DEBUG (print_ins (i, ins));
		/*if (spec [MONO_INST_CLOB] == 'c') {
			MonoCallInst * call = (MonoCallInst*)ins;
			int j;
		}*/
		if (spec [MONO_INST_SRC1]) {
			if (spec [MONO_INST_SRC1] == 'f')
				reginfo1 = reginfof;
			else
				reginfo1 = reginfo;
			reginfo1 [ins->sreg1].prev_use = reginfo1 [ins->sreg1].last_use;
			reginfo1 [ins->sreg1].last_use = i;
		} else {
			ins->sreg1 = -1;
		}
		if (spec [MONO_INST_SRC2]) {
			if (spec [MONO_INST_SRC2] == 'f')
				reginfo2 = reginfof;
			else
				reginfo2 = reginfo;
			reginfo2 [ins->sreg2].prev_use = reginfo2 [ins->sreg2].last_use;
			reginfo2 [ins->sreg2].last_use = i;
		} else {
			ins->sreg2 = -1;
		}
		if (spec [MONO_INST_DEST]) {
			if (spec [MONO_INST_DEST] == 'f')
				reginfod = reginfof;
			else
				reginfod = reginfo;
			if (spec [MONO_INST_DEST] != 'b') /* it's not just a base register */
				reginfod [ins->dreg].killed_in = i;
			reginfod [ins->dreg].prev_use = reginfod [ins->dreg].last_use;
			reginfod [ins->dreg].last_use = i;
			if (reginfod [ins->dreg].born_in == 0 || reginfod [ins->dreg].born_in > i)
				reginfod [ins->dreg].born_in = i;
			if (spec [MONO_INST_DEST] == 'l') {
				/* result in eax:edx, the virtual register is allocated sequentially */
				reginfod [ins->dreg + 1].prev_use = reginfod [ins->dreg + 1].last_use;
				reginfod [ins->dreg + 1].last_use = i;
				if (reginfod [ins->dreg + 1].born_in == 0 || reginfod [ins->dreg + 1].born_in > i)
					reginfod [ins->dreg + 1].born_in = i;
			}
		} else {
			ins->dreg = -1;
		}
		reversed = inst_list_prepend (cfg->mempool, reversed, ins);
		++i;
		ins = ins->next;
	}

	cur_iregs = PPC_CALLER_REGS;
	cur_fregs = PPC_CALLER_FREGS;

	DEBUG (print_regtrack (reginfo, rs->next_vireg));
	DEBUG (print_regtrack (reginfof, rs->next_vfreg));
	tmp = reversed;
	while (tmp) {
		int prev_dreg, prev_sreg1, prev_sreg2;
		--i;
		ins = tmp->data;
		spec = ins_spec [ins->opcode];
		DEBUG (g_print ("processing:"));
		DEBUG (print_ins (i, ins));
		/* make the register available for allocation: FIXME add fp reg */
		if (ins->opcode == OP_SETREG || ins->opcode == OP_SETREGIMM) {
			cur_iregs |= 1 << ins->dreg;
			DEBUG (g_print ("adding %d to cur_iregs\n", ins->dreg));
		} else if (ins->opcode == OP_SETFREG) {
			cur_fregs |= 1 << ins->dreg;
			DEBUG (g_print ("adding %d to cur_fregs\n", ins->dreg));
		} else if (spec [MONO_INST_CLOB] == 'c') {
			MonoCallInst *cinst = (MonoCallInst*)ins;
			DEBUG (g_print ("excluding regs 0x%x from cur_iregs (0x%x)\n", cinst->used_iregs, cur_iregs));
			DEBUG (g_print ("excluding fpregs 0x%x from cur_fregs (0x%x)\n", cinst->used_fregs, cur_fregs));
			cur_iregs &= ~cinst->used_iregs;
			cur_fregs &= ~cinst->used_fregs;
			DEBUG (g_print ("available cur_iregs: 0x%x\n", cur_iregs));
			DEBUG (g_print ("available cur_fregs: 0x%x\n", cur_fregs));
			/* registers used by the calling convention are excluded from 
			 * allocation: they will be selectively enabled when they are 
			 * assigned by the special SETREG opcodes.
			 */
		}
		dest_mask = src1_mask = src2_mask = cur_iregs;
		/* update for use with FP regs... */
		if (spec [MONO_INST_DEST] == 'f') {
			dest_mask = cur_fregs;
			if (ins->dreg >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->dreg];
				prev_dreg = ins->dreg;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_float (rs, dest_mask);
					if (val < 0)
						val = get_float_register_spilling (cfg, tmp, ins, dest_mask, ins->dreg);
					rs->fassign [ins->dreg] = val;
					if (spill)
						create_spilled_store_float (cfg, spill, val, prev_dreg, ins);
				}
				DEBUG (g_print ("\tassigned dreg %s to dest R%d\n", mono_arch_regname (val), ins->dreg));
				rs->fsymbolic [val] = prev_dreg;
				ins->dreg = val;
				if (spec [MONO_INST_CLOB] == 'c' && ins->dreg != ppc_f1) {
					/* this instruction only outputs to ppc_f1, need to copy */
					create_copy_ins_float (cfg, ins->dreg, ppc_f1, ins);
				}
			} else {
				prev_dreg = -1;
			}
			if (freg_is_freeable (ins->dreg) && prev_dreg >= 0 && (reginfof [prev_dreg].born_in >= i || !(cur_fregs & (1 << ins->dreg)))) {
				DEBUG (g_print ("\tfreeable float %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfof [prev_dreg].born_in));
				mono_regstate_free_float (rs, ins->dreg);
			}
		} else if (ins->dreg >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->dreg];
			prev_dreg = ins->dreg;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, dest_mask);
				if (val < 0)
					val = get_register_spilling (cfg, tmp, ins, dest_mask, ins->dreg);
				rs->iassign [ins->dreg] = val;
				if (spill)
					create_spilled_store (cfg, spill, val, prev_dreg, ins);
			}
			DEBUG (g_print ("\tassigned dreg %s to dest R%d\n", mono_arch_regname (val), ins->dreg));
			rs->isymbolic [val] = prev_dreg;
			ins->dreg = val;
			if (spec [MONO_INST_DEST] == 'l') {
				int hreg = prev_dreg + 1;
				val = rs->iassign [hreg];
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_int (rs, dest_mask);
					if (val < 0)
						val = get_register_spilling (cfg, tmp, ins, dest_mask, hreg);
					rs->iassign [hreg] = val;
					if (spill)
						create_spilled_store (cfg, spill, val, hreg, ins);
				}
				DEBUG (g_print ("\tassigned hreg %s to dest R%d\n", mono_arch_regname (val), hreg));
				rs->isymbolic [val] = hreg;
				/* FIXME:? ins->dreg = val; */
				if (ins->dreg == ppc_r4) {
					if (val != ppc_r3)
						create_copy_ins (cfg, val, ppc_r3, ins);
				} else if (ins->dreg == ppc_r3) {
					if (val == ppc_r4) {
						/* swap */
						create_copy_ins (cfg, ppc_r4, ppc_r0, ins);
						create_copy_ins (cfg, ppc_r3, ppc_r4, ins);
						create_copy_ins (cfg, ppc_r0, ppc_r3, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, ins->dreg, ppc_r4, ins);
						create_copy_ins (cfg, val, ppc_r3, ins);
					}
				} else {
					if (val == ppc_r3) {
						create_copy_ins (cfg, ins->dreg, ppc_r4, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, val, ppc_r3, ins);
						create_copy_ins (cfg, ins->dreg, ppc_r4, ins);
					}
				}
				if (reg_is_freeable (val) && hreg >= 0 && (reginfo [hreg].born_in >= i && !(cur_iregs & (1 << val)))) {
					DEBUG (g_print ("\tfreeable %s (R%d)\n", mono_arch_regname (val), hreg));
					mono_regstate_free_int (rs, val);
				}
			} else if (spec [MONO_INST_DEST] == 'a' && ins->dreg != ppc_r3 && spec [MONO_INST_CLOB] != 'd') {
				/* this instruction only outputs to ppc_r3, need to copy */
				create_copy_ins (cfg, ins->dreg, ppc_r3, ins);
			}
		} else {
			prev_dreg = -1;
		}
		if (spec [MONO_INST_DEST] == 'f' && freg_is_freeable (ins->dreg) && prev_dreg >= 0 && (reginfof [prev_dreg].born_in >= i)) {
			DEBUG (g_print ("\tfreeable float %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfof [prev_dreg].born_in));
			mono_regstate_free_float (rs, ins->dreg);
		} else if (spec [MONO_INST_DEST] != 'f' && reg_is_freeable (ins->dreg) && prev_dreg >= 0 && (reginfo [prev_dreg].born_in >= i)) {
			DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
			mono_regstate_free_int (rs, ins->dreg);
		}
		if (spec [MONO_INST_SRC1] == 'f') {
			src1_mask = cur_fregs;
			if (ins->sreg1 >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->sreg1];
				prev_sreg1 = ins->sreg1;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					//g_assert (val == -1); /* source cannot be spilled */
					val = mono_regstate_alloc_float (rs, src1_mask);
					if (val < 0)
						val = get_float_register_spilling (cfg, tmp, ins, src1_mask, ins->sreg1);
					rs->fassign [ins->sreg1] = val;
					DEBUG (g_print ("\tassigned sreg1 %s to R%d\n", mono_arch_regname (val), ins->sreg1));
					if (spill) {
						MonoInst *store = create_spilled_store_float (cfg, spill, val, prev_sreg1, NULL);
						insert_before_ins (ins, tmp, store);
					}
				}
				rs->fsymbolic [val] = prev_sreg1;
				ins->sreg1 = val;
			} else {
				prev_sreg1 = -1;
			}
		} else if (ins->sreg1 >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->sreg1];
			prev_sreg1 = ins->sreg1;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				if (0 && ins->opcode == OP_MOVE) {
					/* 
					 * small optimization: the dest register is already allocated
					 * but the src one is not: we can simply assign the same register
					 * here and peephole will get rid of the instruction later.
					 * This optimization may interfere with the clobbering handling:
					 * it removes a mov operation that will be added again to handle clobbering.
					 * There are also some other issues that should with make testjit.
					 */
					mono_regstate_alloc_int (rs, 1 << ins->dreg);
					val = rs->iassign [ins->sreg1] = ins->dreg;
					//g_assert (val >= 0);
					DEBUG (g_print ("\tfast assigned sreg1 %s to R%d\n", mono_arch_regname (val), ins->sreg1));
				} else {
					//g_assert (val == -1); /* source cannot be spilled */
					val = mono_regstate_alloc_int (rs, src1_mask);
					if (val < 0)
						val = get_register_spilling (cfg, tmp, ins, src1_mask, ins->sreg1);
					rs->iassign [ins->sreg1] = val;
					DEBUG (g_print ("\tassigned sreg1 %s to R%d\n", mono_arch_regname (val), ins->sreg1));
				}
				if (spill) {
					MonoInst *store = create_spilled_store (cfg, spill, val, prev_sreg1, NULL);
					insert_before_ins (ins, tmp, store);
				}
			}
			rs->isymbolic [val] = prev_sreg1;
			ins->sreg1 = val;
		} else {
			prev_sreg1 = -1;
		}
		if (spec [MONO_INST_SRC2] == 'f') {
			src2_mask = cur_fregs;
			if (ins->sreg2 >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->sreg2];
				prev_sreg2 = ins->sreg2;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_regstate_alloc_float (rs, src2_mask);
					if (val < 0)
						val = get_float_register_spilling (cfg, tmp, ins, src2_mask, ins->sreg2);
					rs->fassign [ins->sreg2] = val;
					DEBUG (g_print ("\tassigned sreg2 %s to R%d\n", mono_arch_regname (val), ins->sreg2));
					if (spill)
						create_spilled_store_float (cfg, spill, val, prev_sreg2, ins);
				}
				rs->fsymbolic [val] = prev_sreg2;
				ins->sreg2 = val;
			} else {
				prev_sreg2 = -1;
			}
		} else if (ins->sreg2 >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->sreg2];
			prev_sreg2 = ins->sreg2;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_regstate_alloc_int (rs, src2_mask);
				if (val < 0)
					val = get_register_spilling (cfg, tmp, ins, src2_mask, ins->sreg2);
				rs->iassign [ins->sreg2] = val;
				DEBUG (g_print ("\tassigned sreg2 %s to R%d\n", mono_arch_regname (val), ins->sreg2));
				if (spill)
					create_spilled_store (cfg, spill, val, prev_sreg2, ins);
			}
			rs->isymbolic [val] = prev_sreg2;
			ins->sreg2 = val;
		} else {
			prev_sreg2 = -1;
		}

		if (spec [MONO_INST_CLOB] == 'c') {
			int j, s;
			guint32 clob_mask = PPC_CALLER_REGS;
			for (j = 0; j < MONO_MAX_IREGS; ++j) {
				s = 1 << j;
				if ((clob_mask & s) && !(rs->ifree_mask & s) && j != ins->sreg1) {
					//g_warning ("register %s busy at call site\n", mono_arch_regname (j));
				}
			}
		}
		/*if (reg_is_freeable (ins->sreg1) && prev_sreg1 >= 0 && reginfo [prev_sreg1].born_in >= i) {
			DEBUG (g_print ("freeable %s\n", mono_arch_regname (ins->sreg1)));
			mono_regstate_free_int (rs, ins->sreg1);
		}
		if (reg_is_freeable (ins->sreg2) && prev_sreg2 >= 0 && reginfo [prev_sreg2].born_in >= i) {
			DEBUG (g_print ("freeable %s\n", mono_arch_regname (ins->sreg2)));
			mono_regstate_free_int (rs, ins->sreg2);
		}*/
		
		//DEBUG (print_ins (i, ins));
		tmp = tmp->next;
	}
	cfg->max_ireg = MAX (cfg->max_ireg, rs->max_ireg);
}

static guchar*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg. ppc_f0 is used a scratch */
	ppc_fctiwz (code, ppc_f0, sreg);
	ppc_stfd (code, ppc_f0, -8, ppc_sp);
	ppc_lwz (code, dreg, -4, ppc_sp);
	if (!is_signed) {
		if (size == 1)
			ppc_andid (code, dreg, dreg, 0xff);
		else if (size == 2)
			ppc_andid (code, dreg, dreg, 0xffff);
	} else {
		if (size == 1)
			ppc_extsb (code, dreg, dreg);
		else if (size == 2)
			ppc_extsh (code, dreg, dreg);
	}
	return code;
}

static unsigned char*
mono_emit_stack_alloc (guchar *code, MonoInst* tree)
{
#if 0
	int sreg = tree->sreg1;
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, tree->sreg1);
	if (tree->flags & MONO_INST_INIT) {
		int offset = 0;
		if (tree->dreg != X86_EAX && sreg != X86_EAX) {
			x86_push_reg (code, X86_EAX);
			offset += 4;
		}
		if (tree->dreg != X86_ECX && sreg != X86_ECX) {
			x86_push_reg (code, X86_ECX);
			offset += 4;
		}
		if (tree->dreg != X86_EDI && sreg != X86_EDI) {
			x86_push_reg (code, X86_EDI);
			offset += 4;
		}
		
		x86_shift_reg_imm (code, X86_SHR, sreg, 2);
		if (sreg != X86_ECX)
			x86_mov_reg_reg (code, X86_ECX, sreg, 4);
		x86_alu_reg_reg (code, X86_XOR, X86_EAX, X86_EAX);
				
		x86_lea_membase (code, X86_EDI, X86_ESP, offset);
		x86_cld (code);
		x86_prefix (code, X86_REP_PREFIX);
		x86_stosl (code);
		
		if (tree->dreg != X86_EDI && sreg != X86_EDI)
			x86_pop_reg (code, X86_EDI);
		if (tree->dreg != X86_ECX && sreg != X86_ECX)
			x86_pop_reg (code, X86_ECX);
		if (tree->dreg != X86_EAX && sreg != X86_EAX)
			x86_pop_reg (code, X86_EAX);
	}
#endif
	return code;
}

typedef struct {
	guchar *code;
	guchar *target;
	int absolute;
	int found;
} PatchData;

#define is_call_imm(diff) ((gint)(diff) >= -33554432 && (gint)(diff) <= 33554431)

static int
search_thunk_slot (void *data, int csize, int bsize, void *user_data) {
	PatchData *pdata = (PatchData*)user_data;
	guchar *code = data;
	guint32 *thunks = data;
	guint32 *endthunks = (guint32*)(code + bsize);
	guint32 load [2];
	guchar *templ;
	int i, count = 0;
	int difflow, diffhigh;

	/* always ensure a call from pdata->code can reach to the thunks without further thunks */
	difflow = (char*)pdata->code - (char*)thunks;
	diffhigh = (char*)pdata->code - (char*)endthunks;
	if (!((is_call_imm (thunks) && is_call_imm (endthunks)) || (is_call_imm (difflow) && is_call_imm (diffhigh))))
		return 0;

	templ = (guchar*)load;
	ppc_lis (templ, ppc_r0, (guint32)(pdata->target) >> 16);
	ppc_ori (templ, ppc_r0, ppc_r0, (guint32)(pdata->target) & 0xffff);

	//g_print ("thunk nentries: %d\n", ((char*)endthunks - (char*)thunks)/16);
	if ((pdata->found == 2) || (pdata->code >= code && pdata->code <= code + csize)) {
		while (thunks < endthunks) {
			//g_print ("looking for target: %p at %p (%08x-%08x)\n", pdata->target, thunks, thunks [0], thunks [1]);
			if ((thunks [0] == load [0]) && (thunks [1] == load [1])) {
				ppc_patch (pdata->code, (guchar*)thunks);
				mono_arch_flush_icache (pdata->code, 4);
				pdata->found = 1;
				return 1;
			} else if ((thunks [0] == 0) && (thunks [1] == 0)) {
				/* found a free slot instead: emit thunk */
				code = (guchar*)thunks;
				ppc_lis (code, ppc_r0, (guint32)(pdata->target) >> 16);
				ppc_ori (code, ppc_r0, ppc_r0, (guint32)(pdata->target) & 0xffff);
				ppc_mtctr (code, ppc_r0);
				ppc_bcctr (code, PPC_BR_ALWAYS, 0);
				mono_arch_flush_icache ((guchar*)thunks, 16);

				ppc_patch (pdata->code, (guchar*)thunks);
				mono_arch_flush_icache (pdata->code, 4);
				pdata->found = 1;
				return 1;
			}
			/* skip 16 bytes, the size of the thunk */
			thunks += 4;
			count++;
		}
		//g_print ("failed thunk lookup for %p from %p at %p (%d entries)\n", pdata->target, pdata->code, data, count);
	}
	return 0;
}

static void
handle_thunk (int absolute, guchar *code, guchar *target) {
	MonoDomain *domain = mono_domain_get ();
	PatchData pdata;

	pdata.code = code;
	pdata.target = target;
	pdata.absolute = absolute;
	pdata.found = 0;

	mono_domain_lock (domain);
	mono_code_manager_foreach (domain->code_mp, search_thunk_slot, &pdata);

	if (!pdata.found) {
		/* this uses the first available slot */
		pdata.found = 2;
		mono_code_manager_foreach (domain->code_mp, search_thunk_slot, &pdata);
	}
	mono_domain_unlock (domain);

	if (pdata.found != 1)
		g_print ("thunk failed for %p from %p\n", target, code);
	g_assert (pdata.found == 1);
}

void
ppc_patch (guchar *code, guchar *target)
{
	guint32 ins = *(guint32*)code;
	guint32 prim = ins >> 26;
	guint32 ovf;

	//g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);
	if (prim == 18) {
		// prefer relative branches, they are more position independent (e.g. for AOT compilation).
		gint diff = target - code;
		if (diff >= 0){
			if (diff <= 33554431){
				ins = (18 << 26) | (diff) | (ins & 1);
				*(guint32*)code = ins;
				return;
			}
		} else {
			/* diff between 0 and -33554432 */
			if (diff >= -33554432){
				ins = (18 << 26) | (diff & ~0xfc000000) | (ins & 1);
				*(guint32*)code = ins;
				return;
			}
		}
		
		if ((glong)target >= 0){
			if ((glong)target <= 33554431){
				ins = (18 << 26) | ((guint32) target) | (ins & 1) | 2;
				*(guint32*)code = ins;
				return;
			}
		} else {
			if ((glong)target >= -33554432){
				ins = (18 << 26) | (((guint32)target) & ~0xfc000000) | (ins & 1) | 2;
				*(guint32*)code = ins;
				return;
			}
		}

		handle_thunk (TRUE, code, target);
		return;

		g_assert_not_reached ();
	}
	
	
	if (prim == 16) {
		// absolute address
		if (ins & 2) {
			guint32 li = (guint32)target;
			ins = (ins & 0xffff0000) | (ins & 3);
			ovf  = li & 0xffff0000;
			if (ovf != 0 && ovf != 0xffff0000)
				g_assert_not_reached ();
			li &= 0xffff;
			ins |= li;
			// FIXME: assert the top bits of li are 0
		} else {
			gint diff = target - code;
			ins = (ins & 0xffff0000) | (ins & 3);
			ovf  = diff & 0xffff0000;
			if (ovf != 0 && ovf != 0xffff0000)
				g_assert_not_reached ();
			diff &= 0xffff;
			ins |= diff;
		}
		*(guint32*)code = ins;
	} else {
		g_assert_not_reached ();
	}
//	g_print ("patched with 0x%08x\n", ins);
}

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint offset;
	guint8 *code = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	guint last_offset = 0;
	int max_len, cpos;

	if (cfg->opt & MONO_OPT_PEEPHOLE)
		peephole_pass (cfg, bb);

	/* we don't align basic blocks of loops on ppc */

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		//MonoCoverageInfo *cov = mono_get_coverage_info (cfg->method);
		//g_assert (!mono_compile_aot);
		//cpos += 6;
		//if (bb->cil_code)
		//	cov->data [bb->dfn].iloffset = bb->cil_code - cfg->cil_code;
		/* this is not thread save, but good enough */
		/* fixme: howto handle overflows? */
		//x86_inc_mem (code, &cov->data [bb->dfn].count); 
	}

	ins = bb->code;
	while (ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
		}
	//	if (ins->cil_code)
	//		g_print ("cil code\n");
		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_BIGMUL:
			ppc_mullw (code, ppc_r4, ins->sreg1, ins->sreg2);
			ppc_mulhw (code, ppc_r3, ins->sreg1, ins->sreg2);
			break;
		case OP_BIGMUL_UN:
			ppc_mullw (code, ppc_r4, ins->sreg1, ins->sreg2);
			ppc_mulhwu (code, ppc_r3, ins->sreg1, ins->sreg2);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			ppc_li (code, ppc_r0, ins->inst_imm);
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stb (code, ppc_r0, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_stbx (code, ppc_r0, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI2_MEMBASE_IMM:
			ppc_li (code, ppc_r0, ins->inst_imm);
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_sth (code, ppc_r0, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_sthx (code, ppc_r0, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			ppc_load (code, ppc_r0, ins->inst_imm);
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stw (code, ppc_r0, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_stwx (code, ppc_r0, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI1_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stb (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_stbx (code, ins->sreg1, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_STOREI2_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_sth (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_sthx (code, ins->sreg1, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stw (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_stwx (code, ins->sreg1, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
			g_assert_not_reached ();
			//x86_mov_reg_mem (code, ins->dreg, ins->inst_p0, 4);
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			//x86_mov_reg_imm (code, ins->dreg, ins->inst_p0);
			//x86_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 4);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lwz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lwzx (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			break;
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lbz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lbzx (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			if (ins->opcode == OP_LOADI1_MEMBASE)
				ppc_extsb (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADU2_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lhz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lhzx (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			break;
		case OP_LOADI2_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lha (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lhax (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			break;
		case CEE_CONV_I1:
			ppc_extsb (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_I2:
			ppc_extsh (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_U1:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, 0, 24, 31);
			break;
		case CEE_CONV_U2:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, 0, 16, 31);
			break;
		case OP_COMPARE:
			if (ins->next && 
					((ins->next->opcode >= CEE_BNE_UN && ins->next->opcode <= CEE_BLT_UN) ||
					(ins->next->opcode >= OP_COND_EXC_NE_UN && ins->next->opcode <= OP_COND_EXC_LT_UN) ||
					(ins->next->opcode == OP_CLT_UN || ins->next->opcode == OP_CGT_UN)))
				ppc_cmpl (code, 0, 0, ins->sreg1, ins->sreg2);
			else
				ppc_cmp (code, 0, 0, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
			if (ins->next && 
					((ins->next->opcode >= CEE_BNE_UN && ins->next->opcode <= CEE_BLT_UN) ||
					(ins->next->opcode >= OP_COND_EXC_NE_UN && ins->next->opcode <= OP_COND_EXC_LT_UN) ||
					(ins->next->opcode == OP_CLT_UN || ins->next->opcode == OP_CGT_UN))) {
				if (ppc_is_uimm16 (ins->inst_imm)) {
					ppc_cmpli (code, 0, 0, ins->sreg1, (ins->inst_imm & 0xffff));
				} else {
					ppc_load (code, ppc_r11, ins->inst_imm);
					ppc_cmpl (code, 0, 0, ins->sreg1, ppc_r11);
				}
			} else {
				if (ppc_is_imm16 (ins->inst_imm)) {
					ppc_cmpi (code, 0, 0, ins->sreg1, (ins->inst_imm & 0xffff));
				} else {
					ppc_load (code, ppc_r11, ins->inst_imm);
					ppc_cmp (code, 0, 0, ins->sreg1, ppc_r11);
				}
			}
			break;
		case OP_X86_TEST_NULL:
			ppc_cmpi (code, 0, 0, ins->sreg1, 0);
			break;
		case CEE_BREAK:
			ppc_break (code);
			break;
		case OP_ADDCC:
			ppc_addc (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_ADD:
			ppc_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
			ppc_adde (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADDCC_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
				ppc_addic (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_addc (code, ins->dreg, ins->sreg1, ppc_r11);
			}
			break;
		case OP_ADD_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
				ppc_addi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_add (code, ins->dreg, ins->sreg1, ppc_r11);
			}
			break;
		case OP_ADC_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_adde (code, ins->dreg, ins->sreg1, ppc_r11);
			break;
		case CEE_ADD_OVF:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case CEE_ADD_OVF_UN:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addco (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case CEE_SUB_OVF:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfo (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case CEE_SUB_OVF_UN:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfc (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addeo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_UN_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_addeo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfeo (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_UN_CARRY:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			ppc_subfeo (code, ins->dreg, ins->sreg2, ins->sreg1);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<13));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUBCC:
			ppc_subfc (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SUBCC_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_subfc (code, ins->dreg, ppc_r11, ins->sreg1);
			break;
		case CEE_SUB:
			ppc_subf (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SBB:
			ppc_subfe (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_SUB_IMM:
			// we add the negated value
			if (ppc_is_imm16 (-ins->inst_imm))
				ppc_addi (code, ins->dreg, ins->sreg1, -ins->inst_imm);
			else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_sub (code, ins->dreg, ins->sreg1, ppc_r11);
			}
			break;
		case OP_SBB_IMM:
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_subfe (code, ins->dreg, ppc_r11, ins->sreg1);
			break;
		case OP_PPC_SUBFIC:
			g_assert (ppc_is_imm16 (ins->inst_imm));
			ppc_subfic (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_PPC_SUBFZE:
			ppc_subfze (code, ins->dreg, ins->sreg1);
			break;
		case CEE_AND:
			/* FIXME: the ppc macros as inconsistent here: put dest as the first arg! */
			ppc_and (code, ins->sreg1, ins->dreg, ins->sreg2);
			break;
		case OP_AND_IMM:
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_andid (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_andisd (code, ins->sreg1, ins->dreg, ((guint32)ins->inst_imm >> 16));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_and (code, ins->sreg1, ins->dreg, ppc_r11);
			}
			break;
		case CEE_DIV: {
			guint32 *divisor_is_m1;
                         /* XER format: SO, OV, CA, reserved [21 bits], count [8 bits]
                         */
			ppc_cmpi (code, 0, 0, ins->sreg2, -1);
			divisor_is_m1 = code;
			ppc_bc (code, PPC_BR_FALSE | PPC_BR_LIKELY, PPC_BR_EQ, 0);
			ppc_lis (code, ppc_r11, 0x8000);
			ppc_cmp (code, 0, 0, ins->sreg1, ppc_r11);
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "ArithmeticException");
			ppc_patch (divisor_is_m1, code);
			 /* XER format: SO, OV, CA, reserved [21 bits], count [8 bits]
			 */
			ppc_divwod (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			break;
		}
		case CEE_DIV_UN:
			ppc_divwuod (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			break;
		case OP_DIV_IMM:
			g_assert_not_reached ();
#if 0
			ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_divwod (code, ins->dreg, ins->sreg1, ppc_r11);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			/* FIXME: use OverflowException for 0x80000000/-1 */
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			break;
#endif
		case CEE_REM: {
			guint32 *divisor_is_m1;
			ppc_cmpi (code, 0, 0, ins->sreg2, -1);
			divisor_is_m1 = code;
			ppc_bc (code, PPC_BR_FALSE | PPC_BR_LIKELY, PPC_BR_EQ, 0);
			ppc_lis (code, ppc_r11, 0x8000);
			ppc_cmp (code, 0, 0, ins->sreg1, ppc_r11);
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "ArithmeticException");
			ppc_patch (divisor_is_m1, code);
			ppc_divwod (code, ppc_r11, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			/* FIXME: use OverflowException for 0x80000000/-1 */
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			ppc_mullw (code, ppc_r11, ppc_r11, ins->sreg2);
			ppc_subf (code, ins->dreg, ppc_r11, ins->sreg1);
			break;
		}
		case CEE_REM_UN:
			ppc_divwuod (code, ppc_r11, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "DivideByZeroException");
			ppc_mullw (code, ppc_r11, ppc_r11, ins->sreg2);
			ppc_subf (code, ins->dreg, ppc_r11, ins->sreg1);
			break;
		case OP_REM_IMM:
			g_assert_not_reached ();
		case CEE_OR:
			ppc_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_ori (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_oris (code, ins->sreg1, ins->dreg, ((guint32)(ins->inst_imm) >> 16));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_or (code, ins->sreg1, ins->dreg, ppc_r11);
			}
			break;
		case CEE_XOR:
			ppc_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
			if (!(ins->inst_imm & 0xffff0000)) {
				ppc_xori (code, ins->sreg1, ins->dreg, ins->inst_imm);
			} else if (!(ins->inst_imm & 0xffff)) {
				ppc_xoris (code, ins->sreg1, ins->dreg, ((guint32)(ins->inst_imm) >> 16));
			} else {
				ppc_load (code, ppc_r11, ins->inst_imm);
				ppc_xor (code, ins->sreg1, ins->dreg, ppc_r11);
			}
			break;
		case CEE_SHL:
			ppc_slw (code, ins->sreg1, ins->dreg, ins->sreg2);
			break;
		case OP_SHL_IMM:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f), 0, (31 - (ins->inst_imm & 0x1f)));
			//ppc_load (code, ppc_r11, ins->inst_imm);
			//ppc_slw (code, ins->sreg1, ins->dreg, ppc_r11);
			break;
		case CEE_SHR:
			ppc_sraw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHR_IMM:
			// there is also ppc_srawi
			//ppc_load (code, ppc_r11, ins->inst_imm);
			//ppc_sraw (code, ins->dreg, ins->sreg1, ppc_r11);
			ppc_srawi (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			break;
		case OP_SHR_UN_IMM:
			/*ppc_load (code, ppc_r11, ins->inst_imm);
			ppc_srw (code, ins->dreg, ins->sreg1, ppc_r11);*/
			ppc_rlwinm (code, ins->dreg, ins->sreg1, (32 - (ins->inst_imm & 0x1f)), (ins->inst_imm & 0x1f), 31);
			break;
		case CEE_SHR_UN:
			ppc_srw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_NOT:
			ppc_not (code, ins->dreg, ins->sreg1);
			break;
		case CEE_NEG:
			ppc_neg (code, ins->dreg, ins->sreg1);
			break;
		case CEE_MUL:
			ppc_mullw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_MUL_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
			    ppc_mulli (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
			    ppc_load (code, ppc_r11, ins->inst_imm);
			    ppc_mullw (code, ins->dreg, ins->sreg1, ppc_r11);
			}
			break;
		case CEE_MUL_OVF:
			/* we annot use mcrxr, since it's not implemented on some processors 
			 * XER format: SO, OV, CA, reserved [21 bits], count [8 bits]
			 */
			ppc_mullwo (code, ins->dreg, ins->sreg1, ins->sreg2);
			ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case CEE_MUL_OVF_UN:
			/* we first multiply to get the high word and compare to 0
			 * to set the flags, then the result is discarded and then 
			 * we multiply to get the lower * bits result
			 */
			ppc_mulhwu (code, ppc_r0, ins->sreg1, ins->sreg2);
			ppc_cmpi (code, 0, 0, ppc_r0, 0);
			EMIT_COND_SYSTEM_EXCEPTION (CEE_BNE_UN - CEE_BEQ, "OverflowException");
			ppc_mullw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ICONST:
		case OP_SETREGIMM:
			ppc_load (code, ins->dreg, ins->inst_c0);
			break;
		case OP_AOTCONST:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			ppc_lis (code, ins->dreg, 0);
			ppc_ori (code, ins->dreg, ins->dreg, 0);
			break;
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
		case OP_SETREG:
			ppc_mr (code, ins->dreg, ins->sreg1);
			break;
		case OP_SETLRET: {
			int saved = ins->sreg1;
			if (ins->sreg1 == ppc_r3) {
				ppc_mr (code, ppc_r0, ins->sreg1);
				saved = ppc_r0;
			}
			if (ins->sreg2 != ppc_r3)
				ppc_mr (code, ppc_r3, ins->sreg2);
			if (saved != ppc_r4)
				ppc_mr (code, ppc_r4, saved);
			break;
		}
		case OP_SETFREG:
		case OP_FMOVE:
			ppc_fmr (code, ins->dreg, ins->sreg1);
			break;
		case OP_FCONV_TO_R4:
			ppc_frsp (code, ins->dreg, ins->sreg1);
			break;
		case CEE_JMP: {
			int i, pos = 0;
			
			/*
			 * Keep in sync with mono_arch_emit_epilog
			 */
			g_assert (!cfg->method->save_lmf);
			if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
				if (ppc_is_imm16 (cfg->stack_usage + PPC_RET_ADDR_OFFSET)) {
					ppc_lwz (code, ppc_r0, cfg->stack_usage + PPC_RET_ADDR_OFFSET, cfg->frame_reg);
				} else {
					ppc_load (code, ppc_r11, cfg->stack_usage + PPC_RET_ADDR_OFFSET);
					ppc_lwzx (code, ppc_r0, cfg->frame_reg, ppc_r11);
				}
				ppc_mtlr (code, ppc_r0);
			}
			if (ppc_is_imm16 (cfg->stack_usage)) {
				ppc_addic (code, ppc_sp, cfg->frame_reg, cfg->stack_usage);
			} else {
				ppc_load (code, ppc_r11, cfg->stack_usage);
				ppc_add (code, ppc_sp, cfg->frame_reg, ppc_r11);
			}
			if (!cfg->method->save_lmf) {
				/*for (i = 31; i >= 14; --i) {
					if (cfg->used_float_regs & (1 << i)) {
						pos += sizeof (double);
						ppc_lfd (code, i, -pos, cfg->frame_reg);
					}
				}*/
				for (i = 31; i >= 13; --i) {
					if (cfg->used_int_regs & (1 << i)) {
						pos += sizeof (gulong);
						ppc_lwz (code, i, -pos, cfg->frame_reg);
					}
				}
			} else {
				/* FIXME restore from MonoLMF: though this can't happen yet */
			}
			mono_add_patch_info (cfg, (guint8*) code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			ppc_b (code, 0);
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			ppc_lwz (code, ppc_r0, 0, ins->sreg1);
			break;
		case OP_ARGLIST: {
			if (ppc_is_imm16 (cfg->sig_cookie + cfg->stack_usage)) {
				ppc_addi (code, ppc_r11, cfg->frame_reg, cfg->sig_cookie + cfg->stack_usage);
			} else {
				ppc_load (code, ppc_r11, cfg->sig_cookie + cfg->stack_usage);
				ppc_add (code, ppc_r11, cfg->frame_reg, ppc_r11);
			}
			ppc_stw (code, ppc_r11, 0, ins->sreg1);
			break;
		}
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case CEE_CALL:
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD, call->method);
			else
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, call->fptr);
			ppc_bl (code, 0);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			ppc_mtlr (code, ins->sreg1);
			ppc_blrl (code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			ppc_lwz (code, ppc_r0, ins->inst_offset, ins->sreg1);
			ppc_mtlr (code, ppc_r0);
			ppc_blrl (code);
			break;
		case OP_OUTARG:
			g_assert_not_reached ();
			break;
		case OP_LOCALLOC: {
			guint32 * zero_loop_jump, zero_loop_start;
			/* keep alignment */
			int alloca_waste = PPC_STACK_PARAM_OFFSET + cfg->param_area + 31;
			int area_offset = alloca_waste;
			area_offset &= ~31;
			ppc_addi (code, ppc_r11, ins->sreg1, alloca_waste + 31);
			ppc_rlwinm (code, ppc_r11, ppc_r11, 0, 0, 27);
			/* use ctr to store the number of words to 0 if needed */
			if (ins->flags & MONO_INST_INIT) {
				/* we zero 4 bytes at a time */
				ppc_addi (code, ppc_r0, ins->sreg1, 3);
				ppc_srawi (code, ppc_r0, ppc_r0, 2);
				ppc_mtctr (code, ppc_r0);
			}
			ppc_lwz (code, ppc_r0, 0, ppc_sp);
			ppc_neg (code, ppc_r11, ppc_r11);
			ppc_stwux (code, ppc_r0, ppc_sp, ppc_r11);
			
			if (ins->flags & MONO_INST_INIT) {
				/* adjust the dest reg by -4 so we can use stwu */
				ppc_addi (code, ins->dreg, ppc_sp, (area_offset - 4));
				ppc_li (code, ppc_r11, 0);
				zero_loop_start = code;
				ppc_stwu (code, ppc_r11, 4, ins->dreg);
				zero_loop_jump = code;
				ppc_bc (code, PPC_BR_DEC_CTR_NONZERO, 0, 0);
				ppc_patch (zero_loop_jump, zero_loop_start);
			}
			ppc_addi (code, ins->dreg, ppc_sp, area_offset);
			break;
		}
		case CEE_RET:
			ppc_blr (code);
			break;
		case CEE_THROW: {
			//ppc_break (code);
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception");
			ppc_bl (code, 0);
			break;
		}
		case OP_RETHROW: {
			//ppc_break (code);
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception");
			ppc_bl (code, 0);
			break;
		}
		case OP_START_HANDLER:
			ppc_mflr (code, ppc_r0);
			if (ppc_is_imm16 (ins->inst_left->inst_offset)) {
				ppc_stw (code, ppc_r0, ins->inst_left->inst_offset, ins->inst_left->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_left->inst_offset);
				ppc_stwx (code, ppc_r0, ppc_r11, ins->inst_left->inst_basereg);
			}
			break;
		case OP_ENDFILTER:
			if (ins->sreg1 != ppc_r3)
				ppc_mr (code, ppc_r3, ins->sreg1);
			if (ppc_is_imm16 (ins->inst_left->inst_offset)) {
				ppc_lwz (code, ppc_r0, ins->inst_left->inst_offset, ins->inst_left->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_left->inst_offset);
				ppc_lwzx (code, ppc_r0, ins->inst_left->inst_basereg, ppc_r11);
			}
			ppc_mtlr (code, ppc_r0);
			ppc_blr (code);
			break;
		case CEE_ENDFINALLY:
			ppc_lwz (code, ppc_r0, ins->inst_left->inst_offset, ins->inst_left->inst_basereg);
			ppc_mtlr (code, ppc_r0);
			ppc_blr (code);
			break;
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			ppc_bl (code, 0);
			break;
		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case CEE_BR:
			//g_print ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
			//if ((ins->inst_target_bb == bb->next_bb) && ins == bb->last_ins)
			//break;
			if (ins->flags & MONO_INST_BRLABEL) {
				/*if (ins->inst_i0->inst_c0) {
					ppc_b (code, 0);
					//x86_jump_code (code, cfg->native_code + ins->inst_i0->inst_c0);
				} else*/ {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_LABEL, ins->inst_i0);
					ppc_b (code, 0);
				}
			} else {
				/*if (ins->inst_target_bb->native_offset) {
					ppc_b (code, 0);
					//x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
				} else*/ {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
					ppc_b (code, 0);
				} 
			}
			break;
		case OP_BR_REG:
			ppc_mtctr (code, ins->sreg1);
			ppc_bcctr (code, PPC_BR_ALWAYS, 0);
			break;
		case OP_CEQ:
			ppc_li (code, ins->dreg, 0);
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 2);
			ppc_li (code, ins->dreg, 1);
			break;
		case OP_CLT:
		case OP_CLT_UN:
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_CGT:
		case OP_CGT_UN:
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_NE_UN:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_LT_UN:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_GT_UN:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GE_UN:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LE_UN:
			EMIT_COND_SYSTEM_EXCEPTION (ins->opcode - OP_COND_EXC_EQ, ins->inst_p1);
			break;
		case OP_COND_EXC_C:
			/* check XER [0-3] (SO, OV, CA): we can't use mcrxr
			 */
			/*ppc_mfspr (code, ppc_r0, ppc_xer);
			ppc_andisd (code, ppc_r0, ppc_r0, (1<<14));
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;*/
		case OP_COND_EXC_OV:
			/*ppc_mcrxr (code, 0);
			EMIT_COND_SYSTEM_EXCEPTION (CEE_BGT - CEE_BEQ, ins->inst_p1);
			break;*/
		case OP_COND_EXC_NC:
		case OP_COND_EXC_NO:
			g_assert_not_reached ();
			break;
		case CEE_BEQ:
		case CEE_BNE_UN:
		case CEE_BLT:
		case CEE_BLT_UN:
		case CEE_BGT:
		case CEE_BGT_UN:
		case CEE_BGE:
		case CEE_BGE_UN:
		case CEE_BLE:
		case CEE_BLE_UN:
			EMIT_COND_BRANCH (ins, ins->opcode - CEE_BEQ);
			break;

		/* floating point opcodes */
		case OP_R8CONST:
			ppc_load (code, ppc_r11, ins->inst_p0);
			ppc_lfd (code, ins->dreg, 0, ppc_r11);
			break;
		case OP_R4CONST:
			ppc_load (code, ppc_r11, ins->inst_p0);
			ppc_lfs (code, ins->dreg, 0, ppc_r11);
			break;
		case OP_STORER8_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stfd (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_stfdx (code, ins->sreg1, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_LOADR8_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lfd (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lfdx (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stfs (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_stfsx (code, ins->sreg1, ppc_r11, ins->inst_destbasereg);
			}
			break;
		case OP_LOADR4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lfs (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				ppc_load (code, ppc_r11, ins->inst_offset);
				ppc_lfsx (code, ins->dreg, ppc_r11, ins->inst_basereg);
			}
			break;
		case CEE_CONV_R_UN: {
			static const guint64 adjust_val = 0x4330000000000000ULL;
			ppc_addis (code, ppc_r0, ppc_r0, 0x4330);
			ppc_stw (code, ppc_r0, -8, ppc_sp);
			ppc_stw (code, ins->sreg1, -4, ppc_sp);
			ppc_load (code, ppc_r11, &adjust_val);
			ppc_lfd (code, ins->dreg, -8, ppc_sp);
			ppc_lfd (code, ppc_f0, 0, ppc_r11);
			ppc_fsub (code, ins->dreg, ins->dreg, ppc_f0);
			break;
		}
		case CEE_CONV_R4: /* FIXME: change precision */
		case CEE_CONV_R8: {
			static const guint64 adjust_val = 0x4330000080000000ULL;
			// addis is special for ppc_r0
			ppc_addis (code, ppc_r0, ppc_r0, 0x4330);
			ppc_stw (code, ppc_r0, -8, ppc_sp);
			ppc_xoris (code, ins->sreg1, ppc_r11, 0x8000);
			ppc_stw (code, ppc_r11, -4, ppc_sp);
			ppc_lfd (code, ins->dreg, -8, ppc_sp);
			ppc_load (code, ppc_r11, &adjust_val);
			ppc_lfd (code, ppc_f0, 0, ppc_r11);
			ppc_fsub (code, ins->dreg, ins->dreg, ppc_f0);
			break;
		}
		case OP_X86_FP_LOAD_I8:
			g_assert_not_reached ();
			/*x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);*/
			break;
		case OP_X86_FP_LOAD_I4:
			g_assert_not_reached ();
			/*x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, FALSE);*/
			break;
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);
			break;
		case OP_FCONV_TO_I8:
		case OP_FCONV_TO_U8:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_R_UN:
			g_assert_not_reached ();
			/* Implemented as helper calls */
			break;
		case OP_LCONV_TO_OVF_I: {
			guint32 *negative_branch, *msword_positive_branch, *msword_negative_branch, *ovf_ex_target;
			// Check if its negative
			ppc_cmpi (code, 0, 0, ins->sreg1, 0);
			negative_branch = code;
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 0);
			// Its positive msword == 0
			ppc_cmpi (code, 0, 0, ins->sreg2, 0);
			msword_positive_branch = code;
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_EQ, 0);

			ovf_ex_target = code;
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_ALWAYS, 0, "OverflowException");
			// Negative
			ppc_patch (negative_branch, code);
			ppc_cmpi (code, 0, 0, ins->sreg2, -1);
			msword_negative_branch = code;
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 0);
			ppc_patch (msword_negative_branch, ovf_ex_target);
			
			ppc_patch (msword_positive_branch, code);
			if (ins->dreg != ins->sreg1)
				ppc_mr (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_SQRT:
			ppc_fsqrtd (code, ins->dreg, ins->sreg1);
			break;
		case OP_FADD:
			ppc_fadd (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			ppc_fsub (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FMUL:
			ppc_fmul (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FDIV:
			ppc_fdiv (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FNEG:
			ppc_fneg (code, ins->dreg, ins->sreg1);
			break;		
		case OP_FREM:
			/* emulated */
			g_assert_not_reached ();
			break;
		case OP_FCOMPARE:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			break;
		case OP_FCEQ:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 0);
			ppc_bc (code, PPC_BR_FALSE, PPC_BR_EQ, 2);
			ppc_li (code, ins->dreg, 1);
			break;
		case OP_FCLT:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCLT_UN:
			ppc_fcmpu (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 3);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_LT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCGT:
			ppc_fcmpo (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FCGT_UN:
			ppc_fcmpu (code, 0, ins->sreg1, ins->sreg2);
			ppc_li (code, ins->dreg, 1);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_SO, 3);
			ppc_bc (code, PPC_BR_TRUE, PPC_BR_GT, 2);
			ppc_li (code, ins->dreg, 0);
			break;
		case OP_FBEQ:
			EMIT_COND_BRANCH (ins, CEE_BEQ - CEE_BEQ);
			break;
		case OP_FBNE_UN:
			EMIT_COND_BRANCH (ins, CEE_BNE_UN - CEE_BEQ);
			break;
		case OP_FBLT:
			EMIT_COND_BRANCH (ins, CEE_BLT - CEE_BEQ);
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH_FLAGS (ins, PPC_BR_TRUE, PPC_BR_SO);
			EMIT_COND_BRANCH (ins, CEE_BLT_UN - CEE_BEQ);
			break;
		case OP_FBGT:
			EMIT_COND_BRANCH (ins, CEE_BGT - CEE_BEQ);
			break;
		case OP_FBGT_UN:
			EMIT_COND_BRANCH_FLAGS (ins, PPC_BR_TRUE, PPC_BR_SO);
			EMIT_COND_BRANCH (ins, CEE_BGT_UN - CEE_BEQ);
			break;
		case OP_FBGE:
			EMIT_COND_BRANCH (ins, CEE_BGE - CEE_BEQ);
			break;
		case OP_FBGE_UN:
			EMIT_COND_BRANCH (ins, CEE_BGE_UN - CEE_BEQ);
			break;
		case OP_FBLE:
			EMIT_COND_BRANCH (ins, CEE_BLE - CEE_BEQ);
			break;
		case OP_FBLE_UN:
			EMIT_COND_BRANCH (ins, CEE_BLE_UN - CEE_BEQ);
			break;
		case CEE_CKFINITE: {
			ppc_stfd (code, ins->sreg1, -8, ppc_sp);
			ppc_lwz (code, ppc_r11, -8, ppc_sp);
			ppc_rlwinm (code, ppc_r11, ppc_r11, 0, 1, 31);
			ppc_addis (code, ppc_r11, ppc_r11, -32752);
			ppc_rlwinmd (code, ppc_r11, ppc_r11, 1, 31, 31);
			EMIT_COND_SYSTEM_EXCEPTION (CEE_BEQ - CEE_BEQ, "ArithmeticException");
			break;
		}
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((cfg->opt & MONO_OPT_BRANCH) && ((code - cfg->native_code - offset) > max_len)) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
		last_offset = offset;
		
		ins = ins->next;
	}

	cfg->code_len = code - cfg->native_code;
}

void
mono_arch_register_lowlevel_calls (void)
{
}

#define patch_lis_ori(ip,val) do {\
		guint16 *__lis_ori = (guint16*)(ip);	\
		__lis_ori [1] = (((guint32)(val)) >> 16) & 0xffff;	\
		__lis_ori [3] = ((guint32)(val)) & 0xffff;	\
	} while (0)

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		switch (patch_info->type) {
		case MONO_PATCH_INFO_IP:
			patch_lis_ori (ip, ip);
			continue;
		case MONO_PATCH_INFO_METHOD_REL:
			g_assert_not_reached ();
			*((gpointer *)(ip)) = code + patch_info->data.offset;
			continue;
		case MONO_PATCH_INFO_SWITCH: {
			gpointer *table = (gpointer *)patch_info->data.target;
			int i;

			// FIXME: inspect code to get the register
			ppc_load (ip, ppc_r11, patch_info->data.target);
			//*((gconstpointer *)(ip + 2)) = patch_info->data.target;

			for (i = 0; i < patch_info->table_size; i++) {
				table [i] = (int)patch_info->data.table [i] + code;
			}
			/* we put into the table the absolute address, no need for ppc_patch in this case */
			continue;
		}
		case MONO_PATCH_INFO_METHODCONST:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IMAGE:
		case MONO_PATCH_INFO_FIELD:
		case MONO_PATCH_INFO_VTABLE:
		case MONO_PATCH_INFO_IID:
		case MONO_PATCH_INFO_SFLDA:
		case MONO_PATCH_INFO_LDSTR:
		case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		case MONO_PATCH_INFO_LDTOKEN:
			/* from OP_AOTCONST : lis + ori */
			patch_lis_ori (ip, target);
			continue;
		case MONO_PATCH_INFO_R4:
		case MONO_PATCH_INFO_R8:
			g_assert_not_reached ();
			*((gconstpointer *)(ip + 2)) = patch_info->data.target;
			continue;
		case MONO_PATCH_INFO_EXC_NAME:
			g_assert_not_reached ();
			*((gconstpointer *)(ip + 1)) = patch_info->data.name;
			continue;
		case MONO_PATCH_INFO_BB_OVF:
		case MONO_PATCH_INFO_EXC_OVF:
			/* everything is dealt with at epilog output time */
			continue;
		default:
			break;
		}
		ppc_patch (ip, target);
	}
}

int
mono_arch_max_epilog_size (MonoCompile *cfg)
{
	int max_epilog_size = 16 + 20*4;
	MonoJumpInfo *patch_info;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	/* count the number of exception infos */
     
	/* 
	 * make sure we have enough space for exceptions
	 * 24 is the simulated call to throw_exception_by_name
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			max_epilog_size += 24;
		else if (patch_info->type == MONO_PATCH_INFO_BB_OVF)
			max_epilog_size += 12;
		else if (patch_info->type == MONO_PATCH_INFO_EXC_OVF)
			max_epilog_size += 12;
	}

	return max_epilog_size;
}

/*
 * Stack frame layout:
 * 
 *   ------------------- sp
 *   	MonoLMF structure or saved registers
 *   -------------------
 *   	spilled regs
 *   -------------------
 *   	locals
 *   -------------------
 *   	optional 8 bytes for tracing
 *   -------------------
 *   	param area             size is cfg->param_area
 *   -------------------
 *   	linkage area           size is PPC_STACK_PARAM_OFFSET
 *   ------------------- sp
 *   	red zone
 */
guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int alloc_size, pos, max_offset, i;
	guint8 *code;
	CallInfo *cinfo;
	int tracing = 0;
	int lmf_offset = 0;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		tracing = 1;

	sig = method->signature;
	cfg->code_size = 256 + sig->param_count * 20;
	code = cfg->native_code = g_malloc (cfg->code_size);

	if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
		ppc_mflr (code, ppc_r0);
		ppc_stw (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);
	}
	if (cfg->max_ireg >= 29)
		cfg->used_int_regs |= USE_EXTRA_TEMPS;

	alloc_size = cfg->stack_offset;
	pos = 0;

	if (!method->save_lmf) {
		/*for (i = 31; i >= 14; --i) {
			if (cfg->used_float_regs & (1 << i)) {
				pos += sizeof (gdouble);
				ppc_stfd (code, i, -pos, ppc_sp);
			}
		}*/
		for (i = 31; i >= 13; --i) {
			if (cfg->used_int_regs & (1 << i)) {
				pos += sizeof (gulong);
				ppc_stw (code, i, -pos, ppc_sp);
			}
		}
	} else {
		int ofs;
		pos += sizeof (MonoLMF);
		lmf_offset = pos;
		ofs = -pos + G_STRUCT_OFFSET(MonoLMF, iregs);
		ppc_stmw (code, ppc_r13, ppc_r1, ofs);
		for (i = 14; i < 32; i++) {
			ppc_stfd (code, i, (-pos + G_STRUCT_OFFSET(MonoLMF, fregs) + ((i-14) * sizeof (gdouble))), ppc_r1);
		}
	}
	alloc_size += pos;
	// align to PPC_STACK_ALIGNMENT bytes
	if (alloc_size & (PPC_STACK_ALIGNMENT - 1)) {
		alloc_size += PPC_STACK_ALIGNMENT - 1;
		alloc_size &= ~(PPC_STACK_ALIGNMENT - 1);
	}

	cfg->stack_usage = alloc_size;
	g_assert ((alloc_size & (PPC_STACK_ALIGNMENT-1)) == 0);
	if (alloc_size) {
		if (ppc_is_imm16 (-alloc_size)) {
			ppc_stwu (code, ppc_sp, -alloc_size, ppc_sp);
		} else {
			ppc_load (code, ppc_r11, -alloc_size);
			ppc_stwux (code, ppc_sp, ppc_sp, ppc_r11);
		}
	}
	if (cfg->frame_reg != ppc_sp)
		ppc_mr (code, cfg->frame_reg, ppc_sp);

        /* compute max_offset in order to use short forward jumps
	 * we always do it on ppc because the immediate displacement
	 * for jumps is too small 
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins = bb->code;
		bb->max_offset = max_offset;

		if (cfg->prof_options & MONO_PROFILE_COVERAGE)
			max_offset += 6; 

		while (ins) {
			max_offset += ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];
			ins = ins->next;
		}
	}

	/* load arguments allocated to register from the stack */
	pos = 0;

	cinfo = calculate_sizes (sig, sig->pinvoke);

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		ArgInfo *ainfo = &cinfo->ret;
		inst = cfg->ret;
		if (ppc_is_imm16 (inst->inst_offset)) {
			ppc_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
		} else {
			ppc_load (code, ppc_r11, inst->inst_offset);
			ppc_stwx (code, ainfo->reg, ppc_r11, inst->inst_basereg);
		}
	}
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->varinfo [pos];
		
		if (cfg->verbose_level > 2)
			g_print ("Saving argument %d (type: %d)\n", i, ainfo->regtype);
		if (inst->opcode == OP_REGVAR) {
			if (ainfo->regtype == RegTypeGeneral)
				ppc_mr (code, inst->dreg, ainfo->reg);
			else if (ainfo->regtype == RegTypeFP)
				ppc_fmr (code, inst->dreg, ainfo->reg);
			else if (ainfo->regtype == RegTypeBase) {
				ppc_lwz (code, ppc_r11, 0, ppc_sp);
				ppc_lwz (code, inst->dreg, ainfo->offset, ppc_r11);
			} else
				g_assert_not_reached ();

			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		} else {
			/* the argument should be put on the stack: FIXME handle size != word  */
			if (ainfo->regtype == RegTypeGeneral) {
				switch (ainfo->size) {
				case 1:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stb (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_stbx (code, ainfo->reg, ppc_r11, inst->inst_basereg);
					}
					break;
				case 2:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_sth (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_sthx (code, ainfo->reg, ppc_r11, inst->inst_basereg);
					}
					break;
				case 8:
					if (ppc_is_imm16 (inst->inst_offset + 4)) {
						ppc_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
						ppc_stw (code, ainfo->reg + 1, inst->inst_offset + 4, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_add (code, ppc_r11, ppc_r11, inst->inst_basereg);
						ppc_stw (code, ainfo->reg, 0, ppc_r11);
						ppc_stw (code, ainfo->reg + 1, 4, ppc_r11);
					}
					break;
				default:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_stwx (code, ainfo->reg, ppc_r11, inst->inst_basereg);
					}
					break;
				}
			} else if (ainfo->regtype == RegTypeBase) {
				/* load the previous stack pointer in r11 */
				ppc_lwz (code, ppc_r11, 0, ppc_sp);
				ppc_lwz (code, ppc_r0, ainfo->offset, ppc_r11);
				switch (ainfo->size) {
				case 1:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stb (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_stbx (code, ppc_r0, ppc_r11, inst->inst_basereg);
					}
					break;
				case 2:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_sth (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_sthx (code, ppc_r0, ppc_r11, inst->inst_basereg);
					}
					break;
				case 8:
					if (ppc_is_imm16 (inst->inst_offset + 4)) {
						ppc_stw (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
						ppc_lwz (code, ppc_r0, ainfo->offset + 4, ppc_r11);
						ppc_stw (code, ppc_r0, inst->inst_offset + 4, inst->inst_basereg);
					} else {
						/* FIXME */
						g_assert_not_reached ();
					}
					break;
				default:
					if (ppc_is_imm16 (inst->inst_offset)) {
						ppc_stw (code, ppc_r0, inst->inst_offset, inst->inst_basereg);
					} else {
						ppc_load (code, ppc_r11, inst->inst_offset);
						ppc_stwx (code, ppc_r0, ppc_r11, inst->inst_basereg);
					}
					break;
				}
			} else if (ainfo->regtype == RegTypeFP) {
				g_assert (ppc_is_imm16 (inst->inst_offset));
				if (ainfo->size == 8)
					ppc_stfd (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
				else if (ainfo->size == 4)
					ppc_stfs (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
				else
					g_assert_not_reached ();
			} else if (ainfo->regtype == RegTypeStructByVal) {
				int doffset = inst->inst_offset;
				int soffset = 0;
				int cur_reg;
				int size = 0;
				g_assert (ppc_is_imm16 (inst->inst_offset));
				g_assert (ppc_is_imm16 (inst->inst_offset + ainfo->size * sizeof (gpointer)));
				if (mono_class_from_mono_type (inst->inst_vtype))
					size = mono_class_native_size (mono_class_from_mono_type (inst->inst_vtype), NULL);
				for (cur_reg = 0; cur_reg < ainfo->size; ++cur_reg) {
/*
Darwin handles 1 and 2 byte structs specially by loading h/b into the arg
register.  Should this case include linux/ppc?
*/
#if __APPLE__
					if (size == 2)
						ppc_sth (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					else if (size == 1)
						ppc_stb (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					else 
#endif
						ppc_stw (code, ainfo->reg + cur_reg, doffset, inst->inst_basereg);
					soffset += sizeof (gpointer);
					doffset += sizeof (gpointer);
				}
				if (ainfo->vtsize) {
					/* load the previous stack pointer in r11 (r0 gets overwritten by the memcpy) */
					ppc_lwz (code, ppc_r11, 0, ppc_sp);
					/* FIXME: handle overrun! with struct sizes not multiple of 4 */
					code = emit_memcpy (code, ainfo->vtsize * sizeof (gpointer), inst->inst_basereg, doffset, ppc_r11, ainfo->offset + soffset);
				}
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				g_assert (ppc_is_imm16 (inst->inst_offset));
				/* FIXME: handle overrun! with struct sizes not multiple of 4 */
				code = emit_memcpy (code, ainfo->vtsize * sizeof (gpointer), inst->inst_basereg, inst->inst_offset, ainfo->reg, 0);
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf) {

		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
				     (gpointer)"mono_get_lmf_addr");
		ppc_bl (code, 0);
		/* we build the MonoLMF structure on the stack - see mini-ppc.h */
		/* lmf_offset is the offset from the previous stack pointer,
		 * alloc_size is the total stack space allocated, so the offset
		 * of MonoLMF from the current stack ptr is alloc_size - lmf_offset.
		 * The pointer to the struct is put in ppc_r11 (new_lmf).
		 * The callee-saved registers are already in the MonoLMF structure
		 */
		ppc_addi (code, ppc_r11, ppc_sp, alloc_size - lmf_offset);
		/* ppc_r3 is the result from mono_get_lmf_addr () */
		ppc_stw (code, ppc_r3, G_STRUCT_OFFSET(MonoLMF, lmf_addr), ppc_r11);
		/* new_lmf->previous_lmf = *lmf_addr */
		ppc_lwz (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r3);
		ppc_stw (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r11);
		/* *(lmf_addr) = r11 */
		ppc_stw (code, ppc_r11, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r3);
		/* save method info */
		ppc_load (code, ppc_r0, method);
		ppc_stw (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, method), ppc_r11);
		ppc_stw (code, ppc_sp, G_STRUCT_OFFSET(MonoLMF, ebp), ppc_r11);
		/* save the current IP */
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		ppc_load (code, ppc_r0, 0x01010101);
		ppc_stw (code, ppc_r0, G_STRUCT_OFFSET(MonoLMF, eip), ppc_r11);
	}

	if (tracing)
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);

	cfg->code_len = code - cfg->native_code;
	g_assert (cfg->code_len < cfg->code_size);
	g_free (cinfo);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	MonoMethod *method = cfg->method;
	int pos, i;
	guint8 *code;

	/*
	 * Keep in sync with CEE_JMP
	 */
	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method)) {
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);
	}
	pos = 0;

	if (method->save_lmf) {
		int lmf_offset;
		pos +=  sizeof (MonoLMF);
		lmf_offset = pos;
		/* save the frame reg in r8 */
		ppc_mr (code, ppc_r8, cfg->frame_reg);
		ppc_addi (code, ppc_r11, cfg->frame_reg, cfg->stack_usage - lmf_offset);
		/* r5 = previous_lmf */
		ppc_lwz (code, ppc_r5, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r11);
		/* r6 = lmf_addr */
		ppc_lwz (code, ppc_r6, G_STRUCT_OFFSET(MonoLMF, lmf_addr), ppc_r11);
		/* *(lmf_addr) = previous_lmf */
		ppc_stw (code, ppc_r5, G_STRUCT_OFFSET(MonoLMF, previous_lmf), ppc_r6);
		/* FIXME: speedup: there is no actual need to restore the registers if
		 * we didn't actually change them (idea from Zoltan).
		 */
		/* restore iregs */
		ppc_lmw (code, ppc_r13, ppc_r11, G_STRUCT_OFFSET(MonoLMF, iregs));
		/* restore fregs */
		/*for (i = 14; i < 32; i++) {
			ppc_lfd (code, i, G_STRUCT_OFFSET(MonoLMF, fregs) + ((i-14) * sizeof (gdouble)), ppc_r11);
		}*/
		g_assert (ppc_is_imm16 (cfg->stack_usage + PPC_RET_ADDR_OFFSET));
		/* use the saved copy of the frame reg in r8 */
		if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
			ppc_lwz (code, ppc_r0, cfg->stack_usage + PPC_RET_ADDR_OFFSET, ppc_r8);
			ppc_mtlr (code, ppc_r0);
		}
		ppc_addic (code, ppc_sp, ppc_r8, cfg->stack_usage);
	} else {
		if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
			if (ppc_is_imm16 (cfg->stack_usage + PPC_RET_ADDR_OFFSET)) {
				ppc_lwz (code, ppc_r0, cfg->stack_usage + PPC_RET_ADDR_OFFSET, cfg->frame_reg);
			} else {
				ppc_load (code, ppc_r11, cfg->stack_usage + PPC_RET_ADDR_OFFSET);
				ppc_lwzx (code, ppc_r0, cfg->frame_reg, ppc_r11);
			}
			ppc_mtlr (code, ppc_r0);
		}
		if (ppc_is_imm16 (cfg->stack_usage)) {
			ppc_addic (code, ppc_sp, cfg->frame_reg, cfg->stack_usage);
		} else {
			ppc_load (code, ppc_r11, cfg->stack_usage);
			ppc_add (code, ppc_sp, cfg->frame_reg, ppc_r11);
		}

		/*for (i = 31; i >= 14; --i) {
			if (cfg->used_float_regs & (1 << i)) {
				pos += sizeof (double);
				ppc_lfd (code, i, -pos, ppc_sp);
			}
		}*/
		for (i = 31; i >= 13; --i) {
			if (cfg->used_int_regs & (1 << i)) {
				pos += sizeof (gulong);
				ppc_lwz (code, i, -pos, ppc_sp);
			}
		}
	}
	ppc_blr (code);

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_BB_OVF: {
			MonoOvfJump *ovfj = patch_info->data.target;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			/* patch the initial jump */
			ppc_patch (ip, code);
			ppc_bc (code, ovfj->b0_cond, ovfj->b1_cond, 2);
			ppc_b (code, 0);
			ppc_patch (code - 4, ip + 4); /* jump back after the initiali branch */
			/* jump back to the true target */
			ppc_b (code, 0);
			ip = ovfj->bb->native_offset + cfg->native_code;
			ppc_patch (code - 4, ip);
			break;
		}
		case MONO_PATCH_INFO_EXC_OVF: {
			MonoOvfJump *ovfj = patch_info->data.target;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			/* patch the initial jump */
			ppc_patch (ip, code);
			ppc_bc (code, ovfj->b0_cond, ovfj->b1_cond, 2);
			ppc_b (code, 0);
			ppc_patch (code - 4, ip + 4); /* jump back after the initiali branch */
			/* jump back to the true target */
			ppc_b (code, 0);
			ip = (char*)ovfj->ip + 4;
			ppc_patch (code - 4, ip);
			break;
		}
		case MONO_PATCH_INFO_EXC: {
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			ppc_patch (ip, code);
			/*mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_NAME, patch_info->data.target);*/
			ppc_load (code, ppc_r3, patch_info->data.target);
			/* simulate a call from ip */
			ppc_load (code, ppc_r0, ip + 4);
			ppc_mtlr (code, ppc_r0);
			patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "mono_arch_throw_exception_by_name";
			patch_info->ip.i = code - cfg->native_code;
			ppc_b (code, 0);
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	int this_dreg = ppc_r3;
	
	if (vt_reg != -1)
		this_dreg = ppc_r4;

	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this;
		MONO_INST_NEW (cfg, this, OP_SETREG);
		this->type = this_type;
		this->sreg1 = this_reg;
		this->dreg = this_dreg;
		mono_bblock_add_inst (cfg->cbb, this);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_SETREG);
		vtarg->type = STACK_MP;
		vtarg->sreg1 = vt_reg;
		vtarg->dreg = ppc_r3;
		mono_bblock_add_inst (cfg->cbb, vtarg);
	}
}

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	/*
	MonoInst *ins = NULL;

	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sqrt") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SQRT);
			ins->inst_i0 = args [0];
		}
	}
	return ins;
	*/
	return NULL;
}

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	return 0;
}

MonoInst* mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
	return NULL;
}

MonoInst* mono_arch_get_thread_intrinsic (MonoCompile* cfg)
{
	return NULL;
}
