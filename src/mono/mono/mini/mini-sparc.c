/**
 * \file
 * Sparc backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * Modified for SPARC:
 *   Christopher Taylor (ct@gentoo.org)
 *   Mark Crichton (crichton@gimp.org)
 *   Zoltan Varga (vargaz@freemail.hu)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>
#include <pthread.h>
#include <unistd.h>

#ifndef __linux__
#include <thread.h>
#endif

#include <unistd.h>
#include <sys/mman.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-hwcap.h>
#include <mono/utils/unlocked.h>

#include "mini-sparc.h"
#include "trace.h"
#include "cpu-sparc.h"
#include "jit-icalls.h"
#include "ir-emit.h"

/*
 * Sparc V9 means two things:
 * - the instruction set
 * - the ABI
 *
 * V9 instructions are only usable if the underlying processor is 64 bit. Most Sparc 
 * processors in use are 64 bit processors. The V9 ABI is only usable if the 
 * mono executable is a 64 bit executable. So it would make sense to use the 64 bit
 * instructions without using the 64 bit ABI.
 */

/*
 * Register usage:
 * - %i0..%i<n> hold the incoming arguments, these are never written by JITted 
 * code. Unused input registers are used for global register allocation.
 * - %o0..%o5 and %l7 is used for local register allocation and passing arguments
 * - %l0..%l6 is used for global register allocation
 * - %o7 and %g1 is used as scratch registers in opcodes
 * - all floating point registers are used for local register allocation except %f0. 
 *   Only double precision registers are used.
 * In 64 bit mode:
 * - fp registers %d0..%d30 are used for parameter passing, and %d32..%d62 are
 *   used for local allocation.
 */

/*
 * Alignment:
 * - doubles and longs must be stored in dword aligned locations
 */

/*
 * The following things are not implemented or do not work:
 *  - some fp arithmetic corner cases
 * The following tests in mono/mini are expected to fail:
 *  - test_0_simple_double_casts
 *      This test casts (guint64)-1 to double and then back to guint64 again.
 *    Under x86, it returns 0, while under sparc it returns -1.
 *
 * In addition to this, the runtime requires the trunc function, or its 
 * solaris counterpart, aintl, to do some double->int conversions. If this 
 * function is not available, it is emulated somewhat, but the results can be
 * strange.
 */

/*
 * SPARCV9 FIXME:
 * - optimize sparc_set according to the memory model
 * - when non-AOT compiling, compute patch targets immediately so we don't
 *   have to emit the 6 byte template.
 * - varags
 * - struct arguments/returns
 */

/*
 * SPARCV9 ISSUES:
 * - sparc_call_simple can't be used in a lot of places since the displacement
 *   might not fit into an imm30.
 * - g1 can't be used in a lot of places since it is used as a scratch reg in
 *   sparc_set.
 * - sparc_f0 can't be used as a scratch register on V9
 * - the %d34..%d62 fp registers are encoded as: %dx = %f(x - 32 + 1), ie.
 *   %d36 = %f5.
 * - ldind.i4/u4 needs to sign extend/clear out upper word -> slows things down
 * - ins->dreg can't be used as a scatch register in r4 opcodes since it might
 *   be a double precision register which has no single precision part.
 * - passing/returning structs is hard to implement, because:
 *   - the spec is very hard to understand
 *   - it requires knowledge about the fields of structure, needs to handle
 *     nested structures etc.
 */

/*
 * Possible optimizations:
 * - delay slot scheduling
 * - allocate large constants to registers
 * - add more mul/div/rem optimizations
 */

#ifndef __linux__
#define MONO_SPARC_THR_TLS 1
#endif

/*
 * There was a 64 bit bug in glib-2.2: g_bit_nth_msf (0, -1) would return 32,
 * causing infinite loops in dominator computation. So glib-2.4 is required.
 */
#ifdef SPARCV9
#if GLIB_MAJOR_VERSION == 2 && GLIB_MINOR_VERSION < 4
#error "glib 2.4 or later is required for 64 bit mode."
#endif
#endif

#define SIGNAL_STACK_SIZE (64 * 1024)

#define STACK_BIAS MONO_SPARC_STACK_BIAS

#ifdef SPARCV9

/* %g1 is used by sparc_set */
#define GP_SCRATCH_REG sparc_g4
/* %f0 is used for parameter passing */
#define FP_SCRATCH_REG sparc_f30
#define ARGS_OFFSET (STACK_BIAS + 128)

#else

#define FP_SCRATCH_REG sparc_f0
#define ARGS_OFFSET 68
#define GP_SCRATCH_REG sparc_g1

#endif

/* Whenever this is a 64bit executable */
#if SPARCV9
static gboolean v64 = TRUE;
#else
static gboolean v64 = FALSE;
#endif

static gpointer mono_arch_get_lmf_addr (void);

const char*
mono_arch_regname (int reg) {
	static const char * rnames[] = {
		"sparc_g0", "sparc_g1", "sparc_g2", "sparc_g3", "sparc_g4",
		"sparc_g5", "sparc_g6", "sparc_g7", "sparc_o0", "sparc_o1",
		"sparc_o2", "sparc_o3", "sparc_o4", "sparc_o5", "sparc_sp",
		"sparc_call", "sparc_l0", "sparc_l1", "sparc_l2", "sparc_l3",
		"sparc_l4", "sparc_l5", "sparc_l6", "sparc_l7", "sparc_i0",
		"sparc_i1", "sparc_i2", "sparc_i3", "sparc_i4", "sparc_i5",
		"sparc_fp", "sparc_retadr"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg) {
	static const char *rnames [] = {
		"sparc_f0", "sparc_f1", "sparc_f2", "sparc_f3", "sparc_f4", 
		"sparc_f5", "sparc_f6", "sparc_f7", "sparc_f8", "sparc_f9",
		"sparc_f10", "sparc_f11", "sparc_f12", "sparc_f13", "sparc_f14", 
		"sparc_f15", "sparc_f16", "sparc_f17", "sparc_f18", "sparc_f19",
		"sparc_f20", "sparc_f21", "sparc_f22", "sparc_f23", "sparc_f24", 
		"sparc_f25", "sparc_f26", "sparc_f27", "sparc_f28", "sparc_f29",
		"sparc_f30", "sparc_f31"
	};

	if (reg >= 0 && reg < 32)
		return rnames [reg];
	else
		return "unknown";
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

gboolean
mono_arch_have_fast_tls (void)
{
	return FALSE;
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	guint32 opts = 0;

	*exclude_mask = 0;

	/*
	 * On some processors, the cmov instructions are even slower than the
	 * normal ones...
	 */
	if (mono_hwcap_sparc_is_v9)
		opts |= MONO_OPT_CMOV | MONO_OPT_FCMOV;
	else
		*exclude_mask |= MONO_OPT_CMOV | MONO_OPT_FCMOV;

	return opts;
}

/*
 * This function test for all SIMD functions supported.
 *
 * Returns a bitmask corresponding to all supported versions.
 *
 */
guint32
mono_arch_cpu_enumerate_simd_versions (void)
{
	/* SIMD is currently unimplemented */
	return 0;
}

#ifdef __GNUC__
#define flushi(addr)    __asm__ __volatile__ ("iflush %0"::"r"(addr):"memory")
#else /* assume Sun's compiler */
static void flushi(void *addr)
{
    asm("flush %i0");
}
#endif

#ifndef __linux__
void sync_instruction_memory(caddr_t addr, int len);
#endif

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#ifndef __linux__
	/* Hopefully this is optimized based on the actual CPU */
	sync_instruction_memory (code, size);
#else
	gulong start = (gulong) code;
	gulong end = start + size;
	gulong align;

	/* Sparcv9 chips only need flushes on 32 byte
	 * cacheline boundaries.
	 *
	 * Sparcv8 needs a flush every 8 bytes.
	 */
	align = (mono_hwcap_sparc_is_v9 ? 32 : 8);

	start &= ~(align - 1);
	end = (end + (align - 1)) & ~(align - 1);

	while (start < end) {
#ifdef __GNUC__
		__asm__ __volatile__ ("iflush %0"::"r"(start));
#else
		flushi (start);
#endif
		start += align;
	}
#endif
}

/*
 * mono_sparc_flushw:
 *
 * Flush all register windows to memory. Every register window is saved to
 * a 16 word area on the stack pointed to by its %sp register.
 */
void
mono_sparc_flushw (void)
{
	static guint32 start [64];
	static int inited = 0;
	guint32 *code;
	static void (*flushw) (void);

	if (!inited) {
		code = start;

		sparc_save_imm (code, sparc_sp, -160, sparc_sp);
		sparc_flushw (code);
		sparc_ret (code);
		sparc_restore_simple (code);

		g_assert ((code - start) < 64);

		mono_arch_flush_icache ((guint8*)start, (guint8*)code - (guint8*)start);

		flushw = (gpointer)start;

		inited = 1;
	}

	flushw ();
}

void
mono_arch_flush_register_windows (void)
{
	mono_sparc_flushw ();
}

gboolean 
mono_arch_is_inst_imm (int opcode, int imm_opcode, gint64 imm)
{
	return sparc_is_imm13 (imm);
}

gboolean 
mono_sparc_is_v9 (void) {
	return mono_hwcap_sparc_is_v9;
}

gboolean 
mono_sparc_is_sparc64 (void) {
	return v64;
}

typedef enum {
	ArgInIReg,
	ArgInIRegPair,
	ArgInSplitRegStack,
	ArgInFReg,
	ArgInFRegPair,
	ArgOnStack,
	ArgOnStackPair,
	ArgInFloatReg,  /* V9 only */
	ArgInDoubleReg  /* V9 only */
} ArgStorage;

typedef struct {
	gint16 offset;
	/* This needs to be offset by %i0 or %o0 depending on caller/callee */
	gint8  reg;
	ArgStorage storage;
	guint32 vt_offset; /* for valuetypes */
} ArgInfo;

struct CallInfo {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
};

#define DEBUG(a)

/* %o0..%o5 */
#define PARAM_REGS 6

static void inline
add_general (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo, gboolean pair)
{
	ainfo->offset = *stack_size;

	if (!pair) {
		if (*gr >= PARAM_REGS) {
			ainfo->storage = ArgOnStack;
		}
		else {
			ainfo->storage = ArgInIReg;
			ainfo->reg = *gr;
			(*gr) ++;
		}

		/* Allways reserve stack space for parameters passed in registers */
		(*stack_size) += sizeof (target_mgreg_t);
	}
	else {
		if (*gr < PARAM_REGS - 1) {
			/* A pair of registers */
			ainfo->storage = ArgInIRegPair;
			ainfo->reg = *gr;
			(*gr) += 2;
		}
		else if (*gr >= PARAM_REGS) {
			/* A pair of stack locations */
			ainfo->storage = ArgOnStackPair;
		}
		else {
			ainfo->storage = ArgInSplitRegStack;
			ainfo->reg = *gr;
			(*gr) ++;
		}

		(*stack_size) += 2 * sizeof (target_mgreg_t);
	}
}

#ifdef SPARCV9

#define FLOAT_PARAM_REGS 32

static void inline
add_float (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo, gboolean single)
{
	ainfo->offset = *stack_size;

	if (single) {
		if (*gr >= FLOAT_PARAM_REGS) {
			ainfo->storage = ArgOnStack;
		}
		else {
			/* A single is passed in an even numbered fp register */
			ainfo->storage = ArgInFloatReg;
			ainfo->reg = *gr + 1;
			(*gr) += 2;
		}
	}
	else {
		if (*gr < FLOAT_PARAM_REGS) {
			/* A double register */
			ainfo->storage = ArgInDoubleReg;
			ainfo->reg = *gr;
			(*gr) += 2;
		}
		else {
			ainfo->storage = ArgOnStack;
		}
	}

	(*stack_size) += sizeof (target_mgreg_t);
}

#endif

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * For V8, see the "System V ABI, Sparc Processor Supplement" Sparc V8 version 
 * document for more information.
 * For V9, see the "Low Level System Information (64-bit psABI)" chapter in
 * the 'Sparc Compliance Definition 2.4' document.
 */
static CallInfo*
get_call_info (MonoCompile *cfg, MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i, gr, fr;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;
	MonoType *ret_type;

	cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	gr = 0;
	fr = 0;

#ifdef SPARCV9
	if (MONO_TYPE_ISSTRUCT ((sig->ret))) {
		/* The address of the return value is passed in %o0 */
		add_general (&gr, &stack_size, &cinfo->ret, FALSE);
		cinfo->ret.reg += sparc_i0;
		/* FIXME: Pass this after this as on other platforms */
		NOT_IMPLEMENTED;
	}
#endif

	/* this */
	if (sig->hasthis)
		add_general (&gr, &stack_size, cinfo->args + 0, FALSE);

	if ((sig->call_convention == MONO_CALL_VARARG) && (n == 0)) {
		gr = PARAM_REGS;

		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie, FALSE);
	}

	for (i = 0; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];
		MonoType *ptype;

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			gr = PARAM_REGS;

			/* Emit the signature cookie just before the implicit arguments */
			add_general (&gr, &stack_size, &cinfo->sig_cookie, FALSE);
		}

		DEBUG(printf("param %d: ", i));
		if (sig->params [i]->byref) {
			DEBUG(printf("byref\n"));
			
			add_general (&gr, &stack_size, ainfo, FALSE);
			continue;
		}
		ptype = mini_get_underlying_type (sig->params [i]);
		switch (ptype->type) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			add_general (&gr, &stack_size, ainfo, FALSE);
			/* the value is in the ls byte */
			ainfo->offset += sizeof (target_mgreg_t) - 1;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			add_general (&gr, &stack_size, ainfo, FALSE);
			/* the value is in the ls word */
			ainfo->offset += sizeof (target_mgreg_t) - 2;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
			add_general (&gr, &stack_size, ainfo, FALSE);
			/* the value is in the ls dword */
			ainfo->offset += sizeof (target_mgreg_t) - 4;
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
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (ptype)) {
				add_general (&gr, &stack_size, ainfo, FALSE);
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
#ifdef SPARCV9
			if (sig->pinvoke)
				NOT_IMPLEMENTED;
#endif
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_TYPEDBYREF:
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
#ifdef SPARCV9
			add_general (&gr, &stack_size, ainfo, FALSE);
#else
			add_general (&gr, &stack_size, ainfo, TRUE);
#endif
			break;
		case MONO_TYPE_R4:
#ifdef SPARCV9
			add_float (&fr, &stack_size, ainfo, TRUE);
			gr ++;
#else
			/* single precision values are passed in integer registers */
			add_general (&gr, &stack_size, ainfo, FALSE);
#endif
			break;
		case MONO_TYPE_R8:
#ifdef SPARCV9
			add_float (&fr, &stack_size, ainfo, FALSE);
			gr ++;
#else
			/* double precision values are passed in a pair of registers */
			add_general (&gr, &stack_size, ainfo, TRUE);
#endif
			break;
		default:
			g_assert_not_reached ();
		}
	}

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n > 0) && (sig->sentinelpos == sig->param_count)) {
		gr = PARAM_REGS;

		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie, FALSE);
	}

	/* return value */
	ret_type = mini_get_underlying_type (sig->ret);
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
		cinfo->ret.reg = sparc_i0;
		if (gr < 1)
			gr = 1;
		break;
	case MONO_TYPE_U8:
	case MONO_TYPE_I8:
#ifdef SPARCV9
		cinfo->ret.storage = ArgInIReg;
		cinfo->ret.reg = sparc_i0;
		if (gr < 1)
			gr = 1;
#else
		cinfo->ret.storage = ArgInIRegPair;
		cinfo->ret.reg = sparc_i0;
		if (gr < 2)
			gr = 2;
