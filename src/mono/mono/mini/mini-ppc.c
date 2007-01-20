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
#ifdef __APPLE__
#include <sys/sysctl.h>
#endif

#define FORCE_INDIR_CALL 1

enum {
	TLS_MODE_DETECT,
	TLS_MODE_FAILED,
	TLS_MODE_LTHREADS,
	TLS_MODE_NPTL,
	TLS_MODE_DARWIN_G4,
	TLS_MODE_DARWIN_G5
};

int mono_exc_esp_offset = 0;
static int tls_mode = TLS_MODE_DETECT;
static int lmf_pthread_key = -1;
static int monothread_key = -1;
static int monodomain_key = -1;

static int
offsets_from_pthread_key (guint32 key, int *offset2)
{
	int idx1 = key / 32;
	int idx2 = key % 32;
	*offset2 = idx2 * sizeof (gpointer);
	return 284 + idx1 * sizeof (gpointer);
}

#define emit_linuxthreads_tls(code,dreg,key) do {\
		int off1, off2;	\
		off1 = offsets_from_pthread_key ((key), &off2);	\
		ppc_lwz ((code), (dreg), off1, ppc_r2);	\
		ppc_lwz ((code), (dreg), off2, (dreg));	\
	} while (0);

#define emit_darwing5_tls(code,dreg,key) do {\
		int off1 = 0x48 + key * sizeof (gpointer);	\
		ppc_mfspr ((code), (dreg), 104);	\
		ppc_lwz ((code), (dreg), off1, (dreg));	\
	} while (0);

/* FIXME: ensure the sc call preserves all but r3 */
#define emit_darwing4_tls(code,dreg,key) do {\
		int off1 = 0x48 + key * sizeof (gpointer);	\
		if ((dreg) != ppc_r3) ppc_mr ((code), ppc_r11, ppc_r3);	\
		ppc_li ((code), ppc_r0, 0x7FF2);	\
		ppc_sc ((code));	\
		ppc_lwz ((code), (dreg), off1, ppc_r3);	\
		if ((dreg) != ppc_r3) ppc_mr ((code), ppc_r3, ppc_r11);	\
	} while (0);

#define emit_tls_access(code,dreg,key) do {	\
		switch (tls_mode) {	\
		case TLS_MODE_LTHREADS: emit_linuxthreads_tls(code,dreg,key); break;	\
		case TLS_MODE_DARWIN_G5: emit_darwing5_tls(code,dreg,key); break;	\
		case TLS_MODE_DARWIN_G4: emit_darwing4_tls(code,dreg,key); break;	\
		default: g_assert_not_reached ();	\
		}	\
	} while (0)

