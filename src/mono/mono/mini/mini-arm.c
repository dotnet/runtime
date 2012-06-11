/*
 * mini-arm.c: ARM backend for the Mono code generator
 *
 * Authors:
 *   Paolo Molaro (lupus@ximian.com)
 *   Dietmar Maurer (dietmar@ximian.com)
 *
 * (C) 2003 Ximian, Inc.
 * Copyright 2003-2011 Novell, Inc (http://www.novell.com)
 * Copyright 2011 Xamarin, Inc (http://www.xamarin.com)
 */
#include "mini.h"
#include <string.h>

#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/utils/mono-mmap.h>

#include "mini-arm.h"
#include "cpu-arm.h"
#include "trace.h"
#include "ir-emit.h"
#include "debugger-agent.h"
#include "mini-gc.h"
#include "mono/arch/arm/arm-fpa-codegen.h"
#include "mono/arch/arm/arm-vfp-codegen.h"

#if defined(__ARM_EABI__) && defined(__linux__) && !defined(PLATFORM_ANDROID)
#define HAVE_AEABI_READ_TP 1
#endif

#ifdef ARM_FPU_VFP_HARD
#define ARM_FPU_VFP 1
#endif

#ifdef ARM_FPU_FPA
#define IS_FPA 1
#else
#define IS_FPA 0
#endif

#ifdef ARM_FPU_VFP
#define IS_VFP 1
#else
#define IS_VFP 0
#endif

#ifdef MONO_ARCH_SOFT_FLOAT
#define IS_SOFT_FLOAT 1
#else
#define IS_SOFT_FLOAT 0
#endif

#define ALIGN_TO(val,align) ((((guint64)val) + ((align) - 1)) & ~((align) - 1))

static gint lmf_tls_offset = -1;
static gint lmf_addr_tls_offset = -1;

/* This mutex protects architecture specific caches */
#define mono_mini_arch_lock() EnterCriticalSection (&mini_arch_mutex)
#define mono_mini_arch_unlock() LeaveCriticalSection (&mini_arch_mutex)
static CRITICAL_SECTION mini_arch_mutex;

static int v5_supported = 0;
static int v6_supported = 0;
static int v7_supported = 0;
static int thumb_supported = 0;
/*
 * Whenever to use the ARM EABI
 */
static int eabi_supported = 0;

/*
 * Whenever we are on arm/darwin aka the iphone.
 */
static int darwin = 0;
/* 
 * Whenever to use the iphone ABI extensions:
 * http://developer.apple.com/library/ios/documentation/Xcode/Conceptual/iPhoneOSABIReference/index.html
 * Basically, r7 is used as a frame pointer and it should point to the saved r7 + lr.
 * This is required for debugging/profiling tools to work, but it has some overhead so it should
 * only be turned on in debug builds.
 */
static int iphone_abi = 0;

/*
 * The FPU we are generating code for. This is NOT runtime configurable right now,
 * since some things like MONO_ARCH_CALLEE_FREGS still depend on defines.
 */
static MonoArmFPU arm_fpu;

static int i8_align;

static volatile int ss_trigger_var = 0;

static gpointer single_step_func_wrapper;
static gpointer breakpoint_func_wrapper;

/*
 * The code generated for sequence points reads from this location, which is
 * made read-only when single stepping is enabled.
 */
static gpointer ss_trigger_page;

/* Enabled breakpoints read from this trigger page */
static gpointer bp_trigger_page;

/* Structure used by the sequence points in AOTed code */
typedef struct {
	gpointer ss_trigger_page;
	gpointer bp_trigger_page;
	guint8* bp_addrs [MONO_ZERO_LEN_ARRAY];
} SeqPointInfo;

/*
 * TODO:
 * floating point support: on ARM it is a mess, there are at least 3
 * different setups, each of which binary incompat with the other.
 * 1) FPA: old and ugly, but unfortunately what current distros use
 *    the double binary format has the two words swapped. 8 double registers.
 *    Implemented usually by kernel emulation.
 * 2) softfloat: the compiler emulates all the fp ops. Usually uses the
 *    ugly swapped double format (I guess a softfloat-vfp exists, too, though).
 * 3) VFP: the new and actually sensible and useful FP support. Implemented
 *    in HW or kernel-emulated, requires new tools. I think this is what symbian uses.
 *
 * The plan is to write the FPA support first. softfloat can be tested in a chroot.
 */
int mono_exc_esp_offset = 0;

#define arm_is_imm12(v) ((v) > -4096 && (v) < 4096)
#define arm_is_imm8(v) ((v) > -256 && (v) < 256)
#define arm_is_fpimm8(v) ((v) >= -1020 && (v) <= 1020)

#define LDR_MASK ((0xf << ARMCOND_SHIFT) | (3 << 26) | (1 << 22) | (1 << 20) | (15 << 12))
#define LDR_PC_VAL ((ARMCOND_AL << ARMCOND_SHIFT) | (1 << 26) | (0 << 22) | (1 << 20) | (15 << 12))
#define IS_LDR_PC(val) (((val) & LDR_MASK) == LDR_PC_VAL)

#define ADD_LR_PC_4 ((ARMCOND_AL << ARMCOND_SHIFT) | (1 << 25) | (1 << 23) | (ARMREG_PC << 16) | (ARMREG_LR << 12) | 4)
#define MOV_LR_PC ((ARMCOND_AL << ARMCOND_SHIFT) | (1 << 24) | (0xa << 20) |  (ARMREG_LR << 12) | ARMREG_PC)
#define DEBUG_IMT 0
 
/* A variant of ARM_LDR_IMM which can handle large offsets */
#define ARM_LDR_IMM_GENERAL(code, dreg, basereg, offset, scratch_reg) do { \
	if (arm_is_imm12 ((offset))) { \
		ARM_LDR_IMM (code, (dreg), (basereg), (offset));	\
	} else {												\
		g_assert ((scratch_reg) != (basereg));					   \
		code = mono_arm_emit_load_imm (code, (scratch_reg), (offset));	\
		ARM_LDR_REG_REG (code, (dreg), (basereg), (scratch_reg));		\
	}																	\
	} while (0)

#define ARM_STR_IMM_GENERAL(code, dreg, basereg, offset, scratch_reg) do {	\
	if (arm_is_imm12 ((offset))) { \
		ARM_STR_IMM (code, (dreg), (basereg), (offset));	\
	} else {												\
		g_assert ((scratch_reg) != (basereg));					   \
		code = mono_arm_emit_load_imm (code, (scratch_reg), (offset));	\
		ARM_STR_REG_REG (code, (dreg), (basereg), (scratch_reg));		\
	}																	\
	} while (0)

static void mono_arch_compute_omit_fp (MonoCompile *cfg);

const char*
mono_arch_regname (int reg)
{
	static const char * rnames[] = {
		"arm_r0", "arm_r1", "arm_r2", "arm_r3", "arm_v1",
		"arm_v2", "arm_v3", "arm_v4", "arm_v5", "arm_v6",
		"arm_v7", "arm_fp", "arm_ip", "arm_sp", "arm_lr",
		"arm_pc"
	};
	if (reg >= 0 && reg < 16)
		return rnames [reg];
	return "unknown";
}

const char*
mono_arch_fregname (int reg)
{
	static const char * rnames[] = {
		"arm_f0", "arm_f1", "arm_f2", "arm_f3", "arm_f4",
		"arm_f5", "arm_f6", "arm_f7", "arm_f8", "arm_f9",
		"arm_f10", "arm_f11", "arm_f12", "arm_f13", "arm_f14",
		"arm_f15", "arm_f16", "arm_f17", "arm_f18", "arm_f19",
		"arm_f20", "arm_f21", "arm_f22", "arm_f23", "arm_f24",
		"arm_f25", "arm_f26", "arm_f27", "arm_f28", "arm_f29",
		"arm_f30", "arm_f31"
	};
	if (reg >= 0 && reg < 32)
		return rnames [reg];
	return "unknown";
}

#ifndef DISABLE_JIT

static guint8*
emit_big_add (guint8 *code, int dreg, int sreg, int imm)
{
	int imm8, rot_amount;
	if ((imm8 = mono_arm_is_rotated_imm8 (imm, &rot_amount)) >= 0) {
		ARM_ADD_REG_IMM (code, dreg, sreg, imm8, rot_amount);
		return code;
	}
	g_assert (dreg != sreg);
	code = mono_arm_emit_load_imm (code, dreg, imm);
	ARM_ADD_REG_REG (code, dreg, dreg, sreg);
	return code;
}

static guint8*
emit_memcpy (guint8 *code, int size, int dreg, int doffset, int sreg, int soffset)
{
	/* we can use r0-r3, since this is called only for incoming args on the stack */
	if (size > sizeof (gpointer) * 4) {
		guint8 *start_loop;
		code = emit_big_add (code, ARMREG_R0, sreg, soffset);
		code = emit_big_add (code, ARMREG_R1, dreg, doffset);
		start_loop = code = mono_arm_emit_load_imm (code, ARMREG_R2, size);
		ARM_LDR_IMM (code, ARMREG_R3, ARMREG_R0, 0);
		ARM_STR_IMM (code, ARMREG_R3, ARMREG_R1, 0);
		ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_R0, 4);
		ARM_ADD_REG_IMM8 (code, ARMREG_R1, ARMREG_R1, 4);
		ARM_SUBS_REG_IMM8 (code, ARMREG_R2, ARMREG_R2, 4);
		ARM_B_COND (code, ARMCOND_NE, 0);
		arm_patch (code - 4, start_loop);
		return code;
	}
	if (arm_is_imm12 (doffset) && arm_is_imm12 (doffset + size) &&
			arm_is_imm12 (soffset) && arm_is_imm12 (soffset + size)) {
		while (size >= 4) {
			ARM_LDR_IMM (code, ARMREG_LR, sreg, soffset);
			ARM_STR_IMM (code, ARMREG_LR, dreg, doffset);
			doffset += 4;
			soffset += 4;
			size -= 4;
		}
	} else if (size) {
		code = emit_big_add (code, ARMREG_R0, sreg, soffset);
		code = emit_big_add (code, ARMREG_R1, dreg, doffset);
		doffset = soffset = 0;
		while (size >= 4) {
			ARM_LDR_IMM (code, ARMREG_LR, ARMREG_R0, soffset);
			ARM_STR_IMM (code, ARMREG_LR, ARMREG_R1, doffset);
			doffset += 4;
			soffset += 4;
			size -= 4;
		}
	}
	g_assert (size == 0);
	return code;
}

static guint8*
emit_call_reg (guint8 *code, int reg)
{
	if (v5_supported) {
		ARM_BLX_REG (code, reg);
	} else {
		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		if (thumb_supported)
			ARM_BX (code, reg);
		else
			ARM_MOV_REG_REG (code, ARMREG_PC, reg);
	}
	return code;
}

static guint8*
emit_call_seq (MonoCompile *cfg, guint8 *code)
{
	if (cfg->method->dynamic) {
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		code = emit_call_reg (code, ARMREG_IP);
	} else {
		ARM_BL (code, 0);
	}
	return code;
}

static guint8*
emit_move_return_value (MonoCompile *cfg, MonoInst *ins, guint8 *code)
{
	switch (ins->opcode) {
	case OP_FCALL:
	case OP_FCALL_REG:
	case OP_FCALL_MEMBASE:
		if (IS_FPA) {
			if (ins->dreg != ARM_FPA_F0)
				ARM_FPA_MVFD (code, ins->dreg, ARM_FPA_F0);
		} else if (IS_VFP) {
			if (((MonoCallInst*)ins)->signature->ret->type == MONO_TYPE_R4) {
				ARM_FMSR (code, ins->dreg, ARMREG_R0);
				ARM_CVTS (code, ins->dreg, ins->dreg);
			} else {
				ARM_FMDRR (code, ARMREG_R0, ARMREG_R1, ins->dreg);
			}
		}
		break;
	}

	return code;
}

/*
 * emit_save_lmf:
 *
 *   Emit code to push an LMF structure on the LMF stack.
 * On arm, this is intermixed with the initialization of other fields of the structure.
 */
static guint8*
emit_save_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset)
{
	gboolean get_lmf_fast = FALSE;
	int i;

#ifdef HAVE_AEABI_READ_TP
	gint32 lmf_addr_tls_offset = mono_get_lmf_addr_tls_offset ();

	if (lmf_addr_tls_offset != -1) {
		get_lmf_fast = TRUE;

		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
							 (gpointer)"__aeabi_read_tp");
		code = emit_call_seq (cfg, code);

		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_R0, lmf_addr_tls_offset);
		get_lmf_fast = TRUE;
	}
#endif
	if (!get_lmf_fast) {
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
								 (gpointer)"mono_get_lmf_addr");
		code = emit_call_seq (cfg, code);
	}
	/* we build the MonoLMF structure on the stack - see mini-arm.h */
	/* lmf_offset is the offset from the previous stack pointer,
	 * alloc_size is the total stack space allocated, so the offset
	 * of MonoLMF from the current stack ptr is alloc_size - lmf_offset.
	 * The pointer to the struct is put in r1 (new_lmf).
	 * ip is used as scratch
	 * The callee-saved registers are already in the MonoLMF structure
	 */
	code = emit_big_add (code, ARMREG_R1, ARMREG_SP, lmf_offset);
	/* r0 is the result from mono_get_lmf_addr () */
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* new_lmf->previous_lmf = *lmf_addr */
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* *(lmf_addr) = r1 */
	ARM_STR_IMM (code, ARMREG_R1, ARMREG_R0, G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* Skip method (only needed for trampoline LMF frames) */
	ARM_STR_IMM (code, ARMREG_SP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, sp));
	ARM_STR_IMM (code, ARMREG_FP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, fp));
	/* save the current IP */
	ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_PC);
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_R1, G_STRUCT_OFFSET (MonoLMF, ip));

	for (i = 0; i < sizeof (MonoLMF); i += sizeof (mgreg_t))
		mini_gc_set_slot_type_from_fp (cfg, lmf_offset + i, SLOT_NOREF);

	return code;
}

/*
 * emit_save_lmf:
 *
 *   Emit code to pop an LMF structure from the LMF stack.
 */
static guint8*
emit_restore_lmf (MonoCompile *cfg, guint8 *code, gint32 lmf_offset)
{
	int basereg, offset;

	if (lmf_offset < 32) {
		basereg = cfg->frame_reg;
		offset = lmf_offset;
	} else {
		basereg = ARMREG_R2;
		offset = 0;
		code = emit_big_add (code, ARMREG_R2, cfg->frame_reg, lmf_offset);
	}

	/* ip = previous_lmf */
	ARM_LDR_IMM (code, ARMREG_IP, basereg, offset + G_STRUCT_OFFSET (MonoLMF, previous_lmf));
	/* lr = lmf_addr */
	ARM_LDR_IMM (code, ARMREG_LR, basereg, offset + G_STRUCT_OFFSET (MonoLMF, lmf_addr));
	/* *(lmf_addr) = previous_lmf */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_LR, G_STRUCT_OFFSET (MonoLMF, previous_lmf));

	return code;
}

#endif /* #ifndef DISABLE_JIT */

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
	guint32 size, align, pad;
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
		size = mini_type_stack_size_full (NULL, csig->params [k], &align, csig->pinvoke);

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

#define MAX_ARCH_DELEGATE_PARAMS 3

static gpointer
get_delegate_invoke_impl (gboolean has_target, gboolean param_count, guint32 *code_size)
{
	guint8 *code, *start;

	if (has_target) {
		start = code = mono_global_codeman_reserve (12);

		/* Replace the this argument with the target */
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R0, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_R0, G_STRUCT_OFFSET (MonoDelegate, target));
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

		g_assert ((code - start) <= 12);

		mono_arch_flush_icache (start, 12);
	} else {
		int size, i;

		size = 8 + param_count * 4;
		start = code = mono_global_codeman_reserve (size);

		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_R0, G_STRUCT_OFFSET (MonoDelegate, method_ptr));
		/* slide down the arguments */
		for (i = 0; i < param_count; ++i) {
			ARM_MOV_REG_REG (code, (ARMREG_R0 + i), (ARMREG_R0 + i + 1));
		}
		ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

		g_assert ((code - start) <= size);

		mono_arch_flush_icache (start, size);
	}

	if (code_size)
		*code_size = code - start;

	return start;
}

/*
 * mono_arch_get_delegate_invoke_impls:
 *
 *   Return a list of MonoAotTrampInfo structures for the delegate invoke impl
 * trampolines.
 */
GSList*
mono_arch_get_delegate_invoke_impls (void)
{
	GSList *res = NULL;
	guint8 *code;
	guint32 code_len;
	int i;

	code = get_delegate_invoke_impl (TRUE, 0, &code_len);
	res = g_slist_prepend (res, mono_tramp_info_create (g_strdup ("delegate_invoke_impl_has_target"), code, code_len, NULL, NULL));

	for (i = 0; i <= MAX_ARCH_DELEGATE_PARAMS; ++i) {
		code = get_delegate_invoke_impl (FALSE, i, &code_len);
		res = g_slist_prepend (res, mono_tramp_info_create (g_strdup_printf ("delegate_invoke_impl_target_%d", i), code, code_len, NULL, NULL));
	}

	return res;
}

gpointer
mono_arch_get_delegate_invoke_impl (MonoMethodSignature *sig, gboolean has_target)
{
	guint8 *code, *start;

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

		if (mono_aot_only)
			start = mono_aot_get_trampoline ("delegate_invoke_impl_has_target");
		else
			start = get_delegate_invoke_impl (TRUE, 0, NULL);
		cached = start;
		mono_mini_arch_unlock ();
		return cached;
	} else {
		static guint8* cache [MAX_ARCH_DELEGATE_PARAMS + 1] = {NULL};
		int i;

		if (sig->param_count > MAX_ARCH_DELEGATE_PARAMS)
			return NULL;
		for (i = 0; i < sig->param_count; ++i)
			if (!mono_is_regsize_var (sig->params [i]))
				return NULL;

		mono_mini_arch_lock ();
		code = cache [sig->param_count];
		if (code) {
			mono_mini_arch_unlock ();
			return code;
		}

		if (mono_aot_only) {
			char *name = g_strdup_printf ("delegate_invoke_impl_target_%d", sig->param_count);
			start = mono_aot_get_trampoline (name);
			g_free (name);
		} else {
			start = get_delegate_invoke_impl (FALSE, sig->param_count, NULL);
		}
		cache [sig->param_count] = start;
		mono_mini_arch_unlock ();
		return start;
	}

	return NULL;
}

gpointer
mono_arch_get_this_arg_from_call (mgreg_t *regs, guint8 *code)
{
	return (gpointer)regs [ARMREG_R0];
}

/*
 * Initialize the cpu to execute managed code.
 */
void
mono_arch_cpu_init (void)
{
#if defined(__ARM_EABI__)
	eabi_supported = TRUE;
#endif
#if defined(__APPLE__) && defined(MONO_CROSS_COMPILE)
		i8_align = 4;
#else
		i8_align = __alignof__ (gint64);
#endif
}

static gpointer
create_function_wrapper (gpointer function)
{
	guint8 *start, *code;

	start = code = mono_global_codeman_reserve (96);

	/*
	 * Construct the MonoContext structure on the stack.
	 */

	ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, sizeof (MonoContext));

	/* save ip, lr and pc into their correspodings ctx.regs slots. */
	ARM_STR_IMM (code, ARMREG_IP, ARMREG_SP, G_STRUCT_OFFSET (MonoContext, regs) + sizeof (mgreg_t) * ARMREG_IP);
	ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, G_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_LR);
	ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, G_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_PC);

	/* save r0..r10 and fp */
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_SP, G_STRUCT_OFFSET (MonoContext, regs));
	ARM_STM (code, ARMREG_IP, 0x0fff);

	/* now we can update fp. */
	ARM_MOV_REG_REG (code, ARMREG_FP, ARMREG_SP);

	/* make ctx.esp hold the actual value of sp at the beginning of this method. */
	ARM_ADD_REG_IMM8 (code, ARMREG_R0, ARMREG_FP, sizeof (MonoContext));
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_IP, 4 * ARMREG_SP);
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_FP, G_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_SP);

	/* make ctx.eip hold the address of the call. */
	ARM_SUB_REG_IMM8 (code, ARMREG_LR, ARMREG_LR, 4);
	ARM_STR_IMM (code, ARMREG_LR, ARMREG_SP, G_STRUCT_OFFSET (MonoContext, pc));

	/* r0 now points to the MonoContext */
	ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_FP);

	/* call */
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
	ARM_B (code, 0);
	*(gpointer*)code = function;
	code += 4;
	ARM_BLX_REG (code, ARMREG_IP);

	/* we're back; save ctx.eip and ctx.esp into the corresponding regs slots. */
	ARM_LDR_IMM (code, ARMREG_R0, ARMREG_FP, G_STRUCT_OFFSET (MonoContext, pc));
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_FP, G_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_LR);
	ARM_STR_IMM (code, ARMREG_R0, ARMREG_FP, G_STRUCT_OFFSET (MonoContext, regs) + 4 * ARMREG_PC);

	/* make ip point to the regs array, then restore everything, including pc. */
	ARM_ADD_REG_IMM8 (code, ARMREG_IP, ARMREG_FP, G_STRUCT_OFFSET (MonoContext, regs));
	ARM_LDM (code, ARMREG_IP, 0xffff);

	mono_arch_flush_icache (start, code - start);

	return start;
}

/*
 * Initialize architecture specific code.
 */
void
mono_arch_init (void)
{
	InitializeCriticalSection (&mini_arch_mutex);

	if (mini_get_debug_options ()->soft_breakpoints) {
		single_step_func_wrapper = create_function_wrapper (debugger_agent_single_step_from_context);
		breakpoint_func_wrapper = create_function_wrapper (debugger_agent_breakpoint_from_context);
	} else {
		ss_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ|MONO_MMAP_32BIT);
		bp_trigger_page = mono_valloc (NULL, mono_pagesize (), MONO_MMAP_READ|MONO_MMAP_32BIT);
		mono_mprotect (bp_trigger_page, mono_pagesize (), 0);
	}

	mono_aot_register_jit_icall ("mono_arm_throw_exception", mono_arm_throw_exception);
	mono_aot_register_jit_icall ("mono_arm_throw_exception_by_token", mono_arm_throw_exception_by_token);
	mono_aot_register_jit_icall ("mono_arm_resume_unwind", mono_arm_resume_unwind);

#ifdef ARM_FPU_FPA
	arm_fpu = MONO_ARM_FPU_FPA;
#elif defined(ARM_FPU_VFP_HARD)
	arm_fpu = MONO_ARM_FPU_VFP_HARD;
#elif defined(ARM_FPU_VFP)
	arm_fpu = MONO_ARM_FPU_VFP;
#else
	arm_fpu = MONO_ARM_FPU_NONE;
