/*
 * mini-sparc.c: Sparc backend for the Mono code generator
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
#include <sys/systeminfo.h>
#include <thread.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-math.h>

#include "mini-sparc.h"
#include "inssel.h"
#include "trace.h"
#include "cpu-sparc.h"

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
 * - %l0..%l7 is used for local register allocation
 * - %o0..%o6 is used for outgoing arguments
 * - %o7 and %g1 is used as scratch registers in opcodes
 * - all floating point registers are used for local register allocation except %f0. 
 *   Only double precision registers are used.
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
 * In addition to this, the runtime requires the truncl function, or its 
 * solaris counterpart, aintl, to do some double->int conversions. If this 
 * function is not available, it is emulated somewhat, but the results can be
 * strange.
 */

/*
 * Possible optimizations:
 * - delay slot scheduling
 * - allocate large constants to registers
 * - use %o registers for local allocation
 * - implement unwinding through native frames
 * - add more mul/div/rem optimizations
 */

#if SPARCV9
#error "Sparc V9 support not yet implemented."
#endif

#ifndef __linux__
#define MONO_SPARC_THR_TLS 1
#endif

#define NOT_IMPLEMENTED g_assert_not_reached ();

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))

#define SIGNAL_STACK_SIZE (64 * 1024)

/* Whenever the CPU supports v9 instructions */
gboolean sparcv9 = FALSE;

static gpointer mono_arch_get_lmf_addr (void);

static int
mono_spillvar_offset_float (MonoCompile *cfg, int spillvar);

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
	char buf [1024];
	guint32 opts = 0;

	*exclude_mask = 0;

#ifndef __linux__
	if (!sysinfo (SI_ISALIST, buf, 1024))
		g_assert_not_reached ();
#else
	/* From glibc.  If the getpagesize is 8192, we're on sparc64, which
	 * (in)directly implies that we're a v9 or better.
	 * Improvements to this are greatly accepted...
	 * Also, we don't differentiate between v7 and v8.  I sense SIGILL
	 * sniffing in my future.  
	 */
	if (getpagesize() == 8192)
		strcpy (buf, "sparcv9");
	else
		strcpy (buf, "sparcv8");
#endif

	/* 
	 * On some processors, the cmov instructions are even slower than the
	 * normal ones...
	 */
	if (strstr (buf, "sparcv9")) {
		opts |= MONO_OPT_CMOV | MONO_OPT_FCMOV;
		sparcv9 = TRUE;
	}
	else
		*exclude_mask |= MONO_OPT_CMOV | MONO_OPT_FCMOV;

	return opts;
}

static void
mono_sparc_break (void)
{
}

#ifdef __GNUC__
#define flushi(addr)    __asm__ __volatile__ ("iflush %0"::"r"(addr):"memory")
#else /* assume Sun's compiler */
static void flushi(void *addr)
{
    asm("flush %i0");
}
#endif

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#ifndef __linux__
	/* Hopefully this is optimized based on the actual CPU */
	sync_instruction_memory (code, size);
#else
	guint64 *p = (guint64*)code;
	guint64 *end = (guint64*)(code + ((size + 8) /8));

	/* 
	 * FIXME: Flushing code in dword chunks in _slow_.
	 */
	while (p < end)
#ifdef __GNUC__
		__asm__ __volatile__ ("iflush %0"::"r"(p++));
#else
			flushi (p ++);
#endif
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
mono_sparc_is_v9 (void) {
	return sparcv9;
}

typedef enum {
	ArgInIReg,
	ArgInIRegPair,
	ArgInSplitRegStack,
	ArgInFReg,
	ArgInFRegPair,
	ArgOnStack,
	ArgOnStackPair
} ArgStorage;

typedef struct {
	gint16 offset;
	/* This needs to be offset by %i0 or %o0 depending on caller/callee */
	gint8  reg;
	ArgStorage storage;
	guint32 vt_offset; /* for valuetypes */
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	guint32 reg_usage;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

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
		(*stack_size) += 4;
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
			ainfo->offset = *stack_size;
		}
		else {
			ainfo->storage = ArgInSplitRegStack;
			ainfo->reg = *gr;
			ainfo->offset = *stack_size;
			(*gr) ++;
		}

		(*stack_size) += 8;
	}
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * See the "System V ABI, Sparc Processor Supplement" Sparc V8 version document for
 * more information.
 */
static CallInfo*
get_call_info (MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i, gr, simpletype;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;

	cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	gr = 0;

	/* this */
	if (sig->hasthis)
		add_general (&gr, &stack_size, cinfo->args + 0, FALSE);

	for (i = 0; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			add_general (&gr, &stack_size, &cinfo->sig_cookie, FALSE);
			/* Prevent implicit arguments from being passed in registers */
			gr = PARAM_REGS;
		}

		DEBUG(printf("param %d: ", i));
		if (sig->params [i]->byref) {
			DEBUG(printf("byref\n"));
			
			add_general (&gr, &stack_size, ainfo, FALSE);
			continue;
		}
		simpletype = sig->params [i]->type;
	enum_calc_size:
		switch (simpletype) {
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_I1:
		case MONO_TYPE_U1:
			add_general (&gr, &stack_size, ainfo, FALSE);
			/* the value is in the ls byte */
			ainfo->offset += 3;
			break;
		case MONO_TYPE_I2:
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			add_general (&gr, &stack_size, ainfo, FALSE);
			/* the value is in the ls word */
			ainfo->offset += 2;
			break;
		case MONO_TYPE_I4:
		case MONO_TYPE_U4:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
		case MONO_TYPE_PTR:
		case MONO_TYPE_CLASS:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_STRING:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->params [i]->data.klass->enumtype) {
				simpletype = sig->params [i]->data.klass->enum_basetype->type;
				goto enum_calc_size;
			}

			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_TYPEDBYREF:
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		case MONO_TYPE_R4:
			/* single precision values are passed in integer registers */
			add_general (&gr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_R8:
			/* double precision values are passed in a pair of registers */
			add_general (&gr, &stack_size, ainfo, TRUE);
			break;
		default:
			g_assert_not_reached ();
		}
	}

	/* return value */
	{
		simpletype = sig->ret->type;
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
			cinfo->ret.storage = ArgInIRegPair;
			cinfo->ret.reg = sparc_i0;
			if (gr < 2)
				gr = 2;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.storage = ArgInFReg;
			cinfo->ret.reg = sparc_f0;
			break;
		case MONO_TYPE_VALUETYPE:
			if (sig->ret->data.klass->enumtype) {
				simpletype = sig->ret->data.klass->enum_basetype->type;
				goto enum_retvalue;
			}
			cinfo->ret.storage = ArgOnStack;
			break;
		case MONO_TYPE_TYPEDBYREF:
			cinfo->ret.storage = ArgOnStack;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	cinfo->stack_usage = stack_size;
	cinfo->reg_usage = gr;
	return cinfo;
}

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	switch (t->type) {
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
		return TRUE;
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_ARRAY:
		return TRUE;
	case MONO_TYPE_VALUETYPE:
		if (t->data.klass->enumtype)
			return is_regsize_var (t->data.klass->enum_basetype);
		return FALSE;
	}
	return FALSE;
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
	int i;
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = cfg->method->signature;

	cinfo = get_call_info (sig, FALSE);

	/* Use unused input registers */
	for (i = cinfo->reg_usage; i < 6; ++i)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (sparc_i0 + i));

	/* Use %l0..%l3 as global registers */
	for (i = 16; i < 20; ++i)
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
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;
	CallInfo *cinfo;

	header = ((MonoMethodNormal *)m->method)->header;

	sig = m->method->signature;

	cinfo = get_call_info (sig, FALSE);

	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
		case ArgInFReg:
		case ArgInIRegPair:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = cinfo->ret.reg;
			break;
		case ArgOnStack:
			/* valuetypes */
			m->ret->opcode = OP_REGOFFSET;
			m->ret->inst_basereg = sparc_fp;
			m->ret->inst_offset = 64;
			break;
		default:
			NOT_IMPLEMENTED;
		}
	}

	/*
	 * We use the Sparc V8 calling conventions for managed code as well.
	 * FIXME: Use something more optimized.
	 */

	/* Locals are allocated backwards from %fp */
	m->frame_reg = sparc_fp;
	offset = 0;

	/* 
	 * Reserve a stack slot for holding information used during exception 
	 * handling.
	 */
	if (header->num_clauses)
		offset += 8;

	if (m->method->save_lmf) {
		offset += sizeof (MonoLMF);
		m->arch.lmf_offset = offset;
	}

	curinst = m->locals_start;
	for (i = curinst; i < m->num_varinfo; ++i) {
		inst = m->varinfo [i];

		if (inst->opcode == OP_REGVAR) {
			//g_print ("allocating local %d to %s\n", i, mono_arch_regname (inst->dreg));
			continue;
		}

		/* inst->unused indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->unused && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
			size = mono_class_native_size (inst->inst_vtype->data.klass, &align);
		else
			size = mono_type_stack_size (inst->inst_vtype, &align);

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
		inst->inst_offset = -offset;

		//g_print ("allocating local %d to [%s - %d]\n", i, mono_arch_regname (inst->inst_basereg), - inst->inst_offset);
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		m->sig_cookie = cinfo->sig_cookie.offset + 68;
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

			if (!arg_type->byref && ((arg_type->type == MONO_TYPE_R4) 
									 || (arg_type->type == MONO_TYPE_R8)))
				/*
				 * Since float arguments are passed in integer registers, we need to
				 * save them to the stack in the prolog.
				 */
				inreg = FALSE;

			/* FIXME: Allocate volatile arguments to registers */
			if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
				inreg = FALSE;

			if (MONO_TYPE_ISSTRUCT (arg_type))
				/* FIXME: this isn't needed */
				inreg = FALSE;

			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInIRegPair:
				if (inreg) {
					inst->opcode = OP_REGVAR;
					inst->dreg = sparc_i0 + ainfo->reg;
					break;
				}
				else {
					/* Fall through */
				}
			case ArgOnStack:
			case ArgOnStackPair:
			case ArgInSplitRegStack:
				/* Split arguments are saved to the stack in the prolog */
				inst->opcode = OP_REGOFFSET;
				/* in parent frame */
				inst->inst_basereg = sparc_fp;
				inst->inst_offset = ainfo->offset + 68;

				if (!arg_type->byref && (arg_type->type == MONO_TYPE_R8)) {
					/* 
					 * It is very hard to load doubles from non-doubleword aligned
					 * memory locations. So if the offset is misaligned, we copy the
					 * argument to a stack location in the prolog.
					 */
					if (inst->inst_offset % 8) {
						inst->inst_basereg = sparc_fp;
						offset += 8;
						align = 8;
						offset += align - 1;
						offset &= ~(align - 1);
						inst->inst_offset = -offset;

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
				MONO_INST_NEW (m, indir, 0);
				*indir = *inst;
				inst->opcode = OP_SPARC_INARG_VT;
				inst->inst_left = indir;
			}
		}
	}

	/* 
	 * spillvars are stored between the normal locals and the storage reserved
	 * by the ABI.
	 */

	m->stack_offset = offset;

	/* Add a properly aligned dword for use by int<->float conversion opcodes */
	m->spill_count ++;
	mono_spillvar_offset_float (m, 0);

	g_free (cinfo);
}

