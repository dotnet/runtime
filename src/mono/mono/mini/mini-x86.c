/*
 * mini-x86.c: x86 backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Patrik Torstensson
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>
#include <math.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/utils/mono-math.h>

#include "trace.h"
#include "mini-x86.h"
#include "inssel.h"
#include "cpu-pentium.h"

static gint lmf_tls_offset = -1;
static gint appdomain_tls_offset = -1;
static gint thread_tls_offset = -1;

#ifdef PLATFORM_WIN32
/* Under windows, the default pinvoke calling convention is stdcall */
#define CALLCONV_IS_STDCALL(call_conv) (((call_conv) == MONO_CALL_STDCALL) || ((call_conv) == MONO_CALL_DEFAULT))
#else
#define CALLCONV_IS_STDCALL(call_conv) ((call_conv) == MONO_CALL_STDCALL)
#endif

#define SIGNAL_STACK_SIZE (64 * 1024)

const char*
mono_arch_regname (int reg) {
	switch (reg) {
	case X86_EAX: return "%eax";
	case X86_EBX: return "%ebx";
	case X86_ECX: return "%ecx";
	case X86_EDX: return "%edx";
	case X86_ESP: return "%esp";	case X86_EBP: return "%ebp";
	case X86_EDI: return "%edi";
	case X86_ESI: return "%esi";
	}
	return "unknown";
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

static const guchar cpuid_impl [] = {
	0x55,                   	/* push   %ebp */
	0x89, 0xe5,                	/* mov    %esp,%ebp */
	0x53,                   	/* push   %ebx */
	0x8b, 0x45, 0x08,             	/* mov    0x8(%ebp),%eax */
	0x0f, 0xa2,                	/* cpuid   */
	0x50,                   	/* push   %eax */
	0x8b, 0x45, 0x10,             	/* mov    0x10(%ebp),%eax */
	0x89, 0x18,                	/* mov    %ebx,(%eax) */
	0x8b, 0x45, 0x14,             	/* mov    0x14(%ebp),%eax */
	0x89, 0x08,                	/* mov    %ecx,(%eax) */
	0x8b, 0x45, 0x18,             	/* mov    0x18(%ebp),%eax */
	0x89, 0x10,                	/* mov    %edx,(%eax) */
	0x58,                   	/* pop    %eax */
	0x8b, 0x55, 0x0c,             	/* mov    0xc(%ebp),%edx */
	0x89, 0x02,                	/* mov    %eax,(%edx) */
	0x5b,                   	/* pop    %ebx */
	0xc9,                   	/* leave   */
	0xc3,                   	/* ret     */
};

typedef void (*CpuidFunc) (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx);

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
		CpuidFunc func = (CpuidFunc)cpuid_impl;
		func (id, p_eax, p_ebx, p_ecx, p_edx);
		/*
		 * We use this approach because of issues with gcc and pic code, see:
		 * http://gcc.gnu.org/cgi-bin/gnatsweb.pl?cmd=view%20audit-trail&database=gcc&pr=7329
		__asm__ __volatile__ ("cpuid"
			: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
			: "a" (id));
		*/
		return 1;
	}
	return 0;
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
	guint16 fpcw;

	/* spec compliance requires running with double precision */
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
	fpcw &= ~X86_FPCW_PRECC_MASK;
	fpcw |= X86_FPCW_PREC_DOUBLE;
	__asm__  __volatile__ ("fldcw %0\n": : "m" (fpcw));
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));

}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	int eax, ebx, ecx, edx;
	guint32 opts = 0;
	
	*exclude_mask = 0;
	/* Feature Flags function, flags returned in EDX. */
	if (cpuid (1, &eax, &ebx, &ecx, &edx)) {
		if (edx & (1 << 15)) {
			opts |= MONO_OPT_CMOV;
			if (edx & 1)
				opts |= MONO_OPT_FCMOV;
			else
				*exclude_mask |= MONO_OPT_FCMOV;
		} else
			*exclude_mask |= MONO_OPT_CMOV;
	}
	return opts;
}

/*
 * Determine whenever the trap whose info is in SIGINFO is caused by
 * integer overflow.
 */
gboolean
mono_arch_is_int_overflow (void *sigctx, void *info)
{
	struct sigcontext *ctx = (struct sigcontext*)sigctx;
	guint8* ip;

	ip = (guint8*)ctx->SC_EIP;

	if ((ip [0] == 0xf7) && (x86_modrm_mod (ip [1]) == 0x3) && (x86_modrm_reg (ip [1]) == 0x7)) {
		gint32 reg;

		/* idiv REG */
		switch (x86_modrm_rm (ip [1])) {
		case X86_ECX:
			reg = ctx->SC_ECX;
			break;
		case X86_EBX:
			reg = ctx->SC_EBX;
			break;
		default:
			g_assert_not_reached ();
			reg = -1;
		}

		if (reg == -1)
			return TRUE;
	}
			
	return FALSE;
}

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	switch (mono_type_get_underlying_type (t)->type) {
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

		if ((ins->flags & (MONO_INST_IS_DEAD|MONO_INST_VOLATILE|MONO_INST_INDIRECT)) || 
		    (ins->opcode != OP_LOCAL && ins->opcode != OP_ARG))
			continue;

		/* we dont allocate I1 to registers because there is no simply way to sign extend 
		 * 8bit quantities in caller saved registers on x86 */
		if (is_regsize_var (ins->inst_vtype) || (ins->inst_vtype->type == MONO_TYPE_BOOLEAN) || 
		    (ins->inst_vtype->type == MONO_TYPE_U1) || (ins->inst_vtype->type == MONO_TYPE_U2)||
		    (ins->inst_vtype->type == MONO_TYPE_I2) || (ins->inst_vtype->type == MONO_TYPE_CHAR)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	/* we can use 3 registers for global allocation */
	regs = g_list_prepend (regs, (gpointer)X86_EBX);
	regs = g_list_prepend (regs, (gpointer)X86_ESI);
	regs = g_list_prepend (regs, (gpointer)X86_EDI);

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
	MonoInst *ins = cfg->varinfo [vmv->idx];

	if (cfg->method->save_lmf)
		/* The register is already saved */
		return (ins->opcode == OP_ARG) ? 1 : 0;
	else
		/* push+pop+possible load if it is an argument */
		return (ins->opcode == OP_ARG) ? 3 : 2;
}
 
/*
 * Set var information according to the calling convention. X86 version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *m)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *inst;
	int i, offset, size, align, curinst;

	header = mono_method_get_header (m->method);

	sig = m->method->signature;

	offset = 8;
	curinst = 0;
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		m->ret->opcode = OP_REGOFFSET;
		m->ret->inst_basereg = X86_EBP;
		m->ret->inst_offset = offset;
		offset += sizeof (gpointer);
	} else {
		/* FIXME: handle long and FP values */
		switch (sig->ret->type) {
		case MONO_TYPE_VOID:
			break;
		default:
			m->ret->opcode = OP_REGVAR;
			m->ret->inst_c0 = X86_EAX;
			break;
		}
	}
	if (sig->hasthis) {
		inst = m->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = X86_EBP;
		}
		inst->inst_offset = offset;
		offset += sizeof (gpointer);
		curinst++;
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		m->sig_cookie = offset;
		offset += sizeof (gpointer);
	}

	for (i = 0; i < sig->param_count; ++i) {
		inst = m->varinfo [curinst];
		if (inst->opcode != OP_REGVAR) {
			inst->opcode = OP_REGOFFSET;
			inst->inst_basereg = X86_EBP;
		}
		inst->inst_offset = offset;
		size = mono_type_size (sig->params [i], &align);
		size += 4 - 1;
		size &= ~(4 - 1);
		offset += size;
		curinst++;
	}

	offset = 0;

	/* reserve space to save LMF and caller saved registers */

	if (m->method->save_lmf) {
		offset += sizeof (MonoLMF);
	} else {
		if (m->used_int_regs & (1 << X86_EBX)) {
			offset += 4;
		}

		if (m->used_int_regs & (1 << X86_EDI)) {
			offset += 4;
		}

		if (m->used_int_regs & (1 << X86_ESI)) {
			offset += 4;
		}
	}

	for (i = curinst; i < m->num_varinfo; ++i) {
		inst = m->varinfo [i];

		if ((inst->flags & MONO_INST_IS_DEAD) || inst->opcode == OP_REGVAR)
			continue;

		/* inst->unused indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (inst->unused && MONO_TYPE_ISSTRUCT (inst->inst_vtype) && inst->inst_vtype->type != MONO_TYPE_TYPEDBYREF)
			size = mono_class_native_size (inst->inst_vtype->data.klass, &align);
		else
			size = mono_type_size (inst->inst_vtype, &align);

		offset += size;
		offset += align - 1;
		offset &= ~(align - 1);
		inst->opcode = OP_REGOFFSET;
		inst->inst_basereg = X86_EBP;
		inst->inst_offset = -offset;
		//g_print ("allocating local %d to %d\n", i, -offset);
	}
	offset += (MONO_ARCH_FRAME_ALIGNMENT - 1);
	offset &= ~(MONO_ARCH_FRAME_ALIGNMENT - 1);

	/* change sign? */
	m->stack_offset = -offset;
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
	int i, n, stack_size, type;
	MonoType *ptype;

	stack_size = 0;
	/* add the vararg cookie before the non-implicit args */
	if (call->signature->call_convention == MONO_CALL_VARARG) {
		MonoInst *sig_arg;
		/* FIXME: Add support for signature tokens to AOT */
		cfg->disable_aot = TRUE;
		MONO_INST_NEW (cfg, arg, OP_OUTARG);
		MONO_INST_NEW (cfg, sig_arg, OP_ICONST);
		sig_arg->inst_p0 = call->signature;
		arg->inst_left = sig_arg;
		arg->type = STACK_PTR;
		/* prepend, so they get reversed */
		arg->next = call->out_args;
		call->out_args = arg;
		stack_size += sizeof (gpointer);
	}
	sig = call->signature;
	n = sig->param_count + sig->hasthis;

	if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret))
		stack_size += sizeof (gpointer);
	for (i = 0; i < n; ++i) {
		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instrucion */
			in = call->args [i];
			stack_size += 4;
		} else {
			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code = in->cil_code;
			arg->inst_left = in;
			arg->type = in->type;
			/* prepend, so they get reversed */
			arg->next = call->out_args;
			call->out_args = arg;
			if (i >= sig->hasthis) {
				MonoType *t = sig->params [i - sig->hasthis];
				ptype = mono_type_get_underlying_type (t);
				if (t->byref)
					type = MONO_TYPE_U;
				else
					type = ptype->type;
				/* FIXME: validate arguments... */
				switch (type) {
				case MONO_TYPE_I:
				case MONO_TYPE_U:
				case MONO_TYPE_BOOLEAN:
				case MONO_TYPE_CHAR:
				case MONO_TYPE_I1:
				case MONO_TYPE_U1:
				case MONO_TYPE_I2:
				case MONO_TYPE_U2:
				case MONO_TYPE_I4:
				case MONO_TYPE_U4:
				case MONO_TYPE_STRING:
				case MONO_TYPE_CLASS:
				case MONO_TYPE_OBJECT:
				case MONO_TYPE_PTR:
				case MONO_TYPE_FNPTR:
				case MONO_TYPE_ARRAY:
				case MONO_TYPE_SZARRAY:
					stack_size += 4;
					break;
				case MONO_TYPE_I8:
				case MONO_TYPE_U8:
					stack_size += 8;
					break;
				case MONO_TYPE_R4:
					stack_size += 4;
					arg->opcode = OP_OUTARG_R4;
					break;
				case MONO_TYPE_R8:
					stack_size += 8;
					arg->opcode = OP_OUTARG_R8;
					break;
				case MONO_TYPE_VALUETYPE: {
					int size;
					if (sig->pinvoke) 
						size = mono_type_native_stack_size (&in->klass->byval_arg, NULL);
					else 
						size = mono_type_stack_size (&in->klass->byval_arg, NULL);

					stack_size += size;
					arg->opcode = OP_OUTARG_VT;
					arg->klass = in->klass;
					arg->unused = sig->pinvoke;
					arg->inst_imm = size; 
					break;
				}
				case MONO_TYPE_TYPEDBYREF:
					stack_size += sizeof (MonoTypedRef);
					arg->opcode = OP_OUTARG_VT;
					arg->klass = in->klass;
					arg->unused = sig->pinvoke;
					arg->inst_imm = sizeof (MonoTypedRef); 
					break;
				default:
					g_error ("unknown type 0x%02x in mono_arch_call_opcode\n", type);
				}
			} else {
				/* the this argument */
				stack_size += 4;
			}
		}
	}
	/* if the function returns a struct, the called method already does a ret $0x4 */
	if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret))
		stack_size -= 4;
	call->stack_usage = stack_size;
	/* 
	 * should set more info in call, such as the stack space
	 * used by the args that needs to be added back to esp
	 */

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

	/* if some args are passed in registers, we need to save them here */
	x86_push_reg (code, X86_EBP);
	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_METHODCONST, cfg->method);
	x86_push_imm (code, cfg->method);
	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	x86_call_code (code, 0);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);

	return code;
}

enum {
	SAVE_NONE,
	SAVE_STRUCT,
	SAVE_EAX,
	SAVE_EAX_EDX,
	SAVE_FP
};

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
	int arg_size = 0, save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;
	
	switch (mono_type_get_underlying_type (method->signature->ret)->type) {
	case MONO_TYPE_VOID:
		/* special case string .ctor icall */
		if (strcmp (".ctor", method->name) && method->klass == mono_defaults.string_class)
			save_mode = SAVE_EAX;
		else
			save_mode = SAVE_NONE;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		save_mode = SAVE_EAX_EDX;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		save_mode = SAVE_FP;
		break;
	case MONO_TYPE_VALUETYPE:
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_EAX;
		break;
	}

	switch (save_mode) {
	case SAVE_EAX_EDX:
		x86_push_reg (code, X86_EDX);
		x86_push_reg (code, X86_EAX);
		if (enable_arguments) {
			x86_push_reg (code, X86_EDX);
			x86_push_reg (code, X86_EAX);
			arg_size = 8;
		}
		break;
	case SAVE_EAX:
		x86_push_reg (code, X86_EAX);
		if (enable_arguments) {
			x86_push_reg (code, X86_EAX);
			arg_size = 4;
		}
		break;
	case SAVE_FP:
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
		x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
		if (enable_arguments) {
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
			x86_fst_membase (code, X86_ESP, 0, TRUE, TRUE);
			arg_size = 8;
		}
		break;
	case SAVE_STRUCT:
		if (enable_arguments) {
			x86_push_membase (code, X86_EBP, 8);
			arg_size = 4;
		}
		break;
	case SAVE_NONE:
	default:
		break;
	}


	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_METHODCONST, method);
	x86_push_imm (code, method);
	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_ABS, func);
	x86_call_code (code, 0);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, arg_size + 4);

	switch (save_mode) {
	case SAVE_EAX_EDX:
		x86_pop_reg (code, X86_EAX);
		x86_pop_reg (code, X86_EDX);
		break;
	case SAVE_EAX:
		x86_pop_reg (code, X86_EAX);
		break;
	case SAVE_FP:
		x86_fld_membase (code, X86_ESP, 0, TRUE);
		x86_alu_reg_imm (code, X86_ADD, X86_ESP, 8);
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}