const char*
mono_arch_regname (int reg) {
	static const char rnames[][4] = {
		"r0", "sp", "r2", "r3", "r4",
		"r5", "r6", "r7", "r8", "r9",
		"r10", "r11", "r12", "r13", "r14",
		"r15", "r16", "r17", "r18", "r19",
		"r20", "r21", "r22", "r23", "r24",
		"r25", "r26", "r27", "r28", "r29",
		"r30", "r31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg) {
	static const char rnames[][4] = {
		"f0", "f1", "f2", "f3", "f4",
		"f5", "f6", "f7", "f8", "f9",
		"f10", "f11", "f12", "f13", "f14",
		"f15", "f16", "f17", "f18", "f19",
		"f20", "f21", "f22", "f23", "f24",
		"f25", "f26", "f27", "f28", "f29",
		"f30", "f31"
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
	*exclude_mask = 0;
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
	case MONO_TYPE_FNPTR:
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TRUE;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (t))
			return TRUE;
		return FALSE;
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

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i, top = 32;
	if (cfg->frame_reg != ppc_sp)
		top = 31;
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

typedef struct {
	long int type;
	long int value;
} AuxVec;

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	guint8 *p, *endp, *start;
	static int cachelinesize = 0;
	static int cachelineinc = 16;

	if (!cachelinesize) {
#ifdef __APPLE__
		int mib [3], len;
		mib [0] = CTL_HW;
		mib [1] = HW_CACHELINE;
		len = sizeof (cachelinesize);
		if (sysctl(mib, 2, &cachelinesize, &len, NULL, 0) == -1) {
			perror ("sysctl");
			cachelinesize = 128;
		} else {
			cachelineinc = cachelinesize;
			/*g_print ("setting cl size to %d\n", cachelinesize);*/
		}
#elif defined(__linux__)
		/* sadly this will work only with 2.6 kernels... */
		FILE* f = fopen ("/proc/self/auxv", "rb");
		if (f) {
			AuxVec vec;
			while (fread (&vec, sizeof (vec), 1, f) == 1) {
				if (vec.type == 19) {
					cachelinesize = vec.value;
					break;
				}
			}
			fclose (f);
		}
		if (!cachelinesize)
			cachelinesize = 128;
#else
#warning Need a way to get cache line size
		cachelinesize = 128;
#endif
	}
	p = start = code;
	endp = p + size;
	start = (guint8*)((guint32)start & ~(cachelinesize - 1));
	/* use dcbf for smp support, later optimize for UP, see pem._64bit.d20030611.pdf page 211 */
	if (1) {
		for (p = start; p < endp; p += cachelineinc) {
			asm ("dcbf 0,%0;" : : "r"(p) : "memory");
		}
	} else {
		for (p = start; p < endp; p += cachelineinc) {
			asm ("dcbst 0,%0;" : : "r"(p) : "memory");
		}
	}
	asm ("sync");
	p = code;
	for (p = start; p < endp; p += cachelineinc) {
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
#define FP_ALSO_IN_REG(s)
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

#if __APPLE__
/* size == 4 is checked already */
static gboolean
has_only_a_r4_field (MonoClass *klass)
{
	gpointer iter;
	MonoClassField *f;
	iter = NULL;
	while ((f = mono_class_get_fields (klass, &iter))) {
		if (!(f->type->attrs & FIELD_ATTRIBUTE_STATIC)) {
			if (!f->type->byref && f->type->type == MONO_TYPE_R4)
				return TRUE;
			return FALSE;
		}
	}
	return FALSE;
}
#endif

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
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->params [i])) {
				cinfo->args [n].size = sizeof (gpointer);
				add_general (&gr, &stack_size, cinfo->args + n, TRUE);
				n++;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE: {
			gint size;
			MonoClass *klass;
			klass = mono_class_from_mono_type (sig->params [i]);
			if (is_pinvoke)
			    size = mono_class_native_size (klass, NULL);
			else
			    size = mono_class_value_size (klass, NULL);
#if __APPLE__
			if (size == 4 && has_only_a_r4_field (klass)) {
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
			}
#endif
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
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->ret)) {
				cinfo->ret.reg = ppc_r3;
				break;
			}
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

	m->flags |= MONO_CFG_HAS_SPILLUP;

	/* allow room for the vararg method args: void* and long/double */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (m->method))
		m->param_area = MAX (m->param_area, sizeof (gpointer)*8);
	/* this is bug #60332: remove when #59509 is fixed, so no weird vararg 
	 * call convs needs to be handled this way.
	 */
	if (m->flags & MONO_CFG_HAS_VARARGS)
		m->param_area = MAX (m->param_area, sizeof (gpointer)*8);
	/* gtk-sharp and other broken code will dllimport vararg functions even with
	 * non-varargs signatures. Since there is little hope people will get this right
	 * we assume they won't.
	 */
	if (m->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE)
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

	sig = mono_method_signature (m->method);
	
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

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
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
			arg->inst_call = call;
			arg->type = in->type;
			/* prepend, we'll need to reverse them later */
			arg->next = call->out_args;
			call->out_args = arg;
			if (ainfo->regtype == RegTypeGeneral) {
				arg->backend.reg3 = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
				if (arg->type == STACK_I8)
					call->used_iregs |= 1 << (ainfo->reg + 1);
			} else if (ainfo->regtype == RegTypeStructByAddr) {
				/* FIXME: where si the data allocated? */
				arg->backend.reg3 = ainfo->reg;
				call->used_iregs |= 1 << ainfo->reg;
			} else if (ainfo->regtype == RegTypeStructByVal) {
				int cur_reg;
				MonoPPCArgInfo *ai = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoPPCArgInfo));
				/* mark the used regs */
				for (cur_reg = 0; cur_reg < ainfo->size; ++cur_reg) {
					call->used_iregs |= 1 << (ainfo->reg + cur_reg);
				}
				arg->opcode = OP_OUTARG_VT;
				ai->reg = ainfo->reg;
				ai->size = ainfo->size;
				ai->vtsize = ainfo->vtsize;
				ai->offset = ainfo->offset;
				arg->backend.data = ai;
			} else if (ainfo->regtype == RegTypeBase) {
				MonoPPCArgInfo *ai = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoPPCArgInfo));
				arg->opcode = OP_OUTARG_MEMBASE;
				ai->reg = ainfo->reg;
				ai->size = ainfo->size;
				ai->offset = ainfo->offset;
				arg->backend.data = ai;
			} else if (ainfo->regtype == RegTypeFP) {
				arg->opcode = OP_OUTARG_R8;
				arg->backend.reg3 = ainfo->reg;
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
	int offset;
	MonoMethod *method = cfg->method;
	int rtype = mono_type_get_underlying_type (mono_method_signature (method)->ret)->type;
	int save_offset = PPC_STACK_PARAM_OFFSET + cfg->param_area;
	save_offset += 15;
	save_offset &= ~15;
	
	offset = code - cfg->native_code;
	/* we need about 16 instructions */
	if (offset > (cfg->code_size - 16 * 4)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		code = cfg->native_code + offset;
	}
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
	union {
		MonoBasicBlock *bb;
		const char *exception;
	} data;
	guint32 ip_offset;
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
			ovfj->data.bb = ins->inst_true_bb;	\
			ovfj->ip_offset = 0;	\
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
			ovfj->data.exception = (exc_name);	\
			ovfj->ip_offset = code - cfg->native_code;	\
			ovfj->b0_cond = (b0);	\
			ovfj->b1_cond = (b1);	\
		        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj); \
			ppc_bl (code, 0);	\
			cfg->bb_exit->max_offset += 24;	\
		} else {	\
			mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name);  \
			ppc_bcl (code, (b0), (b1), 0);	\
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

