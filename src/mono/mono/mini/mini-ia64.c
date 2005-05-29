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

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-math.h>

#include "trace.h"
#include "mini-ia64.h"
#include "inssel.h"
#include "cpu-ia64.h"

static gint lmf_tls_offset = -1;
static gint appdomain_tls_offset = -1;
static gint thread_tls_offset = -1;

const char * const ia64_desc [OP_LAST];
static const char*const * ins_spec = ia64_desc;

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

#define SIGNAL_STACK_SIZE (64 * 1024)

#define ARGS_OFFSET 16

#define GP_SCRATCH_REG 31
#define GP_SCRATCH_REG2 30
#define FP_SCRATCH_REG 32

#define LOOP_ALIGNMENT 8
#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

#define NOT_IMPLEMENTED g_assert_not_reached ()

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

typedef enum {
	ArgInIReg,
	ArgInFloatReg,
	ArgOnStack,
	ArgValuetypeAddrInIReg,
	ArgValuetypeInReg,
	ArgNone /* only in pair_storage */
} ArgStorage;

typedef struct {
	gint16 offset;
	gint8  reg;
	ArgStorage storage;

	/* Only if storage == ArgValuetypeInReg */
	ArgStorage pair_storage [2];
	gint8 pair_regs [2];
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	guint32 freg_usage;
	gboolean need_stack_align;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a) if (cfg->verbose_level > 1) a

#define NEW_ICONST(cfg,dest,val) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = OP_ICONST;	\
		(dest)->inst_c0 = (val);	\
		(dest)->type = STACK_I4;	\
	} while (0)

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
		ainfo->storage = ArgInFloatReg;
		ainfo->reg = 8 + *fr;
		(*fr) += 1;
		(*gr) += 1;
    }
}

static void
add_valuetype (MonoMethodSignature *sig, ArgInfo *ainfo, MonoType *type,
	       gboolean is_return,
	       guint32 *gr, guint32 *fr, guint32 *stack_size)
{
	guint32 size;
	MonoClass *klass;

	klass = mono_class_from_mono_type (type);
	if (sig->pinvoke) 
		size = mono_type_native_stack_size (&klass->byval_arg, NULL);
	else 
		size = mono_type_stack_size (&klass->byval_arg, NULL);

	if (!sig->pinvoke || (size == 0)) {
		/* Allways pass in memory */
		ainfo->offset = *stack_size;
		*stack_size += ALIGN_TO (size, 8);
		ainfo->storage = ArgOnStack;

		return;
	}
	else
		NOT_IMPLEMENTED;
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * For IA64, see the "Itanium Software Conventions and Runtime Architecture
 * Gude" document for more information.
 */
static CallInfo*
get_call_info (MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i, gr, fr;
	MonoType *ret_type;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;

	cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	gr = 0;
	fr = 0;

	/* return value */
	{
		ret_type = mono_type_get_underlying_type (sig->ret);
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
		case MONO_TYPE_VALUETYPE: {
			guint32 tmp_gr = 0, tmp_fr = 0, tmp_stacksize = 0;

			add_valuetype (sig, &cinfo->ret, sig->ret, TRUE, &tmp_gr, &tmp_fr, &tmp_stacksize);
			if (cinfo->ret.storage == ArgOnStack)
				/* The caller passes the address where the value is stored */
				add_general (&gr, &stack_size, &cinfo->ret);
			if (cinfo->ret.storage == ArgInIReg)
				cinfo->ret.storage = ArgValuetypeAddrInIReg;
			break;
		}
		case MONO_TYPE_TYPEDBYREF:
			/* Same as a valuetype with size 24 */
			add_general (&gr, &stack_size, &cinfo->ret);
			if (cinfo->ret.storage == ArgInIReg)
				cinfo->ret.storage = ArgValuetypeAddrInIReg;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/* this */
	if (sig->hasthis)
		add_general (&gr, &stack_size, cinfo->args + 0);

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == 0)) {
		gr = PARAM_REGS;
		fr = FLOAT_PARAM_REGS;
		
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie);
	}

	for (i = 0; i < sig->param_count; ++i) {
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
		case MONO_TYPE_VALUETYPE:
			/* FIXME: */
			/* We allways pass valuetypes on the stack */
			add_valuetype (sig, ainfo, sig->params [i], FALSE, &gr, &fr, &stack_size);
			break;
		case MONO_TYPE_TYPEDBYREF:
			stack_size += sizeof (MonoTypedRef);
			ainfo->storage = ArgOnStack;
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
	g_assert_not_reached ();

	return 0;
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
	*exclude_mask = 0;

	return 0;
}

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	t = mono_type_get_underlying_type (t);
	switch (t->type) {
	case MONO_TYPE_I1:
	case MONO_TYPE_U1:
	case MONO_TYPE_I2:
	case MONO_TYPE_U2:
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

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		if (is_regsize_var (ins->inst_vtype)) {
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

	if (cfg->arch.reg_local0 > 0)
		/* Already done */
		return;

	cinfo = get_call_info (mono_method_signature (cfg->method), FALSE);

	/* Three registers are reserved for use by the prolog/epilog */
	cfg->arch.reg_in0 = 32;
	cfg->arch.reg_local0 = cfg->arch.reg_in0 + cinfo->reg_usage + 3;
	cfg->arch.reg_out0 = cfg->arch.reg_local0 + 8;

	cfg->arch.reg_saved_ar_pfs = cfg->arch.reg_local0 - 1;
	cfg->arch.reg_saved_b0 = cfg->arch.reg_local0 - 2;
	cfg->arch.reg_saved_sp = cfg->arch.reg_local0 - 3;

	g_free (cinfo);
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
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset;
	guint32 locals_stack_size, locals_stack_align;
	gint32 *offsets;
	CallInfo *cinfo;

	mono_ia64_alloc_stacked_registers (m);

	header = mono_method_get_header (m->method);

	sig = mono_method_signature (m->method);

	cinfo = get_call_info (sig, FALSE);

	/*
	 * We use the ABI calling conventions for managed code as well.
	 * Exception: valuetypes are never passed or returned in registers.
	 */

	/* Locals are allocated backwards from %fp */
	m->frame_reg = m->arch.reg_saved_sp;
	offset = 0;

	if (m->method->save_lmf) {
		/* FIXME: */
#if 0
		/* Reserve stack space for saving LMF + argument regs */
		offset += sizeof (MonoLMF);
		m->arch.lmf_offset = offset;
#endif
	}

	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = cinfo->ret.reg;
			break;
		case ArgInFloatReg:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = cinfo->ret.reg;
			break;
		case ArgValuetypeAddrInIReg:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = m->arch.reg_in0 + cinfo->ret.reg;
			break;
		default:
			g_assert_not_reached ();
		}
		m->ret->dreg = m->ret->inst_c0;
	}

	/* Allocate locals */
	offsets = mono_allocate_stack_slots (m, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align) {
		offset += (locals_stack_align - 1);
		offset &= ~(locals_stack_align - 1);
	}
	for (i = m->locals_start; i < m->num_varinfo; i++) {
		if (offsets [i] != -1) {
			MonoInst *inst = m->varinfo [i];
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = m->frame_reg;
			inst->inst_offset = - (offset + offsets [i]);
			// printf ("allocated local %d to ", i); mono_print_tree_nl (inst);
		}
	}
	g_free (offsets);
	offset += locals_stack_size;

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG)) {
		g_assert (cinfo->sig_cookie.storage == ArgOnStack);
		m->sig_cookie = cinfo->sig_cookie.offset + ARGS_OFFSET;
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		inst = m->varinfo [i];
		if (inst->opcode != OP_REGVAR) {
			ArgInfo *ainfo = &cinfo->args [i];
			gboolean inreg = TRUE;
			MonoType *arg_type;

			if (sig->hasthis && (i == 0))
				arg_type = &mono_defaults.object_class->byval_arg;
			else
				arg_type = sig->params [i - sig->hasthis];

			/* FIXME: Allocate volatile arguments to registers */
			if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
				inreg = FALSE;

			inst->opcode = OP_REGOFFSET;

			switch (ainfo->storage) {
			case ArgInIReg:
				inst->opcode = OP_REGVAR;
				inst->dreg = m->arch.reg_in0 + ainfo->reg;
				break;
			case ArgInFloatReg:
				/* 
				 * Since float regs are volatile, we save the arguments to
				 * the stack in the prolog.
				 * FIXME: Avoid this if the method contains no calls.
				 */
				inreg = FALSE;
				break;
			case ArgOnStack:
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = m->frame_reg;
				inst->inst_offset = ARGS_OFFSET + ainfo->offset;
				break;
			case ArgValuetypeInReg:
				break;
			default:
				NOT_IMPLEMENTED;
			}

			if (!inreg && (ainfo->storage != ArgOnStack)) {
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = m->frame_reg;
				/* These arguments are saved to the stack in the prolog */
				if (ainfo->storage == ArgValuetypeInReg) {
					NOT_IMPLEMENTED;
					offset += 2 * sizeof (gpointer);
				}
				else
					offset += sizeof (gpointer);
				inst->inst_offset = - offset;
			}
		}
	}

	m->stack_offset = offset;

	g_free (cinfo);
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	g_assert_not_reached ();
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, MonoInst *arg, ArgStorage storage, int reg, MonoInst *tree)
{
	switch (storage) {
	case ArgInIReg:
		arg->opcode = OP_OUTARG_REG;
		arg->inst_left = tree;
		arg->inst_right = (MonoInst*)call;
		arg->unused = reg;
		call->used_iregs |= 1 << reg;
		break;
	case ArgInFloatReg:
		arg->opcode = OP_OUTARG_FREG;
		arg->inst_left = tree;
		arg->inst_right = (MonoInst*)call;
		arg->unused = reg;
		call->used_fregs |= 1 << reg;
		break;
	default:
		g_assert_not_reached ();
	}
}