/* 
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 */
MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual) {
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	guint32 extra_space = 0;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = get_call_info (sig, sig->pinvoke);

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;
		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instruction */
			in = call->args [i];
		} else {
			if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
				/* FIXME: Test varargs with 0 implicit args */
				/* FIXME: Test interaction with hasthis */
				/* Emit the signature cookie just before the first implicit argument */
				MonoInst *sig_arg;
				/* FIXME: Add support for signature tokens to AOT */
				cfg->disable_aot = TRUE;
				/* We allways pass the signature on the stack for simplicity */
				MONO_INST_NEW (cfg, arg, OP_SPARC_OUTARG_MEM);
				arg->inst_basereg = sparc_sp;
				arg->inst_imm = 68 + cinfo->sig_cookie.offset;
				MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
				sig_arg->inst_p0 = call->signature;
				arg->inst_left = sig_arg;
				arg->type = STACK_PTR;
				/* prepend, so they get reversed */
				arg->next = call->out_args;
				call->out_args = arg;
			}

			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code = in->cil_code;
			arg->inst_left = in;
			arg->type = in->type;
			/* prepend, we'll need to reverse them later */
			arg->next = call->out_args;
			call->out_args = arg;

			if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(sig->params [i - sig->hasthis]))) {
				MonoInst *inst;
				gint align;
				guint32 offset, pad;
				guint32 size;
				
				if (sig->params [i - sig->hasthis]->type == MONO_TYPE_TYPEDBYREF) {
					size = sizeof (MonoTypedRef);
					align = 4;
				}
				else
				if (sig->pinvoke)
					size = mono_type_native_stack_size (&in->klass->byval_arg, &align);
				else
					size = mono_type_stack_size (&in->klass->byval_arg, &align);

				/* 
				 * We use OP_OUTARG_VT to copy the valuetype to a stack location, then
				 * use the normal OUTARG opcodes to pass the address of the location to
				 * the callee.
				 */
				MONO_INST_NEW (cfg, inst, OP_OUTARG_VT);
				inst->inst_left = in;

				/* The first 6 argument locations are reserved */
				if (cinfo->stack_usage < 24)
					cinfo->stack_usage = 24;

				offset = ALIGN_TO (68 + cinfo->stack_usage, align);
				pad = offset - (68 + cinfo->stack_usage);

				inst->inst_c1 = offset;
				inst->unused = size;
				arg->inst_left = inst;

				cinfo->stack_usage += size;
				cinfo->stack_usage += pad;
			}

			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInFReg:
			case ArgInIRegPair:
				if (ainfo->storage == ArgInIRegPair)
					arg->opcode = OP_SPARC_OUTARG_REGPAIR;
				arg->unused = sparc_o0 + ainfo->reg;
				/* outgoing arguments begin at sp+68 */
				arg->inst_basereg = sparc_sp;
				arg->inst_imm = 68 + ainfo->offset;
				call->used_iregs |= 1 << ainfo->reg;

				if ((i >= sig->hasthis) && (sig->params [i - sig->hasthis]->type == MONO_TYPE_R8)) {
					/*
					 * The OUTARG (freg) implementation needs an extra dword to store
					 * the temporary value.
					 */
					extra_space += 8;
				}
				break;
			case ArgOnStack:
				arg->opcode = OP_SPARC_OUTARG_MEM;
				arg->inst_basereg = sparc_sp;
				arg->inst_imm = 68 + ainfo->offset;
				break;
			case ArgOnStackPair:
				arg->opcode = OP_SPARC_OUTARG_MEMPAIR;
				arg->inst_basereg = sparc_sp;
				arg->inst_imm = 68 + ainfo->offset;
				break;
			case ArgInSplitRegStack:
				arg->opcode = OP_SPARC_OUTARG_SPLIT_REG_STACK;
				arg->unused = sparc_o0 + ainfo->reg;
				arg->inst_basereg = sparc_sp;
				arg->inst_imm = 68 + ainfo->offset;
				call->used_iregs |= 1 << ainfo->reg;
				break;
			default:
				NOT_IMPLEMENTED;
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
	call->stack_usage = cinfo->stack_usage + extra_space;
	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;

	g_free (cinfo);
	return call;
}

/* Map opcode to the sparc condition codes */
static inline SparcCond
opcode_to_sparc_cond (int opcode)
{
	switch (opcode) {
	case OP_FBGE:
		return sparc_fbge;
	case OP_FBLE:
		return sparc_fble;
	case OP_FBEQ:
	case OP_FCEQ:
		return sparc_fbe;
	case OP_FBLT:
	case OP_FCLT:
	case OP_FCLT_UN:
		return sparc_fbl;
	case OP_FBGT:
	case OP_FCGT:
	case OP_FCGT_UN:
		return sparc_fbg;
	case CEE_BEQ:
	case OP_CEQ:
	case OP_COND_EXC_EQ:
		return sparc_be;
	case CEE_BNE_UN:
	case OP_COND_EXC_NE_UN:
		return sparc_bne;
	case CEE_BLT:
	case OP_CLT:
	case OP_COND_EXC_LT:
		return sparc_bl;
	case CEE_BLT_UN:
	case OP_CLT_UN:
	case OP_COND_EXC_LT_UN:
		return sparc_blu;
	case CEE_BGT:
	case OP_CGT:
	case OP_COND_EXC_GT:
		return sparc_bg;
	case CEE_BGT_UN:
	case OP_CGT_UN:
	case OP_COND_EXC_GT_UN:
		return sparc_bgu;
	case CEE_BGE:
	case OP_COND_EXC_GE:
		return sparc_bge;
	case CEE_BGE_UN:
	case OP_COND_EXC_GE_UN:
		return sparc_beu;
	case CEE_BLE:
	case OP_COND_EXC_LE:
		return sparc_ble;
	case CEE_BLE_UN:
	case OP_COND_EXC_LE_UN:
		return sparc_bleu;
	case OP_COND_EXC_OV:
		return sparc_bvs;
	case OP_COND_EXC_C:
		return sparc_bcs;

	case OP_COND_EXC_NO:
	case OP_COND_EXC_NC:
		NOT_IMPLEMENTED;
	default:
		g_assert_not_reached ();
		return sparc_be;
	}
}

#define COMPUTE_DISP(ins) \
if (ins->flags & MONO_INST_BRLABEL) { \
        if (ins->inst_i0->inst_c0) \
           disp = (ins->inst_i0->inst_c0 - ((guint8*)code - cfg->native_code)) >> 2; \
        else { \
            disp = 0; \
	        mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_LABEL, ins->inst_i0); \
        } \
} else { \
        if (ins->inst_true_bb->native_offset) \
           disp = (ins->inst_true_bb->native_offset - ((guint8*)code - cfg->native_code)) >> 2; \
        else { \
            disp = 0; \
	        mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
        } \
}

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