#define EMIT_COND_BRANCH(ins,cond,sign) \
if (ins->flags & MONO_INST_BRLABEL) { \
        if (ins->inst_i0->inst_c0) { \
	        x86_branch (code, cond, cfg->native_code + ins->inst_i0->inst_c0, sign); \
        } else { \
	        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_LABEL, ins->inst_i0); \
	        if ((cfg->opt & MONO_OPT_BRANCH) && \
                    x86_is_imm8 (ins->inst_i0->inst_c1 - cpos)) \
		        x86_branch8 (code, cond, 0, sign); \
                else \
	                x86_branch32 (code, cond, 0, sign); \
        } \
} else { \
        if (ins->inst_true_bb->native_offset) { \
	        x86_branch (code, cond, cfg->native_code + ins->inst_true_bb->native_offset, sign); \
        } else { \
	        mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	        if ((cfg->opt & MONO_OPT_BRANCH) && \
                    x86_is_imm8 (ins->inst_true_bb->max_offset - cpos)) \
		        x86_branch8 (code, cond, 0, sign); \
                else \
	                x86_branch32 (code, cond, 0, sign); \
        } \
}

/* emit an exception if condition is fail */
#define EMIT_COND_SYSTEM_EXCEPTION(cond,signed,exc_name)            \
        do {                                                        \
		mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name);  \
	        x86_branch32 (code, cond, 0, signed);               \
	} while (0); 

#define EMIT_FPCOMPARE(code) do { \
	x86_fcompp (code); \
	x86_fnstsw (code); \
} while (0); 

/* FIXME: Add more instructions */
#define INST_IGNORES_CFLAGS(ins) (((ins)->opcode == CEE_BR) || ((ins)->opcode == OP_STORE_MEMBASE_IMM) || ((ins)->opcode == OP_STOREI4_MEMBASE_REG))

static void
peephole_pass (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *last_ins = NULL;
	ins = bb->code;

	while (ins) {

		switch (ins->opcode) {
		case OP_ICONST:
			/* reg = 0 -> XOR (reg, reg) */
			/* XOR sets cflags on x86, so we cant do it always */
			if (ins->inst_c0 == 0 && ins->next && INST_IGNORES_CFLAGS (ins->next)) {
				ins->opcode = CEE_XOR;
				ins->sreg1 = ins->dreg;
				ins->sreg2 = ins->dreg;
			}
			break;
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
		case OP_COMPARE_IMM:
			/* OP_COMPARE_IMM (reg, 0) 
			 * --> 
			 * OP_X86_TEST_NULL (reg) 
			 */
			if (!ins->inst_imm)
				ins->opcode = OP_X86_TEST_NULL;
			break;
		case OP_X86_COMPARE_MEMBASE_IMM:
			/* 
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_X86_COMPARE_MEMBASE_IMM offset(basereg), imm
			 * -->
			 * OP_STORE_MEMBASE_REG reg, offset(basereg)
			 * OP_COMPARE_IMM reg, imm
			 *
			 * Note: if imm = 0 then OP_COMPARE_IMM replaced with OP_X86_TEST_NULL
			 */
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
					ins->opcode = OP_COMPARE_IMM;
					ins->sreg1 = last_ins->sreg1;

					/* check if we can remove cmp reg,0 with test null */
					if (!ins->inst_imm)
						ins->opcode = OP_X86_TEST_NULL;
				}

			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
			/* 
			 * Note: if reg1 = reg2 the load op is removed
			 *
			 * OP_STORE_MEMBASE_REG reg1, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_STORE_MEMBASE_REG reg1, offset(basereg)
			 * OP_MOVE reg1, reg2
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
			 * Note: if reg1 = reg2 is equal then second load is removed
			 *
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
			/* 
			 * Note: if reg1 = reg2 the load op is removed
			 *
			 * OP_STORE_MEMBASE_REG reg1, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_STORE_MEMBASE_REG reg1, offset(basereg)
			 * OP_MOVE reg1, reg2
			 */
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
			/* 
			 * Note: if reg1 = reg2 the load op is removed
			 *
			 * OP_STORE_MEMBASE_REG reg1, offset(basereg) 
			 * OP_LOAD_MEMBASE offset(basereg), reg2
			 * -->
			 * OP_STORE_MEMBASE_REG reg1, offset(basereg)
			 * OP_MOVE reg1, reg2
			 */
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
			
		case OP_X86_PUSH_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI4_MEMBASE_REG ||
				         last_ins->opcode == OP_STORE_MEMBASE_REG) &&
			    ins->inst_basereg == last_ins->inst_destbasereg &&
			    ins->inst_offset == last_ins->inst_offset) {
				    ins->opcode = OP_X86_PUSH;
				    ins->sreg1 = last_ins->sreg1;
			}
			break;
		}
		last_ins = ins;
		ins = ins->next;
	}
	bb->last_ins = last_ins;
}

static const int 
branch_cc_table [] = {
	X86_CC_EQ, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_NE, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_O, X86_CC_NO, X86_CC_C, X86_CC_NC
};

#define DEBUG(a) if (cfg->verbose_level > 1) a
//#define DEBUG(a)

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
			cfg->stack_offset -= sizeof (gpointer);
			info->offset = cfg->stack_offset;
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
 * returns the offset used by spillvar. It allocates a new
 * spill float variable if necessary. 
 * (same as mono_spillvar_offset but for float)
 */
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
			cfg->stack_offset -= sizeof (double);
			info->offset = cfg->stack_offset;
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
 * Creates a store for spilled floating point items
 */
static MonoInst*
create_spilled_store_float (MonoCompile *cfg, int spill, int reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORER8_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = X86_EBP;
	store->inst_offset = mono_spillvar_offset_float (cfg, spill);

	DEBUG (g_print ("SPILLED FLOAT STORE (%d at 0x%08x(%%sp)) (from %d)\n", spill, store->inst_offset, reg));
	return store;
}

/*
 * Creates a load for spilled floating point items 
 */
static MonoInst*
create_spilled_load_float (MonoCompile *cfg, int spill, int reg, MonoInst *ins)
{
	MonoInst *load;
	MONO_INST_NEW (cfg, load, OP_LOADR8_SPILL_MEMBASE);
	load->dreg = reg;
	load->inst_basereg = X86_EBP;
	load->inst_offset = mono_spillvar_offset_float (cfg, spill);

	DEBUG (g_print ("SPILLED FLOAT LOAD (%d at 0x%08x(%%sp)) (from %d)\n", spill, load->inst_offset, reg));
	return load;
}

#define reg_is_freeable(r) ((r) >= 0 && (r) <= 7 && X86_IS_CALLEE ((r)))

typedef struct {
	int born_in;
	int killed_in;
	int last_use;
	int prev_use;
	int flags;		/* used to track fp spill/load */
} RegTrack;

