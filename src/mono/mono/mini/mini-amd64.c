/*
 * mini-amd64.c: AMD64 backend for the Mono code generator
 *
 * Based on mini-x86.c.
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *   Patrik Torstensson
 *   Zoltan Varga (vargaz@gmail.com)
 *
 * (C) 2003 Ximian, Inc.
 */
#include "mini.h"
#include <string.h>
#include <math.h>
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-debug.h>
#include <mono/utils/mono-math.h>

#include "trace.h"
#include "mini-amd64.h"
#include "inssel.h"
#include "cpu-amd64.h"

static gint lmf_tls_offset = -1;
static gint lmf_addr_tls_offset = -1;
static gint appdomain_tls_offset = -1;
static gint thread_tls_offset = -1;

#ifdef MONO_XEN_OPT
static gboolean optimize_for_xen = TRUE;
#else
#define optimize_for_xen 0
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

#define IS_IMM32(val) ((((guint64)val) >> 32) == 0)

#define IS_REX(inst) (((inst) >= 0x40) && ((inst) <= 0x4f))

#ifdef PLATFORM_WIN32
/* Under windows, the default pinvoke calling convention is stdcall */
#define CALLCONV_IS_STDCALL(call_conv) (((call_conv) == MONO_CALL_STDCALL) || ((call_conv) == MONO_CALL_DEFAULT))
#else
#define CALLCONV_IS_STDCALL(call_conv) ((call_conv) == MONO_CALL_STDCALL)
#endif

/* This mutex protects architecture specific caches */
#define mono_mini_arch_lock() EnterCriticalSection (&mini_arch_mutex)
#define mono_mini_arch_unlock() LeaveCriticalSection (&mini_arch_mutex)
static CRITICAL_SECTION mini_arch_mutex;

MonoBreakpointInfo
mono_breakpoint_info [MONO_BREAKPOINT_ARRAY_SIZE];

#ifdef PLATFORM_WIN32
/* On Win64 always reserve first 32 bytes for first four arguments */
#define ARGS_OFFSET 48
#else
#define ARGS_OFFSET 16
#endif
#define GP_SCRATCH_REG AMD64_R11

/*
 * AMD64 register usage:
 * - callee saved registers are used for global register allocation
 * - %r11 is used for materializing 64 bit constants in opcodes
 * - the rest is used for local allocation
 */

/*
 * Floating point comparison results:
 *                  ZF PF CF
 * A > B            0  0  0
 * A < B            0  0  1
 * A = B            1  0  0
 * A > B            0  0  0
 * UNORDERED        1  1  1
 */

const char*
mono_arch_regname (int reg)
{
	switch (reg) {
	case AMD64_RAX: return "%rax";
	case AMD64_RBX: return "%rbx";
	case AMD64_RCX: return "%rcx";
	case AMD64_RDX: return "%rdx";
	case AMD64_RSP: return "%rsp";	
	case AMD64_RBP: return "%rbp";
	case AMD64_RDI: return "%rdi";
	case AMD64_RSI: return "%rsi";
	case AMD64_R8: return "%r8";
	case AMD64_R9: return "%r9";
	case AMD64_R10: return "%r10";
	case AMD64_R11: return "%r11";
	case AMD64_R12: return "%r12";
	case AMD64_R13: return "%r13";
	case AMD64_R14: return "%r14";
	case AMD64_R15: return "%r15";
	}
	return "unknown";
}

static const char * xmmregs [] = {
	"xmm0", "xmm1", "xmm2", "xmm3", "xmm4", "xmm5", "xmm6", "xmm7", "xmm8",
	"xmm9", "xmm10", "xmm11", "xmm12", "xmm13", "xmm14", "xmm15"
};

const char*
mono_arch_fregname (int reg)
{
	if (reg < AMD64_XMM_NREG)
		return xmmregs [reg];
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

	if (!getenv ("COUNT"))
		return TRUE;

	if (count == atoi (getenv ("COUNT"))) {
		break_count ();
	}

	if (count > atoi (getenv ("COUNT"))) {
		return FALSE;
	}

	return TRUE;
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

static inline gboolean
amd64_is_near_call (guint8 *code)
{
	/* Skip REX */
	if ((code [0] >= 0x40) && (code [0] <= 0x4f))
		code += 1;

	return code [0] == 0xe8;
}

static inline void 
amd64_patch (unsigned char* code, gpointer target)
{
	guint8 rex = 0;

	/* Skip REX */
	if ((code [0] >= 0x40) && (code [0] <= 0x4f)) {
		rex = code [0];
		code += 1;
	}

	if ((code [0] & 0xf8) == 0xb8) {
		/* amd64_set_reg_template */
		*(guint64*)(code + 1) = (guint64)target;
	}
	else if ((code [0] == 0x8b) && rex && x86_modrm_mod (code [1]) == 0 && x86_modrm_rm (code [1]) == 5) {
		/* mov 0(%rip), %dreg */
		*(guint32*)(code + 2) = (guint32)(guint64)target - 7;
	}
	else if ((code [0] == 0xff) && (code [1] == 0x15)) {
		/* call *<OFFSET>(%rip) */
		*(guint32*)(code + 2) = ((guint32)(guint64)target) - 7;
	}
	else if ((code [0] == 0xe8)) {
		/* call <DISP> */
		gint64 disp = (guint8*)target - (guint8*)code;
		g_assert (amd64_is_imm32 (disp));
		x86_patch (code, (unsigned char*)target);
	}
	else
		x86_patch (code, (unsigned char*)target);
}

void 
mono_amd64_patch (unsigned char* code, gpointer target)
{
	amd64_patch (code, target);
}

typedef enum {
	ArgInIReg,
	ArgInFloatSSEReg,
	ArgInDoubleSSEReg,
	ArgOnStack,
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

#ifdef PLATFORM_WIN32
#define PARAM_REGS 4

static AMD64_Reg_No param_regs [] = { AMD64_RCX, AMD64_RDX, AMD64_R8, AMD64_R9 };

static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };
#else
#define PARAM_REGS 6
 
static AMD64_Reg_No param_regs [] = { AMD64_RDI, AMD64_RSI, AMD64_RDX, AMD64_RCX, AMD64_R8, AMD64_R9 };

 static AMD64_Reg_No return_regs [] = { AMD64_RAX, AMD64_RDX };
#endif

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
		ainfo->reg = param_regs [*gr];
		(*gr) ++;
    }
}

#ifdef PLATFORM_WIN32
#define FLOAT_PARAM_REGS 4
#else
#define FLOAT_PARAM_REGS 8
#endif

static void inline
add_float (guint32 *gr, guint32 *stack_size, ArgInfo *ainfo, gboolean is_double)
{
    ainfo->offset = *stack_size;

    if (*gr >= FLOAT_PARAM_REGS) {
		ainfo->storage = ArgOnStack;
		(*stack_size) += sizeof (gpointer);
    }
    else {
		/* A double register */
		if (is_double)
			ainfo->storage = ArgInDoubleSSEReg;
		else
			ainfo->storage = ArgInFloatSSEReg;
		ainfo->reg = *gr;
		(*gr) += 1;
    }
}

typedef enum ArgumentClass {
	ARG_CLASS_NO_CLASS,
	ARG_CLASS_MEMORY,
	ARG_CLASS_INTEGER,
	ARG_CLASS_SSE
} ArgumentClass;

static ArgumentClass
merge_argument_class_from_type (MonoType *type, ArgumentClass class1)
{
	ArgumentClass class2 = ARG_CLASS_NO_CLASS;
	MonoType *ptype;

	ptype = mono_type_get_underlying_type (type);
	switch (ptype->type) {
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
	case MONO_TYPE_STRING:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_CLASS:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_PTR:
	case MONO_TYPE_FNPTR:
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		class2 = ARG_CLASS_INTEGER;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
#ifdef PLATFORM_WIN32
		class2 = ARG_CLASS_INTEGER;
#else
		class2 = ARG_CLASS_SSE;
#endif
		break;

	case MONO_TYPE_TYPEDBYREF:
		g_assert_not_reached ();

	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (ptype)) {
			class2 = ARG_CLASS_INTEGER;
			break;
		}
		/* fall through */
	case MONO_TYPE_VALUETYPE: {
		MonoMarshalType *info = mono_marshal_load_type_info (ptype->data.klass);
		int i;

		for (i = 0; i < info->num_fields; ++i) {
			class2 = class1;
			class2 = merge_argument_class_from_type (info->fields [i].field->type, class2);
		}
		break;
	}
	default:
		g_assert_not_reached ();
	}

	/* Merge */
	if (class1 == class2)
		;
	else if (class1 == ARG_CLASS_NO_CLASS)
		class1 = class2;
	else if ((class1 == ARG_CLASS_MEMORY) || (class2 == ARG_CLASS_MEMORY))
		class1 = ARG_CLASS_MEMORY;
	else if ((class1 == ARG_CLASS_INTEGER) || (class2 == ARG_CLASS_INTEGER))
		class1 = ARG_CLASS_INTEGER;
	else
		class1 = ARG_CLASS_SSE;

	return class1;
}

static void
add_valuetype (MonoGenericSharingContext *gsctx, MonoMethodSignature *sig, ArgInfo *ainfo, MonoType *type,
	       gboolean is_return,
	       guint32 *gr, guint32 *fr, guint32 *stack_size)
{
	guint32 size, quad, nquads, i;
	ArgumentClass args [2];
	MonoMarshalType *info;
	MonoClass *klass;

	klass = mono_class_from_mono_type (type);
	if (sig->pinvoke) 
		size = mono_type_native_stack_size (&klass->byval_arg, NULL);
	else 
		size = mini_type_stack_size (gsctx, &klass->byval_arg, NULL);

	if (!sig->pinvoke || (size == 0) || (size > 16)) {
		/* Allways pass in memory */
		ainfo->offset = *stack_size;
		*stack_size += ALIGN_TO (size, 8);
		ainfo->storage = ArgOnStack;

		return;
	}

	/* FIXME: Handle structs smaller than 8 bytes */
	//if ((size % 8) != 0)
	//	NOT_IMPLEMENTED;

	if (size > 8)
		nquads = 2;
	else
		nquads = 1;

	/*
	 * Implement the algorithm from section 3.2.3 of the X86_64 ABI.
	 * The X87 and SSEUP stuff is left out since there are no such types in
	 * the CLR.
	 */
	info = mono_marshal_load_type_info (klass);
	g_assert (info);

#ifndef PLATFORM_WIN32
	if (info->native_size > 16) {
		ainfo->offset = *stack_size;
		*stack_size += ALIGN_TO (info->native_size, 8);
		ainfo->storage = ArgOnStack;

		return;
	}
#else
	switch (info->native_size) {
	case 1: case 2: case 4: case 8:
		break;
	default:
		ainfo->offset = *stack_size;
		*stack_size += ALIGN_TO (info->native_size, 16);
		ainfo->storage = ArgOnStack;
		return;
	}
#endif

	args [0] = ARG_CLASS_NO_CLASS;
	args [1] = ARG_CLASS_NO_CLASS;
	for (quad = 0; quad < nquads; ++quad) {
		int size;
		guint32 align;
		ArgumentClass class1;
		
		class1 = ARG_CLASS_NO_CLASS;
		for (i = 0; i < info->num_fields; ++i) {
			size = mono_marshal_type_size (info->fields [i].field->type, 
										   info->fields [i].mspec, 
										   &align, TRUE, klass->unicode);
			if ((info->fields [i].offset < 8) && (info->fields [i].offset + size) > 8) {
				/* Unaligned field */
				NOT_IMPLEMENTED;
			}

			/* Skip fields in other quad */
			if ((quad == 0) && (info->fields [i].offset >= 8))
				continue;
			if ((quad == 1) && (info->fields [i].offset < 8))
				continue;

			class1 = merge_argument_class_from_type (info->fields [i].field->type, class1);
		}
		g_assert (class1 != ARG_CLASS_NO_CLASS);
		args [quad] = class1;
	}

	/* Post merger cleanup */
	if ((args [0] == ARG_CLASS_MEMORY) || (args [1] == ARG_CLASS_MEMORY))
		args [0] = args [1] = ARG_CLASS_MEMORY;

	/* Allocate registers */
	{
		int orig_gr = *gr;
		int orig_fr = *fr;

		ainfo->storage = ArgValuetypeInReg;
		ainfo->pair_storage [0] = ainfo->pair_storage [1] = ArgNone;
		for (quad = 0; quad < nquads; ++quad) {
			switch (args [quad]) {
			case ARG_CLASS_INTEGER:
				if (*gr >= PARAM_REGS)
					args [quad] = ARG_CLASS_MEMORY;
				else {
					ainfo->pair_storage [quad] = ArgInIReg;
					if (is_return)
						ainfo->pair_regs [quad] = return_regs [*gr];
					else
						ainfo->pair_regs [quad] = param_regs [*gr];
					(*gr) ++;
				}
				break;
			case ARG_CLASS_SSE:
				if (*fr >= FLOAT_PARAM_REGS)
					args [quad] = ARG_CLASS_MEMORY;
				else {
					ainfo->pair_storage [quad] = ArgInDoubleSSEReg;
					ainfo->pair_regs [quad] = *fr;
					(*fr) ++;
				}
				break;
			case ARG_CLASS_MEMORY:
				break;
			default:
				g_assert_not_reached ();
			}
		}

		if ((args [0] == ARG_CLASS_MEMORY) || (args [1] == ARG_CLASS_MEMORY)) {
			/* Revert possible register assignments */
			*gr = orig_gr;
			*fr = orig_fr;

			ainfo->offset = *stack_size;
			*stack_size += ALIGN_TO (info->native_size, 8);
			ainfo->storage = ArgOnStack;
		}
	}
}

/*
 * get_call_info:
 *
 *  Obtain information about a call according to the calling convention.
 * For AMD64, see the "System V ABI, x86-64 Architecture Processor Supplement 
 * Draft Version 0.23" document for more information.
 */
static CallInfo*
get_call_info (MonoGenericSharingContext *gsctx, MonoMemPool *mp, MonoMethodSignature *sig, gboolean is_pinvoke)
{
	guint32 i, gr, fr;
	MonoType *ret_type;
	int n = sig->hasthis + sig->param_count;
	guint32 stack_size = 0;
	CallInfo *cinfo;

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
			cinfo->ret.reg = AMD64_RAX;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.storage = ArgInIReg;
			cinfo->ret.reg = AMD64_RAX;
			break;
		case MONO_TYPE_R4:
			cinfo->ret.storage = ArgInFloatSSEReg;
			cinfo->ret.reg = AMD64_XMM0;
			break;
		case MONO_TYPE_R8:
			cinfo->ret.storage = ArgInDoubleSSEReg;
			cinfo->ret.reg = AMD64_XMM0;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (sig->ret)) {
				cinfo->ret.storage = ArgInIReg;
				cinfo->ret.reg = AMD64_RAX;
				break;
			}
			/* fall through */
		case MONO_TYPE_VALUETYPE: {
			guint32 tmp_gr = 0, tmp_fr = 0, tmp_stacksize = 0;

			add_valuetype (gsctx, sig, &cinfo->ret, sig->ret, TRUE, &tmp_gr, &tmp_fr, &tmp_stacksize);
			if (cinfo->ret.storage == ArgOnStack)
				/* The caller passes the address where the value is stored */
				add_general (&gr, &stack_size, &cinfo->ret);
			break;
		}
		case MONO_TYPE_TYPEDBYREF:
			/* Same as a valuetype with size 24 */
			add_general (&gr, &stack_size, &cinfo->ret);
			;
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
			/* fall through */
		case MONO_TYPE_VALUETYPE:
			add_valuetype (gsctx, sig, ainfo, sig->params [i], FALSE, &gr, &fr, &stack_size);
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
			add_float (&fr, &stack_size, ainfo, FALSE);
			break;
		case MONO_TYPE_R8:
			add_float (&fr, &stack_size, ainfo, TRUE);
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

#ifdef PLATFORM_WIN32
	// There always is 32 bytes reserved on the stack when calling on Winx64
	stack_size += 0x20;
#endif

	if (stack_size & 0x8) {
		/* The AMD64 ABI requires each stack frame to be 16 byte aligned */
		cinfo->need_stack_align = TRUE;
		stack_size += 8;
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

static int 
cpuid (int id, int* p_eax, int* p_ebx, int* p_ecx, int* p_edx)
{
#ifndef _MSC_VER
	__asm__ __volatile__ ("cpuid"
		: "=a" (*p_eax), "=b" (*p_ebx), "=c" (*p_ecx), "=d" (*p_edx)
		: "a" (id));
#else
	int info[4];
	__cpuid(info, id);
	*p_eax = info[0];
	*p_ebx = info[1];
	*p_ecx = info[2];
	*p_edx = info[3];
#endif
	return 1;
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
#ifndef _MSC_VER
	guint16 fpcw;

	/* spec compliance requires running with double precision */
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
	fpcw &= ~X86_FPCW_PRECC_MASK;
	fpcw |= X86_FPCW_PREC_DOUBLE;
	__asm__  __volatile__ ("fldcw %0\n": : "m" (fpcw));
	__asm__  __volatile__ ("fnstcw %0\n": "=m" (fpcw));
#else
	/* TODO: This is crashing on Win64 right now.
	* _control87 (_PC_53, MCW_PC);
	*/
#endif
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
	InitializeCriticalSection (&mini_arch_mutex);
}

/*
 * Cleanup architecture specific code.
 */
void
mono_arch_cleanup (void)
{
	DeleteCriticalSection (&mini_arch_mutex);
}

/*
 * This function returns the optimizations supported on this cpu.
 */
guint32
mono_arch_cpu_optimizazions (guint32 *exclude_mask)
{
	int eax, ebx, ecx, edx;
	guint32 opts = 0;

	/* FIXME: AMD64 */

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

		if (mono_is_regsize_var (ins->inst_vtype)) {
			g_assert (MONO_VARINFO (cfg, i)->reg == -1);
			g_assert (i == vmv->idx);
			vars = g_list_prepend (vars, vmv);
		}
	}

	vars = mono_varlist_sort (cfg, vars, 0);

	return vars;
}

/**
 * mono_arch_compute_omit_fp:
 *
 *   Determine whenever the frame pointer can be eliminated.
 */