#endif
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
	const char *cpu_arch = getenv ("MONO_CPU_ARCH");
	if (cpu_arch != NULL) {
		thumb_supported = strstr (cpu_arch, "thumb") != NULL;
		if (strncmp (cpu_arch, "armv", 4) == 0) {
			v5_supported = cpu_arch [4] >= '5';
			v6_supported = cpu_arch [4] >= '6';
			v7_supported = cpu_arch [4] >= '7';
		}
	} else {
#if __APPLE__
	thumb_supported = TRUE;
	v5_supported = TRUE;
	darwin = TRUE;
	iphone_abi = TRUE;
#else
	char buf [512];
	char *line;
	FILE *file = fopen ("/proc/cpuinfo", "r");
	if (file) {
		while ((line = fgets (buf, 512, file))) {
			if (strncmp (line, "Processor", 9) == 0) {
				char *ver = strstr (line, "(v");
				if (ver && (ver [2] == '5' || ver [2] == '6' || ver [2] == '7'))
					v5_supported = TRUE;
				if (ver && (ver [2] == '6' || ver [2] == '7'))
					v6_supported = TRUE;
				if (ver && (ver [2] == '7'))
					v7_supported = TRUE;
				continue;
			}
			if (strncmp (line, "Features", 8) == 0) {
				char *th = strstr (line, "thumb");
				if (th) {
					thumb_supported = TRUE;
					if (v5_supported)
						break;
				}
				continue;
			}
		}
		fclose (file);
		/*printf ("features: v5: %d, thumb: %d\n", v5_supported, thumb_supported);*/
	}
#endif
	}

	/* no arm-specific optimizations yet */
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


#ifndef DISABLE_JIT

static gboolean
is_regsize_var (MonoType *t) {
	if (t->byref)
		return TRUE;
	t = mini_type_get_underlying_type (NULL, t);
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

#define USE_EXTRA_TEMPS 0

GList *
mono_arch_get_global_int_regs (MonoCompile *cfg)
{
	GList *regs = NULL;

	mono_arch_compute_omit_fp (cfg);

	/* 
	 * FIXME: Interface calls might go through a static rgctx trampoline which
	 * sets V5, but it doesn't save it, so we need to save it ourselves, and
	 * avoid using it.
	 */
	if (cfg->flags & MONO_CFG_HAS_CALLS)
		cfg->uses_rgctx_reg = TRUE;

	if (cfg->arch.omit_fp)
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_FP));
	regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V1));
	regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V2));
	regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V3));
	if (darwin)
		/* V4=R7 is used as a frame pointer, but V7=R10 is preserved */
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V7));
	else
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V4));
	if (!(cfg->compile_aot || cfg->uses_rgctx_reg || COMPILE_LLVM (cfg)))
		/* V5 is reserved for passing the vtable/rgctx/IMT method */
		regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V5));
	/*regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V6));*/
	/*regs = g_list_prepend (regs, GUINT_TO_POINTER (ARMREG_V7));*/

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

#endif /* #ifndef DISABLE_JIT */

#ifndef __GNUC_PREREQ
#define __GNUC_PREREQ(maj, min) (0)
#endif

void
mono_arch_flush_icache (guint8 *code, gint size)
{
#if __APPLE__
	sys_icache_invalidate (code, size);
#elif __GNUC_PREREQ(4, 1)
	__clear_cache (code, code + size);
#elif defined(PLATFORM_ANDROID)
	const int syscall = 0xf0002;
	__asm __volatile (
		"mov	 r0, %0\n"			
		"mov	 r1, %1\n"
		"mov	 r7, %2\n"
		"mov     r2, #0x0\n"
		"svc     0x00000000\n"
		:
		:	"r" (code), "r" (code + size), "r" (syscall)
		:	"r0", "r1", "r7", "r2"
		);
#else
	__asm __volatile ("mov r0, %0\n"
			"mov r1, %1\n"
			"mov r2, %2\n"
			"swi 0x9f0002       @ sys_cacheflush"
			: /* no outputs */
			: "r" (code), "r" (code + size), "r" (0)
			: "r0", "r1", "r3" );
#endif
}

typedef enum {
	RegTypeNone,
	RegTypeGeneral,
	RegTypeIRegPair,
	RegTypeBase,
	RegTypeBaseGen,
	RegTypeFP,
	RegTypeStructByVal,
	RegTypeStructByAddr
} ArgStorage;

typedef struct {
	gint32  offset;
	guint16 vtsize; /* in param area */
	guint8  reg;
	ArgStorage  storage;
	gint32  struct_size;
	guint8  size    : 4; /* 1, 2, 4, 8, or regs used by RegTypeStructByVal */
} ArgInfo;

typedef struct {
	int nargs;
	guint32 stack_usage;
	gboolean vtype_retaddr;
	/* The index of the vret arg in the argument list */
	int vret_arg_index;
	ArgInfo ret;
	ArgInfo sig_cookie;
	ArgInfo args [1];
} CallInfo;

#define DEBUG(a)

#ifndef __GNUC__
/*#define __alignof__(a) sizeof(a)*/
#define __alignof__(type) G_STRUCT_OFFSET(struct { char c; type x; }, x)
#endif

#define PARAM_REGS 4

static void inline
add_general (guint *gr, guint *stack_size, ArgInfo *ainfo, gboolean simple)
{
	if (simple) {
		if (*gr > ARMREG_R3) {
			ainfo->offset = *stack_size;
			ainfo->reg = ARMREG_SP; /* in the caller */
			ainfo->storage = RegTypeBase;
			*stack_size += 4;
		} else {
			ainfo->storage = RegTypeGeneral;
			ainfo->reg = *gr;
		}
	} else {
		gboolean split;

		if (eabi_supported)
			split = i8_align == 4;
		else
			split = TRUE;
		
		if (*gr == ARMREG_R3 && split) {
			/* first word in r3 and the second on the stack */
			ainfo->offset = *stack_size;
			ainfo->reg = ARMREG_SP; /* in the caller */
			ainfo->storage = RegTypeBaseGen;
			*stack_size += 4;
		} else if (*gr >= ARMREG_R3) {
			if (eabi_supported) {
				/* darwin aligns longs to 4 byte only */
				if (i8_align == 8) {
					*stack_size += 7;
					*stack_size &= ~7;
				}
			}
			ainfo->offset = *stack_size;
			ainfo->reg = ARMREG_SP; /* in the caller */
			ainfo->storage = RegTypeBase;
			*stack_size += 8;
		} else {
			if (eabi_supported) {
				if (i8_align == 8 && ((*gr) & 1))
					(*gr) ++;
			}
			ainfo->storage = RegTypeIRegPair;
			ainfo->reg = *gr;
		}
		(*gr) ++;
	}
	(*gr) ++;
}

static CallInfo*
get_call_info (MonoGenericSharingContext *gsctx, MonoMemPool *mp, MonoMethodSignature *sig)
{
	guint i, gr, pstart;
	int n = sig->hasthis + sig->param_count;
	MonoType *simpletype;
	guint32 stack_size = 0;
	CallInfo *cinfo;
	gboolean is_pinvoke = sig->pinvoke;

	if (mp)
		cinfo = mono_mempool_alloc0 (mp, sizeof (CallInfo) + (sizeof (ArgInfo) * n));
	else
		cinfo = g_malloc0 (sizeof (CallInfo) + (sizeof (ArgInfo) * n));

	cinfo->nargs = n;
	gr = ARMREG_R0;

	/* FIXME: handle returning a struct */
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		guint32 align;

		if (is_pinvoke && mono_class_native_size (mono_class_from_mono_type (sig->ret), &align) <= sizeof (gpointer)) {
			cinfo->ret.storage = RegTypeStructByVal;
		} else {
			cinfo->vtype_retaddr = TRUE;
		}
	}

	pstart = 0;
	n = 0;
	/*
	 * To simplify get_this_arg_reg () and LLVM integration, emit the vret arg after
	 * the first argument, allowing 'this' to be always passed in the first arg reg.
	 * Also do this if the first argument is a reference type, since virtual calls
	 * are sometimes made using calli without sig->hasthis set, like in the delegate
	 * invoke wrappers.
	 */
	if (cinfo->vtype_retaddr && !is_pinvoke && (sig->hasthis || (sig->param_count > 0 && MONO_TYPE_IS_REFERENCE (mini_type_get_underlying_type (gsctx, sig->params [0]))))) {
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0, TRUE);
		} else {
			add_general (&gr, &stack_size, &cinfo->args [sig->hasthis + 0], TRUE);
			pstart = 1;
		}
		n ++;
		add_general (&gr, &stack_size, &cinfo->ret, TRUE);
		cinfo->vret_arg_index = 1;
	} else {
		/* this */
		if (sig->hasthis) {
			add_general (&gr, &stack_size, cinfo->args + 0, TRUE);
			n ++;
		}

		if (cinfo->vtype_retaddr)
			add_general (&gr, &stack_size, &cinfo->ret, TRUE);
	}

	DEBUG(printf("params: %d\n", sig->param_count));
	for (i = pstart; i < sig->param_count; ++i) {
		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Prevent implicit arguments and sig_cookie from
			   being passed in registers */
			gr = ARMREG_R3 + 1;
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
		simpletype = mini_type_get_underlying_type (NULL, sig->params [i]);
		switch (simpletype->type) {
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
		case MONO_TYPE_R4:
			cinfo->args [n].size = sizeof (gpointer);
			add_general (&gr, &stack_size, cinfo->args + n, TRUE);
			n++;
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (simpletype)) {
				cinfo->args [n].size = sizeof (gpointer);
				add_general (&gr, &stack_size, cinfo->args + n, TRUE);
				n++;
				break;
			}
			/* Fall through */
		case MONO_TYPE_TYPEDBYREF:
		case MONO_TYPE_VALUETYPE: {
			gint size;
			int align_size;
			int nwords;
			guint32 align;

			if (simpletype->type == MONO_TYPE_TYPEDBYREF) {
				size = sizeof (MonoTypedRef);
				align = sizeof (gpointer);
			} else {
				MonoClass *klass = mono_class_from_mono_type (sig->params [i]);
				if (is_pinvoke)
					size = mono_class_native_size (klass, &align);
				else
					size = mono_class_value_size (klass, &align);
			}
			DEBUG(printf ("load %d bytes struct\n",
				      mono_class_native_size (sig->params [i]->data.klass, NULL)));
			align_size = size;
			nwords = 0;
			align_size += (sizeof (gpointer) - 1);
			align_size &= ~(sizeof (gpointer) - 1);
			nwords = (align_size + sizeof (gpointer) -1 ) / sizeof (gpointer);
			cinfo->args [n].storage = RegTypeStructByVal;
			cinfo->args [n].struct_size = size;
			/* FIXME: align stack_size if needed */
			if (eabi_supported) {
				if (align >= 8 && (gr & 1))
					gr ++;
			}
			if (gr > ARMREG_R3) {
				cinfo->args [n].size = 0;
				cinfo->args [n].vtsize = nwords;
			} else {
				int rest = ARMREG_R3 - gr + 1;
				int n_in_regs = rest >= nwords? nwords: rest;

				cinfo->args [n].size = n_in_regs;
				cinfo->args [n].vtsize = nwords - n_in_regs;
				cinfo->args [n].reg = gr;
				gr += n_in_regs;
				nwords -= n_in_regs;
			}
			cinfo->args [n].offset = stack_size;
			/*g_print ("offset for arg %d at %d\n", n, stack_size);*/
			stack_size += nwords * sizeof (gpointer);
			n++;
			break;
		}
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
		case MONO_TYPE_R8:
			cinfo->args [n].size = 8;
			add_general (&gr, &stack_size, cinfo->args + n, FALSE);
			n++;
			break;
		default:
			g_error ("Can't trampoline 0x%x", sig->params [i]->type);
		}
	}

	/* Handle the case where there are no implicit arguments */
	if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
		/* Prevent implicit arguments and sig_cookie from
		   being passed in registers */
		gr = ARMREG_R3 + 1;
		/* Emit the signature cookie just before the implicit arguments */
		add_general (&gr, &stack_size, &cinfo->sig_cookie, TRUE);
	}

	{
		simpletype = mini_type_get_underlying_type (NULL, sig->ret);
		switch (simpletype->type) {
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
			cinfo->ret.storage = RegTypeGeneral;
			cinfo->ret.reg = ARMREG_R0;
			break;
		case MONO_TYPE_U8:
		case MONO_TYPE_I8:
			cinfo->ret.storage = RegTypeIRegPair;
			cinfo->ret.reg = ARMREG_R0;
			break;
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			cinfo->ret.storage = RegTypeFP;
			cinfo->ret.reg = ARMREG_R0;
			/* FIXME: cinfo->ret.reg = ???;
			cinfo->ret.storage = RegTypeFP;*/
			break;
		case MONO_TYPE_GENERICINST:
			if (!mono_type_generic_inst_is_valuetype (simpletype)) {
				cinfo->ret.storage = RegTypeGeneral;
				cinfo->ret.reg = ARMREG_R0;
				break;
			}
			/* Fall through */
		case MONO_TYPE_VALUETYPE:
		case MONO_TYPE_TYPEDBYREF:
			if (cinfo->ret.storage != RegTypeStructByVal)
				cinfo->ret.storage = RegTypeStructByAddr;
			break;
		case MONO_TYPE_VOID:
			break;
		default:
			g_error ("Can't handle as return value 0x%x", sig->ret->type);
		}
	}

	/* align stack size to 8 */
	DEBUG (printf ("      stack size: %d (%d)\n", (stack_size + 15) & ~15, stack_size));
	stack_size = (stack_size + 7) & ~7;

	cinfo->stack_usage = stack_size;
	return cinfo;
}

#ifndef DISABLE_JIT

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

	header = cfg->header;

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);
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

		if (ainfo->storage == RegTypeBase || ainfo->storage == RegTypeBaseGen || ainfo->storage == RegTypeStructByVal) {
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
}

/*
 * Set var information according to the calling convention. arm version.
 * The locals var stuff should most likely be split in another method.
 */
void
mono_arch_allocate_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	MonoMethodHeader *header;
	MonoInst *ins;
	int i, offset, size, align, curinst;
	CallInfo *cinfo;
	guint32 ualign;

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	mono_arch_compute_omit_fp (cfg);

	if (cfg->arch.omit_fp)
		cfg->frame_reg = ARMREG_SP;
	else
		cfg->frame_reg = ARMREG_FP;

	cfg->flags |= MONO_CFG_HAS_SPILLUP;

	/* allow room for the vararg method args: void* and long/double */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method))
		cfg->param_area = MAX (cfg->param_area, sizeof (gpointer)*8);

	header = cfg->header;

	/* See mono_arch_get_global_int_regs () */
	if (cfg->flags & MONO_CFG_HAS_CALLS)
		cfg->uses_rgctx_reg = TRUE;

	if (cfg->frame_reg != ARMREG_SP)
		cfg->used_int_regs |= 1 << cfg->frame_reg;

	if (cfg->compile_aot || cfg->uses_rgctx_reg || COMPILE_LLVM (cfg))
		/* V5 is reserved for passing the vtable/rgctx/IMT method */
		cfg->used_int_regs |= (1 << ARMREG_V5);

	offset = 0;
	curinst = 0;
	if (!MONO_TYPE_ISSTRUCT (sig->ret)) {
		switch (mini_type_get_underlying_type (NULL, sig->ret)->type) {
		case MONO_TYPE_VOID:
			break;
		default:
			cfg->ret->opcode = OP_REGVAR;
			cfg->ret->inst_c0 = ARMREG_R0;
			break;
		}
	}
	/* local vars are at a positive offset from the stack pointer */
	/* 
	 * also note that if the function uses alloca, we use FP
	 * to point at the local variables.
	 */
	offset = 0; /* linkage area */
	/* align the offset to 16 bytes: not sure this is needed here  */
	//offset += 8 - 1;
	//offset &= ~(8 - 1);

	/* add parameter area size for called functions */
	offset += cfg->param_area;
	offset += 8 - 1;
	offset &= ~(8 - 1);
	if (cfg->flags & MONO_CFG_HAS_FPOUT)
		offset += 8;

	/* allow room to save the return value */
	if (mono_jit_trace_calls != NULL && mono_trace_eval (cfg->method))
		offset += 8;

	/* the MonoLMF structure is stored just below the stack pointer */
	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		if (cinfo->ret.storage == RegTypeStructByVal) {
			cfg->ret->opcode = OP_REGOFFSET;
			cfg->ret->inst_basereg = cfg->frame_reg;
			offset += sizeof (gpointer) - 1;
			offset &= ~(sizeof (gpointer) - 1);
			cfg->ret->inst_offset = - offset;
		} else {
			ins = cfg->vret_addr;
			offset += sizeof(gpointer) - 1;
			offset &= ~(sizeof(gpointer) - 1);
			ins->inst_offset = offset;
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			if (G_UNLIKELY (cfg->verbose_level > 1)) {
				printf ("vret_addr =");
				mono_print_ins (cfg->vret_addr);
			}
		}
		offset += sizeof(gpointer);
	}

	/* Allocate these first so they have a small offset, OP_SEQ_POINT depends on this */
	if (cfg->arch.seq_point_info_var) {
		MonoInst *ins;

		ins = cfg->arch.seq_point_info_var;

		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;

		ins = cfg->arch.ss_trigger_page_var;
		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}

	if (cfg->arch.seq_point_read_var) {
		MonoInst *ins;

		ins = cfg->arch.seq_point_read_var;

		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;

		ins = cfg->arch.seq_point_ss_method_var;
		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;

		ins = cfg->arch.seq_point_bp_method_var;
		size = 4;
		align = 4;
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_basereg = cfg->frame_reg;
		ins->inst_offset = offset;
		offset += size;
	}

	cfg->locals_min_stack_offset = offset;

	curinst = cfg->locals_start;
	for (i = curinst; i < cfg->num_varinfo; ++i) {
		ins = cfg->varinfo [i];
		if ((ins->flags & MONO_INST_IS_DEAD) || ins->opcode == OP_REGVAR || ins->opcode == OP_REGOFFSET)
			continue;

		/* inst->backend.is_pinvoke indicates native sized value types, this is used by the
		* pinvoke wrappers when they call functions returning structure */
		if (ins->backend.is_pinvoke && MONO_TYPE_ISSTRUCT (ins->inst_vtype) && ins->inst_vtype->type != MONO_TYPE_TYPEDBYREF) {
			size = mono_class_native_size (mono_class_from_mono_type (ins->inst_vtype), &ualign);
			align = ualign;
		}
		else
			size = mono_type_size (ins->inst_vtype, &align);

		/* FIXME: if a structure is misaligned, our memcpy doesn't work,
		 * since it loads/stores misaligned words, which don't do the right thing.
		 */
		if (align < 4 && size >= 4)
			align = 4;
		if (ALIGN_TO (offset, align) > ALIGN_TO (offset, 4))
			mini_gc_set_slot_type_from_fp (cfg, ALIGN_TO (offset, 4), SLOT_NOREF);
		offset += align - 1;
		offset &= ~(align - 1);
		ins->opcode = OP_REGOFFSET;
		ins->inst_offset = offset;
		ins->inst_basereg = cfg->frame_reg;
		offset += size;
		//g_print ("allocating local %d to %d\n", i, inst->inst_offset);
	}

	cfg->locals_max_stack_offset = offset;

	curinst = 0;
	if (sig->hasthis) {
		ins = cfg->args [curinst];
		if (ins->opcode != OP_REGVAR) {
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			offset += sizeof (gpointer) - 1;
			offset &= ~(sizeof (gpointer) - 1);
			ins->inst_offset = offset;
			offset += sizeof (gpointer);
		}
		curinst++;
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		size = 4;
		align = 4;

		/* Allocate a local slot to hold the sig cookie address */
		offset += align - 1;
		offset &= ~(align - 1);
		cfg->sig_cookie = offset;
		offset += size;
	}			

	for (i = 0; i < sig->param_count; ++i) {
		ins = cfg->args [curinst];

		if (ins->opcode != OP_REGVAR) {
			ins->opcode = OP_REGOFFSET;
			ins->inst_basereg = cfg->frame_reg;
			size = mini_type_stack_size_full (NULL, sig->params [i], &ualign, sig->pinvoke);
			align = ualign;
			/* FIXME: if a structure is misaligned, our memcpy doesn't work,
			 * since it loads/stores misaligned words, which don't do the right thing.
			 */
			if (align < 4 && size >= 4)
				align = 4;
			/* The code in the prolog () stores words when storing vtypes received in a register */
			if (MONO_TYPE_ISSTRUCT (sig->params [i]))
				align = 4;
			if (ALIGN_TO (offset, align) > ALIGN_TO (offset, 4))
				mini_gc_set_slot_type_from_fp (cfg, ALIGN_TO (offset, 4), SLOT_NOREF);
			offset += align - 1;
			offset &= ~(align - 1);
			ins->inst_offset = offset;
			offset += size;
		}
		curinst++;
	}

	/* align the offset to 8 bytes */
	if (ALIGN_TO (offset, 8) > ALIGN_TO (offset, 4))
		mini_gc_set_slot_type_from_fp (cfg, ALIGN_TO (offset, 4), SLOT_NOREF);
	offset += 8 - 1;
	offset &= ~(8 - 1);

	/* change sign? */
	cfg->stack_offset = offset;
}

void
mono_arch_create_vars (MonoCompile *cfg)
{
	MonoMethodSignature *sig;
	CallInfo *cinfo;

	sig = mono_method_signature (cfg->method);

	if (!cfg->arch.cinfo)
		cfg->arch.cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);
	cinfo = cfg->arch.cinfo;

	if (cinfo->ret.storage == RegTypeStructByVal)
		cfg->ret_var_is_local = TRUE;

	if (MONO_TYPE_ISSTRUCT (sig->ret) && cinfo->ret.storage != RegTypeStructByVal) {
		cfg->vret_addr = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_ARG);
		if (G_UNLIKELY (cfg->verbose_level > 1)) {
			printf ("vret_addr = ");
			mono_print_ins (cfg->vret_addr);
		}
	}

	if (cfg->gen_seq_points) {
		if (cfg->soft_breakpoints) {
			MonoInst *ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_read_var = ins;

			ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_ss_method_var = ins;

			ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_bp_method_var = ins;

			g_assert (!cfg->compile_aot);
		} else if (cfg->compile_aot) {
			MonoInst *ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.seq_point_info_var = ins;

			/* Allocate a separate variable for this to save 1 load per seq point */
			ins = mono_compile_create_var (cfg, &mono_defaults.int_class->byval_arg, OP_LOCAL);
			ins->flags |= MONO_INST_VOLATILE;
			cfg->arch.ss_trigger_page_var = ins;
		}
	}
}

static void
emit_sig_cookie (MonoCompile *cfg, MonoCallInst *call, CallInfo *cinfo)
{
	MonoMethodSignature *tmp_sig;
	int sig_reg;

	if (call->tail_call)
		NOT_IMPLEMENTED;

	g_assert (cinfo->sig_cookie.storage == RegTypeBase);
			
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

	sig_reg = mono_alloc_ireg (cfg);
	MONO_EMIT_NEW_SIGNATURECONST (cfg, sig_reg, tmp_sig);

	MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, cinfo->sig_cookie.offset, sig_reg);
}