static const char*const * ins_spec = pentium_desc;

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
	load->inst_basereg = X86_EBP;
	load->inst_offset = mono_spillvar_offset (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("SPILLED LOAD (%d at 0x%08x(%%ebp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_int (cfg->rs, 1 << sel);
	g_assert (i == sel);

	return sel;
}

static int
get_register_spilling (MonoCompile *cfg, InstList *item, MonoInst *ins, guint32 regmask, int reg)
{
	MonoInst *load;
	int i, sel, spill;

	DEBUG (g_print ("\tstart regmask to assign R%d: 0x%08x (R%d <- R%d R%d)\n", reg, regmask, ins->dreg, ins->sreg1, ins->sreg2));
	/* exclude the registers in the current instruction */
	if (reg != ins->sreg1 && (reg_is_freeable (ins->sreg1) || (ins->sreg1 >= MONO_MAX_IREGS && cfg->rs->iassign [ins->sreg1] >= 0))) {
		if (ins->sreg1 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg1]);
		else
			regmask &= ~ (1 << ins->sreg1);
		DEBUG (g_print ("\t\texcluding sreg1 %s\n", mono_arch_regname (ins->sreg1)));
	}
	if (reg != ins->sreg2 && (reg_is_freeable (ins->sreg2) || (ins->sreg2 >= MONO_MAX_IREGS && cfg->rs->iassign [ins->sreg2] >= 0))) {
		if (ins->sreg2 >= MONO_MAX_IREGS)
			regmask &= ~ (1 << cfg->rs->iassign [ins->sreg2]);
		else
			regmask &= ~ (1 << ins->sreg2);
		DEBUG (g_print ("\t\texcluding sreg2 %s %d\n", mono_arch_regname (ins->sreg2), ins->sreg2));
	}
	if (reg != ins->dreg && reg_is_freeable (ins->dreg)) {
		regmask &= ~ (1 << ins->dreg);
		DEBUG (g_print ("\t\texcluding dreg %s\n", mono_arch_regname (ins->dreg)));
	}

	DEBUG (g_print ("\t\tavailable regmask: 0x%08x\n", regmask));
	g_assert (regmask); /* need at least a register we can free */
	sel = -1;
	/* we should track prev_use and spill the register that's farther */
	for (i = 0; i < MONO_MAX_IREGS; ++i) {
		if (regmask & (1 << i)) {
			sel = i;
			DEBUG (g_print ("\t\tselected register %s has assignment %d\n", mono_arch_regname (sel), cfg->rs->iassign [sel]));
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
	load->inst_basereg = X86_EBP;
	load->inst_offset = mono_spillvar_offset (cfg, spill);
	if (item->prev) {
		while (ins->next != item->prev->data)
			ins = ins->next;
	}
	load->next = ins->next;
	ins->next = load;
	DEBUG (g_print ("\tSPILLED LOAD (%d at 0x%08x(%%ebp)) R%d (freed %s)\n", spill, load->inst_offset, i, mono_arch_regname (sel)));
	i = mono_regstate_alloc_int (cfg->rs, 1 << sel);
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
create_spilled_store (MonoCompile *cfg, int spill, int reg, int prev_reg, MonoInst *ins)
{
	MonoInst *store;
	MONO_INST_NEW (cfg, store, OP_STORE_MEMBASE_REG);
	store->sreg1 = reg;
	store->inst_destbasereg = X86_EBP;
	store->inst_offset = mono_spillvar_offset (cfg, spill);
	if (ins) {
		store->next = ins->next;
		ins->next = store;
	}
	DEBUG (g_print ("\tSPILLED STORE (%d at 0x%08x(%%ebp)) R%d (from %s)\n", spill, store->inst_offset, prev_reg, mono_arch_regname (reg)));
	return store;
}

static void
insert_before_ins (MonoInst *ins, InstList *item, MonoInst* to_insert)
{
	MonoInst *prev;
	if (item->next) {
		prev = item->next->data;

		while (prev->next != ins)
			prev = prev->next;
		to_insert->next = ins;
		prev->next = to_insert;
	} else {
		to_insert->next = ins;
	}
	/* 
	 * needed otherwise in the next instruction we can add an ins to the 
	 * end and that would get past this instruction.
	 */
	item->data = to_insert; 
}


#if  0
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
#endif

/* flags used in reginfo->flags */
enum {
	MONO_X86_FP_NEEDS_LOAD_SPILL	= 1 << 0,
	MONO_X86_FP_NEEDS_SPILL			= 1 << 1,
	MONO_X86_FP_NEEDS_LOAD			= 1 << 2,
	MONO_X86_REG_NOT_ECX			= 1 << 3,
	MONO_X86_REG_EAX				= 1 << 4,
	MONO_X86_REG_EDX				= 1 << 5,
	MONO_X86_REG_ECX				= 1 << 6
};

static int
mono_x86_alloc_int_reg (MonoCompile *cfg, InstList *tmp, MonoInst *ins, guint32 dest_mask, int sym_reg, int flags)
{
	int val;
	int test_mask = dest_mask;

	if (flags & MONO_X86_REG_EAX)
		test_mask &= (1 << X86_EAX);
	else if (flags & MONO_X86_REG_EDX)
		test_mask &= (1 << X86_EDX);
	else if (flags & MONO_X86_REG_ECX)
		test_mask &= (1 << X86_ECX);
	else if (flags & MONO_X86_REG_NOT_ECX)
		test_mask &= ~ (1 << X86_ECX);

	val = mono_regstate_alloc_int (cfg->rs, test_mask);
	if (val >= 0 && test_mask != dest_mask)
		DEBUG(g_print ("\tUsed flag to allocate reg %s for R%u\n", mono_arch_regname (val), sym_reg));

	if (val < 0 && (flags & MONO_X86_REG_NOT_ECX)) {
		DEBUG(g_print ("\tFailed to allocate flag suggested mask (%u) but exluding ECX\n", test_mask));
		val = mono_regstate_alloc_int (cfg->rs, (dest_mask & (~1 << X86_ECX)));
	}

	if (val < 0) {
		val = mono_regstate_alloc_int (cfg->rs, dest_mask);
		if (val < 0)
			val = get_register_spilling (cfg, tmp, ins, dest_mask, sym_reg);
	}

	return val;
}


/*#include "cprop.c"*/

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
	int i, val, fpcount;
	RegTrack *reginfo, *reginfof;
	RegTrack *reginfo1, *reginfo2, *reginfod;
	InstList *tmp, *reversed = NULL;
	const char *spec;
	guint32 src1_mask, src2_mask, dest_mask;
	GList *fspill_list = NULL;
	int fspill = 0;

	if (!bb->code)
		return;
	rs->next_vireg = bb->max_ireg;
	rs->next_vfreg = bb->max_freg;
	mono_regstate_assign (rs);
	reginfo = g_malloc0 (sizeof (RegTrack) * rs->next_vireg);
	reginfof = g_malloc0 (sizeof (RegTrack) * rs->next_vfreg);
	rs->ifree_mask = X86_CALLEE_REGS;

	ins = bb->code;

	/*if (cfg->opt & MONO_OPT_COPYPROP)
		local_copy_prop (cfg, ins);*/

	i = 1;
	fpcount = 0;
	DEBUG (g_print ("LOCAL regalloc: basic block: %d\n", bb->block_num));
	/* forward pass on the instructions to collect register liveness info */
	while (ins) {
		spec = ins_spec [ins->opcode];
		
		DEBUG (print_ins (i, ins));

		if (spec [MONO_INST_SRC1]) {
			if (spec [MONO_INST_SRC1] == 'f') {
				GList *spill;
				reginfo1 = reginfof;

				spill = g_list_first (fspill_list);
				if (spill && fpcount < MONO_MAX_FREGS) {
					reginfo1 [ins->sreg1].flags |= MONO_X86_FP_NEEDS_LOAD;
					fspill_list = g_list_remove (fspill_list, spill->data);
				} else
					fpcount--;
			}
			else
				reginfo1 = reginfo;
			reginfo1 [ins->sreg1].prev_use = reginfo1 [ins->sreg1].last_use;
			reginfo1 [ins->sreg1].last_use = i;
			if (spec [MONO_INST_SRC1] == 'L') {
				/* The virtual register is allocated sequentially */
				reginfo1 [ins->sreg1 + 1].prev_use = reginfo1 [ins->sreg1 + 1].last_use;
				reginfo1 [ins->sreg1 + 1].last_use = i;
				if (reginfo1 [ins->sreg1 + 1].born_in == 0 || reginfo1 [ins->sreg1 + 1].born_in > i)
					reginfo1 [ins->sreg1 + 1].born_in = i;

				reginfo1 [ins->sreg1].flags |= MONO_X86_REG_EAX;
				reginfo1 [ins->sreg1 + 1].flags |= MONO_X86_REG_EDX;
			}
		} else {
			ins->sreg1 = -1;
		}
		if (spec [MONO_INST_SRC2]) {
			if (spec [MONO_INST_SRC2] == 'f') {
				GList *spill;
				reginfo2 = reginfof;
				spill = g_list_first (fspill_list);
				if (spill) {
					reginfo2 [ins->sreg2].flags |= MONO_X86_FP_NEEDS_LOAD;
					fspill_list = g_list_remove (fspill_list, spill->data);
					if (fpcount >= MONO_MAX_FREGS) {
						fspill++;
						fspill_list = g_list_prepend (fspill_list, GINT_TO_POINTER(fspill));
						reginfo2 [ins->sreg2].flags |= MONO_X86_FP_NEEDS_LOAD_SPILL;
					}
				} else
					fpcount--;
			}
			else
				reginfo2 = reginfo;
			reginfo2 [ins->sreg2].prev_use = reginfo2 [ins->sreg2].last_use;
			reginfo2 [ins->sreg2].last_use = i;
			if (spec [MONO_INST_SRC2] == 'L') {
				/* The virtual register is allocated sequentially */
				reginfo2 [ins->sreg2 + 1].prev_use = reginfo2 [ins->sreg2 + 1].last_use;
				reginfo2 [ins->sreg2 + 1].last_use = i;
				if (reginfo2 [ins->sreg2 + 1].born_in == 0 || reginfo2 [ins->sreg2 + 1].born_in > i)
					reginfo2 [ins->sreg2 + 1].born_in = i;
			}
			if (spec [MONO_INST_CLOB] == 's') {
				reginfo2 [ins->sreg1].flags |= MONO_X86_REG_NOT_ECX;
				reginfo2 [ins->sreg2].flags |= MONO_X86_REG_ECX;
			}
		} else {
			ins->sreg2 = -1;
		}
		if (spec [MONO_INST_DEST]) {
			if (spec [MONO_INST_DEST] == 'f') {
				reginfod = reginfof;
				if (fpcount >= MONO_MAX_FREGS) {
					reginfod [ins->dreg].flags |= MONO_X86_FP_NEEDS_SPILL;
					fspill++;
					fspill_list = g_list_prepend (fspill_list, GINT_TO_POINTER(fspill));
					fpcount--;
				}
				fpcount++;
			}
			else
				reginfod = reginfo;
			if (spec [MONO_INST_DEST] != 'b') /* it's not just a base register */
				reginfod [ins->dreg].killed_in = i;
			reginfod [ins->dreg].prev_use = reginfod [ins->dreg].last_use;
			reginfod [ins->dreg].last_use = i;
			if (reginfod [ins->dreg].born_in == 0 || reginfod [ins->dreg].born_in > i)
				reginfod [ins->dreg].born_in = i;
			if (spec [MONO_INST_DEST] == 'l' || spec [MONO_INST_DEST] == 'L') {
				/* The virtual register is allocated sequentially */
				reginfod [ins->dreg + 1].prev_use = reginfod [ins->dreg + 1].last_use;
				reginfod [ins->dreg + 1].last_use = i;
				if (reginfod [ins->dreg + 1].born_in == 0 || reginfod [ins->dreg + 1].born_in > i)
					reginfod [ins->dreg + 1].born_in = i;

				reginfod [ins->dreg].flags |= MONO_X86_REG_EAX;
				reginfod [ins->dreg + 1].flags |= MONO_X86_REG_EDX;
			}
		} else {
			ins->dreg = -1;
		}

		reversed = inst_list_prepend (cfg->mempool, reversed, ins);
		++i;
		ins = ins->next;
	}

	// todo: check if we have anything left on fp stack, in verify mode?
	fspill = 0;

	DEBUG (print_regtrack (reginfo, rs->next_vireg));
	DEBUG (print_regtrack (reginfof, rs->next_vfreg));
	tmp = reversed;
	while (tmp) {
		int prev_dreg, prev_sreg1, prev_sreg2, clob_dreg;
		dest_mask = src1_mask = src2_mask = X86_CALLEE_REGS;
		--i;
		ins = tmp->data;
		spec = ins_spec [ins->opcode];
		prev_dreg = -1;
		clob_dreg = -1;
		DEBUG (g_print ("processing:"));
		DEBUG (print_ins (i, ins));
		if (spec [MONO_INST_CLOB] == 's') {
			if (rs->ifree_mask & (1 << X86_ECX)) {
				DEBUG (g_print ("\tshortcut assignment of R%d to ECX\n", ins->sreg2));
				rs->iassign [ins->sreg2] = X86_ECX;
				rs->isymbolic [X86_ECX] = ins->sreg2;
				ins->sreg2 = X86_ECX;
				rs->ifree_mask &= ~ (1 << X86_ECX);
			} else {
				int need_ecx_spill = TRUE;
				/* 
				 * we first check if src1/dreg is already assigned a register
				 * and then we force a spill of the var assigned to ECX.
				 */
				/* the destination register can't be ECX */
				dest_mask &= ~ (1 << X86_ECX);
				src1_mask &= ~ (1 << X86_ECX);
				val = rs->iassign [ins->dreg];
				/* 
				 * the destination register is already assigned to ECX:
				 * we need to allocate another register for it and then
				 * copy from this to ECX.
				 */
				if (val == X86_ECX && ins->dreg != ins->sreg2) {
					int new_dest;
					new_dest = mono_x86_alloc_int_reg (cfg, tmp, ins, dest_mask, ins->dreg, reginfo [ins->dreg].flags);
					g_assert (new_dest >= 0);
					DEBUG (g_print ("\tclob:s changing dreg R%d to %s from ECX\n", ins->dreg, mono_arch_regname (new_dest)));

					rs->isymbolic [new_dest] = ins->dreg;
					rs->iassign [ins->dreg] = new_dest;
					clob_dreg = ins->dreg;
					ins->dreg = new_dest;
					create_copy_ins (cfg, X86_ECX, new_dest, ins);
					need_ecx_spill = FALSE;
					/*DEBUG (g_print ("\tforced spill of R%d\n", ins->dreg));
					val = get_register_force_spilling (cfg, tmp, ins, ins->dreg);
					rs->iassign [ins->dreg] = val;
					rs->isymbolic [val] = prev_dreg;
					ins->dreg = val;*/
				}
				val = rs->iassign [ins->sreg1];
				if (val == X86_ECX) {
					g_assert_not_reached ();
				} else if (val >= 0) {
					/* 
					 * the first src reg was already assigned to a register,
					 * we need to copy it to the dest register because the 
					 * shift instruction clobbers the first operand.
					 */
					MonoInst *copy = create_copy_ins (cfg, ins->dreg, val, NULL);
					DEBUG (g_print ("\tclob:s moved sreg1 from R%d to R%d\n", val, ins->dreg));
					insert_before_ins (ins, tmp, copy);
				}
				val = rs->iassign [ins->sreg2];
				if (val >= 0 && val != X86_ECX) {
					MonoInst *move = create_copy_ins (cfg, X86_ECX, val, NULL);
					DEBUG (g_print ("\tmoved arg from R%d (%d) to ECX\n", val, ins->sreg2));
					move->next = ins;
					g_assert_not_reached ();
					/* FIXME: where is move connected to the instruction list? */
					//tmp->prev->data->next = move;
				}
				if (need_ecx_spill && !(rs->ifree_mask & (1 << X86_ECX))) {
					DEBUG (g_print ("\tforced spill of R%d\n", rs->isymbolic [X86_ECX]));
					get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [X86_ECX]);
					mono_regstate_free_int (rs, X86_ECX);
				}
				/* force-set sreg2 */
				rs->iassign [ins->sreg2] = X86_ECX;
				rs->isymbolic [X86_ECX] = ins->sreg2;
				ins->sreg2 = X86_ECX;
				rs->ifree_mask &= ~ (1 << X86_ECX);
			}
		} else if (spec [MONO_INST_CLOB] == 'd') { /* division */
			int dest_reg = X86_EAX;
			int clob_reg = X86_EDX;
			if (spec [MONO_INST_DEST] == 'd') {
				dest_reg = X86_EDX; /* reminder */
				clob_reg = X86_EAX;
			}
			val = rs->iassign [ins->dreg];
			if (0 && val >= 0 && val != dest_reg && !(rs->ifree_mask & (1 << dest_reg))) {
				DEBUG (g_print ("\tforced spill of R%d\n", rs->isymbolic [dest_reg]));
				get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [dest_reg]);
				mono_regstate_free_int (rs, dest_reg);
			}
			if (val < 0) {
				if (val < -1) {
					/* the register gets spilled after this inst */
					int spill = -val -1;
					dest_mask = 1 << clob_reg;
					prev_dreg = ins->dreg;
					val = mono_regstate_alloc_int (rs, dest_mask);
					if (val < 0)
						val = get_register_spilling (cfg, tmp, ins, dest_mask, ins->dreg);
					rs->iassign [ins->dreg] = val;
					if (spill)
						create_spilled_store (cfg, spill, val, prev_dreg, ins);
					DEBUG (g_print ("\tassigned dreg %s to dest R%d\n", mono_arch_regname (val), ins->dreg));
					rs->isymbolic [val] = prev_dreg;
					ins->dreg = val;
					if (val != dest_reg) { /* force a copy */
						create_copy_ins (cfg, val, dest_reg, ins);
					}
				} else {
					DEBUG (g_print ("\tshortcut assignment of R%d to %s\n", ins->dreg, mono_arch_regname (dest_reg)));
					prev_dreg = ins->dreg;
					rs->iassign [ins->dreg] = dest_reg;
					rs->isymbolic [dest_reg] = ins->dreg;
					ins->dreg = dest_reg;
					rs->ifree_mask &= ~ (1 << dest_reg);
				}
			} else {
				//DEBUG (g_print ("dest reg in div assigned: %s\n", mono_arch_regname (val)));
				if (val != dest_reg) { /* force a copy */
					create_copy_ins (cfg, val, dest_reg, ins);
					if (!(rs->ifree_mask & (1 << dest_reg)) && rs->isymbolic [dest_reg] >= MONO_MAX_IREGS) {
						DEBUG (g_print ("\tforced spill of R%d\n", rs->isymbolic [dest_reg]));
						get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [dest_reg]);
						mono_regstate_free_int (rs, dest_reg);
					}
				}
			}
			if (!(rs->ifree_mask & (1 << clob_reg)) && (clob_reg != val) && (rs->isymbolic [clob_reg] >= 8)) {
				DEBUG (g_print ("\tforced spill of clobbered reg R%d\n", rs->isymbolic [clob_reg]));
				get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [clob_reg]);
				mono_regstate_free_int (rs, clob_reg);
			}
			src1_mask = 1 << X86_EAX;
			src2_mask = 1 << X86_ECX;
		}
		if (spec [MONO_INST_DEST] == 'l') {
			int hreg;
			val = rs->iassign [ins->dreg];
			/* check special case when dreg have been moved from ecx (clob shift) */
			if (spec [MONO_INST_CLOB] == 's' && clob_dreg != -1)
				hreg = clob_dreg + 1;
			else
				hreg = ins->dreg + 1;

			/* base prev_dreg on fixed hreg, handle clob case */
			val = hreg - 1;

			if (val != rs->isymbolic [X86_EAX] && !(rs->ifree_mask & (1 << X86_EAX))) {
				DEBUG (g_print ("\t(long-low) forced spill of R%d\n", rs->isymbolic [X86_EAX]));
				get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [X86_EAX]);
				mono_regstate_free_int (rs, X86_EAX);
			}
			if (hreg != rs->isymbolic [X86_EDX] && !(rs->ifree_mask & (1 << X86_EDX))) {
				DEBUG (g_print ("\t(long-high) forced spill of R%d\n", rs->isymbolic [X86_EDX]));
				get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [X86_EDX]);
				mono_regstate_free_int (rs, X86_EDX);
			}
		}

		/* Track dreg */
		if (spec [MONO_INST_DEST] == 'f') {
			if (reginfof [ins->dreg].flags & MONO_X86_FP_NEEDS_SPILL) {
				GList *spill_node;
				MonoInst *store;
				spill_node = g_list_first (fspill_list);
				g_assert (spill_node);

				store = create_spilled_store_float (cfg, GPOINTER_TO_INT (spill_node->data), ins->dreg, ins);
				insert_before_ins (ins, tmp, store);
				fspill_list = g_list_remove (fspill_list, spill_node->data);
				fspill--;
			}
		} else if (spec [MONO_INST_DEST] == 'L') {
			int hreg;
			val = rs->iassign [ins->dreg];
			/* check special case when dreg have been moved from ecx (clob shift) */
			if (spec [MONO_INST_CLOB] == 's' && clob_dreg != -1)
				hreg = clob_dreg + 1;
			else
				hreg = ins->dreg + 1;

			/* base prev_dreg on fixed hreg, handle clob case */
			prev_dreg = hreg - 1;

			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_x86_alloc_int_reg (cfg, tmp, ins, dest_mask, ins->dreg, reginfo [ins->dreg].flags);
				rs->iassign [ins->dreg] = val;
				if (spill)
					create_spilled_store (cfg, spill, val, prev_dreg, ins);
			}

			DEBUG (g_print ("\tassigned dreg (long) %s to dest R%d\n", mono_arch_regname (val), hreg - 1));
 
			rs->isymbolic [val] = hreg - 1;
			ins->dreg = val;
			
			val = rs->iassign [hreg];
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_x86_alloc_int_reg (cfg, tmp, ins, dest_mask, hreg, reginfo [hreg].flags);
				rs->iassign [hreg] = val;
				if (spill)
					create_spilled_store (cfg, spill, val, hreg, ins);
			}

			DEBUG (g_print ("\tassigned hreg (long-high) %s to dest R%d\n", mono_arch_regname (val), hreg));
			rs->isymbolic [val] = hreg;
			/* save reg allocating into unused */
			ins->unused = val;

			/* check if we can free our long reg */
			if (reg_is_freeable (val) && hreg >= 0 && reginfo [hreg].born_in >= i) {
				DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (val), hreg, reginfo [hreg].born_in));
				mono_regstate_free_int (rs, val);
			}
		}
		else if (ins->dreg >= MONO_MAX_IREGS) {
			int hreg;
			val = rs->iassign [ins->dreg];
			if (spec [MONO_INST_DEST] == 'l') {
				/* check special case when dreg have been moved from ecx (clob shift) */
				if (spec [MONO_INST_CLOB] == 's' && clob_dreg != -1)
					hreg = clob_dreg + 1;
				else
					hreg = ins->dreg + 1;

				/* base prev_dreg on fixed hreg, handle clob case */
				prev_dreg = hreg - 1;
			} else
				prev_dreg = ins->dreg;

			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_x86_alloc_int_reg (cfg, tmp, ins, dest_mask, ins->dreg, reginfo [ins->dreg].flags);
				rs->iassign [ins->dreg] = val;
				if (spill)
					create_spilled_store (cfg, spill, val, prev_dreg, ins);
			}
			DEBUG (g_print ("\tassigned dreg %s to dest R%d\n", mono_arch_regname (val), ins->dreg));
			rs->isymbolic [val] = prev_dreg;
			ins->dreg = val;
			/* handle cases where lreg needs to be eax:edx */
			if (spec [MONO_INST_DEST] == 'l') {
				/* check special case when dreg have been moved from ecx (clob shift) */
				int hreg = prev_dreg + 1;
				val = rs->iassign [hreg];
				if (val < 0) {
					int spill = 0;
					if (val < -1) {
						/* the register gets spilled after this inst */
						spill = -val -1;
					}
					val = mono_x86_alloc_int_reg (cfg, tmp, ins, dest_mask, hreg, reginfo [hreg].flags);
					rs->iassign [hreg] = val;
					if (spill)
						create_spilled_store (cfg, spill, val, hreg, ins);
				}
				DEBUG (g_print ("\tassigned hreg %s to dest R%d\n", mono_arch_regname (val), hreg));
				rs->isymbolic [val] = hreg;
				if (ins->dreg == X86_EAX) {
					if (val != X86_EDX)
						create_copy_ins (cfg, val, X86_EDX, ins);
				} else if (ins->dreg == X86_EDX) {
					if (val == X86_EAX) {
						/* swap */
						g_assert_not_reached ();
					} else {
						/* two forced copies */
						create_copy_ins (cfg, val, X86_EDX, ins);
						create_copy_ins (cfg, ins->dreg, X86_EAX, ins);
					}
				} else {
					if (val == X86_EDX) {
						create_copy_ins (cfg, ins->dreg, X86_EAX, ins);
					} else {
						/* two forced copies */
						create_copy_ins (cfg, val, X86_EDX, ins);
						create_copy_ins (cfg, ins->dreg, X86_EAX, ins);
					}
				}
				if (reg_is_freeable (val) && hreg >= 0 && reginfo [hreg].born_in >= i) {
					DEBUG (g_print ("\tfreeable %s (R%d)\n", mono_arch_regname (val), hreg));
					mono_regstate_free_int (rs, val);
				}
			} else if (spec [MONO_INST_DEST] == 'a' && ins->dreg != X86_EAX && spec [MONO_INST_CLOB] != 'd') {
				/* this instruction only outputs to EAX, need to copy */
				create_copy_ins (cfg, ins->dreg, X86_EAX, ins);
			} else if (spec [MONO_INST_DEST] == 'd' && ins->dreg != X86_EDX && spec [MONO_INST_CLOB] != 'd') {
				create_copy_ins (cfg, ins->dreg, X86_EDX, ins);
			}
		}
		if (spec [MONO_INST_DEST] != 'f' && reg_is_freeable (ins->dreg) && prev_dreg >= 0 && reginfo [prev_dreg].born_in >= i) {
			DEBUG (g_print ("\tfreeable %s (R%d) (born in %d)\n", mono_arch_regname (ins->dreg), prev_dreg, reginfo [prev_dreg].born_in));
			mono_regstate_free_int (rs, ins->dreg);
		}
		/* put src1 in EAX if it needs to be */
		if (spec [MONO_INST_SRC1] == 'a') {
			if (!(rs->ifree_mask & (1 << X86_EAX))) {
				DEBUG (g_print ("\tforced spill of R%d\n", rs->isymbolic [X86_EAX]));
				get_register_force_spilling (cfg, tmp, ins, rs->isymbolic [X86_EAX]);
				mono_regstate_free_int (rs, X86_EAX);
			}
			/* force-set sreg1 */
			rs->iassign [ins->sreg1] = X86_EAX;
			rs->isymbolic [X86_EAX] = ins->sreg1;
			ins->sreg1 = X86_EAX;
			rs->ifree_mask &= ~ (1 << X86_EAX);
		}

		/* Track sreg1 */
		if (spec [MONO_INST_SRC1] == 'f') {
			if (reginfof [ins->sreg1].flags & MONO_X86_FP_NEEDS_LOAD) {
				MonoInst *load;
				MonoInst *store = NULL;

				if (reginfof [ins->sreg1].flags & MONO_X86_FP_NEEDS_LOAD_SPILL) {
					GList *spill_node;
					spill_node = g_list_first (fspill_list);
					g_assert (spill_node);

					store = create_spilled_store_float (cfg, GPOINTER_TO_INT (spill_node->data), ins->sreg1, ins);		
					fspill_list = g_list_remove (fspill_list, spill_node->data);
				}

				fspill++;
				fspill_list = g_list_prepend (fspill_list, GINT_TO_POINTER(fspill));
				load = create_spilled_load_float (cfg, fspill, ins->sreg1, ins);
				insert_before_ins (ins, tmp, load);
				if (store) 
					insert_before_ins (load, tmp, store);
			}
		} else if ((spec [MONO_INST_DEST] == 'L') && (spec [MONO_INST_SRC1] == 'L')) {
			/* force source to be same as dest */
			rs->iassign [ins->sreg1] = ins->dreg;
			rs->iassign [ins->sreg1 + 1] = ins->unused;
			rs->isymbolic [ins->dreg] = ins->sreg1;
			rs->isymbolic [ins->unused] = ins->sreg1 + 1;

			DEBUG (g_print ("\tassigned sreg1 (long) %s to sreg1 R%d\n", mono_arch_regname (ins->dreg), ins->sreg1));
			DEBUG (g_print ("\tassigned sreg1 (long-high) %s to sreg1 R%d\n", mono_arch_regname (ins->unused), ins->sreg1 + 1));

			ins->sreg1 = ins->dreg;
			/* 
			 * No need for saving the reg, we know that src1=dest in this cases
			 * ins->inst_c0 = ins->unused;
			 */

			/* make sure that we remove them from free mask */
			rs->ifree_mask &= ~ (1 << ins->dreg);
			rs->ifree_mask &= ~ (1 << ins->unused);
		}
		else if (ins->sreg1 >= MONO_MAX_IREGS) {
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
					val = mono_x86_alloc_int_reg (cfg, tmp, ins, src1_mask, ins->sreg1, reginfo [ins->sreg1].flags);
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
		/* handle clobbering of sreg1 */
		if ((spec [MONO_INST_CLOB] == '1' || spec [MONO_INST_CLOB] == 's') && ins->dreg != ins->sreg1) {
			MonoInst *copy = create_copy_ins (cfg, ins->dreg, ins->sreg1, NULL);
			DEBUG (g_print ("\tneed to copy sreg1 %s to dreg %s\n", mono_arch_regname (ins->sreg1), mono_arch_regname (ins->dreg)));
			if (ins->sreg2 == -1 || spec [MONO_INST_CLOB] == 's') {
				/* note: the copy is inserted before the current instruction! */
				insert_before_ins (ins, tmp, copy);
				/* we set sreg1 to dest as well */
				prev_sreg1 = ins->sreg1 = ins->dreg;
			} else {
				/* inserted after the operation */
				copy->next = ins->next;
				ins->next = copy;
			}
		}
		/* track sreg2 */
		if (spec [MONO_INST_SRC2] == 'f') {
			if (reginfof [ins->sreg2].flags & MONO_X86_FP_NEEDS_LOAD) {
				MonoInst *load;
				MonoInst *store = NULL;

				if (reginfof [ins->sreg2].flags & MONO_X86_FP_NEEDS_LOAD_SPILL) {
					GList *spill_node;

					spill_node = g_list_first (fspill_list);
					g_assert (spill_node);
					if (spec [MONO_INST_SRC1] == 'f' && (reginfof [ins->sreg1].flags & MONO_X86_FP_NEEDS_LOAD_SPILL))
						spill_node = g_list_next (spill_node);
	
					store = create_spilled_store_float (cfg, GPOINTER_TO_INT (spill_node->data), ins->sreg2, ins);
					fspill_list = g_list_remove (fspill_list, spill_node->data);
				} 
				
				fspill++;
				fspill_list = g_list_prepend (fspill_list, GINT_TO_POINTER(fspill));
				load = create_spilled_load_float (cfg, fspill, ins->sreg2, ins);
				insert_before_ins (ins, tmp, load);
				if (store) 
					insert_before_ins (load, tmp, store);
			}
		} 
		else if (ins->sreg2 >= MONO_MAX_IREGS) {
			val = rs->iassign [ins->sreg2];
			prev_sreg2 = ins->sreg2;
			if (val < 0) {
				int spill = 0;
				if (val < -1) {
					/* the register gets spilled after this inst */
					spill = -val -1;
				}
				val = mono_x86_alloc_int_reg (cfg, tmp, ins, src2_mask, ins->sreg2, reginfo [ins->sreg2].flags);
				rs->iassign [ins->sreg2] = val;
				DEBUG (g_print ("\tassigned sreg2 %s to R%d\n", mono_arch_regname (val), ins->sreg2));
				if (spill)
					create_spilled_store (cfg, spill, val, prev_sreg2, ins);
			}
			rs->isymbolic [val] = prev_sreg2;
			ins->sreg2 = val;
			if (spec [MONO_INST_CLOB] == 's' && ins->sreg2 != X86_ECX) {
				DEBUG (g_print ("\tassigned sreg2 %s to R%d, but ECX is needed (R%d)\n", mono_arch_regname (val), ins->sreg2, rs->iassign [X86_ECX]));
			}
		} else {
			prev_sreg2 = -1;
		}

		if (spec [MONO_INST_CLOB] == 'c') {
			int j, s;
			guint32 clob_mask = X86_CALLEE_REGS;
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
		/* this may result from a insert_before call */
		if (!tmp->next)
			bb->code = tmp->data;
		tmp = tmp->next;
	}

	g_free (reginfo);
	g_free (reginfof);
	g_list_free (fspill_list);
}