static void
mono_arch_compute_omit_fp (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	int i, locals_size;
	CallInfo *cinfo;

	if (cfg->arch.omit_fp_computed)
		return;

	header = mono_method_get_header (cfg->method);

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig, FALSE);
	cinfo = cfg->arch.cinfo;

	/*
	 * FIXME: Remove some of the restrictions.
	 */
	cfg->arch.omit_fp = TRUE;
	cfg->arch.omit_fp_computed = TRUE;

	if (cfg->disable_omit_fp)
		cfg->arch.omit_fp = FALSE;

	if (!debug_omit_fp ())
		cfg->arch.omit_fp = FALSE;
	/*
	if (cfg->method->save_lmf)
		cfg->arch.omit_fp = FALSE;
	*/
	if (cfg->flags & MONO_CFG_HAS_ALLOCA)
		cfg->arch.omit_fp = FALSE;
	if (header->num_clauses)
		cfg->arch.omit_fp = FALSE;
	if (cfg->param_area)
		cfg->arch.omit_fp = FALSE;
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG))
		cfg->arch.omit_fp = FALSE;
	if ((mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method)) ||
		(cfg->prof_options & MONO_PROFILE_ENTER_LEAVE))
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

	locals_size = 0;
	for (i = cfg->locals_start; i < cfg->num_varinfo; i++) {
		MonoInst *ins = cfg->varinfo [i];
		int ialign;

		locals_size += mono_type_size (ins->inst_vtype, &ialign);
	}

	if ((cfg->num_varinfo > 10000) || (locals_size >= (1 << 15))) {
		/* Avoid hitting the stack_alloc_size < (1 << 16) assertion in emit_epilog () */
		cfg->arch.omit_fp = FALSE;
	}
}

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	mono_arch_compute_omit_fp (cfg);

	if (cfg->arch.omit_fp)
		regs = g_list_prepend (regs, (gpointer)AMD64_RBP);

	/* We use the callee saved registers for global allocation */
	regs = g_list_prepend (regs, (gpointer)AMD64_RBX);
	regs = g_list_prepend (regs, (gpointer)AMD64_R12);
	regs = g_list_prepend (regs, (gpointer)AMD64_R13);
	regs = g_list_prepend (regs, (gpointer)AMD64_R14);
	regs = g_list_prepend (regs, (gpointer)AMD64_R15);

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
		/* substract 1 for the invisible store in the prolog */
		return (ins->opcode == OP_ARG) ? 0 : 1;
	else
		/* push+pop */
		return (ins->opcode == OP_ARG) ? 1 : 2;
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

	header = mono_method_get_header (cfg->method);

	sig = mono_method_signature (cfg->method);

	cinfo = cfg->arch.cinfo;

	mono_arch_compute_omit_fp (cfg);

	/*
	 * We use the ABI calling conventions for managed code as well.
	 * Exception: valuetypes are never passed or returned in registers.
	 */

	if (cfg->arch.omit_fp) {
		cfg->flags |= MONO_CFG_HAS_SPILLUP;
		cfg->frame_reg = AMD64_RSP;
		offset = 0;
	} else {
		/* Locals are allocated backwards from %fp */
		cfg->frame_reg = AMD64_RBP;
		offset = 0;
	}

	if (cfg->method->save_lmf) {
		/* Reserve stack space for saving LMF */
		/* mono_arch_find_jit_info () expects to find the LMF at a fixed offset */
		g_assert (offset == 0);
		if (cfg->arch.omit_fp) {
			cfg->arch.lmf_offset = offset;
			offset += sizeof (MonoLMF);
		}
		else {
			offset += sizeof (MonoLMF);
			cfg->arch.lmf_offset = -offset;
		}
	} else {
		/* Reserve space for caller saved registers */
		for (i = 0; i < AMD64_NREG; ++i)
			if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
				offset += sizeof (gpointer);
			}
	}

	if (sig->ret->type != MONO_TYPE_VOID) {
		switch (cinfo->ret.storage) {
		case ArgInIReg:
		case ArgInFloatSSEReg:
		case ArgInDoubleSSEReg:
			if ((MONO_TYPE_ISSTRUCT (sig->ret) && !mono_class_from_mono_type (sig->ret)->enumtype) || (sig->ret->type == MONO_TYPE_TYPEDBYREF)) {
				/* The register is volatile */
				cfg->vret_addr->opcode = OP_REGOFFSET;
				cfg->vret_addr->inst_basereg = cfg->frame_reg;
				if (cfg->arch.omit_fp) {
					cfg->vret_addr->inst_offset = offset;
					offset += 8;
				} else {
					offset += 8;
					cfg->vret_addr->inst_offset = -offset;
				}
				if (G_UNLIKELY (cfg->verbose_level > 1)) {
					printf ("vret_addr =");
					mono_print_ins (cfg->vret_addr);
				}
			}
			else {
				cfg->ret->opcode = OP_REGVAR;
				cfg->ret->inst_c0 = cinfo->ret.reg;
			}
			break;
		case ArgValuetypeInReg:
			/* Allocate a local to hold the result, the epilog will copy it to the correct place */
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = cfg->frame_reg;
			if (cfg->arch.omit_fp) {
				cfg->ret->inst_offset = offset;
				offset += 16;
			} else {
				offset += 16;
				cfg->ret->inst_offset = - offset;
			}
			break;
		default:
			g_assert_not_reached ();
		}
		cfg->ret->dreg = cfg->ret->inst_c0;
	}

	/* Allocate locals */
	offsets = mono_allocate_stack_slots_full (cfg, cfg->arch.omit_fp ? FALSE: TRUE, &locals_stack_size, &locals_stack_align);
	if (locals_stack_align) {
		offset += (locals_stack_align - 1);
		offset &= ~(locals_stack_align - 1);
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
			//printf ("allocated local %d to ", i); mono_print_tree_nl (inst);
		}
	}
	offset += locals_stack_size;

	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG)) {
		g_assert (!cfg->arch.omit_fp);
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

			/* FIXME: Allocate volatile arguments to registers */
			if (inst->flags & (MONO_INST_VOLATILE|MONO_INST_INDIRECT))
				inreg = FALSE;

			/* 
			 * Under AMD64, all registers used to pass arguments to functions
			 * are volatile across calls.
			 * FIXME: Optimize this.
			 */
			if ((ainfo->storage == ArgInIReg) || (ainfo->storage == ArgInFloatSSEReg) || (ainfo->storage == ArgInDoubleSSEReg) || (ainfo->storage == ArgValuetypeInReg))
				inreg = FALSE;

			inst->opcode = OP_REGOFFSET;

			switch (ainfo->storage) {
			case ArgInIReg:
			case ArgInFloatSSEReg:
			case ArgInDoubleSSEReg:
				inst->opcode = OP_REGVAR;
				inst->dreg = ainfo->reg;
				break;
			case ArgOnStack:
				g_assert (!cfg->arch.omit_fp);
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = cfg->frame_reg;
				inst->inst_offset = ainfo->offset + ARGS_OFFSET;
				break;
			case ArgValuetypeInReg:
				break;
			default:
				NOT_IMPLEMENTED;
			}

			if (!inreg && (ainfo->storage != ArgOnStack)) {
				inst->opcode = OP_REGOFFSET;
				inst->inst_basereg = cfg->frame_reg;
				/* These arguments are saved to the stack in the prolog */
				offset = ALIGN_TO (offset, sizeof (gpointer));
				if (cfg->arch.omit_fp) {
					inst->inst_offset = offset;
					offset += (ainfo->storage == ArgValuetypeInReg) ? 2 * sizeof (gpointer) : sizeof (gpointer);
				} else {
					offset += (ainfo->storage == ArgValuetypeInReg) ? 2 * sizeof (gpointer) : sizeof (gpointer);
					inst->inst_offset = - offset;
				}
			}
		}
	}

	cfg->stack_offset = offset;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig, FALSE);
	cinfo = cfg->arch.cinfo;

	if (cinfo->ret.storage == ArgValuetypeInReg)
		cfg->ret_var_is_local = TRUE;

	if ((cinfo->ret.storage != ArgValuetypeInReg) && MONO_TYPE_ISSTRUCT (sig->ret)) {
		cfg->vret_addr = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}
}

static void
add_outarg_reg (MonoCompile *cfg, MonoCallInst *call, MonoInst *arg, ArgStorage storage, int reg, MonoInst *tree)
{
	switch (storage) {
	case ArgInIReg:
		arg->opcode = OP_OUTARG_REG;
		arg->inst_left = tree;
		arg->inst_call = call;
		arg->backend.reg3 = reg;
		break;
	case ArgInFloatSSEReg:
		arg->opcode = OP_AMD64_OUTARG_XMMREG_R4;
		arg->inst_left = tree;
		arg->inst_call = call;
		arg->backend.reg3 = reg;
		break;
	case ArgInDoubleSSEReg:
		arg->opcode = OP_AMD64_OUTARG_XMMREG_R8;
		arg->inst_left = tree;
		arg->inst_call = call;
		arg->backend.reg3 = reg;
		break;
	default:
		g_assert_not_reached ();
	}
}

/* Fixme: we need an alignment solution for enter_method and mono_arch_call_opcode,
 * currently alignment in mono_arch_call_opcode is computed without arch_get_argument_info 
 */

static int
arg_storage_to_ldind (ArgStorage storage)
{
	switch (storage) {
	case ArgInIReg:
		return CEE_LDIND_I;
	case ArgInDoubleSSEReg:
		return CEE_LDIND_R8;
	case ArgInFloatSSEReg:
		return CEE_LDIND_R4;
	default:
		g_assert_not_reached ();
	}

	return -1;
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoInst *arg;
	MonoMethodSignature *tmp_sig;
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
	MONO_INST_LIST_ADD (&arg->node, &call->out_args);
}

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
	int i, n, stack_size;
	CallInfo *cinfo;
	ArgInfo *ainfo;

	stack_size = 0;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig, sig->pinvoke);

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
		}

		if (is_virtual && i == 0) {
			/* the argument will be attached to the call instruction */
			in = call->args [i];
		} else {
			MONO_INST_NEW (cfg, arg, OP_OUTARG);
			in = call->args [i];
			arg->cil_code = in->cil_code;
			arg->inst_left = in;
			arg->type = in->type;
			if (!cinfo->stack_usage)
				/* Keep the assignments to the arg registers in order if possible */
				MONO_INST_LIST_ADD_TAIL (&arg->node, &call->out_args);
			else
				MONO_INST_LIST_ADD (&arg->node, &call->out_args);

			if ((i >= sig->hasthis) && (MONO_TYPE_ISSTRUCT(sig->params [i - sig->hasthis]))) {
				guint32 align;
				guint32 size;

				if (sig->params [i - sig->hasthis]->type == MONO_TYPE_TYPEDBYREF) {
					size = sizeof (MonoTypedRef);
					align = sizeof (gpointer);
				}
				else
				if (sig->pinvoke)
					size = mono_type_native_stack_size (&in->klass->byval_arg, &align);
				else {
					/* 
					 * Other backends use mini_type_stack_size (), but that
					 * aligns the size to 8, which is larger than the size of
					 * the source, leading to reads of invalid memory if the
					 * source is at the end of address space.
					 */
					size = mono_class_value_size (in->klass, &align);
				}
				if (ainfo->storage == ArgValuetypeInReg) {
					if (ainfo->pair_storage [1] == ArgNone) {
						MonoInst *load;

						/* Simpler case */

						MONO_INST_NEW (cfg, load, arg_storage_to_ldind (ainfo->pair_storage [0]));
						load->inst_left = in;

						add_outarg_reg (cfg, call, arg, ainfo->pair_storage [0], ainfo->pair_regs [0], load);
					}
					else {
						/* Trees can't be shared so make a copy */
						MonoInst *vtaddr = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
						MonoInst *load, *load2, *offset_ins;

						/* Reg1 */
						MONO_INST_NEW (cfg, load, CEE_LDIND_I);
						load->ssa_op = MONO_SSA_LOAD;
						load->inst_i0 = (cfg)->varinfo [vtaddr->inst_c0];

						NEW_ICONST (cfg, offset_ins, 0);
						MONO_INST_NEW (cfg, load2, CEE_ADD);
						load2->inst_left = load;
						load2->inst_right = offset_ins;

						MONO_INST_NEW (cfg, load, arg_storage_to_ldind (ainfo->pair_storage [0]));
						load->inst_left = load2;

						add_outarg_reg (cfg, call, arg, ainfo->pair_storage [0], ainfo->pair_regs [0], load);

						/* Reg2 */
						MONO_INST_NEW (cfg, load, CEE_LDIND_I);
						load->ssa_op = MONO_SSA_LOAD;
						load->inst_i0 = (cfg)->varinfo [vtaddr->inst_c0];

						NEW_ICONST (cfg, offset_ins, 8);
						MONO_INST_NEW (cfg, load2, CEE_ADD);
						load2->inst_left = load;
						load2->inst_right = offset_ins;

						MONO_INST_NEW (cfg, load, arg_storage_to_ldind (ainfo->pair_storage [1]));
						load->inst_left = load2;

						MONO_INST_NEW (cfg, arg, OP_OUTARG);
						arg->cil_code = in->cil_code;
						arg->type = in->type;
						MONO_INST_LIST_ADD (&arg->node, &call->out_args);

						add_outarg_reg (cfg, call, arg, ainfo->pair_storage [1], ainfo->pair_regs [1], load);

						/* Prepend a copy inst */
						MONO_INST_NEW (cfg, arg, CEE_STIND_I);
						arg->cil_code = in->cil_code;
						arg->ssa_op = MONO_SSA_STORE;
						arg->inst_left = vtaddr;
						arg->inst_right = in;
						arg->type = in->type;

						MONO_INST_LIST_ADD (&arg->node, &call->out_args);
					}
				}
				else {
					arg->opcode = OP_OUTARG_VT;
					arg->klass = in->klass;
					arg->backend.is_pinvoke = sig->pinvoke;
					arg->inst_imm = size;
				}
			}
			else {
				switch (ainfo->storage) {
				case ArgInIReg:
					add_outarg_reg (cfg, call, arg, ainfo->storage, ainfo->reg, in);
					break;
				case ArgInFloatSSEReg:
				case ArgInDoubleSSEReg:
					add_outarg_reg (cfg, call, arg, ainfo->storage, ainfo->reg, in);
					break;
				case ArgOnStack:
					arg->opcode = OP_OUTARG;
					if (!sig->params [i - sig->hasthis]->byref) {
						if (sig->params [i - sig->hasthis]->type == MONO_TYPE_R4)
							arg->opcode = OP_OUTARG_R4;
						else
							if (sig->params [i - sig->hasthis]->type == MONO_TYPE_R8)
								arg->opcode = OP_OUTARG_R8;
					}
					break;
				default:
					g_assert_not_reached ();
				}
			}
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos)) {
		emit_sig_cookie (cfg, call, cinfo);
	}

	if (cinfo->need_stack_align) {
		MONO_INST_NEW (cfg, arg, OP_AMD64_OUTARG_ALIGN_STACK);
		arg->inst_c0 = 8;
		MONO_INST_LIST_ADD (&arg->node, &call->out_args);
	}

#ifdef PLATFORM_WIN32
	/* Always reserve 32 bytes of stack space on Win64 */
	MONO_INST_NEW (cfg, arg, OP_AMD64_OUTARG_ALIGN_STACK);
	arg->inst_c0 = 32;
	MONO_INST_LIST_ADD_TAIL (&arg->node, &call->out_args);
#endif

	if (cfg->method->save_lmf) {
		MONO_INST_NEW (cfg, arg, OP_AMD64_SAVE_SP_TO_LMF);
		MONO_INST_LIST_ADD_TAIL (&arg->node, &call->out_args);
	}

	call->stack_usage = cinfo->stack_usage;
	cfg->param_area = MAX (cfg->param_area, call->stack_usage);
	cfg->flags |= MONO_CFG_HAS_CALLS;

	return call;
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
		MonoInst *tins = mono_branch_optimize_exception_target (cfg, bb, exc_name); \
		if (tins == NULL) {										\
			mono_add_patch_info (cfg, code - cfg->native_code,   \
					MONO_PATCH_INFO_EXC, exc_name);  \
			x86_branch32 (code, cond, 0, signed);               \
		} else {	\
			EMIT_COND_BRANCH (tins, cond, signed);	\
		}			\
	} while (0); 

#define EMIT_FPCOMPARE(code) do { \
	amd64_fcompp (code); \
	amd64_fnstsw (code); \
} while (0); 

#define EMIT_SSE2_FPFUNC(code, op, dreg, sreg1) do { \
    amd64_movsd_membase_reg (code, AMD64_RSP, -8, (sreg1)); \
	amd64_fld_membase (code, AMD64_RSP, -8, TRUE); \
	amd64_ ##op (code); \
	amd64_fst_membase (code, AMD64_RSP, -8, TRUE, TRUE); \
	amd64_movsd_reg_membase (code, (dreg), AMD64_RSP, -8); \
} while (0);

static guint8*
emit_call_body (MonoCompile *cfg, guint8 *code, guint32 patch_type, gconstpointer data)
{
	/* 
	 * FIXME: Add support for thunks
	 */
	{
		gboolean near_call = FALSE;

		/*
		 * Indirect calls are expensive so try to make a near call if possible.
		 * The caller memory is allocated by the code manager so it is 
		 * guaranteed to be at a 32 bit offset.
		 */

		if (patch_type != MONO_PATCH_INFO_ABS) {
			/* The target is in memory allocated using the code manager */
			near_call = TRUE;

			if ((patch_type == MONO_PATCH_INFO_METHOD) || (patch_type == MONO_PATCH_INFO_METHOD_JUMP)) {
				if (((MonoMethod*)data)->klass->image->assembly->aot_module)
					/* The callee might be an AOT method */
					near_call = FALSE;
				if (((MonoMethod*)data)->dynamic)
					/* The target is in malloc-ed memory */
					near_call = FALSE;
			}

			if (patch_type == MONO_PATCH_INFO_INTERNAL_METHOD) {
				/* 
				 * The call might go directly to a native function without
				 * the wrapper.
				 */
				MonoJitICallInfo *mi = mono_find_jit_icall_by_name (data);
				if (mi) {
					gconstpointer target = mono_icall_get_wrapper (mi);
					if ((((guint64)target) >> 32) != 0)
						near_call = FALSE;
				}
			}
		}
		else {
			if (mono_find_class_init_trampoline_by_addr (data))
				near_call = TRUE;
			else {
				MonoJitICallInfo *info = mono_find_jit_icall_by_addr (data);
				if (info) {
					if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && 
						strstr (cfg->method->name, info->name)) {
						/* A call to the wrapped function */
						if ((((guint64)data) >> 32) == 0)
							near_call = TRUE;
					}
					else if (info->func == info->wrapper) {
						/* No wrapper */
						if ((((guint64)info->func) >> 32) == 0)
							near_call = TRUE;
					}
					else {
						/* See the comment in mono_codegen () */
						if ((info->name [0] != 'v') || (strstr (info->name, "ves_array_new_va_") == NULL && strstr (info->name, "ves_array_element_address_") == NULL))
							near_call = TRUE;
					}
				}
				else if ((((guint64)data) >> 32) == 0)
					near_call = TRUE;
			}
		}

		if (cfg->method->dynamic)
			/* These methods are allocated using malloc */
			near_call = FALSE;

		if (cfg->compile_aot)
			near_call = TRUE;

#ifdef MONO_ARCH_NOMAP32BIT
		near_call = FALSE;
#endif

		if (near_call) {
			/* 
			 * Align the call displacement to an address divisible by 4 so it does
			 * not span cache lines. This is required for code patching to work on SMP
			 * systems.
			 */
			if (((guint32)(code + 1 - cfg->native_code) % 4) != 0)
				amd64_padding (code, 4 - ((guint32)(code + 1 - cfg->native_code) % 4));
			mono_add_patch_info (cfg, code - cfg->native_code, patch_type, data);
			amd64_call_code (code, 0);
		}
		else {
			mono_add_patch_info (cfg, code - cfg->native_code, patch_type, data);
			amd64_set_reg_template (code, GP_SCRATCH_REG);
			amd64_call_reg (code, GP_SCRATCH_REG);
		}
	}

	return code;
}

static inline guint8*
emit_call (MonoCompile *cfg, guint8 *code, guint32 patch_type, gconstpointer data, gboolean win64_adjust_stack)
{
#ifdef PLATFORM_WIN32
	if (win64_adjust_stack)
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 32);
#endif
	code = emit_call_body (cfg, code, patch_type, data);
