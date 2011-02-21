/*
 * mini-ia64.c: IA64 backend for the Mono code generator
 *
 * Authors:
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>
#include <math.h>
#include <unistd.h>
#include <sys/mman.h>

#ifdef __INTEL_COMPILER
#include <ia64intrin.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-math.h>

#include "trace.h"
#include "mini-ia64.h"
#include "cpu-ia64.h"
#include "jit-icalls.h"
#include "ir-emit.h"

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define IS_IMM32(val) ((((guint64)val) >> 32) == 0)

/*
 * IA64 register usage:
 * - local registers are used for global register allocation
 * - r8..r11, r14..r30 is used for local register allocation
 * - r31 is a scratch register used within opcode implementations
 * - FIXME: Use out registers as well
 * - the first three locals are used for saving ar.pfst, b0, and sp
 * - compare instructions allways set p6 and p7
 */

/*
 * There are a lot of places where generated code is disassembled/patched.
 * The automatic bundling of instructions done by the code generation macros
 * could complicate things, so it is best to call 
 * ia64_codegen_set_one_ins_per_bundle () at those places.
 */

#define ARGS_OFFSET 16

#define GP_SCRATCH_REG 31
#define GP_SCRATCH_REG2 30
#define FP_SCRATCH_REG 32
#define FP_SCRATCH_REG2 33

#define LOOP_ALIGNMENT 8
#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

static const char* gregs [] = {
	"r0", "r1", "r2", "r3", "r4", "r5", "r6", "r7", "r8", "r9",
	"r10", "r11", "r12", "r13", "r14", "r15", "r16", "r17", "r18", "r19",
	"r20", "r21", "r22", "r23", "r24", "r25", "r26", "r27", "r28", "r29",
	"r30", "r31", "r32", "r33", "r34", "r35", "r36", "r37", "r38", "r39",
	"r40", "r41", "r42", "r43", "r44", "r45", "r46", "r47", "r48", "r49",
	"r50", "r51", "r52", "r53", "r54", "r55", "r56", "r57", "r58", "r59",
	"r60", "r61", "r62", "r63", "r64", "r65", "r66", "r67", "r68", "r69",
	"r70", "r71", "r72", "r73", "r74", "r75", "r76", "r77", "r78", "r79",
	"r80", "r81", "r82", "r83", "r84", "r85", "r86", "r87", "r88", "r89",
	"r90", "r91", "r92", "r93", "r94", "r95", "r96", "r97", "r98", "r99",
	"r100", "r101", "r102", "r103", "r104", "r105", "r106", "r107", "r108", "r109",
	"r110", "r111", "r112", "r113", "r114", "r115", "r116", "r117", "r118", "r119",
	"r120", "r121", "r122", "r123", "r124", "r125", "r126", "r127"
};

const char*
mono_arch_regname (int reg)
{
	if (reg < 128)
		return gregs [reg];
	else
		return "unknown";
}

static const char* fregs [] = {
	"f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9",
	"f10", "f11", "f12", "f13", "f14", "f15", "f16", "f17", "f18", "f19",
	"f20", "f21", "f22", "f23", "f24", "f25", "f26", "f27", "f28", "f29",
	"f30", "f31", "f32", "f33", "f34", "f35", "f36", "f37", "f38", "f39",
	"f40", "f41", "f42", "f43", "f44", "f45", "f46", "f47", "f48", "f49",
	"f50", "f51", "f52", "f53", "f54", "f55", "f56", "f57", "f58", "f59",
	"f60", "f61", "f62", "f63", "f64", "f65", "f66", "f67", "f68", "f69",
	"f70", "f71", "f72", "f73", "f74", "f75", "f76", "f77", "f78", "f79",
	"f80", "f81", "f82", "f83", "f84", "f85", "f86", "f87", "f88", "f89",
	"f90", "f91", "f92", "f93", "f94", "f95", "f96", "f97", "f98", "f99",
	"f100", "f101", "f102", "f103", "f104", "f105", "f106", "f107", "f108", "f109",
	"f110", "f111", "f112", "f113", "f114", "f115", "f116", "f117", "f118", "f119",
	"f120", "f121", "f122", "f123", "f124", "f125", "f126", "f127"
};

const char*
mono_arch_fregname (int reg)
{
	if (reg < 128)
		return fregs [reg];
	else
		return "unknown";
}

G_GNUC_UNUSED static void
break_count (void)
{
}

G_GNUC_UNUSED static gboolean
debug_count (void)
{
	static int count = 0;
	count ++;

	if (count == atoi (getenv ("COUNT"))) {
		break_count ();
	}

	if (count > atoi (getenv ("COUNT"))) {
		return FALSE;
	}

	return TRUE;
}

static gboolean
debug_ins_sched (void)
{
#if 0
	return debug_count ();
#else
	return TRUE;
#endif
}

static gboolean
debug_omit_fp (void)
{
#if 0
	return debug_count ();
#else
	return TRUE;
#endif
}

static void 
ia64_patch (unsigned char* code, gpointer target);

typedef enum {
	ArgInIReg,
	ArgInFloatReg,
	ArgInFloatRegR4,
	ArgOnStack,
	ArgValuetypeAddrInIReg,
	ArgAggregate,
	ArgSingleHFA,
	ArgDoubleHFA,
	ArgNone
} ArgStorage;

typedef enum {
	AggregateNormal,
	AggregateSingleHFA,
	AggregateDoubleHFA
} AggregateType;

typedef struct {
	gint16 offset;
	gint8  reg;
	ArgStorage storage;

	/* Only if storage == ArgAggregate */
	int nregs, nslots;
	AggregateType atype;
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	guint32 freg_usage;
	gboolean need_stack_align;
	gboolean vtype_retaddr;
	/* The index of the vret arg in the argument list */
	int vret_arg_index;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a) if (cfg->verbose_level > 1) a

#define PARAM_REGS 8

static void inline
add_general (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo)
{
    ainfo->offset = *stack_size;

    if (*gr >= PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		(*stack_size) += sizeof (gpointer);
    }
    else {
		ainfo->storage = ArgInIReg;
		ainfo->reg = *gr;
		*(gr) += 1;
    }
}

#define FLOAT_PARAM_REGS 8

static void inline
add_float (guint32 *gr, guint32 *fr, guint32 *stack_size, ArgInfo *ainfo, gboolean is_double)
{
    ainfo->offset = *stack_size;

    if (*gr >= PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		(*stack_size) += sizeof (gpointer);
    }
    else {
		ainfo->storage = is_double ? ArgInFloatReg : ArgInFloatRegR4;
		ainfo->reg = 8 + *fr;
		(*fr) += 1;
		(*gr) += 1;
    }
}

static void
add_valuetype (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, ArgInfo *ainfo, MonoType *type,
	       gboolean is_return,
	       guint32 *gr, guint32 *fr, guint32 *stack_size)
{
	guint32 size, i;
	MonoClass *klass;
	MonoMarshalType *info;
	gboolean is_hfa = TRUE;
	guint32 hfa_type = 0;

	klass = mono_class_from_mono_type (type);
	if (type->type == MONO_TYPE_TYPEDBYREF)
		size = 3 * sizeof (gpointer);
	else if (sig->pinvoke) 
		size = mono_type_native_stack_size (&klass->byval_arg, NULL);
	else 
		size = mini_type_stack_size (gsctx, &klass->byval_arg, NULL);

	if (!sig->pinvoke || (size == 0)) {
		/* Allways pass in memory */
		ainfo->offset = *stack_size;
		*stack_size += ALIGN_TO (size, 8);
		ainfo->storage = ArgOnStack;

		return;
	}

	/* Determine whenever it is a HFA (Homogeneous Floating Point Aggregate) */
	info = mono_marshal_load_type_info (klass);
	g_assert (info);
	for (i = 0; i < info->num_fields; ++i) {
		guint32 ftype = info->fields [i].field->type->type;
		if (!(info->fields [i].field->type->byref) && 
			((ftype == MONO_TYPE_R4) || (ftype == MONO_TYPE_R8))) {
			if (hfa_type == 0)
				hfa_type = ftype;
			else if (hfa_type != ftype)
				is_hfa = FALSE;
		}
		else
			is_hfa = FALSE;
	}
	if (hfa_type == 0)
		is_hfa = FALSE;

	ainfo->storage = ArgAggregate;
	ainfo->atype = AggregateNormal;

	if (is_hfa) {
		ainfo->atype = hfa_type == MONO_TYPE_R4 ? AggregateSingleHFA : AggregateDoubleHFA;
		if (is_return) {
			if (info->num_fields <= 8) {
				ainfo->reg = 8;
				ainfo->nregs = info->num_fields;
				ainfo->nslots = ainfo->nregs;
				return;
			}
			/* Fall through */
		}
		else {
			if ((*fr) + info->num_fields > 8)
				NOT_IMPLEMENTED;

			ainfo->reg = 8 + (*fr);
			ainfo->nregs = info->num_fields;
			ainfo->nslots = ainfo->nregs;
			(*fr) += info->num_fields;
			if (ainfo->atype == AggregateSingleHFA) {
				/*
				 * FIXME: Have to keep track of the parameter slot number, which is
				 * not the same as *gr.
				 */
				(*gr) += ALIGN_TO (info->num_fields, 2) / 2;
			} else {
				(*gr) += info->num_fields;
			}
			return;
		}
	}

	/* This also handles returning of TypedByRef used by some icalls */
	if (is_return) {
		if (size <= 32) {
			ainfo->reg = IA64_R8;
			ainfo->nregs = (size + 7) / 8;
			ainfo->nslots = ainfo->nregs;
			return;
		}
		NOT_IMPLEMENTED;
	}

	ainfo->reg = (*gr);
	ainfo->offset = *stack_size;
	ainfo->nslots = (size + 7) / 8;

	if (((*gr) + ainfo->nslots) <= 8) {
		/* Fits entirely in registers */
		ainfo->nregs = ainfo->nslots;
		(*gr) += ainfo->nregs;
		return;
	}

	ainfo->nregs = 8 - (*gr);
	(*gr) = 8;
	(*stack_size) += (ainfo->nslots - ainfo->nregs) * 8;
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * For IA64, see the "Itanium Software Conventions and Runtime Architecture
 * Gude" document for more information.
 */
static CallInfo*
get_call_info (MonoCompile *cfg, MonoMemPool *mp, MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i, gr, fr, pstart;
	MonoType *ret_type;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;
	MonoGenericSharingContext *gsctx = cfg ? cfg->generic_sharing_context : NULL;

	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	gr = 0;
	fr = 0;

	/* return value */
	{
		ret_type = mono_type_get_underlying_type (sig->ret);
		ret_type = mini_get_basic_type_from_generic (gsctx, ret_type);
		switch (ret_type->type) {
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
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = IA64_R8;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = IA64_R8;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.storage = ArgInFloatReg;
			cinfo->ret.reg = 8;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ret_type)) {
				cinfo->ret.storage = ArgInIReg;
				cinfo->ret.reg = IA64_R8;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_TYPEDBYREF: {
			guint32 tmp_gr = 0, tmp_fr = 0, tmp_stacksize = 0;

			if (sig->ret->byref) {
				/* This seems to happen with ldfld wrappers */
				cinfo->ret.storage = ArgInIReg;
			} else {
				add_valuetype (gsctx, sig, &cinfo->ret, sig->ret, TRUE, &tmp_gr, &tmp_fr, &tmp_stacksize);
				if (cinfo->ret.storage == ArgOnStack) {
					/* The caller passes the address where the value is stored */
					cinfo->vtype_retaddr = TRUE;
				}
			}
			break;
		}
		case MONO_TYPE_VOID:
			cinfo->ret.storage = ArgNone;
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	pstart = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (cinfo->vtype_retaddr && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_type_get_underlying_type (gsctx, sig->params [0]))))) {
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0);
		} else {
			add_general (&gr, &stack_size, &cinfo->args [sig->hasthis + 0]);
			pstart = 1;
		}
		add_general (&gr, &stack_size, &cinfo->ret);
		if (cinfo->ret.storage == ArgInIReg)
			cinfo->ret.storage = ArgValuetypeAddrInIReg;
		cinfo->vret_arg_index = 1;
	} else {
		/* this */
		if (sig->hasthis)
			add_general (&gr, &stack_size, cinfo->args + 0);

		if (cinfo->vtype_retaddr) {
			add_general (&gr, &stack_size, &cinfo->ret);
			if (cinfo->ret.storage == ArgInIReg)
				cinfo->ret.storage = ArgValuetypeAddrInIReg;
		}
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == 0)) {
		gr = PARAM_REGS;
		fr = FLOAT_PARAM_REGS;
		
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie);
	}

	for (i = pstart; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];
		MonoType *ptype;

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* We allways pass the sig cookie on the stack for simplicity */
			/* 
			 * Prevent implicit arguments + the sig cookie from being passed 
			 * in registers.
			 */
			gr = PARAM_REGS;
			fr = FLOAT_PARAM_REGS;

			/* Emit the signature cookie just before the implicit arguments */
			add_general (&gr, &stack_size, &cinfo->sig_cookie);
		}

		if (sig->params [i]->byref) {
			add_general (&gr, &stack_size, ainfo);
			continue;
		}
		ptype = mono_type_get_underlying_type (sig->params [i]);
		ptype = mini_get_basic_type_from_generic (gsctx, ptype);
		switch (ptype->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			add_general (&gr, &stack_size, ainfo);
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
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ptype)) {
				add_general (&gr, &stack_size, ainfo);
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_TYPEDBYREF:
			/* FIXME: */
			/* We allways pass valuetypes on the stack */
			add_valuetype (gsctx, sig, ainfo, sig->params [i], FALSE, &gr, &fr, &stack_size);
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			add_general (&gr, &stack_size, ainfo);
			break;
		case MONO_TYPE_R4:
			add_float (&gr, &fr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_R8:
			add_float (&gr, &fr, &stack_size, ainfo, TRUE);
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n > 0) && (sig->sentinelpos == sig->param_count)) {
		gr = PARAM_REGS;
		fr = FLOAT_PARAM_REGS;
		
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie);
	}

	cinfo->stack_usage = stack_size;
	cinfo->reg_usage = gr;
	cinfo->freg_usage = fr;
	return cinfo;
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
 * Returns the size of the argument area on the stack.
 */