static unsigned char*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int size, gboolean is_signed)
{
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);
	x86_fnstcw_membase(code, X86_ESP, 0);
	x86_mov_reg_membase (code, dreg, X86_ESP, 0, 2);
	x86_alu_reg_imm (code, X86_OR, dreg, 0xc00);
	x86_mov_membase_reg (code, X86_ESP, 2, dreg, 2);
	x86_fldcw_membase (code, X86_ESP, 2);
	if (size == 8) {
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
		x86_fist_pop_membase (code, X86_ESP, 0, TRUE);
		x86_pop_reg (code, dreg);
		/* FIXME: need the high register 
		 * x86_pop_reg (code, dreg_high);
		 */
	} else {
		x86_push_reg (code, X86_EAX); // SP = SP - 4
		x86_fist_pop_membase (code, X86_ESP, 0, FALSE);
		x86_pop_reg (code, dreg);
	}
	x86_fldcw_membase (code, X86_ESP, 0);
	x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);

	if (size == 1)
		x86_widen_reg (code, dreg, dreg, is_signed, FALSE);
	else if (size == 2)
		x86_widen_reg (code, dreg, dreg, is_signed, TRUE);
	return code;
}

static unsigned char*
mono_emit_stack_alloc (guchar *code, MonoInst* tree)
{
	int sreg = tree->sreg1;
#ifdef PLATFORM_WIN32
	guint8* br[5];

	/*
	 * Under Windows:
	 * If requested stack size is larger than one page,
	 * perform stack-touch operation
	 */
	/*
	 * Generate stack probe code.
	 * Under Windows, it is necessary to allocate one page at a time,
	 * "touching" stack after each successful sub-allocation. This is
	 * because of the way stack growth is implemented - there is a
	 * guard page before the lowest stack page that is currently commited.
	 * Stack normally grows sequentially so OS traps access to the
	 * guard page and commits more pages when needed.
	 */
	x86_test_reg_imm (code, sreg, ~0xFFF);
	br[0] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);

	br[2] = code; /* loop */
	x86_alu_reg_imm (code, X86_SUB, X86_ESP, 0x1000);
	x86_test_membase_reg (code, X86_ESP, 0, X86_ESP);
	x86_alu_reg_imm (code, X86_SUB, sreg, 0x1000);
	x86_alu_reg_imm (code, X86_CMP, sreg, 0x1000);
	br[3] = code; x86_branch8 (code, X86_CC_AE, 0, FALSE);
	x86_patch (br[3], br[2]);
	x86_test_reg_reg (code, sreg, sreg);
	br[4] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, sreg);

	br[1] = code; x86_jump8 (code, 0);

	x86_patch (br[0], code);
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, sreg);
	x86_patch (br[1], code);
	x86_patch (br[4], code);