static void
insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *to_insert)
{
	if (ins == NULL) {
		ins = bb->code;
		bb->code = to_insert;
		to_insert->next = ins;
	} else {
		to_insert->next = ins->next;
		ins->next = to_insert;
	}
}

#define NEW_INS(cfg,dest,op) do {       \
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));       \
		(dest)->opcode = (op);  \
		insert_after_ins (bb, last_ins, (dest)); \
	} while (0)

static int
map_to_reg_reg_op (int op)
{
	switch (op) {
	case OP_ADD_IMM:
		return CEE_ADD;
	case OP_SUB_IMM:
		return CEE_SUB;
	case OP_AND_IMM:
		return CEE_AND;
	case OP_COMPARE_IMM:
		return OP_COMPARE;
	case OP_ADDCC_IMM:
		return OP_ADDCC;
	case OP_ADC_IMM:
		return OP_ADC;
	case OP_SUBCC_IMM:
		return OP_SUBCC;
	case OP_SBB_IMM:
		return OP_SBB;
	case OP_OR_IMM:
		return CEE_OR;
	case OP_XOR_IMM:
		return CEE_XOR;
	case OP_MUL_IMM:
		return CEE_MUL;
	case OP_LOAD_MEMBASE:
		return OP_LOAD_MEMINDEX;
	case OP_LOADI4_MEMBASE:
		return OP_LOADI4_MEMINDEX;
	case OP_LOADU4_MEMBASE:
		return OP_LOADU4_MEMINDEX;
	case OP_LOADU1_MEMBASE:
		return OP_LOADU1_MEMINDEX;
	case OP_LOADI2_MEMBASE:
		return OP_LOADI2_MEMINDEX;
	case OP_LOADU2_MEMBASE:
		return OP_LOADU2_MEMINDEX;
	case OP_LOADI1_MEMBASE:
		return OP_LOADI1_MEMINDEX;
	case OP_LOADR4_MEMBASE:
		return OP_LOADR4_MEMINDEX;
	case OP_LOADR8_MEMBASE:
		return OP_LOADR8_MEMINDEX;
	case OP_STOREI1_MEMBASE_REG:
		return OP_STOREI1_MEMINDEX;
	case OP_STOREI2_MEMBASE_REG:
		return OP_STOREI2_MEMINDEX;
	case OP_STOREI4_MEMBASE_REG:
		return OP_STOREI4_MEMINDEX;
	case OP_STORE_MEMBASE_REG:
		return OP_STORE_MEMINDEX;
	case OP_STORER4_MEMBASE_REG:
		return OP_STORER4_MEMINDEX;
	case OP_STORER8_MEMBASE_REG:
		return OP_STORER8_MEMINDEX;
	case OP_STORE_MEMBASE_IMM:
		return OP_STORE_MEMBASE_REG;
	case OP_STOREI1_MEMBASE_IMM:
		return OP_STOREI1_MEMBASE_REG;
	case OP_STOREI2_MEMBASE_IMM:
		return OP_STOREI2_MEMBASE_REG;
	case OP_STOREI4_MEMBASE_IMM:
		return OP_STOREI4_MEMBASE_REG;
	}
	g_assert_not_reached ();
}