#endif
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		cinfo->ret.storage = ArgInFReg;
		cinfo->ret.reg = sparc_f0;
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ret_type)) {
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = sparc_i0;
			if (gr < 1)
				gr = 1;
			break;
		}
		/* Fall through */
	case MONO_TYPE_VALUETYPE:
		if (v64) {
			if (sig->pinvoke)
				NOT_IMPLEMENTED;
			else
				/* Already done */
				;
		}
		else
			cinfo->ret.storage = ArgOnStack;
		break;
	case MONO_TYPE_TYPEDBYREF:
		if (v64) {
			if (sig->pinvoke)
				/* Same as a valuetype with size 24 */
				NOT_IMPLEMENTED;
			else
				/* Already done */
				;
		}
		else
			cinfo->ret.storage = ArgOnStack;
		break;
	case MONO_TYPE_VOID:
		break;
	default:
		g_error ("Can't handle as return value 0x%x", sig->ret->type);
	}

	cinfo->stack_usage = stack_size;
	cinfo->reg_usage = gr;
	return cinfo;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	/* 
	 * FIXME: If an argument is allocated to a register, then load it from the
	 * stack in the prolog.
	 */

	for (i = 0; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		MonoMethodVar *vmv = MONO_VARINFO (cfg, i);

		/* unused vars */
		if (vmv->range.first_use.abs_pos >= vmv->range.last_use.abs_pos)
			continue;

		/* FIXME: Make arguments on stack allocateable to registers */
		if (ins->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT) || (ins->opcode == OP_REGVAR) || (ins->opcode == OP_ARG))
			continue;

		if (mono_is_regsize_var (ins->inst_vtype)) {
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
	int i;
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature_internal (cfg->method);

	cinfo = get_call_info (cfg, sig, FALSE);

	/* Use unused input registers */
	for (i = cinfo->reg_usage; i < 6; ++i)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (sparc_i0 + i));

	/* Use %l0..%l6 as global registers */
	for (i = sparc_l0; i < sparc_l7; ++i)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (i));

	g_free (cinfo);

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
	return 0;
}

/*
 * Set var information according to the calling convention. sparc version.
 * The locals var stuff should most likely be split in another method.
 */

void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;
	CallInfo *cinfo;

	header = cfg->header;

	sig = mono_method_signature_internal (cfg->method);

	cinfo = get_call_info (cfg, sig, FALSE);

	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
		case ArgInFReg:
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = cinfo->ret.reg;
			break;
		case ArgInIRegPair: {
			MonoType *t = mini_get_underlying_type (sig->ret);
			if (((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MonoInst *low = get_vreg_to_inst (cfg, MONO_LVREG_LS (cfg->ret->dreg));
				MonoInst *high = get_vreg_to_inst (cfg, MONO_LVREG_MS (cfg->ret->dreg));

				low->opcode = OP_REGVAR;
				low->dreg = cinfo->ret.reg + 1;
				high->opcode = OP_REGVAR;
				high->dreg = cinfo->ret.reg;
			}
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = cinfo->ret.reg;
			break;
		}
		case ArgOnStack:
#ifdef SPARCV9
			g_assert_not_reached ();
#else
			/* valuetypes */
			cfg->vret_addr->opcode = OP_REGOFFSET;
			cfg->vret_addr->inst_basereg = sparc_fp;
			cfg->vret_addr->inst_offset = 64;
#endif
			break;
		default:
			NOT_IMPLEMENTED;
		}
		cfg->ret->dreg = cfg->ret->inst_c0;
	}

	/*
	 * We use the ABI calling conventions for managed code as well.
	 * Exception: valuetypes are never returned in registers on V9.
	 * FIXME: Use something more optimized.
	 */

	/* Locals are allocated backwards from %fp */
	cfg->frame_reg = sparc_fp;
	offset = 0;

	/* 
	 * Reserve a stack slot for holding information used during exception 
	 * handling.
	 */
	if (header->num_clauses)
		offset += sizeof (target_mgreg_t) * 2;

	if (cfg->method->save_lmf) {
		offset += sizeof (MonoLMF);
		cfg->arch.lmf_offset = offset;
	}

	curinst = cfg->locals_start;
	for (i = curinst; i < cfg->num_varinfo; ++i) {
		inst = cfg->varinfo [i];

		if ((inst->opcode == OP_REGVAR) || (inst->opcode == OP_REGOFFSET)) {
			//g_print ("allocating local %d to %s\n", i, mono_arch_regname (inst->dreg));
			continue;
		}

		if (inst->flags & MONO_INST_IS_DEAD)
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
			size = mono_class_native_size (mono_class_from_mono_type_internal (inst->inst_vtype), &align);
		else
			size = mini_type_stack_size (inst->inst_vtype, &align);

		/* 
		 * This is needed since structures containing doubles must be doubleword 
         * aligned.
		 * FIXME: Do this only if needed.
		 */
		if (MONO_TYPE_ISSTRUCT (inst->inst_vtype))
			align = 8;

		/*
		 * variables are accessed as negative offsets from %fp, so increase
		 * the offset before assigning it to a variable
		 */
		offset += size;

		offset += align - 1;
		offset &= ~(align - 1);
		inst->opcode = OP_REGOFFSET;
		inst->inst_basereg = sparc_fp;
		inst->inst_offset = STACK_BIAS + -offset;

		//g_print ("allocating local %d to [%s - %d]\n", i, mono_arch_regname (inst->inst_basereg), - inst->inst_offset);
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		cfg->sig_cookie = cinfo->sig_cookie.offset + ARGS_OFFSET;
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		inst = cfg->args [i];
		if (inst->opcode != OP_REGVAR) {
			ArgInfo *ainfo = &cinfo->args [i];
			gboolean inreg = TRUE;
			MonoType *arg_type;
			ArgStorage storage;

			if (sig->hasthis && (i == 0))
				arg_type = mono_get_object_type ();
			else
				arg_type = sig->params [i - sig->hasthis];

#ifndef SPARCV9
			if (!arg_type->byref && ((arg_type->type == MONO_TYPE_R4) 
									 || (arg_type->type == MONO_TYPE_R8)))
				/*
				 * Since float arguments are passed in integer registers, we need to
				 * save them to the stack in the prolog.
				 */
				inreg = FALSE;
#endif

			/* FIXME: Allocate volatile arguments to registers */
			/* FIXME: This makes the argument holding a vtype address into volatile */
			if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
				inreg = FALSE;

			if (MONO_TYPE_ISSTRUCT (arg_type))
				/* FIXME: this isn't needed */
				inreg = FALSE;

			inst->opcode = OP_REGOFFSET;

			if (!inreg)
				storage = ArgOnStack;
			else
				storage = ainfo->storage;

			switch (storage) {
			case ArgInIReg:
				inst->opcode = OP_REGVAR;
				inst->dreg = sparc_i0 + ainfo->reg;
				break;
			case ArgInIRegPair:
				if (inst->type == STACK_I8) {
					MonoInst *low = get_vreg_to_inst (cfg, MONO_LVREG_LS (inst->dreg));
					MonoInst *high = get_vreg_to_inst (cfg, MONO_LVREG_MS (inst->dreg));

					low->opcode = OP_REGVAR;
					low->dreg = sparc_i0 + ainfo->reg + 1;
					high->opcode = OP_REGVAR;
					high->dreg = sparc_i0 + ainfo->reg;
				}
				inst->opcode = OP_REGVAR;
				inst->dreg = sparc_i0 + ainfo->reg;
				break;
			case ArgInFloatReg:
			case ArgInDoubleReg:
				/* 
				 * Since float regs are volatile, we save the arguments to
				 * the stack in the prolog.
				 * FIXME: Avoid this if the method contains no calls.
				 */
			case ArgOnStack:
			case ArgOnStackPair:
			case ArgInSplitRegStack:
				/* Split arguments are saved to the stack in the prolog */
				inst->opcode = OP_REGOFFSET;
				/* in parent frame */
				inst->inst_basereg = sparc_fp;
				inst->inst_offset = ainfo->offset + ARGS_OFFSET;

				if (!arg_type->byref && (arg_type->type == MONO_TYPE_R8)) {
					/* 
					 * It is very hard to load doubles from non-doubleword aligned
					 * memory locations. So if the offset is misaligned, we copy the
					 * argument to a stack location in the prolog.
					 */
					if ((inst->inst_offset - STACK_BIAS) % 8) {
						inst->inst_basereg = sparc_fp;
						offset += 8;
						align = 8;
						offset += align - 1;
						offset &= ~(align - 1);
						inst->inst_offset = STACK_BIAS + -offset;

					}
				}
				break;
			default:
				NOT_IMPLEMENTED;
			}

			if (MONO_TYPE_ISSTRUCT (arg_type)) {
				/* Add a level of indirection */
				/*
				 * It would be easier to add OP_LDIND_I here, but ldind_i instructions
				 * are destructively modified in a lot of places in inssel.brg.
				 */
				MonoInst *indir;
				MONO_INST_NEW (cfg, indir, 0);
				*indir = *inst;
				inst->opcode = OP_VTARG_ADDR;
				inst->inst_left = indir;
			}
		}
	}

	/* 
	 * spillvars are stored between the normal locals and the storage reserved
	 * by the ABI.
	 */

	cfg->stack_offset = offset;

	g_free (cinfo);
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;

	sig = mono_method_signature_internal (cfg->method);

	if (MONO_TYPE_ISSTRUCT ((sig->ret))) {
		cfg->vret_addr = mono_compile_create_var (cfg, mono_get_int_type (), OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (!sig->ret->byref && (sig->ret->type == MONO_TYPE_I8 || sig->ret->type == MONO_TYPE_U8)) {
		MonoInst *low = get_vreg_to_inst (cfg, MONO_LVREG_LS (cfg->ret->dreg));
		MonoInst *high = get_vreg_to_inst (cfg, MONO_LVREG_MS (cfg->ret->dreg));

		low->flags |= MONO_INST_VOLATILE;
		high->flags |= MONO_INST_VOLATILE;
	}

	/* Add a properly aligned dword for use by int<->float conversion opcodes */
	cfg->arch.float_spill_slot = mono_compile_create_var (cfg, m_class_get_byval_arg (mono_defaults.double_class), OP_ARG);
	((MonoInst*)cfg->arch.float_spill_slot)->flags |= MONO_INST_VOLATILE;
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, ArgStorage storage, int reg, guint32 sreg)
{
	MonoInst *arg;

	MONO_INST_NEW (cfg, arg, 0);

	arg->sreg1 = sreg;

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
	default:
		g_assert_not_reached ();
	}

	MONO_ADD_INS (cfg->cbb, arg);
}

static void
add_outarg_load (MonoCompile *cfg, MonoCallInst *call, int opcode, int basereg, int offset, int reg)
{
	int dreg = mono_alloc_ireg (cfg);

    MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, sparc_sp, offset);

	mono_call_inst_add_outarg_reg (cfg, call, dreg, reg, FALSE);
}

static void
emit_pass_long (MonoCompile *cfg, MonoCallInst *call, ArgInfo *ainfo, MonoInst *in)
{
	int offset = ARGS_OFFSET + ainfo->offset;

	switch (ainfo->storage) {
	case ArgInIRegPair:
		add_outarg_reg (cfg, call, ArgInIReg, sparc_o0 + ainfo->reg + 1, MONO_LVREG_LS (in->dreg));
		add_outarg_reg (cfg, call, ArgInIReg, sparc_o0 + ainfo->reg, MONO_LVREG_MS (in->dreg));
		break;
	case ArgOnStackPair:
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, sparc_sp, offset, MONO_LVREG_MS (in->dreg));
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, sparc_sp, offset + 4, MONO_LVREG_LS (in->dreg));
		break;
	case ArgInSplitRegStack:
		add_outarg_reg (cfg, call, ArgInIReg, sparc_o0 + ainfo->reg, MONO_LVREG_MS (in->dreg));
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, sparc_sp, offset + 4, MONO_LVREG_LS (in->dreg));
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
emit_pass_double (MonoCompile *cfg, MonoCallInst *call, ArgInfo *ainfo, MonoInst *in)
{
	int offset = ARGS_OFFSET + ainfo->offset;

	switch (ainfo->storage) {
	case ArgInIRegPair:
		/* floating-point <-> integer transfer must go through memory */
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, sparc_sp, offset, in->dreg);

		/* Load into a register pair */
		add_outarg_load (cfg, call, OP_LOADI4_MEMBASE, sparc_sp, offset, sparc_o0 + ainfo->reg);
		add_outarg_load (cfg, call, OP_LOADI4_MEMBASE, sparc_sp, offset + 4, sparc_o0 + ainfo->reg + 1);
		break;
	case ArgOnStackPair:
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, sparc_sp, offset, in->dreg);
		break;
	case ArgInSplitRegStack:
		/* floating-point <-> integer transfer must go through memory */
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, sparc_sp, offset, in->dreg);
		/* Load most significant word into register */
		add_outarg_load (cfg, call, OP_LOADI4_MEMBASE, sparc_sp, offset, sparc_o0 + ainfo->reg);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
emit_pass_float (MonoCompile *cfg, MonoCallInst *call, ArgInfo *ainfo, MonoInst *in)
{
	int offset = ARGS_OFFSET + ainfo->offset;

	switch (ainfo->storage) {
	case ArgInIReg:
		/* floating-point <-> integer transfer must go through memory */
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, sparc_sp, offset, in->dreg);
		add_outarg_load (cfg, call, OP_LOADI4_MEMBASE, sparc_sp, offset, sparc_o0 + ainfo->reg);
		break;
	case ArgOnStack:
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, sparc_sp, offset, in->dreg);
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
emit_pass_other (MonoCompile *cfg, MonoCallInst *call, ArgInfo *ainfo, MonoType *arg_type, MonoInst *in);

static void
emit_pass_vtype (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo, ArgInfo *ainfo, MonoType *arg_type, MonoInst *in, gboolean pinvoke)
{
	MonoInst *arg;
	guint32 align, offset, pad, size;

	if (arg_type->type == MONO_TYPE_TYPEDBYREF) {
		size = MONO_ABI_SIZEOF (MonoTypedRef);
		align = sizeof (target_mgreg_t);
	}
	else if (pinvoke)
		size = mono_type_native_stack_size (m_class_get_byval_arg (in->klass), &align);
	else {
		/* 
		 * Other backends use mono_type_stack_size (), but that
		 * aligns the size to 8, which is larger than the size of
		 * the source, leading to reads of invalid memory if the
		 * source is at the end of address space.
		 */
		size = mono_class_value_size (in->klass, &align);
	}

	/* The first 6 argument locations are reserved */
	if (cinfo->stack_usage < 6 * sizeof (target_mgreg_t))
		cinfo->stack_usage = 6 * sizeof (target_mgreg_t);

	offset = ALIGN_TO ((ARGS_OFFSET - STACK_BIAS) + cinfo->stack_usage, align);
	pad = offset - ((ARGS_OFFSET - STACK_BIAS) + cinfo->stack_usage);

	cinfo->stack_usage += size;
	cinfo->stack_usage += pad;

	/* 
	 * We use OP_OUTARG_VT to copy the valuetype to a stack location, then
	 * use the normal OUTARG opcodes to pass the address of the location to
	 * the callee.
	 */
	if (size > 0) {
		MONO_INST_NEW (cfg, arg, OP_OUTARG_VT);
		arg->sreg1 = in->dreg;
		arg->klass = in->klass;
		arg->backend.size = size;
		arg->inst_p0 = call;
		arg->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
		memcpy (arg->inst_p1, ainfo, sizeof (ArgInfo));
		((ArgInfo*)(arg->inst_p1))->offset = STACK_BIAS + offset;
		MONO_ADD_INS (cfg->cbb, arg);

		MONO_INST_NEW (cfg, arg, OP_ADD_IMM);
		arg->dreg = mono_alloc_preg (cfg);
		arg->sreg1 = sparc_sp;
		arg->inst_imm = STACK_BIAS + offset;
		MONO_ADD_INS (cfg->cbb, arg);

		emit_pass_other (cfg, call, ainfo, NULL, arg);
	}
}