#ifdef PLATFORM_WIN32
	if (win64_adjust_stack)
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 32);
#endif	
	
	return code;
}

static inline int
store_membase_imm_to_store_membase_reg (int opcode)
{
	switch (opcode) {
	case OP_STORE_MEMBASE_IMM:
		return OP_STORE_MEMBASE_REG;
	case OP_STOREI4_MEMBASE_IMM:
		return OP_STOREI4_MEMBASE_REG;
	case OP_STOREI8_MEMBASE_IMM:
		return OP_STOREI8_MEMBASE_REG;
	}

	return -1;
}

#define INST_IGNORES_CFLAGS(opcode) (!(((opcode) == OP_ADC) || ((opcode) == OP_ADC_IMM) || ((opcode) == OP_IADC) || ((opcode) == OP_IADC_IMM) || ((opcode) == OP_SBB) || ((opcode) == OP_SBB_IMM) || ((opcode) == OP_ISBB) || ((opcode) == OP_ISBB_IMM)))

/*
 * mono_arch_peephole_pass_1:
 *
 *   Perform peephole opts which should/can be performed before local regalloc
 */
void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		MonoInst *last_ins = mono_inst_list_prev (&ins->node, &bb->ins_list);

		switch (ins->opcode) {
		case OP_ADD_IMM:
		case OP_IADD_IMM:
		case OP_LADD_IMM:
			if ((ins->sreg1 < MONO_MAX_IREGS) && (ins->dreg >= MONO_MAX_IREGS) && (ins->inst_imm > 0)) {
				/* 
				 * X86_LEA is like ADD, but doesn't have the
				 * sreg1==dreg restriction. inst_imm > 0 is needed since LEA sign-extends 
				 * its operand to 64 bit.
				 */
				ins->opcode = OP_X86_LEA_MEMBASE;
				ins->inst_basereg = ins->sreg1;
			}
			break;
		case OP_LXOR:
		case OP_IXOR:
			if ((ins->sreg1 == ins->sreg2) && (ins->sreg1 == ins->dreg)) {
				MonoInst *ins2;

				/* 
				 * Replace STORE_MEMBASE_IMM 0 with STORE_MEMBASE_REG since 
				 * the latter has length 2-3 instead of 6 (reverse constant
				 * propagation). These instruction sequences are very common
				 * in the initlocals bblock.
				 */
				for (ins2 = mono_inst_list_next (&ins->node, &bb->ins_list); ins2;
						ins2 = mono_inst_list_next (&ins2->node, &bb->ins_list)) {
					if (((ins2->opcode == OP_STORE_MEMBASE_IMM) || (ins2->opcode == OP_STOREI4_MEMBASE_IMM) || (ins2->opcode == OP_STOREI8_MEMBASE_IMM) || (ins2->opcode == OP_STORE_MEMBASE_IMM)) && (ins2->inst_imm == 0)) {
						ins2->opcode = store_membase_imm_to_store_membase_reg (ins2->opcode);
						ins2->sreg1 = ins->dreg;
					} else if ((ins2->opcode == OP_STOREI1_MEMBASE_IMM) || (ins2->opcode == OP_STOREI2_MEMBASE_IMM) || (ins2->opcode == OP_STOREI8_MEMBASE_REG) || (ins2->opcode == OP_STORE_MEMBASE_REG)) {
						/* Continue */
					} else if (((ins2->opcode == OP_ICONST) || (ins2->opcode == OP_I8CONST)) && (ins2->dreg == ins->dreg) && (ins2->inst_c0 == 0)) {
						NULLIFY_INS (ins2);
						/* Continue */
					} else {
						break;
					}
				}
			}
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			/* OP_COMPARE_IMM (reg, 0) 
			 * --> 
			 * OP_AMD64_TEST_NULL (reg) 
			 */
			if (!ins->inst_imm)
				ins->opcode = OP_AMD64_TEST_NULL;
			break;
		case OP_ICOMPARE_IMM:
			if (!ins->inst_imm)
				ins->opcode = OP_X86_TEST_NULL;
			break;
		case OP_AMD64_ICOMPARE_MEMBASE_IMM:
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
					ins->opcode = OP_ICOMPARE_IMM;
					ins->sreg1 = last_ins->sreg1;

					/* check if we can remove cmp reg,0 with test null */
					if (!ins->inst_imm)
						ins->opcode = OP_X86_TEST_NULL;
				}

			break;
		}

		mono_peephole_ins (bb, ins);
	}
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_ICONST:
		case OP_I8CONST: {
			MonoInst *next;

			/* reg = 0 -> XOR (reg, reg) */
			/* XOR sets cflags on x86, so we cant do it always */
			next = mono_inst_list_next (&ins->node, &bb->ins_list);
			if (ins->inst_c0 == 0 && (!next ||
					(next && INST_IGNORES_CFLAGS (next->opcode)))) {
				ins->opcode = OP_LXOR;
				ins->sreg1 = ins->dreg;
				ins->sreg2 = ins->dreg;
				/* Fall through */
			} else {
				break;
			}
		}
		case OP_LXOR:
			/*
			 * Use IXOR to avoid a rex prefix if possible. The cpu will sign extend the 
			 * 0 result into 64 bits.
			 */
			if ((ins->sreg1 == ins->sreg2) && (ins->sreg1 == ins->dreg)) {
				ins->opcode = OP_IXOR;
			}
			/* Fall through */
		case OP_IXOR:
			if ((ins->sreg1 == ins->sreg2) && (ins->sreg1 == ins->dreg)) {
				MonoInst *ins2;

				/* 
				 * Replace STORE_MEMBASE_IMM 0 with STORE_MEMBASE_REG since 
				 * the latter has length 2-3 instead of 6 (reverse constant
				 * propagation). These instruction sequences are very common
				 * in the initlocals bblock.
				 */
				for (ins2 = mono_inst_list_next (&ins->node, &bb->ins_list); ins2;
						ins2 = mono_inst_list_next (&ins2->node, &bb->ins_list)) {
					if (((ins2->opcode == OP_STORE_MEMBASE_IMM) || (ins2->opcode == OP_STOREI4_MEMBASE_IMM) || (ins2->opcode == OP_STOREI8_MEMBASE_IMM) || (ins2->opcode == OP_STORE_MEMBASE_IMM)) && (ins2->inst_imm == 0)) {
						ins2->opcode = store_membase_imm_to_store_membase_reg (ins2->opcode);
						ins2->sreg1 = ins->dreg;
					} else if ((ins2->opcode == OP_STOREI1_MEMBASE_IMM) || (ins2->opcode == OP_STOREI2_MEMBASE_IMM) || (ins2->opcode == OP_STOREI4_MEMBASE_REG) || (ins2->opcode == OP_STOREI8_MEMBASE_REG) || (ins2->opcode == OP_STORE_MEMBASE_REG)) {
						/* Continue */
					} else if (((ins2->opcode == OP_ICONST) || (ins2->opcode == OP_I8CONST)) && (ins2->dreg == ins->dreg) && (ins2->inst_c0 == 0)) {
						NULLIFY_INS (ins2);
						/* Continue */
					} else {
						break;
					}
				}
			}
			break;
		case OP_IADD_IMM:
			if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_INC_REG;
			break;
		case OP_ISUB_IMM:
			if ((ins->inst_imm == 1) && (ins->dreg == ins->sreg1))
				ins->opcode = OP_X86_DEC_REG;
			break;
		}

		mono_peephole_ins (bb, ins);
	}
}

#define NEW_INS(cfg,ins,dest,op) do {	\
		MONO_INST_NEW ((cfg), (dest), (op)); \
        (dest)->cil_code = (ins)->cil_code; \
		MONO_INST_LIST_ADD_TAIL (&(dest)->node, &(ins)->node); \
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
	MonoInst *ins, *n, *temp;

	if (bb->max_vreg > cfg->rs->next_vreg)
		cfg->rs->next_vreg = bb->max_vreg;

	/*
	 * FIXME: Need to add more instructions, but the current machine 
	 * description can't model some parts of the composite instructions like
	 * cdq.
	 */
	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_DIV_IMM:
		case OP_REM_IMM:
		case OP_IDIV_IMM:
		case OP_IREM_IMM:
		case OP_IDIV_UN_IMM:
		case OP_IREM_UN_IMM:
			mono_decompose_op_imm (cfg, ins);
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			if (!amd64_is_imm32 (ins->inst_imm)) {
				NEW_INS (cfg, ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->opcode = OP_COMPARE;
				ins->sreg2 = temp->dreg;
			}
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE:
			if (!amd64_is_imm32 (ins->inst_offset)) {
				NEW_INS (cfg, ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->opcode = OP_AMD64_LOADI8_MEMINDEX;
				ins->inst_indexreg = temp->dreg;
			}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM:
			if (!amd64_is_imm32 (ins->inst_imm)) {
				NEW_INS (cfg, ins, temp, OP_I8CONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_regstate_next_int (cfg->rs);
				ins->opcode = OP_STOREI8_MEMBASE_REG;
				ins->sreg1 = temp->dreg;
			}
			break;
		default:
			break;
		}
	}

	bb->max_vreg = cfg->rs->next_vreg;
}

static const int 
branch_cc_table [] = {
	X86_CC_EQ, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_NE, X86_CC_GE, X86_CC_GT, X86_CC_LE, X86_CC_LT,
	X86_CC_O, X86_CC_NO, X86_CC_C, X86_CC_NC
};

/* Maps CMP_... constants to X86_CC_... constants */
static const int
cc_table [] = {
	X86_CC_EQ, X86_CC_NE, X86_CC_LE, X86_CC_GE, X86_CC_LT, X86_CC_GT,
	X86_CC_LE, X86_CC_GE, X86_CC_LT, X86_CC_GT
};

static const int
cc_signed_table [] = {
	TRUE, TRUE, TRUE, TRUE, TRUE, TRUE,
	FALSE, FALSE, FALSE, FALSE
};

/*#include "cprop.c"*/

static unsigned char*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	amd64_sse_cvttsd2si_reg_reg (code, dreg, sreg);

	if (size == 1)
		amd64_widen_reg (code, dreg, dreg, is_signed, FALSE);
	else if (size == 2)
		amd64_widen_reg (code, dreg, dreg, is_signed, TRUE);
	return code;
}

static unsigned char*
mono_emit_stack_alloc (guchar *code, MonoInst* tree)
{
	int sreg = tree->sreg1;
	int need_touch = FALSE;

#if defined(PLATFORM_WIN32) || defined(MONO_ARCH_SIGSEGV_ON_ALTSTACK)
	if (!tree->flags & MONO_INST_INIT)
		need_touch = TRUE;
#endif

	if (need_touch) {
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
		amd64_test_reg_imm (code, sreg, ~0xFFF);
		br[0] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);

		br[2] = code; /* loop */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 0x1000);
		amd64_test_membase_reg (code, AMD64_RSP, 0, AMD64_RSP);
		amd64_alu_reg_imm (code, X86_SUB, sreg, 0x1000);
		amd64_alu_reg_imm (code, X86_CMP, sreg, 0x1000);
		br[3] = code; x86_branch8 (code, X86_CC_AE, 0, FALSE);
		amd64_patch (br[3], br[2]);
		amd64_test_reg_reg (code, sreg, sreg);
		br[4] = code; x86_branch8 (code, X86_CC_Z, 0, FALSE);
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, sreg);

		br[1] = code; x86_jump8 (code, 0);

		amd64_patch (br[0], code);
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, sreg);
		amd64_patch (br[1], code);
		amd64_patch (br[4], code);
	}
	else
		amd64_alu_reg_reg (code, X86_SUB, AMD64_RSP, tree->sreg1);

	if (tree->flags & MONO_INST_INIT) {
		int offset = 0;
		if (tree->dreg != AMD64_RAX && sreg != AMD64_RAX) {
			amd64_push_reg (code, AMD64_RAX);
			offset += 8;
		}
		if (tree->dreg != AMD64_RCX && sreg != AMD64_RCX) {
			amd64_push_reg (code, AMD64_RCX);
			offset += 8;
		}
		if (tree->dreg != AMD64_RDI && sreg != AMD64_RDI) {
			amd64_push_reg (code, AMD64_RDI);
			offset += 8;
		}
		
		amd64_shift_reg_imm (code, X86_SHR, sreg, 3);
		if (sreg != AMD64_RCX)
			amd64_mov_reg_reg (code, AMD64_RCX, sreg, 8);
		amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
				
		amd64_lea_membase (code, AMD64_RDI, AMD64_RSP, offset);
		amd64_cld (code);
		amd64_prefix (code, X86_REP_PREFIX);
		amd64_stosl (code);
		
		if (tree->dreg != AMD64_RDI && sreg != AMD64_RDI)
			amd64_pop_reg (code, AMD64_RDI);
		if (tree->dreg != AMD64_RCX && sreg != AMD64_RCX)
			amd64_pop_reg (code, AMD64_RCX);
		if (tree->dreg != AMD64_RAX && sreg != AMD64_RAX)
			amd64_pop_reg (code, AMD64_RAX);
	}
	return code;
}

static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	CallInfo *cinfo;
	guint32 quad;

	/* Move return value to the target register */
	/* FIXME: do this in the local reg allocator */
	switch (ins->opcode) {
	case OP_CALL:
	case OP_CALL_REG:
	case OP_CALL_MEMBASE:
	case OP_LCALL:
	case OP_LCALL_REG:
	case OP_LCALL_MEMBASE:
		g_assert (ins->dreg == AMD64_RAX);
		break;
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4) {
			amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, AMD64_XMM0);
		}
		else {
			if (ins->dreg != AMD64_XMM0)
				amd64_sse_movsd_reg_reg (code, ins->dreg, AMD64_XMM0);
		}
		break;
	case OP_VCALL:
	case OP_VCALL_REG:
	case OP_VCALL_MEMBASE:
		cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, ((MonoCallInst*)ins)->signature, FALSE);
		if (cinfo->ret.storage == ArgValuetypeInReg) {
			/* Pop the destination address from the stack */
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
			amd64_pop_reg (code, AMD64_RCX);
			
			for (quad = 0; quad < 2; quad ++) {
				switch (cinfo->ret.pair_storage [quad]) {
				case ArgInIReg:
					amd64_mov_membase_reg (code, AMD64_RCX, (quad * 8), cinfo->ret.pair_regs [quad], 8);
					break;
				case ArgInFloatSSEReg:
					amd64_movss_membase_reg (code, AMD64_RCX, (quad * 8), cinfo->ret.pair_regs [quad]);
					break;
				case ArgInDoubleSSEReg:
					amd64_movsd_membase_reg (code, AMD64_RCX, (quad * 8), cinfo->ret.pair_regs [quad]);
					break;
				case ArgNone:
					break;
				default:
					NOT_IMPLEMENTED;
				}
			}
		}
		break;
	}

	return code;
}

/*
 * emit_tls_get:
 * @code: buffer to store code to
 * @dreg: hard register where to place the result
 * @tls_offset: offset info
 *
 * emit_tls_get emits in @code the native code that puts in the dreg register
 * the item in the thread local storage identified by tls_offset.
 *
 * Returns: a pointer to the end of the stored code
 */
static guint8*
emit_tls_get (guint8* code, int dreg, int tls_offset)
{
	if (optimize_for_xen) {
		x86_prefix (code, X86_FS_PREFIX);
		amd64_mov_reg_mem (code, dreg, 0, 8);
		amd64_mov_reg_membase (code, dreg, dreg, tls_offset, 8);
	} else {
		x86_prefix (code, X86_FS_PREFIX);
		amd64_mov_reg_mem (code, dreg, tls_offset, 8);
	}
	return code;
}

/*
 * emit_load_volatile_arguments:
 *
 *  Load volatile arguments from the stack to the original input registers.
 * Required before a tail call.
 */
static guint8*
emit_load_volatile_arguments (MonoCompile *cfg, guint8 *code)
{
	MonoMethod *method = cfg->method;
	MonoMethodSignature *sig;
	MonoInst *ins;
	CallInfo *cinfo;
	guint32 i, quad;

	/* FIXME: Generate intermediate code instead */

	sig = mono_method_signature (method);

	cinfo = cfg->arch.cinfo;
	
	/* This is the opposite of the code in emit_prolog */
	if (sig->ret->type != MONO_TYPE_VOID) {
		if (cfg->vret_addr && (cfg->vret_addr->opcode != OP_REGVAR))
			amd64_mov_reg_membase (code, cinfo->ret.reg, cfg->vret_addr->inst_basereg, cfg->vret_addr->inst_offset, 8);
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		MonoType *arg_type;
		ins = cfg->args [i];

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		if (ins->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg: {
				guint32 size = 8;

				/* FIXME: I1 etc */
				amd64_mov_reg_membase (code, ainfo->reg, ins->inst_basereg, ins->inst_offset, size);
				break;
			}
			case ArgInFloatSSEReg:
				amd64_movss_reg_membase (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
				break;
			case ArgInDoubleSSEReg:
				amd64_movsd_reg_membase (code, ainfo->reg, ins->inst_basereg, ins->inst_offset);
				break;
			case ArgValuetypeInReg:
				for (quad = 0; quad < 2; quad ++) {
					switch (ainfo->pair_storage [quad]) {
					case ArgInIReg:
						amd64_mov_reg_membase (code, ainfo->pair_regs [quad], ins->inst_basereg, ins->inst_offset + (quad * sizeof (gpointer)), sizeof (gpointer));
						break;
					case ArgInFloatSSEReg:
					case ArgInDoubleSSEReg:
						g_assert_not_reached ();
						break;
					case ArgNone:
						break;
					default:
						g_assert_not_reached ();
					}
				}
				break;
			default:
				break;
			}
		}
		else {
			g_assert (ainfo->storage == ArgInIReg);

			amd64_mov_reg_reg (code, ainfo->reg, ins->dreg, 8);
		}
	}

	return code;
}

