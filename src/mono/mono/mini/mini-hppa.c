/*
 * mini-hppa.c: HPPA backend for the Mono code generator
 *
 * Copyright (c) 2007 Randolph Chung
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 */
#include "mini.h"
#include <string.h>
#include <pthread.h>
#include <unistd.h>

#include <unistd.h>
#include <sys/mman.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/tokentype.h>
#include <mono/utils/mono-math.h>

#include "mini-hppa.h"
#include "trace.h"
#include "cpu-hppa.h"

#define ALIGN_TO(val,align) (((val) + ((align) - 1)) & ~((align) - 1))
#define SIGNAL_STACK_SIZE (64 * 1024)

#define DEBUG(a) // a
#define DEBUG_FUNC_ENTER() // printf("Entering %s\n", __FUNCTION__)
#define DEBUG_FUNC_EXIT() // printf("Exiting %s\n", __FUNCTION__)

static const guchar 
branch_b0_table [] = {
	TRUE, /* OP_HPPA_BEQ */
	FALSE, /* OP_HPPA_BGE */
	FALSE, /* OP_HPPA_BGT */
	TRUE, /* OP_HPPA_BLE */
	TRUE, /* OP_HPPA_BLT */
	FALSE, /* OP_HPPA_BNE */
	FALSE, /* OP_HPPA_BGE_UN */
	FALSE, /* OP_HPPA_BGT_UN */
	TRUE, /* OP_HPPA_BLE_UN */
	TRUE, /* OP_HPPA_BLT_UN */
};

static const guchar 
branch_b1_table [] = {
	HPPA_CMP_COND_EQ, /* OP_HPPA_BEQ */
	HPPA_CMP_COND_SLT, /* OP_HPPA_BGE */
	HPPA_CMP_COND_SLE, /* OP_HPPA_BGT */
	HPPA_CMP_COND_SLE, /* OP_HPPA_BLE */
	HPPA_CMP_COND_SLT, /* OP_HPPA_BLT */
	HPPA_CMP_COND_EQ, /* OP_HPPA_BNE_UN */
	HPPA_CMP_COND_ULT, /* OP_HPPA_BGE_UN */
	HPPA_CMP_COND_ULE, /* OP_HPPA_BGT_UN */
	HPPA_CMP_COND_ULE, /* OP_HPPA_BLE_UN */
	HPPA_CMP_COND_ULT, /* OP_HPPA_BLT_UN */
};

/* Note that these are inverted from the OP_xxx, because we nullify
 * the branch if the condition is met
 */
static const guchar
float_branch_table [] = {
	26, /* OP_FBEQ */
	11, /* OP_FBGE */
	15, /* OP_FBGT */
	19, /* OP_FBLE */
	23, /* OP_FBLT */
	4, /* OP_FBNE_UN */
	8, /* OP_FBGE_UN */
	13, /* OP_FBGT_UN */
	17, /* OP_FBLE_UN */
	20, /* OP_FBLT_UN */
};

static const guchar
float_ceq_table [] = {
	26, /* OP_FCEQ */
	15, /* OP_FCGT */
	13, /* OP_FCGT_UN */
	23, /* OP_FCLT */
	21, /* OP_FCLT_UN */
};

/*
 * Branches have short (14 or 17 bit) targets on HPPA. To make longer jumps,
 * we will need to rely on stubs - basically we create stub structures in
 * the epilogue that uses a long branch to the destination, and any short
 * jumps inside a method that cannot reach the destination directly will
 * branch first to the stub.
 */
typedef struct MonoOvfJump {
	union {
		MonoBasicBlock *bb;
		const char *exception;
	} data;
	guint32 ip_offset;
} MonoOvfJump;

/* Create a literal 0.0 double for FNEG */
double hppa_zero = 0;

const char*
mono_arch_regname (int reg) 
{
	static const char * rnames[] = {
		"hppa_r0", "hppa_r1", "hppa_rp", "hppa_r3", "hppa_r4",
		"hppa_r5", "hppa_r6", "hppa_r7", "hppa_r8", "hppa_r9",
		"hppa_r10", "hppa_r11", "hppa_r12", "hppa_r13", "hppa_r14",
		"hppa_r15", "hppa_r16", "hppa_r17", "hppa_r18", "hppa_r19",
		"hppa_r20", "hppa_r21", "hppa_r22", "hppa_r23", "hppa_r24",
		"hppa_r25", "hppa_r26", "hppa_r27", "hppa_r28", "hppa_r29",
		"hppa_sp", "hppa_r31"
	};
	if (reg >= 0 && reg < MONO_MAX_IREGS)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg) 
{
	static const char *rnames [] = {
		"hppa_fr0", "hppa_fr1", "hppa_fr2", "hppa_fr3", "hppa_fr4", 
		"hppa_fr5", "hppa_fr6", "hppa_fr7", "hppa_fr8", "hppa_fr9",
		"hppa_fr10", "hppa_fr11", "hppa_fr12", "hppa_fr13", "hppa_fr14", 
		"hppa_fr15", "hppa_fr16", "hppa_fr17", "hppa_fr18", "hppa_fr19",
		"hppa_fr20", "hppa_fr21", "hppa_fr22", "hppa_fr23", "hppa_fr24", 
		"hppa_fr25", "hppa_fr26", "hppa_fr27", "hppa_fr28", "hppa_fr29",
		"hppa_fr30", "hppa_fr31",
	};

	if (reg >= 0 && reg < MONO_MAX_FREGS)
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
	guint32 dummy;
	mono_arch_cpu_optimizations(&dummy);
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
mono_arch_cpu_optimizations (guint32 *exclude_mask)
{
	guint32 opts = 0;
	*exclude_mask = 0;
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

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	guint8* p = (guint8*)((guint32)code & ~(0x3f));
	guint8* end = (guint8*)((guint32)code + size);
	while (p < end) {
		__asm__ __volatile__ ("fdc %%r0(%%sr3, %0)\n"
			"sync\n"
			"fic %%r0(%%sr3, %0)\n"
			"sync\n"
			: : "r"(p));
		p += 32; /* can be 64 on pa20 cpus */
	}
}

void
mono_arch_flush_register_windows (void)
{
	/* No register windows on hppa */
}

typedef enum {
	ArgInIReg,
	ArgInIRegPair,
	ArgInFReg,
	ArgInDReg,
	ArgOnStack,
} ArgStorage;

typedef struct {
	gint16 offset;
	gint16 size;
	guint8 type;
	gint8  reg;
	ArgStorage storage;
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	int struct_return;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define PARAM_REGS 4
#define ARGS_OFFSET 36

static void
add_parameter (CallInfo *cinfo, ArgInfo *ainfo, MonoType *type)
{
	int is_fp = (type->type == MONO_TYPE_R4 || type->type == MONO_TYPE_R8);
	int ofs, align;

	DEBUG_FUNC_ENTER ();
	ainfo->reg = -1;
	ainfo->size = mono_type_size (type, &align);
	ainfo->type = type->type;

	if (ainfo->size <= 4) {
		cinfo->stack_usage += 4;
		ainfo->offset = cinfo->stack_usage - (4 - ainfo->size);
	}
	else if (ainfo->size <= 8)
	{
		cinfo->stack_usage += 8;
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, 8);
		ainfo->offset = cinfo->stack_usage - (8 - ainfo->size);
	}
	else
	{
		cinfo->stack_usage += ainfo->size;
		cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, align);
		ainfo->offset = cinfo->stack_usage;
	}

	ofs = (ALIGN_TO (ainfo->offset, 4) - ARGS_OFFSET) / 4;
	if (ofs < PARAM_REGS) {
		if (!is_fp) {
			if (ainfo->size <= 4)
				ainfo->storage = ArgInIReg;
			else
				ainfo->storage = ArgInIRegPair;
			ainfo->reg = hppa_r26 - ofs;
		} else if (type->type == MONO_TYPE_R4) {
			ainfo->storage = ArgInFReg;
			ainfo->reg = hppa_fr4 + ofs;
		} else { /* type->type == MONO_TYPE_R8 */
			ainfo->storage = ArgInDReg;
			ainfo->reg = hppa_fr4 + ofs;
		}
	}
	else {
		/* frame pointer based offset */
		ainfo->reg = hppa_r3;
		ainfo->storage = ArgOnStack;
	}

	/* All offsets are negative relative to the frame pointer */
	ainfo->offset = -ainfo->offset;

	DEBUG_FUNC_EXIT ();
}

static void 
analyze_return (CallInfo *cinfo, MonoMethodSignature *sig)
{
	MonoType *type;
	int align;
	int size;

	type = sig->ret;
	size = mono_type_size (type, &align);

	/* ref: mono_type_to_stind */
	cinfo->ret.type = type->type;
	if (type->byref) {
		cinfo->ret.storage = ArgInIReg;
		cinfo->ret.reg = hppa_r28;
	} else {
handle_enum:
		switch (type->type) {
		case MONO_TYPE_VOID:
			break;
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
		case MONO_TYPE_STRING:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_ARRAY:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = hppa_r28;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.storage = ArgInIRegPair;
			cinfo->ret.reg = hppa_r28;
			break;
		case MONO_TYPE_R4:
			cinfo->ret.storage = ArgInFReg;
			cinfo->ret.reg = hppa_fr4;
			break;
		case MONO_TYPE_R8:
			cinfo->ret.storage = ArgInDReg;
			cinfo->ret.reg = hppa_fr4;
			break;
		case MONO_TYPE_GENERICINST:
			type = &type->data.generic_class->container_class->byval_arg;
			goto handle_enum;
			
		case MONO_TYPE_VALUETYPE:
			if (type->data.klass->enumtype) {
				type = mono_class_enum_basetype (type->data.klass);
				goto handle_enum;
			}
			/* Fall through */
		case MONO_TYPE_TYPEDBYREF:
			cinfo->struct_return = 1;
			/* cinfo->ret.storage tells us how the ABI expects 
			 * the parameter to be returned
			 */
			if (size <= 4) {
				cinfo->ret.storage = ArgInIReg;
				cinfo->ret.reg = hppa_r28;
			} else if (size <= 8) {
				cinfo->ret.storage = ArgInIRegPair;
				cinfo->ret.reg = hppa_r28;
			} else {
				cinfo->ret.storage = ArgOnStack;
				cinfo->ret.reg = hppa_sp;
			}

			/* We always allocate stack space for this because the
			 * arch-indep code expects us to
			 */
			cinfo->stack_usage += size;
			cinfo->stack_usage = ALIGN_TO (cinfo->stack_usage, align);
			cinfo->ret.offset = -cinfo->stack_usage;
			break;

		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 */
static CallInfo*
get_call_info (MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i;
	int n = sig->hasthis + sig->param_count;
	CallInfo *cinfo;
	MonoType *type;
	MonoType ptrtype;
	int dummy;

	ptrtype.type = MONO_TYPE_PTR;

	DEBUG_FUNC_ENTER();
	cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	/* The area below ARGS_OFFSET is the linkage area... */
	cinfo->stack_usage = ARGS_OFFSET - 4;
	/* -4, because the first argument will allocate the area it needs */

	/* this */
	if (sig->hasthis) {
		add_parameter (cinfo, cinfo->args + 0, &ptrtype);
		DEBUG (printf ("param <this>: assigned to reg %s offset %d\n", mono_arch_regname (cinfo->args[0].reg), cinfo->args[0].offset));
	}

	/* TODO: What to do with varargs? */

	for (i = 0; i < sig->param_count; ++i) {
		ArgInfo *ainfo = &cinfo->args [sig->hasthis + i];
		if (sig->params [i]->byref)
			type = &ptrtype;
		else
			type = mono_type_get_underlying_type (sig->params [i]);
		add_parameter (cinfo, ainfo, type);

		DEBUG (printf ("param %d: type %d size %d assigned to reg %s offset %d\n", i, type->type, mono_type_size (type, &dummy), mono_arch_regname (ainfo->reg), ainfo->offset));
	}

	analyze_return (cinfo, sig);

	DEBUG_FUNC_EXIT();
	return cinfo;
}

GList *
mono_arch_get_allocatable_int_vars (MonoCompile *cfg)
{
	GList *vars = NULL;
	int i;

	DEBUG_FUNC_ENTER();
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
			vars = mono_varlist_insert_sorted (cfg, vars, vmv, FALSE);
		}
	}
	DEBUG_FUNC_EXIT();

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;
	int i;

	/* r3 is sometimes used as our frame pointer, so don't allocate it
	 * r19 is the GOT pointer, don't allocate it either
	 */

	DEBUG_FUNC_ENTER();
	for (i = 4; i <= 18; i++)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (i));
	DEBUG_FUNC_EXIT();

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
	/* FIXME */
	return 0;
}