#ifdef ENABLE_LLVM
LLVMCallInfo*
mono_arch_get_llvm_call_info (MonoCompile *cfg, MonoMethodSignature *sig)
{
	int i, n;
	CallInfo *cinfo;
	ArgInfo *ainfo;
	LLVMCallInfo *linfo;

	n = sig->param_count + sig->hasthis;

	cinfo = get_call_info (cfg->generic_sharing_context, cfg->mempool, sig);

	linfo = mono_mempool_alloc0 (cfg->mempool, sizeof (LLVMCallInfo) + (sizeof (LLVMArgInfo) * n));

	/*
	 * LLVM always uses the native ABI while we use our own ABI, the
	 * only difference is the handling of vtypes:
	 * - we only pass/receive them in registers in some cases, and only 
	 *   in 1 or 2 integer registers.
	 */
	if (cinfo->vtype_retaddr) {
		/* Vtype returned using a hidden argument */
		linfo->ret.storage = LLVMArgVtypeRetAddr;
		linfo->vret_arg_index = cinfo->vret_arg_index;
	} else if (cinfo->ret.storage != RegTypeGeneral && cinfo->ret.storage != RegTypeNone && cinfo->ret.storage != RegTypeFP && cinfo->ret.storage != RegTypeIRegPair) {
		cfg->exception_message = g_strdup ("unknown ret conv");
		cfg->disable_llvm = TRUE;
		return linfo;
	}

	for (i = 0; i < n; ++i) {
		ainfo = cinfo->args + i;

		linfo->args [i].storage = LLVMArgNone;

		switch (ainfo->storage) {
		case RegTypeGeneral:
		case RegTypeIRegPair:
		case RegTypeBase:
			linfo->args [i].storage = LLVMArgInIReg;
			break;
		case RegTypeStructByVal:
			// FIXME: Passing entirely on the stack or split reg/stack
			if (ainfo->vtsize == 0 && ainfo->size <= 2) {
				linfo->args [i].storage = LLVMArgVtypeInReg;
				linfo->args [i].pair_storage [0] = LLVMArgInIReg;
				if (ainfo->size == 2)
					linfo->args [i].pair_storage [1] = LLVMArgInIReg;
				else
					linfo->args [i].pair_storage [1] = LLVMArgNone;
			} else {
				cfg->exception_message = g_strdup_printf ("vtype-by-val on stack");
				cfg->disable_llvm = TRUE;
			}
			break;
		default:
			cfg->exception_message = g_strdup_printf ("ainfo->storage (%d)", ainfo->storage);
			cfg->disable_llvm = TRUE;
			break;
		}
	}

	return linfo;
}
#endif

void
mono_arch_emit_call (MonoCompile *cfg, MonoCallInst *call)
{
	MonoInst *in, *ins;
	MonoMethodSignature *sig;
	int i, n;
	CallInfo *cinfo;

	sig = call->signature;
	n = sig->param_count + sig->hasthis;
	
	cinfo = get_call_info (cfg->generic_sharing_context, NULL, sig);

	for (i = 0; i < n; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		MonoType *t;

		if (i >= sig->hasthis)
			t = sig->params [i - sig->hasthis];
		else
			t = &mono_defaults.int_class->byval_arg;
		t = mini_type_get_underlying_type (NULL, t);

		if ((sig->call_convention == MONO_CALL_VARARG) && (i == sig->sentinelpos)) {
			/* Emit the signature cookie just before the implicit arguments */
			emit_sig_cookie (cfg, call, cinfo);
		}

		in = call->args [i];

		switch (ainfo->storage) {
		case RegTypeGeneral:
		case RegTypeIRegPair:
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = in->dreg + 1;
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);

				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = in->dreg + 2;
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg + 1, FALSE);
			} else if (!t->byref && ((t->type == MONO_TYPE_R8) || (t->type == MONO_TYPE_R4))) {
				if (ainfo->size == 4) {
					if (IS_SOFT_FLOAT) {
						/* mono_emit_call_args () have already done the r8->r4 conversion */
						/* The converted value is in an int vreg */
						MONO_INST_NEW (cfg, ins, OP_MOVE);
						ins->dreg = mono_alloc_ireg (cfg);
						ins->sreg1 = in->dreg;
						MONO_ADD_INS (cfg->cbb, ins);
						mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);
					} else {
						int creg;

						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, ARMREG_SP, (cfg->param_area - 8), in->dreg);
						creg = mono_alloc_ireg (cfg);
						MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8));
						mono_call_inst_add_outarg_reg (cfg, call, creg, ainfo->reg, FALSE);
					}
				} else {
					if (IS_SOFT_FLOAT) {
						MONO_INST_NEW (cfg, ins, OP_FGETLOW32);
						ins->dreg = mono_alloc_ireg (cfg);
						ins->sreg1 = in->dreg;
						MONO_ADD_INS (cfg->cbb, ins);
						mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);

						MONO_INST_NEW (cfg, ins, OP_FGETHIGH32);
						ins->dreg = mono_alloc_ireg (cfg);
						ins->sreg1 = in->dreg;
						MONO_ADD_INS (cfg->cbb, ins);
						mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg + 1, FALSE);
					} else {
						int creg;

						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, (cfg->param_area - 8), in->dreg);
						creg = mono_alloc_ireg (cfg);
						MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8));
						mono_call_inst_add_outarg_reg (cfg, call, creg, ainfo->reg, FALSE);
						creg = mono_alloc_ireg (cfg);
						MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8 + 4));
						mono_call_inst_add_outarg_reg (cfg, call, creg, ainfo->reg + 1, FALSE);
					}
				}
				cfg->flags |= MONO_CFG_HAS_FPOUT;
			} else {
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = in->dreg;
				MONO_ADD_INS (cfg->cbb, ins);

				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ainfo->reg, FALSE);
			}
			break;
		case RegTypeStructByAddr:
			NOT_IMPLEMENTED;
#if 0
			/* FIXME: where si the data allocated? */
			arg->backend.reg3 = ainfo->reg;
			call->used_iregs |= 1 << ainfo->reg;
			g_assert_not_reached ();
#endif
			break;
		case RegTypeStructByVal:
			MONO_INST_NEW (cfg, ins, OP_OUTARG_VT);
			ins->opcode = OP_OUTARG_VT;
			ins->sreg1 = in->dreg;
			ins->klass = in->klass;
			ins->inst_p0 = call;
			ins->inst_p1 = mono_mempool_alloc (cfg->mempool, sizeof (ArgInfo));
			memcpy (ins->inst_p1, ainfo, sizeof (ArgInfo));
			mono_call_inst_add_outarg_vt (cfg, call, ins);
			MONO_ADD_INS (cfg->cbb, ins);
			break;
		case RegTypeBase:
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI8_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
			} else if (!t->byref && ((t->type == MONO_TYPE_R4) || (t->type == MONO_TYPE_R8))) {
				if (t->type == MONO_TYPE_R8) {
					MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
				} else {
					if (IS_SOFT_FLOAT)
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STOREI4_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
					else
						MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER4_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
				}
			} else {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, in->dreg);
			}
			break;
		case RegTypeBaseGen:
			if (!t->byref && ((t->type == MONO_TYPE_I8) || (t->type == MONO_TYPE_U8))) {
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, (G_BYTE_ORDER == G_BIG_ENDIAN) ? in->dreg + 1 : in->dreg + 2);
				MONO_INST_NEW (cfg, ins, OP_MOVE);
				ins->dreg = mono_alloc_ireg (cfg);
				ins->sreg1 = G_BYTE_ORDER == G_BIG_ENDIAN ? in->dreg + 2 : in->dreg + 1;
				MONO_ADD_INS (cfg->cbb, ins);
				mono_call_inst_add_outarg_reg (cfg, call, ins->dreg, ARMREG_R3, FALSE);
			} else if (!t->byref && (t->type == MONO_TYPE_R8)) {
				int creg;

				/* This should work for soft-float as well */

				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORER8_MEMBASE_REG, ARMREG_SP, (cfg->param_area - 8), in->dreg);
				creg = mono_alloc_ireg (cfg);
				mono_call_inst_add_outarg_reg (cfg, call, creg, ARMREG_R3, FALSE);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 8));
				creg = mono_alloc_ireg (cfg);
				MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOAD_MEMBASE, creg, ARMREG_SP, (cfg->param_area - 4));
				MONO_EMIT_NEW_STORE_MEMBASE (cfg, OP_STORE_MEMBASE_REG, ARMREG_SP, ainfo->offset, creg);
				cfg->flags |= MONO_CFG_HAS_FPOUT;
			} else {
				g_assert_not_reached ();
			}
			break;
		case RegTypeFP: {
			/* FIXME: */
			NOT_IMPLEMENTED;
#if 0
			arg->backend.reg3 = ainfo->reg;
			/* FP args are passed in int regs */
			call->used_iregs |= 1 << ainfo->reg;
			if (ainfo->size == 8) {
				arg->opcode = OP_OUTARG_R8;
				call->used_iregs |= 1 << (ainfo->reg + 1);
			} else {
				arg->opcode = OP_OUTARG_R4;
			}
#endif
			cfg->flags |= MONO_CFG_HAS_FPOUT;
			break;
		}
		default:
			g_assert_not_reached ();
		}
	}

	/* Handle the case where there are no implicit arguments */
	if (!sig->pinvoke && (sig->call_convention == MONO_CALL_VARARG) && (n == sig->sentinelpos))
		emit_sig_cookie (cfg, call, cinfo);

	if (sig->ret && MONO_TYPE_ISSTRUCT (sig->ret)) {
		MonoInst *vtarg;

		if (cinfo->ret.storage == RegTypeStructByVal) {
			/* The JIT will transform this into a normal call */
			call->vret_in_reg = TRUE;
		} else {
			MONO_INST_NEW (cfg, vtarg, OP_MOVE);
			vtarg->sreg1 = call->vret_var->dreg;
			vtarg->dreg = mono_alloc_preg (cfg);
			MONO_ADD_INS (cfg->cbb, vtarg);

			mono_call_inst_add_outarg_reg (cfg, call, vtarg->dreg, cinfo->ret.reg, FALSE);
		}
	}

	call->stack_usage = cinfo->stack_usage;

	g_free (cinfo);
}

void
mono_arch_emit_outarg_vt (MonoCompile *cfg, MonoInst *ins, MonoInst *src)
{
	MonoCallInst *call = (MonoCallInst*)ins->inst_p0;
	ArgInfo *ainfo = ins->inst_p1;
	int ovf_size = ainfo->vtsize;
	int doffset = ainfo->offset;
	int struct_size = ainfo->struct_size;
	int i, soffset, dreg, tmpreg;

	soffset = 0;
	for (i = 0; i < ainfo->size; ++i) {
		dreg = mono_alloc_ireg (cfg);
		switch (struct_size) {
		case 1:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, dreg, src->dreg, soffset);
			break;
		case 2:
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU2_MEMBASE, dreg, src->dreg, soffset);
			break;
		case 3:
			tmpreg = mono_alloc_ireg (cfg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, dreg, src->dreg, soffset);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, tmpreg, src->dreg, soffset + 1);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, tmpreg, tmpreg, 8);
			MONO_EMIT_NEW_BIALU (cfg, OP_IOR, dreg, dreg, tmpreg);
			MONO_EMIT_NEW_LOAD_MEMBASE_OP (cfg, OP_LOADU1_MEMBASE, tmpreg, src->dreg, soffset + 2);
			MONO_EMIT_NEW_BIALU_IMM (cfg, OP_SHL_IMM, tmpreg, tmpreg, 16);
			MONO_EMIT_NEW_BIALU (cfg, OP_IOR, dreg, dreg, tmpreg);
			break;
		default:
			MONO_EMIT_NEW_LOAD_MEMBASE (cfg, dreg, src->dreg, soffset);
			break;
		}
		mono_call_inst_add_outarg_reg (cfg, call, dreg, ainfo->reg + i, FALSE);
		soffset += sizeof (gpointer);
		struct_size -= sizeof (gpointer);
	}
	//g_print ("vt size: %d at R%d + %d\n", doffset, vt->inst_basereg, vt->inst_offset);
	if (ovf_size != 0)
		mini_emit_memcpy (cfg, ARMREG_SP, doffset, src->dreg, soffset, MIN (ovf_size * sizeof (gpointer), struct_size), struct_size < 4 ? 1 : 4);
}

void
mono_arch_emit_setret (MonoCompile *cfg, MonoMethod *method, MonoInst *val)
{
	MonoType *ret = mini_type_get_underlying_type (cfg->generic_sharing_context, mono_method_signature (method)->ret);

	if (!ret->byref) {
		if (ret->type == MONO_TYPE_I8 || ret->type == MONO_TYPE_U8) {
			MonoInst *ins;

			if (COMPILE_LLVM (cfg)) {
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
			} else {
				MONO_INST_NEW (cfg, ins, OP_SETLRET);
				ins->sreg1 = val->dreg + 1;
				ins->sreg2 = val->dreg + 2;
				MONO_ADD_INS (cfg->cbb, ins);
			}
			return;
		}
		switch (arm_fpu) {
		case MONO_ARM_FPU_NONE:
			if (ret->type == MONO_TYPE_R8) {
				MonoInst *ins;

				MONO_INST_NEW (cfg, ins, OP_SETFRET);
				ins->dreg = cfg->ret->dreg;
				ins->sreg1 = val->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
				return;
			}
			if (ret->type == MONO_TYPE_R4) {
				/* Already converted to an int in method_to_ir () */
				MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
				return;
			}
			break;
		case MONO_ARM_FPU_VFP:
			if (ret->type == MONO_TYPE_R8 || ret->type == MONO_TYPE_R4) {
				MonoInst *ins;

				MONO_INST_NEW (cfg, ins, OP_SETFRET);
				ins->dreg = cfg->ret->dreg;
				ins->sreg1 = val->dreg;
				MONO_ADD_INS (cfg->cbb, ins);
				return;
			}
			break;
		case MONO_ARM_FPU_FPA:
			if (ret->type == MONO_TYPE_R4 || ret->type == MONO_TYPE_R8) {
				MONO_EMIT_NEW_UNALU (cfg, OP_FMOVE, cfg->ret->dreg, val->dreg);
				return;
			}
			break;
		default:
			g_assert_not_reached ();
		}
	}

	MONO_EMIT_NEW_UNALU (cfg, OP_MOVE, cfg->ret->dreg, val->dreg);
}

#endif /* #ifndef DISABLE_JIT */

gboolean 
mono_arch_is_inst_imm (gint64 imm)
{
	return TRUE;
}

#define DYN_CALL_STACK_ARGS 6

typedef struct {
	MonoMethodSignature *sig;
	CallInfo *cinfo;
} ArchDynCallInfo;

typedef struct {
	mgreg_t regs [PARAM_REGS + DYN_CALL_STACK_ARGS];
	mgreg_t res, res2;
	guint8 *ret;
} DynCallArgs;

static gboolean
dyn_call_supported (CallInfo *cinfo, MonoMethodSignature *sig)
{
	int i;

	if (sig->hasthis + sig->param_count > PARAM_REGS + DYN_CALL_STACK_ARGS)
		return FALSE;

	switch (cinfo->ret.storage) {
	case RegTypeNone:
	case RegTypeGeneral:
	case RegTypeIRegPair:
	case RegTypeStructByAddr:
		break;
	case RegTypeFP:
		if (IS_FPA)
			return FALSE;
		else if (IS_VFP)
			break;
		else
			return FALSE;
	default:
		return FALSE;
	}

	for (i = 0; i < cinfo->nargs; ++i) {
		switch (cinfo->args [i].storage) {
		case RegTypeGeneral:
			break;
		case RegTypeIRegPair:
			break;
		case RegTypeBase:
			if (cinfo->args [i].offset >= (DYN_CALL_STACK_ARGS * sizeof (gpointer)))
				return FALSE;
			break;
		case RegTypeStructByVal:
			if (cinfo->args [i].reg + cinfo->args [i].vtsize >= PARAM_REGS + DYN_CALL_STACK_ARGS)
				return FALSE;
			break;
		default:
			return FALSE;
		}
	}

	// FIXME: Can't use cinfo only as it doesn't contain info about I8/float */
	for (i = 0; i < sig->param_count; ++i) {
		MonoType *t = sig->params [i];

		if (t->byref)
			continue;

		switch (t->type) {
		case MONO_TYPE_R4:
		case MONO_TYPE_R8:
			if (IS_SOFT_FLOAT)
				return FALSE;
			else
				break;
			/*
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			return FALSE;
			*/
		default:
			break;
		}
	}

	return TRUE;
}

MonoDynCallInfo*
mono_arch_dyn_call_prepare (MonoMethodSignature *sig)
{
	ArchDynCallInfo *info;
	CallInfo *cinfo;

	cinfo = get_call_info (NULL, NULL, sig);

	if (!dyn_call_supported (cinfo, sig)) {
		g_free (cinfo);
		return NULL;
	}

	info = g_new0 (ArchDynCallInfo, 1);
	// FIXME: Preprocess the info to speed up start_dyn_call ()
	info->sig = sig;
	info->cinfo = cinfo;
	
	return (MonoDynCallInfo*)info;
}

void
mono_arch_dyn_call_free (MonoDynCallInfo *info)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;

	g_free (ainfo->cinfo);
	g_free (ainfo);
}

void
mono_arch_start_dyn_call (MonoDynCallInfo *info, gpointer **args, guint8 *ret, guint8 *buf, int buf_len)
{
	ArchDynCallInfo *dinfo = (ArchDynCallInfo*)info;
	DynCallArgs *p = (DynCallArgs*)buf;
	int arg_index, greg, i, j, pindex;
	MonoMethodSignature *sig = dinfo->sig;

	g_assert (buf_len >= sizeof (DynCallArgs));

	p->res = 0;
	p->ret = ret;

	arg_index = 0;
	greg = 0;
	pindex = 0;

	if (sig->hasthis || dinfo->cinfo->vret_arg_index == 1) {
		p->regs [greg ++] = (mgreg_t)*(args [arg_index ++]);
		if (!sig->hasthis)
			pindex = 1;
	}

	if (dinfo->cinfo->vtype_retaddr)
		p->regs [greg ++] = (mgreg_t)ret;

	for (i = pindex; i < sig->param_count; i++) {
		MonoType *t = mono_type_get_underlying_type (sig->params [i]);
		gpointer *arg = args [arg_index ++];
		ArgInfo *ainfo = &dinfo->cinfo->args [i + sig->hasthis];
		int slot = -1;

		if (ainfo->storage == RegTypeGeneral || ainfo->storage == RegTypeIRegPair || ainfo->storage == RegTypeStructByVal)
			slot = ainfo->reg;
		else if (ainfo->storage == RegTypeBase)
			slot = PARAM_REGS + (ainfo->offset / 4);
		else
			g_assert_not_reached ();

		if (t->byref) {
			p->regs [slot] = (mgreg_t)*arg;
			continue;
		}

		switch (t->type) {
		case MONO_TYPE_STRING:
		case MONO_TYPE_CLASS:  
		case MONO_TYPE_ARRAY:
		case MONO_TYPE_SZARRAY:
		case MONO_TYPE_OBJECT:
		case MONO_TYPE_PTR:
		case MONO_TYPE_I:
		case MONO_TYPE_U:
			p->regs [slot] = (mgreg_t)*arg;
			break;
		case MONO_TYPE_BOOLEAN:
		case MONO_TYPE_U1:
			p->regs [slot] = *(guint8*)arg;
			break;
		case MONO_TYPE_I1:
			p->regs [slot] = *(gint8*)arg;
			break;
		case MONO_TYPE_I2:
			p->regs [slot] = *(gint16*)arg;
			break;
		case MONO_TYPE_U2:
		case MONO_TYPE_CHAR:
			p->regs [slot] = *(guint16*)arg;
			break;
		case MONO_TYPE_I4:
			p->regs [slot] = *(gint32*)arg;
			break;
		case MONO_TYPE_U4:
			p->regs [slot] = *(guint32*)arg;
			break;
		case MONO_TYPE_I8:
		case MONO_TYPE_U8:
			p->regs [slot ++] = (mgreg_t)arg [0];
			p->regs [slot] = (mgreg_t)arg [1];
			break;
		case MONO_TYPE_R4:
			p->regs [slot] = *(mgreg_t*)arg;
			break;
		case MONO_TYPE_R8:
			p->regs [slot ++] = (mgreg_t)arg [0];
			p->regs [slot] = (mgreg_t)arg [1];
			break;
		case MONO_TYPE_GENERICINST:
			if (MONO_TYPE_IS_REFERENCE (t)) {
				p->regs [slot] = (mgreg_t)*arg;
				break;
			} else {
				/* Fall though */
			}
		case MONO_TYPE_VALUETYPE:
			g_assert (ainfo->storage == RegTypeStructByVal);

			if (ainfo->size == 0)
				slot = PARAM_REGS + (ainfo->offset / 4);
			else
				slot = ainfo->reg;

			for (j = 0; j < ainfo->size + ainfo->vtsize; ++j)
				p->regs [slot ++] = ((mgreg_t*)arg) [j];
			break;
		default:
			g_assert_not_reached ();
		}
	}
}

void
mono_arch_finish_dyn_call (MonoDynCallInfo *info, guint8 *buf)
{
	ArchDynCallInfo *ainfo = (ArchDynCallInfo*)info;
	MonoMethodSignature *sig = ((ArchDynCallInfo*)info)->sig;
	guint8 *ret = ((DynCallArgs*)buf)->ret;
	mgreg_t res = ((DynCallArgs*)buf)->res;
	mgreg_t res2 = ((DynCallArgs*)buf)->res2;

	switch (mono_type_get_underlying_type (sig->ret)->type) {
	case MONO_TYPE_VOID:
		*(gpointer*)ret = NULL;
		break;
	case MONO_TYPE_STRING:
	case MONO_TYPE_CLASS:  
	case MONO_TYPE_ARRAY:
	case MONO_TYPE_SZARRAY:
	case MONO_TYPE_OBJECT:
	case MONO_TYPE_I:
	case MONO_TYPE_U:
	case MONO_TYPE_PTR:
		*(gpointer*)ret = (gpointer)res;
		break;
	case MONO_TYPE_I1:
		*(gint8*)ret = res;
		break;
	case MONO_TYPE_U1:
	case MONO_TYPE_BOOLEAN:
		*(guint8*)ret = res;
		break;
	case MONO_TYPE_I2:
		*(gint16*)ret = res;
		break;
	case MONO_TYPE_U2:
	case MONO_TYPE_CHAR:
		*(guint16*)ret = res;
		break;
	case MONO_TYPE_I4:
		*(gint32*)ret = res;
		break;
	case MONO_TYPE_U4:
		*(guint32*)ret = res;
		break;
	case MONO_TYPE_I8:
	case MONO_TYPE_U8:
		/* This handles endianness as well */
		((gint32*)ret) [0] = res;
		((gint32*)ret) [1] = res2;
		break;
	case MONO_TYPE_GENERICINST:
		if (MONO_TYPE_IS_REFERENCE (sig->ret)) {
			*(gpointer*)ret = (gpointer)res;
			break;
		} else {
			/* Fall though */
		}
	case MONO_TYPE_VALUETYPE:
		g_assert (ainfo->cinfo->vtype_retaddr);
		/* Nothing to do */
		break;
	case MONO_TYPE_R4:
		g_assert (IS_VFP);
		*(float*)ret = *(float*)&res;
		break;
	case MONO_TYPE_R8: {
		mgreg_t regs [2];

		g_assert (IS_VFP);
		regs [0] = res;
		regs [1] = res2;

		*(double*)ret = *(double*)&regs;
		break;
	}
	default:
		g_assert_not_reached ();
	}
}

#ifndef DISABLE_JIT

/*
 * Allow tracing to work with this interface (with an optional argument)
 */