#define REAL_PRINT_REG(text,reg) \
mono_assert (reg >= 0); \
amd64_push_reg (code, AMD64_RAX); \
amd64_push_reg (code, AMD64_RDX); \
amd64_push_reg (code, AMD64_RCX); \
amd64_push_reg (code, reg); \
amd64_push_imm (code, reg); \
amd64_push_imm (code, text " %d %p\n"); \
amd64_mov_reg_imm (code, AMD64_RAX, printf); \
amd64_call_reg (code, AMD64_RAX); \
amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 3*4); \
amd64_pop_reg (code, AMD64_RCX); \
amd64_pop_reg (code, AMD64_RDX); \
amd64_pop_reg (code, AMD64_RAX);

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
	guint last_offset = 0;
	int max_len, cpos;

	if (cfg->opt & MONO_OPT_LOOP) {
		int pad, align = LOOP_ALIGNMENT;
		/* set alignment depending on cpu */
		if (bb_is_loop_start (bb) && (pad = (cfg->code_len & (align - 1)))) {
			pad = align - pad;
			/*g_print ("adding %d pad at %x to loop in %s\n", pad, cfg->code_len, cfg->method->name);*/
			amd64_padding (code, pad);
			cfg->code_len += pad;
			bb->native_offset = cfg->code_len;
		}
	}

	if (cfg->verbose_level > 2)
		g_print ("Basic block %d starting at offset 0x%x\n", bb->block_num, bb->native_offset);

	cpos = bb->max_offset;

	if (cfg->prof_options & MONO_PROFILE_COVERAGE) {
		MonoProfileCoverageInfo *cov = cfg->coverage_info;
		g_assert (!cfg->compile_aot);
		cpos += 6;

		cov->data [bb->dfn].cil_code = bb->cil_code;
		amd64_mov_reg_imm (code, AMD64_R11, (guint64)&cov->data [bb->dfn].count);
		/* this is not thread save, but good enough */
		amd64_inc_membase (code, AMD64_R11, 0);
	}

	offset = code - cfg->native_code;

	mono_debug_open_block (cfg, bb, offset);

	MONO_BB_FOR_EACH_INS (bb, ins) {
		offset = code - cfg->native_code;

		max_len = ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];

		if (G_UNLIKELY (offset > (cfg->code_size - max_len - 16))) {
			cfg->code_size *= 2;
			cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
			code = cfg->native_code + offset;
			mono_jit_stats.code_reallocs++;
		}

		if (cfg->debug_info)
			mono_debug_record_line_number (cfg, ins, offset);

		switch (ins->opcode) {
		case OP_BIGMUL:
			amd64_mul_reg (code, ins->sreg2, TRUE);
			break;
		case OP_BIGMUL_UN:
			amd64_mul_reg (code, ins->sreg2, FALSE);
			break;
		case OP_X86_SETEQ_MEMBASE:
			amd64_set_membase (code, X86_CC_EQ, ins->inst_basereg, ins->inst_offset, TRUE);
			break;
		case OP_STOREI1_MEMBASE_IMM:
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 1);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 2);
			break;
		case OP_STOREI4_MEMBASE_IMM:
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_STOREI1_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 1);
			break;
		case OP_STOREI2_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 2);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI8_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 8);
			break;
		case OP_STOREI4_MEMBASE_REG:
			amd64_mov_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1, 4);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI8_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_mov_membase_imm (code, ins->inst_destbasereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_LOAD_MEM:
		case OP_LOADI8_MEM:
			// FIXME: Decompose this earlier
			if (amd64_is_imm32 (ins->inst_imm))
				amd64_mov_reg_mem (code, ins->dreg, ins->inst_imm, sizeof (gpointer));
			else {
				amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
				amd64_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 8);
			}
			break;
		case OP_LOADI4_MEM:
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_movsxd_reg_membase (code, ins->dreg, ins->dreg, 0);
			break;
		case OP_LOADU4_MEM:
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_p0);
			amd64_mov_reg_membase (code, ins->dreg, ins->dreg, 0, 4);
			break;
		case OP_LOADU1_MEM:
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_widen_membase (code, ins->dreg, ins->dreg, 0, FALSE, FALSE);
			break;
		case OP_LOADU2_MEM:
			amd64_mov_reg_imm (code, ins->dreg, ins->inst_imm);
			amd64_widen_membase (code, ins->dreg, ins->dreg, 0, FALSE, TRUE);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI8_MEMBASE:
			g_assert (amd64_is_imm32 (ins->inst_offset));
			amd64_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, sizeof (gpointer));
			break;
		case OP_LOADI4_MEMBASE:
			amd64_movsxd_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADU4_MEMBASE:
			amd64_mov_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_LOADU1_MEMBASE:
			amd64_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, FALSE);
			break;
		case OP_LOADI1_MEMBASE:
			amd64_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, FALSE);
			break;
		case OP_LOADU2_MEMBASE:
			amd64_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, FALSE, TRUE);
			break;
		case OP_LOADI2_MEMBASE:
			amd64_widen_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset, TRUE, TRUE);
			break;
		case OP_AMD64_LOADI8_MEMINDEX:
			amd64_mov_reg_memindex_size (code, ins->dreg, ins->inst_basereg, 0, ins->inst_indexreg, 0, 8);
			break;
		case OP_LCONV_TO_I1:
		case OP_ICONV_TO_I1:
		case OP_SEXT_I1:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, TRUE, FALSE);
			break;
		case OP_LCONV_TO_I2:
		case OP_ICONV_TO_I2:
		case OP_SEXT_I2:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, TRUE, TRUE);
			break;
		case OP_LCONV_TO_U1:
		case OP_ICONV_TO_U1:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, FALSE, FALSE);
			break;
		case OP_LCONV_TO_U2:
		case OP_ICONV_TO_U2:
			amd64_widen_reg (code, ins->dreg, ins->sreg1, FALSE, TRUE);
			break;
		case OP_ZEXT_I4:
			/* Clean out the upper word */
			amd64_mov_reg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_SEXT_I4:
			amd64_movsxd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_COMPARE:
		case OP_LCOMPARE:
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_LCOMPARE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_CMP, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_COMPARE_REG_MEMBASE:
			amd64_alu_reg_membase (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset);
			break;
		case OP_X86_TEST_NULL:
			amd64_test_reg_reg_size (code, ins->sreg1, ins->sreg1, 4);
			break;
		case OP_AMD64_TEST_NULL:
			amd64_test_reg_reg (code, ins->sreg1, ins->sreg1);
			break;

		case OP_X86_ADD_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_ADD, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_SUB_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_SUB, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_AND_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_AND, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_OR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_OR, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_X86_XOR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_XOR, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;

		case OP_X86_ADD_MEMBASE_IMM:
			/* FIXME: Make a 64 version too */
			amd64_alu_membase_imm_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_SUB_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_AND_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_OR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_XOR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_X86_ADD_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_SUB_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_AND_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_OR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_XOR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_X86_INC_MEMBASE:
			amd64_inc_membase_size (code, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_X86_INC_REG:
			amd64_inc_reg_size (code, ins->dreg, 4);
			break;
		case OP_X86_DEC_MEMBASE:
			amd64_dec_membase_size (code, ins->inst_basereg, ins->inst_offset, 4);
			break;
		case OP_X86_DEC_REG:
			amd64_dec_reg_size (code, ins->dreg, 4);
			break;
		case OP_X86_MUL_REG_MEMBASE:
			amd64_imul_reg_membase_size (code, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_AMD64_ICOMPARE_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->sreg2, 4);
			break;
		case OP_AMD64_ICOMPARE_MEMBASE_IMM:
			amd64_alu_membase_imm_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_AMD64_COMPARE_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_COMPARE_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_X86_COMPARE_MEMBASE8_IMM:
			amd64_alu_membase8_imm_size (code, X86_CMP, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 4);
			break;
		case OP_AMD64_ICOMPARE_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset, 4);
			break;
		case OP_AMD64_COMPARE_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_CMP, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;

		case OP_AMD64_ADD_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_ADD, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_SUB_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_SUB, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_AND_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_AND, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_OR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_OR, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;
		case OP_AMD64_XOR_REG_MEMBASE:
			amd64_alu_reg_membase_size (code, X86_XOR, ins->sreg1, ins->sreg2, ins->inst_offset, 8);
			break;

		case OP_AMD64_ADD_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_SUB_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_AND_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_OR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;
		case OP_AMD64_XOR_MEMBASE_REG:
			amd64_alu_membase_reg_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->sreg2, 8);
			break;

		case OP_AMD64_ADD_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_ADD, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_SUB_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_SUB, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_AND_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_AND, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_OR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_OR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;
		case OP_AMD64_XOR_MEMBASE_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_membase_imm_size (code, X86_XOR, ins->inst_basereg, ins->inst_offset, ins->inst_imm, 8);
			break;

		case OP_BREAK:
			amd64_breakpoint (code);
			break;
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_STORE:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_ADDCC:
		case OP_LADD:
			amd64_alu_reg_reg (code, X86_ADD, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
			amd64_alu_reg_reg (code, X86_ADC, ins->sreg1, ins->sreg2);
			break;
		case OP_ADD_IMM:
		case OP_LADD_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_ADD, ins->dreg, ins->inst_imm);
			break;
		case OP_ADC_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_ADC, ins->dreg, ins->inst_imm);
			break;
		case OP_SUBCC:
		case OP_LSUB:
			amd64_alu_reg_reg (code, X86_SUB, ins->sreg1, ins->sreg2);
			break;
		case OP_SBB:
			amd64_alu_reg_reg (code, X86_SBB, ins->sreg1, ins->sreg2);
			break;
		case OP_SUB_IMM:
		case OP_LSUB_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_SUB, ins->dreg, ins->inst_imm);
			break;
		case OP_SBB_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_SBB, ins->dreg, ins->inst_imm);
			break;
		case OP_LAND:
			amd64_alu_reg_reg (code, X86_AND, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_LAND_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_AND, ins->sreg1, ins->inst_imm);
			break;
		case OP_LMUL:
			amd64_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			break;
		case OP_MUL_IMM:
		case OP_LMUL_IMM:
		case OP_IMUL_IMM: {
			guint32 size = (ins->opcode == OP_IMUL_IMM) ? 4 : 8;
			
			switch (ins->inst_imm) {
			case 2:
				/* MOV r1, r2 */
				/* ADD r1, r1 */
				if (ins->dreg != ins->sreg1)
					amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, size);
				amd64_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 3:
				/* LEA r1, [r2 + r2*2] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				break;
			case 5:
				/* LEA r1, [r2 + r2*4] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				break;
			case 6:
				/* LEA r1, [r2 + r2*2] */
				/* ADD r1, r1          */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				amd64_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 9:
				/* LEA r1, [r2 + r2*8] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 3);
				break;
			case 10:
				/* LEA r1, [r2 + r2*4] */
				/* ADD r1, r1          */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				amd64_alu_reg_reg (code, X86_ADD, ins->dreg, ins->dreg);
				break;
			case 12:
				/* LEA r1, [r2 + r2*2] */
				/* SHL r1, 2           */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 1);
				amd64_shift_reg_imm (code, X86_SHL, ins->dreg, 2);
				break;
			case 25:
				/* LEA r1, [r2 + r2*4] */
				/* LEA r1, [r1 + r1*4] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				amd64_lea_memindex (code, ins->dreg, ins->dreg, 0, ins->dreg, 2);
				break;
			case 100:
				/* LEA r1, [r2 + r2*4] */
				/* SHL r1, 2           */
				/* LEA r1, [r1 + r1*4] */
				amd64_lea_memindex (code, ins->dreg, ins->sreg1, 0, ins->sreg1, 2);
				amd64_shift_reg_imm (code, X86_SHL, ins->dreg, 2);
				amd64_lea_memindex (code, ins->dreg, ins->dreg, 0, ins->dreg, 2);
				break;
			default:
				amd64_imul_reg_reg_imm_size (code, ins->dreg, ins->sreg1, ins->inst_imm, size);
				break;
			}
			break;
		}
		case OP_LDIV:
		case OP_LREM:
			/* Regalloc magic makes the div/rem cases the same */
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_cdq (code);
				amd64_div_membase (code, AMD64_RSP, -8, TRUE);
			} else {
				amd64_cdq (code);
				amd64_div_reg (code, ins->sreg2, TRUE);
			}
			break;
		case OP_LDIV_UN:
		case OP_LREM_UN:
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_membase (code, AMD64_RSP, -8, FALSE);
			} else {
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_reg (code, ins->sreg2, FALSE);
			}
			break;
		case OP_IDIV:
		case OP_IREM:
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_cdq_size (code, 4);
				amd64_div_membase_size (code, AMD64_RSP, -8, TRUE, 4);
			} else {
				amd64_cdq_size (code, 4);
				amd64_div_reg_size (code, ins->sreg2, TRUE, 4);
			}
			break;
		case OP_IDIV_UN:
		case OP_IREM_UN:
			if (ins->sreg2 == AMD64_RDX) {
				amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RDX, 8);
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_membase_size (code, AMD64_RSP, -8, FALSE, 4);
			} else {
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RDX, AMD64_RDX);
				amd64_div_reg_size (code, ins->sreg2, FALSE, 4);
			}
			break;
		case OP_LMUL_OVF:
			amd64_imul_reg_reg (code, ins->sreg1, ins->sreg2);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		case OP_LOR:
			amd64_alu_reg_reg (code, X86_OR, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_LOR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_OR, ins->sreg1, ins->inst_imm);
			break;
		case OP_LXOR:
			amd64_alu_reg_reg (code, X86_XOR, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_LXOR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_alu_reg_imm (code, X86_XOR, ins->sreg1, ins->inst_imm);
			break;
		case OP_LSHL:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg (code, X86_SHL, ins->dreg);
			break;
		case OP_LSHR:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg (code, X86_SAR, ins->dreg);
			break;
		case OP_SHR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm_size (code, X86_SAR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LSHR_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm (code, X86_SAR, ins->dreg, ins->inst_imm);
			break;
		case OP_SHR_UN_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm_size (code, X86_SHR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LSHR_UN_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm (code, X86_SHR, ins->dreg, ins->inst_imm);
			break;
		case OP_LSHR_UN:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg (code, X86_SHR, ins->dreg);
			break;
		case OP_SHL_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm_size (code, X86_SHL, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_LSHL_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_shift_reg_imm (code, X86_SHL, ins->dreg, ins->inst_imm);
			break;

		case OP_IADDCC:
		case OP_IADD:
			amd64_alu_reg_reg_size (code, X86_ADD, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IADC:
			amd64_alu_reg_reg_size (code, X86_ADC, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IADD_IMM:
			amd64_alu_reg_imm_size (code, X86_ADD, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_IADC_IMM:
			amd64_alu_reg_imm_size (code, X86_ADC, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISUBCC:
		case OP_ISUB:
			amd64_alu_reg_reg_size (code, X86_SUB, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_ISBB:
			amd64_alu_reg_reg_size (code, X86_SBB, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_ISUB_IMM:
			amd64_alu_reg_imm_size (code, X86_SUB, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISBB_IMM:
			amd64_alu_reg_imm_size (code, X86_SBB, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_IAND:
			amd64_alu_reg_reg_size (code, X86_AND, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IAND_IMM:
			amd64_alu_reg_imm_size (code, X86_AND, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_IOR:
			amd64_alu_reg_reg_size (code, X86_OR, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IOR_IMM:
			amd64_alu_reg_imm_size (code, X86_OR, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_IXOR:
			amd64_alu_reg_reg_size (code, X86_XOR, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IXOR_IMM:
			amd64_alu_reg_imm_size (code, X86_XOR, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_INEG:
			amd64_neg_reg_size (code, ins->sreg1, 4);
			break;
		case OP_INOT:
			amd64_not_reg_size (code, ins->sreg1, 4);
			break;
		case OP_ISHL:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg_size (code, X86_SHL, ins->dreg, 4);
			break;
		case OP_ISHR:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg_size (code, X86_SAR, ins->dreg, 4);
			break;
		case OP_ISHR_IMM:
			amd64_shift_reg_imm_size (code, X86_SAR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISHR_UN_IMM:
			amd64_shift_reg_imm_size (code, X86_SHR, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_ISHR_UN:
			g_assert (ins->sreg2 == AMD64_RCX);
			amd64_shift_reg_size (code, X86_SHR, ins->dreg, 4);
			break;
		case OP_ISHL_IMM:
			amd64_shift_reg_imm_size (code, X86_SHL, ins->dreg, ins->inst_imm, 4);
			break;
		case OP_IMUL:
			amd64_imul_reg_reg_size (code, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_IMUL_OVF:
			amd64_imul_reg_reg_size (code, ins->sreg1, ins->sreg2, 4);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		case OP_IMUL_OVF_UN:
		case OP_LMUL_OVF_UN: {
			/* the mul operation and the exception check should most likely be split */
			int non_eax_reg, saved_eax = FALSE, saved_edx = FALSE;
			int size = (ins->opcode == OP_IMUL_OVF_UN) ? 4 : 8;
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
					amd64_push_reg (code, X86_EAX);
				}
				amd64_mov_reg_reg (code, X86_EAX, ins->sreg1, size);
				non_eax_reg = ins->sreg2;
			}
			if (ins->dreg == X86_EDX) {
				if (!saved_eax) {
					saved_eax = TRUE;
					amd64_push_reg (code, X86_EAX);
				}
			} else {
				saved_edx = TRUE;
				amd64_push_reg (code, X86_EDX);
			}
			amd64_mul_reg_size (code, non_eax_reg, FALSE, size);
			/* save before the check since pop and mov don't change the flags */
			if (ins->dreg != X86_EAX)
				amd64_mov_reg_reg (code, ins->dreg, X86_EAX, size);
			if (saved_edx)
				amd64_pop_reg (code, X86_EDX);
			if (saved_eax)
				amd64_pop_reg (code, X86_EAX);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_O, FALSE, "OverflowException");
			break;
		}
		case OP_ICOMPARE:
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_ICOMPARE_IMM:
			amd64_alu_reg_imm_size (code, X86_CMP, ins->sreg1, ins->inst_imm, 4);
			break;
		case OP_IBEQ:
		case OP_IBLT:
		case OP_IBGT:
		case OP_IBGE:
		case OP_IBLE:
		case OP_LBEQ:
		case OP_LBLT:
		case OP_LBGT:
		case OP_LBGE:
		case OP_LBLE:
		case OP_IBNE_UN:
		case OP_IBLT_UN:
		case OP_IBGT_UN:
		case OP_IBGE_UN:
		case OP_IBLE_UN:
		case OP_LBNE_UN:
		case OP_LBLT_UN:
		case OP_LBGT_UN:
		case OP_LBGE_UN:
		case OP_LBLE_UN:
			EMIT_COND_BRANCH (ins, cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)]);
			break;

		case OP_LNOT:
			amd64_not_reg (code, ins->sreg1);
			break;
		case OP_LNEG:
			amd64_neg_reg (code, ins->sreg1);
			break;

		case OP_ICONST:
		case OP_I8CONST:
			if ((((guint64)ins->inst_c0) >> 32) == 0)
				amd64_mov_reg_imm_size (code, ins->dreg, ins->inst_c0, 4);
			else
				amd64_mov_reg_imm_size (code, ins->dreg, ins->inst_c0, 8);
			break;
		case OP_AOTCONST:
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			amd64_mov_reg_membase (code, ins->dreg, AMD64_RIP, 0, 8);
			break;
		case OP_MOVE:
			amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, sizeof (gpointer));
			break;
		case OP_AMD64_SET_XMMREG_R4: {
			amd64_sse_cvtsd2ss_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_AMD64_SET_XMMREG_R8: {
			if (ins->dreg != ins->sreg1)
				amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		}
		case OP_JMP: {
			/*
			 * Note: this 'frame destruction' logic is useful for tail calls, too.
			 * Keep in sync with the code in emit_epilog.
			 */
			int pos = 0, i;

			/* FIXME: no tracing support... */
			if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
				code = mono_arch_instrument_epilog (cfg, mono_profiler_method_leave, code, FALSE);

			g_assert (!cfg->method->save_lmf);

			code = emit_load_volatile_arguments (cfg, code);

			if (cfg->arch.omit_fp) {
				guint32 save_offset = 0;
				/* Pop callee-saved registers */
				for (i = 0; i < AMD64_NREG; ++i)
					if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
						amd64_mov_reg_membase (code, i, AMD64_RSP, save_offset, 8);
						save_offset += 8;
					}
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, cfg->arch.stack_alloc_size);
			}
			else {
				for (i = 0; i < AMD64_NREG; ++i)
					if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i)))
						pos -= sizeof (gpointer);
			
				if (pos)
					amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, pos);

				/* Pop registers in reverse order */
				for (i = AMD64_NREG - 1; i > 0; --i)
					if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
						amd64_pop_reg (code, i);
					}

				amd64_leave (code);
			}

			offset = code - cfg->native_code;
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			if (cfg->compile_aot)
				amd64_mov_reg_membase (code, AMD64_R11, AMD64_RIP, 0, 8);
			else
				amd64_set_reg_template (code, AMD64_R11);
			amd64_jump_reg (code, AMD64_R11);
			break;
		}
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			amd64_alu_membase_imm_size (code, X86_CMP, ins->sreg1, 0, 0, 4);
			break;
		case OP_ARGLIST: {
			amd64_lea_membase (code, AMD64_R11, cfg->frame_reg, cfg->sig_cookie);
			amd64_mov_membase_reg (code, ins->sreg1, 0, AMD64_R11, 8);
			break;
		}
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;
			/*
			 * The AMD64 ABI forces callers to know about varargs.
			 */
			if ((call->signature->call_convention == MONO_CALL_VARARG) && (call->signature->pinvoke))
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
			else if ((cfg->method->wrapper_type == MONO_WRAPPER_MANAGED_TO_NATIVE) && (cfg->method->klass->image != mono_defaults.corlib)) {
				/* 
				 * Since the unmanaged calling convention doesn't contain a 
				 * 'vararg' entry, we have to treat every pinvoke call as a
				 * potential vararg call.
				 */
				guint32 nregs, i;
				nregs = 0;
				for (i = 0; i < AMD64_XMM_NREG; ++i)
					if (call->used_fregs & (1 << i))
						nregs ++;
				if (!nregs)
					amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
				else
					amd64_mov_reg_imm (code, AMD64_RAX, nregs);
			}

			if (ins->flags & MONO_INST_HAS_METHOD)
				code = emit_call (cfg, code, MONO_PATCH_INFO_METHOD, call->method, FALSE);
			else
				code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, call->fptr, FALSE);
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention))
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, call->stack_usage);
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			call = (MonoCallInst*)ins;

			if (AMD64_IS_ARGUMENT_REG (ins->sreg1)) {
				amd64_mov_reg_reg (code, AMD64_R11, ins->sreg1, 8);
				ins->sreg1 = AMD64_R11;
			}

			/*
			 * The AMD64 ABI forces callers to know about varargs.
			 */
			if ((call->signature->call_convention == MONO_CALL_VARARG) && (call->signature->pinvoke)) {
				if (ins->sreg1 == AMD64_RAX) {
					amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, 8);
					ins->sreg1 = AMD64_R11;
				}
				amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
			}
			amd64_call_reg (code, ins->sreg1);
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention))
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, call->stack_usage);
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			call = (MonoCallInst*)ins;

			if (AMD64_IS_ARGUMENT_REG (ins->sreg1)) {
				/* 
				 * Can't use R11 because it is clobbered by the trampoline 
				 * code, and the reg value is needed by get_vcall_slot_addr.
				 */
				amd64_mov_reg_reg (code, AMD64_RAX, ins->sreg1, 8);
				ins->sreg1 = AMD64_RAX;
			}

			amd64_call_membase (code, ins->sreg1, ins->inst_offset);
			if (call->stack_usage && !CALLCONV_IS_STDCALL (call->signature->call_convention))
				amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, call->stack_usage);
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_AMD64_SAVE_SP_TO_LMF:
			amd64_mov_membase_reg (code, cfg->frame_reg, cfg->arch.lmf_offset + G_STRUCT_OFFSET (MonoLMF, rsp), AMD64_RSP, 8);
			break;
		case OP_OUTARG:
		case OP_X86_PUSH:
			amd64_push_reg (code, ins->sreg1);
			break;
		case OP_X86_PUSH_IMM:
			g_assert (amd64_is_imm32 (ins->inst_imm));
			amd64_push_imm (code, ins->inst_imm);
			break;
		case OP_X86_PUSH_MEMBASE:
			amd64_push_membase (code, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_X86_PUSH_OBJ: 
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, ins->inst_imm);
			amd64_push_reg (code, AMD64_RDI);
			amd64_push_reg (code, AMD64_RSI);
			amd64_push_reg (code, AMD64_RCX);
			if (ins->inst_offset)
				amd64_lea_membase (code, AMD64_RSI, ins->inst_basereg, ins->inst_offset);
			else
				amd64_mov_reg_reg (code, AMD64_RSI, ins->inst_basereg, 8);
			amd64_lea_membase (code, AMD64_RDI, AMD64_RSP, 3 * 8);
			amd64_mov_reg_imm (code, AMD64_RCX, (ins->inst_imm >> 3));
			amd64_cld (code);
			amd64_prefix (code, X86_REP_PREFIX);
			amd64_movsd (code);
			amd64_pop_reg (code, AMD64_RCX);
			amd64_pop_reg (code, AMD64_RSI);
			amd64_pop_reg (code, AMD64_RDI);
			break;
		case OP_X86_LEA:
			amd64_lea_memindex (code, ins->dreg, ins->sreg1, ins->inst_imm, ins->sreg2, ins->backend.shift_amount);
			break;
		case OP_X86_LEA_MEMBASE:
			amd64_lea_membase (code, ins->dreg, ins->sreg1, ins->inst_imm);
			break;
		case OP_X86_XCHG:
			amd64_xchg_reg_reg (code, ins->sreg1, ins->sreg2, 4);
			break;
		case OP_LOCALLOC:
			/* keep alignment */
			amd64_alu_reg_imm (code, X86_ADD, ins->sreg1, MONO_ARCH_FRAME_ALIGNMENT - 1);
			amd64_alu_reg_imm (code, X86_AND, ins->sreg1, ~(MONO_ARCH_FRAME_ALIGNMENT - 1));
			code = mono_emit_stack_alloc (code, ins);
			amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);
			break;
		case OP_LOCALLOC_IMM: {
			guint32 size = ins->inst_imm;
			size = (size + (MONO_ARCH_FRAME_ALIGNMENT - 1)) & ~ (MONO_ARCH_FRAME_ALIGNMENT - 1);

			if (ins->flags & MONO_INST_INIT) {
				/* FIXME: Optimize this */
				amd64_mov_reg_imm (code, ins->dreg, size);
				ins->sreg1 = ins->dreg;

				code = mono_emit_stack_alloc (code, ins);
				amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);
			} else {
				amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, size);
				amd64_mov_reg_reg (code, ins->dreg, AMD64_RSP, 8);
			}
			break;
		}
		case OP_THROW: {
			amd64_mov_reg_reg (code, AMD64_ARG_REG1, ins->sreg1, 8);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception", FALSE);
			break;
		}
		case OP_RETHROW: {
			amd64_mov_reg_reg (code, AMD64_ARG_REG1, ins->sreg1, 8);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception", FALSE);
			break;
		}
		case OP_CALL_HANDLER: 
			/* Align stack */
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			amd64_call_imm (code, 0);
			/* Restore stack alignment */
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
			break;
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			amd64_mov_membase_reg (code, spvar->inst_basereg, spvar->inst_offset, AMD64_RSP, 8);
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			amd64_mov_reg_membase (code, AMD64_RSP, spvar->inst_basereg, spvar->inst_offset, 8);
			amd64_ret (code);
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			amd64_mov_reg_membase (code, AMD64_RSP, spvar->inst_basereg, spvar->inst_offset, 8);
			/* The local allocator will put the result into RAX */
			amd64_ret (code);
			break;
		}

		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case OP_BR:
			if (ins->flags & MONO_INST_BRLABEL) {
				if (ins->inst_i0->inst_c0) {
					amd64_jump_code (code, cfg->native_code + ins->inst_i0->inst_c0);
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
					amd64_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
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
			amd64_jump_reg (code, ins->sreg1);
			break;
		case OP_CEQ:
		case OP_LCEQ:
		case OP_ICEQ:
		case OP_CLT:
		case OP_LCLT:
		case OP_ICLT:
		case OP_CGT:
		case OP_ICGT:
		case OP_LCGT:
		case OP_CLT_UN:
		case OP_LCLT_UN:
		case OP_ICLT_UN:
		case OP_CGT_UN:
		case OP_LCGT_UN:
		case OP_ICGT_UN:
			amd64_set_reg (code, cc_table [mono_opcode_to_cond (ins->opcode)], ins->dreg, cc_signed_table [mono_opcode_to_cond (ins->opcode)]);
			amd64_widen_reg (code, ins->dreg, ins->dreg, FALSE, FALSE);
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
			EMIT_COND_SYSTEM_EXCEPTION (cc_table [mono_opcode_to_cond (ins->opcode)], cc_signed_table [mono_opcode_to_cond (ins->opcode)], ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
		case OP_COND_EXC_NO:
		case OP_COND_EXC_C:
		case OP_COND_EXC_NC:
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_EQ], 
						    (ins->opcode < OP_COND_EXC_NE_UN), ins->inst_p1);
			break;
		case OP_COND_EXC_IOV:
		case OP_COND_EXC_INO:
		case OP_COND_EXC_IC:
		case OP_COND_EXC_INC:
			EMIT_COND_SYSTEM_EXCEPTION (branch_cc_table [ins->opcode - OP_COND_EXC_IEQ], 
						    (ins->opcode < OP_COND_EXC_INE_UN), ins->inst_p1);
			break;

		/* floating point opcodes */
		case OP_R8CONST: {
			double d = *(double *)ins->inst_p0;

			if ((d == 0.0) && (mono_signbit (d) == 0)) {
				amd64_sse_xorpd_reg_reg (code, ins->dreg, ins->dreg);
			}
			else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, ins->inst_p0);
				amd64_sse_movsd_reg_membase (code, ins->dreg, AMD64_RIP, 0);
			}
			break;
		}
		case OP_R4CONST: {
			float f = *(float *)ins->inst_p0;

			if ((f == 0.0) && (mono_signbit (f) == 0)) {
				amd64_sse_xorpd_reg_reg (code, ins->dreg, ins->dreg);
			}
			else {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R4, ins->inst_p0);
				amd64_sse_movss_reg_membase (code, ins->dreg, AMD64_RIP, 0);
				amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, ins->dreg);
			}
			break;
		}
		case OP_STORER8_MEMBASE_REG:
			amd64_sse_movsd_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, ins->sreg1);
			break;
		case OP_LOADR8_SPILL_MEMBASE:
			g_assert_not_reached ();
			break;
		case OP_LOADR8_MEMBASE:
			amd64_sse_movsd_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_STORER4_MEMBASE_REG:
			/* This requires a double->single conversion */
			amd64_sse_cvtsd2ss_reg_reg (code, AMD64_XMM15, ins->sreg1);
			amd64_sse_movss_membase_reg (code, ins->inst_destbasereg, ins->inst_offset, AMD64_XMM15);
			break;
		case OP_LOADR4_MEMBASE:
			amd64_sse_movss_reg_membase (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			amd64_sse_cvtss2sd_reg_reg (code, ins->dreg, ins->dreg);
			break;
		case OP_ICONV_TO_R4: /* FIXME: change precision */
		case OP_ICONV_TO_R8:
			amd64_sse_cvtsi2sd_reg_reg_size (code, ins->dreg, ins->sreg1, 4);
			break;
		case OP_LCONV_TO_R4: /* FIXME: change precision */
		case OP_LCONV_TO_R8:
			amd64_sse_cvtsi2sd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_FCONV_TO_R4:
			/* FIXME: nothing to do ?? */
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
		case OP_FCONV_TO_U4:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, FALSE);			
			break;
		case OP_FCONV_TO_I4:
		case OP_FCONV_TO_I:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 4, TRUE);
			break;
		case OP_FCONV_TO_I8:
			code = emit_float_to_int (cfg, code, ins->dreg, ins->sreg1, 8, TRUE);
			break;
		case OP_LCONV_TO_R_UN: { 
			guint8 *br [2];

			/* Based on gcc code */
			amd64_test_reg_reg (code, ins->sreg1, ins->sreg1);
			br [0] = code; x86_branch8 (code, X86_CC_S, 0, TRUE);

			/* Positive case */
			amd64_sse_cvtsi2sd_reg_reg (code, ins->dreg, ins->sreg1);
			br [1] = code; x86_jump8 (code, 0);
			amd64_patch (br [0], code);

			/* Negative case */
			/* Save to the red zone */
			amd64_mov_membase_reg (code, AMD64_RSP, -8, AMD64_RAX, 8);
			amd64_mov_membase_reg (code, AMD64_RSP, -16, AMD64_RCX, 8);
			amd64_mov_reg_reg (code, AMD64_RCX, ins->sreg1, 8);
			amd64_mov_reg_reg (code, AMD64_RAX, ins->sreg1, 8);
			amd64_alu_reg_imm (code, X86_AND, AMD64_RCX, 1);
			amd64_shift_reg_imm (code, X86_SHR, AMD64_RAX, 1);
			amd64_alu_reg_imm (code, X86_OR, AMD64_RAX, AMD64_RCX);
			amd64_sse_cvtsi2sd_reg_reg (code, ins->dreg, AMD64_RAX);
			amd64_sse_addsd_reg_reg (code, ins->dreg, ins->dreg);
			/* Restore */
			amd64_mov_reg_membase (code, AMD64_RCX, AMD64_RSP, -16, 8);
			amd64_mov_reg_membase (code, AMD64_RAX, AMD64_RSP, -8, 8);
			amd64_patch (br [1], code);
			break;
		}
		case OP_LCONV_TO_OVF_U4:
			amd64_alu_reg_imm (code, X86_CMP, ins->sreg1, 0);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_LT, TRUE, "OverflowException");
			amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, 8);
			break;
		case OP_LCONV_TO_OVF_I4_UN:
			amd64_alu_reg_imm (code, X86_CMP, ins->sreg1, 0x7fffffff);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_GT, FALSE, "OverflowException");
			amd64_mov_reg_reg (code, ins->dreg, ins->sreg1, 8);
			break;
		case OP_FMOVE:
			if (ins->dreg != ins->sreg1)
				amd64_sse_movsd_reg_reg (code, ins->dreg, ins->sreg1);
			break;
		case OP_FADD:
			amd64_sse_addsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;
		case OP_FSUB:
			amd64_sse_subsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;		
		case OP_FMUL:
			amd64_sse_mulsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;		
		case OP_FDIV:
			amd64_sse_divsd_reg_reg (code, ins->dreg, ins->sreg2);
			break;		
		case OP_FNEG: {
			static double r8_0 = -0.0;

			g_assert (ins->sreg1 == ins->dreg);
					
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, &r8_0);
			amd64_sse_xorpd_reg_membase (code, ins->dreg, AMD64_RIP, 0);
			break;
		}
		case OP_SIN:
			EMIT_SSE2_FPFUNC (code, fsin, ins->dreg, ins->sreg1);
			break;		
		case OP_COS:
			EMIT_SSE2_FPFUNC (code, fcos, ins->dreg, ins->sreg1);
			break;		
		case OP_ABS: {
			static guint64 d = 0x7fffffffffffffffUL;

			g_assert (ins->sreg1 == ins->dreg);
					
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_R8, &d);
			amd64_sse_andpd_reg_membase (code, ins->dreg, AMD64_RIP, 0);
			break;		
		}
		case OP_SQRT:
			EMIT_SSE2_FPFUNC (code, fsqrt, ins->dreg, ins->sreg1);
			break;
		case OP_IMIN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			amd64_cmov_reg_size (code, X86_CC_GT, TRUE, ins->dreg, ins->sreg2, 4);
			break;
		case OP_IMAX:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg_size (code, X86_CMP, ins->sreg1, ins->sreg2, 4);
			amd64_cmov_reg_size (code, X86_CC_LT, TRUE, ins->dreg, ins->sreg2, 4);
			break;
		case OP_LMIN:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			amd64_cmov_reg (code, X86_CC_GT, TRUE, ins->dreg, ins->sreg2);
			break;
		case OP_LMAX:
			g_assert (cfg->opt & MONO_OPT_CMOV);
			g_assert (ins->dreg == ins->sreg1);
			amd64_alu_reg_reg (code, X86_CMP, ins->sreg1, ins->sreg2);
			amd64_cmov_reg (code, X86_CC_LT, TRUE, ins->dreg, ins->sreg2);
			break;	
		case OP_X86_FPOP:
			break;		
		case OP_FCOMPARE:
			/* 
			 * The two arguments are swapped because the fbranch instructions
			 * depend on this for the non-sse case to work.
			 */
			amd64_sse_comisd_reg_reg (code, ins->sreg2, ins->sreg1);
			break;
		case OP_FCEQ: {
			/* zeroing the register at the start results in 
			 * shorter and faster code (we can also remove the widening op)
			 */
			guchar *unordered_check;
			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_reg (code, ins->sreg1, ins->sreg2);
			unordered_check = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			amd64_set_reg (code, X86_CC_EQ, ins->dreg, FALSE);
			amd64_patch (unordered_check, code);
			break;
		}
		case OP_FCLT:
		case OP_FCLT_UN:
			/* zeroing the register at the start results in 
			 * shorter and faster code (we can also remove the widening op)
			 */
			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_reg (code, ins->sreg2, ins->sreg1);
			if (ins->opcode == OP_FCLT_UN) {
				guchar *unordered_check = code;
				guchar *jump_to_end;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				amd64_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
				jump_to_end = code;
				x86_jump8 (code, 0);
				amd64_patch (unordered_check, code);
				amd64_inc_reg (code, ins->dreg);
				amd64_patch (jump_to_end, code);
			} else {
				amd64_set_reg (code, X86_CC_GT, ins->dreg, FALSE);
			}
			break;
		case OP_FCGT:
		case OP_FCGT_UN: {
			/* zeroing the register at the start results in 
			 * shorter and faster code (we can also remove the widening op)
			 */
			guchar *unordered_check;
			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_reg (code, ins->sreg2, ins->sreg1);
			if (ins->opcode == OP_FCGT) {
				unordered_check = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				amd64_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
				amd64_patch (unordered_check, code);
			} else {
				amd64_set_reg (code, X86_CC_LT, ins->dreg, FALSE);
			}
			break;
		}
		case OP_FCLT_MEMBASE:
		case OP_FCGT_MEMBASE:
		case OP_FCLT_UN_MEMBASE:
		case OP_FCGT_UN_MEMBASE:
		case OP_FCEQ_MEMBASE: {
			guchar *unordered_check, *jump_to_end;
			int x86_cond;

			amd64_alu_reg_reg (code, X86_XOR, ins->dreg, ins->dreg);
			amd64_sse_comisd_reg_membase (code, ins->sreg1, ins->sreg2, ins->inst_offset);

			switch (ins->opcode) {
			case OP_FCEQ_MEMBASE:
				x86_cond = X86_CC_EQ;
				break;
			case OP_FCLT_MEMBASE:
			case OP_FCLT_UN_MEMBASE:
				x86_cond = X86_CC_LT;
				break;
			case OP_FCGT_MEMBASE:
			case OP_FCGT_UN_MEMBASE:
				x86_cond = X86_CC_GT;
				break;
			default:
				g_assert_not_reached ();
			}

			unordered_check = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			amd64_set_reg (code, x86_cond, ins->dreg, FALSE);

			switch (ins->opcode) {
			case OP_FCEQ_MEMBASE:
			case OP_FCLT_MEMBASE:
			case OP_FCGT_MEMBASE:
				amd64_patch (unordered_check, code);
				break;
			case OP_FCLT_UN_MEMBASE:
			case OP_FCGT_UN_MEMBASE:
				jump_to_end = code;
				x86_jump8 (code, 0);
				amd64_patch (unordered_check, code);
				amd64_inc_reg (code, ins->dreg);
				amd64_patch (jump_to_end, code);
				break;
			default:
				break;
			}
			break;
		}
		case OP_FBEQ: {
			guchar *jump = code;
			x86_branch8 (code, X86_CC_P, 0, TRUE);
			EMIT_COND_BRANCH (ins, X86_CC_EQ, FALSE);
			amd64_patch (jump, code);
			break;
		}
		case OP_FBNE_UN:
			/* Branch if C013 != 100 */
			/* branch if !ZF or (PF|CF) */
			EMIT_COND_BRANCH (ins, X86_CC_NE, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_B, FALSE);
			break;
		case OP_FBLT:
			EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_GT, FALSE);
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
			if (ins->opcode == OP_FBGT) {
				guchar *br1;

				/* skip branch if C1=1 */
				br1 = code;
				x86_branch8 (code, X86_CC_P, 0, FALSE);
				/* branch if (C0 | C3) = 1 */
				EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
				amd64_patch (br1, code);
				break;
			} else {
				EMIT_COND_BRANCH (ins, X86_CC_LT, FALSE);
			}
			break;
		case OP_FBGE: {
			/* Branch if C013 == 100 or 001 */
			guchar *br1;

			/* skip branch if C1=1 */
			br1 = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			/* branch if (C0 | C3) = 1 */
			EMIT_COND_BRANCH (ins, X86_CC_BE, FALSE);
			amd64_patch (br1, code);
			break;
		}
		case OP_FBGE_UN:
			/* Branch if C013 == 000 */
			EMIT_COND_BRANCH (ins, X86_CC_LE, FALSE);
			break;
		case OP_FBLE: {
			/* Branch if C013=000 or 100 */
			guchar *br1;

			/* skip branch if C1=1 */
			br1 = code;
			x86_branch8 (code, X86_CC_P, 0, FALSE);
			/* branch if C0=0 */
			EMIT_COND_BRANCH (ins, X86_CC_NB, FALSE);
			amd64_patch (br1, code);
			break;
		}
		case OP_FBLE_UN:
			/* Branch if C013 != 001 */
			EMIT_COND_BRANCH (ins, X86_CC_P, FALSE);
			EMIT_COND_BRANCH (ins, X86_CC_GE, FALSE);
			break;
		case OP_CKFINITE:
			/* Transfer value to the fp stack */
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 16);
			amd64_movsd_membase_reg (code, AMD64_RSP, 0, ins->sreg1);
			amd64_fld_membase (code, AMD64_RSP, 0, TRUE);

			amd64_push_reg (code, AMD64_RAX);
			amd64_fxam (code);
			amd64_fnstsw (code);
			amd64_alu_reg_imm (code, X86_AND, AMD64_RAX, 0x4100);
			amd64_alu_reg_imm (code, X86_CMP, AMD64_RAX, X86_FP_C0);
			amd64_pop_reg (code, AMD64_RAX);
			amd64_fstp (code, 0);
			EMIT_COND_SYSTEM_EXCEPTION (X86_CC_EQ, FALSE, "ArithmeticException");
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 16);
			break;
		case OP_TLS_GET: {
			code = emit_tls_get (code, ins->dreg, ins->inst_offset);
			break;
		}
		case OP_MEMORY_BARRIER: {
			/* Not needed on amd64 */
			break;
		}
		case OP_ATOMIC_ADD_I4:
		case OP_ATOMIC_ADD_I8: {
			int dreg = ins->dreg;
			guint32 size = (ins->opcode == OP_ATOMIC_ADD_I4) ? 4 : 8;

			if (dreg == ins->inst_basereg)
				dreg = AMD64_R11;
			
			if (dreg != ins->sreg2)
				amd64_mov_reg_reg (code, ins->dreg, ins->sreg2, size);

			x86_prefix (code, X86_LOCK_PREFIX);
			amd64_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, dreg, size);

			if (dreg != ins->dreg)
				amd64_mov_reg_reg (code, ins->dreg, dreg, size);

			break;
		}
		case OP_ATOMIC_ADD_NEW_I4:
		case OP_ATOMIC_ADD_NEW_I8: {
			int dreg = ins->dreg;
			guint32 size = (ins->opcode == OP_ATOMIC_ADD_NEW_I4) ? 4 : 8;

			if ((dreg == ins->sreg2) || (dreg == ins->inst_basereg))
				dreg = AMD64_R11;

			amd64_mov_reg_reg (code, dreg, ins->sreg2, size);
			amd64_prefix (code, X86_LOCK_PREFIX);
			amd64_xadd_membase_reg (code, ins->inst_basereg, ins->inst_offset, dreg, size);
			/* dreg contains the old value, add with sreg2 value */
			amd64_alu_reg_reg_size (code, X86_ADD, dreg, ins->sreg2, size);
			
			if (ins->dreg != dreg)
				amd64_mov_reg_reg (code, ins->dreg, dreg, size);

			break;
		}
		case OP_ATOMIC_EXCHANGE_I4:
		case OP_ATOMIC_EXCHANGE_I8:
		case OP_ATOMIC_CAS_IMM_I4: {
			guchar *br[2];
			int sreg2 = ins->sreg2;
			int breg = ins->inst_basereg;
			guint32 size;
			gboolean need_push = FALSE, rdx_pushed = FALSE;

			if (ins->opcode == OP_ATOMIC_EXCHANGE_I8)
				size = 8;
			else
				size = 4;

			/* 
			 * See http://msdn.microsoft.com/en-us/magazine/cc302329.aspx for
			 * an explanation of how this works.
			 */

			/* cmpxchg uses eax as comperand, need to make sure we can use it
			 * hack to overcome limits in x86 reg allocator 
			 * (req: dreg == eax and sreg2 != eax and breg != eax) 
			 */
			g_assert (ins->dreg == AMD64_RAX);

			if (breg == AMD64_RAX && ins->sreg2 == AMD64_RAX)
				/* Highly unlikely, but possible */
				need_push = TRUE;

			/* The pushes invalidate rsp */
			if ((breg == AMD64_RAX) || need_push) {
				amd64_mov_reg_reg (code, AMD64_R11, breg, 8);
				breg = AMD64_R11;
			}

			/* We need the EAX reg for the comparand */
			if (ins->sreg2 == AMD64_RAX) {
				if (breg != AMD64_R11) {
					amd64_mov_reg_reg (code, AMD64_R11, AMD64_RAX, 8);
					sreg2 = AMD64_R11;
				} else {
					g_assert (need_push);
					amd64_push_reg (code, AMD64_RDX);
					amd64_mov_reg_reg (code, AMD64_RDX, AMD64_RAX, size);
					sreg2 = AMD64_RDX;
					rdx_pushed = TRUE;
				}
			}

			if (ins->opcode == OP_ATOMIC_CAS_IMM_I4) {
				if (ins->backend.data == NULL)
					amd64_alu_reg_reg (code, X86_XOR, AMD64_RAX, AMD64_RAX);
				else
					amd64_mov_reg_imm (code, AMD64_RAX, ins->backend.data);

				amd64_prefix (code, X86_LOCK_PREFIX);
				amd64_cmpxchg_membase_reg_size (code, breg, ins->inst_offset, sreg2, size);
			} else {
				amd64_mov_reg_membase (code, AMD64_RAX, breg, ins->inst_offset, size);

				br [0] = code; amd64_prefix (code, X86_LOCK_PREFIX);
				amd64_cmpxchg_membase_reg_size (code, breg, ins->inst_offset, sreg2, size);
				br [1] = code; amd64_branch8 (code, X86_CC_NE, -1, FALSE);
				amd64_patch (br [1], br [0]);
			}

			if (rdx_pushed)
				amd64_pop_reg (code, AMD64_RDX);

			break;
		}
		default:
			g_warning ("unknown opcode %s in %s()\n", mono_inst_name (ins->opcode), __FUNCTION__);
			g_assert_not_reached ();
		}

		if ((code - cfg->native_code - offset) > max_len) {
			g_warning ("wrong maximal instruction length of instruction %s (expected %d, got %ld)",
				   mono_inst_name (ins->opcode), max_len, code - cfg->native_code - offset);
			g_assert_not_reached ();
		}
	       
		cpos += max_len;

		last_offset = offset;
	}

	cfg->code_len = code - cfg->native_code;
}