static void
emit_pass_other (MonoCompile *cfg, MonoCallInst *call, ArgInfo *ainfo, MonoType *arg_type, MonoInst *in)
{
	int offset = ARGS_OFFSET + ainfo->offset;
	int opcode;

	switch (ainfo->storage) {
	case ArgInIReg:
		add_outarg_reg (cfg, call, ArgInIReg, sparc_o0 + ainfo->reg, in->dreg);
		break;
	case ArgOnStack:
#ifdef SPARCV9
		NOT_IMPLEMENTED;
#else
		if (offset & 0x1)
			opcode = OP_STOREI1_MEMBASE_REG;
		else if (offset & 0x2)
			opcode = OP_STOREI2_MEMBASE_REG;
		else
			opcode = OP_STOREI4_MEMBASE_REG;
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, opcode, sparc_sp, offset, in->dreg);
#endif
		break;
	default:
		g_assert_not_reached ();
	}
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;

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

	/* FIXME: Add support for signature tokens to AOT */
	cfg->disable_aot = TRUE;
	/* We allways pass the signature on the stack for simplicity */
	MONO_EMIT_NEW_STORE_MEMBASE_IMM (cfg, OP_STORE_MEMBASE_IMM, sparc_sp, ARGS_OFFSET + cinfo->sig_cookie.offset, tmp_sig);
}

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *in;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	guint32 extra_space = 0;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = get_call_info (cfg, sig, sig->pinvoke);

	if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret)) {
		/* Set the 'struct/union return pointer' location on the stack */
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, sparc_sp, 64, call->vret_var->dreg);
	}

	for (i = 0; i < n; ++i) {
		MonoType *arg_type;

		ainfo = cinfo->args + i;

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the first implicit argument */
			emit_sig_cookie (cfg, call, cinfo);
		}

		in = call->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = mono_get_object_type ();
		else
			arg_type = sig->params [i - sig->hasthis];

		arg_type = mini_get_underlying_type (arg_type);
		if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(sig->params [i - sig->hasthis])))
			emit_pass_vtype (cfg, call, cinfo, ainfo, arg_type, in, sig->pinvoke);
		else if (!arg_type->byref && ((arg_type->type == MONO_TYPE_I8) || (arg_type->type == MONO_TYPE_U8)))
			emit_pass_long (cfg, call, ainfo, in);
		else if (!arg_type->byref && (arg_type->type == MONO_TYPE_R8))
			emit_pass_double (cfg, call, ainfo, in);
		else if (!arg_type->byref && (arg_type->type == MONO_TYPE_R4))
			emit_pass_float (cfg, call, ainfo, in);
		else
			emit_pass_other (cfg, call, ainfo, arg_type, in);
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos)) {
		emit_sig_cookie (cfg, call, cinfo);
	}

	call->stack_usage = cinfo->stack_usage + extra_space;

	g_free (cinfo);
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	ArgInfo *ainfo = (ArgInfo*)ins->inst_p1;
	int size = ins->backend.size;

	mini_emit_memcpy (cfg, sparc_sp, ainfo->offset, src->dreg, 0, size, TARGET_SIZEOF_VOID_P);
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	CallInfo *cinfo = get_call_info (cfg, mono_method_signature_internal (method), FALSE);
	MonoType *ret = mini_get_underlying_type (mono_method_signature_internal (method)->ret);

	switch (cinfo->ret.storage) {
	case ArgInIReg:
		MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
		break;
	case ArgInIRegPair:
		if (ret->type == MONO_TYPE_I8 || ret->type == MONO_TYPE_U8) {
			MONO_EMIT_NEW_UNALU (cfg, OP_LMOVE, cfg->ret->dreg, val->dreg);
		} else {
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_MS (cfg->ret->dreg), MONO_LVREG_MS (val->dreg));
			MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, MONO_LVREG_LS (cfg->ret->dreg), MONO_LVREG_LS (val->dreg));
		}
		break;
	case ArgInFReg:
		if (ret->type == MONO_TYPE_R4)
			MONO_EMIT_NEW_UNALU (cfg, OP_SETFRET, cfg->ret->dreg, val->dreg);
		else
			MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
		break;
	default:
		g_assert_not_reached ();
	}

	g_assert (cinfo);
}

int cond_to_sparc_cond [][3] = {
	{sparc_be,   sparc_be,   sparc_fbe},
	{sparc_bne,  sparc_bne,  0},
	{sparc_ble,  sparc_ble,  sparc_fble},
	{sparc_bge,  sparc_bge,  sparc_fbge},
	{sparc_bl,   sparc_bl,   sparc_fbl},
	{sparc_bg,   sparc_bg,   sparc_fbg},
	{sparc_bleu, sparc_bleu, 0},
	{sparc_beu,  sparc_beu,  0},
	{sparc_blu,  sparc_blu,  sparc_fbl},
	{sparc_bgu,  sparc_bgu,  sparc_fbg}
};

/* Map opcode to the sparc condition codes */
static inline SparcCond
opcode_to_sparc_cond (int opcode)
{
	CompRelation rel;
	CompType t;

	switch (opcode) {
	case OP_COND_EXC_OV:
	case OP_COND_EXC_IOV:
		return sparc_bvs;
	case OP_COND_EXC_C:
	case OP_COND_EXC_IC:
		return sparc_bcs;
	case OP_COND_EXC_NO:
	case OP_COND_EXC_NC:
		NOT_IMPLEMENTED;
	default:
		rel = mono_opcode_to_cond (opcode);
		t = mono_opcode_to_type (opcode, -1);

		return cond_to_sparc_cond [rel][t];
		break;
	}

	return -1;
}

#define COMPUTE_DISP(ins) \
if (ins->inst_true_bb->native_offset) \
   disp = (ins->inst_true_bb->native_offset - ((guint8*)code - cfg->native_code)) >> 2; \
else { \
    disp = 0; \
	mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
}

#ifdef SPARCV9
#define DEFAULT_ICC sparc_xcc_short
#else
#define DEFAULT_ICC sparc_icc_short
#endif

#ifdef SPARCV9
#define EMIT_COND_BRANCH_ICC(ins,cond,annul,filldelay,icc) \
    do { \
        gint32 disp; \
        guint32 predict; \
        COMPUTE_DISP(ins); \
        predict = (disp != 0) ? 1 : 0; \
        g_assert (sparc_is_imm19 (disp)); \
        sparc_branchp (code, (annul), cond, icc, (predict), disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)
#define EMIT_COND_BRANCH(ins,cond,annul,filldelay) EMIT_COND_BRANCH_ICC ((ins), (cond), (annul), (filldelay), (sparc_xcc_short))
#define EMIT_FLOAT_COND_BRANCH(ins,cond,annul,filldelay) \
    do { \
        gint32 disp; \
        guint32 predict; \
        COMPUTE_DISP(ins); \
        predict = (disp != 0) ? 1 : 0; \
        g_assert (sparc_is_imm19 (disp)); \
        sparc_fbranch (code, (annul), cond, disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)
#else
#define EMIT_COND_BRANCH_ICC(ins,cond,annul,filldelay,icc) g_assert_not_reached ()
#define EMIT_COND_BRANCH_GENERAL(ins,bop,cond,annul,filldelay) \
    do { \
        gint32 disp; \
        COMPUTE_DISP(ins); \
        g_assert (sparc_is_imm22 (disp)); \
        sparc_ ## bop (code, (annul), cond, disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)
#define EMIT_COND_BRANCH(ins,cond,annul,filldelay) EMIT_COND_BRANCH_GENERAL((ins),branch,(cond),annul,filldelay)
#define EMIT_FLOAT_COND_BRANCH(ins,cond,annul,filldelay) EMIT_COND_BRANCH_GENERAL((ins),fbranch,(cond),annul,filldelay)
#endif

#define EMIT_COND_BRANCH_PREDICTED(ins,cond,annul,filldelay) \
    do { \
	    gint32 disp; \
        guint32 predict; \
        COMPUTE_DISP(ins); \
        predict = (disp != 0) ? 1 : 0; \
        g_assert (sparc_is_imm19 (disp)); \
		sparc_branchp (code, (annul), (cond), DEFAULT_ICC, (predict), disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)

#define EMIT_COND_BRANCH_BPR(ins,bop,predict,annul,filldelay) \
    do { \
	    gint32 disp; \
        COMPUTE_DISP(ins); \
		g_assert (sparc_is_imm22 (disp)); \
		sparc_ ## bop (code, (annul), (predict), ins->sreg1, disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)

/* emit an exception if condition is fail */
/*
 * We put the exception throwing code out-of-line, at the end of the method
 */
#define EMIT_COND_SYSTEM_EXCEPTION_GENERAL(ins,cond,sexc_name,filldelay,icc) do {     \
		mono_add_patch_info (cfg, (guint8*)(code) - (cfg)->native_code,   \
				    MONO_PATCH_INFO_EXC, sexc_name);  \
        if (mono_hwcap_sparc_is_v9 && ((icc) != sparc_icc_short)) {          \
           sparc_branchp (code, 0, (cond), (icc), 0, 0); \
        } \
        else { \
			sparc_branch (code, 0, cond, 0);     \
        } \
        if (filldelay) sparc_nop (code);     \
	} while (0); 

#define EMIT_COND_SYSTEM_EXCEPTION(ins,cond,sexc_name) EMIT_COND_SYSTEM_EXCEPTION_GENERAL(ins,cond,sexc_name,TRUE,DEFAULT_ICC)

#define EMIT_COND_SYSTEM_EXCEPTION_BPR(ins,bop,sexc_name) do { \
		mono_add_patch_info (cfg, (guint8*)(code) - (cfg)->native_code,   \
				    MONO_PATCH_INFO_EXC, sexc_name);  \
		sparc_ ## bop (code, FALSE, FALSE, ins->sreg1, 0); \
        sparc_nop (code);    \
} while (0);

#define EMIT_ALU_IMM(ins,op,setcc) do { \
			if (sparc_is_imm13 ((ins)->inst_imm)) \
				sparc_ ## op ## _imm (code, (setcc), (ins)->sreg1, ins->inst_imm, (ins)->dreg); \
			else { \
				sparc_set (code, ins->inst_imm, sparc_o7); \
				sparc_ ## op (code, (setcc), (ins)->sreg1, sparc_o7, (ins)->dreg); \
			} \
} while (0);

#define EMIT_LOAD_MEMBASE(ins,op) do { \
			if (sparc_is_imm13 (ins->inst_offset)) \
				sparc_ ## op ## _imm (code, ins->inst_basereg, ins->inst_offset, ins->dreg); \
			else { \
				sparc_set (code, ins->inst_offset, sparc_o7); \
				sparc_ ## op (code, ins->inst_basereg, sparc_o7, ins->dreg); \
			} \
} while (0);

/* max len = 5 */
#define EMIT_STORE_MEMBASE_IMM(ins,op) do { \
			guint32 sreg; \
			if (ins->inst_imm == 0) \
				sreg = sparc_g0; \
			else { \
				sparc_set (code, ins->inst_imm, sparc_o7); \
				sreg = sparc_o7; \
			} \
			if (!sparc_is_imm13 (ins->inst_offset)) { \
				sparc_set (code, ins->inst_offset, GP_SCRATCH_REG); \
				sparc_ ## op (code, sreg, ins->inst_destbasereg, GP_SCRATCH_REG); \
			} \
			else \
				sparc_ ## op ## _imm (code, sreg, ins->inst_destbasereg, ins->inst_offset); \
																						 } while (0);

#define EMIT_STORE_MEMBASE_REG(ins,op) do { \
			if (!sparc_is_imm13 (ins->inst_offset)) { \
				sparc_set (code, ins->inst_offset, sparc_o7); \
				sparc_ ## op (code, ins->sreg1, ins->inst_destbasereg, sparc_o7); \
			} \
				  else \
				sparc_ ## op ## _imm (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset); \
																						 } while (0);

#define EMIT_CALL() do { \
    if (v64) { \
        sparc_set_template (code, sparc_o7); \
        sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7); \
    } \
    else { \
        sparc_call_simple (code, 0); \
    } \
    sparc_nop (code); \
} while (0);

/*
 * A call template is 7 instructions long, so we want to avoid it if possible.
 */
static guint32*
emit_call (MonoCompile *cfg, guint32 *code, guint32 patch_type, gconstpointer data)
{
	ERROR_DECL (error);
	gpointer target;

	/* FIXME: This only works if the target method is already compiled */
	if (0 && v64 && !cfg->compile_aot) {
		MonoJumpInfo patch_info;

		patch_info.type = patch_type;
		patch_info.data.target = data;

		target = mono_resolve_patch_target (cfg->method, cfg->domain, NULL, &patch_info, FALSE, error);
		mono_error_raise_exception_deprecated (error); /* FIXME: don't raise here */

		/* FIXME: Add optimizations if the target is close enough */
		sparc_set (code, target, sparc_o7);
		sparc_jmpl (code, sparc_o7, sparc_g0, sparc_o7);
		sparc_nop (code);
	}
	else {
		mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, patch_type, data);
		EMIT_CALL ();
	}
	
	return code;
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
		case OP_MUL_IMM: 
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
#ifndef SPARCV9
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
					MONO_DELETE_INS (bb, ins);
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
					MONO_DELETE_INS (bb, ins);
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
#endif
		case OP_LOADI1_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					MONO_DELETE_INS (bb, ins);
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}
			}
			break;
		case OP_LOADI2_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				if (ins->dreg == last_ins->sreg1) {
					MONO_DELETE_INS (bb, ins);
					continue;
				} else {
					//static int c = 0; printf ("MATCHX %s %d\n", cfg->method->name,c++);
					ins->opcode = OP_MOVE;
					ins->sreg1 = last_ins->sreg1;
				}
			}
			break;
		case OP_STOREI4_MEMBASE_IMM:
			/* Convert pairs of 0 stores to a dword 0 store */
			/* Used when initializing temporaries */
			/* We know sparc_fp is dword aligned */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_IMM) &&
				(ins->inst_destbasereg == last_ins->inst_destbasereg) && 
				(ins->inst_destbasereg == sparc_fp) &&
				(ins->inst_offset < 0) &&
				((ins->inst_offset % 8) == 0) &&
				((ins->inst_offset == last_ins->inst_offset - 4)) &&
				(ins->inst_imm == 0) &&
				(last_ins->inst_imm == 0)) {
				if (mono_hwcap_sparc_is_v9) {
					last_ins->opcode = OP_STOREI8_MEMBASE_IMM;
					last_ins->inst_offset = ins->inst_offset;
					MONO_DELETE_INS (bb, ins);
					continue;
				}
			}
			break;
		case OP_IBEQ:
		case OP_IBNE_UN:
		case OP_IBLT:
		case OP_IBGT:
		case OP_IBGE:
		case OP_IBLE:
		case OP_COND_EXC_EQ:
		case OP_COND_EXC_GE:
		case OP_COND_EXC_GT:
		case OP_COND_EXC_LE:
		case OP_COND_EXC_LT:
		case OP_COND_EXC_NE_UN:
			/*
			 * Convert compare with zero+branch to BRcc
			 */
			/* 
			 * This only works in 64 bit mode, since it examines all 64
			 * bits of the register.
			 * Only do this if the method is small since BPr only has a 16bit
			 * displacement.
			 */
			if (v64 && (cfg->header->code_size < 10000) && last_ins && 
				(last_ins->opcode == OP_COMPARE_IMM) &&
				(last_ins->inst_imm == 0)) {
				switch (ins->opcode) {
				case OP_IBEQ:
					ins->opcode = OP_SPARC_BRZ;
					break;
				case OP_IBNE_UN:
					ins->opcode = OP_SPARC_BRNZ;
					break;
				case OP_IBLT:
					ins->opcode = OP_SPARC_BRLZ;
					break;
				case OP_IBGT:
					ins->opcode = OP_SPARC_BRGZ;
					break;
				case OP_IBGE:
					ins->opcode = OP_SPARC_BRGEZ;
					break;
				case OP_IBLE:
					ins->opcode = OP_SPARC_BRLEZ;
					break;
				case OP_COND_EXC_EQ:
					ins->opcode = OP_SPARC_COND_EXC_EQZ;
					break;
				case OP_COND_EXC_GE:
					ins->opcode = OP_SPARC_COND_EXC_GEZ;
					break;
				case OP_COND_EXC_GT:
					ins->opcode = OP_SPARC_COND_EXC_GTZ;
					break;
				case OP_COND_EXC_LE:
					ins->opcode = OP_SPARC_COND_EXC_LEZ;
					break;
				case OP_COND_EXC_LT:
					ins->opcode = OP_SPARC_COND_EXC_LTZ;
					break;
				case OP_COND_EXC_NE_UN:
					ins->opcode = OP_SPARC_COND_EXC_NEZ;
					break;
				default:
					g_assert_not_reached ();
				}
				ins->sreg1 = last_ins->sreg1;
				*last_ins = *ins;
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			break;
		case OP_MOVE:
			/* 
			 * OP_MOVE reg, reg 
			 */
			if (ins->dreg == ins->sreg1) {
				MONO_DELETE_INS (bb, ins);
				continue;
			}
			/* 
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
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *ins)
{
	switch (ins->opcode) {
	case OP_LNEG:
		MONO_EMIT_NEW_BIALU (cfg, OP_SUBCC, MONO_LVREG_LS (ins->dreg), 0, MONO_LVREG_LS (ins->sreg1));
		MONO_EMIT_NEW_BIALU (cfg, OP_SBB, MONO_LVREG_MS (ins->dreg), 0, MONO_LVREG_MS (ins->sreg1));
		NULLIFY_INS (ins);
		break;
	default:
		break;
	}
}

void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

/* FIXME: Strange loads from the stack in basic-float.cs:test_2_rem */

static void
sparc_patch (guint32 *code, const gpointer target)
{
	guint32 *c = code;
	guint32 ins = *code;
	guint32 op = ins >> 30;
	guint32 op2 = (ins >> 22) & 0x7;
	guint32 rd = (ins >> 25) & 0x1f;
	guint8* target8 = (guint8*)target;
	gint64 disp = (target8 - (guint8*)code) >> 2;
	int reg;

//	g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);

	if ((op == 0) && (op2 == 2)) {
		if (!sparc_is_imm22 (disp))
			NOT_IMPLEMENTED;
		/* Bicc */
		*code = ((ins >> 22) << 22) | (disp & 0x3fffff);
	}
	else if ((op == 0) && (op2 == 1)) {
		if (!sparc_is_imm19 (disp))
			NOT_IMPLEMENTED;
		/* BPcc */
		*code = ((ins >> 19) << 19) | (disp & 0x7ffff);
	}
	else if ((op == 0) && (op2 == 3)) {
		if (!sparc_is_imm16 (disp))
			NOT_IMPLEMENTED;
		/* BPr */
		*code &= ~(0x180000 | 0x3fff);
		*code |= ((disp << 21) & (0x180000)) | (disp & 0x3fff);
	}
	else if ((op == 0) && (op2 == 6)) {
		if (!sparc_is_imm22 (disp))
			NOT_IMPLEMENTED;
		/* FBicc */
		*code = ((ins >> 22) << 22) | (disp & 0x3fffff);
	}
	else if ((op == 0) && (op2 == 4)) {
		guint32 ins2 = code [1];

		if (((ins2 >> 30) == 2) && (((ins2 >> 19) & 0x3f) == 2)) {
			/* sethi followed by or */			
			guint32 *p = code;
			sparc_set (p, target8, rd);
			while (p <= (code + 1))
				sparc_nop (p);
		}
		else if (ins2 == 0x01000000) {
			/* sethi followed by nop */
			guint32 *p = code;
			sparc_set (p, target8, rd);
			while (p <= (code + 1))
				sparc_nop (p);
		}
		else if ((sparc_inst_op (ins2) == 3) && (sparc_inst_imm (ins2))) {
			/* sethi followed by load/store */
#ifndef SPARCV9
			guint32 t = (guint32)target8;
			*code &= ~(0x3fffff);
			*code |= (t >> 10);
			*(code + 1) &= ~(0x3ff);
			*(code + 1) |= (t & 0x3ff);
#endif
		}
		else if (v64 && 
				 (sparc_inst_rd (ins) == sparc_g1) &&
				 (sparc_inst_op (c [1]) == 0) && (sparc_inst_op2 (c [1]) == 4) &&
				 (sparc_inst_op (c [2]) == 2) && (sparc_inst_op3 (c [2]) == 2) &&
				 (sparc_inst_op (c [3]) == 2) && (sparc_inst_op3 (c [3]) == 2))
		{
			/* sparc_set */
			guint32 *p = c;
			reg = sparc_inst_rd (c [1]);
			sparc_set (p, target8, reg);
			while (p < (c + 6))
				sparc_nop (p);
		}
		else if ((sparc_inst_op (ins2) == 2) && (sparc_inst_op3 (ins2) == 0x38) && 
				 (sparc_inst_imm (ins2))) {
			/* sethi followed by jmpl */
#ifndef SPARCV9
			guint32 t = (guint32)target8;
			*code &= ~(0x3fffff);
			*code |= (t >> 10);
			*(code + 1) &= ~(0x3ff);
			*(code + 1) |= (t & 0x3ff);
#endif
		}
		else
			NOT_IMPLEMENTED;
	}
	else if (op == 01) {
		gint64 disp = (target8 - (guint8*)code) >> 2;

		if (!sparc_is_imm30 (disp))
			NOT_IMPLEMENTED;
		sparc_call_simple (code, target8 - (guint8*)code);
	}
	else if ((op == 2) && (sparc_inst_op3 (ins) == 0x2) && sparc_inst_imm (ins)) {
		/* mov imm, reg */
		g_assert (sparc_is_imm13 (target8));
		*code &= ~(0x1fff);
		*code |= (guint32)target8;
	}
	else if ((sparc_inst_op (ins) == 2) && (sparc_inst_op3 (ins) == 0x7)) {
		/* sparc_set case 5. */
		guint32 *p = c;

		g_assert (v64);
		reg = sparc_inst_rd (c [3]);
		sparc_set (p, target, reg);
		while (p < (c + 6))
			sparc_nop (p);
	}
	else
		NOT_IMPLEMENTED;

//	g_print ("patched with 0x%08x\n", ins);
}

/*
 * mono_sparc_emit_save_lmf:
 *
 *  Emit the code neccesary to push a new entry onto the lmf stack. Used by
 * trampolines as well.
 */
guint32*
mono_sparc_emit_save_lmf (guint32 *code, guint32 lmf_offset)
{
	/* Save lmf_addr */
	sparc_sti_imm (code, sparc_o0, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* Save previous_lmf */
	sparc_ldi (code, sparc_o0, sparc_g0, sparc_o7);
	sparc_sti_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* Set new lmf */
	sparc_add_imm (code, FALSE, sparc_fp, lmf_offset, sparc_o7);
	sparc_sti (code, sparc_o7, sparc_o0, sparc_g0);

	return code;
}

guint32*
mono_sparc_emit_restore_lmf (guint32 *code, guint32 lmf_offset)
{
	/* Load previous_lmf */
	sparc_ldi_imm (code, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), sparc_l0);
	/* Load lmf_addr */
	sparc_ldi_imm (code, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), sparc_l1);
	/* *(lmf) = previous_lmf */
	sparc_sti (code, sparc_l0, sparc_l1, sparc_g0);
	return code;
}