void*
mono_arch_instrument_prolog (MonoCompile *cfg, void *func, void *p, gboolean enable_arguments)
{
	guchar *code = p;

	code = mono_arm_emit_load_imm (code, ARMREG_R0, (guint32)cfg->method);
	ARM_MOV_REG_IMM8 (code, ARMREG_R1, 0); /* NULL ebp for now */
	code = mono_arm_emit_load_imm (code, ARMREG_R2, (guint32)func);
	code = emit_call_reg (code, ARMREG_R2);
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
	guchar *code = p;
	int save_mode = SAVE_NONE;
	int offset;
	MonoMethod *method = cfg->method;
	int rtype = mini_type_get_underlying_type (cfg->generic_sharing_context, mono_method_signature (method)->ret)->type;
	int save_offset = cfg->param_area;
	save_offset += 7;
	save_offset &= ~7;
	
	offset = code - cfg->native_code;
	/* we need about 16 instructions */
	if (offset > (cfg->code_size - 16 * 4)) {
		cfg->code_size *= 2;
		cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
		code = cfg->native_code + offset;
	}
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
		ARM_STR_IMM (code, ARMREG_R0, cfg->frame_reg, save_offset);
		ARM_STR_IMM (code, ARMREG_R1, cfg->frame_reg, save_offset + 4);
		if (enable_arguments) {
			ARM_MOV_REG_REG (code, ARMREG_R2, ARMREG_R1);
			ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_R0);
		}
		break;
	case SAVE_ONE:
		ARM_STR_IMM (code, ARMREG_R0, cfg->frame_reg, save_offset);
		if (enable_arguments) {
			ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_R0);
		}
		break;
	case SAVE_FP:
		/* FIXME: what reg?  */
		if (enable_arguments) {
			/* FIXME: what reg?  */
		}
		break;
	case SAVE_STRUCT:
		if (enable_arguments) {
			/* FIXME: get the actual address  */
			ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_R0);
		}
		break;
	case SAVE_NONE:
	default:
		break;
	}

	code = mono_arm_emit_load_imm (code, ARMREG_R0, (guint32)cfg->method);
	code = mono_arm_emit_load_imm (code, ARMREG_IP, (guint32)func);
	code = emit_call_reg (code, ARMREG_IP);

	switch (save_mode) {
	case SAVE_TWO:
		ARM_LDR_IMM (code, ARMREG_R0, cfg->frame_reg, save_offset);
		ARM_LDR_IMM (code, ARMREG_R1, cfg->frame_reg, save_offset + 4);
		break;
	case SAVE_ONE:
		ARM_LDR_IMM (code, ARMREG_R0, cfg->frame_reg, save_offset);
		break;
	case SAVE_FP:
		/* FIXME */
		break;
	case SAVE_NONE:
	default:
		break;
	}

	return code;
}

/*
 * The immediate field for cond branches is big enough for all reasonable methods
 */
#define EMIT_COND_BRANCH_FLAGS(ins,condcode) \
if (0 && ins->inst_true_bb->native_offset) { \
	ARM_B_COND (code, (condcode), (code - cfg->native_code + ins->inst_true_bb->native_offset) & 0xffffff); \
} else { \
	mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_true_bb); \
	ARM_B_COND (code, (condcode), 0);	\
}

#define EMIT_COND_BRANCH(ins,cond) EMIT_COND_BRANCH_FLAGS(ins, branch_cc_table [(cond)])

/* emit an exception if condition is fail
 *
 * We assign the extra code used to throw the implicit exceptions
 * to cfg->bb_exit as far as the big branch handling is concerned
 */
#define EMIT_COND_SYSTEM_EXCEPTION_FLAGS(condcode,exc_name)            \
        do {                                                        \
		mono_add_patch_info (cfg, code - cfg->native_code,   \
				    MONO_PATCH_INFO_EXC, exc_name);  \
		ARM_BL_COND (code, (condcode), 0);	\
	} while (0); 

#define EMIT_COND_SYSTEM_EXCEPTION(cond,exc_name) EMIT_COND_SYSTEM_EXCEPTION_FLAGS(branch_cc_table [(cond)], (exc_name))

void
mono_arch_peephole_pass_1 (MonoCompile *cfg, MonoBasicBlock *bb)
{
}

void
mono_arch_peephole_pass_2 (MonoCompile *cfg, MonoBasicBlock *bb)
{
	MonoInst *ins, *n, *last_ins = NULL;

	MONO_BB_FOR_EACH_INS_SAFE (bb, n, ins) {
		switch (ins->opcode) {
		case OP_MUL_IMM: 
		case OP_IMUL_IMM: 
			/* Already done by an arch-independent pass */
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
		case OP_LOADU1_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI1_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI1_MEMBASE) ? OP_ICONV_TO_I1 : OP_ICONV_TO_U1;
				ins->sreg1 = last_ins->sreg1;				
			}
			break;
		case OP_LOADU2_MEMBASE:
		case OP_LOADI2_MEMBASE:
			if (last_ins && (last_ins->opcode == OP_STOREI2_MEMBASE_REG) &&
					ins->inst_basereg == last_ins->inst_destbasereg &&
					ins->inst_offset == last_ins->inst_offset) {
				ins->opcode = (ins->opcode == OP_LOADI2_MEMBASE) ? OP_ICONV_TO_I2 : OP_ICONV_TO_U2;
				ins->sreg1 = last_ins->sreg1;				
			}
			break;
		case OP_MOVE:
			ins->opcode = OP_MOVE;
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

/* 
 * the branch_cc_table should maintain the order of these
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
branch_cc_table [] = {
	ARMCOND_EQ, 
	ARMCOND_GE, 
	ARMCOND_GT, 
	ARMCOND_LE,
	ARMCOND_LT, 
	
	ARMCOND_NE, 
	ARMCOND_HS, 
	ARMCOND_HI, 
	ARMCOND_LS,
	ARMCOND_LO
};

#define ADD_NEW_INS(cfg,dest,op) do {       \
		MONO_INST_NEW ((cfg), (dest), (op)); \
        mono_bblock_insert_before_ins (bb, ins, (dest)); \
	} while (0)

static int
map_to_reg_reg_op (int op)
{
	switch (op) {
	case OP_ADD_IMM:
		return OP_IADD;
	case OP_SUB_IMM:
		return OP_ISUB;
	case OP_AND_IMM:
		return OP_IAND;
	case OP_COMPARE_IMM:
		return OP_COMPARE;
	case OP_ICOMPARE_IMM:
		return OP_ICOMPARE;
	case OP_ADDCC_IMM:
		return OP_ADDCC;
	case OP_ADC_IMM:
		return OP_ADC;
	case OP_SUBCC_IMM:
		return OP_SUBCC;
	case OP_SBB_IMM:
		return OP_SBB;
	case OP_OR_IMM:
		return OP_IOR;
	case OP_XOR_IMM:
		return OP_IXOR;
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
	MonoInst *ins, *temp, *last_ins = NULL;
	int rot_amount, imm8, low_imm;

	MONO_BB_FOR_EACH_INS (bb, ins) {
loop_start:
		switch (ins->opcode) {
		case OP_ADD_IMM:
		case OP_SUB_IMM:
		case OP_AND_IMM:
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
		case OP_ADDCC_IMM:
		case OP_ADC_IMM:
		case OP_SUBCC_IMM:
		case OP_SBB_IMM:
		case OP_OR_IMM:
		case OP_XOR_IMM:
		case OP_IADD_IMM:
		case OP_ISUB_IMM:
		case OP_IAND_IMM:
		case OP_IADC_IMM:
		case OP_ISBB_IMM:
		case OP_IOR_IMM:
		case OP_IXOR_IMM:
			if ((imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount)) < 0) {
				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_imm;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->sreg2 = temp->dreg;
				ins->opcode = mono_op_imm_to_op (ins->opcode);
			}
			if (ins->opcode == OP_SBB || ins->opcode == OP_ISBB || ins->opcode == OP_SUBCC)
				goto loop_start;
			else
				break;
		case OP_MUL_IMM:
		case OP_IMUL_IMM:
			if (ins->inst_imm == 1) {
				ins->opcode = OP_MOVE;
				break;
			}
			if (ins->inst_imm == 0) {
				ins->opcode = OP_ICONST;
				ins->inst_c0 = 0;
				break;
			}
			imm8 = mono_is_power_of_two (ins->inst_imm);
			if (imm8 > 0) {
				ins->opcode = OP_SHL_IMM;
				ins->inst_imm = imm8;
				break;
			}
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = OP_IMUL;
			break;
		case OP_SBB:
		case OP_ISBB:
		case OP_SUBCC:
		case OP_ISUBCC:
			if (ins->next  && (ins->next->opcode == OP_COND_EXC_C || ins->next->opcode == OP_COND_EXC_IC))
				/* ARM sets the C flag to 1 if there was _no_ overflow */
				ins->next->opcode = OP_COND_EXC_NC;
			break;
		case OP_LOCALLOC_IMM:
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = temp->dreg;
			ins->opcode = OP_LOCALLOC;
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
		case OP_LOADU1_MEMBASE:
			/* we can do two things: load the immed in a register
			 * and use an indexed load, or see if the immed can be
			 * represented as an ad_imm + a load with a smaller offset
			 * that fits. We just do the first for now, optimize later.
			 */
			if (arm_is_imm12 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_LOADI2_MEMBASE:
		case OP_LOADU2_MEMBASE:
		case OP_LOADI1_MEMBASE:
			if (arm_is_imm8 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_LOADR4_MEMBASE:
		case OP_LOADR8_MEMBASE:
			if (arm_is_fpimm8 (ins->inst_offset))
				break;
			low_imm = ins->inst_offset & 0x1ff;
			if ((imm8 = mono_arm_is_rotated_imm8 (ins->inst_offset & ~0x1ff, &rot_amount)) >= 0) {
				ADD_NEW_INS (cfg, temp, OP_ADD_IMM);
				temp->inst_imm = ins->inst_offset & ~0x1ff;
				temp->sreg1 = ins->inst_basereg;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->inst_basereg = temp->dreg;
				ins->inst_offset = low_imm;
			} else {
				MonoInst *add_ins;

				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);

				ADD_NEW_INS (cfg, add_ins, OP_IADD);
				add_ins->sreg1 = ins->inst_basereg;
				add_ins->sreg2 = temp->dreg;
				add_ins->dreg = mono_alloc_ireg (cfg);

				ins->inst_basereg = add_ins->dreg;
				ins->inst_offset = 0;
			}
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
		case OP_STOREI1_MEMBASE_REG:
			if (arm_is_imm12 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_STOREI2_MEMBASE_REG:
			if (arm_is_imm8 (ins->inst_offset))
				break;
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_offset;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg2 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			break;
		case OP_STORER4_MEMBASE_REG:
		case OP_STORER8_MEMBASE_REG:
			if (arm_is_fpimm8 (ins->inst_offset))
				break;
			low_imm = ins->inst_offset & 0x1ff;
			if ((imm8 = mono_arm_is_rotated_imm8 (ins->inst_offset & ~ 0x1ff, &rot_amount)) >= 0 && arm_is_fpimm8 (low_imm)) {
				ADD_NEW_INS (cfg, temp, OP_ADD_IMM);
				temp->inst_imm = ins->inst_offset & ~0x1ff;
				temp->sreg1 = ins->inst_destbasereg;
				temp->dreg = mono_alloc_ireg (cfg);
				ins->inst_destbasereg = temp->dreg;
				ins->inst_offset = low_imm;
			} else {
				MonoInst *add_ins;

				ADD_NEW_INS (cfg, temp, OP_ICONST);
				temp->inst_c0 = ins->inst_offset;
				temp->dreg = mono_alloc_ireg (cfg);

				ADD_NEW_INS (cfg, add_ins, OP_IADD);
				add_ins->sreg1 = ins->inst_destbasereg;
				add_ins->sreg2 = temp->dreg;
				add_ins->dreg = mono_alloc_ireg (cfg);

				ins->inst_destbasereg = add_ins->dreg;
				ins->inst_offset = 0;
			}
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI1_MEMBASE_IMM:
		case OP_STOREI2_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			ADD_NEW_INS (cfg, temp, OP_ICONST);
			temp->inst_c0 = ins->inst_imm;
			temp->dreg = mono_alloc_ireg (cfg);
			ins->sreg1 = temp->dreg;
			ins->opcode = map_to_reg_reg_op (ins->opcode);
			last_ins = temp;
			goto loop_start; /* make it handle the possibly big ins->inst_offset */
		case OP_FCOMPARE: {
			gboolean swap = FALSE;
			int reg;

			if (!ins->next) {
				/* Optimized away */
				NULLIFY_INS (ins);
				break;
			}

			/* Some fp compares require swapped operands */
			switch (ins->next->opcode) {
			case OP_FBGT:
				ins->next->opcode = OP_FBLT;
				swap = TRUE;
				break;
			case OP_FBGT_UN:
				ins->next->opcode = OP_FBLT_UN;
				swap = TRUE;
				break;
			case OP_FBLE:
				ins->next->opcode = OP_FBGE;
				swap = TRUE;
				break;
			case OP_FBLE_UN:
				ins->next->opcode = OP_FBGE_UN;
				swap = TRUE;
				break;
			default:
				break;
			}
			if (swap) {
				reg = ins->sreg1;
				ins->sreg1 = ins->sreg2;
				ins->sreg2 = reg;
			}
			break;
		}
		}

		last_ins = ins;
	}
	bb->last_ins = last_ins;
	bb->max_vreg = cfg->next_vreg;
}

void
mono_arch_decompose_long_opts (MonoCompile *cfg, MonoInst *long_ins)
{
	MonoInst *ins;

	if (long_ins->opcode == OP_LNEG) {
		ins = long_ins;
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ARM_RSBS_IMM, ins->dreg + 1, ins->sreg1 + 1, 0);
		MONO_EMIT_NEW_BIALU_IMM (cfg, OP_ARM_RSC_IMM, ins->dreg + 2, ins->sreg1 + 2, 0);
		NULLIFY_INS (ins);
	}
}

static guchar*
emit_float_to_int (MonoCompile *cfg, guchar *code, int dreg, int sreg, int size, gboolean is_signed)
{
	/* sreg is a float, dreg is an integer reg  */
	if (IS_FPA)
		ARM_FPA_FIXZ (code, dreg, sreg);
	else if (IS_VFP) {
		if (is_signed)
			ARM_TOSIZD (code, ARM_VFP_F0, sreg);
		else
			ARM_TOUIZD (code, ARM_VFP_F0, sreg);
		ARM_FMRS (code, dreg, ARM_VFP_F0);
	}
	if (!is_signed) {
		if (size == 1)
			ARM_AND_REG_IMM8 (code, dreg, dreg, 0xff);
		else if (size == 2) {
			ARM_SHL_IMM (code, dreg, dreg, 16);
			ARM_SHR_IMM (code, dreg, dreg, 16);
		}
	} else {
		if (size == 1) {
			ARM_SHL_IMM (code, dreg, dreg, 24);
			ARM_SAR_IMM (code, dreg, dreg, 24);
		} else if (size == 2) {
			ARM_SHL_IMM (code, dreg, dreg, 16);
			ARM_SAR_IMM (code, dreg, dreg, 16);
		}
	}
	return code;
}

#endif /* #ifndef DISABLE_JIT */

typedef struct {
	guchar *code;
	const guchar *target;
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
	int count = 0;
	int difflow, diffhigh;

	/* always ensure a call from pdata->code can reach to the thunks without further thunks */
	difflow = (char*)pdata->code - (char*)thunks;
	diffhigh = (char*)pdata->code - (char*)endthunks;
	if (!((is_call_imm (thunks) && is_call_imm (endthunks)) || (is_call_imm (difflow) && is_call_imm (diffhigh))))
		return 0;

	/*
	 * The thunk is composed of 3 words:
	 * load constant from thunks [2] into ARM_IP
	 * bx to ARM_IP
	 * address constant
	 * Note that the LR register is already setup
	 */
	//g_print ("thunk nentries: %d\n", ((char*)endthunks - (char*)thunks)/16);
	if ((pdata->found == 2) || (pdata->code >= code && pdata->code <= code + csize)) {
		while (thunks < endthunks) {
			//g_print ("looking for target: %p at %p (%08x-%08x)\n", pdata->target, thunks, thunks [0], thunks [1]);
			if (thunks [2] == (guint32)pdata->target) {
				arm_patch (pdata->code, (guchar*)thunks);
				mono_arch_flush_icache (pdata->code, 4);
				pdata->found = 1;
				return 1;
			} else if ((thunks [0] == 0) && (thunks [1] == 0) && (thunks [2] == 0)) {
				/* found a free slot instead: emit thunk */
				/* ARMREG_IP is fine to use since this can't be an IMT call
				 * which is indirect
				 */
				code = (guchar*)thunks;
				ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
				if (thumb_supported)
					ARM_BX (code, ARMREG_IP);
				else
					ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
				thunks [2] = (guint32)pdata->target;
				mono_arch_flush_icache ((guchar*)thunks, 12);

				arm_patch (pdata->code, (guchar*)thunks);
				mono_arch_flush_icache (pdata->code, 4);
				pdata->found = 1;
				return 1;
			}
			/* skip 12 bytes, the size of the thunk */
			thunks += 3;
			count++;
		}
		//g_print ("failed thunk lookup for %p from %p at %p (%d entries)\n", pdata->target, pdata->code, data, count);
	}
	return 0;
}

static void
handle_thunk (MonoDomain *domain, int absolute, guchar *code, const guchar *target, MonoCodeManager *dyn_code_mp)
{
	PatchData pdata;

	if (!domain)
		domain = mono_domain_get ();

	pdata.code = code;
	pdata.target = target;
	pdata.absolute = absolute;
	pdata.found = 0;

	if (dyn_code_mp) {
		mono_code_manager_foreach (dyn_code_mp, search_thunk_slot, &pdata);
	}

	if (pdata.found != 1) {
		mono_domain_lock (domain);
		mono_domain_code_foreach (domain, search_thunk_slot, &pdata);

		if (!pdata.found) {
			/* this uses the first available slot */
			pdata.found = 2;
			mono_domain_code_foreach (domain, search_thunk_slot, &pdata);
		}
		mono_domain_unlock (domain);
	}

	if (pdata.found != 1) {
		GHashTable *hash;
		GHashTableIter iter;
		MonoJitDynamicMethodInfo *ji;

		/*
		 * This might be a dynamic method, search its code manager. We can only
		 * use the dynamic method containing CODE, since the others might be freed later.
		 */
		pdata.found = 0;

		mono_domain_lock (domain);
		hash = domain_jit_info (domain)->dynamic_code_hash;
		if (hash) {
			/* FIXME: Speed this up */
			g_hash_table_iter_init (&iter, hash);
			while (g_hash_table_iter_next (&iter, NULL, (gpointer*)&ji)) {
				mono_code_manager_foreach (ji->code_mp, search_thunk_slot, &pdata);
				if (pdata.found == 1)
					break;
			}
		}
		mono_domain_unlock (domain);
	}
	if (pdata.found != 1)
		g_print ("thunk failed for %p from %p\n", target, code);
	g_assert (pdata.found == 1);
}

static void
arm_patch_general (MonoDomain *domain, guchar *code, const guchar *target, MonoCodeManager *dyn_code_mp)
{
	guint32 *code32 = (void*)code;
	guint32 ins = *code32;
	guint32 prim = (ins >> 25) & 7;
	guint32 tval = GPOINTER_TO_UINT (target);

	//g_print ("patching 0x%08x (0x%08x) to point to 0x%08x\n", code, ins, target);
	if (prim == 5) { /* 101b */
		/* the diff starts 8 bytes from the branch opcode */
		gint diff = target - code - 8;
		gint tbits;
		gint tmask = 0xffffffff;
		if (tval & 1) { /* entering thumb mode */
			diff = target - 1 - code - 8;
			g_assert (thumb_supported);
			tbits = 0xf << 28; /* bl->blx bit pattern */
			g_assert ((ins & (1 << 24))); /* it must be a bl, not b instruction */
			/* this low bit of the displacement is moved to bit 24 in the instruction encoding */
			if (diff & 2) {
				tbits |= 1 << 24;
			}
			tmask = ~(1 << 24); /* clear the link bit */
			/*g_print ("blx to thumb: target: %p, code: %p, diff: %d, mask: %x\n", target, code, diff, tmask);*/
		} else {
			tbits = 0;
		}
		if (diff >= 0) {
			if (diff <= 33554431) {
				diff >>= 2;
				ins = (ins & 0xff000000) | diff;
				ins &= tmask;
				*code32 = ins | tbits;
				return;
			}
		} else {
			/* diff between 0 and -33554432 */
			if (diff >= -33554432) {
				diff >>= 2;
				ins = (ins & 0xff000000) | (diff & ~0xff000000);
				ins &= tmask;
				*code32 = ins | tbits;
				return;
			}
		}
		
		handle_thunk (domain, TRUE, code, target, dyn_code_mp);
		return;
	}

	/*
	 * The alternative call sequences looks like this:
	 *
	 * 	ldr ip, [pc] // loads the address constant
	 * 	b 1f         // jumps around the constant
	 * 	address constant embedded in the code
	 *   1f:
	 * 	mov lr, pc
	 * 	mov pc, ip
	 *
	 * There are two cases for patching:
	 * a) at the end of method emission: in this case code points to the start
	 *    of the call sequence
	 * b) during runtime patching of the call site: in this case code points
	 *    to the mov pc, ip instruction
	 *
	 * We have to handle also the thunk jump code sequence:
	 *
	 * 	ldr ip, [pc]
	 * 	mov pc, ip
	 * 	address constant // execution never reaches here
	 */
	if ((ins & 0x0ffffff0) == 0x12fff10) {
		/* Branch and exchange: the address is constructed in a reg 
		 * We can patch BX when the code sequence is the following:
		 *  ldr     ip, [pc, #0]    ; 0x8
		 *  b       0xc
   		 *  .word code_ptr
   	 	 *  mov     lr, pc
  		 *  bx      ips
		 * */
		guint32 ccode [4];
		guint8 *emit = (guint8*)ccode;
		ARM_LDR_IMM (emit, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (emit, 0);
		ARM_MOV_REG_REG (emit, ARMREG_LR, ARMREG_PC);
		ARM_BX (emit, ARMREG_IP);

		/*patching from magic trampoline*/
		if (ins == ccode [3]) {
			g_assert (code32 [-4] == ccode [0]);
			g_assert (code32 [-3] == ccode [1]);
			g_assert (code32 [-1] == ccode [2]);
			code32 [-2] = (guint32)target;
			return;
		}
		/*patching from JIT*/
		if (ins == ccode [0]) {
			g_assert (code32 [1] == ccode [1]);
			g_assert (code32 [3] == ccode [2]);
			g_assert (code32 [4] == ccode [3]);
			code32 [2] = (guint32)target;
			return;
		}
		g_assert_not_reached ();
	} else if ((ins & 0x0ffffff0) == 0x12fff30) {
		/*
		 * ldr ip, [pc, #0]
		 * b 0xc
		 * .word code_ptr
		 * blx ip
		 */
		guint32 ccode [4];
		guint8 *emit = (guint8*)ccode;
		ARM_LDR_IMM (emit, ARMREG_IP, ARMREG_PC, 0);
		ARM_B (emit, 0);
		ARM_BLX_REG (emit, ARMREG_IP);

		g_assert (code32 [-3] == ccode [0]);
		g_assert (code32 [-2] == ccode [1]);
		g_assert (code32 [0] == ccode [2]);

		code32 [-1] = (guint32)target;
	} else {
		guint32 ccode [4];
		guint32 *tmp = ccode;
		guint8 *emit = (guint8*)tmp;
		ARM_LDR_IMM (emit, ARMREG_IP, ARMREG_PC, 0);
		ARM_MOV_REG_REG (emit, ARMREG_LR, ARMREG_PC);
		ARM_MOV_REG_REG (emit, ARMREG_PC, ARMREG_IP);
		ARM_BX (emit, ARMREG_IP);
		if (ins == ccode [2]) {
			g_assert_not_reached (); // should be -2 ...
			code32 [-1] = (guint32)target;
			return;
		}
		if (ins == ccode [0]) {
			/* handles both thunk jump code and the far call sequence */
			code32 [2] = (guint32)target;
			return;
		}
		g_assert_not_reached ();
	}
//	g_print ("patched with 0x%08x\n", ins);
}

void
arm_patch (guchar *code, const guchar *target)
{
	arm_patch_general (NULL, code, target, NULL);
}

/* 
 * Return the >= 0 uimm8 value if val can be represented with a byte + rotation
 * (with the rotation amount in *rot_amount. rot_amount is already adjusted
 * to be used with the emit macros.
 * Return -1 otherwise.
 */
int
mono_arm_is_rotated_imm8 (guint32 val, gint *rot_amount)
{
	guint32 res, i;
	for (i = 0; i < 31; i+= 2) {
		res = (val << (32 - i)) | (val >> i);
		if (res & ~0xff)
			continue;
		*rot_amount = i? 32 - i: 0;
		return res;
	}
	return -1;
}

/*
 * Emits in code a sequence of instructions that load the value 'val'
 * into the dreg register. Uses at most 4 instructions.
 */
guint8*
mono_arm_emit_load_imm (guint8 *code, int dreg, guint32 val)
{
	int imm8, rot_amount;
#if 0
	ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
	/* skip the constant pool */
	ARM_B (code, 0);
	*(int*)code = val;
	code += 4;
	return code;
#endif
	if ((imm8 = mono_arm_is_rotated_imm8 (val, &rot_amount)) >= 0) {
		ARM_MOV_REG_IMM (code, dreg, imm8, rot_amount);
	} else if ((imm8 = mono_arm_is_rotated_imm8 (~val, &rot_amount)) >= 0) {
		ARM_MVN_REG_IMM (code, dreg, imm8, rot_amount);
	} else {
		if (v7_supported) {
			ARM_MOVW_REG_IMM (code, dreg, val & 0xffff);
			if (val >> 16)
				ARM_MOVT_REG_IMM (code, dreg, (val >> 16) & 0xffff);
			return code;
		}
		if (val & 0xFF) {
			ARM_MOV_REG_IMM8 (code, dreg, (val & 0xFF));
			if (val & 0xFF00) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF00) >> 8, 24);
			}
			if (val & 0xFF0000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF0000) >> 16, 16);
			}
			if (val & 0xFF000000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF000000) >> 24, 8);
			}
		} else if (val & 0xFF00) {
			ARM_MOV_REG_IMM (code, dreg, (val & 0xFF00) >> 8, 24);
			if (val & 0xFF0000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF0000) >> 16, 16);
			}
			if (val & 0xFF000000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF000000) >> 24, 8);
			}
		} else if (val & 0xFF0000) {
			ARM_MOV_REG_IMM (code, dreg, (val & 0xFF0000) >> 16, 16);
			if (val & 0xFF000000) {
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF000000) >> 24, 8);
			}
		}
		//g_assert_not_reached ();
	}
	return code;
}