void
mono_arch_register_lowlevel_calls (void)
{
}

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;
	gboolean compile_aot = !run_cctors;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		unsigned char *target;

		target = mono_resolve_patch_target (method, domain, code, patch_info, run_cctors);

		if (compile_aot) {
			switch (patch_info->type) {
			case MONO_PATCH_INFO_BB:
			case MONO_PATCH_INFO_LABEL:
				break;
			default:
				/* No need to patch these */
				continue;
			}
		}

		switch (patch_info->type) {
		case MONO_PATCH_INFO_NONE:
			continue;
		case MONO_PATCH_INFO_METHOD_REL:
		case MONO_PATCH_INFO_R8:
		case MONO_PATCH_INFO_R4:
			g_assert_not_reached ();
			continue;
		case MONO_PATCH_INFO_BB:
			break;
		default:
			break;
		}

		/* 
		 * Debug code to help track down problems where the target of a near call is
		 * is not valid.
		 */
		if (amd64_is_near_call (ip)) {
			gint64 disp = (guint8*)target - (guint8*)ip;

			if (!amd64_is_imm32 (disp)) {
				printf ("TYPE: %d\n", patch_info->type);
				switch (patch_info->type) {
				case MONO_PATCH_INFO_INTERNAL_METHOD:
					printf ("V: %s\n", patch_info->data.name);
					break;
				case MONO_PATCH_INFO_METHOD_JUMP:
				case MONO_PATCH_INFO_METHOD:
					printf ("V: %s\n", patch_info->data.method->name);
					break;
				default:
					break;
				}
			}
		}

		amd64_patch (ip, (gpointer)target);
	}
}