static guint32*
emit_save_sp_to_lmf (MonoCompile *cfg, guint32 *code)
{
	/*
	 * Since register windows are saved to the current value of %sp, we need to
	 * set the sp field in the lmf before the call, not in the prolog.
	 */
	if (cfg->method->save_lmf) {
		gint32 lmf_offset = MONO_SPARC_STACK_BIAS - cfg->arch.lmf_offset;

		/* Save sp */
		sparc_sti_imm (code, sparc_sp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, sp));
	}

	return code;
}

static guint32*
emit_vret_token (MonoInst *ins, guint32 *code)
{
	MonoCallInst *call = (MonoCallInst*)ins;
	guint32 size;

	/* 
	 * The sparc ABI requires that calls to functions which return a structure
	 * contain an additional unimpl instruction which is checked by the callee.
	 */
	if (call->signature->pinvoke && MONO_TYPE_ISSTRUCT(call->signature->ret)) {
		if (call->signature->ret->type == MONO_TYPE_TYPEDBYREF)
			size = mini_type_stack_size (call->signature->ret, NULL);
		else
			size = mono_class_native_size (call->signature->ret->data.klass, NULL);
		sparc_unimp (code, size & 0xfff);
	}

	return code;
}

static guint32*
emit_move_return_value (MonoInst *ins, guint32 *code)
{
	/* Move return value to the target register */
	/* FIXME: do more things in the local reg allocator */
	switch (ins->opcode) {
	case OP_VOIDCALL:
	case OP_VOIDCALL_REG:
	case OP_VOIDCALL_MEMBASE:
		break;
	case OP_CALL:
	case OP_CALL_REG:
	case OP_CALL_MEMBASE:
		g_assert (ins->dreg == sparc_o0);
		break;
	case OP_LCALL:
	case OP_LCALL_REG:
	case OP_LCALL_MEMBASE:
		/* 
		 * ins->dreg is the least significant reg due to the lreg: LCALL rule
		 * in inssel-long32.brg.
		 */
#ifdef SPARCV9
		sparc_mov_reg_reg (code, sparc_o0, ins->dreg);
#else
		g_assert (ins->dreg == sparc_o1);
#endif
		break;
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
#ifdef SPARCV9
		if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4) {
			sparc_fmovs (code, sparc_f0, ins->dreg);
			sparc_fstod (code, ins->dreg, ins->dreg);
		}
		else
			sparc_fmovd (code, sparc_f0, ins->dreg);
#else		
		sparc_fmovs (code, sparc_f0, ins->dreg);
		if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4)
			sparc_fstod (code, ins->dreg, ins->dreg);
		else
			sparc_fmovs (code, sparc_f1, ins->dreg + 1);
#endif
		break;
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
	case OP_VCALL2:
	case OP_VCALL2_REG:
	case OP_VCALL2_MEMBASE:
		break;
	default:
		NOT_IMPLEMENTED;
	}

	return code;
}

/*
 * emit_load_volatile_arguments:
 *
 *  Load volatile arguments from the stack to the original input registers.
 * Required before a tailcall.
 */
static guint32*
emit_load_volatile_arguments (MonoCompile *cfg, guint32 *code)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *inst;
	CallInfo *cinfo;
	guint32 i, ireg;

	/* FIXME: Generate intermediate code instead */

	sig = mono_method_signature_internal (method);

	cinfo = get_call_info (cfg, sig, FALSE);
	
	/* This is the opposite of the code in emit_prolog */

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;
		MonoType *arg_type;

		inst = cfg->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = mono_get_object_type ();
		else
			arg_type = sig->params [i - sig->hasthis];

		stack_offset = ainfo->offset + ARGS_OFFSET;
		ireg = sparc_i0 + ainfo->reg;

		if (ainfo->storage == ArgInSplitRegStack) {
			g_assert (inst->opcode == OP_REGOFFSET);

			if (!sparc_is_imm13 (stack_offset))
				NOT_IMPLEMENTED;
			sparc_st_imm (code, inst->inst_basereg, stack_offset, sparc_i5);
		}

		if (!v64 && !arg_type->byref && (arg_type->type == MONO_TYPE_R8)) {
			if (ainfo->storage == ArgInIRegPair) {
				if (!sparc_is_imm13 (inst->inst_offset + 4))
					NOT_IMPLEMENTED;
				sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset, ireg);
				sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset + 4, ireg + 1);
			}
			else
				if (ainfo->storage == ArgInSplitRegStack) {
					if (stack_offset != inst->inst_offset) {
						sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset, sparc_i5);
						sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset + 4, sparc_o7);
						sparc_st_imm (code, sparc_o7, sparc_fp, stack_offset + 4);

					}
				}
			else
				if (ainfo->storage == ArgOnStackPair) {
					if (stack_offset != inst->inst_offset) {
						/* stack_offset is not dword aligned, so we need to make a copy */
						sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset, sparc_o7);
						sparc_st_imm (code, sparc_o7, sparc_fp, stack_offset);

						sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset + 4, sparc_o7);
						sparc_st_imm (code, sparc_o7, sparc_fp, stack_offset + 4);

					}
				}
			 else
				g_assert_not_reached ();
		}
		else
			if ((ainfo->storage == ArgInIReg) && (inst->opcode != OP_REGVAR)) {
				/* Argument in register, but need to be saved to stack */
				if (!sparc_is_imm13 (stack_offset))
					NOT_IMPLEMENTED;
				if ((stack_offset - ARGS_OFFSET) & 0x1)
					/* FIXME: Is this ldsb or ldub ? */
					sparc_ldsb_imm (code, inst->inst_basereg, stack_offset, ireg);
				else
					if ((stack_offset - ARGS_OFFSET) & 0x2)
						sparc_ldsh_imm (code, inst->inst_basereg, stack_offset, ireg);
				else
					if ((stack_offset - ARGS_OFFSET) & 0x4)
						sparc_ld_imm (code, inst->inst_basereg, stack_offset, ireg);
					else {
						if (v64)
							sparc_ldx_imm (code, inst->inst_basereg, stack_offset, ireg);
						else
							sparc_ld_imm (code, inst->inst_basereg, stack_offset, ireg);
					}
			}
			else if ((ainfo->storage == ArgInIRegPair) && (inst->opcode != OP_REGVAR)) {
				/* Argument in regpair, but need to be saved to stack */
				if (!sparc_is_imm13 (inst->inst_offset + 4))
					NOT_IMPLEMENTED;
				sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset, ireg);
				sparc_st_imm (code, inst->inst_basereg, inst->inst_offset + 4, ireg + 1);
			}
			else if ((ainfo->storage == ArgInFloatReg) && (inst->opcode != OP_REGVAR)) {
				NOT_IMPLEMENTED;
			}
			else if ((ainfo->storage == ArgInDoubleReg) && (inst->opcode != OP_REGVAR)) {
				NOT_IMPLEMENTED;
			}

		if ((ainfo->storage == ArgInSplitRegStack) || (ainfo->storage == ArgOnStack))
			if (inst->opcode == OP_REGVAR)
				/* FIXME: Load the argument into memory */
				NOT_IMPLEMENTED;
	}

	g_free (cinfo);

	return code;
}

/*
 * mono_sparc_is_virtual_call:
 *
 *  Determine whenever the instruction at CODE is a virtual call.
 */
gboolean 
mono_sparc_is_virtual_call (guint32 *code)
{
	guint32 buf[1];
	guint32 *p;

	p = buf;

	if ((sparc_inst_op (*code) == 0x2) && (sparc_inst_op3 (*code) == 0x38)) {
		/*
		 * Register indirect call. If it is a virtual call, then the 
		 * instruction in the delay slot is a special kind of nop.
		 */

		/* Construct special nop */
		sparc_or_imm (p, FALSE, sparc_g0, 0xca, sparc_g0);
		p --;

		if (code [1] == p [0])
			return TRUE;
	}

	return FALSE;
}

#define CMP_SIZE 3
#define BR_SMALL_SIZE 2
#define BR_LARGE_SIZE 2
#define JUMP_IMM_SIZE 5
#define ENABLE_WRONG_METHOD_CHECK 0

/*
 * LOCKING: called with the domain lock held
 */
gpointer
mono_arch_build_imt_trampoline (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count,
								gpointer fail_tramp)
{
	int i;
	int size = 0;
	guint32 *code, *start;

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done)
					item->chunk_size += CMP_SIZE;
				item->chunk_size += BR_SMALL_SIZE + JUMP_IMM_SIZE;
			} else {
				if (fail_tramp)
					item->chunk_size += 16;
				item->chunk_size += JUMP_IMM_SIZE;
#if ENABLE_WRONG_METHOD_CHECK
				item->chunk_size += CMP_SIZE + BR_SMALL_SIZE + 1;
#endif
			}
		} else {
			item->chunk_size += CMP_SIZE + BR_LARGE_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}
	if (fail_tramp)
		code = mono_method_alloc_generic_virtual_trampoline (domain, size * 4);
	else
		code = mono_domain_code_reserve (domain, size * 4);
	start = code;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = (guint8*)code;
		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case) {
					sparc_set (code, (guint32)item->key, sparc_g5);
					sparc_cmp (code, MONO_ARCH_IMT_REG, sparc_g5);
				}
				item->jmp_code = (guint8*)code;
				sparc_branch (code, 0, sparc_bne, 0);
				sparc_nop (code);
				if (item->has_target_code) {
					sparc_set (code, item->value.target_code, sparc_f5);
				} else {
					sparc_set (code, ((guint32)(&(vtable->vtable [item->value.vtable_slot]))), sparc_g5);
					sparc_ld (code, sparc_g5, 0, sparc_g5);
				}
				sparc_jmpl (code, sparc_g5, sparc_g0, sparc_g0);
				sparc_nop (code);

				if (fail_case) {
					sparc_patch (item->jmp_code, code);
					sparc_set (code, fail_tramp, sparc_g5);
					sparc_jmpl (code, sparc_g5, sparc_g0, sparc_g0);
					sparc_nop (code);
					item->jmp_code = NULL;
				}
			} else {
				/* enable the commented code to assert on wrong method */
#if ENABLE_WRONG_METHOD_CHECK
				g_assert_not_reached ();
#endif
				sparc_set (code, ((guint32)(&(vtable->vtable [item->value.vtable_slot]))), sparc_g5);
				sparc_ld (code, sparc_g5, 0, sparc_g5);
				sparc_jmpl (code, sparc_g5, sparc_g0, sparc_g0);
				sparc_nop (code);
#if ENABLE_WRONG_METHOD_CHECK
				g_assert_not_reached ();
#endif
			}
		} else {
			sparc_set (code, (guint32)item->key, sparc_g5);
			sparc_cmp (code, MONO_ARCH_IMT_REG, sparc_g5);
			item->jmp_code = (guint8*)code;
			sparc_branch (code, 0, sparc_beu, 0);
			sparc_nop (code);
		}
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				sparc_patch ((guint32*)item->jmp_code, imt_entries [item->check_target_idx]->code_target);
			}
		}
	}

	mono_arch_flush_icache ((guint8*)start, (code - start));
	MONO_PROFILER_RAISE (jit_code_buffer, (start, code - start, MONO_PROFILER_CODE_BUFFER_IMT_TRAMPOLINE, NULL));

	UnlockedAdd (&mono_stats.imt_trampolines_size, (code - start));
	g_assert (code - start <= size);

	mono_tramp_info_register (mono_tramp_info_create (NULL, start, code - start, NULL, NULL), domain);

	return start;
}