int
mono_arch_get_argument_info (MonoMethodSignature *csig, int param_count, MonoJitArgumentInfo *arg_info)
{
	int k;
	CallInfo *cinfo = get_call_info (NULL, NULL, csig, FALSE);
	guint32 args_size = cinfo->stack_usage;

	/* The arguments are saved to a stack area in mono_arch_instrument_prolog */
	if (csig->hasthis) {
		arg_info [0].offset = 0;
	}

	for (k = 0; k < param_count; k++) {
		arg_info [k + 1].offset = ((k + csig->hasthis) * 8);
		/* FIXME: */
		arg_info [k + 1].size = 0;
	}

	g_free (cinfo);

	return args_size;
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	*exclude_mask = 0;

	return 0;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	CallInfo *cinfo;

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		MonoInst *ins = cfg->args [i];

		ArgInfo *ainfo = &cinfo->args [i];

		if (ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT))
			continue;

		if (ainfo->storage == ArgInIReg) {
			/* The input registers are non-volatile */
			ins->opcode = OP_REGVAR;
			ins->dreg = 32 + ainfo->reg;
		}
	}

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		if (mono_is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

static void
mono_ia64_alloc_stacked_registers (MonoCompile *cfg)
{
	CallInfo *cinfo;
	guint32 reserved_regs;
	MonoMethodHeader *header;

	if (cfg->arch.reg_local0 > 0)
		/* Already done */
		return;

	cinfo = get_call_info (cfg, cfg->mempool, mono_method_signature (cfg->method), FALSE);

	header = cfg->header;
	
	/* Some registers are reserved for use by the prolog/epilog */
	reserved_regs = header->num_clauses ? 4 : 3;

	if ((mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)) ||
		(cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)) {
		/* One registers is needed by instrument_epilog to save the return value */
		reserved_regs ++;
		if (cinfo->reg_usage < 2)
			/* Number of arguments passed to function call in instrument_prolog */
			cinfo->reg_usage = 2;
	}

	cfg->arch.reg_in0 = 32;
	cfg->arch.reg_local0 = cfg->arch.reg_in0 + cinfo->reg_usage + reserved_regs;
	cfg->arch.reg_out0 = cfg->arch.reg_local0 + 16;

	cfg->arch.reg_saved_ar_pfs = cfg->arch.reg_local0 - 1;
	cfg->arch.reg_saved_b0 = cfg->arch.reg_local0 - 2;
	cfg->arch.reg_fp = cfg->arch.reg_local0 - 3;

	/* 
	 * Frames without handlers save sp to fp, frames with handlers save it into
	 * a dedicated register.
	 */
	if (header->num_clauses)
		cfg->arch.reg_saved_sp = cfg->arch.reg_local0 - 4;
	else
		cfg->arch.reg_saved_sp = cfg->arch.reg_fp;

	if ((mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)) ||
		(cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)) {
		cfg->arch.reg_saved_return_val = cfg->arch.reg_local0 - reserved_regs;
	}

	/* 
	 * Need to allocate at least 2 out register for use by OP_THROW / the system
	 * exception throwing code.
	 */
	cfg->arch.n_out_regs = MAX (cfg->arch.n_out_regs, 2);
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i;

	mono_ia64_alloc_stacked_registers (cfg);

	for (i = cfg->arch.reg_local0; i < cfg->arch.reg_out0; ++i) {
		/* FIXME: regmask */
		g_assert (i < 64);
		regs = g_list_prepend (regs, (gpointer)(gssize)(i));
	}

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
	/* FIXME: Increase costs linearly to avoid using all local registers */

	return 0;
}
 
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset;
	guint32 locals_stack_size, locals_stack_align;
	gint32 *offsets;
	CallInfo *cinfo;

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);

	/*
	 * Determine whenever the frame pointer can be eliminated.
	 * FIXME: Remove some of the restrictions.
	 */
	cfg->arch.omit_fp = TRUE;

	if (!debug_omit_fp ())
		cfg->arch.omit_fp = FALSE;

	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		cfg->arch.omit_fp = FALSE;
	if (header->num_clauses)
		cfg->arch.omit_fp = FALSE;
	if (cfg->param_area)
		cfg->arch.omit_fp = FALSE;
	if ((sig->ret->type != MONO_TYPE_VOID) && (cinfo->ret.storage == ArgAggregate))
		cfg->arch.omit_fp = FALSE;
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
		cfg->arch.omit_fp = FALSE;
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];

		if (ainfo->storage == ArgOnStack) {
			/* 
			 * The stack offset can only be determined when the frame
			 * size is known.
			 */
			cfg->arch.omit_fp = FALSE;
		}
	}

	mono_ia64_alloc_stacked_registers (cfg);

	/*
	 * We use the ABI calling conventions for managed code as well.
	 * Exception: valuetypes are never passed or returned in registers.
	 */

	if (cfg->arch.omit_fp) {
		cfg->flags |= MONO_CFG_HAS_SPILLUP;
		cfg->frame_reg = IA64_SP;
		offset = ARGS_OFFSET;
	}
	else {
		/* Locals are allocated backwards from %fp */
		cfg->frame_reg = cfg->arch.reg_fp;
		offset = 0;
	}

	if (cfg->method->save_lmf) {
		/* No LMF on IA64 */
	}

	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = cinfo->ret.reg;
			break;
		case ArgInFloatReg:
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = cinfo->ret.reg;
			break;
		case ArgValuetypeAddrInIReg:
			cfg->vret_addr->opcode = OP_REGVAR;
			cfg->vret_addr->dreg = cfg->arch.reg_in0 + cinfo->ret.reg;
			break;
		case ArgAggregate:
			/* Allocate a local to hold the result, the epilog will copy it to the correct place */
			if (cfg->arch.omit_fp)
				g_assert_not_reached ();
			offset = ALIGN_TO (offset, 8);
			offset += cinfo->ret.nslots * 8;
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = cfg->frame_reg;
			cfg->ret->inst_offset = - offset;
			break;
		default:
			g_assert_not_reached ();
		}
		cfg->ret->dreg = cfg->ret->inst_c0;
	}

	/* Allocate locals */
	offsets = mono_allocate_stack_slots_full (cfg, cfg->arch.omit_fp ? FALSE : TRUE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align) {
		offset = ALIGN_TO (offset, locals_stack_align);
	}
	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		if (offsets [i] != -1) {
			MonoInst *inst = cfg->varinfo [i];
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = cfg->frame_reg;
			if (cfg->arch.omit_fp)
				inst->inst_offset = (offset + offsets [i]);
			else
				inst->inst_offset = - (offset + offsets [i]);
			// printf ("allocated local %d to ", i); mono_print_tree_nl (inst);
		}
	}
	offset += locals_stack_size;

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG)) {
		if (cfg->arch.omit_fp)
			g_assert_not_reached ();
		g_assert (cinfo->sig_cookie.storage == ArgOnStack);
		cfg->sig_cookie = cinfo->sig_cookie.offset + ARGS_OFFSET;
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		inst = cfg->args [i];
		if (inst->opcode != OP_REGVAR) {
			ArgInfo *ainfo = &cinfo->args [i];
			gboolean inreg = TRUE;
			MonoType *arg_type;

			if (sig->hasthis && (i == 0))
				arg_type = &mono_defaults.object_class->byval_arg;
			else
				arg_type = sig->params [i - sig->hasthis];

			/* FIXME: VOLATILE is only set if the liveness pass runs */
			if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
				inreg = FALSE;

			inst->opcode = OP_REGOFFSET;

			switch (ainfo->storage) {
			case ArgInIReg:
				inst->opcode = OP_REGVAR;
				inst->dreg = cfg->arch.reg_in0 + ainfo->reg;
				break;
			case ArgInFloatReg:
			case ArgInFloatRegR4:
				/* 
				 * Since float regs are volatile, we save the arguments to
				 * the stack in the prolog.
				 */
				inreg = FALSE;
				break;
			case ArgOnStack:
				if (cfg->arch.omit_fp)
					g_assert_not_reached ();
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = cfg->frame_reg;
				inst->inst_offset = ARGS_OFFSET + ainfo->offset;
				break;
			case ArgAggregate:
				inreg = FALSE;
				break;
			default:
				NOT_IMPLEMENTED;
			}

			if (!inreg && (ainfo->storage != ArgOnStack)) {
				guint32 size = 0;

				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = cfg->frame_reg;
				/* These arguments are saved to the stack in the prolog */
				switch (ainfo->storage) {
				case ArgAggregate:
					if (ainfo->atype == AggregateSingleHFA)
						size = ainfo->nslots * 4;
					else
						size = ainfo->nslots * 8;
					break;
				default:
					size = sizeof (gpointer);
					break;
				}

				offset = ALIGN_TO (offset, sizeof (gpointer));

				if (cfg->arch.omit_fp) {
					inst->inst_offset = offset;
					offset += size;
				} else {
					offset += size;
					inst->inst_offset = - offset;
				}
			}
		}
	}

	/* 
	 * FIXME: This doesn't work because some variables are allocated during local
	 * regalloc.
	 */
	/*
	if (cfg->arch.omit_fp && offset == 16)
		offset = 0;
	*/

	cfg->stack_offset = offset;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature (cfg->method);

	cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);

	if (cinfo->ret.storage == ArgAggregate)
		cfg->ret_var_is_local = TRUE;
	if (cinfo->ret.storage == ArgValuetypeAddrInIReg) {
		cfg->vret_addr = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, MonoInst *tree)
{
	MonoInst *arg;

	MONO_INST_NEW (cfg, arg, OP_NOP);
	arg->sreg1 = tree->dreg;

	switch (storage) {
	case ArgInIReg:
		arg->opcode = OP_MOVE;
		arg->dreg = mono_alloc_ireg (cfg);

		mono_call_inst_add_outarg_reg (cfg, call, arg->dreg, reg, FALSE);
		break;
	case ArgInFloatReg:
		arg->opcode = OP_FMOVE;
		arg->dreg = mono_alloc_freg (cfg);

		mono_call_inst_add_outarg_reg (cfg, call, arg->dreg, reg, TRUE);
		break;
	case ArgInFloatRegR4:
		arg->opcode = OP_FCONV_TO_R4;
		arg->dreg = mono_alloc_freg (cfg);

		mono_call_inst_add_outarg_reg (cfg, call, arg->dreg, reg, TRUE);
		break;
	default:
		g_assert_not_reached ();
	}

	MONO_ADD_INS (cfg->cbb, arg);
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;

	/* Emit the signature cookie just before the implicit arguments */
	MonoInst *sig_arg;
	/* FIXME: Add support for signature tokens to AOT */
	cfg->disable_aot = TRUE;

	g_assert (cinfo->sig_cookie.storage == ArgOnStack);

	/*
	 * mono_ArgIterator_Setup assumes the signature cookie is 
	 * passed first and all the arguments which were before it are
	 * passed on the stack after the signature. So compensate by 
	 * passing a different signature.
	 */
	tmp_sig = mono_metadata_signature_dup (call->signature);
	tmp_sig->param_count -= call->signature->sentinelpos;
	tmp_sig->sentinelpos = 0;
	memcpy (tmp_sig->params, call->signature->params + call->signature->sentinelpos, tmp_sig->param_count * sizeof (MonoType*));

	MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
	sig_arg->dreg = mono_alloc_ireg (cfg);
	sig_arg->inst_p0 = tmp_sig;
	MONO_ADD_INS (cfg->cbb, sig_arg);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, IA64_SP, 16 + cinfo->sig_cookie.offset, sig_arg->dreg);
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *in;
	MonoMethodSignature *sig;
	int i, n, stack_size;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	stack_size = 0;

	mono_ia64_alloc_stacked_registers (cfg);

	sig = call->signature;
	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg, cfg->mempool, sig, sig->pinvoke);

	if (cinfo->ret.storage == ArgAggregate) {
		MonoInst *vtarg;
		MonoInst *local;

		/* 
		 * The valuetype is in registers after the call, need to be copied 
		 * to the stack. Save the address to a local here, so the call 
		 * instruction can access it.
		 */
		local = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
		local->flags |= MONO_INST_VOLATILE;
		cfg->arch.ret_var_addr_local = local;

		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->sreg1 = call->vret_var->dreg;
		vtarg->dreg = local->dreg;
		MONO_ADD_INS (cfg->cbb, vtarg);
	}

	if (cinfo->ret.storage == ArgValuetypeAddrInIReg) {
		add_outarg_reg (cfg, call, ArgInIReg, cfg->arch.reg_out0 + cinfo->ret.reg, call->vret_var);
	}

	for (i = 0; i < n; ++i) {
		MonoType *arg_type;

		ainfo = cinfo->args + i;

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
		}

		in = call->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(arg_type))) {
			guint32 align;
			guint32 size;

			if (arg_type->type == MONO_TYPE_TYPEDBYREF) {
				size = sizeof (MonoTypedRef);
				align = sizeof (gpointer);
			}
			else if (sig->pinvoke)
				size = mono_type_native_stack_size (&in->klass->byval_arg, &align);
			else {
				/* 
				 * Other backends use mono_type_stack_size (), but that
				 * aligns the size to 8, which is larger than the size of
				 * the source, leading to reads of invalid memory if the
				 * source is at the end of address space.
				 */
				size = mono_class_value_size (in->klass, &align);
			}

			if (size > 0) {
				MonoInst *arg;

				MONO_INST_NEW (cfg, arg, OP_OUTARG_VT);
				arg->sreg1 = in->dreg;
				arg->klass = in->klass;
				arg->backend.size = size;
				arg->inst_p0 = call;
				arg->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
				memcpy (arg->inst_p1, ainfo, sizeof (ArgInfo));

				MONO_ADD_INS (cfg->cbb, arg);
			}
		}
		else {
			switch (ainfo->storage) {
			case ArgInIReg:
				add_outarg_reg (cfg, call, ainfo->storage, cfg->arch.reg_out0 + ainfo->reg, in);
				break;
			case ArgInFloatReg:
			case ArgInFloatRegR4:
				add_outarg_reg (cfg, call, ainfo->storage, ainfo->reg, in);
				break;
			case ArgOnStack:
				if (arg_type->type == MONO_TYPE_R4 && !arg_type->byref)
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, IA64_SP, 16 + ainfo->offset, in->dreg);
				else if (arg_type->type == MONO_TYPE_R8 && !arg_type->byref)
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, IA64_SP, 16 + ainfo->offset, in->dreg);
				else
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, IA64_SP, 16 + ainfo->offset, in->dreg);
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos)) {
		emit_sig_cookie (cfg, call, cinfo);
	}

	call->stack_usage = cinfo->stack_usage;
	cfg->arch.n_out_regs = MAX (cfg->arch.n_out_regs, cinfo->reg_usage);
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int size = ins->backend.size;

	if (ainfo->storage == ArgAggregate) {
		MonoInst *load, *store;
		int i, slot;

		/* 
		 * Part of the structure is passed in registers.
		 */
		for (i = 0; i < ainfo->nregs; ++i) {
			slot = ainfo->reg + i;
			
			if (ainfo->atype == AggregateSingleHFA) {
				MONO_INST_NEW (cfg, load, OP_LOADR4_MEMBASE);
				load->inst_basereg = src->dreg;
				load->inst_offset = i * 4;
				load->dreg = mono_alloc_freg (cfg);

				mono_call_inst_add_outarg_reg (cfg, call, load->dreg, ainfo->reg + i, TRUE);
			} else if (ainfo->atype == AggregateDoubleHFA) {
				MONO_INST_NEW (cfg, load, OP_LOADR8_MEMBASE);
				load->inst_basereg = src->dreg;
				load->inst_offset = i * 8;
				load->dreg = mono_alloc_freg (cfg);

				mono_call_inst_add_outarg_reg (cfg, call, load->dreg, ainfo->reg + i, TRUE);
			} else {
				MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
				load->inst_basereg = src->dreg;
				load->inst_offset = i * 8;
				load->dreg = mono_alloc_ireg (cfg);

				mono_call_inst_add_outarg_reg (cfg, call, load->dreg, cfg->arch.reg_out0 + ainfo->reg + i, FALSE);
			}
			MONO_ADD_INS (cfg->cbb, load);
		}

		/* 
		 * Part of the structure is passed on the stack.
		 */
		for (i = ainfo->nregs; i < ainfo->nslots; ++i) {
			slot = ainfo->reg + i;

			MONO_INST_NEW (cfg, load, OP_LOADI8_MEMBASE);
			load->inst_basereg = src->dreg;
			load->inst_offset = i * sizeof (gpointer);
			load->dreg = mono_alloc_preg (cfg);
			MONO_ADD_INS (cfg->cbb, load);

			MONO_INST_NEW (cfg, store, OP_STOREI8_MEMBASE_REG);
			store->sreg1 = load->dreg;
			store->inst_destbasereg = IA64_SP;
			store->inst_offset = 16 + ainfo->offset + (slot - 8) * 8;
			MONO_ADD_INS (cfg->cbb, store);
		}
	} else {
		mini_emit_memcpy (cfg, IA64_SP, 16 + ainfo->offset, src->dreg, 0, size, 4);
	}
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	CallInfo *cinfo = get_call_info (cfg, cfg->mempool, mono_method_signature (method), FALSE);

	switch (cinfo->ret.storage) {
	case ArgInIReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
		break;
	case ArgInFloatReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		break;
	default:
		g_assert_not_reached ();
	}
}

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *last_ins = NULL;
	ins = bb->code;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_MOVE:
		case OP_FMOVE:
			/*
			 * Removes:
			 *
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			/* 
			 * Removes:
			 *
			 * OP_MOVE sreg, dreg 
			 * OP_MOVE dreg, sreg
			 */
			if (last_ins && last_ins->opcode == OP_MOVE &&
			    ins->sreg1 == last_ins->dreg &&
			    ins->dreg == last_ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			break;
		case OP_MUL_IMM: 
		case OP_IMUL_IMM: 
			/* remove unnecessary multiplication with 1 */
			if (ins->inst_imm == 1) {
				if (ins->dreg != ins->sreg1) {
					ins->opcode = OP_MOVE;
				} else {
					MONO_DELETE_INS (bb, ins);
					continue;
				}
			}
			break;
		}

		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

int cond_to_ia64_cmp [][3] = {
	{OP_IA64_CMP_EQ, OP_IA64_CMP4_EQ, OP_IA64_FCMP_EQ},
	{OP_IA64_CMP_NE, OP_IA64_CMP4_NE, OP_IA64_FCMP_NE},
	{OP_IA64_CMP_LE, OP_IA64_CMP4_LE, OP_IA64_FCMP_LE},
	{OP_IA64_CMP_GE, OP_IA64_CMP4_GE, OP_IA64_FCMP_GE},
	{OP_IA64_CMP_LT, OP_IA64_CMP4_LT, OP_IA64_FCMP_LT},
	{OP_IA64_CMP_GT, OP_IA64_CMP4_GT, OP_IA64_FCMP_GT},
	{OP_IA64_CMP_LE_UN, OP_IA64_CMP4_LE_UN, OP_IA64_FCMP_LE_UN},
	{OP_IA64_CMP_GE_UN, OP_IA64_CMP4_GE_UN, OP_IA64_FCMP_GE_UN},
	{OP_IA64_CMP_LT_UN, OP_IA64_CMP4_LT_UN, OP_IA64_FCMP_LT_UN},
	{OP_IA64_CMP_GT_UN, OP_IA64_CMP4_GT_UN, OP_IA64_FCMP_GT_UN}
};

static int
opcode_to_ia64_cmp (int opcode, int cmp_opcode)
{
	return cond_to_ia64_cmp [mono_opcode_to_cond (opcode)][mono_opcode_to_type (opcode, cmp_opcode)];
}

int cond_to_ia64_cmp_imm [][3] = {
	{OP_IA64_CMP_EQ_IMM, OP_IA64_CMP4_EQ_IMM, 0},
	{OP_IA64_CMP_NE_IMM, OP_IA64_CMP4_NE_IMM, 0},
	{OP_IA64_CMP_GE_IMM, OP_IA64_CMP4_GE_IMM, 0},
	{OP_IA64_CMP_LE_IMM, OP_IA64_CMP4_LE_IMM, 0},
	{OP_IA64_CMP_GT_IMM, OP_IA64_CMP4_GT_IMM, 0},
	{OP_IA64_CMP_LT_IMM, OP_IA64_CMP4_LT_IMM, 0},
	{OP_IA64_CMP_GE_UN_IMM, OP_IA64_CMP4_GE_UN_IMM, 0},
	{OP_IA64_CMP_LE_UN_IMM, OP_IA64_CMP4_LE_UN_IMM, 0},
	{OP_IA64_CMP_GT_UN_IMM, OP_IA64_CMP4_GT_UN_IMM, 0},
	{OP_IA64_CMP_LT_UN_IMM, OP_IA64_CMP4_LT_UN_IMM, 0},
};

static int
opcode_to_ia64_cmp_imm (int opcode, int cmp_opcode)
{
	/* The condition needs to be reversed */
	return cond_to_ia64_cmp_imm [mono_opcode_to_cond (opcode)][mono_opcode_to_type (opcode, cmp_opcode)];
}

#define NEW_INS(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = (op);	\
        mono_bblock_insert_after_ins (bb, last_ins, (dest)); \
        last_ins = (dest); \
	} while (0)