/* 
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 * Issue: who does the spilling if needed, and when?
 */
MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual)
{
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n, stack_size;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	stack_size = 0;

	mono_ia64_alloc_stacked_registers (cfg);

	sig = call->signature;
	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (sig, sig->pinvoke);

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			MonoMethodSignature *tmp_sig;

			NOT_IMPLEMENTED;

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
			sig_arg->inst_p0 = tmp_sig;

			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			arg->inst_left = sig_arg;
			arg->type = STACK_PTR;

			/* prepend, so they get reversed */
			arg->next = call->out_args;
			call->out_args = arg;
		}

		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instruction */
			in = call->args [i];
		} else {
			MonoType *arg_type;

			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code = in->cil_code;
			arg->inst_left = in;
			arg->type = in->type;
			/* prepend, so they get reversed */
			arg->next = call->out_args;
			call->out_args = arg;

			if (sig->hasthis && (i == 0))
				arg_type = &mono_defaults.object_class->byval_arg;
			else
				arg_type = sig->params [i - sig->hasthis];

			if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(arg_type))) {
				MonoInst *stack_addr;
				gint align;
				guint32 size;

				if (arg_type->type == MONO_TYPE_TYPEDBYREF) {
					size = sizeof (MonoTypedRef);
					align = sizeof (gpointer);
				}
				else
				if (sig->pinvoke)
					size = mono_type_native_stack_size (&in->klass->byval_arg, &align);
				else
					size = mono_type_stack_size (&in->klass->byval_arg, &align);
				if (ainfo->storage == ArgValuetypeInReg) {
					NOT_IMPLEMENTED;
				}

				MONO_INST_NEW (cfg, stack_addr, OP_REGOFFSET);
				stack_addr->inst_basereg = IA64_SP;
				stack_addr->inst_offset = 16 + ainfo->offset;
				stack_addr->inst_imm = size;

				arg->opcode = OP_OUTARG_VT;
				arg->inst_right = stack_addr;
			}
			else {
				switch (ainfo->storage) {
				case ArgInIReg:
					add_outarg_reg (cfg, call, arg, ainfo->storage, cfg->arch.reg_out0 + ainfo->reg, in);
					break;
				case ArgInFloatReg:
					add_outarg_reg (cfg, call, arg, ainfo->storage, ainfo->reg, in);
					break;
				case ArgOnStack:
					if (arg_type->type == MONO_TYPE_R4 && !arg_type->byref) {
						arg->opcode = OP_OUTARG_R4;
					}
					else
						arg->opcode = OP_OUTARG;
					arg->inst_imm = 16 + ainfo->offset;
					break;
				default:
					g_assert_not_reached ();
				}
			}
		}
	}

	call->stack_usage = cinfo->stack_usage;
	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->arch.n_out_regs = MAX (cfg->arch.n_out_regs, cinfo->reg_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;

	g_free (cinfo);

	return call;
}