gboolean
mono_arm_thumb_supported (void)
{
	return thumb_supported;
}

#ifndef DISABLE_JIT

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
	MonoInst *inst;
	CallInfo *cinfo;
	guint32 i, pos;

	/* FIXME: Generate intermediate code instead */

	sig = mono_method_signature (method);

	/* This is the opposite of the code in emit_prolog */

	pos = 0;

	cinfo = get_call_info (cfg->generic_sharing_context, NULL, sig);

	if (MONO_TYPE_ISSTRUCT (sig->ret)) {
		ArgInfo *ainfo = &cinfo->ret;
		inst = cfg->vret_addr;
		g_assert (arm_is_imm12 (inst->inst_offset));
		ARM_LDR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
	}
	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];
		
		if (cfg->verbose_level > 2)
			g_print ("Loading argument %d (type: %d)\n", i, ainfo->storage);
		if (inst->opcode == OP_REGVAR) {
			if (ainfo->storage == RegTypeGeneral)
				ARM_MOV_REG_REG (code, inst->dreg, ainfo->reg);
			else if (ainfo->storage == RegTypeFP) {
				g_assert_not_reached ();
			} else if (ainfo->storage == RegTypeBase) {
				// FIXME:
				NOT_IMPLEMENTED;
				/*
				if (arm_is_imm12 (prev_sp_offset + ainfo->offset)) {
					ARM_LDR_IMM (code, inst->dreg, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
					ARM_LDR_REG_REG (code, inst->dreg, ARMREG_SP, ARMREG_IP);
				}
				*/
			} else
				g_assert_not_reached ();
		} else {
			if (ainfo->storage == RegTypeGeneral || ainfo->storage == RegTypeIRegPair) {
				switch (ainfo->size) {
				case 1:
				case 2:
					// FIXME:
					NOT_IMPLEMENTED;
					break;
				case 8:
					g_assert (arm_is_imm12 (inst->inst_offset));
					ARM_LDR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					g_assert (arm_is_imm12 (inst->inst_offset + 4));
					ARM_LDR_IMM (code, ainfo->reg + 1, inst->inst_basereg, inst->inst_offset + 4);
					break;
				default:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_LDR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_LDR_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				}
			} else if (ainfo->storage == RegTypeBaseGen) {
				// FIXME:
				NOT_IMPLEMENTED;
			} else if (ainfo->storage == RegTypeBase) {
				/* Nothing to do */
			} else if (ainfo->storage == RegTypeFP) {
				g_assert_not_reached ();
			} else if (ainfo->storage == RegTypeStructByVal) {
				int doffset = inst->inst_offset;
				int soffset = 0;
				int cur_reg;
				int size = 0;
				if (mono_class_from_mono_type (inst->inst_vtype))
					size = mono_class_native_size (mono_class_from_mono_type (inst->inst_vtype), NULL);
				for (cur_reg = 0; cur_reg < ainfo->size; ++cur_reg) {
					if (arm_is_imm12 (doffset)) {
						ARM_LDR_IMM (code, ainfo->reg + cur_reg, inst->inst_basereg, doffset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, doffset);
						ARM_LDR_REG_REG (code, ainfo->reg + cur_reg, inst->inst_basereg, ARMREG_IP);
					}
					soffset += sizeof (gpointer);
					doffset += sizeof (gpointer);
				}
				if (ainfo->vtsize)
					// FIXME:
					NOT_IMPLEMENTED;
			} else if (ainfo->storage == RegTypeStructByAddr) {
			} else {
				// FIXME:
				NOT_IMPLEMENTED;
			}
		}
		pos ++;
	}

	g_free (cinfo);

	return code;
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
	int imm8, rot_amount;

	/* we don't align basic blocks of loops on arm */

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

    if (mono_break_at_bb_method && mono_method_desc_full_match (mono_break_at_bb_method, cfg->method) && bb->block_num == mono_break_at_bb_bb_num) {
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
							 (gpointer)"mono_break");
		code = emit_call_seq (cfg, code);
	}

	MONO_BB_FOR_EACH_INS (bb, ins) {
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
		case OP_MEMORY_BARRIER:
			if (v6_supported) {
				ARM_MOV_REG_IMM8 (code, ARMREG_R0, 0);
				ARM_MCR (code, 15, 0, ARMREG_R0, 7, 10, 5);
			}
			break;
		case OP_TLS_GET:
#ifdef HAVE_AEABI_READ_TP
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
								 (gpointer)"__aeabi_read_tp");
			code = emit_call_seq (cfg, code);

			ARM_LDR_IMM (code, ins->dreg, ARMREG_R0, ins->inst_offset);
#else
			g_assert_not_reached ();
#endif
			break;
		/*case OP_BIGMUL:
			ppc_mullw (code, ppc_r4, ins->sreg1, ins->sreg2);
			ppc_mulhw (code, ppc_r3, ins->sreg1, ins->sreg2);
			break;
		case OP_BIGMUL_UN:
			ppc_mullw (code, ppc_r4, ins->sreg1, ins->sreg2);
			ppc_mulhwu (code, ppc_r3, ins->sreg1, ins->sreg2);
			break;*/
		case OP_STOREI1_MEMBASE_IMM:
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_imm & 0xFF);
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_STRB_IMM (code, ARMREG_LR, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STOREI2_MEMBASE_IMM:
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_imm & 0xFFFF);
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_STRH_IMM (code, ARMREG_LR, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STORE_MEMBASE_IMM:
		case OP_STOREI4_MEMBASE_IMM:
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_imm);
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_STR_IMM (code, ARMREG_LR, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STOREI1_MEMBASE_REG:
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_STRB_IMM (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STOREI2_MEMBASE_REG:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_STRH_IMM (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_STORE_MEMBASE_REG:
		case OP_STOREI4_MEMBASE_REG:
			/* this case is special, since it happens for spill code after lowering has been called */
			if (arm_is_imm12 (ins->inst_offset)) {
				ARM_STR_IMM (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_STR_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ARMREG_LR);
			}
			break;
		case OP_STOREI1_MEMINDEX:
			ARM_STRB_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STOREI2_MEMINDEX:
			ARM_STRH_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_STORE_MEMINDEX:
		case OP_STOREI4_MEMINDEX:
			ARM_STR_REG_REG (code, ins->sreg1, ins->inst_destbasereg, ins->sreg2);
			break;
		case OP_LOADU4_MEM:
			g_assert_not_reached ();
			break;
		case OP_LOAD_MEMINDEX:
		case OP_LOADI4_MEMINDEX:
		case OP_LOADU4_MEMINDEX:
			ARM_LDR_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI1_MEMINDEX:
			ARM_LDRSB_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADU1_MEMINDEX:
			ARM_LDRB_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADI2_MEMINDEX:
			ARM_LDRSH_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOADU2_MEMINDEX:
			ARM_LDRH_REG_REG (code, ins->dreg, ins->inst_basereg, ins->sreg2);
			break;
		case OP_LOAD_MEMBASE:
		case OP_LOADI4_MEMBASE:
		case OP_LOADU4_MEMBASE:
			/* this case is special, since it happens for spill code after lowering has been called */
			if (arm_is_imm12 (ins->inst_offset)) {
				ARM_LDR_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_LDR_REG_REG (code, ins->dreg, ins->inst_basereg, ARMREG_LR);
			}
			break;
		case OP_LOADI1_MEMBASE:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_LDRSB_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADU1_MEMBASE:
			g_assert (arm_is_imm12 (ins->inst_offset));
			ARM_LDRB_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADU2_MEMBASE:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_LDRH_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_LOADI2_MEMBASE:
			g_assert (arm_is_imm8 (ins->inst_offset));
			ARM_LDRSH_IMM (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_ICONV_TO_I1:
			ARM_SHL_IMM (code, ins->dreg, ins->sreg1, 24);
			ARM_SAR_IMM (code, ins->dreg, ins->dreg, 24);
			break;
		case OP_ICONV_TO_I2:
			ARM_SHL_IMM (code, ins->dreg, ins->sreg1, 16);
			ARM_SAR_IMM (code, ins->dreg, ins->dreg, 16);
			break;
		case OP_ICONV_TO_U1:
			ARM_AND_REG_IMM8 (code, ins->dreg, ins->sreg1, 0xff);
			break;
		case OP_ICONV_TO_U2:
			ARM_SHL_IMM (code, ins->dreg, ins->sreg1, 16);
			ARM_SHR_IMM (code, ins->dreg, ins->dreg, 16);
			break;
		case OP_COMPARE:
		case OP_ICOMPARE:
			ARM_CMP_REG_REG (code, ins->sreg1, ins->sreg2);
			break;
		case OP_COMPARE_IMM:
		case OP_ICOMPARE_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_CMP_REG_IMM (code, ins->sreg1, imm8, rot_amount);
			break;
		case OP_BREAK:
			/*
			 * gdb does not like encountering the hw breakpoint ins in the debugged code. 
			 * So instead of emitting a trap, we emit a call a C function and place a 
			 * breakpoint there.
			 */
			//*(int*)code = 0xef9f0001;
			//code += 4;
			//ARM_DBRK (code);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
								 (gpointer)"mono_break");
			code = emit_call_seq (cfg, code);
			break;
		case OP_RELAXED_NOP:
			ARM_NOP (code);
			break;
		case OP_NOP:
		case OP_DUMMY_USE:
		case OP_DUMMY_STORE:
		case OP_NOT_REACHED:
		case OP_NOT_NULL:
			break;
		case OP_SEQ_POINT: {
			int i;
			MonoInst *info_var = cfg->arch.seq_point_info_var;
			MonoInst *ss_trigger_page_var = cfg->arch.ss_trigger_page_var;
			MonoInst *ss_read_var = cfg->arch.seq_point_read_var;
			MonoInst *ss_method_var = cfg->arch.seq_point_ss_method_var;
			MonoInst *bp_method_var = cfg->arch.seq_point_bp_method_var;
			MonoInst *var;
			int dreg = ARMREG_LR;

			if (cfg->soft_breakpoints) {
				g_assert (!cfg->compile_aot);
			}

			/*
			 * For AOT, we use one got slot per method, which will point to a
			 * SeqPointInfo structure, containing all the information required
			 * by the code below.
			 */
			if (cfg->compile_aot) {
				g_assert (info_var);
				g_assert (info_var->opcode == OP_REGOFFSET);
				g_assert (arm_is_imm12 (info_var->inst_offset));
			}

			if (!cfg->soft_breakpoints) {
				/*
				 * Read from the single stepping trigger page. This will cause a
				 * SIGSEGV when single stepping is enabled.
				 * We do this _before_ the breakpoint, so single stepping after
				 * a breakpoint is hit will step to the next IL offset.
				 */
				g_assert (((guint64)(gsize)ss_trigger_page >> 32) == 0);
			}

			if (ins->flags & MONO_INST_SINGLE_STEP_LOC) {
				if (cfg->soft_breakpoints) {
					/* Load the address of the sequence point trigger variable. */
					var = ss_read_var;
					g_assert (var);
					g_assert (var->opcode == OP_REGOFFSET);
					g_assert (arm_is_imm12 (var->inst_offset));
					ARM_LDR_IMM (code, dreg, var->inst_basereg, var->inst_offset);

					/* Read the value and check whether it is non-zero. */
					ARM_LDR_IMM (code, dreg, dreg, 0);
					ARM_CMP_REG_IMM (code, dreg, 0, 0);

					/* Load the address of the sequence point method. */
					var = ss_method_var;
					g_assert (var);
					g_assert (var->opcode == OP_REGOFFSET);
					g_assert (arm_is_imm12 (var->inst_offset));
					ARM_LDR_IMM (code, dreg, var->inst_basereg, var->inst_offset);

					/* Call it conditionally. */
					ARM_BLX_REG_COND (code, ARMCOND_NE, dreg);
				} else {
					if (cfg->compile_aot) {
						/* Load the trigger page addr from the variable initialized in the prolog */
						var = ss_trigger_page_var;
						g_assert (var);
						g_assert (var->opcode == OP_REGOFFSET);
						g_assert (arm_is_imm12 (var->inst_offset));
						ARM_LDR_IMM (code, dreg, var->inst_basereg, var->inst_offset);
					} else {
						ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
						ARM_B (code, 0);
						*(int*)code = (int)ss_trigger_page;
						code += 4;
					}
					ARM_LDR_IMM (code, dreg, dreg, 0);
				}
			}

			mono_add_seq_point (cfg, bb, ins, code - cfg->native_code);

			if (cfg->soft_breakpoints) {
				/* Load the address of the breakpoint method into ip. */
				var = bp_method_var;
				g_assert (var);
				g_assert (var->opcode == OP_REGOFFSET);
				g_assert (arm_is_imm12 (var->inst_offset));
				ARM_LDR_IMM (code, dreg, var->inst_basereg, var->inst_offset);

				/*
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				ARM_NOP (code);
			} else if (cfg->compile_aot) {
				guint32 offset = code - cfg->native_code;
				guint32 val;

				ARM_LDR_IMM (code, dreg, info_var->inst_basereg, info_var->inst_offset);
				/* Add the offset */
				val = ((offset / 4) * sizeof (guint8*)) + G_STRUCT_OFFSET (SeqPointInfo, bp_addrs);
				ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF), 0);
				if (val & 0xFF00)
					ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF00) >> 8, 24);
				if (val & 0xFF0000)
					ARM_ADD_REG_IMM (code, dreg, dreg, (val & 0xFF0000) >> 16, 16);
				g_assert (!(val & 0xFF000000));
				/* Load the info->bp_addrs [offset], which is either 0 or the address of a trigger page */
				ARM_LDR_IMM (code, dreg, dreg, 0);

				/* What is faster, a branch or a load ? */
				ARM_CMP_REG_IMM (code, dreg, 0, 0);
				/* The breakpoint instruction */
				ARM_LDR_IMM_COND (code, dreg, dreg, 0, ARMCOND_NE);
			} else {
				/* 
				 * A placeholder for a possible breakpoint inserted by
				 * mono_arch_set_breakpoint ().
				 */
				for (i = 0; i < 4; ++i)
					ARM_NOP (code);
			}
			break;
		}
		case OP_ADDCC:
		case OP_IADDCC:
			ARM_ADDS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_IADD:
			ARM_ADD_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADC:
		case OP_IADC:
			ARM_ADCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_ADDCC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ADDS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ADD_IMM:
		case OP_IADD_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ADD_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ADC_IMM:
		case OP_IADC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ADCS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IADD_OVF:
			ARM_ADD_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_IADD_OVF_UN:
			ARM_ADD_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ISUB_OVF:
			ARM_SUB_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ISUB_OVF_UN:
			ARM_SUB_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_CARRY:
			ARM_ADCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_ADD_OVF_UN_CARRY:
			ARM_ADCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_CARRY:
			ARM_SBCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_FALSE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUB_OVF_UN_CARRY:
			ARM_SBCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			//EMIT_COND_SYSTEM_EXCEPTION_FLAGS (PPC_BR_TRUE, PPC_BR_EQ, "OverflowException");
			break;
		case OP_SUBCC:
		case OP_ISUBCC:
			ARM_SUBS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SUBCC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_SUBS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ISUB:
			ARM_SUB_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SBB:
		case OP_ISBB:
			ARM_SBCS_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SUB_IMM:
		case OP_ISUB_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_SUB_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_SBB_IMM:
		case OP_ISBB_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_SBCS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ARM_RSBS_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_RSBS_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ARM_RSC_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_RSC_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IAND:
			ARM_AND_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_AND_IMM:
		case OP_IAND_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_AND_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IDIV:
		case OP_IDIV_UN:
		case OP_DIV_IMM:
		case OP_IREM:
		case OP_IREM_UN:
		case OP_REM_IMM:
			/* crappy ARM arch doesn't have a DIV instruction */
			g_assert_not_reached ();
		case OP_IOR:
			ARM_ORR_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_OR_IMM:
		case OP_IOR_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_ORR_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_IXOR:
			ARM_EOR_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_XOR_IMM:
		case OP_IXOR_IMM:
			imm8 = mono_arm_is_rotated_imm8 (ins->inst_imm, &rot_amount);
			g_assert (imm8 >= 0);
			ARM_EOR_REG_IMM (code, ins->dreg, ins->sreg1, imm8, rot_amount);
			break;
		case OP_ISHL:
			ARM_SHL_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHL_IMM:
		case OP_ISHL_IMM:
			if (ins->inst_imm)
				ARM_SHL_IMM (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_ISHR:
			ARM_SAR_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_SHR_IMM:
		case OP_ISHR_IMM:
			if (ins->inst_imm)
				ARM_SAR_IMM (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_SHR_UN_IMM:
		case OP_ISHR_UN_IMM:
			if (ins->inst_imm)
				ARM_SHR_IMM (code, ins->dreg, ins->sreg1, (ins->inst_imm & 0x1f));
			else if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_ISHR_UN:
			ARM_SHR_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_INOT:
			ARM_MVN_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_INEG:
			ARM_RSB_REG_IMM8 (code, ins->dreg, ins->sreg1, 0);
			break;
		case OP_IMUL:
			if (ins->dreg == ins->sreg2)
				ARM_MUL_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			else
				ARM_MUL_REG_REG (code, ins->dreg, ins->sreg2, ins->sreg1);
			break;
		case OP_MUL_IMM:
			g_assert_not_reached ();
			break;
		case OP_IMUL_OVF:
			/* FIXME: handle ovf/ sreg2 != dreg */
			ARM_MUL_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			/* FIXME: MUL doesn't set the C/O flags on ARM */
			break;
		case OP_IMUL_OVF_UN:
			/* FIXME: handle ovf/ sreg2 != dreg */
			ARM_MUL_REG_REG (code, ins->dreg, ins->sreg1, ins->sreg2);
			/* FIXME: MUL doesn't set the C/O flags on ARM */
			break;
		case OP_ICONST:
			code = mono_arm_emit_load_imm (code, ins->dreg, ins->inst_c0);
			break;
		case OP_AOTCONST:
			/* Load the GOT offset */
			mono_add_patch_info (cfg, offset, (MonoJumpInfoType)ins->inst_i1, ins->inst_p0);
			ARM_LDR_IMM (code, ins->dreg, ARMREG_PC, 0);
			ARM_B (code, 0);
			*(gpointer*)code = NULL;
			code += 4;
			/* Load the value from the GOT */
			ARM_LDR_REG_REG (code, ins->dreg, ARMREG_PC, ins->dreg);
			break;
		case OP_ICONV_TO_I4:
		case OP_ICONV_TO_U4:
		case OP_MOVE:
			if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		case OP_SETLRET: {
			int saved = ins->sreg2;
			if (ins->sreg2 == ARM_LSW_REG) {
				ARM_MOV_REG_REG (code, ARMREG_LR, ins->sreg2);
				saved = ARMREG_LR;
			}
			if (ins->sreg1 != ARM_LSW_REG)
				ARM_MOV_REG_REG (code, ARM_LSW_REG, ins->sreg1);
			if (saved != ARM_MSW_REG)
				ARM_MOV_REG_REG (code, ARM_MSW_REG, saved);
			break;
		}
		case OP_FMOVE:
			if (IS_FPA)
				ARM_FPA_MVFD (code, ins->dreg, ins->sreg1);
			else if (IS_VFP)
				ARM_CPYD (code, ins->dreg, ins->sreg1);
			break;
		case OP_FCONV_TO_R4:
			if (IS_FPA)
				ARM_FPA_MVFS (code, ins->dreg, ins->sreg1);
			else if (IS_VFP) {
				ARM_CVTD (code, ins->dreg, ins->sreg1);
				ARM_CVTS (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_JMP:
			/*
			 * Keep in sync with mono_arch_emit_epilog
			 */
			g_assert (!cfg->method->save_lmf);

			code = emit_load_volatile_arguments (cfg, code);

			code = emit_big_add (code, ARMREG_SP, cfg->frame_reg, cfg->stack_usage);
			if (iphone_abi) {
				if (cfg->used_int_regs)
					ARM_POP (code, cfg->used_int_regs);
				ARM_POP (code, (1 << ARMREG_R7) | (1 << ARMREG_LR));
			} else {
				ARM_POP (code, cfg->used_int_regs | (1 << ARMREG_LR));
			}
			mono_add_patch_info (cfg, (guint8*) code - cfg->native_code, MONO_PATCH_INFO_METHOD_JUMP, ins->inst_p0);
			if (cfg->compile_aot) {
				ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);
				ARM_B (code, 0);
				*(gpointer*)code = NULL;
				code += 4;
				ARM_LDR_REG_REG (code, ARMREG_PC, ARMREG_PC, ARMREG_IP);
			} else {
				ARM_B (code, 0);
			}
			break;
		case OP_CHECK_THIS:
			/* ensure ins->sreg1 is not NULL */
			ARM_LDRB_IMM (code, ARMREG_LR, ins->sreg1, 0);
			break;
		case OP_ARGLIST: {
			g_assert (cfg->sig_cookie < 128);
			ARM_LDR_IMM (code, ARMREG_IP, cfg->frame_reg, cfg->sig_cookie);
			ARM_STR_IMM (code, ARMREG_IP, ins->sreg1, 0);
			break;
		}
		case OP_FCALL:
		case OP_LCALL:
		case OP_VCALL:
		case OP_VCALL2:
		case OP_VOIDCALL:
		case OP_CALL:
			call = (MonoCallInst*)ins;
			if (ins->flags & MONO_INST_HAS_METHOD)
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_METHOD, call->method);
			else
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_ABS, call->fptr);
			code = emit_call_seq (cfg, code);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_REG:
		case OP_LCALL_REG:
		case OP_VCALL_REG:
		case OP_VCALL2_REG:
		case OP_VOIDCALL_REG:
		case OP_CALL_REG:
			code = emit_call_reg (code, ins->sreg1);
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_FCALL_MEMBASE:
		case OP_LCALL_MEMBASE:
		case OP_VCALL_MEMBASE:
		case OP_VCALL2_MEMBASE:
		case OP_VOIDCALL_MEMBASE:
		case OP_CALL_MEMBASE:
			g_assert (arm_is_imm12 (ins->inst_offset));
			g_assert (ins->sreg1 != ARMREG_LR);
			call = (MonoCallInst*)ins;
			if (call->dynamic_imt_arg || call->method->klass->flags & TYPE_ATTRIBUTE_INTERFACE) {
				ARM_ADD_REG_IMM8 (code, ARMREG_LR, ARMREG_PC, 4);
				ARM_LDR_IMM (code, ARMREG_PC, ins->sreg1, ins->inst_offset);
				/* 
				 * We can't embed the method in the code stream in PIC code, or
				 * in gshared code.
				 * Instead, we put it in V5 in code emitted by 
				 * mono_arch_emit_imt_argument (), and embed NULL here to 
				 * signal the IMT thunk that the value is in V5.
				 */
				if (call->dynamic_imt_arg)
					*((gpointer*)code) = NULL;
				else
					*((gpointer*)code) = (gpointer)call->method;
				code += 4;
			} else {
				ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
				ARM_LDR_IMM (code, ARMREG_PC, ins->sreg1, ins->inst_offset);
			}
			ins->flags |= MONO_INST_GC_CALLSITE;
			ins->backend.pc_offset = code - cfg->native_code;
			code = emit_move_return_value (cfg, ins, code);
			break;
		case OP_LOCALLOC: {
			/* keep alignment */
			int alloca_waste = cfg->param_area;
			alloca_waste += 7;
			alloca_waste &= ~7;
			/* round the size to 8 bytes */
			ARM_ADD_REG_IMM8 (code, ins->dreg, ins->sreg1, 7);
			ARM_BIC_REG_IMM8 (code, ins->dreg, ins->dreg, 7);
			if (alloca_waste)
				ARM_ADD_REG_IMM8 (code, ins->dreg, ins->dreg, alloca_waste);
			ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ins->dreg);
			/* memzero the area: dreg holds the size, sp is the pointer */
			if (ins->flags & MONO_INST_INIT) {
				guint8 *start_loop, *branch_to_cond;
				ARM_MOV_REG_IMM8 (code, ARMREG_LR, 0);
				branch_to_cond = code;
				ARM_B (code, 0);
				start_loop = code;
				ARM_STR_REG_REG (code, ARMREG_LR, ARMREG_SP, ins->dreg);
				arm_patch (branch_to_cond, code);
				/* decrement by 4 and set flags */
				ARM_SUBS_REG_IMM8 (code, ins->dreg, ins->dreg, sizeof (mgreg_t));
				ARM_B_COND (code, ARMCOND_GE, 0);
				arm_patch (code - 4, start_loop);
			}
			ARM_ADD_REG_IMM8 (code, ins->dreg, ARMREG_SP, alloca_waste);
			break;
		}
		case OP_DYN_CALL: {
			int i;
			MonoInst *var = cfg->dyn_call_var;

			g_assert (var->opcode == OP_REGOFFSET);
			g_assert (arm_is_imm12 (var->inst_offset));

			/* lr = args buffer filled by mono_arch_get_dyn_call_args () */
			ARM_MOV_REG_REG( code, ARMREG_LR, ins->sreg1);
			/* ip = ftn */
			ARM_MOV_REG_REG( code, ARMREG_IP, ins->sreg2);

			/* Save args buffer */
			ARM_STR_IMM (code, ARMREG_LR, var->inst_basereg, var->inst_offset);

			/* Set stack slots using R0 as scratch reg */
			/* MONO_ARCH_DYN_CALL_PARAM_AREA gives the size of stack space available */
			for (i = 0; i < DYN_CALL_STACK_ARGS; ++i) {
				ARM_LDR_IMM (code, ARMREG_R0, ARMREG_LR, (PARAM_REGS + i) * sizeof (mgreg_t));
				ARM_STR_IMM (code, ARMREG_R0, ARMREG_SP, i * sizeof (mgreg_t));
			}

			/* Set argument registers */
			for (i = 0; i < PARAM_REGS; ++i)
				ARM_LDR_IMM (code, i, ARMREG_LR, i * sizeof (mgreg_t));

			/* Make the call */
			ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
			ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);

			/* Save result */
			ARM_LDR_IMM (code, ARMREG_IP, var->inst_basereg, var->inst_offset);
			ARM_STR_IMM (code, ARMREG_R0, ARMREG_IP, G_STRUCT_OFFSET (DynCallArgs, res)); 
			ARM_STR_IMM (code, ARMREG_R1, ARMREG_IP, G_STRUCT_OFFSET (DynCallArgs, res2)); 
			break;
		}
		case OP_THROW: {
			if (ins->sreg1 != ARMREG_R0)
				ARM_MOV_REG_REG (code, ARMREG_R0, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_throw_exception");
			code = emit_call_seq (cfg, code);
			break;
		}
		case OP_RETHROW: {
			if (ins->sreg1 != ARMREG_R0)
				ARM_MOV_REG_REG (code, ARMREG_R0, ins->sreg1);
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_INTERNAL_METHOD, 
					     (gpointer)"mono_arch_rethrow_exception");
			code = emit_call_seq (cfg, code);
			break;
		}
		case OP_START_HANDLER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			int i, rot_amount;

			/* Reserve a param area, see filter-stack.exe */
			if (cfg->param_area) {
				if ((i = mono_arm_is_rotated_imm8 (cfg->param_area, &rot_amount)) >= 0) {
					ARM_SUB_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, cfg->param_area);
					ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
				}
			}

			if (arm_is_imm12 (spvar->inst_offset)) {
				ARM_STR_IMM (code, ARMREG_LR, spvar->inst_basereg, spvar->inst_offset);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_IP, spvar->inst_offset);
				ARM_STR_REG_REG (code, ARMREG_LR, spvar->inst_basereg, ARMREG_IP);
			}
			break;
		}
		case OP_ENDFILTER: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			int i, rot_amount;

			/* Free the param area */
			if (cfg->param_area) {
				if ((i = mono_arm_is_rotated_imm8 (cfg->param_area, &rot_amount)) >= 0) {
					ARM_ADD_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, cfg->param_area);
					ARM_ADD_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
				}
			}

			if (ins->sreg1 != ARMREG_R0)
				ARM_MOV_REG_REG (code, ARMREG_R0, ins->sreg1);
			if (arm_is_imm12 (spvar->inst_offset)) {
				ARM_LDR_IMM (code, ARMREG_IP, spvar->inst_basereg, spvar->inst_offset);
			} else {
				g_assert (ARMREG_IP != spvar->inst_basereg);
				code = mono_arm_emit_load_imm (code, ARMREG_IP, spvar->inst_offset);
				ARM_LDR_REG_REG (code, ARMREG_IP, spvar->inst_basereg, ARMREG_IP);
			}
			ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
			break;
		}
		case OP_ENDFINALLY: {
			MonoInst *spvar = mono_find_spvar_for_region (cfg, bb->region);
			int i, rot_amount;

			/* Free the param area */
			if (cfg->param_area) {
				if ((i = mono_arm_is_rotated_imm8 (cfg->param_area, &rot_amount)) >= 0) {
					ARM_ADD_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, cfg->param_area);
					ARM_ADD_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
				}
			}

			if (arm_is_imm12 (spvar->inst_offset)) {
				ARM_LDR_IMM (code, ARMREG_IP, spvar->inst_basereg, spvar->inst_offset);
			} else {
				g_assert (ARMREG_IP != spvar->inst_basereg);
				code = mono_arm_emit_load_imm (code, ARMREG_IP, spvar->inst_offset);
				ARM_LDR_REG_REG (code, ARMREG_IP, spvar->inst_basereg, ARMREG_IP);
			}
			ARM_MOV_REG_REG (code, ARMREG_PC, ARMREG_IP);
			break;
		}
		case OP_CALL_HANDLER: 
			mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_BB, ins->inst_target_bb);
			ARM_BL (code, 0);
			mono_cfg_add_try_hole (cfg, ins->inst_eh_block, code, bb);
			break;
		case OP_LABEL:
			ins->inst_c0 = code - cfg->native_code;
			break;
		case OP_BR:
			/*if (ins->inst_target_bb->native_offset) {
				ARM_B (code, 0);
				//x86_jump_code (code, cfg->native_code + ins->inst_target_bb->native_offset); 
			} else*/ {
				mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_BB, ins->inst_target_bb);
				ARM_B (code, 0);
			} 
			break;
		case OP_BR_REG:
			ARM_MOV_REG_REG (code, ARMREG_PC, ins->sreg1);
			break;
		case OP_SWITCH:
			/* 
			 * In the normal case we have:
			 * 	ldr pc, [pc, ins->sreg1 << 2]
			 * 	nop
			 * If aot, we have:
			 * 	ldr lr, [pc, ins->sreg1 << 2]
			 * 	add pc, pc, lr
			 * After follows the data.
			 * FIXME: add aot support.
			 */
			mono_add_patch_info (cfg, offset, MONO_PATCH_INFO_SWITCH, ins->inst_p0);
			max_len += 4 * GPOINTER_TO_INT (ins->klass);
			if (offset + max_len > (cfg->code_size - 16)) {
				cfg->code_size += max_len;
				cfg->code_size *= 2;
				cfg->native_code = g_realloc (cfg->native_code, cfg->code_size);
				code = cfg->native_code + offset;
			}
			ARM_LDR_REG_REG_SHIFT (code, ARMREG_PC, ARMREG_PC, ins->sreg1, ARMSHIFT_LSL, 2);
			ARM_NOP (code);
			code += 4 * GPOINTER_TO_INT (ins->klass);
			break;
		case OP_CEQ:
		case OP_ICEQ:
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_EQ);
			break;
		case OP_CLT:
		case OP_ICLT:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_LT);
			break;
		case OP_CLT_UN:
		case OP_ICLT_UN:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_LO);
			break;
		case OP_CGT:
		case OP_ICGT:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_GT);
			break;
		case OP_CGT_UN:
		case OP_ICGT_UN:
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_HI);
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
			EMIT_COND_SYSTEM_EXCEPTION (ins->opcode - OP_COND_EXC_IEQ, ins->inst_p1);
			break;
		case OP_COND_EXC_C:
		case OP_COND_EXC_IC:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_CS, ins->inst_p1);
			break;
		case OP_COND_EXC_OV:
		case OP_COND_EXC_IOV:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_VS, ins->inst_p1);
			break;
		case OP_COND_EXC_NC:
		case OP_COND_EXC_INC:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_CC, ins->inst_p1);
			break;
		case OP_COND_EXC_NO:
		case OP_COND_EXC_INO:
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_VC, ins->inst_p1);
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
		case OP_IBLE_UN:
			EMIT_COND_BRANCH (ins, ins->opcode - OP_IBEQ);
			break;

		/* floating point opcodes */