/*
 * mono_arch_lowering_pass:
 *
 *  Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *next, *temp, *temp2, *temp3, *last_ins = NULL;
	ins = bb->code;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM:
		case OP_STORE_MEMBASE_IMM:
			/* There are no store_membase instructions on ia64 */
			if (ins->inst_offset == 0) {
				temp2 = NULL;
			} else if (ia64_is_imm14 (ins->inst_offset)) {
				NEW_INS (cfg, temp2, OP_ADD_IMM);
				temp2->sreg1 = ins->inst_destbasereg;
				temp2->inst_imm = ins->inst_offset;
				temp2->dreg = mono_alloc_ireg (cfg);
			}
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);

				NEW_INS (cfg, temp2, OP_LADD);
				temp2->sreg1 = ins->inst_destbasereg;
				temp2->sreg2 = temp->dreg;
				temp2->dreg = mono_alloc_ireg (cfg);
			}

			switch (ins->opcode) {
			case OP_STOREI1_MEMBASE_IMM:
				ins->opcode = OP_STOREI1_MEMBASE_REG;
				break;
			case OP_STOREI2_MEMBASE_IMM:
				ins->opcode = OP_STOREI2_MEMBASE_REG;
				break;
			case OP_STOREI4_MEMBASE_IMM:
				ins->opcode = OP_STOREI4_MEMBASE_REG;
				break;
			case OP_STOREI8_MEMBASE_IMM:
			case OP_STORE_MEMBASE_IMM:
				ins->opcode = OP_STOREI8_MEMBASE_REG;
				break;
			default:
				g_assert_not_reached ();
			}

			if (ins->inst_imm == 0)
				ins->sreg1 = IA64_R0;
			else {
				NEW_INS (cfg, temp3, OP_I8CONST);
				temp3->inst_c0 = ins->inst_imm;
				temp3->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = temp3->dreg;
			}

			ins->inst_offset = 0;
			if (temp2)
				ins->inst_destbasereg = temp2->dreg;
			break;
		case OP_STOREI1_MEMBASE_REG:
		case OP_STOREI2_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
		case OP_STOREI8_MEMBASE_REG:
		case OP_STORER4_MEMBASE_REG:
		case OP_STORER8_MEMBASE_REG:
		case OP_STORE_MEMBASE_REG:
			/* There are no store_membase instructions on ia64 */
			if (ins->inst_offset == 0) {
				break;
			}
			else if (ia64_is_imm14 (ins->inst_offset)) {
				NEW_INS (cfg, temp2, OP_ADD_IMM);
				temp2->sreg1 = ins->inst_destbasereg;
				temp2->inst_imm = ins->inst_offset;
				temp2->dreg = mono_alloc_ireg (cfg);
			}
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);
				NEW_INS (cfg, temp2, OP_LADD);
				temp2->sreg1 = ins->inst_destbasereg;
				temp2->sreg2 = temp->dreg;
				temp2->dreg = mono_alloc_ireg (cfg);
			}

			ins->inst_offset = 0;
			ins->inst_destbasereg = temp2->dreg;
			break;
		case OP_LOADI1_MEMBASE:
		case OP_LOADU1_MEMBASE:
		case OP_LOADI2_MEMBASE:
		case OP_LOADU2_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
		case OP_LOADI8_MEMBASE:
		case OP_LOAD_MEMBASE:
		case OP_LOADR4_MEMBASE:
		case OP_LOADR8_MEMBASE:
		case OP_ATOMIC_EXCHANGE_I4:
		case OP_ATOMIC_EXCHANGE_I8:
		case OP_ATOMIC_ADD_NEW_I4:
		case OP_ATOMIC_ADD_NEW_I8:
		case OP_ATOMIC_ADD_IMM_NEW_I4:
		case OP_ATOMIC_ADD_IMM_NEW_I8:
			/* There are no membase instructions on ia64 */
			if (ins->inst_offset == 0) {
				break;
			}
			else if (ia64_is_imm14 (ins->inst_offset)) {
				NEW_INS (cfg, temp2, OP_ADD_IMM);
				temp2->sreg1 = ins->inst_basereg;
				temp2->inst_imm = ins->inst_offset;
				temp2->dreg = mono_alloc_ireg (cfg);
			}
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);
				NEW_INS (cfg, temp2, OP_LADD);
				temp2->sreg1 = ins->inst_basereg;
				temp2->sreg2 = temp->dreg;
				temp2->dreg = mono_alloc_ireg (cfg);
			}

			ins->inst_offset = 0;
			ins->inst_basereg = temp2->dreg;
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
		case OP_ISUB_IMM:
		case OP_LSUB_IMM:
		case OP_AND_IMM:
		case OP_IAND_IMM:
		case OP_LAND_IMM:
		case OP_IOR_IMM:
		case OP_LOR_IMM:
		case OP_IXOR_IMM:
		case OP_LXOR_IMM:
		case OP_SHL_IMM:
		case OP_SHR_IMM:
		case OP_ISHL_IMM:
		case OP_LSHL_IMM:
		case OP_ISHR_IMM:
		case OP_LSHR_IMM:
		case OP_ISHR_UN_IMM:
		case OP_LSHR_UN_IMM: {
			gboolean is_imm = FALSE;
			gboolean switched = FALSE;

			if (ins->opcode == OP_AND_IMM && ins->inst_imm == 255) {
				ins->opcode = OP_ZEXT_I1;
				break;
			}

			switch (ins->opcode) {
			case OP_ADD_IMM:
			case OP_IADD_IMM:
			case OP_LADD_IMM:
				is_imm = ia64_is_imm14 (ins->inst_imm);
				switched = TRUE;
				break;
			case OP_ISUB_IMM:
			case OP_LSUB_IMM:
				is_imm = ia64_is_imm14 (- (ins->inst_imm));
				if (is_imm) {
					/* A = B - IMM -> A = B + (-IMM) */
					ins->inst_imm = - ins->inst_imm;
					ins->opcode = OP_IADD_IMM;
				}
				switched = TRUE;
				break;
			case OP_IAND_IMM:
			case OP_IOR_IMM:
			case OP_IXOR_IMM:
			case OP_AND_IMM:
			case OP_LAND_IMM:
			case OP_LOR_IMM:
			case OP_LXOR_IMM:
				is_imm = ia64_is_imm8 (ins->inst_imm);
				switched = TRUE;
				break;
			case OP_SHL_IMM:
			case OP_SHR_IMM:
			case OP_ISHL_IMM:
			case OP_LSHL_IMM:
			case OP_ISHR_IMM:
			case OP_LSHR_IMM:
			case OP_ISHR_UN_IMM:
			case OP_LSHR_UN_IMM:
				is_imm = (ins->inst_imm >= 0) && (ins->inst_imm < 64);
				break;
			default:
				break;
			}

			if (is_imm) {
				if (switched)
					ins->sreg2 = ins->sreg1;
				break;
			}

			ins->opcode = mono_op_imm_to_op (ins->opcode);

			if (ins->inst_imm == 0)
				ins->sreg2 = IA64_R0;
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
			}
			break;
		}
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		case OP_LCOMPARE_IMM: {
			/* Instead of compare+b<cond>, ia64 has compare<cond>+br */
			gboolean imm;
			CompRelation cond;

			next = ins->next;

			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}

			/* 
			 * The compare_imm instructions have switched up arguments, and 
			 * some of them take an imm between -127 and 128.
			 */
			next = ins->next;
			cond = mono_opcode_to_cond (next->opcode);
			if ((cond == CMP_LT) || (cond == CMP_GE))
				imm = ia64_is_imm8 (ins->inst_imm - 1);
			else if ((cond == CMP_LT_UN) || (cond == CMP_GE_UN))
				imm = ia64_is_imm8 (ins->inst_imm - 1) && (ins->inst_imm > 0);
			else
				imm = ia64_is_imm8 (ins->inst_imm);

			if (imm) {
				ins->opcode = opcode_to_ia64_cmp_imm (next->opcode, ins->opcode);
				ins->sreg2 = ins->sreg1;
			}
			else {
				ins->opcode = opcode_to_ia64_cmp (next->opcode, ins->opcode);

				if (ins->inst_imm == 0)
					ins->sreg2 = IA64_R0;
				else {
					NEW_INS (cfg, temp, OP_I8CONST);
					temp->inst_c0 = ins->inst_imm;
					temp->dreg = mono_alloc_ireg (cfg);
					ins->sreg2 = temp->dreg;
				}
			}

			if (MONO_IS_COND_BRANCH_OP (next)) {
				next->opcode = OP_IA64_BR_COND;
				next->inst_target_bb = next->inst_true_bb;
			} else if (MONO_IS_COND_EXC (next)) {
				next->opcode = OP_IA64_COND_EXC;
			} else if (MONO_IS_SETCC (next)) {
				next->opcode = OP_IA64_CSET;
			} else {
				printf ("%s\n", mono_inst_name (next->opcode));
				NOT_IMPLEMENTED;
			}

			break;
		}
		case OP_COMPARE:
		case OP_ICOMPARE:
		case OP_LCOMPARE:
		case OP_FCOMPARE: {
			/* Instead of compare+b<cond>, ia64 has compare<cond>+br */

			next = ins->next;

			/* Branch opts can eliminate the branch */
			if (!next || (!(MONO_IS_COND_BRANCH_OP (next) || MONO_IS_COND_EXC (next) || MONO_IS_SETCC (next)))) {
				NULLIFY_INS (ins);
				break;
			}

			ins->opcode = opcode_to_ia64_cmp (next->opcode, ins->opcode);

			if (MONO_IS_COND_BRANCH_OP (next)) {
				next->opcode = OP_IA64_BR_COND;
				next->inst_target_bb = next->inst_true_bb;
			} else if (MONO_IS_COND_EXC (next)) {
				next->opcode = OP_IA64_COND_EXC;
			} else if (MONO_IS_SETCC (next)) {
				next->opcode = OP_IA64_CSET;
			} else {
				printf ("%s\n", mono_inst_name (next->opcode));
				NOT_IMPLEMENTED;
			}

			break;
		}
		case OP_FCEQ:
		case OP_FCGT:
		case OP_FCGT_UN:
		case OP_FCLT:
		case OP_FCLT_UN:
			/* The front end removes the fcompare, so introduce it again */
			NEW_INS (cfg, temp, opcode_to_ia64_cmp (ins->opcode, OP_FCOMPARE));
			temp->sreg1 = ins->sreg1;
			temp->sreg2 = ins->sreg2;
			
			ins->opcode = OP_IA64_CSET;
			MONO_INST_NULLIFY_SREGS (ins);
			break;
		case OP_MUL_IMM:
		case OP_LMUL_IMM:
		case OP_IMUL_IMM: {
			int i, sum_reg;
			gboolean found = FALSE;
			int shl_op = ins->opcode == OP_IMUL_IMM ? OP_ISHL_IMM : OP_SHL_IMM;

			/* First the easy cases */
			if (ins->inst_imm == 1) {
				ins->opcode = OP_MOVE;
				break;
			}
			for (i = 1; i < 64; ++i)
				if (ins->inst_imm == (((gint64)1) << i)) {
					ins->opcode = shl_op;
					ins->inst_imm = i;
					found = TRUE;
					break;
				}

			/* This could be optimized */
			if (!found) {
				sum_reg = 0;
				for (i = 0; i < 64; ++i) {
					if (ins->inst_imm & (((gint64)1) << i)) {
						NEW_INS (cfg, temp, shl_op);
						temp->dreg = mono_alloc_ireg (cfg);
						temp->sreg1 = ins->sreg1;
						temp->inst_imm = i;

						if (sum_reg == 0)
							sum_reg = temp->dreg;
						else {
							NEW_INS (cfg, temp2, OP_LADD);
							temp2->dreg = mono_alloc_ireg (cfg);
							temp2->sreg1 = sum_reg;
							temp2->sreg2 = temp->dreg;
							sum_reg = temp2->dreg;
						}
					}
				}
				ins->opcode = OP_MOVE;
				ins->sreg1 = sum_reg;
			}
			break;
		}
		case OP_LCONV_TO_OVF_U4:
			NEW_INS (cfg, temp, OP_IA64_CMP4_LT);
			temp->sreg1 = ins->sreg1;
			temp->sreg2 = IA64_R0;

			NEW_INS (cfg, temp, OP_IA64_COND_EXC);
			temp->inst_p1 = (char*)"OverflowException";

			ins->opcode = OP_MOVE;
			break;
		case OP_LCONV_TO_OVF_I4_UN:
			NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = 0x7fffffff;
			temp->dreg = mono_alloc_ireg (cfg);

			NEW_INS (cfg, temp2, OP_IA64_CMP4_GT_UN);
			temp2->sreg1 = ins->sreg1;
			temp2->sreg2 = temp->dreg;

			NEW_INS (cfg, temp, OP_IA64_COND_EXC);
			temp->inst_p1 = (char*)"OverflowException";

			ins->opcode = OP_MOVE;
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I2:
		case OP_FCONV_TO_U2:
		case OP_FCONV_TO_I1:
		case OP_FCONV_TO_U1:
			NEW_INS (cfg, temp, OP_FCONV_TO_I8);
			temp->sreg1 = ins->sreg1;
			temp->dreg = ins->dreg;

			switch (ins->opcode) {
			case OP_FCONV_TO_I4:
				ins->opcode = OP_SEXT_I4;
				break;
			case OP_FCONV_TO_I2:
				ins->opcode = OP_SEXT_I2;
				break;
			case OP_FCONV_TO_U2:
				ins->opcode = OP_ZEXT_I4;
				break;
			case OP_FCONV_TO_I1:
				ins->opcode = OP_SEXT_I1;
				break;
			case OP_FCONV_TO_U1:
				ins->opcode = OP_ZEXT_I1;
				break;
			default:
				g_assert_not_reached ();
			}
			ins->sreg1 = ins->dreg;
			break;
		default:
			break;
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;

	bb->max_vreg = cfg->next_vreg;
}

/*
 * emit_load_volatile_arguments:
 *
 *  Load volatile arguments from the stack to the original input registers.
 * Required before a tail call.
 */
static Ia64CodegenState
emit_load_volatile_arguments (MonoCompile *cfg, Ia64CodegenState code)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *ins;
	CallInfo *cinfo;
	guint32 i;

	/* FIXME: Generate intermediate code instead */

	sig = mono_method_signature (method);

	cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);
	
	/* This is the opposite of the code in emit_prolog */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;
		MonoType *arg_type;

		ins = cfg->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		arg_type = mono_type_get_underlying_type (arg_type);

		stack_offset = ainfo->offset + ARGS_OFFSET;

		/* Save volatile arguments to the stack */
		if (ins->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInFloatReg:
				/* FIXME: big offsets */
				g_assert (ins->opcode == OP_REGOFFSET);
				ia64_adds_imm (code, GP_SCRATCH_REG, ins->inst_offset, ins->inst_basereg);
				if (arg_type->byref)
					ia64_ld8 (code, cfg->arch.reg_in0 + ainfo->reg, GP_SCRATCH_REG);
				else {
					switch (arg_type->type) {
					case MONO_TYPE_R4:
						ia64_ldfs (code, ainfo->reg, GP_SCRATCH_REG);
						break;
					case MONO_TYPE_R8:
						ia64_ldfd (code, ainfo->reg, GP_SCRATCH_REG);
						break;
					default:
						ia64_ld8 (code, cfg->arch.reg_in0 + ainfo->reg, GP_SCRATCH_REG);
						break;
					}
				}
				break;
			case ArgOnStack:
				break;
			default:
				NOT_IMPLEMENTED;
			}
		}

		if (ins->opcode == OP_REGVAR) {
			/* Argument allocated to (non-volatile) register */
			switch (ainfo->storage) {
			case ArgInIReg:
				if (ins->dreg != cfg->arch.reg_in0 + ainfo->reg)
					ia64_mov (code, cfg->arch.reg_in0 + ainfo->reg, ins->dreg);
				break;
			case ArgOnStack:
				ia64_adds_imm (code, GP_SCRATCH_REG, 16 + ainfo->offset, cfg->frame_reg);
				ia64_st8 (code, GP_SCRATCH_REG, ins->dreg);
				break;
			default:
				NOT_IMPLEMENTED;
			}
		}
	}

	return code;
}

static Ia64CodegenState
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, Ia64CodegenState code)
{
	CallInfo *cinfo;
	int i;

	/* Move return value to the target register */
	switch (ins->opcode) {
	case OP_VOIDCALL:
	case OP_VOIDCALL_REG:
	case OP_VOIDCALL_MEMBASE:
		break;
	case OP_CALL:
	case OP_CALL_REG:
	case OP_CALL_MEMBASE:
	case OP_LCALL:
	case OP_LCALL_REG:
	case OP_LCALL_MEMBASE:
		g_assert (ins->dreg == IA64_R8);
		break;
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		g_assert (ins->dreg == 8);
		if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4)
			ia64_fnorm_d_sf (code, ins->dreg, ins->dreg, 0);
		break;
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
	case OP_VCALL2:
	case OP_VCALL2_REG:
	case OP_VCALL2_MEMBASE: {
		ArgStorage storage;

		cinfo = get_call_info (cfg, cfg->mempool, ((MonoCallInst*)ins)->signature, FALSE);
		storage = cinfo->ret.storage;

		if (storage == ArgAggregate) {
			MonoInst *local = (MonoInst*)cfg->arch.ret_var_addr_local;

			/* Load address of stack space allocated for the return value */
			ia64_movl (code, GP_SCRATCH_REG, local->inst_offset);
			ia64_add (code, GP_SCRATCH_REG, GP_SCRATCH_REG, local->inst_basereg);
			ia64_ld8 (code, GP_SCRATCH_REG, GP_SCRATCH_REG);

			for (i = 0; i < cinfo->ret.nregs; ++i) {
				switch (cinfo->ret.atype) {
				case AggregateNormal:
					ia64_st8_inc_imm_hint (code, GP_SCRATCH_REG, cinfo->ret.reg + i, 8, 0);
					break;
				case AggregateSingleHFA:
					ia64_stfs_inc_imm_hint (code, GP_SCRATCH_REG, cinfo->ret.reg + i, 4, 0);
					break;
				case AggregateDoubleHFA:
					ia64_stfd_inc_imm_hint (code, GP_SCRATCH_REG, cinfo->ret.reg + i, 8, 0);
					break;
				default:
					g_assert_not_reached ();
				}
			}
		}
		break;
	}
	default:
		g_assert_not_reached ();
	}

	return code;
}

#define add_patch_info(cfg,code,patch_type,data) do { \
	mono_add_patch_info (cfg, code.buf + code.nins - cfg->native_code, patch_type, data); \
} while (0)