static int
get_max_epilog_size (MonoCompile *cfg)
{
	int max_epilog_size = 16;
	
	if (cfg->method->save_lmf)
		max_epilog_size += 256;
	
	if (mono_jit_trace_calls != NULL)
		max_epilog_size += 50;

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		max_epilog_size += 50;

	max_epilog_size += (AMD64_NREG * 2);

	return max_epilog_size;
}

/*
 * This macro is used for testing whenever the unwinder works correctly at every point
 * where an async exception can happen.
 */
/* This will generate a SIGSEGV at the given point in the code */
#define async_exc_point(code) do { \
    if (mono_inject_async_exc_method && mono_method_desc_full_match (mono_inject_async_exc_method, cfg->method)) { \
         if (cfg->arch.async_point_count == mono_inject_async_exc_pos) \
             amd64_mov_reg_mem (code, AMD64_RAX, 0, 4); \
         cfg->arch.async_point_count ++; \
    } \
} while (0)

guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *ins;
	int alloc_size, pos, max_offset, i, quad, max_epilog_size;
	guint8 *code;
	CallInfo *cinfo;
	gint32 lmf_offset = cfg->arch.lmf_offset;
	gboolean args_clobbered = FALSE;
	gboolean trace = FALSE;

	cfg->code_size =  MAX (((MonoMethodNormal *)method)->header->code_size * 4, 10240);

	code = cfg->native_code = g_malloc (cfg->code_size);

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		trace = TRUE;

	/* Amount of stack space allocated by register saving code */
	pos = 0;

	/* 
	 * The prolog consists of the following parts:
	 * FP present:
	 * - push rbp, mov rbp, rsp
	 * - save callee saved regs using pushes
	 * - allocate frame
	 * - save rgctx if needed
	 * - save lmf if needed
	 * FP not present:
	 * - allocate frame
	 * - save rgctx if needed
	 * - save lmf if needed
	 * - save callee saved regs using moves
	 */

	async_exc_point (code);

	if (!cfg->arch.omit_fp) {
		amd64_push_reg (code, AMD64_RBP);
		async_exc_point (code);
		amd64_mov_reg_reg (code, AMD64_RBP, AMD64_RSP, sizeof (gpointer));
		async_exc_point (code);
	}

	/* Save callee saved registers */
	if (!cfg->arch.omit_fp && !method->save_lmf) {
		for (i = 0; i < AMD64_NREG; ++i)
			if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
				amd64_push_reg (code, i);
				pos += sizeof (gpointer);
				async_exc_point (code);
			}
	}

	if (cfg->arch.omit_fp) {
		/* 
		 * On enter, the stack is misaligned by the the pushing of the return
		 * address. It is either made aligned by the pushing of %rbp, or by
		 * this.
		 */
		alloc_size = ALIGN_TO (cfg->stack_offset, 8);
		if ((alloc_size % 16) == 0)
			alloc_size += 8;
	} else {
		alloc_size = ALIGN_TO (cfg->stack_offset, MONO_ARCH_FRAME_ALIGNMENT);

		alloc_size -= pos;
	}

	cfg->arch.stack_alloc_size = alloc_size;

	/* Allocate stack frame */
	if (alloc_size) {
		/* See mono_emit_stack_alloc */
#if defined(PLATFORM_WIN32) || defined(MONO_ARCH_SIGSEGV_ON_ALTSTACK)
		guint32 remaining_size = alloc_size;
		while (remaining_size >= 0x1000) {
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 0x1000);
			async_exc_point (code);
			amd64_test_membase_reg (code, AMD64_RSP, 0, AMD64_RSP);
			remaining_size -= 0x1000;
		}
		if (remaining_size) {
			amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, remaining_size);
			async_exc_point (code);
		}
#else
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, alloc_size);
		async_exc_point (code);
#endif
	}

	/* Stack alignment check */
#if 0
	{
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_RSP, 8);
		amd64_alu_reg_imm (code, X86_AND, AMD64_RAX, 0xf);
		amd64_alu_reg_imm (code, X86_CMP, AMD64_RAX, 0);
		x86_branch8 (code, X86_CC_EQ, 2, FALSE);
		amd64_breakpoint (code);
	}