#ifdef ARM_FPU_FPA
		case OP_R8CONST:
			if (cfg->compile_aot) {
				ARM_FPA_LDFD (code, ins->dreg, ARMREG_PC, 0);
				ARM_B (code, 1);
				*(guint32*)code = ((guint32*)(ins->inst_p0))[0];
				code += 4;
				*(guint32*)code = ((guint32*)(ins->inst_p0))[1];
				code += 4;
			} else {
				/* FIXME: we can optimize the imm load by dealing with part of 
				 * the displacement in LDFD (aligning to 512).
				 */
				code = mono_arm_emit_load_imm (code, ARMREG_LR, (guint32)ins->inst_p0);
				ARM_FPA_LDFD (code, ins->dreg, ARMREG_LR, 0);
			}
			break;
		case OP_R4CONST:
			if (cfg->compile_aot) {
				ARM_FPA_LDFS (code, ins->dreg, ARMREG_PC, 0);
				ARM_B (code, 0);
				*(guint32*)code = ((guint32*)(ins->inst_p0))[0];
				code += 4;
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, (guint32)ins->inst_p0);
				ARM_FPA_LDFS (code, ins->dreg, ARMREG_LR, 0);
			}
			break;
		case OP_STORER8_MEMBASE_REG:
			/* This is generated by the local regalloc pass which runs after the lowering pass */
			if (!arm_is_fpimm8 (ins->inst_offset)) {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->inst_destbasereg);
				ARM_FPA_STFD (code, ins->sreg1, ARMREG_LR, 0);
			} else {
				ARM_FPA_STFD (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			}
			break;
		case OP_LOADR8_MEMBASE:
			/* This is generated by the local regalloc pass which runs after the lowering pass */
			if (!arm_is_fpimm8 (ins->inst_offset)) {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->inst_basereg);
				ARM_FPA_LDFD (code, ins->dreg, ARMREG_LR, 0);
			} else {
				ARM_FPA_LDFD (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			g_assert (arm_is_fpimm8 (ins->inst_offset));
			ARM_FPA_STFS (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_LOADR4_MEMBASE:
			g_assert (arm_is_fpimm8 (ins->inst_offset));
			ARM_FPA_LDFS (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			break;
		case OP_ICONV_TO_R_UN: {
			int tmpreg;
			tmpreg = ins->dreg == 0? 1: 0;
			ARM_CMP_REG_IMM8 (code, ins->sreg1, 0);
			ARM_FPA_FLTD (code, ins->dreg, ins->sreg1);
			ARM_B_COND (code, ARMCOND_GE, 8);
			/* save the temp register */
			ARM_SUB_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 8);
			ARM_FPA_STFD (code, tmpreg, ARMREG_SP, 0);
			ARM_FPA_LDFD (code, tmpreg, ARMREG_PC, 12);
			ARM_FPA_ADFD (code, ins->dreg, ins->dreg, tmpreg);
			ARM_FPA_LDFD (code, tmpreg, ARMREG_SP, 0);
			ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 8);
			/* skip the constant pool */
			ARM_B (code, 8);
			code += 4;
			*(int*)code = 0x41f00000;
			code += 4;
			*(int*)code = 0;
			code += 4;
			/* FIXME: adjust:
			 * ldfltd  ftemp, [pc, #8] 0x41f00000 0x00000000
			 * adfltd  fdest, fdest, ftemp
			 */
			break;
		}
		case OP_ICONV_TO_R4:
			ARM_FPA_FLTS (code, ins->dreg, ins->sreg1);
			break;
		case OP_ICONV_TO_R8:
			ARM_FPA_FLTD (code, ins->dreg, ins->sreg1);
			break;

#elif defined(ARM_FPU_VFP)

		case OP_R8CONST:
			if (cfg->compile_aot) {
				ARM_FLDD (code, ins->dreg, ARMREG_PC, 0);
				ARM_B (code, 1);
				*(guint32*)code = ((guint32*)(ins->inst_p0))[0];
				code += 4;
				*(guint32*)code = ((guint32*)(ins->inst_p0))[1];
				code += 4;
			} else {
				/* FIXME: we can optimize the imm load by dealing with part of 
				 * the displacement in LDFD (aligning to 512).
				 */
				code = mono_arm_emit_load_imm (code, ARMREG_LR, (guint32)ins->inst_p0);
				ARM_FLDD (code, ins->dreg, ARMREG_LR, 0);
			}
			break;
		case OP_R4CONST:
			if (cfg->compile_aot) {
				ARM_FLDS (code, ins->dreg, ARMREG_PC, 0);
				ARM_B (code, 0);
				*(guint32*)code = ((guint32*)(ins->inst_p0))[0];
				code += 4;
				ARM_CVTS (code, ins->dreg, ins->dreg);
			} else {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, (guint32)ins->inst_p0);
				ARM_FLDS (code, ins->dreg, ARMREG_LR, 0);
				ARM_CVTS (code, ins->dreg, ins->dreg);
			}
			break;
		case OP_STORER8_MEMBASE_REG:
			/* This is generated by the local regalloc pass which runs after the lowering pass */
			if (!arm_is_fpimm8 (ins->inst_offset)) {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->inst_destbasereg);
				ARM_FSTD (code, ins->sreg1, ARMREG_LR, 0);
			} else {
				ARM_FSTD (code, ins->sreg1, ins->inst_destbasereg, ins->inst_offset);
			}
			break;
		case OP_LOADR8_MEMBASE:
			/* This is generated by the local regalloc pass which runs after the lowering pass */
			if (!arm_is_fpimm8 (ins->inst_offset)) {
				code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
				ARM_ADD_REG_REG (code, ARMREG_LR, ARMREG_LR, ins->inst_basereg);
				ARM_FLDD (code, ins->dreg, ARMREG_LR, 0);
			} else {
				ARM_FLDD (code, ins->dreg, ins->inst_basereg, ins->inst_offset);
			}
			break;
		case OP_STORER4_MEMBASE_REG:
			g_assert (arm_is_fpimm8 (ins->inst_offset));
			ARM_CVTD (code, ARM_VFP_F0, ins->sreg1);
			ARM_FSTS (code, ARM_VFP_F0, ins->inst_destbasereg, ins->inst_offset);
			break;
		case OP_LOADR4_MEMBASE:
			g_assert (arm_is_fpimm8 (ins->inst_offset));
			ARM_FLDS (code, ARM_VFP_F0, ins->inst_basereg, ins->inst_offset);
			ARM_CVTS (code, ins->dreg, ARM_VFP_F0);
			break;
		case OP_ICONV_TO_R_UN: {
			g_assert_not_reached ();
			break;
		}
		case OP_ICONV_TO_R4:
			ARM_FMSR (code, ARM_VFP_F0, ins->sreg1);
			ARM_FSITOS (code, ARM_VFP_F0, ARM_VFP_F0);
			ARM_CVTS (code, ins->dreg, ARM_VFP_F0);
			break;
		case OP_ICONV_TO_R8:
			ARM_FMSR (code, ARM_VFP_F0, ins->sreg1);
			ARM_FSITOD (code, ins->dreg, ARM_VFP_F0);
			break;

		case OP_SETFRET:
			if (mono_method_signature (cfg->method)->ret->type == MONO_TYPE_R4) {
				ARM_CVTD (code, ARM_VFP_F0, ins->sreg1);
				ARM_FMRS (code, ARMREG_R0, ARM_VFP_F0);
			} else {
				ARM_FMRRD (code, ARMREG_R0, ARMREG_R1, ins->sreg1);
			}
			break;

#endif

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
		case OP_LCONV_TO_OVF_I4_2: {
			guint8 *high_bit_not_set, *valid_negative, *invalid_negative, *valid_positive;
			/* 
			 * Valid ints: 0xffffffff:8000000 to 00000000:0x7f000000
			 */

			ARM_CMP_REG_IMM8 (code, ins->sreg1, 0);
			high_bit_not_set = code;
			ARM_B_COND (code, ARMCOND_GE, 0); /*branch if bit 31 of the lower part is not set*/

			ARM_CMN_REG_IMM8 (code, ins->sreg2, 1); /*This have the same effect as CMP reg, 0xFFFFFFFF */
			valid_negative = code;
			ARM_B_COND (code, ARMCOND_EQ, 0); /*branch if upper part == 0xFFFFFFFF (lower part has bit 31 set) */
			invalid_negative = code;
			ARM_B_COND (code, ARMCOND_AL, 0);
			
			arm_patch (high_bit_not_set, code);

			ARM_CMP_REG_IMM8 (code, ins->sreg2, 0);
			valid_positive = code;
			ARM_B_COND (code, ARMCOND_EQ, 0); /*branch if upper part == 0 (lower part has bit 31 clear)*/

			arm_patch (invalid_negative, code);
			EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_AL, "OverflowException");

			arm_patch (valid_negative, code);
			arm_patch (valid_positive, code);

			if (ins->dreg != ins->sreg1)
				ARM_MOV_REG_REG (code, ins->dreg, ins->sreg1);
			break;
		}
#ifdef ARM_FPU_FPA
		case OP_FADD:
			ARM_FPA_ADFD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			ARM_FPA_SUFD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FMUL:
			ARM_FPA_MUFD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FDIV:
			ARM_FPA_DVFD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FNEG:
			ARM_FPA_MNFD (code, ins->dreg, ins->sreg1);
			break;
#elif defined(ARM_FPU_VFP)
		case OP_FADD:
			ARM_VFP_ADDD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;
		case OP_FSUB:
			ARM_VFP_SUBD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FMUL:
			ARM_VFP_MULD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FDIV:
			ARM_VFP_DIVD (code, ins->dreg, ins->sreg1, ins->sreg2);
			break;		
		case OP_FNEG:
			ARM_NEGD (code, ins->dreg, ins->sreg1);
			break;