MonoMethod*
mono_arch_find_imt_method (host_mgreg_t *regs, guint8 *code)
{
#ifdef SPARCV9
	g_assert_not_reached ();
#endif

	return (MonoMethod*)regs [sparc_g1];
}

gpointer
mono_arch_get_this_arg_from_call (host_mgreg_t *regs, guint8 *code)
{
	mono_sparc_flushw ();

	return (gpointer)regs [sparc_o0];
}

/*
 * Some conventions used in the following code.
 * 2) The only scratch registers we have are o7 and g1.  We try to
 * stick to o7 when we can, and use g1 when necessary.
 */

void
mono_arch_output_basic_block (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins;
	MonoCallInst *call;
	guint offset;
	guint32 *code = (guint32*)(cfg->native_code + cfg->code_len);
	MonoInst *last_ins = NULL;
	int max_len, cpos;
	const char *spec;

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	MONO_BB_FOR_EACH_INS (bb, ins) {
		guint8* code_start;

		offset = (guint8*)code - cfg->native_code;
		spec = ins_get_spec (ins->opcode);
		max_len = ins_get_size (ins->opcode);
		code = realloc_code (cfg, max_len);
		code_start = (guint8*)code;
		//	if (ins->cil_code)
		//		g_print ("cil code\n");
		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_STOREI1_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, stb);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, sth);
			break;
		case OP_STORE_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, sti);
			break;
		case OP_STOREI4_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, st);
			break;
		case OP_STOREI8_MEMBASE_IMM:
#ifdef SPARCV9
			EMIT_STORE_MEMBASE_IMM (ins, stx);
#else
			/* Only generated by peephole opts */
			g_assert ((ins->inst_offset % 8) == 0);
			g_assert (ins->inst_imm == 0);
			EMIT_STORE_MEMBASE_IMM (ins, stx);
#endif
			break;
		case OP_STOREI1_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, stb);
			break;
		case OP_STOREI2_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, sth);
			break;
		case OP_STOREI4_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, st);
			break;
		case OP_STOREI8_MEMBASE_REG:
#ifdef SPARCV9
			EMIT_STORE_MEMBASE_REG (ins, stx);
#else
			/* Only used by OP_MEMSET */
			EMIT_STORE_MEMBASE_REG (ins, std);
#endif
			break;
		case OP_STORE_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, sti);
			break;
		case OP_LOADU4_MEM:
			sparc_set (code, ins->inst_c0, ins->dreg);
			sparc_ld (code, ins->dreg, sparc_g0, ins->dreg);
			break;
		case OP_LOADI4_MEMBASE:
#ifdef SPARCV9
			EMIT_LOAD_MEMBASE (ins, ldsw);
#else
			EMIT_LOAD_MEMBASE (ins, ld);
#endif
			break;
		case OP_LOADU4_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ld);
			break;
		case OP_LOADU1_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldub);
			break;
		case OP_LOADI1_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldsb);
			break;
		case OP_LOADU2_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, lduh);
			break;
		case OP_LOADI2_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldsh);
			break;
		case OP_LOAD_MEMBASE:
#ifdef SPARCV9
				EMIT_LOAD_MEMBASE (ins, ldx);
#else
				EMIT_LOAD_MEMBASE (ins, ld);
#endif
			break;
#ifdef SPARCV9
		case OP_LOADI8_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldx);
			break;