#endif

	/* Save LMF */
	if (method->save_lmf) {
		/* 
		 * The ip field is not set, the exception handling code will obtain it from the stack location pointed to by the sp field.
		 */
		/* sp is saved right before calls */
		/* Skip method (only needed for trampoline LMF frames) */
		/* Save callee saved regs */
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbx), AMD64_RBX, 8);
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbp), AMD64_RBP, 8);
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r12), AMD64_R12, 8);
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r13), AMD64_R13, 8);
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r14), AMD64_R14, 8);
		amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r15), AMD64_R15, 8);
	}

	/* Save callee saved registers */
	if (cfg->arch.omit_fp && !method->save_lmf) {
		gint32 save_area_offset = 0;

		/* Save caller saved registers after sp is adjusted */
		/* The registers are saved at the bottom of the frame */
		/* FIXME: Optimize this so the regs are saved at the end of the frame in increasing order */
		for (i = 0; i < AMD64_NREG; ++i)
			if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
				amd64_mov_membase_reg (code, AMD64_RSP, save_area_offset, i, 8);
				save_area_offset += 8;
				async_exc_point (code);
			}
	}

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		g_assert (cfg->rgctx_var->opcode == OP_REGOFFSET &&
				(cfg->rgctx_var->inst_basereg == AMD64_RBP || cfg->rgctx_var->inst_basereg == AMD64_RSP));

		amd64_mov_membase_reg (code, cfg->rgctx_var->inst_basereg, cfg->rgctx_var->inst_offset, MONO_ARCH_RGCTX_REG, 8);
	}

	/* compute max_offset in order to use short forward jumps */
	max_offset = 0;
	max_epilog_size = get_max_epilog_size (cfg);
	if (cfg->opt & MONO_OPT_BRANCH) {
		for (bb = cfg->bb_entry; bb; bb = bb->next_bb) {
			bb->max_offset = max_offset;

			if (cfg->prof_options & MONO_PROFILE_COVERAGE)
				max_offset += 6;
			/* max alignment for loops */
			if ((cfg->opt & MONO_OPT_LOOP) && bb_is_loop_start (bb))
				max_offset += LOOP_ALIGNMENT;

			MONO_BB_FOR_EACH_INS (bb, ins) {
				if (ins->opcode == OP_LABEL)
					ins->inst_c1 = max_offset;
				
				max_offset += ((guint8 *)ins_get_spec (ins->opcode))[MONO_INST_LEN];
			}

			if (mono_jit_trace_calls && bb == cfg->bb_exit)
				/* The tracing code can be quite large */
				max_offset += max_epilog_size;
		}
	}

	sig = mono_method_signature (method);
	pos = 0;

	cinfo = cfg->arch.cinfo;

	if (sig->ret->type != MONO_TYPE_VOID) {
		/* Save volatile arguments to the stack */
		if (cfg->vret_addr && (cfg->vret_addr->opcode != OP_REGVAR))
			amd64_mov_membase_reg (code, cfg->vret_addr->inst_basereg, cfg->vret_addr->inst_offset, cinfo->ret.reg, 8);
	}

	/* Keep this in sync with emit_load_volatile_arguments */
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		gint32 stack_offset;
		MonoType *arg_type;

		ins = cfg->args [i];

		if ((ins->flags & MONO_INST_IS_DEAD) && !trace)
			/* Unused arguments */
			continue;

		if (sig->hasthis && (i == 0))
			arg_type = &mono_defaults.object_class->byval_arg;
		else
			arg_type = sig->params [i - sig->hasthis];

		stack_offset = ainfo->offset + ARGS_OFFSET;

		/* Save volatile arguments to the stack */
		if (ins->opcode != OP_REGVAR) {
			switch (ainfo->storage) {
			case ArgInIReg: {
				guint32 size = 8;

				/* FIXME: I1 etc */
				/*
				if (stack_offset & 0x1)
					size = 1;
				else if (stack_offset & 0x2)
					size = 2;
				else if (stack_offset & 0x4)
					size = 4;
				else
					size = 8;
				*/
				amd64_mov_membase_reg (code, ins->inst_basereg, ins->inst_offset, ainfo->reg, size);
				break;
			}
			case ArgInFloatSSEReg:
				amd64_movss_membase_reg (code, ins->inst_basereg, ins->inst_offset, ainfo->reg);
				break;
			case ArgInDoubleSSEReg:
				amd64_movsd_membase_reg (code, ins->inst_basereg, ins->inst_offset, ainfo->reg);
				break;
			case ArgValuetypeInReg:
				for (quad = 0; quad < 2; quad ++) {
					switch (ainfo->pair_storage [quad]) {
					case ArgInIReg:
						amd64_mov_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof (gpointer)), ainfo->pair_regs [quad], sizeof (gpointer));
						break;
					case ArgInFloatSSEReg:
						amd64_movss_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof (gpointer)), ainfo->pair_regs [quad]);
						break;
					case ArgInDoubleSSEReg:
						amd64_movsd_membase_reg (code, ins->inst_basereg, ins->inst_offset + (quad * sizeof (gpointer)), ainfo->pair_regs [quad]);
						break;
					case ArgNone:
						break;
					default:
						g_assert_not_reached ();
					}
				}
				break;
			default:
				break;
			}
		} else {
			/* Argument allocated to (non-volatile) register */
			switch (ainfo->storage) {
			case ArgInIReg:
				amd64_mov_reg_reg (code, ins->dreg, ainfo->reg, 8);
				break;
			case ArgOnStack:
				amd64_mov_reg_membase (code, ins->dreg, AMD64_RBP, ARGS_OFFSET + ainfo->offset, 8);
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	/* Might need to attach the thread to the JIT  or change the domain for the callback */
	if (method->wrapper_type == MONO_WRAPPER_NATIVE_TO_MANAGED) {
		guint64 domain = (guint64)cfg->domain;

		args_clobbered = TRUE;

		/* 
		 * The call might clobber argument registers, but they are already
		 * saved to the stack/global regs.
		 */
		if (appdomain_tls_offset != -1 && lmf_tls_offset != -1) {
			guint8 *buf, *no_domain_branch;

			code = emit_tls_get (code, AMD64_RAX, appdomain_tls_offset);
			if ((domain >> 32) == 0)
				amd64_mov_reg_imm_size (code, AMD64_ARG_REG1, domain, 4);
			else
				amd64_mov_reg_imm_size (code, AMD64_ARG_REG1, domain, 8);
			amd64_alu_reg_reg (code, X86_CMP, AMD64_RAX, AMD64_ARG_REG1);
			no_domain_branch = code;
			x86_branch8 (code, X86_CC_NE, 0, 0);
			code = emit_tls_get ( code, AMD64_RAX, lmf_addr_tls_offset);
			amd64_test_reg_reg (code, AMD64_RAX, AMD64_RAX);
			buf = code;
			x86_branch8 (code, X86_CC_NE, 0, 0);
			amd64_patch (no_domain_branch, code);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					  (gpointer)"mono_jit_thread_attach", FALSE);
			amd64_patch (buf, code);
		} else {
			g_assert (!cfg->compile_aot);
			if ((domain >> 32) == 0)
				amd64_mov_reg_imm_size (code, AMD64_ARG_REG1, domain, 4);
			else
				amd64_mov_reg_imm_size (code, AMD64_ARG_REG1, domain, 8);
			code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD,
					  (gpointer)"mono_jit_thread_attach", FALSE);
		}
	}

	if (method->save_lmf) {
		if ((lmf_tls_offset != -1) && !optimize_for_xen) {
			/*
			 * Optimized version which uses the mono_lmf TLS variable instead of indirection
			 * through the mono_lmf_addr TLS variable.
			 */
			/* %rax = previous_lmf */
			x86_prefix (code, X86_FS_PREFIX);
			amd64_mov_reg_mem (code, AMD64_RAX, lmf_tls_offset, 8);

			/* Save previous_lmf */
			amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_RAX, 8);
			/* Set new lmf */
			if (lmf_offset == 0) {
				x86_prefix (code, X86_FS_PREFIX);
				amd64_mov_mem_reg (code, lmf_tls_offset, cfg->frame_reg, 8);
			} else {
				amd64_lea_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset);
				x86_prefix (code, X86_FS_PREFIX);
				amd64_mov_mem_reg (code, lmf_tls_offset, AMD64_R11, 8);
			}
		} else {
			if (lmf_addr_tls_offset != -1) {
				/* Load lmf quicky using the FS register */
				code = emit_tls_get (code, AMD64_RAX, lmf_addr_tls_offset);
			}
			else {
				/* 
				 * The call might clobber argument registers, but they are already
				 * saved to the stack/global regs.
				 */
				args_clobbered = TRUE;
				code = emit_call (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, 
								  (gpointer)"mono_get_lmf_addr", FALSE);		
			}

			/* Save lmf_addr */
			amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), AMD64_RAX, 8);
			/* Save previous_lmf */
			amd64_mov_reg_membase (code, AMD64_R11, AMD64_RAX, 0, 8);
			amd64_mov_membase_reg (code, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), AMD64_R11, 8);
			/* Set new lmf */
			amd64_lea_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset);
			amd64_mov_membase_reg (code, AMD64_RAX, 0, AMD64_R11, 8);
		}
	}

	if (trace) {
		args_clobbered = TRUE;
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);
	}

	if (cfg->prof_options & MONO_PROFILE_ENTER_LEAVE)
		args_clobbered = TRUE;

	/*
	 * Optimize the common case of the first bblock making a call with the same
	 * arguments as the method. This works because the arguments are still in their
	 * original argument registers.
	 * FIXME: Generalize this
	 */
	if (!args_clobbered) {
		MonoBasicBlock *first_bb = cfg->bb_entry;
		MonoInst *next;

		next = mono_inst_list_first (&first_bb->ins_list);
		if (!next && first_bb->next_bb) {
			first_bb = first_bb->next_bb;
			next = mono_inst_list_first (&first_bb->ins_list);
		}

		if (first_bb->in_count > 1)
			next = NULL;

		for (i = 0; next && i < sig->param_count + sig->hasthis; ++i) {
			ArgInfo *ainfo = cinfo->args + i;
			gboolean match = FALSE;
			
			ins = cfg->args [i];
			if (ins->opcode != OP_REGVAR) {
				switch (ainfo->storage) {
				case ArgInIReg: {
					if (((next->opcode == OP_LOAD_MEMBASE) || (next->opcode == OP_LOADI4_MEMBASE)) && next->inst_basereg == ins->inst_basereg && next->inst_offset == ins->inst_offset) {
						if (next->dreg == ainfo->reg) {
							NULLIFY_INS (next);
							match = TRUE;
						} else {
							next->opcode = OP_MOVE;
							next->sreg1 = ainfo->reg;
							/* Only continue if the instruction doesn't change argument regs */
							if (next->dreg == ainfo->reg || next->dreg == AMD64_RAX)
								match = TRUE;
						}
					}
					break;
				}
				default:
					break;
				}
			} else {
				/* Argument allocated to (non-volatile) register */
				switch (ainfo->storage) {
				case ArgInIReg:
					if (next->opcode == OP_MOVE && next->sreg1 == ins->dreg && next->dreg == ainfo->reg) {
						NULLIFY_INS (next);
						match = TRUE;
					}
					break;
				default:
					break;
				}
			}

			if (match) {
				next = mono_inst_list_next (&next->node, &first_bb->ins_list);
				if (!next)
					break;
			}
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int quad, pos, i;
	guint8 *code;
	int max_epilog_size;
	CallInfo *cinfo;
	gint32 lmf_offset = cfg->arch.lmf_offset;
	
	max_epilog_size = get_max_epilog_size (cfg);

	while (cfg->code_len + max_epilog_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);

	/* the code restoring the registers must be kept in sync with OP_JMP */
	pos = 0;
	
	if (method->save_lmf) {
		if ((lmf_tls_offset != -1) && !optimize_for_xen) {
			/*
			 * Optimized version which uses the mono_lmf TLS variable instead of indirection
			 * through the mono_lmf_addr TLS variable.
			 */
			/* reg = previous_lmf */
			amd64_mov_reg_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), 8);
			x86_prefix (code, X86_FS_PREFIX);
			amd64_mov_mem_reg (code, lmf_tls_offset, AMD64_R11, 8);
		} else {
			/* Restore previous lmf */
			amd64_mov_reg_membase (code, AMD64_RCX, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf), 8);
			amd64_mov_reg_membase (code, AMD64_R11, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr), 8);
			amd64_mov_membase_reg (code, AMD64_R11, 0, AMD64_RCX, 8);
		}

		/* Restore caller saved regs */
		if (cfg->used_int_regs & (1 << AMD64_RBP)) {
			amd64_mov_reg_membase (code, AMD64_RBP, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbp), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_RBX)) {
			amd64_mov_reg_membase (code, AMD64_RBX, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, rbx), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R12)) {
			amd64_mov_reg_membase (code, AMD64_R12, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r12), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R13)) {
			amd64_mov_reg_membase (code, AMD64_R13, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r13), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R14)) {
			amd64_mov_reg_membase (code, AMD64_R14, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r14), 8);
		}
		if (cfg->used_int_regs & (1 << AMD64_R15)) {
			amd64_mov_reg_membase (code, AMD64_R15, cfg->frame_reg, lmf_offset + G_STRUCT_OFFSET (MonoLMF, r15), 8);
		}
	} else {

		if (cfg->arch.omit_fp) {
			gint32 save_area_offset = 0;

			for (i = 0; i < AMD64_NREG; ++i)
				if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
					amd64_mov_reg_membase (code, i, AMD64_RSP, save_area_offset, 8);
					save_area_offset += 8;
				}
		}
		else {
			for (i = 0; i < AMD64_NREG; ++i)
				if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i)))
					pos -= sizeof (gpointer);

			if (pos) {
				if (pos == - sizeof (gpointer)) {
					/* Only one register, so avoid lea */
					for (i = AMD64_NREG - 1; i > 0; --i)
						if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
							amd64_mov_reg_membase (code, i, AMD64_RBP, pos, 8);
						}
				}
				else {
					amd64_lea_membase (code, AMD64_RSP, AMD64_RBP, pos);

					/* Pop registers in reverse order */
					for (i = AMD64_NREG - 1; i > 0; --i)
						if (AMD64_IS_CALLEE_SAVED_REG (i) && (cfg->used_int_regs & (1 << i))) {
							amd64_pop_reg (code, i);
						}
				}
			}
		}
	}

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	if (cinfo->ret.storage == ArgValuetypeInReg) {
		ArgInfo *ainfo = &cinfo->ret;
		MonoInst *inst = cfg->ret;

		for (quad = 0; quad < 2; quad ++) {
			switch (ainfo->pair_storage [quad]) {
			case ArgInIReg:
				amd64_mov_reg_membase (code, ainfo->pair_regs [quad], inst->inst_basereg, inst->inst_offset + (quad * sizeof (gpointer)), sizeof (gpointer));
				break;
			case ArgInFloatSSEReg:
				amd64_movss_reg_membase (code, ainfo->pair_regs [quad], inst->inst_basereg, inst->inst_offset + (quad * sizeof (gpointer)));
				break;
			case ArgInDoubleSSEReg:
				amd64_movsd_reg_membase (code, ainfo->pair_regs [quad], inst->inst_basereg, inst->inst_offset + (quad * sizeof (gpointer)));
				break;
			case ArgNone:
				break;
			default:
				g_assert_not_reached ();
			}
		}
	}

	if (cfg->arch.omit_fp) {
		if (cfg->arch.stack_alloc_size)
			amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, cfg->arch.stack_alloc_size);
	} else {
		amd64_leave (code);
	}
	async_exc_point (code);
	amd64_ret (code);

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

	if (cfg->arch.omit_fp) {
		/* 
		 * Encode the stack size into used_int_regs so the exception handler
		 * can access it.
		 */
		g_assert (cfg->arch.stack_alloc_size < (1 << 16));
		cfg->used_int_regs |= (1 << 31) | (cfg->arch.stack_alloc_size << 16);
	}
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int nthrows, i;
	guint8 *code;
	MonoClass *exc_classes [16];
	guint8 *exc_throw_start [16], *exc_throw_end [16];
	guint32 code_size = 0;

	/* Compute needed space */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC)
			code_size += 40;
		if (patch_info->type == MONO_PATCH_INFO_R8)
			code_size += 8 + 15; /* sizeof (double) + alignment */
		if (patch_info->type == MONO_PATCH_INFO_R4)
			code_size += 4 + 15; /* sizeof (float) + alignment */
	}

	while (cfg->code_len + code_size > (cfg->code_size - 16)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		mono_jit_stats.code_reallocs++;
	}

	code = cfg->native_code + cfg->code_len;

	/* add code to raise exceptions */
	nthrows = 0;
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		switch (patch_info->type) {
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			guint8 *buf, *buf2;
			guint32 throw_ip;

			amd64_patch (patch_info->ip.i + cfg->native_code, code);

			exc_class = mono_class_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			g_assert (exc_class);
			throw_ip = patch_info->ip.i;

			//x86_breakpoint (code);
			/* Find a throw sequence for the same exception class */
			for (i = 0; i < nthrows; ++i)
				if (exc_classes [i] == exc_class)
					break;
			if (i < nthrows) {
				amd64_mov_reg_imm (code, AMD64_ARG_REG2, (exc_throw_end [i] - cfg->native_code) - throw_ip);
				x86_jump_code (code, exc_throw_start [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
			}
			else {
				buf = code;
				amd64_mov_reg_imm_size (code, AMD64_ARG_REG2, 0xf0f0f0f0, 4);
				buf2 = code;

				if (nthrows < 16) {
					exc_classes [nthrows] = exc_class;
					exc_throw_start [nthrows] = code;
				}
				amd64_mov_reg_imm (code, AMD64_ARG_REG1, exc_class->type_token);

				patch_info->type = MONO_PATCH_INFO_NONE;

				code = emit_call_body (cfg, code, MONO_PATCH_INFO_INTERNAL_METHOD, "mono_arch_throw_corlib_exception");

				amd64_mov_reg_imm (buf, AMD64_ARG_REG2, (code - cfg->native_code) - throw_ip);
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

	/* Handle relocations with RIP relative addressing */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		gboolean remove = FALSE;

		switch (patch_info->type) {
		case MONO_PATCH_INFO_R8:
		case MONO_PATCH_INFO_R4: {
			guint8 *pos;

			/* The SSE opcodes require a 16 byte alignment */
			code = (guint8*)ALIGN_TO (code, 16);

			pos = cfg->native_code + patch_info->ip.i;

			if (IS_REX (pos [1]))
				*(guint32*)(pos + 5) = (guint8*)code - pos - 9;
			else
				*(guint32*)(pos + 4) = (guint8*)code - pos - 8;

			if (patch_info->type == MONO_PATCH_INFO_R8) {
				*(double*)code = *(double*)patch_info->data.target;
				code += sizeof (double);
			} else {
				*(float*)code = *(float*)patch_info->data.target;
				code += sizeof (float);
			}

			remove = TRUE;
			break;
		}
		default:
			break;
		}

		if (remove) {
			if (patch_info == cfg->patch_info)
				cfg->patch_info = patch_info->next;
			else {
				MonoJumpInfo *tmp;

				for (tmp = cfg->patch_info; tmp->next != patch_info; tmp = tmp->next)
					;
				tmp->next = patch_info->next;
			}
		}
	}

	cfg->code_len = code - cfg->native_code;

	g_assert (cfg->code_len < cfg->code_size);

}

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
	CallInfo *cinfo = NULL;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int i, n, stack_area = 0;

	/* Keep this in sync with mono_arch_get_argument_info */

	if (enable_arguments) {
		/* Allocate a new area on the stack and save arguments there */
		sig = mono_method_signature (cfg->method);

		cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig, FALSE);

		n = sig->param_count + sig->hasthis;

		stack_area = ALIGN_TO (n * 8, 16);

		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, stack_area);

		for (i = 0; i < n; ++i) {
			inst = cfg->args [i];

			if (inst->opcode == OP_REGVAR)
				amd64_mov_membase_reg (code, AMD64_RSP, (i * 8), inst->dreg, 8);
			else {
				amd64_mov_reg_membase (code, AMD64_R11, inst->inst_basereg, inst->inst_offset, 8);
				amd64_mov_membase_reg (code, AMD64_RSP, (i * 8), AMD64_R11, 8);
			}
		}
	}

	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_METHODCONST, cfg->method);
	amd64_set_reg_template (code, AMD64_ARG_REG1);
	amd64_mov_reg_reg (code, AMD64_ARG_REG2, AMD64_RSP, 8);
	code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func, TRUE);

	if (enable_arguments)
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, stack_area);

	return code;
}

enum {
	SAVE_NONE,
	SAVE_STRUCT,
	SAVE_EAX,
	SAVE_EAX_EDX,
	SAVE_XMM
};

void*
mono_arch_instrument_epilog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;
	int save_mode = SAVE_NONE;
	MonoMethod *method = cfg->method;
	int rtype = mono_type_get_underlying_type (mono_method_signature (method)->ret)->type;
	
	switch (rtype) {
	case MONO_TYPE_VOID:
		/* special case string .ctor icall */
		if (strcmp (".ctor", method->name) && method->klass == mono_defaults.string_class)
			save_mode = SAVE_EAX;
		else
			save_mode = SAVE_NONE;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		save_mode = SAVE_EAX;
		break;
	case MONO_TYPE_R4:
	case MONO_TYPE_R8:
		save_mode = SAVE_XMM;
		break;
	case MONO_TYPE_GENERICINST:
		if (!mono_type_generic_inst_is_valuetype (mono_method_signature (method)->ret)) {
			save_mode = SAVE_EAX;
			break;
		}
		/* Fall through */
	case MONO_TYPE_VALUETYPE:
		save_mode = SAVE_STRUCT;
		break;
	default:
		save_mode = SAVE_EAX;
		break;
	}

	/* Save the result and copy it into the proper argument register */
	switch (save_mode) {
	case SAVE_EAX:
		amd64_push_reg (code, AMD64_RAX);
		/* Align stack */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
		if (enable_arguments)
			amd64_mov_reg_reg (code, AMD64_ARG_REG2, AMD64_RAX, 8);
		break;
	case SAVE_STRUCT:
		/* FIXME: */
		if (enable_arguments)
			amd64_mov_reg_imm (code, AMD64_ARG_REG2, 0);
		break;
	case SAVE_XMM:
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
		amd64_movsd_membase_reg (code, AMD64_RSP, 0, AMD64_XMM0);
		/* Align stack */
		amd64_alu_reg_imm (code, X86_SUB, AMD64_RSP, 8);
		/* 
		 * The result is already in the proper argument register so no copying
		 * needed.
		 */
		break;
	case SAVE_NONE:
		break;
	default:
		g_assert_not_reached ();
	}

	/* Set %al since this is a varargs call */
	if (save_mode == SAVE_XMM)
		amd64_mov_reg_imm (code, AMD64_RAX, 1);
	else
		amd64_mov_reg_imm (code, AMD64_RAX, 0);

	mono_add_patch_info (cfg, code-cfg->native_code, MONO_PATCH_INFO_METHODCONST, method);
	amd64_set_reg_template (code, AMD64_ARG_REG1);
	code = emit_call (cfg, code, MONO_PATCH_INFO_ABS, (gpointer)func, TRUE);

	/* Restore result */
	switch (save_mode) {
	case SAVE_EAX:
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
		amd64_pop_reg (code, AMD64_RAX);
		break;
	case SAVE_STRUCT:
		/* FIXME: */
		break;
	case SAVE_XMM:
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
		amd64_movsd_reg_membase (code, AMD64_XMM0, AMD64_RSP, 0);
		amd64_alu_reg_imm (code, X86_ADD, AMD64_RSP, 8);
		break;
	case SAVE_NONE:
		break;
	default:
		g_assert_not_reached ();
	}

	return code;
}

void
mono_arch_flush_icache (guint8 *code, gint size)
{
	/* Not needed */
}

void
mono_arch_flush_register_windows (void)
{
}

gboolean 
mono_arch_is_inst_imm (gint64 imm)
{
	return amd64_is_imm32 (imm);
}

/*
 * Determine whenever the trap whose info is in SIGINFO is caused by
 * integer overflow.
 */
gboolean
mono_arch_is_int_overflow (void *sigctx, void *info)
{
	MonoContext ctx;
	guint8* rip;
	int reg;
	gint64 value;

	mono_arch_sigctx_to_monoctx (sigctx, &ctx);

	rip = (guint8*)ctx.rip;

	if (IS_REX (rip [0])) {
		reg = amd64_rex_b (rip [0]);
		rip ++;
	}
	else
		reg = 0;

	if ((rip [0] == 0xf7) && (x86_modrm_mod (rip [1]) == 0x3) && (x86_modrm_reg (rip [1]) == 0x7)) {
		/* idiv REG */
		reg += x86_modrm_rm (rip [1]);

		switch (reg) {
		case AMD64_RAX:
			value = ctx.rax;
			break;
		case AMD64_RBX:
			value = ctx.rbx;
			break;
		case AMD64_RCX:
			value = ctx.rcx;
			break;
		case AMD64_RDX:
			value = ctx.rdx;
			break;
		case AMD64_RBP:
			value = ctx.rbp;
			break;
		case AMD64_RSP:
			value = ctx.rsp;
			break;
		case AMD64_RSI:
			value = ctx.rsi;
			break;
		case AMD64_RDI:
			value = ctx.rdi;
			break;
		case AMD64_R12:
			value = ctx.r12;
			break;
		case AMD64_R13:
			value = ctx.r13;
			break;
		case AMD64_R14:
			value = ctx.r14;
			break;
		case AMD64_R15:
			value = ctx.r15;
			break;
		default:
			g_assert_not_reached ();
			reg = -1;
		}			

		if (value == -1)
			return TRUE;
	}

	return FALSE;
}

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	return 3;
}