#define emit_cond_system_exception(cfg,code,exc_name,predicate) do { \
	MonoInst *tins = mono_branch_optimize_exception_target (cfg, bb, exc_name); \
    if (tins == NULL) \
        add_patch_info (cfg, code, MONO_PATCH_INFO_EXC, exc_name); \
    else \
		add_patch_info (cfg, code, MONO_PATCH_INFO_BB, tins->inst_true_bb); \
	ia64_br_cond_pred (code, (predicate), 0); \
} while (0)

static Ia64CodegenState
emit_call (MonoCompile *cfg, Ia64CodegenState code, guint32 patch_type, gconstpointer data)
{
	add_patch_info (cfg, code, patch_type, data);

	if ((patch_type == MONO_PATCH_INFO_ABS) || (patch_type == MONO_PATCH_INFO_INTERNAL_METHOD)) {
		/* Indirect call */
		/* mono_arch_patch_callsite will patch this */
		/* mono_arch_nullify_class_init_trampoline will patch this */
		ia64_movl (code, GP_SCRATCH_REG, 0);
		ia64_ld8_inc_imm (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, 8);
		ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2);
		ia64_ld8 (code, IA64_GP, GP_SCRATCH_REG);
		ia64_br_call_reg (code, IA64_B0, IA64_B6);
	}
	else {
		/* Can't use a direct call since the displacement might be too small */
		/* mono_arch_patch_callsite will patch this */
		ia64_movl (code, GP_SCRATCH_REG, 0);
		ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
		ia64_br_call_reg (code, IA64_B0, IA64_B6);
	}

	return code;
}

#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint offset;
	Ia64CodegenState code;
	guint8 *code_start = cfg->native_code + cfg->code_len;
	MonoInst *last_ins = NULL;
	guint last_offset = 0;
	int max_len, cpos;

	if (cfg->opt & MONO_OPT_LOOP) {
		/* FIXME: */
	}

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		NOT_IMPLEMENTED;
	}

	offset = code_start - cfg->native_code;

	ia64_codegen_init (code, code_start);

#if 0
	if (strstr (cfg->method->name, "conv_ovf_i1") && (bb->block_num == 2))
		break_count ();