#endif
		case OP_FREM:
			/* emulated */
			g_assert_not_reached ();
			break;
		case OP_FCOMPARE:
			if (IS_FPA) {
				ARM_FPA_FCMP (code, ARM_FPA_CMF, ins->sreg1, ins->sreg2);
			} else if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			break;
		case OP_FCEQ:
			if (IS_FPA) {
				ARM_FPA_FCMP (code, ARM_FPA_CMF, ins->sreg1, ins->sreg2);
			} else if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 0, ARMCOND_NE);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_EQ);
			break;
		case OP_FCLT:
			if (IS_FPA) {
				ARM_FPA_FCMP (code, ARM_FPA_CMF, ins->sreg1, ins->sreg2);
			} else {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			break;
		case OP_FCLT_UN:
			if (IS_FPA) {
				ARM_FPA_FCMP (code, ARM_FPA_CMF, ins->sreg1, ins->sreg2);
			} else if (IS_VFP) {
				ARM_CMPD (code, ins->sreg1, ins->sreg2);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_VS);
			break;
		case OP_FCGT:
			/* swapped */
			if (IS_FPA) {
				ARM_FPA_FCMP (code, ARM_FPA_CMF, ins->sreg2, ins->sreg1);
			} else if (IS_VFP) {
				ARM_CMPD (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			break;
		case OP_FCGT_UN:
			/* swapped */
			if (IS_FPA) {
				ARM_FPA_FCMP (code, ARM_FPA_CMF, ins->sreg2, ins->sreg1);
			} else if (IS_VFP) {
				ARM_CMPD (code, ins->sreg2, ins->sreg1);
				ARM_FMSTAT (code);
			}
			ARM_MOV_REG_IMM8 (code, ins->dreg, 0);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_MI);
			ARM_MOV_REG_IMM8_COND (code, ins->dreg, 1, ARMCOND_VS);
			break;
		/* ARM FPA flags table:
		 * N        Less than               ARMCOND_MI
		 * Z        Equal                   ARMCOND_EQ
		 * C        Greater Than or Equal   ARMCOND_CS
		 * V        Unordered               ARMCOND_VS
		 */
		case OP_FBEQ:
			EMIT_COND_BRANCH (ins, OP_IBEQ - OP_IBEQ);
			break;
		case OP_FBNE_UN:
			EMIT_COND_BRANCH (ins, OP_IBNE_UN - OP_IBEQ);
			break;
		case OP_FBLT:
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_MI); /* N set */
			break;
		case OP_FBLT_UN:
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_VS); /* V set */
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_MI); /* N set */
			break;
		case OP_FBGT:
		case OP_FBGT_UN:
		case OP_FBLE:
		case OP_FBLE_UN:
			g_assert_not_reached ();
			break;
		case OP_FBGE:
			if (IS_VFP) {
				EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_GE);
			} else {
				/* FPA requires EQ even thou the docs suggests that just CS is enough */
				EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_EQ);
				EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_CS);
			}
			break;
		case OP_FBGE_UN:
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_VS); /* V set */
			EMIT_COND_BRANCH_FLAGS (ins, ARMCOND_GE);
			break;

		case OP_CKFINITE: {
			if (IS_FPA) {
				if (ins->dreg != ins->sreg1)
					ARM_FPA_MVFD (code, ins->dreg, ins->sreg1);
			} else if (IS_VFP) {
				ARM_ABSD (code, ARM_VFP_D1, ins->sreg1);
				ARM_FLDD (code, ARM_VFP_D0, ARMREG_PC, 0);
				ARM_B (code, 1);
				*(guint32*)code = 0xffffffff;
				code += 4;
				*(guint32*)code = 0x7fefffff;
				code += 4;
				ARM_CMPD (code, ARM_VFP_D1, ARM_VFP_D0);
				ARM_FMSTAT (code);
				EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_GT, "ArithmeticException");
				ARM_CMPD (code, ins->sreg1, ins->sreg1);
				ARM_FMSTAT (code);
				EMIT_COND_SYSTEM_EXCEPTION_FLAGS (ARMCOND_VS, "ArithmeticException");
				ARM_CPYD (code, ins->dreg, ins->sreg1);
			}
			break;
		}

		case OP_GC_LIVENESS_DEF:
		case OP_GC_LIVENESS_USE:
		case OP_GC_PARAM_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			break;
		case OP_GC_SPILL_SLOT_LIVENESS_DEF:
			ins->backend.pc_offset = code - cfg->native_code;
			bb->spill_slot_defs = g_slist_prepend_mempool (cfg->mempool, bb->spill_slot_defs, ins);
			break;

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
	}

	cfg->code_len = code - cfg->native_code;
}

#endif /* DISABLE_JIT */

#ifdef HAVE_AEABI_READ_TP
void __aeabi_read_tp (void);
#endif

void
mono_arch_register_lowlevel_calls (void)
{
	/* The signature doesn't matter */
	mono_register_jit_icall (mono_arm_throw_exception, "mono_arm_throw_exception", mono_create_icall_signature ("void"), TRUE);
	mono_register_jit_icall (mono_arm_throw_exception_by_token, "mono_arm_throw_exception_by_token", mono_create_icall_signature ("void"), TRUE);

#ifndef MONO_CROSS_COMPILE
#ifdef HAVE_AEABI_READ_TP
	mono_register_jit_icall (__aeabi_read_tp, "__aeabi_read_tp", mono_create_icall_signature ("void"), TRUE);
#endif
#endif
}

#define patch_lis_ori(ip,val) do {\
		guint16 *__lis_ori = (guint16*)(ip);	\
		__lis_ori [1] = (((guint32)(val)) >> 16) & 0xffff;	\
		__lis_ori [3] = ((guint32)(val)) & 0xffff;	\
	} while (0)

void
mono_arch_patch_code (MonoMethod *method, MonoDomain *domain, guint8 *code, MonoJumpInfo *ji, MonoCodeManager *dyn_code_mp, gboolean run_cctors)
{
	MonoJumpInfo *patch_info;
	gboolean compile_aot = !run_cctors;

	for (patch_info = ji; patch_info; patch_info = patch_info->next) {
		unsigned char *ip = patch_info->ip.i + code;
		const unsigned char *target;

		if (patch_info->type == MONO_PATCH_INFO_SWITCH && !compile_aot) {
			gpointer *jt = (gpointer*)(ip + 8);
			int i;
			/* jt is the inlined jump table, 2 instructions after ip
			 * In the normal case we store the absolute addresses,
			 * otherwise the displacements.
			 */
			for (i = 0; i < patch_info->data.table->table_size; i++)
				jt [i] = code + (int)patch_info->data.table->table [i];
			continue;
		}
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
		case MONO_PATCH_INFO_IP:
			g_assert_not_reached ();
			patch_lis_ori (ip, ip);
			continue;
		case MONO_PATCH_INFO_METHOD_REL:
			g_assert_not_reached ();
			*((gpointer *)(ip)) = code + patch_info->data.offset;
			continue;
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
			g_assert_not_reached ();
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
		arm_patch_general (domain, ip, target, dyn_code_mp);
	}
}

#ifndef DISABLE_JIT

/*
 * Stack frame layout:
 * 
 *   ------------------- fp
 *   	MonoLMF structure or saved registers
 *   -------------------
 *   	locals
 *   -------------------
 *   	spilled regs
 *   -------------------
 *   	optional 8 bytes for tracing
 *   -------------------
 *   	param area             size is cfg->param_area
 *   ------------------- sp
 */
guint8 *
mono_arch_emit_prolog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	MonoBasicBlock *bb;
	MonoMethodSignature *sig;
	MonoInst *inst;
	int alloc_size, orig_alloc_size, pos, max_offset, i, rot_amount;
	guint8 *code;
	CallInfo *cinfo;
	int tracing = 0;
	int lmf_offset = 0;
	int prev_sp_offset, reg_offset;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method))
		tracing = 1;

	sig = mono_method_signature (method);
	cfg->code_size = 256 + sig->param_count * 64;
	code = cfg->native_code = g_malloc (cfg->code_size);

	mono_emit_unwind_op_def_cfa (cfg, code, ARMREG_SP, 0);

	alloc_size = cfg->stack_offset;
	pos = 0;
	prev_sp_offset = 0;

	if (!method->save_lmf) {
		if (iphone_abi) {
			/* 
			 * The iphone uses R7 as the frame pointer, and it points at the saved
			 * r7+lr:
			 *         <lr>
			 * r7 ->   <r7>
			 *         <rest of frame>
			 * We can't use r7 as a frame pointer since it points into the middle of
			 * the frame, so we keep using our own frame pointer.
			 * FIXME: Optimize this.
			 */
			g_assert (darwin);
			ARM_PUSH (code, (1 << ARMREG_R7) | (1 << ARMREG_LR));
			ARM_MOV_REG_REG (code, ARMREG_R7, ARMREG_SP);
			prev_sp_offset += 8; /* r7 and lr */
			mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset);
			mono_emit_unwind_op_offset (cfg, code, ARMREG_R7, (- prev_sp_offset) + 0);

			/* No need to push LR again */
			if (cfg->used_int_regs)
				ARM_PUSH (code, cfg->used_int_regs);
		} else {
			ARM_PUSH (code, cfg->used_int_regs | (1 << ARMREG_LR));
			prev_sp_offset += 4;
		}
		for (i = 0; i < 16; ++i) {
			if (cfg->used_int_regs & (1 << i))
				prev_sp_offset += 4;
		}
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset);
		reg_offset = 0;
		for (i = 0; i < 16; ++i) {
			if ((cfg->used_int_regs & (1 << i))) {
				mono_emit_unwind_op_offset (cfg, code, i, (- prev_sp_offset) + reg_offset);
				mini_gc_set_slot_type_from_cfa (cfg, (- prev_sp_offset) + reg_offset, SLOT_NOREF);
				reg_offset += 4;
			}
		}
		if (iphone_abi) {
			mono_emit_unwind_op_offset (cfg, code, ARMREG_LR, -4);
			mini_gc_set_slot_type_from_cfa (cfg, -4, SLOT_NOREF);
		} else {
			mono_emit_unwind_op_offset (cfg, code, ARMREG_LR, -4);
			mini_gc_set_slot_type_from_cfa (cfg, -4, SLOT_NOREF);
		}
	} else {
		ARM_MOV_REG_REG (code, ARMREG_IP, ARMREG_SP);
		ARM_PUSH (code, 0x5ff0);
		prev_sp_offset += 4 * 10; /* all but r0-r3, sp and pc */
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset);
		reg_offset = 0;
		for (i = 0; i < 16; ++i) {
			if ((i > ARMREG_R3) && (i != ARMREG_SP) && (i != ARMREG_PC)) {
				mono_emit_unwind_op_offset (cfg, code, i, (- prev_sp_offset) + reg_offset);
				reg_offset += 4;
			}
		}
		pos += sizeof (MonoLMF) - prev_sp_offset;
		lmf_offset = pos;
	}
	alloc_size += pos;
	orig_alloc_size = alloc_size;
	// align to MONO_ARCH_FRAME_ALIGNMENT bytes
	if (alloc_size & (MONO_ARCH_FRAME_ALIGNMENT - 1)) {
		alloc_size += MONO_ARCH_FRAME_ALIGNMENT - 1;
		alloc_size &= ~(MONO_ARCH_FRAME_ALIGNMENT - 1);
	}

	/* the stack used in the pushed regs */
	if (prev_sp_offset & 4)
		alloc_size += 4;
	cfg->stack_usage = alloc_size;
	if (alloc_size) {
		if ((i = mono_arm_is_rotated_imm8 (alloc_size, &rot_amount)) >= 0) {
			ARM_SUB_REG_IMM (code, ARMREG_SP, ARMREG_SP, i, rot_amount);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_IP, alloc_size);
			ARM_SUB_REG_REG (code, ARMREG_SP, ARMREG_SP, ARMREG_IP);
		}
		mono_emit_unwind_op_def_cfa_offset (cfg, code, prev_sp_offset + alloc_size);
	}
	if (cfg->frame_reg != ARMREG_SP) {
		ARM_MOV_REG_REG (code, cfg->frame_reg, ARMREG_SP);
		mono_emit_unwind_op_def_cfa_reg (cfg, code, cfg->frame_reg);
	}
	//g_print ("prev_sp_offset: %d, alloc_size:%d\n", prev_sp_offset, alloc_size);
	prev_sp_offset += alloc_size;

	for (i = 0; i < alloc_size - orig_alloc_size; i += 4)
		mini_gc_set_slot_type_from_cfa (cfg, (- prev_sp_offset) + orig_alloc_size + i, SLOT_NOREF);

        /* compute max_offset in order to use short forward jumps
	 * we could skip do it on arm because the immediate displacement
	 * for jumps is large enough, it may be useful later for constant pools
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

	/* store runtime generic context */
	if (cfg->rgctx_var) {
		MonoInst *ins = cfg->rgctx_var;

		g_assert (ins->opcode == OP_REGOFFSET);

		if (arm_is_imm12 (ins->inst_offset)) {
			ARM_STR_IMM (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ins->inst_offset);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
			ARM_STR_REG_REG (code, MONO_ARCH_RGCTX_REG, ins->inst_basereg, ARMREG_LR);
		}
	}

	/* load arguments allocated to register from the stack */
	pos = 0;

	cinfo = get_call_info (cfg->generic_sharing_context, NULL, sig);

	if (MONO_TYPE_ISSTRUCT (sig->ret) && cinfo->ret.storage != RegTypeStructByVal) {
		ArgInfo *ainfo = &cinfo->ret;
		inst = cfg->vret_addr;
		g_assert (arm_is_imm12 (inst->inst_offset));
		ARM_STR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
	}

	if (sig->call_convention == MONO_CALL_VARARG) {
		ArgInfo *cookie = &cinfo->sig_cookie;

		/* Save the sig cookie address */
		g_assert (cookie->storage == RegTypeBase);

		g_assert (arm_is_imm12 (prev_sp_offset + cookie->offset));
		g_assert (arm_is_imm12 (cfg->sig_cookie));
		ARM_ADD_REG_IMM8 (code, ARMREG_IP, cfg->frame_reg, prev_sp_offset + cookie->offset);
		ARM_STR_IMM (code, ARMREG_IP, cfg->frame_reg, cfg->sig_cookie);
	}

	for (i = 0; i < sig->param_count + sig->hasthis; ++i) {
		ArgInfo *ainfo = cinfo->args + i;
		inst = cfg->args [pos];
		
		if (cfg->verbose_level > 2)
			g_print ("Saving argument %d (type: %d)\n", i, ainfo->storage);
		if (inst->opcode == OP_REGVAR) {
			if (ainfo->storage == RegTypeGeneral)
				ARM_MOV_REG_REG (code, inst->dreg, ainfo->reg);
			else if (ainfo->storage == RegTypeFP) {
				g_assert_not_reached ();
			} else if (ainfo->storage == RegTypeBase) {
				if (arm_is_imm12 (prev_sp_offset + ainfo->offset)) {
					ARM_LDR_IMM (code, inst->dreg, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
					ARM_LDR_REG_REG (code, inst->dreg, ARMREG_SP, ARMREG_IP);
				}
			} else
				g_assert_not_reached ();

			if (cfg->verbose_level > 2)
				g_print ("Argument %d assigned to register %s\n", pos, mono_arch_regname (inst->dreg));
		} else {
			/* the argument should be put on the stack: FIXME handle size != word  */
			if (ainfo->storage == RegTypeGeneral || ainfo->storage == RegTypeIRegPair) {
				switch (ainfo->size) {
				case 1:
					if (arm_is_imm12 (inst->inst_offset))
						ARM_STRB_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRB_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 2:
					if (arm_is_imm8 (inst->inst_offset)) {
						ARM_STRH_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRH_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 8:
					g_assert (arm_is_imm12 (inst->inst_offset));
					ARM_STR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					g_assert (arm_is_imm12 (inst->inst_offset + 4));
					ARM_STR_IMM (code, ainfo->reg + 1, inst->inst_basereg, inst->inst_offset + 4);
					break;
				default:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ainfo->reg, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ainfo->reg, inst->inst_basereg, ARMREG_IP);
					}
					break;
				}
			} else if (ainfo->storage == RegTypeBaseGen) {
				g_assert (arm_is_imm12 (prev_sp_offset + ainfo->offset));
				g_assert (arm_is_imm12 (inst->inst_offset));
				ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset + 4);
				ARM_STR_IMM (code, ARMREG_R3, inst->inst_basereg, inst->inst_offset);
			} else if (ainfo->storage == RegTypeBase) {
				if (arm_is_imm12 (prev_sp_offset + ainfo->offset)) {
					ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, (prev_sp_offset + ainfo->offset));
				} else {
					code = mono_arm_emit_load_imm (code, ARMREG_IP, prev_sp_offset + ainfo->offset);
					ARM_LDR_REG_REG (code, ARMREG_LR, ARMREG_SP, ARMREG_IP);
				}

				switch (ainfo->size) {
				case 1:
					if (arm_is_imm8 (inst->inst_offset)) {
						ARM_STRB_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRB_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 2:
					if (arm_is_imm8 (inst->inst_offset)) {
						ARM_STRH_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STRH_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				case 8:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					if (arm_is_imm12 (prev_sp_offset + ainfo->offset + 4)) {
						ARM_LDR_IMM (code, ARMREG_LR, ARMREG_SP, (prev_sp_offset + ainfo->offset + 4));
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, prev_sp_offset + ainfo->offset + 4);
						ARM_LDR_REG_REG (code, ARMREG_LR, ARMREG_SP, ARMREG_IP);
					}
					if (arm_is_imm12 (inst->inst_offset + 4)) {
						ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset + 4);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset + 4);
						ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				default:
					if (arm_is_imm12 (inst->inst_offset)) {
						ARM_STR_IMM (code, ARMREG_LR, inst->inst_basereg, inst->inst_offset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, inst->inst_offset);
						ARM_STR_REG_REG (code, ARMREG_LR, inst->inst_basereg, ARMREG_IP);
					}
					break;
				}
			} else if (ainfo->storage == RegTypeFP) {
				g_assert_not_reached ();
			} else if (ainfo->storage == RegTypeStructByVal) {
				int doffset = inst->inst_offset;
				int soffset = 0;
				int cur_reg;
				int size = 0;
				size = mini_type_stack_size_full (cfg->generic_sharing_context, inst->inst_vtype, NULL, sig->pinvoke);
				for (cur_reg = 0; cur_reg < ainfo->size; ++cur_reg) {
					if (arm_is_imm12 (doffset)) {
						ARM_STR_IMM (code, ainfo->reg + cur_reg, inst->inst_basereg, doffset);
					} else {
						code = mono_arm_emit_load_imm (code, ARMREG_IP, doffset);
						ARM_STR_REG_REG (code, ainfo->reg + cur_reg, inst->inst_basereg, ARMREG_IP);
					}
					soffset += sizeof (gpointer);
					doffset += sizeof (gpointer);
				}
				if (ainfo->vtsize) {
					/* FIXME: handle overrun! with struct sizes not multiple of 4 */
					//g_print ("emit_memcpy (prev_sp_ofs: %d, ainfo->offset: %d, soffset: %d)\n", prev_sp_offset, ainfo->offset, soffset);
					code = emit_memcpy (code, ainfo->vtsize * sizeof (gpointer), inst->inst_basereg, doffset, ARMREG_SP, prev_sp_offset + ainfo->offset);
				}
			} else if (ainfo->storage == RegTypeStructByAddr) {
				g_assert_not_reached ();
				/* FIXME: handle overrun! with struct sizes not multiple of 4 */
				code = emit_memcpy (code, ainfo->vtsize * sizeof (gpointer), inst->inst_basereg, inst->inst_offset, ainfo->reg, 0);
			} else
				g_assert_not_reached ();
		}
		pos++;
	}

	if (method->save_lmf)
		code = emit_save_lmf (cfg, code, alloc_size - lmf_offset);

	if (tracing)
		code = mono_arch_instrument_prolog (cfg, mono_trace_enter_method, code, TRUE);

	if (cfg->arch.seq_point_info_var) {
		MonoInst *ins = cfg->arch.seq_point_info_var;

		/* Initialize the variable from a GOT slot */
		mono_add_patch_info (cfg, code - cfg->native_code, MONO_PATCH_INFO_SEQ_POINT_INFO, cfg->method);
		ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(gpointer*)code = NULL;
		code += 4;
		ARM_LDR_REG_REG (code, ARMREG_R0, ARMREG_PC, ARMREG_R0);

		g_assert (ins->opcode == OP_REGOFFSET);

		if (arm_is_imm12 (ins->inst_offset)) {
			ARM_STR_IMM (code, ARMREG_R0, ins->inst_basereg, ins->inst_offset);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
			ARM_STR_REG_REG (code, ARMREG_R0, ins->inst_basereg, ARMREG_LR);
		}
	}

	/* Initialize ss_trigger_page_var */
	if (!cfg->soft_breakpoints) {
		MonoInst *info_var = cfg->arch.seq_point_info_var;
		MonoInst *ss_trigger_page_var = cfg->arch.ss_trigger_page_var;
		int dreg = ARMREG_LR;

		if (info_var) {
			g_assert (info_var->opcode == OP_REGOFFSET);
			g_assert (arm_is_imm12 (info_var->inst_offset));

			ARM_LDR_IMM (code, dreg, info_var->inst_basereg, info_var->inst_offset);
			/* Load the trigger page addr */
			ARM_LDR_IMM (code, dreg, dreg, G_STRUCT_OFFSET (SeqPointInfo, ss_trigger_page));
			ARM_STR_IMM (code, dreg, ss_trigger_page_var->inst_basereg, ss_trigger_page_var->inst_offset);
		}
	}

	if (cfg->arch.seq_point_read_var) {
		MonoInst *read_ins = cfg->arch.seq_point_read_var;
		MonoInst *ss_method_ins = cfg->arch.seq_point_ss_method_var;
		MonoInst *bp_method_ins = cfg->arch.seq_point_bp_method_var;

		g_assert (read_ins->opcode == OP_REGOFFSET);
		g_assert (arm_is_imm12 (read_ins->inst_offset));
		g_assert (ss_method_ins->opcode == OP_REGOFFSET);
		g_assert (arm_is_imm12 (ss_method_ins->inst_offset));
		g_assert (bp_method_ins->opcode == OP_REGOFFSET);
		g_assert (arm_is_imm12 (bp_method_ins->inst_offset));

		ARM_MOV_REG_REG (code, ARMREG_LR, ARMREG_PC);
		ARM_B (code, 2);
		*(volatile int **)code = &ss_trigger_var;
		code += 4;
		*(gpointer*)code = single_step_func_wrapper;
		code += 4;
		*(gpointer*)code = breakpoint_func_wrapper;
		code += 4;

		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_LR, 0);
		ARM_STR_IMM (code, ARMREG_IP, read_ins->inst_basereg, read_ins->inst_offset);
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_LR, 4);
		ARM_STR_IMM (code, ARMREG_IP, ss_method_ins->inst_basereg, ss_method_ins->inst_offset);
		ARM_LDR_IMM (code, ARMREG_IP, ARMREG_LR, 8);
		ARM_STR_IMM (code, ARMREG_IP, bp_method_ins->inst_basereg, bp_method_ins->inst_offset);
	}

	cfg->code_len = code - cfg->native_code;
	g_assert (cfg->code_len < cfg->code_size);
	g_free (cinfo);

	return code;
}