#endif
		case OP_ICONV_TO_I1:
			sparc_sll_imm (code, ins->sreg1, 24, sparc_o7);
			sparc_sra_imm (code, sparc_o7, 24, ins->dreg);
			break;
		case OP_ICONV_TO_I2:
			sparc_sll_imm (code, ins->sreg1, 16, sparc_o7);
			sparc_sra_imm (code, sparc_o7, 16, ins->dreg);
			break;
		case OP_ICONV_TO_U1:
			sparc_and_imm (code, FALSE, ins->sreg1, 0xff, ins->dreg);
			break;
		case OP_ICONV_TO_U2:
			sparc_sll_imm (code, ins->sreg1, 16, sparc_o7);
			sparc_srl_imm (code, sparc_o7, 16, ins->dreg);
			break;
		case OP_LCONV_TO_OVF_U4:
		case OP_ICONV_TO_OVF_U4:
			/* Only used on V9 */
			sparc_cmp_imm (code, ins->sreg1, 0);
			mono_add_patch_info (cfg, (guint8*)(code) - (cfg)->native_code,
								 MONO_PATCH_INFO_EXC, "OverflowException");
			sparc_branchp (code, 0, sparc_bl, sparc_xcc_short, 0, 0);
			/* Delay slot */
			sparc_set (code, 1, sparc_o7);
			sparc_sllx_imm (code, sparc_o7, 32, sparc_o7);
			sparc_cmp (code, ins->sreg1, sparc_o7);
			mono_add_patch_info (cfg, (guint8*)(code) - (cfg)->native_code,
								 MONO_PATCH_INFO_EXC, "OverflowException");
			sparc_branchp (code, 0, sparc_bge, sparc_xcc_short, 0, 0);
			sparc_nop (code);
			sparc_mov_reg_reg (code, ins->sreg1, ins->dreg);
			break;
		case OP_LCONV_TO_OVF_I4_UN:
		case OP_ICONV_TO_OVF_I4_UN:
			/* Only used on V9 */
			NOT_IMPLEMENTED;
			break;
		case OP_COMPARE:
		case OP_LCOMPARE:
		case OP_ICOMPARE:
			sparc_cmp (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
			if (sparc_is_imm13 (ins->inst_imm))
				sparc_cmp_imm (code, ins->sreg1, ins->inst_imm);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_cmp (code, ins->sreg1, sparc_o7);
			}
			break;
		case OP_BREAK:
			/*
			 * gdb does not like encountering 'ta 1' in the debugged code. So 
			 * instead of emitting a trap, we emit a call a C function and place a 
			 * breakpoint there.
			 */
			//sparc_ta (code, 1);
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, mono_break);
			EMIT_CALL();
			break;
		case OP_ADDCC:
		case OP_IADDCC:
			sparc_add (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_IADD:
			sparc_add (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ADDCC_IMM:
		case OP_ADD_IMM:
		case OP_IADD_IMM:
			/* according to inssel-long32.brg, this should set cc */
			EMIT_ALU_IMM (ins, add, TRUE);
			break;
		case OP_ADC:
		case OP_IADC:
			/* according to inssel-long32.brg, this should set cc */
			sparc_addx (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ADC_IMM:
		case OP_IADC_IMM:
			EMIT_ALU_IMM (ins, addx, TRUE);
			break;
		case OP_SUBCC:
		case OP_ISUBCC:
			sparc_sub (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ISUB:
			sparc_sub (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SUBCC_IMM:
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
			/* according to inssel-long32.brg, this should set cc */
			EMIT_ALU_IMM (ins, sub, TRUE);
			break;
		case OP_SBB:
		case OP_ISBB:
			/* according to inssel-long32.brg, this should set cc */
			sparc_subx (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SBB_IMM:
		case OP_ISBB_IMM:
			EMIT_ALU_IMM (ins, subx, TRUE);
			break;
		case OP_IAND:
			sparc_and (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_AND_IMM:
		case OP_IAND_IMM:
			EMIT_ALU_IMM (ins, and, FALSE);
			break;
		case OP_IDIV:
			/* Sign extend sreg1 into %y */
			sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
			sparc_wry (code, sparc_o7, sparc_g0);
			sparc_sdiv (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			EMIT_COND_SYSTEM_EXCEPTION_GENERAL (code, sparc_boverflow, "ArithmeticException", TRUE, sparc_icc_short);
			break;
		case OP_IDIV_UN:
			sparc_wry (code, sparc_g0, sparc_g0);
			sparc_udiv (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_DIV_IMM:
		case OP_IDIV_IMM: {
			int i, imm;

			/* Transform division into a shift */
			for (i = 1; i < 30; ++i) {
				imm = (1 << i);
				if (ins->inst_imm == imm)
					break;
			}
			if (i < 30) {
				if (i == 1) {
					/* gcc 2.95.3 */
					sparc_srl_imm (code, ins->sreg1, 31, sparc_o7);
					sparc_add (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
					sparc_sra_imm (code, ins->dreg, 1, ins->dreg);
				}
				else {
					/* http://compilers.iecc.com/comparch/article/93-04-079 */
					sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
					sparc_srl_imm (code, sparc_o7, 32 - i, sparc_o7);
					sparc_add (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
					sparc_sra_imm (code, ins->dreg, i, ins->dreg);
				}
			}
			else {
				/* Sign extend sreg1 into %y */
				sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
				sparc_wry (code, sparc_o7, sparc_g0);
				EMIT_ALU_IMM (ins, sdiv, TRUE);
				EMIT_COND_SYSTEM_EXCEPTION_GENERAL (code, sparc_boverflow, "ArithmeticException", TRUE, sparc_icc_short);
			}
			break;
		}
		case OP_IDIV_UN_IMM:
			sparc_wry (code, sparc_g0, sparc_g0);
			EMIT_ALU_IMM (ins, udiv, FALSE);
			break;
		case OP_IREM:
			/* Sign extend sreg1 into %y */
			sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
			sparc_wry (code, sparc_o7, sparc_g0);
			sparc_sdiv (code, TRUE, ins->sreg1, ins->sreg2, sparc_o7);
			EMIT_COND_SYSTEM_EXCEPTION_GENERAL (code, sparc_boverflow, "ArithmeticException", TRUE, sparc_icc_short);
			sparc_smul (code, FALSE, ins->sreg2, sparc_o7, sparc_o7);
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case OP_IREM_UN:
			sparc_wry (code, sparc_g0, sparc_g0);
			sparc_udiv (code, FALSE, ins->sreg1, ins->sreg2, sparc_o7);
			sparc_umul (code, FALSE, ins->sreg2, sparc_o7, sparc_o7);
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case OP_REM_IMM:
		case OP_IREM_IMM:
			/* Sign extend sreg1 into %y */
			sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
			sparc_wry (code, sparc_o7, sparc_g0);
			if (!sparc_is_imm13 (ins->inst_imm)) {
				sparc_set (code, ins->inst_imm, GP_SCRATCH_REG);
				sparc_sdiv (code, TRUE, ins->sreg1, GP_SCRATCH_REG, sparc_o7);
				EMIT_COND_SYSTEM_EXCEPTION_GENERAL (code, sparc_boverflow, "ArithmeticException", TRUE, sparc_icc_short);
				sparc_smul (code, FALSE, sparc_o7, GP_SCRATCH_REG, sparc_o7);
			}
			else {
				sparc_sdiv_imm (code, TRUE, ins->sreg1, ins->inst_imm, sparc_o7);
				EMIT_COND_SYSTEM_EXCEPTION_GENERAL (code, sparc_boverflow, "ArithmeticException", TRUE, sparc_icc_short);
				sparc_smul_imm (code, FALSE, sparc_o7, ins->inst_imm, sparc_o7);
			}
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case OP_IREM_UN_IMM:
			sparc_set (code, ins->inst_imm, GP_SCRATCH_REG);
			sparc_wry (code, sparc_g0, sparc_g0);
			sparc_udiv (code, FALSE, ins->sreg1, GP_SCRATCH_REG, sparc_o7);
			sparc_umul (code, FALSE, GP_SCRATCH_REG, sparc_o7, sparc_o7);
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case OP_IOR:
			sparc_or (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_OR_IMM:
		case OP_IOR_IMM:
			EMIT_ALU_IMM (ins, or, FALSE);
			break;
		case OP_IXOR:
			sparc_xor (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
			EMIT_ALU_IMM (ins, xor, FALSE);
			break;
		case OP_ISHL:
			sparc_sll (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
			if (ins->inst_imm < (1 << 5))
				sparc_sll_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_sll (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_ISHR:
			sparc_sra (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ISHR_IMM:
		case OP_SHR_IMM:
			if (ins->inst_imm < (1 << 5))
				sparc_sra_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_sra (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_SHR_UN_IMM:
		case OP_ISHR_UN_IMM:
			if (ins->inst_imm < (1 << 5))
				sparc_srl_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_srl (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_ISHR_UN:
			sparc_srl (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_LSHL:
			sparc_sllx (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_LSHL_IMM:
			if (ins->inst_imm < (1 << 6))
				sparc_sllx_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_sllx (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_LSHR:
			sparc_srax (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_LSHR_IMM:
			if (ins->inst_imm < (1 << 6))
				sparc_srax_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_srax (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_LSHR_UN:
			sparc_srlx (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_LSHR_UN_IMM:
			if (ins->inst_imm < (1 << 6))
				sparc_srlx_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_srlx (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_INOT:
			/* can't use sparc_not */
			sparc_xnor (code, FALSE, ins->sreg1, sparc_g0, ins->dreg);
			break;
		case OP_INEG:
			/* can't use sparc_neg */
			sparc_sub (code, FALSE, sparc_g0, ins->sreg1, ins->dreg);
			break;
		case OP_IMUL:
			sparc_smul (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_IMUL_IMM:
		case OP_MUL_IMM: {
			int i, imm;

			if ((ins->inst_imm == 1) && (ins->sreg1 == ins->dreg))
				break;

			/* Transform multiplication into a shift */
			for (i = 0; i < 30; ++i) {
				imm = (1 << i);
				if (ins->inst_imm == imm)
					break;
			}
			if (i < 30)
				sparc_sll_imm (code, ins->sreg1, i, ins->dreg);
			else
				EMIT_ALU_IMM (ins, smul, FALSE);
			break;
		}
		case OP_IMUL_OVF:
			sparc_smul (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			sparc_rdy (code, sparc_g1);
			sparc_sra_imm (code, ins->dreg, 31, sparc_o7);
			sparc_cmp (code, sparc_g1, sparc_o7);
			EMIT_COND_SYSTEM_EXCEPTION_GENERAL (ins, sparc_bne, "OverflowException", TRUE, sparc_icc_short);
			break;
		case OP_IMUL_OVF_UN:
			sparc_umul (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			sparc_rdy (code, sparc_o7);
			sparc_cmp (code, sparc_o7, sparc_g0);
			EMIT_COND_SYSTEM_EXCEPTION_GENERAL (ins, sparc_bne, "OverflowException", TRUE, sparc_icc_short);
			break;
		case OP_ICONST:
			sparc_set (code, ins->inst_c0, ins->dreg);
			break;
		case OP_I8CONST:
			sparc_set (code, ins->inst_l, ins->dreg);
			break;
		case OP_AOTCONST:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			sparc_set_template (code, ins->dreg);
			break;
		case OP_JUMP_TABLE:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			sparc_set_template (code, ins->dreg);
			break;
		case OP_ICONV_TO_I4:
		case OP_ICONV_TO_U4:
		case OP_MOVE:
			if (ins->sreg1 != ins->dreg)
				sparc_mov_reg_reg (code, ins->sreg1, ins->dreg);
			break;
		case OP_FMOVE:
#ifdef SPARCV9
			if (ins->sreg1 != ins->dreg)
				sparc_fmovd (code, ins->sreg1, ins->dreg);
#else
			sparc_fmovs (code, ins->sreg1, ins->dreg);
			sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
#endif
			break;
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			/* Might be misaligned in case of vtypes so use a byte load */
			sparc_ldsb_imm (code, ins->sreg1, 0, sparc_g0);
			break;
		case OP_ARGLIST:
			sparc_add_imm (code, FALSE, sparc_fp, cfg->sig_cookie, sparc_o7);
			sparc_sti_imm (code, sparc_o7, ins->sreg1, 0);
			break;
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;
			g_assert (!call->virtual);
			code = emit_save_sp_to_lmf (cfg, code);
			if (ins->flags & MONO_INST_HAS_METHOD)
			    code = emit_call (cfg, code, MONO_PATCH_INFO_METHOD, call->method);
			else
			    code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, call->fptr);

			code = emit_vret_token (ins, code);
			code = emit_move_return_value (ins, code);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			call = (MonoCallInst*)ins;
			code = emit_save_sp_to_lmf (cfg, code);
			sparc_jmpl (code, ins->sreg1, sparc_g0, sparc_callsite);
			/*
			 * We emit a special kind of nop in the delay slot to tell the 
			 * trampoline code that this is a virtual call, thus an unbox
			 * trampoline might need to be called.
			 */
			if (call->virtual)
				sparc_or_imm (code, FALSE, sparc_g0, 0xca, sparc_g0);
			else
				sparc_nop (code);

			code = emit_vret_token (ins, code);
			code = emit_move_return_value (ins, code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			call = (MonoCallInst*)ins;
			code = emit_save_sp_to_lmf (cfg, code);
			if (sparc_is_imm13 (ins->inst_offset)) {
				sparc_ldi_imm (code, ins->inst_basereg, ins->inst_offset, sparc_o7);
			} else {
				sparc_set (code, ins->inst_offset, sparc_o7);
				sparc_ldi (code, ins->inst_basereg, sparc_o7, sparc_o7);
			}
			sparc_jmpl (code, sparc_o7, sparc_g0, sparc_callsite);
			if (call->virtual)
				sparc_or_imm (code, FALSE, sparc_g0, 0xca, sparc_g0);
			else
				sparc_nop (code);

			code = emit_vret_token (ins, code);
			code = emit_move_return_value (ins, code);
			break;
		case OP_SETFRET:
			if (mono_method_signature_internal (cfg->method)->ret->type == MONO_TYPE_R4)
				sparc_fdtos (code, ins->sreg1, sparc_f0);
			else {
#ifdef SPARCV9
				sparc_fmovd (code, ins->sreg1, ins->dreg);
#else
				/* FIXME: Why not use fmovd ? */
				sparc_fmovs (code, ins->sreg1, ins->dreg);
				sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
#endif
			}
			break;
		case OP_LOCALLOC: {
			guint32 size_reg;
			gint32 offset2;

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
			/* Perform stack touching */
			NOT_IMPLEMENTED;
#endif

			/* Keep alignment */
			/* Add 4 to compensate for the rounding of localloc_offset */
			sparc_add_imm (code, FALSE, ins->sreg1, 4 + MONO_ARCH_LOCALLOC_ALIGNMENT - 1, ins->dreg);
			sparc_set (code, ~(MONO_ARCH_LOCALLOC_ALIGNMENT - 1), sparc_o7);
			sparc_and (code, FALSE, ins->dreg, sparc_o7, ins->dreg);

			if ((ins->flags & MONO_INST_INIT) && (ins->sreg1 == ins->dreg)) {
#ifdef SPARCV9
				size_reg = sparc_g4;
#else
				size_reg = sparc_g1;
#endif
				sparc_mov_reg_reg (code, ins->dreg, size_reg);
			}
			else
				size_reg = ins->sreg1;

			sparc_sub (code, FALSE, sparc_sp, ins->dreg, ins->dreg);
			/* Keep %sp valid at all times */
			sparc_mov_reg_reg (code, ins->dreg, sparc_sp);
			/* Round localloc_offset too so the result is at least 8 aligned */
			offset2 = ALIGN_TO (cfg->arch.localloc_offset, 8);
			g_assert (sparc_is_imm13 (MONO_SPARC_STACK_BIAS + offset2));
			sparc_add_imm (code, FALSE, ins->dreg, MONO_SPARC_STACK_BIAS + offset2, ins->dreg);

			if (ins->flags & MONO_INST_INIT) {
				guint32 *br [3];
				/* Initialize memory region */
				sparc_cmp_imm (code, size_reg, 0);
				br [0] = code;
				sparc_branch (code, 0, sparc_be, 0);
				/* delay slot */
				sparc_set (code, 0, sparc_o7);
				sparc_sub_imm (code, 0, size_reg, mono_hwcap_sparc_is_v9 ? 8 : 4, size_reg);
				/* start of loop */
				br [1] = code;
				if (mono_hwcap_sparc_is_v9)
					sparc_stx (code, sparc_g0, ins->dreg, sparc_o7);
				else
					sparc_st (code, sparc_g0, ins->dreg, sparc_o7);
				sparc_cmp (code, sparc_o7, size_reg);
				br [2] = code;
				sparc_branch (code, 0, sparc_bl, 0);
				sparc_patch (br [2], br [1]);
				/* delay slot */
				sparc_add_imm (code, 0, sparc_o7, mono_hwcap_sparc_is_v9 ? 8 : 4, sparc_o7);
				sparc_patch (br [0], code);
			}
			break;
		}
		case OP_LOCALLOC_IMM: {
			gint32 offset = ins->inst_imm;
			gint32 offset2;
			
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
			/* Perform stack touching */
			NOT_IMPLEMENTED;
#endif

			/* To compensate for the rounding of localloc_offset */
			offset += sizeof (target_mgreg_t);
			offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);
			if (sparc_is_imm13 (offset))
				sparc_sub_imm (code, FALSE, sparc_sp, offset, sparc_sp);
			else {
				sparc_set (code, offset, sparc_o7);
				sparc_sub (code, FALSE, sparc_sp, sparc_o7, sparc_sp);
			}
			/* Round localloc_offset too so the result is at least 8 aligned */
			offset2 = ALIGN_TO (cfg->arch.localloc_offset, 8);
			g_assert (sparc_is_imm13 (MONO_SPARC_STACK_BIAS + offset2));
			sparc_add_imm (code, FALSE, sparc_sp, MONO_SPARC_STACK_BIAS + offset2, ins->dreg);
			if ((ins->flags & MONO_INST_INIT) && (offset > 0)) {
				guint32 *br [2];
				int i;

				if (offset <= 16) {
					i = 0;
					while (i < offset) {
						if (mono_hwcap_sparc_is_v9) {
							sparc_stx_imm (code, sparc_g0, ins->dreg, i);
							i += 8;
						}
						else {
							sparc_st_imm (code, sparc_g0, ins->dreg, i);
							i += 4;
						}
					}
				}
				else {
					sparc_set (code, offset, sparc_o7);
					sparc_sub_imm (code, 0, sparc_o7, mono_hwcap_sparc_is_v9 ? 8 : 4, sparc_o7);
					/* beginning of loop */
					br [0] = code;
					if (mono_hwcap_sparc_is_v9)
						sparc_stx (code, sparc_g0, ins->dreg, sparc_o7);
					else
						sparc_st (code, sparc_g0, ins->dreg, sparc_o7);
					sparc_cmp_imm (code, sparc_o7, 0);
					br [1] = code;
					sparc_branch (code, 0, sparc_bne, 0);
					/* delay slot */
					sparc_sub_imm (code, 0, sparc_o7, mono_hwcap_sparc_is_v9 ? 8 : 4, sparc_o7);
					sparc_patch (br [1], br [0]);
				}
			}
			break;
		}
		case OP_THROW:
			sparc_mov_reg_reg (code, ins->sreg1, sparc_o0);
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL, 
					     (gpointer)"mono_arch_throw_exception");
			EMIT_CALL ();
			break;
		case OP_RETHROW:
			sparc_mov_reg_reg (code, ins->sreg1, sparc_o0);
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL, 
					     (gpointer)"mono_arch_rethrow_exception");
			EMIT_CALL ();
			break;
		case OP_START_HANDLER: {
			/*
			 * The START_HANDLER instruction marks the beginning of a handler 
			 * block. It is called using a call instruction, so %o7 contains 
			 * the return address. Since the handler executes in the same stack
             * frame as the method itself, we can't use save/restore to save 
			 * the return address. Instead, we save it into a dedicated 
			 * variable.
			 */
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (!sparc_is_imm13 (spvar->inst_offset)) {
				sparc_set (code, spvar->inst_offset, GP_SCRATCH_REG);
				sparc_sti (code, sparc_o7, spvar->inst_basereg, GP_SCRATCH_REG);
			}
			else
				sparc_sti_imm (code, sparc_o7, spvar->inst_basereg, spvar->inst_offset);
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (!sparc_is_imm13 (spvar->inst_offset)) {
				sparc_set (code, spvar->inst_offset, GP_SCRATCH_REG);
				sparc_ldi (code, spvar->inst_basereg, GP_SCRATCH_REG, sparc_o7);
			}
			else
				sparc_ldi_imm (code, spvar->inst_basereg, spvar->inst_offset, sparc_o7);
			sparc_jmpl_imm (code, sparc_o7, 8, sparc_g0);
			/* Delay slot */
			sparc_mov_reg_reg (code, ins->sreg1, sparc_o0);
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (!sparc_is_imm13 (spvar->inst_offset)) {
				sparc_set (code, spvar->inst_offset, GP_SCRATCH_REG);
				sparc_ldi (code, spvar->inst_basereg, GP_SCRATCH_REG, sparc_o7);
			}
			else
				sparc_ldi_imm (code, spvar->inst_basereg, spvar->inst_offset, sparc_o7);
			sparc_jmpl_imm (code, sparc_o7, 8, sparc_g0);
			sparc_nop (code);
			break;
		}
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			/* This is a jump inside the method, so call_simple works even on V9 */
			sparc_call_simple (code, 0);
			sparc_nop (code);
			for (GList *tmp = ins->inst_eh_blocks; tmp != bb->clause_holes; tmp = tmp->prev)
				mono_cfg_add_try_hole (cfg, ((MonoLeaveClause *) tmp->data)->clause, code, bb);
			break;
		case OP_LABEL:
			ins->inst_c0 = (guint8*)code - cfg->native_code;
			break;
		case OP_RELAXED_NOP:
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_ICONST:
		case OP_DUMMY_I8CONST:
		case OP_DUMMY_R8CONST:
		case OP_DUMMY_R4CONST:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_BR:
			//g_print ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
			if ((ins->inst_target_bb == bb->next_bb) && ins == bb->last_ins)
				break;
			if (ins->inst_target_bb->native_offset) {
				gint32 disp = (ins->inst_target_bb->native_offset - ((guint8*)code - cfg->native_code)) >> 2;
				g_assert (sparc_is_imm22 (disp));
				sparc_branch (code, 1, sparc_ba, disp);
			} else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
				sparc_branch (code, 1, sparc_ba, 0);
			}
			sparc_nop (code);
			break;
		case OP_BR_REG:
			sparc_jmp (code, ins->sreg1, sparc_g0);
			sparc_nop (code);
			break;
		case OP_CEQ:
		case OP_CLT:
		case OP_CLT_UN:
		case OP_CGT:
		case OP_CGT_UN:
			if (v64 && (cfg->opt & MONO_OPT_CMOV)) {
				sparc_clr_reg (code, ins->dreg);
				sparc_movcc_imm (code, sparc_xcc, opcode_to_sparc_cond (ins->opcode), 1, ins->dreg);
			}
			else {
				sparc_clr_reg (code, ins->dreg);
#ifdef SPARCV9
				sparc_branchp (code, 1, opcode_to_sparc_cond (ins->opcode), DEFAULT_ICC, 0, 2);
#else
				sparc_branch (code, 1, opcode_to_sparc_cond (ins->opcode), 2);
#endif
				/* delay slot */
				sparc_set (code, 1, ins->dreg);
			}
			break;
		case OP_ICEQ:
		case OP_ICLT:
		case OP_ICLT_UN:
		case OP_ICGT:
		case OP_ICGT_UN:
		    if (v64 && (cfg->opt & MONO_OPT_CMOV)) {
				sparc_clr_reg (code, ins->dreg);
				sparc_movcc_imm (code, sparc_icc, opcode_to_sparc_cond (ins->opcode), 1, ins->dreg);
		    }
		    else {
			sparc_clr_reg (code, ins->dreg);
			sparc_branchp (code, 1, opcode_to_sparc_cond (ins->opcode), sparc_icc_short, 0, 2);
			/* delay slot */
			sparc_set (code, 1, ins->dreg);
		    }
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
		case OP_COND_EXC_OV:
		case OP_COND_EXC_NO:
		case OP_COND_EXC_C:
		case OP_COND_EXC_NC:
		case OP_COND_EXC_IEQ:
		case OP_COND_EXC_INE_UN:
		case OP_COND_EXC_ILT:
		case OP_COND_EXC_ILT_UN:
		case OP_COND_EXC_IGT:
		case OP_COND_EXC_IGT_UN:
		case OP_COND_EXC_IGE:
		case OP_COND_EXC_IGE_UN:
		case OP_COND_EXC_ILE:
		case OP_COND_EXC_ILE_UN:
		case OP_COND_EXC_IOV:
		case OP_COND_EXC_INO:
		case OP_COND_EXC_IC:
		case OP_COND_EXC_INC:
#ifdef SPARCV9
			NOT_IMPLEMENTED;
#else
			EMIT_COND_SYSTEM_EXCEPTION (ins, opcode_to_sparc_cond (ins->opcode), ins->inst_p1);
#endif
			break;
		case OP_SPARC_COND_EXC_EQZ:
			EMIT_COND_SYSTEM_EXCEPTION_BPR (ins, brz, ins->inst_p1);
			break;
		case OP_SPARC_COND_EXC_GEZ:
			EMIT_COND_SYSTEM_EXCEPTION_BPR (ins, brgez, ins->inst_p1);
			break;
		case OP_SPARC_COND_EXC_GTZ:
			EMIT_COND_SYSTEM_EXCEPTION_BPR (ins, brgz, ins->inst_p1);
			break;
		case OP_SPARC_COND_EXC_LEZ:
			EMIT_COND_SYSTEM_EXCEPTION_BPR (ins, brlez, ins->inst_p1);
			break;
		case OP_SPARC_COND_EXC_LTZ:
			EMIT_COND_SYSTEM_EXCEPTION_BPR (ins, brlz, ins->inst_p1);
			break;
		case OP_SPARC_COND_EXC_NEZ:
			EMIT_COND_SYSTEM_EXCEPTION_BPR (ins, brnz, ins->inst_p1);
			break;

		case OP_IBEQ:
		case OP_IBNE_UN:
		case OP_IBLT:
		case OP_IBLT_UN:
		case OP_IBGT:
		case OP_IBGT_UN:
		case OP_IBGE:
		case OP_IBGE_UN:
		case OP_IBLE:
		case OP_IBLE_UN: {
			if (mono_hwcap_sparc_is_v9)
				EMIT_COND_BRANCH_PREDICTED (ins, opcode_to_sparc_cond (ins->opcode), 1, 1);
			else
				EMIT_COND_BRANCH (ins, opcode_to_sparc_cond (ins->opcode), 1, 1);
			break;
		}

		case OP_SPARC_BRZ:
			EMIT_COND_BRANCH_BPR (ins, brz, 1, 1, 1);
			break;
		case OP_SPARC_BRLEZ:
			EMIT_COND_BRANCH_BPR (ins, brlez, 1, 1, 1);
			break;
		case OP_SPARC_BRLZ:
			EMIT_COND_BRANCH_BPR (ins, brlz, 1, 1, 1);
			break;
		case OP_SPARC_BRNZ:
			EMIT_COND_BRANCH_BPR (ins, brnz, 1, 1, 1);
			break;
		case OP_SPARC_BRGZ:
			EMIT_COND_BRANCH_BPR (ins, brgz, 1, 1, 1);
			break;
		case OP_SPARC_BRGEZ:
			EMIT_COND_BRANCH_BPR (ins, brgez, 1, 1, 1);
			break;

		/* floating point opcodes */
		case OP_R8CONST:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, ins->inst_p0);
#ifdef SPARCV9
			sparc_set_template (code, sparc_o7);
#else
			sparc_sethi (code, 0, sparc_o7);
#endif
			sparc_lddf_imm (code, sparc_o7, 0, ins->dreg);
			break;
		case OP_R4CONST:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R4, ins->inst_p0);
#ifdef SPARCV9
			sparc_set_template (code, sparc_o7);
#else
			sparc_sethi (code, 0, sparc_o7);
#endif
			sparc_ldf_imm (code, sparc_o7, 0, FP_SCRATCH_REG);

			/* Extend to double */
			sparc_fstod (code, FP_SCRATCH_REG, ins->dreg);
			break;
		case OP_STORER8_MEMBASE_REG:
			if (!sparc_is_imm13 (ins->inst_offset + 4)) {
				sparc_set (code, ins->inst_offset, sparc_o7);
				/* SPARCV9 handles misaligned fp loads/stores */
				if (!v64 && (ins->inst_offset % 8)) {
					/* Misaligned */
					sparc_add (code, FALSE, ins->inst_destbasereg, sparc_o7, sparc_o7);
					sparc_stf (code, ins->sreg1, sparc_o7, sparc_g0);
					sparc_stf_imm (code, ins->sreg1 + 1, sparc_o7, 4);
				} else
					sparc_stdf (code, ins->sreg1, ins->inst_destbasereg, sparc_o7);
			}
			else {
				if (!v64 && (ins->inst_offset % 8)) {
					/* Misaligned */
					sparc_stf_imm (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
					sparc_stf_imm (code, ins->sreg1 + 1, ins->inst_destbasereg, ins->inst_offset + 4);
				} else
					sparc_stdf_imm (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			}
			break;
		case OP_LOADR8_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, lddf);
			break;
		case OP_STORER4_MEMBASE_REG:
			/* This requires a double->single conversion */
			sparc_fdtos (code, ins->sreg1, FP_SCRATCH_REG);
			if (!sparc_is_imm13 (ins->inst_offset)) {
				sparc_set (code, ins->inst_offset, sparc_o7);
				sparc_stf (code, FP_SCRATCH_REG, ins->inst_destbasereg, sparc_o7);
			}
			else
				sparc_stf_imm (code, FP_SCRATCH_REG, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_LOADR4_MEMBASE: {
			/* ldf needs a single precision register */
			int dreg = ins->dreg;
			ins->dreg = FP_SCRATCH_REG;
			EMIT_LOAD_MEMBASE (ins, ldf);
			ins->dreg = dreg;
			/* Extend to double */
			sparc_fstod (code, FP_SCRATCH_REG, ins->dreg);
			break;
		}
		case OP_ICONV_TO_R4: {
			MonoInst *spill = cfg->arch.float_spill_slot;
			gint32 reg = spill->inst_basereg;
			gint32 offset = spill->inst_offset;

			g_assert (spill->opcode == OP_REGOFFSET);
#ifdef SPARCV9
			if (!sparc_is_imm13 (offset)) {
				sparc_set (code, offset, sparc_o7);
				sparc_stx (code, ins->sreg1, reg, offset);
				sparc_lddf (code, reg, offset, FP_SCRATCH_REG);
			} else {
				sparc_stx_imm (code, ins->sreg1, reg, offset);
				sparc_lddf_imm (code, reg, offset, FP_SCRATCH_REG);
			}
			sparc_fxtos (code, FP_SCRATCH_REG, FP_SCRATCH_REG);
#else
			if (!sparc_is_imm13 (offset)) {
				sparc_set (code, offset, sparc_o7);
				sparc_st (code, ins->sreg1, reg, sparc_o7);
				sparc_ldf (code, reg, sparc_o7, FP_SCRATCH_REG);
			} else {
				sparc_st_imm (code, ins->sreg1, reg, offset);
				sparc_ldf_imm (code, reg, offset, FP_SCRATCH_REG);
			}
			sparc_fitos (code, FP_SCRATCH_REG, FP_SCRATCH_REG);
#endif
			sparc_fstod (code, FP_SCRATCH_REG, ins->dreg);
			break;
		}
		case OP_ICONV_TO_R8: {
			MonoInst *spill = cfg->arch.float_spill_slot;
			gint32 reg = spill->inst_basereg;
			gint32 offset = spill->inst_offset;

			g_assert (spill->opcode == OP_REGOFFSET);

#ifdef SPARCV9
			if (!sparc_is_imm13 (offset)) {
				sparc_set (code, offset, sparc_o7);
				sparc_stx (code, ins->sreg1, reg, sparc_o7);
				sparc_lddf (code, reg, sparc_o7, FP_SCRATCH_REG);
			} else {
				sparc_stx_imm (code, ins->sreg1, reg, offset);
				sparc_lddf_imm (code, reg, offset, FP_SCRATCH_REG);
			}
			sparc_fxtod (code, FP_SCRATCH_REG, ins->dreg);
#else
			if (!sparc_is_imm13 (offset)) {
				sparc_set (code, offset, sparc_o7);
				sparc_st (code, ins->sreg1, reg, sparc_o7);
				sparc_ldf (code, reg, sparc_o7, FP_SCRATCH_REG);
			} else {
				sparc_st_imm (code, ins->sreg1, reg, offset);
				sparc_ldf_imm (code, reg, offset, FP_SCRATCH_REG);
			}
			sparc_fitod (code, FP_SCRATCH_REG, ins->dreg);
#endif
			break;
		}
		case OP_FCONV_TO_I1:
		case OP_FCONV_TO_U1:
		case OP_FCONV_TO_I2:
		case OP_FCONV_TO_U2:
#ifndef SPARCV9
		case OP_FCONV_TO_I:
		case OP_FCONV_TO_U:
#endif
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_U4: {
			MonoInst *spill = cfg->arch.float_spill_slot;
			gint32 reg = spill->inst_basereg;
			gint32 offset = spill->inst_offset;

			g_assert (spill->opcode == OP_REGOFFSET);

			sparc_fdtoi (code, ins->sreg1, FP_SCRATCH_REG);
			if (!sparc_is_imm13 (offset)) {
				sparc_set (code, offset, sparc_o7);
				sparc_stdf (code, FP_SCRATCH_REG, reg, sparc_o7);
				sparc_ld (code, reg, sparc_o7, ins->dreg);
			} else {
				sparc_stdf_imm (code, FP_SCRATCH_REG, reg, offset);
				sparc_ld_imm (code, reg, offset, ins->dreg);
			}

			switch (ins->opcode) {
			case OP_FCONV_TO_I1:
			case OP_FCONV_TO_U1:
				sparc_and_imm (code, 0, ins->dreg, 0xff, ins->dreg);
				break;
			case OP_FCONV_TO_I2:
			case OP_FCONV_TO_U2:
				sparc_set (code, 0xffff, sparc_o7);
				sparc_and (code, 0, ins->dreg, sparc_o7, ins->dreg);
				break;
			default:
				break;
			}
			break;
		}
		case OP_FCONV_TO_I8:
		case OP_FCONV_TO_U8:
			/* Emulated */
			g_assert_not_reached ();
			break;
		case OP_FCONV_TO_R4:
			/* FIXME: Change precision ? */
#ifdef SPARCV9
			sparc_fmovd (code, ins->sreg1, ins->dreg);
#else
			sparc_fmovs (code, ins->sreg1, ins->dreg);
			sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
#endif
			break;
		case OP_LCONV_TO_R_UN: { 
			/* Emulated */
			g_assert_not_reached ();
			break;
		}
		case OP_LCONV_TO_OVF_I:
		case OP_LCONV_TO_OVF_I4_2: {
			guint32 *br [3], *label [1];

			/* 
			 * Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000
			 */
			sparc_cmp_imm (code, ins->sreg1, 0);
			br [0] = code; 
			sparc_branch (code, 1, sparc_bneg, 0);
			sparc_nop (code);

			/* positive */
			/* ms word must be 0 */
			sparc_cmp_imm (code, ins->sreg2, 0);
			br [1] = code;
			sparc_branch (code, 1, sparc_be, 0);
			sparc_nop (code);

			label [0] = code;

			EMIT_COND_SYSTEM_EXCEPTION (ins, sparc_ba, "OverflowException");

			/* negative */
			sparc_patch (br [0], code);

			/* ms word must 0xfffffff */
			sparc_cmp_imm (code, ins->sreg2, -1);
			br [2] = code;
			sparc_branch (code, 1, sparc_bne, 0);
			sparc_nop (code);
			sparc_patch (br [2], label [0]);

			/* Ok */
			sparc_patch (br [1], code);
			if (ins->sreg1 != ins->dreg)
				sparc_mov_reg_reg (code, ins->sreg1, ins->dreg);
			break;
		}
		case OP_FADD:
			sparc_faddd (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_FSUB:
			sparc_fsubd (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;		
		case OP_FMUL:
			sparc_fmuld (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;		
		case OP_FDIV:
			sparc_fdivd (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;		
		case OP_FNEG:
#ifdef SPARCV9
			sparc_fnegd (code, ins->sreg1, ins->dreg);
#else
			/* FIXME: why don't use fnegd ? */
			sparc_fnegs (code, ins->sreg1, ins->dreg);
#endif
			break;		
		case OP_FREM:
			sparc_fdivd (code, ins->sreg1, ins->sreg2, FP_SCRATCH_REG);
			sparc_fmuld (code, ins->sreg2, FP_SCRATCH_REG, FP_SCRATCH_REG);
			sparc_fsubd (code, ins->sreg1, FP_SCRATCH_REG, ins->dreg);
			break;
		case OP_FCOMPARE:
			sparc_fcmpd (code, ins->sreg1, ins->sreg2);
			break;
		case OP_FCEQ:
		case OP_FCLT:
		case OP_FCLT_UN:
		case OP_FCGT:
		case OP_FCGT_UN:
			sparc_fcmpd (code, ins->sreg1, ins->sreg2);
			sparc_clr_reg (code, ins->dreg);
			switch (ins->opcode) {
			case OP_FCLT_UN:
			case OP_FCGT_UN:
				sparc_fbranch (code, 1, opcode_to_sparc_cond (ins->opcode), 4);
				/* delay slot */
				sparc_set (code, 1, ins->dreg);
				sparc_fbranch (code, 1, sparc_fbu, 2);
				/* delay slot */
				sparc_set (code, 1, ins->dreg);
				break;
			default:
				sparc_fbranch (code, 1, opcode_to_sparc_cond (ins->opcode), 2);
				/* delay slot */
				sparc_set (code, 1, ins->dreg);				
			}
			break;
		case OP_FBEQ:
		case OP_FBLT:
		case OP_FBGT:
			EMIT_FLOAT_COND_BRANCH (ins, opcode_to_sparc_cond (ins->opcode), 1, 1);
			break;
		case OP_FBGE: {
			/* clt.un + brfalse */
			guint32 *p = code;
			sparc_fbranch (code, 1, sparc_fbul, 0);
			/* delay slot */
			sparc_nop (code);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fba, 1, 1);
			sparc_patch (p, (guint8*)code);
			break;
		}
		case OP_FBLE: {
			/* cgt.un + brfalse */
			guint32 *p = code;
			sparc_fbranch (code, 1, sparc_fbug, 0);
			/* delay slot */
			sparc_nop (code);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fba, 1, 1);
			sparc_patch (p, (guint8*)code);
			break;
		}
		case OP_FBNE_UN:
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbne, 1, 1);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbu, 1, 1);
			break;
		case OP_FBLT_UN:
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbl, 1, 1);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbu, 1, 1);
			break;
		case OP_FBGT_UN:
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbg, 1, 1);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbu, 1, 1);
			break;
		case OP_FBGE_UN:
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbge, 1, 1);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbu, 1, 1);
			break;
		case OP_FBLE_UN:
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fble, 1, 1);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fbu, 1, 1);
			break;
		case OP_CKFINITE: {
			MonoInst *spill = cfg->arch.float_spill_slot;
			gint32 reg = spill->inst_basereg;
			gint32 offset = spill->inst_offset;

			g_assert (spill->opcode == OP_REGOFFSET);

			if (!sparc_is_imm13 (offset)) {
				sparc_set (code, offset, sparc_o7);
				sparc_stdf (code, ins->sreg1, reg, sparc_o7);
				sparc_lduh (code, reg, sparc_o7, sparc_o7);
			} else {
				sparc_stdf_imm (code, ins->sreg1, reg, offset);
				sparc_lduh_imm (code, reg, offset, sparc_o7);
			}
			sparc_srl_imm (code, sparc_o7, 4, sparc_o7);
			sparc_and_imm (code, FALSE, sparc_o7, 2047, sparc_o7);
			sparc_cmp_imm (code, sparc_o7, 2047);
			EMIT_COND_SYSTEM_EXCEPTION (ins, sparc_be, "OverflowException");
#ifdef SPARCV9
			sparc_fmovd (code, ins->sreg1, ins->dreg);
#else
			sparc_fmovs (code, ins->sreg1, ins->dreg);
			sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
#endif
			break;
		}

		case OP_MEMORY_BARRIER:
			sparc_membar (code, sparc_membar_all);
			break;
		case OP_LIVERANGE_START: {
			if (cfg->verbose_level > 1)
				printf ("R%d START=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_start = code - cfg->native_code;
			break;
		}
		case OP_LIVERANGE_END: {
			if (cfg->verbose_level > 1)
				printf ("R%d END=0x%x\n", MONO_VARINFO (cfg, ins->inst_c0)->vreg, (int)(code - cfg->native_code));
			MONO_VARINFO (cfg, ins->inst_c0)->live_range_end = code - cfg->native_code;
			break;
		}
		case OP_GC_SAFE_POINT:
			break;

		default:
#ifdef __GNUC__
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
#else
			g_warning ("%s:%d: unknown opcode %s\n", __FILE__, __LINE__, mono_inst_name (ins->opcode));
#endif
			g_assert_not_reached ();
		}

		if ((((guint8*)code) - code_start) > max_len) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %d)",
				   mono_inst_name (ins->opcode), max_len, ((guint8*)code) - code_start);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_ins = ins;
	}
	set_code_cursor (cfg, code);
}

void
mono_arch_register_lowlevel_calls (void)
{
	mono_register_jit_icall (mono_arch_get_lmf_addr, NULL, TRUE);
}

void
mono_arch_patch_code (MonoCompile *cfg, MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors, MonoError *error)
{
	MonoJumpInfo *patch_info;

	error_init (error);

	/* FIXME: Move part of this to arch independent code */
	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		gpointer target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors, error);
		return_if_nok (error);

		switch (patch_info->type) {
		case MONO_PATCH_INFO_NONE:
			continue;
		case MONO_PATCH_INFO_METHOD_JUMP: {
			guint32 *ip2 = (guint32*)ip;
			/* Might already been patched */
			sparc_set_template (ip2, sparc_o7);
			break;
		}
		default:
			break;
		}
		sparc_patch ((guint32*)ip, target);
	}
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	int i;
	guint32 *code = (guint32*)p;
	MonoMethodSignature *sig = mono_method_signature_internal (cfg->method);
	CallInfo *cinfo;

	/* Save registers to stack */
	for (i = 0; i < 6; ++i)
		sparc_sti_imm (code, sparc_i0 + i, sparc_fp, ARGS_OFFSET + (i * sizeof (target_mgreg_t)));

	cinfo = get_call_info (cfg, sig, FALSE);

	/* Save float regs on V9, since they are caller saved */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;

		stack_offset = ainfo->offset + ARGS_OFFSET;

		if (ainfo->storage == ArgInFloatReg) {
			if (!sparc_is_imm13 (stack_offset))
				NOT_IMPLEMENTED;
			sparc_stf_imm (code, ainfo->reg, sparc_fp, stack_offset);
		}
		else if (ainfo->storage == ArgInDoubleReg) {
			/* The offset is guaranteed to be aligned by the ABI rules */
			sparc_stdf_imm (code, ainfo->reg, sparc_fp, stack_offset);
		}
	}

	sparc_set (code, cfg->method, sparc_o0);
	sparc_add_imm (code, FALSE, sparc_fp, MONO_SPARC_STACK_BIAS, sparc_o1);

	mono_add_patch_info (cfg, (guint8*)code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	EMIT_CALL ();

	/* Restore float regs on V9 */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;

		stack_offset = ainfo->offset + ARGS_OFFSET;

		if (ainfo->storage == ArgInFloatReg) {
			if (!sparc_is_imm13 (stack_offset))
				NOT_IMPLEMENTED;
			sparc_ldf_imm (code, sparc_fp, stack_offset, ainfo->reg);
		}
		else if (ainfo->storage == ArgInDoubleReg) {
			/* The offset is guaranteed to be aligned by the ABI rules */
			sparc_lddf_imm (code, sparc_fp, stack_offset, ainfo->reg);
		}
	}

	g_free (cinfo);

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
	guint32 *code = (guint32*)p;
	int save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;

	switch (mini_get_underlying_type (mono_method_signature_internal (method)->ret)->type) {
	case MONO_TYPE_VOID:
		/* special case string .ctor icall */
		if (strcmp (".ctor", method->name) && method->klass == mono_defaults.string_class)
			save_mode = SAVE_ONE;
		else
			save_mode = SAVE_NONE;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
#ifdef SPARCV9
		save_mode = SAVE_ONE;
#else
		save_mode = SAVE_TWO;
#endif
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

	/* Save the result to the stack and also put it into the output registers */

	switch (save_mode) {
	case SAVE_TWO:
		/* V8 only */
		sparc_st_imm (code, sparc_i0, sparc_fp, 68);
		sparc_st_imm (code, sparc_i0, sparc_fp, 72);
		sparc_mov_reg_reg (code, sparc_i0, sparc_o1);
		sparc_mov_reg_reg (code, sparc_i1, sparc_o2);
		break;
	case SAVE_ONE:
		sparc_sti_imm (code, sparc_i0, sparc_fp, ARGS_OFFSET);
		sparc_mov_reg_reg (code, sparc_i0, sparc_o1);
		break;
	case SAVE_FP:
#ifdef SPARCV9
		sparc_stdf_imm (code, sparc_f0, sparc_fp, ARGS_OFFSET);
#else
		sparc_stdf_imm (code, sparc_f0, sparc_fp, 72);
		sparc_ld_imm (code, sparc_fp, 72, sparc_o1);
		sparc_ld_imm (code, sparc_fp, 72 + 4, sparc_o2);
#endif
		break;
	case SAVE_STRUCT:
#ifdef SPARCV9
		sparc_mov_reg_reg (code, sparc_i0, sparc_o1);
#else
		sparc_ld_imm (code, sparc_fp, 64, sparc_o1);
#endif
		break;
	case SAVE_NONE:
	default:
		break;
	}

	sparc_set (code, cfg->method, sparc_o0);

	mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_ABS, func);
	EMIT_CALL ();

	/* Restore result */

	switch (save_mode) {
	case SAVE_TWO:
		sparc_ld_imm (code, sparc_fp, 68, sparc_i0);
		sparc_ld_imm (code, sparc_fp, 72, sparc_i0);
		break;
	case SAVE_ONE:
		sparc_ldi_imm (code, sparc_fp, ARGS_OFFSET, sparc_i0);
		break;
	case SAVE_FP:
		sparc_lddf_imm (code, sparc_fp, ARGS_OFFSET, sparc_f0);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *inst;
	guint32 *code;
	CallInfo *cinfo;
	guint32 i, offset;

	cfg->code_size = 256;
	cfg->native_code = g_malloc (cfg->code_size);
	code = (guint32*)cfg->native_code;

	/* FIXME: Generate intermediate code instead */

	offset = cfg->stack_offset;
	offset += (16 * sizeof (target_mgreg_t)); /* register save area */
#ifndef SPARCV9
	offset += 4; /* struct/union return pointer */
#endif

	/* add parameter area size for called functions */
	if (cfg->param_area < (6 * sizeof (target_mgreg_t)))
		/* Reserve space for the first 6 arguments even if it is unused */
		offset += 6 * sizeof (target_mgreg_t);
	else
		offset += cfg->param_area;
	
	/* align the stack size */
	offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * localloc'd memory is stored between the local variables (whose
	 * size is given by cfg->stack_offset), and between the space reserved
	 * by the ABI.
	 */
	cfg->arch.localloc_offset = offset - cfg->stack_offset;

	cfg->stack_offset = offset;

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
			/* Perform stack touching */
			NOT_IMPLEMENTED;
#endif

	if (!sparc_is_imm13 (- cfg->stack_offset)) {
		/* Can't use sparc_o7 here, since we're still in the caller's frame */
		sparc_set (code, (- cfg->stack_offset), GP_SCRATCH_REG);
		sparc_save (code, sparc_sp, GP_SCRATCH_REG, sparc_sp);
	}
	else
		sparc_save_imm (code, sparc_sp, - cfg->stack_offset, sparc_sp);

/*
	if (strstr (cfg->method->name, "foo")) {
		mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_ABS, mono_sparc_break);
		sparc_call_simple (code, 0);
		sparc_nop (code);
	}
*/

	sig = mono_method_signature_internal (method);

	cinfo = get_call_info (cfg, sig, FALSE);

	/* Keep in sync with emit_load_volatile_arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;
		MonoType *arg_type;
		inst = cfg->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = mono_get_object_type ();
		else
			arg_type = sig->params [i - sig->hasthis];

		stack_offset = ainfo->offset + ARGS_OFFSET;

		/* Save the split arguments so they will reside entirely on the stack */
		if (ainfo->storage == ArgInSplitRegStack) {
			/* Save the register to the stack */
			g_assert (inst->opcode == OP_REGOFFSET);
			if (!sparc_is_imm13 (stack_offset))
				NOT_IMPLEMENTED;
			sparc_st_imm (code, sparc_i5, inst->inst_basereg, stack_offset);
		}

		if (!v64 && !arg_type->byref && (arg_type->type == MONO_TYPE_R8)) {
			/* Save the argument to a dword aligned stack location */
			/*
			 * stack_offset contains the offset of the argument on the stack.
			 * inst->inst_offset contains the dword aligned offset where the value 
			 * should be stored.
			 */
			if (ainfo->storage == ArgInIRegPair) {
				if (!sparc_is_imm13 (inst->inst_offset + 4))
					NOT_IMPLEMENTED;
				sparc_st_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, inst->inst_offset);
				sparc_st_imm (code, sparc_i0 + ainfo->reg + 1, inst->inst_basereg, inst->inst_offset + 4);
			}
			else
				if (ainfo->storage == ArgInSplitRegStack) {
#ifdef SPARCV9
					g_assert_not_reached ();
#endif
					if (stack_offset != inst->inst_offset) {
						/* stack_offset is not dword aligned, so we need to make a copy */
						sparc_st_imm (code, sparc_i5, inst->inst_basereg, inst->inst_offset);
						sparc_ld_imm (code, sparc_fp, stack_offset + 4, sparc_o7);
						sparc_st_imm (code, sparc_o7, inst->inst_basereg, inst->inst_offset + 4);
					}
				}
			else
				if (ainfo->storage == ArgOnStackPair) {
#ifdef SPARCV9
					g_assert_not_reached ();
#endif
					if (stack_offset != inst->inst_offset) {
						/* stack_offset is not dword aligned, so we need to make a copy */
						sparc_ld_imm (code, sparc_fp, stack_offset, sparc_o7);
						sparc_st_imm (code, sparc_o7, inst->inst_basereg, inst->inst_offset);
						sparc_ld_imm (code, sparc_fp, stack_offset + 4, sparc_o7);
						sparc_st_imm (code, sparc_o7, inst->inst_basereg, inst->inst_offset + 4);
					}
				}
			else
				g_assert_not_reached ();
		}
		else
			if ((ainfo->storage == ArgInIReg) && (inst->opcode != OP_REGVAR)) {
				/* Argument in register, but need to be saved to stack */
				if (!sparc_is_imm13 (stack_offset))
					NOT_IMPLEMENTED;
				if ((stack_offset - ARGS_OFFSET) & 0x1)
					sparc_stb_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
				else
					if ((stack_offset - ARGS_OFFSET) & 0x2)
						sparc_sth_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
				else
					if ((stack_offset - ARGS_OFFSET) & 0x4)
						sparc_st_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);				
					else {
						if (v64)
							sparc_stx_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
						else
							sparc_st_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
					}
			}
		else
			if ((ainfo->storage == ArgInIRegPair) && (inst->opcode != OP_REGVAR)) {
#ifdef SPARCV9
				NOT_IMPLEMENTED;
#endif
				/* Argument in regpair, but need to be saved to stack */
				if (!sparc_is_imm13 (inst->inst_offset + 4))
					NOT_IMPLEMENTED;
				sparc_st_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, inst->inst_offset);
				sparc_st_imm (code, sparc_i0 + ainfo->reg + 1, inst->inst_basereg, inst->inst_offset + 4);				
			}
		else if ((ainfo->storage == ArgInFloatReg) && (inst->opcode != OP_REGVAR)) {
				if (!sparc_is_imm13 (stack_offset))
					NOT_IMPLEMENTED;
				sparc_stf_imm (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
				}
			else if ((ainfo->storage == ArgInDoubleReg) && (inst->opcode != OP_REGVAR)) {
				/* The offset is guaranteed to be aligned by the ABI rules */
				sparc_stdf_imm (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
			}
					
		if ((ainfo->storage == ArgInFloatReg) && (inst->opcode == OP_REGVAR)) {
			/* Need to move into the a double precision register */
			sparc_fstod (code, ainfo->reg, ainfo->reg - 1);
		}

		if ((ainfo->storage == ArgInSplitRegStack) || (ainfo->storage == ArgOnStack))
			if (inst->opcode == OP_REGVAR)
				/* FIXME: Load the argument into memory */
				NOT_IMPLEMENTED;
	}

	g_free (cinfo);

	if (cfg->method->save_lmf) {
		gint32 lmf_offset = STACK_BIAS - cfg->arch.lmf_offset;

		/* Save ip */
		mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		sparc_set_template (code, sparc_o7);
		sparc_sti_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ip));
		/* Save sp */
		sparc_sti_imm (code, sparc_sp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, sp));
		/* Save fp */
		sparc_sti_imm (code, sparc_fp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp));
		/* Save method */
		/* FIXME: add a relocation for this */
		sparc_set (code, cfg->method, sparc_o7);
		sparc_sti_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method));

		mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_JIT_ICALL, 
							 (gpointer)"mono_arch_get_lmf_addr");		
		EMIT_CALL ();

		code = (guint32*)mono_sparc_emit_save_lmf (code, lmf_offset);
	}

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = (guint32*)mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);

	set_code_cursor (cfg, code);

	return (guint8*)code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	guint32 *code;
	int can_fold = 0;
	int max_epilog_size = 16 + 20 * 4;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	code = (guint32 *)realloc_code (cfg, max_epilog_size);

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = (guint32*)mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);

	if (cfg->method->save_lmf) {
		gint32 lmf_offset = STACK_BIAS - cfg->arch.lmf_offset;

		code = mono_sparc_emit_restore_lmf (code, lmf_offset);
	}

	/* 
	 * The V8 ABI requires that calls to functions which return a structure
	 * return to %i7+12
	 */
	if (!v64 && mono_method_signature_internal (cfg->method)->pinvoke && MONO_TYPE_ISSTRUCT(mono_method_signature_internal (cfg->method)->ret))
		sparc_jmpl_imm (code, sparc_i7, 12, sparc_g0);
	else
		sparc_ret (code);

	/* Only fold last instruction into the restore if the exit block has an in count of 1
	   and the previous block hasn't been optimized away since it may have an in count > 1 */
	if (cfg->bb_exit->in_count == 1 && cfg->bb_exit->in_bb[0]->native_offset != cfg->bb_exit->native_offset)
		can_fold = 1;

	/* 
	 * FIXME: The last instruction might have a branch pointing into it like in 
	 * int_ceq sparc_i0 <-
	 */
	can_fold = 0;

	/* Try folding last instruction into the restore */
	if (can_fold && (sparc_inst_op (code [-2]) == 0x2) && (sparc_inst_op3 (code [-2]) == 0x2) && sparc_inst_imm (code [-2]) && (sparc_inst_rd (code [-2]) == sparc_i0)) {
		/* or reg, imm, %i0 */
		int reg = sparc_inst_rs1 (code [-2]);
		int imm = (((gint32)(sparc_inst_imm13 (code [-2]))) << 19) >> 19;
		code [-2] = code [-1];
		code --;
		sparc_restore_imm (code, reg, imm, sparc_o0);
	}
	else
	if (can_fold && (sparc_inst_op (code [-2]) == 0x2) && (sparc_inst_op3 (code [-2]) == 0x2) && (!sparc_inst_imm (code [-2])) && (sparc_inst_rd (code [-2]) == sparc_i0)) {
		/* or reg, reg, %i0 */
		int reg1 = sparc_inst_rs1 (code [-2]);
		int reg2 = sparc_inst_rs2 (code [-2]);
		code [-2] = code [-1];
		code --;
		sparc_restore (code, reg1, reg2, sparc_o0);
	}
	else
		sparc_restore_imm (code, sparc_g0, 0, sparc_g0);

	set_code_cursor (cfg, code);
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	guint32 *code;
	int nthrows = 0, i;
	int exc_count = 0;
	guint32 code_size;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];

	/* Compute needed space */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			exc_count++;
	}
     
	/* 
	 * make sure we have enough space for exceptions
	 */