#endif

	MONO_BB_FOR_EACH_INS (bb, ins) {
		offset = code.buf - cfg->native_code;

		max_len = ((int)(((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN])) + 128;

		while (offset + max_len + 16 > cfg->code_size) {
			ia64_codegen_close (code);

			offset = code.buf - cfg->native_code;

			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code_start = cfg->native_code + offset;
			mono_jit_stats.code_reallocs++;

			ia64_codegen_init (code, code_start);
		}

		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_ICONST:
		case OP_I8CONST:
			if (ia64_is_imm14 (ins->inst_c0))
				ia64_adds_imm (code, ins->dreg, ins->inst_c0, IA64_R0);
			else
				ia64_movl (code, ins->dreg, ins->inst_c0);
			break;
		case OP_JUMP_TABLE:
			add_patch_info (cfg, code, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			ia64_movl (code, ins->dreg, 0);
			break;
		case OP_MOVE:
			ia64_mov (code, ins->dreg, ins->sreg1);
			break;
		case OP_BR:
		case OP_IA64_BR_COND: {
			int pred = 0;
			if (ins->opcode == OP_IA64_BR_COND)
				pred = 6;
			if (ins->inst_target_bb->native_offset) {
				guint8 *pos = code.buf + code.nins;

				ia64_br_cond_pred (code, pred, 0);
				ia64_begin_bundle (code);
				ia64_patch (pos, cfg->native_code + ins->inst_target_bb->native_offset);
			} else {
				add_patch_info (cfg, code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
				ia64_br_cond_pred (code, pred, 0);
			} 
			break;
		}
		case OP_LABEL:
			ia64_begin_bundle (code);
			ins->inst_c0 = code.buf - cfg->native_code;
			break;
		case OP_NOP:
		case OP_RELAXED_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_STORE:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_BR_REG:
			ia64_mov_to_br (code, IA64_B6, ins->sreg1);
			ia64_br_cond_reg (code, IA64_B6);
			break;
		case OP_IADD:
		case OP_LADD:
			ia64_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISUB:
		case OP_LSUB:
			ia64_sub (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IAND:
		case OP_LAND:
			ia64_and (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IOR:
		case OP_LOR:
			ia64_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IXOR:
		case OP_LXOR:
			ia64_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_INEG:
		case OP_LNEG:
			ia64_sub (code, ins->dreg, IA64_R0, ins->sreg1);
			break;
		case OP_INOT:
		case OP_LNOT:
			ia64_andcm_imm (code, ins->dreg, -1, ins->sreg1);
			break;
		case OP_ISHL:
		case OP_LSHL:
			ia64_shl (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR:
			ia64_sxt4 (code, GP_SCRATCH_REG, ins->sreg1);
			ia64_shr (code, ins->dreg, GP_SCRATCH_REG, ins->sreg2);
			break;
		case OP_LSHR:
			ia64_shr (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR_UN:
			ia64_zxt4 (code, GP_SCRATCH_REG, ins->sreg1);
			ia64_shr_u (code, ins->dreg, GP_SCRATCH_REG, ins->sreg2);
			break;
		case OP_LSHR_UN:
			ia64_shr_u (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IADDCC:
			/* p6 and p7 is set if there is signed/unsigned overflow */
			
			/* Set p8-p9 == (sreg2 > 0) */
			ia64_cmp4_lt (code, 8, 9, IA64_R0, ins->sreg2);

			ia64_add (code, GP_SCRATCH_REG, ins->sreg1, ins->sreg2);
			
			/* (sreg2 > 0) && (res < ins->sreg1) => signed overflow */
			ia64_cmp4_lt_pred (code, 8, 6, 10, GP_SCRATCH_REG, ins->sreg1);
			/* (sreg2 <= 0) && (res > ins->sreg1) => signed overflow */
			ia64_cmp4_lt_pred (code, 9, 6, 10, ins->sreg1, GP_SCRATCH_REG);

			/* res <u sreg1 => unsigned overflow */
			ia64_cmp4_ltu (code, 7, 10, GP_SCRATCH_REG, ins->sreg1);

			/* FIXME: Predicate this since this is a side effect */
			ia64_mov (code, ins->dreg, GP_SCRATCH_REG);
			break;
		case OP_ISUBCC:
			/* p6 and p7 is set if there is signed/unsigned overflow */
			
			/* Set p8-p9 == (sreg2 > 0) */
			ia64_cmp4_lt (code, 8, 9, IA64_R0, ins->sreg2);

			ia64_sub (code, GP_SCRATCH_REG, ins->sreg1, ins->sreg2);
			
			/* (sreg2 > 0) && (res > ins->sreg1) => signed overflow */
			ia64_cmp4_gt_pred (code, 8, 6, 10, GP_SCRATCH_REG, ins->sreg1);
			/* (sreg2 <= 0) && (res < ins->sreg1) => signed overflow */
			ia64_cmp4_lt_pred (code, 9, 6, 10, GP_SCRATCH_REG, ins->sreg1);

			/* sreg1 <u sreg2 => unsigned overflow */
			ia64_cmp4_ltu (code, 7, 10, ins->sreg1, ins->sreg2);

			/* FIXME: Predicate this since this is a side effect */
			ia64_mov (code, ins->dreg, GP_SCRATCH_REG);
			break;
		case OP_ADDCC:
			/* Same as OP_IADDCC */
			ia64_cmp_lt (code, 8, 9, IA64_R0, ins->sreg2);

			ia64_add (code, GP_SCRATCH_REG, ins->sreg1, ins->sreg2);
			
			ia64_cmp_lt_pred (code, 8, 6, 10, GP_SCRATCH_REG, ins->sreg1);
			ia64_cmp_lt_pred (code, 9, 6, 10, ins->sreg1, GP_SCRATCH_REG);

			ia64_cmp_ltu (code, 7, 10, GP_SCRATCH_REG, ins->sreg1);

			ia64_mov (code, ins->dreg, GP_SCRATCH_REG);
			break;
		case OP_SUBCC:
			/* Same as OP_ISUBCC */

			ia64_cmp_lt (code, 8, 9, IA64_R0, ins->sreg2);

			ia64_sub (code, GP_SCRATCH_REG, ins->sreg1, ins->sreg2);
			
			ia64_cmp_gt_pred (code, 8, 6, 10, GP_SCRATCH_REG, ins->sreg1);
			ia64_cmp_lt_pred (code, 9, 6, 10, GP_SCRATCH_REG, ins->sreg1);

			ia64_cmp_ltu (code, 7, 10, ins->sreg1, ins->sreg2);

			ia64_mov (code, ins->dreg, GP_SCRATCH_REG);
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
			ia64_adds_imm (code, ins->dreg, ins->inst_imm, ins->sreg1);
			break;
		case OP_IAND_IMM:
		case OP_AND_IMM:
		case OP_LAND_IMM:
			ia64_and_imm (code, ins->dreg, ins->inst_imm, ins->sreg1);
			break;
		case OP_IOR_IMM:
		case OP_LOR_IMM:
			ia64_or_imm (code, ins->dreg, ins->inst_imm, ins->sreg1);
			break;
		case OP_IXOR_IMM:
		case OP_LXOR_IMM:
			ia64_xor_imm (code, ins->dreg, ins->inst_imm, ins->sreg1);
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
		case OP_LSHL_IMM:
			ia64_shl_imm (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_SHR_IMM:
		case OP_LSHR_IMM:
			ia64_shr_imm (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_ISHR_IMM:
			g_assert (ins->inst_imm <= 64);
			ia64_extr (code, ins->dreg, ins->sreg1, ins->inst_imm, 32 - ins->inst_imm);
			break;
		case OP_ISHR_UN_IMM:
			ia64_zxt4 (code, GP_SCRATCH_REG, ins->sreg1);
			ia64_shr_u_imm (code, ins->dreg, GP_SCRATCH_REG, ins->inst_imm);
			break;
		case OP_LSHR_UN_IMM:
			ia64_shr_u_imm (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_LMUL:
			/* Based on gcc code */
			ia64_setf_sig (code, FP_SCRATCH_REG, ins->sreg1);
			ia64_setf_sig (code, FP_SCRATCH_REG2, ins->sreg2);
			ia64_xmpy_l (code, FP_SCRATCH_REG, FP_SCRATCH_REG, FP_SCRATCH_REG2);
			ia64_getf_sig (code, ins->dreg, FP_SCRATCH_REG);
			break;

		case OP_STOREI1_MEMBASE_REG:
			ia64_st1_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_STOREI2_MEMBASE_REG:
			ia64_st2_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_STOREI4_MEMBASE_REG:
			ia64_st4_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_STOREI8_MEMBASE_REG:
		case OP_STORE_MEMBASE_REG:
			if (ins->inst_offset != 0) {
				/* This is generated by local regalloc */
				if (ia64_is_imm14 (ins->inst_offset)) {
					ia64_adds_imm (code, GP_SCRATCH_REG, ins->inst_offset, ins->inst_destbasereg);
				} else {
					ia64_movl (code, GP_SCRATCH_REG, ins->inst_offset);
					ia64_add (code, GP_SCRATCH_REG, GP_SCRATCH_REG, ins->inst_destbasereg);
				}
				ins->inst_destbasereg = GP_SCRATCH_REG;
			}
			ia64_st8_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;

		case OP_IA64_STOREI1_MEMBASE_INC_REG:
			ia64_st1_inc_imm_hint (code, ins->inst_destbasereg, ins->sreg1, 1, 0);
			break;
		case OP_IA64_STOREI2_MEMBASE_INC_REG:
			ia64_st2_inc_imm_hint (code, ins->inst_destbasereg, ins->sreg1, 2, 0);
			break;
		case OP_IA64_STOREI4_MEMBASE_INC_REG:
			ia64_st4_inc_imm_hint (code, ins->inst_destbasereg, ins->sreg1, 4, 0);
			break;
		case OP_IA64_STOREI8_MEMBASE_INC_REG:
			ia64_st8_inc_imm_hint (code, ins->inst_destbasereg, ins->sreg1, 8, 0);
			break;

		case OP_LOADU1_MEMBASE:
			ia64_ld1 (code, ins->dreg, ins->inst_basereg);
			break;
		case OP_LOADU2_MEMBASE:
			ia64_ld2 (code, ins->dreg, ins->inst_basereg);
			break;
		case OP_LOADU4_MEMBASE:
			ia64_ld4 (code, ins->dreg, ins->inst_basereg);
			break;
		case OP_LOADI1_MEMBASE:
			ia64_ld1 (code, ins->dreg, ins->inst_basereg);
			ia64_sxt1 (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADI2_MEMBASE:
			ia64_ld2 (code, ins->dreg, ins->inst_basereg);
			ia64_sxt2 (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADI4_MEMBASE:
			ia64_ld4 (code, ins->dreg, ins->inst_basereg);
			ia64_sxt4 (code, ins->dreg, ins->dreg);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE:
			if (ins->inst_offset != 0) {
				/* This is generated by local regalloc */
				if (ia64_is_imm14 (ins->inst_offset)) {
					ia64_adds_imm (code, GP_SCRATCH_REG, ins->inst_offset, ins->inst_basereg);
				} else {
					ia64_movl (code, GP_SCRATCH_REG, ins->inst_offset);
					ia64_add (code, GP_SCRATCH_REG, GP_SCRATCH_REG, ins->inst_basereg);
				}
				ins->inst_basereg = GP_SCRATCH_REG;
			}
			ia64_ld8 (code, ins->dreg, ins->inst_basereg);
			break;

		case OP_IA64_LOADU1_MEMBASE_INC:
			ia64_ld1_inc_imm_hint (code, ins->dreg, ins->inst_basereg, 1, 0);
			break;
		case OP_IA64_LOADU2_MEMBASE_INC:
			ia64_ld2_inc_imm_hint (code, ins->dreg, ins->inst_basereg, 2, 0);
			break;
		case OP_IA64_LOADU4_MEMBASE_INC:
			ia64_ld4_inc_imm_hint (code, ins->dreg, ins->inst_basereg, 4, 0);
			break;
		case OP_IA64_LOADI8_MEMBASE_INC:
			ia64_ld8_inc_imm_hint (code, ins->dreg, ins->inst_basereg, 8, 0);
			break;

		case OP_SEXT_I1:
			ia64_sxt1 (code, ins->dreg, ins->sreg1);
			break;
		case OP_SEXT_I2:
			ia64_sxt2 (code, ins->dreg, ins->sreg1);
			break;
		case OP_SEXT_I4:
			ia64_sxt4 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ZEXT_I1:
			ia64_zxt1 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ZEXT_I2:
			ia64_zxt2 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ZEXT_I4:
			ia64_zxt4 (code, ins->dreg, ins->sreg1);
			break;

			/* Compare opcodes */
		case OP_IA64_CMP4_EQ:
			ia64_cmp4_eq (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_NE:
			ia64_cmp4_ne (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_LE:
			ia64_cmp4_le (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_LT:
			ia64_cmp4_lt (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_GE:
			ia64_cmp4_ge (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_GT:
			ia64_cmp4_gt (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_LT_UN:
			ia64_cmp4_ltu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_LE_UN:
			ia64_cmp4_leu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_GT_UN:
			ia64_cmp4_gtu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_GE_UN:
			ia64_cmp4_geu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_EQ:
			ia64_cmp_eq (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_NE:
			ia64_cmp_ne (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_LE:
			ia64_cmp_le (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_LT:
			ia64_cmp_lt (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_GE:
			ia64_cmp_ge (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_GT:
			ia64_cmp_gt (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_GT_UN:
			ia64_cmp_gtu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_LT_UN:
			ia64_cmp_ltu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_GE_UN:
			ia64_cmp_geu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP_LE_UN:
			ia64_cmp_leu (code, 6, 7, ins->sreg1, ins->sreg2);
			break;
		case OP_IA64_CMP4_EQ_IMM:
			ia64_cmp4_eq_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_NE_IMM:
			ia64_cmp4_ne_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_LE_IMM:
			ia64_cmp4_le_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_LT_IMM:
			ia64_cmp4_lt_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_GE_IMM:
			ia64_cmp4_ge_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_GT_IMM:
			ia64_cmp4_gt_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_LT_UN_IMM:
			ia64_cmp4_ltu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_LE_UN_IMM:
			ia64_cmp4_leu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_GT_UN_IMM:
			ia64_cmp4_gtu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP4_GE_UN_IMM:
			ia64_cmp4_geu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_EQ_IMM:
			ia64_cmp_eq_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_NE_IMM:
			ia64_cmp_ne_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_LE_IMM:
			ia64_cmp_le_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_LT_IMM:
			ia64_cmp_lt_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_GE_IMM:
			ia64_cmp_ge_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_GT_IMM:
			ia64_cmp_gt_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_GT_UN_IMM:
			ia64_cmp_gtu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_LT_UN_IMM:
			ia64_cmp_ltu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_GE_UN_IMM:
			ia64_cmp_geu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_CMP_LE_UN_IMM:
			ia64_cmp_leu_imm (code, 6, 7, ins->inst_imm, ins->sreg2);
			break;
		case OP_IA64_FCMP_EQ:
			ia64_fcmp_eq_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_NE:
			ia64_fcmp_ne_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_LT:
			ia64_fcmp_lt_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_GT:
			ia64_fcmp_gt_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_LE:
			ia64_fcmp_le_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_GE:
			ia64_fcmp_ge_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_GT_UN:
			ia64_fcmp_gt_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			ia64_fcmp_unord_sf_pred (code, 7, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_LT_UN:
			ia64_fcmp_lt_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			ia64_fcmp_unord_sf_pred (code, 7, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_GE_UN:
			ia64_fcmp_ge_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			ia64_fcmp_unord_sf_pred (code, 7, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;
		case OP_IA64_FCMP_LE_UN:
			ia64_fcmp_le_sf (code, 6, 7, ins->sreg1, ins->sreg2, 0);
			ia64_fcmp_unord_sf_pred (code, 7, 6, 7, ins->sreg1, ins->sreg2, 0);
			break;

		case OP_COND_EXC_IOV:
		case OP_COND_EXC_OV:
			emit_cond_system_exception (cfg, code, "OverflowException", 6);
			break;
		case OP_COND_EXC_IC:
		case OP_COND_EXC_C:
			emit_cond_system_exception (cfg, code, "OverflowException", 7);
			break;
		case OP_IA64_COND_EXC:
			emit_cond_system_exception (cfg, code, ins->inst_p1, 6);
			break;
		case OP_IA64_CSET:
			ia64_mov_pred (code, 7, ins->dreg, IA64_R0);
			ia64_no_stop (code);
			ia64_add1_pred (code, 6, ins->dreg, IA64_R0, IA64_R0);
			break;
		case OP_ICONV_TO_I1:
		case OP_LCONV_TO_I1:
			/* FIXME: Is this needed ? */
			ia64_sxt1 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_I2:
		case OP_LCONV_TO_I2:
			/* FIXME: Is this needed ? */
			ia64_sxt2 (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_I4:
			/* FIXME: Is this needed ? */
			ia64_sxt4 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_U1:
		case OP_LCONV_TO_U1:
			/* FIXME: Is this needed */
			ia64_zxt1 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_U2:
		case OP_LCONV_TO_U2:
			/* FIXME: Is this needed */
			ia64_zxt2 (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_U4:
			/* FIXME: Is this needed */
			ia64_zxt4 (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_I8:
		case OP_ICONV_TO_I:
		case OP_LCONV_TO_I8:
		case OP_LCONV_TO_I:
			ia64_sxt4 (code, ins->dreg, ins->sreg1);
			break;
		case OP_LCONV_TO_U8:
		case OP_LCONV_TO_U:
			ia64_zxt4 (code, ins->dreg, ins->sreg1);
			break;

			/*
			 * FLOAT OPCODES
			 */
		case OP_R8CONST: {
			double d = *(double *)ins->inst_p0;

			if ((d == 0.0) && (mono_signbit (d) == 0))
				ia64_fmov (code, ins->dreg, 0);
			else if (d == 1.0)
				ia64_fmov (code, ins->dreg, 1);
			else {
				add_patch_info (cfg, code, MONO_PATCH_INFO_R8, ins->inst_p0);
				ia64_movl (code, GP_SCRATCH_REG, 0);
				ia64_ldfd (code, ins->dreg, GP_SCRATCH_REG);
			}
			break;
		}
		case OP_R4CONST: {
			float f = *(float *)ins->inst_p0;

			if ((f == 0.0) && (mono_signbit (f) == 0))
				ia64_fmov (code, ins->dreg, 0);
			else if (f == 1.0)
				ia64_fmov (code, ins->dreg, 1);
			else {
				add_patch_info (cfg, code, MONO_PATCH_INFO_R4, ins->inst_p0);
				ia64_movl (code, GP_SCRATCH_REG, 0);
				ia64_ldfs (code, ins->dreg, GP_SCRATCH_REG);
			}
			break;
		}
		case OP_FMOVE:
			ia64_fmov (code, ins->dreg, ins->sreg1);
			break;
		case OP_STORER8_MEMBASE_REG:
			if (ins->inst_offset != 0) {
				/* This is generated by local regalloc */
				if (ia64_is_imm14 (ins->inst_offset)) {
					ia64_adds_imm (code, GP_SCRATCH_REG, ins->inst_offset, ins->inst_destbasereg);
				} else {
					ia64_movl (code, GP_SCRATCH_REG, ins->inst_offset);
					ia64_add (code, GP_SCRATCH_REG, GP_SCRATCH_REG, ins->inst_destbasereg);
				}
				ins->inst_destbasereg = GP_SCRATCH_REG;
			}
			ia64_stfd_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_STORER4_MEMBASE_REG:
			ia64_fnorm_s_sf (code, FP_SCRATCH_REG, ins->sreg1, 0);
			ia64_stfs_hint (code, ins->inst_destbasereg, FP_SCRATCH_REG, 0);
			break;
		case OP_LOADR8_MEMBASE:
			if (ins->inst_offset != 0) {
				/* This is generated by local regalloc */
				if (ia64_is_imm14 (ins->inst_offset)) {
					ia64_adds_imm (code, GP_SCRATCH_REG, ins->inst_offset, ins->inst_basereg);
				} else {
					ia64_movl (code, GP_SCRATCH_REG, ins->inst_offset);
					ia64_add (code, GP_SCRATCH_REG, GP_SCRATCH_REG, ins->inst_basereg);
				}
				ins->inst_basereg = GP_SCRATCH_REG;
			}
			ia64_ldfd (code, ins->dreg, ins->inst_basereg);
			break;
		case OP_LOADR4_MEMBASE:
			ia64_ldfs (code, ins->dreg, ins->inst_basereg);
			ia64_fnorm_d_sf (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_ICONV_TO_R4:
		case OP_LCONV_TO_R4:
			ia64_setf_sig (code, ins->dreg, ins->sreg1);
			ia64_fcvt_xf (code, ins->dreg, ins->dreg);
			ia64_fnorm_s_sf (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_ICONV_TO_R8:
		case OP_LCONV_TO_R8:
			ia64_setf_sig (code, ins->dreg, ins->sreg1);
			ia64_fcvt_xf (code, ins->dreg, ins->dreg);
			ia64_fnorm_d_sf (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_FCONV_TO_R4:
			ia64_fnorm_s_sf (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_FCONV_TO_I8:
		case OP_FCONV_TO_I:
			ia64_fcvt_fx_trunc_sf (code, FP_SCRATCH_REG, ins->sreg1, 0);
			ia64_getf_sig (code, ins->dreg, FP_SCRATCH_REG);
			break;
		case OP_FADD:
			ia64_fma_d_sf (code, ins->dreg, ins->sreg1, 1, ins->sreg2, 0);
			break;
		case OP_FSUB:
			ia64_fms_d_sf (code, ins->dreg, ins->sreg1, 1, ins->sreg2, 0);
			break;
		case OP_FMUL:
			ia64_fma_d_sf (code, ins->dreg, ins->sreg1, ins->sreg2, 0, 0);
			break;
		case OP_FNEG:
			ia64_fmerge_ns (code, ins->dreg, ins->sreg1, ins->sreg1);
			break;
		case OP_CKFINITE:
			/* Quiet NaN */
			ia64_fclass_m (code, 6, 7, ins->sreg1, 0x080);
			emit_cond_system_exception (cfg, code, "ArithmeticException", 6);
			/* Signaling NaN */
			ia64_fclass_m (code, 6, 7, ins->sreg1, 0x040);
			emit_cond_system_exception (cfg, code, "ArithmeticException", 6);
			/* Positive infinity */
			ia64_fclass_m (code, 6, 7, ins->sreg1, 0x021);
			emit_cond_system_exception (cfg, code, "ArithmeticException", 6);
			/* Negative infinity */
			ia64_fclass_m (code, 6, 7, ins->sreg1, 0x022);
			emit_cond_system_exception (cfg, code, "ArithmeticException", 6);
			break;

		/* Calls */
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			/* Can't use ld8 as this could be a vtype address */
			ia64_ld1 (code, GP_SCRATCH_REG, ins->sreg1);
			break;
		case OP_ARGLIST:
			ia64_adds_imm (code, GP_SCRATCH_REG, cfg->sig_cookie, cfg->frame_reg);
			ia64_st8 (code, ins->sreg1, GP_SCRATCH_REG);
			break;
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;

			if (ins->flags & MONO_INST_HAS_METHOD)
				code = emit_call (cfg, code, MONO_PATCH_INFO_METHOD, call->method);
			else
				code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, call->fptr);

			code = emit_move_return_value (cfg, ins, code);
			break;

		case OP_CALL_REG:
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG: {
			MonoCallInst *call = (MonoCallInst*)ins;
			CallInfo *cinfo;
			int out_reg;

			/* 
			 * mono_arch_get_this_arg_from_call () needs to find the this argument in a global 
			 * register.
			 */
			cinfo = get_call_info (cfg, cfg->mempool, call->signature, FALSE);
			out_reg = cfg->arch.reg_out0;
			ia64_mov (code, IA64_R10, out_reg);

			/* Indirect call */
			ia64_mov (code, IA64_R8, ins->sreg1);
			ia64_ld8_inc_imm (code, GP_SCRATCH_REG2, IA64_R8, 8);
			ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2);
			ia64_ld8 (code, IA64_GP, IA64_R8);
			ia64_br_call_reg (code, IA64_B0, IA64_B6);

			code = emit_move_return_value (cfg, ins, code);
			break;
		}
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE: {
			MonoCallInst *call = (MonoCallInst*)ins;
			CallInfo *cinfo;
			int out_reg;

			ia64_mov (code, IA64_R11, ins->sreg1);
			if (ia64_is_imm14 (ins->inst_offset))
				ia64_adds_imm (code, IA64_R8, ins->inst_offset, ins->sreg1);
			else {
				ia64_movl (code, GP_SCRATCH_REG, ins->inst_offset);
				ia64_add (code, IA64_R8, GP_SCRATCH_REG, ins->sreg1);
			}

			if (call->method && ins->inst_offset < 0) {
				/* 
				 * This is a possible IMT call so save the IMT method in a global 
				 * register where mono_arch_find_imt_method () and its friends can 
				 * access it.
				 */
				ia64_movl (code, IA64_R9, call->method);
			}

			/* 
			 * mono_arch_find_this_arg () needs to find the this argument in a global 
			 * register.
			 */
			cinfo = get_call_info (cfg, cfg->mempool, call->signature, FALSE);
			out_reg = cfg->arch.reg_out0;
			ia64_mov (code, IA64_R10, out_reg);

			ia64_ld8 (code, GP_SCRATCH_REG, IA64_R8);

			ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);

			ia64_br_call_reg (code, IA64_B0, IA64_B6);

			code = emit_move_return_value (cfg, ins, code);
			break;
		}
		case OP_JMP: {
			/*
			 * Keep in sync with the code in emit_epilog.
			 */

			if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
				NOT_IMPLEMENTED;

			g_assert (!cfg->method->save_lmf);

			/* Load arguments into their original registers */
			code = emit_load_volatile_arguments (cfg, code);

			if (cfg->arch.stack_alloc_size) {
				if (cfg->arch.omit_fp) {
					if (ia64_is_imm14 (cfg->arch.stack_alloc_size))
						ia64_adds_imm (code, IA64_SP, (cfg->arch.stack_alloc_size), IA64_SP);
					else {
						ia64_movl (code, GP_SCRATCH_REG, cfg->arch.stack_alloc_size);
						ia64_add (code, IA64_SP, GP_SCRATCH_REG, IA64_SP);
					}
				}
				else
					ia64_mov (code, IA64_SP, cfg->arch.reg_saved_sp);
			}
			ia64_mov_to_ar_i (code, IA64_PFS, cfg->arch.reg_saved_ar_pfs);
			ia64_mov_ret_to_br (code, IA64_B0, cfg->arch.reg_saved_b0);

			add_patch_info (cfg, code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			ia64_movl (code, GP_SCRATCH_REG, 0);
			ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
			ia64_br_cond_reg (code, IA64_B6);

			break;
		}
		case OP_BREAK:
			code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, mono_break);
			break;

		case OP_LOCALLOC: {
			gint32 abi_offset;

			/* FIXME: Sigaltstack support */

			/* keep alignment */
			ia64_adds_imm (code, GP_SCRATCH_REG, MONO_ARCH_LOCALLOC_ALIGNMENT - 1, ins->sreg1);
			ia64_movl (code, GP_SCRATCH_REG2, ~(MONO_ARCH_LOCALLOC_ALIGNMENT - 1));
			ia64_and (code, GP_SCRATCH_REG, GP_SCRATCH_REG, GP_SCRATCH_REG2);

			ia64_sub (code, IA64_SP, IA64_SP, GP_SCRATCH_REG);

			ia64_mov (code, ins->dreg, IA64_SP);

			/* An area at sp is reserved by the ABI for parameter passing */
			abi_offset = - ALIGN_TO (cfg->param_area + 16, MONO_ARCH_LOCALLOC_ALIGNMENT);
			if (ia64_is_adds_imm (abi_offset))
				ia64_adds_imm (code, IA64_SP, abi_offset, IA64_SP);
			else {
				ia64_movl (code, GP_SCRATCH_REG2, abi_offset);
				ia64_add (code, IA64_SP, IA64_SP, GP_SCRATCH_REG2);
			}

			if (ins->flags & MONO_INST_INIT) {
				/* Upper limit */
				ia64_add (code, GP_SCRATCH_REG2, ins->dreg, GP_SCRATCH_REG);

				ia64_codegen_set_one_ins_per_bundle (code, TRUE);

				/* Init loop */
				ia64_st8_inc_imm_hint (code, ins->dreg, IA64_R0, 8, 0);
				ia64_cmp_lt (code, 8, 9, ins->dreg, GP_SCRATCH_REG2);
				ia64_br_cond_pred (code, 8, -2);

				ia64_codegen_set_one_ins_per_bundle (code, FALSE);

				ia64_sub (code, ins->dreg, GP_SCRATCH_REG2, GP_SCRATCH_REG);
			}

			break;
		}
		case OP_LOCALLOC_IMM: {
			gint32 abi_offset;

			/* FIXME: Sigaltstack support */

			gssize size = ins->inst_imm;
			size = (size + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

			if (ia64_is_adds_imm (size))
				ia64_adds_imm (code, GP_SCRATCH_REG, size, IA64_R0);
			else
				ia64_movl (code, GP_SCRATCH_REG, size);

			ia64_sub (code, IA64_SP, IA64_SP, GP_SCRATCH_REG);
			ia64_mov (code, ins->dreg, IA64_SP);

			/* An area at sp is reserved by the ABI for parameter passing */
			abi_offset = - ALIGN_TO (cfg->param_area + 16, MONO_ARCH_FRAME_ALIGNMENT);
			if (ia64_is_adds_imm (abi_offset))
				ia64_adds_imm (code, IA64_SP, abi_offset, IA64_SP);
			else {
				ia64_movl (code, GP_SCRATCH_REG2, abi_offset);
				ia64_add (code, IA64_SP, IA64_SP, GP_SCRATCH_REG2);
			}

			if (ins->flags & MONO_INST_INIT) {
				/* Upper limit */
				ia64_add (code, GP_SCRATCH_REG2, ins->dreg, GP_SCRATCH_REG);

				ia64_codegen_set_one_ins_per_bundle (code, TRUE);

				/* Init loop */
				ia64_st8_inc_imm_hint (code, ins->dreg, IA64_R0, 8, 0);
				ia64_cmp_lt (code, 8, 9, ins->dreg, GP_SCRATCH_REG2);
				ia64_br_cond_pred (code, 8, -2);

				ia64_codegen_set_one_ins_per_bundle (code, FALSE);

				ia64_sub (code, ins->dreg, GP_SCRATCH_REG2, GP_SCRATCH_REG);
			}

			break;
		}
		case OP_TLS_GET:
			ia64_adds_imm (code, ins->dreg, ins->inst_offset, IA64_TP);
			ia64_ld8 (code, ins->dreg, ins->dreg);
			break;

			/* Synchronization */
		case OP_MEMORY_BARRIER:
			ia64_mf (code);
			break;
		case OP_ATOMIC_ADD_IMM_NEW_I4:
			g_assert (ins->inst_offset == 0);
			ia64_fetchadd4_acq_hint (code, ins->dreg, ins->inst_basereg, ins->inst_imm, 0);
			ia64_adds_imm (code, ins->dreg, ins->inst_imm, ins->dreg);
			break;
		case OP_ATOMIC_ADD_IMM_NEW_I8:
			g_assert (ins->inst_offset == 0);
			ia64_fetchadd8_acq_hint (code, ins->dreg, ins->inst_basereg, ins->inst_imm, 0);
			ia64_adds_imm (code, ins->dreg, ins->inst_imm, ins->dreg);
			break;
		case OP_ATOMIC_EXCHANGE_I4:
			ia64_xchg4_hint (code, ins->dreg, ins->inst_basereg, ins->sreg2, 0);
			ia64_sxt4 (code, ins->dreg, ins->dreg);
			break;
		case OP_ATOMIC_EXCHANGE_I8:
			ia64_xchg8_hint (code, ins->dreg, ins->inst_basereg, ins->sreg2, 0);
			break;
		case OP_ATOMIC_ADD_NEW_I4: {
			guint8 *label, *buf;

			/* From libatomic_ops */
			ia64_mf (code);

			ia64_begin_bundle (code);
			label = code.buf + code.nins;
			ia64_ld4_acq (code, GP_SCRATCH_REG, ins->sreg1);
			ia64_add (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, ins->sreg2);
			ia64_mov_to_ar_m (code, IA64_CCV, GP_SCRATCH_REG);
			ia64_cmpxchg4_acq_hint (code, GP_SCRATCH_REG2, ins->sreg1, GP_SCRATCH_REG2, 0);
			ia64_cmp4_eq (code, 6, 7, GP_SCRATCH_REG, GP_SCRATCH_REG2);
			buf = code.buf + code.nins;
			ia64_br_cond_pred (code, 7, 0);
			ia64_begin_bundle (code);
			ia64_patch (buf, label);
			ia64_add (code, ins->dreg, GP_SCRATCH_REG, ins->sreg2);
			break;
		}
		case OP_ATOMIC_ADD_NEW_I8: {
			guint8 *label, *buf;

			/* From libatomic_ops */
			ia64_mf (code);

			ia64_begin_bundle (code);
			label = code.buf + code.nins;
			ia64_ld8_acq (code, GP_SCRATCH_REG, ins->sreg1);
			ia64_add (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, ins->sreg2);
			ia64_mov_to_ar_m (code, IA64_CCV, GP_SCRATCH_REG);
			ia64_cmpxchg8_acq_hint (code, GP_SCRATCH_REG2, ins->sreg1, GP_SCRATCH_REG2, 0);
			ia64_cmp_eq (code, 6, 7, GP_SCRATCH_REG, GP_SCRATCH_REG2);
			buf = code.buf + code.nins;
			ia64_br_cond_pred (code, 7, 0);
			ia64_begin_bundle (code);
			ia64_patch (buf, label);
			ia64_add (code, ins->dreg, GP_SCRATCH_REG, ins->sreg2);
			break;
		}

			/* Exception handling */
		case OP_CALL_HANDLER:
			/*
			 * Using a call instruction would mess up the register stack, so
			 * save the return address to a register and use a
			 * branch.
			 */
			ia64_codegen_set_one_ins_per_bundle (code, TRUE);
			ia64_mov (code, IA64_R15, IA64_R0);
			ia64_mov_from_ip (code, GP_SCRATCH_REG);
			/* Add the length of OP_CALL_HANDLER */
			ia64_adds_imm (code, GP_SCRATCH_REG, 5 * 16, GP_SCRATCH_REG);
			add_patch_info (cfg, code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			ia64_movl (code, GP_SCRATCH_REG2, 0);
			ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2);
			ia64_br_cond_reg (code, IA64_B6);
			// FIXME:
			//mono_cfg_add_try_hole (cfg, ins->inst_eh_block, code, bb);
			ia64_codegen_set_one_ins_per_bundle (code, FALSE);
			break;
		case OP_START_HANDLER: {
			/*
			 * We receive the return address in GP_SCRATCH_REG.
			 */
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			/* 
			 * R15 determines our caller. It is used since it is writable using
			 * libunwind.
			 * R15 == 0 means we are called by OP_CALL_HANDLER or via resume_context ()
			 * R15 != 0 means we are called by call_filter ().
			 */
			ia64_codegen_set_one_ins_per_bundle (code, TRUE);
			ia64_cmp_eq (code, 6, 7, IA64_R15, IA64_R0);

			ia64_br_cond_pred (code, 6, 6);

			/*
			 * Called by call_filter:
			 * Allocate a new stack frame, and set the fp register from the 
			 * value passed in by the caller.
			 * We allocate a similar frame as is done by the prolog, so
			 * if an exception is thrown while executing the filter, the
			 * unwinder can unwind through the filter frame using the unwind
			 * info for the prolog. 
			 */
			ia64_alloc (code, cfg->arch.reg_saved_ar_pfs, cfg->arch.reg_local0 - cfg->arch.reg_in0, cfg->arch.reg_out0 - cfg->arch.reg_local0, cfg->arch.n_out_regs, 0);
			ia64_mov_from_br (code, cfg->arch.reg_saved_b0, IA64_B0);
			ia64_mov (code, cfg->arch.reg_saved_sp, IA64_SP);
			ia64_mov (code, cfg->frame_reg, IA64_R15);
			/* Signal to endfilter that we are called by call_filter */
			ia64_mov (code, GP_SCRATCH_REG, IA64_R0);

			/* Branch target: */
			if (ia64_is_imm14 (spvar->inst_offset)) 
				ia64_adds_imm (code, GP_SCRATCH_REG2, spvar->inst_offset, cfg->frame_reg);
			else {
				ia64_movl (code, GP_SCRATCH_REG2, spvar->inst_offset);
				ia64_add (code, GP_SCRATCH_REG2, cfg->frame_reg, GP_SCRATCH_REG2);
			}

			/* Save the return address */				
			ia64_st8_hint (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, 0);
			ia64_codegen_set_one_ins_per_bundle (code, FALSE);

			break;
		}
		case OP_ENDFINALLY:
		case OP_ENDFILTER: {
			/* FIXME: Return the value in ENDFILTER */
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);

			/* Load the return address */
			if (ia64_is_imm14 (spvar->inst_offset)) {
				ia64_adds_imm (code, GP_SCRATCH_REG, spvar->inst_offset, cfg->frame_reg);
			} else {
				ia64_movl (code, GP_SCRATCH_REG, spvar->inst_offset);
				ia64_add (code, GP_SCRATCH_REG, cfg->frame_reg, GP_SCRATCH_REG);
			}
			ia64_ld8_hint (code, GP_SCRATCH_REG, GP_SCRATCH_REG, 0);

			/* Test caller */
			ia64_cmp_eq (code, 6, 7, GP_SCRATCH_REG, IA64_R0);
			ia64_br_cond_pred (code, 7, 4);

			/* Called by call_filter */
			/* Pop frame */
			ia64_mov_to_ar_i (code, IA64_PFS, cfg->arch.reg_saved_ar_pfs);
			ia64_mov_to_br (code, IA64_B0, cfg->arch.reg_saved_b0);
			ia64_br_ret_reg (code, IA64_B0);			

			/* Called by CALL_HANDLER */
			ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
			ia64_br_cond_reg (code, IA64_B6);
			break;
		}
		case OP_THROW:
			ia64_mov (code, cfg->arch.reg_out0, ins->sreg1);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
							  (gpointer)"mono_arch_throw_exception");

			/* 
			 * This might be the last instruction in the method, so add a dummy
			 * instruction so the unwinder will work.
			 */
			ia64_break_i (code, 0);
			break;
		case OP_RETHROW:
			ia64_mov (code, cfg->arch.reg_out0, ins->sreg1);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
							  (gpointer)"mono_arch_rethrow_exception");

			ia64_break_i (code, 0);
			break;

		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((code.buf - cfg->native_code - offset) > max_len) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, code.buf - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
		last_offset = offset;
	}

	ia64_codegen_close (code);

	cfg->code_len = code.buf - cfg->native_code;
}

void
mono_arch_register_lowlevel_calls (void)
{
}

static Ia64InsType ins_types_in_template [32][3] = {
	{IA64_INS_TYPE_M, IA64_INS_TYPE_I, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_I, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_I, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_I, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_LX, IA64_INS_TYPE_LX},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_LX, IA64_INS_TYPE_LX},
	{0, 0, 0},
	{0, 0, 0},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_F, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_F, IA64_INS_TYPE_I},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_F},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_F},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_I, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_I, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_B, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_B, IA64_INS_TYPE_B},
	{0, 0, 0},
	{0, 0, 0},
	{IA64_INS_TYPE_B, IA64_INS_TYPE_B, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_B, IA64_INS_TYPE_B, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_M, IA64_INS_TYPE_B},
	{0, 0, 0},
	{0, 0, 0},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_F, IA64_INS_TYPE_B},
	{IA64_INS_TYPE_M, IA64_INS_TYPE_F, IA64_INS_TYPE_B},
	{0, 0, 0},
	{0, 0, 0}
};

static gboolean stops_in_template [32][3] = {
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, TRUE, FALSE },
	{ FALSE, TRUE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, FALSE },

	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ TRUE, FALSE, FALSE },
	{ TRUE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },

	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },

	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, TRUE },
	{ FALSE, FALSE, FALSE },
	{ FALSE, FALSE, FALSE }
};

static int last_stop_in_template [32] = {
	-1, 2, 1, 2, -1, 2, -1, -1,
	-1, 2, 0, 2, -1, 2, -1, 2,
	-1, 2, -1, 2, -1, -1, -1, 2,
	-1, 2, -1, -1, -1, 2, -1, -1
};

static guint64 nops_for_ins_types [6] = {
	IA64_NOP_I,
	IA64_NOP_I,
	IA64_NOP_M,
	IA64_NOP_F,
	IA64_NOP_B,
	IA64_NOP_X
};

#define ITYPE_MATCH(itype1, itype2) (((itype1) == (itype2)) || (((itype2) == IA64_INS_TYPE_A) && (((itype1) == IA64_INS_TYPE_I) || ((itype1) == IA64_INS_TYPE_M))))

/* 
 * Debugging support
 */

#if 0
#define DEBUG_INS_SCHED(a) do { a; } while (0)
#else
#define DEBUG_INS_SCHED(a)
#endif

static void
ia64_analyze_deps (Ia64CodegenState *code, int *deps_start, int *stops)
{
	int i, pos, ins_index, current_deps_start, current_ins_start, reg;
	guint8 *deps = code->dep_info;
	gboolean need_stop, no_stop;

	for (i = 0; i < code->nins; ++i)
		stops [i] = FALSE;
	
	ins_index = 0;
	current_deps_start = 0;
	current_ins_start = 0;
	deps_start [ins_index] = current_ins_start;
	pos = 0;
	no_stop = FALSE;
	DEBUG_INS_SCHED (printf ("BEGIN.\n"));
	while (pos < code->dep_info_pos) {
		need_stop = FALSE;
		switch (deps [pos]) {
		case IA64_END_OF_INS:
			ins_index ++;
			current_ins_start = pos + 2;
			deps_start [ins_index] = current_ins_start;
			no_stop = FALSE;
			DEBUG_INS_SCHED (printf ("(%d) END INS.\n", ins_index - 1));
			break;
		case IA64_NONE:
			break;
		case IA64_READ_GR:
			reg = deps [pos + 1];

			DEBUG_INS_SCHED (printf ("READ GR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_GR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_WRITE_GR:
			reg = code->dep_info [pos + 1];

			DEBUG_INS_SCHED (printf ("WRITE GR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_GR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_READ_PR:
			reg = deps [pos + 1];

			DEBUG_INS_SCHED (printf ("READ PR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (((deps [i] == IA64_WRITE_PR) || (deps [i] == IA64_WRITE_PR_FLOAT)) && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_READ_PR_BRANCH:
			reg = deps [pos + 1];

			/* Writes to prs by non-float instructions are visible to branches */
			DEBUG_INS_SCHED (printf ("READ PR BRANCH: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_PR_FLOAT && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_WRITE_PR:
			reg = code->dep_info [pos + 1];

			DEBUG_INS_SCHED (printf ("WRITE PR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (((deps [i] == IA64_WRITE_PR) || (deps [i] == IA64_WRITE_PR_FLOAT)) && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_WRITE_PR_FLOAT:
			reg = code->dep_info [pos + 1];

			DEBUG_INS_SCHED (printf ("WRITE PR FP: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (((deps [i] == IA64_WRITE_GR) || (deps [i] == IA64_WRITE_PR_FLOAT)) && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_READ_BR:
			reg = deps [pos + 1];

			DEBUG_INS_SCHED (printf ("READ BR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_BR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_WRITE_BR:
			reg = code->dep_info [pos + 1];

			DEBUG_INS_SCHED (printf ("WRITE BR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_BR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_READ_BR_BRANCH:
			reg = deps [pos + 1];

			/* Writes to brs are visible to branches */
			DEBUG_INS_SCHED (printf ("READ BR BRACH: %d\n", reg));
			break;
		case IA64_READ_FR:
			reg = deps [pos + 1];

			DEBUG_INS_SCHED (printf ("READ BR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_FR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_WRITE_FR:
			reg = code->dep_info [pos + 1];

			DEBUG_INS_SCHED (printf ("WRITE BR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_FR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_READ_AR:
			reg = deps [pos + 1];

			DEBUG_INS_SCHED (printf ("READ AR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_AR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_WRITE_AR:
			reg = code->dep_info [pos + 1];

			DEBUG_INS_SCHED (printf ("WRITE AR: %d\n", reg));
			for (i = current_deps_start; i < current_ins_start; i += 2)
				if (deps [i] == IA64_WRITE_AR && deps [i + 1] == reg)
					need_stop = TRUE;
			break;
		case IA64_NO_STOP:
			/* 
			 * Explicitly indicate that a stop is not required. Useful for
			 * example when two predicated instructions with negated predicates
			 * write the same registers.
			 */
			no_stop = TRUE;
			break;
		default:
			g_assert_not_reached ();
		}
		pos += 2;

		if (need_stop && !no_stop) {
			g_assert (ins_index > 0);
			stops [ins_index - 1] = 1;

			DEBUG_INS_SCHED (printf ("STOP\n"));
			current_deps_start = current_ins_start;

			/* Skip remaining deps for this instruction */
			while (deps [pos] != IA64_END_OF_INS)
				pos += 2;
		}
	}

	if (code->nins > 0) {
		/* No dependency info for the last instruction */
		stops [code->nins - 1] = 1;
	}

	deps_start [code->nins] = code->dep_info_pos;
}

static void
ia64_real_emit_bundle (Ia64CodegenState *code, int *deps_start, int *stops, int n, guint64 template, guint64 ins1, guint64 ins2, guint64 ins3, guint8 nops)
{
	int stop_pos, i, deps_to_shift, dep_shift;

	g_assert (n <= code->nins);

	// if (n > 1) printf ("FOUND: %ld.\n", template);

	ia64_emit_bundle_template (code, template, ins1, ins2, ins3);

	stop_pos = last_stop_in_template [template] + 1;
	if (stop_pos > n)
		stop_pos = n;

	/* Compute the number of 'real' instructions before the stop */
	deps_to_shift = stop_pos;
	if (stop_pos >= 3 && (nops & (1 << 2)))
		deps_to_shift --;
	if (stop_pos >= 2 && (nops & (1 << 1)))
		deps_to_shift --;
	if (stop_pos >= 1 && (nops & (1 << 0)))
		deps_to_shift --;

	/* 
	 * We have to keep some dependencies whose instructions have been shifted
	 * out of the buffer. So nullify the end_of_ins markers in the dependency
	 * array.
	 */
	for (i = deps_start [deps_to_shift]; i < deps_start [n]; i += 2)
		if (code->dep_info [i] == IA64_END_OF_INS)
			code->dep_info [i] = IA64_NONE;

	g_assert (deps_start [deps_to_shift] <= code->dep_info_pos);
	memcpy (code->dep_info, &code->dep_info [deps_start [deps_to_shift]], code->dep_info_pos - deps_start [deps_to_shift]);
	code->dep_info_pos = code->dep_info_pos - deps_start [deps_to_shift];

	dep_shift = deps_start [deps_to_shift];
	for (i = 0; i < code->nins + 1 - n; ++i)
		deps_start [i] = deps_start [n + i] - dep_shift;

	/* Determine the exact positions of instructions with unwind ops */
	if (code->unw_op_count) {
		int ins_pos [16];
		int curr_ins, curr_ins_pos;

		curr_ins = 0;
		curr_ins_pos = ((code->buf - code->region_start - 16) / 16) * 3;
		for (i = 0; i < 3; ++i) {
			if (! (nops & (1 << i))) {
				ins_pos [curr_ins] = curr_ins_pos + i;
				curr_ins ++;
			}
		}

		for (i = code->unw_op_pos; i < code->unw_op_count; ++i) {
			if (code->unw_ops_pos [i] < n) {
				code->unw_ops [i].when = ins_pos [code->unw_ops_pos [i]];
				//printf ("UNW-OP: %d -> %d\n", code->unw_ops_pos [i], code->unw_ops [i].when);
			}
		}
		if (code->unw_op_pos < code->unw_op_count)
			code->unw_op_pos += n;
	}

	if (n == code->nins) {
		code->template = 0;
		code->nins = 0;
	}		
	else {
		memcpy (&code->instructions [0], &code->instructions [n], (code->nins - n) * sizeof (guint64));
		memcpy (&code->itypes [0], &code->itypes [n], (code->nins - n) * sizeof (int));
		memcpy (&stops [0], &stops [n], (code->nins - n) * sizeof (int));
		code->nins -= n;
	}
}

void
ia64_emit_bundle (Ia64CodegenState *code, gboolean flush)
{
	int i, ins_type, template, nins_to_emit;
	int deps_start [16];
	int stops [16];
	gboolean found;

	/*
	 * We implement a simple scheduler which tries to put three instructions 
	 * per bundle, then two, then one.
	 */
	ia64_analyze_deps (code, deps_start, stops);

	if ((code->nins >= 3) && !code->one_ins_per_bundle) {
		/* Find a suitable template */
		for (template = 0; template < 32; ++template) {
			if (stops_in_template [template][0] != stops [0] ||
				stops_in_template [template][1] != stops [1] ||
				stops_in_template [template][2] != stops [2])
				continue;

			found = TRUE;
			for (i = 0; i < 3; ++i) {
				ins_type = ins_types_in_template [template][i];
				switch (code->itypes [i]) {
				case IA64_INS_TYPE_A:
					found &= (ins_type == IA64_INS_TYPE_I) || (ins_type == IA64_INS_TYPE_M);
					break;
				default:
					found &= (ins_type == code->itypes [i]);
					break;
				}
			}

			if (found)
				found = debug_ins_sched ();

			if (found) {
				ia64_real_emit_bundle (code, deps_start, stops, 3, template, code->instructions [0], code->instructions [1], code->instructions [2], 0);
				break;
			}
		}
	}

	if (code->nins < IA64_INS_BUFFER_SIZE && !flush)
		/* Wait for more instructions */
		return;

	/* If it didn't work out, try putting two instructions into one bundle */
	if ((code->nins >= 2) && !code->one_ins_per_bundle) {
		/* Try a nop at the end */
		for (template = 0; template < 32; ++template) {
			if (stops_in_template [template][0] != stops [0] ||
				((stops_in_template [template][1] != stops [1]) &&
				 (stops_in_template [template][2] != stops [1])))
				 
				continue;

			if (!ITYPE_MATCH (ins_types_in_template [template][0], code->itypes [0]) ||
				!ITYPE_MATCH (ins_types_in_template [template][1], code->itypes [1]))
				continue;

			if (!debug_ins_sched ())
				continue;

			ia64_real_emit_bundle (code, deps_start, stops, 2, template, code->instructions [0], code->instructions [1], nops_for_ins_types [ins_types_in_template [template][2]], 1 << 2);
			break;
		}
	}

	if (code->nins < IA64_INS_BUFFER_SIZE && !flush)
		/* Wait for more instructions */
		return;

	if ((code->nins >= 2) && !code->one_ins_per_bundle) {
		/* Try a nop in the middle */
		for (template = 0; template < 32; ++template) {
			if (((stops_in_template [template][0] != stops [0]) &&
				 (stops_in_template [template][1] != stops [0])) ||
				stops_in_template [template][2] != stops [1])
				continue;

			if (!ITYPE_MATCH (ins_types_in_template [template][0], code->itypes [0]) ||
				!ITYPE_MATCH (ins_types_in_template [template][2], code->itypes [1]))
				continue;

			if (!debug_ins_sched ())
				continue;

			ia64_real_emit_bundle (code, deps_start, stops, 2, template, code->instructions [0], nops_for_ins_types [ins_types_in_template [template][1]], code->instructions [1], 1 << 1);
			break;
		}
	}

	if ((code->nins >= 2) && flush && !code->one_ins_per_bundle) {
		/* Try a nop at the beginning */
		for (template = 0; template < 32; ++template) {
			if ((stops_in_template [template][1] != stops [0]) ||
				(stops_in_template [template][2] != stops [1]))
				continue;

			if (!ITYPE_MATCH (ins_types_in_template [template][1], code->itypes [0]) ||
				!ITYPE_MATCH (ins_types_in_template [template][2], code->itypes [1]))
				continue;

			if (!debug_ins_sched ())
				continue;

			ia64_real_emit_bundle (code, deps_start, stops, 2, template, nops_for_ins_types [ins_types_in_template [template][0]], code->instructions [0], code->instructions [1], 1 << 0);
			break;
		}
	}

	if (code->nins < IA64_INS_BUFFER_SIZE && !flush)
		/* Wait for more instructions */
		return;

	if (flush)
		nins_to_emit = code->nins;
	else
		nins_to_emit = 1;

	while (nins_to_emit > 0) {
		if (!debug_ins_sched ())
			stops [0] = 1;
		switch (code->itypes [0]) {
		case IA64_INS_TYPE_A:
			if (stops [0])
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MIIS, code->instructions [0], IA64_NOP_I, IA64_NOP_I, 0);
			else
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MII, code->instructions [0], IA64_NOP_I, IA64_NOP_I, 0);
			break;
		case IA64_INS_TYPE_I:
			if (stops [0])
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MIIS, IA64_NOP_M, code->instructions [0], IA64_NOP_I, 0);
			else
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MII, IA64_NOP_M, code->instructions [0], IA64_NOP_I, 0);
			break;
		case IA64_INS_TYPE_M:
			if (stops [0])
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MIIS, code->instructions [0], IA64_NOP_I, IA64_NOP_I, 0);
			else
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MII, code->instructions [0], IA64_NOP_I, IA64_NOP_I, 0);
			break;
		case IA64_INS_TYPE_B:
			if (stops [0])
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MIBS, IA64_NOP_M, IA64_NOP_I, code->instructions [0], 0);
			else
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MIB, IA64_NOP_M, IA64_NOP_I, code->instructions [0], 0);
			break;
		case IA64_INS_TYPE_F:
			if (stops [0])
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MFIS, IA64_NOP_M, code->instructions [0], IA64_NOP_I, 0);
			else
				ia64_real_emit_bundle (code, deps_start, stops, 1, IA64_TEMPLATE_MFI, IA64_NOP_M, code->instructions [0], IA64_NOP_I, 0);
			break;
		case IA64_INS_TYPE_LX:
			if (stops [0] || stops [1])
				ia64_real_emit_bundle (code, deps_start, stops, 2, IA64_TEMPLATE_MLXS, IA64_NOP_M, code->instructions [0], code->instructions [1], 0);
			else
				ia64_real_emit_bundle (code, deps_start, stops, 2, IA64_TEMPLATE_MLX, IA64_NOP_M, code->instructions [0], code->instructions [1], 0);
			nins_to_emit --;
			break;
		default:
			g_assert_not_reached ();
		}
		nins_to_emit --;
	}
}

unw_dyn_region_info_t*
mono_ia64_create_unwind_region (Ia64CodegenState *code)
{
	unw_dyn_region_info_t *r;

	g_assert (code->nins == 0);
	r = g_malloc0 (_U_dyn_region_info_size (code->unw_op_count));
	memcpy (&r->op, &code->unw_ops, sizeof (unw_dyn_op_t) * code->unw_op_count);
	r->op_count = code->unw_op_count;
	r->insn_count = ((code->buf - code->region_start) >> 4) * 3;
	code->unw_op_count = 0;
	code->unw_op_pos = 0;
	code->region_start = code->buf;

	return r;
}

static void 
ia64_patch (unsigned char* code, gpointer target)
{
	int template, i;
	guint64 instructions [3];
	guint8 gen_buf [16];
	Ia64CodegenState gen;
	int ins_to_skip;
	gboolean found;

	/* 
	 * code encodes both the position inside the buffer and code.nins when
	 * the instruction was emitted.
	 */
	ins_to_skip = (guint64)code % 16;
	code = (unsigned char*)((guint64)code & ~15);

	/*
	 * Search for the first instruction which is 'patchable', skipping
	 * ins_to_skip instructions.
	 */

	while (TRUE) {

	template = ia64_bundle_template (code);
	instructions [0] = ia64_bundle_ins1 (code);
	instructions [1] = ia64_bundle_ins2 (code);
	instructions [2] = ia64_bundle_ins3 (code);

	ia64_codegen_init (gen, gen_buf);

	found = FALSE;
	for (i = 0; i < 3; ++i) {
		guint64 ins = instructions [i];
		int opcode = ia64_ins_opcode (ins);

		if (ins == nops_for_ins_types [ins_types_in_template [template][i]])
			continue;

		if (ins_to_skip) {
			ins_to_skip --;
			continue;
		}

		switch (ins_types_in_template [template][i]) {
		case IA64_INS_TYPE_A:
		case IA64_INS_TYPE_M:
			if ((opcode == 8) && (ia64_ins_x2a (ins) == 2) && (ia64_ins_ve (ins) == 0)) {
				/* adds */
				ia64_adds_imm_pred (gen, ia64_ins_qp (ins), ia64_ins_r1 (ins), (guint64)target, ia64_ins_r3 (ins));
				instructions [i] = gen.instructions [0];
				found = TRUE;
			}
			else
				NOT_IMPLEMENTED;
			break;
		case IA64_INS_TYPE_B:
			if ((opcode == 4) && (ia64_ins_btype (ins) == 0)) {
				/* br.cond */
				gint64 disp = ((guint8*)target - code) >> 4;

				/* FIXME: hints */
				ia64_br_cond_hint_pred (gen, ia64_ins_qp (ins), disp, 0, 0, 0);
				
				instructions [i] = gen.instructions [0];
				found = TRUE;
			}
			else if (opcode == 5) {
				/* br.call */
				gint64 disp = ((guint8*)target - code) >> 4;

				/* FIXME: hints */
				ia64_br_call_hint_pred (gen, ia64_ins_qp (ins), ia64_ins_b1 (ins), disp, 0, 0, 0);
				instructions [i] = gen.instructions [0];
				found = TRUE;
			}
			else
				NOT_IMPLEMENTED;
			break;
		case IA64_INS_TYPE_LX:
			if (i == 1)
				break;

			if ((opcode == 6) && (ia64_ins_vc (ins) == 0)) {
				/* movl */
				ia64_movl_pred (gen, ia64_ins_qp (ins), ia64_ins_r1 (ins), target);
				instructions [1] = gen.instructions [0];
				instructions [2] = gen.instructions [1];
				found = TRUE;
			}
			else
				NOT_IMPLEMENTED;

			break;
		default:
			NOT_IMPLEMENTED;
		}

		if (found) {
			/* Rewrite code */
			ia64_codegen_init (gen, code);
			ia64_emit_bundle_template (&gen, template, instructions [0], instructions [1], instructions [2]);
			return;
		}
	}

	code += 16;
	}
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		if (patch_info->type == MONO_PATCH_INFO_NONE)
			continue;
		if (mono_compile_aot) {
			NOT_IMPLEMENTED;
		}

		ia64_patch (ip, (gpointer)target);
	}
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int alloc_size, pos, i;
	Ia64CodegenState code;
	CallInfo *cinfo;
	
	sig = mono_method_signature (method);
	pos = 0;

	cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);

	cfg->code_size =  MAX (cfg->header->code_size * 4, 512);

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		cfg->code_size += 1024;
	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		cfg->code_size += 1024;

	cfg->native_code = g_malloc (cfg->code_size);

	ia64_codegen_init (code, cfg->native_code);

	alloc_size = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);
	if (cfg->param_area)
		alloc_size += cfg->param_area;
	if (alloc_size)
		/* scratch area */
		alloc_size += 16;
	alloc_size = ALIGN_TO (alloc_size, MONO_ARCH_FRAME_ALIGNMENT);

	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		/* Force sp to be saved/restored */
		alloc_size += MONO_ARCH_FRAME_ALIGNMENT;

	cfg->arch.stack_alloc_size = alloc_size;

	pos = 0;

	if (method->save_lmf) {
		/* No LMF on IA64 */
	}

	alloc_size -= pos;

	ia64_unw_save_reg (code, UNW_IA64_AR_PFS, UNW_IA64_GR + cfg->arch.reg_saved_ar_pfs);
	ia64_alloc (code, cfg->arch.reg_saved_ar_pfs, cfg->arch.reg_local0 - cfg->arch.reg_in0, cfg->arch.reg_out0 - cfg->arch.reg_local0, cfg->arch.n_out_regs, 0);
	ia64_unw_save_reg (code, UNW_IA64_RP, UNW_IA64_GR + cfg->arch.reg_saved_b0);
	ia64_mov_from_br (code, cfg->arch.reg_saved_b0, IA64_B0);

	if ((alloc_size || cinfo->stack_usage) && !cfg->arch.omit_fp) {
		ia64_unw_save_reg (code, UNW_IA64_SP, UNW_IA64_GR + cfg->arch.reg_saved_sp);
		ia64_mov (code, cfg->arch.reg_saved_sp, IA64_SP);
		if (cfg->frame_reg != cfg->arch.reg_saved_sp)
			ia64_mov (code, cfg->frame_reg, IA64_SP);
	}

	if (alloc_size) {
#if defined(MONO_ARCH_SIGSEGV_ON_ALTSTACK)
		int pagesize = getpagesize ();

		if (alloc_size >= pagesize) {
			gint32 remaining_size = alloc_size;

			/* Generate stack touching code */
			ia64_mov (code, GP_SCRATCH_REG, IA64_SP);			
			while (remaining_size >= pagesize) {
				ia64_movl (code, GP_SCRATCH_REG2, pagesize);
				ia64_sub (code, GP_SCRATCH_REG, GP_SCRATCH_REG, GP_SCRATCH_REG2);
				ia64_ld8 (code, GP_SCRATCH_REG2, GP_SCRATCH_REG);
				remaining_size -= pagesize;
			}
		}
#endif
		if (ia64_is_imm14 (-alloc_size)) {
			if (cfg->arch.omit_fp)
				ia64_unw_add (code, UNW_IA64_SP, (-alloc_size));
			ia64_adds_imm (code, IA64_SP, (-alloc_size), IA64_SP);
		}
		else {
			ia64_movl (code, GP_SCRATCH_REG, -alloc_size);
			if (cfg->arch.omit_fp)
				ia64_unw_add (code, UNW_IA64_SP, (-alloc_size));
			ia64_add (code, IA64_SP, GP_SCRATCH_REG, IA64_SP);
		}
	}

	ia64_begin_bundle (code);

	/* Initialize unwind info */
	cfg->arch.r_pro = mono_ia64_create_unwind_region (&code);

	if (sig->ret->type != MONO_TYPE_VOID) {
		if ((cinfo->ret.storage == ArgInIReg) && (cfg->ret->opcode != OP_REGVAR)) {
			/* Save volatile arguments to the stack */
			NOT_IMPLEMENTED;
		}
	}

	/* Keep this in sync with emit_load_volatile_arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;
		MonoType *arg_type;

		inst = cfg->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		arg_type = mono_type_get_underlying_type (arg_type);

		stack_offset = ainfo->offset + ARGS_OFFSET;

		/*
		 * FIXME: Native code might pass non register sized integers 
		 * without initializing the upper bits.
		 */
		if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED && !arg_type->byref && ainfo->storage == ArgInIReg) {
			int reg = cfg->arch.reg_in0 + ainfo->reg;

			switch (mono_type_to_load_membase (cfg, arg_type)) {
			case OP_LOADI1_MEMBASE:
				ia64_sxt1 (code, reg, reg);
				break;
			case OP_LOADU1_MEMBASE:
				ia64_zxt1 (code, reg, reg);
				break;
			case OP_LOADI2_MEMBASE:
				ia64_sxt2 (code, reg, reg);
				break;
			case OP_LOADU2_MEMBASE:
				ia64_zxt2 (code, reg, reg);
				break;
			default:
				break;
			}
		}

		/* Save volatile arguments to the stack */
		if (inst->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInFloatReg:
			case ArgInFloatRegR4:
				g_assert (inst->opcode == OP_REGOFFSET);
				if (ia64_is_adds_imm (inst->inst_offset))
					ia64_adds_imm (code, GP_SCRATCH_REG, inst->inst_offset, inst->inst_basereg);
				else {
					ia64_movl (code, GP_SCRATCH_REG2, inst->inst_offset);
					ia64_add (code, GP_SCRATCH_REG, GP_SCRATCH_REG, GP_SCRATCH_REG2);
				}
				if (arg_type->byref)
					ia64_st8_hint (code, GP_SCRATCH_REG, cfg->arch.reg_in0 + ainfo->reg, 0);
				else {
					switch (arg_type->type) {
					case MONO_TYPE_R4:
						ia64_stfs_hint (code, GP_SCRATCH_REG, ainfo->reg, 0);
						break;
					case MONO_TYPE_R8:
						ia64_stfd_hint (code, GP_SCRATCH_REG, ainfo->reg, 0);
						break;
					default:
						ia64_st8_hint (code, GP_SCRATCH_REG, cfg->arch.reg_in0 + ainfo->reg, 0);
						break;
					}
				}
				break;
			case ArgOnStack:
				break;
			case ArgAggregate:
				if (ainfo->nslots != ainfo->nregs)
					NOT_IMPLEMENTED;

				g_assert (inst->opcode == OP_REGOFFSET);
				ia64_adds_imm (code, GP_SCRATCH_REG, inst->inst_offset, inst->inst_basereg);
				for (i = 0; i < ainfo->nregs; ++i) {
					switch (ainfo->atype) {
					case AggregateNormal:
						ia64_st8_inc_imm_hint (code, GP_SCRATCH_REG, cfg->arch.reg_in0 + ainfo->reg + i, sizeof (gpointer), 0);
						break;
					case AggregateSingleHFA:
						ia64_stfs_inc_imm_hint (code, GP_SCRATCH_REG, ainfo->reg + i, 4, 0);
						break;
					case AggregateDoubleHFA:
						ia64_stfd_inc_imm_hint (code, GP_SCRATCH_REG, ainfo->reg + i, sizeof (gpointer), 0);
						break;
					default:
						NOT_IMPLEMENTED;
					}
				}
				break;
			default:
				g_assert_not_reached ();
			}
		}

		if (inst->opcode == OP_REGVAR) {
			/* Argument allocated to (non-volatile) register */
			switch (ainfo->storage) {
			case ArgInIReg:
				if (inst->dreg != cfg->arch.reg_in0 + ainfo->reg)
					ia64_mov (code, inst->dreg, cfg->arch.reg_in0 + ainfo->reg);
				break;
			case ArgOnStack:
				ia64_adds_imm (code, GP_SCRATCH_REG, 16 + ainfo->offset, cfg->frame_reg);
				ia64_ld8 (code, inst->dreg, GP_SCRATCH_REG);
				break;
			default:
				NOT_IMPLEMENTED;
			}
		}
	}

	if (method->save_lmf) {
		/* No LMF on IA64 */
	}

	ia64_codegen_close (code);

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code.buf = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code.buf, TRUE);

	cfg->code_len = code.buf - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

	cfg->arch.prolog_end_offset = cfg->code_len;

	return code.buf;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int i, pos;
	int max_epilog_size = 16 * 4;
	Ia64CodegenState code;
	guint8 *buf;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 1024;

	cfg->arch.epilog_begin_offset = cfg->code_len;

	while (cfg->code_len + max_epilog_size > cfg->code_size) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

	/* FIXME: Emit unwind info */

	buf = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		buf = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, buf, TRUE);

	ia64_codegen_init (code, buf);

	/* the code restoring the registers must be kept in sync with OP_JMP */
	pos = 0;
	
	if (method->save_lmf) {
		/* No LMF on IA64 */
	}

	/* Load returned vtypes into registers if needed */
	cinfo = get_call_info (cfg, cfg->mempool, mono_method_signature (method), FALSE);
	ainfo = &cinfo->ret;
	switch (ainfo->storage) {
	case ArgAggregate:
		if (ainfo->nslots != ainfo->nregs)
			NOT_IMPLEMENTED;

		g_assert (cfg->ret->opcode == OP_REGOFFSET);
		ia64_adds_imm (code, GP_SCRATCH_REG, cfg->ret->inst_offset, cfg->ret->inst_basereg);
		for (i = 0; i < ainfo->nregs; ++i) {
			switch (ainfo->atype) {
			case AggregateNormal:
				ia64_ld8_inc_imm_hint (code, ainfo->reg + i, GP_SCRATCH_REG, sizeof (gpointer), 0);
				break;
			case AggregateSingleHFA:
				ia64_ldfs_inc_imm_hint (code, ainfo->reg + i, GP_SCRATCH_REG, 4, 0);
				break;
			case AggregateDoubleHFA:
				ia64_ldfd_inc_imm_hint (code, ainfo->reg + i, GP_SCRATCH_REG, sizeof (gpointer), 0);
				break;
			default:
				g_assert_not_reached ();
			}
		}
		break;
	default:
		break;
	}

	ia64_begin_bundle (code);

	code.region_start = cfg->native_code;

	/* Label the unwind state at the start of the exception throwing region */
	//ia64_unw_label_state (code, 1234);

	if (cfg->arch.stack_alloc_size) {
		if (cfg->arch.omit_fp) {
			if (ia64_is_imm14 (cfg->arch.stack_alloc_size)) {
				ia64_unw_pop_frames (code, 1);
				ia64_adds_imm (code, IA64_SP, (cfg->arch.stack_alloc_size), IA64_SP);
			} else {
				ia64_movl (code, GP_SCRATCH_REG, cfg->arch.stack_alloc_size);
				ia64_unw_pop_frames (code, 1);
				ia64_add (code, IA64_SP, GP_SCRATCH_REG, IA64_SP);
			}
		}
		else {
			ia64_unw_pop_frames (code, 1);
			ia64_mov (code, IA64_SP, cfg->arch.reg_saved_sp);
		}
	}
	ia64_mov_to_ar_i (code, IA64_PFS, cfg->arch.reg_saved_ar_pfs);
	ia64_mov_ret_to_br (code, IA64_B0, cfg->arch.reg_saved_b0);
	ia64_br_ret_reg (code, IA64_B0);

	ia64_codegen_close (code);

	cfg->arch.r_epilog = mono_ia64_create_unwind_region (&code);
	cfg->arch.r_pro->next = cfg->arch.r_epilog;

	cfg->code_len = code.buf - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int i, nthrows;
	Ia64CodegenState code;
	gboolean empty = TRUE;
	//unw_dyn_region_info_t *r_exceptions;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];
	guint32 code_size = 0;

	/* Compute needed space */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			code_size += 256;
		if (patch_info->type == MONO_PATCH_INFO_R8)
			code_size += 8 + 7; /* sizeof (double) + alignment */
		if (patch_info->type == MONO_PATCH_INFO_R4)
			code_size += 4 + 7; /* sizeof (float) + alignment */
	}

	if (code_size == 0)
		return;

	while (cfg->code_len + code_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

	ia64_codegen_init (code, cfg->native_code + cfg->code_len);

	/* The unwind state here is the same as before the epilog */
	//ia64_unw_copy_state (code, 1234);

	/* add code to raise exceptions */
	/* FIXME: Optimize this */
	nthrows = 0;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			guint8* throw_ip;
			guint8* buf;
			guint64 exc_token_index;

			exc_class = mono_class_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			g_assert (exc_class);
			exc_token_index = mono_metadata_token_index (exc_class->type_token);
			throw_ip = cfg->native_code + patch_info->ip.i;

			ia64_begin_bundle (code);

			ia64_patch (cfg->native_code + patch_info->ip.i, code.buf);

			/* Find a throw sequence for the same exception class */
			for (i = 0; i < nthrows; ++i)
				if (exc_classes [i] == exc_class)
					break;

			if (i < nthrows) {
				gint64 offset = exc_throw_end [i] - 16 - throw_ip;

				if (ia64_is_adds_imm (offset))
					ia64_adds_imm (code, cfg->arch.reg_out0 + 1, offset, IA64_R0);
				else
					ia64_movl (code, cfg->arch.reg_out0 + 1, offset);

				buf = code.buf + code.nins;
				ia64_br_cond_pred (code, 0, 0);
				ia64_begin_bundle (code);
				ia64_patch (buf, exc_throw_start [i]);

				patch_info->type = MONO_PATCH_INFO_NONE;
			}
			else {
				/* Arg1 */
				buf = code.buf;
				ia64_movl (code, cfg->arch.reg_out0 + 1, 0);

				ia64_begin_bundle (code);

				if (nthrows < 16) {
					exc_classes [nthrows] = exc_class;
					exc_throw_start [nthrows] = code.buf;
				}

				/* Arg2 */
				if (ia64_is_adds_imm (exc_token_index))
					ia64_adds_imm (code, cfg->arch.reg_out0 + 0, exc_token_index, IA64_R0);
				else
					ia64_movl (code, cfg->arch.reg_out0 + 0, exc_token_index);

				patch_info->data.name = "mono_arch_throw_corlib_exception";
				patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
				patch_info->ip.i = code.buf + code.nins - cfg->native_code;

				/* Indirect call */
				ia64_movl (code, GP_SCRATCH_REG, 0);
				ia64_ld8_inc_imm (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, 8);
				ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2);
				ia64_ld8 (code, IA64_GP, GP_SCRATCH_REG);

				ia64_br_call_reg (code, IA64_B0, IA64_B6);

				/* Patch up the throw offset */
				ia64_begin_bundle (code);

				ia64_patch (buf, (gpointer)(code.buf - 16 - throw_ip));

				if (nthrows < 16) {
					exc_throw_end [nthrows] = code.buf;
					nthrows ++;
				}
			}

			empty = FALSE;
			break;
		}
		default:
			break;
		}
	}

	if (!empty)
		/* The unwinder needs this to work */
		ia64_break_i (code, 0);

	ia64_codegen_close (code);

	/* FIXME: */
	//r_exceptions = mono_ia64_create_unwind_region (&code);
	//cfg->arch.r_epilog = r_exceptions;

	cfg->code_len = code.buf - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	Ia64CodegenState code;
	CallInfo *cinfo = NULL;
	MonoMethodSignature *sig;
	MonoInst *ins;
	int i, n, stack_area = 0;

	ia64_codegen_init (code, p);

	/* Keep this in sync with mono_arch_get_argument_info */

	if (enable_arguments) {
		/* Allocate a new area on the stack and save arguments there */
		sig = mono_method_signature (cfg->method);

		cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);

		n = sig->param_count + sig->hasthis;

		stack_area = ALIGN_TO (n * 8, 16);

		if (n) {
			ia64_movl (code, GP_SCRATCH_REG, stack_area);

			ia64_sub (code, IA64_SP, IA64_SP, GP_SCRATCH_REG);

			/* FIXME: Allocate out registers */

			ia64_mov (code, cfg->arch.reg_out0 + 1, IA64_SP);

			/* Required by the ABI */
			ia64_adds_imm (code, IA64_SP, -16, IA64_SP);

			add_patch_info (cfg, code, MONO_PATCH_INFO_METHODCONST, cfg->method);
			ia64_movl (code, cfg->arch.reg_out0 + 0, 0);

			/* Save arguments to the stack */
			for (i = 0; i < n; ++i) {
				ins = cfg->args [i];

				if (ins->opcode == OP_REGVAR) {
					ia64_movl (code, GP_SCRATCH_REG, (i * 8));
					ia64_add (code, GP_SCRATCH_REG, cfg->arch.reg_out0 + 1, GP_SCRATCH_REG);
					ia64_st8 (code, GP_SCRATCH_REG, ins->dreg);
				}
				else {
					ia64_movl (code, GP_SCRATCH_REG, ins->inst_offset);
					ia64_add (code, GP_SCRATCH_REG, ins->inst_basereg, GP_SCRATCH_REG);
					ia64_ld8 (code, GP_SCRATCH_REG2, GP_SCRATCH_REG);
					ia64_movl (code, GP_SCRATCH_REG, (i * 8));				
					ia64_add (code, GP_SCRATCH_REG, cfg->arch.reg_out0 + 1, GP_SCRATCH_REG);
					ia64_st8 (code, GP_SCRATCH_REG, GP_SCRATCH_REG2);
				}
			}
		}
		else
			ia64_mov (code, cfg->arch.reg_out0 + 1, IA64_R0);
	}
	else
		ia64_mov (code, cfg->arch.reg_out0 + 1, IA64_R0);

	add_patch_info (cfg, code, MONO_PATCH_INFO_METHODCONST, cfg->method);
	ia64_movl (code, cfg->arch.reg_out0 + 0, 0);

	code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func);

	if (enable_arguments && stack_area) {
		ia64_movl (code, GP_SCRATCH_REG, stack_area);

		ia64_add (code, IA64_SP, IA64_SP, GP_SCRATCH_REG);

		ia64_adds_imm (code, IA64_SP, 16, IA64_SP);
	}

	ia64_codegen_close (code);

	return code.buf;
}

void*
mono_arch_instrument_epilog_full (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers)
{
	Ia64CodegenState code;
	CallInfo *cinfo = NULL;
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig = mono_method_signature (cfg->method);

	ia64_codegen_init (code, p);

	cinfo = get_call_info (cfg, cfg->mempool, sig, FALSE);

	/* Save return value + pass it to func */
	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		ia64_mov (code, cfg->arch.reg_saved_return_val, cinfo->ret.reg);
		ia64_mov (code, cfg->arch.reg_out0 + 1, cinfo->ret.reg);
		break;
	case ArgInFloatReg:
		ia64_adds_imm (code, IA64_SP, -16, IA64_SP);
		ia64_adds_imm (code, GP_SCRATCH_REG, 16, IA64_SP);
		ia64_stfd_hint (code, GP_SCRATCH_REG, cinfo->ret.reg, 0);
		ia64_fmov (code, 8 + 1, cinfo->ret.reg);
		break;
	case ArgValuetypeAddrInIReg:
		ia64_mov (code, cfg->arch.reg_out0 + 1, cfg->arch.reg_in0 + cinfo->ret.reg);
		break;
	case ArgAggregate:
		NOT_IMPLEMENTED;
		break;
	default:
		break;
	}

	add_patch_info (cfg, code, MONO_PATCH_INFO_METHODCONST, method);
	ia64_movl (code, cfg->arch.reg_out0 + 0, 0);
	code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func);

	/* Restore return value */
	switch (cinfo->ret.storage) {
	case ArgNone:
		break;
	case ArgInIReg:
		ia64_mov (code, cinfo->ret.reg, cfg->arch.reg_saved_return_val);
		break;
	case ArgInFloatReg:
		ia64_adds_imm (code, GP_SCRATCH_REG, 16, IA64_SP);
		ia64_ldfd (code, cinfo->ret.reg, GP_SCRATCH_REG);
		break;
	case ArgValuetypeAddrInIReg:
		break;
	case ArgAggregate:
		break;
	default:
		break;
	}

	ia64_codegen_close (code);

	return code.buf;
}

void
mono_arch_save_unwind_info (MonoCompile *cfg)
{
	unw_dyn_info_t *di;

	/* FIXME: Unregister this for dynamic methods */

	di = g_malloc0 (sizeof (unw_dyn_info_t));
	di->start_ip = (unw_word_t) cfg->native_code;
	di->end_ip = (unw_word_t) cfg->native_code + cfg->code_len;
	di->gp = 0;
	di->format = UNW_INFO_FORMAT_DYNAMIC;
	di->u.pi.name_ptr = (unw_word_t)mono_method_full_name (cfg->method, TRUE);
	di->u.pi.regions = cfg->arch.r_pro;

	_U_dyn_register (di);

	/*
	{
		unw_dyn_region_info_t *region = di->u.pi.regions;

		printf ("Unwind info for method %s:\n", mono_method_full_name (cfg->method, TRUE));
		while (region) {
			printf ("    [Region: %d]\n", region->insn_count);
			region = region->next;
		}
	}
	*/
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	guint8* p = (guint8*)((guint64)code & ~(0x3f));
	guint8* end = (guint8*)((guint64)code + size);

#ifdef __INTEL_COMPILER
	/* icc doesn't define an fc.i instrinsic, but fc==fc.i on itanium 2 */
	while (p < end) {
		__fc ((guint64)p);
		p += 32;
	}
#else
	while (p < end) {
		__asm__ __volatile__ ("fc.i %0"::"r"(p));
		/* FIXME: This could be increased to 128 on some cpus */
		p += 32;
	}
#endif
}

void
mono_arch_flush_register_windows (void)
{
	/* Not needed because of libunwind */
}

gboolean 
mono_arch_is_inst_imm (gint64 imm)
{
	/* The lowering pass will take care of it */

	return TRUE;
}

/*
 * Determine whenever the trap whose info is in SIGINFO is caused by
 * integer overflow.
 */
gboolean
mono_arch_is_int_overflow (void *sigctx, void *info)
{
	/* Division is emulated with explicit overflow checks */
	return FALSE;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	NOT_IMPLEMENTED;

	return 0;
}

gpointer*
mono_arch_get_delegate_method_ptr_addr (guint8* code, mgreg_t *regs)
{
	NOT_IMPLEMENTED;

	return NULL;
}

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

#ifdef MONO_ARCH_HAVE_IMT

/*
 * LOCKING: called with the domain lock held
 */
gpointer
mono_arch_build_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count,
	gpointer fail_tramp)
{
	int i;
	int size = 0;
	guint8 *start, *buf;
	Ia64CodegenState code;

	size = count * 256;
	buf = g_malloc0 (size);
	ia64_codegen_init (code, buf);

	/* IA64_R9 contains the IMT method */

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		ia64_begin_bundle (code);
		item->code_target = (guint8*)code.buf + code.nins;
		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case) {
					ia64_movl (code, GP_SCRATCH_REG, item->key);
					ia64_cmp_eq (code, 6, 7, IA64_R9, GP_SCRATCH_REG);
				}
				item->jmp_code = (guint8*)code.buf + code.nins;
				ia64_br_cond_pred (code, 7, 0);

				if (item->has_target_code) {
					ia64_movl (code, GP_SCRATCH_REG, item->value.target_code);
				} else {
					ia64_movl (code, GP_SCRATCH_REG, &(vtable->vtable [item->value.vtable_slot]));
					ia64_ld8 (code, GP_SCRATCH_REG, GP_SCRATCH_REG);
				}
				ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
				ia64_br_cond_reg (code, IA64_B6);

				if (fail_case) {
					ia64_begin_bundle (code);
					ia64_patch (item->jmp_code, (guint8*)code.buf + code.nins);
					ia64_movl (code, GP_SCRATCH_REG, fail_tramp);
					ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
					ia64_br_cond_reg (code, IA64_B6);
					item->jmp_code = NULL;
				}
			} else {
				/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
				g_assert_not_reached ();
#endif
				ia64_movl (code, GP_SCRATCH_REG, &(vtable->vtable [item->value.vtable_slot]));
				ia64_ld8 (code, GP_SCRATCH_REG, GP_SCRATCH_REG);
				ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG);
				ia64_br_cond_reg (code, IA64_B6);
#if ENABLE_WRONG_METHOD_CHECK
				g_assert_not_reached ();
#endif
			}
		} else {
			ia64_movl (code, GP_SCRATCH_REG, item->key);
			ia64_cmp_geu (code, 6, 7, IA64_R9, GP_SCRATCH_REG);
			item->jmp_code = (guint8*)code.buf + code.nins;
			ia64_br_cond_pred (code, 6, 0);
		}
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				ia64_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
			}
		}
	}

	ia64_codegen_close (code);
	g_assert (code.buf - buf <= size);

	size = code.buf - buf;
	if (fail_tramp) {
		start = mono_method_alloc_generic_virtual_thunk (domain, size + 16);
		start = (gpointer)ALIGN_TO (start, 16);
	} else {
		start = mono_domain_code_reserve (domain, size);
	}
	memcpy (start, buf, size);

	mono_arch_flush_icache (start, size);

	mono_stats.imt_thunks_size += size;

	return start;
}

MonoMethod*
mono_arch_find_imt_method (mgreg_t *regs, guint8 *code)
{
	return (MonoMethod*)regs [IA64_R9];
}

void
mono_arch_emit_imt_argument (MonoCompile *cfg, MonoCallInst *call, MonoInst *imt_arg)
{
	/* Done by the implementation of the CALL_MEMBASE opcodes */
}
#endif

gpointer
mono_arch_get_this_arg_from_call (mgreg_t *regs, guint8 *code)
{
	return (gpointer)regs [IA64_R10];
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	return NULL;
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	if (cmethod->klass->image == mono_defaults.corlib &&
		(strcmp (cmethod->klass->name_space, "System.Threading") == 0) &&
		(strcmp (cmethod->klass->name, "Interlocked") == 0)) {

		/* 
		 * We don't use the generic version in mini_emit_inst_for_method () since we
		 * ia64 has atomic_add_imm opcodes.
		 */
		if (strcmp (cmethod->name, "Increment") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_IMM_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_IMM_NEW_I8;
			else
				g_assert_not_reached ();
			MONO_INST_NEW (cfg, ins, opcode);
			ins->dreg = mono_alloc_preg (cfg);
			ins->inst_imm = 1;
			ins->inst_basereg = args [0]->dreg;
			ins->inst_offset = 0;
			MONO_ADD_INS (cfg->cbb, ins);
		} else if (strcmp (cmethod->name, "Decrement") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_IMM_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_IMM_NEW_I8;
			else
				g_assert_not_reached ();
			MONO_INST_NEW (cfg, ins, opcode);
			ins->dreg = mono_alloc_preg (cfg);
			ins->inst_imm = -1;
			ins->inst_basereg = args [0]->dreg;
			ins->inst_offset = 0;
			MONO_ADD_INS (cfg->cbb, ins);
		} else if (strcmp (cmethod->name, "Add") == 0) {
			guint32 opcode;
			gboolean is_imm = FALSE;
			gint64 imm = 0;

			if ((args [1]->opcode == OP_ICONST) || (args [1]->opcode == OP_I8CONST)) {
				imm = (args [1]->opcode == OP_ICONST) ? args [1]->inst_c0 : args [1]->inst_l;

				is_imm = (imm == 1 || imm == 4 || imm == 8 || imm == 16 || imm == -1 || imm == -4 || imm == -8 || imm == -16);
			}

			if (is_imm) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_ATOMIC_ADD_IMM_NEW_I4;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_ATOMIC_ADD_IMM_NEW_I8;
				else
					g_assert_not_reached ();

				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = args [0]->dreg;
				ins->inst_offset = 0;
				ins->inst_imm = imm;
				ins->type = (opcode == OP_ATOMIC_ADD_IMM_NEW_I4) ? STACK_I4 : STACK_I8;
			} else {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_ATOMIC_ADD_NEW_I4;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_ATOMIC_ADD_NEW_I8;
				else
					g_assert_not_reached ();

				MONO_INST_NEW (cfg, ins, opcode);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = args [0]->dreg;
				ins->inst_offset = 0;
				ins->sreg2 = args [1]->dreg;
				ins->type = (opcode == OP_ATOMIC_ADD_NEW_I4) ? STACK_I4 : STACK_I8;
			}
			MONO_ADD_INS (cfg->cbb, ins);
		}
	}

	return ins;
}

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	return 0;
}

MonoInst*
mono_arch_get_domain_intrinsic (MonoCompile* cfg)
{
	return mono_get_domain_intrinsic (cfg);
}

gpointer
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	/* FIXME: implement */
	g_assert_not_reached ();
}