#define EMIT_COND_BRANCH_PREDICTED(ins,cond,annul,filldelay) \
    do { \
	    gint32 disp; \
        guint32 predict; \
        COMPUTE_DISP(ins); \
        predict = (disp != 0) ? 1 : 0; \
        g_assert (sparc_is_imm19 (disp)); \
		sparc_branchp (code, (annul), (cond), sparc_icc_short, (predict), disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)

#define EMIT_COND_BRANCH_BPR(ins,bop,predict,annul,filldelay) \
    do { \
	    gint32 disp; \
        COMPUTE_DISP(ins); \
		g_assert (sparc_is_imm22 (disp)); \
		sparc_ ## bop (code, (annul), (predict), ins->sreg2, disp); \
        if (filldelay) sparc_nop (code); \
    } while (0)

/* emit an exception if condition is fail */
/*
 * We put the exception throwing code out-of-line, at the end of the method
 */
#define EMIT_COND_SYSTEM_EXCEPTION(ins,cond,sexc_name) do {     \
		mono_add_patch_info (cfg, (guint8*)(code) - (cfg)->native_code,   \
				    MONO_PATCH_INFO_EXC, sexc_name);  \
        if (sparcv9) { \
           sparc_branchp (code, 0, (cond), sparc_icc_short, 0, 0); \
        } \
        else { \
			sparc_branch (code, 1, cond, 0);     \
        } \
        sparc_nop (code);     \
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
				sparc_set (code, ins->inst_offset, sparc_g1); \
				sparc_ ## op (code, sreg, ins->inst_destbasereg, sparc_g1); \
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
				if (sparcv9) {
					last_ins->opcode = OP_STOREI8_MEMBASE_IMM;
					last_ins->inst_offset = ins->inst_offset;
					last_ins->next = ins->next;				
					ins = ins->next;
					continue;
				}
			}
			break;
		case CEE_BEQ:
		case CEE_BNE_UN:
		case CEE_BLT:
		case CEE_BGT:
		case CEE_BGE:
		case CEE_BLE:
			/*
			 * Convert compare with zero+branch to BRcc
			 */
			/* 
			 * This only works in 64 bit mode, since it examines all 64
			 * bits of the register.
			 */
			if (0 && sparcv9 && last_ins && 
				(last_ins->opcode == OP_COMPARE_IMM) &&
				(last_ins->inst_imm == 0)) {
				MonoInst *next = ins->next;
				switch (ins->opcode) {
				case CEE_BEQ:
					ins->opcode = OP_SPARC_BRZ;
					break;
				case CEE_BNE_UN:
					ins->opcode = OP_SPARC_BRNZ;
					break;
				case CEE_BLT:
					ins->opcode = OP_SPARC_BRLZ;
					break;
				case CEE_BGT:
					ins->opcode = OP_SPARC_BRGZ;
					break;
				case CEE_BGE:
					ins->opcode = OP_SPARC_BRGEZ;
					break;
				case CEE_BLE:
					ins->opcode = OP_SPARC_BRLEZ;
					break;
				default:
					g_assert_not_reached ();
				}
				ins->sreg2 = last_ins->sreg1;
				*last_ins = *ins;
				last_ins->next = next;
				ins = next;
				continue;
			}
			break;
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
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

/* Parameters used by the register allocator */

/* Use %l4..%l7 as local registers */
#define ARCH_CALLER_REGS (0xf0<<16)
/* Use %f2..%f30 as the double precision floating point local registers */
#define ARCH_CALLER_FREGS (0x55555554)

#undef DEBUG
#define DEBUG(a) if (cfg->verbose_level > 1) a
//#define DEBUG(a)
#define reg_is_freeable(r) ((1 << (r)) & ARCH_CALLER_REGS)
#define freg_is_freeable(r) (((1) << (r)) & ARCH_CALLER_FREGS)

typedef struct {
	int born_in;
	int killed_in;
	int last_use;
	int prev_use;
} RegTrack;

static const char*const * ins_spec = sparc_desc;