#else /* PLATFORM_WIN32 */
	x86_alu_reg_reg (code, X86_SUB, X86_ESP, tree->sreg1);
#endif
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
	return code;
}

#define REAL_PRINT_REG(text,reg) \
mono_assert (reg >= 0); \
x86_push_reg (code, X86_EAX); \
x86_push_reg (code, X86_EDX); \
x86_push_reg (code, X86_ECX); \
x86_push_reg (code, reg); \
x86_push_imm (code, reg); \
x86_push_imm (code, text " %d %p\n"); \
x86_mov_reg_imm (code, X86_EAX, printf); \
x86_call_reg (code, X86_EAX); \
x86_alu_reg_imm (code, X86_ADD, X86_ESP, 3*4); \
x86_pop_reg (code, X86_ECX); \
x86_pop_reg (code, X86_EDX); \
x86_pop_reg (code, X86_EAX);

/* benchmark and set based on cpu */
#define LOOP_ALIGNMENT 8
#define bb_is_loop_start(bb) ((bb)->loop_body_start && (bb)->nesting)

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

	if (cfg->opt & MONO_OPT_LOOP) {
		int pad, align = LOOP_ALIGNMENT;
		/* set alignment depending on cpu */
		if (bb_is_loop_start (bb) && (pad = (cfg->code_len & (align - 1)))) {
			pad = align - pad;
			/*g_print ("adding %d pad at %x to loop in %s\n", pad, cfg->code_len, cfg->method->name);*/
			x86_padding (code, pad);
			cfg->code_len += pad;
			bb->native_offset = cfg->code_len;
		}
	}

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		MonoProfileCoverageInfo *cov = cfg->coverage_info;
		g_assert (!mono_compile_aot);
		cpos += 6;

		cov->data [bb->dfn].cil_code = bb->cil_code;
		/* this is not thread save, but good enough */
		x86_inc_mem (code, &cov->data [bb->dfn].count); 
	}

	offset = code - cfg->native_code;

	ins = bb->code;
	while (ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_spec [ins->opcode])[MONO_INST_LEN];

		if (offset > (cfg->code_size - max_len - 16)) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
			mono_jit_stats.code_reallocs++;
		}

		mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_BIGMUL:
			x86_mul_reg (code, ins->sreg2, TRUE);
			break;
		case OP_BIGMUL_UN:
			x86_mul_reg (code, ins->sreg2, FALSE);
			break;
		case OP_X86_SETEQ_MEMBASE:
		case OP_X86_SETNE_MEMBASE:
			x86_set_membase (code, ins->opcode == OP_X86_SETEQ_MEMBASE ? X86_CC_EQ : X86_CC_NE,
		                         ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			x86_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 1);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			x86_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 2);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			x86_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_STOREI1_MEMBASE_REG:
			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 1);
			break;
		case OP_STOREI2_MEMBASE_REG:
			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 2);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			x86_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 4);
			break;
		case CEE_LDIND_I:
		case CEE_LDIND_I4:
		case CEE_LDIND_U4:
			x86_mov_reg_mem (code, ins->dreg, ins->inst_p0, 4);
			break;
		case OP_LOADU4_MEM:
			x86_mov_reg_imm (code, ins->dreg, ins->inst_p0);
			x86_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 4);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			x86_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_LOADU1_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, FALSE);
			break;
		case OP_LOADI1_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, FALSE);
			break;
		case OP_LOADU2_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, TRUE);
			break;
		case OP_LOADI2_MEMBASE:
			x86_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, TRUE);
			break;
		case CEE_CONV_I1:
			x86_widen_reg (code, ins->dreg, ins->sreg1, TRUE, FALSE);
			break;
		case CEE_CONV_I2:
			x86_widen_reg (code, ins->dreg, ins->sreg1, TRUE, TRUE);
			break;
		case CEE_CONV_U1:
			x86_widen_reg (code, ins->dreg, ins->sreg1, FALSE, FALSE);
			break;
		case CEE_CONV_U2:
			x86_widen_reg (code, ins->dreg, ins->sreg1, FALSE, TRUE);
			break;
		case OP_COMPARE:
			x86_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
			x86_alu_reg_imm (code, X86_CMP, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_COMPARE_MEMBASE_REG:
			x86_alu_membase_reg (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->sreg2);
			break;
		case OP_X86_COMPARE_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_COMPARE_MEMBASE8_IMM:
			x86_alu_membase8_imm (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_COMPARE_REG_MEMBASE:
			x86_alu_reg_membase (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_TEST_NULL:
			x86_test_reg_reg (code, ins->sreg1, ins->sreg1);
			break;
		case OP_X86_ADD_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_ADD_MEMBASE:
			x86_alu_reg_membase (code, X86_ADD, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_SUB_MEMBASE_IMM:
			x86_alu_membase_imm (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->inst_imm);
			break;
		case OP_X86_SUB_MEMBASE:
			x86_alu_reg_membase (code, X86_SUB, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_INC_MEMBASE:
			x86_inc_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_INC_REG:
			x86_inc_reg (code, ins->dreg);
			break;
		case OP_X86_DEC_MEMBASE:
			x86_dec_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_DEC_REG:
			x86_dec_reg (code, ins->dreg);
			break;
		case OP_X86_MUL_MEMBASE:
			x86_imul_reg_membase (code, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case CEE_BREAK:
			x86_breakpoint (code);
			break;
		case OP_ADDCC:
		case CEE_ADD:
			x86_alu_reg_reg (code, X86_ADD, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
			x86_alu_reg_reg (code, X86_ADC, ins->sreg1, ins->sreg2);
			break;
		case OP_ADDCC_IMM:
		case OP_ADD_IMM:
			x86_alu_reg_imm (code, X86_ADD, ins->dreg, ins->inst_imm);
			break;
		case OP_ADC_IMM:
			x86_alu_reg_imm (code, X86_ADC, ins->dreg, ins->inst_imm);
			break;
		case OP_SUBCC:
		case CEE_SUB:
			x86_alu_reg_reg (code, X86_SUB, ins->sreg1, ins->sreg2);
			break;
		case OP_SBB:
			x86_alu_reg_reg (code, X86_SBB, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBCC_IMM:
		case OP_SUB_IMM:
			x86_alu_reg_imm (code, X86_SUB, ins->dreg, ins->inst_imm);
			break;
		case OP_SBB_IMM:
			x86_alu_reg_imm (code, X86_SBB, ins->dreg, ins->inst_imm);
			break;
		case CEE_AND:
			x86_alu_reg_reg (code, X86_AND, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
			x86_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_imm);
			break;
		case CEE_DIV:
			x86_cdq (code);
			x86_div_reg (code, ins->sreg2, TRUE);
			break;
		case CEE_DIV_UN:
			x86_alu_reg_reg (code, X86_XOR, X86_EDX, X86_EDX);
			x86_div_reg (code, ins->sreg2, FALSE);
			break;
		case OP_DIV_IMM:
			x86_mov_reg_imm (code, ins->sreg2, ins->inst_imm);
			x86_cdq (code);
			x86_div_reg (code, ins->sreg2, TRUE);
			break;
		case CEE_REM:
			x86_cdq (code);
			x86_div_reg (code, ins->sreg2, TRUE);
			break;
		case CEE_REM_UN:
			x86_alu_reg_reg (code, X86_XOR, X86_EDX, X86_EDX);
			x86_div_reg (code, ins->sreg2, FALSE);
			break;
		case OP_REM_IMM:
			x86_mov_reg_imm (code, ins->sreg2, ins->inst_imm);
			x86_cdq (code);
			x86_div_reg (code, ins->sreg2, TRUE);
			break;
		case CEE_OR:
			x86_alu_reg_reg (code, X86_OR, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
			x86_alu_reg_imm (code, X86_OR, ins->sreg1, ins->inst_imm);
			break;
		case CEE_XOR:
			x86_alu_reg_reg (code, X86_XOR, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
			x86_alu_reg_imm (code, X86_XOR, ins->sreg1, ins->inst_imm);
			break;
		case CEE_SHL:
			g_assert (ins->sreg2 == X86_ECX);
			x86_shift_reg (code, X86_SHL, ins->dreg);
			break;
		case CEE_SHR:
			g_assert (ins->sreg2 == X86_ECX);
			x86_shift_reg (code, X86_SAR, ins->dreg);
			break;
		case OP_SHR_IMM:
			x86_shift_reg_imm (code, X86_SAR, ins->dreg, ins->inst_imm);
			break;
		case OP_SHR_UN_IMM:
			x86_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_imm);
			break;
		case CEE_SHR_UN:
			g_assert (ins->sreg2 == X86_ECX);
			x86_shift_reg (code, X86_SHR, ins->dreg);
			break;
		case OP_SHL_IMM:
			x86_shift_reg_imm (code, X86_SHL, ins->dreg, ins->inst_imm);
			break;
		case OP_LSHL: {
			guint8 *jump_to_end;

			/* handle shifts below 32 bits */
			x86_shld_reg (code, ins->unused, ins->sreg1);
			x86_shift_reg (code, X86_SHL, ins->sreg1);

			x86_test_reg_imm (code, X86_ECX, 32);
			jump_to_end = code; x86_branch8 (code, X86_CC_EQ, 0, TRUE);

			/* handle shift over 32 bit */
			x86_mov_reg_reg (code, ins->unused, ins->sreg1, 4);
			x86_clear_reg (code, ins->sreg1);
			
			x86_patch (jump_to_end, code);
			}
			break;
		case OP_LSHR: {
			guint8 *jump_to_end;

			/* handle shifts below 32 bits */
			x86_shrd_reg (code, ins->sreg1, ins->unused);
			x86_shift_reg (code, X86_SAR, ins->unused);

			x86_test_reg_imm (code, X86_ECX, 32);
			jump_to_end = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);

			/* handle shifts over 31 bits */
			x86_mov_reg_reg (code, ins->sreg1, ins->unused, 4);
			x86_shift_reg_imm (code, X86_SAR, ins->unused, 31);
			
			x86_patch (jump_to_end, code);
			}
			break;
		case OP_LSHR_UN: {
			guint8 *jump_to_end;

			/* handle shifts below 32 bits */
			x86_shrd_reg (code, ins->sreg1, ins->unused);
			x86_shift_reg (code, X86_SHR, ins->unused);

			x86_test_reg_imm (code, X86_ECX, 32);
			jump_to_end = code; x86_branch8 (code, X86_CC_EQ, 0, FALSE);

			/* handle shifts over 31 bits */
			x86_mov_reg_reg (code, ins->sreg1, ins->unused, 4);
			x86_shift_reg_imm (code, X86_SHR, ins->unused, 31);
			
			x86_patch (jump_to_end, code);
			}
			break;
		case OP_LSHL_IMM:
			if (ins->inst_imm >= 32) {
				x86_mov_reg_reg (code, ins->unused, ins->sreg1, 4);
				x86_clear_reg (code, ins->sreg1);
				x86_shift_reg_imm (code, X86_SHL, ins->unused, ins->inst_imm - 32);
			} else {
				x86_shld_reg_imm (code, ins->unused, ins->sreg1, ins->inst_imm);
				x86_shift_reg_imm (code, X86_SHL, ins->sreg1, ins->inst_imm);
			}
			break;
		case OP_LSHR_IMM:
			if (ins->inst_imm >= 32) {
				x86_mov_reg_reg (code, ins->sreg1, ins->unused,  4);
				x86_shift_reg_imm (code, X86_SAR, ins->unused, 0x1f);
				x86_shift_reg_imm (code, X86_SAR, ins->sreg1, ins->inst_imm - 32);
			} else {
				x86_shrd_reg_imm (code, ins->sreg1, ins->unused, ins->inst_imm);
				x86_shift_reg_imm (code, X86_SAR, ins->unused, ins->inst_imm);
			}
			break;
		case OP_LSHR_UN_IMM:
			if (ins->inst_imm >= 32) {
				x86_mov_reg_reg (code, ins->sreg1, ins->unused, 4);
				x86_clear_reg (code, ins->unused);
				x86_shift_reg_imm (code, X86_SHR, ins->sreg1, ins->inst_imm - 32);
			} else {
				x86_shrd_reg_imm (code, ins->sreg1, ins->unused, ins->inst_imm);
				x86_shift_reg_imm (code, X86_SHR, ins->unused, ins->inst_imm);
			}
			break;
		case CEE_NOT:
			x86_not_reg (code, ins->sreg1);
			break;
		case CEE_NEG:
			x86_neg_reg (code, ins->sreg1);
			break;
		case OP_SEXT_I1:
			x86_widen_reg (code, ins->dreg, ins->sreg1, TRUE, FALSE);
			break;
		case OP_SEXT_I2:
			x86_widen_reg (code, ins->dreg, ins->sreg1, TRUE, TRUE);
			break;
		case CEE_MUL:
			x86_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MUL_IMM:
			x86_imul_reg_reg_imm (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case CEE_MUL_OVF:
			x86_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		case CEE_MUL_OVF_UN: {
			/* the mul operation and the exception check should most likely be split */
			int non_eax_reg, saved_eax = FALSE, saved_edx = FALSE;
			/*g_assert (ins->sreg2 == X86_EAX);
			g_assert (ins->dreg == X86_EAX);*/
			if (ins->sreg2 == X86_EAX) {
				non_eax_reg = ins->sreg1;
			} else if (ins->sreg1 == X86_EAX) {
				non_eax_reg = ins->sreg2;
			} else {
				/* no need to save since we're going to store to it anyway */
				if (ins->dreg != X86_EAX) {
					saved_eax = TRUE;
					x86_push_reg (code, X86_EAX);
				}
				x86_mov_reg_reg (code, X86_EAX, ins->sreg1, 4);
				non_eax_reg = ins->sreg2;
			}
			if (ins->dreg == X86_EDX) {
				if (!saved_eax) {
					saved_eax = TRUE;
					x86_push_reg (code, X86_EAX);
				}
			} else if (ins->dreg != X86_EAX) {
				saved_edx = TRUE;
				x86_push_reg (code, X86_EDX);
			}
			x86_mul_reg (code, non_eax_reg, FALSE);
			/* save before the check since pop and mov don't change the flags */
			if (ins->dreg != X86_EAX)
				x86_mov_reg_reg (code, ins->dreg, X86_EAX, 4);
			if (saved_edx)
				x86_pop_reg (code, X86_EDX);
			if (saved_eax)
				x86_pop_reg (code, X86_EAX);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		}
		case OP_ICONST:
			x86_mov_reg_imm (code, ins->dreg, ins->inst_c0);
			break;
		case OP_AOTCONST:
			g_assert_not_reached ();
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			x86_mov_reg_imm (code, ins->dreg, 0);
			break;
		case OP_LOAD_GOTADDR:
			x86_call_imm (code, 0);
			/* 
			 * The patch needs to point to the pop, since the GOT offset needs 
			 * to be added to that address.
			 */
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_GOT_OFFSET, NULL);
			x86_pop_reg (code, ins->dreg);
			x86_alu_reg_imm (code, X86_ADD, ins->dreg, 0xf0f0f0f0);
			break;
		case OP_GOT_ENTRY:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_right->inst_i1, ins->inst_right->inst_p0);
			x86_mov_reg_membase (code, ins->dreg, ins->inst_basereg, 0xf0f0f0f0, 4);
			break;
		case OP_X86_PUSH_GOT_ENTRY:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_right->inst_i1, ins->inst_right->inst_p0);
			x86_push_membase (code, ins->inst_basereg, 0xf0f0f0f0);
			break;
		case CEE_CONV_I4:
		case OP_MOVE:
			x86_mov_reg_reg (code, ins->dreg, ins->sreg1, 4);
			break;
		case CEE_CONV_U4:
			g_assert_not_reached ();
		case CEE_JMP: {
			/*
			 * Note: this 'frame destruction' logic is useful for tail calls, too.
			 * Keep in sync with the code in emit_epilog.
			 */
			int pos = 0;

			/* FIXME: no tracing support... */
			if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
				code = mono_arch_instrument_epilog (cfg, mono_profiler_method_leave, code, FALSE);
			/* reset offset to make max_len work */
			offset = code - cfg->native_code;

			g_assert (!cfg->method->save_lmf);

			if (cfg->used_int_regs & (1 << X86_EBX))
				pos -= 4;
			if (cfg->used_int_regs & (1 << X86_EDI))
				pos -= 4;
			if (cfg->used_int_regs & (1 << X86_ESI))
				pos -= 4;
			if (pos)
				x86_lea_membase (code, X86_ESP, X86_EBP, pos);
	
			if (cfg->used_int_regs & (1 << X86_ESI))
				x86_pop_reg (code, X86_ESI);
			if (cfg->used_int_regs & (1 << X86_EDI))
				x86_pop_reg (code, X86_EDI);
			if (cfg->used_int_regs & (1 << X86_EBX))
				x86_pop_reg (code, X86_EBX);
	
			/* restore ESP/EBP */
			x86_leave (code);
			offset = code - cfg->native_code;
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			x86_jump32 (code, 0);
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL
			 * note that cmp DWORD PTR [eax], eax is one byte shorter than
			 * cmp DWORD PTR [eax], 0
		         */
			x86_alu_membase_reg (code, X86_CMP, ins->sreg1, 0, ins->sreg1);
			break;
		case OP_ARGLIST: {
			int hreg = ins->sreg1 == X86_EAX? X86_ECX: X86_EAX;
			x86_push_reg (code, hreg);
			x86_lea_membase (code, hreg, X86_EBP, cfg->sig_cookie);
			x86_mov_membase_reg (code, ins->sreg1, 0, hreg, 4);
			x86_pop_reg (code, hreg);
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
			else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, call->fptr);
			}
			x86_call_code (code, 0);
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention)) {
				/* a pop is one byte, while an add reg, imm is 3. So if there are 4 or 8
				 * bytes to pop, we want to use pops. GCC does this (note it won't happen
				 * for P4 or i686 because gcc will avoid using pop push at all. But we aren't
				 * smart enough to do that optimization yet
				 *
				 * It turns out that on my P4, doing two pops for 8 bytes on the stack makes
				 * mcs botstrap slow down. However, doing 1 pop for 4 bytes creates a small,
				 * (most likely from locality benefits). People with other processors should
				 * check on theirs to see what happens.
				 */
				if (call->stack_usage == 4) {
					/* we want to use registers that won't get used soon, so use
					 * edx and ecx, as registers get allocated in the order eax, ecx, edx.
					 * However, for a long call, part of the return is in edx, so only use ecx
					 */
					if (ins->opcode != OP_LCALL)
						x86_pop_reg (code, X86_EDX);
					else
						x86_pop_reg (code, X86_ECX);
				} else {
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, call->stack_usage);
				}
			}
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			call = (MonoCallInst*)ins;
			x86_call_reg (code, ins->sreg1);
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention)) {
				if (call->stack_usage == 4) {
					if (ins->opcode != OP_LCALL_REG)
						x86_pop_reg (code, X86_EDX);
					else
						x86_pop_reg (code, X86_ECX);
				} else {
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, call->stack_usage);
				}
			}
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			call = (MonoCallInst*)ins;
			x86_call_membase (code, ins->sreg1, ins->inst_offset);
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention)) {
				if (call->stack_usage == 4) {
					if (ins->opcode != OP_LCALL_MEMBASE)
						x86_pop_reg (code, X86_EDX);
					else
						x86_pop_reg (code, X86_ECX);
				} else {
					x86_alu_reg_imm (code, X86_ADD, X86_ESP, call->stack_usage);
				}
			}
			break;
		case OP_OUTARG:
		case OP_X86_PUSH:
			x86_push_reg (code, ins->sreg1);
			break;
		case OP_X86_PUSH_IMM:
			x86_push_imm (code, ins->inst_imm);
			break;
		case OP_X86_PUSH_MEMBASE:
			x86_push_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_PUSH_OBJ: 
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, ins->inst_imm);
			x86_push_reg (code, X86_EDI);
			x86_push_reg (code, X86_ESI);
			x86_push_reg (code, X86_ECX);
			if (ins->inst_offset)
				x86_lea_membase (code, X86_ESI, ins->inst_basereg, ins->inst_offset);
			else
				x86_mov_reg_reg (code, X86_ESI, ins->inst_basereg, 4);
			x86_lea_membase (code, X86_EDI, X86_ESP, 12);
			x86_mov_reg_imm (code, X86_ECX, (ins->inst_imm >> 2));
			x86_cld (code);
			x86_prefix (code, X86_REP_PREFIX);
			x86_movsd (code);
			x86_pop_reg (code, X86_ECX);
			x86_pop_reg (code, X86_ESI);
			x86_pop_reg (code, X86_EDI);
			break;
		case OP_X86_LEA:
			x86_lea_memindex (code, ins->dreg, ins->sreg1, ins->inst_imm, ins->sreg2, ins->unused);
			break;
		case OP_X86_LEA_MEMBASE:
			x86_lea_membase (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_XCHG:
			x86_xchg_reg_reg (code, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_LOCALLOC:
			/* keep alignment */
			x86_alu_reg_imm (code, X86_ADD, ins->sreg1, MONO_ARCH_FRAME_ALIGNMENT - 1);
			x86_alu_reg_imm (code, X86_AND, ins->sreg1, ~(MONO_ARCH_FRAME_ALIGNMENT - 1));
			code = mono_emit_stack_alloc (code, ins);
			x86_mov_reg_reg (code, ins->dreg, X86_ESP, 4);
			break;
		case CEE_RET:
			x86_ret (code);
			break;
		case CEE_THROW: {
			x86_push_reg (code, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception");
			x86_call_code (code, 0);
			break;
		}
		case OP_RETHROW: {
			x86_push_reg (code, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception");
			x86_call_code (code, 0);
			break;
		}
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			x86_call_imm (code, 0);
			break;
		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case CEE_BR:
			//g_print ("target: %p, next: %p, curr: %p, last: %p\n", ins->inst_target_bb, bb->next_bb, ins, bb->last_ins);
			//if ((ins->inst_target_bb == bb->next_bb) && ins == bb->last_ins)
			//break;
			if (ins->flags & MONO_INST_BRLABEL) {
				if (ins->inst_i0->inst_c0) {
					x86_jump_code (code, cfg->native_code + ins->inst_i0->inst_c0);
				} else {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_LABEL, ins->inst_i0);
					if ((cfg->opt & MONO_OPT_BRANCH) &&
					    x86_is_imm8 (ins->inst_i0->inst_c1 - cpos))
						x86_jump8 (code, 0);
					else 
						x86_jump32 (code, 0);
				}
			} else {
				if (ins->inst_target_bb->native_offset) {
					x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
				} else {
					mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
					if ((cfg->opt & MONO_OPT_BRANCH) &&
					    x86_is_imm8 (ins->inst_target_bb->max_offset - cpos))
						x86_jump8 (code, 0);
					else 
						x86_jump32 (code, 0);
				} 
			}
			break;
		case OP_BR_REG:
			x86_jump_reg (code, ins->sreg1);
			break;
		case OP_CEQ:
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_CLT:
			x86_set_reg (code, X86_CC_LT, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_CLT_UN:
			x86_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_CGT:
			x86_set_reg (code, X86_CC_GT, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_CGT_UN:
			x86_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
			break;
		case OP_CNE:
			x86_set_reg (code, X86_CC_NE, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
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
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_EQ], 
						    (ins->opcode < OP_COND_EXC_NE_UN), ins->inst_p1);
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
			EMIT_COND_BRANCH (ins, branch_cc_table [ins->opcode - CEE_BEQ], (ins->opcode < CEE_BNE_UN));
			break;

		/* floating point opcodes */
		case OP_R8CONST: {
			double d = *(double *)ins->inst_p0;

			if ((d == 0.0) && (mono_signbit (d) == 0)) {
				x86_fldz (code);
			} else if (d == 1.0) {
				x86_fld1 (code);
			} else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, ins->inst_p0);
				x86_fld (code, NULL, TRUE);
			}
			break;
		}
		case OP_R4CONST: {
			float f = *(float *)ins->inst_p0;

			if ((f == 0.0) && (mono_signbit (f) == 0)) {
				x86_fldz (code);
			} else if (f == 1.0) {
				x86_fld1 (code);
			} else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R4, ins->inst_p0);
				x86_fld (code, NULL, FALSE);
			}
			break;
		}
		case OP_STORER8_MEMBASE_REG:
			x86_fst_membase (code, ins->inst_destbasereg, ins->inst_offset, TRUE, TRUE);
			break;
		case OP_LOADR8_SPILL_MEMBASE:
			x86_fld_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);
			x86_fxch (code, 1);
			break;
		case OP_LOADR8_MEMBASE:
			x86_fld_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_STORER4_MEMBASE_REG:
			x86_fst_membase (code, ins->inst_destbasereg, ins->inst_offset, FALSE, TRUE);
			break;
		case OP_LOADR4_MEMBASE:
			x86_fld_membase (code, ins->inst_basereg, ins->inst_offset, FALSE);
			break;
		case CEE_CONV_R4: /* FIXME: change precision */
		case CEE_CONV_R8:
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, FALSE);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_X86_FP_LOAD_I8:
			x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_X86_FP_LOAD_I4:
			x86_fild_membase (code, ins->inst_basereg, ins->inst_offset, FALSE);
			break;
		case OP_FCONV_TO_I1:
			code = emit_float_to_int (cfg, code, ins->dreg, 1, TRUE);
			break;
		case OP_FCONV_TO_U1:
			code = emit_float_to_int (cfg, code, ins->dreg, 1, FALSE);
			break;
		case OP_FCONV_TO_I2:
			code = emit_float_to_int (cfg, code, ins->dreg, 2, TRUE);
			break;
		case OP_FCONV_TO_U2:
			code = emit_float_to_int (cfg, code, ins->dreg, 2, FALSE);
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, 4, TRUE);
			break;
		case OP_FCONV_TO_I8:
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 4);
			x86_fnstcw_membase(code, X86_ESP, 0);
			x86_mov_reg_membase (code, ins->dreg, X86_ESP, 0, 2);
			x86_alu_reg_imm (code, X86_OR, ins->dreg, 0xc00);
			x86_mov_membase_reg (code, X86_ESP, 2, ins->dreg, 2);
			x86_fldcw_membase (code, X86_ESP, 2);
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 8);
			x86_fist_pop_membase (code, X86_ESP, 0, TRUE);
			x86_pop_reg (code, ins->dreg);
			x86_pop_reg (code, ins->unused);
			x86_fldcw_membase (code, X86_ESP, 0);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 4);
			break;
		case OP_LCONV_TO_R_UN: { 
			static guint8 mn[] = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x3f, 0x40 };
			guint8 *br;

			/* load 64bit integer to FP stack */
			x86_push_imm (code, 0);
			x86_push_reg (code, ins->sreg2);
			x86_push_reg (code, ins->sreg1);
			x86_fild_membase (code, X86_ESP, 0, TRUE);
			/* store as 80bit FP value */
			x86_fst80_membase (code, X86_ESP, 0);
			
			/* test if lreg is negative */
			x86_test_reg_reg (code, ins->sreg2, ins->sreg2);
			br = code; x86_branch8 (code, X86_CC_GEZ, 0, TRUE);
	
			/* add correction constant mn */
			x86_fld80_mem (code, mn);
			x86_fld80_membase (code, X86_ESP, 0);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			x86_fst80_membase (code, X86_ESP, 0);

			x86_patch (br, code);

			x86_fld80_membase (code, X86_ESP, 0);
			x86_alu_reg_imm (code, X86_ADD, X86_ESP, 12);

			break;
		}
		case OP_LCONV_TO_OVF_I: {
			guint8 *br [3], *label [1];

			/* 
			 * Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000
			 */
			x86_test_reg_reg (code, ins->sreg1, ins->sreg1);

			/* If the low word top bit is set, see if we are negative */
			br [0] = code; x86_branch8 (code, X86_CC_LT, 0, TRUE);
			/* We are not negative (no top bit set, check for our top word to be zero */
			x86_test_reg_reg (code, ins->sreg2, ins->sreg2);
			br [1] = code; x86_branch8 (code, X86_CC_EQ, 0, TRUE);
			label [0] = code;

			/* throw exception */
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_EXC, "OverflowException");
		        x86_jump32 (code, 0);
	
			x86_patch (br [0], code);
			/* our top bit is set, check that top word is 0xfffffff */
			x86_alu_reg_imm (code, X86_CMP, ins->sreg2, 0xffffffff);
		
			x86_patch (br [1], code);
			/* nope, emit exception */
			br [2] = code; x86_branch8 (code, X86_CC_NE, 0, TRUE);
			x86_patch (br [2], label [0]);

			if (ins->dreg != ins->sreg1)
				x86_mov_reg_reg (code, ins->dreg, ins->sreg1, 4);
			break;
		}
		case OP_FADD:
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			break;
		case OP_FSUB:
			x86_fp_op_reg (code, X86_FSUB, 1, TRUE);
			break;		
		case OP_FMUL:
			x86_fp_op_reg (code, X86_FMUL, 1, TRUE);
			break;		
		case OP_FDIV:
			x86_fp_op_reg (code, X86_FDIV, 1, TRUE);
			break;		
		case OP_FNEG:
			x86_fchs (code);
			break;		
		case OP_SIN:
			x86_fsin (code);
			x86_fldz (code);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			break;		
		case OP_COS:
			x86_fcos (code);
			x86_fldz (code);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			break;		
		case OP_ABS:
			x86_fabs (code);
			break;		
		case OP_TAN: {
			/* 
			 * it really doesn't make sense to inline all this code,
			 * it's here just to show that things may not be as simple 
			 * as they appear.
			 */
			guchar *check_pos, *end_tan, *pop_jump;
			x86_push_reg (code, X86_EAX);
			x86_fptan (code);
			x86_fnstsw (code);
			x86_test_reg_imm (code, X86_EAX, X86_FP_C2);
			check_pos = code;
			x86_branch8 (code, X86_CC_NE, 0, FALSE);
			x86_fstp (code, 0); /* pop the 1.0 */
			end_tan = code;
			x86_jump8 (code, 0);
			x86_fldpi (code);
			x86_fp_op (code, X86_FADD, 0);
			x86_fxch (code, 1);
			x86_fprem1 (code);
			x86_fstsw (code);
			x86_test_reg_imm (code, X86_EAX, X86_FP_C2);
			pop_jump = code;
			x86_branch8 (code, X86_CC_NE, 0, FALSE);
			x86_fstp (code, 1);
			x86_fptan (code);
			x86_patch (pop_jump, code);
			x86_fstp (code, 0); /* pop the 1.0 */
			x86_patch (check_pos, code);
			x86_patch (end_tan, code);
			x86_fldz (code);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			x86_pop_reg (code, X86_EAX);
			break;
		}
		case OP_ATAN:
			x86_fld1 (code);
			x86_fpatan (code);
			x86_fldz (code);
			x86_fp_op_reg (code, X86_FADD, 1, TRUE);
			break;		
		case OP_SQRT:
			x86_fsqrt (code);
			break;		
		case OP_X86_FPOP:
			x86_fstp (code, 0);
			break;		
		case OP_FREM: {
			guint8 *l1, *l2;

			x86_push_reg (code, X86_EAX);
			/* we need to exchange ST(0) with ST(1) */
			x86_fxch (code, 1);

			/* this requires a loop, because fprem somtimes 
			 * returns a partial remainder */
			l1 = code;
			/* looks like MS is using fprem instead of the IEEE compatible fprem1 */
			/* x86_fprem1 (code); */
			x86_fprem (code);
			x86_fnstsw (code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_C2);
			l2 = code + 2;
			x86_branch8 (code, X86_CC_NE, l1 - l2, FALSE);

			/* pop result */
			x86_fstp (code, 1);

			x86_pop_reg (code, X86_EAX);
			break;
		}
		case OP_FCOMPARE:
			if (cfg->opt & MONO_OPT_FCMOV) {
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				break;
			}
			/* this overwrites EAX */
			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			break;
		case OP_FCEQ:
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in 
				 * shorter and faster code (we can also remove the widening op)
				 */
				guchar *unordered_check;
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				unordered_check = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				x86_set_reg (code, X86_CC_EQ, ins->dreg, FALSE);
				x86_patch (unordered_check, code);
				break;
			}
			if (ins->dreg != X86_EAX) 
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4000);
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX) 
				x86_pop_reg (code, X86_EAX);
			break;
		case OP_FCLT:
		case OP_FCLT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in 
				 * shorter and faster code (we can also remove the widening op)
				 */
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				if (ins->opcode == OP_FCLT_UN) {
					guchar *unordered_check = code;
					guchar *jump_to_end;
					x86_branch8 (code, X86_CC_P, 0, FALSE);
					x86_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
					jump_to_end = code;
					x86_jump8 (code, 0);
					x86_patch (unordered_check, code);
					x86_inc_reg (code, ins->dreg);
					x86_patch (jump_to_end, code);
				} else {
					x86_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
				}
				break;
			}
			if (ins->dreg != X86_EAX) 
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			if (ins->opcode == OP_FCLT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX) 
				x86_pop_reg (code, X86_EAX);
			break;
		case OP_FCGT:
		case OP_FCGT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* zeroing the register at the start results in 
				 * shorter and faster code (we can also remove the widening op)
				 */
				guchar *unordered_check;
				x86_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
				x86_fcomip (code, 1);
				x86_fstp (code, 0);
				if (ins->opcode == OP_FCGT) {
					unordered_check = code;
					x86_branch8 (code, X86_CC_P, 0, FALSE);
					x86_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
					x86_patch (unordered_check, code);
				} else {
					x86_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
				}
				break;
			}
			if (ins->dreg != X86_EAX) 
				x86_push_reg (code, X86_EAX);

			EMIT_FPCOMPARE(code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, X86_FP_CC_MASK);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			if (ins->opcode == OP_FCGT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);
	
				x86_patch (end_jump, code);
			}
			x86_set_reg (code, X86_CC_EQ, ins->dreg, TRUE);
			x86_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);

			if (ins->dreg != X86_EAX) 
				x86_pop_reg (code, X86_EAX);
			break;
		case OP_FBEQ:
			if (cfg->opt & MONO_OPT_FCMOV) {
				guchar *jump = code;
				x86_branch8 (code, X86_CC_P, 0, TRUE);
				EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
				x86_patch (jump, code);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0x4000);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, TRUE);
			break;
		case OP_FBNE_UN:
			/* Branch if C013 != 100 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				/* branch if !ZF or (PF|CF) */
				EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_B, FALSE);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C3);
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			break;
		case OP_FBLT:
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
				break;
			}
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBLT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
				break;
			}
			if (ins->opcode == OP_FBLT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			if (ins->opcode == OP_FBGT_UN) {
				guchar *is_not_zero_check, *end_jump;
				is_not_zero_check = code;
				x86_branch8 (code, X86_CC_NZ, 0, TRUE);
				end_jump = code;
				x86_jump8 (code, 0);
				x86_patch (is_not_zero_check, code);
				x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_CC_MASK);

				x86_patch (end_jump, code);
			}
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBGE:
			/* Branch if C013 == 100 or 001 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				guchar *br1;

				/* skip branch if C1=1 */
				br1 = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				/* branch if (C0 | C3) = 1 */
				EMIT_COND_BRANCH (ins, X86_CC_BE, FALSE);
				x86_patch (br1, code);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C3);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBGE_UN:
			/* Branch if C013 == 000 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_LE, FALSE);
				break;
			}
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			break;
		case OP_FBLE:
			/* Branch if C013=000 or 100 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				guchar *br1;

				/* skip branch if C1=1 */
				br1 = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				/* branch if C0=0 */
				EMIT_COND_BRANCH (ins, X86_CC_NB, FALSE);
				x86_patch (br1, code);
				break;
			}
			x86_alu_reg_imm (code, X86_AND, X86_EAX, (X86_FP_C0|X86_FP_C1));
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, 0);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			break;
		case OP_FBLE_UN:
			/* Branch if C013 != 001 */
			if (cfg->opt & MONO_OPT_FCMOV) {
				EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
				EMIT_COND_BRANCH (ins, X86_CC_GE, FALSE);
				break;
			}
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			break;
		case CEE_CKFINITE: {
			x86_push_reg (code, X86_EAX);
			x86_fxam (code);
			x86_fnstsw (code);
			x86_alu_reg_imm (code, X86_AND, X86_EAX, 0x4100);
			x86_alu_reg_imm (code, X86_CMP, X86_EAX, X86_FP_C0);
			x86_pop_reg (code, X86_EAX);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_EQ, FALSE, "ArithmeticException");
			break;
		}
		case OP_TLS_GET: {
			x86_prefix (code, X86_GS_PREFIX);
			x86_mov_reg_mem (code, ins->dreg, ins->inst_offset, 4);			
			break;
		}
		case OP_ATOMIC_ADD_I4: {
			x86_prefix (code, X86_LOCK_PREFIX);
			x86_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			if (ins->sreg2 != ins->dreg)
				x86_mov_reg_reg (code, ins->dreg, ins->sreg2, 4);
			break;
		}
		case OP_ATOMIC_ADD_NEW_I4: {
			int sreg2 = ins->sreg2;
			/* hack: limit in regalloc, dest != sreg2 */
			if (ins->dreg == sreg2) {
				/* select one temp reg */
				if (sreg2 == X86_EBX && ins->inst_basereg != X86_EDI)
					sreg2 = X86_EDI;
				else if (ins->inst_basereg != X86_EBX) 
					sreg2 = X86_EBX;
				else
					sreg2 = X86_ESI;

				x86_push_reg (code, sreg2);
				x86_mov_reg_reg (code, sreg2, ins->sreg2, 4);
			}

			x86_prefix (code, X86_LOCK_PREFIX);
			x86_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			if (sreg2 != ins->dreg)
				x86_mov_reg_reg (code, ins->dreg, ins->sreg2, 4);

			x86_alu_reg_reg (code, X86_ADD, ins->dreg, sreg2);
			if (sreg2 != ins->sreg2)
				x86_pop_reg (code, sreg2);

			break;
		}
		case OP_ATOMIC_EXCHANGE_I4: {
			guchar *br[2];
			int sreg2 = ins->sreg2;
			int breg = ins->inst_basereg;

			/* cmpxchg uses eax as comperand, need to make sure we can use it
			 * hack to overcome limits in x86 reg allocator 
			 * (req: dreg == eax and sreg2 != eax and breg != eax) 
			 */
			if (ins->dreg != X86_EAX)
				x86_push_reg (code, X86_EAX);
			
			/* We need the EAX reg for the cmpxchg */
			if (ins->sreg2 == X86_EAX) {
				x86_push_reg (code, X86_EDX);
				x86_mov_reg_reg (code, X86_EDX, X86_EAX, 4);
				sreg2 = X86_EDX;
			}

			if (breg == X86_EAX) {
				x86_push_reg (code, X86_ESI);
				x86_mov_reg_reg (code, X86_ESI, X86_EAX, 4);
				breg = X86_ESI;
			}

			x86_mov_reg_membase (code, X86_EAX, breg, ins->inst_offset, 4);

			br [0] = code; x86_prefix (code, X86_LOCK_PREFIX);
			x86_cmpxchg_membase_reg (code, breg, ins->inst_offset, sreg2);
			br [1] = code; x86_branch8 (code, X86_CC_NE, -1, FALSE);
			x86_patch (br [1], br [0]);

			if (breg != ins->inst_basereg)
				x86_pop_reg (code, X86_ESI);

			if (ins->dreg != X86_EAX) {
				x86_mov_reg_reg (code, ins->dreg, X86_EAX, 4);
				x86_pop_reg (code, X86_EAX);
			}

			if (ins->sreg2 != sreg2)
				x86_pop_reg (code, X86_EDX);

			break;
		}
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((code - cfg->native_code - offset) > max_len) {
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

static gpointer *aot_got = NULL;
static guint32 got_len = 0;
static guint32 got_offset = 0;

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		if (mono_compile_aot) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_BB:
			case MONO_PATCH_INFO_LABEL:
			case MONO_PATCH_INFO_GOT_OFFSET:
				break;
			case MONO_PATCH_INFO_METHOD_REL:
			case MONO_PATCH_INFO_SWITCH:
			case MONO_PATCH_INFO_IID:
			case MONO_PATCH_INFO_METHODCONST:
			case MONO_PATCH_INFO_CLASS:
			case MONO_PATCH_INFO_IMAGE:
			case MONO_PATCH_INFO_FIELD:
			case MONO_PATCH_INFO_VTABLE:
			case MONO_PATCH_INFO_SFLDA:
			case MONO_PATCH_INFO_EXC_NAME:
			case MONO_PATCH_INFO_LDSTR:
			case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
			case MONO_PATCH_INFO_LDTOKEN: {
				/* Just to make code run at aot time work */
				if (aot_got == NULL) {
					aot_got = g_new0 (gpointer, 1024);
					got_len = 1024;
					got_offset = 0;
				} else if (got_offset >= got_len) {
					got_len *= 2;
					aot_got = g_realloc (aot_got, got_len * sizeof (gpointer));
				}

				aot_got [got_offset] = (gpointer)target;
				target = (const unsigned char*)(got_offset * sizeof (gpointer));
				got_offset ++;

				break;
			}
			default:
				break;
			}
		}

		switch (patch_info->type) {
		case MONO_PATCH_INFO_IP:
			*((gconstpointer *)(ip)) = target;
			continue;
		case MONO_PATCH_INFO_CLASS_INIT: {
			guint8 *code = ip;
			/* Might already been changed to a nop */
			x86_call_code (code, 0);
			break;
		}
		case MONO_PATCH_INFO_R4:
		case MONO_PATCH_INFO_R8:
			*((gconstpointer *)(ip + 2)) = target;
			continue;
		case MONO_PATCH_INFO_GOT_OFFSET: {
			guint32 offset = mono_arch_get_patch_offset (ip);
			*((guint32*)(ip + offset)) = (guint32)((guint8*)aot_got - (guint8*)ip);
			continue;
		}
		case MONO_PATCH_INFO_METHOD_REL:
		case MONO_PATCH_INFO_SWITCH:
		case MONO_PATCH_INFO_IID:
		case MONO_PATCH_INFO_METHODCONST:
		case MONO_PATCH_INFO_CLASS:
		case MONO_PATCH_INFO_IMAGE:
		case MONO_PATCH_INFO_FIELD:
		case MONO_PATCH_INFO_VTABLE:
		case MONO_PATCH_INFO_SFLDA:
		case MONO_PATCH_INFO_EXC_NAME:
		case MONO_PATCH_INFO_LDSTR:
		case MONO_PATCH_INFO_TYPE_FROM_HANDLE:
		case MONO_PATCH_INFO_LDTOKEN: {
			guint32 offset = mono_arch_get_patch_offset (ip);
			*((gconstpointer *)(ip + offset)) = target;
			continue;
		}
		case MONO_PATCH_INFO_NONE:
			continue;
		default:
			break;
		}
		x86_patch (ip, target);
	}
}