void
mono_arch_emit_epilog (MonoCompile *cfg)
{
	MonoMethod *method = cfg->method;
	int pos, i, rot_amount;
	int max_epilog_size = 16 + 20*4;
	guint8 *code;
	CallInfo *cinfo;

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

	/*
	 * Keep in sync with OP_JMP
	 */
	code = cfg->native_code + cfg->code_len;

	if (mono_jit_trace_calls != NULL && mono_trace_eval (method)) {
		code = mono_arch_instrument_epilog (cfg, mono_trace_leave_method, code, TRUE);
	}
	pos = 0;

	/* Load returned vtypes into registers if needed */
	cinfo = cfg->arch.cinfo;
	if (cinfo->ret.storage == RegTypeStructByVal) {
		MonoInst *ins = cfg->ret;

		if (arm_is_imm12 (ins->inst_offset)) {
			ARM_LDR_IMM (code, ARMREG_R0, ins->inst_basereg, ins->inst_offset);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_LR, ins->inst_offset);
			ARM_LDR_REG_REG (code, ARMREG_R0, ins->inst_basereg, ARMREG_LR);
		}
	}

	if (method->save_lmf) {
		int lmf_offset, reg, sp_adj, regmask;
		/* all but r0-r3, sp and pc */
		pos += sizeof (MonoLMF) - (MONO_ARM_NUM_SAVED_REGS * sizeof (mgreg_t));
		lmf_offset = pos;

		code = emit_restore_lmf (cfg, code, cfg->stack_usage - lmf_offset);

		/* This points to r4 inside MonoLMF->iregs */
		sp_adj = (sizeof (MonoLMF) - MONO_ARM_NUM_SAVED_REGS * sizeof (mgreg_t));
		reg = ARMREG_R4;
		regmask = 0x9ff0; /* restore lr to pc */
		/* Skip caller saved registers not used by the method */
		while (!(cfg->used_int_regs & (1 << reg)) && reg < ARMREG_FP) {
			regmask &= ~(1 << reg);
			sp_adj += 4;
			reg ++;
		}
		/* point sp at the registers to restore: 10 is 14 -4, because we skip r0-r3 */
		code = emit_big_add (code, ARMREG_SP, cfg->frame_reg, cfg->stack_usage - lmf_offset + sp_adj);
		/* restore iregs */
		ARM_POP (code, regmask); 
	} else {
		if ((i = mono_arm_is_rotated_imm8 (cfg->stack_usage, &rot_amount)) >= 0) {
			ARM_ADD_REG_IMM (code, ARMREG_SP, cfg->frame_reg, i, rot_amount);
		} else {
			code = mono_arm_emit_load_imm (code, ARMREG_IP, cfg->stack_usage);
			ARM_ADD_REG_REG (code, ARMREG_SP, cfg->frame_reg, ARMREG_IP);
		}

		if (iphone_abi) {
			/* Restore saved gregs */
			if (cfg->used_int_regs)
				ARM_POP (code, cfg->used_int_regs);
			/* Restore saved r7, restore LR to PC */
			ARM_POP (code, (1 << ARMREG_R7) | (1 << ARMREG_PC));
		} else {
			ARM_POP (code, cfg->used_int_regs | (1 << ARMREG_PC));
		}
	}

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
	if (strcmp (name, "ArgumentException") == 0)
		return MONO_EXC_ARGUMENT;
	g_error ("Unknown intrinsic exception %s\n", name);
	return -1;
}

void
mono_arch_emit_exceptions (MonoCompile *cfg)
{
	MonoJumpInfo *patch_info;
	int i;
	guint8 *code;
	guint8* exc_throw_pos [MONO_EXC_INTRINS_NUM];
	guint8 exc_throw_found [MONO_EXC_INTRINS_NUM];
	int max_epilog_size = 50;

	for (i = 0; i < MONO_EXC_INTRINS_NUM; i++) {
		exc_throw_pos [i] = NULL;
		exc_throw_found [i] = 0;
	}

	/* count the number of exception infos */
     
	/* 
	 * make sure we have enough space for exceptions
	 */
	for (patch_info = cfg->patch_info; patch_info; patch_info = patch_info->next) {
		if (patch_info->type == MONO_PATCH_INFO_EXC) {
			i = exception_id_by_name (patch_info->data.target);
			if (!exc_throw_found [i]) {
				max_epilog_size += 32;
				exc_throw_found [i] = TRUE;
			}
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
		case MONO_PATCH_INFO_EXC: {
			MonoClass *exc_class;
			unsigned char *ip = patch_info->ip.i + cfg->native_code;

			i = exception_id_by_name (patch_info->data.target);
			if (exc_throw_pos [i]) {
				arm_patch (ip, exc_throw_pos [i]);
				patch_info->type = MONO_PATCH_INFO_NONE;
				break;
			} else {
				exc_throw_pos [i] = code;
			}
			arm_patch (ip, code);

			exc_class = mono_class_from_name (mono_defaults.corlib, "System", patch_info->data.name);
			g_assert (exc_class);

			ARM_MOV_REG_REG (code, ARMREG_R1, ARMREG_LR);
			ARM_LDR_IMM (code, ARMREG_R0, ARMREG_PC, 0);
			patch_info->type = MONO_PATCH_INFO_INTERNAL_METHOD;
			patch_info->data.name = "mono_arch_throw_corlib_exception";
			patch_info->ip.i = code - cfg->native_code;
			ARM_BL (code, 0);
			*(guint32*)(gpointer)code = exc_class->type_token;
			code += 4;
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

#endif /* #ifndef DISABLE_JIT */

void
mono_arch_finish_init (void)
{
	lmf_tls_offset = mono_get_lmf_tls_offset ();
	lmf_addr_tls_offset = mono_get_lmf_addr_tls_offset ();
}

void
mono_arch_free_jit_tls_data (MonoJitTlsData *tls)
{
}

MonoInst*
mono_arch_emit_inst_for_method (MonoCompile *cfg, MonoMethod *cmethod, MonoMethodSignature *fsig, MonoInst **args)
{
	/* FIXME: */
	return NULL;
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

guint32
mono_arch_get_patch_offset (guint8 *code)
{
	/* OP_AOTCONST */
	return 8;
}

void
mono_arch_flush_register_windows (void)
{
}

#ifdef MONO_ARCH_HAVE_IMT

#ifndef DISABLE_JIT

void
mono_arch_emit_imt_argument (MonoCompile *cfg, MonoCallInst *call, MonoInst *imt_arg)
{
	if (cfg->compile_aot) {
		int method_reg = mono_alloc_ireg (cfg);
		MonoInst *ins;

		call->dynamic_imt_arg = TRUE;

		if (imt_arg) {
			mono_call_inst_add_outarg_reg (cfg, call, imt_arg->dreg, ARMREG_V5, FALSE);
		} else {
			MONO_INST_NEW (cfg, ins, OP_AOTCONST);
			ins->dreg = method_reg;
			ins->inst_p0 = call->method;
			ins->inst_c1 = MONO_PATCH_INFO_METHODCONST;
			MONO_ADD_INS (cfg->cbb, ins);

			mono_call_inst_add_outarg_reg (cfg, call, method_reg, ARMREG_V5, FALSE);
		}
	} else if (cfg->generic_context || imt_arg || mono_use_llvm) {

		/* Always pass in a register for simplicity */
		call->dynamic_imt_arg = TRUE;

		cfg->uses_rgctx_reg = TRUE;

		if (imt_arg) {
			mono_call_inst_add_outarg_reg (cfg, call, imt_arg->dreg, ARMREG_V5, FALSE);
		} else {
			MonoInst *ins;
			int method_reg = mono_alloc_preg (cfg);

			MONO_INST_NEW (cfg, ins, OP_PCONST);
			ins->inst_p0 = call->method;
			ins->dreg = method_reg;
			MONO_ADD_INS (cfg->cbb, ins);

			mono_call_inst_add_outarg_reg (cfg, call, method_reg, ARMREG_V5, FALSE);
		}
	}
}

#endif /* DISABLE_JIT */

MonoMethod*
mono_arch_find_imt_method (mgreg_t *regs, guint8 *code)
{
	guint32 *code_ptr = (guint32*)code;
	code_ptr -= 2;

	if (mono_use_llvm)
		/* Passed in V5 */
		return (MonoMethod*)regs [ARMREG_V5];

	/* The IMT value is stored in the code stream right after the LDC instruction. */
	if (!IS_LDR_PC (code_ptr [0])) {
		g_warning ("invalid code stream, instruction before IMT value is not a LDC in %s() (code %p value 0: 0x%x -1: 0x%x -2: 0x%x)", __FUNCTION__, code, code_ptr [2], code_ptr [1], code_ptr [0]);
		g_assert (IS_LDR_PC (code_ptr [0]));
	}
	if (code_ptr [1] == 0)
		/* This is AOTed code, the IMT method is in V5 */
		return (MonoMethod*)regs [ARMREG_V5];
	else
		return (MonoMethod*) code_ptr [1];
}

MonoVTable*
mono_arch_find_static_call_vtable (mgreg_t *regs, guint8 *code)
{
	return (MonoVTable*) regs [MONO_ARCH_RGCTX_REG];
}

#define ENABLE_WRONG_METHOD_CHECK 0
#define BASE_SIZE (6 * 4)
#define BSEARCH_ENTRY_SIZE (4 * 4)
#define CMP_SIZE (3 * 4)
#define BRANCH_SIZE (1 * 4)
#define CALL_SIZE (2 * 4)
#define WMC_SIZE (5 * 4)
#define DISTANCE(A, B) (((gint32)(B)) - ((gint32)(A)))

static arminstr_t *
arm_emit_value_and_patch_ldr (arminstr_t *code, arminstr_t *target, guint32 value)
{
	guint32 delta = DISTANCE (target, code);
	delta -= 8;
	g_assert (delta >= 0 && delta <= 0xFFF);
	*target = *target | delta;
	*code = value;
	return code + 1;
}

gpointer
mono_arch_build_imt_thunk (MonoVTable *vtable, MonoDomain *domain, MonoIMTCheckItem **imt_entries, int count,
	gpointer fail_tramp)
{
	int size, i, extra_space = 0;
	arminstr_t *code, *start, *vtable_target = NULL;
	gboolean large_offsets = FALSE;
	guint32 **constant_pool_starts;

	size = BASE_SIZE;
	constant_pool_starts = g_new0 (guint32*, count);

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->has_target_code || !arm_is_imm12 (DISTANCE (vtable, &vtable->vtable[item->value.vtable_slot]))) {
				item->chunk_size += 32;
				large_offsets = TRUE;
			}

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case)
					item->chunk_size += CMP_SIZE;
				item->chunk_size += BRANCH_SIZE;
			} else {
#if ENABLE_WRONG_METHOD_CHECK
				item->chunk_size += WMC_SIZE;
#endif
			}
			if (fail_case) {
				item->chunk_size += 16;
				large_offsets = TRUE;
			}
			item->chunk_size += CALL_SIZE;
		} else {
			item->chunk_size += BSEARCH_ENTRY_SIZE;
			imt_entries [item->check_target_idx]->compare_done = TRUE;
		}
		size += item->chunk_size;
	}

	if (large_offsets)
		size += 4 * count; /* The ARM_ADD_REG_IMM to pop the stack */

	if (fail_tramp)
		code = mono_method_alloc_generic_virtual_thunk (domain, size);
	else
		code = mono_domain_code_reserve (domain, size);
	start = code;

#if DEBUG_IMT
	printf ("building IMT thunk for class %s %s entries %d code size %d code at %p end %p vtable %p\n", vtable->klass->name_space, vtable->klass->name, count, size, start, ((guint8*)start) + size, vtable);
	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		printf ("method %d (%p) %s vtable slot %p is_equals %d chunk size %d\n", i, item->key, item->key->name, &vtable->vtable [item->value.vtable_slot], item->is_equals, item->chunk_size);
	}
#endif

	if (large_offsets)
		ARM_PUSH4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
	else
		ARM_PUSH2 (code, ARMREG_R0, ARMREG_R1);
	ARM_LDR_IMM (code, ARMREG_R0, ARMREG_LR, -4);
	vtable_target = code;
	ARM_LDR_IMM (code, ARMREG_IP, ARMREG_PC, 0);

	if (mono_use_llvm) {
		/* LLVM always passes the IMT method in R5 */
		ARM_MOV_REG_REG (code, ARMREG_R0, ARMREG_V5);
	} else {
		/* R0 == 0 means we are called from AOT code. In this case, V5 contains the IMT method */
		ARM_CMP_REG_IMM8 (code, ARMREG_R0, 0);
		ARM_MOV_REG_REG_COND (code, ARMREG_R0, ARMREG_V5, ARMCOND_EQ);
	}

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		arminstr_t *imt_method = NULL, *vtable_offset_ins = NULL, *target_code_ins = NULL;
		gint32 vtable_offset;

		item->code_target = (guint8*)code;

		if (item->is_equals) {
			gboolean fail_case = !item->check_target_idx && fail_tramp;

			if (item->check_target_idx || fail_case) {
				if (!item->compare_done || fail_case) {
					imt_method = code;
					ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
					ARM_CMP_REG_REG (code, ARMREG_R0, ARMREG_R1);
				}
				item->jmp_code = (guint8*)code;
				ARM_B_COND (code, ARMCOND_NE, 0);
			} else {
				/*Enable the commented code to assert on wrong method*/
#if ENABLE_WRONG_METHOD_CHECK
				imt_method = code;
				ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
				ARM_CMP_REG_REG (code, ARMREG_R0, ARMREG_R1);
				ARM_B_COND (code, ARMCOND_NE, 1);

				ARM_DBRK (code);
#endif
			}

			if (item->has_target_code) {
				target_code_ins = code;
				/* Load target address */
				ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
				/* Save it to the fourth slot */
				ARM_STR_IMM (code, ARMREG_R1, ARMREG_SP, 3 * sizeof (gpointer));
				/* Restore registers and branch */
				ARM_POP4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
				
				code = arm_emit_value_and_patch_ldr (code, target_code_ins, (gsize)item->value.target_code);
			} else {
				vtable_offset = DISTANCE (vtable, &vtable->vtable[item->value.vtable_slot]);
				if (!arm_is_imm12 (vtable_offset)) {
					/* 
					 * We need to branch to a computed address but we don't have
					 * a free register to store it, since IP must contain the 
					 * vtable address. So we push the two values to the stack, and
					 * load them both using LDM.
					 */
					/* Compute target address */
					vtable_offset_ins = code;
					ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
					ARM_LDR_REG_REG (code, ARMREG_R1, ARMREG_IP, ARMREG_R1);
					/* Save it to the fourth slot */
					ARM_STR_IMM (code, ARMREG_R1, ARMREG_SP, 3 * sizeof (gpointer));
					/* Restore registers and branch */
					ARM_POP4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
				
					code = arm_emit_value_and_patch_ldr (code, vtable_offset_ins, vtable_offset);
				} else {
					ARM_POP2 (code, ARMREG_R0, ARMREG_R1);
					if (large_offsets)
						ARM_ADD_REG_IMM8 (code, ARMREG_SP, ARMREG_SP, 2 * sizeof (gpointer));
					ARM_LDR_IMM (code, ARMREG_PC, ARMREG_IP, vtable_offset);
				}
			}

			if (fail_case) {
				arm_patch (item->jmp_code, (guchar*)code);

				target_code_ins = code;
				/* Load target address */
				ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
				/* Save it to the fourth slot */
				ARM_STR_IMM (code, ARMREG_R1, ARMREG_SP, 3 * sizeof (gpointer));
				/* Restore registers and branch */
				ARM_POP4 (code, ARMREG_R0, ARMREG_R1, ARMREG_IP, ARMREG_PC);
				
				code = arm_emit_value_and_patch_ldr (code, target_code_ins, (gsize)fail_tramp);
				item->jmp_code = NULL;
			}

			if (imt_method)
				code = arm_emit_value_and_patch_ldr (code, imt_method, (guint32)item->key);

			/*must emit after unconditional branch*/
			if (vtable_target) {
				code = arm_emit_value_and_patch_ldr (code, vtable_target, (guint32)vtable);
				item->chunk_size += 4;
				vtable_target = NULL;
			}

			/*We reserve the space for bsearch IMT values after the first entry with an absolute jump*/
			constant_pool_starts [i] = code;
			if (extra_space) {
				code += extra_space;
				extra_space = 0;
			}
		} else {
			ARM_LDR_IMM (code, ARMREG_R1, ARMREG_PC, 0);
			ARM_CMP_REG_REG (code, ARMREG_R0, ARMREG_R1);

			item->jmp_code = (guint8*)code;
			ARM_B_COND (code, ARMCOND_GE, 0);
			++extra_space;
		}
	}

	for (i = 0; i < count; ++i) {
		MonoIMTCheckItem *item = imt_entries [i];
		if (item->jmp_code) {
			if (item->check_target_idx)
				arm_patch (item->jmp_code, imt_entries [item->check_target_idx]->code_target);
		}
		if (i > 0 && item->is_equals) {
			int j;
			arminstr_t *space_start = constant_pool_starts [i];
			for (j = i - 1; j >= 0 && !imt_entries [j]->is_equals; --j) {
				space_start = arm_emit_value_and_patch_ldr (space_start, (arminstr_t*)imt_entries [j]->code_target, (guint32)imt_entries [j]->key);
			}
		}
	}

#if DEBUG_IMT
	{
		char *buff = g_strdup_printf ("thunk_for_class_%s_%s_entries_%d", vtable->klass->name_space, vtable->klass->name, count);
		mono_disassemble_code (NULL, (guint8*)start, size, buff);
		g_free (buff);
	}
#endif

	g_free (constant_pool_starts);

	mono_arch_flush_icache ((guint8*)start, size);
	mono_stats.imt_thunks_size += code - start;

	g_assert (DISTANCE (start, code) <= size);
	return start;
}

#endif

mgreg_t
mono_arch_context_get_int_reg (MonoContext *ctx, int reg)
{
	return ctx->regs [reg];
}

void
mono_arch_context_set_int_reg (MonoContext *ctx, int reg, mgreg_t val)
{
	ctx->regs [reg] = val;
}

/*
 * mono_arch_get_trampolines:
 *
 *   Return a list of MonoTrampInfo structures describing arch specific trampolines
 * for AOT.
 */
GSList *
mono_arch_get_trampolines (gboolean aot)
{
	return mono_arm_get_exception_trampolines (aot);
}

/*
 * mono_arch_set_breakpoint:
 *
 *   Set a breakpoint at the native code corresponding to JI at NATIVE_OFFSET.
 * The location should contain code emitted by OP_SEQ_POINT.
 */
void
mono_arch_set_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	guint8 *code = ip;
	guint32 native_offset = ip - (guint8*)ji->code_start;
	MonoDebugOptions *opt = mini_get_debug_options ();

	if (opt->soft_breakpoints) {
		g_assert (!ji->from_aot);
		code += 4;
		ARM_BLX_REG (code, ARMREG_LR);
		mono_arch_flush_icache (code - 4, 4);
	} else if (ji->from_aot) {
		SeqPointInfo *info = mono_arch_get_seq_point_info (mono_domain_get (), ji->code_start);

		g_assert (native_offset % 4 == 0);
		g_assert (info->bp_addrs [native_offset / 4] == 0);
		info->bp_addrs [native_offset / 4] = bp_trigger_page;
	} else {
		int dreg = ARMREG_LR;

		/* Read from another trigger page */
		ARM_LDR_IMM (code, dreg, ARMREG_PC, 0);
		ARM_B (code, 0);
		*(int*)code = (int)bp_trigger_page;
		code += 4;
		ARM_LDR_IMM (code, dreg, dreg, 0);

		mono_arch_flush_icache (code - 16, 16);

#if 0
		/* This is currently implemented by emitting an SWI instruction, which 
		 * qemu/linux seems to convert to a SIGILL.
		 */
		*(int*)code = (0xef << 24) | 8;
		code += 4;
		mono_arch_flush_icache (code - 4, 4);
#endif
	}
}

/*
 * mono_arch_clear_breakpoint:
 *
 *   Clear the breakpoint at IP.
 */
void
mono_arch_clear_breakpoint (MonoJitInfo *ji, guint8 *ip)
{
	MonoDebugOptions *opt = mini_get_debug_options ();
	guint8 *code = ip;
	int i;

	if (opt->soft_breakpoints) {
		g_assert (!ji->from_aot);
		code += 4;
		ARM_NOP (code);
		mono_arch_flush_icache (code - 4, 4);
	} else if (ji->from_aot) {
		guint32 native_offset = ip - (guint8*)ji->code_start;
		SeqPointInfo *info = mono_arch_get_seq_point_info (mono_domain_get (), ji->code_start);

		g_assert (native_offset % 4 == 0);
		g_assert (info->bp_addrs [native_offset / 4] == bp_trigger_page);
		info->bp_addrs [native_offset / 4] = 0;
	} else {
		for (i = 0; i < 4; ++i)
			ARM_NOP (code);

		mono_arch_flush_icache (ip, code - ip);
	}
}
	
/*
 * mono_arch_start_single_stepping:
 *
 *   Start single stepping.
 */
void
mono_arch_start_single_stepping (void)
{
	if (ss_trigger_page)
		mono_mprotect (ss_trigger_page, mono_pagesize (), 0);
	else
		ss_trigger_var = 1;
}
	
/*
 * mono_arch_stop_single_stepping:
 *
 *   Stop single stepping.
 */
void
mono_arch_stop_single_stepping (void)
{
	if (ss_trigger_page)
		mono_mprotect (ss_trigger_page, mono_pagesize (), MONO_MMAP_READ);
	else
		ss_trigger_var = 0;
}

#if __APPLE__
#define DBG_SIGNAL SIGBUS
#else
#define DBG_SIGNAL SIGSEGV
#endif

/*
 * mono_arch_is_single_step_event:
 *
 *   Return whenever the machine state in SIGCTX corresponds to a single
 * step event.
 */
gboolean
mono_arch_is_single_step_event (void *info, void *sigctx)
{
	siginfo_t *sinfo = info;

	if (!ss_trigger_page)
		return FALSE;

	/* Sometimes the address is off by 4 */
	if (sinfo->si_addr >= ss_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)ss_trigger_page + 128)
		return TRUE;
	else
		return FALSE;
}

/*
 * mono_arch_is_breakpoint_event:
 *
 *   Return whenever the machine state in SIGCTX corresponds to a breakpoint event.
 */
gboolean
mono_arch_is_breakpoint_event (void *info, void *sigctx)
{
	siginfo_t *sinfo = info;

	if (!ss_trigger_page)
		return FALSE;

	if (sinfo->si_signo == DBG_SIGNAL) {
		/* Sometimes the address is off by 4 */
		if (sinfo->si_addr >= bp_trigger_page && (guint8*)sinfo->si_addr <= (guint8*)bp_trigger_page + 128)
			return TRUE;
		else
			return FALSE;
	} else {
		return FALSE;
	}
}

/*
 * mono_arch_skip_breakpoint:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_breakpoint (MonoContext *ctx, MonoJitInfo *ji)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 4);
}

/*
 * mono_arch_skip_single_step:
 *
 *   See mini-amd64.c for docs.
 */
void
mono_arch_skip_single_step (MonoContext *ctx)
{
	MONO_CONTEXT_SET_IP (ctx, (guint8*)MONO_CONTEXT_GET_IP (ctx) + 4);
}

/*
 * mono_arch_get_seq_point_info:
 *
 *   See mini-amd64.c for docs.
 */
gpointer
mono_arch_get_seq_point_info (MonoDomain *domain, guint8 *code)
{
	SeqPointInfo *info;
	MonoJitInfo *ji;

	// FIXME: Add a free function

	mono_domain_lock (domain);
	info = g_hash_table_lookup (domain_jit_info (domain)->arch_seq_points, 
								code);
	mono_domain_unlock (domain);

	if (!info) {
		ji = mono_jit_info_table_find (domain, (char*)code);
		g_assert (ji);

		info = g_malloc0 (sizeof (SeqPointInfo) + ji->code_size);

		info->ss_trigger_page = ss_trigger_page;
		info->bp_trigger_page = bp_trigger_page;

		mono_domain_lock (domain);
		g_hash_table_insert (domain_jit_info (domain)->arch_seq_points,
							 code, info);
		mono_domain_unlock (domain);
	}

	return info;
}

/*
 * mono_arch_set_target:
 *
 *   Set the target architecture the JIT backend should generate code for, in the form
 * of a GNU target triplet. Only used in AOT mode.
 */
void
mono_arch_set_target (char *mtriple)
{
	/* The GNU target triple format is not very well documented */
	if (strstr (mtriple, "armv7")) {
		v6_supported = TRUE;
		v7_supported = TRUE;
	}
	if (strstr (mtriple, "armv6")) {
		v6_supported = TRUE;
	}
	if (strstr (mtriple, "darwin")) {
		v5_supported = TRUE;
		thumb_supported = TRUE;
		darwin = TRUE;
		iphone_abi = TRUE;
	}
	if (strstr (mtriple, "gnueabi"))
		eabi_supported = TRUE;
}