static void
print_ins (int i, MonoInst *ins)
{
	const char *spec = ins_spec [ins->opcode];
	g_print ("\t%-2d %s", i, mono_inst_name (ins->opcode));
	if (spec [MONO_INST_DEST]) {
		if (ins->dreg >= MONO_MAX_IREGS)
			g_print (" R%d <-", ins->dreg);
		else
			if (spec [MONO_INST_DEST] == 'b')
				g_print (" [%s + 0x%x] <-", mono_arch_regname (ins->dreg), ins->inst_offset);
		else
			g_print (" %s <-", mono_arch_regname (ins->dreg));
	}
	if (spec [MONO_INST_SRC1]) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			g_print (" R%d", ins->sreg1);
		else
			if (spec [MONO_INST_SRC1] == 'b')
				g_print (" [%s + 0x%x]", mono_arch_regname (ins->sreg1), ins->inst_offset);
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

#define STACK_OFFSETS_POSITIVE

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
			cfg->stack_offset += sizeof (gpointer);
			info->offset = - cfg->stack_offset;
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
			cfg->stack_offset += sizeof (double);
			cfg->stack_offset = ALIGN_TO (cfg->stack_offset, 8);
			info->offset = - cfg->stack_offset;
		}

		if (i == spillvar)
			return (*si)->offset;

		i++;
		si = &(*si)->next;
	}

	g_assert_not_reached ();
	return 0;
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
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%sp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
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
	DEBUG (g_print ("SPILLED STORE (%d at 0x%08x(%%sp)) R%d (from %s)\n", spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
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

/* FIXME: Strange loads from the stack in basic-float.cs:test_2_rem */

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

	/* FIXME: Use caller saved regs and %i1-%2 for allocation */

	if (!bb->code)
		return;
	rs->next_vireg = bb->max_ireg;
	rs->next_vfreg = bb->max_freg;
	mono_regstate_assign (rs);
	reginfo = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vireg);
	reginfof = mono_mempool_alloc0 (cfg->mempool, sizeof (RegTrack) * rs->next_vfreg);
	rs->ifree_mask = ARCH_CALLER_REGS;
	rs->ffree_mask = ARCH_CALLER_FREGS;

	ins = bb->code;
	i = 1;
	DEBUG (g_print ("LOCAL regalloc: basic block: %d\n", bb->block_num));
	/* forward pass on the instructions to collect register liveness info */
	while (ins) {
		spec = ins_spec [ins->opcode];
		if (!spec) {
			mono_print_tree_nl (ins);
			g_assert (spec);
		}
		DEBUG (print_ins (i, ins));

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

	cur_iregs = ARCH_CALLER_REGS;
	cur_fregs = ARCH_CALLER_FREGS;

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
			/* Dont free register which can't be allocated */
			if (reg_is_freeable (ins->dreg)) {
				cur_iregs |= 1 << ins->dreg;
				DEBUG (g_print ("adding %d to cur_iregs\n", ins->dreg));
			}
		} else if (ins->opcode == OP_SETFREG) {
			if (freg_is_freeable (ins->dreg)) {
				cur_fregs |= 1 << ins->dreg;
				DEBUG (g_print ("adding %d to cur_fregs\n", ins->dreg));
			}
		} else if (spec [MONO_INST_CLOB] == 'c') {
			MonoCallInst *cinst = (MonoCallInst*)ins;
			DEBUG (g_print ("excluding regs 0x%x from cur_iregs (0x%x)\n", cinst->used_iregs, cur_iregs));
			cur_iregs &= ~cinst->used_iregs;
			cur_fregs &= ~cinst->used_fregs;
			DEBUG (g_print ("available cur_iregs: 0x%x\n", cur_iregs));
			/* registers used by the calling convention are excluded from 
			 * allocation: they will be selectively enabled when they are 
			 * assigned by the special SETREG opcodes.
			 */
		}
		dest_mask = src1_mask = src2_mask = cur_iregs;

		/*
		 * DEST
		 */
		/* update for use with FP regs... */
		if (spec [MONO_INST_DEST] == 'f') {
			if (ins->dreg >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->dreg];
				prev_dreg = ins->dreg;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					dest_mask = cur_fregs;
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
			} else {
				prev_dreg = -1;
			}
			if (freg_is_freeable (ins->dreg) && prev_dreg >= 0 && (reginfo [prev_dreg].born_in >= i || !(cur_fregs & (1 << ins->dreg)))) {
				DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
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
					/* The second register must be a pair of the first */
					dest_mask = 1 << (rs->iassign [prev_dreg] + 1);
					val = mono_regstate_alloc_int (rs, dest_mask);
					if (val < 0)
						val = get_register_spilling (cfg, tmp, ins, dest_mask, hreg);
					rs->iassign [hreg] = val;
					if (spill)
						create_spilled_store (cfg, spill, val, hreg, ins);
				}
				else {
					/* The second register must be a pair of the first */
					if (val != rs->iassign [prev_dreg] + 1) {
						dest_mask = 1 << (rs->iassign [prev_dreg] + 1);

						val = mono_regstate_alloc_int (rs, dest_mask);
						if (val < 0)
							val = get_register_spilling (cfg, tmp, ins, dest_mask, hreg);

						create_copy_ins (cfg, rs->iassign [hreg], val, ins);

						rs->iassign [hreg] = val;
					}
				}					

				DEBUG (g_print ("\tassigned hreg %s to dest R%d\n", mono_arch_regname (val), hreg));
				rs->isymbolic [val] = hreg;

				if (reg_is_freeable (val) && hreg >= 0 && (reginfo [hreg].born_in >= i && !(cur_iregs & (1 << val)))) {
					DEBUG (g_print ("\tfreeable %s (R%d)\n", mono_arch_regname (val), hreg));
					mono_regstate_free_int (rs, val);
				}
			}
		} else {
			prev_dreg = -1;
		}
		if (spec [MONO_INST_DEST] != 'f' && reg_is_freeable (ins->dreg) && prev_dreg >= 0 && (reginfo [prev_dreg].born_in >= i)) {
			DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
			mono_regstate_free_int (rs, ins->dreg);
		}

		/**
		 * SRC1
		 */
		if (spec [MONO_INST_SRC1] == 'f') {
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
					src1_mask = cur_fregs;
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
				if (0 && (ins->opcode == OP_MOVE) && reg_is_freeable (ins->dreg)) {
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

		/*
		 * SRC2
		 */
		if (spec [MONO_INST_SRC2] == 'f') {
			if (ins->sreg2 >= MONO_MAX_FREGS) {
				val = rs->fassign [ins->sreg2];
				prev_sreg2 = ins->sreg2;
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					src2_mask = cur_fregs;
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
			guint32 clob_mask = ARCH_CALLER_REGS;
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
}

static void
sparc_patch (guint8 *code, const guint8 *target)
{
	guint32 ins = *(guint32*)code;
	guint32 op = ins >> 30;
	guint32 op2 = (ins >> 22) & 0x7;
	guint32 rd = (ins >> 25) & 0x1f;
	gint32 disp = (target - code) >> 2;

//	g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);

	if ((op == 0) && (op2 == 2)) {
		if (!sparc_is_imm22 (disp))
			NOT_IMPLEMENTED;
		/* Bicc */
		*(guint32*)code = ((ins >> 22) << 22) | (disp & 0x3fffff);
	}
	else if ((op == 0) && (op2 == 1)) {
		if (!sparc_is_imm19 (disp))
			NOT_IMPLEMENTED;
		/* BPcc */
		*(guint32*)code = ((ins >> 19) << 19) | (disp & 0x7ffff);
	}
	else if ((op == 0) && (op2 == 3)) {
		if (!sparc_is_imm16 (disp))
			NOT_IMPLEMENTED;
		/* BPr */
		*(guint32*)code &= ~(0x180000 | 0x3fff);
		*(guint32*)code |= ((disp << 21) & (0x180000)) | (disp & 0x3fff);
	}
	else if ((op == 0) && (op2 == 6)) {
		if (!sparc_is_imm22 (disp))
			NOT_IMPLEMENTED;
		/* FBicc */
		*(guint32*)code = ((ins >> 22) << 22) | (disp & 0x3fffff);
	}
	else if ((op == 0) && (op2 == 4)) {
		guint32 ins2 = *(guint32*)(code + 4);

		if (((ins2 >> 30) == 2) && (((ins2 >> 19) & 0x3f) == 2)) {
			/* sethi followed by or */
			guint32 *p = (guint32*)code;
			sparc_set (p, target, rd);
			while (p <= (code + 4))
				sparc_nop (p);
		}
		else if (ins2 == 0x01000000) {
			/* sethi followed by nop */
			guint32 *p = (guint32*)code;
			sparc_set (p, target, rd);
			while (p <= (code + 4))
				sparc_nop (p);
		}
		else if ((sparc_inst_op (ins2) == 3) && (sparc_inst_imm (ins2))) {
			/* sethi followed by load/store */
			guint32 t = (guint32)target;
			*(guint32*)code &= ~(0x3fffff);
			*(guint32*)code |= (t >> 10);
			*(guint32*)(code + 4) &= ~(0x3ff);
			*(guint32*)(code + 4) |= (t & 0x3ff);
		}
		else if ((sparc_inst_op (ins2) == 2) && (sparc_inst_op3 (ins2) == 0x38) && 
				 (sparc_inst_imm (ins2))) {
			/* sethi followed by jmpl */
			guint32 t = (guint32)target;
			*(guint32*)code &= ~(0x3fffff);
			*(guint32*)code |= (t >> 10);
			*(guint32*)(code + 4) &= ~(0x3ff);
			*(guint32*)(code + 4) |= (t & 0x3ff);
		}
		else
			NOT_IMPLEMENTED;
	}
	else if (op == 01) {
		sparc_call_simple (code, target - code);
	}
	else if ((op == 2) && (sparc_inst_op3 (ins) == 0x2) && sparc_inst_imm (ins)) {
		/* mov imm, reg */
		g_assert (sparc_is_imm13 (target));
		*(guint32*)code &= ~(0x1fff);
		*(guint32*)code |= (guint32)target;
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
	sparc_st_imm (code, sparc_o0, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* Save previous_lmf */
	sparc_ld (code, sparc_o0, sparc_g0, sparc_o7);
	sparc_st_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* Set new lmf */
	sparc_add_imm (code, FALSE, sparc_fp, lmf_offset, sparc_o7);
	sparc_st (code, sparc_o7, sparc_o0, sparc_g0);

	return code;
}

guint32*
mono_sparc_emit_restore_lmf (guint32 *code, guint32 lmf_offset)
{
	/* Load previous_lmf */
	sparc_ld_imm (code, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), sparc_l0);
	/* Load lmf_addr */
	sparc_ld_imm (code, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), sparc_l1);
	/* *(lmf) = previous_lmf */
	sparc_st (code, sparc_l0, sparc_l1, sparc_g0);
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
		gint32 lmf_offset = - cfg->arch.lmf_offset;

		/* Save sp */
		sparc_st_imm (code, sparc_sp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, sp));
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
			size = mono_type_stack_size (call->signature->ret, NULL);
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
	/* FIXME: do this in the local reg allocator */
	switch (ins->opcode) {
	case OP_VOIDCALL:
	case OP_VOIDCALL_REG:
	case OP_VOIDCALL_MEMBASE:
		break;
	case CEE_CALL:
	case OP_CALL_REG:
	case OP_CALL_MEMBASE:
		sparc_mov_reg_reg (code, sparc_o0, ins->dreg);
		break;
	case OP_LCALL:
	case OP_LCALL_REG:
	case OP_LCALL_MEMBASE:
		/* 
		 * ins->dreg is the least significant reg due to the lreg: LCALL rule
		 * in inssel.brg.
		 */
		sparc_mov_reg_reg (code, sparc_o0, ins->dreg + 1);
		sparc_mov_reg_reg (code, sparc_o1, ins->dreg);
		break;
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		sparc_fmovs (code, sparc_f0, ins->dreg);
		if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4)
			sparc_fstod (code, ins->dreg, ins->dreg);
		else
			sparc_fmovs (code, sparc_f1, ins->dreg + 1);
		break;
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
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
 * Required before a tail call.
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

	sig = method->signature;

	cinfo = get_call_info (sig, FALSE);
	
	/* This is the opposite of the code in emit_prolog */

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		guint32 stack_offset;
		MonoType *arg_type;
		inst = cfg->varinfo [i];

		if (sig->hasthis && (i == 0))
			arg_type = mono_defaults.object_class;
		else
			arg_type = sig->params [i - sig->hasthis];

		stack_offset = ainfo->offset + 68;
		ireg = sparc_i0 + ainfo->reg;

		if (ainfo->storage == ArgInSplitRegStack) {
			g_assert (inst->opcode == OP_REGOFFSET);
			if (!sparc_is_imm13 (stack_offset))
				NOT_IMPLEMENTED;
			sparc_st_imm (code, inst->inst_basereg, stack_offset, sparc_i5);
		}

		if (!arg_type->byref && (arg_type->type == MONO_TYPE_R8)) {
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
				if (stack_offset & 0x1)
					/* FIXME: Is this ldsb or ldub ? */
					sparc_ldsb_imm (code, inst->inst_basereg, stack_offset, ireg);
				else
					if (stack_offset & 0x2)
						sparc_ldsh_imm (code, inst->inst_basereg, stack_offset, ireg);
				else
					sparc_ld_imm (code, inst->inst_basereg, stack_offset, ireg);
			}
		else
			if ((ainfo->storage == ArgInIRegPair) && (inst->opcode != OP_REGVAR)) {
				/* Argument in regpair, but need to be saved to stack */
				if (!sparc_is_imm13 (inst->inst_offset + 4))
					NOT_IMPLEMENTED;
				sparc_ld_imm (code, inst->inst_basereg, inst->inst_offset, ireg);
				sparc_st_imm (code, inst->inst_basereg, inst->inst_offset + 4, ireg + 1);
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

/*
 * mono_sparc_get_vcall_slot_addr:
 *
 *  Determine the vtable slot used by a virtual call.
 */
gpointer*
mono_sparc_get_vcall_slot_addr (guint32 *code, guint32 *fp)
{
	guint32 ins = code [0];
	guint32 prev_ins = code [-1];

	mono_sparc_flushw ();

	if ((sparc_inst_op (ins) == 0x2) && (sparc_inst_op3 (ins) == 0x38)) {
		if ((sparc_inst_op (prev_ins) == 0x3) && (sparc_inst_op3 (prev_ins) == 0)) {
			/* ld [r1 + CONST ], r2; call r2 */
			guint32 base = sparc_inst_rs1 (prev_ins);
			guint32 disp = sparc_inst_imm13 (prev_ins);
			guint32 base_val;

			g_assert (sparc_inst_rd (prev_ins) == sparc_inst_rs1 (ins));

			g_assert ((base >= sparc_o0) && (base <= sparc_i7));
			
			base_val = fp [base - 16];

			return (gpointer)((guint8*)base_val + disp);
		}
		else
			g_assert_not_reached ();
	}
	else
		g_assert_not_reached ();

	return FALSE;
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

	if (cfg->opt & MONO_OPT_PEEPHOLE)
		peephole_pass (cfg, bb);

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		NOT_IMPLEMENTED;
	}

	ins = bb->code;
	while (ins) {
		guint8* code_start;

		offset = (guint8*)code - cfg->native_code;

		max_len = ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = (guint32*)(cfg->native_code + offset);
		}
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
		case OP_STOREI4_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, st);
			break;
		case OP_STOREI8_MEMBASE_IMM:
			/* Only generated by peephole opts */
			g_assert ((ins->inst_offset % 8) == 0);
			g_assert (ins->inst_imm == 0);
			EMIT_STORE_MEMBASE_IMM (ins, stx);
			break;
		case OP_STOREI1_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, stb);
			break;
		case OP_STOREI2_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, sth);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, st);
			break;
		case OP_STOREI8_MEMBASE_REG:
			/* Only used by OP_MEMSET */
			EMIT_STORE_MEMBASE_REG (ins, std);
			break;
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
			sparc_ld (code, ins->inst_p0, sparc_g0, ins->dreg);
			break;
		/* The cast IS BAD (maybe).  But it needs to be done... */
		case OP_LOADU4_MEM:
			sparc_set (code, (guint)ins->inst_p0, ins->dreg);
			sparc_ld (code, ins->dreg, sparc_g0, ins->dreg);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
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
		case CEE_CONV_I1:
			sparc_sll_imm (code, ins->sreg1, 24, sparc_o7);
			sparc_sra_imm (code, sparc_o7, 24, ins->dreg);
			break;
		case CEE_CONV_I2:
			sparc_sll_imm (code, ins->sreg1, 16, sparc_o7);
			sparc_sra_imm (code, sparc_o7, 16, ins->dreg);
			break;
		/* GCC does this one differently.  Don't ask me WHY. */
		case CEE_CONV_U1:
			sparc_and_imm (code, FALSE, ins->sreg1, 0xff, ins->dreg);
			break;
		case CEE_CONV_U2:
			sparc_sll_imm (code, ins->sreg1, 16, sparc_o7);
			sparc_srl_imm (code, sparc_o7, 16, ins->dreg);
			break;
		case OP_COMPARE:
			sparc_cmp (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
			if (sparc_is_imm13 (ins->inst_imm))
				sparc_cmp_imm (code, ins->sreg1, ins->inst_imm);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_cmp (code, ins->sreg1, sparc_o7);
			}
			break;
		case OP_X86_TEST_NULL:
			sparc_cmp_imm (code, ins->sreg1, 0);
			break;
		case CEE_BREAK:
			/*
			 * gdb does not like encountering 'ta 1' in the debugged code. So 
			 * instead of emitting a trap, we emit a call a C function and place a 
			 * breakpoint there.
			 */
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, mono_sparc_break);
			sparc_call_simple (code, 0);
			sparc_nop (code);
			break;
		case OP_ADDCC:
			sparc_add (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case CEE_ADD:
			sparc_add (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ADDCC_IMM:
		case OP_ADD_IMM:
			/* according to inssel-long32.brg, this should set cc */
			EMIT_ALU_IMM (ins, add, TRUE);
			break;
		case OP_ADC:
			/* according to inssel-long32.brg, this should set cc */
			sparc_addx (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ADC_IMM:
			EMIT_ALU_IMM (ins, addx, TRUE);
			break;
		case OP_SUBCC:
			sparc_sub (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case CEE_SUB:
			sparc_sub (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SUBCC_IMM:
		case OP_SUB_IMM:
			/* according to inssel-long32.brg, this should set cc */
			EMIT_ALU_IMM (ins, sub, TRUE);
			break;
		case OP_SBB:
			/* according to inssel-long32.brg, this should set cc */
			sparc_subx (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SBB_IMM:
			EMIT_ALU_IMM (ins, subx, TRUE);
			break;
		case CEE_AND:
			sparc_and (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_AND_IMM:
			EMIT_ALU_IMM (ins, and, FALSE);
			break;
		case CEE_DIV:
			/* Sign extend sreg1 into %y */
			sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
			sparc_wry (code, sparc_o7, sparc_g0);
			sparc_sdiv (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			EMIT_COND_SYSTEM_EXCEPTION (code, sparc_boverflow, "ArithmeticException");
			break;
		case CEE_DIV_UN:
			sparc_wry (code, sparc_g0, sparc_g0);
			sparc_udiv (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_DIV_IMM: {
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
				EMIT_COND_SYSTEM_EXCEPTION (code, sparc_boverflow, "ArithmeticException");
			}
			break;
		}
		case CEE_REM:
			/* Sign extend sreg1 into %y */
			sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
			sparc_wry (code, sparc_o7, sparc_g0);
			sparc_sdiv (code, TRUE, ins->sreg1, ins->sreg2, sparc_o7);
			EMIT_COND_SYSTEM_EXCEPTION (code, sparc_boverflow, "ArithmeticException");
			sparc_smul (code, FALSE, ins->sreg2, sparc_o7, sparc_o7);
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case CEE_REM_UN:
			sparc_wry (code, sparc_g0, sparc_g0);
			sparc_udiv (code, FALSE, ins->sreg1, ins->sreg2, sparc_o7);
			sparc_umul (code, FALSE, ins->sreg2, sparc_o7, sparc_o7);
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case OP_REM_IMM:
			/* Sign extend sreg1 into %y */
			sparc_sra_imm (code, ins->sreg1, 31, sparc_o7);
			sparc_wry (code, sparc_o7, sparc_g0);
			if (!sparc_is_imm13 (ins->inst_imm)) {
				sparc_set (code, ins->inst_imm, sparc_g1);
				sparc_sdiv (code, TRUE, ins->sreg1, sparc_g1, sparc_o7);
				EMIT_COND_SYSTEM_EXCEPTION (code, sparc_boverflow, "ArithmeticException");
				sparc_smul (code, FALSE, sparc_o7, sparc_g1, sparc_o7);
			}
			else {
				sparc_sdiv_imm (code, TRUE, ins->sreg1, ins->inst_imm, sparc_o7);
				EMIT_COND_SYSTEM_EXCEPTION (code, sparc_boverflow, "ArithmeticException");
				sparc_smul_imm (code, FALSE, sparc_o7, ins->inst_imm, sparc_o7);
			}
			sparc_sub (code, FALSE, ins->sreg1, sparc_o7, ins->dreg);
			break;
		case CEE_OR:
			sparc_or (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_OR_IMM:
			EMIT_ALU_IMM (ins, or, FALSE);
			break;
		case CEE_XOR:
			sparc_xor (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_XOR_IMM:
			EMIT_ALU_IMM (ins, xor, FALSE);
			break;
		case CEE_SHL:
			sparc_sll (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SHL_IMM:
			if (sparc_is_imm13 (ins->inst_imm))
				sparc_sll_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_sll (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case CEE_SHR:
			sparc_sra (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SHR_IMM:
			if (sparc_is_imm13 (ins->inst_imm))
				sparc_sra_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_sra (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case OP_SHR_UN_IMM:
			if (sparc_is_imm13 (ins->inst_imm))
				sparc_srl_imm (code, ins->sreg1, ins->inst_imm, ins->dreg);
			else {
				sparc_set (code, ins->inst_imm, sparc_o7);
				sparc_srl (code, ins->sreg1, sparc_o7, ins->dreg);
			}
			break;
		case CEE_SHR_UN:
			sparc_srl (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case CEE_NOT:
			/* can't use sparc_not */
			sparc_xnor (code, FALSE, ins->sreg1, sparc_g0, ins->dreg);
			break;
		case CEE_NEG:
			/* can't use sparc_neg */
			sparc_sub (code, FALSE, sparc_g0, ins->sreg1, ins->dreg);
			break;
		case CEE_MUL:
			sparc_smul (code, FALSE, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_MUL_IMM: {
			int i, imm;

			/* Transform multiplication into a shift */
			for (i = 1; i < 30; ++i) {
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
		case CEE_MUL_OVF:
			sparc_smul (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			sparc_rdy (code, sparc_g1);
			sparc_sra_imm (code, ins->dreg, 31, sparc_o7);
			sparc_cmp (code, sparc_g1, sparc_o7);
			EMIT_COND_SYSTEM_EXCEPTION (ins, sparc_bne, "OverflowException");
			break;
		case CEE_MUL_OVF_UN:
			sparc_umul (code, TRUE, ins->sreg1, ins->sreg2, ins->dreg);
			sparc_rdy (code, sparc_o7);
			sparc_cmp (code, sparc_o7, sparc_g0);
			EMIT_COND_SYSTEM_EXCEPTION (ins, sparc_bne, "OverflowException");
			break;
		case OP_ICONST:
		case OP_SETREGIMM:
			sparc_set (code, ins->inst_c0, ins->dreg);
			break;
		case OP_AOTCONST:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			sparc_set (code, 0xffffff, ins->dreg);
			break;
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
		case OP_SETREG:
			if (ins->sreg1 != ins->dreg)
				sparc_mov_reg_reg (code, ins->sreg1, ins->dreg);
			break;
		case CEE_JMP:
			if (cfg->method->save_lmf)
				NOT_IMPLEMENTED;

			code = emit_load_volatile_arguments (cfg, code);
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			sparc_set (code, 0xffffff, sparc_o7);
			sparc_jmpl (code, sparc_o7, sparc_g0, sparc_g0);
			/* Restore parent frame in delay slot */
			sparc_restore_imm (code, sparc_g0, 0, sparc_g0);
			break;
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			sparc_ld_imm (code, ins->sreg1, 0, sparc_g0);
			break;
		case OP_ARGLIST:
			sparc_add_imm (code, FALSE, sparc_fp, cfg->sig_cookie, sparc_o7);
			sparc_st_imm (code, sparc_o7, ins->sreg1, 0);
			break;
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case CEE_CALL:
			call = (MonoCallInst*)ins;
			g_assert (!call->virtual);
			code = emit_save_sp_to_lmf (cfg, code);
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_METHOD, call->method);
			else
				mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_ABS, call->fptr);
			sparc_call_simple (code, 0);
			sparc_nop (code);

			code = emit_vret_token (ins, code);
			code = emit_move_return_value (ins, code);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
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
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			call = (MonoCallInst*)ins;
			g_assert (sparc_is_imm13 (ins->inst_offset));
			code = emit_save_sp_to_lmf (cfg, code);
			sparc_ld_imm (code, ins->inst_basereg, ins->inst_offset, sparc_o7);
			sparc_jmpl (code, sparc_o7, sparc_g0, sparc_callsite);
			if (call->virtual)
				sparc_or_imm (code, FALSE, sparc_g0, 0xca, sparc_g0);
			else
				sparc_nop (code);

			code = emit_vret_token (ins, code);
			code = emit_move_return_value (ins, code);
			break;
		case OP_SETFRET:
			if (cfg->method->signature->ret->type == MONO_TYPE_R4)
				sparc_fdtos (code, ins->sreg1, sparc_f0);
			else {
				sparc_fmovs (code, ins->sreg1, ins->dreg);
				sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
			}
			break;
		case OP_OUTARG:
			g_assert_not_reached ();
			break;
		case OP_LOCALLOC:
			/* Keep alignment */
			sparc_add_imm (code, FALSE, ins->sreg1, MONO_ARCH_FRAME_ALIGNMENT - 1, ins->dreg);
			sparc_set (code, ~(MONO_ARCH_FRAME_ALIGNMENT - 1), sparc_o7);
			sparc_and (code, FALSE, ins->dreg, sparc_o7, ins->dreg);
			sparc_sub (code, FALSE, sparc_sp, ins->dreg, ins->dreg);
			/* Keep %sp valid at all times */
			sparc_mov_reg_reg (code, ins->dreg, sparc_sp);
			g_assert (sparc_is_imm13 (cfg->arch.localloc_offset));
			sparc_add_imm (code, FALSE, ins->dreg, cfg->arch.localloc_offset, ins->dreg);
			break;
		case OP_SPARC_LOCALLOC_IMM: {
			guint32 offset = ins->inst_c0;
			offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);
			if (sparc_is_imm13 (offset))
				sparc_sub_imm (code, FALSE, sparc_sp, offset, sparc_sp);
			else {
				sparc_set (code, offset, sparc_o7);
				sparc_sub (code, FALSE, sparc_sp, sparc_o7, sparc_sp);
			}
			sparc_mov_reg_reg (code, sparc_sp, ins->dreg);
			g_assert (sparc_is_imm13 (cfg->arch.localloc_offset));
			sparc_add_imm (code, FALSE, ins->dreg, cfg->arch.localloc_offset, ins->dreg);
			break;
		}
		case CEE_RET:
			/* The return is done in the epilog */
			g_assert_not_reached ();
			break;
		case CEE_THROW:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception");
			sparc_call_simple (code, 0);
			/* Delay slot */
			sparc_mov_reg_reg (code, ins->sreg1, sparc_o0);
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
				sparc_set (code, spvar->inst_offset, sparc_g0);
				sparc_st (code, sparc_o7, spvar->inst_basereg, sparc_g0);
			}
			else
				sparc_st_imm (code, sparc_o7, spvar->inst_basereg, spvar->inst_offset);
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (!sparc_is_imm13 (spvar->inst_offset)) {
				sparc_set (code, spvar->inst_offset, sparc_g0);
				sparc_ld (code, spvar->inst_basereg, sparc_g0, sparc_o7);
			}
			else
				sparc_ld_imm (code, spvar->inst_basereg, spvar->inst_offset, sparc_o7);
			sparc_jmpl_imm (code, sparc_o7, 8, sparc_g0);
			/* Delay slot */
			sparc_mov_reg_reg (code, ins->sreg1, sparc_o0);
			break;
		}
		case CEE_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			if (!sparc_is_imm13 (spvar->inst_offset)) {
				sparc_set (code, spvar->inst_offset, sparc_g0);
				sparc_ld (code, spvar->inst_basereg, sparc_g0, sparc_o7);
			}
			else
				sparc_ld_imm (code, spvar->inst_basereg, spvar->inst_offset, sparc_o7);
			sparc_jmpl_imm (code, sparc_o7, 8, sparc_g0);
			sparc_nop (code);
			break;
		}
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			sparc_call_simple (code, 0);
			sparc_nop (code);
			break;
		case OP_LABEL:
			ins->inst_c0 = (guint8*)code - cfg->native_code;
			break;
		case CEE_BR:
			//g_print ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
			if ((ins->inst_target_bb == bb->next_bb) && ins == bb->last_ins)
				break;
			if (ins->flags & MONO_INST_BRLABEL) {
				if (ins->inst_i0->inst_c0) {
					gint32 disp = (ins->inst_i0->inst_c0 - ((guint8*)code - cfg->native_code)) >> 2;
					g_assert (sparc_is_imm22 (disp));
					sparc_branch (code, 1, sparc_ba, disp);
				} else {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_LABEL, ins->inst_i0);
					sparc_branch (code, 1, sparc_ba, 0);
				}
			} else {
				if (ins->inst_target_bb->native_offset) {
					gint32 disp = (ins->inst_target_bb->native_offset - ((guint8*)code - cfg->native_code)) >> 2;
					g_assert (sparc_is_imm22 (disp));
					sparc_branch (code, 1, sparc_ba, disp);
				} else {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
					sparc_branch (code, 1, sparc_ba, 0);
				} 
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
			//if (cfg->opt & MONO_OPT_CMOV) {
			if (0) {
				sparc_clr_reg (code, ins->dreg);
				sparc_movcc_imm (code, sparc_icc, opcode_to_sparc_cond (ins->opcode), 1, ins->dreg);
			}
			else {
				sparc_clr_reg (code, ins->dreg);
				sparc_branch (code, 1, opcode_to_sparc_cond (ins->opcode), 2);
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
			EMIT_COND_SYSTEM_EXCEPTION (ins, opcode_to_sparc_cond (ins->opcode), ins->inst_p1);
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
		case CEE_BLE_UN: {
			if (sparcv9)
				EMIT_COND_BRANCH_PREDICTED (ins, opcode_to_sparc_cond (ins->opcode), 1, 1);
			else
				EMIT_COND_BRANCH (ins, opcode_to_sparc_cond (ins->opcode), 1, 1);
			break;
		}
		case OP_SPARC_BRZ:
			/* We misuse the macro arguments */
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
			sparc_sethi (code, 0, sparc_o7);
			sparc_lddf_imm (code, sparc_o7, 0, ins->dreg);
			break;
		case OP_R4CONST:
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R4, ins->inst_p0);
			sparc_sethi (code, 0, sparc_o7);
			sparc_ldf_imm (code, sparc_o7, 0, ins->dreg);

			/* Extend to double */
			sparc_fstod (code, ins->dreg, ins->dreg);
			break;
		case OP_STORER8_MEMBASE_REG:
			if (!sparc_is_imm13 (ins->inst_offset + 4)) {
				sparc_set (code, ins->inst_offset, sparc_o7);
				if (ins->inst_offset % 8) {
					/* Misaligned */
					sparc_add (code, FALSE, ins->inst_destbasereg, sparc_o7, sparc_o7);
					sparc_stf (code, ins->sreg1, sparc_o7, sparc_g0);
					sparc_stf_imm (code, ins->sreg1 + 1, sparc_o7, 4);
				} else
					sparc_stdf (code, ins->sreg1, ins->inst_destbasereg, sparc_o7);
			}
			else {
				if (ins->inst_offset % 8) {
					/* Misaligned */
					sparc_stf_imm (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
					sparc_stf_imm (code, ins->sreg1 + 1, ins->inst_destbasereg, ins->inst_offset + 4);
				} else
					sparc_stdf_imm (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			}
			break;
		case OP_LOADR8_MEMBASE:
			g_assert ((ins->inst_offset % 8) == 0);
			EMIT_LOAD_MEMBASE (ins, lddf);
			break;
		case OP_STORER4_MEMBASE_REG:
			/* This requires a double->single conversion */
			sparc_fdtos (code, ins->sreg1, sparc_f0);
			if (!sparc_is_imm13 (ins->inst_offset)) {
				sparc_set (code, ins->inst_offset, sparc_o7);
				sparc_stf (code, sparc_f0, ins->inst_destbasereg, sparc_o7);
			}
			else
				sparc_stf_imm (code, sparc_f0, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_LOADR4_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldf);
			/* Extend to double */
			sparc_fstod (code, ins->dreg, ins->dreg);
			break;
		case OP_FMOVE:
			sparc_fmovs (code, ins->sreg1, ins->dreg);
			sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
			break;
		case CEE_CONV_R4: {
			guint32 offset = mono_spillvar_offset_float (cfg, 0);
			if (!sparc_is_imm13 (offset))
				NOT_IMPLEMENTED;
			sparc_st_imm (code, ins->sreg1, sparc_sp, offset);
			sparc_ldf_imm (code, sparc_sp, offset, sparc_f0);
			sparc_fitos (code, sparc_f0, sparc_f0);
			sparc_fstod (code, sparc_f0, ins->dreg);
			break;
		}
		case CEE_CONV_R8: {
			guint32 offset = mono_spillvar_offset_float (cfg, 0);
			if (!sparc_is_imm13 (offset))
				NOT_IMPLEMENTED;
			sparc_st_imm (code, ins->sreg1, sparc_sp, offset);
			sparc_ldf_imm (code, sparc_sp, offset, sparc_f0);
			sparc_fitod (code, sparc_f0, ins->dreg);
			break;
		}
		case OP_FCONV_TO_I1:
		case OP_FCONV_TO_U1:
		case OP_FCONV_TO_I2:
		case OP_FCONV_TO_U2:
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
		case OP_FCONV_TO_U4:
		case OP_FCONV_TO_U: {
			guint32 offset = mono_spillvar_offset_float (cfg, 0);
			if (!sparc_is_imm13 (offset))
				NOT_IMPLEMENTED;
			/* FIXME: Is having the same code for all of these ok ? */
			sparc_fdtoi (code, ins->sreg1, sparc_f0);
			sparc_stdf_imm (code, sparc_f0, sparc_sp, offset);
			sparc_ld_imm (code, sparc_sp, offset, ins->dreg);
			break;
		}
		case OP_FCONV_TO_I8:
		case OP_FCONV_TO_U8:
			/* Emulated */
			g_assert_not_reached ();
			break;
		case CEE_CONV_R_UN:
			/* Emulated */
			g_assert_not_reached ();
			break;
		case OP_LCONV_TO_R_UN: { 
			/* Emulated */
			g_assert_not_reached ();
			break;
		}
		case OP_LCONV_TO_OVF_I: {
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
			sparc_fnegs (code, ins->sreg1, ins->dreg);
			break;		
		case OP_FREM:
			sparc_fdivd (code, ins->sreg1, ins->sreg2, sparc_f0);
			sparc_fmuld (code, ins->sreg2, sparc_f0, sparc_f0);
			sparc_fsubd (code, ins->sreg1, sparc_f0, ins->dreg);
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
			sparc_patch ((guint8*)p, (guint8*)code);
			break;
		}
		case OP_FBLE: {
			/* cgt.un + brfalse */
			guint32 *p = code;
			sparc_fbranch (code, 1, sparc_fbug, 0);
			/* delay slot */
			sparc_nop (code);
			EMIT_FLOAT_COND_BRANCH (ins, sparc_fba, 1, 1);
			sparc_patch ((guint8*)p, (guint8*)code);
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
		case CEE_CKFINITE: {
			guint32 offset = mono_spillvar_offset_float (cfg, 0);
			if (!sparc_is_imm13 (offset))
				NOT_IMPLEMENTED;
			sparc_stdf_imm (code, ins->sreg1, sparc_sp, offset);
			sparc_lduh_imm (code, sparc_sp, offset, sparc_o7);
			sparc_srl_imm (code, sparc_o7, 4, sparc_o7);
			sparc_and_imm (code, FALSE, sparc_o7, 2047, sparc_o7);
			sparc_cmp_imm (code, sparc_o7, 2047);
			EMIT_COND_SYSTEM_EXCEPTION (ins, sparc_be, "ArithmeticException");
			sparc_fmovs (code, ins->sreg1, ins->dreg);
			sparc_fmovs (code, ins->sreg1 + 1, ins->dreg + 1);
			break;
		}
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
		
		ins = ins->next;
	}

	cfg->code_len = (guint8*)code - cfg->native_code;
}

void
mono_arch_register_lowlevel_calls (void)
{
	mono_register_jit_icall (mono_sparc_break, "mono_sparc_break", NULL, TRUE);
	mono_register_jit_icall (mono_arch_get_lmf_addr, "mono_arch_get_lmf_addr", NULL, TRUE);
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	/* FIXME: Move part of this to arch independent code */
	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target = NULL;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		switch (patch_info->type) {
		case MONO_PATCH_INFO_CLASS_INIT: {
			unsigned char *ip2 = ip;
			/* Might already been changed to a nop */
			sparc_call_simple (ip2, 0);
			break;
		}
		case MONO_PATCH_INFO_R4: {
			float *f = g_new0 (float, 1);
			*f = *(float*)patch_info->data.target;
			target = f;
			break;
		}
		case MONO_PATCH_INFO_R8: {
			double *d = g_new0 (double, 1);
			*d = *(double*)patch_info->data.target;
			target = d;			
			break;
		}
		default:
			break;
		}
		sparc_patch (ip, target);
	}
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
	*code = 256; /* max bytes needed: check this number */
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	int stack, code_size;
	guint32 *code = (guint32*)p;

	/* Save registers to stack */
	sparc_st_imm (code, sparc_i0, sparc_fp, 68);
	sparc_st_imm (code, sparc_i1, sparc_fp, 72);
	sparc_st_imm (code, sparc_i2, sparc_fp, 76);
	sparc_st_imm (code, sparc_i3, sparc_fp, 80);
	sparc_st_imm (code, sparc_i4, sparc_fp, 84);
	sparc_st_imm (code, sparc_i5, sparc_fp, 88);

	sparc_set (code, cfg->method, sparc_o0);
	sparc_mov_reg_reg (code, sparc_fp, sparc_o1);

	mono_add_patch_info (cfg, (guint8*)code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	sparc_sethi (code, 0, sparc_o7);
	sparc_jmpl_imm (code, sparc_o7, 0, sparc_callsite);
	sparc_nop (code);

	mono_arch_instrument_mem_needs (cfg->method, &stack, &code_size);

	g_assert ((code - (guint32*)p) <= (code_size * 4));

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
	int rtype = method->signature->ret->type;
	
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
		if (method->signature->ret->data.klass->enumtype) {
			rtype = method->signature->ret->data.klass->enum_basetype->type;
			goto handle_enum;
		}
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_ONE;
		break;
	}

	/* Save the result to the stack and also put it into the output registers */

	switch (save_mode) {
	case SAVE_TWO:
		sparc_st_imm (code, sparc_i0, sparc_fp, 68);
		sparc_st_imm (code, sparc_i0, sparc_fp, 72);
		sparc_mov_reg_reg (code, sparc_i0, sparc_o1);
		sparc_mov_reg_reg (code, sparc_i1, sparc_o2);
		break;
	case SAVE_ONE:
		sparc_st_imm (code, sparc_i0, sparc_fp, 68);
		sparc_mov_reg_reg (code, sparc_i0, sparc_o1);
		break;
	case SAVE_FP:
		sparc_stdf_imm (code, sparc_f0, sparc_fp, 72);
		sparc_ld_imm (code, sparc_fp, 72, sparc_o1);
		sparc_ld_imm (code, sparc_fp, 72, sparc_o2);
		break;
	case SAVE_STRUCT:
		sparc_ld_imm (code, sparc_fp, 64, sparc_o1);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	sparc_set (code, cfg->method, sparc_o0);

	mono_add_patch_info (cfg, (guint8*)code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	sparc_sethi (code, 0, sparc_o7);
	sparc_jmpl_imm (code, sparc_o7, 0, sparc_callsite);
	sparc_nop (code);

	/* Restore result */

	switch (save_mode) {
	case SAVE_TWO:
		sparc_ld_imm (code, sparc_fp, 68, sparc_i0);
		sparc_ld_imm (code, sparc_fp, 72, sparc_i0);
		break;
	case SAVE_ONE:
		sparc_ld_imm (code, sparc_fp, 68, sparc_i0);
		break;
	case SAVE_FP:
		sparc_lddf_imm (code, sparc_fp, 72, sparc_f0);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}

int
mono_arch_max_epilog_size (MonoCompile *cfg)
{
	int exc_count = 0, max_epilog_size = 16 + 20*4;
	MonoJumpInfo *patch_info;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	/* count the number of exception infos */
     
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			exc_count++;
	}

	/* 
	 * make sure we have enough space for exceptions
	 */
	max_epilog_size += exc_count * 24;

	return max_epilog_size;
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *inst;
	guint8 *code;
	CallInfo *cinfo;
	guint32 i, offset;

	cfg->code_size = 256;
	code = cfg->native_code = g_malloc (cfg->code_size);

	/* FIXME: Generate intermediate code instead */

	offset = cfg->stack_offset;
	offset += 64; /* register save area */
	offset += 4; /* struct/union return pointer */

	/* add parameter area size for called functions */
	if (cfg->param_area < 24)
		/* Reserve space for the first 6 arguments even if it is unused */
		offset += 24;
	else
		offset += cfg->param_area;
	
	/* align the stack size to 8 bytes */
	offset = ALIGN_TO (offset, MONO_ARCH_FRAME_ALIGNMENT);

	/*
	 * localloc'd memory is stored between the local variables (whose
	 * size is given by cfg->stack_offset), and between the space reserved
	 * by the ABI.
	 */
	cfg->arch.localloc_offset = offset - cfg->stack_offset;

	cfg->stack_offset = offset;

	if (!sparc_is_imm13 (- cfg->stack_offset)) {
		/* Can't use sparc_o7 here, since we're still in the caller's frame */
		sparc_set (code, (- cfg->stack_offset), sparc_g1);
		sparc_save (code, sparc_sp, sparc_g1, sparc_sp);
	}
	else
		sparc_save_imm (code, sparc_sp, - cfg->stack_offset, sparc_sp);

	if (strstr (cfg->method->name, "test_marshal_struct")) {
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_ABS, mono_sparc_break);
		sparc_call_simple (code, 0);
		sparc_nop (code);
	}

	sig = method->signature;

	cinfo = get_call_info (sig, FALSE);

	/* Keep in sync with emit_load_volatile_arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		guint32 stack_offset;
		MonoType *arg_type;
		inst = cfg->varinfo [i];

		if (sig->hasthis && (i == 0))
			arg_type = mono_defaults.object_class;
		else
			arg_type = sig->params [i - sig->hasthis];

		stack_offset = ainfo->offset + 68;

		/* Save the split arguments so they will reside entirely on the stack */
		if (ainfo->storage == ArgInSplitRegStack) {
			/* Save the register to the stack */
			g_assert (inst->opcode == OP_REGOFFSET);
			if (!sparc_is_imm13 (stack_offset))
				NOT_IMPLEMENTED;
			sparc_st_imm (code, sparc_i5, inst->inst_basereg, stack_offset);
		}

		if (!arg_type->byref && (arg_type->type == MONO_TYPE_R8)) {
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
					if (stack_offset != inst->inst_offset) {
						/* stack_offset is not dword aligned, so we need to make a copy */
						sparc_st_imm (code, sparc_i5, inst->inst_basereg, inst->inst_offset);
						sparc_ld_imm (code, sparc_fp, stack_offset + 4, sparc_o7);
						sparc_st_imm (code, sparc_o7, inst->inst_basereg, inst->inst_offset + 4);
					}
				}
			else
				if (ainfo->storage == ArgOnStackPair) {
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
				if (stack_offset & 0x1)
					sparc_stb_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
				else
					if (stack_offset & 0x2)
						sparc_sth_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
				else
					sparc_st_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, stack_offset);
			}
		else
			if ((ainfo->storage == ArgInIRegPair) && (inst->opcode != OP_REGVAR)) {
				/* Argument in regpair, but need to be saved to stack */
				if (!sparc_is_imm13 (inst->inst_offset + 4))
					NOT_IMPLEMENTED;
				sparc_st_imm (code, sparc_i0 + ainfo->reg, inst->inst_basereg, inst->inst_offset);
				sparc_st_imm (code, sparc_i0 + ainfo->reg + 1, inst->inst_basereg, inst->inst_offset + 4);				
			}

		if ((ainfo->storage == ArgInSplitRegStack) || (ainfo->storage == ArgOnStack))
			if (inst->opcode == OP_REGVAR)
				/* FIXME: Load the argument into memory */
				NOT_IMPLEMENTED;
	}

	g_free (cinfo);

	if (cfg->method->save_lmf) {
		gint32 lmf_offset = - cfg->arch.lmf_offset;

		/* Save ip */
		mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		sparc_set (code, 0xfffffff, sparc_o7);
		sparc_st_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ip));
		/* Save sp */
		sparc_st_imm (code, sparc_sp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, sp));
		/* Save fp */
		sparc_st_imm (code, sparc_fp, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, ebp));
		/* Save method */
		/* FIXME: add a relocation for this */
		sparc_set (code, cfg->method, sparc_o7);
		sparc_st_imm (code, sparc_o7, sparc_fp, lmf_offset + G_STRUCT_OFFSET (MonoLMF, method));

		mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
							 (gpointer)"mono_arch_get_lmf_addr");		
		sparc_call_simple (code, 0);
		sparc_nop (code);

		code = (guint32*)mono_sparc_emit_save_lmf ((guint32*)code, lmf_offset);
	}

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len <= cfg->code_size);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	MonoMethod *method = cfg->method;
	guint8 *code;

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);

	if (cfg->method->save_lmf) {
		gint32 lmf_offset = - cfg->arch.lmf_offset;

		code = mono_sparc_emit_restore_lmf (code, lmf_offset);
	}

	/* 
	 * The sparc ABI requires that calls to functions which return a structure
	 * return to %i7+12
	 */
	if (cfg->method->signature->pinvoke && MONO_TYPE_ISSTRUCT(cfg->method->signature->ret))
		sparc_jmpl_imm (code, sparc_i7, 12, sparc_g0);
	else
		sparc_ret (code);
	sparc_restore_imm (code, sparc_g0, 0, sparc_g0);

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC:
			sparc_patch (cfg->native_code + patch_info->ip.i, code);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC_NAME, patch_info->data.target);
			sparc_set (code, 0xffffff, sparc_o0);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_METHOD_REL, (gpointer)patch_info->ip.i);
			sparc_set (code, 0xffffff, sparc_o1);
			patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "mono_arch_throw_exception_by_name";
			patch_info->ip.i = code - cfg->native_code;
			sparc_call_simple (code, 0);
			sparc_nop (code);
			break;
		default:
			/* do nothing */
			break;
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

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

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
#ifdef __linux__
	struct sigaltstack sa;
#else
	stack_t         sigstk;
#endif
 
	printf ("SIGALT!\n");
	/* Setup an alternate signal stack */
	tls->signal_stack = g_malloc (SIGNAL_STACK_SIZE);
	tls->signal_stack_size = SIGNAL_STACK_SIZE;

#ifdef __linux__
	sa.ss_sp = tls->signal_stack;
	sa.ss_size = SIGNAL_STACK_SIZE;
	sa.ss_flags = 0;
	g_assert (sigaltstack (&sa, NULL) == 0);
#else
	sigstk.ss_sp = tls->signal_stack;
	sigstk.ss_size = SIGNAL_STACK_SIZE;
	sigstk.ss_flags = 0;
	g_assert (sigaltstack (&sigstk, NULL) == 0);
#endif
#endif

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

#ifdef MONO_SPARC_THR_TLS
	thr_setspecific (lmf_addr_key, &tls->lmf);
#else
	pthread_setspecific (lmf_addr_key, &tls->lmf);
#endif
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this;
		MONO_INST_NEW (cfg, this, OP_SETREG);
		this->type = this_type;
		this->sreg1 = this_reg;
		this->dreg = sparc_o0;
		mono_bblock_add_inst (cfg->cbb, this);
	}

	if (vt_reg != -1) {
		/* Set the 'struct/union return pointer' location on the stack */
		MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, sparc_sp, 64, vt_reg);
	}
}


gint
mono_arch_get_opcode_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	return -1;
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

	cinfo = get_call_info (csig, FALSE);

	if (csig->hasthis) {
		ainfo = &cinfo->args [0];
		arg_info [0].offset = 68 + ainfo->offset;
	}

	for (k = 0; k < param_count; k++) {
		ainfo = &cinfo->args [k + csig->hasthis];

		arg_info [k + 1].offset = 68 + ainfo->offset;
		arg_info [k + 1].size = mono_type_size (csig->params [k], &align);
	}

	g_free (cinfo);

	/* FIXME: */
	return 0;
}

gboolean
mono_arch_print_tree (MonoInst *tree, int arity)
{
	return 0;
}