#ifdef SPARCV9
	code_size = exc_count * (20 * 4);
#else
	code_size = exc_count * 24;
#endif
	code = (guint32*)realloc_code (cfg, code_size);

	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			guint32 *buf, *buf2;
			guint32 throw_ip, type_idx;
			gint32 disp;

			sparc_patch ((guint32*)(cfg->native_code + patch_info->ip.i), code);

			exc_class = mono_class_load_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			type_idx = m_class_get_type_token (exc_class) - MONO_TOKEN_TYPE_DEF;
			throw_ip = patch_info->ip.i;

			/* Find a throw sequence for the same exception class */
			for (i = 0; i < nthrows; ++i)
				if (exc_classes [i] == exc_class)
					break;

			if (i < nthrows) {
				guint32 throw_offset = (((guint8*)exc_throw_end [i] - cfg->native_code) - throw_ip) >> 2;
				if (!sparc_is_imm13 (throw_offset))
					sparc_set32 (code, throw_offset, sparc_o1);

				disp = (exc_throw_start [i] - (guint8*)code) >> 2;
				g_assert (sparc_is_imm22 (disp));
				sparc_branch (code, 0, sparc_ba, disp);
				if (sparc_is_imm13 (throw_offset))
					sparc_set32 (code, throw_offset, sparc_o1);
				else
					sparc_nop (code);
				patch_info->type = MONO_PATCH_INFO_NONE;
			}
			else {
				/* Emit the template for setting o1 */
				buf = code;
				if (sparc_is_imm13 (((((guint8*)code - cfg->native_code) - throw_ip) >> 2) - 8))
					/* Can use a short form */
					sparc_nop (code);
				else
					sparc_set_template (code, sparc_o1);
				buf2 = code;

				if (nthrows < 16) {
					exc_classes [nthrows] = exc_class;
					exc_throw_start [nthrows] = (guint8*)code;
				}

				/*
				mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_ABS, mono_sparc_break);
				EMIT_CALL();
				*/

				/* first arg = type token */
				/* Pass the type index to reduce the size of the sparc_set */
				if (!sparc_is_imm13 (type_idx))
					sparc_set32 (code, type_idx, sparc_o0);

				/* second arg = offset between the throw ip and the current ip */
				/* On sparc, the saved ip points to the call instruction */
				disp = (((guint8*)code - cfg->native_code) - throw_ip) >> 2;
				sparc_set32 (buf, disp, sparc_o1);
				while (buf < buf2)
					sparc_nop (buf);

				if (nthrows < 16) {
					exc_throw_end [nthrows] = (guint8*)code;
					nthrows ++;
				}

				patch_info->data.name = "mono_arch_throw_corlib_exception";
				patch_info->type = MONO_PATCH_INFO_JIT_ICALL;
				patch_info->ip.i = (guint8*)code - cfg->native_code;

				EMIT_CALL ();

				if (sparc_is_imm13 (type_idx)) {
					/* Put it into the delay slot */
					code --;
					buf = code;
					sparc_set32 (code, type_idx, sparc_o0);
					g_assert (code - buf == 1);
				}
			}
			break;
		}
		default:
			/* do nothing */
			break;
		}

		set_code_cursor (cfg, code);
	}

	set_code_cursor (cfg, code);
}