/*
 * Set var information according to the calling convention.
 * The locals var stuff should most likely be split in another method.
 *
 * updates m->stack_offset based on the amount of stack space needed for
 * local vars
 */
void
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;
	guint32 stack_ptr;
	guint rettype;
	CallInfo *cinfo;

	DEBUG_FUNC_ENTER();
	m->flags |= MONO_CFG_HAS_SPILLUP;

	header = m->header;

	sig = mono_method_signature (m->method);
	DEBUG (printf ("Allocating locals - incoming params:\n"));
	cinfo = get_call_info (sig, FALSE);

	/*
	 * We use the ABI calling conventions for managed code as well.
	 */
	if (m->flags & MONO_CFG_HAS_ALLOCA) {
		stack_ptr = hppa_r4;
		m->used_int_regs |= 1 << hppa_r4;
	} else {
		stack_ptr = hppa_sp;
	}

	/* Before this function is called, we would have looked at all 
	 * calls from this method and figured out how much space is needed
	 * for the param area.
	 *
	 * Locals are allocated backwards, right before the param area 
	 */
	/* TODO: in some cases we don't need the frame pointer... */
	m->frame_reg = hppa_r3;
	offset = m->param_area;

	/* Return values can be passed back either in four ways:
	 * r28 is used for data <= 4 bytes (32-bit ABI)
	 * r28/r29 are used for data >4 && <= 8 bytes
	 * fr4 is used for floating point data
	 * data larger than 8 bytes is returned on the stack pointed to 
	 * 	by r28
	 *
	 * This code needs to be in sync with how CEE_RET is handled
	 * in mono_method_to_ir (). In some cases when we return small
	 * structs, the ABI specifies that they should be returned in
	 * registers, but the code in mono_method_to_ir () always emits
	 * a memcpy for valuetype returns, so we need to make sure we
	 * allocate space on the stack for this copy.
	 */
	if (cinfo->struct_return) {
		/* this is used to stash the incoming r28 pointer */
		offset += sizeof (gpointer);
		m->ret->opcode = OP_REGOFFSET;
		m->ret->inst_basereg = stack_ptr;
		m->ret->inst_offset = -offset;
	} else if (sig->ret->type != MONO_TYPE_VOID) {
		m->ret->opcode = OP_REGVAR;
		m->ret->inst_c0 = cinfo->ret.reg;
	}

	curinst = m->locals_start;
	for (i = curinst; i < m->num_varinfo; ++i) {
		inst = m->varinfo [i];

		if (inst->opcode == OP_REGVAR) {
			DEBUG (printf ("allocating local %d to %s\n", i, mono_arch_regname (inst->dreg)));
			continue;
		}

		if (inst->flags & MONO_INST_IS_DEAD)
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
			size = mono_class_native_size (inst->inst_vtype->data.klass, &align);
		else
			size = mini_type_stack_size (cfg->generic_sharing_context, inst->inst_vtype, &align);

		/* 
		 * This is needed since structures containing doubles must be doubleword 
         * aligned.
		 * FIXME: Do this only if needed.
		 */
		if (MONO_TYPE_ISSTRUCT (inst->inst_vtype))
			align = 8;

		/*
		 * variables are accessed as negative offsets from hppa_sp
		 */
		inst->opcode = OP_REGOFFSET;
		inst->inst_basereg = stack_ptr;
		offset += size;
		offset = ALIGN_TO (offset, align);
		inst->inst_offset = -offset;

		DEBUG (printf ("allocating local %d (size = %d) to [%s - %d]\n", i, size, mono_arch_regname (inst->inst_basereg), -inst->inst_offset));
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		/* TODO */
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = &cinfo->args [i];
		inst = m->args [i];
		if (inst->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInIRegPair:
			case ArgInFReg:
			case ArgInDReg:
				/* Currently mono requests all incoming registers
				 * be assigned to a stack location :-(
				 */
#if 0
				if (!(inst->flags & (MONO_INST_VOLATILE | MONO_INST_INDIRECT))) {
					inst->opcode = OP_REGVAR;
					inst->dreg = ainfo->reg;
					DEBUG (printf ("param %d in register %s\n", i, mono_arch_regname (inst->dreg)));
					break;
				}
#endif
				/* fallthrough */
			case ArgOnStack:
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = hppa_r3;
				inst->inst_offset = ainfo->offset;
				DEBUG (printf ("param %d stored on stack [%s - %d]\n", i, mono_arch_regname (hppa_r3), -inst->inst_offset));
				break;
			}
		}
	}

	m->stack_offset = offset; /* Includes cfg->param_area */

	g_free (cinfo);
	DEBUG_FUNC_EXIT();
}

/* 
 * take the arguments and generate the arch-specific
 * instructions to properly call the function in call.
 * This includes pushing, moving arguments to the right register
 * etc.
 *
 * sets call->stack_usage and cfg->param_area
 */
MonoCallInst*
mono_arch_call_opcode (MonoCompile *cfg, MonoBasicBlock* bb, MonoCallInst *call, int is_virtual) 
{
	MonoInst *arg, *in;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	DEBUG_FUNC_ENTER();
	DEBUG (printf ("is_virtual = %d\n", is_virtual));

	sig = call->signature;
	n = sig->param_count + sig->hasthis;

	DEBUG (printf ("Calling method with %d parameters\n", n));
	
	cinfo = get_call_info (sig, sig->pinvoke);

	// DEBUG
	g_assert (sig->call_convention != MONO_CALL_VARARG);

	for (i = 0; i < n; ++i) {
		ainfo = &cinfo->args [i];

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* TODO */
		}

		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instruction */
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

			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInIRegPair: {
				MonoHPPAArgInfo *ai = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoHPPAArgInfo));
				ai->reg = ainfo->reg;
				ai->size = ainfo->size;
				ai->offset = ainfo->offset;
				ai->pass_in_reg = 1;
				arg->backend.data = ai;

				call->used_iregs |= 1 << ainfo->reg;
				if (ainfo->storage == ArgInIRegPair)
					call->used_iregs |= 1 << (ainfo->reg + 1);
				if (ainfo->type == MONO_TYPE_VALUETYPE)
					arg->opcode = OP_OUTARG_VT;
				break;
			}
			case ArgOnStack: {
				MonoHPPAArgInfo *ai = mono_mempool_alloc0 (cfg->mempool, sizeof (MonoHPPAArgInfo));
				ai->reg = hppa_sp;
				ai->size = ainfo->size;
				ai->offset = ainfo->offset;
				ai->pass_in_reg = 0;
				arg->backend.data = ai;
				if (ainfo->type == MONO_TYPE_VALUETYPE)
					arg->opcode = OP_OUTARG_VT;
				else
					arg->opcode = OP_OUTARG_MEMBASE;
				call->used_iregs |= 1 << ainfo->reg;
				break;
			}
			case ArgInFReg:
				arg->backend.reg3 = ainfo->reg;
				arg->opcode = OP_OUTARG_R4;
				call->used_fregs |= 1 << ainfo->reg;
				break;
			case ArgInDReg:
				arg->backend.reg3 = ainfo->reg;
				arg->opcode = OP_OUTARG_R8;
				call->used_fregs |= 1 << ainfo->reg;
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
	call->stack_usage = cinfo->stack_usage;
	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->param_area = ALIGN_TO (cfg->param_area, MONO_ARCH_FRAME_ALIGNMENT);

	cfg->flags |= MONO_CFG_HAS_CALLS;

	g_free (cinfo);

	DEBUG_FUNC_EXIT();
	return call;
}

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	DEBUG_FUNC_ENTER();
	DEBUG_FUNC_EXIT();
}

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

/*
 * Remove from the instruction list the instructions that can't be
 * represented with very simple instructions with no register
 * requirements.
 */