/**
 * mono_breakpoint_clean_code:
 *
 * Copy @size bytes from @code - @offset to the buffer @buf. If the debugger inserted software
 * breakpoints in the original code, they are removed in the copy.
 *
 * Returns TRUE if no sw breakpoint was present.
 */
gboolean
mono_breakpoint_clean_code (guint8 *method_start, guint8 *code, int offset, guint8 *buf, int size)
{
	int i;
	gboolean can_write = TRUE;
	/*
	 * If method_start is non-NULL we need to perform bound checks, since we access memory
	 * at code - offset we could go before the start of the method and end up in a different
	 * page of memory that is not mapped or read incorrect data anyway. We zero-fill the bytes
	 * instead.
	 */
	if (!method_start || code - offset >= method_start) {
		memcpy (buf, code - offset, size);
	} else {
		int diff = code - method_start;
		memset (buf, 0, size);
		memcpy (buf + offset - diff, method_start, diff + size - offset);
	}
	code -= offset;
	for (i = 0; i < MONO_BREAKPOINT_ARRAY_SIZE; ++i) {
		int idx = mono_breakpoint_info_index [i];
		guint8 *ptr;
		if (idx < 1)
			continue;
		ptr = mono_breakpoint_info [idx].address;
		if (ptr >= code && ptr < code + size) {
			guint8 saved_byte = mono_breakpoint_info [idx].saved_byte;
			can_write = FALSE;
			/*g_print ("patching %p with 0x%02x (was: 0x%02x)\n", ptr, saved_byte, buf [ptr - code]);*/
			buf [ptr - code] = saved_byte;
		}
	}
	return can_write;
}

gpointer
mono_arch_get_vcall_slot (guint8 *code, gpointer *regs, int *displacement)
{
	guint8 buf [10];
	guint32 reg;
	gint32 disp;
	guint8 rex = 0;

	mono_breakpoint_clean_code (NULL, code, 9, buf, sizeof (buf));
	code = buf + 9;

	*displacement = 0;

	/* go to the start of the call instruction
	 *
	 * address_byte = (m << 6) | (o << 3) | reg
	 * call opcode: 0xff address_byte displacement
	 * 0xff m=1,o=2 imm8
	 * 0xff m=2,o=2 imm32
	 */
	code -= 7;

	/* 
	 * A given byte sequence can match more than case here, so we have to be
	 * really careful about the ordering of the cases. Longer sequences
	 * come first.
	 */
#ifdef MONO_ARCH_HAVE_IMT
	if ((code [-2] == 0x41) && (code [-1] == 0xbb) && (code [4] == 0xff) && (x86_modrm_mod (code [5]) == 1) && (x86_modrm_reg (code [5]) == 2) && ((signed char)code [6] < 0)) {
		/* IMT-based interface calls: with MONO_ARCH_IMT_REG == r11
		 * 41 bb 14 f8 28 08       mov    $0x828f814,%r11d
		 * ff 50 fc                call   *0xfffffffc(%rax)
		 */
		reg = amd64_modrm_rm (code [5]);
		disp = (signed char)code [6];
		/* R10 is clobbered by the IMT thunk code */
		g_assert (reg != AMD64_R10);
	}
#else
	if (0) {
	}
#endif
	else if ((code [-1] == 0x8b) && (amd64_modrm_mod (code [0]) == 0x2) && (code [5] == 0xff) && (amd64_modrm_reg (code [6]) == 0x2) && (amd64_modrm_mod (code [6]) == 0x0)) {
			/*
			 * This is a interface call
			 * 48 8b 80 f0 e8 ff ff   mov    0xffffffffffffe8f0(%rax),%rax
			 * ff 10                  callq  *(%rax)
			 */
		if (IS_REX (code [4]))
			rex = code [4];
		reg = amd64_modrm_rm (code [6]);
		disp = 0;
		/* R10 is clobbered by the IMT thunk code */
		g_assert (reg != AMD64_R10);
	} else if ((code [0] == 0x41) && (code [1] == 0xff) && (code [2] == 0x15)) {
		/* call OFFSET(%rip) */
		disp = *(guint32*)(code + 3);
		return (gpointer*)(code + disp + 7);
	}
	else if ((code [1] == 0xff) && (amd64_modrm_reg (code [2]) == 0x2) && (amd64_modrm_mod (code [2]) == 0x2)) {
		/* call *[reg+disp32] */
		if (IS_REX (code [0]))
			rex = code [0];
		reg = amd64_modrm_rm (code [2]);
		disp = *(gint32*)(code + 3);
		/* R10 is clobbered by the IMT thunk code */
		g_assert (reg != AMD64_R10);
	}
	else if (code [2] == 0xe8) {
		/* call <ADDR> */
		return NULL;
	}
	else if (IS_REX (code [4]) && (code [5] == 0xff) && (amd64_modrm_reg (code [6]) == 0x2) && (amd64_modrm_mod (code [6]) == 0x3)) {
		/* call *%reg */
		return NULL;
	}
	else if ((code [4] == 0xff) && (amd64_modrm_reg (code [5]) == 0x2) && (amd64_modrm_mod (code [5]) == 0x1)) {
		/* call *[reg+disp8] */
		if (IS_REX (code [3]))
			rex = code [3];
		reg = amd64_modrm_rm (code [5]);
		disp = *(gint8*)(code + 6);
		//printf ("B: [%%r%d+0x%x]\n", reg, disp);
	}
	else if ((code [5] == 0xff) && (amd64_modrm_reg (code [6]) == 0x2) && (amd64_modrm_mod (code [6]) == 0x0)) {
			/*
			 * This is a interface call: should check the above code can't catch it earlier 
			 * 8b 40 30   mov    0x30(%eax),%eax
			 * ff 10      call   *(%eax)
			 */
		if (IS_REX (code [4]))
			rex = code [4];
		reg = amd64_modrm_rm (code [6]);
		disp = 0;
	}
	else
		g_assert_not_reached ();

	reg += amd64_rex_b (rex);

	/* R11 is clobbered by the trampoline code */
	g_assert (reg != AMD64_R11);

	*displacement = disp;
	return regs [reg];
}

gpointer*
mono_arch_get_vcall_slot_addr (guint8* code, gpointer *regs)
{
	gpointer vt;
	int displacement;
	vt = mono_arch_get_vcall_slot (code, regs, &displacement);
	if (!vt)
		return NULL;
	return (gpointer*)((char*)vt + displacement);
}

int
mono_arch_get_this_arg_reg (MonoMethodSignature *sig, MonoGenericSharingContext *gsctx)
{
	int this_reg = AMD64_ARG_REG1;

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		CallInfo *cinfo = get_call_info (gsctx, NULL, sig, FALSE);
		
		if (cinfo->ret.storage != ArgValuetypeInReg)
			this_reg = AMD64_ARG_REG2;
		g_free (cinfo);
	}

	return this_reg;
}

gpointer
mono_arch_get_this_arg_from_call (MonoMethodSignature *sig, gssize *regs, guint8 *code)
{
	return (gpointer)regs [mono_arch_get_this_arg_reg (sig, NULL)];
}

#define MAX_ARCH_DELEGATE_PARAMS 10

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;
	int i;

	if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
		return NULL;

	/* FIXME: Support more cases */
	if (MONO_TYPE_ISSTRUCT (sig->ret))
		return NULL;

	if (has_target) {
		static guint8* cached = NULL;
		mono_mini_arch_lock ();
		if (cached) {
			mono_mini_arch_unlock ();
			return cached;
		}

		start = code = mono_global_codeman_reserve (64);

		/* Replace the this argument with the target */
		amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
		amd64_mov_reg_membase (code, AMD64_ARG_REG1, AMD64_RAX, G_STRUCT_OFFSET (MonoDelegate, target), 8);
		amd64_jump_membase (code, AMD64_RAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));

		g_assert ((code - start) < 64);

		cached = start;
		mono_debug_add_delegate_trampoline (start, code - start);
		mono_mini_arch_unlock ();
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;
		if (sig->param_count > 4)
			return NULL;

		mono_mini_arch_lock ();
		code = cache [sig->param_count];
		if (code) {
			mono_mini_arch_unlock ();
			return code;
		}

		start = code = mono_global_codeman_reserve (64);

		if (sig->param_count == 0) {
			amd64_jump_membase (code, AMD64_ARG_REG1, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		} else {
			/* We have to shift the arguments left */
			amd64_mov_reg_reg (code, AMD64_RAX, AMD64_ARG_REG1, 8);
			for (i = 0; i < sig->param_count; ++i)
				amd64_mov_reg_reg (code, param_regs [i], param_regs [i + 1], 8);

			amd64_jump_membase (code, AMD64_RAX, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		}
		g_assert ((code - start) < 64);

		cache [sig->param_count] = start;
		
		mono_debug_add_delegate_trampoline (start, code - start);
		mono_mini_arch_unlock ();
	}

	return start;
}

/*
 * Support for fast access to the thread-local lmf structure using the GS
 * segment register on NPTL + kernel 2.6.x.
 */

static gboolean tls_offset_inited = FALSE;

void
mono_arch_setup_jit_tls_data (MonoJitTlsData *tls)
{
	if (!tls_offset_inited) {
		tls_offset_inited = TRUE;
#ifdef MONO_XEN_OPT
		optimize_for_xen = access ("/proc/xen", F_OK) == 0;
#endif
		appdomain_tls_offset = mono_domain_get_tls_offset ();
  		lmf_tls_offset = mono_get_lmf_tls_offset ();
		lmf_addr_tls_offset = mono_get_lmf_addr_tls_offset ();
		thread_tls_offset = mono_thread_get_tls_offset ();
	}		
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

void
mono_arch_emit_this_vret_args (MonoCompile *cfg, MonoCallInst *inst, int this_reg, int this_type, int vt_reg)
{
	MonoCallInst *call = (MonoCallInst*)inst;
	CallInfo * cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, inst->signature, FALSE);

	if (vt_reg != -1) {
		MonoInst *vtarg;

		if (cinfo->ret.storage == ArgValuetypeInReg) {
			/*
			 * The valuetype is in RAX:RDX after the call, need to be copied to
			 * the stack. Push the address here, so the call instruction can
			 * access it.
			 */
			MONO_INST_NEW (cfg, vtarg, OP_X86_PUSH);
			vtarg->sreg1 = vt_reg;
			mono_bblock_add_inst (cfg->cbb, vtarg);

			/* Align stack */
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SUB_IMM, X86_ESP, X86_ESP, 8);
		}
		else {
			MONO_INST_NEW (cfg, vtarg, OP_MOVE);
			vtarg->sreg1 = vt_reg;
			vtarg->dreg = mono_regstate_next_int (cfg->rs);
			mono_bblock_add_inst (cfg->cbb, vtarg);

			mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		}
	}

	/* add the this argument */
	if (this_reg != -1) {
		MonoInst *this;
		MONO_INST_NEW (cfg, this, OP_MOVE);
		this->type = this_type;
		this->sreg1 = this_reg;
		this->dreg = mono_regstate_next_int (cfg->rs);
		mono_bblock_add_inst (cfg->cbb, this);

		mono_call_inst_add_outarg_reg (cfg, call, this->dreg, cinfo->args [0].reg, FALSE);
	}
}

#ifdef MONO_ARCH_HAVE_IMT

#define CMP_SIZE (6 + 1)
#define CMP_REG_REG_SIZE (4 + 1)
#define BR_SMALL_SIZE 2
#define BR_LARGE_SIZE 6
#define MOV_REG_IMM_SIZE 10
#define MOV_REG_IMM_32BIT_SIZE 6
#define JUMP_REG_SIZE (2 + 1)

static int
imt_branch_distance (MonoIMTCheckItem **imt_entries, int start, int target)
{
	int i, distance = 0;
	for (i = start; i < target; ++i)
		distance += imt_entries [i]->chunk_size;
	return distance;
}

/*
 * LOCKING: called with the domain lock held
 */
gpointer
mono_arch_build_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count)
{
	int i;
	int size = 0;
	guint8 *code, *start;
	gboolean vtable_is_32bit = ((gsize)(vtable) == (gsize)(int)(gsize)(vtable));

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done) {
					if (amd64_is_imm32 (item->method))
						item->chunk_size += CMP_SIZE;
					else
						item->chunk_size += MOV_REG_IMM_SIZE + CMP_REG_REG_SIZE;
				}
				if (vtable_is_32bit)
					item->chunk_size += MOV_REG_IMM_32BIT_SIZE;
				else
					item->chunk_size += MOV_REG_IMM_SIZE;
				item->chunk_size += BR_SMALL_SIZE + JUMP_REG_SIZE;
			} else {
				if (vtable_is_32bit)
					item->chunk_size += MOV_REG_IMM_32BIT_SIZE;
				else
					item->chunk_size += MOV_REG_IMM_SIZE;
				item->chunk_size += JUMP_REG_SIZE;
				/* with assert below:
				 * item->chunk_size += CMP_SIZE + BR_SMALL_SIZE + 1;
				 */
			}
		} else {
			if (amd64_is_imm32 (item->method))
				item->chunk_size += CMP_SIZE;
			else
				item->chunk_size += MOV_REG_IMM_SIZE + CMP_REG_REG_SIZE;
			item->chunk_size += BR_LARGE_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}
	code = mono_code_manager_reserve (domain->code_mp, size);
	start = code;
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		item->code_target = code;
		if (item->is_equals) {
			if (item->check_target_idx) {
				if (!item->compare_done) {
					if (amd64_is_imm32 (item->method))
						amd64_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gssize)item->method);
					else {
						amd64_mov_reg_imm (code, AMD64_R10, item->method);
						amd64_alu_reg_reg (code, X86_CMP, MONO_ARCH_IMT_REG, AMD64_R10);
					}
				}
				item->jmp_code = code;
				amd64_branch8 (code, X86_CC_NE, 0, FALSE);
				/* See the comment below about R10 */
				amd64_mov_reg_imm (code, AMD64_R10, & (vtable->vtable [item->vtable_slot]));
				amd64_jump_membase (code, AMD64_R10, 0);
			} else {
				/* enable the commented code to assert on wrong method */
#if 0
				if (amd64_is_imm32 (item->method))
					amd64_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gssize)item->method);
				else {
					amd64_mov_reg_imm (code, AMD64_R10, item->method);
					amd64_alu_reg_reg (code, X86_CMP, MONO_ARCH_IMT_REG, AMD64_R10);
				}
				item->jmp_code = code;
				amd64_branch8 (code, X86_CC_NE, 0, FALSE);
				/* See the comment below about R10 */
				amd64_mov_reg_imm (code, AMD64_R10, & (vtable->vtable [item->vtable_slot]));
				amd64_jump_membase (code, AMD64_R10, 0);
				amd64_patch (item->jmp_code, code);
				amd64_breakpoint (code);
				item->jmp_code = NULL;
#else
				/* We're using R10 here because R11
				   needs to be preserved.  R10 needs
				   to be preserved for calls which
				   require a runtime generic context,
				   but interface calls don't. */
				amd64_mov_reg_imm (code, AMD64_R10, & (vtable->vtable [item->vtable_slot]));
				amd64_jump_membase (code, AMD64_R10, 0);
#endif
			}
		} else {
			if (amd64_is_imm32 (item->method))
				amd64_alu_reg_imm (code, X86_CMP, MONO_ARCH_IMT_REG, (guint32)(gssize)item->method);
			else {
				amd64_mov_reg_imm (code, AMD64_R10, item->method);
				amd64_alu_reg_reg (code, X86_CMP, MONO_ARCH_IMT_REG, AMD64_R10);
			}
			item->jmp_code = code;
			if (x86_is_imm8 (imt_branch_distance (imt_entries, i, item->check_target_idx)))
				x86_branch8 (code, X86_CC_GE, 0, FALSE);
			else
				x86_branch32 (code, X86_CC_GE, 0, FALSE);
		}
		g_assert (code - item->code_target <= item->chunk_size);
	}
	/* patch the branches to get to the target items */
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx) {
				amd64_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
			}
		}
	}
		
	mono_stats.imt_thunks_size += code - start;
	g_assert (code - start <= size);

	return start;
}

MonoMethod*
mono_arch_find_imt_method (gpointer *regs, guint8 *code)
{
	return regs [MONO_ARCH_IMT_REG];
}

MonoObject*
mono_arch_find_this_argument (gpointer *regs, MonoMethod *method, MonoGenericSharingContext *gsctx)
{
	return regs [mono_arch_get_this_arg_reg (mono_method_signature (method), gsctx)];
}
#endif

MonoVTable*
mono_arch_find_static_call_vtable (gpointer *regs, guint8 *code)
{
	return (MonoVTable*) regs [MONO_ARCH_RGCTX_REG];
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
		} else if (strcmp (cmethod->name, "Sqrt") == 0) {
			MONO_INST_NEW (cfg, ins, OP_SQRT);
			ins->inst_i0 = args [0];
		} else if (strcmp (cmethod->name, "Abs") == 0 && fsig->params [0]->type == MONO_TYPE_R8) {
			MONO_INST_NEW (cfg, ins, OP_ABS);
			ins->inst_i0 = args [0];
		}

		if (cfg->opt & MONO_OPT_CMOV) {
			int opcode = 0;

			if (strcmp (cmethod->name, "Min") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMIN;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMIN;
			} else if (strcmp (cmethod->name, "Max") == 0) {
				if (fsig->params [0]->type == MONO_TYPE_I4)
					opcode = OP_IMAX;
				else if (fsig->params [0]->type == MONO_TYPE_I8)
					opcode = OP_LMAX;
			}		

			if (opcode) {
				MONO_INST_NEW (cfg, ins, opcode);
				ins->inst_i0 = args [0];
				ins->inst_i1 = args [1];
			}
		}

#if 0
		/* OP_FREM is not IEEE compatible */
		else if (strcmp (cmethod->name, "IEEERemainder") == 0) {
			MONO_INST_NEW (cfg, ins, OP_FREM);
			ins->inst_i0 = args [0];
			ins->inst_i1 = args [1];
		}
#endif
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

#define _CTX_REG(ctx,fld,i) ((gpointer)((&ctx->fld)[i]))

gpointer
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	switch (reg) {
	case AMD64_RCX: return (gpointer)ctx->rcx;
	case AMD64_RDX: return (gpointer)ctx->rdx;
	case AMD64_RBX: return (gpointer)ctx->rbx;
	case AMD64_RBP: return (gpointer)ctx->rbp;
	case AMD64_RSP: return (gpointer)ctx->rsp;
	default:
		if (reg < 8)
			return _CTX_REG (ctx, rax, reg);
		else if (reg >= 12)
			return _CTX_REG (ctx, r12, reg - 12);
		else
			g_assert_not_reached ();
	}
}