gboolean lmf_addr_key_inited = FALSE;

#ifdef MONO_SPARC_THR_TLS
thread_key_t lmf_addr_key;
#else
pthread_key_t lmf_addr_key;
#endif

gpointer
mono_arch_get_lmf_addr (void)
{
	/* This is perf critical so we bypass the IO layer */
	/* The thr_... functions seem to be somewhat faster */
#ifdef MONO_SPARC_THR_TLS
	gpointer res;
	thr_getspecific (lmf_addr_key, &res);
	return res;
#else
	return pthread_getspecific (lmf_addr_key);
#endif
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

/*
 * There seems to be no way to determine stack boundaries under solaris,
 * so it's not possible to determine whenever a SIGSEGV is caused by stack
 * overflow or not.
 */
#error "--with-sigaltstack=yes not supported on solaris"

#endif

void
mono_arch_tls_init (void)
{
	MonoJitTlsData *jit_tls;

	if (!lmf_addr_key_inited) {
		int res;

		lmf_addr_key_inited = TRUE;

#ifdef MONO_SPARC_THR_TLS
		res = thr_keycreate (&lmf_addr_key, NULL);
#else
		res = pthread_key_create (&lmf_addr_key, NULL);
#endif
		g_assert (res == 0);

	}

	jit_tls = mono_get_jit_tls ();

#ifdef MONO_SPARC_THR_TLS
	thr_setspecific (lmf_addr_key, &jit_tls->lmf);
#else
	pthread_setspecific (lmf_addr_key, &jit_tls->lmf);
#endif
}

void
mono_arch_finish_init (void)
{
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	return ins;
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
	int k, align;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	cinfo = get_call_info (NULL, csig, FALSE);

	if (csig->hasthis) {
		ainfo = &cinfo->args [0];
		arg_info [0].offset = ARGS_OFFSET - MONO_SPARC_STACK_BIAS + ainfo->offset;
	}

	for (k = 0; k < param_count; k++) {
		ainfo = &cinfo->args [k + csig->hasthis];

		arg_info [k + 1].offset = ARGS_OFFSET - MONO_SPARC_STACK_BIAS + ainfo->offset;
		arg_info [k + 1].size = mono_type_size (csig->params [k], &align);
	}

	g_free (cinfo);

	return 0;
}

host_mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	/* FIXME: implement */
	g_assert_not_reached ();
}

gboolean
mono_arch_opcode_supported (int opcode)
{
	return FALSE;
}

gboolean
mono_arch_tailcall_supported (MonoCompile *cfg, MonoMethodSignature *caller_sig, MonoMethodSignature *callee_sig, gboolean virtual_)
{
	return FALSE;
}