int
mono_arch_max_epilog_size (MonoCompile *cfg)
{
	int exc_count = 0, max_epilog_size = 16;
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
	 * 16 is the size of two push_imm instructions and a call
	 */
	max_epilog_size += exc_count * 16;

	return max_epilog_size;
}

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int alloc_size, pos, max_offset, i;
	guint8 *code;

	cfg->code_size =  MAX (mono_method_get_header (method)->code_size * 4, 256);
	code = cfg->native_code = g_malloc (cfg->code_size);

	x86_push_reg (code, X86_EBP);
	x86_mov_reg_reg (code, X86_EBP, X86_ESP, 4);

	alloc_size = - cfg->stack_offset;
	pos = 0;

	if (method->save_lmf) {
		pos += sizeof (MonoLMF);

		/* save the current IP */
		mono_add_patch_info (cfg, code + 1 - cfg->native_code, MONO_PATCH_INFO_IP, NULL);
		x86_push_imm_template (code);

		/* save all caller saved regs */
		x86_push_reg (code, X86_EBP);
		x86_push_reg (code, X86_ESI);
		x86_push_reg (code, X86_EDI);
		x86_push_reg (code, X86_EBX);

		/* save method info */
		x86_push_imm (code, method);

		/* get the address of lmf for the current thread */
		/* 
		 * This is performance critical so we try to use some tricks to make
		 * it fast.
		 */
		if (lmf_tls_offset != -1) {
			/* Load lmf quicky using the GS register */
			x86_prefix (code, X86_GS_PREFIX);
			x86_mov_reg_mem (code, X86_EAX, lmf_tls_offset, 4);
		}
		else {
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
								 (gpointer)"mono_get_lmf_addr");
			x86_call_code (code, 0);
		}

		/* push lmf */
		x86_push_reg (code, X86_EAX); 
		/* push *lfm (previous_lmf) */
		x86_push_membase (code, X86_EAX, 0);
		/* *(lmf) = ESP */
		x86_mov_membase_reg (code, X86_EAX, 0, X86_ESP, 4);
	} else {

		if (cfg->used_int_regs & (1 << X86_EBX)) {
			x86_push_reg (code, X86_EBX);
			pos += 4;
		}

		if (cfg->used_int_regs & (1 << X86_EDI)) {
			x86_push_reg (code, X86_EDI);
			pos += 4;
		}

		if (cfg->used_int_regs & (1 << X86_ESI)) {
			x86_push_reg (code, X86_ESI);
			pos += 4;
		}
	}

	alloc_size -= pos;

	if (alloc_size) {
		/* See mono_emit_stack_alloc */
#ifdef PLATFORM_WIN32
		guint32 remaining_size = alloc_size;
		while (remaining_size >= 0x1000) {
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, 0x1000);
			x86_test_membase_reg (code, X86_ESP, 0, X86_ESP);
			remaining_size -= 0x1000;
		}
		if (remaining_size)
			x86_alu_reg_imm (code, X86_SUB, X86_ESP, remaining_size);