void
mono_arch_lowering_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *next, *temp, *last_ins = NULL;
	int imm;

	MONO_BB_FOR_EACH_INS (bb, ins) {
loop_start:
		switch (ins->opcode) {
		case OP_ADD_IMM:
		case OP_ADDCC_IMM:
			if (!hppa_check_bits (ins->inst_imm, 11)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
			}
			break;
		case OP_SUB_IMM:
		case OP_SUBCC_IMM:
			if (!hppa_check_bits (ins->inst_imm, 11)) {
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = map_to_reg_reg_op (ins->opcode);
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
			else {
				int tmp = mono_alloc_ireg (cfg);
				NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_c0;
				temp->dreg = tmp;

				ins->opcode = CEE_MUL;
				ins->sreg2 = tmp;
				/* Need to rewrite the CEE_MUL too... */
				goto loop_start;
			}
			break;

		case CEE_MUL: {
			int freg1 = mono_alloc_freg (cfg);
			int freg2 = mono_alloc_freg (cfg);

			NEW_INS(cfg, temp, OP_STORE_MEMBASE_REG);
			temp->sreg1 = ins->sreg1;
			temp->inst_destbasereg = hppa_sp;
			temp->inst_offset = -16;

			NEW_INS(cfg, temp, OP_LOADR4_MEMBASE);
			temp->dreg = freg1;
			temp->inst_basereg = hppa_sp;
			temp->inst_offset = -16;

			NEW_INS(cfg, temp, OP_STORE_MEMBASE_REG);
			temp->sreg1 = ins->sreg2;
			temp->inst_destbasereg = hppa_sp;
			temp->inst_offset = -16;

			NEW_INS(cfg, temp, OP_LOADR4_MEMBASE);
			temp->dreg = freg2;
			temp->inst_basereg = hppa_sp;
			temp->inst_offset = -16;

			NEW_INS (cfg, temp, OP_HPPA_XMPYU);
			temp->dreg = freg2;
			temp->sreg1 = freg1;
			temp->sreg2 = freg2;

			NEW_INS(cfg, temp, OP_HPPA_STORER4_RIGHT);
			temp->sreg1 = freg2;
			temp->inst_destbasereg = hppa_sp;
			temp->inst_offset = -16;

			ins->opcode = OP_LOAD_MEMBASE;
			ins->inst_basereg = hppa_sp;
			ins->inst_offset = -16;
		}
		break;

		default:
			break;
		}
		last_ins = ins;
	}
	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;
	
}

void
hppa_patch (guint32 *code, const gpointer target)
{
	guint32 ins = *code;
	gint32 val = (gint32)target;
	gint32 disp = (val - (gint32)code - 8) >> 2;
	int reg1, reg2;

	DEBUG (printf ("patching 0x%08x (0x%08x) to point to 0x%08x (disp = %d)\n", code, ins, val, disp));

	switch (*code >> 26) {
	case 0x08: /* ldil, next insn can be a ldo, ldw, or ble */
		*code = *code & ~0x1fffff;
		*code = *code | hppa_op_imm21 (hppa_lsel (val));
		code++;

		if ((*code >> 26) == 0x0D) { /* ldo */
			*code = *code & ~0x3fff;
			*code = *code | hppa_op_imm14 (hppa_rsel (val));
		} else if ((*code >> 26) == 0x12) { /* ldw */
			*code = *code & ~0x3fff;
			*code = *code | hppa_op_imm14 (hppa_rsel (val));
		} else if ((*code >> 26) == 0x39) { /* ble */
			*code = *code & ~0x1f1ffd;
			*code = *code | hppa_op_imm17 (hppa_rsel (val));
		}

		break;

	case 0x3A: /* bl */
		if (disp == 0) {
			hppa_nop (code);
			break;
		}
		if (!hppa_check_bits (disp, 17)) 
			goto jump_overflow;
		reg1 = (*code >> 21) & 0x1f;
		*code = (*code & ~0x1f1ffd) | hppa_op_imm17(disp);
		break;

	case 0x20: /* combt */
	case 0x22: /* combf */
		if (!hppa_check_bits (disp >> 2, 12))
			goto jump_overflow;
		*code = (*code & ~0x1ffd) | hppa_op_imm12(disp);
		break;

	default:
		g_warning ("Unpatched opcode %x\n", *code >> 26);
	}

	return;

jump_overflow:
	g_warning ("cannot branch to target, insn is %08x, displacement is %d\n", (int)*code, (int)disp);
	g_assert_not_reached ();
}

static guint32 *
emit_float_to_int (MonoCompile *cfg, guint32 *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg. */
	hppa_fcnvfxt (code, HPPA_FP_FMT_DBL, HPPA_FP_FMT_SGL, sreg, sreg);
	hppa_fstws (code, sreg, 0, -16, hppa_sp);
	hppa_ldw (code, -16, hppa_sp, dreg);
	if (!is_signed) {
		if (size == 1)
			hppa_extru (code, dreg, 31, 8, dreg);
		else if (size == 2)
			hppa_extru (code, dreg, 31, 16, dreg);
	} else {
		if (size == 1)
			hppa_extrs (code, dreg, 31, 8, dreg);
		else if (size == 2)
			hppa_extrs (code, dreg, 31, 16, dreg);
	}
	return code;
}

/* Clobbers r1, r20, r21 */
static guint32 *
emit_memcpy (guint32 *code, int doff, int dreg, int soff, int sreg, int size)
{
	/* r20 is the destination */
	hppa_set (code, doff, hppa_r20);
	hppa_add (code, hppa_r20, dreg, hppa_r20);

	/* r21 is the source */
	hppa_set (code, soff, hppa_r21);
	hppa_add (code, hppa_r21, sreg, hppa_r21);

	while (size >= 4) {
		hppa_ldw (code, 0, hppa_r21, hppa_r1);
		hppa_stw (code, hppa_r1, 0, hppa_r20);
		hppa_ldo (code, 4, hppa_r21, hppa_r21);
		hppa_ldo (code, 4, hppa_r20, hppa_r20);
		size -= 4;
	}
	while (size >= 2) {
		hppa_ldh (code, 0, hppa_r21, hppa_r1);
		hppa_sth (code, hppa_r1, 0, hppa_r20);
		hppa_ldo (code, 2, hppa_r21, hppa_r21);
		hppa_ldo (code, 2, hppa_r20, hppa_r20);
		size -= 2;
	}
	while (size > 0) {
		hppa_ldb (code, 0, hppa_r21, hppa_r1);
		hppa_stb (code, hppa_r1, 0, hppa_r20);
		hppa_ldo (code, 1, hppa_r21, hppa_r21);
		hppa_ldo (code, 1, hppa_r20, hppa_r20);
		size -= 1;
	}

	return code;
}

/*
 * mono_arch_get_vcall_slot_addr:
 *
 *  Determine the vtable slot used by a virtual call.
 */
gpointer*
mono_arch_get_vcall_slot_addr (guint8 *code8, mgreg_t *regs)
{
	guint32 *code = (guint32*)((unsigned long)code8 & ~3);

	DEBUG_FUNC_ENTER();

	code -= 2;
	/* This is the special virtual call token */
	if (code [-1] != 0x34000eee) /* ldo 0x777(r0),r0 */
		return NULL;

	if ((code [0] >> 26) == 0x39 &&		/* ble */
	    (code [-2] >> 26) == 0x12) {	/* ldw */
		guint32 ldw = code [-2];
		guint32 reg = (ldw >> 21) & 0x1f;
		gint32 disp = ((ldw & 1) ? (-1 << 13) : 0) | ((ldw & 0x3fff) >> 1);
		/* FIXME: we are not guaranteed that reg is saved in the LMF.
		 * In fact, it probably isn't, since it is allocated as a
		 * callee register.  Right now just return an address; this 
		 * is sufficient for non-AOT operation
		 */
		// return (gpointer)((guint8*)regs [reg] + disp);
		return code;
	}
	else
		g_assert_not_reached ();

	DEBUG_FUNC_EXIT();
}

/* ins->dreg = *(ins->inst_desgbasereg + ins->inst_offset) */
#define EMIT_LOAD_MEMBASE(ins, op) do {				\
	if (!hppa_check_bits (ins->inst_offset, 14)) {		\
		hppa_set (code, ins->inst_offset, hppa_r1);	\
		hppa_ ## op ## x (code, hppa_r1, ins->inst_basereg, ins->dreg); \
	}							\
	else {							\
		hppa_ ## op (code, ins->inst_offset, ins->inst_basereg, ins->dreg); \
	}							\
} while (0)

#define EMIT_COND_BRANCH_FLAGS(ins,r1,r2,b0,b1) do {\
	if (b0) \
		hppa_combf (code, r1, r2, b1, 2); \
	else \
		hppa_combt (code, r1, r2, b1, 2); \
	hppa_nop (code); \
	mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	hppa_bl (code, 0, hppa_r0); \
	hppa_nop (code); \
} while (0)

#define EMIT_COND_BRANCH(ins,r1,r2,cond) EMIT_COND_BRANCH_FLAGS(ins, r1, r2, branch_b0_table [(cond)], branch_b1_table [(cond)])

#define EMIT_FLOAT_COND_BRANCH_FLAGS(ins,r1,r2,b0) do {\
	hppa_fcmp (code, HPPA_FP_FMT_DBL, b0, r1, r2); \
	hppa_ftest (code, 0); \
	mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	hppa_bl (code, 8, hppa_r0); \
	hppa_nop (code); \
} while (0)

#define EMIT_FLOAT_COND_BRANCH(ins,r1,r2,cond) EMIT_FLOAT_COND_BRANCH_FLAGS(ins, r1, r2, float_branch_table [cond])

#define EMIT_COND_SYSTEM_EXCEPTION_FLAGS(r1,r2,b0,b1,exc_name)		\
do {									\
	MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump)); \
	ovfj->data.exception = (exc_name);			\
	ovfj->ip_offset = (guint8*)code - cfg->native_code;	\
	hppa_bl (code, 8, hppa_r2);				\
	hppa_depi (code, 0, 31, 2, hppa_r2);			\
	hppa_ldo (code, 8, hppa_r2, hppa_r2);			\
	if (b0)							\
		hppa_combf (code, r1, r2, b1, 2);		\
	else							\
		hppa_combt (code, r1, r2, b1, 2);		\
	hppa_nop (code);					\
	mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj); \
	hppa_bl (code, 0, hppa_r0);				\
	hppa_nop (code);					\
} while (0)

#define EMIT_COND_SYSTEM_EXCEPTION(r1,r2,cond,exc_name) EMIT_COND_SYSTEM_EXCEPTION_FLAGS(r1, r2, branch_b0_table [(cond)], branch_b1_table [(cond)], (exc_name))

/* TODO: MEM_INDEX_REG - cannot be r1 */
#define MEM_INDEX_REG hppa_r31
/* *(ins->inst_destbasereg + ins->inst_offset) = ins->inst_imm */
#define EMIT_STORE_MEMBASE_IMM(ins, op) do {			\
	guint32 sreg;						\
	if (ins->inst_imm == 0)					\
		sreg = hppa_r0;					\
	else {							\
		hppa_set (code, ins->inst_imm, hppa_r1);	\
		sreg = hppa_r1;					\
	}							\
	if (!hppa_check_bits (ins->inst_offset, 14)) {		\
		hppa_set (code, ins->inst_offset, MEM_INDEX_REG); \
		hppa_addl (code, ins->inst_destbasereg, MEM_INDEX_REG, MEM_INDEX_REG); \
		hppa_ ## op (code, sreg, 0, MEM_INDEX_REG);	\
	}							\
	else {							\
		hppa_ ## op (code, sreg, ins->inst_offset, ins->inst_destbasereg); \
	}							\
} while (0)