static void
peephole_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *last_ins = NULL;
	ins = bb->code;

	while (ins) {
		switch (ins->opcode) {
		case CEE_CONV_I8:
		case CEE_CONV_I:
		case OP_MOVE:
		case OP_FMOVE:
		case OP_SETREG:
			/*
			 * Removes:
			 *
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				if (last_ins)
					last_ins->next = ins->next;				
				ins = ins->next;
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

static int
opcode_to_ia64_cmp (int opcode)
{
	switch (opcode) {
	case CEE_BEQ:
		return OP_IA64_CMP_EQ;
	case CEE_BNE_UN:
		return OP_IA64_CMP_NE;
	case CEE_BLE:
		return OP_IA64_CMP_LE;
	case CEE_BGE:
		return OP_IA64_CMP_GE;
	case CEE_BLT:
		return OP_IA64_CMP_LT;
	case CEE_BGT:
		return OP_IA64_CMP_GT;
	case CEE_BLE_UN:
		return OP_IA64_CMP_LE_UN;
	case CEE_BGE_UN:
		return OP_IA64_CMP_GE_UN;
	case CEE_BLT_UN:
		return OP_IA64_CMP_LT_UN;
	case CEE_BGT_UN:
		return OP_IA64_CMP_GT_UN;
	case OP_COND_EXC_GT_UN:
		return OP_IA64_CMP_GT_UN;
	case OP_COND_EXC_LE_UN:
		return OP_IA64_CMP_LE_UN;
	case OP_COND_EXC_NE_UN:
		return OP_IA64_CMP_NE;
	case OP_COND_EXC_LT:
		return OP_IA64_CMP_LT;
	case OP_COND_EXC_GT:
		return OP_IA64_CMP_GT;
	case OP_CEQ:
		return OP_IA64_CMP_EQ;
	case OP_CLT:
		return OP_IA64_CMP_LT;
	case OP_CGT:
		return OP_IA64_CMP_GT;
	case OP_CLT_UN:
		return OP_IA64_CMP_LT_UN;
	case OP_CGT_UN:
		return OP_IA64_CMP_GT_UN;
	case OP_IBEQ:
		return OP_IA64_CMP4_EQ;
	case OP_IBNE_UN:
		return OP_IA64_CMP4_NE;
	case OP_IBLE:
		return OP_IA64_CMP4_LE;
	case OP_IBLT:
		return OP_IA64_CMP4_LT;
	case OP_IBGT:
		return OP_IA64_CMP4_GT;
	case OP_IBGE:
		return OP_IA64_CMP4_GE;
	case OP_IBLE_UN:
		return OP_IA64_CMP4_LE_UN;
	case OP_IBLT_UN:
		return OP_IA64_CMP4_LT_UN;
	case OP_IBGE_UN:
		return OP_IA64_CMP4_GE_UN;
	case OP_IBGT_UN:
		return OP_IA64_CMP4_GT_UN;
	case OP_ICEQ:
		return OP_IA64_CMP4_EQ;
	case OP_ICLT:
		return OP_IA64_CMP4_LT;
	case OP_ICGT:
		return OP_IA64_CMP4_GT;
	case OP_ICLT_UN:
		return OP_IA64_CMP4_LT_UN;
	case OP_ICGT_UN:
		return OP_IA64_CMP4_GT_UN;
	case OP_FBEQ:
	case OP_FCEQ:
		return OP_IA64_FCMP_EQ;
	case OP_FBNE_UN:
		return OP_IA64_FCMP_NE;
	case OP_FBLT:
	case OP_FCLT:
		return OP_IA64_FCMP_LT;
	case OP_FBLE:
		return OP_IA64_FCMP_LE;
	case OP_FBGT:
	case OP_FCGT:
		return OP_IA64_FCMP_GT;
	case OP_FBGE:
		return OP_IA64_FCMP_GE;
	case OP_FBLT_UN:
	case OP_FCLT_UN:
		return OP_IA64_FCMP_LT_UN;
	case OP_FCGT_UN:
	case OP_FBGT_UN:
		return OP_IA64_FCMP_GT_UN;
	case OP_FBGE_UN:
		return OP_IA64_FCMP_GE_UN;
	case OP_FBLE_UN:
		return OP_IA64_FCMP_LE_UN;
	default:
		printf ("%s\n", mono_inst_name (opcode));
		NOT_IMPLEMENTED;
		return 0;
	}
}

static int
opcode_to_ia64_cmp_imm (int opcode)
{
	switch (opcode) {
	case CEE_BEQ:
		return OP_IA64_CMP_EQ_IMM;
	case CEE_BNE_UN:
		return OP_IA64_CMP_NE_IMM;
	case CEE_BLE:
		return OP_IA64_CMP_GE_IMM;
	case CEE_BGE:
		return OP_IA64_CMP_LE_IMM;
	case CEE_BLT:
		return OP_IA64_CMP_GT_IMM;
	case CEE_BGT:
		return OP_IA64_CMP_LT_IMM;
	case CEE_BLE_UN:
		return OP_IA64_CMP_GE_UN_IMM;
	case CEE_BGE_UN:
		return OP_IA64_CMP_LE_UN_IMM;
	case CEE_BLT_UN:
		return OP_IA64_CMP_GT_UN_IMM;
	case CEE_BGT_UN:
		return OP_IA64_CMP_LT_UN_IMM;
	case OP_CEQ:
		return OP_IA64_CMP_EQ_IMM;
	case OP_CLT:
		return OP_IA64_CMP_GT_IMM;
	case OP_CGT:
		return OP_IA64_CMP_LT_IMM;
	case OP_CLT_UN:
		return OP_IA64_CMP_GT_UN_IMM;
	case OP_CGT_UN:
		return OP_IA64_CMP_LT_UN_IMM;
	case OP_COND_EXC_EQ:
		return OP_IA64_CMP_EQ_IMM;
	case OP_COND_EXC_GT_UN:
		return OP_IA64_CMP_LT_UN_IMM;
	case OP_COND_EXC_LT:
		return OP_IA64_CMP_GT_IMM;
	case OP_COND_EXC_GT:
		return OP_IA64_CMP_LT_IMM;
	case OP_COND_EXC_LE_UN:
		return OP_IA64_CMP_GE_UN_IMM;
	case OP_COND_EXC_NE_UN:
		return OP_IA64_CMP_NE_IMM;
	case OP_COND_EXC_LT_UN:
		return OP_IA64_CMP_GT_UN_IMM;
	case OP_IBEQ:
		return OP_IA64_CMP4_EQ_IMM;
	case OP_IBNE_UN:
		return OP_IA64_CMP4_NE_IMM;
	case OP_IBLE:
		return OP_IA64_CMP4_GE_IMM;
	case OP_IBGE:
		return OP_IA64_CMP4_LE_IMM;
	case OP_IBLT:
		return OP_IA64_CMP4_GT_IMM;
	case OP_IBGT:
		return OP_IA64_CMP4_LT_IMM;
	case OP_IBLE_UN:
		return OP_IA64_CMP4_GE_UN_IMM;
	case OP_IBGE_UN:
		return OP_IA64_CMP4_LE_UN_IMM;
	case OP_IBLT_UN:
		return OP_IA64_CMP4_GT_UN_IMM;
	case OP_IBGT_UN:
		return OP_IA64_CMP4_LT_UN_IMM;
	case OP_ICEQ:
		return OP_IA64_CMP4_EQ_IMM;
	case OP_ICLT:
		return OP_IA64_CMP4_GT_IMM;
	case OP_ICGT:
		return OP_IA64_CMP4_LT_IMM;
	case OP_ICLT_UN:
		return OP_IA64_CMP4_GT_UN_IMM;
	case OP_ICGT_UN:
		return OP_IA64_CMP4_LT_UN_IMM;
	default:
		printf ("%s\n", mono_inst_name (opcode));
		NOT_IMPLEMENTED;
		return 0;
	}
}

static void
insert_after_ins (MonoBasicBlock *bb, MonoInst *ins, MonoInst *to_insert)
{
	if (ins == NULL) {
		ins = bb->code;
		bb->code = to_insert;
		to_insert->next = ins;
	}
	else {
		to_insert->next = ins->next;
		ins->next = to_insert;
	}
}

#define NEW_INS(cfg,dest,op) do {	\
		(dest) = mono_mempool_alloc0 ((cfg)->mempool, sizeof (MonoInst));	\
		(dest)->opcode = (op);	\
        insert_after_ins (bb, last_ins, (dest)); \
        last_ins = (dest); \
	} while (0)

/*
 * mono_arch_lowering_pass:
 *
 *  Converts complex opcodes into simpler ones so that each IR instruction
 * corresponds to one machine instruction.
 */
static void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next, *temp, *temp2, *temp3, *last_ins = NULL;
	ins = bb->code;

	if (bb->max_ireg > cfg->rs->next_vireg)
		cfg->rs->next_vireg = bb->max_ireg;
	if (bb->max_freg > cfg->rs->next_vfreg)
		cfg->rs->next_vfreg = bb->max_freg;

	while (ins) {
		switch (ins->opcode) {
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM:
		case OP_STORE_MEMBASE_IMM:
			/* There are no store_membase instructions on ia64 */
			NEW_INS (cfg, temp, OP_I8CONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_regstate_next_int (cfg->rs);
			NEW_INS (cfg, temp2, CEE_ADD);
			temp2->sreg1 = ins->inst_destbasereg;
			temp2->sreg2 = temp->dreg;
			temp2->dreg = mono_regstate_next_int (cfg->rs);

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
				temp3->dreg = mono_regstate_next_int (cfg->rs);
				ins->sreg1 = temp3->dreg;
			}

			ins->inst_offset = 0;
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
			if (ia64_is_imm14 (ins->inst_offset)) {
				NEW_INS (cfg, temp2, OP_ADD_IMM);
				temp2->sreg1 = ins->inst_destbasereg;
				temp2->inst_imm = ins->inst_offset;
				temp2->dreg = mono_regstate_next_int (cfg->rs);
			}
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				NEW_INS (cfg, temp2, CEE_ADD);
				temp2->sreg1 = ins->inst_destbasereg;
				temp2->sreg2 = temp->dreg;
				temp2->dreg = mono_regstate_next_int (cfg->rs);
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
			/* There are no load_membase instructions on ia64 */
			if (ia64_is_imm14 (ins->inst_offset)) {
				NEW_INS (cfg, temp2, OP_ADD_IMM);
				temp2->sreg1 = ins->inst_basereg;
				temp2->inst_imm = ins->inst_offset;
				temp2->dreg = mono_regstate_next_int (cfg->rs);
			}
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				NEW_INS (cfg, temp2, CEE_ADD);
				temp2->sreg1 = ins->inst_basereg;
				temp2->sreg2 = temp->dreg;
				temp2->dreg = mono_regstate_next_int (cfg->rs);
			}

			ins->inst_offset = 0;
			ins->inst_basereg = temp2->dreg;
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			/* There are no membase instructions on ia64 */
			if (ia64_is_imm14 (ins->inst_offset)) {
				NEW_INS (cfg, temp2, OP_ADD_IMM);
				temp2->sreg1 = ins->sreg1;
				temp2->inst_imm = ins->inst_offset;
				temp2->dreg = mono_regstate_next_int (cfg->rs);
			}
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				NEW_INS (cfg, temp2, CEE_ADD);
				temp2->sreg1 = ins->sreg1;
				temp2->sreg2 = temp->dreg;
				temp2->dreg = mono_regstate_next_int (cfg->rs);
			}

			NEW_INS (cfg, temp, OP_LOADI8_MEMBASE);
			temp->sreg1 = temp2->dreg;
			temp->dreg = mono_regstate_next_int (cfg->rs);

			ins->sreg1 = temp->dreg;

			switch (ins->opcode) {
			case OP_FCALL_MEMBASE:
				ins->opcode = OP_FCALL_REG;
				break;
			case OP_LCALL_MEMBASE:
				ins->opcode = OP_LCALL_REG;
				break;
			case OP_VCALL_MEMBASE:
				ins->opcode = OP_VCALL_REG;
				break;
			case OP_VOIDCALL_MEMBASE:
				ins->opcode = OP_VOIDCALL_REG;
				break;
			case OP_CALL_MEMBASE:
				ins->opcode = OP_CALL_REG;
				break;
			default:
				g_assert_not_reached ();
			}
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_ISUB_IMM:
		case OP_IAND_IMM:
		case OP_IOR_IMM:
		case OP_IXOR_IMM:
		case OP_ISHL_IMM:
		case OP_ISHR_IMM:
		case OP_ISHR_UN_IMM:
		case OP_AND_IMM:
		case OP_SHL_IMM:
		case OP_LSHL_IMM:
		case OP_LSHR_UN_IMM:
			if ((ins->opcode == OP_ADD_IMM) && (ia64_is_imm14 (ins->inst_imm))) {
				break;
			}

			/* FIXME: There is an alu imm instruction */
			switch (ins->opcode) {
			case OP_ADD_IMM:					
				ins->opcode = CEE_ADD;
				break;
			case OP_IADD_IMM:
				ins->opcode = OP_IADD;
				break;
			case OP_ISUB_IMM:
				ins->opcode = OP_ISUB;
				break;
			case OP_IAND_IMM:
				ins->opcode = OP_IAND;
				break;
			case OP_IOR_IMM:
				ins->opcode = OP_IOR;
				break;
			case OP_IXOR_IMM:
				ins->opcode = OP_IXOR;
				break;
			case OP_ISHL_IMM:
				ins->opcode = OP_ISHL;
				break;
			case OP_ISHR_IMM:
				ins->opcode = OP_ISHR;
				break;
			case OP_ISHR_UN_IMM:
				ins->opcode = OP_ISHR_UN;
				break;
			case OP_AND_IMM:
				ins->opcode = CEE_AND;
				break;
			case OP_SHL_IMM:
				ins->opcode = OP_LSHL;
				break;
			case OP_LSHL_IMM:
				ins->opcode = OP_LSHL;
				break;
			case OP_LSHR_UN_IMM:
				ins->opcode = OP_LSHR_UN;
				break;
			default:
				g_assert_not_reached ();
			}

			if (ins->inst_imm == 0)
				ins->sreg2 = IA64_R0;
			else {
				NEW_INS (cfg, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->sreg2 = temp->dreg;
			}
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM: {
			/* Instead of compare+b<cond>, ia64 has compare<cond>+br */
			gboolean imm;

			/* 
			 * The compare_imm instructions have switched up arguments, and 
			 * some of them take an imm between -127 and 128.
			 */
			next = ins->next;
			switch (next->opcode) {
			case CEE_BGE:
			case CEE_BLT:
			case CEE_BGE_UN:
			case CEE_BLT_UN:
			case OP_COND_EXC_LT:
			case OP_IBGE:
			case OP_IBLT:
			case OP_IBGE_UN:
			case OP_IBLT_UN:
				imm = ia64_is_imm8 (ins->inst_imm - 1);
				break;
			default:
				imm = ia64_is_imm8 (ins->inst_imm);
				break;
			}

			if (imm) {
				ins->opcode = opcode_to_ia64_cmp_imm (next->opcode);
				ins->sreg2 = ins->sreg1;
			}
			else {
				ins->opcode = opcode_to_ia64_cmp (next->opcode);

				if (ins->inst_imm == 0)
					ins->sreg2 = IA64_R0;
				else {
					NEW_INS (cfg, temp, OP_I8CONST);
					temp->inst_c0 = ins->inst_imm;
					temp->dreg = mono_regstate_next_int (cfg->rs);
					ins->sreg2 = temp->dreg;
				}
			}

			switch (next->opcode) {
			case CEE_BEQ:
			case CEE_BNE_UN:
			case CEE_BLE:
			case CEE_BGT:
			case CEE_BLE_UN:
			case CEE_BGT_UN:
			case CEE_BGE:
			case CEE_BLT:
			case CEE_BGE_UN:
			case CEE_BLT_UN:
			case OP_IBEQ:
			case OP_IBNE_UN:
			case OP_IBLE:
			case OP_IBLT:
			case OP_IBGT:
			case OP_IBGE:
			case OP_IBLE_UN:
			case OP_IBLT_UN:
			case OP_IBGE_UN:
			case OP_IBGT_UN:
				next->opcode = OP_IA64_BR_COND;
				if (! (next->flags & MONO_INST_BRLABEL))
					next->inst_target_bb = next->inst_true_bb;
				break;
			case OP_COND_EXC_EQ:
			case OP_COND_EXC_GT:
			case OP_COND_EXC_LT:
			case OP_COND_EXC_GT_UN:
			case OP_COND_EXC_LE_UN:
			case OP_COND_EXC_NE_UN:
			case OP_COND_EXC_LT_UN:
				next->opcode = OP_IA64_COND_EXC;
				break;
			case OP_CEQ:
			case OP_CLT:
			case OP_CGT:
			case OP_CLT_UN:
			case OP_CGT_UN:
			case OP_ICEQ:
			case OP_ICLT:
			case OP_ICGT:
			case OP_ICLT_UN:
			case OP_ICGT_UN:
				next->opcode = OP_IA64_CSET;
				break;
			default:
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
			ins->opcode = opcode_to_ia64_cmp (next->opcode);
			switch (next->opcode) {
			case CEE_BEQ:
			case CEE_BNE_UN:
			case CEE_BLE:
			case CEE_BGE:
			case CEE_BLT:
			case CEE_BGT:
			case CEE_BLE_UN:
			case CEE_BGE_UN:
			case CEE_BLT_UN:
			case CEE_BGT_UN:
			case OP_IBEQ:
			case OP_IBNE_UN:
			case OP_IBLE:
			case OP_IBLT:
			case OP_IBGT:
			case OP_IBGE:
			case OP_IBLE_UN:
			case OP_IBLT_UN:
			case OP_IBGE_UN:
			case OP_IBGT_UN:
			case OP_FBEQ:
			case OP_FBNE_UN:
			case OP_FBLT:
			case OP_FBLT_UN:
			case OP_FBGT:
			case OP_FBGT_UN:
			case OP_FBGE:
			case OP_FBGE_UN:
			case OP_FBLE:
			case OP_FBLE_UN:
				next->opcode = OP_IA64_BR_COND;
				if (! (next->flags & MONO_INST_BRLABEL))
					next->inst_target_bb = next->inst_true_bb;
				break;
			case OP_COND_EXC_LT:
			case OP_COND_EXC_GT:
			case OP_COND_EXC_GT_UN:
			case OP_COND_EXC_LE_UN:
				next->opcode = OP_IA64_COND_EXC;
				break;
			case OP_CEQ:
			case OP_CLT:
			case OP_CGT:
			case OP_CLT_UN:
			case OP_CGT_UN:
			case OP_ICEQ:
			case OP_ICLT:
			case OP_ICGT:
			case OP_ICLT_UN:
			case OP_ICGT_UN:
			case OP_FCEQ:
			case OP_FCLT:
			case OP_FCGT:
			case OP_FCLT_UN:
			case OP_FCGT_UN:
				next->opcode = OP_IA64_CSET;
				break;
			default:
				printf ("%s\n", mono_inst_name (next->opcode));
				NOT_IMPLEMENTED;
			}
			break;
		}
		default:
			break;
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;

	bb->max_ireg = cfg->rs->next_vireg;
	bb->max_freg = cfg->rs->next_vfreg;
}

void
mono_arch_local_regalloc (MonoCompile *cfg, MonoBasicBlock *bb)
{
	if (!bb->code)
		return;

	mono_arch_lowering_pass (cfg, bb);

	mono_local_regalloc (cfg, bb);
}

static Ia64CodegenState
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, Ia64CodegenState code)
{
	CallInfo *cinfo;

	/* Move return value to the target register */
	/* FIXME: do this in the local reg allocator */
	switch (ins->opcode) {
	case OP_VOIDCALL:
	case OP_VOIDCALL_REG:
	case OP_VOIDCALL_MEMBASE:
		break;
	case CEE_CALL:
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
		break;
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
		cinfo = get_call_info (((MonoCallInst*)ins)->signature, FALSE);
		if (cinfo->ret.storage == ArgValuetypeInReg) {
			NOT_IMPLEMENTED;
		}
		g_free (cinfo);
		break;
	default:
		g_assert_not_reached ();
	}

	return code;
}

static Ia64CodegenState
emit_call (MonoCompile *cfg, Ia64CodegenState code, guint32 patch_type, gconstpointer data)
{
	mono_add_patch_info (cfg, code.buf - cfg->native_code, patch_type, data);

	if (patch_type == MONO_PATCH_INFO_ABS) {
		/* Indirect call */
		ia64_movl (code, GP_SCRATCH_REG, 0);
		ia64_ld8_inc_imm_hint (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, 8, 0);
		ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2, 0, 0, 0);
		ia64_ld8 (code, IA64_GP, GP_SCRATCH_REG);
		ia64_br_call_reg (code, IA64_B0, IA64_B6);
	}
	else
		ia64_br_call_hint (code, IA64_B0, 0, 0, 0, 0);

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

	if (cfg->opt & MONO_OPT_PEEPHOLE)
		peephole_pass (cfg, bb);

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

	ins = bb->code;
	while (ins) {
		offset = code.buf - cfg->native_code;

		max_len = ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
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
		case OP_MOVE:
			ia64_mov (code, ins->dreg, ins->sreg1);
			break;
		case CEE_BR:
		case OP_IA64_BR_COND: {
			int pred = 0;
			if (ins->opcode == OP_IA64_BR_COND)
				pred = 6;
			if (ins->flags & MONO_INST_BRLABEL) {
				if (ins->inst_i0->inst_c0) {
					NOT_IMPLEMENTED;
				} else {
					ia64_begin_bundle (code);
					mono_add_patch_info (cfg, code.buf - cfg->native_code, MONO_PATCH_INFO_LABEL, ins->inst_i0);
					ia64_br_cond_hint_pred (code, pred, 0, 0, 0, 0);
				}
			} else {
				if (ins->inst_target_bb->native_offset) {
					gint64 disp = ((gint64)ins->inst_target_bb->native_offset - offset) >> 4;
					ia64_br_cond_hint_pred (code, pred, disp, 0, 0, 0);
				} else {
					ia64_begin_bundle (code);
					mono_add_patch_info (cfg, code.buf - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
					ia64_br_cond_hint_pred (code, pred, 0, 0, 0, 0);
				} 
			}
			break;
		}
		case OP_LABEL:
			ia64_begin_bundle (code);
			ins->inst_c0 = code.buf - cfg->native_code;
			break;
		case OP_BR_REG:
			ia64_mov_to_br (code, IA64_B6, ins->sreg1, 0, 0, 0);
			ia64_br_cond_reg_hint (code, IA64_B6, 0, 0, 0);
			break;
		case CEE_ADD:
		case OP_IADD:
			ia64_add (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_AND:
		case OP_IAND:
			ia64_and (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IOR:
		case CEE_OR:
			ia64_or (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IXOR:
		case CEE_XOR:
			ia64_xor (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_INEG:
		case CEE_NEG:
			ia64_sub (code, ins->dreg, IA64_R0, ins->sreg1);
			break;
		case OP_INOT:
		case CEE_NOT:
			ia64_andcm_imm (code, ins->dreg, -1, ins->sreg1);
			break;
		case OP_ISHL:
			ia64_shl (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR:
		case OP_LSHR:
			ia64_shr (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ISHR_UN:
			ia64_zxt4 (code, GP_SCRATCH_REG, ins->sreg1);
			ia64_shr_u (code, ins->dreg, GP_SCRATCH_REG, ins->sreg2);
			break;
		case CEE_SHL:
		case OP_LSHL:
			ia64_shl (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_LSHR_UN:
			ia64_shr_u (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case CEE_SUB:
		case OP_ISUB:
			ia64_sub (code, ins->dreg, ins->sreg1, ins->sreg2);
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

			ia64_mov (code, ins->dreg, GP_SCRATCH_REG);

			/* FIXME: Set p7 as well */
			break;
		case OP_ADD_IMM:
			ia64_adds_imm (code, ins->dreg, ins->inst_imm, ins->sreg1);
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
			ia64_st8_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_LOADU1_MEMBASE:
			ia64_ld1_hint (code, ins->dreg, ins->inst_basereg, 0);
			break;
		case OP_LOADU2_MEMBASE:
			ia64_ld2_hint (code, ins->dreg, ins->inst_basereg, 0);
			break;
		case OP_LOADU4_MEMBASE:
			ia64_ld4_hint (code, ins->dreg, ins->inst_basereg, 0);
			break;
		case OP_LOADI1_MEMBASE:
			ia64_ld1_hint (code, ins->dreg, ins->inst_basereg, 0);
			ia64_sxt1 (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADI2_MEMBASE:
			ia64_ld2_hint (code, ins->dreg, ins->inst_basereg, 0);
			ia64_sxt2 (code, ins->dreg, ins->dreg);
			break;
		case OP_LOADI4_MEMBASE:
			ia64_ld4_hint (code, ins->dreg, ins->inst_basereg, 0);
			ia64_sxt4 (code, ins->dreg, ins->dreg);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE:
			ia64_ld8_hint (code, ins->dreg, ins->inst_basereg, 0);
			break;
		case OP_SEXT_I1:
			ia64_sxt1 (code, ins->dreg, ins->sreg1);
			break;
		case OP_SEXT_I2:
			ia64_sxt2 (code, ins->dreg, ins->sreg1);
			break;

		case OP_MUL_IMM:
			/* This should be emulated, but rules in inssel.brg generate it */
			switch (ins->inst_imm) {
			case 1:
				ia64_mov (code, ins->dreg, ins->sreg1);
				break;
			case 2:
				ia64_shl_imm (code, ins->dreg, ins->sreg1, 1);
				break;
			case 4:
				ia64_shl_imm (code, ins->dreg, ins->sreg1, 2);
				break;
			case 8:
				ia64_shl_imm (code, ins->dreg, ins->sreg1, 3);
				break;
			default:
				/* FIXME: */
				printf ("A: %d\n", ins->inst_imm);
				NOT_IMPLEMENTED;
			}
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
			/* FIXME: */
			ia64_break_i_pred (code, 6, 0);
			break;
		case OP_COND_EXC_IC:
			/* FIXME: */
			ia64_break_i_pred (code, 7, 0);
			break;
		case OP_IA64_COND_EXC:
			/* FIXME: */
			ia64_break_i_pred (code, 6, 0);
			break;
		case OP_IA64_CSET:
			/* FIXME: Do this with one instruction ? */
			ia64_mov (code, ins->dreg, IA64_R0);
			ia64_add1_pred (code, 6, ins->dreg, IA64_R0, IA64_R0);
			break;
		case CEE_CONV_I1:
			/* FIXME: Is this needed ? */
			ia64_sxt1 (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_I2:
			/* FIXME: Is this needed ? */
			ia64_sxt2 (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_I4:
			/* FIXME: Is this needed ? */
			ia64_sxt4 (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_U1:
			/* FIXME: Is this needed */
			ia64_zxt1 (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_U2:
			/* FIXME: Is this needed */
			ia64_zxt2 (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_I8:
		case CEE_CONV_I:
			ia64_mov (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_U8:
		case CEE_CONV_U:
			ia64_zxt4 (code, ins->dreg, ins->sreg1);
			break;
		case CEE_CONV_OVF_U4:
			/* FIXME: */
			ia64_mov (code, ins->dreg, ins->sreg1);
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
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, ins->inst_p0);
				ia64_movl (code, GP_SCRATCH_REG, 0);
				ia64_ldfd_hint (code, ins->dreg, GP_SCRATCH_REG, 0);
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
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R4, ins->inst_p0);
				ia64_movl (code, GP_SCRATCH_REG, 0);
				ia64_ldfs_hint (code, ins->dreg, GP_SCRATCH_REG, 0);
			}
			break;
		}
		case OP_FMOVE:
			ia64_fmov (code, ins->dreg, ins->sreg1);
			break;
		case OP_STORER8_MEMBASE_REG:
			ia64_stfd_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_STORER4_MEMBASE_REG:
			ia64_stfs_hint (code, ins->inst_destbasereg, ins->sreg1, 0);
			break;
		case OP_LOADR8_MEMBASE:
			ia64_ldfd_hint (code, ins->dreg, ins->inst_basereg, 0);
			break;
		case OP_LOADR4_MEMBASE:
			ia64_ldfs_hint (code, ins->dreg, ins->inst_basereg, 0);
			break;
		case CEE_CONV_R4:
			ia64_setf_sig (code, ins->dreg, ins->sreg1);
			ia64_fcvt_xf (code, ins->dreg, ins->dreg);
			ia64_fnorm_s_sf (code, ins->dreg, ins->dreg, 0);
			break;
		case CEE_CONV_R8:
			ia64_setf_sig (code, ins->dreg, ins->sreg1);
			ia64_fcvt_xf (code, ins->dreg, ins->dreg);
			ia64_fnorm_d_sf (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_LCONV_TO_R8:
			/* FIXME: Difference with CEE_CONV_R8 ? */
			ia64_setf_sig (code, ins->dreg, ins->sreg1);
			ia64_fcvt_xf (code, ins->dreg, ins->dreg);
			ia64_fnorm_d_sf (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_FCONV_TO_R4:
			ia64_fnorm_s_sf (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I2:
		case OP_FCONV_TO_U2:
		case OP_FCONV_TO_U1:
			/* FIXME: sign/zero extend ? */
			ia64_fcvt_fx_trunc_sf (code, FP_SCRATCH_REG, ins->sreg1, 0);
			ia64_getf_sig (code, ins->dreg, FP_SCRATCH_REG);
			break;
		case OP_FCONV_TO_I8:
			/* FIXME: Difference with OP_FCONV_TO_I4 ? */
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

		/* Calls */
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			ia64_ld8_hint (code, GP_SCRATCH_REG, ins->sreg1, 0);
			break;

		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case CEE_CALL:
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
		case OP_VOIDCALL_REG:
			call = (MonoCallInst*)ins;

			if (call->virtual) {
				/* Keep this in synch with get_vcall_slot_addr */

				/* This is a virtual call */
				ia64_mov_to_br (code, IA64_B6, ins->sreg1, 0, 0, 0);

				/*
				 * This nop will tell get_vcall_slot_addr that this is a virtual 
				 * call.
				 */
				ia64_nop_i (code, 0x12345);
			}
			else {
				/* Indirect call */
				ia64_mov (code, GP_SCRATCH_REG, ins->sreg1);
				ia64_ld8_inc_imm_hint (code, GP_SCRATCH_REG2, GP_SCRATCH_REG, 8, 0);
				ia64_mov_to_br (code, IA64_B6, GP_SCRATCH_REG2, 0, 0, 0);
				ia64_ld8 (code, IA64_GP, GP_SCRATCH_REG);
			}
			ia64_br_call_reg (code, IA64_B0, IA64_B6);

			code = emit_move_return_value (cfg, ins, code);
			break;

			/* Exception handling */
		case OP_CALL_HANDLER:
			/* FIXME: */
			break;

		case CEE_THROW:
			/* FIXME: */
			ia64_break_i (code, 0x1234);
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
		
		ins = ins->next;
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

static void 
ia64_patch (unsigned char* code, gpointer target)
{
	int template, i;
	guint64 instructions [3];
	guint8 gen_buf [8];
	Ia64CodegenState gen;

	template = ia64_bundle_template (code);
	instructions [0] = ia64_bundle_ins1 (code);
	instructions [1] = ia64_bundle_ins2 (code);
	instructions [2] = ia64_bundle_ins3 (code);

	ia64_codegen_init (gen, gen_buf);

	for (i = 0; i < 3; ++i) {
		guint64 ins = instructions [i];
		int opcode = ia64_ins_opcode (ins);

		/* Skip nops */
		gboolean nop = FALSE;
		switch (ins_types_in_template [template][i]) {
		case IA64_INS_TYPE_I:
			nop = (ins == IA64_NOP_I);
			break;
		case IA64_INS_TYPE_M:
			nop = (ins == IA64_NOP_M);
			break;
		case IA64_INS_TYPE_LX:
			break;
		default:
			break;
		}

		if (nop)
			continue;

		switch (ins_types_in_template [template][i]) {
		case IA64_INS_TYPE_B:
			if ((opcode == 4) && (ia64_ins_btype (ins) == 0)) {
				/* br.cond */
				gint64 disp = ((guint8*)target - code) >> 4;

				/* FIXME: hints */
				ia64_br_cond_hint_pred (gen, ia64_ins_qp (ins), disp, 0, 0, 0);
				
				instructions [i] = gen.instructions [0];
			}
			else if (opcode == 5) {
				/* br.call */
				gint64 disp = ((guint8*)target - code) >> 4;

				/* FIXME: hints */
				ia64_br_call_hint_pred (gen, ia64_ins_qp (ins), ia64_ins_b1 (ins), disp, 0, 0, 0);
				instructions [i] = gen.instructions [0];
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
			}
			else
				NOT_IMPLEMENTED;

			break;
		default:
			NOT_IMPLEMENTED;
		}
	}

	/* Rewrite code */
	ia64_codegen_init (gen, code);
	ia64_emit_bundle_template (&gen, template, instructions [0], instructions [1], instructions [2]);
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

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
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int alloc_size, pos, max_offset, i, quad;
	Ia64CodegenState code;
	CallInfo *cinfo;

	sig = mono_method_signature (method);
	pos = 0;

	cinfo = get_call_info (sig, FALSE);

	cfg->code_size =  MAX (((MonoMethodNormal *)method)->header->code_size * 4, 512);
	cfg->native_code = g_malloc (cfg->code_size);

	ia64_codegen_init (code, cfg->native_code);

	ia64_alloc (code, cfg->arch.reg_saved_ar_pfs, cfg->arch.reg_local0 - cfg->arch.reg_in0, cfg->arch.reg_out0 - cfg->arch.reg_local0, cfg->arch.n_out_regs, 0);
	ia64_mov_from_br (code, cfg->arch.reg_saved_b0, IA64_B0);

	alloc_size = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);
	if (cfg->param_area)
		alloc_size += cfg->param_area;
	if (alloc_size)
		/* scratch area */
		alloc_size += 16;
	alloc_size = ALIGN_TO (alloc_size, MONO_ARCH_FRAME_ALIGNMENT);

	pos = 0;

	if (method->save_lmf) {
		/* FIXME: */
	}

	alloc_size -= pos;

	if (alloc_size || cinfo->stack_usage)
		ia64_mov (code, cfg->frame_reg, IA64_SP);

	if (alloc_size) {
		/* See mono_emit_stack_alloc */
#if defined(MONO_ARCH_SIGSEGV_ON_ALTSTACK)
		NOT_IMPLEMENTED;
#else
		ia64_mov (code, cfg->arch.reg_saved_sp, IA64_SP);

		if (ia64_is_imm14 (-alloc_size))
			ia64_adds_imm (code, IA64_SP, (-alloc_size), IA64_SP);
		else {
			ia64_movl (code, GP_SCRATCH_REG, -alloc_size);
			ia64_add (code, IA64_SP, GP_SCRATCH_REG, IA64_SP);
		}
#endif
	}

	/* compute max_offset in order to use short forward jumps */
	max_offset = 0;
	if (cfg->opt & MONO_OPT_BRANCH) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			MonoInst *ins = bb->code;
			bb->max_offset = max_offset;

			if (cfg->prof_options & MONO_PROFILE_COVERAGE)
				max_offset += 6;
			/* max alignment for loops */
			if ((cfg->opt & MONO_OPT_LOOP) && bb_is_loop_start (bb))
				max_offset += LOOP_ALIGNMENT;

			while (ins) {
				if (ins->opcode == OP_LABEL)
					ins->inst_c1 = max_offset;
				
				max_offset += ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];
				ins = ins->next;
			}
		}
	}

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
		inst = cfg->varinfo [i];

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		arg_type = mono_type_get_underlying_type (arg_type);

		stack_offset = ainfo->offset + ARGS_OFFSET;

		/* Save volatile arguments to the stack */
		if (inst->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInFloatReg:
				/* FIXME: big offsets */
				g_assert (inst->opcode = OP_REGOFFSET);
				ia64_adds_imm (code, GP_SCRATCH_REG, inst->inst_offset, inst->inst_basereg);
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
			default:
				NOT_IMPLEMENTED;
			}
		}

		if (inst->opcode == OP_REGVAR) {
			/* Argument allocated to (non-volatile) register */
			switch (ainfo->storage) {
			case ArgInIReg:
				if (inst->dreg != cfg->arch.reg_in0 + ainfo->reg)
					ia64_mov (code, inst->dreg, cfg->arch.reg_in0 + ainfo->reg);
				break;
			default:
				NOT_IMPLEMENTED;
			}
		}
	}

	if (method->save_lmf) {
		/* FIXME: */
	}

	ia64_codegen_close (code);

	g_free (cinfo);

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code.buf = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code.buf, TRUE);

	cfg->code_len = code.buf - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

	return code.buf;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int quad, pos, i, alloc_size;
	int max_epilog_size = 16 * 4;
	Ia64CodegenState code;
	guint *buf;
	CallInfo *cinfo;

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

	buf = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		buf = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, buf, TRUE);

	ia64_codegen_init (code, buf);

	/* the code restoring the registers must be kept in sync with CEE_JMP */
	pos = 0;
	
	if (method->save_lmf) {
		/* FIXME: */
	}

	/* Load returned vtypes into registers if needed */
	cinfo = get_call_info (mono_method_signature (method), FALSE);
	if (cinfo->ret.storage == ArgValuetypeInReg) {
		NOT_IMPLEMENTED;
	}
	g_free (cinfo);

	if (cfg->stack_offset)
		ia64_mov (code, IA64_SP, cfg->arch.reg_saved_sp);

	ia64_mov_to_ar_i (code, IA64_PFS, cfg->arch.reg_saved_ar_pfs);
	ia64_mov_ret_to_br (code, IA64_B0, cfg->arch.reg_saved_b0, 0, 0, 0);
	ia64_br_ret_reg_hint (code, IA64_B0, 0, 0, 0);

	ia64_codegen_close (code);

	cfg->code_len = code.buf - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int nthrows, i;
	Ia64CodegenState code;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];
	guint32 code_size = 0;

	/* Compute needed space */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			code_size += 40;
		if (patch_info->type == MONO_PATCH_INFO_R8)
			code_size += 8 + 7; /* sizeof (double) + alignment */
		if (patch_info->type == MONO_PATCH_INFO_R4)
			code_size += 4 + 7; /* sizeof (float) + alignment */
	}

	ia64_codegen_init (code, cfg->native_code + cfg->code_len);

	/* add code to raise exceptions */
	nthrows = 0;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			NOT_IMPLEMENTED;
		default:
			break;
		}
		}
	}

	ia64_codegen_close (code);

	cfg->code_len = code.buf - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	NOT_IMPLEMENTED;

	return NULL;
}

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	NOT_IMPLEMENTED;

	return NULL;
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	guint8* p = (guint8*)((guint64)code & ~(0x3f));
	guint8* end = (guint8*)((guint64)code + size);

	while (p < end) {
		__asm__ __volatile__ ("fc.i %0"::"r"(p));
		p += 32;
	}
}

void
mono_arch_flush_register_windows (void)
{
	NOT_IMPLEMENTED;
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
	NOT_IMPLEMENTED;

	return FALSE;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	NOT_IMPLEMENTED;

	return 0;
}

gpointer*
mono_arch_get_vcall_slot_addr (guint8* code, gpointer *regs)
{
	guint8 *bundle1 = code - 48;
	guint8 *bundle2 = code - 32;
	guint8 *bundle3 = code - 16;
	guint64 ins11 = ia64_bundle_ins1 (bundle1);
	guint64 ins12 = ia64_bundle_ins2 (bundle1);
	guint64 ins13 = ia64_bundle_ins3 (bundle1);
	guint64 ins21 = ia64_bundle_ins1 (bundle2);
	guint64 ins22 = ia64_bundle_ins2 (bundle2);
	guint64 ins23 = ia64_bundle_ins3 (bundle2);
	guint64 ins31 = ia64_bundle_ins1 (bundle3);
	guint64 ins32 = ia64_bundle_ins2 (bundle3);
	guint64 ins33 = ia64_bundle_ins3 (bundle3);
	int reg;

	/* 
	 * Virtual calls are made with:
	 * [MII]       nop.m 0x0
	 *             mov.sptk b6=r8,0x2000000000f32a80
	 *             nop.i 0x0;;
	 * [MII]       nop.m 0x0
	 *             nop.i 0x123456
	 *             nop.i 0x0
	 * [MIB]       nop.m 0x0
	 *             nop.i 0x0
	 *             br.call.sptk.few b0=b6;;
	 */

	if ((ia64_bundle_template (bundle2) == IA64_TEMPLATE_MIIS) &&
		(ia64_bundle_template (bundle3) == IA64_TEMPLATE_MIBS) &&
		(ins21 == IA64_NOP_M) && 
		(ia64_ins_opcode (ins22) == 0) && (ia64_ins_x3 (ins22) == 0) && (ia64_ins_x6 (ins22) == 0x1) && (ia64_ins_y (ins22) == 0) &&
		(ins23 == IA64_NOP_I) &&
		(ins31 == IA64_NOP_M) &&
		(ins32 == IA64_NOP_I) &&
		//	   (ia64_ins_opcode (ins33) == 1) && (ia64_ins_b1 (ins33) == 0) && (ia64_ins_b2 (ins33) == 6) &&
		((ins22 >> 6) & 0xfffff) == 0x12345) {
		g_assert (ins11 == IA64_NOP_M);
		g_assert (ins13 == IA64_NOP_I);
		g_assert (ia64_ins_opcode (ins12) == 0);
		g_assert (ia64_ins_x3 (ins12) == 7);
		g_assert (ia64_ins_x (ins12) == 0);
		g_assert (ia64_ins_b1 (ins12) == IA64_B6);

		reg = ia64_ins_r2 (ins12);

		/* 
		 * Must be a scratch register, since only those are saved by the trampoline
		 */
		g_assert ((1 << reg) & MONO_ARCH_CALLEE_REGS);

		g_assert (regs [reg]);

		return regs [reg];
	}

	return NULL;
}

gpointer*
mono_arch_get_delegate_method_ptr_addr (guint8* code, gpointer *regs)
{
	NOT_IMPLEMENTED;

	return NULL;
}

static gboolean tls_offset_inited = FALSE;

/* code should be simply return <tls var>; */
static int 
read_tls_offset_from_method (void* method)
{
	NOT_IMPLEMENTED;

	return 0;
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

static void
setup_stack (MonoJitTlsData *tls)
{
	NOT_IMPLEMENTED;
}

#endif

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
	if (!tls_offset_inited) {
		tls_offset_inited = TRUE;

		/* FIXME: */
		/*
		lmf_tls_offset = read_tls_offset_from_method (mono_get_lmf_addr);
		appdomain_tls_offset = read_tls_offset_from_method (mono_domain_get);
		thread_tls_offset = read_tls_offset_from_method (mono_thread_current);
		*/
	}		

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	setup_stack (tls);
#endif
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	struct sigaltstack sa;

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_DISABLE;
	sigaltstack  (&sa, NULL);

	if (tls->signal_stack)
		munmap (tls->signal_stack, SIGNAL_STACK_SIZE);
#endif
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	MonoCallInst *call = (MonoCallInst*)inst;
	int out_reg = cfg->arch.reg_out0;

	if (vt_reg != -1) {
		CallInfo * cinfo = get_call_info (inst->signature, FALSE);
		MonoInst *vtarg;

		if (cinfo->ret.storage == ArgValuetypeInReg) {
			NOT_IMPLEMENTED;
		}
		else {
			MONO_INST_NEW (cfg, vtarg, OP_MOVE);
			vtarg->sreg1 = vt_reg;
			vtarg->dreg = mono_regstate_next_int (cfg->rs);
			mono_bblock_add_inst (cfg->cbb, vtarg);

			mono_call_inst_add_outarg_reg (call, vtarg->dreg, out_reg, FALSE);

			out_reg ++;
		}

		g_free (cinfo);
	}

	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this;
		MONO_INST_NEW (cfg, this, OP_MOVE);
		this->type = this_type;
		this->sreg1 = this_reg;
		this->dreg = mono_regstate_next_int (cfg->rs);
		mono_bblock_add_inst (cfg->cbb, this);

		mono_call_inst_add_outarg_reg (call, this->dreg, out_reg, FALSE);
	}
}

MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	/* FIXME: */
	return NULL;

	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sin") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SIN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Cos") == 0) {
			MONO_INST_NEW (cfg, ins, OP_COS);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Tan") == 0) {
				return ins;
			MONO_INST_NEW (cfg, ins, OP_TAN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Atan") == 0) {
				return ins;
			MONO_INST_NEW (cfg, ins, OP_ATAN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SQRT);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			MONO_INST_NEW (cfg, ins, OP_ABS);
			ins->inst_i0 = args [0];
		}
#if 0
		/* OP_FREM is not IEEE compatible */
		else if (strcmp (cmethod->name, "IEEERemainder") == 0) {
			MONO_INST_NEW (cfg, ins, OP_FREM);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif
	} else if(cmethod->klass->image == mono_defaults.corlib &&
			   (strcmp (cmethod->klass->name_space, "System.Threading") == 0) &&
			   (strcmp (cmethod->klass->name, "Interlocked") == 0)) {

		if (strcmp (cmethod->name, "Increment") == 0) {
			MonoInst *ins_iconst;
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();
			MONO_INST_NEW (cfg, ins, opcode);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = 1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Decrement") == 0) {
			MonoInst *ins_iconst;
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_NEW_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_NEW_I8;
			else
				g_assert_not_reached ();
			MONO_INST_NEW (cfg, ins, opcode);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = -1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Add") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_ADD_I4;
			else if (fsig->params [0]->type == MONO_TYPE_I8)
				opcode = OP_ATOMIC_ADD_I8;
			else
				g_assert_not_reached ();
			
			MONO_INST_NEW (cfg, ins, opcode);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		} else if (strcmp (cmethod->name, "Exchange") == 0) {
			guint32 opcode;

			if (fsig->params [0]->type == MONO_TYPE_I4)
				opcode = OP_ATOMIC_EXCHANGE_I4;
			else if ((fsig->params [0]->type == MONO_TYPE_I8) ||
					 (fsig->params [0]->type == MONO_TYPE_I) ||
					 (fsig->params [0]->type == MONO_TYPE_OBJECT))
				opcode = OP_ATOMIC_EXCHANGE_I8;
			else
				return NULL;

			MONO_INST_NEW (cfg, ins, opcode);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		} else if (strcmp (cmethod->name, "Read") == 0 && (fsig->params [0]->type == MONO_TYPE_I8)) {
			/* 64 bit reads are already atomic */
			MONO_INST_NEW (cfg, ins, CEE_LDIND_I8);
			ins->inst_i0 = args [0];
		}

		/* 
		 * Can't implement CompareExchange methods this way since they have
		 * three arguments.
		 */
	}

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
	
	if (appdomain_tls_offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = appdomain_tls_offset;
	return ins;
}

MonoInst* mono_arch_get_thread_intrinsic (MonoCompile* cfg)
{
	MonoInst* ins;
	
	if (thread_tls_offset == -1)
		return NULL;
	
	MONO_INST_NEW (cfg, ins, OP_TLS_GET);
	ins->inst_offset = thread_tls_offset;
	return ins;
}