#else
		x86_alu_reg_imm (code, X86_SUB, X86_ESP, alloc_size);
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

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);

	/* load arguments allocated to register from the stack */
	sig = method->signature;
	pos = 0;

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		inst = cfg->varinfo [pos];
		if (inst->opcode == OP_REGVAR) {
			x86_mov_reg_membase (code, inst->dreg, X86_EBP, inst->inst_offset, 4);
			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		}
		pos++;
	}

	cfg->code_len = code - cfg->native_code;

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig = method->signature;
	int pos, nthrows, i;
	guint32 stack_to_pop;
	guint8 *code;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);

	/* the code restoring the registers must be kept in sync with CEE_JMP */
	pos = 0;
	
	if (method->save_lmf) {
		gint32 prev_lmf_reg;

		/* Find a spare register */
		switch (sig->ret->type) {
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			prev_lmf_reg = X86_EDI;
			cfg->used_int_regs |= (1 << X86_EDI);
			break;
		default:
			prev_lmf_reg = X86_EDX;
			break;
		}

		/* reg = previous_lmf */
		x86_mov_reg_membase (code, prev_lmf_reg, X86_EBP, -32, 4);

		/* ecx = lmf */
		x86_mov_reg_membase (code, X86_ECX, X86_EBP, -28, 4);

		/* *(lmf) = previous_lmf */
		x86_mov_membase_reg (code, X86_ECX, 0, prev_lmf_reg, 4);

		/* restore caller saved regs */
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			x86_mov_reg_membase (code, X86_EBX, X86_EBP, -20, 4);
		}

		if (cfg->used_int_regs & (1 << X86_EDI)) {
			x86_mov_reg_membase (code, X86_EDI, X86_EBP, -16, 4);
		}
		if (cfg->used_int_regs & (1 << X86_ESI)) {
			x86_mov_reg_membase (code, X86_ESI, X86_EBP, -12, 4);
		}

		/* EBP is restored by LEAVE */
	} else {
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			pos -= 4;
		}
		if (cfg->used_int_regs & (1 << X86_EDI)) {
			pos -= 4;
		}
		if (cfg->used_int_regs & (1 << X86_ESI)) {
			pos -= 4;
		}

		if (pos)
			x86_lea_membase (code, X86_ESP, X86_EBP, pos);

		if (cfg->used_int_regs & (1 << X86_ESI)) {
			x86_pop_reg (code, X86_ESI);
		}
		if (cfg->used_int_regs & (1 << X86_EDI)) {
			x86_pop_reg (code, X86_EDI);
		}
		if (cfg->used_int_regs & (1 << X86_EBX)) {
			x86_pop_reg (code, X86_EBX);
		}
	}

	x86_leave (code);

	if (CALLCONV_IS_STDCALL (sig->call_convention)) {
		MonoJitArgumentInfo *arg_info = alloca (sizeof (MonoJitArgumentInfo) * (sig->param_count + 1));

		stack_to_pop = mono_arch_get_argument_info (sig, sig->param_count, arg_info);
	} else if (MONO_TYPE_ISSTRUCT (cfg->method->signature->ret))
		stack_to_pop = 4;
	else
		stack_to_pop = 0;

	if (stack_to_pop)
		x86_ret_imm (code, stack_to_pop);
	else
		x86_ret (code);

	/* add code to raise exceptions */
	nthrows = 0;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			guint8 *buf, *buf2;
			guint32 throw_ip;

			x86_patch (patch_info->ip.i + cfg->native_code, code);

			exc_class = mono_class_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			g_assert (exc_class);
			throw_ip = patch_info->ip.i;

			/* Find a throw sequence for the same exception class */
			for (i = 0; i < nthrows; ++i)
				if (exc_classes [i] == exc_class)
					break;
			if (i < nthrows) {
				x86_push_imm (code, (exc_throw_end [i] - cfg->native_code) - throw_ip);
				x86_jump_code (code, exc_throw_start [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
			}
			else {
				if ((code - cfg->native_code) - throw_ip < 115) {
					/* Use the shorter form */
					buf = buf2 = code;
					x86_push_imm (code, 0);
				}
				else {
					buf = code;
					x86_push_imm (code, 0xf0f0f0f0);
					buf2 = code;
				}

				if (nthrows < 16) {
					exc_classes [nthrows] = exc_class;
					exc_throw_start [nthrows] = code;
				}

				x86_push_imm (code, exc_class->type_token);
				patch_info->data.name = "mono_arch_throw_corlib_exception";
				patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
				patch_info->ip.i = code - cfg->native_code;
				x86_call_code (code, 0);
				x86_push_imm (buf, (code - cfg->native_code) - throw_ip);
				while (buf < buf2)
					x86_nop (buf);

				if (nthrows < 16) {
					exc_throw_end [nthrows] = code;
					nthrows ++;
				}
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

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	/* not needed */
}

void
mono_arch_flush_register_windows (void)
{
}

/*
 * Support for fast access to the thread-local lmf structure using the GS
 * segment register on NPTL + kernel 2.6.x.
 */

static gboolean tls_offset_inited = FALSE;

/* code should be simply return <tls var>; */
static int read_tls_offset_from_method (void* method)
{
	guint8* code = (guint8*) method;
	/* 
	 * Determine the offset of the variable inside the TLS structures
	 * by disassembling the function.
	 */

	/* gcc-3.3.2
	 *
	 * push ebp
	 * mov ebp, esp
	 * mov eax, gs:0
	 * mov eax, DWORD PTR [eax+<offset>]
	 */
	if (
		(code [0] == 0x55) && (code [1] == 0x89) && (code [2] == 0xe5) &&
		(code [3] == 0x65) && (code [4] == 0xa1) && (code [5] == 0x00) &&
		(code [6] == 0x00) && (code [7] == 0x00) && (code [8] == 0x00) &&
		(code [9] == 0x8b) && (code [10] == 0x80)) {
		return *(int*)&(code [11]);
	}
	
	/* gcc-3.4
	 *
	 * push ebp
	 * mov ebp, esp
	 * mov eax, gs:<offset>
	 */
	if (
		(code [0] == 0x55) && (code [1] == 0x89) && (code [2] == 0xe5) &&
		(code [3] == 0x65) && (code [4] == 0xa1)) {
		return *(int*)&(code [5]);
	}
	
	/* 3.2.2 with -march=athlon
	 *
	 * push ebp
	 * mov eax, gs:<offset>
	 * mov ebp, esp
	 */
	if (
		(code [0] == 0x55) && (code [1] == 0x65) && (code [2] == 0xa1)) {
		return *(int*)&(code [3]);
	}
	
	return -1;
}
void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK
	pthread_t self = pthread_self();
	pthread_attr_t attr;
	void *staddr = NULL;
	size_t stsize = 0;
	struct sigaltstack sa;
#endif

	if (!tls_offset_inited) {
		tls_offset_inited = TRUE;
		if (getenv ("MONO_NPTL")) {
			lmf_tls_offset = read_tls_offset_from_method (mono_get_lmf_addr);
			appdomain_tls_offset = read_tls_offset_from_method (mono_domain_get);
			thread_tls_offset = read_tls_offset_from_method (mono_thread_current);
		}
	}		

#ifdef MONO_ARCH_SIGSEGV_ON_ALTSTACK

	/* Determine stack boundaries */
	if (!mono_running_on_valgrind ()) {
#ifdef HAVE_PTHREAD_GETATTR_NP
		pthread_getattr_np( self, &attr );
#else
#ifdef HAVE_PTHREAD_ATTR_GET_NP
		pthread_attr_get_np( self, &attr );
#elif defined(sun)
		pthread_attr_init( &attr );
		pthread_attr_getstacksize( &attr, &stsize );
#else
#error "Not implemented"
#endif
#endif
#ifndef sun
		pthread_attr_getstack( &attr, &staddr, &stsize );
#endif
	}

	/* 
	 * staddr seems to be wrong for the main thread, so we keep the value in
	 * tls->end_of_stack
	 */
	tls->stack_size = stsize;

	/* Setup an alternate signal stack */
	tls->signal_stack = g_malloc (SIGNAL_STACK_SIZE);
	tls->signal_stack_size = SIGNAL_STACK_SIZE;

	sa.ss_sp = tls->signal_stack;
	sa.ss_size = SIGNAL_STACK_SIZE;
	sa.ss_flags = SS_ONSTACK;
	sigaltstack (&sa, NULL);
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
		g_free (tls->signal_stack);
#endif
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{

	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this;
		MONO_INST_NEW (cfg, this, OP_OUTARG);
		this->type = this_type;
		this->sreg1 = this_reg;
		mono_bblock_add_inst (cfg->cbb, this);
	}

	if (vt_reg != -1) {
		MonoInst *vtarg;
		MONO_INST_NEW (cfg, vtarg, OP_OUTARG);
		vtarg->type = STACK_MP;
		vtarg->sreg1 = vt_reg;
		mono_bblock_add_inst (cfg->cbb, vtarg);
	}
}


MonoInst*
mono_arch_get_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	MonoInst *ins = NULL;

	if (cmethod->klass == mono_defaults.math_class) {
		if (strcmp (cmethod->name, "Sin") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SIN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Cos") == 0) {
			MONO_INST_NEW (cfg, ins, OP_COS);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Tan") == 0) {
			MONO_INST_NEW (cfg, ins, OP_TAN);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Atan") == 0) {
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

		if (strcmp (cmethod->name, "Increment") == 0 && fsig->params [0]->type == MONO_TYPE_I4) {
			MonoInst *ins_iconst;

			MONO_INST_NEW (cfg, ins, OP_ATOMIC_ADD_NEW_I4);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = 1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Decrement") == 0 && fsig->params [0]->type == MONO_TYPE_I4) {
			MonoInst *ins_iconst;

			MONO_INST_NEW (cfg, ins, OP_ATOMIC_ADD_NEW_I4);
			MONO_INST_NEW (cfg, ins_iconst, OP_ICONST);
			ins_iconst->inst_c0 = -1;

			ins->inst_i0 = args [0];
			ins->inst_i1 = ins_iconst;
		} else if (strcmp (cmethod->name, "Exchange") == 0 && fsig->params [0]->type == MONO_TYPE_I4) {
			MONO_INST_NEW (cfg, ins, OP_ATOMIC_EXCHANGE_I4);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		} else if (strcmp (cmethod->name, "Add") == 0 && fsig->params [0]->type == MONO_TYPE_I4) {
			MONO_INST_NEW (cfg, ins, OP_ATOMIC_ADD_I4);

			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
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

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	if ((code [0] == 0x8b) && (x86_modrm_mod (code [1]) == 0x2))
		return 2;
	else if ((code [0] == 0xba))
		return 1;
	else if ((code [0] == 0x68))
		/* push IMM */
		return 1;
	else if ((code [0] == 0xff) && (x86_modrm_reg (code [1]) == 0x6))
		/* push <OFFSET>(<REG>) */
		return 2;
	else if ((code [0] == 0x58) && (code [1] == 0x05))
		/* pop %eax; add <OFFSET>, %eax */
		return 2;
	else if ((code [0] >= 0x58) && (code [0] <= 0x58 + X86_NREG) && (code [1] == 0x81))
		/* pop <REG>; add <OFFSET>, <REG> */
		return 3;
	else {
		g_assert_not_reached ();
		return -1;
	}
}