/* *(ins->inst_destbasereg + ins->inst_offset) = ins->sreg1 */
#define EMIT_STORE_MEMBASE_REG(ins, op) do {			\
	if (!hppa_check_bits (ins->inst_offset, 14)) {		\
		hppa_set (code, ins->inst_offset, MEM_INDEX_REG); \
		hppa_addl (code, ins->inst_destbasereg, MEM_INDEX_REG, MEM_INDEX_REG); \
		hppa_ ## op (code, ins->sreg1, 0, MEM_INDEX_REG);	\
	}							\
	else {							\
		hppa_ ## op (code, ins->sreg1, ins->inst_offset, ins->inst_destbasereg); \
	}							\
} while (0)

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

	DEBUG_FUNC_ENTER();

	if (cfg->verbose_level > 2)
		g_print ("[%s::%s] Basic block %d starting at offset 0x%x\n", cfg->method->klass->name, cfg->method->name, bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		NOT_IMPLEMENTED;
	}

	MONO_BB_FOR_EACH_INS (bb, ins) {
		guint8* code_start;

		offset = (guint8*)code - cfg->native_code;

		spec = ins_get_spec (ins->opcode);

		max_len = ((guint8 *)spec) [MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = (guint32*)(cfg->native_code + offset);
			cfg->stat_code_reallocs++;
		}
		code_start = (guint8*)code;
		//	if (ins->cil_code)
		//		g_print ("cil code\n");
		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_RELAXED_NOP:
			break;
		case OP_STOREI1_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, stb);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, sth);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			EMIT_STORE_MEMBASE_IMM (ins, stw);
			break;
		case OP_STOREI1_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, stb);
			break;
		case OP_STOREI2_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, sth);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			EMIT_STORE_MEMBASE_REG (ins, stw);
			break;
		case OP_LOADU1_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldb);
			break;
		case OP_LOADI1_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldb);
			hppa_extrs (code, ins->dreg, 31, 8, ins->dreg);
			break;
		case OP_LOADU2_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldh);
			break;
		case OP_LOADI2_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldh);
			hppa_extrs (code, ins->dreg, 31, 16, ins->dreg);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			EMIT_LOAD_MEMBASE (ins, ldw);
			break;
		case CEE_CONV_I1:
			hppa_extrs (code, ins->sreg1, 31, 8, ins->dreg);
			break;
		case CEE_CONV_I2:
			hppa_extrs (code, ins->sreg1, 31, 16, ins->dreg);
			break;
		case CEE_CONV_U1:
			hppa_extru (code, ins->sreg1, 31, 8, ins->dreg);
			break;
		case CEE_CONV_U2:
			hppa_extru (code, ins->sreg1, 31, 16, ins->dreg);
			break;
		case CEE_CONV_U:
		case CEE_CONV_I4:
		case CEE_CONV_U4:
		case OP_MOVE:
			if (ins->sreg1 != ins->dreg)
				hppa_copy (code, ins->sreg1, ins->dreg);
			break;
		case OP_SETLRET:
			hppa_copy (code, ins->sreg1 + 1, ins->dreg);
			hppa_copy (code, ins->sreg1, ins->dreg + 1);
			break;

		case OP_BREAK:
			/* break 4,8 - this is what gdb normally uses... */
			*code++ = 0x00010004;
			break;
		case OP_ADDCC:
		case CEE_ADD:
			hppa_add (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ADC:
			hppa_addc (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_ADDCC_IMM:
		case OP_ADD_IMM:
			hppa_addi (code, ins->inst_imm, ins->sreg1, ins->dreg);
			break;
		case OP_ADC_IMM:
			hppa_set (code, ins->inst_imm, hppa_r1);
			hppa_addc (code, ins->sreg1, hppa_r1, ins->dreg);
			break;
		case OP_HPPA_ADD_OVF: {
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));
			hppa_bl (code, 8, hppa_r2);
			hppa_depi (code, 0, 31, 2, hppa_r2);
			hppa_ldo (code, 12, hppa_r2, hppa_r2);

			if (ins->backend.reg3 == CEE_ADD_OVF)
				hppa_add_cond (code, HPPA_ADD_COND_NSV, ins->sreg1, ins->sreg2, ins->dreg);
			else			
				hppa_add_cond (code, HPPA_ADD_COND_NUV, ins->sreg1, ins->sreg2, ins->dreg);

			ovfj->data.exception = "OverflowException";
			ovfj->ip_offset = (guint8*)code - cfg->native_code;
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj);
			hppa_bl_n (code, 8, hppa_r0);
			break;
		}
		case OP_HPPA_ADDC_OVF: {
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));
			hppa_bl (code, 8, hppa_r2);
			hppa_depi (code, 0, 31, 2, hppa_r2);
			hppa_ldo (code, 12, hppa_r2, hppa_r2);

			if (ins->backend.reg3 == OP_LADD_OVF)
				hppa_addc_cond (code, HPPA_ADD_COND_NSV, ins->sreg1, ins->sreg2, ins->dreg);
			else			
				hppa_addc_cond (code, HPPA_ADD_COND_NUV, ins->sreg1, ins->sreg2, ins->dreg);

			ovfj->data.exception = "OverflowException";
			ovfj->ip_offset = (guint8*)code - cfg->native_code;
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj);
			hppa_bl_n (code, 8, hppa_r0);
			break;
		}
		case OP_SUBCC:
		case CEE_SUB:
			hppa_sub (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SUBCC_IMM:
		case OP_SUB_IMM:
			hppa_addi (code, -ins->inst_imm, ins->sreg1, ins->dreg);
			break;
		case OP_SBB:
			hppa_subb (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_SBB_IMM:
			hppa_set (code, ins->inst_imm, hppa_r1);
			hppa_subb (code, ins->sreg1, hppa_r1, ins->dreg);
			break;
		case OP_HPPA_SUB_OVF: {
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));
			hppa_bl (code, 8, hppa_r2);
			hppa_depi (code, 0, 31, 2, hppa_r2);
			hppa_ldo (code, 12, hppa_r2, hppa_r2);
			hppa_sub_cond (code, HPPA_SUB_COND_NSV, ins->sreg1, ins->sreg2, ins->dreg);
			ovfj->data.exception = "OverflowException";
			ovfj->ip_offset = (guint8*)code - cfg->native_code;
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj);
			hppa_bl_n (code, 8, hppa_r0);
			break;
		}
		case OP_HPPA_SUBB_OVF: {
			MonoOvfJump *ovfj = mono_mempool_alloc (cfg->mempool, sizeof (MonoOvfJump));
			hppa_bl (code, 8, hppa_r2);
			hppa_depi (code, 0, 31, 2, hppa_r2);
			hppa_ldo (code, 12, hppa_r2, hppa_r2);

			hppa_subb_cond (code, HPPA_SUB_COND_NSV, ins->sreg1, ins->sreg2, ins->dreg);
			ovfj->data.exception = "OverflowException";
			ovfj->ip_offset = (guint8*)code - cfg->native_code;
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_EXC_OVF, ovfj);
			hppa_bl_n (code, 8, hppa_r0);
			break;
		}

		case CEE_AND:
			hppa_and (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_AND_IMM:
			hppa_set (code, ins->inst_imm, hppa_r1);
			hppa_and (code, ins->sreg1, hppa_r1, ins->dreg);
			break;

		case CEE_OR:
			hppa_or (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;

		case OP_OR_IMM:
			hppa_set (code, ins->inst_imm, hppa_r1);
			hppa_or (code, ins->sreg1, hppa_r1, ins->dreg);
			break;

		case CEE_XOR:
			hppa_xor (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_XOR_IMM:
			hppa_set (code, ins->inst_imm, hppa_r1);
			hppa_xor (code, ins->sreg1, hppa_r1, ins->dreg);
			break;
		case CEE_SHL:
			if (ins->sreg1 != ins->dreg) {
				hppa_shl (code, ins->sreg1, ins->sreg2, ins->dreg);
			} 
			else {
				hppa_copy (code, ins->sreg1, hppa_r1);
				hppa_shl (code, hppa_r1, ins->sreg2, ins->dreg);
			}
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
			g_assert (ins->inst_imm < 32);
			if (ins->sreg1 != ins->dreg) {
				hppa_zdep (code, ins->sreg1, 31-ins->inst_imm, 32-ins->inst_imm, ins->dreg);
			} 
			else {
				hppa_copy (code, ins->sreg1, hppa_r1);
				hppa_zdep (code, hppa_r1, 31-ins->inst_imm, 32-ins->inst_imm, ins->dreg);
			}
			break;
		case CEE_SHR:
			if (ins->sreg1 != ins->dreg) {
				hppa_shr (code, ins->sreg1, ins->sreg2, ins->dreg);
			}
			else {
				hppa_copy (code, ins->sreg1, hppa_r1);
				hppa_shr (code, hppa_r1, ins->sreg2, ins->dreg);
			}
			break;
		case OP_SHR_IMM:
			g_assert (ins->inst_imm < 32);
			if (ins->sreg1 != ins->dreg) {
				hppa_extrs (code, ins->sreg1, 31-ins->inst_imm, 32-ins->inst_imm, ins->dreg);
			} 
			else {
				hppa_copy (code, ins->sreg1, hppa_r1);
				hppa_extrs (code, hppa_r1, 31-ins->inst_imm, 32-ins->inst_imm, ins->dreg);
			}
			break;
		case OP_SHR_UN_IMM:
			g_assert (ins->inst_imm < 32);
			if (ins->sreg1 != ins->dreg) {
				hppa_extru (code, ins->sreg1, 31-ins->inst_imm, 32-ins->inst_imm, ins->dreg);
			} 
			else {
				hppa_copy (code, ins->sreg1, hppa_r1);
				hppa_extru (code, hppa_r1, 31-ins->inst_imm, 32-ins->inst_imm, ins->dreg);
			}
			break;
		case CEE_SHR_UN:
			if (ins->sreg1 != ins->dreg) {
				hppa_lshr (code, ins->sreg1, ins->sreg2, ins->dreg);
			}
			else {
				hppa_copy (code, ins->sreg1, hppa_r1);
				hppa_lshr (code, hppa_r1, ins->sreg2, ins->dreg);
			}
			break;
		case CEE_NOT:
			hppa_not (code, ins->sreg1, ins->dreg);
			break;
		case CEE_NEG:
			hppa_subi (code, 0, ins->sreg1, ins->dreg);
			break;

		case CEE_MUL:
		case OP_MUL_IMM:
			/* Should have been rewritten using xmpyu */
			g_assert_not_reached ();

		case OP_ICONST:
			if ((ins->inst_c0 > 0 && ins->inst_c0 >= (1 << 13)) ||
			    (ins->inst_c0 < 0 && ins->inst_c0 < -(1 << 13))) {
				hppa_ldil (code, hppa_lsel (ins->inst_c0), ins->dreg);
				hppa_ldo (code, hppa_rsel (ins->inst_c0), ins->dreg, ins->dreg);
			} else {
				hppa_ldo (code, ins->inst_c0, hppa_r0, ins->dreg);
			}
			break;
		case OP_AOTCONST:
			g_assert_not_reached ();
		/*
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			hppa_set_template (code, ins->dreg);
		*/
			g_warning ("unimplemented opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			NOT_IMPLEMENTED;
			break;
		case OP_FMOVE:
			if (ins->sreg1 != ins->dreg)
				hppa_fcpy (code, HPPA_FP_FMT_DBL, ins->sreg1, ins->dreg);
			break;

		case OP_HPPA_OUTARG_R4CONST:
			hppa_set (code, (unsigned int)ins->inst_p0, hppa_r1);
			hppa_fldwx (code, hppa_r0, hppa_r1, ins->dreg, 0);
			break;

		case OP_HPPA_OUTARG_REGOFFSET:
			hppa_ldo (code, ins->inst_offset, ins->inst_basereg, ins->dreg);
			break;

		case OP_JMP:
			/*
			 * Keep in sync with mono_arch_emit_epilog
			 */
			g_assert (!cfg->method->save_lmf);
			mono_add_patch_info (cfg, (guint8*) code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			hppa_bl (code, 8, hppa_r0);
			break;
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			hppa_ldw (code, 0, ins->sreg1, hppa_r1);
			break;
		case OP_ARGLIST:
			break;
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD, call->method);
			else
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, call->fptr);
			hppa_ldil (code, 0, hppa_r1); 
			hppa_ldo (code, 0, hppa_r1, hppa_r1); 
			/* 
			 * We may have loaded an actual function address, or
			 * it might be a plabel. Check to see if the plabel
			 * bit is set, and load the actual fptr from it if
			 * needed
			 */
			hppa_bb_n (code, HPPA_BIT_COND_MSB_CLR, hppa_r1, 30, 2);
			hppa_depi (code, 0, 31, 2, hppa_r1);
			hppa_ldw (code, 4, hppa_r1, hppa_r19);
			hppa_ldw (code, 0, hppa_r1, hppa_r1);
			hppa_ble (code, 0, hppa_r1);
			hppa_copy (code, hppa_r31, hppa_r2);
			if (call->signature->ret->type == MONO_TYPE_R4)
				hppa_fcnvff (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, hppa_fr4, hppa_fr4);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			call = (MonoCallInst*)ins;
			g_assert (!call->virtual);
			hppa_copy (code, ins->sreg1, hppa_r1);
			hppa_bb_n (code, HPPA_BIT_COND_MSB_CLR, hppa_r1, 30, 2);
			hppa_depi (code, 0, 31, 2, hppa_r1);
			hppa_ldw (code, 4, hppa_r1, hppa_r19);
			hppa_ldw (code, 0, hppa_r1, hppa_r1);
			hppa_ble (code, 0, hppa_r1);
			hppa_copy (code, hppa_r31, hppa_r2);
			if (call->signature->ret->type == MONO_TYPE_R4)
				hppa_fcnvff (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, hppa_fr4, hppa_fr4);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			call = (MonoCallInst*)ins;
			/* jump to ins->inst_sreg1 + ins->inst_offset */
			hppa_ldw (code, ins->inst_offset, ins->sreg1, hppa_r1);

			/* For virtual calls, emit a special token that can
			 * be used by get_vcall_slot_addr
			 */
			if (call->virtual)
				hppa_ldo (code, 0x777, hppa_r0, hppa_r0);
			hppa_ble (code, 0, hppa_r1);
			hppa_copy (code, hppa_r31, hppa_r2);
			break;
		case OP_LOCALLOC: {
			guint32 size_reg;

			/* Keep alignment */
			hppa_ldo (code, MONO_ARCH_LOCALLOC_ALIGNMENT - 1, ins->sreg1, ins->dreg);
			hppa_depi (code, 0, 31, 6, ins->dreg);
			hppa_copy (code, hppa_sp, hppa_r1);
			hppa_addl (code, ins->dreg, hppa_sp, hppa_sp);
			hppa_copy (code, hppa_r1, ins->dreg);

			if (ins->flags & MONO_INST_INIT) {
				hppa_stw (code, hppa_r0, 0, hppa_r1);
				hppa_combt (code, hppa_r1, hppa_sp, HPPA_CMP_COND_ULT, -3);
				hppa_ldo (code, 4, hppa_r1, hppa_r1);
			}
			break;
		}
		
		case OP_THROW:
			hppa_copy (code, ins->sreg1, hppa_r26);
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception");
			hppa_ldil (code, 0, hppa_r1); 
			hppa_ldo (code, 0, hppa_r1, hppa_r1);
			hppa_ble (code, 0, hppa_r1);
			hppa_copy (code, hppa_r31, hppa_r2);
			/* should never return */
			*code++ = 0xffeeddcc;
			break;
		case OP_RETHROW:
			hppa_copy (code, ins->sreg1, hppa_r26);
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception");
			hppa_ldil (code, 0, hppa_r1); 
			hppa_ldo (code, 0, hppa_r1, hppa_r1);
			hppa_ble (code, 0, hppa_r1);
			hppa_copy (code, hppa_r31, hppa_r2);
			/* should never return */
			*code++ = 0xffeeddcc;
			break;
		case OP_START_HANDLER:
			if (hppa_check_bits (ins->inst_left->inst_offset, 14))
				hppa_stw (code, hppa_r2, ins->inst_left->inst_offset, ins->inst_left->inst_basereg);
			else {
				hppa_set (code, ins->inst_left->inst_offset, hppa_r1);
				hppa_addl (code, ins->inst_left->inst_basereg, hppa_r1, hppa_r1);
				hppa_stw (code, hppa_r2, 0, hppa_r1);
			}
			break;
		case OP_ENDFILTER:
			if (ins->sreg1 != hppa_r26)
				hppa_copy (code, ins->sreg1, hppa_r26);
			if (hppa_check_bits (ins->inst_left->inst_offset, 14))
				hppa_ldw (code, ins->inst_left->inst_offset, ins->inst_left->inst_basereg, hppa_r2);
			else {
				hppa_set (code, ins->inst_left->inst_offset, hppa_r1);
				hppa_ldwx (code, hppa_r1, ins->inst_left->inst_basereg, hppa_r2);
			}
			hppa_bv (code, hppa_r0, hppa_r2);
			hppa_nop (code);
			break;
		case OP_ENDFINALLY:
			if (hppa_check_bits (ins->inst_left->inst_offset, 14))
				hppa_ldw (code, ins->inst_left->inst_offset, ins->inst_left->inst_basereg, hppa_r1);
			else {
				hppa_set (code, ins->inst_left->inst_offset, hppa_r1);
				hppa_ldwx (code, hppa_r1, ins->inst_left->inst_basereg, hppa_r1);
			}
			hppa_bv (code, hppa_r0, hppa_r1);
			hppa_nop (code);
			break;
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, (guint8*)code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			hppa_bl (code, 0, hppa_r2);
			hppa_nop (code);
			break;
		case OP_LABEL:
			ins->inst_c0 = (guint8*)code - cfg->native_code;
			break;
		case OP_BR: {
			guint32 target;
			DEBUG (printf ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins));
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			hppa_bl (code, 8, hppa_r0); 
			/* TODO: if the branch is too long, we may need to
			 * use a long-branch sequence:
			 *	hppa_ldil (code, 0, hppa_r1); 
			 *	hppa_ldo (code, 0, hppa_r1, hppa_r1); 
			 *	hppa_bv (code, hppa_r0, hppa_r1);
			 */
			hppa_nop (code);
			break;
		}
		case OP_BR_REG:
			hppa_bv (code, hppa_r0, ins->sreg1);
			hppa_nop(code);
			break;

		case OP_SWITCH: {
			int i;

			max_len += 8 * GPOINTER_TO_INT (ins->klass);
			if (offset > (cfg->code_size - max_len - 16)) {
				cfg->code_size += max_len;
				cfg->code_size *= 2;
				cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
				code = cfg->native_code + offset;
				code_start = (guint8*)code;
			}
			hppa_blr (code, ins->sreg1, hppa_r0);
			hppa_nop (code);
			for (i = 0; i < GPOINTER_TO_INT (ins->klass); ++i) {
				*code++ = 0xdeadbeef;
				*code++ = 0xdeadbeef;
			}
			break;
		}

		/* comclr is cool :-) */
		case OP_HPPA_CEQ:
			hppa_comclr_cond (code, HPPA_SUB_COND_NE, ins->sreg1, ins->sreg2, ins->dreg);
			hppa_ldo (code, 1, hppa_r0, ins->dreg);
			break;

		case OP_HPPA_CLT:
			hppa_comclr_cond (code, HPPA_SUB_COND_SGE, ins->sreg1, ins->sreg2, ins->dreg);
			hppa_ldo (code, 1, hppa_r0, ins->dreg);
			break;

		case OP_HPPA_CLT_UN:
			hppa_comclr_cond (code, HPPA_SUB_COND_UGE, ins->sreg1, ins->sreg2, ins->dreg);
			hppa_ldo (code, 1, hppa_r0, ins->dreg);
			break;

		case OP_HPPA_CGT:
			hppa_comclr_cond (code, HPPA_SUB_COND_SLE, ins->sreg1, ins->sreg2, ins->dreg);
			hppa_ldo (code, 1, hppa_r0, ins->dreg);
			break;

		case OP_HPPA_CGT_UN:
			hppa_comclr_cond (code, HPPA_SUB_COND_ULE, ins->sreg1, ins->sreg2, ins->dreg);
			hppa_ldo (code, 1, hppa_r0, ins->dreg);
			break;

		case OP_CEQ:
		case OP_CLT:
		case OP_CLT_UN:
		case OP_CGT:
		case OP_CGT_UN:
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
		case OP_COND_EXC_IOV:
		case OP_COND_EXC_IC:
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
		case OP_COMPARE:
		case OP_LCOMPARE:
		case OP_ICOMPARE:
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
			g_warning ("got opcode %s in %s(), should be reduced\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
			break;

		case OP_HPPA_BEQ:
		case OP_HPPA_BNE:
		case OP_HPPA_BLT:
		case OP_HPPA_BLT_UN:
		case OP_HPPA_BGT:
		case OP_HPPA_BGT_UN:
		case OP_HPPA_BGE:
		case OP_HPPA_BGE_UN:
		case OP_HPPA_BLE:
		case OP_HPPA_BLE_UN:
			EMIT_COND_BRANCH (ins, ins->sreg1, ins->sreg2, ins->opcode - OP_HPPA_BEQ);
			break;

		case OP_HPPA_COND_EXC_EQ:
		case OP_HPPA_COND_EXC_GE:
		case OP_HPPA_COND_EXC_GT:
		case OP_HPPA_COND_EXC_LE:
		case OP_HPPA_COND_EXC_LT:
		case OP_HPPA_COND_EXC_NE_UN:
		case OP_HPPA_COND_EXC_GE_UN:
		case OP_HPPA_COND_EXC_GT_UN:
		case OP_HPPA_COND_EXC_LE_UN:
		case OP_HPPA_COND_EXC_LT_UN: 
			EMIT_COND_SYSTEM_EXCEPTION (ins->sreg1, ins->sreg2, ins->opcode - OP_HPPA_COND_EXC_EQ, ins->inst_p1);
			break;

		case OP_HPPA_COND_EXC_OV:
		case OP_HPPA_COND_EXC_NO:
		case OP_HPPA_COND_EXC_C:
		case OP_HPPA_COND_EXC_NC: 
			NOT_IMPLEMENTED;

		/* floating point opcodes */
		case OP_R8CONST:
			hppa_set (code, (unsigned int)ins->inst_p0, hppa_r1);
			hppa_flddx (code, hppa_r0, hppa_r1, ins->dreg);
			break;
		case OP_R4CONST:
			hppa_set (code, (unsigned int)ins->inst_p0, hppa_r1);
			hppa_fldwx (code, hppa_r0, hppa_r1, hppa_fr31, 0);
			hppa_fcnvff (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, hppa_fr31, ins->dreg);
			break;
		case OP_STORER8_MEMBASE_REG:
			hppa_set (code, ins->inst_offset, hppa_r1);
			hppa_fstdx (code, ins->sreg1, hppa_r1, ins->inst_destbasereg);
			break;
		case OP_LOADR8_MEMBASE:
			hppa_set (code, ins->inst_offset, hppa_r1);
			hppa_flddx (code, hppa_r1, ins->inst_basereg, ins->dreg);
			break;
		case OP_STORER4_MEMBASE_REG:
			hppa_fcnvff (code, HPPA_FP_FMT_DBL, HPPA_FP_FMT_SGL, ins->sreg1, hppa_fr31);
			if (hppa_check_bits (ins->inst_offset, 5)) {
				hppa_fstws (code, hppa_fr31, 0, ins->inst_offset, ins->inst_destbasereg);
			} else {
				hppa_set (code, ins->inst_offset, hppa_r1);
				hppa_fstwx (code, hppa_fr31, 0, hppa_r1, ins->inst_destbasereg);
			}
			break;
		case OP_HPPA_STORER4_LEFT:
		case OP_HPPA_STORER4_RIGHT:
			if (hppa_check_bits (ins->inst_offset, 5)) {
				hppa_fstws (code, ins->sreg1, (ins->opcode == OP_HPPA_STORER4_RIGHT), ins->inst_offset, ins->inst_destbasereg);
			} else {
				hppa_set (code, ins->inst_offset, hppa_r1);
				hppa_fstwx (code, ins->sreg1, (ins->opcode == OP_HPPA_STORER4_RIGHT), hppa_r1, ins->inst_destbasereg);
			}
			break;
		case OP_LOADR4_MEMBASE:
			if (hppa_check_bits (ins->inst_offset, 5)) {
				hppa_fldws (code, ins->inst_offset, ins->inst_basereg, hppa_fr31, 0);
			} else {
				hppa_set (code, ins->inst_offset, hppa_r1);
				hppa_fldwx (code, hppa_r1, ins->inst_basereg, hppa_fr31, 0);
			}
			hppa_fcnvff (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, hppa_fr31, ins->dreg);
			break;
		case OP_HPPA_LOADR4_LEFT:
		case OP_HPPA_LOADR4_RIGHT:
			if (hppa_check_bits (ins->inst_offset, 5)) {
				hppa_fldws (code, ins->inst_offset, ins->inst_basereg, ins->dreg, (ins->opcode == OP_HPPA_LOADR4_RIGHT));
			} else {
				hppa_set (code, ins->inst_offset, hppa_r1);
				hppa_fldwx (code, hppa_r1, ins->inst_basereg, ins->dreg, (ins->opcode == OP_HPPA_LOADR4_RIGHT));
			}
			break;
		
		case CEE_CONV_R4:
			hppa_stw (code, ins->sreg1, -16, hppa_sp);
			hppa_fldws (code, -16, hppa_sp, hppa_fr31, 0);
			hppa_fcnvxf (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_SGL, hppa_fr31, ins->dreg);
			hppa_fcnvff (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, ins->dreg, ins->dreg);
			break;

		case OP_FCONV_TO_R4:
			/* reduce precision */
			hppa_fcnvff (code, HPPA_FP_FMT_DBL, HPPA_FP_FMT_SGL, ins->sreg1, ins->dreg);
			hppa_fcnvff (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, ins->dreg, ins->dreg);
			break;

		case OP_HPPA_SETF4REG:
			hppa_fcnvff (code, HPPA_FP_FMT_DBL, HPPA_FP_FMT_SGL, ins->sreg1, ins->dreg);
			break;
		case CEE_CONV_R8: 
			hppa_stw (code, ins->sreg1, -16, hppa_sp);
			hppa_fldws (code, -16, hppa_sp, hppa_fr31, 0);
			hppa_fcnvxf (code, HPPA_FP_FMT_SGL, HPPA_FP_FMT_DBL, hppa_fr31, ins->dreg);
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

		case OP_LCONV_TO_OVF_I: 
			NOT_IMPLEMENTED;
			break;

		case OP_FADD:
			hppa_fadd (code, HPPA_FP_FMT_DBL, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_FSUB:
			hppa_fsub (code, HPPA_FP_FMT_DBL, ins->sreg1, ins->sreg2, ins->dreg);
			break;		
		case OP_FMUL:
			hppa_fmul (code, HPPA_FP_FMT_DBL, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_FDIV:
			hppa_fdiv (code, HPPA_FP_FMT_DBL, ins->sreg1, ins->sreg2, ins->dreg);
			break;
		case OP_FREM:
			NOT_IMPLEMENTED;
			break;		

		case OP_FCOMPARE:
			g_assert_not_reached();
			break;

		case OP_FCEQ:
		case OP_FCLT:
		case OP_FCLT_UN:
		case OP_FCGT:
		case OP_FCGT_UN:
			hppa_fcmp (code, HPPA_FP_FMT_DBL, float_ceq_table [ins->opcode - OP_FCEQ], ins->sreg1, ins->sreg2);
			hppa_ftest (code, 0);
			hppa_bl (code, 12, hppa_r0);
			hppa_ldo (code, 1, hppa_r0, ins->dreg);
			hppa_ldo (code, 0, hppa_r0, ins->dreg);
			break;

		case OP_FBEQ:
		case OP_FBLT:
		case OP_FBGT:
		case OP_FBGE:
		case OP_FBLE:
		case OP_FBNE_UN:
		case OP_FBLT_UN:
		case OP_FBGT_UN:
		case OP_FBGE_UN:
		case OP_FBLE_UN:
			EMIT_FLOAT_COND_BRANCH (ins, ins->sreg1, ins->sreg2, ins->opcode - OP_FBEQ);
			break;

		case OP_CKFINITE: 
		case OP_MEMORY_BARRIER:
			break;

		case OP_HPPA_XMPYU:
			hppa_xmpyu (code, ins->sreg1, ins->sreg2, ins->dreg);
			break;

		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
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

	cfg->code_len = (guint8*)code - cfg->native_code;
	DEBUG_FUNC_EXIT();
}

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	DEBUG_FUNC_ENTER();
	/* FIXME: Move part of this to arch independent code */
	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		gpointer target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);
		DEBUG (printf ("patch_info->type = %d, target = %p\n", patch_info->type, target));

		switch (patch_info->type) {
		case MONO_PATCH_INFO_NONE:
		case MONO_PATCH_INFO_BB_OVF:
		case MONO_PATCH_INFO_EXC_OVF:
			continue;

		case MONO_PATCH_INFO_IP:
			hppa_patch ((guint32 *)ip, ip);
			continue;

		case MONO_PATCH_INFO_CLASS_INIT: {
			break;
		}
		case MONO_PATCH_INFO_METHOD_JUMP: {
			break;
		}
		case MONO_PATCH_INFO_SWITCH: {
			int i;
			gpointer *table = (gpointer *)target;
			ip += 8;
			for (i = 0; i < patch_info->data.table->table_size; i++) { 
				DEBUG (printf ("Patching switch table, table[%d] = %p\n", i, table[i]));
				hppa_ldil (ip, hppa_lsel (table [i]), hppa_r1);
				hppa_be_n (ip, hppa_rsel (table [i]), hppa_r1);
			}
			continue;
		}
		default:
			break;
		}
		hppa_patch ((guint32 *)ip, target);
	}

	DEBUG_FUNC_EXIT();
}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guint32 *code = (guint32*)p;

	DEBUG_FUNC_ENTER();

	hppa_set (code, cfg->method, hppa_r26);
	hppa_copy (code, hppa_r0, hppa_r25); /* NULL sp for now */
	hppa_set (code, func, hppa_r1);
	hppa_depi (code, 0, 31, 2, hppa_r1);
	hppa_ldw (code, 0, hppa_r1, hppa_r1);
	hppa_ble (code, 0, hppa_r1);
	hppa_copy (code, hppa_r31, hppa_r2);

	DEBUG_FUNC_EXIT();
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
mono_arch_instrument_epilog_full (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments, gboolean preserve_argument_registers)
{
	guint32 *code = (guint32*)p;
	DEBUG_FUNC_ENTER();
#if 0
	int save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;

	switch (mono_type_get_underlying_type (mono_method_signature (method)->ret)->type) {
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
#endif
	DEBUG_FUNC_EXIT();
	return code;
}

/* 
 * The HPPA stack frame should look like this:
 *
 * ---------------------
 *  incoming params area
 * ---------------------
 *     linkage area 		size = ARGS_OFFSET
 * --------------------- 	fp = psp
 * HPPA_STACK_LMF_OFFSET
 * ---------------------
 * MonoLMF structure or saved registers
 * -------------------
 *        locals		size = cfg->stack_offset - cfg->param_area
 * ---------------------
 *      params area		size = cfg->param_area - ARGS_OFFSET  (aligned)
 * ---------------------
 *  callee linkage area 	size = ARGS_OFFSET
 * --------------------- sp
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

	DEBUG_FUNC_ENTER();
	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		tracing = 1;

	sig = mono_method_signature (method);
	cfg->code_size = 512 + sig->param_count * 20;
	code = cfg->native_code = g_malloc (cfg->code_size);

	/* TODO: enable tail call optimization */
	if (1 || cfg->flags & MONO_CFG_HAS_CALLS) {
		hppa_stw (code, hppa_r2, -20, hppa_sp);
	}

	/* locals area */
	pos = HPPA_STACK_LMF_OFFSET;

	/* figure out how much space we need for spilling */
	if (!method->save_lmf) {
		/* spill callee-save registers */
		guint32 mask = cfg->used_int_regs & MONO_ARCH_CALLEE_SAVED_REGS;
		for (i = 0; i < 32; i++) {
			if ((1 << i) & mask)
				pos += sizeof (gulong);
		}
	} else {
		lmf_offset = pos;
		pos += sizeof (MonoLMF);
	}

	alloc_size = ALIGN_TO (pos + cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);
	g_assert ((alloc_size & (MONO_ARCH_FRAME_ALIGNMENT - 1)) == 0);

	cfg->stack_usage = alloc_size;

	if (alloc_size) {
		hppa_copy (code, hppa_r3, hppa_r1);
		hppa_copy (code, hppa_sp, hppa_r3);
		if (hppa_check_bits (alloc_size, 14))
			hppa_stwm (code, hppa_r1, alloc_size, hppa_sp);
		else {
			hppa_stwm (code, hppa_r1, 8100, hppa_sp);
			hppa_addil (code, hppa_lsel (alloc_size - 8100), hppa_sp);
			hppa_ldo (code, hppa_rsel (alloc_size - 8100), hppa_r1, hppa_sp);
		}
	}

        /* compute max_offset in order to use short forward jumps
	 * we always do it on hppa because the immediate displacement
	 * for jumps is small 
	 */
	max_offset = 0;
	for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
		MonoInst *ins = bb->code;
		bb->max_offset = max_offset;

		if (cfg->prof_options & MONO_PROFILE_COVERAGE)
			max_offset += 6; 

		MONO_BB_FOR_EACH_INS (bb, ins)
			max_offset += ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
	}

	DEBUG (printf ("Incoming arguments: \n"));
	cinfo = get_call_info (sig, sig->pinvoke);

	/* We do this first so that we don't have to worry about the LMF-
	 * saving code clobbering r28
	 */
	if (cinfo->struct_return)
		hppa_stw (code, hppa_r28, cfg->ret->inst_offset, hppa_sp);

	/* Save the LMF or the spilled registers */
	pos = HPPA_STACK_LMF_OFFSET;
	if (!method->save_lmf) {
		/* spill callee-save registers */
		guint32 mask = cfg->used_int_regs & MONO_ARCH_CALLEE_SAVED_REGS;
		for (i = 0; i < 32; i++) {
			if ((1 << i) & mask) {
				if (i == hppa_r3) {
					hppa_ldw (code, 0, hppa_r3, hppa_r1);
					hppa_stw (code, hppa_r1, pos, hppa_r3);
				} else
					hppa_stw (code, i, pos, hppa_r3);
				pos += sizeof (gulong);
			}
		}
	} else {
		int ofs = lmf_offset + G_STRUCT_OFFSET (MonoLMF, regs);
		int reg;

		hppa_ldw (code, 0, hppa_r3, hppa_r1);
		hppa_stw (code, hppa_r1, ofs, hppa_r3);
		ofs += sizeof (gulong);
		for (reg = 4; reg < 32; reg++) {
			if (HPPA_IS_SAVED_GREG (reg)) {
				hppa_stw (code, reg, ofs, hppa_r3);
				ofs += sizeof (gulong);
			}
		}
		/* We shouldn't need to save the FP regs.... */
		ofs = ALIGN_TO (ofs, sizeof(double));
		hppa_set (code, ofs, hppa_r1);
		for (reg = 0; reg < 32; reg++) {
			if (HPPA_IS_SAVED_FREG (reg)) {
				hppa_fstdx (code, reg, hppa_r1, hppa_r3);
				hppa_ldo (code, sizeof(double), hppa_r1, hppa_r1);
			}
		}

		/* We also spill the arguments onto the stack, because
		 * the call to hppa_get_lmf_addr below can clobber them
		 *
		 * This goes in the param area that is always allocated
		 */
		ofs = -36;
		for (reg = hppa_r26; reg >= hppa_r23; reg--) {
			hppa_stw (code, reg, ofs, hppa_sp);
			ofs -= 4;
		}
	}

	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		hppa_copy (code, hppa_r30, hppa_r4);

	if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		hppa_set (code, cfg->domain, hppa_r26);
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, (gpointer)"mono_jit_thread_attach");
		hppa_ldil (code, 0, hppa_r1);
		hppa_ldo (code, 0, hppa_r1, hppa_r1);
		hppa_depi (code, 0, 31, 2, hppa_r1);
		hppa_ldw (code, 0, hppa_r1, hppa_r1);
		hppa_ble (code, 0, hppa_r1);
		hppa_copy (code, hppa_r31, hppa_r2);
	}

	if (method->save_lmf) {
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
				     (gpointer)"mono_get_lmf_addr");
		hppa_ldil (code, 0, hppa_r1);
		hppa_ldo (code, 0, hppa_r1, hppa_r1);
		hppa_depi (code, 0, 31, 2, hppa_r1);
		hppa_ldw (code, 0, hppa_r1, hppa_r1);
		hppa_ble (code, 0, hppa_r1);
		hppa_copy (code, hppa_r31, hppa_r2);

		/* lmf_offset is the offset from the previous stack pointer,
		 * The pointer to the struct is put in hppa_r22 (new_lmf).
		 * The callee-saved registers are already in the MonoLMF 
		 * structure
		 */

		/* hppa_r22 = new_lmf (on the stack) */
		hppa_ldo (code, lmf_offset, hppa_r3, hppa_r22);
		/* lmf_offset is the offset from the previous stack pointer,
		 */
		hppa_stw (code, hppa_r28, G_STRUCT_OFFSET(MonoLMF, lmf_addr), hppa_r22);
		/* new_lmf->previous_lmf = *lmf_addr */
		hppa_ldw (code, 0, hppa_r28, hppa_r1);
		hppa_stw (code, hppa_r1, G_STRUCT_OFFSET(MonoLMF, previous_lmf), hppa_r22);
		/* *(lmf_addr) = r22 */
		hppa_stw (code, hppa_r22, 0, hppa_r28);
		hppa_set (code, method, hppa_r1);
		hppa_stw (code, hppa_r1, G_STRUCT_OFFSET(MonoLMF, method), hppa_r22);
		hppa_stw (code, hppa_sp, G_STRUCT_OFFSET(MonoLMF, ebp), hppa_r22);
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		hppa_ldil (code, 0, hppa_r1);
		hppa_ldo (code, 0, hppa_r1, hppa_r1);
		hppa_stw (code, hppa_r1, G_STRUCT_OFFSET(MonoLMF, eip), hppa_r22);

		/* Now reload the arguments from the stack */
		hppa_ldw (code, -36, hppa_sp, hppa_r26);
		hppa_ldw (code, -40, hppa_sp, hppa_r25);
		hppa_ldw (code, -44, hppa_sp, hppa_r24);
		hppa_ldw (code, -48, hppa_sp, hppa_r23);
	}

	/* load arguments allocated to register from the stack */
	pos = 0;

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];

		if (inst->opcode == OP_REGVAR) {
			/* Want the argument in a register */
			switch (ainfo->storage) {
			case ArgInIReg:
				if (ainfo->reg != inst->dreg)
					hppa_copy (code, ainfo->reg, inst->dreg);
				DEBUG (printf ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg)));
				break;

			case ArgInIRegPair:
				if (ainfo->reg != inst->dreg) {
					hppa_copy (code, ainfo->reg, inst->dreg);
					hppa_copy (code, ainfo->reg + 1, inst->dreg + 1);
				}
				DEBUG (printf ("Argument %d assigned to register %s, %s\n", pos, mono_arch_regname (inst->dreg), mono_arch_regname (inst->dreg + 1)));
				break;

			case ArgInFReg:
				if (ainfo->reg != inst->dreg)
					hppa_fcpy (code, HPPA_FP_FMT_SGL, ainfo->reg, inst->dreg);
				DEBUG (printf ("Argument %d assigned to single register %s\n", pos, mono_arch_fregname (inst->dreg)));
				break;

			case ArgInDReg:
				if (ainfo->reg != inst->dreg)
					hppa_fcpy (code, HPPA_FP_FMT_DBL, ainfo->reg, inst->dreg);
				DEBUG (printf ("Argument %d assigned to double register %s\n", pos, mono_arch_fregname (inst->dreg)));
				break;

			case ArgOnStack:
				switch (ainfo->size) {
				case 1:
					hppa_ldb (code, ainfo->offset, hppa_r3, inst->dreg);
					break;
				case 2:
					hppa_ldh (code, ainfo->offset, hppa_r3, inst->dreg);
					break;
				case 4:
					hppa_ldw (code, ainfo->offset, hppa_r3, inst->dreg);
					break;
				default:
					g_assert_not_reached ();
				}

				
				DEBUG (printf ("Argument %d loaded from the stack [%s - %d]\n", pos, mono_arch_regname (hppa_r3), -ainfo->offset));
				break;

			default:
				g_assert_not_reached ();
			}
		} 
		else {
			/* Want the argument on the stack */
			switch (ainfo->storage)
			{
			case ArgInIReg: {
				int off, reg;
				DEBUG (printf ("Argument %d stored from register %s to stack [%s + %d]\n", pos, mono_arch_regname (ainfo->reg), mono_arch_regname (inst->inst_basereg), inst->inst_offset));
				if (hppa_check_bits (inst->inst_offset, 14)) {
					off = inst->inst_offset;
					reg = inst->inst_basereg;
				}
				else {
					hppa_set (code, inst->inst_offset, hppa_r1);
					hppa_add (code, hppa_r1, inst->inst_basereg, hppa_r1);
					off = 0;
					reg = hppa_r1;
				}
				switch (ainfo->size)
				{
				case 1:
					hppa_stb (code, ainfo->reg, off, reg);
					break;
				case 2:
					hppa_sth (code, ainfo->reg, off, reg);
					break;
				case 4:
					hppa_stw (code, ainfo->reg, off, reg);
					break;
				default:
					g_assert_not_reached ();
				}
				break;
			}
			case ArgInIRegPair:
				DEBUG (printf ("Argument %d stored from register (%s,%s) to stack [%s + %d]\n", pos, mono_arch_regname (ainfo->reg), mono_arch_regname (ainfo->reg+1), mono_arch_regname (inst->inst_basereg), inst->inst_offset));
				if (hppa_check_bits (inst->inst_offset + 4, 14)) {
					hppa_stw (code, ainfo->reg, inst->inst_offset, inst->inst_basereg);
					hppa_stw (code, ainfo->reg + 1, inst->inst_offset + 4, inst->inst_basereg);
				}
				else {
					hppa_ldo (code, inst->inst_offset, inst->inst_basereg, hppa_r1);
					hppa_stw (code, ainfo->reg, 0, hppa_r1);
					hppa_stw (code, ainfo->reg + 1, 4, hppa_r1);
				}
				break;

			case ArgInFReg:
				DEBUG (printf ("Argument %d (float) stored from register %s to stack [%s + %d]\n", pos, mono_arch_fregname (ainfo->reg), mono_arch_regname (inst->inst_basereg), inst->inst_offset));
				hppa_ldo (code, inst->inst_offset, inst->inst_basereg, hppa_r1);
				hppa_fstwx (code, ainfo->reg, 0, hppa_r0, hppa_r1);
				break;

			case ArgInDReg:
				DEBUG (printf ("Argument %d (double) stored from register %s to stack [%s + %d]\n", pos, mono_arch_fregname (ainfo->reg), mono_arch_regname (inst->inst_basereg), inst->inst_offset));
				hppa_ldo (code, inst->inst_offset, inst->inst_basereg, hppa_r1);
				hppa_fstdx (code, ainfo->reg, hppa_r0, hppa_r1);
				break;

			case ArgOnStack:
				DEBUG (printf ("Argument %d copied from [%s - %d] to [%s + %d] (size=%d)\n", pos, mono_arch_regname (hppa_r3), -ainfo->offset, mono_arch_regname (inst->inst_basereg), inst->inst_offset, ainfo->size));
				if (inst->inst_offset != ainfo->offset ||
				    inst->inst_basereg != hppa_r3)
					code = emit_memcpy (code, inst->inst_offset, inst->inst_basereg, ainfo->offset, hppa_r3, ainfo->size);
				break;

			default:
				g_assert_not_reached ();
			}
		}

		pos++;
	}


	if (tracing)
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);

	if (getenv("HPPA_BREAK")) {
		*(guint32*)code = 0x00010004;
		code += 4;
	}

	cfg->code_len = code - cfg->native_code;
	g_assert (cfg->code_len < cfg->code_size);
	g_free (cinfo);

	DEBUG_FUNC_EXIT();
	return code;
}