#define compare_opcode_is_unsigned(opcode) \
		(((opcode) >= CEE_BNE_UN && (opcode) <= CEE_BLT_UN) ||	\
		((opcode) >= OP_COND_EXC_NE_UN && (opcode) <= OP_COND_EXC_LT_UN) ||	\
		((opcode) == OP_CLT_UN || (opcode) == OP_CGT_UN))
/*
 * Remove from the instruction list the instructions that can't be
 * represented with very simple instructions with no register
 * requirements.
 */
static void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next, *temp, *last_ins = NULL;
	int imm;

	/* setup the virtual reg allocator */
	if (bb->max_ireg > cfg->rs->next_vireg)
		cfg->rs->next_vireg = bb->max_ireg;

	ins = bb->code;
	while (ins) {
loop_start:
		switch (ins->opcode) {
		case OP_ADD_IMM:
		case OP_ADDCC_IMM:
			if (!ppc_is_imm16 (ins->inst_imm)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_SUB_IMM:
			if (!ppc_is_imm16 (-ins->inst_imm)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_AND_IMM:
		case OP_OR_IMM:
		case OP_XOR_IMM:
			if ((ins->inst_imm & 0xffff0000) && (ins->inst_imm & 0xffff)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_SBB_IMM:
		case OP_SUBCC_IMM:
		case OP_ADC_IMM:
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_regstate_next_int (cfg->rs);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_COMPARE_IMM:
			if (compare_opcode_is_unsigned (ins->next->opcode)) {
				if (!ppc_is_uimm16 (ins->inst_imm)) {
					NEW_INS (cfg, temp, OP_ICONST);
					temp->inst_c0 = ins->inst_imm;
					temp->dreg = mono_regstate_next_int (cfg->rs);
					ins->sreg2 = temp->dreg;
					ins->opcode = map_to_reg_reg_op (ins->opcode);
				}
			} else {
				if (!ppc_is_imm16 (ins->inst_imm)) {
					NEW_INS (cfg, temp, OP_ICONST);
					temp->inst_c0 = ins->inst_imm;
					temp->dreg = mono_regstate_next_int (cfg->rs);
					ins->sreg2 = temp->dreg;
					ins->opcode = map_to_reg_reg_op (ins->opcode);
				}
			}
			break;
		case OP_MUL_IMM:
			if (ins->inst_imm == 1) {
				ins->opcode = OP_MOVE;
				break;
			}
			if (ins->inst_imm == 0) {
				ins->opcode = OP_ICONST;
				ins->inst_c0 = 0;
				break;
			}
			imm = mono_is_power_of_two (ins->inst_imm);
			if (imm > 0) {
				ins->opcode = OP_SHL_IMM;
				ins->inst_imm = imm;
				break;
			}
			if (!ppc_is_imm16 (ins->inst_imm)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
		case OP_LOADI2_MEMBASE:
		case OP_LOADU2_MEMBASE:
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
		case OP_LOADR4_MEMBASE:
		case OP_LOADR8_MEMBASE:
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
		case OP_STOREI2_MEMBASE_REG:
		case OP_STOREI1_MEMBASE_REG:
		case OP_STORER4_MEMBASE_REG:
		case OP_STORER8_MEMBASE_REG:
			/* we can do two things: load the immed in a register
			 * and use an indexed load, or see if the immed can be
			 * represented as an ad_imm + a load with a smaller offset
			 * that fits. We just do the first for now, optimize later.
			 */
			if (ppc_is_imm16 (ins->inst_offset))
				break;
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_regstate_next_int (cfg->rs);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_regstate_next_int (cfg->rs);
			ins->sreg1 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			last_ins = temp;
			goto loop_start; /* make it handle the possibly big ins->inst_offset */
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
	bb->max_ireg = cfg->rs->next_vireg;
	
}

void
mono_arch_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	if (!bb->code)
		return;
	mono_arch_lowering_pass (cfg, bb);
	mono_local_regalloc (cfg, bb);
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
				/*{
					static int num_thunks = 0;
					num_thunks++;
					if ((num_thunks % 20) == 0)
						g_print ("num_thunks lookup: %d\n", num_thunks);
				}*/
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
				/*{
					static int num_thunks = 0;
					num_thunks++;
					if ((num_thunks % 20) == 0)
						g_print ("num_thunks: %d\n", num_thunks);
				}*/
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
		return;
	}

	if (prim == 15 || ins == 0x4e800021 || ins == 0x4e800020 || ins == 0x4e800420) {
		guint32 *seq;
		/* the trampoline code will try to patch the blrl, blr, bcctr */
		if (ins == 0x4e800021 || ins == 0x4e800020 || ins == 0x4e800420) {
			code -= 12;
		}
		/* this is the lis/ori/mtlr/blrl sequence */
		seq = (guint32*)code;
		g_assert ((seq [0] >> 26) == 15);
		g_assert ((seq [1] >> 26) == 24);
		g_assert ((seq [2] >> 26) == 31);
		g_assert (seq [3] == 0x4e800021 || seq [3] == 0x4e800020 || seq [3] == 0x4e800420);
		/* FIXME: make this thread safe */
		ppc_lis (code, ppc_r0, (guint32)(target) >> 16);
		ppc_ori (code, ppc_r0, ppc_r0, (guint32)(target) & 0xffff);
		mono_arch_flush_icache (code - 8, 8);
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

		max_len = ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
		}
	//	if (ins->cil_code)
	//		g_print ("cil code\n");
		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_TLS_GET:
			emit_tls_access (code, ins->dreg, ins->inst_offset);
			break;
		case OP_BIGMUL:
			ppc_mullw (code, ppc_r0, ins->sreg1, ins->sreg2);
			ppc_mulhw (code, ppc_r3, ins->sreg1, ins->sreg2);
			ppc_mr (code, ppc_r4, ppc_r0);
			break;
		case OP_BIGMUL_UN:
			ppc_mullw (code, ppc_r0, ins->sreg1, ins->sreg2);
			ppc_mulhwu (code, ppc_r3, ins->sreg1, ins->sreg2);
			ppc_mr (code, ppc_r4, ppc_r0);
			break;
		case OP_MEMORY_BARRIER:
			ppc_sync (code);
			break;
		case OP_STOREI1_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stb (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_STOREI2_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_sth (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stw (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_STOREI1_MEMINDEX:
			ppc_stbx (code, ins->sreg1, ins->sreg2, ins->inst_destbasereg);
			break;
		case OP_STOREI2_MEMINDEX:
			ppc_sthx (code, ins->sreg1, ins->sreg2, ins->inst_destbasereg);
			break;
		case OP_STORE_MEMINDEX:
		case OP_STOREI4_MEMINDEX:
			ppc_stwx (code, ins->sreg1, ins->sreg2, ins->inst_destbasereg);
			break;
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
			g_assert_not_reached ();
			//x86_mov_reg_mem (code, ins->dreg, ins->inst_p0, 4);
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lwz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lbz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				g_assert_not_reached ();
			}
			if (ins->opcode == OP_LOADI1_MEMBASE)
				ppc_extsb (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADU2_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lhz (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_LOADI2_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lha (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_LOAD_MEMINDEX:
		case OP_LOADI4_MEMINDEX:
		case OP_LOADU4_MEMINDEX:
			ppc_lwzx (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			break;
		case OP_LOADU2_MEMINDEX:
			ppc_lhzx (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			break;
		case OP_LOADI2_MEMINDEX:
			ppc_lhax (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			break;
		case OP_LOADU1_MEMINDEX:
			ppc_lbzx (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			break;
		case OP_LOADI1_MEMINDEX:
			ppc_lbzx (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			ppc_extsb (code, ins->dreg, ins->dreg);
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
			if (ins->next && compare_opcode_is_unsigned (ins->next->opcode))
				ppc_cmpl (code, 0, 0, ins->sreg1, ins->sreg2);
			else
				ppc_cmp (code, 0, 0, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
			if (ins->next && compare_opcode_is_unsigned (ins->next->opcode)) {
				if (ppc_is_uimm16 (ins->inst_imm)) {
					ppc_cmpli (code, 0, 0, ins->sreg1, (ins->inst_imm & 0xffff));
				} else {
					g_assert_not_reached ();
				}
			} else {
				if (ppc_is_imm16 (ins->inst_imm)) {
					ppc_cmpi (code, 0, 0, ins->sreg1, (ins->inst_imm & 0xffff));
				} else {
					g_assert_not_reached ();
				}
			}
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
				g_assert_not_reached ();
			}
			break;
		case OP_ADD_IMM:
			if (ppc_is_imm16 (ins->inst_imm)) {
				ppc_addi (code, ins->dreg, ins->sreg1, ins->inst_imm);
			} else {
				g_assert_not_reached ();
			}
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
				g_assert_not_reached ();
			}
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
				g_assert_not_reached ();
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
				g_assert_not_reached ();
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
				g_assert_not_reached ();
			}
			break;
		case CEE_SHL:
			ppc_slw (code, ins->sreg1, ins->dreg, ins->sreg2);
			break;
		case OP_SHL_IMM:
			ppc_rlwinm (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f), 0, (31 - (ins->inst_imm & 0x1f)));
			break;
		case CEE_SHR:
			ppc_sraw (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHR_IMM:
			ppc_srawi (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			break;
		case OP_SHR_UN_IMM:
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
			    g_assert_not_reached ();
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
			if (FORCE_INDIR_CALL || cfg->method->dynamic) {
				ppc_lis (code, ppc_r0, 0);
				ppc_ori (code, ppc_r0, ppc_r0, 0);
				ppc_mtlr (code, ppc_r0);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
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
			guint32 * zero_loop_jump, * zero_loop_start;
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
			if (FORCE_INDIR_CALL || cfg->method->dynamic) {
				ppc_lis (code, ppc_r0, 0);
				ppc_ori (code, ppc_r0, ppc_r0, 0);
				ppc_mtlr (code, ppc_r0);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
			break;
		}
		case OP_RETHROW: {
			//ppc_break (code);
			ppc_mr (code, ppc_r3, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception");
			if (FORCE_INDIR_CALL || cfg->method->dynamic) {
				ppc_lis (code, ppc_r0, 0);
				ppc_ori (code, ppc_r0, ppc_r0, 0);
				ppc_mtlr (code, ppc_r0);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
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
				g_assert_not_reached ();
			}
			break;
		case OP_LOADR8_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lfd (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			ppc_frsp (code, ins->sreg1, ins->sreg1);
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_stfs (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_LOADR4_MEMBASE:
			if (ppc_is_imm16 (ins->inst_offset)) {
				ppc_lfs (code, ins->dreg, ins->inst_offset, ins->inst_basereg);
			} else {
				g_assert_not_reached ();
			}
			break;
		case OP_LOADR4_MEMINDEX:
			ppc_lfsx (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			break;
		case OP_LOADR8_MEMINDEX:
			ppc_lfdx (code, ins->dreg, ins->sreg2, ins->inst_basereg);
			break;
		case OP_STORER4_MEMINDEX:
			ppc_frsp (code, ins->sreg1, ins->sreg1);
			ppc_stfsx (code, ins->sreg1, ins->sreg2, ins->inst_destbasereg);
			break;
		case OP_STORER8_MEMINDEX:
			ppc_stfdx (code, ins->sreg1, ins->sreg2, ins->inst_destbasereg);
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
			gpointer *table = (gpointer *)patch_info->data.table->table;
			int i;

			patch_lis_ori (ip, table);

			for (i = 0; i < patch_info->data.table->table_size; i++) { 
				table [i] = (int)patch_info->data.table->table [i] + code;
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
		case MONO_PATCH_INFO_NONE:
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

	sig = mono_method_signature (method);
	cfg->code_size = 256 + sig->param_count * 20;
	code = cfg->native_code = g_malloc (cfg->code_size);

	if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
		ppc_mflr (code, ppc_r0);
		ppc_stw (code, ppc_r0, PPC_RET_ADDR_OFFSET, ppc_sp);
	}

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
			max_offset += ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
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

	if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		ppc_load (code, ppc_r3, cfg->domain);
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, (gpointer)"mono_jit_thread_attach");
		if (FORCE_INDIR_CALL || cfg->method->dynamic) {
			ppc_lis (code, ppc_r0, 0);
			ppc_ori (code, ppc_r0, ppc_r0, 0);
			ppc_mtlr (code, ppc_r0);
			ppc_blrl (code);
		} else {
			ppc_bl (code, 0);
		}
	}

	if (method->save_lmf) {
		if (lmf_pthread_key != -1) {
			emit_tls_access (code, ppc_r3, lmf_pthread_key);
			if (G_STRUCT_OFFSET (MonoJitTlsData, lmf))
				ppc_addi (code, ppc_r3, ppc_r3, G_STRUCT_OFFSET (MonoJitTlsData, lmf));
		} else {
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
				     (gpointer)"mono_get_lmf_addr");
			if (FORCE_INDIR_CALL || cfg->method->dynamic) {
				ppc_lis (code, ppc_r0, 0);
				ppc_ori (code, ppc_r0, ppc_r0, 0);
				ppc_mtlr (code, ppc_r0);
				ppc_blrl (code);
			} else {
				ppc_bl (code, 0);
			}
		}
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
	int max_epilog_size = 16 + 20*4;
	guint8 *code;

	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

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

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

/* remove once throw_exception_by_name is eliminated */
static int
exception_id_by_name (const char *name)
{
	if (strcmp (name, "IndexOutOfRangeException") == 0)
		return MONO_EXC_INDEX_OUT_OF_RANGE;
	if (strcmp (name, "OverflowException") == 0)
		return MONO_EXC_OVERFLOW;
	if (strcmp (name, "ArithmeticException") == 0)
		return MONO_EXC_ARITHMETIC;
	if (strcmp (name, "DivideByZeroException") == 0)
		return MONO_EXC_DIVIDE_BY_ZERO;
	if (strcmp (name, "InvalidCastException") == 0)
		return MONO_EXC_INVALID_CAST;
	if (strcmp (name, "NullReferenceException") == 0)
		return MONO_EXC_NULL_REF;
	if (strcmp (name, "ArrayTypeMismatchException") == 0)
		return MONO_EXC_ARRAY_TYPE_MISMATCH;
	g_error ("Unknown intrinsic exception %s\n", name);
	return 0;
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int nthrows, i;
	guint8 *code;
	const guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM] = {NULL};
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM] = {0};
	guint32 code_size;
	int exc_count = 0;
	int max_epilog_size = 50;

	/* count the number of exception infos */
     
	/* 
	 * make sure we have enough space for exceptions
	 * 24 is the simulated call to throw_exception_by_name
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC) {
			i = exception_id_by_name (patch_info->data.target);
			if (!exc_throw_found [i]) {
				max_epilog_size += 24;
				exc_throw_found [i] = TRUE;
			}
		} else if (patch_info->type == MONO_PATCH_INFO_BB_OVF)
			max_epilog_size += 12;
		else if (patch_info->type == MONO_PATCH_INFO_EXC_OVF) {
			MonoOvfJump *ovfj = patch_info->data.target;
			i = exception_id_by_name (ovfj->data.exception);
			if (!exc_throw_found [i]) {
				max_epilog_size += 24;
				exc_throw_found [i] = TRUE;
			}
			max_epilog_size += 8;
		}
	}

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

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
			ip = ovfj->data.bb->native_offset + cfg->native_code;
			ppc_patch (code - 4, ip);
			break;
		}
		case MONO_PATCH_INFO_EXC_OVF: {
			MonoOvfJump *ovfj = patch_info->data.target;
			MonoJumpInfo *newji;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			unsigned char *bcl = code;
			/* patch the initial jump: we arrived here with a call */
			ppc_patch (ip, code);
			ppc_bc (code, ovfj->b0_cond, ovfj->b1_cond, 0);
			ppc_b (code, 0);
			ppc_patch (code - 4, ip + 4); /* jump back after the initiali branch */
			/* patch the conditional jump to the right handler */
			/* make it processed next */
			newji = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));
			newji->type = MONO_PATCH_INFO_EXC;
			newji->ip.i = bcl - cfg->native_code;
			newji->data.target = ovfj->data.exception;
			newji->next = patch_info->next;
			patch_info->next = newji;
			break;
		}
		case MONO_PATCH_INFO_EXC: {
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			i = exception_id_by_name (patch_info->data.target);
			if (exc_throw_pos [i]) {
				ppc_patch (ip, exc_throw_pos [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
				break;
			} else {
				exc_throw_pos [i] = code;
			}
			ppc_patch (ip, code);
			/*mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_NAME, patch_info->data.target);*/
			ppc_load (code, ppc_r3, patch_info->data.target);
			/* we got here from a conditional call, so the calling ip is set in lr already */
			patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "mono_arch_throw_exception_by_name";
			patch_info->ip.i = code - cfg->native_code;
			if (FORCE_INDIR_CALL || cfg->method->dynamic) {
				ppc_lis (code, ppc_r0, 0);
				ppc_ori (code, ppc_r0, ppc_r0, 0);
				ppc_mtctr (code, ppc_r0);
				ppc_bcctr (code, PPC_BR_ALWAYS, 0);
			} else {
				ppc_b (code, 0);
			}
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

static int
try_offset_access (void *value, guint32 idx)
{
	register void* me __asm__ ("r2");
	void ***p = (void***)((char*)me + 284);
	int idx1 = idx / 32;
	int idx2 = idx % 32;
	if (!p [idx1])
		return 0;
	if (value != p[idx1][idx2])
		return 0;
	return 1;
}

static void
setup_tls_access (void)
{
	guint32 ptk;
	guint32 *ins, *code;
	guint32 cmplwi_1023, li_0x48, blr_ins;
	if (tls_mode == TLS_MODE_FAILED)
		return;

	if (g_getenv ("MONO_NO_TLS")) {
		tls_mode = TLS_MODE_FAILED;
		return;
	}

	if (tls_mode == TLS_MODE_DETECT) {
		ins = (guint32*)pthread_getspecific;
		/* uncond branch to the real method */
		if ((*ins >> 26) == 18) {
			gint32 val;
			val = (*ins & ~3) << 6;
			val >>= 6;
			if (*ins & 2) {
				/* absolute */
				ins = (guint32*)val;
			} else {
				ins = (guint32*) ((char*)ins + val);
			}
		}
		code = &cmplwi_1023;
		ppc_cmpli (code, 0, 0, ppc_r3, 1023);
		code = &li_0x48;
		ppc_li (code, ppc_r4, 0x48);
		code = &blr_ins;
		ppc_blr (code);
		if (*ins == cmplwi_1023) {
			int found_lwz_284 = 0;
			for (ptk = 0; ptk < 20; ++ptk) {
				++ins;
				if (!*ins || *ins == blr_ins)
					break;
				if ((guint16)*ins == 284 && (*ins >> 26) == 32) {
					found_lwz_284 = 1;
					break;
				}
			}
			if (!found_lwz_284) {
				tls_mode = TLS_MODE_FAILED;
				return;
			}
			tls_mode = TLS_MODE_LTHREADS;
		} else if (*ins == li_0x48) {
			++ins;
			/* uncond branch to the real method */
			if ((*ins >> 26) == 18) {
				gint32 val;
				val = (*ins & ~3) << 6;
				val >>= 6;
				if (*ins & 2) {
					/* absolute */
					ins = (guint32*)val;
				} else {
					ins = (guint32*) ((char*)ins + val);
				}
				code = &val;
				ppc_li (code, ppc_r0, 0x7FF2);
				if (ins [1] == val) {
					/* Darwin on G4, implement */
					tls_mode = TLS_MODE_FAILED;
					return;
				} else {
					code = &val;
					ppc_mfspr (code, ppc_r3, 104);
					if (ins [1] != val) {
						tls_mode = TLS_MODE_FAILED;
						return;
					}
					tls_mode = TLS_MODE_DARWIN_G5;
				}
			} else {
				tls_mode = TLS_MODE_FAILED;
				return;
			}
		} else {
			tls_mode = TLS_MODE_FAILED;
			return;
		}
	}
	if (monodomain_key == -1) {
		ptk = mono_domain_get_tls_key ();
		if (ptk < 1024) {
			ptk = mono_pthread_key_for_tls (ptk);
			if (ptk < 1024) {
				monodomain_key = ptk;
			}
		}
	}
	if (lmf_pthread_key == -1) {
		ptk = mono_pthread_key_for_tls (mono_jit_tls_id);
		if (ptk < 1024) {
			/*g_print ("MonoLMF at: %d\n", ptk);*/
			/*if (!try_offset_access (mono_get_lmf_addr (), ptk)) {
				init_tls_failed = 1;
				return;
			}*/
			lmf_pthread_key = ptk;
		}
	}
	if (monothread_key == -1) {
		ptk = mono_thread_get_tls_key ();
		if (ptk < 1024) {
			ptk = mono_pthread_key_for_tls (ptk);
			if (ptk < 1024) {
				monothread_key = ptk;
				/*g_print ("thread inited: %d\n", ptk);*/
			}
		} else {
			/*g_print ("thread not inited yet %d\n", ptk);*/
		}
	}
}

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
	setup_tls_access ();
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
		this->dreg = mono_regstate_next_int (cfg->rs);
		mono_bblock_add_inst (cfg->cbb, this);
		mono_call_inst_add_outarg_reg (cfg, inst, this->dreg, this_dreg, FALSE);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_SETREG);
		vtarg->type = STACK_MP;
		vtarg->sreg1 = vt_reg;
		vtarg->dreg = mono_regstate_next_int (cfg->rs);
		mono_bblock_add_inst (cfg->cbb, vtarg);
		mono_call_inst_add_outarg_reg (cfg, inst, vtarg->dreg, ppc_r3, FALSE);
	}
}

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	if (cmethod->klass == mono_defaults.thread_class &&
			strcmp (cmethod->name, "MemoryBarrier") == 0) {
		MONO_INST_NEW (cfg, ins, OP_MEMORY_BARRIER);
	}
	/*if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sqrt") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SQRT);
			ins->inst_i0 = args [0];
		}
	}*/
	return ins;
}

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	return 0;
}

MonoInst* mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
	MonoInst* ins;

	setup_tls_access ();
	if (monodomain_key == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = monodomain_key;
	return ins;
}

MonoInst* 
mono_arch_get_thread_intrinsic (MonoCompile* cfg)
{
	MonoInst* ins;

	setup_tls_access ();
	if (monothread_key == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = monothread_key;
	return ins;
}