void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	guint32 *code;
	int max_epilog_size = 16 + 20 * 4;
	int pos;
	
	DEBUG_FUNC_ENTER();
	sig = mono_method_signature (cfg->method);
	if (cfg->method->save_lmf)
		max_epilog_size += 128;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		cfg->stat_code_reallocs++;
	}

	code = (guint32*)(cfg->native_code + cfg->code_len);

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);

	pos = HPPA_STACK_LMF_OFFSET;
	if (cfg->method->save_lmf) {
		int reg;
		hppa_ldo (code, pos, hppa_r3, hppa_r22);
		hppa_ldw (code, G_STRUCT_OFFSET(MonoLMF, previous_lmf), hppa_r22, hppa_r21);
		hppa_ldw (code, G_STRUCT_OFFSET(MonoLMF, lmf_addr), hppa_r22, hppa_r20);
		hppa_stw (code, hppa_r21, G_STRUCT_OFFSET(MonoLMF, previous_lmf), hppa_r20);

		pos += G_STRUCT_OFFSET(MonoLMF, regs) + sizeof (gulong);
		/* We skip the restore of r3 here, it is restored from the
		 * stack anyway. This makes the code a bit easier.
		 */
		for (reg = 4; reg < 31; reg++) {
			if (HPPA_IS_SAVED_GREG (reg)) {
				hppa_ldw (code, pos, hppa_r3, reg);
				pos += sizeof(gulong);
			}
		}
		
		pos = ALIGN_TO (pos, sizeof (double));
		hppa_set (code, pos, hppa_r1);
		for (reg = 0; reg < 31; reg++) {
			if (HPPA_IS_SAVED_FREG (reg)) {
				hppa_flddx (code, hppa_r1, hppa_r3, reg);
				hppa_ldo (code, sizeof (double), hppa_r1, hppa_r1);
				pos += sizeof (double);
			}
		}
	} else {
		guint32 mask = cfg->used_int_regs & MONO_ARCH_CALLEE_SAVED_REGS;
		int i;
		for (i = 0; i < 32; i++) {
			if (i == hppa_r3)
				continue;
			if ((1 << i) & mask) {
				hppa_ldw (code, pos, hppa_r3, i);
				pos += sizeof (gulong);
			}
		}
	}

	if (sig->ret->type != MONO_TYPE_VOID && 
	    mono_type_to_stind (sig->ret) == CEE_STOBJ) {
		CallInfo *cinfo = get_call_info (sig, sig->pinvoke);

		switch (cinfo->ret.storage) {
		case ArgInIReg:
			hppa_ldw (code, cfg->ret->inst_offset, hppa_sp, hppa_r28);
			hppa_ldw (code, 0, hppa_r28, hppa_r28);
			break;
		case ArgInIRegPair:
			hppa_ldw (code, cfg->ret->inst_offset, hppa_sp, hppa_r28);
			hppa_ldw (code, 4, hppa_r28, hppa_r29);
			hppa_ldw (code, 0, hppa_r28, hppa_r28);
			break;
		case ArgOnStack:
			/* Nothing to do */
			break;
		default:
			g_assert_not_reached ();
		}
		g_free (cinfo);
	}

	if (1 || cfg->flags & MONO_CFG_HAS_CALLS)
		hppa_ldw (code, -20, hppa_r3, hppa_r2);
	hppa_ldo (code, 64, hppa_r3, hppa_sp);
	hppa_bv (code, hppa_r0, hppa_r2);
	hppa_ldwm (code, -64, hppa_sp, hppa_r3);

	cfg->code_len = (guint8*)code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
	DEBUG_FUNC_EXIT();
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
	int i;
	guint8 *code;
	const guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM] = {NULL};
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM] = {0};
	int max_epilog_size = 50;

	DEBUG_FUNC_ENTER();

	/* count the number of exception infos */
     
	/* 
	 * make sure we have enough space for exceptions
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_BB_OVF:
			g_assert_not_reached ();
			break;

		case MONO_PATCH_INFO_EXC_OVF: {
			const MonoOvfJump *ovfj = patch_info->data.target;
			max_epilog_size += 8;
			i = exception_id_by_name (ovfj->data.exception);
			if (!exc_throw_found [i]) {
				max_epilog_size += 24;
				exc_throw_found [i] = TRUE;
			}
			break;
		}

		case MONO_PATCH_INFO_EXC:
			i = exception_id_by_name (patch_info->data.target);
			if (!exc_throw_found [i]) {
				max_epilog_size += 24;
				exc_throw_found [i] = TRUE;
			}
			break;

		default:
			break;
		}
	}

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		cfg->stat_code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	/* add code to raise exceptions */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_BB_OVF: {
			/* TODO */
			break;
		}
		case MONO_PATCH_INFO_EXC_OVF: {
			const MonoOvfJump *ovfj = patch_info->data.target;
			MonoJumpInfo *newji;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			unsigned char *stub = code;

			/* Patch original call, point it at the stub */
			hppa_patch ((guint32 *)ip, code);

			/* Write the stub */
			/* SUBTLE: this has to be PIC, because the code block
			 * can be relocated
			 */
			hppa_bl_n (code, 8, hppa_r0);
			hppa_nop (code);

			/* Add a patch info to patch the stub to point to the exception code */
			newji = mono_mempool_alloc (cfg->mempool, sizeof (MonoJumpInfo));
			newji->type = MONO_PATCH_INFO_EXC;
			newji->ip.i = stub - cfg->native_code;
			newji->data.target = ovfj->data.exception;
			newji->next = patch_info->next;
			patch_info->next = newji;
			break;
		}
		case MONO_PATCH_INFO_EXC: {
			unsigned char *ip = patch_info->ip.i + cfg->native_code;
			i = exception_id_by_name (patch_info->data.target);
			if (exc_throw_pos [i]) {
				hppa_patch ((guint32 *)ip, exc_throw_pos [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
				break;
			} else {
				exc_throw_pos [i] = code;
			}
			hppa_patch ((guint32 *)ip, code);
			hppa_set (code, patch_info->data.target, hppa_r26);
			patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "mono_arch_throw_exception_by_name";
			patch_info->ip.i = code - cfg->native_code;
			
			/* Assume the caller has set r2, we can't set it 
			 * here based on ip, because the caller may 
			 * be relocated (also the "ip" may be from an overflow
			 * stub)
			 */
			hppa_ldil (code, 0, hppa_r1);
			hppa_ldo (code, 0, hppa_r1, hppa_r1);
			hppa_bv (code, hppa_r0, hppa_r1);
			hppa_nop (code);
			break;
		}
		default:
			/* do nothing */
			break;
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);
	DEBUG_FUNC_EXIT();
}

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

#error "--with-sigaltstack=yes not supported on hppa"

#endif

void
mono_arch_finish_init (void)
{
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
		MONO_INST_NEW (cfg, this, OP_MOVE);
		this->type = this_type;
		this->sreg1 = this_reg;
		this->dreg = mono_alloc_ireg (cfg);
		mono_bblock_add_inst (cfg->cbb, this);
		mono_call_inst_add_outarg_reg (cfg, inst, this->dreg, hppa_r26, FALSE);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_MOVE);
		vtarg->type = STACK_MP;
		vtarg->sreg1 = vt_reg;
		vtarg->dreg = mono_alloc_ireg (cfg);
		mono_bblock_add_inst (cfg->cbb, vtarg);
		mono_call_inst_add_outarg_reg (cfg, inst, vtarg->dreg, hppa_r28, FALSE);
	}
}


MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;
	DEBUG_FUNC_ENTER();
	DEBUG_FUNC_EXIT();

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

	DEBUG_FUNC_ENTER();
	cinfo = get_call_info (csig, FALSE);

	if (csig->hasthis) {
		ainfo = &cinfo->args [0];
		arg_info [0].offset = ainfo->offset;
	}

	for (k = 0; k < param_count; k++) {
		ainfo = &cinfo->args [k + csig->hasthis];

		arg_info [k + 1].offset = ainfo->offset;
		arg_info [k + 1].size = mono_type_size (csig->params [k], &align);
	}

	g_free (cinfo);
	DEBUG_FUNC_EXIT();
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

gpointer
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	/* FIXME: implement */
	g_assert_not_reached ();
}
